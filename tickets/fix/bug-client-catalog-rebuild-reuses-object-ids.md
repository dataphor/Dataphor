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
