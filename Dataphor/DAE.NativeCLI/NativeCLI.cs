/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Data;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Listener;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public abstract class NativeCLI
	{
		public const string CDefaultInstanceName = "Dataphor";
		
		public NativeCLI(string AHostName) : this(AHostName, CDefaultInstanceName, 0) { }
		public NativeCLI(string AHostName, string AInstanceName) : this(AHostName, AInstanceName, 0) { }
		public NativeCLI(string AHostName, string AInstanceName, int AOverridePortNumber)
		{
			FHostName = AHostName;
			FInstanceName = AInstanceName;
			FOverridePortNumber = AOverridePortNumber;
			FServerURI = GetNativeServerURI(FHostName, FInstanceName, FOverridePortNumber);
		}
		
		private string FHostName;
		public string HostName { get { return FHostName; } }
		
		private string FInstanceName;
		public string InstanceName { get { return FInstanceName; } }

		private int FOverridePortNumber;
		public int OverridePortNumber { get { return FOverridePortNumber; } }
		
		private string FServerURI;
		public string ServerURI { get { return FServerURI; } }
		
		private INativeCLI FNativeInterface;
		
		private void ResetNativeInterface()
		{
			FNativeInterface = null;
		}
		
		protected INativeCLI GetNativeInterface()
		{
			if (FNativeInterface == null)
			{
				RemotingUtility.EnsureClientChannel();
				FNativeInterface = (INativeCLI)Activator.GetObject(typeof(INativeCLI), FServerURI);
			}
			
			return FNativeInterface;
		}

		public static string GetNativeServerURI(string AHostName, string AInstanceName, int AOverridePortNumber)
		{
			if (AOverridePortNumber > 0)
				return RemotingUtility.BuildInstanceURI(AHostName, AOverridePortNumber, AInstanceName, true);
			else
				return ListenerFactory.GetInstanceURI(AHostName, AInstanceName, true);
		}
	}
	
	public class NativeSessionCLI : NativeCLI
	{
		public NativeSessionCLI(string AHostName) : base(AHostName) { }
		public NativeSessionCLI(string AHostName, string AInstanceName) : base(AHostName, AInstanceName) { }
		public NativeSessionCLI(string AHostName, string AInstanceName, int AOverridePortNumber) : base(AHostName, AInstanceName, AOverridePortNumber) { }
		
		public NativeSessionHandle StartSession(NativeSessionInfo ASessionInfo)
		{
			return GetNativeInterface().StartSession(ASessionInfo);
		}
		
		public void StopSession(NativeSessionHandle ASessionHandle)
		{
			GetNativeInterface().StopSession(ASessionHandle);
		}
		
		public void BeginTransaction(NativeSessionHandle ASessionHandle, IsolationLevel AIsolationLevel)
		{
			GetNativeInterface().BeginTransaction(ASessionHandle, AIsolationLevel);
		}
		
		public void PrepareTransaction(NativeSessionHandle ASessionHandle)
		{
			GetNativeInterface().PrepareTransaction(ASessionHandle);
		}
		
		public void CommitTransaction(NativeSessionHandle ASessionHandle)
		{
			GetNativeInterface().CommitTransaction(ASessionHandle);
		}
		
		public void RollbackTransaction(NativeSessionHandle ASessionHandle)
		{
			GetNativeInterface().RollbackTransaction(ASessionHandle);
		}
		
		public int GetTransactionCount(NativeSessionHandle ASessionHandle)
		{
			return GetNativeInterface().GetTransactionCount(ASessionHandle);
		}
		
		public NativeResult Execute(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			return GetNativeInterface().Execute(ASessionHandle, AStatement, AParams, AOptions);
		}

		public NativeResult[] Execute(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations)
		{
			return GetNativeInterface().Execute(ASessionHandle, AOperations);
		}
	}
	
	public class NativeStatelessCLI : NativeCLI
	{
		public NativeStatelessCLI(string AHostName) : base(AHostName) { }
		public NativeStatelessCLI(string AHostName, string AInstanceName) : base(AHostName, AInstanceName) { }
		public NativeStatelessCLI(string AHostName, string AInstanceName, int AOverridePortNumber) : base(AHostName, AInstanceName, AOverridePortNumber) { }

		public NativeResult Execute(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions)
		{
			return GetNativeInterface().Execute(ASessionInfo, AStatement, AParams, AOptions);
		}
		
		public NativeResult[] Execute(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations)
		{
			return GetNativeInterface().Execute(ASessionInfo, AOperations);
		}
	}
}
