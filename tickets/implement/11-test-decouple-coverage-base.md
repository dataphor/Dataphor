description: The helper library that the engine's test scripts depend on currently insists on a SQL Server database on the local machine; move that database connection into the separate device-testing library so the main test scripts run anywhere.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework/TestFramework.d4l, tests/libraries/TestFramework/Documents/Register.d4, tests/libraries/TestFramework/Documents/Device.d4, tests/libraries/TestFramework/Documents/DeviceMSSQL.d4, tests/libraries/TestFramework/Documents/DataTypesMSSQL.d4, tests/libraries/TestFramework/Documents/DeviceOracle.d4, tests/libraries/TestFramework/Documents/DataTypes.d4, tests/libraries/TestFramework.Coverage.Devices/TestFramework.Coverage.Devices.d4l, tests/libraries/TestFramework.Coverage.Devices/Documents/Register.d4, tests/libraries/TestFramework.Coverage.Base/TestFramework.Coverage.Base.d4l, tests/libraries/TestFramework.Coverage.Base/Documents/Register.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents, tests/DAE.Server.Tests, docs/migration-notes.md
difficulty: medium
----
**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout). Before the move, `tests/libraries/TestFramework*` was `Libraries/TestFramework*` and `tests/DAE.Server.Tests` was `Tests/DAE.Server.Tests`. The coverage manifest and fixture are whatever `test-harness-inprocess` produced; read its complete/review ticket for their exact paths and format.

## Goal

Fix K14: `TestFramework.Coverage.Base`, the helper library every coverage-script run registers, cannot register without a SQL Server on `localhost`. The plan's cross-cutting requirement (WS3): "`TestFramework.Coverage.Base` must register with no external DBMS (move the SQL Server device into the device-coverage library)." With this fixed, the 12 scripts the spike could not run because the helper library failed to register, and `SecurityLibrary`, run for real for the first time on .NET 10. They either pass or show real failures, and the manifest records which.

## Cause (verified by reading)

The dependency is entirely through the `TestFramework` library; the helper library's own documents do not touch SQL Server.

