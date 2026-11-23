description: Nothing in the test suite has ever stopped a running program at a breakpoint, so the half of the debugger that reads a paused program - its call stack, its variables - has never run. Add a test script that sets a breakpoint, has it hit, and checks what the debugger then reports.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugLibrary.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/DAE/Debug/Debugger.cs, src/engine/DAE/Debug/DebugNodes.cs, src/engine/DAE/Debug/Debug.d4, src/engine/DAE/Debug/Breakpoint.cs, src/engine/DAE/Debug/CallStackEntry.cs, src/engine/DAE/Debug/DebugLocator.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime/Program.cs, docs/architecture/sessions-processes-plans.md
difficulty: hard
----
## What this is

The engine has a debugger: a D4 session can start one, attach another process to it, stop that process at a breakpoint, read its call stack and its variables, step it, and let it run on. The IDE's debug window is the only thing that drives it (`src/windows/Dataphoria/Debug/Debugger.cs`), and the test suite reaches only the first half - starting a debugger, attaching and detaching a process, listing both. **No test in this repository has ever hit a breakpoint.** Everything that only exists once a program is paused is unreached: `src/engine/DAE/Debug/Breakpoint.cs` and `src/engine/DAE/Debug/CallStackEntry.cs` are at exactly 0 %, and about two thirds of `Debugger.cs` and `DebugNodes.cs` with them (35.7 % line coverage over the area's 894 coverable lines, measured on the full three-part run of 2026-11-21 at commit `f6200026`, recorded in `docs/coverage-report.md`).

This ticket adds **one new D4 coverage script**, `DebugBreakpoints.d4`, that sets a breakpoint in an operator, runs a second process that calls it, and asserts on what the debugger reports about the paused program. Two sibling tickets follow it: `debt-debugger-stepping-script` (stepping, pausing a running process, break-on-exception, and the teardown paths) and `debt-debugger-coverage` (session-level debugging, which needs a second session and therefore an NUnit test).

`DebugLibrary.d4` is the script this one sits beside and the shape to follow: start a debugger, start a second process on the same session, attach it. Read it first.

## Do not reuse what `DebugTest.d4` does

`DebugTest.d4` is the script that was meant to cover this and cannot be the model. Its `while` loop increments and then decrements its counter, so it never ends - the manifest records it as a known failure with a 5 s timeout and the note "infinite loop by design: a debugger stepping exercise" - and it calls `Debug.GetCallStack(45596481)` and `Debug.GetStack(14000148, 0)` with process identifiers from whatever session its author had open in 2009. **`test-pre-3-1-scripts-policy` owns that file.** Do not edit, retire or delete it, and do not change its manifest entry.

## How a breakpoint is addressed, and why it can be done from a script

This is the part that is not obvious from reading the operators, and it is what makes a D4 script workable. Five facts, each verified in the source:

**A breakpoint is a locator, a line and a line position**, compared in `Debugger.ShouldBreak` against the running program's current location. There are three kinds of locator. A **program locator**, `program:<guid>`, is what dynamic or ad-hoc execution gets - a fresh GUID every time (`Program.cs`, `SetSourceContext`) - so a breakpoint can never be placed in the text of a script passed to `ExecuteAsync`. A **document locator**, `doc:<Library>:<Document>`, is what the Frontend library's `ExecuteScript(library, document)` operator sets (`libraries/Frontend/Source/Operators/Operators.cs`, around line 328) and what the IDE's editor uses. An **operator locator**, `operator:<OperatorName>(<Signature>)`, is what an operator created by a script with no document locator gets. **So the breakpoint has to go in a persisted operator**, and the script creates one.

**The script does not have to guess the operator's locator: the catalog publishes it.** The `Operators` table (`src/engine/DAE/Schema/SystemCatalog.d4`, around line 1583) has `Locator`, `Line` and `LinePos` columns, written from the operator's own locator (`CatalogStore.cs`, around line 2340). So

```
var LLocator := (Operators where OperatorName = 'TestDebugTarget')[].Locator;
```

gives the exact string, with no dependence on how the signature is spelled.

