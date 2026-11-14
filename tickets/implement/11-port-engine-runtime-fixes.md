description: Fix engine behaviours that compile on .NET 10 but go wrong at run time (running a script in the background, copying operator source text out of a script, locating the program's own folder), and pin string comparison to the old Windows rules with a test.
prereq: test-harness-inprocess
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Debug/SourceUtility.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Schema/Operator.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/Base.Windows/PathUtility.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugLibrary.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/TestProcessExecutes.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/Transaction.Isolation.Run.d4
difficulty: medium
----
# Engine runtime fixes: K2, K5 (engine sites), K22, dead `using` directives

**Paths.** This ticket runs after `repo-move-to-new-layout`; paths are in the target layout of the plan's [Repository layout](../../docs/modernization-plan.md#repository-layout) section (`src/engine/DAE` is today's `Dataphor/DAE`, `src/engine/Base.Windows` is `Dataphor/Base.Windows`, `tests/libraries/TestFramework.Coverage.Scripts` is `Libraries/TestFramework.Coverage.Scripts`).

## Goal

The engine compiles on .NET 10 almost unchanged, but some code paths behave differently at run time. This ticket fixes the engine-level ones that are already known, each with its reproduction test at the lowest layer that shows it, so that the D4 coverage scripts that exercise them pass in the harness.

## Scope

In scope: K2, K5's three engine sites, K22, and the dead-`using` arm (which turns out to need no edits; see below).

Out of scope, with owners:

- K4 (`ThreadUtility.SetThreadName`, null reference at server start in Debug builds): `build-core-sdk-projects`. This ticket's K5 work runs scripts on other threads, which calls `SetThreadName` from `ServerProcess.BeginCall`; it relies on K4 being fixed.
- K5's IDE site, `Dataphoria/TextEditor/ScriptExecutor.cs`: `feat-ws6-windows-client-and-ide`.
- A further `Delegate.BeginInvoke` in `src/client/DAE.Client/DAEDataSet.cs` (~line 279) is inside `#if USEASYNCSTOPPROCESS`, which nothing defines (`CompilerDefines.txt` lists it as an option) and which refers to fields that no longer exist; dead code, not compiled. Leave it; WS6 owns the client.
- `PathUtility.GetInstallationDirectory`'s assumption about the installation folder structure (it pops `bin` and one more level), which decides the default library directory of new instances: a layout question for `host-instance-configuration` in `feat-ws5-protocol-and-hosting` and for `build-runtime-layout`. Not K22.
- Removing dead `using` directives as a sweep: WS12's rule is "opportunistic, in files otherwise being edited; never as a sweep".

## Settled decisions

- D2: the coverage scripts are the oracle; never edit an expected value in an existing D4 test script to make it pass.
- Operator signatures and semantics of `ExecuteAsync`, `ExecuteWithTimeout` and `ExecuteMultiple` do not change (compatibility surface). Where the old implementation had odd behaviour (swallowed errors, a fixed-length wait), preserve it and mark it with a `NOTE:`; changing it is a D4 semantics change and needs its own ticket.

## K2: `SourceUtility.CopySection` overshoots on single-line sections

Site: `src/engine/DAE/Debug/SourceUtility.cs`. `CopySection(script, lineInfo)` returns the text between (`Line`, `LinePos`) and (`EndLine`, `EndLinePos`), 1-based. It walks `pos` to the start of `Line`, then forward `LinePos - 1` characters to the start column. It then sets `endPos = pos` — the start *column*, not the start of the line — skips `EndLine - Line` newlines and adds `EndLinePos - 1`. When the section spans lines, skipping to the next newline from mid-line lands in the same place as from the line start, so the result is right. When `Line == EndLine`, no newline is skipped and `endPos` ends `LinePos - 1` characters past the true end. The returned substring is too long by the start column; if that runs past the end of the script, `Substring` throws "Index and length must refer to a location within the string", which the compiler reports as an internal error.

