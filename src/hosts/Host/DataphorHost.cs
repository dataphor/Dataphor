/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

namespace Alphora.Dataphor.DAE.Service
{
	using Alphora.Dataphor.DAE.Server;

	/// <summary>
	/// The Dataphor Server host program: it registers or removes the Windows service, or runs a server instance, in a
	/// console session or as a Windows service.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Both host executables are one line over this class. DAEServer.exe and DAEService.exe therefore behave
	/// identically and take the same switches; the two names are kept because existing scripts and service
	/// registrations name them (DAEService.exe is the binary a registration carries).
	/// </para>
	/// <para>
	/// Running means running a .NET Generic Host. <c>UseWindowsService</c> is the whole of "the same executable runs as
	/// a Windows service": it replaces the host lifetime only when the process really was started by the service
	/// control manager, so there is one code path and -r is simply an alias for running. A console session is stopped
	/// with Ctrl+C, which the host's console lifetime handles; the old host read a line from standard input, which is
	/// wrong for a program whose input may be redirected or closed (see docs/migration-notes.md).
	/// </para>
	/// </remarks>
	// Windows-only because the program registers a Windows service and reads the Windows event log; the product is
	// Windows-only, and its callers (each executable's Main, and the tests) carry the same annotation.
	[SupportedOSPlatform("windows")]
	public static class DataphorHost
	{
		/// <summary>The shutdown deadline the Generic Host is given; the remarks say what it does and does not bound.</summary>
		/// <remarks>
		/// <para>
		/// Stopping is bounded twice before the engine is reached: the listener's host and the instance's host each get
		/// NetTcpServiceHost's ten seconds for Kestrel to close open connections, so twenty seconds can pass before the
		/// engine's own stop - ending sessions and closing the catalog store - begins. The Generic Host's default of
		/// thirty seconds would leave that ten seconds; sixty leaves forty.
		/// </para>
		/// <para>
		/// What it does when it expires, measured on this version of the Generic Host: it cancels the token the host
		/// passes to <see cref="DataphorHostedService.StopAsync"/>, and for a Windows service it is also how long
		/// WindowsServiceLifetime waits before reporting the service stopped to the service control manager. It does
		/// not abandon a stop in progress - that StopAsync stops the server synchronously and never reads the token, so
		/// a slower stop simply takes longer and the process exits when it finishes. Raising the bound therefore only
		/// buys the engine's stop more room before the service control manager is told the service has stopped. A
		/// constant rather than a setting, as the Kestrel bound already is.
		/// </para>
		/// </remarks>
		public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(60);

		/// <summary>The timestamp shape Server.LogMessage writes, so that the console window and the instance's log file read alike.</summary>
		private const string LogTimestampFormat = "yyyy/MM/dd HH:mm:ss.ffff ";

		/// <summary>Runs the program. Returns 0 for a clean stop and 1 for a failure the program has reported.</summary>
		public static int Run(string[] args)
		{
			HostCommandLine commandLine;
			try
			{
				commandLine = HostCommandLine.Parse(args);
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine(exception.Message);
				return 1;
			}

			switch (commandLine.Command)
			{
				case HostCommand.Install : return Install(commandLine.InstanceName);
				case HostCommand.Uninstall : return Uninstall(commandLine.InstanceName);
				default : return RunServer(commandLine.InstanceName);
			}
		}

		// Install and Uninstall report only the message: the reader is an operator at a prompt, and the service control
		// manager's errors (not elevated, already registered, not registered) say everything a stack trace would.
		private static int Install(string instanceName)
		{
			try
			{
				ServiceUtility.Install(instanceName);
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine(exception.Message);
				return 1;
			}

			Console.WriteLine(Strings.Get("ServiceInstalled", ServiceUtility.GetServiceName(instanceName)));
			return 0;
		}

		private static int Uninstall(string instanceName)
		{
			try
			{
				ServiceUtility.Uninstall(instanceName);
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine(exception.Message);
				return 1;
			}

			Console.WriteLine(Strings.Get("ServiceUninstalled", ServiceUtility.GetServiceName(instanceName)));
			return 0;
		}

