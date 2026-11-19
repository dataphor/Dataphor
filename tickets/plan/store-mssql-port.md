description: Decide whether the SQL Server catalog store is ported to the modern SQL client cheaply enough to keep, or parked under legacy; produce the follow-up implement or park ticket.
architecture: docs/modernization-plan.md#ws4-catalog-store
prereq: test-store-contract
files: src/engine/Store.MSSQL/DAE.MSSQL.csproj, src/engine/Store.MSSQL/MSSQLStore.cs, src/engine/Store.MSSQL/MSSQLStoreConnection.cs, src/engine/Store.MSSQL/MSSQLStoreCursor.cs, Dataphor.slnx, tests/DAE.Server.Tests/StoreContractTests.cs
difficulty: medium
----
## What this is

The plan's WS4 row for `store-mssql-port` says: "Port the SQL Server catalog store to `Microsoft.Data.SqlClient` if trivial, else park it under `legacy/`; run the store contract tests where a server is configured." The decision — trivial or not — needs the code actually looked at and tried once on .NET 10 before an implement ticket is useful. This plan ticket does the look, settles the call, and produces one next ticket:

- **Implement ticket** if the port is a project-file change (SDK-style csproj, `netstandard2.0` or `net10.0` TFM, `Microsoft.Data.SqlClient` package reference) and a one- or two-line code change (the `System.Data.SqlClient` `using`, maybe the connection-string encrypt defaults — the known compatibility trap in the plan's WS7 row for `device-mssql-port`).
- **Park ticket** (`debt-store-mssql-park`) if the port touches logic, needs a test infrastructure beyond what `test-store-contract` lands, or the SQL Server tooling differs in a way that would fight the contract tests.

The umbrella's "where a server is configured" and `test-store-contract`'s `MSSQLStoreContractTests` subclass make the contract run against the ported store. This ticket produces no store code itself.

## Starting state (as of plan time)

- `src/engine/Store.MSSQL/` holds `MSSQLStore.cs`, `MSSQLStoreConnection.cs`, `MSSQLStoreCursor.cs`, `SystemStoreCatalog.sql` and `DAE.MSSQL.csproj`.
- `DAE.MSSQL.csproj` is still the legacy VS2003 project (`TargetFrameworkVersion v4.5.2`, ToolsVersion 12.0, bootstrapper package metadata) and is **not** in `Dataphor.slnx` (the solution lists only `Store.SQLite`). So "does it compile on .NET 10" has not been checked since the reorganization.
- `MSSQLStore.cs` currently `using System.Data.SqlClient;` at the top, inside a `#define USESQLCONNECTION` block that reroutes most calls through `Alphora.Dataphor.DAE.Connection.MSSQLConnection` (`libraries/MSSQLDevice/Source/MSSQLConnection.cs`). The `Connection` library's SQL Server client has its own port (`device-mssql-port`, a WS7 ticket); this store's port must share that client's choice of SQL-client package.
- The `SystemStoreCatalog.sql` is the SQL Server flavour of the catalog seed.
- `MSSQLStore.InternalInitialize` sets `_supportsUpdatableCursor = false` — same as the SQLite store.

## Research the plan ticket does

Each question below is settled in the plan ticket's output (the next-stage implement or park ticket), not left open:

1. **Compile on .NET 10.** Convert `DAE.MSSQL.csproj` to SDK-style (`Microsoft.NET.Sdk`, `net10.0`), reference `Microsoft.Data.SqlClient` instead of `System.Data.SqlClient`, add the project to `Dataphor.slnx`, and `dotnet build Dataphor.slnx`. Record what builds, what warns and what errors. A clean build with `using System.Data.SqlClient` replaced by `Microsoft.Data.SqlClient` is "trivial".
2. **Compatibility defaults.** `Microsoft.Data.SqlClient` encrypts by default and validates server certificates; the connection-string arrives from `Server.CatalogStoreConnectionString` (user configuration), so the store does not synthesize it. Confirm. If the store does build any part of its own connection string (`MSSQLStore.InternalInitialize` rewrites `Initial Catalog` → `master` for the `CREATE DATABASE` check), note any default the new client changes that a 3.1 configuration would not expect.
3. **Contract fit.** With the built store wired to a local SQL Server, run `StoreContractTests.MSSQLStoreContractTests` from `test-store-contract` against it (tests author left the fixture inconclusive-skipping unless `DAE_MSSQL_CONNECTION` is set). For each failure, decide: already a real defect inherited from 3.1 (the plan says the store has had little maintenance) → park it with the ticket. Behaviour the contract pins that SQL Server legitimately differs on (null ordering, index naming) → split the test or add a store hook; name it in the implement ticket.
4. **Solution membership.** Is the port carried in the main solution (`Dataphor.slnx`), or kept as an optional project behind a solution filter? The plan's "Done when" for WS4 names only that SQLite is the default and the contract is green; the MSSQL store does not have to be a mandatory build. A solution filter is fine, name it in the implement ticket.
5. **Test fixture logistics.** How the implement ticket drives the `CREATE DATABASE`/`DROP DATABASE` for the contract tests (connection string from environment variable; database name per fixture or per test). Record the measured per-test cost — `test-store-contract` leaves that open.

If the port turns out non-trivial on any of (1), (2) or (3), stop and file the park ticket instead. The park ticket:

- Moves `src/engine/Store.MSSQL/` to `legacy/Store.MSSQL/`, with no edits.
- Deletes the `MSSQLStoreContractTests` subclass from `test-store-contract` (or marks it `[Ignore]` with a one-liner pointing at the park ticket — pick the cleaner option at park time).
- Flips the plan's WS4 `store-mssql-port` row to a park note.
- Carries `tradeoffs:` naming the deferred port and the condition under which a user might ask for it back (a known deployment on SQL Server, cited by name internally — see the privacy rule).

Deliberately out of scope: the WS7 `device-mssql-port` work (port of the `MSSQLDevice` and its connection client). This plan asks only whether the **catalog store** piece of the SQL Server code ports trivially, in isolation from the device library.

## Expected output

One ticket into `implement/` or `backlog/` (as `debt-store-mssql-park`), chained with `prereq:` so later WS4 and WS7 tickets see the right dependency. `store-parameterized-statements` keeps its own `prereq: test-store-contract` either way.

## Edge cases & interactions

- **Encrypted-by-default under `Microsoft.Data.SqlClient`.** Known compatibility trap documented on `device-mssql-port`. The store itself may not be affected (user-provided connection string), but a developer running the contract tests locally will hit it. Mention in the implement ticket's test-setup notes.
- **The MSSQL client library.** `MSSQLStore.cs` routes through `Alphora.Dataphor.DAE.Connection.MSSQLConnection` from the `MSSQLDevice` library. That library's own port is `device-mssql-port`; if the store's port depends on it being on `Microsoft.Data.SqlClient` already, chain the implement ticket on `device-mssql-port` and say so. Checked once in planning: the `#define USESQLCONNECTION` arm uses `MSSQLConnection` only for the database-existence probe in `InternalInitialize`; the rest of the store uses `System.Data` abstractions through `SQLConnection` in the base.
- **Contract tests landing empty.** `test-store-contract` lands the MSSQLStore subclass with one behaviour test (restart-and-reopen) but leaves the broader suite inconclusive-skipping. The implement ticket turns that on, not this one.
- **Pre-existing failures.** None in `tickets/.pre-existing-known.md` touch the MSSQL store.
