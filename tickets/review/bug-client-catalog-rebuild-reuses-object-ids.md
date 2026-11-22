description: A program connected to a Dataphor server keeps a local copy of the server's schema and rebuilds it from scratch when the server's schema changes. It used to number the rebuilt copy from one again, so a rebuilt item could be given the same identifier as an item the program was still using, and discarding the old item then made the new one unfindable. The rebuilt copy now carries on numbering from where it left off.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/Engine.cs, src/engine/DAE/Schema/ObjectIDGenerator.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/ObjectIDPerEngineTest.cs, tests/DAE.Server.Tests/AbandonedClientProcessTest.cs, docs/modernization-plan.md
----
## What landed

One behavioural change, three comment records, one test added, one test restored, one plan sentence.

**The fix.** `Engine.InitializeCatalog` (`src/engine/DAE/Server/Engine.cs:1241`) now guards the generator reset with `if (!IsEngine)`. `IsEngine` is true for a repository engine - a client's catalog cache, started by `LocalServer` with no catalog store - and `Server` overrides it to false, so a full server still resets and a repository engine no longer does. The comment above it states the settled rule in both directions and no longer defers to this ticket: an engine with a store resets because the built-in objects it creates before reading the store must land on the identifiers the store already holds; a repository engine must not, because `ClearCatalog` rebuilds its catalog while the objects of the catalog being replaced are still in its clients' hands.

**`src/engine/DAE/Schema/ObjectIDGenerator.cs`**: the class remarks now say which kind of engine calls `Reset` and why the other kind does not, and `Reset` itself carries a one-line remark pointing at them. No code change.

**Two `NOTE:` records, both at their sites in `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`:**

- A **tripwire** at `ClearCatalogObject` (line ~1941): the identifier-to-name index entry is removed by identifier without checking that the entry still names the object being cleared. That is sound only while no engine reissues an identifier its catalog already holds - the rule `InitializeCatalog` now keeps for a repository engine too - and it records that nothing renames a catalog object in place today (there is no rename path in that class), which is what would make a future check possible.
- An **accepted tradeoff** at the `#if !DEBUG` block in `CacheCatalogObject` (line ~1915): the release-only replace-by-name is kept. Turning it into a throw was declined because it is last-ditch recovery for a cache that is already wrong, so it would turn a degraded client into a dead one with no upside, and a release client that started failing a user's form where it used to limp is a behaviour change applications could notice. Revisit condition stated: if the catalog cache ever gains a consistency check that fails the same way in both configurations.

**`docs/modernization-plan.md`**: one sentence appended to the WS2 paragraph about the process-wide counter (line 461), recording that the reset half is settled too.

## Tests

| Test | What it verifies |
|---|---|
| `ObjectIDPerEngineTest.RebuildingAnEnginesCatalogDoesNotReissueTheIdentifiersItHasAlreadyGivenOut` (`tests/DAE.Server.Tests/ObjectIDPerEngineTest.cs`) | A bare `Engine`, started the way `LocalServer` starts its catalog cache, creates a table, calls `ClearCatalog()`, creates a second table; the two tables do not share an identifier. **Verified to fail without the guard** - both tables get identifier 359, the number the ticket recorded - and to pass with it. 342 ms. |

The fixture's own summary was widened: it spoke only of a second engine's start, and now names both ways an engine's identifiers used to be handed out twice.

No test was added for the eviction itself - a stale object's drop removing a live object's index entry. That is the consequence the identifier collision has, not a separate behaviour, and reaching it needs a view held across a rebuild in a client over the wire: an out-of-process fixture costing seconds to say what the 342 ms case says.

`AbandonedClientProcessTest.SessionCount` is back to a cursor over `System.Sessions`, which is the natural form, and its `NOTE:` deferring to this ticket is gone.

**Correction to the ticket on that last point, and the main thing for a reviewer to weigh.** The ticket said that case "drives a real client-over-the-wire rebuild, which is where the suite gets its coverage of that path". Measured both ways:

- It *does* drive the rebuild. Probed with a temporary `Console.WriteLine` at `LocalServer.EnsureCacheConsistent`'s `ClearCatalog()` call: exactly one client-side rebuild per run of the case. The probe has been removed - `LocalServer.cs` is unmodified in this diff.
- It does **not** detect this defect. With the guard removed it still passes. Each poll closes its cursor before the next one opens, so no view from before the rebuild is still held when the rebuilt catalog is numbered, and the collision needs one that is.

So the restored form buys real path coverage but is not a reproduction, and the `SessionCount` remarks now say that plainly instead of implying otherwise. **The suite has no over-the-wire reproduction of this defect** - the engine-layer case is the whole of it. A reviewer who wants one would need a client that holds prepared table expressions across a server schema change, which is the eight-reads-then-alter shape the ticket's own measurement used; that is a new out-of-process fixture, not an edit to this one.

## Validation run

- `dotnet build Dataphor.slnx`: 0 errors, 143 warnings (the tree's standing set).
- The three suite parts from `AGENTS.md`, each in the foreground: part 1 **667 passed, 0 failed, 33 skipped** (6 m 25 s); part 2 **67 passed, 0 failed, 11 skipped** (3 m 1 s); part 3 **50 passed, 0 failed, 12 skipped** (3 m 3 s). Part 1's time is inside the band `AGENTS.md` records for it (5 m 20 s to 7 m 32 s). No failures surfaced, so no `.pre-existing-error.md` was written.
- `tools/net-spike/run.py` was **not** run, deliberately: it builds the converted projects, which `dotnet build Dataphor.slnx` already covers at 0 errors, and then runs an in-process `Server`, which overrides `IsEngine` to false and so takes the unchanged branch. It would exercise nothing this change touches.
- No Release build was run. Nothing here is configuration-dependent except the `#if !DEBUG` block, which is unchanged.

## Migration notes

**No entry was added**, per the ticket: the change alters no public type or member, no D4 semantics, no registered class name, no error code, no file format and no contract shape.

Flagging the one argument against that, so the reviewer makes the call rather than discovering it: a custom C# host that embeds a bare `Engine` and calls `ClearCatalog()` would now see object identifiers continue instead of restarting at one. That was never a documented property, it is not reproducible in any useful sense (a repository engine persists no identifier and sends none to the server), and it is the defect being fixed - but it is an observable difference in the effect of a public method.

## What to exercise

- `dotnet test tests/DAE.Server.Tests --filter Name=RebuildingAnEnginesCatalogDoesNotReissueTheIdentifiersItHasAlreadyGivenOut`, and confirm it fails if `if (!IsEngine)` is removed from `Engine.InitializeCatalog`.
- `dotnet test tests/DAE.Server.Tests --filter Name=AKilledClientProcessIsReclaimedAndTheServerKeepsServing` - the restored cursor read, about 12 s.
- Whether the guard belongs on `IsEngine` at all. It is the predicate the ticket named and the only thing separating the two kinds of engine today, but it reads as "is a definition repository", not "has no catalog store", and `Server` is the only subclass of `Engine` in the tree. If a reviewer thinks the condition should say "has no catalog store" directly, this is where to make that call.
- The two `NOTE:` records, as records: whether the tripwire at `ClearCatalogObject` states its condition sharply enough for a future reader to act on, and whether the accepted tradeoff at `#if !DEBUG` is the decision a reviewer would have made. The ticket said that disagreeing with the tradeoff is a compatibility-surface call for `blocked/` with proposed text, not an inline decision.
