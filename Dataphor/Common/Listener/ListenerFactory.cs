/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Listener
{
	public static class ListenerFactory
	{
		public static string[] EnumerateInstances(string hostName)
		{
			return EnumerateInstances(hostName, 0, ConnectionSecurityMode.None);
		}
		
		public static string[] EnumerateInstances(string hostName, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode)
		{
			using (ListenerClient client = new ListenerClient(hostName, overrideListenerPortNumber, listenerSecurityMode))
			{
				return client.EnumerateInstances();
			}
		}
		
		public static string GetInstanceURI(string hostName, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode, string instanceName, ConnectionSecurityMode securityMode)
		{
			return GetInstanceURI(hostName, overrideListenerPortNumber, listenerSecurityMode, instanceName, securityMode, false);
		}
		
		public static string GetInstanceURI(string hostName, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode, string instanceName, ConnectionSecurityMode securityMode, bool useNative)
		{
			using (ListenerClient client = new ListenerClient(hostName, overrideListenerPortNumber, listenerSecurityMode))
			{
				if (useNative)
					return client.GetNativeInstanceURI(instanceName, securityMode);
				else
					return client.GetInstanceURI(instanceName, securityMode);
			}
		}
	}
}
