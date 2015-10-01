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
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteSession    
    public class RemoteSessionImplementation : RemoteSession
    {
		public RemoteSessionImplementation(ServerProcess process, Schema.ServerLink serverLink) : base(process, serverLink)
		{
			_nativeCLISession = 
				new NativeCLISession
				(
					serverLink.HostName, 
					serverLink.InstanceName, 
					serverLink.OverridePortNumber, 
					serverLink.UseSecureConnection
						? ConnectionSecurityMode.Transport 
						: ConnectionSecurityMode.None, 
					serverLink.OverrideListenerPortNumber, 
					serverLink.UseSecureListenerConnection
						? ConnectionSecurityMode.Transport 
						: ConnectionSecurityMode.None, 
					GetNativeSessionInfo()
				);
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_nativeCLISession != null)
			{
				_nativeCLISession.Dispose();
				_nativeCLISession = null;
			}
			
			base.Dispose(disposing);
		}
		
		public NativeSessionInfo GetNativeSessionInfoFromProcess(ServerProcess process)
		{
			NativeSessionInfo nativeSessionInfo = new NativeSessionInfo();
			nativeSessionInfo.DefaultIsolationLevel = NativeCLIUtility.IsolationLevelToNativeIsolationLevel(process.DefaultIsolationLevel);
			//LNativeSessionInfo.DefaultLibraryName = AProcess.ServerSession.CurrentLibrary.Name; // Still not sure if this makes sense...
			nativeSessionInfo.DefaultMaxCallDepth = process.ServerSession.SessionInfo.DefaultMaxCallDepth;
			nativeSessionInfo.DefaultMaxStackDepth = process.ServerSession.SessionInfo.DefaultMaxStackDepth;
			nativeSessionInfo.DefaultUseDTC = process.UseDTC;
			nativeSessionInfo.DefaultUseImplicitTransactions = process.UseImplicitTransactions;
			nativeSessionInfo.ShouldEmitIL = process.ServerSession.SessionInfo.ShouldEmitIL;
			nativeSessionInfo.UsePlanCache = process.ServerSession.SessionInfo.UsePlanCache;
			return nativeSessionInfo;
		}

		public NativeSessionInfo GetNativeSessionInfoFromSessionInfo(SessionInfo sessionInfo, ProcessInfo processInfo)
		{
			NativeSessionInfo nativeSessionInfo = new NativeSessionInfo();
			nativeSessionInfo.DefaultIsolationLevel = NativeCLIUtility.IsolationLevelToNativeIsolationLevel(processInfo == null ? sessionInfo.DefaultIsolationLevel : processInfo.DefaultIsolationLevel);
			//LNativeSessionInfo.DefaultLibraryName = ASessionInfo.DefaultLibraryName; // This doesn't make a lot of sense in the default scenario
			nativeSessionInfo.DefaultMaxCallDepth = sessionInfo.DefaultMaxCallDepth;
			nativeSessionInfo.DefaultMaxStackDepth = sessionInfo.DefaultMaxStackDepth;
			nativeSessionInfo.DefaultUseDTC = processInfo == null ? sessionInfo.DefaultUseDTC : processInfo.UseDTC;
			nativeSessionInfo.DefaultUseImplicitTransactions = processInfo == null ? sessionInfo.DefaultUseImplicitTransactions : processInfo.UseImplicitTransactions;
			nativeSessionInfo.ShouldEmitIL = sessionInfo.ShouldEmitIL;
			nativeSessionInfo.UsePlanCache = sessionInfo.UsePlanCache;
			return nativeSessionInfo;
		}
		
		private NativeSessionInfo GetServerLinkNativeSessionInfo()
		{
			NativeSessionInfo nativeSessionInfo = null;
			
			if (ServerLink.MetaData != null)
			{
				Tag tag;
				
				tag = ServerLink.GetMetaDataTag("DefaultIsolationLevel");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.DefaultIsolationLevel = (NativeIsolationLevel)Enum.Parse(typeof(NativeIsolationLevel), tag.Value);
				}
					
				tag = ServerLink.GetMetaDataTag("DefaultLibraryName");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.DefaultLibraryName = tag.Value;
				}
					
				tag = ServerLink.GetMetaDataTag("DefaultMaxCallDepth");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.DefaultMaxCallDepth = Convert.ToInt32(tag.Value);
				}
					
				tag = ServerLink.GetMetaDataTag("DefaultMaxStackDepth");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.DefaultMaxStackDepth = Convert.ToInt32(tag.Value);
				}

				tag = ServerLink.GetMetaDataTag("DefaultUseDTC");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.DefaultUseDTC = Convert.ToBoolean(tag.Value);
				}

				tag = ServerLink.GetMetaDataTag("DefaultUseImplicitTransactions");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.DefaultUseImplicitTransactions = Convert.ToBoolean(tag.Value);
				}
				
				tag = ServerLink.GetMetaDataTag("ShouldEmitIL");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.ShouldEmitIL = Convert.ToBoolean(tag.Value);
				}
				
				tag = ServerLink.GetMetaDataTag("UsePlanCache");
				if (tag != Tag.None)
				{
					if (nativeSessionInfo == null) nativeSessionInfo = new NativeSessionInfo();
					nativeSessionInfo.UsePlanCache = Convert.ToBoolean(tag.Value);
				}
			}

			return nativeSessionInfo;
		}
		
		public NativeSessionInfo GetDefaultNativeSessionInfo()
		{
			NativeSessionInfo sessionInfo = GetServerLinkNativeSessionInfo();
			if (sessionInfo != null)
				return sessionInfo;
				
			if (ServerLink.UseSessionInfo)
				return GetNativeSessionInfoFromSessionInfo(ServerProcess.ServerSession.SessionInfo, ServerProcess.ProcessInfo);
				
			return GetNativeSessionInfoFromProcess(ServerProcess);
		}
		
		private NativeSessionInfo GetNativeSessionInfo()
		{
			NativeSessionInfo nativeSessionInfo = GetDefaultNativeSessionInfo();
			
			// Determine credentials
			Schema.ServerLinkUser linkUser = ServerLink.GetUser(ServerProcess.ServerSession.User.ID);
			nativeSessionInfo.HostName = ServerProcess.ServerSession.SessionInfo.HostName;
			nativeSessionInfo.UserID = linkUser.ServerLinkUserID;
			nativeSessionInfo.Password = Schema.SecurityUtility.DecryptPassword(linkUser.ServerLinkPassword);
			return nativeSessionInfo;
		}
		
		private NativeCLISession _nativeCLISession;
		
		public override int TransactionCount
		{
			get { return _nativeCLISession.GetTransactionCount(); }
		}
		
		public override void BeginTransaction(IsolationLevel isolationLevel)
		{
			_nativeCLISession.BeginTransaction(NativeCLIUtility.IsolationLevelToNativeIsolationLevel(isolationLevel));
		}
		
		public override void PrepareTransaction()
		{
			_nativeCLISession.PrepareTransaction();
		}
		
		public override void CommitTransaction()
		{
			_nativeCLISession.CommitTransaction();
		}
		
		public override void RollbackTransaction()
		{
			_nativeCLISession.RollbackTransaction();
		}
		
		public override void Execute(string statement, DataParams paramsValue)
		{
			NativeParam[] localParamsValue = NativeMarshal.DataParamsToNativeParams(ServerProcess, paramsValue);
			_nativeCLISession.Execute(statement, localParamsValue);
			NativeMarshal.SetDataOutputParams(ServerProcess, paramsValue, localParamsValue);
		}
		
		public override IDataValue Evaluate(string expression, DataParams paramsValue)
		{
			NativeParam[] localParamsValue = NativeMarshal.DataParamsToNativeParams(ServerProcess, paramsValue);
			NativeResult result = _nativeCLISession.Execute(expression, localParamsValue);
			NativeMarshal.SetDataOutputParams(ServerProcess, paramsValue, result.Params);
			return NativeMarshal.NativeValueToDataValue(ServerProcess, result.Value);
		}
		
		public override Schema.TableVar PrepareTableVar(Plan plan, string expression, DataParams paramsValue)
		{
			NativeParam[] localParamsValue = NativeMarshal.DataParamsToNativeParams(ServerProcess, paramsValue);
			NativeResult result = _nativeCLISession.Execute(expression, localParamsValue, NativeExecutionOptions.SchemaOnly);
			if (result.Value is NativeTableValue)
				return NativeMarshal.NativeTableToTableVar(plan, (NativeTableValue)result.Value);
			throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
		}
    }
}
