description: Passing a picture or document value into a routine, or reading one out of a record, made the engine clean it up at the wrong moment - either never, so the memory was held for the rest of the session, or twice, so a later read failed with an error. The fix lets the piece of the plan that produced the value say who owns it instead of guessing from how the routine declared its parameter; review the fix and the tests that now pin it.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/PlanNode.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Runtime.Instructions/ExtractionNodes.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/ListNodes.cs, src/engine/DAE/Runtime/Stack.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, docs/architecture/value-lifetime.md, docs/architecture.md, docs/migration-notes.md
difficulty: hard
----
## What landed

The engine had no way to say who owns the value handed to a call for one of its arguments. `InstructionNodeBase.CleanupOperand` decided from the **operand's modifier** alone - `in` release, `var` write back, `const` do nothing - but ownership is a property of the **plan node that produced the argument**: a node that reads a stack slot, or a column of a row in one, can hand over the value it already holds, where a node that computes one (a literal, a conversion, another call) produces a value nothing else holds.

Two new public virtuals on `PlanNode` carry that fact:

- `bool ReturnsByReference` - whether `Execute` returns a value belonging to something else.
- `bool SetByReference()` - asks the node to lend rather than copy, and reports whether it will.

The const branch of `InstructionNodeBase.DetermineDataType` asks, once, at compile time (replacing two type tests); `CleanupOperand`'s new `Modifier.Const` branch reads the answer back off the same node at run time and releases the value unless the node lent it. Four nodes override: `StackReferenceNode` and `StackColumnReferenceNode` over their existing `ByReference` fields, `ExtractColumnNode` over a new private flag set only where its source row outlives the call, and `IndexerNode` over its existing field with `ReturnsByReference` only (no `SetByReference`, with a `NOTE:` saying why).

`ExtractColumnNode` - what `LR.B` compiles to - now **copies** the column except where a const operand asked it not to. It used to hand over the row's own value unasked, with no way to say so.

**The invariant the whole change rests on, and the thing most worth attacking in review:** a node lends only when it was asked, and only the `const` branch asks. That is what lets `CleanupOperand`'s `in` branch go on releasing unconditionally, and what closes the two arms where a row's column was released out from under the row.

## The three failures this closes

All three were verified before the fix and are now covered by tests that were watched failing (see *How it was validated*):

- **A `const` operand's computed value was never released.** `F(Binary('0001'))` held one stream for the life of the session, every call. This reaches far more code than the word `const` suggests: the compiler promotes a by-value operand the body never assigns to to `const` (`Compiler.CompileOperatorBlock`), and every comparison, conversion, selector and property accessor the engine generates for a scalar type declares `const` outright.
- **A by-value operand given a record's column destroyed the record's copy.** `G(LR.B)` where `G`'s body assigns to its parameter left `LR.B.Value` failing with `StreamID (n) not found`.
- **Reading a record's column into anything that takes ownership failed.** `var LB : Binary := LR.B;` inside a block raised at the end of the block; `LB := LR.B;` and `row { LR.B B }` failed the same way. Entirely ordinary D4 - no operator, no modifier, just reading a field.

## How to exercise it by hand

Each of these is ordinary D4 against any in-process or hosted server; `Diagnostics.StreamCount()` resolves unqualified and reports how many streams the server holds.

```
create operator F(const AB : Binary) : Integer begin result := Length(AB.Value); end;
F(Binary('0001'));                  -- StreamCount() must be unchanged across this

create operator G(AB : Binary) begin AB := Binary('0002'); end;
var LR : row { B : Binary } := row { Binary('0001') B };
G(LR.B);
LR.B.Value;                         -- must still read '0001', not raise

begin
    var LR2 : row { B : Binary } := row { Binary('0001') B };
    var LB : Binary := LR2.B;       -- must leave both readable, and the block must exit clean
end;
```

## Tests added

All five are in `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`, the fixture that already owns this ground; its class remarks gained a paragraph naming the operand half of the ownership rule, and `AnOperatorThatAssignsToAnInParameterReleasesTheValueItReplaces`'s summary was corrected (it claimed the `in` branch was the only shape in which operand cleanup releases anything, which the new const branch makes untrue).

| Test | What it pins |
|---|---|
| `AConstOperandReleasesTheValueComputedForIt` | A const operand given a computed value leaves the stream count where it found it, for a **function** and for a **procedure** - two forms in one `Assert.Multiple`, because the by-value-to-const promotion read the wrong operand's symbol for every operator with a result until `bug-assigning-a-binary-variable-leaks-its-stream`, so a regression could reach one and not the other |
| `AConstOperandGivenAVariableLeavesTheCallersValueAlone` | The **control**: the by-reference path that must keep working. Nothing covered it before. Asserts the call still sees the value *and* the caller's variable is still readable after 100 calls |
| `AByValueOperandGivenARowsColumnLeavesTheRowsColumnAlone` | Arm 2: the call completes and the row's column still reads back its original value |
| `ARowsColumnReadIntoSomethingThatOwnsItStaysReadable` | Arm 3, three shapes in one `Assert.Multiple`: into a new variable, over an existing variable, into a row selector. Each asserts both the copy and the row's own column afterwards |

Shape and loop counts follow the fixture's existing conventions (`AssignmentCount` = 100, `RowAssignmentCount` = 10, a value assertion beside every count assertion). One new private helper, `AssertShapeLeavesNoStreams`, names the shape in both assertions and throws from neither, so one shape's failure inside an `Assert.Multiple` does not hide the shapes after it.

No test for the `ExtractColumnNode` table branch or for `IndexerNode.ReturnsByReference`: both are unreachable today and the ticket said so explicitly. A `NOTE:` sits at each site.

## How it was validated

