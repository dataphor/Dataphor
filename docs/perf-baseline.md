# Performance baseline

How fast the engine is, recorded so that a change which makes it much slower shows up as a difference rather than as a hunch. There are two sources, each with its own sections below, and everything here is hand-written: `PerfBaselineTest` in `tests/DAE.Server.Tests` runs the coverage corpus's timing scripts on every test run and writes the machine-readable copy of their numbers to `tests/goldens/perf/perf-baseline.json`; the benchmark program `DAE.Benchmarks` in `tests/Dataphor.Benchmarks` is run by hand and measures what a script inside a running server cannot reach. This document says where every number came from, what it means and what it does not.

This is a tripwire, not a performance measurement of the product. Nothing in the suite gates on these numbers unless a run explicitly asks it to.

## The timing scripts

| | |
|---|---|
| Label | `dev-slow` |
| Date | 2026-11-22 |
| Configuration | Debug, .NET 10, Windows x64 |
| Machine | one slow development laptop, nothing deliberately competing - the slower of the two machine classes the root [AGENTS.md](../AGENTS.md) quotes suite timings for. Suite timings on this machine varied by half again between runs of the same build on the day these numbers were taken, so "quiet" here means no other work was started on purpose, not that the machine was idle |
| Measured by | `PerfBaselineTest`, two scenarios, script time only |

| Scenario | What it runs | Milliseconds |
|---|---|---|
| `Timing` | the `Timing` coverage script: a ten-million-iteration counting loop, which is the engine interpreter's per-node cost and nothing else | 17,764 |
| `Timings` | the `Timings` coverage script, whole: the four blocks below, each timed by the engine's own `BenchmarkExecute` operator | 3,959 |
| `Timings#1` | insert 10,000 rows into a table on the in-memory device, one row per statement | 663 |
| `Timings#2` | update all 10,000 rows in one statement | 1,711 |
| `Timings#3` | `while exists (T) do delete T return 1 by { ID };`, then drop the table | 1,346 |
| `Timings#4` | 100,000 integer additions in a loop | 204 |

Run-to-run spread on this machine. `Timing` has been measured six times: 15,246, 16,144, 17,333 and 19,291 ms over the four runs taken when this table first held one scenario, and 15,111 and 17,764 ms on the day `Timings` was added - about ±12 % around the middle of that range. `Timings` has been measured twice, at 3,343 and 3,959 ms (its blocks at 447/1,409/1,294/150 and 663/1,711/1,346/204 ms). The recorded numbers are one run's, the run that rewrote the file, and for `Timing` that run sits in the upper half of the range, so a later run on this machine has room to be slower without warning - read a warning on these scenarios as a reason to measure again, not as a finding. That spread, on one machine with nothing deliberately competing, is the reason the warning factor is 3 and not something tighter.

For comparison, the script's own comments record 12,500 ms for the same loop on the hardware of 2008, in a release build. So the current Debug build on this laptop runs the interpreter at roughly the speed a 2008 release build ran on 2008 hardware. That is a useful calibration to keep in mind when reading any other number here.

## What the timing-script numbers do not mean

**They are wall clock on a Debug build of one machine.** An absolute time here is comparable only with another run on the same machine class in the same configuration, which is what the label is for. Comparing a number from this table with one from a different label, or from a Release build, says nothing.

**They are script time, not suite time.** The fixture starts its server once and then measures batches, so no number here includes the seconds a test case spends copying library folders and starting an engine. The coverage runner's figures for the same scripts do include that, so the two sets must never be put in one table.

**A covered scenario is not a measured subsystem.** A counting loop and one script of inserts, updates and deletes against the in-memory device say something about the interpreter's statement dispatch and that device's write path, and nothing about the compiler, the catalog store, the SQL devices or the network. Engine start, a mix of database work and the catalog store cold and warm are measured by the benchmark program instead; its numbers are further down this document and were taken in Release, so they are not comparable with the Debug numbers here.

## How to regenerate the timing-script numbers, and how to compare

The pass is one NUnit case, `PerfBaselineTest.TheTimingScriptsRunAndTheirNumbersMatchTheBaseline`, and it reads two environment variables.

