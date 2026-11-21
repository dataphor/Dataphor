/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Alphora.Dataphor.DAE.Service;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A temporary instance directory, free loopback ports, and a <see cref="DataphorServiceHost"/> started over a
	/// configuration of its own: what a test needs to reach a hosted server without touching this machine's state.
	/// </summary>
	/// <remarks>
	/// Here rather than in CoreWcfProtocolTest, which is where it was and whose own size note asked for the move, so that
	/// a second fixture can host a server without a copy of the plumbing. The static members are the same pieces one step
	/// apart, for a case that has to drive the start itself - one that expects it to fail, for instance.
	/// </remarks>
	internal sealed class HostedInstance : IDisposable
	{
		/// <param name="listenerPortNumber">0 to reserve a free port; otherwise the port the listener must use, so that a test can hold it first.</param>
		public HostedInstance(string instanceName, bool shouldListen, int listenerPortNumber)
		{
			_instanceName = instanceName;
			_directory = TestEnvironment.NewDirectory(instanceName);
			try
			{
				if (listenerPortNumber == 0)
				{
					int[] ports = ReserveLoopbackPorts(2);
					_portNumber = ports[0];
					_listenerPortNumber = ports[1];
				}
				else
				{
					_portNumber = ReserveLoopbackPorts(1)[0];
					_listenerPortNumber = listenerPortNumber;
				}

				// No libraries: System is built in, and the server creates General. This is also what makes the
				// library directory, which the server requires.
				TestEnvironment.CopyLibraries(Path.Combine(_directory, TestServerInstance.LibrariesFolderName), new string[0]);

				_host = NewHost();
				_host.Start(Configuration(instanceName, _directory, _portNumber, shouldListen, _listenerPortNumber));
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private readonly string _directory;
		/// <summary>The instance's own temporary root: its library copy, its instance folder and so its catalog store are all under it.</summary>
		public string RootDirectory => _directory;

		private readonly string _instanceName;
		public string InstanceName => _instanceName;

		private readonly int _portNumber;
		/// <summary>The instance's port; never 8061, and never a port a real instance could own.</summary>
		public int PortNumber => _portNumber;

		private readonly int _listenerPortNumber;
		/// <summary>The port this instance's listener was configured for; never the default 8060.</summary>
		public int ListenerPortNumber => _listenerPortNumber;

		private DataphorServiceHost _host;
		public DataphorServiceHost Host => _host;

		public void Dispose()
		{
			try
			{
				if (_host != null)
				{
					_host.Stop();
					_host = null;
				}
			}
			finally
			{
				TestEnvironment.TryDeleteDirectory(_directory);
			}
		}

		/// <summary>
		/// The host name the hosted instances listen on and the clients connect to.
		/// </summary>
		/// <remarks>
		/// Loopback by literal, so a test never opens a port another machine can reach. The product default is
		/// Environment.MachineName, which is what makes CoreWCF bind both address families; that is established by
		/// measurement and recorded under "CoreWCF hosting" in docs/modernization-plan.md, not re-measured here.
		/// </remarks>
		internal const string LoopbackAddress = "127.0.0.1";

		/// <summary>A configuration for a server of this suite's own, with its catalog, libraries and instance directory under <paramref name="directory"/>.</summary>
		internal static ServerConfiguration Configuration(string instanceName, string directory, int portNumber, bool shouldListen, int listenerPortNumber)
		{
			return
				new ServerConfiguration
				{
					Name = instanceName,
					PortNumber = portNumber,
					ShouldListen = shouldListen,
					OverrideListenerPortNumber = listenerPortNumber,
					LibraryDirectories = Path.Combine(directory, TestServerInstance.LibrariesFolderName),
					InstanceDirectory = Path.Combine(directory, TestServerInstance.InstanceFolderName),
					CatalogStoreClassName = TestServerInstance.CatalogStoreClassName,
					CatalogStoreConnectionString = TestServerInstance.CatalogStoreConnectionString
				};
		}

		/// <summary>A host listening on loopback, with logging off.</summary>
		/// <remarks>
		/// Logging is off as everywhere in this suite: a logging server in an elevated process also creates and writes the
		/// Windows event-log source.
		/// </remarks>
		internal static DataphorServiceHost NewHost()
		{
			DataphorServiceHost host = new DataphorServiceHost();
			host.HostName = LoopbackAddress;
			host.LoggingEnabled = TestEnvironment.LoggingEnabled;
			return host;
		}

		/// <summary>Reserves the given number of distinct free loopback ports by binding them all and then releasing them.</summary>
		/// <remarks>
		/// A configured port of 0 cannot be used instead: the endpoint address is what CoreWCF builds its address table
		/// from, and that happens before Kestrel binds the socket, so the real port is not known in time. The port is
		/// also part of the address the client is given.
		/// </remarks>
		// NOTE: another process could take one of these ports between the release here and CoreWCF's bind. Nothing on a
		// test machine hands out ephemeral ports in that window, and the alternative - a fixed port - would collide with a
		// real instance on the build machine.
		internal static int[] ReserveLoopbackPorts(int count)
		{
			TcpListener[] probes = new TcpListener[count];
			int[] result = new int[count];
			try
			{
				for (int index = 0; index < count; index++)
				{
					probes[index] = new TcpListener(IPAddress.Parse(LoopbackAddress), 0);
					probes[index].Start();
					result[index] = ((IPEndPoint)probes[index].LocalEndpoint).Port;
				}
			}
			finally
			{
				foreach (TcpListener probe in probes)
					if (probe != null)
						probe.Stop();
			}
			return result;
		}

		/// <summary>Every port this machine has a listening TCP socket on, one entry per socket.</summary>
		internal static List<int> ListeningPorts()
		{
			List<int> result = new List<int>();
			foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
				result.Add(endPoint.Port);
			return result;
		}

		internal static bool IsListeningOn(int portNumber)
		{
			return ListeningPorts().Contains(portNumber);
		}
	}
}
