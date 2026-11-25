description: Dataphor's query language has an operator whose engine code existed twice over - the version that actually runs, and an older version the engine could no longer reach but still offered to applications. The unreachable version is now deleted, along with the catalog entries and the database translation that pointed at it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/RowNodes.cs, src/engine/DAE/Language.D4/Instructions.cs, src/engine/DAE/Schema/SystemCatalog.d4, src/engine/DAE/DAE.csproj, libraries/SQLDevice/Source/SQLOperators.cs, libraries/MSSQLDevice/Source/SystemCatalog.d4, libraries/PGSQLDevice/Source/SystemCatalog.d4, tests/DAE.Server.Tests/OperandOwnershipShapeTest.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/Cursors.d4, docs/architecture/operand-ownership.md, docs/coverage-report.md, docs/migration-notes.md
difficulty: medium
----

## What landed

The node form of the D4 `redefine` operator is gone. `Compiler.EmitRedefineNode` rewrites `T redefine { A := A + A }` into `T add { A + A Temp } remove { A } rename { Temp A }`, and has done for the whole life of this repository; the three classes implementing the alternative — `RedefineNode`, `RowRedefineNode`, `RedefineTable` — were reachable only from a branch behind `#if USEREDEFINENODE`, a symbol defined nowhere, whose text would not have compiled anyway because it still used the `A`-prefixed parameter names this repository renamed away. The owner authorized retiring them on 2026-11-22, which is the explicit decision the compatibility rule requires before a registered D4 class name is withdrawn.

Everything in the ticket's site list was done, in one change because it cannot be split: the engine's two `create operator iRedefine(...)` declarations and their two `grant usage` lines, the two SQL devices' `System.iRedefine(table)` mappings, and `SQLDevice.SQLRedefine` (which casts to `RedefineNode`) all fall together — `CompileDeviceOperatorMap` resolves a mapping's operator against the catalog before anything else, so a mapping left behind would fail the device's whole operator-map script and stop SQL Server and PostgreSQL devices from registering.

**Measured removal: 775 lines of source.** `RedefineNode.cs` 396 (whole file), `RedefineTable.cs` 118 (whole file), `RowRedefineNode` in `RowNodes.cs` 192, `SQLRedefine` in `SQLOperators.cs` 56, the dead conditional in `Compiler.cs` 13 — counted from `git diff --stat HEAD`, which totals 796 deletions against 15 insertions once the eleven single-line removals and the documentation edits are included. Of those lines, 446 are the coverable lines the 2026-11-21 instrumented run measured at exactly zero hits.

## What the verification observed

The ticket asked for this before anything was deleted, because the migration note rests on it. A scratch fixture over `InProcessTestFixture` evaluated both operators by name on a real in-process engine (added, run, removed — nothing checked in):

| expression | before the change | after the change |
|---|---|---|
| `iRedefine(table { row { 1 A } })` | `CompilerException: Internal error: Unexpected exception encountered during compilation.` → `NullReferenceException` | `CompilerException: Unable to resolve operator reference "iRedefine" with signature "(table{A:System.Integer})".` |
| `iRedefine(row { 1 A })` | the same pair | the same, with signature `(row{A:System.Integer})` |
| `table { row { 1 A } } redefine { A := A + A }` | evaluated | evaluated |

So retiring these operators removed nothing that worked: the only code that set the field their `DetermineDataType` reads first was the disabled branch, and a direct call died at compile time on that null. The migration note says exactly this, and quotes the post-change message verbatim because the second run confirmed its wording.

## Tests

**No new test, and none is owed.** `redefine` is the behaviour that must not change, and four coverage scripts already execute it — which is how the dead code was found in the first place. Nothing new became testable: after the change the operators do not exist, so a script naming them gets a compile error. `OperandOwnershipShapeTest` is the structural guard and fails if the deletion is partial.

