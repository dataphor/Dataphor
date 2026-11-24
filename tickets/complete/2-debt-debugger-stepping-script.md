description: A new test script drives everything a person does with a program the debugger has already stopped - stepping it one statement at a time, stopping one that is only running, stopping one on an error, and letting a stopped one go again without leaving the server stuck.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4, tests/DAE.Server.Tests/coverage-manifest.json, docs/architecture/sessions-processes-plans.md
----
## What landed

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4` (501 lines after review, 447 as implemented), its manifest entry, and three new paragraphs plus two corrections in the **Debugger** section of `docs/architecture/sessions-processes-plans.md`. No engine code changed, and no migration note: nothing an application, a deployment script or a C# extension can notice moved.

Eight cases, each on a process of its own, cleaning up after itself: break on start and the flag it sets; stepping into the operator the program calls; stepping into a call and stepping over it from the *same* stopping point, so a reader can see the contrast; step over's depth rule surfacing in the caller; `Debug.Pause` stopping a program that is only running, and `Debug.Run` letting it finish; break on exception stopping at the raise with the raised error readable in the `Error` column of `Debug.GetProcesses`; a plain breakpoint inside an `except` handler also reporting an error there (added by the review, below); and both ways of releasing a stopped process - detaching it and stopping the debugger - leaving it stoppable.

No engine defect was found, by the implementation or by the review. Every path that had never run before behaves as predicted. The manifest entry carries `preamble: "SetUseImplicitTransactions(false);"` for the same reason as `DebugBreakpoints` (the script reads the locator and declaration line of operators it creates itself out of the `Operators` catalog table, and a catalog store view answers from committed data only while the harness runs a whole document inside one implicit transaction) and `timeoutSeconds: 60`, because every wait in the script is bounded at 10 s by the script itself and raises an error naming what it was waiting for.

The implementation handoff, in `ticket(implement): debt-debugger-stepping-script`, records four places where the implement ticket's own predictions did not survive contact with the engine - `DidBreak` is true for break-on-start and not false; all target operators share one locator so lines and not locators tell them apart; stepping off a statement that calls an operator takes two steps because the statement holds two breakable nodes at one line; and the `Debug.Pause` loop needs a million iterations rather than a few hundred thousand, counting rather than summing, because D4's `Integer` is signed 32 bits. The review re-derived all four from the source and confirms each.

## Review findings

The implement-stage diff was read before the handoff summary. Everything the handoff asserts about the engine was re-derived from the source rather than taken on trust: `Debugger.Yield`, `ShouldBreak`, `InternalRun`, `Detach`, `Dispose`, `Pulse` and `IsPaused`; `ServerProcess.SetStepOver`, `SetStepInto`, `ClearBreakInfo` and `InternalShouldBreak`; the `IsBreakable` state of `CallNode`, `VariableNode`, `RaiseNode`, `TryExceptNode` and `ErrorHandlerNode` and the comment in `InstructionNodeBase`'s constructor; both yield paths of `PlanNode.Execute`, including that `ReportThrow` runs only for a *new* exception; `Program.ReportStart` and `ReportThrow`; and `Engine.TerminateProcessThread`'s rescue and its half-of-`ProcessTerminationTimeout` budget. Every line offset the script asserts on was recomputed by hand against the file and the two-breakable-nodes-per-call-statement and depth-rule step sequences were traced node by node; all agree with the script. Source hygiene checked: CRLF, no byte-order mark and tabs, matching its two sibling scripts, no trailing whitespace, and - as implemented - 447 lines of which 51 are blank and 92 are line comments - the comments say why (which node breaks, which lock is not held, what a failure will look like), not what.

**Majors: none.** Nothing in the diff warranted a ticket. Three things were found and fixed in this pass, all in prose that a future reader would have believed:

- **The architecture page miscounted what stops a program.** The new paragraph read "Five things stop a program and four of them go through `Debugger.ShouldBreak`" and then listed five that do, before calling `Debug.Pause` "the fifth". It is six and five. Corrected.
- **The architecture page said the `Error` column is only ever filled by break-on-exception.** It is not: `TryExceptNode` sets the program's error variable as it catches and clears it only once the handler has run, so a plain breakpoint on a statement inside an `except` handler reports a value there too. The page now says what sets the variable, names both sites, and says break-on-exception is not the only stop that reports one. The same overclaim in a comment in the script was corrected with it.
- **The architecture page's corrected sentence about `CallNode` traded one wrong grouping for another.** It placed `CallNode` among the imperative statement nodes and then said instruction nodes are not breakable - but `CallNode` *is* an instruction node, which is the whole reason the original sentence was wrong. It now says instruction nodes are not breakable by default and that `CallNode` is the one that opts back in.

**One test added**, for the one contract the review's own correction left unverified. The claim above about the `except` handler was derived statically, and a documented claim nobody has run is the kind this ticket exists to retire. `TestDebugStepHandler` raises and catches inside itself; a breakpoint on the statement inside its handler stops the process there, and the case asserts the exact line, `DidBreak` true (a breakpoint, not break-on-exception, stopped it) and the handled error's message in the `Error` column. It passes, so the correction is now pinned rather than reasoned. It costs no measurable wall clock: `DebugStepping` is still 6 s. The manifest note was extended by one clause to say the script covers it.

**One arm appended, no ticket filed.** `tickets/implement/3-debt-debugger-coverage.md` is the last ticket of this three-ticket arc and its handoff already has to enumerate what is still uncovered in the debugger. Three gaps this script leaves were added to that instruction so the next coverage pass does not lose them: the waiting loop inside `Debug.WaitForPause()`, `Debug.GetStack` with a window index other than 0, and a breakpoint whose `LinePos` is a real position rather than `-1`. None of the three is in that ticket's scope; none is worth a ticket of its own at the filing bar, and none is conditional, so none is a tripwire either - they are an arm on the ticket that already owns the subject.

**Found and deliberately left alone, with the reason in each case** - the duplication a DRY pass would flag first:

- `TestDebugStepAwait` is a near-verbatim copy of `TestDebugAwaitPause` in `DebugBreakpoints.d4`. It cannot be shared: the coverage library (`TestFramework.Coverage.Scripts.d4l`) registers no operators, each document is run as a self-contained script, and the harness gives each script its own server instance. Sharing the helper would mean giving the library a loaded operator, which is a larger change than the duplication costs.
- The step-into and step-over halves of the central case are near-duplicates. Folding them into one operator taking a Boolean would be a flag argument whose two branches are the two different final assertions, and the contrast between the two blocks laid out one after the other is the thing the case is for.
- The four-line prelude that starts a process, attaches it, runs the outer operator and waits appears four times. Extracting it into an operator that returns the new process identifier would make it *worse*, not better: if the wait inside raised, the caller would never have the identifier and the process would be unstoppable. Keeping `StartProcess()` in the caller's scope is what lets each case pair it with `StopProcess` itself.

**Checked and confirmed correct, against the handoff's own doubts:**

- The handoff asks for a second opinion on whether the step-over case's "never inside the inner operator" assertion is weaker than it looks, since it sees at most four stops. It is not weaker - it is exhaustive. A stop parks the process until the script steps it again, and `TestDebugStepOnce` reads the line of every stop it waits for, so no stop between the breakpoint and the first changed line can go unobserved. The assertion covers every stop in the window it tests.
- The two range assertions (where `Debug.Pause` lands inside the loop, and where stepping into an operator lands) are the right call and should not be pinned to exact lines. The entered operator stops at its body block, whose line is the `begin`, but which node of an entered operator breaks first is a compiler decision - `Compiler.cs` already marks several synthetic block nodes non-breakable - and no published contract fixes it. Pinning it would couple the test to that decision.
- The last two cases have no `try ... finally StopProcess`, unlike the other six. That is deliberate and correct: in those two, `StopProcess` *is* the assertion, wrapped in `try ... except` so that a failure to release reads as a named error. A failure elsewhere in them cannot hang the run either - a process parked in `Debugger.Yield` implies a pause has been requested, which with one attached process means `IsPaused`, which is exactly the condition `Engine.TerminateProcessThread` detaches on.
- The timing assumption in the `Debug.Pause` case is already parked as a `NOTE:` at the loop operator, with the measurement, the margin and what to do if a faster machine closes it. That is the right disposition for a conditional concern and it was left as it stands. The failure it describes is a clean assertion naming the stop, not a hang or a timeout.
- The harness gives every script its own `TestServerInstance`, disposed after the case, so nothing this script leaves behind on a failure can reach another script.

**Tripwires parked this pass: none new.** The implementation's two `NOTE:`s (the loop's timing margin, and `Engine.TerminateProcessThread`'s rescue not being able to mask a failure of the release cases) were reviewed and kept as written.

## Tests

| Test | What it verifies |
|---|---|
| `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4` (coverage script, manifest `pass`) | the eight cases listed under *What landed*: break on start and the flag it sets; step into entering a called operator and step over not entering it, from the same stopping point; step over's depth rule surfacing in the caller; `Debug.Pause` stopping a program that is only running and `Debug.Run` letting it finish; break on exception stopping at the raise with the raised error readable in the `Error` column; a breakpoint inside an `except` handler reporting the handled error there; and both ways of releasing a stopped process |

No NUnit test. The subsystem is reached from D4 and a script through the in-process coverage harness is the lowest layer that shows the behaviour; the one thing a script cannot reach, debugging a second session, is owned by `debt-debugger-coverage`.

## Validation

`dotnet build Dataphor.slnx` - 0 errors, run after each edit.

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=DebugStepping|Name=DebugBreakpoints|Name=DebugLibrary|Name=AsyncExecution|Name=Exceptions|Name=ReproContextError|Name=SessionAndProcessScopedTables|Name=SessionTables|Name=TestProcessExecutes" --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class == Alphora.Dataphor.DAE.Server.Tests.ParserRoundTripCorpusTest or class == Alphora.Dataphor.DAE.Server.Tests.D4EmitterRoundTripTest or class == Alphora.Dataphor.DAE.Server.Tests.InternalCoverageTests"
python tools/docs-size-check/check_size.py
```

