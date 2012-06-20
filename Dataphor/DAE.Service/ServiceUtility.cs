using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Alphora.Dataphor.DAE.Service
{
	public enum ServiceStatus { Running, Stopped, Unavailable, Unassigned };

	public static class ServiceUtility
	{
		// Do not localize
        public const string EventLogSource = "Dataphor Server";
		public const string ServiceStartModeRegName = "Start";
		public const string ServiceAutoStartRegValueName = "Start";
		public const int ServiceAutoStart = 2;
		public const int ServiceManualStart = 3;

		public static string GetServiceName(string instanceName)
		{
			return String.Format("Alphora Dataphor ({0})", instanceName);
		}

		private static Installer PrepareInstaller(string instanceName)
		{
			TransactedInstaller installer = new TransactedInstaller();
			installer.Context = new InstallContext("DAEService.InstallLog", new string[] {});
			installer.Context.Parameters.Add("InstanceName", instanceName);
			installer.Installers.Add(new ProjectInstaller());
			return installer;
		}

		public static void Install(string instanceName)
		{
			Installer installer = PrepareInstaller(instanceName);
			installer.Context.Parameters.Add
			(
			    "assemblypath", 
			    typeof(ServiceUtility).Assembly.Location
			);
			installer.Install(new HybridDictionary());
		}
		
		public static void Uninstall(string instanceName)
		{
			Installer installer = PrepareInstaller(instanceName);
			installer.Uninstall(null);
		}
		
		public static string GetServiceAutoStartRegKeyName(string instanceName)
		{
			return String.Format(@"System\CurrentControlSet\Services\{0}", GetServiceName(instanceName));
		}
		
		public static bool GetServiceAutoStart(string instanceName)
		{
			Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(GetServiceAutoStartRegKeyName(instanceName), true);

			// if we didn't find this reg key, the service isn't installed
			if (regKey == null)
				return false;
				
			try
			{
				object objectValue = regKey.GetValue(ServiceStartModeRegName);
				int serviceStart = (int)(objectValue != null ? objectValue : ServiceManualStart);
				return serviceStart == ServiceAutoStart;
			}
			finally
			{
				regKey.Close();
			}
		}
		
		public static void SetServiceAutoStart(string instanceName, bool autoStart)
		{
			Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(GetServiceAutoStartRegKeyName(instanceName), true);
			
			if (regKey == null)
				return;
				
			try
			{
				regKey.SetValue(ServiceAutoStartRegValueName, autoStart ? ServiceAutoStart : ServiceManualStart);
			}
			finally
			{
				regKey.Close();
			}
		}
		
		public static ServiceStatus GetServiceStatus(string instanceName)
		{
			try
			{
				ServiceController serviceController = new ServiceController(GetServiceName(instanceName));

				switch (serviceController.Status)
				{
					case ServiceControllerStatus.Running: return ServiceStatus.Running;
					case ServiceControllerStatus.Stopped: return ServiceStatus.Stopped;
				}
			}
			catch
			{
			}
			
			return ServiceStatus.Unavailable;
		}
	}
}
