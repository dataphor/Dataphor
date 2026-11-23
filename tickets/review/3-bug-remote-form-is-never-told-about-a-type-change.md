description: A form already open outside the server now notices when a data type gains a check, a default or a change handler, as long as that client has heard from the server about anything else since the form opened; a form that has heard nothing still reports the change only when the row is saved.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/protocol/Client/Server/LocalCursor.cs, src/protocol/Client/Server/LocalPlan.cs, tests/DAE.Server.Tests/RemoteProposalStalenessTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/modernization-plan.md, docs/migration-notes.md
repro: verified
----
## What landed

A client outside the server answers three questions about the row a user is editing — what should this column default to, what follows from the user having changed this column, is this row acceptable — out of its own copy of the schema whenever the server has said it may. The permission is computed once, when the plan is prepared (`ServerProcess.CopyDataType` emits `DAE.IsChangeRemotable`, `DAE.IsDefaultRemotable` and `DAE.IsValidateRemotable`, each as "the work is remotable, **or** there is no work"), and `LocalCursor.Default`, `Change` and `Validate` read it back through `TableVar.Is*CallRemotable`. A form holds its plan for as long as the user keeps the form open, and nothing re-sends the permission or the copy it is exercised against, so a data type altered after the form opened was invisible to that form.

The fix is one extra condition at each of the three call sites: `IsPlanSchemaCurrent`, a new private property on `LocalCursor` that compares the plan's own recorded catalog generation with the newest generation the client has been told about. A plan that is behind sends the proposal to the server, which answers from the live schema (which `bug-rolled-back-type-constraint-stops-the-column-check`, K43, made correct for an open server-side cursor).

- `LocalPlan` gained `internal long PreparedCacheTimeStamp`, returning `_descriptor.CacheTimeStamp` — the value `Catalog.CacheTimeStamp` had when the plan was prepared, which `RemoteServerPlan.GetCatalog` puts in every expression plan's descriptor.
- The client's highest-known value is `LocalServer._serverCacheTimeStamp`, read directly: it is already `protected internal` and `LocalCursor` is in the same assembly. No second field was added — see *What to challenge* below.
- The whole explanation (why the client answers these at all, why `CacheTimeStamp` and not `ShouldCallProposablesTimeStamp`, why the read is unsynchronized, and the window left open) is in the doc comment and the `NOTE:` at `IsPlanSchemaCurrent`. It is long because the mechanism is spread over five files and no one of them said it; cut it if you think it over-explains.

Measured against an in-process server with the shipped client stack in front of it (a `LocalServer` over a `RemoteServer` over the engine), with the type altered on a session of the engine's own while the client's cursor stayed open. The three failures the ticket predicted reproduced exactly, with the values it recorded — the created check was not refused, the created default came back with no value, the attached change handler left 2 where the engine left 99 — and all three pass with the condition in place. The reproduction was run before the fix, in this stage, not taken from the ticket.

## Use cases to exercise

The behaviour is a Windows client form, not a script, so the cases below are what a reviewer should ask of it. All of them are reachable from `ClientStackTestFixture`, which stands the shipped client stack up in process.

- **A form open across `alter type T { create constraint ... }`.** The user edits a column of type `T`, leaves the field, and must be told then rather than on saving. Needs the client to have prepared anything at all since the `alter` — a lookup, a detail view, another form on the same session.
- **The same across `alter type T { create default ... }`.** A new row must come back carrying the new default. Note that the *whole-row* default is decided by the table-level summary, so a stale permission costs every column of the table its default, not only the column whose type changed.
- **The same across `attach operator ... to T on { change }`.** This one needs the attach to have moved `Catalog.CacheTimeStamp` at all, which it does only since `bug-attached-handler-never-reaches-a-remote-client` (K45) — it is the arm that would silently stop working if that bump were ever reverted.
- **A form that has heard nothing.** No prepare of any kind between the catalog change and the edit: the client still answers out of its own copy and still reports the change on saving. This is deliberate and is the one case to check has *not* changed, because closing it costs a round trip per proposal or a notification channel this architecture does not have.
- **The cost side.** A form whose plan is behind pays a round trip for every default, change and validate call until the plan is replaced — which happens when the form is reopened. Worth a thought about whether any shipped Frontend path makes these calls in a loop; nothing was found, but nothing was measured either.
- **A plan prepared *after* the change** must go back to answering locally. Covered incidentally by every other fixture on this stack (each opens its cursors fresh), but worth an eye: an error in the comparison's direction would send every proposal to the server for ever and no test would fail.

