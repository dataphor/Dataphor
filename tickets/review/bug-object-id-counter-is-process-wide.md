description: Each Dataphor engine now numbers its own catalog. Before this, a program holding two engines at once - which is what happens whenever a program opens a network connection to a server - had the second one reset a counter the first was still using, and the first then reused identifiers it had already handed out and its schema changes started failing.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Schema/ObjectIDGenerator.cs, src/engine/DAE/Server/RemoteSession.cs, tests/DAE.Server.Tests/ObjectIDPerEngineTest.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/ProtocolClient.cs, tests/DAE.Server.Tests/ConnectionKeepAliveTest.cs, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs, tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs, docs/migration-notes.md
----
## What landed

The last of the four steps the chain `engine-object-id-generator` began. `Engine`'s constructor now creates a `Schema.ObjectIDGenerator` of its own and keeps it for the engine's lifetime, so two engines in one program number their catalogs independently.

- `src/engine/DAE/Server/Engine.cs`: `_objectIDGenerator = new Schema.ObjectIDGenerator();` in the constructor, where it was `Schema.Object.SharedObjectIDGenerator`. The field's comment says why it is per engine and why it is never replaced.
- `src/engine/DAE/Schema/Schema.cs`: the transition's shared instance and the three statics that drew from it - `GetNextObjectID()`, `SetNextObjectID(int)` and `GetObjectID(MetaData)` - are deleted. `Schema.Object.SaveObjectID`, `RemoveObjectID` and `UnassignedID` are untouched.
- `src/engine/DAE/Schema/ObjectIDGenerator.cs`: `SetLastAllocated` is deleted - it existed only for `SetNextObjectID`, and nothing else may move a counter down. `Allocate`, `Allocate(MetaData)`, `Reset` and `EnsureAllocatedThrough` are unchanged.
- `Engine.InitializeCatalog`'s `ObjectIDGenerator.Reset()` is **unchanged**, in the same place and under the same conditions, as the ticket required; it now resets the engine's own generator. Whether a repository engine should reset at all is still `bug-client-catalog-rebuild-reuses-object-ids`'s question.

Nothing in the solution needed fixing afterwards - the build was the census and it is 0 errors in Debug and Release - which is what the three earlier tickets were for.

**The workarounds are gone.** The ticket said `TestEnvironment.WithTheObjectIDCounterRestored` had one use; it had **four**, and all four are removed: `ProtocolClient`'s constructor, `ConnectionKeepAliveTest.NewClient`, and two in `CoreWcfProtocolTest` (`WithASecondHost`, and the body of `TestTheInstancePortBeingTakenStopsTheServer`, whose lambda is unwrapped). The helper itself is deleted, as is the save-and-restore in `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs` (its `_nextObjectID` field, both assignments and the two lines in `Release()`).

**Two factual corrections to the ticket, both worth knowing:**

- **`tests/Dataphor.Benchmarks` is in `Dataphor.slnx`** (line 40). The ticket - and the previous link's handoff - said the compiler would not flag it and it had to be checked by hand. It is compiled by every build, so the deleted statics would have failed it. Checked anyway by running the program (below).
- **The failure the reproduction produces is the in-memory one, not the store one.** Measured, by putting every engine back on one shared generator and running the new test: the second `create table` fails in `CatalogDeviceSession.CacheCatalogObject` with `An item with the same key has already been added. Key: 359`, because the engine's catalog index refuses the reused identifier before the statement reaches the store. `UNIQUE constraint failed: DAEObjects.ID` - what the protocol fixtures showed and what the ticket predicted - is the same collision arriving where the colliding object is not already in the engine's cache. Both are the defect; the test's remarks record both routes.

## Tests

One test added, the reproduction, at the lowest layer that shows it.

| Test | What it verifies |
|---|---|
| `ObjectIDPerEngineTest.StartingASecondEngineLeavesTheFirstServersCatalogAlone` (`tests/DAE.Server.Tests/ObjectIDPerEngineTest.cs`) | An in-process server creates a table; a second `Engine` is then started and disposed in the same process the way `LocalServer` does (its own GUID name, no library directory, no catalog store); the first server's next `create table` still succeeds. **Verified to fail before the fix** and to pass after it, as above. Disposing the second engine before the second `create table` is deliberate - a stopped engine used to have moved the shared counter just as far as a running one. |

No test was added for the generator itself: `ObjectIDGeneratorTest` already covers its three properties, from `engine-object-id-generator`, and the restart property is covered by `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`. Three `CoreWcfProtocolTest` cases become the second, broader reproduction now that the workaround is out of `ProtocolClient.cs` - they are the cases that failed with the store error before it was written - and needed no edit beyond the removal.

## Validation run

