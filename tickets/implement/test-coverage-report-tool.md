description: Add a small checked-in tool that turns the test run's code-coverage output into a readable report of which parts of the engine the tests actually exercise, so the untested parts can be found by running a command instead of by reading a large XML file by hand.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/areas.json, tools/coverage-report/README.md, tools/coverage-report/test_coverage_report.py, tools/docs-link-check/check_links.py, AGENTS.md
difficulty: medium
----
## What this is

Half of the plan's WS3 work item `test-coverage-gap-analysis`, and the half that needs no long test run. The plan's WS3 cross-cutting requirements ask for "line coverage reported (not gated) per assembly". This ticket builds the reporting tool and the map that gives it meaning; the sibling ticket `test-coverage-gap-analysis` runs the suite under coverage collection, publishes the report and files one ticket per gap.

Everything below is settled. Do not re-open a choice; if a measurement here turns out to be wrong, say so in the handoff.

## What collects the coverage (settled: nothing new to install)

`Microsoft.NET.Test.Sdk` 18.11.0, which every test project here already references, brings Microsoft's own coverage collector. No `coverlet` package, no global tool:

```
dotnet test tests/Common.Tests --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part1
```

It writes one Cobertura XML file into a GUID-named subfolder of the results directory. Verified on this repository on 2026-11-21 (all measurements below are from that date, Debug, on the slow development machine):

- **It works with no project change and no new package.** `coverlet` was the plan ticket's default; it is not needed and is not added.
- **It coexists with the NUnit filter arguments** the suite is run with: `--collect:"…" … -- "NUnit.Where=…"` collected normally (`--` must stay last).
- **It follows child processes.** A run of the single test `AServerInAnotherProgramServesTheShippedClient`, whose server is a separate program, reported `Alphora.Dataphor.DAE.Server.Hosting` at 62 % and the child's own assembly `DAE.Test.Harness` at 41 %. So the out-of-process cases do contribute coverage, and the test harness assembly appears in the report and has to be excluded.
- **It covers assemblies the server loads at run time** from its copy of the runtime layout: `Alphora.Dataphor.Frontend.Server`, `Alphora.Dataphor.DAE.Device.Simple` and `Alphora.Dataphor.DAE.Diagnostics` all appeared with non-zero rates.
- **Cost:** two coverage-script cases took 10 s of test time against 9 s without collection, and 29.9 s against 20.5 s of wall clock — so roughly a tenth of the test time plus about nine seconds of fixed start-up and merge cost.
- **The report includes generated source.** The CoreWCF operation-invoker generator's output (`artifacts/obj/Server/debug/CoreWCF.BuildTools.Roslyn4.0/…/OperationInvoker.g.cs`, reported under a package named `CoreWCF.Dispatcher`) is in it. Generated source is excluded by path, not by package name.

The Cobertura shape, which the tool reads: `<package name="…">` is one **assembly** (its assembly name, so the server assembly appears as `AlphoraDataphorServer`), `<class name="…" filename="…">` is one type with the **absolute** path of its source file, and each `<line number= hits= />` is one coverable line.

## What the tool is

`tools/coverage-report/`, mirroring `tools/docs-link-check/` exactly: Python 3, standard library only, one module, one test file, one README. That folder is the precedent for everything not stated here — argument handling, repository-relative paths with forward slashes, one finding per line, a summary line, and exit codes 0 (all well), 1 (a finding), 2 (bad arguments).

```
python tools/coverage-report/coverage_report.py artifacts/coverage [MORE...] [options]
```

Each positional argument is a Cobertura file or a directory; a directory is searched recursively for `*.cobertura.xml`. Options:

| Option | Default | Meaning |
|---|---|---|
| `--areas FILE` | `tools/coverage-report/areas.json` beside the script | the assembly lists, the area map and the exclusions |
| `--repo-root DIR` | the folder holding `.git`, found upward from the script | what source paths are made relative to |
| `-o FILE` | standard output | where the Markdown goes |
| `--gap-below PCT` | `25` | below this line coverage an area is a **gap** |
| `--thin-below PCT` | `50` | below this (and not a gap) an area is **thin** |
| `--min-lines N` | `200` | an area with fewer coverable lines is never a gap, whatever its rate |
| `--json FILE` | — | also write the rolled-up numbers as JSON, for a later pass to diff against |
| `--allow-unassigned` | off | print the report even when the map is incomplete (still exits 1) |

Exit 1, with the finding lines before the report, when either boundary check fails:

- an engine source file under `src/` that no area and no exclusion claims (printed one per line as `unassigned: <path> (<n> lines)`);
- an assembly in the input that none of the three assembly lists names (`unlisted assembly: <name> (<n> lines)`).

Both exist so that a new source folder or a newly converted project cannot quietly disappear from the report: the next person to add one is told to put it on the map. Exit 1 also when no input file was found or no package in the input matched any list.

