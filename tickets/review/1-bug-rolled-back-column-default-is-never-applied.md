description: A rolled-back removal of a column's default value or of one of its checks used to leave the server refusing to use something its own catalog said was there; the rollback now recomputes what it had stopped recomputing, and three tests pin the behaviour.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/ColumnChangeRollbackTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
difficulty: medium
----
## What landed

Beside each table variable and each of its columns the engine keeps three summary flags — in the code `ShouldDefault`, `ShouldValidate` and `ShouldChange`. They are not part of the definition; they are derived from it and say "there is something here worth calling", and the engine reads them as a fast path: `TableNode.Default` returns without applying anything when `ShouldDefault` is false, and `Validate` and `Change` do the same with their own flags.

The undo log restored the definition — a column's default object, a column's constraint — and nothing recomputed the flags, so they kept the value the rolled-back statement had left. Eight `Undo` methods in `CatalogDeviceSession` now recompute, through a new private helper `RedetermineRemotable` (two overloads, one taking the table variable and one taking a column), called last in each `Undo` so the recompute sees the whole restored definition:

- `AttachTableVarConstraintInstruction` / `DetachTableVarConstraintInstruction`
- `AttachTableVarColumnInstruction` / `DetachTableVarColumnInstruction`
- `AttachTableVarColumnConstraintInstruction` / `DetachTableVarColumnConstraintInstruction`
- `SetTableVarColumnDefaultInstruction`
- `SetTableVarColumnIsNilableInstruction`

That is the whole set the ticket named, with no public signature change — see the next section. The recompute is `DetermineRemotable`, not the flags set directly, so the whole derived family (`IsDefaultRemotable`, `IsValidateRemotable`, `IsChangeRemotable` as well as the three `Should…` flags) is recomputed from the restored definition, which is what a restart would compute.

## The design question the ticket left open, and its answer

The ticket asked which of two routes gives an undo the table variable when the instruction holds only a `Schema.TableVarColumn`: resolve `ParentObjectID` through the session, or pass the table variable down and change `SetTableVarColumnDefault`'s public signature.

**Neither was needed.** `Schema.TableVarColumn` carries a public back-reference to its table variable (`src/engine/DAE/Schema/TableVar.cs:311`), maintained by the collection that owns it (`TableVarColumns.Adding`/`Removing`, lines 789–799). So there is no object resolution and no new parameter: `SetTableVarColumnDefault` and `SetTableVarColumnIsNilable` keep the signatures they had, and `docs/migration-notes.md` records only the behaviour change, with nothing for a custom C# extension to adapt to.

The back-reference is null while a column is detached. An `Undo` never meets that state — a drop of the column is logged *after* any change to the column, so the reverse-order replay has already put the column back — but the column overload guards for it rather than relying on the argument; the reasoning is in its doc comment.

## What the ticket got wrong, and what the reviewer should check instead

The ticket says the stale flag lasts "until the server is restarted". **It does not.** Compiling any expression over a table recomputes that table's flags on the live catalog object (`TableVarNode.DetermineCharacteristics`, `src/engine/DAE/Runtime.Instructions/TableNodes.cs:1456`), so a cursor *opened after* the rollback repairs the flags on its way in and gets the default. Measured before the fix: the flags are stale immediately after the rollback (`ShouldDefault` false with the default present), the next fresh cursor both repairs them and applies the default, and the plan cache gives no reproduction either, because the `alter table` invalidates it.

What the defect actually reaches is **a cursor that was already open across the rollback** — an open form, in practice — which reads the stale summary for as long as it stays open. That is where all three tests ask, and before the fix all three failed there (measured: `Default` returned no value through the very cursor that had returned `abc` a moment earlier). It is a genuine defect with a real but bounded window, not the permanent one the ticket described; a reviewer judging "is this worth fixing" should weigh it on the narrower claim.

