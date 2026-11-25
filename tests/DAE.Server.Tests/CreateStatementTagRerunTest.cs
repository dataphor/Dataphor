/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What running the same create statement a second time applies to the tags it is written with. A D4 object can carry tags -
	/// named string settings given by the statement that creates it - and eleven of the compiler's create statements used to hand
	/// the object they built the statement's own tag collection rather than a copy. Altering a tag afterwards edits the live
	/// object's collection in place, so the edit landed in the create statement as well and the next run of that statement created
	/// the object with the altered value instead of the one the statement is written with.
	/// </summary>
	/// <remarks>
	/// The statement has to be run through a plan the caller prepares and executes twice, not through a loop or through the same
	/// text executed twice. A create node executed again inside one program re-registers the catalog object the compiler already
	/// built, so a loop would report the altered value whether or not the statement was edited; and the same text executed twice
	/// no longer reaches a cached plan, because a drop evicts it and the second run recompiles from the text
	/// (SchemaCacheInvalidationTest.DropRoleInvalidatesCachedPlans is that route, and it is green either way). A prepared plan
	/// whose statement changes the catalog recompiles before each execution after the first, and it recompiles the *parsed
	/// statement* the plan holds - which is the tree the alteration wrote into. DeviceClassAttributeRerunTest makes the same
	/// three-way distinction for the class-definition half of this defect.
	/// </remarks>
	[TestFixture]
	public class CreateStatementTagRerunTest : DeviceClassAttributeFixture
	{
		[Test]
		public void TestPreparedCreateStatementExecutedAgainAppliesItsOwnTags()
		{
			IServerStatementPlan plan =
				Process.PrepareStatement("create table TagRerunTable { ID : Integer, key { ID } } tags { Frontend.Title = \"Own\" };", null);
			try
			{
				plan.Execute(null);
				Process.Execute("alter table TagRerunTable alter tags { alter Frontend.Title = \"Altered\" };", null);
				Process.Execute("drop table TagRerunTable;", null);
				plan.Execute(null);
			}
			finally
			{
				Process.UnprepareStatement(plan);
			}

			// Both the live object and the emitted create statement: the first is what the running engine holds, the second is what
			// a catalog rebuild would replay.
			Assert.That(
				CatalogObject<Schema.BaseTableVar>("TagRerunTable").GetMetaDataTag("Frontend.Title").Value,
				Is.EqualTo("Own"),
				"the second execution created the table with the tag the alteration wrote into the statement");
			Assert.That(Script("TagRerunTable"), Does.Contain("Frontend.Title = \"Own\""));
		}
	}
}
