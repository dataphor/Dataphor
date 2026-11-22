description: When a program connects to the server through its simplified call interface and then disappears mid-transaction, the server keeps that work open for as long as it runs, and if the work had changed the database schema nobody else can change the schema again until the server is restarted. Clean up a session nobody has used for five minutes, the way the server already cleans up an abandoned client connection.
prereq: native-cli-session-liveness-clock
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Service/NativeSessionManager.cs, src/engine/Server/NativeCLI/NativeServer.cs, src/engine/Server/Service/ConnectionManager.cs, src/engine/Server.Hosting/DataphorServiceHost.cs, tests/DAE.Server.Tests/NativeSessionCleanupTest.cs, tests/DAE.Server.Tests/NativeSessionKeepAliveTest.cs, tests/DAE.Server.Tests/HostedInstance.cs, docs/modernization-plan.md, docs/migration-notes.md
difficulty: medium
repro: static
----
## What goes wrong

The server offers two ways in. The main one hands out *connections*, and a watcher reclaims a connection whose client has stopped saying "still here" (`src/engine/Server/Service/ConnectionManager.cs`; `port-abandoned-session-cleanup` settled and tested that path). The other is the simplified call interface — the "native" interface — which hands out *sessions* instead, identified by a generated identifier, and which nothing watches.

`NativeServer` holds its sessions in a dictionary keyed by that identifier (`src/engine/Server/NativeCLI/NativeServer.cs`). A session is created by one call and destroyed by another, and in between nothing decides whether its client is still alive. So a client that starts a session, begins a transaction, changes something, and then dies leaves that session and its open transaction in the server for as long as the server runs. Nothing times it out and nothing rolls it back.

Two consequences, in order of how much they hurt:

- **Schema changes stop working for everybody.** The catalog database allows one writer at a time, and a transaction that has changed the schema holds that write lock until it ends. An abandoned transaction in that state therefore blocks every other session's schema change — each failing after the lock wait with error 130103 — with no end, because nothing will ever end the abandoned transaction. On the main interface the same situation clears itself within a few minutes. Here it clears only when the server is restarted.
- **The session's own resources are held too**: its process, its plans and cursors, any locks its transaction took in a device, and the memory behind them.

The most likely route in a real deployment is not a hand-written client. A **server link** — one Dataphor server querying another — connects through exactly this interface (`src/engine/Server/Server/RemoteSessionImplementation.cs` uses the native client). So if server A links to server B and A is stopped, crashes, or loses the network mid-transaction, B carries A's abandoned work indefinitely. The other route is an external integration written against the interface, which is what it exists for.

This has always been so. The interface has never been .NET Remoting and so was never covered by the leases that used to reclaim the main interface's objects, and the connection watcher has never known about native sessions. It is not a regression introduced by moving to .NET 10.

`repro: static` — found by reading the code. The first test below is its reproduction and must be seen to fail before the watcher is wired in: start it against the tree as it stands with the watcher's construction commented out, watch the catalog change keep failing with 130103 until the case's own timeout, and say so in the handoff.

## The design

`native-cli-session-liveness-clock` (the prereq) has already given each native session a monotonic clock that every operation on it moves, an in-use count that is above zero exactly while a call is in flight, and the public read `NativeServer.GetSessionIdleSeconds()`; and it has given the client library `NativeCLISession` a keep-alive ping, so that a live client holding an idle session keeps its clock moving. What is left is the thing that decides, on a clock, that a session nobody has touched is abandoned, and ends it.

### A watcher of its own, beside the connection watcher

Add `src/engine/Server/Service/NativeSessionManager.cs`, in namespace `Alphora.Dataphor.DAE.Service` beside `ConnectionManager`, and give it that class's shape: a public constructor taking the thing it watches and, in a second overload, the threshold in seconds; one background watcher thread started from the constructor; a `ManualResetEvent` the thread waits on; a wake period of a quarter of the threshold with a floor of one second; an idempotent `Dispose` that signals the thread and joins it for at most `WatcherStopTimeoutInSeconds`; and the same guarded logging. Copy its remarks too, adjusted — in particular the explanation of why the thread is a background thread and why the join is bounded, which are the two things a reader will otherwise want to change.

