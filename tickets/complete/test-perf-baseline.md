description: A small benchmark program now measures three things the test suite cannot - how long the engine takes to start, how fast a representative run of database work is, and what the catalog storage costs on a first and a later start - and its numbers are published so that later slowdowns are visible.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/Dataphor.Benchmarks/DAE.Benchmarks.csproj, tests/Dataphor.Benchmarks/Program.cs, tests/Dataphor.Benchmarks/EngineBenchmarks.cs, tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs, tests/Dataphor.Benchmarks/Properties/AssemblyInfo.cs, tests/Dataphor.Benchmarks/README.md, Dataphor.slnx, Directory.Packages.props, docs/perf-baseline.md, docs/modernization-plan.md, docs/README.md, AGENTS.md
----
## What landed

A new program, `tests/Dataphor.Benchmarks` (assembly `DAE.Benchmarks`, `Exe` on `net10.0`), built with BenchmarkDotNet 0.15.8 on an in-process toolchain. It starts in-process Dataphor engines and measures four things:

| Benchmark | Measures | Published number (Release, slow machine class) |
|---|---|---|
| `EngineStart` | a fresh instance directory, `Start`, `Stop` - the whole cost of coming up | 2,759 ms |
| `DdlDmlMix` | create a table on the in-memory device, insert 3,000 rows one at a time, update them all, read them all back through a cursor, empty the table, drop it | 180 ms |
| `CatalogStoreCold` | a server started on a new instance directory, then stopped - the store's write path | 2,809 ms |
| `CatalogStoreWarm` | a server started on an instance directory a previous start populated - the store's read path | 301 ms |

A whole run takes about 100 seconds, so it is inside what a ticket can run rather than a by-hand-only step. It is in `Dataphor.slnx` under `/tests/` so that every build compiles it, is not a test project so `dotnet test` never discovers it, and is absent from `tools/layout/Layout.csproj` so nothing ships it. BenchmarkDotNet's own output goes to the already-git-ignored `artifacts/benchmarks/`.

`docs/perf-baseline.md` gained three sections for these numbers (the numbers with their machine and configuration head, what they do not mean, how to regenerate), and its existing timing-script sections were retitled so the two sources are told apart. The plan's `test-perf-baseline` work-item row is **Done**; `AGENTS.md` and `docs/README.md` gained a pointer each.

**The one result worth reading.** A cold start costs about 2.8 s and a warm one about 300 ms, so about nine tenths of a cold start is creating the SQLite catalog and registering the system library, not the engine's own startup. That is the empty-catalog case of what `research-catalog-load-profile` is meant to measure on realistic catalogs.

The implement stage is commit `ticket(implement): test-perf-baseline`; the review changed four things in that work, listed below.

## Review findings

The implement diff was read before the handoff. The program was built in both configurations, run in full once more (a third independent run, giving 2,719.8 / 191.0 / 2,675.9 / 268.4 ms in 100 s, every iteration of every benchmark succeeding and no `MinIterationTime` warning), run filtered, and run with every informational switch; `BenchmarkServerInstance` and `BenchmarkEnvironment` were read line by line against the `TestServerInstance` and `TestEnvironment` they copy; and the whole test suite was run in its three parts.

### Fixed in this pass

- **The program failed its own documented informational commands.** `--list`, `--help`, `--version` and `--info` make BenchmarkDotNet print something and run nothing, so the "nothing measured at all is a failure" guard fired on them: each printed a spurious `No benchmark ran. Check the filter: DAE.Benchmarks --list flat shows the names.` on standard error and exited **1** - including `--list flat`, which that very message recommends. A scripted caller or an agent following the README saw a failure. `Program.cs` now exempts those four switches from the guard; verified all four exit 0 and a filter matching nothing still exits 1. The explanatory comment moved down to the check it explains.
- **A published claim the measurements contradict.** `docs/perf-baseline.md` and the program's README both said the gap between `EngineStart` and `CatalogStoreCold` *is* `EngineStart`'s directory setup, and that the two ceasing to agree would be worth finding out about. It cannot be: across the three runs now on record the gap was -50 ms, +308 ms and +44 ms - it changes sign, while the extra work (creating two empty folders and constructing the `Server` object) can only be a cost. Both documents now say plainly not to read the gap as a measurement of anything, and that the only signal the pair carries is one of the two moving a long way while the other does not.
- **The record was two runs where three were available.** `docs/perf-baseline.md` now carries the third run's means, the corrected run-to-run spreads (up to 2 %, 16 %, 13 % and 12 %) and the fact that `DdlDmlMix` has the widest confidence interval of the four relative to its own mean (43 % of the mean in the tabulated run, 57 % in the third), so a reader knows how large a change has to be before that number moves out of the noise. The "every iteration succeeded in both runs" claim became all three.
- **Dead code.** `BenchmarkServerInstance.Directory` was public and unused. Removed - and worth removing rather than leaving for the eventual shared-helper extraction, because a property of that name inside a type whose file otherwise calls `System.IO.Directory` statically is a shadowing trap for the next edit.

