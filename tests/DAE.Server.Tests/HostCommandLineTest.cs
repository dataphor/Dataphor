/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Service;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The command line DAEServer.exe and DAEService.exe take. The two programs had a hand-written parser each, and
	/// host-generic-host replaced both with one; what those parsers accepted is what an operator's script and an
	/// existing service registration pass, so it is pinned here rather than left to be rediscovered.
	/// </summary>
	[TestFixture]
	public class HostCommandLineTest
	{
		private sealed class Case
		{
			public Case(string[] args, HostCommand command, string instanceName)
			{
				Args = args;
				Command = command;
				InstanceName = instanceName;
			}

			public readonly string[] Args;
			public readonly HostCommand Command;
			public readonly string InstanceName;

			public override string ToString()
			{
				return Args.Length == 0 ? "(no arguments)" : String.Join(" ", Args);
			}
		}

		private const string Instance = "Second";

		private static readonly Case[] _cases =
			new Case[]
			{
				// No arguments, and the switches that only ask to run.
				new Case(new string[0], HostCommand.Run, Engine.DefaultServerName),
				new Case(new string[] { "-r" }, HostCommand.Run, Engine.DefaultServerName),
				new Case(new string[] { "-run" }, HostCommand.Run, Engine.DefaultServerName),

				// An instance name on its own asks to run: this is the command line a registered service carries.
				new Case(new string[] { "-n", Instance }, HostCommand.Run, Instance),
				new Case(new string[] { "-name", Instance }, HostCommand.Run, Instance),
				new Case(new string[] { "-N", Instance }, HostCommand.Run, Instance),
				new Case(new string[] { "-NAME", Instance }, HostCommand.Run, Instance),
				new Case(new string[] { "-r", "-n", Instance }, HostCommand.Run, Instance),

				// An instance name with a space in it, which the default service registration quotes.
				new Case(new string[] { "-name", "Two Words" }, HostCommand.Run, "Two Words"),

				// Registering and removing the service, with and without an instance name.
				new Case(new string[] { "-i" }, HostCommand.Install, Engine.DefaultServerName),
				new Case(new string[] { "-install" }, HostCommand.Install, Engine.DefaultServerName),
				new Case(new string[] { "-i", "-n", Instance }, HostCommand.Install, Instance),
				new Case(new string[] { "-u" }, HostCommand.Uninstall, Engine.DefaultServerName),
				new Case(new string[] { "-uninstall" }, HostCommand.Uninstall, Engine.DefaultServerName),
				new Case(new string[] { "-uninstall", "-name", Instance }, HostCommand.Uninstall, Instance),

				// A trailing -n with nothing after it yields the default instance rather than an error, because the
				// scan for the name stops one argument short of the end. Both parsers behaved this way, so a script
				// could rely on it.
				new Case(new string[] { "-n" }, HostCommand.Run, Engine.DefaultServerName),
				new Case(new string[] { "-r", "-name" }, HostCommand.Run, Engine.DefaultServerName)
			};

		[Test]
		public void ParseReadsEveryArgumentFormTheProgramsAccepted()
		{
			Assert.Multiple
			(
				() =>
				{
					foreach (Case test in _cases)
					{
						HostCommandLine commandLine = HostCommandLine.Parse(test.Args);
						Assert.That(commandLine.Command, Is.EqualTo(test.Command), test.ToString());
						Assert.That(commandLine.InstanceName, Is.EqualTo(test.InstanceName), test.ToString());
					}
				}
			);
		}

		[Test]
		public void ParseRejectsAFirstArgumentThatIsNotASwitchItKnows()
		{
			// DAEService threw on anything else; DAEServer accepted and ignored every argument, which meant a
			// mistyped switch silently started the default instance. One parser means one answer, and it is this one.
			Assert.Multiple
			(
				() =>
				{
					Assert.That(Assert.Catch(() => HostCommandLine.Parse(new string[] { "-x" })), Is.TypeOf<ArgumentException>());
					Assert.That(Assert.Catch(() => HostCommandLine.Parse(new string[] { Instance })), Is.TypeOf<ArgumentException>());
					Assert.That(Assert.Catch(() => HostCommandLine.Parse(new string[] { "/name", Instance })), Is.TypeOf<ArgumentException>());
				}
			);
		}
	}
}
