description: While a transaction that changes who may use a right is still open, every session can read a single shared cached answer for that right; another session can use a grant that is later rolled back, and the session making the change can miss its own change. Close the window by tracking which rights, users and "every user" have pending changes at the device level, and making right checks bypass the cache for those while the change is open.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDevice.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md
repro: static
----
## Shape

The remaining sharing is that right-check answers live on the shared `Schema.User` objects, so while one session's transaction has changed a right, whichever session checks first fills the single cached answer for every session — correct for the filler, wrong for everyone else. The per-session record `CatalogDeviceSession` already keeps (`_changedRightNames`, `_changedRightsUserIDs`, `_allRightsChanged`) already names what is uncertain for every session while a transaction is open; mirror that set at the **device** so the right check sees it, and have the check skip the cache (both read and fill) while the entry is set.

### What to add on `CatalogDevice`

Three counts, all read and written only inside `lock (Catalog)` (the existing shared lock the right helpers and `UserHasRight` already take):

- `Dictionary<string, int> _pendingRightNameCounts` — count of open per-session records that have added each right name. Case-sensitive, like `_changedRightNames` and the store's `DAERights` key.
- `Dictionary<string, int> _pendingUserIDCounts` — count per userID of open per-session records that have added that user (for role-membership changes). Case-insensitive, like `_changedRightsUserIDs`.
- `int _pendingAllRightsCount` — open per-session records that have the "all rights changed" flag set.

Expose, on `CatalogDevice`:

- `BeginPendingRight(string)` / `EndPendingRight(string)`, `BeginPendingUser(string)` / `EndPendingUser(string)`, `BeginPendingAll()` / `EndPendingAll()` — all expect `lock (Catalog)` already held and bump/drop the matching counter; drop removes the dictionary entry when it reaches zero so the fast path stays cheap.
- `HasPendingRight(string)`, `HasPendingUser(string)`, bool `HasPendingAllRights` — read-only, same lock expectation.

### What changes in `CatalogDeviceSession`

In the Security region:

- `RightsChanged(params string[] rightNames)`: inside the existing `lock (Catalog)`, after the immediate clear, when `InTransaction`, for each name whose `_changedRightNames.Add(name)` returned `true`, call `Device.BeginPendingRight(name)`. (A right added again in a nested level is already in the record and must not double-count.)
- `UserRightsChanged(string userID)`: same shape against `_changedRightsUserIDs` and `BeginPendingUser`.
- `AllRightsChanged()`: when `InTransaction` and `_allRightsChanged` was false, set it, then call `Device.BeginPendingAll()`.
- `ClearChangedRights(bool forget)`: when `forget` is true, inside the same `lock (Catalog)`, before (or after — order is a no-op) nulling the fields, call `EndPendingRight` for every name in `_changedRightNames`, `EndPendingUser` for every userID in `_changedRightsUserIDs`, and `EndPendingAll()` if `_allRightsChanged`. A nested-level end still clears cached answers now but leaves the pending state in place: the record (and the device counts) stay until the outermost level ends, which matches how the session record already works.

### What changes in `ServerCatalogDeviceSession.UserHasRight`

Inside the existing `lock (Catalog)`:

1. Resolve the user and compute `bool hasPendingChange = Device.HasPendingAllRights || Device.HasPendingRight(rightName) || Device.HasPendingUser(userID);`.
2. If `!hasPendingChange`: try `user.FindCachedRightAssignment(rightName)`; a hit returns its `Granted`.
3. Otherwise (or on cache miss): acquire the store connection and read `CatalogStoreConnection.UserHasRight(userID, rightName)` — this session's own connection, which sees this session's uncommitted writes and only committed writes from others.
4. If `!hasPendingChange` and this was a cache miss, `CacheRightAssignment` the answer. If `hasPendingChange`, do **not** write to the cache: the answer we just read is specific to this session's view.

The fast path — no pending anything — stays one dictionary-empty check plus two `HasPending*` calls (each is a dictionary `TryGetValue`); the common case where nothing has pending rights is one cache lookup and no store round-trip, as today.

### Why this is safe under the catalog's lock

Both the `Begin*`/`End*` calls and the right check run inside `lock (Catalog)`; `RightsChanged` (clear + register + bump) and `UserHasRight` (pending check + cache fill) are serialized against each other. A check either runs entirely before a change registers — in which case the change's own post-write clear in `RightsChanged` drops any stale cached answer when it runs — or sees the pending flag and refuses to use or fill the cache. A rolled-back change ends by decrementing the count and clearing again (`ClearChangedRights(true)`), both under the lock; the next check sees no pending flag and will cache a fresh store read.

