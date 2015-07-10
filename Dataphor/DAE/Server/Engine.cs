/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define LOGCACHEEVENTS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;

/*
	DAE Object Hierarchy ->
	
		System.Object
			|- ServerObject
			|	|- Server
			|	|- ServerChildObject
			|	|	|- ServerSession
			|	|	|- ServerProcess
			|	|	|- ServerScript
			|	|	|- ServerBatch
			|	|	|- ServerPlan
			|	|	|	|- ServerStatementPlan
			|	|	|	|- ServerExpressionPlan
			|	|	|- ServerCursor

 		System.MarshalByRefObject
			|- RemoteServerObject
				|- RemoteServer
				|- RemoteServerChildObject
					|- RemoteServerConnection
					|- RemoteServerSession
					|- RemoteServerProcess
					|- RemoteServerScript
					|- RemoteServerBatch
					|- RemoteServerPlan
					|	|- RemoteServerStatementPlan
					|	|- RemoteServerExpressionPlan
					|- RemoteServerCursor				

	DAE Server Transaction Management ->
	
		The DAE is capable of using the MS DTC through .NET Enterprise Services, if it is running on Windows 2000 or higher.  The UseDTC
		flag on the ServerProcess controls whether to use the DTC to control distributed transactions.  If the DTC is used, the DAE will simulate
		nested transactions by allowing multiple transactions to be started, however, once a rollback is initiated, all transactions will
		rollback because the nested transactions are only simulated within the DTC transaction.  If the DTC is not used, the DAE functions as
		an optimistic two phase commit coordinator.  Each device is responsible for ensuring the transactional integrity of its own data.
		Note that if the DTC is not used, the SQL devices will function as manual transaction processors.  When running under a DAE
		transaction, they will always vote to commit.
	
		All work in the DAE must be done within a transaction.
		If a transaction is not active at the time of a call, one is initiated.
		All transactions initiated during a call that are active after the call are committed if the call was successful, and rolled back otherwise.
		All active transactions on a session are rolled back when the session is disconnected.
*/

namespace Alphora.Dataphor.DAE.Server
{
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;
	using Schema = Alphora.Dataphor.DAE.Schema;

	/// <summary> Dataphor DAE Server class. </summary>
	/// <remarks>
	///		Provides an instance of a Dataphor DAE Server.  This object is usually accessed
	///		through the IServerXXX common interfaces which make up the DAE CLI.  Instances
	///		are usually created and obtained through the <see cref="ServerFactory"/> class.
	/// </remarks>
	public class Engine : ServerObject, IDisposable, IServer
	{
		// Do not localize
		public const string DefaultServerName = "Dataphor";
		public const string ServerLogName = @"Dataphor";											 
		public const string ServerSourceName = @"Dataphor Server";
		public const string UserRoleName = "System.User";
		public const string SystemUserID = "System";
		public const string AdminUserID = "Admin";
		public const int CatalogDeviceID = 1;
		public const string CatalogDeviceName = "System.Catalog";
		public const string TempDeviceName = "System.Temp";
		public const string ATDeviceName = "System.ApplicationTransactionDevice";
		public const int TempDeviceMaxRowCount = 1000;
		public const int ATDeviceMaxRowCount = 100;
		public const int MaxLogs = 9;
		public const int SystemSessionID = 0;
		public const int DefaultMaxConcurrentProcesses = 200;
		public const int DefaultMaxStackDepth = 32767; // Also specified in the base stack
		public const int DefaultMaxCallDepth = 100; // Also specified in the base window stack
		public const int DefaultProcessWaitTimeout = 30;
		public const int DefaultProcessTerminationTimeout = 30;
		public const int DefaultPlanCacheSize = 100;
		
		// constructor		
		public Engine() : base()
		{
			_sessions = new ServerSessions();
		}
        
