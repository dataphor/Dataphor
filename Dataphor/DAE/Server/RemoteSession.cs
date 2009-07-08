/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteSession    
    public class RemoteSession : Disposable
    {
		public RemoteSession(ServerProcess AProcess, Schema.ServerLink AServerLink)
		{
			FServerProcess = AProcess;
			FServerLink = AServerLink;
			FNativeCLISession = new NativeCLISession(AServerLink.HostName, AServerLink.InstanceName, AServerLink.OverridePortNumber, GetNativeSessionInfo());
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FNativeCLISession != null)
			{
				FNativeCLISession.Dispose();
				FNativeCLISession = null;
			}
			
			FServerProcess = null;
			
			base.Dispose(ADisposing);
		}
		
		private ServerProcess FServerProcess;
		public ServerProcess ServerProcess { get { return FServerProcess; } }

		private Schema.ServerLink FServerLink;
		public Schema.ServerLink ServerLink { get { return FServerLink; } }
		
		public NativeSessionInfo GetNativeSessionInfoFromProcess(ServerProcess AProcess)
		{
			NativeSessionInfo LNativeSessionInfo = new NativeSessionInfo();
			LNativeSessionInfo.DefaultIsolationLevel = NativeCLIUtility.IsolationLevelToSystemDataIsolationLevel(AProcess.DefaultIsolationLevel);
			//LNativeSessionInfo.DefaultLibraryName = AProcess.ServerSession.CurrentLibrary.Name; // Still not sure if this makes sense...
			LNativeSessionInfo.DefaultMaxCallDepth = AProcess.Context.MaxCallDepth;
			LNativeSessionInfo.DefaultMaxStackDepth = AProcess.Context.MaxStackDepth;
			LNativeSessionInfo.DefaultUseDTC = AProcess.UseDTC;
			LNativeSessionInfo.DefaultUseImplicitTransactions = AProcess.UseImplicitTransactions;
			LNativeSessionInfo.ShouldEmitIL = AProcess.ServerSession.SessionInfo.ShouldEmitIL;
			LNativeSessionInfo.UsePlanCache = AProcess.ServerSession.SessionInfo.UsePlanCache;
			return LNativeSessionInfo;
		}

		public NativeSessionInfo GetNativeSessionInfoFromSessionInfo(SessionInfo ASessionInfo, ProcessInfo AProcessInfo)
		{
			NativeSessionInfo LNativeSessionInfo = new NativeSessionInfo();
			LNativeSessionInfo.DefaultIsolationLevel = NativeCLIUtility.IsolationLevelToSystemDataIsolationLevel(AProcessInfo == null ? ASessionInfo.DefaultIsolationLevel : AProcessInfo.DefaultIsolationLevel);
			//LNativeSessionInfo.DefaultLibraryName = ASessionInfo.DefaultLibraryName; // This doesn't make a lot of sense in the default scenario
			LNativeSessionInfo.DefaultMaxCallDepth = ASessionInfo.DefaultMaxCallDepth;
			LNativeSessionInfo.DefaultMaxStackDepth = ASessionInfo.DefaultMaxStackDepth;
			LNativeSessionInfo.DefaultUseDTC = AProcessInfo == null ? ASessionInfo.DefaultUseDTC : AProcessInfo.UseDTC;
			LNativeSessionInfo.DefaultUseImplicitTransactions = AProcessInfo == null ? ASessionInfo.DefaultUseImplicitTransactions : AProcessInfo.UseImplicitTransactions;
			LNativeSessionInfo.ShouldEmitIL = ASessionInfo.ShouldEmitIL;
			LNativeSessionInfo.UsePlanCache = ASessionInfo.UsePlanCache;
			return LNativeSessionInfo;
		}
		
		public NativeSessionInfo GetDefaultNativeSessionInfo()
		{
			if (FServerLink.DefaultNativeSessionInfo != null)
				return FServerLink.DefaultNativeSessionInfo.Copy();
				
			if (FServerLink.UseSessionInfo)
				return GetNativeSessionInfoFromSessionInfo(FServerProcess.ServerSession.SessionInfo, FServerProcess.ProcessInfo);
				
			return GetNativeSessionInfoFromProcess(FServerProcess);
		}
		
		private NativeSessionInfo GetNativeSessionInfo()
		{
			NativeSessionInfo LNativeSessionInfo = GetDefaultNativeSessionInfo();
			
			// Determine credentials
			Schema.ServerLinkUser LLinkUser = FServerLink.GetUser(FServerProcess.Plan.User.ID);
			LNativeSessionInfo.HostName = FServerProcess.ServerSession.SessionInfo.HostName;
			LNativeSessionInfo.UserID = LLinkUser.ServerLinkUserID;
			LNativeSessionInfo.Password = Schema.SecurityUtility.DecryptPassword(LLinkUser.ServerLinkPassword);
			return LNativeSessionInfo;
		}
		
		private NativeCLISession FNativeCLISession;
		
		public int TransactionCount
		{
			get { return FNativeCLISession.GetTransactionCount(); }
		}
		
		public bool InTransaction
		{
			get { return TransactionCount > 0; }
		}
		
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			FNativeCLISession.BeginTransaction(NativeCLIUtility.IsolationLevelToSystemDataIsolationLevel(AIsolationLevel));
		}
		
		public void PrepareTransaction()
		{
			FNativeCLISession.PrepareTransaction();
		}
		
		public void CommitTransaction()
		{
			FNativeCLISession.CommitTransaction();
		}
		
		public void RollbackTransaction()
		{
			FNativeCLISession.RollbackTransaction();
		}
		
		public void Execute(string AStatement, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(FServerProcess, AParams);
			FNativeCLISession.Execute(AStatement, LParams);
			NativeMarshal.SetDataOutputParams(FServerProcess, AParams, LParams);
		}
		
		public DataVar Evaluate(string AExpression, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(FServerProcess, AParams);
			NativeResult LResult = FNativeCLISession.Execute(AExpression, LParams);
			NativeMarshal.SetDataOutputParams(FServerProcess, AParams, LResult.Params);
			DataValue LDataValue = NativeMarshal.NativeValueToDataValue(FServerProcess, LResult.Value);
			return new DataVar(LDataValue.DataType, LDataValue);
		}
		
		public Schema.TableVar PrepareTableVar(string AExpression, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(FServerProcess, AParams);
			NativeResult LResult = FNativeCLISession.Execute(AExpression, LParams, NativeExecutionOptions.SchemaOnly);
			if (LResult.Value is NativeTableValue)
				return NativeMarshal.NativeTableToTableVar(FServerProcess, (NativeTableValue)LResult.Value);
			throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
		}
    }
    
	public class RemoteSessions : DisposableTypedList
	{
		public RemoteSessions() : base()
		{
			FItemType = typeof(RemoteSession);
			FItemsOwned = true;
		}
		
		public new RemoteSession this[int AIndex]
		{
			get { return (RemoteSession)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(Schema.ServerLink ALink)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].ServerLink.Equals(ALink))
					return LIndex;
			return -1;
		}
		
		public bool Contains(Schema.ServerLink ALink)
		{
			return IndexOf(ALink) >= 0;
		}
	}
}
