/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using CoreWCF.Configuration;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Service;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The host's half of the protocol's extension points, <see cref="DataphorServiceHostHooks"/>: a deployment can
	/// replace what a Dataphor server listens with, which is the only route there has ever been to a secured connection.
	/// </summary>
	/// <remarks>
	/// A fixture of its own rather than more cases in CoreWcfProtocolTest, whose one shared host is started before any
	/// case runs and assumes the default binding. The host plumbing both use is in <see cref="HostedInstance"/>.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: the hooks are process-wide static fields, so a case here changes what every host built in
	// this process would be served on; and two servers in one process share the engine's static state (see the note on
	// TestServerInstance).
	public class HostBindingHookTest
	{
		/// <summary>What a throwing hook throws, so that a case can find it again in the failure that reaches the caller.</summary>
		private const string ThrowingHookMessage = "HostBindingHookTest installed a hook that throws.";

		/// <summary>
		/// Puts the hooks back whatever a case did with them, on the failure path too: they are process-wide, so a hook
		/// left installed would be in force for every fixture that runs after this one.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			DataphorServiceHostHooks.RevertHooksToDefault();
		}

		/// <summary>
		/// The capability the hook exists for: what the server listens with is the site's, not the product's. The text
		/// message encoder in place of the binary one is the smallest unambiguous difference - no shipped client speaks it
		/// - so a client assembled exactly as a connection alias assembles one cannot complete a call against a host
		/// built from the replaced hook, and can against the default. Nothing short of this establishes the capability:
		/// the hook being called proves only that it was called.
		/// </summary>
		[Test]
		public void TestAReplacedBindingChangesWhatTheServerAccepts()
		{
			DataphorServiceHostHooks.BuildBindingCall = TextEncodedBinding;
			Assert.That(DataphorServiceHostHooks.HasCustomBinding, Is.True, "A replaced BuildBindingCall is still reported as the product's own binding.");
			using (HostedInstance host = new HostedInstance("HostBindingHookReplaced", false, 0))
			{
				Exception error =
					Assert.Catch
					(
						() => Establish(host),
						"The shipped client completed a call against a server listening with a binding it cannot speak, so the replaced hook never reached the wire."
					);
				// The reason, not only a failure: the server is up and reachable, and the call fails because the two
				// halves no longer agree on the wire. Anything else would pass the assertion above for the wrong reason.
				Assert.That(error, Is.InstanceOf<ServerException>(), "The call failed as something other than a communication failure. Actual: " + error);
				Assert.That((long)((ServerException)error).Code, Is.EqualTo((long)ServerException.Codes.CommunicationFailure), "Actual: " + error);
			}

			DataphorServiceHostHooks.RevertHooksToDefault();
			Assert.That(DataphorServiceHostHooks.HasCustomBinding, Is.False, "Reverting the hooks left the binding reported as a site's own.");
			using (HostedInstance host = new HostedInstance("HostBindingHookDefault", false, 0))
				Assert.DoesNotThrow(() => Establish(host), "The same client cannot reach a server on the product's own binding, so the case above proves nothing.");
		}

		/// <summary>
		/// Both hosts in the process reach the builder hook - the instance's and the listener's - and a hook that throws
		/// fails the start rather than leaving a server up that a site believed it had configured. The count is the
		/// subject because there is no behavioural way to watch a hook that only installs service credentials, that
		/// needing a certificate; an extension point nothing calls is the defect this hook exists to fix.
		/// </summary>
		[Test]
		public void TestBothHostsReachTheServiceBuilderHook()
		{
			int calls = 0;
			DataphorServiceHostHooks.ServiceBuilderHook = builder => calls++;
			using (HostedInstance host = new HostedInstance("HostBindingHookBuilder", true, 0))
			{
				Assert.That(host.Host.IsListening, Is.True, "The listener did not start, so only one host was built and the count below is not the two this case is about.");
				Assert.That(calls, Is.EqualTo(2), "The builder hook was not called once for the instance's host and once for the listener's.");
			}

			DataphorServiceHostHooks.ServiceBuilderHook = builder => throw new InvalidOperationException(ThrowingHookMessage);
			Exception error = StartAndCatch("HostBindingHookBuilderThrows", false);
			Assert.That(error.ToString(), Does.Contain(ThrowingHookMessage), "A builder hook that threw did not reach the caller. Actual: " + error);
		}

		/// <summary>
		/// The listener's host builds its binding before the try that swallows a failure to start, so a misconfigured
		/// binding hook is reported as itself rather than as a listener port already in use. The instance's host is built
		/// first, so a hook that works once and then throws is what lands the failure on the listener's host.
		/// </summary>
		[Test]
		public void TestTheListenerHostsBindingHookFailureIsNotSwallowed()
		{
			int calls = 0;
			DataphorServiceHostHooks.BuildBindingCall =
				() =>
				{
					calls++;
					if (calls > 1)
						throw new InvalidOperationException(ThrowingHookMessage);
					return DataphorServiceHostBinding.Build();
				};

			Exception error = StartAndCatch("HostBindingHookListenerThrows", true);
			Assert.That(calls, Is.EqualTo(2), "The listener's host did not build a binding of its own from the hook.");
			Assert.That(error.ToString(), Does.Contain(ThrowingHookMessage), "The listener's binding hook failure was swallowed as a listener that could not start. Actual: " + error);
		}

		/// <summary>
		/// A hook that returns no binding is refused by name. CoreWCF would otherwise fail while an endpoint was being
		/// added, with nothing in the message to say that a hook produced the null.
		/// </summary>
		/// <remarks>
		/// Through the listener's host, which is a host on its own with no engine server behind it: the guard is the one
		/// both hosts read, and reaching it this way costs a reserved port rather than a server start. Nothing ends up
		/// listening - the refusal comes before the host is built - and the using is there for the regression where it
		/// does not.
		/// </remarks>
		[Test]
		public void TestANullBindingIsRefusedByName()
		{
			DataphorServiceHostHooks.BuildBindingCall = () => null;
			int port = HostedInstance.ReserveLoopbackPorts(1)[0];
			Exception error = Assert.Catch(() => { using (new ListenerServiceHost(HostedInstance.LoopbackAddress, port)) { } });
			Assert.That(error.ToString(), Does.Contain("DataphorServiceHostHooks.BuildBindingCall"), "The failure does not name the hook that caused it. Actual: " + error);
		}

		/// <summary>
		/// What the builder hook is for: a site reaches each host's service host through it, which is where CoreWCF
		/// carries the service credentials a server certificate is installed into. Run rather than read off CoreWCF's
		/// metadata, because the hook is called from inside the application-pipeline callback, after the host's
		/// dependency-injection container is built - a configuration route that had to register a service would be too
		/// late there, and the extension point would not do the one thing it exists for.
		/// </summary>
		/// <remarks>
		/// No certificate is installed, the suite having none, and none is needed: the question is whether the delegate
		/// reaches a service host with credentials there to be set. On the listener's host, which carries no engine
		/// server; both hosts call the same hook, which TestBothHostsReachTheServiceBuilderHook establishes.
		/// </remarks>
		[Test]
		public void TestTheBuilderHookReachesEachHostsServiceCredentials()
		{
			bool configured = false;
			bool credentialsReachable = false;
			DataphorServiceHostHooks.ServiceBuilderHook =
				builder =>
					builder.ConfigureAllServiceHostBase
					(
						serviceHost =>
						{
							configured = true;
							credentialsReachable = serviceHost.Credentials != null;
						}
					);

			using (ListenerServiceHost listener = new ListenerServiceHost(HostedInstance.LoopbackAddress, HostedInstance.ReserveLoopbackPorts(1)[0]))
			{
				// IsStarted rather than an assertion inside the delegate: this class swallows what starting its host
				// threw, so a delegate that failed there would otherwise read as a listener port already in use.
				Assert.That(listener.IsStarted, Is.True, "The listener's host did not start. Reported: " + listener.StartFailureMessage);
				Assert.That(configured, Is.True, "The configuration delegate the builder hook installed never reached a service host.");
				Assert.That(credentialsReachable, Is.True, "The service host reached the delegate with no credentials to install.");
			}
		}

		/// <summary>The host binding with the text message encoder in place of the binary one: a wire no shipped client speaks.</summary>
		private static CoreWCF.Channels.Binding TextEncodedBinding()
		{
			return
				new CoreWCF.Channels.CustomBinding
				(
					new CoreWCF.Channels.TextMessageEncodingBindingElement(),
					new CoreWCF.Channels.TcpTransportBindingElement()
				);
		}

		/// <summary>Opens a connection on the wire the way a connection alias does, and closes it.</summary>
		/// <remarks>
		/// A bare ClientServer rather than CoreWcfProtocolTest's ProtocolClient: a LocalServer would start a client-side
		/// engine as well, and whether the channel carries a call at all is the whole question here.
		/// </remarks>
		private static void Establish(HostedInstance host)
		{
			ClientServer clientServer =
				new ClientServer(HostedInstance.LoopbackAddress, host.InstanceName, host.PortNumber, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default);
			try
			{
				IRemoteServerConnection connection = clientServer.Establish("HostBindingHook", "localhost");
				clientServer.Relinquish(connection);
			}
			finally
			{
				clientServer.Close();
			}
		}

		/// <summary>Starts a server over a configuration of its own and returns what starting it threw; fails the case if it started.</summary>
		private static Exception StartAndCatch(string instanceName, bool shouldListen)
		{
			string directory = TestEnvironment.NewDirectory(instanceName);
			try
			{
				TestEnvironment.CopyLibraries(Path.Combine(directory, TestServerInstance.LibrariesFolderName), new string[0]);

				int[] ports = HostedInstance.ReserveLoopbackPorts(2);
				DataphorServiceHost host = HostedInstance.NewHost();
				try
				{
					return Assert.Catch(() => host.Start(HostedInstance.Configuration(instanceName, directory, ports[0], shouldListen, ports[1])));
				}
				finally
				{
					// Start's own failure path has already stopped everything it built; this is for the case where it did
					// not fail, which Assert.Catch has then already reported.
					host.Stop();
				}
			}
			finally
			{
				TestEnvironment.TryDeleteDirectory(directory);
			}
		}
	}
}
