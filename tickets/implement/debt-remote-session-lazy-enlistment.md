description: Today every statement a session runs opens a transaction on every other Dataphor server that session has ever queried, even when the statement has nothing to do with those servers - three network round trips each, per statement. Bring such a server into a transaction only when a statement actually uses it.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md, docs/architecture/sessions-processes-plans.md, docs/modernization-plan.md
difficulty: hard
----

## Vocabulary, for a reader without the context

A *server link* is a D4 object naming another Dataphor server. The first `ExecuteOn`, `EvaluateOn` or `select … on <link>` opens a connection to that server and the session's process keeps it; the engine calls such a connection a **remote session** (`src/engine/DAE/Server/RemoteSession.cs`), and `System.RemoteSessions` lists them. A process also holds one **device session** per back-end database it has used. Both are what the engine calls *participants* in the process's transactions.

*Enlisting* a connection in a transaction means beginning a transaction on the other server, one begin per local transaction level. *Catching up* a connection means beginning, one at a time, as many levels on it as the process has open - the loop in `ServerProcess.RemoteConnect`, which runs when a connection is opened part-way through a session's transactions.

This ticket is only about connections to other **Dataphor** servers. Device sessions keep being enlisted eagerly and nothing about them changes.

## The owner's decision

The owner chose, on 2026-11-22: **enlist a connection to another Dataphor server in a transaction only when a statement uses it.** The eager begin on every held connection goes, and the catch-up becomes the only way a connection joins open transactions. The decision explicitly accepts the change in what a transaction spanning two Dataphor servers promises, set out under *What a transaction now promises* below.

Lineage: this was the fourth open question on `bug-dead-remote-session-blocks-every-statement` (landed; `tickets/complete/`), and it depends on `bug-remote-catchup-uses-innermost-isolation` (also landed) having made the catch-up open each level at that level's own isolation level. The planning step has since settled the three questions that were left open - the invariant, the ending operations, and where the dead-connection reclaim goes - and they are written out below. Nothing in this ticket is left to the implementer's judgement about *what* the engine should do.

## What happens today

`ServerProcess.InternalBeginTransaction` begins a transaction on every connection the process holds. With implicit transactions on - the default - every statement begins a transaction, so a session that queried another server once pays, on **every** statement afterwards, a begin, a prepare and a commit on that connection: three calls over the network, whether the statement will ever touch the link or not. A failing statement pays two (begin, rollback).

The machinery to bring a connection in later already exists and is used: `RemoteConnect` catches a *new* connection up to the process's depth before adding it to the list.

## The change

### 1. The begin stops touching connections

In `InternalBeginTransaction`, delete the remote-session loop, the `remoteSessionsBegun` list and the snapshot copy of `RemoteSessions` that the loop needed. `RollbackBegunParticipants` loses its remote arm and its `remoteSessionsBegun` parameter, becoming device-only; rewrite its summary and remarks accordingly (its "the remote sessions are named rather than counted" remark goes with the arm). The device loop, the `UseDTC` check and the half-committed refusal above it are untouched.

### 2. One named catch-up, used by every enlistment

Extract the catch-up loop into a private method on `ServerProcess`, so there is exactly one place a connection is enlisted:

```csharp
/// <summary>Brings a connection to another Dataphor server up to this process's transaction depth, beginning each level it is missing at the isolation level that level was begun at.</summary>
private void EnlistRemoteSession(RemoteSession session)
{
    // Indexed by the connection's own depth, as DeviceConnect's catch-up is: the levels being caught up on are the
    // outer ones, each with the isolation level it was begun at.
    while (session.TransactionCount < _transactions.Count)
        session.BeginTransaction(_transactions[session.TransactionCount].IsolationLevel);

    // The guarantee RemoteConnect gives its callers, asserted at the one seam that gives it: work only ever reaches a
    // connection through RemoteConnect, so every remote operation runs at a remote level that corresponds to this
    // process's innermost level.
    Error.AssertFail
    (
        session.TransactionCount == _transactions.Count,
        "The connection for server link \"{0}\" has {1} transaction level(s) open where this process has {2}.",
        session.ServerLink.Name,
        session.TransactionCount,
        _transactions.Count
    );
}
```

