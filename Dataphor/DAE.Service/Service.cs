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

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.DAE.Service
{
	public class Service : System.ServiceProcess.ServiceBase
	{
		// Do not localize
        public const string CEventLogSource = "Dataphor Server";

		public Service(string AServiceName)
		{
			ServiceName = AServiceName;
		}

		private static string GetServiceName(string[] AArgs)
		{
			for (int i = 1; i < AArgs.Length - 1; i++)
				if ((AArgs[i].ToLower() == "-name") || (AArgs[i].ToLower() == "-n"))
					return AArgs[i + 1];
			return ServerService.GetServiceName();
		}

		private static Installer PrepareInstaller(string AServiceName)
		{
			TransactedInstaller LInstaller = new TransactedInstaller();
			LInstaller.Context = new InstallContext("DAEService.InstallLog", new string[] {});
			LInstaller.Context.Parameters.Add("ServiceName", AServiceName);
			LInstaller.Installers.Add(new ProjectInstaller());
			return LInstaller;
		}

		// The main entry point for the process
		static void Main(string[] AArgs)
		{
			string LServiceName = GetServiceName(AArgs);
			if (AArgs.Length > 0)
			{
				Installer LInstaller;
				switch (AArgs[0].ToLower())
				{
					case "-i" :
					case "-install" :
						LInstaller = PrepareInstaller(LServiceName);
						LInstaller.Context.Parameters.Add
						(
							"assemblypath", 
							String.Format
							(
								"{0} -name \"{1}\"",
								System.Reflection.Assembly.GetExecutingAssembly().Location,
								LServiceName
							)
						);
						LInstaller.Install(new HybridDictionary());
						return;
					case "-u" :
					case "-uninstall" :
						LInstaller = PrepareInstaller(LServiceName);
						LInstaller.Uninstall(null);
						return;
					case "-r" :
					case "-run" :
						Service LService = new Service(LServiceName);
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
				new System.ServiceProcess.ServiceBase[] { new Service(LServiceName) }
			);
		}

		/// <summary>The Server Service object declaration </summary>
		public Alphora.Dataphor.DAE.Server.ServerService FService = new Alphora.Dataphor.DAE.Server.ServerService();
		
		/// <summary>
		/// Set things in motion so your Dataphor service can do its work. Checks to see if the ServerSettings.dst file exists
		/// in the directory containing the service before deserializing the settings from the file stream.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			FService = new Alphora.Dataphor.DAE.Server.ServerService();
			FService.CatalogDirectory = DAE.Server.Server.GetDefaultCatalogDirectory();
			try 
			{
				string LConfigFileName = ServerService.GetServiceConfigFileName();
				if (File.Exists(LConfigFileName))
				{
					using(FileStream LStream = new FileStream(LConfigFileName, FileMode.Open, FileAccess.Read))
					{
						(new Alphora.Dataphor.BOP.Deserializer()).Deserialize(LStream, FService);
					}
				}
				else
				{
					// Default the configuration
					FService.LibraryDirectory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(GetType().Assembly.Location)), @"Libraries");
					FService.CatalogDirectory = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"Catalog");

					// Write the configuration
					using(FileStream LStream = new FileStream(LConfigFileName, FileMode.Create, FileAccess.Write))
					{
						(new Alphora.Dataphor.BOP.Serializer()).Serialize(LStream, FService);
					}

					System.Diagnostics.EventLog.WriteEntry(CEventLogSource, Strings.Get("CCreatedSettingsFile"), EventLogEntryType.Warning);
				}
			}
			catch (Exception LException)
			{
				LogException(LException); 
			}
				
			try
			{
				FService.Start();   // start the server
			}
			catch (Exception LException)
			{
				LogException(LException);
				throw;	// Don't continue
			}
		}
 
		protected override void OnStop()
		{
			try 
			{
				FService.Stop();   //   stop the server
			}
			catch (Exception LException)
			{
				LogException(LException); 
			}
		}
		/// <summary>Logs exceptions thrown by the server</summary>
		public static void LogException(Exception AException)
		{
			System.Diagnostics.EventLog.WriteEntry(CEventLogSource, ExceptionUtility.BriefDescription(AException), EventLogEntryType.Error); 
		}
	}
}

