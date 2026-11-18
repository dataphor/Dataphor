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

## Review findings

Read the diff of `ticket(implement): bug-catalog-deferred-device-stops-ignore-nesting` before the handoff, then the code around it: `Schema.DeviceSession` begin/commit/rollback (`src/engine/DAE/Schema/Device.cs`), `ServerProcess` begin/prepare/commit/rollback and `DeviceConnect`, `ServerTransaction.InvokeDeferredHandlers`, the undo path `StartDeviceInstruction.Undo`, and every caller of `StopDevice`.

- **Correctness of the level bookkeeping — checked, no defect.** The level a stop is tagged with is the catalog device session's `Transactions.Count`, and that always equals the process's transaction level: `DeviceConnect` begins as many levels as the process has open when it connects a session mid-transaction, and `InternalBeginTransaction` begins every connected session. Inside `InternalCommitTransaction` / `InternalRollbackTransaction` the level has not ended yet (`CommitTransaction` / `RollbackTransaction` end it afterwards), so `Count` is the level ending, as the code comment says. The nested transaction that `InvokeDeferredHandlers` opens during prepare goes through `ServerProcess.InternalBeginTransaction` / `InternalCommitTransaction`, the same path as an explicit nested transaction, so a drop in an event handler is handed to the committing level and stopped at the outermost commit. Begin-failure unwinding (`RollbackBegunParticipants`) rolls back only the level just begun, which holds no stops. The undo of a create (`StartDeviceInstruction.Undo` → `StopDevice(device, true)`) still stops at once, and a rollback's `RollbackDeferredDeviceStops` runs in the `finally`, so it runs even when an undo throws.
- **Error paths — checked, unchanged.** A stop that throws at the outermost commit leaves the rest in the list and propagates, after the catalog store committed; same as before. A `DropDevice` that throws after `StopDevice` in an explicit transaction still leaves a stop recorded, but by then `DeleteCatalogObject` has already removed the device from the catalog cache and logged its undo, so that is the same partial-statement case as the next bullet, not new to this change.
- **Pre-existing partial effects of a failed `drop device` (raised by the implementer) — not filed; tripwire.** A `drop device` that fails at `CheckNoDependents` leaves the device's maps dropped in the open explicit transaction. This is not local to the node: no multi-step DDL statement undoes its earlier steps when a later one fails inside an explicit transaction, and the engine rolls a transaction back on a failed statement only on a stop error. That is legacy D4 behaviour, which is the specification; making statements atomic would change D4 semantics and no current plan section asks for it, so it does not meet the filing bar. Recorded as a `NOTE:` above `DropDeviceMaps` in `DropDeviceNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`) with the revisit condition.
- **Ordering change outside a transaction — checked.** With implicit transactions off, the device's maps are now dropped while it is still running; the transactional path always did that, and `Device.Stop` touches only the device's own sessions, not the catalog, so neither order depends on the other.
- **Tests — kept all four, added none.** Each pins one arm (nested rollback keeps the outer stop, nested commit hands it over, rollback discards its own level, failed drop records nothing), three of them failed before the fix; none restates the implementation or uses a mock. The prepare-phase nested transaction is the same code path as `TestNestedCommitDefersStop`, so a separate test would not pay for itself.
- **Source hygiene — fine.** The private `DeferredDeviceStop` class and the two replaced methods are small and commented with the why (the level meaning, the commit/rollback rules). `DDLNodes.cs` kept its CRLF and byte-order mark; the edit also dropped trailing tabs from the one line it touched.
- **Docs and migration notes — none needed.** No document under `docs/` describes deferred device stops (searched `architecture.md`, `modernization-plan.md`, `migration-notes.md`). The visible change is only when a dropped device stops, on paths that were wrong before; no compatibility surface changed, so no migration note.
- **Validation.** `dotnet build Dataphor.slnx`: 0 errors. `dotnet test tests/DAE.Server.Tests --no-build`: 183 total, 158 passed, 25 skipped (the known-failure, unstable and fragment coverage entries), 0 failed.
