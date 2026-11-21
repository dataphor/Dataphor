description: Changing or removing the credentials one Dataphor server uses to reach another has no effect on a session that has already connected to it: that session goes on using the old credentials until it ends. Make the change end that session's connection, so its next use of the link connects with what the catalog now says.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md
repro: verified
difficulty: easy
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server; `ExecuteOn`, `EvaluateOn` and `select ... on <link>` run work on it. Which username and password the link connects with comes from its *link users* - a per-user mapping with a fallback - and six D4 operators change that mapping: `CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword` and `DropServerLinkUser` (all in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs`, lines 1716-1823).

The credentials are read once, when the connection to the other server is opened, and the connection is then kept for the life of the session's process. None of the six operators ends it. So a session that has already used the link keeps reaching the other server as whoever it first connected as - including the session that performs the change, which is the one an administrator is most likely to be sitting in front of. A password rotation, a change of which account the link uses, and the removal of the mapping altogether all take effect only for sessions that connect afterwards.

The sibling ticket `engine-server-link-session-outlives-drop` closed the same gap for the link's *other* settings: `drop server` and `alter server` now end the issuing process's connection, so its next use of the link connects with what the catalog currently says. It left the link users alone, because the behaviour it was fixing was about host name, port and session settings. The result is that half a rule holds:

> a catalog change that affects how a link connects ends the issuing process's connection to that link, so that its next use of the link connects afresh.

`alter server` honours it; the six link-user operators do not. This ticket makes the rule hold.

## Reproduced, and the fix measured

Verified twice, Debug, one machine, 2026-11-21. Both runs used a throwaway case added to `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs` and removed again; the working tree is back to its committed state and nothing of the experiment is left behind.

The case creates a link naming a second Dataphor server started as a child process, uses it once so that the connection opens with the fallback credentials (the other server's `Admin`, blank password), then runs `CreateServerLinkUser('Admin', Name('<link>'), 'Admin', 'wrong')` - credentials the other server refuses - and uses the link again.

Before any change, as committed:

```
REPRO connection count after the change: 1
REPRO issuing process use after the change: SUCCEEDED - old connection kept
REPRO second process use after the change: failed - Runtime error: Exception has been thrown by the target of an invocation.
```

So the credentials really did change in the catalog - a second process on the same session does connect, and is refused - and only the already-open connection failed to notice.

With the change this ticket asks for, the same case:

```
REPRO connection count after the change: 0
REPRO issuing process use after the change: failed - Runtime error: Exception has been thrown by the target of an invocation.
REPRO second process use after the change: failed - Runtime error: Exception has been thrown by the target of an invocation.
```

and the six cases already in `ServerLinkConnectionTest` plus the `ServerLink` coverage script (which creates link users through D4) all passed, with the explicit close in `AlterServerNode` removed as well - 8 of 8 in 32 seconds including the build, run as:

```
dotnet test tests/DAE.Server.Tests --filter "Name=TestAlterClosesTheAlteringProcessConnection|Name=TestDropClosesTheDroppingProcessConnection|Name=TestDropInTransactionDefersTheCloseToTheCommit|Name=TestRolledBackDropKeepsTheConnection|Name=TestLinkRecreatedUnderTheSameNameConnectsAfresh|Name=TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow|Name=ServerLink" --logger "console;verbosity=normal"
```

## The change

**One site, not seven.** Every one of the six operators, and `AlterServerNode` too, finishes by calling `program.CatalogDeviceSession.UpdateCatalogObject(serverLink)`, and across the whole tree those seven are the only `Schema.ServerLink` arguments `UpdateCatalogObject` ever gets (checked: the tree has 30 `UpdateCatalogObject` call sites, of which 7 pass a server link - the six in `SystemNodes.cs` and `AlterServerNode` at `DDLNodes.cs:2659` - and the rest pass scalar types, roles, event handlers, table variables, views, operators, constraints, references and devices). So the rule belongs in `UpdateCatalogObject` itself:

```csharp
		/// <summary>Updates the given object in the catalog cache. If this is not a repository, also updates the object in the catalog store.</summary>
		public void UpdateCatalogObject(Schema.CatalogObject objectValue)
		{
			// If we are not deserializing
			if (!ServerProcess.InLoadingContext())
			{
				InternalUpdateCatalogObject(objectValue);

				if (objectValue is Schema.ServerLink serverLink)
					CloseRemoteSession(serverLink);
			}
		}
```

That is the whole engine change; `CloseRemoteSession` already exists (`CatalogDeviceSession.cs:3298`), already defers to the outermost commit inside a transaction and already discards the deferred close on rollback.

Three things that placement settles, and that the implementation should keep settled:

- **Inside the loading guard.** Catalog loading goes through `CacheCatalogObject`, not `UpdateCatalogObject`, and `UpdateCatalogObject` additionally does nothing while the process is deserializing. Keeping the close inside that same guard means a load never closes anything.
- **After `InternalUpdateCatalogObject`.** The same reasoning the comment in `AlterServerNode` records: a catalog update that fails closes nothing, so a failed change does not cost a reconnect.
- **The type test is the narrowing.** `UpdateCatalogObject` is called for every kind of catalog object from about thirty sites; the condition states at that one site exactly which kind of object the rule is about.

**`AlterServerNode`'s explicit call becomes redundant** and should be removed (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`, the `program.CatalogDeviceSession.CloseRemoteSession(server);` after `UpdateCatalogObject(server)`), because `UpdateCatalogObject(server)` immediately above it now does the same thing. The comment above that call carries the recorded accepted tradeoff about other sessions, so **move it, do not delete it**: the ordering reasoning and the `NOTE: accepted tradeoff` about this process's connection only both belong at the new site in `UpdateCatalogObject` (or on `CloseRemoteSession`, whose remarks already say the same thing in prose - in which case keep the `NOTE:` tag so the set stays greppable). A reviewer who finds no accepted-tradeoff note at either place will re-file the question about other sessions.

