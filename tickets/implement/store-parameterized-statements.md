description: Replace the catalog store's hand-written SQL literals with parameters for inserts, updates and deletes, keeping behaviour identical and measuring the effect against the baseline.
architecture: docs/modernization-plan.md#ws4-catalog-store
prereq: test-store-contract
files: src/engine/DAE/Store/SQLStoreConnection.cs, src/engine/DAE/Store/SQLStoreCursor.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/Store.SQLite/SQLiteStoreConnection.cs, src/engine/Store.MSSQL/MSSQLStoreConnection.cs, tests/DAE.Server.Tests/StoreContractTests.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
difficulty: medium
----
## What this is

The catalog store builds its insert, update and delete statements by string-concatenating literal values with a hand-rolled quote-doubling (`SQLStoreConnection.NativeToLiteralValue`). The TODO at `src/engine/DAE/Store/SQLStoreConnection.cs:391` has been there since 3.1 and the note just above it (`src/engine/DAE/Store/SQLStoreConnection.cs:377-379`) already flags the invariant-culture problem waiting to happen the day a `double` or `date` column is added. Replace the literals with ADO.NET parameters, keep every externally visible behaviour (including the three undo-log statements per write and the key conditions, and the `is null` rendering), and measure against the baseline `store-rebuild-workflow` prints.

Reaches only the statement path (`PerformInsert`, `PerformUpdate`, `PerformDelete` and their `Generate*Statement` helpers). The cursor path (`SQLStoreCursor.Insert`/`Update`/`Delete`) writes through its own update/insert/delete statements built the same way — parameterize those too; they share `GenerateInsertStatement`/`GenerateUpdateStatement`/`GenerateDeleteStatement` already.

Delete `NativeToLiteralValue` once nothing calls it (confirm by grep in the handoff). The culture `NOTE:` above it goes with it.

## Shape

### New primitive

`SQLStoreConnection`:

- Private helper `IDbCommand BuildParameterizedStatement(string sql, IReadOnlyList<(string name, object value)> parameters)`. Each store's own `IDbCommand` creation path (already there for `ExecuteStatement`) adds the parameters. Parameter names are positional (`@p0`, `@p1`, … on SQL Server; `$p0`, `$p1`, … on SQLite — pick the form each store's provider accepts; justify the choice).
- Overload `ExecuteStatement(string sql, params (string name, object value)[] parameters)` that runs `BuildParameterizedStatement` and executes non-query. The existing no-parameter overload stays and continues to serve `ExecuteScalar` and seeding.

### Generators

