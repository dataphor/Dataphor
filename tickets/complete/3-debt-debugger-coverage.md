description: A test now covers attaching the debugger to a whole login, so that everything that login runs is debugged - the one part of the debugger a test script could never reach.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/DebuggerSessionTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, src/engine/DAE/Debug/Debugger.cs, docs/architecture/sessions-processes-plans.md, tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugLibrary.d4
----
## What landed

`tests/DAE.Server.Tests/DebuggerSessionTest.cs`, an NUnit fixture on `InProcessTestFixture` with six cases, and its `Compile` entry in `DAE.Server.Tests.csproj` (default compile items are off in this repository, so a source file that is not listed is silently not built). The base fixture gives one server and one session and process per case - that session is the debugger's; the fixture opens a second session of its own in a `[SetUp]` and closes it in a `[TearDown]`. Everything the debugger does is driven by executing D4 on the debugger session's process, the way `src/windows/Dataphoria/Debug/Debugger.cs` drives it, so what is tested is the surface an application reaches.

This is the exception to the project's preference for a D4 coverage script at the lowest layer, for the reason `ticket(implement): debt-debugger-coverage` gave and the commented-out block in `DebugLibrary.d4` already said: the thing under test is the relationship between two sessions, and no D4 operator opens a session and hands back its identifier.

## The six cases, and what each one reaches

Four landed in the implement stage:

- **`AttachingASessionAttachesTheTaskItIsAlreadyRunning`** - the second session starts a process and is given a bounded counting loop on a background task; once the engine reports that process as running (polled from the `Processes` catalog table), the debugger session runs `Debug.AttachSession(<id>)`. Asserts, in this order: the task is *still* running (so the case cannot pass on a loop that had already ended), `Debug.GetSessions()` lists the attached session, and `Debug.GetProcesses()` lists its running task. Reaches `Debugger.AttachSession`'s walk of the session's existing processes and the `DebugGetSessionsNode` table node.
- **`ATaskStartedAfterTheAttachIsAttachedToo`** - with the session attached, it starts a *new* process; `Debug.GetProcesses()` lists it without the debugger naming it. Reaches the three lines of the `ServerProcess` constructor that read `ServerSession.DebuggedByID` and attach through `Engine.GetDebugger`. This is the point of attaching a login rather than a task, and the coverage run behind `docs/coverage-report.md` reached none of those lines.
- **`DetachingASessionDetachesItsTasks`** - `Debug.DetachSession(<id>)` after two tasks are attached (one from before the attach, one that attached itself): `Debug.GetSessions()` is empty, neither task is listed any more, and a *third* task started afterwards is not attached either, which is how `DebuggedByID` being back to zero is observed through the operators rather than by reading the field.
- **`StoppingTheDebuggerDetachesTheSession`** - `Debug.Stop()` with a session still attached: no debugger is left, `Debug.GetSessions()` answers empty rather than raising (the `DebugGetSessionsNode` null check, where most of the other operators go through `CheckedDebugger` and raise), a new task on the session starts without error, and the session's tasks stop and the session disconnects without error. The failure this guards against is a session left marked as debugged by a debugger that no longer exists: every task started on it afterwards would fail in its own constructor, because `Engine.GetDebugger` raises `DebuggerNotStarted` for the stopped debugger's session.

Two were added by the review, each for a contract the implement stage left unverified - see `## Review findings`:

- **`DisconnectingADebuggedSessionDetachesItFromTheDebugger`** - the reverse of the case above: disconnect the *attached* session while the debugger lives, and the debugger lists neither it nor its task and still stops cleanly. No code in `Debugger.cs` performs this detach; it falls out of `ServerChildObjects` (`src/engine/DAE/Server/ServerObject.cs`), whose collections subscribe to each child's `Disposed` event and disown it when it fires. `DebugLibrary.d4` already asserts the *process* half of that ("Disposed process did not detach"); this is the session half, which needs a second session and so can only be asserted here.
- **`ADebuggerRefusesToAttachTheSessionItRunsOn`** - a debugger started on the second session, attaching that same session, is refused with `ServerException.Codes.CannotAttachToDebuggerSession` (106197). Asserts the error *code*, which is the whole point of the case: the block in `DebugLibrary.d4` that looks like this one trips a different guard and asserts only that something was raised.

