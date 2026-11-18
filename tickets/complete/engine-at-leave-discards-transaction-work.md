description: Leaving an application transaction while an ordinary transaction is still open used to throw away the work done in the application transaction without a word; leaving in that case now fails with a clear error and keeps the work.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Device.ApplicationTransaction/ApplicationTransaction.cs, src/engine/DAE/CLI.cs, src/client/Frontend.Client/Session.cs, src/engine/DAE/Device.ApplicationTransaction/ApplicationTransactionException.cs, src/engine/DAE/Device.ApplicationTransaction/ApplicationTransactionException.resx, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerTransaction.cs, tests/DAE.Server.Tests/ApplicationTransactionTest.cs, tests/DAE.Server.Tests/coverage-manifest.json, docs/migration-notes.md, docs/developers-guide/space-between-data.md
repro: verified
----
## What was decided

The owner chose "refuse to leave" (2026-11-18): `LeaveApplicationTransaction()` raises a new `ApplicationTransactionException`, code **125111**, "Cannot leave application transaction while a transaction is open on the process.", and the process stays joined with its transaction and the application transaction's (AT's) work untouched. Background: leaving disconnects the process's AT device session, and disposing that session rolls back every transaction still open on it, which removed the rows from the AT copies and the operations from the AT's replay log.

## Where the implementation departs from the decision's wording, and why

The decision says "refuse whenever the AT session has an open transaction". Taken literally that refuses **every** leave while implicit transactions are on (the default): each statement, including `LeaveApplicationTransaction();` on its own, runs in an implicit transaction it begins itself, and a connected device session begins a device transaction whenever the process does (`ServerProcess.InternalBeginTransaction`). The ticket's own premise ("each batch's implicit transaction has committed before the leave") missed that the leave's own batch has one. So the rule implemented is:

> Refuse when the process is joined, its AT device session is connected and in a transaction, and some process transaction is open other than the implicit transaction of the statement (transactional call) doing the leave. For a call from a client through `IServerProcess`, no statement is running, so any open transaction refuses.

Refused: an explicit `BeginTransaction` still open; the transaction a `SetUseImplicitTransactions(false)` batch leaves open (see below); an enclosing statement's transaction (a leave inside `Execute`/`ExecuteScript`). Allowed: a leave statement with no other transaction open, in either implicit-transaction mode. The rule does not depend on what the transactions did, as the owner asked.

**Known gap, filed as `engine-at-leave-same-statement-work` (blocked, proposal):** changes made earlier in the *same* statement as the leave (a `begin … end` block, or an operator body that joins, inserts and leaves) are in that statement's own implicit transaction and are still discarded silently. Refusing them needs a check on what the transaction did, which the owner preferred to avoid, so it went back to the owner as a decision.

## How it works