Fix: remember the start of `Line` before advancing to the start column, and start `endPos` from there:

```csharp
int lineStart = pos;            // after the loop that skips Line - 1 newlines
… pos += LinePos - 1 …
int endPos = lineStart;
… skip EndLine - Line newlines, then add EndLinePos - 1 …
```

Keep the loops' style; the edit is the one starting point.

Why it matters beyond the exception: the copied sections become `Operator.DeclarationText` and `BodyText` (`Compiler.ProcessSourceContext`, `Compiling/Compiler.cs` ~lines 6362–6385; aggregate operators ~lines 6729–6732), and `Operator.EmitStatement` (`Schema/Operator.cs` ~line 527, aggregates ~line 669) emits `DeclarationText + BodyText` verbatim as the operator's script — the text stored in the catalog and returned by `Script()`. For a one-line operator that is not at the end of the script, the stored script silently carries text that follows the operator, and the declaration text runs into the body. The compiler also re-parses `DeclarationText` (`Compiler.cs` ~line 6622) when an operator is recompiled.

Reproduction (L1, D4 script through the harness): add a coverage script, for example `tests/libraries/TestFramework.Coverage.Scripts/Documents/OperatorSourceText.d4`, and add it to the harness manifest as `pass`. It uses `Execute(…)`, which compiles its argument as a script of its own (`ServerScript` sets the source context to the string), so the case is independent of where the line sits in the file:

- End-of-script case: `Execute('	create operator K2End() : Integer begin result := 1; end;');` — a leading tab, no trailing newline. Before the fix this raises; after it, `K2End()` must return 1.
- Mid-script case: `Execute('	create operator K2Mid() : Integer begin result := 2; end; create table K2After { ID : Integer, key { ID } };');` then assert that `Script("K2Mid")` does not contain `K2After` and does contain `result := 2`, and that `K2Mid()` returns 2. (Check whether `Script` takes a `Name` or a string in the form you use; both overloads exist in `SystemCatalog.d4`.)
- Multi-line case, to pin that the existing path still works: an operator spread over three lines with indentation; `Script` of it contains its whole body and nothing after `end;`.
- Drop what you created at the end so the script can run twice.

Confirm before fixing that the end-of-script case fails, and say in the handoff how.

## K5: `Delegate.BeginInvoke` in the asynchronous execution operators

Sites: `src/engine/DAE/Runtime.Instructions/SystemNodes.cs`

1. `SystemExecuteAsyncNode` (~line 1442): `new ExecuteDelegate(localScript.Execute).BeginInvoke(paramsValue, ExecuteResults, localScript)`. D4 `ExecuteAsync(script)` and `ExecuteAsync(processID, script)`; the first starts a new process and stops it afterwards (`ShouldCleanupProcess`).
2. `SystemExecuteWithTimeoutNode` (~line 1494): `BeginInvoke(paramsValue, ExecuteResults, null)`, then `Thread.Sleep(timeout)`, then throws `ServerException.Codes.ExecutionTimeout` if the callback has not set `_done`; copies out-parameters; `finally` unprepares the script.
3. `SystemExecuteMultipleNode` (~line 1579): starts `number` sessions, each executing the same statement through `BeginInvoke`, then busy-waits `while (!_done);` until a locked callback has seen every one finish; `finally` unprepares and disconnects everything, swallowing errors.

On .NET 10 `BeginInvoke` throws `PlatformNotSupportedException` at the call, so all three operators fail immediately.

What the old code actually did, and what the replacement must preserve:

- **The script runs on another thread, and the calling D4 statement continues at once** (`ExecuteAsync`), or after the wait (`ExecuteWithTimeout`, `ExecuteMultiple`).
- **Errors raised by the script are not returned to the caller.** `EndInvoke` is never called, so an exception in `Execute` was captured by the asynchronous call and dropped. For `ExecuteWithTimeout` this means a script that raises returns normally (no timeout error, out-parameters copied from whatever state they were left in); for `ExecuteMultiple` errors in any session are ignored. Preserve this; put a `NOTE:` at each site saying errors are deliberately not surfaced to the caller and that changing it is a D4 semantics change.
- **"Exceptions surface on the target process"** (the plan's wording): the error happens *inside* the target process's `Execute`, so the engine's own handling in that process still applies — in particular a debugger attached to that process with break-on-exception pauses the process at the error. This is what `DebugLibrary.d4` checks (below). It comes for free as long as the script runs through the same `IServerScript.Execute` / `IServerStatementPlan.Execute` call on its own thread.
- **An unhandled exception must never escape the worker.** With `BeginInvoke` it could not. A raw `Thread` whose delegate throws terminates the whole server process, so if you use a `Thread`, wrap the body in a catch-all. A `Task` captures the exception; an unobserved task exception does not crash the process on .NET 10.
- **`ExecuteAsync` cleanup**: after the script finishes (normally or by exception), unprepare the script and, if `ShouldCleanupProcess`, stop the process — exactly what `ExecuteResults` does, including swallowing its own errors. With a task, do this in a `finally` inside the task body rather than a continuation, so it runs on the worker as before.
- **Cancellation / abort**: there is none in the old code and none to add. Stopping a process that is running an async script is cooperative and already handled by the engine: `Engine.TerminateProcessThread` (`DAE/Server/Engine.cs` ~line 478) sets `IsAborted`, detaches a paused debugger, and waits up to half of `ProcessTerminationTimeout` for `process.ExecutingThread` to go null; the worker notices through `CheckAborted` and leaves `Execute`. `ServerProcess.BeginCall`/`EndCall` (~lines 1734–1790) record the worker as `ExecutingThread`. The replacement needs nothing extra for this, but the worker must be a thread that can block for a long time: a script paused in the debugger holds its thread until it is detached.
- **`ExecuteWithTimeout` waits the full timeout.** The old code always sleeps `timeout` milliseconds, then checks. `Transaction.Isolation.Run.d4` is written around those timings (fixed `Sleep(400)` calls between steps). Keep the fixed-length sleep, then check completion; mark it with a `NOTE:` that returning as soon as the script finishes would be the obvious improvement and is a timing change. On timeout the script keeps running and the `finally` unprepares it while it may still be executing — the old behaviour; keep it and mark it with a `NOTE:` (if this throws differently on .NET 10, that is a finding to file, not to fix here).
- **`ExecuteMultiple` waits for all sessions.** Replace the busy-wait with waiting on the tasks (for example `Task.WaitAll` inside a `try`/`catch` that discards the aggregate exception, to keep the swallow behaviour). The busy-wait reads a non-volatile `bool`, which the JIT may hoist out of the loop, and burns a core while it waits; replacing it is part of the fix.
- **Reentrancy**: `SystemExecuteWithTimeoutNode` keeps `_done` and `_script`, and `SystemExecuteMultipleNode` keeps `_doneList` and `_done`, in instance fields of the plan node, so two executions of the same node (recursion, or a cached plan reused) share them. Move them into locals of the executing method. No behaviour change for a single execution.

Recommended mechanism: `Task.Factory.StartNew(body, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)`. `LongRunning` gives each script a dedicated thread, which suits scripts that may sleep or sit paused in the debugger without starving the thread pool, and the task captures exceptions. `Task.Run` (thread pool, as `BeginInvoke` used) is acceptable if you prefer; say which in the handoff and why.

Thread state differences to check, not to engineer around: `BeginInvoke` ran on a pool thread with the caller's execution context; a task also flows the execution context (culture included, through `AsyncLocal`). Thread names: `BeginCall` names the executing thread through `ThreadUtility.SetThreadName` (K4, fixed in `build-core-sdk-projects`); on .NET 10 a thread's name can be set more than once, so pool reuse is not a problem.

Reproduction (L1, D4 scripts through the harness):

- `DebugLibrary.d4` (`tests/libraries/TestFramework.Coverage.Scripts/Documents/`), the plan's named check: starts a debugger with break-on-exception, attaches it to a new process, runs `ExecuteAsync(LProcessID, 'raise Error("Debug Error");')`, then `StopProcess(LProcessID)` and asserts the process no longer exists — i.e. the async script breaks in the debugger on its own thread and stopping the process detaches and ends it. It fails today only on K5 (plan Appendix A). Move it from `known-failure` to `pass` in the harness manifest.
- `TestProcessExecutes.d4`: `ExecuteWithTimeout(LPID, 'Sleep(100);', 1000)` must not throw; `ExecuteWithTimeout(LPID, 'Sleep(1000);', 100)` must throw. It also requires that the harness session is the only non-system session (`Count(Sessions where User_ID <> "System") <> 1` raises). It is not wired to an NUnit test today; check its manifest entry and flip it to `pass` if it passes. If it fails for a reason other than K5 (for example the single-session precondition under the harness), leave it `known-failure` with that reason and note it for `test-triage-known-failures`.
- `Transaction.Isolation.Run.d4` uses `ExecuteAsync` and `ExecuteWithTimeout` across processes with fixed sleeps; also unwired today. Run it and report the result; flip its manifest entry only if it passes reliably over several runs. It depends on lock timing, so do not chase failures here unless they are clearly K5.
- `ExecuteMultiple` has no coverage script. Add a short one, for example `AsyncExecution.d4`, in the manifest as `pass`: create a table keyed on a `Guid` column, call `ExecuteMultiple('insert row { NewGuid() ID } into <table>;', 3)` (each execution inserts a distinct row, so the sessions cannot conflict on the key), and assert after it returns that the table has 3 rows; then call `ExecuteMultiple` with a statement that raises and assert it returns normally (pins the swallow behaviour). The four-argument form takes a user and password; the harness user is `Admin` with an empty password.

## K22: `PathUtility.GetBinDirectory` and `AppDomain.RelativeSearchPath`

Site: `src/engine/Base.Windows/PathUtility.cs`, `GetBinDirectory()` (~line 164): if `AppDomain.CurrentDomain.RelativeSearchPath` is not null, return `BaseDirectory` combined with its first entry; otherwise return `BaseDirectory`. On .NET Framework `RelativeSearchPath` was non-null only where the host set a private bin path — in this product, ASP.NET (the WebForms web client, removed by D6, whose assemblies lived in `bin\` under the site root). On .NET 10 the property always returns null, so the method already returns `BaseDirectory` on every remaining host. No `app.config` in the tree sets `probing privatePath` for a product executable (checked; only the vendored NUnit runner did).

What the callers need — in every case "the folder that holds the host's own assemblies":

- `Server.GetDefaultLibraryDirectory()` and relative entries in `LibraryDirectory` (`src/engine/Server/Server/Server.cs` ~lines 105, 226): resolve library folders relative to the binaries.
- `PathUtility.GetFullFileName` (system-library file references, `Server.GetFullFileName` ~line 920).
- `LibraryUtility.RegisterLibraryFiles` (`src/engine/Server/Schema/LibraryUtility.cs` ~lines 386, 419): only in the `#if !LOADFROMLIBRARIES` branches; `LOADFROMLIBRARIES` is defined at the top of that file, so these are not compiled.
- `LocalServer` client-side library cache (`src/protocol/Client/Server/LocalServer.cs` ~lines 437, 446): `LocalBin\<server>` under the client's folder.
- Dataphoria's syntax-highlighting files (`Dataphoria.cs` ~line 47).

Fix: make `GetBinDirectory()` return `AppContext.BaseDirectory` and delete the `RelativeSearchPath` branch, so the code states what it does. Observable behaviour on .NET 10 is unchanged. `GetInstallationDirectory()` keeps using the same base directory (it reads `AppDomain.CurrentDomain.BaseDirectory`; switch it to `AppContext.BaseDirectory` too for consistency — the same value).

Test (L0 unit, in the Base or engine unit-test project that `test-harness-inprocess` or `build-core-sdk-projects` set up): `PathUtility.GetFullFileName("x.dll")` equals `Path.Combine(AppContext.BaseDirectory, "x.dll")`, and a rooted name is returned unchanged. This pins the rule the server's library and system-file resolution relies on. There is no reproduction of a failure, because none occurs on .NET 10; say so in the handoff.

## Dead `using` directives

Inventory, checked against the compile lists:

- `using System.Runtime.Remoting.Messaging` in both `SecurityNodes.cs` files: removed by `build-core-sdk-projects` (compile-only).
- `System.Runtime.Remoting*` and `System.EnterpriseServices` in `src/engine/Server/Server/RemoteServer*.cs`, `ServerDTCTransaction.cs`, `Streams/FileStreamProvider.cs`: removed with the remoting and COM+ code by `build-server-project` (K6, K11).
- `using System.Runtime.Remoting.Messaging` in `src/engine/DAE/Runtime.Instructions/GeneratorNodes.cs`: the file is not in the engine's compile list (a stray); `build-globbing-cleanup` deletes strays.
- `src/protocol/Client/Server/ServerFactory.cs` is compiled but its whole content, remoting `using`s included, is inside a block comment. Leave it (stray content for `build-globbing-cleanup`/WS12).
- `System.Web` `using`s in `libraries/Frontend/Source/*.cs`: those files are not compiled; `library-frontend-web-leftovers` deletes them. `libraries/System.Internet/Source/InternetNodes.cs` uses `System.Web.HttpUtility`, which exists on .NET 10 — live, not dead.
- `using System.Security.Permissions;` in 17 engine files (`src/engine/DAE/SessionInfo.cs` and `src/engine/DAE/Schema/{Column, Constraint, CursorType, DataType, Default, Device, GenericType, Library, ListType, Reference, RowType, ScalarType, Security, ServerLink, Signature, TableType}.cs`): dead in every file (no permission type is used), but the namespace still exists on .NET 10, so they compile and do nothing. Per WS12 ("never as a sweep") and the source-hygiene rule (do not touch files you are not otherwise changing), leave them; remove one only if this ticket edits that file for another reason (it edits none of them).

So this arm needs no edits. Confirm the inventory still holds when you run (search compiled sources of the engine, server, protocol and store projects for `using System.Runtime.Remoting`, `System.EnterpriseServices`, `System.Configuration.Install`, `System.Data.SqlServerCe`), and list what you checked in the handoff. If a dead `using` of a namespace that does not exist on .NET 10 turns up in a compiled file, the project would not build, so it would already have been found; anything that does turn up is a stray file, not a fix for here.

## NLS globalization pin

Plan WS2 "Globalization" and D2: `build-core-sdk-projects` selects Windows NLS (`System.Globalization.UseNls`) for every executable and test host, so that string comparison, casing and sort order match .NET Framework. Nothing yet proves the setting is in effect or keeps it there. Add one small D4 script, run through the harness, that pins a handful of cases where ICU and NLS are likely to differ (candidates, to be confirmed by running them both ways): ordering of strings that differ only in hyphens or apostrophes (NLS "word sort" ignores them for ordering), comparison of strings containing embedded null or other control characters, and case-insensitive comparison of a few non-ASCII letters (for example Turkish dotted/dotless i under the invariant culture). The expected values are the NLS results; establish them on a host where `UseNls` is set and confirm at least one case fails when it is switched off (record which in the handoff; do not commit a test that toggles the setting). Use the D4 comparison and ordering operators (`=`, `<`, `CompareText`, `order by` over a small table), not C#, so the script also pins what D4 does on top of the runtime.

## Edge cases & interactions

- K2, section starting at column 1 on a single line: `LinePos - 1 = 0`, the old and new code agree. Covered by the multi-line case's first line and by inspection.
- K2, last line without trailing newline: the end-of-script case in the new script.
- K2, `\r\n` line endings: the newline scan looks for `\n` only and positions count characters, so `\r` is part of the previous line and does not shift columns on the next one. Unchanged by the fix; inspection.
- K2, aggregate operators: the same function builds `InitializationText`, `AggregationText`, `FinalizationText`; an aggregate written on one line hit the same bug. Covered by inspection (same code path); add a one-line aggregate to the K2 script if it costs a few lines.
- K5, `ExecuteAsync` on a process that is stopped while the script runs: covered by `DebugLibrary.d4`.
- K5, `ExecuteAsync(script)` without a process ID: the operator starts a process with the caller's session info and the worker stops it at the end. Covered by `Transaction.Isolation.Run.d4` (uses the process-ID form) and inspection; add one line to the new async script that calls the no-ID form and then waits briefly (`Sleep`) and checks the process count returned to its start value.
- K5, the server stopping while async scripts run: `Server.Stop` stops sessions and processes; the worker's process is terminated cooperatively as above. Verified by the harness fixture's teardown not hanging after the scripts above; inspection otherwise.
- K5, exception on the worker after the calling session has gone: swallowed as before; with tasks, no crash. Inspection.
- K5, `ExecuteMultiple` with `number` = 0: the old code never calls `ExecuteResults`, so `_done` stays false and the busy-wait never ends (a hang). Waiting on zero tasks returns at once. Note the difference in the handoff as a fixed hang; no test.
- K22: a relative `LibraryDirectories` entry in an instance configuration resolves against the folder of the host's binaries, as before. Covered by the unit test of `GetFullFileName` plus inspection of `Server.LibraryDirectory`.
- Formatting: tabs, CRLF and byte-order marks preserved in every edited `.cs` file; new `.d4` files follow the encoding and line endings of their neighbours in the same folder.

## Done when

- K2 fixed; the new D4 script passes and is in the manifest as `pass`; it failed before the fix.
- K5's three sites no longer call `BeginInvoke`; `DebugLibrary` is `pass` in the manifest; `TestProcessExecutes` and `Transaction.Isolation.Run` results are recorded and their manifest entries updated as described; the `ExecuteMultiple` script passes.
- K22 fixed with its unit test.
- The NLS script passes and is in the manifest as `pass`; the handoff names a case that fails without NLS.
- Dead-`using` inventory confirmed in the handoff; no file edited for it.
- Every other manifest entry unchanged (no new failures).
- No `docs/migration-notes.md` entry: on 3.1 these operators worked, and the fixed versions keep their observable behaviour; the only differences are a hang fixed in `ExecuteMultiple` with zero sessions and operators now working where on .NET 10 they would throw. If you change any observable behaviour beyond that, add an entry.

## TODO

- NLS: write the comparison script, confirm one case differs with `UseNls` off (locally, not committed), add it to the manifest as `pass`.
- K2: write the D4 script and confirm it fails; fix `CopySection`; add the script to the manifest as `pass`.
- K5: replace the three `BeginInvoke` calls with long-running tasks; move per-call state into locals; keep the swallow and fixed-wait behaviours with `NOTE:`s; replace the busy-wait.
- K5: run `DebugLibrary`, `TestProcessExecutes`, `Transaction.Isolation.Run`; write the `ExecuteMultiple`/no-ID `ExecuteAsync` script; update the manifest entries.
- K22: switch `GetBinDirectory` and `GetInstallationDirectory` to `AppContext.BaseDirectory`, drop the `RelativeSearchPath` branch; add the unit test.
- Re-check the dead-`using` inventory; record it in the handoff.
- Run the full L1 suite and the unit tests; record results.
