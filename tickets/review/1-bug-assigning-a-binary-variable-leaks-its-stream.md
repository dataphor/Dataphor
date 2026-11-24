description: Storing a new picture or document into a value the engine was already holding used to throw the old contents away without releasing the memory behind them; it now releases them, so a routine that does this in a loop no longer makes the server grow until it is restarted.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime/Stack.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Compiling/Symbols.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime/Program.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
difficulty: hard
----
## What landed

An assignment now releases the value it replaces. `Diagnostics.StreamCount()` comes back to where it started after any number of assignments to a `Binary`, `Graphic` or row variable, and after calls to an operator that assigns to a parameter - including calls that raise.

The shape is the one the implement ticket asked for, with three additions it did not anticipate. All three are in the fixture.

**The mechanism.** New `Alphora.Dataphor.DAE.Runtime.Stack.PokeOwned(int offset, object value, IValueManager manager)` stores a value in a slot and releases what it displaced, releasing nothing when the slot already held the value being stored (stream identifiers compared by value, because `StreamID` is a struct and two boxes can carry one identifier; everything else by reference). Its remarks are where the ownership rule for stack slots is written down. `AssignmentNode.InternalExecute` calls it.

**The `var` parameter write-back moved into a `finally`.** A `var` parameter's slot is an alias of the caller's slot, so the body's assignment releases a value the caller's slot still points at, and only the write-back makes that safe. It used to run on the success path only, so a body that assigned and then raised left the caller pointing at a released value. It now runs on every exit path. That changes one D4-visible behaviour, which the implement ticket authorised: **after an operator raises having assigned to a `var` parameter, the caller's variable holds the assigned value**, where it used to hold the value it had before the call. `docs/migration-notes.md` records it.

**Addition 1 - the write-back is now unconditional, not just for `var` operands.** The implement ticket missed this and it is not optional. An `in` parameter's slot holds a copy the callee owns; a body that assigns to one releases that copy, and `InstructionNodeBase.CleanupOperand`'s `In` branch then released `arguments[index]`, which was that same copy - a second release of one stream, which the stream manager raises an error for. Writing every operand's final slot value back keeps `arguments[index]` naming the value the callee's slot owns, which is what both cleanup branches need. This is ordinary code: `create operator F(AB : Binary) begin AB := ...; end` is a normal thing to write. Note also that the compiler promotes an unassigned `in` operand to `const` when it compiles the body, so an operand that is still `in` at run time is one the body *does* assign to - the only shape in which that cleanup branch releases anything at all.

**Addition 2 - one variable passed twice by reference.** `F(var LB, var LB)` compiles and ran today, leaking two streams. With the release in place its first assignment released the shared value and its second released it again, raising `StreamID (n) not found`. `CallNode.InternalExecute` now gives a repeat of a by-reference operand its own copy, so each callee slot owns a distinct value. Observable behaviour is unchanged (the last write-back wins, as before); one of the two leaks is gone and the other remains, because such a call always loses one of its two writes and the lost one is not reachable to release. The detection is a type test per operand with no comparison at all for single-operand calls.

**Addition 3 - the `foreach` loop variable.** This is the one that changes the implement ticket's framing. The ticket says "the value in a stack slot is owned by that slot - that is already the engine's rule". **It is not the rule for a `foreach` loop variable:** the loop lends that slot the one row value it reads each row into and goes on reading into it, so releasing it left the next iteration selecting into a disposed row - measured as `A nil was encountered where a value was expected`. `Symbol` therefore gains a read-only `IsBorrowed` and `Symbols` a `SetIsBorrowed(int offset)`; both `foreach` compile sites mark the loop variable, and `AssignmentNode` keeps a plain `Poke` for a borrowed target. Both marker sites were checked to be load-bearing by disabling each and watching the fixture fail. The mark is never cleared, deliberately: a `foreach` over a pre-existing variable borrows that variable's slot for the loop and, in the list form, leaves the last element in it, so an assignment after the loop is no safer than one inside it.

A `foreach` over a **list** turned out not to be at risk: a list operand is copied into the loop, so the elements it lends belong to that copy. Measured, not assumed.

## Where to look hardest

