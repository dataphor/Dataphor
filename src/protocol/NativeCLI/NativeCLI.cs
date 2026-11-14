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
	public abstract class NativeCLIClient : DataphorServiceClient<IClientNativeCLIService>
	{
		public const string DefaultInstanceName = "Dataphor";
		
		public NativeCLIClient(string hostName) : this(hostName, DefaultInstanceName, 0, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default) { }
		public NativeCLIClient(string hostName, string instanceName) : this(hostName, instanceName, 0, ConnectionSecurityMode.Default, 0, ConnectionSecurityMode.Default) { }
		public NativeCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode) : this(hostName, instanceName, overridePortNumber, ConnectionSecurityMode.Default, 0, securityMode) { }
		public NativeCLIClient(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode) : base(new Uri(GetNativeServerURI(hostName, instanceName, overridePortNumber, overrideListenerPortNumber)))
		{
			_hostName = hostName;
			_instanceName = instanceName;
			_overridePortNumber = overridePortNumber;
			_securityMode = securityMode;
			_overrideListenerPortNumber = overrideListenerPortNumber;
			_listenerSecurityMode = listenerSecurityMode;
		}

		public NativeCLIClient(string hostName, string instanceName, string endpointConfigurationName) : base(endpointConfigurationName)
		{
			_hostName = hostName;
			_instanceName = instanceName;
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginStartSession(sessionInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndStartSession(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginStopSession(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndStopSession(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginBeginTransaction(sessionHandle, isolationLevel, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndBeginTransaction(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginPrepareTransaction(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndPrepareTransaction(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginCommitTransaction(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndCommitTransaction(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginRollbackTransaction(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndRollbackTransaction(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginGetTransactionCount(sessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetTransactionCount(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginSessionExecuteStatement(sessionHandle, statement, paramsValue, options, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndSessionExecuteStatement(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginSessionExecuteStatements(sessionHandle, operations, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndSessionExecuteStatements(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginExecuteStatement(sessionInfo, statement, paramsValue, options, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndExecuteStatement(result);
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
				var channel = GetInterface();
				IAsyncResult result = channel.BeginExecuteStatements(sessionInfo, operations, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndExecuteStatements(result);
			}
			catch (FaultException<NativeCLIFault> exception)
			{
				throw NativeCLIFaultUtility.FaultToException(exception.Detail);
			}
		}
	}
}