`dotnet build Dataphor.slnx` - 0 errors, 143 warnings, all pre-existing.

- **The new tests were watched failing.** The two behavioural halves of the fix were temporarily reversed (`CleanupOperand`'s const release disabled, `ExtractColumnNode` made to lend unasked) and the fixture re-run: `AByValueOperandGivenARowsColumnLeavesTheRowsColumnAlone`, `AConstOperandReleasesTheValueComputedForIt` and `ARowsColumnReadIntoSomethingThatOwnsItStaysReadable` **failed**, `AConstOperandGivenAVariableLeavesTheCallersValueAlone` (the control) **passed** - which is exactly what a control should do. The files were restored from byte copies and rebuilt.
- **Ownership fixtures**: `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest"` - 21 passed, 0 failed, ~5 s.
- **Both coverage-script halves**, the check that matters here because every D4 script calls generated `const` operators constantly. The two commands under *Agents run the suite in parts* in the root `AGENTS.md`: part A-R **72 passed / 11 skipped in 4 m 37 s**, part S-Z **50 passed / 12 skipped in 3 m 33 s**. Both green, and the counts match what the fix ticket measured against its prototype exactly. **No `coverage-manifest.json` entry needs flipping**, and nothing in the manifest names this ticket.
- Part 1 of the three-part split (everything except the coverage scripts) was **not run**: the fix ticket measured it at 11 m 13 s on this machine against the same prototype, past the agent shell's ten-minute command limit, and recorded it green (915 passed / 0 failed / 33 skipped). CI runs it.
- `python tools/docs-size-check/check_size.py` - 12 files checked, 0 over the limit. The new page is 1,274 words.
- No pre-existing failures surfaced, so no `tickets/.pre-existing-error.md` was written.

## Known gaps, honestly

- **The invariant is held by inspection, not by the type system.** `StackReferenceNode.ByReference` is a public field, and sites outside the const branch still set it directly. Each was checked and none reaches an `in` operand: the update and delete targets in `Compiler.cs` are not instruction operands at all; `ExtractColumnNode` sets it for its own source; and the by-reference nodes built for a generated equality (`Compiler.cs`, `InNodes.cs`, `TableNode.cs`) and for an event handler's arguments (`Compiler.cs`) reach `const` or `var` operands only - `iEqual` declares `const` in `SystemCatalog.d4`, and an event signature is only ever `Var` or `Const` (`Compiler.GetTableVarEventSignature` and its siblings). Recorded as a tripwire `NOTE:` at the `Modifier.In` branch, naming the remedy if a new site ever lands there. **A reviewer who wants to attack one thing should attack this list.**
- **The `Modifier.Var` branch still type-tests and sets the field directly** rather than calling `SetByReference()`, as the ticket directed: the type test there is a compiler *check* (it raises `ConstantObjectCannotBePassedByReference`), not an inference. The cost is that one flag now has two ways of being set.
- **The ticket's full stream-delta table was not re-measured.** The three arms and the control are pinned by the new tests, and the whole coverage suite is green, but the individual deltas the fix ticket recorded for the compound scalar selector and for reading and writing a compound scalar's property (`delta 1 → 0`, `delta 2 → 0`, `delta 3 → 0`) were taken on its own throwaway fixture and are not independently reconfirmed here.
- **`IndexerNode.ReturnsByReference` is dead today** - nothing sets `ByReference`, so it always answers false. Deliberate, per the ticket; the `NOTE:` at the site says so and points at `bug-a-list-value-never-releases-its-elements`.
- **One list leak is explicitly not closed.** A list of binaries read through its indexer goes from 3 orphaned streams to 2 with this change (the index expression's own leak closes); the remaining 2 belong to `bug-a-list-value-never-releases-its-elements`.
- `AnOperatorThatAssignsToAnInParameterReleasesTheValueItReplaces` and the fixture's class remarks were re-worded, not re-tested: the behaviour they describe is unchanged, only the claim about being the *only* releasing shape was wrong.

## Also in this change

- **New architecture page `docs/architecture/value-lifetime.md`**, with its line added to the index list in `docs/architecture.md`. Nothing under `docs/architecture/` owned value lifetime - no page mentioned the stack, streams or who releases a value - and `sessions-processes-plans.md` is within fifty words of the 1,500-word cap. The page says who owns a value in a stack slot, in an operand of each modifier, in a row and in a list, what releases each, and how a plan node declares that it is lending rather than giving. A `<para>` on `Stack.PokeOwned`'s remarks now points at it, so a reader arriving from either end finds the two in agreement; `PokeOwned`'s own list of non-replacing `Poke` sites was already right and is unchanged.
- **`docs/migration-notes.md`** gained one entry in *Custom C# extensions*: the const release, the two new public virtuals, and `ExtractColumnNode` now copying. Both are things a custom C# node could notice.

## Watch for, while reviewing

- `tickets/fix/4-bug-a-list-value-never-releases-its-elements` names `src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs` and `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` in its `files:`. Different regions of each: this change touched `StackReferenceNode` (~line 1865) and `StackColumnReferenceNode` (~line 1998) in `ImperativeNodes.cs`, where that ticket is about `ListValue` and `ForEachNode`; and it appended four cases to the test fixture rather than editing existing ones. `tickets/implement/4-debt-streams-coverage` lists this ticket as a prerequisite and is now unblocked by it.
- `ExtractColumnNode.InternalClone` carries the new flag beside `_shouldDisposeSource`; worth checking that the pair cannot get out of step across a plan-cache clone.
- Source hygiene: every edited file keeps its indentation (tabs), CRLF line endings and byte-order-mark state; `ImperativeNodes.cs` still has the same eight LF-only lines it had at `HEAD`, none of them touched. The new markdown page is CRLF with no BOM, like its siblings.
