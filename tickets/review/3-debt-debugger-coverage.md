description: A test now covers attaching the debugger to a whole login, so that everything that login runs is debugged - the one part of the debugger a test script could never reach.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/DebuggerSessionTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Debug/Debugger.cs, docs/architecture/sessions-processes-plans.md, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugLibrary.d4
difficulty: medium
----
## What landed

`tests/DAE.Server.Tests/DebuggerSessionTest.cs`, a new NUnit fixture on `InProcessTestFixture` with four cases, and its `Compile` entry in `DAE.Server.Tests.csproj` (default compile items are off in this repository, so a new source file that is not listed is silently not built). The base fixture gives one server and one session and process per case - that session is the debugger's; the fixture opens a second session of its own in a `[SetUp]` and closes it in a `[TearDown]`. Everything the debugger does is driven by executing D4 on the debugger session's process, the way `src/windows/Dataphoria/Debug/Debugger.cs` drives it, so what is tested is the surface an application reaches.

This is the exception to the project's preference for a D4 coverage script at the lowest layer, for the reason the implement ticket gave and the commented-out block in `DebugLibrary.d4` already said: the thing under test is the relationship between two sessions, and no D4 operator opens a session and hands back its identifier.

## The four cases, and what each one reaches

- **`AttachingASessionAttachesTheTaskItIsAlreadyRunning`** - the second session starts a process and is given a bounded counting loop on a background task; once the engine reports that process as running (polled from the `Processes` catalog table), the debugger session runs `Debug.AttachSession(<id>)`. Asserts, in this order: the task is *still* running (so the case cannot pass on a loop that had already ended), `Debug.GetSessions()` lists the attached session, and `Debug.GetProcesses()` lists its running task. Reaches `Debugger.AttachSession`'s walk of the session's existing processes and the `DebugGetSessionsNode` table node.
- **`ATaskStartedAfterTheAttachIsAttachedToo`** - with the session attached, it starts a *new* process; `Debug.GetProcesses()` lists it without the debugger naming it. Reaches the three lines of the `ServerProcess` constructor that read `ServerSession.DebuggedByID` and attach through `Engine.GetDebugger`. This is the point of attaching a login rather than a task, and the coverage run behind `docs/coverage-report.md` reached none of those lines.
- **`DetachingASessionDetachesItsTasks`** - `Debug.DetachSession(<id>)` after two tasks are attached (one from before the attach, one that attached itself): `Debug.GetSessions()` is empty, neither task is listed any more, and a *third* task started afterwards is not attached either, which is how `DebuggedByID` being back to zero is observed through the operators rather than by reading the field.
- **`StoppingTheDebuggerDetachesTheSession`** - `Debug.Stop()` with a session still attached: no debugger is left, `Debug.GetSessions()` answers empty rather than raising (the `DebugGetSessionsNode` null check, where most of the other operators go through `CheckedDebugger` and raise), a new task on the session starts without error, and the session's tasks stop and the session disconnects without error. The failure this guards against is a session left marked as debugged by a debugger that no longer exists: every task started on it afterwards would fail in its own constructor, because `Engine.GetDebugger` raises `DebuggerNotStarted` for the stopped debugger's session.

No case stops a program. `DebugBreakpoints.d4` and `DebugStepping.d4` hold the pause machinery, and a breakpoint hit through a session attach would only re-test what they already cover; what this fixture owns is which processes end up attached, and when.

## The one thing the implement ticket did not know: only a client's session can be attached

