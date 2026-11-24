description: Passing a picture or document value into a routine, or reading one out of a record, made the engine clean it up at the wrong moment - either never, so the memory was held for the rest of the session, or twice, so a later read failed with an error. The piece of the plan that produced the value now says who owns it, instead of the engine guessing from how the routine declared its parameter.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/PlanNode.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Runtime.Instructions/ExtractionNodes.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/ListNodes.cs, src/engine/DAE/Runtime/Stack.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, docs/architecture/value-lifetime.md, docs/architecture.md, docs/migration-notes.md
repro: verified
----
## What landed

The engine had no way to say who owns the value handed to a call for one of its arguments. `InstructionNodeBase.CleanupOperand` decided from the **operand's modifier** alone - `in` release, `var` write back, `const` do nothing - but ownership is a property of the **plan node that produced the argument**: a node that reads a stack slot, or a column of a row in one, can hand over the value it already holds, where a node that computes one (a literal, a conversion, another call) produces a value nothing else holds.

Two new public virtuals on `PlanNode` carry that fact:

- `bool ReturnsByReference` - whether `Execute` returns a value belonging to something else.
- `bool SetByReference()` - asks the node to lend rather than copy, and reports whether it will.

The const branch of `InstructionNodeBase.DetermineDataType` asks, once, at compile time (replacing two type tests); `CleanupOperand`'s new `Modifier.Const` branch reads the answer back off the same node at run time and releases the value unless the node lent it. Four nodes override: `StackReferenceNode` and `StackColumnReferenceNode` over their existing `ByReference` fields, `ExtractColumnNode` over a new private flag set only where its source row outlives the call, and `IndexerNode` over its existing field with `ReturnsByReference` only.

`ExtractColumnNode` - what `LR.B` compiles to - now **copies** the column except where a const operand asked it not to. It used to hand over the row's own value unasked, with no way to say so.

The invariant the whole change rests on: **a node lends only when it was asked, and only the `const` branch asks.** That is what lets `CleanupOperand`'s `in` branch go on releasing unconditionally, and what closes the two arms where a row's column was released out from under the row.

### The three failures this closes

- **A `const` operand's computed value was never released.** `F(Binary('0001'))` held one stream for the life of the session, every call. This reaches far more code than the word `const` suggests: the compiler promotes a by-value operand the body never assigns to to `const` (`Compiler.CompileOperatorBlock`), and every comparison, conversion, selector and property accessor the engine generates for a scalar type declares `const` outright.
- **A by-value operand given a record's column destroyed the record's copy.** `G(LR.B)` where `G`'s body assigns to its parameter left `LR.B.Value` failing with `StreamID (n) not found`.
- **Reading a record's column into anything that takes ownership failed.** `var LB : Binary := LR.B;` inside a block raised at the end of the block; `LB := LR.B;` and `row { LR.B B }` failed the same way. Entirely ordinary D4 - no operator, no modifier, just reading a field.

### Tests

Four cases in `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`: `AConstOperandReleasesTheValueComputedForIt` (a function and a procedure), `AConstOperandGivenAVariableLeavesTheCallersValueAlone` (the control for the by-reference path, which nothing covered before), `AByValueOperandGivenARowsColumnLeavesTheRowsColumnAlone`, and `ARowsColumnReadIntoSomethingThatOwnsItStaysReadable` (three shapes: a new variable, an existing variable, a row selector). Each asserts a stream count and a value read back. One new private helper, `AssertShapeLeavesNoStreams`, names the shape in both assertions and throws from neither.

### Also in this change

- **New architecture page `docs/architecture/value-lifetime.md`** (1,274 words), with its line in the index list in `docs/architecture.md`. Nothing under `docs/architecture/` owned value lifetime, and `sessions-processes-plans.md` is within fifty words of the 1,500-word cap. The page says who owns a value in a stack slot, in an operand of each modifier, in a row and in a list, what releases each, and how a plan node declares that it is lending rather than giving. `Stack.PokeOwned` and `InstructionNodeBase.CleanupOperand` both point at it, and its own "Where to look" table points back at them.
- **`docs/migration-notes.md`** gained one entry under *Custom C# extensions*: the const release, the two new public virtuals, and `ExtractColumnNode` now copying.
- No `tests/DAE.Server.Tests/coverage-manifest.json` entry needed flipping, and nothing in the manifest names this ticket.

## Review findings

The implement-stage diff (`ticket(implement): bug-an-operator-argument-is-released-by-the-wrong-owner`) was read before the handoff summary. The handoff's own invitation - "a reviewer who wants to attack one thing should attack this list" - was taken up: the by-reference-site list was re-derived from the code rather than read off the handoff.

### Checked, nothing wrong

