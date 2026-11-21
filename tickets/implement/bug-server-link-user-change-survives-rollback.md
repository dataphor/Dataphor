description: When a transaction that changed how one Dataphor server reaches another is rolled back, the change is undone in the saved catalog but not in the running server's memory, so from then on the server uses settings and credentials that its own catalog does not contain.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs, src/engine/DAE/Runtime.Instructions/SystemNodes.cs, src/engine/DAE/Runtime.Instructions/DDLNodes.cs, tests/DAE.Server.Tests/ServerLinkUserRollbackTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
difficulty: medium
----
## What goes wrong

A *server link* is a D4 object naming another Dataphor server. Its metadata says how to reach that server (host name, instance name, port, and whether to use a secure connection), and its *link users* say which username and password a given local user connects with.

When a transaction changes either of those and is then rolled back, the change is undone in the saved catalog but not in the running server's memory. The server and its own catalog disagree from then until it is restarted, and the server believes the version that was rolled back. Sessions that use the link after the rollback go to the host the rolled-back change named, or connect with the credential it set - a password that was never really set, an account that was never really pointed at, or, after a rolled-back drop of a link user, no mapping at all and therefore the link's fallback account.

The engine already has the machinery for this: a session that changes the catalog inside a transaction records an *undo instruction* for each in-memory change, and the rollback replays them backwards (`CatalogDeviceSession`, the `DDLInstruction` classes and `InternalRollbackTransaction`). The store write is undone separately, by the store's own transaction. These two code paths record no undo instruction for what they change in memory.

Two further consequences of the missing records, both fixed by the same change: the rollback concludes that no catalog change was made at all (`undidDDL` stays false), so it does not run `InvalidateCachesAfterUndo` and the server keeps whatever it cached from the catalog as the rolled-back transaction left it; and a nested rollback, which must undo only its own level, has nothing to undo either.

## Arm 1 - the six link user operators

`CreateServerLinkUser`, `CreateServerLinkUserWithEncryptedPassword`, `SetServerLinkUserID`, `SetServerLinkUserPassword`, `ChangeServerLinkUserPassword` and `DropServerLinkUser`, all in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` (lines 1716-1829), change `serverLink.Users` directly and then call `program.CatalogDeviceSession.UpdateCatalogObject(serverLink)`. `UpdateCatalogObject` writes the link to the store and closes this process's connection to it; it records nothing for undo, and neither do the operators.

## Arm 2 - alter server

`AlterServerNode.InternalExecute` in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` (lines 2652-2661) calls `CatalogDeviceSession.AlterMetaData`, which *does* record an undo instruction for the link's metadata, and then calls `server.ResetServerLink()` and `server.ApplyMetaData()`, which recompute the link's connection settings from that metadata. Nothing records the recomputed settings, and `AlterMetaDataInstruction.Undo` only puts the metadata object back - it does not recompute. So a rolled-back `alter server` leaves the link's metadata correct and its connection settings wrong, which is the worse of the two: the settings are what a connection is actually made from, and the catalog table `System.ServerLinks` reports them rather than the metadata.

Note the ordering trap when fixing this arm. Instructions are undone in reverse order of recording, so an instruction recorded *after* `AlterMetaData` is undone *before* the metadata is put back. An undo that recomputed the settings from the link's metadata would therefore recompute them from the metadata the rolled-back statement left. The undo must restore the recorded setting values directly, not re-derive them.

## How it was reproduced

A new in-process fixture, five cases, all five failing at `64cb42d0`. No second Dataphor server is needed: none of these operators connects, so a link naming a host that does not answer is enough. The file as it was run is below; it is the test this ticket should land (with the two pointless trailing assertions on `Name` removed - they were scaffolding).

Measured failures, in `dotnet test tests/DAE.Server.Tests --filter "FullyQualifiedName~ServerLinkUserRollbackTest"`:

| Case | Expected after the rollback | Observed |
|---|---|---|
| `TestRolledBackCreateLeavesNoUser` | `ServerLinkUserExists` false | true |
| `TestRolledBackDropKeepsTheUser` | `ServerLinkUserExists` true | false |
| `TestRolledBackSetUserIDKeepsTheOldUserID` | `UserID` `Remote` | `Other` |
| `TestRolledBackSetPasswordKeepsTheOldPassword` | password `secret` | `changed` |
| `TestRolledBackAlterKeepsTheOldHostName` | `HostName` `localhost` | `elsewhere` |

The other half of the claim - that the *store* really is rolled back, so the two sides disagree - was read rather than run: `ServerCatalogDeviceSession.UpdatePersistentObject` acquires the store connection with `AcquireCatalogStoreConnection(true)`, which calls `BeginStoreTransactions()`, and `InternalAfterRollbackTransaction` rolls that store transaction back. Showing it directly would need the instance to be stopped and reopened, which the assertions above do not need: "a rolled-back change leaves the running server as it was" is the behaviour being pinned, and the store agreeing follows.

## The shape to follow

The project fixed the same class of defect for object ownership in `bug-set-object-owner-rollback-keeps-owner` (completed; its ticket in `tickets/complete/` records the reasoning). Two differences to be aware of:

- **Order is the other way round.** That fix writes the store first and changes memory second, because the store write did not depend on memory. Here it cannot: the store write emits the link's own create script (`ServerLink.EmitStatement` through `ScriptPersistentObject`), so the in-memory change has to be made first for the store to be given the new text. So: record the undo, change memory, then `UpdateCatalogObject`. A store write that then fails leaves memory changed, but inside a transaction the recorded undo is exactly what puts it back, and outside a transaction nothing records anything today either.
- **Do not hold the catalog lock across `UpdateCatalogObject`.** The remarks on `UpdateCatalogObject` and on `CloseRemoteSession` say why: closing this process's connection to the link makes a network call to the other server. None of these callers holds `lock (Catalog)` today; keep it that way. `SetDeviceReconcileMode` and `SetDeviceReconcileMaster` at the end of the *Device* region of `CatalogDeviceSession.cs` are the closest existing pattern to copy - capture the original, change it, record the instruction under `#if LOGDDLINSTRUCTIONS` when `(!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction`.

Both arms call into `CatalogDeviceSession` from nodes in the engine assembly, so the new methods belong on `CatalogDeviceSession` itself (`src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`), public, like `AlterMetaData` - not on `ServerCatalogDeviceSession`, which the engine's nodes cannot see.

Suggested surface, a new `#region Server link` after the existing `#region Device user`:

```csharp
public void CreateServerLinkUser(Schema.ServerLink serverLink, Schema.ServerLinkUser serverLinkUser);
public void DropServerLinkUser(Schema.ServerLink serverLink, string userID);
public void SetServerLinkUserID(Schema.ServerLink serverLink, string userID, string serverLinkUserID);
public void SetServerLinkUserPassword(Schema.ServerLink serverLink, string userID, string serverLinkPassword);
public void ApplyServerLinkMetaData(Schema.ServerLink serverLink);   // arm 2: records the settings, then ResetServerLink + ApplyMetaData
```

with one `DDLInstruction` each, beside the others in the *Instructions* region. The undo of a drop re-adds the `ServerLinkUser` instance that was removed - it keeps its `UserID` and its back-reference to the link, so `ServerLinkUsers.Add(user)` restores it exactly. For `ApplyServerLinkMetaData`, record the seven values `ApplyMetaData` derives (`HostName`, `InstanceName`, `OverridePortNumber`, `UseSecureConnection`, `OverrideListenerPortNumber`, `UseSecureListenerConnection`, `UseSessionInfo`) and put them back; reading and writing the existing public properties of `Schema.ServerLink` adds no new compatibility surface, which a settings type on `ServerLink` would.

Whether each operator calls `UpdateCatalogObject` itself afterwards, as the nodes do now, or the new methods do it, is the implementer's call - but keep the existing order (memory, then store, then the connection close) and the existing comments at `UpdateCatalogObject` that explain why the close comes last.

