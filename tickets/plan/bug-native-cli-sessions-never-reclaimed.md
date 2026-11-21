description: When a program connects to the server through its simplified call interface and then disappears mid-transaction, the server keeps that work open for as long as it runs; nothing ever cleans it up, and if the work had changed the database schema, nobody else can change the schema again until the server is restarted.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/NativeCLI/NativeServer.cs, src/engine/Server/NativeCLI/NativeSession.cs, src/engine/Server/NativeCLI/NativeCLIService.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, src/engine/Server/Service/ConnectionManager.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: It has behaved this way since the interface was introduced and needs a client that dies inside a transaction to hit, so a maintainer may reasonably wait for a deployment to report it; the fix is not a small one — it needs a liveness scheme designed for an interface that has no keep-alive message and no way to add one without changing a published contract.
----
## What goes wrong

The server offers two ways in. The main one hands out *connections*, and a watcher reclaims a connection whose client has stopped saying "still here" (`src/engine/Server/Service/ConnectionManager.cs`; `port-abandoned-session-cleanup` settled and tested that path). The other is the simplified call interface — the "native" interface — which hands out *sessions* instead, identified by a generated identifier, and which nothing watches.

`NativeServer` holds its sessions in a dictionary keyed by that identifier (`src/engine/Server/NativeCLI/NativeServer.cs`). A session is created by one call and destroyed by another, and in between the server records nothing about whether its client is still alive: there is no last-use time, no idle sweep, and no keep-alive message in the interface's contract to carry one. So a client that starts a session, begins a transaction, changes something, and then dies leaves that session and its open transaction in the server for as long as the server runs. Nothing times it out and nothing rolls it back.

Two consequences, in order of how much they hurt:

- **Schema changes stop working for everybody.** The catalog database allows one writer at a time, and a transaction that has changed the schema holds that write lock until it ends. An abandoned transaction in that state therefore blocks every other session's schema change — each failing after the lock wait with error 130103 — with no end, because nothing will ever end the abandoned transaction. On the main interface the same situation clears itself within a few minutes. Here it clears only when the server is restarted.
- **The session's own resources are held too**: its process, its plans and cursors, any locks its transaction took in a device, and the memory behind them.

## How a real deployment reaches it

The most likely route is not a hand-written client. A **server link** — one Dataphor server querying another — connects through exactly this interface (`src/engine/Server/Server/RemoteSessionImplementation.cs` uses the native client). So if server A links to server B and A is stopped, crashes, or loses the network mid-transaction, B carries A's abandoned work indefinitely. The other route is an external integration written against the interface, which is what it exists for.

## Why it is not simply an oversight of the port

This has always been so. The interface has never been .NET Remoting and so was never covered by the leases that used to reclaim the main interface's objects, and the watcher that reclaims connections has never known about native sessions. Finding it is a by-product of establishing what reclaims an abandoned session; it is not a regression introduced by moving to .NET 10.

## One thing has changed since this was filed, on the clock question

`test-client-server-abandoned` adds `DataphorServiceHost.ConnectionIdleTimeInSeconds` — a property on the host object, defaulting to `ConnectionManager.IdleTimeInSeconds` (300 seconds), threaded through a new `DataphorService(RemoteServer, int)` overload to the two-argument `ConnectionManager` constructor that `port-abandoned-session-cleanup` already added. It exists so that a test which starts a server in a child process can shorten the reclamation clock enough to watch a reclamation happen; it joins `HostName` and `LoggingEnabled`, the other two things a host needs that an instance configuration cannot carry.

Read it before designing this, but do not read it as an answer to anything here. It answers neither of the two questions in the last bullet below: it is not an operator setting (nothing in the instance list or the command line changes, deliberately, so that this ticket stays free to decide whether either clock should become one), and it says nothing about where a clock for the native interface should live. What it does mean is that the existing connection clock is now reachable by whatever owns the host, which may or may not be a useful shape for the native side too.

## What a fix has to settle

Stated as the shape of the problem, not as a design — that is the planning stage's work:

- **Liveness without a keep-alive message.** The contract `INativeCLIService` is a compatibility surface and adding an operation to it is not available without a decision that says so. The promising alternative is that every operation on a session goes through one place in the server (`NativeServer.GetNativeSession`), which could stamp a last-use time with no contract change at all.
- **Not reclaiming a session that is merely busy.** Stamping on entry is not enough on its own: a call that runs for an hour would leave its stamp an hour old and a sweep would reclaim a session that is working. Whatever is built needs to know a session is in use, which means the entry and exit of each operation, not just its entry.
- **What "reclaim" means here.** `NativeServer.StopSession` already stops the process and disconnects the session, which rolls the transaction back; the question is who calls it, on what clock, and what the client sees afterwards if it comes back.
- **Where the clock lives.** The connection watcher's threshold is a compiled-in constant for the reasons `port-abandoned-session-cleanup` recorded; whether the native side shares it, and whether either should become an operator setting, is one question, not two.

## How to confirm it before fixing it

`repro: static` — this was found by reading the code, not by watching it happen. What would confirm it: start a server, open a native session, begin a transaction, run a `create table`, then abandon the session without stopping it (drop the client, or simply never call `StopSession`), and watch another session's `create table` fail with 130103 and keep failing. The in-process pieces for this already exist — `tests/DAE.Server.Tests/CatalogStoreConcurrencyTest.cs` shows how to shorten the lock wait, and the fixture `port-abandoned-session-cleanup` adds (`tests/DAE.Server.Tests/AbandonedSessionCleanupTest.cs`) is the same scenario on the main interface, where it does clear.
