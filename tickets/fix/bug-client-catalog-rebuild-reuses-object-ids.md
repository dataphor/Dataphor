description: A program connected to a Dataphor server over the network fails, or silently stores the wrong thing, after the server's schema changes and it reads a table again - it reuses an identifier for two different things in the copy of the schema it keeps locally.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/protocol/Client/Server/LocalServer.cs, src/protocol/Client/Server/LocalPlan.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Server/Engine.cs, src/engine/Server/Server/CatalogCache.cs, src/engine/Server/Server/RemoteServerPlan.cs, tests/DAE.Server.Tests/AbandonedClientProcessTest.cs
repro: verified
difficulty: hard
----
## What goes wrong

A program that connects to a Dataphor server over the network keeps a local copy of the part of the server's schema it has compiled against. When the server's schema changes, that local copy is thrown away and rebuilt. After the rebuild, the program can give one identifier to two different things in its local copy - and then either fails outright or, in a release build, silently replaces one of them with the other.

**This happens with a single engine in the program and no second engine involved**, which is what separates it from `bug-object-id-counter-is-process-wide`. That ticket fixes a different defect at the same line of code - an engine starting up resetting a counter that belonged to the whole program - and the chain it ends gives every engine an identifier generator of its own. Reproduce and diagnose this one on the tree as it stands, which is also the only way to see the reproduction fail; but the **fix** will be expressed in terms of that per-engine generator, so the implement ticket this one emits must carry `prereq: bug-object-id-counter-is-process-wide`.

## The evidence on record

Reproduced by an earlier agent (verified, deterministic, Debug build, 2026-11-21), and this is the reproduction to rebuild first:

- One client connected over the network to a server in another process reads a table eight times, then changes the server's schema, then reads the table once more.
- The last read fails with error 106185, "Errors occurred while deserializing client cache time stamp", wrapping `An item with the same key has already been added. Key: 984` from `CatalogDeviceSession.CacheCatalogObject` (`Device.CatalogIndex.Add`, `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`), reached from `LocalExpressionPlan.GetDataType` (`src/protocol/Client/Server/LocalPlan.cs`).
- The colliding identifier (984) and the number of reads needed (8) were the same on every run; the identifier counter stood at 997 at the failure.
- Reading a *table* is what trips it. A scalar read (`Count(...)`) never does: the server sends the client no schema fragment for one.
- **A release build does not fail; it silently replaces.** The `#if !DEBUG` block at the top of `CacheCatalogObject` removes any object already in the local copy *by name* before adding, which is what lets the identifier collision through without an error - its comment says "it must be there as a result of some error and the best course of action in a production scenario is to just replace it". So in a shipped client this is a silently wrong local copy of the schema, and in a Debug build of the same client it is an exception. That asymmetry is why it has not been seen.
- Where it bites in the product: any client that holds a connection across changes to the server's schema and reads tables through it - which is what the Windows client, Dataphoria and `DataView` do continuously.
- No existing test does enough table reads to trip it. `test-client-server-abandoned` met it with a cursor-based poll and moved that case to a scalar count rather than build on the defect; the reason is recorded at the site (`AbandonedClientProcessTest.SessionCount`).

## What the planning pass established, and what it could not

The earlier record explains the collision as the server sending the client a schema fragment that carries the server's own identifiers, which then clash with the client's. **That explanation is wrong, and should not be built on.** The fragment is emitted with `EmitMode.ForRemote` (`RemoteServerPlan.GetCatalog`), and every `EmitStatement` override that writes a `DAE.ObjectID` tag writes it only for `EmitMode.ForStorage` and strips it otherwise (`Schema.Object.SaveObjectID` / `RemoveObjectID`; checked across `src/engine/DAE/Schema`). So the fragment carries no identifiers at all: the client allocates every identifier in its local copy itself, from its own engine.

What the planning pass did establish, by reading the code:

- The client's local engine is a repository (`Engine.IsEngine` is true; `Server` overrides it to false). It has no catalog store, never persists anything and never sends an identifier back to the server. Its identifiers are purely local, so nothing requires them to match the server's or to be reproducible from one rebuild to the next.
- The rebuild path is `LocalServer.EnsureCacheConsistent` -> `Engine.ClearCatalog` -> `InitializeCatalog`, which resets the identifier counter to zero and then re-creates the engine's built-in objects. It runs in the middle of preparing an expression, where no caller is in a position to save and restore anything - which is why the three-line workaround that `bug-object-id-counter-is-process-wide` removed could never have covered this path.
- Client and server agree about what the client holds: the server keeps a per-session record of it and resets that record to the base set when the schema timestamp moves (`CatalogCache.GetRequiredObjects` -> `cache.EnsureConsistent(..., _defaultCachedObjects)`), which is the counterpart of the client's own throw-away-and-rebuild. So the client is not simply missing objects the server thinks it has.

**What is not established is the mechanism of the recorded collision.** After the reset the local copy is empty and the built-in objects are re-created with fresh identifiers, so on the face of it nothing should be able to duplicate one. Something survives the rebuild, or is created twice, or is added to the local copy with an identifier allocated before the reset, and reading the code did not find it. Candidates noticed but not confirmed: `EnsureCacheConsistent` carries the old class loader's assemblies and classes across the rebuild, and `Schema.RegisteredClass` objects carry identifiers allocated before it; and the rebuild may create a different number of objects than the first start-up did, because classes registered on demand during the earlier reads are already loaded the second time.

## Expected behaviour

Two different things in one engine's catalog never carry the same identifier, whatever the engine does to its catalog in between. In particular, a client that holds a connection across any number of schema changes on the server, and reads tables through it, keeps working - and a release build of it does not quietly substitute one schema object for another.

## What done looks like

