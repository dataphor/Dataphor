/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The PostgreSQL device executing against a real PostgreSQL server, through the device-coverage corpus of
	/// TestFramework.Coverage.Devices. Ignored unless DATAPHOR_TEST_PGSQL names a server.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The sibling of <see cref="LiveDeviceTest"/>, which does the same for the SQL Server device, and the first pass in this
	/// repository that sends a statement to a PostgreSQL server: nothing had since about 2017, and
	/// <see cref="PGSQLDeviceTranslationGoldenTests"/> - the only other pass that sees this device - contacts no server at
	/// all. So this fixture is where the device's type mapping, its parameter handling, its connection-string keywords and
	/// its error codes are actually exercised.
	/// </para>
	/// <para>
	/// The oracle is the D4 script, not this file (decision D2). TestPGSQLDevice.d4 runs the corpus - one row of
	/// Coverage.Devices.DeviceCase per case - against a device and raises an error naming every case that failed; this
	/// fixture creates the device the script needs, runs the script and reports what came back. There is deliberately no
	/// assertion here about what PostgreSQL ought to do with a value: a new expectation written in C# would be a second,
	/// unreviewed oracle beside the one the corpus already is.
	/// </para>
	/// <para>
	/// Two devices, for the reason <see cref="LiveDeviceTest"/> gives: registering TestFramework.Coverage.Devices runs its
	/// Register.d4, which sets TestingDevice as the library's default device and alters it, so that device has to exist
	/// first and the corpus's own tables are created on it; TestPGSQLDevice.d4 then drives TestDevice. Both are created in
	/// TestFramework.Coverage.Base, which is registered before the corpus library and is one of its requisites.
	/// </para>
	/// <para>
	/// Both devices are pointed at one database, as the SQL Server pass's are, and the database is emptied at the start of
	/// the run rather than at the end - see <see cref="TestEnvironment.PGSQLEmptyDatabaseStatement"/> for why, and for why
	/// the emptying goes out over the device's own connection instead of over one of the harness's.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: it starts a server, and two engines in one process share the engine's static state (see the note
	// on TestServerInstance).
	[TestFixture]
	public class LivePGSQLDeviceTest
	{
		/// <summary>The library holding the device-coverage corpus and the script that runs it.</summary>
		private const string CorpusLibraryName = "TestFramework.Coverage.Devices";

		/// <summary>The library the two devices are created in; see the fixture's remarks.</summary>
		private const string DeviceLibraryName = "TestFramework.Coverage.Base";

		/// <summary>The device the corpus library's own registration needs, as its descriptor's default device name.</summary>
		private const string LibraryDeviceName = "TestingDevice";

		/// <summary>The device TestPGSQLDevice.d4 drives.</summary>
		private const string DeviceUnderTestName = "TestDevice";

		/// <summary>The script that runs the corpus, in the corpus library's Documents folder.</summary>
		private const string ScriptName = "TestPGSQLDevice";

		/// <summary>
		/// The database this pass puts both devices' tables in, on the server DATAPHOR_TEST_PGSQL names. Lower case because
		/// PostgreSQL folds an unquoted identifier to lower case, and a database named in mixed case has to be quoted
		/// everywhere afterwards.
		/// </summary>
		private const string DatabaseName = "dataphordevicecoverage";

		/// <summary>
		/// The library folders the server carries. Frontend first in the registration below, because System.Internet's
		/// registration script - which nothing here needs, but which the library set could grow to include - calls a Frontend
		/// operator without declaring the dependency (bug-system-internet-registration-needs-frontend). MSSQLDevice is here
		/// although no SQL Server is involved: the corpus library's descriptor requires it, and registering a library
		/// registers its requisites, so its folder has to be present or the corpus library cannot be registered at all.
		/// </summary>
		private static readonly string[] Libraries =
		[
			"Frontend",
			"SimpleDevice",
			"Connection",
			"SQLDevice",
			"MSSQLDevice",
			"PGSQLConnection",
			"PGSQLDevice",
			"TestFramework",
			DeviceLibraryName,
			CorpusLibraryName
		];

		private TestServerInstance _instance;

		/// <summary>What the server says it is, for the matrix row this run produces; null where the query failed.</summary>
		private string _serverVersion;

		[OneTimeSetUp]
		public void BuildTheServer()
		{
			if (!TestEnvironment.PGSQLConfigured)
				return;

			_instance = new TestServerInstance("LivePGSQLDevice", Libraries);
			try
			{
				_instance.Server.Start();
				_instance.ExecuteScript(Registration());
				_serverVersion = ServerVersion();
				TestContext.Progress.WriteLine("PostgreSQL: {0}", _serverVersion ?? "(the version query failed)");
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
		/// database emptied, the corpus library itself - registering it creates the corpus tables on TestingDevice - and then
		/// the device under test started.
		/// </summary>
		/// <remarks>
		/// <para>
		/// PGSQLDevice is registered by name although the devices are created from its class: the class alias
		/// PostgreSQLDevice.PostgreSQLDevice does not exist until the library that declares it has been registered.
		/// </para>
		/// <para>
		/// TestingDevice is started here, in a statement of its own, before the database is emptied: the emptying goes out
		/// over that device's connection, so the device has to be started first - and starting it is also what creates the
		/// database and the DAE_ support functions, which the emptying must therefore not remove (it drops tables only).
		/// The device under test is started in a statement of its own for the reason LiveDeviceTest records: a D4 query over
		/// a catalog table the store answers sees committed data only, so the operator maps a device start writes have to be
		/// committed before the script reads for them.
		/// </para>
		/// </remarks>
		private static string Registration()
		{
			return
				String.Format
				(
					"EnsureLibraryRegistered('Frontend');\r\n" +
						"EnsureLibraryRegistered('TestFramework');\r\n" +
						"EnsureLibraryRegistered('PGSQLDevice');\r\n" +
						"EnsureLibraryRegistered('{0}');\r\n" +
						"SetLibrary('{0}');\r\n" +
						"{1}" +
						"{2}" +
						"Diagnostics.EnsureDeviceStarted('{3}');\r\n" +
						"{4}" +
						"EnsureLibraryRegistered('{5}');\r\n" +
						"Diagnostics.EnsureDeviceStarted('{6}');\r\n",
					DeviceLibraryName,
					DeviceDefinition(LibraryDeviceName),
					DeviceDefinition(DeviceUnderTestName),
					LibraryDeviceName,
					TestEnvironment.PGSQLEmptyDatabaseStatement(LibraryDeviceName),
					CorpusLibraryName,
					DeviceUnderTestName
				);
		}

		/// <summary>
		/// One PostgreSQL device against the configured server, in the reconciliation mode the corpus needs: the device is
		/// the master and data-definition commands reach it, which is what lets the corpus create and drop its tables there.
		/// </summary>
		private static string DeviceDefinition(string deviceName)
		{
			return TestEnvironment.PGSQLDeviceDefinition(deviceName, DatabaseName, "command");
		}

		/// <summary>
		/// What the server reports for <c>version()</c>, read through the device, or null where that query failed. Recorded
		/// for the supported-version matrix in docs/users-guide/dataphor-service.md: this fixture is what that matrix is
		/// measured with, one PostgreSQL version at a time.
		/// </summary>
		private string ServerVersion()
		{
			IServerSession session = _instance.Server.Connect(new SessionInfo("Admin", String.Empty, DeviceLibraryName));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					IServerCursor cursor =
						process.OpenCursor
						(
							// The key is a D4 key definition, not a column list: SQLQueryNode hands it to Parser.ParseKeyDefinition.
							String.Format("SQLDevice.SQLQuery('{0}', 'select version() as \"Version\"', 'key {{ Version }}')", LibraryDeviceName),
							null
						);
					try
					{
						if (!cursor.Next())
							return null;
						using (IRow row = cursor.Select())
							return Convert.ToString(row["Version"]);
					}
					finally
					{
						process.CloseCursor(cursor);
					}
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

		/// <summary>
		/// The device read the version of the server it is pointed at. DetermineVersion takes the second space-separated word
		/// of what <c>version()</c> returns and the characters before its first dot as the major version, so a server whose
		/// version string it cannot parse fails the device's start outright - and no other pass can see that, because without
		/// a server nothing calls it. The version string itself goes to the test output, so that the TRX of any run records
		/// which server the run proved.
		/// </summary>
		/// <remarks>
		/// MajorVersion is read off the started device by reflection because the device assembly is loaded from the runtime
		/// layout at run time - that is what a library is - so this project cannot reference it. The same reason
		/// MSSQLVersionMatrixTest reads it that way.
		/// </remarks>
		[Test]
		public void TheDeviceReadsTheServerVersion()
		{
			if (!TestEnvironment.PGSQLConfigured)
				Assert.Ignore(TestEnvironment.PGSQLNotConfigured("measure what the PostgreSQL device makes of a PostgreSQL version"));

			TestContext.Out.WriteLine("PostgreSQL: {0}", _serverVersion);
			Assert.That(_serverVersion, Does.StartWith("PostgreSQL "), "the server did not report a version string of the shape the device parses.");

			string reported = _serverVersion.Split(' ')[1];
			int reportedMajor = Int32.Parse(reported.Substring(0, reported.IndexOf('.')));
			Assert.That(DeviceProperty(LibraryDeviceName, "MajorVersion"), Is.EqualTo(reportedMajor), "the device did not determine the server's major version.");
		}

		/// <summary>
		/// The families of device-case failure this corpus still has against a PostgreSQL server, each with the ticket that
		/// owns it and a pattern the roll-up must contain. The same bargain a known-failure entry of the coverage manifest
		/// strikes, and the one <see cref="LiveDeviceTest"/> strikes in C#: the corpus must fail, and fail in these ways, or
		/// the run fails - so a family that is fixed, or a new one that appears, is reported rather than absorbed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Measured 2026-11-26 against PostgreSQL 18.7 in a container, three runs of the same build. The four patterns below
		/// were present in every run; two further families are real and are not pinned here because they are intermittently
		/// hidden, for the reason in the next paragraph: "System.UserID compared sensitively" (two cases - PostgreSQL compares
		/// a character varying column by the database collation, which is case-sensitive, where the corpus expects D4's
		/// case-insensitive UserID; it is the mirror of the case-insensitive arm of bug-mssql-device-text-columns-lose-characters,
		/// and recorded there), and the Time cases after the first.
		/// </para>
		/// <para>
		/// Which members of the ordering family fail changes between identical runs - measured: of the pair at sequence 300 and
		/// 301 of each scalar type, run to run a different member fails. That is the same instability
		/// bug-mssql-device-live-coverage-failures recorded against SQL Server, so only the family's message is pinned, never a
		/// case id.
		/// </para>
		/// <para>
		/// The corpus also cannot run to its end today, and that is itself one of the recorded families: a PostgreSQL
		/// transaction is unusable after any statement error, the corpus runs inside one transaction, and the connection class
		/// does not report the failure - so the first server-side error (the Time one) makes every later case fail with 25P02
		/// or with a leftover table from the case that could not clean up. About sixty cases are hidden behind it.
		/// bug-pgsql-connection-discards-the-error-code owns that.
		/// </para>
		/// </remarks>
		private static readonly (string Ticket, Regex Pattern)[] KnownFailureFamilies =
		[
			(
				"bug-mssql-device-live-coverage-failures",
				new Regex(@"Next\(\) returned an unexpected row", RegexOptions.CultureInvariant)
			),
			(
				"bug-mssql-device-live-coverage-failures",
				new Regex(@"current time values were not retrieved properly", RegexOptions.CultureInvariant)
			),
			(
				"bug-binary-comparison-needs-the-mssql-device",
				new Regex(@"Violation of insert transition for constraint ""Key"" defined on table ""[^""]*ListBinaryTestTable""", RegexOptions.CultureInvariant)
			),
			(
				"bug-pgsql-device-time-is-timestamp",
				new Regex(@"operator does not exist: time without time zone <> timestamp without time zone", RegexOptions.CultureInvariant)
			),
			(
				"bug-pgsql-connection-discards-the-error-code",
				new Regex(@"25P02: current transaction is aborted", RegexOptions.CultureInvariant)
			)
		];

		/// <summary>
		/// The device-coverage corpus against a real PostgreSQL server, checked against the failure families recorded above.
		/// The script's own error is reported whole: it is the list of every device case that failed, and that list is the
		/// result of the run.
		/// </summary>
		[Test]
		public void TheDeviceCoverageCorpusRunsAgainstTheServer()
		{
			if (!TestEnvironment.PGSQLConfigured)
				Assert.Ignore(TestEnvironment.PGSQLNotConfigured(String.Format("run the device-coverage corpus ({0}) against a PostgreSQL server", ScriptName)));

			string error = RunTheScript();
			if (error == null)
				Assert.Fail(String.Format("{0} now runs to the end against PostgreSQL ({1}). Every recorded failure family has gone: record what it does now here, which is what this fixture is for.", ScriptName, _serverVersion));

			foreach ((string ticket, Regex pattern) in KnownFailureFamilies)
				if (!pattern.IsMatch(error))
					Assert.Fail(String.Format("known-failure ({0}): expected the roll-up to match \"{1}\", and it did not. Against PostgreSQL ({2}) {3} failed with:\r\n{4}", ticket, pattern, _serverVersion, ScriptName, error));

			Assert.Warn(String.Format("known failures against PostgreSQL ({0}): {1} failed with:\r\n{2}", _serverVersion, ScriptName, error));
		}

		/// <summary>The value the byte-array case below writes and reads back, as D4 spells one - the base64 the MSSQLBinary selector takes.</summary>
		private const string ByteArrayValue = "07f9b0d7ea0843708a9e24245b19e12e";

		/// <summary>
		/// A column of the shared SQL-device scalar type <c>SQLDevice.MSSQLBinary</c> can be created on the device, written to
		/// and read back. Both halves were broken, and each in a place nothing else in the tree touches: the device declared
		/// the column <c>bytea(30)</c>, which PostgreSQL rejects outright because <c>bytea</c> takes no type modifier, and
		/// <c>PostgreSQLCommand</c> had no branch for the <c>SQLByteArrayType</c> parameter such a column binds, so an insert
		/// raised <em>unknown SQL data type</em> from the connection class before reaching the server.
		/// </summary>
		/// <remarks>
		/// A case in C# rather than one in the corpus because the corpus cannot reach it: its MSSQLBinary cases are named for
		/// the SQL Server device's own scalar type, so they are not applicable to this device, and the translation goldens pin
		/// a corpus shared by both devices that has no column of this type. The read-back compares the bytes in the engine
		/// rather than in D4, so that what is asserted is the device's own round trip and not the pushed-down comparison of
		/// this type, which is a separate open question (bug-binary-comparison-needs-the-mssql-device). The table is dropped
		/// again, because this fixture's database is emptied only at the start of a run.
		/// </remarks>
		[Test]
		public void TheDeviceRoundTripsAByteArrayColumn()
		{
			if (!TestEnvironment.PGSQLConfigured)
				Assert.Ignore(TestEnvironment.PGSQLNotConfigured("write and read an SQLDevice.MSSQLBinary column on a PostgreSQL device"));

			_instance.ExecuteScript
			(
				String.Format
				(
					"SetLibrary('{0}');\r\n" +
						"create table ByteArrayColumnTest in {1} {{ ID : Integer, Payload : SQLDevice.MSSQLBinary, key {{ ID }} }};\r\n" +
						"insert table {{ row {{ 1 ID, SQLDevice.MSSQLBinary(\"{2}\") Payload }} }} into ByteArrayColumnTest;\r\n",
					DeviceLibraryName,
					DeviceUnderTestName,
					ByteArrayValue
				)
			);
			try
			{
				Assert.That
				(
					ReadTheByteArrayColumn(),
					Is.EqualTo(Convert.FromBase64String(ByteArrayValue)),
					"the value read back from the SQLDevice.MSSQLBinary column is not the one that was written."
				);
			}
			finally
			{
				_instance.ExecuteScript(String.Format("SetLibrary('{0}');\r\ndrop table ByteArrayColumnTest;\r\n", DeviceLibraryName));
			}
		}

		/// <summary>The one byte-array value the case above wrote, read back off the device through the engine.</summary>
		private byte[] ReadTheByteArrayColumn()
		{
			IServerSession session = _instance.Server.Connect(new SessionInfo("Admin", String.Empty, DeviceLibraryName));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					IServerCursor cursor = process.OpenCursor("select ByteArrayColumnTest", null);
					try
					{
						Assert.That(cursor.Next(), Is.True, "the row inserted into ByteArrayColumnTest is not there.");
						using (IRow row = cursor.Select())
							return ((IScalar)row.GetValue("Payload")).AsByteArray;
					}
					finally
					{
						process.CloseCursor(cursor);
					}
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

		/// <summary>Runs the script and returns null, or its whole error text with the newlines the roll-up puts in it.</summary>
		private string RunTheScript()
		{
			// The session's default library is the corpus library: the script's objects are created in the current library,
			// and the corpus resolves its own tables and operators there.
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

		/// <summary>One property of a started device, by reflection; see TheDeviceReadsTheServerVersion.</summary>
		private object DeviceProperty(string deviceName, string name)
		{
			string qualifiedName = DeviceLibraryName + "." + deviceName;
			int index = _instance.Server.Catalog.IndexOfName(qualifiedName);
			Assert.That(index, Is.GreaterThanOrEqualTo(0), String.Format("{0} is not in the catalog.", qualifiedName));
			object device = _instance.Server.Catalog[index];
			PropertyInfo property = device.GetType().GetProperty(name);
			Assert.That(property, Is.Not.Null, String.Format("{0} has no property {1}; it has been renamed, and this fixture has to follow it.", device.GetType().Name, name));
			return property.GetValue(device);
		}
	}
}
