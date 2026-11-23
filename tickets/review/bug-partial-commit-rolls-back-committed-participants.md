description: A session that commits a transaction spanning more than one database, where the commit succeeds on some of them and then fails on another, no longer loses the work of the transaction outside the one that failed; and after such a failure the session is told plainly that the transaction can only be rolled back, instead of being allowed to commit the outer one by accident.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerTransaction.cs, src/engine/DAE/Server/ServerException.cs, src/engine/DAE/Server/ServerException.resx, tests/DAE.Server.Tests/PartialCommitTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
----

## What this is about, for a reader without the context

One Dataphor transaction can span several places at once. A session's *process* holds one connection per back-end database it has used - a **device session**: a SQL Server or PostgreSQL database, Dataphor's in-memory store, and always the catalog, which is Dataphor's own store of its table, type, operator and user definitions - and one per other Dataphor server it has queried, a **remote session**. Each of those is a **participant**: every transaction level the session begins is begun on every participant, and ending the level ends it on every participant.

Transactions nest: a session can hold an outer level and an inner level at once, finish the inner one, and carry on with the outer one.

The commit is sequential. The process asks each participant in turn - remote sessions first, then the back-end databases, the catalog last - and there is no protocol that can undo a participant that has already said yes. So if the fourth participant's commit fails, the first three have committed that level and the fourth has not. The engine then had no explicit record of that state and read it off raw level counts, which is where the two defects came from.

## What changed

### One predicate, asked at every site that ends a level

`ServerProcess.HoldsEndingLevel` was a one-line helper for connections to other Dataphor servers (added by `bug-remote-catchup-uses-innermost-isolation`, which named this ticket as the owner of the rest). It is now a shared body plus two thin overloads - one naming a remote session, one naming a device session - and `InternalRollbackTransaction` asks it in all three of its loops.

The question is "does this participant still hold the level this rollback is ending?", answered as *participant levels == process depth*, with *participant levels <= process depth* asserted rather than assumed. The two device loops used to ask the weaker question `InTransaction` - "is this participant in a transaction at all?" - which after a partial commit is true of a committed participant **for the level outside the one being ended**. That level was then rolled back on it, silently.

`RollbackBegunParticipants`, which unwinds a failed *begin*, deliberately still names the participants that began the level rather than counting them, and now carries a comment saying so, because the obvious "cleanup" is to convert it and that would be wrong.

### One explicit record of a half-committed level

`ServerTransaction.CommitBegun` is set in `InternalCommitTransaction` after the prepare returns and before the first participant commit, and is never cleared; the path that succeeds removes the transaction object, so a transaction object still carrying it is one whose commit failed part-way. Two operations are then refused with the new error **106213**, `TransactionCanOnlyBeRolledBack`:

- a retried commit, refused in `InternalPrepareTransaction` **before** the `Prepared` test, so it is refused whether or not the first prepare had completed;
- a nested begin, refused in `InternalBeginTransaction` beside the existing `UseDTC` check, before any transaction state changes.

A rollback is always allowed and is unchanged by this arm.

The existing `Error.AssertFail` on remote session counts in `InternalPrepareTransaction` stays, with its comment updated: the one way a correct engine could reach it was a retried commit after a partial one, which is now refused in front of it, so it is back to meaning "the engine has lost count".

### The test seam

`PartialCommitTest.FailingCommitDevice` is an in-memory device with two overrides - its commit and its prepare each fail once when told to - registered into the running engine at run time through the public `Catalog.ClassLoader.Classes` list. No new library and no change to shipped code.

## Corrections to the implement ticket, for the reviewer's benefit

- **The ticket said there was "no way to make a back-end database's commit fail in a test". There already was most of one.** `tests/DAE.Server.Tests/TransactionBeginTest.cs` registers `Test.FailingBeginDevice` through exactly the same `Catalog.ClassLoader.Classes` seam to make a *begin* fail. What is new here is a device that fails a **commit** and a **prepare**, not the technique. The new device's class name is `Test.FailingCommitDevice`, not the ticket's `TestDevice.FailingCommitDevice`, so that it reads as the sibling of the one already in the tree, and both classes name each other in comments.
- **The ticket's phase order was not executable as written.** Two of its three cases assert on error 106213, so they cannot compile before the error code exists. The code and its `.resx` entry were added first, inert - nothing threw it - then the three cases were run against the unfixed engine (see the measurements below), then the engine was changed.
- **The shared predicate takes the participant's kind and name separately**, not one pre-formatted description as the ticket sketched, because `Error.AssertFail` formats only when the assertion fails and this runs once per participant per level ended.
- **The ticket's claim that `==` is the shape `debt-remote-session-lazy-enlistment` will need is still not right**, as the sibling ticket's review already noted; `HoldsEndingLevel`'s remarks say which direction will break it.

