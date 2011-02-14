/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerProcess
	public class RemoteServerProcess : RemoteServerChildObject, IRemoteServerProcess
	{
		internal RemoteServerProcess(RemoteServerSession session, ServerProcess process) : base()
		{
			_session = session;
			_serverProcess = process;
			AttachServerProcess();
		}
		
		private void AttachServerProcess()
		{
			_serverProcess.Disposed += new EventHandler(FServerProcessDisposed);
		}

		private void FServerProcessDisposed(object sender, EventArgs args)
		{
			DetachServerProcess();
			_serverProcess = null;
			Dispose();
		}
		
		private void DetachServerProcess()
		{
			_serverProcess.Disposed -= new EventHandler(FServerProcessDisposed);
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_serverProcess != null)
				{
					DetachServerProcess();
					_serverProcess.Dispose();
					_serverProcess = null;
				}
			}
			finally
			{
				_session = null;
				base.Dispose(disposing);
			}
		}
		
		// RemoteServerSession
		private RemoteServerSession _session;
		public RemoteServerSession Session { get { return _session; } }
		
		IRemoteServerSession IRemoteServerProcess.Session { get { return _session; } }
		
		private ServerProcess _serverProcess;
		internal ServerProcess ServerProcess { get { return _serverProcess; } }
		
		public void Stop()
		{
			_session.StopProcess(this);
		}
		
		// Execution
		internal Exception WrapException(Exception exception)
		{
			return RemoteServer.WrapException(exception);
		}

		// ProcessID
		public int ProcessID { get { return _serverProcess.ProcessID; } }
		
		// ProcessInfo
		public ProcessInfo ProcessInfo { get { return _serverProcess.ProcessInfo; } }
		
		// IStreamManager
		public IStreamManager StreamManager { get { return _serverProcess.StreamManager; } }
		
		StreamID IStreamManager.Allocate()
		{
			return StreamManager.Allocate();
		}
		
		StreamID IStreamManager.Reference(StreamID streamID)
		{
			return StreamManager.Reference(streamID);
		}
		
		void IStreamManager.Deallocate(StreamID streamID)
		{
			StreamManager.Deallocate(streamID);
		}
		
		Stream IStreamManager.Open(StreamID streamID, LockMode lockMode)
		{
			return StreamManager.Open(streamID, lockMode);
		}

		IRemoteStream IStreamManager.OpenRemote(StreamID streamID, LockMode lockMode)
		{
			return StreamManager.OpenRemote(streamID, lockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			StreamManager.Close(AStreamID);
		}
		#endif
		
		public string GetClassName(string className)
		{
			return _serverProcess.ServerSession.Server.Catalog.ClassLoader.Classes[className].ClassName;
		}
		
		public ServerFileInfo[] GetFileNames(string className, string environment)
		{
			Schema.RegisteredClass classValue = _serverProcess.ServerSession.Server.Catalog.ClassLoader.Classes[className];

			List<string> libraryNames = new List<string>();
			List<string> fileNames = new List<string>();
			List<string> assemblyFileNames = new List<string>();
			ArrayList fileDates = new ArrayList();
			
			// Build the list of all files required to load the assemblies in all libraries required by the library for the given class
			Schema.Library library = _serverProcess.ServerSession.Server.Catalog.Libraries[classValue.Library.Name];
			ServerFileInfos fileInfos = _session.Server.Server.GetFileNames(library, environment);
			
			// Return the results in reverse order to ensure that dependencies are loaded in the correct order
			ServerFileInfo[] results = new ServerFileInfo[fileInfos.Count];
			for (int index = fileInfos.Count - 1; index >= 0; index--)
				results[fileInfos.Count - index - 1] = fileInfos[index];
				
			return results;
		}
		
		public IRemoteStream GetFile(string libraryName, string fileName)
		{
			return 
				new CoverStream
				(
					new FileStream
					(
						_session.Server.Server.GetFullFileName(_serverProcess.ServerSession.Server.Catalog.Libraries[libraryName], fileName), 
						FileMode.Open, 
						FileAccess.Read, 
						FileShare.Read
					), 
					true
				);
		}
		
		internal PlanDescriptor GetPlanDescriptor(IRemoteServerStatementPlan plan)
		{
			PlanDescriptor descriptor = new PlanDescriptor();
			descriptor.ID = plan.ID;
			descriptor.CacheTimeStamp = plan.Process.Session.Server.CacheTimeStamp;
			descriptor.Statistics = plan.PlanStatistics;
			descriptor.Messages = DataphorFaultUtility.ExceptionsToFaults(plan.Messages);
			return descriptor;
		}
		
		private void CleanupPlans(RemoteProcessCleanupInfo cleanupInfo)
		{
			int planIndex;
			for (int index = 0; index < cleanupInfo.UnprepareList.Length; index++)
			{
				planIndex = _serverProcess.Plans.IndexOf(((RemoteServerPlan)cleanupInfo.UnprepareList[index]).ServerPlan);
				if (planIndex >= 0)
				{
					if (cleanupInfo.UnprepareList[index] is RemoteServerStatementPlan)
						UnprepareStatement((RemoteServerStatementPlan)cleanupInfo.UnprepareList[index]);
					else
						UnprepareExpression((RemoteServerExpressionPlan)cleanupInfo.UnprepareList[index]);
				}
			}
		}

		public IRemoteServerStatementPlan PrepareStatement(string statement, RemoteParam[] paramsValue, DebugLocator locator, out PlanDescriptor planDescriptor, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				CleanupPlans(cleanupInfo);
				ServerStatementPlan statementPlan = (ServerStatementPlan)_serverProcess.PrepareStatement(statement, RemoteParamsToDataParams(paramsValue), locator);
				RemoteServerStatementPlan remotePlan = new RemoteServerStatementPlan(this, statementPlan);
				planDescriptor = GetPlanDescriptor(remotePlan);
				return remotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareStatement(IRemoteServerStatementPlan plan)
		{
			try
			{
				_serverProcess.UnprepareStatement(((RemoteServerStatementPlan)plan).ServerStatementPlan);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		// Execute
		public void Execute(string statement, ref RemoteParamData paramsValue, ProcessCallInfo callInfo, RemoteProcessCleanupInfo cleanupInfo)
		{
			ProcessCallInfo(callInfo);
			DataParams localParamsValue = RemoteParamsToDataParams(paramsValue.Params);
			_serverProcess.Execute(statement, localParamsValue);
			DataParamsToRemoteParamData(localParamsValue, ref paramsValue);
		}
        
		internal PlanDescriptor GetPlanDescriptor(RemoteServerExpressionPlan plan, RemoteParam[] paramsValue)
		{
			PlanDescriptor descriptor = new PlanDescriptor();
			descriptor.ID = plan.ID;
			descriptor.Statistics = plan.PlanStatistics;
			descriptor.Messages = DataphorFaultUtility.ExceptionsToFaults(plan.Messages);
			if (plan.ServerExpressionPlan.ActualDataType is Schema.ICursorType)
			{
				descriptor.Capabilities = plan.Capabilities;
				descriptor.CursorIsolation = plan.Isolation;
				descriptor.CursorType = plan.CursorType;
				if (((TableNode)plan.ServerExpressionPlan.Program.Code.Nodes[0]).Order != null)
					descriptor.Order = ((TableNode)plan.ServerExpressionPlan.Program.Code.Nodes[0]).Order.Name;
				else
					descriptor.Order = String.Empty;
			}
			descriptor.Catalog = plan.GetCatalog(paramsValue, out descriptor.ObjectName, out descriptor.CacheTimeStamp, out descriptor.ClientCacheTimeStamp, out descriptor.CacheChanged);
			return descriptor;
		}
		
		public IRemoteServerExpressionPlan PrepareExpression(string expression, RemoteParam[] paramsValue, DebugLocator locator, out PlanDescriptor planDescriptor, RemoteProcessCleanupInfo cleanupInfo)
		{
			try
			{
				CleanupPlans(cleanupInfo);
				RemoteServerExpressionPlan remotePlan = new RemoteServerExpressionPlan(this, (ServerExpressionPlan)_serverProcess.PrepareExpression(expression, RemoteParamsToDataParams(paramsValue), locator));
				planDescriptor = GetPlanDescriptor(remotePlan, paramsValue);
				return remotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareExpression(IRemoteServerExpressionPlan plan)
		{
			try
			{
				_serverProcess.UnprepareExpression(((RemoteServerExpressionPlan)plan).ServerExpressionPlan);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		// Evaluate
		public byte[] Evaluate
		(
			string expression, 
			ref RemoteParamData paramsValue, 
			out IRemoteServerExpressionPlan plan, 
			out PlanDescriptor planDescriptor, 
			out ProgramStatistics executeTime,
			ProcessCallInfo callInfo,
			RemoteProcessCleanupInfo cleanupInfo
		)
		{
			ProcessCallInfo(callInfo);
			plan = PrepareExpression(expression, paramsValue.Params, null, out planDescriptor, cleanupInfo);
			try
			{
				return plan.Evaluate(ref paramsValue, out executeTime, EmptyCallInfo());
			}
			catch
			{
				UnprepareExpression(plan);
				plan = null;
				throw;
			}
		}
		
		// OpenCursor
		public IRemoteServerCursor OpenCursor
		(
			string expression, 
			ref RemoteParamData paramsValue, 
			out IRemoteServerExpressionPlan plan, 
			out PlanDescriptor planDescriptor, 
			out ProgramStatistics executeTime,
			ProcessCallInfo callInfo,
			RemoteProcessCleanupInfo cleanupInfo
		)
		{
			ProcessCallInfo(callInfo);
			plan = PrepareExpression(expression, paramsValue.Params, null, out planDescriptor, cleanupInfo);
			try
			{
				return plan.Open(ref paramsValue, out executeTime, EmptyCallInfo());
			}
			catch
			{
				UnprepareExpression(plan);
				plan = null;
				throw;
			}
		}
		
		// OpenCursor
		public IRemoteServerCursor OpenCursor
		(
			string expression,
			ref RemoteParamData paramsValue,
			out IRemoteServerExpressionPlan plan,
			out PlanDescriptor planDescriptor,
			out ProgramStatistics executeTime,
			ProcessCallInfo callInfo,
			RemoteProcessCleanupInfo cleanupInfo,
			out Guid[] bookmarks,
			int count,
			out RemoteFetchData fetchData
		)
		{
			ProcessCallInfo(callInfo);
			plan = PrepareExpression(expression, paramsValue.Params, null, out planDescriptor, cleanupInfo);
			try
			{
				IRemoteServerCursor cursor = plan.Open(ref paramsValue, out executeTime, EmptyCallInfo());
				fetchData = cursor.Fetch(out bookmarks, count, true, EmptyCallInfo());
				return cursor;
			}
			catch
			{
				UnprepareExpression(plan);
				plan = null;
				throw;
			}
		}
		
		// CloseCursor
		public void CloseCursor(IRemoteServerCursor cursor, ProcessCallInfo callInfo)
		{
			IRemoteServerExpressionPlan plan = cursor.Plan;
			try
			{
				plan.Close(cursor, callInfo);
			}
			finally
			{
				UnprepareExpression(plan);
			}
		}
		
		// PrepareScript
		public IRemoteServerScript PrepareScript(string script, DebugLocator locator)
		{
			ServerScript localScript = (ServerScript)_serverProcess.PrepareScript(script, locator);
			return new RemoteServerScript(this, localScript);
		}
		
		// UnprepareScript
		public void UnprepareScript(IRemoteServerScript script)
		{
			_serverProcess.UnprepareScript(((RemoteServerScript)script).ServerScript);
		}
        
		// ExecuteScript
		public void ExecuteScript(string script, ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			_serverProcess.ExecuteScript(script);
		}

		// TransactionCount
		public int TransactionCount { get { return _serverProcess.TransactionCount; } }

		// InTransaction
		public bool InTransaction { get { return _serverProcess.InTransaction; } }

		// BeginTransaction
		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			try
			{
				_serverProcess.BeginTransaction(isolationLevel);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		// PrepareTransaction
		public void PrepareTransaction()
		{
			try
			{
				_serverProcess.PrepareTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
        
		// CommitTransaction
		public void CommitTransaction()
		{
			try
			{
				_serverProcess.CommitTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		// RollbackTransaction        
		public void RollbackTransaction()
		{
			try
			{
				_serverProcess.RollbackTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
    
		// Application Transactions
		public Guid ApplicationTransactionID { get { return _serverProcess.ApplicationTransactionID; } }
		
		internal void ProcessCallInfo(ProcessCallInfo callInfo)
		{
			for (int index = 0; index < callInfo.TransactionList.Length; index++)
				BeginTransaction(callInfo.TransactionList[index]);
		}
		
		internal ProcessCallInfo EmptyCallInfo()
		{
			return Contracts.ProcessCallInfo.Empty;
		}
		
		public Guid BeginApplicationTransaction(bool shouldJoin, bool isInsert, ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			try
			{
				return _serverProcess.BeginApplicationTransaction(shouldJoin, isInsert);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void PrepareApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			try
			{
				_serverProcess.PrepareApplicationTransaction(iD);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void CommitApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			try
			{
				_serverProcess.CommitApplicationTransaction(iD);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void RollbackApplicationTransaction(Guid iD, ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			try
			{
				_serverProcess.RollbackApplicationTransaction(iD);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void JoinApplicationTransaction(Guid iD, bool isInsert, ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			try
			{
				_serverProcess.JoinApplicationTransaction(iD, isInsert);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void LeaveApplicationTransaction(ProcessCallInfo callInfo)
		{
			ProcessCallInfo(callInfo);
			try
			{
				_serverProcess.LeaveApplicationTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public static Modifier RemoteModifierToModifier(RemoteParamModifier remoteModifier)
		{
			switch (remoteModifier)
			{
				case RemoteParamModifier.In : return Modifier.In;
				case RemoteParamModifier.Out : return Modifier.Out;
				case RemoteParamModifier.Var : return Modifier.Var;
				case RemoteParamModifier.Const : return Modifier.Const;
				default: throw new ArgumentOutOfRangeException("ARemoteModifier");
			}
		}
		
		// Parameter Translation
		public DataParams RemoteParamsToDataParams(RemoteParam[] paramsValue)
		{
			if ((paramsValue != null) && (paramsValue.Length > 0))
			{
				DataParams localParamsValue = new DataParams();
				foreach (RemoteParam remoteParam in paramsValue)
					localParamsValue.Add(new DataParam(remoteParam.Name, (Schema.ScalarType)_serverProcess.ServerSession.Server.Catalog[remoteParam.TypeName], RemoteModifierToModifier(remoteParam.Modifier)));

				return localParamsValue;
			}
			else
				return null;
		}
		
		public DataParams RemoteParamDataToDataParams(RemoteParamData paramsValue)
		{
			if ((paramsValue.Params != null) && (paramsValue.Params.Length > 0))
			{
				DataParams localParamsValue = new DataParams();
				Schema.RowType rowType = new Schema.RowType();
				for (int index = 0; index < paramsValue.Params.Length; index++)
					rowType.Columns.Add(new Schema.Column(paramsValue.Params[index].Name, (Schema.ScalarType)_serverProcess.ServerSession.Server.Catalog[paramsValue.Params[index].TypeName]));
					
				Row row = new Row(_serverProcess.ValueManager, rowType);
				try
				{
					row.ValuesOwned = false;
					row.AsPhysical = paramsValue.Data.Data;

					for (int index = 0; index < paramsValue.Params.Length; index++)
						if (row.HasValue(index))
							localParamsValue.Add(new DataParam(row.DataType.Columns[index].Name, row.DataType.Columns[index].DataType, RemoteModifierToModifier(paramsValue.Params[index].Modifier), DataValue.CopyValue(_serverProcess.ValueManager, row[index])));
						else
							localParamsValue.Add(new DataParam(row.DataType.Columns[index].Name, row.DataType.Columns[index].DataType, RemoteModifierToModifier(paramsValue.Params[index].Modifier), null));

					return localParamsValue;
				}
				finally
				{
					row.Dispose();
				}
			}
			else
				return null;
		}
		
		public void DataParamsToRemoteParamData(DataParams paramsValue, ref RemoteParamData remoteParams)
		{
			if (paramsValue != null)
			{
				Schema.RowType rowType = new Schema.RowType();
				for (int index = 0; index < paramsValue.Count; index++)
					rowType.Columns.Add(new Schema.Column(paramsValue[index].Name, paramsValue[index].DataType));
					
				Row row = new Row(_serverProcess.ValueManager, rowType);
				try
				{
					row.ValuesOwned = false;
					for (int index = 0; index < paramsValue.Count; index++)
						row[index] = paramsValue[index].Value;
					
					remoteParams.Data.Data = row.AsPhysical;
				}
				finally
				{
					row.Dispose();
				}
			}
		}
		
		public string GetCatalog(string name, out long cacheTimeStamp, out long clientCacheTimeStamp, out bool cacheChanged)
		{
			cacheTimeStamp = _serverProcess.ServerSession.Server.CacheTimeStamp;

			Schema.Catalog catalog = new Schema.Catalog();
			catalog.IncludeDependencies(_serverProcess.CatalogDeviceSession, _serverProcess.Catalog, _serverProcess.Catalog[name], EmitMode.ForRemote);
			
			#if LOGCACHEEVENTS
			FServerProcess.ServerSession.Server.LogMessage(String.Format("Getting catalog for data type '{0}'.", AName));
			#endif

			cacheChanged = true;
			string[] requiredObjects = _session.Server.CatalogCaches.GetRequiredObjects(_session, catalog, cacheTimeStamp, out clientCacheTimeStamp);
			if (requiredObjects.Length > 0)
			{
				string catalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(catalog.EmitStatement(_serverProcess.CatalogDeviceSession, EmitMode.ForRemote, requiredObjects));
				return catalogString;
			}
			return String.Empty;
		}
	}

	// RemoteServerProcesses
	public class RemoteServerProcesses : RemoteServerChildObjects
	{		
		protected override void Validate(RemoteServerChildObject objectValue)
		{
			if (!(objectValue is RemoteServerProcess))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerProcess");
		}
		
		public new RemoteServerProcess this[int index]
		{
			get { return (RemoteServerProcess)base[index]; } 
			set { base[index] = value; } 
		}
		
		public RemoteServerProcess GetProcess(int processID)
		{
			foreach (RemoteServerProcess process in this)
				if (process.ProcessID == processID)
					return process;
			throw new ServerException(ServerException.Codes.ProcessNotFound, processID);
		}
	}
}
