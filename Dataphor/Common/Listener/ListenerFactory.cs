/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Listener
{
	public static class ListenerFactory
	{
		public static string[] EnumerateInstances(string AHostName)
		{
			using (ListenerClient LClient = new ListenerClient(AHostName))
			{
				return LClient.EnumerateInstances();
			}
		}
		
		public static string GetInstanceURI(string AHostName, string AInstanceName)
		{
			return GetInstanceURI(AHostName, AInstanceName, false);
		}
		
		public static string GetInstanceURI(string AHostName, string AInstanceName, bool AUseNative)
		{
			using (ListenerClient LClient = new ListenerClient(AHostName))
			{
				if (AUseNative)
					return LClient.GetNativeInstanceURI(AInstanceName);
				else
					return LClient.GetInstanceURI(AInstanceName);
			}
		}
	}
}
