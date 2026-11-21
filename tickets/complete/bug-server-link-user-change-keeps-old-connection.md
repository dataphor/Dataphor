description: Changing or removing the credentials one Dataphor server uses to reach another now ends the issuing session's connection to it, so the session's next use of the link connects with what the catalog says rather than going on with the old credentials.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md, docs/modernization-plan.md
repro: verified
----
## What landed

A *server link* is a D4 object naming another Dataphor server; which username and password it connects with comes from its *link users*. Six D4 operators change that mapping (`CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword`, `DropServerLinkUser`). Before this change none of them affected a session that had already connected through the link: the credentials are read once when the connection opens, and the connection is kept for the life of the session's process, so a password rotation took effect only for sessions that connected afterwards.

The rule `drop server` and `alter server` already honoured — *a catalog change that affects how a link connects ends the issuing process's connection to that link, so that its next use of the link connects afresh* — now holds for the link user operators too, and it lives at one site instead of being repeated at seven. `CatalogDeviceSession.UpdateCatalogObject` tests the object it has just updated and calls the existing `CloseRemoteSession` when it is a `Schema.ServerLink`; `AlterServerNode`'s own explicit call is gone as redundant. `ticket(implement): bug-server-link-user-change-keeps-old-connection` has the detail, and the two accepted tradeoffs the work settled are recorded as `NOTE:` comments at their sites: other processes keep the connections they have already opened, and `DropServerLinkUser` lets the issuing session fall back to the link's default credentials rather than refusing while a connection made with the dropped mapping is held.

A drop still closes from `DropServerNode` rather than from the symmetric `DeleteCatalogObject`, and that asymmetry is deliberate rather than an oversight — see the tripwire below.

## Test

