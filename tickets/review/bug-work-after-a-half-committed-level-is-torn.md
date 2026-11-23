description: A session is no longer allowed to keep working in a transaction whose commit already failed after part of it had been saved; every statement in it, a read included, is now refused and the session must roll back.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Server/ServerTransaction.cs, src/engine/DAE/Schema/Device.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/PartialCommitTest.cs, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
difficulty: hard
----

## What landed

**The rule.** A transaction level whose commit has already failed after it began asking participants to commit accepts no new work on any participant - only a rollback. One predicate, `ServerProcess.CheckNotHalfCommitted` (`CommitBegun` set on the current transaction and `InRollback` not set), raising the existing error 106213, asked at five seams:

- `Schema.DeviceSession.Execute`, `InsertRow`, `UpdateRow`, `DeleteRow` (`src/engine/DAE/Schema/Device.cs`) - each before the method's own `try`, so the refusal never goes through `WrapException` and so never sets `StopError`.
- `ServerCatalogDeviceSession.AcquireCatalogStoreConnection` when `isUpdatable` is true - the funnel for every catalog-store write, and so for all of DDL.
- `ServerProcess.RemoteConnect` - the funnel for every use of a server link, compile-time path included (the ticket offered the option of exempting it; it is not exempted, because it can only happen inside a statement that would be refused anyway).

**The deferred-handler arm.** `ServerTransaction.InvokeDeferredHandlers` now reads the nested transaction's `CommitBegun` before rolling it back and sets it on the enclosing level if it was set, so a commit retried after a partial commit inside the deferred-handler transaction is refused with 106213 instead of running every handler a second time.

**Two things the ticket did not call for, both explained below:** the set point of `CommitBegun` moved, and the deferred-handler catch now tests depth rather than `InTransaction`.

### Deviation 1 - `CommitBegun` is now set in the commit's catch, not before its participant loops

**This is the one thing a reviewer should look at hardest.** The ticket's predicate (`CommitBegun && !InRollback`) is only correct if the commit's own work on the participants is exempt, and the ticket's reasoning covered the rollback span but not the commit's. The commit does participant work *after* the point where `CommitBegun` used to be set:

- `ServerTransactions.EndTransaction(true)` calls `ServerTableVar.DeleteCheckTableChecks`, which opens a cursor and deletes rows in the temp device - so the check would have refused **every commit that had deferred constraint checks**. (This is the same call K40's text already names as the one that "can throw after the catalog has committed".)
- `CatalogDeviceSession.InternalCommitTransaction` runs `_deferredDeviceStops.Commit(...)` after the store commit, and a deferred device stop can write the catalog store (the existing comment there says a right change made by one autocommits).

Rather than add a second flag (`InCommit`, symmetric with `InRollback`), `InternalCommitTransaction` now wraps its participant phase in `try`/`catch` and sets `CommitBegun` in the catch, on a transaction object captured in a local before the loops. This is behaviour-neutral for the three refusals that read the record - all three are reached only from outside a commit, and the path that succeeds removes the transaction object - and it means the predicate is exactly the two terms the ticket wrote. Capturing the object in a local also fixes a latent trap: if `EndTransaction(true)` throws after `RemoveAt` has already disposed the level (`ServerTransaction.Dispose` can throw out of `UnprepareDeferredHandlers`), the record lands on the object that is gone rather than on the enclosing level, which the commit did reach the end of.

**What to check:** whether there is any *other* reader of `CommitBegun` that wanted it set during the commit's own participant phase. I found none (`InternalBeginTransaction` and `InternalPrepareTransaction` are the only readers besides the new one, and nothing inside the commit reaches either). If you disagree, the alternative is the `InCommit` flag.

### Deviation 2 - the deferred-handler catch tests depth, not `InTransaction`

`InvokeDeferredHandlers`' catch used `if (program.ServerProcess.InTransaction)` to decide whether to roll back the level its own begin added. If the nested commit threw *after* it had already ended that level, `InTransaction` is still true for the enclosing level and the catch rolled **that** back instead. It now captures the depth before its begin and compares. Found while making the new `CommitBegun` read unambiguous (the read assumes the nested level is the current one). `repro: static` - the window needs `ServerTransaction.Dispose` to throw, which no test can force; the fix is two lines and strictly narrower than what it replaces, so it is in this ticket per the project's "bugs met along the way" rule rather than filed.

## Tests - each beside what it verifies

All in `tests/DAE.Server.Tests/PartialCommitTest.cs`, on the existing `FailingCommitDevice` seam. The fixture's `[OneTimeSetUp]` gained `HandlerTable`, `AuditTable`, an `AuditHandlerInsert` operator and an `attach … tags { DAE.IsDeferred = "true" }`; `[SetUp]` clears the two new tables as well.

| Case | What it verifies | Seen to fail before the fix |
|---|---|---|
| `TestAStatementAfterAFailedCommitIsRefused` | A statement issued on a half-committed level is refused with 106213, the depths are unchanged, and after rolling the failed level back and committing the enclosing one the statement's row is on neither device | Yes - `Assert.Catch` got `null`: no exception raised |
| `TestWorkAfterAFailedCommitSwallowedInsideOneStatementIsRefused` | The same when the failed commit and the work after it are inside one D4 statement with the failure swallowed by the script's own `except` - the case that rules out the statement boundary | Yes - the 106213 assertion passed pre-fix (from the implicit commit at the end of the call, as the ticket measured) but `GoodTable where ID = 3` was 1, not 0 |
| `TestDDLAfterAFailedCommitIsRefused` | DDL is refused, through the catalog-store seam. Uses `create operator`, not `create table`: measured, a `create operator` is **allowed** with the catalog arm alone removed, where a `create table` is refused by the device arm regardless - so this is the case that actually pins that seam. Also checks the catalog and the store agree afterwards by creating the operator again | Not run with the fix disabled as a whole; its seam was isolated by the probe above |
| `TestTheD4RollbackIdiomsStillWorkAfterAFailedCommit` | `RollbackTransaction();` and `while TransactionCount() > 0 do RollbackTransaction();` still work on a half-committed level, and the session carries on | Cannot fail before the fix - it guards the *rejected* design. Seen to fail with `CheckNotHalfCommitted()` placed in `BeginTransactionalCall` |
| `TestARetriedCommitAfterAPartialCommitInADeferredHandlerIsRefused` | A commit retried after a partial commit inside the deferred-handler transaction raises 106213 rather than re-running the handlers | Yes - `FindError(…, 106213)` got `null`; pre-fix the retry raised the handler's duplicate-key error, as the ticket measured |

The four cases the ticket asked for are all present; the DDL case is one more, added because the catalog seam otherwise had no coverage at all and the probe showed it is load-bearing rather than redundant.

`TestRollbackAfterAFailedDeviceCommitKeepsTheOuterLevel`, `TestARetriedCommitAfterAFailedOneIsRefused`, `TestANestedBeginAfterAFailedCommitIsRefused` and `TestACommitThatFailsInItsPrepareStaysRetryable` still pass unchanged, as do `TransactionBeginTest` (3 cases) and `ConnectionTransactionLevelTest` (3 cases).

## Validation run

`dotnet build Dataphor.slnx` clean (0 errors; warning count unchanged at 143 for a full rebuild). Then the three suite parts, each in the foreground:

- part 1 (`class != …CoverageScripts`): Common.Tests 18/18; DAE.Server.Tests **692 passed, 0 failed**, 33 skipped, 8 m 7 s
- part 2 (coverage scripts A-R): **67 passed, 0 failed**, 11 skipped, 3 m 6 s
- part 3 (the rest): **50 passed, 0 failed**, 12 skipped, 3 m 5 s

No pre-existing failures surfaced, so `tickets/.pre-existing-error.md` was not written. Parts 2 and 3 ran against engine sources byte-identical to what is in the tree now (verified by diffing `git diff -- src/` against the patch saved before those runs); part 1 was re-run after the DDL case was added.

## Tripwires parked (index only - the analysis is at each site)

- `ServerTransaction.Dispose` - a `NOTE:` saying the transaction is still the current one while it disposes, with `CommitBegun` set and `InRollback` back to false, so anything added on that path that reached a participant would be refused. Nothing there does today.
- `ServerProcess.HoldsEndingLevel`'s existing `NOTE:` about `debt-remote-session-lazy-enlistment` now also says what that landing does to the remote arm of this check, as the ticket asked (one note, not two).

## Honest gaps - where to look

- **The remote arm has no test.** `ServerProcess.RemoteConnect` is a one-line check at a funnel, and standing a second Dataphor server up to exercise it would cost far more than it pins; `ConnectionTransactionLevelTest` has the fixture that could, if a reviewer judges it worth it. Verified by reading the call graph only: `Program.RemoteConnect` and `Plan.RemoteConnect` are its only callers, and `SystemTableValues.cs`'s apparent third caller is inside `#if OnExpression`, which is defined nowhere.
- **No caller inside the engine turned out to need a read on a half-committed level**, which the ticket named as the thing that would be evidence against refusing reads. The two engine paths that do participant work on such a level are the commit's own clean-up and the rollback's, and both are exempt by construction rather than by a read exemption. So the "reads are refused too" choice stands as the ticket made it - but it is the most user-visible part of this change, and the migration note is the only place a client learns of it.
- **The `AcquireCatalogStoreConnection` check is on the writing acquire only.** A catalog *read* on a half-committed level is still allowed through that seam, and is only refused if it also reaches `Schema.DeviceSession.Execute`. That is a slight inconsistency with "reads are refused too": `Count(System.Operators)` is refused (it goes through `Execute`), but a read the catalog session makes internally with `AcquireCatalogStoreConnection(false)` is not. Deliberate - checking the non-updatable acquire would refuse the compiler's own catalog reads during the rollback's plan work - but worth a second opinion on whether the asymmetry should be written down somewhere.
- **The one-statement case's surviving tear.** `TestWorkAfterAFailedCommitSwallowedInsideOneStatementIsRefused` asserts only that *row 3* (the work after the failure) is nowhere. Row 2 is still on the working device and not on the failing one - the half-commit's own tear, which K40 records as unfixable without a two-phase recovery record. If a reviewer wants that asserted explicitly rather than only explained in the comment, it is one more line.
- **`CheckNotHalfCommitted` is `public`.** It has to be: `ServerCatalogDeviceSession` is in a different assembly (`AlphoraDataphorServer`) and this repository has no `InternalsVisibleTo` convention. That is an addition to the public surface, not a break, so no migration-notes entry of its own - the behaviour entry covers what an extension would actually notice, which is that a custom device's `Execute`/`InsertRow`/… can now throw 106213 before reaching the device.

## Docs

- `docs/migration-notes.md` - one new row in "D4, catalog and libraries": work in a half-committed transaction is refused (reads included), and the deferred-handler retry is refused.
- `docs/modernization-plan.md` - K41 added with the measurements; K40's "Still open after this fix" paragraph replaced by a pointer to it.
