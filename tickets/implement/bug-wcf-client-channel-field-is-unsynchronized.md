description: The client code that talks to a Dataphor server over the network keeps one network connection in a field and rebuilds it when it breaks, without any guard against two threads doing that at the same time. Two threads really are there, so the client leaks connections it has just built, and a call can fail with an error that names nothing a user could act on.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: src/protocol/Common/WCF/ChannelHolder.cs (new), src/protocol/Common/WCF/ServiceClient.cs, src/protocol/Common/Common.csproj, src/protocol/Client/Client/ClientServer.cs, tests/Common.Tests/ChannelHolderTest.cs (new), tests/Common.Tests/Common.Tests.csproj, tests/DAE.Server.Tests/CoreWcfProtocolTest.cs, docs/modernization-plan.md, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What goes wrong

Two classes hold a network channel to a Dataphor server lazily, in a field, and rebuild it when it is missing or no longer usable:

- `ServiceClient<T>.GetInterface()` in `src/protocol/Common/WCF/ServiceClient.cs` — the base of every client the protocol assemblies build (`DataphorServiceClient<T>` → `ListenerClient`, `NativeCLIClient`).
- `ClientServer.GetServiceInterface()` in `src/protocol/Client/Client/ClientServer.cs` — the same pattern written out a second time, for the main client interface.

Both are the same eleven lines, and both are wrong in the same three ways.

**They read the field three times and assume it did not change in between.** `GetInterface` tests `_channel` for null, passes it to `IsChannelValid`, and then returns `_channel` — three separate reads of a field that two other methods write without synchronization. `SetChannel` is itself a three-step update (detach the fault handler from the old channel, assign, attach it to the new one), and `ChannelFaulted` clears the field from whatever thread the communication stack raises the event on. On the fast path, where the held channel is valid and no channel is created at all, a fault arriving between the validity test and the return makes the method **return null**, and the caller's next line is `channel.BeginSomething(...)` — a `NullReferenceException` out of the middle of an ordinary operation, where the honest answer was a communication failure.

**They drop channels on the floor.** Two threads that both find the field unusable both create a channel; `SetChannel` overwrites without closing what it replaces, and the method returns the *field*, so whichever thread assigned last supplies the channel both callers use. The other channel — a WCF channel over a TCP connection — is referenced by nobody and closed by nobody. The binding sets no receive time-out (`DataphorServiceUtility`, `TimeSpan.MaxValue`), so a leaked channel that has been opened holds its socket, and the server's end of it, until the client process ends.

