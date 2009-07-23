/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalProcess : LocalServerChildObject, IServerProcess
    {
		public const int CStreamManagerID = 10;

		public LocalProcess(LocalSession ASession, ProcessInfo AProcessInfo, int AProcessID, IRemoteServerProcess AProcess) : base()
		{
			FSession = ASession;
			FProcess = AProcess;
			FProcessID = AProcessID;
			FProcessInfo = AProcessInfo;
			FStreamManager = new LocalStreamManager((IStreamManager)FProcess);
			FInternalProcess = (ServerProcess)FSession.FInternalSession.StartProcess(new ProcessInfo(FSession.FInternalSession.SessionInfo));
			FSession.FServer.OnCacheCleared += new CacheClearedEvent(CacheCleared);
			FSession.FServer.OnCacheClearing += new CacheClearedEvent(CacheClearing);
			CacheCleared(FSession.FServer);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if ((FSession != null) && (FSession.FServer != null))
			{
				if (FSession.FServer.Catalog != null)
					FSession.FServer.Catalog.ClassLoader.OnMiss -= new Schema.ClassLoaderMissedEvent(ClassLoaderMissed);
				FSession.FServer.OnCacheClearing -= new CacheClearedEvent(CacheClearing);
				FSession.FServer.OnCacheCleared -= new CacheClearedEvent(CacheCleared);
			}
			
			if (FStreamManager != null)
			{
				FStreamManager.Dispose();
				FStreamManager = null;
			}
			
			if (FInternalProcess != null)
			{
				if ((FSession != null) && (FSession.FInternalSession != null))
					FSession.FInternalSession.StopProcess(FInternalProcess);
				FInternalProcess = null;
			}
			
			FProcess = null;
			FProcessID = -1;
			FSession = null;
			base.Dispose(ADisposing);
		}
		
		private int FProcessID;
		public int ProcessID { get { return FProcessID; } }
		
		private ProcessInfo FProcessInfo;
		public ProcessInfo ProcessInfo { get { return FProcessInfo; } }
		
		public ServerProcess GetServerProcess()
		{
			return FInternalProcess;
		}
		
		private IRemoteServerProcess FProcess;
		public IRemoteServerProcess RemoteProcess { get { return FProcess; } }
		
		private Schema.DataTypes FDataTypes;
		public Schema.DataTypes DataTypes { get { return FDataTypes; } }
		
		private void ClassLoaderMissed(Schema.ClassLoader AClassLoader, ClassDefinition AClassDefinition)
		{
			if ((FSession != null) && (FSession.FServer != null))
				FSession.FServer.ClassLoaderMissed(this, AClassLoader, AClassDefinition);
		}
		
		public object CreateObject(ClassDefinition AClassDefinition, object[] AArguments)
		{
			return FSession.FServer.Catalog.ClassLoader.CreateObject(AClassDefinition, AArguments);
		}
		
		public Type CreateType(ClassDefinition AClassDefinition)
		{
			return FSession.FServer.Catalog.ClassLoader.CreateType(AClassDefinition);
		}
		
		private void CacheClearing(LocalServer AServer)
		{
			AServer.FInternalServer.Catalog.ClassLoader.OnMiss -= new Schema.ClassLoaderMissedEvent(ClassLoaderMissed);
		}
		
		private void CacheCleared(LocalServer AServer)
		{
			if (FDataTypes != null)
				FDataTypes.OnCatalogLookupFailed -= new Schema.CatalogLookupFailedEvent(CatalogLookupFailed);
			FDataTypes = new Schema.DataTypes(AServer.FInternalServer.Catalog);
			FDataTypes.OnCatalogLookupFailed += new Schema.CatalogLookupFailedEvent(CatalogLookupFailed);
			
			FSession.FServer.Catalog.ClassLoader.OnMiss += new Schema.ClassLoaderMissedEvent(ClassLoaderMissed);
		}
		
		private void CatalogLookupFailed(Schema.Catalog ACatalog, string AName)
		{
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Retrieving catalog for '{0}'.", AName));
			#endif
			
			// A cache miss forces the retrieval of the datatype and its dependencies from the remote server process
			long LCacheTimeStamp;
			long LClientCacheTimeStamp;
			bool LCacheChanged;
			string LString = FProcess.GetCatalog(AName, out LCacheTimeStamp, out LClientCacheTimeStamp, out LCacheChanged);
		
			// Ensure that the local cache is consistent before adding to the cache
			if (LCacheChanged)
			{
				try
				{
					FSession.FServer.WaitForCacheTimeStamp(this, LClientCacheTimeStamp - 1);
					FSession.FServer.AcquireCacheLock(this, LockMode.Exclusive);
					try
					{
						FSession.FServer.EnsureCacheConsistent(LCacheTimeStamp);
						if (LString != String.Empty)
						{
							IServerScript LScript = ((IServerProcess)FInternalProcess).PrepareScript(LString);
							try
							{
								LScript.Execute(null);
							}
							finally
							{
								((IServerProcess)FInternalProcess).UnprepareScript(LScript);
							}
						}
					}
					finally
					{
						FSession.FServer.ReleaseCacheLock(this, LockMode.Exclusive);
					}
				}
				catch (Exception E)
				{
					// Notify the server that the client cache is out of sync
					Execute(".System.UpdateTimeStamps();", null);
					E = new ServerException(ServerException.Codes.CacheDeserializationError, E, LClientCacheTimeStamp);
					FSession.FServer.FInternalServer.LogError(E);
					throw E;
				}
				finally
				{
					FSession.FServer.SetCacheTimeStamp(this, LClientCacheTimeStamp);
				}
			}
		}
		
		protected internal ServerProcess FInternalProcess;

		protected internal LocalStreamManager FStreamManager;

		protected internal LocalSession FSession;		
		public IServerSession Session { get { return FSession; } }
		
		StreamID IStreamManager.Allocate()
		{
			return FStreamManager.Allocate();
		}
		
		StreamID IStreamManager.Reference(StreamID AStreamID)
		{
			return FStreamManager.Reference(AStreamID);
		}
		
		void IStreamManager.Deallocate(StreamID AStreamID)
		{
			FStreamManager.Deallocate(AStreamID);
		}
		
		Stream IStreamManager.Open(StreamID AStreamID, LockMode ALockMode)
		{
			return FStreamManager.Open(AStreamID, ALockMode);
		}
		
		IRemoteStream IStreamManager.OpenRemote(StreamID AStreamID, LockMode ALockMode)
		{
			return FStreamManager.OpenRemote(AStreamID, ALockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			FStreamManager.Close(AStreamID);
		}
		#endif

		// Parameter Translation
		public RemoteParam[] DataParamsToRemoteParams(DataParams AParams)
		{
			int LParamCount = AParams != null ? AParams.Count : 0;
			if (LParamCount > 0)
			{
				RemoteParam[] LParams= new RemoteParam[LParamCount];

				for (int LIndex = 0; LIndex < LParamCount; LIndex++)
				{
					LParams[LIndex].Name = AParams[LIndex].Name;
					LParams[LIndex].TypeName = AParams[LIndex].DataType.Name;
					LParams[LIndex].Modifier = (byte)AParams[LIndex].Modifier;//hack: to fix fixup error
				}
				return LParams;
			}
			else
				return null;
		}
		
		public RemoteParamData DataParamsToRemoteParamData(DataParams AParams)
		{
			int LParamCount = AParams != null ? AParams.Count : 0;
			if (LParamCount > 0)
			{
				Schema.RowType LRowType = new Schema.RowType();
				if (AParams != null)
					foreach (DataParam LParam in AParams)
						LRowType.Columns.Add(new Schema.Column(LParam.Name, LParam.DataType));
				using (Row LRow = new Row(this, LRowType))
				{
					LRow.ValuesOwned = false;
					RemoteParamData LParams = new RemoteParamData();
					LParams.Params = new RemoteParam[LParamCount];
					for (int LIndex = 0; LIndex < LParamCount; LIndex++)
					{
						LParams.Params[LIndex].Name = AParams[LIndex].Name;
						LParams.Params[LIndex].TypeName = AParams[LIndex].DataType.Name;
						LParams.Params[LIndex].Modifier = (byte)AParams[LIndex].Modifier;//hack: cast to fix fixup error
						if (AParams[LIndex].Value != null)
							LRow[LIndex] = AParams[LIndex].Value;
					}
					EnsureOverflowReleased(LRow);
					LParams.Data.Data = LRow.AsPhysical;
					return LParams;
				}
			}
			else	// optimization
			{
				return new RemoteParamData();
			}
		}
		
		public void RemoteParamDataToDataParams(DataParams AParams, RemoteParamData ARemoteParams)
		{
			if ((AParams != null) && (AParams.Count > 0))
			{
				Schema.RowType LRowType = new Schema.RowType();
				foreach (DataParam LParam in AParams)
					LRowType.Columns.Add(new Schema.Column(LParam.Name, LParam.DataType));
				using (Row LRow = new Row(this, LRowType))
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = ARemoteParams.Data.Data;
					for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
						if (AParams[LIndex].Modifier != Modifier.In)
						{
							if (LRow.HasValue(LIndex))
								AParams[LIndex].Value = DataValue.CopyValue(FInternalProcess, LRow[LIndex]);
							else
								AParams[LIndex].Value = null;
						}
				}
			}
		}
        
		public void EnsureOverflowConsistent(Row ARow)
		{
			ArrayList LList = new ArrayList();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				if (ARow.HasNonNativeValue(LIndex))
					LList.Add(ARow.GetNonNativeStreamID(LIndex));

			StreamID[] LStreamIDArray = new StreamID[LList.Count];
			for (int LIndex = 0; LIndex < LStreamIDArray.Length; LIndex++)
				LStreamIDArray[LIndex] = (StreamID)LList[LIndex];

			FStreamManager.FlushStreams(LStreamIDArray);
		}

		public void EnsureOverflowReleased(Row ARow)
		{
			ArrayList LList = new ArrayList();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				if (ARow.HasNonNativeValue(LIndex)) // TODO: This won't work for non-scalar-valued attributes
					LList.Add(ARow.GetNonNativeStreamID(LIndex));

			StreamID[] LStreamIDArray = new StreamID[LList.Count];
			for (int LIndex = 0; LIndex < LStreamIDArray.Length; LIndex++)
				LStreamIDArray[LIndex] = (StreamID)LList[LIndex];

			FStreamManager.ReleaseStreams(LStreamIDArray);
		}
		
		private ArrayList FUnprepareList = new ArrayList();

		private void ReportCleanup(IRemoteServerPlan APlan)
		{
			FUnprepareList.Add(APlan);
		}
		
		private void ClearCleanup(IRemoteServerPlan APlan)
		{
			for (int LIndex = 0; LIndex < FUnprepareList.Count; LIndex++)
				if (Object.ReferenceEquals(APlan, FUnprepareList[LIndex]))
				{
					FUnprepareList.RemoveAt(LIndex);
					break;
				}
		}
		
		private ProcessCleanupInfo GetProcessCleanupInfo()
		{
			ProcessCleanupInfo LInfo = new ProcessCleanupInfo();
			LInfo.UnprepareList = new RemoteServerPlan[FUnprepareList.Count];
			for (int LIndex = 0; LIndex < FUnprepareList.Count; LIndex++)
				LInfo.UnprepareList[LIndex] = (IRemoteServerPlan)FUnprepareList[LIndex];
				
			FUnprepareList.Clear();
			
			return LInfo;
		}
		
		private ArrayList FTransactionList = new ArrayList();
		
		internal ProcessCallInfo GetProcessCallInfo()
		{
			ProcessCallInfo LInfo = new ProcessCallInfo();
			LInfo.TransactionList = new IsolationLevel[FTransactionList.Count];
			for (int LIndex = 0; LIndex < FTransactionList.Count; LIndex++)
				LInfo.TransactionList[LIndex] = (IsolationLevel)FTransactionList[LIndex];
				
			FTransactionList.Clear();
			
			return LInfo;
		}
		
        /// <summary> Prepares the given statement for execution. </summary>
        /// <param name='AStatement'> A single valid Dataphor statement to prepare. </param>
        /// <returns> An <see cref="IServerStatementPlan"/> instance for the prepared statement. </returns>
        public IServerStatementPlan PrepareStatement(string AStatement, DataParams AParams)
        {
			PlanDescriptor LPlanDescriptor;
			IRemoteServerStatementPlan LPlan = FProcess.PrepareStatement(AStatement, DataParamsToRemoteParams(AParams), out LPlanDescriptor, GetProcessCleanupInfo());
			return new LocalStatementPlan(this, LPlan, LPlanDescriptor);
		}
        
        /// <summary> Unprepares a statement plan. </summary>
        /// <param name="APlan"> A reference to a plan object returned from a call to PrepareStatement. </param>
        public void UnprepareStatement(IServerStatementPlan APlan)
        {
			try
			{
				// The plan will be unprepared on the next prepare call
				ReportCleanup(((LocalStatementPlan)APlan).RemotePlan);
				//FProcess.UnprepareStatement(((LocalStatementPlan)APlan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here 
			}
			((LocalStatementPlan)APlan).Dispose();
		}
		
		public void Execute(string AStatement, DataParams AParams)
		{
			RemoteParamData LParamData = DataParamsToRemoteParamData(AParams);
			FProcess.Execute(AStatement, ref LParamData, GetProcessCallInfo(), GetProcessCleanupInfo());
			RemoteParamDataToDataParams(AParams, LParamData);
		}
		
        /// <summary> Prepares the given expression for selection. </summary>
        /// <param name='AExpression'> A single valid Dataphor expression to prepare. </param>
        /// <returns> An <see cref="IServerExpressionPlan"/> instance for the prepared expression. </returns>
        public IServerExpressionPlan PrepareExpression(string AExpression, DataParams AParams)
        {
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} preparing expression '{1}'.", Thread.CurrentThread.GetHashCode(), AExpression));
			#endif
			
			PlanDescriptor LPlanDescriptor;
			IRemoteServerExpressionPlan LPlan = FProcess.PrepareExpression(AExpression, DataParamsToRemoteParams(AParams), out LPlanDescriptor, GetProcessCleanupInfo());
			return new LocalExpressionPlan(this, LPlan, LPlanDescriptor, AParams);
		}
		
        /// <summary> Unprepares an expression plan. </summary>
        /// <param name="APlan"> A reference to a plan object returned from a call to PrepareExpression. </param>
        public void UnprepareExpression(IServerExpressionPlan APlan)
        {
			try
			{
				// The plan will be unprepared on the next prepare call
				ReportCleanup(((LocalExpressionPlan)APlan).RemotePlan);
			}
			catch
			{
				// do nothing here as this indicates that the plan has been disconnected at the server
			}
			((LocalExpressionPlan)APlan).Dispose();
		}
		
		public DataValue Evaluate(string AExpression, DataParams AParams)
		{
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} evaluating expression '{1}'.", Thread.CurrentThread.GetHashCode(), AExpression));
			#endif
			
			RemoteParamData LParams = DataParamsToRemoteParamData(AParams);
			IRemoteServerExpressionPlan LPlan;
			PlanDescriptor LPlanDescriptor;
			byte[] LResult = FProcess.Evaluate(AExpression, ref LParams, out LPlan, out LPlanDescriptor, GetProcessCallInfo(), GetProcessCleanupInfo());
			RemoteParamDataToDataParams(AParams, LParams);
			
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} expression evaluated.", Thread.CurrentThread.GetHashCode()));
			#endif
			
			LocalExpressionPlan LLocalPlan = new LocalExpressionPlan(this, LPlan, LPlanDescriptor, AParams);
			try
			{
				return LResult == null ? null : DataValue.FromPhysical(this, LLocalPlan.DataType, LResult, 0);
			}
			finally
			{
				UnprepareExpression(LLocalPlan);
			}
		}
		
		public IServerCursor OpenCursor(string AExpression, DataParams AParams)
		{
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} opening cursor '{1}'.", Thread.CurrentThread.GetHashCode(), AExpression));
			#endif
			
			RemoteParamData LParams = DataParamsToRemoteParamData(AParams);
			IRemoteServerExpressionPlan LPlan;
			IRemoteServerCursor LCursor;
			PlanDescriptor LDescriptor;
			LocalExpressionPlan LLocalPlan;
			LocalCursor LLocalCursor;
			
			if (ProcessInfo.FetchAtOpen && (ProcessInfo.FetchCount > 1))
			{
				Guid[] LBookmarks;
				RemoteFetchData LFetchData;
				LCursor = FProcess.OpenCursor(AExpression, ref LParams, out LPlan, out LDescriptor, GetProcessCallInfo(), GetProcessCleanupInfo(), out LBookmarks, ProcessInfo.FetchCount, out LFetchData);
				RemoteParamDataToDataParams(AParams, LParams);
				LLocalPlan = new LocalExpressionPlan(this, LPlan, LDescriptor, AParams);
				LLocalCursor = new LocalCursor(LLocalPlan, LCursor);
				LLocalCursor.ProcessFetchData(LFetchData, LBookmarks, true);
			}
			else
			{
				LCursor = FProcess.OpenCursor(AExpression, ref LParams, out LPlan, out LDescriptor, GetProcessCallInfo(), GetProcessCleanupInfo());
				RemoteParamDataToDataParams(AParams, LParams);
				LLocalPlan = new LocalExpressionPlan(this, LPlan, LDescriptor, AParams);
				LLocalCursor = new LocalCursor(LLocalPlan, LCursor);
			}
			return LLocalCursor;
		}
		
		public void CloseCursor(IServerCursor ACursor)
		{
			IServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor);
			}
			finally
			{
				UnprepareExpression(LPlan);
			}
		}
		
		public IServerScript PrepareScript(string AScript)
		{
			return new LocalScript(this, FProcess.PrepareScript(AScript));
		}
		
		public void UnprepareScript(IServerScript AScript)
		{
			try
			{
				FProcess.UnprepareScript(((LocalScript)AScript).RemoteScript);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalScript)AScript).Dispose();
		}
		
		public void ExecuteScript(string AScript)
		{
			FProcess.ExecuteScript(AScript, GetProcessCallInfo());
		}

        /// <summary>Begins a new transaction on this process.  Transactions may be nested.</summary>
        public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			FTransactionList.Add(AIsolationLevel);
			//FProcess.BeginTransaction(AIsolationLevel);
		}
		
		public void PrepareTransaction()
		{
			// If the current transaction has not been started on the server side, preparing it will not do anything
			if (FTransactionList.Count == 0)
				FProcess.PrepareTransaction();
		}
        
        /// <summary>
        ///     Commits the currently active transaction.  
        ///     Reduces the transaction nesting level by one.  
        ///     Will raise if no transaction is currently active.
        /// </summary>
        public void CommitTransaction()
        {
			if (FTransactionList.Count > 0)
				FTransactionList.RemoveAt(FTransactionList.Count - 1);
			else
				FProcess.CommitTransaction();
		}
        
        /// <summary>
        ///     Rolls back the currently active transaction.
        ///     Reduces the transaction nesting level by one.
        ///     Will raise if no transaction is currently active.
        /// </summary>
        public void RollbackTransaction()
        {
			if (FTransactionList.Count > 0)
				FTransactionList.RemoveAt(FTransactionList.Count - 1);
			else
				FProcess.RollbackTransaction();
		}
        
        /// <value>Returns whether the process currently has an active transaction.</value>
        public bool InTransaction { get { return (FTransactionList.Count > 0) || FProcess.InTransaction; } }
        
        /// <value>Returns the number of currently active transactions on the current process.</value>
        public int TransactionCount { get { return FTransactionList.Count + FProcess.TransactionCount; } }
		
		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert)
		{
			return FProcess.BeginApplicationTransaction(AShouldJoin, AIsInsert, GetProcessCallInfo());
		}
		
		public void PrepareApplicationTransaction(Guid AID)
		{
			FProcess.PrepareApplicationTransaction(AID, GetProcessCallInfo());
		}
		
		public void CommitApplicationTransaction(Guid AID)
		{
			FProcess.CommitApplicationTransaction(AID, GetProcessCallInfo());
		}
		
		public void RollbackApplicationTransaction(Guid AID)
		{
			FProcess.RollbackApplicationTransaction(AID, GetProcessCallInfo());
		}
		
		public Guid ApplicationTransactionID { get { return FProcess.ApplicationTransactionID; } }

		public void JoinApplicationTransaction(Guid AID, bool AIsInsert)
		{
			FProcess.JoinApplicationTransaction(AID, AIsInsert, GetProcessCallInfo());
		}
		
		public void LeaveApplicationTransaction()
		{
			FProcess.LeaveApplicationTransaction(GetProcessCallInfo());
		}
    }
}
