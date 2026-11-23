description: Attaching a handler to a data type or a table has no effect at all for a user working outside the server — not even in a form opened after the handler was attached — because nothing tells that user's client that its copy of the type or table is out of date.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/Server/Server/CatalogCache.cs, src/protocol/Client/Server/LocalServer.cs, tests/DAE.Server.Tests/
difficulty: medium
repro: verified
----
## What is wrong

A client that runs outside the server keeps its own copy of the part of the catalog its plans refer to, and evaluates what it can against that copy rather than asking the server. The copy is kept in step by a counter: `Schema.Catalog.CacheTimeStamp` is incremented by every statement that changes an object a client may be holding, each prepared plan carries the counter's value back to the client, and a client whose value is behind throws its whole copy away and takes a fresh one (`CatalogCache.EnsureConsistent` on the server, `LocalServer.EnsureCacheConsistent` on the client). Between those rebuilds a session is sent only the objects it does not already hold, matched **by name** (`CatalogCaches.GetRequiredObjects`) — so an object the client already has is never re-sent, and the counter is the only thing that can correct it.

`attach operator`, `alter` and `detach operator` do not increment that counter. `CreateEventHandlerNode.InternalExecute` and `AlterEventHandlerNode.InternalExecute` increment nothing; `DropEventHandlerNode.InternalExecute` increments only the *plan* cache counter, which coordinates the server's own memo of compiled statements and has nothing to do with clients. So a client that already holds a type or a table goes on holding the version without the handler, for the life of the connection, and the handler never runs for that user — not in the form that was open when the handler was attached, and not in a form opened afterwards either.

This is not the arm K42 declined. K42 deliberately left the event-handler nodes without a `PlanCacheTimeStamp` increment, on the ground that firing a handler reads the live handler list of the table variable, column or type as the statement runs. That is true of the engine, and it is why no plan the engine has compiled needs evicting. It says nothing about a client, which does not read the live list — it reads a copy. Different counter, different cache, different reason.

## What it does today, measured

Measured 2026-11-23 against an in-process server with the shipped client stack in front of it (a `LocalServer` over a `RemoteServer` over the engine).

Which statements move which counter, read off the running server:

| statement | `CacheTimeStamp` | `ShouldCallProposablesTimeStamp` |
|---|---|---|
| `alter type T { create constraint ... }` | 8 → 9 | moved |
| `alter type T { create default ... }` | 9 → 10 | moved |
| `attach operator ... to T on { change }` | **10 → 10** | moved |

And what that costs a user. A type `FreshT`, a table `FreshTbl` with a column of it, and a change handler that puts 99 in the column. The client opened and closed a cursor on the table first, so that it holds a copy of the type; then the handler was attached; then a **new** cursor was opened and asked what follows from the user changing the column from 1 to 2:

```
Amount after Change through a cursor opened AFTER the attach: 2
Amount after Change through the engine's own cursor:          99
```

The handler did not run for the client, in a cursor opened after it was attached. Through the engine it ran. Had the client not cached the type beforehand it would have been sent the version with the handler and would have behaved correctly, which is why this is invisible to anything that connects, does one thing and disconnects.

The same question for a constraint rather than a handler comes out right, because `alter type { create constraint }` does move the counter: `Validate` through a cursor opened after that statement raised `Violation of constraint "IsPositive" defined on scalar type "General.FreshCT"`, as it should.

## What to do

Increment `Catalog.CacheTimeStamp` in the three event-handler nodes, and gate each increment on `program.ServerProcess.ShouldAffectTimeStamp`, which is how `DropEventHandlerNode` already gates its plan-cache increment and for the same reason: that node is also how the server drops the handlers it generates on its own copies of a table — at the end of every application transaction, and at the disconnect of every session holding session objects — and both of those run inside a context that exists to keep such internal work from moving the coordination counters (`ServerSession.DropSessionObjects`). Without the gate, every application transaction a form opens would make every connected client throw its catalog copy away, which costs far more than it corrects.

The log line the neighbouring nodes write when they move this counter (`DDLNodes.cs:1814`, `:2406`) is the pattern to follow for the new ones, so that a deployment diagnosing cache churn can see which statement caused it.

Handlers attach to three kinds of thing — a data type, a table variable, and a column of a table variable — and the reasoning above does not distinguish them: in each case the client holds a copy of an object it will not be re-sent. Check all three and keep the cases that reproduce; a kind that turns out already to be corrected by something else is worth one sentence in the handoff rather than a test.

### TODO

- Increment `Catalog.CacheTimeStamp`, gated on `ShouldAffectTimeStamp`, in `CreateEventHandlerNode`, `AlterEventHandlerNode` and `DropEventHandlerNode`, with the log line the neighbouring nodes write.
- Write the reproduction on the shared client-stack fixture that `bug-remote-validate-validates-the-wrong-row` adds: the client caches the object, a handler is attached on a session of the engine's own, and a cursor opened **after** the attach must behave as the engine's own cursor does. One case per kind of thing a handler attaches to that actually reproduces.
- Check what the gate excludes, with a test or with a measurement named in the handoff: an application transaction opened and ended on a table must not move `CacheTimeStamp`, or every client rebuilds its catalog copy on every form edit. `ReportWhichTimeStampsMove`-style instrumentation on the running server's `Catalog` is enough — `BeginApplicationTransaction` alone moved neither counter in the measurement above, but that call joins no table, so the case has to reach a table inside the transaction for the measurement to mean anything.
- Add an entry to `docs/migration-notes.md`: attaching or detaching a handler now invalidates connected clients' catalog copies, so a client sees the handler without reconnecting, and a deployment that attaches handlers in bulk will see those clients refetch the catalog.
- Add the record to the known-defects table in `docs/modernization-plan.md` and name this slug in it. Say in that record why this is not K42's declined arm, so the next reader does not have to re-derive it.
- Targeted tests: the new cases, plus `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~SchemaCacheInvalidationTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~TypeChangeSummaryTest"` and the coverage scripts for event handlers and application transactions (`--filter "Name~EventHandler|Name~ApplicationTransaction"`). Name the filters in the handoff.
