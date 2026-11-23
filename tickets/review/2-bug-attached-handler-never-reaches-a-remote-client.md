description: Attaching a change, default or validate handler to a table or one of its columns now takes effect for a user working outside the server; previously it could be ignored for the life of that user's connection. Review the fix, which turned out to need a different mechanism than the ticket described.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/RemoteEventHandlerCacheTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md, docs/migration-notes.md, tickets/backlog/bug-type-change-handler-never-runs-for-a-client.md
difficulty: medium
----
## Read this first: the implement ticket's analysis was wrong, and the fix is not the one it specified

The implement ticket said the defect was the **client's catalog copy**: that a client holds a copy of the type or table, that `Catalog.CacheTimeStamp` is the only thing that can correct it, and that incrementing that counter in the three event-handler nodes is the fix. That is not what is happening, and the `CacheTimeStamp` increment on its own fixes nothing — it was implemented first, measured, and found to leave every case still failing.

What is actually happening, measured on the running server:

- **A handler is never part of a client's copy of the catalog at all.** `Schema.EventHandler.DetermineRemotable` sets `IsRemotable = false` for every handler (the `TRIGGERSREMOTABLE` compiler constant that would make it otherwise is defined nowhere), and `Schema.TableVar.IncludeHandlers` filters non-remotable handlers out of what is emitted for a client. So no amount of refreshing the client's copy puts the handler in it. The client's copy was never the thing that was wrong.
- **What a client gets instead is a verdict, per column, computed when a plan is compiled.** `ServerProcess.CopyDataType` adorns the plan's table variable with `DAE.IsChangeRemotable`, `DAE.IsDefaultRemotable` and `DAE.IsValidateRemotable` tags, each `the work is remotable || there is no work`, and `LocalCursor.Change`/`Default`/`Validate` read them through `TableVar.Is*CallRemotable(columnName)` to decide between answering locally and calling the server. Attaching a handler flips the verdict to "call the server" — on the live catalog object.
- **The stale thing is the compiled plan.** That verdict is built at compile time and kept with the plan in the server's plan cache, which is keyed by statement text and evicted only by `Catalog.PlanCacheTimeStamp`. `CreateEventHandlerNode` and `AlterEventHandlerNode` moved that counter. So a client re-preparing a statement text that had been prepared before the attach got the pre-attach verdict back and went on answering locally.

The proof, which is also what makes the ticket's own measurement interpretable: the *same* question worded differently, so that no compiled plan existed for it, got the right answer immediately, with or without any fix. The ticket's measurement opened a new cursor on the same text and read that as a catalog-cache problem.

## What landed

`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`, three nodes, each gated on `program.ServerProcess.ShouldAffectTimeStamp`:

- `CreateEventHandlerNode` and `AlterEventHandlerNode` now bump `Catalog.PlanCacheTimeStamp` **and** `Catalog.CacheTimeStamp`, with the log line the neighbouring nodes write when they move the second.
- `DropEventHandlerNode` already bumped `PlanCacheTimeStamp` (K42); it now also bumps `CacheTimeStamp`, and its comment records the second reason for the bump it already had.
- `DerivationTimeStamp` is deliberately not moved: a handler changes no part of a form the Frontend derives. Say so if you disagree — it is a one-line change either way.

**Why both counters, when only one of them fixes this defect.** `PlanCacheTimeStamp` is the fix and is what the tests pin. `CacheTimeStamp` is for a plan a client still holds **open**, which no eviction can reach, because nothing re-sends an open plan its verdict. The sibling implement ticket `bug-remote-form-is-never-told-about-a-type-change` is designed around exactly that: the client keeps the highest `CacheTimeStamp` it has heard of, each plan carries the value it was prepared at, and that ticket teaches the three proposal calls to deny the local branch when the plan is behind. It names this ticket as its prerequisite, in those words, precisely so that attaching a handler moves that counter. Its text is unchanged and its premise holds. The cost of the second bump is that a connected client refetches its catalog copy once per attach or detach, which is a schema-definition operation, and the gate keeps it off the application-transaction path (pinned by a test).

