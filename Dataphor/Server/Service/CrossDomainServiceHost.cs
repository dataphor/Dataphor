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
		public CrossDomainServiceHost(string hostName, int portNumber, int securePortNumber, bool requireSecureConnection)
		{
			List<Uri> baseAddresses = new List<Uri>();
			baseAddresses.Add(new Uri(DataphorServiceUtility.BuildCrossDomainServiceURI(hostName, securePortNumber, true)));
			
			if (!requireSecureConnection)
				baseAddresses.Add(new Uri(DataphorServiceUtility.BuildCrossDomainServiceURI(hostName, portNumber, false)));
				
			_serviceHost = new WebServiceHost(typeof(CrossDomainService), baseAddresses.ToArray());
			
			_serviceHost.AddServiceEndpoint
			(
				typeof(ICrossDomainService),
				new WebHttpBinding(WebHttpSecurityMode.Transport),
				""
			);
		
			if (!requireSecureConnection)
				_serviceHost.AddServiceEndpoint
				(
					typeof(ICrossDomainService), 
					new WebHttpBinding(),
					""
				);
				
			try
			{
				_serviceHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a cross domain service running in another process
			}
		}
		
		#region IDisposable Members

		public void Dispose()
		{
			if (_serviceHost != null)
			{
				if (_serviceHost.State == CommunicationState.Opened)
					_serviceHost.Close();
				_serviceHost = null;
			}
		}

		#endregion

		private ServiceHost _serviceHost;
	}
}
