description: The largest of the three chunks the automated test suite is run in had grown past the ten-minute point where the tooling gives up, so it was cut into three smaller chunks and the new commands and timings were written into the file every agent reads before running tests.
architecture: docs/modernization-plan.md#ws3-test-suite
files: AGENTS.md, docs/coverage-report.md, tickets/rules/10-dataphor.md
difficulty: easy
----
## What landed

Documentation only. Nothing under `src/` or `tests/` changed, no test was added or edited, and no code path moved — the thing under change is the list of commands an agent is told to run, so running those commands is the check.

The suite's part 1 (every NUnit fixture, i.e. everything but the D4 coverage scripts) was one `dotnet test` taking 10 m 9 s on the slow machine, past the ticket runner's 10-minute idle limit. It is now three commands cut by the first letter of the test class's name, so `AGENTS.md`'s *Agents run the suite in parts* section lists five commands rather than three. The two coverage-script parts are untouched.

- **`AGENTS.md`** (lines 75-85): "these three commands" → "these five commands"; the fence now holds the three new fixture commands followed by the two unchanged coverage-script ones; the paragraph after it was rewritten with the new per-part figures, the partition-by-construction argument, the lowered 6-minute split trigger and the pre-chosen next cut (`Ca`). The three warnings the ticket asked to keep are kept verbatim in substance: that one measurement is not a part's cost, the 2026-11-21 spread figures, best-of-two when comparing, and why the coverage filters match `test` and not `name`.
- **`docs/coverage-report.md`** (*Reproducing it* fence): the single part-1 line became three, with `--results-directory artifacts/coverage/part1a`, `part1b`, `part1c`; collector arguments stay before the `--`. "run the three parts" → "run the parts".
- **`tickets/rules/10-dataphor.md`** (lines 5, 8, 9): the three count-bearing phrases now name the part commands without a count, so the next split need not find this file.

**One edit beyond the ticket's list, deliberately.** `docs/coverage-report.md` line 16 said "The three parts are the ones the root `AGENTS.md` describes under *Agents run the suite in parts*" — a cross-reference into the section this ticket rewrote, so it became a pointer to something that no longer says three. It is now dated ("described … when this was collected — that section now splits the first of them three ways, so it lists five parts rather than three"). The three genuinely dated measurement figures in that file (lines 13, 52, both about the 2026-11-21 collected run) were left alone, as were `.github/workflows/build.yml` and the dated "suite part 1"/"part 3" figures in `docs/modernization-plan.md`.

## How it was verified, and what a reviewer should re-check

`dotnet build Dataphor.slnx` — 0 errors, 144 warnings (the count the ticket predicted at `4715e9cc`).

Then the three new commands, each as its own foreground command, copied from what is now in `AGENTS.md` — verified byte-identical to the file's lines 78-80 afterwards:

| Piece | Round 1 (in the plan ticket) | Round 2 (this stage) | Best | Cases |
|---|---|---|---|---|
| part 1a | 3 m 11 s | 3 m 13 s | 3 m 11 s | `DAE.Server.Tests` 494 passed / 19 skipped, `Common.Tests` 18 passed / 0 skipped |
| part 1b | 3 m 01 s | 3 m 01 s | 3 m 01 s | 252 passed / 14 skipped |
| part 1c | 4 m 10 s | 4 m 04 s | 4 m 04 s | 210 passed / 0 skipped |

Two rounds agree within 9 s on every piece, which is the project's best-of-two rule satisfied. The figures recorded in `AGENTS.md` are the best-of-two column.

**The arithmetic check passed, and it is the check that matters here.** 494 + 252 + 210 = 956 passed, and 19 + 14 + 0 = 33 skipped — exactly the one-command part 1's counts from the gardener's drain run, plus `Common.Tests`' 18. So the three filters cover part 1 with nothing run twice and nothing dropped. A reviewer who wants to re-confirm the split should re-run this arithmetic rather than read the regexes: a filter that quietly matches nothing still reports a green run, so the counts are the only real evidence.

The three pieces together cost about 11 s more wall clock than the single command did (two extra test-host starts), matching the plan ticket's estimate.

The two coverage-script commands were **not** re-run this stage: they are unchanged text, their last measurement (2026-11-21, 3 m 28 s and 3 m 48 s) is what the section now records for them, and re-running them would have added ~7 minutes to confirm a line that did not change. That is the one deliberate gap in the verification.

## Things worth a reviewer's eye

- **The `||` clause in part 1a** is the first `NUnit.Where` expression in the repository to use `||`. It works — the run selected `Common.Tests` and the `O`-`Z` fixtures together, which is what the 18 + 494 split shows — but it is the clause most likely to be broken by a careless later edit. Read it against the counts, not by eye.
- **`CoverageScripts` is a `C`.** Part 1c keeps the `class != …CoverageScripts` clause that the old part 1 carried; its 210 cases (not 370-odd) is the evidence that the ~160 script cases did not leak in.
- **Quoting is Bash.** Single quotes inside the double-quoted `NUnit.Where` argument. The section already declares Bash syntax and that was kept; the commands were run through the Bash tool exactly as written, not retyped for PowerShell.
- **Part 1a's skip listing prints more lines than it counts.** The run printed 19 skipped `.d4` corpus files plus 3 skipped sample-corpus cases, but reported `Skipped: 19`. That discrepancy is pre-existing (the drain run's part 1 reported the same 19), is in VSTest's reporting rather than in anything this ticket touched, and the three pieces' reported counts add up correctly, so it was not chased. Worth knowing before a reviewer reads the listing and the total as disagreeing.
- **Source hygiene:** all three files are CRLF with no byte-order mark, and were rewritten in binary preserving both; `file` confirms each is unchanged in encoding and line endings. No hard wrapping — one paragraph per line.
- No `docs/architecture/` page was touched, so no `tools/docs-size-check/check_size.py` run was owed. No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension could notice changed.

## Review findings (from this stage)

- **Tripwire, parked in `AGENTS.md` itself rather than as a ticket.** The 6-minute trigger and the pre-chosen `Ca` cut are now in the section an agent reads before running the suite, which is where the next person to meet a slow part will actually be looking. There is no code site for it. Nothing else was noticed that needed parking.