		private static int RunServer(string instanceName)
		{
			IHost host;
			try
			{
				host = BuildHost(instanceName, () => StartServer(instanceName));
			}
			catch (Exception exception)
			{
				// Nothing has a logger yet, so this line is the only report there can be.
				Console.Error.WriteLine(ExceptionUtility.BriefDescription(exception));
				return 1;
			}

			using (host)
			{
				try
				{
					host.Run();
				}
				catch (Exception)
				{
					// Already reported: the hosted service logs a failure to start or to stop with the instance name,
					// and a start failure the Generic Host logs again as "Hosting failed to start". Another copy here
					// would only be noise, and the exit code is what a caller reads.
					return 1;
				}
			}

			return 0;
		}

		/// <summary>The program's own server: the named instance of this machine's instance list, started.</summary>
		private static DataphorServiceHost StartServer(string instanceName)
		{
			DataphorServiceHost serviceHost = new DataphorServiceHost();
			serviceHost.InstanceName = instanceName;
			serviceHost.Start();
			return serviceHost;
		}

		/// <summary>Builds the host that owns a running server instance; it is not started.</summary>
		/// <param name="instanceName">The instance, which names the Windows service and appears in the host's log lines.</param>
		/// <param name="startServer">
		/// Produces the running server: it creates a <see cref="DataphorServiceHost"/>, starts it, and returns it for
		/// the hosted service to stop when this host stops. It is called from the hosted service's start, never before,
		/// so building a host starts nothing.
		/// <para>
		/// Starting belongs to the caller because it is also where the server's configuration comes from:
		/// <see cref="DataphorServiceHost.Start()"/> reads this machine's instance list and adds a default instance to
		/// it when the name is not there, which a test must not do, so a test hands in a server started from a
		/// configuration of its own through <see cref="DataphorServiceHost.Start(ServerConfiguration)"/>. That is also
		/// why this method is public: the repository has no InternalsVisibleTo convention to use instead.
		/// </para>
		/// </param>
		// NOTE: accepted tradeoff - host-instance-directory added InstanceManager.InstanceDirectory and
		// DATAPHOR_INSTANCE_DIRECTORY, so a test can now point Start() at an instance list of its own and the machine's list
		// is no longer the reason this parameter exists; it is kept anyway and the plan paragraph that gave that as its
		// reason was corrected instead. Two things the redirection cannot carry: DataphorServiceHost.HostName and
		// LoggingEnabled are properties of the host object rather than settings in a ServerConfiguration, and the fixture
		// needs both (127.0.0.1 so that no case opens a port another machine can reach, logging off so that an elevated run
		// does not create the Windows event-log source); and one case needs a start that throws, which a real server can only
		// be made to do more slowly and less reliably. Revisit if those two properties ever move into the configuration.
		public static IHost BuildHost(string instanceName, Func<DataphorServiceHost> startServer)
		{
			// HostBuilder, not Host.CreateDefaultBuilder: nothing here reads appsettings.json, environment-variable or
			// command-line configuration, and the default builder would add all three plus providers this program does
			// not want. NetTcpServiceHost builds the two CoreWCF hosts the same way.
			return
				new HostBuilder()
					// Outside a Windows service this does nothing at all. Inside one it replaces the host lifetime,
					// sets the content root to the program's own folder (a service's working directory is system32),
					// and adds an event-log logging provider of its own - which the logging below replaces, because
					// that one would write under a new source named after the executable.
					.UseWindowsService(options => options.ServiceName = ServiceUtility.GetServiceName(instanceName))
					.ConfigureLogging(ConfigureLogging)
					.ConfigureServices
					(
						services =>
						{
							services.Configure<HostOptions>(options => options.ShutdownTimeout = ShutdownTimeout);
							services.AddHostedService(provider => new DataphorHostedService(instanceName, startServer, provider.GetRequiredService<ILogger<DataphorHostedService>>()));
						}
					)
					.Build();
		}

