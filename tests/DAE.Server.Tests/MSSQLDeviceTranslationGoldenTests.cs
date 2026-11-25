/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The translation corpus of <see cref="DeviceTranslationGoldenTests"/> against the SQL Server device, with the
	/// recording connection in place of a SQL Server client. The goldens are under tests/goldens/devices/MSSQL.
	/// </summary>
	/// <remarks>
	/// The three settings turned off below are the device's own, not test-only switches: a deployment can set them, and
	/// each of them connects when the device starts (DetermineVersion, EnsureDatabase and EnsureOperators in
	/// libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLDevice.cs). With ShouldDetermineVersion off the device keeps its
	/// default version assumption, which is what the goldens pin; see the remarks on the base class.
	/// </remarks>
	// Not [Parallelizable]: see the base class.
	[TestFixture]
	public class MSSQLDeviceTranslationGoldenTests : DeviceTranslationGoldenTests
	{
		protected override string DeviceLibraryName => "MSSQLDevice";

		protected override string GoldensDeviceFolderName => "MSSQL";

		/// <summary>A server name no machine resolves, so that a device that connected for real would fail rather than reach something.</summary>
		protected override string UnreachableServerName => "no.such.server.invalid";

		/// <summary>
		/// Microsoft.Data.SqlClient encrypts by default and then validates the server's certificate, which
		/// System.Data.SqlClient - the client this device used through release 3 - did neither of. A device definition
		/// written then names a server and a database and nothing about encryption, so the device supplies what the old
		/// client negotiated; without it such a definition stops connecting to any SQL Server whose certificate is
		/// self-signed or absent. MSSQLADODotNetConnectionStringBuilder sets it and says why at the site;
		/// docs/architecture/devices.md says where the other half of the same decision lives, in the catalog store.
		/// </summary>
		protected override string[] RequiredConnectionStringText => ["Encrypt=False"];

		protected override string CreateDeviceStatement =>
			@"create device " + DeviceName + @"
	reconciliation { mode = { command }, master = device }
	class ""MSSQLDevice.MSSQLDevice""
	attributes
	{
		""ServerName"" = """ + UnreachableServerName + @""",
		""DatabaseName"" = ""DataphorDeviceGoldens"",
		""ConnectionClass"" = ""SQLRecorder.RecordingConnection"",
		""ShouldDetermineVersion"" = ""False"",
		""ShouldEnsureDatabase"" = ""False"",
		""ShouldEnsureOperators"" = ""False"",
		""UseIntegratedSecurity"" = ""True""
	};";

		/// <summary>
		/// What this device refuses. Difference is the one relational operator the SQL Server device does not translate
		/// at all (the comment at the top of MSSQLDevice.cs lists it as unsupported), and an expression over a table in
		/// another device cannot be pushed into this one.
		/// </summary>
		protected override UnsupportedCase[] UnsupportedCases =>
		[
			// SQLException.Codes.QueryUnsupported
			new UnsupportedCase("Difference", "(Employee over { ID }) minus (Department over { ID })", 126125),
			// SQLException.Codes.QuerySupportedByDifferentDevice
			new UnsupportedCase("TableInAnotherDevice", OtherDeviceTableName, 126124),
		];
	}
}
