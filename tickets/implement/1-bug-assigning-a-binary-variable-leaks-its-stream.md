description: Storing a new picture or document into a value the engine is already holding throws the old contents away without releasing the memory behind them, so a routine that does this in a loop makes the server grow until it is restarted.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Runtime/Stack.cs, src/engine/DAE/Runtime.Data/DataValue.cs, src/engine/DAE/Streams/ServerStreamManager.cs, src/engine/DAE/Streams/StreamManager.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/InProcessTestFixture.cs, docs/migration-notes.md
repro: verified
difficulty: hard
----
## What happens

The engine keeps values too large to carry around inline - the two D4 types for them are `Binary` and `Graphic` - in a managed stream owned by the server, and it counts the holders of each one so that the last holder to let go is the one that frees it. The D4 operator `Diagnostics.StreamCount()` reports how many of those streams the server is holding; in a freshly started server with nothing running it is 0.

Declaring a D4 variable of type `Binary` or `Graphic` and giving it a value takes out one of those streams, and leaving the variable's scope gives it back. **Assigning a second value to a variable that already holds one takes out another stream and never gives back the first.** The orphan is held for the life of the session: nothing else refers to it, no later statement releases it, and only disconnecting frees it.

Each orphan is an entry in the server's in-memory stream store, so the bytes of the discarded value are retained too, not just a bookkeeping record. A D4 operator that builds up or iterates over picture or document values in a loop therefore grows the server's memory by the size of every intermediate value it ever held.

## The measurement

Verified twice - when this ticket was filed, and again in the fix pass of 2026-11-23 - against the current `modernization` head, through a throwaway NUnit fixture on `InProcessTestFixture` that read `Diagnostics.StreamCount()` between D4 statements (built, run, then removed again; the fixture is not in the tree). Every run started from a server holding 0 streams. The numbers below are the change in the count across each script; "orphans" means streams the server was still holding after the script and its variables had gone.

| script | orphans |
|---|---:|
| a `Binary` variable declared, then assigned 100 times | **100** |
| the same loop over a `String` variable | 0 |
| a `row { B : Binary }` variable declared, then assigned 10 times | **10** |
| `LB := LB` once | **1** (and the value reads back correctly) |
| calling `create operator ZReplace(var AB : Binary) begin AB := Binary('0003'); end;` once | **1** (and `LB` reads back as the assigned value) |
| calling an operator with a `var Binary` parameter whose body never assigns to it | 0 |
| a block that declares a `Binary` variable and then raises | **1** - *not this ticket; see the sibling below* |
| the same, with the error caught by a D4 `try ... except` | **1** - *not this ticket* |
| an operator that assigns to its `var Binary` parameter and then raises, the error caught by the caller | **1**, and the caller's variable still reads back as the value it had **before** the call |

Also measured inside the block rather than after it: after 10 assignments the count was 11 - the ten orphans plus the variable's own live stream - so the orphans accumulate as the loop runs rather than at scope exit.

## Root cause

The value in a stack slot is **owned by that slot**. That is already the engine's rule, in the two places that do the disposal this one is missing:

- `DeallocateVariableNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs`, around line 1348) releases a variable's value when its scope ends, with `DataValue.DisposeValue(program.ValueManager, program.Stack[Location])`. That is what frees the variable's own stream, and it is why the count comes back down by one rather than not at all.
- `InstructionNode.CleanupOperand`'s `Modifier.In` branch (`src/engine/DAE/Runtime.Instructions/InstructionNode.cs`, around line 217) releases an operator argument the same way.

`AssignmentNode.InternalExecute` (`ImperativeNodes.cs`, around line 1399) breaks the rule. It evaluates the new value, validates it, and calls `program.Stack.Poke(location, objectValue)`; `Stack.Poke` (`src/engine/DAE/Stack.cs`, around line 108) simply overwrites the array element. Nothing disposes what the slot held. Every leak in the table above except the two marked "not this ticket" comes through this one line, including the `var` parameter case - the orphan there is the body's own assignment, not the write-back.

`VariableNode.InternalExecute` is **not** a second site: it pushes a fresh `null` slot and pokes into that, so there is nothing to dispose.

