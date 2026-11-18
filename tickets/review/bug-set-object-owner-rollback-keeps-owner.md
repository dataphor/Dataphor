description: Changing the owner of a table, type or other catalog object leaves the running server's security out of step with the saved catalog: a rolled-back change keeps the new owner in memory, and a committed change lets the previous owner keep using the object's rights.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/Server/Runtime.Instructions/SecurityNodes.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md
repro: verified
----
## The defect (both arms reproduced before the fix)

- **Rollback kept the new owner in memory.** `SystemSetObjectOwnerNode.ChangeObjectOwner` assigned `objectValue.Owner` itself and logged nothing for undo, so after a rollback (outermost or nested) the store said `Admin` but `Server.Catalog[...]`'s `Owner` (and that of a scalar type's generated operators) still said the new owner. The in-memory owner decides who may call `SetObjectOwner` on the object again and the security context under which operator bodies, views, table handlers and application-transaction code are compiled and run.
- **The previous owner kept its cached rights after a committed change.** `ServerCatalogDeviceSession.SetCatalogObjectOwner(int, string)` cleared only the new owner's cached right answers; the previous owner's cached "granted" for the object's rights stayed until a restart.

## What changed

- `CatalogDeviceSession` (engine): new protected `CacheCatalogObjectOwner(objectValue, owner)`, which under `lock (Catalog)` sets `Owner` and clears the object's rights (`objectValue.GetRights()`) from every cached user. New `SetCatalogObjectOwnerInstruction` (inside `#if LOGDDLINSTRUCTIONS`, beside the other undo instructions) whose `Undo` calls it with the original owner, so an undo also drops answers cached from the uncommitted owner.
- `ServerCatalogDeviceSession.SetCatalogObjectOwner(Schema.CatalogObject, Schema.User)` replaces the `(int, string)` overload: it writes the store first (a failed write leaves memory unchanged; a partial store write is undone by the transaction's store rollback), then under `lock (Catalog)` logs the instruction when `ServerProcess.InTransaction && !ServerProcess.InLoadingContext()` and calls `CacheCatalogObjectOwner`. The store-then-catalog-lock order keeps to the rule that the store's write lock is not awaited while holding the catalog's lock.
- `ChangeObjectOwner` calls the new method instead of assigning `Owner`; each generated operator of a scalar type gets its own instruction.
- `SetRightOwner`: now clears that one right's answer for every user (was: every answer of the new owner only), which fixes the arm-2 shape for rights not tied to an object. It has no in-memory owner, so no undo instruction; its rollback-cache gap is part of the backlog ticket below.
- The cached-answer clear is narrower than before for the new owner (only the object's rights, not all of that user's answers). This is enough because `CatalogStore.UserHasRight` grants a right to its own owner only; ownership confers nothing else.
- Plan cache: no time stamp bump. Confirmed with a temporary trace in `PlanCache.Get` (removed) that the second, identical insert in `PreviousOwnerLosesObjectRights` came from the plan cache and was refused while being bound to the process (`PlanNode.BindToProcess` re-checks rights through `Plan.CheckRight`). A one-line comment at `SetCatalogObjectOwner` records this.
- `docs/migration-notes.md`: one entry under *D4, catalog and libraries* (the previous owner loses owner-only rights at once; a rolled-back change restores the in-memory owner) and one under *Custom C# extensions* (the replaced public overload and the new protected method).

## Tests (in `tests/DAE.Server.Tests/CatalogStoreTest.cs`)

- `RollbackRestoresCachedOwner(False)` / `(True)`: outermost and nested rollback of `SetObjectOwner` on a table and on a scalar type with a representation; asserts the in-memory owner of the table, the type, its representation selector and its equality operator was `OwnerUser` inside the transaction and is `Admin` after the rollback.
- `PreviousOwnerLosesObjectRights`: arm 2. `OwnerUser` inserts into a table it owns; Admin takes the table back; the same insert text as `OwnerUser` fails with `UnauthorizedRight` (106157). Using identical text also pins the cached-plan re-check.
- New private helper `RunAs(instance, userID, action)` (session + process for a user); `NestedRollbackRestoresEveryRightsOwner` now uses it instead of its inline boilerplate (no change to what it asserts).
- All three new cases were run against the old behaviour (temporarily restored in `SetCatalogObjectOwner`) and failed: the rollback cases with `< "OwnerUser", … >`, the arm-2 case without the access-denied error.

## Validation

- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest"`: 14 passed.
- `dotnet test tests/DAE.Server.Tests`: 148 passed, 25 skipped (the coverage manifest's known-failure, unstable and fragment cases), 0 failed; 6 min 19 s on this machine.
- `dotnet build Dataphor.slnx`: 0 errors.

## Known gaps, for the reviewer

- **Right-answer cache filled during an open transaction** (filed as `backlog/bug-right-cache-stale-across-transactions`): another session's check during the changing transaction reads the committed store and caches the old answer after the clear, and nothing clears it at commit; and grants, revokes, role changes and `SetRightOwner` have no undo for answers cached from their uncommitted state. This ticket fixes only the `SetObjectOwner` undo path of that class.
- Pre-existing TODOs at `SetCatalogObjectOwner` are untouched: operators owned by the object, or depending on it, are not marked for recompile, so an operator body already compiled under the previous owner's security context keeps that compiled plan until it is recompiled.
- The new owner is visible to other sessions in memory before the change commits, like every other in-memory DDL change.
- The removed `(int, string)` overload had no other caller in the repository; an outside extension calling it gets a compile error (migration note added).
