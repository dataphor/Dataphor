description: The client code that talks to a Dataphor server over the network keeps one network connection in a field and rebuilds it when it breaks, without any guard against two threads doing that at the same time. The client's own background "still here" message runs on one of those threads, so two threads really do reach it together, and when they collide the client can lose a connection it just built or fail a call for no reason the user can see.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: src/protocol/Common/WCF/ServiceClient.cs, src/protocol/Client/Client/ClientServer.cs, src/protocol/Client/Server/LocalServer.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: Nobody has reported it, the window is a few instructions wide, and the usual outcome is one wasted channel rather than a visible failure, so a maintainer may reasonably rank it below work with a user complaint behind it; and the obvious fix puts a lock in the path of every call the client makes, which wants measuring before it is accepted.
----
## What goes wrong

Two classes hold a network channel to a Dataphor server lazily, in a field, and rebuild it when it is missing or no longer usable:

- `ServiceClient<T>.GetInterface()` in `src/protocol/Common/WCF/ServiceClient.cs` — the base class of every client the protocol assemblies build, including the native call interface's `NativeCLIClient`.
- `ClientServer.GetServiceInterface()` in `src/protocol/Client/Client/ClientServer.cs` — the same pattern written out again for the main client interface.

Both read the field, test it, and assign a freshly created channel to it, with no lock and no interlocked operation. `ServiceClient`'s version also subscribes and unsubscribes a `Faulted` event handler as it assigns, and a third method, `ChannelFaulted`, clears the field from whatever thread the communication stack happens to raise the event on.

Two threads arriving together can therefore both find the field empty or faulted and both create a channel. One of the two is then stored and the other is dropped on the floor without being closed: an open TCP connection and its WCF plumbing, released only when the garbage collector finds it. Worse interleavings are possible because the field, the event subscription and the `Faulted` handler are three unsynchronized writers of the same state: a handler can be left attached to a channel the field no longer holds, so a later fault clears a channel that is in use, and a caller can be handed a channel another thread has just abandoned.

## Why two threads are really there

This is not hypothetical concurrency. The client's keep-alive is a background thread: `LocalServer` pings its connection every two minutes (`src/protocol/Client/Server/LocalServer.cs`), and that ping goes through `ClientConnection.Ping` to `ClientServer.GetServiceInterface()` — the same object and the same field the application's own calls go through. So every client process that holds a connection has a second thread entering this code on a timer, and the two collide whenever a ping lands at the moment the application makes a call and the channel is absent or faulted. That is most likely just after a network blip, which is exactly when the client is rebuilding and when losing the rebuilt channel costs the most.

`repro: static` — found by reading the code while planning `bug-native-cli-sessions-never-reclaimed`, not by watching it happen. What would confirm it: a test holding one `ClientServer` and calling it from two threads in a loop while a third faults the channel, counting channels created against channels closed; or, at the unit level, the same loop over a `ServiceClient<T>` subclass with a stub channel factory. Either needs care not to become a test that merely exercises a race without being able to fail on it.

## Two sites, one root cause

One ticket with two arms, not two tickets: the defect is the same unsynchronized lazy field in two copies, and a fix wants to be the same in both. Whether the second copy should exist at all is a fair question to answer at the same time — `ClientServer` does not derive from `ServiceClient<T>` and keeps its own channel factory — but merging them is a larger change than making them correct, and this ticket need not take it on.

## What a fix has to settle

- **What the lock covers.** The narrow answer is the test-and-create in each method, plus the two event-handler assignments, under one lock per client object. The cost is a lock acquisition on the path of every call the client makes, which is the thing to measure before accepting it: these methods are called once per operation, so the measurement belongs with the protocol timings in `docs/perf-baseline.md` rather than being argued about.
- **What happens to the loser's channel.** A thread whose channel is not the one stored must close or abort it rather than drop it, or the fix trades a race for a leak.
- **Whether `ChannelFaulted` can stay as it is.** It runs on a stack thread and compares by reference before clearing, which is nearly right; under a lock it becomes straightforwardly right, but it must not deadlock against a fault raised inside a call that already holds the lock.
- **Whether `ClientServer.ReportCommunicationError` should do anything again.** Its body is commented out, with a note saying that closing the channel made unwinding ten times slower against a server that had gone. That interacts with any fix here, because it is the other thing that would have cleared the field.

Note for whoever takes this: nothing currently in the tree *depends* on the fix. `native-cli-session-liveness-clock` adds a second thread to the native client but serializes it against that session's own calls with a lock of its own, precisely so as not to add a third caller to this race.
