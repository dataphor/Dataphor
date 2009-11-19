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
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
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
			try
			{
				return FNativeServer.StartSession(ASessionInfo);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void StopSession(NativeSessionHandle ASessionHandle)
		{
			try
			{
				FNativeServer.StopSession(ASessionHandle);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void BeginTransaction(NativeSessionHandle ASessionHandle, NativeIsolationLevel AIsolationLevel)
		{
			try
			{
				FNativeServer.BeginTransaction(ASessionHandle, AIsolationLevel);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void PrepareTransaction(NativeSessionHandle ASessionHandle)
		{
			try
			{
				FNativeServer.PrepareTransaction(ASessionHandle);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void CommitTransaction(NativeSessionHandle ASessionHandle)
		{
			try
			{
				FNativeServer.CommitTransaction(ASessionHandle);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void RollbackTransaction(NativeSessionHandle ASessionHandle)
		{
			try
			{
				FNativeServer.RollbackTransaction(ASessionHandle);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public int GetTransactionCount(NativeSessionHandle ASessionHandle)
		{
			try
			{
				return FNativeServer.GetTransactionCount(ASessionHandle);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public NativeResult ExecuteStatement(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			try
			{
				return FNativeServer.Execute(ASessionInfo, AStatement, AParams, AOptions);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public NativeResult[] ExecuteStatements(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations)
		{
			try
			{
				return FNativeServer.Execute(ASessionInfo, AOperations);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public NativeResult SessionExecuteStatement(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			try
			{
				return FNativeServer.Execute(ASessionHandle, AStatement, AParams, AOptions);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public NativeResult[] SessionExecuteStatements(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations)
		{
			try
			{
				return FNativeServer.Execute(ASessionHandle, AOperations);
			}
			catch (NativeCLIException LException)
			{
				throw new FaultException<NativeCLIFault>(NativeCLIFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		#endregion
	}
}
