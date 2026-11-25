description: Review the change that stopped every statement opening a transaction on every other Dataphor server the session had ever queried; such a server is now brought into a transaction only when a statement actually uses it.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md, docs/architecture/sessions-processes-plans.md, docs/modernization-plan.md
difficulty: hard
----

## Vocabulary, for a reader without the context

A *server link* is a D4 object naming another Dataphor server. The first `ExecuteOn`, `EvaluateOn` or `select … on <link>` opens a connection to that server and the session's process keeps it; the engine calls such a connection a **remote session**, and `System.RemoteSessions` lists them. A process also holds one **device session** per back-end database it has used. Both are *participants* in the process's transactions. *Enlisting* a connection in a transaction means beginning a transaction on the other server, one begin per local transaction level.

This change is only about connections to other **Dataphor** servers. Device sessions are still enlisted eagerly and nothing about them changed.

## What the change is

The eager begin is gone. `ServerProcess.InternalBeginTransaction` no longer speaks to any connection; a connection joins the process's open levels when a statement uses that link, through the new `ServerProcess.EnlistRemoteSession`, which `RemoteConnect` now calls on *every* use (it used to catch up only a newly built connection). A connection's open levels are therefore a contiguous prefix of the process's level stack, and the prepare, the commit and the rollback each ask a connection only about the level it holds, through the existing `HoldsEndingLevel`.

The dead-connection reclaim moved with the begin: it now lives in `RemoteConnect`'s held-connection arm. A connection whose catch-up fails while it holds no level of its own is let go and the same use reconnects once; one that holds a level fails the use and is kept.

## The measurement

Measured as a count of calls the process makes on the connection, with `ConnectionTransactionLevelTest`'s `RecordingRemoteSession` standing in for it — exact and deterministic, and each of those calls is one network round trip in a deployment. Taken on a session holding one connection, under the default `UseImplicitTransactions`, for a statement that has **nothing to do with** the link:

| Statement | Before | After |
|---|---|---|
| succeeds | **3** — `begin CursorStability at level 1`, `prepare level 1`, `commit level 1` | **0** |
| fails at run time (`raise Error(...)`) | **2** — `begin CursorStability at level 1`, `rollback level 1` | **0** |
| rejected by the compiler (`select NoSuchTableHere;`) | **0** | **0** |

The third row was not in the ticket and is worth knowing: a statement the compiler rejects never cost anything even before the change, because the implicit transaction is begun after compilation. So the "two calls per failing statement" figure holds for a run-time failure only.

Both "before" figures were read off a real pre-change run (the test asserting `Is.Empty`, with the engine edit not yet applied). No time figure is given: this suite has no second server on a network to measure one against. With implicit transactions off the saving is per *begin* rather than per statement, which is the smaller case.

## What a transaction now promises

A remote read inside a transaction sees the other server's state as of this process's **first use of that link inside the transaction**, not as of the transaction's start. Two statements in one local transaction that both use the link are still consistent with each other — they share one remote transaction — but that remote snapshot is taken later than the local one. An application that needs the old timing touches the link at the top of the transaction (`ExecuteOn('<link>', 'select 1;')` is enough). This is the cost the owner accepted on 2026-11-22; it is in `docs/migration-notes.md`. Nothing else changed: the remote half of a transaction is still committed and rolled back with its local half, and the sequential commit's partial-failure behaviour (`ServerTransaction.CommitBegun`, error 106213) is untouched.

## Tests

### Five new cases, all in `tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs`

Each was seen to fail before the engine edit; how it failed is given, because that is the evidence the case pins something.

- **`TestAStatementThatDoesNotUseALinkMakesNoCallOnItsConnection`** — a statement with nothing to do with the link makes no call on the connection, and the connection is *kept*. Failed before with `Expected: <empty> But was: < "begin CursorStability at level 1", "prepare level 1", "commit level 1" >`. This is where the measurement above is taken; it carries both the successful and the run-time-failing statement under one `Assert.Multiple`, so that one path's count is not hidden by the other's failure.
- **`TestALinkUsedAtTwoLevelsIsEnlistedAtEachAsItIsReached`** — the connection joins the second level at the *use*, not at the begin, each level at its own isolation level. Failed before at `TransactionCount` after the nested begin: `Expected: 1 But was: 2`. The discriminator is the count read between the begin and the use; the call log alone is identical before and after, because the only thing that moved is *when* the begin happens.
- **`TestACommitOfALevelTheConnectionDoesNotHoldSkipsIt`** — committing a level the connection does not hold reaches it not at all; committing the level it does hold reaches it. Failed before with `Expected: <empty> But was: < "prepare level 2", "commit level 2" >`. This is the case that would trip the assertion the prepare used to carry.
- **`TestADeadConnectionHoldingNoLevelIsLetGoWhenTheLinkIsUsedAgain`** — the moved reclaim. Failed before with `Expected: <empty> But was: < "begin CursorStability at level 1" >`, i.e. the pre-change begin spoke to the connection and released it there. **Note the shape:** the case deliberately runs a statement that does *not* use the link while the connection is dead, and asserts nothing happened; without that arm the case passed both before and after, since the reclaim only changed site.
- **`TestADeadConnectionHoldingALevelFailsTheUseAndIsKept`** — the participant arm: the use fails reporting error 106212, the connection is kept, and the one level its part-way catch-up had already opened is kept too (a shorter prefix is legal; nothing is unwound). Failed before by not throwing at all (`Expected: instance of <System.Exception> But was: null`), because the pre-change nested begin had already brought the connection to depth 2 and the use made no call.

