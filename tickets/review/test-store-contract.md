description: A new test file pins the behaviour every catalog store must have — cursors, ranges, nested-transaction undo and restart persistence — and runs it against the SQLite store, so a later change to a store is measured against one written description of it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/StoreContractTests.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj
----
## What landed

`tests/DAE.Server.Tests/StoreContractTests.cs` — an abstract fixture holding 11 tests, with one concrete subclass per catalog store we build. `SQLiteStoreContractTests` runs all 11 today. The two direct-cursor tests and their two helpers were removed from `CatalogStoreTest.cs`, along with the four `using` lines that became dead with them.

The abstract base takes a store from its subclass through two hooks — `CreateStore(directory)` and `DisposeStore(store, directory)` — plus `ReopenStore()` for the one test that needs a second open on the same database. Each test gets its own directory and creates the small schema it needs (`T { K primary key, G, V }`, four rows in two groups), so nothing carries between tests. The catalog's own seed script is used by two tests only.

## Read this first: the partial commit carried a live mutation

The prior interrupted run (commit `01e01583`, "partial — salvaged from interrupted run") got all 11 tests green, then began verifying each test by deliberately breaking the behaviour it claims and checking the test noticed. The runner killed it mid-verification, and the salvage committed the working tree **with the last mutation still in place**: `OpenGroupedCursor` passed the non-unique index column `G` as the cursor's row key instead of the primary key `K`.

That is not a cosmetic difference. The row key is what `SQLStoreCursor` puts in the `where` clause of its update, delete and undo-log statements (`SQLStoreConnection.AppendKeyCondition`), and `CatalogStore.cs:830` shows the catalog always passes the table's primary key columns. With `G` there, four of the eleven tests failed. Reverted to `K` in this ticket; all 11 pass again.

Two consequences for the reviewer:

- The engine sources the prior run also patched (`src/engine/DAE/Device.Catalog/CatalogStore.cs`, `src/engine/DAE/Store/SQLStoreCursor.cs`, `src/engine/Store.SQLite/SQLiteStoreConnection.cs`) were checked and are **clean**: the working tree was clean at HEAD, neither salvage commit touched them, and their last commits are the legitimate `bug-store-nested-rollback-keeps-rows` and `store-cursor-update-wrong-rows` ticket commits. No mutation survived in engine code.
- The revert accidentally produced a good mutation test. Flipping the row key from the primary key to the non-unique index column is exactly the shape of `bug-set-object-owner-rollback-keeps-owner`, and four tests caught it.

## Tests added, and what each one verifies

All on the base, so each runs once per store.

| Test | What it verifies | Caught a real break? |
| --- | --- | --- |
| `SeedSchemaReopens` | A new database seeds itself from the store assembly's embedded `SystemStoreCatalog.sql`, and a second store opened on the same directory finds what the first wrote — what a server restarting on an existing instance depends on. Stands in for the deleted `SQLStoreTest` restart check. | not verified by mutation |
| `RowReadThroughSameCursorAfterUpdate` | A row updated through a cursor is read back through that same cursor once re-ranged. (From the deleted `SQLCEReadAfterUpdateTest`.) | yes — row key |
| `RowInsertedThroughOneCursorReadByAnother` | A row inserted through one cursor is read by another cursor on the same index and connection, in one transaction. | not verified by mutation |
| `CursorsInSequenceWithinOneTransaction` | Cursors opened one after another on the same table each see what the earlier ones wrote; the catalog opens a cursor per write. | not verified by mutation |
| `TwoCursorsOpenOnOneTableInOneTransaction` | A second cursor opened, read and closed part way through a first one's range does not move the first one. | not verified by mutation |
| `CursorMovesFromUpdatedRow` | After `Update()` the cursor stays on the row, and `Prior()`/`Next()` continue from its new key. Moved down from `CatalogStoreTest`. | yes — row key |
| `CursorMovesFromDeletedRow` | After `Delete()` the cursor sits between the deleted row's neighbours, so a `Next()`/`Delete()` loop removes exactly the range. Moved down from `CatalogStoreTest`. | yes — row key |
| `NestedRollbackUndoesStatementInsert` | A nested level's rollback undoes a row inserted by statement in it. Reproduction, below the server, of `bug-store-nested-rollback-keeps-rows`. | yes — nested-level log disabled |
| `NestedRollbackUndoesStatementDelete` | The delete arm of the same bug: a nested rollback puts the row back with the values it had. | yes — nested-level log disabled |
| `NestedRollbackReinsertsCursorUpdates` | A nested rollback of a cursor update restores that row alone, not every row sharing its index value. Row-identity half of `bug-set-object-owner-rollback-keeps-owner`. | yes — twice, two different breaks |
| `OuterRollbackDoesNotReplayUndoLog` | The outermost rollback undoes everything including a committed nested level, leaving nothing for a later connection; the undo log is cleared at the outermost end and the database's own rollback does the work. | not verified by mutation |

Seven of the eleven are confirmed to fail when the behaviour they describe is broken. The five marked "not verified by mutation" are green and were read for correctness, but nobody has shown they would go red — the prior run was killed before reaching them, and this run spent its mutation budget on the row-key revert instead. Worth a reviewer's eye, in rough order of doubt: `CursorsInSequenceWithinOneTransaction` and `TwoCursorsOpenOnOneTableInOneTransaction` make several assertions each and could pass for the wrong reason.

## Deliberate deviations from the implement ticket

Three places where this does not match the ticket's shape. Each is a judgement call a reviewer may want to reverse.

