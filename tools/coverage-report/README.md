# coverage-report — what the test suite actually exercises

Turns the test run's code-coverage output into a Markdown report: line coverage per assembly, then per engine *area* (a named set of source paths), then every file the run never entered at all, then file by file for the areas with the biggest holes. It exists so that the untested parts of the engine can be found by running a command instead of by reading a 24 MB XML file by hand. It needs nothing beyond Python 3: one module, standard library only, no network.

It reports; it never gates. No build or CI step fails on a number from here.

## Collect the coverage

Nothing to install. `Microsoft.NET.Test.Sdk`, which every test project here already references, brings Microsoft's own coverage collector, so adding two arguments to a normal `dotnet test` is the whole of it:

```
dotnet build Dataphor.slnx
dotnet test tests/Common.Tests --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part1
```

Each run writes one Cobertura XML file into a GUID-named subfolder of the results directory. Things worth knowing:

- **It coexists with the NUnit filter arguments** the suite is run in parts with (AGENTS.md, *Agents run the suite in parts*). Put the collector arguments before the `--`, which must stay last: `--collect:"…" … -- "NUnit.Where=…"`.
- **It follows child processes**, so the out-of-process client/server cases contribute coverage too — their child's own assembly (`DAE.Test.Harness`) turns up in the report and is on the `ignored` list below.
- **It covers assemblies the server loads at run time** from its copy of the runtime layout: the Frontend server and the Simple device appear with non-zero rates. (The coverage helper library does not: its source is under `tests/`, which the tool drops.)
- **Cost is small.** Measured 2026-11-21 (Debug, slow development machine): a two-case run took 20.5 s of wall clock with collection against 20.2 s without, both reporting 7 s of test time — inside the noise at that size. An earlier first run with collection reported 15 s of test time, so expect a one-off warm-up. At the scale of the whole suite, `test-coverage-gap-analysis` measured the longest part at 6 m 03 s with collection against 5 m 13 s without, **+16 %**, and the three parts at 13 m 40 s together; see [docs/coverage-report.md](../../docs/coverage-report.md).
- NOTE: **one run per script would answer "does script X cover anything the others do not"**, because the collector can be pointed at a single test (`--filter Name=<script>`), and that is what would let the coverage manifest be pruned. Nobody has done it: 140 scripts at one `dotnet test` start-up each is hours of wall clock, well past what an agent ticket can hold. If that question becomes worth answering, give it a machine with time rather than a ticket - and merge the per-script reports with this tool, which already unions them.
- **Use one build.** Reports of different builds must not be added up; the tool refuses them (see *Merging* below).

## Run the report

```
python tools/coverage-report/coverage_report.py artifacts/coverage
python tools/coverage-report/coverage_report.py artifacts/coverage/part1 artifacts/coverage/part2 -o docs/coverage-report.md --json artifacts/coverage/rollup.json
python -m unittest discover -s tools/coverage-report               # its tests
```

One Cobertura file of this repository is about 24 MB whatever the run measured (its bulk is the line list, which does not grow with the number of tests), so a run of the whole suite gives roughly 24 MB per part - about 120 MB over the five parts the root `AGENTS.md` lists today. Measured 2026-11-21: 6.6 s and a 199 MB memory peak to read one of them, of which 8 MB is retained - the parse tree of each input is freed before the next is read, so the peak does not grow with the number of parts.

Each positional argument is a Cobertura file or a folder, which is searched recursively for `*.cobertura.xml`. Findings go to standard output, one per line, then a summary line; the report goes to standard output too unless `-o` names a file. A report or JSON file written with `-o`/`--json` is UTF-8 with CRLF line endings, as the repository’s tracked text files have.

