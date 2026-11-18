description: When starting a transaction failed part-way, the session was left inside a half-started transaction that never ended on its own, so later work ran outside the transaction on some devices and was half-kept when the transaction was rolled back. Starting a transaction is now all or nothing.
prereq: store-sqlite-concurrency
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Schema/Device.cs, src/engine/DAE/Server/ServerTransaction.cs, tests/DAE.Server.Tests/TransactionBeginTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
----
## What was wrong

A process transaction has participants: the process itself, each remote session (a server link) and each device session the process has connected, the catalog device session among them. `ServerProcess.InternalBeginTransaction` pushed the process transaction and then began each remote session and each device session in list order, undoing nothing if one threw; `Schema.DeviceSession.BeginTransaction` likewise kept the level it had pushed when the device's `InternalBeginTransaction` threw. The process stayed one level deeper, some sessions had the new level and some did not, every later statement ran inside the half-started transaction, and the eventual rollback did not undo the work of sessions that never began the level.

## What changed (`ticket(implement): bug-process-begin-leaves-partial-transaction`)

- `Schema.DeviceSession.BeginTransaction` removes the level it added when `InternalBeginTransaction` throws; the abstract method documents that an override that throws must leave nothing begun.
- `ServerProcess.InternalBeginTransaction` counts the remote and device sessions that began; on failure it rolls back only those (device sessions in reverse with the catalog session last, then remote sessions in reverse, errors logged), removes the process transaction and rethrows the original error. A `NOTE:` at the `catch` records that a `StopError` set by a failing begin unwinds only the begin's own level.
- `ServerTransaction.InvokeDeferredHandlers`: a comment on why the nested begin stays above its `try` (it already was).
- Known defect K30 in `docs/modernization-plan.md`; a migration note for the `InternalBeginTransaction` contract.
- `tests/DAE.Server.Tests/TransactionBeginTest.cs`: a device that fails to begin at a chosen level; implicit begin, nested begin and the deferred-handler begin each fail and leave the process as it was. All three failed against the pre-fix engine (per the implement handoff).

## Review findings

Read the implement diff first, then the handoff.

**Checked, no change needed**
- Correctness of the unwind: the failing device session pops its own level before `WrapException`; an exception from the list push itself never enters the inner `try`, so nothing is popped twice. The process's `EndTransaction(false)` disposes the `ServerTransaction` exactly as a normal rollback does. `InRollback` is set around the unwind, as `InternalRollbackTransaction` does, so row-level undo in the memory, application-transaction and base device paths does not log the undo operations.
- Every in-tree `InternalBeginTransaction` override meets the new contract: memory (no-op), catalog (base plus a `#if LOGDDLINSTRUCTIONS` log entry that the rollback of the level consumes), application transaction (push onto its own list), SQL device (records the isolation level; the DBMS transaction is deferred). The server catalog session's store hooks (`InternalAfterRollbackTransaction`, `StoreHasEndingLevel`, `CheckStoreTransactionLevels`) handle rolling back a level that wrote nothing.
- Callers: `BeginTransactionalCall` sets `ImplicitCallDepth` only after a successful begin and calls `EndCall` on failure; the public `BeginTransaction` wraps once; `InvokeDeferredHandlers` begins outside its `try`, which the deferred-handler test pins.
- `StopError` left set by a failing begin outside a statement is read only by `EndTransactionalCall`, which always follows a `BeginTransactionalCall` that clears it; the implementer's `NOTE:` covers the policy question. No change.
- Rollback order of the unwind (devices then remote) differs from `InternalRollbackTransaction` (remote then devices); it is the reverse of the begin order and the participants are independent. Left as is.
- DRY: the catalog-last ordering appears in both the unwind and `InternalRollbackTransaction`. The full rollback rolls back every session in a transaction, which would end the outer level on sessions that never began the new one, so it cannot be reused; the duplication is three lines. Left as is.
- Source hygiene: CRLF and byte-order-mark state preserved in every edited file (checked with `grep -c $'\r$'` and the first three bytes); `TransactionBeginTest.cs` is 144 lines; docs (K30 row, migration note) match the code.
- Compatibility: no D4 semantics, class names, file formats, error codes or contracts changed; the extension-visible contract change has its migration note.

**Fixed in this pass (minor)**
- The comment on `TestDeferredHandlerBeginFails` described a different failure (the implicit-transaction commit path rolling back the client's level) from the one the test exercises (a client `CommitTransaction` whose nested begin fails, which left the half-begun level open). Rewrote it to say what the test pins, including why the begin must stay outside the handlers' `try`.

**Tests**
- The three new tests each reproduce one entry path to the defect (implicit begin, explicit nested begin, deferred-handler begin) with real branching; none restates a mock. Kept all three; added none.
- `dotnet build Dataphor.slnx`: 0 errors. `dotnet test Dataphor.slnx --no-build`: DAE.Server.Tests 145 passed, 25 skipped (the coverage manifest's known-failure, unstable and fragment entries), 0 failed.

**Filed**
- `backlog/bug-catalog-deferred-device-stops-ignore-nesting` (pre-existing, raised in the implement handoff, static): the catalog session's pending device stops are one list for the session, so any nested commit stops a device the outer transaction dropped and any nested rollback forgets the stop. The fix is a representation change (stops per transaction level), so it is filed at that rung rather than as a point fix; the new unwind of a failed nested begin is one more nested rollback that reaches it.

**Not filed, recorded**
- Remote sessions whose begin failed after the other server began the level are not rolled back (the engine cannot tell). Conditional on server links, which have no test harness yet; the handoff and K30 row say so. No tripwire added: the unwind's doc comment already says only sessions that began are rolled back.
- The unwind reads `DeviceSessions` by index and assumes the list does not change during a begin; the begin's own `foreach` already depends on that. No action.
- The test device's static `FailAtLevel` is shared across the server; only this fixture uses the device and the fixture has its own server. No action.
