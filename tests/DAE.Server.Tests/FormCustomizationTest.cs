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
	/// Part of L5 of the WS3 test layer: every customization (.dfdx) of a library this fixture carries loads through the
	/// server, and the delta a designer would write from a customized form reproduces that form when merged back. Together
	/// these cover the document device's read path, DilxDocument, the ancestor resolution in FrontendDevice and both
	/// directions of the BOP inheritance machinery (Inheritance.Merge and Inheritance.Diff). What this fixture carries, and
	/// what becomes of the customizations of a library it does not, is EveryCustomizationIsAccountedFor below.
	/// </summary>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class FormCustomizationTest : FrontendFixture
	{
		/// <summary>
		/// The libraries whose customizations this pass leaves alone, each with the reason: this fixture cannot carry
		/// them, so there is no server here to load their documents through.
		/// </summary>
		/// <remarks>
		/// Named by library rather than by document on purpose. The sixteen documents behind these four entries all fail
		/// for the same reason - their library is not registered - so naming them one at a time would be sixteen copies of
		/// one fact, and sixteen cases whose only assertion is that an unregistered library is unregistered. Scope follows
		/// FrontendFixture.FrontendLibraries: adding a library there turns its customizations into cases and makes its
		/// entry here fail, and a library that turns up with a customization and is in neither list fails as well, so a
		/// new one cannot slip past unexamined. EveryCustomizationIsAccountedFor holds all three of those.
		/// </remarks>
		private static readonly Dictionary<string, string> LibrariesNotCarried = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Sample.Forum", "requires a SQL Server device" },
			// The tutorial's tables are created by running its scripts by hand, not by registering the library.
			{ "Sample.Rental", "cannot be registered into a usable state; test-samples-corpus" },
			{ "Sample.Shipping", "is not in the runtime layout (its C# project is not converted)" },
			{ "TestFramework.Coverage", "requires TestFramework.Coverage.Devices, which requires a SQL Server" },
		};

		/// <summary>
		/// The customizations of a carried library that still do not load: the reason, the ticket that owns it, and the
		/// text the failure must carry. A file listed here must still fail to load, and fail in the recorded way - an entry
		/// whose file now loads, or now fails differently, fails its case, so the list drains.
		/// </summary>
		// NOTE: a new entry carries a one-line reason, same shape as these; see ParserRoundTripCorpusTest.AllowList.
		private static readonly Dictionary<string, (string Reason, string Error)> Exclusions = new Dictionary<string, (string Reason, string Error)>(StringComparer.OrdinalIgnoreCase)
		{
			// Name table variables the Security library no longer creates (its Group table is commented out in Security.d4).
			{ "Security.GroupRoleBrowse", ("names Security.GroupRole, which the library no longer creates; debt-security-stale-form-documents", "Unknown identifier \"Security.GroupRole\"") },
			{ "Security.GroupDeviceUserBrowse", ("names Security.GroupDeviceUser, which the library no longer creates; debt-security-stale-form-documents", "Unknown identifier \"Security.GroupDeviceUser\"") },
		};

		/// <summary>Page types the Frontend.PageTypes table offers that the derivation engine does not build.</summary>
		/// <remarks>
		/// PageTypes is catalog content an application can read, and InterfaceBuilder.Structure() is what actually builds
		/// a form; the two lists disagree, and these cases record which one is the real one. The disagreement runs both
		/// ways - Structure() also builds "Report", which PageTypes does not offer - but only a page type a user can pick
		/// and the engine then refuses is worth pinning. Neither list is to be changed without a ticket that says so:
		/// PageTypes is a compatibility surface.
		/// </remarks>
		private static readonly string[] RejectedPageTypes = ["Manage", "Preview", "Filter"];

		/// <summary>A surface of a registered library whose Browse page does derive, so that only the page type is in question.</summary>
		/// <remarks>
		/// That its Browse page derives is not asserted here: FrontendDerivationGoldenTest derives Browse for all 52
		/// surfaces of the carried libraries, so a control case beside this one would assert the same thing again.
		/// </remarks>
		private const string DerivableSurface = "Security.Role";

		private static (string Reason, string Error)? FindExclusion(FormDocument document)
		{
			return Exclusions.TryGetValue(document.QualifiedName, out (string Reason, string Error) exclusion) ? exclusion : null;
		}

		private static IEnumerable<FormDocument> RepositoryCustomizations()
		{
			return FormDocumentCorpusTest.RepositoryFormDocuments().Where(document => document.IsCustomization);
		}

		/// <summary>The customizations in this pass's scope: the ones of a library the fixture registers.</summary>
		private static IEnumerable<FormDocument> CarriedCustomizations()
		{
			HashSet<string> carried = new HashSet<string>(FrontendLibraries, StringComparer.OrdinalIgnoreCase);
			return RepositoryCustomizations().Where(document => carried.Contains(document.LibraryName));
		}

		public static IEnumerable<TestCaseData> Customizations()
		{
			foreach (FormDocument document in CarriedCustomizations())
				yield return new TestCaseData(document).SetName(FormDocumentCorpusTest.RepositoryRelativePath(document));
		}

		public static IEnumerable<TestCaseData> LoadableCustomizations()
		{
			foreach (FormDocument document in CarriedCustomizations())
				if (FindExclusion(document) == null)
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
			(string Reason, string Error)? exclusion = FindExclusion(document);

			if (exclusion != null)
			{
				// An excluded document is one this fixture's server does reach, so its failure is about the document and
				// can be asserted. "Still fails" on its own would also be satisfied by the server having stopped working.
				Exception failure = Failure(() => LoadAndProcess(document.LibraryName, document.DocumentName));
				if (failure == null)
					Assert.Fail(String.Format("\"{0}\" is excluded from this pass ({1}) but now loads. Remove its entry from the exclusion list so that the case asserts the load.", relativePath, exclusion.Value.Reason));
				Assert.That(FlattenMessages(failure), Does.Contain(exclusion.Value.Error),
					String.Format("\"{0}\" still fails to load, but no longer for the recorded reason ({1}).", relativePath, exclusion.Value.Reason));
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
		/// The two lists above and the repository have to agree. Every customization in the tree is either a case here, or
		/// a recorded failure, or the document of a library this fixture cannot carry - and every entry in either list
		/// still names something real, so neither becomes dead text hiding a document nobody looks at.
		/// </summary>
		[Test]
		public void EveryCustomizationIsAccountedFor()
		{
			List<FormDocument> customizations = RepositoryCustomizations().ToList();
			HashSet<string> carried = new HashSet<string>(FrontendLibraries, StringComparer.OrdinalIgnoreCase);
			HashSet<string> cases = new HashSet<string>(CarriedCustomizations().Select(document => document.QualifiedName), StringComparer.OrdinalIgnoreCase);

			Assert.Multiple(() =>
			{
				Assert.That
				(
					customizations.Where(document => !carried.Contains(document.LibraryName) && !LibrariesNotCarried.ContainsKey(document.LibraryName)).Select(FormDocumentCorpusTest.RepositoryRelativePath),
					Is.Empty,
					"These customizations are of a library this fixture neither carries nor explains. Add the library to FrontendFixture.FrontendLibraries, or an entry to LibrariesNotCarried saying why it cannot be carried."
				);
				Assert.That
				(
					LibrariesNotCarried.Keys.Where(library => carried.Contains(library)),
					Is.Empty,
					"These libraries are in FrontendFixture.FrontendLibraries now, so their customizations are cases; remove their LibrariesNotCarried entries."
				);
				Assert.That
				(
					LibrariesNotCarried.Keys.Where(library => !customizations.Any(document => String.Equals(document.LibraryName, library, StringComparison.OrdinalIgnoreCase))),
					Is.Empty,
					"These LibrariesNotCarried entries name no library with a customization in the repository."
				);
				Assert.That
				(
					Exclusions.Keys.Where(name => !cases.Contains(name)),
					Is.Empty,
					"These exclusions name no customization of a carried library. An exclusion is for a document that fails on this server, not for one out of its scope."
				);
			});
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
