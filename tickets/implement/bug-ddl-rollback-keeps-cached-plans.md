description: After a transaction that created a table or an operator is rolled back, queries and operator calls that were already run inside that transaction keep working against the object that no longer exists, in every session, until the server restarts; the rollback must also discard what the server cached about the rolled-back objects.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Server/PlanCache.cs, src/engine/DAE/Compiling/OperatorResolutionCache.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/CatalogStoreConcurrencyTest.cs
repro: verified
----
## What happens

In one explicit transaction: `create table RWT { ID : Integer, key { ID } }`, insert a row, evaluate `Count(RWT)` (1); `create view RWV RWT where ID > 0`; `create operator RWO(const A : Integer) : Integer begin result := A + Count(RWV); end`, evaluate `RWO(1)` (2). Roll back. Afterwards, in the same process or a new session:

- `Count(RWT where ID = 1)` (never compiled before) fails with 105167, unknown identifier: the in-memory catalog itself is rolled back correctly.
- `Count(RWT)`, the exact text compiled inside the transaction, still evaluates.
- `RWO(1)` and `RWO(3) + 0` still evaluate, although `RWO` is gone from the catalog and the store. After a restart `RWO(1)` fails with 105149, operator not found.

It does not involve the catalog store (it behaves the same before the SQLite store work).

## Cause (verified)

A rollback undoes the session's logged DDL instructions (`CatalogDeviceSession.InternalRollbackTransaction`, about line 1450, calling `DDLInstruction.Undo` back to the transaction's `BeginTransactionInstruction`; for a create, `CreateCatalogObjectInstruction.Undo` → `ClearCatalogObject`). `ClearCatalogObject` removes the object from the catalog and clears the device's name caches (`NameCache`, `OperatorNameCache`) but nothing derived from the object elsewhere:

- **Plan cache.** `PlanCache.Get` (`src/engine/DAE/Server/PlanCache.cs`) reuses a cached plan unless `Catalog.PlanCacheTimeStamp` has moved past the plan's stamp. The forward drop nodes advance it (`UpdatePlanCacheTimeStamp` in `DDLNodes.cs`: `DropTableNode`, `DropViewNode`, `DropScalarTypeNode`, `DropOperatorNode`, `DropReferenceNode`, …); an undo does not, so the plan for `Count(RWT)` compiled inside the transaction is reused after it.
- **Operator resolution cache.** `Catalog.OperatorResolutionCache` (shared by every session) maps a call signature to the operator it resolved to. It is cleared by operator name when a `create operator` or `drop operator` statement is *compiled* (`Compiler.cs`, `CompileCreateOperatorStatement` about line 6532, `CompileDropOperatorStatement` about line 10298), never when a creation is undone, so `RWO(Integer)` keeps resolving to the removed operator even when the plan is recompiled.

Checked with a throwaway change (reverted; tree left clean): adding `Catalog.UpdatePlanCacheTimeStamp(); Catalog.OperatorResolutionCache.Clear();` in the `finally` of `CatalogDeviceSession.InternalRollbackTransaction` makes `Count(RWT)`, `RWO(1)` and `RWO(3) + 0` all fail correctly right after the rollback, with `TransactionReadsItsOwnStoreWrites` otherwise unchanged and passing. With the plan cache bump alone, `RWO(1)` still evaluates: both caches must be invalidated.

## Fix

One site for every kind of DDL instruction: `CatalogDeviceSession.InternalRollbackTransaction`. While undoing, note whether any instruction other than `BeginTransactionInstruction` was undone; if so, after the loop (in the `finally`, so it also runs when an undo throws and is logged), bring every cache the forward drop nodes keep in step:

- `Catalog.UpdatePlanCacheTimeStamp()` — server-side plan cache;
- `Catalog.UpdateCacheTimeStamp()` — client-side catalog caches (a client that fetched `RWT`'s descriptor inside the transaction must refetch);
- `Catalog.UpdateDerivationTimeStamp()` — the Frontend server's derived-page cache;
- `Catalog.OperatorResolutionCache.Clear()` — the whole cache, not per name. An undo can also *restore* a dropped operator, conversion or scalar type, which changes which overload a signature resolves to, and the per-object `Clear` overloads don't cover every instruction kind; `CompileDropScalarTypeStatement` already clears the whole cache for the same reason. A rollback that undid DDL is rare, so recomputing resolutions afterwards costs little.

Bumping on every undo, not only on undone creates, keeps it one rule: undoing a drop (which re-adds the object) is just as able to leave a stale plan compiled after the drop inside the transaction. `Catalog.TimeStamp` (the catalog device's cache tables) is not part of drop parity — the drop nodes don't move it — so leave it. Nested rollbacks go through the same loop (back to the nested level's `BeginTransactionInstruction`) and get the same invalidation.

`ServerCatalogDeviceSession` (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs`) overrides only `InternalAfterRollbackTransaction` and calls the base, so the fix in the base class covers the server.

Related, not merged: `bug-set-object-owner-rollback-keeps-owner` adds an undo for a different in-memory change in the same file; it is a separate cause (a change that is not logged at all), and it will get this invalidation for free once its instruction is logged.

Tripwire for the implementer to park as a `NOTE:` in `PlanCache.Add`: `Add` stamps a plan with the timestamp current when it is *added*, not when its compilation *started*, so a plan another session compiled against the catalog just before an undo (or a forward drop) and adds just after the bump would survive with the new stamp. The window is a compile racing the DDL; the forward drop nodes have the same one. Fine now; if stale plans are ever seen under concurrent DDL, stamp the plan before compiling.

No migration note: the change only makes a rolled-back object unresolvable, as it already is after a restart; no application can rely on the old behaviour.

## Test

The reproduction is `CatalogStoreConcurrencyTest.TransactionReadsItsOwnStoreWrites`, which today works around the bug (it checks only expression texts never compiled before, and the operator only after a restart; see its comment naming this ticket). Change it so that directly after `process.RollbackTransaction()`:

- `Count(RWT)` (plus the two existing fresh texts) fails with `UnknownIdentifier`;
- `RWO(1)` (the text compiled in the transaction) and `RWO(3) + 0` (a new text, so only the operator resolution cache can answer it) fail with `OperatorNotFound`;
- remove the workaround comment. Keep the store-row checks and the after-restart `RWO(1)` check: they pin the store side.

The file has a UTF-8 byte-order mark and CRLF line endings; so does `CatalogDeviceSession.cs`. Preserve both.

## TODO

- In `CatalogDeviceSession.InternalRollbackTransaction`, track whether a DDL instruction was undone and, in the `finally`, update the plan cache, cache and derivation timestamps and clear the operator resolution cache; add a short comment saying it mirrors what the forward drop nodes invalidate.
- Add the `NOTE:` tripwire in `PlanCache.Add` described above.
- Update `TransactionReadsItsOwnStoreWrites` as described under *Test*; confirm it fails without the fix and passes with it.
- Run the whole `DAE.Server.Tests` project (`dotnet test tests/DAE.Server.Tests`, about 3.5 minutes) — the coverage scripts include transaction and rollback scripts (`Transaction.*`) whose manifest entries must not change; if one flips, investigate rather than edit the manifest.
