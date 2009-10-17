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
        public const string CEventLogSource = "Dataphor Server";
		public const string CServiceStartModeRegName = "Start";
		public const string CServiceAutoStartRegValueName = "Start";
		public const int CServiceAutoStart = 2;
		public const int CServiceManualStart = 3;

		public static string GetServiceName(string AInstanceName)
		{
			return String.Format("Alphora Dataphor ({0}){1}", AInstanceName, AInstanceName == Server.Server.CDefaultServerName ? " <default instance>" : "");
		}

		private static Installer PrepareInstaller(string AInstanceName)
		{
			TransactedInstaller LInstaller = new TransactedInstaller();
			LInstaller.Context = new InstallContext("DAEService.InstallLog", new string[] {});
			LInstaller.Context.Parameters.Add("InstanceName", AInstanceName);
			LInstaller.Installers.Add(new ProjectInstaller());
			return LInstaller;
		}

		public static void Install(string AInstanceName)
		{
			Installer LInstaller = PrepareInstaller(AInstanceName);
			LInstaller.Context.Parameters.Add
			(
			    "assemblypath", 
			    typeof(ServiceUtility).Assembly.Location
			);
			LInstaller.Install(new HybridDictionary());
		}
		
		public static void Uninstall(string AInstanceName)
		{
			Installer LInstaller = PrepareInstaller(AInstanceName);
			LInstaller.Uninstall(null);
		}
		
		public static string GetServiceAutoStartRegKeyName(string AInstanceName)
		{
			return String.Format(@"System\CurrentControlSet\Services\{0}", GetServiceName(AInstanceName));
		}
		
		public static bool GetServiceAutoStart(string AInstanceName)
		{
			Microsoft.Win32.RegistryKey LRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(GetServiceAutoStartRegKeyName(AInstanceName), true);

			// if we didn't find this reg key, the service isn't installed
			if (LRegKey == null)
				return false;
				
			try
			{
				object LObject = LRegKey.GetValue(CServiceStartModeRegName);
				int LServiceStart = (int)(LObject != null ? LObject : CServiceManualStart);
				return LServiceStart == CServiceAutoStart;
			}
			finally
			{
				LRegKey.Close();
			}
		}
		
		public static void SetServiceAutoStart(string AInstanceName, bool AAutoStart)
		{
			Microsoft.Win32.RegistryKey LRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(GetServiceAutoStartRegKeyName(AInstanceName), true);
			
			if (LRegKey == null)
				return;
				
			try
			{
				LRegKey.SetValue(CServiceAutoStartRegValueName, AAutoStart ? CServiceAutoStart : CServiceManualStart);
			}
			finally
			{
				LRegKey.Close();
			}
		}
		
		public static ServiceStatus GetServiceStatus(string AInstanceName)
		{
			try
			{
				ServiceController LServiceController = new ServiceController(GetServiceName(AInstanceName));

				switch (LServiceController.Status)
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
