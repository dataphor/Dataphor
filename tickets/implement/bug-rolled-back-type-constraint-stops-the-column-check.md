description: When a data type gains or loses a check or a default, a form that is already open goes on behaving as though the change had not happened, so the user is told their entry is wrong only when they try to save instead of as they leave the field.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/TypeChangeSummaryTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
difficulty: hard
repro: verified
----
## The one thing that is wrong

Beside each table variable, and beside each of its columns, the engine keeps a three-bit summary of what a row of that table needs while a client is editing it: is there a default worth asking for, a check worth running, a change handler worth calling. The engine reads it as a fast path - `TableNode.Default`, `Change` and `Validate` each return at once when the bit is false - so when the summary says "nothing here", the definition is never looked at.

The summary is derived, never stored, and the derivation is one method: `Schema.TableVar.DetermineShouldCallProposables` (`src/engine/DAE/Schema/TableVar.cs:2294`, and its per-column half at `:97`). It reads a column's own default, constraints and handlers **and the default, constraints and handlers of the column's data type**.

**Nothing invalidates the summary when any of those change.** It is instead recomputed as a side effect of compiling an expression over the table (`TableVarNode.DetermineCharacteristics`, `src/engine/DAE/Runtime.Instructions/TableNodes.cs:1456`, whose own comment calls that side effect wrong), plus nine recomputes placed by hand on particular statements and particular undo steps. Those nine cover changes to a *table's* own definition. None of them covers a change to a *data type's* definition, because the step that changes a type holds only the type, and the summaries that need recomputing belong to every table with a column of that type - which the engine has no index for.

That is the whole defect, and it is one site: the summary is a cache with no invalidation.

## What was measured, and what it says

An in-process server, this branch at `21f5105d`, Debug, 2026-11-23. Each case opens a cursor first - that is how an open form holds a table - then changes a definition, then asks the engine through that same still-open cursor for the early check or the default.

| | what was done | asked through | result |
|---|---|---|---|
| A | `alter type T { drop constraint C }` in a transaction, a read of the table, rollback | `select Tbl` | **accepts the value the constraint rejects** |
| B | `alter type T { create constraint C }`, **committed, no transaction at all** | `select Tbl` | **accepts the value the constraint rejects** |
| C | `alter type T { drop default }` in a transaction, a read, rollback | `select Tbl` | **offers no default** |
| D | `alter type T { create default 7 }`, **committed** | `select Tbl` | **offers no default** |
| J | `alter type T { create constraint C }`, **committed** | `select Vw`, a view over the table | **accepts the value the constraint rejects** |
| K | `attach operator Op to T on { change }`, **committed** | the table's own summary | **stays false: the change handler is never called** |
| E | `alter table Tbl { alter column C { create constraint } }`, committed | `select Tbl` | refuses - correct |
| F | `alter table Tbl { alter column C { drop constraint } }` rolled back | `select Tbl where ID > 0` | refuses - correct |
| G | the same | `select Vw`, a view over the table | refuses - correct |
| I | `alter type T { create constraint C }`, committed | `select Tbl where ID > 0` | refuses - correct |

Three things follow, and together they narrow the three options the `fix/` ticket listed down to one.

**It is not a rollback defect.** B, D and K involve no transaction. A committed change to a type - the ordinary way an application evolves a type - leaves every already-open form on a table using that type behaving as though the change had not happened, until something else happens to compile an expression over that table. The `fix/` ticket's "the window is narrow" and its `likelihood: unusual` were both wrong: this is normal use, and the window is however long the form stays open.

**Its reach is the top of the open plan.** F, G and I pass because a plan-local result table variable (`Schema.ResultTableVar`) starts life with all three bits *set* - `Schema.TableVar`'s field initialiser, `src/engine/DAE/Schema/TableVar.cs:1984` - and nothing lowers them except `BaseTableVar.DetermineRemotable`. So a restriction's gate is always open, the proposal descends, and the descent checks the real constraints rather than the summary. The summary only loses a check when the *top* node of the open plan holds a catalog object whose bit was lowered: a base table variable (A-D) or a view (J). G passing and J failing is the same rule in two directions - a view recomputes without resetting (`Schema.DerivedTableVar.DetermineRemotable`), so a bit that was raised when the cursor was compiled stays raised (G), and one that was low stays low (J).

**No existing catalog timestamp can serve as the cache's validity key.** The `fix/` ticket proposed stamping the summary against "the catalog's existing cache timestamp, which every DDL statement already bumps". It does not: `CreateEventHandlerNode` and `AlterEventHandlerNode` bump none of the four timestamps, which is exactly case K. The key has to be a counter of its own.