Deliberately a second class rather than an extra responsibility on `ConnectionManager`, and deliberately not a shared watcher abstraction extracted from it: `port-abandoned-session-cleanup` and `test-client-server-abandoned` have only just settled and tested that mechanism, and refactoring it for tidiness now risks a freshly-proven path for no behaviour. Record that at the top of the new class as a `NOTE:` — the thread shape here is `ConnectionManager`'s, copied; if a third such watcher ever appears, extract one then.

### What the sweep does

The watcher calls one new method on `NativeServer`:

```csharp
/// <summary>Stops every session that has had no call on it for longer than the given number of seconds, and returns how many it stopped.</summary>
public int ReclaimIdleSessions(int idleSeconds)
```

Public for the same reason the prereq's read is, and with the same kind of remark. It works in two steps, and the split matters:

- **Under `lock (_nativeSessions)`:** walk the sessions, select those whose in-use count is zero and whose last-activity tick is older than the threshold, and remove each selected one from the dictionary. Deciding and removing must be one atomic step, or a session another thread is about to acquire can be reclaimed between the decision and the removal. This is the one place the native interface has an advantage over the main one: the plan records, for connections, that "barring the watcher from a connection with a call in flight would need the handle-to-connection plumbing that does not exist". Here every operation goes through one acquire, so the plumbing is already there and the watcher can simply leave a busy session alone. Say so at the site.
- **Outside the lock:** call the existing `StopNativeSession` on each removed session, each inside its own `try`/`catch` so that one failure does not end the sweep, and log each reclamation. Outside the lock because stopping a session's process can wait for half of `Engine.ProcessTerminationTimeout` — 15 seconds by default — and holding the dictionary lock for that would stall every other client's call. Carry over `ConnectionManager`'s note that the sweep is serial, so one slow stop delays the rest, with the same remedy recorded if it is ever seen.

`StopNativeSession` already stops the process and disconnects the session, and that is what rolls the transaction back: `IServerSession.StopProcess(IServerProcess)` disposes the `ServerProcess`, which rolls back its open transactions and so releases the catalog store's write lock. Nothing new is needed for the rollback.

### What a returning client sees, and what the log says

**No new error and no new error code.** A client that comes back to a reclaimed session gets the "Invalid session handle" `ArgumentException` the dictionary lookup already raises, wrapped by `NativeCLIUtility.WrapException` as every other native error is. Remembering reclaimed handles so as to answer "reclaimed after N seconds of inactivity" instead would mean unbounded bookkeeping or an eviction policy for it, and a new error code is a compatibility surface. The alternative is recorded here rather than built.

**The server log is where a reclamation is visible**, as it is for a connection. One line per reclaimed session naming the session identifier, the user, the client host name from its `NativeSessionInfo`, how long it had been without a call, and whether it had an open transaction — that last is what tells an operator whether anyone else was being blocked. `NativeServer` holds its server as `IServer`, which has no logging, so reach the log the way `ConnectionManager` does, through the concrete `Alphora.Dataphor.DAE.Server.Server`; read the field once, tolerate it being absent, and guard the logging call itself, for the reasons `ConnectionManager.RelinquishIdleConnections` writes out at length (a sweep overlapping a server stop writes to a log that has been closed, and an exception escaping there kills the watcher thread for the life of the process).

### Where the clock lives

A compiled-in default of 300 seconds, as `NativeSessionManager.IdleTimeInSeconds`, and a host-object property `DataphorServiceHost.NativeSessionIdleTimeInSeconds` defaulting to it — exactly the shape and the justification of `ConnectionIdleTimeInSeconds`, which `test-client-server-abandoned` added for the connection clock. Copy that property's remarks, changing what differs: the floor here is the keep-alive interval of the clients that will connect, which for the shipped client library is `NativeCLISession.DefaultKeepAliveInterval` (two minutes), and the value is passed through unchecked to the manager's constructor, which refuses anything below one second.

**Not an instance-configuration setting, and the same number as the connection clock without sharing a constant.** Two separate constants of the same value, because their floors are set by different clients — the connection's by the WinForms client's compiled-in keep-alive interval, the native one's by the native client library's — and because a test needs to shorten one without touching the other. The question of whether *either* should become something an operator can set in the instance list is one question and already has an answer recorded, at `ConnectionManager.IdleTimeInSeconds`: not until a deployment reports the problem, and then as an additive `[DefaultValue]` property on `ServerConfiguration` with a validated floor, a user-guide section and a migration note. Point the new constant's note at that one rather than writing a second copy of the reasoning.

