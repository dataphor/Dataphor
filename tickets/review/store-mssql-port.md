description: Review the work that brought the SQL Server version of the catalog store back into the build and proved it works against a real SQL Server.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/Store.MSSQL/DAE.MSSQL.csproj, src/engine/Store.MSSQL/Properties/AssemblyInfo.cs, src/engine/Store.MSSQL/MSSQLStore.cs, Dataphor.slnx, tools/layout/Layout.csproj, tests/DAE.Server.Tests/StoreContractTests.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/modernization-plan.md, AGENTS.md
----
## What landed

The SQL Server catalog store builds, ships in the runtime layout, and passes the store contract. The planning pass's measurement held: the port was the project file alone, and no line of `MSSQLStore.cs`, `MSSQLStoreConnection.cs` or `MSSQLStoreCursor.cs` had to change to compile.

- **`src/engine/Store.MSSQL/DAE.MSSQL.csproj`** rewritten in SDK format, modelled on `src/engine/Store.SQLite/DAE.SQLite.csproj`: same `DefineConstants` block, project references to `Base`, `DAE` and `Server`, the three `Compile` items plus `Properties\AssemblyInfo.cs`, and `SystemStoreCatalog.sql` as an `EmbeddedResource`. No SQL-client package of its own - a comment in the file says why, and that the swap to `Microsoft.Data.SqlClient` stays with `device-mssql-port`.
- **`Properties/AssemblyInfo.cs`** trimmed to title, description, `ComVisible` and the existing `Guid`, keeping the licence header; the version, company, product, copyright, configuration and the key-file trio are gone, as in every other converted project.
- **`MSSQLStore.InternalInitialize`** now forces `MultipleActiveResultSets=True` into `ConnectionString` and sets `_supportsMARS` true, instead of reading the option and reporting whatever it found. The comment at the site says why, and points at `SQLiteStore.NormalizeConnectionString` as the precedent.
- **`Dataphor.slnx`** and the `Bin` list of **`tools/layout/Layout.csproj`** carry the project, next to `Store.SQLite` in both. Verified: `artifacts/layout/debug/bin/Alphora.Dataphor.DAE.MSSQL.dll` is there after a build.
- **`tests/DAE.Server.Tests`** references the project and gains `MSSQLStoreContractTests` at the bottom of `StoreContractTests.cs`. The stale "Not compiled against" comment above the two store references is replaced.
- **`docs/migration-notes.md`** has the entry under *Platform and deployment*; the plan's WS4 `store-mssql-port` row records what landed. **`AGENTS.md`** moves `Store.MSSQL` from "everything else" into the converted list - not in the ticket, but the sentence was made wrong by this change.

## Tests

One new fixture, no new test *cases*: `MSSQLStoreContractTests` is a second subclass of the existing `StoreContractTests`, so it runs the 14 cases that already describe the catalog store contract against a second store. That is the ticket's point and the project's testing rule for a re-hosted subsystem; adding store-specific cases beyond the contract was deliberately not done.

What it verifies, and the one thing worth a reviewer's eye: `TwoCursorsOpenOnOneTableInOneTransaction` is the case the forced multiple-active-result-sets option exists for. With the option absent it fails with *"There is already an open DataReader associated with this Command which must be closed first."* and the other 13 pass; that asymmetry was measured in planning, and the fixture no longer depends on the developer setting the option because the store sets it.

Deliberately **not** tested here: the `varchar` parameter mapping that loses non-ASCII text through this store's writes. It is pre-existing, shared with the SQL Server device, and recorded as an arm on `feat-ws7-devices-and-libraries`; the reproduction belongs with the fix.

## How to run it

```
$env:DATAPHOR_TEST_MSSQL = 'Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI'
dotnet test tests\DAE.Server.Tests\DAE.Server.Tests.csproj --no-build --filter 'FullyQualifiedName~MSSQLStoreContractTests' --logger 'console;verbosity=normal'
```

The variable names a **server**, not a database: the fixture puts its own database, `DataphorStoreContract`, into whatever is set. Start LocalDB first if it is not running (`sqllocaldb start MSSQLLocalDB`).

