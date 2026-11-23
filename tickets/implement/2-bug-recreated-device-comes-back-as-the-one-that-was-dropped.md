description: A script that creates a storage device, changes one of its settings and is then re-run in the same session can get the device back with the changed setting instead of the one the script states, because the server hands back the compiled copy of the create statement it kept from the first run even though the device was dropped in between.
prereq: bug-remembered-statement-applies-a-changed-setting
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs, docs/modernization-plan.md
repro: verified
----
## What goes wrong

The engine keeps the compiled form of a statement so that running the same text again is cheap (`src/engine/DAE/Server/PlanCache.cs`, shared by every session, keyed by statement text). A kept plan is handed back as long as no catalog change has advanced `Catalog.PlanCacheTimeStamp` since the plan was compiled; each DDL statement that changes the catalog in a way a compiled plan could care about calls `Catalog.UpdatePlanCacheTimeStamp()` so that stale plans are thrown away.

`drop device` does not call it. Neither does `alter device`. So a session that runs

```
create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" };
alter device D alter class { alter "MaxRowCount" = "200" };
drop device D;
create device D class "System.MemoryDevice" attributes { "MaxRowCount" = "100" };   -- same text as the first
```

gets the kept plan back for the last statement, and the new device comes up with a row limit of **200**. The same sequence with a tag instead of a setting (`tags { Foo = "1" }`, the tag altered to `"2"`, the device dropped, the create re-run) brings the device back with `Foo = "2"`. Both measured on 2026-11-23 against an in-process server through `IServerProcess.Execute`, which is the path a client and the Dataphor IDE take and the one that consults the kept plans.

This is the worse half of the family the sibling ticket `bug-remembered-statement-applies-a-changed-setting` came from, because a drop-and-recreate from a script is an ordinary deployment step: a catalog rebuild script that creates a device, later changes one of its settings, and is then re-run in the same session builds the device with the changed setting, and no error is raised either time.

## Root cause

A compiled `create` plan does not hold the statement — it holds the **catalog object the compiler already built**. `CompileCreateDeviceStatement` constructs the `Schema.Device` at compile time and stores it on the node (`src/engine/DAE/Compiling/Compiler.cs:9372`), and executing the node is just `program.CatalogDeviceSession.CreateDevice(_newDevice)` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:781`). Every `Create…Node` in that file is built this way. So re-executing a kept create plan does not re-create anything: it re-registers the very object that was in the catalog, carrying whatever the statements in between did to it — including its object ID.

The engine's own answer to "a kept plan whose catalog premise has changed" is the plan-cache timestamp, and the drop *is* a catalog change that invalidates the premise. The defect is the missing bump, the same defect and the same remedy as K33 in `docs/modernization-plan.md` (which added the bump to `CreateConversionNode`, `DropConversionNode` and `CreateOperatorNode` for the same reason, and whose reproductions are in `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs`). Adding `program.Catalog.UpdatePlanCacheTimeStamp()` to `DropDeviceNode` after the drop makes both reproductions above pass — measured 2026-11-23.

Present since the initial open-source commit; not a port consequence.

## Which nodes are missing it

Reading every `Create…Node`, `Alter…Node` and `Drop…Node` in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs`, the bump is present on `AlterTableNode`, `AlterViewNode`, `AlterScalarTypeNode`, `AlterOperatorNode`, `AlterAggregateOperatorNode`, `AlterReferenceNode`, `DropTableNode`, `DropViewNode`, `DropScalarTypeNode`, `DropOperatorNode`, `DropReferenceNode`, `CreateOperatorNode`, `CreateReferenceNode`, `CreateConversionNode` and `DropConversionNode` — and absent on the alter and drop of **devices**, **server links**, **roles**, **rights**, **sorts**, **constraints** and **event handlers**.

Devices are measured. The rest are read from the code and not measured: each needs its own answer, because the question is not "is it symmetrical" but "can a plan compiled before this statement still be right afterwards". A dropped catalog constraint that a kept plan still enforces, or a dropped sort a kept plan still uses, look like the same defect, but say so only after measuring. The *create* side is a judgement per object kind and is deliberately not uniform today: `CreateOperatorNode` bumps because a new, closer overload changes how an existing call resolves (K32), while `CreateTableNode` does not, because a plan compiled when the table did not exist could not have referred to it. Do not add a bump to a create node without saying which existing plan it could invalidate.

Bump **after** the catalog change, never before: K33 records that a plan compiled between a bump and the change would otherwise survive with the new stamp.

## What this ticket does not cover

The kept-plan cache is not the only way to run a compiled statement twice. A caller can hold a plan from `IServerProcess.PrepareStatement` and execute it as often as it likes, and no timestamp is consulted on that path: prepare the create statement above, execute it, alter the setting, drop the device, execute the prepared plan again, and the device comes back with 200 even with this ticket's fix and the sibling ticket's fix both in. Measured 2026-11-23. Whether a compiled DDL plan may be executed more than once at all is an unsettled question and is filed as `blocked/bug-prepared-create-statement-executes-twice`; leave it alone here, and in the handoff say that the prepared-plan route is still open and where it is tracked.

## TODO

- Add `program.Catalog.UpdatePlanCacheTimeStamp()` to `DropDeviceNode.InternalExecute` after `DropDevice`, and to `AlterDeviceNode` after its catalog change, with a short comment pointing at what a kept plan would otherwise hold.
- For the alter and drop of server links, roles, rights, sorts, constraints and event handlers: for each, either add the bump or write one sentence in the handoff saying why a plan compiled before that statement is still right afterwards. Measure the ones you claim are broken before claiming it.
- Add the two reproductions as cases. `tests/DAE.Server.Tests/SchemaCacheInvalidationTest.cs` is where K33's equivalents live and is the right fixture; its cases are named `…InvalidatesCachedPlans`. They must go through `IServerProcess.Execute` (not `ExecuteScript`, which does not consult the kept plans), and reading a memory device's live row limit needs the helper that `DeviceClassAttributeRollbackTest` has — move it somewhere both fixtures can use it rather than copying it.
- Add a row to the known-defects table in `docs/modernization-plan.md`, in the style of K32 and K33, and an entry in `docs/migration-notes.md` under *D4, catalog and libraries*: a session that drops a device (or alters one) now has its kept compiled plans thrown away, so a statement repeated after device DDL is compiled again — slower in that one case, and no longer able to re-register the device that was dropped.
- Build, then run the suite in the three parts `AGENTS.md` gives, each as its own foreground command.
