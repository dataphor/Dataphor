description: When one program holds two Dataphor engines at once, the second one to start resets a shared counter, so the first one then reuses catalog identifiers it has already handed out and schema changes start failing or overwriting each other; this is the step that gives each engine its own counter.
prereq: engine-object-id-transient-tablevars
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Schema/ObjectIDGenerator.cs, tests/DAE.Server.Tests/ProtocolClient.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs, docs/migration-notes.md
repro: verified
difficulty: medium
----
## What goes wrong

Every catalog object the engine creates gets an integer identifier from one counter. That counter is a single variable shared by the whole program (`Schema.Object.GetNextObjectID`, a `static int` in `src/engine/DAE/Schema/Schema.cs`), and starting an engine sets it back to zero (`Engine.InitializeCatalog`, with the comment "this must be the first object created to avoid the ID being different on subsequent loads").

So when a program starts a second engine while a first one is still running, the second one's reset hands the first one identifiers it has already used. The next schema change in the first engine tries to write a catalog row whose identifier is already there, and the persistent catalog refuses it:

```
UNIQUE constraint failed: DAEObjects.ID
```

That is the visible failure. The quieter one is that two different objects can end up carrying the same identifier in memory before anything is written.

### How a program ends up with two engines

A client connection holds one. `LocalServer` - the client-side half of every network connection - starts an engine of its own to hold the catalog cache it compiles against (three lines: `new Engine()`, a name, `Start()`; no libraries directory and no catalog store). So:

- **Reproduced**: a test, or any program, that runs an in-process server and then opens a network connection to it. `tests/DAE.Server.Tests/CoreWcfProtocolTest.cs` does exactly this and failed with the error above on its first `create table` until it worked around the problem.
- **Reachable in a shipped configuration**: the IDE lets a user open several connections at once, and an embedded in-process server alongside a connection to a remote server is two engines in one process. A server-to-server link is *not* affected: it goes through `NativeCLISession`, which starts no engine.
- Out of process - a client program and a server program, which is how the product normally runs - there is one engine per process and nothing goes wrong. That is why this has survived.

## The work this ticket does

The three tickets before it (`engine-object-id-generator`, `engine-object-id-keys-and-orders`, `engine-object-id-transient-tablevars`) moved every allocation in the tree onto an `ObjectIDGenerator` reached through the engine - about 300 call sites - while leaving every engine pointing at one shared generator instance, so that nothing changed. This ticket flips that last switch and removes the scaffolding:

- `Engine.ObjectIDGenerator` returns the engine's **own** generator, created in the `Engine` constructor and never replaced (not held on `Schema.Catalog`, which `ClearCatalog` throws away).
- Delete from `Schema.Object`: the `static int` counter, `GetNextObjectID()`, `SetNextObjectID(int)`, `GetObjectID(MetaData)` and the transition's shared generator instance. Nothing should reference them; if the build says otherwise, that site was missed by one of the earlier tickets and belongs on the generator like the rest.
- `Engine.InitializeCatalog` keeps resetting - now its own engine's generator, through `ObjectIDGenerator.Reset()` - in the same place and under the same conditions as today. **Do not make the reset conditional in this ticket.** Whether a repository engine should reset at all is a separate, unsettled question, and `bug-client-catalog-rebuild-reuses-object-ids` owns it; leaving the reset exactly as it is keeps this ticket's only behaviour change the one in its title.
- Remove the three-line workaround and its helper: `TestEnvironment.WithTheObjectIDCounterRestored` (`tests/DAE.Server.Tests/TestEnvironment.cs`), its single use and the `NOTE:` above it in `tests/DAE.Server.Tests/ProtocolClient.cs`, and the same save-and-restore in `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs` (two `GetNextObjectID` reads and one `SetNextObjectID` write). The benchmarks project is outside `Dataphor.slnx`, so the compiler will not flag it - the deleted statics will, only when that project is built through the spike, so check it by hand.

## Tests

This is a bug fix, so it owes its reproduction, at the lowest layer that shows it.

**`ObjectIDPerEngineTest` (new, `tests/DAE.Server.Tests`).** One test: start an in-process server through the existing harness (`TestServerInstance`, as `StoreRebuildWorkflowTest` does), create a table in it; then start a second engine in the same process the way `LocalServer` does -

```csharp
Engine second = new Engine();
second.Name = Schema.Object.NameFromGuid(Guid.NewGuid());
second.Start();
```

