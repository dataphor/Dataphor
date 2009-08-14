/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerProcess
	public class RemoteServerProcess : RemoteServerChildObject, IRemoteServerProcess
	{
		internal RemoteServerProcess(RemoteServerSession ASession, ServerProcess AProcess) : base()
		{
			FSession = ASession;
			FServerProcess = AProcess;
			AttachServerProcess();
		}
		
		private void AttachServerProcess()
		{
			FServerProcess.Disposed += new EventHandler(FServerProcessDisposed);
		}

		private void FServerProcessDisposed(object ASender, EventArgs AArgs)
		{
			DetachServerProcess();
			FServerProcess = null;
			Dispose();
		}
		
		private void DetachServerProcess()
		{
			FServerProcess.Disposed -= new EventHandler(FServerProcessDisposed);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FServerProcess != null)
				{
					DetachServerProcess();
					FServerProcess.Dispose();
					FServerProcess = null;
				}
			}
			finally
			{
				FSession = null;
				base.Dispose(ADisposing);
			}
		}
		
		// RemoteServerSession
		private RemoteServerSession FSession;
		public RemoteServerSession Session { get { return FSession; } }
		
		IRemoteServerSession IRemoteServerProcess.Session { get { return FSession; } }
		
		private ServerProcess FServerProcess;
		internal ServerProcess ServerProcess { get { return FServerProcess; } }
		
		// Execution
		internal Exception WrapException(Exception AException)
		{
			return RemoteServer.WrapException(AException);
		}

		// ProcessID
		public int ProcessID { get { return FServerProcess.ProcessID; } }
		
		// ProcessInfo
		public ProcessInfo ProcessInfo { get { return FServerProcess.ProcessInfo; } }
		
		// IStreamManager
		public IStreamManager StreamManager { get { return FServerProcess.StreamManager; } }
		
		StreamID IStreamManager.Allocate()
		{
			return StreamManager.Allocate();
		}
		
		StreamID IStreamManager.Reference(StreamID AStreamID)
		{
			return StreamManager.Reference(AStreamID);
		}
		
		void IStreamManager.Deallocate(StreamID AStreamID)
		{
			StreamManager.Deallocate(AStreamID);
		}
		
		Stream IStreamManager.Open(StreamID AStreamID, LockMode ALockMode)
		{
			return StreamManager.Open(AStreamID, ALockMode);
		}

		IRemoteStream IStreamManager.OpenRemote(StreamID AStreamID, LockMode ALockMode)
		{
			return StreamManager.OpenRemote(AStreamID, ALockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			StreamManager.Close(AStreamID);
		}
		#endif
		
		public string GetClassName(string AClassName)
		{
			return FServerProcess.ServerSession.Server.Catalog.ClassLoader.Classes[AClassName].ClassName;
		}
		
		public void GetFileNames(string AClassName, out string[] ALibraryNames, out string[] AFileNames, out DateTime[] AFileDates, out string[] AAssemblyFileNames)
		{
			Schema.RegisteredClass LClass = FServerProcess.ServerSession.Server.Catalog.ClassLoader.Classes[AClassName];

			StringCollection LLibraryNames = new StringCollection();
			StringCollection LFileNames = new StringCollection();
			StringCollection LAssemblyFileNames = new StringCollection();
			ArrayList LFileDates = new ArrayList();
			
			// Build the list of all files required to load the assemblies in all libraries required by the library for the given class
			Schema.Library LLibrary = FServerProcess.ServerSession.Server.Catalog.Libraries[LClass.Library.Name];
			FServerProcess.GetFileNames(LLibrary, LLibraryNames, LFileNames, LFileDates);
			FServerProcess.GetAssemblyFileNames(LLibrary, LAssemblyFileNames);
			
			ALibraryNames = new string[LLibraryNames.Count];
			LLibraryNames.CopyTo(ALibraryNames, 0);
			
			AFileNames = new string[LFileNames.Count];
			LFileNames.CopyTo(AFileNames, 0);
			
			AFileDates = new DateTime[LFileDates.Count];
			LFileDates.CopyTo(AFileDates, 0);
			
			// Return the results in reverse order to ensure that dependencies are loaded in the correct order
			AAssemblyFileNames = new string[LAssemblyFileNames.Count];
			for (int LIndex = LAssemblyFileNames.Count - 1; LIndex >= 0; LIndex--)
				AAssemblyFileNames[LAssemblyFileNames.Count - LIndex - 1] = LAssemblyFileNames[LIndex];
		}
		
		public IRemoteStream GetFile(string ALibraryName, string AFileName)
		{
			return 
				new CoverStream
				(
					new FileStream
					(
						FServerProcess.GetFullFileName(FServerProcess.ServerSession.Server.Catalog.Libraries[ALibraryName], AFileName), 
						FileMode.Open, 
						FileAccess.Read, 
						FileShare.Read
					), 
					true
				);
		}
		
		internal PlanDescriptor GetPlanDescriptor(IRemoteServerStatementPlan APlan)
		{
			PlanDescriptor LDescriptor = new PlanDescriptor();
			LDescriptor.ID = APlan.ID;
			LDescriptor.CacheTimeStamp = APlan.Process.Session.Server.CacheTimeStamp;
			LDescriptor.Statistics = APlan.Statistics;
			LDescriptor.Messages = APlan.Messages;
			return LDescriptor;
		}
		
		private void CleanupPlans(ProcessCleanupInfo ACleanupInfo)
		{
			int LPlanIndex;
			for (int LIndex = 0; LIndex < ACleanupInfo.UnprepareList.Length; LIndex++)
			{
				LPlanIndex = FServerProcess.Plans.IndexOf(((RemoteServerPlan)ACleanupInfo.UnprepareList[LIndex]).ServerPlan);
				if (LPlanIndex >= 0)
				{
					if (ACleanupInfo.UnprepareList[LIndex] is RemoteServerStatementPlan)
						UnprepareStatement((RemoteServerStatementPlan)ACleanupInfo.UnprepareList[LIndex]);
					else
						UnprepareExpression((RemoteServerExpressionPlan)ACleanupInfo.UnprepareList[LIndex]);
				}
			}
		}

		public IRemoteServerStatementPlan PrepareStatement(string AStatement, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, ProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				CleanupPlans(ACleanupInfo);
				ServerStatementPlan LStatementPlan = (ServerStatementPlan)FServerProcess.PrepareStatement(AStatement, RemoteParamsToDataParams(AParams), ALocator);
				RemoteServerStatementPlan LRemotePlan = new RemoteServerStatementPlan(this, LStatementPlan);
				APlanDescriptor = GetPlanDescriptor(LRemotePlan);
				return LRemotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			try
			{
				FServerProcess.UnprepareStatement(((RemoteServerStatementPlan)APlan).ServerStatementPlan);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		// Execute
		public void Execute(string AStatement, ref RemoteParamData AParams, ProcessCallInfo ACallInfo, ProcessCleanupInfo ACleanupInfo)
		{
			ProcessCallInfo(ACallInfo);
			DataParams LParams = RemoteParamsToDataParams(AParams.Params);
			FServerProcess.Execute(AStatement, LParams);
			DataParamsToRemoteParamData(LParams, ref AParams);
		}
        
		internal PlanDescriptor GetPlanDescriptor(RemoteServerExpressionPlan APlan, RemoteParam[] AParams)
		{
			PlanDescriptor LDescriptor = new PlanDescriptor();
			LDescriptor.ID = APlan.ID;
			LDescriptor.Statistics = APlan.Statistics;
			LDescriptor.Messages = APlan.Messages;
			if (APlan.ServerExpressionPlan.DataType is Schema.ITableType)
			{
				LDescriptor.Capabilities = APlan.Capabilities;
				LDescriptor.CursorIsolation = APlan.Isolation;
				LDescriptor.CursorType = APlan.CursorType;
				if (((TableNode)APlan.ServerExpressionPlan.Code.Nodes[0]).Order != null)
					LDescriptor.Order = ((TableNode)APlan.ServerExpressionPlan.Code.Nodes[0]).Order.Name;
				else
					LDescriptor.Order = String.Empty;
			}
			LDescriptor.Catalog = APlan.GetCatalog(AParams, out LDescriptor.ObjectName, out LDescriptor.CacheTimeStamp, out LDescriptor.ClientCacheTimeStamp, out LDescriptor.CacheChanged);
			return LDescriptor;
		}
		
		public IRemoteServerExpressionPlan PrepareExpression(string AExpression, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, ProcessCleanupInfo ACleanupInfo)
		{
			try
			{
				CleanupPlans(ACleanupInfo);
				RemoteServerExpressionPlan LRemotePlan = new RemoteServerExpressionPlan(this, (ServerExpressionPlan)FServerProcess.PrepareExpression(AExpression, RemoteParamsToDataParams(AParams), ALocator));
				APlanDescriptor = GetPlanDescriptor(LRemotePlan, AParams);
				return LRemotePlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			FServerProcess.UnprepareExpression(((RemoteServerExpressionPlan)APlan).ServerExpressionPlan);
		}
		
		// Evaluate
		public byte[] Evaluate
		(
			string AExpression, 
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan, 
			out PlanDescriptor APlanDescriptor, 
			ProcessCallInfo ACallInfo,
			ProcessCleanupInfo ACleanupInfo
		)
		{
			ProcessCallInfo(ACallInfo);
			APlan = PrepareExpression(AExpression, AParams.Params, null, out APlanDescriptor, ACleanupInfo);
			try
			{
				TimeSpan LExecuteTime;
				byte[] LResult = APlan.Evaluate(ref AParams, out LExecuteTime, EmptyCallInfo());
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LResult;
			}
			catch
			{
				UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// OpenCursor
		public IRemoteServerCursor OpenCursor
		(
			string AExpression, 
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan, 
			out PlanDescriptor APlanDescriptor, 
			ProcessCallInfo ACallInfo,
			ProcessCleanupInfo ACleanupInfo
		)
		{
			ProcessCallInfo(ACallInfo);
			APlan = PrepareExpression(AExpression, AParams.Params, null, out APlanDescriptor, ACleanupInfo);
			try
			{
				TimeSpan LExecuteTime;
				IRemoteServerCursor LCursor = APlan.Open(ref AParams, out LExecuteTime, EmptyCallInfo());
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LCursor;
			}
			catch
			{
				UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// OpenCursor
		public IRemoteServerCursor OpenCursor
		(
			string AExpression,
			ref RemoteParamData AParams,
			out IRemoteServerExpressionPlan APlan,
			out PlanDescriptor APlanDescriptor,
			ProcessCallInfo ACallInfo,
			ProcessCleanupInfo ACleanupInfo,
			out Guid[] ABookmarks,
			int ACount,
			out RemoteFetchData AFetchData
		)
		{
			ProcessCallInfo(ACallInfo);
			APlan = PrepareExpression(AExpression, AParams.Params, null, out APlanDescriptor, ACleanupInfo);
			try
			{
				TimeSpan LExecuteTime;
				IRemoteServerCursor LCursor = APlan.Open(ref AParams, out LExecuteTime, EmptyCallInfo());
				AFetchData = LCursor.Fetch(out ABookmarks, ACount, EmptyCallInfo());
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LCursor;
			}
			catch
			{
				UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// CloseCursor
		public void CloseCursor(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			IRemoteServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor, ACallInfo);
			}
			finally
			{
				UnprepareExpression(LPlan);
			}
		}
		
		// PrepareScript
		public IRemoteServerScript PrepareScript(string AScript, DebugLocator ALocator)
		{
			ServerScript LScript = (ServerScript)FServerProcess.PrepareScript(AScript, ALocator);
			return new RemoteServerScript(this, LScript);
		}
		
		// UnprepareScript
		public void UnprepareScript(IRemoteServerScript AScript)
		{
			FServerProcess.UnprepareScript(((RemoteServerScript)AScript).ServerScript);
		}
        
		// ExecuteScript
		public void ExecuteScript(string AScript, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			FServerProcess.ExecuteScript(AScript);
		}

		// TransactionCount
		public int TransactionCount { get { return FServerProcess.TransactionCount; } }

		// InTransaction
		public bool InTransaction { get { return FServerProcess.InTransaction; } }

		// BeginTransaction
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			try
			{
				FServerProcess.BeginTransaction(AIsolationLevel);
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
				FServerProcess.PrepareTransaction();
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
				FServerProcess.CommitTransaction();
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
				FServerProcess.RollbackTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
    
		// Application Transactions
		public Guid ApplicationTransactionID { get { return FServerProcess.ApplicationTransactionID; } }
		
		internal void ProcessCallInfo(ProcessCallInfo ACallInfo)
		{
			for (int LIndex = 0; LIndex < ACallInfo.TransactionList.Length; LIndex++)
				BeginTransaction(ACallInfo.TransactionList[LIndex]);
		}
		
		internal ProcessCallInfo EmptyCallInfo()
		{
			ProcessCallInfo LInfo = new ProcessCallInfo();
			LInfo.TransactionList = new IsolationLevel[0];
			return LInfo;
		}
		
		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			try
			{
				return FServerProcess.BeginApplicationTransaction(AShouldJoin, AIsInsert);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void PrepareApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			try
			{
				FServerProcess.PrepareApplicationTransaction(AID);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void CommitApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			try
			{
				FServerProcess.CommitApplicationTransaction(AID);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void RollbackApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			try
			{
				FServerProcess.RollbackApplicationTransaction(AID);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void JoinApplicationTransaction(Guid AID, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			try
			{
				FServerProcess.JoinApplicationTransaction(AID, AIsInsert);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public void LeaveApplicationTransaction(ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			try
			{
				FServerProcess.LeaveApplicationTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		// Parameter Translation
		public DataParams RemoteParamsToDataParams(RemoteParam[] AParams)
		{
			if ((AParams != null) && (AParams.Length > 0))
			{
				DataParams LParams = new DataParams();
				foreach (RemoteParam LRemoteParam in AParams)
					LParams.Add(new DataParam(LRemoteParam.Name, (Schema.ScalarType)FServerProcess.ServerSession.Server.Catalog[LRemoteParam.TypeName], (Modifier)LRemoteParam.Modifier));//hack: cast to fix fixup error

				return LParams;
			}
			else
				return null;
		}
		
		public DataParams RemoteParamDataToDataParams(RemoteParamData AParams)
		{
			if ((AParams.Params != null) && (AParams.Params.Length > 0))
			{
				DataParams LParams = new DataParams();
				Schema.RowType LRowType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
					LRowType.Columns.Add(new Schema.Column(AParams.Params[LIndex].Name, (Schema.ScalarType)FServerProcess.ServerSession.Server.Catalog[AParams.Params[LIndex].TypeName]));
					
				Row LRow = new Row(FServerProcess, LRowType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = AParams.Data.Data;

					for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
						if (LRow.HasValue(LIndex))
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow.DataType.Columns[LIndex].DataType, (Modifier)AParams.Params[LIndex].Modifier, DataValue.CopyValue(FServerProcess, LRow[LIndex])));//Hack: cast to fix fixup error
						else
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow.DataType.Columns[LIndex].DataType, (Modifier)AParams.Params[LIndex].Modifier, null));//Hack: cast to fix fixup error

					return LParams;
				}
				finally
				{
					LRow.Dispose();
				}
			}
			else
				return null;
		}
		
		public void DataParamsToRemoteParamData(DataParams AParams, ref RemoteParamData ARemoteParams)
		{
			if (AParams != null)
			{
				Schema.RowType LRowType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
					LRowType.Columns.Add(new Schema.Column(AParams[LIndex].Name, AParams[LIndex].DataType));
					
				Row LRow = new Row(FServerProcess, LRowType);
				try
				{
					LRow.ValuesOwned = false;
					for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
						LRow[LIndex] = AParams[LIndex].Value;
					
					ARemoteParams.Data.Data = LRow.AsPhysical;
				}
				finally
				{
					LRow.Dispose();
				}
			}
		}
		
		public string GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			ACacheTimeStamp = FServerProcess.ServerSession.Server.CacheTimeStamp;

			Schema.Catalog LCatalog = new Schema.Catalog();
			LCatalog.IncludeDependencies(FServerProcess, FServerProcess.Plan.Catalog, FServerProcess.Plan.Catalog[AName], EmitMode.ForRemote);
			
			#if LOGCACHEEVENTS
			FServerProcess.ServerSession.Server.LogMessage(String.Format("Getting catalog for data type '{0}'.", AName));
			#endif

			ACacheChanged = true;
			string[] LRequiredObjects = FSession.Server.CatalogCaches.GetRequiredObjects(FSession, LCatalog, ACacheTimeStamp, out AClientCacheTimeStamp);
			if (LRequiredObjects.Length > 0)
			{
				string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(LCatalog.EmitStatement(FServerProcess, EmitMode.ForRemote, LRequiredObjects));
				return LCatalogString;
			}
			return String.Empty;
		}
	}

	// RemoteServerProcesses
	public class RemoteServerProcesses : RemoteServerChildObjects
	{		
		protected override void Validate(RemoteServerChildObject AObject)
		{
			if (!(AObject is RemoteServerProcess))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerProcess");
		}
		
		public new RemoteServerProcess this[int AIndex]
		{
			get { return (RemoteServerProcess)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
		
		public RemoteServerProcess GetProcess(int AProcessID)
		{
			foreach (RemoteServerProcess LProcess in this)
				if (LProcess.ProcessID == AProcessID)
					return LProcess;
			throw new ServerException(ServerException.Codes.ProcessNotFound, AProcessID);
		}
	}
}