**The window is not a few instructions wide; it is a whole round trip.** `IsChannelValid` requires `CommunicationState.Opened`, and a channel from `ChannelFactory<T>.CreateChannel()` is in state `Created` — WCF opens it implicitly inside the first operation called on it. So from the moment a channel is created until the first call on it has finished opening it (a TCP connect, and whatever the binding's handshake costs), every other thread reaching `GetInterface` finds the field unusable and creates another channel. The cache this class exists to provide does not hit at all over that interval. The original ticket's estimate of "a few instructions" was wrong by the cost of a network round trip, and that interval is exactly the one the two threads below compete over.

## How it was reproduced

Measured 2026-11-22, Release, one machine, with a throwaway console program referencing `src/protocol/Client` (`ClientServer` against `localhost:65001`, where nothing listens — none of this needs a server, because none of it gets as far as the network). The program has been deleted; it is six lines around `GetServiceInterface` and is quicker to rewrite than to read.

- A channel straight out of `GetServiceInterface()` reports state `Created`, and a second sequential call returns a *different* channel instance. Both clients, deterministically, every run. That is the third defect above: the field is never reused until a call has opened the channel.
- Two threads calling `GetServiceInterface()` 4,000 times each on one `ClientServer`: of 8,000 channels created, 13, 130 and 58 were handed to nobody in three successive runs — created, overwritten by the other thread, never closed. That is the second defect, directly observed, at 0.2 % to 1.6 % of calls on a tight loop.

The `NullReferenceException` arm is read from the code rather than observed: forcing it needs a channel that is already `Opened` and then faults between two reads of the field, which needs a real server and a severed connection. The deterministic way to confirm it is the stub test below, which can raise `Faulted` from inside the `State` read that `IsChannelValid` performs; it belongs with the fix, not before it.

## Why two threads are really there

The client's keep-alive is a background thread: `LocalServer` pings its connection every two minutes, and that ping runs `ClientConnection.Ping` → `ClientServer.GetServiceInterface()` — the same object and the same field the application's own calls go through. Every client process holding a connection has a second thread entering this code on a timer. The collision needs the channel to be absent, faulted or newly created when the ping lands, which is most likely just after a network blip — exactly when the client is rebuilding and when losing the rebuilt channel costs the most.

## The fix

One guarded holder, used by both sites, so that the pattern exists once and the bad states are unreachable rather than merely unlikely.

**New type, `Alphora.Common.WCF.ChannelHolder<T>`, in `src/protocol/Common/WCF/ChannelHolder.cs`.** Public, because the `Client` assembly uses it and `Common` exposes no internals to it.

```csharp
public class ChannelHolder<T> : IDisposable where T : class
{
    public ChannelHolder(Func<T> createChannel);

    /// <summary>A channel that is usable now. Never null, and never the field: the caller is given the
    /// channel this call settled on, whatever another thread does next.</summary>
    public T GetChannel();

    /// <summary>Releases the held channel, if any. Idempotent.</summary>
    public void Dispose();
}
```

`GetChannel` holds one private lock for the whole decision:

```
T result, abandoned = null;
lock (sync)
{
    CheckNotDisposed();
    if (_channel != null && IsUsable(_channel))
        result = _channel;
    else
    {
        abandoned = _channel;
        _channel = _createChannel();
        result = _channel;
    }
}
if (abandoned != null)
    Abort(abandoned);       // non-blocking, and off the lock
return result;              // the local, never a re-read of the field
```

Four things that shape make true, each of which is a defect above:

- **`IsUsable` is "not `Faulted`, `Closing` or `Closed`"**, so `Created` and `Opening` count as usable. A channel is then reused from the moment it is created, which is what the caching in this class was always for, and the window two threads can collide over shrinks from a round trip to the few instructions the lock covers.
- **The channel being replaced is aborted, not dropped.** `Abort()` and not `Close()`: the channel being replaced is faulted, superseded or closing, so there is no orderly shutdown to attempt, and `Close()` against a server that has gone blocks — which is the cost the note in `ClientServer.ReportCommunicationError` records. `Abort()` returns immediately and releases the socket. Only `Dispose` closes (`Close()` when `Opened`, `Abort()` otherwise, exceptions swallowed, as both classes do today).
- **The fault handler goes away entirely.** With `IsUsable` reading the state under the lock on every call, `ChannelFaulted` has nothing left to do: it cleared the field a little sooner than the next call would have, and a faulted channel's socket is dead either way. Deleting it removes the third writer of the field and, with it, the question the original ticket raised about a fault raised on a stack thread deadlocking against a call — nothing in the holder calls the network under the lock (`CreateChannel` does no I/O; see the measurement below), and `Monitor` is re-entrant, so a `Faulted` event raised synchronously on the calling thread could not deadlock even if one existed.
- **`GetChannel` returns a local.** The null return is unreachable, not merely rare.

**`ServiceClient<T>`** keeps its channel-factory wrapper and loses everything else: `_channel`, `SetChannel`, `ChannelFaulted`, `IsChannelFaulted` (already unused today), `IsChannelValid` and `CloseChannel` all go, replaced by one `ChannelHolder<T>` built in the constructor over `() => _channelFactoryWrapper.ChannelFactory.CreateChannel()`. `GetInterface()` becomes one line. `Dispose()` disposes the holder before releasing the wrapper, in the same `try`/`finally` shape it has now. The class needs `where T : class` added for the holder's sake; every use in the tree already satisfies it, because `DataphorServiceClient<T>` declares it.

**`ClientServer`** keeps its own `ChannelFactory<IClientDataphorService>` — do not move it onto the shared `ChannelFactoryCache`, which would change factory lifetime and when `DataphorServiceUtility.ServiceEndpointHook` runs, and is a separate question. It loses the same six members, gains a `ChannelHolder<IClientDataphorService>` created in `Open()` over `() => factory.CreateChannel()` where `factory` is the local the method has just built (captured, not re-read from the field), and `Close()` disposes the holder and then nulls both fields. `GetServiceInterface()` reads the holder field once into a local and raises `ServerException.Codes.ServerInactive` if it is null, instead of letting `CheckActive()` pass and then dereferencing a field `Close()` has nulled — which today is a `NullReferenceException` from the ping thread whenever a dispose races a ping. A thread that got the local just before `Close` now sees `ObjectDisposedException` from the holder: an honest error, and `SendKeepAlive` already swallows it.

**`ReportCommunicationError` stays a no-op.** Its body is commented out with a reason — closing the channel made unwinding against a vanished server ten times slower — and that is a decision already made, not a defect to re-open. Convert the comment into the project's `// NOTE: accepted tradeoff — …` form at the site, stating what was declined, why, and the revisit condition, so the next reviewer does not re-discover it. Say in it what the state test now does and does not cover: `Faulted`, `Closing` and `Closed` are caught on the next call, and a channel that reports `Opened` while every call on it throws `CommunicationException` is the case still left to the caller.

## Why the lock is affordable

The question the original ticket left open, settled by measurement rather than argument. Measured 2026-11-22, Release, same machine, 200,000 iterations: an uncontended `lock` acquire and release costs 44 ns in one run and 123 ns in another. `GetServiceInterface()` with one `CreateChannel()` in it costs 13.0, 18.0 and 24.1 µs across three runs. So the lock is well under one per cent of the channel creation it guards on the slow path, and on the fast path it is ~100 ns in front of a call that is a network round trip. Nothing here needs a `docs/perf-baseline.md` entry: the margin is five orders of magnitude and the numbers above are the record.

## Tests

The holder is the lowest layer that shows all of this, and it needs no WCF at all: a stub implementing `ICommunicationObject` and a one-method service interface, with a settable `State`, `Abort`/`Close` counters, and a hook that runs an action the first time `State` is read. `tests/Common.Tests` is the right project — it is milliseconds, and it keeps these cases out of the engine fixtures. Its `.csproj` has default compile items off and currently references only `Base` and `Base.Windows`, so add both the `Compile` item and a `ProjectReference` to `src/protocol/Common/Common.csproj`.

Four cases, one behaviour each:

- A channel is reused once created: two `GetChannel()` calls with the stub left in `Created` return the same instance and the factory ran once. This is the characterization of what the caching was always meant to do, and it fails on today's code.
- A channel that is `Faulted` is replaced, and the one replaced is aborted exactly once and never closed.
- A fault raised from inside the `State` read — the stub's hook — does not make `GetChannel` return null. Deterministic, single-threaded, and the arm that could not be reproduced before the seam existed. (Write it against the holder's own behaviour, not against a reconstruction of `ChannelFaulted`: the point is that the caller gets a usable channel.)
- The accounting invariant, under threads: N threads × enough iterations, with a factory that counts creations and a stub that faults itself periodically so replacement happens throughout, asserting that **every channel created was either returned to a caller or aborted**. The counters are exact, so the assertion cannot pass vacuously; only whether a collision occurs is left to timing, and the probe above found collisions within a few thousand iterations in every run. Pick the iteration count so the case stays well under a second.

