/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.Versioning;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Service;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The tray configuration utility asks <see cref="ServiceUtility"/> for the status and start mode of whichever instance is
	/// selected, on a timer, whether or not that instance has a service registered. Both answers must therefore be a default
	/// rather than an exception, which is the branch most easily lost in the move off the .NET Framework installer classes
	/// (host-service-registration).
	///
	/// Install, Uninstall and SetServiceAutoStart are not tested: they need administrative rights and change machine state, and
	/// a mock of the service control manager would test the mock. They are checked by hand from an elevated prompt.
	/// </summary>
	// The members under test are annotated Windows-only, so the fixture is too: the product is Windows-only and the suite runs there.
	[SupportedOSPlatform("windows")]
	[TestFixture]
	public class ServiceUtilityTest
	{
		/// <summary>An instance name no service can be registered under: a fresh GUID, so the test cannot collide with a real installation.</summary>
		private static string UnregisteredInstanceName()
		{
			return "Test" + Guid.NewGuid().ToString("N");
		}

		[Test]
		public void UnregisteredServiceReportsDefaults()
		{
			string instanceName = UnregisteredInstanceName();

			Assert.That(ServiceUtility.GetServiceStatus(instanceName), Is.EqualTo(ServiceStatus.Unavailable));
			Assert.That(ServiceUtility.GetServiceAutoStart(instanceName), Is.False);
		}
	}
}