Results on the development machine, 2026-11-22, SQL Server 2016 LocalDB (`MSSQLLocalDB`, 13.1.4001.0):

- **14 of 14 passed**, 6.05 s for the fixture, slowest single test 331 ms (`AwkwardStringRoundTripsThroughWrites`).
- **Variable unset: 14 skipped**, each naming `DATAPHOR_TEST_MSSQL` and giving the example connection string, and nothing else in the suite affected.
- **Leftover database from a crashed run**: created `DataphorStoreContract` by hand, then ran the fixture - 14 passed, the `[OneTimeSetUp]` drop handled it.
- **Connection string that already sets the option to True**: 14 passed.
- Build: `dotnet build Dataphor.slnx` - 0 errors, and no warning attributed to `DAE.MSSQL` or to `StoreContractTests.cs`.
- Full suite in the three parts from the root `AGENTS.md`, with `DATAPHOR_TEST_MSSQL` set so the new fixture ran inside it: **part 1** 675 passed / 19 skipped / 0 failed (6 m 08 s), **part 2** 67 passed / 11 skipped (3 m 06 s), **part 3** 50 passed / 12 skipped (3 m 06 s). No pre-existing failures surfaced, so no `.pre-existing-error.md` was written.

## Decisions a reviewer may want to weigh

- **The fixture cleans up by dropping the database's tables after each test**, not by dropping and creating the database per test. The ticket's measurement is why: `create database` took 98-202 ms and the single-user-plus-drop pair 21 ms once and 3.0 s twice, so per-test databases would add 1.5-45 s to a 6 s fixture. It also cannot empty the database in `CreateStore`, because the base calls `CreateStore` a second time on the same directory for `SeedSchemaReopens`. The cost is that the 14 cases share one database, so a case that left state behind that is not a table would leak into the next; today none does.
- **`DropTables` drops every table in the database in one T-SQL batch**, relying on the contract's tables and the catalog seed having no foreign keys between them. A future contract case that adds a foreign key breaks the teardown, not the case - a reviewer may want that stated louder than the comment at the site does.
- **The fixture's helpers go through `MSSQLConnection`** (the store's own connection class), not `SqlConnection` directly. Written with `SqlConnection` first, which compiled but added 8 `CS0618` obsolete-type warnings - `System.Data.SqlClient` is obsolete on .NET 10 and the repository does not suppress those warnings. Going through `MSSQLConnection` leaves the file warning-free and means the fixture follows `device-mssql-port`'s client swap for free.
- **Forcing the option writes over a configured `False`.** Intentional, and the ticket's call: a deployment that set it to `False` was broken. The migration note tells a deployment to drop the setting. A reviewer who disagrees would have to argue the store should instead fail to start, which is a louder break for the same outcome.
- **`_shouldEnsureDatabase` is left at its default**, so `InternalInitialize` runs its own `create database` probe against `master` and the fixture only ever drops. The probe swallows its own exceptions by design (pre-existing), which means a genuinely unreachable server shows up as a later, less pointed failure - unchanged by this ticket, worth knowing if the fixture ever fails obscurely in someone's environment.

## Known gaps, honestly

- **Proved on SQL Server 2016 LocalDB, which is below the 2022 minimum the plan sets (Q12).** A weaker proof, not a wrong one. `device-sqlserver-version-matrix` owns the version question; this ticket deliberately did not go looking for a newer server.
- **Non-ASCII text through this store's writes is lost** (`varchar` parameters against `nvarchar` columns, in `MSSQLCommand.PrepareParameters` and `GetParamTypeDescriptor`). Pre-existing, measured in planning, shared with the SQL Server device, recorded as an arm on `feat-ws7-devices-and-libraries`, and called out in the migration note as a reason SQLite stays the recommendation. Not fixed here.
- **One parameter type per distinct string length**, so SQL Server caches a plan per length. Conditional and not this store's to decide; the same arm records it.
- **No CI coverage.** The fixture self-skips where `DATAPHOR_TEST_MSSQL` is unset, so CI is unchanged; supplying LocalDB in CI belongs to `test-device-live`.
- **Nothing drives this store through a running server.** The contract runs below the catalog, as it is designed to; a server actually booting on a SQL Server catalog is not proved by this ticket.
