# The benchmark program

A console program that measures what the test suite's timing scripts cannot, and publishes the numbers to [docs/perf-baseline.md](../../docs/perf-baseline.md) so that a later change which makes the engine much slower shows up as a difference rather than as a hunch. Like those scripts it is a tripwire, not a performance measurement of the product: nothing gates on these numbers.

The timing scripts (`Timing` and `Timings`, measured by `PerfBaselineTest` in [tests/DAE.Server.Tests](../DAE.Server.Tests)) run inside an already-started server, so three costs are invisible to them. Those are what this program measures.

| Benchmark | What it measures |
|---|---|
| `EngineStart` | The whole cost of bringing an engine up the way a test case or a tool invocation does: a fresh instance directory, `Start`, `Stop`. The number to look at when a test run feels slow. |
| `DdlDmlMix` | A representative mix of database work on the in-memory device, on a server started outside the measured region: create a table, insert rows one at a time, update them all in one statement, read them all back through a cursor, empty the table and drop it. One number for the compiler and the memory device together. |
| `CatalogStoreCold` | The catalog store's write path: a server started on a new instance directory, so that the SQLite catalog is created and the system library registered, then stopped. |
| `CatalogStoreWarm` | The catalog store's read path: a server started on an instance directory a previous start already populated, then stopped. This is the number `research-catalog-load-profile` will extend to realistic catalogs. |

`EngineStart` and `CatalogStoreCold` overlap on purpose — starting an engine on a new instance directory *is* the store's write path — because they answer two different questions. The difference between the two numbers is `EngineStart`'s directory setup, which is inside its measured region and outside the other's.

`EngineBenchmarks.cs` holds the four measurements and the D4 the mix runs; the D4 is in one named place so that the next person to change it can see what the published numbers were measured against. **A silent change to the mix, or to its row count, invalidates every recorded number for it**: re-record, do not compare.

## Running it

Build the solution and run the program. **In Release**: a benchmark on a Debug build measures the wrong thing, and the numbers in `docs/perf-baseline.md` are kept in separate tables per configuration for that reason. A Debug build says so on standard error before it runs.

```
dotnet build Dataphor.slnx -c Release
artifacts/bin/DAE.Benchmarks/release/DAE.Benchmarks.exe
```

With no arguments it runs every benchmark. Otherwise it takes BenchmarkDotNet's own command line, so one benchmark is:

```
artifacts/bin/DAE.Benchmarks/release/DAE.Benchmarks.exe --filter '*CatalogStore*'
```

It exits non-zero if any benchmark's iterations failed, if a validator reported a critical error, or if nothing ran at all — a filter that matches nothing would otherwise look like a clean run.

**What a run costs.** Measured on 2026-11-21 in Release on the slower of the two machine classes the root [AGENTS.md](../../AGENTS.md) quotes suite timings for: **about 100 seconds** for all four benchmarks (99 s and 104 s on two runs). That is inside the roughly ten minutes the root `AGENTS.md` calls the limit of what an agent can run in a ticket, so it does not have to be a by-hand-only step — but mind that it is 100 seconds of near-silence, and raising the iteration count raises it proportionally.

**Where its output goes.** BenchmarkDotNet writes its log and its exports (markdown, CSV, HTML) to `artifacts/benchmarks/`, not to the `BenchmarkDotNet.Artifacts/` folder it would otherwise create beside the working directory: `artifacts/` is already git-ignored, so a run leaves nothing in the tree and nothing new has to be ignored. `artifacts/benchmarks/results/DAE.Benchmarks.EngineBenchmarks-report-github.md` is the markdown table that gets carried into `docs/perf-baseline.md` by hand, the way `docs/coverage-report.md` carries the coverage tool's output in by hand.

## Why it is configured the way it is

**It is in `Dataphor.slnx`, so it is compiled.** A benchmark project left out of the solution is built by no command anyone runs and rots inside a release. `dotnet build Dataphor.slnx` and CI therefore compile and type-check it.

**Nothing runs it automatically, and that is intended.** It is not a test project — no `Microsoft.NET.Test.Sdk`, no NUnit, and `IsTestProject` is set to false outright so that stays true however the SDK's defaults change — so `dotnet test` neither discovers nor runs it. CI builds it and runs nobody. Take the absence of a CI job here for a decision, not an oversight: a benchmark on a shared runner measures the runner.

**An in-process toolchain (`InProcessNoEmitToolchain`), and not optional.** By default BenchmarkDotNet generates a project per benchmark and builds it with the SDK. A generated project under this repository inherits `Directory.Build.props`, which switches the SDK's default compile items off (uncompiled source files sit beside compiled ones in this codebase) and redirects `ArtifactsPath` — so the generated project compiles nothing and the run fails in a way that looks like a BenchmarkDotNet defect. An in-process toolchain generates and builds nothing. It costs nothing here either: every iteration starts a Dataphor engine, so process-level isolation would buy little, and the engine has process-wide static state in any case (see the object-id counter in `BenchmarkServerInstance`).

**`RunStrategy.Monitoring` with stated iteration counts.** BenchmarkDotNet's default job is built for operations measured in microseconds: it runs a pilot to find how many invocations fit in a time slice, then many warmup and measurement iterations. Against an operation that costs seconds — starting a server is seconds — the default job would run for a very long time and tell us nothing extra. `Program.cs` therefore states one warmup iteration, ten measured iterations and one invocation per iteration, with the reasoning beside each.

**Every iteration gets a server of its own.** Sharing one across iterations would measure a warm engine in one iteration and a cold one in the next. `BenchmarkServerInstance` gives each instance a fresh temporary directory under `%TEMP%\Dataphor.Benchmarks`, with *copies* of the library folders it needs — never the runtime layout itself, which a running server would write into — and hands the process its object-id counter back when the instance is disposed. It duplicates a part of the test suite's `TestServerInstance` on purpose; the reasons and the condition for extracting a shared helper instead are in a `NOTE:` at the top of that type.