### Lifecycle in the host

`DataphorServiceHost.Start` builds the manager where it builds `DataphorService` from the connection clock, and `Stop` disposes it in the block that disposes `_service` — before `_remoteServer.Dispose()` and before `_server.Stop()`, which is what makes the bounded join in `Dispose` sufficient and keeps a sweep from running during the engine's teardown. `Stop` tolerates a half-built state, as its own remark says, so the field may be null.

Nothing stops the remaining native sessions at shutdown and nothing needs to: `Engine.Stop` calls `CloseSessions`, which disposes every session the engine holds, native ones included.

## Edge cases & interactions

- **A session with a call in flight is never reclaimed.** The in-use count is read inside the same lock as the removal. Pinned by the second test below.
- **A session acquired between the decision and the stop.** Impossible: the removal is inside the lock, so a later `AcquireNativeSession` finds nothing and raises the invalid-handle error rather than handing out a session that is being stopped. By inspection.
- **A client's `StopSession` racing the sweep.** Whichever removes from the dictionary first wins; the other raises the invalid-handle error. For a server link that means `NativeCLISession.Dispose` propagates the failure and `RemoteSessionImplementation.Dispose` logs it as a warning and releases the local side — the path `engine-server-link-session-outlives-drop` built. Verify by reading those two methods and say so in the handoff.
- **A brand-new session swept before its first call.** Its clock is stamped in its constructor (the prereq), so it starts with zero idle time. By inspection.
- **A session whose process will not stop when aborted.** The sweep waits for it and the rest of that sweep is delayed, exactly as a connection sweep is. Noted at the site, not solved.
- **A session that started asynchronous work and then went quiet.** D4's `ExecuteAsync` runs a script on a *sibling* process of the same session, so the native session's own in-use count is zero while that work runs; after the threshold the session is reclaimed and disconnecting it aborts the sibling process. That is the intended answer for a client that has made no call for five minutes, and no client using the shipped library reaches it because the keep-alive keeps the clock moving. Named here because it is the one case where "idle" and "doing nothing" differ; by inspection.
- **A reclamation overlapping server shutdown.** The manager is disposed before the engine stops, and the sweep's logging is guarded. By inspection, following `ConnectionManager`'s notes.
- **The stateless `Execute(NativeSessionInfo, ...)` overloads.** Their sessions never enter the dictionary, so the sweep cannot see them and a call of any length is unaffected. By inspection.
- **A client that speaks the WCF contract directly and does not use the shipped library.** It cannot ping, so a session it holds without calling anything for five minutes is now reclaimed where before it was kept for ever. That is the deliberate cost of the fix and the subject of the migration note; an integration that needs longer has the host property, and one that holds a transaction open across five idle minutes is already blocking every other session's schema change, which is the defect being fixed.
- **A 3.1 client against a 4.0 server.** The same case as above: 3.1's client library does not ping. Covered by the same note.

## Tests

Two fixtures. Neither is `[Parallelizable]`, for the reason `TestServerInstance`'s note gives.

**`tests/DAE.Server.Tests/NativeSessionCleanupTest.cs`** — new, and deliberately a close parallel of `AbandonedSessionCleanupTest`, which does the same thing for the main interface. Read that file first: its `AssertTheCatalogWriteLockIsHeld`, `WaitForACatalogChangeToSucceed` and `FindError` helpers, its one-second threshold and its thirty-second reclamation wait are the shape to follow. The lowest layer that shows the behaviour: a `TestServerInstance`, a `NativeServer` built over its `Server`, and a `NativeSessionManager` at a one-second threshold, all in process. No WCF host, no port, no network.

