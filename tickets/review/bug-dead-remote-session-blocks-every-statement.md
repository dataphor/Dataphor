description: A session whose connection to another Dataphor server has died now recovers by itself: the next statement releases the dead connection, logs a warning naming the link and runs, unless that connection still has work in an open transaction, in which case the statement fails with an error that names the link.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerException.cs, src/engine/DAE/Server/ServerException.resx, src/engine/Server/Server/RemoteSessionImplementation.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md, docs/modernization-plan.md
difficulty: hard
repro: verified
----
## The rule that landed

A connection to another Dataphor server that is a participant in **no** transaction this process still has open may be let go at any time: nothing has been done on it that this process's transaction protocol still owes an answer for. A connection that **is** a participant may not, because forgetting it would let the local commit believe the remote half committed.

The reclaim is at the transaction begin, which is the one call a process makes on every connection it holds whether the statement uses that connection or not, and the one point at which the far server has provably done nothing yet at the level being begun. Nothing new was added to D4: no operator, no setting.

## What landed, in the order it was written

**The new error code.** `ServerException.Codes.RemoteSessionTransactionFailed = 106212`, with its `<data name="106212">` entry at the end of `ServerException.resx`: *A transaction operation failed on the connection to instance "{1}" on host "{2}" through server link "{0}".* Parameters are `ServerLink.Name`, `ServerLink.InstanceName`, `ServerLink.HostName`; the cause goes in as the inner exception. Purely additive - no existing code's number or text changed.

**`RemoteSession` counts its own transaction depth** (`src/engine/DAE/Server/RemoteSession.cs`). `TransactionCount` is a non-abstract property over a private field; `BeginTransaction`, `PrepareTransaction`, `CommitTransaction` and `RollbackTransaction` are the base class's own methods over four new `protected abstract` `InternalBeginTransaction(IsolationLevel)`, `InternalPrepareTransaction()`, `InternalCommitTransaction()` and `InternalRollbackTransaction()`. The begin increments only on success; the commit and the rollback decrement in a `finally`; the prepare does not change the count. All four turn a failure out of their `Internal...` method into error 106212 with the original as the inner exception. The invariant - *every remote session in a process's `RemoteSessions` list has begun exactly as many transaction levels as the process has open* - is recorded in the remarks on `TransactionCount`.

`RemoteSessionImplementation` (`src/engine/Server/Server/RemoteSessionImplementation.cs`) lost its `TransactionCount` override and its four transaction methods are now the `Internal...` overrides. Its use of `NativeCLISession.GetTransactionCount` went with the override; the keep-alive's own use of that *operation* lives inside `NativeCLISession` and was not touched.

**The begin lets a dead non-participant go** (`ServerProcess.InternalBeginTransaction`). It iterates a snapshot (`new List<RemoteSession>(RemoteSessions)`), because the release removes the entry from the list. Per session: success, record it; failure with `TransactionCount == 0`, release it and carry on; failure with `TransactionCount > 0`, rethrow and let the existing all-or-nothing unwind do the rest. The remote half of that unwind is now a `List<RemoteSession>` of the sessions that began rather than an index count, and `RollbackBegunParticipants` rolls those back in reverse; the device half is still an index count, because device sessions are not released here.

The release is `ServerProcess.DiscardRemoteSession(RemoteSession, Exception)`: one `LogEntryType.Warning` through `ServerSession.Server.LogMessage` naming the link, the instance, the host and `cause.Message` and saying this server has released its side and the next use of the link connects again (shaped on `RemoteSessionImplementation.ReportFailedRemoteStop`), logged *before* the disposal because the disposal clears the session's `ServerProcess`; then `RemoteSessions.Disown(session)` and `session.Dispose()` inside a `try`/`catch` that logs with `LogError`.

**One assertion.** `InternalPrepareTransaction` now `Error.AssertFail`s that each remote session's `TransactionCount` equals `_transactions.Count` before preparing it. Prepare is the first of the three ending operations and a commit reaches it, so this one check covers both.

**Two `NOTE:`s, both "do not change this".** At `DiscardRemoteSession`: why the reclaim is at the begin and not at `Execute`. At `InternalCommitTransaction`'s remote loop: that remote sessions committing before any device session is load-bearing for this ticket, so the two loops must not be reordered.

## Claims confirmed against the code rather than assumed

