description: The debugger can be attached to a whole login rather than to one running task, so that everything that login starts afterwards is debugged too. Nothing has ever tested that, because a test script cannot open a second login. Add a test that can.
prereq: debt-debugger-stepping-script
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/DebuggerSessionTest.cs, tests/DAE.Server.Tests/InProcessTestFixture.cs, tests/DAE.Server.Tests/TestServerInstance.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugLibrary.d4, src/engine/DAE/Debug/Debugger.cs, src/engine/DAE/Debug/DebugNodes.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerSession.cs
difficulty: medium
----
## What this is

The last of three tickets closing the debugger's test gap. `debt-debugger-breakpoint-script` and `debt-debugger-stepping-script` cover everything a single login can do from a D4 script: breakpoints, reading a stopped program, stepping, and the teardown paths. What is left is **session-level debugging** - attaching the debugger to another login so that every task that login is already running, and every task it starts afterwards, is debugged without being named one at a time. That is the IDE's "attach to session" (`src/windows/Dataphoria/Debug/Debugger.cs`, `AttachSession`/`DetachSession`), and it is at exactly 0 %: `Debug.GetSessions()` (38 lines in `DebugNodes.cs`), `Debugger.GetSessions`, `Debugger.AttachSession` and `Debugger.DetachSession`, and with them the three lines in the `ServerProcess` constructor that attach a newly started task to the debugger its session is already attached to.

**A D4 coverage script cannot reach this.** `Debug.AttachSession` needs the identifier of a session other than the debugger's own - attaching the debugger to its own session is refused by design, which `DebugLibrary.d4` already asserts - and no D4 operator opens a session and hands back its identifier. `ExecuteMultiple` opens sessions but keeps them to itself; `ExecuteAsync` and `StartProcess` both work inside the caller's session. `DebugLibrary.d4` has the real test commented out with its author's note: *"This needs to be run on a separate session... Probably needs to be managed by an NUnit test directly."* That note is correct, and this ticket follows it.

So this one is an NUnit fixture, not a script. That is a deliberate exception to the project's preference for a D4 script at the lowest layer: the thing under test is the relationship between two sessions, and only the harness can hold two.

## The shape

A new fixture `DebuggerSessionTest` on `InProcessTestFixture` (`tests/DAE.Server.Tests/InProcessTestFixture.cs`). The base fixture gives one server, and one session and process per test; a second session comes from `Instance.Connect()` with its own `StartProcess`. The debugger is driven exactly as the IDE drives it - by executing D4 on the debugger session's process (`Debug.Start()`, `Debug.AttachSession(<id>)`, …) - so the fixture tests the same surface an application reaches and not a C# back door.

Waits are NUnit's, not hand-rolled: a bounded poll with a named failure message, so that "it never paused" reads as an assertion and not as a hung test. Nothing here needs more than a second or two.

## What to test

One case per behaviour, four in all:

- **Attaching a session attaches the tasks it is already running.** Session B starts a process and is given long-enough work to still be running; session A starts a debugger and attaches session B; `Debug.GetSessions()` on A then lists B's identifier, and `Debug.GetProcesses()` lists B's running process. This is `Debugger.AttachSession`, which walks the session's processes, and the `GetSessions` node.
- **A task started after the attach is attached too.** With session B attached, B starts a *new* process; `Debug.GetProcesses()` on A lists it without A naming it. This is the auto-attach in the `ServerProcess` constructor (it reads `ServerSession.DebuggedByID` and attaches through `Engine.GetDebugger`), which nothing reaches today, and it is the whole point of attaching a session rather than a process.
- **Detaching a session detaches its tasks.** `Debug.DetachSession(<B>)` on A: `Debug.GetSessions()` is empty, `Debug.GetProcesses()` no longer lists either of B's processes, and `ServerSession.DebuggedByID` is back to zero - observable as a *third* process started on B afterwards not appearing in `Debug.GetProcesses()`. Assert through the operators, not by reading the field.
- **Stopping the debugger detaches the session.** With session B still attached, `Debug.Stop()` on A: `Debugger.Dispose` detaches every session it holds before it disposes them. Afterwards a new process on B is not debugged, and B's processes can be stopped and B disconnected without error. The failure this guards against is a session left marked as debugged by a debugger that no longer exists, which would make every later process on it fail to attach.

