description: Run the whole test suite once with code-coverage collection switched on, publish the resulting report of what the engine's tests do and do not exercise, and file one ticket for each part of the engine that no test touches.
prereq: test-coverage-report-tool
architecture: docs/modernization-plan.md#ws3-test-suite
files: docs/coverage-report.md, docs/README.md, tools/coverage-report/coverage_report.py, tools/coverage-report/areas.json, tests/DAE.Server.Tests/coverage-manifest.json
difficulty: hard
----
## What this is

The second half of the plan's WS3 work item `test-coverage-gap-analysis`: "With coverage data: list engine areas with no script coverage (candidates: application transactions, server links, security, library upgrade scripts, streams) and file a ticket per area."

The suite reports 116 passing coverage scripts, but a pass count is not coverage: a script that executes a path without checking its result still counts as a pass, and nine scripts pass while asserting nothing at all. This ticket asks the inverted question — what in the engine is not reached by anything that passed — answers it with measured numbers, and turns the answer into work on the board.

`test-coverage-report-tool` builds the tool and the area map and documents the collect command; read its ticket (or, once it has landed, `tools/coverage-report/README.md`) before starting. This ticket runs the pass.

## Run the pass

Build first, then the three suite parts of AGENTS.md (*Agents run the suite in parts*) with collection added, **each as its own foreground command with no redirection** — the runner kills an agent that prints nothing for ten minutes, and a redirected command prints nothing:

```
dotnet build Dataphor.slnx
dotnet test Dataphor.slnx --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part1 -- "NUnit.Where=class != Alphora.Dataphor.DAE.Server.Tests.CoverageScripts"
dotnet test tests/DAE.Server.Tests --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part2 -- "NUnit.Where=test =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.CoverageScripts\.(Script\.)?(?!Script$)[A-Ra-r]'"
dotnet test tests/DAE.Server.Tests --no-build --collect:"Code Coverage;Format=cobertura" --results-directory artifacts/coverage/part3 -- "NUnit.Where=test =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.CoverageScripts\.(Script\.)?(?!Script$)[^A-Ra-r]'"
python tools/coverage-report/coverage_report.py artifacts/coverage -o docs/coverage-report.md --json artifacts/coverage/summary.json
```

`--collect` and the `--` run-settings arguments coexist; `--` stays last (verified 2026-11-21). `artifacts/` is git-ignored, so nothing from the run is left in the tree. Without collection the three parts took about 2.5, 3.5 and 6 minutes on the slow machine; collection adds roughly a tenth of the test time plus about nine seconds of fixed cost per part, so budget about 14 minutes of commands.

**Run all three parts.** A one-part report would understate every area that only the scripts reach and would make this whole pass misleading. If a part cannot be completed, stop and hand off rather than publish a partial report; the report is read as the record of what the suite covers.

**Watch what instrumentation does to the timing-sensitive cases** and write down what you saw: `AbandonedClientProcessTest` (a reclamation measured at 4.7–5.1 s against a 5 s threshold) and the manifest's `timeoutSeconds` entries are the ones that could behave differently under collection. This is the evidence nobody has yet, and it decides whether coverage can later be wired into CI. If a case fails only under collection, that is a finding about the measurement and not a defect in the suite: record it in the report and in the handoff, and re-run that part without collection to confirm.

## Publish the report

`docs/coverage-report.md`, from the tool's output, with a short hand-written head before the generated tables:

- the date, the git commit, the configuration (`Debug`), the machine class, and that the numbers come from one full three-part run;
- **what the numbers mean and do not mean**, in two or three plain sentences: a covered line is a line that ran, not a line whose result anything checked. Name the nine scripts the manifest records as asserting nothing (`Functions`, `LibraryFilesCoverage`, `LockAllocation`, `Register`, `RowAllocation`, `ScalarAllocation`, `SerializationDevices`, `TestGH37`, `Warnings`) as the clearest case of a pass that is not coverage;
- how to reproduce it (the commands above, one line pointing at `tools/coverage-report/README.md`);
- what instrumentation did to the run (above);
- the list of tickets this pass filed, so the report and the board can be read against each other.

