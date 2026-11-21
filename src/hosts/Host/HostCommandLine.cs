/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Service
{
	using Alphora.Dataphor.DAE.Server;

	/// <summary>What the host program was asked to do.</summary>
	public enum HostCommand { Run, Install, Uninstall }

	/// <summary>The command line both host executables take.</summary>
	/// <remarks>
	/// <para>
	/// One parser for the console host and the Windows service, which are now one program under two names, so that a
	/// 3.1 script or an existing service registration calling either of them means the same thing. The behaviour is
	/// the behaviour of the two hand-written parsers it replaces, quirks included.
	/// </para>
	/// <para>
	/// The instance name comes from -n or -name anywhere in the arguments and is the argument after it. The scan stops
	/// one short of the end, so a trailing -n with no value after it yields no instance name rather than an error, and
	/// the default is used; that is what both programs did, and a script could rely on it. The command comes from the
	/// first argument alone, and anything else there is rejected - which DAEService always did and DAEServer, which
	/// accepted and ignored any arguments, now does too (see docs/migration-notes.md).
	/// </para>
	/// </remarks>
	public sealed class HostCommandLine
	{
		private HostCommandLine(HostCommand command, string instanceName)
		{
			_command = command;
			_instanceName = instanceName;
		}

		private readonly HostCommand _command;
		public HostCommand Command { get { return _command; } }

		private readonly string _instanceName;
		/// <summary>The instance to act on; never null, defaulting to <see cref="Engine.DefaultServerName"/>.</summary>
		public string InstanceName { get { return _instanceName; } }

		/// <summary>Parses a host executable's arguments.</summary>
		/// <exception cref="ArgumentException">The first argument is not one of the recognized switches.</exception>
		public static HostCommandLine Parse(string[] args)
		{
			return new HostCommandLine(GetCommand(args), GetInstanceName(args) ?? Engine.DefaultServerName);
		}

		private static HostCommand GetCommand(string[] args)
		{
			if (args.Length == 0)
				return HostCommand.Run;

			switch (args[0].ToLower())
			{
				case "-i" :
				case "-install" :
					return HostCommand.Install;
				case "-u" :
				case "-uninstall" :
					return HostCommand.Uninstall;
				case "-r" :
				case "-run" :
				// Running is also what an instance name on its own asks for, which is the command line a registered service carries.
				case "-n" :
				case "-name" :
					return HostCommand.Run;
				default :
					throw new ArgumentException(Strings.Get("InvalidCommandLine"));
			}
		}

		private static string GetInstanceName(string[] args)
		{
			for (int i = 0; i < args.Length - 1; i++)
				if ((args[i].ToLower() == "-name") || (args[i].ToLower() == "-n"))
					return args[i + 1];

			return null;
		}
	}
}
