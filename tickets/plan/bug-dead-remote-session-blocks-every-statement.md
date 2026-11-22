description: Once a session has run a query on another Dataphor server and that other server later stops or becomes unreachable, every statement the session runs afterwards fails with a connection error - even a statement that has nothing to do with the other server - and there is no way to clear the dead connection except to disconnect the session.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, src/engine/DAE/Server/RemoteSession.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: It has behaved this way for as long as server links have existed and the user can recover by reconnecting, so a maintainer may reasonably rank it below work that loses data; and deciding what a dead transaction participant should do is a change to the transaction protocol, not a one-line repair.
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server; `ExecuteOn`, `EvaluateOn` and `select ... on <link>` run work on it. The first such statement opens a connection to that server and the engine keeps it for the life of the process that opened it, so later statements do not pay for a connection again.

Every statement on a process begins a transaction (implicitly, unless the session has turned implicit transactions off), and beginning one begins a transaction on **every** connection the process holds to another server - `ServerProcess.InternalBeginTransaction` in `src/engine/DAE/Server/ServerProcess.cs` loops over them. That is a call over the network. So once the other server has gone, the next statement on that process - any statement, `select 1 + 1` included - fails at its transaction begin with the connection error, before the statement itself runs.

The session is then stuck. There is no D4 operator that closes a connection to another server, and `drop server`, which since `engine-server-link-session-outlives-drop` would close it, cannot run either: it is a statement, so it fails at the same transaction begin. The only way out is to disconnect the session, or stop the process, and start again. That does work: the teardown path was made to complete whatever the network does by the same ticket.

The transaction begin is all-or-nothing (since `bug-process-begin-leaves-partial-transaction`), so each failed attempt leaves the process in a consistent state. It simply cannot do anything.

## How it was reproduced

Measured 2026-11-21, Debug, one machine, while implementing `engine-server-link-session-outlives-drop`.

In a fixture shaped like `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`: start a second Dataphor server as a child process, create a link naming it by host and port, run `EvaluateOn` on the link so that the connection opens, stop the child server, then run statements on the same process. Both `select 1 + 1;` and `drop server <link>;` fail with

```
System.ServiceModel.EndpointNotFoundException: Could not connect to net.tcp://127.0.0.1:<port>/<instance>/service/native ... TCP error code 10061
```

thrown from `RemoteSessionImplementation.BeginTransaction` inside `ServerProcess.InternalBeginTransaction`, and so does evaluating `Count(RemoteSessions where Process_ID = ProcessID())`. The case was written, run and then removed rather than committed, because it cannot pass until this is settled. The fixture that did land covers the behaviours that ticket fixed, including that stopping the process still works.

## What a fix has to settle

Stated as the shape of the problem rather than as a design:

- **What a failed remote begin should mean.** Candidates: treat that connection as dead and discard it, so the next statement that uses the link connects again - which is what a user would expect of a server that has come back; leave it in place but let statements that do not touch the link proceed; or keep refusing and give an operator a way to clear it. Only the first needs no new surface.
- **Where a discard is safe.** A connection that was a participant in an *open* transaction cannot simply be forgotten: work already done on it is lost, and the local transaction would commit believing the remote part committed too. A begin that fails is the easy case, because nothing of that level has been done remotely yet. A prepare, commit or rollback that fails on a dead connection is not, and `ServerProcess` enlists remote sessions in all four.
- **What the user should see.** Today they see a WCF exception carrying a socket error code and nothing that names the server link. Whatever the fix does, the error wants to name the link and the instance, as the warning `RemoteSessionImplementation.Dispose` now writes does.
- **Whether a reachability check belongs anywhere.** Beginning a transaction on a link the statement will not use costs a round trip even when everything is healthy. That is a separate, older question - when a link should be enlisted at all - and it may be worth answering at the same time.

## What is already in place

`engine-server-link-session-outlives-drop` made the teardown of a connection complete whatever the network does: `NativeCLISession.Dispose` releases this side in a `finally`, and `RemoteSessionImplementation.Dispose` logs a failed remote stop as a warning instead of throwing. So discarding a dead connection is now a safe thing to do; what is missing is anything that decides to do it.

The far-server half of the same situation - the *other* server keeping the abandoned session and its open transaction for as long as it runs - is `bug-native-cli-sessions-never-reclaimed`, which is planned and chained behind `native-cli-session-liveness-clock`. Different site, and neither fix implies the other.
