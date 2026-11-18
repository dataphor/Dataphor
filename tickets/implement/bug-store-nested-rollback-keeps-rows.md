description: When a nested transaction that changed the schema is rolled back inside an outer transaction that then commits, the server forgets the change in memory but keeps it in its saved catalog on the SQLite store (and a rolled-back drop loses the saved object), so the catalog on disk no longer matches the server after a restart.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, tickets/backlog/feat-ws4-store-followups.md
repro: verified
----
## Decision

The owner took the recommendation: **log statement writes** in `CatalogStoreConnection.InsertRow` and `DeleteRows`, the way store cursors already log theirs. Not SQLite savepoints. The fix is generic, so it also covers the SQL Server and PostgreSQL stores (all three set `_supportsUpdatableCursor = false`). It is internal: no compatibility surface, no migration note.

## How nested store transactions work

The catalog store (`SQLStoreConnection`, `src/engine/DAE/Store/SQLStoreConnection.cs`) makes only the outermost level a real database transaction. Each nested level is a `BeginTransactionOperation` marker in `_operationLog`, followed by `ModifyOperation`s, each holding the inverse SQL statement of one write. `RollbackTransaction` pops and runs those inverse statements, newest first, back to the level's marker; `CommitTransaction` removes the level's marker, so its entries become part of the enclosing level.

Only store cursors add entries: `SQLStoreCursor.Insert` / `Update` / `Delete` call `LogInsert` / `LogUpdate` / `LogDelete` when `TransactionCount > 1`. On a store without updatable cursors (every store we build), `CatalogStoreConnection.InsertRow` and `DeleteRows` (`src/engine/DAE/Device.Catalog/CatalogStore.cs`, around lines 865–933) write with `_connection.PerformInsert` / `PerformDelete` and log nothing. Nearly all catalog writes go that way (`DAEObjects`, `DAECatalogObjects`, `DAEOperators`, `DAEObjectDependencies`, `DAERights`, users, loaded libraries, …). The in-memory catalog's undo (`CatalogDeviceSession.InternalRollbackTransaction`) only touches memory and relies on the store to undo its own rows.

## Reproduction (verified)

Run on SQLite through the in-process harness, as a temporary test in `CatalogStoreTest` (removed again; this ticket lands it). Begin a transaction, begin a nested one, run a schema change, roll back the nested one, commit the outer one, then count the table's rows in `DAEObjects` by reading the catalog file directly:

- `create table NestedT …` in the nested level: in memory the table is gone (correct), the store keeps its row (expected 0, got 1).
- `drop table NestedT` in the nested level, on a table created and committed beforehand: in memory the table is back (correct), the store has lost its row (expected 1, got 0).

The test, as it ran (rename and tidy when landing it; the helpers `RunWithin`, `RunAs` and `TestServerInstance.OpenCatalogStoreFile` already exist in that file):

```csharp
[TestCase(false)]
[TestCase(true)]
public void NestedRollbackUndoesStatementWrites(bool drop)
{
	using (TestServerInstance instance = new TestServerInstance(nameof(NestedRollbackUndoesStatementWrites)))
	{
		instance.Server.Start();
		if (drop)
			instance.ExecuteScript("create table NestedT { ID : Integer, key { ID } };");
		int before = StoredObjectCount(instance, "NestedT");
		RunWithin(instance, () => RunAs(instance, "Admin", process =>
		{
			process.BeginTransaction(IsolationLevel.CursorStability);
			process.BeginTransaction(IsolationLevel.CursorStability);
			process.ExecuteScript(drop ? "drop table NestedT;" : "create table NestedT { ID : Integer, key { ID } };");
			process.RollbackTransaction();
			process.CommitTransaction();
		}));
		Assert.That(instance.Server.Catalog.Contains("NestedT"), Is.EqualTo(drop));
		Assert.That(StoredObjectCount(instance, "NestedT"), Is.EqualTo(before));
	}
}
```

`StoredObjectCount` counted `select count(*) from DAEObjects where Name = @Name or Name like '%.' || @Name or Name like '%' || @Name || '%'` (the table plus the objects generated for it). A stricter check is fine, e.g. the table's own `DAECatalogObjects` row plus a count of its generated `DAEObjects` rows.

## The fix (prototyped and verified)

A prototype of the changes below made both cases pass, kept the 16 `CatalogStoreTest` tests green, and the whole `DAE.Server.Tests` run passed (154 passed, 25 skipped, which are the coverage manifest's known/unstable/fragment cases). The prototype was removed again; re-apply it properly.

- **`InsertRow`, statement path**: after `PerformInsert`, when `_connection.TransactionCount > 1`, call `_connection.LogInsert(tableName, header.Columns, indexHeader.Columns, row)`. Pass a row padded with nulls to `header.Columns.Count`: callers may pass fewer values than columns (`GenerateInsertStatement` already treats missing trailing values as null), and the inverse delete reads each primary-key column by its position in the row.
- **`DeleteRows`, matched-rows path** (any index other than a fully specified primary key): it already reads every matching row through a cursor before deleting; after each `PerformDelete`, when `TransactionCount > 1`, `LogDelete` that row. The inverse is a re-insert of the full row.
- **`DeleteRows`, full-primary-key path**: it has only the key values. When `TransactionCount > 1`, read the row first (`OpenMatchedCursor(indexName, false, keyValues)`, one `Next()` / `Select()`, `CloseCursor`), skip the delete if no row matched, otherwise `PerformDelete` with that row and `LogDelete` it. At level 1 keep the existing key-only delete, with no read.
- Log only after the write succeeds, as the cursor paths do, so a failed write leaves no undo entry.

The other statement writes in `CatalogStore.cs` (the `insert into DAELoadedLibraries …` statements around line 166) run while the store is first created, outside any nested level; leave them.

## Also fix: the undo log is never emptied

`SQLStoreConnection.CommitTransaction` removes one `BeginTransactionOperation` and nothing else, so when the outermost level commits, the entries that nested levels committed into it stay in `_operationLog` for the life of the connection. They are never replayed (a later rollback stops at its own level's marker), so this is a memory leak, not wrong data, but logging every catalog write at nested levels makes it grow with every nested schema change. When `_transactionCount` reaches 0 on commit, clear `_operationLog`. On the outermost rollback the loop already pops back to the first marker; anything left before it is the same stale residue, so clear the log there too once the database rollback has been issued. (Found by reading the code; not measured.)

Optional, only if it stays simple: the outermost rollback replays the undo statements of committed nested levels before rolling back the database transaction, which is wasted work (the database rollback undoes them anyway). Skipping the replay when the count reaches 0 is correct; it is not required by this ticket.

## TODO

- Re-apply the `InsertRow` / `DeleteRows` logging described above in `src/engine/DAE/Device.Catalog/CatalogStore.cs` (preserve the file's tabs, CRLF and byte-order-mark state).
- Clear `_operationLog` when the outermost level commits or rolls back in `src/engine/DAE/Store/SQLStoreConnection.cs`.
- Land the reproduction as a test in `tests/DAE.Server.Tests/CatalogStoreTest.cs` (both the create and the drop case), with a `<summary>` saying what it pins, next to `NestedRollbackRestoresEveryRightsOwner`.
- Run `dotnet build Dataphor.slnx` and `dotnet test tests/DAE.Server.Tests` (about 5–6 minutes; run in the foreground).
- Update the `bug-store-nested-rollback-keeps-rows` arm in `tickets/backlog/feat-ws4-store-followups.md`: the defect is fixed and pinned on SQLite by the new test; the store contract tests should absorb it and run it against every built store.
