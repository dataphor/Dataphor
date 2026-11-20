/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.ExceptionServices;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using CoreWCF.Configuration;

namespace Alphora.Dataphor.DAE.Service
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Contracts;
	using Alphora.Dataphor.DAE.NativeCLI;

	/// <summary>
	/// Starts an instance of the Dataphor server and serves it on the network: the main service and the native CLI
	/// service on the instance's port, and, when the instance should listen, the listener service on the shared
	/// listener port.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The two services of the instance share one .NET Generic Host, built here. The listener has a second host of its
	/// own inside <see cref="ListenerServiceHost"/>, because its port is shared with every other instance on the
	/// machine and a failure to take it must not stop this server; see the remarks on that class.
	/// </para>
	/// <para>
	/// The host is kept inside this class rather than handed in, so that the console host, the Windows service and
	/// DAE.Client's non-embedded in-process alias keep working unchanged. host-generic-host reshapes those next, and
	/// owns routing this server's log and the Windows event log through Microsoft.Extensions.Logging; the logging
	/// providers are cleared here.
	/// </para>
	/// </remarks>
	public class DataphorServiceHost
	{
		/// <summary>How long Kestrel is given to close open connections before it aborts them.</summary>
		/// <remarks>
		/// Kestrel's graceful shutdown waits for open connections, and a client channel may still be open, so stopping
		/// is a bounded wait and then an abort rather than an indefinite one. Before CoreWCF, Stop closed the WCF hosts
		/// asynchronously and did not wait at all.
		/// </remarks>
		internal static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

		private Server _server;
		/// <summary>
		/// Provides in-process access to the hosted server
		/// </summary>
		public IServer Server { get { return _server; } }

		private RemoteServer _remoteServer;
		private DataphorService _service;
		private NativeServer _nativeServer;
		private NativeCLIService _nativeService;
		private IHost _host;
		private ListenerServiceHost _listenerServiceHost;

		public bool IsActive { get { return _server != null; } }

		/// <summary>Whether this host is serving the listener service.</summary>
		/// <remarks>
		/// False when the instance is configured not to listen, and false when its listener port was already taken, which
		/// is the normal case for the second and later server processes on one machine. Start writes the second case to the
		/// server's log; this is how a caller (or a test) reads it without the log.
		/// </remarks>
		public bool IsListening { get { return _listenerServiceHost != null && _listenerServiceHost.IsStarted; } }

		private void CheckInactive()
		{
			if (IsActive)
				throw new ServerException(ServerException.Codes.ServiceActive);
		}

		private string _instanceName;
		public string InstanceName
		{
			get { return _instanceName; }
			set
			{
				CheckInactive();
				_instanceName = value;
			}
		}

		private string _hostName = Environment.MachineName;
		/// <summary>The host name used in both the listening address and the endpoint addresses.</summary>
		/// <remarks>
		/// The machine name by default, which is what 3.1 published, so the addresses a 4.0 server publishes are the
		/// same byte for byte. It is also what gives both address families: for a name CoreWCF opens two listening
		/// sockets on the port, 0.0.0.0 and [::], as the old ServiceHost did, so clients reaching the server by literal
		/// address, by "localhost" and by machine name all connect at once. A literal address binds that address only, and a client
		/// whose name resolves to the other family then pays about two seconds per channel open before the stack falls
		/// back (measured; see "CoreWCF hosting" under WS5 in docs/modernization-plan.md). Set it to listen on one
		/// interface of a multi-homed machine, or on loopback, as the tests do.
		/// </remarks>
		public string HostName
		{
			get { return _hostName; }
			set
			{
				CheckInactive();
				_hostName = value;
			}
		}

		private bool _loggingEnabled = true;
		/// <summary>Whether the hosted server logs; applied to the server before it starts.</summary>
		/// <remarks>
		/// On by default, as a hosted server has always been. A logging server in an elevated process also creates and
		/// writes the Windows event-log source, which a test must not do.
		/// </remarks>
		public bool LoggingEnabled
		{
			get { return _loggingEnabled; }
			set
			{
				CheckInactive();
				_loggingEnabled = value;
			}
		}

		/// <summary>Hosts the instance named by <see cref="InstanceName"/> in this machine's instance list.</summary>
		/// <remarks>Adds a default configuration to the list and saves it when the list has no such instance, as it always has.</remarks>
		public void Start()
		{
			if (!IsActive)
			{
				InstanceManager.Load();

				ServerConfiguration instance = InstanceManager.Instances[_instanceName];
				if (instance == null)
				{
					// Establish a default configuration
					instance = ServerConfiguration.DefaultInstance(_instanceName);
					InstanceManager.Instances.Add(instance);
					InstanceManager.Save();
				}

				Start(instance);
			}
		}

		/// <summary>Hosts the given configuration, without reading or writing this machine's instance list.</summary>
		public void Start(ServerConfiguration instance)
		{
			if (!IsActive)
			{
				try
				{
					_server = new Server();
					instance.ApplyTo(_server);
					_server.LoggingEnabled = _loggingEnabled;
					_remoteServer = new RemoteServer(_server);
					_nativeServer = new NativeServer(_server);
					_server.Start();
					try
					{
						_server.LogMessage("Creating WCF Service...");
						_service = new DataphorService(_remoteServer);

						_server.LogMessage("Creating Native CLI Service...");
						_nativeService = new NativeCLIService(_nativeServer);

						_server.LogMessage("Configuring Service Host...");
						_host = BuildHost(instance);

						_server.LogMessage("Opening Service Host...");
						_host.Start();

						// Start the listener
						if (instance.ShouldListen)
						{
							_server.LogMessage("Starting Listener Service...");
							_listenerServiceHost = new ListenerServiceHost(_hostName, instance.OverrideListenerPortNumber, instance.RequireSecureListenerConnection);
							if (!_listenerServiceHost.IsStarted)
								_server.LogMessage
								(
									LogEntryType.Warning,
									String.Format
									(
										"The listener service is not running: its port is already in use, usually by the listener of another instance on this machine. This machine's instances are then discoverable through that listener rather than through this server. The error reported was: {0}",
										_listenerServiceHost.StartFailureMessage
									)
								);
						}
					}
					catch (Exception exception)
					{
						_server.LogError(exception);
						throw;
					}
				}
				catch
				{
					Stop();
					throw;
				}
			}
		}

		/// <summary>
		/// The instance's own two endpoints on one host: the main service, and the native CLI service at a path below
		/// it. Both addresses are the ones DataphorServiceUtility builds, which is the compatibility surface.
		/// </summary>
		private IHost BuildHost(ServerConfiguration instance)
		{
			CoreWCF.Channels.Binding binding = DataphorServiceHostBinding.Build();
			DataphorService service = _service;
			NativeCLIService nativeService = _nativeService;

			// The .NET Generic Host, not WebHostBuilder: .NET 10 marks WebHostBuilder and IWebHost obsolete
			// (ASPDEPR004, ASPDEPR008). CoreWCF's NetTcp listener is a Kestrel connection handler and configures the
			// same way under either, and this is the host host-generic-host builds on.
			return
				new HostBuilder()
					.ConfigureLogging(logging => logging.ClearProviders())
					.ConfigureWebHost
					(
						webHostBuilder =>
							webHostBuilder
								.UseKestrel()
								.UseContentRoot(AppContext.BaseDirectory)
								// Without this Kestrel also opens its own default HTTP endpoint; this process listens on nothing but net.tcp.
								.UseSetting(WebHostDefaults.ServerUrlsKey, String.Empty)
								.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "true")
								// One Listen call for the one port both endpoints are on; CoreWCF dispatches between them by path.
								.UseNetTcp(options => options.Listen(ListenAddress(_hostName, instance.PortNumber)))
								.ConfigureServices
								(
									services =>
									{
										services.AddServiceModelServices();

										// How "new ServiceHost(_service)" is spelled on CoreWCF: with InstanceContextMode.Single on
										// the service class, CoreWCF resolves the service type from dependency injection and makes
										// that object the well-known singleton, so every call reaches the instances built above.
										services.AddSingleton(service);
										services.AddSingleton(nativeService);
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
												builder.AddServiceEndpoint<DataphorService, IDataphorService>(binding, new Uri(DataphorServiceUtility.BuildInstanceURI(_hostName, instance.PortNumber, instance.Name)));
												builder.AddService<NativeCLIService>();
												builder.AddServiceEndpoint<NativeCLIService, INativeCLIService>(binding, new Uri(DataphorServiceUtility.BuildNativeInstanceURI(_hostName, instance.PortNumber, instance.Name)));
											}
										)
								)
					)
					.Build();
		}

		/// <summary>The address CoreWCF listens on, which carries no path: the endpoint addresses supply those.</summary>
		internal static Uri ListenAddress(string hostName, int portNumber)
		{
			return new Uri(String.Format("{0}://{1}:{2}/", Uri.UriSchemeNetTcp, hostName, portNumber));
		}

		/// <summary>
		/// Stops everything Start built, in the reverse order, and tolerates a half-built state: Start's own failure
		/// path calls this, so a stage may find a host that was built but never started, an engine server that was
		/// created but never started, or a null service.
		/// </summary>
		public void Stop()
		{
			// A failure to stop either host is captured rather than thrown from here, so that it cannot leave the engine
			// server running; the first one is rethrown once everything else is stopped.
			Exception hostError = null;

			ListenerServiceHost listenerServiceHost = _listenerServiceHost;
			_listenerServiceHost = null;
			if (listenerServiceHost != null)
			{
				try
				{
					listenerServiceHost.Dispose();
				}
				catch (Exception exception)
				{
					hostError = exception;
				}
			}

			IHost host = _host;
			_host = null;
			if (host != null)
			{
				try
				{
					// The host is stopped before the service is disposed: the other order leaves an in-flight call
					// reaching a disposed server.
					host.StopAsync(StopTimeout).GetAwaiter().GetResult();
				}
				catch (Exception exception)
				{
					if (hostError == null)
						hostError = exception;
				}
				finally
				{
					host.Dispose();
				}
			}

			if (_service != null)
			{
				_service.Dispose();
				_service = null;
			}

			_nativeService = null;
			_nativeServer = null;

			if (_remoteServer != null)
			{
				_remoteServer.Dispose();
				_remoteServer = null;
			}

			if (_server != null)
			{
				_server.Stop();
				_server = null;
			}

			if (hostError != null)
				ExceptionDispatchInfo.Capture(hostError).Throw();
		}
	}
}
