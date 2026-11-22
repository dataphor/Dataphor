description: The server has no way to tell whether a program connected through its simplified call interface is still there, and the client library never says so. Give the server a per-session clock that every call moves, and make the client library say "still here" on a session it is holding, so that a later change can safely clean up the ones nobody is using.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/NativeCLI/NativeServer.cs, src/engine/Server/NativeCLI/NativeSession.cs, src/protocol/NativeCLI/NativeCLISession.cs, src/engine/Server.Hosting/DataphorServiceHost.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, tests/DAE.Server.Tests/NativeSessionKeepAliveTest.cs, docs/migration-notes.md
difficulty: medium
----
## Why this exists

`bug-native-cli-sessions-never-reclaimed` is the defect: a program that connects through the native call interface, starts a transaction and then dies leaves that transaction open in the server for as long as the server runs, blocking every other session's schema change. That ticket adds the idle watcher that cleans such a session up. This one lays the two pieces the watcher needs, and that can be built and tested before anything is reclaimed:

- a **clock per native session** in the server, which every operation on that session moves, and which says whether a call is in flight right now;
- a **keep-alive ping** in the client library (`NativeCLISession`), so that a session a live client is holding but not currently using keeps its clock moving and is not mistaken for an abandoned one.

Both are inert on their own: nothing reads the clock yet and nothing acts on it. Landing them first means the watcher can be added without an interval in which a legitimately idle session would be thrown away — in particular the session a *server link* holds on another Dataphor server for the life of a user session, which can be idle for hours.

Read `src/engine/Server/Service/ConnectionManager.cs` and `src/engine/Server/Server/RemoteServerConnection.cs` first. The main client interface has exactly this pair of mechanisms — a last-ping time per connection, and `LocalServer`'s keep-alive thread — and the names, comments and shapes here should read like theirs, so that whoever next maintains either finds the same design twice rather than two inventions. Where this one differs, the differences are deliberate and are called out below.

## What the server side is

`NativeServer` (`src/engine/Server/NativeCLI/NativeServer.cs`) holds its sessions in a `Dictionary<Guid, NativeSession>`, and every operation that takes a `NativeSessionHandle` reaches its session through the single private method `GetNativeSession`. That is the one place a clock has to be stamped, which is why no change to the service contract `INativeCLIService` is needed — and none is permitted here.

Replace `GetNativeSession` with an acquire/release pair:

- `AcquireNativeSession(NativeSessionHandle)` — under the `_nativeSessions` lock: look the session up, raise the existing "Invalid session handle" `ArgumentException` if it is not there, otherwise increment its in-use count, stamp its clock, and return it.
- `ReleaseNativeSession(NativeSession)` — under the same lock: decrement the in-use count and stamp the clock again.

Every public method of `NativeServer` that takes a handle (`BeginTransaction`, `PrepareTransaction`, `CommitTransaction`, `RollbackTransaction`, `GetTransactionCount`, and both `Execute` overloads) then becomes an acquire, the work in a `try`, and the release in a `finally`, inside the existing `try`/`catch (Exception) { throw NativeCLIUtility.WrapException(...); }` wrapper. Stamping on exit as well as entry is the point: a call that runs for an hour would otherwise leave an hour-old stamp behind it and look abandoned the moment it finished.

`StopSession` keeps going through `RemoveNativeSession`, which already removes under the lock. The two `Execute(NativeSessionInfo, ...)` overloads create a session, use it and stop it inside the one call and never put it in the dictionary, so they need no accounting; say so in a comment rather than leaving a reader to work it out.

### The clock is monotonic, not wall-clock

Store the stamp as `Environment.TickCount64` — milliseconds since the machine started — not as a `DateTime`. `RemoteServerConnection.LastActivityTime` uses `DateTime.Now`, and that is a defect: when the local clock moves, whether by a daylight-saving spring-forward, an operator setting the time, or a large time-synchronization step, every stamp suddenly looks an hour or more old and live connections are all reclaimed in one sweep. It is filed as `bug-idle-clocks-use-wall-clock-time` against the existing mechanism; do not repeat it here. A comment at the new field should say which basis it uses and why it differs from the connection's.

### What `NativeSession` carries

In `src/engine/Server/NativeCLI/NativeSession.cs`, add to `NativeSession`:

- the last-activity tick, set in the constructor, so that a brand-new session is never already idle;
- an in-use count — an `int`, only ever touched under `NativeServer`'s `_nativeSessions` lock. Do not reach for `Interlocked`: the watcher in the next ticket has to read the count and the dictionary together, atomically, to decide.
- a read of how long the session has been without a call, in seconds, which reports 0 while the in-use count is above zero.

### The one public read

`bug-native-cli-sessions-never-reclaimed`'s watcher lives in the same assembly and could use internals, but the test here cannot: this repository has no `InternalsVisibleTo` convention — see the comment at `src/hosts/Host/DataphorHost.cs:175` for the precedent of a public member justified that way. So add one public read to `NativeServer`:

```csharp
/// <summary>How long each session this server holds has been without a call, in seconds; a session with a call in flight reports 0.</summary>
public long[] GetSessionIdleSeconds()
```

and one public accessor to `DataphorServiceHost`, beside the existing `Server` one:

```csharp
public NativeServer NativeServer { get { return _nativeServer; } }
```

Each carries a remark saying what reads it and why it is public. Nothing else of the session table becomes public.

### The unlocked dictionary read, fixed on the way past

`GetNativeSession` today calls `_nativeSessions.TryGetValue` with **no lock**, while `AddNativeSession` and `RemoveNativeSession` mutate the same dictionary under one. A `Dictionary<,>` read concurrent with a write can return a wrong value or throw, and the service runs under `ConcurrencyMode.Multiple`, so two clients' calls genuinely race. The acquire/release pair above puts every access under the one lock and so fixes it by construction. There is no test for it — a data race has no deterministic reproduction — so verify by inspection that no `_nativeSessions` access is left outside `lock (_nativeSessions)`, and list the fix in the handoff.

## What the client side is

`NativeCLISession` (`src/protocol/NativeCLI/NativeCLISession.cs`) is the client library's handle on a native session, and it is what a server link uses: `RemoteSessionImplementation` builds one per remote session and holds it for the life of the local `ServerProcess`. Give it a keep-alive, shaped after `LocalServer`'s — the last `#region` of `src/protocol/Client/Server/LocalServer.cs`.

- A `public const int DefaultKeepAliveInterval = 120;`, the same two minutes and for the same reason as the client's, plus an additive constructor overload taking the interval in seconds. **0 means no keep-alive**, which is what a test watching a reclamation needs; a negative value, or one large enough to overflow the millisecond wait, is refused as `LocalServer`'s is. The existing seven-argument constructor keeps its signature and uses the default, so `RemoteSessionImplementation` needs no change but the comment correction below.
- One dedicated background thread per session, signalled by a `ManualResetEvent` on `Dispose`, exactly as `LocalServer.StartKeepAlive`, `KeepAlive` and `StopKeepAlive` do. **Not** a `System.Threading.Timer`: the binding sets `SendTimeout = TimeSpan.MaxValue` (`src/protocol/Common/Contracts/DataphorServiceUtility.cs`), so a ping to a server that has stopped answering without closing the socket never returns, and on the thread pool that would hold a pool thread for the life of the process — one per hung session. Record at the site that the cost of this choice is one thread per session per server link, where the main client's keep-alive is one thread per client process, and that the step if that ever matters is a single shared keep-alive thread with a bounded wait per ping, not the pool.
- **The ping is `GetTransactionCount(_sessionHandle)`.** It is already on the contract, takes nothing but the handle, has no side effect — it reads `ServerProcess.TransactionCount`, the length of a list — and reaches the server through the same acquire path every other operation does, so it moves the clock with no server-side special case and no contract change. Say that at the site; a reader who does not know why will otherwise take it for a leftover.
- **A ping is skipped, not queued, when the session is busy.** Guard every operation of `NativeCLISession` with a lock, and have the keep-alive thread take it with `Monitor.TryEnter` and no wait: if the owner is inside a call, the server already counts the session as in use, so the ping has nothing to add and must not sit behind a long statement. The lock is also what keeps this class's one new concurrent caller off the unsynchronized lazy channel field in `ServiceClient<T>.GetInterface`; `bug-wcf-client-channel-field-is-unsynchronized` owns that pre-existing race, and nothing here depends on it being fixed. Note the one cost: an application call can now wait for a ping in flight, which on a network that has stopped delivering means waiting about as long as its own call would have waited anyway.
- **A failed ping is swallowed and the next one is sent on schedule**, for the reasons written out at `LocalServer.SendKeepAlive`. That includes a ping that comes back "Invalid session handle" because the far server has already reclaimed the session: the client learns that from its next real call, which fails with the same error, and discovering it on a background thread gives it nowhere to report it to. Nothing is logged, as there.
- `Dispose` stops the keep-alive **before** it takes the lock and calls `StopSession`, and the ping checks the handle under the lock, so that no ping can follow the session's end.

Also correct the comments that now say something untrue. `RemoteSessionImplementation.Dispose`'s remark says the far session is "the far server's own to reclaim when its connection goes idle" — which is true of the main interface and, until `bug-native-cli-sessions-never-reclaimed` lands, false of this one. State what is actually so: the far session stays on that server until that server reclaims it, and reclamation is what the next ticket adds.

## Edge cases & interactions