### Checked and sound

- **It really is compiled and really is not run.** `dotnet build Dataphor.slnx` in both configurations: 0 errors, and 0 warnings from this project in Release - no `NU1902` from the `CoreWCF.NetTcp` dependency chain (the package reference plus `<FrameworkReference Include="Microsoft.AspNetCore.App" />` are there for that) and no `CS8002` (BenchmarkDotNet is strong-named). Suite part 1 is `dotnet test Dataphor.slnx` and reported "a total of 1 test files matched" per test project - two test assemblies, not three - so `IsTestProject=false` does what the project file's comment claims.
- **Nothing ships it and nothing litters.** `artifacts/layout/release/bin` holds no `DAE.Benchmarks` or `BenchmarkDotNet` file. After a full run plus a filtered run plus five informational runs, `git status` was clean and `%TEMP%\Dataphor.Benchmarks` was empty - the stale-directory sweep and the process-exit hook both work.
- **The instance helper is a faithful copy.** `BenchmarkEnvironment`'s layout lookup, run-directory creation, stale sweep, `IsRunning`, `RandomHex`, `NewDirectory` and the copy helpers match `TestEnvironment` statement for statement, minus the `DATAPHOR_TEST_LOGGING` keep-files path (which a benchmark should not have) and the `TestContext.Progress` writes (which are why it is a copy at all). The object-id counter is captured per instance and restored in `Dispose`, which nests correctly: `CatalogStoreWarm`'s template is created first and disposed last, so the restores unwind in order.
- **The measured regions are the right size.** Only `EngineStart` has its instance creation inside the timed body, by design; the mix's server start and row-cap lift, the cold benchmark's directory creation and the warm benchmark's directory copy are all in `[IterationSetup]`, which BenchmarkDotNet does not time. The mix's cursor loop counts its rows and raises if the count is wrong, so an iteration that silently did less work fails instead of publishing a faster number.
- **The outlier claim.** `docs/perf-baseline.md` says that under `RunStrategy.Monitoring` BenchmarkDotNet reports outliers without removing them, so they are inside the published means. Confirmed independently: the third run detected two outliers in `DdlDmlMix` and still reported `N = 10`.
- **The 3,000-row choice.** The third run's `DdlDmlMix` minimum was 153.6 ms, clear of the 100 ms floor the row count was chosen for, and no run produced a `MinIterationTime` warning.
- **No migration note is owed.** Nothing public changed: no type or member an extension uses, no D4 semantics or operator signature, no registered class name, no file format, no contract shape, no port, no on-disk layout. Nothing ships this program.
- **No new test, and that is right here.** The project's testing rule asks for a characterization test from a ticket that ports or changes a subsystem's behaviour. This one changes no engine behaviour; it is a program outside the suite driving paths the suite already covers far harder. Its verification is the run itself, and three full runs now stand behind the numbers.
- **The accepted-tradeoff and tripwire notes the implementer parked are all at their sites and all read correctly** (the duplication of `TestServerInstance` with its "if a third consumer appears" revisit condition, the process-id-reuse note on the sweep, the iteration-count/run-cost note in `Program.cs`, and the "nothing gates on these numbers" note in `docs/perf-baseline.md`).

### Tripwires recorded

None new. The four the implementer parked cover what this pass would have wanted parked, and the two documentation corrections above replaced a wrong claim rather than adding a conditional one.

### Not filed as tickets

Nothing reached the filing bar. The one design question worth a second opinion - whether `EngineStart` and `CatalogStoreCold` both earn their 55 seconds of the run when they measure the same thing to within noise - is now stated honestly in both documents rather than argued away, and dropping one is a judgement for whoever next finds the run too slow, which the existing iteration-count note already points at. Taking on a third-party dependency (BenchmarkDotNet) for four numbers is a real cost; the project file and README both carry the reasoning, and this pass found nothing that weakens it.

## Tests

Full suite, built then run in the three parts the root `AGENTS.md` prescribes: **693 passed, 0 failed, 24 skipped** (13 `Common.Tests` + 564 part 1, 67 part 2, 49 part 3; the 24 skips are the coverage manifest's 21 known failures and 3 fragments), at 5 m 37 s, 4 m 15 s and 6 m 23 s. No pre-existing failures surfaced, so `tickets/.pre-existing-error.md` was not written. The review's changes touch only `DAE.Benchmarks` sources and two documents, none of which any test loads, so the suite was not re-run after them; both configurations were rebuilt (0 errors) and the program re-exercised in full.