The gate is the same one `DropEventHandlerNode` already used and for the same reason: these nodes are also how the server attaches and drops the handlers it generates on the copy an application transaction makes of each table it joins (`ApplicationTransactionDevice.AddTableVar`, `ServerSession.DropSessionObjects`), and loading a stored handler from the catalog runs `CreateEventHandlerNode` inside a time-stamp-safe context too (`ServerCatalogDeviceSession.LoadPersistentObject`).

## Measurements

All 2026-11-23, against an in-process server with the shipped client stack in front of it (`ClientStackTestFixture`: a `LocalServer` over a `RemoteServer` over the engine).

A table whose `Amount` column gains a change handler that writes 99, asked what follows from the user changing `Amount` from 1 to 2:

| query | before the fix | after |
|---|---|---|
| `select Tbl capabilities { updateable }`, prepared before the attach and again after it | client: nothing changed, 2 — engine: changed, 99 | both: changed, 99 |
| `select Tbl where ID >= 0 capabilities { updateable }`, never prepared before | both: changed, 99 | both: changed, 99 |

Counters over one attach: `PlanCacheTimeStamp` did not move before the fix and moves now; `CacheTimeStamp` moved in both arrangements once the second bump was in (it is not what corrected the answer).

An application transaction joined to a table that carries a change handler, with a row inserted inside it so the transaction's copy of the table and of the handler is actually made and then dropped: `PlanCacheTimeStamp` 1203 → 1203, `CacheTimeStamp` 8 → 8. The gate holds.

## Tests added

`tests/DAE.Server.Tests/RemoteEventHandlerCacheTest.cs`, new, on K44's `ClientStackTestFixture`; added to `DAE.Server.Tests.csproj` (that project lists its sources explicitly). Every case asks the same question of the client stack and of the engine's own cursor and asserts they agree, then names the value, as `RemoteValidateTest` does.

| case | what it verifies |
|---|---|
| `TestAttachingToAColumnReachesAClientHoldingIt` | a change handler attached to a column runs for a client that had already prepared that query. **Seen to fail** with the `CreateEventHandlerNode` bump disabled. |
| `TestAttachingToATableReachesAClientHoldingIt` | the same for a handler attached to the table variable. **Seen to fail** the same way. |
| `TestDetachingFromAColumnReachesAClientHoldingIt` | a detached handler stops running for such a client. **Passes with and without this change** — `DropEventHandlerNode`'s bump is K42's. It is here because K42 left the event-handler nodes with no test at all (its own record says so) and because this is the second, independent reason that bump has to stay. Cut it if you judge that it pays for itself only as a guard. |
| `TestAnApplicationTransactionMovesNeitherTimeStamp` | the gate: an application transaction that makes and drops its own copy of a table carrying a change handler moves neither counter. This is the case that stops a future edit from making every form edit flush every session's plans. |

No test for the `invoke` (reorder) statement, i.e. `AlterEventHandlerNode` — see the gaps below.

## Known gaps, honestly