- **`RemoteSessions.Disown` really removes by reference.** `RemoteSessions` declares `IndexOf(Schema.ServerLink)` as an *overload*, not an override; `DisposableList<T>.Disown` goes through `ValidatingBaseList<T>.Remove(T)` to the base `IndexOf(T)`, which matches with `Object.Equals`, and `RemoteSession` overrides neither `Equals` nor `IndexOf`. So it is reference equality, as the plan assumed.
- **Disowning first is what guarantees the entry leaves the list.** `DisposableList<T>.Removing` unhooks the item's `Disposed` handler, and `_disowning` suppresses the disposal, so `Disown` then `Dispose` removes exactly once, and a disposal that threw before raising `Disposed` cannot leave a dead entry behind.
- **`RemoteConnect`'s isolation-level expression is untouched.** `_transactions[_transactions.Count - 1].IsolationLevel` is still exactly as it was, so `bug-remote-catchup-uses-innermost-isolation` (now in `tickets/fix/`) does not collide with this change. Only `session.TransactionCount` underneath it changed meaning, from a network call to a local count.

## Tests

Three cases in `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, each on a process of its own started from `Session` so the fixture's own process and the other cases are untouched, each against a child server it starts and stops itself. The fixture's class remark and the remark on `DoomedTargetInstanceName` were updated to say that several cases now use such a child.

| Case | What it pins | Run alone |
|---|---|---|
| `TestADeadConnectionIsLetGoAndTheStatementRuns` | The reproduction. Link used so the connection opens, child stopped; then `1 + 1` evaluates, the process's connection count is 0, and `drop server <link>;` succeeds - both the statement with nothing to do with the link and the statement that was the only way out. | 8 s, passed |
| `TestOnlyTheConnectionWhoseServerHasGoneIsLetGo` | Two links on one process, one to the case's doomed child and one to the fixture's shared live child, both used; child stopped; the next statement runs, the connection count is 1, and the live link still answers. | 9 s, passed |
| `TestABeginOnADeadParticipantFailsNamingTheLink` | Connection made a participant at depth 1 while the child is alive, then the child stopped: a nested `BeginTransaction` throws with error 106212 in its exception chain and `TransactionCount` is still 1; `RollbackTransaction` throws; `InTransaction` is false; the next statement succeeds and the connection count is 0. | 13 s, passed |

Asserted by **code, not message**: a helper walks the thrown exception and its `InnerException` chain for the first `DataphorException` and compares its `Code` with `(int)ServerException.Codes.RemoteSessionTransactionFailed`.

**What was seen with each change removed.**

- *Release arm removed* (the begin always rethrows): all three cases failed, with `System.ServiceModel.EndpointNotFoundException` over `SocketException` from `RemoteSessionImplementation.InternalBeginTransaction` under `RemoteSession.BeginTransaction` - the same failure the plan ticket recorded on 2026-11-21, re-measured here on 2026-11-22. Cases 1 and 2 fail at the first statement after the child is stopped; case 3 fails at its recovery assertions, which depend on the release too.
- *Participant guard removed* (the begin always releases): case 3 failed at `Assert.Catch` on the nested begin - "Expected: instance of `System.Exception`, But was: null" - which is the arm that must keep failing the statement.

**Not covered by a test, deliberately.** The discard warning reaching the log: the fixture's servers run with logging off, and asserting on log text would pin the wording rather than the behaviour. A far server that comes back: that is `RemoteConnect`'s existing "no entry in the list, so connect" path, which `TestLinkRecreatedUnderTheSameNameConnectsAfresh` already pins, and `OutOfProcessInstance` reserves a fresh port per instance so a replacement child is not reachable through the original link's port tag anyway.

## Edge cases settled by inspection rather than by a test

Re-read these rather than re-derive them.

- **Two dead connections on one process.** Both released in the one begin and the statement still runs: the release arm `continue`s, with no early exit from the loop.
- **The commit of a dead participant.** Fails at the remote prepare, which runs before any device session commits, so nothing local has committed when it fails. This is the ordering the new `NOTE:` protects.
- **The rollback of a dead participant.** `InternalRollbackTransaction` already catches each remote rollback failure, logs it and rethrows the last after the local unwind; the count decrementing in the `finally` is what leaves the process and the connection agreeing afterwards, so the next begin can release it. Case 3 asserts the recovery; the decrement is what makes it pass.
- **A connection first used inside an open transaction against a server that then dies.** `RemoteConnect` catches it up to the process's depth before adding it, so it is a participant from the moment it enters the list. A catch-up begin that fails still disposes the session and rethrows without the session ever entering the list - existing behaviour, unchanged.
- **`UseImplicitTransactions` off.** No transaction per statement, so a statement that does not touch the dead link never reaches the begin; the connection is released at the next explicit `begin transaction`.
- **An application transaction, and a commit's deferred handlers.** Both begin nested levels through the same `InternalBeginTransaction`, so both get this treatment with no code of their own.
- **The count can never go negative, so the new assertion cannot be undermined.** At depth N the loop runs with `_transactions.Count` already N + 1 and each session's count at N, so a release can only ever happen at depth 0; at depth 1 or deeper every session is `InTransaction` and its failure rethrows. The three callers of the decrementing methods are `InternalRollbackTransaction` (guarded by `InTransaction`), `RollbackBegunParticipants` (only sessions that incremented) and `InternalCommitTransaction` (reached only through the assertion). A session added by a deferred handler during prepare is caught up by `RemoteConnect`, so it too agrees.
- **`drop server` and `alter server` inside a transaction.** Their close is deferred to the outermost commit, and `CatalogDeviceSession.InternalCloseRemoteSession` guards on `RemoteSessions.Contains(link)`, so a connection released in between makes the deferred close a no-op rather than error 106120.
- **Concurrent readers of the list.** Unchanged. The release is a mutation on the owning process's own thread inside `BeginCall`'s `_syncHandle`, exactly like `RemoteConnect` and `RemoteDisconnect`; `CatalogDevice.PopulateRemoteSessions` reads another process's list without a lock and the `NOTE:` already at that site owns that question.
- **Process teardown.** `ServerProcess.Dispose` rolls back every open transaction and then calls `CloseRemoteSessions`, untouched here and already pinned by `TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow` (passing).

## Compatibility

Both entries landed in `docs/migration-notes.md`.

- *D4, catalog and libraries*: the new behaviour, and that a failed transaction operation on a link now arrives as Dataphor error 106212 rather than as the raw WCF exception - an application that matched on the WCF exception type must match on the code instead.
- *Custom C# extensions*: `RemoteSession.TransactionCount` is local and no longer abstract, and the four `Internal...` methods are what a subclass overrides. Noted in the same entry that `ServerProcess.RemoteSessionClassName` is internal, so the shipped `RemoteSessionImplementation` is the only subclass the engine can be asked to build - which bounds who can notice this.

No D4 semantics, operator signature, `DAERegister` class name, assembly name, namespace, file format, WCF contract shape, port or on-disk layout changed. The one error-code change is additive.

## Documentation

`docs/modernization-plan.md` *Known defects* gained **K39**, in the shape of its neighbours: what went wrong with the re-measured failure, the rule, where the reclaim is and why it is not at `Execute`, what the user now sees in both situations, the three cases and what was seen when each change was removed, the files, state `fixed`, owner `WS2 bug-dead-remote-session-blocks-every-statement`, and that the defect is in the initial open-source commit rather than a port regression.

**The WS2 and K35 prose check, and what it found: nothing to change.** Read: the whole WS2 section (lines 248-308) for every mention of a link or a remote session, K35's row, and every `migration-notes.md` row mentioning a server link. K35's row makes no claim that `drop server` could not run - its sentences about nothing closing a cached connection and `RemoteDisconnect` having no caller are historical statements of what K35 itself fixed, and both still read as true of the state before that ticket. The `migration-notes.md` row on `drop server`/`alter server` already said "a connection to a server that cannot be reached is let go anyway, with a warning in the server log naming the link", which is about the *drop's* disposal, not the transaction begin, and is still accurate. Nothing anywhere described this defect as open, so nothing was edited.

## Build and tests

`dotnet build Dataphor.slnx`: 0 errors. Then the suite in the three parts the root `AGENTS.md` names, each in the foreground:

- part 1 (everything but the coverage scripts): `Common.Tests` 18 passed; `DAE.Server.Tests` 677 passed, 0 failed, 33 skipped, 7 m 55 s.
- part 2 (coverage scripts A-R): 67 passed, 0 failed, 11 skipped, 3 m 4 s.
- part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 2 s.

No failures, and nothing pre-existing to report - `tickets/.pre-existing-error.md` was not written.

## What a reviewer might want to push on

An honest list of where judgment was exercised, not of gaps that were papered over.

- **The warning's wording and volume.** One warning per connection released, undeduplicated, matching the precedent at `ReportUnhonouredSecuritySettings`. A process that repeatedly reconnects to a server that is down and then begins a transaction would write one per cycle: bounded by reconnections rather than by statements, but not by anything smaller.
- **Wrapping all four transaction operations in 106212, not only the begin.** The plan asked for this, and the reclaim decision reads `TransactionCount` rather than the exception, so the wrapping costs that decision nothing. The visible consequence is the migration note: callers that matched on the WCF exception type for a prepare, commit or rollback failure on a link see a Dataphor error now.
- **The assertion's cost and placement.** One integer comparison per connection per commit, on a list that is empty for almost every process, and only inside the `if (!transaction.Prepared)` block. It is an `Error.AssertFail`, so it raises an internal error rather than being compiled out in Release - deliberate, because a disagreeing count would otherwise surface as a confusing "no transaction in progress" from the far server.
