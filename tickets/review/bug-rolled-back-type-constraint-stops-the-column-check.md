description: A form that is already open now sees a change to a data type's checks, default or change handler, instead of behaving as though the change had not happened until it is reopened.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Catalog.cs, src/engine/DAE/Schema/TableVar.cs, src/engine/DAE/Runtime.Instructions/TableNode.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/TypeChangeSummaryTest.cs, tests/DAE.Server.Tests/CatalogRollbackSnapshotTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
difficulty: hard
repro: verified
----
## What landed

The three-flag summary each table variable and each of its columns derives from its definition - is there a default worth asking for, a check worth running, a change handler worth calling - was a cache with no invalidation. It is now keyed on a catalog counter, and the proposal path recomputes it when the counter has moved.

**The counter.** `Schema.Catalog.ShouldCallProposablesTimeStamp`, a fifth coordination timestamp beside `TimeStamp`, `CacheTimeStamp`, `PlanCacheTimeStamp` and `DerivationTimeStamp`, with the same shape (`long`, read-only property, `UpdateShouldCallProposablesTimeStamp()` incrementing under `lock (this)`). It means "some definition a summary is derived from may have changed". Bumped in exactly two places, which is what makes the set complete by construction rather than by a list:

- `DDLNode.InternalBeforeExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs:28`), outside the existing `InLoadingContext` guard. Every statement that changes the catalog is a `DDLNode`, so no statement added later can be forgotten.
- `CatalogDeviceSession.InvalidateCachesAfterUndo` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:1726`), beside the three timestamps it already bumps. An undo changes the definition again, so a summary computed inside the transaction from the half-altered definition is as stale as one computed before it.

**The stamp and the ensure.** `Schema.TableVar` gains a private `_shouldCallProposablesTimeStamp` (initialised to -1, so a summary never yet ensured always recomputes) and `public void EnsureShouldCallProposables(Schema.Catalog)`, which recomputes and records the stamp only when the counter has moved. The per-kind rule is a new `protected abstract void RecomputeShouldCallProposables()`, so the compiler forces any future kind of table variable to state its own:

- `BaseTableVar` - recompute with `reset: true`. The flags are a total function of the definition, so this is correct, idempotent, and can lower a flag as well as raise one.
- `DerivedTableVar` (a view) - recompute with `reset: false`, raise-only. A view's flags also carry what a compilation inferred from the expression it is over, which only a compilation can produce, so clearing them first would throw that away. Raise-only is enough for the user-visible defect, because a view's columns carry the source columns' data types.
- `ResultTableVar` (plan-local) - nothing. Its flags are the compile-time propagation and the stub is rebuilt by every compilation, so it is never stale; a raise-only recompute there could reopen a gate a node's propagation rule closed deliberately.

`DetermineShouldCallProposables` and `DetermineRemotable` deliberately do **not** record the stamp, and both now say so where a reader would otherwise add it. This is load-bearing: the counter is bumped *before* the statement runs, and a statement can compile an expression over the table in the middle of its own work (`AlterTableVarNode.ValidateConstraint` does), so a compile-time recompute that recorded the stamp would leave the statement's own later mutation wearing a current stamp - this defect, returning.

**Three read sites call the ensure**, each before its own `Should...` test: `TableNode.Validate`, `Change` and `Default`. Those three are also how a proposal descends a plan (`UnaryTableNode.InternalValidate` and its siblings call them on the source node), so the three calls cover every node of an open plan, not only the top one. No signature changed and nothing outside `TableNode.cs` calls `ShouldValidate`/`ShouldChange`/`ShouldDefault`.

**The nine hand-placed per-statement recomputes stay**, against what the `fix/` ticket expected. Three comments predicted this change retiring them and have been corrected to say what actually landed and why they stay: `DDLNodes.cs` `CreateConstraints` and `AlterColumns`, and `CatalogDeviceSession.RedetermineRemotable`'s remarks. The reason: the *stored* flags have two readers that are not proposals - `ServerProcess.CopyDataType` builds a remote client's copy of the table variable from them at prepare time, and `CatalogRollbackSnapshotTest` compares them by reflection around a rolled-back statement. What changed is that no proposal depends on that list being complete any more.

**`docs/migration-notes.md`** gains two entries: the behaviour an application notices (in *D4, catalog and libraries*), and the new public members (in *Custom C# extensions*, including the note that a `Schema.TableVar` subclass in a source copy must now implement `RecomputeShouldCallProposables()`).

## Tests added

`tests/DAE.Server.Tests/TypeChangeSummaryTest.cs`, six cases, with its `Compile` entry in `DAE.Server.Tests.csproj`. A new file rather than cases in `ColumnChangeRollbackTest` because half of them involve no rollback. Each opens a cursor first - that is how an open form holds a table - then changes a definition, then asks the engine through that same still-open cursor.

| Test | What it verifies |
|---|---|
| `TestRolledBackDropTypeConstraintStillRefusesTheValueItRejects` | A rolled-back `alter type T { drop constraint C }` (with a read of the table inside the transaction, which is what lowers the flag) still refuses the value the check rejects. Reaches the invalidation through the catalog undo path. |
| `TestCommittedCreateTypeConstraintRefusesTheValueItRejects` | A committed `alter type T { create constraint C }`, no transaction at all, refuses the value the check rejects through a cursor opened before the statement. Reaches it through the statement path. |
| `TestRolledBackDropTypeDefaultStillDefaultsANewRow` | A rolled-back `alter type T { drop default }` still offers the type's default for a whole new row. |
| `TestCommittedCreateTypeDefaultDefaultsANewRow` | A committed `alter type T { create default ... }` is offered to a cursor opened before the statement. |
| `TestCommittedCreateTypeConstraintRefusesThroughAView` | The same through a view rather than the table - the one case that exercises the raise-only recompute rather than the full one. |
| `TestCommittedAttachTypeChangeHandlerRunsForAnOpenCursor` | `attach operator Op to T on { change }`, committed, makes the handler run for an already-open cursor. Written behaviourally (does the handler change the value) rather than by reading the flag. This is also the case that proves the counter cannot be an existing timestamp: `CreateEventHandlerNode` and `AlterEventHandlerNode` bump none of the other four. |

The four shapes the ticket measured as already passing (a restriction over the table, forward or rolled back; a view over a rolled-back column change) get no test: the gate is never closed there, because a plan-local table variable starts with every flag raised, so a test would pin an accident of the plan rather than a behaviour.

**All six were confirmed to be real reproductions**, not vacuous passes: with the three `EnsureShouldCallProposables` calls in `TableNode.cs` temporarily commented out and the solution rebuilt, all six fail; restored, all six pass. The engine source was confirmed back to its intended state afterwards (`grep` for the temporary marker returns nothing, the three calls are at `TableNode.cs:1957`, `:2092` and `:2147`).

No other test was added. The fix is one mechanism at one site; a second test of the same mechanism through a different statement would be the same behaviour twice.

## What was run

Build: `dotnet build Dataphor.slnx` - succeeded, 0 errors, no new warnings (the 143 warnings are the pre-existing CA1416/SYSLIB/CA2022 set; the obsolete-`Assert.Catch` warnings come from `ApplicationTransactionTest`, not from the new file).

Targeted fixtures, as the ticket named them:

```
dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~TypeChangeSummaryTest|FullyQualifiedName~ColumnChangeRollbackTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~CursorTest|FullyQualifiedName~TableAssignmentEvaluationTest" --logger "console;verbosity=normal"
```

112 tests, 112 passed, 35 s. `CatalogRollbackSnapshotTest`'s whole matrix and `ColumnChangeRollbackTest`'s three cases are green unchanged.

Coverage scripts for every area the diff touches - the proposal path (defaults, checks, change handlers on tables, columns and types), the catalog undo path, and every DDL statement's before-execute:

```
dotnet test tests/DAE.Server.Tests --filter "Name=EventHandlers|Name=Constraints|Name=Types|Name=Tables|Name=Views|Name=Domains|Name=TransitionConstraints|Name=TransitionConstraintWithNamedRowVariables|Name=SerializationConstraints|Name=SerializationEventHandlers|Name=DeferredConstraintChecking|Name=DeferredConstraintsWithKeys|Name=TestAdornedConstraints|Name=Transaction.Atomicity.Cleanup|Name=Transaction.Atomicity.Run|Name=Transaction.Atomicity.SetOne|Name=Transaction.Atomicity.SetTwo|Name=Transaction.Atomicity.TestDAE|Name=Transaction.Atomicity.TestDAEMSSQL|Name=Transaction.Atomicity.TestMSSQL|Name=Transaction.Atomicity.TestMSSQLDAE|Name=Transaction.Isolation.Run|Name=ApplicationTransactions|Name=DistributedTransactions" --logger "console;verbosity=normal"
```

24 cases: 16 passed, 0 failed, 3 fragments (run only as part of other scripts), 5 known failures all matching their recorded tickets and none unexpectedly passing - `test-pre-3-1-scripts-policy` (Domains), `feat-ws7-devices-and-libraries` (the three MSSQL atomicity cases), `debt-memory-device-transaction-isolation` (Transaction.Isolation.Run). 1.1 minutes. No pre-existing failure outside this set was met, so `tickets/.pre-existing-error.md` was not written.

The full suite was not run: CI runs it in Debug and Release on every push, and this change does not cut across the build or test infrastructure, the lexer, the parser or the compiler.

## What a reviewer should push on

- **Is the two-bump-site claim actually complete?** The whole design rests on "every statement that changes the catalog is a `DDLNode`, and every rollback that undid DDL goes through `InvalidateCachesAfterUndo`". The first half was taken from the ticket and checked only by reading `PlanNode.Execute` (one `InternalBeforeExecute` hook, overridden by `DDLNode` and `TableNode` only). A path that changes a type's constraints, default or handlers *without* executing a `DDLNode` - a direct `CatalogDeviceSession` call from a device, from the application-transaction machinery, or from a library-loading path - would be a third site. Worth grepping `CatalogDeviceSession` for the mutators of `ScalarType.Constraints`, `ScalarType.Default` and `ScalarType.EventHandlers` and asking who else reaches them.
- **The view's raise-only recompute never lowers a flag.** `TestCommittedCreateTypeConstraintRefusesThroughAView` pins the raise. The matching lower - a committed `alter type T { drop constraint C }` leaving a view with `ShouldValidate` raised - is untested and is *by design* left raised, since a raised flag costs a call that finds nothing to do. If a reviewer disagrees that this is acceptable for a view, the alternative (recording which flags a compilation inferred, so a reset can restore them) is a larger change than this ticket.
- **The `ResultTableVar` no-op.** Argued from the fact that plan-local flags start raised and only the node's own propagation rules lower them, so nothing a catalog definition does can make them stale. Verified by reading every writer of `ShouldChange`/`ShouldDefault`/`ShouldValidate` outside `Schema/TableVar.cs` (`DifferenceNode`, `ExtendNode`, `JoinNodes`, `QuotaNode`, `RedefineNode`, `RenameNode`, `RestrictNode`, `UnaryTableNode`, `UnionNode` - all on their own `ResultTableVar`). A reviewer who finds a tenth writer that touches a catalog object's flags would break the argument.
- **The cost claim is inherited, not re-measured.** The 5.65 us recompute against 59.6 us per whole-row `Default` proposal comes from the ticket's own measurement at `21f5105d`; this stage did not re-measure it, and the `NOTE:` at the bump site states it as measured. `PerfBaselineTest` was not run.
- **Whether `DetermineRemotable` needs its own "do not stamp here" comment.** The explanation sits on `DetermineShouldCallProposables`, which is where a reader would most likely add the stamp. `BaseTableVar.DetermineRemotable` and `DerivedTableVar.DetermineRemotable` are the other two plausible places and carry no such note; a reviewer may want one line on each.
- **Error-message matching in the new tests.** Three cases assert `Does.Contain("IsPositive")` on the exception text, copying `ColumnChangeRollbackTest`'s style. That couples them to how a type-constraint violation is reported. It is the same coupling the sibling fixture already has, so it was not changed, but it is worth a reviewer's opinion.

## Tripwires parked (not tickets)

- **`Schema.TableVar.EnsureShouldCallProposables`'s remarks** - the three flags share one bit field with the rest of the table variable's characteristics, and a catalog table variable is shared by every session, so two sessions recomputing at once are two unsynchronized read-modify-writes of that field. They write the same values, and compiling any expression over the table has always done the same thing from any session, so this adds writers at the rate compilations already do rather than a new kind of race. If a lost characteristic bit is ever seen under concurrency, the field needs interlocked updates or the table variable a lock - not this call removed.
- **`DDLNode.InternalBeforeExecute`** - one counter for the whole catalog, so any DDL makes every table variable recompute once on its next read. Cheap and DDL is rare; if a workload that interleaves DDL with editing ever shows it, narrow the counter to the objects a statement touched.
- **`Schema.ResultTableVar.RecomputeShouldCallProposables`** - the no-op is safe only while a plan-local summary depends on nothing but the compilation that built it. A node taught to derive a flag from a catalog definition that can change under an open plan would make it the stale one.
- **`CatalogRollbackSnapshotTest`'s remarks** - a new bullet beside its other recorded omissions, saying why its matrix does not pair a type change with a table that has a column of that type: the dependent tables' *stored* flags are now deliberately left alone, so a case there would report a difference the engine no longer acts on, and `TypeChangeSummaryTest` pins the behaviour instead.

## Known gaps

- **No test covers the loading path.** The bump sits outside the `InLoadingContext` guard, so it also fires while a library loads. Argued harmless (one recompute per table variable, and the summary is recomputed from the real definition anyway) and exercised incidentally by every fixture that starts a server, but nothing asserts it.
- **No test covers two sessions.** Every case is one session, one process. The counter is per catalog, so a DDL in one session invalidates the other's summaries; that follows from the design but is not pinned.
- **The remote-client path is untouched and unverified.** `ServerProcess.CopyDataType` still reads the *stored* flags at prepare time, so a remote client's copy of a table variable is as stale after a type change as it was before this fix - the client is told once, at prepare, and nothing re-tells it. That is outside this ticket's defect (an open form over an in-process or already-prepared plan) but it is the same cache seen from the other end, and a reviewer may judge it a ticket.