- A test in the suite that reproduces this before the fix: a single client, over the network, enough table reads, a schema change on the server, one more read. Model it on the out-of-process client fixtures (`AbandonedClientProcessTest`, `OutOfProcessProtocolTest`), which already stand a server up in another process. It must fail on the unfixed tree, in a Debug build, with the collision above.
- The mechanism named - the one place where an identifier is reused - rather than a guess.
- The fix. The planning pass's candidate, to be confirmed or discarded against the reproduction: a repository engine should not reset its identifier generator when its catalog is rebuilt, because nothing requires its identifiers to be reproducible, only unique; its generator starts at zero when the engine is constructed, and `ClearCatalog` should leave it where it is. An engine that persists its catalog must keep resetting, for the reason `bug-object-id-counter-is-process-wide` and the comment in `InitializeCatalog` give. If that is not sufficient, the reproduction will say so.
- Whatever `AbandonedClientProcessTest.SessionCount` had to avoid because of this defect, reconsidered: its `NOTE:` records that it uses a scalar count where a cursor-based poll would have been natural.
- A decision, recorded, about the `#if !DEBUG` replace-by-name block in `CacheCatalogObject`. It is what turns this defect into silent wrongness in a shipped client rather than a visible error. Keeping it as an accepted tradeoff is a legitimate answer; leaving it undiscussed is not.

## Arm added by `engine-object-id-transient-tablevars` (2026-11-22): one mechanism measured, at the same site

`engine-object-id-transient-tablevars` stopped a schema object built from a name alone from taking an identifier, which among other things stopped a column of a row or table type from taking one. That shrank the number of identifiers an engine spends on its own start-up from several hundred to a few dozen, and it immediately turned `AbandonedClientProcessTest.AKilledClientProcessIsReclaimedAndTheServerKeepsServing` red with this ticket's exact failure: `An item with the same key has already been added` out of `CatalogDeviceSession.CacheCatalogObject`, reached from `LocalExpressionPlan.GetDataType`, with the colliding identifier small (5) rather than large (984).

That instance was traced to the end and **is not a catalog rebuild**. Instrumenting `ObjectIDGenerator.Reset`, `SetLastAllocated` and the duplicate-key branch of `CacheCatalogObject` showed, in order:

- one `Reset` only, from the first `Engine.InitializeCatalog` of the run (so no rebuild had happened yet);
- the engine numbering its own catalog from there - `.System.Temp` came out as identifier 5;
- `SETLAST from 358 to 1` - `tests/DAE.Server.Tests/TestEnvironment.WithTheObjectIDCounterRestored` putting the process-wide counter back to the value it had saved before the body, **while the `LocalServer`'s engine from inside that body was still running**;
- that engine allocating 5 again a moment later, for the operator `System.iCompare0000000005`, which collided with `.System.Temp` in `CatalogDevice.CatalogIndex`.

Two things worth carrying forward:

- **The fixture's save-and-restore was itself handing a live engine identifiers its catalog already held.** It now restores the counter only when that would raise it (`TestEnvironment.cs`), which is all it can safely do; `bug-object-id-counter-is-process-wide` deletes the workaround outright. `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs` still has the unguarded form, in a project no solution builds.
- **`Schema.Operator.Name` is generated from the operator's own identifier** (`Operator.EnsureNameReset` -> `Schema.Object.GetGeneratedName(_operatorName, ID)`). So the colliding object's *name* tells you the identifier it was given, which is how the above was read off a single line of output. If this ticket's recorded reproduction (identifier 984) names a `…0000000984` object, the same reading applies to it.

**What this does not settle.** The reproduction on record is a client over the network doing eight table reads and then a schema change, and this arm's instance needed neither the reads nor the change - so they may be two mechanisms reaching one site, or one. Rebuild the recorded reproduction first and check it against the fixture above: if it was the same rewind, the fix in `TestEnvironment.cs` has already removed it and the reproduction will no longer fail, which is itself the answer to "name the mechanism". If it still fails, the rebuild path is a second mechanism and this arm is only context.

## Note added by `bug-object-id-counter-is-process-wide` (2026-11-22): the tree this ticket reproduces against has changed

The prerequisite has landed, so three things the arm above refers to no longer exist and the reproduction has to be rebuilt against the current tree rather than against the description in this ticket:

- Every `Engine` now creates its own `Schema.ObjectIDGenerator` in its constructor. Two engines in one process can no longer reach each other's identifiers at all, so the rewind that arm measured is gone as a mechanism.
- `tests/DAE.Server.Tests/TestEnvironment.WithTheObjectIDCounterRestored` is deleted, with its four call sites, and so is the same save-and-restore in `tests/Dataphor.Benchmarks/BenchmarkServerInstance.cs` (that project *is* in `Dataphor.slnx`, contrary to what the arm said). `Schema.Object.GetNextObjectID`, `SetNextObjectID` and `GetObjectID(MetaData)` are gone, and so is `ObjectIDGenerator.SetLastAllocated`, so no instrumentation of them is possible and nothing in the tree can move a generator's counter down.
- `AbandonedClientProcessTest.AKilledClientProcessIsReclaimedAndTheServerKeepsServing` passes on the current tree, as does the whole suite.

So start from this ticket's own recorded reproduction - one client over the network, eight table reads, a schema change on the server, one more read - and run it against the current tree. If it no longer fails, the rewind was the whole mechanism and this ticket is already fixed; say so and close it with that evidence. If it still fails, the catalog rebuild path (`LocalServer.EnsureCacheConsistent` -> `Engine.ClearCatalog` -> `InitializeCatalog`, which still resets the engine's own generator) is a second mechanism, and the candidate fix this ticket already names - a repository engine not resetting its generator on a catalog rebuild - is the one to try.
