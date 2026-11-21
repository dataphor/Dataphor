description: A query that names another Dataphor server opens a connection to it and keeps it for the rest of the session; if the definition of that other server is deleted and a new one is created under the same name, the old connection is used instead of a new one, so the new definition is quietly ignored.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/ServerLink.d4, tests/DAE.Server.Tests/coverage-manifest.json
repro: verified
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server; `ExecuteOn`, `EvaluateOn` and `select … on <link>` run work on it. The first such statement opens a connection to the other server and the engine keeps it for the life of the process that opened it, so the second and later statements do not pay for a connection again. That cache is correct as far as it goes. Two things about it are not:

- **Nothing closes a cached connection when its link is dropped.** `drop server <name>` deletes the catalog object and nothing else. `ServerProcess.RemoteDisconnect(Schema.ServerLink)` exists for exactly this and **has no caller anywhere in the tree**; `DropServerNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`) carries the original author's `// TODO: Prevent dropping when server sessions are active`, so the gap was known and left open.
- **The cache is keyed by the link's name, not the link.** `RemoteSessions.IndexOf(Schema.ServerLink)` (`src/engine/DAE/Server/RemoteSession.cs`) compares with `Schema.Object.Equals`, which compares names. So a link created after a drop, under a name the dropped link used, *matches* the dropped link's cached connection.

Together: create a link, use it, drop it, create a different link under the same name, use it — and the work goes to the first link's server, on the first link's connection, with the first link's credentials and session settings. The new link's host name, instance name, port, link users and session-info tags are all ignored, silently, for as long as that process lives.

## How it was found, and what it looks like

The `ServerLink` coverage script (`tests/libraries/TestFramework.Coverage.Scripts/Documents/ServerLink.d4`) runs for the first time as of `test-client-server`, which gave it the second Dataphor server it has always needed. 125 of its 126 statements pass. The one that fails is at line 34:

```
// Server Link With Use Session Info
create server Test tags { HostName = "localhost", InstanceName = "Dataphor", UseSessionInfo = "true" };
SetMaxCallDepth(128);
… EvaluateOn('Test', …) …
drop server Test;

// Server Link With Session Info
create server Test tags { HostName = "localhost", InstanceName = "Dataphor", DefaultMaxCallDepth = "128" };
if (EvaluateOn('Test', 'select Processes[ProcessID()].MaxNestingLevel') as Integer) <> 128 then
	raise Error("Server link with Session Info failed");
```

The second link asks for a remote session whose maximum call depth is 128. Measured on one process against a child server (2026-11-21): the remote value is **100**, the product default, because the connection used is the first link's, which asked for no such thing. The same statement **under a link name no earlier link used returns 128**, so the tag itself is read and sent correctly — the defect is entirely the reuse. `Count(RemoteSessions where Process_ID = ProcessID())` is 1 at that point, which is the dropped link's session still being there.

`repro: verified` — run the coverage case: `dotnet test tests/DAE.Server.Tests --filter Name=ServerLink`. Its manifest entry is `known-failure` against this slug on the substring `Server link with Session Info failed`; when this is fixed the entry becomes `"status": "pass"` with `ticket`, `error` and `note` dropped, in the same commit.

## Why this is its own ticket rather than a line added to the drop

Three questions have to be answered together, and each is a decision about what the product does rather than a correction:

- **Whose connections does a drop close?** The cache is per `ServerProcess`. A link in use by twenty sessions has twenty cached connections, and the one dropping it can reach only its own. Closing only the dropper's leaves the other nineteen wrong in exactly the way above. Closing all of them means reaching across sessions from a DDL statement, which nothing in the catalog layer does for this kind of object today.
- **What happens to a connection inside a transaction?** A cached connection may be a participant in the dropping process's transaction (`ServerProcess` enlists remote sessions in its two-phase commit). Dropping the link out from under an open distributed transaction is not obviously allowed, and the author's `TODO` proposes the other answer: refuse the drop while sessions are active. Either answer is defensible; they are not the same product.
- **Should the cache be keyed by identity instead of name?** Comparing by the link's object id would stop the wrong reuse on its own, because a re-created link gets a new id — rung 1 of the architecture ladder, making the bad state unrepresentable rather than catching one instance of it. But it would then *leak* the dropped link's connection instead of reusing it, and nothing on the far side reclaims a native session (`bug-native-cli-sessions-never-reclaimed`), so that half has to land with a close rather than instead of one.

## What a fix has to settle

- Which of the two shapes the product takes: a drop that closes the link's remote sessions, or a drop that is refused while any are open. If the first, across which sessions; if the second, what the error says and how an administrator gets out of it.
- Whether `RemoteSessions` stops matching by name. If it does, every other name-based lookup of a schema object in that neighbourhood is worth the same look.
- Whether `alter server` needs the same treatment. `AlterServerNode.InternalExecute` carries its own `// TODO: Prevent altering the server link with active sessions?` and calls `ResetServerLink()`/`ApplyMetaData()` on the schema object, which cached connections do not re-read — so an altered link also goes on using the settings it was created with until the process ends. The `ServerLink` script's "Alter Server Link" block passes today only because the altered link happens to end up pointing where the stale connection already pointed.
- What `RemoteDisconnect` is for once this is settled: either it gains the caller it was written for, or it goes.

## Compatibility

Closing a remote session on a drop, or refusing the drop, is a change a D4 library could notice, so whatever lands goes in `docs/migration-notes.md`. Nothing here changes an operator signature, a class name, a file format or an error code; a refusal would need a new error code.
