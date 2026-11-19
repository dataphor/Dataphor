description: The L0 parser/emitter round-trip corpus test (`ParserRoundTripCorpusTest`) is landed, accepted, and the ninety-seven files its `AllowList` quarantines are tracked by three new backlog tickets.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, AGENTS.md
----
## What landed

The implement commit `ticket(implement): test-parser-roundtrip-corpus` adds `tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs`, wires it into the project, and names the new fixture in `AGENTS.md`'s build-and-test paragraph. It walks the repository for `.d4` files (387 found today) and runs one case per file: read as UTF-8, `Parser.ParseScript`, `D4TextEmitter.Emit`, re-parse, assert the two parse trees are structurally equal through a reflective comparator that elides source positions, treats a null AST node as equivalent to an empty collection, compares `Tags` through its indexer, and filters `EmptyStatement`/empty-`Block` from statement lists. Two hand-crafted smokes exercise the comparator independently of the corpus. A fixture-level `AllowList` quarantines 97 files that the current parser or emitter cannot round-trip, each with a one-line category (`parse`, `emitter-malformed`, `emitter-reshape`). On this machine the test runs 292 cases (290 corpus + 2 smokes) in about 3 seconds through `dotnet test ... --filter FullyQualifiedName~ParserRoundTripCorpusTest`.

The 97-entry AllowList is a deviation from the implement ticket's spec, which said "Expect this list to be empty"; the implementer surfaced three defect classes at the parser and emitter instead of filing per-file tickets. The review accepted the quarantine as the right pragmatic call and filed one backlog ticket per defect class (below).

## Review findings

**What was checked.**

- Read the 525-line implement diff end to end. Built `DAE.Server.Tests.csproj` clean (0 errors; 143 warnings, all pre-existing). Ran the fixture with `VSTEST_CONNECTION_TIMEOUT=180 dotnet test --filter FullyQualifiedName~ParserRoundTripCorpusTest`; 292 passes, no failures.
- **Comparator excludes.** Verified source-position fields reflectively: `Statement._lineInfo` is the backing field, `LineInfo` is short-circuited by type check, `MetaData.Line/LinePos/EndLine/EndLinePos` and `LanguageModifier.Line/LinePos` are public fields caught by the name-based exclusion in `GetComparableFields`. `ValueExpression.Token` is a `TokenType` enum (semantic — Integer vs Hex vs String literal) and is correctly compared, not elided. The ticket spec's "Token" exclusion mention was misleading.
- **Tags and null-vs-empty handling.** `Tags` extends `System.Object`, not `IEnumerable`, so the dedicated `Tags`-by-name branch in `IsEmptyCollection` is necessary. The null-vs-empty equivalence is driven by `MetaData.Tags` being a lazy getter that materialises an empty collection on first access during emit; without this, every case with `MetaData` would false-fail. Comment at the branch explains why.
- **EmptyStatement/empty-Block filter.** This is the "trailing whitespace in tree form" that the ticket spec explicitly allows; comment at the filter names the exact emitter behaviour (`;` after a nested block plus another `;` from the enclosing block). Accepted.
- **Discovery root.** `FindRepositoryRoot` walks up from `AppContext.BaseDirectory` looking for `Dataphor.slnx`. CI runs from the repo root; the compiled test DLL sits under `artifacts/bin/DAE.Server.Tests/debug/`, and the walk reaches the root in two steps. Robust to an unusual CWD.
- **`System.Collections.Generic.Stack<string>` fully qualified.** Necessary — `using Alphora.Dataphor.DAE.Language.D4` brings `Alphora.Dataphor.DAE.Stack<T>` into scope via the test's `Alphora.Dataphor.DAE.Server.Tests` namespace, and an unqualified `Stack<string>` crashes discovery.
- **AllowList categorisation.** Spot-read `libraries/System.Internet/Documents/Dynamic.d4` (emitter-malformed) and `tests/libraries/TestFramework.Coverage.Scripts/Documents/BooleanLibrary.d4` (emitter-reshape, multi-var pattern) against their category; both match. No sign of a miscategorised entry on sampling.
- **Source hygiene.** 525-line test fixture; 105 of those are the AllowList dictionary. Comments on the comparator are all WHY-comments (reasons for a non-obvious exclusion), not statement narration.

**What was fixed inline.** Nothing. The implementation is accepted as-is.

**What was filed.**

- `tickets/backlog/bug-d4-emitter-malformed-output.md` — eight files where the emitter produces text the parser cannot read. Clear emitter defect; the catalog store uses `D4TextEmitter` to persist DDL, so the same output goes to the store.
- `tickets/backlog/bug-d4-emitter-reshapes-parse-tree.md` — fifty-one files where both parses succeed but the tree differs. Four sub-patterns named (multi-var flattening, try/commit desugaring, added `by` clause, tag ordering); the two active catalog scripts under `src/engine/DAE/Schema/` are the priority within the list.
- `tickets/backlog/bug-d4-parser-rejects-active-sources.md` — thirty-eight files the parser rejects on first read, including the three active SQL-device catalogs the server loads at bootstrap. The decision per sub-group is whether to fix the parser or move the file under `legacy/`.

Each drain ticket cites the AllowList as its reproduction mechanism: remove one entry, re-run the test, read the failure. Together the three tickets drain the AllowList; each emptied category removes its AllowList entries in the same commit.

**What was not filed.**

- No tripwire `NOTE:` at the fixture — the AllowList entries, each with a one-line category, already serve as the in-code tripwire. One set of markers, not two. The three drain tickets above cover the "major, named class-level invariant" bar.
- No ticket for emitter modes beyond `ForCopy`. `D4TextEmitter.EmitMode` has other values; this fixture only exercises the default. Noted in the review handoff of the implement ticket; low priority because the catalog store uses the default. Add later as a drain ticket if `ForStorage` ever gets a new caller.

**Minor inaccuracies in the implement handoff (not in code).** The handoff said "the 292 corpus cases plus the 2 smokes" and "Expected: all 294 cases pass" — the actual run is 290 corpus plus 2 smokes = 292 total (`387 - 97 = 290`). Not worth a correction since the file now sits in `complete/` and will be pruned.

**Pre-existing failures.** None observed. The only test executions were under `--filter FullyQualifiedName~ParserRoundTripCorpusTest`; no other `DAE.Server.Tests` cases were exercised, so this review neither reproduces nor affects the three entries in `tickets/.pre-existing-known.md`.

## End
