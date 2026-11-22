/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>One page of one table variable: what this pass derives, and what it compares against a checked-in file.</summary>
	public sealed class DerivationCase
	{
		public DerivationCase(string libraryName, string surfaceName, string pageType)
		{
			LibraryName = libraryName;
			SurfaceName = surfaceName;
			PageType = pageType;
		}

		/// <summary>The D4 library the table variable belongs to.</summary>
		public string LibraryName { get; }

		/// <summary>
		/// The table variable's catalog name, qualified with its library (Security.UserRole), which is what
		/// Frontend.Derive takes as its expression.
		/// </summary>
		public string SurfaceName { get; }

		/// <summary>The kind of form to build: one of <see cref="FrontendDerivationGoldenTest.PageTypes"/>.</summary>
		public string PageType { get; }

		/// <summary>The name without the library qualification, which is what the golden's file name carries.</summary>
		public string UnqualifiedSurfaceName =>
			SurfaceName.StartsWith(LibraryName + ".", StringComparison.OrdinalIgnoreCase) ? SurfaceName.Substring(LibraryName.Length + 1) : SurfaceName;

		/// <summary>
		/// The golden's path below tests/goldens/frontend, forward-slashed: the key the two sets are compared on and the
		/// name a message quotes.
		/// </summary>
		public string GoldenRelativePath =>
			String.Format("{0}/{1}.{2}{3}", LibraryName, UnqualifiedSurfaceName, PageType, FrontendDerivationGoldenTest.GoldenExtension);

		public override string ToString() => String.Format("{0} of {1}", PageType, SurfaceName);
	}

	/// <summary>
	/// The golden half of L5 of the WS3 test layer: every page type of every table variable the carried libraries define
	/// is derived through Frontend.Derive and compared, character for character, with a checked-in file. A change anywhere
	/// in the derivation engine - elaboration, structuring, layout, document production - then shows up as a reviewable
	/// difference in these files rather than as a form a user finds wrong.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A golden is not the engine's literal output. Frontend.Derive returns XmlDocument.OuterXml, which is one line with
	/// no indentation, and a 3 KB single line makes every change look like a whole-file rewrite - which defeats reviewing
	/// these files in a diff. So the stored form is the derived document printed: parsed and written back indented, with
	/// tabs and newlines between elements and nothing else altered. Both sides of the comparison go through the same
	/// printer, so a real change in the form still fails; only whitespace between elements is normalized away.
	/// </para>
	/// <para>
	/// NOTE: no element of a derived form has text content - every one is empty or holds only other elements - so the
	/// printer has no mixed content to indent into, and re-indenting cannot change what a form says. If the derivation
	/// engine ever emits an element with text in it, printing would reflow that text: give the printer
	/// PreserveWhitespace then, and regenerate.
	/// </para>
	/// <para>
	/// No other normalization is applied, and none is needed: measured on 2026-11-21 over the whole corpus below, all
	/// 416 forms came out byte-identical on two independently created server instances, and deriving twice in one
	/// instance gave identical text as well - that half measured over the 369 forms that derived before K36 was fixed -
	/// so no timestamp, identifier or machine name reaches the output and tag ordering is insertion-ordered. If a field
	/// ever does drift, add a normalizer then and name the field; do not add one pre-emptively.
	/// </para>
	/// <para>
	/// The corpus is 52 surfaces across eight of the ten carried libraries - System.Platform and SimpleDevice define no
	/// table variable of their own - which is 416 cases, every one of which derives.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class FrontendDerivationGoldenTest : FrontendFixture
	{
		/// <summary>Set to 1 to write the goldens instead of comparing them; see AGENTS.md "Build and test".</summary>
		public const string UpdateVariable = "DATAPHOR_UPDATE_FRONTEND_GOLDENS";

		/// <summary>
		/// The extension a golden carries. Deliberately not .dfd: these files are not form documents, so nothing can
		/// mistake them for library content and the repository-wide .dfd walk in FormDocumentCorpusTest leaves them alone.
		/// </summary>
		public const string GoldenExtension = ".xml";

		/// <summary>This pass's own tree below <see cref="TestEnvironment.GoldensDirectory"/>.</summary>
		public const string GoldensFolderName = "frontend";

		/// <summary>
		/// The page types the derivation engine builds. InterfaceBuilder.Structure() refuses the other three the
		/// Frontend.PageTypes table offers (Manage, Preview, Filter), which FormCustomizationTest.DeriveRejectsThePageType
		/// pins; adding one here would assert that refusal a second time, as an unexplained failure.
		/// </summary>
		public static readonly string[] PageTypes = ["Browse", "List", "Report", "OrderBrowse", "Add", "Edit", "View", "Delete"];

		/// <summary>
		/// The libraries whose table variables this pass derives: the ones the fixture's server carries. Two of them,
		/// System.Platform and SimpleDevice, define no table variable of their own and so contribute no case.
		/// </summary>
		// NOTE: accepted tradeoff - the System library's surfaces are left out, although deriving its catalog tables is
		// what the IDE does most: those forms change with every engine change, so their goldens would churn on work that
		// has nothing to do with the Frontend, while the libraries named here change only when a library changes.
		// Revisit if a derivation defect is ever found on a catalog table and not on a library table - but note that
		// "System" cannot simply be added here: this is the fixture's list, which also names the library folders to copy,
		// and System is the built-in catalog library with no folder of its own. Give this field an array of its own, with
		// "System" added, and generate its goldens.
		public static readonly string[] CorpusLibraries = FrontendLibraries;

		/// <summary>A surface whose Browse page derives, for the cases that are about something other than the surface.</summary>
		private const string DerivableSurface = "Security.Role";

		/// <summary>
		/// A derivation that is known to fail, with the ticket that owns the cause and a substring the error must carry.
		/// </summary>
		/// <remarks>
		/// A row is permission for the cases it matches to fail, not a requirement: "*" matches every page type or every
		/// surface, and a matched case that derives anyway is compared with its golden like any other. A row that no
		/// longer matches a single failing case fails this pass, so the row drains when the defect is fixed - and a case
		/// that fails with no row matching it fails with its error, so a new defect cannot slip in as a missing file.
		/// </remarks>
		// NOTE: empty today - every case of the corpus derives, so every case has a golden. This is where the next
		// derivation defect is recorded rather than left as a missing file: an entry carries the ticket that owns the
		// cause and a substring the failure must contain, as the row for bug-orderbrowse-derivation-fails did until that
		// defect was fixed; see ParserRoundTripCorpusTest.AllowList for a live example of the shape.
		private static readonly ExpectedFailure[] ExpectedFailures = [];

		/// <summary>One row of <see cref="ExpectedFailures"/>.</summary>
		private sealed class ExpectedFailure
		{
			public ExpectedFailure(string pageType, string surfaceName, string ticket, string error)
			{
				PageType = pageType;
				SurfaceName = surfaceName;
				Ticket = ticket;
				Error = error;
			}

			/// <summary>A page type, or "*" for every page type.</summary>
			public string PageType { get; }

			/// <summary>A surface's qualified name, or "*" for every surface.</summary>
			public string SurfaceName { get; }

			/// <summary>The slug of the ticket that owns the cause.</summary>
			public string Ticket { get; }

			/// <summary>A substring the failure's messages must contain, so that "still fails" cannot be satisfied by a different fault.</summary>
			public string Error { get; }

			public bool Matches(DerivationCase derivation)
			{
				return MatchesPattern(PageType, derivation.PageType) && MatchesPattern(SurfaceName, derivation.SurfaceName);
			}

			private static bool MatchesPattern(string pattern, string value)
			{
				return pattern == AnyPattern || String.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
			}

			public override string ToString() => String.Format("{0} of {1} ({2})", PageType, SurfaceName, Ticket);
		}

		/// <summary>What an <see cref="ExpectedFailure"/> writes for "every page type" or "every surface".</summary>
		private const string AnyPattern = "*";

		private static bool Updating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

		/// <summary>This pass's golden tree, tests/goldens/frontend.</summary>
		private static string GoldensDirectory => Path.Combine(TestEnvironment.GoldensDirectory, GoldensFolderName);

		/// <summary>
		/// The table variables a library defines: the surfaces a form can be derived from. Generated table variables are
		/// left out - those are the ones the engine creates for a constraint or a key, not something a user browses.
		/// </summary>
		/// <remarks>
		/// Asked of the catalog rather than read from a checked-in list, so that a table added to a library turns up here
		/// as new cases. Takes the library name as an argument so that a corpus of application libraries outside this
		/// repository (compat-corpus-harness) adds a caller rather than a second query.
		/// </remarks>
		public static List<string> EnumerateSurfaces(IServerProcess process, string libraryName)
		{
			List<string> result = new List<string>();
			IServerCursor cursor =
				process.OpenCursor
				(
					String.Format("select TableVars where Library_Name = '{0}' and not(IsGenerated) over {{ Name }} order by {{ Name }}", libraryName),
					null
				);
			try
			{
				while (cursor.Next())
					using (IRow row = cursor.Select())
						result.Add((string)row["Name"]);
			}
			finally
			{
				process.CloseCursor(cursor);
			}
			return result;
		}

		/// <summary>Every case this pass covers: each carried library's surfaces crossed with the page types.</summary>
		private List<DerivationCase> Cases()
		{
			List<DerivationCase> result = new List<DerivationCase>();
			foreach (string library in CorpusLibraries)
				foreach (string surface in EnumerateSurfaces(Process, library))
					foreach (string pageType in PageTypes)
						result.Add(new DerivationCase(library, surface, pageType));
			return result;
		}

		/// <summary>The form Frontend.Derive builds, as the single-line XML string the operator returns.</summary>
		private string Derive(DerivationCase derivation)
		{
			return Derive(derivation.SurfaceName, derivation.PageType);
		}

		private string Derive(string surfaceName, string pageType)
		{
			return Derive(Process, surfaceName, pageType);
		}

		/// <summary>The same derivation on a process this fixture does not own.</summary>
		/// <remarks>
		/// Static and taking the process for the reason EnumerateSurfaces above gives: a corpus pass that is not a
		/// FrontendFixture - SamplesCorpusTest, and the corpus outside this repository that compat-corpus-harness points
		/// at - adds a caller rather than a second copy of the expression and its parameter names. Values go in as
		/// parameters rather than as literals for the reason EvaluateString gives: a derived form is XML holding quotes of
		/// both kinds.
		/// </remarks>
		public static string Derive(IServerProcess process, string surfaceName, string pageType)
		{
			DataParams parameters = new DataParams();
			parameters.Add(DataParam.Create(process, "ASurface", surfaceName));
			parameters.Add(DataParam.Create(process, "APageType", pageType));
			return (string)((ServerProcess)process).Evaluate("Frontend.Derive(ASurface, APageType)", parameters).AsNative;
		}

		/// <summary>
		/// The stored form of a golden: the derived document printed, indented with tabs and newlines, ending in a
		/// newline. See the remarks on this class for why the stored form is not the engine's literal output.
		/// </summary>
		/// <remarks>
		/// NewLineHandling stays at its default, Replace, which writes a line break inside an attribute value as a
		/// character reference - a derived form has them, because an expression attribute holds emitted D4 and a
		/// scriptaction's script holds C#. The XML declaration is omitted because the derived document has none: the
		/// derivation engine builds an XmlDocument and never adds one, and a declaration this printer invented would be
		/// a difference between the golden and the thing it stands for. A golden's file encoding is settled where it is
		/// written, not here, because a StringBuilder is UTF-16 whatever the writer is told.
		/// </remarks>
		public static string Print(string derived)
		{
			XmlWriterSettings settings =
				new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\n",
					OmitXmlDeclaration = true,
				};
			StringBuilder result = new StringBuilder();
			using (XmlWriter writer = XmlWriter.Create(result, settings))
				XDocument.Parse(derived).Save(writer);
			result.Append('\n');
			return result.ToString();
		}

		private static string GoldenPath(string relativePath)
		{
			return Path.Combine(GoldensDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
		}

		/// <summary>Reads a golden as text, or null if there is no such file.</summary>
		private static string ReadGolden(string relativePath)
		{
			string path = GoldenPath(relativePath);
			return File.Exists(path) ? File.ReadAllText(path) : null;
		}

		/// <summary>Writes a golden: UTF-8 with no byte-order mark, carrying the newlines the printer put in it.</summary>
		private static void WriteGolden(string relativePath, string text)
		{
			string path = GoldenPath(relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text, new UTF8Encoding(false));
		}

		/// <summary>
		/// Every file in the golden tree, forward-slashed and relative to it. Every file, not only the ones with the
		/// golden extension, so that a stray file is reported rather than left to accumulate unnoticed.
		/// </summary>
		private static List<string> ExistingGoldens()
		{
			if (!Directory.Exists(GoldensDirectory))
				return new List<string>();
			return
				Directory.EnumerateFiles(GoldensDirectory, "*", SearchOption.AllDirectories)
					.Select(path => Path.GetRelativePath(GoldensDirectory, path).Replace('\\', '/'))
					.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
					.ToList();
		}

		/// <summary>
		/// The pass itself: every case derives and matches its golden, or fails in a recorded way; and the set of golden
		/// files is exactly the set the cases claim, so neither a form nobody looks at nor a file nothing produces can
		/// sit in the tree.
		/// </summary>
		/// <remarks>
		/// One case rather than one per surface because the three questions - does it derive, does it match, do the two
		/// sets agree - are answered by one pass over the corpus, and because the set comparison needs every case's
		/// answer. A test that needed its siblings to have run first would give the wrong answer under the name filters
		/// AGENTS.md runs the suite with.
		/// </remarks>
		[Test]
		public void EveryDerivedFormMatchesItsGolden()
		{
			bool updating = Updating;
			List<string> problems = new List<string>();
			HashSet<string> claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<ExpectedFailure, int> failuresSeen = ExpectedFailures.ToDictionary(failure => failure, failure => 0);
			int written = 0;
			int deleted = 0;

			foreach (DerivationCase derivation in Cases())
			{
				string derived;
				try
				{
					derived = Derive(derivation);
				}
				catch (Exception exception)
				{
					problems.AddRange(Unexpected(derivation, exception, failuresSeen));
					continue;
				}

				claimed.Add(derivation.GoldenRelativePath);
				string printed = Print(derived);

				if (updating)
				{
					WriteGolden(derivation.GoldenRelativePath, printed);
					written++;
					continue;
				}

				string golden = ReadGolden(derivation.GoldenRelativePath);
				if (golden == null)
					problems.Add(String.Format("{0}: no golden file. Create \"{1}\" by running with {2}=1.", derivation, derivation.GoldenRelativePath, UpdateVariable));
				else if (golden != printed)
					problems.Add(String.Format("{0}: the derived form no longer matches \"{1}\". {2}", derivation, derivation.GoldenRelativePath, FirstDifference(golden, printed)));
			}

			foreach (string existing in ExistingGoldens())
			{
				if (claimed.Contains(existing))
					continue;
				if (updating)
				{
					File.Delete(GoldenPath(existing));
					deleted++;
				}
				else
					problems.Add(String.Format("\"{0}\" is a golden no case produces. Delete it, or find out which surface or page type stopped being derived.", existing));
			}

			if (updating)
				RemoveEmptyDirectories(GoldensDirectory);

			foreach (KeyValuePair<ExpectedFailure, int> entry in failuresSeen)
				if (entry.Value == 0)
					problems.Add(String.Format("The expected-failure row {0} matches no case that fails that way any more. Remove the row; its cases now have goldens.", entry.Key));

			if (updating)
				Assert.Fail(String.Format("{0}=1: wrote {1} golden file(s) and deleted {2}. An update run always fails, so that it cannot be mistaken for a passing one - the diff is the review, so read it before committing it.{3}", UpdateVariable, written, deleted, Report(problems)));

			if (problems.Count > 0)
				Assert.Fail(String.Format("{0} derived form(s) or golden file(s) do not agree with the checked-in set. Run with {1}=1 to rewrite the goldens, then read the diff before committing it.{2}", problems.Count, UpdateVariable, Report(problems)));
		}

		/// <summary>
		/// What to say about a derivation that threw: nothing when a row expects it (the row is counted instead), and
		/// otherwise one problem naming either the unrecorded failure or the row whose error text no longer matches.
		/// </summary>
		private static IEnumerable<string> Unexpected(DerivationCase derivation, Exception exception, Dictionary<ExpectedFailure, int> failuresSeen)
		{
			string messages = FlattenMessages(exception);
			ExpectedFailure expected = ExpectedFailures.FirstOrDefault(failure => failure.Matches(derivation) && messages.Contains(failure.Error));
			if (expected != null)
			{
				failuresSeen[expected]++;
				yield break;
			}

			ExpectedFailure wrongError = ExpectedFailures.FirstOrDefault(failure => failure.Matches(derivation));
			yield return
				wrongError == null
					? String.Format("{0}: deriving failed and no expected-failure row covers it: {1}", derivation, messages)
					: String.Format("{0}: deriving still fails, but not for the recorded reason {1}: {2}", derivation, wrongError, messages);
		}

		/// <summary>At most this many of the problems are listed; a failure that names every file of the corpus is unreadable.</summary>
		private const int ReportLimit = 25;

		private static string Report(List<string> problems)
		{
			if (problems.Count == 0)
				return String.Empty;
			StringBuilder result = new StringBuilder();
			foreach (string problem in problems.Take(ReportLimit))
				result.Append("\n  ").Append(problem);
			if (problems.Count > ReportLimit)
				result.Append(String.Format("\n  ... and {0} more.", problems.Count - ReportLimit));
			return result.ToString();
		}

		/// <summary>Where two golden texts first differ, as a line number and the two lines, so that a failure says what changed.</summary>
		private static string FirstDifference(string expected, string actual)
		{
			string[] expectedLines = expected.Split('\n');
			string[] actualLines = actual.Split('\n');
			for (int index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
			{
				string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<end of file>";
				string actualLine = index < actualLines.Length ? actualLines[index] : "<end of file>";
				if (expectedLine != actualLine)
					return String.Format("First difference at line {0}:\n    golden:  {1}\n    derived: {2}", index + 1, Excerpt(expectedLine), Excerpt(actualLine));
			}
			return "The two texts differ although no line does, so the difference is in the line endings.";
		}

		private const int ExcerptLength = 160;

		private static string Excerpt(string line)
		{
			return line.Length <= ExcerptLength ? line : line.Substring(0, ExcerptLength) + "...";
		}

		/// <summary>Removes the directories an update emptied, so that a library that stops contributing leaves no folder behind.</summary>
		private static void RemoveEmptyDirectories(string directory)
		{
			if (!Directory.Exists(directory))
				return;
			foreach (string subdirectory in Directory.EnumerateDirectories(directory))
			{
				RemoveEmptyDirectories(subdirectory);
				if (!Directory.EnumerateFileSystemEntries(subdirectory).Any())
					Directory.Delete(subdirectory);
			}
		}

		/// <summary>
		/// The property the goldens rest on: deriving the same page of the same surface twice gives the same text. It
		/// holds today by measurement rather than by construction, so a change that introduced a timestamp or made a tag
		/// collection iterate a hash table should fail here rather than as a mismatch on every golden at once.
		/// </summary>
		/// <remarks>
		/// The derivation cache is cleared between the two derivations: a session caches the document it built for a
		/// seed, so without the clear the second call would hand back the first call's document and the case would
		/// assert nothing.
		/// </remarks>
		[Test]
		public void DerivingTheSameFormTwiceGivesTheSameText()
		{
			string first = Derive(DerivableSurface, "Browse");
			ExecuteScript("Frontend.ClearDerivationCache();");
			string second = Derive(DerivableSurface, "Browse");

			Assert.That(second, Is.EqualTo(first), String.Format("Deriving the Browse page of {0} twice gave two different documents.", DerivableSurface));
		}

		/// <summary>A surface and page type whose derived form holds a line break inside an attribute value.</summary>
		private const string MultiLineAttributeSurface = DerivableSurface;
		private const string MultiLineAttributePageType = "Browse";

		/// <summary>
		/// A derived form carries line breaks inside attribute values - an expression attribute holds emitted D4 - and
		/// printing must not eat them. Asserted on the attribute values themselves rather than only through the golden
		/// comparison, so that a change of writer settings cannot quietly rewrite every golden at once.
		/// </summary>
		[Test]
		public void PrintingKeepsALineBreakInsideAnAttribute()
		{
			List<string> derived = AttributeValues(XDocument.Parse(Derive(MultiLineAttributeSurface, MultiLineAttributePageType)));

			Assert.That
			(
				derived.Any(value => value.Contains('\n')),
				Is.True,
				String.Format("No attribute of the {0} page of {1} holds a line break any more, so this case no longer covers what it is for; point it at a form that does.", MultiLineAttributePageType, MultiLineAttributeSurface)
			);
			Assert.That
			(
				AttributeValues(XDocument.Parse(Print(Derive(MultiLineAttributeSurface, MultiLineAttributePageType)))),
				Is.EqualTo(derived),
				"Printing a derived form and reading it back changed an attribute value."
			);
		}

		/// <summary>Every attribute value in the document, in document order, so that two documents can be compared on them alone.</summary>
		private static List<string> AttributeValues(XDocument document)
		{
			return document.Descendants().SelectMany(element => element.Attributes()).Select(attribute => attribute.Value).ToList();
		}
	}
}
