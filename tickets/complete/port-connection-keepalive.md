description: A client program used to tell the server "I am still here" only while it had a session open, so a connection it held between sessions could be thrown away by the server while the program was still using it; the client now keeps saying it for as long as it holds the connection.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/protocol/Client/Server/LocalServer.cs, src/protocol/Client/Server/LocalSession.cs, tests/DAE.Server.Tests/ConnectionKeepAliveTest.cs, tests/DAE.Server.Tests/TestEnvironment.cs, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, tickets/implement/14.65-port-abandoned-session-cleanup.md
----
## What landed

The server decides whether a client is still there by watching one clock per **connection** (`RemoteServerConnection.LastActivityTime`), and only the client calling `Ping()` moves that clock. The client used to ping from `LocalSession`, one thread per **session**, so a connection held with no session open was pinged by nothing and the server reclaimed it after five minutes. The keep-alive now belongs to `LocalServer`, the object that owns the connection for its whole life.

The implement stage is commit `ticket(implement): port-connection-keepalive`. It moved the ping; the review changed five things in that work. Both are described together below, with the review's own changes marked.

**`src/protocol/Client/Server/LocalServer.cs`**

- A `#region Keep Alive` at the bottom of the class holds `DefaultKeepAliveInterval` (120 seconds, the old `LocalSession.LocalKeepAliveInterval` renamed), the interval field, and `StartKeepAlive`/`KeepAlive`/`StopKeepAlive` moved across from `LocalSession`. The strategy comment moved with them, corrected: the 120 seconds is the client's interval, the five minutes is the server's threshold (`ConnectionManager.IdleTimeInSeconds`), and the only relation between them is that the server's has to be the larger.
- The keep-alive thread is a **background** thread. It was a foreground thread, so an undisposed client held the process open after `Main` returned; the server's own idle watcher was made a background thread for the same reason, and the comment points at it.
- `StartKeepAlive()` is the last statement of the constructor, after `Establish`; `StopKeepAlive()` is the first thing `Dispose` does, before the connection is released.
- A new public constructor overload `LocalServer(IRemoteServer, bool, string, int keepAliveIntervalSeconds)`; the three-argument constructor delegates to it with the default.
- **Review: a ping that fails no longer ends the keep-alive.** The ping moved into a named `SendKeepAlive`, which swallows the failure so that the loop waits and pings again. See *Review findings*.
- **Review: the interval is checked against both bounds.** The constructor rejected an interval below one second and accepted anything above. The wait is expressed in milliseconds, so a large interval overflowed to a negative time-out, which throws, which killed the keep-alive thread with no connection pinged and nothing reported. The accepted range is now 1 to 2,147,483 seconds.
- **Review: the loop's `ManualResetEvent` is disposed in a `finally`**, so it is released on every exit from the thread and not only on the ordinary one.
- **Review: `Dispose` delegates to a new `RelinquishConnection`.** Releasing the connection is wrapped in a catch-and-ignore, because a server that has already reclaimed the connection must not stop a client from disconnecting. Extracting it removed two levels of nesting from `Dispose` and a `try`/`finally` that nothing could reach, and the reason for ignoring the failure now reads as the method's remarks rather than as a six-line comment inside five nested blocks.

**`src/protocol/Client/Server/LocalSession.cs`** lost `StartKeepAlive`, `KeepAlive`, `StopKeepAlive`, `_keepAliveSignal`, `_keepAliveReferenceLock`, `LocalKeepAliveInterval` and the two calls; a two-line comment points at `LocalServer`, and its `Dispose` loses a level of nesting. **Review: the `using System.Threading;` that this change orphaned is gone**; two other `using` lines in that file were already unused before the change and are left alone.

**`docs/migration-notes.md`** has one row under *Custom C# extensions* covering every visible change: the ping is per connection, the constant moved to `LocalServer.DefaultKeepAliveInterval`, the new overload, the background thread, and — **added by review** — that a failed ping no longer ends the keep-alive.

**Test-side refactor.** `WithTheObjectIDCounterRestored` moved from `CoreWcfProtocolTest` to `TestEnvironment` (public static) so that both fixtures share one copy, including the use that was written out inline in `ProtocolClient`'s constructor. No behaviour change.

## The tests

`tests/DAE.Server.Tests/ConnectionKeepAliveTest.cs`, one fixture of three cases, at the lowest layer that shows the behaviour: a `LocalServer` over an in-process `RemoteServer` over a `TestServerInstance`. No WCF host, no port, no network. Not `[Parallelizable]`, like every other fixture here that starts a second engine in the process. The client is built through the new overload with a one-second interval, with the object-id counter handed back around its construction.