`GenerateInsertStatement`, `GenerateUpdateStatement`, `GenerateDeleteStatement` and `AppendKeyCondition` return a tuple `(string sql, (string name, object value)[] parameters)` or write to a shared builder, picked at implement time. The `is null` branch in `AppendKeyCondition` stays as a literal `is null` (ADO.NET's `DBNull` through a parameter gives `= @p` which does not match `null` in standard SQL); keep the branch — this is a real place where a literal is correct and a parameter would silently break.

### Callers

- `PerformInsert`, `PerformUpdate`, `PerformDelete` call the new parameterized path.
- `LogInsert`, `LogUpdate`, `LogDelete` still generate statement text (undo log entries are replayed as text, not re-parameterized — the log is private to one connection on one database flavour, and the inverse SQL is only built once per nested write). Keep them as they are, using the same generator overload that returns the literal-interpolated SQL; i.e. the generator has two shapes, parameterized and literal, built on one tree. Or: build the parameterized SQL and substitute the values back as literals for the log, using `NativeToLiteralValue` — simpler, and lets the invariant-culture note apply to one site. Pick in the implement ticket and justify.
- `SQLStoreCursor.Insert`/`Update`/`Delete` call the same parameterized path.

### Per-store concerns

- **SQLite** (`System.Data.SQLite`): supports named `$p` and `@p` parameters; `DBNull.Value` round-trips through text columns. Tests from `test-store-contract` cover the catalog's shapes.
- **SQL Server** (`System.Data.SqlClient` or `Microsoft.Data.SqlClient` — pending `store-mssql-port`): named `@p` parameters. The parameter builder lives on the base class; the per-store override is only `IDbCommand.CreateParameter`. If `store-mssql-port` parks the store, the base's behaviour alone matters.

## Measurement

Run `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` (from `store-rebuild-workflow`) before and after; report the two timings in the handoff. Expectation: parameterized inserts and updates are at worst the same speed, and up to 20–40% faster on batched writes because of SQLite's statement cache. Do not gate on a number — just record it.

Secondary measurement: the three-part suite's total wall time before and after. Report both numbers.

## TODO

- Add the parameterized overload on `SQLStoreConnection`; delete `NativeToLiteralValue` once nothing calls it (or move its body into the single logging path).
- Parameterize `GenerateInsertStatement`, `GenerateUpdateStatement`, `GenerateDeleteStatement` and their callers.
- Confirm by grep that no live caller passes a `double`, `date` or `decimal` through the statement path today; if one has snuck in since `bug-store-nested-rollback-keeps-rows`, name it.
- Add one `StoreContractTests` test that writes a row whose string column contains `';'`, `');'` and a NUL, round-trips it through update and delete and reads it back. That test is for this ticket (it is the one behaviour that quote-doubling could get wrong today and parameterization fixes); it lives on the contract so every store proves it.
- Also add the umbrella's open case the previous tickets left: an `Update()` that moves a cursor's row out of its current range ends the scan on `Next()`. One test, on `StoreContractTests`. Reproduction: open a cursor on an index with a range that admits only the current row, update a key column to a value outside the range, call `Next()` and assert false. This test is independent of the parameterization change — same ticket because this is the first place it will be verified against every store.
- Run the three-part suite named in the root `AGENTS.md`. Report timings against the baseline from `store-rebuild-workflow`.

## Edge cases & interactions

- **`is null` vs `= @p`.** Covered above. One line NOTE at `AppendKeyCondition` saying so.
- **Boolean mapping.** `NativeToLiteralValue` maps `bool` to `1`/`0`; the catalog's tables declare boolean columns as `tinyint` / `integer`. Pass `1`/`0` through the parameter too — do not pass `bool`, which an ADO.NET provider will map to its own native boolean and some providers may then reject against an integer column. Same mapping, parameterized form; the catalog tables stay unchanged. Pin as a test if one of the contract tests does not already exercise it (reviewer's call).
- **Statement text in exceptions.** `SQLStoreConnection.ExecuteStatement` wraps provider errors into a `StoreException` that carries the statement text. With parameterization, substitute parameter placeholders with their rendered literal value (using the old `NativeToLiteralValue` logic kept private to the error path, not for the on-wire statement) before raising, so a failed insert's error message still names the row. Verify the three catalog error codes from `CatalogException` still fire with legible bodies.
- **Culture / invariant formatting.** The existing `NOTE:` says `ToString()` is exact only because the catalog's columns are integer or text. The parameterized path removes the question entirely for numeric columns (`int`/`long` cross into `IDbDataParameter.Value` without a ToString). Delete the `NOTE:` with `NativeToLiteralValue`.
- **The undo log.** The log stores inverse SQL as text and replays it with `ExecuteStatement(string)`. Do not parameterize those entries — they are replayed once, locally, and parameterizing them adds complexity to the rollback path. Keep the literal shape there; name the exception to the "parameters everywhere" rule in the handoff.
- **Interaction with `store-mssql-port`.** The parameterized path on the base assumes a provider that accepts named parameters. Both the SQLite provider and both SQL Server providers (old and new) accept `@name`. Nothing in this change depends on which SQL Server package the store uses.
- **Pre-existing failures.** `tickets/.pre-existing-known.md` names three; none touches this change.

## Verification

- `StoreContractTests.*` green on SQLite, including the new "quotes, parens and NUL round-trip" and "update that moves a row out of the range" cases.
- `CatalogStoreTest.*` green (catalog-level behaviour unchanged).
- `dotnet build Dataphor.slnx` 0 errors; three-part suite from root `AGENTS.md` 0 failures.
- Handoff reports before/after timings from `StoreRebuildWorkflowTest` and the three-part suite.
