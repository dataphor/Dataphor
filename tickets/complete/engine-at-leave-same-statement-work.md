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

## Where the implementation narrows the proposal (reviewed, accepted)

The ticket's **Proposed rule** reads: "a recorded operation (including nested transactions committed into it) **or a row written to one of the application transaction's tables (including rows copied in when a table joined it)**". The implementation covers the recorded-operation half; it does not separately check rows copied in by a populate-source during a `JoinExpression` (the path that fills the AT's copy of a table when it first joins).

Reviewed and accepted. Populate-source inserts take the `Transaction.IsPopulatingSource` branch in `ApplicationTransactionDeviceSession.InternalInsertRow` (`ApplicationTransaction.cs:2388`), which calls `base.InternalInsertRow` and does not add an `InsertOperation` to `Transaction.Operations` or to `_transactions.CurrentTransaction().Operations`. `InternalRollbackTransaction` (`ApplicationTransaction.cs:2309`) only reverses rows that are in the `Operations` list, and the AT's `Transaction.Tables` (returned by the override of `GetTables`, `ApplicationTransaction.cs:2369`) is owned by the `ApplicationTransaction` object, which outlives the device session. So the disconnect that follows a refused-then-forced leave does not discard populate-source rows: they remain in the AT's native tables and are still there on a rejoin. The parenthetical in the proposal was overcautious about this specific path.

## Tests added

Both in `tests/DAE.Server.Tests/ApplicationTransactionTest.cs`, same fixture as the sibling ticket's `TestLeaveInsideTransactionRefused` and `TestCommitOutsideTransaction`. Each is a characterization test for a behaviour that had no automated cover before:

- `TestLeaveAfterSameStatementChangeRefused` — the reproduction of "remaining loss" from the ticket. One `begin … end` block inserts into the AT's copy of the table and then calls `LeaveApplicationTransaction()`; the leave raises 125111, a `try/except` around it swallows that code, the implicit transaction carries the insert into the AT normally, and a later separate-statement leave + `CommitApplicationTransaction` lands the row in the base table. Fails without the new clause.
- `TestLeaveAfterSameStatementReadSucceeds` — a statement that joins, reads an empty AT table (so no populate-source rows are copied in) and leaves, all in one block. Still succeeds because the implicit transaction's `Operations` list is empty. Guards against a reviewer deciding to widen the check in a way that would refuse every post-join leave; also exercises the join path without the batch boundary used by `TestLeaveInsideTransactionRefused`.

No test for the batch-by-batch success case: `TestLeaveInsideTransactionRefused`'s final `ExecuteScript("LeaveApplicationTransaction();")` call and the `ApplicationTransactions` coverage script already cover "a plain leave batch succeeds".

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, 143 warnings (all pre-existing — `CA1416` on `Connection.OleDb` and `CS0618` on NUnit's `TestDelegate` in the fixture's helper).
- `dotnet test Dataphor.slnx --no-build --filter "FullyQualifiedName~ApplicationTransactionTest"`: 5/5 pass (two new tests plus the three from the sibling tickets).
- Full suite in three parts (per `AGENTS.md`):
  - Part 1 (`class != CoverageScripts`): 13/13 Common.Tests + 57/57 DAE.Server.Tests, 2 m 27 s.
  - Part 2 (coverage A–R): 67 passed, 11 skipped, 0 failed, 3 m 7 s.
  - Part 3 (coverage non-A–R): 48 passed, 13 skipped, **1 failed** — pre-existing one-off `ServerLink` timeout, documented in `tickets/.pre-existing-known.md` as machine-specific loopback TCP behaviour (the manifest expects `SocketException`, this machine replaces it with a 120 s `HarnessTimeout`). The record says "do not change the manifest entry or the script for it"; nothing to file.

## Files changed

- `src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs` — extended `CheckCanLeaveApplicationTransaction` (new clause + updated `<summary>`/`<remarks>`); new private `HasRecordedWork`; updated `NOTE:` at `LeaveApplicationTransaction`'s `DeviceDisconnect` to describe both clauses.
- `tests/DAE.Server.Tests/ApplicationTransactionTest.cs` — two new tests.
- `docs/migration-notes.md` — the 125111 row now describes both clauses and recommends putting the leave in a separate statement from the changes.

Line endings and byte-order marks are unchanged: both `.cs` files CRLF with no BOM; `migration-notes.md` LF.

## Review findings

