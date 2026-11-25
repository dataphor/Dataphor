description: The largest of the three chunks the automated test suite is run in now takes nearly ten minutes, and the tooling that runs it gives up at ten, so it needs to be cut into smaller chunks before it crosses the line.
architecture: docs/modernization-plan.md#ws3-test-suite
files: AGENTS.md
tradeoffs: Nothing is broken today and the figure moves around by a minute or more between identical runs, so a maintainer may reasonably wait for a run that actually hits the limit - at the cost of that run being a lost ticket rather than a measurement.
----
## The limit and the measurements

The root `AGENTS.md` tells an agent to run the suite as three commands rather than one, because the tooling that runs tickets gives up on a command that has printed nothing for ten minutes. The first of the three - everything except the D4 coverage scripts - is the one that grows, because new fixtures land in it. `AGENTS.md` sets its own threshold: "If a part grows past about 8 minutes, split it further", and also warns that a single slow run is noise until it repeats.

It has now repeated, on the same machine:

| When | Part 1 | Source |
|---|---|---|
| 2026-11-21 | 5 m 35 s best, 7 m 32 s worst over several runs | the figures recorded in `AGENTS.md` |
| 2026-11-23 | 8 m 43 s | `debt-rollback-leaves-catalog-as-it-was`, implement |
| 2026-11-23 | 8 m 40 s | `debt-rollback-leaves-catalog-as-it-was`, review, with 22 more cases worth about 4 s |
| 2026-11-24 | 9 m 48 s | `bug-an-argument-is-orphaned-when-a-later-one-raises`, review, with one more case worth under a second |

The three runs are growth rather than one slow afternoon, and the newest leaves **twelve seconds** of the ten-minute allowance. It was run twice in that review - 9 m 54 s and 9 m 48 s - so it is not a single slow run; part 1's case count is unchanged from 2026-11-23 bar one, so the jump of over a minute is not new tests. Parts 2 and 3 are still not close - 3 m 29 s and 3 m 11 s in the same review run.

## What is being asked for

Part 1 cut into pieces that each stay well under the limit, and `AGENTS.md`'s command list and timing figures updated to match - it is the file every agent reads before running the suite, so a split that is not recorded there has not happened. Parts 2 and 3 split the coverage scripts by the first letter of the script name; part 1 is a different shape, so the cut needs choosing rather than copying: by test project, by fixture name, or by separating the handful of fixtures that each start their own server (which is where the time goes) from the rest.

Whoever does it should re-measure before and after on the slow machine and record both, best of two runs, the way `AGENTS.md` already asks.

## Why it matters even though everything passes

A part that crosses the limit does not report a test failure - the ticket running it is killed, and its work is lost. That cost falls on whichever unrelated ticket happens to be running when the line is crossed, which is the worst moment to discover it.

## Gardener note, 2026-11-25

The limit has now been crossed: the gardener's board-drain run at `15a617f0` took **10 m 9 s** for part 1 (DAE.Server.Tests 956 passed / 33 skipped, 10 m 9 s; Common.Tests 2 s), against 3 m 29 s and 3 m 13 s for parts 2 and 3, all green. Run by hand from the shell, so nothing killed it, but an agent running it would have been. The tradeoff's condition (wait for a run that hits the limit) is met; promoted ahead of WS7 so its tickets get a part 1 that fits.
