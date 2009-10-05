/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Collections;
using System.Configuration;
using System.ServiceModel.Web;

using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Service
{
	public class ListenerServiceHost : IDisposable
	{
		public ListenerServiceHost()
		{
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("listener");
			
			// Set shouldListen=false to turn off the listener for a process
			if ((LSettings != null) && LSettings.Contains("shouldListen") && !Convert.ToBoolean(LSettings["shouldListen"]))
				return;
				
			FListenerHost = new ServiceHost(typeof(ListenerService));
			
			FListenerHost.AddServiceEndpoint
			(
				typeof(IListenerService), 
				DataphorServiceUtility.GetBinding(), 
				DataphorServiceUtility.BuildListenerURI(Environment.MachineName)
			);
			
			try
			{
				FListenerHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
			}
			
			FCrossDomainServiceHost = new CrossDomainServiceHost(Environment.MachineName, DataphorServiceUtility.CDefaultListenerPortNumber);
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (FCrossDomainServiceHost != null)
			{
				FCrossDomainServiceHost.Dispose();
				FCrossDomainServiceHost = null;
			}
			
			if (FListenerHost != null)
			{
				if (FListenerHost.State == CommunicationState.Opened)
					FListenerHost.Close();
				FListenerHost = null;
			}
		}

		#endregion

		private ServiceHost FListenerHost;
		private CrossDomainServiceHost FCrossDomainServiceHost;
		
	}
}
