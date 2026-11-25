description: Comparing two rows whose columns are of different but convertible number types destroyed one of the two rows, so the next thing that read it failed; the comparison now asks for the row it borrows after it rewrites its own arguments, instead of before.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs, src/engine/DAE/Runtime.Instructions/RowNodes.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, docs/architecture/operand-ownership.md
difficulty: medium
----

## What changed

`RowEqualNode.DetermineDataType` and `CompoundScalarEqualNode.DetermineDataType` (`src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs`) each called `base.DetermineDataType(plan)` first and then rewrote their own argument nodes. The base call's const branch is the one place in the engine that asks an argument node to lend the value it already holds rather than copy it, and the rewrite below it can replace either argument node with an implicit conversion - so the permission to lend was granted to a node that was no longer the operand, and ended up underneath a consumer (`RowExtendNode`, reached from `Compiler.ConvertNode` through `EmitRedefineNode`) that disposes the row it is handed. Comparing two rows whose columns differ by a convertible type therefore destroyed the row in one of the two variables, and the next read of that variable failed.

Both overrides now start with `DetermineModifiers(plan)` and end with `base.DetermineDataType(plan)` followed by the `_dataType` assignment, so the question reaches whichever node is actually in the operand slot. Nothing else moved: the ask still happens (a variable's row is still lent rather than copied) and `InternalExecute` still releases each argument whose node answers `ReturnsByReference` false, raise path included - both halves of `bug-row-equality-leaks-the-rows-it-compares` are intact.

The alternative - making `RowExtendNode` honour `Nodes[0].ReturnsByReference` - was **not** taken: it also makes the script pass but leaves the grant on the wrong node, so the next rewrite that interposes a consumer has the same bug. It is recorded as a tripwire `NOTE:` on `sourceRow.Dispose()` in `RowExtendNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/RowNodes.cs`), which says why that unconditional release is right today and where to fix such a case instead.

One factual correction to the fix ticket, worth knowing while reading the code comments: the ticket's narration said the destroyed variable was the side the compiler left unconverted. It is the other way round - the conversion is built over the very node that was asked to lend (`EmitRedefineNode` passes the existing `sourceNode` object straight into `EmitExtendNode`), so the destroyed row is the one on the side the conversion wraps. That also matches the ticket's own observation that `ALeftRow` (the `Long` side, which is what `FindConversionPath` converts first) was the one that broke. The comments say the corrected version.

## Use cases to exercise

- Two row variables whose single column differs by a convertible scalar type, compared in both directions, then each read again - the shape in `tests/libraries/TestFramework.Coverage.Scripts/Documents/ImplicitConversions.d4` (`row { ID : Long }` against `row { ID : Byte }`). Both comparisons must answer equal and both variables must still be readable afterwards.
- The same with a stream-backed column alongside the converted one (`row { X : Binary, N : Long }` against `row { X : Binary, N : Byte }`), which is what the new test does: the conversion rewrites only `N`, `X` rides through the extend/project/rename chain, and the server's stream count must be where it started.
- Row comparison with no conversion at all (equal column types, and a generic `row` operand) - the paths the previous ticket's cases cover, which must not have regressed.
- Compound scalar equality - the same two moves were made in `CompoundScalarEqualNode`. See the gap below: no test reaches its conversion branch.

## Tests

One test added, in the fixture the row-equality rule already lives in:

