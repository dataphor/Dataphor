/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteSession    
    public class RemoteSessionImplementation : RemoteSession
    {
		public RemoteSessionImplementation(ServerProcess AProcess, Schema.ServerLink AServerLink) : base(AProcess, AServerLink)
		{
			FNativeCLISession = new NativeCLISession(AServerLink.HostName, AServerLink.InstanceName, AServerLink.OverridePortNumber, GetNativeSessionInfo());
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FNativeCLISession != null)
			{
				FNativeCLISession.Dispose();
				FNativeCLISession = null;
			}
			
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
			LNativeSessionInfo.DefaultMaxCallDepth = AProcess.ServerSession.SessionInfo.DefaultMaxCallDepth;
			LNativeSessionInfo.DefaultMaxStackDepth = AProcess.ServerSession.SessionInfo.DefaultMaxStackDepth;
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
			Schema.ServerLinkUser LLinkUser = FServerLink.GetUser(FServerProcess.ServerSession.User.ID);
			LNativeSessionInfo.HostName = FServerProcess.ServerSession.SessionInfo.HostName;
			LNativeSessionInfo.UserID = LLinkUser.ServerLinkUserID;
			LNativeSessionInfo.Password = Schema.SecurityUtility.DecryptPassword(LLinkUser.ServerLinkPassword);
			return LNativeSessionInfo;
		}
		
/*
 * Previously in Schema.ServerLink
		private NativeSessionInfo FDefaultNativeSessionInfo;
		public NativeSessionInfo DefaultNativeSessionInfo
		{
			get { return FDefaultNativeSessionInfo; }
		}
		
		private NativeSessionInfo EnsureDefaultNativeSessionInfo()
		{
			if (FDefaultNativeSessionInfo == null)
				FDefaultNativeSessionInfo = new NativeSessionInfo();
				
			return FDefaultNativeSessionInfo;
		}
		
		public void ResetServerLink()
		{
			FHostName = String.Empty;
			FInstanceName = String.Empty;
			FOverridePortNumber = 0;
			FUseSessionInfo = true;
			FDefaultNativeSessionInfo = null;
		}
		
		public void ApplyMetaData()
		{
			if (MetaData != null)
			{
				FHostName = MetaData.Tags.GetTagValue("HostName", "localhost");
				FInstanceName = MetaData.Tags.GetTagValue("InstanceName", Server.Server.CDefaultServerName);
				FOverridePortNumber = Convert.ToInt32(MetaData.Tags.GetTagValue("OverridePortNumber", "0"));
				FUseSessionInfo = Convert.ToBoolean(MetaData.Tags.GetTagValue("UseSessionInfo", "true"));
				
				Tag LTag;
				
				LTag = MetaData.Tags.GetTag("DefaultIsolationLevel");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultIsolationLevel = (System.Data.IsolationLevel)Enum.Parse(typeof(System.Data.IsolationLevel), LTag.Value);
					
				LTag = MetaData.Tags.GetTag("DefaultLibraryName");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultLibraryName = LTag.Value;
					
				LTag = MetaData.Tags.GetTag("DefaultMaxCallDepth");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultMaxCallDepth = Convert.ToInt32(LTag.Value);
					
				LTag = MetaData.Tags.GetTag("DefaultMaxStackDepth");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultMaxStackDepth = Convert.ToInt32(LTag.Value);

				LTag = MetaData.Tags.GetTag("DefaultUseDTC");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultUseDTC = Convert.ToBoolean(LTag.Value);

				LTag = MetaData.Tags.GetTag("DefaultUseImplicitTransactions");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().DefaultUseImplicitTransactions = Convert.ToBoolean(LTag.Value);
				
				LTag = MetaData.Tags.GetTag("ShouldEmitIL");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().ShouldEmitIL = Convert.ToBoolean(LTag.Value);
				
				LTag = MetaData.Tags.GetTag("UsePlanCache");
				if (LTag != Tag.None)
					EnsureDefaultNativeSessionInfo().UsePlanCache = Convert.ToBoolean(LTag.Value);
			}
		}
*/

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
		
		public DataValue Evaluate(string AExpression, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(FServerProcess, AParams);
			NativeResult LResult = FNativeCLISession.Execute(AExpression, LParams);
			NativeMarshal.SetDataOutputParams(FServerProcess, AParams, LResult.Params);
			return NativeMarshal.NativeValueToDataValue(FServerProcess, LResult.Value);
		}
		
		public Schema.TableVar PrepareTableVar(Plan APlan, string AExpression, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(FServerProcess, AParams);
			NativeResult LResult = FNativeCLISession.Execute(AExpression, LParams, NativeExecutionOptions.SchemaOnly);
			if (LResult.Value is NativeTableValue)
				return NativeMarshal.NativeTableToTableVar(APlan, (NativeTableValue)LResult.Value);
			throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
		}
    }
}
