description: The whole test suite was run once with code-coverage collection switched on, the measured report of what the engine's tests reach was published, and one backlog ticket was filed for each untested area worth one.
architecture: docs/modernization-plan.md#ws3-test-suite
files: docs/coverage-report.md, docs/README.md, tools/coverage-report/areas.json, tools/coverage-report/README.md, tools/coverage-report/coverage_report.py, tickets/backlog/debt-realsql-coverage.md, tickets/backlog/debt-sql-translation-coverage.md, tickets/backlog/debt-library-upgrades-coverage.md, tickets/backlog/debt-streams-coverage.md, tickets/backlog/debt-debugger-coverage.md, tickets/backlog/debt-tableindexer-coverage.md, tickets/backlog/debt-ws12-hygiene-remainder.md
----
## What landed

The pass ran, the report is published at `docs/coverage-report.md`, and six tickets are on the backlog. Nothing in the engine changed; the only code change is three entries in a data file.

- **`docs/coverage-report.md`** (new) - a hand-written head (run provenance, what the numbers mean, how to reproduce, what instrumentation did, what the pass found, the tickets filed) followed by the tool's generated tables. CRLF, no byte-order mark, one paragraph per line.
- **`docs/README.md`** - one line added to *Project documents*.
- **`tools/coverage-report/areas.json`** - one new `excluded` entry covering three dead files (below).
- **`tools/coverage-report/README.md`** - the measured whole-suite cost replaces the promise to measure it; the *Why CI does not collect this* section now reports the evidence instead of its absence; and the per-script-coverage question is parked there as a `NOTE:` tripwire, as the ticket asked.
- **`tools/coverage-report/coverage_report.py`** - the `NOTE:` at the top said "if a later pass shows the suite is stable under instrumentation"; that pass has now happened, so the note records what it found.
- **`tickets/backlog/debt-ws12-hygiene-remainder.md`** - one new arm, `hygiene-remove-dead-compiled-files`.

## The run

Build clean (0 errors, 143 warnings, all pre-existing). Three parts, each its own foreground command, all with collection:

| Part | Result | With collection |
|---|---|---|
| 1 - everything but the coverage scripts | 466 passed, 0 failed | 6 m 03 s |
| 2 - coverage scripts A-R | 67 passed, 11 skipped | 4 m 19 s |
| 3 - coverage scripts outside A-R | 49 passed, 13 skipped | 3 m 18 s |

582 passed, **0 failures anywhere**, 24 skipped - and every skip is a known failure the manifest already records, with no new one. So there is nothing to write to `tickets/.pre-existing-error.md`, and no entry in `tickets/.pre-existing-known.md` was triggered.

## Headline numbers

Engine total **66.0 %** (67,020 of 101,513 lines).

| Engine assembly | Line coverage | Coverable |
|---|---:|---:|
| `Alphora.Dataphor.DAE` | 68.9 % | 85,744 |
| `AlphoraDataphorServer` | 53.6 % | 11,402 |
| `Alphora.Dataphor` | 36.2 % | 3,670 |
| `Alphora.Dataphor.DAE.SQLite` | 64.5 % | 203 |
| `Alphora.Dataphor.Windows` | 45.9 % | 194 |
| `Alphora.Dataphor.DAE.Server.Hosting` | 89.0 % | 300 |

The libraries and client stack roll up to 17.9 % of 17,874 lines, with the two large holes being `Alphora.Dataphor.DAE.Device.SQL` at 0.2 % of 6,143 and `Alphora.Dataphor.Frontend.Server` at 14.2 % of 5,157. Four `reported` assemblies read `not in this run` (`AlphoraMSSQLDevice`, `System.Integration`, `DAEServer`, `DAEService`): nothing loads them, which is expected - the host executables are exercised through `Alphora.Dataphor.DAE.Host` rather than by being started.

The tool exits **0**: no `unassigned` file, no `unlisted assembly`. The area map needed no correction, so none of its library assembly names turned out wrong.

## What instrumentation did

This was the evidence the ticket wanted and it is favourable.

