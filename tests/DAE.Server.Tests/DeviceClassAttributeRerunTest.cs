/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What running the same class-definition statement a second time applies. A statement that creates one of an object's class
	/// attributes used to put its own attribute - the object holding the name and the text of the value - straight into the live
	/// class definition, so a later statement that altered that setting wrote the new value into the first statement as well, and
	/// the next run of the first statement applied the altered value instead of the one it is written with.
	/// </summary>
	/// <remarks>
	/// Nothing here opens a transaction: every statement commits, and what is under test is only what the statement still carries
	/// afterwards. Both cases go through <c>IServerProcess</c> rather than <c>ExecuteScript</c>, because those are the two ways a
	/// client (and the Dataphor IDE) runs one statement twice: the engine's cache of compiled plans, and a plan the caller prepared
	/// and holds.
	/// </remarks>
	[TestFixture]
	public class DeviceClassAttributeRerunTest : DeviceClassAttributeFixture
	{
		/// <summary>The statement text that creates the row limit, run twice; the engine recompiles it for the second run.</summary>
		// NOTE: this case no longer reaches a cached plan. It did only because neither alter device nor drop device advanced the
		// plan cache's timestamp; bug-recreated-device-comes-back-as-the-one-that-was-dropped added that bump to both, so the
		// second run now recompiles from the text and this case passes whether or not the attribute is cloned - the prepared-plan
		// case below is the clone's only guard. Kept, deliberately: it is the only case over what a client sees when it runs one
		// statement twice through the plan cache's path, and it fails if both mechanisms ever go at once. Do not read it as a
		// reproduction of either defect on its own.
		[Test]
		public void TestStatementRunAgainFromThePlanCacheAppliesItsOwnValue()
		{
			Process.Execute("create device CachedPlanDevice class \"System.MemoryDevice\";", null);

			string createMaxRowCount = "alter device CachedPlanDevice alter class { create \"MaxRowCount\" = \"100\" };";
			Process.Execute(createMaxRowCount, null);
			Process.Execute("alter device CachedPlanDevice alter class { alter \"MaxRowCount\" = \"200\" };", null);
			Process.Execute("alter device CachedPlanDevice alter class { drop \"MaxRowCount\" };", null);
			Process.Execute(createMaxRowCount, null);

			Assert.That(MaxRowCount("CachedPlanDevice"), Is.EqualTo(100));
			Assert.That(Script("CachedPlanDevice"), Does.Contain("\"MaxRowCount\" = \"100\""));
		}

		/// <summary>A plan the caller prepared and executes twice, which reaches the same statement without the plan cache.</summary>
		[Test]
		public void TestPreparedPlanExecutedAgainAppliesItsOwnValue()
		{
			Process.Execute("create device PreparedPlanDevice class \"System.MemoryDevice\";", null);

			IServerStatementPlan plan = Process.PrepareStatement("alter device PreparedPlanDevice alter class { create \"MaxRowCount\" = \"100\" };", null);
			try
			{
				plan.Execute(null);
				Process.Execute("alter device PreparedPlanDevice alter class { alter \"MaxRowCount\" = \"200\" };", null);
				Process.Execute("alter device PreparedPlanDevice alter class { drop \"MaxRowCount\" };", null);
				plan.Execute(null);
			}
			finally
			{
				Process.UnprepareStatement(plan);
			}

			Assert.That(MaxRowCount("PreparedPlanDevice"), Is.EqualTo(100));
			Assert.That(Script("PreparedPlanDevice"), Does.Contain("\"MaxRowCount\" = \"100\""));
		}
	}
}