### Merging several runs

The suite is run in three parts (AGENTS.md, *Agents run the suite in parts*), so the tool takes several reports and merges them. Merge rule: union keyed on `(package name, class name, class filename, line number)`, a line counted covered if **any** report gives it a non-zero hit count. Verified sound: across two independent runs (a `Common.Tests` run and a `DAE.Server.Tests` run) 76 classes appeared in both and every one of them had an identical set of line numbers, because both ran against the same build. The tool must therefore **reject** inputs that disagree: if a class common to two reports has different line-number sets, print `line sets differ: <package>/<class>` and exit 1 rather than produce a number nobody can trust — that is what a stale report mixed with a fresh one looks like.

### Rolling up

Three levels, in this order:

1. **Per assembly** — every assembly in the `engine` and `reported` lists, with its covered and coverable line counts and the rate. This is the plan's "line coverage reported per assembly".
2. **Per area**, for the engine assemblies only. An area is a named set of repository-relative path prefixes; a class belongs to the area whose **longest matching prefix** claims its file, so a single-file entry beats the folder entry containing it. Areas are classified `gap`, `thin` or `covered` by the thresholds above, and sorted by **uncovered line count** (coverable × (1 − rate)) descending, so the biggest holes come first.
3. **Per file**, listed under each `gap` area only, worst first, so the gap tickets can name files rather than namespaces.

Classes whose filename is under `tests/` are dropped entirely wherever they appear: test code is not the subject. Classes in `reported` assemblies need no area (libraries, client, host); they are rolled up per assembly only and are never "unassigned". Classes in `ignored` assemblies are dropped.

### The map: `tools/coverage-report/areas.json`

```json
{
  "assemblies": {
    "engine":   ["Alphora.Dataphor", "Alphora.Dataphor.Windows", "Alphora.Dataphor.DAE",
                 "AlphoraDataphorServer", "Alphora.Dataphor.DAE.Server.Hosting",
                 "Alphora.Dataphor.DAE.SQLite"],
    "reported": ["AlphoraDataphorCommon", "AlphoraDataphorClient", "Alphora.Dataphor.DAE.NativeCLI",
                 "Alphora.Dataphor.DAE.Host", "Alphora.Dataphor.Frontend.Server",
                 "Alphora.Dataphor.DAE.Device.SQL", "Alphora.Dataphor.DAE.Device.MSSQL",
                 "Alphora.Dataphor.DAE.Device.Simple", "Alphora.Dataphor.DAE.Connection",
                 "Alphora.Dataphor.DAE.Diagnostics", "Alphora.Dataphor.System.Platform",
                 "Alphora.Dataphor.System.Internet", "Alphora.Dataphor.System.Integration"],
    "ignored":  ["DAE.Server.Tests", "Common.Tests", "DAE.Test.Harness", "LibraryLoadingProbe",
                 "Layout", "CoreWCF.Dispatcher"]
  },
  "areas": [ { "name": "…", "paths": ["…"] } ],
  "excluded": [ { "paths": ["…"], "reason": "…", "ticket": "…" } ]
}
```

The assembly names are the ones the collector printed, not the project file names: confirm each against a real report rather than guessing (`AlphoraDataphorServer`, `AlphoraDataphorCommon` and `AlphoraDataphorClient` have no dots, and the library assembly names above are from the project files and are **unverified** — a run that loads them will say, and `--allow-unassigned` plus the `unlisted assembly` findings is how you find out). `CoreWCF.Dispatcher` is in `ignored` because its only content here is the generated operation invoker, which the `artifacts/` exclusion also removes; keep both, they are cheap.

Areas, from the namespace and per-file rollups measured on 2026-11-21. Paths are repository-relative with forward slashes; a path ending in `/` is a folder prefix:

| Area | Paths |
|---|---|
| Application transactions | `src/engine/DAE/Device.ApplicationTransaction/` |
| Streams | `src/engine/DAE/Streams/`, `src/engine/Server/Streams/`, `src/engine/Base/StreamUtility.cs`, `src/engine/Base/TemporaryStream.cs` |
| Security | `src/engine/DAE/Schema/Security.cs`, `src/engine/DAE/Runtime.Instructions/SecurityNodes.cs`, `src/engine/Server/Runtime.Instructions/SecurityNodes.cs` |
| Server links | `src/engine/DAE/Schema/ServerLink.cs`, `src/engine/DAE/Server/RemoteSession.cs`, `src/engine/Server/Server/RemoteSessionImplementation.cs` |
| Libraries and upgrades | `src/engine/DAE/Schema/Library.cs`, `src/engine/DAE/Runtime.Instructions/LibraryNodes.cs`, `src/engine/Server/Runtime.Instructions/LibraryNodes.cs`, `src/engine/Server/Schema/LibraryUtility.cs` |
| RealSQL | `src/engine/DAE/Language.RealSQL/` |
| Debugger | `src/engine/DAE/Debug/` |
| Catalog device and store | `src/engine/DAE/Device.Catalog/`, `src/engine/Server/Device.Catalog/`, `src/engine/DAE/Store/`, `src/engine/Store.SQLite/` |
| Memory device | `src/engine/DAE/Device.Memory/` |
| D4 language | `src/engine/DAE/Language.D4/`, `src/engine/DAE/Language/` |
| Compiler | `src/engine/DAE/Compiling/` |
| Plan nodes | `src/engine/DAE/Runtime.Instructions/`, `src/engine/Server/Runtime.Instructions/` |
| Runtime values | `src/engine/DAE/Runtime.Data/`, `src/engine/DAE/Runtime/` |
| Schema | `src/engine/DAE/Schema/` |
| Server and sessions | `src/engine/DAE/Server/`, `src/engine/Server/Server/` |
| WCF services and contracts | `src/engine/Server/Service/`, `src/engine/DAE/Contracts/`, `src/engine/Server/Contracts/`, `src/engine/Server.Hosting/` |
| Native CLI (server side) | `src/engine/Server/NativeCLI/` |
| SQL translation (engine) | `src/engine/DAE/Language.SQL/` |
| Device connections (engine) | `src/engine/DAE/Connection/`, `src/engine/Server/Connection/` |
| Base utilities | `src/engine/Base/`, `src/engine/Base.Windows/` |

The last seven rows are deliberately broad folder prefixes that the specific rows above them carve pieces out of; that is what the longest-prefix rule is for. Every `src/` file of an engine assembly is claimed by one of them, which is what makes the `unassigned` check meaningful: it fires on a *new* folder, not on today's tree. Add any folder the run reports as unassigned, and say in the handoff which ones you had to add.

Exclusions (reported in a short list at the end of the report, never classified, never ticketed):

| Paths | Reason |
|---|---|
| `artifacts/` | generated source; the CoreWCF operation-invoker generator writes into `artifacts/obj/` |
| `src/engine/DAE/REST/`, `src/engine/Server/REST/` | `RESTResult` and `RESTMarshal` are compiled but nothing in the repository calls them: their only consumer was the web prototype now parked in `legacy/`. Removal belongs to the hygiene umbrella — see the TODO below |

**The plan ticket's guess about `#if SILVERLIGHT` does not apply.** A conditional branch is not a separate source file, so dead Silverlight code cannot be allow-listed by path and does not show up as a 0 % file. Nothing to do; this sentence is the record that it was checked.

**`Alphora.Dataphor.DAE.Language.RealSQL` is live code, not dead.** It looked like an obvious allow-list candidate at 0 % over 2,846 lines. It is reachable: `QueryLanguage.RealSQL`, selected by `SetLanguage('RealSQL')`, is used by Dataphoria's SQL editor (`src/windows/Dataphoria/TextEditor/D4Editor.cs`) and by a Frontend operator (`libraries/Frontend/Source/Operators/Operators.cs`), and `Compiler.cs` and `ServerProcess.cs` dispatch to it. It stays an area and is a real gap.

## Tests

One per behaviour, against small Cobertura fixtures written by the test (the real reports are far too large to check in), in `tools/coverage-report/test_coverage_report.py`, run by `python -m unittest discover -s tools/coverage-report` as the link checker's tests are:

- **Merge union.** Two reports, one class in both, a line covered in only the second: the merged class counts it covered. A class present in only one report appears in the merge.
- **Merge mismatch.** Two reports whose common class has different line numbers: exit 1, with the class named, and no report.
- **Longest-prefix area match.** A map with both a folder entry and a single-file entry inside it: the file lands in the file entry's area, and the folder's totals exclude it.
- **Classification boundaries.** An area at exactly `--gap-below` is not a gap; just under it is. An area at exactly `--thin-below` is not thin.
- **`--min-lines`.** An area under the gap threshold with fewer coverable lines than `--min-lines` is reported, but not as a gap.
- **Unassigned engine file.** An engine-assembly class under `src/` that no area claims: exit 1, the path named; with `--allow-unassigned` the report is printed and the exit code is still 1.
- **Exclusions and test code.** A class under an excluded path, and a class whose filename is under `tests/`, each contribute to no area and raise no `unassigned` finding.

No test for argument parsing, for the JSON file's shape or for the Markdown's exact wording.

## Documentation

- `tools/coverage-report/README.md` — what it is, the collect command, the run command, the thresholds, the two boundary checks and their exit codes, how to add an area, and what the numbers do and do not mean (line coverage says a line ran, not that anything asserted on its result).
- **AGENTS.md**, a short paragraph at the end of *Build and test*, after *Agents run the suite in parts*: how to collect coverage, that it needs no extra package, the measured cost, and a pointer to `tools/coverage-report/README.md`.
- No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension could notice changes. No `docs/coverage-report.md` either — the sibling ticket writes that from a real run.