No case stops a program. `DebugBreakpoints.d4` and `DebugStepping.d4` hold the pause machinery, and a breakpoint hit through a session attach would only re-test what they already cover; what this fixture owns is which processes end up attached, and when.

## Only a client's session can be attached

`ServerSession.SetDebuggedByID` refuses a session whose `SessionInfo.CatalogCacheName` is empty, with error 106204, "The debugger cannot be attached to an in-process session". A session opened straight off the engine - which is what `TestServerInstance.Connect` and the base fixture give - has it empty; every session a *client* opens has it set, because `LocalServer.Connect` assigns the client's internal server name to every connection it makes. So the fixture's second session names a catalog cache, and that one line is what makes it stand in for the client session the IDE debugs rather than a way around the check. Verified twice, by implement and again by review: with the line commented out, all six cases fail, and the refusal case fails saying the refusal was the in-process one.

Nothing else in the engine branches on `CatalogCacheName` - `src/engine/DAE` reads it in exactly two places, this check and the `Connection_Name` column the `Sessions` catalog table reports - so naming one on the second session changes nothing else about it. (The *remote server* layer, `src/engine/Server/Server/CatalogCache.cs`, keys its catalog caches by the same name, but an in-process session never reaches that layer.)

## Teardown, including on a failure

NUnit runs the base `[SetUp]` before the derived one and the derived `[TearDown]` before the base one, so the second session is opened after the fixture's own and closed before it. The derived teardown, which runs however the case ended, first stops the debugger if one is still started (`if exists (Debug.GetDebuggers() where Session_ID = SessionID()) then Debug.Stop();`) and only then closes the second session, in a `finally` so a failure in the first step cannot skip the second.

Either order is in fact safe - that is what the review's first new case established, and the comment that used to claim otherwise was corrected. This order is kept because it is the reverse of the setup and keeps the debugger's lifetime inside the debugged session's.

Closing the second session stops every process the fixture started on it - they are recorded as they are started, so no case has to remember - and disconnects it, and is idempotent, because two cases do it themselves as part of their own assertions. The one background task is waited for by the case that started it, with a named failure if the loop never ends; in the teardown it is only glanced at and reported to the test output, where it cannot hide the case's own failure (stopping a process aborts whatever it is running, so a loop still going there is about to be aborted anyway).

No case leaves a process paused, deliberately: `Engine.TerminateProcessThread` detaches a paused debugged process but otherwise spins for half of `ProcessTerminationTimeout` before raising `ProcessNotResponding`, so a paused leftover would turn a failure into a 15-second stall.

## Evidence the cases are load-bearing

Implement checked the first four by breaking the code they claim to cover and confirming the failure; review re-ran the third of those mutations and added one of its own. All reverted - the only engine change in the final diff is the comment described below.

- Auto-attach disabled in the `ServerProcess` constructor: cases 2, 3 and 4 fail, case 1 passes - correctly, since its task existed before the attach and comes in through `AttachSession`'s walk.
- The session-detach loop disabled in `Debugger.Dispose`: only case 4 fails, with its own message, "Starting a task on the session after the debugger was stopped failed, so stopping the debugger left the session marked as debugged."
- `CatalogCacheName` not set on the second session: all six fail, and `ADebuggerRefusesToAttachTheSessionItRunsOn` fails with "Attaching the debugger's own session was refused for the wrong reason: ... The debugger cannot be attached to an in-process session", which is what distinguishes the two refusals.