`ServerSession.SetDebuggedByID` refuses a session whose `SessionInfo.CatalogCacheName` is empty, with error 106204, "The debugger cannot be attached to an in-process session". A session opened straight off the engine - which is what `TestServerInstance.Connect` and the base fixture give - has it empty; every session a *client* opens has it set, because `LocalServer.Connect` assigns the client's internal server name to every connection it makes. So the fixture's second session names a catalog cache, and that one line is what makes it stand in for the client session the IDE debugs rather than a way around the check. Verified rather than inferred: with the line commented out, `Debug.AttachSession` fails with exactly that error (the run is in this ticket's history, not kept as a file).

Two consequences a reviewer should know:

- The refusal `DebugLibrary.d4` asserts - `Debug.AttachSession(SessionID())` raising - comes from *this* check under the coverage harness, not from the "cannot attach to the debugger's own session" check (106197) the script's comment implies. The script only asserts that some error was raised, so it passes either way, but 106197 is still at zero and no test distinguishes the two refusals. Reaching it needs a session that has a catalog cache name *and* a debugger of its own; it is one short case in this fixture if the review decides it is worth one. Left out here deliberately: the implement ticket specified four cases, one per behaviour, and this is a fifth.
- Nothing else in the engine branches on `CatalogCacheName` (`src/engine/DAE` reads it in exactly two places: this check, and the `Connection_Name` column the `Sessions` catalog table reports), so naming one on the second session changes nothing else about it.

## Teardown, including on a failure

NUnit runs the base `[SetUp]` before the derived one and the derived `[TearDown]` before the base one, which is exactly the required order: the second session is opened after the fixture's own and closed before it. The derived teardown, which runs however the case ended, first stops the debugger if one is still started (`if exists (Debug.GetDebuggers() where Session_ID = SessionID()) then Debug.Stop();`) and only then closes the second session, in a `finally` so a failure in the first step cannot skip the second. That order is the point: a session whose `DebuggedByID` still points at a debugger is one nothing else detaches, and disconnecting it first would leave the debugger holding a disposed session.

Closing the second session stops every process the fixture started on it - they are recorded as they are started, so no case has to remember - and disconnects it, and is idempotent, because the fourth case does it itself as part of its own assertion. The one background task is waited for by the case that started it, with a named failure if the loop never ends; in the teardown it is only glanced at and reported to the test output, where it cannot hide the case's own failure (stopping a process aborts whatever it is running, so a loop still going there is about to be aborted anyway).

No case leaves a process paused, deliberately: `Engine.TerminateProcessThread` detaches a paused debugged process but otherwise spins for half of `ProcessTerminationTimeout` before raising `ProcessNotResponding`, so a paused leftover would turn a failure into a 15-second stall.

## Evidence the cases are load-bearing

Each case was checked against the code it claims to cover, by breaking that code temporarily and confirming the failure (both mutations reverted; `git diff` of `ServerProcess.cs` is empty and `Debugger.cs` carries only the comment described below):

- Auto-attach disabled in the `ServerProcess` constructor: cases 2, 3 and 4 fail, case 1 passes - correctly, since its task existed before the attach and comes in through `AttachSession`'s walk.
- The session-detach loop disabled in `Debugger.Dispose`: only case 4 fails, with its own message, "Starting a task on the session after the debugger was stopped failed, so stopping the debugger left the session marked as debugged."

## What was run, and what it cost

Built first, `dotnet build Dataphor.slnx`: 0 errors, no new warnings from the new file.

- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~DebuggerSessionTest" --logger "console;verbosity=normal"` - 4 passed. Five runs: the whole run 6.2 to 7.1 s wall clock, of which about 1.1 s is case time (the running-task case 0.85 to 1.0 s, the other three 5 to 65 ms each); the rest is `dotnet test` start-up and the fixture's own in-process server.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "Name=DebugBreakpoints|Name=DebugStepping|Name=DebugLibrary"` - 3 passed, 5 s, 10 s and 2 to 3 s, about 23 s for the run. These are the neighbours of the one engine file this ticket touched.

**The filter the implement ticket suggested, `--filter "Name~DebuggerSession"`, matches nothing**: NUnit's `Name` is the test's own name, and these four case names do not contain "DebuggerSession". Use `FullyQualifiedName~DebuggerSessionTest`, as above.

Part 1 of the three-part run was not re-measured - two runs of it would be most of the budget the runner allows, and the project's rule puts the full suite with CI. The isolated figure above is the honest estimate of what part 1 grows by: about a second of case time plus one more bare in-process server start.

## Tripwires parked

- `src/engine/DAE/Debug/Debugger.cs`, at `AttachSession`: marking the session and attaching the processes it already has are not atomic with respect to a process being started on that session at the same moment. `ServerSession.StartProcess` attaches the new process from the `ServerProcess` constructor, which reads `DebuggedByID`, and only then adds it to the session's list - so a process constructed just before the mark is set is neither self-attached nor found by the walk, and one constructed during `DetachSession` can attach itself to a session that has just been detached. Fine now, because an attach is a person clicking in the IDE and the cost of losing the race is one task not debugged; the `NOTE:` says what would close the window if that ever changes. Found by reading the two code paths, not observed.
- `tests/DAE.Server.Tests/DebuggerSessionTest.cs`, at `RunningWork`: the fixture's one timing assumption, a million-iteration loop (about a second here) against the window it must outlast (one `Debug.AttachSession` and one read of the `Processes` table, tens of milliseconds). If a faster machine ever closes that gap, the case fails on its own assertion saying so, and the fix is in the `NOTE:`.

## Documentation

`docs/architecture/sessions-processes-plans.md` gains one paragraph in its Debugger section: that attaching a session works in both directions (the processes it already has, and the ones it starts afterwards, each attaching itself), that detaching and stopping the debugger reverse both, and that only a session a client opened can be attached at all, with the reason. The page is at 1,273 of its 1,500 words; `python tools/docs-size-check/check_size.py` reports 11 files checked, 0 over the limit. `tools/docs-link-check/check_links.py` reports no error in the architecture set (its 203 errors are all pre-existing, in `docs/users-guide` and `docs/reference`).

`DebugLibrary.d4` gains two comment lines inside the block that was already commented out, saying where those cases now live. The block itself is untouched, as the implement ticket required.

No `docs/migration-notes.md` entry: nothing here changes a surface an application, a deployment script or a C# extension could notice - a new test file, one project `Compile` entry and three comments. `docs/coverage-report.md` is untouched; its generated half belongs to the next measured run.

## What is still uncovered in the debugger area, after all three tickets

Carried forward so the next coverage pass does not re-file any of it:

- `src/engine/DAE/Debug/DebugContext.cs` - dead code that no test can cover; its deletion is an arm on `debt-ws12-hygiene-remainder`.
- `Debug.RunTo`, `Debug.StepIntoSpecific` and `Debug.GetStepIntoOperators` - **do not exist**, commented out in both `Debug.d4` and `DebugNodes.cs` and called by nothing, the IDE included. Not to be tested or implemented.
- The waiting loop inside `Debugger.WaitForPause` (`_waitSignal.WaitOne(500)` then `program.Yield`) - needs a caller that waits while nothing is yet stopped. Named by the review of `debt-debugger-stepping-script`.
- `Debug.GetStack` with a window index other than 0 - an outer frame, or a negative one. Same source.
- A breakpoint whose `LinePos` is a real position rather than `-1`. Same source.
- `ServerException.Codes.CannotAttachToDebuggerSession` (106197), for the reason in the section above: the one assertion that looks like it covers it actually trips the in-process check.

## Known gaps in this work

- The four cases assert *which* processes are attached, never that a session attach leads to a stop. That is deliberate and argued above, but it does mean nothing tests that a breakpoint set by the debugger session fires in a task the attach brought in - the two mechanisms are only tested separately.
- The running-task case is the only one with a timing assumption, and it is the only case whose failure could be a slow machine rather than a defect. Its message says so.
- `Debug.GetProcesses()` reports its `IsPaused`, `Locator`, `Line`, `LinePos`, `DidBreak` and `Error` columns only for a process that is running *and* paused, so every row this fixture reads takes the null branch of `DebugGetProcessesNode`. The populated branch is `DebugBreakpoints.d4`'s and `DebugStepping.d4`'s.
