description: A program connected to a Dataphor server keeps a local copy of the server's schema, and when the server's schema changes it rebuilds that copy from scratch - numbering the rebuilt copy from one again, so a newly rebuilt item can end up with the same identifier as an item the program is still using.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Schema/ObjectIDGenerator.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/protocol/Client/Server/LocalServer.cs, src/protocol/Client/Server/LocalPlan.cs, tests/DAE.Server.Tests/ObjectIDPerEngineTest.cs, tests/DAE.Server.Tests/AbandonedClientProcessTest.cs, docs/modernization-plan.md
repro: verified
----
## What goes wrong, as measured on the current tree

A client's local copy of the server's schema lives in an engine of its own - a *repository* engine, one with no catalog store, started by `LocalServer`. When the server's schema moves on, the client throws that copy away and rebuilds it: `LocalServer.EnsureCacheConsistent` -> `Engine.ClearCatalog` -> `Engine.InitializeCatalog`, which calls `ObjectIDGenerator.Reset()` (`src/engine/DAE/Server/Engine.cs:1238`). The generator restarts at zero, so the rebuilt copy is numbered from one again - and it reissues exactly the identifiers the copy it replaced had used.

That matters because objects from before the rebuild stay alive: every prepared table expression the client still holds owns a view in that copy, and the client drops that view when the plan is unprepared (`LocalExpressionPlan.DropDataType`, `src/protocol/Client/Server/LocalPlan.cs:212`). Dropping it runs `CatalogDeviceSession.DeleteCatalogObject` -> `ClearCatalogObject`, which removes the identifier from the catalog device's identifier-to-name index unconditionally (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:1932`). If a rebuilt object now holds that identifier, the drop of the *stale* object evicts the *live* object's index entry. The live object stays in the catalog but can no longer be found by its identifier: `ResolveCatalogObject(int)` and `ResolveCachedCatalogObject(int)` return null for it (or throw `ObjectNotCached` where the caller insists), and `MarkViewForRecompile(int)` silently does nothing for it, so a view whose references should be re-inferred never gets marked.

Measured, deterministically, Debug build, 2026-11-22, by instrumenting `ClearCatalogObject` to report when the index entry it removes names a different object. One client over the network to a server in another process: prepare eight table expressions and hold them, change the server's schema, prepare eight more (the first of those takes the rebuild path), then unprepare the first eight. Eight mismatched removals, at identifiers 382, 392, 402, 412, 422, 432, 442 and 452 - every rebuilt view's entry evicted by the stale view that had been given the same number. The generator stood at 460 before the rebuild and at 382 again afterwards.

The same thing shows with no network and no client at all, which is where it belongs in the suite: start a bare `Engine`, create a table, call `ClearCatalog`, create another table. Both tables get identifier 359. (Measured on the current tree; 350 ms.)

## The recorded reproduction no longer fails, and why

The reproduction this ticket carried - one client over the network, eight table reads, a schema change, one more read - was rebuilt against the current tree and **passes**, including an amplified form that runs six full rounds of eight reads and a schema change. The generator resets cleanly from 460 (then 470) to zero on each of the six rebuilds and nothing collides, and no duplicate-identifier error appears. So the `An item with the same key has already been added. Key: 984` failure on record was the process-wide counter rewind that `bug-object-id-counter-is-process-wide` has since deleted, exactly as that ticket's note predicted.

Two things learned while rebuilding it, worth keeping:

- **`create table` does not move the server's catalog cache timestamp**, so it does not make a client rebuild. Only the alter and drop nodes do (`AlterTableNode`, `AlterViewNode`, `AlterScalarTypeNode`, `AlterOperatorNode`, `AlterAggregateOperatorNode`, `DropTableNode`, `DropViewNode`, `DropScalarTypeNode`, `DropOperatorNode`, `DropReferenceNode` in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs`), and so does rolling back a transaction that changed the catalog (`CatalogDeviceSession.InvalidateCachesAfterUndo`). The original record's "changes the server's catalog" has to be one of those; a bare `create table` is not enough to take the client down the rebuild path.
- **The server tells the client its cache changed on every table-valued prepare** (`RemoteServerPlan.GetCatalog` sets `cacheChanged = true` unconditionally, `src/engine/Server/Server/RemoteServerPlan.cs:251`). The client then calls `EnsureCacheConsistent`, which rebuilds only when the server's timestamp has actually moved past the one the client holds. So the rebuild is common in ordinary use, not a rare path.

## Expected behaviour