`DisconnectingADebuggedSessionDetachesItFromTheDebugger` was not mutation-checked - the detach it tests comes from a collection every engine object uses, so disabling it would break the whole suite rather than that one case. Its first assertion is the guard instead: it fails if the debugger is not holding the task at the moment the session is disconnected, so the case cannot pass vacuously.

## Documentation

`docs/architecture/sessions-processes-plans.md` gains one paragraph in its Debugger section: that attaching a session works in both directions (the processes it already has, and the ones it starts afterwards, each attaching itself); that detaching, stopping the debugger and disconnecting the attached session each reverse both, the last of those through the collections in `ServerObject.cs` rather than through any code in the debugger; that a debugger never attaches the session it runs on, and why; and that only a session a client opened can be attached at all, with the reason. The page is at 1,324 of its 1,500 words; `python tools/docs-size-check/check_size.py` reports 11 files checked, 0 over the limit, and `tools/docs-link-check/check_links.py` reports no error under `docs/architecture/` (its 203 errors are all pre-existing, in `docs/users-guide` and `docs/reference`).

`DebugLibrary.d4` gains comments in two places, and no assertion changed. Inside the block that was already commented out, two lines saying where those cases now live. Above the "attaching your own session fails" block, a corrected comment: it used to be labelled `// Cannot attach to debugger session.`, which names a refusal that block does not actually reach.

No `docs/migration-notes.md` entry: nothing here changes a surface an application, a deployment script or a C# extension could notice - a new test file, one project `Compile` entry, and comments. `docs/coverage-report.md` is untouched; its generated half belongs to the next measured run.

## Review findings

The implement-stage diff was read before the handoff summary. Everything the handoff asserts about the engine was re-derived from the source rather than taken on trust: `Debugger.Attach`, `Detach`, `AttachSession`, `DetachSession`, `Dispose`, `SetSession`, `GetSessions` and `GetProcesses`; `ServerSession.SetDebuggedByID`, `StartProcess`, `StopProcess`, `StopProcesses` and `Dispose`; the three lines of the `ServerProcess` constructor that self-attach, and `ServerProcess.Dispose`'s matching detach; `Engine.GetDebugger` and `GetDebuggers`; `DebugGetSessionsNode` and `DebugGetProcessesNode`; and the whole of `ServerChildObjects` - `Add`, `Adding`, `Removing`, `Disown`, `DisownAll`, `Clear` and `ObjectDispose`. `InProcessTestFixture` and `TestServerInstance` were read for what the base fixture actually provides, and `CoverageScripts.cs` for how the coverage harness connects. Source hygiene checked: 516 lines (mid-pack among the project's fixtures, the largest of which is 873), CRLF, a UTF-8 byte-order mark and tabs, matching its siblings; the comments say why - which guard raises, which order the teardown needs, what the one timing margin is - not what.

**Majors: none.** Nothing in the diff warranted a ticket. Two things were found and fixed in this pass, both in prose a future reader would have believed, and each turned out to leave a contract unverified, so each got the one case that pins it:

- **The teardown's stated reason was wrong.** Its comment said the debugger had to be stopped before the debugged session was disconnected, because "were it disconnected first the debugger would hold a disposed session until its own Dispose walked it". It would not: `ServerChildObjects.Adding` subscribes to every child's `Disposed` event and `ObjectDispose` disowns it, so the debugger's session and process lists empty themselves as the session goes. Checked by running it, not by reading: with a session attached and a task of it held, disconnecting the session leaves `Debug.GetSessions()` empty, the task unlisted, and `Debug.Stop()` succeeding. The comment now says that, and names the case that holds it. **A false constraint in a comment is worse than no comment** - the next person to touch this teardown would have worked around a problem that does not exist, and the stale-session failure it imagined (`DetachSession` locking a disposed session's process list, which is null, and `SetDebuggedByID` reading its `SessionInfo`, which is also null) would have stayed unguarded against for real.
- **`DebugLibrary.d4` was labelled with the wrong refusal.** `// Cannot attach to debugger session.` sits above a block whose `Debug.AttachSession(SessionID())` actually trips the *in-process* guard (106204) under the coverage harness, because `CoverageScripts` connects through `TestServerInstance.Connect` and so has no catalog cache name. The script asserts only that some error was raised, so it passes either way - but the label made 106197 look covered when it was at zero. The implement handoff spotted this and handed the decision to review. Both halves are now closed: the comment says which refusal that block reaches and which one it does not, and `ADebuggerRefusesToAttachTheSessionItRunsOn` reaches 106197 for real (a debugger started on the second session, attaching that session; it needs a session with both a catalog cache name and a debugger of its own, which only this fixture can arrange). The assertion is on the error code, because "some error was raised" is exactly the weak assertion that caused the confusion. No expected value in the D4 script was touched.

