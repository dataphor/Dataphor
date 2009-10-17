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
		public ListenerServiceHost(int AOverridePortNumber, int AOverrideSecurePortNumber, bool ARequireSecureConnection, bool AUseCrossDomainService)
		{
			int LListenerPort = AOverridePortNumber == 0 ? DataphorServiceUtility.CDefaultListenerPortNumber : AOverridePortNumber;
			int LSecureListenerPort = AOverrideSecurePortNumber == 0 ? DataphorServiceUtility.CDefaultSecureListenerPortNumber : AOverrideSecurePortNumber;
				
			FListenerHost = new ServiceHost(typeof(ListenerService));
			
			if (!ARequireSecureConnection)
				FListenerHost.AddServiceEndpoint
				(
					typeof(IListenerService), 
					DataphorServiceUtility.GetBinding(false), 
					DataphorServiceUtility.BuildListenerURI(Environment.MachineName, LListenerPort, false)
				);
				
			FListenerHost.AddServiceEndpoint
			(
				typeof(IListenerService), 
				DataphorServiceUtility.GetBinding(true), 
				DataphorServiceUtility.BuildListenerURI(Environment.MachineName, LSecureListenerPort, true)
			);

			try
			{
				FListenerHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
			}
			
			if (AUseCrossDomainService)
				FCrossDomainServiceHost = new CrossDomainServiceHost(Environment.MachineName, LListenerPort, LSecureListenerPort, ARequireSecureConnection);
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
