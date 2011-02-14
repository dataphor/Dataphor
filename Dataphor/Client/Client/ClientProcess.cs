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
		public ClientProcess(ClientSession clientSession, ProcessInfo processInfo, ProcessDescriptor processDescriptor)
		{
			_clientSession = clientSession;
			_processInfo = processInfo;
			_processDescriptor = processDescriptor;
		}
		
		private ClientSession _clientSession;
		public ClientSession ClientSession { get { return _clientSession; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientSession.ClientConnection.ClientServer.GetServiceInterface();
		}
		
		private ProcessInfo _processInfo;
		
		private ProcessDescriptor _processDescriptor;
		
		public int ProcessHandle { get { return _processDescriptor.Handle; } }
		
		#region IRemoteServerProcess Members

		public IRemoteServerSession Session
		{
			get { return _clientSession; }
		}

		public Guid BeginApplicationTransaction(bool shouldJoin, bool isInsert, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginBeginApplicationTransaction(ProcessHandle, callInfo, shouldJoin, isInsert, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndBeginApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void PrepareApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginPrepareApplicationTransaction(ProcessHandle, callInfo, iD, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndPrepareApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void CommitApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginCommitApplicationTransaction(ProcessHandle, callInfo, iD, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndCommitApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void RollbackApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginRollbackApplicationTransaction(ProcessHandle, callInfo, iD, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndRollbackApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public Guid ApplicationTransactionID
		{
			get 
			{ 
				try
				{
					IAsyncResult result = GetServiceInterface().BeginGetApplicationTransactionID(ProcessHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetApplicationTransactionID(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		public void JoinApplicationTransaction(Guid iD, bool isInsert, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginJoinApplicationTransaction(ProcessHandle, callInfo, iD, isInsert, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndJoinApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void LeaveApplicationTransaction(ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginLeaveApplicationTransaction(ProcessHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndLeaveApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}
		
		public static ProcessCleanupInfo GetCleanupInfo(RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				ProcessCleanupInfo localCleanupInfo = new ProcessCleanupInfo();
				localCleanupInfo.UnprepareList = new int[cleanupInfo.UnprepareList.Length];
				for (int index = 0; index < localCleanupInfo.UnprepareList.Length; index++)
					localCleanupInfo.UnprepareList[index] = ((ClientPlan)cleanupInfo.UnprepareList[index]).PlanHandle;
				return localCleanupInfo;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public IRemoteServerStatementPlan PrepareStatement(string statement, RemoteParam[] paramsValue, DebugLocator locator, out PlanDescriptor planDescriptor, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginPrepareStatement(ProcessHandle, GetCleanupInfo(cleanupInfo), statement, paramsValue, locator, null, null);
				result.AsyncWaitHandle.WaitOne();
				planDescriptor = GetServiceInterface().EndPrepareStatement(result);
				return new ClientStatementPlan(this, planDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void UnprepareStatement(IRemoteServerStatementPlan plan)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginUnprepareStatement(((ClientPlan)plan).PlanHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUnprepareStatement(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Execute(string statement, ref RemoteParamData paramsValue, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginExecuteStatement(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, statement, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				ExecuteResult executeResult = GetServiceInterface().EndExecuteStatement(result);
				paramsValue.Data = executeResult.ParamData;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public IRemoteServerExpressionPlan PrepareExpression(string expression, RemoteParam[] paramsValue, DebugLocator locator, out PlanDescriptor planDescriptor, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginPrepareExpression(ProcessHandle, GetCleanupInfo(cleanupInfo), expression, paramsValue, locator, null, null);
				result.AsyncWaitHandle.WaitOne();
				planDescriptor = GetServiceInterface().EndPrepareExpression(result);
				return new ClientExpressionPlan(this, planDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan plan)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginUnprepareExpression(((ClientPlan)plan).PlanHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUnprepareExpression(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public byte[] Evaluate(string expression, ref RemoteParamData paramsValue, out IRemoteServerExpressionPlan plan, out PlanDescriptor planDescriptor, out ProgramStatistics executeTime, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginEvaluateExpression(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, expression, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				DirectEvaluateResult evaluateResult = GetServiceInterface().EndEvaluateExpression(result);
				paramsValue.Data = evaluateResult.ParamData;
				planDescriptor = evaluateResult.PlanDescriptor;
				executeTime = evaluateResult.ExecuteTime;
				plan = new ClientExpressionPlan(this, planDescriptor);
				return evaluateResult.Result;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public IRemoteServerCursor OpenCursor(string expression, ref RemoteParamData paramsValue, out IRemoteServerExpressionPlan plan, out PlanDescriptor planDescriptor, out ProgramStatistics executeTime, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginOpenCursor(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, expression, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				DirectCursorResult cursorResult = GetServiceInterface().EndOpenCursor(result);
				paramsValue.Data = cursorResult.ParamData;
				planDescriptor = cursorResult.PlanDescriptor;
				executeTime = cursorResult.ExecuteTime;
				plan = new ClientExpressionPlan(this, planDescriptor);
				return new ClientCursor((ClientExpressionPlan)plan, cursorResult.CursorDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public IRemoteServerCursor OpenCursor(string expression, ref RemoteParamData paramsValue, out IRemoteServerExpressionPlan plan, out PlanDescriptor planDescriptor, out ProgramStatistics executeTime, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo, out Guid[] bookmarks, int count, out RemoteFetchData fetchData)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginOpenCursorWithFetch(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, expression, paramsValue, count, null, null);
				result.AsyncWaitHandle.WaitOne();
				DirectCursorWithFetchResult cursorResult = GetServiceInterface().EndOpenCursorWithFetch(result);
				paramsValue.Data = cursorResult.ParamData;
				planDescriptor = cursorResult.PlanDescriptor;
				executeTime = cursorResult.ExecuteTime;
				bookmarks = cursorResult.Bookmarks;
				fetchData = cursorResult.FetchData;
				plan = new ClientExpressionPlan(this, planDescriptor);
				return new ClientCursor((ClientExpressionPlan)plan, cursorResult.CursorDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void CloseCursor(IRemoteServerCursor cursor, ProcessCallInfo callInfo)
		{
			ClientExpressionPlan plan = (ClientExpressionPlan)cursor.Plan;
			plan.Close(cursor, callInfo);
			UnprepareExpression(plan);
		}

		public IRemoteServerScript PrepareScript(string script, DebugLocator locator)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginPrepareScript(ProcessHandle, script, locator, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientScript(this, GetServiceInterface().EndPrepareScript(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void UnprepareScript(IRemoteServerScript script)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginUnprepareScript(((ClientScript)script).ScriptHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndUnprepareScript(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void ExecuteScript(string script, ProcessCallInfo callInfo)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginExecuteScript(ProcessHandle, callInfo, script, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndExecuteScript(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public string GetCatalog(string name, out long cacheTimeStamp, out long clientCacheTimeStamp, out bool cacheChanged)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetCatalog(ProcessHandle, name, null, null);
				result.AsyncWaitHandle.WaitOne();
				CatalogResult catalogResult = GetServiceInterface().EndGetCatalog(result);
				cacheTimeStamp = catalogResult.CacheTimeStamp;
				clientCacheTimeStamp = catalogResult.ClientCacheTimeStamp;
				cacheChanged = catalogResult.CacheChanged;
				return catalogResult.Catalog;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public string GetClassName(string className)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetClassName(ProcessHandle, className, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetClassName(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public Alphora.Dataphor.DAE.Server.ServerFileInfo[] GetFileNames(string className, string environment)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetFileNames(ProcessHandle, className, environment, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndGetFileNames(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public IRemoteStream GetFile(string libraryName, string fileName)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginGetFile(ProcessHandle, libraryName, fileName, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientStream(this, GetServiceInterface().EndGetFile(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		#endregion

		#region IServerProcessBase Members

		public int ProcessID
		{
			get { return _processDescriptor.ID; }
		}

		public ProcessInfo ProcessInfo
		{
			get { return _processInfo; }
		}

		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginBeginTransaction(ProcessHandle, isolationLevel, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndBeginTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void PrepareTransaction()
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginPrepareTransaction(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndPrepareTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void CommitTransaction()
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginCommitTransaction(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndCommitTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void RollbackTransaction()
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginRollbackTransaction(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndRollbackTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public bool InTransaction
		{
			get 
			{ 
				try
				{
					IAsyncResult result = GetServiceInterface().BeginGetTransactionCount(ProcessHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetTransactionCount(result) > 0;
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		public int TransactionCount
		{
			get 
			{ 
				try
				{
					IAsyncResult result = GetServiceInterface().BeginGetTransactionCount(ProcessHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetTransactionCount(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		#endregion

		#region IStreamManager Members

		public StreamID Allocate()
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginAllocateStream(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndAllocateStream(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public StreamID Reference(StreamID streamID)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginReferenceStream(ProcessHandle, streamID, null, null);
				result.AsyncWaitHandle.WaitOne();
				return GetServiceInterface().EndReferenceStream(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Deallocate(StreamID streamID)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginDeallocateStream(ProcessHandle, streamID, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDeallocateStream(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public Stream Open(StreamID streamID, LockMode mode)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginOpenStream(ProcessHandle, streamID, mode, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientStream(this, GetServiceInterface().EndOpenStream(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public IRemoteStream OpenRemote(StreamID streamID, LockMode mode)
		{
			return (IRemoteStream)Open(streamID, mode);
		}

		#endregion
	}
}
