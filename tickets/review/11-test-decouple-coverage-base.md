description: The helper library that the engine's test scripts depend on no longer needs a SQL Server on the machine; the SQL Server test device moved to the separate device-testing library, and 15 more test scripts now run and pass.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework/TestFramework.d4l, tests/libraries/TestFramework/Documents/Register.d4, tests/libraries/TestFramework.Coverage.Devices/TestFramework.Coverage.Devices.d4l, tests/libraries/TestFramework.Coverage.Devices/Documents/Register.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/Device.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/DeviceMSSQL.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/DataTypesMSSQL.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/DeviceOracle.d4, tests/DAE.Server.Tests/CoverageScripts.cs, tests/DAE.Server.Tests/coverage-manifest.json, docs/migration-notes.md, docs/modernization-plan.md, AGENTS.md, tickets/plan/13-test-triage-known-failures.md
----
# Review: decouple the coverage helper library from SQL Server (K14)

## What changed

**Libraries.** The SQL Server device `TestingDevice` and the `MSSQLDevice` requirement moved from `TestFramework` to `TestFramework.Coverage.Devices`.

- Moved, content unchanged: `DeviceMSSQL.d4`, `DataTypesMSSQL.d4`, `DeviceOracle.d4`, from `tests/libraries/TestFramework/Documents/` to `tests/libraries/TestFramework.Coverage.Devices/Documents/`.
- Moved and edited: `Device.d4`. Its two calls now read `ExecuteScript('Coverage.Devices', 'DeviceMSSQL');` and, still commented out, `//ExecuteScript('Coverage.Devices', 'DeviceOracle');`.
- `TestFramework.d4l`: the `MSSQLDevice` reference is gone and `defaultdevicename=""`. Nothing else changed: CRLF, no BOM, still no final newline.
- `TestFramework/Documents/Register.d4`: runs only `DataTypes` (the types `D4Expression`, `Description`, `ShortID`).
- `TestFramework.Coverage.Devices.d4l`: adds `<libraries.libraryreference name="MSSQLDevice" version="*" />` after its two existing references.
- `TestFramework.Coverage.Devices/Documents/Register.d4`: runs `Device` and `DataTypesMSSQL` first, before `Types`.
- `DeviceMSSQL.d4` and `DataTypesMSSQL.d4` are unedited. They name the device without a library prefix, and `SetDefaultDeviceName("TestingDevice")` now sets the default of the devices library, which already named that device.
- `TestFramework.Coverage.Base` and every coverage script are unchanged. No expected value was edited.

**Single commit, not two.** The ticket asked for a commit with only the renames, then one with the edits. The runner makes one commit for the whole ticket, and agents may not commit. The renames are staged with `git mv`. `git diff HEAD -M` detects all four as renames: three at 100 % similarity, `Device.d4` at 57 %. So `git log --follow` should work for all four, but `Device.d4`'s rename shares its commit with a 2-line edit. If that matters, a human can split the commit.

**Harness** (`tests/DAE.Server.Tests/CoverageScripts.cs`):

- `MSSQLDevice` is removed from the fixed `Libraries` list.
- A failure to register a library in the template now throws `InvalidOperationException("Registering <lib> failed.")`, with the engine's error as inner exception. `BuildTemplate` catches it into `_templateError`, and every script case then fails with "The coverage template instance could not be built: …". That is the "make the registration failure fail the run" step that `test-harness-inprocess` left for this ticket.
- New `CheckNoExternalDevice()` runs after the helper library registers. It fails the template, and so the run, if `exists (LoadedLibraries where Name = 'MSSQLDevice')` or `exists (Devices where Name like '%TestingDevice')`.
- The old K14 `NOTE:` at the registration is removed; it described the bug that is now fixed.

**Manifest.** No entry names `test-decouple-coverage-base` any more.

