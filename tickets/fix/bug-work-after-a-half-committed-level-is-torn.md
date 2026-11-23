description: If a transaction's commit fails after part of it has already been saved, the session is still allowed to carry on working in that transaction, and that later work then gets kept in some of the places it touched and thrown away in the others. The session should be told the transaction can only be rolled back instead.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerTransaction.cs, src/engine/DAE/Server/ServerException.cs, tests/DAE.Server.Tests/PartialCommitTest.cs, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
difficulty: medium
----

## The situation this is about

One Dataphor transaction can reach several places at once: the in-memory store, each SQL database the session has used, Dataphor's own store of its table, type, operator and user definitions, and any other Dataphor server the session has queried. The commit asks each of them in turn and there is no protocol that can undo one that has already said yes, so a commit that fails part-way leaves some of them holding the transaction committed and the rest still holding it open.

`bug-partial-commit-rolls-back-committed-participants` made the clean-up after such a failure correct: the rollback now ends the failed transaction only where it is still open, so the *enclosing* transaction's work survives, and a retried commit or a nested begin is refused with error 106213 rather than appearing to succeed.

What that left behind is this: the session may still **run statements** in the transaction whose commit failed. Those two refusals both sit at the point a transaction level is begun or ended, and an ordinary statement begins no level, so nothing stops it.

## What goes wrong

The places that already committed are now one level shallower than the session. Each of them files new work against whatever level it is holding innermost - which is the failed transaction on the ones that did not commit, and the *enclosing* transaction on the ones that did. So one statement's work is split: the rollback that follows undoes it in the first group and keeps it in the second, and a commit of the enclosing transaction then makes that half permanent.

The session gets no error. Nothing in the log says the statement was torn.

### Reproduced, 2026-11-23

In `tests/DAE.Server.Tests/PartialCommitTest.cs`, whose setup already arranges a transaction over two in-memory devices (`GoodTable` on an ordinary one, `FailTable` on one whose commit can be made to fail on command), two levels deep with a row inserted at each level into both tables, and the inner commit made to fail on the second device. Then, before rolling back:

```
insert row { 3 ID } into GoodTable; insert row { 3 ID } into FailTable;
```

Measured immediately afterwards: the session held 2 levels, the working device 1 and the failing device 2 - so the statement's two inserts went to *different* levels. After rolling back the failed level and committing the enclosing one, `GoodTable` held rows 1, 2 and 3 and `FailTable` held only row 1: the statement's row survived on one device and was gone from the other, permanently. Rolling back both levels instead gives the mirror image - the row stays on the working device and is gone from the failing one.

## A second arm at the same site, read from the code and not run

The engine runs a transaction's **deferred event handlers** inside a nested transaction of its own during the commit's prepare phase (`ServerTransaction.InvokeDeferredHandlers`). If *that* nested commit fails part-way, the handlers' work has been committed on the places that said yes; the enclosing commit then fails in its prepare, which by design leaves it retryable, and the retry runs every handler again - a second time on the places that already have its work.

Same root as the arm above (work landing on a level some participants no longer hold), same site, so one ticket with two arms. This one has not been reproduced; it needs a partial commit to happen inside the deferred-handler transaction specifically.

## What the fix has to decide

The shape that retires the whole class is one rule: **a transaction level whose commit has already begun asking participants to commit accepts no new work - only a rollback.** The engine already records exactly that state, on `ServerTransaction.CommitBegun`, and already refuses two operations on it with error 106213; this would be the third.

The question the fix has to settle is **which calls must still get through**, because a rollback is not only an API call:

- `IServerProcess.RollbackTransaction` must always work. It does not run as a statement, so it is unaffected by a refusal placed at the statement boundary.
- The D4 statement `rollback transaction;` **does** run as a statement - it goes through `ServerProcess.BeginTransactionalCall` like any other - so a blanket refusal there would leave a session that drives transactions from D4 with no way out at all. Either the refusal is placed where it can tell that statement apart, or it is placed somewhere other than the statement boundary.
- A read-only statement is also torn in no way at all, so refusing it is a judgement about predictability rather than about correctness; decide deliberately rather than by accident of where the check lands.
- Whatever is refused should be refused with the existing error 106213, whose text already says the transaction can only be rolled back.

The second arm needs its own answer: either the deferred-handler transaction's partial failure must poison the enclosing transaction too (it currently does not, because `CommitBegun` is deliberately set *after* the prepare so that an ordinary deferred-constraint violation stays retryable), or the handler list must record which handlers have already been applied.

## Expectations to pin

- A statement issued in a transaction whose commit failed part-way is refused with error 106213, or - if the fix chooses otherwise - is documented as safe and shown to be so.
- `rollback transaction;` as a D4 statement still works after such a failure, and leaves the session able to carry on. This is the case most likely to be broken by the fix and the one that most needs a test.
- `IServerProcess.RollbackTransaction` still works after such a failure (already covered in `PartialCommitTest`).
- A commit that fails in its *prepare* phase has committed nothing and is still retryable, and running statements after it is still allowed. This must not change; `PartialCommitTest.TestACommitThatFailsInItsPrepareStaysRetryable` pins it.

## Notes for whoever takes this

The seam that makes it testable already exists and needs nothing new: `PartialCommitTest.FailingCommitDevice` is an in-memory device whose commit and prepare each fail once when told to, registered into the running engine at run time through the public `Catalog.ClassLoader.Classes` list. The reproduction above is three lines added to that fixture.

`src/engine/DAE/Server/ServerTransaction.cs` carries a `NOTE:` in `CommitBegun`'s remarks naming this ticket, and `docs/modernization-plan.md`'s K40 row records it as the part of that defect left open.
