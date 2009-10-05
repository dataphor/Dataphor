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
	public class CrossDomainServiceHost : IDisposable
	{
		public CrossDomainServiceHost(string AHostName, int APortNumber)
		{
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("crossDomainService");
			
			// Set enableCrossDomainService=false to turn off the cross domain service for a process
			if ((LSettings != null) && LSettings.Contains("enableCrossDomainService") && !Convert.ToBoolean(LSettings["enableCrossDomainService"]))
				return;
				
			FServiceHost = new WebServiceHost(typeof(CrossDomainService), new Uri(DataphorServiceUtility.BuildCrossDomainServiceURI(AHostName, APortNumber)));
			
			FServiceHost.AddServiceEndpoint
			(
				typeof(ICrossDomainService), 
				new WebHttpBinding(),
				""
			);
			
			try
			{
				FServiceHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
			}
		}
		
		#region IDisposable Members

		public void Dispose()
		{
			if (FServiceHost != null)
			{
				if (FServiceHost.State == CommunicationState.Opened)
					FServiceHost.Close();
				FServiceHost = null;
			}
		}

		#endregion

		private ServiceHost FServiceHost;
	}
}