### Scenarios after the fix

- **Another session uses an uncommitted grant.** Admin's transaction grants R to U → `BeginPendingRight(R)` → count 1. Admin's own `UserHasRight('U', R)` inside the transaction sees pending, reads Admin's uncommitted store (granted), does not cache. U's session does an insert that triggers `UserHasRight('U', R)`: sees pending, reads U's own connection (committed, no grant), is refused. Admin rolls back → `EndPendingRight(R)` → count 0, clear again. Correct.
- **The changing session misses its own change.** Admin's transaction revokes R from U → `BeginPendingRight(R)`. Another session's `UserHasRight('U', R)` meanwhile: sees pending, reads committed store (granted), does not cache. Admin's own `UserHasRight('U', R)` inside the transaction: sees pending, reads Admin's uncommitted store (revoked). Correct.

## Compatibility and migration

The behaviour change is only inside the window of an open transaction that has changed a right: right checks during that window may return different answers than 3.1 did (they return what the checking session's store view says, instead of a first-write-wins shared cache). Outside that window nothing changes. Add one sentence to the *D4, catalog and libraries* entry in `docs/migration-notes.md` that this gap from `bug-right-cache-stale-across-transactions` is now closed; no new migration category is needed. No public type or member on `CatalogDevice` is removed; the three added helpers are `internal` to the DAE assembly (same as `UsersCache`).

## Tests (add to `tests/DAE.Server.Tests/CatalogStoreTest.cs`)

Lowest layer that shows each arm; existing tests stay. One each — neither duplicates another:

- `UncommittedGrantNotUsableInOtherSession`: Admin opens a transaction, `GrantRightToUser('General.…Insert', 'RightsUser')`, evaluates `UserHasRight('RightsUser', …)` inside it (true, from the fix), then another `RunAs(instance, "RightsUser", …)` tries the insert — must be refused with `UnauthorizedRight`. Then Admin rolls back. The static-repro note on the ticket calls out this exact extension of `GrantNotKeptAfterRollbackDespiteCheckDuringTransaction`.
- `ChangingSessionSeesOwnRevokeDespiteOtherSessionCheck`: `GrantRightToUser('General.RevokedTInsert', 'RightsUser')`. Admin opens a transaction, `RevokeRightFromUser('General.RevokedTInsert', 'RightsUser')`, then (still inside) a second Admin session evaluates `UserHasRight('RightsUser', 'General.RevokedTInsert')` — gets true, from the committed store. Admin's own session then evaluates the same — must get false (pending-flag bypasses the cache the second Admin filled). Commit. Afterwards `RightsUser`'s insert is refused — `RevokeRefusedAfterCommitDespiteCheckDuringTransaction` already covers that half, so stop at the in-transaction assertion.

No test for the role-membership or "all-rights" arms: they share one code path with the per-right arm (same `HashSet.Add` → `Begin*` wiring), and `RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction` already covers the per-user record.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, no new warnings in the edited files.
- Focused: `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest|Name=Security|Name=SecurityLibrary"` — passes with the new tests.
- Full suite in parts under the agent runner (see root `AGENTS.md` — never `dotnet test Dataphor.slnx` as one command).
- Preserve CRLF, tabs and byte-order-mark state of every edited `.cs` file; `docs/migration-notes.md` stays LF.

## TODO

- Add the three pending maps and the six device-level helpers to `CatalogDevice` (DAE assembly, same file as `UsersCache`).
- Wire `RightsChanged`, `UserRightsChanged`, `AllRightsChanged` and `ClearChangedRights(bool)` in `CatalogDeviceSession` to the device counters via first-add / forget-time bookkeeping.
- Change `ServerCatalogDeviceSession.UserHasRight` to consult the pending flags and bypass cache read + fill when any apply.
- Update the *D4, catalog and libraries* migration-notes entry for `bug-right-cache-stale-across-transactions` to note the in-transaction gap is closed.
- Add `UncommittedGrantNotUsableInOtherSession` and `ChangingSessionSeesOwnRevokeDespiteOtherSessionCheck` to `CatalogStoreTest.cs`; confirm each fails before the fix and passes after.
- Run the build, focused filter, and the three-part full-suite commands; record pass counts in the handoff.
