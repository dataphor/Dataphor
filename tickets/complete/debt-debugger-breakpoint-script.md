description: A new test script stops a running program at a breakpoint and checks what the debugger then reports about it - where it is, its call stack, its variables - which nothing in the test suite had ever done before.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4, tests/DAE.Server.Tests/coverage-manifest.json, docs/architecture/sessions-processes-plans.md
----
## What landed

One new D4 coverage script, `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` (237 lines), its manifest entry, and a **Debugger** section in `docs/architecture/sessions-processes-plans.md`. No engine code changed, and no migration note: nothing an application, a deployment script or a C# extension can notice moved.

The script creates an operator of pure integer arithmetic, sets a breakpoint in it, has a second process call it, and asserts on what the debugger reports while that process is held. It is the first thing in this repository ever to hit a breakpoint, so `src/engine/DAE/Debug/Breakpoint.cs` and `CallStackEntry.cs` ran for the first time, along with the paused halves of `Debugger.cs` and `DebugNodes.cs`. **The paused paths work.** No engine defect was found in them.

The manifest entry carries `preamble: "SetUseImplicitTransactions(false);"` and `timeoutSeconds: 60`. The preamble is load-bearing, not tidiness: the script reads the operator's locator and line back out of the `Operators` catalog table, and a catalog store view answers from committed data only (`CatalogDeviceTable.InternalOpen` opens a connection outside the calling transaction's store transaction, which its own `NOTE:` records as deliberate), while the harness runs a whole document inside the one implicit transaction of its outer `ExecuteScript` batch. With implicit transactions on the read finds no row at all, whatever batch created the operator.

## What the script asserts, case by case

Each case clears its breakpoints and stops its process before the next one, and the two process-using cases take a process of their own.

| Case | Asserted |
|---|---|
| Breakpoint bookkeeping, no process | `ToggleBreakpoint` returns true, `GetBreakpoints` then holds exactly that one row (locator, line and `-1` position), toggling the same three values returns false, and `GetBreakpoints` is then empty |
| The breakpoint is hit | `GetProcesses` reports the process paused at the breakpoint's locator and line, with `DidBreak` true - `DidBreak` being what tells a breakpoint hit from a pause the debugger asked for |
| `WaitForPause` | returns, called after the poll has already seen the pause |
| The call stack | exactly two frames; frame 0 carries the operator's locator, the paused line, and a `Statement` containing `LTotal`; frame 1's `Location` starts with `program:` |
| The variables | the innermost window holds exactly three entries and their values are `1`, `3` and `7` - the argument the caller passed and the two locals as they stand at the paused statement |
| `GetSource`, both locator kinds | the operator locator's text contains the operator's body (`LTripled`), the program locator's text contains the call the process was given (`TestDebugTarget(1)`) and **not** the operator's body, so the two are different texts rather than the same one twice |
| `Debug.Run` resumes it | a second breakpoint two statements further on is hit, at its own line, with `LTotal` now `80` - the value only the two statements between the two breakpoints produce. That arithmetic is the evidence the process ran, rather than the debugger reporting the same pause again |
| Both breakpoints cleared | `GetBreakpoints` empty, then `Run()` |
| A cleared breakpoint stops nothing | on a fresh attached process, `ExecuteWithTimeout(process, 'TestDebugTarget(5);', 2000)` returns rather than raising - so the call was not still running when the timeout expired - and the process reports itself not paused |
| Teardown | `Debug.Stop()`, and `GetDebuggers` no longer has a row for the session |

## Things about the engine the script records, for whoever reads it next

