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

	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class DataphorService : IDataphorService, IDisposable
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
		
		#region IDisposable Members

		public void Dispose()
		{
			if (FConnectionManager != null)
			{
				FConnectionManager.Dispose();
				FConnectionManager = null;
			}
		}

		#endregion

		#region IDataphorService Members
		
		public string GetServerName()
		{
			try
			{
				return FServer.Name;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public long GetCacheTimeStamp()
		{
			try
			{
				return FServer.CacheTimeStamp;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public long GetDerivationTimeStamp()
		{
			try
			{
				return FServer.DerivationTimeStamp;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public int OpenConnection(string AConnectionName, string AHostName)
		{
			try
			{
				return FHandleManager.GetHandle(FRemoteServer.Establish(AConnectionName, AHostName));
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public void PingConnection(int AConnectionHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerConnection>(AConnectionHandle).Ping();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public void CloseConnection(int AConnectionHandle)
		{
			try
			{
				FRemoteServer.Relinquish(FHandleManager.GetObject<RemoteServerConnection>(AConnectionHandle));
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public SessionDescriptor Connect(int AConnectionHandle, SessionInfo ASessionInfo)
		{
			try
			{
				RemoteServerSession LSession = (RemoteServerSession)FHandleManager.GetObject<RemoteServerConnection>(AConnectionHandle).Connect(ASessionInfo);
				return new SessionDescriptor(FHandleManager.GetHandle(LSession), LSession.SessionID);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void Disconnect(int ASessionHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerSession>(ASessionHandle).Dispose();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public ProcessDescriptor StartProcess(int ASessionHandle, ProcessInfo AProcessInfo)
		{
			try
			{
				int LProcessID;
				return 
					new ProcessDescriptor
					(
						FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerSession>(ASessionHandle).StartProcess(AProcessInfo, out LProcessID)), 
						LProcessID
					);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void StopProcess(int AProcessHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).Stop();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void BeginTransaction(int AProcessHandle, IsolationLevel AIsolationLevel)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).BeginTransaction(AIsolationLevel);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void PrepareTransaction(int AProcessHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareTransaction();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void CommitTransaction(int AProcessHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).CommitTransaction();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void RollbackTransaction(int AProcessHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).RollbackTransaction();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public int GetTransactionCount(int AProcessHandle)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).TransactionCount;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public Guid BeginApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, bool AShouldJoin, bool AIsInsert)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).BeginApplicationTransaction(AShouldJoin, AIsInsert, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void PrepareApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareApplicationTransaction(AID, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void CommitApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).CommitApplicationTransaction(AID, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void RollbackApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).RollbackApplicationTransaction(AID, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public Guid GetApplicationTransactionID(int AProcessHandle)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).ApplicationTransactionID;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void JoinApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID, bool AIsInsert)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).JoinApplicationTransaction(AID, AIsInsert, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void LeaveApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).LeaveApplicationTransaction(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		private RemoteProcessCleanupInfo GetRemoteProcessCleanupInfo(ProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				RemoteProcessCleanupInfo LCleanupInfo = new RemoteProcessCleanupInfo();
				LCleanupInfo.UnprepareList = new IRemoteServerPlan[ACleanupInfo.UnprepareList.Length];
				for (int LIndex = 0; LIndex < ACleanupInfo.UnprepareList.Length; LIndex++)
					LCleanupInfo.UnprepareList[LIndex] = FHandleManager.GetObject<RemoteServerPlan>(ACleanupInfo.UnprepareList[LIndex]);
				return LCleanupInfo;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public PlanDescriptor PrepareStatement(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AStatement, RemoteParam[] AParams, DebugLocator ALocator)
		{
			try
			{
				PlanDescriptor LDescriptor;
				// It seems to me we should be able to perform the assignment directly to LDescriptor because it will be assigned by the out
				// evaluation in the PrepareStatement call, but the C# compiler is complaining that this is a use of unassigned local variable.
				//LDescriptor.Handle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareStatement(AStatement, AParams, ALocator, out LDescriptor, ACleanupInfo));
				int LHandle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareStatement(AStatement, AParams, ALocator, out LDescriptor, GetRemoteProcessCleanupInfo(ACleanupInfo)));
				LDescriptor.Handle = LHandle;
				return LDescriptor;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public ExecuteResult ExecutePlan(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams)
		{
			try
			{
				ProgramStatistics LExecuteTime;
				FHandleManager.GetObject<RemoteServerStatementPlan>(APlanHandle).Execute(ref AParams, out LExecuteTime, ACallInfo);
				return new ExecuteResult() { ExecuteTime = LExecuteTime, ParamData = AParams.Data };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void UnprepareStatement(int APlanHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerStatementPlan>(APlanHandle).Unprepare();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public ExecuteResult ExecuteStatement(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AStatement, RemoteParamData AParams)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).Execute(AStatement, ref AParams, ACallInfo, GetRemoteProcessCleanupInfo(ACleanupInfo));
				return new ExecuteResult() { ExecuteTime = new ProgramStatistics(), ParamData = AParams.Data };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public PlanDescriptor PrepareExpression(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AExpression, RemoteParam[] AParams, DebugLocator ALocator)
		{
			try
			{
				PlanDescriptor LDescriptor;
				int LHandle = FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareExpression(AExpression, AParams, ALocator, out LDescriptor, GetRemoteProcessCleanupInfo(ACleanupInfo)));
				LDescriptor.Handle = LHandle;
				return LDescriptor;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public EvaluateResult EvaluatePlan(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams)
		{
			try
			{
				ProgramStatistics LExecuteTime;
				byte[] LResult = FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Evaluate(ref AParams, out LExecuteTime, ACallInfo);
				return new EvaluateResult() { ExecuteTime = LExecuteTime, ParamData = AParams.Data, Result = LResult };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public CursorResult OpenPlanCursor(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams)
		{
			try
			{
				ProgramStatistics LExecuteTime;
				RemoteServerCursor LCursor = (RemoteServerCursor)FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Open(ref AParams, out LExecuteTime, ACallInfo);
				CursorDescriptor LDescriptor = new CursorDescriptor(FHandleManager.GetHandle(LCursor), LCursor.Capabilities, LCursor.CursorType, LCursor.Isolation);
				return new CursorResult() { ExecuteTime = LExecuteTime, ParamData = AParams.Data, CursorDescriptor = LDescriptor };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CursorWithFetchResult OpenPlanCursorWithFetch(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams, int ACount)
		{
			try
			{
				ProgramStatistics LExecuteTime;
				Guid[] LBookmarks;
				RemoteFetchData LFetchData;
				RemoteServerCursor LCursor = (RemoteServerCursor)FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Open(ref AParams, out LExecuteTime, out LBookmarks, ACount, out LFetchData, ACallInfo);
				CursorDescriptor LDescriptor = new CursorDescriptor(FHandleManager.GetHandle(LCursor), LCursor.Capabilities, LCursor.CursorType, LCursor.Isolation);
				return new CursorWithFetchResult() { ExecuteTime = LExecuteTime, ParamData = AParams.Data, CursorDescriptor = LDescriptor, Bookmarks = LBookmarks, FetchData = LFetchData };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void UnprepareExpression(int APlanHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerExpressionPlan>(APlanHandle).Unprepare();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public DirectEvaluateResult EvaluateExpression(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AExpression, RemoteParamData AParams)
		{
			try
			{
				IRemoteServerExpressionPlan LPlan;
				PlanDescriptor LPlanDescriptor;
				ProgramStatistics LExecuteTime;
				byte[] LResult = FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).Evaluate(AExpression, ref AParams, out LPlan, out LPlanDescriptor, out LExecuteTime, ACallInfo, GetRemoteProcessCleanupInfo(ACleanupInfo));
				LPlanDescriptor.Handle = FHandleManager.GetHandle(LPlan);
				return new DirectEvaluateResult { ExecuteTime = LExecuteTime, ParamData = AParams.Data, Result = LResult, PlanDescriptor = LPlanDescriptor };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public DirectCursorResult OpenCursor(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AExpression, RemoteParamData AParams)
		{
			try
			{
				IRemoteServerExpressionPlan LPlan;
				PlanDescriptor LPlanDescriptor;
				ProgramStatistics LExecuteTime;
				IRemoteServerCursor LCursor = FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).OpenCursor(AExpression, ref AParams, out LPlan, out LPlanDescriptor, out LExecuteTime, ACallInfo, GetRemoteProcessCleanupInfo(ACleanupInfo));
				LPlanDescriptor.Handle = FHandleManager.GetHandle(LPlan);
				return 
					new DirectCursorResult 
					{
						ExecuteTime = LExecuteTime, 
						ParamData = AParams.Data, 
						PlanDescriptor = LPlanDescriptor, 
						CursorDescriptor = new CursorDescriptor(FHandleManager.GetHandle(LCursor), LCursor.Capabilities, LCursor.CursorType, LCursor.Isolation) 
					};
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public DirectCursorWithFetchResult OpenCursorWithFetch(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AExpression, RemoteParamData AParams, int ACount)
		{
			try
			{
				IRemoteServerExpressionPlan LPlan;
				PlanDescriptor LPlanDescriptor;
				ProgramStatistics LExecuteTime;
				Guid[] LBookmarks;
				RemoteFetchData LFetchData;
				IRemoteServerCursor LCursor = FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).OpenCursor(AExpression, ref AParams, out LPlan, out LPlanDescriptor, out LExecuteTime, ACallInfo, GetRemoteProcessCleanupInfo(ACleanupInfo), out LBookmarks, ACount, out LFetchData);
				LPlanDescriptor.Handle = FHandleManager.GetHandle(LPlan);
				return 
					new DirectCursorWithFetchResult
					{
						ExecuteTime = LExecuteTime,
						ParamData = AParams.Data,
						PlanDescriptor = LPlanDescriptor,
						CursorDescriptor = new CursorDescriptor(FHandleManager.GetHandle(LCursor), LCursor.Capabilities, LCursor.CursorType, LCursor.Isolation),
						Bookmarks = LBookmarks,
						FetchData = LFetchData
					};
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public void CloseCursor(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Close();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteRowBody Select(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Select(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteRowBody SelectSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Select(AHeader, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public FetchResult Fetch(int ACursorHandle, ProcessCallInfo ACallInfo, int ACount)
		{
			try
			{
				Guid[] LBookmarks;
				RemoteFetchData LFetchData = FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Fetch(out LBookmarks, ACount, ACallInfo);
				return new FetchResult { Bookmarks = LBookmarks, FetchData = LFetchData };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public FetchResult FetchSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader, int ACount)
		{
			try
			{
				Guid[] LBookmarks;
				RemoteFetchData LFetchData = FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Fetch(AHeader, out LBookmarks, ACount, ACallInfo);
				return new FetchResult { Bookmarks = LBookmarks, FetchData = LFetchData };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CursorGetFlags GetFlags(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GetFlags(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteMoveData MoveBy(int ACursorHandle, ProcessCallInfo ACallInfo, int ADelta)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).MoveBy(ADelta, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CursorGetFlags First(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).First(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CursorGetFlags Last(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Last(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CursorGetFlags Reset(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Reset(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void Insert(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Insert(ARow, AValueFlags, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void Update(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Update(ARow, AValueFlags, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void Delete(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Delete(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public Guid GetBookmark(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GetBookmark(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteGotoData GotoBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark, bool AForward)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GotoBookmark(ABookmark, AForward, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public int CompareBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark1, Guid ABookmark2)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).CompareBookmarks(ABookmark1, ABookmark2, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void DisposeBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).DisposeBookmark(ABookmark, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void DisposeBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid[] ABookmarks)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).DisposeBookmarks(ABookmarks, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public string GetOrder(int ACursorHandle)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Order;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteRow GetKey(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).GetKey(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteGotoData FindKey(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).FindKey(AKey, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CursorGetFlags FindNearest(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).FindNearest(AKey, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteGotoData Refresh(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Refresh(ARow, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public int GetRowCount(int ACursorHandle, ProcessCallInfo ACallInfo)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).RowCount(ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteProposeData Default(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody ARow, string AColumn)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Default(ARow, AColumn, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteProposeData Change(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Change(AOldRow, ANewRow, AColumn, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public RemoteProposeData Validate(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerCursor>(ACursorHandle).Validate(AOldRow, ANewRow, AColumn, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public ScriptDescriptor PrepareScript(int AProcessHandle, string AScript, DebugLocator ALocator)
		{
			try
			{
				RemoteServerScript LScript = (RemoteServerScript)FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).PrepareScript(AScript, ALocator);
				ScriptDescriptor LDescriptor = new ScriptDescriptor(FHandleManager.GetHandle(LScript));
				foreach (Exception LException in LScript.Messages)
					LDescriptor.Messages.Add(DataphorFaultUtility.ExceptionToFault(LException is DataphorException ? (DataphorException)LException : new DataphorException(LException)));
				foreach (RemoteServerBatch LBatch in LScript.Batches)
					LDescriptor.Batches.Add(new BatchDescriptor(FHandleManager.GetHandle(LBatch), LBatch.IsExpression(), LBatch.Line));
				return LDescriptor;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void UnprepareScript(int AScriptHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerScript>(AScriptHandle).Unprepare();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void ExecuteScript(int AProcessHandle, ProcessCallInfo ACallInfo, string AScript)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).ExecuteScript(AScript, ACallInfo);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public string GetBatchText(int ABatchHandle)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerBatch>(ABatchHandle).GetText();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public PlanDescriptor PrepareBatch(int ABatchHandle, RemoteParam[] AParams)
		{
			try
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
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void UnprepareBatch(int APlanHandle)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerPlan>(APlanHandle).Unprepare();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public ExecuteResult ExecuteBatch(int ABatchHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams)
		{
			try
			{
				FHandleManager.GetObject<RemoteServerBatch>(ABatchHandle).Execute(ref AParams, ACallInfo);
				return new ExecuteResult { ExecuteTime = new ProgramStatistics(), ParamData = AParams.Data };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public CatalogResult GetCatalog(int AProcessHandle, string AName)
		{
			try
			{
				long LCacheTimeStamp;
				long LClientCacheTimeStamp;
				bool LCacheChanged;
				string LCatalog = FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetCatalog(AName, out LCacheTimeStamp, out LClientCacheTimeStamp, out LCacheChanged);
				return new CatalogResult { Catalog = LCatalog, CacheTimeStamp = LCacheTimeStamp, ClientCacheTimeStamp = LClientCacheTimeStamp, CacheChanged = LCacheChanged };
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public string GetClassName(int AProcessHandle, string AClassName)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetClassName(AClassName);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public ServerFileInfo[] GetFileNames(int AProcessHandle, string AClassName, string AEnvironment)
		{
			try
			{
				return FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetFileNames(AClassName, AEnvironment);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public int GetFile(int AProcessHandle, string ALibraryName, string AFileName)
		{
			try
			{
				return FHandleManager.GetHandle(FHandleManager.GetObject<RemoteServerProcess>(AProcessHandle).GetFile(ALibraryName, AFileName));
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public StreamID AllocateStream(int AProcessHandle)
		{
			try
			{
				return FHandleManager.GetObject<IStreamManager>(AProcessHandle).Allocate();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public StreamID ReferenceStream(int AProcessHandle, StreamID AStreamID)
		{
			try
			{
				return FHandleManager.GetObject<IStreamManager>(AProcessHandle).Reference(AStreamID);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void DeallocateStream(int AProcessHandle, StreamID AStreamID)
		{
			try
			{
				FHandleManager.GetObject<IStreamManager>(AProcessHandle).Deallocate(AStreamID);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public int OpenStream(int AProcessHandle, StreamID AStreamID, LockMode ALockMode)
		{
			try
			{
				return FHandleManager.GetHandle(FHandleManager.GetObject<IStreamManager>(AProcessHandle).Open(AStreamID, ALockMode));
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void CloseStream(int AStreamHandle)
		{
			try
			{
				FHandleManager.ReleaseObject<Stream>(AStreamHandle).Close();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public long GetStreamLength(int AStreamHandle)
		{
			try
			{
				return FHandleManager.GetObject<Stream>(AStreamHandle).Length;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void SetStreamLength(int AStreamHandle, long AValue)
		{
			try
			{
				FHandleManager.GetObject<Stream>(AStreamHandle).SetLength(AValue);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public long GetStreamPosition(int AStreamHandle)
		{
			try
			{
				return FHandleManager.GetObject<Stream>(AStreamHandle).Position;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void SetStreamPosition(int AStreamHandle, long APosition)
		{
			try
			{
				FHandleManager.GetObject<Stream>(AStreamHandle).Position = APosition;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void FlushStream(int AStreamHandle)
		{
			try
			{
				FHandleManager.GetObject<Stream>(AStreamHandle).Flush();
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public byte[] ReadStream(int AStreamHandle, int ACount)
		{
			try
			{
				byte[] LResult = new byte[ACount];
				int ARead = FHandleManager.GetObject<Stream>(AStreamHandle).Read(LResult, 0, ACount);
				if (ARead == ACount)
					return LResult;
				
				byte[] LReadResult = new byte[ARead];
				Array.Copy(LResult, LReadResult, ARead);
				return LReadResult;
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public long SeekStream(int AStreamHandle, long AOffset, SeekOrigin AOrigin)
		{
			try
			{
				return FHandleManager.GetObject<Stream>(AStreamHandle).Seek(AOffset, AOrigin);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		public void WriteStream(int AStreamHandle, byte[] AData)
		{
			try
			{
				FHandleManager.GetObject<Stream>(AStreamHandle).Write(AData, 0, AData.Length);
			}
			catch (DataphorException LException)
			{
				throw new FaultException<DataphorFault>(DataphorFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}

		#endregion
	}
}