		/// <summary>
		/// The host's own logging, which carries host lifecycle only: the instance starting, started, the listener
		/// state, stopping, stopped, and failures.
		/// </summary>
		/// <remarks>
		/// The server's own logging is not routed through this and is not touched: Server.LogMessage writes the
		/// instance's rolling log file in the 3.1 format and, for an elevated process, the Windows event log under the
		/// source the server creates. That log file is a surface operators read and ListLogs serves over the protocol,
		/// so re-implementing it on a logging provider would change a compatibility surface for no gain.
		/// </remarks>
		private static void ConfigureLogging(ILoggingBuilder logging)
		{
			// After UseWindowsService, so that its own event-log provider goes with everything else.
			logging.ClearProviders();
			logging.SetMinimumLevel(LogLevel.Information);

			if (WindowsServiceHelpers.IsWindowsService())
				AddEventLog(logging);
			else
				logging.AddSimpleConsole
				(
					options =>
					{
						options.SingleLine = true;
						options.TimestampFormat = LogTimestampFormat;
					}
				);
		}

		/// <summary>Logs to the event log the server already uses, if its source is there; a host whose event log is unavailable must still start.</summary>
		/// <remarks>
		/// The source is created by the server's own StartLog, which runs elevated in a service, so the first start of a
		/// new installation writes no host lifecycle lines to the event log and later ones do. Adding the provider
		/// regardless would be worse: writing to a source that does not exist creates it, under this program's name
		/// rather than the server's, splitting a deployment's event log entries between two sources.
		/// </remarks>
		private static void AddEventLog(ILoggingBuilder logging)
		{
			try
			{
				if (!EventLog.SourceExists(ServiceUtility.EventLogSource))
					return;
			}
			catch
			{
				// The event log cannot be read at all (policy, or a service account that may not enumerate sources).
				return;
			}

			logging.AddEventLog
			(
				settings =>
				{
					settings.SourceName = ServiceUtility.EventLogSource;
					settings.LogName = Engine.ServerLogName;
				}
			);
		}
	}

	/// <summary>The one hosted service of <see cref="DataphorHost"/>: it owns the running server for the life of the host.</summary>
	/// <remarks>
	/// <see cref="IHostedService"/> rather than BackgroundService: there is no loop to run, only a start and a stop.
	/// Both are synchronous, as this program's service start and stop have always been, so the time the service control
	/// manager waits for a start is unchanged.
	/// </remarks>
	internal sealed class DataphorHostedService : IHostedService
	{
		public DataphorHostedService(string instanceName, Func<DataphorServiceHost> startServer, ILogger logger)
		{
			_instanceName = instanceName;
			_startServer = startServer;
			_logger = logger;
		}

		private readonly string _instanceName;
		private readonly Func<DataphorServiceHost> _startServer;
		private readonly ILogger _logger;

		private DataphorServiceHost _serviceHost;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Dataphor Server instance {InstanceName} is starting.", _instanceName);
			try
			{
				// Assigned only once the server is running: a start that fails has already stopped what it built, and
				// there is then nothing for StopAsync to stop.
				_serviceHost = _startServer();
			}
			catch (Exception exception)
			{
				// Rethrown, so that a console session exits 1 and the service control manager sees a failure rather
				// than a server that is not serving.
				_logger.LogError(exception, "Dataphor Server instance {InstanceName} failed to start.", _instanceName);
				throw;
			}

			_logger.LogInformation("Dataphor Server instance {InstanceName} started.", _instanceName);
			if (_serviceHost.IsListening)
				_logger.LogInformation("The listener service is serving in this process.");
			else
				_logger.LogInformation("The listener service is not serving in this process: either this instance is configured not to listen, or its listener port is already held, normally by another instance's listener on this machine. This instance's own endpoints are unaffected; the instance log has the detail.");

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			DataphorServiceHost serviceHost = _serviceHost;
			_serviceHost = null;
			if (serviceHost != null)
			{
				_logger.LogInformation("Dataphor Server instance {InstanceName} is stopping.", _instanceName);
				try
				{
					serviceHost.Stop();
				}
				catch (Exception exception)
				{
					// Logged here because nothing else logs it: the host passes a hosted service's failure to stop to the
					// caller of its own StopAsync, but logs it only at Debug level (measured), which this program's
					// minimum level does not pass - so without this the console would show "is stopping" and nothing else.
					// Rethrown for the same reason a start failure is: a stop that failed is not a clean stop.
					_logger.LogError(exception, "Dataphor Server instance {InstanceName} did not stop cleanly; treat it as having stopped abruptly.", _instanceName);
					throw;
				}

				_logger.LogInformation("Dataphor Server instance {InstanceName} stopped.", _instanceName);
			}

			return Task.CompletedTask;
		}
	}
}
