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
		public static string[] EnumerateInstances(string AHostName)
		{
			return EnumerateInstances(AHostName, 0, ConnectionSecurityMode.None);
		}
		
		public static string[] EnumerateInstances(string AHostName, int AOverrideListenerPortNumber, ConnectionSecurityMode AListenerSecurityMode)
		{
			using (ListenerClient LClient = new ListenerClient(AHostName, AOverrideListenerPortNumber, AListenerSecurityMode))
			{
				return LClient.EnumerateInstances();
			}
		}
		
		public static string GetInstanceURI(string AHostName, int AOverrideListenerPortNumber, ConnectionSecurityMode AListenerSecurityMode, string AInstanceName, ConnectionSecurityMode ASecurityMode)
		{
			return GetInstanceURI(AHostName, AOverrideListenerPortNumber, AListenerSecurityMode, AInstanceName, ASecurityMode, false);
		}
		
		public static string GetInstanceURI(string AHostName, int AOverrideListenerPortNumber, ConnectionSecurityMode AListenerSecurityMode, string AInstanceName, ConnectionSecurityMode ASecurityMode, bool AUseNative)
		{
			using (ListenerClient LClient = new ListenerClient(AHostName, AOverrideListenerPortNumber, AListenerSecurityMode))
			{
				if (AUseNative)
					return LClient.GetNativeInstanceURI(AInstanceName, ASecurityMode);
				else
					return LClient.GetInstanceURI(AInstanceName, ASecurityMode);
			}
		}
	}
}
