description: When another Dataphor server that a session has queried goes away, the session stops failing every statement afterwards: the dead connection is let go, the statement runs, and the server log says which link failed - unless that connection has work in a transaction that is still open, in which case the statement still fails, now with an error that names the link.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerException.cs, src/engine/DAE/Server/ServerException.resx, src/engine/Server/Server/RemoteSessionImplementation.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md, docs/modernization-plan.md
difficulty: hard
repro: verified
----
## The rule this establishes

A *server link* is a D4 object naming another Dataphor server; the first `ExecuteOn`, `EvaluateOn` or `select … on <link>` opens a connection to it and the process that opened it keeps it. Every transaction this process begins begins one on every such connection it holds, whether the statement uses that link or not (`ServerProcess.InternalBeginTransaction`), so once the other server has gone the next statement fails at its transaction begin - `select 1 + 1` included, and `drop server <link>`, which is the one statement that would have cleared the connection. The session can then do nothing until it disconnects.

The rule that fixes it:

> A connection to another server that is **not** a participant in any transaction this process still has open may be let go at any time, because nothing has been done on it that this process's transaction protocol still owes an answer for. A connection that **is** a participant may not: work has been done on it at a level that is still open, and forgetting it would let the local transaction commit believing the remote part committed.

So the transaction begin - the one call that touches every connection the process holds whether the statement uses it or not, and the one point where the far server has provably done nothing yet at the level being begun - lets a connection go when its begin fails and it has no open level, writes one warning to the server's log naming the link, and carries on with the rest of the begin. A begin that fails on a connection that *does* have an open level fails the statement, as it must; so do a prepare, a commit and a rollback, which are only ever reached on connections that are participants.

Nothing new is added to D4: no operator to clear a connection, no setting. A session whose other server has gone recovers on its next statement, or, if it had an open transaction with work on that link, on its next statement after it rolls back.

## What the user sees afterwards

**The far server died while the session had no transaction open.** This is the ordinary case: with implicit transactions on - the default - every statement begins and ends its own transaction, so the connection is at depth 0 between statements. The next statement's begin lets the connection go, logs the warning and runs. `drop server` on the dead link runs. `System.RemoteSessions` stops listing the connection. The next use of that link reaches `ServerProcess.RemoteConnect`, finds no entry and connects again - so a far server that has come back is simply used again, with the link's current settings.

**The far server died inside an open transaction that had used the link.** Statements that begin no new level go on working, because the remote begin only happens when a level is actually begun. A nested begin fails with the new error naming the link, and the process stays at the depth it had (the begin has been all or nothing since `bug-process-begin-leaves-partial-transaction`). A commit fails at the remote prepare, which runs before any device session commits, so nothing local has committed when it fails. A rollback unwinds the local transaction and then reports that the remote half could not be rolled back - which the user needs to be told, and is what `InternalRollbackTransaction` already does with a remote rollback failure - while leaving the process at depth 0 and the connection's own count back at 0. The next statement's begin then lets the connection go and the session is usable again.

## Change 1 - the process knows each connection's transaction depth without asking

`RemoteSession.TransactionCount` is abstract and `RemoteSessionImplementation` answers it with `NativeCLISession.GetTransactionCount()`, a call over the network. On a dead connection the question "is this connection a participant?" therefore cannot be asked at all, which is why the decision above is not expressible today. It is also a round trip the process does not need: the process begins exactly one remote level per local level and unwinds one per level, so it already knows the answer.

Make `RemoteSession` (`src/engine/DAE/Server/RemoteSession.cs`) keep the count itself:

- `TransactionCount` becomes a non-abstract property over a private field.
- `BeginTransaction`, `PrepareTransaction`, `CommitTransaction` and `RollbackTransaction` stop being abstract and become the base class's own methods. Each delegates to a new `protected abstract` `InternalBeginTransaction(IsolationLevel)`, `InternalPrepareTransaction()`, `InternalCommitTransaction()` and `InternalRollbackTransaction()` - the `Internal…` split the engine already uses for the same job on `ServerProcess` and `CatalogDeviceSession`.
- `BeginTransaction` increments the field only on success. `CommitTransaction` and `RollbackTransaction` decrement it in a `finally`: the level is being abandoned locally whatever the far server did, and a count left high would make the next begin treat a dead connection as a participant and never let it go. `PrepareTransaction` does not change it.
- Each of the four wrappers catches a failure out of its `Internal…` method and throws the new `ServerException.Codes.RemoteSessionTransactionFailed` (Change 3) with the original as the inner exception, so that every caller of these four gets an error saying which link failed instead of a bare WCF exception. The decision in Change 2 is made on `TransactionCount`, never on the exception, so wrapping here costs that decision nothing.
- Record the invariant in the remarks on `TransactionCount`: **every remote session in a process's `RemoteSessions` list has begun exactly as many transaction levels as the process has open.** `RemoteConnect` catches a new connection up to the process's depth before adding it; `InternalBeginTransaction` either begins a level on a connection, lets the connection go, or fails the whole begin; and the unwind decrements on every path, failures included.

`RemoteSessionImplementation` (`src/engine/Server/Server/RemoteSessionImplementation.cs`) loses its `TransactionCount` override and renames its four transaction methods to the new `Internal…` overrides. Its use of `NativeCLISession.GetTransactionCount` disappears with the override; the keep-alive's own use of that *operation* lives inside `NativeCLISession` and must not be touched - it is the native interface's ping, as the remarks there explain.

Add one assertion where the invariant is load-bearing: in `InternalPrepareTransaction`'s loop over `RemoteSessions`, `Error.AssertFail` that the session's `TransactionCount` equals `_transactions.Count`. Prepare is the first of the three ending operations to run and commit reaches it, so one assertion covers both. It turns a would-be "no transaction in progress" error from the far server into a statement naming the broken invariant, and costs one integer comparison per connection per commit on a list that is empty for almost every process.

## Change 2 - the begin lets a dead non-participant go

In `ServerProcess.InternalBeginTransaction` (`src/engine/DAE/Server/ServerProcess.cs`), iterate a **snapshot** of `RemoteSessions`, not the list: letting a connection go removes its entry (`DisposableList<T>` disowns an item when the item raises `Disposed`, and `Disown` is also called directly below), which would invalidate an enumerator over the list itself. For each session:

- the begin succeeds - record the session in a list of the ones that began;
- it throws and `session.TransactionCount == 0` - let the connection go (below) and carry on with the next session;
- it throws and `session.TransactionCount > 0` - rethrow, and the existing all-or-nothing unwind does the rest.

`remoteSessionsBegun` is an *index* count today and `RollbackBegunParticipants` indexes back into `RemoteSessions` with it. That breaks as soon as a connection can be removed mid-loop, so replace the remote half of that bookkeeping with the collected list of sessions that began and roll those back in reverse. The device half stays an index count: device sessions are not let go here.

Letting a connection go belongs in one new private method on `ServerProcess` - name it for what it does to the connection, for example `DiscardRemoteSession(RemoteSession session, Exception cause)` - which:

- writes one `LogEntryType.Warning` through `ServerSession.Server.LogMessage`, naming the server link, the instance name, the host name and `cause.Message`, and saying that this server has released its side and the next use of the link will connect again. Shape it on `RemoteSessionImplementation.ReportFailedRemoteStop`, which is the existing warning for the neighbouring case and the one a reader will compare it with. One warning per connection let go, so the volume is bounded by connections rather than by statements;
- logs before it disposes, so the link is read while the session is still live;
- calls `RemoteSessions.Disown(session)` and *then* `session.Dispose()` inside a `try`/`catch` that logs with `ServerSession.Server.LogError`. Disowning first is what guarantees the entry is out of the list however the disposal goes; without it a disposal that threw before raising `Disposed` would leave the dead entry in place to fail the next statement in the same way. The disposal of a dead connection is already written not to throw (`RemoteSessionImplementation.Dispose` logs a failed remote stop as a warning, since `engine-server-link-session-outlives-drop`), so say in a comment that the catch is a guard rather than an expected path.