- **The `invoke`/`AlterEventHandlerNode` arm is reasoned, not measured.** Reordering handlers changes nothing a client is told directly: the handler list is never sent, and the firing order is decided on the server, which reads the live list. What made me bump it anyway is that `CatalogDeviceSession.MoveEventHandler` calls `tableVar.DetermineRemotable`, which recomputes exactly the characteristics a cached plan description carries — and that leaving one of three sibling statements without invalidation is the shape of this defect. I could not construct a case where a reorder alone changes a client's answer. **This is the arm to challenge**: either find such a case, or decide the bump is unjustified and take it out with a `NOTE:` saying why.
- **A handler attached to a scalar *type* is still not reached, and is not fixed here.** This is a different defect with a different cause, verified during this work and filed as `tickets/backlog/bug-type-change-handler-never-runs-for-a-client.md`: `Schema.TableVarColumn.SetIsChangeRemotable(ScalarType dataType)` walks the column's own handlers instead of the type's (a copy-paste slip — its `SetIsValidateRemotable(ScalarType)` and `SetIsDefaultRemotable()` siblings read the type correctly), so the column stays marked change-remotable and the server tells the client to answer locally, where there is no handler. Measured in the most favourable arrangement there is — the handler attached before the table existed, the client's first-ever query — client said "nothing changed, 2" where the engine said "changed, 99". The implement ticket asked for a case per kind of event source and said "a kind that turns out already to be corrected by something else is worth one sentence in the handoff"; this kind is not corrected by anything, which is why it is a ticket and not a sentence. Filed fresh rather than appended to `bug-remote-form-is-never-told-about-a-type-change` (the one open ticket the site-claim grep found mentioning `IsChangeRemotable`) because that ticket's root-cause site is `LocalCursor`, not `TableVar`, and its fix covers only a plan prepared *before* the change; the measured case is a plan prepared after it. That sibling implement ticket is deliberately left untouched.
- **A test can pass without exercising the client's local branch.** Each case asserts the client's answer against the engine's, so a fix that moved the work to the server rather than running it on the client would also pass. That is intended — there is no reference runtime, and "the client stack agrees with the engine" is the property that matters — but it means these cases do not pin *where* the handler ran. The two attach cases were each run against the unfixed engine to confirm they do fail, which is the check that keeps them honest.
- **No out-of-process case.** Everything here is above the transport, which is what `ClientStackTestFixture` exists for; the protocol layer only marshals the calls `RemoteServerCursor` and `LocalCursor` already make.

## Tests run

Build: `dotnet build Dataphor.slnx` — 0 errors.

- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~RemoteEventHandlerCacheTest|FullyQualifiedName~RemoteValidateTest|FullyQualifiedName~SchemaCacheInvalidationTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~TypeChangeSummaryTest|FullyQualifiedName~ApplicationTransactionTest"` — 130 passed, 0 failed. (`SchemaCacheInvalidationTest` is where K42's plan-cache cases live; `CatalogRollbackSnapshotTest` has the attach/detach rollback snapshots; `TypeChangeSummaryTest` is K43's.)
- `dotnet test tests/DAE.Server.Tests --filter "Name~EventHandler|Name~ApplicationTransaction"` — 17 passed, 0 failed: the `EventHandlers`, `SerializationEventHandlers`, `ApplicationTransactions` and `TestNetworkApplicationTransactions` coverage scripts, plus the fixtures whose method names match.

No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.

## Docs changed

- `docs/modernization-plan.md`: new known-defect record **K45**, and K42's "left alone deliberately" clause amended — it listed the attach and `invoke` of an event handler as deliberately not invalidating the plan cache, on the ground that firing reads the live handler list. That ground is sound for the engine and says nothing about a client; the clause now says so and points at K45.
- `docs/migration-notes.md`, *D4, catalog and libraries*: one entry, written for someone moving an application — what now takes effect without reopening a form, that a handler on a data type still does not, and that nothing is required of them.

## What to look at

- The `invoke` arm (above). It is the one change here I would not defend hard.
- Whether `CacheTimeStamp` should be moved at all before `bug-remote-form-is-never-told-about-a-type-change` lands to consume it. The alternative is to move only `PlanCacheTimeStamp` now and let that ticket add the second bump when it adds the reader. I kept both because the counter's stated meaning is "a change a client could care about", this is such a change, and the sibling ticket names this one as its prerequisite for exactly this reason — but a reviewer could reasonably want the unused half deferred.
- Whether `DerivationTimeStamp` should move too. I believe not; the neighbouring `alter`/`drop` nodes move all three together, so the asymmetry is worth a second opinion.
- The comment volume in `DDLNodes.cs`. Three bumps carry a lot of prose, because the two counters, the gate and the reason K42's declined arm is not this one all have to survive the next reader. Trim if you think it has tipped over.
