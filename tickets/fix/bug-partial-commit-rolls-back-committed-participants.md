description: When a session commits a transaction that spans more than one database and the commit succeeds on some of them but then fails on another, the clean-up that follows throws away work on the ones that already committed - including work from an outer transaction the session has not finished with and may still want to keep.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Schema/Device.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs
repro: static
----

## The vocabulary, for a reader without the context

One Dataphor transaction can span several places at once. The session's *process* holds one connection per back-end database it has used (a **device session**: a SQL Server or PostgreSQL database, Dataphor's in-memory store, and always the catalog - Dataphor's own store of its table, type, operator and user definitions) and one per other Dataphor server it has queried (a **remote session**). Each of those is a **participant**: every transaction level the session begins is begun on every participant, and ending the level ends it on every participant.

Transactions nest. A session can have an outer level and an inner level open at once, commit or roll back the inner one, and carry on with the outer one.

The commit is sequential: the process asks each participant in turn to commit the level. There is no protocol that can undo a participant that has already said yes, so if the fourth participant's commit fails, the first three have already committed that level and the fourth has not.

## What goes wrong

After such a partial commit the process is in a state nothing in the code models explicitly: some participants have ended the level and some have not, while the process itself still has it open. All the clean-up has to go on is a count of how many levels each participant holds.

The rollback that follows asks each participant "are you in a transaction at all?" and ends a level on every one that says yes. For a participant that already committed, the level it still holds is the **outer** one - and that is the level that gets ended. The outer transaction's work on that database is discarded, silently, while the same transaction's work everywhere else stays live and committable. The session can then commit the outer transaction and get a result that is half of what it asked for.

`ticket(implement): bug-remote-catchup-uses-innermost-isolation` fixed exactly this for connections to **other Dataphor servers**, by asking the stronger question - does this participant still hold the level being ended? - instead of the weaker one. It left the loop for **back-end databases** asking the weak question, on a reading that covers only one of the two cases: a participant whose own commit *threw* does still hold the level and is still rolled back correctly, but the participants that committed *before* it are the ones that get the outer level ended. The same mistake, at the sibling site.

## Root cause, and the invariant to put in its place

One question, asked three different ways in the same method, `ServerProcess.InternalRollbackTransaction`:

| Loop | Asks today | Right after a partial commit? |
|---|---|---|
| connections to other Dataphor servers | `HoldsEndingLevel(remoteSession)` - the participant's level count equals the process's depth | yes, since the fix above |
| back-end databases | `deviceSession.InTransaction` - the participant holds any level at all | no |
| the catalog database, rolled back last | `catalogDeviceSession.InTransaction` | no |

A fourth place already asks it correctly, for the catalog's own store connection: `ServerCatalogDeviceSession.StoreHasEndingLevel`, which compares the store connection's count against the session's open levels.

So the fix is not three guards but **one named predicate - "does this participant still hold the level that is ending?" - answered in one place per kind of participant and used at every site that ends a level**, with the comparison it rests on (a participant can never be deeper than the process) asserted rather than assumed. That removes the class rather than the instance: a fifth loop added later asks the predicate and cannot get it wrong, and nothing has to re-derive from a raw count what the question even is.

Two things to settle while doing it, both of which the current code leaves implicit:

- **A participant can never be deeper than the process.** True today for both kinds, and for different reasons: `RemoteSession.RollbackTransaction` counts the level down in a `finally`, and `Schema.DeviceSession.RollbackTransaction` ends its level in a `finally`. Worth asserting where the predicate lives, as `ServerCatalogDeviceSession.CheckStoreTransactionLevels` already does for the store.
- **What a partial commit leaves behind, stated once.** The counts are the only record of it today and the clean-up infers from them. An explicit record of which participants committed would make the question answerable without inference, and would also settle the second arm below. Either shape is acceptable; the planning step should pick one and say why.

## Second arm, same root cause: retrying the commit reports an internal error

`IServerProcess.CommitTransaction` rolls nothing back when it fails, so a client that catches the failure and calls it again is making a legal call. On the second call `ServerProcess.InternalPrepareTransaction` reaches `Error.AssertFail` - the engine's "internal error" assertion - because the connections to other Dataphor servers are now one level shallower than the process. The diagnostic is accurate, and failing loudly beats the silent half-commit the raw counts would otherwise produce, but an assertion about the engine's own consistency is the wrong error for a state the client reached by doing something allowed. It wants a real error telling the client that the transaction can only be rolled back now.

This is an arm of this ticket rather than its own because it resolves at the same place: once a partial commit is recorded explicitly, the retry has something true to answer from.

## How reachable this is

Read from the code, not reproduced - there is no seam today that lets a test make a back-end database's commit fail, which is the first thing the fix stage owes (see *What would confirm it*). What the reading says:

- **At one transaction level only, the weak guard is accidentally right.** A back-end database that commits its only level drops to zero levels held, so "in a transaction at all" is false and it is skipped. Nothing is lost.
- **At two or more levels it is wrong.** A nested commit on a SQL device does no work at the DBMS and effectively cannot fail, but it does count the level down; the catalog database's nested commit *does* write - it releases the store's savepoint for that level - and can fail on a disk error or on the store's write lock timing out (error 130103). The catalog commits last, after every other participant. So: a session with two levels open, work in a memory or SQL device at both, a nested commit whose catalog half fails - and the device's outer level is rolled back under it. A device folds a committed inner level's record of changes into its parent level (`DeviceTransactions.EndTransaction(true)`), so that rollback replays the inverse of **both** levels' work.
- **Likelihood: unusual, not contrived.** It needs a failure during a commit, which is itself the unusual part; given one, nothing further is needed. The worst outcome is a user-visible wrong result - part of a transaction's work gone, with no error naming it.

## What would confirm it

A test needs one participant whose commit of a nested level fails after another's has succeeded. Connections to other Dataphor servers already have a seam for this - `ServerProcess.RemoteSessionClassName`, which `tests/DAE.Server.Tests/ConnectionTransactionLevelTest.cs` uses to stand a recording double in for a connection - and back-end databases have no equivalent: a device is named in D4 by a registered class name, so a test device means a registered class. Finding or making that seam is the first task, and it is reusable - several error paths through the commit and rollback of a transaction spanning more than one database are untestable today for the same reason.

Cheapest shapes to weigh, in increasing cost: drive `ServerProcess` directly with a device session whose commit throws, if the catalog's `ClassLoader` can be given a class at run time from a test; a small test device class registered through the existing test libraries under `tests/libraries/`; or making the catalog store's nested commit fail, which needs no new device but is the hardest failure to provoke on purpose.

## Relation to other tickets

`debt-remote-session-lazy-enlistment` (in `plan/`) will make a connection to another Dataphor server hold *fewer* levels than the process by design, which breaks the comparison the predicate rests on for that one kind of participant - its own planning list already records that the three ending operations must act per connection. Doing this ticket first gives that work one predicate to change instead of three guards; doing it second means the predicate has to be written twice. Neither blocks the other, so this is not a `prereq:` - just the cheaper order.
