/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public static class ListenerFactory
	{
		private static IListener GetListener(string AHostName)
		{
			RemotingUtility.EnsureClientChannel();
			IListener LListener = (IListener)Activator.GetObject(typeof(IListener), RemotingUtility.BuildListenerURI(AHostName));
			if (LListener == null)
				throw new ArgumentException(String.Format("Could not connect to listener on host \"{0}\".", AHostName));
			return LListener;
		}
		
		public static string[] EnumerateInstances(string AHostName)
		{
			return GetListener(AHostName).EnumerateInstances();
		}
		
		public static string GetInstanceURI(string AHostName, string AInstanceName)
		{
			return GetListener(AHostName).GetInstanceURI(AInstanceName);
		}
		
		public static string GetInstanceURI(string AHostName, string AInstanceName, bool AUseNative)
		{
			return GetListener(AHostName).GetInstanceURI(AInstanceName, AUseNative);
		}
	}
}
