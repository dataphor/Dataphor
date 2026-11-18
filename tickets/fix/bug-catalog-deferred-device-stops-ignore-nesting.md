description: When a transaction drops a device, the server waits for the transaction to commit before stopping it, but it keeps that pending stop in one list for the whole session instead of per nested transaction level, so a nested commit stops the device too early and a nested rollback forgets the stop.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs
repro: static
severity: edge-case
likelihood: unusual
tradeoffs: Dropping a device inside an explicit transaction that then nests another transaction is rare in practice, and nothing has been seen to fail from it; a maintainer may defer it behind work that customers hit.
----
## What is wrong

Dropping a device inside a transaction does not stop the device at once: `CatalogDeviceSession.StopDevice` adds it to `_deferredDeviceStops`, so that work already done in that device can still commit. The list is one field on the catalog device session, not tied to a transaction level:

- `InternalCommitTransaction` calls `ExecuteDeferredDeviceStops` on **every** commit, nested or not. A nested commit (an explicit nested transaction, or the nested transaction a commit opens to run deferred event handlers, `ServerTransaction.InvokeDeferredHandlers`) therefore stops the dropped device, disposing every session of it, while the outer transaction that dropped it is still open and may still have work in it to commit or roll back.
- `InternalRollbackTransaction` calls `ClearDeferredDeviceStops` on **every** rollback. A nested rollback inside the transaction that dropped the device forgets the stop, and when the outer transaction commits the dropped device is never stopped (its sessions and connections stay open).

Every nested rollback of the catalog session reaches the second case, including the one a failed nested begin now performs (`bug-process-begin-leaves-partial-transaction`, which rolls back the levels a half-done begin started).

## Expected behaviour

A deferred device stop belongs to the transaction level that dropped the device: a nested commit hands its pending stops to the enclosing level, a nested rollback discards only the stops its own level added (the undo of the drop restores the device), and the stops run only when the outermost transaction commits.

## Confirming it

Static reading only. A characterization test in `DAE.Server.Tests`: create a memory device, then in an explicit transaction drop it, begin and roll back a nested transaction, commit, and check the device object is no longer running; and a second case where a nested commit after the drop leaves the device running until the outer commit.
