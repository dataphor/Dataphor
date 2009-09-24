/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.Service
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Contracts;
	using Alphora.Dataphor.DAE.Debug;

	// TODO: Exception management
	//[ExceptionShielding("WCF Exception Shielding")]
	//[ServiceBehavior(Namespace = "http://Alphora.Dataphor.ServiceContracts/2009/09", Name = "DataphorService")]
	public class DataphorService : IDataphorService
	{
		public DataphorService()
		{
			string LInstanceName = ServerConfiguration.CDefaultLocalInstanceName;

			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("instance");
			if ((LSettings != null) && LSettings.Contains("name"))
				LInstanceName = (string)LSettings["name"];
				
			ServerConfiguration LConfiguration = InstanceManager.GetInstance(LInstanceName);
				
			FServer = new Server();
			LConfiguration.ApplyTo(FServer);

			FServer.Start();
			FRemoteServer = new RemoteServer(FServer);
			
			FConnectionManager = new ConnectionManager(FRemoteServer);
		}
		
		public DataphorService(RemoteServer ARemoteServer)
		{
			FServer = ARemoteServer.Server;
			FRemoteServer = ARemoteServer;
			FConnectionManager = new ConnectionManager(FRemoteServer);
		}
		
		private Server FServer;
		private RemoteServer FRemoteServer;

		private HandleManager FHandleManager = new HandleManager();
		private ConnectionManager FConnectionManager;
		
		#region IDataphorService Members

		public SessionDescriptor Connect(SessionInfo ASessionInfo)
		{
			RemoteServerConnection LConnection = FConnectionManager.GetConnection(ASessionInfo.CatalogCacheName, ASessionInfo.HostName);
			RemoteServerSession LSession = (RemoteServerSession)LConnection.Connect(ASessionInfo);
			return new SessionDescriptor(FHandleManager.GetHandle(LSession), LSession.SessionID);
		}

		public void Disconnect(int ASessionHandle)
		{
			// TODO: Relinquish connection on last session disconnect... (may be done by lifetime management...)
			FHandleManager.GetObject<RemoteServerSession>(ASessionHandle).Dispose();
		}

		public ProcessDescriptor StartProcess(int ASessionHandle, ProcessInfo AProcessInfo)
		{
			int LProcessID;
			return 
				new ProcessDescriptor
				(
					FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerSession>(ASessionHandle).StartProcess(AProcessInfo, out LProcessID)), 
					LProcessID
				);
		}

		public void StopProcess(int AProcessHandle)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).Stop();
		}

		public void BeginTransaction(int AProcessHandle, IsolationLevel AIsolationLevel)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).BeginTransaction(AIsolationLevel);
		}

		public void PrepareTransaction(int AProcessHandle)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareTransaction();
		}

		public void CommitTransaction(int AProcessHandle)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).CommitTransaction();
		}

		public void RollbackTransaction(int AProcessHandle)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).RollbackTransaction();
		}

		public int GetTransactionCount(int AProcessHandle)
		{
			return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).TransactionCount;
		}

		public Guid BeginApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, bool AShouldJoin, bool AIsInsert)
		{
			return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).BeginApplicationTransaction(AShouldJoin, AIsInsert, ACallInfo);
		}

		public void PrepareApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareApplicationTransaction(AID, ACallInfo);
		}

		public void CommitApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).CommitApplicationTransaction(AID, ACallInfo);
		}

		public void RollbackApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).RollbackApplicationTransaction(AID, ACallInfo);
		}

		public Guid GetApplicationTransactionID(int AProcessHandle)
		{
			return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).ApplicationTransactionID;
		}

		public void JoinApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID, bool AIsInsert)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).JoinApplicationTransaction(AID, AIsInsert, ACallInfo);
		}

		public void LeaveApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).LeaveApplicationTransaction(ACallInfo);
		}

		public PlanDescriptor PrepareStatement(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AStatement, RemoteParam[] AParams, DebugLocator ALocator)
		{
			PlanDescriptor LDescriptor;
			// It seems to me we should be able to perform the assignment directly to LDescriptor because it will be assigned by the out
			// evaluation in the PrepareStatement call, but the C# compiler is complaining that this is a use of unassigned local variable.
			//LDescriptor.Handle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareStatement(AStatement, AParams, ALocator, out LDescriptor, ACleanupInfo));
			int LHandle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareStatement(AStatement, AParams, ALocator, out LDescriptor, GetRemoteProcessCleanupInfo(ACleanupInfo)));
			LDescriptor.Handle = LHandle;
			return LDescriptor;
		}

		public void ExecutePlan(int APlanHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams, out TimeSpan AExecuteTime)
		{
			FHandleManager.GetObject<RemoteServerStatementPlan>(APlanHandle).Execute(ref AParams, out AExecuteTime, ACallInfo);
		}

		public void UnprepareStatement(int APlanHandle)
		{
			FHandleManager.GetObject<RemoteServerStatementPlan>(APlanHandle).Unprepare();
		}
		
		private RemoteProcessCleanupInfo GetRemoteProcessCleanupInfo(ProcessCleanupInfo ACleanupInfo)
		{
			RemoteProcessCleanupInfo LCleanupInfo = new RemoteProcessCleanupInfo();
			LCleanupInfo.UnprepareList = new IRemoteServerPlan[ACleanupInfo.UnprepareList.Length];
			for (int LIndex = 0; LIndex < ACleanupInfo.UnprepareList.Length; LIndex++)
				LCleanupInfo.UnprepareList[LIndex] = FHandleManager.GetObject<RemoteServerPlan>(ACleanupInfo.UnprepareList[LIndex]);
			return LCleanupInfo;
		}

		public PlanDescriptor PrepareExpression(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AExpression, RemoteParam[] AParams, DebugLocator ALocator)
		{
			PlanDescriptor LDescriptor;
			int LHandle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareExpression(AExpression, AParams, ALocator, out LDescriptor, GetRemoteProcessCleanupInfo(ACleanupInfo)));
			LDescriptor.Handle = LHandle;
			return LDescriptor;
		}

		public byte[] EvaluatePlan(int APlanHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams, out TimeSpan AExecuteTime)
		{
			return FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Evaluate(ref AParams, out AExecuteTime, ACallInfo);
		}

		public CursorDescriptor OpenPlanCursor(int APlanHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams, out TimeSpan AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData)
		{
			RemoteServerCursor LCursor = (RemoteServerCursor)FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Open(ref AParams, out AExecuteTime, out ABookmarks, ACount, out AFetchData, ACallInfo);
			CursorDescriptor LDescriptor = new CursorDescriptor(FHandleManager.GetHandle(LCursor), LCursor.Capabilities, LCursor.CursorType, LCursor.Isolation);
			return LDescriptor;
		}

		public void UnprepareExpression(int APlanHandle)
		{
			FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Unprepare();
		}

		public void CloseCursor(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Close();
		}

		public RemoteRowBody Select(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Select(ACallInfo);
		}

		public RemoteRowBody SelectSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Select(AHeader, ACallInfo);
		}

		public RemoteFetchData Fetch(int ACursorHandle, ProcessCallInfo ACallInfo, out Guid[] ABookmarks, int ACount)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Fetch(out ABookmarks, ACount, ACallInfo);
		}

		public RemoteFetchData FetchSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Fetch(AHeader, out ABookmarks, ACount, ACallInfo);
		}

		public CursorGetFlags GetFlags(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GetFlags(ACallInfo);
		}

		public RemoteMoveData MoveBy(int ACursorHandle, ProcessCallInfo ACallInfo, int ADelta)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).MoveBy(ADelta, ACallInfo);
		}

		public CursorGetFlags First(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).First(ACallInfo);
		}

		public CursorGetFlags Last(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Last(ACallInfo);
		}

		public CursorGetFlags Reset(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Reset(ACallInfo);
		}

		public void Insert(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags)
		{
			FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Insert(ARow, AValueFlags, ACallInfo);
		}

		public void Update(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags)
		{
			FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Update(ARow, AValueFlags, ACallInfo);
		}

		public void Delete(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Delete(ACallInfo);
		}

		public Guid GetBookmark(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GetBookmark(ACallInfo);
		}

		public RemoteGotoData GotoBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark, bool AForward)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GotoBookmark(ABookmark, AForward, ACallInfo);
		}

		public int CompareBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark1, Guid ABookmark2)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).CompareBookmarks(ABookmark1, ABookmark2, ACallInfo);
		}

		public void DisposeBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark)
		{
			FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).DisposeBookmark(ABookmark, ACallInfo);
		}

		public void DisposeBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid[] ABookmarks)
		{
			FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).DisposeBookmarks(ABookmarks, ACallInfo);
		}

		public string GetOrder(int ACursorHandle)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Order;
		}

		public RemoteRow GetKey(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GetKey(ACallInfo);
		}

		public RemoteGotoData FindKey(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).FindKey(AKey, ACallInfo);
		}

		public CursorGetFlags FindNearest(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).FindNearest(AKey, ACallInfo);
		}

		public RemoteGotoData Refresh(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Refresh(ARow, ACallInfo);
		}

		public int GetRowCount(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).RowCount(ACallInfo);
		}

		public RemoteProposeData Default(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody ARow, string AColumn)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Default(ARow, AColumn, ACallInfo);
		}

		public RemoteProposeData Change(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Change(AOldRow, ANewRow, AColumn, ACallInfo);
		}

		public RemoteProposeData Validate(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Validate(AOldRow, ANewRow, AColumn, ACallInfo);
		}

		public ScriptDescriptor PrepareScript(int AProcessHandle, string AScript, DebugLocator ALocator)
		{
			RemoteServerScript LScript = (RemoteServerScript)FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareScript(AScript, ALocator);
			ScriptDescriptor LDescriptor = new ScriptDescriptor(FHandleManager.GetHandle(LScript));
			foreach (Exception LException in LScript.Messages)
				LDescriptor.Messages.Add(LException);
			foreach (RemoteServerBatch LBatch in LScript.Batches)
				LDescriptor.Batches.Add(new BatchDescriptor(FHandleManager.GetHandle(LBatch), LBatch.IsExpression(), LBatch.Line));
			return LDescriptor;
		}

		public void UnprepareScript(int AScriptHandle)
		{
			FHandleManager.GetObject<RemoteServerScript>(AScriptHandle).Unprepare();
		}

		public void ExecuteScript(int AProcessHandle, ProcessCallInfo ACallInfo, string AScript)
		{
			FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).ExecuteScript(AScript, ACallInfo);
		}

		public string GetBatchText(int ABatchHandle)
		{
			return FHandleManager.GetObject<RemoteServerBatch>(ABatchHandle).GetText();
		}

		public PlanDescriptor PrepareBatch(int ABatchHandle, RemoteParam[] AParams)
		{
			RemoteServerBatch LBatch = FHandleManager.GetObject<RemoteServerBatch>(ABatchHandle);
			if (LBatch.IsExpression())
			{
				PlanDescriptor LDescriptor;
				int LHandle = FHandleManager.GetHandle(LBatch.PrepareExpression(AParams, out LDescriptor));
				LDescriptor.Handle = LHandle;
				return LDescriptor;
			}
			else
			{
				PlanDescriptor LDescriptor;
				int LHandle = FHandleManager.GetHandle(LBatch.PrepareStatement(AParams, out LDescriptor));
				LDescriptor.Handle = LHandle;
				return LDescriptor;
			}
		}

		public void UnprepareBatch(int APlanHandle)
		{
			FHandleManager.GetObject<RemoteServerPlan>(APlanHandle).Unprepare();
		}

		public void ExecuteBatch(int ABatchHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams)
		{
			FHandleManager.GetObject<RemoteServerBatch>(ABatchHandle).Execute(ref AParams, ACallInfo);
		}

		public string GetCatalog(int AProcessHandle, string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetCatalog(AName, out ACacheTimeStamp, out AClientCacheTimeStamp, out ACacheChanged);
		}

		public string GetClassName(int AProcessHandle, string AClassName)
		{
			return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetClassName(AClassName);
		}

		public ServerFileInfo[] GetFileNames(int AProcessHandle, string AClassName)
		{
			return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetFileNames(AClassName);
		}

		public byte[] GetFile(int AProcessHandle, string ALibraryName, string AFileName)
		{
			using (IRemoteStream LStream = FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetFile(ALibraryName, AFileName))
			{
				byte[] LResult = new byte[LStream.Length];
				LStream.Read(LResult, 0, LResult.Length);
				return LResult;
			}
		}

		public StreamID AllocateStream(int AProcessHandle)
		{
			return FHandleManager.GetObject<IStreamManager>(AProcessHandle).Allocate();
		}

		public StreamID ReferenceStream(int AProcessHandle, StreamID AStreamID)
		{
			return FHandleManager.GetObject<IStreamManager>(AProcessHandle).Reference(AStreamID);
		}

		public void DeallocateStream(int AProcessHandle, StreamID AStreamID)
		{
			FHandleManager.GetObject<IStreamManager>(AProcessHandle).Deallocate(AStreamID);
		}

		public int OpenStream(int AProcessHandle, StreamID AStreamID, LockMode ALockMode)
		{
			return FHandleManager.GetHandle(FHandleManager.GetObject<IStreamManager>(AProcessHandle).Open(AStreamID, ALockMode));
		}

		public void CloseStream(int AStreamHandle)
		{
			FHandleManager.ReleaseObject<Stream>(AStreamHandle).Close();
		}

		public long GetStreamLength(int AStreamHandle)
		{
			return FHandleManager.GetObject<Stream>(AStreamHandle).Length;
		}

		public void SetStreamLength(int AStreamHandle, long AValue)
		{
			FHandleManager.GetObject<Stream>(AStreamHandle).SetLength(AValue);
		}

		public long GetStreamPosition(int AStreamHandle)
		{
			return FHandleManager.GetObject<Stream>(AStreamHandle).Position;
		}

		public void SetStreamPosition(int AStreamHandle, long APosition)
		{
			FHandleManager.GetObject<Stream>(AStreamHandle).Position = APosition;
		}

		public void FlushStream(int AStreamHandle)
		{
			FHandleManager.GetObject<Stream>(AStreamHandle).Flush();
		}

		public byte[] ReadStream(int AStreamHandle, int ACount)
		{
			byte[] LResult = new byte[ACount];
			int ARead = FHandleManager.GetObject<Stream>(AStreamHandle).Read(LResult, 0, ACount);
			if (ARead == ACount)
				return LResult;
			
			byte[] LReadResult = new byte[ARead];
			Array.Copy(LResult, LReadResult, ARead);
			return LReadResult;
		}

		public long SeekStream(int AStreamHandle, long AOffset, SeekOrigin AOrigin)
		{
			return FHandleManager.GetObject<Stream>(AStreamHandle).Seek(AOffset, AOrigin);
		}

		public void WriteStream(int AStreamHandle, byte[] AData)
		{
			FHandleManager.GetObject<Stream>(AStreamHandle).Write(AData, 0, AData.Length);
		}

		#endregion
	}
}
