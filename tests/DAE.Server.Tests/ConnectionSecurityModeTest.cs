/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel.Channels;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What a client does with a connection setting that asks for a secured connection: refuse it. The connection this
	/// version builds carries no transport security, and connecting unsecured instead would defeat exactly what the
	/// setting asked for.
	/// </summary>
	/// <remarks>
	/// No server, and no port opened: every case here is settled before a connection is opened, which is the property the
	/// refusal has to have - a connection must make no unsecured call on its way to being refused - so the fixture costs
	/// milliseconds. The server's half of this work is two log warnings, which are verified by inspection: asserting a log
	/// line needs a server with logging on, and this suite deliberately runs its servers with logging off (see the root
	/// AGENTS.md).
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: one case installs a client binding hook, which is a process-wide static field, so it changes
	// what every client built in this process would connect with.
	public class ConnectionSecurityModeTest
	{
		/// <summary>An override port, which keeps the listener out of the path. Nothing is connected to, so no port is opened.</summary>
		private const int OverridePort = 18061;

		private const string HostName = "localhost";
		private const string InstanceName = "ConnectionSecurityMode";

		/// <summary>
		/// Puts the client hooks back whatever a case did with them, on the failure path too: they are process-wide, so a
		/// hook left installed would be in force for every fixture that runs after this one.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			DataphorServiceUtility.RevertHooksToDefault();
		}

		/// <summary>
		/// The refusal itself, and what the message owes the person who has to act on it: the setting that caused it, and
		/// the one route to a secured connection there has ever been.
		/// </summary>
		[Test]
		public void TestAConnectionAskingForTransportSecurityIsRefused()
		{
			Exception error =
				Assert.Catch
				(
					() => Open(ConnectionSecurityMode.Transport, ConnectionSecurityMode.Default, OverridePort),
					"A connection asking for transport security was opened rather than refused."
				);
			Assert.That(error, Is.InstanceOf<NotSupportedException>(), "Actual: " + error);
			Assert.That(error.Message, Does.Contain("SecurityMode"), "The refusal does not name the setting that caused it. Actual: " + error.Message);
			Assert.That(error.Message, Does.Contain("BuildBindingCall"), "The refusal does not name the one route to a secured connection. Actual: " + error.Message);
		}

		/// <summary>
		/// The listener's setting is a request only on the path that uses the listener. The refusing case runs with no
		/// listener anywhere, so a refusal that arrived after the discovery call would fail on the exception type.
		/// </summary>
		[Test]
		public void TestTheListenersSecurityModeIsCheckedOnlyWhereTheListenerIsUsed()
		{
			Exception error =
				Assert.Catch
				(
					() => Open(ConnectionSecurityMode.Default, ConnectionSecurityMode.Transport, 0),
					"A connection whose listener lookup asked for transport security was opened rather than refused."
				);
			Assert.That(error, Is.InstanceOf<NotSupportedException>(), "The connection was refused by something other than the security check, so the listener was contacted first. Actual: " + error);
			Assert.That(error.Message, Does.Contain("ListenerSecurityMode"), "The refusal does not name the listener's setting. Actual: " + error.Message);

			Assert.DoesNotThrow
			(
				() => Open(ConnectionSecurityMode.Default, ConnectionSecurityMode.Transport, OverridePort),
				"A connection given an override port was refused for a listener setting it never consults."
			);
		}

		/// <summary>Default and None both mean an unsecured connection, which is what they get; only Transport is a request this version cannot honour.</summary>
		[Test]
		public void TestUnsecuredModesAreNotRefused()
		{
			Assert.DoesNotThrow(() => Open(ConnectionSecurityMode.Default, ConnectionSecurityMode.Default, OverridePort));
			Assert.DoesNotThrow(() => Open(ConnectionSecurityMode.None, ConnectionSecurityMode.None, OverridePort));
		}

		/// <summary>
		/// A site that supplies its own binding owns that channel's security, and is the one deployment that could ever
		/// have had a secured connection, so nothing is refused there.
		/// </summary>
		[Test]
		public void TestASiteSuppliedBindingSuppressesTheRefusal()
		{
			// The product's own binding is fetched before the hook is installed, for two reasons: it is what the hook hands
			// back, and fetching it first leaves DataphorServiceUtility's memoized binding the product's own, so this case
			// cannot change what a client built by a later fixture connects with.
			Binding productBinding = DataphorServiceUtility.GetBinding();
			DataphorServiceUtility.BuildBindingCall = () => productBinding;
			Assert.That(DataphorServiceUtility.HasCustomBinding, Is.True, "A replaced BuildBindingCall is still reported as the product's own binding.");

			Assert.DoesNotThrow
			(
				() => Open(ConnectionSecurityMode.Transport, ConnectionSecurityMode.Default, OverridePort),
				"A connection was refused at a site whose own binding may well secure the transport."
			);

			DataphorServiceUtility.RevertHooksToDefault();
			Assert.That(DataphorServiceUtility.HasCustomBinding, Is.False, "Reverting the hooks left the binding reported as a site's own.");
		}

		/// <summary>
		/// The four-argument NativeCLIClient constructor applies its security mode to the connection, not to the listener.
		/// It passed the caller's mode into the listener's slot and left the connection's at Default; with an override port
		/// given the listener's mode is not consulted at all, so before the fix nothing here was refused.
		/// </summary>
		[Test]
		public void TestTheNativeClientsSecurityModeIsTheConnections()
		{
			Exception error =
				Assert.Catch
				(
					() => { using (new NativeSessionCLIClient(HostName, InstanceName, OverridePort, ConnectionSecurityMode.Transport)) { } },
					"A native CLI client asking for transport security was built rather than refused."
				);
			Assert.That(error, Is.InstanceOf<NotSupportedException>(), "Actual: " + error);
			Assert.That(error.Message, Does.Contain("SecurityMode"), "The refusal does not name the setting that caused it. Actual: " + error.Message);
			Assert.That(error.Message, Does.Not.Contain("ListenerSecurityMode"), "The refusal names the listener's setting, so the mode still reaches the listener's slot. Actual: " + error.Message);
		}

		/// <summary>Builds a client the way a connection alias builds one, and closes it again when it opened.</summary>
		/// <remarks>
		/// A bare ClientServer rather than a LocalServer: no engine is wanted here, and the constructor calls Open, so a
		/// refusal arrives from the constructor.
		/// </remarks>
		private static void Open(ConnectionSecurityMode securityMode, ConnectionSecurityMode listenerSecurityMode, int overridePortNumber)
		{
			ClientServer clientServer = new ClientServer(HostName, InstanceName, overridePortNumber, securityMode, 0, listenerSecurityMode);
			clientServer.Close();
		}
	}
}
