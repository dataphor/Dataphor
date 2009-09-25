/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientProcess : IRemoteServerProcess
	{
		public ClientProcess(ClientSession AClientSession, ProcessInfo AProcessInfo, ProcessDescriptor AProcessDescriptor)
		{
			FClientSession = AClientSession;
			FProcessInfo = AProcessInfo;
			FProcessDescriptor = AProcessDescriptor;
		}
		
		private ClientSession FClientSession;
		public ClientSession ClientSession { get { return FClientSession; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return FClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}
		
		private ProcessInfo FProcessInfo;
		
		private ProcessDescriptor FProcessDescriptor;
		
		public int ProcessHandle { get { return FProcessDescriptor.Handle; } }
		
		#region IRemoteServerProcess Members

		public IRemoteServerSession Session
		{
			get { return FClientSession; }
		}

		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginBeginApplicationTransaction(ProcessHandle, ACallInfo, AShouldJoin, AIsInsert, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndBeginApplicationTransaction(LResult);
		}

		public void PrepareApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginPrepareApplicationTransaction(ProcessHandle, ACallInfo, AID, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndPrepareApplicationTransaction(LResult);
		}

		public void CommitApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginCommitApplicationTransaction(ProcessHandle, ACallInfo, AID, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndCommitApplicationTransaction(LResult);
		}

		public void RollbackApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginRollbackApplicationTransaction(ProcessHandle, ACallInfo, AID, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndRollbackApplicationTransaction(LResult);
		}

		public Guid ApplicationTransactionID
		{
			get 
			{ 
				IAsyncResult LResult = GetServiceInterface().BeginGetApplicationTransactionID(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetApplicationTransactionID(LResult);
			}
		}

		public void JoinApplicationTransaction(Guid AID, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginJoinApplicationTransaction(ProcessHandle, ACallInfo, AID, AIsInsert, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndJoinApplicationTransaction(LResult);
		}

		public void LeaveApplicationTransaction(ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginLeaveApplicationTransaction(ProcessHandle, ACallInfo, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndLeaveApplicationTransaction(LResult);
		}
		
		public static ProcessCleanupInfo GetCleanupInfo(RemoteProcessCleanupInfo ACleanupInfo)
		{
			ProcessCleanupInfo LCleanupInfo = new ProcessCleanupInfo();
			LCleanupInfo.UnprepareList = new int[ACleanupInfo.UnprepareList.Length];
			for (int LIndex = 0; LIndex < LCleanupInfo.UnprepareList.Length; LIndex++)
				LCleanupInfo.UnprepareList[LIndex] = ((ClientPlan)ACleanupInfo.UnprepareList[LIndex]).PlanHandle;
			return LCleanupInfo;
		}

		public IRemoteServerStatementPlan PrepareStatement(string AStatement, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginPrepareStatement(ProcessHandle, GetCleanupInfo(ACleanupInfo), AStatement, AParams, ALocator, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			APlanDescriptor = GetServiceInterface().EndPrepareStatement(LResult);
			return new ClientStatementPlan(this, APlanDescriptor);
		}

		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			IAsyncResult LResult = GetServiceInterface().BeginUnprepareStatement(((ClientPlan)APlan).PlanHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndUnprepareStatement(LResult);
		}

		public void Execute(string AStatement, ref RemoteParamData AParams, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerExpressionPlan PrepareExpression(string AExpression, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginPrepareExpression(ProcessHandle, GetCleanupInfo(ACleanupInfo), AExpression, AParams, ALocator, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			APlanDescriptor = GetServiceInterface().EndPrepareExpression(LResult);
			return new ClientExpressionPlan(this, APlanDescriptor);
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			IAsyncResult LResult = GetServiceInterface().BeginUnprepareExpression(((ClientPlan)APlan).PlanHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndUnprepareExpression(LResult);
		}

		public byte[] Evaluate(string AExpression, ref RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out PlanDescriptor APlanDescriptor, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor OpenCursor(string AExpression, ref RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out PlanDescriptor APlanDescriptor, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerCursor OpenCursor(string AExpression, ref RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out PlanDescriptor APlanDescriptor, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData)
		{
			throw new NotImplementedException();
		}

		public void CloseCursor(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public IRemoteServerScript PrepareScript(string AScript, DebugLocator ALocator)
		{
			IAsyncResult LResult = GetServiceInterface().BeginPrepareScript(ProcessHandle, AScript, ALocator, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return new ClientScript(this, GetServiceInterface().EndPrepareScript(LResult));
		}

		public void UnprepareScript(IRemoteServerScript AScript)
		{
			IAsyncResult LResult = GetServiceInterface().BeginUnprepareScript(((ClientScript)AScript).ScriptHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndUnprepareScript(LResult);
		}

		public void ExecuteScript(string AScript, ProcessCallInfo ACallInfo)
		{
			IAsyncResult LResult = GetServiceInterface().BeginExecuteScript(ProcessHandle, ACallInfo, AScript, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndExecuteScript(LResult);
		}

		public string GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetCatalog(ProcessHandle, AName, out ACacheTimeStamp, out AClientCacheTimeStamp, out ACacheChanged, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetCatalog(LResult);
		}

		public string GetClassName(string AClassName)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetClassName(ProcessHandle, AClassName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetClassName(LResult);
		}

		public Alphora.Dataphor.DAE.Server.ServerFileInfo[] GetFileNames(string AClassName)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetFileNames(ProcessHandle, AClassName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndGetFileNames(LResult);
		}

		public IRemoteStream GetFile(string ALibraryName, string AFileName)
		{
			IAsyncResult LResult = GetServiceInterface().BeginGetFile(ProcessHandle, ALibraryName, AFileName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			byte[] LFile = GetServiceInterface().EndGetFile(LResult);
			MemoryStream LStream = new MemoryStream(LFile);
			return new CoverStream(LStream);
		}

		#endregion

		#region IServerProcessBase Members

		public int ProcessID
		{
			get { return FProcessDescriptor.ID; }
		}

		public ProcessInfo ProcessInfo
		{
			get { return FProcessInfo; }
		}

		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			IAsyncResult LResult = GetServiceInterface().BeginBeginTransaction(ProcessHandle, AIsolationLevel, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndBeginTransaction(LResult);
		}

		public void PrepareTransaction()
		{
			IAsyncResult LResult = GetServiceInterface().BeginPrepareTransaction(ProcessHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndPrepareTransaction(LResult);
		}

		public void CommitTransaction()
		{
			IAsyncResult LResult = GetServiceInterface().BeginCommitTransaction(ProcessHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndCommitTransaction(LResult);
		}

		public void RollbackTransaction()
		{
			IAsyncResult LResult = GetServiceInterface().BeginRollbackTransaction(ProcessHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndRollbackTransaction(LResult);
		}

		public bool InTransaction
		{
			get 
			{ 
				IAsyncResult LResult = GetServiceInterface().BeginGetTransactionCount(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetTransactionCount(LResult) > 0;
			}
		}

		public int TransactionCount
		{
			get 
			{ 
				IAsyncResult LResult = GetServiceInterface().BeginGetTransactionCount(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetTransactionCount(LResult);
			}
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion

		#region IStreamManager Members

		public StreamID Allocate()
		{
			IAsyncResult LResult = GetServiceInterface().BeginAllocateStream(ProcessHandle, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndAllocateStream(LResult);
		}

		public StreamID Reference(StreamID AStreamID)
		{
			IAsyncResult LResult = GetServiceInterface().BeginReferenceStream(ProcessHandle, AStreamID, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetServiceInterface().EndReferenceStream(LResult);
		}

		public void Deallocate(StreamID AStreamID)
		{
			IAsyncResult LResult = GetServiceInterface().BeginDeallocateStream(ProcessHandle, AStreamID, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			GetServiceInterface().EndDeallocateStream(LResult);
		}

		public Stream Open(StreamID AStreamID, LockMode AMode)
		{
			IAsyncResult LResult = GetServiceInterface().BeginOpenStream(ProcessHandle, AStreamID, AMode, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return new ClientStream(this, GetServiceInterface().EndOpenStream(LResult));
		}

		public IRemoteStream OpenRemote(StreamID AStreamID, LockMode AMode)
		{
			return (IRemoteStream)Open(AStreamID, AMode);
		}

		#endregion
	}
}
