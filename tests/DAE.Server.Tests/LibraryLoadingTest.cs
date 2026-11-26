/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Exercises <c>LibraryAssemblyResolver</c>: a library's private managed package and private native dll both live flat in
	/// the library's folder, and the resolver finds them when its assembly asks for them. See port-library-loading.
	/// </summary>
	/// <remarks>
	/// LibraryLoadingProbe's folder in the layout holds Newtonsoft.Json.Bson.dll (a managed package not present in the host's
	/// bin) and Microsoft.Data.SqlClient.SNI.dll (a native dll flattened for the Windows runtime). The probe's two operators
	/// trip the two resolver hooks: ProbeManagedResolver references a Bson type (whose assembly must be found by the managed
	/// hook on the library folder), and ProbeNativeResolver loads the SNI dll through its own assembly's load context, which
	/// goes through the unmanaged hook.
	///
	/// The Connection arm is a regression guard: that library's own four private managed packages have been copied flat into
	/// its folder since <c>build-runtime-layout</c>, and registering it must still work once the resolver is installed.
	///
	/// The ODBCConnection arm is the same guard for the library device-connection-cleanup brought back into the build: its
	/// folder holds one private managed package, System.Data.Odbc, and registering the library must not need an ODBC driver
	/// manager on the machine - the connection class is constructed only when a device names it as its ConnectionClass.
	///
	/// The PGSQLConnection arm is that guard again for the library device-pgsql-connection-port brought back: its folder
	/// holds one private managed package, the Npgsql driver, and its .d4l lists Npgsql.dll, so registering the library must
	/// need no PostgreSQL server, the driver connecting only when a device or a catalog store asks it to. Unlike the other
	/// three arms it no longer shows where the driver was loaded from; the note in the case says why.
	/// </remarks>
	[TestFixture]
	public class LibraryLoadingTest : InProcessTestFixture
	{
		protected override string[] Libraries => new[] { "LibraryLoadingProbe", "Connection", "ODBCConnection", "PGSQLConnection" };

		protected override string[] LibrariesToRegister => new[] { "LibraryLoadingProbe" };

		[Test]
		public void ManagedPrivatePackageResolvesFromLibraryFolder()
		{
			object result = Instance.Evaluate("ProbeManagedResolver()");
			Assert.That(result, Is.EqualTo("Newtonsoft.Json.Bson"));
		}

		[Test]
		public void NativePrivateDependencyResolvesFromLibraryFolder()
		{
			object result = Instance.Evaluate("ProbeNativeResolver()");
			Assert.That(result, Is.EqualTo("Microsoft.Data.SqlClient.SNI"));
		}

		[Test]
		public void ConnectionLibraryStillRegistersWithFlatPrivatePackages()
		{
			Assert.DoesNotThrow(() => Instance.ExecuteScript("EnsureLibraryRegistered('Connection');"));
		}

		[Test]
		public void ODBCConnectionLibraryRegistersWithoutADriverManager()
		{
			Assert.DoesNotThrow(() => Instance.ExecuteScript("EnsureLibraryRegistered('ODBCConnection');"));
		}

		[Test]
		public void PGSQLConnectionLibraryRegistersWithoutAPostgreSQLServer()
		{
			Assert.DoesNotThrow(() => Instance.ExecuteScript("EnsureLibraryRegistered('PGSQLConnection');"));

			// NOTE: this arm used to go on to assert that an Npgsql assembly was in the process's loaded set, on the ground
			// that nothing else in this process referenced the driver, so the only thing that could have loaded it was the
			// .d4l entry for Npgsql.dll. That ground is gone since store-pgsql-port: this project references the PostgreSQL
			// catalog store, whose connection classes are this library's, so Npgsql.dll is in this assembly's own folder and
			// another fixture may already have loaded it from there - which would have made the assertion pass without
			// registering anything. It was removed rather than weakened, because an assertion that cannot fail is worse than
			// its absence. What this arm still establishes is its own title: the library registers, with the driver's files
			// where the layout puts them and no PostgreSQL server in reach. The resolver finding a library's private package
			// in the library folder is what the LibraryLoadingProbe arms above prove, and they prove it properly: the probe's
			// Newtonsoft.Json.Bson.dll and Microsoft.Data.SqlClient.SNI.dll are in no other folder this process can load from.
		}
	}
}
