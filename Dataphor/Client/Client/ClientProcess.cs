/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ServiceModel;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientProcess : ClientObject, IRemoteServerProcess
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
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginBeginApplicationTransaction(ProcessHandle, ACallInfo, AShouldJoin, AIsInsert, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndBeginApplicationTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void PrepareApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginPrepareApplicationTransaction(ProcessHandle, ACallInfo, AID, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndPrepareApplicationTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void CommitApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginCommitApplicationTransaction(ProcessHandle, ACallInfo, AID, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndCommitApplicationTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void RollbackApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginRollbackApplicationTransaction(ProcessHandle, ACallInfo, AID, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndRollbackApplicationTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public Guid ApplicationTransactionID
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetApplicationTransactionID(ProcessHandle, null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetApplicationTransactionID(LResult);
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		public void JoinApplicationTransaction(Guid AID, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginJoinApplicationTransaction(ProcessHandle, ACallInfo, AID, AIsInsert, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndJoinApplicationTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void LeaveApplicationTransaction(ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginLeaveApplicationTransaction(ProcessHandle, ACallInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndLeaveApplicationTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}
		
		public static ProcessCleanupInfo GetCleanupInfo(RemoteProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				ProcessCleanupInfo LCleanupInfo = new ProcessCleanupInfo();
				LCleanupInfo.UnprepareList = new int[ACleanupInfo.UnprepareList.Length];
				for (int LIndex = 0; LIndex < LCleanupInfo.UnprepareList.Length; LIndex++)
					LCleanupInfo.UnprepareList[LIndex] = ((ClientPlan)ACleanupInfo.UnprepareList[LIndex]).PlanHandle;
				return LCleanupInfo;
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public IRemoteServerStatementPlan PrepareStatement(string AStatement, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginPrepareStatement(ProcessHandle, GetCleanupInfo(ACleanupInfo), AStatement, AParams, ALocator, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				APlanDescriptor = GetServiceInterface().EndPrepareStatement(LResult);
				return new ClientStatementPlan(this, APlanDescriptor);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginUnprepareStatement(((ClientPlan)APlan).PlanHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUnprepareStatement(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Execute(string AStatement, ref RemoteParamData AParams, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginExecuteStatement(ProcessHandle, GetCleanupInfo(ACleanupInfo), ACallInfo, AStatement, AParams, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				ExecuteResult LExecuteResult = GetServiceInterface().EndExecuteStatement(LResult);
				AParams.Data = LExecuteResult.ParamData;
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public IRemoteServerExpressionPlan PrepareExpression(string AExpression, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginPrepareExpression(ProcessHandle, GetCleanupInfo(ACleanupInfo), AExpression, AParams, ALocator, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				APlanDescriptor = GetServiceInterface().EndPrepareExpression(LResult);
				return new ClientExpressionPlan(this, APlanDescriptor);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginUnprepareExpression(((ClientPlan)APlan).PlanHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUnprepareExpression(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public byte[] Evaluate(string AExpression, ref RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out PlanDescriptor APlanDescriptor, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginEvaluateExpression(ProcessHandle, GetCleanupInfo(ACleanupInfo), ACallInfo, AExpression, AParams, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				DirectEvaluateResult LEvaluateResult = GetServiceInterface().EndEvaluateExpression(LResult);
				AParams.Data = LEvaluateResult.ParamData;
				APlanDescriptor = LEvaluateResult.PlanDescriptor;
				AExecuteTime = LEvaluateResult.ExecuteTime;
				APlan = new ClientExpressionPlan(this, APlanDescriptor);
				return LEvaluateResult.Result;
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public IRemoteServerCursor OpenCursor(string AExpression, ref RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out PlanDescriptor APlanDescriptor, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginOpenCursor(ProcessHandle, GetCleanupInfo(ACleanupInfo), ACallInfo, AExpression, AParams, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				DirectCursorResult LCursorResult = GetServiceInterface().EndOpenCursor(LResult);
				AParams.Data = LCursorResult.ParamData;
				APlanDescriptor = LCursorResult.PlanDescriptor;
				AExecuteTime = LCursorResult.ExecuteTime;
				APlan = new ClientExpressionPlan(this, APlanDescriptor);
				return new ClientCursor((ClientExpressionPlan)APlan, LCursorResult.CursorDescriptor);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public IRemoteServerCursor OpenCursor(string AExpression, ref RemoteParamData AParams, out IRemoteServerExpressionPlan APlan, out PlanDescriptor APlanDescriptor, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginOpenCursorWithFetch(ProcessHandle, GetCleanupInfo(ACleanupInfo), ACallInfo, AExpression, AParams, ACount, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				DirectCursorWithFetchResult LCursorResult = GetServiceInterface().EndOpenCursorWithFetch(LResult);
				AParams.Data = LCursorResult.ParamData;
				APlanDescriptor = LCursorResult.PlanDescriptor;
				AExecuteTime = LCursorResult.ExecuteTime;
				ABookmarks = LCursorResult.Bookmarks;
				AFetchData = LCursorResult.FetchData;
				APlan = new ClientExpressionPlan(this, APlanDescriptor);
				return new ClientCursor((ClientExpressionPlan)APlan, LCursorResult.CursorDescriptor);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void CloseCursor(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			ClientExpressionPlan LPlan = (ClientExpressionPlan)ACursor.Plan;
			LPlan.Close(ACursor, ACallInfo);
			UnprepareExpression(LPlan);
		}

		public IRemoteServerScript PrepareScript(string AScript, DebugLocator ALocator)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginPrepareScript(ProcessHandle, AScript, ALocator, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return new ClientScript(this, GetServiceInterface().EndPrepareScript(LResult));
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void UnprepareScript(IRemoteServerScript AScript)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginUnprepareScript(((ClientScript)AScript).ScriptHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUnprepareScript(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void ExecuteScript(string AScript, ProcessCallInfo ACallInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginExecuteScript(ProcessHandle, ACallInfo, AScript, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndExecuteScript(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public string GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetCatalog(ProcessHandle, AName, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				CatalogResult LCatalogResult = GetServiceInterface().EndGetCatalog(LResult);
				ACacheTimeStamp = LCatalogResult.CacheTimeStamp;
				AClientCacheTimeStamp = LCatalogResult.ClientCacheTimeStamp;
				ACacheChanged = LCatalogResult.CacheChanged;
				return LCatalogResult.Catalog;
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public string GetClassName(string AClassName)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetClassName(ProcessHandle, AClassName, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetClassName(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public Alphora.Dataphor.DAE.Server.ServerFileInfo[] GetFileNames(string AClassName, string AEnvironment)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetFileNames(ProcessHandle, AClassName, AEnvironment, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetFileNames(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public IRemoteStream GetFile(string ALibraryName, string AFileName)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginGetFile(ProcessHandle, ALibraryName, AFileName, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return new ClientStream(this, GetServiceInterface().EndGetFile(LResult));
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
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
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginBeginTransaction(ProcessHandle, AIsolationLevel, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndBeginTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void PrepareTransaction()
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginPrepareTransaction(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndPrepareTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void CommitTransaction()
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginCommitTransaction(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndCommitTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void RollbackTransaction()
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginRollbackTransaction(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndRollbackTransaction(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public bool InTransaction
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetTransactionCount(ProcessHandle, null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetTransactionCount(LResult) > 0;
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		public int TransactionCount
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetTransactionCount(ProcessHandle, null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetTransactionCount(LResult);
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		#endregion

		#region IStreamManager Members

		public StreamID Allocate()
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginAllocateStream(ProcessHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndAllocateStream(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public StreamID Reference(StreamID AStreamID)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginReferenceStream(ProcessHandle, AStreamID, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndReferenceStream(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Deallocate(StreamID AStreamID)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginDeallocateStream(ProcessHandle, AStreamID, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDeallocateStream(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public Stream Open(StreamID AStreamID, LockMode AMode)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginOpenStream(ProcessHandle, AStreamID, AMode, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return new ClientStream(this, GetServiceInterface().EndOpenStream(LResult));
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public IRemoteStream OpenRemote(StreamID AStreamID, LockMode AMode)
		{
			return (IRemoteStream)Open(AStreamID, AMode);
		}

		#endregion
	}
}
