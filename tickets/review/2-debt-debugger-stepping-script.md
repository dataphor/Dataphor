description: A new test script drives everything a person does with a program the debugger has already stopped - stepping it one statement at a time, stopping one that is only running, stopping one on an error, and letting a stopped one go again without leaving the server stuck.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4, tests/DAE.Server.Tests/coverage-manifest.json, docs/architecture/sessions-processes-plans.md
----
## What landed

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4` (447 lines), its manifest entry, and three new paragraphs plus one correction in the **Debugger** section of `docs/architecture/sessions-processes-plans.md`. No engine code changed, and no migration note: nothing an application, a deployment script or a C# extension can notice moved.

**Everything the ticket asked for works, and no engine defect was found.** The paths that had never run before - `Debug.SetBreakOnStart`, `Debug.StepInto`, `Debug.StepOver`, `Debug.Pause`'s stopping of a running program, break-on-exception, the `Error` column of `Debug.GetProcesses` with a value in it, and both release paths (`Debug.DetachProcess` and `Debug.Stop()` on a stopped process) - all behave as the ticket predicted. The script passes; it ran green on seven consecutive runs.

The manifest entry carries `preamble: "SetUseImplicitTransactions(false);"` for the same load-bearing reason as `DebugBreakpoints` (the script reads the locator and declaration line of operators it creates itself out of the `Operators` catalog table, and a catalog store view answers from committed data only while the harness runs a whole document inside one implicit transaction), and `timeoutSeconds: 60`, because every wait in the script is bounded at 10 s by the script itself and raises an error naming what it was waiting for.

## Where the ticket was wrong, and what the script does instead

Four of the ticket's predictions did not survive contact with the engine. Each was checked against the source and then confirmed by running a throw-away tracing version of the script, which printed the whole sequence of stops as an error message; a reviewer re-deriving any of them should start from the named code site.

- **`DidBreak` is true for break-on-start, not false.** The ticket's first case says "with `DidBreak` false - this is a pause the debugger asked for, not a breakpoint", and its own *Edge cases* section then says the opposite and is right. `Program.ReportStart` asks for the stop through `ServerProcess.SetStepInto`, so `Debugger.ShouldBreak` returns true, and `Debugger.Yield` adds the process to its broken set whenever `ShouldBreak` is true, whatever decided it. `Debug.Pause` is the *only* stop for which `DidBreak` is false. The script asserts true for break-on-start and for break-on-exception, false for `Pause`.
- **Both target operators share one locator, so a locator cannot tell them apart.** The ticket asks to "step until the reported `Locator` is the outer operator's locator, then ... until it is the inner operator's locator". One document created both operators, so both carry `doc:Coverage.Scripts:DebugStepping`. The script uses the locator only for what it can do - telling an operator apart from the ad-hoc `program:` locator of the script that calls it - and uses **lines** to tell the two operators apart, computed from each declaration line read out of the catalog.
- **Stepping off the statement that calls an operator takes two steps, not one.** That statement holds two breakable nodes at the same line: the `VariableNode` of the `var` statement and the `CallNode` of the invocation, which sets `IsBreakable = true` in its own constructor (`ImperativeNodes.cs`). So both halves of the step-into/step-over pair step in a small bounded loop until the reported line leaves the call statement, rather than asserting on the result of one step. This also made the existing architecture text wrong - see below.
- **The `Pause` loop needed a million iterations, not "a few hundred thousand", and it has to count rather than sum.** D4's `Integer` is signed 32 bits (`System.Int32Conveyor`, `DataTypes.d4`), so an accumulating loop over a few hundred thousand indices raises an arithmetic overflow long before the loop is long enough to be worth pausing - which is how the first draft failed. Measurements below.

## What the script asserts, case by case

Eight processes in seven cases, each case taking a process of its own and cleaning up after itself (breakpoints cleared, both flags cleared, process stopped).

| Case | Asserted |
|---|---|
| Break on start | `Debug.GetDebuggers` reports `BreakOnStart`; the attached process stops at the first breakable node of the script it was given, with a `program:` locator and `DidBreak` true |
| Step into from there | stepping into, in a loop bounded at 6 steps, reaches the document locator, at a line inside `TestDebugStepOuter`'s declaration |
| Step into the call | from a breakpoint on the statement that calls `TestDebugStepInner`: the stop is on that line, and stepping into (bounded at 4) reaches a line of the inner operator's declaration |
| Step over the call | from the same breakpoint on a process of its own: stepping over (bounded at 4) reaches the *next statement of the outer operator* exactly, and no stop it observes on the way is inside the inner operator |
| Step over the last statement of an operator | from a breakpoint on `result := LPlusOne`, one step over surfaces in the caller, at the statement after the call - the depth rule, asserted once |
| `Debug.Pause` on a running program | the process stops with `DidBreak` false, at a line inside the loop operator and before the statement after its loop; then `Debug.Run` and the breakpoint on that statement is hit, with `DidBreak` true, which is the evidence the loop ran on from where it was held |
| Break on exception | `Debug.GetDebuggers` reports `BreakOnException`; the process stops at the raise statement's line with `DidBreak` true; the `Error` column is not nil and its `Message` is the message the operator raised |
| Detaching a stopped process | it disappears from `Debug.GetProcesses` at once, and `StopProcess` then **returns** |
| Stopping the debugger | `Debug.GetDebuggers` has no row for the session, and `StopProcess` then **returns** |

## Things about the engine the script records, for whoever reads it next

- **`CallNode` is breakable.** The invocation of a D4-defined operator yields to the debugger before it pushes its own stack window, so the stop is reported in the *caller's* window at the call site's line. A built-in instruction (arithmetic, a comparison) is not breakable, and `InstructionNodeBase`'s constructor says why. The architecture page claimed the opposite for both; corrected.
- **Entering an operator stops at its `begin` line.** The first breakable node of an operator body is its block, whose line is the `begin`. The script asserts a range (a line of the declaration) rather than that exact line, because which node of the entered operator breaks first is a compiler detail, not a published contract.
- **Break on exception stops *after* the raising node, not before it.** `RaiseNode.InternalExecute` calls `Program.ReportThrow` and then throws; the stop happens in `PlanNode.Execute`'s own exception path, which yields with `afterNode` true, so the line reported is the statement's *end* line - the same line for a statement written on one line, which is why the script keeps the raise on one line.
- **The error does not stop the program twice.** `Error('...')` selects a `DataphorException` of `ErrorSeverity.User`, which every enclosing node's catch re-throws unchanged (`isNew` stays false), so `ReportThrow` is not called again as the exception unwinds and `Debug.Run` lets it propagate to the worker, where `SystemExecuteAsyncNode` discards it.
- **`DebugGetProcessesNode`'s `row[6] = process.Error` works.** This is the first thing in the repository ever to read that column with a value in it. The CLR exception survives into a D4 `Error` value: the script reads `Message` back, and the tracing run also read `Code` (500000, `DataphorException.ApplicationError`). Nothing needed fixing.
- **A step clears the pause request, so a step too many ends the program.** Every bounded loop in the script exists for that: the ad-hoc program the process runs has one statement, so stepping past it finishes the program rather than stopping it again, and the wait inside each step turns that into a named error instead of the harness's timeout.
- **`Engine.TerminateProcessThread` has a rescue of its own** - it detaches a *paused* debugged process in order to unblock `Debugger.Yield`'s wait - but it cannot mask a failure of the two release cases, because `Debugger.Detach` has already cleared the process's debugger by then. Noted at the site.

## What a reviewer should look at hardest

- **The one timing assumption.** The `Pause` case needs the loop still running 100 ms after `ExecuteAsync`. Measured on one slow development laptop in Debug: a million iterations takes about 1.5 s on its own and about 2 s with a debugger attached (every breakable node takes the debugger's lock), and reading the loop counter off the stopped stack 100 ms in gave 30,297 - about 3 % of the way through, so roughly a thirtyfold margin. `DateTime()` has one-second resolution in D4, so those two figures are to the nearest second; the counter is exact and is the better number. Parked as a `NOTE:` at the loop operator saying what to do if a faster machine or a Release build ever closes that gap: the case fails on its own assertion, naming the stop as being at the statement after the loop with `DidBreak` set, and the fix is to raise the count.
- **The step-over "never inside the inner operator" assertion sees only the stops the bounded loop observes** - at most four. A step over that entered and left the inner operator between two observed stops would not be caught. What makes that impossible is the depth rule (`ServerProcess.SetStepOver` plus `InternalShouldBreak`), not the assertion; the assertion covers what the debugger reports, which is what a person sees. Worth a second opinion on whether anything stronger is reachable from D4.
- **Two assertions are ranges rather than exact lines**, both deliberately: where `Debug.Pause` lands inside the loop operator (a timing matter), and where stepping into an operator lands (the `begin` line in practice, a compiler detail). Both are stated as such in comments. A reviewer who thinks either should be pinned exactly should say which contract pins it.
- **`Debug.WaitForPause()` is not called at all**, as the ticket directed: it has no timeout of its own and `DebugBreakpoints` already covers the path where it returns at once. Its waiting loop is still unreached by anything in the suite.
- **The catalog read is guarded once, for four operators.** All four are created the same way in the same script, so a read that finds one finds all; the comment at the site says so. Without the guard the first failure would be `A nil was encountered where a value was expected`, because `[]` on an empty table yields nil.

## Tests

| Test | What it verifies |
|---|---|
| `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4` (coverage script, manifest `pass`) | the behaviour table above: break on start and the flag it sets; step into entering a called operator and step over not entering it, from the same stopping point; step over's depth rule surfacing in the caller; `Debug.Pause` stopping a program that is only running and `Debug.Run` letting it finish; break on exception stopping at the raise with the raised error readable in the `Error` column; and both ways of releasing a stopped process - detaching it and stopping the debugger - leaving it stoppable |

No NUnit test. The subsystem is reached from D4 and a script through the in-process coverage harness is the lowest layer that shows the behaviour; the one thing a script cannot reach, debugging a second session, is owned by `debt-debugger-coverage`. No test was added for the `Integer` overflow met while sizing the loop: 32-bit `Integer` is the declared type (`DataTypes.d4` names it "System Signed 32-bit Integer"), so the overflow was a mistake in the draft script, not a defect.

## Validation

`dotnet build Dataphor.slnx` - 0 errors, run after each edit.

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=DebugStepping|Name=DebugBreakpoints|Name=DebugLibrary|Name=AsyncExecution|Name=Exceptions" --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class == Alphora.Dataphor.DAE.Server.Tests.ParserRoundTripCorpusTest or class == Alphora.Dataphor.DAE.Server.Tests.D4EmitterRoundTripTest or class == Alphora.Dataphor.DAE.Server.Tests.InternalCoverageTests"
python tools/docs-size-check/check_size.py
```

