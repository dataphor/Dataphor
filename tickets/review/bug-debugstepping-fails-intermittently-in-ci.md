description: Stopping a task right after it had run a script in the background intermittently failed with a bogus "a nil was encountered" error, because the engine tore the task down while a background thread was still cleaning up after it. Shutting a task down now waits for that cleanup, and a new test reproduces the old failure.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/ProcessStopDuringAsync.d4, tests/DAE.Server.Tests/coverage-manifest.json, docs/migration-notes.md
difficulty: medium
----
## What landed

Three changes and one tripwire. The whole diff is 25 added lines across three tracked files, plus one new coverage script.

**The fix — `src/engine/DAE/Server/ServerProcess.cs:84`.** `ServerProcess.Dispose(bool)` was the one entry point into a process that never took the process's call latch (`_syncHandle`, taken by `BeginCall` and released by `EndCall` so that only one call originating in the CLI runs through a process at a time). Disposal now takes it: the old body moved verbatim into a new `private void InternalDispose(bool disposing)`, and `Dispose` became `lock (_syncHandle) InternalDispose(disposing);` with a comment naming what it serializes against and why `TerminateProcessThread`'s executing-thread test is not enough on its own. Nothing else in the method changed — the diff on the body is zero lines, which is why the method was extracted rather than re-indented.