And the cost of the one remaining option the `fix/` ticket would not accept without a measurement - derive the summary on every read instead of caching it - was measured: on a 21-column table a full re-derivation takes **5.65 µs**, against **59.6 µs** for one whole-row `Default` proposal, so 9.5% of a proposal. Affordable, but not free, and it cannot be applied to a view or to a plan-local result table variable at all, because their summaries include inference from the expression they are over that only a compilation can produce. So: keep the cache, and give it an invalidation.

## The fix

A validity counter on the catalog, a stamp on each table variable, and a recompute on the first read after the counter moves.

**The counter.** `Schema.Catalog` gains a fifth timestamp beside `TimeStamp`, `CacheTimeStamp`, `PlanCacheTimeStamp` and `DerivationTimeStamp` (`src/engine/DAE/Schema/Catalog.cs:85-151`), with the same shape - a `long`, a read-only property, an `Update…` that increments under `lock (this)`. It means "some definition a table variable's summary is derived from may have changed".

**Two places bump it**, which is what makes this complete by construction rather than by a list:

- `DDLNode.InternalBeforeExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:28`). Every statement that changes the catalog is a `DDLNode`, so no per-statement discipline is needed and no statement added later can be forgotten. Put the bump outside the existing `InLoadingContext` guard: a bump during loading costs one recompute and is harmless.
- `CatalogDeviceSession.InvalidateCachesAfterUndo` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:1726`). Every rollback that undid DDL already goes through it, and an undo changes the definition again, so the summary computed from the half-altered definition inside the transaction has to be invalidated too.

**The stamp is recorded only by the ensure, never by the compile-time recompute.** This matters. The bump happens *before* the statement runs, and a statement can compile an expression over the table in the middle of its own work (`AlterTableVarNode.ValidateConstraint` does), which recomputes the summary from the definition as it stands at that moment. If that recompute recorded the stamp, the statement's own later mutation would leave a stale summary wearing a current stamp - this defect, returning. So `DetermineShouldCallProposables` and `DetermineRemotable` keep writing the bits and must not touch the stamp.

**The ensure**, on `Schema.TableVar`:

```csharp
private long _shouldCallProposablesTimeStamp = -1;

/// <summary>Recomputes the summary if any definition it is derived from may have changed since it was last computed.</summary>
public virtual void EnsureShouldCallProposables(Schema.Catalog catalog)
{
    long timeStamp = catalog.<the new timestamp>;
    if (_shouldCallProposablesTimeStamp != timeStamp)
    {
        DetermineShouldCallProposables(/* reset */ …);
        _shouldCallProposablesTimeStamp = timeStamp;
    }
}
```

Three behaviours, one per kind of table variable, each needing its reason recorded at the site:

- `BaseTableVar`: recompute with `reset: true`. The bits are a total function of the definition, so this is both correct and idempotent, and it can lower a bit as well as raise one.
- `DerivedTableVar`, a view: recompute with `reset: false`. A view's bits also carry inference from the expression it is over, which only a compilation produces, so clearing them would throw that inference away - which is why `DerivedTableVar.DetermineRemotable` passes `false` today. Raise-only is enough for the user-visible defect: case J is fixed, because a view's columns carry the source columns' data types and so the recompute sees the type's new constraint, and nothing a compilation inferred is ever lost. It is also the honest answer to the last arm of the `fix/` ticket - a view's summary is raised, not restored, and under this design there is nothing left to restore.
- `ResultTableVar`, a plan-local one: do nothing. Its bits are the compile-time propagation and it is built fresh by every compilation, so it is never stale; and a raise-only recompute could open a gate a node's propagation rule deliberately left closed, which is a behaviour change with no defect behind it. F, G and I show these gates are open anyway.

**Three read sites call it**, and each already holds the `Program`, so each can reach `program.Catalog`: `TableNode.Validate`, `TableNode.Change` and `TableNode.Default` (`src/engine/DAE/Runtime.Instructions/TableNode.cs:1949`, `:2082`, `:2135`), before the `if (ShouldValidate(columnName))` test each of them opens with. Those three are the only callers of `TableNode.ShouldValidate`, `ShouldChange` and `ShouldDefault`, so no signature changes and nothing outside that file is affected.

## What deliberately does not change

**The nine hand-placed recomputes stay.** The `fix/` ticket expected this fix to supersede them, and three code comments predict it (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1332` and `:1530`, `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:92-110`). It does not, and those comments have to be corrected rather than left predicting something that did not happen. The reason they stay: the *stored* bits have two other readers. `ServerProcess.CopyDataType` (`src/engine/DAE/Server/ServerProcess.cs:876-896`) builds the remote client's copy of the table variable from them at prepare time, and `CatalogRollbackSnapshotTest` compares them, by reflection over every public scalar property, before and after a rolled-back statement; removing the recomputes would leave a rolled-back `alter table` with a raised bit in the stored summary and fail that fixture. What does change is that correctness no longer *depends* on that list being complete - the read path is right whether or not a step recomputes.

