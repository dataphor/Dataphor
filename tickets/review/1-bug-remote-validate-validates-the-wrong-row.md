description: When a form running outside the server asked the server to check the row a user had just edited, the server checked the row as it was before the edit instead. Fixed, with the first test fixture in this repository that stands the shipped client stack up in process.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Server/RemoteServerCursor.cs, tests/DAE.Server.Tests/ClientStackTestFixture.cs, tests/DAE.Server.Tests/RemoteValidateTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/modernization-plan.md
----
## What changed

**The fix is one line.** `RemoteServerCursor.Validate` — the server's entry point for the remote "is this row acceptable" call — called its own body as `InternalValidate(newRow, oldRow, column)`, and `InternalValidate` builds both the row it validates and the row it sends back from its *second* argument. It now passes them through in the declared order, as the two siblings in the same file already do.

```
src/engine/Server/Server/RemoteServerCursor.cs:733
-   return InternalValidate(newRow, oldRow, column);
+   return InternalValidate(oldRow, newRow, column);
```

**Siblings checked, as the ticket asked.** `Default` (one row, no order to get wrong) and `Change` both pass their arguments straight through to their `Internal...` bodies, and those bodies build the validated/returned row from the *new* row parameter. Both are correct; no tests were added for them.

**New shared fixture, `tests/DAE.Server.Tests/ClientStackTestFixture.cs`** — the first thing in this suite that stands the shipped client stack up in process: a `LocalServer` over a `RemoteServer` over the engine, created once per fixture, with a client session/process **and** an engine-side session/process created per test. That second session is what the two sibling tickets want (a session that can change the catalog while the client holds a cursor open), and it is also what lets a case compare the client stack's answer with the engine's own. Deliberately not `ProtocolClient`: that goes over CoreWCF and needs a hosted instance with a port, and these defects are above the transport (`CoreWcfProtocolTest` covers the transport).

**Tests added** (`tests/DAE.Server.Tests/RemoteValidateTest.cs`, 3 cases):

| Case | What it verifies |
|---|---|
| `TestTheNewRowIsTheRowValidated` | The value the user just typed is the one checked: old `Amount` 5 / new `Amount` -1 against a `value > 0` column constraint must be refused through the client stack, exactly as the engine's own cursor refuses it. |
| `TestTheOldRowIsNotValidated` | The row as it was is not checked: old `Amount` -1 / new `Amount` 5 must be accepted, so a stored value that already fails a check is not reported against a user whose edit is fine. |
| `TestTheValidatedRowIsTheRowReturned` | With a validate handler that sets a second column, the row sent back is the row that was checked — the user's `Amount` 20 survives (with the handler's `Note = 'seen'` applied) instead of being replaced by the stored 10. |

Each case asks the same question twice — once on `ClientProcess`, once on `EngineProcess` — and asserts the two outcomes are equal, so what is pinned is "the client stack agrees with the engine" rather than a transcribed expectation (there is no reference runtime). The third case also names its two expected values outright, so a regression that broke *both* paths the same way could not pass by agreeing with itself.

**Both directions measured.** With the fix in place all three pass. With the swap put back (and only that), all three fail, with exactly the measurements the fix ticket recorded:

```
TestTheNewRowIsTheRowValidated:    Expected Refused = True,  Amount =    But was Refused = False, Amount = -1
TestTheOldRowIsNotValidated:       Expected Refused = False, Amount = 5  But was Refused = True,  Amount =
TestTheValidatedRowIsTheRowReturned: Expected Amount = 20, Note = seen   But was Amount = 10, Note = seen
```

That round trip matters more than usual here: the remote path is reached only when the validation is **not** remotable, so a test that accidentally took the client-side shortcut would have passed either way and pinned nothing. Each table therefore tags its edited column `DAE.IsValidateRemotable = "false"`, and the column constraint is added by a separate `alter` — on the constraint the same tag would be metadata of the constraint and would leave the column's own flag alone.