## What was measured

All against the in-process engine on 2026-11-23, two transaction levels with rows inserted at both into a working in-memory device and into the failing device.

**Before the engine change** (the three cases run against the unfixed engine; this was the proof the seam reaches the defect):

| Case | What happened |
|---|---|
| rollback after the failed commit | failed at the depth assertion: the working device was at **0** levels where it should be at 1 |
| retried commit | failed with "Expected: instance of `System.Exception`, But was: null" - the retry raised nothing at all |
| nested begin after the failed commit | the same: nothing raised |

**After**: all four cases pass, and the three fixtures nearest this change (`ConnectionTransactionLevelTest`, `TransactionBeginTest`, `DeferredDeviceStopTest`) and `ApplicationTransactionTest` pass unchanged.

**The flag's placement was proved load-bearing, not argued.** Moving `CommitBegun = true` to before the prepare makes every one of the fixture's cases fail in setup with error 106213: `InternalCommitTransaction` calls `InternalPrepareTransaction` itself, so the flag set first is seen by that very call and every ordinary commit is refused. The engine is unusable that way. The move was reverted and the file verified byte-for-byte back.

## Tests added, and what each verifies

All four are in the new fixture `tests/DAE.Server.Tests/PartialCommitTest.cs` (added to `DAE.Server.Tests.csproj`'s explicit `<Compile>` list in the same edit, since a file missing from that list compiles into nothing and the run reports no error).

| Case | What it pins |
|---|---|
| `TestRollbackAfterAFailedDeviceCommitKeepsTheOuterLevel` | The reproduction of the first defect: the depths after the failed commit (process 2, working device 1, failing device 2) and after the rollback (1, 1, 1), then that the outer commit succeeds and both rows are in the working device's table, including the one inserted at the outer level. |
| `TestARetriedCommitAfterAFailedOneIsRefused` | The reproduction of the second: the retry fails with error 106213 and the three depths are unchanged by the refused call. |
| `TestANestedBeginAfterAFailedCommitIsRefused` | The third face: same error, same unchanged depths. This is what keeps the predicate sound, because a begin on top of a half-committed level makes the level counts stop saying which level a participant holds. |
| `TestACommitThatFailsInItsPrepareStaysRetryable` | The boundary the design rests on: a commit that fails in its prepare phase has committed nothing and is still retryable. A deferred constraint violation is the ordinary way a commit fails, so this is the behaviour that must not change. |

No test was added for anything a type or the compiler already enforces, and the remote-session sibling case stays where it is in `ConnectionTransactionLevelTest`.

## How to exercise it by hand

The cheap way is the fixture itself: `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~PartialCommitTest" --logger "console;verbosity=normal"`.

Against real databases - which this suite has not got, and which is the only way to see the behaviour the way an application does - the shape is: a session with a transaction open on two SQL databases, work done in both at an outer level and at an inner one, the inner commit arranged to fail on the second database (stop it, or cut the connection, between the two commits). Before: the first database's outer transaction was gone and the session's commit of it failed with "No active transaction." After: the first database still holds the outer transaction, the session can commit or roll it back, and a second `CommitTransaction` or a nested `BeginTransaction` says 106213 instead of appearing to work.

## Known gaps, honestly

- **The catalog arm is fixed but not reproduced.** The catalog device session commits last, so reaching the window needs `ServerTransactions.EndTransaction(true)` to throw *after* it - which it can, because it calls `ServerTableVar.DeleteCheckTableChecks`, which opens a cursor and deletes rows in a device - and nothing in the suite makes it do so. The fix is there because it is the same question at the same site, read from the code rather than run. A reviewer who wants it covered would need a seam in the check-table deletion, which is a wider change than this ticket.
- **No case has both a remote session and a device session in one transaction.** The implement ticket records that with a connection to another Dataphor server in the transaction the old retry reached `Error.AssertFail` rather than silently committing; that path is now refused before the assertion, but nothing runs it. Both kinds are covered separately (here and in `ConnectionTransactionLevelTest`) and such a case would exercise the same two expressions once more.
- **The `<=` assertion in the predicate is never seen to fire**, by design: it is an internal-error guard for a state no audited path produces. The audit behind it was re-run rather than trusted - `grep -rn "\.BeginTransaction(" --include=*.cs src libraries` finds participant begins at exactly four sites, all in `ServerProcess.cs` (the two catch-up loops, and the remote and device loops of `InternalBeginTransaction`), and both `Schema.DeviceSession.RollbackTransaction` and `RemoteSession.RollbackTransaction` end their level in a `finally`.
- **The two audits the ticket asked to re-check, re-checked.** Implicit transactions: `BeginTransactionalCall` begins one only when the process holds none, so it never nests inside a client's transaction, and `EndTransactionalCall` rolls back rather than retrying when an implicit commit fails - the predicate is what makes *that* rollback correct too. Deferred device stops and remote session closes, which run inside the catalog's commit while `CommitBegun` is set: no `InternalStop` override in the tree begins a process transaction (the four are the memory, catalog, Frontend and Simple devices, and `grep -n "BeginTransaction" ` over those four files finds only the memory device's own session methods).
- **A statement is still allowed after a failed commit.** It runs inside the half-committed level and begins no new one, because `BeginTransactionalCall` begins an implicit transaction only at depth 0. That is unchanged behaviour and deliberately out of this ticket's two arms; whether such a statement should also be refused is a question nobody has asked yet.
- **`tools/docs-link-check/check_links.py` reports 203 errors** across `docs/developers-guide/` and `docs/users-guide/`, all missing `docs/reference/system-library/...` targets left by the AsciiDoc conversion. Pre-existing and untouched by this change: neither of the two files edited here appears in its output, and this change adds no links. It is a run-by-hand tool, not part of the suite.

## Tripwires parked

- **The half-applied level itself.** A failed commit still leaves the level it failed on applied on the participants that committed it, and nothing here changes that - there is no protocol to undo a participant that has said yes and the engine logs no two-phase recovery record. Parked as a `NOTE:` in `ServerTransaction.CommitBegun`'s remarks, at the record's own site, saying what the fix does change (the damage stops at that level) and that making a distributed commit recoverable is a much larger piece of work. Not filed, per the implement ticket's instruction.
- **The predicate's dependence on full enlistment.** Comparing a participant's level count against the process's depth is sound only while every participant is enlisted at every level. `debt-remote-session-lazy-enlistment` (in `plan/`) would make a connection to another Dataphor server hold fewer levels by design. The `NOTE:` saying so moved from the old `HoldsEndingLevel(RemoteSession)` onto the shared body with the rest of the remarks, so there is now one place for that plan ticket to change instead of three guards.

## Build and test state

`dotnet build Dataphor.slnx`: 0 errors, no new warnings. No `WFDEV` or `SYSLIB` suppression was added.

The suite was run in the three parts from the root `AGENTS.md`, each as its own foreground command, after the final engine state:

| Part | Result |
|---|---|
| 1 (everything but the coverage scripts) | `Common.Tests` 18 passed; `DAE.Server.Tests` 687 passed, 33 skipped, 0 failed, 8 m 23 s |
| 2 (coverage scripts A-R) | 67 passed, 11 skipped, 0 failed, 3 m 9 s |
| 3 (coverage scripts, the rest) | 50 passed, 12 skipped, 0 failed, 3 m 4 s |

Parts 2 and 3 were run against the engine in its final state; part 1 was run again after the fourth test case was added, which is where its 687 comes from (686 before it). No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written.

## Documentation

- `docs/migration-notes.md`, "D4, catalog and libraries": one entry covering both halves - the outer transaction's work is no longer discarded, and after such a failure a retried `CommitTransaction` or a nested `BeginTransaction` fails with 106213 where the first used to appear to succeed (or to report an internal error) and the second used to appear to succeed.
- `docs/modernization-plan.md`, known defects: a new row **K40** in the shape of its neighbours, with the measurements above, and saying in it that the catalog arm is static only and why, and that the device seam is new and reusable.
- No entry in `docs/reference/error-codes.md`: it lists prefix codes only, not individual codes.
