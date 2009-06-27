/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server
{
	/// <summary>
	/// Provides a single-call remote servicing object for the Native CLI.
	/// </summary>
	/// <remarks>
	/// Rather than explicitly exposing the NativeServer as a remotable object,
	/// this class provides a remotable wrapper that routes Native CLI calls
	/// to the NativeServer established by the ServerHost.
	/// </remarks>
	public class NativeServerCLI : MarshalByRefObject, INativeCLI
	{
		private static NativeServer FNativeServer;
		
		public static bool HasNativeServer()
		{
			return FNativeServer != null;
		}
		
		public static NativeServer GetNativeServer()
		{
			if (FNativeServer == null)
				throw new NativeCLIException("A native server has not been established for this application domain.");
				
			return FNativeServer;
		}
		
		public static void SetNativeServer(NativeServer ANativeServer)
		{
			if ((FNativeServer != null) && (ANativeServer != null))
				throw new NativeCLIException("A native server has already been established for this application domain.");

			FNativeServer = ANativeServer;
		}

		#region INativeCLI Members

		public NativeSessionHandle StartSession(NativeSessionInfo ASessionInfo)
		{
			return GetNativeServer().StartSession(ASessionInfo);
		}

		public void StopSession(NativeSessionHandle ASessionHandle)
		{
			GetNativeServer().StopSession(ASessionHandle);
		}

		public void BeginTransaction(NativeSessionHandle ASessionHandle, System.Data.IsolationLevel AIsolationLevel)
		{
			GetNativeServer().BeginTransaction(ASessionHandle, AIsolationLevel);
		}

		public void PrepareTransaction(NativeSessionHandle ASessionHandle)
		{
			GetNativeServer().PrepareTransaction(ASessionHandle);
		}

		public void CommitTransaction(NativeSessionHandle ASessionHandle)
		{
			GetNativeServer().CommitTransaction(ASessionHandle);
		}

		public void RollbackTransaction(NativeSessionHandle ASessionHandle)
		{
			GetNativeServer().RollbackTransaction(ASessionHandle);
		}

		public int GetTransactionCount(NativeSessionHandle ASessionHandle)
		{
			return GetNativeServer().GetTransactionCount(ASessionHandle);
		}

		public NativeResult Execute(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams)
		{
			return GetNativeServer().Execute(ASessionInfo, AStatement, AParams);
		}

		public NativeResult[] Execute(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations)
		{
			return GetNativeServer().Execute(ASessionInfo, AOperations);
		}

		public NativeResult Execute(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams)
		{
			return GetNativeServer().Execute(ASessionHandle, AStatement, AParams);
		}

		public NativeResult[] Execute(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations)
		{
			return GetNativeServer().Execute(ASessionHandle, AOperations);
		}

		#endregion
	}
}
