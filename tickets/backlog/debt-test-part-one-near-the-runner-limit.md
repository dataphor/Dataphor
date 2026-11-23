description: The largest of the three chunks the automated test suite is run in now takes nearly nine minutes, and the tooling that runs it gives up at ten, so it needs to be cut into smaller chunks before it crosses the line.
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

Two runs a day apart agree within three seconds, so this is growth rather than one slow afternoon: about a minute and twenty seconds of the ten-minute allowance is left. Parts 2 and 3 are not close - both 3 m 4 s in the same review run.

## What is being asked for

Part 1 cut into pieces that each stay well under the limit, and `AGENTS.md`'s command list and timing figures updated to match - it is the file every agent reads before running the suite, so a split that is not recorded there has not happened. Parts 2 and 3 split the coverage scripts by the first letter of the script name; part 1 is a different shape, so the cut needs choosing rather than copying: by test project, by fixture name, or by separating the handful of fixtures that each start their own server (which is where the time goes) from the rest.

Whoever does it should re-measure before and after on the slow machine and record both, best of two runs, the way `AGENTS.md` already asks.

## Why it matters even though everything passes

A part that crosses the limit does not report a test failure - the ticket running it is killed, and its work is lost. That cost falls on whichever unrelated ticket happens to be running when the line is crossed, which is the worst moment to discover it.
