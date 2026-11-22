description: The project has no record of how fast the engine is, so a change that makes it slower goes unnoticed. This adds the missing operator that the two existing timing scripts need, measures them on every test run, and publishes the numbers so a later slowdown shows up as a difference instead of a hunch.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Perf/TestFramework.Perf.d4l, tests/libraries/TestFramework.Perf/Documents/Register.d4, tests/DAE.Server.Tests/PerfBaselineTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/CoverageScripts.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/goldens/perf/perf-baseline.json, tests/libraries/TestFramework.Coverage.Scripts/Documents/Timing.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/Timings.d4, src/engine/DAE/Schema/SystemCatalog.d4, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, docs/perf-baseline.md, docs/README.md, AGENTS.md
difficulty: medium
----
## What this is

Two D4 scripts in the coverage corpus exist only to be timed: `Timing` runs a ten-million-iteration counting loop, and `Timings` times four blocks (inserting 10,000 rows, updating them, deleting them one at a time, and 100,000 additions) through an operator called `BenchmarkExecute`. Neither asserts anything; their value is the elapsed time. Nothing currently reads those times, and `Timings` does not even run: the `create operator` line that would define `BenchmarkExecute` is commented out in the script itself (`Timings.d4` line 6), so the script fails to resolve the operator and the coverage manifest records it as a known failure owned by this work.

This ticket makes `Timings` run, captures both scripts' numbers on every test run, compares them against a checked-in baseline when the run asks for that comparison, and publishes the numbers and the procedure in `docs/perf-baseline.md`. The benchmark project the plan also asks for is the follow-on ticket `test-perf-baseline`, which depends on this one.

## The operator, and where it lives

