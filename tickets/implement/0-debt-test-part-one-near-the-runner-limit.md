description: The largest of the three chunks the automated test suite is run in has grown past the ten-minute point where the tooling gives up, so cut it into three smaller chunks and record the new commands and timings in the file every agent reads before running tests.
architecture: docs/modernization-plan.md#ws3-test-suite
files: AGENTS.md, docs/coverage-report.md, tickets/rules/10-dataphor.md
difficulty: easy
----
## What was measured, and what the split is

The design is settled: part 1 becomes three commands, cut by the first letter of the test class name, and the figures below were measured on the slow machine at `4715e9cc` in Debug on 2026-11-25 (one round; the implement stage owes the second round, because this project's rule is best of two).

The whole of part 1 was 10 m 9 s in the gardener's board-drain run earlier the same day — past the limit. The three replacement commands, run one after another on the same build:

| Piece | Wall clock | Cases |
|---|---|---|
| part 1a | 3 m 11 s | `DAE.Server.Tests` 494 passed / 19 skipped, `Common.Tests` 18 passed |
| part 1b | 3 m 01 s | 252 passed / 14 skipped |
| part 1c | 4 m 10 s | 210 passed / 0 skipped |

956 passed and 33 skipped between them, which is exactly what the one-command part 1 reported in the drain run, so the three filters cover it with nothing run twice and nothing dropped. The three together cost about 12 s more wall clock than the single command did, which is the price of two extra test-host starts.

The commands (Bash syntax, as the rest of that section already is):

```
dotnet test Dataphor.slnx --no-build -- "NUnit.Where=class !~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.' || class =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.[^A-N]'"
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.[ABD-N]'"
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.C' && class != Alphora.Dataphor.DAE.Server.Tests.CoverageScripts"
```

## Why this cut and not another

**Why three pieces rather than two.** Two pieces would be about 5 minutes each. The recorded spread on this machine is up to about 1.35× a part's best figure for reasons that have nothing to do with the change being tested, which puts a 5-minute piece at nearly 7 minutes on a bad afternoon — which is where part 1 already sat on 2026-11-21, four days before it crossed the limit. Three pieces put every piece in the same 3-to-4-minute band as the two coverage-script parts, so the whole suite has one margin rather than one part living closer to the edge than the others.

**Why by class-name letter rather than by test project or by "fixtures that start a server".** By project is not available: `Dataphor.slnx` holds exactly two test projects and `Common.Tests` is 18 cases in 1 s, so everything that costs anything is in `DAE.Server.Tests`. Separating the fixtures that start their own server would balance better, but there is no name that tells them apart — it would need a category attribute on each fixture plus a rule that every new fixture carries one, and a fixture whose author forgot would end up in whichever bucket the complementary filter happened to catch. Letter ranges need no source change, give the same automatic home to a new fixture that the coverage-script parts already give to a new script, and are the style the section already uses.

**Why the odd-looking boundaries.** Cost is concentrated, not spread: measured test-body time per fixture, heaviest first, was `CatalogStoreTest` 91 s, `ServerLinkConnectionTest` 40 s, `SamplesCorpusTest` 38 s, `ConcurrentDDLTest` 36 s, `CatalogStoreConcurrencyTest` 35 s, `NativeSessionKeepAliveTest` 32 s, `LibraryAttachTest` 29 s, `ConnectionKeepAliveTest` 28 s, `PerfBaselineTest` 23 s, `CoreWcfProtocolTest` 20 s, `HostBindingHookTest` 17 s, `MemoryDeviceConcurrencyTest` 15 s, `CatalogRollbackSnapshotTest` 14 s, `NativeSessionCleanupTest` 13 s, `AbandonedClientProcessTest` 12 s — 443 s of the 489 s total in fifteen classes, with the remaining fifty-three classes sharing 46 s. (A further ~140 s of part 1's wall clock is in no test's own duration at all: it is the one-time setup of each fixture that starts a server, which is why a piece's cost tracks how many server-starting fixtures it holds as much as what its tests do.) Letter `C` alone — `Catalog*`, `Connection*`, `Concurrent*`, `CoreWcf*` — is 224 s of that, so it has to be a piece of its own; `A`–`N` minus `C` is one more; everything else, plus every test project that is not `DAE.Server.Tests`, is the third.

**The pieces partition the suite by construction, which is the property to keep.** Every test class either is outside the `Alphora.Dataphor.DAE.Server.Tests` namespace (part 1a), or is inside it and starts with a character outside `A`–`N` (part 1a), or with `C` (part 1c, less `CoverageScripts`, which parts 2 and 3 own), or with one of `A`, `B`, `D`–`N` (part 1b). A fixture added in a new namespace, in a new test project, or with a lower-case or underscore-led class name still lands in part 1a rather than nowhere — which is why part 1a is the one command that runs the whole solution and why its last clause is the negated range `[^A-N]` rather than `[O-Z]`.

## What to change

The split has not happened until `AGENTS.md` says so: it is the file every agent reads before running the suite. Three files hold the command list or count it.

- **`AGENTS.md`, the *Agents run the suite in parts* section** (around lines 75-84). Its opening sentence says "these three commands", the fence holds three, and the paragraph after it says "The first runs everything except the coverage scripts; the other two split the scripts by the first letter of their names" and then gives the 2026-11-21 figures. All of that changes: five commands, part 1 replaced by 1a/1b/1c above with the coverage-script parts untouched, and the new figures. Keep the existing warnings as they are — that a single measurement is not a part's cost, that a part can take half again as long for unrelated reasons, that you take the best of two when comparing, and the explanation of why the coverage filters match `test` and not `name`.
- **`docs/coverage-report.md`, the *Reproducing it* fence** (around lines 30-38). It mirrors the same commands with the Cobertura collector added and says "run the three parts". Replace its part-1 line with the three new commands, each with its own `--results-directory artifacts/coverage/part1a`, `part1b`, `part1c`; the collector's arguments stay before the `--` and `coverage_report.py` merges whatever subdirectories it finds, so nothing else there changes.
- **`tickets/rules/10-dataphor.md`** (lines 5, 8 and 9) says "all three parts", "the three part commands" and "the three parts". Reword to the part commands without a count, so the next split does not have to find this file again.

Two things deliberately do not change. **CI** (`.github/workflows/build.yml`) runs `dotnet test Dataphor.slnx` whole and knows nothing about the parts, so it is not touched; the parts exist only because an agent's shell shows nothing until a command ends. And the **dated measurement records** in `docs/modernization-plan.md` that mention "suite part 1" or "part 3" are the record of what was measured when, not instructions — leave them alone.

## Replace the eight-minute trigger with six

`AGENTS.md` currently ends that section with "If a part grows past about 8 minutes, split it further by letter." That trigger did not fire in time: part 1 was 8 m 43 s on 2026-11-23 and 10 m 9 s on 2026-11-25, so the eight-minute mark bought less than two days of board time, and the ticket that happens to be running when a part crosses is killed rather than told. Lower it to **about 6 minutes** for any one part, and say what the next cut is so the agent that meets it does not have to re-derive this: part 1c splits at `Ca` — `class =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.Ca'` holds the three `Catalog*` fixtures, which are 140 s of its 224 s, and `class =~ '^Alphora\.Dataphor\.DAE\.Server\.Tests\.C(?!a)'` with the `CoverageScripts` exclusion holds the rest.

Also worth one sentence in that section, because it is the question a reader will have about a filter they are about to trust: the five filters partition the suite by construction and CI runs the whole suite on every push, so a mistake in one of them costs false confidence in a local run, not lost coverage.

## Edge cases & interactions

- **Completeness.** The check is arithmetic, and it is how the pieces were verified above: the three pieces' pass and skip counts must add to the one-command part 1's counts (956 passed, 33 skipped on this build, plus `Common.Tests`' 18). Verified by running them, not by inspection — a filter that quietly matches nothing still reports a green run.
- **A piece that matches no test at all.** Not reachable today (all three match hundreds of cases) and not something to design around, but if a future filter edit leaves a piece empty, `dotnet test` can report that as a failure rather than a pass; noticing it is what the arithmetic check above is for.
- **`CoverageScripts` is a `C`.** Part 1c is the only piece whose range would otherwise pull the ~160 coverage-script cases in, which is why it keeps the `class != …CoverageScripts` clause that part 1 used to carry. Verified by its case count: 210, not 370-odd.
- **Quoting.** The commands are Bash: single quotes inside the double-quoted `NUnit.Where` argument. PowerShell needs different quoting, and the section already says Bash syntax — keep that, and check the commands as written actually run before recording them.
- **`||` and `&&` in the NUnit filter.** Part 1a is the first command in the repository to use `||` in a `NUnit.Where` expression. It works (verified: it selected `Common.Tests` and the `O`-`Z` fixtures together), but it is the clause most likely to be mistyped — re-run it rather than trusting the edit.
- **Code-coverage collection.** The collector arguments must stay before the `--`; with three part-1 directories instead of one, `coverage_report.py artifacts/coverage` still merges them because it walks the directory. Verified by inspection of the tool's README rather than by a collected run, which is not worth the wall clock here.
- **Nothing in `src/` or `tests/` changes**, so no test is added or edited and no characterization test is owed: the behaviour under change is the documented command list, and running the commands is the check.

## TODO

- `dotnet build Dataphor.slnx` (expect 0 errors; the warning count was 144 at `4715e9cc`).
- Run the three new part-1 commands as written above, each as its own foreground command with no redirection, and record each one's wall clock and its passed/skipped counts. With the round recorded in this ticket that gives best of two, as the project's rule asks.
- Check the arithmetic: the three pieces' counts add to 956 passed / 33 skipped for `DAE.Server.Tests` plus 18 passed for `Common.Tests`. If a count is off, the filters are wrong — say so and fix the filters, do not record the figures.
- Rewrite `AGENTS.md`'s *Agents run the suite in parts* section: five commands in the fence, the new per-piece figures (best of two), the partition-by-construction sentence, the six-minute trigger and the pre-decided next cut for part 1c. Preserve the existing noise/variance guidance and the `test` versus `name` explanation.
- Update the `docs/coverage-report.md` *Reproducing it* fence to the five commands with `part1a`/`part1b`/`part1c` result directories.
- Reword `tickets/rules/10-dataphor.md` lines 5, 8 and 9 so they name the part commands without a count.
- Leave `.github/workflows/build.yml` and the dated figures in `docs/modernization-plan.md` alone.
- Source hygiene: all three files are Markdown — preserve each file's line endings and byte-order-mark state, and no hard wrapping (one paragraph per line).
- No entry in `docs/migration-notes.md`: nothing an application, a deployment script or a C# extension could notice changes.
