description: When a client machine disappears in the middle of a transaction, the server now notices and releases what that client was holding; this was settled, made reliable, and pinned by a test that proves the abandoned work is rolled back and other people's schema changes stop being blocked.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Service/ConnectionManager.cs, src/engine/Server/Server/RemoteServer.cs, src/engine/Server/Server/RemoteServerConnection.cs, tests/DAE.Server.Tests/AbandonedSessionCleanupTest.cs, tests/DAE.Server.Tests/ConnectionKeepAliveTest.cs, docs/modernization-plan.md, docs/architecture.md, docs/migration-notes.md
----
## What landed

The question the plan stage asked — what reclaims a session when its client vanishes, now that .NET Remoting leases are gone — is answered: **the server's own idle watcher, with the client's keep-alive ping as its liveness signal.** Nothing new was invented. The watcher was already in `ConnectionManager`; the work corrected it, documented it and pinned it with a test.

The implement stage is commit `ticket(implement): port-abandoned-session-cleanup`. The review changed four things in that work and corrected two documentation sentences; both stages are described together below, with the review's own changes marked.

**`src/engine/Server/Service/ConnectionManager.cs`** — the same loop, corrected around four points.

- The idle threshold became an instance field with `public const int IdleTimeInSeconds = 300` as its default (the constant itself is untouched). A new constructor `ConnectionManager(RemoteServer, int)` takes the threshold and refuses anything below one second, following `LocalServer`'s interval constructor; the old constructor delegates to it. This is what lets a test drive the real watcher.
- The wake period is separated from the threshold at `Math.Max(1, idle / 4)`. The two used to be one constant, so a connection that went quiet just after a wake was reclaimed at the *second* wake past the threshold: the bound was 300 to 600 seconds and is now 300 to 375.
- The sweep is extracted into `RelinquishIdleConnections(DateTime)`, which reads `_remoteServer.Server` once and returns if it is null. A sweep overlapping the server's teardown used to re-read it inside its own logging and raise there, and the blanket catch in `LifetimeWatcher` swallowed that, silently killing the watcher thread for the life of the process.
- `Dispose` is idempotent and waits up to `WatcherStopTimeoutInSeconds` (10 s, matching `NetTcpServiceHost.StopTimeout`) for the watcher, so no sweep is still reading the server while it is torn down. The thread stays a background thread, so a wait that gives up cannot hold the process open.
- **Review: the sweep's own error reporting can no longer end the watcher.** The extracted guard covers a disposed `RemoteServer`; it does not cover a *disposed engine*, whose `LogMessage` writes to a log file the server closes when it stops. A `LogMessage` that failed inside the per-connection `try` was caught, and the `server.LogError(exception)` in that catch then failed too and escaped to `LifetimeWatcher`'s catch — the exact failure the extraction was meant to retire, one layer out. The reporting call is now guarded; dropping a log line is the cheaper loss. See *Review findings*.

**`src/engine/Server/Server/RemoteServerConnection.cs`**

