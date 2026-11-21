/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Threading;

namespace DAE.Test.Harness
{
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Client;
	using Alphora.Dataphor.DAE.Contracts;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Service;

	/// <summary>
	/// The test suite's own program for either side of a client/server case. In <see cref="ServeMode"/> it hosts one
	/// Dataphor instance in a process of its own, so that a fixture in tests/DAE.Server.Tests can reach a server the way
	/// the product does - from another program, over the network. In <see cref="ClientMode"/> it is a shipped client
	/// stack holding a transaction open until it is killed, which is what a fixture needs to show what the server does
	/// about a client process that dies.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Not the shipped executable (artifacts/layout/&lt;config&gt;/bin/DAEServer.exe), for one reason that cannot be
	/// worked around from outside it: a hosted server logs unless its host turns logging off, and a logging server in an
	/// elevated process - continuous-integration runners are elevated - creates the Windows event-log source "Dataphor
	/// Server" and writes to the Windows event log (Server.StartLog and Server.LogMessage, both gated on
	/// IsAdministrator). A test must create no machine state, and the shipped host has no way to be told to be quiet:
	/// neither its command line nor ServerConfiguration carries LoggingEnabled. This program sets the two host-object
	/// properties that are not instance settings - LoggingEnabled and HostName - and then starts the production host
	/// through the production entry point. debt-test-shipped-host-executable owns covering the shipped executable, and
	/// names the knob it needs.
	/// </para>
	/// <para>
	/// Everything else about the instance comes from an instance list the caller writes and points this process at with
	/// DATAPHOR_INSTANCE_DIRECTORY (<see cref="InstanceManager.InstanceDirectoryVariableName"/>), so no run of this
	/// program reads or writes the machine's own instance list.
	/// </para>
	/// <para>
	/// The client mode needs no instance list and no files of its own: it is a client, and everything it needs is on its
	/// command line. It is in this program rather than one of its own so that there is one path for a fixture to find
	/// (tests/DAE.Server.Tests records this program's build output as assembly metadata) and one place the two sides'
	/// command lines are declared.
	/// </para>
	/// </remarks>
	public static class Program
	{
		public const string ServeMode = "serve";
		public const string ClientMode = "client";
		public const string InstanceOption = "--instance";
		public const string HostNameOption = "--host-name";
		public const string LoggingOption = "--logging";
		public const string IdleSecondsOption = "--idle-seconds";
		public const string HostOption = "--host";
		public const string PortOption = "--port";
		public const string KeepAliveSecondsOption = "--keep-alive-seconds";
		public const string HoldTransactionOption = "--hold-transaction";

		/// <summary>The first word of the one line this program writes to standard output once the server is serving.</summary>
		/// <remarks>The rest of the line is the instance name, the port it is serving on, and listening=&lt;true|false&gt;.</remarks>
		public const string ReadyPrefix = "READY";

		/// <summary>The first word of the one line the client mode writes once its transaction is open.</summary>
		/// <remarks>
		/// The rest of the line is the table name and the identifier of the server session holding the transaction. The
		/// identifier is there so that the fixture can watch for that one session by name rather than reading the
		/// server's whole session table: reading a table over the wire builds a client-side view from a catalog fragment
		/// the server sends, and that path has a defect of its own which has nothing to do with what the case is about
		/// (see the arm on bug-object-id-counter-is-process-wide).
		/// </remarks>
		public const string HoldingPrefix = "HOLDING";

		private const string ServeUsage = "DAE.Test.Harness " + ServeMode + " " + InstanceOption + " <name> [" + HostNameOption + " <name-or-address>] [" + IdleSecondsOption + " <n>] [" + LoggingOption + "]";
		private const string ClientUsage = "DAE.Test.Harness " + ClientMode + " " + HostOption + " <name-or-address> " + PortOption + " <n> " + InstanceOption + " <name> [" + KeepAliveSecondsOption + " <n>] " + HoldTransactionOption + " <table-name>";

		private static string Usage => ServeUsage + Environment.NewLine + "       " + ClientUsage;

		public static int Main(string[] args)
		{
			try
			{
				if (args.Length > 0)
					switch (args[0])
					{
						case ServeMode: return Serve(args);
						case ClientMode: return HoldATransactionUntilKilled(args);
					}

				throw new ArgumentException("Usage: " + Usage);
			}
			catch (Exception exception)
			{
				// The whole exception: this text is all the fixture that started the process has to diagnose a failed start,
				// and it reports it as the reason the server never became ready.
				Console.Error.WriteLine(exception.ToString());
				Console.Error.Flush();
				return 1;
			}
		}