**`DropServerLinkUser` falls back rather than refusing.** With the mapping gone, a fresh connection falls back to the other server's `Admin` with a blank password (`Schema.ServerLink.GetUser`, `src/engine/DAE/Schema/ServerLink.cs:123`). So after `DropServerLinkUser` the rule makes the session connect as the fallback rather than keep the dropped account - a change of identity, not a refusal. The original author's note at that operator asks the other question: `// TODO: Prevent drop if user has active sessions on this server?`. Settle it as fallback, and replace that TODO with a `NOTE:` recording the decision, because a refusal would:

- add a new error to an existing operator signature, which a catalog rebuild script that drops and re-creates a link user would start failing on - the kind of change the project's compatibility rule says needs a ticket that says so explicitly; and
- be refusable by any session at all, since what it would have to consult is the server-wide `RemoteSessions` table and not just this process's list, so an idle session holding a connection would block a password rotation outright. That is worse than the fallback it would be protecting against.

**Other sessions keep their connection.** `engine-server-link-session-outlives-drop` settled, as a recorded accepted tradeoff, that a *different* process keeps its connection until it ends, because closing another process's connection makes a blocking network call on that process's channel. The same reasoning applies here; do not re-open it, and do not add an operator for closing another session's remote connection - that would be a new surface and needs its own ticket.

## Compatibility

The six operator signatures do not change, no error code changes, and no D4 semantics change. What changes is that the session issuing one of these operators stops holding its old connection to that link, so its next use of the link connects again. That is a behaviour a D4 library could notice, so it needs a migration note.

`docs/migration-notes.md` already has a `done` row stating the rule for `drop server` and `alter server` (search it for "end the connection the issuing process holds"). Extend that row to name the link-user operators as well, rather than adding a second row: the rule is one rule, and a reader should not have to find it twice. Keep the row's existing sentences about the deferral to the outermost commit, the rollback, the unreachable server, and other processes keeping their connections.

## Known neighbour, deliberately out of scope

None of the six operators logs a DDL instruction, so a rolled-back transaction around one of them leaves the in-memory catalog holding the change while the store has rolled it back (checked by reading `InternalRollbackTransaction` in `CatalogDeviceSession.cs`: nothing restores `serverLink.Users`, and `undidDDL` stays false, so not even the cache invalidation runs). That is a separate defect with a separate root cause - missing undo instructions for link users - and it is filed as `bug-server-link-user-change-survives-rollback` in `tickets/backlog/`. The change this ticket makes introduces no new inconsistency with it: the deferred close is discarded on rollback, so a rolled-back link-user change keeps both the wrongly kept cache entry and the connection that matches it.

## TODO

- Move the rule into `CatalogDeviceSession.UpdateCatalogObject` (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs:1889`): inside the existing `!ServerProcess.InLoadingContext()` guard, after `InternalUpdateCatalogObject`, call `CloseRemoteSession` when the object is a `Schema.ServerLink`.
- Remove the now-redundant `CloseRemoteSession(server)` call from `AlterServerNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`), and carry its comment - both the ordering reasoning and the `NOTE: accepted tradeoff` about other sessions - to the new single site.
- In `SystemDropServerLinkUserNode` (`src/engine/DAE/Runtime.Instructions/SystemNodes.cs:1803`), replace `// TODO: Prevent drop if user has active sessions on this server?` with a `NOTE:` recording that the drop makes the issuing session fall back to the link's default credentials rather than refusing, why a refusal was declined (a new error on an existing operator signature, and an idle session would be able to block a password rotation), and the revisit condition (a deployment that needs a link user to be undroppable while a connection is held).
- Add one characterization case to `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`, beside the existing drop and alter cases and in their idiom: create a link naming the fixture's child server, use it once so the connection opens, run `CreateServerLinkUser('Admin', Name('<link>'), 'Admin', 'wrong')`, then assert `ConnectionCount()` is 0 and that the next use of the link throws. One case is enough: the rule now lives at one shared site, so a second operator would be testing the same code. Assert only *that* it throws, not the message or the exception type - a refused connection arrives wrapped and reads as "Runtime error: Exception has been thrown by the target of an invocation." (measured). `Throws.Exception` needs a statement-bodied lambda, because the fixture's `RemoteCallDepth` helper returns an `int` and NUnit's overload takes an `Action`.
- No change to `tests/DAE.Server.Tests/DAE.Server.Tests.csproj`: it lists its sources explicitly with `<Compile Include>` rather than globbing, and `ServerLinkConnectionTest.cs` is already listed. Worth knowing for a later ticket: a *new* test file in that project is silently not compiled, and `dotnet test --filter` then reports "discovered 0 of 0".
- Extend the existing `done` row in `docs/migration-notes.md` that states the drop/alter rule so that it names the six link-user operators too.
- Validate: `dotnet build Dataphor.slnx`, then a `--filter` run over the `ServerLinkConnectionTest` cases including the new one together with `Name=ServerLink` for the coverage script, then the three part commands from the root `AGENTS.md`.