The first is the implement stage's targeted set - the whole Debugger script area plus the two scripts a new asynchronous or exception-raising case could disturb: 4 passed, 1 known failure matched (`Exceptions`, against `test-pre-3-1-scripts-policy`), 0 failed, 37 s. `DebugStepping` itself took 6 s on six of seven runs and 9 s on one; `DebugBreakpoints` 4-5 s, unchanged. The second is the pass a new `.d4` file enters as a corpus case, which is how the emitter defect `bug-d4-emitter-drops-qualifier-grouping` surfaced during the previous ticket, plus the manifest's own validation tests: 396 passed, 19 skipped, 0 failed, 3 s. Every read of a line in this script goes through a local first, for that defect. The third: 11 files checked, 0 over the limit - the Debugger page is at about 1,050 words of the 1,500 cap after the three paragraphs added.

`DebugStepping` starts with D, so it joins `DebugBreakpoints` in **part 2** of the three-part agent run, which grows by those 6 s. The three-part full run was not needed: the change touches no build or test infrastructure, no manifest format, and no lexer, parser or compiler code. CI runs the whole suite in Debug and Release on the push - and Release is where the one timing assumption above is least measured, so a red `DebugStepping` in the Release leg is the thing to read first.

No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written. `tickets/.garden-report.md` is modified in the working tree by the gardener, not by this ticket, and was left alone.

