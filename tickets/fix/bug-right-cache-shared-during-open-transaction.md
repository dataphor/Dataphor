description: While a transaction that changes who may use a right is still open, all sessions share one remembered answer to "may this user use this right", so another session can use a grant that is later rolled back, and the session making the change can fail to see its own change.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, src/engine/DAE/Schema/Security.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs
repro: static
severity: edge-case
likelihood: unusual
tradeoffs: The window lasts only as long as the changing transaction and 3.1 behaved the same; closing it adds state shared between sessions to the right check, which every statement runs.
----
## What happens

The server caches each user's answer per right name on the `Schema.User` object, which every session shares (`ServerCatalogDeviceSession.UserHasRight`, under `lock (Catalog)`). Each session reads the catalog store through its own connection, which sees that session's own uncommitted writes and only the committed state of others. Since `bug-right-cache-stale-across-transactions`, each change clears the affected answers after its store write and again when its transaction ends (`RightsChanged` and `ClearChangedRights` in `CatalogDeviceSession`), so no wrong answer outlives the transaction. While the transaction is open, though, whichever session checks first fills the cache for all of them:

- **Another session uses an uncommitted grant.** Admin grants right R to user U in a transaction and checks `UserHasRight('U', R)` inside it: true, read from Admin's uncommitted write, and cached on U. U, in its own session, uses R before Admin's transaction ends: allowed from the cache. If Admin then rolls back, U has used a right that was never granted.
- **The changing session misses its own change.** Admin revokes R from U in a transaction; another session checks U's R and caches the committed answer (granted); Admin's own `UserHasRight('U', R)` inside the transaction then answers granted.

To confirm the first: take `GrantNotKeptAfterRollbackDespiteCheckDuringTransaction` in `tests/DAE.Server.Tests/CatalogStoreTest.cs` and, after the check inside the transaction, have `RightsUser` insert into the table in its own session before the rollback. The insert should be refused; by reading the code it is allowed.

## Expected

A right check answers as the checking session's transaction sees the store: the committed state plus that session's own uncommitted changes, never another session's uncommitted change.

## Notes

One shape that would do it: a device-wide count, per right name (and per user for role changes, and one for "every user"), of open transactions that have changed it, kept by the same helpers that record the change today; `UserHasRight` neither reads nor fills the cache for a right with a pending change and asks the store through the checking session's own connection. Registering the change and filling the cache both happen under the catalog's lock, so a check either finished before the registration (and the clear after the write drops its answer) or sees the pending change.
