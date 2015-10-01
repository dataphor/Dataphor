/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Device.Catalog;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalProcess : LocalServerChildObject, IServerProcess
    {
		public const int StreamManagerID = 10;

		public LocalProcess(LocalSession session, ProcessInfo processInfo, int processID, IRemoteServerProcess process) : base()
		{
			_session = session;
			_process = process;
			_processID = processID;
			_processInfo = processInfo;
			_streamManager = new LocalStreamManager((IStreamManager)_process);
			_internalProcess = (ServerProcess)_session._internalSession.StartProcess(new ProcessInfo(_session._internalSession.SessionInfo));
			_session._server.OnCacheCleared += new CacheClearedEvent(CacheCleared);
			_session._server.OnCacheClearing += new CacheClearedEvent(CacheClearing);
			CacheCleared(_session._server);
			_valueManager = new ValueManager(_internalProcess, this);
		}
		
		protected override void Dispose(bool disposing)
		{
			if ((_session != null) && (_session._server != null))
			{
				if (_session._server.Catalog != null)
					_session._server.Catalog.ClassLoader.OnMiss -= new ClassLoaderMissedEvent(ClassLoaderMissed);
				_session._server.OnCacheClearing -= new CacheClearedEvent(CacheClearing);
				_session._server.OnCacheCleared -= new CacheClearedEvent(CacheCleared);
			}
			
			if (_streamManager != null)
			{
				_streamManager.Dispose();
				_streamManager = null;
			}
			
			if (_internalProcess != null)
			{
				if ((_session != null) && (_session._internalSession != null))
					_session._internalSession.StopProcess(_internalProcess);
				_internalProcess = null;
			}
			
			_valueManager = null;
			_process = null;
			_processID = -1;
			_session = null;
			base.Dispose(disposing);
		}
		
		private int _processID;
		public int ProcessID { get { return _processID; } }
		
		private ProcessInfo _processInfo;
		public ProcessInfo ProcessInfo { get { return _processInfo; } }
		
		public ServerProcess GetServerProcess()
		{
			return _internalProcess;
		}
		
		private IRemoteServerProcess _process;
		public IRemoteServerProcess RemoteProcess { get { return _process; } }
		
		private Schema.DataTypes _dataTypes;
		public Schema.DataTypes DataTypes { get { return _dataTypes; } }
		
		private void ClassLoaderMissed(ClassLoader classLoader, CatalogDeviceSession session, ClassDefinition classDefinition)
		{
			if ((_session != null) && (_session._server != null))
				_session._server.ClassLoaderMissed(this, classLoader, classDefinition);
		}
		
		public object CreateObject(ClassDefinition classDefinition, object[] arguments)
		{
			return _session._server.Catalog.ClassLoader.CreateObject(_internalProcess.CatalogDeviceSession, classDefinition, arguments);
		}
		
		public Type CreateType(ClassDefinition classDefinition)
		{
			return _session._server.Catalog.ClassLoader.CreateType(_internalProcess.CatalogDeviceSession, classDefinition);
		}
		
		private void CacheClearing(LocalServer server)
		{
			server._internalServer.Catalog.ClassLoader.OnMiss -= new ClassLoaderMissedEvent(ClassLoaderMissed);
		}
		
		private void CacheCleared(LocalServer server)
		{
			if (_dataTypes != null)
				_dataTypes.OnCatalogLookupFailed -= new Schema.CatalogLookupFailedEvent(CatalogLookupFailed);
			_dataTypes = new Schema.DataTypes(server._internalServer.Catalog);
			_dataTypes.OnCatalogLookupFailed += new Schema.CatalogLookupFailedEvent(CatalogLookupFailed);
			
			_session._server.Catalog.ClassLoader.OnMiss += new ClassLoaderMissedEvent(ClassLoaderMissed);
		}
		
		private void CatalogLookupFailed(Schema.Catalog catalog, string name)
		{
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Retrieving catalog for '{0}'.", AName));
			#endif
			
			// A cache miss forces the retrieval of the datatype and its dependencies from the remote server process
			long cacheTimeStamp;
			long clientCacheTimeStamp;
			bool cacheChanged;
			string stringValue = _process.GetCatalog(name, out cacheTimeStamp, out clientCacheTimeStamp, out cacheChanged);
		
			// Ensure that the local cache is consistent before adding to the cache
			if (cacheChanged)
			{
				try
				{
					_session._server.WaitForCacheTimeStamp(this, clientCacheTimeStamp - 1);
					_session._server.AcquireCacheLock(this, LockMode.Exclusive);
					try
					{
						_session._server.EnsureCacheConsistent(cacheTimeStamp);
						if (stringValue != String.Empty)
						{
							IServerScript script = ((IServerProcess)_internalProcess).PrepareScript(stringValue);
							try
							{
								script.Execute(null);
							}
							finally
							{
								((IServerProcess)_internalProcess).UnprepareScript(script);
							}
						}
					}
					finally
					{
						_session._server.ReleaseCacheLock(this, LockMode.Exclusive);
					}
				}
				catch (Exception E)
				{
					// Notify the server that the client cache is out of sync
					Execute(".System.UpdateTimeStamps();", null);
					E = new ServerException(ServerException.Codes.CacheDeserializationError, E, clientCacheTimeStamp);
					_session._server._internalServer.LogError(E);
					throw E;
				}
				finally
				{
					_session._server.SetCacheTimeStamp(this, clientCacheTimeStamp);
				}
			}
		}
		
		protected internal ServerProcess _internalProcess;

		protected internal LocalStreamManager _streamManager;
		
		private IValueManager _valueManager;
		public IValueManager ValueManager { get { return _valueManager; } }

		protected internal LocalSession _session;		
		public IServerSession Session { get { return _session; } }
		
		StreamID IStreamManager.Allocate()
		{
			return _streamManager.Allocate();
		}
		
		StreamID IStreamManager.Reference(StreamID streamID)
		{
			return _streamManager.Reference(streamID);
		}
		
		void IStreamManager.Deallocate(StreamID streamID)
		{
			_streamManager.Deallocate(streamID);
		}
		
		Stream IStreamManager.Open(StreamID streamID, LockMode lockMode)
		{
			return _streamManager.Open(streamID, lockMode);
		}
		
		IRemoteStream IStreamManager.OpenRemote(StreamID streamID, LockMode lockMode)
		{
			return _streamManager.OpenRemote(streamID, lockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			FStreamManager.Close(AStreamID);
		}
		#endif
		
		public static RemoteParamModifier ModifierToRemoteModifier(Modifier modifier)
		{
			switch (modifier)
			{
				case Modifier.In : return RemoteParamModifier.In;
				case Modifier.Out : return RemoteParamModifier.Out;
				case Modifier.Var : return RemoteParamModifier.Var;
				case Modifier.Const : return RemoteParamModifier.Const;
				default: throw new ArgumentOutOfRangeException("AModifier");
			}
		}

		// Parameter Translation
		public RemoteParam[] DataParamsToRemoteParams(DataParams paramsValue)
		{
			int paramCount = paramsValue != null ? paramsValue.Count : 0;
			if (paramCount > 0)
			{
				RemoteParam[] localParamsValue= new RemoteParam[paramCount];

				for (int index = 0; index < paramCount; index++)
				{
					localParamsValue[index].Name = paramsValue[index].Name;
					localParamsValue[index].TypeName = paramsValue[index].DataType.Name;
					localParamsValue[index].Modifier = ModifierToRemoteModifier(paramsValue[index].Modifier);
				}
				return localParamsValue;
			}
			else
				return null;
		}
		
		public RemoteParamData DataParamsToRemoteParamData(DataParams paramsValue)
		{
			int paramCount = paramsValue != null ? paramsValue.Count : 0;
			if (paramCount > 0)
			{
				Schema.RowType rowType = new Schema.RowType();
				if (paramsValue != null)
					foreach (DataParam param in paramsValue)
						rowType.Columns.Add(new Schema.Column(param.Name, param.DataType));
				using (Row row = new Row(this.ValueManager, rowType))
				{
					row.ValuesOwned = false;
					RemoteParamData localParamsValue = new RemoteParamData();
					localParamsValue.Params = new RemoteParam[paramCount];
					for (int index = 0; index < paramCount; index++)
					{
						localParamsValue.Params[index].Name = paramsValue[index].Name;
						localParamsValue.Params[index].TypeName = paramsValue[index].DataType.Name;
						localParamsValue.Params[index].Modifier = ModifierToRemoteModifier(paramsValue[index].Modifier);
						if (paramsValue[index].Value != null)
							row[index] = paramsValue[index].Value;
					}
					EnsureOverflowReleased(row);
					localParamsValue.Data.Data = row.AsPhysical;
					return localParamsValue;
				}
			}
			else	// optimization
			{
				return new RemoteParamData();
			}
		}
		
		public void RemoteParamDataToDataParams(DataParams paramsValue, RemoteParamData remoteParams)
		{
			if ((paramsValue != null) && (paramsValue.Count > 0))
			{
				Schema.RowType rowType = new Schema.RowType();
				foreach (DataParam param in paramsValue)
					rowType.Columns.Add(new Schema.Column(param.Name, param.DataType));
				using (Row row = new Row(this.ValueManager, rowType))
				{
					row.ValuesOwned = false;
					row.AsPhysical = remoteParams.Data.Data;
					for (int index = 0; index < paramsValue.Count; index++)
						if (paramsValue[index].Modifier != Modifier.In)
						{
							if (row.HasValue(index))
								paramsValue[index].Value = DataValue.CopyValue(_internalProcess.ValueManager, row[index]);
							else
								paramsValue[index].Value = null;
						}
				}
			}
		}
        
		public void EnsureOverflowConsistent(IRow row)
		{
			List<StreamID> list = new List<StreamID>();
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				if (row.HasNonNativeValue(index))
					list.Add(row.GetNonNativeStreamID(index));

			_streamManager.FlushStreams(list.ToArray());
		}

		public void EnsureOverflowReleased(IRow row)
		{
			List<StreamID> list = new List<StreamID>();
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				if (row.HasNonNativeValue(index)) // TODO: This won't work for non-scalar-valued attributes
					list.Add(row.GetNonNativeStreamID(index));

			_streamManager.ReleaseStreams(list.ToArray());
		}

		private List<IRemoteServerPlan> _unprepareList = new List<IRemoteServerPlan>();

		private void ReportCleanup(IRemoteServerPlan plan)
		{
			_unprepareList.Add(plan);
		}
		
		private void ClearCleanup(IRemoteServerPlan plan)
		{
			for (int index = 0; index < _unprepareList.Count; index++)
				if (Object.ReferenceEquals(plan, _unprepareList[index]))
				{
					_unprepareList.RemoveAt(index);
					break;
				}
		}
		
		private RemoteProcessCleanupInfo GetProcessCleanupInfo()
		{
			RemoteProcessCleanupInfo info = 
				new RemoteProcessCleanupInfo()
				{
					UnprepareList = _unprepareList.ToArray()
				};
				
			_unprepareList.Clear();
			
			return info;
		}

		private List<IsolationLevel> _transactionList = new List<IsolationLevel>();
		
		internal ProcessCallInfo GetProcessCallInfo()
		{
			ProcessCallInfo info = 
				new ProcessCallInfo()
				{
					TransactionList = _transactionList.ToArray()
				};
				
			_transactionList.Clear();
			
			return info;
		}
		
		public IServerStatementPlan PrepareStatement(string statement, DataParams paramsValue)
		{
			return PrepareStatement(statement, paramsValue, null);
		}
		
        /// <summary> Prepares the given statement for execution. </summary>
        /// <param name='statement'> A single valid Dataphor statement to prepare. </param>
        /// <returns> An <see cref="IServerStatementPlan"/> instance for the prepared statement. </returns>
        public IServerStatementPlan PrepareStatement(string statement, DataParams paramsValue, DebugLocator locator)
        {
			PlanDescriptor planDescriptor;
			IRemoteServerStatementPlan plan = _process.PrepareStatement(statement, DataParamsToRemoteParams(paramsValue), locator, out planDescriptor, GetProcessCleanupInfo());
			return new LocalStatementPlan(this, plan, planDescriptor);
		}
        
        /// <summary> Unprepares a statement plan. </summary>
        /// <param name="plan"> A reference to a plan object returned from a call to PrepareStatement. </param>
        public void UnprepareStatement(IServerStatementPlan plan)
        {
			try
			{
				// The plan will be unprepared on the next prepare call
				ReportCleanup(((LocalStatementPlan)plan).RemotePlan);
				//FProcess.UnprepareStatement(((LocalStatementPlan)APlan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here 
			}
			((LocalStatementPlan)plan).Dispose();
		}
		
		public void Execute(string statement, DataParams paramsValue)
		{
			RemoteParamData paramData = DataParamsToRemoteParamData(paramsValue);
			_process.Execute(statement, ref paramData, GetProcessCallInfo(), GetProcessCleanupInfo());
			RemoteParamDataToDataParams(paramsValue, paramData);
		}
		
		public IServerExpressionPlan PrepareExpression(string expression, DataParams paramsValue)
		{
			return PrepareExpression(expression, paramsValue, null);
		}
		
        /// <summary> Prepares the given expression for selection. </summary>
        /// <param name='expression'> A single valid Dataphor expression to prepare. </param>
        /// <returns> An <see cref="IServerExpressionPlan"/> instance for the prepared expression. </returns>
        public IServerExpressionPlan PrepareExpression(string expression, DataParams paramsValue, DebugLocator locator)
        {
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} preparing expression '{1}'.", Thread.CurrentThread.GetHashCode(), AExpression));
			#endif
			
			PlanDescriptor planDescriptor;
			IRemoteServerExpressionPlan plan = _process.PrepareExpression(expression, DataParamsToRemoteParams(paramsValue), locator, out planDescriptor, GetProcessCleanupInfo());
			return new LocalExpressionPlan(this, plan, planDescriptor, paramsValue);
		}
		
        /// <summary> Unprepares an expression plan. </summary>
        /// <param name="plan"> A reference to a plan object returned from a call to PrepareExpression. </param>
        public void UnprepareExpression(IServerExpressionPlan plan)
        {
			try
			{
				// The plan will be unprepared on the next prepare call
				ReportCleanup(((LocalExpressionPlan)plan).RemotePlan);
			}
			catch
			{
				// do nothing here as this indicates that the plan has been disconnected at the server
			}
			((LocalExpressionPlan)plan).Dispose();
		}
		
		public IDataValue Evaluate(string expression, DataParams paramsValue)
		{
			#if USECOLLAPSEDEVALUATECALLS
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} evaluating expression '{1}'.", Thread.CurrentThread.GetHashCode(), AExpression));
			#endif
			
			RemoteParamData localParamsValue = DataParamsToRemoteParamData(AParams);
			IRemoteServerExpressionPlan plan;
			PlanDescriptor planDescriptor;
			ProgramStatistics executeTime;
			byte[] result = FProcess.Evaluate(AExpression, ref localParamsValue, out plan, out planDescriptor, out executeTime, GetProcessCallInfo(), GetProcessCleanupInfo());
			RemoteParamDataToDataParams(AParams, localParamsValue);
			
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} expression evaluated.", Thread.CurrentThread.GetHashCode()));
			#endif
			
			LocalExpressionPlan localPlan = new LocalExpressionPlan(this, plan, planDescriptor, AParams);
			try
			{
				return result == null ? null : DataValue.FromPhysical(this.ValueManager, localPlan.DataType, result, 0);
			}
			finally
			{
				UnprepareExpression(localPlan);
			}
			#else
			IServerExpressionPlan localPlan = PrepareExpression(expression, paramsValue);
			try
			{
				return localPlan.Evaluate(paramsValue);
			}
			finally
			{
				UnprepareExpression(localPlan);
			}
			#endif
		}
		
		public IServerCursor OpenCursor(string expression, DataParams paramsValue)
		{
			#if LOGCACHEEVENTS
			FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} opening cursor '{1}'.", Thread.CurrentThread.GetHashCode(), AExpression));
			#endif
			
			RemoteParamData localParamsValue = DataParamsToRemoteParamData(paramsValue);
			IRemoteServerExpressionPlan plan;
			IRemoteServerCursor cursor;
			PlanDescriptor descriptor;
			ProgramStatistics executeTime;
			LocalExpressionPlan localPlan;
			LocalCursor localCursor;
			
			if (ProcessInfo.FetchAtOpen && (ProcessInfo.FetchCount > 1))
			{
				Guid[] bookmarks;
				RemoteFetchData fetchData;
				cursor = _process.OpenCursor(expression, ref localParamsValue, out plan, out descriptor, out executeTime, GetProcessCallInfo(), GetProcessCleanupInfo(), out bookmarks, ProcessInfo.FetchCount, out fetchData);
				RemoteParamDataToDataParams(paramsValue, localParamsValue);
				localPlan = new LocalExpressionPlan(this, plan, descriptor, paramsValue, executeTime);
				localCursor = new LocalCursor(localPlan, cursor);
				localCursor.ProcessFetchData(fetchData, bookmarks, true);
			}
			else
			{
				cursor = _process.OpenCursor(expression, ref localParamsValue, out plan, out descriptor, out executeTime, GetProcessCallInfo(), GetProcessCleanupInfo());
				RemoteParamDataToDataParams(paramsValue, localParamsValue);
				localPlan = new LocalExpressionPlan(this, plan, descriptor, paramsValue, executeTime);
				localCursor = new LocalCursor(localPlan, cursor);
			}
			return localCursor;
		}
		
		public void CloseCursor(IServerCursor cursor)
		{
			IServerExpressionPlan plan = cursor.Plan;
			try
			{
				plan.Close(cursor);
			}
			finally
			{
				UnprepareExpression(plan);
			}
		}
		
		public IServerScript PrepareScript(string script)
		{
			return PrepareScript(script, null);
		}
		
		public IServerScript PrepareScript(string script, DebugLocator locator)
		{
			return new LocalScript(this, _process.PrepareScript(script, locator));
		}
		
		public void UnprepareScript(IServerScript script)
		{
			try
			{
				_process.UnprepareScript(((LocalScript)script).RemoteScript);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalScript)script).Dispose();
		}
		
		public void ExecuteScript(string script)
		{
			_process.ExecuteScript(script, GetProcessCallInfo());
		}

        /// <summary>Begins a new transaction on this process.  Transactions may be nested.</summary>
        public void BeginTransaction(IsolationLevel isolationLevel)
		{
			_transactionList.Add(isolationLevel);
			//FProcess.BeginTransaction(AIsolationLevel);
		}
		
		public void PrepareTransaction()
		{
			// If the current transaction has not been started on the server side, preparing it will not do anything
			if (_transactionList.Count == 0)
				_process.PrepareTransaction();
		}
        
        /// <summary>
        ///     Commits the currently active transaction.  
        ///     Reduces the transaction nesting level by one.  
        ///     Will raise if no transaction is currently active.
        /// </summary>
        public void CommitTransaction()
        {
			if (_transactionList.Count > 0)
				_transactionList.RemoveAt(_transactionList.Count - 1);
			else
				_process.CommitTransaction();
		}
        
        /// <summary>
        ///     Rolls back the currently active transaction.
        ///     Reduces the transaction nesting level by one.
        ///     Will raise if no transaction is currently active.
        /// </summary>
        public void RollbackTransaction()
        {
			if (_transactionList.Count > 0)
				_transactionList.RemoveAt(_transactionList.Count - 1);
			else
				_process.RollbackTransaction();
		}
        
        /// <value>Returns whether the process currently has an active transaction.</value>
        public bool InTransaction { get { return (_transactionList.Count > 0) || _process.InTransaction; } }
        
        /// <value>Returns the number of currently active transactions on the current process.</value>
        public int TransactionCount { get { return _transactionList.Count + _process.TransactionCount; } }
		
		public Guid BeginApplicationTransaction(bool shouldJoin, bool isInsert)
		{
			return _process.BeginApplicationTransaction(shouldJoin, isInsert, GetProcessCallInfo());
		}
		
		public void PrepareApplicationTransaction(Guid iD)
		{
			_process.PrepareApplicationTransaction(iD, GetProcessCallInfo());
		}
		
		public void CommitApplicationTransaction(Guid iD)
		{
			_process.CommitApplicationTransaction(iD, GetProcessCallInfo());
		}
		
		public void RollbackApplicationTransaction(Guid iD)
		{
			_process.RollbackApplicationTransaction(iD, GetProcessCallInfo());
		}
		
		public Guid ApplicationTransactionID { get { return _process.ApplicationTransactionID; } }

		public void JoinApplicationTransaction(Guid iD, bool isInsert)
		{
			_process.JoinApplicationTransaction(iD, isInsert, GetProcessCallInfo());
		}
		
		public void LeaveApplicationTransaction()
		{
			_process.LeaveApplicationTransaction(GetProcessCallInfo());
		}
    }
}
