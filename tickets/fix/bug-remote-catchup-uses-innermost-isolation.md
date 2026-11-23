description: When a query on another Dataphor server is the first one a session runs inside nested transactions, the transactions it opens on that other server are all opened at the innermost transaction's isolation level instead of each at its own, so the remote query can see more or less uncommitted data than the transaction it is running in allows.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs
repro: static
severity: wrong-result
likelihood: contrived
tradeoffs: It needs nested transactions whose isolation levels differ *and* a server link used for the first time inside the nesting, which is a combination no shipped library or sample does, and the one-expression fix has no cheap way to be observed from a test - so a maintainer may reasonably want it done at the same time as something else in this area rather than on its own.
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server. The first `ExecuteOn`, `EvaluateOn` or `select … on <link>` opens a connection to that server, and the process that opened it keeps it. If the process already has transactions open when that first use happens, the connection has to be brought up to the same depth before the work can run, which `ServerProcess.RemoteConnect` does:

```
while (session.TransactionCount < _transactions.Count)
    session.BeginTransaction(_transactions[_transactions.Count - 1].IsolationLevel);
```

The isolation level it passes is `_transactions[_transactions.Count - 1]`, the **innermost** open transaction's, and it passes that same one for every level it opens. The levels it is catching up to are the outer ones, each of which has its own isolation level - D4 lets a transaction be begun at a named one (`begin transaction isolation level ...`) - so when the levels differ, every transaction opened on the other server but the last gets the wrong one. The expression a reader would expect is the level being begun, `_transactions[session.TransactionCount]`.

What that costs is the thing an isolation level buys: a remote read inside the outer transaction can see uncommitted data it should not, or fail to see committed data it should, depending on which way the two levels differ. The local half of the same transaction is unaffected - this expression reaches only the connection to the other server.

## When it can be reached

All three have to hold:

- the session has more than one transaction open at once, and
- at least two of those levels were begun at different isolation levels, and
- a server link is used for the **first time on that process** while they are open - a link already connected is kept up to date one level at a time by `ServerProcess.InternalBeginTransaction`, which passes each level its own isolation level correctly.

No shipped library, sample or test script in the repository does that combination, which is why this is filed as a defect nobody has met rather than as an active bug.

## How it was found, and what would confirm it

Read, not run: found while planning `bug-dead-remote-session-blocks-every-statement`, which rewrites the surrounding lines and deliberately leaves this expression's meaning alone so the two do not collide. Nothing was executed, so `repro: static`.

What would confirm it is a reading of the transaction's isolation level **on the other server**, which is the part with no cheap answer today: `System.Processes` on that server does not report the isolation level of an open transaction, so showing the wrong level either means adding a way to ask - which is a change of its own - or building a case out of two sessions and a read that only one isolation level permits, against a second Dataphor server in a child process. Whoever takes this should decide which, or record that the fix rests on inspection, before changing the line: the project's rule is that a bug fix owes its reproduction, and this one has to be argued rather than assumed.

## The fix

One expression, at the one site. `ServerProcess.RemoteConnect` is the only place in the tree that opens more than one transaction on a connection at once; `InternalBeginTransaction` opens one level at a time and already passes that level's own isolation level. Note that after `bug-dead-remote-session-blocks-every-statement` lands, `session.TransactionCount` is a local field rather than a call to the other server, so the index is free to read.
