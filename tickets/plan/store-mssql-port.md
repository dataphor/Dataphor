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

The umbrella's "where a server is configured" means the contract tests run against the ported store. `test-store-contract` landed that contract as an abstract fixture with one subclass per store, but with no SQL Server subclass - writing one is part of this work, from the wiring notes below. This ticket produces no store code itself.

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
3. **Contract fit.** Write a SQL Server subclass of `StoreContractTests` (see the wiring notes below) and run the contract's 14 tests against the built store on a local SQL Server. For each failure, decide: already a real defect inherited from 3.1 (the plan says the store has had little maintenance) → park it with the ticket. Behaviour the contract pins that SQL Server legitimately differs on (null ordering, index naming) → split the test or add a store hook; name it in the implement ticket.
4. **Solution membership.** Is the port carried in the main solution (`Dataphor.slnx`), or kept as an optional project behind a solution filter? The plan's "Done when" for WS4 names only that SQLite is the default and the contract is green; the MSSQL store does not have to be a mandatory build. A solution filter is fine, name it in the implement ticket.
5. **Test fixture logistics.** How the implement ticket drives the `CREATE DATABASE`/`DROP DATABASE` for the contract tests (connection string from environment variable; database name per fixture or per test). Record the measured per-test cost — `test-store-contract` leaves that open.

If the port turns out non-trivial on any of (1), (2) or (3), stop and file the park ticket instead. The park ticket:

- Moves `src/engine/Store.MSSQL/` to `legacy/Store.MSSQL/`, with no edits.
- Leaves `tests/DAE.Server.Tests/StoreContractTests.cs` alone: it has no SQL Server subclass to remove. Strike the wiring-notes section below instead, or leave it with a line saying the port was parked.
- Flips the plan's WS4 `store-mssql-port` row to a park note.
- Carries `tradeoffs:` naming the deferred port and the condition under which a user might ask for it back (a known deployment on SQL Server, cited by name internally — see the privacy rule).

Deliberately out of scope: the WS7 `device-mssql-port` work (port of the `MSSQLDevice` and its connection client). This plan asks only whether the **catalog store** piece of the SQL Server code ports trivially, in isolation from the device library.

## Expected output

One ticket into `implement/` or `backlog/` (as `debt-store-mssql-park`), chained with `prereq:` so later WS4 and WS7 tickets see the right dependency. `store-parameterized-statements` keeps its own `prereq: test-store-contract` either way.

## Fixture wiring worked out by `test-store-contract` (review stage, 2026-11-20)

`test-store-contract` landed the contract as an abstract fixture with one subclass per store, and left a SQL Server subclass in the file behind a compile switch nothing defined. The review deleted that subclass: nothing compiled it, so no compiler or test would have told anyone when it went stale, and it sat in a file whose stated job is the behaviour of the stores we actually build. Its wiring decisions are worth keeping, so they are recorded here for research item 5 above. None of it has ever been compiled or run - it is a starting point, not working code.

To add the subclass back, a `StoreContractTests` subclass needs only the two hooks `CreateStore(directory)` and `DisposeStore(store, directory)`, plus `ReopenStore()` which the base already provides. Read `tests/DAE.Server.Tests/StoreContractTests.cs` for the base's shape; the SQLite subclass at the bottom is eight lines.

- **One database per fixture, not per test.** `CREATE DATABASE` and `DROP DATABASE` in `OneTimeSetUp`/`OneTimeTearDown`, with `CreateStore` dropping every table (`select name from sys.tables`) so each test starts empty. Per-test creation was assumed too slow and never measured; measure it before taking the assumption on - for reference, the whole SQLite fixture of 11 tests runs in about 1.1 seconds, so there is room. The contract's own tables have no foreign keys, so the drop order does not matter.
- **A fixed database name, not a generated one**, so a run that crashes leaves one database behind to clean up rather than one per run. `OneTimeSetUp` then has to drop before it creates.
- **`alter database ... set single_user with rollback immediate` before the drop.** A pooled connection from an earlier run blocks `DROP DATABASE` otherwise.
- **Two connection strings.** The environment variable names a *server*; `CREATE DATABASE` runs connected to `master` and the store connects to the fixture's database. Swap the database with `DbConnectionStringBuilder`, removing `Database` and setting `Initial Catalog`, since either spelling may be in the supplied string.
- **`MSSQLStore.ShouldEnsureDatabase = false`** when the fixture has already created the database.
- **Gate on the environment variable with `Assert.Ignore`, not a compile switch.** NUnit then reports the fixture as skipped *with a reason naming the variable to set*, so a developer who wants to run it learns how from the test output; a compile switch makes it vanish silently. A `#if` is still unavoidable for the project reference itself until `DAE.MSSQL.csproj` is in `Dataphor.slnx` - which is the better argument for not having the class at all until then.
- **The restart round-trip the plan asked for is already on the base**, as `SeedSchemaReopens`, and runs on every subclass. No SQL Server-specific copy is needed; what is missing is simply that it has never executed against SQL Server.
## Edge cases & interactions

- **Encrypted-by-default under `Microsoft.Data.SqlClient`.** Known compatibility trap documented on `device-mssql-port`. The store itself may not be affected (user-provided connection string), but a developer running the contract tests locally will hit it. Mention in the implement ticket's test-setup notes.
- **The MSSQL client library.** `MSSQLStore.cs` routes through `Alphora.Dataphor.DAE.Connection.MSSQLConnection` from the `MSSQLDevice` library. That library's own port is `device-mssql-port`; if the store's port depends on it being on `Microsoft.Data.SqlClient` already, chain the implement ticket on `device-mssql-port` and say so. Checked once in planning: the `#define USESQLCONNECTION` arm uses `MSSQLConnection` only for the database-existence probe in `InternalInitialize`; the rest of the store uses `System.Data` abstractions through `SQLConnection` in the base.
- **No SQL Server subclass exists yet.** `test-store-contract` landed the base fixture and its SQLite subclass only; the restart-and-reopen round-trip the plan asked for is a base test (`SeedSchemaReopens`) and runs on any subclass added. Writing that subclass and getting the 14 tests green against SQL Server is this line of work, costed from the notes above.
- **Parameterized writes (`store-parameterized-statements`, landed 2026-11-20).** A store's generated insert, update and delete now carry their values as ADO.NET parameters, and `SQLStoreConnection.NativeToParameter` maps a native value to the parameter that carries it. Nothing in the base needs a SQL Server override: this store's catalog declares its boolean columns `tinyint` and the base sends the integer 1 or 0, which is what `MSSQLCommand` maps to `SqlDbType.TinyInt`. One thing to look at while running the contract: `MSSQLCommand.PrepareParameters` maps `SQLStringType` to `SqlDbType.VarChar`, not `NVarChar`, while every text column in this store's `SystemStoreCatalog.sql` is `nvarchar`. That mapping is not new and is shared with the catalog *read* path, which has always passed `SQLStringType(200)` against the same columns (`ServerCatalogDevice.TranslateScalarParameter`), so it is a pre-existing question rather than one this change introduced - but a write now goes through it too, so a non-ASCII library or object name is the thing to try. The new contract test `AwkwardStringRoundTripsThroughWrites` writes a string with quotes, a parenthesis and an embedded NUL, and is the one most likely to find it.
- **Pre-existing failures.** None in `tickets/.pre-existing-known.md` touch the MSSQL store.
