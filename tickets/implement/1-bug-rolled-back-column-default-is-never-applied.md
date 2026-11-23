description: After a transaction that removed a column's default value or one of its checks is rolled back, the server puts the definition back but stops acting on it, so new rows silently get no default and the check is not offered to the client until the server is restarted.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: verified
difficulty: medium
----
## What goes wrong

A table in Dataphor carries three summary flags next to each table variable and each of its columns — in the code `ShouldDefault`, `ShouldValidate` and `ShouldChange`. They are not part of the table's definition; they are derived from it, and they say "there is something here worth calling". The engine reads them as a fast path: `TableNode.Default` (`src/engine/DAE/Runtime.Instructions/TableNode.cs:2135`) returns immediately without applying any default when `ShouldDefault` is false, and `Validate` and `Change` do the same with their flags. `ServerProcess` also uses them to decide what it tells a remote client about a table (`src/engine/DAE/Server/ServerProcess.cs:877`).

The forward path keeps them right: an `alter table` recomputes them at the end of the statement through `Schema.BaseTableVar.DetermineRemotable`, which calls `DetermineShouldCallProposables(true)` (`src/engine/DAE/Schema/TableVar.cs:2914`). **The rollback does not.** The undo log restores the definition — the column's default object, the column's constraint — and nothing recomputes the flags, so they keep the value the rolled-back statement left.

Measured on an in-process server on 2026-11-23, each statement run inside an `Isolated` transaction that was then rolled back:

| Statement rolled back | Flag before | Flag after the rollback | Effect |
|---|---|---|---|
| `alter table T { alter column C { drop default } }` | `ShouldDefault` true | **false** | the default is back in memory and the engine never applies it |
| `alter table T { alter column C { drop constraint X } }` | `ShouldValidate` true | **false** | the constraint is back in memory and is not offered to the client |
| `alter table T { alter column C { create default "x" } }` | `ShouldDefault` false | **true** | wasted work on every default request; harmless |
| `alter table T { alter column C { create constraint X … } }` | `ShouldValidate` false | **true** | wasted work; harmless |

The first two rows are the defect that matters: the rolled-back *removal* of a default or a column constraint leaves the server refusing to use something its own catalog says is there, until it is restarted. A form that creates a new row gets no default value for that column; `TableNode.Default` is called with an empty column name for a whole new row, and the table-level flag alone decides, so **no** column of that table gets its default.

Rolled-back table-level constraints, transition constraints, event handlers (table and column), keys, orders, tags and type-level defaults were measured the same way and are **not** affected. Event handlers are the example to copy: the attach, move and detach helpers in `CatalogDeviceSession` (around line 2797) are shared by the forward path and by the undo instructions, and each calls `tableVar.DetermineRemotable(this)`, which is why they already undo correctly.

## What to change

Make the undo recompute, the way the forward path and the event-handler helpers already do. The precedent in the same file is `AddDependenciesInstruction.Undo` (line 296), which calls `_object.DetermineRemotable(session)` after restoring.

The instructions whose `Undo` restores part of a table variable's definition and does not recompute are, in file order:

- `AttachTableVarColumnInstruction` / `DetachTableVarColumnInstruction`
- `AttachTableVarConstraintInstruction` / `DetachTableVarConstraintInstruction`
- `AttachTableVarColumnConstraintInstruction` / `DetachTableVarColumnConstraintInstruction`
- `SetTableVarColumnDefaultInstruction`
- `SetTableVarColumnIsNilableInstruction`

Fix the two column-level pairs and `SetTableVarColumnDefaultInstruction` because they are reproduced above. Fix the others in the same pass only if they can be given the owning table variable without changing a public signature; where they cannot, say so in the handoff rather than leaving a half-done set.

**The one real design question is how the undo reaches the table variable from a column**, because three of these instructions hold only a `Schema.TableVarColumn` and `DetermineRemotable` lives on the table variable. Two options, in order of preference:

1. **Resolve the parent.** `Schema.TableVarColumn` derives from `Schema.Object`, which carries `ParentObjectID`; if it is set for columns, `session.ResolveObject(ParentObjectID)` gives the table variable with no signature change. Check this first — it is the cheapest route and costs no compatibility surface.
2. **Pass the table variable down.** `CatalogDeviceSession.SetTableVarColumnDefault(Schema.TableVarColumn, Schema.TableVarColumnDefault)` and the two column-constraint helpers would take the table variable as a parameter, and their callers in `DDLNodes.cs` would supply it. `SetTableVarColumnDefault` and `SetTableVarColumnIsNilable` are `public`, so changing them is a change a custom C# extension could notice and needs an entry in `docs/migration-notes.md`; the two column-constraint helpers are `private` and cost nothing. Prefer adding an overload and leaving the old one in place over replacing it.

Recompute with `DetermineRemotable`, not by setting the flags directly: it recomputes the whole derived family (`IsDefaultRemotable`, `IsValidateRemotable`, `IsChangeRemotable` as well as the three `Should…` flags), from the restored definition, which is exactly what a restart would compute.

## Edge cases & interactions

- **Nested rollback.** Only the level's own instructions are replayed; after an inner rollback the flags must match the outer level's definition, not the original. A recompute from the restored definition gives that for free — but pin it with a test case, because it is the case a per-instruction "restore the old flag value" fix would get wrong.
- **Several changes to the same table in one transaction, rolled back.** Each instruction recomputes, so the last recompute sees the fully restored definition. Ordering is safe as long as the recompute is the last thing each `Undo` does; state that in a comment.
- **A column dropped later in the same transaction.** `DetachTableVarColumnInstruction` puts the column back before the earlier default/constraint instructions are replayed (reverse order), so those land on a live column. By inspection, as in the sibling ticket `bug-server-link-user-change-survives-rollback`.
- **A table variable dropped in the same transaction.** The whole object is restored by `DropCatalogObjectInstruction`; a recompute on the restored instance is correct. By inspection.
- **Views and derived table variables.** `Schema.TableVar.DetermineRemotable` is virtual and the view override calls `DetermineShouldCallProposables(false)` rather than `(true)` (`src/engine/DAE/Schema/TableVar.cs:3031`) — that is, it does not reset first. Calling `DetermineRemotable` on a view from an undo therefore cannot clear a stale true. Check whether any of these instructions can hold a view's column; if so, say what it means in the handoff rather than silently inheriting the difference.
- **Cost.** `DetermineRemotable` walks the table's columns; it is already run once per forward `alter table` statement, so running it once per undone instruction is the same order of work. No measurement needed, but do not call it inside a loop over columns.
- **Scalar type defaults** (`SetScalarTypeDefault`) are deliberately out of scope: the forward path does not recompute the flags of the table variables that use the type either, so a rolled-back `alter type … { drop default }` was measured to leave nothing stale. Changing that would be a wider change than this ticket.

## TODO

- Establish which route gives the table variable from a column (`ParentObjectID` first) and record the answer in the handoff.
- Add the recompute to the `Undo` of the column-default and column-constraint instructions, and to the table-variable constraint and column attach/detach pairs where it costs no public signature change.
- Reproductions in `tests/DAE.Server.Tests/CatalogStoreTest.cs`, which already holds the catalog rollback cases. Two cases are the defect and must be seen to fail before the fix: a rolled-back `drop default` on a column, asserting through the engine that a new row still gets the default (not just that the flag is back); and a rolled-back `drop constraint` on a column, asserting the same way. One case for the nested rollback. No test for the two harmless rows of the table — the flag value alone is not a behaviour worth a test.
- `docs/migration-notes.md` if option 2 is taken for a public member, and one entry under *D4, catalog and libraries* for the behaviour change either way.
- Build (`dotnet build Dataphor.slnx`), run the new cases, then the three part commands from the root `AGENTS.md`, each in the foreground.