### 3. `RemoteConnect` catches up the connection it already holds

Today the `else` arm returns the held connection as it is. It must catch it up, and it is now where the dead-connection reclaim lives:

```csharp
internal RemoteSession RemoteConnect(Schema.ServerLink serverLink)
{
    CheckNotHalfCommitted();

    int index = RemoteSessions.IndexOf(serverLink);
    if (index >= 0)
    {
        RemoteSession heldSession = RemoteSessions[index];
        try
        {
            EnlistRemoteSession(heldSession);
            return heldSession;
        }
        catch (Exception exception)
        {
            // The same rule the transaction begin applied before this reclaim moved here: a connection with no level of
            // its own open is a participant in no transaction this process still has open, so nothing has been done on
            // it that this process's transaction protocol still owes an answer for, and it may be let go and this use
            // connect again. One that does hold a level may not be - work has been done on it at a level that is still
            // open, and forgetting it would let the local commit believe the remote half committed - so its failure is
            // the caller's. A connection whose catch-up failed part-way keeps the levels it did open, which is a legal
            // state under the invariant on RemoteSession.TransactionCount and needs no unwinding.
            if (heldSession.InTransaction)
                throw;

            DiscardRemoteSession(heldSession, exception);
        }
    }

    RemoteSession session = (RemoteSession)Activator.CreateInstance(Type.GetType(RemoteSessionClassName, true), this, serverLink);
    try
    {
        EnlistRemoteSession(session);
        RemoteSessions.Add(session);
        return session;
    }
    catch
    {
        session.Dispose();
        throw;
    }
}
```

Exactly one retry: a discarded connection falls through to the connect below, and a failure there reaches the caller. `CheckNotHalfCommitted` stays where it is and unchanged - see the edge cases.

`DiscardRemoteSession` keeps its body. Two things about it change: its warning says *this* use connects again rather than the next one, and its `NOTE:` about why the reclaim sits at the transaction begin is now wrong and must be replaced with why it sits here (it is the one call this process makes on a connection it is about to do work on, and the one point at which the far server has provably done nothing yet at the level being begun; a failed `Execute` or `Evaluate` is still not a reclaim point, because that call may have been delivered and run).

### 4. The three ending operations act per connection

`InternalPrepareTransaction` must stop asserting that every connection is at the process's depth and skip the ones that are not:

```csharp
foreach (RemoteSession remoteSession in RemoteSessions)
    if (HoldsEndingLevel(remoteSession))
        remoteSession.PrepareTransaction();
```

`InternalCommitTransaction`'s remote loop takes the same guard. `InternalRollbackTransaction`'s remote loop already has it and needs no change. The ordering constraint on the commit - every remote session commits before any device session - is untouched, and so is the `NOTE:` that records why.

`HoldsEndingLevel` itself needs no change: its assertion that a participant is never deeper than the process still holds, and equality still means "holds the level being ended" for the reason given below.

### 5. The invariant that replaces the old one

The comment on `RemoteSession.TransactionCount` records today's invariant - *every remote session in a process's list has begun exactly as many transaction levels as the process has open*. Replace it with the weaker one that survives this change:

> Every remote session in a process's `RemoteSessions` list has open exactly the process's transaction levels 0 through `TransactionCount - 1`, and `TransactionCount` is never greater than the process's depth. A connection's open levels are therefore always a contiguous **prefix** of the process's level stack, and the connection holds the level a prepare, a commit or a rollback is ending exactly when its count equals the process's depth.

Why the prefix holds: the only code that begins a level on a connection is `EnlistRemoteSession`, which begins levels in ascending index order starting at the connection's own count, so the connection's level *j* is always the process's level *j*; the only code that ends one is the three ending operations, each of which ends the innermost level and only on a connection that holds it; and both `CommitTransaction` and `RollbackTransaction` count the level down in a `finally` on every path, so a connection can never be left deeper than the process.