Leave `InternalPrepareTransaction`, `InternalCommitTransaction` and `InternalRollbackTransaction` deciding nothing about dead connections. Two things to record at the site rather than change:

- `InternalCommitTransaction` commits every remote session **before** any device session. That ordering is what makes a dead participant fail the commit before anything local has committed, so it is load-bearing for this ticket and must not be reordered.
- A `NOTE:` at the discard site stating the boundary: the reclaim is at the transaction begin because that is the one call on which the far server has provably done nothing yet. A failed `Execute` or `Evaluate` is not a reclaim point - the call may have been delivered and run - and a process that holds a dead connection and begins no transaction on it keeps it, which with implicit transactions on cannot outlast one statement.

## Change 3 - one new error code

Add `RemoteSessionTransactionFailed = 106212` to `ServerException.Codes` (`src/engine/DAE/Server/ServerException.cs`), after `CommunicationFailure = 106211`, with its `/// <summary>Error code 106212: …</summary>` line in the shape every other entry in that file has, and the matching `<data name="106212" xml:space="preserve">` entry at the end of `src/engine/DAE/Server/ServerException.resx`, after 106211. Text:

```
A transaction operation failed on the connection to instance "{1}" on host "{2}" through server link "{0}".
```

Parameters in that order: `ServerLink.Name`, `ServerLink.InstanceName`, `ServerLink.HostName`; the cause goes in as the inner exception. Additive - no existing code's number or text changes - so the error-code compatibility surface is untouched. Do not add a second code for the begin-on-a-participant case: that failure is this same operation failing, the caller is told which link by the parameters, and the inner exception carries what the network said.

## Edge cases & interactions

Each of these must be handled, and the review will check them. How each is verified is stated; most are settled by inspection because the behaviour is a consequence of the invariant rather than a separate path.

- **Two dead connections on one process.** Both are let go in the one begin and the statement still runs. Covered by iterating the snapshot once; verified by inspection (the loop has no early exit on the discard arm).
- **A dead connection and a live one.** Only the dead one goes; the live one keeps its connection and its session on the far server. Test case 2 below.
- **A dead participant at a nested begin.** The statement fails with 106212 and the process keeps the depth it had. Test case 3 below.
- **The rollback of a dead participant.** `InternalRollbackTransaction` already catches each remote rollback failure, logs it and rethrows the last one after the local unwind. With Change 1 the count is decremented in the `finally` regardless, so the process and the connection agree afterwards and the next begin can let the connection go. Test case 3 asserts the recovery; the decrement-on-failure is what makes it pass.
- **The commit of a dead participant.** Fails at the remote prepare, before any device session has committed. Verified by inspection against the order in `InternalCommitTransaction`, which this ticket must not change.
- **A connection first used inside an open transaction, against a server that then dies.** `RemoteConnect` catches the new session up to the process's depth, so it is a participant from the moment it enters the list and the participant arm applies to it. A catch-up begin that fails still disposes the session and rethrows without it ever entering the list, which is existing behaviour and stays. Verified by inspection.
- **`UseImplicitTransactions` off.** No transaction is begun per statement, so a statement that does not touch the dead link succeeds without reaching the begin at all, and the connection is let go at the next explicit `begin transaction`. No new behaviour; verified by inspection.
- **An application transaction, and a commit's deferred handlers.** Both begin nested transactions through the same `InternalBeginTransaction`, so both get the same treatment with no code of their own. Verified by inspection (`TransactionBeginTest` already covers the deferred-handler begin path for the device half).
- **Concurrent readers of the list.** `System.RemoteSessions` is populated by `CatalogDevice.PopulateRemoteSessions`, which walks another process's `RemoteSessions` without a lock; the `NOTE:` already at that site owns that question and parks it. Letting a connection go is a mutation on the owning process's own thread inside `BeginCall`'s `_syncHandle`, exactly like `RemoteConnect` and `RemoteDisconnect`. Do not change the locking here; verified by inspection.
- **Process teardown.** `ServerProcess.Dispose` rolls back every open transaction and then calls `CloseRemoteSessions`, which is unaffected by any of this and is already pinned by `TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow`.
- **`drop server` and `alter server` inside a transaction.** Their close is deferred to the outermost commit through `DeferredTeardowns<T>` on the catalog device session, and the guard there checks `RemoteSessions.Contains(link)` - so a connection this ticket let go in between makes the deferred close a no-op rather than an error 106120. Verified by inspection of `CatalogDeviceSession.InternalCloseRemoteSession`.
- **`RemoteSessions.IndexOf(RemoteSession)`.** `RemoteSessions` declares an `IndexOf(Schema.ServerLink)` **overload**; `Disown` goes through `ValidatingBaseList<T>.Remove(T)`, which calls the base `IndexOf(T)` and matches on reference equality, since `RemoteSession` overrides neither `Equals` nor `IndexOf`. Confirm this when writing the discard rather than assuming it.
- **The catch-up loop's isolation level.** `RemoteConnect` passes `_transactions[_transactions.Count - 1].IsolationLevel` - the innermost level's - to every catch-up begin, which is wrong when the open levels differ. That is a separate defect at the same line, filed as `bug-remote-catchup-uses-innermost-isolation`; leave the expression's meaning exactly as it is while changing `session.TransactionCount` under it, so the two tickets do not collide.

