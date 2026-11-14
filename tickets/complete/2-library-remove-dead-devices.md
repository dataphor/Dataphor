description: Deleted the database connectors and storage drivers for systems that will not be supported (MySQL, DB2, InterSystems Caché, Linter, Fastore and two old Oracle drivers), which were unbuilt or had no working driver on current .NET, so later work does not have to port or maintain them.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: Libraries/MySQLConnection/, Libraries/DB2Connection/, Libraries/DB2400Connection/, Libraries/DataDirectOracleConnection/, Libraries/OracleConnection/, Libraries/CacheConnection/, Libraries/MySQLDevice/, Libraries/DB2Device/, Libraries/CacheSQLDevice/, Libraries/LinterDevice/, Libraries/FastoreDevice/, External/Fastore.Client.dll, External/Fastore.Communication.dll, External/Thrift.dll, Dataphor/Dataphor.sln, docs/migration-notes.md, docs/architecture.md
difficulty: easy
----
# Remove dead devices and connections — complete

## What landed (`ticket(implement): library-remove-dead-devices`)

- Deleted eleven library folders: the MySQL, DB2, DB2/400, DataDirect Oracle, ODP.NET Oracle (`Libraries/OracleConnection`) and Caché connections; the MySQL, DB2, Caché SQL, Linter and Fastore devices (89 files), plus `External/Fastore.Client`, `External/Fastore.Communication` and `External/Thrift` (`.dll` and `.pdb`), which only the Fastore device used.
- `Dataphor/Dataphor.sln`: removed the ten project blocks with their configuration and `NestedProjects` lines; deletions only, encoding and CRLF unchanged.
- `docs/architecture.md`: removed the "Dead" row of the "What is alive" table, which listed only these libraries.
- `docs/migration-notes.md`: a **done** row for the removed devices and connections (what to do: move data to a supported device, remove `create device` statements naming these classes) and a **planned** row for the parked Oracle (Microsoft provider), ADO and SAS libraries.

## Review findings

Checked:

- **Solution file diff** — read the whole diff: every removed line belongs to one of the ten GUIDs; no surviving project listed any of them in `ProjectDependencies`. No issue.
- **Dangling references in live code** — `git grep` for the library names, assembly names, driver namespaces (`MySql.Data`, `IBM.Data`, `InterSystems`, `Oracle.DataAccess`, `DDTek`) and `Thrift.dll`/`Fastore.` hint paths across csproj, sln, `tools/`, `Tests/` and C# source. The only hits are text that is not compiled or not live: `Dataphor/DAE/Schema/SQLDeviceMappings.d4` (reference text), the old WiX installer `Setup/Files.wxs` and `Setup/Files.wxs.bak`, WS8 documentation, the coverage test data in `Libraries/TestFramework.Coverage.Devices` (left alone per the project rule against editing test expectations), and the `#if USEODP` (undefined) branch in `OracleDevice.cs`. `Dataphor/DAE.NativeCLI/Thrift/` is an unrelated Thrift IDL experiment that never referenced `External/Thrift.dll`. No issue.
- **Library requirements** — checked every `Libraries/*/*.d4l`: only `TestCache` requires a deleted library (`CacheConnection`, `CacheSQLDevice`). `TestCache` is a side-project library that `library-park-side-projects` parks, and that ticket already records this requirement. Registering `TestCache` before the park would fail on the missing library; it is a test library nobody registers by default, and the park ticket covers it, so no new ticket. `OracleDevice.d4l` requires no deleted library.
- **Docs** — read the WS7 table in `docs/modernization-plan.md` (still accurate as the plan of record), `docs/architecture.md` and `docs/migration-notes.md`; open tickets on the board mention none of the deleted paths except `library-park-side-projects`, which mentions them correctly. No update needed.
- **Build and tests** — reran `python tools/net-spike/run.py <scratch dir> --smoke --coverage`: Base/Engine/Server/every library project 0 errors, concurrency 0 errors, resolution cache 0 errors, coverage `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13`, matching the expected values in AGENTS.md. Dataphoria, DAEServer, DAE.Service, D4Runner and the test projects show the same build errors as before; none referenced the deleted projects.
- **Tests** — none added and none needed: the change deletes code that nothing live compiles or loads, and the spike shows the live set unchanged. No tests to cut.
- **Compatibility / migration notes** — removing these D4 class names is a compatibility change explicitly authorized by the plan (WS7, D12); the migration-notes entry is present in the same commit. No issue.
- **Source hygiene** — no edited C# files; docs keep their LF endings. No issue.

Found: nothing requiring a fix, ticket or tripwire.
