description: Deleting one row at a time from a table with the "delete the first row" form of the delete statement no longer gets slower the bigger the table is. One line in the in-memory device decided it; a 10,000-row drain that needed about a quarter of an hour now takes 1.3 seconds.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, tests/DAE.Server.Tests/QuotaDeleteCostTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Server.Tests/PerfBaselineTest.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/goldens/perf/perf-baseline.json, docs/perf-baseline.md, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
----
## What changed

**The fix is one clause removed**, `src/engine/DAE/Device.Memory/MemoryDevice.cs:86` (was line 79): `InternalPrepare` no longer requires `plan.Plan.CursorContext.CursorType != CursorType.Static` before offering an ordered scan over an index the device already keeps. A comment at the site says why: the device knows what it can serve, the requested cursor type says what the *consumer* needs and nothing about what sits in between, and where a snapshot is really required the engine already makes one itself with `Compiler.EmitCopyNode` above whatever has narrowed the row count.

The plan shape, read off `Diagnostics.ShowPlan` before and after, is the whole of the change. `delete T return 1 by { ID }` was `DeleteNode(QuotaNode(OrderNode[DeviceSupported=False](BaseTableVarNode)))` - the engine's `OrderTable` materializing and sorting every row, once per statement - and is now `DeleteNode(CopyNode(QuotaNode(OrderNode[DeviceSupported=True, Device=System.Temp](BaseTableVarNode))))`: the device reads in order, the quota narrows to one row, the copy snapshots that one row.

**Nothing changed at `ParameterNode`**, and that was the one open question the ticket carried. A `NOTE:` at `src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs:1034` records why, and it was settled by reading plans rather than by argument - a scratch fixture on `InProcessTestFixture` dumped them through `Diagnostics.ShowPlan` and was then deleted:

- A genuine `var` table parameter leaves **no `ParameterNode` to bind**: `InstructionNodeBase.DetermineDataType` (`src/engine/DAE/Runtime.Instructions/InstructionNode.cs:165`) removes it and requires what remains to be a `StackReferenceNode`, which is a leaf. `Test(var ATable)` on a local table variable plans as `CallNode(StackReferenceNode[ByReference=True])` - there is no table expression under it for any device to serve. A base table var cannot be passed by reference at all: `SPTVar(var SPT)` fails to compile with "Constant data object cannot be passed by reference to parameter".
- What *does* survive binding is only `var` written on an operand the resolved operator declares `in`. That compiles (`SPTIn(var SPT)` plans as `CallNode(ParameterNode(BaseTableVarNode))` and evaluates), and such an argument was never a snapshot: the engine hands the callee the live `BaseTableVarNode`, as it did before this change. The only shape this change touches there is `SPTIn(var (SPT return 1 by { ID }))`, where the narrowing now happens over a device scan rather than over a sort - the same trade the whole ticket is about.

So the fallback the ticket described (scoping the change to `QuotaNode.InternalBindingTraversal` instead) was not needed and was not taken.

## The reproduction test, and what it is worth

`tests/DAE.Server.Tests/QuotaDeleteCostTest.cs` - one case, `DrainingATableOneRowAtATimeCostsTheSamePerRowAtEverySize`. It warms the engine with a throwaway 50-row drain, then drains 250 rows and 1,000 rows (each twice, counting the cheaper) and asserts that one row of the larger drain costs **at most twice** one row of the smaller. A ratio, never a time, so the case says nothing about how fast the machine is.

Validated by putting the device's old rule back, rebuilding and running it: **it fails**, ratio 5.2 (415 ms and 8,665 ms for the two drains), and the failing run costs 19 s. With the fix it passes in 0.4 s with ratios 0.95, 0.98, 0.98, 1.03, 1.12 and 1.18 over six runs.

That is the only test added. The change owes no other: it is one line of device plan selection, and everything that depends on it is exercised by the existing suite (below).