- `ServerProcess` counts running transactional calls (`_transactionalCallDepth`, incremented at the end of a successful `BeginTransactionalCall`, decremented in `EndTransactionalCall`'s `finally`). When `BeginTransactionalCall` begins an implicit transaction it stamps the new `ServerTransaction.ImplicitCallDepth` with the call's depth (explicit transactions keep 0).
- `EndTransactionalCall` resets the stamp to 0 when the implicit transaction outlives its call. That happens when implicit transactions are switched off during the call: `SetUseImplicitTransactions(false)` run as a batch leaves its own batch's implicit transaction open (legacy behaviour, `EndTransactionalCall` only ends implicit transactions while the setting is on). Without the reset, a later leave batch at the same depth would treat that leftover transaction as its own and discard the work committed into it.
- `ServerProcess.InTransactionBeyondCurrentCall`: true when any open transaction is explicit or carries another call's depth.
- `ServerProcess.LeaveApplicationTransaction()` (the `IServerProcess` member, called by the D4 operator node and `RemoteServerProcess`) takes `BeginCall()`, runs `ApplicationTransactionUtility.CheckCanLeaveApplicationTransaction` **before** its own `BeginTransactionalCall` (so the innermost running call is the enclosing statement, or none for a client call), then calls the old body, now `InternalLeaveApplicationTransaction()`.
- The engine's own leaves call `InternalLeaveApplicationTransaction()` unchecked, behaviour unchanged: process `Dispose`, `ApplicationTransactionUtility.Cleanup` (all participants leave when the AT commits or rolls back) and the temporary join/leave in `PrepareApplicationTransaction`. The last two run inside the commit call's own transactional call, so the check would otherwise refuse every AT commit.
- The `NOTE:` at the `DeviceDisconnect` in `ApplicationTransactionUtility.LeaveApplicationTransaction` now states the rule and why.

## Bug fixed along the way (verified)

With implicit transactions off and no transaction open, `CommitApplicationTransaction` (and `PrepareApplicationTransaction`) of an AT holding changes failed with `NullReferenceException` ("A nil was encountered where a value was expected"): `PrepareApplicationTransaction` recorded each replayed operation on the AT session's current device transaction, which is null outside a transaction. Now it records only when there is one, since outside a transaction there is no rollback to undo. Found by running the reproduction sequence without switching implicit transactions back on.

## Tests added

- `ApplicationTransactionTest.TestLeaveInsideTransactionRefused`: the ticket's reproduction, batch by batch on one process (`SetUseImplicitTransactions(false)`, `BeginTransaction`, join, insert). Checks that the D4 leave and the `IServerProcess` leave both raise 125111; that the process stays joined and the inserted row is still visible; that after `CommitTransaction` the leave is *still* refused, because the transaction the `SetUseImplicitTransactions(false)` batch left open now holds the insert (this covers the stamp reset); that after the second `CommitTransaction` the leave succeeds, the real table is still empty, and `CommitApplicationTransaction` puts the row in it. It fails without the check, and also with only the stamp reset removed (checked by disabling each temporarily).
- `ApplicationTransactionTest.TestCommitOutsideTransaction`: reproduction of the bug above (implicit transactions off through `ProcessInfo`, not `SetUseImplicitTransactions`, so that no leftover transaction hides the bug). It fails with the `NullReferenceException` without the fix.

The other cases are covered by existing tests. A plain leave batch with implicit transactions on is covered by the `ApplicationTransactions` coverage script (manifest `batches: true`, still `pass`). The nested case is `StreamAllocation`, still `known-failure`; its recorded error ("Object "Coverage" not found") comes before it reaches the AT part and is unchanged. The `ApplicationTransactions` manifest `note` now says that a nested run would raise 125111 (it used to say the leave discards the work); its status and fields are unchanged.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors.
- `dotnet test Dataphor.slnx --no-build`: Common.Tests 13/13; DAE.Server.Tests 167 total, 142 passed, 25 skipped (known-failure, unstable and fragment coverage cases), 0 failed.
- Line endings and byte-order marks of every edited file are unchanged (CRLF `.cs`/`.resx`; LF Markdown and JSON).

## Docs

- `docs/migration-notes.md` (D4 section): two `done` rows: the 125111 refusal (what triggers it and what to do), and the commit-outside-a-transaction fix.
- `docs/developers-guide/space-between-data.md`: the `LeaveApplicationTransaction` paragraph states the rule. `docs/reference` has no entry for the operator; the D4 operator reference is still DocBook under `legacy/Documentation/Docs2.0/SystemOperators.xml` (not converted, not edited).

## For the reviewer to weigh

- **The depth rule itself.** It adds two integer updates to `BeginTransactionalCall`/`EndTransactionalCall`, which every cursor call goes through. I judged that cheap enough and did not measure it. The other option I considered was a per-call stack of transaction counts. It does not catch an explicit `BeginTransaction` inside the same block as the leave, which the stamp does.
- **Client code that leaves in a `finally`.** `Frontend.Client/Session.cs` (`EvaluateWith`, `EvaluateScalarWith`) joins, evaluates and leaves in a `finally` on the utility process. If a caller has an explicit transaction open on that process, the leave now raises 125111, which replaces the original exception and leaves the process joined. That is the decided behaviour, and the migration note says so; the client is not converted yet, so it was not exercised.
- **Checks made under `BeginCall`.** `LeaveApplicationTransaction` takes `BeginCall` around both the check and the leave, so a second thread on the same process cannot start a transaction between them.
- **Not changed:** stopping a process that is joined still leaves without the check and rolls back (as the ticket asked); `Cleanup` still makes other processes joined to the AT leave unchecked.

## Review findings

Reviewed the diff of `ticket(implement): engine-at-leave-discards-transaction-work` before the handoff, then the call paths it touches.

- **Correctness of the depth rule — checked, no defect.** Traced `_transactionalCallDepth` and `ImplicitCallDepth` through every path: `BeginTransactionalCall` failing in `InternalBeginTransaction` (depth not incremented, `EndCall` in the catch); `EndTransactionalCall` throwing on commit or rollback (the `finally` still resets and decrements); `StopError` rolling back everything; a nested call turning implicit transactions off (only the outermost call with `nestingLevel == 0` resets, and only a stamp equal to its own depth); a statement that commits the implicit transaction and begins an explicit one (stamp 0, left alone). `ServerTransaction` objects are created fresh per begin (`ServerTransactions.BeginTransaction`), so no stale stamp survives. `ExecuteScript` runs each batch's plan directly without an enclosing transactional call, so a plain `LeaveApplicationTransaction();` batch is at depth 1 and is allowed; a cursor's open call ends its implicit transaction before returning, so an open cursor does not block a later leave. The call from a client (`RemoteServerProcess.LeaveApplicationTransaction`) is at depth 0, so any open transaction refuses, as documented.
- **Failure of the refused statement — checked, no defect.** When the leave is refused inside a statement that runs in an explicit transaction, `EndTransactionalCall` rolls back only transactions above the call's nesting level, so the explicit transaction and the application transaction's work survive (the test asserts this). Refused inside a nested `Execute`, the enclosing statement fails and its implicit transaction rolls back as any failed statement does.
- **Engine-internal leaves — checked.** Process dispose, `ApplicationTransactionUtility.Cleanup` and the temporary join in `PrepareApplicationTransaction` go to `InternalLeaveApplicationTransaction` unchecked, as before; the only other callers of the public leave are the D4 node and `RemoteServerProcess`.
- **The `PrepareApplicationTransaction` null fix — checked.** `deviceTransaction` is null exactly when the AT session is not in a transaction; skipping the record there loses nothing because there is no rollback to undo. Reproduction test present.
- **Thread safety — checked.** The counter is changed and read only under `BeginCall` (the process's monitor), and the public leave holds `BeginCall` across check and leave.
- **Performance — not measured.** Two integer updates per transactional call; the implementer did not measure and neither did I. Nothing to suggest it matters.
- **Tests — kept both.** `TestLeaveInsideTransactionRefused` is the reproduction and also pins the stamp reset; `TestCommitOutsideTransaction` is the reproduction of the bug fixed along the way. Neither restates the implementation or uses a mock. No test added.
- **Docs — one gap fixed.** The public `IServerProcess.LeaveApplicationTransaction` doc comment (`src/engine/DAE/CLI.cs`), which extension and client authors read, did not mention 125111; added a remark. `docs/migration-notes.md`, `docs/developers-guide/space-between-data.md` and the coverage-manifest note match the code. No Markdown error-code reference lists `1251xx` codes, so none needed the new one.
- **Tripwire parked.** `src/client/Frontend.Client/Session.cs` (`EvaluateWith`/`EvaluateScalarWith`) leaves in a `finally`; if a caller has a transaction open on the utility process, 125111 replaces the original error and the process stays joined. Recorded as a `NOTE:` at the site (the client is not converted yet; the migration note already covers the behaviour).
- **Known gap — already routed.** Work done earlier in the same statement as the leave is still discarded; that is `engine-at-leave-same-statement-work` in `blocked/`, awaiting the owner. Nothing to add.
- **Hygiene.** Line endings and byte-order marks of both files edited in this pass are unchanged (both CRLF, neither has a byte-order mark). Comments in the diff state reasons, not narration.

Validation after the review edits: `dotnet build Dataphor.slnx` 0 errors; `dotnet test Dataphor.slnx --no-build`: DAE.Server.Tests 167 total, 142 passed, 25 skipped (known-failure, unstable and fragment coverage cases), 0 failed.
