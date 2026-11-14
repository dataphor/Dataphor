description: When a nested transaction that changed the schema is rolled back inside an outer transaction that then commits, the server forgets the change in memory but keeps it in its saved catalog on the SQLite store, so the rolled-back object can come back (or fail to load) after a restart.
prereq: store-sqlite-concurrency
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, src/engine/Store.SQLite/SQLiteStoreConnection.cs
repro: static
severity: corruption
likelihood: unusual
tradeoffs: Needs an explicit nested transaction that changes the schema and is rolled back while its outer transaction commits, which few applications do; a maintainer may wait for the store contract tests to pin it before choosing between the two fixes.
----
## What happens

The catalog store emulates nested transactions: only the outermost level is a real SQL transaction, and each nested level keeps an in-memory log of inverse statements that a nested rollback replays (`SQLStoreConnection`, `BeginTransactionOperation` / `ModifyOperation`). Only writes made through a store cursor are logged (`SQLStoreCursor.Insert`/`Update`/`Delete`, when `TransactionCount > 1`).

On a store whose cursors are not updatable — SQLite, and also the SQL Server and PostgreSQL stores (`_supportsUpdatableCursor = false`) — `CatalogStoreConnection.InsertRow` and `DeleteRows` (`src/engine/DAE/Device.Catalog/CatalogStore.cs`) write with generated statements through `SQLStoreConnection.PerformInsert` / `PerformDelete` and **log nothing**. Most catalog writes go that way: `DAECatalogObjects`, `DAEOperators`, `DAEObjectDependencies`, `DAERights`, users, loaded libraries and so on. Only the cursor `Update` paths (and a few cursor inserts) are logged.

The in-memory catalog's own undo (`CatalogDeviceSession.InternalRollbackTransaction`, `CreateCatalogObjectInstruction.Undo` → `ClearCatalogObject`) removes objects from memory only and relies on the store to roll back its own rows.

So: outer transaction begins; nested transaction begins; `create table T` (rows inserted by statement, not logged); nested rollback (memory forgets T, store keeps its rows); outer commit (store commits T's rows). After a restart the catalog loads T from the store, or fails on a row whose dependencies were rolled back. The SQL Server Compact store that 3.1 shipped as the default inserted through updatable cursors, so it logged these writes; the gap became the default with the SQLite store.

## Reproduction (static; what would confirm it)

Not run. A D4 script or in-process test: begin a transaction, begin a nested one, create a table, roll back the nested transaction, commit the outer one, restart the server; expected the table does not resolve; predicted it resolves (or the start reports a load error). Reading `DAECatalogObjects` from the catalog file directly after the commit shows the row without a restart.

## Fix options

- **SQLite savepoints** (SQLite-specific, in `SQLiteStoreConnection`): nested levels issue `SAVEPOINT` / `RELEASE` / `ROLLBACK TO`, which undo every statement uniformly, and the cursor undo log is switched off for this store (a virtual on `SQLStoreConnection`, so the log and the savepoint do not both undo a cursor update). Must work with the lazy write transactions of `store-sqlite-concurrency`, where the first write can happen at a nested level (the catch-up then has to create a savepoint per caught-up nested level).
- **Log statement writes** (generic, in `CatalogStoreConnection.InsertRow` / `DeleteRows`): call `LogInsert` / `LogDelete` at nested levels. A delete's undo needs the full row, but the unique-key path of `DeleteRows` builds a row from the key values only, so it must read the row first. Also fixes the other stores.

The store contract tests (`test-store-contract`, `feat-ws4-store-followups`) include nested transaction undo and should pin this either way.

Related, from `test-triage-known-failures`: `store-cursor-update-wrong-rows` (fix/) changes how a cursor update or delete identifies its row (today by the columns of the index the cursor was opened on, so a cursor on a non-unique index rewrites every row sharing the value). The undo log here generates its inverse statements from the same key, so whichever lands second must keep the two consistent.
