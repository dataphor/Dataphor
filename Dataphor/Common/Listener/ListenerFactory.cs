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
			return EnumerateInstances(hostName, 0);
		}
		
		public static string[] EnumerateInstances(string hostName, int overrideListenerPortNumber)
		{
			using (ListenerClient client = new ListenerClient(hostName, overrideListenerPortNumber))
			{
				return client.EnumerateInstances();
			}
		}
		
		public static string GetInstanceURI(string hostName, int overrideListenerPortNumber, string instanceName)
		{
			return GetInstanceURI(hostName, overrideListenerPortNumber, instanceName, false);
		}
		
		public static string GetInstanceURI(string hostName, int overrideListenerPortNumber, string instanceName, bool useNative)
		{
			using (ListenerClient client = new ListenerClient(hostName, overrideListenerPortNumber))
			{
				if (useNative)
					return client.GetNativeInstanceURI(instanceName);
				else
					return client.GetInstanceURI(instanceName);
			}
		}
	}
}
