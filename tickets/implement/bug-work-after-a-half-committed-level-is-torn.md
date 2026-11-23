description: If a transaction's commit fails after part of it has already been saved, the session is still allowed to carry on working in that transaction, and that later work then gets kept in some of the places it touched and thrown away in the others. The session should be told the transaction can only be rolled back instead.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Schema/Device.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerTransaction.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/PartialCommitTest.cs, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
difficulty: hard
----

## The situation this is about

One Dataphor transaction can reach several places at once: the in-memory store, each SQL database the session has used, Dataphor's own store of its table, type, operator and user definitions, and any other Dataphor server the session has queried. The commit asks each of them in turn and there is no protocol that can undo one that has already said yes, so a commit that fails part-way leaves some of them holding the transaction committed and the rest still holding it open. Those two groups are then at different depths: the ones that committed are one level shallower than the session.

`bug-partial-commit-rolls-back-committed-participants` made the clean-up after such a failure correct. The rollback now ends the failed level only where it is still open, so the enclosing transaction's work survives; a retried commit and a nested begin are both refused with error 106213, whose text already says the transaction can only be rolled back; and `ServerTransaction.CommitBegun` is the record that a level reached that state.

What it left behind is that the session may still **do work** in that level. Both existing refusals sit where a level is begun or ended, and ordinary work begins no level.

## The rule this ticket implements

**A transaction level whose commit has already begun asking participants to commit accepts no new work on any participant - only a rollback.** Refuse that work with the existing error 106213.

The state is already recorded (`ServerTransaction.CommitBegun`), and the rollback already has its own record (`ServerTransaction.InRollback`), so the whole rule is one predicate asked at the places where work reaches a participant:

```
refuse when InTransaction && CurrentTransaction.CommitBegun && !CurrentTransaction.InRollback
```

The half-committed level is always the innermost one, because a nested begin on top of it is already refused, so `CurrentTransaction` is the right level to ask about. Once it has been rolled back the enclosing level is clean and work is allowed again, which is the behaviour `bug-partial-commit-rolls-back-committed-participants` deliberately kept (`PartialCommitTest.TestRollbackAfterAFailedDeviceCommitKeepsTheOuterLevel` pins it).

### Why not at the statement boundary

The obvious single site is `ServerProcess.BeginTransactionalCall`, which every statement, expression, cursor operation and transactional process call passes through. **Measured on 2026-11-23, it does not work, for two independent reasons.**

First, it would leave a session that drives transactions from D4 with no way out. `RollbackTransaction()` is an ordinary D4 operator invocation, so as a statement of its own it is a transactional call like any other, and a blanket refusal there refuses it. Both D4 rollback idioms were run on a half-committed level and both work today and must keep working: `RollbackTransaction();` took the process from 2 levels to 1, and `while TransactionCount() > 0 do RollbackTransaction();` took it from 1 to 0. The loop form is not a hypothetical - it is the error-recovery idiom in this repository, at `tests/libraries/TestFramework.Coverage.Base/Documents/All.Coverage.d4:53` - and it cannot be exempted by recognising a plan's root node, because its root is a `while`.

Second, a statement boundary is in the wrong place to catch the worst case. Run as **one** D4 statement, with the partial commit swallowed by the script's own error handler:

```
begin
  BeginTransaction();
  try
    insert row { 2 ID } into GoodTable;
    insert row { 2 ID } into FailTable;
    CommitTransaction();
  except
    var LSwallowed := 1;
  end;
  insert row { 3 ID } into GoodTable;
  insert row { 3 ID } into FailTable;
end;
```

there is no statement boundary between the failed commit and the work after it. Measured: afterwards `GoodTable` held 3 rows including the row-3 insert and `FailTable` held 1 row without it, and that tear was in the **enclosing** transaction, which then committed - so the torn row became permanent. (The statement did raise 106213, from the existing refusal in `InternalPrepareTransaction` meeting the implicit commit at the end of the call, and that rolled the failed level back; the damage had already landed one level out.)

### Where the check goes instead

At the seams where work reaches a participant. Each has the `ServerProcess` to hand, so each can ask one shared helper - put it on `ServerProcess`, next to the state it reads, and name it for what it checks.

