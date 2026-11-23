description: When a session first reached out to a back-end database or to another Dataphor server while it already had several transactions open, those transactions were given the wrong isolation level there, so a query could see data it should not see; and after a commit to another Dataphor server failed halfway through, the clean-up threw away work the session had not finished with. Both are fixed, with three tests that each failed before the fix.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
----

## The vocabulary, for a reader without the context

A session's *process* holds one connection per back-end database it has used (a **device session**) and one per other Dataphor server it has queried (a **remote session**). Both are opened lazily, on first use. If that first use happens while the session already has transactions open, the connection has to be *caught up*: the process opens, one at a time, as many transaction levels on the new connection as it has open itself. Each of a session's open levels was begun at an isolation level of its own - a client picks one per level through `IServerProcess.BeginTransaction(IsolationLevel)`, and D4 has `BeginTransaction('Isolated')` - so a catch-up loop has to pass each level the isolation level *that* level was begun at.

Three places in `src/engine/DAE/Server/ServerProcess.cs` addressed a level by the **process's** depth instead of by **that connection's own** depth.

## What landed

Three expressions, in `ticket(implement): bug-remote-catchup-uses-innermost-isolation`:

| Site | Was | Now |
|---|---|---|
| `DeviceConnect` | `_transactions[_transactions.Count - 1].IsolationLevel` | `_transactions[session.Transactions.Count].IsolationLevel` |
| `RemoteConnect` | `_transactions[_transactions.Count - 1].IsolationLevel` | `_transactions[session.TransactionCount].IsolationLevel` |
| `InternalRollbackTransaction` | `if (remoteSession.InTransaction)` | `if (remoteSession.TransactionCount == _transactions.Count)` |

The two index expressions differ only because the two connection types count their levels under different names (`Schema.DeviceSession.Transactions.Count`, `RemoteSession.TransactionCount`); each now matches the loop condition directly above it, which was already asking the connection how deep it was. `ServerCatalogDeviceSession.BeginStoreTransactions` is the one loop of this shape that was already correct, and the two fixes are shaped on it.

The review pass added, on top of that: the third site's guard extracted into a named predicate, `ServerProcess.HoldsEndingLevel`, and the part of its explanation that was wrong corrected; a fixture-level note on the new test fixture; and one ticket filed for the sibling defect the third site's reasoning had missed. Details under *Review findings*.

## Why this mattered, in user terms

On a SQL device the **outermost** level's isolation level is the one that becomes the back-end transaction's: `SQLDeviceSession.InternalBeginTransaction` records the value only when `Transactions.Count == 1`, and the pool's `AddConnection` then begins the real DBMS transaction with it, mapping `Browse` to read-uncommitted, `CursorStability` to read-committed and `Isolated` to serializable. So the single value the bug got wrong was exactly the value that reached SQL Server or PostgreSQL. A session that began an outer transaction at `Isolated`, an inner one at `Browse`, and then first touched a table in a SQL device ran its *whole* back-end transaction at read-uncommitted. The other direction cost needless serializable locking.

For the third site: after a remote commit of the inner level failed, the connection sat one level shallower than the process, and the rollback that followed ended that connection's innermost *open* level - the **outer** transaction, which the session had not finished with. The other server's half of that transaction was discarded silently while the local half stayed live and committable.

## Tests