- **The operator gets the *document* locator of the script that created it, not an operator locator, and its catalog `Line` is not 0.** `Compiler.ProcessSourceContext` takes the operator-locator branch only when the compiling plan's source context has no locator of its own; under the coverage harness it has one (`doc:Coverage.Scripts:DebugBreakpoints`, starting at line 1), so the catalog's `Line` is the absolute line of the `create operator` statement. The script computes its breakpoint lines as `max(Line, 1) + N - 1`, with `N` the 1-based line within the declaration, which is right in both routes.
- **`Debug.GetSource` has to be given the call stack's `Location` column, not its `Locator`.** `DebugGetSourceNode` accepts a `program:` locator and an `operator:` locator and raises `InvalidDebugLocator` for anything else, a `doc:` locator included. The `Locator` column of a call-stack row is the document locator; the `Location` column is the `operator:`/`program:` pair `GetSource` accepts.
- **`[]` on an empty table yields nil rather than raising,** which turned the first failure into `A nil was encountered where a value was expected`. The script guards the catalog read with `if not exists ...` and a named error.
- **The catalog operator name is qualified** with the session's current library, `General` for a session that named no default library, so the script matches with `like '%TestDebugTarget'`.
- **`Debug.GetStack`'s `Name` column is filled positionally** - `Location0`, `Location1`, ... - rather than with the D4 identifiers, which is what the IDE's stack window shows today, so the assertions are on values and the row count.
- **The waits poll the process with a budget and raise their own named error.** `TestDebugAwaitPause`, an operator the script creates, polls the process's `IsPaused` every 50 ms up to 5 s. `Debug.WaitForPause()` is deliberately not the wait: it has no timeout, so a pause that never came would end the case as a bare `HarnessTimeout` with nothing in it to read.

`Debug.RunTo`, `Debug.StepIntoSpecific` and `Debug.GetStepIntoOperators` do not exist - commented out in both `Debug.d4` and `DebugNodes.cs`, called by nothing. Confirmed again here; they must not be tested or implemented.

## Review findings

### What was checked

The implement-stage diff (`ticket(implement): debt-debugger-breakpoint-script`) was read before the handoff summary. Both of the derivations the handoff asked to be re-done were re-done from the engine source, not taken on trust:

- **The breakpoint line formula**, against `Compiler.ProcessSourceContext`, `PlanNode.SetLineInfo` and the two-argument `DebugLocator` constructor, in both locator routes. With a document locator the operator's locator line is `sourceLocator.Line + statement.Line - 1`, which is `statement.Line` under the harness; `SetLineInfo` stores a body node's line relative to the declaration (`line - (statement.Line - 1)`); the location constructor adds them back (`locator.Line + max(line - 1, 0)`). With an operator locator the line is 0 and the relative line passes through unchanged. `max(Line, 1) + N - 1` is right in both. Correct as written.
- **The stack window ordering.** `Stack.GetStack(0)` returns the window *reversed* (element 0 is the top of the stack) and `Debugger.GetStack` then reverses the index again, so `Index = 0` is the first value pushed. The two reversals cancel rather than compose, which is the off-by-one the handoff was right to flag. The expected values follow: paused *before* line 5, `AValue` is 1, `LTripled` is 3 and `LTotal` is 3 + 4 = 7; paused before line 7, `LTotal` is (7 + 1) * 10 = 80. Correct as written.

Also checked by reading the code, each of which could have made the script silently useless or the harness entry wrong:

- **The error path the manifest's 60 s timeout depends on** - an assertion failing while the debugged process is still held. The `finally` calls `StopProcess`, which reaches `Engine.TerminateProcessThread`; that method detaches a *paused* debugged process explicitly in order to unblock `Debugger.Yield`'s wait, so the cleanup does not hang and the script's own error is what the harness reports rather than a timeout. Works.
- **Rerun hygiene.** With implicit transactions off, a failure part-way leaves the two created operators committed, and neither `Debug.Stop()` nor the `drop operator` statements sit in a `finally`. It costs nothing: the harness builds every script a server copied from a template, so none of it survives into the next run.
- **Every factual claim in the added Debugger documentation**, against `Frontend/Source/Operators/Operators.cs` (which sets `doc:<library>:<document>` at line 1), `Debugger.ShouldBreak`, `D4Editor.cs` (which is what sets the `-1` line position) and `Debugger.GetStack`. Three of them were wrong; see below.
- **`docs/coverage-report.md` was correctly left alone**: everything below its *Line coverage per assembly* heading is generated, and the prose above it records one dated run.

