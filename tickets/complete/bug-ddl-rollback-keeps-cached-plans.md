description: After a transaction that created a table or an operator is rolled back, queries and operator calls that were already run inside that transaction kept working against the object that no longer exists, in every session, until the server restarted; the rollback now also discards what the server cached about the rolled-back objects.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/PlanCache.cs, tests/DAE.Server.Tests/CatalogStoreConcurrencyTest.cs
repro: verified
----
## What changed

A rollback undoes the session's logged DDL instructions in `CatalogDeviceSession.InternalRollbackTransaction` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`). It now notes whether any instruction other than `BeginTransactionInstruction` was undone. If one was, it calls the new private `InvalidateCachesAfterUndo` in the `finally`, before `InternalAfterRollbackTransaction`, so the call also happens when an undo throws and is logged. That method:

- bumps the server plan cache timestamp, the client-side catalog cache timestamp and the Frontend derivation timestamp, the same three the forward drop nodes in `DDLNodes.cs` move;
- clears `Catalog.OperatorResolutionCache` whole;
- clears `Catalog.ConversionPathCache` whole, under the lock that `Compiler.FindScalarConversionPath` holds while it reads and fills that cache. Without this clear, a rolled-back `create conversion` still resolved through the cached path.

`Catalog.TimeStamp` is not bumped, because the forward drop nodes do not move it either. The invalidation runs for every undone instruction, including the undo of a drop, and for nested rollbacks. The server's `ServerCatalogDeviceSession` inherits it from the base class.

`PlanCache.Add` carries a `NOTE:` tripwire. A plan is stamped when it is added, not when its compilation began, so a plan another session compiles against the old catalog and adds just after the bump survives. The forward drop nodes leave the same window.

The reproduction is `CatalogStoreConcurrencyTest.TransactionReadsItsOwnStoreWrites`. After the rollback, texts compiled inside the transaction (`Count(RWT)`, `RWO(1)`) and new texts that only the operator resolution and conversion path caches could answer (`RWO(3) + 0`, `2 + true`) must now fail with unknown identifier or operator not found. The implementer confirmed that each arm fails when its part of the fix is removed.

No migration note: the change only makes a rolled-back object unresolvable, as it already was after a restart.

## Review findings

Read the diff of `ticket(implement): bug-ddl-rollback-keeps-cached-plans` before the handoff, then the surrounding rollback, instruction-logging, cache and compiler code.

- **Correctness.** No defects found.
  - Ordering: the bump happens after every undo, so a plan another session cached while the undo was running is stamped older and is invalidated.
  - Nested rollbacks stop at their own level's begin marker and invalidate the same way.
  - Taking the conversion path cache's lock is safe from deadlock. While the compiler holds it, it only walks `ImplicitConversions` and `ScalarType.Is` and takes no other lock and no store access, and the rollback thread's `Catalog` locks in the timestamp updaters are separate and re-entrant.
  - `CatalogDeviceSession.Catalog` is the engine's catalog, the same object that `program.Catalog` returns at the forward sites.
- **Performance and scope.** I checked whether ordinary queries can log instructions that would flush the caches for every session on rollback.
  - Sorts are created only by explicit DDL nodes.
  - Two non-DDL paths do log instructions: starting a device on first use (`StartDeviceInstruction`), and the device operator map the compiler generates for a like type (`Compiler.cs` about line 9204, `AttachDeviceOperatorInstruction`). Invalidating after undoing either of them is correct, because cached plans may reference the removed map.
  - The cost would only matter for a workload that repeats such rollbacks. Parked as a `NOTE:` tripwire in `InvalidateCachesAfterUndo`.
- **Adjacent defect.** A fourth site clears the conversion path cache without its lock: the admin operator `ClearConversionPathCache()` (`SystemClearConversionPathCacheNode`, `SystemNodes.cs` about line 2528). It is the same class as the backlog ticket `bug-conversion-path-cache-unlocked-clears`, which the implementer filed. I added this site to that ticket as a fourth arm, added the file to its `files:`, and corrected its wording: the whole `Clear()` writes to the dictionary but does not enumerate it. No new ticket.
- **Tests.**
  - The extended reproduction meets the project's rule that a bug fix owes its reproduction. Each assertion pins a separate cache.
  - No test restates the implementation, and none was cut.
  - Not added: tests for nested rollback, the undo of a drop, and the client-cache and derivation timestamps. All of them go through the same unconditional flag and call with no further branching. The two timestamps have no consumer in the in-process tests.
- **Error handling and resource cleanup.** The invalidation sits in the `finally` and runs even when an undo throws (undo errors are logged and the loop continues). It allocates nothing and holds no resources.
- **Type safety and source hygiene.** Private method, no public surface.
  - Comments state reasons: why the clears are whole, and why the lock is taken.
  - The UTF-8 byte-order mark is preserved in all three files. `CatalogDeviceSession.cs` and `PlanCache.cs` stay CRLF in the index. The test file was LF in the index before and after.
- **Docs.** I read `docs/modernization-plan.md` (the known-defects table and WS2) and `docs/architecture.md` for the plan cache and DDL undo. Neither describes the undo path's cache handling, and the fix ticket asked for no known-defects row, so nothing needed updating. `docs/migration-notes.md` needs no entry, for the reason above.
- **Validation.**
  - `dotnet test tests/DAE.Server.Tests`: 167 tests, 142 passed, 25 skipped (the manifest's known-failure, unstable and fragment cases), 0 failed. Log: `tickets/.logs/bug-ddl-rollback-keeps-cached-plans.review.test.log`.
  - After the comment-only `NOTE:` edit, `dotnet build src/engine/DAE/DAE.csproj`: 0 errors. The tests were not rerun for that edit.
