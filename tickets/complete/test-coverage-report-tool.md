description: Reviewed and corrected a new checked-in tool that turns the test run's code-coverage output into a readable report of which parts of the engine the tests actually exercise. Five of the assembly names the tool was told to look for were wrong, which would have left the next full run with no report at all.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tools/coverage-report/coverage_report.py, tools/coverage-report/areas.json, tools/coverage-report/test_coverage_report.py, tools/coverage-report/README.md, AGENTS.md
----
## What landed

Half of the plan's WS3 work item `test-coverage-gap-analysis` - the half that needs no long test run. The plan's WS3 cross-cutting requirements ask for "line coverage reported (not gated) per assembly"; this is the reporting tool and the map that gives the numbers meaning. The sibling ticket `test-coverage-gap-analysis` (still in `implement/`) runs the whole suite under coverage collection, publishes `docs/coverage-report.md` from it and files one ticket per gap, and is what marks the plan's work-item row **Done**.

Four new files in `tools/coverage-report/`, mirroring `tools/docs-link-check/`: Python 3, standard library only, one module (655 lines, against 544 for its sibling), one test file, one README, plus the map as JSON. Two edited files: `AGENTS.md` and one arm appended to the hygiene umbrella in `backlog/`. **No compiled source changed.**

- `coverage_report.py` - reads and merges Cobertura reports, applies the map, classifies each area, emits Markdown, implements the two boundary checks and the three exit codes.
- `areas.json` - the three assembly lists (`engine`, `reported`, `ignored`), 21 areas and 2 exclusions.
- `test_coverage_report.py` - 10 test methods.
- `README.md` - what it is, the collect command, the run command, the thresholds, the two boundary checks and their exit codes, how to add an area, and what the numbers do and do not mean.

Nothing was installed. `Microsoft.NET.Test.Sdk`, which every test project here already references, brings Microsoft's own coverage collector, so `--collect:"Code Coverage;Format=cobertura"` on a normal `dotnet test` is the whole collection story. No `coverlet` package, no global tool, no project change. No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension could notice changes.

The implement stage is commit `ticket(implement): test-coverage-report-tool`.

