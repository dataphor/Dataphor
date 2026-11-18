description: Changing the owner of a table, type or other catalog object leaves the running server's security out of step with the saved catalog: a rolled-back change keeps the new owner in memory, and a committed change lets the previous owner keep using the object's rights.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Runtime.Instructions/SecurityNodes.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: verified
----
## What happens

Both arms below were reproduced with temporary NUnit tests in `CatalogStoreTest` (run with `dotnet test tests/DAE.Server.Tests --filter "Name~..." --logger "console;verbosity=normal"`, then removed).

**Arm 1 — rollback keeps the new owner in memory.** As `Admin`: create user `OwnerUser`, a scalar type (`create type TempT { representation TempT { V : Integer } }`) and a table; through an `IServerProcess`, begin a transaction, `SetObjectOwner` both to `OwnerUser`, roll back. Tried both as an outermost rollback and as a nested one (begin, begin, change, rollback, commit). In both cases:

- store: `DAECatalogObjects.Owner_User_ID` (and the six `DAERights` rows) is `Admin` — correct;
- memory: `((Schema.CatalogObject)Server.Catalog["TempOwnedT"]).Owner.ID` is `OwnerUser`, and so is the type's, its representation selector's and its equality operator's — wrong.

`drop table` as `OwnerUser` is still refused, because drop checks the stored right, not the in-memory owner. What the in-memory `Owner` does decide: `SystemSetObjectOwnerNode` checks `objectValue.IsOwner(program.Plan.User)` (so `OwnerUser` may reassign the object again), and the security context that operator bodies, views, table defaults/constraints/event handlers and application-transaction code are compiled and run under is `new SecurityContext(<object>.Owner)` (`Compiler.cs`, `ImperativeNodes.cs:1159`, `TableNode.cs`, `TableNodes.cs`, `ApplicationTransaction.cs`). So after the rollback those run with `OwnerUser`'s rights until the object is reloaded or the server restarts.

**Arm 2 — the previous owner keeps its cached rights after a committed change.** As `Admin`: create `OwnerUser` and a table, `SetObjectOwner('TempOwnedT', 'OwnerUser')`. As `OwnerUser`, insert a row (succeeds, as owner). As `Admin`, `SetObjectOwner('TempOwnedT', 'Admin')` — the six stored rights are now owned by `Admin`. As `OwnerUser` again, insert a different row: **it succeeds**. Expected: refused, since `OwnerUser` no longer owns the rights and was granted none.

## Cause

`SystemSetObjectOwnerNode.ChangeObjectOwner` (`src/engine/Server/Runtime.Instructions/SecurityNodes.cs:466`) sets `objectValue.Owner = user` directly (for the object and, for a scalar type, its generated operators: equality, comparison, is-special, special selectors/comparers, representation selectors and property accessors), then calls `ServerCatalogDeviceSession.SetCatalogObjectOwner(int catalogObjectID, string userID)` (`src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs:2273`), which:

- clears the cached right assignments of the **new** owner only (`Device.UsersCache[userID].ClearCachedRightAssignments()`), and
- writes the store.

Nothing logs the old in-memory owner for undo. Other in-memory catalog changes are undone through the catalog device session's undo log: a `DDLInstruction` subclass (nested in `CatalogDeviceSession`, `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, under `#if LOGDDLINSTRUCTIONS`, which is defined at the top of both session files) is added to `_instructions` when `ServerProcess.InTransaction` (and not loading), and `InternalRollbackTransaction` calls each `Undo` back to the transaction's `BeginTransactionInstruction`. See `SetUserNameInstruction` and its use in `ServerCatalogDeviceSession` (around line 1970) for the closest pattern.

The right check (`ServerCatalogDeviceSession.UserHasRight`, line 2043) caches each user's answer per right name on the shared `Schema.User` object and only reads the store on a miss. Rights belong to the owner (`CatalogStore.UserHasRight`: the right's owner has it). Clearing only the new owner's cache leaves the old owner's cached `Granted = true` in place — arm 2. The same applies on undo: during the transaction the session's own store connection sees the uncommitted owner, so a right check made then caches answers that the rollback makes wrong.

`CatalogDeviceSession.ClearCachedRightAssignments(string[] rightNames)` (line 1792) already clears a set of right names for every cached user; `objectValue.GetRights()` gives an object's right names (`ClearCatalogObject` uses exactly this).

