description: Two places in the engine treat a session's connection to another Dataphor server as though that connection were always at exactly the same transaction depth as the session itself. One of them opens transactions on the other server at the wrong isolation level, so a remote query can see more or less uncommitted data than it should; the other, after a commit that has already failed partway through, undoes work on the other server that the session has not finished with.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs
repro: static
severity: corruption
likelihood: contrived
tradeoffs: Both arms need nested transactions over a server link, which no shipped library or sample does, and neither has a cheap way to be observed from a test - so a maintainer may reasonably want them done alongside something else in this area rather than on their own.
----
## Arm 1: the catch-up opens every level at the innermost isolation level

A *server link* is a D4 object naming another Dataphor server. The first `ExecuteOn`, `EvaluateOn` or `select … on <link>` opens a connection to that server, and the process that opened it keeps it. If the process already has transactions open when that first use happens, the connection has to be brought up to the same depth before the work can run, which `ServerProcess.RemoteConnect` does:

```
while (session.TransactionCount < _transactions.Count)
    session.BeginTransaction(_transactions[_transactions.Count - 1].IsolationLevel);
```

The isolation level it passes is `_transactions[_transactions.Count - 1]`, the **innermost** open transaction's, and it passes that same one for every level it opens. The levels it is catching up to are the outer ones, each of which has its own isolation level - D4 lets a transaction be begun at a named one (`begin transaction isolation level ...`) - so when the levels differ, every transaction opened on the other server but the last gets the wrong one. The expression a reader would expect is the level being begun, `_transactions[session.TransactionCount]`.

What that costs is the thing an isolation level buys: a remote read inside the outer transaction can see uncommitted data it should not, or fail to see committed data it should, depending on which way the two levels differ. The local half of the same transaction is unaffected - this expression reaches only the connection to the other server.

### When arm 1 can be reached

All three have to hold:

- the session has more than one transaction open at once, and
- at least two of those levels were begun at different isolation levels, and
- a server link is used for the **first time on that process** while they are open - a link already connected is kept up to date one level at a time by `ServerProcess.InternalBeginTransaction`, which passes each level its own isolation level correctly.

No shipped library, sample or test script in the repository does that combination, which is why this is filed as a defect nobody has met rather than as an active bug.

### How arm 1 was found, and what would confirm it

Read, not run: found while planning `bug-dead-remote-session-blocks-every-statement`, which rewrites the surrounding lines and deliberately leaves this expression's meaning alone so the two do not collide. Nothing was executed, so `repro: static`.

What would confirm it is a reading of the transaction's isolation level **on the other server**, which is the part with no cheap answer today: `System.Processes` on that server does not report the isolation level of an open transaction, so showing the wrong level either means adding a way to ask - which is a change of its own - or building a case out of two sessions and a read that only one isolation level permits, against a second Dataphor server in a child process. Whoever takes this should decide which, or record that the fix rests on inspection, before changing the line: the project's rule is that a bug fix owes its reproduction, and this one has to be argued rather than assumed.

### The fix for arm 1

One expression, at the one site. `ServerProcess.RemoteConnect` is the only place in the tree that opens more than one transaction on a connection at once; `InternalBeginTransaction` opens one level at a time and already passes that level's own isolation level. Note that after `bug-dead-remote-session-blocks-every-statement` lands, `session.TransactionCount` is a local field rather than a call to the other server, so the index is free to read.

## Arm 2: after a commit that failed partway, the rollback undoes a level the session still has open

`ServerProcess.InternalCommitTransaction` ends the innermost level on every connection the process holds, one connection at a time, and then on the local devices. If one of those remote commits fails - the far server stopped between the commit's prepare and its commit, which is exactly the situation `bug-dead-remote-session-blocks-every-statement` exists for - the loop throws, the levels already ended stay ended, and the process's own transaction is *not* popped. The process is then one level deeper than the connections whose commit ran.

The rollback that follows does not notice. `ServerProcess.InternalRollbackTransaction` rolls back every connection for which `InTransaction` is true - that is, every connection with any level at all open:

```
foreach (RemoteSession remoteSession in RemoteSessions)
    if (remoteSession.InTransaction)
        remoteSession.RollbackTransaction();
```

For a connection that is now one level short, its innermost open level is the *outer* transaction - one the session has not finished with and may well go on to commit. Rolling it back throws away whatever was done on the other server inside that outer transaction, silently, while the local half of the same transaction is still live and can still be committed. The two servers then disagree about work the user believes is in one transaction.

What the user sees after that, since `bug-dead-remote-session-blocks-every-statement` landed, is an internal error on the next commit: that ticket added an assertion in `ServerProcess.InternalPrepareTransaction` that each connection's level count matches the process's, and this is the one way a correct engine can reach it. Before that assertion existed the same state produced a "no transaction in progress" error from the other server instead. Neither is a loss - the assertion is the better report of the two - but neither is the defect: the defect is the level that was rolled back.

### When arm 2 can be reached

Two things have to hold, and one connection is enough:

- the session has **more than one** transaction open at once, and
- a remote commit of the inner level fails - the far server stopped or became unreachable between that level's prepare and its commit.

At a single level the state repairs itself: the commit's failure leaves the connection at zero levels, the rollback skips it because `InTransaction` is false, and the process pops to zero as well, so the two agree again.

### How arm 2 was found, and what would confirm it

Read, not run: found reviewing `bug-dead-remote-session-blocks-every-statement`, by asking what else could reach the assertion that ticket added. Nothing was executed, so `repro: static`.

Confirming it means failing a remote commit *between* its prepare and its commit, with two transactions open. Two routes, and whoever takes this should pick one rather than fix on inspection:

- A fake `RemoteSession` whose commit throws on demand. The engine builds the class named by `ServerProcess.RemoteSessionClassName`, which is an internal field, and this repository has no `InternalsVisibleTo` convention - so this route needs one of those two things opened up first, and that is a decision worth making deliberately rather than in passing.
- A child server stopped at the right moment, as `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs` already stops one. The timing is the hard part: the window is between the prepare and the commit of the same statement.

### The fix for arm 2

One guard, at the one site. The rollback should roll back a connection only at the level the process is actually ending, rather than at any level at all:

```
if (remoteSession.TransactionCount == _transactions.Count)
    remoteSession.RollbackTransaction();
```

A connection whose commit already ran is then left alone and ends up agreeing with the process again, and in the ordinary case - where every connection is at the process's depth, which is the invariant recorded on `RemoteSession.TransactionCount` - the guard means exactly what `InTransaction` means today. It is also the shape `debt-remote-session-lazy-enlistment` would need if enlistment ever became lazy, since that decision replaces the invariant with a per-level one.

Worth deciding at the same time: whether `InternalCommitTransaction` should go on to the remaining connections after one of them fails, instead of throwing out of the loop and leaving them at a level the process has given up on.

## Why these are one ticket

Both arms are the same mistake: code that assumes a connection to another server is interchangeable with the process holding it, so a level can be addressed by the process's depth instead of by the connection's own. Arm 1 indexes the isolation level that way, arm 2 guards the rollback that way. Both are one expression in `src/engine/DAE/Server/ServerProcess.cs`, both want the same reproduction machinery, and whoever fixes either should read the other before deciding how to prove it.
