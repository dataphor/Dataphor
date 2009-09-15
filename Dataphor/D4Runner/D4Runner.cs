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
		public string Instance = Engine.CDefaultServerName;

		[Argument(ArgumentType.AtMostOnce, HelpText="Port of Dataphor Server (override to bypass the listener).", DefaultValue=0)]
		public int Port = 0;

		[DefaultArgument(ArgumentType.AtMostOnce, HelpText="D4 Script to be executed.")]
		public string Script;

		[Argument(ArgumentType.AtMostOnce, HelpText="File to be run instead of script on command line.")]
		public string File = null;

		[Argument(ArgumentType.AtMostOnce, HelpText="User to login with.", DefaultValue="Admin")]
		public string User = null;

		[Argument(ArgumentType.AtMostOnce, HelpText="Pasword to login with.", DefaultValue="")]
		public string Password = null;

		[Argument(ArgumentType.AtMostOnce, HelpText="When set to true do not print extras or prompt.", DefaultValue=false)]
		public bool Quiet = false;

		[Argument(ArgumentType.AtMostOnce, HelpText="Output options for statement-level execution.", DefaultValue=ScriptExecuteOption.ShowSuccessStatus)]
		public ScriptExecuteOption Options = ScriptExecuteOption.ShowSuccessStatus;

		[Argument(ArgumentType.AtMostOnce, HelpText="Wait for keystroke at end of execution. Warning: if Quiet is on, no prompt will be displayed that a key is required.", DefaultValue=false)]
		public bool Prompt = false;

		public int Run()
		{
			bool LHasErrors = true;
			try
			{
				using (DataSession LDataphorConnection = new DataSession())
				{
					if (File != null)
						using (StreamReader LFile = new StreamReader(File))
						{
							Script = LFile.ReadToEnd();
						}
					if (Script == null)  // Script was not in the commandline or specified in a file
					{
						Console.WriteLine("\r\nMissing D4 script; include on command line or use /File: switch.\r\n");
                        Console.WriteLine(CommandLine.Parser.ArgumentsUsage(this.GetType()));
						return 1;
					}
					if (AliasName == String.Empty)
					{
						ConnectionAlias LAlias = new ConnectionAlias();
						LAlias.HostName = Host;
						LAlias.InstanceName = Instance;
						if (Port > 0)
							LAlias.OverridePortNumber = Port;
						LDataphorConnection.Alias = LAlias;
					}
					else
						LDataphorConnection.AliasName = AliasName;

					if (User != null)
						LDataphorConnection.SessionInfo.UserID = User;
					if (Password != null)
						LDataphorConnection.SessionInfo.Password = Password;

					LDataphorConnection.Open();

					if (!Quiet)
						Console.WriteLine("Executing D4 Script:\r\n{0}\r\n", Script);

					ErrorList LErrors;
					TimeSpan LTimeSpan;
					ScriptExecutionUtility.ExecuteScript
					(
						LDataphorConnection.ServerSession, 
						Script, 
						Options, 
						out LErrors, 
						out LTimeSpan,
						delegate(PlanStatistics AStatistics, string AResults)
						{
							Console.WriteLine(AResults);
						},
						File == null ? null : new DebugLocator("file:" + Path.GetFullPath(File), 1, 1)
					);
					foreach(Exception LException in LErrors)
						Console.WriteLine(LException.Message);
				
					LHasErrors = ScriptExecutionUtility.ContainsError(LErrors);
					if (!Quiet)
						Console.WriteLine("Status: {0}  Total Time: {1}", (LHasErrors ? "Failed" : "Succeeded"), LTimeSpan);
				}
				if (Prompt)
				{
					if (!Quiet)
						Console.Write("Press any key to continue.");
					Console.Read();
				}
			} 
			catch (Exception LException)
			{
				Console.WriteLine(LException.Message);
			}
			if (LHasErrors)
				return 1;
			return 0;
		}

		[STAThread]
		static int Main(string[] AArgs)
		{
			D4Runner LApp = new D4Runner();
			if (CommandLine.Parser.ParseArgumentsWithUsage(AArgs, LApp))
                return LApp.Run();
			return 1;
		}
	}
}
