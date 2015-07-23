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

	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class DataphorService : IDataphorService, IDisposable
	{
		public DataphorService()
		{
			string instanceName = ServerConfiguration.DefaultLocalInstanceName;

			IDictionary settings = (IDictionary)ConfigurationManager.GetSection("instance");
			if ((settings != null) && settings.Contains("name"))
				instanceName = (string)settings["name"];
				
			ServerConfiguration configuration = InstanceManager.GetInstance(instanceName);
				
			_server = new Server();
			configuration.ApplyTo(_server);

			_server.Start();
			_remoteServer = new RemoteServer(_server);
			
			_connectionManager = new ConnectionManager(_remoteServer);
		}
		
		public DataphorService(RemoteServer remoteServer)
		{
			_server = remoteServer.Server;
			_remoteServer = remoteServer;
			_connectionManager = new ConnectionManager(_remoteServer);
		}

		private Server _server;
		private RemoteServer _remoteServer;

		private HandleManager _handleManager = new HandleManager();
		private ConnectionManager _connectionManager;
		
		#region IDisposable Members

		public void Dispose()
		{
			if (_connectionManager != null)
			{
				_connectionManager.Dispose();
				_connectionManager = null;
			}
		}

		#endregion

		#region IDataphorService Members
		
		public string GetServerName()
		{
			try
			{
				return _server.Name;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void Start()
		{
			try
			{
				_server.Start();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void Stop()
		{
			try
			{
				_server.Stop();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public ServerState GetState()
		{
			try
			{
				return _server.State;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public long GetCacheTimeStamp()
		{
			try
			{
				return _server.CacheTimeStamp;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public long GetDerivationTimeStamp()
		{
			try
			{
				return _server.DerivationTimeStamp;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public int OpenConnection(string connectionName, string hostName)
		{
			try
			{
				return _handleManager.GetHandle(_remoteServer.Establish(connectionName, hostName));
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public void PingConnection(int connectionHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerConnection>(connectionHandle).Ping();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public void CloseConnection(int connectionHandle)
		{
			try
			{
				_remoteServer.Relinquish(_handleManager.GetObject<RemoteServerConnection>(connectionHandle));
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public SessionDescriptor Connect(int connectionHandle, SessionInfo sessionInfo)
		{
			try
			{
				RemoteServerSession session = (RemoteServerSession)_handleManager.GetObject<RemoteServerConnection>(connectionHandle).Connect(sessionInfo);
				return new SessionDescriptor(_handleManager.GetHandle(session), session.SessionID);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void Disconnect(int sessionHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerSession>(sessionHandle).Dispose();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public ProcessDescriptor StartProcess(int sessionHandle, ProcessInfo processInfo)
		{
			try
			{
				int processID;
				return 
					new ProcessDescriptor
					(
						_handleManager.GetHandle(_handleManager.GetObject<RemoteServerSession>(sessionHandle).StartProcess(processInfo, out processID)), 
						processID
					);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void StopProcess(int processHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).Dispose();
				//FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).Stop();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void BeginTransaction(int processHandle, IsolationLevel isolationLevel)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).BeginTransaction(isolationLevel);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void PrepareTransaction(int processHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).PrepareTransaction();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void CommitTransaction(int processHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).CommitTransaction();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void RollbackTransaction(int processHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).RollbackTransaction();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public int GetTransactionCount(int processHandle)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerProcess>(processHandle).TransactionCount;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public Guid BeginApplicationTransaction(int processHandle, ProcessCallInfo callInfo, bool shouldJoin, bool isInsert)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerProcess>(processHandle).BeginApplicationTransaction(shouldJoin, isInsert, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void PrepareApplicationTransaction(int processHandle, ProcessCallInfo callInfo, Guid iD)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).PrepareApplicationTransaction(iD, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void CommitApplicationTransaction(int processHandle, ProcessCallInfo callInfo, Guid iD)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).CommitApplicationTransaction(iD, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void RollbackApplicationTransaction(int processHandle, ProcessCallInfo callInfo, Guid iD)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).RollbackApplicationTransaction(iD, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public Guid GetApplicationTransactionID(int processHandle)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerProcess>(processHandle).ApplicationTransactionID;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void JoinApplicationTransaction(int processHandle, ProcessCallInfo callInfo, Guid iD, bool isInsert)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).JoinApplicationTransaction(iD, isInsert, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void LeaveApplicationTransaction(int processHandle, ProcessCallInfo callInfo)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).LeaveApplicationTransaction(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		private RemoteProcessCleanupInfo GetRemoteProcessCleanupInfo(ProcessCleanupInfo cleanupInfo)
		{
			try
			{
				RemoteProcessCleanupInfo localCleanupInfo = new RemoteProcessCleanupInfo();
				localCleanupInfo.UnprepareList = new IRemoteServerPlan[cleanupInfo.UnprepareList.Length];
				for (int index = 0; index < cleanupInfo.UnprepareList.Length; index++)
					localCleanupInfo.UnprepareList[index] = _handleManager.GetObject<RemoteServerPlan>(cleanupInfo.UnprepareList[index]);
				return localCleanupInfo;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public PlanDescriptor PrepareStatement(int processHandle, ProcessCleanupInfo cleanupInfo, string statement, RemoteParam[] paramsValue, DebugLocator locator)
		{
			try
			{
				PlanDescriptor descriptor;
				// It seems to me we should be able to perform the assignment directly to LDescriptor because it will be assigned by the out
				// evaluation in the PrepareStatement call, but the C# compiler is complaining that this is a use of unassigned local variable.
				//LDescriptor.Handle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareStatement(AStatement, AParams, ALocator, out LDescriptor, ACleanupInfo));
				int handle = _handleManager.GetHandle(_handleManager.GetObject<RemoteServerProcess>(processHandle).PrepareStatement(statement, paramsValue, locator, out descriptor, GetRemoteProcessCleanupInfo(cleanupInfo)));
				descriptor.Handle = handle;
				return descriptor;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public ExecuteResult ExecutePlan(int planHandle, ProcessCallInfo callInfo, RemoteParamData paramsValue)
		{
			try
			{
				ProgramStatistics executeTime;
				_handleManager.GetObject<RemoteServerStatementPlan>(planHandle).Execute(ref paramsValue, out executeTime, callInfo);
				return new ExecuteResult() { ExecuteTime = executeTime, ParamData = paramsValue.Data };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void UnprepareStatement(int planHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerStatementPlan>(planHandle).Unprepare();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public ExecuteResult ExecuteStatement(int processHandle, ProcessCleanupInfo cleanupInfo, ProcessCallInfo callInfo, string statement, RemoteParamData paramsValue)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).Execute(statement, ref paramsValue, callInfo, GetRemoteProcessCleanupInfo(cleanupInfo));
				return new ExecuteResult() { ExecuteTime = new ProgramStatistics(), ParamData = paramsValue.Data };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public PlanDescriptor PrepareExpression(int processHandle, ProcessCleanupInfo cleanupInfo, string expression, RemoteParam[] paramsValue, DebugLocator locator)
		{
			try
			{
				PlanDescriptor descriptor;
				int handle = _handleManager.GetHandle(_handleManager.GetObject<RemoteServerProcess>(processHandle).PrepareExpression(expression, paramsValue, locator, out descriptor, GetRemoteProcessCleanupInfo(cleanupInfo)));
				descriptor.Handle = handle;
				return descriptor;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public EvaluateResult EvaluatePlan(int planHandle, ProcessCallInfo callInfo, RemoteParamData paramsValue)
		{
			try
			{
				ProgramStatistics executeTime;
				byte[] result = _handleManager.GetObject<RemoteServerExpressionPlan>(planHandle).Evaluate(ref paramsValue, out executeTime, callInfo);
				return new EvaluateResult() { ExecuteTime = executeTime, ParamData = paramsValue.Data, Result = result };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public CursorResult OpenPlanCursor(int planHandle, ProcessCallInfo callInfo, RemoteParamData paramsValue)
		{
			try
			{
				ProgramStatistics executeTime;
				RemoteServerCursor cursor = (RemoteServerCursor)_handleManager.GetObject<RemoteServerExpressionPlan>(planHandle).Open(ref paramsValue, out executeTime, callInfo);
				CursorDescriptor descriptor = new CursorDescriptor(_handleManager.GetHandle(cursor), cursor.Capabilities, cursor.CursorType, cursor.Isolation);
				return new CursorResult() { ExecuteTime = executeTime, ParamData = paramsValue.Data, CursorDescriptor = descriptor };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CursorWithFetchResult OpenPlanCursorWithFetch(int planHandle, ProcessCallInfo callInfo, RemoteParamData paramsValue, int count)
		{
			try
			{
				ProgramStatistics executeTime;
				Guid[] bookmarks;
				RemoteFetchData fetchData;
				RemoteServerCursor cursor = (RemoteServerCursor)_handleManager.GetObject<RemoteServerExpressionPlan>(planHandle).Open(ref paramsValue, out executeTime, out bookmarks, count, out fetchData, callInfo);
				CursorDescriptor descriptor = new CursorDescriptor(_handleManager.GetHandle(cursor), cursor.Capabilities, cursor.CursorType, cursor.Isolation);
				return new CursorWithFetchResult() { ExecuteTime = executeTime, ParamData = paramsValue.Data, CursorDescriptor = descriptor, Bookmarks = bookmarks, FetchData = fetchData };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void UnprepareExpression(int planHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerExpressionPlan>(planHandle).Unprepare();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public DirectEvaluateResult EvaluateExpression(int processHandle, ProcessCleanupInfo cleanupInfo, ProcessCallInfo callInfo, string expression, RemoteParamData paramsValue)
		{
			try
			{
				IRemoteServerExpressionPlan plan;
				PlanDescriptor planDescriptor;
				ProgramStatistics executeTime;
				byte[] result = _handleManager.GetObject<RemoteServerProcess>(processHandle).Evaluate(expression, ref paramsValue, out plan, out planDescriptor, out executeTime, callInfo, GetRemoteProcessCleanupInfo(cleanupInfo));
				planDescriptor.Handle = _handleManager.GetHandle(plan);
				return new DirectEvaluateResult { ExecuteTime = executeTime, ParamData = paramsValue.Data, Result = result, PlanDescriptor = planDescriptor };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public DirectCursorResult OpenCursor(int processHandle, ProcessCleanupInfo cleanupInfo, ProcessCallInfo callInfo, string expression, RemoteParamData paramsValue)
		{
			try
			{
				IRemoteServerExpressionPlan plan;
				PlanDescriptor planDescriptor;
				ProgramStatistics executeTime;
				IRemoteServerCursor cursor = _handleManager.GetObject<RemoteServerProcess>(processHandle).OpenCursor(expression, ref paramsValue, out plan, out planDescriptor, out executeTime, callInfo, GetRemoteProcessCleanupInfo(cleanupInfo));
				planDescriptor.Handle = _handleManager.GetHandle(plan);
				return 
					new DirectCursorResult 
					{
						ExecuteTime = executeTime, 
						ParamData = paramsValue.Data, 
						PlanDescriptor = planDescriptor, 
						CursorDescriptor = new CursorDescriptor(_handleManager.GetHandle(cursor), cursor.Capabilities, cursor.CursorType, cursor.Isolation) 
					};
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public DirectCursorWithFetchResult OpenCursorWithFetch(int processHandle, ProcessCleanupInfo cleanupInfo, ProcessCallInfo callInfo, string expression, RemoteParamData paramsValue, int count)
		{
			try
			{
				IRemoteServerExpressionPlan plan;
				PlanDescriptor planDescriptor;
				ProgramStatistics executeTime;
				Guid[] bookmarks;
				RemoteFetchData fetchData;
				IRemoteServerCursor cursor = _handleManager.GetObject<RemoteServerProcess>(processHandle).OpenCursor(expression, ref paramsValue, out plan, out planDescriptor, out executeTime, callInfo, GetRemoteProcessCleanupInfo(cleanupInfo), out bookmarks, count, out fetchData);
				planDescriptor.Handle = _handleManager.GetHandle(plan);
				return 
					new DirectCursorWithFetchResult
					{
						ExecuteTime = executeTime,
						ParamData = paramsValue.Data,
						PlanDescriptor = planDescriptor,
						CursorDescriptor = new CursorDescriptor(_handleManager.GetHandle(cursor), cursor.Capabilities, cursor.CursorType, cursor.Isolation),
						Bookmarks = bookmarks,
						FetchData = fetchData
					};
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}
		
		public void CloseCursor(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				_handleManager.GetObject<RemoteServerCursor>(cursorHandle).Dispose();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteRowBody Select(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Select(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteRowBody SelectSpecific(int cursorHandle, ProcessCallInfo callInfo, RemoteRowHeader header)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Select(header, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public FetchResult Fetch(int cursorHandle, ProcessCallInfo callInfo, int count, bool skipCurrent)
		{
			try
			{
				Guid[] bookmarks;
				RemoteFetchData fetchData = _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Fetch(out bookmarks, count, skipCurrent, callInfo);
				return new FetchResult { Bookmarks = bookmarks, FetchData = fetchData };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public FetchResult FetchSpecific(int cursorHandle, ProcessCallInfo callInfo, RemoteRowHeader header, int count, bool skipCurrent)
		{
			try
			{
				Guid[] bookmarks;
				RemoteFetchData fetchData = _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Fetch(header, out bookmarks, count, skipCurrent, callInfo);
				return new FetchResult { Bookmarks = bookmarks, FetchData = fetchData };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CursorGetFlags GetFlags(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).GetFlags(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteMoveData MoveBy(int cursorHandle, ProcessCallInfo callInfo, int delta)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).MoveBy(delta, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CursorGetFlags First(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).First(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CursorGetFlags Last(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Last(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CursorGetFlags Reset(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Reset(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void Insert(int cursorHandle, ProcessCallInfo callInfo, RemoteRow row, BitArray valueFlags)
		{
			try
			{
				_handleManager.GetObject<RemoteServerCursor>(cursorHandle).Insert(row, valueFlags, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void Update(int cursorHandle, ProcessCallInfo callInfo, RemoteRow row, BitArray valueFlags)
		{
			try
			{
				_handleManager.GetObject<RemoteServerCursor>(cursorHandle).Update(row, valueFlags, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void Delete(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				_handleManager.GetObject<RemoteServerCursor>(cursorHandle).Delete(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public Guid GetBookmark(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).GetBookmark(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteGotoData GotoBookmark(int cursorHandle, ProcessCallInfo callInfo, Guid bookmark, bool forward)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).GotoBookmark(bookmark, forward, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public int CompareBookmarks(int cursorHandle, ProcessCallInfo callInfo, Guid bookmark1, Guid bookmark2)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).CompareBookmarks(bookmark1, bookmark2, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void DisposeBookmark(int cursorHandle, ProcessCallInfo callInfo, Guid bookmark)
		{
			try
			{
				_handleManager.GetObject<RemoteServerCursor>(cursorHandle).DisposeBookmark(bookmark, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void DisposeBookmarks(int cursorHandle, ProcessCallInfo callInfo, Guid[] bookmarks)
		{
			try
			{
				_handleManager.GetObject<RemoteServerCursor>(cursorHandle).DisposeBookmarks(bookmarks, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public string GetOrder(int cursorHandle)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Order;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteRow GetKey(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).GetKey(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteGotoData FindKey(int cursorHandle, ProcessCallInfo callInfo, RemoteRow key)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).FindKey(key, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CursorGetFlags FindNearest(int cursorHandle, ProcessCallInfo callInfo, RemoteRow key)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).FindNearest(key, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteGotoData Refresh(int cursorHandle, ProcessCallInfo callInfo, RemoteRow row)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Refresh(row, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public int GetRowCount(int cursorHandle, ProcessCallInfo callInfo)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).RowCount(callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteProposeData Default(int cursorHandle, ProcessCallInfo callInfo, RemoteRowBody row, string column)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Default(row, column, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteProposeData Change(int cursorHandle, ProcessCallInfo callInfo, RemoteRowBody oldRow, RemoteRowBody newRow, string column)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Change(oldRow, newRow, column, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public RemoteProposeData Validate(int cursorHandle, ProcessCallInfo callInfo, RemoteRowBody oldRow, RemoteRowBody newRow, string column)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerCursor>(cursorHandle).Validate(oldRow, newRow, column, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public ScriptDescriptor PrepareScript(int processHandle, string script, DebugLocator locator)
		{
			try
			{
				RemoteServerScript localScript = (RemoteServerScript)_handleManager.GetObject<RemoteServerProcess>(processHandle).PrepareScript(script, locator);
				ScriptDescriptor descriptor = new ScriptDescriptor(_handleManager.GetHandle(localScript));
				foreach (Exception exception in localScript.Messages)
					descriptor.Messages.Add(DataphorFaultUtility.ExceptionToFault(exception is DataphorException ? (DataphorException)exception : new DataphorException(exception)));
				foreach (RemoteServerBatch batch in localScript.Batches)
					descriptor.Batches.Add(new BatchDescriptor(_handleManager.GetHandle(batch), batch.IsExpression(), batch.Line));
				return descriptor;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void UnprepareScript(int scriptHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerScript>(scriptHandle).Unprepare();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void ExecuteScript(int processHandle, ProcessCallInfo callInfo, string script)
		{
			try
			{
				_handleManager.GetObject<RemoteServerProcess>(processHandle).ExecuteScript(script, callInfo);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public string GetBatchText(int batchHandle)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerBatch>(batchHandle).GetText();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public PlanDescriptor PrepareBatch(int batchHandle, RemoteParam[] paramsValue)
		{
			try
			{
				RemoteServerBatch batch = _handleManager.GetObject<RemoteServerBatch>(batchHandle);
				if (batch.IsExpression())
				{
					PlanDescriptor descriptor;
					int handle = _handleManager.GetHandle(batch.PrepareExpression(paramsValue, out descriptor));
					descriptor.Handle = handle;
					return descriptor;
				}
				else
				{
					PlanDescriptor descriptor;
					int handle = _handleManager.GetHandle(batch.PrepareStatement(paramsValue, out descriptor));
					descriptor.Handle = handle;
					return descriptor;
				}
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void UnprepareBatch(int planHandle)
		{
			try
			{
				_handleManager.GetObject<RemoteServerPlan>(planHandle).Unprepare();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public ExecuteResult ExecuteBatch(int batchHandle, ProcessCallInfo callInfo, RemoteParamData paramsValue)
		{
			try
			{
				_handleManager.GetObject<RemoteServerBatch>(batchHandle).Execute(ref paramsValue, callInfo);
				return new ExecuteResult { ExecuteTime = new ProgramStatistics(), ParamData = paramsValue.Data };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public CatalogResult GetCatalog(int processHandle, string name)
		{
			try
			{
				long cacheTimeStamp;
				long clientCacheTimeStamp;
				bool cacheChanged;
				string catalog = _handleManager.GetObject<RemoteServerProcess>(processHandle).GetCatalog(name, out cacheTimeStamp, out clientCacheTimeStamp, out cacheChanged);
				return new CatalogResult { Catalog = catalog, CacheTimeStamp = cacheTimeStamp, ClientCacheTimeStamp = clientCacheTimeStamp, CacheChanged = cacheChanged };
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public string GetClassName(int processHandle, string className)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerProcess>(processHandle).GetClassName(className);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public ServerFileInfo[] GetFileNames(int processHandle, string className, string environment)
		{
			try
			{
				return _handleManager.GetObject<RemoteServerProcess>(processHandle).GetFileNames(className, environment);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public int GetFile(int processHandle, string libraryName, string fileName)
		{
			try
			{
				return _handleManager.GetHandle(_handleManager.GetObject<RemoteServerProcess>(processHandle).GetFile(libraryName, fileName));
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public StreamID AllocateStream(int processHandle)
		{
			try
			{
				return _handleManager.GetObject<IStreamManager>(processHandle).Allocate();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public StreamID ReferenceStream(int processHandle, StreamID streamID)
		{
			try
			{
				return _handleManager.GetObject<IStreamManager>(processHandle).Reference(streamID);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void DeallocateStream(int processHandle, StreamID streamID)
		{
			try
			{
				_handleManager.GetObject<IStreamManager>(processHandle).Deallocate(streamID);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public int OpenStream(int processHandle, StreamID streamID, LockMode lockMode)
		{
			try
			{
				return _handleManager.GetHandle(_handleManager.GetObject<IStreamManager>(processHandle).Open(streamID, lockMode));
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void CloseStream(int streamHandle)
		{
			try
			{
				_handleManager.ReleaseObject<Stream>(streamHandle).Close();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public long GetStreamLength(int streamHandle)
		{
			try
			{
				return _handleManager.GetObject<Stream>(streamHandle).Length;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void SetStreamLength(int streamHandle, long tempValue)
		{
			try
			{
				_handleManager.GetObject<Stream>(streamHandle).SetLength(tempValue);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public long GetStreamPosition(int streamHandle)
		{
			try
			{
				return _handleManager.GetObject<Stream>(streamHandle).Position;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void SetStreamPosition(int streamHandle, long position)
		{
			try
			{
				_handleManager.GetObject<Stream>(streamHandle).Position = position;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void FlushStream(int streamHandle)
		{
			try
			{
				_handleManager.GetObject<Stream>(streamHandle).Flush();
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public byte[] ReadStream(int streamHandle, int count)
		{
			try
			{
				byte[] result = new byte[count];
				int ARead = _handleManager.GetObject<Stream>(streamHandle).Read(result, 0, count);
				if (ARead == count)
					return result;
				
				byte[] readResult = new byte[ARead];
				Array.Copy(result, readResult, ARead);
				return readResult;
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public long SeekStream(int streamHandle, long offset, SeekOrigin origin)
		{
			try
			{
				return _handleManager.GetObject<Stream>(streamHandle).Seek(offset, origin);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		public void WriteStream(int streamHandle, byte[] data)
		{
			try
			{
				_handleManager.GetObject<Stream>(streamHandle).Write(data, 0, data.Length);
			}
			catch (DataphorException exception)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(exception), exception.Message);
			}
		}

		#endregion
	}
}
