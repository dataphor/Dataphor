description: A statement that changes data in an application transaction and then leaves it, all in one statement (for example one operator call or one begin…end block), still silently loses those changes; make leaving in that case fail with the same error as a leave inside an open transaction.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs, tests/DAE.Server.Tests/ApplicationTransactionTest.cs, docs/migration-notes.md
repro: static
----
<!-- resume-note -->
RESUME: A prior agent run on this ticket did not complete.
  Prior run: 2026-11-19T00:20:33.549Z (agent: claude)
  Log file: c:\Projects\DataphorModernized\modernization\tickets\.logs\engine-at-leave-same-statement-work.implement.2026-11-19T00-20-33-549Z.log
Read the log to see what was done. Resume where it left off.
If the prior run hit a timeout or repeated error, be cautious not to rush into the same situation.
<!-- /resume-note -->

## Decision (owner, 2026-11-18): refuse (the proposed rule)

The owner's rule: if the case is contrived, make it fail; if real code does it, look at that use first. No D4 in the repository changes data and leaves in the same statement: every `LeaveApplicationTransaction()` call (the `ApplicationTransactions` coverage script, `Sample.Shipping`'s `RunTest.dfd`) is a statement of its own, and the Frontend client leaves through the call interface. So the case is contrived, and the **Proposed rule** below is adopted.

What to do:

- `LeaveApplicationTransaction` also raises 125111 (reuse the existing code and message; no new code) when the leaving statement's own implicit transaction has already changed anything in the application transaction: a recorded operation (including nested transactions committed into it) or a row written to one of the application transaction's tables (including rows copied in when a table joined it). A leave in a statement that changed nothing goes ahead as now. Check before anything is disconnected, so the process stays joined and its work is intact, as the existing check does.
- Both leave paths (D4 operator and `IServerProcess` call), as for `engine-at-leave-discards-transaction-work`.
- Tests in `ApplicationTransactionTest.cs`: the `begin … end` block under *The remaining loss* now raises 125111, after which the process is still joined and the inserted row is still visible; a statement that joins, reads and leaves in one block still succeeds; the existing batch-by-batch leave still succeeds.
- Full `dotnet test Dataphor.slnx` green. If a `pass` coverage script fails because of the refusal, that is a real use: don't edit the script; route this ticket to `blocked/`, naming the script and the statement, so the owner can look at it.
- `docs/migration-notes.md`: extend the 125111 entry to cover this case. Update the `NOTE:` at the leave site to state the rule.

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
