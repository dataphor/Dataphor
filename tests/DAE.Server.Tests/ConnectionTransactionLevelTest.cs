/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What transaction levels a connection opened part-way through a session's transactions is given, and which of them a
	/// rollback ends. A process holds one connection per back-end database it has used (a device session) and one per other
	/// Dataphor server it has queried (a remote session); both are opened on first use and then caught up to the process's
	/// transaction depth. Each level the catch-up opens has to be begun at the isolation level the client asked for at that
	/// level rather than at the innermost level's, and a rollback has to end a level only on the connections that still hold it.
	/// </summary>
	/// <remarks>
	/// No second Dataphor server is needed: RecordingRemoteSession stands in for the connection and never connects, so a link
	/// naming a host that does not answer is enough, as ServerLinkUserRollbackTest also relies on. The memory device is enough
	/// for the device case because the defect was in the engine's catch-up loop and not in any device; what a SQL device adds is
	/// the consequence - the outermost level's isolation level is the one that becomes the back-end transaction's, see
	/// SQLDeviceSession.InternalBeginTransaction - and showing that would need a real DBMS, which this suite has not got.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: RecordingRemoteSession's log and the connection a case reads are static, because the engine builds
	// the connection and a case has no other handle on it, so two cases of this fixture running at once would read each other's.
	public class ConnectionTransactionLevelTest : InProcessTestFixture
	{
		[OneTimeSetUp]
		public void SetUpDevice()
		{
			// The insert and delete start the device here, on a process of the instance's own, rather than inside a case's
			// transactions: a device start is server-wide, so every case then meets the device started and depends on one
			// thing fewer. The device is explicit so that no case can be reading a session the process already held.
			Instance.ExecuteScript(@"
				create device LevelDevice class ""System.MemoryDevice"";
				create table LevelTable in LevelDevice { ID : Integer, key { ID } };
				insert row { 0 ID } into LevelTable;
				delete LevelTable;
			");
		}

		// Installed per case, because InProcessTestFixture gives each case a process of its own and the class name is the process's.
		[SetUp]
		public void UseRecordingRemoteSessions()
		{
			RecordingRemoteSession.Log = new List<string>();
			RecordingRemoteSession.Connection = null;
			RecordingRemoteSession.FailNextCommit = false;

			// Reflection reaches RemoteSessionClassName deliberately: it is the one seam at which a connection to another
			// Dataphor server can be stood in for, and widening it to make these cases reachable would be worse, as
			// ServiceUtilityTest says of the declarations it reaches the same way.
			FieldInfo classNameField = typeof(ServerProcess).GetField("RemoteSessionClassName", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.That(classNameField, Is.Not.Null, "ServerProcess.RemoteSessionClassName has moved; these cases can no longer stand in for a connection to another server");
			classNameField.SetValue(Process, typeof(RecordingRemoteSession).AssemblyQualifiedName);
		}

		/// <summary>
		/// Every level the catch-up opens on a device session gets the isolation level it was begun at. It used to get the
		/// innermost level's, which on a SQL device is the one value that reaches the DBMS, so an outer Isolated level ran its
		/// back-end transaction at the inner level's read-uncommitted.
		/// </summary>
		[Test]
		public void TestDeviceConnectedInTransactionsGetsEachLevelsIsolationLevel()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			Process.BeginTransaction(IsolationLevel.Browse);
			try
			{
				// This process's first statement to touch the device, so DeviceConnect's catch-up opens both levels at once.
				// A statement run inside an open transaction begins no implicit transaction of its own, so there is no third level.
				ExecuteScript("insert row { 1 ID } into LevelTable;");

				Schema.DeviceSession session = ((ServerProcess)Process).DeviceConnect(DeviceNamed("LevelDevice"));

				Assert.That(session.Transactions.Count, Is.EqualTo(2));
				Assert.That(session.Transactions[0].IsolationLevel, Is.EqualTo(IsolationLevel.Isolated));
				Assert.That(session.Transactions[1].IsolationLevel, Is.EqualTo(IsolationLevel.Browse));
			}
			finally
			{
				Process.RollbackTransaction();
				Process.RollbackTransaction();
			}
		}

		/// <summary>The same for a connection to another Dataphor server, which the catch-up in RemoteConnect opens the levels on.</summary>
		[Test]
		public void TestRemoteSessionConnectedInTransactionsGetsEachLevelsIsolationLevel()
		{
			ExecuteScript(CreateLink("IsolationLink"));

			Process.BeginTransaction(IsolationLevel.Isolated);
			Process.BeginTransaction(IsolationLevel.Browse);
			try
			{
				// This process's first use of the link, so RemoteConnect's catch-up opens both levels at once.
				ExecuteScript("ExecuteOn('IsolationLink', 'select 1;');");

				Assert.That(RecordingRemoteSession.Log, Is.EqualTo(new[] { "begin Isolated at level 1", "begin Browse at level 2", "execute" }));
			}
			finally
			{
				Process.RollbackTransaction();
				Process.RollbackTransaction();
			}
		}

		/// <summary>
		/// A commit to another Dataphor server that fails leaves that connection one level shallower than this process, because
		/// RemoteSession.CommitTransaction counts the level down whatever the far server did. The rollback that follows must not
		/// end a level on it: its innermost open level is the outer transaction, whose work on the other server this session has
		/// not finished with and may still commit. It used to be rolled back, silently, while the local half stayed live.
		/// </summary>
		[Test]
		public void TestRollbackAfterAFailedRemoteCommitKeepsTheOuterLevel()
		{
			ExecuteScript(CreateLink("FailedCommitLink"));

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("ExecuteOn('FailedCommitLink', 'select 1;');");
				Process.BeginTransaction(IsolationLevel.Isolated);

				// The far server stops between the commit's prepare and its commit, so the inner level is ended on the connection
				// and not on this process.
				RecordingRemoteSession.FailNextCommit = true;
				Assert.Catch(() => Process.CommitTransaction());
				Assert.That(Process.TransactionCount, Is.EqualTo(2));
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(1));

				// The rollback of the level the failed commit left open reaches only the connections that still hold it, which is
				// not this one, and this process then pops to the depth the connection is already at.
				Process.RollbackTransaction();
				Assert.That(Process.TransactionCount, Is.EqualTo(1));
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(1));
				Assert.That(RecordingRemoteSession.Log, Has.No.Member("rollback level 1"));
			}
			finally
			{
				Process.RollbackTransaction();
			}

			// The two agree about the outer level again, so ending it reaches the connection as it would have without the failure.
			Assert.That(RecordingRemoteSession.Log, Has.Member("rollback level 1"));
			Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(0));
		}

		/// <summary>A link to a host that does not answer; nothing here connects, because RecordingRemoteSession stands in for the connection.</summary>
		private static string CreateLink(string name)
		{
			return String.Format("create server {0} tags {{ HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" }};", name);
		}

		/// <summary>The device of the given unqualified name, from the running server's catalog; the name D4 gave it carries the library it was created in.</summary>
		private Schema.Device DeviceNamed(string name)
		{
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
				if ((objectValue is Schema.Device device) && (Schema.Object.Unqualify(device.Name) == name))
					return device;
			throw new InvalidOperationException(String.Format("The test server's catalog holds no device named \"{0}\".", name));
		}

		/// <summary>
		/// Stands in for a connection to another Dataphor server: it records what the process asks of it and can be told to fail
		/// one commit, as a far server that stops between a commit's prepare and its commit makes it. The engine builds it
		/// through ServerProcess.RemoteSessionClassName, so what a case reaches it by is static; the cases of a fixture run one
		/// at a time and each gets a process, and so a connection, of its own.
		/// </summary>
		public class RecordingRemoteSession : RemoteSession
		{
			/// <summary>What this process has asked of the connection, oldest first.</summary>
			public static List<string> Log;

			/// <summary>The connection the engine built for the running case, so that the case can read its level count.</summary>
			public static RecordingRemoteSession Connection;

			/// <summary>Set to make the next commit fail, as a far server that has gone makes it.</summary>
			public static bool FailNextCommit;

			public RecordingRemoteSession(ServerProcess process, Schema.ServerLink serverLink) : base(process, serverLink)
			{
				Connection = this;
			}

			// TransactionCount is the count before the operation, so a begin names the level it opens and the others the level they end.
			protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
			{
				Log.Add(String.Format("begin {0} at level {1}", isolationLevel, TransactionCount + 1));
			}

			protected override void InternalPrepareTransaction()
			{
				Log.Add(String.Format("prepare level {0}", TransactionCount));
			}

			protected override void InternalCommitTransaction()
			{
				Log.Add(String.Format("commit level {0}", TransactionCount));
				if (FailNextCommit)
				{
					FailNextCommit = false;
					throw new InvalidOperationException("The far server has gone.");
				}
			}

			protected override void InternalRollbackTransaction()
			{
				Log.Add(String.Format("rollback level {0}", TransactionCount));
			}

			public override void Execute(string statement, DataParams paramsValue)
			{
				Log.Add("execute");
			}

			public override IDataValue Evaluate(string expression, DataParams paramsValue)
			{
				throw new NotSupportedException();
			}

			public override Schema.TableVar PrepareTableVar(Plan plan, string expression, DataParams paramsValue)
			{
				throw new NotSupportedException();
			}
		}
	}
}
