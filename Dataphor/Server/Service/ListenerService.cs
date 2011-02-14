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
		
		private string GetInstanceURI(string hostName, string instanceName, ConnectionSecurityMode securityMode, bool useNative)
		{
			try
			{
				ServerConfiguration server = InstanceManager.LoadConfiguration().Instances[instanceName];
					
				bool secure = false;
				switch (securityMode)
				{
					case ConnectionSecurityMode.Default : 
						secure = server.RequireSecureConnection;
					break;
					
					case ConnectionSecurityMode.None :
						if (server.RequireSecureConnection)
							throw new NotSupportedException("The URI cannot be determined because the instance does not support unsecured connections.");
					break;

					case ConnectionSecurityMode.Transport :
						secure = true;
					break;
				}
					
				if (useNative)
					return DataphorServiceUtility.BuildNativeInstanceURI(hostName, server.PortNumber, secure, server.Name);
				
				return DataphorServiceUtility.BuildInstanceURI(hostName, server.PortNumber, secure, server.Name);
			}
			catch (Exception exception)
			{
				throw new FaultException<ListenerFault>(ListenerFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public string GetInstanceURI(string hostName, string instanceName, ConnectionSecurityMode securityMode)
		{
			return GetInstanceURI(hostName, instanceName, securityMode, false);
		}
		
		public string GetNativeInstanceURI(string hostName, string instanceName, ConnectionSecurityMode securityMode)
		{
			return GetInstanceURI(hostName, instanceName, securityMode, true);
		}
	}
}
