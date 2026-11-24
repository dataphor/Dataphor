description: The new coverage script that steps through D4 code under the debugger failed once in CI with a null-reference error, in the Debug build only, on a commit range that otherwise passed. Find out whether the script or the debugger has a timing race, and fix whichever it is so the script passes every time.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugStepping.d4, tests/DAE.Server.Tests/coverage-manifest.json, src/engine/DAE/Debug/
repro: unverified (one CI failure)
----
## What failed

CI run 45315327836 (`Build and test`, push of `703bfb3c ticket(review): debt-library-attach-from-a-directory`), job **Debug** only; the Release job of the same run passed. One test failed out of 1,028 in `DAE.Server.Tests`:

```
Failed DebugStepping [2 s]
RuntimeException: A nil was encountered where a value was expected. (doc:Coverage.Scripts:DebugStepping@288:3) <= NullReferenceException: Object reference not set to an instance of an object.
  at Alphora.Dataphor.DAE.Server.Tests.CoverageScripts.Evaluate(CoverageEntry entry, String error) in tests/DAE.Server.Tests/CoverageScripts.cs:line 484
```

Line 288 of `DebugStepping.d4` is the `StopProcess(LProcessID);` in the `finally` of the step-over case (the case that toggles a breakpoint on the call in `TestDebugStepOuter` and steps over it up to four times). So either an earlier statement of that block raised and the `finally` then met a nil, or `StopProcess` itself met one, for example a process that had already ended or was not yet paused.

The script came in with `debt-debugger-stepping-script` (`6da37f19`, `1b6d4feb`) and passed in CI at `1b6d4feb` and at `0a6b320c`. Between `0a6b320c` and `703bfb3c` only `debt-library-administration-coverage` and `debt-library-attach-from-a-directory` landed, which do not touch the debugger. So this looks timing-dependent, not a regression, but that is not verified.

## What to do

1. Reproduce. Run `dotnet test tests/DAE.Server.Tests --no-build --filter Name=DebugStepping` in a loop, in Debug, enough times to see the failure rate (20 or more; with other load on the machine if it does not show up idle). If it shows up, capture the full D4 error chain: the script's own error, not only the `finally`'s nil.
2. Find the race. Read the debugger operators the script uses (`Debug.GetProcesses`, the step and run operators, `StopProcess`, and the script's `TestDebugStepAwait` / `TestDebugStepOnce` helpers) and decide whether the script assumes a state the debugger does not guarantee yet (for example, reading `Line` before the process has paused, or stopping a process that has already run to completion), or whether the debugger has a real race (a paused flag set before the line is recorded, or similar).
3. Fix it at the side that is wrong. A script that waits for a state the debugger does guarantee is a script fix. A debugger that reports a state before it is true is an engine fix, with this script as its reproduction. Do not loosen the script's assertions about where stepping lands; those are the behaviour being covered.
4. If it cannot be reproduced locally after a fair try, record the attempt here, and decide on evidence whether to mark the manifest entry `unstable` with this ticket's slug (reported, never fails the run) until it is understood. Say which in the handoff.

