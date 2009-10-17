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
		public Service()
		{
		}
		
		public Service(string AServiceName)
		{
			ServiceName = AServiceName;
		}
		
		public Service(string AServiceName, string AInstanceName)
		{
			ServiceName = AServiceName;
			FInstanceName = AInstanceName;
		}
		
		private string FInstanceName;
		
		private static string GetInstanceName(string[] AArgs)
		{
			for (int i = 0; i < AArgs.Length - 1; i++)
				if ((AArgs[i].ToLower() == "-name") || (AArgs[i].ToLower() == "-n"))
					return AArgs[i + 1];
					
			return null;
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
						if (LInstanceName == null)
							LInstanceName = Server.Server.CDefaultServerName;

						Service LService = new Service(ServiceUtility.GetServiceName(LInstanceName), LInstanceName);
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
				new System.ServiceProcess.ServiceBase[] { LInstanceName == null ? new Service() : new Service(ServiceUtility.GetServiceName(LInstanceName), LInstanceName) }
			);
		}

		public Server.Server FServer;
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
				
				if (FInstanceName == null)
					FInstanceName = GetInstanceName(args);
					
				if (FInstanceName == null)
					FInstanceName = Server.Server.CDefaultServerName;
				
				ServerConfiguration LInstance = InstanceManager.Instances[FInstanceName];
				if (LInstance == null)
				{
					// Establish a default configuration
					LInstance = ServerConfiguration.DefaultInstance(FInstanceName);
					InstanceManager.Instances.Add(LInstance);
					InstanceManager.Save();
				}
				
				FServer = new Server.Server();
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