		public void Dispose()
		{
			Dispose(true);
		}
        
		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					Stop();
				}
				finally
				{
					if (_sessions != null)
					{
						_sessions.Dispose();
						_sessions = null;
					}
				}
			}
			finally
			{
				_catalog = null;
            
				base.Dispose(disposing);
			}
		}

		// Name 
		private string _name = DefaultServerName;
		public virtual string Name
		{
			get { return _name; } 
			set 
			{ 
				CheckState(ServerState.Stopped);
				_name = (value == null ? DefaultServerName : value); 
			}
		}

		// Indicates that this server is only a definition repository and does not accept modification statements
		public virtual bool IsEngine
		{
			get { return true; }
		}

		#region Devices
		
		// TempDevice		
		protected MemoryDevice _tempDevice;
		public MemoryDevice TempDevice 
		{ 
			get 
			{ 
				if (_tempDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _tempDevice; 
			} 
		}

		// CatalogDevice
		protected CatalogDevice _catalogDevice;
		public CatalogDevice CatalogDevice 
		{ 
			get 
			{
				if (_catalogDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _catalogDevice; 
			} 
		}

		// ATDevice		
		protected ApplicationTransactionDevice _aTDevice;
		public ApplicationTransactionDevice ATDevice 
		{ 
			get 
			{ 
				if (_aTDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _aTDevice; 
			} 
		}
		
		// DeviceSettings
		private Schema.DeviceSettings _deviceSettings = new Schema.DeviceSettings();
		public Schema.DeviceSettings DeviceSettings
		{
			get { return _deviceSettings; }
		}

		public event DeviceNotifyEvent OnDeviceStarting;

		private void DoDeviceStarting(Schema.Device device)
		{
			if (OnDeviceStarting != null)
				OnDeviceStarting(this, device);
		}

		public event DeviceNotifyEvent OnDeviceStarted;

		private void DoDeviceStarted(Schema.Device device)
		{
			if (OnDeviceStarted != null)
				OnDeviceStarted(this, device);
		}

		public void StartDevice(ServerProcess process, Schema.Device device)
		{
			if (!device.Running)
			{
				// TODO: It's not at all clear that this would have had any effect.
				// 
				//AProcess.Plan.PushSecurityContext(new SecurityContext(ADevice.Owner));
				//try
				//{
				DoDeviceStarting(device);
				try
				{
					process.CatalogDeviceSession.StartDevice(device);
					process.CatalogDeviceSession.RegisterDevice(device);
				}
				catch (Exception exception)
				{
					throw new ServerException(ServerException.Codes.DeviceStartupError, exception, device.Name);
				}

				if ((device.ReconcileMode & ReconcileMode.Startup) != 0)
				{
					try
					{
						device.Reconcile(process);
					}
					catch (Exception exception)
					{
						throw new ServerException(ServerException.Codes.StartupReconciliationError, exception, device.Name);
					}
				}

				DoDeviceStarted(device);
				//}
				//finally
				//{
				//	AProcess.Plan.PopSecurityContext();
				//}
			}
		}

		private void StartDevices()
		{
			// Perform startup reconciliation as configured for each device
			foreach (Schema.Object objectValue in _catalog)
				if (objectValue is Schema.Device)
					try
					{
						StartDevice(_systemProcess, (Schema.Device)objectValue);
					}
					catch (Exception exception)
					{
						LogError(exception);
					}
		}

		private void StopDevices()
		{
			// Perform shutdown processing as configured for each device
			if (_catalog != null)
			{
				Schema.Device device;
				foreach (Schema.Object objectValue in _catalog)
				{
					if (objectValue is Schema.Device)
					{
						device = (Schema.Device)objectValue;
						try
						{
							if (device.Running)
								device.Stop(_systemProcess);
						}
						catch (Exception exception)
						{
							LogError(new ServerException(ServerException.Codes.DeviceShutdownError, exception, device.Name));
						}
					}
				}
			}
		}

		#endregion
		
		#region Plan Cache
		
		// PlanCache
		private PlanCache _planCache;
		public PlanCache PlanCache { get { return _planCache; } }
		
		public int PlanCacheCount
		{
			get { return _planCache.Count; }
		}
		
		// PlanCacheSize
		public int PlanCacheSize
		{
			get { return _planCache.Size; }
			set { _planCache.Resize(_systemProcess, value); }
		}

		#endregion
		
		#region StreamManager
		
		private ServerStreamManager _streamManager;
		public ServerStreamManager StreamManager 
		{ 
			get 
			{
				if (_streamManager == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _streamManager; 
			} 
		}

		#endregion
		
		#region Security
		
		// User Role
		protected Schema.Role _userRole;
		public Schema.Role UserRole
		{
			get
			{
				if (_userRole == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _userRole;
			}
		}

		// System User
		protected Schema.User _systemUser;
		public Schema.User SystemUser
		{
			get
			{
				if (_systemUser == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _systemUser;
			}
		}

		// Admin User
		protected Schema.User _adminUser;
		public Schema.User AdminUser
		{
			get
			{
				if (_adminUser == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _adminUser;
			}
		}

		protected virtual Schema.User ValidateLogin(int sessionID, SessionInfo sessionInfo)
		{
			return _systemUser;
		}

		#endregion
		
		#region Execution
		
		private object _syncHandle = new System.Object();
		private void BeginCall()
		{
			Monitor.Enter(_syncHandle);
		}
		
		private void EndCall()
		{
			Monitor.Exit(_syncHandle);
		}
		
		#endregion
		
		#region Processes

		internal protected ServerProcess _systemProcess;

		private int _maxConcurrentProcesses = DefaultMaxConcurrentProcesses;
		public int MaxConcurrentProcesses
		{
			get { return _maxConcurrentProcesses; }
			set { _maxConcurrentProcesses = value; }
		}
		
		private TimeSpan _processWaitTimeout = TimeSpan.FromSeconds((double)DefaultProcessWaitTimeout);
		public TimeSpan ProcessWaitTimeout
		{
			get { return _processWaitTimeout; }
			set { _processWaitTimeout = value; }
		}

		private TimeSpan _processTerminationTimeout = TimeSpan.FromSeconds((double)DefaultProcessTerminationTimeout);
		public TimeSpan ProcessTerminationTimeout
		{
			get { return _processTerminationTimeout; }
			set { _processTerminationTimeout = value; }
		}
		
		private int _runningProcesses = 0;
		private AutoResetEvent _processWaitEvent = new AutoResetEvent(true);
		
		internal void BeginProcessCall(ServerProcess process)
		{
			int runningProcesses = Interlocked.Increment(ref _runningProcesses);
			if (runningProcesses > _maxConcurrentProcesses)
				if (!_processWaitEvent.WaitOne(_processWaitTimeout))
				{
					Interlocked.Decrement(ref _runningProcesses);
					throw new ServerException(ServerException.Codes.ProcessWaitTimeout);
				}
		}
		
		internal void EndProcessCall(ServerProcess process)
		{
			int runningProcesses = Interlocked.Decrement(ref _runningProcesses);
			if (runningProcesses >= _maxConcurrentProcesses)
				_processWaitEvent.Set();
		}
		
		public ServerProcess FindProcess(int processID)
		{
			foreach (ServerSession session in Sessions)
			{
				lock (session.Processes)
				{
					foreach (ServerProcess process in session.Processes)
						if (process.ProcessID == processID)
							return process;
				}
			}
			
			return null;
		}
		
		public ServerProcess GetProcess(int processID)
		{
			ServerProcess process = FindProcess(processID);
			if (process != null)
				return process;

			throw new ServerException(ServerException.Codes.ProcessNotFound, processID);
		}
		
		public void StopProcess(int processID)
		{
			TerminateProcess(GetProcess(processID));
		}

		public void TerminateProcessThread(ServerProcess process)
		{
			bool handleReleased = false;
			Monitor.Enter(process.ExecutionSyncHandle);
			try
			{
				if (process.ExecutingThread != null)
				{
					process.IsAborted = true;
					Monitor.Exit(process.ExecutionSyncHandle);
					handleReleased = true;
					
					DateTime endTime = DateTime.Now.AddTicks(ProcessTerminationTimeout.Ticks / 2);
					while (true)
					{
						Debug.Debugger debugger = null;
						Monitor.Enter(process.ExecutionSyncHandle);
						try
						{
							if ((process.ExecutingThread == null) || !process.ExecutingThread.IsAlive)
								return;
								
							if (process.DebuggedBy != null)
								debugger = process.DebuggedBy;
						}
						finally
						{
							Monitor.Exit(process.ExecutionSyncHandle);
						}
						
						if ((debugger != null) && debugger.IsPaused)
							debugger.Detach(process);

						System.Threading.Thread.SpinWait(100);
						if (DateTime.Now > endTime)
							throw new ServerException(ServerException.Codes.ProcessNotResponding);
					}
				}
			}
			finally
			{
				if (!handleReleased)
					Monitor.Exit(process.ExecutionSyncHandle);
			}
		}
		
		public void TerminateProcess(ServerProcess process)
		{
			TerminateProcessThread(process);
			((IServerSession)process.ServerSession).StopProcess(process);
		}
		
		#endregion

		#region Sessions

		internal protected ServerSession _systemSession;

		private ServerSessions _sessions;
		public ServerSessions Sessions { get { return _sessions; } }

		private int _nextSessionID = 1;
		private int GetNextSessionID()
		{
			return Interlocked.Increment(ref _nextSessionID);
		}

		public ServerSession GetSession(int sessionID)
		{
			return _sessions.GetSession(sessionID);
		}

		public void CloseSession(int sessionID)
		{
			GetSession(sessionID).Dispose();
		}

		private void CloseSessions()
		{
			if (_sessions != null)
			{
				while (_sessions.Count > 0)
				{
					try
					{
						InternalDisconnect(_sessions[0]);
					}
					catch (Exception E)
					{
						LogError(E);
					}
				}
			}
		}

		public void DropSessionObject(Schema.CatalogObject objectValue)
		{
			ServerSession session = Sessions.GetSession(objectValue.SessionID);
			lock (session.SessionObjects)
			{
				int objectIndex = session.SessionObjects.IndexOf(objectValue.SessionObjectName);
				if ((objectIndex >= 0) && (((Schema.SessionObject)session.SessionObjects[objectIndex]).GlobalName == objectValue.Name))
					session.SessionObjects.RemoveAt(objectIndex); 				
			}
		}
		
		public void DropSessionOperator(Schema.Operator operatorValue)
		{
			if (!Catalog.OperatorMaps.ContainsName(operatorValue.OperatorName))
			{
				ServerSession session = Sessions.GetSession(operatorValue.SessionID);
				lock (session.SessionOperators)
				{
					int operatorIndex = session.SessionOperators.IndexOf(operatorValue.SessionObjectName);
					if ((operatorIndex >= 0) && (((Schema.SessionObject)session.SessionOperators[operatorIndex]).GlobalName == operatorValue.OperatorName))
						session.SessionOperators.RemoveAt(operatorIndex); 						
				}
			}
		}

		internal void RemoveDeferredConstraintChecks(Schema.TableVar tableVar)
		{
			foreach (ServerSession session in Sessions)
				session.RemoveDeferredConstraintChecks(tableVar);
		}

		internal void RemoveDeferredHandlers(Schema.EventHandler handler)
		{
			foreach (ServerSession session in Sessions)
				session.RemoveDeferredHandlers(handler);
		}

		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint constraint)
		{
			foreach (ServerSession session in Sessions)
				session.RemoveCatalogConstraintCheck(constraint);
		}

		#endregion

		#region Exceptions
		
		public Exception WrapException(Exception exception)
		{
			if (_logErrors)
				LogError(exception);
				
			return exception;
		}

		#endregion

		#region State

		private ServerState _state;
		public ServerState State { get { return _state; } }

		private void SetState(ServerState newState)
		{
			_state = newState;
		}

		protected void CheckState(ServerState state)
		{
			if (_state != state)
				throw new ServerException(ServerException.Codes.InvalidServerState, state.ToString());
		}

		#endregion

		#region Start

		public void Start()
		{
			BeginCall();
			try
			{
				if (_state == ServerState.Stopped)
				{
					try
					{
						SetState(ServerState.Starting);
						StartLog();
						_nextSessionID = 0;
						InternalStart();
						SetState(ServerState.Started);
						LogEvent(DAE.Server.LogEvent.ServerStarted);
						InternalStarted();
					}
					catch
					{
						Stop();
						throw;
					}
				}
			}
			catch (Exception exception)
			{
                throw WrapException(exception);
			}
			finally
			{
				EndCall();
			}
		}
        
		protected virtual void InternalStarted()
		{
			StartDevices();
		}
		
		protected virtual void InternalStart()
		{
			InternalCreateCatalog();
			InitializeAvailableLibraries();
			InitializeCatalog();
			RegisterCatalog();

			if (!_firstRun)
				LoadServerState(); // Load server state from the persistent store
		}

		protected virtual void LoadServerState()
		{
			// abstract
		}

		private void RegisterCoreSystemObjects()
		{
			// Creates and registers the core system objects required to compile and execute any D4 statements
			// This will only be called if this is a repository, or if this is a first-time startup for a new server

			_systemProcess.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				InternalRegisterCoreSystemObjects();

				_systemProcess.CommitTransaction();
			}
			catch
			{
				_systemProcess.RollbackTransaction();
				throw;
			}
		}

		protected virtual void InternalRegisterCoreSystemObjects()
		{
			// Create the Admin user
			_adminUser = new Schema.User(AdminUserID, "Administrator", String.Empty);

			// Register the System and Admin users
			_systemProcess.CatalogDeviceSession.InsertUser(_systemUser);
			_systemProcess.CatalogDeviceSession.InsertUser(_adminUser);

			_userRole = new Schema.Role(UserRoleName);
			_userRole.Owner = _systemUser;
			_userRole.Library = _systemLibrary;
			_systemProcess.CatalogDeviceSession.InsertRole(_userRole);

			// Register the Catalog device
			_systemProcess.CatalogDeviceSession.InsertCatalogObject(_catalogDevice);

			// Create the Temp Device
			_tempDevice = new MemoryDevice(Schema.Object.GetNextObjectID(), TempDeviceName);
			_tempDevice.Owner = _systemUser;
			_tempDevice.Library = _systemLibrary;
			_tempDevice.ClassDefinition = new ClassDefinition("System.MemoryDevice");
			_tempDevice.ClassDefinition.Attributes.Add(new ClassAttributeDefinition("MaxRowCount", TempDeviceMaxRowCount.ToString()));
			_tempDevice.MaxRowCount = TempDeviceMaxRowCount;
			_tempDevice.Start(_systemProcess);
			_tempDevice.Register(_systemProcess);
			_systemProcess.CatalogDeviceSession.InsertCatalogObject(_tempDevice);

			// Create the A/T Device
			_aTDevice = new ApplicationTransactionDevice(Schema.Object.GetNextObjectID(), ATDeviceName);
			_aTDevice.Owner = _systemUser;
			_aTDevice.Library = _systemLibrary;
			_aTDevice.ClassDefinition = new ClassDefinition("System.ApplicationTransactionDevice");
			_aTDevice.ClassDefinition.Attributes.Add(new ClassAttributeDefinition("MaxRowCount", ATDeviceMaxRowCount.ToString()));
			_aTDevice.MaxRowCount = ATDeviceMaxRowCount;
			_aTDevice.Start(_systemProcess);
			_aTDevice.Register(_systemProcess);
			_systemProcess.CatalogDeviceSession.InsertCatalogObject(_aTDevice);
		}

		#endregion

		#region Stop
		
		public void Stop()
		{
			BeginCall();
			try
			{
				if ((_state == ServerState.Starting) || (_state == ServerState.Started))
				{
					try
					{
						SetState(ServerState.Stopping);
						InternalStopping();
					}
					finally
					{
						InternalStop();
						SetState(ServerState.Stopped);
						LogEvent(DAE.Server.LogEvent.ServerStopped);
						StopLog();
					}
				}
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
			finally
			{
				EndCall();
			}
		}
		
		private void InternalStopping(){}

		private void InternalStop()
		{
			try
			{
				try
				{
					try
					{
						try
						{
							if (_planCache != null)
							{
								_planCache.Clear(_systemProcess);
								_planCache = null;
							}
						}
						finally
						{
							StopDevices();
						}
					}
					finally
					{
						CloseSessions();
					}
				}
				finally
				{
					_systemSession = null;
					_systemProcess = null;

					if (_streamManager != null)
					{
						_streamManager.Dispose();
						_streamManager = null;
					}
				}
			}
			finally
			{
				UninitializeAvailableLibraries();
			}
		}

		#endregion
		
		#region Connect / disconnect
				
		// Connect
		public IServerSession Connect(SessionInfo sessionInfo)
		{
			BeginCall();
			try
			{
				#if TIMING
				System.Diagnostics.Debug.WriteLine(String.Format("{0} -- IServer.Connect", DateTime.Now.ToString("hh:mm:ss.ffff")));
				#endif
				CheckState(ServerState.Started);
				#if !SILVERLIGHT
				if (String.IsNullOrEmpty(sessionInfo.HostName))
					sessionInfo.HostName = System.Environment.MachineName;
				#endif
				return (IServerSession)InternalConnect(GetNextSessionID(), sessionInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
		
		// Disconnect
		public void Disconnect(IServerSession session)
		{
			try
			{
				try
				{
					// Clean up any session resources
					InternalDisconnect((ServerSession)session);
				}
				catch (Exception E)
				{
					throw WrapException(E);
				}
			}
			finally
			{
				BeginCall();
				try
				{
					// Remove the session from the sessions list
					_sessions.SafeDisown((ServerSession)session);
				}
				finally
				{
					EndCall();
				}
			}
		}
		
		protected internal IServerSession ConnectAs(SessionInfo sessionInfo)
		{
			sessionInfo.Password = Schema.SecurityUtility.DecryptPassword(_systemProcess.CatalogDeviceSession.ResolveUser(sessionInfo.UserID).Password);
			return ((IServer)this).Connect(sessionInfo);
		}
		
		private ServerSession InternalConnect(int sessionID, SessionInfo sessionInfo)
		{
			Schema.User user = ValidateLogin(sessionID, sessionInfo);
			ServerSession session = new ServerSession(this, sessionID, sessionInfo, user);
			try
			{
				Schema.LoadedLibrary currentLibrary = null;
				if (sessionInfo.DefaultLibraryName != String.Empty)
				{
					if (_systemProcess == null)
						currentLibrary = _catalog.LoadedLibraries[sessionInfo.DefaultLibraryName];
					else
						currentLibrary = _systemProcess.CatalogDeviceSession.ResolveLoadedLibrary(sessionInfo.DefaultLibraryName, false);
				}
				
				if (currentLibrary == null)
					currentLibrary = _catalog.LoadedLibraries[GeneralLibraryName];
					
				session.CurrentLibrary = currentLibrary;

				_sessions.Add(session);
				return session;
			}
			catch
			{
				session.Dispose();
				throw;
			}
		}
        
		private void InternalDisconnect(ServerSession session)
		{
			session.Dispose();
		}
		
		#endregion

		#region Debuggers

		/// <summary>
		/// Returns a list of currently running debuggers in the DAE.
		/// </summary>
		public List<Debug.Debugger> GetDebuggers()
		{
			BeginCall();
			try
			{
				List<Debug.Debugger> result = new List<Debug.Debugger>();
				foreach (ServerSession session in _sessions)
					if (session.Debugger != null)
						result.Add(session.Debugger);

				return result;
			}
			finally
			{
				EndCall();
			}
		}

		public Debug.Debugger GetDebugger(int debuggerID)
		{
			BeginCall();
			try
			{
				return _sessions.GetSession(debuggerID).CheckedDebugger;
			}
			finally
			{
				EndCall();
			}
		}

		#endregion
		
		#region Logging
				
		private bool _loggingEnabled = true;
		/// <summary>Determines whether the DAE instance will create and manage a log for writing events and errors.</summary>
		public bool LoggingEnabled
		{
			get { return _loggingEnabled; }
			set
			{
				CheckState(ServerState.Stopped);
				_loggingEnabled = value;
			}
		}
		
		private bool _logErrors = false;
		/// <summary>Determines whether the server will use the Dataphor event log to report errors that are returned to clients.</summary>
		public bool LogErrors
		{
			get { return _logErrors; }
			set { _logErrors = value; }
		}
		
		public void LogError(Exception exception)
		{
			LogMessage(LogEntryType.Error, ExceptionUtility.DetailedDescription(exception));
		}
		
		public void LogMessage(string description)
		{
			LogMessage(LogEntryType.Information, description);
		}
		
		public void LogEvent(LogEvent eventValue)
		{
			LogMessage(LogEntryType.Information, String.Format("Event: {0}", eventValue.ToString()));
		}

		public virtual void LogMessage(LogEntryType entryType, string description)
		{
			// abstract
		}

		protected virtual void StartLog()
		{
			// abstract
		}

		protected virtual void StopLog()
		{
			// abstract
		}

		public virtual List<string> ListLogs()
		{
			return new List<string>();
		}

		#endregion
		
		#region Internal API calls

		public void RunScript(string script)
		{
			RunScript(_systemProcess, script, String.Empty, null);
		}

		/// <summary> Runs the given script as the specified library. </summary>
		/// <remarks> LibraryName may be the empty string. </remarks>
		public void RunScript(string script, string libraryName)
		{
			RunScript(_systemProcess, script, libraryName, null);
		}

		public void RunScript(ServerProcess process, string script)
		{
			RunScript(process, script, String.Empty, null);
		}

		public void RunScript(ServerProcess process, string script, string libraryName, DAE.Debug.DebugLocator locator)
		{
			if (libraryName != String.Empty)
				process.ServerSession.CurrentLibrary = process.CatalogDeviceSession.ResolveLoadedLibrary(libraryName);
			IServerScript localScript = ((IServerProcess)process).PrepareScript(script, locator);
			try
			{
				localScript.Execute(null);
			}
			finally
			{
				((IServerProcess)process).UnprepareScript(localScript);
			}
		}

		#endregion
		
		#region Libraries

		public const string SystemLibraryName = @"System";
		public const string GeneralLibraryName = @"General";

		// SystemLibrary		
		protected Schema.LoadedLibrary _systemLibrary;
		public Schema.LoadedLibrary SystemLibrary
		{
			get
			{
				if (_systemLibrary == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return _systemLibrary;
			}
		}

		public void LibraryUnloaded(string libraryName)
		{
			foreach (ServerSession session in Sessions)
				if (session.CurrentLibrary.Name == libraryName)
					session.CurrentLibrary = Catalog.LoadedLibraries[Engine.GeneralLibraryName];
		}

		/// <summary>Event that is fired whenever a library begins loading.</summary>		
		public event LibraryNotifyEvent OnLibraryLoading;

		public void DoLibraryLoading(string libraryName)
		{
			if (OnLibraryLoading != null)
				OnLibraryLoading(this, libraryName);
		}

		/// <summary>Event that is fired whenever a library is done being loaded.</summary>
		public event LibraryNotifyEvent OnLibraryLoaded;

		public void DoLibraryLoaded(string libraryName)
		{
			if (OnLibraryLoaded != null)
				OnLibraryLoaded(this, libraryName);
		}

		private void InitializeAvailableLibraries()
		{
		}
		
		private void UninitializeAvailableLibraries()
		{
		}
		
		public virtual void LoadAvailableLibraries()
		{
			lock (_catalog.Libraries)
			{
				_catalog.UpdateTimeStamp();
				InternalLoadAvailableLibraries();

				// Create the implicit system library
				Schema.Library systemLibrary = new Schema.Library(SystemLibraryName);
				systemLibrary.Files.Add(new Schema.FileReference("Alphora.Dataphor.DAE.dll", true));
				Version version = AssemblyNameUtility.GetVersion(typeof(Engine).Assembly.FullName); // HACK: Have to use this instead of Assembly.GetName() to work around Silverlight security
				systemLibrary.Version = new VersionNumber(version.Major, version.Minor, version.Build, version.Revision);
				systemLibrary.DefaultDeviceName = TempDeviceName;
				_catalog.Libraries.Add(systemLibrary);
			}
		}

		protected virtual void InternalLoadAvailableLibraries()
		{
			// virtual
		}

		#endregion
		
		#region Catalog

		// Catalog
		private Schema.Catalog _catalog;
		public Schema.Catalog Catalog { get { return _catalog; } }

		private void InternalCreateCatalog()
		{
			_catalog = new Schema.Catalog();
			_planCache = new PlanCache(DefaultPlanCacheSize);
			if (_streamManager == null)
				_streamManager = new ServerStreamManager(this);
		}

		private void RegisterCatalog()
		{
			// Startup the catalog
			if (!IsEngine && _firstRun)
			{
				LogMessage("Registering system catalog...");
				using (Stream stream = typeof(Engine).Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.SystemCatalog.d4"))
				{
					RunScript(new StreamReader(stream).ReadToEnd(), SystemLibraryName);
				}
				LogMessage("System catalog registered.");
				LogMessage("Registering debug operators...");
				using (Stream stream = typeof(Engine).Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Debug.Debug.d4"))
				{
					RunScript(new StreamReader(stream).ReadToEnd(), SystemLibraryName);
				}
				LogMessage("Debug operators registered.");
			}
		}

		protected bool _firstRun; // Indicates whether or not this is the first time this server has run on the configured store

		/*
			Catalog Startup ->
				Catalog startup occurs in 5 phases
					Bootstrap ->
						Creates the SystemUser and CatalogDevice, then connects the SystemSession and opens a device session with the catalog device
					Core ->
						Creates the core catalog objects required to compile D4 statements. These objects are programmatically created by the
						server and include the Admin user, User role, Temp device, ApplicationTransaction device and the Server-level rights.
					Base ->
						These are the basic objects required to facilitate caching and D4 compilation. These objects are created by running
						the DataTypes.d4 script, and include the majority of the system data types. All the objects created up to this phase
						constitute the base objects that will always be present in any given instance of a Dataphor Server, and are used as
						the default set of cached objects.
					System ->
						The system objects are all the remaining objects in the System library, and are created by running the SystemCatalog.d4
						script. These objects are only created on a first-time run for a given catalog store.
					Load ->
						The load phase finishes preparing the server to compile and run D4 statements by restoring server state.
		*/
		private void InitializeCatalog()
		{
			LogMessage("Initializing Catalog...");
			
			// Create the Catalog device
			// Note that this must be the first object created to avoid the ID being different on subsequent loads
			Schema.Object.SetNextObjectID(0);
			_catalogDevice = CreateCatalogDevice();

			// Create the system user
			_systemUser = new Schema.User(SystemUserID, "System User", String.Empty);

			// Create the system library
			_systemLibrary = new Schema.LoadedLibrary(SystemLibraryName);
			_systemLibrary.Owner = _systemUser;
			LoadSystemAssemblies();
			_catalog.LoadedLibraries.Add(_systemLibrary);

			// Load available libraries
			LoadAvailableLibraries();

			// Connect the System Session
			if (_systemSession != null)
			{
				_systemSession.Dispose();
				_systemProcess = null;
			}
			_systemSession = (ServerSession)InternalConnect(SystemSessionID, new SessionInfo(_systemUser.ID, _systemUser.Password, SystemLibraryName));
			_systemSession.SessionInfo.UsePlanCache = false;
			_systemProcess = (ServerProcess)((IServerSession)_systemSession).StartProcess(new ProcessInfo(_systemSession.SessionInfo));
			_systemProcess.SuppressWarnings = true;

			// Register the Catalog device
			_catalogDevice.Owner = _systemUser;
			_catalogDevice.Library = _systemLibrary;
			_catalogDevice.ClassDefinition = new ClassDefinition("System.CatalogDevice");
			_catalogDevice.Start(_systemProcess);
			_catalogDevice.Register(_systemProcess);

			_firstRun = DetermineFirstRun();

			// If this is a repository or there are no objects in the catalog, register, else resolve
			InternalInitializeCatalog();

			// Bind the native type references to the system data types
			BindNativeTypes();
			
			LogMessage("Catalog Initialized.");
		}
		
		protected virtual CatalogDevice CreateCatalogDevice()
		{
			return new CatalogDevice(Schema.Object.GetNextObjectID(), CatalogDeviceName);
		}
		
		protected virtual void LoadSystemAssemblies()
		{
			Assembly dAEAssembly = typeof(Engine).Assembly;
			_systemLibrary.Assemblies.Add(dAEAssembly);
			_catalog.ClassLoader.RegisterAssembly(_systemLibrary, dAEAssembly);
		}

		protected virtual void InternalInitializeCatalog()
		{
			// create and register the core system objects (Admin user, User role, Temp device, A/T device, Server rights)
			RegisterCoreSystemObjects();

			// register the system data types
			RegisterSystemDataTypes();

			// If this is not a repository, snapshot the base catalog objects
			SnapshotBaseCatalogObjects();
		}

		protected virtual void SnapshotBaseCatalogObjects()
		{
			// virtual
		}

		protected virtual bool DetermineFirstRun()
		{
			return true;
		}

		private void RegisterSystemDataTypes()
		{
			using (Stream stream = typeof(Engine).Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.DataTypes.d4"))
			{
				RunScript(new StreamReader(stream).ReadToEnd(), SystemLibraryName);
			}
		}

		private void BindNativeTypes()
		{
			_catalog.DataTypes.SystemBoolean.NativeType = typeof(bool);
			_catalog.DataTypes.SystemByte.NativeType = typeof(byte);
			_catalog.DataTypes.SystemShort.NativeType = typeof(short);
			_catalog.DataTypes.SystemInteger.NativeType = typeof(int);
			_catalog.DataTypes.SystemLong.NativeType = typeof(long);
			_catalog.DataTypes.SystemDecimal.NativeType = typeof(decimal);
			_catalog.DataTypes.SystemMoney.NativeType = typeof(decimal);
			_catalog.DataTypes.SystemTimeSpan.NativeType = typeof(TimeSpan);
			_catalog.DataTypes.SystemDateTime.NativeType = typeof(DateTime);
			_catalog.DataTypes.SystemDate.NativeType = typeof(DateTime);
			_catalog.DataTypes.SystemTime.NativeType = typeof(DateTime);
			_catalog.DataTypes.SystemGuid.NativeType = typeof(Guid);
			_catalog.DataTypes.SystemString.NativeType = typeof(string);
			_catalog.DataTypes.SystemName.NativeType = typeof(string);
			#if USEISTRING
			FCatalog.DataTypes.SystemIString.NativeType = typeof(string);
			#endif
			_catalog.DataTypes.SystemError.NativeType = typeof(Exception);
			_catalog.DataTypes.SystemBinary.NativeType = typeof(byte[]);
			_catalog.DataTypes.SystemGraphic.NativeType = typeof(byte[]);
		}
		
		public virtual void ClearCatalog()
		{
			InternalCreateCatalog();
			InitializeCatalog();
			RegisterCatalog();
			LoadServerState();
		}

		// CacheTimeStamp
		public long CacheTimeStamp { get { return _catalog.CacheTimeStamp; } }

		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp { get { return _catalog.PlanCacheTimeStamp; } }

		// DerivationTimeStamp
		public long DerivationTimeStamp { get { return _catalog.DerivationTimeStamp; } }

		/// <summary> Emits the creation script for the catalog and returns it as a string. </summary>
		public string ScriptCatalog(CatalogDeviceSession session)
		{
			return new D4TextEmitter().Emit(_catalog.EmitStatement(session, EmitMode.ForCopy, false));
		}

		public string ScriptLibrary(CatalogDeviceSession session, string libraryName)
		{
			return new D4TextEmitter().Emit(_catalog.EmitStatement(session, EmitMode.ForCopy, libraryName, false));
		}

		public string ScriptDropCatalog(CatalogDeviceSession session)
		{
			return new D4TextEmitter().Emit(_catalog.EmitDropStatement(session));
		}

		public string ScriptDropLibrary(CatalogDeviceSession session, string libraryName)
		{
			return new D4TextEmitter().Emit(_catalog.EmitDropStatement(session, new string[] { }, libraryName, false, false, true, true));
		}

		#endregion
	}
}
