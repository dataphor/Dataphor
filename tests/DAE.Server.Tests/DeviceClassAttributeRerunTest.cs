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
	/// afterwards. Three cases, over the three ways one statement runs more than once: the engine's cache of compiled plans, a plan
	/// the caller prepared and holds, and a loop inside one script, which is a single compiled program executing one node twice.
	/// Two of the three still reproduce the attribute-sharing defect; the first no longer does, and each case says what it covers.
	/// </remarks>
	[TestFixture]
	public class DeviceClassAttributeRerunTest : DeviceClassAttributeFixture
	{
		/// <summary>The statement text that creates the row limit, run twice; the engine recompiles it for the second run.</summary>
		// NOTE: this case no longer reaches a cached plan, and it is no longer a reproduction of the attribute-sharing defect. It
		// reached one only because neither alter device nor drop device advanced the plan cache's timestamp;
		// bug-recreated-device-comes-back-as-the-one-that-was-dropped added that bump to both, so the second run now recompiles
		// from the text. What it covers is what a client (and the Dataphor IDE) sees when it runs one statement twice through the
		// plan cache's path, and it fails if the recompilation and the clone ever go at once. The clone's own reproduction is
		// TestOneProgramRunningTheSameAlterTwiceAppliesItsOwnValue below.
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
		// NOTE: this case still reproduces the attribute-sharing defect, which bug-prepared-create-statement-executes-twice
		// expected it to stop doing. That ticket made a plan whose statement changes the catalog compile afresh before each
		// execution after the first, and an alter device plan is one - but the recompilation compiles the *parsed statement* the
		// plan holds, and this defect is the alteration writing its new value into that very statement, so the second compilation
		// reads 200 out of it and applies 200. Measured both ways on 2026-11-25: red with the clone at
		// AlterNode.AlterClassDefinition reverted (Expected 100, was 200), green with it in place. The loop case below is the
		// reproduction that does not depend on where a recompilation reads its statement from.
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

		/// <summary>The attribute-sharing reproduction, at the layer that still shows it: one compiled program, one node, two executions.</summary>
		/// <remarks>
		/// A loop body is compiled once and its nodes run once per iteration, so neither of the two mechanisms that neutralized the
		/// cases above can reach this one - there is no second preparation to recompile and no cached plan to evict. The first
		/// iteration creates the row limit as 100, alters it to 200 and drops it again; without the clone in
		/// AlterNode.AlterClassDefinition that alteration writes 200 into the create statement's own attribute, and the second
		/// iteration's run of that same node applies 200. The device is created by a statement of its own because a block is
		/// compiled before any of it runs, so an alter cannot name an object a create earlier in the same block makes. Measured
		/// both ways on 2026-11-25: red with the clone reverted (Expected 100, was 200), green with it in place.
		/// </remarks>
		[Test]
		public void TestOneProgramRunningTheSameAlterTwiceAppliesItsOwnValue()
		{
			Process.Execute("create device LoopDevice class \"System.MemoryDevice\";", null);
			Process.Execute
			(
				@"
					begin
						var LIteration := 0;
						while LIteration < 2 do
						begin
							alter device LoopDevice alter class { create ""MaxRowCount"" = ""100"" };
							if LIteration = 0 then
							begin
								alter device LoopDevice alter class { alter ""MaxRowCount"" = ""200"" };
								alter device LoopDevice alter class { drop ""MaxRowCount"" };
							end;
							LIteration := LIteration + 1;
						end;
					end;
				",
				null
			);

			Assert.That(MaxRowCount("LoopDevice"), Is.EqualTo(100));
			Assert.That(Script("LoopDevice"), Does.Contain("\"MaxRowCount\" = \"100\""));
		}
	}
}