## A small wart at the same site, to fix along the way

`SetServerLinkUserID`, `SetServerLinkUserPassword` and `ChangeServerLinkUserPassword` index `serverLink.Users[userID]` with no presence check. `ServerLinkUsers` derives from `Dictionary`, so a user id that has no mapping gets a bare `KeyNotFoundException` out of a D4 operator rather than the engine's own error. The engine already has the right error for this condition, `SchemaException.Codes.ServerLinkUserNotFound` (113151), and `DropServerLinkUser` already raises it. Raise it in these three as well: no new error code, no change to any operator signature, and the three lines sit inside the code this ticket is rewriting anyway. It is still a change an application could notice, so it gets a line in the migration note. If this looks like more than a wart once you are in the code, leave it and say so in the handoff rather than widening the ticket.

## The reproduction, as run

Add as `tests/DAE.Server.Tests/ServerLinkUserRollbackTest.cs`, with a `<Compile Include="..." />` entry in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` after `ServerLinkConnectionTest.cs` - default compile items are off in this repository, and a file not listed is silently not compiled (that is what happened on the first run of this reproduction: the tests built and the filter matched nothing). Tabs and CRLF, like its neighbours. Add the fixture's own class comment saying what it pins; the version below has none.

```csharp
/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	[TestFixture]
	public class ServerLinkUserRollbackTest : InProcessTestFixture
	{
		[Test]
		public void TestRolledBackCreateLeavesNoUser()
		{
			ExecuteScript("create server CreateRollbackLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("CreateServerLinkUser('Admin', Name('CreateRollbackLink'), 'Remote', 'secret');");
			Process.RollbackTransaction();

			Assert.That(Evaluate("ServerLinkUserExists('Admin', Name('CreateRollbackLink'))"), Is.False);
		}

		[Test]
		public void TestRolledBackDropKeepsTheUser()
		{
			ExecuteScript("create server DropRollbackLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");
			ExecuteScript("CreateServerLinkUser('Admin', Name('DropRollbackLink'), 'Remote', 'secret');");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("DropServerLinkUser('Admin', Name('DropRollbackLink'));");
			Process.RollbackTransaction();

			Assert.That(Evaluate("ServerLinkUserExists('Admin', Name('DropRollbackLink'))"), Is.True);
		}

		[Test]
		public void TestRolledBackSetUserIDKeepsTheOldUserID()
		{
			ExecuteScript("create server SetIDRollbackLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");
			ExecuteScript("CreateServerLinkUser('Admin', Name('SetIDRollbackLink'), 'Remote', 'secret');");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("SetServerLinkUserID('Admin', Name('SetIDRollbackLink'), 'Other');");
			Process.RollbackTransaction();

			Assert.That(LinkUserID("SetIDRollbackLink"), Is.EqualTo("Remote"));
		}

		[Test]
		public void TestRolledBackSetPasswordKeepsTheOldPassword()
		{
			ExecuteScript("create server SetPasswordRollbackLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");
			ExecuteScript("CreateServerLinkUser('Admin', Name('SetPasswordRollbackLink'), 'Remote', 'secret');");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("SetServerLinkUserPassword('Admin', Name('SetPasswordRollbackLink'), 'changed');");
			Process.RollbackTransaction();

			Assert.That(LinkPassword("SetPasswordRollbackLink", "Admin"), Is.EqualTo("secret"));
		}

		[Test]
		public void TestRolledBackAlterKeepsTheOldHostName()
		{
			ExecuteScript("create server AlterRollbackLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("alter server AlterRollbackLink alter tags { alter HostName = \"elsewhere\" };");
			Process.RollbackTransaction();

			Assert.That(Evaluate("ServerLinks[LibraryName() + '.AlterRollbackLink'].HostName"), Is.EqualTo("localhost"));
		}

		/// <summary>The account the link connects a local user with, from the catalog table rather than from the in-memory object.</summary>
		private string LinkUserID(string linkName)
		{
			return (string)Evaluate(String.Format("(ServerLinkUsers where ServerLink_Name = LibraryName() + '.{0}')[].UserID", linkName));
		}

		/// <summary>Read from the in-memory link: System.ServerLinkUsers deliberately does not expose the password, and nothing else can be asked for it without connecting.</summary>
		private string LinkPassword(string linkName, string userID)
		{
			Schema.ServerLink link = null;
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
				if ((objectValue is Schema.ServerLink) && Schema.Object.Unqualify(objectValue.Name) == linkName)
					link = (Schema.ServerLink)objectValue;
			return SecurityUtility.DecryptPassword(link.Users[userID].ServerLinkPassword);
		}

		private object Evaluate(string expression)
		{
			return ((ServerProcess)Process).Evaluate(expression, null).AsNative;
		}
	}
}
```

If the wart above is fixed, add a sixth case for it: `SetServerLinkUserID` for a user id with no mapping raises error 113151 rather than a `KeyNotFoundException`.

Consider one nested-rollback case as well, as `bug-set-object-owner-rollback-keeps-owner` did: the instruction log is replayed only back to the level's `BeginTransactionInstruction`, and a case that rolls back an inner level and checks the outer level's change survived is what pins that.

## Known gaps, for the handoff rather than for this ticket

A connection to the link *opened inside* the rolled-back transaction, with the credential the transaction set, is kept after the rollback: the deferred close is discarded on rollback (`_deferredRemoteSessionCloses.Rollback`), and nothing closes the connection the rolled-back credential opened. The undo could ask for a close, but it would be asking during the rollback, which is the one moment the deferred-close machinery is draining; it is a separate decision. State it in the handoff; if the implementation turns out to make it a one-liner, take it, and say so.

## TODO

- Add the four link user instructions and the settings instruction to the *Instructions* region of `src/engine/DAE/Device.Catalog/CatalogDeviceSession.cs`, inside the existing `#if LOGDDLINSTRUCTIONS`.
- Add the `#region Server link` methods after `#region Device user` in the same file, each recording its instruction under `(!ServerProcess.InLoadingContext()) && ServerProcess.InTransaction`, and none of them holding `lock (Catalog)` across `UpdateCatalogObject`.
- Point the six operators in `src/engine/DAE/Runtime.Instructions/SystemNodes.cs` at the new methods, keeping `ChangeServerLinkUserPassword`'s old-password check and `DropServerLinkUser`'s not-found error and its accepted-tradeoff `NOTE:` where they are.
- Point `AlterServerNode` in `src/engine/DAE/Runtime.Instructions/DDLNodes.cs` at `ApplyServerLinkMetaData` in place of its `ResetServerLink()` / `ApplyMetaData()` pair, and leave the comment there about why the connection close comes after.
- Raise `ServerLinkUserNotFound` (113151) for a missing mapping in the three operators that index `Users` without checking, or say in the handoff why not.
- Add `tests/DAE.Server.Tests/ServerLinkUserRollbackTest.cs` and its `<Compile Include="..." />` entry; confirm all five cases fail before the fix and pass after.
- Add the nested-rollback case, and the not-found case if that change is made.
- Add one row to `docs/migration-notes.md` under *D4, catalog and libraries*: a rolled-back `alter server` or link user change is now undone in the running server as well as in the saved catalog, where before the running server kept the rolled-back version until it was restarted; and, if it is made, the not-found error change. Row 29 of that section, from `bug-server-link-user-change-keeps-old-connection`, is the neighbouring note - read it first and do not contradict it.
- Preserve each edited file's tabs, CRLF endings and byte-order-mark state. Checked: `CatalogDeviceSession.cs` has a UTF-8 byte-order mark, `SystemNodes.cs` and `DDLNodes.cs` do not, and `ServerLinkConnectionTest.cs` next to the new test file does.
- `dotnet build Dataphor.slnx`, then the three part commands under *Agents run the suite in parts* in the root `AGENTS.md`, each as its own foreground command. Do not run `dotnet test Dataphor.slnx` as one command.
