/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt

	TODO: Localize D4Runner
*/

using System;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;

using CommandLine;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Debug;

namespace D4Runner
{
	class D4Runner
	{
		[Argument(ArgumentType.AtMostOnce, HelpText="The name of the alias to use to connect. If specified, this is used before the Host, Instance, and/or Port options.", DefaultValue="")]
		public string AliasName = String.Empty;
		
		[Argument(ArgumentType.AtMostOnce, HelpText="Host address (name or IP) of Dataphor Server.", DefaultValue="localhost")]
		public string Host = "localhost";
		
		[Argument(ArgumentType.AtMostOnce, HelpText="The name of the instance to use to connect.", DefaultValue="Dataphor")]
		public string Instance = Engine.DefaultServerName;

		[Argument(ArgumentType.AtMostOnce, HelpText="Port of Dataphor Server (override to bypass the listener).", DefaultValue=0)]
		public int Port = 0;

		[DefaultArgument(ArgumentType.AtMostOnce, HelpText="D4 Script to be executed.")]
		public string Script;

		[Argument(ArgumentType.AtMostOnce, HelpText="File to be run instead of script on command line.")]
		public string File = null;

		[Argument(ArgumentType.AtMostOnce, HelpText="User to login with.", DefaultValue="Admin")]
		public string User = null;

		[Argument(ArgumentType.AtMostOnce, HelpText="Password to login with.", DefaultValue="")]
		public string Password = null;

		[Argument(ArgumentType.AtMostOnce, HelpText = "When set to true the Password argument has been encrypted.", DefaultValue = true)]
		public bool PasswordEncrypted = true;

		[Argument(ArgumentType.AtMostOnce, HelpText="When set to true do not print extras or prompt.", DefaultValue=false)]
		public bool Quiet = false;

		[Argument(ArgumentType.AtMostOnce, HelpText="Output options for statement-level execution.", DefaultValue=ScriptExecuteOption.ShowSuccessStatus)]
		public ScriptExecuteOption Options = ScriptExecuteOption.ShowSuccessStatus;

		[Argument(ArgumentType.AtMostOnce, HelpText="Wait for keystroke at end of execution. Warning: if Quiet is on, no prompt will be displayed that a key is required.", DefaultValue=false)]
		public bool Prompt = false;

		public int Run()
		{
			bool hasErrors = true;
			try
			{
				using (DataSession dataphorConnection = new DataSession())
				{
					if (File != null)
						using (StreamReader file = new StreamReader(File))
						{
							Script = file.ReadToEnd();
						}
					if (Script == null)  // Script was not in the commandline or specified in a file
					{
						Console.WriteLine("\r\nMissing D4 script; include on command line or use /File: switch.\r\n");
                        Console.WriteLine(CommandLine.Parser.ArgumentsUsage(this.GetType()));
						return 1;
					}
					if (AliasName == String.Empty)
					{
						ConnectionAlias alias = new ConnectionAlias();
						alias.HostName = Host;
						alias.InstanceName = Instance;
						if (Port > 0)
							alias.OverridePortNumber = Port;
						dataphorConnection.Alias = alias;
					}
					else
						dataphorConnection.AliasName = AliasName;

					if (User != null)
						dataphorConnection.SessionInfo.UserID = User;

					if (Password != null) 
					{
						if (PasswordEncrypted == true)
							dataphorConnection.SessionInfo.UnstructuredData = Password;
						else 
							dataphorConnection.SessionInfo.Password = Password;
					}
					
					dataphorConnection.Open();

					if (!Quiet)
						Console.WriteLine("Executing D4 Script:\r\n{0}\r\n", Script);

					ErrorList errors;
					TimeSpan timeSpan;
					ScriptExecutionUtility.ExecuteScript
					(
						dataphorConnection.ServerSession, 
						Script, 
						Options, 
						out errors, 
						out timeSpan,
						delegate(PlanStatistics AStatistics, string AResults)
						{
							Console.WriteLine(AResults);
						},
						File == null ? null : new DebugLocator("file:" + Path.GetFullPath(File), 1, 1)
					);
					foreach(Exception exception in errors)
						Console.WriteLine(exception.Message);
				
					hasErrors = ScriptExecutionUtility.ContainsError(errors);
					if (!Quiet)
						Console.WriteLine("Status: {0}  Total Time: {1}", (hasErrors ? "Failed" : "Succeeded"), timeSpan);
				}
				if (Prompt)
				{
					if (!Quiet)
						Console.Write("Press any key to continue.");
					Console.Read();
				}
			} 
			catch (Exception exception)
			{
				Console.WriteLine(exception.Message);
			}
			if (hasErrors)
				return 1;
			return 0;
		}

		[STAThread]
		static int Main(string[] args)
		{
			D4Runner app = new D4Runner();
			if (CommandLine.Parser.ParseArgumentsWithUsage(args, app))
                return app.Run();
			return 1;
		}
	}
}