## The `var` parameter write-back: settled

The open question the previous pass left - whether `CleanupOperand`'s `Modifier.Var` branch leaks on its own - is answered: **it does not, because a `var` parameter's slot is an alias of the caller's slot, not a copy.** The chain, quoted here so the next reader does not have to re-derive it:

- A `var` argument is required to be a `StackReferenceNode`, and `InstructionNodeBase.DetermineDataType` sets `ByReference = true` on it (`InstructionNode.cs`, around line 165).
- `StackReferenceNode.InternalExecute` (`ImperativeNodes.cs`, around line 1776) returns `program.Stack[Location]` **unchanged** when `ByReference` is set, and `DataValue.CopyValue(...)` of it otherwise.
- `CallNode.InternalExecute` (`ImperativeNodes.cs`, around line 1164) then does `program.Stack.Push(arguments[index])` for each operand. So for a `var` operand the callee's slot and the caller's slot hold the same object.

The measurement agrees: an operator with a `var Binary` parameter whose body never assigns to it leaks nothing, which it could not do if the callee's slot held its own stream.

**This makes the `Modifier.Var` branch the one principled exception to the "a site that overwrites a slot disposes what it displaced" rule.** The write-back is an ownership *return*, not an overwrite: either the body never assigned, in which case the value being poked is the one already in the slot, or the body did assign, in which case the value the caller's slot holds was disposed by that assignment and must not be disposed again. Keep it a plain `Poke` and say so at the site with a `NOTE:`.

## The hazard this fix introduces, and what to do about it

Disposing what an assignment displaces is correct for an ordinary variable. For a `var` parameter it means **the callee disposes a value the caller's slot still points at**, and that is only safe if the caller's slot is then given the live value. On the normal path it is: `CallNode` copies each `var` operand's slot value back into `arguments[index]` after the body returns, and `CleanupOperand` pokes it into the caller's slot. **On the error path it is not** - the write-back loop sits after `Operator.Block.BlockNode.Execute(program)` on the success path only, so when the body raises, `arguments[index]` is still the original and `CleanupOperand` pokes that back.

Measured today: an operator that assigns `Binary('0004')` to its `var Binary` parameter and then raises, with the caller catching the error, leaks one stream and leaves the caller's variable reading back as `'0001'` - the value it had before the call. With the disposal added and nothing else changed, that same script would leave the caller's variable pointing at a stream the callee deallocated: reading it is a read of freed state, and leaving its scope deallocates it a second time. **A leak would have been turned into a use-after-free, which is worse.** Do not land the disposal without this.

**Recommended fix: move the `var` write-back so that it runs on every exit path** - wrap the body execution in `try ... finally` and copy each `var` operand's slot value out in the `finally`. The index arithmetic already there (`program.Stack.Count - stackDepth + ...`) is written to tolerate a stack left deeper than it started, so it should not need changing; satisfy yourself of that rather than assuming it.

That changes a D4-visible behaviour on an error path: **after an operator raises having assigned to a `var` parameter, the caller's variable holds the assigned value, where today it holds the value it had before the call.** This ticket is the explicit authorisation for that change, for these reasons:

- It is the only option that is uniform - no copying of operands, no per-call allocation, and no type-dependent rule.
- Today's behaviour is not a property of the language, only of where that loop happens to sit, and it is not even consistent: a `var` *table* parameter is handed to the callee live (there is a `NOTE:` saying so on `ParameterNode.InternalBindingTraversal`), so a callee's changes to one of those already survive a raise.
- "The caller sees the callee's last assignment" is what a by-reference parameter means everywhere else.

It needs an entry in `docs/migration-notes.md` in the same commit.

**Rejected, with reasons** - record whichever of these you need to at the site, as a `NOTE:`, if you depart from the recommendation:

