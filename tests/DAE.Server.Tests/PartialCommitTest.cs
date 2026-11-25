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
	/// What a commit that succeeds on some of a transaction's participants and then fails on another leaves behind. A process
	/// holds one connection per back-end database it has used (a device session) and one per other Dataphor server it has
	/// queried (a remote session), the commit asks each in turn, and no protocol can undo one that has already said yes. So the
	/// participants that committed are left one level shallower than the process, the level each of them now holds innermost is
	/// the <em>outer</em> transaction, and the clean-up that follows must neither roll that level back nor commit it unasked.
	/// </summary>
	/// <remarks>
	/// GoodTable is on an ordinary in-memory device and FailTable on one whose commit can be made to fail. The setup touches
	/// them in that order deliberately: the commit asks the device sessions in the order the process connected them, so the
	/// working device has to commit before the failing one for a commit to be partial at all. The same behaviour for a
	/// connection to another Dataphor server is in ConnectionTransactionLevelTest, which stands such a connection in.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: FailNextCommit is on the device, which is server-wide, and the cases of a fixture share its server.
	public class PartialCommitTest : InProcessTestFixture
	{
		private const string FailingDeviceName = "FailDevice";
		private const string WorkingDeviceName = "GoodDevice";

		[OneTimeSetUp]
		public void SetUpDevices()
		{
			// The class loader's registered-class list is public and takes a class at run time, which is how a device double
			// reaches a running engine without a library of its own; TransactionBeginTest's double is registered the same way.
			// The library a registered class carries is inert here: its only reader, Plan.CheckClassDependency, is
			// [Conditional("CHECKCLASSDEPENDENCY")] and that symbol is defined nowhere in the tree.
			Schema.LoadedLibrary system = Instance.Server.Catalog.LoadedLibraries[Engine.SystemLibraryName];
			Schema.RegisteredAssembly assembly = new Schema.RegisteredAssembly(typeof(FailingCommitDevice).Assembly, system);
			Instance.Server.Catalog.ClassLoader.Classes.Add(new Schema.RegisteredClass(FailingCommitDevice.ClassName, system, assembly, typeof(FailingCommitDevice).AssemblyQualifiedName));

			Instance.ExecuteScript(String.Format(@"
				create device {0} class ""System.MemoryDevice"";
				create device {1} class ""{2}"";
				create table GoodTable in {0} {{ ID : Integer, key {{ ID }} }};
				create table FailTable in {1} {{ ID : Integer, key {{ ID }} }};
				create table HandlerTable in {0} {{ ID : Integer, key {{ ID }} }};
				create table AuditTable in {0} {{ ID : Integer, key {{ ID }} }};
				create operator AuditHandlerInsert(const ARow : typeof(HandlerTable[]))
				begin
					insert row {{ ARow.ID ID }} into AuditTable;
				end;
				attach operator AuditHandlerInsert to HandlerTable on {{ after insert }} tags {{ DAE.IsDeferred = ""true"" }};
			", WorkingDeviceName, FailingDeviceName, FailingCommitDevice.ClassName));
		}

		/// <summary>
		/// Clears the tables on the case's own process, and in doing so connects the working device before the failing one,
		/// which is the order the commit then asks them in. Every case also reads both sessions' level counts, so none may be
		/// the first to connect one - see <see cref="Levels"/>.
		/// </summary>
		[SetUp]
		public void ClearTablesInConnectOrder()
		{
			ExecuteScript("delete GoodTable; delete FailTable; delete HandlerTable; delete AuditTable;");
		}

		/// <summary>
		/// A case that fails before the phase it armed has run would otherwise leave the flag set on the device, which outlives
		/// the case, and the next case's [SetUp] would fail in its own implicit commit rather than where it was looking.
		/// </summary>
		[TearDown]
		public void DisarmTheFailingDevice()
		{
			FailingDevice.FailNextCommit = false;
			FailingDevice.FailNextPrepare = false;
		}

		/// <summary>
		/// The rollback of the level a failed commit left open ends that level only on the participants that still hold it. The
		/// device loop used to ask whether the device session was in a transaction at all, which after a partial commit is true
		/// of a committed participant for the <em>outer</em> level: that level was then rolled back on it, silently, taking work
		/// the session had not finished with. Measured before the fix: the working device went to 0 levels here, Count(GoodTable)
		/// to 0, and the outer commit then failed with Schema error "No active transaction."
		/// </summary>
		[Test]
		public void TestRollbackAfterAFailedDeviceCommitKeepsTheOuterLevel()
		{
			FailACommitOfANestedTransaction();
			AssertDepthsAfterTheFailedCommit();

			Process.RollbackTransaction();
			Assert.That(Process.TransactionCount, Is.EqualTo(1));
			Assert.That(Levels(WorkingDeviceName), Is.EqualTo(1));
			Assert.That(Levels(FailingDeviceName), Is.EqualTo(1));

			// The outer transaction is still the session's to finish, and both rows inserted on the working device are in it:
			// the one from the level that committed and the one from the level the session never ended.
			Process.CommitTransaction();
			Assert.That(RowCount("GoodTable"), Is.EqualTo(2));
			Assert.That(RowCount("GoodTable where ID = 1"), Is.EqualTo(1));
		}

		/// <summary>
		/// A retried commit is refused. IServerProcess.CommitTransaction rolls nothing back when it fails, so a client that
		/// catches the failure and calls it again is making a legal call; the retry used to report success, having committed the
		/// outer level on the participants the first commit had already brought down to it - a level the client never asked to
		/// commit and could no longer roll back. Measured before the fix: the retry raised nothing and Count(GoodTable) was 2
		/// with the process still reporting one open transaction.
		/// </summary>
		[Test]
		public void TestARetriedCommitAfterAFailedOneIsRefused()
		{
			FailACommitOfANestedTransaction();

			Exception exception = Assert.Catch(() => Process.CommitTransaction());
			Assert.That(FindError(exception, (int)ServerException.Codes.TransactionCanOnlyBeRolledBack), Is.Not.Null, exception.ToString());
			AssertDepthsAfterTheFailedCommit();

			Process.RollbackTransaction();
			Process.RollbackTransaction();
		}

		/// <summary>
		/// A nested begin after a failed commit is refused, for the same reason and with the same error. It begins a level on
		/// every participant, so it used to leave the shallow participants one level behind the deep ones at every depth above:
		/// the level counts then no longer say which level a participant holds, and the rollback's predicate cannot be sound.
		/// </summary>
		[Test]
		public void TestANestedBeginAfterAFailedCommitIsRefused()
		{
			FailACommitOfANestedTransaction();

			Exception exception = Assert.Catch(() => Process.BeginTransaction(IsolationLevel.Isolated));
			Assert.That(FindError(exception, (int)ServerException.Codes.TransactionCanOnlyBeRolledBack), Is.Not.Null, exception.ToString());
			AssertDepthsAfterTheFailedCommit();

			Process.RollbackTransaction();
			Process.RollbackTransaction();
		}

		/// <summary>
		/// A commit that fails in its prepare phase has committed nothing and stays retryable - which is why the record of a
		/// half-committed level is set after the prepare and not before it. The behaviour this protects is the ordinary way a
		/// commit fails, a deferred constraint violation; the device's prepare stands in for it here because this fixture already
		/// has a device that fails on command and a constraint would need one more piece of setup to say the same thing.
		/// </summary>
		[Test]
		public void TestACommitThatFailsInItsPrepareStaysRetryable()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 1 ID } into GoodTable; insert row { 1 ID } into FailTable;");

			FailingDevice.FailNextPrepare = true;
			Exception exception = Assert.Catch(() => Process.CommitTransaction());
			Assert.That(exception.ToString(), Does.Contain(FailingCommitDeviceSession.PrepareFailureMessage));
			Assert.That(Process.TransactionCount, Is.EqualTo(1));
			Assert.That(Levels(WorkingDeviceName), Is.EqualTo(1));
			Assert.That(Levels(FailingDeviceName), Is.EqualTo(1));

			Process.CommitTransaction();
			Assert.That(Process.InTransaction, Is.False);
			Assert.That(RowCount("GoodTable"), Is.EqualTo(1));
			Assert.That(RowCount("FailTable"), Is.EqualTo(1));
		}

		/// <summary>
		/// Work issued between a failed commit and the rollback is refused outright. It begins no level, so neither of the two
		/// refusals above sees it; it used to be allowed and to land at the level each participant then held innermost, which is
		/// the failed level on the participants that had not committed and the <em>enclosing</em> one on those that had. Measured
		/// before the fix with the statement below: the depths were unchanged by it, so its two inserts went to different levels,
		/// and after rolling the failed level back and committing the enclosing one GoodTable held its row 3 and FailTable did
		/// not - permanently.
		/// </summary>
		[Test]
		public void TestAStatementAfterAFailedCommitIsRefused()
		{
			FailACommitOfANestedTransaction();

			Exception exception = Assert.Catch(() => ExecuteScript("insert row { 3 ID } into GoodTable; insert row { 3 ID } into FailTable;"));
			Assert.That(FindError(exception, (int)ServerException.Codes.TransactionCanOnlyBeRolledBack), Is.Not.Null, exception.ToString());
			AssertDepthsAfterTheFailedCommit();

			// The enclosing level is clean once the failed one is gone, and is still the session's to commit; the refused
			// statement must be nowhere in it.
			Process.RollbackTransaction();
			Process.CommitTransaction();
			Assert.That(RowCount("GoodTable where ID = 3"), Is.EqualTo(0));
			Assert.That(RowCount("FailTable where ID = 3"), Is.EqualTo(0));
		}

		/// <summary>
		/// The same when the failed commit and the work after it are inside one D4 statement, with the failure swallowed by the
		/// script's own error handler. This is the case that rules out putting the refusal at the statement boundary - there is no
		/// statement boundary between the two - and without it the fix looks finished when it is not. Measured before the fix: the
		/// row-3 insert landed on the working device and not on the failing one, and that tear was in the <em>enclosing</em>
		/// transaction, which then committed, so it became permanent.
		/// </summary>
		[Test]
		public void TestWorkAfterAFailedCommitSwallowedInsideOneStatementIsRefused()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 1 ID } into GoodTable; insert row { 1 ID } into FailTable;");

			FailingDevice.FailNextCommit = true;
			Exception exception = Assert.Catch
			(
				() => ExecuteScript
				(
					@"
						begin
							BeginTransaction();
							try
								insert row { 2 ID } into GoodTable;
								insert row { 2 ID } into FailTable;
								CommitTransaction();
							except
								var LSwallowed := 1;
							end;
							insert row { 3 ID } into GoodTable;
							insert row { 3 ID } into FailTable;
						end;
					"
				)
			);
			Assert.That(FindError(exception, (int)ServerException.Codes.TransactionCanOnlyBeRolledBack), Is.Not.Null, exception.ToString());

			// The statement's own unwinding rolled the half-committed level back, so the enclosing transaction is clean and still
			// the session's to finish. Row 2 is on the working device because the failed commit committed the level there, and
			// nothing can undo that; row 3, the work after the failure, must be on neither device.
			Assert.That(Process.TransactionCount, Is.EqualTo(1));
			Process.CommitTransaction();
			Assert.That(RowCount("GoodTable where ID = 3"), Is.EqualTo(0));
			Assert.That(RowCount("FailTable where ID = 3"), Is.EqualTo(0));
		}

		/// <summary>
		/// A statement that changes the catalog is refused too, and this is the case that pins the seam DDL reaches: every write
		/// to the catalog store funnels through <c>ServerCatalogDeviceSession.AcquireCatalogStoreConnection</c> for an updatable
		/// acquire. <c>create operator</c> rather than <c>create table</c> deliberately - a table reaches its device as well, and
		/// so is refused by <c>Schema.DeviceSession</c> whatever the catalog arm does, whereas this statement touches no device:
		/// measured, it is allowed with the catalog arm alone removed.
		/// </summary>
		[Test]
		public void TestDDLAfterAFailedCommitIsRefused()
		{
			FailACommitOfANestedTransaction();

			Exception exception = Assert.Catch(() => ExecuteScript("create operator RefusedOperator() : Integer begin result := 1; end;"));
			Assert.That(FindError(exception, (int)ServerException.Codes.TransactionCanOnlyBeRolledBack), Is.Not.Null, exception.ToString());

			// The rollback has to undo the part of the refused statement that reached the in-memory catalog before its first
			// store write - its own replay writes the store, which is why the rollback is exempt from the refusal - so creating
			// the operator again is the check that the catalog and the store agree afterwards.
			Process.RollbackTransaction();
			Process.CommitTransaction();
			ExecuteScript("create operator RefusedOperator() : Integer begin result := 1; end;");
		}

		/// <summary>
		/// Both D4 rollback idioms still work after a failed commit, and the session carries on afterwards. They are why the
		/// refusal is at the participant seams and not at the statement boundary: <c>RollbackTransaction()</c> is an ordinary D4
		/// operator invocation, so a blanket refusal of transactional calls would refuse the one call that gets the session out,
		/// and the loop form cannot be exempted by recognising a plan's root node because its root is a <c>while</c>. The loop
		/// form is the error-recovery idiom in this repository's own coverage scripts.
		/// </summary>
		[Test]
		public void TestTheD4RollbackIdiomsStillWorkAfterAFailedCommit()
		{
			FailACommitOfANestedTransaction();

			ExecuteScript("RollbackTransaction();");
			Assert.That(Process.TransactionCount, Is.EqualTo(1));

			ExecuteScript("while TransactionCount() > 0 do RollbackTransaction();");
			Assert.That(Process.InTransaction, Is.False);

			ExecuteScript("insert row { 9 ID } into GoodTable;");
			Assert.That(RowCount("GoodTable where ID = 9"), Is.EqualTo(1));
		}

		/// <summary>
		/// A commit retried after a partial commit inside the deferred-handler transaction is refused rather than running the
		/// handlers again. The engine runs a transaction's deferred event handlers in a nested transaction of its own during the
		/// commit's prepare phase; if that nested commit fails part-way, the handlers' work is committed on the participants that
		/// said yes, while the enclosing commit fails in its prepare, which by design leaves it retryable - and nothing takes an
		/// applied handler out of the list. Measured before the fix: the handler's row was still there after the failure and the
		/// retry raised "The table AuditTable already has a row with ID (1)." from inside the handler, which had run a second
		/// time. A handler that appended to a log instead of inserting a keyed row would have doubled its effect silently.
		/// </summary>
		[Test]
		public void TestARetriedCommitAfterAPartialCommitInADeferredHandlerIsRefused()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 1 ID } into HandlerTable; insert row { 1 ID } into FailTable;");

			// The nested transaction the deferred handler runs in is the one whose commit fails: the handler's insert goes to the
			// working device, which commits that level, and the failing device then throws.
			FailingDevice.FailNextCommit = true;
			Exception exception = Assert.Catch(() => Process.CommitTransaction());
			Assert.That(exception.ToString(), Does.Contain(FailingCommitDeviceSession.FailureMessage));

			// The nested level was rolled back correctly - that much already worked - but the handler has been applied.
			Assert.That(Process.TransactionCount, Is.EqualTo(1));
			Assert.That(Levels(WorkingDeviceName), Is.EqualTo(1));
			Assert.That(Levels(FailingDeviceName), Is.EqualTo(1));

			Exception retry = Assert.Catch(() => Process.CommitTransaction());
			Assert.That(FindError(retry, (int)ServerException.Codes.TransactionCanOnlyBeRolledBack), Is.Not.Null, retry.ToString());

			Process.RollbackTransaction();
		}

		/// <summary>
		/// Leaves the process two levels deep with the inner level's commit having failed after the working device had committed
		/// it: both devices carry work at both levels, and the failing device's commit of the inner level throws.
		/// </summary>
		private void FailACommitOfANestedTransaction()
		{
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 1 ID } into GoodTable; insert row { 1 ID } into FailTable;");
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("insert row { 2 ID } into GoodTable; insert row { 2 ID } into FailTable;");

			FailingDevice.FailNextCommit = true;
			Exception exception = Assert.Catch(() => Process.CommitTransaction());
			Assert.That(exception.ToString(), Does.Contain(FailingCommitDeviceSession.FailureMessage));
		}

		/// <summary>
		/// The depths a half-committed level leaves: the working device committed the inner level and so holds only the outer
		/// one, the failing device holds both - Schema.DeviceSession.CommitTransaction ends the level only once the device's own
		/// commit has returned - and the process holds both, because its commit never reached its own end.
		/// </summary>
		private void AssertDepthsAfterTheFailedCommit()
		{
			Assert.That(Process.TransactionCount, Is.EqualTo(2));
			Assert.That(Levels(WorkingDeviceName), Is.EqualTo(1));
			Assert.That(Levels(FailingDeviceName), Is.EqualTo(2));
		}

		/// <summary>
		/// How many transaction levels the case's process holds on the named device. Every case connects both devices in
		/// [SetUp], so this reads a session the process already has; on one it did not, DeviceConnect would open a session and
		/// catch it up to the process's depth, which is the very count a partial commit makes disagree.
		/// </summary>
		private int Levels(string deviceName)
		{
			return ((ServerProcess)Process).DeviceConnect(DeviceNamed(deviceName)).Transactions.Count;
		}

		private FailingCommitDevice FailingDevice
		{
			get { return (FailingCommitDevice)DeviceNamed(FailingDeviceName); }
		}

		private Schema.Device DeviceNamed(string name)
		{
			return (Schema.Device)Instance.Server.Catalog[name];
		}

		private int RowCount(string tableExpression)
		{
			return (int)Process.Evaluate(String.Format("Count({0})", tableExpression), null).AsNative;
		}

		/// <summary>
		/// A device whose commit of a transaction level can be made to fail, as a back-end database that fails part-way through a
		/// commit does. An ordinary in-memory device in every other respect, so a transaction spanning it and another device
		/// commits as usual until the flag is set.
		/// </summary>
		/// <remarks>
		/// Reusable, and worth reusing: several error paths through the commit and the rollback of a transaction that spans more
		/// than one database cannot be reached from a test without a device that fails on command, and this suite has no real
		/// DBMS to fail. Its sibling, TransactionBeginTest.FailingBeginDevice, fails a begin instead.
		/// </remarks>
		public class FailingCommitDevice : MemoryDevice
		{
			public const string ClassName = "Test.FailingCommitDevice";

			public FailingCommitDevice(int iD, string name) : base(iD, name) {}

			/// <summary>Set to make the next commit of a transaction level on any session of this device throw; the commit clears it.</summary>
			// On the device rather than on the session because a case has no handle on the session before the process connects it,
			// and because a device is what a back-end database stands for here. A device is server-wide: see the fixture's note.
			public bool FailNextCommit { get; set; }

			/// <summary>The same for the prepare phase of a commit, which runs before any participant has committed anything.</summary>
			public bool FailNextPrepare { get; set; }

			protected override Schema.DeviceSession InternalConnect(ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo)
			{
				return new FailingCommitDeviceSession(this, serverProcess, deviceSessionInfo);
			}
		}

		public class FailingCommitDeviceSession : MemoryDeviceSession
		{
			public const string FailureMessage = "The test device's commit of this transaction level fails.";
			public const string PrepareFailureMessage = "The test device's prepare of this transaction level fails.";

			public FailingCommitDeviceSession(Schema.Device device, ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo) : base(device, serverProcess, deviceSessionInfo) {}

			public new FailingCommitDevice Device { get { return (FailingCommitDevice)base.Device; } }

			protected override void InternalPrepareTransaction()
			{
				if (Device.FailNextPrepare)
				{
					Device.FailNextPrepare = false;
					throw new InvalidOperationException(PrepareFailureMessage);
				}
				base.InternalPrepareTransaction();
			}

			protected override void InternalCommitTransaction()
			{
				if (Device.FailNextCommit)
				{
					Device.FailNextCommit = false;
					throw new InvalidOperationException(FailureMessage);
				}
				base.InternalCommitTransaction();
			}
		}
	}
}