**Two tests added, none cut.** The two above. All six cases were weighed against the bar: each pins a distinct behaviour, none restates the implementation, none mocks anything, and the first four were mutation-checked by implement. Nothing to cut.

**Checked and confirmed correct, against the handoff's own claims:**

- **The race `NOTE:` in `AttachSession` is accurate.** `ServerSession.StartProcess` constructs the `ServerProcess` - which self-attaches from its constructor by reading `DebuggedByID` - and only then calls `_processes.Add`, so the window the `NOTE:` describes is real in both directions it names. Kept as written; it is a genuine tripwire (fine now, an attach is a person clicking in the IDE) and not a ticket.
- **The adjacent worry, a lock inversion, does not exist.** `AttachSession` and `DetachSession` take the debugger's `_syncHandle` and then the session's process list. Nothing takes them the other way round: the one path that could, the constructor's self-attach, runs *outside* the session's process lock, which is the same fact the race `NOTE:` rests on. So the window is a lost attach, never a deadlock.
- **The debugger's two owning collections cannot dispose what the session owns.** `Debugger._processes` and `_sessions` are `ServerChildObjects` with ownership on, holding children the engine's session also owns - and `ServerChildObjects.Removing` disposes the child unless it is being disowned. `Debugger.Dispose` detaches (disowns) every session and process before disposing either collection, so both are empty by the time `Clear()` could dispose anything. Correct as it stands; worth recording because the next reader of `Debugger.Dispose` will wonder.
- **The timing `NOTE:` on `RunningWork` is accurate and the margin holds here.** `AttachingASessionAttachesTheTaskItIsAlreadyRunning` measured 896-941 ms over five review runs against an attach window of tens of milliseconds (the other five cases do several executions and table reads each and run in 3-67 ms in all). Kept.
- **The filter correction in the handoff is right.** `--filter "Name~DebuggerSession"` matches nothing, because NUnit's `Name` is the case's own name and none of these contains that string. `FullyQualifiedName~DebuggerSessionTest` is what to use; it is what every run below used.
- **`DebugContext.cs` is really dead and really already owned.** The arm naming it is present in `tickets/backlog/debt-ws12-hygiene-remainder.md`, under `hygiene-remove-dead-compiled-files`. Nothing re-filed.

**Found and deliberately not filed, with the reason:**

- **A seventh error-walk helper in the test project.** `RefusalCode` walks an exception chain for a `DataphorException` code; six sibling fixtures each carry their own `FindError` doing something close (`AbandonedSessionCleanupTest`, `CatalogStoreConcurrencyTest`, `CatalogStoreTest`, `NativeSessionCleanupTest`, `PartialCommitTest`, `ServerLinkUserRollbackTest`). Consolidating them is a seven-file change to tests with no behaviour risk and no current-release anchor, which is below the filing bar; introducing the seventh variant rather than the seventh copy is the smaller of the two bad options, since it returns the code it found and so can report the *wrong* refusal in its failure message. Recorded here rather than as a `NOTE:` at one arbitrarily chosen site of seven.
- **The debugger gaps carried forward below.** Named by the review of `debt-debugger-stepping-script`, re-weighed here and left as a carried-forward list rather than a ticket: each needs a bespoke harness arrangement for a small return, and two previous passes made the same call. They are recorded, not forgotten.