- **`CallNode.InternalExecute`'s new `finally`.** The index arithmetic is the implement ticket's, unchanged, and it reads `program.Stack.Count` as it is at that moment, so a body that left the stack deeper than it started still finds its own operands. What is new is that it runs while an exception is unwinding: if that indexing ever threw (in a Debug build the stack indexer bounds-checks), it would replace the original exception. Nothing pops the operands before the window pops above this frame, and the whole suite is green in Debug including the exception-heavy scripts, but this is the line to argue with.
- **`CleanupOperand`'s `Modifier.Var` branch still uses a plain `Poke`**, with a `NOTE:` saying why: the write-back is an ownership return, not a replacement, so releasing there would double-free. That reasoning holds for a D4 operator body (`CallNode`) and for the C# instruction nodes with `var` operands that were checked - the cursor `Default`, `Change` and `Validate` nodes mutate the row in place and never replace `arguments[index]`. A C# node that *replaced* a `var` operand's value would leak the caller's old one at that site. None was found; the search was over `Modifier.Var` signature elements, not exhaustive.
- **`Symbol` is a public struct** and now has one more field. The implement ticket weighed a `Symbol` flag and rejected it - for a different purpose (skipping the release for `var` operands, which would have defeated the fix). For borrowed slots it is the only mechanism that keeps today's behaviour; the alternative was copying a row per iteration of every `foreach`. `docs/migration-notes.md` carries the entry, including that an extension compiled against an earlier 4.0 build must be rebuilt.
- **The implement ticket's "three places overwrite a stack slot" is wrong** - there are about seventeen `Stack.Poke` call sites. The others are iteration slots in the `foreach`, list and aggregate nodes, plus unnamed row-context slots in `InNodes` and `TableNode`, and they deliberately borrow. None of the unnamed ones is reachable from an assignment: `AssignmentNode`'s target is always a `StackReferenceNode` resolved from an identifier, and those slots have no name. That enumeration is the argument that the borrowed-slot surface is exactly the `foreach` loop variable - worth re-checking rather than taking on trust.

## Known gaps

- **The sibling leak is untouched**, as the implement ticket directed: a block left by a raise still leaks the values of the `Binary` variables it declared, through the frame pop rather than through assignment. `bug-an-error-inside-a-block-leaks-its-binary-values` owns it. The fixture is written not to depend on it (no script leaves its declaring block by a raise).
- **Assigning to a `foreach` loop variable still leaks** what it displaces, as every assignment did before this change. Closing that would mean the loop lending a copy per iteration. Recorded as a `NOTE:` at the `AssignmentNode` site, not as a ticket.
- **`F(var LB, var LB)` still leaks one stream** - the write that the call loses. Recorded in the fixture's own remarks.
- **No architecture page was touched.** The ownership rule has one code site (`Stack.PokeOwned`) and lives in its remarks, which the `docs/architecture/` rule reserves the pages for concerns that have no single site. Argue with that if you disagree; nothing under `docs/architecture/` currently mentions the stack or value ownership at all.
- **A `DEBUG`-only stream-count check at program teardown** would catch this whole class of defect the moment it reappeared. Not added, because the sibling leak above would make it fire; left as a `NOTE:` on `Program.Stop` saying so and naming the condition for adding it.

## Use cases to exercise

A reviewer wanting to see the behaviour rather than read about it, on any session:

- `begin var LB : Binary := Binary('0001'); for LI : Integer := 1 to 100 do LB := Binary('0002'); end;` then `Diagnostics.StreamCount()` - unchanged across it, 100 higher before this change.
- The same over `row { B : Binary }`, and over a `Graphic`.
- `create operator F(var AB : Binary) begin AB := Binary('0003'); raise Error('x'); end;` called inside a `try ... except`: the caller's variable now reads back as `'0003'`, the changed behaviour.
- `create operator G(AB : Binary) begin AB := Binary('0005'); end;` called once - the `in` case; before Addition 1 above it raised `StreamID not found`.
- `foreach var LRow in cursor(T) do LRow := row { ... };` over a two-row table - the loop must see both rows.

## Tests