## How to validate

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter Name=DrainingATableOneRowAtATimeCostsTheSamePerRowAtEverySize --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=Timings|Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline" --logger "console;verbosity=normal"
DATAPHOR_PERF_BASELINE=dev-slow dotnet test tests/DAE.Server.Tests --no-build --filter Name=TheTimingScriptsRunAndTheirNumbersMatchTheBaseline --logger "console;verbosity=normal"
```

To see the behaviour by hand rather than through a test, any D4 session will do: `create table T in System.Temp { ID : Integer, key { ID } };`, fill it, then `while exists (T) do delete T return 1 by { ID };` and watch it finish in milliseconds per hundred rows rather than seconds.

## The whole suite

Built from `Dataphor.slnx` in Debug, then the three parts the root `AGENTS.md` gives, each as its own foreground command:

- part 1 (everything but the coverage scripts): **565 passed, 0 failed** in `DAE.Server.Tests`, and 13 passed in `Common.Tests`.
- part 2 (coverage scripts A-R): **67 passed, 11 skipped, 0 failed**.
- part 3 (the rest): **50 passed, 12 skipped, 0 failed** - and `Timings` is one of the passes, in 5 s including engine start, where before this change it was a recorded known failure reported as Skipped. Part 3 therefore went from 49 passed / 13 skipped to 50 / 12, which is the manifest change showing up in the counts.

No pre-existing failures were met, so `tickets/.pre-existing-error.md` was not written.

## Measurements, and whose they are

Measured on this machine (one slow development laptop, the `dev-slow` label's machine class, Debug, 2026-11-22):

| What | With the defect | Now |
|---|---|---|
| 250-row drain | 415 ms | 14-17 ms |
| 1,000-row drain | 8,665 ms | 57-66 ms |
| `Timings` block 3: 10,000-row drain, then drop the table | about a quarter of an hour, by extrapolation | 1,294-1,365 ms |
| `Timings`, whole script | did not finish | 3,343-4,079 ms |

The four `Timings` blocks now measure 663, 1,711, 1,346 and 204 ms against the roughly 5,000, 9,500, 8,000 and 650 ms the script's own 2008 comments record, so every block is several times faster than its 2008 note - which is what the `Timing` calibration in `docs/perf-baseline.md` predicts.

**One table in `docs/perf-baseline.md` is not mine to claim.** The after-fix column of the drain-by-size table (46, 50, 66, 134 and 245 ms at 250 to 4,000 rows) comes from this ticket's own `fix`-stage measurements, carried over from the ticket body; only the 250- and 1,000-row pairs were re-measured here, and they came out lower (14-17 and 57-66 ms) because the reproduction test warms the engine first where those runs each paid a first compile. The document says so at the site. A reviewer who wants one consistent set should re-measure the column rather than trust the mixture.

## What a reviewer should weigh

**The bound of 2 in `QuotaDeleteCostTest` is a judgement, and it is the likeliest flake in this change.** Observed post-fix ratios span 0.95 to 1.18 on one quiet machine, so there is about 1.7x of headroom, and the quadratic signature is 5.2. The `SmallFloorMilliseconds` guard protects only the fast end (a machine whose smaller drain measures at timer resolution); it does nothing for a machine where the *larger* drain alone gets unlucky. Draining each size twice and counting the cheaper was added for exactly that and brought the worst observed ratio from 1.35 to 1.18. If this case ever flakes, raise the bound rather than the sizes: 2,000 rows would make a failing run 35 s.

**The per-row cost is flat, not constant.** 0.19 ms at 250 rows, 0.066 at 1,000 and 0.13 at 10,000. The fall is a drain's fixed costs spread over more rows; the rise from 1,000 to 10,000 is attributed in the document to the index getting deeper, which is an **inference, not a measurement** - nothing here measured where that 2x goes. It is plainly not proportional to the table's size, which is what the ticket set out to fix.

**The blast radius is covered in aggregate, not per device.** Every static cursor context over an ordered in-memory table now gets a device scan, and the change is inherited by `CatalogDevice`, `ApplicationTransactionDevice`, `FrontendDevice` and `SimpleDevice`. Part 1 covers the catalog and frontend devices and parts 2 and 3 cover application transactions, all green, and the quota-data-modification shapes in the corpus stayed green (`ApplicationTransactions.d4:354`, `:362`, `:479`; `DML.d4:1471`-`1484`; `TestProject.d4:68`-`71`; `ViewUpdatability.d4:295`). No test was added per device, deliberately: the device rule is one site and the corpus is what exercises it.

**The perf tripwire for `Timing` got looser.** An update run rewrites the whole label, so re-recording `dev-slow` for the new scenario also replaced `Timing`'s 16,144 ms with this run's 17,764 ms, which sits in the upper half of the six observations the document now lists (15,111 to 19,291). Faster runs never fail, so the only effect is that a future slowdown of that scenario has slightly more room before it warns. The diff was read; the entry's hand-written note was kept. Re-recording to land a lower figure is available but was judged not worth another 27 s run and a second rewrite.

**`docs/coverage-report.md` is one count off, and was left alone on purpose.** Its head says "582 tests passed ... 24 coverage scripts skipped - the 21 ... known failures and the 3 ... fragments"; with `Timings` passing the live numbers are 23 skipped and 20 known failures. That document is an explicitly dated snapshot of one coverage-collected run at commit `f6200026` and is replaced wholesale by the next such run, so editing its numbers with figures from a different, uncollected run would misrepresent it. Its suite total was already stale by a wide margin before this change.

**`QuotaDeleteCostTest` carries a library copy for headroom.** It registers `TestFramework.Perf` for the lifted `System.Temp` row cap, and at 1,000 rows the drain sits exactly at the default cap and would fit without it. The alternative was a second `alter device` statement in the suite; reusing the one library that owns the lift was preferred, and the comment at the site says so. A reviewer who disagrees can drop both overrides and lose only the headroom.

## Also updated

- `tests/DAE.Server.Tests/coverage-manifest.json`: the `Timings` entry is `pass`, with no `ticket`, no `error` and no `timeoutSeconds`; it keeps `register: ["TestFramework.Perf"]` and its note now says the script has no assertions, that the case only checks it still runs, and what the four blocks measured in this build.
- `tests/DAE.Server.Tests/PerfBaselineTest.cs`: `Timings` is a second row in `Scenarios`; the `<remarks>` that explained why it was left out now says what its numbers are worth against the 2008 notes, the `LibrariesToRegister` remarks say what the perf library is for now that a scenario actually calls `BenchmarkExecute`, and the accepted-tradeoff `NOTE:` about a script running twice per suite is reworded to cover both scenarios without changing the decision.
- `tests/goldens/perf/perf-baseline.json`: `dev-slow` re-recorded, six scenario keys where there was one. A comparison run against it passes with no findings.
- `docs/perf-baseline.md`: `Timings` and its four blocks are in the scenario table; the section that explained why it was not became *The `Timings` blocks against their own 2008 notes, and what the drain costs by table size*, carrying the before/after table and the regression test's bound; the run-to-run spread paragraph lists all six `Timing` observations and says which run the recorded figure comes from; the sentence saying the drain row of the 3.1 comparison cannot be filled in is gone.
- `docs/migration-notes.md`, *D4, catalog and libraries*: one entry - an `insert`, `update` or `delete` whose target narrows an ordered in-memory table is served from the device's index instead of a full in-memory sort, so such statements get much faster and no longer cost time proportional to the table's size, with results unchanged. The `var` table parameter half of the entry the ticket suggested is deliberately absent: as the `NOTE:` records, nothing changed there.
- `docs/modernization-plan.md`: known defect **K37**, and the `test-perf-baseline` row's closing sentence, which said `Timings` contributes no number yet.
