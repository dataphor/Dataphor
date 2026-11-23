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
	/// What a rolled-back change to a class definition leaves in the running server. Some D4 objects are backed by a .NET
	/// class, and the attributes of their class definition are applied to the live object as properties; a rollback has to
	/// put both back, so that the running server never goes on enforcing a setting its own catalog does not hold.
	/// </summary>
	/// <remarks>
	/// The in-memory device's row limit is the setting under test because nothing outside the engine is needed to reach it;
	/// the SQL devices take class attributes of their own (server name, connection parameters) through the same code.
	/// </remarks>
	[TestFixture]
	public class DeviceClassAttributeRollbackTest : InProcessTestFixture
	{
		/// <summary>A rolled-back created attribute leaves the live device's property as it was, not only the class definition.</summary>
		[Test]
		public void TestRolledBackCreateKeepsTheOldMaxRowCount()
		{
			ExecuteScript("create device CreateRollbackDevice class \"System.MemoryDevice\";");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("alter device CreateRollbackDevice alter class { create \"MaxRowCount\" = \"100\" };");
			Process.RollbackTransaction();

			Assert.That(MaxRowCount("CreateRollbackDevice"), Is.EqualTo(-1));
			Assert.That(Script("CreateRollbackDevice"), Does.Not.Contain("MaxRowCount"));
		}

		/// <summary>
		/// A statement that fails part-way leaves the device as it was. The attributes are applied one at a time and a value the
		/// property's type cannot take throws, so the values the statement replaces have to be recorded before it runs; the
		/// implicit transaction every statement runs in then undoes what the statement had already applied.
		/// </summary>
		[Test]
		public void TestFailedAlterKeepsTheOldSettings()
		{
			ExecuteScript("create device FailedAlterDevice class \"System.MemoryDevice\";");

			Assert.Catch(() => ExecuteScript("alter device FailedAlterDevice alter class { create \"IgnoreUnsupported\" = \"false\", create \"MaxRowCount\" = \"not a number\" };"));

			Assert.That(IgnoreUnsupported("FailedAlterDevice"), Is.True);
			Assert.That(MaxRowCount("FailedAlterDevice"), Is.EqualTo(-1));
			Assert.That(Script("FailedAlterDevice"), Does.Not.Contain("IgnoreUnsupported"));
		}

		/// <summary>
		/// Two statements on the same device in one transaction undo to the state before the first of them: each records the value
		/// it replaces and they are undone in reverse, so the earliest-recorded value is the one that lands last.
		/// </summary>
		[Test]
		public void TestRolledBackSecondStatementDoesNotKeepTheFirstsValue()
		{
			ExecuteScript("create device TwoStatementDevice class \"System.MemoryDevice\";");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("alter device TwoStatementDevice alter class { create \"MaxRowCount\" = \"100\" };");
			ExecuteScript("alter device TwoStatementDevice alter class { alter \"MaxRowCount\" = \"200\" };");
			Process.RollbackTransaction();

			Assert.That(MaxRowCount("TwoStatementDevice"), Is.EqualTo(-1));
			Assert.That(Script("TwoStatementDevice"), Does.Not.Contain("MaxRowCount"));
		}

		/// <summary>A rollback undoes the changes of its own level only: the outer level's attribute is still there afterwards.</summary>
		[Test]
		public void TestNestedRollbackKeepsTheOuterChange()
		{
			ExecuteScript("create device NestedRollbackDevice class \"System.MemoryDevice\";");

			Process.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				ExecuteScript("alter device NestedRollbackDevice alter class { create \"MaxRowCount\" = \"100\" };");

				Process.BeginTransaction(IsolationLevel.Isolated);
				ExecuteScript("alter device NestedRollbackDevice alter class { alter \"MaxRowCount\" = \"200\" };");
				Process.RollbackTransaction();

				Assert.That(MaxRowCount("NestedRollbackDevice"), Is.EqualTo(100));
			}
			finally
			{
				Process.RollbackTransaction();
			}

			Assert.That(MaxRowCount("NestedRollbackDevice"), Is.EqualTo(-1));
		}

		/// <summary>
		/// A class definition with no live object behind it - an operator's, whose class is instantiated afresh on every
		/// compilation - is restored on its own, with nothing to put back on an instance.
		/// </summary>
		[Test]
		public void TestRolledBackCreateWithNoInstanceRestoresTheClassDefinition()
		{
			ExecuteScript("create operator NoInstanceRollbackOp() : Guid class \"System.NewGuidNode\";");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("alter operator NoInstanceRollbackOp() alter class { create \"Nonesuch\" = \"1\" };");
			Process.RollbackTransaction();

			Assert.That(OperatorScript("NoInstanceRollbackOp()"), Does.Not.Contain("Nonesuch"));
		}

		/// <summary>The row limit the running device enforces, read off the live object: nothing in the catalog reports it.</summary>
		private int MaxRowCount(string deviceName)
		{
			return ((MemoryDevice)Device(deviceName)).MaxRowCount;
		}

		private bool IgnoreUnsupported(string deviceName)
		{
			return Device(deviceName).IgnoreUnsupported;
		}

		private Schema.Device Device(string deviceName)
		{
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
				if ((objectValue is Schema.Device) && (Schema.Object.Unqualify(objectValue.Name) == deviceName))
					return (Schema.Device)objectValue;
			Assert.Fail(String.Format("Device {0} is not in the catalog.", deviceName));
			return null;
		}

		/// <summary>The create statement the catalog emits for the device, which carries its class definition.</summary>
		private string Script(string deviceName)
		{
			return (string)Evaluate(String.Format("Script(Name(LibraryName() + '.{0}'))", deviceName));
		}

		private string OperatorScript(string operatorSpecifier)
		{
			return (string)Evaluate(String.Format("Script(LibraryName() + '.{0}')", operatorSpecifier));
		}

		private object Evaluate(string expression)
		{
			return ((ServerProcess)Process).Evaluate(expression, null).AsNative;
		}
	}
}
