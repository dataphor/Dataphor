description: The network connection that a Dataphor client keeps to a server is now handed out through one guarded holder, so the client no longer throws away connections it has just built or fails a call with an error that names nothing a user could act on.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: src/protocol/Common/WCF/ChannelHolder.cs (new), src/protocol/Common/WCF/ServiceClient.cs, src/protocol/Client/Client/ClientServer.cs, src/protocol/Common/Common.csproj, src/protocol/NativeCLI/NativeCLISession.cs, tests/Common.Tests/ChannelHolderTest.cs (new), tests/Common.Tests/Common.Tests.csproj, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs, docs/modernization-plan.md, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What landed

Two classes held a WCF channel in a field, rebuilt it when it was unusable, and did so without any guard — while two threads really reach that field, because the shipped client pings its connection from a keep-alive thread of its own while the application calls through the same object. The same eleven lines, written twice: `ServiceClient<T>.GetInterface` (the base of `ListenerClient` and `NativeCLIClient`) and `ClientServer.GetServiceInterface`. Both now go through one guarded holder, so the pattern exists once and the bad states are unreachable rather than merely unlikely.

**`src/protocol/Common/WCF/ChannelHolder.cs`** — new public class `Alphora.Common.WCF.ChannelHolder<T>` (`ChannelHolder(Func<T> createChannel)`, `T GetChannel()`, `Dispose()`). `GetChannel` holds one private lock around the whole decision: test the held channel's state, reuse it or create a replacement, and return the **local** the call settled on rather than a re-read of the field. A channel that is being replaced is aborted — not closed — and aborted after the lock is released. Public rather than internal because the `Client` assembly uses it and `Common` exposes no internals to it.

Three shape decisions, each of which was one of the defects:

- **Usable means "not `Faulted`, `Closing` or `Closed`"**, so `Created` and `Opening` count as usable. A channel out of `ChannelFactory<T>.CreateChannel()` is in state `Created` and only reaches `Opened` once the first call on it has connected, so the old `State == Opened` test meant every caller created a new channel for the duration of a whole network round trip, and all but one were discarded. The cache the class exists to provide did not hit at all over exactly the interval the two threads compete over.
- **The replaced channel is aborted, not dropped.** `Abort()` and not `Close()`, because the channel being replaced is faulted, superseded or closing, and a `Close()` against a server that has gone blocks (the binding sets no send or receive time-out). Only `Dispose` closes, and only when the channel reports `Opened`.
- **No fault handler at all.** Reading the state under the lock on every call is what the `Faulted` handler was doing; deleting it removes the third writer of the field, and with it the question of a fault raised on a communication-stack thread.

**`ServiceClient<T>`** gained `where T : class` and lost `_channel`, `SetChannel`, `ChannelFaulted`, `IsChannelFaulted` (already unused), `IsChannelValid` and `CloseChannel`; `GetInterface()` is now one line over a holder built in the constructor. **`ClientServer`** kept its own `ChannelFactory<IClientDataphorService>` (deliberately not moved onto the shared `ChannelFactoryCache` — that changes factory lifetime and when `DataphorServiceUtility.ServiceEndpointHook` runs, and is a separate question), lost the same six members, and builds its holder in `Open()` over the factory *local* rather than the field. `GetServiceInterface()` reads the holder field once into a local and raises `ServerException.Codes.ServerInactive` when it is absent, instead of passing `CheckActive()` and then dereferencing a field `Close()` had nulled.

`ReportCommunicationError`'s commented-out body became a `// NOTE: accepted tradeoff — …` comment stating what was declined (closing the channel on a communication failure), why (unwinding against a vanished server took ten times as long), the revisit condition (a bounded time-out on the client binding), and what the holder's state test now covers instead.

## Use cases to check

- **A client survives a network blip.** Application calls and the two-minute keep-alive ping go through one `ClientServer`; a blip faults the channel, the next call replaces it, and no channel is left holding a socket. The accounting is what to look at: every channel created should reach a caller, and every channel replaced should be aborted exactly once.
- **A client that has just connected.** The first few calls on a fresh client now share one channel rather than creating one each while the first is connecting. This is the behaviour the plan already describes ("the client keeps one channel per connection"), and the steady state was already one shared channel once it had opened — the change is confined to that first window.
- **A dispose racing a ping.** `LocalServer.Dispose` closes the connection while the keep-alive thread may be inside `GetServiceInterface`. The thread either gets `ServerInactive` (holder field already null) or `ObjectDisposedException` (it took the local first); `SendKeepAlive` swallows both. What it must not get is `NullReferenceException`.
- **A client opened, closed and opened again.** `Open()` rebuilds the holder; `Close()` disposes it before closing the factory that made its channels.
- **Listener and native-CLI clients.** Both derive from `ServiceClient<T>`; neither may rely on `GetInterface()` handing back a fresh channel per call. Their fixtures pass.

## Tests

Each case verifies one behaviour, and the counters in them are exact, so none can pass vacuously.

