description: Make SQLite the server's built-in catalog database and remove every remaining reference to the retired SQL Server Compact database, so that a new server works with no settings and an old setting that names the retired database stops the server with a clear message instead of an obscure loading error.
prereq: build-server-project, test-harness-inprocess
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/Server/Server/Server.cs, src/engine/Server/Device.Catalog/ServerCatalogDevice.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Device.Catalog/CatalogException.cs, src/engine/DAE/Device.Catalog/CatalogException.resx, src/engine/DAE/Store/SQLStore.cs, src/engine/Store.MSSQL/Properties/AssemblyInfo.cs, src/windows/WindowsClient (the Windows client executable's project file), tests/DAE.Server.Tests/Utilities/SQLCEServerConfigurationManager.cs, tests/DAE.Server.Tests/Utilities/SQLiteServerConfigurationManager.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs, tests/DAE.Server.Tests/CoverageDeviceTest.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, tests/DAE.Server.Tests/SQLStoreTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, tests/DAE.Client.Tests/OutOfProcessTestFixture.cs, tests/DAE.Client.Tests/DAE.Client.Tests.csproj, docs/architecture.md, docs/migration-notes.md, AGENTS.md
difficulty: easy
----
# Remove SQL CE references; SQLite becomes the default catalog store

**Paths.** This ticket runs after `repo-move-to-new-layout`, so every path here is in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout) section. If the move has not happened yet, find the old location through that section's old → new table (for example `src/engine/Server` is today's `Dataphor/Server`, `src/engine/DAE` is `Dataphor/DAE`, `src/engine/Store.MSSQL` is `Dataphor/DAE.MSSQL`, `src/windows/WindowsClient` is `Dataphor/Frontend.Client.Windows.Executable`, `tests/` is `Tests/`).

## Goal

The server persists its own catalog (object scripts, users, rights, loaded libraries) in an embedded database, the *catalog store*. Until now the built-in default was SQL Server Compact 4.0 ("SQL CE"), which has no .NET 10 provider. Decision D3 settles that SQLite is the catalog store, SQL CE is deleted outright, and existing catalogs are rebuilt from scripts rather than converted. After this ticket:

- A server started with no store settings uses the SQLite store and keeps its catalog in a file under the instance's `Catalog` folder.
- An instance configuration that still names the SQL CE store stops the server at start with an error that says which setting to remove, instead of a type-load exception from deep inside the catalog device.
- No code, configuration, test or `docs/` file mentions SQL CE, apart from the plan, the migration notes and the new error itself.

## Scope

In scope: every SQL CE mention other than K19's code change, listed by file below; the default store class name and default connection string; the start-time check and its two error messages; the migration-notes entry.

Out of scope, with owners:

- K19's code change (`Connection.SQLCEConnection` registration in `libraries/Connection/Source/Register.cs`) and the `Connection` project's reference to the SQL CE project: done by the build ticket that converts the `Connection` project — `build-server-project` (`build-core-sdk-projects` handed Connection, Store.SQLite and the other server-dependent projects to it, because they use types from the server assembly). That ticket also flips the `Connection.SQLCEConnection` migration-notes row to done. Only verify both.
- Deleting the SQL CE store project and its native DLLs, and the committed catalog database under the old `Dataphoria/Default Server Configurations`: `repo-delete-dead-trees`. Only verify they are gone; delete any listed below that survived.
- Write-ahead logging, busy timeout and transaction behaviour of the SQLite store, including any options added to the default connection string: `store-sqlite-concurrency`.
- The old WiX installer (`Setup/Files.wxs`, `Setup/Files.wxs.bak` list SQL CE binaries): deleted by the installer work in `feat-ws9-installer-and-release`. Leave it.
- The parked top-level web prototypes (four project files under `legacy/` that reference the SQL CE project): parked by `library-park-side-projects`, excluded from the build. Leave them.
- `Documentation/` and `legacy/Documentation/`: converted and deleted by WS8 (`docs-convert-asciidoc`, `feat-ws8-documentation-remainder`). Leave them.
- `tools/net-spike/` (its K19 patch): owned by the build tickets; the spike is deleted once the real build exists.
- `docs/modernization-plan.md`: it records the decision; leave its SQL CE mentions.
- Rewriting the legacy test projects onto the new harness: `test-harness-inprocess`. This ticket only removes SQL CE from them.

