description: A form already open outside the server still does not notice when a data type gains a check, a default or a change handler; once that client has heard from the server about anything else, it should stop answering those questions itself and ask the server instead.
prereq: bug-remote-validate-validates-the-wrong-row, bug-attached-handler-never-reaches-a-remote-client
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/protocol/Client/Server/LocalCursor.cs, src/protocol/Client/Server/LocalPlan.cs, src/protocol/Client/Server/LocalServer.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Schema/Catalog.cs, docs/modernization-plan.md
difficulty: hard
repro: verified
----
## What is wrong

While a user edits a row, the client asks three questions about it — what should this column default to, what follows from the user having changed this column, is this row acceptable. To avoid a round trip for the many rows where the answer is "nothing", the server tells the client once, when a plan is prepared, which of the three it may answer out of its own copy of the schema. A form holds its plan for as long as the user keeps the form open, and nothing re-sends that permission or the copy it is exercised against. So a change to a data type made after the form opened is invisible to the form: the user still meets the error on saving rather than on leaving the field.

Concretely, at prepare time `ServerProcess.CopyDataType` adorns the plan's table variable with `DAE.IsChangeRemotable`, `DAE.IsDefaultRemotable` and `DAE.IsValidateRemotable`, each computed as "the work is remotable, **or** there is no work" — so a type with no check at all yields `DAE.IsValidateRemotable = true`. The client receives that in the plan descriptor and `LocalCursor.Validate` takes its local branch (`TableVar.IsValidateCallRemotable(columnName)`), running the proposal against its own copy of the schema and never calling the server. `bug-rolled-back-type-constraint-stops-the-column-check` (K43) made the server recompute its own summary whenever a catalog change may have invalidated it, so a form hosted *inside* the server now sees a type change at once. The client's permission and copy were not part of that change.

Nothing is stored wrong: the insert and the update check the constraint by a path that consults none of this. What is lost is the early report, which is the whole point of the three questions.

## What it does today, measured

Measured 2026-11-23: the seven cases of `tests/DAE.Server.Tests/TypeChangeSummaryTest.cs` re-asked through the shipped client stack (a `LocalServer` over a `RemoteServer` over the engine), with the type altered on a session of the engine's own while the client's cursor stayed open. **Five of the seven fail.**

- `TestCommittedCreateTypeConstraintRefusesTheValueItRejects` — no exception raised.
- `TestCommittedCreateTypeConstraintRefusesThroughAView` — no exception raised.
- `TestCommittedCreateTypeDefaultDefaultsANewRow` — the column came back with no value instead of 7.
- `TestCommittedCreateTypeDefaultIsOfferedThroughARestriction` — likewise.
- `TestCommittedAttachTypeChangeHandlerRunsForAnOpenCursor` — the column came back 2 instead of 99.

The two rolled-back cases pass, and **should not be read as coverage.** Both start with the type already carrying the check or the default, so the client's copy, taken at prepare time and therefore before the drop, is the correct one; a drop that is then rolled back leaves it correct. Those two cases never exercise invalidation on the client side at all. Do not mirror them.

## What the client knows for free

The client keeps the highest value of the server's `Catalog.CacheTimeStamp` it has been told about, in `LocalServer._serverCacheTimeStamp`, and raises it in `LocalServer.EnsureCacheConsistent` — which every prepared expression reaches, because `RemoteServerPlan.GetCatalog` reports the cache as changed on every prepare. Each plan's own descriptor carries the value the counter had when **that** plan was prepared. So a plan whose recorded value is below the client's highest-known value was prepared before a catalog change the client has since heard about, and the client can know that locally, at no cost.

Measured on the same harness, reading the client's private field directly:

```
cursor open:                    server=8 client=8
after the alter:                server=9 client=8
after the client evaluated 1:   server=9 client=9
```

So an open plan prepared at 8, a catalog change taking the server to 9, and the client learning 9 from an unrelated one-line expression. That is what a real form gives you constantly — a lookup, a detail view, another form on the same session — and today none of it is consulted.

## What to do

Deny the local branch in `LocalCursor.Default`, `Change` and `Validate` when the plan's snapshot is known to be behind: alongside `TableVar.Is...CallRemotable(columnName)`, require that the plan's recorded cache timestamp is not below the client's highest-known server value. A denied branch sends the proposal to the server, which answers from the live schema — which is why this ticket waits on `bug-remote-validate-validates-the-wrong-row`, without which the remote `Validate` answers about the wrong row.

Two further reasons this is the right test, beyond the type change:

- When the client's highest-known value rises, `EnsureCacheConsistent` has **thrown the client's whole catalog copy away** and rebuilt it. An open plan's table variable and plan node are not rebuilt with it, so today's local proposals for such a plan already run against schema objects detached from the catalog they were compiled against. The same test stops that too.
- It is one test at the one seam where derived catalog state is exercised, rather than a correction applied per kind of statement.

**Do not use `Catalog.ShouldCallProposablesTimeStamp` as the client's key**, even though it is the key the server side uses for exactly this summary. It is deliberately coarse: `DDLNode.InternalBeforeExecute` increments it for *every* statement that changes the catalog, including the create and drop of the per-table copies an application transaction makes — which is a form's own edit cycle. In the measurement above it stood at 1265 after a bare server start and a handful of statements, against 8 for `CacheTimeStamp`. Server-side that coarseness is affordable because a recompute costs 5.65 µs (measured, recorded at `DDLNode.InternalBeforeExecute`); the client-side equivalent is a network round trip, so a client keyed on that counter would be sent remote for almost every proposal almost immediately, which is the whole optimization gone. `CacheTimeStamp` is the counter that is already gated on `ServerProcess.ShouldAffectTimeStamp` and therefore means "a change a client could care about" — which is why `bug-attached-handler-never-reaches-a-remote-client` has to land first, so that attaching a handler moves it.

### Where to read the client's highest-known value

The straightforward choice is `LocalServer._serverCacheTimeStamp` itself: it already exists, it is already maintained, and `LocalCursor` is in the same assembly. Its limit is that it is raised only where the catalog copy is rebuilt, which is reached by prepared *expressions*; a client that only executes statements between the catalog change and the edit will not have raised it, and will keep shortcutting. That is conservative in the safe direction — it can never report a fresh plan as stale — so take it as the recommendation.

The wider alternative is a second field that records the highest `PlanDescriptor.CacheTimeStamp` the client has been handed from anywhere, statement plans included, and is only ever read, never used to decide a rebuild. It detects strictly more, and the cost is a second piece of state with a meaning close enough to the first to be confused with it, plus the discipline that nothing may clear the catalog copy from those call paths — the rebuild needs the cache lock and the surrounding timestamp protocol. Take it only if it genuinely costs one line where descriptors arrive, and if you do, comment both fields so the difference between "the generation my copy is" and "the generation I have heard of" is on the page.

### The window that is left, and why to leave it

A form that sits idle — no prepare of any kind between the catalog change and the user's edit — still takes the shortcut and still reports late. Closing that needs either a round trip per proposal, which is the exact cost the permission exists to avoid, or the server telling the client without being asked, for which this architecture has no channel: every client-cache mechanism in it is pull-based, carried on the response to a prepare. Building a notification channel from server to client is a large architectural addition, and what it would buy is an earlier report of an error that is still raised, correctly, when the row is saved.

So leave it, and leave the reasoning where the next reader will meet it: a `NOTE:` at the staleness test in `LocalCursor` saying what is still stale, why, and what would change the answer (a per-proposal round trip became acceptable, or a server-to-client notification channel was built for some other reason and this could ride on it). This changes no wire contract, no metadata tag and no D4 semantics, so there is no compatibility question here to route to `blocked/` — the original fix ticket flagged that there might be, and there is not.

### TODO

- Add the staleness test to the three branches in `LocalCursor`, reading the client's highest-known server cache timestamp as discussed above.
- Expose the plan's recorded value where `LocalCursor` can read it (`LocalPlan` holds the descriptor), without widening the public surface more than the call needs.
- Write the reproduction on the shared client-stack fixture from `bug-remote-validate-validates-the-wrong-row`: three cases, one per proposal call — a created type constraint refused through `Validate`, a created type default offered through `Default`, an attached change handler run through `Change` — each with the client preparing something unrelated after the catalog change, which is what a real form does and what the fix consults. Do not mirror the view and restriction cases: on the client those exercise the client's own node tree, which `TypeChangeSummaryTest` already pins; what is new here is the permission, not the descent. Do not mirror the two rolled-back cases, for the reason given above.
- Write the `NOTE:` for the window that is left.
- Add an entry to `docs/migration-notes.md`: a client holding a plan prepared before a catalog change now sends its row proposals to the server instead of answering them itself, so such a form reports a new check or default without being reopened, at the cost of a round trip per proposal until the plan is replaced.
- Update the K43 record in `docs/modernization-plan.md`: it currently says the remote-client arm is open and names this slug. Replace that with what landed, what window is left and where the `NOTE:` is, and add the two sibling records if the earlier tickets have not already.
- Targeted tests: the three new cases, plus `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~TypeChangeSummaryTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~CursorTest"` (`Name` matches a test *method*, so `Name=CursorTest` selects nothing) and the client-stack fixtures the earlier two tickets leave behind. Name the filters in the handoff.
