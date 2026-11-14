description: Delete the database connectors and storage drivers for systems that will not be supported (MySQL, DB2, InterSystems Caché, Linter, Fastore and two old Oracle drivers), which are unbuilt or have no working driver on current .NET, so later work does not have to port or maintain them.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: Libraries/MySQLConnection/, Libraries/DB2Connection/, Libraries/DB2400Connection/, Libraries/DataDirectOracleConnection/, Libraries/OracleConnection/, Libraries/CacheConnection/, Libraries/MySQLDevice/, Libraries/DB2Device/, Libraries/CacheSQLDevice/, Libraries/LinterDevice/, Libraries/FastoreDevice/, External/Fastore.Client.dll, External/Fastore.Communication.dll, External/Thrift.dll, Dataphor/Dataphor.sln, docs/migration-notes.md, docs/architecture.md
difficulty: easy
----
# Remove dead devices and connections — review handoff

## What was done

- `git rm -r` of the eleven library folders (MySQL, DB2, DB2/400, DataDirect Oracle, ODP.NET Oracle and Caché connections; MySQL, DB2, Caché SQL, Linter and Fastore devices) — 89 files — plus `External/Fastore.Client.{dll,pdb}`, `External/Fastore.Communication.{dll,pdb}`, `External/Thrift.{dll,pdb}`.
- `Dataphor/Dataphor.sln`: removed 117 lines — the ten `Project(...)`/`EndProject` blocks, their configuration lines and their `NestedProjects` lines. No other line changed (diff is deletions only); `grep -c -i <GUID>` is 0 for all ten; file is still ASCII, CRLF, no BOM. None of the ten appeared in another project's `ProjectDependencies`.
- `docs/architecture.md`: the "Dead" row of "What is alive" consisted only of these libraries, so the whole row was deleted rather than left empty.
- `docs/migration-notes.md`: split the planned row into a **done** row (removed devices/connections and their D4 class names; move data to a supported device, remove `create device` statements naming these classes) and a **planned** row for the parked Oracle (Microsoft provider), ADO and SAS libraries.
- Both docs files keep LF (git prints its usual "LF will be replaced by CRLF" warning, which is the repo's autocrlf setting, not a change).

## Verification

`git grep -n -I -E "MySQLConnection|MySQLDevice|DB2Connection|DB2400Connection|DB2Device|DataDirectOracle|CacheConnection|CacheSQLDevice|LinterDevice|FastoreDevice|Fastore\.(Client|Communication)|DAE\.Connection\.Oracle\.Oracle"` excluding `tickets/`, `docs/modernization-plan.md`, `docs/migration-notes.md`, hit counts per file:

- `Dataphor/DAE/Schema/SQLDeviceMappings.d4` (5) — uncompiled reference text
- `Documentation/DevelopersGuide/Devices.adoc` (1), `Legacy/Documentation/**` (many, incl. `DataphorDocumentation.sln`, `Docs2.0/DB2DeviceReference.xml`) — WS8 docs
- `Libraries/Documentation/Documents/DocLibraries.d4` (2) — parked by the move
- `Libraries/TestCache/TestCache.d4l` (2), `Libraries/TestCache/Documents/Device.d4` (1) — parked by `library-park-side-projects`
- `Libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCaseData.d4` (26) — test data, untouched per project rule
- `Setup/Files.wxs` (65) and `Setup/Files.wxs.bak` (65) — old installer (the `.bak` was not in the ticket's table but is the same old-installer case)

All are "none" sites in the ticket's reference table. `Libraries/Libraries.sln` no longer exists (deleted by `repo-delete-dead-trees`).

Spike, `python tools/net-spike/run.py <scratch dir> --smoke --coverage`: Base/Engine/Common/Server and every library project 0 errors; concurrency 0 errors; resolution cache 0 errors; coverage `SUMMARY pass=62 fail=14 timeout=0 skipped-ignored=13`, matching AGENTS.md. The non-core projects (Dataphoria, DAEServer, DAE.Service, D4Runner, test projects) show the same build errors they had before; none of them referenced the deleted projects. Failing coverage scripts include SecurityLibrary, Explode, TransitionConstraints, SystemCatalog, NodeOptimization, SemiTables, ScanTable, DebugLibrary, PlanCache, OperatorText (all "operator not registered"/platform errors unrelated to devices). I did not run the parent commit for comparison since the totals match the documented expectation.

## Tests

None added: nothing live changes behaviour, and the spike shows the live set still builds and runs.

## Known gaps / for the reviewer

- `Libraries/OracleDevice/.../OracleDevice.cs` still names `"OracleConnection.OracleConnection"` under `#if USEODP` (undefined); left as the ticket says — `library-park-side-projects` parks that device.
- No other `.d4l` requires any deleted library (ticket's prior check); not re-verified beyond the `git grep` above, which would have shown a `libraryreference` by name.