Read the diff of `ticket(implement): engine-at-leave-same-statement-work` (committed earlier under `tess: timed out on engine-at-leave-same-statement-work — added resume note`, before the implement commit moved the ticket file) with fresh eyes, then the call paths it touches and the implementer's handoff. Ran lint (clean build) and the three-part test suite.

- **Correctness of the new clause — checked, no defect.** Traced the second clause: inside `CheckCanLeaveApplicationTransaction` (`ApplicationTransaction.cs:156`), after the `InTransactionBeyondCurrentCall` arm is false, the `ApplicationTransactionDeviceSession` cast reads `session.Transactions.CurrentTransaction()`. `_transactions.BeginTransaction` (`ApplicationTransaction.cs:2296`) pushes a fresh `ApplicationTransactionDeviceTransaction` on each `InternalBeginTransaction`, and `EndTransaction(true)` moves a committed inner transaction into the parent's `.Transactions` list (`ApplicationTransaction.cs:2240-2244`). `InternalInsertRow`/`InternalUpdateRow`/`InternalDeleteRow` (`ApplicationTransaction.cs:2385-2495`) add their operations to `_transactions.CurrentTransaction().Operations` when `InTransaction` is true. So `HasRecordedWork` sees both the leaving statement's own inserts and inserts committed up from a nested `begin transaction … commit transaction` inside the same leaving statement, which the new test covers.
- **The populate-source narrower rule — checked, accepted.** See the dedicated section above. The parenthetical in the proposal was overcautious: disconnecting does not actually discard populate-source rows because they are in `Transaction.Tables` (owned by the AT), not in `Operations` (owned by a device transaction). Widening the check (e.g. to `session.Transactions.CurrentTransaction().Tables.Count > 0`) would refuse every leave after a join-and-read of a non-empty table — a visible regression on the sibling's coverage path — and would fix no real data loss. `TestLeaveAfterSameStatementReadSucceeds` is deliberately there to pin this; keep it.
- **Operator-body case — checked, no separate test needed.** The check runs at `CheckCanLeaveApplicationTransaction` regardless of whether the leave is a top-level statement or inside a nested operator call on the same transactional call, so an `operator foo begin insert …; LeaveApplicationTransaction(); end;` reaches the same code path. Adding a test for it would restate the "same-statement-work" test at a different syntax tree — a wiring test that would not pay for itself.
- **Defensive `(session.Transactions.Count > 0)` guard — noted, left.** On line 167 this check is redundant with line 161's `InTransaction` (both read from a count that is incremented in lockstep by `BeginTransaction`). Harmless and self-documenting against a future divergence of the two counters; not worth touching.
- **Test defensive branch (`E.InnerError.Code`) — checked, left.** In `TestLeaveAfterSameStatementChangeRefused` the D4 `try/except` reads `E.Code` first and falls back to `E.InnerError.Code`. Today the leave node (`LeaveApplicationTransactionNode.InternalExecute`, `ApplicationTransaction.cs:492`) just forwards to `ServerProcess.LeaveApplicationTransaction`, which raises through `WrapException`, so the inner branch probably never triggers on the current path. The pattern matches `DistributedTransactions.d4` in the sibling area, protects against a future wrapping change, and costs nothing — do not strip.
- **Thread safety — checked.** The public leave takes `BeginCall()` around `CheckCanLeaveApplicationTransaction` and `InternalLeaveApplicationTransaction` (`ServerProcess.cs:1640-1660`), so a second thread on the same process cannot modify the transaction state between the check and the leave.
- **Hygiene — checked.** Both `.cs` files edited keep CRLF line endings and no byte-order mark, as before; `migration-notes.md` stays LF. The `NOTE:` rewrite at the `DeviceDisconnect` call states the reason, not the mechanism. No new narrating comments. The `HasRecordedWork` helper is a 7-line local method with a plain name that makes the recursion read at the call site.
- **Docs — checked.** `docs/migration-notes.md` rewrites the existing 125111 row to cover both clauses and the recommended "put the leave in a separate statement" workaround. The sibling-ticket paragraph in `docs/developers-guide/space-between-data.md` states the depth rule generically ("a leave inside an open transaction is refused"); it does not enumerate the two clauses and does not need to — it points at the migration note. No reference page lists `1251xx` codes.
- **Known gap — none open.** The sibling ticket's "Known gap" paragraph named this ticket as the open item; this ticket closes it.
- **Pre-existing failures.** Part 3's `ServerLink` timeout is the documented one-off; record says do not file. Nothing to report.

Validation after the review: `dotnet build Dataphor.slnx` 0 errors; all five `ApplicationTransactionTest` tests pass; full suite results above.