**Record and pass** - neither variable set, which is what every ordinary test run does. The numbers are printed as they are taken and written to `artifacts/perf/perf-run.json` (git-ignored, so a CI job can upload the file and nothing is left in the tree), and the case passes. It must never do anything else: a run nobody asked to gate cannot be allowed to go red because the machine it ran on is slow.

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline --logger "console;verbosity=normal"
```

**Compare** - set `DATAPHOR_PERF_BASELINE` to a label that `tests/goldens/perf/perf-baseline.json` carries. Each measured scenario is compared with the number recorded under that label: past **3×** the recorded value the case warns, naming the scenario, both numbers and the factor; past **10×** it fails. A label the file does not carry fails with the command that would record it, because naming a label is an explicit request to gate and passing silently would be worse than failing. A scenario the file does not list, and a listed scenario the run did not measure, both fail: the two sets have to agree, or a scenario can stop being measured without anything going red.

```
DATAPHOR_PERF_BASELINE=dev-slow dotnet test tests/DAE.Server.Tests --no-build --filter Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline --logger "console;verbosity=normal"
```

**Record a new label, or re-record one** - set `DATAPHOR_UPDATE_PERF_BASELINE=1` as well. That label's entry is rewritten, every other label is left alone, and **the run then fails anyway**, exactly as `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` does for the derived-form goldens, so that an update can never be mistaken for a pass and its diff gets read before it is committed. The update variable without the label variable fails and says the label is required. A new entry's `note` is a placeholder for you to replace by hand with the machine class the label stands for and what else was running; a re-recording keeps whatever note it finds.

```
DATAPHOR_UPDATE_PERF_BASELINE=1 DATAPHOR_PERF_BASELINE=<label> dotnet test tests/DAE.Server.Tests --no-build --filter Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline
```

**A label, not a machine name, for two reasons.** Absolute times are not comparable across machines - the suite timings in the root `AGENTS.md` already show roughly a threefold spread between this project's fast and slow development machines, which is the whole of the 3× warning band - so a baseline is only meaningful on a machine it was recorded on, and whoever records one has to say which machine class it stands for. And this repository is public: a label the maintainer chooses (`dev-slow`, `ci-windows-latest`) says what is needed without putting a machine's name into a tracked file.

**3× and 10× are chosen policy, not measured thresholds.** 3× is meant to sit above the machine-to-machine and run-to-run spread so that a warning means something; 10× is meant to catch the kind of change that is wrong rather than slow. The way to tune them is to collect a few runs' numbers under one label - the run file is written on every run - and look at the spread, then move the two constants in `PerfBaselineTest` and say here why. They are not tuned from data today; the four runs above are all the data there is.

**CI does not compare.** The workflow sets neither variable, so every CI run records and passes. It will compare once someone records a baseline on a CI runner under a label of its own and sets `DATAPHOR_PERF_BASELINE` in `.github/workflows/build.yml`; that is a separate change and this document is where to say it has happened.

## The `Timings` blocks against their own 2008 notes, and what the drain costs by table size

`Timings` is the one scenario whose numbers can be read against a figure from outside this project: the script carries its 2008 measurements in a comment at the end. It times four blocks through the `BenchmarkExecute` operator, which is declared in the test-only D4 library `tests/libraries/TestFramework.Perf` (that library also lifts the in-memory device's 1,000-row cap, which the script's 10,000 rows need).

| Block | What it does | Measured | The script's own 2008 note |
|---|---|---|---|
| 1 | insert 10,000 rows, one row per statement | 663 ms | about 5,000 ms |
| 2 | update all 10,000 rows in one statement | 1,711 ms | about 9,500 ms |
| 3 | `while exists (T) do delete T return 1 by { ID };`, then drop the table | 1,346 ms | about 8,000 ms |
| 4 | 100,000 integer additions in a loop | 204 ms | about 650 ms |

Every block is now several times *faster* than the 2008 note records, which is what the `Timing` calibration above would predict. Block 3 was the exception until `bug-engine-delete-quota-scans-the-table`: the in-memory device would not serve an ordered scan from an index it already had to a statement that wanted a snapshot, and every `delete` wants one, so the engine sorted the whole table once per deleted row and the drain cost time proportional to the *square* of the table's size. The script needed about a quarter of an hour, and was a recorded known failure of the coverage pass against a short timeout rather than a measured scenario.

The drain, by table size, before and after that fix - the same statement on a table `T { ID : Integer }` on `System.Temp`:

| Rows | With the defect | Now |
|---|---|---|
| 250 | 397 ms | 46 ms |
| 500 | 1,885 ms | 50 ms |
| 1,000 | 7,866 ms | 66 ms |
| 2,000 | 34,308 ms | 134 ms |
| 4,000 | 150,879 ms | 245 ms |
| 10,000 | about a quarter of an hour, by extrapolation | 1,346 ms (block 3 above, which also drops the table) |

Each doubling of the row count used to cost about 4.4 times as much; now the cost per row is roughly flat - 0.19 ms at 250 rows, 0.066 at 1,000 and 0.13 at 10,000, the fall and then the rise being a drain's fixed costs spread over more rows and then the index getting deeper. Both columns were checked at two sizes by putting the device's old rule back on 2026-11-22: with it, drains of 250 and 1,000 rows cost 415 and 8,665 ms, which reproduces the left column; without it they cost 14-17 and 57-66 ms, below the right column because those runs were warmed by a throwaway drain first where the right column's each paid a first compile. `QuotaDeleteCostTest` in `tests/DAE.Server.Tests` is the regression test, and it asserts a *ratio* rather than a time: one row of a 1,000-row drain may cost at most three times what one row of a 250-row drain costs. With the defect that ratio is 5.2; without it, twelve runs gave 0.90 to 1.18 and one gave 1.60, which is why the bound is three and not two.

Deleting rows by key instead - `for LIndex : Integer := 1 to 2000 do delete (T where ID = LIndex);` - cost 130 ms for 2,000 rows even with the defect, and deleting a whole table in one statement cost 66 ms, because neither plan sorts: the cost was specific to the `delete <table> return <n>` form and never to per-row deletion or to the device.

## The benchmark program: engine start, a mix of database work, and the catalog store

Three costs are invisible to the timing scripts above, because those run inside an already-started server: how long an engine takes to come up, which is what every test, every tool invocation and every service restart pays; a mix of database work as one number that moves when the compiler or the in-memory device gets slower; and the catalog store on a first start against a fresh catalog as against a later start that loads an existing one. The benchmark program `DAE.Benchmarks` ([tests/Dataphor.Benchmarks](../tests/Dataphor.Benchmarks)) measures those four things. Its own [README](../tests/Dataphor.Benchmarks/README.md) says what each benchmark does, how to run it and why it is configured as it is; this section carries its numbers.

Nothing runs it automatically. `dotnet test` does not discover it (it is not a test project) and CI builds it and runs nobody, deliberately: a benchmark on a shared runner measures the runner.

| | |
|---|---|
| Label | `dev-slow` |
| Date | 2026-11-21 |
| Configuration | **Release**, .NET 10, Windows x64 |
| Machine | the same machine class as the table at the top of this document: one slow development laptop, nothing deliberately competing - the slower of the two classes the root [AGENTS.md](../AGENTS.md) quotes suite timings for. "Quiet" means no other work was started on purpose, not that the machine was idle |
| Measured by | `DAE.Benchmarks` with no arguments: all four benchmarks, one warmup iteration and ten measured iterations each, one invocation per iteration |
| Wall clock of the run | about 100 seconds (99 s, 104 s and 100 s on the three runs below) |

The table below is BenchmarkDotNet's own GitHub-markdown export, carried in by hand from `artifacts/benchmarks/results/DAE.Benchmarks.EngineBenchmarks-report-github.md`, the way [coverage-report.md](coverage-report.md) carries the coverage tool's output in by hand. `Mean` is the arithmetic mean of all ten iterations, `Error` half of the 99.9 % confidence interval and `StdDev` the standard deviation. The run's output also names the iterations it considers outliers; under `RunStrategy.Monitoring` BenchmarkDotNet reports them without removing them, so they are inside the means above (checked by adding up one benchmark's ten iterations by hand). The export's own environment header - the exact processor model, the Windows build and the SDK build - is **not** carried in, for the same reason this document uses a label rather than a machine name: the repository is public. It is in the run's log under `artifacts/benchmarks/`, which is git-ignored.

```
Toolchain=InProcessNoEmitToolchain  InvocationCount=1  IterationCount=10
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=1
```

| Method           | Mean       | Error     | StdDev    |
|----------------- |-----------:|----------:|----------:|
| EngineStart      | 2,759.4 ms | 606.49 ms | 401.15 ms |
| DdlDmlMix        |   180.5 ms |  77.02 ms |  50.94 ms |
| CatalogStoreCold | 2,808.9 ms | 592.47 ms | 391.88 ms |
| CatalogStoreWarm |   300.6 ms |  49.62 ms |  32.82 ms |

A second run of the same build, minutes later on the same machine, gave 2,785.1, 165.2, 2,477.0 and 273.0 ms for the same four, and a third run of the same build half an hour after that gave 2,719.8, 191.0, 2,675.9 and 268.4 ms - so run to run the means moved by up to 2 %, 16 %, 13 % and 12 %. Within a run the spread between iterations is wider still: `EngineStart` ranged from 2.24 to 3.16 seconds, and `DdlDmlMix` has the widest interval of the four relative to its own mean (its 99.9 % margin was 43 % of the mean in the table above and 57 % in the third run), so a change has to be large before it moves that number out of the noise. Read these numbers with the same caution the top of this document asks for; a later run on this machine coming in at 1.3 times one of them is not yet evidence of anything.

**What they say.** Starting an engine on a new instance directory costs about 2.8 seconds here, and starting one whose catalog already exists costs about 300 ms - so **about nine tenths of a cold start is work that a later start does not repeat**: creating the SQLite catalog and registering the system library. That is also why `EngineStart` and `CatalogStoreCold` come out within a few per cent of each other, which is what the two being the same measurement predicts. The warm number is the one `research-catalog-load-profile` extends to realistic catalogs; this is its empty-catalog case, where the only library registered is System.

**Every iteration of every benchmark succeeded**, in all three runs. None of them shows the pattern that would be expected if the engine's process-wide static state leaked from one iteration to the next - a first iteration much cheaper or much dearer than the rest, or a drift across the ten. The one-off costs a process pays on its first engine are paid by the warmup iteration, which is not in the numbers.

## What the benchmark numbers do not mean

**They are Release; the timing-script numbers at the top of this document are Debug.** The two sets must never be put in one table or compared with each other. That is not a nuance: a Debug build of this engine runs its interpreter at roughly the speed a 2008 release build ran on 2008 hardware, as the first section records.

**`EngineStart` and `CatalogStoreCold` are deliberately the same measurement, asked for by two different questions.** Starting an engine on a new instance directory *is* the catalog store's write path. Keep both: one is what somebody looks at when a test run feels slow, the other is what somebody looks at when the store is the suspect. **Do not read the gap between the two as a measurement of anything.** What `EngineStart` does and `CatalogStoreCold` does not - create two empty folders and construct the `Server` object - can only make it the dearer of the two, and yet across the three runs above the gap was −50 ms, +308 ms and +44 ms, changing sign: it is noise, not the setup cost. The only signal the pair carries is one of the two moving a long way while the other does not.

**`DdlDmlMix` is a judgement about what "representative" means, and the judgement can change.** It creates a table on the in-memory device, inserts 3,000 rows one at a time, updates them all in one statement, reads them all back through a cursor, empties the table in one statement and drops it. The D4 is in one named place in `EngineBenchmarks.cs` with the reasoning beside it. **A change to that script, or to the row count, invalidates every number recorded for it** - re-record, do not compare. The row count is not arbitrary either: below about three thousand rows an iteration is short enough that BenchmarkDotNet warns it cannot measure it well.

**Nothing here gates.** Unlike the timing scripts, these numbers have no machine-readable copy and no comparison step: a change that makes one of them much worse is caught by somebody running the program and reading this section, not by a red build. Giving them a checked-in baseline and a comparison, the way `PerfBaselineTest` has one, is a separate change and this is where to say it has happened.

## How to regenerate the benchmark numbers

```
dotnet build Dataphor.slnx -c Release
artifacts/bin/DAE.Benchmarks/release/DAE.Benchmarks.exe
```

Release, not Debug - the program says so on standard error if it was built in Debug. The run takes about 100 seconds and prints the table at the end; the same table, and the CSV and HTML versions, land under `artifacts/benchmarks/`, which is inside the git-ignored `artifacts/` folder on purpose, so a run leaves nothing in the tree. Copy the markdown table and the configuration line into the section above, and replace the head table's date, configuration and wall clock. Leave BenchmarkDotNet's environment header out, as above.

## The comparison with 3.1

A like-for-like comparison with Dataphor 3.1 needs an installed 3.1 server, so it is done **once, by hand, during the pilot**, and it is deliberately not automated: nothing in this repository builds or installs 3.1, and a comparison that cannot be reproduced by the suite does not belong in it.

The steps, for whoever runs the pilot:

- Install 3.1 beside the modern build on one machine, and run nothing else on that machine for the duration. 3.1 and the modern build keep their instance data apart (the modern server's instance directory is versioned), so both can be installed at once.
- Build this repository in **Release**, not Debug: the numbers in the table at the top of this document are Debug and must not be used for this comparison. Record a Release baseline under a label of its own first, so that the modern side of the comparison is a number this document carries.
- On each server in turn, through that server's own IDE or script runner, run the four `Timings` blocks as four separate `BenchmarkExecute` calls - the text is in `tests/libraries/TestFramework.Coverage.Scripts/Documents/Timings.d4` - and the `Timing` script. On the modern side the `BenchmarkExecute` operator comes from registering `TestFramework.Perf`; on 3.1 declare it with the same `create operator` that library's `Documents/Register.d4` holds, and lift the in-memory device's row cap the same way.
- Note the configuration of each server: the modern build in Release, and which 3.1 build and edition.
- Paste both sides' output into the table below, one row per block, and say in a sentence what the comparison showed.

| Scenario | 3.1 | This build (Release) | Ratio |
|---|---|---|---|
| | | | |

Results, once measured: *not yet run.*