`RecordingRemoteSession` gained `FailNextBegin` beside its `FailNextCommit`, and `ConstructionCount` so a case can tell a reconnect from a kept connection; both are reset in `[SetUp]` with the other statics.

### Four adapted cases

- **`ConnectionTransactionLevelTest.TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel`** — one line added: the link is used again *inside* the inner level, which is what enlists the connection in it. Without that the commit of the inner level skips the connection and `FailNextCommit` never fires. Everything it asserts is unchanged, and it still pins what it was written for.
- **`ServerLinkConnectionTest.TestADeadConnectionIsLetGoAndTheStatementRuns` → `TestADeadConnectionStopsFailingStatementsAndIsLetGoAtTheNextUse`** — the statement still runs, but the connection is now **kept** (`ConnectionCount` 1, where it was 0). The moved reclaim is asserted in the same case: the use lets the connection go and then fails on the reconnect, after which the count is 0 and `drop server` still does not throw.
- **`ServerLinkConnectionTest.TestOnlyTheConnectionWhoseServerHasGoneIsLetGo`** — same shape, name unchanged: after the child server stops, **both** connections are kept; using the dead link lets only it go, and the live one still answers.
- **`ServerLinkConnectionTest.TestABeginOnADeadParticipantFailsNamingTheLink` → `TestAUseOfADeadParticipantFailsNamingTheLink`** — the nested begin now *succeeds* (asserted), and the failure moved to the use. It also pins the asymmetry in the unwind: the rollback of the level the connection does not hold is quiet, the rollback of the level it does hold reports, and the session recovers afterwards with the connection still held until a use lets it go.

**One thing to look at in that last case.** Its assertion on error 106212 had to change from "the code of the first `DataphorException` in the chain" to "a `DataphorException` of this code anywhere in the chain": the failure now arrives out of a D4 statement rather than straight out of `BeginTransaction`, so the outermost exception is `RuntimeException` 104229 ("Runtime error") with 106212 inside it. The error code itself is unchanged, and the fixture's `DataphorErrorCode` helper was replaced by a `FindError(exception, code)` in the style `PartialCommitTest` already uses. That is a real change in what a caller sees at the top of the chain and is worth a reviewer's judgement on whether it deserves a migration-note line of its own — the existing note already says to match on code 106212, which a chain search satisfies.

### Tests NOT added

Everything in the ticket's edge-case table marked *inspection* has none: the half-committed refusal arms (covered by `PartialCommitTest`), application transactions, `System.RemoteSessions`, the compile-time use through `Plan.RemoteConnect`, the deferred-validation window, and `CloseRemoteSession`'s deferral. See *Verified by inspection only* below for what that leaves open.

### What was run

`dotnet build Dataphor.slnx` — clean, no new warnings. Then, from the repository root, all under `dotnet test tests/DAE.Server.Tests --no-build --logger "console;verbosity=normal"` with these `--filter` expressions:

- `FullyQualifiedName~ConnectionTransactionLevelTest|FullyQualifiedName~ServerLinkConnectionTest|Name=ServerLink` — 19 cases, all pass (the `ServerLink` coverage script among them; its manifest entry is `pass` and stays `pass`, so no manifest change).
- `FullyQualifiedName~ServerLinkUserRollbackTest|FullyQualifiedName~TransactionBeginTest|FullyQualifiedName~PartialCommitTest|FullyQualifiedName~ApplicationTransactionTest` — 25 cases, all pass.

44 distinct cases, all green, about 90 seconds in total. No pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written.

## Tripwires parked in the code

