/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The translation corpus of <see cref="DeviceTranslationGoldenTests"/> against the PostgreSQL device, with the
	/// recording connection in place of the PostgreSQL driver. The goldens are under tests/goldens/devices/PGSQL.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This fixture is the whole of what the suite knows about the PostgreSQL device: nothing in the repository has sent a
	/// statement to a PostgreSQL server since about 2017, and a green run here says the device generates the text in those
	/// files and nothing at all about whether a server accepts it. device-pgsql-port is the pass that connects.
	/// </para>
	/// <para>
	/// The three settings turned off below are the device's own, not test-only switches, and each of them connects when
	/// the device starts (DetermineVersion, EnsureDatabase and EnsureOperators in
	/// libraries/PGSQLDevice/Source/Device.PGSQL/PostgreSQLDevice.cs). Two of the three default to true, so turning them
	/// off is what lets the device start with no server; ShouldDetermineVersion already defaults to false here, where the
	/// SQL Server device defaults it true, and it is set anyway so that a change to that default cannot make this fixture
	/// start connecting.
	/// </para>
	/// <para>
	/// Port and SearchPath are deliberately not given. The device defaults them (5432 and empty), and the point of the
	/// Connection case of the corpus is to pin what a definition that names only a server and a database gets - which is
	/// the shape of nearly every device definition a deployment has, and the only check available without a server on the
	/// connection string this device builds. The keyword legend behind that string
	/// (PostgreSQLConnectionStringBuilder) is a compatibility surface - its tag names are what a device definition writes -
	/// and the review of device-pgsql-connection-port measured the whole string against the pinned driver, keyword by
	/// keyword, so a difference in that golden means the device changed rather than that the legend needs correcting. Two
	/// of the six keywords in the golden come from the session rather than from the device definition: User Id and Password
	/// are the device user PostgreSQLDeviceSession was handed, which for this fixture is the test instance's Admin with an
	/// empty password - so the golden moves if the harness ever connects as someone else. And the empty SearchPath= in it is
	/// a no-op at the driver, not a request to empty the search path; the NOTE in PostgreSQLConnection.cs has the
	/// measurement.
	/// </para>
	/// </remarks>
	// Not [Parallelizable]: see the base class.
	[TestFixture]
	public class PGSQLDeviceTranslationGoldenTests : DeviceTranslationGoldenTests
	{
		protected override string DeviceLibraryName => "PGSQLDevice";

		protected override string GoldensDeviceFolderName => "PGSQL";

		/// <summary>
		/// The base list plus PGSQLConnection, which PGSQLDevice.d4l requires: registering a library registers its
		/// requisites, so the connection library's folder has to be one of the folders this fixture's server carries. It is
		/// also what loads the PostgreSQL driver assembly, which this fixture never asks to open anything.
		/// </summary>
		protected override string[] Libraries => ["Connection", "PGSQLConnection", "SQLDevice", DeviceLibraryName, RecorderLibraryName];

		/// <summary>A server name no machine resolves, so that a device that connected for real would fail rather than reach something.</summary>
		protected override string UnreachableServerName => "no.such.server.invalid";

		/// <summary>
		/// The database name is lower case where the SQL Server device's is mixed case, because PostgreSQL folds an
		/// unquoted identifier to lower case and a database named in mixed case is a database that has to be quoted
		/// everywhere afterwards.
		/// </summary>
		protected override string CreateDeviceStatement =>
			@"create device " + DeviceName + @"
	reconciliation { mode = { command }, master = device }
	class ""PostgreSQLDevice.PostgreSQLDevice""
	attributes
	{
		""Server"" = """ + UnreachableServerName + @""",
		""Database"" = ""dataphordevicegoldens"",
		""ConnectionClass"" = ""SQLRecorder.RecordingConnection"",
		""ShouldDetermineVersion"" = ""False"",
		""ShouldEnsureDatabase"" = ""False"",
		""ShouldEnsureOperators"" = ""False""
	};";

		/// <summary>
		/// What this device refuses, which is the same pair the SQL Server device refuses and for the same two reasons:
		/// difference is the one relational operator neither SQL device translates, and an expression over a table in
		/// another device cannot be pushed into this one.
		/// </summary>
		protected override UnsupportedCase[] UnsupportedCases =>
		[
			// SQLException.Codes.QueryUnsupported
			new UnsupportedCase("Difference", "(Employee over { ID }) minus (Department over { ID })", 126125),
			// SQLException.Codes.QuerySupportedByDifferentDevice
			new UnsupportedCase("TableInAnotherDevice", OtherDeviceTableName, 126124),
		];

		// RequiredConnectionStringText is deliberately not overridden: this device defaults nothing into its connection
		// strings for compatibility, where the SQL Server device has to supply Encrypt=False. The whole string is pinned by
		// the Connection case of the corpus instead, so nothing about it goes unchecked.
	}
}
