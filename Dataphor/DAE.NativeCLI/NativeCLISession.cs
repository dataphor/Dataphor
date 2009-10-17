/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public class NativeCLISession : IDisposable
	{
		public NativeCLISession(string AHostName, string AInstanceName, int AOverridePortNumber, ConnectionSecurityMode ASecurityMode, int AOverrideListenerPortNumber, ConnectionSecurityMode AListenerSecurityMode, NativeSessionInfo ANativeSessionInfo)
		{
			FNativeCLI = new NativeSessionCLIClient(AHostName, AInstanceName, AOverridePortNumber, ASecurityMode, AOverrideListenerPortNumber, AListenerSecurityMode);
			FNativeSessionInfo = ANativeSessionInfo;
			FSessionHandle = FNativeCLI.StartSession(ANativeSessionInfo);
		}
		
		private NativeSessionCLIClient FNativeCLI;
		
		public string HostName { get { return FNativeCLI.HostName; } }
		
		public string InstanceName { get { return FNativeCLI.InstanceName; } }

		public int OverridePortNumber { get { return FNativeCLI.OverridePortNumber; } }
		
		public ConnectionSecurityMode SecurityMode { get { return FNativeCLI.SecurityMode; } }
		
		public int OverrideListenerPortNumber { get { return FNativeCLI.OverrideListenerPortNumber; } }
		
		public ConnectionSecurityMode ListenerSecurityMode { get { return FNativeCLI.ListenerSecurityMode; } }
		
		private NativeSessionInfo FNativeSessionInfo;
		public NativeSessionInfo NativeSessionInfo { get { return FNativeSessionInfo; } }
		
		private NativeSessionHandle FSessionHandle;
		
		#region IDisposable Members

		public void Dispose()
		{
			if (FSessionHandle != null)
			{
				FNativeCLI.StopSession(FSessionHandle);
				FSessionHandle = null;
			}
		}

		#endregion
		
		public void BeginTransaction(NativeIsolationLevel AIsolationLevel)
		{
			FNativeCLI.BeginTransaction(FSessionHandle, AIsolationLevel);
		}
		
		public void PrepareTransaction()
		{
			FNativeCLI.PrepareTransaction(FSessionHandle);
		}
		
		public void CommitTransaction()
		{
			FNativeCLI.CommitTransaction(FSessionHandle);
		}
		
		public void RollbackTransaction()
		{
			FNativeCLI.RollbackTransaction(FSessionHandle);
		}
		
		public int GetTransactionCount()
		{
			return FNativeCLI.GetTransactionCount(FSessionHandle);
		}
		
		public NativeResult Execute(string AStatement, NativeParam[] AParams)
		{
			return FNativeCLI.Execute(FSessionHandle, AStatement, AParams, NativeExecutionOptions.Default);
		}
		
		public NativeResult Execute(string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			return FNativeCLI.Execute(FSessionHandle, AStatement, AParams, AOptions);
		}
		
		public NativeResult[] Execute(NativeExecuteOperation[] AOperations)
		{
			return FNativeCLI.Execute(FSessionHandle, AOperations);
		}
	}
}
