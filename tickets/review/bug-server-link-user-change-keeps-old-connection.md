description: Changing or removing the credentials one Dataphor server uses to reach another now ends the issuing session's connection to it, so the session's next use of the link connects with what the catalog says rather than going on with the old credentials.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md
----
## What changed

A *server link* is a D4 object naming another Dataphor server; which username and password it connects with comes from its *link users*. Six D4 operators change that mapping (`CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword`, `DropServerLinkUser`). Before this change, none of them affected a session that had already connected through the link: the credentials are read once when the connection opens, and the connection is kept for the life of the session's process, so a password rotation took effect only for sessions that connected afterwards.

The rule that `drop server` and `alter server` already honoured - *a catalog change that affects how a link connects ends the issuing process's connection to that link, so that its next use of the link connects afresh* - now holds for the link user operators too, and it lives at one site instead of being repeated at seven.

**One site.** `CatalogDeviceSession.UpdateCatalogObject` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:1889`) now calls the existing `CloseRemoteSession` when the object it just updated is a `Schema.ServerLink`. Placement, inside the existing `!ServerProcess.InLoadingContext()` guard and after `InternalUpdateCatalogObject`, is what makes a catalog load close nothing and a failed change cost no reconnect. The type test is the narrowing: `UpdateCatalogObject` is called from about thirty sites for every kind of catalog object, and exactly seven of them pass a server link - the six operators above and `AlterServerNode`.

**`AlterServerNode` lost its own call.** The explicit `CloseRemoteSession(server)` in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` was redundant once `UpdateCatalogObject` does the same thing on the line above, so it is gone. Its comment - the ordering reasoning and the `NOTE: accepted tradeoff` about other sessions keeping their connections - moved to the new single site rather than being deleted.

**`DropServerLinkUser` falls back, it does not refuse.** With the mapping gone, the issuing session's next use of the link falls back to the link's default credentials (`Schema.ServerLink.GetUser`). The original author's `// TODO: Prevent drop if user has active sessions on this server?` is replaced by a `NOTE:` recording that decision, why a refusal was declined (it would add a new error to an existing operator signature, which a catalog rebuild script that drops and re-creates a link user would start failing on; and it would have to consult the server-wide `RemoteSessions`, so an idle session holding a connection could block a password rotation outright) and the revisit condition.

**Other sessions are untouched.** A different process that has already connected keeps its connection until it ends. That was settled as an accepted tradeoff by `engine-server-link-session-outlives-drop` - closing another process's connection makes a blocking network call on that process's channel - and the `NOTE:` carrying it is now at the shared site.

## Test added

One case, `TestLinkUserChangeClosesTheChangingProcessConnection` in `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, beside the existing drop and alter cases. It creates a link naming the fixture's child server, uses it so the connection opens, runs `CreateServerLinkUser('Admin', Name('RecredentialedLink'), 'Admin', 'wrong')` - credentials the other server refuses - and then asserts that this process holds no connection to another server and that the next use of the link throws.

What it verifies: that a link user change ends the issuing process's connection, and that the next use really does reconnect (which is why the new credentials have to be bad ones - a kept connection would succeed). It asserts only *that* it throws: a refused connection arrives wrapped and reads as "Runtime error: Exception has been thrown by the target of an invocation", so the message is the reflection invocation's rather than the far server's.

One case is deliberate: all six operators and the alter now reach the rule through the same `UpdateCatalogObject` site, so a second operator's case would exercise the same code. The five existing cases in the fixture cover the drop, the alter, the deferral to the outermost commit, the discard on rollback, and the re-created link.

## Validation run

`dotnet build Dataphor.slnx` - 0 errors.

The link cases plus the `ServerLink` coverage script, 8 of 8 passed in 41 s:

```
dotnet test tests/DAE.Server.Tests --no-build --filter "Name=TestAlterClosesTheAlteringProcessConnection|Name=TestDropClosesTheDroppingProcessConnection|Name=TestDropInTransactionDefersTheCloseToTheCommit|Name=TestRolledBackDropKeepsTheConnection|Name=TestLinkRecreatedUnderTheSameNameConnectsAfresh|Name=TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow|Name=TestLinkUserChangeClosesTheChangingProcessConnection|Name=ServerLink" --logger "console;verbosity=normal"
```

The whole suite in the three part commands from the root `AGENTS.md`, all green: 13 passed in `Common.Tests` and 445 in `DAE.Server.Tests` (part 1, 7 m 16 s), 67 passed / 11 skipped (part 2, 3 m 47 s), 49 passed / 13 skipped (part 3, 3 m 20 s). No failures, pre-existing or otherwise; the skips are the usual ones that need an external SQL Server.

## Compatibility and migration

No operator signature, error code or D4 semantic changed. What a D4 library can notice is that the session issuing one of these operators stops holding its old connection, so its next use of the link connects again. The existing `done` row in `docs/migration-notes.md` that states the drop/alter rule was extended to name the six link user operators and to say that a link user drop falls back rather than refusing - one row, because it is one rule.

## What a reviewer should look at

- **Is the type test at `UpdateCatalogObject` the right place for the rule, or is it too far from the operators that need it?** The alternative is six explicit calls plus the one in `AlterServerNode`. The argument for the shared site is in the ticket: those seven are the only server-link callers in the tree, and a future operator that updates a link gets the rule for free. The argument against is that a reader of `SystemCreateServerLinkUserNode` cannot see that it closes a connection. Only the `DropServerLinkUser` `NOTE:` points at it.
- **The claim that exactly seven `UpdateCatalogObject` call sites pass a `Schema.ServerLink`** is the ticket's, re-checked here by reading the call sites, not by a mechanical type analysis. If a call site passes a `Schema.CatalogObject` that happens to be a link at run time, it now closes a connection where it did not before. Worth a second pair of eyes.
- **Known neighbour, out of scope and still true:** none of the six operators logs a DDL undo instruction, so a rolled-back transaction around one leaves the in-memory catalog holding the change while the store has rolled it back. That is filed as `bug-server-link-user-change-survives-rollback` in `tickets/backlog/`. This change adds no new inconsistency with it - the deferred close is discarded on rollback, so a rolled-back link user change keeps both the wrongly kept cache entry and the connection that matches it - but the pair is worth understanding together.
- **No tripwires were parked in code by this ticket**, and no new ticket was filed.
