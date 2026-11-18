/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Device.Memory;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Beginning a transaction is all or nothing: when a device session fails to begin, the process and its other sessions are left
	/// as they were before the call. FailTable is on a device whose sessions fail to begin when FailAtLevel says so; the other tables
	/// are on the default memory device, whose session each test's process connects after FailTable's, so that it is a session that a
	/// half-done begin never reached.
	/// </summary>
	[TestFixture]
	public class TransactionBeginTest : InProcessTestFixture
	{
		[OneTimeSetUp]
		public void SetUpTables()
		{
			Schema.LoadedLibrary system = Instance.Server.Catalog.LoadedLibraries[Engine.SystemLibraryName];
			Schema.RegisteredAssembly assembly = new Schema.RegisteredAssembly(typeof(FailingBeginDevice).Assembly, system);
			Instance.Server.Catalog.ClassLoader.Classes.Add(new Schema.RegisteredClass(FailingBeginDevice.ClassName, system, assembly, typeof(FailingBeginDevice).AssemblyQualifiedName));

			Instance.ExecuteScript(String.Format(@"
				create device FailDevice class ""{0}"";
				create table FailTable in FailDevice {{ ID : Integer, key {{ ID }} }};
				create table MemoryTable {{ ID : Integer, key {{ ID }} }};
				create table HandledTable {{ ID : Integer, key {{ ID }} }};
				create table HandlerLog {{ ID : Integer, key {{ ID }} }};
				create operator LogHandled(const ARow : typeof(HandledTable[])) begin insert ARow into HandlerLog; end;
				attach operator LogHandled to HandledTable on after insert tags {{ DAE.IsDeferred = ""true"" }};
			", FailingBeginDevice.ClassName));
		}

		// Runs on each test's new process: FailTable first, so that the process connects the failing session before the memory device's
		[SetUp]
		public void ClearTables()
		{
			ExecuteScript("delete FailTable; delete MemoryTable; delete HandledTable; delete HandlerLog;");
		}

		// The implicit transaction of a statement whose begin failed used to stay open, so that the next statement ran in it, with the
		// memory device's session in no transaction, and the eventual rollback kept that statement's work.
		[Test]
		public void TestImplicitBeginFails()
		{
			AssertBeginFails(1, () => ExecuteScript("insert row { 1 ID } into MemoryTable;"));
			Assert.That(Process.TransactionCount, Is.EqualTo(0));

			ExecuteScript("insert row { 2 ID } into MemoryTable;");
			Assert.That(Process.InTransaction, Is.False);
			Assert.That(RowCount("MemoryTable"), Is.EqualTo(1));
			Assert.That(RowCount("MemoryTable where ID = 2"), Is.EqualTo(1));
		}

		[Test]
		public void TestNestedBeginFails()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 1 ID } into FailTable; insert row { 1 ID } into MemoryTable;");

			AssertBeginFails(2, () => Process.BeginTransaction(IsolationLevel.Isolated));
			Assert.That(Process.TransactionCount, Is.EqualTo(1));

			Process.RollbackTransaction();
			Assert.That(Process.InTransaction, Is.False);
			Assert.That(RowCount("FailTable"), Is.EqualTo(0));
			Assert.That(RowCount("MemoryTable"), Is.EqualTo(0));
		}

		// A commit runs deferred handlers in a nested transaction. When that begin failed, the commit's error handling rolled back a level
		// on every session: the half-begun level on the sessions it reached, and the client's own level on the memory device's session,
		// discarding the client's work in a transaction that stayed open.
		[Test]
		public void TestDeferredHandlerBeginFails()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 1 ID } into HandledTable;");

			AssertBeginFails(2, () => Process.CommitTransaction());
			Assert.That(Process.TransactionCount, Is.EqualTo(1));
			Assert.That(RowCount("HandledTable"), Is.EqualTo(1));

			Process.RollbackTransaction();
			Assert.That(Process.InTransaction, Is.False);
			Assert.That(RowCount("HandledTable"), Is.EqualTo(0));
			Assert.That(RowCount("HandlerLog"), Is.EqualTo(0));
		}

		private static void AssertBeginFails(int level, Action call)
		{
			FailingBeginDeviceSession.FailAtLevel = level;
			try
			{
				Exception exception = Assert.Catch(call);
				Assert.That(exception.ToString(), Does.Contain(FailingBeginDeviceSession.FailureMessage));
			}
			finally
			{
				FailingBeginDeviceSession.FailAtLevel = 0;
			}
		}

		private int RowCount(string tableExpression)
		{
			return (int)Process.Evaluate(String.Format("Count({0})", tableExpression), null).AsNative;
		}

		public class FailingBeginDevice : MemoryDevice
		{
			public const string ClassName = "Test.FailingBeginDevice";

			public FailingBeginDevice(int iD, string name) : base(iD, name) {}

			protected override Schema.DeviceSession InternalConnect(ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo)
			{
				return new FailingBeginDeviceSession(this, serverProcess, deviceSessionInfo);
			}
		}

		public class FailingBeginDeviceSession : MemoryDeviceSession
		{
			public const string FailureMessage = "The test device refuses to begin this transaction level.";

			/// <summary>The level, counting the one being begun, at which every session of the device fails to begin; 0 for none.</summary>
			public static int FailAtLevel;

			public FailingBeginDeviceSession(Schema.Device device, ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo) {}

			protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
			{
				if (Transactions.Count == FailAtLevel)
					throw new InvalidOperationException(FailureMessage);
				base.InternalBeginTransaction(isolationLevel);
			}
		}
	}
}
