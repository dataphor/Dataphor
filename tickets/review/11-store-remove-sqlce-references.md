description: Review the change that makes SQLite the server's built-in catalog database, stops a server whose settings still name the retired SQL Server Compact database with a clear error, and removes the remaining references to that database.
prereq: build-server-project, test-harness-inprocess
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/Server/Server/Server.cs, src/engine/Server/Device.Catalog/ServerCatalogDevice.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Device.Catalog/CatalogException.cs, src/engine/DAE/Device.Catalog/CatalogException.resx, src/engine/DAE/Store/SQLStore.cs, src/engine/Store.MSSQL/Properties/AssemblyInfo.cs, src/windows/WindowsClient/Frontend.Client.Windows.Executable.csproj, tests/DAE.Client.Tests/DAE.Client.Tests.csproj, tests/DAE.Server.Tests/CatalogStoreTest.cs, tests/DAE.Server.Tests/TestServerInstance.cs, docs/architecture.md, docs/migration-notes.md, AGENTS.md
----
# Review: SQL CE references removed; SQLite is the default catalog store

The server keeps its own catalog (object scripts, users, rights, loaded libraries) in an embedded database, the *catalog store*. The 3.1 default was SQL Server Compact 4.0 ("SQL CE"), which has no .NET 10 provider; plan decision D3 makes SQLite the store and deletes SQL CE without catalog conversion.

## What changed

