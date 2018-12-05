using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Service
{
	public class CustomServiceHost : ServiceHost
	{
		public CustomServiceHost(object singletonInstance, params Uri[] baseAddresses) : base(singletonInstance, baseAddresses) 
		{ 
		}

		protected override void ApplyConfiguration()
		{
			var dataphorService = SingletonInstance as DataphorService;
			if (dataphorService != null)
			{
				this.Description.ConfigurationName = dataphorService.GetServerName();
			}

			var cliService = SingletonInstance as NativeCLIService;
			if (cliService != null)
			{
				this.Description.ConfigurationName = String.Format("{0}.Native", cliService.NativeServer.Server.Name);
			}

			var listenerService = SingletonInstance as ListenerService;
			if (listenerService != null)
			{
				this.Description.ConfigurationName = "Listener";
			}

			base.ApplyConfiguration();
		}
	}
}
