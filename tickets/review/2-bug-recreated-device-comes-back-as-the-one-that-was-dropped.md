description: A storage device that a script drops and creates again now comes back the way the script describes it, instead of the way an earlier statement in the same session had changed it. Seven sibling statements that remove or change something in the catalog got the same one-line fix.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs, tests/DAE.Server.Tests/DeviceClassAttributeRerunTest.cs, docs/modernization-plan.md, docs/migration-notes.md
----
## What landed

The engine keeps compiled plans in a cache shared by every session, keyed by statement text (`src/engine/DAE/Server/PlanCache.cs`), and hands one back as long as no catalog change has advanced `Catalog.PlanCacheTimeStamp` since it was compiled. A compiled `create` plan does **not** hold the statement: the compiler builds the catalog object and stores it on the node (`CompileCreateDeviceStatement` and its siblings), and executing the node re-registers that object. So a `drop` that does not advance the stamp leaves the next run of the same create text re-registering the object that was dropped - carrying whatever the statements in between did to it, and its original object id.

Eight nodes in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` now call `program.Catalog.UpdatePlanCacheTimeStamp()` after their catalog change (never before - K33's rule), each with a comment saying what a kept plan would otherwise hold: `DropDeviceNode`, `AlterDeviceNode`, `DropServerNode`, `DropRoleNode`, `AlterSortNode`, `DropSortNode`, `DropConstraintNode` and `DropEventHandlerNode`. The diff is 45 added lines and no changed or deleted line.

One of the eight is gated. `DropEventHandlerNode` bumps only `if (program.ServerProcess.ShouldAffectTimeStamp)`, because that node is also how the server drops the handlers it generates on its own copies of a table - at the end of **every** application transaction and at the disconnect of every session holding session objects, both of which run inside a time-stamp-safe context for exactly this reason (the comment at `ServerSession.DropSessionObjects` says so in as many words). An ungated bump there would flush every session's cached plans that often. No other object kind this ticket touches can be a session or application-transaction object, so the other seven are ungated.

Docs: a `K42` row in the plan's known-defects register (the analysis, the measurements, and why the bump stays per node) and a row in `docs/migration-notes.md` under *D4, catalog and libraries* (what an application notices: one recompilation of a statement repeated after one of those eight, and a create statement that now builds what it says).

## How to see it

Against an in-process server, through `IServerProcess.Execute` - **not** `ExecuteScript`, which does not consult the plan cache:

```
create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" };
alter device D alter class { alter "MaxRowCount" = "200" };
drop device D;
create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" };   -- same text
```

Before: `D` enforces 200 and `Script(Name('.D'))` says `"MaxRowCount" = "200"`. After: 100, both. The same shape with `tags { Scenario = "first" }` altered to `"second"`, with `create server L tags { HostName = "localhost", InstanceName = "Dataphor" }` whose host an `alter server` changes, and with `create role R tags { ... }`.

## The per-statement answers the ticket asked for

The rule the analysis settled on: **a `create` plan re-registers the object it built, so the `drop` must evict; an `alter` must evict only if the compiler kept a decision it made from the object.**

| Statement | Bump | Why |
|---|---|---|
| `drop device` | yes | measured; the create re-registers the dropped device |
| `alter device` | yes | compiling asks the device whether it can run each node and keeps that answer and the device's translation of it in the plan (`PlanNode.DetermineDevice` -> `Device.Prepare`), decided from the class settings and the maps this statement changes. Read from the code, **not measured** |
| `drop server` | yes | measured; the create re-registers the dropped link, under the host, instance and port an alter gave it |
| `alter server` | no | nothing is compiled against a link - `ExecuteOn`, `EvaluateOn` and `select ... on` resolve it at execute time - and K35 already closes the issuing process's connection |
| `drop role` | yes | measured; the create re-registers the dropped role with its altered tags and its object id |
| `alter role` | no | a role carries rights and metadata only; a cached plan's right checks are re-made when it is bound to the executing process (`PlanCache.Get` -> `BindToProcess`, where every DDL node re-checks its right and `BaseTableVarNode` re-runs `DetermineRights`) |
| `create right`, `drop right` | no | those two nodes hold the right's **name**, not an object; the create re-inserts it by name with the owner read at execute time |
| `alter sort`, `drop sort` | yes | compiling resolves a scalar type's sort into the plan's order and key columns (`Compiler.GetSort`, `GetUniqueSort`), so a plan compiled before either goes on comparing with the sort that was replaced or removed; plus the create re-registration on the drop. Read from the code, **not measured** |
| `drop constraint` | yes | the create re-registers the dropped constraint with the expression compiled against the catalog as it then stood. Read from the code, **not measured** |
| `alter constraint` | no | enforcement reads `TableVar.CatalogConstraints` as the modification runs (`TableNode.ValidateCatalogConstraints`), so the altered node and the drop's detach are seen by plans already compiled |
| `drop <event handler>` | yes, gated | the create re-attaches the dropped handler. Read from the code, **not measured** |
| `alter <event handler>` | no | handlers fire from the live list of the table variable, column or type as the modification runs |

## Tests

Four cases added to `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs`, beside K33's three `...InvalidatesCachedPlans` cases. Each was seen to fail against the unfixed engine and pass against the fixed one (procedure below).

- `DropDeviceInvalidatesCachedPlans` - the ticket's primary: the live row limit of the re-created device, and the create statement the catalog emits for it. Red: `Expected: 100 / But was: 200`.
- `DropDeviceInvalidatesCachedPlansThatCarryTags` - the metadata carrier rather than the class definition. Red: the emitted create said `Scenario = "second"`. Note this arm has a second cause of its own - a create statement hands the object it builds its own `MetaData`, an arm of `blocked/bug-view-tags-lose-to-tags-inherited-from-the-source` - and either cause alone brings the altered tag back, so this case does not isolate the timestamp.
- `DropServerLinkInvalidatesCachedPlans` - a second object kind, and the sharpest harm in the family: the live link's `HostName`. Red: `elsewhere` instead of `localhost`.
- `DropRoleInvalidatesCachedPlans` - a third object kind. Red: `Scenario = "second"`.

Test-infrastructure changes, no behaviour of their own: `SchemaCacheInvalidationTest` now derives from `DeviceClassAttributeFixture` for the device probes rather than copying them; that fixture's `Device(name)` lookup is now a one-line call on a new generic `CatalogObject<T>(name)` (same failure message), its `Script` is documented as working for any catalog object (it always did), and its `Evaluate` is **private** - it had no caller outside the base, and leaving it protected would have collided with `SchemaCacheInvalidationTest`'s own `Evaluate`, which deliberately evaluates on a session of its own.

`DeviceClassAttributeRerunTest.TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue` carried a `NOTE:` from the sibling ticket's review asking whoever landed this one to decide whether the case still earns its keep, because this bump makes it recompile instead of reaching a cached plan. **Decided: kept**, with the `NOTE:` rewritten to say what it now covers (the plan-cache path end to end, which only fails if the clone fix and this bump both go) and what it no longer is (a reproduction of either defect on its own). A reviewer who disagrees should cut the case rather than re-open the question.

## What a reviewer should weigh

- **Five of the eight bumps rest on reading, not measurement** (`alter device`, `alter sort`, `drop sort`, `drop constraint`, `drop <handler>`; the measured three are `drop device`, `drop server` and `drop role`). The mechanism is the same one measured four times over, and a bump is conservative - its only cost is a recompilation - but none of those five sites has a test, so a future edit could drop one silently. The observables each would need: for `alter device`, a device whose translation decision changes, which the in-memory device cannot show (it sets `IgnoreUnsupported` and supports almost nothing), so a SQL device and therefore a database server; for the sorts, a scalar type with two orderings and a table ordered by a column of it; for the constraint and handler, the re-registered object's identity rather than anything a user sees.
- **The structural alternative was considered and rejected.** `CatalogDeviceSession.ClearCatalogObject` is the one seam every catalog-object removal passes through, and one bump there would close the whole class at once. It was not taken: the existing drop nodes gate their bumps on `ShouldAffectDerivationTimeStamp` precisely to keep the server's own internal removals quiet, and a bump at the seam would fire for all of those. The reasoning is in the K42 row; there is no `NOTE:` in the code for it.
- **The churn cost is argued, not measured.** K33 recorded its own flush as "no slower for library registration" and this adds flushes on the same kind of statement; the one path where the cost could have been serious (application-transaction and session teardown) is the gated node. What was checked rather than argued: none of the timing scripts (`Timing`, `Timings`) issues any of the eight statements, and the library registration, application-transaction and session-table tests and scripts all pass. Nobody re-ran `PerfBaselineTest`.
- **The prepared-plan route is still open.** A caller that holds a plan from `IServerProcess.PrepareStatement` and executes it twice consults no timestamp, so prepare-execute-alter-drop-execute still brings the device back as it was altered, with this fix and the sibling's both in. Deliberately untouched; `blocked/bug-prepared-create-statement-executes-twice` owns the question of whether a compiled DDL plan may be executed more than once at all.
- **The create side is untouched**, as the ticket required: `CreateDeviceNode` and friends still do not bump, and the question "which existing plan could this create invalidate" was not re-opened for any object kind.
- `docs/migration-notes.md` had one LF-only line ending (the row the sibling ticket added); it is now CRLF like the rest of the working-tree file. This does not appear in `git diff` because this repository is checked out with `core.autocrlf=true`, so the stored blob is LF either way - worth knowing before anyone re-reports it as a hygiene defect.

## Verification run

`dotnet build Dataphor.slnx` - 0 errors; 143 warnings on the one full rebuild of this stage, the same count as the sibling ticket's commit. Then, per this project's implement-stage rule (targeted tests; CI runs the whole suite after the push):

- Fixtures over the changed node file and its neighbours - `--filter "FullyQualifiedName~"` on `SchemaCacheInvalidationTest`, `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `ConcurrentDDLTest`, `DeferredDeviceStopTest`, `CatalogRollbackSnapshotTest`, `ServerLinkUserRollbackTest`, `ServerLinkConnectionTest`, `ApplicationTransactionTest`, `LibraryLoadingTest` - **152 passed, 0 failed, 2 m 53 s.**
- Coverage scripts for every engine area the eight nodes touch, plus every script in the corpus that issues one of the eight statements and the ones that exercise application transactions and session tables - `--filter "Name="` on `Devices`, `ServerLink`, `Security`, `SecurityLibrary`, `Sorts`, `Constraints`, `EventHandlers`, `PlanCache`, `SerializationDevices`, `SerializationEventHandlers`, `SerializationConstraints`, `DML`, `Domains`, `Functions`, `LongAggregates`, `Tables`, `Types`, `LibraryCoverage`, `LibraryDependencies`, `LibraryRename`, `ApplicationTransactions`, `SessionTables`, `SessionAndProcessScopedTables`, `MaximumRowCount` - **22 passed, 0 failed, 1 m 41 s**, plus the two recorded known failures (`Domains`, `MaximumRowCount`, both owned by `test-pre-3-1-scripts-policy`) matching their recorded errors. The `ServerLink` script runs against a second Dataphor server in a child process, which is what makes it worth naming here.
- The before/after measurement: `DDLNodes.cs` was replaced with `git show HEAD:...` (the whole file, so all eight sites went at once), rebuilt, and the fixture run - the four new cases red, the other seven green. The file was then restored from a copy taken before the swap and checked byte for byte (`sha256` equal); the final state was rebuilt and re-run green.

No failure outside the two recorded known ones, so no `tickets/.pre-existing-error.md` was written.