## Settled decisions and known defects

- D3: SQLite is the catalog store; SQL CE is deleted; no catalog migration.
- D2: no .NET Framework build, so no conditional SQL CE support.
- K19 (base `Connection` library's hard reference to SQL CE) is fixed by the Connection project's conversion (`build-server-project`, see above); this ticket is the rest of the plan's `store-remove-sqlce-references` item.
- Sequencing: the server project (`src/engine/Server`) and the SQLite store project are first compiled on .NET 10 by `build-server-project`, and the NUnit harness comes from `test-harness-inprocess`; both are prereqs, so this ticket runs with them landed.
- The instance layout is a compatibility surface (`tickets/rules/10-dataphor.md`). D3 authorizes changing the catalog file; the change is recorded in the migration notes.

## Inventory of SQL CE mentions (verified against the tree, old paths in brackets)

Searched case-insensitively for `SQLCE`, `SqlCe`, `SqlServerCe`, `SQL CE`, `SSCE`, `Server Compact`, `.sdf` across tracked files, excluding `Legacy/`, `Documentation/` and `tickets/`.

Code (change):

- `src/engine/Server/Server/Server.cs` [`Dataphor/Server/Server/Server.cs`]: `GetCatalogStoreClassName()` returns `"Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE"` when the setting is empty (~line 149); `GetCatalogStoreConnectionString()` builds the SQL CE string `Data Source={file};Password=;Mode=Read Write` (~line 186); `GetCatalogStoreDatabaseFileName()` uses extension `.sdf` (~line 203); doc comments at ~lines 129, 144, 160–161, 181.
- `src/engine/DAE/Store/SQLStore.cs` [`Dataphor/DAE/Store/SQLStore.cs`]: the remarks on `DefaultMaxConnections` (~lines 103–118) are about SQL CE ("SSCE") connection limits. Rewrite the remark to state what the value is for; keep the value 60 (changing it is not in scope).
- `src/engine/Store.MSSQL/Properties/AssemblyInfo.cs` [`Dataphor/DAE.MSSQL/...`]: `AssemblyTitle` and `AssemblyDescription` say "SQL CE Store" for the SQL Server store (copy-paste). Correct the two strings to "SQL Server store"; do not touch the assembly name or any other attribute. If `build-version-and-signing` has already moved these attributes into the project file, correct them there.
- `src/windows/WindowsClient/` [`Dataphor/Frontend.Client.Windows.Executable/Frontend.Client.Windows.Executable.csproj` ~line 296]: a ClickOnce `PublishFile Include="System.Data.SqlServerCe"` prerequisite entry. Delete the entry (the project is still in legacy format until `ui-windows-client-port`; a text edit is enough).

Tests (still legacy projects, not compiled until `test-harness-inprocess`; verify by inspection):

- `tests/DAE.Server.Tests/Utilities/SQLCEServerConfigurationManager.cs`: empty subclass, meaning "the server's default store". Delete it and its `Compile` entry.
- `tests/DAE.Server.Tests/InProcessTestFixture.cs` (~line 38), `tests/DAE.Server.Tests/CoverageDeviceTest.cs` (~line 33), `tests/DAE.Client.Tests/OutOfProcessTestFixture.cs` (~line 39): use `SQLiteServerConfigurationManager` instead. That class is declared without `public` (`class SQLiteServerConfigurationManager`); make it `public` because `DAE.Client.Tests` uses it from another assembly.
- `tests/DAE.Server.Tests/CatalogStoreTest.cs`: delete `SQLCECatalogRegressionTest`; remove the `SQLCETiming` table and its join from the comment block at the top.
- `tests/DAE.Server.Tests/SQLStoreTest.cs`: delete `SQLCEReadAfterUpdateTest`, `SqlCeReadAfterUpdateTest`, `SQLCEStoreTest` (the last also contains a developer's drive path, K18) and the `using System.Data.SqlServerCe` / `using Alphora.Dataphor.DAE.Store.SQLCE` lines. Leave `MSSQLStoreTest` for `test-harness-inprocess`.
- `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`: the `System.Data.SqlServerCe` reference (~line 76), the `SQLCEServerConfigurationManager.cs` compile item (~line 91), the SQL CE project reference (~line 117) and twelve `amd64\sqlce*.dll` / `x86\sqlce*.dll` content items (~lines 156–190).
- `tests/DAE.Client.Tests/DAE.Client.Tests.csproj`: the SQL CE project reference (~line 107) and twelve `sqlce*.dll` content items (~lines 142–176).
- Binary files, if `repo-delete-dead-trees` left them: `tests/DAE.Server.Tests/{amd64,x86}/sqlce*.dll`, `tests/DAE.Client.Tests/{amd64,x86}/sqlce*.dll` (12 each), `System.Data.SqlServerCe.dll` (today `Dataphor/bin/`, `external/` after the move). Delete them.

Build files:

- The legacy `Dataphor.sln` (today `Dataphor/Dataphor.sln` ~line 43) lists the `DAE.SQLCE` project; remove the entry and its configuration lines if `repo-delete-dead-trees` left them. If the move deleted the legacy solution, nothing to do.
- `libraries/Connection/Source/DAE.Connection.csproj` project reference to the SQL CE project (~line 125 in the legacy file): replaced by `build-server-project`'s SDK-style project; verify it is gone.

Documentation:

- `docs/architecture.md`: diagram label `SQLCE│SQLite│MSSQL│PG` (~line 30); store assemblies row (~line 43); "updatable cursor where the back end has one (SQL CE)" (~line 58); "The default store is SQL Server Compact 4.0 … DAECatalog.sdf" (~line 60, rewrite for SQLite and the new file name); "native SQL CE DLLs" in the on-disk table (~line 96); the "Cannot survive .NET 10" row (~line 111) can keep SQL CE as history only if worded as removed — simplest is to drop it from that row.
- `AGENTS.md`: the layout table row listing `DAE.SQLCE` (~line 22; the move ticket rewrites this table — check whatever it left) and "SQL CE by default today; SQLite going forward" (~line 55) become "SQLite".
- `docs/migration-notes.md`: see "Migration notes" below.

Checked and needing no change:

- The instance editing form, `src/windows/Frontend.Client.Windows/Forms/EditInstanceForm.cs` (used by Dataphoria through `EditAliasForm` and by the configuration utility's `MainForm`), has free-text class name and connection string boxes and fills the class-name suggestions by scanning the executable's folder for `SQLStore` subclasses; with the SQL CE assembly gone it stops offering it. It has no SQL CE text. No change (it is WinForms and not built until WS6 anyway).
- `src/windows/ConfigurationUtility/ConfigForm.cs` has SQL CE-era "catalog store database name" and "password" boxes but no SQL CE text, and nothing in the tree constructs `ConfigForm`. Leave it to `ui-config-utility` (in `feat-ws6-windows-client-and-ide`).
- `ServerConfiguration.DefaultInstance` / `DefaultLocalInstance` (`src/engine/Server/Server/ServerConfiguration.cs`) set no store fields; the default comes only from `Server.GetCatalogStoreClassName()`.
- No `app.config` file names SQL CE or a store class. The only `ServerConfigurations.config` in the tree (old `Dataphor/Dataphoria/Default Server Configurations/`) has empty store settings and is deleted by `repo-delete-dead-trees`.

## Design

### New defaults (`Server.cs`)

- `GetCatalogStoreClassName()`: empty setting → `"Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite"` (the string the spike and `SQLiteServerConfigurationManager` already use; class and assembly names unchanged by D14). Declare it once as a `public const string DefaultCatalogStoreClassName` next to `DefaultCatalogDatabaseName` so tests and the check below use the same value.
- `GetCatalogStoreDatabaseFileName()`: `<instance>\Catalog\DAECatalog.sqlite` (`Path.ChangeExtension(DefaultCatalogDatabaseName, ".sqlite")`). A different extension from `.sdf` keeps an old SQL CE file and the new SQLite file from being confused.
- `GetCatalogStoreConnectionString()`: empty setting → `Data Source=<that file>`. Nothing else; `store-sqlite-concurrency` adds journal mode and busy timeout where it decides they belong.
- Rewrite the doc comments to describe SQLite.

Confirm that the SQLite store assembly is present beside the server assemblies wherever a server starts (the in-process harness, the runtime layout from `build-runtime-layout`). The store is loaded by name through `Type.GetType` in `CatalogStore.CreateStore`, so nothing references it at compile time; if a host or the test project does not reference the store project, its native SQLite library may not be copied. If it is missing, add the project reference in the host or test project rather than a copy step.

### Start-time checks (clear error, not silent fallback)

An instance configuration that names the SQL CE store must stop the server with an error naming the setting. A silent fallback to SQLite would start an empty catalog in an instance the operator believes is configured, and would hide a stale configuration file; the code makes an explicit error easy, so do that.

Add two codes to `CatalogException` (`src/engine/DAE/Device.Catalog/CatalogException.cs` and `.resx`; the 115 prefix is `DAE.Catalog`; 115107 and 115108 are unused — verified):

- 115107 `CatalogStoreClassNotFound`: "Catalog store class \"{0}\" could not be loaded. Check the catalog store class name setting (catalogstoreclassname) of this instance." Thrown from `CatalogStore.CreateStore` when `Type.GetType(_storeClassName, true, true)` throws, with the original exception as the inner exception. This covers typos and missing assemblies as well as SQL CE.
- 115108 `SQLCEStoreNotSupported`: "The SQL Server Compact catalog store is no longer supported. Remove the catalog store class name and connection string settings (catalogstoreclassname, catalogstoreconnectionstring) from this instance to use the default SQLite store, then rebuild the catalog from the application's scripts." Thrown at the start of `ServerCatalogDevice.InternalStart` (before the store is created) when either: the raw `Server.CatalogStoreClassName` setting, trimmed, names type `Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore` or assembly `Alphora.Dataphor.DAE.SQLCE` (case-insensitive, spaces around the comma ignored); or the class-name setting is empty and the raw `Server.CatalogStoreConnectionString` contains `.sdf` (case-insensitive) — a 3.1 configuration that set only the connection string would otherwise hand a SQL CE connection string to SQLite.

New codes are additions, not changes to existing codes; no existing message text changes. If the reference list of error codes exists under `docs/reference/` by the time this runs, add the two codes there.

## Edge cases & interactions

- Settings empty, fresh instance directory: server starts on SQLite and creates `Catalog\DAECatalog.sqlite`. Verified by test 1.
- Class name names the SQL CE store (any letter case, with or without spaces after the comma): start fails with 115108; no `Catalog\DAECatalog.sqlite` is created and no catalog work runs. Verified by test 2 (assert the file does not exist afterwards).
- Class name empty, connection string names a `.sdf` file: 115108. Verified by inspection of the condition (one branch of the same check; add it as a second case of test 2 only if it costs one line).
- Class name is some other unloadable string: 115107 with the inner exception. Verified by inspection (generic wrapper).
- The instance directory still holds a 3.1 `Catalog\DAECatalog.sdf` and settings are empty: the server starts with a new empty SQLite catalog beside it. This is the intended "rebuild from scripts" path, so it is not an error; write one line to the server log naming the ignored `.sdf` file so an operator who expected the old catalog sees why it is empty. Verified by inspection. Note for later work: the default instances directory, `%ProgramData%\Alphora\Dataphor\Instances`, is not version-specific (`InstanceManager.GetInstanceDirectory`, `Server.InstanceDirectory`), so a 4.0 server installed beside 3.1 shares 3.1's instance configuration file and instance folders. That is for `host-instance-configuration` in `feat-ws5-protocol-and-hosting`; mention it in the handoff, do not fix it here.
- Settings name the SQL Server store or a PostgreSQL store: unaffected by both checks.
- The server is used as a repository/engine (`IsEngine`): `ServerCatalogDevice.InternalStart` creates no store; place the SQL CE check inside the same `!IsEngine` branch so engine-only use is unaffected. Verified by inspection.
- Error raised during `Server.Start`: confirm the server ends in the stopped state and can be started again after the setting is corrected (test 2 restarts with corrected settings in the same instance directory).
- Formatting: `.cs` files keep tabs, CRLF and their byte-order mark; the `.resx` keeps its encoding and line endings.

## Done when

- `git grep -n -I -i -E "sqlce|sqlserverce|sql ce|ssce|server compact|\.sdf\b"` returns hits only in: `docs/modernization-plan.md`, `docs/migration-notes.md` (the entries below), `Legacy/`, `Documentation/`, `legacy/`, `Setup/`, `tickets/`, `tools/net-spike/` (if still present), and the new check and messages in `ServerCatalogDevice.cs` and `CatalogException.cs`/`.resx`. Paste the command's output, trimmed, into the handoff.
- The engine and server build (whatever `build-core-sdk-projects` and `build-server-project` established) with no new warnings.
- Tests (characterization of changed behaviour, as the project rule requires). `build-server-project` proves its in-process start with the spike's smoke program, not an NUnit project, and the NUnit harness arrives with `test-harness-inprocess`. Put the two tests in `tests/DAE.Server.Tests` on the harness fixture (`test-harness-inprocess` is a prereq). Each test starts an in-process `Server` with `InstanceDirectory` set to a fresh temporary directory and `LibraryDirectories` pointing at a copy of the libraries, never the repository or `%ProgramData%`:
  1. Default store: no store settings → `Start()` succeeds, `Evaluate("1 + 1")` works, `Catalog\DAECatalog.sqlite` exists.
  2. SQL CE setting rejected: class name `Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE` → `Start()` throws a `CatalogException` with code 115108 (find it on the exception or its inner chain, whichever way `Server.Start` wraps it) and no catalog file was created; then clear the setting and `Start()` succeeds in the same instance directory.
- `docs/migration-notes.md` updated (below).

## Migration notes

- Change the existing row "The embedded catalog store is SQLite; SQL Server Compact is gone …" to **done** and extend it: the catalog file is now `<instance>\Catalog\DAECatalog.sqlite`; an instance whose settings still name the SQL CE store, or a `.sdf` connection string, does not start and reports error 115108 naming the settings to remove; `Server.GetCatalogStoreDatabaseFileName()` (public, used by extensions only if they locate the catalog file) now returns the `.sqlite` path.
- Check that `build-server-project` marked the `Connection.SQLCEConnection` row done; if not, say so in the handoff rather than editing it here.

## TODO

- Read the plan's WS4 section and D3; confirm `repo-delete-dead-trees` and `build-server-project` removed the SQL CE project, native DLLs, and the K19 registration; note any leftovers.
- Server: add `DefaultCatalogStoreClassName`, switch the three defaults in `Server.cs` to SQLite, rewrite their doc comments.
- Add codes 115107 and 115108 to `CatalogException.cs` and `.resx`; wrap `Type.GetType` in `CatalogStore.CreateStore`; add the SQL CE check in `ServerCatalogDevice.InternalStart`; log the ignored `.sdf` file.
- Fix the `SQLStore.DefaultMaxConnections` remark and the SQL Server store's assembly title and description.
- Remove the ClickOnce `System.Data.SqlServerCe` entry from the Windows client project.
- Tests: delete the SQL CE configuration manager and SQL CE tests, switch the three fixtures to `SQLiteServerConfigurationManager` (made public), clean both legacy test project files, delete surviving SQL CE binaries.
- Remove the `DAE.SQLCE` entry from the legacy solution if it survived.
- Update `docs/architecture.md`, `AGENTS.md` and `docs/migration-notes.md`.
- Add the two tests; run them and the existing build/test command from `AGENTS.md`.
- Run the grep from "Done when" and put its output in the handoff, with the shared-instances-directory observation.

## Note from planning `test-harness-inprocess`

`test-server-fixture` (one of the three tickets `test-harness-inprocess` was split into) already converts `tests/DAE.Server.Tests` to an SDK-style project and deletes `Utilities/` (all four configuration managers, including `SQLCEServerConfigurationManager.cs` and `SQLiteServerConfigurationManager.cs`), `SQLStoreTest.cs`, `CoverageDeviceTest.cs`, the SQL CE and SQL Server variants of `CatalogStoreTest`, and every SQL CE reference and content item in `DAE.Server.Tests.csproj`. The test arms above for `tests/DAE.Server.Tests` are therefore done; check, and say so. `tests/DAE.Client.Tests` is untouched and still names `SQLCEServerConfigurationManager` from a class that no longer exists; it is a legacy project nobody builds, rebuilt by `test-client-server`, so remove only its SQL CE project reference, its `sqlce*.dll` content items and binaries, and leave its fixture to that ticket. Write the two new tests on `TestServerInstance` (`tests/DAE.Server.Tests/TestServerInstance.cs`), which sets the SQLite store explicitly: for the default-store test, construct the server without those two settings.