- **Cost, measured against a control.** Part 1 was re-run immediately afterwards *without* collection: 5 m 13 s against 6 m 03 s with. **+50 s, about +16 %.** Honest limit: the control was run for part 1 only, so the +16 % is a part-1 figure, not an average over the three.
- **The timings in `AGENTS.md` are stale, independently of collection.** It records part 1 at "about 2.5 minutes" and part 3 at "about 6 minutes"; the no-collection control puts part 1 at 5 m 13 s and part 3 ran in 3 m 18 s *with* collection. Part 1 has grown with the out-of-process fixtures and part 3 shrank when `test-client-server` stopped `ServerLink` timing out. I did not edit `AGENTS.md` - the figures there are another ticket's to refresh, and the report says plainly that they are stale so nobody reads the +16 % off them by subtraction.
- **`AbandonedClientProcessTest`.** Run alone, both ways: the killed client's session was gone **5.4 s after the kill under collection, 5.1 s without**; the case took 13 s against 10 s. The 0.3 s is inside the 4.7-5.1 s spread already on record. The case's own threshold is a 5 s idle setting and it waits up to 30 s before failing, so it has roughly five times the slack it needs - it is not close to the edge, which is the thing nobody could previously assert.
- **The manifest's one `timeoutSeconds` entry** is `DebugTest` (5 s, an infinite loop by design). It behaved identically: a recorded known failure either way, its 5 s timeout firing inside an 8 s slot.

No case failed only under collection, so there is no measurement finding to record against the suite.

## The tickets filed

Only **two** areas met the tool's gap bar (under 25 % with at least 200 coverable lines), so the eight-ticket cap never bound. Six filed:

| Ticket | Area | Measured | Why |
|---|---|---|---|
| `debt-realsql-coverage` | RealSQL | 0.0 % of 1,423 | worst gap; live code, reachable by any application |
| `debt-sql-translation-coverage` | SQL translation (engine) | 0.0 % of 1,031 | the other gap; every SQL back end goes through it |
| `debt-library-upgrades-coverage` | Libraries and upgrades | 49.9 % of 1,907 | thin; a plan candidate; one file inside it is at 24 % of 854 |
| `debt-streams-coverage` | Streams | 40.7 % of 1,129 | thin; a plan candidate; `StreamAllocation` is its empty predecessor |
| `debt-debugger-coverage` | Debugger | 35.7 % of 894 | thin; the ticket named it a legitimate gap |
| `debt-tableindexer-coverage` | the D4 table indexer expression | five error codes at 0 hits | mandated; carries the 3.x request forward verbatim |

`debt-realsql-coverage` was filed on the **full-run** number as instructed. It also corrects the earlier figure: the short run's "2,846 lines" double-counted - the collector lists each line twice, under its class and under its method - and the real coverable count is 1,423.

`debt-tableindexer-coverage` is the one filed against a well-covered area, so its case is measured rather than asserted: I read the merged Cobertura data for `Compiler.cs` directly, and the table-indexer region (lines 12,600-12,720) is **86.9 % covered, 86 of 99 lines**, while **all five** error-throw lines are at **zero hits** and so is the whole ambiguity-detection block at 12,677-12,685. The happy path is exercised constantly; not one error path ever has been.

## Gaps and thin areas deliberately left unticketed

Named here and in the report, with numbers, so a later pass can pick any of them up:

- **Base utilities**, 42.3 % of 3,264 - the largest uncovered block left (1,882 lines). Its 0 % files are `BOP/Inheritance.cs` and `BOP/Dilx.cs` (423 lines, the form-document customization machinery) and `CRC32.cs`, and I grepped their consumers: all are the Frontend library's customization operators, Dataphoria, and the Frontend client's document cache. That is a Frontend coverage pass, not an engine ticket, and the Frontend server assembly (14.2 %) is outside the area map anyway.
- **Device connections (engine)**, 33.1 % of 1,555. Its 0 % files are all MSSQL connection and command classes needing a live SQL Server. The manifest already records that absence as three skipped `Transaction.Atomicity.TestMSSQL*` known failures, so a ticket would only restate it. The neighbouring `debt-sql-translation-coverage` says explicitly not to fold this in.
- **WCF services and contracts**, 50.9 % - the tool marks it `covered`; it is in the report only because it is nearest the line.

## The area-map change, which is the judgement call to review

The ticket anticipated finding code at 0 % that is about to be deleted and said to exclude it rather than ticket it. I found three such files and excluded all three in one entry:

