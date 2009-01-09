//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT

/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

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
using Remoting = System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Services;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;
using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;

/*
	DAE Object Hierarchy ->
	
		System.MarshalByRefObject
			|- ServerObject
			|	|- Server
			|	|- ServerChildObject
			|	|	|- ServerSession
			|	|	|- ServerProcess
			|	|	|- ServerScript
			|	|	|- ServerBatch
			|	|	|- ServerPlanBase
			|	|	|	|- ServerPlan
			|	|	|	|	|- ServerStatementPlan
			|	|	|	|	|- ServerExpressionPlan
			|	|	|	|- RemoteServerPlan
			|	|	|	|	|- RemoteServerStatementPlan
			|	|	|	|	|- RemoteServerExpressionPlan
			|	|	|- ServerCursorBase
			|	|	|	|- ServerCursor
			|	|	|	|- RemoteServerCursor
				
	
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
	[AttributeUsage(AttributeTargets.Assembly)]
	public class DAERegisterAttribute : Attribute
	{
		public DAERegisterAttribute(string ARegisterClassName)
		{
			FRegisterClassName = ARegisterClassName;
		}
		
		private string FRegisterClassName;
		public string RegisterClassName
		{
			get { return FRegisterClassName; }
			set { FRegisterClassName = value; }
		}
	}
	
	public enum LogEvent
	{
		LogStarted,
		ServerStarted,
		LogStopped,
		ServerStopped
	}
	
	public enum LogEntryType
	{
		Information,
		Warning,
		Error
	}
	
	public class ServerObject : MarshalByRefObject, IDisposableNotify
	{
		public const int CLeaseManagerPollTimeSeconds = 60;

		static ServerObject()
		{
			LifetimeServices.LeaseManagerPollTime = TimeSpan.FromSeconds(CLeaseManagerPollTimeSeconds);
			LifetimeServices.LeaseTime = TimeSpan.Zero; // default lease to infinity (overridden for session)
		}

		protected virtual void Dispose(bool ADisposing)
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			try
			{
				DoDispose();
			}
			finally
			{
				ILease LLease = (ILease)GetLifetimeService();
				if (LLease != null)
				{
					// Calling cancel on the lease will invoke disconnect on the lease and the server object
					LLease.GetType().GetMethod("Cancel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(LLease, null);
				}
				else
					Remoting.RemotingServices.Disconnect(this);
			}
		}

		#if USEFINALIZER
		~ServerObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
        
		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}
	}
	
	public class ServerChildObject : MarshalByRefObject, IDisposableNotify
	{
		#if USEFINALIZER
		~ServerChildObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
        
		protected virtual void Dispose(bool ADisposing)
		{
			try
			{
				DoDispose();
			}
			finally
			{
				ILease LLease = (ILease)GetLifetimeService();
				if (LLease != null)
					LLease.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(LLease, null);
				Remoting.RemotingServices.Disconnect(this);
			}
		}
		
		public void Dispose()
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}
	}

	[Serializable]	
	public class ServerChildObjects : Disposable, IList
	{
		private const int CDefaultInitialCapacity = 8;
		private const int CDefaultRolloverCount = 20;
		        
		private ServerChildObject[] FItems;
		private int FCount;
		private bool FIsOwner = true;
		
		public ServerChildObjects() : base()
		{
			FItems = new ServerChildObject[CDefaultInitialCapacity];
		}
		
		public ServerChildObjects(bool AIsOwner)
		{
			FItems = new ServerChildObject[CDefaultInitialCapacity];
			FIsOwner = AIsOwner;
		}
		
		public ServerChildObjects(int AInitialCapacity) : base()
		{
			FItems = new ServerChildObject[AInitialCapacity];
		}
		
		public ServerChildObjects(int AInitialCapacity, bool AIsOwner) : base()
		{
			FItems = new ServerChildObject[AInitialCapacity];
			FIsOwner = AIsOwner;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Clear();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		public ServerChildObject this[int AIndex] 
		{ 
			get
			{ 
				return FItems[AIndex];
			} 
			set
			{ 
				lock (this)
				{
					InternalRemoveAt(AIndex);
					InternalInsert(AIndex, value);
				}
			} 
		}
		
		protected int InternalIndexOf(ServerChildObject AItem)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (FItems[LIndex] == AItem)
					return LIndex;
			return -1;
		}
		
		public int IndexOf(ServerChildObject AItem)
		{
			lock (this)
			{
				return InternalIndexOf(AItem);
			}
		}
		
		public bool Contains(ServerChildObject AItem)
		{
			return IndexOf(AItem) >= 0;
		}
		
		public int Add(object AItem)
		{
			lock (this)
			{
				int LIndex = FCount;
				InternalInsert(LIndex, AItem);
				return LIndex;
			}
		}
		
		public void AddRange(ICollection ACollection)
		{
			foreach (object AObject in ACollection)
				Add(AObject);
		}
		
		private void InternalInsert(int AIndex, object AItem)
		{
			ServerChildObject LObject;
			if (AItem is ServerChildObject)
				LObject = (ServerChildObject)AItem;
			else
				throw new ServerException(ServerException.Codes.ObjectContainer);
				
			Validate(LObject);
				
			if (FCount >= FItems.Length)
				Capacity *= 2;
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = LObject;
			FCount++;

			Adding(LObject, AIndex);
		}
		
		public void Insert(int AIndex, object AItem)
		{
			lock (this)
			{
				InternalInsert(AIndex, AItem);
			}
		}
		
		private void InternalSetCapacity(int AValue)
		{
			if (FItems.Length != AValue)
			{
				ServerChildObject[] LNewItems = new ServerChildObject[AValue];
				for (int LIndex = 0; LIndex < ((FCount > AValue) ? AValue : FCount); LIndex++)
					LNewItems[LIndex] = FItems[LIndex];

				if (FCount > AValue)						
					for (int LIndex = FCount - 1; LIndex > AValue; LIndex--)
						InternalRemoveAt(LIndex);
						
				FItems = LNewItems;
			}
		}
		
		public int Capacity
		{
			get { return FItems.Length; }
			set
			{
				lock (this)
				{
					InternalSetCapacity(value);
				}
			}
		}
		
		private void InternalRemoveAt(int AIndex)
		{
			Removing(FItems[AIndex], AIndex);

			FCount--;			
			for (int LIndex = AIndex; LIndex < FCount; LIndex++)
				FItems[LIndex] = FItems[LIndex + 1];
			FItems[FCount] = null; // This clear must occur or the reference is still live 
		}
		
		public void RemoveAt(int AIndex)
		{
			lock (this)
			{
				InternalRemoveAt(AIndex);
			}
		}
		
		public void Remove(ServerChildObject AValue)
		{
			lock (this)
			{
				InternalRemoveAt(InternalIndexOf(AValue));
			}
		}
		
		public void Clear()
		{
			lock (this)
			{
				while (FCount > 0)
					InternalRemoveAt(FCount - 1);
			}
		}
		
		protected bool FDisowning;
		
		public ServerChildObject Disown(ServerChildObject AValue)
		{
			lock (this)
			{
				FDisowning = true;
				try
				{
					InternalRemoveAt(InternalIndexOf(AValue));
					return AValue;
				}
				finally
				{
					FDisowning = false;
				}
			}
		}

		public ServerChildObject SafeDisown(ServerChildObject AValue)
		{
			lock (this)
			{
				FDisowning = true;
				try
				{
					int LIndex = InternalIndexOf(AValue);
					if (LIndex >= 0)
						InternalRemoveAt(LIndex);
					return AValue;
				}
				finally
				{
					FDisowning = false;
				}
			}
		}

		public ServerChildObject DisownAt(int AIndex)
		{
			lock (this)
			{
				ServerChildObject LValue = FItems[AIndex];
				FDisowning = true;
				try
				{
					InternalRemoveAt(AIndex);
					return LValue;
				}
				finally
				{
					FDisowning = false;
				}
			}
		}

		protected virtual void ObjectDispose(object ASender, EventArgs AArgs)
		{
			Disown((ServerChildObject)ASender);
		}
		
		protected virtual void Validate(ServerChildObject AObject)
		{
		}
		
		protected virtual void Adding(ServerChildObject AObject, int AIndex)
		{
			AObject.Disposed += new EventHandler(ObjectDispose);
		}
		
		protected virtual void Removing(ServerChildObject AObject, int AIndex)
		{
			AObject.Disposed -= new EventHandler(ObjectDispose);

			if (FIsOwner && !FDisowning)
				AObject.Dispose();
		}

		// IList
		object IList.this[int AIndex] { get { return this[AIndex]; } set { this[AIndex] = (ServerChildObject)value; } }
		int IList.IndexOf(object AItem) { return (AItem is ServerChildObject) ? IndexOf((ServerChildObject)AItem) : -1; }
		bool IList.Contains(object AItem) { return (AItem is ServerChildObject) ? Contains((ServerChildObject)AItem) : false; }
		void IList.Remove(object AItem) { RemoveAt(IndexOf((ServerChildObject)AItem)); }
		bool IList.IsFixedSize { get { return false; } }
		bool IList.IsReadOnly { get { return false; } }
		
		// ICollection
		public int Count { get { return FCount; } }
		public bool IsSynchronized { get { return true; } }
		public object SyncRoot { get { return this; } }
		public void CopyTo(Array AArray, int AIndex)
		{
			lock (this)
			{
				IList LArray = (IList)AArray;
				for (int LIndex = 0; LIndex < Count; LIndex++)
					LArray[AIndex + LIndex] = this[LIndex];
			}
		}

		// IEnumerable
		public ServerChildObjectEnumerator GetEnumerator()
		{
			return new ServerChildObjectEnumerator(this);
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public class ServerChildObjectEnumerator : IEnumerator
		{
			public ServerChildObjectEnumerator(ServerChildObjects AObjects) : base()
			{
				FObjects = AObjects;
			}
			
			private ServerChildObjects FObjects;
			private int FCurrent =  -1;

			public ServerChildObject Current { get { return FObjects[FCurrent]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				FCurrent++;
				return (FCurrent < FObjects.Count);
			}
			
			public void Reset()
			{
				FCurrent = -1;
			}
		}
	}
	
	// Catalog Caches
	public class CatalogCache : System.Object
	{
		public CatalogCache(string ACacheName, long ACacheTimeStamp, Schema.Catalog ADefaultCachedObjects) : base()
		{
			FCacheName = ACacheName;
			FCacheTimeStamp = ACacheTimeStamp;
			PopulateDefaultCachedObjects(ADefaultCachedObjects);
		}
		
		private string FCacheName;
		public string CacheName { get { return FCacheName; } }
		
		// CacheTimeStamp
		// The time stamp of the server-side catalog when this cache was built
		private long FCacheTimeStamp;
		public long CacheTimeStamp { get { return FCacheTimeStamp; } }
		
		// TimeStamp
		// A logical clock used to coordinate changes to the cache with the client
		// When the client receives the results of a prepare, the cache time stamp and the time stamp
		// are used to decide whether the client-side cache should be recreated, or just modified. If it is to be modified,
		// the client-side must wait until the client-side cache timestamp is timestamp - 1 in order for the cache to be ready
		// to apply the changes in the deserialization script. A timeout waiting for the timestamp to update will result in an
		// UpdateTimeStamps call to the server to reset the caching system.
		private long FTimeStamp = 1;
		public long TimeStamp { get { return FTimeStamp; } }
		
		public void UpdateTimeStamp()
		{
			FTimeStamp += 1;
		}
		
		public bool EnsureConsistent(long ACacheTimeStamp, Schema.Catalog ADefaultCachedObjects)
		{
			if (ACacheTimeStamp > FCacheTimeStamp)
			{
				FCachedObjects.Clear();
				FCacheTimeStamp = ACacheTimeStamp;
				PopulateDefaultCachedObjects(ADefaultCachedObjects);
				UpdateTimeStamp();
				return true;
			}
			return false;
		}
		
		private void PopulateDefaultCachedObjects(Schema.Catalog ADefaultCachedObjects)
		{
			foreach (Schema.Object LObject in ADefaultCachedObjects)
				FCachedObjects.Add(LObject);
		}
		
		private Schema.Objects FCachedObjects = new Schema.Objects();
		public Schema.Objects CachedObjects { get { return FCachedObjects; } }
		
		private ServerSessions FSessions = new ServerSessions(false);
		public ServerSessions Sessions { get { return FSessions; } }
	}
	
	public class CatalogCaches : System.Object
	{
		private Hashtable FCaches = new Hashtable();
		private Schema.Catalog FDefaultCachedObjects = new Schema.Catalog();
		
		public void AddSession(ServerSession ASession)
		{
			lock (this)
			{
				CatalogCache LCache = (CatalogCache)FCaches[ASession.SessionInfo.CatalogCacheName];
				if (LCache == null)
				{
					LCache = new CatalogCache(ASession.SessionInfo.CatalogCacheName, ASession.Server.CacheTimeStamp, FDefaultCachedObjects);
					FCaches.Add(LCache.CacheName, LCache);
				}
				
				LCache.Sessions.Add(ASession);
			}
		}
		
		public void RemoveSession(ServerSession ASession)
		{
			lock (this)
			{
				CatalogCache LCache = (CatalogCache)FCaches[ASession.SessionInfo.CatalogCacheName];
				if (LCache != null)
					LCache.Sessions.Remove(ASession);
			}
		}
		
		public void RemoveCache(string ACatalogCacheName)
		{
			lock (this)
			{
				CatalogCache LCache = (CatalogCache)FCaches[ACatalogCacheName];
				if (LCache != null)
					FCaches.Remove(ACatalogCacheName);
			}
		}
		
		public string[] GetRequiredObjects(ServerSession ASession, Schema.Catalog ACatalog, long ACacheTimeStamp, out long AClientCacheTimeStamp)
		{
			StringCollection LRequiredObjects = new StringCollection();
			CatalogCache LCache = (CatalogCache)FCaches[ASession.SessionInfo.CatalogCacheName];

			lock (LCache)
			{
				bool LCacheChanged = LCache.EnsureConsistent(ACacheTimeStamp, FDefaultCachedObjects);
				foreach (Schema.Object LObject in ACatalog)
					if (!LCache.CachedObjects.ContainsName(LObject.Name))
					{
						if (!((LObject is Schema.DerivedTableVar) && (LObject.Name == ((Schema.DerivedTableVar)LObject).SessionObjectName)))
							LCache.CachedObjects.Add(LObject);
						LRequiredObjects.Add(LObject.Name);
					}
					
				if (!LCacheChanged)
					LCache.UpdateTimeStamp();
					
				AClientCacheTimeStamp = LCache.TimeStamp;
			}

			string[] LResult = new string[LRequiredObjects.Count];
			LRequiredObjects.CopyTo(LResult, 0);

			#if LOGCACHEEVENTS
			ASession.Server.LogMessage(String.Format("Session {0} cache timestamp updated to {1} with required objects: {2}", ASession.SessionID.ToString(), AClientCacheTimeStamp.ToString(), ExceptionUtility.StringsToCommaList(LRequiredObjects)));
			#endif

			return LResult;
		}

		public void RemovePlanDescriptor(ServerSession ASession, string ACatalogObjectName)
		{
			CatalogCache LCache = (CatalogCache)FCaches[ASession.SessionInfo.CatalogCacheName];
			if (LCache != null)
			{
				lock (LCache)
				{
					int LIndex = LCache.CachedObjects.IndexOfName(ACatalogObjectName);
					if (LIndex >= 0)
						LCache.CachedObjects.RemoveAt(LIndex);
				}
			}
		}
		
		public void GatherDefaultCachedObjects(Schema.Objects ABaseObjects)
		{
			for (int LIndex = 0; LIndex < ABaseObjects.Count; LIndex++)
				FDefaultCachedObjects.Add(ABaseObjects[LIndex]);
		}
	}

	/// <summary> Library notify event for the Library notification events in the Dataphor Server </summary>	
	/// <remarks> Note that these events are only surfaced in process, and cannot be used through the remoting boundary. </remarks>
	public delegate void LibraryNotifyEvent(Server AServer, string ALibraryName);

	/// <summary> Device notify event for the Device notification events in the Dataphor Server </summary>
	/// <remarks> Note that these events are only surfaced in process, and cannot be used through the remoting boundary. </remarks>
	public delegate void DeviceNotifyEvent(Server AServer, Schema.Device ADevice);
		
	public enum ServerType 
	{ 
		/// <summary>Standard Dataphor Server, the default behavior is used.</summary>
		Standard, 
	
		/// <summary>Embedded Dataphor Server, the server is intended to be used as a single-user data access engine embedded within a client.</summary>
		Embedded, 
	
		/// <summary>Repository, the server is intended to be used as a client-side catalog repository for a client connecting to a remote server.</summary>
		Repository 
	}

	/// <summary> Dataphor DAE Server class. </summary>
	/// <remarks>
	///		Provides an instance of a Dataphor DAE Server.  This object is usually accessed
	///		through the IServerXXX common interfaces which make up the DAE CLI.  Instances
	///		are usually created and obtained through the <see cref="ServerFactory"/> class.
	/// </remarks>
	public class Server : ServerObject, IDisposable, IServer, IRemoteServer, ITrackingHandler
	{
		// Do not localize
		public const string CDefaultServerName = "dataphor";
		public const string CServerLogName = @"Dataphor";											 
		public const string CServerSourceName = @"Dataphor Server";
		public const string CDefaultLibraryDirectory = @"Libraries";
		public const string CDefaultCatalogDirectory = @"Catalog";
		public const string CDefaultBackupDirectory = @"Backup";
		public const string CDefaultSaveDirectory = @"Save";
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
		public const int CDefaultMaxStackDepth = 32767;
		public const int CDefaultMaxCallDepth = 1024;
		public const int CDefaultProcessWaitTimeout = 30;
		public const int CDefaultProcessTerminationTimeout = 30;
		public const int CDefaultPlanCacheSize = 1000;
		
		// constructor		
		public Server() : base()
		{
			FSessions = new ServerSessions();
			FConnections = new ServerConnections();
			TrackingServices.RegisterTrackingHandler(this);
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
					try
					{
						TrackingServices.UnregisterTrackingHandler(this);
					}
					finally
					{
						Stop();
					}
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
			FCatalogCaches = new CatalogCaches();
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
		
		// PlanCache
		private PlanCache FPlanCache;
		public PlanCache PlanCache { get { return FPlanCache; } }
		
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
		
		public void StopProcess(int AProcessID)
		{
			foreach (ServerSession LSession in Sessions)
			{
				ServerProcess LProcessToStop = null;
				lock (LSession.Processes)
					foreach (ServerProcess LProcess in LSession.Processes)
						if (LProcess.ProcessID == AProcessID)
						{
							LProcessToStop = LProcess;
							break;
						}
				
				if (LProcessToStop != null)
				{
					TerminateProcess(LProcessToStop);
					return;
				}
			}

			throw new ServerException(ServerException.Codes.ProcessNotFound, AProcessID);
		}

		public void CloseSession(int ASessionID)
		{
			foreach (ServerSession LSession in Sessions)
			{
				if (LSession.SessionID == ASessionID)
				{
					LSession.Dispose();
					return;
				}
			}

			throw new ServerException(ServerException.Codes.SessionNotFound, ASessionID);
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
		
		public bool IsRemotableExceptionClass(Exception AException)
		{
			return (AException is DAEException) || (AException.GetType() == typeof(DataphorException));
		}
		
		public bool IsRemotableException(Exception AException)
		{
			if (!IsRemotableExceptionClass(AException))
				return false;
				
			Exception LException = AException;
			while (LException != null)
			{
				if (!IsRemotableExceptionClass(LException))
					return false;
				LException = LException.InnerException;
			}
			return true;
		}
		
		public Exception EnsureRemotableException(Exception AException)
		{
			if (!IsRemotableException(AException))
			{
				Exception LInnerException = null;
				if (AException.InnerException != null)
					LInnerException = EnsureRemotableException(AException.InnerException);
					
				AException = new DataphorException(AException, LInnerException);
			}
			
			return AException;
		}
		
		public Exception WrapException(Exception AException, bool AIsRemoteCall)
		{
			if (FLogErrors)
				LogError(AException);
				
			if (!IsRepository && AIsRemoteCall)
				AException = EnsureRemotableException(AException);
				
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
						StartLog();
						FNextSessionID = 0;
						SetState(ServerState.Starting);
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
				throw WrapException(E, true);
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
//			if (AScript != String.Empty)
//				RunScript(AScript, String.Empty);
//			else
			if (FFirstRun)
				LoadCatalog(); // Load the catalog from the d4c files if this is the first time running on the configured store for this instance
			else
				LoadServerState(); // Load server state from the persistent store

			FCatalogLoaded = true;
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
				throw WrapException(E, true);
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
		
		private string GetLogFileName(int ALogIndex)
		{
			return Path.Combine(IsRepository ? PathUtility.CommonAppDataPath() : PathUtility.GetBinDirectory(), String.Format("{0}{1}.log", CServerLogName, ALogIndex == 0 ? String.Empty : ALogIndex.ToString()));
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
		
		private FileStream FLogFile;
		private StreamWriter FLog;

		private void StartLog()
		{
			if (FLoggingEnabled)
			{
				if (!IsRepository && (System.Environment.OSVersion.Platform == PlatformID.Win32NT) && !EventLog.SourceExists(CServerSourceName))
					EventLog.CreateEventSource(CServerSourceName, CServerLogName);
	
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
							try
							{
								try
								{
									//if (FCatalogLoaded)
									//	SaveCatalog(false);
								}
								finally
								{
									if (FPlanCache != null)
									{
										FPlanCache.Clear(FSystemProcess);
										FPlanCache = null;
									}
								}
							}
							finally
							{
								StopDevices();
							}
						}
						finally
						{
							CloseConnections();
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
						Monitor.Enter(AProcess.ExecutionSyncHandle);
						try
						{
							if ((AProcess.ExecutingThread == null) || !AProcess.ExecutingThread.IsAlive)
								return;
						}
						finally
						{
							Monitor.Exit(AProcess.ExecutionSyncHandle);
						}

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
			try
			{
				TerminateProcessThread(AProcess);
			}
			finally
			{
				((IServerSession)AProcess.ServerSession).StopProcess(AProcess);
			}
		}
		
		// Catalog Caches
		private CatalogCaches FCatalogCaches;
		public CatalogCaches CatalogCaches { get { return FCatalogCaches; } }
		
		// Sessions
		private ServerSessions FSessions;
		internal ServerSessions Sessions { get { return FSessions; } }
		
		// Connections
		private ServerConnections FConnections;
		internal ServerConnections Connections { get { return FConnections; } }

		public void DisconnectedObject(object AObject)
		{
			// Check if this object is a connection that needs to be disposed and dispose it if so
			// This should not recurse because the RemotingServices.Disconnect call in the connection dispose
			//  should not notify TrackingHandlers if the object has already been disconnected, and if the 
			//  connection has already been disposed than it should no longer be associated with this server
			// BTR 5/12/2005 -> Use a thread pool to perform this call so that there is no chance of blocking
			// the lease manager.
			ServerConnection LConnection = AObject as ServerConnection;
			if ((LConnection != null) && (LConnection.Server == this))
				ThreadPool.QueueUserWorkItem(new WaitCallback(DisposeConnection), LConnection);
		}
		
		private void DisposeConnection(Object AStateInfo)
		{
			try
			{
				ServerConnection LConnection = AStateInfo as ServerConnection;
				
				LConnection.CloseSessions();
				
				BeginCall();	// sync here; we may be coming in on a remoting thread
				try
				{
					FConnections.SafeDisown(LConnection);
				}
				finally
				{
					EndCall();
				}
				
				try
				{
					LConnection.Dispose();
				}
				catch (Exception E)
				{
					LogError(E);
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		public void UnmarshaledObject(object AObject, System.Runtime.Remoting.ObjRef ARef)
		{
			// nothing (part of ITrackingHandler)
		}

		public void MarshaledObject(object AObject, System.Runtime.Remoting.ObjRef ARef)
		{
			// nothing (part of ITrackingHandler)
		}

		private int FNextSessionID = 1;
		private int GetNextSessionID()
		{
			return Interlocked.Increment(ref FNextSessionID);
		}
		
		// IServer.Connect
		IServerSession IServer.Connect(SessionInfo ASessionInfo)
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
				throw WrapException(E, false);
			}
			finally
			{
				EndCall();
			}
		}
		
		// IServer.Disconnect
		void IServer.Disconnect(IServerSession ASession)
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
					throw WrapException(E, false);
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
		
		public IRemoteServerConnection Establish(string AConnectionName, string AHostName)
		{
			BeginCall();
			try
			{
				CheckState(ServerState.Started);
				ServerConnection LConnection = new ServerConnection(this, AConnectionName, AHostName);
				try
				{
					FConnections.Add(LConnection);
					return LConnection;
				}
				catch
				{
					LConnection.Dispose();
					throw;
				}
			}
			catch (Exception E)
			{
				throw WrapException(E, true);
			}
			finally
			{
				EndCall();
			}
		}
        
        public void Relinquish(IRemoteServerConnection AConnection)
        {
			ServerConnection LConnection = AConnection as ServerConnection;
			LConnection.CloseSessions();

			BeginCall();
			try
			{
				FConnections.SafeDisown(LConnection);
			}
			finally
			{
				EndCall();
			}
			
			try
			{
				LConnection.Dispose();
			}
			catch (Exception E)
			{
				throw WrapException(E, true);
			}
        }
        
        internal ServerSession RemoteConnect(SessionInfo ASessionInfo)
        {
			BeginCall();
			try
			{
				#if TIMING
				System.Diagnostics.Debug.WriteLine(String.Format("{0} -- IRemoteServer.Connect", DateTime.Now.ToString("hh:mm:ss.ffff")));
				#endif
				CheckState(ServerState.Started);
				return InternalConnect(GetNextSessionID(), ASessionInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E, true);
			}
			finally
			{
				EndCall();
			}
        }
        
        internal void RemoteDisconnect(ServerSession ASession)
        {
			try
			{
				try
				{
					InternalDisconnect(ASession);
				}
				catch (Exception E)
				{
					throw WrapException(E, true);
				}
			}
			finally
			{
				BeginCall();
				try
				{
					FSessions.SafeDisown(ASession);
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
					
				if (ASessionInfo.CatalogCacheName != String.Empty)
					FCatalogCaches.AddSession(LSession);
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
		
		private void CloseConnections()
		{
			if (FConnections != null)
			{
				while (FConnections.Count > 0)
				{
					try
					{
						Relinquish(FConnections[0]);
					}
					catch (Exception E)
					{
						LogError(E);
					}
				}
			}
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
			if (FLoggingEnabled)
			{
				if (!IsRepository && (System.Environment.OSVersion.Platform == PlatformID.Win32NT))
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
				LColumn = new Schema.TableVarColumn(new Schema.Column("ID", AProcess.Plan.Catalog.DataTypes.SystemInteger));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("TraceCode", AProcess.Plan.Catalog.DataTypes.SystemString));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("DateTime", AProcess.Plan.Catalog.DataTypes.SystemDateTime));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("User_ID", AProcess.Plan.Catalog.DataTypes.SystemString));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("Session_ID", AProcess.Plan.Catalog.DataTypes.SystemInteger));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("Process_ID", AProcess.Plan.Catalog.DataTypes.SystemInteger));
				FTraceTableVar.Columns.Add(LColumn);
				FTraceTableVar.DataType.Columns.Add(LColumn.Column);
				LColumn = new Schema.TableVarColumn(new Schema.Column("Description", AProcess.Plan.Catalog.DataTypes.SystemString));
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
		
		private string FCatalogDirectory = String.Empty;
		/// <summary> The directory the DAE will use to persist system and library catalogs. </summary>
		public string CatalogDirectory
		{
			get { return FCatalogDirectory; }
			set
			{
				CheckState(ServerState.Stopped);
				if ((value == null) || (value == String.Empty))
					FCatalogDirectory = String.Empty;
				else
				{
					if (Path.IsPathRooted(value))
						FCatalogDirectory = value;
					else
						FCatalogDirectory = Path.Combine(PathUtility.GetBinDirectory(), value);
				}
			}
		}
		
		private string FCatalogStoreDatabaseName = "DAECatalog";
		/// <summary>The name of the database to be used for the catalog store.</summary>
		public string CatalogStoreDatabaseName
		{
			get { return FCatalogStoreDatabaseName; }
			set 
			{ 
				CheckState(ServerState.Stopped); 
				if ((value == null) || (value == String.Empty))
					FCatalogStoreDatabaseName = "DAECatalog";
				else
				{
					if (!Parser.IsValidIdentifier(value))
						throw new ParserException(ParserException.Codes.InvalidIdentifier, value);
					FCatalogStoreDatabaseName = value;
				}
			}
		}
		
		public string GetCatalogStoreDatabaseFileName()
		{
			string LCatalogDirectory;
			if (FCatalogDirectory == String.Empty)
				LCatalogDirectory = GetDefaultCatalogDirectory();
			else
			{
				LCatalogDirectory = FCatalogDirectory;
				if (!Directory.Exists(LCatalogDirectory))
					Directory.CreateDirectory(LCatalogDirectory);
			}
			
			return Path.Combine(LCatalogDirectory, Path.ChangeExtension(FCatalogStoreDatabaseName, ".sdf"));
		}

		private string FCatalogStorePassword = String.Empty;
		/// <summary>The password to be used to connect to the catalog store.</summary>
		public string CatalogStorePassword
		{
			get { return FCatalogStorePassword; }
			set 
			{ 
				CheckState(ServerState.Stopped); 
				if ((value == null) || (value == String.Empty))
					FCatalogStorePassword = "";
				else
					FCatalogStorePassword = value;
			}
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
		
		public static string GetDefaultCatalogDirectory()
		{
			string LDirectoryName = Path.Combine(PathUtility.GetBinDirectory(), CDefaultCatalogDirectory);
			if (!Directory.Exists(LDirectoryName))
				Directory.CreateDirectory(LDirectoryName);
			return LDirectoryName;
		}

		/// <summary> Saves the system catalog to the given directory. </summary>
		/// <remarks>
		/// 	Serializes the catalog into the directory identified by 
		/// 	<see cref="CatalogDirectory" />.  If CatalogDirectory is empty, nothing
		/// 	happens.
		/// </remarks>
		public void SaveCatalog(bool AShouldThrow)
		{
			if (FCatalogDirectory != String.Empty) 
			{
				try
				{
					SaveCatalog(Path.Combine(FCatalogDirectory, CDefaultSaveDirectory));
					OverwriteCatalog(Path.Combine(FCatalogDirectory, CDefaultSaveDirectory), FCatalogDirectory);
				}
				catch (Exception E)
				{
					LogError(E);
					if (AShouldThrow)
						throw;
				}
			}
		}
		
		public void SaveCatalog()
		{
			SaveCatalog(true);
		}
		
		public void BackupCatalog()
		{
			if (FCatalogDirectory != String.Empty)
				SaveCatalog(Path.Combine(FCatalogDirectory, CDefaultBackupDirectory));
		}
		
		private void BackupFile(string AFileName)
		{
			if (File.Exists(AFileName))
				File.Copy(AFileName, Path.ChangeExtension(AFileName, ".old"), true);
		}
		
		private void RestoreFile(string AFileName)
		{
			if (File.Exists(AFileName))
				File.Copy(AFileName, Path.ChangeExtension(AFileName, ".d4c"), true);
		}
		
		private void OverwriteCatalog(string ASourceDirectory, string ATargetDirectory)
		{
			string LSourceFileName;
			string LTargetFileName;
			string[] LCatalogFileNames = Directory.GetFiles(ASourceDirectory, "*.d4c");
			string[] LWrittenFiles = new string[LCatalogFileNames.Length];
			try
			{
				for (int LIndex = 0; LIndex < LCatalogFileNames.Length; LIndex++)
				{
					LSourceFileName = LCatalogFileNames[LIndex];
					LTargetFileName = Path.Combine(ATargetDirectory, Path.GetFileName(LSourceFileName));
					BackupFile(LTargetFileName);
					File.Copy(LSourceFileName, LTargetFileName, true);
					LWrittenFiles[LIndex] = LTargetFileName;
				}
			}
			catch (Exception E)
			{
				foreach (string LRestoreFileName in LWrittenFiles)
					if (LRestoreFileName != null)
						RestoreFile(LRestoreFileName);
				
				LogError(E);
				throw;
			}
		}
		
		public void SaveLibraryCatalog(Schema.LoadedLibrary ALibrary)
		{
			if (FCatalogDirectory != String.Empty)
				SaveLibraryCatalog(FCatalogDirectory, ALibrary);
		}
		
		public bool CanLoadLibrary(string ALibraryName)
		{
			return (FCatalogDirectory != String.Empty) && File.Exists(Path.Combine(FCatalogDirectory, GetLibraryCatalogFileName(ALibraryName)));
		}
		
		private void SaveLibraryCatalog(string ADirectoryName, Schema.LoadedLibrary ALibrary)
		{
			EnsureCatalogDirectory(ADirectoryName);
			string LFileName = Path.Combine(ADirectoryName, GetLibraryCatalogFileName(ALibrary.Name));
			BackupFile(LFileName);
			using (FileStream LCatalogStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
			{
				StreamWriter LWriter = new StreamWriter(LCatalogStream);
				try
				{
					LWriter.Write(new D4TextEmitter().Emit(FCatalog.EmitStatement(FSystemProcess, EmitMode.ForCopy, ALibrary.Name, false)));
				}
				finally
				{
					LWriter.Close();
				}
			}
		}
		
		private void EnsureCatalogDirectory(string ADirectoryName)
		{
			if (!Path.IsPathRooted(ADirectoryName)) // if a relative path
				ADirectoryName = Path.Combine(PathUtility.GetBinDirectory(), ADirectoryName); // Prepend startup path of executable.

			if (!Directory.Exists(ADirectoryName))
				Directory.CreateDirectory(ADirectoryName);
		}
		
		private string SaveLibraryVersions()
		{
			FSystemSession.CurrentLibrary = FSystemLibrary;
			IServerExpressionPlan LPlan = ((IServerProcess)FSystemProcess).PrepareExpression(@"select 'insert ' + System.ScriptData('System.LibraryVersions') + ' into System.LibraryVersions;'", null);
			try
			{
				return String.Format("{0}\r\n", LPlan.Evaluate(null).AsString);
			}
			finally
			{
				((IServerProcess)FSystemProcess).UnprepareExpression(LPlan);
			}
		}
		
		private string SaveLibraryOwners()
		{
			FSystemSession.CurrentLibrary = FSystemLibrary;
			IServerExpressionPlan LPlan = ((IServerProcess)FSystemProcess).PrepareExpression(@"select 'insert ' + System.ScriptData('System.LibraryOwners') + ' into System.LibraryOwners;'", null);
			try
			{
				return String.Format("{0}\r\n", LPlan.Evaluate(null).AsString);
			}
			finally
			{
				((IServerProcess)FSystemProcess).UnprepareExpression(LPlan);
			}
		}
		
		private string SaveLibraryDirectories()
		{
			StringBuilder LStatement = new StringBuilder();
			
			foreach (Schema.Library LLibrary in Catalog.Libraries)
				if (LLibrary.Directory != String.Empty)
					LStatement.AppendFormat("AttachLibrary('{0}', '{1}');\r\n", LLibrary.Name, LLibrary.GetLibraryDirectory(LibraryDirectory));
					
			return LStatement.ToString();
		}
		
		private string SaveServerSettings()
		{
			StringBuilder LUpdateStatement = new StringBuilder();
			//if (!TracingEnabled)
			//	LUpdateStatement.AppendFormat("TracingEnabled := {0}", TracingEnabled.ToString().ToLower());
			
			//if (LogErrors)
			//{
			//	if (LUpdateStatement.Length > 0)
			//		LUpdateStatement.Append(", ");
			//	LUpdateStatement.AppendFormat("LogErrors := {0}", LogErrors.ToString().ToLower());
			//}
			
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
						Schema.Device LDevice = AProcess.CatalogDeviceSession.ResolveCatalogObject(LRow[0/*"ID"*/].AsInt32) as Schema.Device;
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
						switch (LRow[0/*"ID"*/].AsString)
						{
							case Server.CSystemUserID : break;
							case Server.CAdminUserID : 
								if (FAdminUser.Password != String.Empty)
									LResult.AppendFormat("SetEncryptedPassword('{0}', '{1}');\r\n", FAdminUser.ID, FAdminUser.Password);
							break;
							
							default :
								Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(LRow[0/*"ID"*/].AsString);
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
						Schema.User LUser = AProcess.CatalogDeviceSession.ResolveUser(LRow[0/*"User_ID"*/].AsString);
						Schema.Device LDevice = (Schema.Device)AProcess.CatalogDeviceSession.ResolveCatalogObject(LRow[1/*"Device_ID"*/].AsInt32);
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
						
						LResult.AppendFormat("AddUserToRole('{0}', '{1}');\r\n", LRow[0/*"User_ID"*/].AsString, LRow[1/*"Role_Name"*/].AsString);
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
						
						if (LRow[2/*"IsGranted"*/].AsBoolean)
							LResult.AppendFormat("GrantRightToUser('{0}', '{1}');\r\n", LRow[1/*"Right_Name"*/].AsString, LRow[0/*"User_ID"*/].AsString);
						else
							LResult.AppendFormat("RevokeRightFromUser('{0}', '{1}');\r\n", LRow[1/*"Right_Name"*/].AsString, LRow[0/*"User_ID"*/].AsString);						
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
		
		public void SaveCatalog(string ADirectoryName)
		{
			EnsureCatalogDirectory(ADirectoryName);
			Exception LException = null;
			foreach (Schema.LoadedLibrary LLibrary in FCatalog.LoadedLibraries)
			{
				try
				{
					if (LLibrary.Name != Server.CSystemLibraryName)
						SaveLibraryCatalog(ADirectoryName, LLibrary);
				}
				catch (Exception E)
				{
					LException = E;
					LogError(E);
				}
			}

			try
			{				
				string LFileName = Path.Combine(ADirectoryName, GetLibraryCatalogFileName(CSystemLibraryName));
				BackupFile(LFileName);
				using (FileStream LCatalogStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
				{
					StreamWriter LWriter = new StreamWriter(LCatalogStream);
					try
					{
						LWriter.Write(SaveLibraryVersions());
						LWriter.Write(SaveLibraryOwners());
						LWriter.Write(SaveLibraryDirectories());
						LWriter.Write(SaveServerSettings());
						LWriter.Write(SaveSystemDeviceSettings());
						LWriter.Write(new D4TextEmitter().Emit(FCatalog.EmitStatement(FSystemProcess, EmitMode.ForCopy, false)));
					}
					finally
					{
						LWriter.Close();
					}
				}
			}
			catch (Exception E)
			{
				LogError(E);
				throw;
			}
			
			if (LException != null)
				throw LException;
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
		public string ScriptCatalog(ServerProcess AProcess)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitStatement(AProcess, EmitMode.ForCopy, false));
		}
		
		public string ScriptLibrary(ServerProcess AProcess, string ALibraryName)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitStatement(AProcess, EmitMode.ForCopy, ALibraryName, false));
		}
		
		public string ScriptDropCatalog(ServerProcess AProcess)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitDropStatement(AProcess));
		}
		
		public string ScriptDropLibrary(ServerProcess AProcess, string ALibraryName)
		{
			return new D4TextEmitter().Emit(FCatalog.EmitDropStatement(AProcess, new string[] {}, ALibraryName, false, false, true, true));
		}
		
		public string ScriptLibraryChanges(string AOldCatalogDirectory, string ALibraryName)
		{
			DAE.Server.Server LServer = new DAE.Server.Server();
			try
			{
				LServer.CatalogDirectory = AOldCatalogDirectory;
				LServer.LoggingEnabled = false;
				LServer.Start();

				Schema.SchemaComparer LComparer = new Schema.SchemaComparer(LServer, this, ALibraryName);
				return new D4TextEmitter().Emit(LComparer.EmitChanges());
			}
			finally
			{
				LServer.CatalogDirectory = String.Empty;	// do not save the catalog
				LServer.Dispose();
			}
		}
		
		public void RunScript(string AScript)
		{
			RunScript(FSystemProcess, AScript, String.Empty);
		}
		
		/// <summary> Runs the given script as the specified library. </summary>
		/// <remarks> LibraryName may be the empty string. </remarks>
		public void RunScript(string AScript, string ALibraryName)
		{
			RunScript(FSystemProcess, AScript, ALibraryName);
		}
		
		public void RunScript(ServerProcess AProcess, string AScript)
		{
			RunScript(AProcess, AScript, String.Empty);
		}
		
		public void RunScript(ServerProcess AProcess, string AScript, string ALibraryName)
		{
			if (ALibraryName != String.Empty)
				AProcess.ServerSession.CurrentLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(ALibraryName);
			IServerScript LScript = ((IServerProcess)AProcess).PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareScript(LScript);
			}
		}

		// Indicates that the server is in the process of loading catalog
		private bool FLoadingCatalog = false;
		public bool LoadingCatalog 
		{ 
			get { return FLoadingCatalog; } 
			set { FLoadingCatalog = value; } 
		}
		
		// Indicates that the server has successfully loaded the catalog.
		private bool FCatalogLoaded = false;
		public bool CatalogLoaded { get { return FCatalogLoaded; } }
		
		private string FLoadingCatalogDirectory = String.Empty;
		public string LoadingCatalogDirectory 
		{ 
			get { return FLoadingCatalogDirectory; }
			set { FLoadingCatalogDirectory = value == null ? String.Empty : value; }
		}
		
		// Indicates that the server is in the process of loading the full catalog
		private bool FLoadingFullCatalog = false;
		public bool LoadingFullCatalog
		{
			get { return FLoadingFullCatalog; }
			set { FLoadingFullCatalog = value; }
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
				RunScript(FSystemProcess, "insert table { row { } } into TableDee;", String.Empty);
				
				// Ensure all loaded libraries are loaded
				FSystemProcess.CatalogDeviceSession.ResolveLoadedLibraries();
			}
		}
		
		/// <summary> Loads the system catalog from the given directory. </summary>
		/// <remarks>
		///		Deserializes the catalog from the files in the directory identified by
		///		<see cref="CatalogDirectory"/>. If CatalogDirectory is empty
		///		nothing happens.
		/// </remarks>
		public void LoadCatalog(string ADirectoryName)
		{
			if (!Path.IsPathRooted(ADirectoryName)) // if a relative path
				ADirectoryName = Path.Combine(PathUtility.GetBinDirectory(), ADirectoryName); // Prepend startup path of executable.
			
			string LFileName = Path.Combine(ADirectoryName, GetLibraryCatalogFileName(CSystemLibraryName));
			if (Directory.Exists(ADirectoryName) && File.Exists(LFileName))
			{
				FLoadingCatalog = true;
				FLoadingFullCatalog = true;
				FLoadingCatalogDirectory = ADirectoryName;
				try
				{
					using (FileStream LCatalogStream = new FileStream(LFileName, FileMode.Open, FileAccess.Read))
					{
						using (StreamReader LReader = new StreamReader(LCatalogStream))
						{
							RunScript(LReader.ReadToEnd(), String.Empty);
						}
					}
				}
				finally
				{
					FLoadingCatalogDirectory = String.Empty;
					FLoadingFullCatalog = false;
					FLoadingCatalog = false;
				}
			}
		}
		
		public static string GetLibraryCatalogFileName(string ALibraryName)
		{
			return String.Format("{0}.d4c", ALibraryName);
		}
		
		public void LoadCatalog()
		{
			if ((FCatalogDirectory != null) && (FCatalogDirectory != String.Empty))
				LoadCatalog(FCatalogDirectory);
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

		public void LoadLibrary(string ALibraryName)
		{
			bool LSaveLoadingCatalog = FLoadingCatalog;
			try
			{
				if (!FLoadingCatalog)
					FLoadingCatalog = true;
					
				bool LLoadingCatalogDirectorySet = false;
				if (FLoadingCatalogDirectory == String.Empty)
				{
					LLoadingCatalogDirectorySet = true;
					FLoadingCatalogDirectory = FCatalogDirectory;
				}
				try
				{
					Schema.LoadedLibrary LSaveCurrentLibrary = FSystemSession.CurrentLibrary;
					try
					{
						SystemLoadLibraryNode.LoadLibrary(FSystemProcess, ALibraryName);
					}
					finally
					{
						FSystemSession.CurrentLibrary = LSaveCurrentLibrary;
					}
				}
				finally
				{
					if (LLoadingCatalogDirectorySet)
						FLoadingCatalogDirectory = String.Empty;
				}
			}
			finally
			{
				FLoadingCatalog = LSaveLoadingCatalog;
			}
		}
		
		public void UnloadLibrary(ServerProcess AProcess, string ALibraryName)
		{
			bool LSaveLoadingCatalog = FLoadingCatalog;
			FLoadingCatalog = true;
			try
			{
				SystemUnregisterLibraryNode.UnregisterLibrary(AProcess, ALibraryName, false);
			}
			finally
			{
				FLoadingCatalog = LSaveLoadingCatalog;
			}	   
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
					Schema.Library.GetAvailableLibraries(FLibraryDirectory, FCatalog.Libraries);

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
					CatalogCaches.GatherDefaultCachedObjects(FSystemProcess.CatalogDeviceSession.GetBaseCatalogObjects());
				
					if (FFirstRun)
					{
						LogMessage("Registering system catalog...");
						using (Stream LStream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.DAE.Schema.SystemCatalog.d4"))
						{
							RunScript(new StreamReader(LStream).ReadToEnd(), CSystemLibraryName);
						}
						LogMessage("System catalog registered.");
					}
				}
			}
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
	
	[Transaction(TransactionOption.RequiresNew)]
	public class ServerDTCTransaction : ServicedComponent
	{
		protected override void Dispose(bool ADisposing)
		{
			Rollback();
			base.Dispose(ADisposing);
		}
		
		public void Commit()
		{
			ContextUtil.SetComplete();
		}
		
		public void Rollback()
		{
			ContextUtil.SetAbort();
		}
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel
		{
			get { return FIsolationLevel; }
			set { FIsolationLevel = value; }
		}
	}	

	public class ServerTransaction : Disposable
	{
		public ServerTransaction(ServerProcess AProcess, IsolationLevel AIsolationLevel) : base()
		{
			FProcess = AProcess;
			FIsolationLevel = AIsolationLevel;
			FStartTime = DateTime.Now;
		}
		
		protected void UnprepareDeferredHandlers()
		{
			for (int LIndex = FHandlers.Count - 1; LIndex >= 0; LIndex--)
			{
				FHandlers[LIndex].Deallocate(Process);
				FHandlers.RemoveAt(LIndex);
			}
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if (FHandlers != null)
					{
						UnprepareDeferredHandlers();
						FHandlers = null;
					}
				}
				finally
				{
					if (FCatalogConstraints != null)
					{
						FCatalogConstraints.Clear();
						FCatalogConstraints = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		private ServerProcess FProcess;
		public ServerProcess Process { get { return FProcess; } }
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel { get { return FIsolationLevel; } }

		private DateTime FStartTime;
		public DateTime StartTime { get { return FStartTime; } }

		private bool FPrepared;
		public bool Prepared 
		{
			get { return FPrepared; } 
			set { FPrepared = value; } 
		}
		
		private bool FInRollback;
		public bool InRollback
		{
			get { return FInRollback; }
			set { FInRollback = value; }
		}
		
		private ServerTableVars FTableVars = new ServerTableVars();
		public ServerTableVars TableVars { get { return FTableVars; } }

		public void InvokeDeferredHandlers(ServerProcess AProcess)
		{
			if (FHandlers.Count > 0)
			{
				AProcess.InternalBeginTransaction(IsolationLevel.Isolated);
				try
				{
					foreach (ServerHandler LHandler in FHandlers)
						LHandler.Invoke(AProcess);
					AProcess.InternalCommitTransaction();
				}
				catch (Exception LException)
				{
					if (AProcess.InTransaction)
					{
						try
						{
							AProcess.InternalRollbackTransaction();
						}
						catch (Exception LRollbackException)
						{
							throw new ServerException(ServerException.Codes.RollbackError, LException, LRollbackException.ToString());
						}
					}
					throw LException;
				}
			}
		}
		
		private Schema.CatalogConstraints FCatalogConstraints = new Schema.CatalogConstraints();
		public Schema.CatalogConstraints CatalogConstraints { get { return FCatalogConstraints; } }
		
		public void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			int LIndex = FCatalogConstraints.IndexOf(AConstraint.Name);
			if (LIndex >= 0)
				FCatalogConstraints.RemoveAt(LIndex);
		}
		
		private ServerHandlers FHandlers = new ServerHandlers();
		
		public void AddInsertHandler(Schema.TableVarEventHandler AHandler, Row ARow)
		{
			FHandlers.Add(new ServerInsertHandler(AHandler, ARow));
		}
		
		public void AddUpdateHandler(Schema.TableVarEventHandler AHandler, Row AOldRow, Row ANewRow)
		{
			FHandlers.Add(new ServerUpdateHandler(AHandler, AOldRow, ANewRow));
		}
		
		public void AddDeleteHandler(Schema.TableVarEventHandler AHandler, Row ARow)
		{
			FHandlers.Add(new ServerDeleteHandler(AHandler, ARow));
		}
		
		public void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			for (int LIndex = FHandlers.Count - 1; LIndex >= 0; LIndex--)
				if (FHandlers[LIndex].Handler.Equals(AHandler))
					FHandlers.RemoveAt(LIndex);
		}
	}
	
	public class ServerTransactions : DisposableTypedList
	{
		public ServerTransactions() : base(typeof(ServerTransaction), true, false){}
		
		public void UnprepareDeferredConstraintChecks()
		{
			if (FTableVars != null)
			{
				Exception LException = null;
				while (FTableVars.Count > 0)
					try
					{
						foreach (Schema.TableVar LTableVar in FTableVars.Keys)
						{
							RemoveDeferredConstraintChecks(LTableVar);
							break;
						}
					}
					catch (Exception E)
					{
						LException = E;
					}
					
				FTableVars = null;
				if (LException != null)
					throw LException;
			}
		}
		
		public new ServerTransaction this[int AIndex]
		{
			get { return (ServerTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public ServerTransaction BeginTransaction(ServerProcess AProcess, IsolationLevel AIsolationLevel)
		{
			return this[Add(new ServerTransaction(AProcess, AIsolationLevel))];
		}
		
		public void EndTransaction(bool ASuccess)
		{
			// On successful transaction commit, remove constraint checks that have been completed.
			// On failure, constraint checks that were logged by the transaction will be rolled back with the rest of the transaction.
			if (ASuccess)
				foreach (ServerTableVar LTableVar in this[Count - 1].TableVars.Values)
					LTableVar.DeleteCheckTableChecks(Count - 1);
			RemoveAt(Count - 1);
		}
		
		public ServerTransaction CurrentTransaction()
		{
			return this[Count - 1];
		}
		
		public ServerTransaction RootTransaction()
		{
			return this[0];
		}

		public void ValidateDeferredConstraints(ServerProcess AProcess)
		{
			foreach (ServerTableVar LTableVar in this[Count - 1].TableVars.Values)
				LTableVar.Validate(AProcess, Count - 1);
		}
		
		private ServerTableVars FTableVars = new ServerTableVars();
		public ServerTableVars TableVars { get { return FTableVars; } }
		
		private ServerTableVar EnsureServerTableVar(Schema.TableVar ATableVar)
		{
			ServerTableVar LServerTableVar = null;
			ServerTransaction LCurrentTransaction = CurrentTransaction();

			if (!FTableVars.Contains(ATableVar))
			{
				LServerTableVar = new ServerTableVar(LCurrentTransaction.Process, ATableVar);
				FTableVars.Add(ATableVar, LServerTableVar);
			}
			else
				LServerTableVar = FTableVars[ATableVar];

			if (!LCurrentTransaction.TableVars.Contains(ATableVar))
				LCurrentTransaction.TableVars.Add(ATableVar, LServerTableVar);
				
			return LServerTableVar;
		}

		public void AddInsertTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			EnsureServerTableVar(ATableVar).AddInsertTableVarCheck(Count - 1, ARow);
		}
		
		public void AddUpdateTableVarCheck(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			EnsureServerTableVar(ATableVar).AddUpdateTableVarCheck(Count - 1, AOldRow, ANewRow);
		}
		
		public void AddDeleteTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			EnsureServerTableVar(ATableVar).AddDeleteTableVarCheck(Count - 1, ARow);
		}
		
		public void RemoveDeferredConstraintChecks(Schema.TableVar ATableVar)
		{
			if (FTableVars != null)
			{
				ServerTableVar LTableVar = FTableVars[ATableVar];
				if (LTableVar != null)
				{
					foreach (ServerTransaction LTransaction in this)
						if (LTransaction.TableVars.Contains(ATableVar))
							LTransaction.TableVars.Remove(ATableVar);
					FTableVars.Remove(ATableVar);
					LTableVar.Dispose();
				}
			}
		}
		
		public void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			foreach (ServerTransaction LTransaction in this)
				LTransaction.RemoveDeferredHandlers(AHandler);
		}
		
		public void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			foreach (ServerTransaction LTransaction in this)
				LTransaction.RemoveCatalogConstraintCheck(AConstraint);
		}
	}
	
	public class ServerTableVar : Disposable
	{
		static ServerTableVar()
		{
			FTransactionIndexColumnName = Schema.Object.GetUniqueName();
			FTransitionColumnName = Schema.Object.GetUniqueName();
			FOldRowColumnName = Schema.Object.GetUniqueName();
			FNewRowColumnName = Schema.Object.GetUniqueName();
		}
		
		public ServerTableVar(ServerProcess AProcess, Schema.TableVar ATableVar) : base()
		{
			FProcess = AProcess;
			FTableVar = ATableVar;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				UnprepareCheckTable();
			}
			finally
			{
				FTableVar = null;
				base.Dispose(ADisposing);
			}
		}
		
		protected ServerProcess FProcess;
		public ServerProcess Process { get { return FProcess; } }
		
		protected Schema.TableVar FTableVar;
		public Schema.TableVar TableVar { get { return FTableVar; } }
		
		public override bool Equals(object AObject)
		{
			return (AObject is ServerTableVar) && Schema.Object.NamesEqual(FTableVar.Name, ((ServerTableVar)AObject).TableVar.Name);
		}

		public override int GetHashCode()
		{
			return FTableVar.GetHashCode();
		}

		private string FCheckTableName;
		private static string FTransactionIndexColumnName;
		private static string FTransitionColumnName;
		private static string FOldRowColumnName;
		private static string FNewRowColumnName;
		private Schema.TableVar FCheckTable;
		private Schema.Key FCheckTableKey;
		private Schema.RowType FCheckRowType;

		private Schema.IRowType FNewRowType;
		public Schema.IRowType NewRowType { get { return FNewRowType; } }

		private Schema.IRowType FOldRowType;
		public Schema.IRowType OldRowType { get { return FOldRowType; } }

		private IServerExpressionPlan FPlan;
		private IServerExpressionPlan FTransactionPlan;
		private DataParams FTransactionParams;
		private DataParam FTransactionParam;

		private void CreateCheckTable()
		{
			FCheckTableName = Schema.Object.GetUniqueName();

			/*
				create session table <check table name> in System.Temp
				{ 
					<key columns>, 
					<transaction index column name> : Integer,
					<transition column name> : String static tags { DAE.StaticByteSize = '16' }, 
					<old row column name> : row { nil }, 
					<new row column name> : row { nil }, 
					key { <key column names> },
					order { <transaction index column name>, <key columns> }
				};
			*/

			StringBuilder LBuilder = new StringBuilder();
			LBuilder.AppendFormat("{0} {1} {2} {3} {4} {5} {{ ", Keywords.Create, Keywords.Session, Keywords.Table, FCheckTableName, Keywords.In, Process.ServerSession.Server.TempDevice.Name);
			bool LHasColumns = false;
			Schema.Key LKey = FTableVar.Keys.MinimumKey(false, false);
			for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
			{
				if (LHasColumns)
					LBuilder.AppendFormat("{0} ", Keywords.ListSeparator);
				else
					LHasColumns = true;
				LBuilder.AppendFormat("{0} {1} {2}", LKey.Columns[LIndex].Name, Keywords.TypeSpecifier, LKey.Columns[LIndex].Column.DataType.Name);
			}
			if (LHasColumns)
				LBuilder.AppendFormat("{0} ", Keywords.ListSeparator);
			LBuilder.AppendFormat("{0} : Integer, ", FTransactionIndexColumnName);
			LBuilder.AppendFormat("{0} : String static tags {{ DAE.StaticByteSize = '16' }}, ", FTransitionColumnName);
			LBuilder.AppendFormat("{0} : generic row {{ nil }}, ", FOldRowColumnName);
			LBuilder.AppendFormat("{0} : generic row {{ nil }}, key {{ ", FNewRowColumnName);
			for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LBuilder.AppendFormat("{0} ", Keywords.ListSeparator);
				LBuilder.Append(LKey.Columns[LIndex].Name);
			}
			LBuilder.Append(" }, order { ");
			LBuilder.Append(FTransactionIndexColumnName);
			for (int LIndex = 0; LIndex < LKey.Columns.Count; LIndex++)
				LBuilder.AppendFormat("{0} {1}", Keywords.ListSeparator, LKey.Columns[LIndex].Name);
			LBuilder.Append("} };");

			ApplicationTransaction LTransaction = null;
			if (Process.ApplicationTransactionID != Guid.Empty)
				LTransaction = Process.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					bool LSaveUsePlanCache = Process.ServerSession.SessionInfo.UsePlanCache;
					Process.ServerSession.SessionInfo.UsePlanCache = false;
					try
					{
						// Push a loading context to prevent the DDL from begin logged.
						Process.PushLoadingContext(new LoadingContext(Process.ServerSession.User, true));
						try
						{
							IServerStatementPlan LPlan = ((IServerProcess)Process).PrepareStatement(LBuilder.ToString(), null);
							try
							{
								LPlan.Execute(null);
							}
							finally
							{
								((IServerProcess)Process).UnprepareStatement(LPlan);
							}
						}
						finally
						{
							Process.PopLoadingContext();
						}
					}
					finally
					{
						Process.ServerSession.SessionInfo.UsePlanCache = LSaveUsePlanCache;
					}
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
			
			FCheckTable = Process.Plan.Catalog[((Schema.SessionObject)Process.ServerSession.SessionObjects[FCheckTableName]).GlobalName] as Schema.TableVar;
			FCheckTableKey = FCheckTable.Keys.MinimumKey(true);
			FCheckRowType = new Schema.RowType(FCheckTable.DataType.Columns);
		}
		
		private void CopyKeyValues(Row ASourceRow, Row ATargetRow)
		{
			int LColumnIndex;
			for (int LIndex = 0; LIndex < FCheckTableKey.Columns.Count; LIndex++)
			{
				LColumnIndex = ASourceRow.IndexOfColumn(FCheckTableKey.Columns[LIndex].Name);
				if (ASourceRow.HasValue(LColumnIndex))
					ATargetRow[LIndex] = ASourceRow[LColumnIndex];
				else
					ATargetRow.ClearValue(LIndex);
			}
		}

		private void DeleteCheckTableChecks()
		{
			if (FPlan != null)
			{
				IServerCursor LCursor = FPlan.Open(null);
				try
				{
					LCursor.Next();
					Row LRow = FPlan.RequestRow();
					try
					{
						while (!LCursor.EOF())
						{
							LCursor.Select(LRow);
							if (LRow.HasValue(FOldRowColumnName))
								DataValue.DisposeNative(Process, OldRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FOldRowColumnName)]);
							if (LRow.HasValue(FNewRowColumnName))
								DataValue.DisposeNative(Process, NewRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FNewRowColumnName)]);
							LCursor.Delete();
						}
					}
					finally
					{
						FPlan.ReleaseRow(LRow);
					}
				}
				finally
				{
					FPlan.Close(LCursor);
				}
			}
		}
		
		public void DeleteCheckTableChecks(int ATransactionIndex)
		{
			IServerCursor LCursor = FTransactionPlan.Open(GetTransactionParams(ATransactionIndex));
			try
			{
				LCursor.Next();
				Row LRow = FTransactionPlan.RequestRow();
				try
				{
					while (!LCursor.EOF())
					{
						LCursor.Select(LRow);
						if (LRow.HasValue(FOldRowColumnName))
							DataValue.DisposeNative(Process, OldRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FOldRowColumnName)]);
						if (LRow.HasValue(FNewRowColumnName))
							DataValue.DisposeNative(Process, NewRowType, ((NativeRow)LRow.AsNative).Values[LRow.DataType.Columns.IndexOfName(FNewRowColumnName)]);
						LCursor.Delete();
					}
				}
				finally
				{
					FTransactionPlan.ReleaseRow(LRow);
				}
			}
			finally
			{
				FTransactionPlan.Close(LCursor);
			}
		}
		
		private DataParams GetTransactionParams(int ATransactionIndex)
		{
			FTransactionParam.Value = new Scalar(Process, Process.DataTypes.SystemInteger, ATransactionIndex);
			return FTransactionParams;
		}
		
		private void OpenCheckTable()
		{
			ApplicationTransaction LTransaction = null;
			if (Process.ApplicationTransactionID != Guid.Empty)
				LTransaction = Process.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushGlobalContext();
				try
				{
					bool LSaveUsePlanCache = Process.ServerSession.SessionInfo.UsePlanCache;
					Process.ServerSession.SessionInfo.UsePlanCache = false;
					try
					{
						Process.Context.PushWindow(0);
						try
						{
							FPlan = ((IServerProcess)Process).PrepareExpression(String.Format("select {0} capabilities {{ navigable, searchable, updateable }} type dynamic;", FCheckTableName), null);
							FTransactionParams = new DataParams();
							FTransactionParam = new DataParam("ATransactionIndex", Process.DataTypes.SystemInteger, Modifier.In, new Scalar(Process, Process.DataTypes.SystemInteger, 0));
							FTransactionParams.Add(FTransactionParam);
							FTransactionPlan = ((IServerProcess)Process).PrepareExpression(String.Format("select {0} where {1} = ATransactionIndex capabilities {{ navigable, searchable, updateable }} type dynamic;", FCheckTableName, FTransactionIndexColumnName), FTransactionParams);
						}
						finally
						{
							Process.Context.PopWindow();
						}
					}
					finally
					{
						Process.ServerSession.SessionInfo.UsePlanCache = LSaveUsePlanCache;
					}
				}
				finally
				{
					if (LTransaction != null)
						LTransaction.PopGlobalContext();
				}
			}
			finally
			{
				if (LTransaction != null)
					Monitor.Exit(LTransaction);
			}
		}
		
		private void CloseCheckTable()
		{
			if (FPlan != null)
			{
				((IServerProcess)Process).UnprepareExpression(FPlan);
				FPlan = null;
			}
			
			if (FTransactionPlan != null)
			{
				((IServerProcess)Process).UnprepareExpression(FTransactionPlan);
				FTransactionPlan = null;
			}
		}
		
		private void DropCheckTable()
		{
			if (FCheckTable != null)
			{
				ApplicationTransaction LTransaction = null;
				if (Process.ApplicationTransactionID != Guid.Empty)
					LTransaction = Process.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushGlobalContext();
					try
					{
						ServerStatementPlan LPlan = new ServerStatementPlan(Process);
						try
						{
							LPlan.Plan.EnterTimeStampSafeContext();
							try
							{
								// Push a loading context to prevent the DDL from being logged.
								Process.PushLoadingContext(new LoadingContext(Process.ServerSession.User, true));
								try
								{
									Process.PushExecutingPlan(LPlan);
									try
									{
										PlanNode LPlanNode = Compiler.BindNode(LPlan.Plan, Compiler.CompileStatement(LPlan.Plan, new DropTableStatement(FCheckTableName)));
										LPlan.Plan.CheckCompiled();
										LPlanNode.Execute(Process);
									}
									finally
									{
										Process.PopExecutingPlan(LPlan);
									}
								}
								finally
								{
									Process.PopLoadingContext();
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
					finally
					{
						if (LTransaction != null)
							LTransaction.PopGlobalContext();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
		}
		
		private void PrepareCheckTable()
		{
			if (FCheckTable == null)
			{
				try
				{
					CreateCheckTable();
					OpenCheckTable();
				}
				catch (Exception E)
				{
					DataphorException LDataphorException = E as DataphorException;
					throw new ServerException(ServerException.Codes.CouldNotCreateCheckTable, LDataphorException != null ? LDataphorException.Severity : ErrorSeverity.System, E, FTableVar == null ? "<unknown>" : FTableVar.DisplayName);
				}
			}
		}
		
		private void UnprepareCheckTable()
		{
			DeleteCheckTableChecks();
			CloseCheckTable();
			DropCheckTable();
		}
		
		public void AddInsertTableVarCheck(int ATransactionIndex, Row ARow)
		{
			PrepareCheckTable();
			if (FNewRowType == null)
				FNewRowType = ARow.DataType;

			// Log ARow as an insert transition.
			Row LCheckRow = FPlan.RequestRow();
			try
			{
				IServerCursor LCursor = FPlan.Open(null);
				try
				{
					CopyKeyValues(ARow, LCheckRow);
					LCheckRow[FTransactionIndexColumnName] = new Scalar(Process, Process.DataTypes.SystemInteger, ATransactionIndex);
					LCheckRow[FTransitionColumnName] = new Scalar(Process, Process.DataTypes.SystemString, Keywords.Insert);
					LCheckRow[FNewRowColumnName] = ARow;

					if (LCursor.FindKey(LCheckRow))
						LCursor.Delete();

					LCursor.Insert(LCheckRow);
				}
				finally
				{
					FPlan.Close(LCursor);
				}
			}
			finally
			{
				FPlan.ReleaseRow(LCheckRow);
			}
		}
		
		public void AddUpdateTableVarCheck(int ATransactionIndex, Row AOldRow, Row ANewRow)
		{
			PrepareCheckTable();
			if (FOldRowType == null)
				FOldRowType = AOldRow.DataType;
			if (FNewRowType == null)
				FNewRowType = ANewRow.DataType;
			IServerCursor LCursor = FPlan.Open(null);
			try
			{
				// If there is an existing insert transition check for AOldRow, delete it and log ANewRow as an insert transition.
				// If there is an existing update transition check for AOldRow, delete it and log AOldRow from the existing check and ANewRow from the new check as an update transition.
				// Else log AOldRow, ANewRow as an update transition.
				Row LCheckRow = FPlan.RequestRow();
				try
				{
					// delete <check table name> where <key names> = <old key values>;
					CopyKeyValues(AOldRow, LCheckRow);
					if (LCursor.FindKey(LCheckRow))
					{
						using (Row LRow = LCursor.Select())
						{
							if (LRow.HasValue(FOldRowColumnName))
								AOldRow = (Row)LRow[FOldRowColumnName];
							else
								AOldRow = null;
						}
						LCursor.Delete();
					}

					CopyKeyValues(ANewRow, LCheckRow);
					LCheckRow[FTransactionIndexColumnName] = new Scalar(Process, Process.DataTypes.SystemInteger, ATransactionIndex);
					if (AOldRow != null)
					{
						LCheckRow[FTransitionColumnName] = new Scalar(Process, Process.DataTypes.SystemString, Keywords.Update);
						LCheckRow[FOldRowColumnName] = AOldRow;
					}
					else
						LCheckRow[FTransitionColumnName] = new Scalar(Process, Process.DataTypes.SystemString, Keywords.Insert);

					LCheckRow[FNewRowColumnName] = ANewRow;
					LCursor.Insert(LCheckRow);
				}
				finally
				{
					FPlan.ReleaseRow(LCheckRow);
				}
			}
			finally
			{
				FPlan.Close(LCursor);
			}
		}

		public void AddDeleteTableVarCheck(int ATransactionIndex, Row ARow)
		{
			PrepareCheckTable();
			if (FOldRowType == null)
				FOldRowType = ARow.DataType;
			IServerCursor LCursor = FPlan.Open(null);
			try
			{
				// If there is an existing insert transition for ARow, delete it and don't log anything.
				// If there is an existing update transition for ARow, delete it and log AOldRow from the update transition as a delete transition.
				// Else log ARow as a delete transition.
				Row LCheckRow = FPlan.RequestRow();
				try
				{
					// delete <check table name> where <key names> = <key values>;
					CopyKeyValues(ARow, LCheckRow);
					if (LCursor.FindKey(LCheckRow))
					{
						using (Row LRow = LCursor.Select())
						{
							if (LRow.HasValue(FOldRowColumnName))
								ARow = (Row)LRow[FOldRowColumnName];
							else
								ARow = null;
						}
						LCursor.Delete();
					}

					if (ARow != null)
					{
						LCheckRow[FTransactionIndexColumnName] = new Scalar(Process, Process.DataTypes.SystemInteger, ATransactionIndex);
						LCheckRow[FTransitionColumnName] = new Scalar(Process, Process.DataTypes.SystemString, Keywords.Delete);
						LCheckRow[FOldRowColumnName] = ARow;
						LCursor.Insert(LCheckRow);
					}
				}
				finally
				{
					FPlan.ReleaseRow(LCheckRow);
				}
			}
			finally
			{
				FPlan.Close(LCursor);
			}
		}
		
		public void Validate(ServerProcess AProcess, int ATransactionIndex)
		{
			// cursor through each check and validate all constraints for each check
			IServerCursor LCursor = FTransactionPlan.Open(GetTransactionParams(ATransactionIndex));
			try
			{
				Row LCheckRow = FTransactionPlan.RequestRow();
				try
				{
					while (LCursor.Next())
					{
						LCursor.Select(LCheckRow);
						switch (LCheckRow[FTransitionColumnName].AsString)
						{
							case Keywords.Insert :
							{
								Row LRow = (Row)LCheckRow[FNewRowColumnName];
								try
								{
									AProcess.Context.Push(new DataVar(LRow.DataType, LRow));
									try
									{
										ValidateCheck(Schema.Transition.Insert);
									}
									finally
									{
										AProcess.Context.Pop();
									}
								}
								finally
								{
									LRow.Dispose();
								}
							}
							break;
							
							case Keywords.Update :
							{
								Row LOldRow = (Row)LCheckRow[FOldRowColumnName];
								try
								{
									AProcess.Context.Push(new DataVar(LOldRow.DataType, LOldRow));
									try
									{
										Row LNewRow = (Row)LCheckRow[FNewRowColumnName];
										try
										{
											AProcess.Context.Push(new DataVar(LNewRow.DataType, LNewRow));
											try
											{
												ValidateCheck(Schema.Transition.Update);
											}
											finally
											{
												AProcess.Context.Pop();
											}
										}
										finally
										{
											LNewRow.Dispose();
										}
									}
									finally
									{
										AProcess.Context.Pop();
									}
								}
								finally
								{
									LOldRow.Dispose();
								}
							}
							break;
							
							case Keywords.Delete :
							{
								Row LRow = (Row)LCheckRow[FOldRowColumnName];
								try
								{
									AProcess.Context.Push(new DataVar(LRow.DataType, LRow));
									try
									{
										ValidateCheck(Schema.Transition.Delete);
									}
									finally
									{
										AProcess.Context.Pop();
									}
								}
								finally
								{
									LRow.Dispose();
								}
							}
							break;
						}
					}
				}
				finally
				{
					FTransactionPlan.ReleaseRow(LCheckRow);
				}
			}
			finally
			{
				FTransactionPlan.Close(LCursor);
			}
		}
		
		public void ValidateCheck(Schema.Transition ATransition)
		{
			switch (ATransition)
			{
				case Schema.Transition.Insert :
				{
					Row LNewRow = new Row(Process, this.TableVar.DataType.RowType, (NativeRow)Process.Context.Peek(0).Value.AsNative);
					try
					{
						Process.Context.Push(new DataVar(LNewRow.DataType, LNewRow));
						try
						{
							foreach (Schema.RowConstraint LConstraint in FTableVar.RowConstraints)
								if (LConstraint.Enforced && LConstraint.IsDeferred)
									LConstraint.Validate(Process, ATransition);
						}
						finally
						{
							Process.Context.Pop();
						}
					}
					finally
					{
						LNewRow.Dispose();
					}

					foreach (Schema.TransitionConstraint LConstraint in FTableVar.InsertConstraints)
						if (LConstraint.Enforced && LConstraint.IsDeferred)
							LConstraint.Validate(Process, Schema.Transition.Insert);
				}
				break;

				case Schema.Transition.Update :
				{
					Row LNewRow = new Row(Process, this.TableVar.DataType.RowType, (NativeRow)Process.Context.Peek(0).Value.AsNative);
					try
					{
						Process.Context.Push(new DataVar(LNewRow.DataType, LNewRow));
						try
						{
							foreach (Schema.RowConstraint LConstraint in FTableVar.RowConstraints)
								if (LConstraint.Enforced && LConstraint.IsDeferred)
									LConstraint.Validate(Process, Schema.Transition.Update);
						}
						finally
						{
							Process.Context.Pop();
						}
					}
					finally
					{
						LNewRow.Dispose();
					}
							
					foreach (Schema.TransitionConstraint LConstraint in FTableVar.UpdateConstraints)
						if (LConstraint.Enforced && LConstraint.IsDeferred)
							LConstraint.Validate(Process, Schema.Transition.Update);
				}
				break;

				case Schema.Transition.Delete :
					foreach (Schema.TransitionConstraint LConstraint in FTableVar.DeleteConstraints)
						if (LConstraint.Enforced && LConstraint.IsDeferred)
							LConstraint.Validate(Process, Schema.Transition.Delete);
				break;
			}
		}
	}
	
	public class ServerTableVars : Hashtable
	{
		public ServerTableVar this[Schema.TableVar ATableVar] { get { return (ServerTableVar)base[ATableVar]; } }
	}
	
	public abstract class ServerHandler : System.Object
	{
		public ServerHandler(Schema.TableVarEventHandler AHandler) : base() 
		{
			FHandler = AHandler;
		}
		
		protected Schema.IRowType FNewRowType;
		protected Schema.IRowType FOldRowType;
		
		protected Schema.TableVarEventHandler FHandler;
		public Schema.TableVarEventHandler Handler { get { return FHandler; } }
		
		public abstract void Invoke(ServerProcess AProcess);
		
		public abstract void Deallocate(ServerProcess AProcess);
	}
	
	public class ServerHandlers : List<ServerHandler>
	{
		public void Deallocate(ServerProcess AProcess)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				this[LIndex].Deallocate(AProcess);
		}
	}
	
	public class ServerInsertHandler : ServerHandler
	{
		public ServerInsertHandler(Schema.TableVarEventHandler AHandler, Row ARow) : base(AHandler)
		{
			FNewRowType = ARow.DataType;
			NativeRow = (NativeRow)ARow.CopyNative();
		}
		
		public NativeRow NativeRow;
		
		public override void Invoke(ServerProcess AProcess)
		{
			Row LRow = new Row(AProcess, FNewRowType, NativeRow);
			try
			{
				AProcess.Context.Push(new DataVar(LRow.DataType, LRow));
				try
				{
					FHandler.PlanNode.Execute(AProcess);
				}
				finally
				{
					AProcess.Context.Pop();
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		public override void Deallocate(ServerProcess AProcess)
		{
			if (NativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FNewRowType, NativeRow);
				NativeRow = null;
			}
		}
	}
	
	public class ServerUpdateHandler : ServerHandler
	{
		public ServerUpdateHandler(Schema.TableVarEventHandler AHandler, Row AOldRow, Row ANewRow) : base(AHandler)
		{
			FNewRowType = ANewRow.DataType;
			FOldRowType = AOldRow.DataType;
			OldNativeRow = (NativeRow)AOldRow.CopyNative();
			NewNativeRow = (NativeRow)ANewRow.CopyNative();
		}
		
		public NativeRow OldNativeRow;
		public NativeRow NewNativeRow;

		public override void Invoke(ServerProcess AProcess)
		{
			Row LOldRow = new Row(AProcess, FOldRowType, OldNativeRow);
			try
			{
				Row LNewRow = new Row(AProcess, FNewRowType, NewNativeRow);
				try
				{
					AProcess.Context.Push(new DataVar(LOldRow.DataType, LOldRow));
					try
					{
						AProcess.Context.Push(new DataVar(LNewRow.DataType, LNewRow));
						try
						{
							FHandler.PlanNode.Execute(AProcess);
						}
						finally
						{
							AProcess.Context.Pop();
						}
					}
					finally
					{
						AProcess.Context.Pop();
					}
				}
				finally
				{
					LNewRow.Dispose();
				}
			}
			finally
			{
				LOldRow.Dispose();
			}
		}
		
		public override void Deallocate(ServerProcess AProcess)
		{
			if (OldNativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FOldRowType, OldNativeRow);
				OldNativeRow = null;
			}

			if (NewNativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FNewRowType, NewNativeRow);
				NewNativeRow = null;
			}
		}
	}
	
	public class ServerDeleteHandler : ServerHandler
	{
		public ServerDeleteHandler(Schema.TableVarEventHandler AHandler, Row ARow) : base(AHandler)
		{
			FOldRowType = ARow.DataType;
			NativeRow = (NativeRow)ARow.CopyNative();
		}
		
		public NativeRow NativeRow;

		public override void Invoke(ServerProcess AProcess)
		{
			Row LRow = new Row(AProcess, FOldRowType, NativeRow);
			try
			{
				AProcess.Context.Push(new DataVar(LRow.DataType, LRow));
				try
				{
					FHandler.PlanNode.Execute(AProcess);
				}
				finally
				{
					AProcess.Context.Pop();
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		public override void Deallocate(ServerProcess AProcess)
		{
			if (NativeRow != null)
			{
				DataValue.DisposeNative(AProcess, FOldRowType, NativeRow);
				NativeRow = null;
			}
		}
	}
	
	public class CachedPlanHeader
	{
		public CachedPlanHeader(string AStatement, string ALibraryName, int AContextHashCode, bool AInApplicationTransaction)
		{
			Statement = AStatement;
			LibraryName = ALibraryName;
			ContextHashCode = AContextHashCode;
			InApplicationTransaction = AInApplicationTransaction;
		}
		
		public string Statement;
		public string LibraryName;
		public int ContextHashCode; // Hash of the names of all types present on the context for the process
		public bool InApplicationTransaction;
		
		/// <summary>This flag will be set to true if the plan results in an error on open, indicating that it is invalid, and should not be returned to the plan cache.</summary>
		public bool IsInvalidPlan;
	
		public override int GetHashCode()
		{
			return Statement.GetHashCode() ^ LibraryName.GetHashCode() ^ ContextHashCode ^ InApplicationTransaction.GetHashCode();
		}
		
		public override bool Equals(object AObject)
		{
			CachedPlanHeader LCachedPlanHeader = AObject as CachedPlanHeader;
			return 
				(LCachedPlanHeader != null) 
					&& (LCachedPlanHeader.Statement == Statement) 
					&& (LCachedPlanHeader.LibraryName == LibraryName) 
					&& (LCachedPlanHeader.ContextHashCode == ContextHashCode) 
					&& (LCachedPlanHeader.InApplicationTransaction == InApplicationTransaction);
		}
	}
	
	public class CachedPlans : List
	{
		public new ServerPlanBase this[int AIndex]
		{
			get { return (ServerPlanBase)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class PlanCache : System.Object
	{
		public PlanCache(int ACacheSize) : base() 
		{
			FSize = ACacheSize;
			if (FSize > 1)
				FPlans = new FixedSizeCache(FSize);
		}
		
		private int FSize;
		public int Size { get { return FSize; } }
		
		private FixedSizeCache FPlans; // FixedSizeCache ( <CachedPlanHeader>, <CachedPlans> )
		
		private void DisposeCachedPlan(ServerProcess AProcess, ServerPlanBase APlan)
		{
			try
			{
				APlan.BindToProcess(AProcess.ServerSession.Server.FSystemProcess);
				APlan.Dispose();
			}
			catch
			{
				// ignore disposal exceptions
			}
		}
		
		private void DisposeCachedPlans(ServerProcess AProcess, CachedPlans APlans)
		{
			foreach (ServerPlanBase LPlan in APlans)
				DisposeCachedPlan(AProcess, LPlan);
		}
		
		private CachedPlanHeader GetPlanHeader(ServerProcess AProcess, string AStatement, int AContextHashCode)
		{
			return new CachedPlanHeader(AStatement, AProcess.Plan.CurrentLibrary.Name, AContextHashCode, AProcess.ApplicationTransactionID != Guid.Empty);
		}

		/// <summary>Gets a cached plan for the given statement, if available.</summary>
		/// <remarks>
		/// If a plan is found, it is referenced for the LRU, and disowned by the cache.
		/// The client must call Release to return the plan to the cache.
		/// If no plan is found, null is returned and the cache is unaffected.
		/// </remarks>
		public ServerPlanBase Get(ServerProcess AProcess, string AStatement, int AContextHashCode)
		{
			ServerPlanBase LPlan = null;
			CachedPlanHeader LHeader = GetPlanHeader(AProcess, AStatement, AContextHashCode);
			CachedPlans LBumped = null;
			lock (this)
			{
				if (FPlans != null)
				{
					CachedPlans LPlans = FPlans[LHeader] as CachedPlans;
					if (LPlans != null)
					{
						for (int LPlanIndex = LPlans.Count - 1; LPlanIndex >= 0; LPlanIndex--)
						{
							LPlan = LPlans[LPlanIndex];
							LPlans.RemoveAt(LPlanIndex);
							if (AProcess.Plan.Catalog.PlanCacheTimeStamp > LPlan.PlanCacheTimeStamp)
							{
								DisposeCachedPlan(AProcess, LPlan);
								LPlan = null;
							}
							else
							{
								LBumped = (CachedPlans)FPlans.Reference(LHeader, LPlans);
								break;
							}
						}
					}
				}
			}
			
			if (LBumped != null)
				DisposeCachedPlans(AProcess, LBumped);

			if (LPlan != null)
				LPlan.BindToProcess(AProcess);

			return LPlan;
		}

		/// <summary>Adds the given plan to the plan cache.</summary>		
		/// <remarks>
		/// The plan is not contained within the cache after this call, it is assumed in use by the client.
		/// This call simply reserves storage and marks the plan as referenced for the LRU.
		/// </remarks>
		public void Add(ServerProcess AProcess, string AStatement, int AContextHashCode, ServerPlanBase APlan)
		{
			CachedPlans LBumped = null;
			CachedPlanHeader LHeader = GetPlanHeader(AProcess, AStatement, AContextHashCode);
			APlan.Header = LHeader;
			APlan.PlanCacheTimeStamp = AProcess.Plan.Catalog.PlanCacheTimeStamp;

			lock (this)
			{
				if (FPlans != null)
				{
					CachedPlans LPlans = FPlans[LHeader] as CachedPlans;
					if (LPlans == null)
						LPlans = new CachedPlans();
					LBumped = (CachedPlans)FPlans.Reference(LHeader, LPlans);
				}
			}
			
			if (LBumped != null)
				DisposeCachedPlans(AProcess, LBumped);
		}
		
		/// <summary>Releases the given plan and returns whether or not it was returned to the cache.</summary>
		/// <remarks>
		/// If the plan is returned to the cache, the client is no longer responsible for the plan, it is owned by the cache.
		/// If the plan is not returned to the cache, the cache client is responsible for disposing the plan.
		///	</remarks>
		public bool Release(ServerProcess AProcess, ServerPlanBase APlan)
		{
			CachedPlanHeader LHeader = APlan.Header;

			lock (this)
			{
				if (FPlans != null)
				{
					CachedPlans LPlans = FPlans[LHeader] as CachedPlans;
					if (LPlans != null)
					{
						LPlans.Add(APlan);
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>Clears the plan cache, disposing any plans it contains.</summary>		
		public void Clear(ServerProcess AProcess)
		{
			lock (this)
			{
				if (FPlans != null)
				{
					foreach (DictionaryEntry LEntry in FPlans)
						DisposeCachedPlans(AProcess, LEntry.Value as CachedPlans);
					
					FPlans.Clear();
				}
			}
		}

		/// <summary>Resizes the cache to the specified size.</summary>
		/// <remarks>
		/// Resizing the cache has the effect of clearing the entire cache.
		/// </remarks>
		public void Resize(ServerProcess AProcess, int ASize)
		{
			lock (this)
			{
				if (FPlans != null)
				{
					Clear(AProcess);
					FPlans = null;
				}
				
				FSize = ASize;
				if (FSize > 1)
					FPlans = new FixedSizeCache(FSize);
			}
		}
	}
	
	// ServerConnection
	public class ServerConnection : ServerChildObject, IRemoteServerConnection
	{
		internal ServerConnection(Server AServer, string AConnectionName, string AHostName)
		{
			FServer = AServer;
			FHostName = AHostName;
			FConnectionName = AConnectionName;
			FSessions = new ServerSessions(false);

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FServer.FConnectionCounter != null)
				FServer.FConnectionCounter.Increment();
			#endif
		}
		
		protected bool FDisposed;
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					FSessions.Dispose();
					FSessions = null;
				}
				finally
				{
					FServer.CatalogCaches.RemoveCache(FConnectionName);
				}

				if (!FDisposed)
				{
					#if !DISABLE_PERFORMANCE_COUNTERS
					if (FServer.FConnectionCounter != null)
						FServer.FConnectionCounter.Decrement();
					#endif
				}
			}
			finally
			{
				FServer = null;
				FDisposed = true;
				base.Dispose(ADisposing);
			}
		}

		// Server
		private Server FServer;
		public Server Server { get { return FServer; } }
		
		// Sessions
		private ServerSessions FSessions;
		public ServerSessions Sessions { get { return FSessions; } }
		
		// ConnectionName
		private string FConnectionName;
		public string ConnectionName { get { return FConnectionName; } }
		
		// HostName
		private string FHostName;
		public string HostName { get { return FHostName; } }
		
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
		
		// IRemoteServer.Connect
		IRemoteServerSession IRemoteServerConnection.Connect(SessionInfo ASessionInfo)
		{
			BeginCall();
			try
			{
				ServerSession LSession = FServer.RemoteConnect(ASessionInfo);
				FSessions.Add(LSession);
				return LSession;
			}
			finally
			{
				EndCall();
			}
		}
        
		// IRemoteServer.Disconnect
		void IRemoteServerConnection.Disconnect(IRemoteServerSession ASession)
		{
			BeginCall();
			try
			{
				FServer.RemoteDisconnect((ServerSession)ASession);
			}
			finally
			{
				EndCall();
			}
		}
		
		internal void CloseSessions()
		{
			if (FSessions != null)
			{
				while (FSessions.Count > 0)
				{
					try
					{
						FServer.RemoteDisconnect((ServerSession)FSessions.DisownAt(0));
					}
					catch (Exception E)
					{
						if (FServer != null)
							FServer.LogError(E);
					}
				}
			}
		}
        
		#region Lifetime Management

		/*
			Strategy: The client has a ping thread that will execute every couple minutes, so long as
			 the server's initial and renewal time is longer than the client's ping time, no sponsorhip
			 mechanism is necessary; the client ensures that the server recognizes that it is responsive.
		*/

		public const int CInitialLeaseTimeSeconds = 300;	// 5 Minutes... must be longer than the client side ping
		public const int CRenewOnCallTimeSeconds = 300;

		public override object InitializeLifetimeService()
		{
			ILease LLease = (ILease)base.InitializeLifetimeService();
			if (LLease.CurrentState == LeaseState.Initial)
			{
				LLease.InitialLeaseTime = TimeSpan.FromSeconds(CInitialLeaseTimeSeconds);
				LLease.SponsorshipTimeout = TimeSpan.Zero;
				LLease.RenewOnCallTime = TimeSpan.FromSeconds(CRenewOnCallTimeSeconds);
			}
			return LLease;
		}

		/// <remarks> Provides a way to check that the session is still available (the message will reset the server's keep alive timeout). </remarks>
		public void Ping()
		{
			// intentionally left blank
		}

		#endregion
	}
	
	// ServerConnections
	public class ServerConnections : ServerChildObjects
	{		
		public ServerConnections() : base() {}
		public ServerConnections(bool AIsOwner) : base(AIsOwner) {}
		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerConnection))
				throw new ServerException(ServerException.Codes.ServerConnectionContainer);
		}
		
		public new ServerConnection this[int AIndex]
		{
			get { return (ServerConnection)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
	}
    
	public class ServerSession : ServerChildObject, IServerSession, IRemoteServerSession
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
								if (FApplicationTransactions != null)
								{
									EndApplicationTransactions();
									FApplicationTransactions = null;
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

						#if OnExpression
						if (FRemoteSessions != null)
						{
							FRemoteSessions.Dispose();
							FRemoteSessions = null;
						}
						#endif
					}
				}
				finally
				{
					if ((FSessionInfo != null) && (FSessionInfo.CatalogCacheName != String.Empty) && (FServer != null) && (FServer.CatalogCaches != null))
						FServer.CatalogCaches.RemoveSession(this);
						
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
        
		// SessionID
		private int FSessionID = -1;
		public int SessionID  { get { return FSessionID; } }
        
		// IServerSession.Server
		IServer IServerSession.Server { get { return (IServer)FServer; } }
        
		// IRemoteServerSession.Server
		IRemoteServer IRemoteServerSession.Server { get { return (IRemoteServer)FServer; } }
        
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
		public void AddCachedPlan(ServerProcess AProcess, string AStatement, int AContextHashCode, ServerPlanBase APlan)
		{
			if (FSessionInfo.UsePlanCache && (Server.PlanCache != null) && !HasSessionObjects())
				Server.PlanCache.Add(AProcess, AStatement, AContextHashCode, APlan);
		}
		
		public ServerPlanBase GetCachedPlan(ServerProcess AProcess, string AStatement, int AContextHashCode)
		{
			if (FSessionInfo.UsePlanCache && (Server.PlanCache != null) && !HasSessionObjects())
				return Server.PlanCache.Get(AProcess, AStatement, AContextHashCode);
			return null;
		}
		
		public bool ReleaseCachedPlan(ServerProcess AProcess, ServerPlanBase APlan)
		{
			if (FSessionInfo.UsePlanCache && (Server.PlanCache != null) && (APlan.Header != null) && (!APlan.Header.IsInvalidPlan) && !HasSessionObjects())
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
					
				Block LBlock = (Block)FServer.Catalog.EmitDropStatement(FServer.FSystemProcess, LObjectNameArray, String.Empty);
				
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
		
		#if OnExpression
		// RemoteSessions
		private RemoteSessions FRemoteSessions;
		internal RemoteSessions RemoteSessions
		{
			get 
			{ 
				if (FRemoteSessions == null)
					FRemoteSessions = new RemoteSessions();
				return FRemoteSessions; 
			}
		}
		
		internal RemoteSession RemoteConnect(Schema.ServerLink AServerLink)
		{
			int LIndex = RemoteSessions.IndexOf(AServerLink);
			if (LIndex < 0)
			{
				RemoteSession LSession = new RemoteSession(this, AServerLink);
				try
				{
					RemoteSessions.Add(LSession);
					return LSession;
				}
				catch
				{
					LSession.Dispose();
					throw;
				}
			}
			else
				return RemoteSessions[LIndex];
		}
		
		internal void RemoteDisconnect(Schema.ServerLink AServerLink)
		{
			int LIndex = RemoteSessions.IndexOf(AServerLink);
			if (LIndex >= 0)
				RemoteSessions[LIndex].Dispose();
			else
				throw new ServerException(ServerException.Codes.NoRemoteSessionForServerLink, AServerLink.Name);
		}
		#endif
		
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

		// Execution
		internal Exception WrapException(Exception AException)
		{
			return FServer.WrapException(AException, (FSessionInfo == null) || (FSessionInfo.CatalogCacheName != String.Empty));
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

		private ServerProcess InternalStartProcess(ProcessInfo AProcessInfo)
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
		
		private void InternalStopProcess(ServerProcess AProcess)
		{
			try
			{
				AProcess.Dispose();	// Is protected by a latch in the ServerChildObjects collection
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}

		// IServerSession.StartProcess
		IServerProcess IServerSession.StartProcess(ProcessInfo AProcessInfo)
		{
			return (IServerProcess)InternalStartProcess(AProcessInfo);
		}
		
		// IServerSession.StopProcess
		void IServerSession.StopProcess(IServerProcess AProcess)
		{
			InternalStopProcess((ServerProcess)AProcess);
		}
		
		// IRemoteServerSession.StartProcess
		IRemoteServerProcess IRemoteServerSession.StartProcess(ProcessInfo AProcessInfo, out int AProcessID)
		{
			ServerProcess LProcess = InternalStartProcess(AProcessInfo);
			AProcessID = LProcess.ProcessID;
			return (IRemoteServerProcess)LProcess;
		}
		
		// IRemoteServerSession.StopProcess
		void IRemoteServerSession.StopProcess(IRemoteServerProcess AProcess)
		{
			InternalStopProcess((ServerProcess)AProcess);
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
    
	// ServerProcess
	public class ServerProcess : ServerChildObject, IServerProcess, IRemoteServerProcess
	{
		internal ServerProcess(ServerSession AServerSession) : base()
		{
			InternalCreate(AServerSession, new ProcessInfo(AServerSession.SessionInfo));
		}
		
		internal ServerProcess(ServerSession AServerSession, ProcessInfo AProcessInfo) : base()
		{
			InternalCreate(AServerSession, AProcessInfo);
		}
		
		private void InternalCreate(ServerSession AServerSession, ProcessInfo AProcessInfo)
		{
			FServerSession = AServerSession;
			FPlans = new ServerPlans();
			FExecutingPlans = new ServerPlans();
			FScripts = new ServerScripts();
			FParser = new Parser();
			FProcessID = GetHashCode();
			FProcessInfo = AProcessInfo;
			FDeviceSessions = new Schema.DeviceSessions(false);
			FStreamManager = (IStreamManager)this;
			FContext = new Context(FServerSession.SessionInfo.DefaultMaxStackDepth, FServerSession.SessionInfo.DefaultMaxCallDepth);
			FProcessPlan = new ServerStatementPlan(this);
			PushExecutingPlan(FProcessPlan);

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FServerSession.Server.FProcessCounter != null)
				FServerSession.Server.FProcessCounter.Increment();
			#endif
		}
		
		private bool FDisposed;
	
		#if ALLOWPROCESSCONTEXT	
		protected void DeallocateContextVariables()
		{
			for (int LIndex = 0; LIndex < FContext.Count; LIndex++)
			{
				DataVar LDataVar = FContext[LIndex];
				if ((LDataVar != null) && LDataVar.DataType.IsDisposable && (LDataVar.Value != null))
					((IDisposable)LDataVar.Value).Dispose();
			}
		}
		#endif
		
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
									try
									{
										try
										{
											try
											{
												try
												{
													if (FApplicationTransactionID != Guid.Empty)
														LeaveApplicationTransaction();
												}
												finally
												{
													if (FTransactions != null)
													{
														// Rollback all active transactions
														while (InTransaction)
															RollbackTransaction();
															
														FTransactions.UnprepareDeferredConstraintChecks();
													}
												}
											}
											finally
											{
												if (FPlans != null)
													foreach (ServerPlanBase LPlan in FPlans)
													{
														if (LPlan.ActiveCursor != null)
															LPlan.ActiveCursor.Dispose();
													}
											}
										}
										finally
										{
											if (FContext != null)
											{
												#if ALLOWPROCESSCONTEXT
												DeallocateContextVariables();
												#endif
												FContext.Dispose();
												FContext = null;
											}
										}
									}
									finally
									{
										if (FDeviceSessions != null)
										{
											try
											{
												CloseDeviceSessions();
											}
											finally
											{
												FDeviceSessions.Dispose();
												FDeviceSessions = null;
											}
										}
									}
								}
								finally
								{
									if (FTransactions != null)
									{
										FTransactions.Dispose();
										FTransactions = null;
									}
								}
							}
							finally
							{
								if (FExecutingPlans != null)
								{
									while (FExecutingPlans.Count > 0)
										FExecutingPlans.DisownAt(0);
									FExecutingPlans.Dispose();
									FExecutingPlans = null;
								}
							}
						}
						finally
						{
							if (FScripts != null)
							{
								try
								{
									UnprepareScripts();
								}
								finally
								{
									FScripts.Dispose();
									FScripts = null;
								}
							}
						}
					}
					finally
					{
						if (FPlans != null)
						{
							try
							{
								UnpreparePlans();
							}
							finally
							{
								FPlans.Dispose();
								FPlans = null;
							}
						}
						
						if (!FDisposed)
						{
							#if !DISABLE_PERFORMANCE_COUNTERS
							if (FServerSession.Server.FProcessCounter != null)
								FServerSession.Server.FProcessCounter.Decrement();
							#endif
							FDisposed = true;
						}
					}
				}
				finally
				{
					try
					{
						if (FProcessPlan != null)
						{
							FProcessPlan.Dispose();
							FProcessPlan = null;
						}
					}
					finally
					{
						ReleaseLocks();
					}
				}
			}
			finally
			{
				FSQLParser = null;
				FSQLCompiler = null;
				FStreamManager = null;
				FServerSession = null;
				FProcessID = -1;
				FParser = null;
				base.Dispose(ADisposing);
			}
		}
		
		// ServerSession
		private ServerSession FServerSession;
		public ServerSession ServerSession { get { return FServerSession; } }
		
		private ServerPlanBase FProcessPlan;
		
		// ProcessID
		private int FProcessID = -1;
		public int ProcessID { get { return FProcessID; } }
		
		// ProcessInfo
		private ProcessInfo FProcessInfo;
		public ProcessInfo ProcessInfo { get { return FProcessInfo; } }
		
		// Plan State Exposed from ExecutingPlan
		public Plan Plan { get { return ExecutingPlan.Plan; } }
		
		// Context		
		private Context FContext;
		public Context Context { get { return FContext; } }
		
		public Context SwitchContext(Context AContext)
		{
			Context LContext = FContext;
			FContext = AContext;
			return LContext;
		}
		
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level for transactions on this process.")]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FProcessInfo.DefaultIsolationLevel; }
			set { FProcessInfo.DefaultIsolationLevel = value; }
		}
		
		/// <summary>Returns the isolation level of the current transaction, if one is active. Otherwise, returns the default isolation level.</summary>
		public IsolationLevel CurrentIsolationLevel()
		{
			return FTransactions.Count > 0 ? FTransactions.CurrentTransaction().IsolationLevel : DefaultIsolationLevel;
		}
		
		// NonLogged - Indicates whether logging is performed within the device.  
		// Is is an error to attempt non logged operations against a device that does not support non logged operations
		private int FNonLoggedCount;
		public bool NonLogged
		{
			get { return FNonLoggedCount > 0; }
			set { FNonLoggedCount += (value ? 1 : (NonLogged ? -1 : 0)); }
		}
		
		// IServerProcess.GetServerProcess()
		ServerProcess IServerProcess.GetServerProcess()
		{
			return this;
		}

		// IServerProcess.Session
		IServerSession IServerProcess.Session { get { return FServerSession; } }
		
		// IRemoteServerProcess.Session
		IRemoteServerSession IRemoteServerProcess.Session { get { return FServerSession; } }

		// Parsing
		private Parser FParser;		
		private RealSQL.Parser FSQLParser;
		private RealSQL.Compiler FSQLCompiler;
		
		private void EnsureSQLCompiler()
		{
			if (FSQLParser == null)
			{
				FSQLParser = new RealSQL.Parser();
				FSQLCompiler = new RealSQL.Compiler();
			}
		}

		public Statement ParseScript(string AScript, ParserMessages AMessages)
		{
			Statement LStatement;								  
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginParse, "Begin Parse");
			#endif
			if (FServerSession.SessionInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				LStatement = FSQLCompiler.Compile(FSQLParser.ParseScript(AScript));
			}
			else
				LStatement = FParser.ParseScript(AScript, AMessages);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndParse, "End Parse");
			#endif
			return LStatement;
		}
		
		public Statement ParseStatement(string AStatement, ParserMessages AMessages)
		{
			Statement LStatement;
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginParse, "Begin Parse");
			#endif
			if (FServerSession.SessionInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				LStatement = FSQLCompiler.Compile(FSQLParser.ParseStatement(AStatement));
			}
			else
				LStatement = FParser.ParseStatement(AStatement, AMessages);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndParse, "End Parse");
			#endif
			return LStatement;
		}
		
		public Expression ParseExpression(string AExpression)
		{
			Expression LExpression;
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginParse, "Begin Parse");
			#endif
			if (FServerSession.SessionInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				Statement LStatement = FSQLCompiler.Compile(FSQLParser.ParseStatement(AExpression));
				if (LStatement is SelectStatement)
					LExpression = ((SelectStatement)LStatement).CursorDefinition;
				else
					throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
			}
			else
				LExpression = FParser.ParseCursorDefinition(AExpression);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndParse, "End Parse");
			#endif
			return LExpression;
		}

		// Plans
		private ServerPlans FPlans;
		public ServerPlans Plans { get { return FPlans; } }
        
		private void UnpreparePlans()
		{
			Exception LException = null;
			while (FPlans.Count > 0)
			{
				try
				{
					ServerPlanBase LPlan = FPlans.DisownAt(0) as ServerPlanBase;
					if (!ServerSession.ReleaseCachedPlan(this, LPlan))
						LPlan.Dispose();
				}
				catch (Exception E)
				{
					LException = E;
				}
			}
			if (LException != null)
				throw LException;
		}
        
		// Scripts
		private ServerScripts FScripts;
		public ServerScripts Scripts { get { return FScripts; } }

		private void UnprepareScripts()
		{
			Exception LException = null;
			while (FScripts.Count > 0)
			{
				try
				{
					FScripts.DisownAt(0).Dispose();
				}
				catch (Exception E)
				{
					LException = E;
				}
			}
			if (LException != null)
				throw LException;
		}
		
		#if USEROWMANAGER
		// RowManager
		public RowManager RowManager { get { return FServerSession.Server.RowManager; } }
		#endif
		
		#if USESCALARMANAGER
		// ScalarManager
		public ScalarManager ScalarManager { get { return FServerSession.Server.ScalarManager; } }
		#endif
		
		// LockManager
		public void Lock(LockID ALockID, LockMode AMode)
		{
			FServerSession.Server.LockManager.Lock(FProcessID, ALockID, AMode);
		}
		
		public bool LockImmediate(LockID ALockID, LockMode AMode)
		{
			return FServerSession.Server.LockManager.LockImmediate(FProcessID, ALockID, AMode);
		}
		
		public void Unlock(LockID ALockID)
		{
			FServerSession.Server.LockManager.Unlock(FProcessID, ALockID);
		}
		
		// Release any locks associated with the process that have not yet been released
		private void ReleaseLocks()
		{
			// TODO: Release locks taken for a given process (presumably keep a hash table of locks acquired on this process, which means every request for a lock must go through the process)
			//FServerSession.Server.LockManager.Unlock(FProcessID);
		}

		// IStreamManager
		private IStreamManager FStreamManager;
		public IStreamManager StreamManager { get { return FStreamManager; } }
		
		StreamID IStreamManager.Allocate()
		{
			return ServerSession.Server.StreamManager.Allocate();
		}
		
		StreamID IStreamManager.Reference(StreamID AStreamID)
		{
			return ServerSession.Server.StreamManager.Reference(AStreamID);
		}
		
		void IStreamManager.Deallocate(StreamID AStreamID)
		{
			ServerSession.Server.StreamManager.Deallocate(AStreamID);
		}
		
		Stream IStreamManager.Open(StreamID AStreamID, LockMode ALockMode)
		{
			return ServerSession.Server.StreamManager.Open(FProcessID, AStreamID, ALockMode);
		}

		IRemoteStream IStreamManager.OpenRemote(StreamID AStreamID, LockMode ALockMode)
		{
			return ServerSession.Server.StreamManager.OpenRemote(FProcessID, AStreamID, ALockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			ServerSession.Server.StreamManager.Close(FProcessID, AStreamID);
		}
		#endif
		
		// ClassLoader
		object IServerProcess.CreateObject(ClassDefinition AClassDefinition, object[] AArguments)
		{
			return FServerSession.Server.Catalog.ClassLoader.CreateObject(AClassDefinition, AArguments);
		}
		
		Type IServerProcess.CreateType(ClassDefinition AClassDefinition)
		{
			return FServerSession.Server.Catalog.ClassLoader.CreateType(AClassDefinition);
		}
		
		string IRemoteServerProcess.GetClassName(string AClassName)
		{
			return FServerSession.Server.Catalog.ClassLoader.Classes[AClassName].ClassName;
		}
		
		private void GetFileNames(Schema.Library ALibrary, StringCollection AFileNames, ArrayList AFileDates)
		{
			foreach (Schema.FileReference LReference in ALibrary.Files)
				if (!AFileNames.Contains(LReference.FileName))
				{
					AFileNames.Add(LReference.FileName);
					AFileDates.Add(File.GetLastWriteTimeUtc(GetFullFileName(LReference.FileName)));
				} 
			
			foreach (Schema.LibraryReference LLibrary in ALibrary.Libraries)
				GetFileNames(FServerSession.Server.Catalog.Libraries[LLibrary.Name], AFileNames, AFileDates);
		}
		
		private void GetAssemblyFileNames(Schema.Library ALibrary, StringCollection AFileNames)
		{
			Schema.Libraries LLibraries = new Schema.Libraries();
			LLibraries.Add(ALibrary);
			
			while (LLibraries.Count > 0)
			{
				Schema.Library LLibrary = LLibraries[0];
				LLibraries.RemoveAt(0);

				foreach (Schema.FileReference LReference in LLibrary.Files)
					if (LReference.IsAssembly && !AFileNames.Contains(LReference.FileName))
						AFileNames.Add(LReference.FileName);
						
				foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
					if (!LLibraries.Contains(LReference.Name))
						LLibraries.Add(FServerSession.Server.Catalog.Libraries[LReference.Name]);
			}
		}
		
		void IRemoteServerProcess.GetFileNames(string AClassName, out string[] AFileNames, out DateTime[] AFileDates, out string[] AAssemblyFileNames)
		{
			Schema.RegisteredClass LClass = FServerSession.Server.Catalog.ClassLoader.Classes[AClassName];

			StringCollection LFileNames = new StringCollection();
			StringCollection LAssemblyFileNames = new StringCollection();
			ArrayList LFileDates = new ArrayList();
			
			// Build the list of all files required to load the assemblies in all libraries required by the library for the given class
			Schema.Library LLibrary = FServerSession.Server.Catalog.Libraries[LClass.Library.Name];
			GetFileNames(LLibrary, LFileNames, LFileDates);
			GetAssemblyFileNames(LLibrary, LAssemblyFileNames);
			
			AFileNames = new string[LFileNames.Count];
			LFileNames.CopyTo(AFileNames, 0);
			
			AFileDates = new DateTime[LFileDates.Count];
			LFileDates.CopyTo(AFileDates, 0);
			
			// Return the results in reverse order to ensure that dependencies are loaded in the correct order
			AAssemblyFileNames = new string[LAssemblyFileNames.Count];
			for (int LIndex = LAssemblyFileNames.Count - 1; LIndex >= 0; LIndex--)
				AAssemblyFileNames[LAssemblyFileNames.Count - LIndex - 1] = LAssemblyFileNames[LIndex];
		}
		
		private string GetFullFileName(string AFileName)
		{
			return PathUtility.GetFullFileName(AFileName);
		}
		
		IRemoteStream IRemoteServerProcess.GetFile(string AFileName)
		{
			return new CoverStream(new FileStream(GetFullFileName(AFileName), FileMode.Open, FileAccess.Read, FileShare.Read), true);
		}
		
		// DeviceSessions		
		private Schema.DeviceSessions FDeviceSessions;
		internal Schema.DeviceSessions DeviceSessions { get { return FDeviceSessions; } }

		public Schema.DeviceSession DeviceConnect(Schema.Device ADevice)
		{
			int LIndex = DeviceSessions.IndexOf(ADevice);
			if (LIndex < 0)
			{
				EnsureDeviceStarted(ADevice);
				Schema.DeviceSession LSession = ADevice.Connect(this, ServerSession.SessionInfo);
				try
				{
					#if REFERENCECOUNTDEVICESESSIONS
					LSession.FReferenceCount = 1;
					#endif
					while (LSession.Transactions.Count < FTransactions.Count)
						LSession.BeginTransaction(FTransactions[FTransactions.Count - 1].IsolationLevel);
					DeviceSessions.Add(LSession);
					return LSession;
				}
				catch
				{
					ADevice.Disconnect(LSession);
					throw;
				}
			}
			else
			{
				#if REFERENCECOUNTDEVICESESSIONS
				DeviceSessions[LIndex].FReferenceCount++;
				#endif
				return DeviceSessions[LIndex];
			}
		}
		
		internal void CloseDeviceSessions()
		{
			while (DeviceSessions.Count > 0)
				DeviceSessions[0].Device.Disconnect(DeviceSessions[0]);
		}
		
		public void DeviceDisconnect(Schema.Device ADevice)
		{
			int LIndex = DeviceSessions.IndexOf(ADevice);
			if (LIndex >= 0)
			{
				#if REFERENCECOUNTDEVICESESSIONS
				DeviceSessions[LIndex].FReferenceCount--;
				if (DeviceSessions[LIndex].FReferenceCount == 0)
				#endif
				ADevice.Disconnect(DeviceSessions[LIndex]);
			}
		}
		
		public DataVar DeviceExecute(Schema.Device ADevice, PlanNode APlanNode)
		{	
			if (IsReconciliationEnabled() || (APlanNode.DataType != null))
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return DeviceConnect(ADevice).Execute(Plan.GetDevicePlan(APlanNode));
				}
				finally
				{
					RootExecutingPlan.Statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}

			return null;
		}
		
		public void EnsureDeviceStarted(Schema.Device ADevice)
		{
			ServerSession.Server.StartDevice(this, ADevice);
		}
		
		// CatalogDeviceSession
		public CatalogDeviceSession CatalogDeviceSession
		{
			get
			{
				CatalogDeviceSession LSession = DeviceConnect(ServerSession.Server.CatalogDevice) as CatalogDeviceSession;
				Error.AssertFail(LSession != null, "Could not connect to catalog device");
				return LSession;
			}
		}
		
		// Tracing
		#if TRACEEVENTS
		public void RaiseTraceEvent(string ATraceCode, string ADescription)
		{
			FServerSession.Server.RaiseTraceEvent(this, ATraceCode, ADescription);
		}
		#endif
		
		// Plans
		/// <summary>Indicates whether or not warnings encountered during compilation of plans on this process will be reported.</summary>
		public bool SuppressWarnings
		{
			get { return FProcessInfo.SuppressWarnings; }
			set { FProcessInfo.SuppressWarnings = value; }
		}
		
		private void CleanupPlans(ProcessCleanupInfo ACleanupInfo)
		{
			int LPlanIndex;
			for (int LIndex = 0; LIndex < ACleanupInfo.UnprepareList.Length; LIndex++)
			{
				LPlanIndex = FPlans.IndexOf((ServerChildObject)ACleanupInfo.UnprepareList[LIndex]);
				if (LPlanIndex >= 0)
					InternalUnprepare(FPlans[LPlanIndex]);
			}
		}

		#if ALLOWPROCESSCONTEXT
		private void PushProcessContext(Plan APlan)
		{
			APlan.Symbols.PushFrame();
			for (int LIndex = FContext.Count - 1; LIndex >= 0; LIndex--)
				APlan.Symbols.Push(FContext[LIndex]);
			if (FContext.AllowExtraWindowAccess)
				APlan.Symbols.AllowExtraWindowAccess = true;
		}
		
		private void PopProcessContext(Plan APlan)
		{
			APlan.Symbols.PopFrame();
			if (FContext.AllowExtraWindowAccess)
				APlan.Symbols.AllowExtraWindowAccess = false;
		}
		#endif
		
		private void Compile(ServerPlanBase AServerPlan, Statement AStatement, DataParams AParams, bool AIsExpression)
		{
			PushExecutingPlan(AServerPlan);
			try
			{
				#if ALLOWPROCESSCONTEXT
				// Push process context on the symbols for the plan
				PushProcessContext(AServerPlan.Plan);
				try
				{
				#endif
					#if TIMING
					DateTime LStartTime = DateTime.Now;
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile", DateTime.Now.ToString("hh:mm:ss.ffff")));
					#endif
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.BeginCompile, "Begin Compile");
					#endif
					Schema.DerivedTableVar LTableVar = null;
					LTableVar = new Schema.DerivedTableVar(Schema.Object.GetNextObjectID(), Schema.Object.NameFromGuid(AServerPlan.ID));
					LTableVar.SessionObjectName = LTableVar.Name;
					LTableVar.SessionID = AServerPlan.ServerProcess.ServerSession.SessionID;
					AServerPlan.Plan.PushCreationObject(LTableVar);
					try
					{
						AServerPlan.Code = Compiler.Compile(AServerPlan.Plan, AStatement, AParams, AIsExpression);

						// Include dependencies for any process context that may be being referenced by the plan
						// This is necessary to support the variable declaration statements that will be added to support serialization of the plan
						for (int LIndex = Context.Count - 1; LIndex >= 0; LIndex--)
							if ((AParams == null) || !AParams.Contains(Context[LIndex].Name))
								AServerPlan.Plan.PlanCatalog.IncludeDependencies(this, AServerPlan.Plan.Catalog, Context[LIndex].DataType, EmitMode.ForRemote);

						if (!AServerPlan.Plan.Messages.HasErrors && AIsExpression)
						{
							if (AServerPlan.Code.DataType == null)
								throw new CompilerException(CompilerException.Codes.ExpressionExpected);
							AServerPlan.DataType = CopyDataType(AServerPlan, LTableVar);
						}
					}
					finally
					{
						AServerPlan.Plan.PopCreationObject();
					}
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.EndCompile, "End Compile");
					#endif
					#if TIMING
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile -- Compile Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
					#endif
				#if ALLOWPROCESSCONTEXT
				}
				finally
				{
					// Pop process context from the plan
					PopProcessContext(AServerPlan.Plan);
				}
				#endif
			}
			finally
			{
				PopExecutingPlan(AServerPlan);
			}
		}
        
		internal ServerPlanBase CompileStatement(Statement AStatement, ParserMessages AMessages, DataParams AParams)
		{
			ServerPlanBase LPlan = new ServerStatementPlan(this);
			try
			{
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AStatement, AParams, false);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
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
		
		private void InternalUnprepare(ServerPlanBase APlan)
		{
			BeginCall();
			try
			{
				FPlans.Disown(APlan);
				if (!ServerSession.ReleaseCachedPlan(this, APlan))
					APlan.Dispose();
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
        
		private int GetContextHashCode(DataParams AParams)
		{
			StringBuilder LContextName = new StringBuilder();
			if (FContext != null)
				for (int LIndex = 0; LIndex < FContext.Count; LIndex++)
					LContextName.Append(FContext.Peek(LIndex).DataType.Name);
			if (AParams != null)
				for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
					LContextName.Append(AParams[LIndex].DataType.Name);
			return LContextName.ToString().GetHashCode();
		}
		
		// IServerProcess.PrepareStatement        
		IServerStatementPlan IServerProcess.PrepareStatement(string AStatement, DataParams AParams)
		{
			BeginCall();
			try
			{
				int LContextHashCode = GetContextHashCode(AParams);
				IServerStatementPlan LPlan = ServerSession.GetCachedPlan(this, AStatement, LContextHashCode) as IServerStatementPlan;
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
				#endif
				if (LPlan != null)
					FPlans.Add(LPlan);
				else
				{
					ParserMessages LMessages = new ParserMessages();
					Statement LStatement = ParseStatement(AStatement, LMessages);
					LPlan = (IServerStatementPlan)CompileStatement(LStatement, LMessages, AParams);
					if (!LPlan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AStatement, LContextHashCode, (ServerPlanBase)LPlan);
				}
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
				#endif
				return LPlan;
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
        
		// IServerProcess.UnprepareStatement
		void IServerProcess.UnprepareStatement(IServerStatementPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
        
		internal ServerPlanBase CompileRemoteStatement(Statement AStatement, ParserMessages AMessages, RemoteParam[] AParams)
		{
			ServerPlanBase LPlan = new RemoteServerStatementPlan(this);
			try
			{
				DataParams LParams = RemoteParamsToDataParams(AParams);
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AStatement, LParams, false);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		// IServerProcess.Execute
		void IServerProcess.Execute(string AStatement, DataParams AParams)
		{
			IServerStatementPlan LStatementPlan = ((IServerProcess)this).PrepareStatement(AStatement, AParams);
			try
			{
				LStatementPlan.Execute(AParams);
			}
			finally
			{
				((IServerProcess)this).UnprepareStatement(LStatementPlan);
			}
		}
		
		private int GetContextHashCode(RemoteParam[] AParams)
		{
			StringBuilder LContextName = new StringBuilder();
			if (FContext != null)
				for (int LIndex = 0; LIndex < FContext.Count; LIndex++)
					LContextName.Append(FContext.Peek(LIndex).DataType.Name);
			if (AParams != null)
				for (int LIndex = 0; LIndex < AParams.Length; LIndex++)
					LContextName.Append(AParams[LIndex].TypeName);
			return LContextName.ToString().GetHashCode();
		}
		
		// IRemoteServerProcess.PrepareStatement
		IRemoteServerStatementPlan IRemoteServerProcess.PrepareStatement(string AStatement, RemoteParam[] AParams, out PlanDescriptor APlanDescriptor, ProcessCleanupInfo ACleanupInfo)
		{
			BeginCall();
			try
			{
				CleanupPlans(ACleanupInfo);
				int LContextHashCode = GetContextHashCode(AParams);
				IRemoteServerStatementPlan LPlan = ServerSession.GetCachedPlan(this, AStatement, LContextHashCode) as IRemoteServerStatementPlan;
				if (LPlan != null)
					FPlans.Add(LPlan);
				else
				{
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
					#endif
					ParserMessages LMessages = new ParserMessages();
					Statement LStatement = ParseStatement(AStatement, LMessages);
					LPlan = (IRemoteServerStatementPlan)CompileRemoteStatement(LStatement, LMessages, AParams);
					if (!((ServerPlanBase)LPlan).Plan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AStatement, LContextHashCode, (ServerPlanBase)LPlan);
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
					#endif
				}
				APlanDescriptor = GetPlanDescriptor(LPlan);
				return LPlan;
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
        
		// IRemoteServerProcess.UnprepareStatement
		void IRemoteServerProcess.UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
		
		// IRemoteServerProcess.Execute
		void IRemoteServerProcess.Execute(string AStatement, ref RemoteParamData AParams, ProcessCallInfo ACallInfo, ProcessCleanupInfo ACleanupInfo)
		{
			ProcessCallInfo(ACallInfo);
			PlanDescriptor LDescriptor;
			IRemoteServerStatementPlan LPlan = ((IRemoteServerProcess)this).PrepareStatement(AStatement, AParams.Params, out LDescriptor, ACleanupInfo);
			try
			{
				TimeSpan LExecuteTime;
				LPlan.Execute(ref AParams, out LExecuteTime, EmptyCallInfo());
				LDescriptor.Statistics.ExecuteTime = LExecuteTime;
			}
			finally
			{
				((IRemoteServerProcess)this).UnprepareStatement(LPlan);
			}
		}
        
		private Schema.IDataType CopyDataType(ServerPlanBase APlan, Schema.DerivedTableVar ATableVar)
		{
			#if TIMING
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerProcess.CopyDataType", DateTime.Now.ToString("hh:mm:ss.ffff")));
			#endif
			if (APlan.Code.DataType is Schema.CursorType)
			{
				TableNode LSourceNode = (TableNode)APlan.Code.Nodes[0];	
				Schema.TableVar LSourceTableVar = LSourceNode.TableVar;

				AdornExpression LAdornExpression = new AdornExpression();
				LAdornExpression.MetaData = new MetaData();
				LAdornExpression.MetaData.Tags.Add(new Tag("DAE.IsChangeRemotable", (LSourceTableVar.IsChangeRemotable || !LSourceTableVar.ShouldChange).ToString()));
				LAdornExpression.MetaData.Tags.Add(new Tag("DAE.IsDefaultRemotable", (LSourceTableVar.IsDefaultRemotable || !LSourceTableVar.ShouldDefault).ToString()));
				LAdornExpression.MetaData.Tags.Add(new Tag("DAE.IsValidateRemotable", (LSourceTableVar.IsValidateRemotable || !LSourceTableVar.ShouldValidate).ToString()));

				if (LSourceNode.Order != null)
				{
					if (!LSourceTableVar.Orders.Contains(LSourceNode.Order))
						LSourceTableVar.Orders.Add(LSourceNode.Order);
					LAdornExpression.MetaData.Tags.Add(new Tag("DAE.DefaultOrder", LSourceNode.Order.Name));
				}
				
				foreach (Schema.TableVarColumn LColumn in LSourceNode.TableVar.Columns)
				{
					if (!((LColumn.IsDefaultRemotable || !LColumn.ShouldDefault) && (LColumn.IsValidateRemotable || !LColumn.ShouldValidate) && (LColumn.IsChangeRemotable || !LColumn.ShouldChange)))
					{
						AdornColumnExpression LColumnExpression = new AdornColumnExpression();
						LColumnExpression.ColumnName = Schema.Object.EnsureRooted(LColumn.Name);
						LColumnExpression.MetaData = new MetaData();
						LColumnExpression.MetaData.Tags.AddOrUpdate("DAE.IsChangeRemotable", (LColumn.IsChangeRemotable || !LColumn.ShouldChange).ToString());
						LColumnExpression.MetaData.Tags.AddOrUpdate("DAE.IsDefaultRemotable", (LColumn.IsDefaultRemotable || !LColumn.ShouldDefault).ToString());
						LColumnExpression.MetaData.Tags.AddOrUpdate("DAE.IsValidateRemotable", (LColumn.IsValidateRemotable || !LColumn.ShouldValidate).ToString());
						LAdornExpression.Expressions.Add(LColumnExpression);
					}
				}
				
				PlanNode LViewNode = LSourceNode;
				while (LViewNode is BaseOrderNode)
					LViewNode = LViewNode.Nodes[0];
					
				LViewNode = Compiler.EmitAdornNode(APlan.Plan, LViewNode, LAdornExpression);

				ATableVar.CopyTableVar((TableNode)LViewNode);
				ATableVar.CopyReferences((TableNode)LViewNode);
				ATableVar.InheritMetaData(((TableNode)LViewNode).TableVar.MetaData);
				#if USEORIGINALEXPRESSION
				ATableVar.OriginalExpression = (Expression)LViewNode.EmitStatement(EmitMode.ForRemote);
				#else
				ATableVar.InvocationExpression = (Expression)LViewNode.EmitStatement(EmitMode.ForRemote);
				#endif
				
				// Gather AT dependencies
				// Each AT object will be emitted remotely as the underlying object, so report the dependency so that the catalog emission happens in the correct order.
				Schema.Objects LATObjects = new Schema.Objects();
				if (ATableVar.HasDependencies())
					for (int LIndex = 0; LIndex < ATableVar.Dependencies.Count; LIndex++)
					{
						Schema.Object LObject = ATableVar.Dependencies.ResolveObject(APlan.ServerProcess, LIndex);
						if ((LObject != null) && LObject.IsATObject && ((LObject is Schema.TableVar) || (LObject is Schema.Operator)))
							LATObjects.Add(LObject);
					}

				foreach (Schema.Object LATObject in LATObjects)
				{
					Schema.TableVar LATTableVar = LATObject as Schema.TableVar;
					if (LATTableVar != null)
					{
						Schema.TableVar LTableVar = (Schema.TableVar)APlan.Plan.Catalog[APlan.Plan.Catalog.IndexOfName(LATTableVar.SourceTableName)];
						ATableVar.Dependencies.Ensure(LTableVar);
						continue;
					}

					Schema.Operator LATOperator = LATObject as Schema.Operator;
					if (LATOperator != null)
					{
						ATableVar.Dependencies.Ensure(ServerSession.Server.ATDevice.ResolveSourceOperator(this, LATOperator));
					}
				}
				
				APlan.Plan.PlanCatalog.IncludeDependencies(this, APlan.Plan.Catalog, ATableVar, EmitMode.ForRemote);
				APlan.Plan.PlanCatalog.Remove(ATableVar);
				APlan.Plan.PlanCatalog.Add(ATableVar);
				return ATableVar.DataType;
			}
			else
			{
				APlan.Plan.PlanCatalog.IncludeDependencies(this, APlan.Plan.Catalog, ATableVar, EmitMode.ForRemote);
				APlan.Plan.PlanCatalog.SafeRemove(ATableVar);
				APlan.Plan.PlanCatalog.IncludeDependencies(this, APlan.Plan.Catalog, APlan.Code.DataType, EmitMode.ForRemote);
				return APlan.Code.DataType;
			}
		}

		internal ServerPlanBase CompileExpression(Statement AExpression, ParserMessages AMessages, DataParams AParams)
		{
			ServerPlanBase LPlan = new ServerExpressionPlan(this);
			try
			{
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AExpression, AParams, true);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		internal PlanDescriptor GetPlanDescriptor(IRemoteServerExpressionPlan APlan, RemoteParam[] AParams)
		{
			PlanDescriptor LDescriptor = new PlanDescriptor();
			LDescriptor.ID = APlan.ID;
			LDescriptor.Statistics = APlan.Statistics;
			LDescriptor.Messages = APlan.Messages;
			if (((ServerPlanBase)APlan).DataType is Schema.ITableType)
			{
				LDescriptor.Capabilities = APlan.Capabilities;
				LDescriptor.CursorIsolation = APlan.Isolation;
				LDescriptor.CursorType = APlan.CursorType;
				if (((TableNode)((ServerPlanBase)APlan).Code.Nodes[0]).Order != null)
					LDescriptor.Order = ((TableNode)((ServerPlanBase)APlan).Code.Nodes[0]).Order.Name;
				else
					LDescriptor.Order = String.Empty;
			}
			LDescriptor.Catalog = ((RemoteServerExpressionPlan)APlan).GetCatalog(AParams, out LDescriptor.ObjectName, out LDescriptor.CacheTimeStamp, out LDescriptor.ClientCacheTimeStamp, out LDescriptor.CacheChanged);
			return LDescriptor;
		}
		
		// IServerProcess.PrepareExpression
		IServerExpressionPlan IServerProcess.PrepareExpression(string AExpression, DataParams AParams)
		{
			BeginCall();
			try
			{
				int LContextHashCode = GetContextHashCode(AParams);
				IServerExpressionPlan LPlan = ServerSession.GetCachedPlan(this, AExpression, LContextHashCode) as IServerExpressionPlan;
				if (LPlan != null)
					FPlans.Add(LPlan);
				else
				{
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
					#endif
					ParserMessages LMessages = new ParserMessages();
					LPlan = (IServerExpressionPlan)CompileExpression(ParseExpression(AExpression), LMessages, AParams);
					if (!LPlan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AExpression, LContextHashCode, (ServerPlanBase)LPlan);
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
					#endif
				}
				return LPlan;
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
        
		// IServerProcess.UnprepareExpression
		void IServerProcess.UnprepareExpression(IServerExpressionPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
		
		// IServerProcess.Evaluate
		DataValue IServerProcess.Evaluate(string AExpression, DataParams AParams)
		{
			IServerExpressionPlan LPlan = ((IServerProcess)this).PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Evaluate(AParams);
			}
			finally
			{
				((IServerProcess)this).UnprepareExpression(LPlan);
			}
		}
		
		// IServerProcess.OpenCursor
		IServerCursor IServerProcess.OpenCursor(string AExpression, DataParams AParams)
		{
			IServerExpressionPlan LPlan = ((IServerProcess)this).PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Open(AParams);
			}
			catch
			{
				((IServerProcess)this).UnprepareExpression(LPlan);
				throw;
			}
		}
		
		// IServerProcess.CloseCursor
		void IServerProcess.CloseCursor(IServerCursor ACursor)
		{
			IServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor);
			}
			finally
			{
				((IServerProcess)this).UnprepareExpression(LPlan);
			}
		}
        
		internal ServerPlanBase CompileRemoteExpression(Statement AExpression, ParserMessages AMessages, RemoteParam[] AParams)
		{
			ServerPlanBase LPlan = new RemoteServerExpressionPlan(this);
			try
			{
				DataParams LParams = RemoteParamsToDataParams(AParams);
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AExpression, LParams, true);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch 
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		private IRemoteServerExpressionPlan InternalPrepareRemoteExpression(string AExpression, RemoteParam[] AParams, ProcessCleanupInfo ACleanupInfo)
		{
			CleanupPlans(ACleanupInfo);
			int LContextHashCode = GetContextHashCode(AParams);
			IRemoteServerExpressionPlan LPlan = ServerSession.GetCachedPlan(this, AExpression, LContextHashCode) as IRemoteServerExpressionPlan;
			if (LPlan != null)
				FPlans.Add(LPlan);
			else
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
				#endif
				ParserMessages LMessages = new ParserMessages();
				LPlan = (IRemoteServerExpressionPlan)CompileRemoteExpression(ParseExpression(AExpression), LMessages, AParams);
				if (!((ServerPlanBase)LPlan).Plan.Messages.HasErrors)
					ServerSession.AddCachedPlan(this, AExpression, LContextHashCode, (ServerPlanBase)LPlan);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
				#endif
			}
			return LPlan;
		}
		
		// IRemoteServerProcess.PrepareExpression
		IRemoteServerExpressionPlan IRemoteServerProcess.PrepareExpression(string AExpression, RemoteParam[] AParams, out PlanDescriptor APlanDescriptor, ProcessCleanupInfo ACleanupInfo)
		{
			BeginCall();
			try
			{
				IRemoteServerExpressionPlan LPlan = InternalPrepareRemoteExpression(AExpression, AParams, ACleanupInfo);
				APlanDescriptor = GetPlanDescriptor(LPlan, AParams);
				return LPlan;
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
        
		// IRemoteServerProcess.UnprepareExpression
		void IRemoteServerProcess.UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
		
		// IRemoteServerProcess.Evaluate
		byte[] IRemoteServerProcess.Evaluate
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
			
			BeginCall();
			try
			{
				APlan = InternalPrepareRemoteExpression(AExpression, AParams.Params, ACleanupInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
			
			#if LOGCACHEEVENTS
			FServerSession.Server.LogMessage(String.Format("Evaluating '{0}'.", AExpression));
			#endif
			
			try
			{
				TimeSpan LExecuteTime;
				byte[] LResult = APlan.Evaluate(ref AParams, out LExecuteTime, EmptyCallInfo());
				APlanDescriptor = GetPlanDescriptor(APlan, AParams.Params);
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				
				#if LOGCACHEEVENTS
				FServerSession.Server.LogMessage("Expression evaluated.");
				#endif

				return LResult;
			}
			catch
			{
				((IRemoteServerProcess)this).UnprepareExpression(APlan);
				throw;
			}
		}
		
		// IRemoteServerProcess.OpenCursor
		IRemoteServerCursor IRemoteServerProcess.OpenCursor
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

			BeginCall();
			try
			{
				APlan = InternalPrepareRemoteExpression(AExpression, AParams.Params, ACleanupInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}

			try
			{
				TimeSpan LExecuteTime;
				IRemoteServerCursor LCursor = APlan.Open(ref AParams, out LExecuteTime, EmptyCallInfo());
				APlanDescriptor = GetPlanDescriptor(APlan, AParams.Params);
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LCursor;
			}
			catch
			{
				((IRemoteServerProcess)this).UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// IRemoteServerProcess.OpenCursor
		IRemoteServerCursor IRemoteServerProcess.OpenCursor
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

			BeginCall();
			try
			{
				APlan = InternalPrepareRemoteExpression(AExpression, AParams.Params, ACleanupInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
			
			try
			{
				TimeSpan LExecuteTime;
				IRemoteServerCursor LCursor = APlan.Open(ref AParams, out LExecuteTime, EmptyCallInfo());
				AFetchData = LCursor.Fetch(out ABookmarks, ACount, EmptyCallInfo());
				APlanDescriptor = GetPlanDescriptor(APlan, AParams.Params);
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LCursor;
			}
			catch
			{
				((IRemoteServerProcess)this).UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// IRemoteServerProcess.CloseCursor
		void IRemoteServerProcess.CloseCursor(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			IRemoteServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor, ACallInfo);
			}
			finally
			{
				((IRemoteServerProcess)this).UnprepareExpression(LPlan);
			}
		}
		
		private ServerScript InternalPrepareScript(string AScript)
		{
			BeginCall();
			try
			{
				ServerScript LScript = new ServerScript(this, AScript);
				try
				{
					FScripts.Add(LScript);
					return LScript;
				}
				catch
				{
					LScript.Dispose();
					throw;
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
        
		private void InternalUnprepareScript(ServerScript AScript)
		{
			BeginCall();
			try
			{
				AScript.Dispose();
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
        
		// IServerProcess.PrepareScript
		IServerScript IServerProcess.PrepareScript(string AScript)
		{
			return (IServerScript)InternalPrepareScript(AScript);
		}
        
		// IServerProcess.UnprepareScript
		void IServerProcess.UnprepareScript(IServerScript AScript)
		{
			InternalUnprepareScript((ServerScript)AScript);
		}
		
		// IServerProcess.ExecuteScript
		void IServerProcess.ExecuteScript(string AScript)
		{
			IServerScript LScript = ((IServerProcess)this).PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)this).UnprepareScript(LScript);
			}
		}
        
		// IRemoteServerProcess.PrepareScript
		IRemoteServerScript IRemoteServerProcess.PrepareScript(string AScript)
		{
			return (IRemoteServerScript)InternalPrepareScript(AScript);
		}
        
		// IRemoteServerProcess.UnprepareScript
		void IRemoteServerProcess.UnprepareScript(IRemoteServerScript AScript)
		{
			InternalUnprepareScript((ServerScript)AScript);
		}
		
		// IRemoteServerProcess.ExecuteScript
		void IRemoteServerProcess.ExecuteScript(string AScript, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			IServerScript LScript = ((IServerProcess)this).PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)this).UnprepareScript(LScript);
			}
		}

		// Transaction		
		private ServerDTCTransaction FDTCTransaction;
		private ServerTransactions FTransactions = new ServerTransactions();
		internal ServerTransactions Transactions { get { return FTransactions; } }

		internal ServerTransaction CurrentTransaction { get { return FTransactions.CurrentTransaction(); } }
		internal ServerTransaction RootTransaction { get { return FTransactions.RootTransaction(); } }
		
		internal void AddInsertTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			FTransactions.AddInsertTableVarCheck(ATableVar, ARow);
		}
		
		internal void AddUpdateTableVarCheck(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			FTransactions.AddUpdateTableVarCheck(ATableVar, AOldRow, ANewRow);
		}
		
		internal void AddDeleteTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			FTransactions.AddDeleteTableVarCheck(ATableVar, ARow);
		}
		
		internal void RemoveDeferredConstraintChecks(Schema.TableVar ATableVar)
		{
			if (FTransactions != null)
				FTransactions.RemoveDeferredConstraintChecks(ATableVar);
		}
		
		internal void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			if (FTransactions != null)
				FTransactions.RemoveDeferredHandlers(AHandler);
		}
		
		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			if (FTransactions != null)
				FTransactions.RemoveCatalogConstraintCheck(AConstraint);
		}
		
		// TransactionCount
		public int TransactionCount { get { return FTransactions.Count; } }

		// InTransaction
		public bool InTransaction { get { return FTransactions.Count > 0; } }

		private void CheckInTransaction()
		{
			if (!InTransaction)
				throw new ServerException(ServerException.Codes.NoTransactionActive);
		}
        
		// UseDTC
		public bool UseDTC
		{
			get { return FProcessInfo.UseDTC; }
			set
			{
				lock (this)
				{
					if (InTransaction)
						throw new ServerException(ServerException.Codes.TransactionActive);

					if (value && (Environment.OSVersion.Version.Major < 5))
						throw new ServerException(ServerException.Codes.DTCNotSupported);
					FProcessInfo.UseDTC = value;
				}
			}
		}
		
		// UseImplicitTransactions
		public bool UseImplicitTransactions
		{
			get { return FProcessInfo.UseImplicitTransactions; }
			set { FProcessInfo.UseImplicitTransactions = value; }
		}

		// BeginTransaction
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			BeginCall();
			try
			{
				InternalBeginTransaction(AIsolationLevel);
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
        
		// PrepareTransaction
		public void PrepareTransaction()
		{
			BeginCall();
			try
			{
				InternalPrepareTransaction();
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
        
		// CommitTransaction
		public void CommitTransaction()
		{
			BeginCall();
			try
			{
				InternalCommitTransaction();
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

		// RollbackTransaction        
		public void RollbackTransaction()
		{
			BeginCall();
			try
			{
				InternalRollbackTransaction();
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
    
		protected internal void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginBeginTransaction, "Begin BeginTransaction");
			#endif
			FTransactions.BeginTransaction(this, AIsolationLevel);

			if (UseDTC && (FDTCTransaction == null))
			{
				CloseDeviceSessions();
				FDTCTransaction = new ServerDTCTransaction();
				FDTCTransaction.IsolationLevel = AIsolationLevel;
			}
			
			// Begin a transaction on all device sessions
			foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
				LDeviceSession.BeginTransaction(AIsolationLevel);

			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndBeginTransaction, "End BeginTransaction");
			#endif
		}
		
		protected internal void InternalPrepareTransaction()
		{
			CheckInTransaction();
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginPrepareTransaction, "Begin PrepareTransaction");
			#endif

			ServerTransaction LTransaction = FTransactions.CurrentTransaction();
			if (!LTransaction.Prepared)
			{
				foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
					LDeviceSession.PrepareTransaction();
					
				// Invoke event handlers for work done during this transaction
				LTransaction.InvokeDeferredHandlers(this);

				// Validate Constraints which have been affected by work done during this transaction
				FTransactions.ValidateDeferredConstraints(this);

				foreach (Schema.CatalogConstraint LConstraint in LTransaction.CatalogConstraints)
					LConstraint.Validate(this);
				
				LTransaction.Prepared = true;
			}

			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndPrepareTransaction, "End PrepareTransaction");
			#endif
		}
		
		protected internal void InternalCommitTransaction()
		{
			CheckInTransaction();
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginCommitTransaction, "Begin CommitTransaction");
			#endif

			// Prepare Commit Phase
			InternalPrepareTransaction();

			try
			{
				// Commit the DTC if this is the Top Level Transaction
				if (UseDTC && (FTransactions.Count == 1))
				{
					try
					{
						FDTCTransaction.Commit();
					}
					finally
					{
						FDTCTransaction.Dispose();
						FDTCTransaction = null;
					}
				}
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
			
			// Commit
			Schema.DeviceSession LCatalogDeviceSession = null;
			foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
			{
				if (LDeviceSession is CatalogDeviceSession)
					LCatalogDeviceSession = LDeviceSession;
				else
					LDeviceSession.CommitTransaction();
			}
			
			// Commit the catalog device session last. The catalog device session tracks devices
			// dropped during the session, and will defer the stop of those devices until after the
			// transaction has committed, allowing any active transactions in that device to commit.
			if (LCatalogDeviceSession != null)
				LCatalogDeviceSession.CommitTransaction();

			FTransactions.EndTransaction(true);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndCommitTransaction, "End CommitTransaction");
			#endif
		}

		protected internal void InternalRollbackTransaction()
		{
			CheckInTransaction();
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginRollbackTransaction, "Begin RollbackTransaction");
			#endif
			try
			{
				FTransactions.CurrentTransaction().InRollback = true;
				try
				{
					if (UseDTC && (FTransactions.Count == 1))
					{
						try
						{
							FDTCTransaction.Rollback();
						}
						finally
						{
							FDTCTransaction.Dispose();
							FDTCTransaction = null;
						}
					}

					Exception LException = null;
					Schema.DeviceSession LCatalogDeviceSession = null;
					foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
					{
						try
						{
							if (LDeviceSession is CatalogDeviceSession)
								LCatalogDeviceSession = LDeviceSession;
							else
								if (LDeviceSession.InTransaction)
									LDeviceSession.RollbackTransaction();
						}
						catch (Exception E)
						{
							LException = E;
							this.ServerSession.Server.LogError(E);
						}
					}
					
					try
					{
						// Catalog device session transaction must be rolled back last
						// Well, it's not really correct to roll it back last, or first, or in any particular order,
						// but I don't have another option unless I start managing all transaction logging for
						// all devices, so rolling it back last seems like the best option (even at that I have
						// to ignore errors that occur during the rollback - see CatalogDeviceSession.InternalRollbackTransaction).
						if ((LCatalogDeviceSession != null) && LCatalogDeviceSession.InTransaction)
							LCatalogDeviceSession.RollbackTransaction();
					}
					catch (Exception E)
					{
						LException = E;
						this.ServerSession.Server.LogError(E);
					}
					
					if (LException != null)
						throw LException;
				}
				finally
				{
					FTransactions.CurrentTransaction().InRollback = false;
				}
			}
			finally
			{
				FTransactions.EndTransaction(false);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndRollbackTransaction, "End RollbackTransaction");
				#endif
			}
		}
		
		// Application Transactions
		private Guid FApplicationTransactionID = Guid.Empty;
		public Guid ApplicationTransactionID { get { return FApplicationTransactionID; } }
		
		private bool FIsInsert;
		/// <summary>Indicates whether this process is an insert participant in an application transaction.</summary>
		public bool IsInsert 
		{ 
			get { return FIsInsert; } 
			set { FIsInsert = value; }
		}
		
		private bool FIsOpeningInsertCursor;
		/// <summary>Indicates whether this process is currently opening an insert cursor.</summary>
		/// <remarks>
		/// This flag is used to implement an optimization to the insert process to prevent the actual
		/// opening of cursors unnecessarily. This flag is used by the devices involved in an expression
		/// to determine whether or not to issue the cursor open, or simply return an empty cursor.
		/// The optimization is valid because the insert cursor expression always has a contradictory
		/// restriction appended. In theory, the optimizers for the target systems should recognize the
		/// contradiction and the insert cursor open should not take any time, but in reality, it
		/// does take time with some complex cursors, and there is also the unnecessary network or RPC hit
		/// for extra-process devices.
		/// </remarks>
		public bool IsOpeningInsertCursor
		{
			get { return FIsOpeningInsertCursor; }
			set { FIsOpeningInsertCursor = value; }
		}
		
		// AddingTableVar
		private int FAddingTableVar;
		/// <summary>Indicates whether the current process is adding an A/T table map.</summary>
		/// <remarks>This is used to prevent the resolution process from attempting to add a table map for the table variable being created before the table map is done.</remarks>
		public bool InAddingTableVar { get { return FAddingTableVar > 0; } }
		
		public void PushAddingTableVar()
		{
			FAddingTableVar++;
		}
		
		public void PopAddingTableVar()
		{
			FAddingTableVar--;
		}
		
		public ApplicationTransaction GetApplicationTransaction()
		{
			return ApplicationTransactionUtility.GetTransaction(this, FApplicationTransactionID);
		}
		
		public void EnsureApplicationTransactionOperator(Schema.Operator AOperator)
		{
			if (FApplicationTransactionID != Guid.Empty && AOperator.IsATObject)
			{
				ApplicationTransaction LTransaction = GetApplicationTransaction();
				try
				{
					LTransaction.EnsureATOperatorMapped(this, AOperator);
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		public void EnsureApplicationTransactionTableVar(Schema.TableVar ATableVar)
		{
			if (FApplicationTransactionID != Guid.Empty && ATableVar.IsATObject)
			{
				ApplicationTransaction LTransaction = GetApplicationTransaction();
				try
				{
					LTransaction.EnsureATTableVarMapped(this, ATableVar);
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
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
		
		Guid IRemoteServerProcess.BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			return BeginApplicationTransaction(AShouldJoin, AIsInsert);
		}
		
		// BeginApplicationTransaction
		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginBeginApplicationTransaction, "Begin BeginApplicationTransaction");
				#endif
				Guid LATID = ApplicationTransactionUtility.BeginApplicationTransaction(this);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndBeginApplicationTransaction, "End BeginApplicationTransaction");
				#endif
				
				if (AShouldJoin)
					JoinApplicationTransaction(LATID, AIsInsert);

				return LATID;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.PrepareApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			PrepareApplicationTransaction(AID);
		}
		
		// PrepareApplicationTransaction
		public void PrepareApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginPrepareApplicationTransaction, "Begin PrepareApplicationTransaction");
				#endif
				ApplicationTransactionUtility.PrepareApplicationTransaction(this, AID);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndPrepareApplicationTransaction, "End PrepareApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.CommitApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			CommitApplicationTransaction(AID);
		}
		
		// CommitApplicationTransaction
		public void CommitApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginCommitApplicationTransaction, "Begin CommitApplicationTransaction");
				#endif
				ApplicationTransactionUtility.CommitApplicationTransaction(this, AID);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndCommitApplicationTransaction, "End CommitApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.RollbackApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			RollbackApplicationTransaction(AID);
		}
		
		// RollbackApplicationTransaction
		public void RollbackApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginRollbackApplicationTransaction, "Begin RollbackApplicationTransaction");
				#endif
				ApplicationTransactionUtility.RollbackApplicationTransaction(this, AID);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndRollbackApplicationTransaction, "End RollbackApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}

		void IRemoteServerProcess.JoinApplicationTransaction(Guid AID, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			JoinApplicationTransaction(AID, AIsInsert);
		}
		
		public void JoinApplicationTransaction(Guid AID, bool AIsInsert)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginJoinApplicationTransaction, "Begin JoinApplicationTransaction");
				#endif
				ApplicationTransactionUtility.JoinApplicationTransaction(this, AID);
				FApplicationTransactionID = AID;
				FIsInsert = AIsInsert;
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndJoinApplicationTransaction, "End JoinApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.LeaveApplicationTransaction(ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			LeaveApplicationTransaction();
		}
		
		public void LeaveApplicationTransaction()
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginLeaveApplicationTransaction, "Begin LeaveApplicationTransaction");
				#endif
				ApplicationTransactionUtility.LeaveApplicationTransaction(this);
				FApplicationTransactionID = Guid.Empty;
				FIsInsert = false;
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndLeaveApplicationTransaction, "End LeaveApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}

		// HandlerDepth
		protected int FHandlerDepth;
		/// <summary>Indicates whether the process has entered an event handler.</summary>
		public bool InHandler { get { return FHandlerDepth > 0; } }
		
		public void PushHandler()
		{
			lock (this)
			{
				FHandlerDepth++;
			}
		}
		
		public void PopHandler()
		{
			lock (this)
			{
				FHandlerDepth--;
			}
		}
        
		protected int FTimeStampCount = 0;
		/// <summary>Indicates whether time stamps should be affected by alter and drop table variable and operator statements.</summary>
		public bool ShouldAffectTimeStamp { get { return FTimeStampCount == 0; } }
		
		public void EnterTimeStampSafeContext()
		{
			FTimeStampCount++;
		}
		
		public void ExitTimeStampSafeContext()
		{
			FTimeStampCount--;
		}
		
		// Execution
		private ServerPlans FExecutingPlans;

		private System.Threading.Thread FExecutingThread;
		public System.Threading.Thread ExecutingThread { get { return FExecutingThread; } }
		
		private int FExecutingThreadCount = 0;
		
		public ServerPlanBase ExecutingPlan
		{
			get 
			{
				if (FExecutingPlans.Count == 0)
					throw new ServerException(ServerException.Codes.NoExecutingPlan);
				return FExecutingPlans[FExecutingPlans.Count - 1];
			}
		}
		
		public ServerPlanBase RootExecutingPlan
		{
			get
			{
				if (FExecutingPlans.Count <= 1)
					return ExecutingPlan;
				return FExecutingPlans[1];
			}
		}
		
		public void PushExecutingPlan(ServerPlanBase APlan)
		{
			lock (this)
			{
				FExecutingPlans.Add(APlan);
			}
		}
		
		public void PopExecutingPlan(ServerPlanBase APlan)
		{
			lock (this)
			{
				if (ExecutingPlan != APlan)
					throw new ServerException(ServerException.Codes.PlanNotExecuting, APlan.ID.ToString());
				FExecutingPlans.DisownAt(FExecutingPlans.Count - 1);
			}
		}
		
		public void Start(ServerPlanBase APlan, DataParams AParams)
		{
			PushExecutingPlan(APlan);
			try
			{
				if (AParams != null)
					foreach (DataParam LParam in AParams)
						FContext.Push(new DataVar(LParam.Name, LParam.DataType, (LParam.Modifier == Modifier.In ? ((LParam.Value == null) ? null : LParam.Value.Copy()) : LParam.Value)));
			}
			catch
			{
				PopExecutingPlan(APlan);
				throw;
			}
		}
		
		public void Stop(ServerPlanBase APlan, DataParams AParams)
		{
			try
			{
				if (AParams != null)
					for (int LIndex = AParams.Count - 1; LIndex >= 0; LIndex--)
					{
						DataVar LDataVar = FContext.Pop();
						if (AParams[LIndex].Modifier != Modifier.In)
							AParams[LIndex].Value = LDataVar.Value;
					}
			}
			finally
			{
				PopExecutingPlan(APlan);
			}
		}
		
		public DataVar Execute(ServerPlanBase APlan, PlanNode ANode, DataParams AParams)
		{	
			DataVar LResult;
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginExecute, "Begin Execute");
			#endif
			Start(APlan, AParams);
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				LResult = ANode.Execute(this);
				APlan.Plan.Statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			finally
			{
				Stop(APlan, AParams);
			}
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndExecute, "End Execute");
			#endif
			return LResult;
		}
		
		// SyncHandle is used to synchronize CLI calls to the process, ensuring that only one call originating in the CLI is allowed to run through the process at a time
		private object FSyncHandle = new System.Object();
		public object SyncHandle { get { return FSyncHandle; } }

		// ExecutionSyncHandle is used to synchronize server-level execution management services involving the executing thread of the process
		private object FExecutionSyncHandle = new System.Object();
		public object ExecutionSyncHandle { get { return FExecutionSyncHandle; } }
		
		// StopError is used to indicate that a system level stop error has occurred on the process, and all transactions should be rolled back
		private bool FStopError = false;
		public bool StopError 
		{ 
			get { return FStopError; } 
			set { FStopError = value; } 
		}
		
		private bool FIsAborted = false;
		public bool IsAborted
		{
			get { return FIsAborted; }
			set { lock (FExecutionSyncHandle) { FIsAborted = value; } }
		}
		
		public void CheckAborted()
		{
			if (FIsAborted)
				throw new ServerException(ServerException.Codes.ProcessAborted);
		}
		
		public bool IsRunning { get { return FExecutingThreadCount > 0; } }
		
		internal void BeginCall()
		{
			Monitor.Enter(FSyncHandle);
			try
			{
				if ((FExecutingThreadCount == 0) && !ServerSession.User.IsAdminUser())
					ServerSession.Server.BeginProcessCall(this);
				Monitor.Enter(FExecutionSyncHandle);
				try
				{
					#if !DISABLE_PERFORMANCE_COUNTERS
					if (ServerSession.Server.FRunningProcessCounter != null)
						ServerSession.Server.FRunningProcessCounter.Increment();
					#endif

					FExecutingThreadCount++;
					if (FExecutingThreadCount == 1)
					{
						FExecutingThread = System.Threading.Thread.CurrentThread;
						FIsAborted = false;
						//FExecutingThread.ApartmentState = ApartmentState.STA; // This had no effect, the ADO thread lock is still occurring
						ThreadUtility.SetThreadName(FExecutingThread, String.Format("ServerProcess {0}", FProcessID.ToString()));
					}
				}
				finally
				{
					Monitor.Exit(FExecutionSyncHandle);
				}
			}
			catch
			{
				try
				{
					if (FExecutingThreadCount > 0)
					{
						Monitor.Enter(FExecutionSyncHandle);
						try
						{
							#if !DISABLE_PERFORMANCE_COUNTERS
							if (ServerSession.Server.FRunningProcessCounter != null)
								ServerSession.Server.FRunningProcessCounter.Decrement();
							#endif

							FExecutingThreadCount--;
							if (FExecutingThreadCount == 0)
							{
								ThreadUtility.SetThreadName(FExecutingThread, null);
								FExecutingThread = null;
							}
						}
						finally
						{
							Monitor.Exit(FExecutionSyncHandle);
						}
					}
				}
				finally
				{
					Monitor.Exit(FSyncHandle);
				}
                throw;
            }
		}
		
		internal void EndCall()
		{
			try
			{
				if ((FExecutingThreadCount == 1) && !ServerSession.User.IsAdminUser())
					ServerSession.Server.EndProcessCall(this);
			}
			finally
			{
				try
				{
					Monitor.Enter(FExecutionSyncHandle);
					try
					{
						#if !DISABLE_PERFORMANCE_COUNTERS
						if (ServerSession.Server.FRunningProcessCounter != null)
							ServerSession.Server.FRunningProcessCounter.Decrement();
						#endif

						FExecutingThreadCount--;
						if (FExecutingThreadCount == 0)
						{
							ThreadUtility.SetThreadName(FExecutingThread, null);
							FExecutingThread = null;
						}
					}
					finally
					{
						Monitor.Exit(FExecutionSyncHandle);
					}
				}
				finally
				{
					Monitor.Exit(FSyncHandle);
				}
			}
		}
		
		internal int BeginTransactionalCall()
		{
			BeginCall();
			try
			{
				FStopError = false;
				int LNestingLevel = FTransactions.Count;
				if ((FTransactions.Count == 0) && UseImplicitTransactions)
					InternalBeginTransaction(DefaultIsolationLevel);
				if (FTransactions.Count > 0)
					FTransactions.CurrentTransaction().Prepared = false;
				return LNestingLevel;
			}
			catch
			{
				EndCall();
				throw;
			}
		}
		
		internal void EndTransactionalCall(int ANestingLevel, Exception AException)
		{
			try
			{
				if (StopError)
					while (FTransactions.Count > 0)
						try
						{
							InternalRollbackTransaction();
						}
						catch (Exception E)
						{
							ServerSession.Server.LogError(E);
						}
				
				if (UseImplicitTransactions)
				{
					Exception LCommitException = null;
					while (FTransactions.Count > ANestingLevel)
						if ((AException != null) || (LCommitException != null))
							InternalRollbackTransaction();
						else
						{
							try
							{
								InternalPrepareTransaction();
								InternalCommitTransaction();
							}
							catch (Exception LException)
							{
								LCommitException = LException;
								InternalRollbackTransaction();
							}
						}

					if (LCommitException != null)
						throw LCommitException;
				}
			}
			catch (Exception E)
			{
				if (AException != null)
					// TODO: Should this actually attach the rollback exception as context information to the server exception object?
					throw new ServerException(ServerException.Codes.RollbackError, AException, E.ToString());
				else
					throw;
			}
			finally
			{
				EndCall();
			}
		}
		
		private Exception WrapException(Exception AException)
		{
			return FServerSession.WrapException(AException);
		}

		// ActiveCursor
		protected ServerCursorBase FActiveCursor;
		public ServerCursorBase ActiveCursor { get { return FActiveCursor; } }
		
		public void SetActiveCursor(ServerCursorBase AActiveCursor)
		{
			/*
			if (FActiveCursor != null)
				throw new ServerException(ServerException.Codes.PlanCursorActive);
			FActiveCursor = AActiveCursor;
			*/
		}
		
		public void ClearActiveCursor()
		{
			//FActiveCursor = null;
		}
		
		// Parameter Translation
		public DataParams RemoteParamsToDataParams(RemoteParam[] AParams)
		{
			if ((AParams != null) && (AParams.Length > 0))
			{
				DataParams LParams = new DataParams();
				foreach (RemoteParam LRemoteParam in AParams)
					LParams.Add(new DataParam(LRemoteParam.Name, (Schema.ScalarType)ServerSession.Server.Catalog[LRemoteParam.TypeName], (Modifier)LRemoteParam.Modifier));//hack: cast to fix fixup error

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
					LRowType.Columns.Add(new Schema.Column(AParams.Params[LIndex].Name, (Schema.ScalarType)ServerSession.Server.Catalog[AParams.Params[LIndex].TypeName]));
					
				Row LRow = new Row(this, LRowType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = AParams.Data.Data;

					for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
						if (LRow.HasValue(LIndex))
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow[LIndex].DataType, (Modifier)AParams.Params[LIndex].Modifier, LRow[LIndex].Copy()));//Hack: cast to fix fixup error
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
					
				Row LRow = new Row(this, LRowType);
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
		
		// Catalog
		public Schema.DataTypes DataTypes { get { return FServerSession.Server.Catalog.DataTypes; } }

		string IRemoteServerProcess.GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			ACacheTimeStamp = ServerSession.Server.CacheTimeStamp;

			Schema.Catalog LCatalog = new Schema.Catalog();
			LCatalog.IncludeDependencies(this, Plan.Catalog, Plan.Catalog[AName], EmitMode.ForRemote);
			
			#if LOGCACHEEVENTS
			ServerSession.Server.LogMessage(String.Format("Getting catalog for data type '{0}'.", AName));
			#endif

			ACacheChanged = true;
			string[] LRequiredObjects = ServerSession.Server.CatalogCaches.GetRequiredObjects(ServerSession, LCatalog, ACacheTimeStamp, out AClientCacheTimeStamp);
			if (LRequiredObjects.Length > 0)
			{
				string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(LCatalog.EmitStatement(this, EmitMode.ForRemote, LRequiredObjects));
				return LCatalogString;
			}
			return String.Empty;
		}
		
		// Loading
		private LoadingContexts FLoadingContexts;
		
		public void PushLoadingContext(LoadingContext AContext)
		{
			if (FLoadingContexts == null)
				FLoadingContexts = new LoadingContexts();
				
			if (AContext.LibraryName != String.Empty)
			{
				AContext.FCurrentLibrary = ServerSession.CurrentLibrary;
				ServerSession.CurrentLibrary = ServerSession.Server.Catalog.LoadedLibraries[AContext.LibraryName];
			}
			
			AContext.FSuppressWarnings = SuppressWarnings;
			SuppressWarnings = true;

			if (!AContext.IsLoadingContext)
			{
				LoadingContext LCurrentLoadingContext = CurrentLoadingContext();
				if ((LCurrentLoadingContext != null) && !LCurrentLoadingContext.IsInternalContext)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidLoadingContext, ErrorSeverity.System);
			}

			FLoadingContexts.Add(AContext);
		}
		
		public void PopLoadingContext()
		{
			LoadingContext LContext = FLoadingContexts[FLoadingContexts.Count - 1];
			FLoadingContexts.RemoveAt(FLoadingContexts.Count - 1);
			if (LContext.LibraryName != String.Empty)
				ServerSession.CurrentLibrary = LContext.FCurrentLibrary;
			SuppressWarnings = LContext.FSuppressWarnings;
		}
		
		private int FReconciliationDisabledCount = 0;
		public void DisableReconciliation()
		{
			FReconciliationDisabledCount++;
		}
		
		public void EnableReconciliation()
		{
			FReconciliationDisabledCount--;
		}
		
		public bool IsReconciliationEnabled()
		{
			return FReconciliationDisabledCount <= 0;
		}
		
		public int SuspendReconciliationState()
		{
			int LResult = FReconciliationDisabledCount;
			FReconciliationDisabledCount = 0;
			return LResult;
		}
		
		public void ResumeReconciliationState(int AReconciliationDisabledCount)
		{
			FReconciliationDisabledCount = AReconciliationDisabledCount;
		}
		
		/// <summary>Returns true if the Server-level loading flag is set, or this process is in a loading context</summary>
		public bool IsLoading()
		{
			return ServerSession.Server.LoadingCatalog || InLoadingContext();
		}
		
		/// <summary>Returns true if this process is in a loading context.</summary>
		public bool InLoadingContext()
		{
			return (FLoadingContexts != null) && (FLoadingContexts.Count > 0) && (FLoadingContexts[FLoadingContexts.Count - 1].IsLoadingContext);
		}
		
		public LoadingContext CurrentLoadingContext()
		{
			if ((FLoadingContexts != null) && FLoadingContexts.Count > 0)
				return FLoadingContexts[FLoadingContexts.Count - 1];
			return null;
		}
	}

	// ServerProcesses
	public class ServerProcesses : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerProcess))
				throw new ServerException(ServerException.Codes.ServerProcessContainer);
		}
		
		public new ServerProcess this[int AIndex]
		{
			get { return (ServerProcess)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
		
		public ServerProcess GetProcess(int AProcessID)
		{
			foreach (ServerProcess LProcess in this)
				if (LProcess.ProcessID == AProcessID)
					return LProcess;
			throw new ServerException(ServerException.Codes.ProcessNotFound, AProcessID);
		}
	}
    
	#if OnExpression
	// RemoteSession    
    public class RemoteSession : Disposable
    {
		public RemoteSession(ServerSession ASession, Schema.ServerLink AServerLink)
		{
			FServerSession = ASession;
			FServerLink = AServerLink;
			FRemoteServer = ServerFactory.Connect(FServerLink.ServerURI);
			FRemoteSessionInfo = FServerLink.GetSessionInfo(FServerSession.User);
			if (FRemoteSessionInfo == null)
				FRemoteSessionInfo = new SessionInfo(FServerSession.SessionInfo.UserID, FServerSession.SessionInfo.Password);
			FRemoteServerSession = FRemoteServer.Connect(FRemoteSessionInfo);
			FRemotePlans = new RemotePlans();
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRemotePlans != null)
			{
				FRemotePlans.Dispose();
				FRemotePlans = null;
			}
			
			if (FRemoteServerSession != null)
			{
				FRemoteServer.Disconnect(FRemoteServerSession);
				FRemoteServerSession = null;
				FRemoteSessionInfo = null;
			}
			
			if (FRemoteServer != null)
			{
				ServerFactory.Disconnect(FRemoteServer);
				FRemoteServer = null;
			}
			
			FServerSession = null;
			
			base.Dispose(ADisposing);
		}

		private ServerSession FServerSession;
		public ServerSession ServerSession { get { return FServerSession; } }

		private Schema.ServerLink FServerLink;
		public Schema.ServerLink ServerLink { get { return FServerLink; } }
		
		private IServer FRemoteServer;
		private SessionInfo FRemoteSessionInfo;

		private IServerSession FRemoteServerSession;
		public IServerSession RemoteServerSession { get { return FRemoteServerSession; } }
		
		private RemotePlans FRemotePlans;
		public RemotePlans RemotePlans { get { return FRemotePlans; } }
		
		public RemotePlan GetPlan(OnNode AOnNode)
		{
			int LIndex = FRemotePlans.IndexOf(AOnNode);
			if (LIndex < 0)
			{
				RemotePlan LRemotePlan = new RemotePlan(this, AOnNode);
				try
				{
					FRemotePlans.Add(LRemotePlan);
					return LRemotePlan;
				}
				catch
				{
					LRemotePlan.Dispose();
					throw;
				}
			}
			else
				return FRemotePlans[LIndex];
		}
    }
    
	public class RemoteSessions : DisposableTypedList
	{
		public RemoteSessions() : base()
		{
			FItemType = typeof(RemoteSession);
			FItemsOwned = true;
		}
		
		public new RemoteSession this[int AIndex]
		{
			get { return (RemoteSession)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(Schema.ServerLink ALink)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].ServerLink.Equals(ALink))
					return LIndex;
			return -1;
		}
		
		public bool Contains(Schema.ServerLink ALink)
		{
			return IndexOf(ALink) >= 0;
		}
	}
	
	public class RemotePlan : Disposable
	{
		public RemotePlan(RemoteSession ASession, OnNode AOnNode) : base()
		{
			FSession = ASession;
			FOnNode = AOnNode;
			FRemoteProcess = FSession.RemoteServerSession.StartProcess();
			FRemoteServerPlan = FRemoteProcess.PrepareExpression(FOnNode.Expression, null);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRemoteCursor != null)
			{
				FRemoteServerPlan.Close(FRemoteCursor);
				FRemoteCursor = null;
			}
			
			if (FRemoteServerPlan != null)
			{
				FRemoteProcess.UnprepareExpression(FRemoteServerPlan);
				FRemoteServerPlan = null;
			}
			
			if (FRemoteProcess != null)
			{
				FSession.RemoteServerSession.StopProcess(FRemoteProcess);
				FRemoteProcess = null;
			}
			
			FSession = null;
			FOnNode = null;
			
			base.Dispose(ADisposing);
		}
		
		private RemoteSession FSession;
		public RemoteSession Session { get { return FSession; } }
		
		protected OnNode FOnNode;
		public OnNode OnNode { get { return FOnNode; } }

		private IServerProcess FRemoteProcess;
		public IServerProcess RemoteProcess { get { return FRemoteProcess; } }

		protected IServerExpressionPlan FRemoteServerPlan;
		public IServerExpressionPlan RemoteServerPlan { get { return FRemoteServerPlan; } }

		protected IServerCursor FRemoteCursor;
		public IServerCursor RemoteCursor
		{
			get
			{
				if (FRemoteCursor == null)
					FRemoteCursor = FRemoteServerPlan.Open(null);
				return FRemoteCursor;
			}
		}
	}

	public class RemotePlans : DisposableTypedList
	{
		public RemotePlans() : base()
		{
			FItemType = typeof(RemotePlan);
			FItemsOwned = true;
		}

		public new RemotePlan this[int AIndex]
		{
			get { return (RemotePlan)base[AIndex]; } 
			set { base[AIndex] = value; }
		}
		
		public int IndexOf(OnNode AOnNode)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].OnNode == AOnNode)
					return LIndex;
			return -1;
		}
		
		public bool Contains(OnNode AOnNode)
		{
			return IndexOf(AOnNode) >= 0;
		}
	}
	#endif
	
	public class CursorContext : System.Object
	{
		public CursorContext() : base() {}
		public CursorContext(CursorType ACursorType, CursorCapability ACapabilities, CursorIsolation AIsolation) : base()
		{
			FCursorType = ACursorType;
			FCursorCapabilities = ACapabilities;
			FCursorIsolation = AIsolation;
		}
		// CursorType
		private CursorType FCursorType;
		public CursorType CursorType
		{
			get { return FCursorType; }
			set { FCursorType = value; }
		}
		
		// CursorCapabilities
		private CursorCapability FCursorCapabilities;
		public CursorCapability CursorCapabilities
		{
			get { return FCursorCapabilities; }
			set { FCursorCapabilities = value; }
		}
		
		// CursorIsolation
		private CursorIsolation FCursorIsolation;
		public CursorIsolation CursorIsolation
		{
			get { return FCursorIsolation; }
			set { FCursorIsolation = value; }
		}
	}
	
	public class CursorContexts : List<CursorContext> { }

	public enum StatementType { Select, Insert, Update, Delete, Assignment }
	
	public class StatementContext : System.Object
	{
		public StatementContext(StatementType AStatementType) : base()
		{
			FStatementType = AStatementType;
		}
		
		private StatementType FStatementType;
		public StatementType StatementType { get { return FStatementType; } }
	}
	
	public class StatementContexts : List<StatementContext> { }

	public class LoadingContext : System.Object
	{
		public LoadingContext(Schema.User AUser, string ALibraryName) : base()
		{
			FUser = AUser;
			FLibraryName = ALibraryName;
		}
		
		public LoadingContext(Schema.User AUser, bool AIsInternalContext)
		{
			FUser = AUser;
			FLibraryName = String.Empty;
			FIsInternalContext = AIsInternalContext;
		}
		
		public LoadingContext(Schema.User AUser, string ALibraryName, bool AIsLoadingContext)
		{
			FUser = AUser;
			FLibraryName = ALibraryName;
			FIsLoadingContext = AIsLoadingContext;
		}
		
		private Schema.User FUser;
		public Schema.User User { get { return FUser; } }
		
		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private bool FIsInternalContext = false;
		/// <summary>Indicates whether this is a true loading context, or an internal context entered to prevent logging of DDL.</summary>
		/// <remarks>
		/// Because loading contexts are non-logging, they are also used by the server to build internal management structures such
		/// as constraint check tables. However, these contexts may result in the creation of objects that should be logged, such
		/// as sorts for types involved in the constraints. This flag indicates that this context is an internal context and that
		/// a logging context may be pushed on top of it.
		/// </remarks>
		public bool IsInternalContext { get { return FIsInternalContext; } }
		
		private bool FIsLoadingContext = true;
		/// <summary>Indicates whether the context is a loading context, or a context pushed to enable logging within a loading context.</summary>
		/// <remarks>
		/// Pushing a non-loading context is only allowed if the current loading context is an internal context, because it should be an error
		/// to create any logged objects as a result of the creation of a deserializing object.
		/// </remarks>
		public bool IsLoadingContext { get { return FIsLoadingContext; } }
		
		internal Schema.LoadedLibrary FCurrentLibrary;
		
		internal bool FSuppressWarnings;
	}
	
	public class LoadingContexts : List<LoadingContext> { }
	
	public class SecurityContext : System.Object
	{
		public SecurityContext(Schema.User AUser) : base()
		{
			FUser = AUser;
		}
		
		private Schema.User FUser;
		public Schema.User User { get { return FUser; } }
		internal void SetUser(Schema.User AUser)
		{
			FUser = AUser;
		}
	}
	
	public class SecurityContexts : List<SecurityContext> { }
	
	// Plan provides compile time state	for the compiler.
	public class Plan : Disposable
	{
		public Plan(ServerProcess AServerProcess) : base()
		{
			FServerProcess = AServerProcess;
			FCatalogLocks = new ArrayList();
			FSymbols = new Context(FServerProcess.Context.MaxStackDepth, FServerProcess.Context.MaxCallDepth);
			PushSecurityContext(new SecurityContext(FServerProcess.ServerSession.User));
			PushStatementContext(new StatementContext(StatementType.Select));
		}
		
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
								if (FStatementContexts.Count > 0)
									PopStatementContext();
									
							}
							finally
							{
								if (FSecurityContexts.Count > 0)
									PopSecurityContext();
							}
						}
						finally
						{
							if (FCatalogLocks != null)
							{
								ReleaseCatalogLocks();
								FCatalogLocks = null;
							}
						}
					}
					finally
					{
						if (FSymbols != null)
						{
							FSymbols.Dispose();
							FSymbols = null;
						}
					}
				}
				finally
				{
					if (FDevicePlans != null)
					{
						Schema.DevicePlan LDevicePlan;
						foreach (object LObject in FDevicePlans.Keys)
						{
							LDevicePlan = (Schema.DevicePlan)FDevicePlans[LObject];
							LDevicePlan.Device.Unprepare(LDevicePlan);
						}
						FDevicePlans = null;
					}
				}
			}
			finally
			{
				FServerProcess = null;

				base.Dispose(ADisposing);
			}
		}

		// Process        
		protected ServerProcess FServerProcess;
		public ServerProcess ServerProcess { get { return FServerProcess; } }
		
		public void BindToProcess(ServerProcess AProcess)
		{
			PopSecurityContext();
			PushSecurityContext(new SecurityContext(AProcess.ServerSession.User));
			FServerProcess = AProcess;
			
			// Reset execution statistics
			FStatistics.ExecuteTime = TimeSpan.Zero;
			FStatistics.DeviceExecuteTime = TimeSpan.Zero;
		}
		
		public void CheckCompiled()
		{
			if (FMessages.HasErrors)
				if (FMessages.Count == 1)
					throw FMessages[0];
				else
					throw new ServerException(ServerException.Codes.UncompiledPlan, FMessages.ToString(CompilerErrorLevel.NonFatal));
		}
		
		// Statistics
		private PlanStatistics FStatistics = new PlanStatistics();
		public PlanStatistics Statistics { get { return FStatistics; } }
		
		// DevicePlans
		private Hashtable FDevicePlans = new Hashtable();

		// GetDevicePlan
		public Schema.DevicePlan GetDevicePlan(PlanNode APlanNode)
		{
			object LDevicePlan = FDevicePlans[APlanNode];
			if (LDevicePlan == null)
			{
				FServerProcess.EnsureDeviceStarted(APlanNode.Device);
				Schema.DevicePlan LNewDevicePlan = APlanNode.Device.Prepare(this, APlanNode);
				AddDevicePlan(LNewDevicePlan);
				return LNewDevicePlan;
			}
			return (Schema.DevicePlan)LDevicePlan;
		}
		
		// AddDevicePlan
		public void AddDevicePlan(Schema.DevicePlan ADevicePlan)
		{
			if (!FDevicePlans.Contains(ADevicePlan.Node))
				FDevicePlans.Add(ADevicePlan.Node, ADevicePlan);
		}
		
		// CatalogLocks
		protected ArrayList FCatalogLocks; // cannot be a hash table because it must be able to contain multiple entries for the same LockID
		public void AcquireCatalogLock(Schema.Object AObject, LockMode AMode)
		{
			#if USECATALOGLOCKS
			LockID LLockID = new LockID(Server.CCatalogManagerID, AObject.Name);
			if (FServerProcess.ServerSession.Server.LockManager.LockImmediate(FServerProcess.ProcessID, LLockID, AMode))
				FCatalogLocks.Add(LLockID);
			else
				throw new RuntimeException(RuntimeException.Codes.UnableToLockObject, AObject);
			#endif
		}
		
		public void ReleaseCatalogLock(Schema.Object AObject)
		{
			#if USECATALOGLOCKS
			LockID LLockID = new LockID(Server.CCatalogManagerID, AObject.Name);
			if (FServerProcess.ServerSession.Server.LockManager.IsLocked(LLockID))
				FServerProcess.ServerSession.Server.LockManager.Unlock(FServerProcess.ProcessID, LLockID);
			#endif
		}
        
		protected void ReleaseCatalogLocks()
		{
			#if USECATALOGLOCKS
			for (int LIndex = FCatalogLocks.Count - 1; LIndex >= 0; LIndex--)
			{
				if (FServerProcess.ServerSession.Server.LockManager.IsLocked((LockID)FCatalogLocks[LIndex]))
					FServerProcess.ServerSession.Server.LockManager.Unlock(FServerProcess.ProcessID, (LockID)FCatalogLocks[LIndex]);
				FCatalogLocks.RemoveAt(LIndex);
			}
			#endif
		}

		// Symbols        
		protected Context FSymbols;
		public Context Symbols { get { return FSymbols; } }
        
		// LoopContext
		protected int FLoopCount;
		public bool InLoop { get { return FLoopCount > 0; } }
        
		public void EnterLoop() 
		{ 
			FLoopCount++; 
		}
        
		public void ExitLoop() 
		{ 
			FLoopCount--; 
		}
		
		// RowContext
		public bool InRowContext { get { return FSymbols.InRowContext; } }
		
		public void EnterRowContext()
		{
			FSymbols.PushFrame(true);
		}
		
		public void ExitRowContext()
		{
			FSymbols.PopFrame();
		}
		
		// ATCreationContext
		private int FATCreationContext;
		/// <summary>Indicates whether the current plan is executing a statement to create an A/T translated object.</summary>
		/// <remarks>
		/// This context is needed because it is not always the case that A/T objects will be being created (or recreated 
		/// such as when view references are reinferred) inside of an A/T. By checking for this context, we are ensured
		/// that things that should not be checked for A/T objects (such as errors about derived references not existing in
		/// adorn expressions, etc.,.) will not occur.
		/// </remarks>
		public bool InATCreationContext { get { return FATCreationContext > 0; } }
		
		public void PushATCreationContext()
		{
			FATCreationContext++;
		}
		
		public void PopATCreationContext()
		{
			FATCreationContext--;
		}
		
		/// <summary>Indicates whether time stamps should be affected by alter and drop table variable and operator statements.</summary>
		public bool ShouldAffectTimeStamp { get { return ServerProcess.ShouldAffectTimeStamp; } }
		
		public void EnterTimeStampSafeContext()
		{
			ServerProcess.EnterTimeStampSafeContext();
		}
		
		public void ExitTimeStampSafeContext()
		{
			ServerProcess.ExitTimeStampSafeContext();
		}
		
		protected int FTypeOfCount;
		public bool InTypeOfContext { get { return FTypeOfCount > 0; } }
		
		public void PushTypeOfContext()
		{
			FTypeOfCount++;
		}
		
		public void PopTypeOfContext()
		{
			FTypeOfCount--;
		}
		
		// TypeContext
		protected ArrayList FTypeStack = new ArrayList();
		
		public void PushTypeContext(Schema.IDataType ADataType)
		{
			FTypeStack.Add(ADataType);
		}
		
		public void PopTypeContext(Schema.IDataType ADataType)
		{
			Error.AssertFail(FTypeStack.Count > 0, "Type stack underflow");
			FTypeStack.RemoveAt(FTypeStack.Count - 1);
		}
		
		public bool InScalarTypeContext()
		{
			return (FTypeStack.Count > 0) && (FTypeStack[FTypeStack.Count - 1] is Schema.IScalarType);
		}
		
		public bool InRowTypeContext()
		{
			return (FTypeStack.Count > 0) && (FTypeStack[FTypeStack.Count - 1] is Schema.IRowType);
		}
		
		public bool InTableTypeContext()
		{
			return (FTypeStack.Count > 0) && (FTypeStack[FTypeStack.Count - 1] is Schema.ITableType);
		}

		public bool InListTypeContext()
		{
			return (FTypeStack.Count > 0) && (FTypeStack[FTypeStack.Count - 1] is Schema.IListType);
		}
		
		// CurrentStatement
		protected ArrayList FStatementStack = new ArrayList();
		
		public void PushStatement(Statement AStatement)
		{
			FStatementStack.Add(AStatement);
		}
		
		public void PopStatement()
		{
			Error.AssertFail(FStatementStack.Count > 0, "Statement stack underflow");
			FStatementStack.RemoveAt(FStatementStack.Count - 1);
		}
		
		/// <remarks>Returns the current statement in the abstract syntax tree being compiled.  Will return null if no statement is on the statement stack.</remarks>
		public Statement CurrentStatement()
		{
			if (FStatementStack.Count > 0)
				return (Statement)FStatementStack[FStatementStack.Count - 1];
			return null;
		}

		// CursorContext
		protected CursorContexts FCursorContexts = new CursorContexts();
		public void PushCursorContext(CursorContext AContext)
		{
			FCursorContexts.Add(AContext);
		}
        
		public void PopCursorContext()
		{
			FCursorContexts.RemoveAt(FCursorContexts.Count - 1);
		}
        
		public CursorContext GetDefaultCursorContext()
		{
			return new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.None);
		}
        
		public CursorContext CursorContext
		{
			get
			{
				if (FCursorContexts.Count > 0)
					return FCursorContexts[FCursorContexts.Count - 1];
				else
					return GetDefaultCursorContext();
			}
		}
		
		// StatementContext
		protected StatementContexts FStatementContexts = new StatementContexts();
		public void PushStatementContext(StatementContext AContext)
		{
			FStatementContexts.Add(AContext);
		}
		
		public void PopStatementContext()
		{
			FStatementContexts.RemoveAt(FStatementContexts.Count - 1);
		}
		
		public StatementContext GetDefaultStatementContext()
		{
			return new StatementContext(StatementType.Select);
		}
		
		public bool HasStatementContext { get { return FStatementContexts.Count > 0; } }

		public StatementContext StatementContext { get { return FStatementContexts[FStatementContexts.Count - 1]; } }
		
		// SecurityContext
		protected SecurityContexts FSecurityContexts = new SecurityContexts();
		public void PushSecurityContext(SecurityContext AContext)
		{
			FSecurityContexts.Add(AContext);
		}
		
		public void PopSecurityContext()
		{
			FSecurityContexts.RemoveAt(FSecurityContexts.Count - 1);
		}
		
		public bool HasSecurityContext { get { return FSecurityContexts.Count > 0; } }
		
		public SecurityContext SecurityContext { get { return FSecurityContexts[FSecurityContexts.Count - 1]; } }
		
		public void UpdateSecurityContexts(Schema.User AUser)
		{
			for (int LIndex = 0; LIndex < FSecurityContexts.Count; LIndex++)
				if (FSecurityContexts[LIndex].User.ID == AUser.ID)
					FSecurityContexts[LIndex].SetUser(AUser);
		}
		
		// A Temporary catalog where catalog objects are registered during compilation
		protected Schema.Catalog FPlanCatalog;
		public Schema.Catalog PlanCatalog
		{
			get
			{
				if (FPlanCatalog == null)
					FPlanCatalog = new Schema.Catalog();
				return FPlanCatalog;
			}
		}
		
		// A temporary list of session table variables created during compilation
		protected Schema.Objects FPlanSessionObjects;
		public Schema.Objects PlanSessionObjects 
		{
			get
			{
				if (FPlanSessionObjects  == null)
					FPlanSessionObjects = new Schema.Objects();
				return FPlanSessionObjects;
			}
		}
		
		protected Schema.Objects FPlanSessionOperators;
		public Schema.Objects PlanSessionOperators
		{
			get
			{
				if (FPlanSessionOperators == null)
					FPlanSessionOperators = new Schema.Objects();
				return FPlanSessionOperators;
			}
		}

		// Catalog		
		public Schema.Catalog Catalog { get { return ServerProcess.ServerSession.Server.Catalog; } }
		
		public Schema.User User { get { return SecurityContext.User; } }
		
		public Schema.LoadedLibrary CurrentLibrary { get { return ServerProcess.ServerSession.CurrentLibrary; } }
		
		public Schema.NameResolutionPath NameResolutionPath { get { return ServerProcess.ServerSession.CurrentLibrary.GetNameResolutionPath(ServerProcess.ServerSession.Server.SystemLibrary); } }
		
		public IStreamManager StreamManager { get { return (IStreamManager)ServerProcess; } }

		public Schema.Device TempDevice { get { return ServerProcess.ServerSession.Server.TempDevice; } }
		
		#if USEHEAPDEVICE
		public Schema.Device HeapDevice { get { return ServerProcess.ServerSession.Server.HeapDevice; } }
		#endif
		
		public CursorManager CursorManager { get { return ServerProcess.ServerSession.CursorManager; } }

		public string DefaultTagNameSpace { get { return String.Empty; } }
		
		public string DefaultDeviceName { get { return GetDefaultDeviceName(ServerProcess.ServerSession.CurrentLibrary.Name, false); } }
		
		public string GetDefaultDeviceName(string ALibraryName, bool AShouldThrow)
		{
			return GetDefaultDeviceName(Catalog.Libraries[ALibraryName], AShouldThrow);
		}
		
		protected string GetDefaultDeviceName(Schema.Library ALibrary, bool AShouldThrow)
		{
			if (ALibrary.DefaultDeviceName != String.Empty)
				return ALibrary.DefaultDeviceName;
			
			Schema.Libraries LLibraries = new Schema.Libraries();
			LLibraries.Add(ALibrary);
			return GetDefaultDeviceName(LLibraries, AShouldThrow);
		}
		
		protected string GetDefaultDeviceName(Schema.Libraries ALibraries, bool AShouldThrow)
		{
			while (ALibraries.Count > 0)
			{
				Schema.Library LLibrary = ALibraries[0];
				ALibraries.RemoveAt(0);
				
				string LDefaultDeviceName = String.Empty;
				Schema.Library LRequiredLibrary;
				foreach (Schema.LibraryReference LLibraryReference in LLibrary.Libraries)
				{
					LRequiredLibrary = Catalog.Libraries[LLibraryReference.Name];
					if (LRequiredLibrary.DefaultDeviceName != String.Empty)
					{
						if (LDefaultDeviceName != String.Empty)
							if (AShouldThrow)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.AmbiguousDefaultDeviceName, LLibrary.Name);
							else
								return String.Empty;
						LDefaultDeviceName = LRequiredLibrary.DefaultDeviceName;
					}
					else
						if (!ALibraries.Contains(LRequiredLibrary))
							ALibraries.Add(LRequiredLibrary);
				}
					
				if (LDefaultDeviceName != String.Empty)
					return LDefaultDeviceName;
			}
			
			return String.Empty;
		}

		public void CheckRight(string ARight)
		{
			if (!InATCreationContext)
				ServerProcess.CatalogDeviceSession.CheckUserHasRight(SecurityContext.User.ID, ARight);
		}
		
		public bool HasRight(string ARight)
		{
			return InATCreationContext || ServerProcess.CatalogDeviceSession.UserHasRight(SecurityContext.User.ID, ARight);
		}

		/// <summary>Raises an error if the current user is not authorized to administer the given user.</summary>		
		public void CheckAuthorized(string AUserID)
		{
			if (!User.IsAdminUser() || !User.IsSystemUser() || (String.Compare(AUserID, User.ID, true) != 0))
				CheckRight(Schema.RightNames.AlterUser);
		}
		
		// During compilation, the creation object stack maintains a context for the
		// object currently being created.  References into the catalog register dependencies
		// against this object as they are encountered by the compiler.
		protected ArrayList FCreationObjects;
		public void PushCreationObject(Schema.Object AObject)
		{
			if (FCreationObjects == null)
				FCreationObjects = new ArrayList();
			FCreationObjects.Add(AObject);
		}
		
		public void PopCreationObject()
		{
			FCreationObjects.RemoveAt(FCreationObjects.Count - 1);
		}
		
		public Schema.Object CurrentCreationObject()
		{
			if ((FCreationObjects == null) || (FCreationObjects.Count == 0))
				return null;
			return (Schema.Object)FCreationObjects[FCreationObjects.Count - 1];
		}
		
		public bool IsOperatorCreationContext
		{
			get
			{
				return 
					(FCreationObjects != null) && 
					(FCreationObjects.Count > 0) && 
					(FCreationObjects[FCreationObjects.Count - 1] is Schema.Operator);
			}
		}
		
		public void CheckClassDependency(ClassDefinition AClassDefinition)
		{
			if ((FCreationObjects != null) && (FCreationObjects.Count > 0))
			{
				Schema.Object LCreationObject = (Schema.Object)FCreationObjects[FCreationObjects.Count - 1];
				if 
				(
					(LCreationObject is Schema.CatalogObject) && 
					(((Schema.CatalogObject)LCreationObject).Library != null) &&
					(((Schema.CatalogObject)LCreationObject).SessionObjectName == null) && 
					(
						!(LCreationObject is Schema.TableVar) || 
						(((Schema.TableVar)LCreationObject).SourceTableName == null)
					)
				)
				{
					CheckClassDependency(((Schema.CatalogObject)LCreationObject).Library, AClassDefinition);
				}
			}
		}
		
		public void CheckClassDependency(Schema.LoadedLibrary ALibrary, ClassDefinition AClassDefinition)
		{
			Schema.RegisteredClass LClass = Catalog.ClassLoader.GetClass(AClassDefinition);
			if (!ALibrary.IsRequiredLibrary(LClass.Library))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.NonRequiredClassDependency, LClass.Name, ALibrary.Name, LClass.Library.Name); // TODO: Better exception
		}
		
		public void AttachDependencies(Schema.ObjectList ADependencies)
		{
			for (int LIndex = 0; LIndex < ADependencies.Count; LIndex++)
				AttachDependency(ADependencies.ResolveObject(ServerProcess, LIndex));
		}
		
		public void AttachDependency(Schema.Object AObject)
		{
			if (FCreationObjects == null)
				FCreationObjects = new ArrayList();
			if (FCreationObjects.Count > 0)
			{
				// If this is a generated object, attach the dependency to the generator, rather than the object directly.
				// Unless it is a device object, in which case the dependency should be attached to the generated object.
				if (AObject.IsGenerated && !(AObject is Schema.DeviceObject) && (AObject.GeneratorID >= 0))
					AObject = AObject.ResolveGenerator(ServerProcess);
					
				if (AObject is Schema.Property)
					AObject = ((Schema.Property)AObject).Representation;
					
				Schema.Object LObject = (Schema.Object)FCreationObjects[FCreationObjects.Count - 1];
				if ((LObject != AObject) && (!(AObject is Schema.Reference) || (LObject is Schema.TableVar)) && (!LObject.HasDependencies() || !LObject.Dependencies.Contains(AObject.ID)))
				{
					if (!ServerProcess.ServerSession.Server.IsRepository)
					{
						if 
						(
							(LObject.Library != null) &&
							!LObject.IsSessionObject &&
							!LObject.IsATObject
						)
						{
							Schema.LoadedLibrary LLibrary = LObject.Library;
							Schema.LoadedLibrary LDependentLibrary = AObject.Library;
							if (LDependentLibrary == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NonLibraryDependency, LObject.Name, LLibrary.Name, AObject.Name);
							if (!LLibrary.IsRequiredLibrary(LDependentLibrary) && (String.Compare(LDependentLibrary.Name, Server.CSystemLibraryName, false) != 0)) // Ignore dependencies to the system library, these are implicitly available
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NonRequiredLibraryDependency, LObject.Name, LLibrary.Name, AObject.Name, LDependentLibrary.Name);
						}
						
						// if LObject is not a session object or an AT object, AObject cannot be a session object
						if ((LObject is Schema.CatalogObject) && !LObject.IsSessionObject && !LObject.IsATObject && AObject.IsSessionObject)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.SessionObjectDependency, LObject.Name, ((Schema.CatalogObject)AObject).SessionObjectName);
								
						// if LObject is not a generated or an AT object or a plan object (Name == SessionObjectName), AObject cannot be an AT object
						if (((LObject is Schema.CatalogObject) && (((Schema.CatalogObject)LObject).SessionObjectName != LObject.Name)) && !LObject.IsGenerated && !LObject.IsATObject && AObject.IsATObject)
							if (AObject is Schema.TableVar)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.ApplicationTransactionObjectDependency, LObject.Name, ((Schema.TableVar)AObject).SourceTableName);
							else
								throw new Schema.SchemaException(Schema.SchemaException.Codes.ApplicationTransactionObjectDependency, LObject.Name, ((Schema.Operator)AObject).SourceOperatorName);
					}
							
					Error.AssertFail(AObject.ID > 0, "Object {0} does not have an object id and cannot be tracked as a dependency.", AObject.Name);
					LObject.AddDependency(AObject);
				}
			}
		}
		
		public void SetIsLiteral(bool AIsLiteral)
		{		
			if ((FCreationObjects != null) && (FCreationObjects.Count > 0))
			{
				Schema.Operator LOperator = FCreationObjects[FCreationObjects.Count - 1] as Schema.Operator;
				if (LOperator != null)
					LOperator.IsLiteral = LOperator.IsLiteral && AIsLiteral;
			}
		}
		
		public void SetIsFunctional(bool AIsFunctional)
		{
			if ((FCreationObjects != null) && (FCreationObjects.Count > 0))
			{
				Schema.Operator LOperator = FCreationObjects[FCreationObjects.Count - 1] as Schema.Operator;
				if (LOperator != null)
					LOperator.IsFunctional = LOperator.IsFunctional && AIsFunctional;
			}
		}
		
		public void SetIsDeterministic(bool AIsDeterministic)
		{
			if ((FCreationObjects != null) && (FCreationObjects.Count > 0))
			{
				Schema.Operator LOperator = FCreationObjects[FCreationObjects.Count - 1] as Schema.Operator;
				if (LOperator != null)
					LOperator.IsDeterministic = LOperator.IsDeterministic && AIsDeterministic;
			}
		}
		
		public void SetIsRepeatable(bool AIsRepeatable)
		{
			if ((FCreationObjects != null) && (FCreationObjects.Count > 0))
			{
				Schema.Operator LOperator = FCreationObjects[FCreationObjects.Count - 1] as Schema.Operator;
				if (LOperator != null)
					LOperator.IsRepeatable = LOperator.IsRepeatable && AIsRepeatable;
			}
		}
		
		public void SetIsNilable(bool AIsNilable)
		{
			if ((FCreationObjects != null) && (FCreationObjects.Count > 0))
			{
				Schema.Operator LOperator = FCreationObjects[FCreationObjects.Count - 1] as Schema.Operator;
				if (LOperator != null)
					LOperator.IsNilable = LOperator.IsNilable || AIsNilable;
			}
		}
		
		// True if the current expression is being evaluated as the target of an assignment		
		public bool IsAssignmentTarget
		{
			get 
			{ 
				return (StatementContext.StatementType == StatementType.Assignment);
			}
		}
		
		// Messages
		private CompilerMessages FMessages = new CompilerMessages();
		public CompilerMessages Messages { get { return FMessages; } }
		
		#if ACCUMULATOR
		// Performance Tracking
		public long Accumulator = 0;
		#endif
	}

	public abstract class ServerPlanBase : ServerChildObject, IServerPlanBase
	{        
		protected internal ServerPlanBase(ServerProcess AProcess) : base()
		{
			FProcess = AProcess;
			FPlan = new Plan(AProcess);

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FProcess.ServerSession.Server.FPlanCounter != null)
				FProcess.ServerSession.Server.FPlanCounter.Increment();
			#endif
		}
		
		private bool FDisposed;
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if (FActiveCursor != null)
						FActiveCursor.Dispose();
				}
				finally
				{
					if (FPlan != null)
					{
						FPlan.Dispose();
						FPlan = null;
					}
					
					if (!FDisposed)
					{
						#if !DISABLE_PERFORMANCE_COUNTERS
						if (FProcess.ServerSession.Server.FPlanCounter != null)
							FProcess.ServerSession.Server.FPlanCounter.Decrement();
						#endif
						FDisposed = true;
					}
				}
			}
			finally
			{
				FCode = null;
				FDataType = null;
				FProcess = null;
	            
				base.Dispose(ADisposing);
			}
		}

		// ID		
		private Guid FID = Guid.NewGuid();
		public Guid ID { get { return FID; } }
		
		// CachedPlanHeader
		private CachedPlanHeader FHeader;
		public CachedPlanHeader Header
		{ 
			get { return FHeader; } 
			set { FHeader = value; }
		}
		
		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp; // Server.PlanCacheTimeStamp when the plan was compiled

		// Plan		
		private Plan FPlan;
		public Plan Plan { get { return FPlan; } }
		
		// Statistics
		public PlanStatistics Statistics { get { return FPlan.Statistics; } }
		
		// Code
		protected PlanNode FCode;
		public PlanNode Code
		{
			get { return FCode; }
			set { FCode = value; }
		}
		
		// Process        
		protected ServerProcess FProcess;
		public ServerProcess ServerProcess { get { return FProcess; } }
		
		public void BindToProcess(ServerProcess AProcess)
		{
			FProcess = AProcess;
			FPlan.BindToProcess(AProcess);
			FCode.BindToProcess(AProcess.Plan);
		}
		
		// ActiveCursor
		protected ServerCursorBase FActiveCursor;
		public ServerCursorBase ActiveCursor { get { return FActiveCursor; } }
		
		public void SetActiveCursor(ServerCursorBase AActiveCursor)
		{
			if (FActiveCursor != null)
				throw new ServerException(ServerException.Codes.PlanCursorActive);
			FActiveCursor = AActiveCursor;
			FProcess.SetActiveCursor(FActiveCursor);
		}
		
		public void ClearActiveCursor()
		{
			FActiveCursor = null;
			FProcess.ClearActiveCursor();
		}
		
		protected Schema.IDataType FDataType;
		public Schema.IDataType DataType 
		{ 
			get 
			{ 
				CheckCompiled();
				return FDataType; 
			} 
			set { FDataType = value; } 
		}
		
		public virtual Statement EmitStatement()
		{
			CheckCompiled();
			return FCode.EmitStatement(EmitMode.ForCopy);
		}

		public void WritePlan(System.Xml.XmlWriter AWriter)
		{
			CheckCompiled();
			FCode.WritePlan(AWriter);
		}
		
		protected Exception WrapException(Exception AException)
		{
			return FProcess.ServerSession.WrapException(AException);
		}
		
		public void CheckCompiled()
		{
			FPlan.CheckCompiled();
		}
	}
	
	// ServerPlans    
	public class ServerPlans : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerPlanBase))
				throw new ServerException(ServerException.Codes.ServerPlanContainer);
		}
		
		public new ServerPlanBase this[int AIndex]
		{
			get { return (ServerPlanBase)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
    
	public class ServerScript : ServerChildObject, IServerScript, IRemoteServerScript
	{
		internal ServerScript(ServerProcess AProcess, string AScript) : base()
		{
			FProcess = AProcess;
			FBatches = new ServerBatches();
			FMessages = new ParserMessages();
			FScript = FProcess.ParseScript(AScript, FMessages);
			if ((FScript is Block) && (((Block)FScript).Statements.Count > 0))
			{
				Block LBlock = (Block)FScript;
						
				for (int LIndex = 0; LIndex < LBlock.Statements.Count; LIndex++)
					FBatches.Add(new ServerBatch(this, LBlock.Statements[LIndex]));
			}
			else
				FBatches.Add(new ServerBatch(this, FScript));
		}
		
		protected void UnprepareBatches()
		{
			Exception LException = null;
			while (FBatches.Count > 0)
			{
				try
				{
					FBatches.DisownAt(0).Dispose();
				}
				catch (Exception E)
				{
					LException = E;
				}			
			}
			if (LException != null)
				throw LException;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FBatches != null)
				{
					UnprepareBatches();
					FBatches.Dispose();
					FBatches = null;
				}
			}
			finally
			{
				FScript = null;
				FMessages = null;
				FProcess = null;
			
				base.Dispose(ADisposing);
			}
		}

		private Statement FScript;
		
		// Process        
		private ServerProcess FProcess;
		public ServerProcess Process { get { return FProcess; } }
		
		// Used by the ExecuteAsync node to indicate whether the process was implicitly created to run this script
		private bool FShouldCleanupProcess = false;
		public bool ShouldCleanupProcess { get { return FShouldCleanupProcess; } set { FShouldCleanupProcess = value; } }

		IServerProcess IServerScript.Process { get { return (IServerProcess)FProcess; } }
		
		IRemoteServerProcess IRemoteServerScript.Process  { get { return (IRemoteServerProcess)FProcess; } }

		// Messages
		private ParserMessages FMessages;
		ParserMessages IServerScript.Messages { get { return FMessages; } }
		
		Exception[] IRemoteServerScript.Messages
		{
			get
			{
				Exception[] LMessages = new Exception[FMessages.Count];
				FMessages.CopyTo(LMessages);
				return LMessages;
			}
		}
		
		public void CheckParsed()
		{
			if (FMessages.HasErrors())
				throw new ServerException(ServerException.Codes.UnparsedScript, FMessages.ToString());
		}
		
		// Batches
		private ServerBatches FBatches; 
		public ServerBatches Batches { get { return FBatches; } }

		IServerBatches IServerScriptBase.Batches { get { return FBatches; } }
		
		void IServerScript.Execute(DataParams AParams)
		{
			foreach (ServerBatch LBatch in FBatches)
				((IServerBatch)LBatch).Execute(AParams);
		}
		
		void IRemoteServerScript.Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			foreach (ServerBatch LBatch in FBatches)
				((IRemoteServerBatch)LBatch).Execute(ref AParams, FProcess.EmptyCallInfo());
		}
	}
	
	// ServerScripts
	public class ServerScripts : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerScript))
				throw new ServerException(ServerException.Codes.ServerScriptContainer);
		}
		
		public new ServerScript this[int AIndex]
		{
			get { return (ServerScript)base[AIndex]; } 
			set { base[AIndex] = value; }
		}
	}

	// ServerBatch
	public class ServerBatch : ServerChildObject, IServerBatch, IRemoteServerBatch
	{
		internal ServerBatch(ServerScript AScript, Statement ABatch) : base()
		{
			FScript = AScript;
			FBatch = ABatch;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FScript = null;
			FBatch = null;
			base.Dispose(ADisposing);
		}

		private ServerScript FScript;

		IServerScript IServerBatch.ServerScript { get { return (IServerScript)FScript; } }
		
		IRemoteServerScript IRemoteServerBatch.ServerScript { get { return (IRemoteServerScript)FScript; } }
		
		private Statement FBatch;
		
		public int Line { get { return FBatch.Line; } }
		
		public bool IsExpression()
		{
			return FBatch is SelectStatement;
		}
		
		public string GetText()
		{
			return new D4TextEmitter().Emit(FBatch);
		}
		
		void IServerBatch.Execute(DataParams AParams)
		{
			try
			{
				if (IsExpression())
				{
					IServerExpressionPlan LPlan = ((IServerBatch)this).PrepareExpression(AParams);
					try
					{
						if (LPlan.DataType is Schema.TableType)
							LPlan.Close(LPlan.Open(AParams));
						else
							LPlan.Evaluate(AParams).Dispose();
					}
					finally
					{
						((IServerBatch)this).UnprepareExpression(LPlan);
					}
				}
				else
				{
					IServerStatementPlan LPlan = ((IServerBatch)this).PrepareStatement(AParams);
					try
					{
						LPlan.Execute(AParams);
					}
					finally
					{
						((IServerBatch)this).UnprepareStatement(LPlan);
					}
				}
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IRemoteServerBatch.Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
		{
			FScript.Process.ProcessCallInfo(ACallInfo);
			try
			{
				RemoteParam[] LParams = new RemoteParam[AParams.Params == null ? 0 : AParams.Params.Length];
				for (int LIndex = 0; LIndex < (AParams.Params == null ? 0 : AParams.Params.Length); LIndex++)
				{
					LParams[LIndex].Name = AParams.Params[LIndex].Name;
					LParams[LIndex].TypeName = AParams.Params[LIndex].TypeName;
					LParams[LIndex].Modifier = AParams.Params[LIndex].Modifier;
				}
				if (IsExpression())
				{
					PlanDescriptor LPlanDescriptor;
					IRemoteServerExpressionPlan LPlan = ((IRemoteServerBatch)this).PrepareExpression(LParams, out LPlanDescriptor);
					try
					{
						LPlan.Close(LPlan.Open(ref AParams, out LPlanDescriptor.Statistics.ExecuteTime, FScript.Process.EmptyCallInfo()), FScript.Process.EmptyCallInfo());
						// TODO: Provide a mechanism for determining whether or not an expression should be evaluated or opened through the remoting CLI.
					}
					finally
					{
						((IRemoteServerBatch)this).UnprepareExpression(LPlan);
					}
				}
				else
				{
					PlanDescriptor LPlanDescriptor;
					IRemoteServerStatementPlan LPlan = ((IRemoteServerBatch)this).PrepareStatement(LParams, out LPlanDescriptor);
					try
					{
						LPlan.Execute(ref AParams, out LPlanDescriptor.Statistics.ExecuteTime, ACallInfo);
					}
					finally
					{
						((IRemoteServerBatch)this).UnprepareStatement(LPlan);
					}
				}
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IServerPlan IServerBatch.Prepare(DataParams AParams)
		{
			if (IsExpression())
				return ((IServerBatch)this).PrepareExpression(AParams);
			else
				return ((IServerBatch)this).PrepareStatement(AParams);
		}
		
		void IServerBatch.Unprepare(IServerPlan APlan)
		{
			if (APlan is IServerExpressionPlan)
				((IServerBatch)this).UnprepareExpression((IServerExpressionPlan)APlan);
			else
				((IServerBatch)this).UnprepareStatement((IServerStatementPlan)APlan);
		}
		
		IServerExpressionPlan IServerBatch.PrepareExpression(DataParams AParams)
		{
			try
			{
				FScript.CheckParsed();
				return (IServerExpressionPlan)((ServerProcess)FScript.Process).CompileExpression(FBatch, null, AParams);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IServerBatch.UnprepareExpression(IServerExpressionPlan APlan)
		{
			try
			{
				((IServerProcess)FScript.Process).UnprepareExpression(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IServerStatementPlan IServerBatch.PrepareStatement(DataParams AParams)
		{
			try
			{
				FScript.CheckParsed();
				return (IServerStatementPlan)((ServerProcess)FScript.Process).CompileStatement(FBatch, null, AParams);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IServerBatch.UnprepareStatement(IServerStatementPlan APlan)
		{
			try
			{
				((IServerProcess)FScript.Process).UnprepareStatement(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IRemoteServerPlan IRemoteServerBatch.Prepare(RemoteParam[] AParams)
		{
			PlanDescriptor LPlanDescriptor;
			if (IsExpression())
				return ((IRemoteServerBatch)this).PrepareExpression(AParams, out LPlanDescriptor);
			else
				return ((IRemoteServerBatch)this).PrepareStatement(AParams, out LPlanDescriptor);
		}
		
		void IRemoteServerBatch.Unprepare(IRemoteServerPlan APlan)
		{
			if (APlan  is IRemoteServerExpressionPlan)
				((IRemoteServerBatch)this).UnprepareExpression((IRemoteServerExpressionPlan)APlan);
			else
				((IRemoteServerBatch)this).UnprepareStatement((IRemoteServerStatementPlan)APlan);
		}
		
		IRemoteServerExpressionPlan IRemoteServerBatch.PrepareExpression(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			try
			{
				FScript.CheckParsed();
				IRemoteServerExpressionPlan LPlan =	(IRemoteServerExpressionPlan)((ServerProcess)FScript.Process).CompileRemoteExpression(FBatch, null, AParams);
				APlanDescriptor = ((ServerProcess)FScript.Process).GetPlanDescriptor(LPlan, AParams);
				return LPlan;
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IRemoteServerBatch.UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			try
			{
				((IRemoteServerProcess)FScript.Process).UnprepareExpression(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		IRemoteServerStatementPlan IRemoteServerBatch.PrepareStatement(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor)
		{
			try
			{
				FScript.CheckParsed();
				IRemoteServerStatementPlan LPlan = (IRemoteServerStatementPlan)((ServerProcess)FScript.Process).CompileRemoteStatement(FBatch, null, AParams);
				APlanDescriptor = ((ServerProcess)FScript.Process).GetPlanDescriptor(LPlan);
				return LPlan;
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
		
		void IRemoteServerBatch.UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			try
			{
				((IRemoteServerProcess)FScript.Process).UnprepareStatement(APlan);
			}
			catch (Exception E)
			{
				throw FScript.Process.ServerSession.WrapException(E);
			}
		}
	}
	
	// ServerBatches
	[Serializable]
	public class ServerBatches : ServerChildObjects, IServerBatches
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerBatch))
				throw new ServerException(ServerException.Codes.ServerBatchContainer);
		}
		
		public new ServerBatch this[int AIndex]
		{
			get { return (ServerBatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		IServerBatch IServerBatches.this[int AIndex]
		{
			get { return (IServerBatch)base[AIndex]; } 
			set { base[AIndex] = (ServerBatch)value; } 
		}
		
		public ServerBatch[] All
		{
			get
			{
				ServerBatch[] LArray = new ServerBatch[Count];
				for (int LIndex = 0; LIndex < Count; LIndex++)
					LArray[LIndex] = this[LIndex];
				return LArray;
			}
			set
			{
				foreach (ServerBatch LBatch in value)
					Add(LBatch);
			}
		}
	}

	// ServerPlan
	public abstract class ServerPlan : ServerPlanBase, IServerPlan
	{
		protected internal ServerPlan(ServerProcess AProcess) : base(AProcess) {}

		public IServerProcess Process  { get { return (IServerProcess)FProcess; } }
		
		public CompilerMessages Messages { get { return Plan.Messages; } }
	}

	// ServerStatementPlan	
	public class ServerStatementPlan : ServerPlan, IServerStatementPlan
	{
		public ServerStatementPlan(ServerProcess AProcess) : base(AProcess) {}
		
		public void Execute(DataParams AParams)
		{
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				FProcess.Execute(this, FCode, AParams);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
	}
	
	// ServerExpressionPlan
	public class ServerExpressionPlan : ServerPlan, IServerExpressionPlan
	{
		protected internal ServerExpressionPlan(ServerProcess AProcess) : base(AProcess) {}
		
		public DataValue Evaluate(DataParams AParams)
		{
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				return ServerProcess.Execute(this, Code, AParams).Value;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(LException);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		public IServerCursor Open(DataParams AParams)
		{
			IServerCursor LServerCursor;
			//ServerProcess.RaiseTraceEvent(TraceCodes.BeginOpenCursor, "Begin Open Cursor");
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();

				#if TIMING
				DateTime LStartTime = DateTime.Now;
				System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerExpressionPlan.Open", DateTime.Now.ToString("hh:mm:ss.ffff")));
				#endif
				ServerCursor LCursor = new ServerCursor(this, AParams);
				try
				{
					LCursor.Open();
					#if TIMING
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerExpressionPlan.Open -- Open Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
					#endif
					LServerCursor = (IServerCursor)LCursor;
				}
				catch
				{
					Close((IServerCursor)LCursor);
					throw;
				}
			}
			catch (Exception E)
			{
				if (Header != null)
					Header.IsInvalidPlan = true;

				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
			//ServerProcess.RaiseTraceEvent(TraceCodes.EndOpenCursor, "End Open Cursor");
			return LServerCursor;
		}
		
		public void Close(IServerCursor ACursor)
		{
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				((ServerCursor)ACursor).Dispose();
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		private Schema.TableVar FTableVar;
		Schema.TableVar IServerExpressionPlan.TableVar 
		{ 
			get 
			{ 
				CheckCompiled();
				if (FTableVar == null)
					FTableVar = (Schema.TableVar)Plan.PlanCatalog[Schema.Object.NameFromGuid(ID)];
				return FTableVar; 
			} 
		}
		
		Schema.Catalog IServerExpressionPlan.Catalog { get { return Plan.PlanCatalog; } }
		
		public Row RequestRow()
		{
			CheckCompiled();
			return new Row(FProcess, ((Schema.TableType)DataType).RowType);
		}
		
		public void ReleaseRow(Row ARow)
		{
			CheckCompiled();
			ARow.Dispose();
		}
		
		public override Statement EmitStatement()
		{
			CheckCompiled();
			return FCode.EmitStatement(EmitMode.ForRemote);
		}

		// SourceNode
		private TableNode SourceNode { get { return (TableNode)Code.Nodes[0]; } }
        
		// Isolation
		public CursorIsolation Isolation 
		{
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorIsolation; 
			}
		}
		
		// CursorType
		public CursorType CursorType 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorType; 
			} 
		}

		// Capabilities		
		public CursorCapability Capabilities 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorCapabilities; 
			} 
		}

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}
		
		// Order
		public Schema.Order Order
		{
			get
			{
				CheckCompiled();
				return SourceNode.Order;
			}
		}
	}

	// ServerCursorBase	
	public class ServerCursorBase : ServerChildObject, IActive
	{
		public ServerCursorBase(ServerPlanBase APlan, DataParams AParams) : base()
		{
			FPlan = APlan;
			FParams = AParams;

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FPlan.ServerProcess.ServerSession.Server.FCursorCounter != null)
				FPlan.ServerProcess.ServerSession.Server.FCursorCounter.Increment();
			#endif
		}
		
		private bool FDisposed;
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Close();
				
				if (!FDisposed)
				{
					#if !DISABLE_PERFORMANCE_COUNTERS
					if (FPlan.ServerProcess.ServerSession.Server.FCursorCounter != null)
						FPlan.ServerProcess.ServerSession.Server.FCursorCounter.Decrement();
					#endif
					FDisposed = true;
				}
			}
			finally
			{
				FParams = null;
				FPlan = null;
				
				base.Dispose(ADisposing);
			}
		}
		
		protected Exception WrapException(Exception AException)
		{
			return FPlan.ServerProcess.ServerSession.WrapException(AException);
		}
		
		protected ServerPlanBase FPlan;

		// IActive
		// Open        
		public void Open()
		{
			if (!FActive)
			{
				#if USESERVERCURSOREVENTS
				DoBeforeOpen();
				#endif
				InternalOpen();
				FActive = true;
				#if USESERVERCURSOREVENTS
				DoAfterOpen();
				#endif
			}
		}
        
		// Close
		public void Close()
		{
			if (FActive)
			{
				#if USESERVERCURSOREVENTS
				DoBeforeClose();
				#endif
				InternalClose();
				FActive = false;
				#if USESERVERCURSOREVENTS
				DoAfterClose();
				#endif
			}
		}
        
		// Active
		protected bool FActive;
		public bool Active
		{
			get { return FActive; }
			set
			{
				if (value)
					Open();
				else
					Close();
			}
		}
        
		protected void CheckActive()
		{
			if (!Active)
				throw new ServerException(ServerException.Codes.CursorInactive);
		}
        
		protected void CheckInactive()
		{
			if (Active)
				throw new ServerException(ServerException.Codes.CursorActive);
		}
        
		protected PlanNode SourceNode { get { return ((ServerPlanBase)FPlan).Code.Nodes[0]; } }
		
		protected DataVar FSourceObject;
		protected DataParams FParams;
		protected Table FSourceTable;
		protected Schema.IRowType FSourceRowType;
		
		protected IStreamManager StreamManager { get {return (FPlan is IServerPlan) ? (IStreamManager)((IServerPlan)FPlan).Process : (IStreamManager)((IRemoteServerPlan)FPlan).Process; } }
				
		protected virtual void InternalOpen()
		{
			// get a table object to supply the data
			FPlan.SetActiveCursor(this);
			FPlan.ServerProcess.Start(FPlan, FParams);
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;

				CursorNode LCursorNode = (CursorNode)((ServerPlanBase)FPlan).Code;
				//LCursorNode.EnsureApplicationTransactionJoined(FPlan.ServerProcess);
				FSourceObject = LCursorNode.SourceNode.Execute(FPlan.ServerProcess);
				FSourceTable = (Table)FSourceObject.Value;
				FSourceTable.Open();
				FSourceRowType = FSourceTable.DataType.RowType;
				
				FPlan.Statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch
			{
				InternalClose();
				throw;
			}
		}
		
		protected virtual void InternalClose()
		{
			try
			{
				try
				{
					try
					{
						if (FBookmarks != null)
						{
							InternalDisposeBookmarks();
							FBookmarks = null;
						}
					}
					finally
					{
						if (FSourceTable != null)
						{
							FSourceTable.Dispose();
							FSourceTable = null;
							FSourceObject = null;
							FSourceRowType = null;
						}
					}
				}
				finally
				{
					FPlan.ServerProcess.Stop(FPlan, FParams);
				}
			}
			finally
			{
				FPlan.ClearActiveCursor();
			}
		}
		
		// Isolation
		public CursorIsolation Isolation { get { return FSourceTable.Isolation; } }
		
		// CursorType
		public CursorType CursorType { get { return FSourceTable.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return FSourceTable.Capabilities; } }

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}

		protected void CheckCapability(CursorCapability ACapability)
		{
			if (!Supports(ACapability))
				throw new ServerException(ServerException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), ACapability));
		}

		#if USESERVERCURSOREVENTS
		// Events
		public event EventHandler BeforeOpen;
		protected virtual void DoBeforeOpen()
		{
			if (BeforeOpen != null)
				BeforeOpen(this, EventArgs.Empty);
		}
        
		public event EventHandler AfterOpen;
		protected virtual void DoAfterOpen()
		{
			if (AfterOpen != null)
				AfterOpen(this, EventArgs.Empty);
		}
        
		public event EventHandler BeforeClose;
		protected virtual void DoBeforeClose()
		{
			if (BeforeClose != null)
				BeforeClose(this, EventArgs.Empty);

		}
        
		public event EventHandler AfterClose;
		protected virtual void DoAfterClose()
		{
			if (AfterClose != null)
				AfterClose(this, EventArgs.Empty);
		}
		#endif

		private Bookmarks FBookmarks = new Bookmarks();

		protected Guid InternalGetBookmark()
		{
			Guid LResult = Guid.NewGuid();
			FBookmarks.Add(LResult, FSourceTable.GetBookmark());
			return LResult;
		}

		protected bool InternalGotoBookmark(Guid ABookmark, bool AForward)
		{
			Row LRow = FBookmarks[ABookmark];
			if (LRow == null)
				throw new ServerException(ServerException.Codes.InvalidBookmark, ABookmark);
			return FSourceTable.GotoBookmark(FBookmarks[ABookmark], AForward);
		}

		protected int InternalCompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			return FSourceTable.CompareBookmarks(FBookmarks[ABookmark1], FBookmarks[ABookmark2]);
		}

		protected void InternalDisposeBookmark(Guid ABookmark)
		{
			Row LInternalBookmark = FBookmarks[ABookmark];
			FBookmarks.Remove(ABookmark);
			if (LInternalBookmark != null)
				LInternalBookmark.Dispose();
		}

		protected void InternalDisposeBookmarks(Guid[] ABookmarks)
		{
			foreach (Guid LBookmark in ABookmarks)
				InternalDisposeBookmark(LBookmark);
		}
		
		protected void InternalDisposeBookmarks()
		{
			Guid[] LKeys = new Guid[FBookmarks.Keys.Count];
			FBookmarks.Keys.CopyTo(LKeys, 0);
			InternalDisposeBookmarks(LKeys);
		}
	}
	
	// ServerCursor    
	public class ServerCursor : ServerCursorBase, IServerCursor
	{
		public ServerCursor(ServerPlanBase APlan, DataParams AParams) : base(APlan, AParams) {}

		public IServerExpressionPlan Plan { get { return (IServerExpressionPlan)FPlan; } }

		// cursor support		
		public void Reset()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.Reset();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Next()
		{
			bool LResult;
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				LResult = FSourceTable.Next();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
			return LResult;
		}
		
		public void Last()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.Last();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool BOF()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{		
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.BOF();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool EOF()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.EOF();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool IsEmpty()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.BOF() && FSourceTable.EOF();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public Row Select()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.Select();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Select(Row ARow)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.Select(ARow);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void First()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.First();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Prior()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.Prior();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public Guid GetBookmark()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetBookmark();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		public bool GotoBookmark(Guid ABookmark, bool AForward)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGotoBookmark(ABookmark, AForward);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalCompareBookmarks(ABookmark1, ABookmark2);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Disposes a bookmark. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmark(Guid ABookmark)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				InternalDisposeBookmark(ABookmark);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] ABookmarks)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				InternalDisposeBookmarks(ABookmarks);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public Schema.Order Order { get { return FSourceTable.Node.Order; } }
		
		public Row GetKey()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.GetKey();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool FindKey(Row AKey)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.FindKey(AKey);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void FindNearest(Row AKey)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				FSourceTable.FindNearest(AKey);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Refresh(Row ARow)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.Refresh(ARow);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Insert(Row ARow)
		{
			Insert(ARow, null);
		}
		
		public void Insert(Row ARow, BitArray AValueFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				FSourceTable.Insert(null, ARow, AValueFlags, false);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Update(Row ARow)
		{
			Update(ARow, null);
		}
		
		public void Update(Row ARow, BitArray AValueFlags)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				FSourceTable.Update(ARow, AValueFlags, false);
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Delete()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Updateable);
				FSourceTable.Delete();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public void Truncate()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				CheckCapability(CursorCapability.Truncateable);
				FSourceTable.Truncate();
				FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public int RowCount()
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.RowCount();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		public bool Default(Row ARow, string AColumnName)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Default(FPlan.ServerProcess, null, ARow, null, AColumnName);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Change(Row AOldRow, Row ANewRow, string AColumnName)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Change(FPlan.ServerProcess, AOldRow, ANewRow, null, AColumnName);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public bool Validate(Row AOldRow, Row ANewRow, string AColumnName)
		{
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return ((TableNode)SourceNode).Validate(FPlan.ServerProcess, AOldRow, ANewRow, null, AColumnName);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
	}

	// RemoteServerPlan	
	public abstract class RemoteServerPlan : ServerPlanBase, IRemoteServerPlan
	{
		protected internal RemoteServerPlan(ServerProcess AProcess) : base(AProcess) {}

		public IRemoteServerProcess Process { get { return (IRemoteServerProcess)FProcess; } }
		
		public Exception[] Messages
		{
			get
			{
				Exception[] LMessages = new Exception[Plan.Messages.Count];
				for (int LIndex = 0; LIndex < Plan.Messages.Count; LIndex++)
					LMessages[LIndex] = Plan.Messages[LIndex];
				return LMessages;
			}
		}
	}

	// RemoteServerStatementPlan	
	public class RemoteServerStatementPlan : RemoteServerPlan, IRemoteServerStatementPlan
	{
		protected internal RemoteServerStatementPlan(ServerProcess AProcess) : base(AProcess) {}
		
		public void Execute(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				FProcess.Execute(this, FCode, LParams);
				FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
				AExecuteTime = Statistics.ExecuteTime;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
	}
	
	// RemoteServerExpressionPlan
	public class RemoteServerExpressionPlan : RemoteServerPlan, IRemoteServerExpressionPlan
	{
		protected internal RemoteServerExpressionPlan(ServerProcess AProcess) : base(AProcess) {}
		
		protected override void Dispose(bool ADisposing)
		{
			RemoveCacheReference();
			base.Dispose(ADisposing);
		}
		
		public byte[] Evaluate(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				DataVar LVar = ServerProcess.Execute(this, Code, LParams);
				FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
				AExecuteTime = Statistics.ExecuteTime;
				if (LVar.Value == null)
					return null;
				if (LVar.DataType.Equivalent(LVar.Value.DataType))
					return LVar.Value.AsPhysical;
				return LVar.Value.CopyAs(LVar.DataType).AsPhysical;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(LException);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		/// <summary> Opens a remote, server-side cursor based on the prepared statement this plan represents. </summary>        
		/// <returns> An <see cref="IRemoteServerCursor"/> instance for the prepared statement. </returns>
		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			IRemoteServerCursor LServerCursor;
			//ServerProcess.RaiseTraceEvent(TraceCodes.BeginOpenCursor, "Begin Open Cursor");
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				RemoteServerCursor LCursor = new RemoteServerCursor(this, LParams);
				try
				{
					LCursor.Open();
					FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
					AExecuteTime = Statistics.ExecuteTime;
					LServerCursor = (IRemoteServerCursor)LCursor;
				}
				catch
				{
					Close((IRemoteServerCursor)LCursor, FProcess.EmptyCallInfo());
					throw;
				}
			}
			catch (Exception E)
			{
				if (Header != null)
					Header.IsInvalidPlan = true;
					
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
			//ServerProcess.RaiseTraceEvent(TraceCodes.EndOpenCursor, "End Open Cursor");
			return LServerCursor;
		}
		
		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData, ProcessCallInfo ACallInfo)
		{
			IRemoteServerCursor LServerCursor = Open(ref AParams, out AExecuteTime, ACallInfo);
			AFetchData = LServerCursor.Fetch(out ABookmarks, ACount, FProcess.EmptyCallInfo());
			return LServerCursor;
		}
		
		/// <summary> Closes a remote, server-side cursor previously created using Open. </summary>
		/// <param name="ACursor"> The cursor to close. </param>
		public void Close(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				((RemoteServerCursor)ACursor).Dispose();
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		private bool ContainsParam(RemoteParam[] AParams, string AParamName)
		{
			if (AParams == null)
				return false;
				
			for (int LIndex = 0; LIndex < AParams.Length; LIndex++)
				if (AParams[LIndex].Name == AParamName)
					return true;
			return false;
		}

		public string GetCatalog(RemoteParam[] AParams, out string ACatalogObjectName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			CheckCompiled();

			if (Code.DataType is Schema.ICursorType)
				ACatalogObjectName = Schema.Object.NameFromGuid(ID);
			else
				ACatalogObjectName = Code.DataType.Name;
				
			#if LOGCACHEEVENTS
			ServerProcess.ServerSession.Server.LogMessage(String.Format("Getting catalog for expression '{0}'.", Header.Statement));
			#endif

			ACacheChanged = true;
			ACacheTimeStamp = ServerProcess.ServerSession.Server.CacheTimeStamp;
			string[] LRequiredObjects = ServerProcess.ServerSession.Server.CatalogCaches.GetRequiredObjects(ServerProcess.ServerSession, Plan.PlanCatalog, ACacheTimeStamp, out AClientCacheTimeStamp);
			if (LRequiredObjects.Length > 0)
			{
				if (Code.DataType is Schema.ICursorType)
				{
					string[] LAllButCatalogObject = new string[LRequiredObjects.Length - 1];
					int LTargetIndex = 0;
					for (int LIndex = 0; LIndex < LRequiredObjects.Length; LIndex++)
						if (LRequiredObjects[LIndex] != ACatalogObjectName)
						{
							LAllButCatalogObject[LTargetIndex] = LRequiredObjects[LIndex];
							LTargetIndex++;
						}
						
					Block LStatement = LAllButCatalogObject.Length > 0 ? (Block)Plan.PlanCatalog.EmitStatement(ServerProcess, EmitMode.ForRemote, LAllButCatalogObject) : new Block();
					
					// Add variable declaration statements for any process context that may be being referenced by the plan
					for (int LIndex = ServerProcess.Context.Count - 1; LIndex >= 0; LIndex--)
						if (!ContainsParam(AParams, ServerProcess.Context[LIndex].Name))
							LStatement.Statements.Add(new VariableStatement(ServerProcess.Context[LIndex].Name, ServerProcess.Context[LIndex].DataType.EmitSpecifier(EmitMode.ForRemote)));
					
					Block LCatalogObjectStatement = (Block)Plan.PlanCatalog.EmitStatement(ServerProcess, EmitMode.ForRemote, new string[]{ ACatalogObjectName });
					LStatement.Statements.AddRange(LCatalogObjectStatement.Statements);
					string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(LStatement);
					return LCatalogString;
				}
				else
				{
					string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(Plan.PlanCatalog.EmitStatement(ServerProcess, EmitMode.ForRemote, LRequiredObjects));
					return LCatalogString;
				}
			}
			return String.Empty;
		}

		public void RemoveCacheReference()
		{
			// Remove the cache object describing the result set for the plan
			if ((Code != null) && (Code.DataType is Schema.ICursorType))
				ServerProcess.ServerSession.Server.CatalogCaches.RemovePlanDescriptor(ServerProcess.ServerSession, Schema.Object.NameFromGuid(ID));
		}
		
		// SourceNode
		public TableNode SourceNode 
		{ 
			get 
			{ 
				CheckCompiled();
				return (TableNode)Code.Nodes[0]; 
			} 
		}

		// Isolation
		public CursorIsolation Isolation 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorIsolation; 
			} 
		}
		
		// CursorType
		public CursorType CursorType 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorType; 
			} 
		}

		// Capabilities		
		public CursorCapability Capabilities 
		{
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorCapabilities; 
			} 
		}

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}
	}
	
	// RemoteServerCursor
	public class RemoteServerCursor : ServerCursorBase, IRemoteServerCursor
	{
		public RemoteServerCursor(ServerPlanBase APlan, DataParams AParams) : base(APlan, AParams){}
		
		public IRemoteServerExpressionPlan Plan { get { return (IRemoteServerExpressionPlan)FPlan; } } 
		
		private Schema.IRowType GetRowType(RemoteRowHeader AHeader)
		{
			Schema.IRowType LRowType = FSourceTable.DataType.CreateRowType(false);
			for (int LIndex = 0; LIndex < AHeader.Columns.Length; LIndex++)
				LRowType.Columns.Add(FSourceTable.DataType.Columns[AHeader.Columns[LIndex]].Copy());
			return LRowType;
		}
		
		// IRemoteServerCursor
		/// <summary> Returns the current row of the cursor. </summary>
		/// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the row information. </returns>
		public RemoteRowBody Select(RemoteRowHeader AHeader, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Row LRow = new Row(FPlan.ServerProcess, GetRowType(AHeader));
					try
					{
						LRow.ValuesOwned = false;
						FSourceTable.Select(LRow);
						RemoteRowBody LBody = new RemoteRowBody();
						LBody.Data = LRow.AsPhysical;
						return LBody;
					}
					finally
					{
						LRow.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		public RemoteRowBody Select(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Row LRow = new Row(FPlan.ServerProcess, FSourceRowType);
					try
					{
						LRow.ValuesOwned = false;
						FSourceTable.Select(LRow);
						RemoteRowBody LBody = new RemoteRowBody();
						LBody.Data = LRow.AsPhysical;
						return LBody;
					}
					finally
					{
						LRow.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		// SourceFetch
		protected int SourceFetch(Row[] ARows, Guid[] ABookmarks, int ACount)
		{
			int LCount = 0;
			while (ACount != 0)
			{
				if (ACount > 0)
				{
					if ((LCount == 0) && FSourceTable.BOF() && !FSourceTable.Next())
						break;

					if ((LCount > 0) && (!FSourceTable.Next()))
						break;
					FSourceTable.Select(ARows[LCount]);
					ABookmarks[LCount] = InternalGetBookmark();
					ACount--;
				}
				else
				{
					if ((LCount == 0) && FSourceTable.EOF() && !FSourceTable.Prior())
						break;
						
					if ((LCount > 0) && (!FSourceTable.Prior()))
						break;
					FSourceTable.Select(ARows[LCount]);
					ABookmarks[LCount] = InternalGetBookmark();
					ACount++;
				}
				LCount++;
			}
			return LCount;
		}
		
		private RemoteFetchData InternalFetch(Schema.IRowType ARowType, Guid[] ABookmarks, int ACount)
		{
			Row[] LRows = new Row[Math.Abs(ACount)];
			try
			{
				for (int LIndex = 0; LIndex < LRows.Length; LIndex++)
				{
					LRows[LIndex] = new Row(FPlan.ServerProcess, ARowType);
					LRows[LIndex].ValuesOwned = false;
				}
				
				int LCount = SourceFetch(LRows, ABookmarks, ACount);
				
				RemoteFetchData LFetchData = new RemoteFetchData();
				LFetchData.Body = new RemoteRowBody[LCount];
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					LFetchData.Body[LIndex].Data = LRows[LIndex].AsPhysical;
				
				LFetchData.Flags = (byte)InternalGetFlags();//hack:cast to fix MSs error
				return LFetchData;
			}
			finally
			{
				for (int LIndex = 0; LIndex < LRows.Length; LIndex++)
					if (LRows[LIndex] != null)
						LRows[LIndex].Dispose();
			}
		}
        
		/// <summary> Returns the requested number of rows from the cursor. </summary>
		/// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
		/// <param name='ACount'> The number of rows to fetch, with a negative number indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </returns>
		public RemoteFetchData Fetch(RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					ABookmarks = new Guid[Math.Abs(ACount)];
					return InternalFetch(GetRowType(AHeader), ABookmarks, ACount);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		public RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					ABookmarks = new Guid[Math.Abs(ACount)];
					return InternalFetch(FSourceRowType, ABookmarks, ACount);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		private RemoteCursorGetFlags InternalGetFlags()
		{
			RemoteCursorGetFlags LGetFlags = RemoteCursorGetFlags.None;
			if (FSourceTable.BOF())
				LGetFlags = LGetFlags | RemoteCursorGetFlags.BOF;
			if (FSourceTable.EOF())
				LGetFlags = LGetFlags | RemoteCursorGetFlags.EOF;
			return LGetFlags;
		}

		/// <summary> Indicates whether the cursor is on the BOF crack, the EOF crack, or both, which indicates an empty cursor. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the current state of the cursor. </returns>
		public RemoteCursorGetFlags GetFlags(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Provides a mechanism for navigating the cursor by a specified number of rows. </summary>        
		/// <param name='ADelta'> The number of rows to move by, with a negative value indicating backwards movement. </param>
		/// <returns> A <see cref="RemoteMoveData"/> structure containing the result of the move. </returns>
		public RemoteMoveData MoveBy(int ADelta, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					int LDelta = 0;
					while (ADelta != 0)
					{
						if (ADelta > 0)
						{
							if (!FSourceTable.Next())
								break;
							ADelta--;
						}
						else
						{
							if (!FSourceTable.Prior())
								break;
							ADelta++;
						}
						LDelta++;
					}
					RemoteMoveData LMoveData = new RemoteMoveData();
					LMoveData.Count = LDelta;
					LMoveData.Flags = InternalGetFlags();
					return LMoveData;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Positions the cursor on the BOF crack. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public RemoteCursorGetFlags First(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.First();
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Positions the cursor on the EOF crack. </summary>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
		public RemoteCursorGetFlags Last(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.Last();
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Resets the server-side cursor, causing any data to be re-read and leaving the cursor on the BOF crack. </summary>        
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the reset. </returns>
		public RemoteCursorGetFlags Reset(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.Reset();
					return InternalGetFlags();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Inserts the given <see cref="RemoteRow"/> into the cursor. </summary>        
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be inserted. </param>
		public void Insert(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					foreach (string LString in ARow.Header.Columns)
						LType.Columns.Add(FSourceTable.DataType.Columns[LString].Copy());
					Row LRow = new Row(FPlan.ServerProcess, LType);
					try
					{
						LRow.ValuesOwned = false;
						LRow.AsPhysical = ARow.Body.Data;
						FSourceTable.Insert(null, LRow, AValueFlags, false);
					}
					finally
					{
						LRow.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Updates the current row of the cursor using the given <see cref="RemoteRow"/>. </summary>        
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be updated. </param>
		public void Update(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					foreach (string LString in ARow.Header.Columns)
						LType.Columns.Add(FSourceTable.DataType.Columns[LString].Copy());
					Row LRow = new Row(FPlan.ServerProcess, LType);
					try
					{
						LRow.ValuesOwned = false;
						LRow.AsPhysical = ARow.Body.Data;
						FSourceTable.Update(LRow, AValueFlags, false);
					}
					finally
					{
						LRow.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		/// <summary> Deletes the current DataBuffer from the cursor. </summary>
		public void Delete(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					FSourceTable.Delete();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		public const int CBookmarkTypeInt = 0;
		public const int CBookmarkTypeRow = 1;

		/// <summary> Gets a bookmark for the current DataBuffer suitable for use in the <c>GotoBookmark</c> and <c>CompareBookmark</c> methods. </summary>
		/// <returns> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </returns>
		public Guid GetBookmark(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalGetBookmark();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}	
		}

		/// <summary> Positions the cursor on the DataBuffer denoted by the given bookmark obtained from a previous call to <c> GetBookmark </c> . </summary>
		/// <param name="ABookmark"> A <see cref="RemoteRowBody"/> structure containing the data for the bookmark. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the goto call. </returns>
		public unsafe RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					RemoteGotoData LGotoData = new RemoteGotoData();
					LGotoData.Success = InternalGotoBookmark(ABookmark, AForward);
					LGotoData.Flags = InternalGetFlags();
					return LGotoData;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Compares the value of two bookmarks obtained from previous calls to <c>GetBookmark</c> . </summary>        
		/// <param name="ABookmark1"> A <see cref="RemoteRowBody"/> structure containing the data for the first bookmark to compare. </param>
		/// <param name="ABookmark2"> A <see cref="RemoteRowBody"/> structure containing the data for the second bookmark to compare. </param>
		/// <returns> An integer value indicating whether the first bookmark was less than (negative), equal to (0) or greater than (positive) the second bookmark. </returns>
		public unsafe int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalCompareBookmarks(ABookmark1, ABookmark2);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Disposes a bookmark. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmark(Guid ABookmark, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					InternalDisposeBookmark(ABookmark);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed. </remarks>
		public void DisposeBookmarks(Guid[] ABookmarks, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					InternalDisposeBookmarks(ABookmarks);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		/// <value> Accesses the <see cref="Order"/> of the cursor. </value>
		public string Order { get { return FSourceTable.Node.Order.Name; } }
        
		/// <returns> A <see cref="RemoteRow"/> structure containing the key for current row. </returns>
		public RemoteRow GetKey(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Row LKey = FSourceTable.GetKey();
					RemoteRow LRow = new RemoteRow();
					LRow.Header = new RemoteRowHeader();
					LRow.Header.Columns = new string[LKey.DataType.Columns.Count];
					for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
						LRow.Header.Columns[LIndex] = LKey.DataType.Columns[LIndex].Name;
					LRow.Body = new RemoteRowBody();
					LRow.Body.Data = LKey.AsPhysical;
					return LRow;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		/// <summary> Attempts to position the cursor on the row matching the given key.  If the key is not found, the cursor position remains unchanged. </summary>
		/// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the find. </returns>
		public RemoteGotoData FindKey(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					for (int LIndex = 0; LIndex < AKey.Header.Columns.Length; LIndex++)
						LType.Columns.Add(FSourceTable.DataType.Columns[AKey.Header.Columns[LIndex]].Copy());

					Row LKey = new Row(FPlan.ServerProcess, LType);
					try
					{
						LKey.ValuesOwned = false;
						LKey.AsPhysical = AKey.Body.Data;
						RemoteGotoData LGotoData = new RemoteGotoData();
						LGotoData.Success = FSourceTable.FindKey(LKey);
						LGotoData.Flags = InternalGetFlags();
						return LGotoData;
					}
					finally
					{
						LKey.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		/// <summary> Positions the cursor on the record most closely matching the given key. </summary>
		/// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
		/// <returns> A <see cref="RemoteCursorGetFlags"/> value indicating the state of the cursor after the search. </returns>
		public RemoteCursorGetFlags FindNearest(RemoteRow AKey, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					for (int LIndex = 0; LIndex < AKey.Header.Columns.Length; LIndex++)
						LType.Columns.Add(FSourceTable.DataType.Columns[AKey.Header.Columns[LIndex]].Copy());

					Row LKey = new Row(FPlan.ServerProcess, LType);
					try
					{
						LKey.ValuesOwned = false;
						LKey.AsPhysical = AKey.Body.Data;
						FSourceTable.FindNearest(LKey);
						return InternalGetFlags();
					}
					finally
					{
						LKey.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		/// <summary> Refreshes the cursor and attempts to reposition it on the given row. </summary>
		/// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the row to be positioned on after the refresh. </param>
		/// <returns> A <see cref="RemoteGotoData"/> structure containing the result of the refresh. </returns>
		public RemoteGotoData Refresh(RemoteRow ARow, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					Schema.RowType LType = new Schema.RowType();
					for (int LIndex = 0; LIndex < ARow.Header.Columns.Length; LIndex++)
						LType.Columns.Add(FSourceTable.DataType.Columns[ARow.Header.Columns[LIndex]].Copy());

					Row LRow = new Row(FPlan.ServerProcess, LType);
					try
					{
						LRow.ValuesOwned = false;
						LRow.AsPhysical = ARow.Body.Data;
						RemoteGotoData LGotoData = new RemoteGotoData();
						LGotoData.Success = FSourceTable.Refresh(LRow);
						LGotoData.Flags = InternalGetFlags();
						return LGotoData;
					}
					finally
					{
						LRow.Dispose();
					}
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		// Countable
		/// <returns>An integer value indicating the number of rows in the cursor.</returns>
		public int RowCount(ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return FSourceTable.RowCount();
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		private RemoteProposeData InternalDefault(RemoteRowBody ARow, string AColumn)
		{
			Row LRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = ARow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = ((TableNode)SourceNode).Default(FPlan.ServerProcess, null, LRow, null, AColumn);
					LProposeData.Body = new RemoteRowBody();
					LProposeData.Body.Data = LRow.AsPhysical;
					return LProposeData;
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			finally
			{
				LRow.Dispose();
			}
		}
		
		// IRemoteProposable
		/// <summary>
		///	Requests the default values for a new row in the cursor.  
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="AColumn"></param>
		/// <returns></returns>
		public RemoteProposeData Default(RemoteRowBody ARow, string AColumn, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalDefault(ARow, AColumn);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
        
		private RemoteProposeData InternalChange(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			Row LOldRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
			try
			{
				LOldRow.ValuesOwned = false;
				LOldRow.AsPhysical = AOldRow.Data;
				
				Row LNewRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
				try
				{
					LNewRow.ValuesOwned = false;
					LNewRow.AsPhysical = ANewRow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = ((TableNode)SourceNode).Change(FPlan.ServerProcess, LOldRow, LNewRow, null, AColumn);
					LProposeData.Body = new RemoteRowBody();
					LProposeData.Body.Data = LNewRow.AsPhysical;
					return LProposeData;
				}
				finally
				{
					LNewRow.Dispose();
				}
			}
			finally
			{
				LOldRow.Dispose();
			}
		}
        
		/// <summary>
		/// Requests the affect of a change to the given row. 
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="AColumn"></param>
		/// <returns></returns>
		public RemoteProposeData Change(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalChange(AOldRow, ANewRow, AColumn);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}

		private RemoteProposeData InternalValidate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn)
		{
			Row LOldRow = null;
			if (AOldRow.Data != null)
				LOldRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
			try
			{
				if (LOldRow != null)
				{
					LOldRow.ValuesOwned = false;
					LOldRow.AsPhysical = AOldRow.Data;
				}
				
				Row LNewRow = new Row(FPlan.ServerProcess, FSourceTable.DataType.RowType);
				try
				{
					LNewRow.ValuesOwned = false;
					LNewRow.AsPhysical = ANewRow.Data;
					RemoteProposeData LProposeData = new RemoteProposeData();
					LProposeData.Success = ((TableNode)SourceNode).Validate(FPlan.ServerProcess, LOldRow, LNewRow, null, AColumn);
					LProposeData.Body = new RemoteRowBody();
					LProposeData.Body.Data = LNewRow.AsPhysical;
					return LProposeData;
				}
				finally
				{
					LNewRow.Dispose();
				}
			}
			finally
			{
				if (LOldRow != null)
					LOldRow.Dispose();
			}
		}

		/// <summary>
		/// Ensures that the given row is valid.
		/// </summary>
		/// <param name="ARow"></param>
		/// <param name="AColumn"></param>
		public RemoteProposeData Validate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo)
		{
			FPlan.ServerProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FPlan.ServerProcess.BeginTransactionalCall();
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return InternalValidate(ANewRow, AOldRow, AColumn);
				}
				finally
				{
					FPlan.Statistics.ExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FPlan.ServerProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
	}

	internal class Bookmarks : System.Collections.Hashtable
	{
		public Row this[Guid AKey] { get { return (Row)base[AKey]; } }
	}
}
