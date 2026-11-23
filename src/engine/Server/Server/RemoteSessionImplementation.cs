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
			// Reported before the connection is made, so that a link which then fails to connect has still said what it
			// was asked for and did not do.
			ReportUnhonouredSecuritySettings(process, serverLink);

			_nativeCLISession = 
				new NativeCLISession
				(
					serverLink.HostName, 
					serverLink.InstanceName, 
					serverLink.OverridePortNumber, 
					// None rather than the link's tags: a client asking for Transport is refused outright, because the
					// connection this version builds carries no transport security, and breaking a running application to
					// report a setting that never worked is the wrong trade when the library is a customer artifact the
					// person running the query cannot edit. So the connection proceeds unsecured and the warning above is
					// how that is said. Decision D17 in docs/modernization-plan.md.
					ConnectionSecurityMode.None, 
					serverLink.OverrideListenerPortNumber, 
					ConnectionSecurityMode.None, 
					GetNativeSessionInfo()
				);
		}
		
		/// <summary>Writes one warning to the server's log when a server link's metadata asks for a secured connection, which this product cannot give it.</summary>
		/// <remarks>
		/// The tags <c>UseSecureConnection</c> and <c>UseSecureListenerConnection</c> are read into
		/// <see cref="Schema.ServerLink"/>'s properties as they always were, so a library that sets one keeps its meaning
		/// and the catalog and the D4 emitter keep round-tripping it; what no part of the product does is secure the
		/// connection. Silent when a site has supplied its own client binding, which may well secure the transport.
		/// </remarks>
		// NOTE: one warning per remote session, and a remote session is one per server link per user session, so the
		// volume is bounded by sessions rather than by statements. Left undeduplicated for simplicity; if a deployment
		// ever finds it noisy, dedupe on the ServerLink.
		private static void ReportUnhonouredSecuritySettings(ServerProcess process, Schema.ServerLink serverLink)
		{
			if (DataphorServiceUtility.HasCustomBinding || !(serverLink.UseSecureConnection || serverLink.UseSecureListenerConnection))
				return;

			string tags =
				serverLink.UseSecureConnection
					? (serverLink.UseSecureListenerConnection ? "tags UseSecureConnection and UseSecureListenerConnection" : "tag UseSecureConnection")
					: "tag UseSecureListenerConnection";

			process.ServerSession.Server.LogMessage
			(
				LogEntryType.Warning,
				String.Format
				(
					"The server link \"{0}\" carries the metadata {1}, but nothing in this product secures a connection on its own: this link's connection to instance \"{2}\" on host \"{3}\" is not secured. The remedy is to remove what is set from the link, or to secure the transport by supplying a binding through DataphorServiceUtility.BuildBindingCall from this server's own host application start-up code.",
					serverLink.Name,
					tags,
					serverLink.InstanceName,
					serverLink.HostName
				)
			);
		}
		
		/// <summary>Lets the connection go, and finishes doing so even when the far server cannot be told about it.</summary>
		/// <remarks>
		/// A remote session is let go by a process tearing itself down and by a server link being dropped or altered.
		/// Neither may fail because the other server is unreachable: the local half is released either way (see
		/// NativeCLISession.Dispose), and the far session then stays on that server until that server reclaims it, which
		/// there is nothing the caller can do about from here. What reclaims it, on a 4.0 server: that server's native
		/// session watcher (Alphora.Dataphor.DAE.Service.NativeSessionManager), which stops a session nothing has called
		/// for its idle threshold - five minutes by default - and rolls back its transaction; on a 3.1 server, nothing, so
		/// a session this process could not end stays there for as long as that server runs. The keep-alive
		/// NativeCLISession sends until this disposal is what keeps a session this process is still holding out of that
		/// watcher's way. Reported as a warning in the server log, the way ReportUnhonouredSecuritySettings reports.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			if (_nativeCLISession != null)
			{
				try
				{
					_nativeCLISession.Dispose();
				}
				catch (Exception exception)
				{
					ReportFailedRemoteStop(exception);
				}
				finally
				{
					_nativeCLISession = null;
				}
			}
			
			base.Dispose(disposing);
		}
		
		/// <summary>Writes one warning naming the link and the instance whose session could not be ended.</summary>
		/// <remarks>Before base.Dispose, which clears ServerProcess; a logging failure of its own is not worth failing the teardown for either.</remarks>
		private void ReportFailedRemoteStop(Exception exception)
		{
			try
			{
				ServerProcess.ServerSession.Server.LogMessage
				(
					LogEntryType.Warning,
					String.Format
					(
						"The session the server link \"{0}\" held on instance \"{1}\" on host \"{2}\" could not be ended on that server, so it stays there until that server reclaims it; this server has released its side of the connection. The error was: {3}",
						ServerLink.Name,
						ServerLink.InstanceName,
						ServerLink.HostName,
						exception.Message
					)
				);
			}
			// A server whose log has already been closed - this disposal can be part of its shutdown - would otherwise
			// throw out of the catch block that called this, which is the one thing the caller exists to prevent.
			catch { }
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