That same mechanism answers a question a reviewer will have about `AlterColumns` in `DDLNodes.cs`: the forward path has no `DetermineRemotable` call of its own for a default or a column constraint, and is right only because the statement always compiles an expression over its own table. That is recorded as a `NOTE:` tripwire above `AlterColumns` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:1475`) — comment only, no behaviour change.

## Tests added

In the new `tests/DAE.Server.Tests/ColumnChangeRollbackTest.cs` (over `InProcessTestFixture`, one in-process server per fixture), with a `<Compile Include="…" />` entry in alphabetical position. Each case opens a cursor before the transaction and asks through it after the rollback, which is the state the defect is visible in; all three were run against the un-fixed engine first and all three failed.

| Test | What it verifies |
|---|---|
| `TestRolledBackDropDefaultStillDefaultsANewRow` | After a rolled-back `alter column C { drop default }`, the engine still proposes the default for a new row. Failed before the fix with `Expected: "abc" But was: null`. |
| `TestRolledBackDropColumnConstraintStillRefusesTheValueItRejects` | After a rolled-back `alter column C { drop constraint X }`, the engine still refuses a value the constraint rejects, naming the constraint. Failed before the fix by raising nothing at all. |
| `TestNestedRollbackLeavesTheOuterLevelsDefault` | An inner rollback lands on the outer level's definition (the default the outer transaction created), and the outer rollback then lands on the original (no default). Pins that the recompute reads the restored definition rather than replaying a recorded flag value. |

No test for the two harmless cases in the ticket's table (a rolled-back *create* leaves a flag raised, costing a call that finds nothing to do) — the flag value alone is not behaviour worth a test.

**Where they live.** The ticket said `tests/DAE.Server.Tests/CatalogStoreTest.cs`. They are in a fixture of their own instead, following the two sibling rollback tickets that landed before this one (`DeviceClassAttributeRollbackTest.cs`, `ServerLinkUserRollbackTest.cs`): these cases read nothing from the catalog store, and `CatalogStoreTest` starts a fresh `TestServerInstance` per case where `InProcessTestFixture` starts one per fixture. Reviewer's call if that is wrong; moving them is mechanical.

## What a reviewer should look at hardest

- **Four of the eight call sites are fixes with no reproduction behind them.** `AttachTableVarConstraintInstruction` / `DetachTableVarConstraintInstruction` were fixed because they restore part of a table variable's definition and the recompute cost nothing, not because a defect was found there. Measured on an open cursor across a rollback of `alter table T { drop constraint X }` (a row constraint) and of a transition constraint: `ShouldValidate` never went false in the first place, so the rollback left nothing stale — and `Validate` on a whole new row does not run a table-level row constraint anyway. Same for the column attach/detach pair, and for `SetTableVarColumnIsNilableInstruction`, where nothing derived reads nilability today (there is a one-line comment saying so at that site). If the reviewer's bar is "every line of the fix has a test", those four do not meet it; the argument for them is that the rule "an undo that restores part of a table variable's definition leaves the derived summary consistent with it" is worth more uniform than patched only at the sites that happen to be observable today.
- **Views.** `Schema.DerivedTableVar.DetermineRemotable` calls `DetermineShouldCallProposables(false)` where the base-table override passes `true` — it does not reset before recomputing, so on a view the recompute can raise a flag but cannot lower one. Only the table-variable constraint instructions can hold a view (`AlterViewNode` passes a `Schema.DerivedTableVar` to the shared `DropConstraints`/`CreateConstraints`; the column-level instructions are reached only from `AlterTableNode`, which works on a `Schema.BaseTableVar`). So a rolled-back `alter view V { create constraint X }` can still leave `ShouldValidate` raised — the harmless direction, and no worse than before the change. Recorded in the helper's doc comment.
- The helper's doc comment is long by this file's standards. The content is the part worth keeping; trim the prose if it reads as too much.
- `RedetermineRemotable` is `private static` with two overloads differing only in the second parameter's type. If that reads as too clever for this codebase, two differently-named methods are fine.

## Scope deliberately not touched

`SetScalarTypeDefault` and the scalar-type constraint instructions, as the ticket set out: the forward path does not recompute the flags of the table variables that use a type either, so a rolled-back `alter type … { drop default }` leaves nothing stale. Changing that is a wider change than this ticket.

## Build and test state

`dotnet build Dataphor.slnx` — 0 errors, no new warnings. All three parts of the suite run in the foreground after the fix: part 1 **700 passed, 0 failed, 33 skipped** (8 m 15 s), part 2 **67 passed, 0 failed, 11 skipped** (3 m 4 s), part 3 **50 passed, 0 failed, 12 skipped** (3 m 5 s). No pre-existing failures surfaced, so nothing was written to `tickets/.pre-existing-error.md`.

`docs/migration-notes.md` carries one new entry under *D4, catalog and libraries*, describing the behaviour change in terms of what a form sees and saying explicitly that the effect was limited to a cursor open across the rollback.
