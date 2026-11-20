/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Listener;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The WS5 CoreWCF proof: the client code the product ships completes real work against a .NET 10 server hosted by
	/// CoreWCF on the existing binding, with the wire unchanged. Each case is named for the property it establishes.
	/// </summary>
	/// <remarks>
	/// Nothing here asserts anything about the machine: the ports are ephemeral (never 8060 or 8061) and the listener
	/// case asserts only that its contract answers, because ListenerService reads the machine-wide instance
	/// configuration.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: two servers in one process share the engine's static state (see the note on TestServerInstance).
	public class CoreWcfProtocolTest
	{
		/// <summary>The instance name, which is the first path segment of the service address on both sides.</summary>
		private const string InstanceName = "CoreWcfProof";

		/// <summary>Clears the 64 KB message limit, the 8 KB string-content quota and the 16 KB array quota at once.</summary>
		private const int LargeValueLength = 3 * 1024 * 1024;

		private TestServerInstance _instance;
		private CoreWcfProofHost _host;

		[OneTimeSetUp]
		public void SetUpFixture()
		{
			_instance = new TestServerInstance(InstanceName);
			try
			{
				_instance.Server.Start();
				_host = new CoreWcfProofHost(_instance.Server, InstanceName);
			}
			catch
			{
				TearDownFixture();
				throw;
			}
		}

		/// <summary>Stops the host before the instance, and runs on the failure path too, or the instance directory stays locked.</summary>
		[OneTimeTearDown]
		public void TearDownFixture()
		{
			try
			{
				if (_host != null)
				{
					_host.Dispose();
					_host = null;
				}
			}
			finally
			{
				if (_instance != null)
				{
					_instance.Dispose();
					_instance = null;
				}
			}
		}

		/// <summary>
		/// The shipped client stack, assembled the way src/client/DAE.Client/ServerConnection.cs assembles it for a
		/// connection alias: a ClientServer on the binding DataphorServiceUtility.GetBinding builds, wrapped in a
		/// LocalServer. No channel is hand-rolled and no hook on DataphorServiceUtility is touched.
		/// </summary>
		private sealed class ProtocolClient : IDisposable
		{
			public ProtocolClient(int portNumber)
			{
				// The loopback literal, not "localhost": this host listens on IPv4 only, and on Windows "localhost"
				// resolves to ::1 first, where a connect costs about two seconds before the stack falls back to
				// 127.0.0.1 (measured). That is why a host serving real clients has to listen on both families, which is
				// handed to protocol-corewcf-host; here the literal is also what keeps the fixture at about a second.
				_clientServer = new ClientServer(CoreWcfProofHost.LoopbackAddress, InstanceName, portNumber, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default);
				try
				{
					// LocalServer starts an Engine of its own to hold the client-side catalog cache, and Engine.InitializeCatalog
					// resets the process-wide Schema.Object object-id counter to zero (bug-object-id-counter-is-process-wide).
					// With the server in this same process that hands the server ids its catalog already holds, and the next
					// create statement fails on a unique-constraint violation in the catalog store. Handing the counter back
					// afterwards leaves both engines allocating increasing ids. Out of process - how the product runs - the two
					// counters are separate and none of this arises.
					int nextObjectID = Schema.Object.GetNextObjectID();
					// The third argument is the client's own host name, which the server records against the connection; it
					// is not a connection target.
					_localServer = new LocalServer(_clientServer, false, "localhost");
					Schema.Object.SetNextObjectID(nextObjectID);
				}
				catch
				{
					_clientServer.Close();
					throw;
				}
			}

			private readonly ClientServer _clientServer;
			private LocalServer _localServer;

			/// <summary>The client-side IServer: every call on it is a call on the wire.</summary>
			public IServer Server => _localServer;

			/// <summary>The handle the service minted for this connection, which is per service instance.</summary>
			public int ConnectionHandle => ((ClientConnection)_localServer.ServerConnection).ConnectionHandle;

			public void Dispose()
			{
				try
				{
					if (_localServer != null)
					{
						_localServer.Dispose();
						_localServer = null;
					}
				}
				finally
				{
					_clientServer.Close();
				}
			}
		}

		/// <summary>Runs <paramref name="body"/> on a session and process over the wire, cleaning both up on either path.</summary>
		private void OnWire(Action<IServerProcess> body)
		{
			using (ProtocolClient client = new ProtocolClient(_host.PortNumber))
			{
				IServerSession session = client.Server.Connect(new SessionInfo("Admin", ""));
				try
				{
					IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
					try
					{
						body(process);
					}
					finally
					{
						session.StopProcess(process);
					}
				}
				finally
				{
					client.Server.Disconnect(session);
				}
			}
		}

		/// <summary>
		/// A whole session over the wire: connect, prepare and execute a statement, fetch a cursor, use the process as a
		/// stream manager, and shut down cleanly. The D4 stays on built-in operators and types over the memory device, so
		/// that the client-side engine never misses a class alias and sends LocalServer down the library-download path.
		/// </summary>
		[Test]
		public void TestWholeSessionOverTheWire()
		{
			OnWire
			(
				process =>
				{
					process.Execute("create table WireSession { ID : Integer, Name : String, key { ID } };", null);

					IServerStatementPlan plan = process.PrepareStatement("insert row { 1 ID, 'one' Name } into WireSession;", null);
					try
					{
						plan.Execute(null);
					}
					finally
					{
						process.UnprepareStatement(plan);
					}

					process.Execute("insert row { 2 ID, 'two' Name } into WireSession;", null);

					List<string> names = new List<string>();
					IServerCursor cursor = process.OpenCursor("select WireSession order by { ID }", null);
					try
					{
						while (cursor.Next())
							using (IRow row = cursor.Select())
								names.Add((string)row["Name"]);
					}
					finally
					{
						process.CloseCursor(cursor);
					}

					Assert.That(names, Is.EqualTo(new[] { "one", "two" }));

					// The stream handles are the process's: Allocate, the length probe the first write makes, the close and
					// Deallocate are all calls on the wire. The bytes themselves are held by the client-side deferred-write
					// cache, which is what the shipped client does; nothing here forces a flush, because no public client API
					// does.
					byte[] written = Encoding.UTF8.GetBytes("a stream over the wire");
					StreamID streamID = process.Allocate();
					try
					{
						using (Stream stream = process.Open(streamID, LockMode.Exclusive))
							stream.Write(written, 0, written.Length);

						byte[] read = new byte[written.Length];
						using (Stream stream = process.Open(streamID, LockMode.Exclusive))
							Assert.That(stream.Read(read, 0, read.Length), Is.EqualTo(written.Length));

						Assert.That(read, Is.EqualTo(written));
					}
					finally
					{
						process.Deallocate(streamID);
					}
				}
			);
		}

		/// <summary>
		/// A message far above every relevant default in both directions: a few megabytes in as a statement parameter and
		/// out again through a cursor fetch. A parameter rather than a D4 literal, so the case exercises the transport and
		/// not the lexer.
		/// </summary>
		/// <remarks>
		/// The quotas are already in play before this runs: LocalServer's catalog fetch at connect time crosses 64 KB.
		/// </remarks>
		[Test]
		public void TestLargeMessageInBothDirections()
		{
			OnWire
			(
				process =>
				{
					string sent = new String('d', LargeValueLength);

					process.Execute("create table LargeValue { ID : Integer, Value : String, key { ID } };", null);

					DataParams paramsValue = new DataParams();
					paramsValue.Add(DataParam.Create(process, "AValue", sent));
					process.Execute("insert row { 1 ID, AValue Value } into LargeValue;", paramsValue);

					string returned = null;
					IServerCursor cursor = process.OpenCursor("select LargeValue", null);
					try
					{
						Assert.That(cursor.Next(), Is.True);
						using (IRow row = cursor.Select())
							returned = (string)row["Value"];
					}
					finally
					{
						process.CloseCursor(cursor);
					}

					Assert.That(returned, Is.EqualTo(sent));
				}
			);
		}

		/// <summary>
		/// A server-side engine error reaches the caller as the engine's own exception with its own error code, which can
		/// only happen if FaultException&lt;DataphorFault&gt; arrived typed and DataphorFaultUtility.FaultToException ran.
		/// This is the assertion that the CoreWCF fault type matters: a System.ServiceModel fault thrown by the service
		/// would be serialized as an untyped unhandled-error fault, ClientServer's catch of CommunicationException would
		/// turn it into ServerException.Codes.CommunicationFailure, and every engine error would look like a dropped
		/// connection.
		/// </summary>
		[Test]
		public void TestServerErrorArrivesAsTypedFault()
		{
			OnWire
			(
				process =>
				{
					Exception error = Assert.Catch(() => process.Execute("select NoSuchTableAnywhereInTheCatalog;", null));

					Assert.That
					(
						error,
						Is.InstanceOf<CompilerException>(),
						String.Format
						(
							"The engine's compiler error did not survive as itself; a ServerException with code {0} (CommunicationFailure) is the shape an unrecognised fault takes. Actual: {1}: {2}",
							(int)ServerException.Codes.CommunicationFailure,
							error.GetType().FullName,
							error.Message
						)
					);
					Assert.That(((CompilerException)error).Code, Is.EqualTo((int)CompilerException.Codes.UnknownIdentifier));
					Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.FaultException>(), "An untyped fault escaped to the caller.");
					Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.CommunicationException>(), "A bare communication error escaped to the caller.");
				}
			);
		}

		/// <summary>
		/// The listener contract answers on its own port, through the shipped ListenerFactory, and its typed ListenerFault
		/// comes back as the exception the client builds from it. Nothing is asserted about which instances are listed:
		/// ListenerService reads the machine-wide ServerConfigurations.config, so that list is whatever is installed here.
		/// </summary>
		[Test]
		public void TestListenerAnswersOnItsOwnPort()
		{
			Assert.That(ListenerFactory.EnumerateInstances(CoreWcfProofHost.LoopbackAddress, _host.ListenerPortNumber), Is.Not.Null);

			// An instance name that is certainly not configured: the service turns the lookup failure into a
			// FaultException<ListenerFault>, and ListenerClient turns that back into a DataphorException.
			string unconfigured = "NoSuchInstance" + Guid.NewGuid().ToString("N");
			Exception error = Assert.Catch(() => ListenerFactory.GetInstanceURI(CoreWcfProofHost.LoopbackAddress, _host.ListenerPortNumber, unconfigured));

			// A plain Exception carrying the service's message is what ListenerFaultUtility.FaultToException builds - the
			// listener fault has no exception-class name to rebuild from, unlike DataphorFault. What matters is that it is
			// not a FaultException: an unrecognised fault would have escaped ListenerClient's catch as one.
			Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.FaultException>(), "An untyped fault escaped to the caller: " + error.GetType().FullName + ": " + error.Message);
			Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.CommunicationException>(), "A bare communication error escaped to the caller: " + error.Message);
			Assert.That(error.Message, Does.Contain(unconfigured));
		}

		/// <summary>
		/// One service object serves every connection, as InstanceContextMode.Single says. Without CoreWCF's own
		/// ServiceBehavior attribute the instancing would silently fall back to PerSession: each connection would get its
		/// own DataphorService, and so its own HandleManager, and a handle minted on one connection would be unknown on the
		/// next. Two connections whose handles come from one counter is the observable form of that.
		/// </summary>
		[Test]
		public void TestOneServiceInstanceServesEveryConnection()
		{
			using (ProtocolClient first = new ProtocolClient(_host.PortNumber))
			using (ProtocolClient second = new ProtocolClient(_host.PortNumber))
			{
				Assert.That(second.ConnectionHandle, Is.Not.EqualTo(first.ConnectionHandle), "Two connections were given the same handle, so each was served by a HandleManager of its own.");

				// And the object answering is the one the host built over this fixture's engine server: the name can come
				// from nowhere else, and a DataphorService CoreWCF constructed for itself could not be built at all, the
				// configuration-reading constructor having been removed.
				Assert.That(first.Server.Name, Is.EqualTo(InstanceName));
				Assert.That(second.Server.Name, Is.EqualTo(InstanceName));
			}
		}

		/// <summary>CoreWCF bound the ports the host asked for, so neither service is reachable on 8060 or 8061.</summary>
		[Test]
		public void TestListensOnlyOnTheReservedPorts()
		{
			Assert.That(_host.ServiceEndPoint, Is.Not.Null);
			Assert.That(_host.ServiceEndPoint.Port, Is.EqualTo(_host.PortNumber));
			Assert.That(_host.ListenerEndPoint, Is.Not.Null);
			Assert.That(_host.ListenerEndPoint.Port, Is.EqualTo(_host.ListenerPortNumber));
			foreach (int portNumber in new[] { _host.PortNumber, _host.ListenerPortNumber })
			{
				Assert.That(portNumber, Is.Not.EqualTo(DataphorServiceUtility.DefaultListenerPortNumber));
				Assert.That(portNumber, Is.Not.EqualTo(ServerConfiguration.DefaultPortNumber));
			}
		}

		/// <summary>
		/// The host's binding and the client's say the same thing. They are written twice, against the unrelated Binding
		/// hierarchies of CoreWCF.Channels and System.ServiceModel.Channels, so nothing but this comparison stops an edit
		/// to one of them from silently changing the protocol on one side only - which the whole proof rests on not
		/// happening. Compared: the element list and its order, the four size limits, the two timeouts, the transfer mode
		/// and the message version. Settings neither declaration names are left out, because the two stacks are entitled
		/// to default them differently.
		/// </summary>
		[Test]
		public void TestHostBindingMatchesTheClientBinding()
		{
			System.ServiceModel.Channels.Binding clientBinding = DataphorServiceUtility.GetBinding();
			CoreWCF.Channels.Binding hostBinding = CoreWcfProofHost.BuildBinding();

			System.ServiceModel.Channels.BindingElementCollection clientElements = clientBinding.CreateBindingElements();
			CoreWCF.Channels.BindingElementCollection hostElements = hostBinding.CreateBindingElements();
			Assert.That(ElementTypeNames(hostElements), Is.EqualTo(ElementTypeNames(clientElements)), "The two binding declarations list different elements, or list them in a different order.");

			System.ServiceModel.Channels.BinaryMessageEncodingBindingElement clientEncoding = clientElements.Find<System.ServiceModel.Channels.BinaryMessageEncodingBindingElement>();
			CoreWCF.Channels.BinaryMessageEncodingBindingElement hostEncoding = hostElements.Find<CoreWCF.Channels.BinaryMessageEncodingBindingElement>();
			Assert.That(hostEncoding.MessageVersion.ToString(), Is.EqualTo(clientEncoding.MessageVersion.ToString()));
			Assert.That(hostEncoding.ReaderQuotas.MaxArrayLength, Is.EqualTo(clientEncoding.ReaderQuotas.MaxArrayLength));
			Assert.That(hostEncoding.ReaderQuotas.MaxStringContentLength, Is.EqualTo(clientEncoding.ReaderQuotas.MaxStringContentLength));

			System.ServiceModel.Channels.TcpTransportBindingElement clientTransport = clientElements.Find<System.ServiceModel.Channels.TcpTransportBindingElement>();
			CoreWCF.Channels.TcpTransportBindingElement hostTransport = hostElements.Find<CoreWCF.Channels.TcpTransportBindingElement>();
			Assert.That(hostTransport.MaxBufferSize, Is.EqualTo(clientTransport.MaxBufferSize));
			Assert.That(hostTransport.MaxReceivedMessageSize, Is.EqualTo(clientTransport.MaxReceivedMessageSize));
			Assert.That(hostTransport.TransferMode.ToString(), Is.EqualTo(clientTransport.TransferMode.ToString()));

			Assert.That(hostBinding.SendTimeout, Is.EqualTo(clientBinding.SendTimeout));
			Assert.That(hostBinding.ReceiveTimeout, Is.EqualTo(clientBinding.ReceiveTimeout));
		}

		/// <summary>The binding elements' simple type names in order, which is all the two element collections have in common.</summary>
		private static List<string> ElementTypeNames(System.Collections.IEnumerable elements)
		{
			List<string> result = new List<string>();
			foreach (object element in elements)
				result.Add(element.GetType().Name);
			return result;
		}
	}
}