**The test — `tests/libraries/TestFramework.Coverage.Scripts/Documents/ProcessStopDuringAsync.d4`**, a new D4 coverage script, plus its `coverage-manifest.json` entry (`pass`, `timeoutSeconds` 60, no preamble — it reads no catalog table, so the harness's implicit transaction is harmless). 200 iterations of `StartProcess` / `ExecuteAsync(<id>, 'Sleep(20);')` / `Sleep(20)` / `StopProcess` in a `finally`. The two sleeps line the worker's post-execution cleanup up with the stop.

**The migration note — `docs/migration-notes.md`**, one row at the end of *D4, catalog and libraries*: the spurious nil is gone, and disposing a process now waits for a call in flight on it.

**The tripwire — a `NOTE:` at `ServerProcess.UnprepareScripts` (`ServerProcess.cs:424`).** The three teardown loops (`UnprepareScripts`, `UnpreparePlans`, `ServerScript.UnprepareBatches`) are check-then-act over collections that `ServerChildObjects` locks per operation but not per pair; they are safe only because each is reached only from a `Dispose` that now runs under the latch. The note says that, and says that if another concurrent-disposal path is ever found the fix is an atomic "disown the first item or report empty" primitive on `ServerChildObjects`, not a second lock at the crash site. I checked the callers: `UnprepareScripts` and `UnpreparePlans` are called only from `InternalDispose` (lines 214 and 230), `UnprepareBatches` only from `ServerScript.Dispose`. No second reachable path, so no ticket.

## Why a nil

Worth keeping in the reviewer's head, because the symptom names the wrong thing. `PlanNode.Execute` turns any `NullReferenceException` into `RuntimeException.Codes.NilEncountered`, so a null dereference anywhere under a statement reaches a D4 author as *"A nil was encountered where a value was expected"* against that statement. The `StopProcess(<id>)` argument was never the problem, and neither `DebugStepping.d4` nor the debugger was: the debugger is a magnifier, because each of that script's cases ends with `Debug.Run()` releasing the held worker at the moment the script thread goes to stop the process.

## Measurements (all 2026-11-23, Debug, one slow development laptop)

Sizing the loop was the work, so here is the evidence a reviewer would otherwise have to re-derive.

- **Fails without the fix, 5 of 5 runs.** At 400 iterations and at 200, every run failed with `RuntimeException: A nil was encountered ... <= NullReferenceException` at the script's `StopProcess` line, each within 2-3 s of case time (the loop aborts at its first failure).
- **Failure rate.** A temporary probe that counted failures instead of raising saw **15 and 29 failures over 400 iterations, the first at iteration 22 and at 65** — about one iteration in 20. 200 iterations therefore carry a wide margin, which is why I kept 200 rather than the 400 the fix ticket measured with: half the cost, and still roughly 7 expected failures per run on this machine. The probe is gone; its numbers are recorded in the script's header comment and in the manifest note.
- **Passes with the fix, 5 of 5 runs**, about 8 s of case time each (the pass case runs the whole loop; a failing case does not).
- **Cost to the suite:** about 8 s added to the coverage scripts.

## Tests run

Built first (`dotnet build Dataphor.slnx`, 0 errors). Then the ticket's targeted filter, with `Transaction.Isolation.Run` added because the garden report named it as a second script hitting the same race:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=ProcessStopDuringAsync|Name=DebugStepping|Name=DebugBreakpoints|Name=DebugLibrary|Name=AsyncExecution|Name=TestProcessExecutes|Name=Transaction.Isolation.Run|FullyQualifiedName~DebuggerSessionTest" --logger "console;verbosity=normal"
```

13 cases, 12 passed, 1 skipped, 49 s. `AsyncExecution`, `DebugBreakpoints`, `DebugLibrary`, `DebugStepping`, `ProcessStopDuringAsync`, `TestProcessExecutes` and all six `DebuggerSessionTest` cases passed. **`Transaction.Isolation.Run` matched its recorded known failure again** — the `LostUpdate` assertion owned by `debt-memory-device-transaction-isolation`, not the nil CI saw at `b919d8cd`. That is the garden report's second symptom resolved, and the reason to keep this filter in the review pass.

Second, because the change is in process teardown and a new lock there would show up as a hang rather than a failure, the process- and session-lifecycle fixtures:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~AbandonedClientProcessTest|FullyQualifiedName~AbandonedSessionCleanupTest|FullyQualifiedName~NativeSessionCleanupTest|FullyQualifiedName~MemoryDeviceConcurrencyTest|FullyQualifiedName~ConcurrentDDLTest|FullyQualifiedName~TransactionBeginTest|FullyQualifiedName~DeferredDeviceStopTest" --logger "console;verbosity=normal"
```

20 cases, all passed, 1.8 minutes — including `ConcurrentSessionsStoreTheirOwnScripts` (16 sessions x 15 DDL cycles, 41 s) and `ASessionWithACallInFlightIsNotReclaimed`, the fixture closest to the new wait.

No pre-existing failures surfaced, so `tickets/.pre-existing-error.md` was not written.

## What to look at, honestly

- **The new wait is the only behaviour risk, and it is a lock held across a long body.** `Dispose` now holds `_syncHandle` for its whole teardown — rolling back transactions, closing device sessions, closing *remote* sessions (network calls to linked servers), stopping the process program. Anything that blocks in there now also blocks a thread calling into that process. I reasoned through the one shape that could become a cycle — two processes each running `StopProcess` on the other — and it cannot reach the lock: `Engine.TerminateProcess` calls `TerminateProcessThread` first, and that *throws* `ProcessNotResponding` when its budget (`ProcessTerminationTimeout / 2`) expires rather than returning, so neither thread ever gets to `Dispose`. That is reading, not a test. If the reviewer wants more than reading there, the cheapest probe is two D4 processes stopping each other and asserting both get `ProcessNotResponding`; I did not add it, because it pins engine timeout behaviour rather than this fix.
- **The loop sizing is machine-dependent by nature.** One iteration in 20 is this laptop; a machine where the window is narrower needs more iterations to detect a regression. The script and the manifest both say so, and both say not to shrink it without remeasuring against an engine whose `Dispose` does not take the latch. Worth deciding whether that is protection enough, and whether the manifest wants this as `unstable` rather than `pass` — I chose `pass`, because it passed 5 of 5 and a case that cannot fail the run is not a guard.
- **I declined the fix ticket's optional arm.** It proposed replacing `throw <saved exception>` with `ExceptionDispatchInfo` in `UnprepareScripts`, `UnprepareBatches` and `ServerSession.StopProcess`, so that the original stack survives — diagnosing this bug needed three extra reproduction rounds because it does not. The ticket said to leave it if it became a sweep, and it is one: `grep -rn "throw exception;" src/engine/` finds **18 sites in 10 files** (`ServerProcess.cs` 3, `Compiler.cs` 3, `Parser.cs` 2, `ServerTransaction.cs` 2, `ApplicationTransaction.cs` 2, and `BaseList`, `DisposableList`, `SQLConnection`, `Device`, `PlanNode`, `ServerScript` one each). `ServerSession.StopProcess` is a different and much larger pattern — `throw WrapException(E)`, where `WrapException` logs and returns the same object, used throughout the server. Both are real diagnosability debt and either is a `debt-` ticket of its own if anyone wants it; neither belongs in this fix.
- **One thing the fix does not do.** A `StopProcess` that now arrives while the worker's cleanup holds the latch waits, and in the other order the worker's `BeginCall` runs after `Dispose` nulled `_serverSession` and throws a `NullReferenceException` inside `BeginCall`. That is harmless and was always the design: `SystemExecuteAsyncNode.ExecuteResults` swallows everything on the worker, and the process has by then disposed the script itself. It needs a reviewer's eye only to confirm that "harmless" is the right word for it.