- `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`, `RowEqualityWithAnImplicitConversionLeavesTheCallersRowsAlone` - two row variables of `{ X : Binary, N : Long }` and `{ X : Binary, N : Byte }`, compared both ways round ten times, then both variables read. Verifies the ownership behaviour (neither variable's row is destroyed) and, through `AssertScriptLeavesNoStreams`, that the comparison still leaks nothing. The `Binary` column is what gives the stream count something to measure; a row of number columns has no stream to lose.

No `.d4` script was added and `ImplicitConversions.d4` was not touched - it already covered the behaviour; what was missing was a case in the fixture the implement and review stages of the previous ticket actually ran.

**This test is a verified reproduction, not an assertion of present behaviour**: the fix was temporarily reverted (base call moved back to the top of `RowEqualNode.DetermineDataType`), rebuilt, and both `RowEqualityWithAnImplicitConversionLeavesTheCallersRowsAlone` and `ImplicitConversions` failed with the same `Could not determine the current location for program <guid>` error; the fix was then restored and rebuilt.

Nothing was added to the fixture's class summary: its `NOTE:` says the file is past the size at which it should be split and that the summary should stop growing, so the new case documents itself in its own doc comment.

## Verification run

`dotnet build Dataphor.slnx` - 0 errors (and `-c Release`, 0 errors).

Debug, after the final edit:

- `dotnet test tests/DAE.Server.Tests --filter "Name=ImplicitConversions|FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~OperandOwnershipShapeTest|FullyQualifiedName~ContainerValueOwnershipTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest"` - 42 passed, 0 failed (41 before this ticket, plus the new case).
- `dotnet test tests/DAE.Server.Tests --filter "Name=ImplicitConversions|Name=Conversions|Name=RowLibrary|Name=RowAllocation|Name=RowInsert|Name=RowParameterColumnOrder|Name=Types.CompoundEquality|Name=Types|Name=TestSargabilityWithConversion|Name=Operators|Name=EntryLibrary|Name=TableAndRowValuedAttributes|Name=StreamValues|Name=StreamAllocation|Name=ScalarAllocation|Name=Domains|Name=LibraryTypes"` - 14 passed, 0 failed, 3 skipped (`Domains`, `EntryLibrary`, `StreamAllocation`, which the coverage manifest already excludes).

Release:

- `dotnet test tests/DAE.Server.Tests -c Release --filter "Name=ImplicitConversions|Name=RowEqualityWithAnImplicitConversionLeavesTheCallersRowsAlone|Name=RowEqualityGivenVariablesLeavesTheCallersRowsAlone|Name=RowEqualityReleasesTheRowsItCompares|Name=CompoundScalarEqualityReleasesTheValuesItCompares"` - 5 passed, 0 failed.

`python tools/docs-size-check/check_size.py` - 13 files checked, 0 over the limit.

No pre-existing failures were met, so `tickets/.pre-existing-error.md` was not written.

## Documentation

- `docs/architecture/operand-ownership.md`, in "How a node says it is lending": one sentence saying the question reaches whichever node is in the operand slot when it is asked, so a node that rewrites its own arguments must ask after the rewrite. An equivalent amount was trimmed from the now-historical narration of `bug-row-equality-leaks-the-rows-it-compares` in the same section. The page is **1,498 words against the 1,500-word cap**, so it has two words of headroom and the next ticket that adds to it has to trim or split it (the plan's WS8 rule). No other page changed.
- No `docs/migration-notes.md` entry, deliberately: nothing an application, a deployment script or a C# extension can see changes. The comparisons answer as they always did - this restores the pre-`e321e388` answer for the conversion case - and the entry that ticket added still reads true.

## Known gaps for the reviewer

- **`CompoundScalarEqualNode`'s conversion branch has no test.** The two moves there are symmetric with `RowEqualNode`'s and are reasoned, not measured: reaching that branch needs two *different* compound scalar types with a conversion path between them, and the existing coverage (`Types.CompoundEquality.d4`, and `CompoundScalarEqualityReleasesTheValuesItCompares`) only ever compares one compound type with itself. Whether a D4 script can declare such a pair, and whether the same destroy-the-operand failure is reachable there, was not established. If it is reachable it wants its own case; if it is not, the branch is dead and that is worth knowing instead.
- **The masking error message is untouched, by design.** The real failure is a `NullReferenceException` in `ExtractColumnNode.InternalExecute` reading a disposed row, which `PlanNode.Execute` turns into a readable message and then throws away, because `program.GetLocation` dereferences a null `Locator` on the window `RowEqualNode.InternalCompare` pushed. That is `backlog/bug-a-runtime-error-inside-a-row-or-table-comparison-loses-its-message`; the fix ticket said not to fix it here, and no error message is improved by this change. A reviewer who breaks the fix to check the test will see the unhelpful `Could not determine the current location` message rather than a nil-value message.
- **Table comparison was not examined.** `ImplicitConversions.d4` has the same case over tables immediately after the row case and it passes, so the table comparison nodes appear not to have this shape, but that is taken from the green test rather than read in the code.
- **The two filter sets above are the ticket's, not a full suite.** This change is in the compiler's plan building, which every coverage script goes through, so a regression outside those filters is conceivable; CI's full Debug and Release runs are the backstop.

## TODO

- Review the two `DetermineDataType` overrides for anything else that depends on the order that moved - in particular that `_dataType` after `base` is right, and that nothing between `DetermineModifiers` and `base` reads a characteristic `base` would have set.
- Decide the `CompoundScalarEqualNode` gap above: a case, or a finding that the branch is unreachable.
- Confirm the tripwire `NOTE:` in `RowExtendNode.InternalExecute` says enough for someone meeting it cold.
