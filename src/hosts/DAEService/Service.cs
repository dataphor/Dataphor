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
using System.ServiceModel;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	public class Service : System.ServiceProcess.ServiceBase
	{
		public Service() : base()
		{
		}
		
		public Service(string serviceName) : base()
		{
			ServiceName = serviceName;
		}
		
		public Service(string serviceName, string instanceName) : base()
		{
			ServiceName = serviceName;
			_instanceName = instanceName;
		}
		
		private string _instanceName;
		
		private static string GetInstanceName(string[] args)
		{
			for (int i = 0; i < args.Length - 1; i++)
				if ((args[i].ToLower() == "-name") || (args[i].ToLower() == "-n"))
					return args[i + 1];
					
			return null;
		}
		
		// The main entry point for the process
		static void Main(string[] args)
		{
			string instanceName = GetInstanceName(args);
			if (args.Length > 0)
			{
				switch (args[0].ToLower())
				{
					case "-i" :
					case "-install" :
						ServiceUtility.Install(instanceName);
						return;
					case "-u" :
					case "-uninstall" :
						ServiceUtility.Uninstall(instanceName);
						return;
					case "-r" :
					case "-run" :
						if (instanceName == null)
							instanceName = Server.Engine.DefaultServerName;
							
						Service service = new Service(ServiceUtility.GetServiceName(instanceName), instanceName);
						Console.WriteLine(Strings.Get("ServiceStarting"));
						service.OnStart(args);
						try
						{
							Console.WriteLine(Strings.Get("ServiceRunning"));
							Console.ReadLine();
						}
						finally
						{
							Console.WriteLine(Strings.Get("ServiceStopping"));
							service.OnStop();
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
				new System.ServiceProcess.ServiceBase[] { instanceName == null ? new Service() : new Service(ServiceUtility.GetServiceName(instanceName), instanceName) }
			);
		}

		private DataphorServiceHost _dataphorServiceHost;
		
		/// <summary>
		/// Set things in motion so your Dataphor service can do its work. Checks to see if the ServerSettings.dst file exists
		/// in the directory containing the service before deserializing the settings from the file stream.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			// The args passed here are only coming in from the arguments to the service 
			// as a result of a manual start from the service control panel, not the
			// arguments to the actual service. The InstanceName is pulled from
			// those arguments in the Main static and passed to the constructor of
			// the service.
			try
			{
				if (_instanceName == null)
					_instanceName = GetInstanceName(args);

				if (_instanceName == null)
					_instanceName = Server.Engine.DefaultServerName;
					
				_dataphorServiceHost = new DataphorServiceHost();
				_dataphorServiceHost.InstanceName = _instanceName;
				_dataphorServiceHost.Start();
			}
			catch (Exception exception)
			{
				LogException(exception);
				throw;
			}
		}
 
		protected override void OnStop()
		{
			try 
			{
				if (_dataphorServiceHost != null)
				{
					if (_dataphorServiceHost.IsActive)
						_dataphorServiceHost.Stop();
					_dataphorServiceHost = null;
				}
			}
			catch (Exception exception)
			{
				LogException(exception); 
			}
		}

		/// <summary>Logs exceptions thrown by the server</summary>
		public static void LogException(Exception exception)
		{
			System.Diagnostics.EventLog.WriteEntry(ServiceUtility.EventLogSource, ExceptionUtility.BriefDescription(exception), EventLogEntryType.Error); 
		}
	}
}