### Minor findings, fixed in this pass

**Two wrong statements in the Debugger documentation this ticket added, plus a third that was misleading.**

- "Only statement nodes are breakable (`ImperativeNodes.cs`); expression nodes never yield" is false in both halves. `IsBreakable` is also set by `DMLNode` (`TableNodes.cs` - insert, update, delete) and by `ListForEachNode` and `TableForEachNode` (`ListNodes.cs`), and `ListForEachNode` is what `Compiler.CompileForEachExpression` compiles a `foreach` **expression** into, so a pause can land inside an expression. Rewritten to name the three places and to say what is deliberately *not* breakable (an operator invocation, an arithmetic instruction).
- "`Debug.GetCallStack`, `Debug.GetStack` and `Debug.GetSource`, each of which refuses unless the debugger is paused" is false for `GetSource`. `DebugGetSourceNode` touches the debugger only for a `program:` locator; an `operator:` locator is answered through `ResolveCatalogObjectSpecifier` and `D4TextEmitter`, needing neither a pause nor a debugger at all. Rewritten, and the same wrong claim in the script's own comment at the `GetSource` case was fixed with it.
- "reports itself paused once every attached process that is running is parked - which a debugger with nothing attached satisfies with both counts at zero" omitted the other half of `Debugger.IsPaused`, which is `_isPauseRequested`. As written it reads as though an idle debugger reports itself paused. **The handoff asserts exactly that wrong version** ("`Debug.GetDebuggers().IsPaused` is true for a debugger with nothing attached"), and it is false - a freshly started debugger has `_isPauseRequested == false` and reports itself not paused. Nothing in the script depended on the error: the script's own `NOTE:` has it right, and polling the process row is the right choice regardless, because the debugger-level flag does not say *which* process is paused. The sentence now names both conjuncts.

**One assertion that cannot fail, removed.** After clearing both breakpoints the script called `Debug.Run()` and then asserted the process no longer reports itself paused. `Debugger.Run` clears `_isPauseRequested`, and `GetProcesses` computes the `IsPaused` column as the process running *and* `Debugger.IsPaused`, so that row reads not-paused the instant `Run` returns, whatever the process does. The handoff's behaviour table lists it as asserted. Removed, and replaced by a `NOTE:` at the site saying why the final resumption cannot be asserted from D4 at all: every other reader of a paused program (`GetCallStack`, `GetStack`, `GetSource` of a program locator) refuses once the debugger is running, so nothing the engine publishes distinguishes "resumed and finished" from "resumed and still running". The pause at the second breakpoint, with `LTotal` at 80, is and remains the script's evidence that `Run` resumes a held process.

**The cleared-breakpoint case's evidence was overstated, and is now stated honestly.** `ExecuteWithTimeout` reports whether the script was *still running* when the timeout expired, not whether it succeeded: `AsyncExecution.Start` swallows the script's error and its task completes, so `IsCompleted` is true for a call that failed immediately. The case would therefore pass just as well if `TestDebugTarget(5);` had failed outright instead of pausing. Parked as a `NOTE:` at the site rather than restructured - the case above establishes that the call runs, and the only way to close the gap would be a side effect in an operator deliberately kept to pure arithmetic so that a process paused inside it holds no lock the script could then wait for.

**The open question the handoff left, answered.** The call-stack frame count was asserted as `>= 2` to avoid a spurious failure. `Debugger.GetCallStack` skips `ExecutingPrograms[0]`, so only the asynchronous script's program contributes, with its operator window and its base window: exactly two. Tightened to `= 2` and confirmed over four consecutive runs. The exact count pins the frame set rather than only its floor, which is what a characterization test is for.

