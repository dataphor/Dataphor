description: After a transaction that created a table or an operator is rolled back, queries and operator calls that were already run inside that transaction kept working against the object that no longer exists, in every session, until the server restarted; the rollback now also discards what the server cached about the rolled-back objects.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/PlanCache.cs, tests/DAE.Server.Tests/CatalogStoreConcurrencyTest.cs
repro: verified
----
## What changed

A rollback undoes the session's logged DDL instructions in `CatalogDeviceSession.InternalRollbackTransaction` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, about line 1450). It now notes whether any instruction other than `BeginTransactionInstruction` was undone, and if so, in the `finally` (so it also runs when an undo throws and is logged), before `InternalAfterRollbackTransaction`, calls the new private `InvalidateCachesAfterUndo`, which:

- bumps `Catalog.UpdatePlanCacheTimeStamp()` (server plan cache), `UpdateCacheTimeStamp()` (client-side catalog caches) and `UpdateDerivationTimeStamp()` (Frontend derived pages): what the forward drop nodes in `DDLNodes.cs` move;
- clears `Catalog.OperatorResolutionCache` whole;
- clears `Catalog.ConversionPathCache` whole, under `lock (Catalog.ConversionPathCache)`, the lock `Compiler.FindScalarConversionPath` holds while reading and filling it.

The conversion path cache is **beyond the fix ticket's list**: the ticket named the three timestamps and the operator resolution cache. A rolled-back `create conversion` left the cached path from one type to another pointing at the removed conversion, so a later compile (even after the operator resolution cache was cleared) resolved through it. Verified: with everything but this clear, `2 + true` still evaluated after the rollback. Taking its lock is safe from a deadlock: the compiler takes no other lock while holding it (`TraceScalarConversionPath` walks `ImplicitConversions` only).

`Catalog.TimeStamp` (the catalog device's cache tables) is deliberately not bumped: the forward drop nodes don't move it either. Every undone instruction triggers it, not only undone creates: undoing a drop re-adds the object and can leave equally stale plans. Nested rollbacks run the same loop back to their level's `BeginTransactionInstruction`, so they get the same invalidation. `ServerCatalogDeviceSession` only overrides `InternalAfterRollbackTransaction` (and calls the base), so the server is covered by the base class.

`PlanCache.Add` got the tripwire `NOTE:` the fix ticket asked for: plans are stamped when added, not when compilation began, so a plan compiled against the old catalog by another session and added just after the bump survives. Same window as the forward drop nodes.

No migration note: the change only makes a rolled-back object unresolvable, as it already was after a restart.

## Test

`CatalogStoreConcurrencyTest.TransactionReadsItsOwnStoreWrites` is the reproduction; the workaround comment is gone. Inside the transaction it now also creates `RWC(const A : Boolean) : Integer` and `create conversion Boolean to Integer using RWC widening`, and evaluates `1 + true` (2). Directly after `process.RollbackTransaction()`:

- `Count(RWT)` (compiled in the transaction), `Count(RWT where ID = 1)`, `Count(RWV where ID = 1)` fail with `UnknownIdentifier`;
- `RWO(1)` (compiled in the transaction), `RWO(3) + 0` and `2 + true` (new texts, so only the operator resolution and conversion path caches could answer them) fail with `OperatorNotFound`.

The store-row checks and the after-restart `RWO(1)` check are unchanged.

Confirmed each arm fails without its part of the fix (temporary edits, all reverted; `grep TEMP` on both files is clean):

- no plan cache bump → fails at `Count(RWT)` (evaluates);
- no operator resolution clear → fails at `RWO(1)`; with `RWO(1)` also removed from the list, at `RWO(3) + 0`;
- no conversion path clear → fails at `2 + true`.

Full project: `dotnet test tests/DAE.Server.Tests` — 167 tests, 142 passed, 25 skipped (the manifest's known-failure, unstable and fragment cases), 0 failed; no coverage-manifest entry changed, `Transaction.*` scripts included. Log: `tickets/.logs/bug-ddl-rollback-keeps-cached-plans.test.log`. The whole solution (`dotnet build Dataphor.slnx`) was not built separately; the test build covers DAE, Server and their dependents, and the change adds no public member.

## Known gaps for the reviewer

- Not tested: the client-cache and derivation timestamp bumps (no client or Frontend derivation cache in the in-process tests); a nested-transaction rollback; the undo of a *drop* inside a transaction (restoring an object). All go through the same loop and flag, but none has a test.
- The undo loop runs only under `#if LOGDDLINSTRUCTIONS`, which `CatalogDeviceSession.cs` defines at its top; without it `undidDDL` stays false and nothing is invalidated, which matches there being nothing undone.
- Adjacent defect noticed, not fixed: three compiler sites (create conversion, drop conversion, create type with `like`) clear the conversion path cache **without** its lock, a race of the same class as K3. Filed as `tickets/backlog/bug-conversion-path-cache-unlocked-clears.md` (repro: static).
- Unrelated working-tree change present during this run: `tickets/.garden-report.md` (not mine; left alone).
