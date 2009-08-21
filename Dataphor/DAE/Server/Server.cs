/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
//#define USETHREADABORT
//#define LOGCACHEEVENTS

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.EnterpriseServices;
using System.Threading;
using System.Reflection;
using System.Resources;
using System.Security.Principal;

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
	using Alphora.Dataphor.Logging;
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;

	/// <summary> Dataphor DAE Server class. </summary>
	/// <remarks>
	///		Provides an instance of a Dataphor DAE Server.  This object is usually accessed
	///		through the IServerXXX common interfaces which make up the DAE CLI.  Instances
	///		are usually created and obtained through the <see cref="ServerFactory"/> class.
	/// </remarks>
	public class Server : ServerObject, IDisposable, IServer
	{
		// Do not localize
		public const string CDefaultServerName = "Dataphor";
		public const string CServerLogName = @"Dataphor";											 
		public const string CServerSourceName = @"Dataphor Server";
		public const string CDefaultLibraryDirectory = @"Libraries";
		public const string CDefaultLibraryDataDirectory = @"LibraryData";
		public const string CDefaultInstanceDirectory = @"Instances";
		public const string CDefaultCatalogDirectory = @"Catalog";
		public const string CDefaultCatalogDatabaseName = @"DAECatalog";
		public const string CDefaultBackupDirectory = @"Backup";
		public const string CDefaultSaveDirectory = @"Save";
		public const string CDefaultLogDirectory = @"Log";
		public const string CSystemLibraryName = @"System";
		public const string CGeneralLibraryName = @"General";
		public const string CUserRoleName = "System.User";
		public const string CSystemUserID = "System";
		public const string CAdminUserID = "Admin";
		public const int CCatalogDeviceID = 1;
		public const string CCatalogDeviceName = "System.Catalog";
		#if USEHEAPDEVICE
		public const string CHeapDeviceName = "System.Heap";
		#endif
		public const string CTempDeviceName = "System.Temp";
		public const string CATDeviceName = "System.ApplicationTransactionDevice";
		public const int CTempDeviceMaxRowCount = 1000;
		public const int CATDeviceMaxRowCount = 100;
		public const int CMaxLogs = 9;
		public const int CSystemSessionID = 0;
		public const int CLockManagerID = 1; // lock manager resource manager id
		#if USEHEAPDEVICE
		public const int CHeapDeviceManagerID = 2; // heap device resource manager id
		#endif
		public const int CTempDeviceManagerID = 3; // temp device resource manager id
		public const int CCatalogDeviceManagerID = 4; // catalog device resource manager id
		public const int CStreamManagerID = 5; // stream manager resource manager id
		public const int CCatalogManagerID = 6; // catalog resource manager id
		public const int CRowManagerID = 7; // row manager resource manager id
		public const int CScalarManagerID = 8; // scalar manager resource manager id
		public const int CATDeviceManagerID = 9; // application transaction device resource manager id
		public const int CBaseResourceManagerID = 100; // base resource manager id
		public const int CDefaultMaxConcurrentProcesses = 200;
		public const int CDefaultMaxStackDepth = 32767; // Also specified in the base stack
		public const int CDefaultMaxCallDepth = 1024; // Also specified in the base window stack
		public const int CDefaultProcessWaitTimeout = 30;
		public const int CDefaultProcessTerminationTimeout = 30;
		public const int CDefaultPlanCacheSize = 1000;

        private static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof(Server));
		
		// constructor		
		public Server() : base()
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
		
		// ResourceManagers
		[Reference]
		private Hashtable FResourceManagers;
		private int FNextResourceManagerID;
		public int GetNextResourceManagerID()
		{
			lock (FResourceManagers)
			{
				FNextResourceManagerID++;
				return FNextResourceManagerID - 1;
			}
		}
		
		public void RegisterResourceManager(int AResourceManagerID, object AResourceManager)
		{
			lock (FResourceManagers)
			{
				FResourceManagers.Add(AResourceManagerID, AResourceManager);
			}
		}
		
		public int RegisterResourceManager(object AResourceManager)
		{
			lock (FResourceManagers)
			{
				int FResourceManagerID = GetNextResourceManagerID();
				RegisterResourceManager(FResourceManagerID, AResourceManager);
				return FResourceManagerID;
			}
		}
		
		public void UnregisterResourceManager(int AResourceManagerID)
		{
			lock (FResourceManagers)
			{
				FResourceManagers.Remove(AResourceManagerID);
			}
		}
		
		public void InitializeResourceManagers()
		{
			FResourceManagers = new Hashtable();
			FCatalog = new Schema.Catalog();
			RegisterResourceManager(CCatalogManagerID, FCatalog);
			FPlanCache = new PlanCache(CDefaultPlanCacheSize);
			if (FLockManager == null)
				FLockManager = new LockManager();
			if (FStreamManager == null)
				FStreamManager = new ServerStreamManager(CStreamManagerID, FLockManager, this);
			RegisterResourceManager(CLockManagerID, FLockManager);
			RegisterResourceManager(CStreamManagerID, FStreamManager);
			#if USEROWMANAGER
			FRowManager = new RowManager(CRowManagerID);
			RegisterResourceManager(CRowManagerID, FRowManager);
			#endif
			#if USESCALARMANAGER
			FScalarManager = new ScalarManager(CScalarManagerID);
			RegisterResourceManager(CScalarManagerID, FScalarManager);
			#endif
			FNextResourceManagerID = CBaseResourceManagerID;
		}
		
		// SystemLibrary		
		private Schema.LoadedLibrary FSystemLibrary;
		public Schema.LoadedLibrary SystemLibrary 
		{ 
			get 
			{ 
				if (FSystemLibrary == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FSystemLibrary; 
			} 
		}
		
		// TempDevice		
		private MemoryDevice FTempDevice;
		public MemoryDevice TempDevice 
		{ 
			get 
			{ 
				if (FTempDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FTempDevice; 
			} 
		}

		#if USEHEAPDEVICE
		// HeapDevice
		private HeapDevice FHeapDevice;
		public HeapDevice HeapDevice 
		{ 
			get 
			{ 
				if (FHeapDevice == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FHeapDevice; 
			}
		}
		#endif
		
		// CatalogDevice
		private CatalogDevice FCatalogDevice;
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
		private ApplicationTransactionDevice FATDevice;
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

		// LockManager
		private LockManager FLockManager;
		public LockManager LockManager 
		{ 
			get 
			{ 
				if (FLockManager == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FLockManager; 
			} 
		}
		
		// StreamManager        
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

		#if USEROWMANAGER		
		// RowManager
		private RowManager FRowManager;
		public RowManager RowManager
		{
			get 
			{
				if (FRowManager == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FRowManager;
			}
		}
		#endif
		
		#if USESCALARMANAGER
		// ScalarManager
		private ScalarManager FScalarManager;
		public ScalarManager ScalarManager
		{
			get 
			{
				if (FScalarManager == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FScalarManager;
			}
		}
		#endif
		
		// User Role
		private Schema.Role FUserRole;
		public Schema.Role UserRole
		{
			get
			{
				if (FUserRole == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FUserRole;
			}
		}
		
		// System Session
		internal ServerSession FSystemSession;
		internal ServerProcess FSystemProcess;

		// System User
		private Schema.User FSystemUser;
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
		private Schema.User FAdminUser;
		public Schema.User AdminUser 
		{
			get 
			{
				if (FAdminUser == null)
					throw new ServerException(ServerException.Codes.InvalidServerState, ServerState.Started.ToString());
				return FAdminUser; 
			} 
		}
		
		// PerformanceCounters
		public const string CDataphorServerCategoryName = "Dataphor Server";
		public const string CConnectionCounterName = "Connections";
		public const string CSessionCounterName = "Sessions";
		public const string CProcessCounterName = "Processes";
		public const string CRunningProcessCounterName = "Running Processes";
		public const string CPlanCounterName = "Plans";
		public const string CCursorCounterName = "Cursors";
		public const string CStreamsAllocatedCounterName = "Streams Allocated";
		public const string CStreamAllocationsPerSecondCounterName = "Stream Allocations/Sec";
		
		#if !DISABLE_PERFORMANCE_COUNTERS
		internal PerformanceCounter FConnectionCounter;
		internal PerformanceCounter FSessionCounter;
		internal PerformanceCounter FProcessCounter;
		internal PerformanceCounter FRunningProcessCounter;
		internal PerformanceCounter FPlanCounter;
		internal PerformanceCounter FCursorCounter;
		#endif
				
		private void RegisterPerformanceCounters()
		{
			#if !DISABLE_PERFORMANCE_COUNTERS
			if (!IsRepository)
			{
				try
				{
					// Adding and deleting counters is difficult.
					// I think you may have to delete the category and readd it, but that won't work if your a different user (like ASPNET)
					if (!PerformanceCounterCategory.Exists("Dataphor Server"))
					{
						CounterCreationDataCollection LPerfCounters = new CounterCreationDataCollection();
						LPerfCounters.Add(new CounterCreationData(CConnectionCounterName, "The number of open connections.  Execute 'select Connections;' for a detailed listing.", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData(CSessionCounterName, "The number of open sessions.  Execute 'select Sessions;' for a detailed listing.", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData(CProcessCounterName, "The number of active processes.  Execute 'select Processes;' for a detailed listing.", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData(CRunningProcessCounterName, "The number of running processes. Execute 'select Processes;' for a detailed listing.", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData(CPlanCounterName, "The number of open plans.  Execute 'select Plans;' for a detailed listing.  A plan is a compiled statement/expression/query.", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData(CCursorCounterName, "The number of open cursors.", PerformanceCounterType.NumberOfItems32));

						LPerfCounters.Add(new CounterCreationData(CStreamsAllocatedCounterName, "The number of streams allocated inside of the DAE.  The DAE uses streams internaly for handling all database values.", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData(CStreamAllocationsPerSecondCounterName, "The number of streams allocated/per second inside of the DAE.  The DAE uses streams internaly for handling all database values.", PerformanceCounterType.RateOfCountsPerSecond32));
						#if USEDEVICECOUNTERS
						LPerfCounters.Add(new CounterCreationData("DAE Stream Memory Allocated", "The number of bytes allocated to all streams allocated in the DAE.  The DAE uses streams internaly for handling all database values.", PerformanceCounterType.NumberOfItems64));

						LPerfCounters.Add(new CounterCreationData("Device Cursors", "", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData("Device Sessions", "", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData("Device Plans", "", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData("Device SQL Connections", "", PerformanceCounterType.NumberOfItems32));
						LPerfCounters.Add(new CounterCreationData("Device SQL Connections Active", "", PerformanceCounterType.NumberOfItems32));
						#endif

                        PerformanceCounterCategory.Create(CDataphorServerCategoryName, "Alphora Dataphor Server Performance Counters", PerformanceCounterCategoryType.Unknown, LPerfCounters);
					}
					
					FConnectionCounter = new PerformanceCounter(CDataphorServerCategoryName, CConnectionCounterName, FName, false);
					FConnectionCounter.RawValue = 0;
					
					FSessionCounter = new PerformanceCounter(CDataphorServerCategoryName, CSessionCounterName, FName, false);
					FSessionCounter.RawValue = 0;

					FProcessCounter = new PerformanceCounter(CDataphorServerCategoryName, CProcessCounterName, FName, false);
					FProcessCounter.RawValue = 0;

					FRunningProcessCounter = new PerformanceCounter(CDataphorServerCategoryName, CRunningProcessCounterName, FName, false);
					FRunningProcessCounter.RawValue = 0;

					FPlanCounter = new PerformanceCounter(CDataphorServerCategoryName, CPlanCounterName, FName, false);
					FPlanCounter.RawValue = 0;

					FCursorCounter = new PerformanceCounter(CDataphorServerCategoryName, CCursorCounterName, FName, false);
					FCursorCounter.RawValue = 0;
				}
				catch
				{
					// ignore the exception
				}
			}
			#endif
		}
		
		// Execution
		private object FSyncHandle = new System.Object();
		private void BeginCall()
		{
			Monitor.Enter(FSyncHandle);
		}
		
		private void EndCall()
		{
			Monitor.Exit(FSyncHandle);
		}
		
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
		private ArrayList FWaitingProcesses = new ArrayList();
		
		internal void BeginProcessCall(ServerProcess AProcess)
		{
			lock (FWaitingProcesses)
			{
				if (FRunningProcesses < FMaxConcurrentProcesses)
				{
					FRunningProcesses++;
					return;
				}
				
				FWaitingProcesses.Add(Thread.CurrentThread);
			}

			try
			{
				System.Threading.Thread.Sleep(FProcessWaitTimeout);
			}
			catch (ThreadInterruptedException)
			{
				lock (FWaitingProcesses)
				{
					if (FRunningProcesses < FMaxConcurrentProcesses)
					{
						FRunningProcesses++;
						return;
					}
				}
			}
			
			throw new ServerException(ServerException.Codes.ProcessWaitTimeout);
		}
		
		internal void EndProcessCall(ServerProcess AProcess)
		{
			lock (FWaitingProcesses)
			{
				FRunningProcesses--;
				while (FWaitingProcesses.Count > 0)
				{
					System.Threading.Thread LThread = (System.Threading.Thread)FWaitingProcesses[0];
					FWaitingProcesses.RemoveAt(0);
					if ((LThread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0)
					{
						LThread.Interrupt();
						break;
					}
				}
			}
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
		
		public ServerSession GetSession(int ASessionID)
		{
			return FSessions.GetSession(ASessionID);
		}

		public void CloseSession(int ASessionID)
		{
			GetSession(ASessionID).Dispose();
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
		
		public Exception WrapException(Exception AException)
		{
			SRFLogger.WriteLine(TraceLevel.Verbose,"Wrapped Exception {0}",AException);
			if (FLogErrors)
				LogError(AException);
				
			return AException;
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
		
		internal void LibraryUnloaded(string ALibraryName)
		{
			foreach (ServerSession LSession in Sessions)
				if (LSession.CurrentLibrary.Name == ALibraryName)
					LSession.CurrentLibrary = Catalog.LoadedLibraries[Server.CGeneralLibraryName];
		}
		
		/// <summary>Event that is fired whenever a library begins loading.</summary>		
		public event LibraryNotifyEvent OnLibraryLoading;
		
		internal void DoLibraryLoading(string ALibraryName)
		{
			if (OnLibraryLoading != null)
				OnLibraryLoading(this, ALibraryName);
		}

		/// <summary>Event that is fired whenever a library is done being loaded.</summary>
		public event LibraryNotifyEvent OnLibraryLoaded;
		
		internal void DoLibraryLoaded(string ALibraryName)
		{
			if (OnLibraryLoaded != null)
				OnLibraryLoaded(this, ALibraryName);
		}
		
		// Start

		public void Start()
		{
			BeginCall();
			try
			{
				#if !SKIPBETACHECK
				//CheckBetaTimeBomb();
				#endif
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
			catch (Exception E)
			{
			    Exception LWrappedException = WrapException(E);
                throw LWrappedException;
			}
			finally
			{
				EndCall();
			}
		}
        
		private void InternalStarted()
		{
			StartDevices();
			RunStartupScript();
			#if AUTOUPGRADELIBRARIES
			SystemUpgradeLibraryNode.UpgradeLibraries(FSystemProcess);
			#endif
		}
		
		private void CheckBetaTimeBomb()
		{
			#if CHECKBETATIMEBOMB
			if (DateTime.Today > new DateTime(2004, 6, 28).AddDays(120))
				throw new ServerException(ServerException.Codes.BetaExpired);
			#endif
		}
		
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
		private void InternalStart()
		{
			RegisterPerformanceCounters();
			InitializeResourceManagers();
			InitializeAvailableLibraries();
			InitializeCatalog();
			RegisterCatalog();

			if (!FFirstRun)
				LoadServerState(); // Load server state from the persistent store

			EnsureGeneralLibraryLoaded();
		}

		// Stop
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
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
		
		private string GetLogFileName()
		{
			string LLogFileName = GetLogFileName(CMaxLogs);
			if (File.Exists(LLogFileName))
				File.Delete(LLogFileName);
			for (int LIndex = CMaxLogs - 1; LIndex >= 0; LIndex--)
			{
				LLogFileName = GetLogFileName(LIndex);
				if (File.Exists(LLogFileName))
					File.Move(LLogFileName, GetLogFileName(LIndex + 1));
			}
			return GetLogFileName(0);
		}
		
		private string GetLogName(int ALogIndex)
		{
			return String.Format("{0}{1}", CServerLogName, ALogIndex == 0 ? " (current)" : ALogIndex.ToString());
		}
		
		private string GetLogDirectory()
		{
			string LResult = Path.Combine(InstanceDirectory, CDefaultLogDirectory);
			Directory.CreateDirectory(LResult);
			return LResult;
		}
		
		private string GetLogFileName(int ALogIndex)
		{
			return Path.Combine(GetLogDirectory(), String.Format("{0}{1}.log", CServerLogName, ALogIndex == 0 ? String.Empty : ALogIndex.ToString()));
		}
		
		public StringCollection ListLogs()
		{
			StringCollection LLogList = new StringCollection();
			for (int LIndex = 0; LIndex <= CMaxLogs; LIndex++)
			{
				string LLogName = GetLogName(LIndex);
				string LLogFileName = GetLogFileName(LIndex);
				if (File.Exists(LLogFileName))
					LLogList.Add(LLogName);
			}
			
			return LLogList;
		}

        private bool IsAdministrator()
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                WindowsIdentity LWID = WindowsIdentity.GetCurrent();
                WindowsPrincipal LWP = new WindowsPrincipal(LWID);
                return LWP.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
                return (false); // Might not be Windows
        }

        private FileStream FLogFile;
		private StreamWriter FLog;

		private void StartLog()
		{
			if (FLoggingEnabled)
			{
                if (!IsRepository && IsAdministrator())
                {
                    if (!EventLog.SourceExists(CServerSourceName))
                        EventLog.CreateEventSource(CServerSourceName, CServerLogName);
                }
				try
				{
					OpenLogFile(GetLogFileName());
				}
				catch
				{
					StopLog();
					if (!IsRepository)
						throw; // Eat the error if this is a repository
				}
				
				LogEvent(DAE.Server.LogEvent.LogStarted);
			}
		}
		
		private void StopLog()
		{
			if (FLoggingEnabled && (FLog != null))
			{
				LogEvent(DAE.Server.LogEvent.LogStopped);
				try
				{
					CloseLogFile();
				}
				finally
				{
					FLog = null;
					FLogFile = null;
				}
			}
		}
		
		private void OpenLogFile(string ALogFileName)
		{
			FLogFile = new FileStream(ALogFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			FLogFile.Position = FLogFile.Length;
			FLog = new StreamWriter(FLogFile);
			FLog.AutoFlush = true;
		}
		
		private void CloseLogFile()
		{
			FLog.Flush();
			FLog.Close();
			FLogFile.Close();
		}
		
		public string ShowLog()
		{
			if (FLoggingEnabled && (FLog != null))
			{
				string LLogFileName = FLogFile.Name;
				CloseLogFile();
				try
				{
					using (StreamReader LReader = new StreamReader(LLogFileName))
					{
						return LReader.ReadToEnd();
					}
				}
				finally
				{
					OpenLogFile(LLogFileName);
				}
			}

			return String.Empty;
		}
		
		public string ShowLog(int ALogIndex)
		{
			if (ALogIndex == 0)
				return ShowLog();
			else
			{
				using (StreamReader LReader = new StreamReader(GetLogFileName(ALogIndex)))
				{
					return LReader.ReadToEnd();
				}
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
		
		// State
		private ServerState FState;
		public ServerState State { get { return FState; } }
		
		private void SetState(ServerState ANewState)
		{
			FState = ANewState;
		}
		
		private void CheckState(ServerState AState)
		{
			if (FState != AState)
				throw new ServerException(ServerException.Codes.InvalidServerState, AState.ToString());
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
								
							if (AProcess.Debugger != null)
								LDebugger = AProcess.Debugger;
						}
						finally
						{
							Monitor.Exit(AProcess.ExecutionSyncHandle);
						}
						
						if ((LDebugger != null) && LDebugger.IsPaused)
							LDebugger.Detach(AProcess);

						System.Threading.Thread.SpinWait(100);
						if (DateTime.Now > LEndTime)
							#if USETHREADABORT
							break;
							#else
							throw new ServerException(ServerException.Codes.ProcessNotResponding);
							#endif
					}
					
					#if USETHREADABORT
					Monitor.Enter(AProcess.ExecutionSyncHandle);
					LHandleReleased = false;
					
					if ((AProcess.ExecutingThread != null) && AProcess.ExecutingThread.IsAlive)
					{
						#if LOGCACHEEVENTS
						LogMessage(String.Format("Aborting thread {0}.", AProcess.ExecutingThread.GetHashCode()));
						#endif
						AProcess.ExecutingThread.Abort();
					}
					
					Monitor.Exit(AProcess.ExecutionSyncHandle);
					LHandleReleased = true;
					LEndTime = DateTime.Now.AddTicks(ProcessTerminationTimeout.Ticks / 2);
					while (true)
					{
						Monitor.Enter(AProcess.ExecutionSyncHandle);
						try
						{
							if ((AProcess.ExecutingThread == null) || !AProcess.ExecutingThread.IsAlive)
								break;
						}
						finally
						{
							Monitor.Exit(AProcess.ExecutionSyncHandle);
						}

						System.Threading.Thread.SpinWait(100);
						if (DateTime.Now > LEndTime)
							throw new ServerException(ServerException.Codes.ProcessNotResponding);
					}
					#endif
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
		
		// Sessions
		private ServerSessions FSessions;
		internal ServerSessions Sessions { get { return FSessions; } }
		
		private int FNextSessionID = 1;
		private int GetNextSessionID()
		{
			return Interlocked.Increment(ref FNextSessionID);
		}
		
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
				if ((ASessionInfo.HostName == null) || (ASessionInfo.HostName == ""))
					ASessionInfo.HostName = System.Environment.MachineName;
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
		
		private Schema.User ValidateLogin(int ASessionID, SessionInfo ASessionInfo)
		{
			if (ASessionInfo == null)
				throw new ServerException(ServerException.Codes.SessionInformationRequired);
				
			if (String.Compare(ASessionInfo.UserID, CSystemUserID, true) == 0)
			{
				if (!IsRepository && (ASessionID != CSystemSessionID))
					throw new ServerException(ServerException.Codes.CannotLoginAsSystemUser);

				return FSystemUser;
			}
			else
			{
				Schema.User LUser = FSystemProcess.CatalogDeviceSession.ResolveUser(ASessionInfo.UserID);
				if (String.Compare(Schema.SecurityUtility.DecryptPassword(LUser.Password), ASessionInfo.Password, true) != 0)
					throw new ServerException(ServerException.Codes.InvalidPassword);
					
				return LUser;
			}
		}
		
		internal IServerSession ConnectAs(SessionInfo ASessionInfo)
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
		
		private string FInstanceDirectory;
		/// <summary>
		/// The primary data directory for the instance. All write activity for the server should occur in this directory (logging, catalog, device data, etc.,.)
		/// </summary>
		public string InstanceDirectory
		{
			get 
			{ 
				if (State != ServerState.Stopped)
				{
					if (String.IsNullOrEmpty(FInstanceDirectory))
						FInstanceDirectory = Name;
						
					if (!Path.IsPathRooted(FInstanceDirectory))
						FInstanceDirectory = Path.Combine(Path.Combine(PathUtility.CommonAppDataPath(string.Empty, VersionModifier.None), CDefaultInstanceDirectory), FInstanceDirectory);

					if (!Directory.Exists(FInstanceDirectory))
						Directory.CreateDirectory(FInstanceDirectory);
				}

				return FInstanceDirectory; 
			}
			set
			{
				CheckState(ServerState.Stopped);
				FInstanceDirectory = value;
			}
		}
		
		private bool FTracingEnabled = true;
		/// <summary> Determines whether trace logging for server events is enabled. </summary>
		/// <remarks> The default is true. </remarks>
        /// <seealso cref="SessionInfo.SessionTracingEnabled"/>
		public bool TracingEnabled
		{
			get { return FTracingEnabled; }
			set { FTracingEnabled = value; }
		}
		
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
		
		public EventLogEntryType LogEntryTypeToEventLogEntryType(LogEntryType AEntryType)
		{
			switch (AEntryType)
			{
				case LogEntryType.Error : return EventLogEntryType.Error;
				case LogEntryType.Warning : return EventLogEntryType.Warning;
				default : return EventLogEntryType.Information;
			}
		}
		
		public void LogMessage(LogEntryType AEntryType, string ADescription)
		{
            SRFLogger.WriteLine(TraceLevel.Verbose, "Logging {0} Message: {0}", AEntryType, ADescription);
            
            if (FLoggingEnabled)
			{
				if (!IsRepository && IsAdministrator() && (System.Environment.OSVersion.Platform == PlatformID.Win32NT))
					try
					{
						EventLog.WriteEntry(CServerSourceName, String.Format("Server: {0}\r\n{1}", Name, ADescription), LogEntryTypeToEventLogEntryType(AEntryType));
					}
					catch
					{
						// ignore an error writing to the event log (it's probably complaining that it's full)
					}
		
				if (FLog != null)				
				{
					lock (FLog)
					{
						FLog.Write
						(
							String.Format
							(
								"{0} {1}{2}\r\n", 
								DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"), 
								(AEntryType == LogEntryType.Information) ? 
									String.Empty : 
									String.Format("{0}: ", AEntryType.ToString()),
								ADescription
							)
						);
					}
				}
			}
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

		// Tracing
		#if TRACEEVENTS
		private object FTracingSyncHandle = new System.Object();
		private void BeginTracingCall()
		{
			Monitor.Enter(FTracingSyncHandle);
		}
		
		private void EndTracingCall()
		{
			Monitor.Exit(FTracingSyncHandle);
		}

		private int FTraceID;
		private Schema.BaseTableVar FTraceTableVar;
		private Hashtable FTraceCodes = new Hashtable(); // hashtable key is TraceCode (string), value is boolean indicating whether the trace is on or off, off by default
        
		private bool InternalTracing(string ATraceCode)
		{
			object LValue = FTraceCodes[ATraceCode];
			return (LValue is bool) && (bool)LValue;
		}
        
		public bool Tracing(string ATraceCode)
		{
			BeginTracingCall();
			try
			{
				return InternalTracing(ATraceCode);
			}
			finally
			{
				EndTracingCall();
			}
		}
        
		public void TraceOn(string ATraceCode)
		{
			BeginTracingCall();
			try
			{
				FTraceCodes[ATraceCode] = true;
				switch (ATraceCode)
				{
					case TraceCodes.StreamTracing : FStreamManager.StreamTracingEnabled = true; break;
					case TraceCodes.LockTracing : FLockManager.LockTracingEnabled = true; break;
				}
			}
			finally
			{
				EndTracingCall();
			}
		}
        
		public void TraceOff(string ATraceCode)
		{
			BeginTracingCall();
			try
			{
				FTraceCodes[ATraceCode] = false;
				switch (ATraceCode)
				{
					case TraceCodes.StreamTracing : FStreamManager.StreamTracingEnabled = false; break;
					case TraceCodes.LockTracing : FLockManager.LockTracingEnabled = false; break;
				}
			}
			finally
			{
				EndTracingCall();
			}
		}
        
		private void EnsureTraceTable(ServerProcess AProcess)
		{
			if ((FTraceTableVar == null) || (FTraceTableVar.Device != FTempDevice))
			{
				FTraceID = 0;
				FTraceTableVar = new Schema.BaseTableVar("System.TraceEvents", new Schema.TableType(), FTempDevice);
				FTraceTableVar.Owner = FAdminUser;
				FTraceTableVar.Library = FSystemLibrary;
				Schema.TableVarColumn LColumn;
				LColumn = new Schema.TableVarColumn(new Schema.Column("ID", AProcess.DataTypes.SystemInteger));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("TraceCode", AProcess.DataTypes.SystemString));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("DateTime", AProcess.DataTypes.SystemDateTime));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("User_ID", AProcess.DataTypes.SystemString));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("Session_ID", AProcess.DataTypes.SystemInteger));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("Process_ID", AProcess.DataTypes.SystemInteger));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("Description", AProcess.DataTypes.SystemString));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				FTraceTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{FTraceTableVar.Columns["ID"]}));
				FTraceTableVar.IsGenerated = true;
				Compiler.BindNode(AProcess.Plan, new CreateTableNode(FTraceTableVar)).Execute(AProcess);
			}
		}

		public void RaiseTraceEvent(ServerProcess AProcess, string ATraceCode, string ADescription)
		{
			BeginTracingCall();
			try
			{
				if (InternalTracing(ATraceCode))
				{
					EnsureTraceTable(AProcess);
					Schema.DeviceSession LDeviceSession = AProcess.DeviceConnect(FTraceTableVar.Device);
					Row LRow = new Row(AProcess, FTraceTableVar.DataType.RowType);
					try
					{
						FTraceID++;
						LRow[0].AsInt32 = FTraceID;
						LRow[1].AsString = ATraceCode;
						LRow[2].AsDateTime = DateTime.Now;
						LRow[3].AsString =AProcess.ServerSession.User.ID;
						LRow[4].AsInt32 = AProcess.ServerSession.SessionID;
						LRow[5].AsInt32 = AProcess.ProcessID;
						LRow[6].AsString = ADescription;
						AProcess.NonLogged = true;
						try
						{
							LDeviceSession.InsertRow(FTraceTableVar, LRow);
						}
						finally
						{
							AProcess.NonLogged = false;
						}
					}
					finally
					{
						LRow.Dispose();
					}
				}
			}
			finally
			{
				EndTracingCall();
			}
		}
        #endif
        
		// Catalog
		private Schema.Catalog FCatalog;
		public Schema.Catalog Catalog { get { return FCatalog; } }
		
		// InstanceID
		private Guid FInstanceID = Guid.NewGuid();
		public Guid InstanceID { get { return FInstanceID; } }
		
		// CacheTimeStamp
		public long CacheTimeStamp { get { return FCatalog.CacheTimeStamp; } }
		
		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp { get { return FCatalog.PlanCacheTimeStamp; } }
		
		// DerivationTimeStamp
		public long DerivationTimeStamp { get { return FCatalog.DerivationTimeStamp; } }
		
		private string FCatalogStoreClassName;
		/// <summary>
		/// Gets or sets the assembly qualified class name of the store used to persist the system catalog.
		/// </summary>
		/// <remarks>
		/// This property cannot be changed once the server has been started. If this property
		/// is not set, the default store class (SQLCEStore in the DAE.SQLCE assembly) will be used.
		/// </remarks>
		public string CatalogStoreClassName
		{
			get { return FCatalogStoreClassName; }
			set
			{
				CheckState(ServerState.Stopped);
				FCatalogStoreClassName = value;
			}
		}
		
		/// <summary>
		/// Gets the class name of the store used to persist the system catalog.
		/// </summary>
		/// <returns>The value of the CatalogStoreClassName property if it is set, otherwise, the assembly qualified class name of the SQLCEStore.</returns>
		public string GetCatalogStoreClassName()
		{
			return 
				String.IsNullOrEmpty(FCatalogStoreClassName) 
					? "Alphora.Dataphor.DAE.Store.SQLCE.SQLCEStore,Alphora.Dataphor.DAE.SQLCE" 
					: FCatalogStoreClassName;
		}
		
		private string FCatalogStoreConnectionString;
		/// <summary>
		/// Gets or sets the connection string for the store used to persist the system catalog.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property cannot be changed once the server has been started. If this property is not
		/// set, a default SQLCE connection string will be built that specifies the catalog will be
		/// stored in the Catalog subfolder of the instance directory, and named DAECatalog.sdf.
		/// </para>
		/// <para>
		/// If the CatalogStoreConnectionString is specified, the token %CatalogPath% will be replaced
		/// by the catalog directory of the instance.
		/// </para>
		/// </remarks>
		public string CatalogStoreConnectionString
		{
			get { return FCatalogStoreConnectionString; }
			set
			{
				CheckState(ServerState.Stopped);
				FCatalogStoreConnectionString = value;
			}
		}
		
		/// <summary>
		/// Gets the connection string for the store used to persist the system catalog.
		/// </summary>
		/// <returns>The value of the CatalogStoreCnnectionString property if it set, otherwise, a default SQL CE connection string.</returns>
		public string GetCatalogStoreConnectionString()
		{
			return 
				String.IsNullOrEmpty(FCatalogStoreConnectionString)
					? String.Format("Data Source={0};Password={1};Mode={2}", GetCatalogStoreDatabaseFileName(), String.Empty, "Read Write")
					: FCatalogStoreConnectionString.Replace("%CatalogPath%", GetCatalogDirectory());
		}
		
		/// <summary>
		/// Returns the catalog directory for this instance. This is always a directory named Catalog within the instance directory.
		/// </summary>
		public string GetCatalogDirectory()
		{
			string LDirectory = Path.Combine(InstanceDirectory, CDefaultCatalogDirectory);
			if (!Directory.Exists(LDirectory))
				Directory.CreateDirectory(LDirectory);
			return LDirectory;
		}
		
		public string GetCatalogStoreDatabaseFileName()
		{
			return Path.Combine(GetCatalogDirectory(), Path.ChangeExtension(CDefaultCatalogDatabaseName, ".sdf"));
		}

		private string FLibraryDirectory = String.Empty;
		/// <summary> The directory the DAE uses to find available libraries. </summary>
		public string LibraryDirectory
		{
			get { return FLibraryDirectory; }
			set
			{
				if (FState != ServerState.Starting && FState != ServerState.Stopped)
					throw new ServerException(ServerException.Codes.InvalidServerState, "Starting or Stopped");
				if ((value == null) || (value == String.Empty))
					FLibraryDirectory = GetDefaultLibraryDirectory();
				else
					FLibraryDirectory = value;
					
				string[] LDirectories = FLibraryDirectory.Split(';');
				
				StringBuilder LLibraryDirectory = new StringBuilder();
				for (int LIndex = 0; LIndex < LDirectories.Length; LIndex++)
				{
					if (!Path.IsPathRooted(LDirectories[LIndex]))
						LDirectories[LIndex] = Path.Combine(PathUtility.GetBinDirectory(), LDirectories[LIndex]);
						
					if (!Directory.Exists(LDirectories[LIndex]) && !IsRepository)
						Directory.CreateDirectory(LDirectories[LIndex]);

					if (LIndex > 0)
						LLibraryDirectory.Append(";");
					
					LLibraryDirectory.Append(LDirectories[LIndex]);
				}
				
				FLibraryDirectory = LLibraryDirectory.ToString();
			}
		}																							  
		
		public static string GetDefaultLibraryDirectory()
		{
			return Path.Combine(PathUtility.GetBinDirectory(), CDefaultLibraryDirectory);
		}
		
		private string SaveServerSettings()
		{
			StringBuilder LUpdateStatement = new StringBuilder();
			
			if (MaxConcurrentProcesses != CDefaultMaxConcurrentProcesses)
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("MaxConcurrentProcesses := {0}", MaxConcurrentProcesses.ToString());
			}
			
			if (ProcessWaitTimeout != TimeSpan.FromSeconds(CDefaultProcessWaitTimeout))
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("ProcessWaitTimeout := TimeSpan.Ticks({0})", ProcessWaitTimeout.Ticks.ToString());
			}
			
			if (ProcessTerminationTimeout != TimeSpan.FromSeconds(CDefaultProcessTerminationTimeout))
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("ProcessTerminateTimeout := TimeSpan.Ticks({0})", ProcessTerminationTimeout.Ticks.ToString());
			}
			
			if (PlanCacheSize != CDefaultPlanCacheSize)
			{
				if (LUpdateStatement.Length > 0)
					LUpdateStatement.Append(", ");
				LUpdateStatement.AppendFormat("PlanCacheSize := {0}", PlanCacheSize.ToString());
			}
			
			if (LUpdateStatement.Length > 0)
			{
				LUpdateStatement.Insert(0, "update ServerSettings set { ");
				LUpdateStatement.Append(" };\r\n");
			}
			
			return LUpdateStatement.ToString();
		}
		
		private Statement SaveSystemDeviceSettings(Schema.Device ADevice)
		{
			AlterDeviceStatement LStatement = new AlterDeviceStatement();
			LStatement.DeviceName = ADevice.Name;
			LStatement.AlterClassDefinition = new AlterClassDefinition();
			LStatement.AlterClassDefinition.AlterAttributes.AddRange(ADevice.ClassDefinition.Attributes);
			return LStatement;
		}
		
		private string SaveSystemDeviceSettings()
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			Block LBlock = new Block();
			if (FTempDevice.ClassDefinition.Attributes.Count > 0)
				LBlock.Statements.Add(SaveSystemDeviceSettings(FTempDevice));
			if (FATDevice.ClassDefinition.Attributes.Count > 0)
				LBlock.Statements.Add(SaveSystemDeviceSettings(FATDevice));
			if (FCatalogDevice.ClassDefinition.Attributes.Count > 0)
				LBlock.Statements.Add(SaveSystemDeviceSettings(FCatalogDevice));
			return new D4TextEmitter().Emit(LBlock) + "\r\n";
		}
		
		private string SaveDeviceSettings(ServerProcess AProcess)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			Block LBlock = new Block();

			IServerProcess LProcess = (IServerProcess)AProcess;
			IServerCursor LCursor = LProcess.OpenCursor("select Devices { ID }", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						Schema.Device LDevice = AProcess.CatalogDeviceSession.ResolveCatalogObject((int)LRow[0/*"ID"*/]) as Schema.Device;
						if ((LDevice != null) && (LDevice.ClassDefinition.Attributes.Count > 0))
							LBlock.Statements.Add(SaveSystemDeviceSettings(LDevice));
					}
				}
			} 
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			return new D4TextEmitter().Emit(LBlock) + "\r\n";
		}
		
		private string SaveSecurity(ServerProcess AProcess)
		{
			StringBuilder LResult = new StringBuilder();
			IServerProcess LProcess = (IServerProcess)AProcess;
			IServerCursor LCursor;

			// Users
			LResult.Append("// Users\r\n");
			
			LCursor = LProcess.OpenCursor("select Users { ID }", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						switch ((string)LRow[0/*"ID"*/])
						{
							case Server.CSystemUserID : break;
							case Server.CAdminUserID : 
								if (FAdminUser.Password != String.Empty)
									LResult.AppendFormat("SetEncryptedPassword('{0}', '{1}');\r\n", FAdminUser.ID, FAdminUser.Password);
							break;
							
							default :
								Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser((string)LRow[0/*"ID"*/]);
								LResult.AppendFormat("CreateUserWithEncryptedPassword('{0}', '{1}', '{2}');\r\n", LUser.ID, LUser.Name, LUser.Password);
							break;
						}
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}
			
			LResult.Append("\r\n");
			LResult.Append("// Device Users\r\n");

			// DeviceUsers
			LCursor = LProcess.OpenCursor("select DeviceUsers join (Devices { ID Device_ID, Name Device_Name }) { User_ID, Device_ID }", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser((string)LRow[0/*"User_ID"*/]);
						Schema.Device LDevice = (Schema.Device)AProcess.CatalogDeviceSession.ResolveCatalogObject((int)LRow[1/*"Device_ID"*/]);
						Schema.DeviceUser LDeviceUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(LDevice, LUser);
						LResult.AppendFormat("CreateDeviceUserWithEncryptedPassword('{0}', '{1}', '{2}', '{3}', '{4}');\r\n", LDeviceUser.User.ID, LDeviceUser.Device.Name, LDeviceUser.DeviceUserID, LDeviceUser.DevicePassword, LDeviceUser.ConnectionParameters);
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			LResult.Append("// User Roles\r\n");

			// UserRoles
			LCursor = LProcess.OpenCursor("select UserRoles where Role_Name <> 'System.User'", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						
						LResult.AppendFormat("AddUserToRole('{0}', '{1}');\r\n", (string)LRow[0/*"User_ID"*/], (string)LRow[1/*"Role_Name"*/]);
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			LResult.Append("// User Right Assignments\r\n");

			// UserRightAssignments
			LCursor = LProcess.OpenCursor("select UserRightAssignments", null);
			try
			{
				using (Row LRow = LCursor.Plan.RequestRow())
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						
						if ((bool)LRow[2/*"IsGranted"*/])
							LResult.AppendFormat("GrantRightToUser('{0}', '{1}');\r\n", (string)LRow[1/*"Right_Name"*/], (string)LRow[0/*"User_ID"*/]);
						else
							LResult.AppendFormat("RevokeRightFromUser('{0}', '{1}');\r\n", (string)LRow[1/*"Right_Name"*/], (string)LRow[0/*"User_ID"*/]);						
					}
				}
			}
			finally
			{
				LProcess.CloseCursor(LCursor);
			}

			LResult.Append("\r\n");
			return LResult.ToString();
		}
		
		/// <summary>Returns a script to recreate the server state.</summary>
		public string ScriptServerState(ServerProcess AProcess)
		{
			StringBuilder LBuilder = new StringBuilder();
			LBuilder.Append("// Server Settings\r\n");
			LBuilder.Append(SaveServerSettings());
			
			LBuilder.Append("\r\n");
			LBuilder.Append("// Device Settings\r\n");
			LBuilder.Append(SaveDeviceSettings(AProcess));
			LBuilder.Append("\r\n");

			LBuilder.Append(SaveSecurity(AProcess));
			return LBuilder.ToString();
		}
		
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
			return new D4TextEmitter().Emit(FCatalog.EmitDropStatement(ASession, new string[] {}, ALibraryName, false, false, true, true));
		}
		
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

		public void LoadServerState()
		{
			if (!IsRepository)
			{
				// Load server configuration settings
				FSystemProcess.CatalogDeviceSession.LoadServerSettings(this);
				
				// Attaches any libraries that were attached from an explicit library directory
				FSystemProcess.CatalogDeviceSession.AttachLibraries();
				
				// Set the object ID generator				
				Schema.Object.SetNextObjectID(FSystemProcess.CatalogDeviceSession.GetMaxObjectID() + 1);
				
				// Insert a row into TableDee...
				RunScript(FSystemProcess, "insert table { row { } } into TableDee;");
				
				// Ensure all loaded libraries are loaded
				FSystemProcess.CatalogDeviceSession.ResolveLoadedLibraries();
			}
		}
		
		public void ClearCatalog()
		{
			InitializeResourceManagers();
			FCatalogInitialized = false;
			InitializeCatalog();
			FCatalogRegistered = false;
			RegisterCatalog();
			LoadServerState();
			EnsureGeneralLibraryLoaded();
		}

		private string FStartupScriptUri = String.Empty;
		/// <summary> A URI reference to a startup script. </summary>
		/// <remarks> Cannot be set once the server is running. </remarks>
		public string StartupScriptUri
		{
			get { return FStartupScriptUri; }
			set
			{
				CheckState(ServerState.Stopped);
				FStartupScriptUri = value == null ? String.Empty : value;
			}
		}
		
		private void RunStartupScript() 
		{
			if (FStartupScriptUri != String.Empty)
			{
				IServerSession LSession = ((IServer)this).Connect(new SessionInfo(FAdminUser.ID, FAdminUser.Password));
				try
				{
					IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo));
					try
					{
						IServerScript LScript = LProcess.PrepareScript(WebUtility.ReadStringFromWeb(FStartupScriptUri));
						try
						{
							LScript.Execute(null);
						}
						finally
						{
							LProcess.UnprepareScript(LScript);
						}
					}
					finally
					{
						LSession.StopProcess(LProcess);
					}
				}
				finally
				{
					((IServer)this).Disconnect(LSession);
				}
			}
		}

		public bool MaintainedLibraryUpdate 
		{ 
			get 
			{ 
				#if USEWATCHERS
				return !FLibraryWatcher.EnableRaisingEvents; 
				#else
				return false;
				#endif
			} 
			set 
			{ 
				#if USEWATCHERS
				FLibraryWatcher.EnableRaisingEvents = !value;
				#endif
			} 
		}		
		
		#if USEWATCHERS
		private FileSystemWatcher FLibraryWatcher;
		
		private void LibraryDirectoryChanged(object ASender, FileSystemEventArgs AArgs)
		{
			lock (FSystemProcess)
			{
				SystemRefreshLibrariesNode.RefreshLibraries(FSystemProcess);
			}
		}
		
		private void LibraryDirectoryRenamed(object ASender, RenamedEventArgs AArgs)
		{
			lock (FSystemProcess)
			{
				SystemRefreshLibrariesNode.RefreshLibraries(FSystemProcess);
			}
		}
		#endif

		private void InitializeAvailableLibraries()
		{
			if (FLibraryDirectory == String.Empty)
				LibraryDirectory = GetDefaultLibraryDirectory();
			#if USEWATCHERS
			FLibraryWatcher = new FileSystemWatcher(FLibraryDirectory);
			FLibraryWatcher.IncludeSubdirectories = false;
			FLibraryWatcher.Changed += new FileSystemEventHandler(LibraryDirectoryChanged);
			FLibraryWatcher.Created += new FileSystemEventHandler(LibraryDirectoryChanged);
			FLibraryWatcher.Deleted += new FileSystemEventHandler(LibraryDirectoryChanged);
			FLibraryWatcher.Renamed += new RenamedEventHandler(LibraryDirectoryRenamed);
			#endif
		}
		
		private void UninitializeAvailableLibraries()
		{
			#if USEWATCHERS
			FLibraryWatcher.Changed -= new FileSystemEventHandler(LibraryDirectoryChanged);
			FLibraryWatcher.Created -= new FileSystemEventHandler(LibraryDirectoryChanged);
			FLibraryWatcher.Deleted -= new FileSystemEventHandler(LibraryDirectoryChanged);
			FLibraryWatcher.Renamed -= new RenamedEventHandler(LibraryDirectoryRenamed);
			FLibraryWatcher.Dispose();
			#endif
		}
		
		public void LoadAvailableLibraries()
		{
			lock (FCatalog.Libraries)
			{
				FCatalog.UpdateTimeStamp();
				if (!IsRepository)
					Schema.Library.GetAvailableLibraries(InstanceDirectory, FLibraryDirectory, FCatalog.Libraries);

				// Create the implicit system library
				Schema.Library LSystemLibrary = new Schema.Library(CSystemLibraryName);
				LSystemLibrary.Files.Add(new Schema.FileReference("Alphora.Dataphor.DAE.dll", true));
				Version LVersion = GetType().Assembly.GetName().Version;
				LSystemLibrary.Version = new VersionNumber(LVersion.Major, LVersion.Minor, LVersion.Build, LVersion.Revision);
				LSystemLibrary.DefaultDeviceName = CTempDeviceName;
				FCatalog.Libraries.Add(LSystemLibrary);

				if (!IsRepository)
				{				
					// Ensure the general library exists
					if (!FCatalog.Libraries.Contains(CGeneralLibraryName))
					{
						Schema.Library LGeneralLibrary = new Schema.Library(CGeneralLibraryName);
						LGeneralLibrary.Libraries.Add(new Schema.LibraryReference(CSystemLibraryName, new VersionNumber(-1, -1, -1, -1)));
						FCatalog.Libraries.Add(LGeneralLibrary);
						string LLibraryDirectory = Path.Combine(Schema.Library.GetDefaultLibraryDirectory(LibraryDirectory), LGeneralLibrary.Name);
						MaintainedLibraryUpdate = true;
						try
						{
							if (!Directory.Exists(LLibraryDirectory))
								Directory.CreateDirectory(LLibraryDirectory);
							LGeneralLibrary.SaveToFile(Path.Combine(LLibraryDirectory, Schema.Library.GetFileName(LGeneralLibrary.Name)));
						}
						finally
						{
							MaintainedLibraryUpdate = false;
						}
					}
				}
			}
		}
		
		private bool FFirstRun; // Indicates whether or not this is the first time this server has run on the configured store
		private bool FCatalogInitialized;
		private void InitializeCatalog()
		{
			if (!FCatalogInitialized)
			{
				LogMessage("Initializing Catalog...");
				
				// Create the Catalog device
				// Note that this must be the first object created to avoid the ID being different on subsequent loads
				Schema.Object.SetNextObjectID(0);
				FCatalogDevice = new CatalogDevice(Schema.Object.GetNextObjectID(), CCatalogDeviceName, CCatalogDeviceManagerID);
				RegisterResourceManager(CCatalogDeviceManagerID, FCatalogDevice);

				// Create the system user
				FSystemUser = new Schema.User(CSystemUserID, "System User", String.Empty);

				// Create the system library
				FSystemLibrary = new Schema.LoadedLibrary(CSystemLibraryName);
				FSystemLibrary.Owner = FSystemUser;
				Assembly LDAEAssembly = GetType().Assembly;
				FSystemLibrary.Assemblies.Add(LDAEAssembly);
				FCatalog.ClassLoader.RegisterAssembly(FSystemLibrary, LDAEAssembly);
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
				FCatalogDevice.MetaData = new MetaData();
				FCatalogDevice.MetaData.Tags.Add(new Tag("DAE.ResourceManagerID", CCatalogDeviceManagerID.ToString()));
				FCatalogDevice.Start(FSystemProcess);
				FCatalogDevice.Register(FSystemProcess);

				if (!IsRepository)				
					FFirstRun = FSystemProcess.CatalogDeviceSession.IsEmpty();
				
				// If this is a repository or there are no objects in the catalog
				if (IsRepository || FFirstRun)
				{
					// create and register the core system objects (Admin user, User role, Temp device, A/T device, Server rights)
					RegisterCoreSystemObjects();

					// register the system data types
					RegisterSystemDataTypes();
					
					// If this is not a repository, snapshot the base catalog objects
					if (!IsRepository)
						FSystemProcess.CatalogDeviceSession.SnapshotBase();
				}
				else if (!IsRepository)
				{
					// resolve the core system objects
					ResolveCoreSystemObjects();
					
					// resolve the system data types
					ResolveSystemDataTypes();
				}
				
				// Bind the native type references to the system data types
				BindNativeTypes();
				
				LogMessage("Catalog Initialized.");
				
				FCatalogInitialized = true;
			}
		}

		private ServerType FServerType = ServerType.Standard;
		public ServerType ServerType { get { return FServerType; } set { FServerType = value; } }
		
		// Indicates that this server is only a definition repository and does not accept modification statements
		public bool IsRepository
		{
			get { return FServerType == ServerType.Repository; }
			set { FServerType = value ? ServerType.Repository : ServerType.Standard; }
		}				   		
		public bool IsEmbedded
		{
			get { return FServerType == ServerType.Embedded; }
			set { FServerType = value ? ServerType.Embedded : ServerType.Standard; }
		}
		
		private void RegisterCoreSystemObjects()
		{
			// Creates and registers the core system objects required to compile and execute any D4 statements
			// This will only be called if this is a repository, or if this is a first-time startup for a new server
			
			FSystemProcess.BeginTransaction(IsolationLevel.Isolated);
			try
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
				FTempDevice = new MemoryDevice(Schema.Object.GetNextObjectID(), CTempDeviceName, CTempDeviceManagerID);
				RegisterResourceManager(CTempDeviceManagerID, FTempDevice);
				FTempDevice.Owner = FSystemUser;
				FTempDevice.Library = FSystemLibrary;
				FTempDevice.ClassDefinition = new ClassDefinition("System.MemoryDevice");
				FTempDevice.ClassDefinition.Attributes.Add(new ClassAttributeDefinition("MaxRowCount", CTempDeviceMaxRowCount.ToString()));
				FTempDevice.MaxRowCount = CTempDeviceMaxRowCount;
				FTempDevice.MetaData = new MetaData();
				FTempDevice.MetaData.Tags.Add(new Tag("DAE.ResourceManagerID", CTempDeviceManagerID.ToString()));
				FTempDevice.Start(FSystemProcess);			
				FTempDevice.Register(FSystemProcess);
				FSystemProcess.CatalogDeviceSession.InsertCatalogObject(FTempDevice);
				
				// Create the A/T Device
				FATDevice = new ApplicationTransactionDevice(Schema.Object.GetNextObjectID(), CATDeviceName, CATDeviceManagerID);
				RegisterResourceManager(CATDeviceManagerID, FATDevice);
				FATDevice.Owner = FSystemUser;
				FATDevice.Library = FSystemLibrary;
				FATDevice.ClassDefinition = new ClassDefinition("System.ApplicationTransactionDevice");
				FATDevice.ClassDefinition.Attributes.Add(new ClassAttributeDefinition("MaxRowCount", CATDeviceMaxRowCount.ToString()));
				FATDevice.MaxRowCount = CATDeviceMaxRowCount;
				FATDevice.MetaData = new MetaData();
				FATDevice.MetaData.Tags.Add(new Tag("DAE.ResourceManagerID", CATDeviceManagerID.ToString()));
				FATDevice.Start(FSystemProcess);			
				FATDevice.Register(FSystemProcess);
				FSystemProcess.CatalogDeviceSession.InsertCatalogObject(FATDevice);

				if (!IsRepository)
				{
					// Grant usage rights on the Temp and A/T devices to the User role
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.CreateStore), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.AlterStore), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.DropStore), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.Read), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FTempDevice.GetRight(Schema.RightNames.Write), FUserRole.ID);
						
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.CreateStore), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.AlterStore), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.DropStore), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.Read), FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(FATDevice.GetRight(Schema.RightNames.Write), FUserRole.ID);
					
					// Create and register the system rights
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateType, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateTable, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateView, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateOperator, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateDevice, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateConstraint, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateReference, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateUser, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.CreateRole, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.AlterUser, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.DropUser, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.MaintainSystemDeviceUsers, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.MaintainUserSessions, FSystemUser.ID);
					FSystemProcess.CatalogDeviceSession.InsertRight(Schema.RightNames.HostImplementation, FSystemUser.ID);
					
					// Grant create rights to the User role
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateType, FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateTable, FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateView, FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateOperator, FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateDevice, FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateConstraint, FUserRole.ID);
					FSystemProcess.CatalogDeviceSession.GrantRightToRole(Schema.RightNames.CreateReference, FUserRole.ID);
				}

				FSystemProcess.CommitTransaction();
			}
			catch
			{
				FSystemProcess.RollbackTransaction();
				throw;
			}
		}
		
		private void ResolveCoreSystemObjects()
		{
			// Use the persistent catalog store to resolve references to the core catalog objects
			FSystemProcess.CatalogDeviceSession.ResolveUser(CSystemUserID);
			FSystemProcess.CatalogDeviceSession.CacheCatalogObject(FCatalogDevice);
			FAdminUser = FSystemProcess.CatalogDeviceSession.ResolveUser(CAdminUserID);
			FUserRole = (Schema.Role)FSystemProcess.CatalogDeviceSession.ResolveName(CUserRoleName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			FTempDevice = (MemoryDevice)FSystemProcess.CatalogDeviceSession.ResolveName(CTempDeviceName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			FATDevice = (ApplicationTransactionDevice)FSystemProcess.CatalogDeviceSession.ResolveName(CATDeviceName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
		}
		
		private void RegisterSystemDataTypes()
		{
			using (Stream LStream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.DataTypes.d4"))
			{
				RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
			}
		}
		
		private void ResolveSystemDataTypes()
		{
			// Note that these (and the native type references set in BindNativeTypes) are also set in the CatalogDevice (FixupSystemTypeReferences)
			// The functionality is duplicated to ensure that the references will be set on a delayed load of a system type, as well as to ensure that a repository functions correctly without delayed resolution
			Catalog.DataTypes.SystemScalar = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemScalar, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemBoolean = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemBoolean, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemDecimal = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemDecimal, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemLong = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemLong, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemInteger = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemInteger, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemShort = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemShort, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemByte = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemByte, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemString = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemString, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemTimeSpan = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemTimeSpan, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemDateTime = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemDateTime, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemDate = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemDate, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemTime = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemTime, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemMoney = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemMoney, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemGuid = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemGuid, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemBinary = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemBinary, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemGraphic = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemGraphic, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemError = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemError, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
			Catalog.DataTypes.SystemName = (Schema.ScalarType)FSystemProcess.CatalogDeviceSession.ResolveName(Schema.DataTypes.CSystemName, FSystemLibrary.GetNameResolutionPath(FSystemLibrary), new StringCollection());
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
		
		private bool FCatalogRegistered;
		private void RegisterCatalog()
		{
			// Startup the catalog
			if (!FCatalogRegistered)
			{
				FCatalogRegistered = true;
				
				if (!IsRepository)
				{
					if (FFirstRun)
					{
						LogMessage("Registering system catalog...");
						using (Stream LStream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.SystemCatalog.d4"))
						{
							RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
						}
						LogMessage("System catalog registered.");
						LogMessage("Registering debug operators...");
						using (Stream LStream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Debug.Debug.d4"))
						{
							RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
						}
						LogMessage("Debug operators registered.");
					}
				}
			}
		}
		
		internal Schema.Objects GetBaseCatalogObjects()
		{
			CheckState(ServerState.Started);
			return FSystemProcess.CatalogDeviceSession.GetBaseCatalogObjects();
		}
		
		private void EnsureGeneralLibraryLoaded()
		{
			if (!IsRepository)
			{
				// Ensure the general library is loaded
				if (!FCatalog.LoadedLibraries.Contains(CGeneralLibraryName))
				{
					SystemEnsureLibraryRegisteredNode.EnsureLibraryRegistered(FSystemProcess, CGeneralLibraryName, true);
					FSystemSession.CurrentLibrary = FSystemLibrary;
				}
			}
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
				AProcess.Plan.PushSecurityContext(new SecurityContext(ADevice.Owner));
				try
				{
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
				}
				finally
				{
					AProcess.Plan.PopSecurityContext();
				}
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
	}
}
