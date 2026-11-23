description: The server decides a client has gone away by comparing two readings of the local clock, so when the clock itself jumps forward — the spring daylight-saving change, an operator correcting the time, a large time-synchronization step — every connected client looks like it has been idle for an hour and the server throws all of their work away at once. Measure elapsed time with a clock that only ever moves forward at one rate.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Server/RemoteServerConnection.cs, src/engine/Server/Service/ConnectionManager.cs, src/engine/DAE/Server/Engine.cs, src/engine/Server/NativeCLI/NativeSession.cs, tests/DAE.Server.Tests/ConnectionKeepAliveTest.cs, docs/modernization-plan.md, docs/migration-notes.md, docs/architecture.md
repro: static
difficulty: medium
----
## What is wrong

Two places in the server decide something by subtracting one reading of the *local wall clock* from another. The difference between two readings of `DateTime.Now` is not the time that passed: it is the time that passed plus however much the clock itself moved in between. Local time moves on its own at every daylight-saving transition, and it moves whenever an operator corrects a wrong clock or a time-synchronization service applies a large step.

**The connection idle watcher (the reason this ticket exists).** A client keeps its connection alive by pinging every two minutes. `RemoteServerConnection.Ping` stamps `_lastActivityTime = DateTime.Now`; `ConnectionManager.RelinquishIdleConnections` is handed `DateTime.Now.AddSeconds(-300)` and relinquishes every connection whose stamp is older than that. When local time steps forward by more than five minutes, every stamp on the server instantly looks that old, so the first sweep after the step relinquishes **every connection at once** — closing their sessions, aborting each process's executing thread, and rolling back their open transactions. Nothing in the log distinguishes it from every client having genuinely vanished. Clients reconnect (the shipped client rebuilds its channel), but their sessions and uncommitted work are gone. The autumn transition has the harmless opposite effect: for an hour nothing is reclaimed, so an abandoned connection is kept about an hour longer than it should be.

**The process-termination wait (the same defect, second site).** `Engine.TerminateProcessThread` (`src/engine/DAE/Server/Engine.cs`, around lines 497 and 519) sets `DateTime endTime = DateTime.Now.AddTicks(ProcessTerminationTimeout.Ticks / 2)` and spins until the aborted process's thread dies or `DateTime.Now > endTime`. A forward step makes that deadline expire on the loop's first pass, so a process that was about to unwind normally is reported as `ProcessNotResponding` instead; a backward step makes the loop wait up to an hour for a thread that is never going to stop. This is the same wait `ConnectionManager`'s own note refers to, so a clock step can hit both arms of this ticket in the same sweep.

## What was measured, and what was not

The clock step and the comparison it breaks are measured, not assumed. For this machine's zone (Mountain), two real instants two minutes apart that span the 2027 spring transition give a local-time delta of **1 h 2 min** (offset −07:00 before, −06:00 after), and the sweep's own formula — stamp `01:59:00` against a cut-off of `now − 300 s` = `02:56:00` — evaluates **true**, so a connection pinged two minutes earlier is relinquished. That is arithmetic over the installed time-zone data, so it holds for any zone with a one-hour spring transition.

What was *not* observed is the end-to-end effect — sessions torn down, transactions rolled back — because that needs the machine clock to actually move, hence `repro: static`. There is no way to step the clock from inside a test in this suite: the `TZ` environment variable plus `TimeZoneInfo.ClearCachedData()` does **not** shift `TimeZoneInfo.Local` on Windows (probed on .NET 10.0.15 in this tree: the zone stayed Mountain and `DateTime.Now` did not move), and changing the machine clock is out of bounds for a test. After the fix the code reads a source nothing in the process can step, so the absence of a reproduction is structural rather than an omission — see *Tests* below for what is pinned instead.

## The fix

Measure elapsed time from a monotonic source and compare elapsed against elapsed. This is the choice `native-cli-session-liveness-clock` already made for `NativeSession` after reading this site and deciding not to copy it; that file is the pattern, and its remarks point here.

