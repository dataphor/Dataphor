/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Service;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The host program (<see cref="DataphorHost"/>), which both host executables are one line over: that the .NET
	/// Generic Host it builds brings a server up when it is started and takes it down when it is stopped, that a
	/// failure to start is not swallowed, and that each executable's dependency file names the one assembly the server
	/// loads by simple name.
	/// </summary>
	/// <remarks>
	/// The server here is handed in already started from a configuration of this suite's own, which is what
	/// <see cref="DataphorHost.BuildHost"/>'s factory parameter is for: the program's own factory calls
	/// <see cref="DataphorServiceHost.Start()"/>, which reads this machine's instance list and adds a default instance
	/// to it when the name is not there. No case here reads or writes that list, opens a port another machine can
	/// reach, or needs administrative rights. What the server then does on the wire is CoreWcfProtocolTest's subject,
	/// not this fixture's; here it only has to answer.
	/// </remarks>
	// The program is annotated Windows-only (it registers a Windows service and reads the Windows event log), so the
	// fixture is too, as ServiceUtilityTest already is.
	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	[TestFixture]
	// Not [Parallelizable]: two servers in one process share the engine's static state (see the note on TestServerInstance).
	public class HostProgramTest
	{
		private const string InstanceName = "HostProgram";

		/// <summary>Loopback by literal, so that no case opens a port another machine can reach.</summary>
		private const string LoopbackAddress = "127.0.0.1";

		[Test]
		public void TheBuiltHostStartsAndStopsTheServer()
		{
			string directory = TestEnvironment.NewDirectory(InstanceName);
			try
			{
				// No libraries: System is built in, and the server creates General. This is also what makes the library
				// directory, which the server requires.
				TestEnvironment.CopyLibraries(Path.Combine(directory, TestServerInstance.LibrariesFolderName), new string[0]);
				ServerConfiguration configuration = Configuration(directory, ReserveLoopbackPort());

				DataphorServiceHost serviceHost = null;
				using
				(
					IHost host =
						DataphorHost.BuildHost
						(
							InstanceName,
							() =>
							{
								DataphorServiceHost result = NewServiceHost();
								result.Start(configuration);
								serviceHost = result;
								return result;
							}
						)
				)
				{
					Assert.That(serviceHost, Is.Null, "building the host started the server; nothing should run until the host is started");

					host.StartAsync().GetAwaiter().GetResult();
					try
					{
						Assert.That(serviceHost, Is.Not.Null, "the host started without starting the server");
						Assert.That(serviceHost.IsActive, Is.True, "the host started but the server is not active");
						Assert.That(Evaluate(serviceHost, "1 + 1"), Is.EqualTo(2), "the started server does not answer");
					}
					finally
					{
						host.StopAsync().GetAwaiter().GetResult();
					}

					Assert.That(serviceHost.IsActive, Is.False, "the host stopped but the server is still active");
				}
			}
			finally
			{
				TestEnvironment.TryDeleteDirectory(directory);
			}
		}

		[Test]
		public void AServerThatFailsToStartFailsTheHost()
		{
			// The failure must reach the caller of host.Run, which is how a console session exits 1 and the service
			// control manager learns that the service did not start. Swallowing it would leave a process that runs and
			// serves nothing.
			InvalidOperationException failure = new InvalidOperationException("the server did not start");

			using (IHost host = DataphorHost.BuildHost(InstanceName, () => { throw failure; }))
			{
				Assert.That(Assert.Catch(() => host.StartAsync().GetAwaiter().GetResult()), Is.SameAs(failure));

				// And there is nothing left to stop: the hosted service holds a server only once it is running.
				Assert.DoesNotThrow(() => host.StopAsync().GetAwaiter().GetResult());
			}
		}

		[Test]
		public void TheHostsDependencyFilesNameTheAssembliesTheServerLoadsByName()
		{
			// On .NET 10 an assembly that sits next to the executable but is not named in the executable's .deps.json
			// cannot be loaded by simple name, however it got into the folder: there is no app-local probing to fall
			// back on, and the engine's assembly-resolve hook only matches assemblies that are already loaded. The
			// server loads exactly one assembly this way - the catalog store, which CatalogStore.Initialize resolves
			// from Server.DefaultCatalogStoreClassName with Type.GetType - and a missing entry is invisible at compile
			// time and shows up only when a server starts, as error 115107.
			string[] storeClassName = Server.DefaultCatalogStoreClassName.Split(',');
			Assert.That(storeClassName.Length, Is.EqualTo(2), "DefaultCatalogStoreClassName is no longer <type>,<assembly>; this case needs rewriting");
			string storeAssemblyFileName = storeClassName[1].Trim() + ".dll";

			string binDirectory = Path.Combine(TestEnvironment.LayoutDirectory, "bin");
			foreach (string dependencyFileName in new string[] { "DAEServer.deps.json", "DAEService.deps.json" })
			{
				string dependencyFilePath = Path.Combine(binDirectory, dependencyFileName);
				Assert.That(File.Exists(dependencyFilePath), Is.True, String.Format("the runtime layout has no {0}; the host executables are not in it", dependencyFileName));
				Assert.That
				(
					File.ReadAllText(dependencyFilePath),
					Does.Contain(storeAssemblyFileName),
					String.Format("{0} does not name {1}, so a server started from the layout cannot load its catalog store; give the executable's project a reference to the store project", dependencyFileName, storeAssemblyFileName)
				);
			}
		}

		/// <summary>A configuration whose catalog, libraries and instance directory are all under <paramref name="directory"/>.</summary>
		private static ServerConfiguration Configuration(string directory, int portNumber)
		{
			return
				new ServerConfiguration
				{
					Name = InstanceName,
					PortNumber = portNumber,
					// Nothing here needs discovery, and the listener port is shared with every instance on the machine.
					ShouldListen = false,
					LibraryDirectories = Path.Combine(directory, TestServerInstance.LibrariesFolderName),
					InstanceDirectory = Path.Combine(directory, TestServerInstance.InstanceFolderName),
					CatalogStoreClassName = TestServerInstance.CatalogStoreClassName,
					CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString
				};
		}

		/// <summary>A host listening on loopback, with logging off as everywhere in this suite.</summary>
		/// <remarks>A logging server in an elevated process also creates and writes the Windows event-log source.</remarks>
		private static DataphorServiceHost NewServiceHost()
		{
			DataphorServiceHost serviceHost = new DataphorServiceHost();
			serviceHost.HostName = LoopbackAddress;
			serviceHost.LoggingEnabled = TestEnvironment.LoggingEnabled;
			return serviceHost;
		}

		/// <summary>Reserves a free loopback port by binding it and releasing it.</summary>
		/// <remarks>
		/// A configured port of 0 cannot be used instead: CoreWCF builds its address table from the endpoint address
		/// before Kestrel binds the socket, so the real port is not known in time (see CoreWcfProtocolTest).
		/// </remarks>
		// NOTE: another process could take the port between the release here and CoreWCF's bind. Nothing on a test
		// machine hands out ephemeral ports in that window, and a fixed port would collide with a real instance.
		private static int ReserveLoopbackPort()
		{
			TcpListener probe = new TcpListener(IPAddress.Parse(LoopbackAddress), 0);
			try
			{
				probe.Start();
				return ((IPEndPoint)probe.LocalEndpoint).Port;
			}
			finally
			{
				probe.Stop();
			}
		}

		/// <summary>Evaluates a D4 expression on the hosted server in process, which is all this fixture asks of it.</summary>
		private static object Evaluate(DataphorServiceHost serviceHost, string expression)
		{
			IServer server = serviceHost.Server;
			IServerSession session = server.Connect(new SessionInfo("Admin", ""));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					return ((ServerProcess)process).Evaluate(expression, null).AsNative;
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				server.Disconnect(session);
			}
		}
	}
}