## Documentation changed

`docs/architecture/sessions-processes-plans.md`, **Debugger** section:

- **Corrected** "an operator invocation or an arithmetic instruction is deliberately not breakable". The first half is false: `CallNode` sets `IsBreakable` in its constructor. The sentence now names `CallNode` among the breakable imperative nodes, keeps the true half about built-in instructions, and states the consequence a person stepping actually meets - two breakable nodes at the line of a statement that calls an operator, so stepping off it takes two steps.
- **Added** three paragraphs: the five things that stop a program and which four go through `Debugger.ShouldBreak` (so that what `DidBreak` and the `Error` column mean follows from the mechanism); step over's depth rule, that a step clears the pause request, and that a step on a debugger that is not paused does nothing; and how a held process is released, with why `Engine.TerminateProcessThread` matters to shutdown and what its own rescue can and cannot unblock.

## What is still unexercised

- **`Debug.WaitForPause()`'s waiting loop** (`_waitSignal.WaitOne(500)` then `program.Yield`), which needs a thread that waits while nothing is yet stopped.
- **`Debug.GetStack` with a window index other than 0** - an outer frame, or a negative one. The tracing run read window 0 of a stopped loop; nothing reads another window.
- **Debugging a second session** (`Debug.AttachSession` of a session other than the debugger's own, `Debug.GetSessions` with a row in it, `Debug.DetachSession`), which no D4 operator can reach from a script. `debt-debugger-coverage`.
- **`LinePos`** is still only asserted where a breakpoint carries `-1`; this script asserts on lines alone.
- **`Debugger.Pulse` on its own** - reached here only through `Detach`.
- **`src/engine/DAE/Debug/DebugContext.cs`** still cannot move: nothing in the repository constructs it, and its coverable lines stay uncovered until `debt-ws12-hygiene-remainder` deletes it.

## Coverage this should move

On top of what `DebugBreakpoints` moved: `Debugger.StepOver`, `Debugger.StepInto`, `Debugger.Pause`, `Debugger.Detach`, `Debugger.Dispose`'s detaching branches and `Debugger.Pulse`; `ServerProcess.SetStepOver`, `SetStepInto`, `ClearBreakInfo` and `InternalShouldBreak`, including its two depth-lowering branches; `Program.ReportStart` and `Program.ReportThrow` with a debugger that wants them; and the nodes over all of it - `DebugSetBreakOnStartNode`, `DebugStepOverNode`, `DebugStepIntoNode`, `DebugPauseNode`, `DebugDetachProcessNode` and `DebugStopNode` - plus the `process.Error` assignment in `DebugGetProcessesNode` with a value in it. Do not hand-edit `docs/coverage-report.md`; the generated half is replaced wholesale by the next measured run.