One new fixture, `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`, on `InProcessTestFixture`, with its `<Compile Include>` entry in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`. Each case and what it pins, with what it reported before the fix:

| Test | Verifies | Before |
|---|---|---|
| `AssigningABinaryVariableReleasesTheValueItReplaces` | 100 assignments to a `Binary` variable leave the stream count where it started | 100 orphans |
| `AssigningARowVariableReleasesTheRowItReplaces` | 10 assignments to a `row { B : Binary }` variable leave the count where it started | 10 orphans |
| `AnOperatorThatAssignsToAVarParameterReleasesTheValueItReplaces` | the count comes back and the caller sees the assignment; would catch a later change giving the callee a copy | 1 orphan |
| `AnOperatorThatRaisesAfterAssigningToAVarParameterReleasesTheValueItReplaces` | the count comes back on the error path **and** the caller sees the assignment - the record of the behaviour decision, said so in the test's own comment | 1 orphan, and the caller saw the pre-call value |
| `AnOperatorThatAssignsToAnInParameterReleasesTheValueItReplaces` | an `in` parameter's copy is released exactly once (Addition 1) | 1 orphan; without Addition 1, `StreamID not found` |
| `TheSameVariablePassedTwiceByReferenceStillRuns` | the call completes and the last write-back reaches the caller (Addition 2) | ran; without Addition 2, `StreamID not found` |
| `AssigningToTheVariableOfAForEachOverACursorLeavesTheLoopIntact` | the loop sees every row, for both forms of loop variable (Addition 3) | ran; without Addition 3, `A nil was encountered where a value was expected` |

The three regression cases were each confirmed load-bearing by disabling their guard, rebuilding and watching them fail. A `foreach` over a list is deliberately not covered, for the measured reason above. No existing D4 script's expected values were touched.

## Cost of the extra work

**Not separable from run-to-run noise.** A tight D4 loop of 4,000,000 integer assignments, Release, in-process, three timed runs per pass and three passes each way, discarding each pass's first run as warm-up: plain `Poke` 422-503 ms (mean 454 ms), `PokeOwned` 415-529 ms (mean 457 ms). The means differ by 3 ms, about 0.7%, against a spread of ±50 ms within either arm. Per assignment that is roughly 110 ns either way, with the added work bounded by the spread at well under 25 ns. The extra work on a native value is the same-value test plus the type tests inside `DataValue.DisposeValue`, which falls through for anything it does not recognise. The timing harness was a throwaway test in the new fixture, removed again; `tests/Dataphor.Benchmarks` measures engine start and a DDL/DML mix, neither of which isolates an assignment.

## Build and tests run

`dotnet build Dataphor.slnx` - 0 errors, no new warnings (143 before and after).

Because the change touches the compiler and the node every assignment and every operator call goes through, the whole suite was run rather than a targeted set, in Debug, in the three parts the root `AGENTS.md` gives:

- Part 1 (everything but the coverage scripts): 891 passed, 0 failed, 34 skipped, 10 m 7 s.
- Part 2 (coverage scripts A-R): 72 passed, 0 failed, 11 skipped, 3 m 58 s.
- Part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 26 s.

The 23 skips across parts 2 and 3 are the manifest's known failures and are unchanged; no manifest entry was flipped. `StreamAllocation` is among them, failing on `Object "Coverage" not found` under `test-pre-3-1-scripts-policy` - an unrelated missing helper, not this leak.

Before that, the ticket's named targeted set: `--filter "FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest|FullyQualifiedName~TableAssignmentEvaluationTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest|FullyQualifiedName~DebuggerSessionTest"` - 48 passed, 0 failed, 1 skipped. And the coverage scripts over the areas the diff touches: `--filter "Name=LanguageConstructs|Name=Operators|Name=SpecificAssignment|Name=ScalarAllocation|Name=RowAllocation|Name=StreamAllocation|Name=LockAllocation|Name=AggregateOperators|Name=Aggregates|Name=ListLibrary|Name=RowLibrary|Name=TableLibrary|Name=EventHandlers|Name=Exceptions|Name=DynamicEvaluation|Name=TableCallsWithStackReferences|Name=Cursors|Name=CursorLibrary|Name=Functions|Name=Types"` - 18 passed, 2 skipped (both known failures).

No pre-existing failure surfaced, so nothing was written to `tickets/.pre-existing-error.md`.

## Compatibility

Nothing an application can see changes except the engine's memory use and the `var` parameter error path above. Two `docs/migration-notes.md` entries landed in this change: one under *D4, catalog and libraries* for the behaviour, one under *Custom C# extensions* for `Stack.PokeOwned`, `Symbol.IsBorrowed` and `Symbols.SetIsBorrowed`. No D4 operator signature, class name, assembly name, file format, error code, WCF contract or port changed. The engine allocates no identifier differently, so no catalog rebuild is implied.

## Still open elsewhere

`debt-streams-coverage` names this ticket as a prerequisite and covers the same ground from a D4 coverage script; its "the count comes back to where it started" assertions could not pass while this leak existed. Nothing was written toward it here.