**Line numbers inside such an operator are relative to its `create operator` line, 1-based.** When a script creates an operator and the script has no document locator, the operator's locator line is `0` (`Compiler.ProcessSourceContext`, around line 6427), and the compiler sets `plan.CompilingOffset` so that every plan node in the body carries a line relative to the operator's first line (`Compiler.cs` around line 6401, `PlanNode.SetLineInfo` around line 455). At run time `Program.GetLocation` combines the frame's locator with the node's line through the `DebugLocator(DebugLocator, int, int)` constructor, and because the operator locator's line is `0` that constructor returns the node's relative line unchanged. So line 1 is the `create operator` line, and a breakpoint on the third line of the declaration is line `3`. Assert that `(Operators where OperatorName = 'TestDebugTarget')[].Line` is `0` as part of the script: that zero is what the convention rests on.

**Pass `-1` for the line position.** `Debugger.ShouldBreak` treats `-1` as "any position on this line", and `-1` is what the IDE passes (`src/windows/Dataphoria/TextEditor/D4Editor.cs`, around line 798). The position the engine reconstructs is one less than the node's 1-based position, because that same `DebugLocator` constructor subtracts one; do not depend on it and do not "fix" it here.

**Only statement nodes are breakable.** `ImperativeNodes.cs` sets `IsBreakable = true` on statement nodes; expression nodes never yield. The breakpoint line must be a statement line in the operator body.

**If `Operators.Locator` turns out not to hold what this says** - check it first, with a one-line query - there is a second route that needs no catalog read: `Debug.SetBreakOnStart(true)` makes an attached process break at the first statement of its program (`Program.ReportStart`), and `Debug.StepInto` repeatedly from there eventually puts the current location inside the called operator, where `Debug.GetProcesses()` reports its locator. That is slower to write, so try the catalog first; `debt-debugger-stepping-script` covers `SetBreakOnStart` either way.

## What the script must do

Shape: one session (the script's own), one extra process, one operator. The operator must be **pure computation on integers** - no table access - so that pausing it cannot hold a lock the script's own process then waits for, and so that it finishes on its own even if every debugger call does nothing. Give it a parameter and at least two local variables and four or five statements, so there is something for the stack window to show and room to pause in the middle.

Create the operator **at the top level of the script, not inside a `begin … end` block.** Each top-level batch runs in its own implicit transaction; an operator created inside a block is not committed until the block ends, and the second process has its own transaction context and would not resolve it. (`AsyncExecution.d4` and `SecurityLibrary.d4` carry `preamble: "SetUseImplicitTransactions(false);"` for the same family of reason; reach for it only if the top-level placement is not enough, and say so in the manifest note.)

The cases:

- **A breakpoint can be set and listed.** `Debug.ToggleBreakpoint(LLocator, LLine, -1)` returns true, and `Debug.GetBreakpoints()` then has exactly that row. Toggling the same three values again returns false and `Debug.GetBreakpoints()` is empty. This is all of `Breakpoint.cs` and the `GetBreakpoints` node.
- **The breakpoint is hit.** With the breakpoint set, `StartProcess()`, `Debug.AttachProcess(LProcessID)`, `ExecuteAsync(LProcessID, 'TestDebugTarget(1);')`, then wait (see *Waiting*) until the row for that process in `Debug.GetProcesses()` has `IsPaused` true. Assert `Locator` is `LLocator`, `Line` is `LLine`, and `DidBreak` is true. `DidBreak` is what tells a breakpoint hit apart from a pause the debugger asked for.
- **`Debug.WaitForPause()` returns.** Call it once, *after* the wait above has already shown the pause, so it returns at once and is covered without being the thing that blocks.
- **The paused program's call stack reads.** `Debug.GetCallStack(LProcessID)` has at least two rows; the innermost (`Index = 0`) has `Location` equal to `LLocator` and `Line` equal to the paused line, and `Statement` is not empty; a row further out has a `Location` that starts with `program:`. This is `CallStackEntry.cs` and the `GetCallStack` node.
- **The paused program's variables read.** `Debug.GetStack(LProcessID, 0)` - window `0` is the innermost frame - has a row whose `Value` is the argument the caller passed. **Assert on values and on the row count, not on names:** the `Name` column is filled with `Location0`, `Location1`, … positionally (`Debugger.GetStack`), not with the D4 identifiers, and that is what the IDE shows today. Put a `NOTE:` in the script saying so, so the next reader does not take it for a bug in the test.
- **`Debug.GetSource` answers for both locator kinds.** The call-stack rows give both: `Debug.GetSource` of the operator locator re-emits the operator's statement (assert it contains the operator's name), and `Debug.GetSource` of the `program:` locator returns the script text the process is running (assert it contains `TestDebugTarget`). Both need the process paused - `GetProgram` goes through `CheckPaused`.
- **`Debug.Run()` lets it finish.** After `Run()`, the process's row in `Debug.GetProcesses()` stops reporting `IsPaused`.
- **A cleared breakpoint does not stop anything.** Toggle the breakpoint off, run the same call on the process again, and assert the process does *not* pause within the same wait budget - then stop the process and the debugger normally. A breakpoint that cannot be cleared is as bad as one that cannot be set.

