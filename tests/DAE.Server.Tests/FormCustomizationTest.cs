/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using NUnit.Framework;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Part of L5 of the WS3 test layer: every customization (.dfdx) of a registrable library loads through the server,
	/// and the delta a designer would write from a customized form reproduces that form when merged back. Together these
	/// cover the document device's read path, DilxDocument, the ancestor resolution in FrontendDevice and both directions
	/// of the BOP inheritance machinery (Inheritance.Merge and Inheritance.Diff).
	/// </summary>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class FormCustomizationTest : FrontendFixture
	{
		/// <summary>
		/// The customizations this suite cannot load, each with the reason and, where one exists, the ticket that owns it.
		/// A file listed here must still fail to load: an entry whose file now loads fails its case, so the list drains.
		/// </summary>
		// NOTE: a new entry carries a one-line reason, same shape as these; see ParserRoundTripCorpusTest.AllowList.
		private static readonly Dictionary<string, string> Exclusions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// Name table variables the Security library no longer creates (its Group table is commented out in Security.d4).
			{ "Security.GroupRoleBrowse", "names Security.GroupRole, which the library no longer creates; debt-security-stale-form-documents" },
			{ "Security.GroupDeviceUserBrowse", "names Security.GroupDeviceUser, which the library no longer creates; debt-security-stale-form-documents" },
			// The tutorial's tables are created by running its scripts by hand, not by registering the library; test-samples-corpus owns it.
			{ "Sample.Rental.AddSaleCompleted", "Sample.Rental cannot be registered into a usable state; test-samples-corpus" },
			{ "Sample.Rental.BrowseMovieActorCompleted", "Sample.Rental cannot be registered into a usable state; test-samples-corpus" },
			{ "Sample.Rental.ReturnVideoCompleted", "Sample.Rental cannot be registered into a usable state; test-samples-corpus" },
			{ "Sample.Rental.SaleItemFrameCompleted", "Sample.Rental cannot be registered into a usable state; test-samples-corpus" },
			// Sample.Shipping is parked: its C# assembly is not built, so the library is not in the runtime layout.
			{ "Sample.Shipping.BrowseInvoiceItem", "Sample.Shipping is not in the runtime layout (its C# project is not converted)" },
			{ "Sample.Shipping.ContactCategoriesEdit", "Sample.Shipping is not in the runtime layout (its C# project is not converted)" },
			{ "Sample.Shipping.LocationsNeedingItems", "Sample.Shipping is not in the runtime layout (its C# project is not converted)" },
			{ "Sample.Shipping.ProcessSale", "Sample.Shipping is not in the runtime layout (its C# project is not converted)" },
			{ "Sample.Shipping.Receiving", "Sample.Shipping is not in the runtime layout (its C# project is not converted)" },
			{ "Sample.Shipping.ShippingProcess", "Sample.Shipping is not in the runtime layout (its C# project is not converted)" },
			// Needs a SQL Server device.
			{ "Sample.Forum.Start", "Sample.Forum requires a SQL Server device" },
			// Needs TestFramework.Coverage.Devices, which needs MSSQLDevice and so a SQL Server.
			{ "TestFramework.Coverage.CaseResultsExtended", "TestFramework.Coverage requires TestFramework.Coverage.Devices, which requires a SQL Server" },
			{ "TestFramework.Coverage.Cases", "TestFramework.Coverage requires TestFramework.Coverage.Devices, which requires a SQL Server" },
			{ "TestFramework.Coverage.ChooseDeviceInstance", "TestFramework.Coverage requires TestFramework.Coverage.Devices, which requires a SQL Server" },
			{ "TestFramework.Coverage.DeviceCase", "TestFramework.Coverage requires TestFramework.Coverage.Devices, which requires a SQL Server" },
			{ "TestFramework.Coverage.DeviceCaseResultsExtended", "TestFramework.Coverage requires TestFramework.Coverage.Devices, which requires a SQL Server" },
		};

		/// <summary>Page types the Frontend.PageTypes table offers that the derivation engine does not build.</summary>
		/// <remarks>
		/// PageTypes is catalog content an application can read, and InterfaceBuilder.Structure() is what actually builds
		/// a form; the two lists disagree, and these cases record which one is the real one. Neither is to be changed
		/// without a ticket that says so: PageTypes is a compatibility surface.
		/// </remarks>
		private static readonly string[] RejectedPageTypes = ["Manage", "Preview", "Filter"];

		/// <summary>A surface of a registered library whose Browse page does derive, so that only the page type is in question.</summary>
		private const string DerivableSurface = "Security.Role";

		private static string Exclusion(FormDocument document)
		{
			return Exclusions.TryGetValue(document.QualifiedName, out string reason) ? reason : null;
		}

		private static IEnumerable<FormDocument> RepositoryCustomizations()
		{
			return FormDocumentCorpusTest.RepositoryFormDocuments().Where(document => document.IsCustomization);
		}

		public static IEnumerable<TestCaseData> Customizations()
		{
			foreach (FormDocument document in RepositoryCustomizations())
				yield return new TestCaseData(document).SetName(FormDocumentCorpusTest.RepositoryRelativePath(document));
		}

		public static IEnumerable<TestCaseData> LoadableCustomizations()
		{
			foreach (FormDocument document in RepositoryCustomizations())
				if (Exclusion(document) == null)
					yield return new TestCaseData(document).SetName(FormDocumentCorpusTest.RepositoryRelativePath(document));
		}

		/// <summary>
		/// Frontend.LoadAndProcess resolves each ancestor expression against the live catalog and merges the delta over
		/// the result, so one call covers the document device's read path, DilxDocument, FrontendDevice.MergeAncestors
		/// and Inheritance.Merge.
		/// </summary>
		[TestCaseSource(nameof(Customizations))]
		public void LoadsThroughTheServer(FormDocument document)
		{
			string relativePath = FormDocumentCorpusTest.RepositoryRelativePath(document);
			string exclusion = Exclusion(document);

			if (exclusion != null)
			{
				// The exclusion says only that the document does not load here, not why it failed: the libraries whose
				// documents these are mostly are not even in this fixture, so the error is about the library and not
				// about the document. The reason is for the reader; the assertion is what drains the list.
				Exception failure = Failure(() => LoadAndProcess(document.LibraryName, document.DocumentName));
				if (failure == null)
					Assert.Fail(String.Format("\"{0}\" is excluded from this pass ({1}) but now loads. Remove its entry from the exclusion list so that the case asserts the load.", relativePath, exclusion));
				TestContext.Out.WriteLine("Excluded ({0}): {1}", exclusion, FlattenMessages(failure));
				return;
			}

			XDocument form = XDocument.Parse(LoadAndProcess(document.LibraryName, document.DocumentName));
			Assert.That(form.Root.Name.LocalName, Is.EqualTo(FormDocumentCorpusTest.InterfaceElementName),
				String.Format("Loading \"{0}\" produced a document whose root element is <{1}>, not <{2}>.", relativePath, form.Root.Name.LocalName, FormDocumentCorpusTest.InterfaceElementName));
		}

		/// <summary>
		/// The property worth having: the delta a designer writes from a customized form reproduces that form when
		/// merged back over the same ancestors. Differencing the loaded form against the document its delta applied to
		/// and merging that difference back has to land on the form it started from.
		/// </summary>
		[TestCaseSource(nameof(LoadableCustomizations))]
		public void CustomizationRoundTrips(FormDocument document)
		{
			string relativePath = FormDocumentCorpusTest.RepositoryRelativePath(document);
			string loaded = LoadAndProcess(document.LibraryName, document.DocumentName);

			AssertRoundTrips(MergedAncestors(document), loaded, String.Format("the form loaded from \"{0}\"", relativePath));
		}

		/// <summary>The document a customization's delta applies to: its ancestor expressions evaluated and folded, as FrontendDevice.MergeAncestors does.</summary>
		private string MergedAncestors(FormDocument document)
		{
			DilxDocument customization = new DilxDocument();
			customization.Read(File.ReadAllText(document.Path));

			string result = null;
			foreach (string ancestor in customization.Ancestors)
			{
				string resolved = EvaluateString(ancestor);
				result = result == null ? resolved : Merge(result, resolved);
			}
			return result;
		}

		/// <summary>
		/// A base document for the shapes of change the eight customizations in the repository may not all contain.
		/// Kept here rather than as files on disk: a reader should see the documents next to the assertion.
		/// </summary>
		private const string SyntheticBase =
			"""
			<interface xmlns:bop="www.alphora.com/schemas/bop" xmlns:ibop="www.alphora.com/schemas/ibop">
			  <column bop:name="Root">
			    <textbox bop:name="First" title="First" width="20" />
			    <textbox bop:name="Second" title="Second" />
			  </column>
			</interface>
			""";

		[Test]
		public void SyntheticRoundTrip_ElementAdded()
		{
			AssertRoundTrips
			(
				SyntheticBase,
				"""
				<interface xmlns:bop="www.alphora.com/schemas/bop" xmlns:ibop="www.alphora.com/schemas/ibop">
				  <column bop:name="Root">
				    <textbox bop:name="First" title="First" width="20" />
				    <textbox bop:name="Added" title="Added" />
				    <textbox bop:name="Second" title="Second" />
				  </column>
				</interface>
				""",
				"an element added"
			);
		}

		[Test]
		public void SyntheticRoundTrip_SiblingsReordered()
		{
			AssertRoundTrips
			(
				SyntheticBase,
				"""
				<interface xmlns:bop="www.alphora.com/schemas/bop" xmlns:ibop="www.alphora.com/schemas/ibop">
				  <column bop:name="Root">
				    <textbox bop:name="Second" title="Second" />
				    <textbox bop:name="First" title="First" width="20" />
				  </column>
				</interface>
				""",
				"two sibling elements reordered"
			);
		}

		[Test]
		public void SyntheticRoundTrip_AttributeRemoved()
		{
			AssertRoundTrips
			(
				SyntheticBase,
				"""
				<interface xmlns:bop="www.alphora.com/schemas/bop" xmlns:ibop="www.alphora.com/schemas/ibop">
				  <column bop:name="Root">
				    <textbox bop:name="First" title="First" />
				    <textbox bop:name="Second" title="Second" />
				  </column>
				</interface>
				""",
				"an attribute removed"
			);
		}

		/// <summary>
		/// The fourth shape, recorded rather than asserted as a round trip: BOP inheritance has no operation that removes
		/// an element. Inheritance.Diff walks the children of the modified document, so a child that only the original has
		/// contributes nothing to the delta, and the merge leaves it where it was. That is the machinery as designed - a
		/// node of a derived form is overridden, never deleted - and measuring it here means a later attempt to add a
		/// deletion operation has to come past this case rather than silently change what a customization can say.
		/// </summary>
		[Test]
		public void SyntheticRoundTrip_ElementRemoved_IsNotExpressible()
		{
			const string modified =
				"""
				<interface xmlns:bop="www.alphora.com/schemas/bop" xmlns:ibop="www.alphora.com/schemas/ibop">
				  <column bop:name="Root">
				    <textbox bop:name="First" title="First" width="20" />
				  </column>
				</interface>
				""";

			string delta = Difference(SyntheticBase, modified);
			Assert.That(ParseIgnoringWhitespace(delta).Root.HasElements, Is.False,
				String.Format("The difference against a document with an element removed is no longer empty:\n{0}", delta));
			Assert.That(XNode.DeepEquals(ParseIgnoringWhitespace(Merge(SyntheticBase, delta)), ParseIgnoringWhitespace(SyntheticBase)), Is.True,
				"Merging that difference back no longer leaves the original unchanged.");
		}

		private void AssertRoundTrips(string original, string modified, string what)
		{
			string delta = Difference(original, modified);
			string remerged = Merge(original, delta);

			XDocument expected = ParseIgnoringWhitespace(modified);
			XDocument actual = ParseIgnoringWhitespace(remerged);
			if (!XNode.DeepEquals(expected, actual))
				Assert.Fail(String.Format("Merging the difference back did not reproduce {0}.\nDifference:\n{1}\nExpected:\n{2}\nGot:\n{3}", what, delta, expected, actual));
		}

		// XmlReader rather than XDocument.Parse: the two sides of a merge are written by different writers and indent
		// differently, and XNode.DeepEquals counts whitespace text nodes.
		private static XDocument ParseIgnoringWhitespace(string text)
		{
			using (XmlReader reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { IgnoreWhitespace = true }))
				return XDocument.Load(reader);
		}

		/// <summary>
		/// The one document in the repository with multi-line script and quotes inside an attribute value, asserted on
		/// the attribute itself rather than only through document equality, so that a change of reader or writer settings
		/// cannot eat them silently. Line breaks and quotes come through because the file writes them as character
		/// references, which XML attribute-value normalization leaves alone; the one literal tab in the file does not,
		/// because normalization replaces literal whitespace in an attribute value with a space before anything in
		/// Dataphor sees it. That is what the conversion in Dilx.cs protects against for a tab written as &amp;#x9; -
		/// see FormDocumentCorpusTest.CustomizationReadKeepsCharacterReferencedWhitespace - and it cannot recover a tab
		/// a file stores literally.
		/// </summary>
		[Test]
		public void MultiLineScriptInAnAttributeSurvivesLoading()
		{
			XElement action =
				XDocument.Parse(LoadAndProcess("Sample.DetailAdd", "DetailBrowse")).Descendants("scriptaction")
					.Single(element => (string)element.Attribute(Persistence.XmlBOPName) == "AddCategory");
			string script = (string)action.Attribute("script");

			Assert.That(script, Is.Not.Null, "The scriptaction has no script attribute.");
			Assert.That(script, Does.Contain("AParams[\"AForm\"]"), "The quotes in the script did not survive loading.");
			// One substring covering all three: the line break kept, the literal tab arriving as a single space, the quotes kept.
			Assert.That(script, Does.Contain("\r\n throw new Exception(\"Form is null\");\r\n"), "The script's line breaks or indentation changed.");
		}

		/// <summary>
		/// The Frontend.PageTypes table offers page types the derivation engine refuses to build. Pinning the refusal
		/// records which of the two lists is the real one, and that the error names the page type, so that a caller can
		/// tell a bad page type from a bad surface.
		/// </summary>
		[TestCaseSource(nameof(RejectedPageTypes))]
		public void DeriveRejectsThePageType(string pageType)
		{
			Exception failure = Failure(() => EvaluateString("Frontend.Derive(ASurface, APageType)", ("ASurface", DerivableSurface), ("APageType", pageType)));

			Assert.That(failure, Is.Not.Null, String.Format("Deriving a \"{0}\" page of {1} succeeded; the derivation engine has no builder for that page type.", pageType, DerivableSurface));
			Assert.That(FlattenMessages(failure), Does.Contain(String.Format("\"{0}\"", pageType)),
				String.Format("Deriving a \"{0}\" page of {1} failed without naming the page type.", pageType, DerivableSurface));
		}

		/// <summary>
		/// The other side of the case above: a page type the engine does accept, so that the refusal is known to be
		/// about the page type and not about the surface.
		/// </summary>
		[Test]
		public void DeriveAcceptsTheBrowsePageType()
		{
			string derived = EvaluateString("Frontend.Derive(ASurface, APageType)", ("ASurface", DerivableSurface), ("APageType", "Browse"));
			Assert.That(XDocument.Parse(derived).Root.Name.LocalName, Is.EqualTo(FormDocumentCorpusTest.InterfaceElementName));
		}

		/// <summary>An exclusion for a file that is no longer in the tree is dead text; the entry goes with the file.</summary>
		[Test]
		public void EveryExclusionNamesADocumentInTheTree()
		{
			HashSet<string> found = new HashSet<string>(RepositoryCustomizations().Select(document => document.QualifiedName), StringComparer.OrdinalIgnoreCase);
			Assert.That(Exclusions.Keys.Where(name => !found.Contains(name)), Is.Empty, "These exclusions name no customization in the repository.");
		}

		/// <summary>Runs <paramref name="body"/> and returns what it threw, or null.</summary>
		private static Exception Failure(Action body)
		{
			try
			{
				body();
				return null;
			}
			catch (Exception exception)
			{
				return exception;
			}
		}
	}
}
