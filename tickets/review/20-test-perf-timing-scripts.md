description: The test suite now measures how fast the engine's interpreter is on every run and compares it against a checked-in number when asked, and the missing operator that the two old timing scripts needed has been added. The second script still cannot be measured, because running it for the first time uncovered an engine defect that makes emptying a table one row at a time take about a quarter of an hour.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Perf/TestFramework.Perf.d4l, tests/libraries/TestFramework.Perf/Documents/Register.d4, tests/DAE.Server.Tests/PerfBaselineTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/CoverageScripts.cs, tests/DAE.Server.Tests/CoverageManifest.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/goldens/perf/perf-baseline.json, docs/perf-baseline.md, docs/README.md, AGENTS.md, tickets/backlog/bug-engine-delete-quota-scans-the-table.md
difficulty: medium
----
## What landed

**The missing operator, in a test-only D4 library.** `tests/libraries/TestFramework.Perf/` is two text files and no C# project: a `.d4l` descriptor requiring only `System`, and a `Documents/Register.d4` that binds `BenchmarkExecute(const AString : String) : TimeSpan` to the engine node `System.SystemBenchmarkExecuteNode`, which has always been there and needed no change. It carries the determinism tag set the engine's own `Execute` operator carries, for the reason the ticket gave: with `IsLiteral` left at its default the compiler may evaluate a benchmark while compiling it, and the deterministic and repeatable flags invite the plan cache to hand one measurement back for several calls. The library joins the runtime layout through the existing `tests/libraries/*/*.d4l` glob in `tools/layout/Layout.csproj`, with no edit to that file, and is registered only by the two consumers that name it. `"TestFramework.Perf"` was added to `CoverageScripts.Libraries`, which is what lets a manifest entry name it in `register`.

**One thing the ticket did not anticipate, and it is the first thing to look at.** That `Register.d4` also runs `alter device System.Temp alter class { alter "MaxRowCount" = "-1" };`. Without it `Timings` fails at its 1,001st row: the engine's in-memory device caps a table at 1,000 rows (`Engine.TempDeviceMaxRowCount`, error 114113) and the script inserts 10,000. The script's own recorded numbers are for 10,000 rows, so it ran with the cap lifted - the old hand-wired fixture registered `TestFramework.Coverage.Devices`, whose `Register.d4` lifts it the same way, and `LongAggregates.d4` lifts it inside its own script. Coverage scripts are never edited, so the lift went into the new library. **It is a server-wide device setting changed by a library registration**, which is a side effect worth a reviewer's judgement; it reaches only the servers that register this library (the `Timings` coverage case and `PerfBaselineTest`), and the comment at the site says all of this.

**The perf pass.** `tests/DAE.Server.Tests/PerfBaselineTest.cs`, one fixture over `InProcessTestFixture` and one test method looping a scenario table, in the shape of `FrontendDerivationGoldenTest`. It reads each script's text from its own server's copy of `TestFramework.Coverage.Scripts` - the same place the coverage runner's `batches` path reads it - and runs it through `IServerProcess.PrepareScript` batch by batch: a statement batch is timed with a `Stopwatch`, and an expression batch whose value is a `TimeSpan` is recorded by its value rather than by the wall clock around it, with a fall back to wall clock otherwise and an open-and-close branch for a table-valued expression. Scenario keys are the script's name for its own elapsed time and `name#ordinal` per expression batch. Three behaviours, by two environment variables: record and pass by default (writing `artifacts/perf/perf-run.json`), compare when `DATAPHOR_PERF_BASELINE=<label>` names a label (warn past 3×, fail past 10×), and rewrite that label's entry and then fail when `DATAPHOR_UPDATE_PERF_BASELINE=1` joins it. The comparison itself is a pure static method taking the numbers, the whole baseline file, the label and the two factors.

**The baseline and the document.** `tests/goldens/perf/perf-baseline.json` carries one label, `dev-slow`, with `Timing` at 16,144 ms, written by an update run and read back by a comparison run. `docs/perf-baseline.md` is new, modelled on `docs/coverage-report.md`, with the numbers, what they do not mean, how to regenerate and compare, the reasoning behind the two factors, the `Timings` finding, and the 3.1 comparison as a written procedure with an empty results table. One line added to the *Project documents* list in `docs/README.md`; the link checker's error count is unchanged by this change (203 before, 203 after - all pre-existing, owned by the documentation conversion).

## What did NOT land, and why

**`Timings` is not a baseline scenario and is not a passing coverage case.** This is the substantive gap and the reviewer should read it as such.

Running the script for the first time - which nothing had ever done, because the operator did not exist - showed that three of its four blocks are fast and the third does not finish. Its third block drains a 10,000-row table one row at a time with `while exists (T) do delete T return 1 by { ID };`, and the cost of that statement grows with the number of rows in the table, so the loop is quadratic. Measured on this machine, in Debug, on 2026-11-21:

| Rows | Drain | Cost per row |
|---|---|---|
| 250 | 397 ms | 1.6 ms |
| 500 | 1,885 ms | 3.8 ms |
| 1,000 | 7,866 ms | 7.9 ms |
| 2,000 | 34,308 ms | 17.2 ms |
| 4,000 | 150,879 ms | 37.7 ms |

About 4.4 times per doubling, so about a quarter of an hour at the script's 10,000 rows, against the 8 seconds the script's own 2008 comments record. Four controls at 2,000 rows pin it down: the same loop with no order clause costs the same 34 s (so the order is not the cause), `for LIndex := 1 to 2000 do delete (T where ID = LIndex);` costs 130 ms (so per-row deletion, the device and the transaction are not the cause), `delete T;` costs 66 ms (so deleting many rows is cheap), and reading the same quota 2,000 times without deleting costs 39 ms (so the quota expression is not the cause). The cost appears only when a quota is the target of a delete.

Blocks 1 and 2, for the record, took **431 ms** and **1,461 ms** against the roughly 5 s and 9.5 s the 2008 comments record - several times faster - and `Timing` took 15-19 s against the 12.5 s those comments record for the same loop, so the drain is a genuine outlier and not a Debug-build or slow-machine artefact. Block 4 was never reached.

So the manifest entry for `Timings` is a `known-failure` against `bug-engine-delete-quota-scans-the-table` with the error `HarnessTimeout: script did not finish within` and `timeoutSeconds: 15` - short on purpose, so the suite does not pay for the grind; the case costs about 20 s in part 3. The known-failure count across the suite is unchanged at 21: `Timings` was already one, and its cause has moved from "nothing declares the operator" to the measured one. `PerfBaselineTest`'s scenario table has one row, with a remark saying that adding `Timings` back is one line once the defect is fixed.

**I did not file this to `blocked/`, although the ticket said to.** The ticket's escalation rule was "if the script takes more than 240 s, file the choice between moving it out of the coverage corpus and leaving it in to `blocked/`". That rule assumed the time was real work and that the cost was the question. It is not: three of four blocks total under two seconds, and the fourth is a defect. There is no human decision to take - the script stays a recorded known failure costing a bounded 15 s either way, which is what the manifest already encoded - so the finding went to `tickets/backlog/bug-engine-delete-quota-scans-the-table.md` with `repro: verified`, the curve, the four controls, the named code site to look at first, and an explicit line separating what was measured from what was inferred. If the reviewer thinks the decision is a human's after all, that is the thing to overrule here.

## Tests added

| Test | What it verifies |
|---|---|
| `PerfBaselineTest.TheTimingScriptsRunAndTheirNumbersMatchTheBaseline` | the pass itself: every scenario's script runs batch by batch without raising, its numbers are recorded, and - when a label is named - each is inside tolerance of the number checked in under that label. This is also the only place in the suite where a breakage in `TestFramework.Perf`'s `Register.d4` fails plainly, which is why the fixture registers that library although its one scenario does not call the operator. |
| `PerfComparisonTest.ComparingTellsTheBandsApart` | the pure comparison method, on a fabricated baseline with factors of its own and no server: at the recorded number it reports nothing, at exactly the warning factor it is inside tolerance, just past it warns, just past the failure factor it fails, an unknown label fails, a measured scenario the file does not list fails, and a recorded scenario the run did not measure fails. |

No test was added for the library descriptor, the manifest entries or the csproj entry: the layout's `.d4l` check, the manifest validator and the compiler already enforce those, and the `Timing` and `Timings` coverage cases exercise the library end to end.

## How to validate

