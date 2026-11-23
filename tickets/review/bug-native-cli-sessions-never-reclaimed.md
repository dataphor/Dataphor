description: A program that connects to the server through its simplified call interface and then disappears no longer holds its work open for ever: the server now ends a session nobody has called for five minutes and rolls back its transaction, the way it already cleans up an abandoned client connection.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/NativeCLI/NativeServer.cs, src/engine/Server/Service/NativeSessionManager.cs, src/engine/Server/Service/ConnectionManager.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, src/engine/Server/Server.csproj, src/engine/Server.Hosting/DataphorServiceHost.cs, tests/DAE.Server.Tests/NativeSessionCleanupTest.cs, tests/DAE.Server.Tests/NativeSessionKeepAliveTest.cs, tests/DAE.Server.Tests/HostedInstance.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md, docs/architecture.md, docs/migration-notes.md
repro: verified
----
## What landed

The native CLI - the server's simplified call interface - hands out *sessions* rather than connections, and nothing watched them. A client that started a session, opened a transaction, changed the schema and then died left that transaction open for the life of the server, holding the catalog store's single write lock against every other session's schema change, each of which then failed with error 130103 after its lock wait. This adds the sweep that ends such a session, using the clock and the in-use count `native-cli-session-liveness-clock` put on each session.

**`NativeServer.ReclaimIdleSessions(int idleSeconds)`** - new, public, returns how many sessions it stopped. Two steps, and the split is the substance:

- *Under `lock (_nativeSessions)`*: walk the sessions, select those whose `IdleSeconds` exceeds the threshold (that property reports 0 while a call is in flight, so one comparison applies both conditions), collect them, then remove each from the dictionary. Deciding and removing is one atomic step so that a session the sweep has chosen cannot take a call in between. This is where the native interface beats the main one: every operation goes through one `AcquireNativeSession`, so the count of calls in flight is there to read, where for a connection it is not.
- *Outside the lock*: for each removed session, read its open-transaction count for the log, call the existing `StopNativeSession` in its own `try` (a failure is captured, not thrown, so one session that will not stop cannot leave the rest removed-and-never-stopped), then write one log line. Outside the lock because stopping a session's process can wait half of `Engine.ProcessTerminationTimeout` - 15 s by default - and every operation of `NativeServer` takes that lock. `StopNativeSession` is what rolls the transaction back: it disposes the `ServerProcess`, which rolls back its open transactions and releases the store's write lock. No new rollback machinery.

**`src/engine/Server/Service/NativeSessionManager.cs`** - new public class beside `ConnectionManager`, with that class's shape copied: `IdleTimeInSeconds` of 300, `WatcherStopTimeoutInSeconds` of 10, two constructors (the second taking the threshold, refusing below 1 second), one background watcher thread, a `ManualResetEvent`, a wake period of a quarter of the threshold with a floor of a second, an idempotent `Dispose` with a bounded join, and guarded logging. A `NOTE:` at the top records that the thread shape is copied deliberately rather than extracted, and that a third such watcher is the condition for extracting one.

**`DataphorServiceHost.NativeSessionIdleTimeInSeconds`** - new host-object property, defaulting to `NativeSessionManager.IdleTimeInSeconds`, built into the manager in `Start` beside the native service and disposed in `Stop` before the `RemoteServer` and before the engine stops.

**No new error, no new error code.** A client returning to a reclaimed session gets the "Invalid session handle" `ArgumentException` the dictionary lookup already raises, wrapped by `NativeCLIUtility.WrapException`. The server log is the operator-visible record: one line per reclamation naming the session ID, user, client host name, idle seconds and open-transaction count.

**Also corrected along the way, because the change made them false:** the `docs/architecture.md` sentence saying native sessions were covered by no watcher, and the remark on `RemoteSessionImplementation.Dispose` saying "What reclaims it, as of this version: nothing". Neither was named in the ticket.

## How to exercise it