- `tests/libraries/TestFramework.Coverage.Base/TestFramework.Coverage.Base.d4l` requires `Frontend`, `TestFramework`, `SQLDevice`; its default device is `Temp`. Its `Documents/Register.d4` runs `DiagnosticsLogic`, `Types`, `Tables`, `InsertCases` (once), `CreateTableFunctions`. Its SQL Server scripts (`Device.d4`, entirely commented out; `TypesMSSQL.d4`, which alters `TestingDevice`) are commented out of `Register.d4`, and `UnsupportedOracleOperators.d4` is never run. Its `Tables.d4` uses `SQLText` from `SQLDevice` (a C# library that needs no database server).
- `tests/libraries/TestFramework/TestFramework.d4l` requires `System` and **`MSSQLDevice`**, with `defaultdevicename="TestingDevice"`. Its `Documents/Register.d4` runs `DataTypes`, then `Device`, then `DataTypesMSSQL`:
  - `DataTypes.d4` creates the device-independent types `D4Expression`, `Description`, `ShortID`. These must stay where they are: `DML.d4` (one of the 12 blocked scripts) declares columns and variables of type `ShortID` about 400 times, and `tests/libraries/TestFramework.StressTest/Documents/Tables.d4` uses all three.
  - `Device.d4` runs `DeviceMSSQL` (and has `DeviceOracle` commented out).
  - `DeviceMSSQL.d4` creates `TestingDevice`, class `MSSQLDevice.MSSQLDevice`, `ServerName = "localhost"`, `DatabaseName = "DataphorTests"`, integrated security, and sets it as the library's default device.
  - `DataTypesMSSQL.d4` runs `alter device TestingDevice { create type D4Expression …, Description …, ShortID … }` (storage lengths).
  - `DeviceOracle.d4` (never run) creates an Oracle `TestingDevice`.

So registering `TestFramework.Coverage.Base` registers `TestFramework`, whose `Register.d4` creates and alters a SQL Server device. That is the step that fails without a server.

The 12 blocked scripts all use helper-library operators: `NegativeTest` (`Lexer`, `TransitionConstraints`, `TransitionConstraintWithNamedRowVariables`, and others), `TestNavigable`/`TestSearchable`/`TestBackwardsNavigable` (`DML`, `Cursors`, `Explode`, `SystemCatalog`), `TestRestriction`/`CheckRestrictionAlgorithm`/`CheckJoinAlgorithm`/`CheckSupported` (`Cursors`, `ScanTable`, `NodeOptimization`), `RunNavigableTest` (`SemiTables`), `TestIsPlanCached` (`PlanCache`), `TestOperatorText` (`OperatorText`). `SecurityLibrary` starts with `SetLibrary('Coverage.Base')` and uses `NegativeTestAs`.

## What moves where

Move the SQL Server device definition from `TestFramework` into `TestFramework.Coverage.Devices`, the device-coverage library. That library already requires `TestFramework` and `TestFramework.Coverage.Base`, already declares `defaultdevicename="TestingDevice"`, already creates its own SQL Server device (`TestMSSQLDevice.d4`, server `.`), and is registered only by device tests.

1. **Rename-only commit** (`git mv`, no content change — the project rule is that a move and an edit are separate commits):
   - `tests/libraries/TestFramework/Documents/Device.d4` → `tests/libraries/TestFramework.Coverage.Devices/Documents/Device.d4`
   - `tests/libraries/TestFramework/Documents/DeviceMSSQL.d4` → `tests/libraries/TestFramework.Coverage.Devices/Documents/DeviceMSSQL.d4`
   - `tests/libraries/TestFramework/Documents/DataTypesMSSQL.d4` → `tests/libraries/TestFramework.Coverage.Devices/Documents/DataTypesMSSQL.d4`
   - `tests/libraries/TestFramework/Documents/DeviceOracle.d4` → `tests/libraries/TestFramework.Coverage.Devices/Documents/DeviceOracle.d4`
   None of these names exists in the destination (it has `TypesMSSQL.d4`, `TestDevice.d4`, `TestMSSQLDevice.d4`, which are different files).
2. **Edit commit:**
   - `TestFramework.d4l`: remove `<libraries.libraryreference name="MSSQLDevice" version="*" />`; change `defaultdevicename="TestingDevice"` to `defaultdevicename=""`. Leave every other attribute and the file's encoding and line endings exactly as they are.
   - `TestFramework/Documents/Register.d4`: keep only `Frontend.ExecuteScript('TestFramework', 'DataTypes');` (delete the `Device` and `DataTypesMSSQL` lines).
   - `TestFramework.Coverage.Devices.d4l`: add `<libraries.libraryreference name="MSSQLDevice" version="*" />` (the device class and `MSSQLDevice` types now come from this library's own scripts).
   - `TestFramework.Coverage.Devices/Documents/Register.d4`: as the first two statements, before `Types`, add `ExecuteScript('Coverage.Devices', 'Device');` and `ExecuteScript('Coverage.Devices', 'DataTypesMSSQL');`, matching the existing call style in that file. `TestingDevice` must exist before the library's tables are created, because they go to the library's default device, `TestingDevice`.
   - Moved `Device.d4`: change `Frontend.ExecuteScript('TestFramework', 'DeviceMSSQL');` to `ExecuteScript('Coverage.Devices', 'DeviceMSSQL');` (and the commented Oracle line the same way, so it stays correct if uncommented).
   - `DeviceMSSQL.d4` and `DataTypesMSSQL.d4` need no edit: they refer to the device by its unqualified name, and `SetDefaultDeviceName("TestingDevice")` now sets the devices library's default, which already names it.
3. **Not changed:** `TestFramework.Coverage.Base` (its `.d4l` keeps `TestFramework`, so `ShortID` and the other types stay loaded for `DML`), its never-run SQL Server leftovers (`Device.d4`, `TypesMSSQL.d4`, `UnsupportedOracleOperators.d4`, listed in the handoff for `feat-ws7-devices-and-libraries`), and every file in `TestFramework.Coverage.Scripts`. **No expected value in any coverage script is edited** (D2; `tickets/rules/10-dataphor.md`).

After this, registering `TestFramework.Coverage.Base` loads `Frontend` (→ `SimpleDevice`), `TestFramework` (→ `System` only), `SQLDevice` (→ `Connection`); nothing in that set creates or contacts an external device. If the harness copies a fixed list of libraries into its instance, drop `MSSQLDevice` from that list.

## Scripts that depend on the SQL Server device

Searched all 135 scripts for SQL Server device use (`MSSQL`, `TestingDevice`, `localhost`, `DataphorTests`, `CoverageDevice`, device classes):

- `Transaction.Atomicity.TestMSSQL`, `Transaction.Atomicity.TestDAEMSSQL`, `Transaction.Atomicity.TestMSSQLDAE` set the default device to `Coverage.Base.CoverageDevice`, a SQL Server device whose definition is commented out of `TestFramework.Coverage.Base/Documents/Device.d4` (it named a specific host). They cannot pass with or without a server until a device test library defines that device. They stay where they are (moving them would change the script set the manifest is built from) and are `known-failure` with slug `feat-ws7-devices-and-libraries` (the umbrella that holds `test-device-live`) and the observed error — unless `test-harness-inprocess` already recorded them with a more specific slug.
- `LibraryCoverage` passes the string `"TestingDevice"` to `LibraryDescriptor` and `SetDefaultDeviceName`, which do not check that the device exists; it passed in the spike while `TestingDevice` could not be created. Not dependent.
- `ServerLink` needs a second Dataphor server on `localhost`, not SQL Server; it belongs to `test-triage-known-failures`.
- `Serialization` (ignored in the legacy suite) has a comment noting that its library script once included an `MSSQLBinary` representation. With `MSSQLDevice` no longer loaded, its output may change; it is triaged by `test-triage-known-failures`, not here.
- No other script refers to a SQL Server device.

## Expected outcome and manifest update

Run the full harness and update the manifest from what it shows:

- The 12 (`Lexer`, `DML`, `Cursors`, `TransitionConstraintWithNamedRowVariables`, `Explode`, `TransitionConstraints`, `SystemCatalog`, `NodeOptimization`, `SemiTables`, `ScanTable`, `PlanCache`, `OperatorText`) and `SecurityLibrary`: each becomes `pass`, or `known-failure` with slug `test-triage-known-failures` and the error now observed (a real failure, no longer the registration error). `SecurityLibrary` must run with implicit transactions off, as the legacy test did (`SetUseImplicitTransactions(false);` before the script); use the harness's per-script run option for this.
- Any other entry whose recorded error was the helper library failing to register is updated the same way.
- No entry keeps slug `test-decouple-coverage-base` after this ticket.

## Tests

- K14's reproduction: the harness's fixture setup already fails when the helper library cannot register. Add one assertion at that seam (fixture setup, or one small test): after `EnsureLibraryRegistered('TestFramework.Coverage.Base')`, `MSSQLDevice` is not among the loaded libraries and no device named `TestingDevice` exists. This pins the decoupling so a later edit cannot reintroduce the dependency silently.
- The 13 scripts above are the characterization tests for the change; nothing else is added.

## Migration notes

The 3.1 installer shipped the `TestFramework.*` libraries (`Setup/Files.wxs` lists them). Add one row, status **done**, under "D4, catalog and libraries" in `docs/migration-notes.md`: the `TestFramework` test library no longer requires `MSSQLDevice` or creates the SQL Server device `TestingDevice`; that device is now created by `TestFramework.Coverage.Devices`. What to do: register `TestFramework.Coverage.Devices` where the device is wanted.

## Edge cases & interactions

- **Library dependency order.** Registration registers required libraries first. After the change, `TestFramework.Coverage.Devices` requires `TestFramework`, `TestFramework.Coverage.Base` and `MSSQLDevice`; its `Register.d4` creates `TestingDevice` before anything that needs it. Verify by inspection; registering this library still needs a SQL Server, and nothing in the default test run registers it (check that the harness does not).
- **Default-device resolution.** A library with an empty default device inherits one from its required libraries and raises `AmbiguousDefaultDeviceName` (113208) if two of them name different devices (`GetDefaultDeviceName` in `src/engine/DAE/Compiling/Plan.cs`). With `TestFramework` now empty, libraries that relied on inheriting `TestingDevice` from it change:
  - `TestFramework.StressTest` (empty default device, requires only `TestFramework`) now resolves to `System.Temp` (the System library's default) instead of the SQL Server device, so its table goes to memory. No test runs it; note it in the handoff.
  - `TestFramework.Coverage` (empty default; requires `TestFramework`, `TestFramework.Coverage.Base` (`Temp`) and `TestFramework.Coverage.Devices` (`TestingDevice`)) was already ambiguous and stays so; its `Register.d4` is empty. Unchanged; inspection.
  - `TestFramework.Controls` (empty `Register.d4`, one form) creates nothing. Unchanged.
  - `TestFramework.Coverage.Base` names `Temp` explicitly and is unaffected.
- **Qualified names.** The device's full name changes from `TestFramework.TestingDevice` to `TestFramework.Coverage.Devices.TestingDevice`. A search of `.d4`, `.d4l`, `.dfd`, `.dfdx` and `.cs` files finds no qualified reference; repeat the search (`TestFramework.TestingDevice`) before committing.
- **Name resolution of `ShortID` and friends in scripts.** Coverage scripts run with `General` as the current library and resolve `ShortID` because `TestFramework` is loaded. This ticket keeps `TestFramework` loaded through the helper library's requirement; `DML` passing (or failing for an unrelated reason) confirms it.
- **Something else requiring `TestFramework`.** Only test libraries require it: `TestFramework.Controls`, `TestFramework.Coverage`, `TestFramework.Coverage.Base`, `TestFramework.Coverage.Devices`, `TestFramework.StressTest`. The only C# references were in `CoverageDeviceTest.cs` and the old in-process fixture, which `test-harness-inprocess` replaced or deleted. Verify with a search before committing.
- **Stale catalogs.** The harness uses a fresh instance each run, so no catalog still holds a `TestingDevice` owned by `TestFramework`. A developer instance outside the harness that registered the old `TestFramework` should be rebuilt; this is covered by the migration note.
- **File hygiene.** `.d4` and `.d4l` files keep their existing encoding and line endings. The files touched here are UTF-8 without a BOM and use CRLF (other libraries' `.d4l` files, such as `SQLDevice.d4l`, do carry a BOM). Do not reformat. The move commit has only renames, so `git log --follow` works.

## TODO

- Read the `test-harness-inprocess` handoff: manifest path and format, per-script run options, how libraries reach the fixture.
- Commit 1 (renames only): move `Device.d4`, `DeviceMSSQL.d4`, `DataTypesMSSQL.d4`, `DeviceOracle.d4` from `tests/libraries/TestFramework/Documents/` to `tests/libraries/TestFramework.Coverage.Devices/Documents/`.
- Commit 2 (edits): `TestFramework.d4l` (drop `MSSQLDevice`, empty default device); `TestFramework/Documents/Register.d4` (only `DataTypes`); `TestFramework.Coverage.Devices.d4l` (add `MSSQLDevice`); `TestFramework.Coverage.Devices/Documents/Register.d4` (run `Device` and `DataTypesMSSQL` first); moved `Device.d4` (call targets).
- If the harness copies a fixed list of libraries, remove `MSSQLDevice` from it.
- Add the K14 assertion (no `MSSQLDevice` loaded, no `TestingDevice`) at the fixture seam.
- Make sure `SecurityLibrary` runs with implicit transactions off through the manifest's run option.
- Run the full harness; update the manifest entries for the 12 scripts, `SecurityLibrary`, and the three `Transaction.Atomicity.*MSSQL*` scripts as described; no entry left on slug `test-decouple-coverage-base`.
- Search for `TestFramework.TestingDevice` and for C# references to `TestFramework`; confirm none.
- Add the migration-notes row.
- Handoff: per-script results for the 13, the StressTest default-device change, the helper library's never-run SQL Server leftovers (for `feat-ws7-devices-and-libraries`).