- and then create a second table in the first server and assert that it succeeds. Before this ticket that second `create table` fails with `UNIQUE constraint failed: DAEObjects.ID`; after it, both engines number their own catalogs. Dispose the second engine in a `finally`. Name the ticket in the test's remarks.

That is the one test to add. Three existing `CoreWcfProtocolTest` cases also become reproductions once the workaround comes out of `ProtocolClient.cs` - they failed with the same error before the workaround was written - so they are the second, broader check and need no edit beyond the removal. Do not add a test for the generator itself: a test that `Allocate` returns increasing numbers checks arithmetic, and the restart property is already covered by `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`.

## Edge cases and interactions

- **A missed call site.** The build names it, because the statics are gone. Every project in `Dataphor.slnx` is covered; the ones outside it (`src/client`, `src/windows`, the client half of the frontend, the test libraries and `tests/Dataphor.Benchmarks`) are covered only by `python tools/net-spike/run.py <work-dir> --smoke`, so run it.
- **A second engine started and then stopped.** An engine that started and was disposed has moved nothing that belongs to another engine once this lands, which is the whole point; the comment on the removed workaround notes that today a stopped engine has done just as much damage as a running one. Verified by the new test, which could be extended to dispose the second engine before the second `create table` - do that, it costs nothing and covers the case the comment calls out.
- **Several engines allocating at once.** Two engines compiling on different threads now touch different generators, and the processes of one engine still share theirs through `Interlocked.Increment`. Verified by the existing concurrency fixtures (`ConcurrentDDLTest`, `CatalogStoreConcurrencyTest`) and by the protocol fixtures, which run a client-side engine and a server engine in one process.
- **The reset still has to be the first thing an engine's catalog does.** `InitializeCatalog` resets and then creates the catalog device, which must come out as identifier 1 on every start against the same store. Verified by `StoreRebuildWorkflowTest.RebuildRegistersLibrariesAndSurvivesRestart`.
- **A catalog store written by an earlier development build.** Across this chain's four commits the number of identifiers consumed during engine start-up changes, so the built-in objects are numbered differently and a store written by an earlier build of 4.0 will not line up with an engine built from the finished chain. 4.0 catalogs are rebuilt from their scripts rather than migrated - a decision the plan already records - and 4.0 has not shipped, so this is acceptable; it needs the migration note below. Not covered by any test: each test run builds a fresh store.
- **The client-side catalog rebuild.** A separate defect, with a single client-side engine and no second engine involved, survives this ticket: see `bug-client-catalog-rebuild-reuses-object-ids`, which carries the evidence and the reproduction. This ticket neither fixes nor worsens it.
- **Identifier exhaustion.** Unchanged from `engine-object-id-generator`: refused with a clear error rather than wrapping into the `-1` sentinel. A per-engine generator makes exhaustion *less* likely, not more, since a long-lived client-side engine no longer shares its counter with anything.

## TODO

- Read `AGENTS.md`, `docs/modernization-plan.md#ws2-engine-and-server-port` and the three earlier tickets in this chain. Preserve each file's tabs, CRLF line endings and byte-order-mark state.
- Make `Engine.ObjectIDGenerator` the engine's own instance, created in the constructor.
- Delete the `Schema.Object` statics and the transition's shared instance; fix whatever the build then names, by routing it through the engine, not by reinstating a static.
- Keep `InitializeCatalog`'s reset exactly as it is, now against the engine's own generator, and keep its comment.
- Remove the workaround from `ProtocolClient.cs`, the helper from `TestEnvironment.cs`, and the save-and-restore from `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs`.
- Add `ObjectIDPerEngineTest` as described.
- Add the `docs/migration-notes.md` entries: `Schema.Object.GetNextObjectID`, `SetNextObjectID` and `GetObjectID(MetaData)` are gone, and a custom C# extension reaches its engine's generator through `Plan.GetNextObjectID()`, `Plan.GetObjectID(MetaData)` or `Engine.ObjectIDGenerator`; and a catalog store created by an earlier 4.0 development build must be rebuilt from its scripts, because the identifiers of the engine's built-in objects have moved.
- `dotnet build Dataphor.slnx`, then the three suite parts from `AGENTS.md`, each as its own foreground command. Then `python tools/net-spike/run.py <work-dir> --smoke`.