| Option | Default | Meaning |
|---|---|---|
| `--areas FILE` | `areas.json` beside the script | the assembly lists, the area map and the exclusions |
| `--repo-root DIR` | the folder holding `.git` | what source paths are made relative to |
| `-o FILE` | standard output | where the Markdown goes |
| `--gap-below PCT` | `25` | below this line coverage an area is a **gap** |
| `--thin-below PCT` | `50` | below this (and not a gap) an area is **thin** |
| `--min-lines N` | `200` | an area with fewer coverable lines is never a gap, whatever its rate |
| `--untested-min-lines N` | `20` | a wholly untested file with fewer coverable lines is counted rather than listed under *Wholly untested files* |
| `--json FILE` | — | also write the rolled-up numbers as JSON, for a later pass to diff against |
| `--allow-unassigned` | off | print the report even when the map is incomplete (the exit code is still 1) |

Exit codes: **0** all well, **1** a finding, **2** bad arguments (a path that does not exist, thresholds that are not `0 <= --gap-below <= --thin-below <= 100`, an unreadable or malformed areas file, an input that is not well-formed XML). A well-formed document that is not a Cobertura report has no assembly on any list, so it exits 1 by that route.

## The two boundary checks

Both exist so that a new source folder or a newly converted project cannot quietly disappear from the report: the next person to add one is told to put it on the map rather than finding out months later that a subsystem was never in the numbers.

- `unassigned: <path> (<n> lines)` — a source file under `src/` in an engine assembly that no area and no exclusion claims.
- `unlisted assembly: <name> (<n> lines)` — an assembly in the input that none of the three assembly lists names.

Either one exits 1 and **suppresses the report**, because a rollup missing a subsystem reads as if that subsystem were fine - an assembly on no list is as absent from the areas and the totals as an unclaimed file is. `--allow-unassigned` prints the report anyway and still exits 1; that is the combination to use while finding out what a new run contains. The report it prints opens with a section naming every hole, so a published `docs/coverage-report.md` cannot read as complete when it is not. Exit 1 also when no input file was found, when no assembly in the input is on any list, and when two inputs disagree (below).

## Merging several runs

The suite is run in parts (five today, listed in the root `AGENTS.md`), so the tool takes several reports and merges them: the union keyed on assembly, class, source file and line number, with a line counted covered if **any** report hit it. Reports of one build agree exactly about which lines are coverable — verified over two independent runs where every class common to both had an identical line set. So a disagreement means the inputs were measured against *different* builds, which is what a stale report mixed with a fresh one looks like, and the tool prints `line sets differ: <assembly>/<class>` and exits 1 rather than produce a number nobody can trust. Delete the old results directory and collect again.

An assembly that one part loads and another does not is normal: the union covers it, and the report says how many input files were merged so a reader can tell a one-part run from a whole-suite one. An assembly on a list that no input measured shows as `not in this run`.

## The map: `areas.json`

Three assembly lists and the area table.

- `engine` — the assemblies the area map covers: the engine proper.
- `reported` — libraries, the client stack and the hosts. A sample library with an assembly of its own belongs here too (`Alphora.Shipping` is the one today): its source is under `samples/`, not `tests/`, so the tool does not drop it, and a run that loads it would otherwise find it on no list. Rolled up per assembly only; they need no area and are never "unassigned".
- `ignored` — dropped entirely: the test assemblies, the test harness, the two test-support libraries (`TestFramework.Coverage.Base`, `LibraryLoadingProbe`) and the layout tool. Anything whose source is under `tests/` belongs here rather than in `reported`: the tool drops `tests/` source before the assembly rollup, so a `reported` entry for it could only ever read `not in this run`.

