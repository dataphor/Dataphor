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

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>Installs the Dataphor DAE as a service on the target machine</summary>
	[RunInstaller(true)]
	public class ProjectInstaller : System.Configuration.Install.Installer
	{
		private ServiceProcessInstaller FServiceProcessInstaller;
		private ServiceInstaller FServiceInstaller;
		
		public ProjectInstaller()
		{
			FServiceProcessInstaller = new ServiceProcessInstaller();
			FServiceProcessInstaller.Account = ServiceAccount.LocalSystem;

			FServiceInstaller = new ServiceInstaller();
			FServiceInstaller.StartType = ServiceStartMode.Automatic;

			//Add installers to the collection.
			Installers.AddRange(new Installer[] {FServiceInstaller, FServiceProcessInstaller});
		}

		private void Prepare()
		{
			string LServiceName = Context.Parameters["ServiceName"];
			if (LServiceName == null)
				LServiceName = Alphora.Dataphor.DAE.Server.ServerService.GetServiceName();

			FServiceInstaller.DisplayName = LServiceName;
			FServiceInstaller.ServiceName = LServiceName;
		}

		public override void Install(IDictionary AStateSaver)
		{
			Prepare();
			base.Install(AStateSaver);
		}

		public override void Uninstall(IDictionary ASavedState)
		{
			Prepare();
			base.Uninstall(ASavedState);
		}
	}
}