		/// <summary>
		/// Serves one instance until a line arrives on standard input, or standard input reaches its end, and then stops.
		/// </summary>
		/// <remarks>
		/// End of input matters as much as the line: when the parent process dies, this process's input pipe closes, and
		/// that is what keeps a crashed or killed test run from leaving a server behind holding a port, a catalog and a
		/// temporary folder. DataphorHost deliberately does not read standard input for the shipped host, because a
		/// server's input may be redirected or closed; for a program whose only caller is a test fixture that is the right
		/// mechanism rather than the wrong one.
		/// </remarks>
		private static int Serve(string[] args)
		{
			string instanceName = null;
			// The product default, which is what DataphorServiceHost defaults HostName to; the fixtures pass the loopback
			// literal instead, so no port this suite opens is reachable from another machine - except for a child that
			// serves a listener, which passes this default deliberately (see OutOfProcessInstance.HostName).
			string hostName = Environment.MachineName;
			// Off, which is the opposite of the shipped host's default and the whole reason this program exists.
			bool loggingEnabled = false;
			// The product's five minutes, so a run that does not ask reclaims connections exactly as a deployment does.
			int connectionIdleTimeInSeconds = ConnectionManager.IdleTimeInSeconds;

			for (int index = 1; index < args.Length; index++)
				switch (args[index])
				{
					case InstanceOption: instanceName = OptionValue(args, ref index, ServeUsage); break;
					case HostNameOption: hostName = OptionValue(args, ref index, ServeUsage); break;
					case IdleSecondsOption: connectionIdleTimeInSeconds = OptionNumber(args, ref index, ServeUsage); break;
					case LoggingOption: loggingEnabled = true; break;
					default: throw new ArgumentException(String.Format("Unrecognized argument \"{0}\". Usage: {1}", args[index], ServeUsage));
				}

			if (String.IsNullOrEmpty(instanceName))
				throw new ArgumentException(String.Format("{0} is required. Usage: {1}", InstanceOption, ServeUsage));

			ServerConfiguration instance = RequireInstance(instanceName);

			DataphorServiceHost host = new DataphorServiceHost();
			host.InstanceName = instanceName;
			host.HostName = hostName;
			host.LoggingEnabled = loggingEnabled;
			// Below one second the host refuses to start, in the ConnectionManager constructor this reaches; the caller
			// then sees that error on standard error, which is the right answer for a value no deployment ever passes.
			host.ConnectionIdleTimeInSeconds = connectionIdleTimeInSeconds;

			// The production entry point, which reads the instance list this process was pointed at.
			host.Start();
			try
			{
				Console.Out.WriteLine("{0} {1} {2} listening={3}", ReadyPrefix, instanceName, instance.PortNumber, host.IsListening ? "true" : "false");
				Console.Out.Flush();

				Console.In.ReadLine();
			}
			finally
			{
				host.Stop();
			}

			return 0;
		}

		/// <summary>The instance this run is to serve, read from the list this process was pointed at.</summary>
		/// <remarks>
		/// Required to be there, rather than left to the host: DataphorServiceHost.Start() registers a default
		/// configuration for a name its list does not hold and saves the list, and that configuration's library directory
		/// is the runtime layout's own - which a running server writes into. A caller that got the redirection or the
		/// file wrong must fail here instead of starting a server against the layout. The port for the ready line comes
		/// from the same read, so that line also says which configuration this process actually found.
		/// </remarks>
		private static ServerConfiguration RequireInstance(string instanceName)
		{
			InstanceManager.Load();
			ServerConfiguration result = InstanceManager.Instances[instanceName];
			if (result == null)
				throw new ArgumentException
				(
					String.Format
					(
						"The instance list in \"{0}\" has no instance named \"{1}\", so there is no configuration to serve. Write one there, or set {2} to the folder that holds it.",
						InstanceManager.InstanceDirectory,
						instanceName,
						InstanceManager.InstanceDirectoryVariableName
					)
				);
			return result;
		}

		private static string OptionValue(string[] args, ref int index, string usage)
		{
			string option = args[index];
			index++;
			if (index >= args.Length)
				throw new ArgumentException(String.Format("{0} needs a value. Usage: {1}", option, usage));
			return args[index];
		}

		private static int OptionNumber(string[] args, ref int index, string usage)
		{
			string option = args[index];
			string value = OptionValue(args, ref index, usage);
			if (!Int32.TryParse(value, out int result))
				throw new ArgumentException(String.Format("{0} needs a whole number, not \"{1}\". Usage: {2}", option, value, usage));
			return result;
		}