The names are **assembly names as the collector prints them**, not project file names, so the server assembly is `AlphoraDataphorServer` with no dots and the MSSQL device library is `AlphoraMSSQLDevice`. Never guess one from its project or folder name: read it off the built output (`artifacts/bin/<project file name>/<config>/`, or the layout's `bin/` and `Libraries/`), or off the project's `<AssemblyName>`, which is where every name in these lists was checked. A name that is wrong anyway announces itself as an `unlisted assembly` finding, and `--allow-unassigned` is how you see the report while sorting it out.

An **area** is a named set of repository-relative path prefixes with forward slashes. A path ending in `/` is a folder prefix; anything else is one file. A class belongs to the area whose **longest matching path** claims its source file, so a single-file entry beats the folder entry that contains it — that is how `Security` owns `src/engine/DAE/Schema/Security.cs` while `Schema` owns the rest of that folder. The last several areas are deliberately broad folder prefixes that the specific ones carve pieces out of.

Classes whose source file is under `tests/` are dropped wherever they appear: test code is not the subject of the report.

**To add an area**, or to answer an `unassigned` finding: add the path to the area it belongs to, or add a new `{"name": …, "paths": [...]}` entry. Two areas may not claim the same path (the longest-prefix rule would have nothing to choose between them) and two areas may not share a name; the tool exits 2 if they do.

`excluded` is the allow-list: paths counted by the collector but deliberately left out of the areas, listed in a short table at the end of the report and never classified or ticketed. Each entry carries a `reason` and, where one exists, the `ticket` that owns it. Today: `artifacts/`, where the CoreWCF operation-invoker generator writes the source it generates into the server assembly, and the two REST leftovers nothing calls.

## What the numbers mean, and what they do not

- **Line coverage says a line ran. It does not say anything checked the result.** Several of the D4 coverage scripts assert nothing at all, and a script that executes a path without checking what it produced still colours that path green. Read a high rate as *exercised*, never as *verified*; read a low one as a real absence.
- **Line counts are not statement counts**, and they are not comparable across builds: the collector decides which lines are coverable and that moves with the code. Compare *rates* between runs of the same build. (Each line is listed twice in the file — once under its class and once under its method — so a naive count of `<line>` elements doubles the real figure; this tool counts distinct line numbers.)
- **An area's rate is a weighted average of its files**, so one large well-covered file can hide a wholly untested neighbour. The per-file table under each gap was not enough for that: a file at exactly 0 % sitting in an area that averages 66 % used to appear in no table in the report at all. **Wholly untested files** is the section that answers it - every file in an engine area with coverable lines and not one of them covered, worst first, each row naming the file's area and that area's rate so a zero inside a healthy area stands out. It searches every area, gaps included, so a file at 0 % in a gap area is printed there and in that gap's own table too; that keeps the section's claim unconditional. Its scope is the **engine areas only**: a file in a `reported` assembly, under an `excluded` path, or under `src/` that no area claims cannot appear in it, and the two boundary checks above are what catch that last case. Files below `--untested-min-lines` are counted in a closing sentence rather than listed, and when nothing qualifies the section says so in as many words rather than going silent.
- **A gap is a candidate for a ticket, not a ticket.** Some of this code is reachable only from a UI, from a device that needs a database server, or from a deployment path no test can stand up. The areas with the biggest uncovered counts come first because that is where reading is most likely to pay, not because they are automatically wrong.

The companion work item `test-coverage-gap-analysis` runs the whole suite under collection, publishes `docs/coverage-report.md` from it and files one ticket per gap worth one.

## Why CI does not collect this

See the `NOTE:` at the top of `coverage_report.py`. The original reason was that this suite has cases which assert on wall-clock behaviour (a session reclamation against a 5 s idle threshold; a `timeoutSeconds` entry in the coverage manifest) and nobody had run the whole suite under instrumentation, so there was no evidence either way.

That evidence now exists and it is favourable: `test-coverage-gap-analysis` ran all three parts under collection on 2026-11-21 with no failure of any kind, the reclamation case moved from 5.1 s to 5.4 s against a 30 s wait, and the cost was +16 % on the longest part. Collecting in CI is therefore a decision someone should take rather than a risk to avoid; it stays off only because no ticket has made the change. Whenever it is made the number must remain reported and never gated - nothing in CI should be able to fail a build on a coverage rate.
