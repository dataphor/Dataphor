description: When a session commits a transaction that spans more than one database and the commit succeeds on some of them but then fails on another, the clean-up that follows throws away work on the ones that already committed - including work from an outer transaction the session has not finished with and may still want to keep. Retrying the failed commit makes it worse: it silently commits that outer transaction on those same databases without being asked.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerTransaction.cs, src/engine/DAE/Server/ServerException.cs, src/engine/DAE/Server/ServerException.resx, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
difficulty: medium
----

## The vocabulary, for a reader without the context

One Dataphor transaction can span several places at once. The session's *process* holds one connection per back-end database it has used (a **device session**: a SQL Server or PostgreSQL database, Dataphor's in-memory store, and always the catalog - Dataphor's own store of its table, type, operator and user definitions) and one per other Dataphor server it has queried (a **remote session**). Each of those is a **participant**: every transaction level the session begins is begun on every participant, and ending the level ends it on every participant.

Transactions nest. A session can have an outer level and an inner level open at once, commit or roll back the inner one, and carry on with the outer one.

The commit is sequential: the process asks each participant in turn to commit the level - remote sessions first, then the back-end databases, the catalog last. There is no protocol that can undo a participant that has already said yes, so if the fourth participant's commit fails, the first three have already committed that level and the fourth has not.

## What goes wrong, measured

Reproduced on 2026-11-23 against the in-process engine, with a test device whose commit can be made to fail (see *The seam*, which was built and run for this). Two transaction levels, rows inserted into a memory device at both levels, and a second device whose commit of the inner level throws:

| | process | memory device | failing device |
|---|---|---|---|
| before the commit | 2 | 2 | 2 |
| after `CommitTransaction()` throws | 2 | **1** | 2 |
| after `RollbackTransaction()` | 1 | **0** | 1 |

The memory device committed the inner level, so it dropped to one level - and that one level is the **outer** transaction. The rollback that follows asks each back-end database "are you in a transaction at all?", which is true, and ends a level on it: the outer one. `Count(GoodTable)` is then `0` where both inserted rows should still be there, one of them inserted at a level the session has not finished with. The session's own `CommitTransaction()` of the outer level then fails with Schema error `No active transaction.` and leaves the process still holding the level, so there is no clean way out either.

`ticket(implement): bug-remote-catchup-uses-innermost-isolation` fixed exactly this for connections to **other Dataphor servers**, by asking the stronger question - does this participant still hold the level being ended? - instead of the weaker one, and named this ticket as the owner of doing the same for the other two kinds of participant.

### Second arm: retrying the failed commit

`IServerProcess.CommitTransaction` rolls nothing back when it fails, so a client that catches the failure and calls it again is making a legal call. Measured on the same run, the retry **raised nothing at all**:

| | process | memory device | failing device |
|---|---|---|---|
| before the retry | 2 | 1 | 2 |
| after the retry | 1 | **0** | 1 |

`Count(GoodTable)` was then `2`: the retry committed the memory device's **outer** level - a level the client never asked to commit and can no longer roll back - while committing the failing device's inner one, and reported success. The process still reports one open transaction. For a process that also holds a connection to another Dataphor server the retry instead reaches `Error.AssertFail` in `ServerProcess.InternalPrepareTransaction`, the engine's "internal error" assertion, which is at least loud but is the wrong error for a state the client reached by doing something allowed.

### Third face, found while designing the fix: a nested begin after a failed commit

A client that calls `BeginTransaction` instead of rolling back is also making a legal call today, and it begins a level on **every** participant, so the shallow participant goes from 1 to 2 while the deep one goes from 2 to 3. The counts then no longer say which level a participant holds at all: at process depth 3 the memory device's second level *is* the process's level 3, but the count says 2, so even the corrected predicate below would skip it and the same silent loss returns one level up. Refusing the begin is part of this fix, not a separate concern - it is what keeps the predicate sound.

## Root cause

One question, asked three different ways in the same method, `ServerProcess.InternalRollbackTransaction`:

| Loop | Asks today | Right after a partial commit? |
|---|---|---|
| connections to other Dataphor servers | `HoldsEndingLevel(remoteSession)` - the participant's level count equals the process's depth | yes, since the sibling ticket |
| back-end databases | `deviceSession.InTransaction` - the participant holds any level at all | no (measured above) |
| the catalog database, rolled back last | `catalogDeviceSession.InTransaction` | no |

