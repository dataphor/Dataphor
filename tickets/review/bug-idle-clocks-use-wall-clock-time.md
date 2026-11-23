description: The server used to decide a client had gone away by subtracting two readings of the local clock, so a clock that jumped forward — the spring daylight-saving change, an operator correcting the time — made every connected client look idle at once and the server threw all of their work away. Elapsed time is now measured from a clock that only ever moves forward at one rate.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Server/RemoteServerConnection.cs, src/engine/Server/Service/ConnectionManager.cs, src/engine/DAE/Server/Engine.cs, src/engine/Server/NativeCLI/NativeSession.cs, tests/DAE.Server.Tests/ConnectionKeepAliveTest.cs, docs/modernization-plan.md, docs/migration-notes.md
repro: static
difficulty: medium
----
## What landed

Two places in the server decided something by subtracting one reading of `DateTime.Now` from another. Both now measure elapsed time from a monotonic source and compare elapsed against elapsed. The threshold, the budget and the boundary of each comparison are unchanged; only the clock behind them changed.

**Arm 1 — the connection idle watcher.** `RemoteServerConnection` no longer keeps `DateTime _lastActivityTime`. It keeps `long _lastActivityTicks` from `Environment.TickCount64`, stamped in the constructor and in `Ping` with `Volatile.Write`, and the public read is now `public TimeSpan IdleTime` (`Volatile.Read` of the field subtracted from a fresh `TickCount64`). `ConnectionManager` holds its threshold as a `TimeSpan` built once in the constructor, `RelinquishIdleConnections` takes no parameter any more (there is no instant for a caller to compute), and the sweep reads each connection's idle time once into a local, compares `idleTime > _idleTime` — strictly, which is the boundary the wall-clock comparison had — and logs that same local as `IdleSeconds=<n>` where it used to log `LastActivity=<clock time>`.

**Arm 2 — the process-termination deadline.** `Engine.TerminateProcessThread` (`src/engine/DAE/Server/Engine.cs:497`) took its deadline from `DateTime.Now.AddTicks(ProcessTerminationTimeout.Ticks / 2)` and spun until `DateTime.Now > endTime`. It now starts a `Stopwatch` before the loop and compares `terminationWatch.Elapsed > terminationBudget`, the budget still being half of `ProcessTerminationTimeout` and the comparison still strict. A like-for-like substitution of the time source.

`Stopwatch` in arm 2 and a plain tick count with `Volatile` in arm 1, deliberately: arm 2's deadline is a local read by one thread inside one method, where a stopwatch is the clearest form, while the connection clock is a field written by ping threads and read by the watcher thread — which is how `NativeSession` already does it.

**`LastActivityTime` was replaced rather than kept beside the new measure**, which is what the implement ticket recommended. Two clocks on one object is the trap that produced the bug, and the only thing the `DateTime` was still buying was one field of one log line. The migration note for it is in `docs/migration-notes.md` under *Custom C# extensions*.

**Remarks and documents corrected**, all of which named or described the old member:

- `ConnectionManager`'s class remark and its `<see cref>`, now pointing at `IdleTime` and saying in one clause why the measure is monotonic.
- `RemoteServerConnection`'s `Lifetime Management` region: everything it said about *why* nothing else moves the clock is kept, and the field's own remark carries the monotonic argument, pointing at `NativeSession` rather than repeating it.
- Both references to this site in `src/engine/Server/NativeCLI/NativeSession.cs`, including the `<see cref>` the rename would have broken. The mutual pointers are kept — they are what makes the three clocks greppable as a set.
- `docs/modernization-plan.md`, *Reclaiming an abandoned session*: the `LastActivityTime` sentence, a new **Decisions** bullet recording that the measure is monotonic and why, and one sentence in *What rests on the test* naming the new regression case.
- `docs/architecture.md` line 71 was read and **not** changed: it describes the mechanism and the 300-to-375-second window without naming the member, and nothing in it became false — if anything the window is now reliably true rather than true absent a clock step.

## Tests

| Test | What it verifies |
|---|---|
| `ConnectionKeepAliveTest.TheSweepSparesAPingedConnectionAndReclaimsAnUnpingedOne` (new) | The rewritten sweep comparison: at a 10 s threshold, a connection a `LocalServer` pings every second is spared by the very sweep that reclaims an unpinged control connection. This is the regression test for the way the change could go wrong — an inverted or mis-scaled comparison, whose symptom is this bug's own symptom, a live client reclaimed. |
| `ConnectionKeepAliveTest.WaitForAPingToLand` (converted helper, three call sites) | Unchanged coverage: that a ping reaches a connection with no session open, one whose last session was disconnected, and one whose previous ping failed. The old helper waited for an instant to increase; the new one waits for the idle time to *decrease* between two consecutive reads, since only a ping shortens it. The fixture keeps its one-second keep-alive interval, which is why `IdleTime` is a `TimeSpan` rather than whole seconds. |

Both arms of the new case were shown to be load-bearing, not just green:

- Make the sweep relinquish unconditionally and the case fails on the pinged client — observed, with a `NullReferenceException` out of `LocalProcess.Evaluate`. That is why the assertion catches and names the exception rather than letting it propagate as a stack trace.
- Remove the unpinged control connection and the case would pass on a watcher that never swept at all; the control's own wait fails the case with that stated in its message if it is never reclaimed.

**No test for arm 2**, and the implement ticket's reasoning for that stands: reaching the deadline needs a process whose thread will not stop when aborted, which this suite has no way to produce, and the plan already records that path as verified by inspection. The change is a substitution of the time source with the budget and comparison unchanged.

