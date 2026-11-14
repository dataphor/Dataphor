/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>Installs the Dataphor DAE as a service on the target machine</summary>
	[RunInstaller(true)]
	public class ProjectInstaller : System.Configuration.Install.Installer
	{
		private ServiceProcessInstaller _serviceProcessInstaller;
		private ServiceInstaller _serviceInstaller;
		private string _instanceName;
		
		public ProjectInstaller()
		{
			_serviceProcessInstaller = new ServiceProcessInstaller();
			_serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

			_serviceInstaller = new ServiceInstaller();
			_serviceInstaller.StartType = ServiceStartMode.Automatic;

			//Add installers to the collection.
			Installers.AddRange(new Installer[] { _serviceInstaller, _serviceProcessInstaller });
		}

		private void Prepare()
		{
			_instanceName = Context.Parameters["InstanceName"];
			if (_instanceName == null)
				_instanceName = Server.Engine.DefaultServerName;
				
			string serviceName = ServiceUtility.GetServiceName(_instanceName);

			_serviceInstaller.DisplayName = serviceName;
			_serviceInstaller.ServiceName = serviceName;
			_serviceInstaller.Description = "Provides platform services for Dataphor applications.";
		}

		public override void Install(IDictionary stateSaver)
		{
			Prepare();
			base.Install(stateSaver);
			
			string serviceKey = String.Format("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\{0}", _serviceInstaller.ServiceName);
			string imagePath = Registry.GetValue(serviceKey, "ImagePath", null) as String;
			if (imagePath != null)
				Registry.SetValue(serviceKey, "ImagePath", String.Format("{0} -name \"{1}\"", imagePath, _instanceName));
			else
				throw new InvalidOperationException("Could not retrieve service ImagePath from registry.");
		}

		public override void Uninstall(IDictionary savedState)
		{
			Prepare();
			base.Uninstall(savedState);
		}
	}
}
