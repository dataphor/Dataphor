description: When a process leaves an application transaction while one of its ordinary transactions is still open, the engine silently throws away the changes it made in the application transaction during that open transaction, even if the open transaction later commits. Decide what leaving should do in that case: keep the work, refuse to leave, or stay as it is.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs, src/engine/DAE/Device.ApplicationTransaction/ApplicationTransactionException.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Schema/Device.cs
repro: verified
----
**Blocked: silent specification (D4 semantics).** Unblocks when a human picks one of the behaviours below (or edits the proposed wording); the choice changes D4 behaviour that applications could notice, so the project rules route it here rather than into a fix.

## What happens

An application transaction (AT) is the engine's long-running edit space: `BeginApplicationTransaction()` creates one, `JoinApplicationTransaction(id, false)` makes the process's reads and writes of tables go to the AT's private copies, `LeaveApplicationTransaction()` returns the process to the real tables, and `CommitApplicationTransaction(id)` later replays the recorded changes onto the real tables.

If the process leaves while it has an ordinary (process) transaction open, every change it made in the AT during that open transaction is lost. The AT copy no longer has the rows, and the AT no longer has the operations to replay at commit. Nothing reports it, and committing the process transaction afterwards does not bring the work back.

Reproduced in-process (scratch test, since removed) on a process with implicit transactions off, each line run as its own batch through `IServerProcess.ExecuteScript`, as a client or Dataphoria would:

```
create table T { ID : Integer, key { ID } };
create table A { ID : Guid, key { } };
A := table { row { BeginApplicationTransaction() ID } };
SetUseImplicitTransactions(false);
BeginTransaction();
create table Z { ID : Integer, key { ID } };   // only so the catalog store transaction is already open (see store-at-enlist-inside-catalog-lock)
JoinApplicationTransaction(A[].ID, false);
insert row { 1 ID } into T;
if not exists (T) then raise Error("A");        // passes: the row is in the AT copy
LeaveApplicationTransaction();
if exists (T) then raise Error("C");            // passes: the real table is untouched
JoinApplicationTransaction(A[].ID, false);
if not exists (T) then raise Error("D");        // FAILS: the AT copy is empty again
```

The same happens when a script runs nested inside one statement (the D4 `Execute` and `ExecuteScript` operators), because the outer statement's implicit transaction stays open across the whole nested script. That is how the coverage harness used to run the `ApplicationTransactions` script, and how the `StreamAllocation` coverage script runs every script in its library. With implicit transactions on and each statement run as its own batch (the usual way), each batch's implicit transaction has committed before the leave, so nothing is lost.

## Mechanism (traced)

`ApplicationTransactionUtility.LeaveApplicationTransaction` (`ApplicationTransaction.cs`, about line 150) calls `process.DeviceDisconnect(transaction.Device)`; the comment says this is "to ensure that the saved pointer to this AT is cleared" (the session caches its `ApplicationTransaction` in `_transaction`). Disconnecting disposes the process's `ApplicationTransactionDeviceSession`. Its `Dispose` (about line 2230) and the base `DeviceSession.Dispose` (`Schema/Device.cs`, about line 1580) call `EnsureTransactionsRolledback()`, which rolls back every device transaction the session still has open: the base rollback undoes the row writes in the AT copies, and `ApplicationTransactionDeviceSession.InternalRollbackTransaction` removes the recorded operations from the AT's operation log. A stack trace taken at that rollback during the reproduction above shows `LeaveApplicationTransaction → DeviceDisconnect → Device.Disconnect → Dispose → EnsureTransactionsRolledback → RollbackTransaction` with 2 operations in the rolled-back transaction.

## Is it legacy behaviour?

Yes. The leave, disconnect and dispose code is unchanged since tag `v3.1` (the only later commit to `ApplicationTransaction.cs`, `ab33bb27`, removes unused exception variables and adds a byte-order mark). The legacy NUnit test for the `ApplicationTransactions` script was ignored from 2007 with "Doesn't work through ExecuteScript", and the script's header says its batches must run as a whole script on one process. So this is not a porting defect. It is still silent data loss for any application that leaves an AT inside an explicit transaction, which is why it is raised rather than only recorded.

## Proposed behaviour (recommended default)

> **Leaving an application transaction keeps the process transaction's work.** When a process leaves an application transaction while a process transaction is open, the changes it made in the application transaction during that transaction stay pending: they become part of the application transaction if the process transaction commits, and are discarded if it rolls back, exactly as if the process had stayed joined until the transaction ended.

Implementation sketch, for sizing only: on leave, if the AT device session is in a transaction, do not disconnect it; detach it from the process's AT (clear the cached pointer, keep a direct reference to the AT for commit and rollback) and disconnect it when the outermost process transaction ends. The session's rollback path currently finds the AT through `ServerProcess.ApplicationTransactionID`, which is empty after the leave, so it must use the kept reference. Rejoining the same AT before the transaction ends has to reuse that session rather than connect a second one.

## Alternatives

- **Refuse to leave.** `LeaveApplicationTransaction()` raises a new `ApplicationTransactionException` (next free code in the 125xxx range is 125111, "Cannot leave application transaction while a transaction is open on the process") when the AT session has an open transaction. Simple and makes the loss visible; but it turns scripts that today "work" (losing nothing because they made no AT changes in that transaction) into errors unless the check is limited to transactions that recorded operations, and nested scripts such as the coverage library's `StreamAllocation` could never leave and rejoin.
- **Keep the current behaviour and document it.** No code change; add the rule to the D4 reference documentation for `LeaveApplicationTransaction` and keep the `NOTE:` at the code site. Zero compatibility risk; the loss stays silent.

Rejected: committing the AT session's work into the AT at leave regardless of the process transaction. It keeps the rows, but a later rollback of the process transaction would no longer undo them, which breaks the atomicity of the process transaction.

## If we do nothing

Applications that leave an AT inside an explicit transaction keep losing work silently, as they did on 3.1. The coverage harness avoids the case by running `ApplicationTransactions` batch by batch (`engine-at-leave-rolls-back-open-work`), and `StreamAllocation` cannot pass while it nests that script (its future is with `test-pre-3-1-scripts-policy`, which proposes retiring it).

## Reversibility

The recommended behaviour only keeps work that is lost today, so no correct application can depend on the loss; it is reversible in code. The "refuse" alternative adds an error code, which is a compatibility surface and harder to take back. Any change needs an entry in `docs/migration-notes.md`.