Add a line to the *Project documents* list in `docs/README.md`. Leave the plan unedited — it already carries the requirement, and this ticket does not change it.

## File one ticket per gap

Each goes in `tickets/backlog/` (the gap is settled work of unsettled timing), with:

```
description: <plain sentence: what is untested and what could go wrong unnoticed>
architecture: docs/modernization-plan.md#ws3-test-suite
files: <the worst-covered files of the area, from the report>
tradeoffs: <one honest sentence on why a maintainer might decline or defer this>
```

and a body giving the area name, its measured line coverage and coverable line count, the three or four worst files with their numbers, and — the part that makes the ticket worth having — **the kinds of behaviour a new coverage script would exercise**, in terms of D4 that a later agent can write: statements, operators, system tables, error codes. Do not write the scripts here. Do not set `severity:`/`likelihood:` — these are not bug tickets.

Slug: `debt-<area>-coverage`, for example `debt-application-transactions-coverage`.

**Which areas get a ticket.** The tool classifies an area as a `gap` at under 25 % line coverage with at least 200 coverable lines. File for the gaps, **worst first by uncovered line count, at most eight tickets in this pass.** The cap is deliberate: eight tickets is a quarter's worth of coverage work, and a queue of twenty `debt-` tickets nobody picks up is worse than a report that names the rest. Every area the tool reports as `gap` or `thin` and that you did not ticket stays in the report with its numbers, which is enough for a later pass to pick it up; say in the handoff which gaps you left unticketed and why.

Before filing each one, run the site-claim grep from the ticket rules over the files you are about to name, and read around the worst-covered code for an accepted-tradeoff `NOTE:`. Two areas that resolve at the same code site are one ticket with two arms.

**Two tickets to file whatever the numbers say:**

- **`debt-tableindexer-coverage`.** The deleted hand-wired test list named a coverage script `TableIndexer` that does not exist, with the comment "Build this coverage"; `test-harness-inprocess` passed it to this ticket. The subject is the D4 **table indexer expression** (`MyTable[key]`), compiled in `src/engine/DAE/Compiling/Compiler.cs` around lines 12,610–12,715, with five error codes of its own: `AmbiguousTableIndexerKey` (105259), `UnresolvedTableIndexerKey` (105260), `InvalidTableIndexerKey` (105261), `TooManyTermsForImplicitTableIndexer` (105264) and `PotentiallyAmbiguousImplicitTableIndexer` (105275). It is not a data structure, and it is not the uncompiled `src/engine/DAE/Runtime.Data/Index.cs` that the name suggests. A new script should index by a declared key, by an implicit key, and provoke each of those five errors; error codes are a compatibility surface, so a script that pins them is worth more than its length. Carry the original intent forward verbatim in the ticket.
- **`debt-realsql-coverage`.** `Alphora.Dataphor.DAE.Language.RealSQL` measured 0 % over 2,846 lines on a short run and is live code, not dead: `SetLanguage('RealSQL')` selects it, Dataphoria's SQL editor uses it (`src/windows/Dataphoria/TextEditor/D4Editor.cs`) and a Frontend operator sets `QueryLanguage.RealSQL` (`libraries/Frontend/Source/Operators/Operators.cs`). Confirm the number from the full run before filing — if the full suite reaches it, file what the report actually says.

**The nine no-assertion scripts are evidence, not an area.** Their names say what their authors meant to cover and did not: `LockAllocation`, `RowAllocation` and `ScalarAllocation` (allocation and leak counting), `LibraryFilesCoverage`, `Register`, `SerializationDevices`, `Functions`, `Warnings`. Where an area ticket covers the same behaviour, name the script in that ticket as what the new script replaces. Whether the empty files themselves stay in the library is `test-pre-3-1-scripts-policy`'s call, not this one: do not edit, retire or delete a script here, and do not change `tests/DAE.Server.Tests/coverage-manifest.json`. The two scripts that ticket flagged for this one — `Exceptions` (every error code has a resource string) and `StreamAllocation` (stream leak counting) — belong in the security/error-code and stream tickets respectively as "what this script was for".