### Tripwires parked

The two `NOTE:` lines described above, both at their exact code sites in the script: the final resumption cannot be asserted, and `ExecuteWithTimeout` reports "still running" rather than "succeeded".

### Major findings: none, and why

No ticket was filed. The change touches no engine code and crosses no compatibility surface - no D4 semantics or operator signature, no class registered through `DAERegister`, no file format, no error code, no contract shape - so there was nothing of that kind to find. The one real defect met while writing the script was already filed with a verified reproduction (`bug-d4-emitter-drops-qualifier-grouping`, below). Every remaining gap the handoff lists - break-on-exception and the never-populated `Error` column of `GetProcesses`, `WaitForPause`'s waiting loop, `GetStack` with a window index other than 0, stopping a process while it is still paused, debugging a second session - already belongs to `debt-debugger-stepping-script` or `debt-debugger-coverage`, so filing them again would only duplicate the board.

### Tests cut: none

Every assertion in the script except the one removed above tests a value the engine computes rather than restating the implementation, and none of them mocks anything. One assertion was strengthened (the frame count) and one removed (the vacuous one). No test was added: no defect was found in the code under review, and the contract the review found unverified - that the resumed process ran to completion - cannot be observed from D4 at all, which is why it is a `NOTE:` and not a test.

### One thing left alone

`tickets/.garden-report.md`, which the implement commit also edited, now has a plan row whose description still mentions "debugger stepping" after the debugger tickets moved out of that row. It is the gardener's own scratch file and is rewritten each tending pass, so it was not touched.

## A defect met along the way: filed, not fixed

Adding a `.d4` file puts it in `ParserRoundTripCorpusTest`, and the first version of this one failed it. The cause is in the D4 emitter, not in the script: **`D4TextEmitter` drops the brackets that make a qualifier chain left-associative**, so a source that writes `(a.b).c` is printed as `a.b.c` and reads back as `a.(b.c)`. Minimal reproduction, verified: a one-line file containing `var P := (LRow.Line).ToString();` fails the corpus case; the same line without the brackets passes.

It is the third defect of the class that `bug-d4-emitter-malformed-output` and `bug-d4-emitter-reshapes-parse-tree` retired, and the generalized guard for the class already exists and is what caught it. It is not fixed here, because the fix changes the text the server stores for every object whose definition contains the construct and has to decide which qualifier operands need brackets without turning a dotted *name* into `(System).Integer` - a design call on an emission surface, not a local fix. Filed as `bug-d4-emitter-drops-qualifier-grouping` (promoted out of `backlog/` while this review was running), with the reproduction table, the site (`EmitQualifierOperand`), and what a fix has to decide. Both groupings compiled and evaluated identically through the engine, so no wrong result has been shown; the ticket's severity is `edge-case` and its body says plainly what has not been established. Three assertions in this script read the line into a local first because of it, and a comment at that site names the slug.

## What is still unexercised

- **`DebugGetProcessesNode`'s `row[6] = process.Error`** has still never run with a value in it: for a process paused at a breakpoint the stack's `ErrorVar` is null, so the assignment ran with null and succeeded. The case that would put a CLR exception into a D4 `Error` column is break-on-exception, which belongs to `debt-debugger-stepping-script`.
- **`Debugger.GetStack`'s temporary clearing of the target's debugger** reads correctly and behaves: the script calls `GetStack` five times across two pauses and the process pauses again afterwards, which is the symptom that would show if the debugger were ever left cleared. The later cases passing is the evidence; there is no assertion of its own.
- **`Debug.WaitForPause()` is covered only on the path where it returns at once.** Its loop - `_waitSignal.WaitOne(500)` then `program.Yield(node, false)` - is unreached, and reaching it needs a thread that waits while nothing is yet paused.
- **`LinePos` is asserted only where a breakpoint carries it** (`= -1`). The position the engine reconstructs for a location is one less than the node's 1-based position, because the `DebugLocator(DebugLocator, int, int)` constructor subtracts one; the implement ticket says not to depend on it and not to "fix" it, and nothing here does either.
- **`Debug.GetStack` with a window index other than 0** - an outer frame, or a negative - is untested, as is stopping a process while it is still paused (the script always lets it finish first). Both belong to `debt-debugger-stepping-script`.
- **Debugging a second session**, which no D4 operator can reach from a script, belongs to `debt-debugger-coverage`.

