/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Contracts;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	/// <summary>
	/// Provides a single-call remote servicing object for the Native CLI.
	/// </summary>
	/// <remarks>
	/// Rather than explicitly exposing the NativeServer as a remotable object,
	/// this class provides a remotable wrapper that routes Native CLI calls
	/// to the NativeServer established by the ServerHost.
	/// </remarks>
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
	public class NativeCLIService : INativeCLIService
	{
		public NativeCLIService(NativeServer ANativeServer)
		{
			FNativeServer = ANativeServer;
		}
		
		private NativeServer FNativeServer;
		
		#region INativeCLI Members

		public NativeSessionHandle StartSession(NativeSessionInfo ASessionInfo)
		{
			return FNativeServer.StartSession(ASessionInfo);
		}

		public void StopSession(NativeSessionHandle ASessionHandle)
		{
			FNativeServer.StopSession(ASessionHandle);
		}

		public void BeginTransaction(NativeSessionHandle ASessionHandle, NativeIsolationLevel AIsolationLevel)
		{
			FNativeServer.BeginTransaction(ASessionHandle, AIsolationLevel);
		}

		public void PrepareTransaction(NativeSessionHandle ASessionHandle)
		{
			FNativeServer.PrepareTransaction(ASessionHandle);
		}

		public void CommitTransaction(NativeSessionHandle ASessionHandle)
		{
			FNativeServer.CommitTransaction(ASessionHandle);
		}

		public void RollbackTransaction(NativeSessionHandle ASessionHandle)
		{
			FNativeServer.RollbackTransaction(ASessionHandle);
		}

		public int GetTransactionCount(NativeSessionHandle ASessionHandle)
		{
			return FNativeServer.GetTransactionCount(ASessionHandle);
		}

		public NativeResult ExecuteStatement(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			return FNativeServer.Execute(ASessionInfo, AStatement, AParams, AOptions);
		}

		public NativeResult[] ExecuteStatements(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations)
		{
			return FNativeServer.Execute(ASessionInfo, AOperations);
		}

		public NativeResult SessionExecuteStatement(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			return FNativeServer.Execute(ASessionHandle, AStatement, AParams, AOptions);
		}

		public NativeResult[] SessionExecuteStatements(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations)
		{
			return FNativeServer.Execute(ASessionHandle, AOperations);
		}

		#endregion
	}
}
