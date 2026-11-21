/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace DAE.Test.Harness
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Service;

	/// <summary>
	/// The test suite's own server program: it hosts one Dataphor instance in a process of its own so that a fixture in
	/// tests/DAE.Server.Tests can reach a server the way the product does - from another program, over the network.
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
	/// </remarks>
	public static class Program
	{
		public const string ServeMode = "serve";
		public const string InstanceOption = "--instance";
		public const string HostNameOption = "--host-name";
		public const string LoggingOption = "--logging";

		/// <summary>The first word of the one line this program writes to standard output once the server is serving.</summary>
		/// <remarks>The rest of the line is the instance name, the port it is serving on, and listening=&lt;true|false&gt;.</remarks>
		public const string ReadyPrefix = "READY";

		private const string Usage = "DAE.Test.Harness " + ServeMode + " " + InstanceOption + " <name> [" + HostNameOption + " <name-or-address>] [" + LoggingOption + "]";

		public static int Main(string[] args)
		{
			try
			{
				if ((args.Length > 0) && (args[0] == ServeMode))
					return Serve(args);

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
			// literal instead, so no port this suite opens is reachable from another machine.
			string hostName = Environment.MachineName;
			// Off, which is the opposite of the shipped host's default and the whole reason this program exists.
			bool loggingEnabled = false;

			for (int index = 1; index < args.Length; index++)
				switch (args[index])
				{
					case InstanceOption: instanceName = OptionValue(args, ref index); break;
					case HostNameOption: hostName = OptionValue(args, ref index); break;
					case LoggingOption: loggingEnabled = true; break;
					default: throw new ArgumentException(String.Format("Unrecognized argument \"{0}\". Usage: {1}", args[index], Usage));
				}

			if (String.IsNullOrEmpty(instanceName))
				throw new ArgumentException(String.Format("{0} is required. Usage: {1}", InstanceOption, Usage));

			ServerConfiguration instance = RequireInstance(instanceName);

			DataphorServiceHost host = new DataphorServiceHost();
			host.InstanceName = instanceName;
			host.HostName = hostName;
			host.LoggingEnabled = loggingEnabled;

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

		private static string OptionValue(string[] args, ref int index)
		{
			string option = args[index];
			index++;
			if (index >= args.Length)
				throw new ArgumentException(String.Format("{0} needs a value. Usage: {1}", option, Usage));
			return args[index];
		}
	}
}
