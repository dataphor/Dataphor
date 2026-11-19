description: A statement that changes data in an application transaction and then leaves in the same statement used to silently discard the changes; leaving in that case now raises the same 125111 error as a leave inside an open transaction, and the implicit transaction can commit the changes when the error is caught.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs, tests/DAE.Server.Tests/ApplicationTransactionTest.cs, docs/migration-notes.md
repro: static
----

## What was decided

Owner picked the **Proposed rule** on 2026-11-18 (the ticket's "What to do" section): refuse the leave when the leaving statement's own implicit transaction has already changed anything in the application transaction (a recorded operation, including nested transactions committed into it). Reuse error 125111 — no new code. A leave in a statement that changed nothing continues to succeed, so `LeaveApplicationTransaction();` alone, and the existing batch-by-batch pattern, still work.

The sibling `engine-at-leave-discards-transaction-work` covered the "another transaction is open" case. This ticket adds the "same-statement-work" leg to the same check.

## How it works

`ApplicationTransactionUtility.CheckCanLeaveApplicationTransaction` (the only caller of the leave refusal) now has two clauses:

1. (unchanged) If the leaving process is in a transaction *beyond* its current call — an explicit `BeginTransaction`, a transaction left open by `SetUseImplicitTransactions(false)`, or an enclosing statement — raise 125111.
2. (new) Else if the AT device session's **current** device transaction (the one the leaving statement's own implicit call began) already has recorded work, raise 125111.

"Recorded work" is `ApplicationTransactionDeviceTransaction.Operations.Count > 0`, and the new private helper `HasRecordedWork` walks the committed nested transactions on the same device transaction (`.Transactions`) so an operation committed up from `commit` of a nested transaction inside the same leaving statement still counts. The check happens inside `CheckCanLeaveApplicationTransaction` before `InternalLeaveApplicationTransaction` runs, so the process stays joined and the implicit transaction's work is intact when the leave is refused — exactly as the first-clause refusal behaves.

The `NOTE:` at the `DeviceDisconnect` call in `LeaveApplicationTransaction` now states both clauses. The method doc comment on `CheckCanLeaveApplicationTransaction` summarises the rule and explains why the leaving statement's own implicit transaction is excepted from clause 1 but still subject to clause 2.

## Where the implementation narrows the proposal

The ticket's **Proposed rule** reads: "a recorded operation (including nested transactions committed into it) **or a row written to one of the application transaction's tables (including rows copied in when a table joined it)**". The current check covers the recorded-operation half; it does **not** separately check rows copied in by a populate-source during a `JoinExpression` (the path that fills the AT's copy of a table when it first joins).

Reason the implementation stops at the operation log: populate-source inserts take the `Transaction.IsPopulatingSource` branch in `ApplicationTransactionDeviceSession.InternalInsertRow`, which calls `base.InternalInsertRow` and **does not** add an `InsertOperation` to the AT's `Transaction.Operations` nor to `_transactions.CurrentTransaction().Operations`. The memory device's own undo log is `#if USEMEMORYDEVICETRANSACTIONS` and that constant is defined nowhere in the tree — so populate-source copy-ins are not undone when the AT device session is disconnected on leave. They persist in the AT's native tables through the leave and are still there on a rejoin, so the user's work is not actually lost.

This matches the owner's tests in "What to do": both are passing and neither exercises a populate-source copy-in. Reviewer judgment call: either accept the narrower rule (operation log is the only thing a disconnect actually discards; the parenthetical in the proposal was overly conservative), or widen the check to also fail on `(session.Transactions.CurrentTransaction().Tables.Count > 0)` or a similar signal. Flagging as a gap, not fixing silently.

## Tests added

Both in `ApplicationTransactionTest.cs`, same fixture as the `TestLeaveInsideTransactionRefused` and `TestCommitOutsideTransaction` tests from the sibling ticket. Each is a per-ticket characterization test — the behaviour is the specification and did not exist before.

