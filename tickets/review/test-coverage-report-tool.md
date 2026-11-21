description: Review a new checked-in tool that turns the test run's code-coverage output into a readable report of which parts of the engine the tests actually exercise, so the untested parts can be found by running a command instead of by reading a 24 MB XML file by hand.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/areas.json, tools/coverage-report/test_coverage_report.py, tools/coverage-report/README.md, AGENTS.md, tickets/backlog/debt-ws12-hygiene-remainder.md
difficulty: medium
----
## What landed

Half of the plan's WS3 work item `test-coverage-gap-analysis` - the half that needs no long test run. The plan's WS3 cross-cutting requirements ask for "line coverage reported (not gated) per assembly"; this is the reporting tool and the map that gives the numbers meaning. The sibling ticket `test-coverage-gap-analysis` (still in `implement/`) runs the whole suite under coverage collection, publishes `docs/coverage-report.md` from it and files one ticket per gap.

Four new files in `tools/coverage-report/`, mirroring `tools/docs-link-check/`: Python 3, standard library only, one module, one test file, one README, plus the map as JSON. Two edited files: one paragraph in `AGENTS.md` and one arm appended to the hygiene umbrella in `backlog/`. **No compiled source changed**, so the three suite parts were not run here (see *What was and was not validated*).

- `coverage_report.py` - reads and merges Cobertura reports, applies the map, classifies each area, emits Markdown, implements the two boundary checks and the three exit codes.
- `areas.json` - the three assembly lists (`engine`, `reported`, `ignored`), 21 areas and 2 exclusions.
- `test_coverage_report.py` - 8 test methods over the 7 behaviours the implement ticket named.
- `README.md` - what it is, the collect command, the run command, the thresholds, the two boundary checks and their exit codes, how to add an area, and what the numbers do and do not mean.

Nothing was installed. `Microsoft.NET.Test.Sdk` 18.11.0, which every test project here already references, brings Microsoft's own coverage collector, so `--collect:"Code Coverage;Format=cobertura"` on a normal `dotnet test` is the whole collection story. No `coverlet` package, no global tool, no project change. No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension could notice changes.

