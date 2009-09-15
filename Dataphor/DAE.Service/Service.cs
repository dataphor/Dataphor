/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	public class Service : System.ServiceProcess.ServiceBase
	{
		public Service(string AServiceName)
		{
			ServiceName = AServiceName;
		}
		
		private static string GetInstanceName(string[] AArgs)
		{
			for (int i = 1; i < AArgs.Length - 1; i++)
				if ((AArgs[i].ToLower() == "-name") || (AArgs[i].ToLower() == "-n"))
					return AArgs[i + 1];
					
			return Server.Engine.CDefaultServerName;
		}

		// The main entry point for the process
		static void Main(string[] AArgs)
		{
			string LInstanceName = GetInstanceName(AArgs);
			if (AArgs.Length > 0)
			{
				switch (AArgs[0].ToLower())
				{
					case "-i" :
					case "-install" :
						ServiceUtility.Install(LInstanceName);
						return;
					case "-u" :
					case "-uninstall" :
						ServiceUtility.Uninstall(LInstanceName);
						return;
					case "-r" :
					case "-run" :
						Service LService = new Service(ServiceUtility.GetServiceName(LInstanceName));
						Console.WriteLine(Strings.Get("ServiceStarting"));
						LService.OnStart(AArgs);
						try
						{
							Console.WriteLine(Strings.Get("ServiceRunning"));
							Console.ReadLine();
						}
						finally
						{
							Console.WriteLine(Strings.Get("ServiceStopping"));
							LService.OnStop();
						}
						return;
					case "-name" :
					case "-n" :
						break;
					default :
						throw new ArgumentException(Strings.Get("InvalidCommandLine"));
				}
			}
			System.ServiceProcess.ServiceBase.Run
			(
				new System.ServiceProcess.ServiceBase[] { new Service(ServiceUtility.GetServiceName(LInstanceName)) }
			);
		}

		public Server.Engine FServer;
		public ServerHost FServerHost;
		
		/// <summary>
		/// Set things in motion so your Dataphor service can do its work. Checks to see if the ServerSettings.dst file exists
		/// in the directory containing the service before deserializing the settings from the file stream.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			try
			{
				InstanceManager.Load();
				
				string LInstanceName = GetInstanceName(args);
				ServerConfiguration LInstance = InstanceManager.Instances[LInstanceName];
				if (LInstance == null)
				{
					// Establish a default configuration
					LInstance = ServerConfiguration.DefaultInstance(LInstanceName);
					InstanceManager.Instances.Add(LInstance);
					InstanceManager.Save();
				}
				
				FServer = new Server.Engine();
				LInstance.ApplyTo(FServer);
				FServerHost = new ServerHost(FServer, LInstance.PortNumber);
				FServer.Start();
			}
			catch (Exception LException)
			{
				LogException(LException);
				throw;
			}
		}
 
		protected override void OnStop()
		{
			try 
			{
				FServer.Stop();
			}
			catch (Exception LException)
			{
				LogException(LException); 
			}
		}

		/// <summary>Logs exceptions thrown by the server</summary>
		public static void LogException(Exception AException)
		{
			System.Diagnostics.EventLog.WriteEntry(ServiceUtility.CEventLogSource, ExceptionUtility.BriefDescription(AException), EventLogEntryType.Error); 
		}
	}
}