One case, `TestLinkUserChangeClosesTheChangingProcessConnection` in `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, beside the five existing link cases. It opens a connection through a link, runs `CreateServerLinkUser` with credentials the other server refuses, then asserts the process holds no connection and that the next use of the link throws. One case rather than six: all six operators and the alter reach the rule through the same site.

## Review findings

The implement-stage diff was read before its handoff summary. Nothing major was found, no new ticket was filed, and the one structural question the handoff asked the reviewer to settle was settled in favour of what landed.

**Verified, not taken on trust.**

- **The claim that exactly seven `UpdateCatalogObject` call sites can pass a `Schema.ServerLink`.** Re-derived from all 29 call sites in `src/`: six in `SystemNodes.cs` (the link user operators), `AlterServerNode`, and the rest passing a scalar type, role, event handler, table variable, view, operator, aggregate operator, constraint, reference, device scalar type, device operator or device. `Schema.ServerLink` derives from `Schema.CatalogObject`, not from `Schema.Device`, so `RegisterDevice`'s `UpdateCatalogObject(device)` cannot pass one. The claim holds, and no call site can pass a link by accident.
- **The new test is a real reproduction, and dropping `AlterServerNode`'s own call left nothing unguarded.** With the type test at the shared site disabled and the tree rebuilt, both `TestLinkUserChangeClosesTheChangingProcessConnection` and the pre-existing `TestAlterClosesTheAlteringProcessConnection` fail (`Expected: 0, But was: 1`). The site was restored and the tree rebuilt before the suite was run.
- **No rollback-replay path re-enters the close.** No DDL undo instruction class in `CatalogDeviceSession` takes a `Schema.ServerLink`, so the only references to one in that file are the deferred-close list, `CloseRemoteSession` and the new type test.
- **The `DropServerLinkUser` NOTE's fallback claim.** `Schema.ServerLink.GetUser` falls back from the asked-for user to the link's default user mapping, and from there to the admin user with an empty password. The comment is accurate.
- **Test isolation.** `InProcessTestFixture` gives one session and one process per test and `ConnectionCount()` filters by `ProcessID()`, so the bad link user the new case leaves in the fixture's shared catalog cannot reach the other cases' connection counts. The link names are distinct too.

**Minor findings, fixed in this pass.**

- **A byte-order mark was added to `docs/migration-notes.md`.** The file had none before the implement commit and the project's source hygiene rule is to preserve each file's byte-order-mark state. Stripped; the file's one real change, the extended rule row, is untouched.
- **`CloseRemoteSession`'s summary was stale.** It still read "a server link that has just been dropped or altered" after a third kind of change started reaching it. Now names all three, and its remark no longer says "for an alter" where it means any such change.
- **`UpdateCatalogObject` documented nothing about the new side effect.** This is the handoff's own open question — that a reader of `SystemCreateServerLinkUserNode` cannot see that it closes a connection. Answered with one `<remarks>` line on `UpdateCatalogObject`, which every call site sees without duplicating the rule at seven of them; the shared site was kept, as the alter test proves the rule is genuinely reachable there.
- **`AlterServerNode` lost an ordering constraint when its comment was shortened.** The removed comment said the close must come after the metadata is applied so that a failed alter closes nothing; the shared site's replacement only states the ordering against the catalog update. The sentence is back at `AlterServerNode`, where the constraint lives.
- **`docs/modernization-plan.md`'s K35 row read as the whole current rule.** "`drop server` and `alter server` close the issuing process's connection to the link" would tell a reader asking whether a password change reaches their session that it does not. One clause added naming the extension and this ticket; the row's provenance column still names the ticket that fixed K35.

**Tripwire parked in code, no ticket.** The close makes a blocking network call to the other server, so no caller of `UpdateCatalogObject` may hold the catalog lock while passing a server link. None of the seven does today, which is the only reason the shared site is safe — and it is also why a drop's close sits in `DropServerNode` rather than beside this one in `DeleteCatalogObject`: `DropServerNode` calls that method inside `lock (program.Catalog)`. A `NOTE:` at `CatalogDeviceSession.UpdateCatalogObject` records the constraint and the asymmetry it explains, with "revisit if a caller ever has to update a server link under that lock" as the condition. Conditional on a future caller, so a comment rather than a ticket.

**Accepted tradeoffs respected.** Two `NOTE: accepted tradeoff` comments sit at the sites this review examined — other processes keeping their connections, and the drop not refusing. Both were read; neither revisit condition has tripped, and neither was re-litigated.

**Tests.** None cut and none added. The one case the implementer wrote is the bug's reproduction, which the project's testing rule requires, and the negative check above shows it earns its run time. The five review fixes are comments and documentation, so no behaviour changed that a new test could pin.

**One correction to the handoff.** It says `bug-server-link-user-change-survives-rollback` is filed in `tickets/backlog/`; it is in `tickets/fix/`. The neighbour defect itself — none of the six operators logs a DDL undo instruction, so a rolled-back transaction around one leaves the in-memory catalog holding the change — is confirmed out of scope here, and this change adds no new inconsistency with it: the deferred close is discarded on rollback, so a rolled-back link user change keeps both the wrongly kept cache entry and the connection that matches it.

**Nothing else was found.** Error paths (a refused change closes nothing, a close deferred to the outermost commit, a close discarded by a rollback, a connection to a server that cannot be reached), resource cleanup (the close goes through the existing deferred-teardown list and `RemoteDisconnect`, no new handle or client is held), and type safety (the pattern-match narrowing replaces nothing that was unchecked) were all checked by reading and by running the cases that cover them. No file grew past a size worth splitting: the change is 13 added lines across three source files.

## Compatibility and migration

No operator signature, error code or D4 semantic changed. What a D4 library can notice is that the session issuing one of these operators stops holding its old connection, so its next use of the link connects again — the existing `done` row in `docs/migration-notes.md` that stated the drop/alter rule now names the six link user operators and the drop's fallback, as one row because it is one rule. `docs/modernization-plan.md`'s K35 row carries the same extension.

## Validation

`dotnet build Dataphor.slnx` — 0 errors, 143 warnings (all pre-existing).

The link cases plus the `ServerLink` coverage script: 8 of 8 passed in 33 s.

The whole suite in the three part commands from the root `AGENTS.md`, all green and with no pre-existing failure: 13 passed in `Common.Tests` and 445 in `DAE.Server.Tests` (part 1, 4 m 59 s), 67 passed / 11 skipped (part 2, 5 m 57 s), 49 passed / 13 skipped (part 3, 4 m 13 s). The skips are the usual ones that need an external SQL Server.
