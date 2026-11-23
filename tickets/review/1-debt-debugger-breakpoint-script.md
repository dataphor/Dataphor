description: A new test script stops a running program at a breakpoint and checks what the debugger then reports about it - where it is, its call stack, its variables - which nothing in the test suite had ever done before.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4, tests/DAE.Server.Tests/coverage-manifest.json, docs/architecture/sessions-processes-plans.md, tickets/backlog/bug-d4-emitter-drops-qualifier-grouping.md
difficulty: hard
----
## What landed

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` (220 lines), its manifest entry, and a **Debugger** section in `docs/architecture/sessions-processes-plans.md`. No engine code changed. No migration note: nothing an application, a deployment script or a C# extension can notice moved.

The script creates an operator of pure integer arithmetic, sets a breakpoint in it, has a second process call it, and asserts on what the debugger reports while that process is held. It is the first thing in this repository ever to hit a breakpoint, so `src/engine/DAE/Debug/Breakpoint.cs` and `CallStackEntry.cs` ran for the first time, along with the paused halves of `Debugger.cs` and `DebugNodes.cs`.

**The paused paths work.** No engine defect was found. Both of the two the implement ticket warned about came out clean, one of them only partly tested - see *What is still unexercised*.

## Three of the implement ticket's premises were wrong, and the script departs from them

Each was wrong in the same way: the ticket reasoned about a script executed with no document locator, and the coverage harness does not execute scripts that way. It runs each one as `ExecuteScript('Coverage.Scripts', '<name>')`, the Frontend library's operator, which sets the document locator `doc:Coverage.Scripts:<name>` starting at line 1.

**The operator gets a document locator, not an operator locator, and its `Line` is not 0.** `Compiler.ProcessSourceContext` takes the operator locator branch only when the compiling plan's source context has no locator of its own; here it has one, so the operator's locator is `doc:Coverage.Scripts:DebugBreakpoints` and its `Line` is the absolute line of the `create operator` statement - 40, measured. The script therefore does **not** assert that `Operators.Line` is 0, as the ticket asked. It computes the breakpoint line as `max(Line, 1) + N - 1`, where `N` is the 1-based line within the declaration, which is right in both routes: with a declaration line of 0 the `DebugLocator(DebugLocator, int, int)` constructor passes the relative line through unchanged, and with a real declaration line it adds them. The assertion that carries the convention is the real one - that the paused process reports exactly that line.

**The whole document runs in one implicit transaction, not one per top-level batch.** `ServerProcess.BeginTransactionalCall` begins an implicit transaction only when no transaction is open, so the harness's single outer `ExecuteScript(...)` batch owns it and every nested batch runs inside it. Placing the `create operator` at the top level therefore commits nothing. That matters because a **catalog store view answers from committed catalog data only** - `CatalogDeviceTable.InternalOpen` opens a connection of its own, outside the calling transaction's store transaction, and its `NOTE:` records that as deliberate - so with implicit transactions on, `Operators where OperatorName like '%TestDebugTarget'` returned no row at all. The manifest entry carries `preamble: "SetUseImplicitTransactions(false);"`, as `AsyncExecution` and `SecurityLibrary` do, and the entry's note and the script's header both say why. Measured both ways: 0 matching rows with implicit transactions on, 1 with them off.

That first failure presented as `A nil was encountered where a value was expected`, because **`[]` on an empty table yields nil rather than raising**. The script now guards with `if not exists (Operators where ...)` and a named error, so the next person who breaks this gets the cause instead of a nil.

**The catalog operator name is qualified.** `CompileCreateOperatorStatement` qualifies with the session's current library, which is `General` for a session that named no default library (`Engine.InternalConnect` falls back to `GeneralLibraryName`), so `OperatorName` is `General.TestDebugTarget`. The script matches with `like '%TestDebugTarget'` rather than comparing.

The catalog route the ticket preferred was used throughout. The `SetBreakOnStart` + `StepInto` fallback was not needed and is untouched.

## One correction worth carrying forward

**`Debug.GetSource` has to be given the call stack's `Location` column, not its `Locator`.** `DebugGetSourceNode` handles a `program:` locator and an `operator:` locator and raises `InvalidDebugLocator` for anything else - a `doc:` locator included. The `Locator` column of a call-stack row is the document locator here; the `Location` column is `operator:<name><signature>` for a frame with an originating operator and `program:<guid>` for the program's own base frame, which is exactly the two kinds `GetSource` accepts. The implement ticket said "GetSource of the operator locator", which reads as the `Locator` column and would have raised.

## What the script asserts, case by case

Each case clears its breakpoints and stops its process before the next one, and the two process-using cases take a process of their own.

| Case | Asserted |
|---|---|
| Breakpoint bookkeeping, no process | `ToggleBreakpoint` returns true, `GetBreakpoints` then holds exactly that one row (locator, line and `-1` position), toggling the same three values returns false, and `GetBreakpoints` is then empty. This is all of `Breakpoint.cs` and the `GetBreakpoints` node |
| The breakpoint is hit | `GetProcesses` reports the process paused at the breakpoint's locator and line, with `DidBreak` true - `DidBreak` being what tells a breakpoint hit from a pause the debugger asked for |
| `WaitForPause` | returns, called after the poll has already seen the pause |
| The call stack | at least two frames; frame 0 carries the operator's locator, the paused line, and a `Statement` containing `LTotal`; frame 1's `Location` starts with `program:` |
| The variables | the innermost window holds exactly three entries and their values are `1`, `3` and `7` - the argument the caller passed and the two locals as they stand at the paused statement |
| `GetSource`, both locator kinds | the operator locator's text contains the operator's body (`LTripled`), the program locator's text contains the call the process was given (`TestDebugTarget(1)`) and **not** the operator's body, so the two are different texts rather than the same one twice |
| `Debug.Run` resumes it | a second breakpoint two statements further on is hit, at its own line, with `LTotal` now `80` - the value only the two statements between the two breakpoints produce. That arithmetic is the evidence the process ran, rather than the debugger reporting the same pause again |
| Both breakpoints cleared | `GetBreakpoints` empty, `Run()`, and the process no longer reports itself paused |
| A cleared breakpoint stops nothing | on a fresh attached process, `ExecuteWithTimeout(process, 'TestDebugTarget(5);', 2000)` returns without raising - so the call *finished* rather than merely failing to pause - and the process reports itself not paused |
| Teardown | `Debug.Stop()`, and `GetDebuggers` no longer has a row for the session (the assertion `DebugLibrary` already makes) |

Two engine properties are recorded as `NOTE:` lines in the script because a reader would otherwise take each for a bug in the test: `Debug.GetDebuggers().IsPaused` is true for a debugger with nothing attached (both of `Debugger.IsPaused`'s counts are zero), which is why every wait polls the *process* row instead; and `Debug.GetStack`'s `Name` column is filled positionally - `Location0`, `Location1`, … - rather than with the D4 identifiers, which is what the IDE's stack window shows today, so the assertions are on values and the row count.

## Two choices a reviewer should weigh

**The waits poll with a budget and raise their own named error.** `TestDebugAwaitPause`, an operator the script creates, polls the process's `IsPaused` every 50 ms up to 5 s and raises `The debugged process never paused waiting for <what>`. `Debug.WaitForPause()` is deliberately not the wait: it has no timeout, so a pause that never came would end the case as a bare `HarnessTimeout` with nothing in it to read.

**The cleared-breakpoint case uses `ExecuteWithTimeout` on a fresh process, not `ExecuteAsync` on the one above.** Three reasons, and the third is the load-bearing one. `ExecuteWithTimeout` is bounded and tells the caller whether the script finished, where `ExecuteAsync` swallows everything and leaves "no pause observed" as weak evidence. The case then inherits no state. And a second call on the process from the case above would enter `ServerProcess.BeginCall`, which holds `_syncHandle` for the length of a call: issued while that process were still paused or still finishing, it would block the script's own thread with no timeout of its own. The cost is the 1 s–2 s `ExecuteWithTimeout` always sleeps, which is most of the gap between the case's 4 s and the engine work in it.

## A defect met along the way: filed, not fixed

Adding a `.d4` file puts it in `ParserRoundTripCorpusTest`, and the first version of this one failed it. The cause is in the D4 emitter, not in the script: **`D4TextEmitter` drops the brackets that make a qualifier chain left-associative**, so a source that writes `(a.b).c` is printed as `a.b.c` and reads back as `a.(b.c)`. Minimal reproduction, verified: a one-line file containing `var P := (LRow.Line).ToString();` fails the corpus case; the same line without the brackets passes.

It is the third defect of the class that `bug-d4-emitter-malformed-output` and `bug-d4-emitter-reshapes-parse-tree` retired, and the generalized guard for the class already exists and is what caught it. It is **not** fixed here, because the fix changes the text the server stores for every object whose definition contains the construct and has to decide which qualifier operands need brackets without turning a dotted *name* into `(System).Integer` - a design call on an emission surface, not a local fix. Filed as `tickets/backlog/bug-d4-emitter-drops-qualifier-grouping.md`, with the reproduction table, the site (`EmitQualifierOperand`), and what a fix has to decide.

Measured, and in the ticket: both groupings compiled and **evaluated identically** through the engine, so no wrong result has been shown - the severity is `edge-case` and the body says plainly what has not been established. Three assertions in this script were rewritten to read the line into a local first; a comment at that site names the slug.

## What is still unexercised, and what it means for the next ticket

- **`DebugGetProcessesNode`'s `row[6] = process.Error`** still has never run with a value in it. For a process paused at a breakpoint the stack's `ErrorVar` is null, so the assignment ran with null and succeeded. The case that would put a CLR `Exception` into a D4 `Error` column is break-on-exception, which belongs to `debt-debugger-stepping-script`. The warning in the implement ticket stands, undischarged.
- **`Debugger.GetStack`'s temporary clearing of the target's debugger** reads correctly and behaves: the script calls `GetStack` five times across two pauses and the process pauses again afterwards, which is the symptom that would show if the debugger were ever left cleared. Not an assertion of its own - the later cases passing is the evidence.
- **`Debug.WaitForPause()` is covered only on the path where it returns at once.** Its loop - `_waitSignal.WaitOne(500)` then `program.Yield(node, false)` - is unreached, and reaching it needs a thread that waits while nothing is yet paused.
- **The call stack is asserted as `>= 2` frames, not `= 2`.** Two is what the arrangement produces (`Debugger.GetCallStack` skips `ExecutingPrograms[0]`, the process program, so only the async script's program contributes, with its operator window and its base window). The looser bound was chosen so that an extra program in the chain would not fail the case spuriously; a reviewer may reasonably prefer the exact count, which would pin the frame set rather than only its floor.
- **`LinePos` is asserted only where a breakpoint carries it** (`= -1`). The position the engine reconstructs for a location is one less than the node's 1-based position, because the `DebugLocator(DebugLocator, int, int)` constructor subtracts one; the implement ticket says not to depend on it and not to "fix" it, and nothing here does either.
- **`Debug.GetStack` with a window index other than 0** - an outer frame, or a negative - is untested, as is stopping a process while it is still paused (the script always lets it finish first). Both belong to `debt-debugger-stepping-script`.
- **`Debug.RunTo`, `Debug.StepIntoSpecific` and `Debug.GetStepIntoOperators` do not exist** - commented out in both `Debug.d4` and `DebugNodes.cs`, called by nothing. Confirmed again here. They must not be tested or implemented.

## Coverage this should move

The Debugger area was 35.7 % of 894 lines on the three-part run of 2026-11-21 at `f6200026`. `Breakpoint.cs` and `CallStackEntry.cs` were at exactly 0 % and now run in full; the paused members of `Debugger.cs` (`Yield`'s breaking branch, `ShouldBreak`, `CheckPaused`, `GetCallStack`, `GetStack`, `FindProgram`, `GetProgram`, `ToggleBreakpoint`, `Run`, `WaitForPause`) and the nodes over them (`DebugGetBreakpointsNode`, `DebugToggleBreakpointNode`, `DebugGetCallStackNode`, `DebugGetStackNode`, `DebugGetSourceNode`, `DebugWaitForPauseNode`, `DebugRunNode`) now run too. `src/engine/DAE/Debug/DebugContext.cs` **cannot** move - nothing in the repository constructs it, and its 11 coverable lines stay uncovered until `debt-ws12-hygiene-remainder` deletes it. `docs/coverage-report.md` was not touched: everything below its *Line coverage per assembly* heading is generated, and its prose records the run it came from.

## Tests

| Test | What it verifies |
|---|---|
| `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` (coverage script, manifest `pass`) | the behaviour table above: a breakpoint can be set, listed and cleared; it stops a running program at the line it names; the paused program's location, call stack, innermost-frame variables and both source texts read back; `Run` resumes it to a second breakpoint with the arithmetic of the statements in between; a cleared breakpoint stops nothing |

No NUnit test added. The subsystem is reached from D4, the lowest layer that shows the behaviour is a D4 script through the in-process harness, and the one thing a script cannot reach - debugging a second session - is already owned by `debt-debugger-coverage`. No test was added for the emitter defect: its reproduction belongs to the ticket that fixes it, and the corpus pass already catches the class.

## Validation run

`dotnet build Dataphor.slnx` - 0 errors. Then, targeted:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=DebugBreakpoints|Name=DebugLibrary|Name=DebugTest|Name=AsyncExecution|Name=Operators|Name=OperatorSourceText|Name=OperatorText|Name=ParserEmitter|Name=SerializationOperators" --logger "console;verbosity=normal"
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class == Alphora.Dataphor.DAE.Server.Tests.ParserRoundTripCorpusTest or class == Alphora.Dataphor.DAE.Server.Tests.D4EmitterRoundTripTest"
python tools/docs-size-check/check_size.py
```