## Edge cases & interactions

- **An area at 0 % because its tests are elsewhere.** Several areas are covered by NUnit fixtures rather than D4 scripts (`ApplicationTransactionTest.cs`, `ServerLinkConnectionTest.cs`, `ConnectionSecurityModeTest.cs`, `LibraryLoadingTest.cs` all exist). The full run includes them — part 1 — so their contribution is in the numbers. Do not file a ticket saying "nothing tests X" when a fixture does; say what the fixture reaches and what it leaves, from the per-file list.
- **An area that is thin because its code is unreachable in Phase 1.** The map's exclusions already cover the one case found (`REST`). If the full run turns up another — a folder at 0 % whose callers are all in `legacy/` or behind a removed transport — add it to `areas.json`'s `excluded` list with its reason in the same commit instead of filing a coverage ticket for code that is going to be deleted, and append an arm to `tickets/backlog/debt-ws12-hygiene-remainder.md` if nothing owns its removal. Say which in the handoff.
- **The debugger.** `src/engine/DAE/Debug/` measured near 0 % on a short run and is reachable (the IDE drives it, and `DebugLibrary.d4` and `DebugTest.d4` exist, the latter an infinite loop by design that the manifest stops after five seconds). It is a legitimate gap; if it makes the top eight, the ticket must say that a coverage script for it has to avoid the thing `DebugTest` does.
- **Per-script coverage.** The collector can be pointed at one test at a time, which would eventually answer "does script X cover anything the others do not" and let the manifest be pruned. Out of scope here. Record it as a `NOTE:` tripwire in `tools/coverage-report/README.md` — one run per script over 140 scripts is hours, so it is a question for a machine with time, not for this pass.
- **A second pass.** `--json` writes the rolled-up numbers; keeping that file is how a later pass shows movement. `artifacts/` is git-ignored, so decide: either paste the headline numbers into `docs/coverage-report.md` (which this ticket does anyway) and let the JSON go, or say in the report where a future pass should put it. Do not check a generated JSON file into `docs/`.
- **Compatibility and migration notes.** Nothing in this ticket changes a compatibility surface and nothing an application could notice; no `docs/migration-notes.md` entry. If you find yourself wanting to change engine code to make something measurable, stop — that is a different ticket.
- **Source hygiene.** `docs/coverage-report.md` is new: CRLF, no byte-order mark, no hard wrapping (one paragraph per line). `docs/README.md` is edited in place, so match what is there.
- **A pre-existing failure in the suite.** Follow the *Pre-existing test failures* protocol: check `tickets/.pre-existing-known.md` first, write `tickets/.pre-existing-error.md` if it is new, and never skip or loosen a test. A failure that only appears under instrumentation is not a pre-existing failure — it is a finding about the measurement, as above.

## TODO

- Build, then run the three suite parts with collection, each as its own foreground command; note the timing and anything the instrumentation changed.
- Run the report tool over `artifacts/coverage`; fix any `unlisted assembly` or `unassigned` finding by editing `tools/coverage-report/areas.json` (the library assembly names in it are unverified guesses), and re-run until it exits 0.
- Write `docs/coverage-report.md` with the hand-written head and the generated tables; add the line to `docs/README.md`.
- Run the site-claim grep for each area's files, then file the backlog tickets: `debt-tableindexer-coverage`, `debt-realsql-coverage` (numbers confirmed from the full run), and the top gaps up to eight tickets total.
- In the handoff: the per-assembly headline numbers, every gap left unticketed and why, what instrumentation did to the timing-sensitive cases, and any area map or exclusion change you had to make.
- Delete this ticket file on the way out.
