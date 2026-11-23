description: A storage device that a script drops and creates again now comes back the way the script describes it, instead of the way an earlier statement in the same session had changed it. Eight sibling statements that remove or change something in the catalog got the same one-line fix, and review found a ninth.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, tests/DAE.Server.Tests/DeviceClassAttributeFixture.cs, tests/DAE.Server.Tests/DeviceClassAttributeRerunTest.cs, docs/modernization-plan.md, docs/migration-notes.md
----
## What landed

The engine keeps compiled plans in a cache shared by every session, keyed by statement text (`src/engine/DAE/Server/PlanCache.cs`), and hands one back as long as no catalog change has advanced `Catalog.PlanCacheTimeStamp` since it was compiled. A compiled `create` plan does **not** hold the statement: the compiler builds the catalog object and stores it on the node, and executing the node re-registers that object. So a `drop` that did not advance the stamp left the next run of the same create text re-registering the object that had been dropped - carrying whatever the statements in between did to it, and its original object id. A second face of the same gap: a plan that resolved something out of the catalog while compiling - a device's answer about which nodes it can run, a scalar type's sort - went on using that answer after the statement that changed it.

Nine nodes in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` now call `program.Catalog.UpdatePlanCacheTimeStamp()` after their catalog change (never before - K33's rule), each with a comment saying what a kept plan would otherwise hold: `DropDeviceNode`, `AlterDeviceNode`, `DropServerNode`, `DropRoleNode`, `CreateSortNode`, `AlterSortNode`, `DropSortNode`, `DropConstraintNode` and `DropEventHandlerNode`. The implement stage landed eight of them; `CreateSortNode` is the review's own finding, below.

Two of the nine are gated, each for a reason its comment states. `DropEventHandlerNode` bumps only `if (program.ServerProcess.ShouldAffectTimeStamp)`, because that node is also how the server drops the handlers it generates on its own copies of a table - at the end of **every** application transaction and at the disconnect of every session holding session objects, both of which run inside a time-stamp-safe context for exactly this reason. `CreateSortNode` bumps only `if (!program.ServerProcess.InLoadingContext())`, because the compiler executes that node itself to fill in a scalar type's missing unique or equality sort, and the catalog executes it to load a stored sort.

Statements deliberately left alone, each with its reason recorded in the K42 row: `alter server`, `alter role`, `create right`, `drop right`, `alter constraint` and `alter <event handler>`. The create side is otherwise untouched: the question "which existing plan could this create invalidate" was re-opened only for `create sort`, where a plan really does keep a stale answer.

Docs: a `K42` row in the plan's known-defects register and a row in `docs/migration-notes.md` under *D4, catalog and libraries*. What an application notices is one recompilation of a statement repeated after one of those nine, a create statement that now builds what it says, and an ordering statement that now orders by the type's current sort.

## How to see it

Against an in-process server, through `IServerProcess.Execute` - **not** `ExecuteScript`, which does not consult the plan cache:

```
create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" };
alter device D alter class { alter "MaxRowCount" = "200" };
drop device D;
create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" };   -- same text
```

Before: `D` enforces 200 and `Script(Name('.D'))` says `"MaxRowCount" = "200"`. After: 100, both. The same shape holds with `create server L tags { HostName = "localhost", ... }` whose host an `alter server` changes, and with `create role R tags { ... }`. The sort face, through a cursor opened twice on one expression text:

```
create type N like String;
create table T { ID : Integer, N : N, key { ID } };
insert table { row { 1 ID, 'a' N }, row { 2 ID, 'b' N }, row { 3 ID, 'c' N } } into T;
-- open a cursor on "select T order by { N }"; the first row is 1
create sort N using CompareText(right.value, left.value);
-- open a cursor on the same text; before the fix the first row was still 1, not 3
```

## Tests

Four cases in `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs`, beside K33's three. Each was seen to fail against the unfixed engine and pass against the fixed one.

- `DropDeviceInvalidatesCachedPlans` - the primary: the live row limit of the re-created device, and the create statement the catalog emits for it.
- `DropServerLinkInvalidatesCachedPlans` - a second object kind, and the sharpest harm in the family: the live link's `HostName` came back as `elsewhere` instead of `localhost`, which is where the work would have been sent.
- `DropRoleInvalidatesCachedPlans` - a third object kind, and the one whose create statement carries nothing but metadata.
- `CreateSortInvalidatesCachedPlans` - the review's finding, read through a cursor because the order is what a sort decides.

One existing case was re-decided rather than added to. `DeviceClassAttributeRerunTest.TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue` carried a `NOTE:` from the sibling ticket's review asking whoever landed this one to decide whether it still earns its keep, because this bump makes it recompile instead of reaching a cached plan. **Kept**, with the `NOTE:` rewritten to say what it now covers (the plan-cache path end to end, which fails only if the clone fix and this bump both go) and what it no longer is (a reproduction of either defect on its own).

Test infrastructure, no behaviour of its own: `SchemaCacheInvalidationTest` derives from `DeviceClassAttributeFixture` for the device probes rather than copying them, and that fixture's `Device(name)` lookup is now a one-line call on a new generic `CatalogObject<T>(name)`.

## Review findings

The diff of `ticket(implement): bug-recreated-device-comes-back-as-the-one-that-was-dropped` was read before its handoff summary.

**Major - a missed sibling, fixed in this pass with its reproduction.** `CreateSortNode` does the same `CreateSort` + `AttachSort` + `UpdateCatalogObject` as `AlterSortNode` and did not bump, so the implement stage's own argument for `alter sort` - compiling resolves a scalar type's sort into the plan's order and key columns, so a plan compiled before the statement goes on comparing with the sort it replaced - applied unanswered to `create sort`. Where a scalar type has no sort of its own, `Compiler.GetSort` falls back to the unique sort the compiler generates, so a plan compiled before the type's own sort is created keeps ordering by that generated one. Measured, not inferred: the new `CreateSortInvalidatesCachedPlans` fails `Expected: 3 / But was: 1` with the bump removed and the rest of the fix in place. Fixed rather than filed because it is one line at a site the ticket was already changing, in the class the ticket exists to close; the K42 row and the migration note were extended to nine statements. The bump is gated on `!InLoadingContext()`, which is this node's one difference from its siblings: the compiler executes it itself (`Compiler.GetUniqueSort`, `GetEqualitySort`) and so does the catalog's lazy load of a stored sort, and neither can displace an answer an earlier plan captured, since the first sort a type gets is the one every plan compiled before it resolved to as well.

**Checked and sound.** The eight landed bumps all sit after their catalog change and on every path that reaches it. The gating argument for `DropEventHandlerNode` was verified rather than taken on trust: `ServerSession.DropSessionObjects` and all three of the application-transaction device's drop blocks hold `EnterTimeStampSafeContext` across both the compile and the execute, so reading `ShouldAffectTimeStamp` at execute time is equivalent to the siblings' compile-time capture for every internal caller. The seven ungated nodes were each checked against what the server drops internally: session objects are tables, views, types and operators, and application-transaction objects are table variables, event handlers and operators, so no device, server link, role, catalog constraint or sort is ever removed by the server's own teardown. A rollback that undoes one of these drops is already covered - `CatalogDeviceSession.InvalidateCachesAfterUndo` bumps the same stamp - so nothing was needed there. The per-node choice over one bump at `CatalogDeviceSession.ClearCatalogObject` was re-weighed and upheld: a bump at that seam would fire for the internal removals the existing `ShouldAffectDerivationTimeStamp` gating exists to keep quiet.

**Tripwire parked, not filed.** A device scalar type or operator map is a catalog object of its own, stored as the `alter device` statement that creates it, so the catalog's lazy load of one now runs `AlterDeviceNode` and flushes every session's cached plans; a SQL device has many maps, so a cold server warms up with one flush per map first used. Correctness-neutral and unmeasured, and the load of a stored operator has always done the same through `CreateOperatorNode`, so this is not new behaviour in kind. Recorded as a `NOTE:` at the bump in `AlterDeviceNode`, naming `ShouldAffectTimeStamp` as the gate to reach for if plan-cache churn on a cold server ever shows up.

**Test cut.** `DropDeviceInvalidatesCachedPlansThatCarryTags` was removed. It covered `DropDeviceNode` a second time, through a mechanism that is also an arm of `bug-view-tags-lose-to-tags-inherited-from-the-source` (a create statement hands the object it builds its own `MetaData`), and the implement handoff said as much - "this case does not isolate the timestamp". Once that sibling lands it would read as that fix's guard sitting in a fixture about cache invalidation, under a comment that had gone stale. The tag path keeps its coverage through `DropRoleInvalidatesCachedPlans`. Separately, `FirstID` - the new cursor helper - was placed in the fixture's helper block beside `Unique` and `Evaluate` rather than between two test cases.

**Nothing found in these, and why.** Error handling and resource cleanup: every bump is a field increment on an object the node already holds, after the work, inside the lock the node already takes - there is nothing to fail and nothing to release, and a statement that throws before the bump correctly leaves the stamp alone. Type safety: `CatalogObject<T>` is constrained to `Schema.Object` and its one new caller passes `Schema.ServerLink`. Compatibility: no D4 syntax, class name, error code, contract shape or file format moved; the only thing an application can observe is recompilation, which `docs/migration-notes.md` now records. Source hygiene: the comments carry reasons rather than narrating statements, and `DDLNodes.cs` grew 15 lines on top of the implement stage's 45 - no extraction was warranted, and the file's tabs, CRLF line endings and absence of a byte-order mark are unchanged. The five arms the handoff flagged as read-not-measured (`alter device`, `alter sort`, `drop sort`, `drop constraint`, `drop <event handler>`) were left that way: each would need an observable the in-memory device cannot produce, a bump is conservative where it is wrong, and the mechanism is now measured four times over rather than three.

**Still open, deliberately.** A caller that holds a plan from `IServerProcess.PrepareStatement` and executes it twice consults no timestamp, so prepare-execute-alter-drop-execute still brings the device back as it was altered. Untouched here; `plan/bug-prepared-create-statement-executes-twice` owns whether a compiled DDL plan may be executed more than once at all.

## Verification run

`dotnet build Dataphor.slnx` - 0 errors. Then, per this project's review-stage rule (the implement stage's targeted set plus the neighbouring fixtures and the coverage scripts for every engine area the diff touches; CI runs the whole suite after the push):

- Fixtures over the changed files and their neighbours - `--filter "FullyQualifiedName~"` on `SchemaCacheInvalidationTest`, `DeviceClassAttributeRerunTest`, `DeviceClassAttributeRollbackTest`, `ConcurrentDDLTest`, `DeferredDeviceStopTest`, `CatalogRollbackSnapshotTest`, `ServerLinkUserRollbackTest`, `ServerLinkConnectionTest`, `ApplicationTransactionTest`, `LibraryLoadingTest`, plus the ring the sort change adds: `CursorTest`, `D4EmitterRoundTripTest`, `ColumnChangeRollbackTest`, `MemoryDeviceConcurrencyTest`, `CatalogStoreTest` - **224 passed, 0 failed, 4 m 55 s.** `ServerLinkConnectionTest` runs against a second Dataphor server in a child process, which is what makes it worth naming here.
- Coverage scripts for every engine area the nine nodes touch, plus the ordering and serialization scripts a sort regression would show in - `--filter "Name="` on the implement stage's 24 (`Devices`, `ServerLink`, `Security`, `SecurityLibrary`, `Sorts`, `Constraints`, `EventHandlers`, `PlanCache`, `SerializationDevices`, `SerializationEventHandlers`, `SerializationConstraints`, `DML`, `Domains`, `Functions`, `LongAggregates`, `Tables`, `Types`, `LibraryCoverage`, `LibraryDependencies`, `LibraryRename`, `ApplicationTransactions`, `SessionTables`, `SessionAndProcessScopedTables`, `MaximumRowCount`) and ten more (`Cursors`, `Browse`, `Globalization`, `KeyAffectingUpdate`, `ScanTable`, `NodeOptimization`, `Serialization`, `SerializationTypes`, `LibraryDomains`, `LibraryTypes`) - **30 passed, 0 failed, 4 skipped, 1 m 42 s.** The four skips are `Domains`, `LibraryDomains`, `MaximumRowCount` and `Serialization`, all four recorded `known-failure` entries in `tests/DAE.Server.Tests/coverage-manifest.json` owned by `test-pre-3-1-scripts-policy`.
- The before/after measurement for the new arm: the `CreateSortNode` bump was removed, the solution rebuilt, and the fixture re-run - `CreateSortInvalidatesCachedPlans` red at `Expected: 3 / But was: 1`, the other ten green. `DDLNodes.cs` was then restored from a copy taken before the removal and checked byte for byte (`sha256` equal), and the final state rebuilt and re-run green: 25 passed over the touched fixtures and the `Sorts`, `PlanCache`, `Cursors`, `Globalization` and `Types` scripts.

No failure outside the four recorded known ones, so no `tickets/.pre-existing-error.md` was written.