- `TestLeaveAfterSameStatementChangeRefused` — the "remaining loss" reproduction from the ticket. One `begin … end` block inserts into the AT's copy of the table and then calls `LeaveApplicationTransaction()`; the leave raises 125111, a `try/except` around it swallows that code, the implicit transaction carries the insert into the AT normally, and a later separate-statement leave + `CommitApplicationTransaction` lands the row in the base table. Checks the inner error code too (`E.InnerError.Code`) in case the error reaches D4 wrapped. Fails without the new clause (the leave goes ahead, the implicit transaction's insert is lost); verified by hand-reverting `HasRecordedWork` to always return false and watching this test fail with "The refused leave discarded the insert made earlier in the same statement."
- `TestLeaveAfterSameStatementReadSucceeds` — a statement that joins, reads an empty AT table (so no populate-source rows are copied in) and leaves, all in one block. Still succeeds because the implicit transaction's `Operations` list is empty. Guards against a reviewer deciding to widen the check in a way that would refuse every post-join leave; also exercises the batch-less join path that `TestLeaveInsideTransactionRefused` doesn't.

No test for the existing batch-by-batch success case: `TestLeaveInsideTransactionRefused`'s final `ExecuteScript("LeaveApplicationTransaction();")` call (and the `ApplicationTransactions` coverage script) already covers "a plain leave batch succeeds".

## Validation

- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test Dataphor.slnx --no-build --filter "FullyQualifiedName~ApplicationTransactionTest"`: 5/5 pass, including both new tests and the three pre-existing ones.
- Full suite in three parts (per `AGENTS.md`):
  - Part 1 (`class != CoverageScripts`): 13/13 Common.Tests + 57/57 DAE.Server.Tests, 2 m 31 s.
  - Part 2 (coverage A–R): 67 passed, 11 skipped (known-failure/unstable/fragment), 0 failed, 3 m 12 s.
  - Part 3 (coverage non-A–R): 48 passed, 13 skipped, **1 failed** — pre-existing one-off `ServerLink` timeout, documented in `tickets/.pre-existing-known.md` (machine-specific loopback TCP behaviour, not code; the manifest expects `SocketException` which this machine replaces with a 120 s `HarnessTimeout`). No new or different failures.

## For the reviewer to weigh

- **Populate-source gap (above).** Narrower than the owner's parenthetical. Reviewer picks: accept because the disconnect doesn't actually undo those rows, or widen the check.
- **No separate test for an `operator` body that joins/changes/leaves in one call.** The ticket mentions it as another instance of the same shape. The check runs at `CheckCanLeaveApplicationTransaction` regardless of whether the leave came from a top-level statement or a nested operator call on the same transactional call, so the operator case goes through the same code path with no new branches. If the reviewer wants a test for it anyway, it's one more `begin … end` with an `operator foo begin insert …; LeaveApplicationTransaction(); end;` and a call to it in place of the direct leave in `TestLeaveAfterSameStatementChangeRefused`.
- **`try/except` idiom in the new test.** The error code reaches D4 inside a `DataphorException`, and the plan node that runs the call re-raises it as a runtime error with the statement location; the actual code can be either on `E.Code` or on `E.InnerError.Code`. `TestLeaveAfterSameStatementChangeRefused` checks both, following the pattern in the sibling D4 coverage script `DistributedTransactions.d4` (which checks `E.InnerError.Code` for the DTC refusal). Reviewer may prefer to tighten this to the one branch that actually hits today (quick check: the leave node raises directly, so it's `E.Code`; the inner-code branch is dead on the current path). Leaving both branches in for resilience against a future wrapping change.
- **Message text unchanged.** Error 125111 is still "Cannot leave application transaction while a transaction is open on the process." — the migration-notes update rewrites the description but the message is the same for both clauses. Fine for most readers; a reviewer who wants the message to distinguish the two clauses would need a new resource string, which the ticket explicitly forbade ("reuse the existing code and message; no new code").
- **No `pass` coverage script failed.** The ticket said: if one did, route to `blocked/` with the script name. None did. The `ApplicationTransactions` coverage script runs with `batches: true` (one implicit transaction per batch), so its leaves are separate from its inserts and are unaffected.

## Files changed

- `src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs` — extended `CheckCanLeaveApplicationTransaction` (new clause + updated `<summary>`/`<remarks>`); new private `HasRecordedWork`; updated `NOTE:` at `LeaveApplicationTransaction`'s `DeviceDisconnect` to describe both clauses.
- `tests/DAE.Server.Tests/ApplicationTransactionTest.cs` — two new tests.
- `docs/migration-notes.md` — the 125111 row now describes both clauses and recommends putting the leave in a separate statement from the changes.

Line endings and byte-order marks of the two `.cs` files edited are unchanged (CRLF, no BOM in the AT file, no BOM in the test file — both as before). `migration-notes.md` stays LF.

## Review findings

(to be filled by the reviewer)