## Tests

`tests/DAE.Server.Tests/RemoteProposalStalenessTest.cs`, new, on `ClientStackTestFixture`. Each case opens a cursor on both stacks before the change, alters the type on the engine while both stay open, and asks the same question of both — the engine as the oracle, as the two sibling fixtures on this stack do, because there is no reference runtime to transcribe an expected value from — then names the resulting value so that both stacks being wrong the same way could not pass. Each also asserts that the client's copy of the table variable *still says the call is remotable*, so what sends the proposal to the server is the staleness test and nothing else.

| case | what it verifies |
|---|---|
| `TestCreatedTypeConstraintRefusesThroughAnOpenClientCursor` | the `Validate` arm; seen to fail before the fix, client accepted where the engine refused |
| `TestCreatedTypeDefaultIsOfferedThroughAnOpenClientCursor` | the `Default` arm; seen to fail before the fix, client proposed no value where the engine proposed 7 |
| `TestAttachedTypeChangeHandlerRunsForAnOpenClientCursor` | the `Change` arm, and that attaching a handler to a type moves `CacheTimeStamp`; seen to fail before the fix, client left 2 where the engine left 99 |
| `TestAClientThatHasHeardNothingStillAnswersFromItsOwnCopy` | the window left open, and with it the optimization itself |

The fourth case is one more than the ticket asked for and is the one to weigh. Its justification: a current plan's local answer and the server's answer agree by construction, so "the client answered locally" has no behavioural observable — which means an inverted or always-false comparison would send every proposal to the server, cost a round trip each, and pass all three of the other cases. This case is the only thing in the suite that would fail. It does so by pinning a cost rather than a goal, which its remarks say plainly, together with the instruction to rewrite rather than restore it if the window is ever closed. If you disagree that a performance-only regression deserves a case, it is the one to cut.

TypeChangeSummaryTest's view and restriction shapes were deliberately not mirrored (on the client those exercise the client's own node tree, which that fixture already pins; what is new here is the permission, not the descent), nor its two rolled-back cases (their client copy is taken at prepare time and therefore before the drop, so it is correct either way and the case would exercise no invalidation on this side at all).

## What to challenge

- **Only one of the two reading points was implemented.** The ticket offered `LocalServer._serverCacheTimeStamp` (recommended) or a second field recording the highest `PlanDescriptor.CacheTimeStamp` from anywhere, statement plans included. I took the first. The limit is real: a client that *only executes statements* between the catalog change and the edit never raises the field and keeps shortcutting. It is conservative in the safe direction — the field can never report a stale plan as fresh in the other direction — and the second field would have meant two pieces of state with meanings close enough to confuse. Worth a second opinion on whether the wider one is in fact a one-line change where descriptors arrive.
- **The field is read without synchronization**, where every write to it is made under the client's exclusive cache lock. The reasoning is in the doc comment: a lock per proposal taken to decide whether to take a lock, against a worst case of one proposal answered the way it was answered before this change. It also only ever rises, so a stale read can only read it low, which makes the test more permissive and never costs an unneeded round trip. Check that reasoning.
- **`PreparedCacheTimeStamp` is a new `internal` member on the public `LocalPlan`.** Additive, no existing extension can notice it, and no `docs/migration-notes.md` entry was written for it — the same call the K45 review made for `DDLNode.InvalidateCachesForHandlerChange`. Disagree if you read the project's public-surface rule more strictly.
- **Condition order.** `IsPlanSchemaCurrent` is evaluated before `TableVar.Is*CallRemotable(columnName)`. `TableVar` lazily calls `LocalExpressionPlan.GetDataType()` when `_dataType` is null, which itself raises the client's value; for a cursor's plan `_dataType` is always set, because the constructor calls `GetDataType()`. So there is no ordering hazard, but it rests on that invariant and I did not look for a path that could invalidate it.
- **No magnitude was measured.** The round-trip cost of a proposal on a stale plan is not quantified anywhere in this change, and no claim about it is made. The `5.65 µs` figure in the plan record is K43's server-side recompute, carried over from the earlier ticket's measurement, not something re-measured here.
- **The comment is long.** Roughly 40 lines of doc comment and `NOTE:` for a one-line test. It carries the mechanism that no single file states, the counter choice with its reasoning, the synchronization argument and the accepted window. Trim it if you think any of those belongs elsewhere or nowhere.