The first is the implement stage's targeted set widened by one ring: the whole Debugger script area including `DebugTest` on the first run, plus every neighbouring script a new asynchronous, exception-raising or process-starting case could disturb. 8 passed, 1 known failure matched (`Exceptions`, against `test-pre-3-1-scripts-policy`), 0 failed, 44 s with the template build. `DebugStepping` is 6 s both before and after the case the review added; `DebugBreakpoints` 4 s, unchanged. Run again after every edit, including the last. The second is the pass a new `.d4` file enters as a corpus case - how `bug-d4-emitter-drops-qualifier-grouping` surfaced during the previous ticket - plus the manifest's own validation, which runs inside the coverage fixture and reported `discovery problems: 0` on every run above: 396 passed, 19 skipped, 0 failed, 3 s. The third: 11 files checked, 0 over the limit; the Debugger page's file is at 1,155 words of the 1,500 cap.

The three-part full run was not needed and was not run: the change touches no build or test infrastructure, no manifest format, and no lexer, parser or compiler code. `DebugStepping` starts with D, so it joins `DebugBreakpoints` in part 2, which grows by those 6 s. CI runs the whole suite in Debug and Release on the push, and Release is where the `Debug.Pause` case's timing assumption is least measured - a red `DebugStepping` in the Release leg is the thing to read first, and the `NOTE:` at the loop operator says what to do about it.

