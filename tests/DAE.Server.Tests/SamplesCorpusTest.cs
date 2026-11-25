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

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Layer L7 of the WS3 test layer, for the example applications this repository ships: every library folder under
	/// samples/ gets a server of its own carrying only what that sample requires, is registered there, and has a form
	/// derived from every page type of every table variable it defines. A change that breaks one of the examples then
	/// fails the test run rather than being found when somebody opens it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// FrontendDerivationGoldenTest derives the same forms and compares each with a checked-in file, and its goldens
	/// already cover every surface any sample defines today. What this pass adds that the golden pass cannot is breadth
	/// and isolation: the libraries are found by walking a directory, so a sample library added to samples/ becomes a
	/// case with no code change where the golden pass's library list is hand-written; each sample gets a fresh server
	/// carrying its own requisite closure and nothing else, so a sample that only registers because a sibling created
	/// something first fails here; and four of the twelve samples are not in the golden fixture at all.
	/// </para>
	/// <para>
	/// So the derivation check here is deliberately shallow - a form must be produced, parse as XML and have the right
	/// root element. Depth is the golden pass's job, and a second set of expected documents would be a second set to keep
	/// in step.
	/// </para>
	/// <para>
	/// The discovery rule and the requisite-closure walk are in CorpusLibrary, which knows nothing about samples, this
	/// repository or NUnit: the WS13 work item compat-corpus-harness points the same two at a corpus directory outside
	/// this repository, and that is the whole of what this pass owes it.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: each case starts a server, and two engines in one process share the engine's static state
	// (see the note on TestServerInstance).
	[TestFixture]
	public class SamplesCorpusTest
	{
		/// <summary>The repository folder holding the example applications, one library folder each.</summary>
		public const string SamplesFolderName = "samples";

		/// <summary>The library whose registration has to be forced ahead of the rest; see <see cref="Register"/>.</summary>
		private const string FrontendLibraryName = "Frontend";

		/// <summary>The one page type whose derived document is a report rather than an interface.</summary>
		private const string ReportPageType = "Report";

		/// <summary>The document element of a derived report page.</summary>
		private const string ReportElementName = "report";

		/// <summary>
		/// The device libraries whose presence in a requisite closure means the sample needs a running SQL DBMS, which is
		/// the premise the <see cref="NeedsADatabaseServer"/> entries rest on.
		/// </summary>
		/// <remarks>
		/// PGSQLDevice is named although no sample requires it and the build does not assemble it yet (device-pgsql-port):
		/// when it arrives, a sample requiring it needs a database server for the same reason, and a closure naming a
		/// library the layout has no folder for fails its case with that message rather than silently passing.
		/// </remarks>
		private static readonly string[] SqlDeviceLibraries = ["MSSQLDevice", "PGSQLDevice"];

		/// <summary>
		/// The samples whose registration needs a running SQL Server, with the reason each does. Where none is configured
		/// their cases report Inconclusive and start no server; where one is, they are attempted and checked against
		/// <see cref="FailsWithADatabaseServer"/>.
		/// </summary>
		// NOTE: accepted tradeoff - with no server configured these entries are not attempted at all, although a fresh
		// server would show whether they still register. Measured 2026-11-22 on a machine with no SQL Server reachable:
		// failing to reach one costs 33.6 s for Sample.Forum and 21.0 s for Sample.Pubs - 54.6 s, more than the whole
		// rest of this pass put together, for the driver's connect timeout twice over against a server that is not
		// there; Sample.Shipping, which joined the list when library-sample-shipping-convert built its assembly, adds a
		// third such wait. What makes it acceptable: no device here can connect from any machine as written (Sample.Pubs
		// names a developer's own machine, Sample.Shipping a database that no machine has), the samples may not be
		// edited to shorten the timeout, and every .d4 file in the tree is already parsed by ParserRoundTripCorpusTest.
		// test-device-live, which owns running anything against a real SQL Server, made the other half of this real:
		// where DATAPHOR_TEST_MSSQL names a server the three are attempted, so a regression that broke their
		// registration in some other way shows up as a failure that does not match the recorded text.
		private static readonly Dictionary<string, string> NeedsADatabaseServer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Sample.Forum", "creates a SQL Server device in its registration script, so registering it needs a running SQL Server." },
			{ "Sample.Pubs", "creates a SQL Server device naming a developer's own machine, so registering it needs a running SQL Server." },
			{ "Sample.Shipping", "creates a SQL Server device in its registration script, so registering it needs a running SQL Server." },
		};

		/// <summary>
		/// For each sample above, an ordinal substring of the error its registration still fails with when a server IS
		/// configured. Every one of the three names a server of its own in its own script rather than the one
		/// DATAPHOR_TEST_MSSQL points at, and a sample may not be edited to change that, so none of them can reach the
		/// configured server: the useful thing a configured run adds is that the failure is the recorded one and not some
		/// new breakage. Reported as a warning, the way a known-failure coverage script is; a sample that registers, or
		/// fails differently, fails the run.
		/// </summary>
		/// <remarks>
		/// Never the whole text and never a part that changes between runs, which is the coverage manifest's rule for the
		/// same thing (see CoverageManifest). So each entry is the engine's own "device did not start" sentence, which names
		/// the sample's device, and not the SQL client's message under it: measured 2026-11-25, the three failed with three
		/// different driver messages (SQL Network Interfaces error 26, Named Pipes error 40) for the same cause, and that
		/// text depends on how the machine resolves the name each sample happens to have written.
		/// </remarks>
		// NOTE: attempting these three is the slowest thing a configured run does - measured 2026-11-25 against SQL Server
		// 2016 LocalDB at 19 s, 20 s and 19 s, 58 s of the pass's 1 m 40 s, all of it the driver's connect timeout against
		// the server each sample names rather than the configured one. CI runs the suite twice, so it is about two minutes
		// per push. If CI wall clock ever has to come down, this is the first place to look: the information the attempt
		// adds is only that the failure is still the recorded one.
		private static readonly Dictionary<string, string> FailsWithADatabaseServer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Sample.Forum", "An exception occurred during startup of device \"Sample.Forum.Forums\"." },
			{ "Sample.Pubs", "An exception occurred during startup of device \"Sample.Pubs.Pubs\"." },
			{ "Sample.Shipping", "An exception occurred during startup of device \"Sample.Shipping.Shipping\"." },
		};

		/// <summary>
		/// The samples that expose no table variable once registered, with the reason each does not. Registering them is
		/// still what their case is for; there is simply nothing to derive afterwards.
		/// </summary>
		private static readonly Dictionary<string, string> DefinesNoTableVariable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Sample.Components", "is a library of form documents by design and has no create table in it." },
			{ "Sample.Demo", "registers only its Base script, which creates types and operators; the six further scripts that create its tables, views, events, constraints and data are listed in a comment in Register.d4 for a reader to run by hand." },
			{ "Sample.Rental", "has a zero-byte Register.d4: the tutorial's schema is created by running the scripts its RegisterCompleted.d4 lists, which a reader does from the IDE." },
			{ "Sample.ScriptAction", "is a library of form documents and scripts by design and has no create table in it." },
		};

		/// <summary>
		/// The samples the build does not assemble into the runtime layout, with the reason. Their cases report
		/// Inconclusive and start no server: there is no library folder for a server to carry.
		/// </summary>
		/// <remarks>
		/// Empty since library-sample-shipping-convert built the last unconverted sample's assembly. The list stays: it is
		/// asserted against the tree in both directions, so a sample the build cannot assemble (a new one arriving with an
		/// unconverted C# project) belongs here rather than failing its case with a copy that finds nothing.
		/// </remarks>
		private static readonly Dictionary<string, string> NotInTheRuntimeLayout = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
		};

		/// <summary>The three lists above, for the assertions that hold each of them against the tree.</summary>
		private static IEnumerable<(string Name, Dictionary<string, string> Entries)> Lists()
		{
			yield return (nameof(NeedsADatabaseServer), NeedsADatabaseServer);
			yield return (nameof(DefinesNoTableVariable), DefinesNoTableVariable);
			yield return (nameof(NotInTheRuntimeLayout), NotInTheRuntimeLayout);
		}

		/// <summary>The repository's samples/ folder, which is this pass's corpus root.</summary>
		private static string SamplesDirectory => Path.Combine(TestEnvironment.RepositoryDirectory, SamplesFolderName);

		/// <summary>
		/// The sample libraries this pass covers. Walked rather than listed, so a sample added to samples/ becomes a case
		/// with no code change here.
		/// </summary>
		/// <remarks>
		/// samples/ rather than the runtime layout's Libraries/, because the layout mixes the shipped, sample and test
		/// libraries in one folder and leaves out the ones the build cannot assemble - so it can neither name the sample
		/// set nor notice that one of them is missing. What a server carries still comes from the layout: that is what the
		/// build assembles and what the product ships. For a corpus outside this repository the two are the same folder.
		/// </remarks>
		public static IReadOnlyList<CorpusLibrary> DiscoveredSamples()
		{
			return CorpusLibrary.DiscoverLibraries(SamplesDirectory);
		}

		/// <summary>
		/// One case per discovered sample. Discovery reads directories and needs no server, so it runs at NUnit's
		/// discovery time; one case per sample is what makes a single sample's failure or Inconclusive legible.
		/// </summary>
		public static IEnumerable<TestCaseData> Samples()
		{
			foreach (CorpusLibrary sample in DiscoveredSamples())
				yield return new TestCaseData(sample).SetName(sample.Name);
		}

		/// <summary>
		/// The pass itself: a server of this sample's own, carrying its requisite closure and nothing else, registers the
		/// sample, and every page type of every surface the sample then defines derives into a form with the right root
		/// element.
		/// </summary>
		/// <remarks>
		/// The server is started inside the case rather than in a one-time setup because each case needs a different one;
		/// they run one at a time, each disposed before the next starts.
		/// </remarks>
		[TestCaseSource(nameof(Samples))]
		public void TheSampleRegistersAndDerives(CorpusLibrary sample)
		{
			if (NotInTheRuntimeLayout.TryGetValue(sample.Name, out string notAssembled))
				Assert.Inconclusive(String.Format("{0} {1}", sample.Name, notAssembled));
			if (NeedsADatabaseServer.TryGetValue(sample.Name, out string needsADatabaseServer))
			{
				if (!TestEnvironment.MSSQLConfigured)
					Assert.Inconclusive(String.Format("{0} {1} {2}", sample.Name, needsADatabaseServer, TestEnvironment.MSSQLNotConfigured("attempt it")));
				AttemptTheSampleThatNamesItsOwnServer(sample);
				return;
			}

			// The closure is resolved in the layout, so the folders copied are the ones the build assembled, and a sample
			// the layout has no folder for is caught by the list above rather than by a copy that finds nothing.
			IReadOnlyList<CorpusLibrary> closure = CorpusLibrary.RequisiteClosure(TestEnvironment.LibrariesDirectory, sample.Name);

			using (TestServerInstance instance = new TestServerInstance("corpus-" + sample.Name, closure.Select(library => library.Directory).ToList()))
			{
				instance.Server.Start();
				Register(instance, sample.Name, closure);
				DeriveEverySurface(instance, sample);
			}
		}

		/// <summary>
		/// Registers one of the three samples that name a server of their own against the configured server, and checks the
		/// failure against <see cref="FailsWithADatabaseServer"/>. No derivation follows: the sample never registers.
		/// </summary>
		private static void AttemptTheSampleThatNamesItsOwnServer(CorpusLibrary sample)
		{
			string expected = FailsWithADatabaseServer[sample.Name];
			IReadOnlyList<CorpusLibrary> closure = CorpusLibrary.RequisiteClosure(TestEnvironment.LibrariesDirectory, sample.Name);

			string error = null;
			using (TestServerInstance instance = new TestServerInstance("corpus-" + sample.Name, closure.Select(library => library.Directory).ToList()))
			{
				instance.Server.Start();
				try
				{
					Register(instance, sample.Name, closure);
				}
				catch (Exception exception)
				{
					error = FrontendFixture.FlattenMessages(exception);
				}
			}

			if (error == null)
				Assert.Fail(String.Format("{0} now registers against the server {1} names, so its entries in {2} and {3} have to go and it becomes an ordinary case.", sample.Name, TestEnvironment.MSSQLVariable, nameof(NeedsADatabaseServer), nameof(FailsWithADatabaseServer)));
			if (!error.Contains(expected, StringComparison.Ordinal))
				Assert.Fail(String.Format("{0}: expected its registration to fail with an error containing \"{1}\", but it failed with: {2}", sample.Name, expected, error));
			Assert.Warn(String.Format("{0} {1} It was attempted and failed as recorded: {2}", sample.Name, NeedsADatabaseServer[sample.Name], error));
		}

		/// <summary>
		/// Enumerates the sample's surfaces on a session of its own and derives every page type of each, collecting what
		/// went wrong rather than stopping at the first, so that one case reports every form that failed.
		/// </summary>
		private static void DeriveEverySurface(TestServerInstance instance, CorpusLibrary sample)
		{
			IServerSession session = instance.Connect();
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					List<string> surfaces = FrontendDerivationGoldenTest.EnumerateSurfaces(process, sample.Name);
					CheckTheSurfacesAgreeWithTheList(sample, surfaces);

					List<string> problems = new List<string>();
					foreach (string surface in surfaces)
						foreach (string pageType in FrontendDerivationGoldenTest.PageTypes)
						{
							string problem = DerivationProblem(process, surface, pageType);
							if (problem != null)
								problems.Add(problem);
						}

					Assert.That(problems, Is.Empty, String.Format("{0} of the {1} form(s) {2}'s {3} surface(s) should derive did not.", problems.Count, surfaces.Count * FrontendDerivationGoldenTest.PageTypes.Length, sample.Name, surfaces.Count));
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				instance.Server.Disconnect(session);
			}
		}

		/// <summary>
		/// Registers the sample, which pulls in the libraries it requires. Frontend is registered first when the closure
		/// has it: System.Internet's registration script calls the Frontend.ExecuteScript operator without declaring the
		/// dependency, so a server that reaches System.Internet before Frontend fails - a defect in System.Internet,
		/// tracked by bug-system-internet-registration-needs-frontend, which FrontendFixture works around by ordering its
		/// own list. Drop the hoist when that lands.
		/// </summary>
		// NOTE: a library's Upgrades\Upgrade.<version>.d4 scripts are deliberately not run here. The one such folder under
		// samples/ is Sample.Demo's, whose single statement alters a table that registration does not create, so running it
		// would only assert that it fails. The WS7 work item library-upgrade-scripts owns establishing what runs those
		// scripts at all, and debt-library-upgrades-coverage owns testing the upgrade machinery.
		private static void Register(TestServerInstance instance, string sampleName, IEnumerable<CorpusLibrary> closure)
		{
			if (closure.Any(library => String.Equals(library.Name, FrontendLibraryName, StringComparison.OrdinalIgnoreCase)))
				instance.ExecuteScript(String.Format("EnsureLibraryRegistered('{0}');", FrontendLibraryName));
			instance.ExecuteScript(String.Format("EnsureLibraryRegistered('{0}');", sampleName));
		}

		/// <summary>
		/// Both directions of the <see cref="DefinesNoTableVariable"/> list, asserted here because it takes a registered
		/// server to know: a sample with an entry must still expose no surface, and a sample with no entry must expose at
		/// least one.
		/// </summary>
		private static void CheckTheSurfacesAgreeWithTheList(CorpusLibrary sample, List<string> surfaces)
		{
			if (DefinesNoTableVariable.ContainsKey(sample.Name))
				Assert.That
				(
					surfaces,
					Is.Empty,
					String.Format
					(
						"{0} now defines a table variable, so its {1} entry has to go - and the library has to be added to FrontendDerivationGoldenTest.CorpusLibraries in the same change, or the new surface has no golden.",
						sample.Name,
						nameof(DefinesNoTableVariable)
					)
				);
			else
				Assert.That
				(
					surfaces,
					Is.Not.Empty,
					String.Format("{0} registers but defines no table variable, so this case derives nothing. Add an entry to {1} saying why it defines none.", sample.Name, nameof(DefinesNoTableVariable))
				);
		}

		/// <summary>
		/// What is wrong with one derivation, or null when the form derives, parses and has the root element its page type
		/// calls for. Returned rather than asserted so that one case reports every form that failed, not just the first.
		/// </summary>
		private static string DerivationProblem(IServerProcess process, string surfaceName, string pageType)
		{
			string derived;
			try
			{
				derived = FrontendDerivationGoldenTest.Derive(process, surfaceName, pageType);
			}
			catch (Exception exception)
			{
				return String.Format("{0} of {1}: deriving failed: {2}", pageType, surfaceName, FrontendFixture.FlattenMessages(exception));
			}

			if (String.IsNullOrEmpty(derived))
				return String.Format("{0} of {1}: Frontend.Derive returned nothing.", pageType, surfaceName);

			XDocument document;
			try
			{
				document = XDocument.Parse(derived);
			}
			catch (XmlException exception)
			{
				return String.Format("{0} of {1}: the derived form does not parse as XML: {2}", pageType, surfaceName, exception.Message);
			}

			string expected = ExpectedRootElementName(pageType);
			return
				String.Equals(document.Root.Name.LocalName, expected, StringComparison.Ordinal)
					? null
					: String.Format("{0} of {1}: the derived form's root element is <{2}>, not <{3}>.", pageType, surfaceName, document.Root.Name.LocalName, expected);
		}

		/// <summary>
		/// The document element a derived form has. The report page's document root is &lt;report&gt;; every other page
		/// type the engine builds produces an &lt;interface&gt;. This is the one piece of per-page-type knowledge the pass
		/// needs, and the checked-in goldens are where it was read off.
		/// </summary>
		private static string ExpectedRootElementName(string pageType)
		{
			return String.Equals(pageType, ReportPageType, StringComparison.Ordinal) ? ReportElementName : FormDocumentCorpusTest.InterfaceElementName;
		}

		/// <summary>
		/// The corpus is not empty. Without this, a renamed or moved samples/ folder would turn the whole pass into one
		/// passing case that covers nothing.
		/// </summary>
		[Test]
		public void TheSamplesDirectoryHoldsLibraries()
		{
			Assert.That
			(
				DiscoveredSamples(),
				Is.Not.Empty,
				String.Format("No folder under \"{0}\" holds a {1} file named after it, so this pass covers nothing. Has samples/ moved?", SamplesDirectory, CorpusLibrary.DescriptorExtension)
			);
		}

		/// <summary>
		/// The three lists and the tree have to agree, in both directions, so that none of them becomes dead text hiding a
		/// sample nobody looks at. Every entry names a sample that is really there; the samples the runtime layout has no
		/// folder for are exactly the ones recorded as such; and a sample recorded as needing a database server really
		/// requires a SQL device library. The remaining direction of the "defines no table variable" list takes a
		/// registered server to check and is in <see cref="CheckTheSurfacesAgreeWithTheList"/>.
		/// </summary>
		/// <remarks>
		/// One case rather than three: the agreement is a single statement about the whole corpus, and a case that needed
		/// its siblings to have run first would give the wrong answer under the name filters AGENTS.md runs the suite with.
		/// </remarks>
		[Test]
		public void EverySampleIsAccountedFor()
		{
			List<string> samples = DiscoveredSamples().Select(sample => sample.Name).ToList();
			HashSet<string> discovered = new HashSet<string>(samples, StringComparer.OrdinalIgnoreCase);

			Assert.Multiple(() =>
			{
				foreach ((string listName, Dictionary<string, string> entries) in Lists())
					Assert.That
					(
						entries.Keys.Where(name => !discovered.Contains(name)),
						Is.Empty,
						String.Format("These {0} entries name no library folder under \"{1}\". Delete the entry, or find out what became of the sample.", listName, SamplesDirectory)
					);

				Assert.That
				(
					samples.Where(name => Lists().Count(list => list.Entries.ContainsKey(name)) > 1),
					Is.Empty,
					"These samples are in more than one of the three lists; each sample has one reason, and the case reads the lists in order."
				);

				Assert.That
				(
					NotInTheRuntimeLayout.Keys.Where(LayoutHasTheLibrary),
					Is.Empty,
					String.Format("The runtime layout now has a library folder for these samples, so they are ordinary cases: delete their {0} entries.", nameof(NotInTheRuntimeLayout))
				);

				Assert.That
				(
					samples.Where(name => !LayoutHasTheLibrary(name) && !NotInTheRuntimeLayout.ContainsKey(name)),
					Is.Empty,
					String.Format("The runtime layout has no library folder for these samples and nothing says why. Add the library to tools/layout/Layout.csproj, or an entry to {0} saying why the build leaves it out.", nameof(NotInTheRuntimeLayout))
				);

				Assert.That
				(
					NeedsADatabaseServer.Keys.Where(name => LayoutHasTheLibrary(name) && !RequiresASqlDeviceLibrary(name)),
					Is.Empty,
					String.Format("These samples require no SQL device library ({0}), so the premise of their {1} entries has gone: delete the entry and let them run as ordinary cases.", String.Join(" or ", SqlDeviceLibraries), nameof(NeedsADatabaseServer))
				);

				// One entry in each list per sample: the first says why the case is skipped without a server, the second what
				// the attempt fails with when there is one, and a sample with only one of them would leave a configured run
				// (or an unconfigured one) with nothing to read.
				Assert.That
				(
					NeedsADatabaseServer.Keys.Where(name => !FailsWithADatabaseServer.ContainsKey(name)).Concat(FailsWithADatabaseServer.Keys.Where(name => !NeedsADatabaseServer.ContainsKey(name))),
					Is.Empty,
					String.Format("These samples are in one of {0} and {1} but not the other; every sample that needs a database server also has the text its attempt fails with.", nameof(NeedsADatabaseServer), nameof(FailsWithADatabaseServer))
				);
			});
		}

		/// <summary>Whether the runtime layout holds a library folder the server could carry for <paramref name="libraryName"/>.</summary>
		private static bool LayoutHasTheLibrary(string libraryName)
		{
			return CorpusLibrary.FindLibrary(TestEnvironment.LibrariesDirectory, libraryName) != null;
		}

		/// <summary>Whether a library's requisite closure holds one of <see cref="SqlDeviceLibraries"/>.</summary>
		private static bool RequiresASqlDeviceLibrary(string libraryName)
		{
			return
				CorpusLibrary.RequisiteClosure(TestEnvironment.LibrariesDirectory, libraryName)
					.Any(library => SqlDeviceLibraries.Contains(library.Name, StringComparer.OrdinalIgnoreCase));
		}
	}
}
