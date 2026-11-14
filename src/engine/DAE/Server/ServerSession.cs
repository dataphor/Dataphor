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
using AT=Alphora.Dataphor.DAE.Device.ApplicationTransaction;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerSession : ServerChildObject, IServerSession
	{		
		internal ServerSession
		(
			Engine server, 
			int sessionID, 
			SessionInfo sessionInfo,
			Schema.User user
		) : base()
		{
			_server = server;
			_sessionID = sessionID;
			_sessionInfo = sessionInfo;
			_sessionObjects = new Schema.Objects();
			_sessionOperators = new Schema.Objects();
			_user = user;
			_processes = new ServerProcesses();
		}
		
		// Dispose
		protected override void Dispose(bool disposing)
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
									if (_debugger != null)
										_debugger.Stop();
								}
								finally
								{
									if (_applicationTransactions != null)
									{
										EndApplicationTransactions();
										_applicationTransactions = null;
									}
								}
							}
							finally
							{
								if (_sessionObjects  != null)
								{
									DropSessionObjects();
									_sessionObjects  = null;
									_sessionOperators = null;
								}
							}
						}
						finally
						{
							if (_processes != null)
							{
								try
								{
									StopProcesses();
								}
								finally
								{
									_processes.Dispose();
									_processes = null;
								}
							}
						}
					}
					finally
					{
						if (_cursorManager != null)
						{
							_cursorManager.Dispose();
							_cursorManager = null;
						}
					}
				}
				finally
				{
					_sessionInfo = null;
					_sessionID = -1;
					_user = null;
					_server = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
        
		// Processes
		private ServerProcesses _processes;
		public ServerProcesses Processes { get { return _processes; } }		

		// Server
		private Engine _server;
		public Engine Server { get { return _server; } }
		
		IServer IServerSession.Server { get { return _server; } }
        
		// SessionID
		private int _sessionID = -1;
		public int SessionID  { get { return _sessionID; } }
        
		// User        
		private Schema.User _user;
		public Schema.User User { get { return _user; } }
		public void SetUser(Schema.User user)
		{
			_user = user;
		}

		// SessionInfo
		private SessionInfo _sessionInfo;        
		public SessionInfo SessionInfo { get { return _sessionInfo; } }

		// Plan Cache		
		public void AddCachedPlan(ServerProcess process, string statement, int contextHashCode, ServerPlan plan)
		{
			if (_sessionInfo.UsePlanCache && (Server.PlanCacheSize > 0) && !HasNonGeneratedSessionObjects())
			{
				plan.Program.IsCached = true;
				Server.PlanCache.Add(process, statement, contextHashCode, plan);
			}
		}
		
		public ServerPlan GetCachedPlan(ServerProcess process, string statement, int contextHashCode)
		{
			if (_sessionInfo.UsePlanCache && (Server.PlanCacheSize > 0) && !HasNonGeneratedSessionObjects())
				return Server.PlanCache.Get(process, statement, contextHashCode);
			return null;
		}
		
		public bool ReleaseCachedPlan(ServerProcess process, ServerPlan plan)
		{
			if (_sessionInfo.UsePlanCache && (Server.PlanCacheSize > 0) && (plan.Header != null) && (!plan.Header.IsInvalidPlan) && !HasNonGeneratedSessionObjects())
				return Server.PlanCache.Release(process, plan);
			return false;
		}
		
		// CurrentLibrary
		/// <summary> 
		///	Specifies a library which is being registered or loaded on this session. 
		///	All objects created on this session will be part of this library. 
		///	</summary>
		private Schema.LoadedLibrary _currentLibrary;
		public Schema.LoadedLibrary CurrentLibrary
		{
			get 
			{ 
				if (_currentLibrary == null)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.NoCurrentLibrary);
				return _currentLibrary; 
			}
			set 
			{ 
				_currentLibrary = value; 
			}
		}
		
		public Schema.NameResolutionPath NameResolutionPath { get { return CurrentLibrary.GetNameResolutionPath(_server.SystemLibrary); } }

		// ApplicationTransactions
		private Dictionary<Guid, AT.ApplicationTransaction> _applicationTransactions = new Dictionary<Guid, AT.ApplicationTransaction>();
		public Dictionary<Guid, AT.ApplicationTransaction> ApplicationTransactions { get { return _applicationTransactions; } }
		
		private void EndApplicationTransactions()
		{
			while (_applicationTransactions.Count > 0)
				foreach (Guid iD in _applicationTransactions.Keys)
				{
					_server.RunScript(String.Format("System.RollbackApplicationTransaction(Guid('{0}'));", iD.ToString()), String.Empty);
					break;
				}
		}

		// Session-scoped table variables
		private NativeTables _tables;
		public NativeTables Tables
		{
			get
			{
				if (_tables == null)
					_tables = new NativeTables();
				return _tables;
			}
		}

		// SessionObjects 		
		private Schema.Objects _sessionObjects;
		public Schema.Objects SessionObjects { get { return _sessionObjects; } }
		
		// SessionOperators
		private Schema.Objects _sessionOperators;
		public Schema.Objects SessionOperators { get { return _sessionOperators; } }
		
		private void DropSessionObjects()
		{
			if (HasSessionObjects())
			{
				List<String> objectNames = new List<String>();
				for (int index = 0; index < _sessionObjects.Count; index++)
					objectNames.Add(((Schema.SessionObject)_sessionObjects[index]).GlobalName);
					
				for (int index = 0; index < _sessionOperators.Count; index++)
				{
					_server._systemProcess.CatalogDeviceSession.ResolveOperatorName(((Schema.SessionObject)_sessionOperators[index]).GlobalName);
					OperatorMap operatorMap = _server.Catalog.OperatorMaps[((Schema.SessionObject)_sessionOperators[index]).GlobalName];
					foreach (OperatorSignature signature in operatorMap.Signatures.Signatures.Values)
						objectNames.Add(signature.Operator.Name);
				}
				
				string[] objectNameArray = new string[objectNames.Count];
				for (int index = 0; index < objectNames.Count; index++)
					objectNameArray[index] = objectNames[index];
					
				Block block = (Block)_server.Catalog.EmitDropStatement(_server._systemProcess.CatalogDeviceSession, objectNameArray, String.Empty);
				
				_server._systemProcess.BeginCall();
				try
				{
					ServerStatementPlan plan = new ServerStatementPlan(_server._systemProcess);
					try
					{
						// Push a timestamp safe context to prevent the drops from flushing cache-points
						plan.Plan.EnterTimeStampSafeContext();
						try
						{
							for (int index = 0; index < block.Statements.Count; index++)
							{
								plan.Program.Code = Compiler.Compile(plan.Plan, block.Statements[index]);
								plan.Program.Execute(null);
							}
						}
						finally
						{
							plan.Plan.ExitTimeStampSafeContext();
						}
					}
					finally
					{
						plan.Dispose();
					}
				}
				catch (Exception E)
				{
					throw WrapException(E);
				}
				finally
				{
					_server._systemProcess.EndCall();
				}
					
				//FServer.RunScript(new D4TextEmitter().Emit(FServer.Catalog.EmitDropStatement(FServer.FSystemProcess, LObjectNameArray, String.Empty)), FCurrentLibrary.Name);
			}
		}
		
		public bool HasSessionObjects()
		{
			return ((_sessionObjects == null ? 0 : _sessionObjects.Count) + (_sessionOperators == null ? 0 : _sessionOperators.Count)) > 0;
		}
		
		public bool HasNonGeneratedSessionObjects()
		{
			if ((_sessionObjects == null) && (_sessionOperators == null))
				return false;
				
			// ASSERTION: The only way a session object could be marked generated is if it is
			// the check table created to track deferred constraint checks.
			if (_sessionObjects != null)
				for (int index = 0; index < _sessionObjects.Count; index++)
					if (!_sessionObjects[index].IsGenerated)
						return true;
					
			// NOTE: The server does not currently create session operators to support any internal operations
			// If that ever changes, this needs to be changed to look for those generated operators.
			// For now, this is a shortcut for performance.
			if ((_sessionOperators != null) && (_sessionOperators.Count) > 0)
				return true;
			
			return false;
		}
		
		// CursorManager
		private CursorManager _cursorManager;
		public CursorManager CursorManager
		{
			get 
			{ 
				if (_cursorManager == null)
					_cursorManager = new CursorManager();
				return _cursorManager; 
			}
		}
		
		// Debug
		private Debugger _debugger;
		public Debugger Debugger { get { return _debugger; } }
		
		public Debugger CheckedDebugger
		{
			get
			{
				if (_debugger == null)
					throw new ServerException(ServerException.Codes.DebuggerNotStarted, _sessionID);
				return _debugger;
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
		internal void SetDebugger(Debugger debugger)
		{
			if ((_debugger != null) && (debugger != null) && (_debugger != debugger))
				throw new ServerException(ServerException.Codes.DebuggerAlreadyStarted, _sessionID);
			_debugger = debugger;
		}
		
		private int _debuggedByID;
		public int DebuggedByID { get { return _debuggedByID; } }
		
		/// <summary>
		/// Sets the ID of the debugger to which this session is attached.
		/// </summary>
		internal void SetDebuggedByID(int debuggedByID)
		{
			if (String.IsNullOrEmpty(_sessionInfo.CatalogCacheName))
				throw new ServerException(ServerException.Codes.CannotAttachToAnInProcessSession, _sessionID);
			if ((_debugger != null) && (_sessionID == debuggedByID))
				throw new ServerException(ServerException.Codes.CannotAttachToDebuggerSession, _sessionID);
			if ((_debuggedByID != 0) && (debuggedByID != 0))
				throw new ServerException(ServerException.Codes.DebuggerAlreadyAttachedToSession, _sessionID);
			_debuggedByID = debuggedByID;
		}

		// Execution
		internal Exception WrapException(Exception exception)
		{
			return _server.WrapException(exception);
		}

		private void StopProcesses()
		{
			while (_processes.Count > 0)
			{
				try
				{
					try
					{
						Server.TerminateProcessThread(_processes[0]);
					}
					catch (Exception E)
					{
						Server.LogError(E);
					}
					
					_processes.DisownAt(0).Dispose();
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
		/// <param name="processID">The ID of the process to be stopped.</param>
		public void StopProcess(int processID)
		{
			ServerProcess processToStop = null;
			lock (_processes)
				foreach (ServerProcess process in _processes)
					if (process.ProcessID == processID)
					{
						processToStop = process;
						break;
					}
			
			if (processToStop != null)
			{
				Server.TerminateProcess(processToStop);
				return;
			}

			throw new ServerException(ServerException.Codes.ProcessNotFound, processID);
		}

		// StartProcess
		public IServerProcess StartProcess(ProcessInfo processInfo)
		{
			try
			{
				ServerProcess process = new ServerProcess(this, processInfo);
				_processes.Add(process); // Is protected by a latch in the ServerChildObjects collection
				return process;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		// StopProcess
		public void StopProcess(IServerProcess process)
		{
			try
			{
				((ServerProcess)process).Dispose();	// Is protected by a latch in the ServerChildObjects collection
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		internal void RemoveDeferredConstraintChecks(Schema.TableVar tableVar)
		{
			if (_processes != null)
				foreach (ServerProcess process in _processes)
					process.RemoveDeferredConstraintChecks(tableVar);
		}
		
		internal void RemoveDeferredHandlers(Schema.EventHandler handler)
		{
			if (_processes != null)
				foreach (ServerProcess process in _processes)
					process.RemoveDeferredHandlers(handler);
		}
		
		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint constraint)
		{
			if (_processes != null)
				foreach (ServerProcess process in _processes)
					process.RemoveCatalogConstraintCheck(constraint);
		}
	}
	
	// ServerSessions
	public class ServerSessions : ServerChildObjects
	{		
		public ServerSessions() : base() {}
		public ServerSessions(bool isOwner) : base(isOwner) {}
		
		protected override void Validate(ServerChildObject objectValue)
		{
			if (!(objectValue is ServerSession))
				throw new ServerException(ServerException.Codes.ServerSessionContainer);
		}
		
		public new ServerSession this[int index]
		{
			get { return (ServerSession)base[index]; } 
			set { base[index] = value; } 
		}
		
		public ServerSession GetSession(int sessionID)
		{
			foreach (ServerSession session in this)
				if (session.SessionID == sessionID)
					return session;
			throw new ServerException(ServerException.Codes.SessionNotFound, sessionID);
		}
	}
}
