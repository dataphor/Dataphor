# Test coverage report

What the test suite reaches in the engine, and what it does not, measured on one full run of the suite with Microsoft's coverage collector switched on. Everything from *Line coverage per assembly* down is the output of `tools/coverage-report/coverage_report.py`, and the next run replaces it wholesale; this head is written by hand and says where the numbers came from and how to read them.

## This run

| | |
|---|---|
| Date | 2026-11-21 |
| Commit | `f6200026` |
| Configuration | Debug, .NET 10, Windows x64 |
| Machine | one slow development laptop, nothing else running |
| Suite | one full three-part run: 582 tests passed, 0 failed, 24 coverage scripts skipped as the manifest's recorded known failures |
| Collector | Microsoft's `Code Coverage` collector through `dotnet test --collect`, three Cobertura reports merged into one |

The three parts are the ones the root `AGENTS.md` describes under *Agents run the suite in parts*, so between them every test in the repository ran once. That matters for reading the areas below: several engine subsystems are reached only by the NUnit fixtures in part 1 (application transactions, server links, connection security, library loading) and several others only by the D4 coverage scripts in parts 2 and 3, so a report built from fewer than three parts would understate whichever it left out.

## What these numbers mean, and what they do not

**A covered line is a line that ran. It is not a line whose result anything checked.** The collector marks a line green when execution reached it, and says nothing about whether a test looked at what came out. So read a high rate as *exercised* and never as *verified*; read a low rate as a real absence, which is the direction this report is for.

The suite's 116 passing coverage scripts are the clearest case of the difference. Nine of them assert nothing at all - the manifest records each as "no assertions": `Functions`, `LibraryFilesCoverage`, `LockAllocation`, `Register`, `RowAllocation`, `ScalarAllocation`, `SerializationDevices`, `TestGH37` and `Warnings`. Eight hold only comments and one is empty. Every line they execute is green in the tables below and nothing about it is checked, so the parts of the engine those nine were written for are, in substance, untested however they colour. Their names are a fair list of what their authors meant to cover and did not: allocation and leak counting, library file registration, device serialization, the function library and compiler warnings.

Two more mechanical points. Line counts are not statement counts and are not comparable between builds, because the collector decides which lines are coverable and that moves with the code - compare *rates* between runs of the same build, not counts. And an area's rate is a weighted average over its files, so one large well-covered file can hide a wholly untested neighbour; that is what the per-file tables under each gap are for.

An area below 25 % line coverage is a **gap** unless it has fewer than 200 coverable lines; below 50 % and not a gap it is **thin**. Areas are listed worst first by uncovered line count, so the biggest holes are the first a reader meets.

## Reproducing it

Build once, then run the three parts with the collector added and merge them. Each `dotnet test` is its own command; the collector's arguments go before the `--`, which stays last.

```
dotnet build Dataphor.slnx
dotnet test Dataphor.slnx --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part1 -- "NUnit.Where=class != Alphora.Dataphor.DAE.Server.Tests.CoverageScripts"
dotnet test tests/DAE.Server.Tests --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part2 -- "NUnit.Where=test =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.CoverageScripts\.(Script\.)?(?!Script$)[A-Ra-r]'"
dotnet test tests/DAE.Server.Tests --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part3 -- "NUnit.Where=test =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.CoverageScripts\.(Script\.)?(?!Script$)[^A-Ra-r]'"
python tools/coverage-report/coverage_report.py artifacts/coverage -o docs/coverage-report.md
```

`artifacts/` is git-ignored, so a run leaves nothing in the tree. The tool's options, the area map it classifies against and its two boundary checks are documented in [tools/coverage-report/README.md](../tools/coverage-report/README.md).

The tool's `--json` option writes the same rolled-up numbers as a file for a later pass to diff against. Nothing checks one in: `artifacts/` is discarded and a generated data file does not belong in `docs/`. A second pass should therefore compare against the *rates* published here rather than hunt for this run's JSON, and write its own JSON only as a scratch file beside its own collection.

## What instrumentation did to the run

Nobody had run the whole suite under the collector before, and the open question was whether the cases that assert on wall-clock behaviour survive it. They did: every part passed, with no failure of any kind and no case that failed only under collection.