## Tests

In `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, which already holds the fixture this needs: an in-process server, `OutOfProcessInstance` for a second Dataphor server as a child process, `DoomedTargetInstanceName` for a child a case starts and stops itself, the `CreateLink` helpers, `RemoteCallDepth` and `ConnectionCount`, and the precedent case `TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow`. Three cases, each on a process of its own started from `Session` so the fixture's own process and the other cases are untouched:

- **Statements run again once the dead connection is let go.** A doomed child of the case's own; a link naming it; `EvaluateOn` so the connection opens; stop the child; then evaluating `1 + 1` on that process succeeds, the process's connection count is 0, and `drop server <link>;` succeeds. This is the ticket's reproduction - both the statement that has nothing to do with the link and the statement that was the only way out.
- **Only the connection whose server died is let go.** Two links on one process, one naming the case's doomed child and one naming the fixture's shared live child, both used so both connect; stop the doomed child; the next statement runs and the process's connection count is 1.
- **A nested begin on a participant fails naming the link, and the session recovers after a rollback.** Doomed child; link used; `BeginTransaction(IsolationLevel.Isolated)` while the child is alive, so the connection is a participant at depth 1; stop the child; a second `BeginTransaction` throws and the process's `TransactionCount` is still 1; `RollbackTransaction` throws; `InTransaction` is false; the next statement succeeds and the connection count is 0.

Assert the error by **code, not by message**: walk the thrown exception's `InnerException` chain for a `DataphorException` whose `Code` is `(int)ServerException.Codes.RemoteSessionTransactionFailed`. The fixture's existing remark on `TestLinkUserChangeClosesTheChangingProcessConnection` records that a failure from this layer arrives wrapped and that its message can be the reflection invocation's, so a message match would be testing the wrapping rather than the behaviour.

No new case for a far server that comes back: the reconnection is `RemoteConnect`'s existing "no entry in the list, so connect" path, which `TestLinkRecreatedUnderTheSameNameConnectsAfresh` already pins, and `OutOfProcessInstance` reserves a fresh port per instance so a replacement child could not be reached through the original link's port tag anyway. No new case for the discard warning reaching the log: the fixture's servers run with logging off, and asserting on log text would pin the wording rather than the behaviour.

## Documentation

- `docs/migration-notes.md`, under *D4, catalog and libraries*: a statement no longer fails because a connection to another server has died when that connection has no work in an open transaction - the server logs a warning naming the link and the statement runs - and a transaction operation that does fail on a link now arrives as Dataphor error 106212 naming the link, instance and host, with the underlying communication error as its inner exception, where it used to arrive as the raw WCF exception. What to do: an application that matched on the WCF exception type must match on the error code instead.
- `docs/migration-notes.md`, under *Custom C# extensions*: `RemoteSession.TransactionCount` reports the transaction levels the local process has begun on the connection instead of asking the other server, and the four transaction methods are no longer the abstract members a subclass overrides - `InternalBeginTransaction`, `InternalPrepareTransaction`, `InternalCommitTransaction` and `InternalRollbackTransaction` are. Note in the same entry that `ServerProcess.RemoteSessionClassName` is internal, so the shipped `RemoteSessionImplementation` is the only subclass the engine can be asked to build.
- `docs/modernization-plan.md`, *Known defects*: a new row **K39** (K38 is the highest today) in the shape of its neighbours - what went wrong, the measurement from this ticket's fix stage, what the rule now is, where the reclaim is and why it is there and not at `Execute`, what the user now sees, the test that reproduces it, the files, state `fixed`, owner `WS2 bug-dead-remote-session-blocks-every-statement`. Say that the defect is present in the initial open-source commit and is not a port regression: nothing in the tree closed a dead connection, and `RemoteDisconnect` had no caller at all until `engine-server-link-session-outlives-drop`.
- Check the WS2 prose and K35's row for any sentence that describes this defect as open and leaves it so. K35's row names this ticket's site as the reason `drop server` could not run; update it only if it now reads as still true, and say in the handoff which sentences were checked.

## TODO

- Read `src/engine/DAE/Server/RemoteSession.cs`, `ServerProcess.cs` (`RemoteConnect`, `CloseRemoteSessions`, `InternalBeginTransaction`, `RollbackBegunParticipants`, `InternalPrepareTransaction`, `InternalCommitTransaction`, `InternalRollbackTransaction`), `src/engine/Server/Server/RemoteSessionImplementation.cs` and `src/engine/Base/ValidatingBaseList.cs` / `DisposableList.cs` before editing; confirm the `Disown` and `IndexOf(T)` claims above against the code.
- Change 1: the local transaction count and the four `Internal…` overrides on `RemoteSession`, the matching renames on `RemoteSessionImplementation`, the invariant in the remarks, the assertion in `InternalPrepareTransaction`.
- Change 3: the new error code in `ServerException.cs` and `ServerException.resx` (do this before Change 2, which uses it).
- Change 2: the snapshot iteration, the participant decision, `DiscardRemoteSession`, the begun-sessions list in place of the remote index count in `RollbackBegunParticipants`, and the two `NOTE:`s (the reclaim boundary, and the commit ordering being load-bearing).
- The three cases in `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, each asserted by error code rather than message. Run each case on its own first (`dotnet test tests/DAE.Server.Tests --filter Name=<case>`) - a case that leaves a child server running costs the whole fixture.
- Check each case fails without its own change: remove the discard arm and case 1 fails; remove the participant guard and case 3 stops failing where it should; say in the handoff what was seen.
- The two `docs/migration-notes.md` entries, the K39 row, and the WS2/K35 prose check.
- `dotnet build Dataphor.slnx`, then the suite in the three parts the root `AGENTS.md` names, each as its own foreground command with no redirection.
- Preserve each file's tabs, CRLF and byte-order-mark state; `ServerException.resx` is XML and its entries are CRLF like the rest of the file.
- Handoff in `tickets/review/`: what landed, the `RemoteSession` member change as a compatibility note, what the three cases pin and what was seen when each change was removed, and anything from *Edge cases & interactions* settled by inspection rather than by a test, so the reviewer re-reads rather than re-derives.