Two different things in one engine's catalog never carry the same identifier, whatever the engine does to its catalog in between. A client that holds a connection across any number of schema changes on the server, and reads tables through it, keeps working, and the objects in its copy of the schema stay findable by their identifiers.

## The fix

A repository engine must not restart its identifier generator when its catalog is rebuilt. Nothing requires a repository engine's identifiers to be reproducible - it has no catalog store, it never persists an identifier and it never sends one to the server (confirmed: the plan descriptor the client sends back carries a `Guid` plan handle, not a catalog identifier) - only that they are unique. An engine that *does* persist its catalog must keep resetting, because it creates a handful of built-in objects programmatically before it reads its store and those must land on the identifiers the store already holds for them; that is the reason `ObjectIDGenerator.Reset` exists and the reason `Engine.InitializeCatalog` calls it at a fixed point.

`Engine.IsEngine` is the predicate that already separates the two: it is true for a repository engine and `Server` overrides it to false. So guard the reset with it:

```csharp
if (!IsEngine)
    ObjectIDGenerator.Reset();
```

Verified: with that guard the three reproductions above all pass and no mismatched removal occurs; without it the engine-layer one fails with both tables on identifier 359.

Two existing comments are the specification of this decision and must be brought in line rather than left contradicting it: the `NOTE:` above the reset in `Engine.InitializeCatalog`, which defers the question to this ticket by name, and the remark in `ObjectIDGenerator` about why `Reset` exists.

## The `#if !DEBUG` block, and the decision to record

`CatalogDeviceSession.CacheCatalogObject` removes any object already in the catalog *by name* before adding, but only in release builds. That is what turns an identifier collision into a silently wrong local copy of the schema in a shipped client, and into an exception in a Debug build of the same client - the asymmetry that kept this defect unseen.

**Keep it, and record it at the site as an accepted tradeoff.** It is a last-ditch recovery for a cache that is already wrong; now that the cause is fixed, turning it into a throw would convert a degraded client into a dead one with no upside, and a release client that starts failing a user's form where it used to limp is a behaviour change applications could notice. State in the `NOTE:` what was declined, that Debug and release deliberately diverge here, and the revisit condition: revisit if the catalog cache ever gains a consistency check that fails the same way in both configurations. If you disagree, that is a compatibility-surface call and belongs in `blocked/` with the proposed text, not an inline decision.

## TODO

- Guard the reset in `Engine.InitializeCatalog` with `!IsEngine`, and rewrite the `NOTE:` above it to state the settled rule (an engine that persists its catalog resets; a repository engine does not, because its identifiers need only be unique) instead of deferring to this ticket. Bring the `Reset` remark in `src/engine/DAE/Schema/ObjectIDGenerator.cs` in line with it.
- Add the reproduction to `tests/DAE.Server.Tests/ObjectIDPerEngineTest.cs`, beside the case that already pins one engine's identifiers against another's: start a bare `Engine` as that fixture does, create a table, `ClearCatalog()`, create a second table, and assert the two tables do not share an identifier. This is the lowest layer that shows the behaviour, and it runs in well under a second; widen the fixture's own summary, which today speaks only of a second engine, to cover both ways an engine's identifiers can be reissued. Confirm it fails with the guard removed before you keep it.
- Record a tripwire `NOTE:` at `ClearCatalogObject` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`): the index entry is removed by identifier without checking that it belongs to the object being cleared, which is fine while no engine reissues identifiers, and silently evicts a live object's entry if one ever does. Nothing renames a catalog object in place today (there is no rename path in `CatalogDeviceSession`), so the index's name always matches its object - say that, since it is what makes a future check possible.
- Record the `#if !DEBUG` decision above as an accepted-tradeoff `NOTE:` at the block in `CacheCatalogObject`.
- Restore `AbandonedClientProcessTest.SessionCount` to the cursor-based read its `NOTE:` says was the natural form, and delete that `NOTE:`. That case rolls back a transaction that changed the catalog, so it drives a real client-over-the-wire rebuild, which is where the suite gets its coverage of that path without a new out-of-process fixture. Keep the diagnostic message about a reclaimed observer - it is about the idle threshold, not about this defect.
- Append a sentence to the WS2 record in `docs/modernization-plan.md` that mentions this defect's sibling (the paragraph about the process-wide counter, around line 461): the catalog-rebuild half is closed too, and a repository engine no longer restarts its generator.
- No migration-notes entry is needed unless you change the `#if !DEBUG` block or an error code: the fix changes no public type or member, no D4 semantics and no contract shape. Say so in the handoff either way.
- Build, then run the suite in the three parts the root `AGENTS.md` names, each as its own foreground command.
