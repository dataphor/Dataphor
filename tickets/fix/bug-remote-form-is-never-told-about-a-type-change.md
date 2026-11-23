description: A form running in the Windows client, rather than inside the server, still does not notice when a data type gains or loses a check, a default or a change handler while the form is open.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/protocol/Client/Server/LocalCursor.cs, src/protocol/Client/Server/LocalPlan.cs, src/engine/DAE/Contracts/PlanDescriptor.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Schema/TableVar.cs, tests/DAE.Server.Tests/TypeChangeSummaryTest.cs
difficulty: hard
repro: static
----
## What is wrong

When a user is editing a row, the client asks the server three questions about it - what should this column default to, what follows from the user having changed this column, is this row acceptable. To avoid a round trip for the many cases where the answer is "nothing", the server tells the client once, when the plan is prepared, which of the three it may answer by itself out of its own copy of the schema. A form holds its plan for as long as the user keeps the form open.

The answer the server gives is computed from the three-flag summary each table variable derives from its definition - is there a default worth asking for, a check worth running, a change handler worth calling. `bug-rolled-back-type-constraint-stops-the-column-check` made that summary recompute itself whenever a catalog change may have invalidated it, so a form hosted *inside* the server now sees a change to a data type at once. The client's copy was not part of that change: it is a snapshot taken at prepare time and nothing re-sends it, so a form in the Windows client behaves as it did before the fix.

Concretely, before the type has a check: `ServerProcess.CopyDataType` adorns the plan's table variable with `DAE.IsValidateRemotable = IsValidateRemotable || !ShouldValidate`, which is `true` because there is nothing to validate. The client receives that in the plan descriptor, and `LocalCursor.Validate` then takes its local branch (`TableVar.IsValidateCallRemotable(columnName)`) and runs the proposal against its own in-process copy of the schema, never calling the server. After `alter type T { create constraint ... }` the server knows better and the client is never told, so the user still meets the error on saving rather than on leaving the field. The same holds for `{ create default ... }` and for `attach operator ... to T on { change }`, and for the matching drops rolled back.

Nothing is stored wrong: the insert and the update check the constraint by a path that does not consult any of this. What is lost is the early report, which is the whole point of the three questions.

## What to settle

The decision is not *where* to recompute - that part is solved server-side - but **how an open remote plan learns that a shortcut it was granted has been withdrawn**. Two pieces of state are captured at prepare time and both can go stale:

- the three `DAE.Is...Remotable` tags (and their per-column overrides) that grant the shortcut, and
- the client's own copy of the data type's definition, which is what it evaluates against when it takes the shortcut. The client does have a refresh mechanism for its schema cache (`AcquireCacheLock`, `GetCatalog`, `Catalog.CacheTimeStamp` and `ClientCacheTimeStamp` in `PlanDescriptor`); whether a type's new check actually reaches it, and when, is part of what this ticket has to establish.

The shape worth aiming at is an invariant rather than a patch: **derived catalog state must not cross the wire without a validity key the reader re-checks.** The plan descriptor already carries two timestamps, so the ingredients exist; `Catalog.ShouldCallProposablesTimeStamp` is the key the server side now uses for exactly this summary. A shortcut re-checked against a timestamp retires the whole class, where re-sending one adorn after one kind of statement would not.

Compatibility is the constraint that makes this hard: `PlanDescriptor` and the `DAE.Is...Remotable` tags are wire and metadata surfaces, and a client older than the server has to keep working. If the only correct answer changes one of those shapes, that is a question for `blocked/` with the proposed wording, not a change to make quietly.

## What would confirm it

Read from the code, not run: the chain above was established by reading `ServerProcess.CopyDataType` (the tags are built from the stored flags, and its one caller is `ServerProcess.Compile`, which runs while a plan is being prepared), `LocalCursor.Default`, `Change` and `Validate` (each shortcuts on `TableVar.Is...CallRemotable`), and by confirming that nothing re-sends a prepared plan's data type.

What would make it `verified` is a case over the local-client stack rather than the in-process engine - the same seven cases `TypeChangeSummaryTest` pins, asked through an `IServerCursor` obtained from a `LocalServer`/`LocalCursor` instead of straight from the engine, with the cursor opened before the `alter type`. `CoreWcfProtocolTest` and `tests/DAE.Server.Tests`'s other client fixtures show how such a harness is stood up. Expect the in-process cases to pass and the client ones to fail; if a client case passes, find out which refresh made it pass before concluding anything, because that mechanism is probably the one this ticket should be built on.

## Where it came from

Found in the review of `bug-rolled-back-type-constraint-stops-the-column-check`, which fixed the server-side half and recorded this half as out of scope. `docs/modernization-plan.md` K43 names it as that defect's open arm. Present since the initial open-source commit; not a port consequence.
