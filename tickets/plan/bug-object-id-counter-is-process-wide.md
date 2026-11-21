description: When one program holds two Dataphor engines at once, the second one to start resets a shared counter, so the first one then reuses catalog identifiers it has already handed out and schema changes start failing or overwriting each other.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Server/Engine.cs, src/protocol/Client/Server/LocalServer.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/RenameNode.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Schema/TableVar.cs, tests/DAE.Server.Tests/ProtocolClient.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/protocol/Client/Server/LocalPlan.cs
repro: verified
severity: corruption
likelihood: normal-use
tradeoffs: The fix touches about fifteen call sites across the compiler and the plan nodes and has no local form, and the product has been shipping over the one-engine half of this for years without a report, because a release build quietly replaces the colliding object instead of failing - so a maintainer may reasonably keep the test workaround and the release-build tolerance until something traceable goes wrong.
----
## What goes wrong

Every catalog object the engine creates gets an integer identifier from one counter. That counter is a single variable shared by the whole program (`Schema.Object.GetNextObjectID`, a `static int` in `src/engine/DAE/Schema/Schema.cs`), and starting an engine sets it back to zero (`Engine.InitializeCatalog` calls `Schema.Object.SetNextObjectID(0)`, with the comment "this must be the first object created to avoid the ID being different on subsequent loads").

So when a program starts a second engine while a first one is still running, the second one's reset hands the first one identifiers it has already used. The next schema change in the first engine tries to write a catalog row whose identifier is already there, and the persistent catalog refuses it:

```
UNIQUE constraint failed: DAEObjects.ID
```

That is the visible failure. The quieter one is that two different objects can end up carrying the same identifier in memory before anything is written.

## How a program ends up with two engines

A client connection holds one. `LocalServer` — the client-side half of every network connection — starts an engine of its own to hold the catalog cache it compiles against. So:

- **Reproduced**: a test (or any program) that runs an in-process server and then opens a network connection to it. `tests/DAE.Server.Tests/CoreWcfProtocolTest.cs` does exactly this, and failed with the error above on its first `create table` until it worked around the problem (see below).
- **Reachable in a shipped configuration**: the IDE lets a user open several connections at once, and an embedded in-process server alongside a connection to a remote server is two engines in one process. A server-to-server link is *not* affected: it goes through `NativeCLISession`, which starts no engine.
- Out of process — a client program and a server program, which is how the product normally runs — there is one engine per process and nothing goes wrong. That is why this has survived.

## A second place the counter is reset, which the workaround cannot wrap (from `test-client-server-cursors`)

The workaround below wraps the *construction* of a client-side server object, because that is where its engine starts. It is not the only place a client-side engine re-initializes its catalog and so resets the counter: when the server's catalog has moved on since the client last looked, the client throws its cached catalog away and rebuilds it (`LocalServer.EnsureCacheConsistent` calls `Engine.ClearCatalog`, which calls the same `InitializeCatalog`), and that happens in the middle of preparing an expression, where no caller is in a position to save and restore anything.

Harmless as the tests stand, and measured: the two out-of-process cases that run two client connections against one server pass whether or not the workaround is applied, because each connection clears its own cached catalog before it allocates anything into it. Removing the workaround fails three `CoreWcfProtocolTest` cases instead, with `UNIQUE constraint failed: DAEObjects.ID` — that fixture hosts a server in the test's own process and then connects to it, which is the arrangement the workaround was written for. The arrangement that would break is a program where one client-side catalog allocates identifiers *after* another client-side engine has reset the counter; nothing does that today, so there is a `NOTE:` at the workaround saying so.

Two consequences for this ticket's *done* condition: the workaround moved from `CoreWcfProtocolTest.cs` to `tests/DAE.Server.Tests/ProtocolClient.cs` (one copy, used by every protocol fixture), and removing it is not enough — the cache-rebuild path has to allocate from its own engine's counter too, or the same collision returns by a route no test wraps.

## Arm (from `test-client-server-abandoned`): the cache-rebuild path is **not** harmless, and one engine is enough