At the client layer, one case, in `tests/DAE.Server.Tests` beside the other protocol fixtures: a `ClientServer` opened against an override port and then closed raises `ServerException.Codes.ServerInactive` from `GetServiceInterface()`, not `NullReferenceException`. It needs no server — opening with an override port contacts nothing, as the reproduction above relies on. Do not add a higher-layer repeat of the reuse case; it is the same behaviour one layer up.

## Documents

- `docs/modernization-plan.md`: add a row to the *Known defects* table — the next number is **K38** — covering all three arms, the two sites, and the test that pins each. Note while you are there that the WS5 sentence at line 280, "The client keeps one channel per connection and silently builds a new one whenever the old one faults", is only true *after* this fix (today it builds a new one on every call until one has been opened). It needs no edit, but check the wording still reads correctly against what landed.
- `docs/migration-notes.md`, under *Custom C# extensions*: the new public `ChannelHolder<T>`; the `where T : class` constraint added to `ServiceClient<T>` (an extension that supplied a value type could never have worked — it is cast to `ICommunicationObject` — so this turns a run-time failure into a compile error); and the observable change for a deployment, which is fewer sockets: a channel is now reused from creation rather than from its first completed call, and a replaced channel is aborted at once instead of waiting for the garbage collector.

Nothing here touches a contract shape, an operation signature, a port, an error code, a registered class name or an on-disk format.

## TODO

- Add `src/protocol/Common/WCF/ChannelHolder.cs` with the type above, and its `Compile` item in `src/protocol/Common/Common.csproj`. Match the folder's source hygiene: tabs, CRLF, UTF-8 with a byte-order mark, and the same licence header and XML-documentation style as `ChannelFactoryCache.cs`.
- Rewrite `ServiceClient<T>` over the holder: add `where T : class`, delete `_channel`, `SetChannel`, `ChannelFaulted`, `IsChannelFaulted`, `IsChannelValid` and `CloseChannel`, reduce `GetInterface()` to the holder call, and dispose the holder in `Dispose()` before the factory wrapper.
- Rewrite `ClientServer` over the holder: keep its own channel factory, create the holder in `Open()` over the local factory, delete the same six members, make `GetServiceInterface()` read the holder once and raise `ServerInactive` when it is absent, and dispose the holder in `Close()` before closing the factory.
- Convert the commented-out body of `ClientServer.ReportCommunicationError` into a `// NOTE: accepted tradeoff — …` comment with the decision, its reason, the revisit condition, and what the holder's state test now covers.
- Add `tests/Common.Tests/ChannelHolderTest.cs` with the stub channel and the four cases, and wire the file and the `Common.csproj` reference into `tests/Common.Tests/Common.Tests.csproj`.
- Add the `ClientServer`-after-`Close` case to `tests/DAE.Server.Tests`.
- Add the K38 row to `docs/modernization-plan.md` and the three entries to `docs/migration-notes.md`.
- Build, then run the suite in the three parts the root `AGENTS.md` names, each as its own foreground command. Watch for `ListenerClient` and `NativeCLIClient` callers that relied on `GetInterface()` handing back a fresh channel each time — nothing in the tree should, but the listener and native-CLI fixtures are where it would show.
