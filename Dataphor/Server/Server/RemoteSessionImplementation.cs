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
		
		private NativeSessionInfo GetServerLinkNativeSessionInfo()
		{
			NativeSessionInfo LNativeSessionInfo = null;
			
			if (ServerLink.MetaData != null)
			{
				Tag LTag;
				
				LTag = ServerLink.MetaData.Tags.GetTag("DefaultIsolationLevel");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.DefaultIsolationLevel = (System.Data.IsolationLevel)Enum.Parse(typeof(System.Data.IsolationLevel), LTag.Value);
				}
					
				LTag = ServerLink.MetaData.Tags.GetTag("DefaultLibraryName");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.DefaultLibraryName = LTag.Value;
				}
					
				LTag = ServerLink.MetaData.Tags.GetTag("DefaultMaxCallDepth");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.DefaultMaxCallDepth = Convert.ToInt32(LTag.Value);
				}
					
				LTag = ServerLink.MetaData.Tags.GetTag("DefaultMaxStackDepth");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.DefaultMaxStackDepth = Convert.ToInt32(LTag.Value);
				}

				LTag = ServerLink.MetaData.Tags.GetTag("DefaultUseDTC");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.DefaultUseDTC = Convert.ToBoolean(LTag.Value);
				}

				LTag = ServerLink.MetaData.Tags.GetTag("DefaultUseImplicitTransactions");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.DefaultUseImplicitTransactions = Convert.ToBoolean(LTag.Value);
				}
				
				LTag = ServerLink.MetaData.Tags.GetTag("ShouldEmitIL");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.ShouldEmitIL = Convert.ToBoolean(LTag.Value);
				}
				
				LTag = ServerLink.MetaData.Tags.GetTag("UsePlanCache");
				if (LTag != Tag.None)
				{
					if (LNativeSessionInfo == null) LNativeSessionInfo = new NativeSessionInfo();
					LNativeSessionInfo.UsePlanCache = Convert.ToBoolean(LTag.Value);
				}
			}

			return LNativeSessionInfo;
		}
		
		public NativeSessionInfo GetDefaultNativeSessionInfo()
		{
			NativeSessionInfo LSessionInfo = GetServerLinkNativeSessionInfo();
			if (LSessionInfo != null)
				return LSessionInfo;
				
			if (ServerLink.UseSessionInfo)
				return GetNativeSessionInfoFromSessionInfo(ServerProcess.ServerSession.SessionInfo, ServerProcess.ProcessInfo);
				
			return GetNativeSessionInfoFromProcess(ServerProcess);
		}
		
		private NativeSessionInfo GetNativeSessionInfo()
		{
			NativeSessionInfo LNativeSessionInfo = GetDefaultNativeSessionInfo();
			
			// Determine credentials
			Schema.ServerLinkUser LLinkUser = ServerLink.GetUser(ServerProcess.ServerSession.User.ID);
			LNativeSessionInfo.HostName = ServerProcess.ServerSession.SessionInfo.HostName;
			LNativeSessionInfo.UserID = LLinkUser.ServerLinkUserID;
			LNativeSessionInfo.Password = Schema.SecurityUtility.DecryptPassword(LLinkUser.ServerLinkPassword);
			return LNativeSessionInfo;
		}
		
		private NativeCLISession FNativeCLISession;
		
		public override int TransactionCount
		{
			get { return FNativeCLISession.GetTransactionCount(); }
		}
		
		public override void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			FNativeCLISession.BeginTransaction(NativeCLIUtility.IsolationLevelToSystemDataIsolationLevel(AIsolationLevel));
		}
		
		public override void PrepareTransaction()
		{
			FNativeCLISession.PrepareTransaction();
		}
		
		public override void CommitTransaction()
		{
			FNativeCLISession.CommitTransaction();
		}
		
		public override void RollbackTransaction()
		{
			FNativeCLISession.RollbackTransaction();
		}
		
		public override void Execute(string AStatement, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(ServerProcess, AParams);
			FNativeCLISession.Execute(AStatement, LParams);
			NativeMarshal.SetDataOutputParams(ServerProcess, AParams, LParams);
		}
		
		public override DataValue Evaluate(string AExpression, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(ServerProcess, AParams);
			NativeResult LResult = FNativeCLISession.Execute(AExpression, LParams);
			NativeMarshal.SetDataOutputParams(ServerProcess, AParams, LResult.Params);
			return NativeMarshal.NativeValueToDataValue(ServerProcess, LResult.Value);
		}
		
		public override Schema.TableVar PrepareTableVar(Plan APlan, string AExpression, DataParams AParams)
		{
			NativeParam[] LParams = NativeMarshal.DataParamsToNativeParams(ServerProcess, AParams);
			NativeResult LResult = FNativeCLISession.Execute(AExpression, LParams, NativeExecutionOptions.SchemaOnly);
			if (LResult.Value is NativeTableValue)
				return NativeMarshal.NativeTableToTableVar(APlan, (NativeTableValue)LResult.Value);
			throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
		}
    }
}
