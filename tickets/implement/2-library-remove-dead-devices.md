description: Delete the database connectors and storage drivers for systems that will not be supported (MySQL, DB2, InterSystems Caché, Linter, Fastore and two old Oracle drivers), which are unbuilt or have no working driver on current .NET, so later work does not have to port or maintain them.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: Libraries/MySQLConnection/, Libraries/DB2Connection/, Libraries/DB2400Connection/, Libraries/DataDirectOracleConnection/, Libraries/OracleConnection/, Libraries/CacheConnection/, Libraries/MySQLDevice/, Libraries/DB2Device/, Libraries/CacheSQLDevice/, Libraries/LinterDevice/, Libraries/FastoreDevice/, External/Fastore.Client.dll, External/Fastore.Communication.dll, External/Thrift.dll, Dataphor/Dataphor.sln, docs/migration-notes.md, docs/architecture.md
difficulty: easy
----
# Remove dead devices and connections

## Goal

A Dataphor *device* maps D4 tables to an external database; a *connection* library supplies the .NET driver wrapper a device talks through. The plan supports SQL Server and PostgreSQL (D15) and removes the devices and connections that are either not built by any current solution or depend on a driver with no .NET 10 version (WS7 library table, "Remove" row). Deleting them now means the reorganization (`repo-move-to-new-layout`) does not relocate them and no later ticket is tempted to convert them. History keeps them (D12).

## Delete list (verified against the tree)

| Folder | Files | What it is | Notes |
|---|---|---|---|
| `Libraries/MySQLConnection/` | 4 | MySQL connection | no `.d4l` |
| `Libraries/DB2Connection/` | 6 | DB2 connection | |
| `Libraries/DB2400Connection/` | 6 | DB2/400 (iSeries) connection | |
| `Libraries/DataDirectOracleConnection/` | 4 | DataDirect Oracle connection | no `.d4l` |
| `Libraries/OracleConnection/` | 6 | the plan's "ODP" connection: wraps the unmanaged ODP.NET provider (`Oracle.DataAccess`; assembly `Alphora.Dataphor.DAE.Connection.Oracle.Oracle`), plus a VS2003 project file. Used by the Oracle device only when compiled with `USEODP` | the Oracle device's default connection is `MSOracleConnection`, which is parked, not deleted |
| `Libraries/CacheConnection/` | 8 | InterSystems Caché connection, with a vendor XML doc file | |
| `Libraries/MySQLDevice/` | 6 | MySQL device | no `.d4l` |
| `Libraries/DB2Device/` | 12 | DB2 device | |
| `Libraries/CacheSQLDevice/` | 11 | Caché SQL device | |
| `Libraries/LinterDevice/` | 11 | Linter device | |
| `Libraries/FastoreDevice/` | 15 | Fastore device | |
| `External/Fastore.Client.dll` + `.pdb`, `External/Fastore.Communication.dll` + `.pdb`, `External/Thrift.dll` + `.pdb` | 6 | the Fastore device's binaries | `git grep` finds no other reference (the `Thrift` folder in `DAE.NativeCLI` is unrelated and stays) |

The plan's WS7 row calls these "not built by any solution"; in fact `Dataphor/Dataphor.sln` includes ten of their projects (all except `CacheConnection`). The other half of the row's reason — no working driver on modern .NET — holds for every one of them, so the removal stands.

## References found outside the deleted folders, and what to do