- `Server.cs`: new `public const string DefaultCatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite"`; `GetCatalogStoreClassName()` returns it when the setting is empty; `GetCatalogStoreDatabaseFileName()` returns `<instance>\Catalog\DAECatalog.sqlite`; the default connection string is `Data Source=<that file>` and nothing else (journal mode and busy timeout are `store-sqlite-concurrency`'s). Doc comments rewritten.
- `CatalogException`: two new codes. 115107 `CatalogStoreClassNotFound` (thrown by `CatalogStore.CreateStore` when `Type.GetType` fails, with the original exception as inner). 115108 `SQLCEStoreNotSupported`, thrown by `ServerCatalogDevice.InternalStart` (inside the `!IsEngine` branch, before any store is created) when the raw class-name setting's type part is `Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore` or its assembly part is `Alphora.Dataphor.DAE.SQLCE` (case-insensitive, spaces around the comma ignored), or the class name is empty and the connection string contains `.sdf`. No existing code or message changed. `docs/reference/error-codes.md` lists only prefixes, so nothing was added there.
- `ServerCatalogDevice.LogIgnoredSQLCECatalog`: with the default store, a `Catalog\DAECatalog.sdf` left in the instance folder is reported as a warning in the server log; start continues on a new SQLite catalog (the intended rebuild-from-scripts path).
- `SQLStore.DefaultMaxConnections` remark rewritten without SQL CE; value 60 unchanged. The SQL Server store's `AssemblyTitle`/`AssemblyDescription` now say "SQL Server store".
- Windows client project: the ClickOnce `PublishFile System.Data.SqlServerCe` entry removed. `tests/DAE.Client.Tests/DAE.Client.Tests.csproj`: SQL CE project reference and its 12 `sqlce*.dll` content items (and their now-empty `ItemGroup`) removed.
- `TestServerInstance.CatalogStoreClassName` now refers to `Server.DefaultCatalogStoreClassName` instead of repeating the string.
- `docs/architecture.md` (diagram, cursor remark, default-store paragraph, on-disk table, ".NET 10 cannot survive" row), `AGENTS.md` (catalog store rule), `docs/migration-notes.md` (the SQLite row flipped to **done** and extended with the file name, 115108/115107, and the two public members).

## Verified as already done by other tickets

- No SQL CE project, native DLL, `System.Data.SqlServerCe.dll` or `.sdf` file is tracked (`git ls-files | grep -i -E "sqlce|sqlserverce|\.sdf$"` is empty); the legacy `Dataphor.sln` has no `DAE.SQLCE` entry.
- K19: `libraries/Connection/Source/DAE.Connection.csproj` no longer references the SQL CE project (only a history comment remains), and the `Connection.SQLCEConnection` migration-notes row is already **done**.
- `tests/DAE.Server.Tests`: `test-server-fixture` already deleted `Utilities/`, `SQLStoreTest.cs`, `CoverageDeviceTest.cs`, the SQL CE catalog test and every SQL CE item in the project file. `InProcessTestFixture.cs` no longer names a configuration manager.
- The SQLite store assembly reaches the test output through the existing project reference in `DAE.Server.Tests.csproj`; the default-store test proves it loads by name.

## Tests added (`tests/DAE.Server.Tests/CatalogStoreTest.cs`)

- `DefaultStoreIsSQLite`: a server with both store settings cleared starts, evaluates `1 + 1` = 2, and creates `Instance\Catalog\DAECatalog.sqlite`. Pins the new default.
- `SQLCESettingsAreRejected` (3 cases: exact SQL CE class name; lower-case type with a space after the comma; empty class name with a 3.1-style `.sdf` connection string): `Start()` throws with `CatalogException` 115108 somewhere in the inner-exception chain, the server is `Stopped`, no `.sqlite` file exists; then with the settings cleared the same instance starts and creates the file. Pins the check, both branches, and restart after correction.

Not covered by a test (by inspection only): the 115107 wrapper for an arbitrary unloadable class name, the `.sdf` log warning, and that engine-only use (`IsEngine`) skips the check.

## Validation run

- `dotnet build Dataphor.slnx`: 0 errors; no warnings in the changed files (`Server.csproj` still shows its 73 existing warnings, CA1416 and similar).
- `dotnet test Dataphor.slnx --no-build`: DAE.Server.Tests Failed 0, Passed 105, Skipped 44 (the manifest's known/unstable coverage cases).

## Grep from "Done when" (trimmed)

`git grep -n -I -i -E "sqlce|sqlserverce|sql ce|ssce|server compact|\.sdf\b" -- ':!docs/modernization-plan.md' ':!Legacy/' ':!legacy/' ':!Documentation/' ':!Setup/' ':!tickets/'`

```
AGENTS.md:11: Settled direction ... SQLite catalog store, SQL CE deleted, catalogs rebuilt from scripts ...
docs/architecture.md:61: The default store is **SQLite** ... 3.1 SQL Server Compact store, or a `.sdf` connection string, does not start (error 115108 ...)
docs/migration-notes.md:13: | done | The embedded catalog store is SQLite; SQL Server Compact is gone ...
docs/migration-notes.md:26: | done | The D4 class `Connection.SQLCEConnection` no longer exists ...
libraries/Connection/Source/DAE.Connection.csproj:15: <!-- 3.1 also referenced the SQL CE store project ... SQL CE is gone (K19). -->
src/engine/DAE/Device.Catalog/CatalogException.cs:41-42: 115108 SQLCEStoreNotSupported
src/engine/DAE/Device.Catalog/CatalogException.resx:145: 115108 message
src/engine/Server/Device.Catalog/ServerCatalogDevice.cs:72-123: the check, its constants and the log message
tests/DAE.Client.Tests/OutOfProcessTestFixture.cs:39: FConfigurationManager = new SQLCEServerConfigurationManager();
tests/DAE.Server.Tests/CatalogStoreTest.cs:50-62: the new rejection test cases
```

Hits outside the ticket's allowed list, and why they stay:

- `AGENTS.md:11` and `docs/architecture.md:61`: state the decision / describe the new error; they do not present SQL CE as current.
- `libraries/Connection/Source/DAE.Connection.csproj:15`: history comment written by `build-server-project`, which owns that file.
- `tests/DAE.Client.Tests/OutOfProcessTestFixture.cs:39`: per the planning note, this legacy project is unbuilt and its fixture is rebuilt by `test-client-server`; the class it names no longer exists anywhere, so it cannot be switched to `SQLiteServerConfigurationManager` (also deleted). Left for that ticket.
- `tests/DAE.Server.Tests/CatalogStoreTest.cs`: the test of the new check has to name the SQL CE settings.

## For the reviewer / later work

- The default instances directory `%ProgramData%\Alphora\Dataphor\Instances` is not version-specific (`InstanceManager.GetInstanceDirectory`, `Server.InstanceDirectory`), so a 4.0 server installed beside 3.1 shares 3.1's instance configuration file and instance folders. A 3.1 instance with empty store settings will start on 4.0 with an empty SQLite catalog beside the old `.sdf` (logged as a warning). That belongs to `host-instance-configuration` in `feat-ws5-protocol-and-hosting`; not addressed here.
- The SQL CE check reads the raw settings (`CatalogStoreClassName`, `CatalogStoreConnectionString`), not the `Get…` defaults, so an explicitly set connection string with the default class name is checked for `.sdf` only when the class name is empty. A class name set to the SQLite store with a `.sdf` connection string is not rejected (SQLite would create a SQLite database with that name); judged out of scope.
- Whitespace-only class name: not caught as empty by the check; `GetCatalogStoreClassName()` passes it through and `Type.GetType` fails, so it reports 115107. Matches the existing `IsNullOrEmpty` semantics.
- `src/windows/ConfigurationUtility/ConfigForm.cs` still has SQL CE-era "database name"/"password" boxes (no SQL CE text); left to `ui-config-utility` as the ticket said.
