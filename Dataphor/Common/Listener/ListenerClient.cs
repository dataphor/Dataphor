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
		public ListenerClient(string hostName, int overridePortNumber, ConnectionSecurityMode securityMode) : base(DataphorServiceUtility.BuildListenerURI(hostName, overridePortNumber, securityMode)) 
		{ 
			_hostName = hostName;
		}
		
		private string _hostName;
		public string HostName { get { return _hostName; } }
		
		public string[] EnumerateInstances()
		{
			try
			{
				IAsyncResult result = GetInterface().BeginEnumerateInstances(null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndEnumerateInstances(result);
			}
			catch (FaultException<ListenerFault> exception)
			{
				throw ListenerFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public string GetInstanceURI(string instanceName, ConnectionSecurityMode securityMode)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginGetInstanceURI(_hostName, instanceName, securityMode, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndGetInstanceURI(result);
			}
			catch (FaultException<ListenerFault> exception)
			{
				throw ListenerFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public string GetNativeInstanceURI(string instanceName, ConnectionSecurityMode securityMode)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginGetNativeInstanceURI(_hostName, instanceName, securityMode, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndGetNativeInstanceURI(result);
			}
			catch (FaultException<ListenerFault> exception)
			{
				throw ListenerFaultUtility.FaultToException(exception.Detail);
			}
		}
	}
}
