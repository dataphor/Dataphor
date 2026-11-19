description: Right-check answers are no longer shared between sessions while a transaction that has changed a right is open: each session's check reads through its own store connection and bypasses the shared cache when any change is pending.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDevice.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Device.Catalog/ServerCatalogDeviceSession.cs, tests/DAE.Server.Tests/CatalogStoreTest.cs, docs/migration-notes.md
----
## What landed

`CatalogDevice` now carries three counters — `_pendingRightNameCounts` (case-sensitive), `_pendingUserIDCounts` (case-insensitive) and `_pendingAllRightsCount` — read and written only inside `lock (Catalog)`, with six mutators (`BeginPendingRight`/`EndPendingRight`, `BeginPendingUser`/`EndPendingUser`, `BeginPendingAllRights`/`EndPendingAllRights`) and three reads (`HasPendingRight`, `HasPendingUser`, `HasPendingAllRights`). `CatalogDeviceSession` wires the per-session record lifecycle (`_changedRightNames`/`_changedRightsUserIDs`/`_allRightsChanged`) into these counters at first-add and at the outermost end (`ClearChangedRights(forget: true)`). `ServerCatalogDeviceSession.UserHasRight` checks the counters inside its existing `lock (Catalog)` before consulting the shared cache: during the pending window it reads through its own store connection and skips the cache fill, so no session's view leaks into another session's check.

See `ticket(implement): bug-right-cache-shared-during-open-transaction` for the implementation diff.

## Review findings

### Checked

- Lock discipline: every read and mutation of the three counters goes through a path that holds `lock (Catalog)`. The six mutators are called from `RightsChanged`/`UserRightsChanged`/`AllRightsChanged`/`ClearChangedRights`, each inside `lock (Catalog)`; the three reads are called from `ServerCatalogDeviceSession.UserHasRight`, also inside `lock (Catalog)`. No caller outside that lock.
- Begin/End symmetry: `BeginPendingRight` is guarded by `_changedRightNames.Add(name)` returning true, so each session's record contributes exactly one increment per right; `EndPendingRight` runs from `ClearChangedRights(forget: true)` which iterates the same hash set. Same shape for user IDs; `AllRightsChanged` is guarded on `!_allRightsChanged`. Begin and End are paired through the hashset/flag — can't leak unless the hashset is lost without `forget: true`, which only happens if the server abandons the session mid-transaction.
- Clear-before-register ordering in `RightsChanged`/`UserRightsChanged`/`AllRightsChanged`: `ClearCachedRightAssignments` runs *before* `BeginPending*`, inside the same lock, so a reader that holds the lock after the write sees either "not pending, nothing cached" or "pending, won't cache" — never "not pending, stale cache".
- Nested rollback: `ClearChangedRights(forget: false)` clears cached answers but keeps hashsets and pending counts, as the ticket design calls for — outer-level changes are still open and must stay pending for other sessions. Confirmed by reading the two callers in `InternalCommitTransaction` and `InternalRollbackTransaction` at `CatalogDeviceSession.cs:1468,1524`.
- `AllRightsChanged` + per-right interaction across nested levels: outer per-right with inner all-rights (or vice versa) still ends with both End calls at the outermost forget, because the hashset entries are preserved alongside `_allRightsChanged`.
- Non-transaction call sites: `RightsChanged`/`UserRightsChanged`/`AllRightsChanged` skip the hashset add and the Begin* call when `!InTransaction`. Correct — there is nothing to replay later.
- `ServerCatalogDeviceSession.UserHasRight` short-circuits for `SystemUserID`/`AdminUserID`/`IsLoading` before the pending check, so the pending path adds no cost to those cases.
- Tests: the two new cases (`UncommittedGrantNotUsableInOtherSession`, `ChangingSessionSeesOwnRevokeDespiteOtherSessionCheck`) pin the two symmetric halves of the fix at the lowest layer that shows them (in-process engine harness through `TestServerInstance`). Running them against the pre-fix code fails (implementer's record) and against the fix passes. No test for the user-level or all-rights arms — correct call under this project's testing rule: the wiring is identical (`HashSet.Add` → `Begin*`), and `RoleMembershipNotKeptAfterRollbackDespiteCheckDuringTransaction` already exercises the per-user record; a second test of the same wiring is run-time tax without a new behaviour to pin.
- Migration-notes change: the final sentence of the existing `bug-right-cache-stale-across-transactions` entry now describes the closed gap. No separate entry — the compat story is the same, and the previous entry's sentence had already flagged this gap.
- Source hygiene: `.cs` files preserved their prior CRLF + BOM state (`CatalogDeviceSession.cs`, `ServerCatalogDeviceSession.cs`, `CatalogStoreTest.cs`) or CRLF + no BOM (`CatalogDevice.cs`); `docs/migration-notes.md` stays LF in the index. No reformatting.

### Found and fixed inline

- **Public → internal on the Begin/End helpers.** The implementation ticket flagged this: the ticket text called the mutators `internal` but all nine members shipped as `public` because `ServerCatalogDeviceSession` (Server assembly) needed to call the `Has*` reads and there is no `InternalsVisibleTo` across the DAE / Server assemblies. Tightened the six Begin/End mutators to `internal` (their only callers are in `CatalogDeviceSession`, same DAE assembly) and kept the three `Has*` reads `public` (used across the assembly boundary). Matches the asymmetric access the two sides actually need — the record lifecycle is internal to `CatalogDeviceSession`'s machinery, while the state is externally readable. Re-built and re-ran `CatalogStoreTest` (24 passed) and Part 1 (`NUnit.Where=class != …CoverageScripts`, 13 + 59 passed, 0 failed).

### Found and not filed

- **Commit-failure window.** `InternalCommitTransaction` passes `ClearChangedRights(committed)` and `committed = false` means the pending counts stay set, with the comment "A failed commit keeps the record for the rollback that follows it." If a failed outer commit is not followed by a rollback, the counts (and the hashset) leak for the session's lifetime. This is pre-existing — the hashset-based record already had the same fragility — and the server does run a rollback after a failed commit; the review's Begin/End add the same leak risk as the existing hashset. Not filed: no new class of defect, and no site owns it alone.

### Not filed — nothing found

- No major findings warranting a ticket. No tripwires (no "fine now; only matters if X" concerns at the sites touched). No accepted-tradeoff `NOTE:` at any of the sites, so no declined finding to respect.

## Compatibility / migration

Behaviour changes only inside an open transaction that has changed a right, the "every user" or the checked user's roles. Outside that window, the server behaves as before. The sentence added to the `bug-right-cache-stale-across-transactions` entry in `docs/migration-notes.md` describes the closed gap; no separate entry needed.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, 126 warnings, none new in the four touched `.cs` files.
- `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~CatalogStoreTest"`: 24 passed, 0 failed (62 s). Includes the two new tests and the 22 pre-existing cases in the fixture.
- Part 1 of the three-part suite (`NUnit.Where=class != …CoverageScripts`): `Common.Tests` 13 passed; `DAE.Server.Tests` 59 passed. 0 failed, 2 m 39 s.
- Parts 2 and 3 not re-run under this review — the implementer ran them (67 + 48 pass, manifest-expected skips, and the loopback-port one-off for `ServerLink` recorded in `tickets/.pre-existing-known.md`); the review's only code change is a visibility tightening with no runtime effect on those paths.