		/// <summary>The client's own host name, which the server records against the connection; not a connection target.</summary>
		/// <remarks>The literal the rest of this suite's clients use, so that nothing in a server's log names this machine.</remarks>
		private const string ClientHostName = "localhost";

		/// <summary>
		/// Connects to a server with the shipped client stack, opens a transaction that creates one table, says so on
		/// standard output, and then blocks for ever: the caller kills this process, which is the case this mode exists
		/// for.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Nothing below is disposed and no path leads out of this method. That is the point: a client that shut its
		/// session down, or let its process exit, would relinquish its connection the ordinary way and show nothing about
		/// a client that dies holding one. The keep-alive thread goes on pinging until the process is killed, so the
		/// server sees a client that was alive and then was not - which is what distinguishes this from a connection that
		/// was never pinged at all (tests/DAE.Server.Tests/AbandonedSessionCleanupTest.cs covers that one, in process).
		/// </para>
		/// <para>
		/// NOTE: a test process that is killed outright between starting this child and killing it leaves this process
		/// sleeping for ever, because there is deliberately no standard-input stop path here as there is in the serve
		/// mode. The fixture kills it on every path it can reach, which covers a failing or throwing case; only a kill of
		/// the test host itself escapes that. If strays are ever actually seen, give this mode the serve mode's
		/// end-of-input exit - it cannot affect a case, which kills the child while its parent is alive - and have the
		/// fixture assert the child's exit code shows a kill rather than a graceful exit.
		/// </para>
		/// </remarks>
		private static int HoldATransactionUntilKilled(string[] args)
		{
			string hostName = null;
			string instanceName = null;
			string tableName = null;
			int portNumber = 0;
			// The shipped client's two minutes, which a caller whose server runs a shorter idle threshold replaces.
			int keepAliveIntervalSeconds = LocalServer.DefaultKeepAliveInterval;

			for (int index = 1; index < args.Length; index++)
				switch (args[index])
				{
					case HostOption: hostName = OptionValue(args, ref index, ClientUsage); break;
					case PortOption: portNumber = OptionNumber(args, ref index, ClientUsage); break;
					case InstanceOption: instanceName = OptionValue(args, ref index, ClientUsage); break;
					case KeepAliveSecondsOption: keepAliveIntervalSeconds = OptionNumber(args, ref index, ClientUsage); break;
					case HoldTransactionOption: tableName = OptionValue(args, ref index, ClientUsage); break;
					default: throw new ArgumentException(String.Format("Unrecognized argument \"{0}\". Usage: {1}", args[index], ClientUsage));
				}

			RequireOption(HostOption, hostName);
			RequireOption(InstanceOption, instanceName);
			RequireOption(HoldTransactionOption, tableName);
			if (portNumber <= 0)
				throw new ArgumentException(String.Format("{0} is required and must be a port number. Usage: {1}", PortOption, ClientUsage));

			// The shipped client stack, as src/client/DAE.Client/ServerConnection.cs assembles it for a connection alias
			// and as tests/DAE.Server.Tests/ProtocolClient.cs assembles it in the test process: a ClientServer on the
			// product's binding, wrapped in a LocalServer. The LocalServer is what pings.
			ClientServer clientServer = new ClientServer(hostName, instanceName, portNumber, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default);
			LocalServer localServer = new LocalServer(clientServer, false, ClientHostName, keepAliveIntervalSeconds);
			IServerSession session = localServer.Connect(new SessionInfo("Admin", ""));
			IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
			process.BeginTransaction(IsolationLevel.CursorStability);
			process.Execute(String.Format("create table {0} {{ ID : Integer, key {{ ID }} }};", tableName), null);

			// The server's own identifier for this session, which is what its System.Sessions table holds. Read inside
			// the transaction, so a caller that sees the line below knows the session it names is the one holding it.
			int sessionID = (int)process.Evaluate("SessionID()", null).AsNative;

			// Written only once the transaction is open, so that a caller which waits for this line never has to sleep
			// to know the child is in the state its case needs.
			Console.Out.WriteLine("{0} {1} {2}", HoldingPrefix, tableName, sessionID);
			Console.Out.Flush();

			Thread.Sleep(Timeout.Infinite);
			return 0;
		}

		private static void RequireOption(string option, string value)
		{
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException(String.Format("{0} is required. Usage: {1}", option, ClientUsage));
		}
	}
}