**Docs.** A `done` row in the *D4, catalog and libraries* section of `docs/migration-notes.md` (an application that worked around the wrong answer should remove the workaround), and **K44** in the known-defects table of `docs/modernization-plan.md`, naming this slug.

## What a reviewer should look at

- **The fixture's teardown order,** which is the least-covered thing here: client process → client session → engine process → engine session per test, then `LocalServer` → `RemoteServer` → instance at fixture level (outermost first, because the `LocalServer`'s connection is registered with the `RemoteServer`, which holds the engine). Nothing in the suite had this shape before, so there is no precedent to check it against. Nested `try`/`finally` in `TearDown` means a failure in one step still runs the rest. Two servers in one process share some engine static state (the `NOTE:` at the top of `TestServerInstance` lists what has been fixed); the `LocalServer` starts an internal `Engine` of its own, so every test of this fixture runs with two engines live, and all three cases plus the neighbouring runs were clean.
- **Whether `ValidateOutcome` collapsing every refusal to `Refused = true` is the right granularity.** It deliberately does not compare error text: the client stack wraps the engine's exception, so the two messages are not meant to be identical, and the ticket's claim is about the verdict and the row, not the wording. A reviewer who wants the error *code* compared instead has a reasonable case.
- **Whether `FillRow`'s `Note` handling is too clever.** The same helper serves a two-column and a three-column table by setting `Note` only when a value is passed, and reads it back only when the row type has the column. One helper over two shapes was cheaper than two helpers; a reviewer may disagree.

## Known gaps

- **No test for `Default` or `Change` through the client stack.** Both were read and are correct, and the ticket asked for that to be said rather than tested. The consequence is that if a future edit swaps *their* arguments, nothing in the suite catches it. Filing a general "every proposal call through the client stack agrees with the engine" case is the architecture-rung option and was not done here; the fixture now makes it a small piece of work, and a reviewer may judge it worth a `debt-` ticket.
- **Nothing exercises this over the wire.** `OutOfProcessProtocolTest` drives a real client against a server in another program but does not make a proposal call, so the WCF layer's marshalling of `Validate`'s two row bodies is still untested. The argument for leaving it: the protocol layer only marshals the calls `RemoteServerCursor` and `LocalCursor` already make, and `ContractParityTest` plus `CoreWcfProtocolTest` cover the shape and the transport. It is a real gap all the same.
- **The validate handler in the third case is minimal** — it sets a column and returns true. A handler that *refuses* the row, or one that changes the column the user edited, would exercise more of the path; the case was kept to what the ticket measured.

## Tests run

Build: `dotnet build Dataphor.slnx` — 0 errors (143 warnings, all pre-existing CA1416/SYSLIB/CS0618 in untouched projects).

- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~RemoteValidateTest|FullyQualifiedName~CursorTest|FullyQualifiedName~CoreWcfProtocolTest|FullyQualifiedName~ContractParityTest"` — 24/24 passed, 34 s. (The fix ticket wrote `Name=CursorTest`; `Name` matches the test *method*, so that filter selects nothing — `FullyQualifiedName~` is what picks the fixture up. `ContractParityTest` was added because the diff is on a WCF-facing class, although its signature did not change.)
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~TypeChangeSummaryTest|FullyQualifiedName~ColumnChangeRollbackTest|FullyQualifiedName~OutOfProcessProtocolTest"` — 14/14 passed, 23 s. The proposal-call neighbours: `TypeChangeSummaryTest` and `ColumnChangeRollbackTest` ask all three proposal calls on the engine, and `OutOfProcessProtocolTest` drives the real client stack over the wire.
- No pre-existing failures met, so nothing was written to `tickets/.pre-existing-error.md`.

No tripwires were parked for this change: the fix removes code behaviour rather than adding a conditional cost, and the fixture's one non-obvious constraint (teardown order, two engines per process) is a `NOTE:`-free comment at the site because it describes what the code does rather than a condition that might trip later.
