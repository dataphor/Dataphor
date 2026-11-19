description: Design how the project records a performance baseline on .NET 10 — settle where the BenchmarkExecute operator the Timings script needs lives, how the numbers from Timing and Timings are kept across runs, and what the small benchmark project measures — so that later regressions are visible instead of folklore.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/Timings.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/Timing.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/DAE/Runtime.Instructions/SystemNodes.cs
difficulty: medium
----
## What this is

The plan's WS3 work item `test-perf-baseline`: "Benchmarks and timing scripts; publish numbers in `docs/`." Plus the "Coverage and performance baselines" cross-cutting requirement: "the existing `Timing`/`Timings` scripts plus a small benchmark project record a baseline on .NET 10 so that later regressions are visible". The pilot does one hand comparison against an installed 3.1, out-of-band.

## Open questions this plan must settle

1. **The BenchmarkExecute operator.** `Timings.d4` calls `BenchmarkExecute(AString)` four times and has no assertions; the `create operator` line (class `System.SystemBenchmarkExecuteNode`) is commented out in the script itself (line 6). The engine still has `SystemBenchmarkExecuteNode` in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs`. The arm in the umbrella says "in the helper library, not by editing the script". Pick where it lives:
   - a new library `TestFramework.Perf` registered only by the perf fixture, with a `Register.d4` that creates the operator,
   - or add the create to an existing helper library (`TestFramework.Coverage.Base`), which the coverage template registers for *every* run; this would re-register `BenchmarkExecute` on every script, which is cheap but widens the template's attack surface for an unrelated failure.
   Default: new `TestFramework.Perf` library under `tests/libraries/TestFramework.Perf/`. Decide in planning and name the choice in the implement ticket.
2. **How the two timing scripts' numbers are recorded.** `Timing` and `Timings` have no assertions (per the coverage manifest's audit); their value is the elapsed times. Options:
   - a perf fixture runs them and captures `ExecutionTime` through `ExecuteScript(...)`'s timings or through `BenchmarkExecute`'s return value, writes a JSON file next to the TRX output, and compares the current run against a checked-in baseline file with a configurable tolerance (fail if regression > N × baseline);
   - write only, no compare — numbers land in `docs/perf-baseline.md`, humans eyeball them;
   - write-and-compare but only as a Warning (counted, not failed).
   Default: write-and-compare as Warning. The baseline file is `docs/perf-baseline.json` (machine-written) with a companion `docs/perf-baseline.md` (hand-maintained narrative). The fixture re-runs the baseline on demand through an environment variable (`DATAPHOR_UPDATE_PERF_BASELINE=1`).
3. **Flip the manifest entry for `Timings`.** The arm in the umbrella says: "flip `Timings`' manifest entry in the same change." Pick:
   - once the operator exists via the perf library, add `"register": ["TestFramework.Perf"]` and set `Timings` to `pass`; or
   - move `Timings` out of the coverage manifest entirely (and run it only from the perf fixture).
   Default: keep it in the manifest, add `register`, flip to `pass`. The second script `Timing` already passes (no missing operator); it stays `pass` with an added `note` pointing at the perf fixture.
4. **Benchmark project.** The plan asks for "a small benchmark project". Pick:
   - BenchmarkDotNet under `tests/Dataphor.Benchmarks/` as a console project, excluded from `Dataphor.slnx` by default (run out-of-band because BDN runs are long);
   - or a hand-rolled fixture that uses `Stopwatch` and reports to NUnit.
   Default: BenchmarkDotNet, excluded from the default `dotnet test` run; invoked by its own command. What it measures: engine start time, a representative DDL/DML mix on the in-memory device, and the SQLite catalog store's write/read on a cold and warm instance (three scenarios, each in BDN's default job). Numbers export to `docs/perf-baseline.md` on demand.
5. **Pilot comparison with 3.1.** Out-of-band by hand once (the plan's rule). The implement ticket writes down the procedure in `docs/perf-baseline.md` so the pilot's operator can repeat it (install 3.1 beside the modern build, run the same four Timings statements, paste both outputs side by side into the markdown). Not automated.

## Expected output

One `implement/` ticket keeping the slug `test-perf-baseline`, with the open questions settled and the TODOs scoped to:

- Add `tests/libraries/TestFramework.Perf/` with `Register.d4` creating `BenchmarkExecute` (unless the planner picks a different site in #1).
- Add the perf fixture that runs `Timing` and `Timings`, writes the JSON baseline, compares against the committed one with the warning behaviour.
- Add the BenchmarkDotNet project with the three scenarios.
- Flip the manifest entry for `Timings` and add the note on `Timing`.
- Write the initial `docs/perf-baseline.md` (narrative + how-to-regenerate + pilot comparison stub).

## Edge cases & interactions

- `BenchmarkExecute` is registered only under the perf library, so an engine run that does not register that library will still see the operator missing — this is intended, so a stray regression that runs the script somewhere else fails visibly.
- CI machines are noisier than local machines; BDN handles this internally, but the write-and-warning compare in #2 must use a generous tolerance (start at 3× and tune), and the comparison baseline must be checked in from a known machine.
- `research-catalog-load-profile` (backlog, prereq `test-perf-baseline`) builds on these numbers; keep the perf fixture extensible (one scenario per NUnit test case) so that ticket adds a case rather than a new fixture.
- The nine "no assertions" coverage scripts (`Functions`, `LibraryFilesCoverage`, `LockAllocation`, `Register`, `RowAllocation`, `ScalarAllocation`, `SerializationDevices`, `TestGH37`, `Warnings`) are not timing scripts — they belong to `test-coverage-gap-analysis`, not here.
- The three per-process engine baselines (start, DDL/DML, catalog store) must not share state across BDN iterations; each BDN iteration starts a fresh `TestServerInstance` (expensive, but correct).

## TODO

- Settle each open question above with a chosen default; route anything requiring the owner's call to `blocked/`.
- Decide where to publish `docs/perf-baseline.md` (new or part of an existing docs page).
- Emit the implement ticket keeping the slug `test-perf-baseline`.
- Delete this plan ticket on the way out.
