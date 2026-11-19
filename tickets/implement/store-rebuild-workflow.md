description: Write an automated test that rebuilds a server catalog from scripts on a fresh instance (libraries, roles, rights, device users), restarts afterwards and measures how long the sample set takes, so the workflow has a safety net and a baseline.
architecture: docs/modernization-plan.md#ws4-catalog-store
files: tests/DAE.Server.Tests/StoreRebuildWorkflowTest.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs, tests/DAE.Server.Tests/TestServerInstance.cs, src/engine/Server/Server/Server.cs, samples/, libraries/
difficulty: medium
----
## What this is

The plan's WS4 "No migration" paragraph commits the product to catalog rebuilds from the application's scripts — and the plan pins the WS4 **Done when** list on the rebuild workflow being tested. Today the `InProcessTestFixture` already starts a fresh server on an empty SQLite catalog and runs scripts; what is missing is:

- A single test that performs an end-to-end rebuild of a non-trivial set of libraries on a fresh instance, including the security objects (users, roles, rights) and device users that live only in the store.
- A restart in the same test, proving the rebuilt catalog survives server shutdown and reopen.
- A measured timing published back to the ticket for a baseline. Later `docs-admin-guide`, `tool-cli-catalog-commands` and `security-password-storage-design` can quote it.

This test is the proof the plan asks for; it is not the CLI command itself. The CLI command lives in `tool-cli-catalog-commands` (WS10, still in backlog) and will drive the same workflow once it exists.

## Shape

New file `tests/DAE.Server.Tests/StoreRebuildWorkflowTest.cs`, one `[TestFixture]` with one `[Test]` and a small helper method. Not a subclass of `InProcessTestFixture` — the test owns its own `TestServerInstance` because it starts, stops and restarts the server inside the test body.

### Steps the test performs

1. Create a fresh `TestServerInstance("StoreRebuildWorkflow", libraries: sampleSetLibraries)`. `sampleSetLibraries` is the plan's "sample set": `General`, `System.Platform`, `SimpleDevice`, plus the two or three repository samples that register on `SimpleDevice` without a SQL Server (checked once at implement time; `Sample.Common` and `Sample.Demo` are the likely candidates). Name the exact list in the implement handoff.
2. Start the server. Assert the catalog file was created and the server reports `Running`.
3. Rebuild script section:
   - Register the libraries in order (`SystemLibraryUtility`'s `EnsureLibraryRegistered` through `ExecuteScript`).
   - Create two roles and one user through D4 (`CreateRole`, `CreateUser`), grant a named right to each role, grant one role to the user. Create one device user on `General` (`CreateDeviceUser` with a password).
   - Run one small DDL from each library (one `create table`, one `create view`, or similar — enough to prove operator registration and derivation plumbing hit the store).
4. Record the wall time of step 3 with `System.Diagnostics.Stopwatch`; report through `TestContext.WriteLine`. Hook the measurement into `test-perf-baseline` only by printing — no new baseline framework here.
5. Stop the server.
6. Start the same instance again. Assert the catalog file is intact (same size ±, or opens without error — use "opens and returns the expected objects" rather than a byte count).
7. For each role, user, device user and DDL object created in step 3, assert it is present in the catalog after the restart (`select Name from Users where Name = '…'`, `D4Catalog`-style queries). Cleanup is `TestServerInstance.Dispose`.

### What the test is **not**

- Not a benchmark with statistical weight. One measurement, printed. `test-perf-baseline` owns comparison with history.
- Not a test of the CLI `dataphor rebuild` command — that command does not yet exist.
- Not a test of upgrade scripts (the `Frontend` library's `Upgrades/`) — covered by `test-frontend-derivation-golden` and its downstream work.
- Not a test of migration from a 3.1 catalog — the plan rules that out (D3).

## TODO

- Pick and document the sample set in the implement handoff: the list of libraries, the one DDL per library, and the specific roles / users / device users and rights the test creates.
- Write `StoreRebuildWorkflowTest.cs` as above.
- Name the stopwatch output line with a stable prefix so it is greppable in CI logs (e.g. `StoreRebuildWorkflow timing: register+security+DDL = Xms`).
- Add a one-paragraph note in `docs/modernization-plan.md` at the WS4 section, under the "No migration" paragraph, pointing at this test as the live proof — same place and shape as other `**Done**` annotations on neighbouring rows.
- Run the three-part suite from root `AGENTS.md`; include the timing line in the handoff.

## Edge cases & interactions

- **SQLite WAL and the restart.** The server's clean shutdown should drain WAL; a `.sqlite-wal` sidecar left behind after stop is a red flag worth asserting, because a reopen on stale WAL could silently reconcile. Check the directory after stop: only `DAECatalog.sqlite` should remain. Pin as an assertion in the test.
- **Concurrent writers while the restart happens.** The test is single-session; no concurrency here. Not this ticket's problem — `store-sqlite-concurrency` and its tests already pin concurrent DDL.
- **Deferred device stops.** `bug-catalog-deferred-device-stops-ignore-nesting` and `engine-at-leave-rolls-back-open-work` landed. The rebuild test should not trigger them, but if the sample DDL does anything with application transactions, name it; the implement handoff can declare "no application transactions touched" if that is the case.
- **Device users and password storage.** The plan has `security-password-storage-design` still to land. This test creates a device user with a password and reads it back after restart — it asserts the store round-trips the stored form, not that the form is secure. Add one line to the handoff flagging that the password column stores whatever the current implementation puts there; the security ticket owns changing it.
- **Timing variance.** Agents' machines vary. Expect ~1–5 s for the sample set and no assertion on the number — the purpose is a baseline printout, not a pass/fail gate.
- **Sample dependencies.** A sample that requires a SQL Server device (`Sample.Pubs`, `Sample.IssueTracker`, `Sample.Rental` likely) is out of the sample set here; name the exclusions in the handoff. The `feat-ws7-devices-and-libraries` backlog item is where their rebuild test eventually lives.
- **Pre-existing failures.** `tickets/.pre-existing-known.md` names three; none touches this workflow.

## Verification

- `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart` green on SQLite.
- `dotnet build Dataphor.slnx` 0 errors; three-part suite from root `AGENTS.md` 0 failures.
- Handoff carries the measured timing line.
