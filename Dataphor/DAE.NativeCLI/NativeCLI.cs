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
		public const string CDefaultInstanceName = "Dataphor";
		
		public NativeCLIClient(string AHostName) : this(AHostName, CDefaultInstanceName, 0) { }
		public NativeCLIClient(string AHostName, string AInstanceName) : this(AHostName, AInstanceName, 0) { }
		public NativeCLIClient(string AHostName, string AInstanceName, int AOverridePortNumber) : base(GetNativeServerURI(AHostName, AInstanceName, AOverridePortNumber))
		{
			FHostName = AHostName;
			FInstanceName = AInstanceName;
			FOverridePortNumber = AOverridePortNumber;
		}
		
		private string FHostName;
		public string HostName { get { return FHostName; } }
		
		private string FInstanceName;
		public string InstanceName { get { return FInstanceName; } }

		private int FOverridePortNumber;
		public int OverridePortNumber { get { return FOverridePortNumber; } }
		
		public static string GetNativeServerURI(string AHostName, string AInstanceName, int AOverridePortNumber)
		{
			if (AOverridePortNumber > 0)
				return DataphorServiceUtility.BuildNativeInstanceURI(AHostName, AOverridePortNumber, AInstanceName);
			else
				return ListenerFactory.GetInstanceURI(AHostName, AInstanceName, true);
		}
	}
	
	public class NativeSessionCLIClient : NativeCLIClient
	{
		public NativeSessionCLIClient(string AHostName) : base(AHostName) { }
		public NativeSessionCLIClient(string AHostName, string AInstanceName) : base(AHostName, AInstanceName) { }
		public NativeSessionCLIClient(string AHostName, string AInstanceName, int AOverridePortNumber) : base(AHostName, AInstanceName, AOverridePortNumber) { }
		
		public NativeSessionHandle StartSession(NativeSessionInfo ASessionInfo)
		{
			IAsyncResult LResult = GetInterface().BeginStartSession(ASessionInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndStartSession(LResult);
		}
		
		public void StopSession(NativeSessionHandle ASessionHandle)
		{
			IAsyncResult LResult = GetInterface().BeginStopSession(ASessionHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetInterface().EndStopSession(LResult);
		}
		
		public void BeginTransaction(NativeSessionHandle ASessionHandle, NativeIsolationLevel AIsolationLevel)
		{
			IAsyncResult LResult = GetInterface().BeginBeginTransaction(ASessionHandle, AIsolationLevel, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetInterface().EndBeginTransaction(LResult);
		}
		
		public void PrepareTransaction(NativeSessionHandle ASessionHandle)
		{
			IAsyncResult LResult = GetInterface().BeginPrepareTransaction(ASessionHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetInterface().EndPrepareTransaction(LResult);
		}
		
		public void CommitTransaction(NativeSessionHandle ASessionHandle)
		{
			IAsyncResult LResult = GetInterface().BeginCommitTransaction(ASessionHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetInterface().EndCommitTransaction(LResult);
		}
		
		public void RollbackTransaction(NativeSessionHandle ASessionHandle)
		{
			IAsyncResult LResult = GetInterface().BeginRollbackTransaction(ASessionHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetInterface().EndRollbackTransaction(LResult);
		}
		
		public int GetTransactionCount(NativeSessionHandle ASessionHandle)
		{
			IAsyncResult LResult = GetInterface().BeginGetTransactionCount(ASessionHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndGetTransactionCount(LResult);
		}
		
		public NativeResult Execute(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			IAsyncResult LResult = GetInterface().BeginSessionExecuteStatement(ASessionHandle, AStatement, AParams, AOptions, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndSessionExecuteStatement(LResult);
		}

		public NativeResult[] Execute(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations)
		{
			IAsyncResult LResult = GetInterface().BeginSessionExecuteStatements(ASessionHandle, AOperations, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndSessionExecuteStatements(LResult);
		}
	}
	
	public class NativeStatelessCLIClient : NativeCLIClient
	{
		public NativeStatelessCLIClient(string AHostName) : base(AHostName) { }
		public NativeStatelessCLIClient(string AHostName, string AInstanceName) : base(AHostName, AInstanceName) { }
		public NativeStatelessCLIClient(string AHostName, string AInstanceName, int AOverridePortNumber) : base(AHostName, AInstanceName, AOverridePortNumber) { }

		public NativeResult Execute(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			IAsyncResult LResult = GetInterface().BeginExecuteStatement(ASessionInfo, AStatement, AParams, AOptions, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndExecuteStatement(LResult);
		}
		
		public NativeResult[] Execute(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations)
		{
			IAsyncResult LResult = GetInterface().BeginExecuteStatements(ASessionInfo, AOperations, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndExecuteStatements(LResult);
		}
	}
}
