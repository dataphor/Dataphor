description: The server now keeps a clock per session for programs that connect through its simplified call interface, and the client library says "still here" on a session it is holding, so that a later change can safely clean up the ones nobody is using.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/NativeCLI/NativeServer.cs, src/engine/Server/NativeCLI/NativeSession.cs, src/protocol/NativeCLI/NativeCLISession.cs, src/engine/Server.Hosting/DataphorServiceHost.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, tests/DAE.Server.Tests/NativeSessionKeepAliveTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
difficulty: medium
----
## What landed

Two inert mechanisms, both of which `bug-native-cli-sessions-never-reclaimed` needs before its idle watcher can be added safely. Nothing reads the clock yet and nothing is reclaimed; what changes observably is that the client library now sends a periodic call on a session it holds, and that the operations of one `NativeCLISession` are serialized against each other.

### Server: a clock per native session

`NativeSession` (`src/engine/Server/NativeCLI/NativeSession.cs`) gained a *Lifetime Management* region: a last-activity stamp (`Environment.TickCount64`, stamped in the constructor so a new session is never already idle), an in-use count, `EnterCall()`/`ExitCall()`, and `IdleSeconds`, which reports 0 while the count is above zero. All three are documented as touched only under `NativeServer`'s session lock, and the field's remark says why the basis is monotonic rather than `DateTime.Now` — `bug-idle-clocks-use-wall-clock-time` is the same defect on the connection clock and is not repeated here.

`NativeServer` (`src/engine/Server/NativeCLI/NativeServer.cs`): `GetNativeSession` is gone, replaced by `AcquireNativeSession` (look up, raise the same "Invalid session handle" `ArgumentException`, count the call, stamp) and `ReleaseNativeSession` (uncount, stamp again), both wholly inside `lock (_nativeSessions)`. The seven handle-taking operations — `BeginTransaction`, `PrepareTransaction`, `CommitTransaction`, `RollbackTransaction`, `GetTransactionCount` and both `Execute` overloads — are now acquire / work in a `try` / release in a `finally`, inside the existing `WrapException` wrapper. One public read was added, `GetSessionIdleSeconds()`, and one public accessor on `DataphorServiceHost`, `NativeServer`; each carries a remark saying what reads it and that the repository has no `InternalsVisibleTo` convention to use instead.

**A pre-existing data race is fixed on the way past, with no test.** `GetNativeSession` read `_nativeSessions` with no lock while `AddNativeSession` and `RemoveNativeSession` wrote it under one, and the service runs under `ConcurrencyMode.Multiple`. The acquire/release shape puts every access under the one lock. Verified by inspection only — a data race has no deterministic reproduction: `grep -n "_nativeSessions" src/engine/Server/NativeCLI/NativeServer.cs` returns twelve lines, every one of them the `lock` statement itself or a line inside such a block.

### Client: a keep-alive on `NativeCLISession`

`NativeCLISession` (`src/protocol/NativeCLI/NativeCLISession.cs`) gained a *Keep Alive* region shaped after `LocalServer`'s: `DefaultKeepAliveInterval = 120`, an additive constructor overload taking the interval in seconds (0 means no keep-alive; negative or large enough to overflow the millisecond wait is refused), one dedicated background thread per session signalled by a `ManualResetEvent` on `Dispose`, and the ping itself. The ping is `GetTransactionCount` on the session's own handle — already on the contract, no side effect, and it reaches the server by the same acquire path every other operation does, so no contract change and no server-side special case. The existing seven-argument constructor keeps its signature, so `RemoteSessionImplementation` needed no change but a comment correction.

Every operation of the class is now inside `lock (_syncHandle)`, and the keep-alive thread takes that lock with `Monitor.TryEnter` and no wait, so a ping is skipped rather than queued while the owner is in a call. A failed ping is swallowed and the next sent on schedule. `Dispose` signals the keep-alive before it takes the lock, and the ping re-checks the handle under the lock, so no ping can follow the end of the session.

`RemoteSessionImplementation.Dispose`'s remark no longer says the far session is "the far server's own to reclaim when its connection goes idle", which was untrue of this interface; it now says the session stays on that server until that server reclaims it, that as of this version nothing does, and which ticket adds the watcher.

## How to exercise it

Targeted run:

```
dotnet test tests/DAE.Server.Tests --filter Name=TheSessionsClockMovesWhileTheClientHoldsIt --logger "console;verbosity=normal"
```

By hand, against a running instance: build a `NativeCLISession` with a short interval, make no call on it, and poll `DataphorServiceHost.NativeServer.GetSessionIdleSeconds()` — the single entry climbs to just under the interval and drops back each time a ping lands. With the interval set to 0 it climbs without bound, which is the arrangement `bug-native-cli-sessions-never-reclaimed`'s reclamation cases will need.

Usage worth a second look during review:

- A **server link** is the real caller: `RemoteSessionImplementation` builds one `NativeCLISession` per remote session and holds it for the life of the local `ServerProcess`, which can be idle for hours. That session now keeps its clock moving, which is the whole point of landing this before the watcher. `ServerLinkConnectionTest` and `ServerLinkUserRollbackTest` cover the link itself and pass.
- A host embedding `DAE.NativeCLI` directly: nothing changes unless it wants another interval, and it then pays one background thread per session it holds.
- A caller that drove one `NativeCLISession` from several threads now finds its operations serialized. Recorded in `docs/migration-notes.md`.

## Tests

| Test | What it verifies |
|---|---|
| `tests/DAE.Server.Tests/NativeSessionKeepAliveTest.TheSessionsClockMovesWhileTheClientHoldsIt` | That a session a live client holds but never calls is kept young: a hosted server on loopback, a `NativeCLISession` with a three-second keep-alive, no call from the test, then the server's own clock (`instance.Host.NativeServer.GetSessionIdleSeconds()`) is watched aging to 2 s and then falling back below it. The observed run: aged to 2 s after 2.0 s, back to 0 s 1.1 s later. |

The interval is three seconds rather than one because idle time is reported in whole seconds: at one second the clock would barely be seen to age, so a ping arriving would prove nothing. The fixture is not `[Parallelizable]`, for the reason its siblings give.

No other test was added, deliberately. Asserting that the in-use count or the stamp is set is testing an accessor; the reclamation behaviour belongs to `bug-native-cli-sessions-never-reclaimed`, which carries the cases for it.

## Known gaps — verified by inspection only

Flagged rather than papered over; a reviewer should decide whether any of these is worth a case or a ticket.

- **The unlocked dictionary read.** Fixed by construction, no test, as above.
- **Two clients using one session handle at once.** Both acquire, the in-use count reaches 2 and returns to 0. What actually orders their work is the engine's per-process serialization, not this count.
- **`StopSession` arriving while a call is in flight on the same session.** Pre-existing: the remove succeeds and the in-flight call runs on against a session the table no longer holds. Not made worse, not fixed; a `NOTE:` at `RemoveNativeSession` says so, so the new accounting is not read as covering it.
- **The stateless `Execute(NativeSessionInfo, …)` overloads.** No dictionary entry, no accounting, no clock; the overload's new remark says why.
- **The client's ping-skip, failed-ping and `Dispose`-ordering paths have no test.** The main interface's equivalents are tested (`ConnectionKeepAliveTest.AFailedPingDoesNotEndTheKeepAlive`) through a decorator around `IRemoteServer`; there is no equally cheap seam here, because the ping goes through a WCF client rather than an interface the test can wrap, and faulting a real channel needs the hosted server. If the reviewer wants one, the cheapest shape is probably a case that disposes the session while a slow statement is running and asserts no error escapes.
- **Argument validation on the new constructor overload** (0, negative, over `Int32.MaxValue / 1000`) is unasserted; it is three lines of bounds check that the type system does not cover but that no behaviour depends on.

## Tripwires parked in code

Index only — the reasoning is at each site.

- `src/protocol/NativeCLI/NativeCLISession.cs`, at the *Keep Alive* region comment: one keep-alive thread per session, where the main client's is one per client process; a `System.Threading.Timer` must not be used (`SendTimeout = TimeSpan.MaxValue`, so a ping to a hung server never returns and would hold a pool thread for the life of the process); the step if the thread count ever matters is one shared keep-alive thread with a bounded wait per ping.
- `src/protocol/NativeCLI/NativeCLISession.cs`, at `_syncHandle`: an application call can now wait for a ping in flight; on a network that has stopped delivering, that is about as long as the call would itself have waited.
- `src/engine/Server/NativeCLI/NativeServer.cs`, at `RemoveNativeSession`: the stop-during-a-call case above.

## Validation

`dotnet build Dataphor.slnx` clean (0 errors; warning count unchanged from before the change). All three suite parts from the root `AGENTS.md` run and pass:

- part 1 — 668 passed, 0 failed, 33 skipped (6 m 27 s), plus Common.Tests 13 passed
- part 2 — 67 passed, 0 failed, 11 skipped (3 m 2 s)
- part 3 — 50 passed, 0 failed, 12 skipped (3 m 3 s)

No pre-existing failures surfaced, so nothing was written to `tickets/.pre-existing-error.md`.

## Documentation

One entry in the *Custom C# extensions* table of `docs/migration-notes.md`, beside the `LocalServer` and `ConnectionManager` entries: the ping and what it is, the new constructor overload and its 0 case, the serialization of the class's operations, the two new public reads, and the one-thread-per-session cost. Nothing in `docs/modernization-plan.md` — the paragraph under WS2 beginning "The door still open: native CLI sessions are reclaimed by nothing" is still true until the watcher lands, and `bug-native-cli-sessions-never-reclaimed` rewrites it.
