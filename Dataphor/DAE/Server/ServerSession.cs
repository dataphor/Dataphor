/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerSession : ServerChildObject, IServerSession
	{		
		internal ServerSession
		(
			Server AServer, 
			int ASessionID, 
			SessionInfo ASessionInfo,
			Schema.User AUser
		) : base()
		{
			FServer = AServer;
			FSessionID = ASessionID;
			FSessionInfo = ASessionInfo;
			FSessionObjects = new Schema.Objects();
			FSessionOperators = new Schema.Objects();
			FUser = AUser;
			FProcesses = new ServerProcesses();

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FServer.FSessionCounter != null)
				FServer.FSessionCounter.Increment();
			#endif
		}
		
		private bool FDisposed;
		
		// Dispose
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					try
					{
						try
						{
							try
							{
								try
								{
									if (FDebugger != null)
										FDebugger.Stop();
								}
								finally
								{
									if (FApplicationTransactions != null)
									{
										EndApplicationTransactions();
										FApplicationTransactions = null;
									}
								}
							}
							finally
							{
								if (FSessionObjects  != null)
								{
									DropSessionObjects();
									FSessionObjects  = null;
									FSessionOperators = null;
								}
							}
						}
						finally
						{
							if (FProcesses != null)
							{
								try
								{
									StopProcesses();
								}
								finally
								{
									FProcesses.Dispose();
									FProcesses = null;
								}
							}
						}
					}
					finally
					{
						if (FCursorManager != null)
						{
							FCursorManager.Dispose();
							FCursorManager = null;
						}
					}
				}
				finally
				{
					FSessionInfo = null;
					FSessionID = -1;
					FUser = null;
					
					if (!FDisposed)
					{
						#if !DISABLE_PERFORMANCE_COUNTERS
						if (FServer.FSessionCounter != null)
							FServer.FSessionCounter.Decrement();
						#endif
					}

					FServer = null;
				}
			}
			finally
			{
				FDisposed = true;
				base.Dispose(ADisposing);
			}
		}
        
		// Processes
		private ServerProcesses FProcesses;
		internal ServerProcesses Processes { get { return FProcesses; } }		

		// Server
		private Server FServer;
		public Server Server { get { return FServer; } }
		
		IServer IServerSession.Server { get { return FServer; } }
        
		// SessionID
		private int FSessionID = -1;
		public int SessionID  { get { return FSessionID; } }
        
		// User        
		private Schema.User FUser;
		public Schema.User User { get { return FUser; } }
		internal void SetUser(Schema.User AUser)
		{
			FUser = AUser;
		}

		// SessionInfo
		private SessionInfo FSessionInfo;        
		public SessionInfo SessionInfo { get { return FSessionInfo; } }

		// Plan Cache		
		public void AddCachedPlan(ServerProcess AProcess, string AStatement, int AContextHashCode, ServerPlan APlan)
		{
			if (FSessionInfo.UsePlanCache && (Server.PlanCacheSize > 0) && !HasNonGeneratedSessionObjects())
				Server.PlanCache.Add(AProcess, AStatement, AContextHashCode, APlan);
		}
		
		public ServerPlan GetCachedPlan(ServerProcess AProcess, string AStatement, int AContextHashCode)
		{
			if (FSessionInfo.UsePlanCache && (Server.PlanCacheSize > 0) && !HasNonGeneratedSessionObjects())
				return Server.PlanCache.Get(AProcess, AStatement, AContextHashCode);
			return null;
		}
		
		public bool ReleaseCachedPlan(ServerProcess AProcess, ServerPlan APlan)
		{
			if (FSessionInfo.UsePlanCache && (Server.PlanCacheSize > 0) && (APlan.Header != null) && (!APlan.Header.IsInvalidPlan) && !HasNonGeneratedSessionObjects())
				return Server.PlanCache.Release(AProcess, APlan);
			return false;
		}
		
		// CurrentLibrary
		/// <summary> 
		///	Specifies a library which is being registered or loaded on this session. 
		///	All objects created on this session will be part of this library. 
		///	</summary>
		private Schema.LoadedLibrary FCurrentLibrary;
		public Schema.LoadedLibrary CurrentLibrary
		{
			get 
			{ 
				if (FCurrentLibrary == null)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.NoCurrentLibrary);
				return FCurrentLibrary; 
			}
			set 
			{ 
				FCurrentLibrary = value; 
			}
		}

		// ApplicationTransactions
		private Hashtable FApplicationTransactions = new Hashtable();
		public Hashtable ApplicationTransactions { get { return FApplicationTransactions; } }
		
		private void EndApplicationTransactions()
		{
			while (FApplicationTransactions.Count > 0)
				foreach (Guid LID in FApplicationTransactions.Keys)
				{
					FServer.RunScript(String.Format("System.RollbackApplicationTransaction(Guid('{0}'));", LID.ToString()), String.Empty);
					break;
				}
		}

		// Session-scoped table variables
		private NativeTables FTables;
		public NativeTables Tables
		{
			get
			{
				if (FTables == null)
					FTables = new NativeTables();
				return FTables;
			}
		}

		// SessionObjects 		
		private Schema.Objects FSessionObjects;
		public Schema.Objects SessionObjects { get { return FSessionObjects; } }
		
		// SessionOperators
		private Schema.Objects FSessionOperators;
		public Schema.Objects SessionOperators { get { return FSessionOperators; } }
		
		private void DropSessionObjects()
		{
			if (HasSessionObjects())
			{
				List<String> LObjectNames = new List<String>();
				for (int LIndex = 0; LIndex < FSessionObjects.Count; LIndex++)
					LObjectNames.Add(((Schema.SessionObject)FSessionObjects[LIndex]).GlobalName);
					
				for (int LIndex = 0; LIndex < FSessionOperators.Count; LIndex++)
				{
					FServer.FSystemProcess.CatalogDeviceSession.ResolveOperatorName(((Schema.SessionObject)FSessionOperators[LIndex]).GlobalName);
					Schema.OperatorMap LOperatorMap = FServer.Catalog.OperatorMaps[((Schema.SessionObject)FSessionOperators[LIndex]).GlobalName];
					foreach (Schema.OperatorSignature LSignature in LOperatorMap.Signatures.Signatures.Values)
						LObjectNames.Add(LSignature.Operator.Name);
				}
				
				string[] LObjectNameArray = new string[LObjectNames.Count];
				for (int LIndex = 0; LIndex < LObjectNames.Count; LIndex++)
					LObjectNameArray[LIndex] = LObjectNames[LIndex];
					
				Block LBlock = (Block)FServer.Catalog.EmitDropStatement(FServer.FSystemProcess.CatalogDeviceSession, LObjectNameArray, String.Empty);
				
				FServer.FSystemProcess.BeginCall();
				try
				{
					ServerStatementPlan LPlan = new ServerStatementPlan(FServer.FSystemProcess);
					try
					{
						// Push a timestamp safe context to prevent the drops from flushing cache-points
						LPlan.Plan.EnterTimeStampSafeContext();
						try
						{
							FServer.FSystemProcess.PushExecutingPlan(LPlan);
							try
							{
								for (int LIndex = 0; LIndex < LBlock.Statements.Count; LIndex++)
								{
									PlanNode LPlanNode = Compiler.BindNode(LPlan.Plan, Compiler.CompileStatement(LPlan.Plan, LBlock.Statements[LIndex]));
									LPlan.Plan.CheckCompiled();
									LPlanNode.Execute(FServer.FSystemProcess);
								}
							}
							finally
							{
								FServer.FSystemProcess.PopExecutingPlan(LPlan);
							}
						}
						finally
						{
							LPlan.Plan.ExitTimeStampSafeContext();
						}
					}
					finally
					{
						LPlan.Dispose();
					}
				}
				catch (Exception E)
				{
					throw WrapException(E);
				}
				finally
				{
					FServer.FSystemProcess.EndCall();
				}
					
				//FServer.RunScript(new D4TextEmitter().Emit(FServer.Catalog.EmitDropStatement(FServer.FSystemProcess, LObjectNameArray, String.Empty)), FCurrentLibrary.Name);
			}
		}
		
		public bool HasSessionObjects()
		{
			return ((FSessionObjects == null ? 0 : FSessionObjects.Count) + (FSessionOperators == null ? 0 : FSessionOperators.Count)) > 0;
		}
		
		public bool HasNonGeneratedSessionObjects()
		{
			if ((FSessionObjects == null) && (FSessionOperators == null))
				return false;
				
			// ASSERTION: The only way a session object could be marked generated is if it is
			// the check table created to track deferred constraint checks.
			if (FSessionObjects != null)
				for (int LIndex = 0; LIndex < FSessionObjects.Count; LIndex++)
					if (!FSessionObjects[LIndex].IsGenerated)
						return true;
					
			// NOTE: The server does not currently create session operators to support any internal operations
			// If that ever changes, this needs to be changed to look for those generated operators.
			// For now, this is a shortcut for performance.
			if ((FSessionOperators != null) && (FSessionOperators.Count) > 0)
				return true;
			
			return false;
		}
		
		// CursorManager
		private CursorManager FCursorManager;
		public CursorManager CursorManager
		{
			get 
			{ 
				if (FCursorManager == null)
					FCursorManager = new CursorManager();
				return FCursorManager; 
			}
		}
		
		// Debug
		private Debugger FDebugger;
		public Debugger Debugger { get { return FDebugger; } }
		
		public Debugger CheckedDebugger
		{
			get
			{
				if (FDebugger == null)
					throw new ServerException(ServerException.Codes.DebuggerNotStarted, FSessionID);
				return FDebugger;
			}
		}
		
		public void StartDebugger()
		{
			new Debugger(this);
		}
		
		public void StopDebugger()
		{
			CheckedDebugger.Stop();
		}
		
		/// <summary>
		/// Sets the debugger that is started on this session.
		/// </summary>
		internal void SetDebugger(Debugger ADebugger)
		{
			if ((FDebugger != null) && (ADebugger != null) && (FDebugger != ADebugger))
				throw new ServerException(ServerException.Codes.DebuggerAlreadyStarted, FSessionID);
			FDebuggerID = 0; // Clear the debugger ID, just in case
			FDebugger = ADebugger;
		}
		
		private int FDebuggerID;
		public int DebuggerID { get { return FDebuggerID; } }
		
		/// <summary>
		/// Sets the ID of the debugger to which this session is attached.
		/// </summary>
		internal void SetDebuggerID(int ADebuggerID)
		{
			if ((FDebugger != null) && (FSessionID == ADebuggerID))
				throw new ServerException(ServerException.Codes.CannotAttachToDebuggerSession, FSessionID);
			if ((FDebuggerID != 0) && (ADebuggerID != 0))
				throw new ServerException(ServerException.Codes.DebuggerAlreadyAttachedToSession, FSessionID);
			FDebuggerID = ADebuggerID;
		}

		// Execution
		internal Exception WrapException(Exception AException)
		{
			return FServer.WrapException(AException);
		}

		private void StopProcesses()
		{
			while (FProcesses.Count > 0)
			{
				try
				{
					try
					{
						Server.TerminateProcessThread(FProcesses[0]);
					}
					catch (Exception E)
					{
						Server.LogError(E);
					}
					
					FProcesses.DisownAt(0).Dispose();
				}
				catch (Exception E)
				{
					Server.LogError(E);
				}
			}
		}

		/// <summary>
		/// Initiates a termination request for a process.
		/// </summary>
		/// <param name="AProcessID">The ID of the process to be stopped.</param>
		public void StopProcess(int AProcessID)
		{
			ServerProcess LProcessToStop = null;
			lock (FProcesses)
				foreach (ServerProcess LProcess in FProcesses)
					if (LProcess.ProcessID == AProcessID)
					{
						LProcessToStop = LProcess;
						break;
					}
			
			if (LProcessToStop != null)
			{
				Server.TerminateProcess(LProcessToStop);
				return;
			}

			throw new ServerException(ServerException.Codes.ProcessNotFound, AProcessID);
		}

		// StartProcess
		public IServerProcess StartProcess(ProcessInfo AProcessInfo)
		{
			try
			{
				ServerProcess LProcess = new ServerProcess(this, AProcessInfo);
				FProcesses.Add(LProcess); // Is protected by a latch in the ServerChildObjects collection
				return LProcess;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		// StopProcess
		public void StopProcess(IServerProcess AProcess)
		{
			try
			{
				((ServerProcess)AProcess).Dispose();	// Is protected by a latch in the ServerChildObjects collection
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		internal void RemoveDeferredConstraintChecks(Schema.TableVar ATableVar)
		{
			if (FProcesses != null)
				foreach (ServerProcess LProcess in FProcesses)
					LProcess.RemoveDeferredConstraintChecks(ATableVar);
		}
		
		internal void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			if (FProcesses != null)
				foreach (ServerProcess LProcess in FProcesses)
					LProcess.RemoveDeferredHandlers(AHandler);
		}
		
		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			if (FProcesses != null)
				foreach (ServerProcess LProcess in FProcesses)
					LProcess.RemoveCatalogConstraintCheck(AConstraint);
		}
	}
	
	// ServerSessions
	public class ServerSessions : ServerChildObjects
	{		
		public ServerSessions() : base() {}
		public ServerSessions(bool AIsOwner) : base(AIsOwner) {}
		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerSession))
				throw new ServerException(ServerException.Codes.ServerSessionContainer);
		}
		
		public new ServerSession this[int AIndex]
		{
			get { return (ServerSession)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
		
		public ServerSession GetSession(int ASessionID)
		{
			foreach (ServerSession LSession in this)
				if (LSession.SessionID == ASessionID)
					return LSession;
			throw new ServerException(ServerException.Codes.SessionNotFound, ASessionID);
		}
	}
}
