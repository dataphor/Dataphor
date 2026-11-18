description: When a transaction drops a device, the server now stops the device only when the outermost transaction commits; a nested commit hands the pending stop to the enclosing transaction, a nested rollback discards only its own stops, and a drop that fails its checks no longer records a stop.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/DeferredDeviceStopTest.cs
repro: verified
----
## What changed

Dropping a device inside a transaction defers stopping it until commit, so that work already done in that device can still commit. The pending stops used to sit in one list for the whole catalog device session, so any nested commit ran them, any nested rollback cleared them, and a drop that failed its checks had already added its stop.

**`CatalogDeviceSession` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, near `StopDevice`).** Each pending stop is now a private `DeferredDeviceStop` (device plus the transaction level it belongs to), recorded with `Transactions.Count` at the time of the drop. Inside `InternalCommitTransaction` / `InternalRollbackTransaction`, `Transactions.Count` is the level being ended (the base `Schema.DeviceSession` ends the level after calling them; `ServerCatalogDeviceSession` already relies on this with `Transactions.Count - 1`).

- `CommitDeferredDeviceStops` (replaces the call to `ExecuteDeferredDeviceStops` at the end of `InternalCommitTransaction`): at a nested level, re-tags the stops at that level (or deeper) to the enclosing level; at level 1, stops every pending device and clears the list, as before.
- `RollbackDeferredDeviceStops` (replaces `ClearDeferredDeviceStops` in the rollback `finally`): removes the stops whose level is at or deeper than the level being rolled back; at level 1 that is all of them, as before.
- `ExecuteDeferredDeviceStops` and `ClearDeferredDeviceStops` are gone (both private). The undo path, `StopDevice(device, true)`, still stops at once.

**`DropDeviceNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`, end of file).** `StopDevice` moved from first to just before `DropDevice`, after `CheckNotSystem`, `CheckNoBaseTableVarDependents`, `DropDeviceMaps` and `CheckNoDependents`. In a transaction the stop ran at commit anyway, so the order of effects there is unchanged. Outside any transaction (implicit transactions off) the device's maps are now dropped while it is still running, which is the order the transactional path always had.

No compatibility surface changes (no D4 syntax, class names, file formats, error codes or public members); the only visible change is when a dropped device stops. No `docs/migration-notes.md` entry was added for that reason.

## Tests

No tests added in this stage; the reproduction was added in the fix stage, `tests/DAE.Server.Tests/DeferredDeviceStopTest.cs`. Each case creates a memory device, starts it by inserting into a table in it, and drops that table. Expectations were not changed.

- `TestNestedRollbackKeepsOuterStop`: begin; drop device; begin; rollback; commit. Checks that a nested rollback keeps the outer level's stop (the device stops at the outer commit). Failed before, passes now.
- `TestNestedCommitDefersStop`: begin; drop device; begin; commit (still running); commit (stopped). Checks that a nested commit hands the stop over instead of running it. Failed before, passes now.
- `TestNestedRollbackDiscardsItsStop`: begin; begin; drop device; rollback; commit. Checks that a rollback discards the stops of its own level (the undo restores the device, which keeps running). Passed before too; guards the rollback arm.
- `TestFailedDropKeepsDevice`: device with a table; begin; `drop device` fails on the dependent table; commit. Checks that a failed drop leaves no stop behind. Failed before, passes now.

Validation run:

- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~DeferredDeviceStopTest"`: 4 of 4 pass.
- `dotnet test tests/DAE.Server.Tests --logger "console;verbosity=normal"` (coverage scripts included): 183 total, 158 passed, 25 skipped; known failures 22 matched, 0 mismatched, 0 unexpectedly passing. No coverage script depended on the old timing.

## For the reviewer

- **Not covered by a test:** the nested transaction that `ServerTransaction.InvokeDeferredHandlers` opens during a commit's prepare phase goes through the same nested-commit path as an explicit nested transaction, so it is covered by `TestNestedCommitDefersStop` only by analogy. The failed-nested-begin path (a begin that fails rolls back the levels it started) reaches `RollbackDeferredDeviceStops` at those levels only; not tested separately.
- **Stop failure at the outer commit** is unchanged: if `InternalStopDevice` throws partway, the remaining stops stay in the list and the exception propagates out of the commit; a following rollback at level 1 then discards them. The catalog store has already committed by then (the stops run after `InternalAfterCommitTransaction`). Same as before this change.
- **Observed, not changed (pre-existing):** `DropDeviceMaps` runs before `CheckNoDependents`, and a statement that fails inside an explicit transaction does not roll that transaction back. So a `drop device` that fails at `CheckNoDependents` (a dependent other than a base table or a device map) leaves the device's scalar type and operator maps dropped in the open transaction, until that transaction rolls back. Moving the check first would not work, since the maps are themselves dependents the check would find. This is the general question of whether a failed multi-step DDL statement inside an explicit transaction may leave partial effects, not something local to this node; I filed nothing. The reviewer should decide whether it warrants a `blocked/` question on statement-level atomicity.
