description: Review of the fix that stops a query on another Dataphor server from reusing a connection that belonged to a deleted server link, and that now closes the connection when the link is dropped or altered.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Server/RemoteSession.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/Server/Server/RemoteSessionImplementation.cs, src/protocol/NativeCLI/NativeCLISession.cs, tests/DAE.Server.Tests/ServerLinkConnectionTest.cs, tests/DAE.Server.Tests/coverage-manifest.json, docs/migration-notes.md, docs/modernization-plan.md
----
## What landed

Four changes, in the order they matter.

**The cache of connections to other servers is keyed by the link, not by its name.** `RemoteSessions.IndexOf(Schema.ServerLink)` (`src/engine/DAE/Server/RemoteSession.cs`) compared with `Schema.Object.Equals`, which compares names, so a link created after a drop under the dropped link's name matched the dropped link's connection: the work went to the first link's server, on the first link's credentials, and the new link's host name, instance name, port, link users and session-info tags were ignored for the life of the process. It now compares `ServerLink.ID`. The comment at the site says why identity and not name, and why `bug-object-id-counter-is-process-wide` does not reach it.

**`drop server` and `alter server` close the issuing process's connection to the link.** `DropServerNode.InternalExecute` and `AlterServerNode.InternalExecute` (`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`) call the new `CatalogDeviceSession.CloseRemoteSession`, which gives `ServerProcess.RemoteDisconnect` the caller it was written for and had never had. The drop's call is after `CheckNotSystem`/`CheckNoDependents`, so a refused drop closes nothing, and outside `lock (program.Catalog)`, because the close makes a network call. The alter's is after its metadata is applied, for the same reason. Both of the original author's `// TODO`s at those sites are gone; the alter's is replaced by an accepted-tradeoff `NOTE:`.

**Inside an open transaction the close waits for the outermost commit**, and a rollback discards it, mirroring a dropped device's deferred stop - because the process enlists its remote sessions in its own commits and rollbacks, so closing at once would roll remote work back under a transaction that is still running. The level bookkeeping the device stops already had was factored into one generic nested type, `DeferredTeardowns<T>`, now used by both lists (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`). **This is the one place the implementation did more than the ticket asked**: the ticket said "a deferred-close list beside `_deferredDeviceStops`", and a second independent copy of that bookkeeping was the alternative. The reason for sharing is in the type's own remarks - that level logic has already needed a fix of its own, `bug-catalog-deferred-device-stops-ignore-nesting`. `DeferredDeviceStopTest`'s four cases still pass unchanged, which is the evidence that the device behaviour did not move. Weigh it.

**A connection's teardown completes whatever the network does.** `NativeCLISession.Dispose` (`src/protocol/NativeCLI/NativeCLISession.cs`) clears its session handle and disposes its WCF client in a `finally` around `StopSession`, and `RemoteSessionImplementation.Dispose` (`src/engine/Server/Server/RemoteSessionImplementation.cs`) catches a failed remote stop and logs one warning naming the link and the far instance. Before this, a failed `StopSession` left the handle set, the client undisposed - leaking the WCF channel and its cached channel factory - and the entry in the process's list, so `ServerProcess.Dispose` could throw out of a process teardown.

## How to check it

`tests/DAE.Server.Tests/ServerLinkConnectionTest.cs` is new: an in-process server, a second Dataphor server in a child process (`OutOfProcessInstance`, no listener - each link names the child by `HostName` and `OverridePortNumber`), six cases. Each was verified to fail when its own fix is removed, which is the evidence that each one pins something rather than passing by construction:

| Case | What it pins | Fails without |
|---|---|---|
| `TestLinkRecreatedUnderTheSameNameConnectsAfresh` | the defect itself: the link is dropped and re-created from a *second* process, so no close is involved, and the first process's next use of it must see the new link's `DefaultMaxCallDepth` - 128, not the product default 100 | keying by object id |
| `TestDropClosesTheDroppingProcessConnection` | after a drop, `Count(RemoteSessions where Process_ID = ProcessID())` is 0 | the close in `DropServerNode` |
| `TestDropInTransactionDefersTheCloseToTheCommit` | the connection is still there after the drop inside a transaction, and gone after the commit | the deferral |
| `TestRolledBackDropKeepsTheConnection` | a rolled-back drop leaves the link and its connection both in place | the deferral |
| `TestAlterClosesTheAlteringProcessConnection` | after an alter adding `DefaultMaxCallDepth = "128"`, the next use of the link reports 128 | the close in `AlterServerNode` |
| `TestStoppingAProcessWhoseOtherServerHasGoneDoesNotThrow` | a process whose far server has been stopped can still be stopped | the catch in `RemoteSessionImplementation.Dispose` |

