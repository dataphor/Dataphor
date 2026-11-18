description: The server remembers each user's answer to "may this user use this right", and an answer looked up while a transaction that changes rights is still open outlives that transaction, so a user can keep using a revoked right, or keep a right granted in a rolled-back transaction, until the server restarts.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/Security.cs, src/engine/DAE/Device.Catalog/CatalogStore.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md
repro: verified
----
## What happens

`ServerCatalogDeviceSession.UserHasRight` (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs`, about line 2045) caches each user's answer per right name on the shared `Schema.User` object (`CacheRightAssignment` / `FindCachedRightAssignment` in `src/engine/DAE/Schema/Security.cs`), under `lock (Catalog)`, and reads the catalog store only on a miss. Each method that changes who has a right clears the affected answers once, at the moment of the change, before its transaction ends. Two ways an answer then gets cached that the end of the transaction makes wrong:

- **Another session while the transaction is open.** Each session reads the store through its own connection; with SQLite in write-ahead-logging mode that connection sees the last committed state. A check by another session after the clear caches the committed (old) answer, and nothing clears it at commit.
- **The changing session itself, then a rollback.** The changing session's store connection is inside the transaction and sees the uncommitted change; a check there caches the uncommitted answer, and the rollback leaves it. (Only `SetObjectOwner` has an undo that clears answers, `SetCatalogObjectOwnerInstruction`, since `bug-set-object-owner-rollback-keeps-owner`.)

## Reproduction (verified)

Two new tests in `tests/DAE.Server.Tests/CatalogStoreTest.cs` (already written in this fix stage, both fail today; keep them):

- `RevokeRefusedAfterCommitDespiteCheckDuringTransaction`: Admin begins a transaction and runs `RevokeRightFromUser('General.RevokedTInsert', 'RightsUser')`; before Admin commits, `RightsUser` inserts into `RevokedT` in its own session (allowed, from the committed store, and cached); Admin commits; a second insert by `RightsUser` should fail with 106157 (`UnauthorizedRight`). Today it succeeds ("Expected: instance of Exception, But was: null").
- `GrantNotKeptAfterRollbackDespiteCheckDuringTransaction(False)` / `(True)`: in a transaction (outermost, or nested inside an outer one), `GrantRightToUser('General.GrantedTInsert', 'RightsUser')` then `UserHasRight('RightsUser', 'General.GrantedTInsert')` is true; after the rollback the same check should be false. Today it stays true.

Command: `dotnet test tests/DAE.Server.Tests --filter "Name~DespiteCheckDuringTransaction" --logger "console;verbosity=normal"` — 3 failed before the fix.

Note for test writers: a table's rights are named `<Library>.<Table><Right>` (`General.RevokedTInsert`), not `RevokedT.Insert`; `GrantRightToUser` accepts any name without checking it exists.

## Further arms found while researching (static, same class)

- **Creating an object in an open transaction.** `CatalogStore.UserHasRight` (`src/engine/DAE/Device.Catalog/CatalogStore.cs`, about line 1440) answers **true** for a right that has no row in `DAERights` (deliberate, for objects that exist only in memory). The catalog in memory is shared, so a table created in Admin's open transaction is visible to other sessions; another user's check of its rights reads the committed store, finds no right row, caches `true`, and that `true` survives Admin's commit. `InsertRight` clears nothing today. Confirm with: Admin begins a transaction, creates table `X`; another user inserts into `X` (allowed); Admin commits; the user's second insert should be refused.
- **Dropping a role** (not transaction-related, reachable now). `CatalogDeviceSession.DeleteRole` → `ClearCatalogObject(role)` clears only the role object's own rights (`role.GetRights()`), and `InternalDeleteRole` → `CatalogStore.DeleteRole` deletes the role's right assignments and memberships. Members keep the answers they had through the role until a restart. Confirm with: grant a right to a role, add a user, the user uses it, `drop role`, the user's next use should be refused.
- **Unlocked clear.** `ClearUserCachedRightAssignments` (used by `InsertUserRole` / `DeleteUserRole`) sets `_rightsCache = null` without `lock (Catalog)`, while `UserHasRight` does null-check-then-`Add` under the lock; an interleaving can throw `NullReferenceException` in `CacheRightAssignment` or keep an answer read before the change. Fixed for free if every clear goes through one locked helper.
- **Outside any process transaction** (implicit transactions off), the writes autocommit, but the clear happens before the write: a check by another session between the clear and the write caches the old answer for good. Clearing after the write (as well as, or instead of, before) closes it.

## Expected

A right check made after a transaction that changed rights has ended answers from the committed store, whichever session made the check and whenever it first asked. A right check made after a role is dropped does not count that role.

## Design

One mechanism in `ServerCatalogDeviceSession`, so every rights-changing method uses it:

- A per-session record of what the open transaction changed: a set of right names, a set of user IDs whose whole cache must go (role membership), and a flag "every user" (for a role drop, or as a simpler choice for role grants). One record for the whole transaction stack is enough: clearing more than strictly needed only costs cache misses.
- A helper, e.g. `RightsChanged(params string[] rightNames)` / `UserRightsChanged(string userID)` / `AllRightsChanged()`, called by each changing method **after** its store write: under `lock (Catalog)` it clears the answers now (so the changing session sees its own change) and, if `ServerProcess.InTransaction`, adds them to the record. Calling after the store write keeps the rule that the store's write lock is never awaited while holding the catalog's lock (the write has already taken it); `SetCatalogObjectOwner` already orders store-then-lock this way. Callers: `GrantRightToUser`/`ToRole`, `RevokeRightFromUser`/`FromRole`, `RevertRightForUser`/`ForRole`, `InsertUserRole`/`DeleteUserRole`, `SetRightOwner`, `SetCatalogObjectOwner` (via `CacheCatalogObjectOwner`, or record its `GetRights()` beside it), `InsertRight`, `DeleteRight`, dropping an object (`ClearCatalogObject` in `CatalogDeviceSession` — the record lives where both can reach it; a protected hook in the base class that the server session overrides is one way), and role drop (`InternalDeleteRole`: the role's members via `CatalogStoreConnection.SelectRoleUsers` before the delete, or every user).
- At the end of a transaction, replay the record: in `InternalAfterCommitTransaction` **after** `_catalogStoreConnection.CommitTransaction()` when the outermost level commits (a nested commit keeps the record for the outer level), and in `InternalAfterRollbackTransaction` **after** the store rollback at any level (a nested rollback clears but keeps the record, because the outer level's changes are still uncommitted; the outermost rollback clears and empties it). Take `lock (Catalog)` for the replay: `UserHasRight` reads the store and caches inside that lock, so a check either finished before the replay (and is cleared) or reads the store after the commit. At the end of the outermost level this session no longer holds the store's write lock, so taking the catalog lock there is safe. The replay must run even when the store has no level for this transaction (`StoreHasEndingLevel()` false), since a change could in principle be recorded without a store write; place it in a `finally` so a failed store commit or rollback still clears.
- Users that are not in `Device.UsersCache` have no answers to clear; iterate `UsersCache.Values` as `ClearCachedRightAssignments(string[])` already does.
- Leave `SetCatalogObjectOwnerInstruction`'s clear on undo as is (now redundant but harmless), or drop it if the replay covers it — say which in the handoff.
- The engine without a store (`Server.IsEngine`) needs no change in behaviour; the helper still clears.

Plan cache: no time stamp bump needed — a cached plan re-checks its rights as it is bound to a process (`PlanNode.BindToProcess` → `Plan.CheckRight`), per the comment at `SetCatalogObjectOwner`.

## Docs

`docs/migration-notes.md` line 44 (the `SetObjectOwner`/`SetRightOwner` entry) ends with "(A right the previous owner uses in another session while the changing transaction is still open can still stay granted after the commit; that gap is not fixed yet.)" — remove that sentence, and add an entry under *D4, catalog and libraries* saying that grants, revokes, reverts, role membership changes, owner changes and role drops now take effect for every session when the transaction ends (and a rolled-back one leaves no trace), where before an answer looked up during the transaction, or a member's answer through a dropped role, could stay until restart. If any public or protected member that extensions could use changes shape, add a *Custom C# extensions* entry.

## TODO

- Add the record and the locked helpers in `ServerCatalogDeviceSession` (with whatever base-class hook `ClearCatalogObject` and `DeleteRight` need in `CatalogDeviceSession`).
- Route every rights-changing method listed above through the helpers, after its store write; remove the old unlocked `ClearUserCachedRightAssignments` call sites.
- Replay the record after the store commit (outermost only) and after the store rollback (every level), under `lock (Catalog)`, in a `finally`.
- Clear role members' answers on role drop.
- Make `RevokeRefusedAfterCommitDespiteCheckDuringTransaction` and both `GrantNotKeptAfterRollbackDespiteCheckDuringTransaction` cases pass.
- Add reproduction tests for the create-in-open-transaction arm and the role-drop arm (one test each, in `CatalogStoreTest`), confirm each fails before the fix and passes after.
- Update `docs/migration-notes.md` as described.
- Run `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest|Name=Security|Name=SecurityLibrary"` and then the full `dotnet test tests/DAE.Server.Tests`; `dotnet build Dataphor.slnx` with 0 errors.
- Preserve CRLF, tabs and byte-order-mark state in every edited file (`CatalogStoreTest.cs` is UTF-8 with BOM, CRLF; note that Git Bash `sed -i` strips CR on this machine).
