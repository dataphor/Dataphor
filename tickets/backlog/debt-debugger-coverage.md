description: The engine has a debugger that the IDE drives to pause a running program, step through it and read its variables, and no test ever actually stops a program at a breakpoint - so the whole stepping half of it is untested and could break without anyone noticing until someone tried to debug something.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Debug/Debugger.cs, src/engine/DAE/Debug/DebugNodes.cs, src/engine/DAE/Debug/Breakpoint.cs, src/engine/DAE/Debug/CallStackEntry.cs, src/engine/DAE/Debug/DebugContext.cs, src/engine/DAE/Debug/Debug.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugTest.d4
tradeoffs: The debugger is a development aid, not something a deployed application depends on, so a maintainer may rank it below subsystems that serve customer workloads; against that, it is the feature whose breakage is hardest to diagnose, because the tool you would use to investigate is the broken one.
----
## The area

**Debugger**, `src/engine/DAE/Debug/`. Measured **35.7 % line coverage of 894 coverable lines, 575 uncovered**, on the full three-part run of 2026-11-21 at commit `f6200026` (`docs/coverage-report.md`).

| File | Line coverage | Coverable |
|---|---:|---:|
| `src/engine/DAE/Debug/Debugger.cs` | 36.7 % | 411 |
| `src/engine/DAE/Debug/DebugNodes.cs` | 31.4 % | 350 |
| `src/engine/DAE/Debug/DebugLocator.cs` | 56.1 % | 66 |
| `src/engine/DAE/Debug/Breakpoint.cs` | 0.0 % | 19 |
| `src/engine/DAE/Debug/CallStackEntry.cs` | 0.0 % | 16 |
| `src/engine/DAE/Debug/DebugContext.cs` | 0.0 % | 11 |

## What the 35.7 % is, and what the gap is

The 35.7 % is not an accident: one coverage script, `DebugLibrary.d4`, passes and reaches a real part of this. It starts a debugger, checks it appears in `GetDebuggers()`, sets break-on-exception and break-on-start, attaches and detaches a process and a session, and checks each against `Debug.GetProcesses()` and `Debug.GetSessions()`. That is the whole of what the suite covers.

The gap is everything after a program actually stops. `Breakpoint.cs`, `CallStackEntry.cs` and `DebugContext.cs` are at **exactly zero**, and those three are the types that only come into being when something is paused. So: no test in this repository has ever hit a breakpoint. Everything that follows from one - the paused program's call stack, its stack frames and their values, stepping, resuming - is unreached, and that is the two thirds of `Debugger.cs` and `DebugNodes.cs` that is dark.

The operators that make up the untested half, from `DebugNodes.cs` and `src/engine/DAE/Debug/Debug.d4`:

- `Debug.ToggleBreakpoint(locator, line, linePos) : Boolean` and `Debug.GetBreakpoints()`
- `Debug.Pause()`, `Debug.WaitForPause()`, `Debug.Run()`, `Debug.RunTo(processID, locator, line, linePos)`
- `Debug.StepOver(processID)`, `Debug.StepInto(processID)`, `StepIntoSpecific(processID, operatorName)`, `GetStepIntoOperators(processID)`
- `GetCallStack(processID)` and `GetStack(processID, windowIndex)` - the two that read a paused program, and the reason `CallStackEntry` is at zero
- `Debug.Stop()`, and break-on-exception actually breaking rather than merely being set

## Do not reuse what `DebugTest.d4` does

`DebugTest.d4` is the script that was meant to cover this, and reading it is the cheapest way to see the trap. Two reasons it cannot be the model:

- **Its loop never ends.** `var LCounter := 1; while LCounter < 100 do begin LCounter := LCounter + 1; ... LCounter := LCounter - 1; end;` - the body increments and then decrements, so the condition never becomes false. The manifest records the script as a known failure with a 5 s `timeoutSeconds` and the note "infinite loop by design: a debugger stepping exercise", owned by `test-pre-3-1-scripts-policy`. It was written to give a human something to step through interactively, not to be run unattended.
- **Its process identifiers are hard-coded.** It calls `Debug.GetCallStack(45596481)` and `Debug.GetStack(14000148, 0)` with numbers from whatever session its author had open in 2009. They can never match a live process.

A script here must instead give the debugger something that **terminates on its own** even if every debugger call fails - a loop with a bounded counter, or a single operator call - and must take every process identifier from `Debug.GetProcesses()` or `StartProcess()` rather than writing one down. Whether the old file stays in the library is `test-pre-3-1-scripts-policy`'s call; do not edit, retire or delete it here, and do not change `tests/DAE.Server.Tests/coverage-manifest.json`.

## What a coverage script should exercise

A D4 script through the in-process harness, in the shape `DebugLibrary.d4` already establishes - start a debugger, start a second process, attach it - and then the part that script stops short of:

- **Set a breakpoint and have it hit.** Toggle a breakpoint in a known operator in the test library, run a process that calls it, `WaitForPause()`, and assert the process shows as paused at the expected locator and line in `Debug.GetProcesses()`. This one case is what brings `Breakpoint.cs`, `DebugContext.cs` and the paused half of `Debugger.cs` to life.
- **Read the paused program.** `GetCallStack` must show the operator and its caller; `GetStack` must show the parameters and local variables of the frame with their values. This is `CallStackEntry.cs`, and it is the behaviour the IDE's debug window depends on most.
- **Step.** `StepOver` and `StepInto` from the paused point, asserting the line moves as expected and that `StepInto` enters a called operator while `StepOver` does not. `RunTo` a later line. Then `Run()` to completion and assert the process finishes.
- **Toggle off.** Remove the breakpoint, run again, and assert the process does not pause - a breakpoint that cannot be cleared is as bad as one that cannot be set.
- **Break on exception.** `DebugLibrary.d4` sets the flag; nothing checks it works. Raise an error in an attached process and assert it pauses at the raise rather than propagating.
- **Detach while paused.** A paused process whose debugger goes away must not stay stuck, which is the failure mode a human would meet as a server that will not shut down.

Every case needs a bound: `WaitForPause()` must not be the thing that hangs the suite. If the harness does not already offer a timeout around a script, a `timeoutSeconds` entry is the existing mechanism - and the script should be written so that a failure reads as "it never paused" rather than as a harness timeout.
