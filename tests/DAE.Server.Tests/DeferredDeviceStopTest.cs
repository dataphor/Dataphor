/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A device dropped inside a transaction is stopped only when the outermost transaction commits; a nested commit or rollback
	/// must neither stop it early nor forget the stop.
	/// </summary>
	[TestFixture]
	public class DeferredDeviceStopTest : InProcessTestFixture
	{
		[Test]
		public void TestNestedRollbackKeepsOuterStop()
		{
			Schema.Device device = CreateRunningDevice("NestedRollbackDevice");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("drop device NestedRollbackDevice;");
			Process.BeginTransaction(IsolationLevel.Isolated);
			Process.RollbackTransaction();
			Process.CommitTransaction();

			Assert.That(device.Running, Is.False);
		}

		[Test]
		public void TestNestedCommitDefersStop()
		{
			Schema.Device device = CreateRunningDevice("NestedCommitDevice");

			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("drop device NestedCommitDevice;");
			Process.BeginTransaction(IsolationLevel.Isolated);
			Process.CommitTransaction();
			Assert.That(device.Running, Is.True);

			Process.CommitTransaction();
			Assert.That(device.Running, Is.False);
		}

		[Test]
		public void TestNestedRollbackDiscardsItsStop()
		{
			Schema.Device device = CreateRunningDevice("UndoneDropDevice");

			Process.BeginTransaction(IsolationLevel.Isolated);
			Process.BeginTransaction(IsolationLevel.Isolated);
			ExecuteScript("drop device UndoneDropDevice;");
			Process.RollbackTransaction();
			Process.CommitTransaction();

			Assert.That(device.Running, Is.True);
		}

		[Test]
		public void TestFailedDropKeepsDevice()
		{
			Schema.Device device = CreateRunningDevice("FailedDropDevice");
			ExecuteScript("create table FailedDropTable in FailedDropDevice { ID : Integer, key { ID } };");

			Process.BeginTransaction(IsolationLevel.Isolated);
			Assert.Catch(() => ExecuteScript("drop device FailedDropDevice;"));
			Process.CommitTransaction();

			Assert.That(device.Running, Is.True);
		}

		private Schema.Device CreateRunningDevice(string name)
		{
			ExecuteScript(String.Format(@"
				create device {0} class ""System.MemoryDevice"";
				create table {0}Start in {0} {{ ID : Integer, key {{ ID }} }};
				insert row {{ 1 ID }} into {0}Start;
				drop table {0}Start;
			", name));
			Schema.Device device = (Schema.Device)Instance.Server.Catalog[name];
			Assert.That(device.Running, Is.True);
			return device;
		}
	}
}