- `dotnet build Dataphor.slnx` and `dotnet build Dataphor.slnx -c Release`: 0 errors, 143 warnings (the tree's standing set).
- The three suite parts from `AGENTS.md`, each in the foreground: part 1 **666 passed, 0 failed, 33 skipped** (6 m 24 s); part 2 **67 passed, 0 failed, 11 skipped** (3 m 2 s); part 3 **50 passed, 0 failed, 12 skipped** (3 m 2 s). No pre-existing failures surfaced, so no `.pre-existing-error.md` was written.
- `python tools/net-spike/run.py <work-dir> --smoke`: exactly the expected set - the converted projects, `DAE.Client`, `DAE.Client.Provider`, `DAE.Client.Controls`, `Frontend.Client`, `Frontend.Client.Windows` and `Frontend.Client.WPF` 0 errors; `Dataphoria` 1 (`ContextMenu`), `DAE.Service.ConfigurationUtility` 1 (`NotifyIcon.ContextMenu`), `D4Runner` blocked on `NU1201`. Smoke 1,080 operators; concurrency 8×15 DDL+query, 0 errors; resolution cache 0 errors. Work dir was outside the repository and has been deleted.
- `artifacts/bin/DAE.Benchmarks/release/DAE.Benchmarks.exe` run by hand (1 m 49 s, 4 benchmarks, no errors). Worth doing rather than skipping: this program starts roughly forty engines in one process over a run, which is the configuration whose save-and-restore was just deleted. Numbers on this machine, for reference only - not a baseline, and `docs/perf-baseline.md` was not touched: EngineStart 2,807 ms, DdlDmlMix 165 ms, CatalogStoreCold 2,921 ms, CatalogStoreWarm 264 ms.

## What to exercise

- **Two engines in one process, in both orders.** The new test starts the second engine *after* the first server has allocated. The reverse - a client-side engine started first and a server started after it - is covered by the `CoreWcfProtocolTest` fixture (it hosts a server and then connects clients to it) but not by a case that names the property. Worth a reviewer's eye on whether that is enough.
- **A client connection held across schema changes.** `bug-client-catalog-rebuild-reuses-object-ids` is a *separate* defect with a single client-side engine, and this ticket neither fixes nor worsens it. That ticket has a new note (appended by this one) telling it that the tree it reproduces against has changed: the workaround it measured is gone, so its recorded reproduction must be re-run before anything is concluded about the mechanism.
- **The catalog store across this chain's four commits.** The number of identifiers an engine consumes during start-up changed, so a store written by an earlier 4.0 development build does not line up with an engine built from the finished chain. 4.0 catalogs are rebuilt from scripts rather than migrated and 4.0 has not shipped, so this is acceptable; it is in `docs/migration-notes.md`. No test covers it - every test run builds a fresh store.
- **Identifier exhaustion** is unchanged from `engine-object-id-generator`: refused with `SchemaException` 113250 rather than wrapping into the `-1` sentinel. A per-engine generator makes it less likely, not more.

## Known gaps and loose ends, honestly

- **Comment maintenance was a larger part of this change than the ticket anticipated.** Eleven files carried prose asserting that the counter is shared; each is now either corrected or deleted: `TestServerInstance` (its process-wide-state `NOTE:`), `SamplesCorpusTest`, `CoverageScripts`, `OutOfProcessProtocolTest` (three paragraphs), `ObjectIDGeneratorTest`, `RemoteSession`, `tests/Dataphor.Benchmarks/README.md`, and the WS2 paragraph in `docs/modernization-plan.md`. A reviewer should read these as prose edits and check none of them now overstates what landed. Two in particular:
  - **`CoverageScripts.cs`**: the comment justifying a *child process* for the linked server gave the shared counter as its reason. That reason is gone, so the comment now gives the reason that stands - the script resolves the link through the listener service, so the linked server has to be hosted on a port. That is read off the surrounding code (the fixture sets `DataphorServiceUtility.ListenerPortNumber` to the child's listener port), not measured by trying an in-process linked server. The arrangement itself is unchanged and the case passes.
  - **`AbandonedClientProcessTest.SessionCount` and `tests/TestHarness/Program.cs`** both pointed at "an arm on `bug-object-id-counter-is-process-wide`" for the defect that makes a table read over the wire unsafe after a catalog change. That defect is owned by `bug-client-catalog-rebuild-reuses-object-ids`, so both now name that slug. Neither test was changed.
- **`src/engine/DAE/Runtime.Instructions/AdornNode.cs:178` is a commented-out line that names the now-deleted `Schema.Object.GetObjectID`.** Pre-existing dead code from 3.1, left alone deliberately: it is not this ticket's file and deleting commented-out alternatives is a separate concern. A reviewer may cut it; nothing depends on it either way.
- **`docs/migration-notes.md` line 119** (the `engine-object-id-generator` entry) said the three statics "still work"; it now says they still worked *at that point* and points at the new entry that removed them. The earlier entries are historical records, so they were not rewritten further.
- **No measurement of whether the per-engine generator costs anything.** It adds one small object per engine and removes a static field access; the benchmark numbers above are within their own run-to-run spread, so they neither show nor exclude a difference, and nothing was claimed from them.
