description: A query that names another Dataphor server opens a connection to it and keeps it for the rest of the session; if the definition of that other server is deleted and a new one is created under the same name, the old connection is used instead of a new one, so the new definition is quietly ignored. Fix the reuse, and close the connection when the definition is dropped or altered.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Server/ServerProcess.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, src/protocol/NativeCLI/NativeCLISession.cs, tests/DAE.Server.Tests/coverage-manifest.json, tests/DAE.Server.Tests/OutOfProcessInstance.cs, docs/migration-notes.md
difficulty: medium
repro: verified
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server; `ExecuteOn`, `EvaluateOn` and `select … on <link>` run work on it. The first such statement opens a connection to the other server and the engine keeps it for the life of the `ServerProcess` that opened it, so later statements do not pay for a connection again. Two things about that cache are wrong.

**The cache is keyed by the link's name, not the link.** `RemoteSessions.IndexOf(Schema.ServerLink)` (`src/engine/DAE/Server/RemoteSession.cs`) compares with `Schema.Object.Equals`, which compares names. A link created after a drop, under a name the dropped link used, therefore *matches* the dropped link's cached connection: the work goes to the first link's server, on the first link's connection, with the first link's credentials and session settings, and the new link's host name, instance name, port, link users and session-info tags are silently ignored for as long as that process lives.

**Nothing closes a cached connection when its link is dropped or altered.** `drop server <name>` deletes the catalog object and nothing else; `ServerProcess.RemoteDisconnect(Schema.ServerLink)` exists for exactly this and has no caller anywhere in the tree. `alter server` calls `ResetServerLink()`/`ApplyMetaData()` on the schema object, which a cached connection never re-reads, so an altered link goes on using the settings it was created with. Both `DropServerNode` and `AlterServerNode` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`) carry the original author's `// TODO` about active sessions, so the gap was known and left open.

## What the fix stage settled

The fix-stage ticket asked three questions. All three are answered below, from the product's own behaviour rather than from taste.

**A drop closes connections; it does not refuse.** The author's `// TODO: Prevent dropping when server sessions are active` is a note to himself, not the shipped intent: the `ServerLink` coverage script drops a link immediately after using it, seventeen times, and this project's rule is that existing behaviour is the specification. A refusal would also need a new error code and would leave an administrator with no way to clear another session's cached connection, there being no operator for that. So: close.

**The drop closes the dropping process's connection, and only that one.** `drop device`, the closest precedent, closes every process's device sessions (`CatalogDeviceSession.InternalStopDevice` disposes `device.Sessions`, a list that lives on the schema object), but a remote session is not a local object: disposing one makes a blocking network call (`NativeCLISession.Dispose` → `StopSession`) on another process's live channel, and a far server that is down would make the drop wait for a socket timeout per session. Once the cache is keyed by identity, that reach buys nothing for correctness — a re-created link has a new object id, so no other process can reuse the dropped link's connection for new work — it only reclaims sooner. Measured: keying by id alone makes the failing assertion pass (below), with no change to the drop at all.

**A drop inside an open transaction defers its close to the outermost commit**, exactly as a dropped device defers its stop (`CatalogDeviceSession`, the `DeferredDeviceStop` region). The dropping process's remote session may be a participant in that transaction (`ServerProcess` enlists remote sessions in its commit and rollback), so closing it at once would roll the remote work back under a transaction that is still running, and the eventual commit would prepare a session that no longer exists. A rollback discards the pending close, because it undoes the drop.

**`RemoteSessions` stops matching by name, and nothing else in the neighbourhood needs the same look.** Checked: `Schema.DeviceSessions.IndexOf(Device)` already compares by reference, so the device cache was never name-keyed; `RemoteSessions` was the only session cache that was.

**`RemoteDisconnect` gains the caller it was written for** rather than being deleted.

## How it was measured

At HEAD, `dotnet test tests/DAE.Server.Tests --filter Name=ServerLink` fails at line 34 of the script exactly as the manifest records: the second link asks for a remote session whose maximum call depth is 128, and the remote value is 100, the product default, because the connection used is the first link's.

With `RemoteSessions.IndexOf` changed to compare `ServerLink.ID` and **nothing else changed** (measured 2026-11-21, Debug, one machine), line 34 passes and the script runs on to line 43, where it fails for an unrelated reason that is now its own ticket, `bug-right-check-fails-for-granted-right`. Adding the close on drop, inside or outside `lock (program.Catalog)`, changed neither result. So the two arms below are separable: the first is the correctness fix, the second is reclamation.

## Reclaiming a connection must never fail the statement that reclaims it

Today a remote session whose far server is unreachable cannot be disposed cleanly: `NativeCLISession.Dispose` calls `StopSession` over the wire, and when that throws it leaves `_sessionHandle` set, never disposes its `_nativeCLI` client, and never reaches `RemoteSession.Dispose` — so the `Disposed` event that removes the entry from `RemoteSessions` never fires. Two consequences, one of them already live: a `drop server` that closed its connection would fail whenever the far server was down, and `ServerProcess.Dispose` → `CloseRemoteSessions()` can already throw out of a process teardown for the same reason (static: read from the code, not reproduced). Both are fixed by making the teardown complete whatever the network does — `NativeCLISession.Dispose` clearing its handle and disposing its client in a `finally`, and `RemoteSessionImplementation.Dispose` catching a failed remote stop and logging it as a warning through `ServerProcess.ServerSession.Server.LogMessage`, as `ReportUnhonouredSecuritySettings` in that same file already does.