**Arm 1 — the connection clock.** `RemoteServerConnection` keeps `private long _lastActivityTicks` from `Environment.TickCount64` (a signed 64-bit millisecond count since the machine started, unaffected by any clock change or time-zone rule, and which does not wrap within a machine's lifetime), stamped in the constructor and in `Ping`. The public read becomes elapsed time rather than an instant, and `ConnectionManager` compares that against its threshold.

**Arm 2 — the process-termination deadline.** `Engine.TerminateProcessThread` takes its deadline from a `Stopwatch` started before the loop instead of from `DateTime.Now`, preserving the existing semantics exactly: the budget is still half of `ProcessTerminationTimeout`, and the comparison is still strict. `Stopwatch` rather than `TickCount64` here because this is a local variable read by one thread inside one method, where a stopwatch is the idiomatic and clearest form; the connection clock is a field written by ping threads and read by the watcher thread, which is why it is a plain tick count read with `Volatile`, as `NativeSession` does it.

### The one real choice: what happens to `RemoteServerConnection.LastActivityTime`

**Recommendation: replace it, and take the migration note.** Make it `public TimeSpan IdleTime { get { return TimeSpan.FromMilliseconds(Environment.TickCount64 - Volatile.Read(ref _lastActivityTicks)); } }`.

Why replace rather than keep a wall-clock stamp beside the new measure:

- Two clocks on one object is the trap that produced this bug. The next person to add a liveness check picks one of them, and half the time it is the wrong one.
- The only thing the `DateTime` was still buying is the `LastActivity={2}` field of the relinquishment log line, and an idle duration is at least as useful there: the log line is already timestamped by the logger, so "relinquished at T, idle for 412 s" gives the absolute last-ping time by subtraction.
- It matches the sibling that landed last week. `NativeSession` has one monotonic field and one derived read (`IdleSeconds`), and `NativeServer.GetSessionIdleSeconds()` is its public form.

`TimeSpan` rather than whole seconds (the native site's unit) for one concrete reason: `ConnectionKeepAliveTest` runs the client at a one-second keep-alive interval and needs sub-second resolution to see a ping land. Whole seconds would force that fixture's interval up, as it did for `NativeSessionKeepAliveTest`.

The surface this moves is source-level only. `LastActivityTime` is not on `IRemoteServerConnection`, not on any WCF contract, and not reached by any D4 or form surface — checked repo-wide: the only readers are `ConnectionManager`, `ConnectionKeepAliveTest`, a `<see cref>` in `NativeSession.cs` and one sentence of `docs/modernization-plan.md`. A custom C# extension could still read it, which is what the migration note is for.

**The alternative, for the record:** keep `LastActivityTime` as a display-only `DateTime` and add the tick field for the decision. It avoids the migration note entirely. Rejected for the three reasons above; if the reviewer prefers it, the rest of this ticket is unchanged.

### Boundary and threading details not to lose

- **Keep the comparison strict.** Today a connection is relinquished when its stamp is *strictly older* than the cut-off, i.e. when elapsed is strictly greater than the threshold. Write `connection.IdleTime > _idleThreshold`, not `>=`, so the boundary does not move.
- **Hold the threshold as a `TimeSpan`** alongside the existing `_idleTimeInSeconds` (or in place of it) so the comparison is exact ticks rather than a `TotalSeconds` double.
- **`RelinquishIdleConnections` no longer needs a parameter** — it took a cut-off only because the caller computed one. Either drop the parameter or pass the threshold; whichever reads better, it stays private.
- **Read the idle time once per connection** into a local, and log that same value, so the number in the log is the number the decision was made on.
- **`Volatile.Write` in `Ping` and `Volatile.Read` in the getter.** The existing `DateTime` field is written by ping threads and read by the watcher thread with no synchronization at all; on the 64-bit runtime this product runs on, an aligned 8-byte read or write is atomic, so this is an ordering and clarity improvement rather than a fix for an observed defect — say so in a comment rather than claiming more for it. Do **not** reach for the connection's `_syncHandle`: `BeginCall` holds it for the whole of `Connect`/`Disconnect`, so a sweep that took it would serialize itself against clients' in-flight calls.

### Remarks and documents that become false when this lands

Last ticket's review found two stale document statements; these are this ticket's equivalents, and they are not optional.

- `ConnectionManager`'s class remark opens with a `<see cref="RemoteServerConnection.LastActivityTime"/>` and calls it a clock that moves on a ping. Still true about the ping; the cref and the wording need the new member.
- `RemoteServerConnection`'s `Lifetime Management` region remark describes a last-*ping* time. Keep everything it says about *why* nothing else moves the clock, and add why the clock is monotonic — short, pointing at `NativeSession` rather than repeating its paragraph.
- `src/engine/Server/NativeCLI/NativeSession.cs` carries two references to this site, one of them a `<see cref>` that will not resolve after a rename: its field remark says the main interface's connection clock is a wall clock and that "That is bug-idle-clocks-use-wall-clock-time, filed against the connection clock, and it is not repeated here." Once this lands, both interfaces are monotonic and that sentence must say so. The mutual pointer is worth keeping — it is what makes the pair greppable.
- `docs/modernization-plan.md`, the *Reclaiming an abandoned session* section (the paragraph beginning "What follows from the ping being the signal") names `LastActivityTime`. Rename it there, and add one sentence to that section's **Decisions** list recording that the measure is monotonic and why.
- `docs/architecture.md` line 71 describes the mechanism without naming the member. Read it and correct it only if this change makes something there false; do not rewrite it otherwise.
- `docs/migration-notes.md`, the *Custom C# extensions* table, needs an entry in the same commit: `RemoteServerConnection.LastActivityTime` (a `DateTime` of the last ping) is replaced by `IdleTime` (a `TimeSpan` of how long since it), the relinquishment log line now reports idle seconds instead of a clock time, and a clock step no longer reclaims live connections. "What to do": nothing for an application; extension code that read `LastActivityTime` reads `IdleTime`, and code that needs an absolute last-ping instant computes `DateTime.Now - connection.IdleTime` and accepts that it is only as stable as the clock.

## Tests

**Convert the three reads in `ConnectionKeepAliveTest`.** `WaitForTheClockToMovePast` waits for the stamp to increase; the equivalent for an idle duration is to wait for it to *decrease* between two consecutive reads, since only a ping can shorten it. Read `IdleTime` into a local each pass, compare against the previous read, pass on the first decrease, and keep the failure message as informative as the current one. This is a conversion of existing coverage, not a new test — and it keeps the fixture's one-second interval, which is why the new member reports sub-second resolution.

**One new case: the sweep spares a pinged connection and reclaims an unpinged one.** This is the regression test for the comparison this ticket rewrites — the risk the change actually carries is an inverted or mis-scaled comparison, whose symptom is this bug's own symptom, a live client reclaimed. Shape it in `ConnectionKeepAliveTest` (it already has the `WithAServer`/`NewClient` helpers and the LocalServer that does the pinging):

- A `ConnectionManager` at a threshold of about 10 seconds, so the watcher wakes every 2.
- A `LocalServer` pinging every second, holding a session, established first.
- A control connection established directly through the `RemoteServer` with a session on it and never pinged — the same trick `AbandonedSessionCleanupTest` uses, and what makes a vacuous pass impossible: without it the case would also pass on a watcher that never swept.
- After enough time for several sweeps and for the control to cross the threshold (about 13 s), assert the pinged client's session still executes a statement, and that a call on the control's process throws. Assert only that it throws, not its type: in process it is a `NullReferenceException`, for the reason recorded in the `NOTE:` in `AbandonedSessionCleanupTest`.
- Assert through behaviour, not through `RemoteServer.GetCurrentConnections()` — it is `internal` and the test project is a different assembly. Do not widen it.
- Note the fixture's flake margin in a remark: the case fails if a ping is delayed past the whole threshold on a loaded machine, which is why the threshold is ten times the interval rather than three.

**No test for arm 2.** Reaching `TerminateProcessThread`'s deadline needs a process whose thread will not stop when aborted, which this suite has no way to produce; it is already recorded as verified-by-inspection in the plan's *Reclaiming an abandoned session* section. The change is a like-for-like substitution of the time source with the budget and the comparison unchanged, and the build is the check.

**No test for the clock step itself, and no source-grep guard.** The step cannot be produced in process (measured above). A guard test that asserted no `DateTime.Now` appears in the liveness and timeout files was considered and is not recommended: it would be the only source-text test in the suite, it would need a maintained allowlist of the legitimate wall-clock uses (the D4 `Now()`/`Today()` operators above all), and after this ticket there are no remaining decision-grade instances for it to catch. The mutual `NOTE:`/remark pointers between the three clocks are the greppable set instead. Overturn this if you disagree — it is a judgment call, not a measurement.

## The wider class, surveyed and deliberately left alone

Every `DateTime.Now` and `DateTime.UtcNow` in `src/` was read (45 hits). Three groups:

- **Decision-grade**, where a wrong elapsed changes what the server does: the two arms of this ticket, and `NativeSession`, already monotonic. There are no others.
- **Reported durations**, where a clock step makes one printed number wrong and nothing else: `ScriptExecutionUtility` (Dataphoria's execution time), `ServerPlan` and `ServerProcess` (`Debug.WriteLine` timings), `D4Editor`, `TestNodes`, and the `System.Diagnostics.Benchmark*` D4 operators in `SystemNodes`, which return a `TimeSpan` to D4 code. Left alone: each is a one-off cosmetic wrongness, the mechanical fix is a `Stopwatch` at each site, and changing nine sites in five assemblies for that is not worth the diff or the review. No `NOTE:` left at them either — the enumeration lives here, and this ticket is archived into `complete/`.
- **Correctly wall-clock, and compatibility surfaces that must not change**: `DateTimeNodes` (the D4 `Now()` and `Today()` operators), `ServerTransaction.StartTime` (an absolute instant, not an elapsed), and log timestamps.

Not extracted into a shared monotonic primitive, though both arms and `NativeSession` would use one. `native-cli-session-liveness-clock`'s review recorded the same call for its two keep-alives and set the condition explicitly: extract on the third. This is the second site of three lines, in a different assembly from the others, and a shared primitive would be a new public-ish type in `src/engine/Base` or `src/engine/DAE` introduced by a bug fix. If you add a fourth, extract then.

## TODO

**Arm 1 — the connection clock**

- Replace `RemoteServerConnection._lastActivityTime` with `long _lastActivityTicks` from `Environment.TickCount64`, stamped in the constructor and in `Ping` with `Volatile.Write`.
- Replace `public DateTime LastActivityTime` with `public TimeSpan IdleTime`, reading the field with `Volatile.Read`; update the region remark to say why the clock is monotonic, pointing at `NativeSession`.
- Change `ConnectionManager` to hold the threshold as a `TimeSpan`, drop the cut-off parameter from `RelinquishIdleConnections`, and compare `IdleTime > threshold` — strictly, reading each connection's idle time once into a local.
- Change the relinquishment log line to report idle seconds instead of `ToShortTimeString()`, using that same local.
- Update `ConnectionManager`'s class remark and its `<see cref>`.

**Arm 2 — the process-termination deadline**

- Replace the `DateTime endTime` deadline in `Engine.TerminateProcessThread` with a `Stopwatch` started before the loop, keeping the budget at half of `ProcessTerminationTimeout` and the comparison strict.
- Add a short comment saying why it is not a wall clock, pointing at the connection clock.

**Remarks and documents**

- Correct both references to this site in `src/engine/Server/NativeCLI/NativeSession.cs`, including the `<see cref>` that a rename breaks.
- Correct the `LastActivityTime` sentence in `docs/modernization-plan.md`'s *Reclaiming an abandoned session* section and add one decision line there about the monotonic measure.
- Read `docs/architecture.md` line 71 and correct it only if this change makes it false.
- Add the `docs/migration-notes.md` entry under *Custom C# extensions*, in this commit.

**Tests and validation**

- Convert `ConnectionKeepAliveTest`'s three `LastActivityTime` reads and its `WaitForTheClockToMovePast` helper to wait for the idle time to decrease.
- Add the sparing/reclaiming case described above, with its control connection and its flake-margin remark.
- `dotnet build Dataphor.slnx` — expect 0 errors and the tree's standing 143 warnings, unchanged. Never add a `NoWarn` or a `pragma warning disable` to get there.
- Targeted first: `ConnectionKeepAliveTest`, `AbandonedSessionCleanupTest`, `AbandonedClientProcessTest`, `NativeSessionKeepAliveTest`, `NativeSessionCleanupTest`, `ServerLinkConnectionTest`.
- Then the three suite parts from the root `AGENTS.md`, each as its own foreground command. Never `dotnet test Dataphor.slnx` as one command — it exceeds the runner's ten-minute idle timeout.
- Write `tickets/.pre-existing-error.md` for any failure that is plainly not yours, after checking `tickets/.pre-existing-known.md`; never skip or loosen a test to get a green run.
