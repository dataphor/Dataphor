description: When a session first reached out to a back-end database or to another Dataphor server while it already had several transactions open, those transactions were given the wrong isolation level there, so a query could see data it should not see; and after a commit to another Dataphor server failed halfway through, the clean-up threw away work the session had not finished with. Both are fixed, with three tests that each failed before the fix.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
----

## The vocabulary, for a reader without the context

A session's *process* holds one connection per back-end database it has used (a **device session**) and one per other Dataphor server it has queried (a **remote session**). Both are opened lazily, on first use. If that first use happens while the session already has transactions open, the connection has to be *caught up*: the process opens, one at a time, as many transaction levels on the new connection as it has open itself. Each of a session's open levels was begun at an isolation level of its own — a client picks one per level through `IServerProcess.BeginTransaction(IsolationLevel)`, and D4 has `BeginTransaction('Isolated')` — so a catch-up loop has to pass each level the isolation level *that* level was begun at.

Three places in `src/engine/DAE/Server/ServerProcess.cs` addressed a level by the **process's** depth instead of by **that connection's own** depth. Nothing else in the tree has a loop of this shape except `ServerCatalogDeviceSession.BeginStoreTransactions`, which was already correct and is what the fixes are shaped on.

## What changed — three expressions, and nothing else

| Site | Was | Now |
|---|---|---|
| `DeviceConnect` (~line 551) | `_transactions[_transactions.Count - 1].IsolationLevel` | `_transactions[session.Transactions.Count].IsolationLevel` |
| `RemoteConnect` (~line 630) | `_transactions[_transactions.Count - 1].IsolationLevel` | `_transactions[session.TransactionCount].IsolationLevel` |
| `InternalRollbackTransaction` (~line 1533) | `if (remoteSession.InTransaction)` | `if (remoteSession.TransactionCount == _transactions.Count)` |

The two index expressions differ only because the two session types count their levels under different names (`Schema.DeviceSession.Transactions.Count`, `RemoteSession.TransactionCount`); each now matches the loop condition directly above it, which was already asking the connection how deep it was.

The device branch of the rollback loop was deliberately **not** touched. It guards on `InTransaction` and is right to: `Schema.DeviceSession.CommitTransaction` ends its level only *after* `InternalCommitTransaction` returns, so a device session whose commit threw still holds the level the rollback is ending. `RemoteSession.CommitTransaction` decrements in a `finally` instead, which is the whole reason the two kinds of connection need different guards.

Each site carries a comment saying why the index or the guard is what it is; the diff is 28 added lines for three changed ones, and the rest is those comments.

## Why this mattered, in user terms

On a SQL device the **outermost** level's isolation level is the one that becomes the back-end transaction's: `SQLDeviceSession.InternalBeginTransaction` records the value only when `Transactions.Count == 1`, and `AddConnection` then begins the real DBMS transaction with it, mapping `Browse` to read-uncommitted, `CursorStability` to read-committed and `Isolated` to serializable. So the single value the bug got wrong was exactly the value that reached SQL Server or PostgreSQL. A session that began an outer transaction at `Isolated`, an inner one at `Browse`, and then first touched a table in a SQL device ran its *whole* back-end transaction at read-uncommitted. The other direction cost needless serializable locking.

For site 3: after a remote commit of the inner level failed, the connection sat one level shallower than the process, and the rollback that followed ended that connection's innermost *open* level — the **outer** transaction, which the session had not finished with. The other server's half of that transaction was discarded silently while the local half stayed live and committable.

## Tests added, and what each verifies