- **A call that runs longer than any threshold.** Stamped on exit as well as entry, and in use throughout. Correct by the acquire/release shape; the next ticket's test pins it.
- **Two clients using one session handle at once.** Both acquire, the in-use count reaches 2 and returns to 0. Correct by construction; what actually orders their work is the engine's own per-process serialization. By inspection.
- **`StopSession` arriving while a call is in flight on the same session.** Pre-existing: the remove succeeds and the in-flight call goes on against a session the dictionary no longer holds. Not made worse here and not fixed here; say so in a comment at `RemoveNativeSession`, so that the next reader does not take the new accounting to cover it.
- **The stateless `Execute(NativeSessionInfo, ...)` overloads.** No dictionary entry, no accounting, no clock. By inspection.
- **`Environment.TickCount64` on a machine that has been up a long time.** A signed 64-bit millisecond count does not wrap within any machine's lifetime, unlike the 32-bit `TickCount`. No guard needed; one comment saying so.
- **A client that creates many sessions through the shipped `DAE.NativeCLI` assembly.** One keep-alive thread each. Named in the migration note, with the revisit condition recorded at the site.
- **A 4.0 client pinging a 3.1 server.** `GetTransactionCount` is in the 3.1 contract, so the ping is answered. The reverse — a 3.1 client against a 4.0 server, which does not ping at all — is why the next ticket's threshold has to be generous, and is a migration note there rather than here.
- **A server link whose far server has gone.** The ping fails every interval until the local process is torn down, as the main client's does. No new behaviour on the local side; `bug-dead-remote-session-blocks-every-statement` owns what the local session sees.

## Tests

One new fixture, `tests/DAE.Server.Tests/NativeSessionKeepAliveTest.cs`, modelled on `ConnectionKeepAliveTest` — which observes the same mechanism on the main interface by reading the server's own clock through a public property, and whose fixture remarks are the model for this one's. It must not be `[Parallelizable]`, for the reason the sibling fixtures give.

- `TheSessionsClockMovesWhileTheClientHoldsIt` — a `HostedInstance` (`tests/DAE.Server.Tests/HostedInstance.cs`) serving a real endpoint on loopback, a `NativeCLISession` connected to it with a one-second keep-alive interval, no call made on it by the test, and then a poll of `instance.Host.NativeServer.GetSessionIdleSeconds()` waiting for its single entry to fall back toward zero. That is the ping arriving, measured where the next ticket's watcher will read it. Print the measurement with `TestContext.Progress.WriteLine`, as the sibling fixtures do.

Do not add a case asserting that the in-use count or the clock field is set: that is testing an accessor. Everything else above is verified by inspection, and the reclamation behaviour belongs to `bug-native-cli-sessions-never-reclaimed`, which carries the cases for it.

## Documentation

One entry in the *Custom C# extensions* table of `docs/migration-notes.md`, beside the two `ConnectionManager` and `LocalServer` entries already there: `NativeCLISession` now pings the far server every two minutes on a background thread for as long as it is held, the ping is `GetTransactionCount`, there is a new constructor overload taking the interval in seconds (0 to turn it off), its operations are serialized against each other by a lock, and `DataphorServiceHost` and `NativeServer` have the two new public reads. Under *What to do*: nothing for an application; a host embedding the native client against a server whose reclamation clock is not the default can set the interval, and code that called `NativeCLISession` operations concurrently from several threads now finds them serialized.

Nothing in `docs/modernization-plan.md` yet. Its paragraph under WS2 beginning "The door still open: native CLI sessions are reclaimed by nothing" is still true until the watcher lands, and `bug-native-cli-sessions-never-reclaimed` rewrites it.

## TODO

- Add the last-activity tick, the in-use count and the idle-seconds read to `NativeSession`, with the comment on the monotonic basis.
- Replace `GetNativeSession` with `AcquireNativeSession` and `ReleaseNativeSession`, both wholly under `lock (_nativeSessions)`, and convert the seven handle-taking methods of `NativeServer` to acquire, try, finally-release.
- Add `NativeServer.GetSessionIdleSeconds()` and `DataphorServiceHost.NativeServer`, each with a remark saying why it is public.
- Comment the stateless `Execute` overloads and `RemoveNativeSession` as described.
- Add the keep-alive to `NativeCLISession`: the constant, the additive constructor overload, the background thread, the `Monitor.TryEnter` ping on `GetTransactionCount`, the lock around every operation, and the `Dispose` ordering.
- Correct `RemoteSessionImplementation.Dispose`'s remark about who reclaims the far session.
- Write `NativeSessionKeepAliveTest.TheSessionsClockMovesWhileTheClientHoldsIt`.
- Add the `docs/migration-notes.md` entry.
- `dotnet build Dataphor.slnx`, then the targeted run (`dotnet test tests/DAE.Server.Tests --filter Name=TheSessionsClockMovesWhileTheClientHoldsIt --logger "console;verbosity=normal"`), then the three suite parts from the root `AGENTS.md`, each as its own foreground command.
