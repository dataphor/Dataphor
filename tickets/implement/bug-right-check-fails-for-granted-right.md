description: A test script was being refused permission to act as a user it had just created, and it turns out the refusal was correct — the script had not yet saved that user when it tried to use it. The test runner is changed to save each step of the script before the next one runs, after which the whole script passes.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/DAE.Server.Tests/coverage-manifest.json, docs/modernization-plan.md
repro: verified
difficulty: easy
----
## What is actually wrong

Nothing in the engine. The right check was giving the *correct* answer, and the fix ticket's premise — "the check is giving the wrong answer, not reporting a missing grant" — is wrong. The grant really was not there to be seen.

The `ServerLink` coverage script creates a user at its line 40 and then, at line 43, uses `ExecuteAs` to run a small block as that user. `ExecuteAs` does not switch the current session's user: it opens a **new session** on the same server (`SystemExecuteAsNode.ExecuteScript` in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` calls `Connect(sessionInfo)`), and the block is compiled and run there. A new session reads the catalog store through a connection of its own, so it sees committed state plus nothing of any other session's uncommitted work.

Meanwhile, the coverage harness was running the whole script nested inside one statement: `process.ExecuteScript("ExecuteScript('Coverage.Scripts', 'ServerLink');")` (`tests/DAE.Server.Tests/CoverageScripts.cs`, the `else` arm near the end of `RunInProcess`). Every D4 statement runs inside an implicit transaction, begun at the outermost transactional call and committed when that call ends (`ServerProcess.BeginTransactionalCall`/`EndTransactionalCall`), so that single outer statement held **one transaction open for the whole script**. The new user, and its membership of the `System.User` role that carries the grant, were therefore still uncommitted when the `ExecuteAs` session asked whether that user may use the implicit text-to-name conversion operator. The store answered no, correctly: the right row exists and is granted to the role, and the role has no members this session can see.

That is why the measurements in the fix ticket all said the grant was present — they were taken from a session that could see it.

## What was measured

Measured 2026-11-21, Debug, with temporary instrumentation in `ServerCatalogDeviceSession.UserHasRight` and in `CatalogDevice.BeginPendingUser`/`EndPendingUser` (since removed; the working tree is unchanged):

- At the denial, read through the checking session's own store connection: the right row `System.Name0000001789Execute` exists and is owned by `System`, the `System.User` role (object id 1098) has that right granted, and **the role has no members at all** (`roleUsers=[]`) — so the newly created user's membership is invisible to this session.
- The denial happens in session 2 (the `ExecuteAs` session) with no transaction of its own open; the per-user pending-change marker for that user is set, which is the engine recording that *another* session has an open transaction that changed that user's rights.
- The stack trace at the moment the marker was set is `SystemCreateUserNode.InternalExecute` → `InsertUserRole` → `UserRightsChanged`, inside the script, and the marker was cleared only *after* the denial — i.e. the transaction that created the user was still open when the refusal happened. The outermost frame is the harness's single `ServerProcess.ExecuteScript` call.
- With `"batches": true` added to the script's manifest entry (so the harness runs the script's own text batch by batch, one implicit transaction per batch), the **whole script runs to the end with no error**: `dotnet test tests/DAE.Server.Tests --filter Name=ServerLink` reported `pass: 1 passed, 0 failed` in 9 s. Every block after line 43, which had never executed, now does.

## The change

`tests/DAE.Server.Tests/coverage-manifest.json`, the `ServerLink` entry: status `pass`, add `"batches": true`, keep `"linkedServer": true`, drop `"ticket"` and `"error"`, and replace the note. The `batches` option already exists and is already documented for exactly this case — see its description in `CoverageManifest.cs` ("Use it only for a script that depends on batch boundaries … or that must see a batch's work committed") — and `ApplicationTransactions` already uses it. The entry that was verified is:

```
{ "script": "ServerLink", "status": "pass", "batches": true, "linkedServer": true, "note": "batch by batch because the script creates a user and then runs a block as that user through ExecuteAs, which opens a session of its own: nested in one statement, the new session cannot see the uncommitted user" },
```

Note that the manifest is an embedded resource, so a rebuild is needed between editing it and running the test.

`preamble: "SetUseImplicitTransactions(false);"` (what `AsyncExecution` and `SecurityLibrary` use) would also work and was not chosen: `batches` is the narrower statement of what this script needs, it is how Dataphoria runs a script — which is how these scripts were written to be run — and it leaves the script's own transaction behaviour alone.

## Documentation to correct

Two passages in `docs/modernization-plan.md` describe this as an engine defect and now say the wrong thing:

- the **What the script found** paragraph in WS3 (around line 359): its last sentence says the script "stops later and in a new place … line 43, where an ordinary user is refused a right the catalog grants it. That is `bug-right-check-fails-for-granted-right`". Replace with the fact: run whole, the script now passes end to end, and it is run batch by batch because `ExecuteAs` opens a session of its own that cannot see the script's uncommitted user. The paragraph's opening count ("125 of the `ServerLink` script's 126 statements pass") should become all of them.
- the **K35** row (around line 793): its closing clause says the coverage script "stops later, at the rights check the ticket `bug-right-check-fails-for-granted-right` owns". Replace with the script now passing whole, run batch by batch.

No new K row: there is no defect to record. No `docs/migration-notes.md` entry: nothing an application, a deployment script or a C# extension could notice changes — the only edits are to a test manifest and the plan.

## Tests

No new test. The project's rule asks a bug fix for its reproduction, and here the reproduction *is* the coverage script, which this ticket turns into a permanent passing assertion — the script is the lowest layer that shows the behaviour. The underlying engine behaviour (another session cannot use a grant whose transaction has not committed) is already pinned by `UncommittedGrantNotUsableInOtherSession` in `tests/DAE.Server.Tests/CatalogStoreTest.cs`, added by `bug-right-cache-shared-during-open-transaction`; a second test of it would be run-time tax with no new behaviour behind it.

Worth a moment's thought and then, in this ticket's judgement, no: a guard that fails any coverage entry whose script mentions `ExecuteAs`, `ExecuteOn` or `EvaluateOn` without `batches` or the implicit-transactions preamble. Three entries handle this by hand today (`ApplicationTransactions`, `AsyncExecution`, `SecurityLibrary`), the failure mode is loud — the script errors and the case fails — and a textual guard over D4 source would misfire on comments and on scripts that pass anyway. If a fourth script lands in this same trap, that is the moment to reconsider.

## TODO

- Edit the `ServerLink` entry in `tests/DAE.Server.Tests/coverage-manifest.json` to the form above.
- Correct the two `docs/modernization-plan.md` passages named under *Documentation to correct*.
- `dotnet build Dataphor.slnx`, then `dotnet test tests/DAE.Server.Tests --filter Name=ServerLink --logger "console;verbosity=normal"` — expect `pass: 1 passed, 0 failed`.
- Run the three parts of the suite from the root `AGENTS.md` ("Agents run the suite in parts"), each as its own foreground command, and record the counts. Part 3 holds `ServerLink`; its known-failure count drops by one and its pass count rises by one.
- Preserve each edited file's line endings and byte-order-mark state; make no other change to the manifest's entries.
