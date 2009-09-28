/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Listener
{
	public class ListenerClient : ServiceClient<IClientListenerService>
	{
		public ListenerClient(string AHostName) : base(DataphorServiceUtility.BuildListenerURI(AHostName)) { }
		
		public string[] EnumerateInstances()
		{
			IAsyncResult LResult = GetInterface().BeginEnumerateInstances(null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndEnumerateInstances(LResult);
		}
		
		public string GetInstanceURI(string AInstanceName)
		{
			IAsyncResult LResult = GetInterface().BeginGetInstanceURI(AInstanceName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndGetInstanceURI(LResult);
		}
		
		public string GetNativeInstanceURI(string AInstanceName)
		{
			IAsyncResult LResult = GetInterface().BeginGetNativeInstanceURI(AInstanceName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndGetNativeInstanceURI(LResult);
		}
	}
}
