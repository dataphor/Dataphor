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
		public const string CDefaultServerName = "Dataphor";
		public const string CServerLogName = @"Dataphor";											 
		public const string CServerSourceName = @"Dataphor Server";
		public const string CUserRoleName = "System.User";
		public const string CSystemUserID = "System";
		public const string CAdminUserID = "Admin";
		public const int CCatalogDeviceID = 1;
		public const string CCatalogDeviceName = "System.Catalog";
		public const string CTempDeviceName = "System.Temp";
		public const string CATDeviceName = "System.ApplicationTransactionDevice";
		public const int CTempDeviceMaxRowCount = 1000;
		public const int CATDeviceMaxRowCount = 100;
		public const int CMaxLogs = 9;
		public const int CSystemSessionID = 0;
		public const int CDefaultMaxConcurrentProcesses = 200;
		public const int CDefaultMaxStackDepth = 32767; // Also specified in the base stack
		public const int CDefaultMaxCallDepth = 1024; // Also specified in the base window stack
		public const int CDefaultProcessWaitTimeout = 30;
		public const int CDefaultProcessTerminationTimeout = 30;
		public const int CDefaultPlanCacheSize = 1000;
		
		// constructor		
		public Engine() : base()
		{
			FSessions = new ServerSessions();
		}
        
		public void Dispose()
		{
			Dispose(true);
		}
        
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					Stop();
				}
				finally
				{
					if (FSessions != null)
					{
						FSessions.Dispose();
						FSessions = null;
					}
				}
			}
			finally
			{
				FCatalog = null;
            
				base.Dispose(ADisposing);
			}
		}

		// Name 
		private string FName = CDefaultServerName;
		public virtual string Name
		{
			get { return FName; } 
			set 
			{ 
				CheckState(ServerState.Stopped);
				FName = (value == null ? CDefaultServerName : value); 
			}
		}

		// Indicates that this server is only a definition repository and does not accept modification statements
		public virtual bool IsEngine
		{
			get { return true; }
		}

		#region Devices
		
		// TempDevice		
		protected MemoryDevice FTempDevice;
		public MemoryDevice TempDevice 
		{ 
			get 
			{ 
				if (FTempDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FTempDevice; 
			} 
		}

		// CatalogDevice
		protected CatalogDevice FCatalogDevice;
		public CatalogDevice CatalogDevice 
		{ 
			get 
			{
				if (FCatalogDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FCatalogDevice; 
			} 
		}

		// ATDevice		
		protected ApplicationTransactionDevice FATDevice;
		public ApplicationTransactionDevice ATDevice 
		{ 
			get 
			{ 
				if (FATDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FATDevice; 
			} 
		}
		
		// DeviceSettings
		private Schema.DeviceSettings FDeviceSettings = new Schema.DeviceSettings();
		public Schema.DeviceSettings DeviceSettings
		{
			get { return FDeviceSettings; }
		}

		public event DeviceNotifyEvent OnDeviceStarting;

		private void DoDeviceStarting(Schema.Device ADevice)
		{
			if (OnDeviceStarting != null)
				OnDeviceStarting(this, ADevice);
		}

		public event DeviceNotifyEvent OnDeviceStarted;

		private void DoDeviceStarted(Schema.Device ADevice)
		{
			if (OnDeviceStarted != null)
				OnDeviceStarted(this, ADevice);
		}

		public void StartDevice(ServerProcess AProcess, Schema.Device ADevice)
		{
			if (!ADevice.Running)
			{
				// TODO: It's not at all clear that this would have had any effect.
				// 
				//AProcess.Plan.PushSecurityContext(new SecurityContext(ADevice.Owner));
				//try
				//{
				DoDeviceStarting(ADevice);
				try
				{
					AProcess.CatalogDeviceSession.StartDevice(ADevice);
					AProcess.CatalogDeviceSession.RegisterDevice(ADevice);
				}
				catch (Exception LException)
				{
					throw new ServerException(ServerException.Codes.DeviceStartupError, LException, ADevice.Name);
				}

				if ((ADevice.ReconcileMode & ReconcileMode.Startup) != 0)
				{
					try
					{
						ADevice.Reconcile(AProcess);
					}
					catch (Exception LException)
					{
						throw new ServerException(ServerException.Codes.StartupReconciliationError, LException, ADevice.Name);
					}
				}

				DoDeviceStarted(ADevice);
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
			foreach (Schema.Object LObject in FCatalog)
				if (LObject is Schema.Device)
					try
					{
						StartDevice(FSystemProcess, (Schema.Device)LObject);
					}
					catch (Exception LException)
					{
						LogError(LException);
					}
		}

		private void StopDevices()
		{
			// Perform shutdown processing as configured for each device
			if (FCatalog != null)
			{
				Schema.Device LDevice;
				foreach (Schema.Object LObject in FCatalog)
				{
					if (LObject is Schema.Device)
					{
						LDevice = (Schema.Device)LObject;
						try
						{
							if (LDevice.Running)
								LDevice.Stop(FSystemProcess);
						}
						catch (Exception LException)
						{
							LogError(new ServerException(ServerException.Codes.DeviceShutdownError, LException, LDevice.Name));
						}
					}
				}
			}
		}

		#endregion
		
		#region Plan Cache
		
		// PlanCache
		private PlanCache FPlanCache;
		public PlanCache PlanCache { get { return FPlanCache; } }
		
		public int PlanCacheCount
		{
			get { return FPlanCache.Count; }
		}
		
		// PlanCacheSize
		public int PlanCacheSize
		{
			get { return FPlanCache.Size; }
			set { FPlanCache.Resize(FSystemProcess, value); }
		}

		#endregion
		
		#region StreamManager
		
		private ServerStreamManager FStreamManager;
		public ServerStreamManager StreamManager 
		{ 
			get 
			{
				if (FStreamManager == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FStreamManager; 
			} 
		}

		#endregion
		
		#region Security
		
		// User Role
		protected Schema.Role FUserRole;
		public Schema.Role UserRole
		{
			get
			{
				if (FUserRole == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FUserRole;
			}
		}

		// System User
		protected Schema.User FSystemUser;
		public Schema.User SystemUser
		{
			get
			{
				if (FSystemUser == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FSystemUser;
			}
		}

		// Admin User
		protected Schema.User FAdminUser;
		public Schema.User AdminUser
		{
			get
			{
				if (FAdminUser == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FAdminUser;
			}
		}

		protected virtual Schema.User ValidateLogin(int ASessionID, SessionInfo ASessionInfo)
		{
			return FSystemUser;
		}

		#endregion
		
		#region Execution
		
		private object FSyncHandle = new System.Object();
		private void BeginCall()
		{
			Monitor.Enter(FSyncHandle);
		}
		
		private void EndCall()
		{
			Monitor.Exit(FSyncHandle);
		}
		
		#endregion
		
		#region Processes

		internal protected ServerProcess FSystemProcess;

		private int FMaxConcurrentProcesses = CDefaultMaxConcurrentProcesses;
		public int MaxConcurrentProcesses
		{
			get { return FMaxConcurrentProcesses; }
			set { FMaxConcurrentProcesses = value; }
		}
		
		private TimeSpan FProcessWaitTimeout = TimeSpan.FromSeconds((double)CDefaultProcessWaitTimeout);
		public TimeSpan ProcessWaitTimeout
		{
			get { return FProcessWaitTimeout; }
			set { FProcessWaitTimeout = value; }
		}

		private TimeSpan FProcessTerminationTimeout = TimeSpan.FromSeconds((double)CDefaultProcessTerminationTimeout);
		public TimeSpan ProcessTerminationTimeout
		{
			get { return FProcessTerminationTimeout; }
			set { FProcessTerminationTimeout = value; }
		}
		
		private int FRunningProcesses = 0;
		private AutoResetEvent FProcessWaitEvent = new AutoResetEvent(true);
		
		internal void BeginProcessCall(ServerProcess AProcess)
		{
			int LRunningProcesses = Interlocked.Increment(ref FRunningProcesses);
			if (LRunningProcesses > FMaxConcurrentProcesses)
				if (!FProcessWaitEvent.WaitOne(FProcessWaitTimeout))
				{
					Interlocked.Decrement(ref FRunningProcesses);
					throw new ServerException(ServerException.Codes.ProcessWaitTimeout);
				}
		}
		
		internal void EndProcessCall(ServerProcess AProcess)
		{
			int LRunningProcesses = Interlocked.Decrement(ref FRunningProcesses);
			if (LRunningProcesses >= FMaxConcurrentProcesses)
				FProcessWaitEvent.Set();
		}
		
		public ServerProcess FindProcess(int AProcessID)
		{
			foreach (ServerSession LSession in Sessions)
			{
				lock (LSession.Processes)
				{
					foreach (ServerProcess LProcess in LSession.Processes)
						if (LProcess.ProcessID == AProcessID)
							return LProcess;
				}
			}
			
			return null;
		}
		
		public ServerProcess GetProcess(int AProcessID)
		{
			ServerProcess LProcess = FindProcess(AProcessID);
			if (LProcess != null)
				return LProcess;

			throw new ServerException(ServerException.Codes.ProcessNotFound, AProcessID);
		}
		
		public void StopProcess(int AProcessID)
		{
			TerminateProcess(GetProcess(AProcessID));
		}

		public void TerminateProcessThread(ServerProcess AProcess)
		{
			bool LHandleReleased = false;
			Monitor.Enter(AProcess.ExecutionSyncHandle);
			try
			{
				if (AProcess.ExecutingThread != null)
				{
					AProcess.IsAborted = true;
					Monitor.Exit(AProcess.ExecutionSyncHandle);
					LHandleReleased = true;
					
					DateTime LEndTime = DateTime.Now.AddTicks(ProcessTerminationTimeout.Ticks / 2);
					while (true)
					{
						Debug.Debugger LDebugger = null;
						Monitor.Enter(AProcess.ExecutionSyncHandle);
						try
						{
							if ((AProcess.ExecutingThread == null) || !AProcess.ExecutingThread.IsAlive)
								return;
								
							if (AProcess.DebuggedBy != null)
								LDebugger = AProcess.DebuggedBy;
						}
						finally
						{
							Monitor.Exit(AProcess.ExecutionSyncHandle);
						}
						
						if ((LDebugger != null) && LDebugger.IsPaused)
							LDebugger.Detach(AProcess);

						System.Threading.Thread.SpinWait(100);
						if (DateTime.Now > LEndTime)
							throw new ServerException(ServerException.Codes.ProcessNotResponding);
					}
				}
			}
			finally
			{
				if (!LHandleReleased)
					Monitor.Exit(AProcess.ExecutionSyncHandle);
			}
		}
		
		public void TerminateProcess(ServerProcess AProcess)
		{
			TerminateProcessThread(AProcess);
			((IServerSession)AProcess.ServerSession).StopProcess(AProcess);
		}
		
		#endregion

		#region Sessions

		internal protected ServerSession FSystemSession;

		private ServerSessions FSessions;
		public ServerSessions Sessions { get { return FSessions; } }

		private int FNextSessionID = 1;
		private int GetNextSessionID()
		{
			return Interlocked.Increment(ref FNextSessionID);
		}

		public ServerSession GetSession(int ASessionID)
		{
			return FSessions.GetSession(ASessionID);
		}

		public void CloseSession(int ASessionID)
		{
			GetSession(ASessionID).Dispose();
		}

		private void CloseSessions()
		{
			if (FSessions != null)
			{
				while (FSessions.Count > 0)
				{
					try
					{
						InternalDisconnect(FSessions[0]);
					}
					catch (Exception E)
					{
						LogError(E);
					}
				}
			}
		}

		public void DropSessionObject(Schema.CatalogObject AObject)
		{
			ServerSession LSession = Sessions.GetSession(AObject.SessionID);
			lock (LSession.SessionObjects)
			{
				int LObjectIndex = LSession.SessionObjects.IndexOf(AObject.SessionObjectName);
				if ((LObjectIndex >= 0) && (((Schema.SessionObject)LSession.SessionObjects[LObjectIndex]).GlobalName == AObject.Name))
					LSession.SessionObjects.RemoveAt(LObjectIndex); 				
			}
		}
		
		public void DropSessionOperator(Schema.Operator AOperator)
		{
			if (!Catalog.OperatorMaps.ContainsName(AOperator.OperatorName))
			{
				ServerSession LSession = Sessions.GetSession(AOperator.SessionID);
				lock (LSession.SessionOperators)
				{
					int LOperatorIndex = LSession.SessionOperators.IndexOf(AOperator.SessionObjectName);
					if ((LOperatorIndex >= 0) && (((Schema.SessionObject)LSession.SessionOperators[LOperatorIndex]).GlobalName == AOperator.OperatorName))
						LSession.SessionOperators.RemoveAt(LOperatorIndex); 						
				}
			}
		}

		internal void RemoveDeferredConstraintChecks(Schema.TableVar ATableVar)
		{
			foreach (ServerSession LSession in Sessions)
				LSession.RemoveDeferredConstraintChecks(ATableVar);
		}

		internal void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			foreach (ServerSession LSession in Sessions)
				LSession.RemoveDeferredHandlers(AHandler);
		}

		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			foreach (ServerSession LSession in Sessions)
				LSession.RemoveCatalogConstraintCheck(AConstraint);
		}

		#endregion

		#region Exceptions
		
		public Exception WrapException(Exception AException)
		{
			if (FLogErrors)
				LogError(AException);
				
			return AException;
		}

		#endregion

		#region State

		private ServerState FState;
		public ServerState State { get { return FState; } }

		private void SetState(ServerState ANewState)
		{
			FState = ANewState;
		}

		protected void CheckState(ServerState AState)
		{
			if (FState != AState)
				throw new ServerException(ServerException.Codes.InvalidServerState, AState.ToString());
		}

		#endregion

		#region Start

		public void Start()
		{
			BeginCall();
			try
			{
				if (FState == ServerState.Stopped)
				{
					try
					{
						SetState(ServerState.Starting);
						StartLog();
						FNextSessionID = 0;
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
			catch (Exception LException)
			{
                throw WrapException(LException);
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

			if (!FFirstRun)
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

			FSystemProcess.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				InternalRegisterCoreSystemObjects();

				FSystemProcess.CommitTransaction();
			}
			catch
			{
				FSystemProcess.RollbackTransaction();
				throw;
			}
		}

		protected virtual void InternalRegisterCoreSystemObjects()
		{
			// Create the Admin user
			FAdminUser = new Schema.User(CAdminUserID, "Administrator", String.Empty);

			// Register the System and Admin users
			FSystemProcess.CatalogDeviceSession.InsertUser(FSystemUser);
			FSystemProcess.CatalogDeviceSession.InsertUser(FAdminUser);

			FUserRole = new Schema.Role(CUserRoleName);
			FUserRole.Owner = FSystemUser;
			FUserRole.Library = FSystemLibrary;
			FSystemProcess.CatalogDeviceSession.InsertRole(FUserRole);

			// Register the Catalog device
			FSystemProcess.CatalogDeviceSession.InsertCatalogObject(FCatalogDevice);

			// Create the Temp Device
			FTempDevice = new MemoryDevice(Schema.Object.GetNextObjectID(), CTempDeviceName);
			FTempDevice.Owner = FSystemUser;
			FTempDevice.Library = FSystemLibrary;
			FTempDevice.ClassDefinition = new ClassDefinition("System.MemoryDevice");
			FTempDevice.ClassDefinition.Attributes.Add(new ClassAttributeDefinition("MaxRowCount", CTempDeviceMaxRowCount.ToString()));
			FTempDevice.MaxRowCount = CTempDeviceMaxRowCount;
			FTempDevice.Start(FSystemProcess);
			FTempDevice.Register(FSystemProcess);
			FSystemProcess.CatalogDeviceSession.InsertCatalogObject(FTempDevice);

			// Create the A/T Device
			FATDevice = new ApplicationTransactionDevice(Schema.Object.GetNextObjectID(), CATDeviceName);
			FATDevice.Owner = FSystemUser;
			FATDevice.Library = FSystemLibrary;
			FATDevice.ClassDefinition = new ClassDefinition("System.ApplicationTransactionDevice");
			FATDevice.ClassDefinition.Attributes.Add(new ClassAttributeDefinition("MaxRowCount", CATDeviceMaxRowCount.ToString()));
			FATDevice.MaxRowCount = CATDeviceMaxRowCount;
			FATDevice.Start(FSystemProcess);
			FATDevice.Register(FSystemProcess);
			FSystemProcess.CatalogDeviceSession.InsertCatalogObject(FATDevice);
		}

		#endregion

		#region Stop
		
		public void Stop()
		{
			BeginCall();
			try
			{
				if ((FState == ServerState.Starting) || (FState == ServerState.Started))
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
			catch (Exception LException)
			{
				throw WrapException(LException);
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
							if (FPlanCache != null)
							{
								FPlanCache.Clear(FSystemProcess);
								FPlanCache = null;
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
					FSystemSession = null;
					FSystemProcess = null;

					if (FStreamManager != null)
					{
						FStreamManager.Dispose();
						FStreamManager = null;
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
		public IServerSession Connect(SessionInfo ASessionInfo)
		{
			BeginCall();
			try
			{
				#if TIMING
				System.Diagnostics.Debug.WriteLine(String.Format("{0} -- IServer.Connect", DateTime.Now.ToString("hh:mm:ss.ffff")));
				#endif
				CheckState(ServerState.Started);
				#if !SILVERLIGHT
				if (String.IsNullOrEmpty(ASessionInfo.HostName))
					ASessionInfo.HostName = System.Environment.MachineName;
				#endif
				return (IServerSession)InternalConnect(GetNextSessionID(), ASessionInfo);
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
		public void Disconnect(IServerSession ASession)
		{
			try
			{
				try
				{
					// Clean up any session resources
					InternalDisconnect((ServerSession)ASession);
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
					FSessions.SafeDisown((ServerSession)ASession);
				}
				finally
				{
					EndCall();
				}
			}
		}
		
		protected internal IServerSession ConnectAs(SessionInfo ASessionInfo)
		{
			ASessionInfo.Password = Schema.SecurityUtility.DecryptPassword(FSystemProcess.CatalogDeviceSession.ResolveUser(ASessionInfo.UserID).Password);
			return ((IServer)this).Connect(ASessionInfo);
		}
		
		private ServerSession InternalConnect(int ASessionID, SessionInfo ASessionInfo)
		{
			Schema.User LUser = ValidateLogin(ASessionID, ASessionInfo);
			ServerSession LSession = new ServerSession(this, ASessionID, ASessionInfo, LUser);
			try
			{
				Schema.LoadedLibrary LCurrentLibrary = null;
				if (ASessionInfo.DefaultLibraryName != String.Empty)
				{
					if (FSystemProcess == null)
						LCurrentLibrary = FCatalog.LoadedLibraries[ASessionInfo.DefaultLibraryName];
					else
						LCurrentLibrary = FSystemProcess.CatalogDeviceSession.ResolveLoadedLibrary(ASessionInfo.DefaultLibraryName, false);
				}
				
				if (LCurrentLibrary == null)
					LCurrentLibrary = FCatalog.LoadedLibraries[CGeneralLibraryName];
					
				LSession.CurrentLibrary = LCurrentLibrary;

				FSessions.Add(LSession);
				return LSession;
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}
        
		private void InternalDisconnect(ServerSession ASession)
		{
			ASession.Dispose();
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
				List<Debug.Debugger> LResult = new List<Debug.Debugger>();
				foreach (ServerSession LSession in FSessions)
					if (LSession.Debugger != null)
						LResult.Add(LSession.Debugger);

				return LResult;
			}
			finally
			{
				EndCall();
			}
		}

		public Debug.Debugger GetDebugger(int ADebuggerID)
		{
			BeginCall();
			try
			{
				return FSessions.GetSession(ADebuggerID).CheckedDebugger;
			}
			finally
			{
				EndCall();
			}
		}

		#endregion
		
		#region Logging
				
		private bool FLoggingEnabled = true;
		/// <summary>Determines whether the DAE instance will create and manage a log for writing events and errors.</summary>
		public bool LoggingEnabled
		{
			get { return FLoggingEnabled; }
			set
			{
				CheckState(ServerState.Stopped);
				FLoggingEnabled = value;
			}
		}
		
		private bool FLogErrors = false;
		/// <summary>Determines whether the server will use the Dataphor event log to report errors that are returned to clients.</summary>
		public bool LogErrors
		{
			get { return FLogErrors; }
			set { FLogErrors = value; }
		}
		
		public void LogError(Exception AException)
		{
			LogMessage(LogEntryType.Error, ExceptionUtility.DetailedDescription(AException));
		}
		
		public void LogMessage(string ADescription)
		{
			LogMessage(LogEntryType.Information, ADescription);
		}
		
		public void LogEvent(LogEvent AEvent)
		{
			LogMessage(LogEntryType.Information, String.Format("Event: {0}", AEvent.ToString()));
		}

		public virtual void LogMessage(LogEntryType AEntryType, string ADescription)
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

		public void RunScript(string AScript)
		{
			RunScript(FSystemProcess, AScript, String.Empty, null);
		}

		/// <summary> Runs the given script as the specified library. </summary>
		/// <remarks> LibraryName may be the empty string. </remarks>
		public void RunScript(string AScript, string ALibraryName)
		{
			RunScript(FSystemProcess, AScript, ALibraryName, null);
		}

		public void RunScript(ServerProcess AProcess, string AScript)
		{
			RunScript(AProcess, AScript, String.Empty, null);
		}

		public void RunScript(ServerProcess AProcess, string AScript, string ALibraryName, DAE.Debug.DebugLocator ALocator)
		{
			if (ALibraryName != String.Empty)
				AProcess.ServerSession.CurrentLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName);
			IServerScript LScript = ((IServerProcess)AProcess).PrepareScript(AScript, ALocator);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareScript(LScript);
			}
		}

		#endregion
		
		#region Libraries

		public const string CSystemLibraryName = @"System";
		public const string CGeneralLibraryName = @"General";

		// SystemLibrary		
		protected Schema.LoadedLibrary FSystemLibrary;
		public Schema.LoadedLibrary SystemLibrary
		{
			get
			{
				if (FSystemLibrary == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FSystemLibrary;
			}
		}

		public void LibraryUnloaded(string ALibraryName)
		{
			foreach (ServerSession LSession in Sessions)
				if (LSession.CurrentLibrary.Name == ALibraryName)
					LSession.CurrentLibrary = Catalog.LoadedLibraries[Engine.CGeneralLibraryName];
		}

		/// <summary>Event that is fired whenever a library begins loading.</summary>		
		public event LibraryNotifyEvent OnLibraryLoading;

		public void DoLibraryLoading(string ALibraryName)
		{
			if (OnLibraryLoading != null)
				OnLibraryLoading(this, ALibraryName);
		}

		/// <summary>Event that is fired whenever a library is done being loaded.</summary>
		public event LibraryNotifyEvent OnLibraryLoaded;

		public void DoLibraryLoaded(string ALibraryName)
		{
			if (OnLibraryLoaded != null)
				OnLibraryLoaded(this, ALibraryName);
		}

		private void InitializeAvailableLibraries()
		{
		}
		
		private void UninitializeAvailableLibraries()
		{
		}
		
		public virtual void LoadAvailableLibraries()
		{
			lock (FCatalog.Libraries)
			{
				FCatalog.UpdateTimeStamp();
				InternalLoadAvailableLibraries();

				// Create the implicit system library
				Schema.Library LSystemLibrary = new Schema.Library(CSystemLibraryName);
				LSystemLibrary.Files.Add(new Schema.FileReference("Alphora.Dataphor.DAE.dll", true));
				Version LVersion = typeof(Engine).Assembly.GetName().Version;
				LSystemLibrary.Version = new VersionNumber(LVersion.Major, LVersion.Minor, LVersion.Build, LVersion.Revision);
				LSystemLibrary.DefaultDeviceName = CTempDeviceName;
				FCatalog.Libraries.Add(LSystemLibrary);
			}
		}

		protected virtual void InternalLoadAvailableLibraries()
		{
			// virtual
		}

		#endregion
		
		#region Catalog

		// Catalog
		private Schema.Catalog FCatalog;
		public Schema.Catalog Catalog { get { return FCatalog; } }

		private void InternalCreateCatalog()
		{
			FCatalog = new Schema.Catalog();
			FPlanCache = new PlanCache(CDefaultPlanCacheSize);
			if (FStreamManager == null)
				FStreamManager = new ServerStreamManager(this);
		}

		private bool FCatalogRegistered;
		private void RegisterCatalog()
		{
			// Startup the catalog
			if (!FCatalogRegistered)
			{
				FCatalogRegistered = true;

				if (!IsEngine && FFirstRun)
				{
					LogMessage("Registering system catalog...");
					using (Stream LStream = typeof(Engine).Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.SystemCatalog.d4"))
					{
						RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
					}
					LogMessage("System catalog registered.");
					LogMessage("Registering debug operators...");
					using (Stream LStream = typeof(Engine).Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Debug.Debug.d4"))
					{
						RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
					}
					LogMessage("Debug operators registered.");
				}
			}
		}

		protected bool FFirstRun; // Indicates whether or not this is the first time this server has run on the configured store
		private bool FCatalogInitialized;
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
			if (!FCatalogInitialized)
			{
				LogMessage("Initializing Catalog...");
				
				// Create the Catalog device
				// Note that this must be the first object created to avoid the ID being different on subsequent loads
				Schema.Object.SetNextObjectID(0);
				FCatalogDevice = CreateCatalogDevice();

				// Create the system user
				FSystemUser = new Schema.User(CSystemUserID, "System User", String.Empty);

				// Create the system library
				FSystemLibrary = new Schema.LoadedLibrary(CSystemLibraryName);
				FSystemLibrary.Owner = FSystemUser;
				LoadSystemAssemblies();
				FCatalog.LoadedLibraries.Add(FSystemLibrary);

				// Load available libraries
				LoadAvailableLibraries();

				// Connect the System Session
				if (FSystemSession != null)
				{
					FSystemSession.Dispose();
					FSystemProcess = null;
				}
				FSystemSession = (ServerSession)InternalConnect(CSystemSessionID, new SessionInfo(FSystemUser.ID, FSystemUser.Password, CSystemLibraryName));
				FSystemSession.SessionInfo.UsePlanCache = false;
				FSystemProcess = (ServerProcess)((IServerSession)FSystemSession).StartProcess(new ProcessInfo(FSystemSession.SessionInfo));
				FSystemProcess.SuppressWarnings = true;

				// Register the Catalog device
				FCatalogDevice.Owner = FSystemUser;
				FCatalogDevice.Library = FSystemLibrary;
				FCatalogDevice.ClassDefinition = new ClassDefinition("System.CatalogDevice");
				FCatalogDevice.Start(FSystemProcess);
				FCatalogDevice.Register(FSystemProcess);

				FFirstRun = DetermineFirstRun();

				// If this is a repository or there are no objects in the catalog, register, else resolve
				InternalInitializeCatalog();

				// Bind the native type references to the system data types
				BindNativeTypes();
				
				LogMessage("Catalog Initialized.");
				
				FCatalogInitialized = true;
			}
		}
		
		protected virtual CatalogDevice CreateCatalogDevice()
		{
			return new CatalogDevice(Schema.Object.GetNextObjectID(), CCatalogDeviceName);
		}
		
		protected virtual void LoadSystemAssemblies()
		{
			Assembly LDAEAssembly = typeof(Engine).Assembly;
			FSystemLibrary.Assemblies.Add(LDAEAssembly);
			FCatalog.ClassLoader.RegisterAssembly(FSystemLibrary, LDAEAssembly);
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
			using (Stream LStream = typeof(Engine).Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.DataTypes.d4"))
			{
				RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
			}
		}

		private void BindNativeTypes()
		{
			FCatalog.DataTypes.SystemBoolean.NativeType = typeof(bool);
			FCatalog.DataTypes.SystemByte.NativeType = typeof(byte);
			FCatalog.DataTypes.SystemShort.NativeType = typeof(short);
			FCatalog.DataTypes.SystemInteger.NativeType = typeof(int);
			FCatalog.DataTypes.SystemLong.NativeType = typeof(long);
			FCatalog.DataTypes.SystemDecimal.NativeType = typeof(decimal);
			FCatalog.DataTypes.SystemMoney.NativeType = typeof(decimal);
			FCatalog.DataTypes.SystemTimeSpan.NativeType = typeof(TimeSpan);
			FCatalog.DataTypes.SystemDateTime.NativeType = typeof(DateTime);
			FCatalog.DataTypes.SystemDate.NativeType = typeof(DateTime);
			FCatalog.DataTypes.SystemTime.NativeType = typeof(DateTime);
			FCatalog.DataTypes.SystemGuid.NativeType = typeof(Guid);
			FCatalog.DataTypes.SystemString.NativeType = typeof(string);
			FCatalog.DataTypes.SystemName.NativeType = typeof(string);
			#if USEISTRING
			FCatalog.DataTypes.SystemIString.NativeType = typeof(string);
			#endif
			FCatalog.DataTypes.SystemError.NativeType = typeof(Exception);
			FCatalog.DataTypes.SystemBinary.NativeType = typeof(byte[]);
			FCatalog.DataTypes.SystemGraphic.NativeType = typeof(byte[]);
		}
		
		public virtual void ClearCatalog()
		{
			InternalCreateCatalog();
			FCatalogInitialized = false;
			InitializeCatalog();
			FCatalogRegistered = false;
			RegisterCatalog();
			LoadServerState();
		}

		// CacheTimeStamp
		public long CacheTimeStamp { get { return FCatalog.CacheTimeStamp; } }

		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp { get { return FCatalog.PlanCacheTimeStamp; } }

		// DerivationTimeStamp
		public long DerivationTimeStamp { get { return FCatalog.DerivationTimeStamp; } }

		/// <summary> Emits the creation script for the catalog and returns it as a string. </summary>
		public string ScriptCatalog(CatalogDeviceSession ASession)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitStatement(ASession, EmitMode.ForCopy, false));
		}

		public string ScriptLibrary(CatalogDeviceSession ASession, string ALibraryName)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitStatement(ASession, EmitMode.ForCopy, ALibraryName, false));
		}

		public string ScriptDropCatalog(CatalogDeviceSession ASession)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitDropStatement(ASession));
		}

		public string ScriptDropLibrary(CatalogDeviceSession ASession, string ALibraryName)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitDropStatement(ASession, new string[] { }, ALibraryName, false, false, true, true));
		}

		#endregion
	}
}