- `Dispose(bool)` returns at the existing `_disposed` field, so a second disposal does nothing at all, including not re-raising `Disposed`.
- `LastActivityTime` documents what it is — a last-*ping* time, not a last-call time — and why nothing else can move it without new plumbing.
- **Review: the teardown is serialized per connection, so the idempotence holds across threads and not only in sequence.** Two threads relinquish one connection (the idle watcher, and the client's own `CloseConnection` through the handle table), and that is the case the guard exists for; but a plain `if (_disposed) return` is one both can pass, and `CloseSessions` ran under no lock at all. `Dispose` now claims the teardown under a private lock and sets `_disposed` there, and `CloseSessions` runs under the same lock and reads the fields it needs into locals. See *Review findings*.
- `Disconnect` tested the *parameter* rather than the result of its `as` cast, so a session object from elsewhere passed the test and raised a `NullReferenceException` on the null local — the same defect as `Relinquish`, one method away. Fixed the same way, with the same typed error (106191); a null session stays a no-op.

**`src/engine/Server/Server/RemoteServer.cs`**

- `Relinquish` checks the cast instead of dereferencing it, raising error 106191 (`TypedObjectContainer`, the code a `RemoteServerConnections` container already raises for the same mismatch) for a connection that is not one of ours. Its remarks record that it is idempotent, which step tolerates having been taken already, and why `CloseSessions` stays outside the server's lock.
- **Review: a `NOTE:` at `AttachServer`** — nothing calls it, so a `RemoteServer` never learns its engine has been disposed. Dead code rather than a defect (`DetachServer`, which `Dispose` does call, unsubscribes a handler that was never added, which is a no-op), and the note says what would have to change if a second reader ever depended on `Server` going null.

## The four facts the plan stage asked to be checked

All four held when read against the code. Two refinements:

- **`LastActivityTime` is a last-ping time.** Confirmed: set in the constructor and in `Ping()` and nowhere else, and there is no handle-to-connection path to stamp from an ordinary call.
- **A long single call is not at risk while the client pings.** Confirmed, but the plan's wording "so long as a session is open" was already stale: since `port-connection-keepalive` the ping belongs to the connection, so a connection with no session open is pinged too. Corrected in the code comments and the plan text.
- **The bound was 300 to 600 seconds.** Confirmed: the wake period and the threshold were the same constant.
- **Reclaiming twice was unsafe.** Confirmed and reproduced: the second `Relinquish` reached `RemoteServerConnection.Dispose`, dereferenced the `_sessions` field the first had nulled, and raised a `NullReferenceException` wrapped as a `DataphorException`.

On the shutdown race the plan described, one correction: it is narrower than stated but real. `RemoteServer.Dispose` nulls `_connections` **before** `_server`, so a sweep that starts after `Dispose` has finished sees an empty snapshot and does nothing. The window is a sweep that takes its snapshot just before `Dispose` nulls `_connections` — `Dispose` does not take the server's lock — and then logs after `_server` has gone. The bounded join plus the ordering in `DataphorServiceHost.Stop` (`_service.Dispose()` before `_remoteServer.Dispose()`) closes that for the hosted path; the stopped-server guard covers a host that disposes them in the other order.

## What an in-flight call on a reclaimed connection sees

Three steps, each read in the code:

- `ServerSession.StopProcesses` calls `Engine.TerminateProcessThread` (`src/engine/DAE/Server/Engine.cs`), which sets `IsAborted` and spins for half of `Engine.ProcessTerminationTimeout` (15 s by default) waiting for the executing thread; if the thread will not stop it raises `ProcessNotResponding`, which `StopProcesses` logs before disposing the process anyway.
- `ServerProcess.Dispose` rolls back every open transaction before unpreparing deferred constraint checks, and leaves an application transaction first — pre-existing behaviour, unchanged here.
- Disposal raises `Disposed`, and `HandleManager.ObjectDisposed` removes that object's handle from both of its dictionaries, so the next call on it reaches `HandleManager.GetObject`, which raises error 106205, unknown object handle. The same happens at all three levels (connection, session, process). Nothing in the handle table is corrupted: the handle is withdrawn, not left pointing at a disposed object.

## Tests

Both in `tests/DAE.Server.Tests/AbandonedSessionCleanupTest.cs`, in process over a `RemoteServer` plus a `ConnectionManager` at a one-second threshold. No WCF host, no port.

| Test | What it verifies |
|---|---|
| `AnAbandonedTransactionIsRolledBackAndTheCatalogWriteLockReleased` | The whole chain: a connection that is never pinged is relinquished; the explicit transaction it left open is rolled back rather than committed (the table it created does not resolve, with an expression never compiled before); the catalog store's write lock is released, so another session's `create table` succeeds; and a call on the reclaimed process fails instead of reaching the disposed objects behind it. Before the watcher exists it also asserts that the lock was genuinely held (error 130103 at the busy timeout), so the final wait cannot pass on a server where no lock was ever taken |
| `RelinquishingAConnectionTwiceDoesNothing` | The reproduction for the double-reclamation defect; no timing |

The abandoned client goes through the remote CLI directly (`RemoteServer.Establish` → `Connect` → `StartProcess` → `BeginTransaction` → `ExecuteScript`). A `LocalServer` would have pinged the connection — the one thing this case must not have — and started a second engine. Reclamation measured at about 1.1 s at a one-second threshold; the lock-held assertion before it takes about 1.2 s, which is the store's busy timeout; the fixture runs in about 9 s.

Both cases were confirmed to fail before the change: with the `_disposed` guard removed the second fails with `NullReferenceException` at `RemoteServer.Relinquish`; the first cannot run against the pre-change code at all (the injectable threshold is part of the change), so it was negative-controlled at a 600-second threshold, where it fails at its wait.

## Documentation

- `docs/modernization-plan.md`: the WS2 "Remoting leftovers" note states the settled mechanism; the work-item row is marked **Done**; a new `### Reclaiming an abandoned session` subsection carries the mechanism, why it is not the network channel, what follows from the ping being the signal, the four decisions, which claims rest on the test and which on inspection, and the native CLI gap. The WS5 hand-on bullet is amended.
- `docs/architecture.md`, "Catalog and catalog store": the sentence that forward-referenced this ticket is now the settled statement, with the 300-to-375-second bound, the ping as the only thing keeping a connection alive, and the native CLI exception.
- `docs/migration-notes.md`: three rows — the bound on a session abandoned inside a catalog-changing transaction, the timing change under *Platform and deployment*, and the idempotent relinquish, the typed errors and the new `ConnectionManager` constructor and `Dispose` behaviour under *Custom C# extensions*.
- **Review: two sentences in `docs/architecture.md` corrected.** The shutdown-order paragraph said the service's disposal "signals" the idle watcher, which is no longer the whole of it — it now also waits up to ten seconds for a sweep in flight, which is what the claim about the shutdown window rests on. And the paragraph below it still said `RemoteServerObject` "manipulates .NET Remoting leases", which `server-remove-remoting-and-dtc` removed. Both are in the file this change edited, in the paragraphs about this subsystem.

## Review findings

**What was checked.** The implement diff read before the handoff. Both new test cases read against the four claims the plan asked about, and the step order of the first case checked for whether its lock-held assertion is load-bearing (it is: it runs before the `ConnectionManager` exists, so nothing can reclaim the connection while it is measured, and nothing but the reclamation can release the lock afterwards). Every concurrent path into `Relinquish` enumerated and followed (`ConnectionManager` sweep, `DataphorService.CloseConnection` through the handle table, `RemoteServer.Dispose` → `CloseConnections`, and `RemoteServer.Stop`, which has no caller in the tree). The disposal and null-ordering in `RemoteServer.Dispose`, `DataphorService.Dispose` and `DataphorServiceHost.Stop` traced by hand. Every factual claim in the three documents checked against the code: `LocalServer.DefaultKeepAliveInterval` is 120, `NetTcpServiceHost.StopTimeout` is 10 s, `ReceiveTimeout` is `TimeSpan.MaxValue` in both binding declarations, 106191 is `TypedObjectContainer` and is what five sibling containers already raise, 130103 is `StoreException.Codes.StoreLocked`, the quarter-threshold arithmetic gives 300 seconds up to but not including 375, and `bug-native-cli-sessions-never-reclaimed` is on the board in `plan/`. Source hygiene: line endings, byte-order marks and tabs preserved; `ConnectionManager.cs` is 213 lines. The `as`-cast-then-dereference class swept across the whole tree (`grep -rn " as Remote" src/`): the two sites the implement stage fixed were the only ones, the rest are container `Validate` overrides. Lint here is the build's warnings: 143 in Debug and in Release, none in any touched file, unchanged from before.

**Fixed in this pass (minor).**

- **The documented idempotence did not hold across threads.** `Dispose`'s `if (_disposed) return` is a guard two threads can both pass, and `CloseSessions` — which `Relinquish` calls first, outside any lock — read `_sessions` twice and called `DisownAt(0)` on a count it had read earlier. So a watcher sweep and a client's `CloseConnection` arriving together could still null-dereference, or remove an item the list no longer held, which is what the ticket set out to retire. The window is not microseconds: `CloseSessions` can take 15 s per process that will not stop when aborted, and the handle the client resolves is withdrawn only at the end. Both methods now run under one private lock per connection and read the fields they need into locals. Verified by inspection rather than by test: the race has no seam to drive it through, and a stress test for it would be nondeterministic in both directions.
- **The sweep's error handling could still kill the watcher** (described above, under `ConnectionManager`). Also by inspection: the window is a `StreamWriter` the engine closes while a sweep is between its two logging calls.
- **Two stale sentences in `docs/architecture.md`** (described above).
- **A `NOTE:` at `RemoteServer.AttachServer`**, which nothing calls.

**Tests.** No test added and none cut. The two the implement stage wrote both pay for themselves under this project's rule — the first pins the specification this ticket settled, the second is the reproduction of a defect it fixed — and neither restates the implementation or verifies a mock. The amendment to the existing `NOTE:` in `ConnectionKeepAliveTest` is accurate: that fixture's in-process path no longer reaches the client's own guard, the case is still worth keeping for the client contract it asserts, and it passes. Nothing new was owed for the review's own two code fixes, for the reason given against each.

**Weighed and not filed.**

- **A call on a dead `Remote*` object raises `NullReferenceException`, not a Dataphor error.** Class-wide and older than this mechanism: every `Remote*` object nulls its inner reference on disposal and no member checks for it. Unreachable in the product — over the wire the handle table withdraws the handle first, and the only in-process client of the remote CLI is `LocalServer`, which pings its connection and so is never reaped. Parked as a `NOTE:` at the assertion in `AnAbandonedTransactionIsRolledBackAndTheCatalogWriteLockReleased` that records it, which is where a future reader meets it; the note says that the ladder rung, if it ever becomes reachable, is a representation change across the family rather than a check at one site.
- **The disposal ordering `DataphorServiceHost.Stop` relies on is not enforced anywhere.** Agreed, and left as it is: the stopped-server guard in the sweep is the invariant that makes the other order safe, so the ordering is an optimization rather than a correctness requirement, and `docs/migration-notes.md` tells a host that builds its own service to keep it. The review's second fix closes the remaining hole in that guard.
- **No operator setting for the idle threshold.** The decision is sound and its `NOTE:` states a revisit condition a reader can act on, naming the step (an additive `[DefaultValue]` property on `ServerConfiguration` with a floor above the client's keep-alive interval, plus the user-guide section and a migration note) and the trigger (a deployment reporting schema changes blocked for minutes by vanished clients). The floor argument — the shipped client's 120-second interval is compiled into the client, so a lower threshold would kill live sessions — is what makes a setting a trap rather than a convenience. Left alone.
- **`RemoteServer.Stop()` closes connections and stops the engine, but `DataphorService.Stop()` stops the engine without closing connections**, and nothing calls `RemoteServer.Stop`. Noticed while enumerating the concurrent paths; outside this ticket (it is about what a `StopServer` call over the wire leaves behind, not about reclaiming an abandoned client) and no caller makes it reachable today.

**Tripwires recorded.** No new ones beyond the two `NOTE:`s named above (the dead `AttachServer`, and the dead-CLI-object exception type in the test). The implement stage's three stand: the revisit condition at `ConnectionManager.IdleTimeInSeconds`; the serial sweep at `RelinquishIdleConnections`, where one wedged process delays every later connection's reclamation by up to half of `Engine.ProcessTerminationTimeout`; and the amended note in `ConnectionKeepAliveTest`.

**Nothing filed.** No finding reached the bar: the two code defects were small and local enough to fix here, the two documentation errors likewise, and the four weighed items above are either unreachable, already covered by an invariant, or a decision with a recorded revisit condition.

## Out of scope, as the ticket set it

- **Native CLI sessions are reclaimed by nothing.** Confirmed by reading `NativeServer`: sessions in a dictionary keyed by a GUID, no liveness tracking, no sweep, no ping in `INativeCLIService`. A native client that dies inside a catalog-changing transaction holds the store's write lock until the server is stopped. Pre-existing and owned by `bug-native-cli-sessions-never-reclaimed`, in `plan/`; named in both documents so that a reader of "what reclaims an abandoned session" knows which door is still open.
- **A client process killed outright** belongs to `test-client-server`, whose ticket already records the split and the fact that `ConnectionManager` takes the threshold as a constructor argument since this change.

## Build and tests

`dotnet build Dataphor.slnx` and `-c Release` after the review's changes: 0 errors, 143 warnings each, none in a touched file. The suite in the three parts from the root `AGENTS.md`, each in the foreground:

- part 1 (everything but the coverage scripts): 13 passed (`Common.Tests`) and 433 passed (`DAE.Server.Tests`), 0 failed, 0 skipped, 4 m 56 s.
- part 2 (coverage scripts A–R): 67 passed, 11 skipped, 0 failed, 3 m 20 s.
- part 3 (the rest): 48 passed, 14 skipped, 0 failed, 3 m 35 s.

Every skip is a manifest `known-failure`, `fragment` or `unstable` entry; no new failure, and nothing was skipped or loosened. No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.