**Tripwires parked this pass: none new.** Both of the implementation's `NOTE:`s - the `AttachSession` race and the `RunningWork` timing margin - were re-derived from the source and kept as written.

## What is still uncovered in the debugger area, after all three tickets

Carried forward so the next coverage pass does not re-file any of it:

- `src/engine/DAE/Debug/DebugContext.cs` - dead code that no test can cover; its deletion is an arm on `debt-ws12-hygiene-remainder`.
- `Debug.RunTo`, `Debug.StepIntoSpecific` and `Debug.GetStepIntoOperators` - **do not exist**, commented out in both `Debug.d4` and `DebugNodes.cs` and called by nothing, the IDE included. Not to be tested or implemented.
- The waiting loop inside `Debugger.WaitForPause` (`_waitSignal.WaitOne(500)` then `program.Yield`) - needs a caller that waits while nothing is yet stopped. Named by the review of `debt-debugger-stepping-script`.
- `Debug.GetStack` with a window index other than 0 - an outer frame, or a negative one. Same source.
- A breakpoint whose `LinePos` is a real position rather than `-1`. Same source.

`ServerException.Codes.CannotAttachToDebuggerSession` (106197) is no longer on this list: the review closed it.

## Known gaps in this work

- The six cases assert *which* processes are attached and *which* attaches are refused, never that a session attach leads to a stop. That is deliberate and argued above, but it does mean nothing tests that a breakpoint set by the debugger session fires in a task the attach brought in - the two mechanisms are only tested separately.
- The running-task case is the only one with a timing assumption, and the only one whose failure could be a slow machine rather than a defect. Its message says so.
- `Debug.GetProcesses()` reports its `IsPaused`, `Locator`, `Line`, `LinePos`, `DidBreak` and `Error` columns only for a process that is running *and* paused, so every row this fixture reads takes the null branch of `DebugGetProcessesNode`. The populated branch is `DebugBreakpoints.d4`'s and `DebugStepping.d4`'s.

## Tests

Built first, `dotnet build Dataphor.slnx`: 0 errors. `dotnet build tests/DAE.Server.Tests --no-dependencies -t:Rebuild`: 2 warnings, both the pre-existing `CS0618` obsolete-`TestDelegate` pair in `ApplicationTransactionTest.cs`, none from the new file.

- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~DebuggerSessionTest" --logger "console;verbosity=normal"` - 6 passed, 6.9 s for the run, of which about 1.0 s is case time (the running-task case 896-941 ms, the other five 3-67 ms each); the rest is `dotnet test` start-up and the fixture's own in-process server.
- One ring wider, in one run: `--filter "FullyQualifiedName~DebuggerSessionTest|Name=DebugBreakpoints|Name=DebugStepping|Name=DebugLibrary|Name=DebugTest|Name=SessionTables|Name=SessionAndProcessScopedTables|Name=TestProcessExecutes"` - 12 passed, 1 skipped, 44.6 s. The skip is `DebugTest`, the manifest's `known-failure` against `test-pre-3-1-scripts-policy` (`HarnessTimeout` at 5 s, an infinite loop by design), matched as expected. The three `Debug*` scripts are the neighbours of the one engine file this ticket touched and of the script whose comment changed; the three session-and-process scripts were added because the two new cases turn on session and process lifetime.

The three-part full run was not attempted: this diff is a test fixture, three comments and one documentation paragraph, so it cuts across nothing in the build or test infrastructure, and the project's rule puts the full suite with CI, which runs it in Debug and Release after the push. Part 1 grows by about a second of case time plus one more bare in-process server start.

No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written. The run printed one benign harness line, "Could not delete ... Template-1; the next test run deletes it", which the coverage harness emits by design when a previous run still holds a library assembly.
