description: SQLite is now the server's built-in catalog database, a server whose settings still name the retired SQL Server Compact database stops with a clear error, and the remaining references to SQL Server Compact are gone.
prereq: build-server-project, test-harness-inprocess
architecture: docs/modernization-plan.md#ws4-catalog-store
files: src/engine/Server/Server/Server.cs, src/engine/Server/Device.Catalog/ServerCatalogDevice.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, src/engine/DAE/Device.Catalog/CatalogException.cs, src/engine/DAE/Device.Catalog/CatalogException.resx, src/engine/DAE/Store/SQLStore.cs, src/engine/Store.MSSQL/Properties/AssemblyInfo.cs, src/windows/WindowsClient/Frontend.Client.Windows.Executable.csproj, tests/DAE.Client.Tests/DAE.Client.Tests.csproj, tests/DAE.Server.Tests/CatalogStoreTest.cs, tests/DAE.Server.Tests/TestServerInstance.cs, tests/DAE.Server.Tests/ConcurrentDDLTest.cs, docs/architecture.md, docs/migration-notes.md, docs/modernization-plan.md, AGENTS.md
----
# SQL CE references removed; SQLite is the default catalog store

The server keeps its own catalog (object scripts, users, rights, loaded libraries) in an embedded database, the *catalog store*. The 3.1 default was SQL Server Compact 4.0 ("SQL CE"), which has no .NET 10 provider; plan decision D3 makes SQLite the store and deletes SQL CE without catalog conversion.

## What landed

Implemented in `ticket(implement): store-remove-sqlce-references`:

- `Server.DefaultCatalogStoreClassName` (the SQLite store) is returned by `GetCatalogStoreClassName()` when the setting is empty; the default catalog file is `<instance>\Catalog\DAECatalog.sqlite` and the default connection string is `Data Source=<that file>`.
- New error codes in `CatalogException`: 115107 `CatalogStoreClassNotFound` (raised by `CatalogStore.CreateStore` when the store class cannot be loaded, with the original exception inside) and 115108 `SQLCEStoreNotSupported` (raised by `ServerCatalogDevice.InternalStart`, full server only, before any store is created, when the class-name setting names the SQL CE store type or assembly, or the class name is empty and the connection string names a `.sdf` file).
- With the default store, a `DAECatalog.sdf` left in the catalog folder is reported as a warning in the server log and start continues on a new SQLite catalog.
- SQL CE text removed from `SQLStore.DefaultMaxConnections`, the SQL Server store's assembly title, the Windows client's ClickOnce publish list, and `DAE.Client.Tests.csproj` (project reference and 12 native DLL items).
- Documentation: `docs/architecture.md`, `AGENTS.md`, `docs/migration-notes.md` (row flipped to **done**), and, in review, the plan's WS4 row marked **Done**.

Left to other tickets: `src/windows/ConfigurationUtility/ConfigForm.cs` SQL CE-era database name/password boxes (`ui-config-utility`); `tests/DAE.Client.Tests/OutOfProcessTestFixture.cs` naming a deleted SQL CE configuration manager (`test-client-server`, which rebuilds that unbuilt fixture); side-by-side 3.1/4.0 installs sharing the instance folder (K27, `host-instance-configuration`).

## Tests

`tests/DAE.Server.Tests/CatalogStoreTest.cs`:

- `DefaultStoreIsSQLite`: both store settings cleared, the server starts, evaluates D4, and creates `DAECatalog.sqlite`.
- `SQLCESettingsAreRejected` (3 cases): SQL CE class name, case/spacing variant, and empty class name with a 3.1 `.sdf` connection string each stop the start with 115108, leave the server stopped and create no catalog file; clearing the settings then lets the same instance start.
- `UnloadableStoreClassIsReported` (added in review): an unknown store class reports 115107 and leaves the server stopped.

## Review findings

Checked by reading the implement diff, the files it touched, the callers of the catalog store settings (`ServerConfiguration.ApplyTo`, `EditInstanceForm`, `ConcurrentDDLTest`, the net-spike smoke program), the runtime layout (`tools/layout/Layout.csproj` ships `DAE.SQLite` in `bin/`, so loading the default store by name works outside the tests), and by building and running the suite.

- **Correctness / error handling:** the SQL CE check runs before any store is created and only in the full server (`!IsEngine`); a failed start leaves the server `Stopped` and a corrected restart works (pinned by test). The 115107 wrapper catches every exception from `Type.GetType` (type-load, file-not-found, bad-image, argument), which is the intended set. No defect found.
- **Edge cases considered, no change:** the `.sdf` test is a case-insensitive substring match, so a connection string that merely contains `.sdf` elsewhere would also be rejected when no class name is set; with no class name set, 3.1 treated every explicit connection string as SQL CE anyway, so this is not worth a tripwire. A SQLite class name with a `.sdf` connection string and a whitespace-only class name were already weighed in the implement handoff; agreed, out of scope.
- **Compatibility:** no existing error code, message, class name, assembly name or file format changed; two new codes added. `GetCatalogStoreDatabaseFileName()` now returns a `.sqlite` path and is recorded in migration notes, as is the new constant.
- **Source hygiene:** every touched file kept its byte-order mark and CRLF state (compared with the parent commit). A shell edit in this review briefly stripped CRLF from two test files; restored and re-checked.
- **DRY (fixed):** the implement pass copied `ConcurrentDDLTest`'s private `Evaluate(instance, expression)` helper verbatim into `CatalogStoreTest`. Moved it to `TestServerInstance.Evaluate(expression)`, next to the existing `ExecuteScript`, and both tests now call it.
- **Test coverage (fixed):** 115107 is a new error contract with no test; added `UnloadableStoreClassIsReported`. Still by inspection only: the `.sdf` log warning (a log line, no behaviour) and that engine-only use skips the check (the `!IsEngine` branch it sits in already gates the whole store).
- **Docs:** architecture, migration notes and AGENTS.md read and accurate; the plan's WS4 table row for this ticket had not been marked done, as other finished rows are — added a **Done** summary.
- **Performance / resource cleanup:** the check and the log probe are one string comparison and one `File.Exists` per server start; nothing to clean up.
- **Tripwires:** none added. K27 (side-by-side instance folder) is already tracked by `host-instance-configuration` in the plan.
- **Tickets filed:** none.

## Validation

- `dotnet build Dataphor.slnx`: succeeded, 0 errors; no warnings in the changed files (the Server project's existing CA1416 warnings remain).
- `dotnet test Dataphor.slnx --no-build`: DAE.Server.Tests Failed 0, Passed 106, Skipped 44 (the manifest's known-failure and unstable coverage cases).
