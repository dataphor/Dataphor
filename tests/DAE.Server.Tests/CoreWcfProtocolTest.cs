/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Listener;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Service;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The WS5 protocol and hosting tests: the client code the product ships completes real work against a .NET 10
	/// server hosted by the real <see cref="DataphorServiceHost"/>, with the wire unchanged. Each case is named for the
	/// property it establishes.
	/// </summary>
	/// <remarks>
	/// The host is driven through Start(ServerConfiguration), so no case reads or writes this machine's instance list,
	/// and the fixture points InstanceManager at a folder of its own so that the listener case - which serves whatever
	/// list its own process resolves - does not resolve, and so create, the machine's folder either. The ports are free
	/// loopback ports (never 8060 or 8061), and the listener case asserts only that its contract answers, because the
	/// list it serves is not this fixture's subject.
	/// </remarks>
	[TestFixture]
	// Not [Parallelizable]: two servers in one process share the engine's static state (see the note on TestServerInstance).
	// NOTE: 754 lines (wc -l, 2026-11-21), still the largest file in this test project; the host plumbing moved out to
	// HostedInstance.cs when protocol-host-binding-hook needed a second hosted fixture, and the client plumbing to
	// ProtocolClient.cs when test-client-server-fixture needed it out of process. What is left is the cases themselves,
	// so a split from here would be by subject - the hosting and listener cases away from the wire cases.
	public class CoreWcfProtocolTest
	{
		/// <summary>The instance name, which is the first path segment of the service address on both sides.</summary>
		private const string InstanceName = "CoreWcfHost";

		/// <summary>Clears the 64 KB message limit, the 8 KB string-content quota and the 16 KB array quota at once.</summary>
		private const int LargeValueLength = 3 * 1024 * 1024;

		private HostedInstance _host;

		/// <summary>The ports that started listening on this machine while the fixture's host was started.</summary>
		/// <remarks>
		/// A diff of the machine's listening sockets, so a port an unrelated process opened in that window is in here too.
		/// TestListensOnlyOnTheReservedPorts therefore asserts what is and is not in the set, never that the set is exactly
		/// the two ports: it flaked that way once, on 2026-11-20 (host-instance-directory), counting a third port numbered
		/// just above the two the fixture had reserved, and passing on three immediate repeats.
		/// </remarks>
		private List<int> _portsOpenedByTheHost;

		/// <summary>The ports Kestrel opens for itself when nothing clears its server-URLs setting.</summary>
		private static readonly int[] KestrelDefaultHttpPortNumbers = new[] { 5000, 5001 };

		/// <summary>The instance folder this fixture points InstanceManager at, so that no case resolves the machine's own.</summary>
		private string _instanceDirectory;

		[OneTimeSetUp]
		public void SetUpFixture()
		{
			// ListenerService.LoadConfiguration serves the instance list its own process resolves, and resolving the
			// machine's folder creates it under %ProgramData%. Redirected so that this fixture touches no machine state at
			// all; the listener case asserts nothing about which instances are listed, so an empty list of its own will do.
			_instanceDirectory = TestEnvironment.NewDirectory(InstanceName + "-Instances");
			InstanceManager.InstanceDirectory = _instanceDirectory;

			List<int> before = HostedInstance.ListeningPorts();
			try
			{
				_host = new HostedInstance(InstanceName, true, 0);
				_portsOpenedByTheHost = Except(HostedInstance.ListeningPorts(), before);
			}
			catch
			{
				TearDownFixture();
				throw;
			}
		}

		/// <summary>Stops the host, and runs on the failure path too, or the instance directory stays locked.</summary>
		[OneTimeTearDown]
		public void TearDownFixture()
		{
			if (_host != null)
			{
				_host.Dispose();
				_host = null;
			}

			// Restored whether or not the host was built: InstanceManager is static, so a redirection left set would be in
			// force for every fixture that runs after this one.
			InstanceManager.InstanceDirectory = null;
			if (_instanceDirectory != null)
			{
				TestEnvironment.TryDeleteDirectory(_instanceDirectory);
				_instanceDirectory = null;
			}
		}

		private static List<int> Except(List<int> ports, List<int> exclude)
		{
			List<int> result = new List<int>();
			foreach (int port in ports)
				if (!exclude.Contains(port) && !result.Contains(port))
					result.Add(port);
			return result;
		}

		/// <summary>Runs <paramref name="body"/> on a session and process over the wire, cleaning both up on either path.</summary>
		private void OnWire(Action<IServerProcess> body)
		{
			OnWire(_host, body);
		}

		private static void OnWire(HostedInstance host, Action<IServerProcess> body)
		{
			using (ProtocolClient client = new ProtocolClient(HostedInstance.LoopbackAddress, host.InstanceName, host.PortNumber))
				client.OnProcess(body);
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
					// cache, which is what the shipped client does; TestStreamContentCrossesTheWire covers the production
					// path that flushes them.
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
		/// An engine error inside one of the three administrative operations - Start, Stop and GetState, the ones whose client
		/// surface is <see cref="ClientServer"/> itself rather than a session - reaches the caller as the server's own
		/// <see cref="DataphorException"/> with its error code and its cause, as it does for the other 85 operations. That
		/// only happens if the client contract declares DataphorFault on the operation: a WCF client deserializes a fault
		/// into FaultException&lt;TDetail&gt; only for a declared detail type, so without the declaration ClientServer's catch
		/// of the typed fault cannot fire, the bare FaultException falls to the communication-failure catch below it, and the
		/// caller gets ServerException.Codes.CommunicationFailure - no engine type to branch on, no code to log, and not the
		/// one sentence that says what is actually wrong (debt-client-contract-missing-fault-declarations).
		/// ContractParityTest holds the two declarations in step but cannot see what the wire does with them, which is why
		/// this case is hosted.
		/// </summary>
		/// <remarks>
		/// A host of its own, because the case stops the engine; a bare ClientServer rather than a ProtocolClient, because
		/// LocalServer would start a client-side engine as well and these operations are on ClientServer. Stop and GetState
		/// carry no logic of their own beyond the declaration this case exercises.
		/// </remarks>
		[Test]
		public void TestAnAdministrativeErrorArrivesAsTypedFault()
		{
			// ShouldListen false: nothing here involves the listener.
			WithASecondHost
			(
				"CoreWcfAdministrativeFault",
				false,
				0,
				host =>
				{
					// Constructing the client builds its channel factory; the channel itself opens on the first call below.
					ClientServer clientServer = new ClientServer(HostedInstance.LoopbackAddress, host.InstanceName, host.PortNumber, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default);
					try
					{
						// Stopping the engine over the wire releases the catalog store's file handles, so that the store can be
						// ruined and the next Start fails in the store with an error of the engine's own.
						clientServer.Stop();
						RuinTheCatalogStore(host.RootDirectory);

						Exception error = Assert.Catch(() => clientServer.Start());

						Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.FaultException>(), "An untyped fault escaped to the caller: the operation declares no fault detail type on the client contract.");
						Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.CommunicationException>(), "A bare communication error escaped to the caller.");
						Assert.That(error, Is.InstanceOf<DataphorException>(), "The server's own exception did not survive. Actual: " + error.GetType().FullName + ": " + error.Message);
						// The code and the cause, not only the type: the fault's reason text carries the server's message either
						// way, and an undeclared fault is a bare FaultException that the catch below the typed one turns into
						// ServerException.Codes.CommunicationFailure - a DataphorException too, so a type-only assertion would
						// pass against exactly the defect this case is for.
						Assert.That
						(
							((DataphorException)error).Code,
							Is.EqualTo((int)Alphora.Dataphor.DAE.Connection.ConnectionException.Codes.SQLException),
							String.Format
							(
								"The store's error code did not survive; {0} (CommunicationFailure) is the shape an unrecognised fault takes. Actual: {1}: {2}",
								(int)ServerException.Codes.CommunicationFailure,
								error.GetType().FullName,
								error.Message
							)
						);
						Assert.That(error.InnerException, Is.Not.Null, "The underlying cause was lost; it is the sentence that says what is wrong with the instance.");
					}
					finally
					{
						clientServer.Close();
					}
				}
			);
		}

		/// <summary>Overwrites the instance's catalog store with text, so that opening it fails in the store.</summary>
		/// <remarks>
		/// Every file whose name starts with the store's own (the SQLite database and its write-ahead log and shared-memory
		/// files), wherever under the instance directory the store put them. Deleting them instead would not do: a missing
		/// SQLite database is created empty and the server would rebuild its catalog and start.
		/// </remarks>
		private static void RuinTheCatalogStore(string directory)
		{
			string[] files = Directory.GetFiles(directory, "DAECatalog*", SearchOption.AllDirectories);
			Assert.That(files, Is.Not.Empty, "No catalog store files were found to ruin, so the case would prove nothing.");
			foreach (string file in files)
				File.WriteAllText(file, "This is not a database.");
		}

		/// <summary>
		/// A transport failure in one of the three administrative operations reaches the caller as a ServerException with
		/// code CommunicationFailure and the transport's own exception as its cause, the way one already does from the
		/// other five members of <see cref="ClientServer"/>. Those three caught the typed fault and nothing else, so a
		/// transport failure in them escaped as the System.ServiceModel exception it was, while the same failure one call
		/// later on the same object arrived translated (debt-client-contract-missing-fault-declarations).
		/// </summary>
		/// <remarks>
		/// The server is stopped and the call made afterwards, so the failure is a closed port rather than a timing
		/// window: connecting to a closed loopback port is refused at once and alike on every machine. Start stands for
		/// all three - each carries one catch of its own, and which of the three runs is not what is in question - and
		/// this is the one case in the suite that reaches the catch at all: every other error here is a fault, which the
		/// typed catch above it takes first.
		/// </remarks>
		[Test]
		public void TestAnAdministrativeOutageArrivesAsACommunicationFailure()
		{
			// ShouldListen false: nothing here involves the listener.
			WithASecondHost
			(
				"CoreWcfAdministrativeOutage",
				false,
				0,
				host =>
				{
					ClientServer clientServer = new ClientServer(HostedInstance.LoopbackAddress, host.InstanceName, host.PortNumber, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default);
					try
					{
						// The whole host, not the engine alone: the port has to stop answering.
						host.Host.Stop();

						Exception error = Assert.Catch(() => clientServer.Start());

						Assert.That(error, Is.InstanceOf<ServerException>(), "The transport failure did not arrive as the exception the rest of this object raises for a transport failure. Actual: " + error.GetType().FullName + ": " + error.Message);
						Assert.That(((ServerException)error).Code, Is.EqualTo((int)ServerException.Codes.CommunicationFailure));
						Assert.That(error.InnerException, Is.InstanceOf<System.ServiceModel.CommunicationException>(), "The transport's own exception was not kept as the cause, so what failed cannot be read off the error.");
					}
					finally
					{
						clientServer.Close();
					}
				}
			);
		}

		/// <summary>
		/// The native CLI answers at its own address, which is a path *below* the main service's on the same port
		/// (.../&lt;instance&gt;/service/native under .../&lt;instance&gt;/service), and the main service still answers on
		/// that port afterwards. Two endpoints on one port, one nested inside the other, is what the CoreWCF proof could
		/// not test and named as the first thing to check; the addresses themselves are the compatibility surface.
		/// </summary>
		[Test]
		public void TestNativeCliAnswersOnItsNestedAddress()
		{
			using (NativeSessionCLIClient client = new NativeSessionCLIClient(HostedInstance.LoopbackAddress, _host.InstanceName, _host.PortNumber, ConnectionSecurityMode.Default))
			{
				NativeSessionHandle session = client.StartSession(new NativeSessionInfo());
				try
				{
					NativeResult result = client.Execute(session, "select 6 * 7", null, NativeExecutionOptions.Default);
					Assert.That(result.Value, Is.InstanceOf<NativeScalarValue>());
					Assert.That(((NativeScalarValue)result.Value).Value, Is.EqualTo(42));
				}
				finally
				{
					client.StopSession(session);
				}
			}

			OnWire(process => Assert.That((long)(int)process.Evaluate("1 + 1", null).AsNative, Is.EqualTo(2L)));
		}

		/// <summary>
		/// A native CLI call that fails in the engine reaches the client as a NativeCLIException carrying the engine's own
		/// error code, not as a communication failure. This is the one behaviour the CoreWCF using-directive in
		/// NativeCLIService.cs exists for: a System.ServiceModel FaultException&lt;NativeCLIFault&gt; thrown from a
		/// CoreWCF service arrives untyped, NativeSessionCLIClient's catch misses it, and every native CLI error becomes a
		/// dropped connection.
		/// </summary>
		[Test]
		public void TestNativeCliErrorArrivesAsTheTypedException()
		{
			using (NativeSessionCLIClient client = new NativeSessionCLIClient(HostedInstance.LoopbackAddress, _host.InstanceName, _host.PortNumber, ConnectionSecurityMode.Default))
			{
				NativeSessionHandle session = client.StartSession(new NativeSessionInfo());
				try
				{
					Exception error = Assert.Catch(() => client.Execute(session, "select NoSuchTableAnywhereInTheCatalog", null, NativeExecutionOptions.Default));

					Assert.That(error, Is.InstanceOf<NativeCLIException>(), "Actual: " + error.GetType().FullName + ": " + error.Message);
					Assert.That(((NativeCLIException)error).Code, Is.EqualTo((int)CompilerException.Codes.UnknownIdentifier));
					Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.FaultException>(), "An untyped fault escaped to the caller.");
					Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.CommunicationException>(), "A bare communication error escaped to the caller.");
				}
				finally
				{
					client.StopSession(session);
				}
			}
		}

		/// <summary>
		/// The content of a stream crosses the wire, which the CoreWCF proof's stream case could not show: it exercised
		/// the handle protocol, but the bytes stayed in the client's deferred-write cache, because nothing in the shipped
		/// client's public surface forces a flush. The production path that does is a cursor insert carrying an overflow
		/// value: LocalCursor.Insert calls LocalProcess.EnsureOverflowReleased, which calls LocalStreamManager.Release,
		/// which flushes the cached stream to the server and closes it.
		/// </summary>
		[Test]
		public void TestStreamContentCrossesTheWire()
		{
			// Longer than any single transport buffer, and not a round number of them.
			byte[] written = new byte[(64 * 1024) + 7];
			new Random(20261120).NextBytes(written);

			OnWire
			(
				process =>
				{
					process.Execute("create table StreamContent { ID : Integer, Content : Binary, key { ID } };", null);

					// An updatable cursor is asked for the way the shipped DataView asks: a cursor definition naming the
					// capabilities, which is what DAEDataSet emits around its expression.
					IServerCursor cursor = process.OpenCursor("cursor(StreamContent capabilities { navigable, updateable })", null);
					try
					{
						using (IRow row = cursor.Plan.RequestRow())
						{
							row["ID"] = 1;

							// How the shipped WinForms controls load a file into a Binary column: a non-native scalar
							// allocated on the process's stream manager, written through, and handed to the row.
							using (Scalar content = new Scalar(process.ValueManager, process.DataTypes.SystemBinary))
							{
								using (Stream stream = content.OpenStream())
									stream.Write(written, 0, written.Length);
								row["Content"] = content;
							}

							cursor.Insert(row);
						}
					}
					finally
					{
						process.CloseCursor(cursor);
					}

					byte[] read;
					IServerCursor reader = process.OpenCursor("select StreamContent", null);
					try
					{
						Assert.That(reader.Next(), Is.True);
						using (IRow row = reader.Select())
						{
							Assert.That(row.HasValue("Content"), Is.True);
							using (Stream stream = row.GetValue("Content").OpenStream())
							{
								MemoryStream buffer = new MemoryStream();
								StreamUtility.CopyStream(stream, buffer);
								read = buffer.ToArray();
							}
						}
					}
					finally
					{
						process.CloseCursor(reader);
					}

					Assert.That(read, Is.EqualTo(written));
				}
			);
		}

		/// <summary>
		/// The listener contract answers on its own port, through the shipped ListenerFactory, and its typed ListenerFault
		/// comes back as the exception the client builds from it. Nothing is asserted about which instances are listed:
		/// ListenerService serves the list its own process resolves, which this fixture has pointed at an empty folder of
		/// its own (see the remarks on the fixture).
		/// </summary>
		[Test]
		public void TestListenerAnswersOnItsOwnPort()
		{
			Assert.That(_host.Host.IsListening, Is.True);
			Assert.That(ListenerFactory.EnumerateInstances(HostedInstance.LoopbackAddress, _host.ListenerPortNumber), Is.Not.Null);

			// An instance name that is certainly not configured: the service turns the lookup failure into a
			// FaultException<ListenerFault>, and ListenerClient turns that back into a DataphorException.
			string unconfigured = "NoSuchInstance" + Guid.NewGuid().ToString("N");
			Exception error = Assert.Catch(() => ListenerFactory.GetInstanceURI(HostedInstance.LoopbackAddress, _host.ListenerPortNumber, unconfigured));

			// A plain Exception carrying the service's message is what ListenerFaultUtility.FaultToException builds - the
			// listener fault has no exception-class name to rebuild from, unlike DataphorFault. What matters is that it is
			// not a FaultException: an unrecognised fault would have escaped ListenerClient's catch as one.
			Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.FaultException>(), "An untyped fault escaped to the caller: " + error.GetType().FullName + ": " + error.Message);
			Assert.That(error, Is.Not.InstanceOf<System.ServiceModel.CommunicationException>(), "A bare communication error escaped to the caller: " + error.Message);
			Assert.That(error.Message, Does.Contain(unconfigured));
		}

		/// <summary>
		/// A server whose listener port is already held by another socket still starts and still serves its own endpoint,
		/// and says so: one listener per machine serves every instance on it, so the second server process on a machine
		/// normally finds the port taken. The failure is swallowed deliberately, and reported through IsListening and one
		/// line in the server's log.
		/// </summary>
		[Test]
		public void TestTheListenerPortBeingTakenDoesNotStopTheServer()
		{
			TcpListener conflict = new TcpListener(IPAddress.Parse(HostedInstance.LoopbackAddress), 0);
			conflict.Start();
			try
			{
				int listenerPortNumber = ((IPEndPoint)conflict.LocalEndpoint).Port;
				WithASecondHost
				(
					"CoreWcfListenerTaken",
					true,
					listenerPortNumber,
					host =>
					{
						Assert.That(host.Host.IsActive, Is.True, "The server did not start because its listener port was taken.");
						Assert.That(host.Host.IsListening, Is.False, "The listener claims to be running on a port another socket holds.");
						OnWire(host, process => Assert.That((long)(int)process.Evaluate("1 + 1", null).AsNative, Is.EqualTo(2L)));
					}
				);
			}
			finally
			{
				conflict.Stop();
			}
		}

		/// <summary>An instance configured not to listen serves its own endpoint and opens no listener port.</summary>
		[Test]
		public void TestShouldListenFalseHostsNoListener()
		{
			WithASecondHost
			(
				"CoreWcfNoListener",
				false,
				0,
				host =>
				{
					Assert.That(host.Host.IsListening, Is.False);
					Assert.That(HostedInstance.IsListeningOn(host.ListenerPortNumber), Is.False, "Something is listening on the listener port of an instance that should not listen.");
					OnWire(host, process => Assert.That((long)(int)process.Evaluate("1 + 1", null).AsNative, Is.EqualTo(2L)));
				}
			);
		}

		/// <summary>
		/// A host whose instance port is already held by another socket does not start, reports the failure to bind as
		/// itself, and leaves nothing running: Start's own failure path calls Stop, which has to stop an engine server
		/// that did start and a Kestrel host that was built and never did. Stopping a second time afterwards does
		/// nothing, which is the other half of the half-built states Stop has to tolerate.
		/// </summary>
		/// <remarks>
		/// Unlike the listener port, the instance's own port not being available has to fail the start: a server nobody
		/// can reach is not a server.
		/// </remarks>
		[Test]
		public void TestTheInstancePortBeingTakenStopsTheServer()
		{
			TcpListener conflict = new TcpListener(IPAddress.Parse(HostedInstance.LoopbackAddress), 0);
			conflict.Start();
			try
			{
				int portNumber = ((IPEndPoint)conflict.LocalEndpoint).Port;
				string instanceName = "CoreWcfInstancePortTaken";
				string directory = TestEnvironment.NewDirectory(instanceName);
				try
				{
					TestEnvironment.CopyLibraries(Path.Combine(directory, TestServerInstance.LibrariesFolderName), new string[0]);

					DataphorServiceHost host = HostedInstance.NewHost();

					// ShouldListen false: this case is about the instance's own port, and no listener port is to be taken.
					Exception error = Assert.Catch(() => host.Start(HostedInstance.Configuration(instanceName, directory, portNumber, false, 0)));

					Assert.That(error, Is.InstanceOf<IOException>(), "The failure to bind did not arrive as itself; a failure from Stop on the way out would mask it. Actual: " + error.GetType().FullName + ": " + error.Message);
					Assert.That(error.Message, Does.Contain(portNumber.ToString()));
					Assert.That(host.IsActive, Is.False, "The engine server was left running by a host that could not open its port.");
					Assert.DoesNotThrow(() => host.Stop(), "Stopping an already stopped host did something.");
				}
				finally
				{
					TestEnvironment.TryDeleteDirectory(directory);
				}
			}
			finally
			{
				conflict.Stop();
			}
		}

		/// <summary>Runs <paramref name="body"/> against a second hosted instance.</summary>
		private static void WithASecondHost(string instanceName, bool shouldListen, int listenerPortNumber, Action<HostedInstance> body)
		{
			using (HostedInstance host = new HostedInstance(instanceName, shouldListen, listenerPortNumber))
				body(host);
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
			using (ProtocolClient first = new ProtocolClient(HostedInstance.LoopbackAddress, _host.InstanceName, _host.PortNumber))
			using (ProtocolClient second = new ProtocolClient(HostedInstance.LoopbackAddress, _host.InstanceName, _host.PortNumber))
			{
				Assert.That(second.ConnectionHandle, Is.Not.EqualTo(first.ConnectionHandle), "Two connections were given the same handle, so each was served by a HandleManager of its own.");

				// And the object answering is the one the host built over its own engine server: the name can come from
				// nowhere else, and a DataphorService CoreWCF constructed for itself could not be built at all, the
				// configuration-reading constructor having been removed.
				Assert.That(first.Server.Name, Is.EqualTo(InstanceName));
				Assert.That(second.Server.Name, Is.EqualTo(InstanceName));
			}
		}

		/// <summary>
		/// The host opened the instance port and the listener port, and no HTTP port of Kestrel's own - the thing the empty
		/// server-URLs setting in NetTcpServiceHost.Build suppresses, and the regression this case exists to catch. Neither
		/// port is 8060 or 8061, so no case here can reach a real instance on the build machine.
		/// </summary>
		[Test]
		public void TestListensOnlyOnTheReservedPorts()
		{
			Assert.That(_portsOpenedByTheHost, Is.SupersetOf(new[] { _host.PortNumber, _host.ListenerPortNumber }));
			foreach (int portNumber in KestrelDefaultHttpPortNumbers)
				Assert.That(_portsOpenedByTheHost, Does.Not.Contain(portNumber), "The host opened a Kestrel default HTTP port; nothing should clear the empty server-URLs setting.");

			foreach (int portNumber in new[] { _host.PortNumber, _host.ListenerPortNumber })
			{
				Assert.That(portNumber, Is.Not.EqualTo(DataphorServiceUtility.DefaultListenerPortNumber));
				Assert.That(portNumber, Is.Not.EqualTo(ServerConfiguration.DefaultPortNumber));
			}
		}

		/// <summary>
		/// The host's binding and the client's say the same thing. They are written twice, against the unrelated Binding
		/// hierarchies of CoreWCF.Channels and System.ServiceModel.Channels, so nothing but this comparison stops an edit
		/// to one of them from silently changing the protocol on one side only. Compared: the element list and its order,
		/// the four size limits, the two timeouts, the transfer mode and the message version. Settings neither declaration
		/// names are left out, because the two stacks are entitled to default them differently.
		/// </summary>
		[Test]
		public void TestHostBindingMatchesTheClientBinding()
		{
			System.ServiceModel.Channels.Binding clientBinding = DataphorServiceUtility.GetBinding();
			CoreWCF.Channels.Binding hostBinding = DataphorServiceHostBinding.Build();

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