The first: 8 passed, 1 known failure matched (`DebugTest`, against `test-pre-3-1-scripts-policy`), 0 failed. `DebugBreakpoints` 4 s, stable across four consecutive runs; the neighbouring scripts are there because the manifest and the emitted-text comparisons are what this change could disturb. The second: 394 passed, 19 skipped, 0 failed - run because a new `.d4` file becomes a corpus case, which is how the emitter defect surfaced. The third: 11 files checked, 0 over the limit; the page this ticket added to is 637 words of the 1,500 limit.

No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written.

`DebugBreakpoints` lands in part 2 of the three-part run (`[A-Ra-r]`), which took about 3.5 minutes at best; 4 s more. Part 1, 2 and 3 were not run: the change touches no build or test infrastructure, no lexer, parser or compiler code, and no manifest format.

## For the reviewer

The implement ticket's *Do not reuse what `DebugTest.d4` does* still holds: that file and its manifest entry are owned by `test-pre-3-1-scripts-policy` and were not touched.

Worth re-deriving rather than taking on trust:

- the breakpoint line formula `max(Operators.Line, 1) + N - 1`, against `Compiler.ProcessSourceContext`, `PlanNode.SetLineInfo` and the two-argument `DebugLocator` constructor, in both locator routes;
- that the stack window really holds three entries in the order asserted, against `Stack.GetStack`, `StackWindowList.this[int]` and `Debugger.GetStack`'s index reversal - the values `1`, `3`, `7` are what make the assertion mean something, and an off-by-one in that reasoning would still have produced a passing test with different numbers;
- whether the `>= 2` frame bound should be exact, and whether the cleared-breakpoint case should also run on the process from the case above once a bounded way to wait for that process to be free exists.