| Outcome | Scripts |
|---|---|
| now `pass` (15) | `Cursors`, `DML`, `Explode`, `Lexer`, `NodeOptimization`, `OperatorText`, `ParserEmitter`, `PlanCache`, `ScanTable`, `SemiTables`, `SystemCatalog`, `Transaction.Atomicity.SetOne`, `TransitionConstraintWithNamedRowVariables`, `TransitionConstraints`, `_3VLProofs` |
| `known-failure`, `test-triage-known-failures` (7) | `DAE` ("Hiding object name allowed"), `Exceptions` (`TestExceptions` is commented out of `DiagnosticsLogic.d4`; same substring as before), `SecurityLibrary` ("UNIQUE constraint failed: DAERights.Name"; preamble kept), `StreamAllocation` (`Object "Coverage" not found`), `Transaction.Atomicity.Cleanup` (`Unknown identifier "ListIntegerTestTableOne"`), `Transaction.Atomicity.SetTwo` (`Unknown identifier "ListIntegerTestTableOneReferences"`), `Transaction.Atomicity.TestDAE` (`Unknown identifier "CurrentCaseItems"`) |
| `known-failure`, `feat-ws7-devices-and-libraries` (3) | `Transaction.Atomicity.TestDAEMSSQL`, `TestMSSQL`, `TestMSSQLDAE`, all `Unknown identifier "Coverage.Base.CoverageDevice"`, with a note. That backlog ticket already lists them as an arm. |

Ticket vs. result: of the ticket's "12 + `SecurityLibrary`", 11 pass, and `DAE`, `StreamAllocation` and `SecurityLibrary` fail for real. `Exceptions` is not among the 13 but was on this slug; it still fails for a reason of its own. `Transaction.Atomicity.SetOne` passes, which the ticket did not predict.

I appended a note to `tickets/plan/13-test-triage-known-failures.md` with what I saw for each of the 7. Notably, `SecurityLibrary`'s error has no D4 locator and the server log has nothing.

**Docs.**

- `docs/migration-notes.md`: one `done` row under "D4, catalog and libraries". It covers the device's new full name, that `TestFramework` has no default device any more, and what that does to `TestFramework.StressTest`.
- `docs/modernization-plan.md`: K14 is `fixed` with its new location; the WS3 table row gets a **Done** sentence; the "Current state" K14 sentence is now in the past tense.
- `AGENTS.md`: the coverage paragraph's K14 / 25 s caveat is replaced by the new behaviour and timing.

## Validation (Windows 11, .NET 10, no SQL Server on the machine)

- `dotnet build Dataphor.slnx` and `-c Release`: 0 errors.
- `dotnet test Dataphor.slnx --no-build` (Debug), and the same with `-c Release`: both `Test Run Successful.`
  - Common.Tests: 13/13.
  - DAE.Server.Tests: 150 total, 121 passed, 29 skipped.
  - Summary block: 139 cases; `pass` 110 passed, 0 failed; `known-failure` 29 matched, 0 mismatched, 0 unexpectedly passing (feat-ws5 1, feat-ws7 3, triage 25); discovery problems 0.
- **Template time** went from about 25 s to 1.4–2.3 s.
- **Total run time** is 3.2 min (Release) to 3.5 min (Debug), not the 2.5 min the harness handoff measured. Two things changed: other tickets added tests since then (150 tests, not 140), and each script case is slower. In the Release run, 118 of the cases took 1 s or more, and the 37 under a second had a median of 0.94 s; the harness handoff measured 0.65–0.85 s per script. The likely cause is that every copied server now loads the helper library's catalog (TestFramework, SQLDevice, Connection, Coverage.Base and their objects). I did not measure the cause.
- **Stable outcomes.** The 10 still-failing scripts gave the same errors in two Debug runs before the manifest change. The new substrings then matched in one Debug and one Release run.
- **K14 check, by hand** (each change reverted; the layout was rebuilt and checked against the source):
  - `"MSSQLDevice"` added back to `Libraries`, and the layout's `TestFramework.d4l` given the `MSSQLDevice` reference: the template failed with "Registering TestFramework.Coverage.Base loaded MSSQLDevice; …", and `Lexer` failed with it.
  - Same `.d4l` edit without the library folder: "Registering TestFramework.Coverage.Base failed. <= … Object "MSSQLDevice" not found."
  - `create device TestingDevice class "MemoryDevice";` appended to the layout's `TestFramework/Documents/Register.d4`: "… created a device named TestingDevice; …".