## How to run it

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "Name~LibraryLoading" --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/sanity
python tools/coverage-report/coverage_report.py artifacts/coverage/sanity
python -m unittest discover -s tools/coverage-report
```

That is the end-to-end path both stages validated. Collector arguments also compose with the three suite-part commands of AGENTS.md, which end in a bare `--`; review confirmed that ordering against a real run, which the implement stage had only asserted.

A good run ends in one summary line, for example `1 report merged, 5 of 6 engine assemblies measured, 21 areas, 13 gaps, 0 unassigned files, 0 unlisted assemblies`, with the Markdown on standard output and exit 0. **No coverage figure from either stage is a coverage result for this project**: both ran a handful of test cases, so the numbers show only the shape of the output. The real numbers come from the sibling ticket's full-suite run.

## Review findings

The implement handoff read as finished and asked four questions. The diff was read before the handoff was. Three were defects, all fixed in this pass; four were approved as built with a reason; two conditional concerns and one already-declined one were recorded at their code sites, and three documents were corrected. Nothing was filed - the one candidate ticket and why it was not filed is below.

### Fixed in this pass

**1. Five of the assembly names in `areas.json` were wrong, and two assemblies were on no list.** This is the finding that mattered. The handoff said six names were "still unverified, because nothing in this run loaded them", and treated a coverage run as the only way to settle them. They are settled without one: an assembly's name is its project's `<AssemblyName>`, and the built output spells it out. Reading the names off `Dataphor.slnx`'s projects and off `artifacts/bin/` and the runtime layout gave:

| Was in `areas.json` | Actually built as |
|---|---|
| `Alphora.Dataphor.DAE.Device.MSSQL` | `AlphoraMSSQLDevice` |
| `Alphora.Dataphor.System.Platform` | `System.Platform` |
| `Alphora.Dataphor.System.Internet` | `System.Internet` |
| `Alphora.Dataphor.System.Integration` | `System.Integration` |
| `LibraryLoadingProbe` | `Alphora.Dataphor.Diagnostics.LibraryLoadingProbe` |

and `DAEServer` and `DAEService`, the two host executables, were on none of the three lists although the README says `reported` holds "the hosts".

Why it mattered: an assembly on no list is an `unlisted assembly` finding, which exits 1 and **suppresses the report**. The four wrong library names are all libraries a full run loads, so the sibling ticket's whole-suite run - eleven minutes of collection - would have ended with findings and no report, four times over, while four phantom names read `not in this run` for ever. All seven corrected, each against the built file name. Confirmed by a real report for the probe: a collected `LibraryLoading` run prints `Alphora.Dataphor.Diagnostics.LibraryLoadingProbe` as its package name.

**2. A `reported` assembly whose source lives under `tests/` can never report a number.** `Alphora.Dataphor.DAE.Diagnostics` (the coverage helper library, `tests/libraries/TestFramework.Coverage.Base/Source/`) was in `reported`. The tool drops every class whose source is under `tests/` before the assembly rollup, so that entry promised a row it could only ever fill with `not in this run`. Moved to `ignored`, where the other test-support assemblies are, and the README now states the rule. The same ordering is why the wrong probe name in finding 1 had not already broken a run: the probe's source is under `tests/` too, so it was dropped before the unlisted check ever saw it. Recorded as a `NOTE:` on `analyse`, with what to change if a test-support assembly ever has to be measured.

**3. A published report could read as complete while the map was incomplete.** `--allow-unassigned` prints the report and exits 1, but the findings that explain why went only to standard output. `-o docs/coverage-report.md --allow-unassigned` therefore wrote a document with no trace of the hole - every total in it describing less than the engine, with nothing saying so. The report now opens with a **The map is incomplete** section listing each unclaimed file and each unlisted assembly with its line count, whenever one exists. This is the one defect that justified new tests, and it has them.

### Approved as built, with the reason

**The 21 areas carve the engine the right way** (the handoff's first question). The decisive test is the plan's own list of gap candidates - application transactions, server links, security, library upgrade scripts, streams - and each one is its own area, so a gap ticket scopes to exactly what the plan asks about. **Engine errors and registration** is the right home for the eight stragglers: error plumbing, string resources, D4 class registration and assembly metadata are one thing, 94 coverable lines of it, so the area will never be a gap, which is correct. Re-verified independently that the map claims every `.cs` file of the six converted engine projects: 0 unclaimed. Walking all of `src/engine/` turns up nine more files, all in `Store.MSSQL/` and `Store.PGSQL/`, which are not in `Dataphor.slnx` and so never build or appear in a report; when one is converted it announces itself as an `unlisted assembly`.

**No broad `src/engine/DAE/` catch-all** (the handoff's second question) is the right trade, and it was exercised rather than argued: running the tool over a real report with the `Debugger` area deleted from the map named all eight of its files and suppressed the report. A catch-all would have made that silence instead. One line of map maintenance per new folder buys that.

**Suppressing the report on an `unlisted assembly`, not just an `unassigned` file, is right** (the handoff's third question). Both are the same failure: the thing is absent from every table and total below, so the rollup reads as if its subsystem were fine. Kept, with the reasoning as a comment at the site, and finding 3 is what makes `--allow-unassigned` safe to reach for. Finding 1 is also the evidence: the wrong names were caught by this check doing its job.

**The thresholds hold up** (the handoff's fourth question, `--min-lines 200` and 25/50 against the halved line counts). Measured on a real report rather than argued: the 21 areas came out 13 gap, 8 thin, which is a usable spread and not degenerate, and `--min-lines 200` excluded exactly one area - the 94-line error-plumbing one - which is the case it exists for. No change.

### Recorded, not ticketed

- **Merging reports from two different machines would double-count instead of raising.** The merge guard compares classes by a key that carries the resolved source path, so two checkouts at different absolute paths resolve one class to two keys and both are counted - silently inflating the coverable total, where a stale report of the same checkout raises `line sets differ`. Conditional: every report comes from the machine that runs the suite. `NOTE:` on `merge`, saying to key on the class name alone if cross-machine merging is ever needed.
- **An exclusion is consulted before the areas**, so a folder exclusion beats a longer single-file area entry inside it, unlike the longest-prefix rule between areas. No entry relies on it today. `NOTE:` at the site.
- **Standard output is encoded with the console code page**, so a non-ASCII source path printed to the terminal would raise rather than mangle. The handoff recorded this and declined it; everything the tool emits today is ASCII, `-o` writes UTF-8, and `tools/docs-link-check/check_links.py` has the identical exposure. Re-checked, still declined, unchanged.

### Documents brought up to date

Treated as out of date until read. Three corrections, in files the change touches or should have:

- **`AGENTS.md`'s `tools/` table** listed every other subfolder of `tools/` and not `coverage-report/`. The implement stage added a paragraph to the test section and left the layout table stale. Added, pointing at the new paragraph.
- **The README opened by saying the alternative was "reading a 100 MB XML file by hand"** while its own measurement section says one report is about 24 MB. Corrected to 24 MB.
- **The README's exit-code table claimed exit 2 for "an input that is not a Cobertura document".** Only true of XML that does not parse; a well-formed document with no assembly on any list exits 1 through the `no listed assembly` path, which the next paragraph already described. Verified by running it. Wording corrected, and the two stale sentences about the diagnostics library and about confirming an assembly name were corrected alongside finding 2.

`docs/modernization-plan.md` was read and deliberately left alone: its `test-coverage-gap-analysis` row is the whole work item, and the sibling ticket is what marks it **Done**. No `docs/migration-notes.md` entry: nothing an application, a deployment script or a custom C# extension could notice.

### Tests

The eight tests were reviewed as code, not treated as a floor. None restates the implementation or verifies a mock, and none was cut. Two changes, both earning their place:

- `UnassignedTest.test_allow_unassigned_prints_the_report_and_names_the_hole_in_it` - the existing test, extended to assert the rendered Markdown names the unclaimed file. This is the reproduction of finding 3.
- `UnlistedAssemblyTest`, two methods - the `unlisted assembly` branch, which the handoff said was "exercised by hand". It is the branch every wrong name in finding 1 travels down, it has its own detection and now its own rendering, and it was the only contract of the tool with no test at all.

10 tests, all pass.

### The ticket that was considered and not filed

The class behind finding 1 is "a name in `areas.json` that no project produces", and the invariant that would retire it is a check that every listed name matches some project's `<AssemblyName>` or built output. Not filed: it would put an MSBuild reader inside a tool that deliberately only reads Cobertura, the failure already announces itself at run time by design, and every current name is now checked against the built output. The cheap half is done instead - the README says where to read a name off and tells you never to guess one from a project or folder name.

## What was and was not validated

Ran and green:

- `python -m unittest discover -s tools/coverage-report` - 10 tests, OK.
- Two collected runs over `LibraryLoadingTest`, one with `--filter` and one with the documented collector-before-`--` ordering: 4 tests pass each, one Cobertura file each. The tool over one of them: exit 0, no findings. Over both merged: exit 0, `2 reports merged`.
- The incomplete-map paths on real data, through a copy of `areas.json` with one area and one assembly removed: without `--allow-unassigned`, 8 `unassigned` lines plus 1 `unlisted assembly` and no report, exit 1; with it, the report prints and opens with the new section naming all nine, exit 1.
- `-o` and `--json` to files: both UTF-8 with CRLF throughout and no LF-only line.
- Exit 2 by hand for a missing path, inverted thresholds, a missing areas file and a negative `--min-lines`. Exit 1 confirmed for well-formed XML that is not a Cobertura report.

**Not run, and why:** the three suite parts. The whole diff is Python, JSON and Markdown under `tools/` plus one line of `AGENTS.md`; no compiled source changed in either stage, so there is no C# behaviour for them to cover, and the sibling ticket runs all three under collection anyway. The `--no-build` test runs above do confirm the existing build and the suite machinery are intact. No pre-existing test failure was met, so `tickets/.pre-existing-error.md` was not written. `artifacts/coverage/` was deleted after the runs; `artifacts/` is git-ignored and no artifact was left behind.

**Known gaps a later pass should weigh:**

- **The `line sets differ` path has still never seen real stale data**, only its fixture test. Producing it for real needs a rebuild between two collections.
- **No three-part merge of real reports.** Two were merged in each stage, which is where the interesting case (a class in both) lives; the sibling ticket is the first three-part run.
- **Eight assembly names remain unmeasured by a real report**, though every one is now checked against the built file name: `Alphora.Dataphor.DAE.Server.Hosting`, `AlphoraDataphorClient`, `AlphoraMSSQLDevice`, `System.Platform`, `System.Internet`, `System.Integration`, plus `DAEServer` and `DAEService`, which no test project references and which may simply never appear. A wrong one is still self-announcing and a one-line edit to `areas.json`.
- **No third-party assembly appeared in either stage's reports** - ten packages across two runs, all built from this repository - which suggests the collector only instruments modules whose symbols are in the output folder. A full run loads CoreWCF, SQLite and Newtonsoft.Json, so if any of those ships usable symbols it will arrive as an `unlisted assembly`. `--allow-unassigned` is the documented way through it, and the right answer would be an `ignored` entry per package.
- **`#if SILVERLIGHT` is not a coverage question**: a conditional branch is not a separate source file, so dead Silverlight code cannot be allow-listed by path and never shows up as a 0 % file. Both stages checked this; nothing was done.
- **RealSQL stays an area and is a real gap at 0 %.** It is live code: `QueryLanguage.RealSQL`, selected by `SetLanguage('RealSQL')`, is reached from Dataphoria's SQL editor and a Frontend operator. The implement ticket establishes this; neither stage re-tested it.

## CI, deliberately unchanged

The plan's default was nightly collection; on-demand was chosen instead, and that is a decision, not an omission. The overhead is small, but this suite has cases that assert on wall-clock behaviour (`AbandonedClientProcessTest` waits for a session reclamation measured at 4.7-5.1 s against a 5 s threshold; the coverage manifest carries `timeoutSeconds` entries), nothing has yet run the whole suite under instrumentation, and a number that is reported rather than gated must never be able to fail the build. The tripwire is a `NOTE:` at the top of `coverage_report.py` and the README's last section: if a later pass shows the suite is stable under instrumentation, adding the collector arguments to the existing CI test step is a two-line change and gives a Cobertura artifact per push.
