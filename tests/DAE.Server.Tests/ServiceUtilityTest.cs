/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Reflection;
using System.Runtime.InteropServices;
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
	/// Two further cases pin what the build cannot check: that the string table is embedded under the name Strings looks it
	/// up by, and that the event-log source name this assembly keeps its own copy of still matches the engine's.
	///
	/// Install, Uninstall and SetServiceAutoStart are not tested: they need administrative rights and change machine state, and
	/// a mock of the service control manager would test the mock. They are checked by hand from an elevated prompt. What can be
	/// checked without running them is that their interop declarations resolve, which the last case does.
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

		[Test]
		public void StringTableIsEmbeddedUnderTheNameItIsLookedUpBy()
		{
			// DAE.Host.csproj overrides RootNamespace so that Strings.resx keeps the manifest name Strings.BaseName spells.
			// Nothing in the build checks that the two agree: a mismatch compiles, and every lookup then fails at run time,
			// where the first reader is the service's own console output.
			Assert.That(Alphora.Dataphor.DAE.Service.Strings.Get("ServiceStarting"), Is.Not.Null.And.Not.Empty);
		}

		[Test]
		public void EventLogSourceMatchesTheEngineConstantItCopies()
		{
			// EventLogSource is a copy of Engine.ServerSourceName, kept so that DAE.Host needs no reference to the engine:
			// the service writes its exceptions under it and the server creates it. Drift would split a deployment's event
			// log entries between two sources, and neither assembly would notice.
			Assert.That(ServiceUtility.EventLogSource, Is.EqualTo(Engine.ServerSourceName));
		}

		[Test]
		public void EveryServiceControlManagerImportResolves()
		{
			// The three members that register or reconfigure a service cannot run here, so their advapi32 declarations are
			// otherwise unexercised: a misspelled entry point or a wrong character set would surface only at the elevated
			// manual check. Marshal.Prelink resolves a declaration without calling it, and throws EntryPointNotFoundException
			// when it cannot. Reflection reaches the private declarations deliberately: they are the subject of this case, and
			// exposing them to make it reachable would be worse.
			MethodInfo[] imports =
				Array.FindAll
				(
					typeof(ServiceUtility).GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly),
					method => (method.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl
				);

			Assert.That(imports, Is.Not.Empty, "ServiceUtility declares no platform invoke: the interop has moved, and this case no longer checks anything");

			foreach (MethodInfo import in imports)
				Marshal.Prelink(import);
		}
	}
}
