/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class ListenerService : IListenerService
	{
		public string[] EnumerateInstances()
		{
			try
			{
				InstanceConfiguration configuration = InstanceManager.LoadConfiguration();
				string[] result = new string[configuration.Instances.Count];
				for (int index = 0; index < configuration.Instances.Count; index++)
					result[index] = configuration.Instances[index].Name;
				return result;
			}
			catch (Exception exception)
			{
				throw new FaultException<ListenerFault>(ListenerFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		private string GetInstanceURI(string hostName, string instanceName, bool useNative)
		{
			try
			{
				ServerConfiguration server = InstanceManager.LoadConfiguration().Instances[instanceName];
					
				if (useNative)
					return DataphorServiceUtility.BuildNativeInstanceURI(hostName, server.PortNumber, server.Name);
				
				return DataphorServiceUtility.BuildInstanceURI(hostName, server.PortNumber, server.Name);
			}
			catch (Exception exception)
			{
				throw new FaultException<ListenerFault>(ListenerFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public string GetInstanceURI(string hostName, string instanceName)
		{
			return GetInstanceURI(hostName, instanceName, false);
		}
		
		public string GetNativeInstanceURI(string hostName, string instanceName)
		{
			return GetInstanceURI(hostName, instanceName, true);
		}
	}
}
