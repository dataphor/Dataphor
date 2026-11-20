/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Net;
using System.Net.Sockets;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using CoreWCF.Configuration;

using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Service;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The smallest CoreWCF host for the Dataphor protocol: the server's own <see cref="DataphorService"/> and
	/// <see cref="ListenerService"/>, on the binding the shipped client builds, each on a loopback port of its own, so
	/// that the client code the product ships can be run against a .NET 10 server (protocol-corewcf-proof).
	/// </summary>
	/// <remarks>
	/// <para>
	/// It is in the test project rather than in src/engine/Server so that CoreWCF.NetTcp and the ASP.NET Core framework
	/// reference stay out of the server assembly's dependency closure, which DAE.Client and through it the WinForms
	/// client and the IDE inherit. Where hosting belongs is protocol-corewcf-host's decision; this class is written to
	/// be moved there rather than rewritten.
	/// </para>
	/// <para>
	/// It takes no secure-connection settings: RequireSecureConnection, RequireSecureListenerConnection and
	/// AllowSilverlightClients belong to protocol-transport-security.
	/// </para>
	/// </remarks>
	public sealed class CoreWcfProofHost : IDisposable
	{
		/// <summary>The address both the listening sockets and the endpoint addresses are built on.</summary>
		/// <remarks>
		/// Loopback by literal, so the proof never opens a port another machine can reach, and so that the client can be
		/// given the same literal: this host listens on IPv4 only, and a client that connects by a name resolving to ::1
		/// first pays a two-second connect failure before the stack falls back. A host serving real clients therefore has
		/// to listen on both families, which protocol-corewcf-host owns.
		/// </remarks>
		public const string LoopbackAddress = "127.0.0.1";

		/// <summary>Hosts the two services over an already started engine server, which stays the caller's to stop.</summary>
		public CoreWcfProofHost(Server server, string instanceName)
		{
			if (server == null)
				throw new ArgumentNullException(nameof(server));
			if (String.IsNullOrEmpty(instanceName))
				throw new ArgumentException("An instance name is required: it is the first path segment of the service address the client builds.", nameof(instanceName));

			_instanceName = instanceName;
			int[] ports = ReserveLoopbackPorts(2);
			_portNumber = ports[0];
			_listenerPortNumber = ports[1];

			try
			{
				_remoteServer = new RemoteServer(server);
				_service = new DataphorService(_remoteServer);

				CoreWCF.Channels.Binding binding = BuildBinding();

				// The .NET Generic Host, not WebHostBuilder: .NET 10 marks WebHostBuilder and IWebHost obsolete
				// (ASPDEPR004, ASPDEPR008). CoreWCF's NetTcp listener is a Kestrel connection handler and configures the
				// same way under either, and this is the host host-generic-host builds on anyway. There is still only one
				// host and one hosting model here.
				_host =
					new HostBuilder()
						.ConfigureLogging(logging => logging.ClearProviders())
						.ConfigureWebHost
						(
							webHostBuilder =>
								webHostBuilder
									.UseKestrel()
									.UseContentRoot(AppContext.BaseDirectory)
									// Without this Kestrel also opens its own default HTTP endpoint; the proof listens on nothing but the two net.tcp ports.
									.UseSetting(WebHostDefaults.ServerUrlsKey, String.Empty)
									.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "true")
									.UseNetTcp
									(
										options =>
										{
											options.Listen(BaseAddress(_portNumber), listenOptions => _serviceListenOptions = listenOptions);
											options.Listen(BaseAddress(_listenerPortNumber), listenOptions => _listenerListenOptions = listenOptions);
										}
									)
									.ConfigureServices
									(
										services =>
										{
											services.AddServiceModelServices();

											// How "new ServiceHost(_service)" is spelled on CoreWCF: with InstanceContextMode.Single on
											// the service class, CoreWCF resolves the service type from dependency injection and makes
											// that object the well-known singleton, so every call reaches the instance built above.
											// ListenerService is deliberately not registered, which leaves CoreWCF to construct it per
											// session exactly as "new ServiceHost(typeof(ListenerService))" does today.
											services.AddSingleton(_service);
										}
									)
									.Configure
									(
										application =>
											application.UseServiceModel
											(
												builder =>
												{
													builder.AddService<DataphorService>();
													builder.AddServiceEndpoint<DataphorService, IDataphorService>(binding, new Uri(DataphorServiceUtility.BuildInstanceURI(LoopbackAddress, _portNumber, _instanceName)));
													builder.AddService<ListenerService>();
													builder.AddServiceEndpoint<ListenerService, IListenerService>(binding, new Uri(DataphorServiceUtility.BuildListenerURI(LoopbackAddress, _listenerPortNumber)));
												}
											)
									)
						)
						.Build();

				_host.Start();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private readonly string _instanceName;
		/// <summary>The instance name in the service address, which the client must be given to build the same address.</summary>
		public string InstanceName => _instanceName;

		private readonly int _portNumber;
		/// <summary>The port the main service listens on; never 8061, and never a port a real instance could own.</summary>
		public int PortNumber => _portNumber;

		private readonly int _listenerPortNumber;
		/// <summary>The port the listener service listens on; never the default 8060.</summary>
		public int ListenerPortNumber => _listenerPortNumber;

		private readonly RemoteServer _remoteServer;

		private readonly DataphorService _service;
		/// <summary>The hosted singleton, so that a test can assert that the object answering calls is this one.</summary>
		public DataphorService Service => _service;

		private readonly IHost _host;

		private CoreWCF.Channels.TcpListenOptions _serviceListenOptions;
		private CoreWCF.Channels.TcpListenOptions _listenerListenOptions;

		/// <summary>The endpoint CoreWCF bound for the main service, so that a test can check it against the port asked for.</summary>
		public IPEndPoint ServiceEndPoint => _serviceListenOptions == null ? null : _serviceListenOptions.IPEndpoint;

		/// <summary>The endpoint CoreWCF bound for the listener service.</summary>
		public IPEndPoint ListenerEndPoint => _listenerListenOptions == null ? null : _listenerListenOptions.IPEndpoint;

		private static Uri BaseAddress(int portNumber)
		{
			return new Uri(String.Format("{0}://{1}:{2}/", Uri.UriSchemeNetTcp, LoopbackAddress, portNumber));
		}

		/// <summary>
		/// The host's half of the one binding the protocol is specified by. The client's half, which is the original, is
		/// DataphorServiceUtility.DefaultBuildBinding; the two stacks have unrelated Binding types, so the settings are
		/// necessarily written twice and this copy has to be changed with that one. Every number here is read from
		/// DataphorServiceUtility so that at least the limits cannot drift.
		/// </summary>
		private static CoreWCF.Channels.Binding BuildBinding()
		{
			CoreWCF.Channels.BinaryMessageEncodingBindingElement messageEncodingElement = new CoreWCF.Channels.BinaryMessageEncodingBindingElement();
			messageEncodingElement.ReaderQuotas.MaxArrayLength = DataphorServiceUtility.MaxMessageLength;
			messageEncodingElement.ReaderQuotas.MaxStringContentLength = DataphorServiceUtility.MaxMessageLength;

			CoreWCF.Channels.TcpTransportBindingElement transportElement = new CoreWCF.Channels.TcpTransportBindingElement();
			transportElement.MaxBufferSize = DataphorServiceUtility.MaxMessageLength;
			transportElement.MaxReceivedMessageSize = DataphorServiceUtility.MaxMessageLength;

			CoreWCF.Channels.CustomBinding binding = new CoreWCF.Channels.CustomBinding(messageEncodingElement, transportElement);
			binding.SendTimeout = TimeSpan.MaxValue;
			binding.ReceiveTimeout = TimeSpan.MaxValue;
			return binding;
		}

		/// <summary>Reserves the given number of distinct free loopback ports by binding them all and then releasing them.</summary>
		/// <remarks>
		/// A base address of port 0 cannot be used instead. The endpoint address is what CoreWCF builds its address table
		/// from, and that happens before Kestrel binds the socket, so the real port is not known in time; and two endpoints
		/// given addresses relative to the base address would both resolve against the first base address of the scheme,
		/// which would put them on one port rather than two.
		/// </remarks>
		// NOTE: another process could take one of these ports between the release here and CoreWCF's bind. Nothing on a
		// test machine hands out ephemeral ports in that window, and the alternative - a fixed port - would collide with a
		// real instance on the build machine.
		private static int[] ReserveLoopbackPorts(int count)
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

		/// <summary>How long Kestrel is given to close open connections before it aborts them.</summary>
		private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

		private bool _disposed;

		/// <summary>
		/// Stops the host before the service and the RemoteServer are disposed: the other order leaves an in-flight call
		/// reaching a disposed server. The engine server itself belongs to the caller.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;

			try
			{
				if (_host != null)
				{
					try
					{
						// Kestrel's graceful shutdown waits for open connections, and a client channel may still be open, so it
						// gets a bounded wait and then aborts them rather than hanging the test run.
						_host.StopAsync(ShutdownTimeout).GetAwaiter().GetResult();
					}
					finally
					{
						_host.Dispose();
					}
				}
			}
			finally
			{
				try
				{
					if (_service != null)
						_service.Dispose();
				}
				finally
				{
					if (_remoteServer != null)
						_remoteServer.Dispose();
				}
			}
		}
	}
}
