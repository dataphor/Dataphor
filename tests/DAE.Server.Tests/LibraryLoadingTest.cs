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
	/// </remarks>
	[TestFixture]
	public class LibraryLoadingTest : InProcessTestFixture
	{
		protected override string[] Libraries => new[] { "LibraryLoadingProbe", "Connection" };

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
	}
}