End with `Debug.Stop()` and the assertion `DebugLibrary.d4` already makes - that `Debug.GetDebuggers()` no longer has a row for this session.

## Waiting

**Do not use `Debug.WaitForPause()` as the wait.** It has no timeout: if nothing ever pauses, the case becomes a bare `HarnessTimeout` with nothing in it to read. Poll instead, in the idiom `AsyncExecution.d4` already uses - `Sleep(50)` in a `while` with a counter and a budget - and `raise Error('… never paused')` when the budget runs out, so the failure says which case failed and why. A few seconds per wait is enough; the pause happens within milliseconds when it happens at all.

Two traps in what to poll:

- **Poll the process, not the debugger.** `Debug.GetDebuggers().IsPaused` is `Debugger.IsPaused`, which is `_isPauseRequested && (GetRunningCount() == _pausedCount)`; with no process attached both counts are zero, so a debugger that still has a pause requested reports itself paused. Use the `IsPaused` column of the process's row in `Debug.GetProcesses()`. Put a `NOTE:` in the script at the one place it matters.
- **`DidBreak`, `Locator`, `Line` and `LinePos` are nil for a process that is not paused**, not false and not zero (`DebugGetProcessesNode.InternalExecute`), so guard every read of them with the `IsPaused` test rather than reading them unconditionally.

Set `timeoutSeconds` on the manifest entry to `60`: the script's own budgets are the real guard and should fire first, and the entry's default of 120 s only delays a diagnosis. Say that in the entry's note.

## Edge cases & interactions

- **The operator must not touch a table.** Verified by inspection of the script. A paused process holds whatever it has acquired; the script's own process then reads the catalog and the `Debug.*` tables, and a table lock held by the paused process could deadlock the two. Integer arithmetic only.
- **The second process is on the script's own session.** `Debug.AttachSession(SessionID())` is refused by design (`DebugLibrary.d4` asserts the refusal), so the attach must be `Debug.AttachProcess` on a process from `StartProcess()`.
- **`ExecuteAsync(processID, script)` prepares the script on the calling thread and executes it on a worker, and does not stop the process afterwards** (`SystemExecuteAsyncNode`, `shouldCleanup` false). So the script owns stopping that process, in a `finally`, as `DebugLibrary.d4` does.
- **An error raised inside the async script is not returned to the caller** (the same node's note, and `AsyncExecution.d4`'s header). If the operator call fails, nothing surfaces except the pause never happening - which is exactly why the wait must raise its own named error.
- **Stopping a paused process.** The script's `finally` may run while the process is still paused. `Engine.TerminateProcessThread` detaches a paused process from its debugger to release it, and if that did not work it spins for half of `ProcessTerminationTimeout` (15 s of the 30 s default) and raises `ProcessNotResponding`. So a `StopProcess` that returns without error is itself the check that the process was released; `debt-debugger-stepping-script` makes that an assertion of its own.
- **Toggling a breakpoint is an exact three-value match.** `Breakpoint.Equals` compares locator, line and line position, so toggling off must pass the same `-1` it was set with. A mismatch silently adds a second breakpoint instead of removing the first - which the `Debug.GetBreakpoints()` emptiness assertion catches.
- **The run with the breakpoint cleared must still be bounded.** Give it the same wait budget and assert the absence of a pause at the end of it, rather than sleeping a fixed time and hoping.
- **No case may inherit another's state.** The script runs on a server of its own, so there is no leakage between scripts; within the script, clear each breakpoint and detach each process before the next case, and let the final `Debug.Stop()` be the only thing that relies on the debugger's own teardown.
- **Part 2 of the three-part run grows.** The coverage runner splits scripts by first letter and `DebugBreakpoints` lands in part 2 (`[A-Ra-r]`), which took about 3.5 minutes at best. One more script of a few seconds is not a concern; record the measured case time in the handoff so the next person has it.

