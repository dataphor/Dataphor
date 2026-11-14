description: After a transaction that created a table or an operator is rolled back, queries and operator calls that were already run inside that transaction keep working against the object that no longer exists, in every session, until the server restarts; the same text compiled fresh correctly reports the object as unknown.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/PlanCache.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs, src/engine/DAE/Schema/Catalog.cs, tests/DAE.Server.Tests/CatalogStoreConcurrencyTest.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Only a statement whose exact text, or an operator call whose signature, was already used inside the rolled-back transaction is affected, and a restart clears it; a maintainer may defer it behind work that applications hit in normal use.
----
## What happens

Found by `store-sqlite-concurrency` while writing `CatalogStoreConcurrencyTest.TransactionReadsItsOwnStoreWrites`; it behaves the same on the commit before that ticket, so it does not involve the catalog store.

In one explicit transaction: `create table RWT { ID : Integer, key { ID } }`, insert a row, evaluate `Count(RWT)` (1); `create view RWV RWT where ID > 0`; `create operator RWO(const A : Integer) : Integer begin result := A + Count(RWV); end`, evaluate `RWO(1)` (2). Roll back. Then, in the same process and in a new session:

- `Count(RWT where ID = 1)` and `Count(RWV where ID = 1)` (never compiled before) fail with 105167, unknown identifier: the in-memory catalog was rolled back correctly.
- `Count(RWT)` (the exact text compiled inside the transaction) still evaluates.
- `RWO(2)` and `RWO(3) + 0` still evaluate, in the same process and from a new session, although `RWO` is no longer in the catalog (`Catalog.OperatorMaps` has no `RWO`, the store has no row for it).
- After a restart, `RWO(1)` fails with 105149, operator not found.

## Cause

The rollback undoes each logged DDL instruction (`CatalogDeviceSession.InternalRollbackTransaction` → `DDLInstruction.Undo`; for a create, `CreateCatalogObjectInstruction.Undo` → `ClearCatalogObject`), which removes the object from the in-memory catalog but invalidates nothing derived from it:

- The plan cache (`PlanCache`) keeps a plan while `Catalog.PlanCacheTimeStamp` has not moved past the plan's; the drop nodes advance it (`UpdatePlanCacheTimeStamp` in `DDLNodes.cs`), the undo does not.
- The operator resolution cache (`Catalog.OperatorResolutionCache`) is cleared by operator name when a create or drop operator is compiled (`Compiler.cs`), not when an operator's creation is undone, so the signature resolved inside the transaction keeps resolving to the dropped operator.

## Expected

A rolled-back create leaves nothing that resolves to the object: cached plans that reference it are recompiled (and fail as unknown), and an operator call resolves afresh. Which caches a rollback must invalidate should match what the corresponding forward drop invalidates (plan cache timestamp, operator resolution entries, and whatever else the drop nodes update, such as the cache and derivation timestamps); the fix site is the undo path in `CatalogDeviceSession`, one place for every kind of DDL instruction.

The reproduction is the sequence above; `TransactionReadsItsOwnStoreWrites` currently works around it (fresh expression texts, and the operator checked after a restart), and the fix should change it to check `Count(RWT)` and `RWO(1)` directly after the rollback.
