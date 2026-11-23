description: When a session opens its first connection to a back-end database or to another Dataphor server while it already has several transactions open, every transaction it opens on that connection but the innermost is given the wrong isolation level, so a query can see uncommitted data it should not see or miss committed data it should. Separately, after a commit to another Dataphor server has failed halfway through, the clean-up throws away work on that server that the session has not finished with.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, libraries/SQLDevice/Source/SQLDevice.cs, src/engine/DAE/Schema/Device.cs, src/engine/DAE/Server/RemoteSession.cs, tests/DAE.Server.Tests/TransactionBeginTest.cs, docs/migration-notes.md
repro: verified
----

## One mistake, three sites

A session's process holds a connection per back-end database it has used (a *device session*) and a connection per other Dataphor server it has queried (a *remote session*). Both kinds are opened lazily, on first use, and both have to be brought up to the process's transaction depth when that first use happens inside transactions that are already open. Three places address a *level* on such a connection by the **process's** depth instead of by **that connection's own** depth, and all three are in `src/engine/DAE/Server/ServerProcess.cs`. The correct shape already exists one directory away, in `ServerCatalogDeviceSession.BeginStoreTransactions` (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs:171`), which indexes by the connection's own count:

```
while (_catalogStoreConnection.TransactionCount < openLevels)
    _catalogStoreConnection.BeginTransaction(ServerProcess.Transactions[_catalogStoreConnection.TransactionCount].IsolationLevel);
```

The original filing of this ticket found two of the three sites and called the remote one "the only place in the tree that opens more than one transaction on a connection at once". That was wrong: the device-session site below does the same thing, is reachable without any server link, and is the one with real consequences on a shipped device. A sweep of the tree for catch-up loops of this shape (`grep -rn "while (.*TransactionCount\|while (.*Transactions.Count" --include=*.cs src libraries`) finds exactly these three loops and the correct one quoted above; there is no fourth.

## Site 1 - device sessions get every level at the innermost isolation level

`ServerProcess.DeviceConnect`, around line 546:

```
while (session.Transactions.Count < _transactions.Count)
    session.BeginTransaction(_transactions[_transactions.Count - 1].IsolationLevel);
```

`_transactions[_transactions.Count - 1]` is the innermost open transaction, and the same one is passed for every level the loop opens. The levels being opened are the outer ones, each of which has its own isolation level - a client picks one per level through `IServerProcess.BeginTransaction(IsolationLevel)`, and D4 has `BeginTransaction('Isolated')` (declared in `src/engine/DAE/Schema/SystemCatalog.d4:782` and granted to role `User`) - so when two open levels differ, every level opened on the connection but the last gets the wrong one.

On a SQL device this is not a cosmetic mismatch, because it is the **outermost** level's isolation level that becomes the back-end transaction's. `SQLDeviceSession.InternalBeginTransaction` (`libraries/SQLDevice/Source/SQLDevice.cs:3786`) records the isolation level only when `Transactions.Count == 1`, and `AddConnection` then begins the real DBMS transaction with that recorded value, mapped by `IsolationLevelToSQLIsolationLevel` - `Browse` to read-uncommitted, `CursorStability` to read-committed, `Isolated` to serializable. So the single value the bug gets wrong is exactly the value that reaches SQL Server or PostgreSQL. A session that began its outer transaction at `Isolated` and an inner one at `Browse`, and then touched a table in a SQL device for the first time, runs its whole back-end transaction at read-uncommitted: it can read rows another session has written and not committed, and the serializability the outer level asked for is simply not there. The other direction - outer `Browse`, inner `Isolated` - costs needless serializable locking on the back end instead.

**How reachable.** Three things have to hold, and none of them needs a server link:

- the session has more than one transaction open at once, and
- at least two of those levels were begun at different isolation levels, and
- a device session is connected for the **first time on that process** while they are open.

The third is ordinary: device sessions are connected on demand, so the first statement in the session that touches a given device connects it. A device session already connected is kept up to date one level at a time by `ServerProcess.InternalBeginTransaction`, which passes each level its own isolation level correctly, so only the catch-up is affected.

## Site 2 - remote sessions, the same expression

`ServerProcess.RemoteConnect`, around line 623, has the same mistake for a connection to another Dataphor server:

```
while (session.TransactionCount < _transactions.Count)
    session.BeginTransaction(_transactions[_transactions.Count - 1].IsolationLevel);
```

Same conditions as site 1, with a server link used for the first time on the process in place of a device. What it costs is the same thing: a read on the other server inside the outer transaction can see uncommitted data it should not, or miss committed data it should see, depending on which way the two levels differ. The local half of the same transaction is unaffected - this expression reaches only the connection to the other server. A connection already open is kept up to date correctly by `InternalBeginTransaction`, as above.

This site also becomes more load-bearing than it is today: the owner decision recorded in `tickets/plan/debt-remote-session-lazy-enlistment.md` makes `RemoteConnect`'s catch-up the *only* way a connection to another server joins open transactions, which is why that ticket names this one as a prerequisite.

## Site 3 - after a commit that failed partway, the rollback undoes a level the session still has open

`ServerProcess.InternalCommitTransaction` ends the innermost level on every connection to another server, one at a time, and then on the local devices. If one of those remote commits fails - the far server stopped between the commit's prepare and its commit - the loop throws, the levels already ended stay ended, and the process's own transaction is *not* popped. `RemoteSession.CommitTransaction` decrements its count in a `finally` whatever the far server did (deliberately; see its remarks), so that connection is now one level shallower than the process.

The rollback that follows does not notice. `ServerProcess.InternalRollbackTransaction`, around line 1507, rolls back every connection for which `InTransaction` is true - that is, every connection with any level at all open:

```
foreach (RemoteSession remoteSession in RemoteSessions)
    if (remoteSession.InTransaction)
        remoteSession.RollbackTransaction();
```

For the connection that is now one level short, its innermost open level is the *outer* transaction - one the session has not finished with and may well go on to commit. Rolling it back throws away whatever was done on the other server inside that outer transaction, silently, while the local half of the same transaction is still live and can still be committed. The two servers then disagree about work the user believes is in one transaction.

**How reachable.** Two things, and one connection is enough: the session has **more than one** transaction open at once, and a remote commit of the inner level fails. At a single level the state repairs itself - the commit's failure leaves the connection at zero levels, the rollback skips it because `InTransaction` is false, and the process pops to zero as well.

What the user sees today, after `bug-dead-remote-session-blocks-every-statement` landed, is an internal error on the next commit: that ticket added an assertion in `ServerProcess.InternalPrepareTransaction` that each connection's level count matches the process's, and this is the one way a correct engine can reach it. That assertion is a better report than the "no transaction in progress" error the far server used to give, but neither is the defect; the defect is the level that was rolled back.

### The device branch of the same loop is already right - leave it alone

The loop immediately below rolls back device sessions on `deviceSession.InTransaction`, and that is correct, because `Schema.DeviceSession.CommitTransaction` (`src/engine/DAE/Schema/Device.cs:1699`) calls `_transactions.EndTransaction(true)` *after* `InternalCommitTransaction` returns. A device session whose commit throws therefore keeps its level, and the rollback that follows rolls back the level the process is actually ending. The two kinds of connection differ here only because `RemoteSession.CommitTransaction` decrements in a `finally` and `DeviceSession.CommitTransaction` does not. Do not change the device branch.

## The fixes

Three expressions, one per site, each the level being begun or ended rather than the process's innermost:

| Site | Now | Should be |
|---|---|---|
| `DeviceConnect`, ~546 | `_transactions[_transactions.Count - 1].IsolationLevel` | `_transactions[session.Transactions.Count].IsolationLevel` |
| `RemoteConnect`, ~623 | `_transactions[_transactions.Count - 1].IsolationLevel` | `_transactions[session.TransactionCount].IsolationLevel` |
| `InternalRollbackTransaction`, ~1507 | `if (remoteSession.InTransaction)` | `if (remoteSession.TransactionCount == _transactions.Count)` |

The two index expressions differ only because the two session types count their levels under different names: `Schema.DeviceSession` has `Transactions.Count` and no `TransactionCount`, `RemoteSession` has `TransactionCount`. Each matches the loop condition directly above it, which is the point - the loop is already asking the connection how deep it is.

The guard in site 3 is `==` rather than `>=` because a connection can never be *deeper* than the process: that is the invariant recorded on `RemoteSession.TransactionCount`, and `InternalPrepareTransaction` asserts it. In the ordinary case, where every connection is at the process's depth, the guard means exactly what `InTransaction` means today. It is also the shape `debt-remote-session-lazy-enlistment` will need, since that decision replaces the whole-process invariant with a per-connection one.

**The guard restores the invariant rather than deferring the problem**, which was the one question the earlier filing left open. Take a process at depth N with a connection whose commit of level N failed, so that connection is at N-1 and the others are still at N. `_transactions.Count` is still N while the rollback loop runs (`EndTransaction` is in the `finally`), so the connections at N are rolled back to N-1 and the failed one is skipped. The process then pops to N-1 and every connection agrees with it again, so a later commit of the outer level passes the assertion and commits the outer level on each connection. That also answers the second open question from the earlier filing - whether `InternalCommitTransaction` should carry on to the remaining connections after one fails. It should not need to: throwing out of the loop leaves the untouched connections at the level the rollback then ends on them, which is exactly right. No change there.

## Reproduction - verified

All three were reproduced on this branch, and each fix was confirmed to turn its case green, with one throwaway NUnit fixture and no change to any production file but the three expressions above. The fixture was then removed, so nothing of it is in the tree; what it needed to work is below, because it is the part that took the time to find.

The device site needs nothing special: `Schema.DeviceSession.Transactions` and `DeviceTransaction.IsolationLevel` are both public, and so is `ServerProcess.DeviceConnect`, which returns the existing session when the process already holds one. So the case begins two levels at different isolation levels through the CLI (which is how the Windows client, the Frontend and the ADO.NET provider drive transactions), runs an ordinary D4 insert to make the device connect, and then reads the levels back:

```csharp
Instance.ExecuteScript("create device Arm3Device class \"System.MemoryDevice\"; create table Arm3Table in Arm3Device { ID : Integer, key { ID } };");
Schema.Device device = (Schema.Device)Instance.Server.Catalog[Instance.Server.Catalog.IndexOf("Arm3Device")];

Process.BeginTransaction(IsolationLevel.Isolated);
Process.BeginTransaction(IsolationLevel.Browse);
ExecuteScript("insert row { 1 ID } into Arm3Table;");
Schema.DeviceSession session = ((ServerProcess)Process).DeviceConnect(device);

int count = session.Transactions.Count;
IsolationLevel outer = session.Transactions[0].IsolationLevel;
Process.RollbackTransaction();
Process.RollbackTransaction();

Assert.That(count, Is.EqualTo(2));
Assert.That(outer, Is.EqualTo(IsolationLevel.Isolated));   // before the fix: Browse
```

Observed before the fix: both of the device session's two levels report `Browse`. After it: `Isolated` then `Browse`. The memory device is enough to show it, because the defect is in the engine's catch-up loop and not in any device; what a SQL device adds is the consequence described under site 1, and reaching that would mean a real SQL Server or PostgreSQL in the test, which the suite does not have.

The two remote cases need a `RemoteSession` the test controls. `RemoteSession` is a public abstract class whose four transaction operations are `protected abstract`, so a test assembly can subclass it; the only obstacle is that the engine builds the class named by `ServerProcess.RemoteSessionClassName`, which is an `internal` field. Reflection reaches it, which is also what `ServiceUtilityTest` does for the same reason and says so in a comment: widening the field to make the test reachable would be worse than the reflection. No child server and no link user are needed, because the fake never connects - a link naming a host that does not answer is enough, as `ServerLinkUserRollbackTest` already relies on:

```csharp
// in the fixture
typeof(ServerProcess)
    .GetField("RemoteSessionClassName", BindingFlags.NonPublic | BindingFlags.Instance)
    .SetValue((ServerProcess)Process, typeof(RecordingRemoteSession).AssemblyQualifiedName);

// a public nested class, constructed by Activator.CreateInstance(type, process, serverLink)
public class RecordingRemoteSession : RemoteSession
{
    public static List<string> Log;
    public static bool FailNextCommit;

    public RecordingRemoteSession(ServerProcess process, Schema.ServerLink serverLink) : base(process, serverLink) {}

    protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
    {
        Log.Add(String.Format("begin {0} at level {1}", isolationLevel, TransactionCount + 1));
    }

    protected override void InternalPrepareTransaction() { Log.Add(String.Format("prepare level {0}", TransactionCount)); }

    protected override void InternalCommitTransaction()
    {
        Log.Add(String.Format("commit level {0}", TransactionCount));
        if (FailNextCommit) { FailNextCommit = false; throw new InvalidOperationException("the far server has gone"); }
    }

    protected override void InternalRollbackTransaction() { Log.Add(String.Format("rollback level {0}", TransactionCount)); }

    public override void Execute(string statement, DataParams paramsValue) { Log.Add("execute"); }
    public override IDataValue Evaluate(string expression, DataParams paramsValue) { throw new NotSupportedException(); }
    public override Schema.TableVar PrepareTableVar(Plan plan, string expression, DataParams paramsValue) { throw new NotSupportedException(); }
}
```

Site 2's case creates such a link, begins `Isolated` then `Browse`, and runs `ExecuteOn('<link>', 'select 1;')` to make the connection happen. Observed before the fix: `begin Browse at level 1 | begin Browse at level 2`. After it: `begin Isolated at level 1 | begin Browse at level 2`.

Site 3's case creates the link, begins one level, runs `ExecuteOn` so the connection exists at that level, begins a second level, sets `FailNextCommit` and calls `Process.CommitTransaction()`, which fails; the process is then at two levels and the connection at one. The case then calls `Process.RollbackTransaction()` and asserts that the connection was not rolled back. Observed before the fix: the log ends `commit level 2 | rollback level 1`, and the process drops from two levels to one with the connection at zero. After it: the log ends at `commit level 2` and the process and the connection are both at one level.

## TODO

- Fix the three expressions in `src/engine/DAE/Server/ServerProcess.cs` as the table above gives them. Do not touch the device branch of the rollback loop, for the reason under site 3. `ServerProcess.cs` is tabs with CRLF and a byte-order mark; keep all three.

- Add one NUnit fixture under `tests/DAE.Server.Tests/` with the three cases above - a suggested name is `ConnectionTransactionLevelTest.cs`, for what transaction levels a connection opened mid-transaction is given. Derive it from `InProcessTestFixture` (which gives a fresh session and process per case, so each case's connections are its own) and add it to the explicit `<Compile Include>` list in `DAE.Server.Tests.csproj`, which has no default items. Write the fixture's own remarks in the house style of `TransactionBeginTest.cs`, and say in a comment at the reflection why the field is reached that way rather than widened, as `ServiceUtilityTest` does.

- Add one entry to the `## D4, catalog and libraries` table of `docs/migration-notes.md`: a transaction now reaches a back-end database or another Dataphor server at the isolation level it was begun at, where before every level opened during the catch-up got the innermost level's. Say plainly what an application can notice - a query inside an outer `Isolated` transaction over a SQL device no longer runs the back-end transaction at read-uncommitted, so it may now block or deadlock where it used to read through, and conversely an outer `Browse` transaction no longer takes serializable locks it never asked for. The site 3 fix needs no application-facing note of its own beyond saying that a failed commit to another Dataphor server no longer rolls back the outer transaction's work on that server; fold it into the same entry or give it its own, whichever reads better next to the existing entries.

- Build, then run the new fixture (`dotnet test tests/DAE.Server.Tests --filter Name=<the new fixture's cases>`) plus `TransactionBeginTest`, `ServerLinkConnectionTest`, `ServerLinkUserRollbackTest` and `ApplicationTransactionTest` as the neighbours most likely to notice a change in transaction level handling, and then the three part commands from the root `AGENTS.md` - never `dotnet test Dataphor.slnx` as one command.