Filters run, after `dotnet build Dataphor.slnx` (Debug) and `dotnet build Dataphor.slnx -c Release`, both 0 errors:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~OperandOwnershipShapeTest|FullyQualifiedName~ParserRoundTripCorpusTest|FullyQualifiedName~D4EmitterRoundTripTest|Name=Exists|Name=RowLibrary|Name=Cursors|Name=DML" --logger "console;verbosity=normal"
```

430 tests, 411 passed, 19 skipped, 0 failed; coverage scripts `pass: 4 passed, 0 failed`, `known-failure: 0 matched, 0 mismatched, 0 unexpectedly passing`. The 19 skips are `ParserRoundTripCorpusTest`'s parked documents, which it reports and skips by design. What each part of that set proves:

- `Exists`, `RowLibrary`, `Cursors`, `DML` — `redefine` still compiles and runs through the rewrite. These are stronger than a parse check on the engine catalog: each builds a real in-process server whose catalog is created from `src/engine/DAE/Schema/SystemCatalog.d4`, so the two removed declarations and two removed grants were **executed**, not merely parsed. A grant left naming a removed operator, or a renumbering problem, would have failed the catalog build.
- `OperandOwnershipShapeTest.TheNodesOutsideOperandCleanupAreTheOnesReviewedForIt` — its `stale` assertion is what fails on a partial deletion; it passes, so `ReviewedNodes` matches the engine assembly exactly.
- `ParserRoundTripCorpusTest` — all three edited `.d4` catalogs parse and round-trip, including both device files.
- `D4EmitterRoundTripTest` — unaffected, as expected: it works on parsed text, and `RowRedefineNode.EmitStatement` was the only code that emitted a `RedefineExpression` back out of a plan and was never reached.

## Known gaps, flagged honestly

- **The two device mapping edits are verified by inspection only.** No test in the suite executes either device's `SystemCatalog.d4`: `SQLDevice.InternalRegister` runs that script only when the device starts, which needs a reachable DBMS — the same reason the three `Transaction.Atomicity.TestMSSQL*` scripts are recorded as known failures. `ParserRoundTripCorpusTest` catches a syntax slip but not a semantic one. Both edited regions were read back: the removed line sat between `System.iAdorn(table)` and `System.iExtend(table)` in each file, and the `iAdorn` line still ends in a comma, so the comma-separated list is well formed. A reviewer who wants more than inspection needs a SQL Server instance; that is outside what a ticket can run.
- **PGSQLDevice does not build** (its C# project is not converted yet), so its catalog edit is only parsed, not compiled against anything.
- **Catalog object identifiers shift for a store created by this build.** `SystemCatalog.d4` runs on a store's first run only, and identifiers are handed out in script order, so removing two `create operator` lines renumbers every system object declared after them. Checked and nothing pins one: no catalog store file is committed, `tests/goldens/` holds no operator identifiers, and `ObjectIDGeneratorTest` asserts only that the generator starts at one. A store created before this change keeps its own numbers and is loaded rather than re-scripted — and still holds rows naming the removed class names, which is in the migration note as a rebuild-from-scripts case.

## Edits beyond the ticket's literal site list

Two, both forced by the ticket's own instruction to drop `"RowRedefineNode"` from `ReviewedNodes`, which made hand-written counts wrong. The shape test's failure message itself says to remove a stale node "from ReviewedNodes **and from** `docs/architecture/operand-ownership.md`", and the counts are part of that page.

- `docs/architecture/operand-ownership.md` line 30: thirteen operator nodes → twelve, a list of eighteen → seventeen, "the other ten re-override" → nine. The reviewed list is now 5 argument shapes + 12 operator nodes, of which 3 derive from `InstructionNodeBase` directly, leaving 9 that re-override from a shape. Line 34's clause became the row extend node alone, as the ticket asked. `python tools/docs-size-check/check_size.py`: 13 files checked, 0 over the limit.
- `tests/DAE.Server.Tests/OperandOwnershipShapeTest.cs` — the same three counts in its doc comment and remarks ("the thirteen after them", "The ten that re-override", "as eighteen namespace-qualified strings"). Comment text only; no assertion changed.

Worth a reviewer's eye: these counts are hand-maintained prose about a reflected set, and this is the second ticket to have to correct them. That is a tripwire, not a defect — see below.

## Tripwires parked

- `src/engine/DAE/Compiling/Compiler.cs`, immediately above `EmitRedefineNode` (the two-argument overload): a `NOTE:` saying the rewrite is now the only implementation, that a node form existed and was retired here, and that the rewrite spends three plan nodes where one would do — so if `redefine` plans ever show up as slow in a profile, a node form is the known alternative and this is where it went. Deliberately no magnitude: nothing was measured, and the owner's decision says none is needed.
- Not parked anywhere, offered to the reviewer as a judgment call rather than filed: the counts in `docs/architecture/operand-ownership.md` and in `OperandOwnershipShapeTest`'s comments are prose restatements of a set the fixture already reflects, and they go stale every time the set changes. Conditional, so not a ticket — but if a third ticket has to correct them, the answer is probably to stop writing the number down.

## What a reviewer should check

- The `Compiler.cs` diff is the only non-deletion: the live rewrite is untouched and is now the method's whole body, both overloads and `CompileRedefineExpression` are still there, and `ConvertTableNode` / `ConvertRowNode` (around lines 11873 and 11901) still call the first overload for implicit column conversions.
- `RowNodes.cs` reads correctly at the seam: `RowExtendNode`'s closing brace, the file's one-line gap, then the `// operator Rename(row) : row` comment. The surviving comment at line 329 still names `Compiler.EmitRedefineNode`, which still exists, so it stays accurate — as do the matching comments in `RowComparisonNodes.cs` and `SystemNodes.cs`.
- `grep -rn "iRedefine\|RedefineNode\|RedefineTable\|SQLRedefine\|USEREDEFINENODE" src libraries tests samples tools` returns only the tripwire `NOTE:`'s own mention of the retired names and the live `EmitRedefineNode` / `CompileRedefineExpression` call sites.
- `tests/libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCase.d4` was deliberately left alone: its row is named `System.iRedefine_table_` after the old device operator, but the script it holds uses only the `redefine` language operator, and that file is test data rather than a coverage script the suite runs.
- Source hygiene held: every edited file kept its line endings and byte-order-mark state (verified file by file against the checkout), and nothing was reformatted. Note for whoever edits these files next — `sed -i` strips CRLF from a whole file in this environment; it bit this ticket once on `DAE.Server.Tests.csproj` and was restored before anything else was touched, after which every edit went through byte-exact Python replacements. `git diff --check` flags the five added `NOTE:` lines as trailing whitespace; that is the repo-wide CRLF-blob artifact, not real whitespace (commit `e945ec78` shows 152 of the same flag), and the bytes were checked directly.

## Documentation

- `docs/migration-notes.md` — two `done` entries. Under **D4, catalog and libraries**: the two operators no longer exist, a rebuilt catalog does not have them, the SQL devices no longer translate `iRedefine(table)`, `redefine` is unchanged, and neither operator could be called successfully before the change, with what the verification observed. Under **Custom C# extensions**: the three classes, the three D4 class names and `Instructions.Redefine` are removed; `Instructions.Redefine` being a `const` means an already-compiled extension notices nothing until it is recompiled, and the engine no longer answers that an operator named `iRedefine` is a built-in; a pre-existing catalog store naming the removed classes is not converted and should be rebuilt from scripts.
- `docs/coverage-report.md` — the two retired files dropped from the "wholly untested" table (eleven rows of 20+ lines → nine), the counts above it corrected to nineteen files and 978 lines with a sentence saying those are the 2026-11-21 run's figures minus the two deleted files rather than a fresh measurement, and the finding paragraph rewritten in the past tense with what the verification observed. The `debt-unreachable-redefine-nodes` row in the review-tickets table is left as the record of where the finding came from, as the ticket asked. No coverage was re-collected; this is a text edit.
- `docs/architecture/operand-ownership.md` — as described above.
