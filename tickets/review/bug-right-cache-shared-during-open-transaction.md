description: Right-check answers are no longer shared between sessions while a transaction that has changed a right is open: each session's check reads through its own store connection and bypasses the shared cache when any change is pending.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDevice.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md
----
## What landed

`CatalogDevice` now keeps three counts, all read and written only inside `lock (Catalog)`:
- `_pendingRightNameCounts` (case-sensitive, matches DAERights.Name and `_changedRightNames`),
- `_pendingUserIDCounts` (case-insensitive, matches `_changedRightsUserIDs`),
- `_pendingAllRightsCount`.

Six public helpers wrap them: `BeginPendingRight`/`EndPendingRight`, `BeginPendingUser`/`EndPendingUser`, `BeginPendingAllRights`/`EndPendingAllRights`; three read-only members expose the state: `HasPendingRight(string)`, `HasPendingUser(string)`, `HasPendingAllRights`. Entries are removed on last decrement so the fast-path check stays one empty-dictionary test.

The ticket text called these `internal`, but `ServerCatalogDeviceSession` lives in the Server assembly and had to call the `Has*` members from `UserHasRight`. There is no `InternalsVisibleTo` between the two assemblies, so I made all nine members `public` rather than add one just for these. The declaration is on a public class (`CatalogDevice`) and these are new members — nothing is removed — so no compatibility breakage. Reviewer should look at this call.

`CatalogDeviceSession` now calls the device helpers on first-add / forget-time:
- `RightsChanged`: `_changedRightNames.Add(name)` returning true drives `Device.BeginPendingRight(name)` (foreach over names, not `UnionWith`).
- `UserRightsChanged`: `_changedRightsUserIDs.Add(userID)` returning true drives `Device.BeginPendingUser(userID)`.
- `AllRightsChanged`: guarded on the previously-false state of `_allRightsChanged` so a nested call doesn't double-count.
- `ClearChangedRights(forget)`: on the outermost end (`forget = true`), under the existing `lock (Catalog)`, decrements each name/user/all before nulling the fields. A nested end still clears cached answers now but leaves the pending state in place — the ticket's design.

`ServerCatalogDeviceSession.UserHasRight` consults the pending flags inside its existing `lock (Catalog)`:
1. `bool hasPendingChange = Device.HasPendingAllRights || Device.HasPendingRight(rightName) || Device.HasPendingUser(userID);`
2. If pending, skip `FindCachedRightAssignment`; read `CatalogStoreConnection.UserHasRight` through this session's connection; do not fill the cache.
3. If not pending, old behavior: cache lookup, miss reads store and fills the cache.

`docs/migration-notes.md` — the last sentence of the *D4, catalog and libraries* entry for `bug-right-cache-stale-across-transactions` (previously "a check can still return the answer another session's check cached before it, from either side of the change, as in 3.1") now describes the closed gap.

## Tests

Two new tests in `tests/DAE.Server.Tests/CatalogStoreTest.cs` — both pin behaviours of this fix at the lowest layer that shows them (the in-process engine harness through `TestServerInstance`), both verified to fail without the fix and pass with it:

- **`UncommittedGrantNotUsableInOtherSession`** — Admin's open transaction grants right R to user U; a parallel `RunAs(U, ...)` tries the matching insert while the transaction is still open and is refused with `UnauthorizedRight`. Before the fix, the other session's check cached the uncommitted grant (seen through Admin's shared cache fill) and the insert succeeded. Covers "another session uses an uncommitted grant".
- **`ChangingSessionSeesOwnRevokeDespiteOtherSessionCheck`** — Grant R to U first; Admin's open transaction revokes R; a second Admin session evaluates `UserHasRight('U', R)` inside the transaction (sees `true` from the committed store, bypasses cache fill); Admin's own session then evaluates `UserHasRight('U', R)` inside the transaction and must see `false` (its own revoke). Before the fix, Admin's check read the stale cached `true` that the other session filled. Covers "the changing session misses its own change".

No test for the role-membership (`UserRightsChanged`) or "all-rights" (`AllRightsChanged`) arms — per the ticket, they share the same `HashSet.Add` → `Begin*` wiring as `_changedRightNames`/`BeginPendingRight`, and `RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction` already exercises the per-user record.

## Known gaps / things to look at

- **Public visibility of the device helpers** — see above. Reviewer may prefer `InternalsVisibleTo` for Server, or restrict just the `Has*` reads to public and keep `Begin*/End*` internal; either keeps the surface tighter.
- The `D4, catalog and libraries` entry in `migration-notes.md` still reads "done" and now describes the closed state — no new category needed, but worth a glance that the sentence integrates cleanly with the rest of the entry.
- **`UsersCache` lock** — all new `_pendingRightNameCounts` / `_pendingUserIDCounts` / `_pendingAllRightsCount` access goes through `lock (Catalog)`. The ticket's safety argument depends on `RightsChanged`/`UserRightsChanged`/`AllRightsChanged` and `UserHasRight` always taking that lock, which they do today. Reviewer should confirm no caller of `Begin*`/`End*`/`Has*` is added outside that lock.
- **Transactions scoped to a session** — the pending counters are per-device (process-wide, not per-session), which is correct: the whole point is that one session's open change is visible to every other session's check. The invariant that commits/rollbacks drain the counts to zero relies on `ClearChangedRights(forget: true)` running on the outermost `InternalCommitTransaction`/`InternalRollbackTransaction`; an exception path between `BeginPendingX` and the outer end that skips `ClearChangedRights` would leak counts. The existing try/finally in those methods already guards the clear, but reviewer should re-read the finally structure.

## Compatibility / migration

Behavior change is only inside an open transaction that has changed a right. Outside that window, nothing changes. The one sentence in `docs/migration-notes.md` for `bug-right-cache-stale-across-transactions` is updated; no new migration entry.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, no new warnings in the four `.cs` files (126 warnings total, all pre-existing).
- Pre-flip test confirmation (stash the fix, keep the new tests): both new tests fail with the pre-fix code.
- Focused: `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~CatalogStoreTest"` — 24 passed, 0 failed (62 s). Includes the two new tests and all 22 pre-existing cases in the fixture.
- Full suite in three parts (per `AGENTS.md`):
  - Part 1 (non-coverage-scripts across the whole solution): 13 passed (`Common.Tests`) + 59 passed (`DAE.Server.Tests`), 0 failed, 2 min 39 s.
  - Part 2 (`CoverageScripts` A-R): 67 passed, 11 skipped (manifest known-failure/unstable/fragment), 0 failed, 3 min 14 s.
  - Part 3 (`CoverageScripts` other letters): 48 passed, 13 skipped, **1 failed**, 6 min 22 s. The failure is `ServerLink` timing out at 120 s instead of its manifest's `SocketException`. This is the `one-off` recorded in `tickets/.pre-existing-known.md` ("on some machines... TCP connect to any closed loopback port is never refused and sits in SYN_SENT"). Confirmed with the record's protocol: `(New-Object Net.Sockets.TcpClient).ConnectAsync('127.0.0.1',8061).Wait(5000)` returned `False` after ~5.1 s, matching the record. Not a regression from this ticket.
- File hygiene: `.cs` files kept CRLF + BOM (`CatalogDeviceSession.cs`, `ServerCatalogDeviceSession.cs`, `CatalogStoreTest.cs`) or CRLF + no BOM (`CatalogDevice.cs`), matching their prior state. `docs/migration-notes.md` stays LF in git (`core.autocrlf=true` shows CRLF in the working tree but the index record is LF).