| Test | What it verifies |
|---|---|
| `ChannelHolderTest.TestAChannelIsReusedBeforeItHasOpened` | A channel is reused from the moment it is created (state `Created`): two calls return the same instance and the factory ran once. This is defect (c) — the old `Opened`-only test. |
| `ChannelHolderTest.TestAFaultedChannelIsReplacedAndTheReplacedOneAborted` | A faulted channel is replaced, and the one replaced is aborted exactly once and never closed. Defect (b)'s shutdown half. |
| `ChannelHolderTest.TestAFaultDuringTheStateTestDoesNotReturnNull` | A fault raised from inside the holder's own state read does not make `GetChannel` return null. Defect (a), deterministic and single-threaded — the arm that previously needed a real server and a severed connection. |
| `ChannelHolderTest.TestEveryChannelCreatedIsReturnedToACaller` | Under four threads × 2,500 calls, with stubs that fault themselves every few calls: every channel created was handed to a caller, every channel replaced was aborted exactly once, none was closed, and the number aborted is the number created less the one still held. Defect (b)'s leak. |
| `CoreWcfProtocolTest.TestACallOnAClosedClientServerIsRefusedByCode` | A closed `ClientServer` refuses `GetServiceInterface()` with `ServerException.Codes.ServerInactive`, not `NullReferenceException`. Needs no server: constructing with an override port contacts nothing. |

The holder tests need no WCF and no network — the holder reaches a channel only through `ICommunicationObject`, so a stub with a settable state, abort/close counters and a hook that runs inside the state read shows every case. They live in `tests/Common.Tests`, which is milliseconds; the four took 32 ms together. That project's `.csproj` has default compile items off, so the file and a `ProjectReference` to `src/protocol/Common/Common.csproj` were both added.

## Validation run

- `dotnet build Dataphor.slnx` — succeeded, 0 errors, 7 warnings (all pre-existing; no new warning from the new files, and `ServiceClient.cs` still carries exactly the two `CS0168` unused-catch-variable warnings it had before).
- The three suite parts from the root `AGENTS.md`, each in the foreground: part 1 `Failed: 0, Passed: 674, Skipped: 33` (7 m 22 s); part 2 `Failed: 0, Passed: 67, Skipped: 11` (3 m 4 s); part 3 `Failed: 0, Passed: 50, Skipped: 12` (3 m 6 s). `tests/Common.Tests` on its own: `Passed: 17`.
- No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written.

## Known gaps, honestly

- **`ServiceClient<T>.GetInterface()` after `Dispose()` still raises `NullReferenceException`**, because `Dispose` nulls `_channelHolder` the way the file already nulls `_channelFactoryWrapper` — and before this change the same call raised `NullReferenceException` off the nulled wrapper, so the behaviour is unchanged rather than newly wrong. A one-line improvement is available if a reviewer wants it: stop nulling `_channelHolder` in `Dispose` (the holder's own `Dispose` is idempotent), and a post-dispose call then gets `ObjectDisposedException`. Not taken here because the ticket's TODO specified the nulling and nothing in the tree calls a disposed client. `ClientServer`, which is the site with the keep-alive race, does raise an honest error on both paths.
- **Defect (a) is pinned at the holder, not end to end.** Forcing the original `NullReferenceException` through a real client needs a channel that is already `Opened` and then faults between two reads of the field, which needs a real server and a severed connection. The holder's test raises the fault from inside the state read instead, which is the same instant; no end-to-end case was added, as the ticket directed.
- **The 8,000-channel measurement in the ticket came from a throwaway console program that no longer exists** (the fix ticket deleted it). The standing check of that accounting is the threaded holder test, which is cheap enough to keep. Nothing re-measures the 44–123 ns lock cost or the 13–24 µs channel creation; those numbers are recorded in `ChannelHolder`'s remarks and in the K38 row, and the margin between them is five orders of magnitude, so no `docs/perf-baseline.md` entry was added.
- **Concurrent calls now share one channel earlier than before.** Two threads calling a freshly built client used to get two channels (because neither was `Opened` yet) and now get one. WCF channels carry concurrent requests, `NativeCLISession` serializes its own calls with a lock, and once a channel had opened the old code shared it too — but if anything in the tree depended on per-call channel isolation, this is where it would show. The listener, native-CLI and out-of-process protocol fixtures all pass.

## Two edits beyond the TODO, both for accuracy

- `src/protocol/NativeCLI/NativeCLISession.cs` carried a remark saying this ticket "owns that pre-existing race, and nothing here depends on its being fixed". That is now stale; it says instead that the field is guarded in its own right (`ChannelHolder`, K38) and the lock there is for the session handle alone.
- `ClientServer.CheckActive()` had exactly one caller, `GetServiceInterface`, and the holder's own null test replaces it. Removed rather than left dead; `CheckInactive` stays, used by the property setters.
- `CoreWcfProtocolTest.cs` carries a size-debt `NOTE:` with a measured line count; it is re-measured (767 lines, 2026-11-22) rather than left reading 754.

## Documents

- `docs/modernization-plan.md`: K38 added to the *Known defects* table, covering all three arms, both sites, the holder's shape, the measurements, and the test that pins each. The WS5 sentence that "the client keeps one channel per connection and silently builds a new one whenever the old one faults" was checked against what landed and reads correctly now — it was the sentence that was *not* true before this fix; no edit needed.
- `docs/migration-notes.md`, under *Custom C# extensions*: the new public `ChannelHolder<T>`; the `where T : class` constraint on `ServiceClient<T>` (an extension that supplied a value type could never have worked — the channel is cast to `ICommunicationObject` — so this turns a run-time failure into a compile error); and the deployment-visible change, which is fewer sockets held per client.

Nothing here touches a contract shape, an operation signature, a port, an error code, a registered class name or an on-disk format.