**Nothing about D4 semantics, operator signatures, class names, file formats, error codes, contract shapes or ports.** The only behaviour an application sees is the early check and the early default appearing where they previously went missing; the insert itself validated unconditionally before and after, so no value a constraint rejects was ever stored either way. New public members on `Schema.Catalog` and `Schema.TableVar` are additive. Both of those - the behaviour an application notices, and the added public members a custom extension could use - need an entry in `docs/migration-notes.md` in the same commit.

**`CatalogRollbackSnapshotTest`'s matrix is not extended to a type that a table uses.** It would fail, and correctly so: under this design a rolled-back change to a type leaves the *stored* bits of the dependent tables alone and the read path recomputes them, so a fixture that reflects over the stored bits would report a difference the engine no longer acts on. Record that as a `NOTE:` beside the fixture's other recorded omissions, not as a ticket.

## How the fix is confirmed

A new fixture, `tests/DAE.Server.Tests/TypeChangeSummaryTest.cs`, added to the explicit `Compile` list in `DAE.Server.Tests.csproj` - which is how that project picks files up. A new file rather than extra cases in `ColumnChangeRollbackTest`: half of these cases involve no rollback, so they belong under neither that fixture's name nor its remarks, and renaming a file is a move rather than an edit. `ColumnChangeRollbackTest` has the cursor, row, `DefaultOf` and `Validate` helpers to copy; no database server is needed.

One case per measured behaviour that fails today, six in all - A, B, C, D, J and K from the table above. They are not redundant with one another: a forward and a rolled-back change reach the invalidation through different call sites, a base table variable and a view recompute by different rules, and the event-handler case (K) is the one that proves the counter cannot be an existing timestamp. Write K as a behavioural check - does the change handler run for the open cursor - rather than by reading the flag, so that it pins what a user sees. The four cases that already pass (E, F, G, I) get no test: they are the shapes whose gate the summary never closes, and a test there would pin an accident of the plan-local initialiser rather than a behaviour.

`ColumnChangeRollbackTest`'s three existing cases and `CatalogRollbackSnapshotTest`'s whole matrix must stay green unchanged.

## Tasks

- Add the fifth timestamp to `Schema.Catalog`, following the shape of the four beside it, and say in its remarks what it guards and that the two bump sites below are the complete set.
- Bump it in `DDLNode.InternalBeforeExecute`, outside the loading guard, with a `NOTE:` saying it is one of two bump sites and that a global counter means every table recomputes its summary once after any DDL - cheap, because a recompute walks the columns while a proposal does far more, and because DDL is rare; if a workload that interleaves DDL with editing ever shows it, narrow the counter.
- Bump it in `CatalogDeviceSession.InvalidateCachesAfterUndo`, beside the three timestamps it already bumps.
- Add `_shouldCallProposablesTimeStamp` and `EnsureShouldCallProposables` to `Schema.TableVar`, with the `BaseTableVar` (reset), `DerivedTableVar` (raise-only) and `ResultTableVar` (no-op) behaviours and the reason for each recorded at the site. Leave a `NOTE:` on the `ResultTableVar` no-op: it is safe only because nothing lowers a plan-local gate, and a node taught to lower its own gate would make it stale.
- Make sure neither `DetermineShouldCallProposables` nor `DetermineRemotable` records the stamp, and say why in a comment where a reader would otherwise add it - a recompute in the middle of the statement that is about to change the definition would stamp a summary that is about to go stale.
- Call the ensure in `TableNode.Validate`, `Change` and `Default`, before each one's `Should…` test.
- Correct the three comments that predict this ticket retiring the per-statement recomputes - `DDLNodes.cs:1332`, `DDLNodes.cs:1530`, `CatalogDeviceSession.cs:92-110` - to say what landed and why the recomputes stay.
- Add the `NOTE:` to `CatalogRollbackSnapshotTest`'s remarks about why its matrix does not cover a type that a table uses.
- Add `tests/DAE.Server.Tests/TypeChangeSummaryTest.cs` with the six cases, and its `Compile` entry.
- Add the `docs/migration-notes.md` entry: the early check and the early default now appear after a committed or rolled-back change to a type, where an already-open form previously missed them; and the new public members.
- Build (`dotnet build Dataphor.slnx`), then run the targeted set - `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~TypeChangeSummaryTest|FullyQualifiedName~ColumnChangeRollbackTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~CursorTest|FullyQualifiedName~TableAssignmentEvaluationTest" --logger "console;verbosity=normal"` - and the coverage scripts for the areas the diff touches, at least `Name=EventHandlers`, `Name=Constraints` and the `Name=Transaction.*` scripts. Name the filters used in the handoff.