## Tripwires parked at their sites

- `LocalCursor.IsPlanSchemaCurrent`, the `NOTE:` — the window that is left: a form that prepares nothing between the catalog change and the user's edit still reports the change only when the row is saved. What would change the answer is named there: a per-proposal round trip becoming acceptable, or a server-to-client notification channel built for some other reason that this could ride on.

## Tests run

Build: `dotnet build Dataphor.slnx` (and once with `--no-incremental`) — 0 errors, 143 warnings, unchanged from the pre-change baseline and none in the edited files.

Targeted, as the project's stage rule asks:

- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~RemoteProposalStalenessTest"` — 4 passed. Run first **without** the fix: 3 failed with the values above, the fourth passed.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~RemoteProposalStalenessTest|FullyQualifiedName~RemoteValidateTest|FullyQualifiedName~RemoteEventHandlerCacheTest|FullyQualifiedName~TypeChangeSummaryTest|FullyQualifiedName~CatalogRollbackSnapshotTest|FullyQualifiedName~CursorTest"` — 120 passed, 33 s. The ticket's named set plus the two existing fixtures on the client stack.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~CoreWcfProtocolTest|FullyQualifiedName~OutOfProcessProtocolTest|FullyQualifiedName~SchemaCacheInvalidationTest|FullyQualifiedName~ColumnChangeRollbackTest"` — 34 passed, 44 s. The two transports that reach the edited code for real (`OutOfProcessProtocolTest` runs the client in a separate process over a socket), plus the two neighbouring fixtures over catalog invalidation and the change proposal.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "Name=ServerLink|Name~RemoteATOperatorEvaluation|Name~DeferredConstraintChecking|Name~EventHandlers"` — 9 passed. `ServerLink` is the one coverage script that stands a `LocalServer` up over a second server in a child process, so it is the script that runs the edited file; the other three are the scripts over event handlers and remote application-transaction operator evaluation.

No coverage-manifest entry changed. The three whole-suite part commands were not run: the diff is two files in the client stack, one new test fixture and two documents, and nothing in the build, the lexer, the parser, the compiler or the coverage runner that every script passes through. No pre-existing failures surfaced; `tickets/.pre-existing-error.md` was not written.

## Docs changed

- `docs/modernization-plan.md`: K43's closing sentence — which said the remote-client arm was open and named this slug — replaced by what landed, the measurement, the counter choice with its reasoning, the window left open and where the `NOTE:` is; its Where column gained the two client files, its Status now reads "fixed (both arms verified; one window left open deliberately)" and its Fix in column names this ticket beside the original. K45's forward reference to this ticket changed to past tense. No new K record: the ticket asked for K43 to absorb this arm, and it is the same defect reached by a second route.
- `docs/migration-notes.md`: one new entry in the engine section — the last one, which matters because three entries now point at it as "the last entry in this section". Two existing entries were corrected rather than left to contradict it: the K43 entry said an open form in a client "still behaves as it did before this change, which is tracked separately", and the K45 entry said twice that an already-open form "still does not notice" an attached handler and that reopening was the workaround. Both are now true of what ships, and both name the conditions.
