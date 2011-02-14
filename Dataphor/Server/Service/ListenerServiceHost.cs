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
		public ListenerServiceHost(int overridePortNumber, int overrideSecurePortNumber, bool requireSecureConnection, bool useCrossDomainService)
		{
			int listenerPort = overridePortNumber == 0 ? DataphorServiceUtility.DefaultListenerPortNumber : overridePortNumber;
			int secureListenerPort = overrideSecurePortNumber == 0 ? DataphorServiceUtility.DefaultSecureListenerPortNumber : overrideSecurePortNumber;
				
			_listenerHost = new ServiceHost(typeof(ListenerService));
			
			if (!requireSecureConnection)
				_listenerHost.AddServiceEndpoint
				(
					typeof(IListenerService), 
					DataphorServiceUtility.GetBinding(false), 
					DataphorServiceUtility.BuildListenerURI(Environment.MachineName, listenerPort, false)
				);
				
			_listenerHost.AddServiceEndpoint
			(
				typeof(IListenerService), 
				DataphorServiceUtility.GetBinding(true), 
				DataphorServiceUtility.BuildListenerURI(Environment.MachineName, secureListenerPort, true)
			);

			try
			{
				_listenerHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
			}
			
			if (useCrossDomainService)
				_crossDomainServiceHost = new CrossDomainServiceHost(Environment.MachineName, listenerPort, secureListenerPort, requireSecureConnection);
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (_crossDomainServiceHost != null)
			{
				_crossDomainServiceHost.Dispose();
				_crossDomainServiceHost = null;
			}
			
			if (_listenerHost != null)
			{
				if (_listenerHost.State == CommunicationState.Opened)
					_listenerHost.Close();
				_listenerHost = null;
			}
		}

		#endregion

		private ServiceHost _listenerHost;
		private CrossDomainServiceHost _crossDomainServiceHost;
		
	}
}
