description: Deleting one row at a time from a table with the "delete the first row" form of the delete statement gets slower the bigger the table is, because the engine sorts the whole table on every such statement instead of reading it in order from the index it already has. One line in the in-memory device decides this; the fix has been measured and makes a 4,000-row drain 616 times faster.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Memory/MemoryDevice.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/DAE.Server.Tests/PerfBaselineTest.cs, tests/goldens/perf/perf-baseline.json, docs/perf-baseline.md, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
difficulty: medium
----
## The defect, in one sentence

The in-memory device offers an ordered scan of a table over an index it already has, but only when the statement asking for it does **not** want a stable snapshot — and every data-modification statement wants one, so inside `insert`, `update` and `delete` the device declines, the engine sorts the whole table in memory instead, and it does that once per statement.

## The one code site

`src/engine/DAE/Device.Memory/MemoryDevice.cs:79`, in `InternalPrepare`:

```csharp
else if ((planNode is OrderNode) && (planNode.Nodes[0] is BaseTableVarNode) && (plan.Plan.CursorContext.CursorType != CursorType.Static))
```

The third clause is the defect. `CursorType.Static` means "the caller wants a snapshot that does not see later changes", and the three data-modification nodes all bind their target inside such a context (`CursorContext(CursorType.Static, …)` at `src/engine/DAE/Runtime.Instructions/TableNodes.cs:426`, `440`, `466` and `479` in `InsertNode`, `746` and `805` in `UpdateNode`, and `1106` and `1142` in `DeleteNode`). So inside any of them the device refuses the ordered scan, the `OrderNode` is left to the engine's own `OrderTable`, and `OrderTable` materializes and sorts every row of the table.

A quota expression — in D4 `<table> return <n> by { <order> }`, "the first n rows" — is compiled as a `QuotaNode` over an `OrderNode` over the source (`QuotaNode.DetermineDataType` wraps its source in `Compiler.EmitOrderNode`, `src/engine/DAE/Runtime.Instructions/QuotaNode.cs:69`). So `delete T return 1 by { ID }` sorts all N rows and then takes one of them. A loop that empties the table runs N such statements, which is why the whole drain is quadratic.

## The evidence that this is the site, and not something else

The two plans, dumped with the engine's own `Diagnostics.ShowPlan` operator through a scratch NUnit fixture on `InProcessTestFixture` (written for this investigation and deleted again; nothing of it is left in the tree):

- `delete T return 1 by { ID }` — `DeleteNode` → `QuotaNode` (`CursorType="static"`) → `OrderNode` (**`DeviceSupported="False"`**, `CursorType="static"`) → `BaseTableVarNode` (`DeviceSupported="True"`, `Device="System.Temp"`). There is no `CopyNode`, because the `QuotaNode` is already static; the engine's sort is what makes it static.
- `select T return 1 by { ID }` — `CursorNode` → `QuotaNode` (`CursorType="dynamic"`) → `OrderNode` (**`DeviceSupported="True"`**, `Device="System.Temp"`) → `BaseTableVarNode`. The device serves the ordered scan from its index and nothing sorts.

That one attribute is the entire difference between the slow statement and the fast expression, and it also explains the two controls the `fix` stage measured. `delete T` and `delete T where ID = 1` both come out as a `CopyNode` over a device-supported source, so they copy once — the whole table, or the single row the key index hands over — and never sort per row.

## The fix, and what it measures

Drop the `CursorType != Static` clause, so the device offers the ordered scan whenever it can serve it and the engine decides where to make the snapshot. The engine already has that decision: `DeleteNode.InternalBindingTraversal` (`TableNodes.cs:1104`) wraps a non-static target in `Compiler.EmitCopyNode`, and `CursorNode` (`src/engine/DAE/Runtime.Instructions/CursorNodes.cs:106`) and `Compiler.cs:2753` do the same. The plan then becomes `CopyNode(QuotaNode(OrderNode[device scan](BaseTableVarNode)))`: the device reads in order, the quota narrows to n rows, and the copy snapshots those n rows instead of all N — the same shape `delete T where ID = 1` already gets, which the `fix` stage measured at 130 ms for 2,000 single-row deletes.

Measured 2026-11-22, Debug, on the same slow development laptop the rest of `docs/perf-baseline.md` was recorded on, draining with `while exists (T) do delete T return 1 by { ID };` on a table `T { ID : Integer }` on `System.Temp`:

| Rows | Before | After | Per row, after |
|---|---|---|---|
| 250 | 452 ms | 46 ms | 0.19 ms |
| 500 | 2,073 ms | 50 ms | 0.10 ms |
| 1,000 | 8,486 ms | 66 ms | 0.066 ms |
| 2,000 | 34,308 ms (from the `fix` stage) | 134 ms | 0.067 ms |
| 4,000 | 150,879 ms (from the `fix` stage) | 245 ms | 0.061 ms |

The three "before" numbers at 250, 500 and 1,000 rows were re-measured here and reproduce the curve the `fix` stage recorded (397, 1,885 and 7,866 ms). After the change the cost per row is flat from 1,000 rows up, which is what "fixed" means for this ticket; at 4,000 rows the drain is 616 times faster. The `Timings` coverage case completes in 5 s including engine start, well inside the 15 s timeout its manifest entry already carries.

## The whole suite, under the change

All three parts, Debug, built from `Dataphor.slnx`:

- part 1 (everything but the coverage scripts): 565 passed, 0 failed — including `PerfBaselineTest`, the catalog store and concurrency fixtures, and the frontend fixtures. (One case failed on the first run: the investigation's own scratch fixture, which left a table behind for a sibling case. It is gone.)
- part 2 (coverage scripts A–R): 67 passed, 0 failed, 11 skipped.
- part 3 (the rest): 49 passed, and the only failure is the intended one — `Timings` reported as *unexpectedly passing* against its own known-failure entry, which is this ticket's manifest change.

Quota data modification is covered by the corpus and stayed green: `ApplicationTransactions.d4:354` and `:362` update through a quota, `:479` inserts through one, `DML.d4:1471`–`1484` and `TestProject.d4:68`–`71` assert quota reads, and `ViewUpdatability.d4:295` defines a view over a quota.

## Blast radius, and the one place to look before landing

Every static cursor context over an ordered in-memory table now gets a device scan where it previously got a sort. In each of these the engine already re-snapshots above it, so the snapshot is still made — over the rows that survive whatever narrows them, rather than over the whole table:

- `InsertNode`, `UpdateNode`, `DeleteNode` (the eight sites in `TableNodes.cs` listed above), `CursorNode` (`CursorNodes.cs:106`) and the insert-source path at `Compiler.cs:2753`.
- Inherited by every subclass of `MemoryDevice`: `CatalogDevice` (whose own `InternalPrepare` delegates to the base at `src/engine/DAE/Device.Catalog/CatalogDevice.cs:147`), `ApplicationTransactionDevice`, `FrontendDevice` and `SimpleDevice`. Part 1 covers the catalog and frontend devices, parts 2 and 3 cover application transactions.

**The exception, and the thing to settle in this ticket:** `ParameterNode` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs:1037` and `1052`) pushes the same static cursor context for a `var` parameter and adds **no** copy above it. So a `var` parameter of table type bound to an ordered table on an in-memory device would receive a live scan where it previously received a snapshot — a real difference for the callee, which would then see rows it inserts itself. Nothing in the coverage corpus exercises that shape, which is why the suite is green and why this is a reading task rather than a measured finding. Read those two sites; if the snapshot is required there, add the same `Compiler.EmitCopyNode` compensation `CursorNode` performs, rather than narrowing the device again.

## What was considered instead, so it is not re-derived

Scope the change to the quota: have `QuotaNode.InternalBindingTraversal` push a non-static cursor context around the traversal of its source, so only quota plans change shape. That fixes this instance and leaves the device's rule alone. It is not the recommendation because the misjudgement is the device's, not the quota's — the device is choosing where to materialize from the *requested cursor type*, which says what the consumer needs and nothing at all about what sits in between, so anything that narrows the row count above an ordered scan inside a data-modification statement pays the same cost. The compensation belongs where the engine already puts it. Keep this as the fallback if the `ParameterNode` reading above turns out to need a snapshot and the compensation there is awkward.

## Not a port regression — do not claim it was ever faster

Both the device clause and `DeleteNode`'s static cursor context are unchanged since this repository's first commit (`361ebd83`, 2008-03-19); the only later touch is the repository-wide rename pass `01f32a20`. `Timings.d4`'s own note of about 8 s for the 10,000-row drain was therefore taken on code older than anything this repository holds, so whether the drain was ever linear in a shipped build cannot be established here. Say in the documentation that the drain is now fast, not that a regression was repaired.

## The reproduction test

The behaviour to pin is that the cost of one `delete <table> return <n>` does not grow with the number of rows in the table. The lowest layer that shows it is a D4 drain through the in-process engine harness, so: one NUnit case on `InProcessTestFixture` in `tests/DAE.Server.Tests` (a small new file of its own — no existing fixture owns this), registering `TestFramework.Perf` for the lifted row cap on `System.Temp`. Remember that this project's test project lists its sources explicitly, so a new file needs its `Compile` item in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`.

Shape it as a ratio, never an absolute time, so the case says nothing about how fast the machine is:

- warm the engine with a throwaway drain first, so the smaller size is not inflated by compiling the plan for the first time;
- drain at two sizes (250 and 1,000 rows keeps a failing run to about 9 s and a passing one to about 0.1 s);
- assert that the cost per row at the larger size is at most twice the cost per row at the smaller.

Measured separation with those sizes: 4.7× before the change, 0.35× after. Two is far from both. Put the measured numbers and the reason for the bound in a comment beside the constant, the way `PerfBaselineTest` does for its own factors.

## TODO

- Remove the `(plan.Plan.CursorContext.CursorType != CursorType.Static)` clause from `MemoryDevice.InternalPrepare` (`src/engine/DAE/Device.Memory/MemoryDevice.cs:79`), and leave a comment at the site saying why the device offers the ordered scan regardless of the requested cursor type: the device knows what it can serve, and the engine's `EmitCopyNode` compensation is what makes the result static, at the point where the row count is already narrowed.
- Read `ParameterNode.InternalBindingTraversal` and `BindToProcess` (`src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs:1037`, `1052`) as *Blast radius* above describes, and either add the missing copy compensation or record in a `NOTE:` at that site why a live scan is correct for a `var` table parameter.
- Add the reproduction case described above, with its bound and the measured numbers in a comment.
- `tests/DAE.Server.Tests/coverage-manifest.json`: change the `Timings` entry from `known-failure` to `pass`, drop its `ticket` field and its `timeoutSeconds` override, keep `register: ["TestFramework.Perf"]`, and rewrite the note — it currently explains the quadratic drain and carries the old curve. Say instead that the script has no assertions, that the case only checks that it still runs, and what it measured in this build.
- `tests/DAE.Server.Tests/PerfBaselineTest.cs`: add the `Timings` row to the `Scenarios` table (library `CoverageScripts.ScriptsLibrary`, one line on what it runs), and rewrite the `<remarks>` paragraph that explains why it was left out.
- Re-record the `dev-slow` label in `tests/goldens/perf/perf-baseline.json`, which an added scenario requires for any comparison run to pass, with `DATAPHOR_UPDATE_PERF_BASELINE=1 DATAPHOR_PERF_BASELINE=dev-slow` as `docs/perf-baseline.md` describes — remembering that an update run rewrites the whole label, `Timing`'s number included, and always fails, so read the diff. **Only re-record if you are on the `dev-slow` machine class** (the slower of the two the root `AGENTS.md` quotes suite timings for; it is the machine this ticket and the two perf tickets before it were measured on). If you are not, add the scenario row anyway — an ordinary run records and passes, and nothing in CI compares — and say in `docs/perf-baseline.md` that `dev-slow` is missing the new scenario and needs re-recording on that machine.
- `docs/perf-baseline.md`: move `Timings` out of *The other timing script, and why it is not in the table* and into the scenario table; replace that section's block-3 row and quadratic curve with the measured flat curve; strike the sentence at the end of *The comparison with 3.1* that says the drain row cannot be filled in on the modern side.
- `docs/migration-notes.md`: an entry for the plan-shape change. An application can notice it — a `delete`, `update` or `insert` whose target narrows an ordered in-memory table is now served from the device's index rather than from a full in-memory sort, so such statements get much faster and no longer cost time proportional to the table's size; and, if the `ParameterNode` reading above concludes a live scan is correct there, say that a `var` table parameter over an ordered in-memory table is now a live scan rather than a snapshot.
- `docs/modernization-plan.md`: one row in the *Known defects* table (next free number, K37) in the style of the rows around it — the defect, where, status `fixed`, and this ticket as the fix.
- Build, then run the suite in the three parts the root `AGENTS.md` gives, each as its own foreground command, and confirm `Timings` now reports as a pass rather than as unexpectedly passing.