A fourth place already asks it correctly, for the catalog's own store connection: `ServerCatalogDeviceSession.StoreHasEndingLevel`, which compares the store connection's count against the session's open levels. That is a different question one layer down (store connection against device session) and stays exactly as it is.

**Is the catalog's guard reachable?** Yes, read from the code rather than reproduced. The catalog commits last, but `ServerTransactions.EndTransaction(true)` runs after it and is not nothing: it calls `ServerTableVar.DeleteCheckTableChecks`, which opens a cursor and deletes rows from a check table in a device, so it can throw. If it does, every participant has ended the level and the process has not, and the rollback that follows ends the outer level on all of them, catalog included. Fix all three sites, not the one that is measured.

## The invariant, and why one predicate

Both kinds of participant are enlisted at every level and only by the process, so **a participant can never be deeper than the process**. Audited: `session.BeginTransaction(...)` is reached from exactly three places, all in `ServerProcess` (`DeviceConnect`'s catch-up, `RemoteConnect`'s catch-up, and `InternalBeginTransaction`), and both kinds count the level down in a `finally` on every path including failures. So "does this participant still hold the level that is ending?" is `participant levels == process depth`, with `participant levels <= process depth` asserted rather than assumed - as `ServerCatalogDeviceSession.CheckStoreTransactionLevels` already does one layer down.

Answering it in **one place, used at every site that ends a level** is the point of the ticket: a fourth loop added later asks the predicate and cannot get it wrong, and nothing has to re-derive from a raw count what the question even is.

## Design to implement

Two changes, both in the engine, plus the test seam.

### 1. One predicate, used at every rollback site

Keep the name `HoldsEndingLevel` and generalise it. One body that takes the participant's level count and a description for the assertion message, and two thin overloads that name the participant:

```csharp
/// <summary>Whether a participant holding the given number of transaction levels still holds the level a rollback is now ending.</summary>
private bool HoldsEndingLevel(int participantLevels, string participantDescription)
{
    // Equality rather than a lower bound because a participant can never be deeper than this process: every level is begun
    // on every participant and only by this process, and both kinds count the level down in a finally on every path.
    Error.AssertFail(participantLevels <= _transactions.Count, "...");
    return participantLevels == _transactions.Count;
}

private bool HoldsEndingLevel(RemoteSession remoteSession) { ... }
private bool HoldsEndingLevel(Schema.DeviceSession deviceSession) { ... }
```

Then `InternalRollbackTransaction` asks it in all three loops: the remote loop as it already does, the device loop in place of `deviceSession.InTransaction`, and the catalog in place of `catalogDeviceSession.InTransaction`. Move the long remarks now on `HoldsEndingLevel(RemoteSession)` onto the shared body, and drop the paragraph there that says the device loop is still wrong and that this ticket owns it.

`RollbackBegunParticipants`, which unwinds a *failed begin*, deliberately does **not** use the predicate: it names exactly the participants that began the level, which is more precise than a count (the begin may also have let a dead connection go, which shifts every later index). Leave it, and say so in a line of comment so the next reader does not "helpfully" convert it.

### 2. One explicit record of a half-committed level

The counts say which participants are shallow; they cannot say whether this process's own level was left half-committed, and that is the one bit the retry needs. Add it to `ServerTransaction`, beside `Prepared` and `InRollback`:

```csharp
/// <summary>Set when the commit of this level has started asking its participants to commit, and never cleared.</summary>
/// <remarks>
/// A level whose commit completes is removed from the stack, so a transaction object that still carries this is one whose
/// commit failed part-way: some participants have committed the level and some have not, and no protocol can undo the ones
/// that have. The level can then only be rolled back - see ServerProcess.InternalPrepareTransaction and InternalBeginTransaction.
/// </remarks>
public bool CommitBegun { get; set; }
```

`InternalCommitTransaction` sets it immediately after `InternalPrepareTransaction()` returns and before the first participant commit. **Not earlier**: a commit that fails inside prepare - a deferred constraint violation, the ordinary reason a commit fails - has committed nothing, and that transaction stays retryable exactly as it is today. Nothing clears the flag; the successful path removes the transaction object.

Then refuse the two operations that a half-committed level cannot survive, both with a new error:

- `InternalPrepareTransaction`, right after `CheckInTransaction()` and **before** the `if (!transaction.Prepared)` test, so a retried commit is refused whether or not the prepare had completed.
- `InternalBeginTransaction`, beside the existing `UseDTC` check, before any transaction state changes.

New code in `ServerException.Codes` - next free is **106213** - plus the matching entry in `ServerException.resx`:

```
/// <summary>Error code 106213: "A previous commit of this transaction failed after part of it had already been committed; the transaction can now only be rolled back."</summary>
TransactionCanOnlyBeRolledBack = 106213,
```

`InternalRollbackTransaction` is unchanged by this arm: a rollback must always be allowed, and with the predicate it now ends the level only on the participants that still hold it.

The existing `Error.AssertFail` on remote session counts in `InternalPrepareTransaction` **stays**. With the refusal above in front of it, the one way a correct engine could reach it is gone, so it goes back to meaning what an assertion should mean: the engine has lost count. Say that in its comment.

Two paths were audited for the new refusal and neither is affected; re-check rather than trusting this, but do not go looking further:

- **Implicit transactions** (on by default). `BeginTransactionalCall` begins an implicit transaction only when the process holds none, so it never nests inside a client's transaction; and `EndTransactionalCall` already rolls back rather than retrying when an implicit commit fails. The predicate is what makes *that* rollback correct too.
- **Deferred device stops and remote session closes**, which run inside the catalog's commit while `CommitBegun` is set. No `InternalStop` override in the tree begins a process transaction (checked: memory, catalog, Frontend and Simple devices are all that override it, and they unhook events, save files or close connections).

### 3. The seam: a device whose commit can be made to fail

There was no way to make a back-end database's commit fail in a test, which is why the sibling ticket could not reproduce this. There is one, and it needs no new library and no change to shipped code: **the class loader's registered-class list is public and can be given a class at run time.** Verified by building and running it.

```csharp
Schema.Catalog catalog = Instance.Server.Catalog;
Schema.LoadedLibrary library = catalog.LoadedLibraries[Engine.SystemLibraryName];
Schema.RegisteredAssembly assembly = new Schema.RegisteredAssembly(typeof(FailingCommitDevice).Assembly, library);
catalog.ClassLoader.Classes.Add(new Schema.RegisteredClass("TestDevice.FailingCommitDevice", library, assembly, typeof(FailingCommitDevice).AssemblyQualifiedName));
```

and then ordinary D4: `create device FailDevice class "TestDevice.FailingCommitDevice";`. The library on a registered class is inert for this - the only reader is `Plan.CheckClassDependency`, which is `[Conditional("CHECKCLASSDEPENDENCY")]` and that symbol is defined nowhere in the tree.

The device itself is a memory device with one override, so it behaves like a working device in every other respect:

```csharp
public class FailingCommitDevice : MemoryDevice
{
    public FailingCommitDevice(int iD, string name) : base(iD, name) { }

    /// <summary>Set to make the next commit of a transaction level on this device throw, as a back-end database that fails mid-commit does.</summary>
    public bool FailNextCommit { get; set; }

    protected override Schema.DeviceSession InternalConnect(ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo)
    {
        return new FailingCommitDeviceSession(this, serverProcess, deviceSessionInfo);
    }
}

public class FailingCommitDeviceSession : MemoryDeviceSession
{
    public FailingCommitDeviceSession(Schema.Device device, ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo)
        : base(device, serverProcess, deviceSessionInfo) { }

    public new FailingCommitDevice Device { get { return (FailingCommitDevice)base.Device; } }

    protected override void InternalCommitTransaction()
    {
        if (Device.FailNextCommit)
        {
            Device.FailNextCommit = false;
            throw new InvalidOperationException("This device's commit of a transaction level fails.");
        }
        base.InternalCommitTransaction();
    }
}
```

The flag is on the device, which is server-wide, so the fixture must not be `[Parallelizable]` - say why, as `ConnectionTransactionLevelTest` does of its own static state. Document the seam on the device class: it is reusable, and several error paths through the commit and rollback of a transaction spanning more than one database are untestable without it.

Ordering matters in the test: the commit loop asks the participants in `DeviceSessions` order, which is the order the process first connected to each, so the statement that touches the working device must run before the one that touches the failing device.

**`tests/DAE.Server.Tests/DAE.Server.Tests.csproj` lists its sources explicitly** (`<Compile Include="..." />`, alphabetical). A new file that is not added there compiles into nothing and the test run reports "discovered 0 of 0" with no error - this cost a build cycle while reproducing, so add the file to the list in the same edit that creates it.

## Tests

A new fixture, `tests/DAE.Server.Tests/PartialCommitTest.cs`, over `InProcessTestFixture`, carrying the device double above and three cases. The remote-session sibling case stays where it is, in `ConnectionTransactionLevelTest`.