`NativeCLISession.Dispose` not disposing its `NativeSessionCLIClient` is a leak in its own right — `ServiceClient<T>.Dispose` is what closes the WCF channel and releases the cached channel factory — and it sits on the path this ticket makes frequent, so it is fixed here (met along the way) rather than filed. It carries no test of its own: observing a closed channel is not something the suite can assert without reaching into WCF state.

## What stays as it is, deliberately

After a drop or an alter, a *different* process that already holds a connection to that link keeps it until that process ends. For a drop that is only delayed reclamation — the link is gone and the connection cannot be reused for new work. For an **alter** it is a behaviour an operator can notice: sessions that already connected go on using the settings the link was created with, which is what the author's `// TODO: Prevent altering the server link with active sessions? (May not be necessary, the active sessions would just continue to use the current settings)` contemplated. Record that at the site as a `NOTE:` with its revisit condition (a deployment that needs an alter to take effect everywhere at once, which would need an operator for closing another session's remote connection), and say it in the migration note.

## Compatibility

`drop server` and `alter server` now end the issuing session's connection to that link, which a D4 library could notice: an entry in `docs/migration-notes.md` under *D4, catalog and libraries*. No operator signature, class name, file format, error code, contract shape or port changes. `ServerException.Codes.NoRemoteSessionForServerLink` (106120) keeps its meaning: the drop calls `RemoteDisconnect` only when `RemoteSessions.Contains(link)`, so that error is still raised only by a caller that asks to disconnect a link it has no session for.

## The coverage manifest

`ServerLink` cannot flip to `"status": "pass"` in this commit: the script now aborts at line 43 on `bug-right-check-fails-for-granted-right`. Re-point its entry instead — same `known-failure` status, `ticket` becomes `bug-right-check-fails-for-granted-right`, `error` becomes a substring of the new failure that carries no object id (`does not have access to right "System.Name` is the shortest honest one), and the `note` says in one line that the session-reuse half is fixed and what remains. Keep `linkedServer: true`.

## TODO

- Change `RemoteSessions.IndexOf(Schema.ServerLink)` in `src/engine/DAE/Server/RemoteSession.cs` to match on `ServerLink.ID` rather than `Schema.Object.Equals`. Leave a one-line comment saying why identity and not name. (Object ids are unique within one engine's catalog and a re-created link gets a new one; `bug-object-id-counter-is-process-wide` does not reach this, because every link in a `RemoteSessions` list comes from the catalog of the one engine that owns the process.)
- Make a remote session's teardown complete whatever the network does: `NativeCLISession.Dispose` (`src/protocol/NativeCLI/NativeCLISession.cs`) clears `_sessionHandle` and disposes `_nativeCLI` in a `finally` around `StopSession`; `RemoteSessionImplementation.Dispose` (`src/engine/Server/Server/RemoteSessionImplementation.cs`) catches a failed remote stop, logs one warning naming the link and the far instance, and finishes its own disposal.
- Give `DropServerNode.InternalExecute` the close: after `CheckNotSystem` and `CheckNoDependents` (a refused drop must close nothing), outside `lock (program.Catalog)` (the close makes a network call, and the catalog lock is never held across a wait — the same rule the right-cache work followed for store writes), and only when the dropping process has a session for the link. Delete the `// TODO: Prevent dropping when server sessions are active`; leave the neighbouring `// TODO: Drop server link users` alone.
- Defer the close when the dropping process is in a transaction, mirroring the device stop: a deferred-close list on `CatalogDeviceSession` beside `_deferredDeviceStops`, replayed at the outermost commit after the store commit and discarded for the level that rolls back. Keep the two mechanisms adjacent so the next reader sees them as one pattern.
- Give `AlterServerNode.InternalExecute` the same close, through the same path, so the next use of the link reconnects with the new settings. Replace its `// TODO: Prevent altering the server link with active sessions?` with the `NOTE:` described under *What stays as it is*.
- Add the reproduction: a fixture that starts a child server with `new OutOfProcessInstance(<instance name>)` (no listener needed — give the link `HostName` and `OverridePortNumber` tags pointing at the child's `HostName` and `PortNumber`), then, on an in-process server, creates a link, uses it, drops it, creates a second link under the same name with `DefaultMaxCallDepth = "128"`, and asserts `EvaluateOn('<name>', 'select Processes[ProcessID()].MaxNestingLevel')` is 128. That is the lowest layer that shows the defect: `RemoteSessions` is internal to `DAE` and the test assembly has no `InternalsVisibleTo`, so a unit test of `IndexOf` is not available and is not wanted.
- Add a case in the same fixture for the drop closing the connection: after the drop, `Count(RemoteSessions where Process_ID = ProcessID())` is 0 for a session that had used the link.
- Add a case for the deferred close: inside an explicit transaction, drop the link, confirm the connection is still there, then commit and confirm it is gone; and the rollback arm, where the link and its connection both survive.
- Re-point the `ServerLink` entry in `tests/DAE.Server.Tests/coverage-manifest.json` as described above, in this commit.
- Add the `docs/migration-notes.md` entry under *D4, catalog and libraries*, and a row in the *Known defects* table of `docs/modernization-plan.md` (next free K number), as the recent engine-defect tickets did.
- Build, then run the suite in the three parts the root `AGENTS.md` names, each as its own foreground command.