## How to run it, and what a good run looks like

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=Lexer|Name=Types" --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/sanity
python tools/coverage-report/coverage_report.py artifacts/coverage/sanity
python -m unittest discover -s tools/coverage-report
```

That is the end-to-end path this ticket validated. The report from those two test cases (2026-11-21, Debug, the slow machine) gave:

```
1 report merged, 5 of 6 engine assemblies measured, 21 areas, 11 gaps, 0 unassigned files, 0 unlisted assemblies
```

with the engine total at 34.5 % (34,905 of 101,213 coverable lines) and the worst areas, by uncovered line count: Plan nodes 21.8 % of 24,603, Runtime values 25.1 % of 10,408, D4 language 46.9 % of 12,291, Application transactions 2.7 % of 1,603, RealSQL 0.0 % of 1,423, Streams 4.2 % of 1,290. Those figures are from a **two-case run** and are useful only as the shape of the output - they are not a coverage result for this project and must not be quoted as one. The real numbers come from the sibling ticket's full run.

### Use cases a reviewer can exercise

- **The ordinary run.** Collect over any short filter, point the tool at the results directory, read the Markdown. Exit 0, a report on standard output, findings none.
- **Several parts merged.** Collect twice into two directories and pass both. Exit 0, the header says "Merged from 2 coverage reports", and the numbers are the union. Done here with two real reports of the same build: 76+ classes common to both, no line-set disagreement.
- **A stale report mixed with a fresh one.** Rebuild something, collect again into the same tree without deleting the old directory, and run the tool over both. It must print `line sets differ: <assembly>/<class>` and **no report**, exit 1.
- **A new source folder.** Add a `.cs` file in a new folder of an engine project, build, collect, run. It must print `unassigned: <path> (<n> lines)` and no report; with `--allow-unassigned` the report prints and the exit code is still 1. The fix is a line in `areas.json`.
- **A newly converted project.** Same shape, reported as `unlisted assembly: <name> (<n> lines)`.
- **Writing a file.** `-o report.md --json rollup.json`. Both are UTF-8 with CRLF line endings; the JSON carries no timestamp or absolute path, so two runs of the same build diff to nothing.
- **Bad arguments** exit 2: a path that does not exist, `--gap-below` above `--thin-below`, a missing or malformed `--areas` file, an input that is not a Cobertura document.

## Tests added, and what each one pins

In `tools/coverage-report/test_coverage_report.py`, against Cobertura fixtures the test writes (a real report is about 24 MB - far too large to check in). All 8 pass.

| Test | What it verifies |
|---|---|
| `MergeTest.test_a_line_covered_in_either_report_is_covered` | The union merge: a line hit in only the second report counts as covered, and a class only one report saw survives the merge. |
| `MergeTest.test_reports_of_different_builds_are_refused` | A class whose line-number set differs between two inputs exits 1, names the class, and prints no report. |
| `AreaMapTest.test_a_file_entry_beats_the_folder_that_contains_it` | The longest-prefix area rule: a single-file entry claims its file, and the folder entry containing it loses that file from its totals and its file list. |
| `AreaMapTest.test_excluded_and_test_code_belong_to_no_area` | A class under an excluded path and a class whose source is under `tests/` each contribute to no area and raise no `unassigned` finding; the excluded one is counted in the exclusions table, and test code is dropped before even the assembly rollup. |
| `ClassificationTest.test_the_thresholds_are_exclusive` | An area exactly at `--gap-below` is thin, not a gap; just under it is a gap; exactly at `--thin-below` is covered. |
| `ClassificationTest.test_a_small_area_is_never_a_gap` | The `--min-lines` boundary: 199 coverable lines at 0 % is thin, 200 is a gap. |
| `UnassignedTest.test_a_source_folder_no_area_claims_suppresses_the_report` | The unassigned boundary check: exit 1, the path and its line count named, no report. |
| `UnassignedTest.test_allow_unassigned_prints_the_report_and_still_fails` | `--allow-unassigned` prints the report and still exits 1. |

No test for argument parsing, for the JSON file's shape or for the Markdown's exact wording, as the implement ticket directed. The `unlisted assembly` check shares its code path with the tested `unassigned` one and was exercised by hand.

## Measurements that correct the implement ticket

Three of the implement ticket's stated facts did not survive contact with a real report. None of them changes a decision; all three are recorded here rather than papered over.

- **The line magnitudes in the ticket are doubled.** The collector lists every coverable line **twice** - once under `<class><lines>` and again under `<methods><method><lines>` - so a naive count of `<line>` elements is 2x the real figure. Verified directly on `Alphora.Dataphor.DAE.SQLite`: every class had `class-level=N, method-level=N, union=N`. The tool counts distinct line numbers, so RealSQL reads **1,423** coverable lines, not the ticket's 2,846, and the plan-node area **24,603**, not 48,398. Nothing downstream depends on the absolute numbers; the README says plainly that rates, not line counts, are what compares between runs.
- **The generated CoreWCF operation invoker is reported under `AlphoraDataphorServer`**, not under a package named `CoreWCF.Dispatcher`. `CoreWCF.Dispatcher` is the *namespace* in the generated class names; the assembly the collector attributes them to is the server. So the `artifacts/` **path** exclusion is what removes them (1 file, 1,154 coverable lines in the sanity run), and the `CoreWCF.Dispatcher` entry in `ignored` did nothing. Both are kept, as the ticket asked, since they are cheap and a future run may attribute it differently.
- **The collection overhead was inside the noise at this size**, not "a tenth of the test time plus about nine seconds". Two cases: 20.5 s wall clock with collection against 20.2 s without, both reporting 7 s of test time. A *first* collected run did report 15 s of test time, so there is a one-off warm-up. The full-suite cost remains unmeasured; that is the sibling ticket's job and AGENTS.md says only what was measured.

Also measured, and new: one Cobertura report of this repository is about **24 MB** whatever the run measured (its bulk is the line list, which does not grow with the number of tests), and reading one takes **6.6 s with a 199 MB memory peak**, of which 8 MB is retained - each input's parse tree is freed before the next is read, so the peak does not grow with the number of parts. A three-part run is therefore roughly 75 MB of XML and 20 s of reading. Both numbers are in the README.

## The map, and the 13 files the ticket's table did not claim

The implement ticket said "every `src/` file of an engine assembly is claimed by one of them". On today's tree it was not: 13 files had no area. They are now claimed, deliberately **without** adding a broad `src/engine/DAE/` or `src/engine/Server/` catch-all, which would have defeated the `unassigned` check for exactly the case it exists for (a *new* folder).

- `src/engine/DAE/Frame.cs`, `Stack.cs`, `StackWindow.cs` added to **Runtime values** (the evaluation stack).
- `src/engine/DAE/CLI.cs`, `SessionInfo.cs` added to **Server and sessions** (the CLI interfaces and the session info the server implements).
- A new 21st area, **Engine errors and registration**: `src/engine/DAE/DAEException.cs`, `Strings.cs`, `Utility.cs`, `Register.cs`, `src/engine/DAE/Device/` (one file, `DeviceException.cs`), `src/engine/DAE/Properties/`, `src/engine/Server/Register.cs`, `src/engine/Server/Properties/`. It is error plumbing, string resources, D4 class registration and assembly metadata - 94 coverable lines in the sanity run, so it will never be a gap, and that is correct.

Checked independently of any coverage run, by walking the six engine projects' folders and asking the map about each `.cs` file: **0 unclaimed**. A reviewer can redo that:

```
python -c "import os,sys; sys.path.insert(0,'tools/coverage-report'); import coverage_report as cr; m=cr.load_map('tools/coverage-report/areas.json'); print([p for b in ('Base','Base.Windows','DAE','Server','Server.Hosting','Store.SQLite') for f,_,fs in os.walk(os.path.join('src','engine',b)) for n in fs if n.endswith('.cs') for p in [os.path.join(f,n).replace(os.sep,'/')] if m.exclusion_of(p) is None and m.area_of(p) is None])"
```

### Assembly names: five confirmed, six still guesses

The implement ticket flagged the library assembly names as taken from project files and unverified. The sanity run loaded and so **confirmed**: `Alphora.Dataphor.Frontend.Server`, `Alphora.Dataphor.DAE.Device.Simple`, `Alphora.Dataphor.DAE.Device.SQL`, `Alphora.Dataphor.DAE.Connection`, `Alphora.Dataphor.DAE.Diagnostics` - along with every engine name except one, and `AlphoraDataphorCommon`, `Alphora.Dataphor.DAE.NativeCLI` and `Alphora.Dataphor.DAE.Host`.

**Still unverified**, because nothing in this run loaded them: `Alphora.Dataphor.DAE.Server.Hosting` (engine), `AlphoraDataphorClient`, `Alphora.Dataphor.DAE.Device.MSSQL`, `Alphora.Dataphor.System.Platform`, `Alphora.Dataphor.System.Internet`, `Alphora.Dataphor.System.Integration`, and `LibraryLoadingProbe` in the `ignored` list. They print as `not in this run` in the per-assembly table. A wrong name there is self-announcing: the assembly turns up as an `unlisted assembly` finding and exits 1. The sibling ticket's full-suite run is what settles them, and fixing one is a one-line edit to `areas.json`.

## What was and was not validated

Ran and green:

- `dotnet build Dataphor.slnx` - 0 errors, 143 warnings (all pre-existing: `CA1416`, `CA2022`, `CS0618`).
- `python -m unittest discover -s tools/coverage-report` - 8 tests, OK.
- The short sanity collection and the tool over it, exit 0, no findings; and over two real reports merged, exit 0.
- Exit code 2 by hand for a missing path, inverted thresholds and a missing areas file.

**Not run, and why:** the three suite parts. Nothing compiled changed - the diff is four new files under `tools/` and one paragraph of `AGENTS.md`, plus a ticket body - so there is no C# behaviour for them to cover, and the sibling ticket runs all three under collection anyway. No pre-existing test failure was met, so `tickets/.pre-existing-error.md` was not written.

**Known gaps a reviewer should weigh:**

- **The `line sets differ` path has never seen real stale data**, only the fixture test. Producing it for real needs a rebuild between two collections; worth doing once if the reviewer wants the belt-and-braces.
- **No three-part merge of real reports.** Two were merged, which is where the interesting case (a class in both) lives, but the sibling ticket is the first three-part run.
- **A report produced on another machine** falls through `relativize` and is reported verbatim rather than crashing; the sanity reports all came from this machine, so that branch is checked by inspection only, as the implement ticket allowed. Such files are counted in the per-assembly rollup and in a one-line note at the end of the report, and belong to no area.
- **Standard output is encoded with the console code page** (cp1252 here), so a non-ASCII source path printed to the terminal would raise rather than mangle. Everything the tool emits today is ASCII, `-o` writes UTF-8, and `tools/docs-link-check/check_links.py` has the identical exposure; noted, not changed.
- **The exclusion of `src/engine/DAE/REST/` and `src/engine/Server/REST/`** is a judgement the implement ticket settled, and the arm appended to `tickets/backlog/debt-ws12-hygiene-remainder.md` (`hygiene-remove-rest-leftovers`) is what removes both the files and the exclusion. Verified while writing it: the only files in the repository mentioning `RESTResult` or `RESTMarshal` are the two themselves, both have explicit `Compile` entries (`DAE.csproj:114`, `Server.csproj:73`), and `Server.csproj`'s own comment says its `Newtonsoft.Json` reference exists for `RESTMarshal.cs` - while `DAE.NativeCLI.csproj` references that package for its own reasons and keeps it.
- **`#if SILVERLIGHT` is not a coverage question**, as the implement ticket recorded: a conditional branch is not a separate source file, so dead Silverlight code cannot be allow-listed by path and never shows up as a 0 % file. Nothing was done; this sentence is the record that it was checked.
- **RealSQL stays an area and is a real gap at 0 %.** It is live code, not dead: `QueryLanguage.RealSQL`, selected by `SetLanguage('RealSQL')`, is reached from Dataphoria's SQL editor and a Frontend operator. The implement ticket establishes this; nothing here re-tested it.