- **Searches before handing off:**
  - `TestFramework.TestingDevice` in `.d4`, `.d4l`, `.dfd`, `.dfdx`, `.cs`: no hits.
  - C# and project files that name `TestFramework`: only the harness (`CoverageScripts.cs`, `CoverageManifest.cs`) and the `DAE.Diagnostics` project references in `tools/layout/Layout.csproj` and `EngineCheck.csproj`.
  - `.d4l` files that require `TestFramework`: exactly the five test libraries the ticket lists.
  - Nothing in `tests/` or `src/` registers `TestFramework.Coverage.Devices`.
- Docs link check: 203 errors, the known pending set.

## Known gaps and things to check

- **`TestFramework.Coverage.Devices` registration is checked by reading only.** It needs a SQL Server, and this machine has none. By reading:
  - Required libraries register first, so `MSSQLDevice` is loaded before `Register.d4` runs.
  - `Device` → `DeviceMSSQL` creates `TestingDevice` before `Types` and `Tables`, and those tables go to the library's default device `TestingDevice`.
  - `DataTypesMSSQL` alters the device with the `TestFramework` types, resolved by unqualified name.
  
  The library's own SQL Server device-coverage script (`TestMSSQLDevice.d4`, device `TestDevice`) is unchanged. `test-device-live` (in `feat-ws7-devices-and-libraries`) is the first thing that will run this for real.
- **`TestFramework.StressTest`** has an empty default device and requires only `TestFramework`, so its default device now resolves to `System.Temp` (checked in `Plan.GetDefaultDeviceName`). Its table now lives in memory instead of on the SQL Server device. No test runs it; the migration note says so.
- **`TestFramework.Coverage`** was already ambiguous (`Temp` from Coverage.Base, `TestingDevice` from Coverage.Devices) and stays so; its `Register.d4` is empty. `TestFramework.Controls` creates nothing. Both are unchanged, checked by reading.
- **`Setup/Files.wxs`**, the legacy WiX installer, still lists the four moved files under `TestFramework\Documents`. I left it alone, following `library-park-move` and `library-park-side-projects`: the file points at a `..\Deploy\` tree that no longer exists, and WS9 (`feat-ws9-installer-and-release`) replaces it.
- **Leftover SQL Server pieces in the helper library**, for `feat-ws7-devices-and-libraries`: `TestFramework.Coverage.Base/Documents/Device.d4` (entirely commented out; it once defined `CoverageDevice`, which the three `*MSSQL*` scripts need) and `TypesMSSQL.d4` (alters `TestingDevice`; commented out of `Register.d4`). `UnsupportedOracleOperators.d4` is never run. All are left as they were.
- **`Serialization`** still matches its old error (`Syntax error near "select"`). Its output did not visibly change with `MSSQLDevice` no longer loaded.
- **The harness gap from `test-harness-inprocess` is closed.** `Connection` (private OLE DB files) is registered again in the template, through Coverage.Base → SQLDevice → Connection, and a failure there now fails the run.
- **The template folder is now always left behind** for the next run to delete, because the copied `Alphora.Dataphor.DAE.Connection.dll` stays loaded. The harness already reported this ("Could not delete …Template-8 …"), and it is by design from `test-server-fixture`.

## Tests

- **K14 reproduction, at the fixture seam:** `CoverageScripts.CheckNoExternalDevice` (no `MSSQLDevice` loaded, no `TestingDevice`), plus template registration failure now failing the run. It runs once per coverage run; it is not a separate test case.
- **Characterization:** the 25 manifest entries above. 15 are now `pass`, and 10 are `known-failure` with their real errors.
- Nothing else added.

## Tripwires

None new. I removed the old K14 `NOTE:` in `BuildTemplate` because it described the defect this ticket fixes.