- **The rollback after a failed device commit keeps the outer level.** The reproduction of arm 1, the table above: assert the three depths after the failed commit and after the rollback, then that the outer commit succeeds and both rows are there (`Count(GoodTable)` is 2, and the row inserted at the outer level in particular). Seen to fail today at the depth assertion, with the memory device at 0 levels where it should be at 1.
- **A retried commit after a failed one is refused**, with error 106213, and the depths are unchanged by the refused call. Seen today to raise nothing and commit the outer level.
- **A nested begin after a failed commit is refused**, same error, depths unchanged.

Read the counts the way the existing fixture does - `((ServerProcess)Process).DeviceConnect(device)` for a device session, after finding the device in `Instance.Server.Catalog` by its unqualified name. A scalar check of the table contents is enough and needs no cursor: `((ServerProcess)Process).Evaluate("Count(GoodTable)", null).AsNative`, as `ServerLinkUserRollbackTest` does.

## Documentation

- **`docs/migration-notes.md`**, in the same commit: an application can notice both halves. A failed commit of a transaction that spans more than one database no longer discards an outer transaction's work on the databases that had committed; and after such a failure the transaction can only be rolled back - a second `CommitTransaction` or a nested `BeginTransaction` now fails with Dataphor error 106213 where the first used to appear to succeed (or, with a connection to another Dataphor server in the transaction, to report an internal error) and the second used to appear to succeed.
- **`docs/modernization-plan.md`**, the known-defects table: a new row **K40** (K39 is the last), in the shape of the neighbouring rows - what was wrong, the measurements above, what the rule now is, the files, `fixed`, and `WS2 bug-partial-commit-rolls-back-committed-participants`. Note in it that the catalog arm is static only and why, and that the device seam is new and reusable.
- No entry in `docs/reference/error-codes.md`: it lists prefix codes only, not individual codes.

## What this does not do

A failed commit still leaves the level it failed on half-applied on the participants that committed it, and nothing in this ticket changes that - there is no protocol to undo a participant that has already said yes, and the engine logs no two-phase recovery record. What changes is that the damage stops at that level: the *outer* transaction's work survives, and the engine now says so instead of guessing from counts. Put that as a `NOTE:` at the record's site rather than filing it: making a distributed commit recoverable is a different and much larger piece of work than correcting this clean-up.

`debt-remote-session-lazy-enlistment` (in `plan/`) will make a connection to another Dataphor server hold *fewer* levels than the process by design, which breaks the comparison the predicate rests on for that one kind of participant. The `NOTE:` already on `HoldsEndingLevel` saying so must move with the remarks onto the shared body. Landing this first is why that work has one predicate to change instead of three guards.

## TODO

Phase 1 - the seam and the failing reproduction

- Add `tests/DAE.Server.Tests/PartialCommitTest.cs` with the device double, its registration through `Catalog.ClassLoader.Classes`, and the `create device` / `create table` setup for one working memory device and one failing device; add the file to `DAE.Server.Tests.csproj`'s `<Compile>` list.
- Write the three cases. Run them and see the first fail at the depth assertion and the second raise nothing, before changing any engine code - that is the proof the seam reaches the defect.

Phase 2 - one predicate (arm 1)

- Generalise `ServerProcess.HoldsEndingLevel` to the shared body plus the two overloads, with the `<=` assertion, and move the remarks onto it.
- Use it in all three loops of `InternalRollbackTransaction`.
- Add the one-line comment on `RollbackBegunParticipants` saying why it names its participants instead.
- Re-run the first case: it should pass.

Phase 3 - the explicit record (arm 2)

- Add `ServerTransaction.CommitBegun` with its remarks; set it in `InternalCommitTransaction` after the prepare and before the first participant commit.
- Add `ServerException.Codes.TransactionCanOnlyBeRolledBack = 106213` and its `.resx` entry.
- Refuse the retry in `InternalPrepareTransaction` (before the `Prepared` test) and the nested begin in `InternalBeginTransaction`; update the comment on the remote-count `Error.AssertFail` to say the correct-engine path to it is gone.
- Add the `NOTE:` about the half-applied level at the record's site.
- Re-run all three cases.

Phase 4 - docs and the suite

- `docs/migration-notes.md` entry and the `K40` row in `docs/modernization-plan.md`.
- Build, then run the suite in the three parts from the root `AGENTS.md`, each as its own foreground command.
- Preserve each edited file's tabs, CRLF line endings and byte-order-mark state.