A breakpoint actually being hit through a session attach is **not** needed here: `DebugBreakpoints.d4` already covers the pause machinery, and repeating it at this layer would only re-test what the scripts hold. The behaviour this fixture owns is which processes end up attached, and when.

## Edge cases & interactions

- **Two sessions in one server, and the test must not confuse them.** `Debug.AttachSession(SessionID())` is refused (`ServerException.Codes.*`, asserted in `DebugLibrary.d4`); the fixture must pass B's identifier, read from B's own `SessionInfo`/session object, never from a scan of the `Sessions` table that could pick up the System session.
- **Never attach the System session.** The server runs its own internal session, and attaching it would stop server work the test then waits for. The identifier must come from the session the fixture opened.
- **Teardown must not leave a debugged session behind.** Every case stops the debugger in a `finally` (or the fixture's `TearDown`) before disconnecting either session, because a session whose `DebuggedByID` points at a disposed debugger is the exact defect the last case is about. Dispose order matters: the base fixture stops its own process and disconnects its session in `TearDown`, so the second session's cleanup belongs to the test that opened it.
- **A paused process blocks its own disconnect.** If a case leaves a process paused, disconnecting its session has to go through `Engine.TerminateProcessThread`, which detaches a paused process and otherwise spins for half of `ProcessTerminationTimeout` (15 s of the 30 s default) before raising `ProcessNotResponding`. Keep these cases away from pauses - the work session B runs should be a bounded loop that finishes on its own - so a failure is an assertion and not a 15-second stall.
- **`Debug.GetSessions()` returns nothing rather than failing when there is no debugger**, because the node checks `ServerSession.Debugger` for null before asking (`DebugGetSessionsNode`), while `Debug.GetProcesses()` does the same and most of the other operators go through `CheckedDebugger` and raise. Assert the empty result rather than an error for `GetSessions` after `Debug.Stop()`.
- **The fixture's server carries no extra libraries.** Nothing here needs the Frontend or the coverage libraries, so leave `Libraries`/`LibrariesToRegister` empty and keep the fixture's start-up cost to the bare server.
- **This fixture is in part 1 of the three-part run**, which is the part already closest to the runner's idle limit (about 5.5 minutes at best, 7.5 at worst). Four cases on a bare in-process server is a small addition; measure it and say so in the handoff.

## Not in scope

- `Debug.RunTo`, `Debug.StepIntoSpecific` and `Debug.GetStepIntoOperators` **do not exist** - commented out in `src/engine/DAE/Debug/Debug.d4` and `src/engine/DAE/Debug/DebugNodes.cs`, called by nothing. Do not test or implement them.
- **Do not uncomment the block in `DebugLibrary.d4`.** It cannot work from a script, for the reason above. Leave the script as it is; this fixture is what its note asked for. If anything there needs saying, a one-line comment pointing at `DebuggerSessionTest` is enough.
- `DebugTest.d4` belongs to `test-pre-3-1-scripts-policy`.
- `src/engine/DAE/Debug/DebugContext.cs` is dead code that no test can cover; its deletion is an arm on `debt-ws12-hygiene-remainder`.
- Do not hand-edit `docs/coverage-report.md`; its generated half is replaced wholesale by the next measured run.

## TODO

- Read `tests/DAE.Server.Tests/InProcessTestFixture.cs` and `TestServerInstance.cs` for how a second session is opened and cleaned up, and `DebugLibrary.d4` for the commented-out block this replaces.
- Write `tests/DAE.Server.Tests/DebuggerSessionTest.cs` with the four cases, driving the debugger through D4 on the debugger session's process and waiting with bounded polls that fail with their own messages.
- Build (`dotnet build Dataphor.slnx`), then run the targeted tests: `dotnet test tests/DAE.Server.Tests --filter "Name~DebuggerSession" --logger "console;verbosity=normal"`, and the neighbouring debugger cases once, `dotnet test tests/DAE.Server.Tests --filter "Name=DebugBreakpoints|Name=DebugStepping|Name=DebugLibrary"`. Name the filters and the measured times in the handoff.
- Hand off to `review/` with: what the four cases assert, how the second session is cleaned up on a failure, and what of the debugger area is still uncovered after all three tickets (naming `DebugContext.cs` and the three unimplemented operators, so the next coverage pass does not re-file them).
