/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The second half of layer L3 (docs/modernization-plan.md, WS3): the SQL Server device executing against a real server,
	/// through the device-coverage corpus of TestFramework.Coverage.Devices. Ignored unless DATAPHOR_TEST_MSSQL names a server.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The oracle is the D4 script, not this file (decision D2). TestMSSQLDevice.d4 runs the corpus - one row of
	/// Coverage.Devices.DeviceCase per case, 1180 of them - against a device and raises an error naming every case that
	/// failed; all this fixture does is create the device the script needs, run the script and report what came back. There is
	/// deliberately no assertion here about what the device ought to do: a new expectation written in C# would be a second,
	/// unreviewed oracle beside the one the scripts already are.
	/// </para>
	/// <para>
	/// Two devices, because the library needs one before it can be registered at all and the script under test needs one of
	/// its own: registering TestFramework.Coverage.Devices runs its Register.d4, which sets TestingDevice as the library's
	/// default device and alters it to add three storage-length mappings, so that device has to exist first and the library's
	/// tables - DeviceCase and the rest, 1180 rows of corpus - are created on it. TestMSSQLDevice.d4 then drives TestDevice.
	/// Both used to be created by the library's own scripts against a hard-coded server and the database DataphorTests, which
	/// is why neither could run on another machine. Both now live in TestFramework.Coverage.Base: it is registered before
	/// Coverage.Devices and is one of its requisites, so an object the library creates may reference a device there, where an
	/// object in a library that does not require the device's library may not.
	/// </para>
	/// <para>
	/// Why this is a fixture of its own rather than a coverage-manifest entry: TestMSSQLDevice.d4 is in
	/// TestFramework.Coverage.Devices, not in TestFramework.Coverage.Scripts, so the coverage runner never sees it - and it
	/// should not move, because every script that runner finds becomes a case on a server that must need no database.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: it starts a server, and two engines in one process share the engine's static state (see the note
	// on TestServerInstance).
	[TestFixture]
	public class LiveDeviceTest
	{
		/// <summary>The library holding the device-coverage corpus and the script that runs it.</summary>
		private const string CorpusLibraryName = "TestFramework.Coverage.Devices";

		/// <summary>The library the two devices are created in; see the fixture's remarks.</summary>
		private const string DeviceLibraryName = "TestFramework.Coverage.Base";

		/// <summary>The device the corpus library's own registration needs, as its descriptor's default device name.</summary>
		private const string LibraryDeviceName = "TestingDevice";

		/// <summary>The device TestMSSQLDevice.d4 drives.</summary>
		private const string DeviceUnderTestName = "TestDevice";

		/// <summary>The script that runs the corpus, in the corpus library's Documents folder.</summary>
		private const string ScriptName = "TestMSSQLDevice";

		/// <summary>The database this pass puts both devices' tables in, on the server DATAPHOR_TEST_MSSQL names.</summary>
		private const string DatabaseName = "DataphorDeviceCoverage";

		/// <summary>The ticket that owns the script's failure, and an ordinal substring the failure must contain.</summary>
		/// <remarks>
		/// <para>
		/// The same bargain a known-failure entry of the coverage manifest strikes, in C# because this script is not in that
		/// manifest: the script must fail, and fail with this text, or the run fails.
		/// </para>
		/// <para>
		/// What it fails on is the third statement of the script, which cannot succeed on any version: it raises an error
		/// naming every catalog object called TestFramework.Coverage.Devices.TestDevice-something, and the library's own
		/// Logic.d4 creates an operator TestDevice(Name, Integer) whose catalog name is exactly that. Its two neighbours are
		/// a commented-out check and a raise Error('WTF'), so it reads as a developer's scaffolding rather than an assertion;
		/// but whether a statement of a coverage script may be repaired is the owner's call, and test-pre-3-1-scripts-policy
		/// owns that question (it has a third arm for this script, with the evidence and the proposed change).
		/// </para>
		/// <para>
		/// A pattern rather than a substring, and it requires the object id: what the broken statement prints is a row of
		/// Objects.Name, which is the operator's name with its object id appended, and only the catalog spells an operator
		/// that way - an error raised anywhere inside the corpus names the operator by its signature or names a table. So a
		/// regression in the corpus cannot be mistaken for this known failure, where "contains TestDevice" could be.
		/// </para>
		/// <para>
		/// What this fixture therefore proves today, and it is not nothing: the three libraries register, both SQL Server
		/// devices are created from a device definition that names only a server and a database and connect to the configured
		/// server, the corpus library's tables and its 1180 rows are created on one of them through the device, and the script
		/// runs as far as its own broken statement. What it does not yet prove is the corpus itself. Disabling that one
		/// statement by hand (2026-11-25, SQL Server 2016 LocalDB) runs the whole corpus in about 10 s and fails 8 to 12 cases
		/// in four families, recorded on bug-mssql-device-live-coverage-failures and as an arm of
		/// bug-binary-comparison-needs-the-mssql-device.
		/// </para>
		/// </remarks>
		private const string KnownFailureTicket = "test-pre-3-1-scripts-policy";
		private static readonly Regex KnownFailureError =
			new Regex(@"TestFramework\.Coverage\.Devices\.TestDevice[0-9]+", RegexOptions.CultureInvariant);

		/// <summary>
		/// The library folders the server carries. Frontend first in the registration below, because System.Internet's
		/// registration script - which nothing here needs, but which the library set could grow to include - calls a Frontend
		/// operator without declaring the dependency (bug-system-internet-registration-needs-frontend).
		/// </summary>
		private static readonly string[] Libraries =
		[
			"Frontend",
			"SimpleDevice",
			"Connection",
			"SQLDevice",
			"MSSQLDevice",
			"TestFramework",
			DeviceLibraryName,
			CorpusLibraryName
		];

		private TestServerInstance _instance;

		[OneTimeSetUp]
		public void BuildTheServer()
		{
			if (!TestEnvironment.MSSQLConfigured)
				return;

			// At the start of the run rather than at the end of it, so that a kept or crashed run leaves its database to be
			// looked at and this one still starts on an empty database.
			TestEnvironment.EmptyMSSQLDatabase(DatabaseName);

			_instance = new TestServerInstance("LiveDevice", Libraries);
			try
			{
				_instance.Server.Start();
				_instance.ExecuteScript(Registration());
			}
			catch
			{
				_instance.Dispose();
				_instance = null;
				throw;
			}
		}

		[OneTimeTearDown]
		public void StopTheServer()
		{
			if (_instance != null)
			{
				_instance.Dispose();
				_instance = null;
			}
		}

		/// <summary>
		/// Everything the server needs before the script can run: the libraries the corpus rests on, the two devices, the
		/// corpus library itself - registering it starts TestingDevice and creates its tables there - and then the device
		/// under test started.
		/// </summary>
		/// <remarks>
		/// <para>
		/// MSSQLDevice is registered by name although the corpus library requires it, because the two devices are created
		/// before that library is registered and the class alias MSSQLDevice.MSSQLDevice does not exist until the library that
		/// declares it has been registered.
		/// </para>
		/// </remarks>
		// The device under test is started here, in a statement of its own, rather than being left to the script's own
		// EnsureDeviceStarted. Measured 2026-11-25: started inside the script - which this fixture runs nested in one
		// statement, so one transaction - its operator maps are written but the script's next statement,
		// "if not exists (DeviceOperators where Device_Name = ObjectName('TestDevice')) then raise Error('WTF')", still
		// fires. That is the catalog read rule rather than a defect: a D4 query over a catalog table the store answers
		// (CatalogDeviceTable) opens a connection of its own and sees committed data only, never the calling transaction's
		// uncommitted store writes - docs/architecture/catalog-and-store.md, "Store transactions". Started here, in its own
		// statement and so its own transaction, the maps are committed before the script reads for them.
		private static string Registration()
		{
			return
				String.Format
				(
					"EnsureLibraryRegistered('Frontend');\r\n" +
						"EnsureLibraryRegistered('TestFramework');\r\n" +
						"EnsureLibraryRegistered('MSSQLDevice');\r\n" +
						"EnsureLibraryRegistered('{0}');\r\n" +
						"SetLibrary('{0}');\r\n" +
						"{1}\r\n" +
						"{2}\r\n" +
						"EnsureLibraryRegistered('{3}');\r\n" +
						"Diagnostics.EnsureDeviceStarted('{4}');\r\n",
					DeviceLibraryName,
					DeviceDefinition(LibraryDeviceName),
					DeviceDefinition(DeviceUnderTestName),
					CorpusLibraryName,
					DeviceUnderTestName
				);
		}

		/// <summary>
		/// One SQL Server device against the configured server, in the reconciliation mode the library's own scripts defined
		/// both of these in: the device is the master and data-definition commands reach it, which is what lets the corpus
		/// create and drop its tables there.
		/// </summary>
		private static string DeviceDefinition(string deviceName)
		{
			return
				String.Format
				(
					"create device {0} reconciliation {{ mode = {{ command }}, master = device }} class \"MSSQLDevice.MSSQLDevice\" attributes {{ {1} }};",
					deviceName,
					TestEnvironment.MSSQLDeviceAttributes(DatabaseName)
				);
		}

		/// <summary>
		/// The device-coverage corpus against a real SQL Server, checked against the failure recorded above. The script's own
		/// error is reported whole: it is the list of every device case that failed, and that list is the result of the run.
		/// </summary>
		[Test]
		public void TheDeviceCoverageCorpusRunsAgainstTheServer()
		{
			if (!TestEnvironment.MSSQLConfigured)
				Assert.Ignore(TestEnvironment.MSSQLNotConfigured(String.Format("run the device-coverage corpus ({0}) against a SQL Server", ScriptName)));

			string error = RunTheScript();
			if (error == null)
				Assert.Fail(String.Format("{0} now runs to the end. Its known failure ({1}) has gone: record what it does now here, which is what this fixture is for.", ScriptName, KnownFailureTicket));
			if (!KnownFailureError.IsMatch(error))
				Assert.Fail(String.Format("known-failure ({0}): expected the error to match \"{1}\", but {2} failed with: {3}", KnownFailureTicket, KnownFailureError, ScriptName, error));
			Assert.Warn(String.Format("known-failure ({0}): {1} failed with: {2}", KnownFailureTicket, ScriptName, error));
		}

		/// <summary>Runs the script and returns null, or its whole error text with the newlines the roll-up puts in it.</summary>
		private string RunTheScript()
		{
			// The session's default library is the corpus library, as it was for the fixture this replaces: the script's
			// objects are created in the current library, and the corpus resolves its own tables and operators there.
			IServerSession session = _instance.Server.Connect(new SessionInfo("Admin", String.Empty, CorpusLibraryName));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					process.ExecuteScript(String.Format("ExecuteScript('{0}', '{1}');", CorpusLibraryName, ScriptName));
					return null;
				}
				catch (Exception exception)
				{
					// Not CoverageScripts.ErrorText, which flattens the message onto one line: the roll-up this script raises
					// is one line per failed device case, and that shape is the reason to read it.
					return FrontendFixture.FlattenMessages(exception);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				_instance.Server.Disconnect(session);
			}
		}
	}
}