- `AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased` — the reproduction. Start a native session, begin a transaction on it, create a table through it, assert that the catalog store's write lock is held (before the watcher exists, so that the case cannot pass vacuously on a server that never took the lock), then start the manager and wait for another session's `create table` to succeed. Afterwards: the abandoned table is gone rather than committed, the work done after the reclamation is real, and a call on the reclaimed handle fails. Set the instance's `CatalogStoreConnectionString` with `;BusyTimeout=1000` as the sibling fixture does, so a blocked schema change reports in a second rather than in thirty.
- `ASessionWithACallInFlightIsNotReclaimed` — the branch that is easiest to get wrong. With the manager at a one-second threshold, run `Sleep(5000)` on the session from a background task (`Sleep` is a built-in operator, declared in `src/engine/DAE/Schema/SystemCatalog.d4`, so it needs no library). While that runs the session must not be reclaimed: the long call completes, and a further call on the same handle afterwards succeeds. Without the in-use count this case fails, which is what makes it worth its run time.

**`tests/DAE.Server.Tests/NativeSessionKeepAliveTest.cs`** — the fixture the prereq creates, which already hosts a real endpoint through `HostedInstance` and connects a `NativeCLISession` to it. Add the pair of cases that prove the compatibility claim the whole design rests on, both with the host's `NativeSessionIdleTimeInSeconds` set to a few seconds:

- `ASessionTheClientKeepsAliveIsNotReclaimed` — keep-alive interval one second; after several thresholds have passed with no call from the test, a call on the session still succeeds. This is the case that says a server link survives an idle user session.
- `ASessionWithNoKeepAliveIsReclaimed` — the same, with the keep-alive turned off (interval 0); the next call fails. Its only job is to keep the case above from passing for the wrong reason.

`HostedInstance`'s constructor starts the host, and `NewHost` is where its host-object properties are set, so it needs a way to carry the threshold — an additional constructor parameter, or an overload taking the value, whichever reads better beside the existing parameters. Keep the existing constructors' behaviour unchanged: every other fixture uses them.

## Documentation

- **`docs/modernization-plan.md`**, under WS2: replace the paragraph beginning "The door still open: native CLI sessions are reclaimed by nothing" with what landed. It sits among the bullets that record the connection watcher's decisions, so match them: what the mechanism is, the four decisions above with their reasons (a separate watcher rather than a shared abstraction; the in-use count, which the connection side could not have; no new error code and the log as the operator-visible record; the clock as a host property with the same value as the connection's and the instance-setting question still answered at `ConnectionManager`), what rests on the tests and what rests on inspection, and the one cost — a client that speaks the contract directly and cannot ping.
- **`docs/migration-notes.md`**, *Custom C# extensions*, beside the prereq's entry: a native session that nothing calls for five minutes is now ended and its transaction rolled back, the server log records each one, a later call on it fails with "Invalid session handle", the clock is `DataphorServiceHost.NativeSessionIdleTimeInSeconds` and defaults to 300 seconds, and `NativeSessionManager` is new. Under *What to do*: nothing for an application or for a server link, because the shipped client library pings; an integration that speaks the service contract directly must either call something on its sessions within the threshold or be given a longer one by its host.

## TODO

- Add `NativeServer.ReclaimIdleSessions(int)`: select and remove under the dictionary lock, stop and log outside it, one `try`/`catch` per session, with the comments and notes described above.
- Add `src/engine/Server/Service/NativeSessionManager.cs` on `ConnectionManager`'s shape, with its own `IdleTimeInSeconds` constant, the two constructors, the background watcher, the idempotent bounded-join `Dispose`, and the `NOTE:` on the copied thread shape.
- Add the cross-reference from `ConnectionManager.IdleTimeInSeconds`'s note to the new constant, so that the recorded revisit condition is reachable from both.
- Add `DataphorServiceHost.NativeSessionIdleTimeInSeconds`, build the manager in `Start` and dispose it in `Stop` before the remote server and the engine.
- Write `NativeSessionCleanupTest` with its two cases; confirm the first fails before the manager is wired in, and record that in the handoff.
- Give `HostedInstance` a way to set the new threshold before the host starts, leaving the existing constructors' behaviour alone.
- Add the two cases to `NativeSessionKeepAliveTest`.
- Rewrite the WS2 paragraph in `docs/modernization-plan.md` and add the `docs/migration-notes.md` entry.
- `dotnet build Dataphor.slnx`, then the targeted runs (`dotnet test tests/DAE.Server.Tests --filter "Name=AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased|Name=ASessionWithACallInFlightIsNotReclaimed" --logger "console;verbosity=normal"`, then the keep-alive fixture), then the three suite parts from the root `AGENTS.md`, each as its own foreground command.