**`SupportsUpdatableCursor` hook not added.** The ticket offered it as an optional `virtual` so `CursorMovesFromDeletedRow` could be skipped on stores without updatable cursors. No store we build reports `true` — SQLite does not, and `MSSQLStore.InternalInitialize` sets it `false` — so the hook would have had one implementation, one value and no caller. Left out; a `NOTE:` at the test says the delete goes through the statement path and what would change if a store ever supported cursor deletes. Add the hook when a store actually needs it.

**The SQL Server subclass is written but not compiled.** `MSSQLStoreContractTests` sits behind `#if HAVE_MSSQL_STORE`, which nothing defines. `DAE.MSSQL.csproj` is still the legacy VS2003 project and is not in `Dataphor.slnx`, so the test project cannot reference the SQL Server store at all. The class carries the fixture-level `CREATE DATABASE`/`DROP DATABASE` wiring, the drop-first-then-create guard against a crashed earlier run, a per-test table clear, and an `Assert.Ignore` naming `DAE_MSSQL_CONNECTION` when that variable is unset. **It has never been compiled or run — treat it as a sketch, not as working code.** `store-mssql-port` decides whether the SQL Server store is kept; if it is, that ticket converts the project, adds the project reference and defines `HAVE_MSSQL_STORE`, and should expect to debug this class rather than inherit it working.

**No extra restart test on the SQL Server subclass.** The ticket asked for one behaviour test on that subclass today — a restart-and-reopen round-trip. `SeedSchemaReopens` on the base *is* that round-trip and will run on the subclass the moment it compiles, so a second copy would have been redundant. The honest gap is that it has never executed against SQL Server, only against SQLite.

**Chose `Assert.Ignore` over a hard `#if` for the connection-string gate.** The ticket asked for the choice to be justified. `Assert.Ignore` reports the fixture as skipped *with a reason naming the environment variable to set*, so a developer who wants to run it learns how from the test output. A `#if` would make the fixture vanish silently. The `#if HAVE_MSSQL_STORE` around it is a separate matter — that one is unavoidable, because the reference does not exist.

## Costs measured

The whole SQLite fixture, 11 tests, ran in **1.1 s** (slowest test 222 ms, median 52 ms) — well inside the ticket's "few hundred ms per test" budget, so each test keeps its own database file and no sharing was needed. The ticket's warning about per-test `CREATE DATABASE` on SQL Server was taken at face value and not measured: the SQL Server subclass shares one database per fixture and clears its tables per test.

## Tripwires parked

Both at their code site in `StoreContractTests.cs`, tagged `NOTE:`:

- `ConnectCatalogStore()` builds a `CatalogStore` without a started engine, which leaves the inner store's `SupportsMARS` and `SupportsUpdatableCursor` at `false`, so the two nested-rollback tests take the statement path. That is the same path a server takes today for every store we build. If a store ever reports updatable cursors, those two tests would need to drive it through a running server to exercise the path a server would take.
- The cursor-delete path note described under the `SupportsUpdatableCursor` deviation above.

One non-obvious constraint is also documented at its site, as a plain comment rather than a tripwire, because it is a fact rather than a condition: the shared table declares its key `int`, not `integer`, because `System.Data.SQLite` maps `integer` to `Int64` and `int` to `Int32`, the catalog's own tables are `int`, and `SQLStoreCursor` compares a read key against a range value through `IComparable`, which throws when the two are different integer types. The tests moved down from `CatalogStoreTest` therefore compare against `1` where the originals compared against `1L`.

## Tests deliberately not written

The umbrella's "still without a test" list is still uncovered, named here so it is not rediscovered:

- An `Update()` that moves the row out of the cursor's range. Found by hand in an earlier review, with no user path through it today. `store-parameterized-statements` changes the three statement generators and is the cheaper place to add it.
- A cursor opened without its column list. No caller in the repository uses that overload; checked by hand once.

## Verification run

- `dotnet build Dataphor.slnx` — 0 errors, 143 warnings (all pre-existing).
- Part 1 (`class != CoverageScripts`) — 366 passed, 0 failed in `DAE.Server.Tests`; 13 passed, 0 failed in `Common.Tests`.
- Part 2 (coverage scripts `[A-Ra-r]`) — 67 passed, 0 failed, 11 skipped.
- Part 3 (coverage scripts `[^A-Ra-r]`) — 48 passed, 0 failed, 14 skipped.
- `SQLiteStoreContractTests` — all 11 green, and they appear in the output under that fixture name as the ticket asked.

Skipped counts in parts 2 and 3 are the pre-existing explicitly-ignored coverage scripts, unchanged by this ticket. No pre-existing failures surfaced, so no `tickets/.pre-existing-error.md` was written.

## What a reviewer should push on

- The five tests with no mutation evidence (listed above). A test that cannot fail is worse than no test, because it reads as cover.
- Whether `CursorMovesFromDeletedRow`'s final `Next()`/`Delete()` loop asserting `deleted == 1` is tight enough, given the two earlier `Delete()` calls in the same test already removed rows from the same group. The test is a faithful move of the one that was in `CatalogStoreTest`, so this is inherited, not introduced.
- Whether `SeedSchemaReopens` asserting on `DAEServerInfo` alone is enough to call the catalog "reopened", or whether it should check a second table.
- The `#if HAVE_MSSQL_STORE` block is 138 lines of never-compiled code. It may be better deleted and left to `store-mssql-port` to write against a store it can actually reference. Keeping it was a bet that the wiring notes are worth more than the risk of stale code; the reviewer may call that the wrong way round.