**No test for the clock step itself.** It cannot be produced in process — re-confirming what the implement ticket measured, the `TZ` environment variable plus `TimeZoneInfo.ClearCachedData()` does not move `TimeZoneInfo.Local` on Windows, and moving the machine clock is out of bounds for a test. After the fix the code reads a source nothing in the process can step, so this absence is structural. The source-grep guard the implement ticket considered and declined was not added, for the reasons recorded there: it would need a maintained allowlist of the legitimate wall-clock uses, the D4 `Now()` and `Today()` operators above all, and there are no decision-grade instances left for it to catch. **That is the judgment call most worth a second opinion in this ticket.**

## Validation run

- `dotnet build Dataphor.slnx`: **0 errors, 143 warnings** — the tree's standing count, unchanged. No `NoWarn` and no `pragma warning disable` added.
- Targeted, all green: `ConnectionKeepAliveTest` (4 cases), `AbandonedSessionCleanupTest` (2), `AbandonedClientProcessTest` (1), `NativeSessionKeepAliveTest` (2), `NativeSessionCleanupTest` (2), `ServerLinkConnectionTest` (7).
- The three suite parts from the root `AGENTS.md`, each in the foreground: part 1 **673 passed, 0 failed, 33 skipped** (7 m 21 s), part 2 **67 passed, 0 failed, 11 skipped** (3 m 4 s), part 3 **50 passed, 0 failed, 12 skipped** (3 m 4 s). The skips are the coverage manifest's `known-failure`, `fragment` and `unstable` entries plus the three sample-corpus Inconclusive cases.
- Nothing written to `tickets/.pre-existing-error.md`: no failure surfaced in any run.
- The new case takes about 13 s, which is the price of its flake margin (recorded in a remark on `SweepThresholdSeconds`). Part 1 absorbs it within the run-to-run spread the root `AGENTS.md` records for that part.

## What a reviewer should look at

**The flake margin on the new case.** `SweepThresholdSeconds` is 10 against a one-second keep-alive, so the watcher wakes every 2.5 s and the client has to miss ten pings in a row before the sweep can see it as idle. Three intervals was the obvious number and was rejected: on a loaded machine one late ping would fail the case. Ten was chosen on that argument, not by watching the case fail at smaller values. If 13 s of suite time is too much for that margin, the thing to trade is the threshold, not the keep-alive interval — the fixture's other three cases depend on a one-second ping.

**`Volatile` in `Ping` and `IdleTime` claims only ordering.** The old code wrote and read a `DateTime` from different threads with no synchronization at all. On the 64-bit runtime this product runs on an aligned 8-byte access is already atomic, so the `Volatile` pair is an ordering and intent improvement, not a fix for an observed defect — and the comment says exactly that rather than claiming more. The connection's `_syncHandle` is deliberately not taken: `BeginCall` holds it for the whole of `Connect` and `Disconnect`, so a sweep that took it would serialize itself against clients' in-flight calls.

**The liveness probe in `WaitForTheControlToBeReclaimed` is `ExecuteScript("begin end;")`** — an empty D4 block, chosen so that polling a still-live connection changes nothing and can be repeated. Worth a glance that nothing in the engine treats an empty block specially. The case passes and the control is reclaimed at about 12 s, which is what the sweep arithmetic predicts.

**The wider class was surveyed and deliberately left alone.** Every `DateTime.Now` and `DateTime.UtcNow` in `src/` (45 hits) was read when the ticket was written: three decision-grade sites, of which these two were the remainder and `NativeSession` was already monotonic; nine reported-duration sites where a clock step makes one printed number wrong and nothing else (`ScriptExecutionUtility`, `ServerPlan`, `ServerProcess`, `D4Editor`, `TestNodes`, and the `System.Diagnostics.Benchmark*` D4 operators in `SystemNodes`); and the correctly-wall-clock surfaces that must not change (`DateTimeNodes`' `Now()` and `Today()`, `ServerTransaction.StartTime`, log timestamps). The enumeration lives in the implement ticket and is archived with it rather than scattered as `NOTE:` comments. If that is the wrong call, the cheap fix is one bullet in `docs/architecture.md`, not nine `Stopwatch`es across five assemblies.

**No shared monotonic primitive was extracted**, though both arms and `NativeSession` would use one. `native-cli-session-liveness-clock`'s review set the condition explicitly: extract on the third site. This is the second, three lines, in a different assembly from the others, and a shared primitive would mean a new public-ish type in `src/engine/Base` or `src/engine/DAE` introduced by a bug fix. A fourth site should trigger the extraction.

## Use cases to exercise

- An ordinary WinForms client or Dataphoria session held idle across a sweep: it pings every two minutes against a 300-second threshold, so it is spared. Nothing about the shipped numbers moved.
- A client that stops pinging — crashed process, pulled cable, powered-off workstation: still relinquished between 300 and 375 seconds after its last ping, its sessions closed and its transactions rolled back (`AbandonedSessionCleanupTest`, `AbandonedClientProcessTest`).
- A server running across a daylight-saving transition, or one whose clock an operator corrects by more than five minutes: before this change the first sweep after the step relinquished every connection at once; now nothing happens. **Not reproducible in the suite** — it needs the machine clock to move — so if anyone has a machine whose clock they can step, this is the one thing worth watching by hand.
- An operator reading the server log after a reclamation: the line now says `IdleSeconds=412.3` where it said `LastActivity=2:47 PM`. The log line is already timestamped by the logger, so the absolute last-ping instant is still recoverable by subtraction.
- A custom C# extension or host that read `RemoteServerConnection.LastActivityTime`: it no longer compiles, and reads `IdleTime` instead. This is the only compatibility surface this ticket moves, it is source-level only — not on `IRemoteServerConnection`, not on any WCF contract, not reached from D4 or any form surface — and the migration note carries it.