Why this is enough for correctness: work only ever reaches a connection through `RemoteConnect`, which enlists it to the process's full depth before returning it. So **every remote operation is performed at a remote level corresponding to the process's current innermost level, and a connection that holds fewer levels than the process has had no work done on it at the levels it is missing.** There is nothing at those levels for a prepare, a commit or a rollback to say anything about, which is why skipping such a connection is right rather than merely cheap. The assertion in `EnlistRemoteSession` pins the guarantee at the seam that gives it.

`RemoteConnect` being the only funnel is what that argument rests on, so verify it rather than assume it: `Plan.RemoteConnect` and `Program.RemoteConnect` both delegate to it and are the only ways a plan node reaches a connection (`SystemNodes.cs` for `ExecuteOn`/`EvaluateOn`, `TableNodes.cs` for `select … on <link>`), and no site holds a `RemoteSession` reference across statements. The one other caller in the tree, in `Runtime.Data/SystemTableValues.cs`, is inside `#if OnExpression` and is not compiled. Record the dependency as a `NOTE:` on `EnlistRemoteSession` so a future caller that bypasses it is recognised as the defect it would be.

## What a transaction now promises

A remote read inside a transaction sees the other server's state as of this process's **first use of that link inside the transaction**, not as of the transaction's start. Under an isolation level that gives the far server's transaction a consistent snapshot, two statements in one local transaction that both use the link are consistent with each other - they share one remote transaction - but that remote snapshot is taken later than the local one. An application that needs the old timing can get it by touching the link at the start of the transaction (`ExecuteOn('<link>', 'select 1;')` is enough: the connect and the enlistment happen before the statement is sent).

This is the cost the owner accepted, and it must be written down where an application owner will meet it - see *Documentation owed*.

Nothing else about what a transaction promises changes. In particular a transaction's remote half is still committed and rolled back with its local half, and the sequential commit's partial-failure behaviour (`ServerTransaction.CommitBegun`, error 106213) is untouched.

## The dead-connection reclaim moves with the begin

`bug-dead-remote-session-blocks-every-statement` exists because every statement spoke to every connection. After this change a dead connection stops failing statements **by construction** - a statement that does not use the link never speaks to it - which is a strictly better answer to that bug than the reclaim was.

The reclaim is still worth having, for the narrower case it now covers: a statement that *does* use a link whose connection has died, while the process holds no level on that connection, discards it and connects again, so a server that has come back is simply used again. That path has the same reachability it has today - with implicit transactions on, a connection is at depth 0 between statements, so the next statement that uses the link finds it at 0 levels - and it fails the same way with the same error (106212, `RemoteSessionTransactionFailed`) when the far server is still gone.

## Measurement

The saving was never measured; measure it and record the number in the handoff. Measure it as a **count of calls on the connection**, not as wall-clock: `ConnectionTransactionLevelTest`'s `RecordingRemoteSession` records every call the process makes, so the count is exact and deterministic, and each of those calls is one network round trip in a deployment. The expected figures, to be confirmed rather than taken on trust: a session holding one connection pays **three** calls (begin, prepare, commit) on it per successful statement that does not use the link, and two (begin, rollback) per failing one, under the default `UseImplicitTransactions`; after the change, none. With implicit transactions off the saving is per *begin* rather than per statement, which is the smaller case and worth saying so.

Do not convert the count into a time figure: this suite has no second server on a network to measure one against.

## Edge cases & interactions

