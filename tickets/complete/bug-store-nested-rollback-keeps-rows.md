description: When a nested transaction that changed the schema was rolled back inside an outer transaction that then committed, the server forgot the change in memory but kept it in its saved catalog on the SQLite store (and a rolled-back drop lost the saved object), so the catalog on disk no longer matched the server after a restart. Fixed and reviewed.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, tickets/backlog/feat-ws4-store-followups.md
repro: verified
----
## What was wrong

The catalog store (`SQLStoreConnection`) makes only the outermost transaction level a real database transaction. A nested level is a marker in an undo log followed by the inverse SQL statement of each write made in it; a nested rollback runs those statements newest first, back to its marker. Store cursors logged their writes, but `CatalogStoreConnection.InsertRow` and `DeleteRows` write by statement on every store without updatable cursors (every store we build) and logged nothing. So a nested rollback of a `create table` left the table's catalog rows in the store, and of a `drop table` left them deleted, while the in-memory catalog was restored.

## What changed

- `CatalogStore.cs` `InsertRow` (statement path): at a nested level (`TransactionCount > 1`) logs the inverse delete after the insert succeeds.
- `CatalogStore.cs` `DeleteRows` (statement path): at a nested level skips the delete-by-key shortcut so each matching row is read whole before it is deleted, and logs the inverse re-insert of that row. Level 1 is unchanged.
- `SQLStoreConnection.CommitTransaction` / `RollbackTransaction`: at the outermost level the undo log is cleared (not replayed on rollback; the database rollback undoes every level) before the database commit or rollback. Nested levels behave as before.
- Review: a row shorter than its table (trailing columns left out by the caller, which the insert stores as null) is handled in the statement generator, `SQLStoreConnection.ValueAt`, used by both the insert and the key condition, instead of by padding the row in `CatalogStore.InsertRow` (`PadRow`, removed). This also covers the dormant cursor insert path (`SQLStoreCursor.Insert`), which logged short rows unpadded and would have indexed past the row.

Internal only: no compatibility surface, no migration note.

## Tests

`CatalogStoreTest.NestedRollbackUndoesStatementWrites(False/True)`: a create (or a drop of a committed table) inside a nested level that is rolled back, with the outer level committed; asserts the in-memory catalog and every row of every table in the SQLite catalog file are as before. The implementer confirmed both cases fail without the fix (21 extra rows; 23 missing rows).

## Review findings

Read the implement diff (`ticket(implement): bug-store-nested-rollback-keeps-rows`) first, then the surrounding code.

- **Correctness, happy path:** Every statement write in `CatalogStoreConnection` goes through `InsertRow`/`DeleteRows` (checked by grep: the only other `ExecuteStatement` writes are the one-time seeding of a new store, outside any transaction); updates go through `SQLStoreCursor.Update`, which already logs. All `DeleteRows` callers pass exactly the index's column count, so routing nested full-key deletes through the matched-cursor path changes nothing but cost.
- **Outermost rollback no longer replaying:** checked every store's begin (`SQLiteStoreConnection`, base `SQLStoreConnection` → `SQLConnection.BeginTransaction`): level 1 is always a database transaction. Not replaying is also more correct when the database has already aborted the transaction itself (`InTransaction` false): the old replay would then have deleted or re-inserted rows against a database that no longer held them.
- **Commit failure:** log cleared before the commit; a following rollback finds an empty log and rolls back the database if it is still in a transaction. Fine.
- **Minor, fixed:** short-row handling moved from a `CatalogStore` padding helper into the statement generator (above), retiring the dormant cursor-path defect the handoff left open.
- **Tripwire, parked:** literal formatting uses `ToString()` for anything but bool, string and null; exact for the catalog's integer and text columns (checked the three `SystemStoreCatalog.sql` files: only int, tinyint, nvarchar/ntext/text). `NOTE:` at `SQLStoreConnection.NativeToLiteralValue`.
- **Performance:** a nested-level delete by full key now reads before deleting; only nested levels pay it. Not measured; nested DDL is rare. No ticket.
- **Pre-existing, unchanged:** an undo statement that throws during a nested rollback leaves the remaining entries; the outer rollback that follows now clears them. No ticket.
- **Tests:** the new test is a reproduction at the right layer (through the engine, asserting the store file) and passed twice in a row (no flakiness seen from server-written rows). No test added for the log clearing (private state, no observable effect beyond memory); no test cut.
- **Docs:** `docs/architecture.md` describes the undo log in one line that is still accurate; `tickets/backlog/feat-ws4-store-followups.md` arm updated by the implementer and accurate. No migration note needed.
- **Hygiene:** both edited files keep their BOM, CRLF and tabs.
- **Validation:** `dotnet build Dataphor.slnx` 0 errors; `dotnet test tests/DAE.Server.Tests` 154 passed, 0 failed, 25 skipped (the coverage manifest's known/unstable/fragment cases), 4 m 37 s.
- **Tickets filed:** none.