## CI, deliberately unchanged

The plan's default was nightly collection; on-demand was chosen instead and that is a decision, not an omission. The overhead is small, but this suite has cases that assert on wall-clock behaviour (`AbandonedClientProcessTest` waits for a session reclamation measured at 4.7-5.1 s against a 5 s threshold; the coverage manifest carries `timeoutSeconds` entries), nothing has yet run the whole suite under instrumentation, and a number that is reported rather than gated must never be able to fail the build. The tripwire is recorded as a `NOTE:` at the top of `coverage_report.py` and restated in the README's last section: if a later pass shows the suite is stable under instrumentation, adding the collector arguments to the existing CI test step is a two-line change and gives a Cobertura artifact per push.

## Review focus

- `areas.json` is the file whose judgement is worth a second pair of eyes: do the 21 areas carve the engine the way a gap ticket would want to be scoped, and is **Engine errors and registration** the right home for the eight stragglers, or should they go elsewhere?
- The decision **not** to add a broad `src/engine/DAE/` catch-all - it keeps the `unassigned` check meaningful at the price of a line of map maintenance per new folder. Is that the trade you want?
- Whether suppressing the report on an `unlisted assembly` finding (not just an `unassigned` one) is right. The implement ticket spelled out only the unassigned case; both are "the map is incomplete", and `--allow-unassigned` lifts both.
- `--min-lines 200` and the 25 / 50 thresholds are the implement ticket's numbers, unmeasured against anything. With the halved line counts (above), 200 is a slightly bigger slice of the engine than the ticket's author will have had in mind.