- **Cost.** The three parts took 6 m 03 s, 4 m 19 s and 3 m 18 s with collection, 13 m 40 s together. Part 1 re-run immediately afterwards without collection took 5 m 13 s, so the collector cost **+50 s on a 5 m 13 s part, about +16 %**. (The "about 2.5 minutes" recorded for part 1 in `AGENTS.md` is simply stale - part 1 has grown since, with the out-of-process client/server fixtures - and part 3's "about 6 minutes" is now 3 m 18 s because `test-client-server` stopped `ServerLink` timing out. Neither movement is an instrumentation effect.)
- **The session-reclamation case.** `AbandonedClientProcessTest` kills a client process holding a session and measures how long the server takes to reclaim it, against an idle threshold of 5 s. Run alone, it reported the session gone **5.4 s after the kill under collection against 5.1 s without**, and the case itself took 13 s against 10 s. The 0.3 s is inside the 4.7-5.1 s spread already recorded for it, and the case waits up to 30 s before failing, so it has roughly five times the slack it needs. This is the case most likely to be disturbed by instrumentation, and it was not.
- **The one `timeoutSeconds` entry.** `DebugTest` is an infinite loop by design, stopped by the manifest after 5 s; it is a recorded known failure either way and behaved identically under collection, finishing its 5 s timeout within an 8 s slot.

On this evidence the collector is cheap enough and quiet enough to be wired into the existing CI test step. That remains a separate decision and a separate change: nothing here gates on a number, and nothing should.

## What this pass found

The work item guessed at the areas with no script coverage - application transactions, server links, security, library upgrade scripts and streams. The measurement contradicts four of the five. Application transactions (78.9 %), server links (80.5 %) and security (78.4 %) are among the better-covered areas in the engine, because NUnit fixtures reach them even though no D4 script does: `ApplicationTransactionTest`, `ServerLinkConnectionTest`, `ServerLinkUserRollbackTest`, `ConnectionSecurityModeTest` and `LibraryLoadingTest` all exist and all run in part 1. Library upgrades (49.9 %) and streams (40.7 %) are genuinely thin and have tickets below.

The two real gaps were not on that list, and both are whole subsystems at exactly zero: the **RealSQL** language (1,423 lines) and the engine's **SQL emitter** (1,031 lines). Neither is dead code - RealSQL is selected by a D4 operator that any application can call, and the SQL emitter is what every SQL device uses to turn a translated plan into text - so both are absences rather than artefacts of the measurement.

Three files turned out to be neither covered nor worth covering: `src/engine/Base/IndexList.cs`, `src/engine/Base/TemporaryStream.cs` and `src/engine/DAE/Streams/CopyOnWriteStream.cs` - 494 coverable lines of compiled public classes that nothing in the repository references, found at 0 % and then confirmed by grep. All three are excluded from the area rollup as code that is going to be deleted, with their reason and their owning ticket, and the removal is now an arm on `debt-ws12-hygiene-remainder`. Dropping them is why the Streams area reads 40.7 % here rather than the 35.7 % the same run gives with dead code counted, and Base utilities 42.3 % rather than 38.4 %.

## Tickets this pass filed

Six, all in `tickets/backlog/`, so the report and the board can be read against each other.

| Ticket | Area | Measured |
|---|---|---|
| `debt-realsql-coverage` | RealSQL | 0.0 % of 1,423 lines |
| `debt-sql-translation-coverage` | SQL translation (engine) | 0.0 % of 1,031 lines |
| `debt-library-upgrades-coverage` | Libraries and upgrades | 49.9 % of 1,907 lines |
| `debt-streams-coverage` | Streams | 40.7 % of 1,129 lines |
| `debt-debugger-coverage` | Debugger | 35.7 % of 894 lines |
| `debt-tableindexer-coverage` | the D4 table indexer expression, in the Compiler area | five error codes, none of them reached |

Three areas the tables mark `thin` deliberately have no ticket, and the reasons belong with the numbers:

- **Base utilities**, 42.3 %, the largest uncovered block left. Its holes are not engine behaviour: `BOP/Inheritance.cs` and `BOP/Dilx.cs` (423 lines, both at 0 %) are the form-document customization machinery, reached only through the Frontend library's customization operators, and `CRC32.cs` only through the Frontend client's document cache. The Frontend server assembly itself sits at 14.2 % and is outside the engine area map, so this belongs to a Frontend coverage pass rather than to an engine ticket.
- **Device connections (engine)**, 33.1 %. Its 0 % files are all the MSSQL connection and command classes, which need a running SQL Server. The suite already records that absence: the three `Transaction.Atomicity.TestMSSQL*` scripts are skipped known failures for exactly this reason, so a new ticket would only restate what the manifest says.
- **WCF services and contracts**, 50.9 %, which the tool marks `covered`; listed here only because it is the closest of the covered areas to the line.

The cap on this pass was eight tickets. Six were filed, so nothing was left out for want of room.

## Line coverage per assembly

### Engine

| Assembly | Line coverage | Covered | Coverable |
|---|---:|---:|---:|
| Alphora.Dataphor | 36.2 % | 1,329 | 3,670 |
| Alphora.Dataphor.Windows | 45.9 % | 89 | 194 |
| Alphora.Dataphor.DAE | 68.9 % | 59,087 | 85,744 |
| AlphoraDataphorServer | 53.6 % | 6,117 | 11,402 |
| Alphora.Dataphor.DAE.Server.Hosting | 89.0 % | 267 | 300 |
| Alphora.Dataphor.DAE.SQLite | 64.5 % | 131 | 203 |
| **Total** | **66.0 %** | **67,020** | **101,513** |

### Also reported

Libraries, the client stack and the hosts. These are rolled up per assembly only; the area map covers the engine.

| Assembly | Line coverage | Covered | Coverable |
|---|---:|---:|---:|
| AlphoraDataphorCommon | 81.6 % | 310 | 380 |
| AlphoraDataphorClient | 41.9 % | 1,536 | 3,668 |
| Alphora.Dataphor.DAE.NativeCLI | 52.7 % | 251 | 476 |
| Alphora.Dataphor.DAE.Host | 36.2 % | 114 | 315 |
| Alphora.Dataphor.Frontend.Server | 14.2 % | 734 | 5,157 |
| Alphora.Dataphor.DAE.Device.SQL | 0.2 % | 15 | 6,143 |
| AlphoraMSSQLDevice | - | - | not in this run |
| Alphora.Dataphor.DAE.Device.Simple | 57.6 % | 179 | 311 |
| Alphora.Dataphor.DAE.Connection | 4.2 % | 7 | 167 |
| System.Platform | 2.6 % | 10 | 390 |
| System.Internet | 5.8 % | 50 | 867 |
| System.Integration | - | - | not in this run |
| DAEServer | - | - | not in this run |
| DAEService | - | - | not in this run |
| **Total** | **17.9 %** | **3,206** | **17,874** |

## Engine areas

| Area | Line coverage | Covered | Coverable | Uncovered | Verdict |
|---|---:|---:|---:|---:|---|
| Plan nodes | 66.6 % | 16,380 | 24,603 | 8,223 | covered |
| Runtime values | 60.4 % | 6,286 | 10,408 | 4,122 | covered |
| D4 language | 77.6 % | 9,543 | 12,291 | 2,748 | covered |
| Server and sessions | 65.7 % | 5,165 | 7,857 | 2,692 | covered |
| Compiler | 80.9 % | 9,911 | 12,249 | 2,338 | covered |
| Schema | 72.2 % | 5,774 | 8,000 | 2,226 | covered |
| Base utilities | 42.3 % | 1,382 | 3,264 | 1,882 | thin |
| Catalog device and store | 76.7 % | 6,088 | 7,934 | 1,846 | covered |
| RealSQL | 0.0 % | 0 | 1,423 | 1,423 | gap |
| Device connections (engine) | 33.1 % | 515 | 1,555 | 1,040 | thin |
| SQL translation (engine) | 0.0 % | 0 | 1,031 | 1,031 | gap |
| Libraries and upgrades | 49.9 % | 951 | 1,907 | 956 | thin |
| Streams | 40.7 % | 460 | 1,129 | 669 | thin |
| WCF services and contracts | 50.9 % | 689 | 1,354 | 665 | covered |
| Debugger | 35.7 % | 319 | 894 | 575 | thin |
| Application transactions | 78.9 % | 1,265 | 1,603 | 338 | covered |
| Native CLI (server side) | 76.0 % | 673 | 885 | 212 | covered |
| Security | 78.4 % | 473 | 603 | 130 | covered |
| Server links | 80.5 % | 268 | 333 | 65 | covered |
| Engine errors and registration | 60.6 % | 57 | 94 | 37 | covered |
| Memory device | 91.0 % | 223 | 245 | 22 | covered |

## Gaps, file by file

### RealSQL - 0.0 % of 1,423 lines

| File | Line coverage | Covered | Coverable |
|---|---:|---:|---:|
| src/engine/DAE/Language.RealSQL/RealSQLCompiler.cs | 0.0 % | 0 | 771 |
| src/engine/DAE/Language.RealSQL/RealSQLParser.cs | 0.0 % | 0 | 632 |
| src/engine/DAE/Language.RealSQL/RealSQLKeywords.cs | 0.0 % | 0 | 20 |

### SQL translation (engine) - 0.0 % of 1,031 lines

| File | Line coverage | Covered | Coverable |
|---|---:|---:|---:|
| src/engine/DAE/Language.SQL/Emitter.cs | 0.0 % | 0 | 550 |
| src/engine/DAE/Language.SQL/Language.cs | 0.0 % | 0 | 461 |
| src/engine/DAE/Language.SQL/Keywords.cs | 0.0 % | 0 | 20 |

## Excluded from the area rollup

Counted by the collector, deliberately left out of the areas: not a gap, never ticketed.

| Paths | Files | Coverable | Reason |
|---|---:|---:|---|
| `artifacts/` | 1 | 1,154 | generated source; the CoreWCF operation-invoker generator writes into artifacts/obj/ |
| `src/engine/DAE/REST/`, `src/engine/Server/REST/` | 1 | 203 | RESTResult and RESTMarshal are compiled but nothing in the repository calls them: their only consumer was the web prototype now parked in legacy/, and removal is an arm on the hygiene umbrella (debt-ws12-hygiene-remainder) |
| `src/engine/Base/IndexList.cs`, `src/engine/Base/TemporaryStream.cs`, `src/engine/DAE/Streams/CopyOnWriteStream.cs` | 3 | 494 | compiled public classes that nothing in the repository references: outside each file itself the only mention of IndexList, TemporaryStream or CopyOnWriteStream anywhere is its own Compile entry in Base.csproj or DAE.csproj (verified 2026-11-21 by test-coverage-gap-analysis), and removal is an arm on the hygiene umbrella (debt-ws12-hygiene-remainder) |