**CI is not changed, and that is the decision, not an omission.** The plan's default was nightly. On-demand is chosen instead: the overhead is small (about a tenth of the test time) but instrumentation is not free of risk for this suite in particular, which has cases that assert on wall-clock behaviour — `AbandonedClientProcessTest` waits for a session reclamation measured at 4.7–5.1 s against a 5 s threshold, and the coverage manifest carries `timeoutSeconds` entries — and a number that is reported rather than gated must never be able to fail the build. Nothing has yet run the whole suite under instrumentation, so there is no evidence it is stable under it. The sibling ticket is the first run that will have that evidence and records it. Put a one-line `NOTE:` tripwire at the top of `coverage_report.py` or in the README: *if a later pass shows the suite is stable under instrumentation, wiring the collector into the existing CI test step is a two-line change and gives a Cobertura artifact per push.*

## Edge cases & interactions

- **A stale report mixed with a fresh one** gives numbers against two different builds. Handled by the line-set check above (exit 1, name the class). This is the one failure mode worth a test, and it has one.
- **An assembly loaded in one part and not another.** Normal and correct: the union covers it. Nothing to do, but the report should print how many input files were merged so a reader can tell a one-part run from a three-part one.
- **A newly converted project** appears as an `unlisted assembly` and exits 1. By design. Verified by inspection of the branch; no test (it is the same code path as the unassigned-file check, which is tested).
- **A source file moved between folders** lands in a different area, or becomes unassigned. The boundary check is the whole protection; no test.
- **Absolute paths in the Cobertura file** are the build machine's. Relativize against `--repo-root`, and leave a path that is not under it as it is — reported verbatim in the file list rather than crashing. Checked by inspection.
- **Line counts are not statement counts.** The collector's `<line>` entries are what it considers coverable, and the totals are large (48,398 for the plan-node folder). Compare rates between runs of the same build, never line counts across builds; say so in the README.
- **Coverage is not assertion.** Nine coverage scripts pass with no assertions at all (the manifest records them: `Functions`, `LibraryFilesCoverage`, `LockAllocation`, `Register`, `RowAllocation`, `ScalarAllocation`, `SerializationDevices`, `TestGH37`, `Warnings`), and a script that executes a path without checking its result still colours that path green. The README must say this plainly; the sibling ticket's report repeats it.
- **`test-perf-baseline`** also wants numbers from a suite run. Collecting coverage and timing in one `dotnet test` invocation would be convenient and the two are independent. Noted, not acted on — neither ticket blocks the other.
- **Source hygiene.** New files, so there is nothing to preserve: write them CRLF without a byte-order mark, which is what `tools/docs-link-check/check_links.py` and every neighbouring file have. Do not reformat anything in `AGENTS.md` beyond the paragraph added.

## TODO

- Write `tools/coverage-report/coverage_report.py`: read and merge the Cobertura inputs, apply the map, classify, emit Markdown, implement both boundary checks and the three exit codes.
- Write `tools/coverage-report/areas.json` with the assembly lists, the area table and the exclusions above.
- Write `tools/coverage-report/test_coverage_report.py` with the seven tests above; run `python -m unittest discover -s tools/coverage-report`.
- Sanity-check the whole path end to end on a short run — `dotnet test tests/DAE.Server.Tests --no-build --filter "Name=Lexer|Name=Types" --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/sanity`, then the tool over that folder — and fix every `unlisted assembly` and `unassigned` finding it raises. Do **not** publish `docs/coverage-report.md` from a short run; that is the sibling ticket's job from a full one.
- Write `tools/coverage-report/README.md` and the AGENTS.md paragraph.
- Add the `NOTE:` tripwire about wiring CI later.
- Append one arm to `tickets/backlog/debt-ws12-hygiene-remainder.md`: `hygiene-remove-rest-leftovers` — delete `src/engine/DAE/REST/RESTResult.cs` and `src/engine/Server/REST/RESTMarshal.cs` with their compile-list entries and the `Newtonsoft.Json` reference the latter needs, since nothing in the repository calls either and their only consumer was the parked web prototype; a custom C# extension could in principle call `RESTMarshal`, so it needs a `docs/migration-notes.md` line. (An arm on the existing umbrella, not a new ticket: the hygiene umbrella already owns dead-code removal.)
- Validation: `dotnet build Dataphor.slnx`, the Python unittest, and the short sanity run above. No compiled source changes in this ticket — only new files under `tools/` and one paragraph of `AGENTS.md` — so the three suite parts are not run here; say that in the handoff rather than leaving the reader to wonder. The sibling ticket runs all three under coverage.
