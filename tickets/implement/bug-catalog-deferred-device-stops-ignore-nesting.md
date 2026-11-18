description: When a transaction drops a device, the server waits for the transaction to commit before stopping it, but it keeps that pending stop in one list for the whole session instead of per nested transaction level, so a nested commit stops the device too early, a nested rollback forgets the stop, and a drop that fails its checks still stops the device at commit.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/DeferredDeviceStopTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj
repro: verified
----
## What is wrong

Dropping a device inside a transaction does not stop the device at once. `DropDeviceNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`, near the end of the file) calls `CatalogDeviceSession.StopDevice`, which, when the process is in a transaction, adds the device to `_deferredDeviceStops` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, `AddDeferredDeviceStop` / `ExecuteDeferredDeviceStops` / `ClearDeferredDeviceStops`), so that work already done in that device can still commit. Three defects, all reproduced:

- **Nested commit stops too early.** The list is one field for the whole catalog device session. `InternalCommitTransaction` calls `ExecuteDeferredDeviceStops` on every commit, nested or not, so a nested commit (an explicit nested transaction, or the nested transaction that `ServerTransaction.InvokeDeferredHandlers` opens when a commit runs deferred event handlers) stops the dropped device, disposing its sessions, while the outer transaction that dropped it is still open.
- **Nested rollback forgets the stop.** `InternalRollbackTransaction` calls `ClearDeferredDeviceStops` on every rollback, so a nested rollback inside the transaction that dropped the device discards the outer level's stop, and when the outer transaction commits the dropped device is never stopped (its sessions and connections stay open). A failed nested begin reaches this too, since it now rolls back the levels it started.
- **A drop that fails still stops the device.** `DropDeviceNode` calls `StopDevice` first, before `CheckNotSystem`, `CheckNoBaseTableVarDependents` and `CheckNoDependents`. A statement that fails inside an explicit transaction does not roll that transaction back, so the pending stop stays in the list and the next commit stops a device that is still in the catalog (for example one that still has tables, or a system device that a non-system user tried to drop). Outside any transaction (implicit transactions off) the same order stops the device at once even when the drop then fails.

## Expected behaviour

A deferred device stop belongs to the transaction level whose drop recorded it:

- a nested commit hands its pending stops to the enclosing level;
- a rollback discards only the stops recorded at the level being rolled back or deeper (the undo of the drop restores the device, which was never stopped);
- the stops run only when the outermost transaction commits;
- a drop records its stop only once its checks have passed, i.e. `StopDevice` is the last thing `DropDeviceNode` does before `DropDevice`, after `DropDeviceMaps` and `CheckNoDependents`. In the transactional path the stop already runs after the catalog delete (at commit), so moving the call does not change the order of effects there; outside a transaction the device's maps are now dropped while it is still running, which is the order the transactional path already uses.

## Design

The catalog device session's `Transactions` stack is kept in step with the process's (`ServerProcess` begins the missing levels when it connects a device session, `ServerProcess.cs` around line 546), and `Schema.DeviceSession.CommitTransaction` / `RollbackTransaction` call `InternalCommitTransaction` / `InternalRollbackTransaction` before `_transactions.EndTransaction`, so inside both `Transactions.Count` is the level being ended (the existing `LOGDDLINSTRUCTIONS` code already relies on this: `Transactions.Count > 1` means nested).

Record each pending stop with that level:

- `AddDeferredDeviceStop(device)` stores `(device, Transactions.Count)`.
- In `InternalCommitTransaction`: if `Transactions.Count > 1`, re-tag the entries at level `Count` as `Count - 1` (they become the enclosing level's); otherwise run them all as today (`ExecuteDeferredDeviceStops`).
- In `InternalRollbackTransaction` (the `finally`): remove the entries whose level is `>= Transactions.Count`; at level 1 that is all of them, as today.

A small private struct or class for the pair is enough; a stack of lists per level would also work but has to be kept aligned with begin, which the level tag avoids. The undo path (`StopDevice(device, true)` from `StartDeviceInstruction.Undo`) stops at once and is unchanged.

Not a compatibility surface (no D4 syntax, class name, file format or error code changes). The visible change is only when a dropped device stops; no `docs/migration-notes.md` entry is expected unless the implementer finds an extension-visible member changing.

## Reproduction (already in the tree)

`tests/DAE.Server.Tests/DeferredDeviceStopTest.cs` (added to `DAE.Server.Tests.csproj` in the fix stage). Each case creates a memory device, starts it by inserting into a table in it, drops that table, then:

- `TestNestedRollbackKeepsOuterStop`: begin; drop device; begin; rollback; commit — expects the device stopped. **Fails today** (still running).
- `TestNestedCommitDefersStop`: begin; drop device; begin; commit — expects still running; commit — expects stopped. **Fails today** at the first assertion (stopped by the nested commit).
- `TestNestedRollbackDiscardsItsStop`: begin; begin; drop device; rollback; commit — expects running. Passes today; guards the rollback arm of the fix against discarding too little.
- `TestFailedDropKeepsDevice`: device with a table; begin; `drop device` fails on the dependent table; commit — expects running. **Fails today** (stopped at commit).

Run: `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~DeferredDeviceStopTest" --logger "console;verbosity=normal"`.

## TODO

- Tag each deferred stop with its transaction level in `CatalogDeviceSession` and change commit and rollback to hand over / discard by level as above.
- Move `program.CatalogDeviceSession.StopDevice(_dropDevice)` in `DropDeviceNode.InternalExecute` to just before `DropDevice`, after the checks (`DDLNodes.cs` has no byte-order mark; `CatalogDeviceSession.cs` has one; both CRLF and tabs).
- Make all four `DeferredDeviceStopTest` cases pass; do not change their expectations.
- Run the full `dotnet test tests/DAE.Server.Tests` (coverage scripts included, about 3.5 minutes) to check no coverage script depended on the old timing.
