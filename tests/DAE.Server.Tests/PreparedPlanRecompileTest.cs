/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Compiling;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What a plan the caller compiled once and executes more than once does when its statement changes the catalog. The compiled
	/// form of a create statement does not hold the statement: the compiler builds the catalog object while compiling and the node
	/// keeps it, so executing the node a second time put that same object back - same identity, same object id, and carrying any
	/// change an alter had made to it in between - rather than building what the statement describes. Such a plan now compiles its
	/// statement afresh before each execution after the first.
	/// </summary>
	/// <remarks>
	/// Nothing here opens a transaction: every statement commits, and what is under test is what a second execution of the held
	/// plan builds. <c>IServerProcess.PrepareStatement</c> is the route under test, because a plan the caller holds consults no
	/// timestamp and so is reached by neither of the two landed siblings (the plan cache's eviction, and a catalog object owning
	/// its class definition).
	/// </remarks>
	[TestFixture]
	public class PreparedPlanRecompileTest : DeviceClassAttributeFixture
	{
		/// <summary>The reproduction: a held create plan, executed again after the object it made was altered and dropped.</summary>
		[Test]
		public void TestPreparedCreateExecutedAgainBuildsWhatItSays()
		{
			IServerStatementPlan plan = Process.PrepareStatement("create device RecompileDevice class \"System.MemoryDevice\" attributes { \"MaxRowCount\" = \"100\" };", null);
			try
			{
				plan.Execute(null);
				int firstID = Device("RecompileDevice").ID;

				Process.Execute("alter device RecompileDevice alter class { alter \"MaxRowCount\" = \"200\" };", null);
				Process.Execute("drop device RecompileDevice;", null);
				plan.Execute(null);

				Assert.That(MaxRowCount("RecompileDevice"), Is.EqualTo(100));
				Assert.That(Script("RecompileDevice"), Does.Contain("\"MaxRowCount\" = \"100\""));
				// The object id is what proves the device was built afresh rather than the first execution's instance put back.
				Assert.That(Device("RecompileDevice").ID, Is.Not.EqualTo(firstID));
			}
			finally
			{
				Process.UnprepareStatement(plan);
			}
		}

		/// <summary>A held create plan executed again while what it made is still there: the recompilation fails, and the process survives it.</summary>
		[Test]
		public void TestPreparedCreateExecutedAgainOverAnExistingObjectFails()
		{
			IServerStatementPlan plan = Process.PrepareStatement("create device ExistingDevice class \"System.MemoryDevice\";", null);
			try
			{
				plan.Execute(null);

				// The recompilation is what fails now, so the error is the compiler's rather than the one the create node used to
				// raise when it reached the catalog: measured on 2026-11-25, CompilerException 105214 ("An object named
				// "General.ExistingDevice" is already defined.") where the unfixed engine gave a RuntimeException wrapping
				// SchemaException 113106 ("Duplicate object name "General.ExistingDevice"."). The code is what is pinned, since
				// error codes are a compatibility surface and docs/migration-notes.md records this change of one.
				Exception caught = Assert.Catch(() => plan.Execute(null));
				Assert.That(FindError(caught, (int)CompilerException.Codes.CreatingDuplicateObjectName), Is.Not.Null, caught.ToString());

				// A failed recompilation must leave the process usable: the plan it could not replace is still the one it had.
				Process.Execute("create device ExistingDeviceSibling class \"System.MemoryDevice\";", null);
				Assert.That(Device("ExistingDeviceSibling"), Is.Not.Null);
			}
			finally
			{
				Process.UnprepareStatement(plan);
			}
		}

		/// <summary>A held drop plan executed again after what it dropped was created afresh.</summary>
		/// <remarks>
		/// The drop face of the same defect, and a worse outcome than the create face: a compiled drop holds the object the
		/// compiler resolved, so the second execution ran StopDevice and DropDevice on the instance the first execution had
		/// already removed. Measured on 2026-11-25 against the unfixed engine: the second execution raised nothing, the
		/// re-created device was gone from the running catalog (Script could not find it), and the name could not be used again
		/// - "An object named "General.DropArmDevice" is already defined." - because the catalog store still held its row. The
		/// catalog and its store disagreed from then on, which is why this case is kept rather than reported and dropped.
		/// </remarks>
		[Test]
		public void TestPreparedDropExecutedAgainDropsWhatIsThereNow()
		{
			Process.Execute("create device DropArmDevice class \"System.MemoryDevice\";", null);

			IServerStatementPlan plan = Process.PrepareStatement("drop device DropArmDevice;", null);
			try
			{
				plan.Execute(null);
				Process.Execute("create device DropArmDevice class \"System.MemoryDevice\";", null);
				plan.Execute(null);

				// The name is free afterwards, which is the whole of it: the catalog and the catalog store agree that the device
				// the second execution dropped is the one that was there.
				Process.Execute("create device DropArmDevice class \"System.MemoryDevice\";", null);
				Assert.That(Device("DropArmDevice"), Is.Not.Null);
			}
			finally
			{
				Process.UnprepareStatement(plan);
			}
		}
	}
}