Build first; each command is its own foreground run.

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=ComparingTellsTheBandsApart" --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline" --logger "console;verbosity=normal"
DATAPHOR_PERF_BASELINE=dev-slow dotnet test tests/DAE.Server.Tests --no-build --filter "Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline" --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=Timing|Name=Timings" --logger "console;verbosity=normal"
```

What each should show: the comparison test green in milliseconds; the default perf run printing its batch lines as they are taken, then `Timing = <n> ms`, then passing; the labelled run doing the same and passing while comparing against `dev-slow` (it will warn, correctly, on a machine more than three times slower than the one that recorded it - that is the pass working, not a failure of this change); and `Timing` passing with `Timings` skipped as a matched known failure reading `known-failure (bug-engine-delete-quota-scans-the-table): HarnessTimeout: script did not finish within 15 s`.

Things worth poking at specifically:

- **The update path, which must always fail.** `DATAPHOR_UPDATE_PERF_BASELINE=1 DATAPHOR_PERF_BASELINE=dev-slow dotnet test ... --filter Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline` rewrites that label's entry and fails with a message saying so. Check the diff it leaves: the hand-written `note` must survive the rewrite (that is deliberate - the note is human knowledge about the machine class), the file must stay LF (`.gitattributes` pins `tests/goldens/**`), and no other label may move. Then `git checkout` is not the way back - re-record or restore the committed number, whichever you mean.
- **The update variable alone.** With `DATAPHOR_UPDATE_PERF_BASELINE=1` and no label the case fails saying the label is required.
- **An unknown label.** `DATAPHOR_PERF_BASELINE=no-such-label` must fail with the command that would record one, not pass quietly.
- **That the default never gates.** The recorded-and-pass path is what every CI run and every agent run takes; a change that lets it fail on a slow machine is the worst outcome available here.
- **The determinism tags.** The way to see them working is the numbers: a `BenchmarkExecute` value identical to the tick across calls, or a near-zero value for a block that inserts 10,000 rows, would mean the call was folded at compile time or reused from the plan cache. The four values measured here were 431 ms, 1,461 ms and then the drain, all distinct, so nothing is being folded today - but only two of the four were ever reached, so this is less well covered than it will be once `Timings` is a scenario.

## Suite cost, measured

Three parts, as the root `AGENTS.md` describes, all green: part 1 **577 passed, 0 failed**; part 2 **67 passed, 11 skipped**; part 3 **49 passed, 13 skipped** - 24 skipped in total, which is exactly the manifest's 21 known failures and 3 fragments, unchanged by this ticket.

The timings are noisier than the existing figures suggest and I corrected `AGENTS.md` rather than quoting one run: over several runs of the same build that afternoon the same machine gave part 1 anywhere from 5 m 20 s to 7 m 32 s, part 2 from 3 m 28 s to 6 m 35 s and part 3 from 3 m 48 s to 5 m 54 s. The best of each (5 m 35 s, 3 m 28 s, 3 m 48 s) is what the document now records, together with the spread and the warning that part 1's worst observed run leaves only about two and a half minutes of the runner's ten-minute idle limit. I first wrote that the slow runs were the ones taken immediately after a build; part 3's slow run was not, so that claim was removed rather than kept as a guess. **This ticket's own contribution to the suite is about 20 s in part 1 and about 20 s in part 3** - inside that noise, but it is the direction that matters and part 1 is already the part to watch.

## Tripwires and notes parked in the code

- `PerfBaselineTest`'s scenario table carries an accepted-tradeoff `NOTE:` recording that `Timing` now runs twice per suite - once as a coverage case checking only that it does not raise, once here capturing the number - with the two alternatives that were weighed (measuring inside the coverage runner, or moving the script out of the corpus) and the revisit condition (if the table grows enough that the duplication is minutes rather than seconds).
- The same fixture's remarks record that the measurements are script time and never include server start, so the figures here and the coverage runner's for the same script must not be compared with each other; `docs/perf-baseline.md` says it again where the numbers are published.
- A remark on the scenario table records why `Timings` is absent, what its first two blocks measured, and that re-adding it is one line.
- `Measure` writes each number to `TestContext.Progress` as it is taken rather than collecting and printing at the end, with a comment on why: a run that is killed or times out still shows how far it got, and a silent multi-minute case is exactly what the ticket runner's ten-minute idle limit kills. This was not in the ticket's design; it is how the `Timings` problem was found at all.

## One unrelated correction made along the way

`CoverageManifest`'s format remarks said the manifest has LF line endings. The file has CRLF, and `.gitattributes` deliberately leaves `.json` alone, so the remark invited a renormalization that the repository's hygiene rules forbid. One word, corrected in place; no behaviour change.

## Known gaps, stated plainly

- `Timings` contributes no baseline numbers, for the reason above. The plan's requirement that "the existing `Timing`/`Timings` scripts ... record a baseline on .NET 10" is half met, and the other half is gated on `bug-engine-delete-quota-scans-the-table`.
- The baseline has one label and one scenario, recorded in Debug on one machine. 3× and 10× are asserted policy, not thresholds derived from data; four runs spanning 15,246-19,291 ms is all the evidence there is, and the document says so.
- CI records and does not compare, deliberately: `.github/workflows/build.yml` is untouched, and `docs/perf-baseline.md` says what has to happen before it compares.
- The `by { ID }` ordering in the drain was ruled out as the cause of the slowness, but **which node the delete copies** was not isolated. The backlog ticket names `DeleteNode.InternalBindingTraversal` as the lead and says explicitly that the lead is inference.
- `plan.DataType is Schema.TableType` in `ExecuteBatch` is a branch no scenario reaches today. It is there so that a scenario added later cannot fail obscurely; a reviewer who would rather have no unreached branch has a fair case for deleting it.
