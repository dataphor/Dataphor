description: Finish the catalog store work: a contract test every store must pass, the SQL Server store, a tested and timed catalog rebuild from scripts, and parameterized store statements.
architecture: docs/modernization-plan.md#ws4-catalog-store
tradeoffs: The SQL Server store may not be worth porting at all if nobody uses it; the plan allows parking it, so a maintainer may cut that item.
----
## What this is

An umbrella for the WS4 work items left after `store-sqlite-concurrency`. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** `store-sqlite-concurrency` has landed.

## Work items

Scope per the plan's WS4 section ("No migration" and the work-item table).

- `test-store-contract` (from WS3) — L2 store contract: cursors, ranges, nested transaction undo, persistence across restart; replaces the old `SQLStoreTest`. Runs against each store that is built.
  - Arm from `store-sqlite-concurrency` (planning): nested transaction undo is broken for statement-written rows on every store without updatable cursors (SQLite included), filed as `bug-store-nested-rollback-keeps-rows`; the nested-undo contract test should reproduce it, and whichever ticket fixes it lands the test.
  - Arm from `store-cursor-update-wrong-rows`: a store cursor on a non-unique index now orders on the index's columns and then the table's primary key, and its updates and deletes name the row by the primary key. After `Update()` it stays on the row (`Next()`/`Prior()` continue from the row's new values, and a row moved out of the range ends the scan); after `Delete()` `Next()`/`Prior()` continue from the deleted row's neighbours. Pinned on SQLite: the forward update loop and its nested undo through the catalog (`CatalogStoreTest.SetObjectOwnerChangesEveryRight`, `NestedRollbackRestoresEveryRightsOwner`), and, directly on a store cursor, `Prior()`/`Next()` after an update and after a delete and a `Next()`/`Delete()` loop (`CatalogStoreTest.StoreCursorMovesFromUpdatedRow`, `StoreCursorMovesFromDeletedRow`; no store sets `SupportsUpdatableCursor`, so nothing in the catalog deletes through a cursor). The contract tests should absorb those two and run them against every built store. Still without a test: an update that moves the row out of the range, and a cursor opened without its column list (which reads the column names with an extra query; checked once by hand in review, not kept).
  - Arm from `test-harness-inprocess` (its piece `test-server-fixture` deletes `tests/DAE.Server.Tests/SQLStoreTest.cs`; read it from git history). What the deleted tests checked, to carry over against SQLite: (1) inside one store transaction, a row updated through a store cursor is seen with its new values after the cursor re-reads with `SetRange(null, null)` (`SQLCEReadAfterUpdateTest`); (2) a row inserted through one cursor is read back through another (`SqlCeReadAfterUpdateTest`); (3) several store cursors opened, used and disposed in sequence within one transaction — insert, insert, open a range, `Next` (`SQLCEStoreTest`); (4) the same plus two cursors open on one table at once (`MSSQLStoreTest`, against a named SQL Server host — the SQL Server half belongs to `store-mssql-port`).
- `store-mssql-port` — the SQL Server catalog store on `Microsoft.Data.SqlClient` if the port is trivial, else park it under `legacy/`; contract tests where a server is configured.
  - Arm from `test-harness-inprocess`: the deleted `CatalogStoreTest.MSSQLCatalogRegressionTest` (git history, `tests/DAE.Server.Tests/CatalogStoreTest.cs` and `Utilities/MSSQLServerConfigurationManager.cs`) started, stopped and restarted a server on a fresh SQL Server catalog store, dropping and creating its database on `localhost`. The SQLite variant survives on the new fixture; give the SQL Server store the same check, driven by a connection string from an environment variable and reported visibly when none is set.
- `store-rebuild-workflow` — rebuild a catalog from scripts on a fresh instance (libraries, security, device users), restart afterwards, measure how long the sample set takes. Later consumers: `docs-admin-guide`, `tool-cli-catalog-commands`, `security-password-storage-design`.
- `store-parameterized-statements` — replace string concatenation with quote doubling by parameters for inserts and updates; measure.

## Cutting rules

Re-read the WS4 section and the store as `store-sqlite-concurrency` left it; one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`.