No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written. `tickets/.garden-report.md` is modified in the working tree by the gardener and was left alone.

## Documentation changed

`docs/architecture/sessions-processes-plans.md`, **Debugger** section - three paragraphs added by the implementation and two sentences corrected, then three corrections by the review (above):

- What stops a program, which of those go through `Debugger.ShouldBreak`, and so what the `DidBreak` and `Error` columns of `Debug.GetProcesses` mean.
- Step over's depth rule, that a step clears the pause request, and that a step on a debugger that does not report itself paused does nothing.
- How a held process is released, why `Engine.TerminateProcessThread` matters to shutdown, and what its own rescue can and cannot unblock.
- Corrected: which nodes are breakable - instruction nodes are not by default, `CallNode` opts back in, and a statement that calls an operator therefore holds two breakable nodes at one line, so stepping off it takes two steps.

## What is still unexercised

- **`Debug.WaitForPause()`'s waiting loop** (`_waitSignal.WaitOne(500)` then `program.Yield`), which needs a caller that waits while nothing is yet stopped. Armed on `debt-debugger-coverage`.
- **`Debug.GetStack` with a window index other than 0** - an outer frame, or a negative one. Armed on `debt-debugger-coverage`.
- **`LinePos`** is asserted only where a breakpoint carries `-1`; both debugger scripts assert on lines alone. Armed on `debt-debugger-coverage`.
- **Debugging a second session** (`Debug.AttachSession` of a session other than the debugger's own, `Debug.GetSessions` with a row in it, `Debug.DetachSession`), which no D4 operator can reach from a script. That is `debt-debugger-coverage` itself.
- **`Debugger.Pulse` on its own** - reached here only through `Detach`.
- **`src/engine/DAE/Debug/DebugContext.cs`** still cannot move: nothing in the repository constructs it, and its coverable lines stay uncovered until `debt-ws12-hygiene-remainder` deletes it.

## Coverage this should move

On top of what `DebugBreakpoints` moved: `Debugger.StepOver`, `Debugger.StepInto`, `Debugger.Pause`, `Debugger.Detach`, `Debugger.Dispose`'s detaching branches and `Debugger.Pulse`; `ServerProcess.SetStepOver`, `SetStepInto`, `ClearBreakInfo` and `InternalShouldBreak`, including its two depth-lowering branches; `Program.ReportStart` and `Program.ReportThrow` with a debugger that wants them; `TryExceptNode`'s handler path under a debugger; and the nodes over all of it - `DebugSetBreakOnStartNode`, `DebugStepOverNode`, `DebugStepIntoNode`, `DebugPauseNode`, `DebugDetachProcessNode` and `DebugStopNode` - plus the `process.Error` assignment in `DebugGetProcessesNode` with a value in it, now from two different kinds of stop. Do not hand-edit `docs/coverage-report.md`; the generated half is replaced wholesale by the next measured run.