Run them with `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ServerLinkConnectionTest"` - about 8 seconds, most of it the last case starting and stopping a child server of its own.

The `ServerLink` coverage script is re-pointed in `tests/DAE.Server.Tests/coverage-manifest.json`: still `known-failure`, now owned by `bug-right-check-fails-for-granted-right`, matching on the substring `does not have access to right "System.Name`. At HEAD it failed at line 34 on the session reuse; it now runs past that and stops at line 43, where an ordinary user is refused a right the catalog grants. Everything after line 43 in that script has still never run, so a green `ServerLink` is not close.

Build and the whole suite, in the three parts the root `AGENTS.md` names: 0 errors; 443 + 13 passed, then 67 passed and 11 skipped, then 48 passed and 14 skipped. No failures, nothing newly skipped, and no pre-existing failure was met, so `tickets/.pre-existing-error.md` was not written.

## Known gaps, honestly

- **The `NativeSessionCLIClient` disposal is the one arm with no test** - the WCF channel and channel-factory leak. The suite cannot observe a closed channel without reaching into WCF state, and the ticket said as much. It is exercised, not asserted.
- **A dead connection still wedges the whole process, and that is now filed.** While testing the teardown arm it turned out that `drop server` cannot be reached at all once the far server has gone: `ServerProcess.InternalBeginTransaction` begins a transaction on every connection the process holds, so every statement on that process - `select 1 + 1` included - fails at its transaction begin with the socket error, and the only recovery is to disconnect the session. Verified, and filed as `tickets/plan/bug-dead-remote-session-blocks-every-statement.md` with the reproduction. The case that would have proved `drop server` works in that situation was written, run and removed rather than committed, because it cannot pass until that ticket lands. A reviewer should decide whether this ticket's "reclaiming a connection must never fail the statement that reclaims it" is honestly met: for the teardown path it is, for the `drop server` path it is not, and the reason sits outside this ticket's site.
- **Another process keeps its connection to a dropped or altered link** until that process ends. Deliberate, and the ticket settled it: for a drop it is only delayed reclamation, because a re-created link has a new object id and nobody can reuse the dropped link's connection for new work. For an **alter** it is a behaviour an operator can notice, and it is recorded as an accepted-tradeoff `NOTE:` at `AlterServerNode.InternalExecute` and in the migration note, with its revisit condition. Do not re-file it unless that condition has tripped.
- **`RemoteSessions` is read across processes without a lock** - the `NOTE:` already at `CatalogDevice.PopulateRemoteSessions` - and this change makes writes to those lists more frequent, since a drop or alter now removes an entry where before nothing did. Still a pre-existing concern parked at its own site rather than a new one, and nothing in the suite reads `System.RemoteSessions` concurrently.
- **`ServerProcess.RemoteSessions` is a lazily created list**, so a `drop server` on a process that never used a link now allocates an empty one. Trivial, and `RemoteDisconnect` already did the same.

## Where to look hardest

- `DeferredTeardowns<T>.Commit` and `.Rollback` against the two methods they replaced: the level handoff on a nested commit, the `>= level` comparison, and that the list goes back to null are each load-bearing, and `bug-catalog-deferred-device-stops-ignore-nesting` is the ticket that got them right the first time.
- The ordering in `InternalCommitTransaction`: the remote closes run last, after the device stops and after the store commit. The comment says why; check it against what `ClearChangedRights` and `InternalAfterCommitTransaction` assume.
- Whether `InternalCloseRemoteSession`'s `Contains` guard is in the right place. It is there so that `ServerException.Codes.NoRemoteSessionForServerLink` (106120) keeps its meaning - raised only by a caller that asks to disconnect a link it has no session for - rather than being raised by every drop of a link the process never used.
- The empty `catch { }` in `ReportFailedRemoteStop`. It is deliberate - a logging failure inside the catch block that exists to keep a teardown from failing would defeat the point - and it is commented, but it is the kind of thing worth a second opinion.

## Compatibility

`drop server` and `alter server` now end the issuing session's connection to that link, which a D4 library could notice; the entry is in `docs/migration-notes.md` under *D4, catalog and libraries*, and the defect is K35 in the plan's *Known defects* table. No operator signature, D4 class name, assembly name, file format, error code, WCF contract shape or port changed.
