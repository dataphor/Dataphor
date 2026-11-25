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
	/// The catch-up is also the only way a connection to another Dataphor server joins a level: it joins one when a statement
	/// uses that link and not when the process begins the level, so it holds a prefix of the process's level stack and the
	/// prepare, the commit and the rollback ask it only about the levels in that prefix.
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
			RecordingRemoteSession.FailNextBegin = false;
			RecordingRemoteSession.ConstructionCount = 0;

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

				// Used again inside the inner level, which is what enlists the connection in it: a commit only ever reaches a
				// connection that holds the level being committed.
				ExecuteScript("ExecuteOn('FailedCommitLink', 'select 1;');");

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

		/// <summary>
		/// A statement that has nothing to do with a link makes no call at all on the connection the process holds to it. Every
		/// transaction the process began used to begin one on every connection it held, whether the statement used it or not, so
		/// a session that had queried another server once paid a begin, a prepare and a commit on that connection - three network
		/// round trips - on every statement afterwards, and a begin and a rollback on every statement that failed.
		/// </summary>
		[Test]
		public void TestAStatementThatDoesNotUseALinkMakesNoCallOnItsConnection()
		{
			ExecuteScript(CreateLink("UnusedLink"));
			ExecuteScript("ExecuteOn('UnusedLink', 'select 1;');");

			RecordingRemoteSession.Log.Clear();
			ExecuteScript("select 1;");
			List<string> afterASuccessfulStatement = new List<string>(RecordingRemoteSession.Log);

			RecordingRemoteSession.Log.Clear();
			// Failing at run time rather than at compile time, because a statement the compiler rejects has not begun its
			// implicit transaction yet and so never cost anything on a connection.
			Assert.Catch(() => ExecuteScript("raise Error('A statement that fails after its implicit transaction has begun.');"));
			List<string> afterAFailedStatement = new List<string>(RecordingRemoteSession.Log);

			// One Assert.Multiple over both, so that the count for one path is not hidden by the other path's failure: this is
			// where the saving is measured, and the two paths cost different numbers of calls.
			Assert.Multiple
			(
				() =>
				{
					Assert.That(afterASuccessfulStatement, Is.Empty, "A statement that does not use the link spoke to its connection.");
					Assert.That(afterAFailedStatement, Is.Empty, "A failing statement that does not use the link spoke to its connection.");
				}
			);

			Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(0));

			// Kept rather than released: nothing spoke to it, so nothing found anything wrong with it.
			Assert.That(ConnectionCount(), Is.EqualTo(1));
		}

		/// <summary>
		/// A connection joins each of the process's open levels as the statement that uses the link reaches it, at that level's
		/// own isolation level, rather than being brought to the process's depth by the begin.
		/// </summary>
		[Test]
		public void TestALinkUsedAtTwoLevelsIsEnlistedAtEachAsItIsReached()
		{
			ExecuteScript(CreateLink("IncrementalLink"));

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("ExecuteOn('IncrementalLink', 'select 1;');");
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(1));

				// The begin does not reach the connection, which then holds the outer level only - a legal prefix of this
				// process's level stack.
				Process.BeginTransaction(IsolationLevel.Browse);
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(1));

				ExecuteScript("ExecuteOn('IncrementalLink', 'select 1;');");
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(2));
				Assert.That(RecordingRemoteSession.Log, Is.EqualTo(new[] { "begin Isolated at level 1", "execute", "begin Browse at level 2", "execute" }));
			}
			finally
			{
				Process.RollbackTransaction();
				Process.RollbackTransaction();
			}
		}

		/// <summary>
		/// Ending a level the connection does not hold skips it: no work reached it at that level, so there is nothing there for
		/// a prepare or a commit to say anything about. The prepare used to assert that every connection stood at the process's
		/// depth, which lazy enlistment makes false by design.
		/// </summary>
		[Test]
		public void TestACommitOfALevelTheConnectionDoesNotHoldSkipsIt()
		{
			ExecuteScript(CreateLink("SkippedLink"));

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("ExecuteOn('SkippedLink', 'select 1;');");
				Process.BeginTransaction(IsolationLevel.Isolated);
				RecordingRemoteSession.Log.Clear();

				Process.CommitTransaction();
				Assert.That(RecordingRemoteSession.Log, Is.Empty);
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(1));

				// The outer level is one the connection does hold, so ending that one does reach it.
				Process.CommitTransaction();
				Assert.That(RecordingRemoteSession.Log, Is.EqualTo(new[] { "prepare level 1", "commit level 1" }));
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(0));
			}
			finally
			{
				while (Process.InTransaction)
					Process.RollbackTransaction();
			}
		}

		/// <summary>
		/// A connection whose server has gone while this process holds no level on it is let go by the next statement that uses
		/// the link, which then connects afresh and runs. The release used to happen at the transaction begin, which is what made
		/// a statement with nothing to do with the link notice a dead connection at all.
		/// </summary>
		[Test]
		public void TestADeadConnectionHoldingNoLevelIsLetGoWhenTheLinkIsUsedAgain()
		{
			ExecuteScript(CreateLink("ReconnectLink"));
			ExecuteScript("ExecuteOn('ReconnectLink', 'select 1;');");
			Assert.That(RecordingRemoteSession.ConstructionCount, Is.EqualTo(1));

			// Dead from here on. A statement that does not use the link neither notices it nor releases it.
			RecordingRemoteSession.FailNextBegin = true;
			RecordingRemoteSession.Log.Clear();
			ExecuteScript("select 1;");
			Assert.That(RecordingRemoteSession.Log, Is.Empty);
			Assert.That(RecordingRemoteSession.ConstructionCount, Is.EqualTo(1));

			// The use finds it dead, lets it go and connects again - exactly one retry, so the statement runs.
			ExecuteScript("ExecuteOn('ReconnectLink', 'select 1;');");
			Assert.That(RecordingRemoteSession.ConstructionCount, Is.EqualTo(2));
			string implicitBegin = String.Format("begin {0} at level 1", ((ServerProcess)Process).DefaultIsolationLevel);
			Assert.That(RecordingRemoteSession.Log, Is.EqualTo(new[] { implicitBegin, implicitBegin, "execute", "prepare level 1", "commit level 1" }));
			Assert.That(ConnectionCount(), Is.EqualTo(1));
		}

		/// <summary>
		/// A connection whose server has gone while this process holds a level on it may not be let go - work has been done on it
		/// at a level that is still open - so the use that found it dead fails naming the link, and the connection is kept with
		/// the levels it had.
		/// </summary>
		[Test]
		public void TestADeadConnectionHoldingALevelFailsTheUseAndIsKept()
		{
			ExecuteScript(CreateLink("ParticipantLink"));

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("ExecuteOn('ParticipantLink', 'select 1;');");
				Process.BeginTransaction(IsolationLevel.Isolated);

				// The catch-up of the inner level is the first call this process makes on the connection since the far server
				// went, and the connection holds the outer level, so the use fails instead of reconnecting.
				RecordingRemoteSession.FailNextBegin = true;
				Exception failedUse = Assert.Catch(() => ExecuteScript("ExecuteOn('ParticipantLink', 'select 1;');"));
				Assert.That(FindError(failedUse, (int)ServerException.Codes.RemoteSessionTransactionFailed), Is.Not.Null, "The failed use did not report the server link.");

				// A catch-up that failed part-way keeps the levels it did open, which is a legal prefix; nothing is unwound.
				Assert.That(RecordingRemoteSession.Connection.TransactionCount, Is.EqualTo(1));
				Assert.That(RecordingRemoteSession.ConstructionCount, Is.EqualTo(1));
				Assert.That(ConnectionCount(), Is.EqualTo(1));
			}
			finally
			{
				while (Process.InTransaction)
					Process.RollbackTransaction();
			}
		}

		/// <summary>A link to a host that does not answer; nothing here connects, because RecordingRemoteSession stands in for the connection.</summary>
		private static string CreateLink(string name)
		{
			return String.Format("create server {0} tags {{ HostName = \"localhost\", InstanceName = \"Nowhere\", OverridePortNumber = \"1\" }};", name);
		}

		/// <summary>How many connections to other servers this process holds, read the way a client would.</summary>
		private int ConnectionCount()
		{
			return (int)Process.Evaluate("Count(RemoteSessions where Process_ID = ProcessID())", null).AsNative;
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

			/// <summary>Set to make the next begin fail, as a far server that has gone makes it.</summary>
			public static bool FailNextBegin;

			/// <summary>How many connections the engine has built for the running case, so that a case can tell a reconnect from a kept connection.</summary>
			public static int ConstructionCount;

			public RecordingRemoteSession(ServerProcess process, Schema.ServerLink serverLink) : base(process, serverLink)
			{
				Connection = this;
				ConstructionCount++;
			}

			// TransactionCount is the count before the operation, so a begin names the level it opens and the others the level they end.
			protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
			{
				Log.Add(String.Format("begin {0} at level {1}", isolationLevel, TransactionCount + 1));
				if (FailNextBegin)
				{
					FailNextBegin = false;
					throw new InvalidOperationException("The far server has gone.");
				}
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
