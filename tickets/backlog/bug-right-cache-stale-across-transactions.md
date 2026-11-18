description: The server remembers each user's answer to "may this user use this right", and an answer looked up while a transaction that changes rights is still open can outlive that transaction, so a user can keep using a revoked right, or be refused a granted one, until the server restarts.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Schema/Security.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: static
severity: wrong-result
likelihood: unusual
tradeoffs: Under implicit transactions the window is a single statement, so a deployment that changes rights outside explicit transactions may never hit it, and the fix adds bookkeeping to every change of rights, owners and role membership.
----
## What happens

`ServerCatalogDeviceSession.UserHasRight` caches each user's answer per right name on the shared `Schema.User` object (`CacheRightAssignment`/`FindCachedRightAssignment` in `src/engine/DAE/Schema/Security.cs`) and reads the catalog store only when no answer is cached. Every method that changes who has a right clears the affected answers at the moment of the change, before its transaction commits: `GrantRightToUser`/`GrantRightToRole`, `RevokeRightFromUser`/`RevokeRightFromRole`, `RevertRightForUser`/`RevertRightForRole`, `InsertUserRole`/`DeleteUserRole`, `SetCatalogObjectOwner`, `SetRightOwner`, and dropping an object (`CatalogDeviceSession.ClearCatalogObject`). Two ways an answer then gets cached that the end of the transaction makes wrong:

- **Another session, while the transaction is open.** Each session reads the store through its own connection, and with the SQLite store in write-ahead-logging mode that connection sees the last committed state. A right check by another session after the clear caches the old answer, and nothing clears it when the transaction commits. Example: Admin begins a transaction and runs `RevokeRightFromUser('T.Insert', 'U')`; before Admin commits, `U` inserts into `T` in its own session (granted, from the committed store, and cached); Admin commits; `U` can go on inserting.
- **The changing session itself, then a rollback.** The changing session's store connection is inside the transaction and sees the uncommitted change. Example: in a transaction, `GrantRightToUser('T.Insert', 'U'); UserHasRight('U', 'T.Insert')` caches `true`; the transaction rolls back; the cached `true` stays. Since `bug-set-object-owner-rollback-keeps-owner`, a rolled-back `SetObjectOwner` clears its object's rights for every user on undo; none of the other changes listed above has an undo.

Found by reading the code while fixing `bug-set-object-owner-rollback-keeps-owner`; not run. What would confirm it: a test in `CatalogStoreTest` with two sessions following the first example (the last insert should be refused with error 106157, `UnauthorizedRight`), and a single-process test following the second (after the rollback `UserHasRight('U', 'T.Insert')` should be false).

## Expected

A right check made after a transaction that changed rights has ended answers from the committed store, whichever session made the check and when it first asked.

## Known direction

Clearing the changed answers again when the store transaction ends would close both windows: the catalog device session records the right names a transaction changed (or the user, for a role membership change, whose every answer may change) and clears them for every user after the outermost commit has committed the store, and after a rollback at any level. The clear at the time of the change stays, so that the changing session sees its own change.
