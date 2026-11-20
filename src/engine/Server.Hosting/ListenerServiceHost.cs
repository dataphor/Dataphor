/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

// NOTE: nothing compiles this folder: there is no project file here yet, and this constructor still takes
// the two parameters of settings that no longer exist (useCrossDomainService, useServiceConfiguration).
// protocol-corewcf-host-prepare moved the file here unchanged; protocol-corewcf-host creates
// Server.Hosting.csproj and rewrites the class onto CoreWCF, and deletes this note.

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
		public ListenerServiceHost(int overridePortNumber, bool requireSecureConnection, bool useCrossDomainService, bool useServiceConfiguration)
		{
			int listenerPort = overridePortNumber == 0 ? DataphorServiceUtility.DefaultListenerPortNumber : overridePortNumber;
				
			_listenerHost = useServiceConfiguration ? new CustomServiceHost(typeof(ListenerService)) : new ServiceHost(typeof(ListenerService));

			if (!useServiceConfiguration)
			{
				_listenerHost.AddServiceEndpoint
				(
					typeof(IListenerService), 
					DataphorServiceUtility.GetBinding(), 
					DataphorServiceUtility.BuildListenerURI(Environment.MachineName, listenerPort)
				);	 
			}

			try
			{
				_listenerHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
			}	 			
		}

		#region IDisposable Members

		public void Dispose()
		{				
			if (_listenerHost != null)
			{
				if (_listenerHost.State == CommunicationState.Opened)
					_listenerHost.Close();
				_listenerHost = null;
			}
		}

		#endregion

		private ServiceHost _listenerHost;	   		
	}
}