| Case | What must happen | How it is verified |
|---|---|---|
| Connection opened outside a transaction, levels begun afterwards, then the link used | the catch-up opens every level, each at its own isolation level | test (new, incremental catch-up) |
| Connection holds fewer levels than the process at a prepare, a commit or a rollback | skipped; no call reaches it | test (new) |
| Catch-up fails part-way on a connection that already held a level | the levels it did open are kept, the use fails, nothing is unwound - a prefix shorter than the process's depth is legal | test (new, the participant arm of the reclaim) |
| Dead connection holding no level, link used again | discarded with one warning, reconnected once; the use then succeeds or fails on the reconnect | test (new) |
| A link the statement does not use, whose server has gone | never spoken to, so the statement runs and the connection is **kept** - this is the behaviour change in the three adapted cases of `ServerLinkConnectionTest` | test (adapted) |
| Two links held, statement uses one | only that one is enlisted; the other stays at its own depth | covered by the prefix invariant and by the adapted `TestOnlyTheConnectionWhoseServerHasGoneIsLetGo` |
| Use of a link while the current level is half-committed | refused, exactly as today: `CheckNotHalfCommitted` stays unconditional at `RemoteConnect`. A connection that holds fewer levels would otherwise be caught up *into* a level that can only be rolled back, and one that holds the level is doing work on a half-committed level, which is refused on every participant, reads included. Replace the prediction in `HoldsEndingLevel`'s `NOTE:` with this settled answer | inspection; the refusal's own tests are in `PartialCommitTest` and are unaffected |
| Nested begin on top of a half-committed level | still refused, and the reason in `InternalBeginTransaction`'s comment still holds | inspection |
| Deferred handlers and deferred constraint validation, which run inside `InternalPrepareTransaction` *after* its participant loop, can enlist a connection into the level being prepared | that connection is then committed without this process having prepared it. Nothing is lost - a Dataphor server's own `CommitTransaction` prepares first - but the early-failure chance is. Pre-existing in kind (a *new* connection opened by a deferred handler has always done this); lazy enlistment widens it to connections already held. Record as a `NOTE:` at the site saying what would make it airtight (loop the prepare until no participant appears) and why that is not done here. **Tripwire, not a ticket** | inspection; `NOTE:` at the site |
| A server link dropped or altered inside a transaction while its connection holds no level | `CatalogDeviceSession.CloseRemoteSession` defers the close on `ServerProcess.InTransaction` rather than on the connection's own count, so the close still waits for the outermost commit. Conservative and correct; its remark's parenthetical "ServerProcess enlists its remote sessions in its own commits and rollbacks" should read "may hold open transaction levels on its remote sessions" | inspection; `ServerLinkConnectionTest`'s drop and alter cases already cover it |
| Compile-time use (`Plan.RemoteConnect` → `PrepareTableVar`, for `select … on <link>`) | enlists during compilation; with a cached plan the enlistment instead comes from the runtime path. Either way the connection is enlisted before any remote work runs | inspection |
| Application transactions | do not touch `RemoteSessions` or the process's level stack | inspection |
| `System.RemoteSessions` | reports link name and process id only, so unaffected | inspection |
| Device sessions | still enlisted eagerly; `HoldsEndingLevel(deviceSession)` keeps the meaning it has today | inspection |

## Rejected alternatives

- **Keep enlisting eagerly.** Rejected by the owner's decision. It is what makes a remote read line up with the local transaction's start, at the cost measured above.
- **Enlist eagerly, but only the links the plan being executed names.** Not chosen. It keeps the snapshot timing for the links a statement uses and removes the cost for the rest, but it needs the compiler to publish a plan's links, it still pays the three calls for a statement that merely *mentions* a link it does not reach, and it leaves two enlistment paths where this design has one.
- **Make the bad state unrepresentable by routing `Execute`, `Evaluate` and `PrepareTableVar` through non-virtual wrappers on `RemoteSession` that assert enlistment.** Rejected: those three are `public abstract` on a public class, so the change is to an extension point that `docs/migration-notes.md` already has a row about, and it would buy an assertion that `EnlistRemoteSession` can make at the funnel for nothing. Recorded here so a reviewer meets the reasoning rather than re-deriving it.

## Tests

The project's testing rule applies: this changes the behaviour of a subsystem, so it owes characterization tests. Five new cases, all in `tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs`, which is the lowest layer that shows this - its `RecordingRemoteSession` stands in for a connection and never connects, so no second server is needed. Give the double a `FailNextBegin` flag beside its `FailNextCommit`, and a count of how many times it has been constructed so a case can see a reconnect; reset both in `[SetUp]` with the other statics.