## Coverage this should move

The Debugger area was 35.7 % of 894 lines on the three-part run of 2026-11-21 at `f6200026`. `Breakpoint.cs` and `CallStackEntry.cs` were at exactly 0 % and now run in full; the paused members of `Debugger.cs` (`Yield`'s breaking branch, `ShouldBreak`, `CheckPaused`, `GetCallStack`, `GetStack`, `FindProgram`, `GetProgram`, `ToggleBreakpoint`, `Run`, `WaitForPause`) and the nodes over them (`DebugGetBreakpointsNode`, `DebugToggleBreakpointNode`, `DebugGetCallStackNode`, `DebugGetStackNode`, `DebugGetSourceNode`, `DebugWaitForPauseNode`, `DebugRunNode`) now run too. `src/engine/DAE/Debug/DebugContext.cs` cannot move - nothing in the repository constructs it, and its 11 coverable lines stay uncovered until `debt-ws12-hygiene-remainder` deletes it.

## Tests

| Test | What it verifies |
|---|---|
| `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4` (coverage script, manifest `pass`) | the behaviour table above: a breakpoint can be set, listed and cleared; it stops a running program at the line it names; the paused program's location, call stack, innermost-frame variables and both source texts read back; `Run` resumes it to a second breakpoint with the arithmetic of the statements in between; a cleared breakpoint leaves the call running to its end |

No NUnit test. The subsystem is reached from D4, the lowest layer that shows the behaviour is a D4 script through the in-process harness, and the one thing a script cannot reach - debugging a second session - is owned by `debt-debugger-coverage`. No test for the emitter defect: its reproduction belongs to the ticket that fixes it, and the corpus pass already catches the class.

## Validation

`dotnet build Dataphor.slnx` - 0 errors, run after each edit.

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=DebugBreakpoints|Name=DebugLibrary|Name=DebugTest|Name=AsyncExecution|Name=Operators|Name=OperatorSourceText|Name=OperatorText|Name=ParserEmitter|Name=SerializationOperators"
dotnet test tests/DAE.Server.Tests --no-build -- "NUnit.Where=class == Alphora.Dataphor.DAE.Server.Tests.ParserRoundTripCorpusTest or class == Alphora.Dataphor.DAE.Server.Tests.D4EmitterRoundTripTest or class == Alphora.Dataphor.DAE.Server.Tests.InternalCoverageTests"
python tools/docs-size-check/check_size.py
```

The first is the implement stage's targeted set, one ring wider: the whole Debugger script area plus the scripts that print and compare emitted D4, which is what a new `.d4` file and a manifest change could disturb. 8 passed, 1 known failure matched (`DebugTest`, against `test-pre-3-1-scripts-policy`), 0 failed, 30 s. The second adds the fixtures a new `.d4` file enters as a corpus case - which is how the emitter defect surfaced - and the manifest's own validation tests: 395 passed, 19 skipped, 0 failed, 2 s. The third: 11 files checked, 0 over the limit; the page this ticket added to is well inside the 1,500-word cap. `DebugBreakpoints` itself ran 4-5 s and passed on four consecutive runs after the frame-count assertion was tightened.

The three-part full run was not needed: the change touches no build or test infrastructure, no manifest format, and no lexer, parser or compiler code. CI runs the whole suite in Debug and Release on the push.

No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written.
