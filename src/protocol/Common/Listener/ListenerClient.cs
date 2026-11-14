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
	public class ListenerClient : DataphorServiceClient<IClientListenerService>
	{
		public ListenerClient(string hostName, int overridePortNumber) : base(new Uri(DataphorServiceUtility.BuildListenerURI(hostName, overridePortNumber))) 
		{ 
			_hostName = hostName;
		}

		private string _hostName;
		public string HostName { get { return _hostName; } }
		
		public string[] EnumerateInstances()
		{
			try
			{
				var channel = GetInterface();
				IAsyncResult result = channel.BeginEnumerateInstances(null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndEnumerateInstances(result);
			}
			catch (FaultException<ListenerFault> exception)
			{
				throw ListenerFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public string GetInstanceURI(string instanceName)
		{
			try
			{
				var channel = GetInterface();
				IAsyncResult result = channel.BeginGetInstanceURI(_hostName, instanceName, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetInstanceURI(result);
			}
			catch (FaultException<ListenerFault> exception)
			{
				throw ListenerFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public string GetNativeInstanceURI(string instanceName)
		{
			try
			{
				var channel = GetInterface();
				IAsyncResult result = channel.BeginGetNativeInstanceURI(_hostName, instanceName, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetNativeInstanceURI(result);
			}
			catch (FaultException<ListenerFault> exception)
			{
				throw ListenerFaultUtility.FaultToException(exception.Detail);
			}
		}
	}
}
