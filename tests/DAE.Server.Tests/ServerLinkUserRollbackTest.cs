/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What a rolled-back change to a server link leaves in the running server: a rollback undoes a link user change
	/// (create, drop, user id, password) and an <c>alter server</c> in memory as well as in the saved catalog, so that
	/// the running server never goes on using settings or credentials its own catalog does not hold.
	/// </summary>
	/// <remarks>
	/// No second Dataphor server is needed: none of these operators connects, so a link naming a host that does not
	/// answer is enough. The nested case pins that a rollback undoes the changes of its own level only.
	/// </remarks>
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
			Assert.That(LinkUserID("DropRollbackLink"), Is.EqualTo("Remote"));
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

		/// <summary>
		/// A statement that fails part-way leaves the link as it was. <c>ApplyMetaData</c> throws on a tag value it cannot
		/// convert, after <c>ResetServerLink</c> has already cleared the settings, so the settings the statement replaces have
		/// to be recorded before it runs; the implicit transaction every statement runs in then undoes the half-applied change.
		/// </summary>
		[Test]
		public void TestFailedAlterKeepsTheOldSettings()
		{
			ExecuteScript("create server FailedAlterLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");

			Assert.Catch(() => ExecuteScript("alter server FailedAlterLink alter tags { alter OverridePortNumber = \"not a number\" };"));

			Assert.That(Evaluate("ServerLinks[LibraryName() + '.FailedAlterLink'].OverridePortNumber"), Is.EqualTo(1));
		}

		/// <summary>A rollback undoes the changes of its own level only: the outer level's change is still there afterwards.</summary>
		[Test]
		public void TestNestedRollbackKeepsTheOuterChange()
		{
			ExecuteScript("create server NestedRollbackLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("CreateServerLinkUser('Admin', Name('NestedRollbackLink'), 'Outer', 'secret');");

				Process.BeginTransaction(IsolationLevel.Isolated);
				ExecuteScript("SetServerLinkUserID('Admin', Name('NestedRollbackLink'), 'Inner');");
				Process.RollbackTransaction();

				Assert.That(LinkUserID("NestedRollbackLink"), Is.EqualTo("Outer"));
			}
			finally
			{
				Process.RollbackTransaction();
			}

			Assert.That(Evaluate("ServerLinkUserExists('Admin', Name('NestedRollbackLink'))"), Is.False);
		}

		/// <summary>A user id with no mapping raises the engine's own error rather than the dictionary's KeyNotFoundException.</summary>
		[Test]
		public void TestSetUserIDForAnUnmappedUserRaisesNotFound()
		{
			ExecuteScript("create server NotFoundLink tags { HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" };");

			Exception exception = Assert.Catch(() => ExecuteScript("SetServerLinkUserID('Admin', Name('NotFoundLink'), 'Other');"));
			Assert.That(FindError(exception, (int)SchemaException.Codes.ServerLinkUserNotFound), Is.Not.Null, exception.ToString());
		}

		/// <summary>The engine wraps an operator's error in the statement and script errors around it, so the code is looked for down the chain.</summary>
		private static DataphorException FindError(Exception exception, int code)
		{
			for (; exception != null; exception = exception.InnerException)
				if (exception is DataphorException dataphorException && dataphorException.Code == code)
					return dataphorException;
			return null;
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
