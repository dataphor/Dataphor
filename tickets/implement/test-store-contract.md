description: Build an L2 catalog-store contract test that runs against every built store and pins the cursor, range, nested-rollback and restart-persistence behaviour the engine depends on.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/CatalogStoreTest.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs, tests/DAE.Server.Tests/TestServerInstance.cs, src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, src/engine/Store.SQLite/SQLiteStore.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs
difficulty: medium
----
## What this is

Replace the deleted `SQLStoreTest.cs` with an L2 store contract. The contract runs against every store built at head time (today: SQLite; later: SQL Server if `store-mssql-port` keeps it) and exercises the SQL-store surface the catalog depends on — cursors, ranges, nested-transaction undo, and the full-catalog persistence across a server restart. It gives every later store ticket (`store-mssql-port`, `store-parameterized-statements`) a single place to prove the change still obeys the contract.

Catalog-level tests already in `CatalogStoreTest.cs` that exercise the store through the server (nested-rollback statement writes, `SetObjectOwner` on a non-unique-index cursor, `NestedRollbackRestoresEveryRightsOwner`, the two `StoreCursorMovesFrom*` direct-cursor tests) move into the contract so they run against each store, instead of only against SQLite.

## Shape

A `StoreContractTests` fixture (abstract, parameterized) at `tests/DAE.Server.Tests/StoreContractTests.cs`, with one subclass per built store:

- `SQLiteStoreContractTests : StoreContractTests` — always built.
- `MSSQLStoreContractTests : StoreContractTests` — compiled only when `DAE_MSSQL_CONNECTION` is set at test time (kept behind a `#if` or a `[Category("mssql")]` + runtime check that `Assert.Inconclusive`s with the env-var name when unset; pick one in the implement ticket and justify it in the handoff). Lands empty-of-behaviour — the subclass is a `[TestFixture]` + a `CreateStore` override only; the `store-mssql-port` plan decides whether this class is kept, deleted or becomes the connection-string gate in `MSSQLStoreConfigurationManager`.

The base provides, through `protected abstract` or `virtual` hooks:

- `SQLStore CreateStore(string directory)` — returns a store wired to a fresh database under `directory` (SQLite: a `DAECatalog.sqlite` beside the directory; SQL Server: `CREATE/DROP DATABASE` on a server named by the env-var's connection string, with a per-test database name).
- `void DisposeStore(SQLStore store)` — SQLite is file delete; SQL Server is `DROP DATABASE`.
- Optional `bool SupportsUpdatableCursor { get; }` default `false` — one test uses a cursor-path delete loop, and the SQL Server store historically supported it.

Each test lives on the base and takes a store from the current subclass. `TestEnvironment.NewDirectory("StoreContract.<TestName>")` already isolates fixtures; the base uses the same helper.

### Tests the base carries

Named in the plan and the umbrella. Each is a reproduction of a specific behaviour; none of them is wiring.

1. **`SeedSchemaReopens`** — `CreateStore`, dispose, `CreateStore` again on the same directory, open a connection and read one row from `DAEServerInfo`; proves the store seeds itself from `SystemStoreCatalog.sql` on first use and reopens the catalog on later use. Stands in for the deleted `SQLStoreTest`'s restart check.
2. **`RowReadThroughSameCursorAfterUpdate`** — one transaction: open a cursor on a table with a non-unique index, insert, update the row through the cursor, `SetRange(null, null)` and re-read; row has the new values. (From the deleted `SQLCEReadAfterUpdateTest`.)
3. **`RowInsertedThroughOneCursorReadByAnother`** — one transaction: cursor A inserts, cursor B opened on the same index reads the row back. (From the deleted `SqlCeReadAfterUpdateTest`.)
4. **`CursorsInSequenceWithinOneTransaction`** — insert, insert, open a cursor on a range, `Next` finds the first match, close, reopen on a wider range and iterate. (From the deleted `SQLCEStoreTest`.)
5. **`TwoCursorsOpenOnOneTableInOneTransaction`** — cursor A iterating while cursor B opens, reads one row, closes; neither interferes with the other's position. (From the deleted `MSSQLStoreTest`.)
6. **`CursorMovesFromUpdatedRow`** — the body of the existing `CatalogStoreTest.StoreCursorMovesFromUpdatedRow`, generalized to any store: after `Update()` the cursor stays on the row, and `Prior()`/`Next()` continue from the row's new key.
7. **`CursorMovesFromDeletedRow`** — the body of the existing `CatalogStoreTest.StoreCursorMovesFromDeletedRow`: `Delete()` leaves the cursor between neighbours; a `Next()`/`Delete()` loop removes exactly the range. Skipped on stores where `SupportsUpdatableCursor` is false and the test needs it (none do today — see the umbrella); keep the test.
8. **`NestedRollbackUndoesStatementInsert`** — within a nested level, `InsertRow` through `CatalogStoreConnection` using the statement path (every store we build today), then roll the nested level back and commit the outer; the row is gone from the physical table. Reproduction of `bug-store-nested-rollback-keeps-rows`, moved down from the server-level test in `CatalogStoreTest`.
9. **`NestedRollbackUndoesStatementDelete`** — outer-level insert, nested-level `DeleteRows`, nested rollback, outer commit; the row is back. Same reproduction, delete arm.
10. **`NestedRollbackReinsertsCursorUpdates`** — the row-identity half of `bug-set-object-owner-rollback-keeps-owner`: open a cursor on a non-unique index, update one row at a nested level, roll the nested level back and commit the outer; only the updated row's own value is reverted. Generalized reproduction of `CatalogStoreTest.NestedRollbackRestoresEveryRightsOwner`.
11. **`OuterRollbackDoesNotReplayUndoLog`** — insert at level 1, roll level 1 back, open a new connection and verify the table is empty; pins "the undo log is cleared at the outermost end, and the database rollback alone undoes level 1".

### Tests that stay where they are

The server-level catalog tests in `CatalogStoreTest.cs` that drive through the engine (`SetObjectOwnerChangesEveryRight`, `NestedRollbackRestoresEveryRightsOwner`, the `SQLCESettingsAreRejected`, `DefaultStoreIsSQLite`, `UnloadableStoreClassIsReported` cases) stay in `CatalogStoreTest.cs` — they exercise the server's wiring of the store, not the store's contract. Delete only the two `StoreCursorMovesFrom*` tests and the `ConnectGroupedStore`/`OpenGroupedCursor` helpers from `CatalogStoreTest.cs` once their equivalents are on the base and green against SQLite.

### Tests not written

The umbrella's "still without a test" list stays uncovered:

- An `Update()` that moves the row out of the range — a defect found by hand in review, with no user path through it today. Named here so the reviewer and `store-parameterized-statements` know.
- A cursor opened without its column list — unused by every caller in the repository, checked by hand once.

## TODO

- Add `tests/DAE.Server.Tests/StoreContractTests.cs` with the abstract shape above (`CreateStore`, `DisposeStore`, optional `SupportsUpdatableCursor`) and the 11 tests. Each test is self-contained and uses a per-test schema (small tables `T`, `G`, …) created inside its own setup, not a shared seeded schema — the catalog's `SystemStoreCatalog.sql` is for `SeedSchemaReopens` alone.
- Add `SQLiteStoreContractTests` subclass; it compiles today with `[TestFixture]`, `CreateStore` returning a `SQLiteStore` with connection string `Data Source=<directory>\Store.sqlite`, and `DisposeStore` deleting the directory.
- Add `MSSQLStoreContractTests` subclass, inconclusive-skipping when `DAE_MSSQL_CONNECTION` (or whatever name the implementer chooses — justify it) is unset; its body is a `CreateStore` that `CREATE DATABASE`s a per-test name on the configured server, hands the resulting connection string to a `MSSQLStore`, and `DisposeStore` drops the database. The implementer leaves this class **not referenced by the project** if the SQL Server store project is not in the solution at head time (`DAE.MSSQL.csproj` is currently a VS2003 project not in `Dataphor.slnx`); if so, note it clearly in the handoff so `store-mssql-port` picks it up. One behaviour test added on this subclass today, not a full port: a restart-and-reopen round-trip through `CREATE/DROP`, wired like `MSSQLCatalogRegressionTest` in git history at `tests/DAE.Server.Tests/CatalogStoreTest.cs@02841882`.
- Delete `StoreCursorMovesFromUpdatedRow`, `StoreCursorMovesFromDeletedRow`, `ConnectGroupedStore` and `OpenGroupedCursor` from `tests/DAE.Server.Tests/CatalogStoreTest.cs` once their contract-base equivalents pass on SQLite. The two `NestedRollback*StatementWrites` and `NestedRollbackRestoresEveryRightsOwner` tests on `CatalogStoreTest` stay — they prove the server-level composition; the contract tests are the lower-layer reproductions.
- Run the three-part suite named in the root `AGENTS.md`; check that the base's 11 tests land as `SQLiteStoreContractTests.<Name>` in the output.

## Edge cases & interactions

- **Per-store setup cost.** SQLite creates a new database file per test; a per-test fixture costs file I/O and should stay under a few hundred ms. SQL Server `CREATE DATABASE`/`DROP DATABASE` per test will not be bearable — share one database across the fixture and clear tables per test, unless an implement-time measurement shows per-test creation is also fast enough. Record the per-fixture cost in the handoff.
- **Fixture cleanup on test abort.** If a SQL Server test crashes the process, a leftover database stays behind. The subclass's `OneTimeSetUp` drops a database of its own name first, before creating it; verified by hand once.
- **Nested-rollback test leakage onto the physical table.** Each `NestedRollback*` test opens its own store on its own directory and asserts against a plain `select` on the final state; the undo log is private to the connection and does not need direct inspection.
- **Cursor delete on stores without updatable cursors.** `SupportsUpdatableCursor` is `false` on every built store today (SQLite, and also the MSSQL store, per the source), so the cursor-path `Delete()` in `CursorMovesFromDeletedRow` goes through the statement path. Confirmed in `MSSQLStore.InternalInitialize` which sets the flag `false`; no test is skipped by the capability flag today.
- **Fixture start-up when a store class is not in the solution.** If `DAE.MSSQL.csproj` is not in `Dataphor.slnx` at head time, `MSSQLStoreContractTests` cannot reference `MSSQLStore` directly without a project reference. Options for the implementer: add a project reference on the test project only (fine — the test project does not ship), or leave the subclass behind a `#if HAVE_MSSQL_STORE` compiled only when the solution includes the MSSQL store. Decide at implement time and name the choice.
- **Pre-existing failures.** `tickets/.pre-existing-known.md` names three; none touches the store.
- **Interaction with `store-parameterized-statements`.** That ticket changes `GenerateInsertStatement`/`GenerateUpdateStatement`/`GenerateDeleteStatement`; the "Update that moves the row out of the range" case the umbrella leaves open is a small addition there, not here.

## Verification

- `tests/DAE.Server.Tests/StoreContractTests.SQLiteStoreContractTests.*` all green (11 tests).
- `tests/DAE.Server.Tests/CatalogStoreTest.*` still green after the two direct-cursor tests move down.
- `dotnet build Dataphor.slnx` 0 errors; three-part suite from root `AGENTS.md` 0 failures.
