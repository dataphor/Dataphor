description: When one program holds two Dataphor engines at once, the second one to start resets a shared counter, so the first one then reuses catalog identifiers it has already handed out and schema changes start failing or overwriting each other.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Schema.cs, src/engine/DAE/Server/Engine.cs, src/protocol/Client/Server/LocalServer.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/RenameNode.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Schema/TableVar.cs, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs
repro: verified
severity: corruption
likelihood: unusual
tradeoffs: Only a process that runs two engines at once is affected, which no shipped host does on purpose, so a maintainer may reasonably decide the one-line workaround the tests use is enough until a real deployment hits it; the proper fix touches about fifteen call sites across the compiler and the plan nodes and has no local form.
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

## The workaround now in the tree, which is not the fix

`CoreWcfProtocolTest`'s client helper reads the counter before it builds its `LocalServer` and writes the value back afterwards, which leaves both engines allocating increasing identifiers. It is three lines with a comment pointing here. Anything else that needs an engine beside an engine will need the same three lines, which is the argument for fixing the cause: the next ticket that needs it (`test-client-server` is the obvious one) should not have to rediscover it.

## Direction, when this is promoted

The identifier allocator belongs to an engine, not to the program. The question to settle is how the roughly fifteen call sites reach it, because most of them are deep in the compiler and the plan nodes and several are in object constructors that have no engine in hand:

- `Schema.Object`'s own constructor (`Schema.cs`), `ResultTableVar` (`TableVar.cs`), `Compiler` (five sites), `TableNode`, `RenameNode`, `DDLNodes`, `Engine` itself.
- The obvious shapes are: an allocator object reached from the `Plan`/`Program`/`ValueManager` already threaded through the compiler; or an ambient per-engine context. Whichever is chosen, the goal is that *no* code path can read a counter belonging to another engine — the state should not be reachable, rather than merely not reset.
- `Engine.InitializeCatalog`'s reset-to-zero has a reason (identifiers must come out the same on every load of the same catalog, so the built-in objects must number identically); keep that property per engine.
- Done means two engines can run in one process, create objects in both, and neither sees the other's identifiers; and the three-line workaround in `CoreWcfProtocolTest` is removed.