The section above says the cache-rebuild reset is "harmless as the tests stand". It is not. It breaks with a **single** client-side engine in the process, after a handful of ordinary reads, and no second engine is involved:

- Reproduced (verified, deterministic, Debug build, 2026-11-21): one client connected over the network to a server in another process reads a table eight times, then changes the server's catalog, then reads the table once more. The last read fails with error 106185, "Errors occurred while deserializing client cache time stamp", wrapping `An item with the same key has already been added. Key: 984` from `CatalogDeviceSession.CacheCatalogObject` (`Device.CatalogIndex.Add`, `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`), reached from `LocalExpressionPlan.GetDataType` (`src/protocol/Client/Server/LocalPlan.cs`). The colliding identifier (984) and the number of reads needed (8) were the same on every run; the process-wide counter stood at 997 at the failure.
- Why reading a table is what trips it: a table-valued prepare makes the server send a catalog fragment that the client applies as a view in its own catalog, and the client drops that view when the plan is disposed - so each read allocates another identifier and the counter climbs. A change to the server's catalog then makes the next prepare take the rebuild path (`LocalServer.EnsureCacheConsistent` → `Engine.ClearCatalog` → `InitializeCatalog` → `SetNextObjectID(0)`), and the fragment applied after the rebuild carries an identifier the rebuilt catalog already holds. A scalar prepare (`Count(...)`) sends no fragment and never trips it.
- **Release builds do not fail; they silently replace.** The `#if !DEBUG` block at the top of `CacheCatalogObject` removes any object already in the cache *by name* before adding, which is what lets the identifier collision through without an error ("it must be there as a result of some error and the best course of action in a production scenario is to just replace it"). So in a shipped client this is a silent wrong state in the client-side catalog cache rather than a visible failure, and in a Debug build of the same client it is an exception. That asymmetry is why it has not been seen.
- Where it bites in the product: any client that holds a connection across changes to the server's catalog and reads tables through it - which is what the Windows client, Dataphoria and `DataView` do continuously.
- Consequence for this ticket's *done* condition: a reproduction that needs only one engine belongs in the suite, and "the three-line workaround is removed" is not sufficient on its own - the rebuild path has to allocate from its own engine's counter, which the section above already says.
- Consequence for the suite today: no existing test does enough table reads to trip it. `test-client-server-abandoned` met it with a cursor-based poll and moved that case to a scalar count rather than build on the defect; the reason is recorded at the site (`AbandonedClientProcessTest.SessionCount`).

## The workaround now in the tree, which is not the fix

The shared client helper of the protocol tests (`tests/DAE.Server.Tests/ProtocolClient.cs`, `TestEnvironment.WithTheObjectIDCounterRestored`) reads the counter before it builds its `LocalServer` and writes the value back afterwards, which leaves both engines allocating increasing identifiers. It is three lines with a comment pointing here. Anything else that needs an engine beside an engine will need the same three lines, which is the argument for fixing the cause: the out-of-process layer inherited it rather than rediscovering it, and the section above says where it is not enough.

## Direction, when this is promoted

The identifier allocator belongs to an engine, not to the program. The question to settle is how the roughly fifteen call sites reach it, because most of them are deep in the compiler and the plan nodes and several are in object constructors that have no engine in hand:

- `Schema.Object`'s own constructor (`Schema.cs`), `ResultTableVar` (`TableVar.cs`), `Compiler` (five sites), `TableNode`, `RenameNode`, `DDLNodes`, `Engine` itself.
- The obvious shapes are: an allocator object reached from the `Plan`/`Program`/`ValueManager` already threaded through the compiler; or an ambient per-engine context. Whichever is chosen, the goal is that *no* code path can read a counter belonging to another engine — the state should not be reachable, rather than merely not reset.
- `Engine.InitializeCatalog`'s reset-to-zero has a reason (identifiers must come out the same on every load of the same catalog, so the built-in objects must number identically); keep that property per engine.
- Done means two engines can run in one process, create objects in both, and neither sees the other's identifiers; and the three-line workaround in `CoreWcfProtocolTest` is removed.