- **A statement that does not use a link makes no call on its connection.** Open the connection with one `ExecuteOn`, clear the log, run a statement with nothing to do with the link, assert the log is empty. This is also where the measurement above is taken: the same case before the change records a begin, a prepare and a commit.
- **A link used at two levels is enlisted at each, as it is reached.** Begin a level, use the link, begin a second level, use it again; assert the log reads `begin … at level 1`, execute, `begin … at level 2`, execute, with each begin carrying that level's own isolation level. Pins the incremental catch-up, which is new; the existing `TestRemoteSessionConnectedInTransactionsGetsEachLevelsIsolationLevel` only covers two levels caught up at once.
- **A commit of a level the connection does not hold skips it.** Begin a level, use the link, begin a second level, commit it; assert no prepare or commit of level 2 reached the connection and it still holds one level, then commit the outer level and assert that one does reach it. This is the case that trips today's assertion in `InternalPrepareTransaction`.
- **A dead connection holding no level is let go and reconnected when the link is used again.** With `FailNextBegin`, assert the first connection was discarded, a second was built, and the statement then ran.
- **A dead connection holding a level fails the use and is kept.** With the connection enlisted at level 1 and the process at level 2, `FailNextBegin`; assert the use fails with error 106212, the connection is still in the list, and it still holds its one level.

**Four existing cases must be adapted.** Each keeps the behaviour it was written to pin; what changes is the setup that reaches that behaviour, or the mechanism the behaviour now works by. Never weaken an assertion to get a green run - if one of these cannot be made to pin its behaviour again, stop and say so in the handoff.

- `ConnectionTransactionLevelTest.TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel` - the connection must hold the inner level for the commit to reach it, so add a use of the link inside the inner level. Everything it asserts then holds unchanged.
- `ServerLinkConnectionTest.TestADeadConnectionIsLetGoAndTheStatementRuns` - the statement still runs, but the connection is now **kept**, because nothing spoke to it; `ConnectionCount` goes from 0 to 1. Add the moved reclaim to the case: a use of the link then discards it and fails on the reconnect, after which the count is 0. `drop server` must still not throw. Rename to match what it now pins.
- `ServerLinkConnectionTest.TestOnlyTheConnectionWhoseServerHasGoneIsLetGo` - same shape: after the child server stops, both connections are kept; using the dead link lets only it go, and the live one still works.
- `ServerLinkConnectionTest.TestABeginOnADeadParticipantFailsNamingTheLink` - a nested begin no longer touches the connection, so the failure moves to the *use*: begin a level, use the link while the child is alive, stop the child, begin a second level (which now succeeds), then use the link again and assert it fails with 106212 and is kept. Note that the rollback of the level the connection does not hold is now quiet and the rollback of the level it does hold is the one that reports, and that the session recovers afterwards with the connection still held until a use lets it go. Rename to match.

## Documentation owed

- **`docs/migration-notes.md`.** Amend the existing *D4, catalog and libraries* row about a statement no longer failing because a connection to another Dataphor server has died: it says the release happens at the transaction begin and that every transaction begins one on every connection, both of which this change makes false. Rewrite it to say that a statement that does not use the link never speaks to the connection, and that the release now happens when a statement uses the link again. Add a **new** row in the same section for the enlistment timing: what a remote read inside a transaction now sees, that two statements in one transaction using the same link are consistent with each other, and the `ExecuteOn('<link>', 'select 1;')` remedy for an application that needs the old timing. In *Custom C# extensions*, append one sentence to the existing `RemoteSession.TransactionCount` row: the count can now be less than the process's transaction depth, because a connection joins open levels only when a statement uses the link.
- **`docs/architecture/sessions-processes-plans.md`.** The paragraph beginning "A transaction level the process begins is begun on **every participant** it has used" is now wrong for one of the two kinds of participant. Rewrite its first sentence; the rest of the paragraph (the sequential commit, `CommitBegun`, error 106213) stays. A draft that fits: *"A transaction level the process begins is begun at once on each back-end database's session it has used. A connection to another Dataphor server joins the open levels only when a statement next uses that link, so it holds the process's outermost levels up to the depth the process had when that link was last used, and the commit and the rollback ask it only about those. The commit asks the participants one at a time, with no protocol that can undo one that has already said yes."* **The page is at 1,454 of its 1,500-word cap**, so keep the net addition at or under 40 words, run `python tools/docs-size-check/check_size.py`, and if it reports over, tighten within that same paragraph. Do not split the page and do not raise the cap.
- **`docs/modernization-plan.md`.** Rows K39 and K40 of the known-defects table state things this change overturns - K39 that the reclaim is at the transaction begin and that every connection has begun as many levels as the process has open, K40 that `RollbackBegunParticipants` is the only thing that works after a begin has let a dead connection go. Append **one sentence** to each, naming this ticket and saying what changed; do not rewrite the rows, whose measurements and dates are a record of what was true then. The plan is already oversized (`tickets/blocked/debt-split-the-modernization-plan.md`), so keep both addenda to one sentence.