- **`ServerProcess.InternalPrepareTransaction`**, `NOTE:` above the deferred-validation program — the deferred handlers and the deferred constraint validation run *after* the participant loops, so a handler or constraint expression that uses a server link enlists that connection into the level being prepared, after the prepare has passed it by; the commit then commits a level this process never prepared on it. Nothing is lost (a Dataphor server's own commit prepares first), but the early-failure chance is. Pre-existing in kind — a handler opening a *new* connection has always done this — and lazy enlistment widens it to connections already held. The note says what would close it (loop the prepare until no participant appears) and why that is not done here.
- **`ServerProcess.EnlistRemoteSession`**, `NOTE:` — the whole design rests on `RemoteConnect` being the only way work reaches a connection. Verified by grep over `src` and `libraries` before the note was written: `Plan.RemoteConnect` and `Program.RemoteConnect` both delegate to it and are the only ways a plan node reaches a connection (`SystemNodes.cs` for `ExecuteOn`/`EvaluateOn`, `TableNodes.cs` for `select … on <link>`); no site holds a `RemoteSession` across statements; the one other caller, in `Runtime.Data/SystemTableValues.cs`, is inside `#if OnExpression` and is not compiled. The note exists so a future caller that bypasses the funnel is recognised as the defect it would be.
- **`ServerProcess.DiscardRemoteSession`**, `NOTE:` rewritten — why the reclaim now sits at `RemoteConnect`, and that a failed `Execute` or `Evaluate` is still not a reclaim point.

## Verified by inspection only

- **Application transactions** touch neither `RemoteSessions` nor the process's level stack. `ApplicationTransactionTest` passes but exercises no server link.
- **`System.RemoteSessions`** reports link name and process id only, so the change cannot affect it. `ConnectionCount()` in both fixtures reads it, which exercises the path but asserts nothing about its columns.
- **Compile-time use** (`Plan.RemoteConnect` → `PrepareTableVar`, for `select … on <link>`): enlists during compilation; with a cached plan the enlistment comes from the runtime path instead. Either way the connection is enlisted before any remote work runs. No test covers `select … on <link>` at all in this suite — the `ServerLink` coverage script uses `ExecuteOn`/`EvaluateOn`.
- **`CatalogDeviceSession.CloseRemoteSession`** still defers the close on `ServerProcess.InTransaction` rather than on the connection's own count, so it still waits for the outermost commit even for a connection holding no level. Conservative and correct; the stale parenthetical in its remarks was corrected and the reason for asking the process rather than the connection was written down. `ServerLinkConnectionTest`'s drop, alter and rollback cases cover it.
- **The `Error.AssertFail` in `EnlistRemoteSession`** is, strictly, implied by the `while` loop's exit condition: it can only fire if `RemoteSession.BeginTransaction` over-counts. The ticket asked for it as the statement of the guarantee at the seam that gives it, which is what it is; a reviewer who thinks that is not worth a branch has grounds.

## Documentation

- **`docs/migration-notes.md`** — the existing *D4, catalog and libraries* row about a statement no longer failing on a dead connection was rewritten (it said the release happens at the transaction begin and that every transaction begins one on every connection, both now false). A **new** row in the same section covers the enlistment timing, what a remote read now sees, and the `ExecuteOn('<link>', 'select 1;')` remedy. In *Custom C# extensions*, one sentence appended to the `RemoteSession.TransactionCount` row: the count can now be less than the process's depth.
- **`docs/architecture/sessions-processes-plans.md`** — the "begun on **every participant**" sentence rewritten. The page is now **1,491 words of its 1,500-word cap** (`python tools/docs-size-check/check_size.py` reports 13 files checked, 0 over). Net +37 words. The next edit to that page has almost no room; splitting it is `debt-split-the-modernization-plan`'s sibling problem and is not this ticket's business.
- **`docs/modernization-plan.md`** — one sentence appended to each of rows K39 and K40, naming this ticket and saying what changed. Their measurements and dates were left alone.

## Compatibility

Nothing on the compatibility list changed: no D4 semantics or operator signatures, no registered class names, no assembly names or namespaces, no file formats, no error codes (106212 and 106213 keep their meanings and their sites), no WCF contract shapes, no ports, no on-disk layout. No public type or member changed shape; `RemoteSession.TransactionCount`'s *range* widened, which the migration note covers. The two behaviour changes an application can notice — when a remote read's snapshot is taken, and when a dead connection is released — are both in `docs/migration-notes.md`.

## Review focus

- **The prefix invariant on `RemoteSession.TransactionCount`** is the load-bearing claim. It is written out there with its proof sketch; check the sketch against the three code sites it names (`EnlistRemoteSession`, the three ending operations, and the two `finally` blocks that count levels down).
- **`RemoteConnect`'s control flow**: the held-connection arm falls *through* to the connect below after a discard, which is what makes the retry exactly one. Worth reading for whether a second failure path could reach the connect twice.
- **The error-chain change** in `TestAUseOfADeadParticipantFailsNamingTheLink`, described above — the only place where what a caller sees at the top of an exception chain changed.
- **`FindError`** now exists, identically, in three fixtures (`PartialCommitTest`, `ConnectionTransactionLevelTest`, `ServerLinkConnectionTest`). Per-fixture helpers are this suite's house style, but three copies is the point at which that stops being obvious.
- **The deferred-validation tripwire** — whether it is genuinely conditional (parked as a `NOTE:`) or a latent defect that should be a ticket. The judgement made here: nothing is lost, only the early-failure chance, and the window is pre-existing in kind, so it is a tripwire.
