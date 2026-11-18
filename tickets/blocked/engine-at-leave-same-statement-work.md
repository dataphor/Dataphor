description: A statement that changes data in an application transaction and then leaves it, all in one statement (for example one operator call or one begin…end block), still silently loses those changes; decide whether leaving in that case should also fail with an error.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, tests/DAE.Server.Tests/ApplicationTransactionTest.cs, docs/migration-notes.md
repro: static
----
**Blocked: the specification is silent.** The owner decision recorded in `engine-at-leave-discards-transaction-work` ("refuse whenever the application transaction's session has an open transaction") cannot apply to a statement's own implicit transaction, and that case still loses work. Unblock by accepting or editing the proposed rule below.

## Background

An application transaction (AT) is the engine's long-running edit space: a process joins it, its table reads and writes go to the AT's private copies, it leaves, and a later commit replays the recorded changes onto the real tables. Leaving disconnects the process's session on the AT device, and disconnecting rolls back every transaction still open on that session, so any AT changes made during an open transaction are lost.

`engine-at-leave-discards-transaction-work` made `LeaveApplicationTransaction` (D4 operator and `IServerProcess` call) raise error 125111 instead. With implicit transactions on (the default), every statement runs inside an implicit transaction that it begins itself, including the statement `LeaveApplicationTransaction();`. Refusing whenever any transaction is open would make leaving impossible, so the check ignores the implicit transaction of the statement doing the leave. It refuses only when some other transaction is open: one begun with `BeginTransaction`, one left open by `SetUseImplicitTransactions(false)`, or an enclosing statement's (a leave inside `Execute`/`ExecuteScript`).

## The remaining loss

Changes made earlier in the **same** statement as the leave belong to that statement's implicit transaction, and are still discarded without an error. For example, with the process joined and implicit transactions on:

```
begin
	insert row { 1 ID } into T;     // goes into the AT copy, inside this statement's implicit transaction
	LeaveApplicationTransaction();  // allowed; the disconnect rolls the insert back
end;
```

The same applies to a D4 operator whose body joins, changes data and leaves, called as one statement. Inferred from the code path (the disconnect's rollback covers every open transaction on the AT session); not run. To confirm, run the block above batch by batch on one process after a join, then rejoin and check that `T` is empty.

## Proposed rule (recommended default)

> `LeaveApplicationTransaction` also raises 125111 when the leaving statement's own implicit transaction has already changed anything in the application transaction: an operation recorded for replay, or a row written to one of the application transaction's tables (including rows copied in when a table first joined it). A leave in a statement that has changed nothing in the application transaction goes ahead as now.

This is the only way to refuse this case without refusing every leave, but it makes the check depend on what the transaction did, which the owner preferred to avoid for the general rule. The check would read the AT session's current device transaction: its recorded operations (`ApplicationTransactionDeviceTransaction.Operations`, plus the nested transactions committed into it) and the memory device's undo log for the same transaction.

## Alternatives

- **Document it.** No code change: the D4 reference and the developers' guide say to leave in a separate statement from the changes. Zero risk; the loss stays silent in this one shape.
- **Keep the changes for this case.** For the leaving statement's own implicit transaction only, postpone the disconnect until that transaction ends, so its changes become part of the AT when it commits. This is the "keep the work" design the owner declined in general, restricted to a transaction that always ends at the end of the same statement, which makes the postponement short and local.

## If we do nothing

Code that joins, changes data and leaves inside one statement keeps losing those changes silently, as on 3.1. No shipped library does this today (the only D4 uses of the leave are the coverage script `ApplicationTransactions`, which leaves in its own batches, and `Sample.Shipping`'s test form, whose script also leaves in its own batch); the Frontend client joins and leaves through the call interface, which the new check already covers.

## Reversibility

The proposed rule reuses error 125111, so it adds no new error code, but it turns code that "works" today (and loses data) into an error; that needs a line in `docs/migration-notes.md`. The "keep" alternative only keeps work that is lost today.
