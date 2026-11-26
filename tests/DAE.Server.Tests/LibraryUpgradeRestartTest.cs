/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;

using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Taking a new version of an installed application: that an upgrade the files on disk ask for is still pending once the
	/// server has been stopped and started, and that upgrades whose payload is what a customer's upgrade script really carries
	/// - a device with a table in it, a form document - apply after that restart. See library-upgrade-scripts.
	/// </summary>
	/// <remarks>
	/// Why this is C# and not a D4 coverage script. An upgrade is pending when a library's files on disk claim a newer version
	/// than the catalog records as applied. A script cannot produce that state honestly: raising a registered library's
	/// descriptor version carries the recorded version with it, so every section of LibraryUpgrades.d4 stands in for "the new
	/// files arrived and the server was restarted" with an update against the LibraryVersions catalog table. What makes that
	/// stand-in legitimate is one line of LibraryUtility.LoadLibrary: it reads the recorded version before loading the library
	/// and writes the same value back afterwards, deliberately, so that a restart does not swallow a pending upgrade. Only a
	/// fixture that can stop and start a server pins it, and until this one nothing did - a regression there would have left
	/// every section of that script passing over a premise that had become false. Measured by making that line record the
	/// descriptor version instead: all three cases here then fail, the first on the recorded version it reads back and the
	/// two payload cases on the upgrade no longer being pending at all.
	///
	/// How a new version's files arrive here. The descriptor version is raised by rewriting the library's .d4l while the server
	/// is stopped, through the engine's own deserializer and serializer (Schema.LibraryUtility.LoadFromStream and SaveToFile)
	/// rather than as text, so this fixture pins no idea of the descriptor format of its own. That is the honest stand-in for
	/// an installer unpacking a new release over the old folder, because the files are what claims the new version.
	/// SetLibraryDescriptor on the running server is not: it refuses to change a registered library's version at all
	/// (CannotChangeRegisteredLibraryVersion), and raising an unregistered library's leaves nothing pending either, because a
	/// library with no LibraryVersions row records whatever its descriptor says at the moment it is registered.
	///
	/// Each case gets a TestServerInstance of its own - they stop and start the server and rewrite a file in its library
	/// directory - and each library is one the server itself created, so the descriptor rewritten is one the server wrote.
	/// With DATAPHOR_TEST_LOGGING=1 the instance folders are kept, and a kept folder's .d4l then holds the raised version
	/// rather than the version the runtime layout's copy carries. The folders are per-run copies, so that is a difference to
	/// read rather than to repair.
	///
	/// The upgrade machinery's transaction contract is not this fixture's subject: LibraryUpgrades.d4's UpgradeNested section
	/// pins that an upgrade script may upgrade a library outside its own requisites and must leave a transaction open when it
	/// returns. Every upgrade script here is an ordinary one that does neither.
	/// </remarks>
	// NOTE: no case here ships an assembly, and the realistic half of that cannot be shown in one process. A customer's new
	// library version may carry a new build of the library's own assembly, holding the classes the upgrade's new operators
	// name; a real deployment picks it up because the service process restarts. A loaded assembly is never unloaded (see
	// LibraryLoadingTest), and Assembly.LoadFrom on another copy with the same identity returns the instance already loaded,
	// so a Server.Stop()/Start() in this process would go on running the old build and a case over it would prove nothing.
	// The narrower half - a version that *adds* an assembly where the old version had none - would work, that load being the
	// only one, but there is no assembly in the suite to give it: every library assembly in the runtime layout belongs to a
	// library that registers it, and borrowing one would repoint the process-wide LibraryAssemblyResolver entry for that
	// assembly (keyed by assembly, and overwritten on registration) at this fixture's temporary folder, which would make
	// LibraryLoadingTest's own cases depend on test order. So the gap stays open deliberately: closing it costs a test
	// library assembly of this fixture's own (a new project, in the layout's library list and on the coverage report's
	// areas) plus a harness that restarts a host process and reloads its libraries, which nothing in the suite does. Take
	// it when the suite grows that restart - OutOfProcessInstance is the vehicle, and debt-test-shipped-host-executable is
	// the work item that would reach it first - and not before, because in-process cases over it cover nothing.
	[TestFixture]
	public class LibraryUpgradeRestartTest
	{
		/// <summary>An empty D4 library-reference list: the requisite list of a library that requires nothing of its own.</summary>
		private const string NoRequisites = "list(LibraryReference){}";

		/// <summary>
		/// The premise every section of LibraryUpgrades.d4 stands on. A library whose files claim a newer version than the
		/// catalog has applied still has that upgrade pending after the server has been stopped and started, and the upgrade
		/// then applies. The negative assertion is the one that matters: the recorded version after the restart must still be
		/// the old one, because a start that recorded the descriptor version would make the pending upgrade disappear.
		/// </summary>
		[Test]
		public void AnUpgradeTheFilesAskForIsStillPendingAfterARestart()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AnUpgradeTheFilesAskForIsStillPendingAfterARestart)))
			{
				instance.Server.Start();
				CreateAndRegisterLibrary(instance, "UpgradeRestart", NoRequisites);
				SaveUpgrade
				(
					instance,
					"UpgradeRestart",
					UpgradeVersion(1, 0, 1),
					"create table RestartTrace { ID : Integer, Trace : String, key { ID } }; insert table { row { 0 ID, 'restart;' Trace } } into RestartTrace;"
				);

				Assert.That(RecordedVersion(instance, "UpgradeRestart"), Is.EqualTo("1.0.0.*"), "Registering a library records its descriptor version, so nothing is pending yet.");
				Assert.That(UpgradeRequired(instance, "UpgradeRestart"), Is.False, "Saving an upgrade at an explicit version must touch neither version number.");

				instance.Server.Stop();
				RaiseTheDescriptorVersionOnDisk(instance, "UpgradeRestart", UpgradeVersion(1, 0, 1));
				instance.Server.Start();

				Assert.That(DescriptorVersion(instance, "UpgradeRestart"), Is.EqualTo("1.0.1.*"), "The start did not read the version the files on disk claim.");
				Assert.That(RecordedVersion(instance, "UpgradeRestart"), Is.EqualTo("1.0.0.*"), "The start recorded the descriptor version, which swallows a pending upgrade.");
				Assert.That(UpgradeRequired(instance, "UpgradeRestart"), Is.True);
				Assert.That(IsRegistered(instance, "UpgradeRestart"), Is.True, "The restart left the library unregistered, so nothing could upgrade it.");

				instance.ExecuteScript("UpgradeLibrary('UpgradeRestart');");

				Assert.That(RecordedVersion(instance, "UpgradeRestart"), Is.EqualTo("1.0.1.*"));
				Assert.That(instance.Evaluate("Trace from row from .UpgradeRestart.RestartTrace"), Is.EqualTo("restart;"), "The upgrade did not run, or ran more than once.");
			}
		}

		/// <summary>
		/// A payload a customer's upgrade script really carries, rather than the one table in the default memory device that
		/// every section of LibraryUpgrades.d4 upgrades: the upgrade creates a device of its own, a table in that device, and a
		/// row in that table. The row reading back afterwards is what says the device was started - a table in a device cannot
		/// be written or read until it is - and a device created inside the single transaction the upgrade machinery opened is
		/// something no section of that script reaches.
		/// </summary>
		[Test]
		public void AnUpgradeThatAddsADeviceAndATableAppliesAfterARestart()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AnUpgradeThatAddsADeviceAndATableAppliesAfterARestart)))
			{
				instance.Server.Start();
				CreateAndRegisterLibrary(instance, "UpgradeDevicePayload", NoRequisites);
				SaveUpgrade
				(
					instance,
					"UpgradeDevicePayload",
					UpgradeVersion(1, 0, 1),
					"create device PayloadDevice class \"System.MemoryDevice\"; "
						+ "create table Thing in PayloadDevice { ID : Integer, Name : String, key { ID } }; "
						+ "insert table { row { 1 ID, 'first' Name } } into Thing;"
				);

				instance.Server.Stop();
				RaiseTheDescriptorVersionOnDisk(instance, "UpgradeDevicePayload", UpgradeVersion(1, 0, 1));
				instance.Server.Start();

				Assert.That(UpgradeRequired(instance, "UpgradeDevicePayload"), Is.True);
				instance.ExecuteScript("UpgradeLibrary('UpgradeDevicePayload');");

				Assert.That(RecordedVersion(instance, "UpgradeDevicePayload"), Is.EqualTo("1.0.1.*"));
				Assert.That(instance.Evaluate("Name from row from (Devices where Library_Name = 'UpgradeDevicePayload')"), Is.EqualTo("UpgradeDevicePayload.PayloadDevice"), "The upgrade did not leave its device in the catalog.");
				Assert.That(instance.Evaluate("Name from row from .UpgradeDevicePayload.Thing"), Is.EqualTo("first"), "The table in the device the upgrade created does not hold the row the upgrade inserted.");
			}
		}

		/// <summary>
		/// The other payload a release carries: a form document. This one goes through the Frontend device rather than the
		/// catalog, so what proves it is a file in the library's Documents folder - which is the second reason this fixture is
		/// C# rather than a script, a script having no way to see a server-side path.
		/// </summary>
		[Test]
		public void AnUpgradeThatAddsAFormDocumentAppliesAfterARestart()
		{
			using (TestServerInstance instance = new TestServerInstance(nameof(AnUpgradeThatAddsAFormDocumentAppliesAfterARestart), "Frontend", "SimpleDevice"))
			{
				instance.Server.Start();
				CreateAndRegisterLibrary(instance, "UpgradeFormPayload", "{ LibraryReference('Frontend', VersionNumber(3, 0)) }");
				SaveUpgrade
				(
					instance,
					"UpgradeFormPayload",
					UpgradeVersion(1, 0, 1),
					String.Format("Frontend.CreateAndSave('UpgradeFormPayload', 'PayloadForm', 'dfd', {0});", Literal(PayloadForm))
				);

				instance.Server.Stop();
				RaiseTheDescriptorVersionOnDisk(instance, "UpgradeFormPayload", UpgradeVersion(1, 0, 1));
				instance.Server.Start();

				Assert.That(UpgradeRequired(instance, "UpgradeFormPayload"), Is.True);
				instance.ExecuteScript("UpgradeLibrary('UpgradeFormPayload');");

				Assert.That(RecordedVersion(instance, "UpgradeFormPayload"), Is.EqualTo("1.0.1.*"));
				Assert.That(instance.Evaluate("Frontend.DocumentExists('UpgradeFormPayload', 'PayloadForm')"), Is.True, "The upgrade did not leave its document in the Frontend device.");
				Assert.That(instance.Evaluate("Frontend.LoadDocument('UpgradeFormPayload', 'PayloadForm')"), Is.EqualTo(PayloadForm));

				// The document is a file in the library's own folder, which is what a release ships and what a backup of the
				// application has to carry; the row in the Documents table is derived from the folder rather than the other
				// way round.
				Assert.That(File.Exists(Path.Combine(LibraryFolder(instance, "UpgradeFormPayload"), "Documents", "PayloadForm.dfd")), Is.True);
			}
		}

		/// <summary>A minimal real form document: what a release adds to a library when it adds a screen.</summary>
		private const string PayloadForm =
			"<interface xmlns:bop=\"www.alphora.com/schemas/bop\" text=\"Payload Form\">"
				+ "<statictext text=\"Added by the 1.0.1 upgrade.\" bop:name=\"StaticText1\" />"
				+ "</interface>";

		/// <summary>
		/// Has the server create a library at 1.0.0 in its own library directory and register it, so that the descriptor the
		/// cases go on to rewrite is one the server itself wrote. <paramref name="requisites"/> is the D4 list of library
		/// references the descriptor is created with.
		/// </summary>
		private static void CreateAndRegisterLibrary(TestServerInstance instance, string name, string requisites)
		{
			instance.ExecuteScript
			(
				String.Format
				(
					"CreateLibrary(LibraryDescriptor({0}, VersionNumber(1, 0, 0), \"\", list(FileReference){{}}, {1})); RegisterLibrary({0});",
					Literal(name),
					requisites
				)
			);
		}

		/// <summary>
		/// Writes an upgrade script into the library's Upgrades folder at an explicit version, which is what the IDE's upgrade
		/// editor does and the one way to produce an upgrade file without moving either version number.
		/// </summary>
		private static void SaveUpgrade(TestServerInstance instance, string name, VersionNumber version, string script)
		{
			instance.ExecuteScript
			(
				String.Format
				(
					"SaveUpgrade({0}, VersionNumber({1}, {2}, {3}), {4});",
					Literal(name),
					version.Major,
					version.Minor,
					version.Revision,
					Literal(script)
				)
			);
		}

		/// <summary>
		/// Rewrites the library's .d4l so that the files on disk claim <paramref name="version"/> - the stand-in for an
		/// installer unpacking a new release. Only while the server is stopped: a running server reads the descriptor once, at
		/// a start, and would write its own copy back over this one.
		/// </summary>
		private static void RaiseTheDescriptorVersionOnDisk(TestServerInstance instance, string name, VersionNumber version)
		{
			Assert.That(instance.Server.State, Is.EqualTo(ServerState.Stopped), "The descriptor may only be rewritten while the server is stopped.");

			string fileName = Path.Combine(LibraryFolder(instance, name), Schema.LibraryUtility.GetFileName(name));
			Schema.Library descriptor;
			using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				descriptor = Schema.LibraryUtility.LoadFromStream(stream);
			descriptor.Version = version;
			Schema.LibraryUtility.SaveToFile(descriptor, fileName);
		}

		/// <summary>Where the server put a library of this name: its own library directory, these libraries having no directory of their own.</summary>
		private static string LibraryFolder(TestServerInstance instance, string name)
		{
			return Path.Combine(instance.Directory, TestServerInstance.LibrariesFolderName, name);
		}

		/// <summary>What the files on disk claim, which is the ceiling of an upgrade run.</summary>
		private static string DescriptorVersion(TestServerInstance instance, string name)
		{
			return (string)instance.Evaluate(String.Format("(Version from row from (Libraries where Name = {0})).ToString()", Literal(name)));
		}

		/// <summary>What the catalog believes is applied, which is the floor of an upgrade run.</summary>
		private static string RecordedVersion(TestServerInstance instance, string name)
		{
			return (string)instance.Evaluate(String.Format("(Version from row from (LibraryVersions where LibraryName = {0})).ToString()", Literal(name)));
		}

		private static bool UpgradeRequired(TestServerInstance instance, string name)
		{
			return (bool)instance.Evaluate(String.Format("UpgradeRequired from row from (LibraryView where Name = {0})", Literal(name)));
		}

		private static bool IsRegistered(TestServerInstance instance, string name)
		{
			return (bool)instance.Evaluate(String.Format("exists (LoadedLibraries where Name = {0})", Literal(name)));
		}

		/// <summary>
		/// A version number to the revision and no further, which is the only shape an upgrade version may have
		/// (UpgradeUtility.CheckValidVersionNumber) and what VersionNumber(major, minor, revision) means in D4. The C# struct
		/// takes all four components, and -1 is its "not specified".
		/// </summary>
		private static VersionNumber UpgradeVersion(int major, int minor, int revision)
		{
			return new VersionNumber(major, minor, revision, -1);
		}

		/// <summary>A D4 string literal. The lexer has no escape character - a quote is written twice.</summary>
		private static string Literal(string value)
		{
			return "'" + value.Replace("'", "''") + "'";
		}
	}
}
