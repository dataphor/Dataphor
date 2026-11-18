description: When a nested transaction that changed the schema is rolled back inside an outer transaction that then commits, the server forgets the change in memory but keeps it in its saved catalog on the SQLite store, so the rolled-back object can come back (or fail to load) after a restart.
prereq: store-sqlite-concurrency
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, src/engine/Store.SQLite/SQLiteStoreConnection.cs
repro: static
severity: corruption
likelihood: unusual
tradeoffs: Needs an explicit nested transaction that changes the schema and is rolled back while its outer transaction commits, which few applications do; a maintainer may wait for the store contract tests to pin it before choosing between the two fixes.
----
**Blocked: design choice between two fixes.** Unblocks when the owner picks one of the two fixes under *Fix options* (or edits the recommendation below). The defect and its reproduction are settled.

## Recommendation

> **Log statement writes** (the generic option). `CatalogStoreConnection.InsertRow` and `DeleteRows` log their inverse statements at nested levels, the way store cursors already do, reading the row first where only key values are at hand. It fixes the SQLite, SQL Server and PostgreSQL stores at once, keeps a single undo mechanism, and doesn't interact with the lazy write transactions of `store-sqlite-concurrency`.

Rejected as the default: **SQLite savepoints**. Savepoints undo every statement the same way, including future write paths nobody remembers to log. But they fix only SQLite, need the cursor undo log switched off for that store, and need a savepoint created for each nested level the lazy write transaction catches up. Choose it if covering write paths that aren't logged yet matters more than one mechanism for every store.

If nothing is decided, the defect stays as it is: rare, and caught by the store contract tests once they exist. Either fix is internal and reversible, with no compatibility surface.

NTA decision: take recommendation

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

Related: `store-cursor-update-wrong-rows` has landed. A store cursor's updates and deletes, and the inverse statements it logs, now name the row by the table's primary key (the cursor's row key, which `CatalogStoreConnection.OpenCursor` passes), and `DeleteRows`' statement path names each row by the primary key too, reading the matching rows first except when it is given a full primary key. So the "log statement writes" option can log `DeleteRows`' deletes with the same primary-key columns; the full-primary-key arm still has only the key values, not the row, so it must read the row before it can log the re-insert.
