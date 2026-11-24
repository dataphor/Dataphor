description: Stopping a task right after it has finished running a script in the background intermittently fails with a bogus "a nil was encountered" error, because the engine tears the task down while a background thread is still cleaning up after it. Make shutting a task down wait for that cleanup, and cover it with a test.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerScript.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/, tests/DAE.Server.Tests/coverage-manifest.json, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What is wrong

`StopProcess(<id>)` disposes the process while another thread may still be using it. The engine has a latch for exactly this - `ServerProcess._syncHandle`, taken by `BeginCall` and released by `EndCall`, so that only one call originating in the CLI runs through a process at a time - and `ServerProcess.Dispose` (`src/engine/DAE/Server/ServerProcess.cs:84`) is the one entry point that never takes it. Disposal is therefore not serialized against calls, and two threads can run down the same process's object graph at once.

The path that makes it happen in practice is asynchronous execution. `ExecuteAsync(<processID>, <script>)` runs the script on a worker thread, and `SystemExecuteAsyncNode.ExecuteResults` (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs:1443`) then cleans up on that same worker thread, *after* the script's own execution window has closed: it calls `process.UnprepareScript(script)`, which disposes the `ServerScript`. Between the script's last `EndCall` and that cleanup's `BeginCall` the process's `ExecutingThread` is null, and `ExecutingThread` is the only thing `Engine.TerminateProcessThread` (`src/engine/DAE/Server/Engine.cs:483`) waits for. So a `StopProcess` that lands in that window sees an idle process, returns at once, and `ServerSession.StopProcess` → `ServerProcess.Dispose` runs beside the worker's cleanup.

Both then dispose the same `ServerScript`, and the teardown loops are written as check-then-act over a collection the other thread is emptying:

```
while (_batches.Count > 0)
    _batches.DisownAt(0).Dispose();     // ServerScript.UnprepareBatches, ServerScript.cs:46
```

`ServerChildObjects` locks each operation but not the pair, so the count can be read as non-zero and the slot be empty by the time `DisownAt(0)` runs. `InternalRemoveAt` then calls `Removing(null, index)`, which dereferences the item (`ServerObject.cs:354`) and throws `NullReferenceException`; it also decrements `_count` below zero. `ServerProcess.UnprepareScripts` (`ServerProcess.cs:409`) has the same shape over `_scripts`, and the second observed variant is an outright null `_batches`, read after the other thread's `Dispose` finished and nulled the field.

The exception travels back out through `ServerSession.StopProcess` unchanged, and `PlanNode.Execute` turns any `NullReferenceException` into `RuntimeException.Codes.NilEncountered`. That is why a race in process teardown surfaces to a D4 author as *"A nil was encountered where a value was expected"* against the `StopProcess` statement - the statement's argument was fine; the message is simply what the engine says about any null dereference under a node.

Nothing is wrong with `DebugStepping.d4`, and nothing is wrong with the debugger. The script hits this because each of its cases ends with `Debug.Run()` and then `StopProcess` in a `finally`: `Debug.Run` releases the held worker at the exact moment the script thread goes to stop the process, which is the best possible alignment for the window. The debugger is a magnifier, not the cause.

## Evidence

Measured 2026-11-23 in Debug on one slow development laptop.

- `DebugStepping` on its own, `dotnet test tests/DAE.Server.Tests --no-build --filter Name=DebugStepping`: 4 failures in 128 runs, about 3 per cent - 2 of 20, then 1 of 5, 1 of 10, and three further rounds of 18, 20 and 25 runs with none, plus 30 runs as 3 concurrent test hosts with none. The failures landed on three different `StopProcess` statements (script lines 259, 288 and 404), each the `finally` of a case that had just called `Debug.Run()`; line 288 is the one CI reported.
- A debugger-free reproduction fails far more often and in a twelfth of the time. One D4 statement, executed 400 times from a temporary NUnit fixture over `InProcessTestFixture`:

  ```
  begin var LProcessID := StartProcess(); try ExecuteAsync(LProcessID, 'Sleep(20);'); Sleep(20); finally StopProcess(LProcessID); end; end;
  ```

  The two sleeps are what align the worker's cleanup with the stop. Result: 6 failures of 400 and 15 failures of 400 on two runs, about 12 s each, every one of them the same `NilEncountered` over `NullReferenceException`. This is the proof that the bug is in process teardown and not in the debugger.
- Both NRE sites were confirmed by temporarily logging the caught exception inside the two teardown loops - necessary because `UnprepareScripts`, `UnprepareBatches` and `ServerSession.StopProcess` all rethrow with `throw <saved exception>`, which resets the stack trace and hides the real site. See the optional arm below.

## The fix

Take the latch in disposal: wrap the body of `ServerProcess.Dispose(bool disposing)` in `lock (_syncHandle)`. Disposal then becomes just another call on the process, serialized against `BeginCall`/`EndCall` like every other, and the worker's cleanup either finishes before the dispose starts or runs after it and fails harmlessly on the worker (where `ExecuteResults` already swallows everything, and where the process has by then disposed the script itself).

Verified: with that one change, the 400-iteration reproduction above passed three times in a row with no failures, and nothing reached `ServerSession.StopProcess`'s catch at all. `DebugStepping`, `DebugBreakpoints`, `DebugLibrary`, `AsyncExecution`, `TestProcessExecutes` and all six `DebuggerSessionTest` cases passed with it in place.

Why this and not the alternatives:

- *Make `TerminateProcessThread` also wait for a pending async worker* (a counter on `ServerProcess` raised before `AsyncExecution.Start` and lowered after the worker's cleanup) would fix this path too, but it adds new state and only covers the async path; the latch covers any concurrent call on a process being disposed.
- *Make the teardown loops concurrency-safe* (an atomic "disown the first item or report empty" on `ServerChildObjects`) treats the crash site rather than the race, and would leave two threads running down the same object graph - quieter, not correct. See the tripwire task below.

Known cost of the latch, worth stating in the commit: `Dispose` can now wait for an in-flight call on that process. `TerminateProcessThread` runs first and aborts the executing thread, so the normal case is the few milliseconds of the worker's cleanup. The one unbounded-looking case is bounded too: a thread parked inside `BeginCall` → `Engine.BeginProcessCall` waiting on the concurrent-process throttle holds the latch while the process's executing-thread count is still zero, so `TerminateProcessThread` returns at once and `Dispose` waits out at most `Engine.ProcessWaitTimeout` (default 30 s) before proceeding. Today, in that same situation, it corrupts the collection instead.

## The test

A D4 coverage script is the right layer: the reproduction is one D4 statement in a loop, it needs no C# to express, and the coverage harness already runs documents in-process. Add a new document rather than extending `AsyncExecution.d4`, so that its wall-clock cost shows on its own manifest line and a failure in it does not hide the rest of asynchronous execution.

It is a race, so size the loop by measurement, not by taste: pick an iteration count and sleep that make the *unfixed* engine fail on five consecutive runs, then keep the smallest such size. The 400 × 20 ms shape above failed at 6 and 15 occurrences per run, so the margin is there; a smaller, faster shape may well do. Record the measured pre-fix failure count and the run time in a comment in the script, the way `DebugStepping.d4` records its own timing assumptions, so that a later reader knows what the loop is for and does not shrink it.

## TODO

- Reproduce first, on an unchanged tree, with a temporary fixture or script built from the statement above; note the failure count. Do not start from the assumption that it reproduces - the window is machine-dependent, and the sizing of the test depends on what you measure.
- Add `lock (_syncHandle)` around the body of `ServerProcess.Dispose(bool disposing)`, with a comment naming what it serializes against and why `TerminateProcessThread`'s executing-thread test is not enough on its own (the async worker's post-execution cleanup).
- Add the coverage script - suggested name `ProcessStopDuringAsync.d4` in `tests/libraries/TestFramework.Coverage.Scripts/Documents/` - and its `coverage-manifest.json` entry, with `timeoutSeconds` and a note that states the measured cost and what the loop is sizing for. Check whether it needs `SetUseImplicitTransactions(false)`: it reads no catalog table, so it most likely does not.
- Confirm the script fails without the engine change and passes with it, five consecutive runs each way.
- Add a `docs/migration-notes.md` entry in the same commit: an application that stops a task straight after running something on it asynchronously no longer sees a spurious nil error, and disposing a task now waits for a call in flight on it.
- Record the teardown loops' check-then-act as a tripwire, not as a ticket: a `NOTE:` at `ServerProcess.UnprepareScripts` saying that the loop is safe only because disposal now holds the process's call latch, and that if another concurrent-disposal path is ever found, the fix is an atomic disown-first primitive on `ServerChildObjects` rather than a second lock. Add one line to the handoff saying where you parked it. If you do find a second reachable path while you are in there, that is a real latent defect and gets its own ticket.
- Optional, only if it stays small: `ServerProcess.UnprepareScripts`, `ServerScript.UnprepareBatches` and `ServerSession.StopProcess` all rethrow a caught exception with plain `throw <variable>`, which resets its stack trace; diagnosing this bug needed three extra reproduction rounds because of it. Saving an `ExceptionDispatchInfo` instead and rethrowing through it preserves the original site and changes nothing an application can observe - same object, same type, same message. Leave it alone if it turns into a sweep of the engine.

## Tests to run

Build first (`dotnet build Dataphor.slnx`), then the targeted set: `dotnet test tests/DAE.Server.Tests --no-build --filter "Name=ProcessStopDuringAsync|Name=DebugStepping|Name=DebugBreakpoints|Name=DebugLibrary|Name=AsyncExecution|Name=TestProcessExecutes|FullyQualifiedName~DebuggerSessionTest" --logger "console;verbosity=normal"`. That is the new case, every coverage script that uses `ExecuteAsync`, and the debugger fixture - the tests that cover the files the change touches. It ran in about 35 s. Name the filter you used in the handoff.