## What this does not cover, and must not be invented here

- **`Debug.RunTo`, `Debug.StepIntoSpecific` and `Debug.GetStepIntoOperators` do not exist.** They are commented out in both `src/engine/DAE/Debug/Debug.d4` and `src/engine/DAE/Debug/DebugNodes.cs`, and the IDE calls none of them. The plan ticket that preceded this one listed them among the untested operators; that was wrong - they are unimplemented. Do not test them and do not implement them.
- **`src/engine/DAE/Debug/DebugContext.cs` is dead.** Nothing in the repository constructs `DebugContext`; a grep over the whole tree finds only its own file. Its 11 coverable lines can never be covered by any test, and deleting it is an arm on `debt-ws12-hygiene-remainder` (`hygiene-remove-dead-compiled-files`). Expect the Debugger area to keep 11 permanently uncovered lines until that lands.
- **Session-level debugging** (`Debug.AttachSession` of another session, `Debug.DetachSession`, `Debug.GetSessions`) needs a second session, which no D4 operator hands a script - `DebugLibrary.d4`'s own commented-out block says as much. `debt-debugger-coverage` owns it, as an NUnit test.
- **Do not hand-edit `docs/coverage-report.md`.** Everything below its *Line coverage per assembly* heading is generated and replaced wholesale by the next measured run; its head records the run it came from.

## Bugs this may surface

The paused paths have never run, so some may not work. Two that are worth knowing about before they are met:

- `DebugGetProcessesNode` assigns the paused process's CLR `Exception` into a D4 `Error` column (`row[6] = process.Error`). Nothing has ever read that column with a value in it. If the assignment fails, it fails on the break-on-exception case, which belongs to `debt-debugger-stepping-script` - but this script reads the same row, so note it if it appears here.
- `Debugger.GetStack` temporarily clears the target process's debugger around its read and restores it in a `finally`, to evaluate without yielding. That reads correctly; if it ever leaves the debugger cleared, the symptom is a process that stops pausing after the first `GetStack`, which this script's later cases would show.

A defect with a local, obvious fix is fixed here with its reproduction, per the project rules; one whose fix changes a design or a compatibility surface becomes its own ticket.

## Architecture text

None of this is written down anywhere. Add a short **Debugger** section to `docs/architecture/sessions-processes-plans.md` (299 words today, limit 1,500 - it owns processes and plan-node execution, which is where the debugger lives): a process yields to its debugger at every breakable plan node (`PlanNode.Execute` → `Program.Yield` → `Debugger.Yield`); the debugger pauses by holding every attached process in that yield; a location is a locator plus a line, with the three locator kinds and the rule that an operator locator's zero line means its lines are relative to its own `create operator` line; and the stack window reports positional names rather than D4 identifiers. Around 200-250 words. Run `python tools/docs-size-check/check_size.py` afterwards. Nothing goes in the short root `docs/architecture.md`.

## TODO

- Read `DebugLibrary.d4`, `src/engine/DAE/Debug/Debug.d4` and `src/windows/Dataphoria/Debug/Debugger.cs`; confirm with one query that `(Operators where OperatorName = '…')` carries `Locator` and a `Line` of `0` for an operator a script created.
- Write `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` with the cases above, the bounded poll helper, and the two `NOTE:` lines (debugger `IsPaused` with no attached process; `GetStack` positional names).
- Add its entry to `tests/DAE.Server.Tests/coverage-manifest.json` - `pass`, `timeoutSeconds: 60`, with a note saying why the timeout is lower than the default. Entries are sorted by script name in ordinal order; `DebugBreakpoints` goes before `DebugLibrary`.
- Add the **Debugger** section to `docs/architecture/sessions-processes-plans.md` and run `python tools/docs-size-check/check_size.py`.
- Build (`dotnet build Dataphor.slnx`), then run the targeted tests: `dotnet test tests/DAE.Server.Tests --filter "Name=DebugBreakpoints|Name=DebugLibrary|Name=AsyncExecution|Name=Exceptions" --logger "console;verbosity=normal"`. Name the filters in the handoff, with the new case's measured time.
- Hand off to `review/` with: what the script asserts, anything the paused paths turned out to get wrong, whether the catalog route or the step-into fallback was used, and which files you expect the next coverage measurement to move (and that `DebugContext.cs` cannot).
