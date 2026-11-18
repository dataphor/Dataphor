description: When a transaction that changed the owner of a table or other catalog object is rolled back, the saved catalog goes back to the old owner but the running server keeps treating the new user as the owner until the object is reloaded or the server restarts.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/Server/Runtime.Instructions/SecurityNodes.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Needs a SetObjectOwner inside a transaction that is then rolled back, which few applications do, and a restart puts memory back in line with the store; a maintainer may defer it behind defects hit in normal use.
----
## What happens

Found by `store-cursor-update-wrong-rows` while writing `CatalogStoreTest.NestedRollbackRestoresEveryRightsOwner`. In one process: begin a transaction, `SetObjectOwner('<table>', 'Admin')`, begin a nested transaction, `SetObjectOwner('<table>', '<user>')`, roll back the nested transaction, commit the outer one.

- The catalog store is right: the table's `DAECatalogObjects` row and its six `DAERights` rows are owned by `Admin` (the test checks this).
- The in-memory catalog is not: `Server.Catalog`'s object for the table still has `Owner.ID` = `<user>` after the commit (observed with a temporary line in that test that printed it; not kept).

Expected: after the rollback the in-memory owner is `Admin` again, as the store is. The same presumably holds for a rollback of an outermost transaction, which was not tried.

## Cause (static)

`SystemSetObjectOwnerNode.ChangeObjectOwner` (`src/engine/Server/Runtime.Instructions/SecurityNodes.cs`) sets `objectValue.Owner = user` directly, for the object and, for a scalar type, for its generated operators, and then calls `ServerCatalogDeviceSession.SetCatalogObjectOwner`, which writes only the store. Nothing records the old owner for the catalog device session's rollback, unlike creating or dropping an object (whose in-memory changes are undone by the session's instructions, `CreateCatalogObjectInstruction` and its kin in `CatalogDeviceSession`). Ownership decides `IsOwner` checks, so after such a rollback the new user can still do what only the owner may (alter, drop, grant) until the object is reloaded.

Not checked: whether `SetRightOwner` (`SystemSetRightOwnerNode`), which writes only the store and clears cached right assignments, has an in-memory counterpart with the same gap.

## Reproduction to add

Extend `CatalogStoreTest.NestedRollbackRestoresEveryRightsOwner`, or add a test beside it, asserting that the in-memory object's owner (`Server.Catalog`, or an ownership check such as dropping the table as `<user>`) is `Admin` after the rollback.