## Compatibility

Nothing on the compatibility list changes: no D4 semantics or operator signatures, no registered class names, no assembly names or namespaces, no file formats, no error codes (106212 and 106213 keep their meanings and their sites), no WCF contract shapes, no ports, no on-disk layout. No public type or member changes shape; `RemoteSession.TransactionCount`'s *range* widens, which the migration note covers. The two behaviour changes an application can notice - when a remote read's snapshot is taken, and when a dead connection is released - are both in `docs/migration-notes.md`.

## TODO

### Phase 1 - the engine change

- Extract `EnlistRemoteSession` with its assertion and its `NOTE:` about `RemoteConnect` being the only funnel; verify that claim with a grep over `src` and `libraries` before writing the note
- Delete the remote-session loop, the `remoteSessionsBegun` list and the list snapshot from `InternalBeginTransaction`; drop the remote arm and the parameter from `RollbackBegunParticipants` and rewrite its summary and remarks
- Give `RemoteConnect`'s held-connection arm the catch-up and the reclaim, as written above; update `DiscardRemoteSession`'s warning text and replace its `NOTE:`
- Guard the remote loops in `InternalPrepareTransaction` and `InternalCommitTransaction` with `HoldsEndingLevel`, replacing the assertion in the first
- Replace the invariant on `RemoteSession.TransactionCount` with the prefix invariant and its proof sketch; replace the prediction in `HoldsEndingLevel`'s `NOTE:` with the settled answers for both arms it names
- Add the `NOTE:` tripwire in `InternalPrepareTransaction` about deferred validation enlisting a connection after the participant loop
- Correct the stale parenthetical in `CatalogDeviceSession.CloseRemoteSession`'s remarks
- `dotnet build Dataphor.slnx` clean, and no new warnings

### Phase 2 - tests

- Add `FailNextBegin` and a construction count to `RecordingRemoteSession`, reset with the other statics in `[SetUp]`
- Add the five new cases to `ConnectionTransactionLevelTest`
- Take the measurement: record the per-statement call count on a held connection before the change (revert the engine edit or read it off the pre-change run) and after, for a statement that does not use the link
- Adapt `TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel`
- Adapt the three cases in `ServerLinkConnectionTest`, renaming the two whose subject moved
- Confirm each new case fails without the engine change - at least the three that pin new behaviour (no call on an unused link, incremental enlistment, commit skipping a level the connection does not hold) - and say in the handoff how each failed
- Targeted run, per the project's stage rule: `ConnectionTransactionLevelTest`, `ServerLinkConnectionTest`, `ServerLinkUserRollbackTest`, `TransactionBeginTest`, `PartialCommitTest`, `ApplicationTransactionTest`, plus the `ServerLink` coverage script. Name the `--filter` expressions used in the handoff. Run in the foreground with no redirection; `tee` into `tickets/.logs/` only if the output needs grepping afterwards

### Phase 3 - documentation

- The migration-notes amendment, the new migration-notes row and the `TransactionCount` sentence
- The architecture paragraph, then `python tools/docs-size-check/check_size.py`
- The one-sentence addenda to plan rows K39 and K40

### Handoff

- State the measured call counts, which tests were adapted and why each adapted expectation is right, where the tripwire `NOTE:` went, and anything that could not be verified by test rather than by inspection