**In the suite** (all seven of these pass on the final binary; the whole suite is green in its three parts, 0 failures):

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased|Name=ASessionWithACallInFlightIsNotReclaimed" --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=TheSessionsClockMovesWhileTheClientHoldsIt|Name=ASessionTheClientKeepsAliveIsNotReclaimed|Name=ASessionWithNoKeepAliveIsReclaimed" --logger "console;verbosity=normal"
```

**By hand, against a real server**, which is the use case the design rests on: start an instance, connect a `NativeCLISession` with `keepAliveIntervalSeconds: 0`, `BeginTransaction`, `Execute` a `create table`, then stop calling. Every other session's `create table` fails with 130103 until the sweep runs, and succeeds after it; the server log then holds the reclamation line. With the shipped seven-argument constructor instead (two-minute ping) the session survives indefinitely. A host that does not want to wait five minutes sets `DataphorServiceHost.NativeSessionIdleTimeInSeconds` before `Start`.

**Tests added**, and what each verifies:

| Test | What it pins |
|---|---|
| `NativeSessionCleanupTest.AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased` | The reproduction, in process over a `NativeServer` plus a `NativeSessionManager` at a one-second threshold: the write lock is held before the watcher exists (so the case cannot pass vacuously), another session's schema change then succeeds, the abandoned table is gone rather than committed, the post-reclamation work is real, and a call on the reclaimed handle fails. |
| `NativeSessionCleanupTest.ASessionWithACallInFlightIsNotReclaimed` | The branch easiest to get wrong: a 5-second `Sleep` on the session from a background task survives several sweeps at a one-second threshold, and a call on the same handle afterwards still succeeds. |
| `NativeSessionKeepAliveTest.ASessionTheClientKeepsAliveIsNotReclaimed` | The compatibility claim, over a real loopback endpoint at a three-second threshold: a session the client pings every second survives nine seconds in which the test makes no call. This is what says a server link survives an idle user session. |
| `NativeSessionKeepAliveTest.ASessionWithNoKeepAliveIsReclaimed` | That the case above is not passing for the wrong reason - the same setup with the keep-alive off, and the next call fails. |

**Both negative directions were measured, not assumed.**

- With the manager's construction commented out, `AnAbandonedNativeTransactionIsRolledBackAndTheCatalogWriteLockReleased` fails as designed: the write lock is reported held after 1212 ms and the catalog change keeps failing with 130103 for the case's whole 31.1-second wait. With the watcher, the catalog change succeeds 1.1 s after it starts.
- With `NativeSession.IdleSeconds`'s `if (_inUseCount > 0) return 0;` guard removed, `ASessionWithACallInFlightIsNotReclaimed` fails - so the second case is load-bearing rather than decoration. Both temporary edits were reverted and the final binary re-run.

## What is **not** covered by a test, and other things to weigh

Flagged rather than papered over; the reviewer should treat the list as the agenda.

- **The log line is only partly exercised.** The suite runs with logging off (`TestEnvironment.LoggingEnabled => KeepFiles`), so the `String.Format` at the call site runs in the tests - a null field there would surface - but nothing asserts the line's content or that it reaches a log file. By inspection only.
- **`ReclaimIdleSessions` is `public` with no caller outside the assembly.** The ticket specified it; the remark says plainly that `NativeSessionManager` is in this assembly and would not need it to be, and points at the `RemoteServer.GetCurrentConnections`/`Relinquish` precedent and the absent `InternalsVisibleTo` convention. A reviewer may reasonably want it `internal`; that would also drop it from the public surface the migration note describes.
- **Its `idleSeconds < 1` guard is beyond what the ticket asked for.** It is what makes relying on `IdleSeconds` reporting 0 for a busy session sound at this method's boundary rather than only at the manager's. Cheap, but it is a second copy of the manager's check.
- **`TransactionCountOf` is defensive beyond the facts.** Every session in the dictionary has a non-null `Process`, and `ServerProcess.TransactionCount` is a field read, so neither branch of its guard is reachable today; it returns the string `"unknown"` rather than a number when it cannot read. It exists because a throw there would skip the stop that follows it, which is exactly the defect being fixed. A reviewer may judge it unnecessary.
- **The log line is written *after* the stop, where `ConnectionManager` writes its *before*.** Deliberate divergence: by that point the session is out of the table, so nothing else will ever stop it, and a logging failure - the engine closes its log when it stops, which a sweep can overlap - must not be able to skip the stop. The cost is that a sweep whose stop failed logs the reclamation anyway, after the error.
- **Three helpers are duplicated from `AbandonedSessionCleanupTest`** (`AssertTheCatalogWriteLockIsHeld`, `WaitForACatalogChangeToSucceed`, `FindError`), with a `NOTE:` at the fixture's head saying so and naming the condition for extracting them. Each is a few lines and each carries a failure message naming what *this* fixture drives; extracting them would mean touching a fixture `port-abandoned-session-cleanup` has only just proven.
- **A freshly-proven test was touched.** `NativeSessionKeepAliveTest`'s existing case now builds its `NativeCLISession` through a new `NewSession` helper rather than an inline eight-argument construction, so that three cases do not repeat it. Behaviour is identical and the case was re-run (it passes, with the same 2.0 s / 1.1 s clock readings as before).
- **The two keep-alive cases are wall-clock judgments.** At a three-second threshold with a one-second ping, `ASessionTheClientKeepsAliveIsNotReclaimed` needs three consecutive pings to be starved before it flakes; `ASessionWithNoKeepAliveIsReclaimed` waits nine seconds for a reclamation the watcher performs between three and four. Polling is not available to either - any call a case could poll with would itself stamp the clock. Measured runs: 12-13 s each.
- **`ReclaimIdleSessions` returns a count the watcher ignores.** The ticket specified the signature; nothing reads the return value.
- **Verified by reading, not by test** (each is named in the plan's new text): a session acquired between the decision and the stop is impossible because the removal is inside the lock; a brand-new session is never swept before its first call because its clock is stamped in its constructor; a client `StopSession` racing the sweep leaves `NativeCLISession.Dispose` propagating and `RemoteSessionImplementation.Dispose` logging a warning and releasing the local side (both read); a session whose process will not stop delays the rest of its sweep; a reclamation overlapping server shutdown is covered by the dispose ordering and the guarded logging; the stateless `Execute(NativeSessionInfo, ...)` overloads cannot be swept because their sessions never enter the table.
- **The deliberate cost**, in `docs/migration-notes.md`: a client that speaks the WCF contract directly and cannot ping now loses a session it holds without calling for five minutes, as does a 3.1 client. The remedy is the host property or a call within the threshold.

## Documentation

- `docs/modernization-plan.md`, WS2 *Reclaiming an abandoned session*: the "door still open" paragraph is replaced by what landed, a **Decisions for the native interface** block with the four decisions and their reasons, what rests on the tests versus inspection, and the one cost.
- `docs/architecture.md`: the catalog write-lock paragraph now describes the native watcher instead of saying there is none.
- `docs/migration-notes.md`, *Custom C# extensions*: one row beside the prereq's, with the new public surface (`NativeSessionManager`, `NativeServer.ReclaimIdleSessions(int)`, `DataphorServiceHost.NativeSessionIdleTimeInSeconds`), the behaviour change, and what to do.

## Validation run

- `dotnet build Dataphor.slnx`: 0 errors, 143 warnings - the count the plan records for the tree before this change, so no new warning.
- Suite in its three parts, each in the foreground: part 1 `Failed: 0, Passed: 672, Skipped: 33` (7 m 9 s); part 2 `Failed: 0, Passed: 67, Skipped: 11` (3 m 2 s); part 3 `Failed: 0, Passed: 50, Skipped: 12` (3 m 3 s). No pre-existing failure surfaced, so `tickets/.pre-existing-error.md` was not written.