| Where | Reference | Action |
|---|---|---|
| `Dataphor/Dataphor.sln` | ten projects, GUIDs below | remove (see below) |
| `Libraries/Libraries.sln` | eight of the projects | none: `repo-delete-dead-trees` deletes this solution. If it still exists when you run, leave it alone |
| `Libraries/TestCache/TestCache.d4l` | requires `CacheConnection` and `CacheSQLDevice` | none: `TestCache` is parked by `library-park-side-projects`; a parked library may have unmet requirements |
| `Libraries/OracleDevice/Source/Device.Oracle/OracleDevice.cs` ~line 373 | class name `"OracleConnection.OracleConnection"` under `#if USEODP` | none: the Oracle device is parked, and the symbol is not defined in its project |
| `Dataphor/DAE/Schema/SQLDeviceMappings.d4` lines 212, 422, 440, 573, 1654 | `class "DB2Device.…"`, `class "LinterDevice.…"` in example device definitions | none: the file is a `None` item in `Engine.csproj`, not compiled or embedded; it is reference text |
| `Libraries/TestFramework.Coverage.Devices/Documents/InsertDeviceCaseData.d4` | 26 rows with device type `DB2DeviceUDB` / `DB2DeviceiSeries` | none: test data; the project rule forbids editing expected values in test scripts, and rows for an absent device are inert |
| `Libraries/Documentation/Documents/DocLibraries.d4` ~line 140 | `DB2DeviceReference` documentation page | none: that library is parked by the move and its output is WS8's concern |
| `Setup/Files.wxs` | component references for the deleted libraries | none: old installer, replaced by `feat-ws9-installer-and-release` |
| `Legacy/Documentation/`, `Documentation/` | reference chapters for DB2, Linter and others | none: WS8 (`docs-convert-asciidoc`, `feat-ws8-documentation-remainder`) decides what the new reference covers |
| `docs/architecture.md` "What is alive" table | "MySQL, DB2, DB2400, DataDirect, ODP and Caché connections (not built by any solution); Fastore device" in the Dead row | delete those words from the row (the row may already be shorter after `repo-delete-dead-trees`) |
| `docs/migration-notes.md` | the "planned" row on parked and deleted devices | see "Migration notes" |

No other library's `.d4l` requires any deleted library (checked every `libraries.libraryreference` in `Libraries/*/*.d4l`), and the spike (`tools/net-spike/projects.txt`, `COVERAGE_LIBS` in `run.py`) builds and copies none of them.

### Solution entries to remove from `Dataphor/Dataphor.sln`

| Project | GUID |
|---|---|
| `DAE.Connection.DataDirect.Oracle` | `575592B2-44ED-4E9D-B033-A1B213DC86CE` |
| `DAE.Connection.DB2` | `4CA0DBAD-7C1C-4EBF-85A2-CD1758F936E3` |
| `DAE.Connection.DB2400` | `13A4C808-5471-419D-B21E-F65BA5D874FD` |
| `DAE.Connection.MySQL` | `25DD5528-B68B-474C-8F6A-AD475CEE51BC` |
| `DAE.Connection.Oracle.Oracle` | `F513DCE2-6DC9-47CF-B307-8213DDA80F80` |
| `DAEDeviceDB2` | `9969EADB-3E5D-4D60-8268-A7D860CE69E8` |
| `DAEDeviceLinter` | `2E8AA030-8081-44D5-A25C-D1A7726AC640` |
| `DAEDeviceMySQL` | `3F4A39C3-7317-4E96-91E9-C30EADE8D142` |
| `DAEDeviceFastore` | `3E75A4A4-4A4A-4B48-A92F-D802ECEB19ED` |
| `DAEDeviceCacheSQL` | `A11F5D4A-E9AD-41CB-A216-502F7FA57A53` |

Remove each `Project(...)` … `EndProject` block and every other line containing the GUID (configuration lines, `NestedProjects` lines, `ProjectDependencies` entries). Afterwards `grep -c -i <GUID> Dataphor/Dataphor.sln` is 0 for each. Keep CRLF and no byte-order mark; touch no other line.

## Out of scope

- Parking the Oracle device, `MSOracleConnection`, `ADOConnection` and `SASDevice` (moved under `legacy/libraries/`, not deleted): `library-park-side-projects`.
- ODBC and OLE DB connections: kept; `device-connection-cleanup` (in `feat-ws7-devices-and-libraries`) converts or parks them. That ticket also deletes the uncompiled VS2003 project files inside `Libraries/Connection/Source`; the one inside `Libraries/OracleConnection/Source` goes with this ticket's folder.
- `External/ICSharpCode.TextEditor.*`: live, owned by `debt-ws12-hygiene-remainder`.
- The PostgreSQL and SQL Server devices: ported in `feat-ws7-devices-and-libraries`.

## Settled decisions