| Test | What it verifies |
|---|---|
| `TheConnectionIsPingedWithNoSessionOpen` | The server's `LastActivityTime` advances on a connection where no session was ever opened, and advances again after a session has been opened and disconnected. These are the two windows a program holding a `LocalServer` across idle time sits in. |
| `AFailedPingDoesNotEndTheKeepAlive` (**added by review**) | The clock still advances after a ping has thrown. The failure is injected by a decorator over the real server whose connection refuses its first ping with the exception `ClientConnection.Ping` raises for a communication failure; the case also asserts that a ping did fail, so that it cannot pass by never being tested. |
| `DisposingAfterTheServerHasReclaimedTheConnectionDoesNotThrow` | `LocalServer.Dispose` completes after the connection has been relinquished behind its back, which is the state the mechanism itself produces when the pings stop. No timing involved. |

Each case was observed failing before its fix. The first two were checked against the pre-change behaviour with the relevant line neutralised by a scratch edit, reverted afterwards with no `git` operation: with `StartKeepAlive()` removed the first case failed with `LastActivityTime` unmoved after the full 20-second wait, and with `SendKeepAlive`'s catch rethrowing — the behaviour this code had through 3.1 — `AFailedPingDoesNotEndTheKeepAlive` failed the same way. The third failed before the `catch` around the release was added, with the exception out of the second relinquish.

## Review findings

The implement-stage diff was read before its handoff. Every category below, including the empty ones.

### Fixed in this pass

**A ping that failed ended the keep-alive for good — the change's own objective, defeated by its first network blip.** `KeepAlive`'s only exception handler was outside its loop, so one throw from `Ping()` ended the thread and the connection was never pinged again; five minutes later the server reclaimed it, which is precisely the loss the ping exists to prevent. The comment at the site claimed this was right ("a client whose pings do not reach the server has no connection left to keep alive"), and the client is built the other way round: `ClientServer` resets its channel on a communication failure and the next call builds a new one, by design, so that a client survives a blip and keeps using its handles — the reasoning `port-abandoned-session-cleanup` sets out at length. Through 3.1 the ping was per session, so the thread that died was one session's and the next session started another: the client recovered by accident. Consolidating to one thread per connection, which is the whole point of this ticket, removed the accident and left nothing in its place. Fixed by swallowing a failed ping and pinging again at the next interval, with the reproduction above.

That fix's cost is recorded as a `NOTE:` at the site: a connection the server has genuinely dropped is now pinged fruitlessly once per interval until the client disposes the object. That is one failing round trip every two minutes on a background thread, and it was weighed against classifying each failure by exception type across the two transports this class runs over — an in-process `RemoteServer` and `ClientConnection` over WCF — and ending the keep-alive on a wrong guess. Nothing is logged, also deliberately and also recorded at the site with its revisit condition: a server that has gone away would fill the client's log at one entry per interval for as long as the process runs, so the thing to do if a deployment ever needs to see why its connections died is to log the first failure of a run and no others.

**The new interval parameter was checked at one end only.** Below one second it threw; above 2,147,483 seconds the conversion to milliseconds overflowed to a negative time-out, `WaitOne` threw, and the thread died before the first ping — the same silent outcome as the defect above, from a public constructor argument. Both bounds are checked now. No test: it is one guard with no branching behaviour, and the compiler cannot express the range.

**The keep-alive's `ManualResetEvent` was disposed only on the ordinary exit** from the loop, so any other exit left it to its finaliser. Moved into a `finally` with a null check. No test: resource cleanup on a path a test can only reach through the fault injection the case above already does for a different purpose.

**`Dispose` had grown to five levels of nested `try`/`finally` around a six-line comment,** one level of which could not be reached — the `catch` inside it already swallowed everything the `try` could raise. Extracted as `RelinquishConnection`, whose remarks carry the reason. Behaviour unchanged; the third test covers it.

**A comment stated something untrue about the class.** "Nothing is logged; this class has no log of its own here" — `LocalServer` logs through `_internalServer.LogError` twice already, once in exactly this shape (swallow an `IOException`, record it). The replacement says that logging was considered and why it was declined.

**`LocalSession.cs` kept a `using System.Threading;` that nothing in the file uses any more.** Removed. The handoff asked whether to remove the two that were already unused before this change; left, because a file's unrelated dead `using` lines are not this ticket's to churn.

### Filed as tickets

None. The one defect found is fixed above, and the only other thing worth a ticket — that reclaiming a connection twice raises a `NullReferenceException` out of `RemoteServerConnection.Dispose`, whose `_disposed` field is written and never read — is already owned, in detail, by `port-abandoned-session-cleanup` in `implement/`: it names both `RemoteServerConnection.cs` and `RemoteServer.cs` in its `files:`, states the defect as one of its four established facts, and owes a `RelinquishingAConnectionTwiceDoesNothing` test for it. Nothing to add there. The site-claim grep over the five working stages found that ticket, `test-client-server` and `bug-object-id-counter-is-process-wide` on these paths, and no other claim.

### Recorded as tripwires

- **The third test's reach shrinks when the server's half becomes idempotent.** `DisposingAfterTheServerHasReclaimedTheConnectionDoesNotThrow` only reaches the client's catch-and-ignore because `RemoteServer.Relinquish` currently throws on a second release. Once `port-abandoned-session-cleanup` fixes that, the in-process path here stops exercising the guard and only the wired path — where a second release comes back as the service's unknown-object-handle fault — still needs it. Recorded in that test's remarks, where whoever next reads a green run will meet it.
- **A failed ping is not logged, and what to do if that ever matters.** In `SendKeepAlive`'s remarks, with its revisit condition, as described above.