- `src/engine/DAE/Schema/Device.cs`, four public methods of `Schema.DeviceSession`: `Execute`, `InsertRow`, `UpdateRow`, `DeleteRow`. All four are needed: `Execute` carries the device-supported plan nodes (SQL-device statements, reads), and the row-level three carry what `BaseTableVarNode.InternalInsert`/`InternalUpdate`/`InternalDelete` do on a device that supports row-level operations, which is the path a memory device's insert takes (`src/engine/DAE/Runtime.Instructions/TableNodes.cs:1888`, `:1924`, `:1941`) and therefore the path the reproduction below takes. Raise **before** the method's own `try`, the way the existing `CheckCapability` calls in `InsertRow` do, so the refusal does not go through `Schema.DeviceSession.WrapException` and cannot be mistaken for a connection or transaction failure that sets `StopError`.
- `src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs`, `AcquireCatalogStoreConnection` when `isUpdatable` is true. Every write to the catalog store funnels through it - 46 call sites - so this one place covers all of DDL, which does not pass through any of the four methods above.
- `src/engine/DAE/Server/ServerProcess.cs`, `RemoteConnect`, for work sent to another Dataphor server: it is the funnel for `Program.RemoteConnect`, which is how the three runtime sites reach a linked server (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs:1324` and `:1350`, `src/engine/DAE/Runtime.Instructions/TableNodes.cs:2562`). It also serves `Plan.RemoteConnect`, used at compile time by `TableNodes.cs:2505` to describe a remote table; refusing that too is harmless, since it only ever happens as part of a statement that would be refused anyway. If you would rather not refuse the compile-time path, check the three runtime sites instead and say in the handoff why.

The rollback exemption is load-bearing, not defensive: the catalog device's rollback writes the store (its replay of compensating operations reaches `InternalDeleteCatalogObject` and so `DeletePersistentObject`), and that runs while the half-committed transaction is current. `InternalRollbackTransaction` sets `InRollback` on it for exactly the span of the participant loops, and `RollbackBegunParticipants` does the same on the failed-begin path, so the flag already covers both.

One trap to know about rather than guard: `ValidatingBaseList.RemoveAt` disposes the item before it decrements the count, so while `ServerTransaction.Dispose` runs the transaction being ended is still the current one, with `CommitBegun` set and `InRollback` back to false. Nothing on that path touches a participant today - `UnprepareDeferredHandlers` only disposes native rows, and `ServerTransactions.UnprepareDeferredConstraintChecks` is reached only from `ServerProcess.Dispose`, after every transaction has been rolled back - but a future addition there would be refused. Leave a `NOTE:` where it will be met.

### Reads are refused too, deliberately

A read is torn in no way at all: measured on a half-committed level, `Count(GoodTable)` and `Count(FailTable)` both returned 2, which is consistent and correct. Refusing reads is therefore a choice about predictability, and this ticket makes it: one rule, refusing everything that reaches a participant, is simpler to hold than a rule that depends on a plan node being read-only, and the session's only useful next move is a rollback in either case. The cost is that a client whose error handler reads something in order to log it must roll back first. Record that in `docs/migration-notes.md`. If while implementing you find a caller inside the engine that must read on a half-committed level, that is evidence against this choice - say so in the handoff rather than quietly narrowing the check.

## Reproduced, 2026-11-23: the first arm

Added to `tests/DAE.Server.Tests/PartialCommitTest.cs`'s existing arrangement, which puts `GoodTable` on an ordinary in-memory device and `FailTable` on one whose commit can be made to fail on command, two levels deep with a row inserted at each level into both tables and the inner commit made to fail on the second device. Then, before rolling back:

```
insert row { 3 ID } into GoodTable; insert row { 3 ID } into FailTable;
```

Measured immediately afterwards: the session held 2 levels, the working device 1 and the failing device 2 - unchanged by the statement, so its two inserts went to *different* levels. Rolling back the failed level and committing the enclosing one then left `GoodTable` with rows 1, 2 and 3 and `FailTable` with only row 1: the statement's row survived on one device and was gone from the other, permanently.

Rolling **both** levels back instead left both tables empty - so that path is merely wasteful, not inconsistent. (The fix ticket guessed it would be the mirror image of the tear; it is not. It discards the enclosing level's work as well, which is correct if blunt.)

## Reproduced, 2026-11-23: the second arm

The engine runs a transaction's deferred event handlers inside a nested transaction of its own during the commit's prepare phase (`ServerTransaction.InvokeDeferredHandlers`). If *that* nested commit fails part-way, the handlers' work is committed on the participants that said yes; the enclosing commit then fails in its prepare, which by design leaves it retryable, and the retry runs every handler again - `InvokeDeferredHandlers` iterates its list and nothing removes what has been applied.

The fix ticket filed this as read-from-the-code. It is reproducible on the same fixture, and was reproduced: an after-insert handler on `GoodTable` with the `DAE.IsDeferred` tag set to `"true"`, writing one keyed row into a second table on the working device, with a row inserted into each of `GoodTable` and `FailTable` inside one transaction and the failing device armed. Measured: the commit failed with the device's commit failure; the process was left holding 1 level with both devices at 1, so the nested level had been rolled back correctly; the handler's row was **still there**, having been committed on the working device into the enclosing level; and the retried commit then raised `The table AuditTable already has a row with ID (1).` from inside the handler - the handler had run a second time. A handler that appends to a log or adds to a running total, rather than inserting a keyed row, would have doubled its effect with no error at all.

The fix: when the deferred-handler transaction's commit has failed part-way, the enclosing level can no longer be committed either, and must say so. In `InvokeDeferredHandlers`' catch, read the nested transaction's `CommitBegun` **before** rolling it back (the rollback removes the transaction object and with it the record), and if it was set, set `CommitBegun` on the enclosing transaction once the nested level is gone. The retried commit is then refused by the existing check in `InternalPrepareTransaction`, with error 106213, and the session must roll back - which is right, because nothing in that transaction can be committed safely any more.

Do not reach for this by setting `CommitBegun` earlier in the commit, before the prepare. That boundary is deliberate and load-bearing: a commit that fails *in* its prepare has committed nothing, which is the ordinary deferred-constraint failure, and it has to stay retryable. `PartialCommitTest.TestACommitThatFailsInItsPrepareStaysRetryable` pins it.

## Expectations to pin

- A statement issued in a transaction whose commit failed part-way is refused with error 106213, and nothing of it lands on any participant.
- The same when the failed commit and the work after it are inside one D4 statement, with the failure swallowed by the script's own error handler. This is the case that rules out the statement boundary; without it the fix looks finished when it is not.
- `RollbackTransaction();` as a D4 statement still works after such a failure, and so does `while TransactionCount() > 0 do RollbackTransaction();`, and the session is able to carry on afterwards.
- `IServerProcess.RollbackTransaction` still works after such a failure (already covered).
- A retried commit after a partial commit inside the deferred-handler transaction is refused with error 106213 rather than running the handlers again.
- A commit that fails in its *prepare* phase has committed nothing, is still retryable, and work after it is still allowed. Unchanged.
- The enclosing transaction still survives the rollback of a half-committed level and is still committable. Unchanged.

## Notes for whoever takes this

The seam that makes all of this testable already exists and needs nothing new: `PartialCommitTest.FailingCommitDevice` is an in-memory device whose commit and prepare each fail once when told to, registered into the running engine at run time through the public `Catalog.ClassLoader.Classes` list. Every measurement above was taken on it, in a temporary fixture that has been deleted; the cases below are those probes turned into assertions.

`src/engine/DAE/Server/ServerTransaction.cs` carries a `NOTE:` in `CommitBegun`'s remarks describing this defect and naming this ticket. Rewrite it to describe what the code now does, rather than leaving it as a forward reference.

`docs/modernization-plan.md`'s K40 row ends with a "Still open after this fix" paragraph naming this ticket. Replace that paragraph with a pointer to the new row, and add the new row as **K41** - the highest in the table today is K40.

`debt-remote-session-lazy-enlistment` (in `plan/`) will make a linked server's connection join a transaction only when a statement uses it. That changes what the remote arm of this check means, in the same way it changes `HoldsEndingLevel`, which already carries a `NOTE:` saying so. Add this check to that note rather than writing a second one.

## TODO

- Add the shared predicate to `ServerProcess` - `CommitBegun` on the current transaction, not `InRollback` - raising `ServerException.Codes.TransactionCanOnlyBeRolledBack` (106213). Document on it that it is the third refusal reading the same record, alongside `InternalBeginTransaction` and `InternalPrepareTransaction`.
- Ask it in `Schema.DeviceSession.Execute`, `InsertRow`, `UpdateRow` and `DeleteRow`, before each method's own `try`.
- Ask it in `ServerCatalogDeviceSession.AcquireCatalogStoreConnection` when `isUpdatable` is true.
- Ask it in `ServerProcess.RemoteConnect`.
- Leave a `NOTE:` where `ServerTransaction.Dispose`'s participant work would be refused, recording that the transaction is still current while it disposes.
- Fix the second arm in `ServerTransaction.InvokeDeferredHandlers`: capture the nested transaction's `CommitBegun` before the rollback, and set it on the enclosing transaction if it was set.
- Rewrite the `NOTE:` in `ServerTransaction.CommitBegun`'s remarks to describe the rule as implemented.
- `PartialCommitTest`: a case for the torn statement on the client API path; a case for the one-statement swallowed failure; a case for both D4 rollback idioms after a partial commit; a case for the deferred-handler retry. Each must be seen to fail before the fix - the measurements above say what each does today.
- Check that `PartialCommitTest.TestRollbackAfterAFailedDeviceCommitKeepsTheOuterLevel` and `TestACommitThatFailsInItsPrepareStaysRetryable` still pass, and that `TransactionBeginTest` and `ConnectionTransactionLevelTest` do too - they exercise the neighbouring paths on the same seams.
- `docs/migration-notes.md`: a statement, a read included, is now refused on a transaction whose commit failed part-way; and a commit retried after a partial commit inside the deferred-handler transaction is now refused rather than re-running the handlers.
- `docs/modernization-plan.md`: add K41 for this defect with the measurements above, and replace K40's "Still open after this fix" paragraph with a pointer to it.
- Build, then run the suite in the three parts from the root `AGENTS.md`, each as its own foreground command.
