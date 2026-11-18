description: The server remembers each user's answer to "may this user use this right"; an answer looked up while a transaction that changes rights was still open used to outlive that transaction until restart. It is now cleared again when the transaction ends, and dropping a role now takes its rights from its members at once.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md, tickets/backlog/bug-right-cache-shared-during-open-transaction.md
repro: verified
----
## The defect

Answers to right checks are cached per user and right name on the shared `Schema.User` object (`ServerCatalogDeviceSession.UserHasRight`, which reads the store and caches inside `lock (Catalog)`). Each rights-changing method cleared the affected answers once, before its store write. Each session reads the store through its own connection (SQLite write-ahead logging: committed state, plus the session's own uncommitted writes), so an answer cached while the change's transaction was open, by another session (committed, old answer) or by the changing session (uncommitted answer, then rolled back), stayed until restart. Same class: a right of an object created in an open transaction checked by name meanwhile (no row in the committed store means granted), a member's answers through a dropped role (nothing cleared them), and an unlocked clear for role membership changes.

## What changed

**One mechanism, in the base class `CatalogDeviceSession`** (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, Security region), because `ClearCatalogObject`, `DeleteRight` and `CacheCatalogObjectOwner` live there and the server session derives from it:

- A per-session record of what the open transaction changed: right names, user IDs (role membership) and an "every user" flag. One record covers the whole transaction stack.
- New protected helpers `RightsChanged(params string[])`, `UserRightsChanged(string)`, `AllRightsChanged()`: under `lock (Catalog)` they clear the answers now (so the changing session sees its own change) and, when the device session is in a transaction, add to the record.
- `ClearChangedRights(bool forget)` replays the record under `lock (Catalog)`. `InternalCommitTransaction` calls it right after `InternalAfterCommitTransaction` (where the server session commits the store), outermost level only, in a `finally`; a failed commit clears but keeps the record for the rollback that follows. `InternalRollbackTransaction` calls it right after `InternalAfterRollbackTransaction` (the store rollback) at every level; only the outermost level forgets the record.
- `ClearCatalogObject`, `CacheCatalogObjectOwner` and `DeleteRight` now go through `RightsChanged`; `ClearUserCachedRightAssignments` now takes `lock (Catalog)` itself.

**`ServerCatalogDeviceSession`**: every method that changes who has a right calls a helper **after** its store write (so no store write lock is ever awaited inside the catalog's lock; the old clear-before-write is gone): `GrantRightToRole`/`ToUser`, `RevokeRightFromRole`/`FromUser`, `RevertRightForRole`/`ForUser`, `SetRightOwner` and `InsertRight` (`RightsChanged`); `InsertUserRole`/`DeleteUserRole` (`UserRightsChanged`); `InternalInsertRole` and `InsertPersistentObject` (the object's `GetRights()`, since a right without a row is granted to everyone); `DeletePersistentObject` (again after the write, closing the autocommit window that the before-write clear in `DeleteCatalogObject` leaves); `DeleteRight` now writes the store before calling the base; `InternalDeleteRole` calls `AllRightsChanged()`. `SetCatalogObjectOwner` is covered through `CacheCatalogObjectOwner`.

Decisions to check:

- **`SetCatalogObjectOwnerInstruction`'s clear on undo is kept.** The undo must restore the cached owner anyway, and the clear comes with `CacheCatalogObjectOwner`; the replay makes it redundant but harmless.
- **Role drop clears every user's answers** rather than reading the role's members first: no extra store read, and role drops are rare.
- **Grant/revoke/revert to a single user now clears that right for every cached user** (it cleared only that user's): costs cache misses only.
- **The create-in-transaction arm is reachable only through a check by right name.** The fix ticket supposed another session could insert into the uncommitted table; it cannot, because name resolution reads the committed store ("Unknown identifier"). The reachable path is a by-name check such as the `UserHasRight` operator (which clients use to decide what to offer). A plain user has no execute right on `UserHasRight`, so the test makes the check from a second Admin session about `RightsUser`.
- No plan-cache time stamp bump: a cached plan re-checks its rights when it is bound to a process.

## Tests (all in `tests/DAE.Server.Tests/CatalogStoreTest.cs`)

Each failed before the fix (verified) and passes after:

- `RevokeRefusedAfterCommitDespiteCheckDuringTransaction` (written in the fix stage): a revoke committed after the user used the right in another session during the transaction is refused afterwards (106157).
- `GrantNotKeptAfterRollbackDespiteCheckDuringTransaction(False/True)` (fix stage): a grant checked inside an outermost or nested transaction that rolls back is not granted afterwards.
- `CreatedTableRefusedAfterCommitDespiteCheckDuringTransaction` (new): a table created in a transaction, whose insert right was checked for `RightsUser` by another session while it was open (answer true), is refused to `RightsUser` after the commit.
- `DroppedRoleRightRefusedToFormerMember` (new): a user who used a right through a role is refused it after `drop role`.
- `RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction` (added in review): a role membership added in a transaction and checked inside it grants nothing after the rollback.

## Validation

- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest|Name=Security|Name=SecurityLibrary"`: 23 passed.
- `dotnet test tests/DAE.Server.Tests`: 189 total, 164 passed, 25 skipped (the coverage manifest's known-failure and fragment entries), 0 failed.
- `dotnet build Dataphor.slnx`: 0 errors; no warnings in the edited files.
- CRLF, tabs and byte-order marks preserved in the three `.cs` files; `docs/migration-notes.md` kept LF (as it was).

## Known gaps

- **While the transaction is open the cache is still shared** (as in 3.1): another session can use a grant the changing session checked and later rolls back, and the changing session can see another session's committed answer instead of its own change. Fixing it needs state shared across sessions in the right check; filed as `tickets/backlog/bug-right-cache-shared-during-open-transaction.md`. The migration note says so.
- A right change made at the outermost level after the replay (inside `CommitDeferredDeviceStops`) would be cleared at once but replayed only at the session's next transaction end. No such change is known; noted here only.
- Not examined: `DeleteUser` in a transaction removes the user from the users cache while another session may re-resolve and re-cache it from the committed store meanwhile. That is about the users cache, not the answers, and is outside this ticket.
- Tripwire: `NOTE:` at the record in `CatalogDeviceSession` on the replay cost (every created or dropped object adds its rights; the end walks every cached user for each) for large library registrations on servers with many signed-in users.

## Docs

`docs/migration-notes.md`: removed the "that gap is not fixed yet" sentence from the `SetObjectOwner`/`SetRightOwner` entry; new *D4, catalog and libraries* entry (rights changes take effect for every session at transaction end, rolled-back changes leave no trace, role drop, remaining in-transaction sharing); new *Custom C# extensions* entry for the three protected helpers and the locked `ClearUserCachedRightAssignments`.

## Review findings

Read the diff of `ticket(implement): bug-right-cache-stale-across-transactions` before the handoff, then the surrounding transaction code in `CatalogDeviceSession`, `ServerCatalogDeviceSession` (store connection, `BeginStoreTransactions`, `InternalAfterCommitTransaction`/`InternalAfterRollbackTransaction`, `UserHasRight`), `DeviceSession` in `src/engine/DAE/Schema/Device.cs`, `ServerProcess` (how the catalog device session joins open levels), `CatalogStore.UserHasRight` and `Schema.User`'s answer cache.

**Correctness: checked, nothing wrong found.**

- Timing of the replay against the store: the store's write transactions follow the device session's transaction stack (`OpenTransactionLevels`), and the catalog device session joins every open process level when it is first used (`ServerProcess.DeviceConnect`), so `InTransaction` in the helpers is true exactly when the store write belongs to a transaction. Commit replays after `InternalAfterCommitTransaction` (store commit) at the outermost level only; nested commits change nothing other sessions can see. Rollback replays after the store rollback at every level, in a `finally`, so a failing store rollback still clears.
- Race with a concurrent check: `UserHasRight` reads the store and caches inside `lock (Catalog)`, and the clears take the same lock after the write or after the commit/rollback, so a concurrent check either finished first (and is cleared) or reads the new state.
- Lock order: the helpers run after `ReleaseCatalogStoreConnection` and never begin a store transaction inside the catalog's lock; the old code already took the catalog's lock while holding the store's write lock, so no new ordering.
- Undo during rollback: undo instructions (`CacheCatalogObjectOwner`, `ClearCatalogObject`) run while the level is still on the stack, so their clears are recorded and replayed after the store rollback.
- Case of right names: the answer cache is case-sensitive and so is the SQLite store's `DAERights` key (default binary collation), so a clear by name matches what the store treats as the same right. (Only the unconverted SQL Server store could differ; not in scope.)
- Every store write that changes the store's answer (right rows, right owners, user and role right assignments, role membership, object owner, object and role create/drop) now calls a helper after the write; `DeleteUser` removes the user from the users cache instead and is noted in the known gaps.

**Tests: one added.** The replay's per-user branch (role membership changes, `_changedRightsUserIDs`) had no test. Added `RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction` to `tests/DAE.Server.Tests/CatalogStoreTest.cs`; with the per-user record disabled it fails (`Expected: False, But was: True`), with the code as committed it passes. The implementer's four tests and the two fix-stage tests each reproduce a distinct arm (revoke after commit, grant after outermost and nested rollback, object created in a transaction, role drop); none restates the implementation or tests a mock, so none was cut.

**Tripwires: one added.** The handoff mentioned only in the ticket that a right change made by a deferred device stop at the outermost commit is cleared at once but replayed only at the session's next transaction end (cost: cache misses only). Parked as a `NOTE:` above `CommitDeferredDeviceStops()` in `CatalogDeviceSession.InternalCommitTransaction`. The implementer's `NOTE:` on the replay cost for large library registrations stands.

**Source hygiene: checked, nothing to fix.** Comments state why (store commit order, lock reasoning), not what. `CatalogDeviceSession.cs` was already large before this change; the change adds one coherent block to its Security region. CRLF, tabs and byte-order marks preserved in the edited files.

**Simplification considered and declined:** the replay's per-right loop in `ClearChangedRights` repeats `ClearCachedRightAssignments(string[])` over a `HashSet`; unifying them would mean changing `Schema.User.ClearCachedRightAssignments` to take `IEnumerable<string>`, a public member that extensions may call, for no behaviour change.

**Docs: checked.** `docs/migration-notes.md` entries are accurate (the removed "not fixed yet" sentence, the new D4 entry including the remaining in-transaction sharing, the new protected helpers). The defect is not listed in `docs/modernization-plan.md` or `docs/architecture.md`, so neither needed a change.

**Follow-up tickets:** none new. The implementer's `tickets/backlog/bug-right-cache-shared-during-open-transaction.md` (sharing while the transaction is open) carries severity, likelihood, tradeoffs and a `static` repro with a stated confirmation; left as filed.

**Validation (review):** `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest|Name=Security|Name=SecurityLibrary"`: 23 passed before the new test. `dotnet test tests/DAE.Server.Tests`: 190 total, 165 passed, 25 skipped (the coverage manifest's known-failure and fragment entries), 0 failed.
