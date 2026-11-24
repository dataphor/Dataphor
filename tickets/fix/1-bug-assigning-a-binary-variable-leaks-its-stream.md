description: Storing a new picture or document into a value the engine is already holding throws the old contents away without releasing the memory behind them, so a routine that does this in a loop makes the server grow until it is restarted.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Stack.cs, src/engine/DAE/Runtime.Data/DataValue.cs, src/engine/DAE/Streams/ServerStreamManager.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs
repro: verified
difficulty: hard
----
## What happens

The engine keeps values too large to carry around inline - the two D4 types for them are `Binary` and `Graphic` - in a managed stream owned by the server, and it counts the holders of each one so that the last holder to let go is the one that frees it. The D4 operator `Diagnostics.StreamCount()` reports how many of those streams the server is holding; in a freshly started server with nothing running it is 0.

Declaring a D4 variable of type `Binary` or `Graphic` and giving it a value takes out one of those streams, and leaving the variable's scope gives it back. **Assigning a second value to a variable that already holds one takes out another stream and never gives back the first.** The orphan is held for the life of the session: nothing else refers to it, no later statement releases it, and only disconnecting frees it.

## The measurement

Verified 2026-11-23 against the current `modernization` head, through a throwaway NUnit fixture on `InProcessTestFixture` (built, run, then removed again - the fixture is not in the tree). A D4 block that declares one `Binary` variable and then assigns to it 100 times in a loop:

- `StreamCount()` is `0` before the block and `1` after the declaration, as expected.
- It is `101` after the 100 assignments - one new stream per assignment, none released.
- It is still `100` after the block has ended and the variable has gone out of scope. The variable's own stream was released; the 100 orphans were not.
- The same loop over a `String` variable leaves the count unchanged, so this is specific to the types the engine holds in streams.

Each orphan is an entry in the server's in-memory stream store (`MemoryStreamProvider`), so the bytes of the discarded value are retained too, not just a bookkeeping record. A D4 operator that builds up or iterates over picture or document values in a loop therefore grows the server's memory by the size of every intermediate value it ever held.

## Root cause and the two sites

The value in a stack slot is **owned by that slot**. That is already the engine's rule, in two places that both do the disposal this one is missing.

- `DeallocateVariableNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs`, around line 1348) releases a variable's value when its scope ends, with `DataValue.DisposeValue(program.ValueManager, program.Stack[Location])`. That is what frees the variable's own stream, and it is why the count comes back down by one rather than not at all.
- `InstructionNode.CleanupOperand` (`src/engine/DAE/Runtime.Instructions/InstructionNode.cs`, around line 217) releases an operator argument the same way for a `const`/`in` parameter.

`AssignmentNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs`, around line 1399) breaks the rule. It evaluates the new value, validates it, and calls `program.Stack.Poke(location, objectValue)`; `Stack.Poke` (`src/engine/DAE/Stack.cs`, around line 108) simply overwrites the array element. Nothing disposes what the slot held.

**There is a second place at the same seam, and whether it leaks on its own is the one thing left to establish.** `CleanupOperand`'s `Modifier.Var` branch, a few lines below the `Modifier.In` branch that does dispose, pokes an operator's `var` parameter back into the caller's slot without disposing what the caller's slot held - the same "overwrite a slot and drop what it held" shape as `AssignmentNode`. What was measured: calling an operator declared `create operator Replace(var ABinary : Binary)` whose body assigns to its parameter leaves **one** orphan per call (count 103 before the call, 104 after it, 103 again once the caller's variable left scope, against 102 before the caller's block). One orphan is exactly what the body's own assignment accounts for, through the `AssignmentNode` site above. So the write-back did not add a second one - which is evidence that a `var` parameter's slot and the caller's slot hold the same value rather than a copy, and that this branch has nothing live to dispose. Establish that rather than assuming it: if a `var` parameter ever does receive a copy, this branch leaks it, and either way it should go through the same helper as the other two so the question cannot come back.

`VariableNode` is **not** a third site: it pushes a fresh `null` slot and pokes into that, so there is nothing to dispose.

## What a fix has to get right

- **Aliasing.** Disposing the old value before storing the new one is only safe if the two are never the same object. Measured: they are not, today - `LB := LB` moved the count up by one and read back correctly, which means the value read out of the slot was copied (`DataValue.CopyValue` turns a stream identifier into a `StreamManager.Reference`) rather than handed back by reference. That is one observation, not a proof over every node that can produce a value, so the fix should both establish the rule and guard it: capture the old value, store the new one, and skip the disposal when the two are reference-equal. A wrong call here is a read of a deallocated stream, which is worse than the leak.
- **Everything, not just streams.** `DataValue.DisposeValue` also releases rows, lists and tables held in a slot. Whatever the leak costs for streams it costs for those, so the fix should not narrow itself to stream-backed scalars.
- **It is on every assignment in the language.** This is a hot path. The extra work for a native value (an integer, a string) is a few type tests inside `DisposeValue`, which already falls through for anything it does not recognise - but say what the cost is rather than assuming it is nothing. `PerfBaselineTest` and `tests/Dataphor.Benchmarks` are the instruments if it needs measuring.
- **Make the next site impossible, not just correct.** Three places overwrite a stack slot (`AssignmentNode`, `CleanupOperand`'s `Modifier.Var` branch, `VariableNode`) and none of them disposes; two other places dispose a slot's value without overwriting it (`DeallocateVariableNode`, `CleanupOperand`'s `Modifier.In` branch). Nothing in the engine couples the two halves, which is why one site could drift from the rule unnoticed. One helper - something like `PokeOwned(location, value, valueManager)` on `Stack`, or a method on `Program` - that disposes the displaced value, used by every site that overwrites a slot, means a fourth site cannot forget. This is the preferred shape; if it is rejected, record why at the site with a `NOTE:`.
- **A runtime tripwire is available if it is wanted.** A `DEBUG`-only check at program teardown that the server's stream count came back to its value on entry would catch this whole class the moment it reappears. It can only be added once this leak is gone, since it would fire on today's behaviour. Weigh it; do not add it silently.

## Expected behaviour

After the fix, in a session with nothing else running: `StreamCount()` returns to its earlier value after any number of assignments to a `Binary` or `Graphic` variable, and after any number of calls to an operator that assigns to a `var` parameter of one of those types; and reading a variable back after assigning to it still gives the value that was assigned.

## Tests this owes

Its reproduction, at the engine level: a fixture on `InProcessTestFixture` that evaluates `StreamCount()` around a D4 block, because the assertion is arithmetic on an exact number. The loop form - N assignments leave N orphans - is the one that fails loudly rather than by one. Pin the `var` parameter round trip as its own case alongside it, whatever the conclusion about `CleanupOperand` turns out to be: the measured number there is a fact worth holding, and it is the case that would catch a later change making a `var` parameter receive a copy.

`debt-streams-coverage` covers the same ground more broadly from a D4 coverage script and names this ticket as a prerequisite, because its "the count comes back to where it started" assertions cannot pass while this leak exists. Do not write that script here.

## Compatibility

Nothing an application can see changes except the engine's memory use. If the fix adds or changes a public member on `Stack` or `Program` - the helper above would - a custom C# extension could notice it, so that needs a `docs/migration-notes.md` line in the same commit.

## Where this came from

The planning pass on `debt-streams-coverage`, looking for a way for a D4 script to assert that the engine's stream bookkeeping balances. It found that `Diagnostics.StreamCount()` is that instrument, and then that it does not balance.