- **The load-bearing invariant, re-derived.** Every site that sets `ByReference` outside the const branch, found by grep rather than from the handoff: `Compiler.cs` 1191 and 12825 (the generated equality of a `case` expression), 2915 and 3086 (update and delete targets, which are not instruction operands at all), 10644 (an event handler's arguments, whose signature is only ever `Var` or `Const`); `InNodes.cs` 52, 129, 138, 145 and `TableNode.cs` 838 and 840 (all generated equalities); `ExtractionNodes.cs` 186 (`ExtractColumnNode` marking its own source). Each reaches a `const` or `var` operand or is not an operand, so nothing reaches the unconditional `in` release. Also confirmed that neither `StackReferenceNode` nor `StackColumnReferenceNode` has a subclass, so the two type tests the change replaced and the new virtual call cover exactly the same set of nodes - a silent widening was the other way this could have gone wrong.
- **The new const release cannot free a value a consumer still names.** The nodes that build a value *out of* an operand were read: `Row`'s indexer setter copies on store (`DataValue.CopyNative`, `RowValue.cs`), so `CompoundScalarSelectorNode` and `CompoundScalarWriteAccessorNode` build their result independently of the operand they were given; `ScalarSelectorNode`, `ScalarReadAccessorNode`, `ScalarWriteAccessorNode`, their validating variants and `CompoundScalarReadAccessorNode` all copy explicitly; `IndexerNode` copies unless `ByReference`, which nothing sets. These are the generated operators the handoff says nearly every D4 expression reaches, and they are the ones a wrong const release would have corrupted first.
- **Instructions that hand an argument straight back.** `SystemNameReadAccessorNode` (`CatalogNodes.cs`) and `VersionNumberMinNode`/`VersionNumberMaxNode` return an argument value unchanged. All three deal in plain .NET values for which `DataValue.DisposeValue` is a no-op, so the new release cannot reach them.
- **`ExtractColumnNode`'s two flags cannot get out of step across a plan-cache clone**, which the handoff asked to be checked. `_shouldDisposeSource` starts `true` and is only ever assigned `false`; `InternalClone` carries both; `SetByReference` gates on it. No order of determine-then-clone can leave `_byReference` true over a source the node disposes.
- **Nested column reads were fixed as a side effect.** For `LR.A.B` the outer node's source is not a `StackReferenceNode`, so `_shouldDisposeSource` stays true and the outer node disposes what the inner one gave it. The inner node now gives it a copy; before the change it gave the caller's nested row's own value, which the outer node then destroyed. Cost is one intermediate row copy per nested read.
- **A bare `LR.B` as a statement result was fixed too.** `ServerExpressionPlan.Evaluate` hands the value to its caller, who disposes it, so before the change that disposal reached the row's own column.
- **Performance of the extra copies.** Both coverage-script halves ran inside their usual spread (3 m 34 s and 3 m 10 s here against the implementer's 4 m 37 s and 3 m 33 s and the root `AGENTS.md`'s recorded range), so the copies `ExtractColumnNode` now makes are not measurably expensive at suite scale. No tripwire recorded: there is nothing conditional left to watch.
- **Documentation.** Every file the change touches was read, plus `docs/architecture.md`'s index and the files the new page cites (`Symbols.IsBorrowed`, `Stack.PokeOwned`/`ReleaseAbove`, `DataValue.CopyValue`/`DisposeValue`, `src/engine/DAE/Streams/`); all exist and say what the page says they say. `python tools/docs-size-check/check_size.py`: 12 files checked, 0 over the limit.
- **Tests as code under review.** Nothing cut: three of the four new cases reproduce an arm of the bug and the fourth is the control for the path that must keep working, which nothing covered before. None restates the implementation or verifies a mock. `AssertShapeLeavesNoStreams` overlaps `AssertScriptLeavesNoStreams` by two lines; left, because the non-throwing form is what makes one shape's failure inside an `Assert.Multiple` not hide the shapes after it.

### Fixed in this pass

- **A comment in the same file the change edits asserted something the change made false.** `CallNode.ReferencesAnEarlierOperandsSlot`'s remarks said "InstructionNodeBase.CleanupOperand does nothing for const" and built its reasoning on it. The conclusion still holds - a collision is only ever detected on a by-reference `StackReferenceNode`, which now answers `ReturnsByReference`, so the const branch still leaves that operand's value alone - but the reason given was wrong. Reworded to the reason that is now true.
- **Nine new D4 error messages escaped the apostrophe twice.** `variable''''s` renders as `variable''s`, because the D4 lexer (`src/engine/DAE/Language/Lexer.cs`) reads `''` inside a `'...'` literal as one apostrophe and C# does nothing to apostrophes in a verbatim string. The fixture's own existing messages two lines above use `''`. Corrected to `''` in all nine.
- **`CleanupOperand` had no pointer to the new architecture page**, although the page's own "Where to look" table points at it and the implementer added exactly that back-link to `Stack.PokeOwned` for the same reason. Added, as an XML doc comment carrying the tripwire below.

My own edits broke source hygiene once and it was repaired: an `Edit` through the harness rewrote `ImperativeNodes.cs`'s eight LF-only lines as CRLF. Restored byte-for-byte and re-verified (8 LF-only lines, 2,143 CRLF, byte-order mark kept); the final `git diff` of that file is the five-for-three-line comment change and nothing else.

### Filed as a ticket

**`tickets/fix/4.5-bug-an-argument-is-orphaned-when-a-later-one-raises`** - `repro: verified`. The four execute shapes in `InstructionNode.cs` evaluate their arguments **before** entering the `try` whose `finally` runs `CleanupOperand`, so when evaluating a later argument raises, an earlier argument's value is never cleaned up at all. Measured with a throwaway case on the ownership fixture (added, run, removed): `ZProbeTwoConst(Binary('0001'), ZProbeRaises())` raises and leaves the server holding one stream more than before, every time.

This is older than this ticket and was not introduced by it - before this change a `const` argument's value was never released on any path, so the error path was no worse than the normal one, and the four prologue lines are untouched by the diff. It is filed now because closing the normal path makes this the only shape in which a call still orphans an argument. It is not a tripwire: it is wrong every time the path runs, and the path is ordinary D4. The fix is not two lines - `CleanupOperand`'s `var` branch would write a null back over the caller's variable if it ran when the call never happened - which is why it is a ticket rather than something fixed in this pass. A `NOTE:` on `CleanupOperand` names it, to be removed when it lands.

### Considered and declined

- **The duplicated by-reference members.** `ByReference` and the now-identical `ReturnsByReference`/`SetByReference` pair sit verbatim on both `StackReferenceNode` and `StackColumnReferenceNode`, which share the base `VarReferenceNode`. Hoisting them needs the public field to move, which changes the declaring type of a public member that custom C# nodes use and owes a `docs/migration-notes.md` entry of its own. Eight duplicated lines do not pay for that.
- **`SetByReference()`'s return value has no consumer.** `DetermineDataType` discards it and `CleanupOperand` reads `ReturnsByReference` instead, so one question has two answers. Left as it is: the signature is already published in `docs/migration-notes.md` as the extension contract, and the return is the part a custom node needs when it wants to know whether its own ask succeeded.
- **Comment density in `CleanupOperand`.** Three branches carry about thirty lines of comment over eight lines of code, and much of it is restated in the new page. Left: they state constraints and consequences rather than narrating the statements, which is this file's established style, and the page back-link added above gives a reader who wants the fuller statement one hop.
- **`F(var LB, const LB)` still leaks the copy `CallNode` makes.** One variable passed to a var and a const operand gives the later operand a copy that nothing releases, because that operand's node answers `ReturnsByReference`. The `NOTE:` on `ReferencesAnEarlierOperandsSlot` already records this as an accepted tradeoff - "a call that always loses one of its two writes, so today it is a programming error either way" - and its revisit condition, a real application passing one variable to two by-reference parameters, has not tripped.

### Gaps the handoff declared, and what became of them

- **The fix ticket's full stream-delta table is still not re-measured.** The three arms and the control are pinned by the four new cases, the generated const operators they depend on are exercised by the whole coverage corpus, and that corpus is green; the individual deltas recorded on the fix ticket's throwaway fixture remain unconfirmed and are not worth a second throwaway fixture.
- **Part 1 of the three-part split was not run**, here or at implement: it was measured at 11 m 13 s on this machine, past the ten-minute limit the runner kills a command at, and the project's rules make the full suite CI's job. In its place, 481 tests from the fixtures a value-ownership or instruction-node regression would surface in were run (below).
- **`IndexerNode.ReturnsByReference` is dead today** and the `NOTE:` at the site says so, pointing at `bug-a-list-value-never-releases-its-elements`. Confirmed: nothing assigns `IndexerNode.ByReference`.
- **One list leak is not closed** - a list of binaries read through its indexer goes from 3 orphaned streams to 2 - and belongs to `bug-a-list-value-never-releases-its-elements`, which is on the board in `fix/`.

## Verification

`dotnet build Dataphor.slnx` - 0 errors, 143 warnings, all pre-existing.

- **Ownership fixtures** (the implement stage's targeted set): `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest"` - 21 passed, 0 failed, 3 s.
- **One ring wider**, the neighbouring fixtures a value-ownership or instruction-node regression would show up in: the same filter plus `ServerStreamManagerTest`, `StreamBackedRepresentationWriteTest`, `ConveyorTest`, `CursorTest`, `TableAssignmentEvaluationTest`, `InternalCoverageTests`, `ApplicationTransactionTest`, `D4EmitterRoundTripTest` and `ParserRoundTripCorpusTest` - **481 passed, 0 failed, 19 skipped, 23 s**.
- **Both coverage-script halves**, because the change is in the path every compiled operator call takes: part A-R **72 passed / 11 skipped in 3 m 34 s**, part S-Z **50 passed / 12 skipped in 3 m 10 s**. Both green, matching the implement stage's counts exactly.
- `python tools/docs-size-check/check_size.py` - 12 files checked, 0 over the limit.
- The leak finding above was confirmed by a temporary case on the ownership fixture, which failed with the expected one-stream delta and was then removed; the fixture is back to its four new cases and the file carries no probe.
- No pre-existing failures surfaced, so no `tickets/.pre-existing-error.md` was written.
