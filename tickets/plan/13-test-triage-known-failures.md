description: For every engine test script that still fails or has never been part of the test run, find out why and file a bug ticket for each distinct cause, so that every script is either passing or tied to a tracked fix.
prereq: test-decouple-coverage-base, port-engine-runtime-fixes
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents, tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4, tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4, tests/libraries/TestFramework.Coverage.Base/Documents/InsertCases.d4, tests/DAE.Server.Tests
difficulty: medium
----
**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout). Before the move, `tests/libraries/TestFramework*` was `Libraries/TestFramework*` and `tests/DAE.Server.Tests` was `Tests/DAE.Server.Tests`. The legacy `CoverageTest.cs` quoted below is replaced by `test-harness-inprocess`; read it from git history (`Tests/DAE.Server.Tests/CoverageTest.cs` at any commit before that ticket).

## Goal

Close the last part of gate G1's test half. G1 (plan, "Sequencing"): *from a clean clone, `dotnet build` and `dotnet test` pass on .NET 10 with every D4 coverage script accounted for.* "Accounted for" is satisfied when **every one of the 135 scripts** in the coverage manifest is `pass`, or `known-failure` naming a ticket that is on the board and owns its cause. After `test-harness-inprocess` and `test-decouple-coverage-base`, the manifest still holds provisional entries: scripts recorded against `test-triage-known-failures` with only the error observed. This ticket finds each cause, files one `fix/` ticket per cause, and puts that ticket's slug in the manifest.

Why it matters: D2 makes the scripts the only oracle for "behaves like 3.1". A failure that sits in the manifest without a cause is a porting defect nobody is working on.

## Scope

In scope:

- Every manifest entry that is `known-failure` with slug `test-triage-known-failures` after the prerequisites land, and any other `known-failure` whose slug no longer explains it (for example `DebugLibrary` if it still fails after `port-engine-runtime-fixes` fixes K5).
- Every script that was outside the legacy suite (K20, `tests/DAE.Server.Tests/CoverageTest.cs` before the harness): the 47 that no active test ran, and the 12 existing scripts whose tests were `[Ignore]`d, whatever status the harness gave them. A script that now passes needs no ticket, but a pass by a script with no assertions is reported (see open choice 3).
- The ignored legacy test that names a script that does not exist (`TableIndexer`, reason "Build this coverage"): record that there is no script, and make sure the gap is listed for `test-coverage-gap-analysis` (`feat-ws3-test-layers`).
- Filing `fix/` tickets, grouped by cause, each with `repro: verified`, and updating the manifest so each `known-failure` names its ticket. This plan ticket may also emit one small `implement/` ticket for changes to the manifest or harness wiring itself (for example a per-script run option a script needs), if the planner does not make them inline.

Out of scope:

- Fixing the causes. They are the `fix/` tickets' work; this ticket only diagnoses far enough to name the site or the unsettled decision.
- Scripts that fail because of a known defect that already has an owner: K1 (`port-catalog-emitter-race`), K10 (`store-sqlite-concurrency`), K5 (`port-engine-runtime-fixes`, engine sites). Point the manifest entry at that slug; do not file a duplicate.
- Scripts that need infrastructure outside the in-process harness: a SQL Server device (`feat-ws7-devices-and-libraries`, which holds `test-device-live`), a second running Dataphor server or the network protocol (`feat-ws5-protocol-and-hosting`, which holds `test-client-server`), timing and benchmarks (`feat-ws3-test-layers`, `test-perf-baseline`). Point the manifest at the umbrella slug and add the script as an arm in that backlog ticket.
- New coverage for areas without scripts (`test-coverage-gap-analysis` in `feat-ws3-test-layers`).

## Settled constraints

- **The scripts are the oracle (D2).** There is no reference runtime. An assertion that fails on .NET 10 is presumed to be a porting defect until shown otherwise. **Never edit an expected value in a coverage script**, not even to "fix" what looks like a wrong expectation; a script believed wrong gets a ticket that explains the difference and routes the decision to `blocked/` if it changes D4 behaviour (`tickets/rules/10-dataphor.md`, "Compatibility is the constraint").
- **Already broken on 3.1 is a different case, but it has to be shown.** Twelve scripts were ignored in the legacy suite with a stated reason (table below), and some unwired scripts may never have passed. Evidence that a failure predates the port is the legacy ignore reason, the script's git history, and whether the failing construct depends on anything .NET-specific. Such a failure still gets a ticket, but the ticket says it predates the port.
- **No silent skips** (plan WS3, L1): every script stays in the manifest with a status; nothing is removed to make the count work.
- Root cause, not symptom (`tess/agent-rules/tickets.md`, "Before you file a ticket"): two failing scripts with the same cause are one `fix/` ticket listing both; an instance of a defect that already has a ticket is added to that ticket as an arm.
- `fix/` tickets are bug tickets: `repro: verified` (the failing harness case is the reproduction; name the exact test case and the error), `architecture: docs/modernization-plan.md#<section>` for the workstream that owns the site (WS2 for engine and server, WS3 for harness, WS4 for store, and so on).