One new fixture, `tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs` (added to the csproj's explicit `Compile` list), on `InProcessTestFixture` so each case gets its own session and process — and so its own connections.

| Test | What it pins |
|---|---|
| `TestDeviceConnectedInTransactionsGetsEachLevelsIsolationLevel` | Two levels begun at `Isolated` then `Browse`, then the process's first statement touching a memory device; the device session's two levels report `Isolated` then `Browse`. Reproduces site 1. |
| `TestRemoteSessionConnectedInTransactionsGetsEachLevelsIsolationLevel` | The same two levels, then the process's first `ExecuteOn` over a server link; the connection's recorded begins read `begin Isolated at level 1`, `begin Browse at level 2`. Reproduces site 2. |
| `TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel` | One level, a connection made inside it, a second level, then a commit whose remote half fails; the rollback that follows leaves the connection's outer level open, and the later rollback of the outer level does reach it. Reproduces site 3. |

**Each was confirmed to fail without the fixes, on its own site.** With all three expressions reverted and nothing else changed, the three cases failed as: `Expected: Isolated / But was: Browse` on the device session's outer level; `"begin Browse at level 1"` where `"begin Isolated at level 1"` was expected; and the connection at 0 levels where 1 was expected after the rollback. Attribution is unambiguous — the first case creates no server link, the second's failure is in the recorded begin, the third's is the connection having been rolled back. The fixes were then restored and the fixture re-run green.

### Two things in the fixture a reviewer should know about

- **It reaches `ServerProcess.RemoteSessionClassName` by reflection** (an `internal` instance field) to make the engine build a test double in place of a real connection to another server. `RemoteSession` is a public abstract class whose four transaction operations are `protected abstract`, so the double itself needs nothing widened; only the field does. A comment at the reflection says the field is reached that way rather than widened, as `ServiceUtilityTest` says of the private declarations it reaches. No child server and no link user are needed, because the double never connects — a link naming a host that does not answer is enough, which is what `ServerLinkUserRollbackTest` already relies on.
- **The double's state is static** (`Log`, `Connection`, `FailNextCommit`), because the engine constructs it and the test has no other handle on it. Reset in `[SetUp]`; the cases of a fixture run one at a time and each gets a process, and so a connection, of its own.

## Validation run

`dotnet build Dataphor.slnx` clean (0 errors; 143 warnings, all pre-existing — the new file produces none), then:

- the new fixture plus the four neighbours most likely to notice a change in transaction handling (`TransactionBeginTest`, `ServerLinkConnectionTest`, `ServerLinkUserRollbackTest`, `ApplicationTransactionTest`): **29 passed, 0 failed**, 1 m 5 s
- the three suite parts from the root `AGENTS.md`, each as its own foreground command:
  - part 1 (everything but the coverage scripts): 683 + 18 passed, 0 failed, 33 skipped, 8 m 4 s
  - part 2 (coverage scripts A–R): 67 passed, 0 failed, 11 skipped, 3 m 4 s
  - part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 2 s

No pre-existing failures surfaced, so nothing was written to `tickets/.pre-existing-error.md`.

**One honest caveat about the run order.** After the three parts, one comment block in `InternalRollbackTransaction` was reworded (condensed, with the tripwire line below added). Nothing executable changed; the solution was rebuilt and the new fixture re-run green, but the three parts were not re-run after that edit.

## Tripwire parked

One, at the guard in `InternalRollbackTransaction`, as a `NOTE:` comment: **comparing a connection's level count against the process's depth is sound only while every connection is enlisted at every level.** `tickets/plan/debt-remote-session-lazy-enlistment.md` records an owner decision to enlist a linked server's connection only when a statement uses it, and that ticket's own planning list says the three ending operations "must act per connection, by the levels that connection actually began, not by assuming the process's depth". So when that lands, this guard has to ask which transaction each of a connection's levels was begun *for*, not how many it has. It is fine today — enlistment is still eager everywhere — and it is the plan ticket's work, not a new one.

Worth noting against the implement ticket's prose: it claimed `==` "is also the shape `debt-remote-session-lazy-enlistment` will need". It is a step toward that shape, not that shape; the comparand is still the process's depth. That is the one place where this handoff disagrees with the ticket it came from.

## Where a reviewer should look hardest

- **Does `==` restore the invariant or defer the problem?** The argument: `_transactions.Count` is still N while the rollback loop runs (`EndTransaction` is in the outer `finally`), so connections at N are rolled back to N-1 and the one at N-1 is skipped; the process then pops to N-1 and all of them agree again. `TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel` asserts exactly that, including that the subsequent outer rollback *does* reach the connection. Worth a second reading, because the alternative — carrying on through the remaining connections after one commit fails — was considered and rejected on the same reasoning, and no code change was made there.
- **The device-session fix is only tested through a memory device.** That is the lowest layer that shows the engine's catch-up loop misbehaving, and the loop is device-independent. What a SQL device adds is the *consequence* (the outermost level's isolation level is the one that reaches the DBMS), and showing that would need a real SQL Server or PostgreSQL, which this suite has not got. So the mapping from `Isolated` to serializable is asserted nowhere; it is read from `SQLDevice.IsolationLevelToSQLIsolationLevel` and `SQLDeviceSession.InternalBeginTransaction`, both untouched here.
- **Nothing pins the three-or-more-level case.** Both isolation cases use exactly two levels. A third level would exercise the same index expression once more and was judged not to pay for itself; if a reviewer disagrees, the cheap version is a third `BeginTransaction` and one more assertion in the existing case rather than a new one.
- **The second case asserts the recorded calls as an exact list** (`begin`, `begin`, `execute`). That is tight by design — it would catch an extra or missing begin — but it means any future change that makes `ExecuteOn` touch the connection one more time fails this case rather than site 2's actual behaviour. The reason it is deterministic at all is that a statement run inside an already-open transaction begins no implicit transaction of its own (`BeginTransactionalCall` begins one only when `_transactions.Count == 0`), so there is no third level.
- **`Error.AssertFail` in `InternalPrepareTransaction` is the symptom that is now unreachable.** `bug-dead-remote-session-blocks-every-statement` added that assertion, and the site 3 defect was the one way a correct engine could reach it. Nothing asserts that it is now unreachable; the new case simply never trips it.

## Known gaps, stated plainly

- **The bad state is still representable.** Both index expressions are hand-written indexing into `_transactions` from a catch-up loop, and a fourth such loop added later could get it wrong again in the same way. A type or helper that made "bring this connection up to this depth" a single named operation would remove the class; that is a refactor of a public extension point (`Schema.DeviceSession`) and was out of scope for a bug fix. The three loops and the one correct precedent are all that exist today (`grep -rn "while (.*TransactionCount\|while (.*Transactions.Count" --include=*.cs src libraries`).
- **No test covers a device session and a remote session in the same transaction.** Each case has one kind of connection. The two catch-up loops are independent code paths, so a combined case would pin no additional behaviour, but it is the shape a real deployment has.
- **The migration-notes entry describes a behaviour change an application can notice and cannot easily test for.** A query inside an outer `Isolated` transaction over a SQL device may now block or deadlock where it used to read through uncommitted rows. That is the fix working, but it is the kind of change that surfaces in production rather than in a test, and the entry says so.

## Compatibility

No D4 language semantics or operator signatures, registered class names, assembly names, namespaces, file formats, error codes, WCF contract shapes, default ports or on-disk layout changed. Two behaviour changes an application can notice — the isolation level a back-end transaction actually runs at, and a failed remote commit no longer discarding the outer transaction's remote work — are in `docs/migration-notes.md` under *D4, catalog and libraries*.