| File | Coverable | Status |
|---|---:|---|
| `src/engine/Base/IndexList.cs` | 333 | `public class IndexList`, compiled, referenced nowhere |
| `src/engine/Base/TemporaryStream.cs` | 104 | compiled, referenced nowhere |
| `src/engine/DAE/Streams/CopyOnWriteStream.cs` | 57 | compiled, referenced nowhere |

Each was verified by a repository-wide grep (excluding `artifacts/`): outside the file itself, the only mention of its type anywhere is its own `Compile` entry in `Base.csproj` or `DAE.csproj`. They are unlike the files `build-globbing-cleanup` lists, which are *not* compiled - globbing will never drop these - so nothing owned them and they are now the new hygiene arm `hygiene-remove-dead-compiled-files`.

**Two things for a reviewer to weigh.** First, the ticket's wording anticipated "a folder at 0 % whose callers are all in `legacy/`", and these are three single files with no callers at all; I read the existing REST exclusion as the governing precedent (compiled, uncalled, excluded, arm named) but the broader reading is mine. Second, excluding changes a published number: the Streams area reads **40.7 %** with them dropped and **35.7 %** with them counted. The report says so explicitly where it reports the figure, so the two numbers cannot be confused - but if the call is wrong, the fix is to drop the exclusion and re-run the tool, and the Streams ticket then needs its two numbers swapped.

A fourth file is at 0 % and was deliberately **not** excluded: `src/engine/Server/Streams/FileStreamProvider.cs` (91 lines) is only constructed inside `#if UseFileStreamProvider`, and that `#define` is commented out. It is unreachable rather than unreferenced, and whether the engine still wants a file-backed stream provider is a design question. It stays counted, and `debt-streams-coverage` tells its reader not to chase it.

## Tests

**None added, and none owed.** The project rule attaches a characterization test to a ticket that ports, re-hosts or changes the behaviour of a subsystem, or reproduces a bug. This ticket changes no subsystem: it runs an existing command, publishes a document and files tickets. The only code-adjacent change is three paths added to `tools/coverage-report/areas.json`, which is data the report tool consumes, and the tool's own suite already covers exclusion handling - `python -m unittest discover -s tools/coverage-report` passes, 10 tests, re-run after the edit.

## How to check this work

- **The report regenerates.** `python tools/coverage-report/coverage_report.py artifacts/coverage -o <somewhere>` against a fresh three-part collection should exit 0 and reproduce the tables below the head. The head is hand-written and is the part to read critically.
- **The numbers in each ticket match the report.** Every area figure quoted in the six tickets was taken from the same run; a mismatch is a copy error.
- **The five zero-hit lines.** Re-measurable from the Cobertura files: lines 12,614 / 12,625 / 12,684 / 12,709 / 12,713 of `src/engine/DAE/Compiling/Compiler.cs`, all `hits="0"`.
- **The three dead files.** `grep -rn "\bIndexList\b" --include=*.cs --include=*.csproj . | grep -v artifacts` and the same for `TemporaryStream` and `CopyOnWriteStream`; each should return only its own file and one `Compile` entry.
- **The suite is unchanged.** `tests/DAE.Server.Tests/coverage-manifest.json` is untouched, no script was edited, retired or deleted, and no test was skipped or loosened.

## Known gaps in this handoff

- The **+16 %** instrumentation cost is a part-1 measurement with a part-1 control; parts 2 and 3 were not re-run without collection.
- The report asserts the collector is cheap and quiet enough for CI. That is the evidence, not the change: no CI file was touched, and wiring it in is still someone's decision.
- `debt-streams-coverage` asks for assertions on stream reference counts but does not prove a D4 script can read them - it says finding the D4-visible form is part of the work. If a reviewer knows it cannot be done from D4, that ticket's strongest case weakens to the round-trip and rollback cases.
- The site-claim grep turned up `tickets/plan/bug-object-id-counter-is-process-wide.md` naming `src/engine/DAE/Compiling/Compiler.cs` in its `files:`. I read it and it resolves at a different site (object-identifier assignment, not the table indexer region), so `debt-tableindexer-coverage` was filed fresh rather than appended as an arm. Worth a second opinion, since it is the one grep hit I overrode.
- No `docs/migration-notes.md` entry: nothing here changes a compatibility surface or anything an application could notice. The new hygiene arm says the eventual deletion of those three public types does need one.