## Facts verified in the tree

The 14 legacy-ignored tests and their reasons (legacy `CoverageTest.cs`):

| Script | Legacy reason | Notes from reading |
|---|---|---|
| `DAE` | Rewrite for native values processing | 20 lines |
| `Exceptions` | Rewrite for native values processing | 101 lines; checks resource strings for each error code |
| `ParserEmitter` | Fix | 28 lines; uses `TestParserEmitter` from the helper assembly |
| `SecurityLibrary` | Doesn't work through ExecuteScript | Needs implicit transactions off; run for real by `test-decouple-coverage-base` |
| `Serialization` | ScriptLibrary operator needs to be revisited | 66 lines; its own comment notes an unexpected `MSSQLBinary` representation in the library script |
| `ApplicationTransactions` | Doesn't work through ExecuteScript | 707 lines |
| `LibraryFilesCoverage`, `ScalarAllocation`, `RowAllocation`, `LockAllocation` | Build this coverage | 4–5 lines each: header only, no assertions |
| `MaximumRowCount`, `StreamAllocation` | Doesn't run 1/17/2004 | 51 and 36 lines |
| `TableIndexer` | Build this coverage | **No script file exists** |
| `ServerLink` | Integration test, needs server setup | Creates server links to an instance named `Dataphor` on `localhost` |

The 47 scripts no active legacy test ran: `DebugTest Derivation DerivationMaps DerivationReferenceGroups DetailLookupTest Domains EntryLibrary ForeignSuperKeysAndConstantReferences Functions IStringLibrary Internet JoinKeyInference LibraryDomains Patterns PresentationLibrary Register RemoteATOperatorEvaluation ReproContextError Security SerializationAggregateOperators SerializationConstraints SerializationDevices SerializationDomains SerializationEventHandlers SerializationOperators SerializationReferences SerializationTables SerializationTypes SerializationViews TestGH37 TestNetworkApplicationTransactions TestProcessExecutes Timing Timings Transaction.Atomicity.Cleanup Transaction.Atomicity.Run Transaction.Atomicity.SetOne Transaction.Atomicity.SetTwo Transaction.Atomicity.TestDAE Transaction.Atomicity.TestDAEMSSQL Transaction.Atomicity.TestMSSQL Transaction.Atomicity.TestMSSQLDAE Transaction.Isolation.Run Types.Coordinate Types.Length ViewDependencies _3VLProofs` (`DerivationMaps` had a commented-out test, "deprecated feature"). Observations that suggest groups, to be confirmed by running:

- **Not standalone tests:** `Register` is a comment-only placeholder for the library; `TestGH37` is an empty file; `Transaction.Atomicity.SetOne`, `SetTwo`, `Run`, `Cleanup` are helpers called by `Transaction.Atomicity.TestDAE` (and `Run`, `TestDAE`, the three `…MSSQL…` variants and `Transaction.Isolation.Run` use a table `CurrentCaseItems` that no `.d4` or `.cs` file outside `legacy/` creates).
- **Need a SQL Server device that does not exist:** `Transaction.Atomicity.TestMSSQL`, `TestDAEMSSQL`, `TestMSSQLDAE` use `Coverage.Base.CoverageDevice`, commented out of the helper library (handled by `test-decouple-coverage-base`, which points them at `feat-ws7-devices-and-libraries`).
- **Need a library the fixture does not register:** `Internet` uses `System.Internet` operators (`URLEncode`, `HTMLEncode`, …).
- **Need networking or a second server:** `ServerLink`; probably `TestNetworkApplicationTransactions` and `RemoteATOperatorEvaluation` (application transactions); check.
- **Performance, not correctness:** `Timing`, `Timings`.
- **Derivation family** (`Derivation`, `DerivationMaps`, `DerivationReferenceGroups`, `DetailLookupTest`, `PresentationLibrary`): exercise the `Frontend` library's derivation, which the fixture registers.
- **Serialization family** (`Serialization` plus nine `Serialization*` scripts): probably share a cause with the legacy reason for `Serialization`.

Helper-library operators the scripts rely on are defined in `tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4` (D4 wrappers) and the assembly `Alphora.Dataphor.DAE.Diagnostics` (`tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs`). `All.Coverage.d4` and `InsertCases.d4` in the same library are the original authors' own run lists, with per-script comments ("verified BTR 3/5/2007", "TODO: …") that are evidence of which scripts passed on the old runtime.

## Open choices for the planner