### Carried to another ticket

**`port-abandoned-session-cleanup` would have tripped over this change.** Its test plan offers two routes for the step that must leave a connection unreclaimed, one of them "build A through a `LocalServer` over the same `RemoteServer`". That route now pings, and the connection is never reclaimed, so the test would wait out its bound and fail for a reason the ticket does not predict. One paragraph added to it at that spot: take the remote CLI route, or pass a keep-alive interval longer than the test will run. Nothing else in that ticket needed changing — it already reads as if this work had landed, and its statement that the shipped client's interval is a compiled-in 120 seconds remains true of deployments, since `DAE.Client.ServerConnection` uses the default.

### Checked and found nothing

- **Compatibility surfaces.** Nothing here touches D4 semantics, operator signatures, registered class names, assembly names, file formats, error codes, WCF contract shapes or ports. The ping operation on the wire (`PingConnection`) is untouched; only who calls it, and how often, changed. `ContractParityTest` passes.
- **Documentation.** Every file the change touches was read, and the ones it should have touched were looked for. `docs/migration-notes.md` is the only document that owed an entry; neither `docs/modernization-plan.md` nor `docs/architecture.md` mentions the keep-alive, and the plan's WS2 "Remoting leftovers" note that would describe it is explicitly `port-abandoned-session-cleanup`'s to rewrite, with the whole settled mechanism in one place. Leaving it alone avoids two half-descriptions of the same thing.
- **Reference-counted sharing.** `AddReference`/`RemoveReference` drive `Dispose` at the last release, so the keep-alive starts once and stops once however many references a host takes. The only caller of `RemoveReference` is `ServerFactory`, which is commented out in its entirety — the handoff's claim that sharing "is unaffected" is true, but it describes dead code.
- **Disposing twice**, reachable through reference counting and through `Dispose()` being public beside the protected override, is a no-op for the keep-alive: `StopKeepAlive` either sets an already-set signal or finds a null one, all under the lock. Verified by inspection; no test, because the base class has no re-entry guard at all and pinning half of that behaviour here would mislead.
- **A ping landing during `Dispose`.** `StopKeepAlive` signals and does not join, so a ping already past its wait can still run while the connection is being released. It reads `_serverConnection` once into a local (added by review, so that the field being cleared on the other thread is handled rather than merely caught), and anything it raises is swallowed. A join was considered and rejected: it would make `Dispose` wait up to a full interval on a client whose channel is wedged. The comment at the site says this rather than claiming a guarantee the code does not give.
- **Many sessions over one connection now share one ping.** By inspection: the keep-alive starts in the constructor and nothing about it reads a session.
- **The wire path.** The ping reaches the server through `ClientConnection.Ping` exactly as it did before, so this change does not alter it; the out-of-process client/server coverage that would exercise it end to end is `test-client-server`'s, which already lists `LocalServer.cs` among its files.
- **Source hygiene.** `LocalServer.cs` is 744 lines, in range for this codebase and not worth splitting for 40 added lines. Tabs, CRLF and byte-order-mark state preserved in all four source files (`LocalSession.cs` and the test file carry a mark; `LocalServer.cs` and the notes do not). One pre-existing warning sits in `LocalServer.cs` and is left as it is: `CA2200` at line 304, a `throw E;` in `GetCacheTimeStamp` that resets the stack trace. It is in the tree's standing warning count, it is in no method this ticket touches, and changing it is not this ticket's.
- **The implementer's tests.** All three cases pin behaviour that nothing else covers, and none restates the implementation or verifies a mock, so nothing was cut. The decorator in the new case is fault injection at the client's own boundary, not a stand-in for a module we own: every call but the one ping runs the product's code.

## Validation run

- `dotnet build Dataphor.slnx` and `dotnet build Dataphor.slnx -c Release`: 0 errors. Debug 0 warnings on a full incremental rebuild; Release 143, the tree's standing count. The one Debug warning seen on a rebuild that recompiled `Client.csproj` is the pre-existing `CA2200` noted above.
- The three-part suite from the root `AGENTS.md`, each in the foreground, all green: part 1 (everything but the coverage scripts) 444 passed / 0 failed across `Common.Tests` (13) and `DAE.Server.Tests` (431) in 6 minutes — one more than the implement stage's 443, the new case; part 2 (coverage scripts A-R) 67 passed, 11 skipped in 3 m 40 s; part 3 (the rest) 48 passed, 14 skipped in 3 m 25 s. The skips are the manifest's known-failure, unstable and fragment entries, as always.
- `ConnectionKeepAliveTest` and `CoreWcfProtocolTest` were re-run after the last edit: 18 passed.
- No pre-existing failure surfaced; `tickets/.pre-existing-error.md` was not written.
