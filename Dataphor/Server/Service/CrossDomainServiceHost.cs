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
		public CrossDomainServiceHost(string AHostName, int APortNumber, int ASecurePortNumber, bool ARequireSecureConnection)
		{
			List<Uri> LBaseAddresses = new List<Uri>();
			LBaseAddresses.Add(new Uri(DataphorServiceUtility.BuildCrossDomainServiceURI(AHostName, ASecurePortNumber, true)));
			
			if (!ARequireSecureConnection)
				LBaseAddresses.Add(new Uri(DataphorServiceUtility.BuildCrossDomainServiceURI(AHostName, APortNumber, false)));
				
			FServiceHost = new WebServiceHost(typeof(CrossDomainService), LBaseAddresses.ToArray());
			
			FServiceHost.AddServiceEndpoint
			(
				typeof(ICrossDomainService),
				new WebHttpBinding(WebHttpSecurityMode.Transport),
				""
			);
		
			if (!ARequireSecureConnection)
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
				// An error indicates the service could not be started because there is already a cross domain service running in another process
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
