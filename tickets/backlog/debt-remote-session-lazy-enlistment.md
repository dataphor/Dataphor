description: Every statement a session runs opens a transaction on every other Dataphor server that session has ever queried, even when the statement has nothing to do with those servers - one network round trip each, per statement. Decide whether those servers should instead be brought into a transaction only when the statement actually uses them.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/RemoteSession.cs
tradeoffs: Opening the remote transaction at the same moment as the local one is what makes a remote read see the same point in time as the rest of the transaction, so bringing a server in late is a real change to what a query sees and not only a saving - and the cost it removes is one round trip per statement per linked server, which only a deployment that uses server links heavily would ever notice.
----
## What happens now

A *server link* is a D4 object naming another Dataphor server; the first `ExecuteOn`, `EvaluateOn` or `select … on <link>` opens a connection to it and the process keeps that connection. Beginning a transaction on the process then begins one on **every** connection it holds (`ServerProcess.InternalBeginTransaction`), and with implicit transactions on - the default - every statement begins a transaction. So a session that queried another server once pays a network round trip to that server on every statement it runs afterwards, including statements that will never touch the link, and pays another at the commit and another if it nests.

The machinery to bring a connection in later already exists and is used: `ServerProcess.RemoteConnect` brings a connection that is used for the first time *inside* open transactions up to the process's depth before the work runs. Nothing would have to be invented to make every connection work that way - the eager begin could stop and the catch-up could carry the whole job.

## Why it is not just a saving

Opening the remote transaction at the same time as the local one is also what fixes the point in time the remote work sees. A connection brought in at the moment of first use inside a transaction begins its remote transaction later, so under an isolation level that gives a consistent snapshot it reads a *later* state than the rest of the transaction - and two reads on the same link inside one transaction would be consistent with each other but not with when the transaction started. Whether that matters is a question about what a transaction spanning two Dataphor servers is supposed to promise, which no document in the repository answers today.

So this is a decision, not a cleanup. The two shapes to weigh:

- **Keep enlisting eagerly.** Every connection the process holds is in the transaction from its start, remote reads line up with the local transaction's start, and the cost is the round trips above.
- **Enlist on first use.** No cost for a link a statement does not touch, the common case for a session that queried another server once; remote reads see the state at first use rather than at the transaction's start; `InternalPrepareTransaction`, `InternalCommitTransaction` and `InternalRollbackTransaction` have to stop assuming every connection is at the process's depth and act per level instead, and the invariant that `bug-dead-remote-session-blocks-every-statement` establishes - every connection in the list has begun exactly as many levels as the process has open - is replaced by a weaker one that each of those three has to respect.

A third possibility worth costing while deciding: keep enlisting eagerly but only for links the plan being executed actually names, which the compiler knows.

## Where this came from

Named as the fourth open question on `bug-dead-remote-session-blocks-every-statement` ("whether a reachability check belongs anywhere"), and parked there deliberately. That ticket's fix does not depend on which way this goes: it makes a dead connection recoverable at the transaction begin, and if enlistment later becomes lazy the same decision moves with the begin rather than disappearing. The round-trip cost has not been measured; whoever takes this should measure it first - a session holding one linked connection, running a statement that does not use it, with and without the eager begin - because the decision above is only worth making if the saving is real.