- *Give the callee's `var` slot its own copy* (`Push(DataValue.CopyValue(...))`), so the body's assignments never touch the caller's value. This preserves today's error-path behaviour exactly, but `CopyValue` deep-copies a row and copies a whole table, and `var` row parameters are ordinary - change and validate handlers take them. It also still needs `CallNode` to dispose the callee's slot value when the body raises, or the leak simply moves.
- *Mark the alias at compile time* - a flag on the `Symbol` struct (`src/engine/DAE/Compiling/Symbols.cs`) saying "this slot is a `var` operand of the enclosing operator", so `AssignmentNode` can skip the disposal for it. `Symbol` is a public struct that a custom C# extension could be using, and this still needs the failure-path disposal in `CallNode`, so it is strictly more work for a narrower result.

## What else a fix has to get right

- **Aliasing.** Disposing the old value before storing the new one is only safe if the two are never the same value. They are not, today, on any path that reaches an assignment: `ServerStreamManager.Reference` (`src/engine/DAE/Streams/ServerStreamManager.cs`, around line 335) allocates a **new** stream identifier for every copy, so `DataValue.CopyValue` of a stream-backed value never yields the identifier it was given; and no operator in `src/engine/DAE/Schema/SystemCatalog.d4` returns a `Binary` or a `Graphic` from a `Binary` or `Graphic` argument, so no built-in can hand an assignment back the identifier the slot already holds. That is an argument about today's catalog, not a proof over every node, so guard it as well as state it: dispose only when the displaced value is not the value being stored. **`StreamID` is a struct** (`src/engine/DAE/Streams/StreamManager.cs`, line 16), so `Object.ReferenceEquals` alone is the wrong test - two boxes can carry the same identifier. Compare identifiers when both values are `StreamID`s, and fall back to reference equality otherwise. A wrong call here is a read of a deallocated stream, which is worse than the leak.
- **Everything, not just streams.** `DataValue.DisposeValue` also releases rows, lists and tables held in a slot, and the row measurement above shows those leak too. Do not narrow the fix to stream-backed scalars.
- **It is on every assignment in the language**, so it is a hot path. The extra work for a native value - an integer, a string - is the same-value test plus the type tests inside `DisposeValue`, which already falls through for anything it does not recognise. Say what that costs rather than assuming it is nothing; `PerfBaselineTest` and `tests/Dataphor.Benchmarks` are the instruments if it needs measuring.
- **Make the next site impossible, not just correct.** Three places overwrite a stack slot (`AssignmentNode`, `CleanupOperand`'s `Modifier.Var` branch, `VariableNode`) and none disposes; two others dispose a slot's value without overwriting it (`DeallocateVariableNode`, `CleanupOperand`'s `Modifier.In` branch). Nothing couples the two halves, which is why one site drifted from the rule unnoticed. One helper that does both - `PokeOwned(int offset, object value, IValueManager manager)` on `Alphora.Dataphor.DAE.Runtime.Stack` (`src/engine/DAE/Runtime/Stack.cs`), which is the non-generic `Stack<object>` the engine actually runs on and the only one of the two `Stack` classes that can see a value manager - means a fourth site cannot forget. This is the preferred shape; if it is rejected, record why at the site with a `NOTE:`. The two sites that keep a plain `Poke` each get a `NOTE:` saying why they are not using it.
- **The debugger is not a reader of freed values.** Checked, because the fix opens a window in which the caller's slot holds a disposed value while the callee runs, and `AllowExtraWindowAccess` lets the debugger read outer windows: `src/engine/DAE/Debug/Debugger.cs` around line 554 renders a slot with `GetType().FullName` and `ToString()`, and a boxed `StreamID`'s `ToString` prints the identifier without opening the stream. Nothing to do here; recorded so the question does not have to be re-opened.
- **A runtime tripwire is available, but not yet.** A `DEBUG`-only check at program teardown that the server's stream count came back to its value on entry would catch this whole class the moment it reappears. Do not add it in this ticket: the sibling leak below is still open, and such a check would fire on it. It belongs after both have landed - leave a `NOTE:` at the teardown site if you want it remembered, do not file a ticket for it.

## The sibling leak this ticket does not fix

Two rows of the table above are marked "not this ticket": a block that raises leaks the values of the `Binary` variables it declared, whether or not a D4 `try ... except` catches the error. That is a different site - `FrameNode.InternalExecute` (`ImperativeNodes.cs`, around line 123) pops the frame in a `finally`, and `Stack.PopFrame` nulls the slots without disposing, while the `DeallocateVariableNode` statements that would have disposed them are statements in the block the error skipped. It is filed as `bug-an-error-inside-a-block-leaks-its-binary-values`, which waits on this one because it should be built on the ownership rule this ticket writes down. Do not fix it here, and do not write a test here that depends on it being fixed.

## Expected behaviour

After this ticket, in a session with nothing else running: `Diagnostics.StreamCount()` returns to its earlier value after any number of assignments to a `Binary` or `Graphic` variable, after any number of assignments to a row variable with a `Binary` column, and after any number of calls to an operator that assigns to a `var` parameter of one of those types - including calls that raise. Reading a variable back after assigning to it still gives the value that was assigned. A variable whose value an operator assigned before raising reads back as the assigned value.

## Tests

One new fixture on `InProcessTestFixture`, in `tests/DAE.Server.Tests`. Read the count with `Process.Evaluate("StreamCount()", null).AsNative` - `Diagnostics.StreamCount()` resolves unqualified - and assert on exact numbers, because the assertion is arithmetic. Prefer the loop form for the first case: N assignments leaving N orphans fails loudly rather than by one. Cases, one behaviour each:

- N assignments to a `Binary` variable leave the count where it started. This is the reproduction; it reports 100 orphans today.
- N assignments to a `row { B : Binary }` variable leave the count where it started. Reports 10 today.
- An operator with a `var Binary` parameter that assigns to it: the count comes back, and the caller's variable reads back as the assigned value. This is the case that would catch a later change making a `var` parameter receive a copy.
- An operator with a `var Binary` parameter that assigns to it and then raises, with the caller catching the error: the count comes back, **and** the caller's variable reads back as the assigned value. This case is the record of the behaviour decision above, so say in a comment that the value asserted is the new behaviour and what the old one was.

`tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists its sources explicitly in a `<Compile Include>` item group - a new file that is not added there compiles into nothing and NUnit discovers no tests, with no error.

`debt-streams-coverage` covers the same ground more broadly from a D4 coverage script and names this ticket as a prerequisite, because its "the count comes back to where it started" assertions cannot pass while this leak exists. Do not write that script here.

## Compatibility

Nothing an application can see changes except the engine's memory use and the error-path behaviour of `var` parameters set out above. Both the new public member on `Stack` and that behaviour change need an entry in `docs/migration-notes.md` in the same commit.

## TODO

- Add `PokeOwned(int offset, object value, IValueManager manager)` to `Alphora.Dataphor.DAE.Runtime.Stack` (`src/engine/DAE/Runtime/Stack.cs`): store the new value, then dispose what the slot held unless it is the same value. Same-value test: equal `StreamID`s when both are `StreamID`s, `Object.ReferenceEquals` otherwise.
- Point `AssignmentNode.InternalExecute` at it.
- Leave `VariableNode.InternalExecute` on a plain `Poke`, with a `NOTE:` saying the slot it pokes into was just pushed as `null`.
- Move `CallNode.InternalExecute`'s `var` operand write-back loop so that it runs when the body raises as well as when it returns, and satisfy yourself that the existing index arithmetic holds on that path.
- Leave `CleanupOperand`'s `Modifier.Var` branch on a plain `Poke`, with a `NOTE:` saying the write-back is an ownership return and why disposing there would double-free.
- Add the new fixture with the four cases above, and its `<Compile Include>` entry in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`.
- Measure the cost of the extra work on a native assignment, with `PerfBaselineTest` or `tests/Dataphor.Benchmarks`, and put the number in the handoff. If it is material, say so rather than absorbing it.
- Add the `docs/migration-notes.md` entries for the new `Stack` member and the `var` parameter error-path behaviour.
- `dotnet build Dataphor.slnx`, then the new fixture by name plus the existing fixtures over the files touched - `ServerStreamManagerTest`, `ConveyorTest`, `TableAssignmentEvaluationTest`, `BufferedOperatorColumnOwnershipTest`, `DebuggerSessionTest` - with `--filter`. Name the filters in the handoff.
