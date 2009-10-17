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
		public ListenerClient(string AHostName, int AOverridePortNumber, ConnectionSecurityMode ASecurityMode) : base(DataphorServiceUtility.BuildListenerURI(AHostName, AOverridePortNumber, ASecurityMode)) 
		{ 
			FHostName = AHostName;
		}
		
		private string FHostName;
		public string HostName { get { return FHostName; } }
		
		public string[] EnumerateInstances()
		{
			try
			{
				IAsyncResult LResult = GetInterface().BeginEnumerateInstances(null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetInterface().EndEnumerateInstances(LResult);
			}
			catch (FaultException<ListenerFault> LException)
			{
				throw ListenerFaultUtility.FaultToException(LException.Detail);
			}
		}
		
		public string GetInstanceURI(string AInstanceName, ConnectionSecurityMode ASecurityMode)
		{
			try
			{
				IAsyncResult LResult = GetInterface().BeginGetInstanceURI(FHostName, AInstanceName, ASecurityMode, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetInterface().EndGetInstanceURI(LResult);
			}
			catch (FaultException<ListenerFault> LException)
			{
				throw ListenerFaultUtility.FaultToException(LException.Detail);
			}
		}
		
		public string GetNativeInstanceURI(string AInstanceName, ConnectionSecurityMode ASecurityMode)
		{
			try
			{
				IAsyncResult LResult = GetInterface().BeginGetNativeInstanceURI(FHostName, AInstanceName, ASecurityMode, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetInterface().EndGetNativeInstanceURI(LResult);
			}
			catch (FaultException<ListenerFault> LException)
			{
				throw ListenerFaultUtility.FaultToException(LException.Detail);
			}
		}
	}
}
