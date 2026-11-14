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
using Alphora.Dataphor.DAE.Server;

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

		private void ReportCommunicationError()
		{
			_clientSession.ClientConnection.ClientServer.ReportCommunicationError();
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
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginBeginApplicationTransaction(ProcessHandle, callInfo, shouldJoin, isInsert, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndBeginApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void PrepareApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPrepareApplicationTransaction(ProcessHandle, callInfo, iD, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndPrepareApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void CommitApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginCommitApplicationTransaction(ProcessHandle, callInfo, iD, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndCommitApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void RollbackApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginRollbackApplicationTransaction(ProcessHandle, callInfo, iD, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndRollbackApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public Guid ApplicationTransactionID
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetApplicationTransactionID(ProcessHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetApplicationTransactionID(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		public void JoinApplicationTransaction(Guid iD, bool isInsert, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginJoinApplicationTransaction(ProcessHandle, callInfo, iD, isInsert, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndJoinApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void LeaveApplicationTransaction(ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginLeaveApplicationTransaction(ProcessHandle, callInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndLeaveApplicationTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
			catch (CommunicationException ce)
			{
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteServerStatementPlan PrepareStatement(string statement, RemoteParam[] paramsValue, DebugLocator locator, out PlanDescriptor planDescriptor, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPrepareStatement(ProcessHandle, GetCleanupInfo(cleanupInfo), statement, paramsValue, locator, null, null);
				result.AsyncWaitHandle.WaitOne();
				planDescriptor = channel.EndPrepareStatement(result);
				return new ClientStatementPlan(this, planDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void UnprepareStatement(IRemoteServerStatementPlan plan)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginUnprepareStatement(((ClientPlan)plan).PlanHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndUnprepareStatement(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Execute(string statement, ref RemoteParamData paramsValue, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginExecuteStatement(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, statement, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				ExecuteResult executeResult = channel.EndExecuteStatement(result);
				paramsValue.Data = executeResult.ParamData;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteServerExpressionPlan PrepareExpression(string expression, RemoteParam[] paramsValue, DebugLocator locator, out PlanDescriptor planDescriptor, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPrepareExpression(ProcessHandle, GetCleanupInfo(cleanupInfo), expression, paramsValue, locator, null, null);
				result.AsyncWaitHandle.WaitOne();
				planDescriptor = channel.EndPrepareExpression(result);
				return new ClientExpressionPlan(this, planDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void UnprepareExpression(IRemoteServerExpressionPlan plan)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginUnprepareExpression(((ClientPlan)plan).PlanHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndUnprepareExpression(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public byte[] Evaluate(string expression, ref RemoteParamData paramsValue, out IRemoteServerExpressionPlan plan, out PlanDescriptor planDescriptor, out ProgramStatistics executeTime, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginEvaluateExpression(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, expression, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				DirectEvaluateResult evaluateResult = channel.EndEvaluateExpression(result);
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
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteServerCursor OpenCursor(string expression, ref RemoteParamData paramsValue, out IRemoteServerExpressionPlan plan, out PlanDescriptor planDescriptor, out ProgramStatistics executeTime, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginOpenCursor(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, expression, paramsValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				DirectCursorResult cursorResult = channel.EndOpenCursor(result);
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
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteServerCursor OpenCursor(string expression, ref RemoteParamData paramsValue, out IRemoteServerExpressionPlan plan, out PlanDescriptor planDescriptor, out ProgramStatistics executeTime, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo, out Guid[] bookmarks, int count, out RemoteFetchData fetchData)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginOpenCursorWithFetch(ProcessHandle, GetCleanupInfo(cleanupInfo), callInfo, expression, paramsValue, count, null, null);
				result.AsyncWaitHandle.WaitOne();
				DirectCursorWithFetchResult cursorResult = channel.EndOpenCursorWithFetch(result);
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
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPrepareScript(ProcessHandle, script, locator, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientScript(this, channel.EndPrepareScript(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void UnprepareScript(IRemoteServerScript script)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginUnprepareScript(((ClientScript)script).ScriptHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndUnprepareScript(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void ExecuteScript(string script, ProcessCallInfo callInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginExecuteScript(ProcessHandle, callInfo, script, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndExecuteScript(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public string GetCatalog(string name, out long cacheTimeStamp, out long clientCacheTimeStamp, out bool cacheChanged)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetCatalog(ProcessHandle, name, null, null);
				result.AsyncWaitHandle.WaitOne();
				CatalogResult catalogResult = channel.EndGetCatalog(result);
				cacheTimeStamp = catalogResult.CacheTimeStamp;
				clientCacheTimeStamp = catalogResult.ClientCacheTimeStamp;
				cacheChanged = catalogResult.CacheChanged;
				return catalogResult.Catalog;
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public string GetClassName(string className)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetClassName(ProcessHandle, className, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetClassName(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public Alphora.Dataphor.DAE.Server.ServerFileInfo[] GetFileNames(string className, string environment)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetFileNames(ProcessHandle, className, environment, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndGetFileNames(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteStream GetFile(string libraryName, string fileName)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginGetFile(ProcessHandle, libraryName, fileName, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientStream(this, channel.EndGetFile(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginBeginTransaction(ProcessHandle, isolationLevel, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndBeginTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void PrepareTransaction()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPrepareTransaction(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndPrepareTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void CommitTransaction()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginCommitTransaction(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndCommitTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void RollbackTransaction()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginRollbackTransaction(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndRollbackTransaction(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public bool InTransaction
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetTransactionCount(ProcessHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetTransactionCount(result) > 0;
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		public int TransactionCount
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetTransactionCount(ProcessHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetTransactionCount(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		#endregion

		#region IStreamManager Members

		public StreamID Allocate()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginAllocateStream(ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndAllocateStream(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public StreamID Reference(StreamID streamID)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginReferenceStream(ProcessHandle, streamID, null, null);
				result.AsyncWaitHandle.WaitOne();
				return channel.EndReferenceStream(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Deallocate(StreamID streamID)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginDeallocateStream(ProcessHandle, streamID, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndDeallocateStream(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public Stream Open(StreamID streamID, LockMode mode)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginOpenStream(ProcessHandle, streamID, mode, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientStream(this, channel.EndOpenStream(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public IRemoteStream OpenRemote(StreamID streamID, LockMode mode)
		{
			return (IRemoteStream)Open(streamID, mode);
		}

		#endregion
	}
}
