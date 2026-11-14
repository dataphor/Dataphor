description: The helper library that the engine's test scripts depend on no longer needs a SQL Server on the machine; the SQL Server test device moved to the separate device-testing library, and 15 more test scripts now run and pass.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework/TestFramework.d4l, tests/libraries/TestFramework/Documents/Register.d4, tests/libraries/TestFramework.Coverage.Devices/TestFramework.Coverage.Devices.d4l, tests/libraries/TestFramework.Coverage.Devices/Documents/Register.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/Device.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/DeviceMSSQL.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/DataTypesMSSQL.d4, tests/libraries/TestFramework.Coverage.Devices/Documents/DeviceOracle.d4, tests/DAE.Server.Tests/CoverageScripts.cs, tests/DAE.Server.Tests/coverage-manifest.json, docs/migration-notes.md, docs/modernization-plan.md, AGENTS.md, tickets/plan/13-test-triage-known-failures.md
----
# Decouple the coverage helper library from SQL Server (K14)

## Summary

Known defect K14: the coverage helper library `TestFramework.Coverage.Base` requires `TestFramework`, and `TestFramework` required `MSSQLDevice` and created the SQL Server device `TestingDevice` against `localhost`. So the in-memory coverage suite could not register its helper library without a local SQL Server. That device and that requirement now belong to `TestFramework.Coverage.Devices`, the library for SQL Server device testing. This is the change, landed in `ticket(implement): test-decouple-coverage-base`:

- **Moved** from `tests/libraries/TestFramework/Documents/` to `tests/libraries/TestFramework.Coverage.Devices/Documents/`: `DeviceMSSQL.d4`, `DataTypesMSSQL.d4` and `DeviceOracle.d4`, all unchanged. `Device.d4` also moved, and its two `ExecuteScript` calls now name `Coverage.Devices`.
- **`TestFramework`**: `TestFramework.d4l` no longer requires `MSSQLDevice` and has an empty default device. Its `Register.d4` runs only `DataTypes`.
- **`TestFramework.Coverage.Devices`**: its `.d4l` requires `MSSQLDevice`. Its `Register.d4` runs `Device` and `DataTypesMSSQL` before `Types`. Its existing default device `TestingDevice` now resolves to its own device.
- **Harness (`tests/DAE.Server.Tests/CoverageScripts.cs`)**: `MSSQLDevice` is gone from the copied library list. A library that fails to register in the template now fails every script case. The new `CheckNoExternalDevice` fails the run if the template ends up with `MSSQLDevice` loaded or with a device named `TestingDevice`.
- **Manifest**: 15 scripts flipped to `pass`. Seven `known-failure` entries now carry `test-triage-known-failures` and their real errors, and three carry `feat-ws7-devices-and-libraries` (they need the SQL Server device `Coverage.Base.CoverageDevice`). No coverage script and no expected value was edited.
- **Docs**: migration note added, plan K14 marked `fixed`, the plan's WS3 row marked done, and the AGENTS.md coverage paragraph updated.

The renames and the edit to `Device.d4` share one commit: the runner commits each ticket once. `git diff -M` detects all four renames (`Device.d4` at 57 % similarity).

## Review findings

**Read first:** the full implement diff, `git show` of `ticket(implement): test-decouple-coverage-base`, before the handoff.

**Correctness of the library change.** I read `DeviceMSSQL.d4`, `DataTypesMSSQL.d4`, both `Register.d4` files and both `.d4l` files.
- Unqualified `TestingDevice` and `SetDefaultDeviceName("TestingDevice")` run while `TestFramework.Coverage.Devices` is the current library, so they resolve to and set that library's own device.
- `DataTypesMSSQL` alters the device with types from `TestFramework`, a required library, so they resolve.
- `Register.d4` runs `Device` before `Types` and `Tables`, so the device exists before those tables are created on it.
- The only other `TestingDevice` mentions are a string literal in `LibraryCoverage.d4` (a passing script, no dependency) and the commented-out `TypesMSSQL.d4` files.
- I checked `Plan.GetDefaultDeviceName` (`src/engine/DAE/Compiling/Plan.cs:602`): a library with an empty default device inherits from its required libraries. That confirms the migration note's claim about `TestFramework.StressTest`.

No defect found.

**Harness.** `RegisterInTemplate` wraps the engine error as the inner exception. `BuildTemplate` stores it in `_templateError`, and every script case then fails with it, which closes the gap that `test-harness-inprocess` left. `CheckNoExternalDevice` partly overlaps the copied library list: without `MSSQLDevice` in that list, registering `MSSQLDevice` already fails. It still guards against someone adding `MSSQLDevice` back to the list. It checks two names, not a general "no external DBMS" rule. A rule based on device class is not expressible from the system catalog without more machinery, and the name check covers the defect that happened. I kept it as written. No defect found.

**Tests.** The K14 reproduction is the template check above. It runs once per coverage run and adds no test case. The 25 manifest entries are the characterization; each one gives the error that was actually seen, and the notes on `Cleanup`, `SetTwo` and the three `*MSSQL*` scripts give the reason. I cut nothing, because nothing restates the implementation. I added nothing: no defect was found that needs a reproduction.

**Source hygiene.** The edited `.d4`, `.d4l` and `.cs` files keep their byte-order-mark state (only `CoverageScripts.cs` has one, before and after) and their CRLF line endings. The two `.d4l` files still end without a final newline. Comments are short and say why, not what. The K14 `NOTE:` that was removed described the defect this ticket fixes, so removing it was correct.

**Docs.**
- I read the changed AGENTS.md paragraph, the migration-notes row, plan K14, the WS3 row and the "Current state" sentence; they match the code. The run time of about 3.5 minutes matches my run (3.42 min).
- `docs/architecture.md` mentions `MSSQLDevice` only in general terms, so it needs no change.
- `Setup/Files.wxs` still lists the moved files under `TestFramework\Documents`. It is the legacy WiX installer and points at a `..\Deploy\` tree that no longer exists. I left it alone: earlier library moves did the same, and `feat-ws9-installer-and-release` replaces it.

**Board.** `feat-ws7-devices-and-libraries` already lists the three `*MSSQL*` scripts as an arm. The note appended to `tickets/plan/13-test-triage-known-failures.md` gives each of the seven failures with its observed error. No open ticket still points at `test-decouple-coverage-base` for work.

**Validation (this review, Windows 11, no SQL Server on the machine).**
- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test Dataphor.slnx --no-build`, run twice in full.
  - The coverage run was green both times. Template built in 1.4–1.5 s; `pass` 110 passed, 0 failed; `known-failure` 29 matched, 0 mismatched, 0 unexpectedly passing; discovery problems 0.
  - In one of the two runs, the unrelated test `ConcurrentDDLTest.ConcurrentSessionsStoreTheirOwnScripts` failed with "Duplicate object name "General"". Its stack ends in `LoadedLibrary.GetNameResolutionPath` (`src/engine/DAE/Schema/Library.cs`), which fills a shared cache with no lock. Run alone, the test passed 4 times out of 4.
  - That test copies no library folders and touches nothing this ticket changed. It is not in `tickets/.pre-existing-known.md`, so I recorded it in `tickets/.pre-existing-error.md` with the stack and the likely cause, for the triage pass. I did not skip it or fix it here.

**Tripwires.** None new.

**Minor fixes applied in review.** None needed.

**Major findings / tickets filed.** None. The intermittent concurrency failure goes through the pre-existing-failure channel above, not through a ticket from this review.
