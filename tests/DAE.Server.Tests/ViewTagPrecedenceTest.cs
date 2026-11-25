/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A view's own tags take precedence over every tag it inherits from its source. A D4 object can carry tags - named string
	/// settings other parts of the system read, such as the Frontend.* tags the user interface layer derives a form from - and a
	/// view both carries tags of its own and inherits the tags of the table variable its expression is built over. Inheritance
	/// fills gaps; where both declare a tag of the same name, the view's value is the one that must survive.
	/// </summary>
	/// <remarks>
	/// This states the rule on two tags; FrontendDerivationGoldenTest is the end-to-end cover, where the same rule shows up as
	/// the title a derived form carries and as which page type a detail link derives. The second tag is one whose value is read
	/// as a boolean, because that is the shape of tag the derivation branches on.
	/// </remarks>
	[TestFixture]
	public class ViewTagPrecedenceTest : DeviceClassAttributeFixture
	{
		[Test]
		public void TestViewTagsWinOverTheTagsInheritedFromItsSource()
		{
			Process.Execute("create table ViewTagSource { ID : Integer, key { ID } } tags { Frontend.Title = \"Source Title\", Frontend.UseList = \"true\" };", null);
			Process.Execute("create view ViewTagView ViewTagSource tags { Frontend.Title = \"View Title\", Frontend.UseList = \"false\" };", null);

			// Both the live object and the emitted create statement: the first is what the running engine hands the derivation,
			// the second is what the catalog stores for the view and what a rebuild would replay.
			Schema.DerivedTableVar view = CatalogObject<Schema.DerivedTableVar>("ViewTagView");
			Assert.That(view.GetMetaDataTag("Frontend.Title").Value, Is.EqualTo("View Title"));
			Assert.That(view.GetMetaDataTag("Frontend.UseList").Value, Is.EqualTo("false"));

			string script = Script("ViewTagView");
			Assert.That(script, Does.Contain("Frontend.Title = \"View Title\""));
			Assert.That(script, Does.Contain("Frontend.UseList = \"false\""));
		}
	}
}
