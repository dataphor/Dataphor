description: After a cancelled change to a data type, the server can stop offering a form the check that belongs to that type, so the user is told their entry is wrong only when they try to save instead of as they leave the field.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Runtime.Instructions/TableNodes.cs
repro: verified
severity: edge-case
likelihood: unusual
tradeoffs: The window is narrow and nothing is stored wrong - the save still refuses the value - so a maintainer may reasonably file this under "reopen the form" and spend the effort elsewhere; the fix also needs the server to find every table that uses a type, which the code has no cheap way to do today.
----
## What goes wrong

Beside each table, and beside each of its columns, the engine keeps a one-bit summary of the table's definition: is there a default worth asking for, a check worth running, a change handler worth calling. It is derived from the definition, never stored, and it exists so that the common case - a table with none of those - costs nothing. When the summary says "nothing here", the engine skips the work without looking at the definition.

Nothing invalidates that summary when the definition changes. Instead it is recomputed as a side effect of compiling any expression over the table, which in practice happens often enough that the summary is usually right. The gap is a *rollback*: the undo log restores the definition, and whatever the summary last computed - from the half-altered definition the rollback has just thrown away - stays. Anything reading the table through that stale summary then answers from a definition the server no longer holds.

`bug-rolled-back-column-default-is-never-applied` closed that gap for the eight undo steps that restore part of a *table's* own definition. It left open the steps that restore part of a *data type's* definition, which the summary also reads: a column of type `T` counts as "has a check worth running" when `T` itself carries a constraint. So a cancelled `alter type` can still strand the summary.

**Measured on an in-process server, 2026-11-23**, against the current branch with that fix in place:

- a type `ProbeType like Integer { constraint IsPositive value > 0 }` and a table with a column of that type;
- a cursor opened on the table, as an open form holds one;
- in a transaction: `alter type ProbeType { drop constraint IsPositive };` then any read of the table - the read is what recomputes the summary, now from the definition without the constraint;
- the transaction rolled back.

Through that still-open cursor the engine then accepts `-5` for the column without complaint. Without the read inside the transaction it does not reproduce: nothing recomputes, so nothing goes stale.

## What it costs, and what it does not

Only the *early* check is lost - the one a form asks for as the user leaves a field, so that the error appears next to the entry. The insert itself validates unconditionally, by a different path, so a value the constraint rejects is still refused when the row is saved and nothing wrong is ever stored. The user-visible effect is an error that arrives late and away from the field that caused it, for as long as that cursor stays open; a reopened form is correct again.

The same shape applies to a cancelled change to a type's *default*, where the cost is a field the form stops filling in.

## Why it is not a one-line fix

For a table's own definition, the undo step holds the table, so it can recompute. For a type, it holds only the type - and the summaries that need recomputing belong to every table with a column of that type. The engine has no index from a type to those tables, and the forward path does not look for one either, which is why it also leaves stale summaries behind in the same way (harmlessly today, because the next compilation repairs them).

So there is a choice to make, and it is the reason this is filed rather than fixed:

- **Recompute the dependents.** Walk the type's dependents in the catalog on undo and recompute each table that uses it. Correct, and the cost falls only on a rollback - but it needs a dependents walk that is reliable for a type, and it is the one place in the engine that would need it.
- **Stop caching the summary.** Derive it on read instead of storing it. Retires the whole class of defect, including the eight steps already patched, and removes the oddity that compiling an expression over a table silently rewrites part of that table's catalog object - which the code itself calls out as wrong (`src/engine/DAE/Runtime.Instructions/TableNodes.cs:1458`, "changing the definition of a catalog object as a result of it being referenced in an expression. Ouch."). It is a hot path, so this needs a measurement before it is a plan, not after.
- **Invalidate rather than recompute.** Give the summary a validity stamp against the catalog's existing cache timestamp, which every DDL statement already bumps, and recompute lazily on the first read that finds it stale. Middle cost, and it fixes the forward path and the undo path at once.

A maintainer picking the second or third option should know that it supersedes the eight per-step recomputes that already landed rather than adding to them.

## How a fix would be confirmed

The reproduction above belongs beside the three cases in `tests/DAE.Server.Tests/ColumnChangeRollbackTest.cs`, which already has the cursor and row helpers it needs and needs no database server. A fix taking the second or third option should also keep those three cases green with the eight per-step recomputes removed - that is the check that the class is really retired and not just patched twice.