One fixture, `tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs` (added to the csproj's explicit `Compile` list), on `InProcessTestFixture` so each case gets its own session and process - and so its own connections.

| Test | What it pins |
|---|---|
| `TestDeviceConnectedInTransactionsGetsEachLevelsIsolationLevel` | Two levels begun at `Isolated` then `Browse`, then the process's first statement touching a memory device; the device session's two levels report `Isolated` then `Browse`. Reproduces site 1. |
| `TestRemoteSessionConnectedInTransactionsGetsEachLevelsIsolationLevel` | The same two levels, then the process's first `ExecuteOn` over a server link; the connection's recorded begins read `begin Isolated at level 1`, `begin Browse at level 2`. Reproduces site 2. |
| `TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel` | One level, a connection made inside it, a second level, then a commit whose remote half fails; the rollback that follows leaves the connection's outer level open, and the later rollback of the outer level does reach it. Reproduces site 3. |

Each was confirmed in the implement pass to fail without its own fix, with the three expressions reverted and nothing else changed: `Expected: Isolated / But was: Browse` on the device session's outer level; `"begin Browse at level 1"` where `"begin Isolated at level 1"` was expected; and the connection at 0 levels where 1 was expected after the rollback. The review pass did not re-run the reversion; it re-derived each failure from the reverted expression instead, and all three agree with the recorded messages.

Two things about the fixture worth knowing:

- **It reaches `ServerProcess.RemoteSessionClassName` by reflection** (an `internal` instance field) so the engine builds a test double in place of a real connection to another server. `RemoteSession` is a public abstract class whose four transaction operations are `protected abstract`, so the double itself needs nothing widened; only the field does, and the case asserts the field is still there rather than failing obscurely if it moves. No child server and no link user are needed, because the double never connects - a link naming a host that does not answer is enough, which `ServerLinkUserRollbackTest` already relies on.
- **The double's state is static** (`Log`, `Connection`, `FailNextCommit`), because the engine constructs it and the test has no other handle on it. Reset in `[SetUp]`; the review pass added the fixture-level note the repository's convention asks for, saying why the fixture is not `[Parallelizable]`.

## Validation

`dotnet build Dataphor.slnx` clean (0 errors; 143 warnings, the same count as before the change - neither the new file nor the review's edits produce one), then, in the review pass:

- the new fixture plus the four neighbours most likely to notice a change in transaction handling (`TransactionBeginTest`, `ServerLinkConnectionTest`, `ServerLinkUserRollbackTest`, `ApplicationTransactionTest`): **29 passed, 0 failed**, 56 s
- the three suite parts from the root `AGENTS.md`, each as its own foreground command:
  - part 1 (everything but the coverage scripts): 683 passed in `DAE.Server.Tests` and 18 in `Common.Tests`, 0 failed, 33 skipped, 7 m 59 s
  - part 2 (coverage scripts A-R): 67 passed, 0 failed, 11 skipped, 3 m 3 s
  - part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 5 s

Same counts as the implement pass. No pre-existing failures surfaced in either pass, so nothing was written to `tickets/.pre-existing-error.md`. Part 1 at eight minutes is the part to watch against the runner's ten-minute idle limit.

## Compatibility

No D4 language semantics or operator signatures, registered class names, assembly names, namespaces, file formats, error codes, WCF contract shapes, default ports or on-disk layout changed. Two behaviour changes an application can notice - the isolation level a back-end transaction actually runs at, and a failed remote commit no longer discarding the outer transaction's remote work - are in `docs/migration-notes.md` under *D4, catalog and libraries*.

## Review findings

**Read in this order:** the implement diff with no reference to the handoff; then `ServerTransaction.cs` and both participants' `BeginTransaction`/`CommitTransaction`/`RollbackTransaction`, to settle what each count means at each moment; then `InternalBeginTransaction`, `InternalPrepareTransaction`, `InternalCommitTransaction` and `InternalRollbackTransaction` as one unit, because the three ending operations only make sense against each other; then `SQLDeviceSession` for the consequence the fix claims; then the handoff.

### Major: the same defect at the sibling site, filed

`InternalRollbackTransaction`'s **device** loop has the identical defect the third site fixed, and the implement pass's reasoning for leaving it is incomplete. The handoff argues the device loop "guards on `InTransaction` and is right to", because a device session whose commit *threw* still holds the level being ended. That much is true (`Schema.DeviceSession.CommitTransaction` ends its level only after `InternalCommitTransaction` returns) - but it is not the case that goes wrong. The case that goes wrong is the device sessions that committed **before** the one that failed: each of those is one level shallower than the process, so `InTransaction` is true for the *outer* level, and the rollback ends it. The equality guard the third site adopted would be correct for both.

Reachable, read statically: at a single transaction level the weak guard is accidentally right (a device that commits its only level holds none, so it is skipped), but at two or more levels a device's nested commit counts the level down and the catalog device - which commits last, after every other participant - has a nested commit that really can fail, since it releases the store's savepoint for that level. The device's record of a committed inner level is folded into its parent (`DeviceTransactions.EndTransaction(true)`), so the rollback replays the inverse of both levels' work. Worst outcome: part of a transaction's work silently gone, with no error naming it. Unusual rather than contrived - it needs a failure during a commit and nothing more.

Climbed the ladder rather than filing the instance. The root cause is one question - "does this participant still hold the level that is ending?" - asked four different ways in the engine, three of them by hand from a raw count and one of them (`ServerCatalogDeviceSession.StoreHasEndingLevel`) already correct and already named. The ticket is for **one named predicate per kind of participant, used at every site that ends a level, with the comparison it rests on asserted rather than assumed**: `tickets/fix/bug-partial-commit-rolls-back-committed-participants.md`. It carries a second arm at the same root cause - see below - and the note that no seam exists today to make a back-end database's commit fail, which is why this review could not reproduce it and why the ticket's first task is that seam.

Not fixed inline: the project's rule is that a bug fix owes its reproduction, and the reproduction needs a device-failure seam that does not exist. A one-line guard change with no test, on an error path through the commit of a transaction spanning several databases, is worse than a ticket that builds the seam once and reuses it.

**Second arm, same root cause.** The handoff says `Error.AssertFail` in `InternalPrepareTransaction` was reachable only through the site 3 defect and is now unreachable. It isn't: `IServerProcess.CommitTransaction` rolls nothing back when it fails (it uses `BeginCall`, not `BeginTransactionalCall`), so a client that catches a partial-commit failure and retries the commit is making a legal call, and on the retry the remote connections are one level shallower than the process and the assertion fires. Failing loudly beats the silent half-commit the raw counts would otherwise produce, so this is not a regression - but an "internal error" assertion is the wrong error for a state a client reached by doing something allowed, and it resolves wherever a partial commit stops being inferred from counts. Recorded as the ticket's second arm rather than separately.

### Minor, fixed in this pass

- **The guard's comment asserted something false** - that the device loop is right to guard on `InTransaction`. Rewritten to say that loop is wrong in the same way, and to name the ticket that owns it.
- **Fifteen lines of comment inside a loop body**, for a one-expression guard. Extracted into `private bool HoldsEndingLevel(RemoteSession)` with the explanation as its remarks, named for `ServerCatalogDeviceSession.StoreHasEndingLevel`, which asks the same question of the catalog store's connection. No behaviour change, and it makes the filed ticket's work one step smaller.
- **The new fixture carried no `Not [Parallelizable]` note**, which this repository's convention asks of every fixture whose cases share process-wide state - and this one installs three static fields on its test double. Added, with the reason.

### Checked and clean

- **Both index expressions, re-derived rather than taken on trust.** `_transactions` is outermost-first (`ServerTransactions.BeginTransaction` appends, `CurrentTransaction()` is the last entry and `RootTransaction()` the first), and each participant pushes its own level before the per-participant begin runs - `Schema.DeviceSession.BeginTransaction` adds the level then calls `InternalBeginTransaction` and removes it again if that throws, `RemoteSession.BeginTransaction` increments only after it returns. So at each turn of a catch-up loop the participant's own count is exactly the index of the next level to open. Correct, including when a begin part-way through the catch-up fails.
- **The consequence the device fix claims**, read at the source: `SQLDeviceSession.InternalBeginTransaction` records the isolation level only at `Transactions.Count == 1`, and all three places that begin the DBMS transaction use that recorded value. So the outermost level's isolation level is the one that reaches the DBMS, as the handoff and the migration note say.
- **No fourth catch-up loop, and no other site picking a level by index.** `grep -rn "while (.*TransactionCount <\|while (.*Transactions.Count <" --include=*.cs src libraries` finds exactly the two changed sites and `ServerCatalogDeviceSession.BeginStoreTransactions`; a sweep of every `.IsolationLevel` read in `src` and `libraries` finds no other.
- **The failed-begin path does not reach the new guard.** `InternalBeginTransaction` pushes its level before beginning on participants, and its own unwind rolls back a named list of the participants that began (`RollbackBegunParticipants`) rather than consulting any count, so the counts it leaves behind agree with the process's depth.
- **The equality guard is safe in the other direction.** A participant can never end up deeper than the process after a failed *rollback*, because both `RemoteSession.RollbackTransaction` and `Schema.DeviceSession.RollbackTransaction` end their level in a `finally`. The predicate's remarks now record this.
- **The three tests, as code under review.** Each is the reproduction of one defect at the lowest layer that shows it, so each pays for itself; none restates the implementation or verifies a mock's own wiring. Nothing added and nothing cut. The reflection the fixture needs is asserted rather than assumed, and the `[OneTimeSetUp]` device start is server-wide so no case depends on another's state. NUnit runs the base fixture's `[SetUp]` before the derived one's, so `Process` exists when the class name is installed.
- **Docs read for staleness, not assumed.** `docs/architecture.md` (its process-and-transaction paragraph and the whole store-transactions section), `docs/glossary.md`'s isolation entries and the WS2 rows of `docs/modernization-plan.md` describe neither the catch-up nor any participant-depth invariant, so the `docs/migration-notes.md` entry was the only one owed. That entry is accurate and in the right section.

### Conditional concerns - tripwires

No new ones. The implement pass's one tripwire - that comparing a connection's level count against the process's depth is sound only while every connection is enlisted at every level, which `debt-remote-session-lazy-enlistment` would change - moved with the guard into `HoldsEndingLevel`'s remarks, still tagged `NOTE:` so the set stays greppable. The filed ticket repeats it as the reason for doing the predicate work before that plan ticket rather than after.

### Considered and not filed

- **"The bad state is still representable"** (the handoff's own stated gap: two hand-written index expressions, and a fourth loop could get it wrong again). Weighed and left. There are three such loops and one of them was always correct; the two changed ones now read identically, and the first carries the reason. A single helper for "bring this connection up to this depth" would have to span `Schema.DeviceSession` - a public extension point - and `RemoteSession`, which is a wider refactor than either this bug or the filed ticket needs. The filed ticket covers the *ending* side of the same class, which is the side where the defect actually loses data.
- **A three-or-more-level case, and a case with both kinds of connection in one transaction.** Both exercise the same expressions once more and pin no additional behaviour; agreed with the handoff that neither pays for itself.
- **Case 2's exact-list assertion on the recorded calls.** Brittle by design and documented as such; it is deterministic because a statement inside an open transaction begins no implicit transaction of its own. Left as the implementer wrote it.
- **The handoff's correction of the implement ticket's claim** that `==` "is also the shape `debt-remote-session-lazy-enlistment` will need". Agreed - it is a step toward that shape, not that shape - and `HoldsEndingLevel`'s remarks now say which direction will break.