- D12: deletion by ordinary commits; no history rewrite.
- D15: SQL Server and PostgreSQL are the supported devices; Oracle, ODBC and OLE DB are kept only where cheap, otherwise parked.
- D7 and the compatibility rule: D4 class names are a compatibility surface. Removing these libraries removes the class names they registered (for example `DB2Device.DB2Device`, `LinterDevice.LinterDevice`, `MySQLConnection.MySQLConnection`, `OracleConnection.OracleConnection`); the plan authorizes it (WS7 "Remove"), and the migration notes record it.

No known defect (K-number) is fixed here.

## Migration notes

In `docs/migration-notes.md`, "D4, catalog and libraries", split the row "Devices and connections for Oracle, ADO (COM) and SAS are parked; MySQL, DB2, Caché, Linter, DataDirect and Fastore are deleted." into:

- **done**: "The MySQL, DB2, DB2/400, InterSystems Caché, Linter and Fastore devices and connections, the DataDirect Oracle connection and the ODP.NET (`Oracle.DataAccess`) Oracle connection are removed, with the D4 class names they registered." What to do: "Move the data to a supported device (SQL Server, PostgreSQL, the in-memory device or SimpleDevice) before upgrading; remove `create device` statements that name these classes."
- **planned** (left for `library-park-side-projects`): "Devices and connections for Oracle (through the Microsoft provider), ADO (COM) and SAS are parked and not shipped." What to do: unchanged ("SQL Server, PostgreSQL, the in-memory device and SimpleDevice ship in 4.0").

## Done when

- `git ls-files` prints nothing for each folder and file in the delete list.
- `Dataphor/Dataphor.sln` contains none of the ten GUIDs.
- `git grep -n -I -E "MySQLConnection|MySQLDevice|DB2Connection|DB2400Connection|DB2Device|DataDirectOracle|CacheConnection|CacheSQLDevice|LinterDevice|FastoreDevice|Fastore\.(Client|Communication)|DAE\.Connection\.Oracle\.Oracle"` finds only the sites listed as "none" in the reference table above, plus `docs/modernization-plan.md`, `docs/migration-notes.md` and `tickets/`. Put the trimmed output in the handoff.
- The spike gives the same results as before the change: `python tools/net-spike/run.py <work-dir> --smoke --coverage` with `<work-dir>` outside the repository (engine, server and libraries 0 errors; concurrency 0 errors; coverage `pass=62 fail=14`, same failing scripts). Since the spike references none of the deleted paths, one run after the change compared with the expected results in `AGENTS.md` is enough; if it differs, run it once on the parent commit in a separate clone to tell a pre-existing difference from one this ticket caused.
- No test is owed: nothing live changes behaviour, and the spike shows the live set still builds and runs.

## Edge cases & interactions

- **`OracleConnection` versus `MSOracleConnection`.** Only `Libraries/OracleConnection/` (ODP.NET) is deleted; `Libraries/MSOracleConnection/` and `Libraries/OracleDevice/` are parked by `library-park-side-projects`. Verified by inspection of `git status`.
- **Order with `repo-delete-dead-trees`.** Both tickets edit `Dataphor/Dataphor.sln` and `docs/architecture.md`; neither depends on the other. Edit by whole lines so either order applies cleanly. `Libraries/Libraries.sln` is deleted by the other ticket; do not edit it here. Verified by inspection.
- **Parked libraries left with unmet requirements.** `TestCache` (parked) requires two deleted libraries. Parked code is out of the build and out of the runtime layout, so nothing tries to register it. Verified by inspection; `library-park-side-projects` notes it.
- **Test data mentioning removed devices.** The DB2 rows in `InsertDeviceCaseData.d4` stay untouched (project rule). Verified by `git diff --stat` showing no change under `Libraries/TestFramework*`.
- **Formatting.** `Dataphor/Dataphor.sln` keeps CRLF and no BOM; the two `docs/` files keep LF. Verified with `git diff` showing only the intended lines.

## TODO

- Delete the eleven library folders and the six Fastore and Thrift files under `External/` with `git rm -r -q`.
- Remove the ten GUIDs' entries from `Dataphor/Dataphor.sln`; check each count is 0.
- Edit the Dead row in `docs/architecture.md` and split the migration-notes row.
- Run the `git grep` from "Done when" and the spike; record both in the handoff.
