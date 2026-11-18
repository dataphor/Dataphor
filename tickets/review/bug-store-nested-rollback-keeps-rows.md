description: When a nested transaction that changed the schema is rolled back inside an outer transaction that then commits, the server forgot the change in memory but kept it in its saved catalog on the SQLite store (and a rolled-back drop lost the saved object), so the catalog on disk no longer matched the server after a restart. Fixed; review the fix.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, tickets/backlog/feat-ws4-store-followups.md
repro: verified
----
## What was wrong

The catalog store (`SQLStoreConnection`) makes only the outermost transaction level a real database transaction. A nested level is a marker in an undo log (`_operationLog`) followed by the inverse SQL statement of each write made in it; a nested rollback runs those statements newest first, back to its marker. Store cursors log their writes, but `CatalogStoreConnection.InsertRow` and `DeleteRows` write by statement (`PerformInsert` / `PerformDelete`) on every store without updatable cursors, which is every store we build, and logged nothing. Nearly all catalog rows are written that way, so a nested rollback of a `create table` left the table's rows in the store, and of a `drop table` left them deleted, while the in-memory catalog was restored.

## What changed

`src/engine/DAE/Device.Catalog/CatalogStore.cs`

- `InsertRow`, statement path: after `PerformInsert`, at a nested level (`TransactionCount > 1`) it calls `LogInsert` with the row padded with nulls to the table's column count (new private `PadRow`). Callers may pass fewer values than columns; the insert statement stores the missing ones as null, and the inverse delete reads each primary-key column at its position in the row.
- `DeleteRows`, statement path: at a nested level the full-primary-key shortcut (delete by key values, no read) is skipped, so every delete goes through the existing matched-rows path, which reads each matching row whole through a cursor before deleting it. After each `PerformDelete` at a nested level it calls `LogDelete` with that row; the inverse is a re-insert of the full row. At level 1 nothing changes: the shortcut still deletes by key with no read. A key that matches no row now deletes nothing and logs nothing.
- Logging happens only after the write succeeds, as in the cursor paths.

`src/engine/DAE/Store/SQLStoreConnection.cs`

- `CommitTransaction`: when the count reaches 0 the undo log is cleared, before the database commit (so a failed commit leaves no residue for a following rollback to replay). At a nested level it removes the level's marker, as before. Previously the entries that nested levels committed into the outermost level stayed in the log for the life of the connection (found by reading; not measured).
- `RollbackTransaction`: when the count reaches 0 the log is cleared and **not replayed**, then the database transaction is rolled back. This is the ticket's optional item. Taken because the replay was redundant work (the database rollback undoes every level) and because an undo statement that threw during it stopped the method before the database rollback, leaving the database transaction open with the count at 0. At a nested level the pop-and-undo loop is unchanged.

The fix is generic: it also applies to the SQL Server and PostgreSQL stores, which are not built yet, so it is verified on SQLite only. It is internal: no compatibility surface, no migration note.

## Tests

- `CatalogStoreTest.NestedRollbackUndoesStatementWrites(False)`: begin, begin nested, `create table NestedT`, roll back nested, commit outer. Asserts the table is absent from the in-memory catalog and that every row of every table in the SQLite catalog file is as it was before the transaction (helper `StoredCatalog`, which reads the file directly). Without the fix it fails with 21 extra rows (first one `DAEDeviceObjects: …`).
- `CatalogStoreTest.NestedRollbackUndoesStatementWrites(True)`: same, with `drop table NestedT` on a table created and committed beforehand. Asserts the table is back in memory and the stored catalog is unchanged. Without the fix it fails with 23 rows missing (first one `DAECatalogObjectNames: 0 | General.NestedT | …`).

Both were run against the code with the two logging sites disabled and failed as described, then passed with the fix. The whole-store comparison also shows that the re-inserted rows round-trip exactly on SQLite (same values, same text after `Convert.ToString`).

No test for the undo-log clearing: the log is private, and its only effect is memory or (for the outermost rollback) not running redundant statements; observing it would mean reading a private field. The outermost rollback path is exercised by `RollbackRestoresCachedOwner(False)` and the coverage scripts.

Validation run: `dotnet build Dataphor.slnx` (0 errors) and `dotnet test tests/DAE.Server.Tests` (154 passed, 0 failed, 25 skipped, which are the coverage manifest's known/unstable/fragment cases; 5 m 13 s). `CatalogStoreTest` alone: 16 passed.

## For the reviewer

- Values round-trip through `NativeToLiteralValue`, which formats anything other than bool, string and null with `ToString()`. The catalog tables hold only integers, tinyints and text, so that is exact today, and the cursor paths already re-insert rows the same way. A double or date column added to a catalog table would make re-inserts culture-dependent; not addressed here.
- A nested-level delete by full primary key now costs a read before the delete. Only nested levels pay it; level 1 is unchanged.
- Skipping the replay at the outermost rollback relies on every store opening a real database transaction at level 1. All three do (`SQLiteStoreConnection.InternalBeginTransaction` forces an immediate transaction; the base class calls the connection's `BeginTransaction`). A store that did not would already lose level-1 writes on rollback, since level-1 writes are never logged.
- Pre-existing, not changed: if an undo statement throws during a nested rollback, the remaining entries and the level's marker stay in the log and the count is already down. The outer rollback that normally follows now clears the log, so the residue does not survive the transaction.
- The cursor insert path (`SQLStoreCursor.Insert`, used only when a store supports updatable cursors, which none does) logs the row as given, without padding, so a caller that passed fewer values than the key's last column position would make `LogInsert` index past the row. Not checked which callers pass short rows; the path is dormant and was left alone.
- `tickets/backlog/feat-ws4-store-followups.md`: the `test-store-contract` arm now says the defect is fixed and pinned on SQLite by the new test, and that the store contract tests should absorb it and run it against every built store.
