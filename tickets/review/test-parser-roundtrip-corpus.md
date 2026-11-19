description: Review the new L0 parser/emitter round-trip corpus test, its comparator, and the AllowList that quarantines files the current parser/emitter cannot round-trip so follow-up tickets can drain it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, AGENTS.md
----
## What landed

`tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs` adds an L0 property test with one case per `.d4` in the repository. Each case: read the file as UTF-8, parse with `Parser.ParseScript`, emit the parse tree through `D4TextEmitter`, re-parse the emitted text, and assert the two trees are structurally equal through a reflection-based comparator.

- **Discovery.** Walks the directory holding `Dataphor.slnx` (found from `AppContext.BaseDirectory`). Skips folder names `artifacts`, `obj`, `bin`, `.git`, `node_modules`, `external`, and the relative path `tickets/.logs`. Finds 387 `.d4` files today (the ticket's "658" count was from before the repo move / `legacy/` sweep; today the tree is 310 non-legacy + 77 under `legacy/`).
- **Case naming.** Each case's name is the file's repository-relative path with forward slashes, so a failure in TRX/console output points directly at the file.
- **Comparator.** `TreesEqual` walks fields reflectively from the `Statement` base. It:
  - Excludes the source-position backing field `_lineInfo` on `Statement` and the public `Line`/`LinePos`/`EndLine`/`EndLinePos` fields on `MetaData` and `LineInfo`.
  - Treats `null` as equivalent to an empty collection (any `IList`, or any object exposing `Count` — in practice, `D4.Tags`). Needed because `MetaData.Tags` is a lazy getter that materialises an empty `Tags` on first access, so the pre-emit tree ends up with `Tags { Count == 0 }` where the post-emit tree still has `null`.
  - Compares `Tags` through its indexer (count + per-entry `Tag` equality on `==`, `IsStatic`, `IsInherited`) rather than by reflection. The backing `Tag[]` doubles on growth, so two tags with identical content can have different array lengths and would spuriously fail a reflective compare.
  - Filters `EmptyStatement` and empty `Block` (non-`DelimitedBlock`, `Statements.Count == 0`) from statement lists. The parser returns an empty `Block` for a stray `;` (`Parser.Statement()` at `Keywords.StatementTerminator`); the emitter terminates nested blocks with `;` and the enclosing block appends another, which re-parses as "one real statement followed by one empty block". That is tree-form trailing whitespace, not a real difference.
- **Two smoke tests** (`TreesEqual_HandCrafted_EqualTrees`, `TreesEqual_HandCrafted_DifferentTrees`) assert the comparator itself against hand-crafted D4; they catch a regression in the helper independently of the 387-file corpus.
- **AllowList.** A dictionary of `relative-path → one-line category` (`parse` | `emitter-malformed` | `emitter-reshape`) that the fixture skips during discovery. See "Known defects below the bar" for the categories; each entry is a defect to drain, not an "intentionally not parseable" case.

Also wired: `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists the new file; `AGENTS.md` names the new test in the "Build and test" section (one paragraph above the coverage-scripts paragraph).

## How to verify

```
dotnet build tests/DAE.Server.Tests/DAE.Server.Tests.csproj -c Debug
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter FullyQualifiedName~ParserRoundTripCorpusTest --logger "console;verbosity=normal"
```

On this machine the 292 corpus cases plus the 2 smokes take about 2 seconds. Expected: all 294 cases pass. To see a specific case or group, add e.g. `--filter "Name=...."` or `-- "NUnit.Where=test =~ 'DebugTest'"`.

The test is picked up by the normal split commands under "Agents run the suite in parts" in `AGENTS.md`: the first command (`class != CoverageScripts`) runs it.

## Known defects below the bar (the AllowList)

The fixture quarantines 97 files in three categories. Nothing in the parser or emitter was touched inside this ticket; these are the follow-ups.

- **`parse` (38 files).** The parser produces `_messages` entries on the first read, so the file is effectively not parseable by today's grammar. Most are in `legacy/libraries/FHIR.Server/Documents/` and other parked libraries (`PHINVADS.Test`, `TestCQL`, `TestCache`, `OracleDevice`, `SASDevice`), but the active tree also has: `libraries/MSSQLDevice/Source/SystemCatalog.d4`, `libraries/PGSQLDevice/Source/SystemCatalog.d4`, `src/engine/DAE/Schema/SQLDeviceMappings.d4` (SQL-device integer literals that use a device prefix the parser rejects), several `samples/Sample.Shipping/` and `samples/Sample.Common/` documents, and about a dozen scripts under `tests/libraries/TestFramework.Coverage.Scripts/Documents/` (`Domains`, `EntryLibrary`, `ForeignSuperKeysAndConstantReferences`, `IStringLibrary`, `LibraryDomains`, `PresentationLibrary`, `Serialization`, `SerializationDomains`). Each needs its own look — some are genuine grammar gaps, some may be a lexer bug, some may be files that were already broken.
- **`emitter-malformed` (8 files).** The first parse succeeds but the emitter produces text the parser cannot read (`Syntax error near "{"` and so on). Files: `legacy/.../FHIR.LoadConformanceCache.d4`, `legacy/.../ModelInfo.Operators.d4`, `libraries/System.Internet/Documents/Dynamic.d4`, `tests/.../IntegerLibrary.d4`, `tests/.../Lexer.d4`, `tests/.../SecurityLibrary.d4`, `tests/.../SerializationOperators.d4`, `tests/.../ViewUpdatability.d4`. These are the clearest evidence of emitter defects to drain first — the test writes the emitted text as a TRX attachment, so each one has a reproduction ready.
- **`emitter-reshape` (51 files).** Both parses succeed but the two trees differ in shape. Observed sub-patterns on first read (not exhaustive):
  - *Multi-variable statement flattening.* `var a : X, b : Y` parses as a `Block { VariableStatement, VariableStatement }`; the emitter emits each child on its own with its own `;`, so the re-parse produces two separate `VariableStatement` siblings in the enclosing block instead of one nested `Block`. Hits every library-coverage script with multi-var decls (`BooleanLibrary`, `ByteLibrary`, `Conversions`, `DecimalLibrary`, `GuidLibrary`, `LongLibrary`, `ShortLibrary`, `StringLibrary`, …).
  - *`try … commit` desugaring.* The emitter rewrites `try X; commit;` into `begin BeginTransaction(); try X; CommitTransaction(); except begin RollbackTransaction(); raise end end`. The output is a different tree. Hits `PlanCache`, `EventHandlers`, `TransitionConstraints`, `DeferredConstraint{,sWithKeys}Checking`, and the `Sample.Shipping` schema/coverage documents.
  - *`hasByClause` toggled.* `root.…expression.hasByClause: value differs (False vs True)` — emitter adds a `by` clause the source didn't carry. Hits `libraries/System.Integration/Documents/Register.d4` and `tests/.../SystemCatalog.d4`.
  - *Metadata tag ordering.* `metaData.tags[0]: Tag differs (…=… static=True vs …=… static=False)` — the emitter reorders static and non-static tags, so the first-tag mismatch is actually an index shift. Hits `src/engine/DAE/Schema/{DataTypes,SystemCatalog}.d4`, `samples/Sample.IssueTracker/.../Register.d4`, `samples/Sample.Shipping/.../Schema.DataTypes.d4`, `tests/.../SerializationTypes.d4`.

The right next step is one follow-up ticket per sub-pattern (not one per file): fix the emitter at that site, drop every file in that sub-pattern from the AllowList, confirm. That keeps the change localised and the test's value as a regression net. Note that `src/engine/DAE/Schema/{DataTypes,SystemCatalog,SQLDeviceMappings}.d4` are *real* catalog scripts that run at server bootstrap — these are the highest-priority entries to drain because they ship.

## Review targets

Short list for the reviewer; the code's own comments carry the rest.

- **Comparator excludes are defensible.** The three special cases — null-vs-empty-collection, `Tags` via indexer, `EmptyStatement`/empty-`Block` filter in statement lists — each have a code comment with the reason. If any of them silently masks a *real* round-trip bug that the ticket's spec expected the test to catch, that is a review finding.
- **AllowList categorisation.** Spot-check two files per category by removing them from the AllowList and re-running the case: does the failure match the category? If a `parse` entry turns out to be a round-trip issue (or vice versa), the follow-up ticket lands on the wrong defect class.
- **Discovery root.** `FindRepositoryRoot` walks up from `AppContext.BaseDirectory` looking for `Dataphor.slnx`. CI runs from the repo root and the layout dir is under `artifacts/`, so this works; worth a sanity check that it survives a `dotnet test` run from an unusual CWD.
- **`System.Collections.Generic.Stack<string>` fully qualified.** The file uses `using Alphora.Dataphor.DAE.Language.D4;`, which brings `Alphora.Dataphor.DAE.Stack<T>` into scope through the test's `Alphora.Dataphor.DAE.Server.Tests` namespace. Without the qualification the test discovery throws `IndexOutOfRangeException` inside the engine's `Stack`.
- **MetaData `Line`/`LinePos` are public fields, not `_lineInfo`.** The comparator excludes them by name, not by declaring type, so the same exclusion covers `LineInfo`. Confirm the exclusion list in `GetComparableFields` catches every source-position field in the AST (grep for `public int Line` in `Language/` and `Language.D4/` — the current set is the three classes above).

## Review findings

- *AllowList is a working set, not a passing baseline.* 97 files quarantined is a lot; the review should decide whether to keep it at this granularity (one entry per file) or collapse to one entry per sub-pattern with a path predicate. Current choice is per-file because the sub-patterns were inferred from a single run, not from code inspection, and a per-file list makes draining precise.
- *Allowed tradeoff candidate.* `TreesEqual` filters `EmptyStatement` and empty `Block` from statement lists. That is the "trailing whitespace" the ticket spec allows, and a comment at the filter says so — but a reviewer may reasonably decide that an empty `Block` is semantically distinct from a stray `;` and the emitter's `;;` output should be fixed at its layer instead. If so, the filter goes and about 30 `emitter-reshape` entries become `emitter-malformed` or similar.
- *Known gap.* `D4TextEmitter` has an `EmitMode` field (`ForCopy` is the default). This test only exercises `ForCopy`. Other modes (`ForStorage`, `ForRemote` if present) could round-trip differently; a follow-up should either characterise them or document why the default alone is the right scope.
- *Pre-existing failures.* None observed from this ticket's test runs. The only test executions here were `--filter FullyQualifiedName~ParserRoundTripCorpusTest`; no other `DAE.Server.Tests` cases were exercised.
