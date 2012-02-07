/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.Listener;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public abstract class NativeCLIClient : ServiceClient<IClientNativeCLIService>
	{
		public const string DefaultInstanceName = "Dataphor";
		
		public NativeCLIClient(string hostName) : this(hostName, DefaultInstanceName, 0, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default) { }
		public NativeCLIClient(string hostName, string instanceName) : this(hostName, instanceName, 0, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default) { }
		public NativeCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode) : this(hostName, instanceName, 0, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default) { }
		public NativeCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode) : base(GetNativeServerURI(hostName, instanceName, overridePortNumber, overrideListenerPortNumber))
		{
			_hostName = hostName;
			_instanceName = instanceName;
			_overridePortNumber = overridePortNumber;
			_securityMode = securityMode;
			_overrideListenerPortNumber = overrideListenerPortNumber;
			_listenerSecurityMode = listenerSecurityMode;
		}
		
		private string _hostName;
		public string HostName { get { return _hostName; } }
		
		private string _instanceName;
		public string InstanceName { get { return _instanceName; } }

		private int _overridePortNumber;
		public int OverridePortNumber { get { return _overridePortNumber; } }
		
		private ConnectionSecurityMode _securityMode;
		public ConnectionSecurityMode SecurityMode { get { return _securityMode; } }
		
		private int _overrideListenerPortNumber;
		public int OverrideListenerPortNumber { get { return _overrideListenerPortNumber; } }
		
		private ConnectionSecurityMode _listenerSecurityMode;
		public ConnectionSecurityMode ListenerSecurityMode { get { return _listenerSecurityMode; } }
		
		public static string GetNativeServerURI(string hostName, string instanceName, int overridePortNumber, int overrideListenerPortNumber)
		{
			if (overridePortNumber > 0)
				return DataphorServiceUtility.BuildNativeInstanceURI(hostName, overridePortNumber, instanceName);
			else
				return ListenerFactory.GetInstanceURI(hostName, overrideListenerPortNumber, instanceName, true);
		}
	}
	
	public class NativeSessionCLIClient : NativeCLIClient
	{
		public NativeSessionCLIClient(string hostName) : base(hostName) { }
		public NativeSessionCLIClient(string hostName, string instanceName) : base(hostName, instanceName) { }
		public NativeSessionCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode) : base(hostName, instanceName, overridePortNumber, securityMode) { }
		public NativeSessionCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode) : base(hostName, instanceName, overridePortNumber, securityMode, overrideListenerPortNumber, listenerSecurityMode) { }
		
		public NativeSessionHandle StartSession(NativeSessionInfo sessionInfo)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginStartSession(sessionInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndStartSession(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public void StopSession(NativeSessionHandle sessionHandle)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginStopSession(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetInterface().EndStopSession(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public void BeginTransaction(NativeSessionHandle sessionHandle, NativeIsolationLevel isolationLevel)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginBeginTransaction(sessionHandle, isolationLevel, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetInterface().EndBeginTransaction(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public void PrepareTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginPrepareTransaction(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetInterface().EndPrepareTransaction(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public void CommitTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginCommitTransaction(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetInterface().EndCommitTransaction(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public void RollbackTransaction(NativeSessionHandle sessionHandle)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginRollbackTransaction(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetInterface().EndRollbackTransaction(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public int GetTransactionCount(NativeSessionHandle sessionHandle)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginGetTransactionCount(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndGetTransactionCount(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public NativeResult Execute(NativeSessionHandle sessionHandle, string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginSessionExecuteStatement(sessionHandle, statement, paramsValue, options, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndSessionExecuteStatement(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}

		public NativeResult[] Execute(NativeSessionHandle sessionHandle, NativeExecuteOperation[] operations)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginSessionExecuteStatements(sessionHandle, operations, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndSessionExecuteStatements(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
	}
	
	public class NativeStatelessCLIClient : NativeCLIClient
	{
		public NativeStatelessCLIClient(string hostName) : base(hostName) { }
		public NativeStatelessCLIClient(string hostName, string instanceName) : base(hostName, instanceName) { }
		public NativeStatelessCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode) : base(hostName, instanceName, overridePortNumber, securityMode) { }
		public NativeStatelessCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode) : base(hostName, instanceName, overridePortNumber, securityMode, overrideListenerPortNumber, listenerSecurityMode) { }

		public NativeResult Execute(NativeSessionInfo sessionInfo, string statement, NativeParam[] paramsValue, NativeExecutionOptions options)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginExecuteStatement(sessionInfo, statement, paramsValue, options, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndExecuteStatement(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
		
		public NativeResult[] Execute(NativeSessionInfo sessionInfo, NativeExecuteOperation[] operations)
		{
			try
			{
				IAsyncResult result = GetInterface().BeginExecuteStatements(sessionInfo, operations, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetInterface().EndExecuteStatements(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
	}
}