1. **Grouping and granularity.** One `fix/` ticket per cause, not per script. Decide how far to diagnose before filing: enough to name the code site or the decision (the filing bar), not the fix.
2. **Scripts that are not standalone tests** (helpers, placeholders, empty files). Options: run them and record whatever they do (a helper that passes standalone is `pass`); or add a third manifest status, such as `not-a-test`, with a reason, reported and counted in the run summary like a `known-failure`. The plan names only `pass` and `known-failure`; a third status is a change to the harness contract and goes in the small implement ticket if chosen.
3. **Scripts with no assertions** (`LibraryFilesCoverage`, `ScalarAllocation`, `RowAllocation`, `LockAllocation`, possibly `Register`). They will pass trivially. Recommended: record them as `pass` with a manifest comment saying they assert nothing, and list them for `test-coverage-gap-analysis` so the pass count is not read as coverage.
4. **Scripts that need run options** (implicit transactions off, a library registered first, the current library set): fix through the manifest's per-script options where the script's legacy run list shows the original runner did the same; anything beyond that is a harness change.
5. **Where a failure is traced to the harness rather than the engine** (for example "Doesn't work through ExecuteScript"), the ticket's site is the harness; say so.

## Done when

- Every manifest entry is `pass` or `known-failure` whose slug is on the board (a `fix/` ticket filed here, an existing Phase 1 ticket, or a backlog umbrella with the script added as an arm). No entry names `test-triage-known-failures` or `test-decouple-coverage-base`.
- Each `fix/` ticket filed here has `repro: verified`, names the failing harness case(s) and the exact error, states whether the failure is believed to predate the port and why, and names the site or the decision.
- `TableIndexer` and the assertion-free scripts are listed in `feat-ws3-test-layers` under `test-coverage-gap-analysis`.
- `dotnet test` passes with the updated manifest.
- Testing rule: no new tests here; the `fix/` tickets own their reproductions, which are the harness cases.
- Migration notes: none owed by this ticket.

## Risks

- Order dependence: a script that fails only after another has run is a harness interaction, not an engine defect; rerun a failing script alone before diagnosing it.
- Some scripts may hang; rely on the harness's per-script time limit and record timeouts as their own cause.
- Mistaking a porting defect for "never worked": the presumption under D2 is a porting defect unless the evidence above says otherwise.

If this splits, the ticket that finishes the work item keeps the slug `test-triage-known-failures`; earlier pieces get new slugs and are chained to it with `prereq:`.

## Note from planning `test-harness-inprocess`

There are 136 scripts, not 135 (`DistributedTransactions` was added by `server-remove-remoting-and-dtc`). The harness runs each script on its own server copied from a template catalog, because on one shared server scripts interfered: `SerializationOperators`, `SerializationTables`, `SerializationViews` and `TestNetworkApplicationTransactions` failed with "object already defined" after other scripts and pass alone, and `DebugTest` (an infinite loop by design, a debugger exercise) left the catalog locked for every later script. So a failure in the manifest is the script's own, not an effect of order. The planning run's observed errors for every failing script are in the `test-harness-inprocess` ticket; nine passing scripts contain no `raise` at all and carry `"note": "no assertions"` in the manifest.

## Note from `test-decouple-coverage-base`

With the helper library registering, 15 of the 25 scripts recorded against `test-decouple-coverage-base` pass. Seven of the other ten now carry this ticket's slug, each with the error observed in four full runs (Debug three times, Release once; details in that ticket's handoff):

- `DAE`: `TestException: Hiding object name allowed.` at line 8 (`TestCatalog()`). Legacy ignore reason: "Rewrite for native values processing".
- `Exceptions`: `TestExceptions` is not defined: its `create operator` is commented out of `tests/libraries/TestFramework.Coverage.Base/Documents/DiagnosticsLogic.d4` (the class `TestExceptionsNode` exists in `TestNodes.cs`).
- `SecurityLibrary` (run with implicit transactions off, as the legacy test did): `DataphorException: constraint failed  UNIQUE constraint failed: DAERights.Name`. The error has no D4 locator and the server log (`DATAPHOR_TEST_LOGGING=1`) shows only start and stop. The script's only `CreateRight` is its first statement, and `CreateRightNode` checks `RightExists` before inserting, so the duplicate insert probably comes from somewhere else (not traced).
- `StreamAllocation`: `Object "Coverage" not found` at line 5 (`Load('Coverage', 'CursorLibrary')`: the harness does not copy `TestFramework.Coverage`, which holds only forms, and `CursorLibrary` is a document of `TestFramework.Coverage.Scripts`). Legacy ignore reason: "Doesn't run 1/17/2004".
- `Transaction.Atomicity.Cleanup` and `Transaction.Atomicity.SetTwo`: helpers that `Transaction.Atomicity.TestDAE` runs after `Transaction.Atomicity.SetOne`; standalone, the tables `SetOne` creates do not exist. `SetOne` itself passes.
- `Transaction.Atomicity.TestDAE`: fails inside `Transaction.Atomicity.Run` on `CurrentCaseItems`, the same cause as `Run` and `Transaction.Isolation.Run`.

The three `Transaction.Atomicity.*MSSQL*` scripts point at `feat-ws7-devices-and-libraries` (they need `Coverage.Base.CoverageDevice`), which already lists them.