`SetRightOwner` (`SystemSetRightOwnerNode`) has no in-memory counterpart: `ServerCatalogDeviceSession.ResolveRight` reads the store each time. It has the same cache shape as arm 2 (it clears only the new owner's cache), but it is not part of this fix unless the implementer finds it is the same few lines; if not, leave a `NOTE:` at `ServerCatalogDeviceSession.SetRightOwner`.

## Fix

Move the in-memory change into the catalog device session so it is logged like the other catalog changes:

- Add `SetCatalogObjectOwnerInstruction(Schema.CatalogObject objectValue, Schema.User originalOwner)` beside the other instructions in `CatalogDeviceSession.cs`. Its `Undo` restores `objectValue.Owner = originalOwner` and clears the cached right assignments of the object's rights for every user (`session.ClearCachedRightAssignments(objectValue.GetRights())` under `lock (session.Catalog)`; the method is `protected`, so either make the instruction call a session method or widen it to `internal`/`protected internal`).
- In `ServerCatalogDeviceSession`, add `SetCatalogObjectOwner(Schema.CatalogObject objectValue, Schema.User user)` that, under `lock (Catalog)`, records the old owner, sets `objectValue.Owner = user`, logs the instruction when `ServerProcess.InTransaction` and not loading, and clears `objectValue.GetRights()` from every user's cache (replacing the new-owner-only clear — this fixes arm 2); then writes the store as the existing `(int, string)` overload does. Keep or fold the `(int, string)` overload; nothing outside `SecurityNodes.cs` calls it.
- `ChangeObjectOwner` calls the new method instead of assigning `Owner` itself. Each generated operator gets its own instruction, so undo restores each one.

Mind the order relative to the store write: if the store write throws, the in-memory owner must not stay changed (either write the store first, or log the instruction before the store write so the transaction's rollback undoes it).

Plan caching: a statement compiled and cached before the owner change is not re-checked by this fix (`SetObjectOwner` does not bump the plan cache time stamp; the arm-2 repro used a different statement text). If the implementer confirms that an identical cached statement still runs for the previous owner, decide whether `Catalog.UpdatePlanCacheTimeStamp()` belongs in the forward path (grant and revoke would share the question); otherwise leave a `NOTE:` there.

Not a compatibility surface: no D4 operator signature, class name, error code or file format changes. Arm 2 makes the previous owner lose access immediately instead of at its next cache clear or restart; add one line to `docs/migration-notes.md` saying so, since a deployment could notice it.

## Tests

Owed by the project rules (a bug fix owes its reproduction), in `tests/DAE.Server.Tests/CatalogStoreTest.cs` beside `NestedRollbackRestoresEveryRightsOwner`:

- Rollback restores the in-memory owner: one parametrised test (outermost and nested rollback) on a table and a scalar type with a representation, asserting after the rollback that `Server.Catalog[...]`'s `Owner.ID` is `Admin` for the table, the type and one generated operator (e.g. the representation selector). Alternatively extend `NestedRollbackRestoresEveryRightsOwner` with the in-memory assertion and add only the outermost case.
- The previous owner loses its rights: the arm-2 sequence above, asserting the second insert as `OwnerUser` fails with the access-denied error (find the code with `FindError`, as `SetObjectOwnerOfUnknownObjectReportsCatalogObjectExpected` does).

Connect as a non-admin user with `instance.Server.Connect(new SessionInfo("OwnerUser", ""))`. Wrap the process work in `RunWithin` as the neighbouring tests do.

## TODO

- Add `SetCatalogObjectOwnerInstruction` to `CatalogDeviceSession.cs` (inside `#if LOGDDLINSTRUCTIONS`), restoring the owner and clearing the object's cached right assignments for every user.
- Add the object-taking `SetCatalogObjectOwner` to `ServerCatalogDeviceSession`, logging the instruction in a transaction and clearing the object's rights from every user's cache; keep the store write.
- Change `SystemSetObjectOwnerNode.ChangeObjectOwner` to call it instead of assigning `Owner`.
- Look at `SetRightOwner`'s new-owner-only cache clear; fix if trivially the same, else a `NOTE:`.
- Check the cached-plan question above; fix or `NOTE:`.
- Add the two reproduction tests; run `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest"`, then the whole `DAE.Server.Tests` project.
- Add the `docs/migration-notes.md` line for arm 2.
