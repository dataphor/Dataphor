description: The largest of the three chunks the automated test suite is run in had grown past the ten-minute point where the tooling gives up, so it was cut into three smaller chunks and the new commands and timings were written into the file every agent reads before running tests.
architecture: docs/modernization-plan.md#ws3-test-suite
files: AGENTS.md, docs/coverage-report.md, tickets/rules/10-dataphor.md, tools/coverage-report/README.md
----
## What landed

Documentation only. Nothing under `src/` or `tests/` changed and no test was added, cut or edited - the thing under change is the list of commands an agent is told to run, so running those commands is the check.

The suite's part 1 (every NUnit fixture, i.e. everything but the D4 coverage scripts) was one `dotnet test` taking 10 m 9 s on the slow machine, past the ticket runner's 10-minute idle limit. It is now three commands cut by the first letter of the test class's name, so the *Agents run the suite in parts* section of the root `AGENTS.md` lists five commands rather than three. The two coverage-script parts are unchanged.

- **`AGENTS.md`** - "these three commands" became "these five commands"; the fence holds the three new fixture commands followed by the two unchanged coverage-script ones; the paragraph after it carries the new per-part figures, the partition-by-construction argument, a split trigger lowered from 8 to 6 minutes, and the next cut already chosen (`Ca`, the three `Catalog*` fixtures). The three standing warnings were kept: that one measurement is not a part's cost, the 2026-11-21 spread figures with best-of-two when comparing, and why the coverage filters match `test` rather than `name`.
- **`docs/coverage-report.md`** - the *Reproducing it* fence's single part-1 line became three, writing into `artifacts/coverage/part1a`, `part1b` and `part1c`; the cross-reference above it is now dated rather than claiming three parts. The genuinely dated measurement figures in that file (the 2026-11-21 collected run) were left as they were.
- **`tickets/rules/10-dataphor.md`** - the three count-bearing phrases name the part commands without a count, so the next split need not find this file.
- **`tools/coverage-report/README.md`** - fixed in the review pass; see below.

## Review findings

**Read first, then checked.** The implement commit (`ticket(implement): debt-test-part-one-near-the-runner-limit`) was read as a diff before its handoff. For a change whose entire content is a list of commands, the check is running them, and the check that matters is the arithmetic: a filter that quietly matches nothing still reports a green run, so only the case counts show that the three pieces cover the old part with nothing dropped and nothing run twice.

**Verified by a third independent round.** `dotnet build Dataphor.slnx` - 0 errors, 144 warnings. Then the three new commands, copied from `AGENTS.md`, each as its own foreground command:

| Piece | Recorded in `AGENTS.md` | This review's round | Cases |
|---|---|---|---|
| part 1a | 3 m 11 s | 3 m 09 s | `DAE.Server.Tests` 494 passed / 19 skipped, `Common.Tests` 18 passed / 0 skipped |
| part 1b | 3 m 01 s | 3 m 06 s | 252 passed / 14 skipped |
| part 1c | 4 m 04 s | 4 m 06 s | 210 passed / 0 skipped |

0 failures. 494 + 252 + 210 = 956 passed and 19 + 14 + 0 = 33 skipped, which is exactly what the single part-1 command reported on the board-drain run at `15a617f0` (956 / 33, plus `Common.Tests`' 18) - so the three filters partition the old part. Every timing is within 5 s of the figure the file records, across three rounds on two days, so the recorded numbers are sound.

**The partition argument was checked against the code, not just read.** The class names declared under `tests/DAE.Server.Tests` were listed and bucketed by hand: all start with an upper-case letter, A and B and D-N land in part 1b, C in part 1c, O-Z in part 1a, and `Alphora.Dataphor.Common.Tests` fails the namespace clause and so lands in part 1a. `CoverageScripts` is a `C` and part 1c's 210 cases (not 370-odd) are the evidence that the roughly 160 script cases did not leak in. The `||` in part 1a is the first in the repository and it behaves: part 1a selected `Common.Tests` and the `O`-`Z` fixtures together.

**Found and fixed in this pass (minor).**

- `tools/coverage-report/README.md` carried three standing, undated statements that the suite is run in three parts - one of them a derived magnitude, "a three-part run gives roughly 75 MB of XML", which the new five-command reproduce recipe in `docs/coverage-report.md` makes wrong by about 45 MB. That file was not on the implement ticket's list but it is the companion of the file whose reproduce recipe changed, and it is the one a reader consults before collecting. The three statements now name the count-free "in parts (five today, listed in the root `AGENTS.md`)" and the disk figure is stated per part. The dated 2026-11-21 cost measurement in the same file ("the three parts at 13 m 40 s together") was deliberately left alone. `python -m unittest discover -s tools/coverage-report` - 14 tests, OK.
- `AGENTS.md`'s partition argument claimed a fixture added "in a new test project" still lands in the first part. Only projects in `Dataphor.slnx` run at all - `tests/Frontend.Client.WPF.Tests` is a tracked project outside the solution, and although it is a legacy WPF sample application with no NUnit test attribute at all, the claim as written was load-bearing and overstated. It now reads "in a new test project of the solution".

**No major findings, and nothing filed.** The change is three regular expressions and the prose around them; the regular expressions were checked against the whole class inventory and reproduce the old part's counts exactly, and the prose's every claim that could be checked was checked. There was no finding above the filing bar, so no ticket was created - not because none was looked for.

**No tripwire added.** The implementer parked one, correctly, in the `AGENTS.md` section itself rather than as a ticket: the 6-minute split trigger and the pre-chosen `Ca` cut now sit in the paragraph an agent reads before running the suite, which is where the next person to meet a slow part will be looking, and there is no code site for it. Nothing new of that kind was noticed.

**No test added or cut.** There is no code here to test; the project's "a ported subsystem owes a characterization test" rule has no subject in a documentation change, and the suite's own case counts are the assertion.

**Two things deliberately not re-verified.**

- The two coverage-script commands are unchanged text. Their recorded figures are the 2026-11-21 measurements, and the board-drain run two hours before the implement stage ran both green (3 m 29 s, 72 passed / 11 skipped, and 3 m 13 s, 52 / 12). Re-running them would have added about 7 minutes to confirm lines that did not change.
- The next-cut hint's "140 s of that part's 224 s" split between the `Catalog*` fixtures and the rest of the `C` part was not re-measured. It is a forward-looking hint, not a shipped command, and whoever performs that split will measure before and after anyway.

**One stale statement left alone, by ownership.** `tickets/.garden-report.md` opens with a status block dated 2026-11-20 that says agents run the suite in three parts and points at the root `AGENTS.md` for the commands. It is the tending agent's own log, it is dated, and it already names the file that is now correct, so it was not edited from inside a ticket.

## Compatibility and project obligations

No `docs/architecture/` page was touched, so no `tools/docs-size-check/check_size.py` run was owed. No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension could notice changed, and `.github/workflows/build.yml` still runs one unfiltered `dotnet test Dataphor.slnx`, so CI is unaffected by every filter here. All four files are CRLF with no byte-order mark and were rewritten in binary preserving both; no hard wrapping.
