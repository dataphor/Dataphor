description: The server decides that a client has gone away by comparing two readings of the local clock, so when the clock itself moves — the spring daylight-saving change, an operator correcting the time, a large time-synchronization step — every connected client looks like it has been gone for an hour and the server throws all of their work away at once.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Server/RemoteServerConnection.cs, src/engine/Server/Service/ConnectionManager.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: It needs the local clock to jump forward by more than five minutes, which on a well-run machine happens once a year at the daylight-saving change and otherwise not at all, and the clients recover by reconnecting, so a maintainer may reasonably leave it; against that, the change is small and the blast radius when it does fire is every connected client at once.
----
## What goes wrong

A client keeps its connection to the server alive by sending a "still here" message every two minutes. The server records when the last one arrived (`RemoteServerConnection.Ping` stores `DateTime.Now` in `_lastActivityTime`) and a watcher thread wakes periodically and relinquishes every connection whose recorded time is older than five minutes ago (`ConnectionManager.RelinquishIdleConnections`, comparing against `DateTime.Now.AddSeconds(-300)`).

`DateTime.Now` is local wall-clock time, and the difference between two readings of it is not the time that passed. When the clock moves forward, every recorded time instantly looks that much older:

- **The spring daylight-saving change** moves local time forward by an hour. A connection pinged at 01:59 keeps 01:59; the first sweep after the change computes a cut-off near 02:55 and relinquishes it, along with every other connection on the server.
- **An operator correcting a badly wrong clock, or a time-synchronization step** large enough to exceed the five-minute threshold does the same, at any time of year.

Relinquishing a connection closes its sessions, aborts each process's executing thread and rolls back its open transactions. So the effect is every connected client losing its sessions and any open work at the same moment, with nothing in the log to distinguish it from every client having genuinely gone away. The clients reconnect — the shipped client rebuilds its channel — but their sessions and their uncommitted work are gone.

The autumn change moves local time backward and has the opposite, harmless effect: for an hour nothing is reclaimed, so an abandoned connection is kept about an hour longer than it should be.

`repro: static` — read from the code, not watched. What would confirm it: the comparison is pure, so a test can inject the two times rather than move the machine's clock, but `RelinquishIdleConnections` is private and takes its cut-off from `DateTime.Now` inside the watcher, so confirming it at that level needs a seam the fix would add anyway.

## The fix in one line, and why it is filed rather than done

Record elapsed time with a monotonic clock — `Environment.TickCount64`, a signed 64-bit millisecond count since the machine started, which no clock change and no time-zone rule affects and which does not wrap within a machine's lifetime — and compare ticks with ticks. That is what the native interface's session clock does, added by `native-cli-session-liveness-clock`: that site chose the monotonic basis deliberately rather than copying this one, and it is the pattern to follow here.

Filed separately because it is a different site from the ticket that found it, because `ConnectionManager` and `RemoteServerConnection` were settled and tested only recently (`port-abandoned-session-cleanup`, `test-client-server-abandoned`) and are worth changing on their own with their own test rather than as a side effect of other work, and because the public property `RemoteServerConnection.LastActivityTime` is involved: a test in this suite reads it (`ConnectionKeepAliveTest`) and so could anything outside. Keeping it as a `DateTime` for display while deciding on ticks, or replacing it and taking the migration note, is the one real choice the fix has to make.
