description: Changing or removing the credentials one Dataphor server uses to reach another has no effect on a session that has already connected to it: that session goes on using the old credentials until it ends.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/SystemNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, docs/migration-notes.md
repro: verified
severity: edge-case
likelihood: unusual
tradeoffs: It only affects the session that is already connected, and that session can simply be reconnected, so a maintainer may reasonably rank it below work a user can see; and the tidy fix changes where the rule lives rather than adding a line to six operators, which is a bigger change than the symptom warrants.
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server; `ExecuteOn`, `EvaluateOn` and `select ... on <link>` run work on it. Which username and password the link connects with comes from its *link users* - a per-user mapping, with a fallback - and six D4 operators change that mapping: `CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword` and `DropServerLinkUser` (all in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs`).

The credentials are read once, when the connection to the other server is opened, and the connection is then kept for the life of the session's process. None of the six operators ends it. So a session that has already used the link keeps reaching the other server as whoever it first connected as - including the session that performs the change, which is the one an administrator is most likely to be sitting in front of. A password rotation, a change of which account the link uses, and the removal of the mapping altogether all take effect only for sessions that connect afterwards.

`DropServerLinkUser` carries the original author's note about this: `// TODO: Prevent drop if user has active sessions on this server?`.

## Why this is filed now

`engine-server-link-session-outlives-drop` closed exactly this gap for the link's *other* settings: `drop server` and `alter server` now end the issuing process's connection, so its next use of the link connects with what the catalog currently says. It left the link users alone, because the behaviour it was fixing was about host name, port and session settings. The result is that half a rule holds:

> a catalog change that affects how a link connects ends the issuing process's connection to that link, so that its next use of the link connects afresh.

`alter server` honours it; the six link-user operators do not. This ticket is about making the rule hold, not about the six call sites.

## How it was reproduced

Verified 2026-11-21, Debug, one machine, during the review of `engine-server-link-session-outlives-drop`, in a throwaway case in the shape of `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs` (written, run, and removed rather than committed, because it would be a test for a defect this ticket has not fixed yet):

- Create a link naming a second Dataphor server in a child process, and use it once, so the connection opens as the fallback account (the other server's `Admin`, blank password).
- On the same process, run `CreateServerLinkUser('Admin', Name('<link>'), 'Admin', 'wrong')` - credentials the other server will refuse.
- Use the link again on that same process: it still works. The connection was not re-made and the new credentials were never read.
- Start a second process on the same session and use the link there: it fails, because that one does connect and is refused. So the credentials really did change in the catalog; only the already-open connection did not notice.

## What a fix has to settle

Stated as the shape of the problem, not as a design:

- **Where the rule lives.** Adding a `CatalogDeviceSession.CloseRemoteSession(...)` call to each of the six operators would work and is what the symptom asks for, but it is the sixth and seventh copy of a rule that will be forgotten by the eighth. Every one of those operators, and `alter server` too, finishes by calling `program.CatalogDeviceSession.UpdateCatalogObject(serverLink)` - and across the whole tree the only `Schema.ServerLink` arguments `UpdateCatalogObject` ever gets are those seven. So the promising shape is for `UpdateCatalogObject` itself to end the issuing process's connection when the object it is given is a server link, which would also make the explicit call in `AlterServerNode` redundant. What that has to settle: `UpdateCatalogObject` is the forward-DDL update path only (catalog loading uses `CacheCatalogObject`), but it is called for every kind of catalog object from about thirty sites, so the condition wants to be narrow and stated at that one site; and a server-link update that changes nothing about connecting would then also close the connection, which costs one reconnect and may be the right price for the rule holding.
- **Whether the removal of a mapping should behave differently.** With the mapping gone, a fresh connection falls back to the other server's `Admin` with a blank password (`Schema.ServerLink.GetUser`). So after `DropServerLinkUser` the rule makes the session connect as the fallback rather than keep the dropped account - a change of identity, not a refusal. Whether that is wanted, or whether the drop should instead refuse while a session holds a connection (the author's `// TODO`), is the one question here that is not simply "make the rule hold".
- **Other sessions.** `engine-server-link-session-outlives-drop` settled, with a recorded accepted tradeoff at `AlterServerNode.InternalExecute`, that a *different* process keeps its connection until it ends, because closing another process's connection makes a blocking network call on that process's channel. The same reasoning applies here and the same tradeoff should be recorded, not re-opened - unless this ticket is the one that decides a deployment needs an operator for closing another session's remote connection, which would be a new surface.

## Compatibility

The six operator signatures do not change; what changes is that the session issuing one of them stops holding its old connection to that link. That is a behaviour a D4 library could notice, so it needs an entry in `docs/migration-notes.md` alongside the one `engine-server-link-session-outlives-drop` added, and the characterization test belongs beside the cases in `tests/DAE.Server.Tests/ServerLinkConnectionTest.cs`.