The engine still has the node: `SystemBenchmarkExecuteNode` in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` (around line 2421). It runs a script and returns the elapsed time as a `TimeSpan`. It needs no C# change. Every `PlanNode` subclass in `DAE.dll` is registered automatically under the name `System.<type name>` (`src/engine/DAE/Register.cs`), so `System.SystemBenchmarkExecuteNode` is already a resolvable D4 class name; nothing new is registered and no compatibility surface moves.

What is missing is a D4 `create operator` that binds a name to that class. **Put it in a new D4-only test library, `tests/libraries/TestFramework.Perf/`**, rather than in `TestFramework.Coverage.Base`. The helper library is registered by the coverage template for *every* script, so a `create operator` there would run on all 140-odd script servers and any failure in it would fail every script case; the new library is registered only by the entries that ask for it. The script file itself is not touched: coverage scripts are the record of correct behaviour and are never edited.

The library needs two files and no C# project:

- `TestFramework.Perf.d4l` — a descriptor in the shape of `tests/libraries/TestFramework.Coverage.Scripts/TestFramework.Coverage.Scripts.d4l`: one `libraries.libraryreference` for `System`, no `files.filereference` (there is no assembly). `tools/layout/Layout.csproj` globs `tests/libraries/*/*.d4l`, so the library joins the runtime layout with no edit to that file; it must stay out of the project's `Library` reference list, which is only for libraries that have a C# project.
- `Documents/Register.d4` — the `create operator`. A library's `Documents/Register.d4` is run on registration with the current library set to that library (`src/engine/Server/Schema/LibraryUtility.cs`, around line 537).

**The declaration must carry the determinism tags, and the commented-out line in `Timings.d4` does not.** A `create operator` defaults `IsLiteral`, `IsFunctional`, `IsDeterministic` and `IsRepeatable` all to `true` (`Schema.Operator.InternalInitialize`, `src/engine/DAE/Schema/Operator.cs` around line 181). `IsLiteral` means compile-time evaluable, and every call in `Timings.d4` passes a string *literal* — so a tagless declaration invites the compiler to evaluate the benchmark while compiling, and the deterministic and repeatable flags invite the plan cache and common-subexpression elimination to reuse one measurement. Copy the tag set the engine's own `Execute` operator uses in `src/engine/DAE/Schema/SystemCatalog.d4` (around line 481):

```
create operator BenchmarkExecute(const AString : String) : TimeSpan
	class "System.SystemBenchmarkExecuteNode"
	static tags { DAE.IsLiteral = "False", DAE.IsRemotable = "False", DAE.IsFunctional = "False", DAE.IsDeterministic = "False", DAE.IsRepeatable = "False" };
```

Unqualified resolution works across libraries: the operator-resolution path is the current library's required-library closure plus `System`, but when nothing matches there the engine falls back to resolving against every signature in the catalog and succeeds if the name is unambiguous (`OperatorMaps.ResolveCall`, `src/engine/DAE/Compiling/OperatorMap.cs` around line 78). That is already how every coverage script calls `TestCatalog()` and `TestNavigable(...)` out of `TestFramework.Coverage.Base` while declaring only `System` as a requirement. So `Timings` resolves `BenchmarkExecute` without the scripts library's descriptor changing.

## Flipping the manifest

`CoverageScripts.Libraries` (`tests/DAE.Server.Tests/CoverageScripts.cs`, around line 37) is the fixed list of library folders copied for every script's server, and the manifest validator rejects a `register` value that is not in it (`CoverageManifest.Validate`), so add `"TestFramework.Perf"` there. The folder is two small text files, so the copy cost per script server is negligible.

Then in `tests/DAE.Server.Tests/coverage-manifest.json` (entries stay sorted by script name in ordinal order, strict JSON, one entry per line):

- `Timings` becomes `"status": "pass"` with `"register": ["TestFramework.Perf"]`, an explicit `"timeoutSeconds"`, and a `note` saying that the operator comes from the perf library, that the script asserts nothing and exists to be timed, and what the script's measured wall clock was. The `ticket` and `error` fields must go: the validator forbids both on a `pass` entry.
- `Timing` stays `"status": "pass"` and gains a `note` saying the same two things — that it asserts nothing and that `PerfBaselineTest` is what reads its time.

**Set the timeout from a measurement, and escalate if it is large.** The default is 120 s. Measured on 2026-11-22 on one slow development laptop in Debug, the `Timing` coverage case took **23 s** end to end (copying the template instance, starting the server, running the script, stopping the server — the script's own share is not separated out), the template build took 9.4 s, and the `Timings` case took 2 s because it fails at once on the missing operator. `Timings`' own cost is therefore unmeasured until the operator exists: its four blocks are an insert, an update and a one-row-at-a-time delete of 10,000 rows plus 100,000 additions, and the script's own 2008-era comments record roughly 5 s, 9.5 s and 8 s for the three table blocks. Measure it first (`dotnet test tests/DAE.Server.Tests --no-build --filter Name=Timings --logger "console;verbosity=normal"`), set `timeoutSeconds` to roughly four times what you measure, rounded up, and record the measurement in the `note`.

If the script turns out to take more than **240 s**, stop: a four-minute script does not belong in a suite whose slowest part is already about 4 minutes, and the choice between moving the script out of the coverage corpus and leaving it in becomes a real decision. File that to `blocked/` with the measurement and the two options, and leave the entry as it is.

Watch the suite budget either way. `Timings` lands in part 3 of the three-part agent run (the `[^A-Ra-r]` part, about 4 minutes), and `PerfBaselineTest` lands in part 1 (about 5.5 minutes, the part the root `AGENTS.md` already calls the one to watch). Re-measure both parts after the change and bring the figures in `AGENTS.md` up to date; if a part passes about 8 minutes, split it further by letter as that section says.

## The perf fixture

A new `tests/DAE.Server.Tests/PerfBaselineTest.cs`, in the shape of `FrontendDerivationGoldenTest`: **one fixture, one test method** that loops a table of scenarios, collects every number, writes the record once and reports once. That shape is what the goldens pass already uses and it keeps the file write atomic.

Derive the fixture from `InProcessTestFixture` (`tests/DAE.Server.Tests/InProcessTestFixture.cs`): one server for the fixture, `Libraries` naming the folders to copy (`TestFramework.Coverage.Scripts` for the script text, `TestFramework.Perf` for the operator, plus whatever those two require) and `LibrariesToRegister` naming `TestFramework.Perf`.

The fixture reads each script's text from its own server's copy of the scripts library — the same place the coverage runner's `batches` path reads it, `<instance>/Libraries/TestFramework.Coverage.Scripts/Documents/<script>.d4` — and runs it through `IServerProcess.PrepareScript`, batch by batch, which is how Dataphoria runs a script and what the manifest's `batches: true` path is. That buys the per-block numbers:

- A **statement** batch (`ServerBatch.IsExpression()` false, `src/engine/DAE/Server/ServerBatch.cs`): time the `Execute` call with a `Stopwatch`. `Timing` is one such batch.
- An **expression** batch whose value is a `TimeSpan`: that is a `select BenchmarkExecute(...)`, and its value *is* the measurement — record it rather than the wall clock. `Timings` has four such batches. Any other expression batch falls back to wall clock. Note that `ServerBatch.Execute` disposes the value it evaluates; a fixture that evaluates the plan itself has to dispose what it reads.

So the scenario keys are `Timing` (the script's wall clock), `Timings` (the script's wall clock) and `Timings#1` … `Timings#4` (the four `BenchmarkExecute` values, keyed by batch ordinal — not by line number, which would move if a script were ever reformatted). Record milliseconds as integers: sub-millisecond precision is meaningless at this scale.

Adding a scenario later — `research-catalog-load-profile`, named inside `tickets/backlog/feat-ws14-research-precompilation.md`, is the next one — must be one row in that table, not a second fixture.

**Accept, and record, that the two scripts now run twice per suite** — once through the coverage runner, which checks that they do not raise, and once here, which also checks that they do not raise and additionally captures the numbers. Leave an accepted-tradeoff `NOTE:` at the scenario table saying so: the alternatives were to give the measurement to the coverage runner (which would put a gating concern inside the suite's most load-bearing fixture and force it to reason about the three-part split measuring only some scenarios) or to move the two scripts out of the coverage corpus (which would shrink the audited script set and change the counts published in `docs/coverage-report.md`). Paying well under two minutes of suite time was judged cheaper than either. Revisit if the perf scenario table grows enough that the duplication is minutes rather than seconds.

## Recording and comparing

Three behaviours, selected by two environment variables. Put the comparison itself in a small pure method — it takes the measured numbers, a baseline record and the two factors, and returns the findings — so that it can be tested without a server.

**Default (neither variable set): record and pass.** Write the run's numbers to `artifacts/perf/perf-run.json` (`artifacts/` is git-ignored, so a CI job can upload it and nothing litters the tree), print them in the test output, and pass. This is the "a baseline is recorded on .NET 10" half of the plan's requirement and it must never fail a run.

**`DATAPHOR_PERF_BASELINE=<label>`: compare.** Read `tests/goldens/perf/perf-baseline.json`, find the entry whose label matches, and compare each measured scenario with its recorded value. Past **3×** the recorded value, `Assert.Warn` with the scenario, both numbers and the factor; past **10×**, `Assert.Fail`. A label with no entry in the file fails with the message that says how to record it — the variable is an explicit request to gate, so silently passing would be worse than failing.

A label rather than the machine name, for two reasons. Absolute times are not comparable across machines — the figures in the root `AGENTS.md` already show roughly a threefold spread between the fast and slow development machines, which is the whole of the 3× band — so a baseline is only meaningful on a machine it was recorded on, and whoever records one has to say which machine class it stands for. And this repository is public: a label the maintainer chooses (`dev-slow`, `ci-windows-latest`) says what is needed without putting a machine's name in a tracked file. CI therefore does not compare until someone records a CI baseline and sets the variable in `.github/workflows/build.yml`; leave the workflow alone in this ticket and say so in `docs/perf-baseline.md`.

**3× and 10× are chosen policy, not measured thresholds.** 3× is meant to sit above the machine-to-machine and run-to-run spread so that a warning means something; 10× is meant to catch the kind of change that is wrong rather than slow. Name them as constants, document both in `docs/perf-baseline.md` with that reasoning, and say there that the right way to tune them is to collect a few runs' numbers on one label and look at the spread.

**`DATAPHOR_UPDATE_PERF_BASELINE=1` together with `DATAPHOR_PERF_BASELINE=<label>`: rewrite that label's entry** in `tests/goldens/perf/perf-baseline.json`, leaving the other labels untouched, and then **fail the run anyway**, exactly as `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1` does, so that an update can never be mistaken for a pass and the diff gets read before it is committed. The update variable without the label variable fails with a message saying the label is required.

The file goes under `tests/goldens/` because that tree is already "the checked-in files the golden passes compare generated output with" and `TestEnvironment.GoldensDirectory` already resolves it (and checks that it is inside the repository). `FrontendDerivationGoldenTest.GoldensFolderName` is the precedent for a pass having its own subfolder; this one is `perf`. Do not embed the file as a resource the way `coverage-manifest.json` is embedded: an embedded resource cannot be rewritten by an update run.

Shape, with one entry per label:

```json
{
  "schema": 1,
  "baselines": [
    {
      "label": "dev-slow",
      "configuration": "Debug",
      "recorded": "2026-11-22",
      "note": "one slow development laptop, nothing else running",
      "scenarios": { "Timing": 23000, "Timings": 0, "Timings#1": 0, "Timings#2": 0, "Timings#3": 0, "Timings#4": 0 }
    }
  ]
}
```

Record the real numbers from an update run on this machine, in Debug; the zeros above are placeholders, not values to commit. A scenario in the file that the run did not measure, and a measured scenario the file does not list, are both findings to report — the two sets must agree, as they do for the goldens.

## The published document

A new `docs/perf-baseline.md`, modelled on `docs/coverage-report.md`: a hand-written head saying where the numbers came from (date, commit, configuration, machine class, what else was running) and how to read them, then the numbers, then how to regenerate them. Add one line for it to the *Project documents* list in `docs/README.md`, beside the coverage report (around line 120) — `tools/docs-link-check/check_links.py` must stay clean.

It carries four things:

- **The numbers**, per scenario, from the recorded baseline, with the caution that an absolute time is comparable only with another run on the same machine class and configuration.
- **How to regenerate and how to compare** — the two environment variables, the two factors and why they are what they are, and the fact that an update run always fails.
- **What these numbers do not mean.** They are wall clock on a Debug build of one machine; they are a tripwire for a large regression, not a performance measurement of the product.
- **The 3.1 comparison, as a written procedure, not automated.** The plan is explicit that a like-for-like comparison against 3.1 needs an installed 3.1 server and is done once, by hand, during the pilot. Write the steps so the pilot's operator can follow them: install 3.1 beside the modern build, run the same four `Timings` statements against each, and paste both outputs into a section of this document. Leave that section as a stub with the steps and an empty results table.

Hold the whole document to the project's prose rules: no coined vocabulary, no hard wrapping (one paragraph per line).

**No migration note.** A new test library, a new test fixture and a checked-in baseline file are not things an application, a deployment script or a custom C# extension can notice: no public type or member changes, no D4 semantics, no file format, no contract. `TestFramework.Perf` does land in the development runtime layout's `Libraries/` folder, as every test library does, and nothing ships that folder.

## Edge cases & interactions

- **The operator is registered only under the perf library, deliberately.** An engine run that does not register `TestFramework.Perf` still sees `BenchmarkExecute` missing, so a stray run of `Timings` somewhere else fails visibly rather than quietly measuring nothing. Verified by the manifest: the `register` field is what makes the script pass, and removing it would put the known failure back.
- **The determinism tags are the one thing that can silently falsify every number.** With `IsLiteral` left at its default, a measurement can be taken at compile time and reused. Verify by inspection of the `create operator` against `SystemCatalog.d4`'s `Execute`, and at run time by the numbers themselves: four `BenchmarkExecute` values that are identical to the tick, or a near-zero value for a block that inserts 10,000 rows, means the call was folded or cached. Say in the handoff what the four values were.
- **`Timings` leaves a table behind.** Its blocks create `table T` and the third block drops it, but a block that fails part way through leaves `T` in the server. The perf fixture's server is shared across its test method's scenarios, so a failed run must not poison a later one: either run each script on a server of its own, or report the failure and stop rather than continuing with a dirty catalog. The coverage runner is unaffected — every script there gets a fresh server copied from the template.
- **Script-only timing excludes server start.** The fixture measures batches, not instance setup, so its numbers do not include the seconds spent copying library folders and starting the engine — unlike the 23 s figure measured for the whole coverage case. Say which is which wherever a number is published, or the two sets of figures will be compared with each other.
- **A filtered run measures a subset.** `dotnet test --filter Name=PerfBaselineTest` and the three-part split both run the fixture whole, because it is one test method, so the measured set is always the full scenario table. If a later ticket turns the table into one case per scenario, the write-and-compare step has to learn that a run may hold only part of the set; note that where the table is defined.
- **Partial failure of a batch.** A batch that raises must fail the case with the script name, the batch ordinal and the error, not be silently recorded as a time. An expression batch whose value is not a `TimeSpan` falls back to wall clock rather than failing — `Timing`'s single statement batch is the normal case for that path.
- **Two engines in one process share engine static state** (the note on `TestServerInstance`, and `bug-object-id-counter-is-process-wide`). The fixture must not be `[Parallelizable]`, like every other fixture here that starts a server.
- **The baseline file is written by a test.** `TestEnvironment.GoldensDirectory` already refuses a path outside the repository, which is what keeps a copied test assembly from writing into someone else's tree; rely on that rather than composing the path by hand.
- **Strict manifest validation.** The validator forbids `ticket` and `error` on a `pass` entry, rejects an out-of-order entry, and rejects a `register` value that is not in `CoverageScripts.Libraries`. A mistake here fails as one `Harness.ManifestInvalid` case rather than as a script failure, so read that message first if the run goes red.

## TODO

- Add `tests/libraries/TestFramework.Perf/` — the `.d4l` descriptor and `Documents/Register.d4` with the tagged `create operator`. Confirm the library appears in the built layout's `Libraries/` folder.
- Add `"TestFramework.Perf"` to `CoverageScripts.Libraries`.
- Measure `Timings` with the operator in place, then flip its manifest entry to `pass` with `register`, an explicit `timeoutSeconds` and a note carrying the measurement; add the note to `Timing`. Escalate to `blocked/` if the script exceeds 240 s.
- Add `tests/DAE.Server.Tests/PerfBaselineTest.cs`: the scenario table, the batch-by-batch measurement, the run file under `artifacts/perf/`, the three behaviours and the accepted-tradeoff `NOTE:` about the duplicated runs. Register the file in `DAE.Server.Tests.csproj` (the project lists its sources explicitly).
- Add one test for the comparison method — a fabricated baseline record and the four outcomes (inside tolerance, warn band, fail band, unknown label). No server; that is the lowest layer that shows the behaviour.
- Record the baseline on this machine with `DATAPHOR_UPDATE_PERF_BASELINE=1 DATAPHOR_PERF_BASELINE=dev-slow`, read the diff, and commit `tests/goldens/perf/perf-baseline.json`.
- Write `docs/perf-baseline.md` (head, numbers, how to regenerate, what the numbers do not mean, the 3.1 comparison procedure as a stub) and add its line to `docs/README.md`.
- Add `DATAPHOR_PERF_BASELINE` and `DATAPHOR_UPDATE_PERF_BASELINE` to the list of variables the suite reads in the root `AGENTS.md`, and bring the part-1 and part-3 timings there up to date from a re-measurement.
- Build, then run the three suite parts as the root `AGENTS.md` describes, each as its own foreground command. Report the part timings, the four `BenchmarkExecute` values and the recorded baseline in the handoff.
