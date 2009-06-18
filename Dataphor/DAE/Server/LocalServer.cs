/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define USESPINLOCK
#define LOGFILECACHEEVENTS

namespace Alphora.Dataphor.DAE.Server
{
	using System;
	using System.IO;
	using System.Text;
	using System.Threading;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Reflection;
	using System.Runtime.Remoting;
	using System.Runtime.Remoting.Lifetime;
	using System.Windows.Forms;

	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Server;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	/*
		LocalServerObject
			|- LocalServer
		LocalServerChildObject
			|- LocalSession
			|- LocalProcess
			|- LocalScript
			|- LocalBatch
			|- LocalPlan
			|	|- LocalExpressionPlan
			|	|- LocalStatementPlan
			|- LocalCursor
	*/
	
	public class LocalServerObject : MarshalByRefObject, IDisposableNotify
	{
		#if USEFINALIZER
		~LocalServerObject()
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
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			DoDispose();
		}
		
		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

		public override object InitializeLifetimeService()
		{
			return null;	// Should never get a lease as a service and client objects are always held
		}
	}
	
	public class LocalServerChildObject : MarshalByRefObject, IDisposableNotify
	{
		#if USEFINALIZER
		~LocalServerChildObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif

		protected internal void Dispose()
		{
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		protected virtual void Dispose(bool ADisposing)
		{
			DoDispose();
		}
		
		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

		public override object InitializeLifetimeService()
		{
			return null;	// Should never get a lease as a service and client objects are always held
		}
	}
	
	public delegate void CacheClearedEvent(LocalServer AServer);

    public class LocalServer : LocalServerObject, IServer, IDisposable
    {
		/// <remarks>The buffer size to use when copying library files over the CLI.</remarks>
		public const int CFileCopyBufferSize = 32768;
		
		public LocalServer(IRemoteServer AServer, bool AClientSideLoggingEnabled, string AHostName) : base()
		{
			FServer = AServer;
			FHostName = AHostName;
			FReferenceCount = 1;
			FInternalServer = new Server();
			FInternalServer.Name = Schema.Object.NameFromGuid(Guid.NewGuid());
			FInternalServer.TracingEnabled = false;
			FInternalServer.IsRepository = true;
			FInternalServer.LoggingEnabled = AClientSideLoggingEnabled;
			FInternalServer.Start();
			FServerInstanceID = AServer.InstanceID;
			FServerConnection = FServer.Establish(FInternalServer.Name, FHostName);
			FServerCacheTimeStamp = AServer.CacheTimeStamp;
			FClientCacheTimeStamp = 1;
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
					if (FInternalServer != null)
					{
						FInternalServer.Dispose();
						FInternalServer = null;
					}
				}
				finally
				{
					if (FServer != null)
					{
						try
						{
							if (FServerConnection != null)
								FServer.Relinquish(FServerConnection);
						}
						finally
						{
							FServerConnection = null;
							FServer = null;
						}
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		// Reference Counting
		protected int FReferenceCount;
		/// <summary> Increments the reference counter. </summary>
		internal protected void AddReference()
		{
			FReferenceCount++;
		}
		/// <summary> Decrements the reference counter, disposes when no references. </summary>
		internal protected void RemoveReference()
		{
			FReferenceCount--;
			if (FReferenceCount == 0)
				Dispose();
		}

		public string Name 
		{ 
			get 
			{ 
				CheckServerConnection();
				return FServer.Name; 
			} 
		}
		
		
		protected IRemoteServer FServer;
		public IRemoteServer RemoteServer 
		{ 
			get	
			{ 
				CheckServerConnection();
				return FServer; 
			} 
		}
		
		protected IRemoteServerConnection FServerConnection;
		public IRemoteServerConnection ServerConnection
		{
			get { return FServerConnection; }
		}

		protected internal Guid FServerInstanceID;
		public Guid InstanceID { get { return FServerInstanceID; } }
		
		protected internal void CheckServerInstanceID()
		{
			if (FServerInstanceID != FServer.InstanceID)
				throw new ServerException(ServerException.Codes.InvalidServerInstanceID);
		}
		
		protected internal void CheckServerConnection()
		{
			CheckServerInstanceID();
			string LConnectionName = FServerConnection.ConnectionName; // read connection name to validate the connection
		}
		
		// An internal server used to evaluate remote proposable calls
		protected internal Server FInternalServer;
		
		private string FHostName;
		
		// Client-side catalog cache
		
		// The catalog in FInternalServer is used as a client-side catalog cache both to retrieve the result set descriptions
		// of expressions opened remotely, and to allow for the remote evaluation of proposable calls. The cache is transparently
		// maintained for each connection to the remote server using a set of timestamps. When the connection is established, the
		// client-side cache timestamp is initialized to 1, and the cachetimestamp of the catalog is recorded. As expressions are
		// evaluated, the catalog necessary to support the expressions is downloaded to the client-side cache. On the server side,
		// any time the catalog cachetimestamp is incremented (as a result of a catalog change such as an alter table statement),
		// the new catalog cachetimestamp is recorded. Client-side, the following logic is used to ensure that the catalog cache
		// consistent and contains the necessary objects:
		
		// given that:
		//	FServerCacheTimeStamp: The current catalog cachetimestamp of the client-side catalog cache.
		//	AServerCacheTimeStamp: The catalog cachetimestamp of the client-side catalog cache required to support a given expression.
		//  FClientCacheTimeStamp: The current timestamp of the client-side catalog cache.
		//  AClientCacheTimeStamp: The timestamp of the client-side catalog cache required to support a given expression.
		
		// When an expression is evaluated, the resulting plan descriptor will include the catalog cachetimestamp of the catalog
		// at the moment that this expression was prepared (AServerCacheTimeStamp), and the timestamp of the client-side catalog
		// cache required to support compilation of the expression locally and any associated remote proposable calls.
		// if the expression requires new objects to be added to the client-side catalog cache
			// wait for AClientCacheTimeStamp - 1
				// while FClientCacheTimeStamp < AClientCacheTimeStamp and we have waited less than CCachLockTimeout milliseconds
					// Reset the cache sync event and then wait CCacheLockTimeout milliseconds for it be signaled
			// Acquire the catalog cache lock in exclusive mode
			// if FServerCacheTimeStamp < AServerCacheTimeStamp
				// clear the client-side catalog cache
				// set FServerCacheTimeStamp = AServerCacheTimeStamp
			// Execute the DDL statements returned by the server to modify the client-side catalog cache.
			// set the client cache timestamp
			// set the CacheSyncEvent to signaled, waking up any threads that may be waiting for this client cache timestamp
			// Release the catalog cache lock
			
		// All operations that require read access to the client-side catalog cache must take the catalog cache lock in shared mode.
				
		protected internal long FServerCacheTimeStamp;
		protected internal long FClientCacheTimeStamp;

		#if USESPINLOCK		
		private int FCacheSyncRoot = 0;
		#else
		private object FCacheSyncRoot = new object();
		#endif
		
		private SignalPool FCacheSignalPool = new SignalPool();
		private Hashtable FCacheSignals = new Hashtable();

		protected internal void WaitForCacheTimeStamp(IServerProcess AProcess, long AClientCacheTimeStamp)
		{
			#if LOGCACHEEVENTS
			FInternalServer.LogMessage(LogEntryType.Information, String.Format("Thread {0} checking for cache time stamp {1}", Thread.CurrentThread.GetHashCode(), AClientCacheTimeStamp.ToString()));
			#endif
			try
			{
				ManualResetEvent LSignal = null;
				bool LSignalAdded = false;
				
				#if USESPINLOCK
				while (Interlocked.CompareExchange(ref FCacheSyncRoot, 1, 0) == 1);
				try
				#else
				lock (FCacheSyncRoot)
				#endif
				{
					if (FClientCacheTimeStamp == AClientCacheTimeStamp)
						return;
						
					if (FClientCacheTimeStamp > AClientCacheTimeStamp)
					{
						AProcess.Execute(".System.UpdateTimeStamps();", null);
						throw new ServerException(ServerException.Codes.CacheSerializationError, AClientCacheTimeStamp, FClientCacheTimeStamp);
					}
					
					LSignal = (ManualResetEvent)FCacheSignals[AClientCacheTimeStamp];
					LSignalAdded = (LSignal == null);
					if (LSignalAdded)
					{
						LSignal = FCacheSignalPool.Acquire();
						FCacheSignals.Add(AClientCacheTimeStamp, LSignal);
					}

					//Error.AssertFail(FCacheSyncEvent.Reset(), "Internal error: CacheSyncEvent reset failed");
				}
				#if USESPINLOCK
				finally
				{
					Interlocked.Decrement(ref FCacheSyncRoot);
				}
				#endif

				#if LOGCACHEEVENTS
				FInternalServer.LogMessage(LogEntryType.Information, String.Format("Thread {0} waiting for cache time stamp {1}", Thread.CurrentThread.GetHashCode(), AClientCacheTimeStamp.ToString()));
				#endif
				
				try
				{
					if (!(LSignal.WaitOne(CCacheSerializationTimeout, false)))
						throw new ServerException(ServerException.Codes.CacheSerializationTimeout);
				}
				finally
				{
					if (LSignalAdded)
					{
						#if USESPINLOCK
						while (Interlocked.CompareExchange(ref FCacheSyncRoot, 1, 0) == 1);
						try
						#else
						lock (FCacheSyncRoot)
						#endif
						{
							FCacheSignals.Remove(AClientCacheTimeStamp);
						}
						#if USESPINLOCK
						finally
						{
							Interlocked.Decrement(ref FCacheSyncRoot);
						}
						#endif
						
						FCacheSignalPool.Relinquish(LSignal);
					}
				}
			}
			catch (Exception E)
			{
				FInternalServer.LogError(E);
				throw E;
			}
		}
		
		protected internal void SetCacheTimeStamp(IServerProcess AProcess, long AClientCacheTimeStamp)
		{
			#if LOGCACHEEVENTS
			FInternalServer.LogMessage(LogEntryType.Information, String.Format("Thread {0} updating cache time stamp to {1}", Thread.CurrentThread.GetHashCode(), AClientCacheTimeStamp.ToString()));
			#endif
			
			#if USESPINLOCK
			while (Interlocked.CompareExchange(ref FCacheSyncRoot, 1, 0) == 1);
			try
			#else
			lock (FCacheSyncRoot)
			#endif
			{
				FClientCacheTimeStamp = AClientCacheTimeStamp;
				
				ManualResetEvent LSignal = (ManualResetEvent)FCacheSignals[AClientCacheTimeStamp];
				if (LSignal != null)
					LSignal.Set();
			}
			#if USESPINLOCK
			finally
			{
				Interlocked.Decrement(ref FCacheSyncRoot);
			}
			#endif
		}
		
		protected ReaderWriterLock FCacheLock = new ReaderWriterLock();
		
		public const int CCacheLockTimeout = 10000;
		public const int CCacheSerializationTimeout = 30000; // Wait at most 30 seconds for a timestamp to be deserialized
		
		protected internal void AcquireCacheLock(IServerProcess AProcess, LockMode AMode)
		{
			try
			{
				if (AMode == LockMode.Exclusive)
					FCacheLock.AcquireWriterLock(CCacheLockTimeout);
				else
					FCacheLock.AcquireReaderLock(CCacheLockTimeout);
			}
			catch
			{
				throw new ServerException(ServerException.Codes.CacheLockTimeout);
			}
		}
		
		protected internal void ReleaseCacheLock(IServerProcess AProcess, LockMode AMode)
		{
			if (AMode == LockMode.Exclusive)
				FCacheLock.ReleaseWriterLock();
			else
				FCacheLock.ReleaseReaderLock();
		}

		public long CacheTimeStamp 
		{ 
			get 
			{ 
				CheckServerConnection();
				return FServer.CacheTimeStamp;
			} 
		}
		
		public long DerivationTimeStamp 
		{ 
			get 
			{ 
				CheckServerConnection();
				return FServer.DerivationTimeStamp; 
			} 
		}
		
		public event CacheClearedEvent OnCacheClearing;
		private void DoCacheClearing()
		{
			if (OnCacheClearing != null)
				OnCacheClearing(this);
		}
		
		public event CacheClearedEvent OnCacheCleared;
		private void DoCacheCleared()
		{
			if (OnCacheCleared != null)
				OnCacheCleared(this);
		}
		
		public void EnsureCacheConsistent(long AServerCacheTimeStamp)
		{
			if (FServerCacheTimeStamp < AServerCacheTimeStamp)
			{
				DoCacheClearing();
				Schema.Catalog LCatalog = FInternalServer.Catalog;
				FInternalServer.ClearCatalog();
				foreach (Schema.RegisteredAssembly LAssembly in LCatalog.ClassLoader.Assemblies)
					if (LAssembly.Library.Name != Server.CSystemLibraryName)
						FInternalServer.Catalog.ClassLoader.Assemblies.Add(LAssembly);
				foreach (Schema.RegisteredClass LClass in LCatalog.ClassLoader.Classes)
					if (!FInternalServer.Catalog.ClassLoader.Classes.Contains(LClass))
						FInternalServer.Catalog.ClassLoader.Classes.Add(LClass);
				foreach (ServerSession LSession in FInternalServer.Sessions)
				{
					if (LSession.User.ID == FInternalServer.SystemUser.ID)
						LSession.SetUser(FInternalServer.SystemUser);
					if (LSession.User.ID == FInternalServer.AdminUser.ID)
						LSession.SetUser(FInternalServer.AdminUser);
					LSession.SessionObjects.Clear();
				}
				FServerCacheTimeStamp = AServerCacheTimeStamp;
				DoCacheCleared();
			}
		}
		
		private StringCollection FFilesCached = new StringCollection();
		private StringCollection FAssembliesCached = new StringCollection();
		
		private string FLocalBinDirectory;
		private string LocalBinDirectory
		{
			get
			{
				if (FLocalBinDirectory == null)
				{
					FLocalBinDirectory = Path.Combine(Path.Combine(PathUtility.GetBinDirectory(), "LocalBin"), FServer.Name);
					Directory.CreateDirectory(FLocalBinDirectory);
				}
				return FLocalBinDirectory;
			}
		}
		
		private string GetLocalFileName(string ALibraryName, string AFileName)
		{
			return Path.Combine(ALibraryName == Server.CSystemLibraryName ? PathUtility.GetBinDirectory() : LocalBinDirectory, Path.GetFileName(AFileName));
		}
		
		public void GetFile(LocalProcess AProcess, string ALibraryName, string AFileName, DateTime AFileDate)
		{
			if (!FFilesCached.Contains(AFileName))
			{
				string LFullFileName = GetLocalFileName(ALibraryName, AFileName);
				if (!File.Exists(LFullFileName) || (File.GetLastWriteTimeUtc(LFullFileName) < AFileDate))
				{
					#if LOGFILECACHEEVENTS
					if (!File.Exists(LFullFileName))
						FInternalServer.LogMessage(String.Format(@"Downloading file ""{0}"" from server because it does not exist on the client.", LFullFileName));
					else
						FInternalServer.LogMessage(String.Format(@"Downloading newer version of file ""{0}"" from server. Client write time: ""{1}"". Server write time: ""{2}"".", LFullFileName, File.GetLastWriteTimeUtc(LFullFileName), AFileDate.ToString()));
					#endif
					using (Stream LSourceStream = new RemoteStreamWrapper(AProcess.RemoteProcess.GetFile(ALibraryName, AFileName)))
					{
						FileUtility.EnsureWriteable(LFullFileName);
						try
						{
							using (FileStream LTargetStream = File.Open(LFullFileName, FileMode.Create, FileAccess.Write, FileShare.None))
							{
								StreamUtility.CopyStreamWithBufferSize(LSourceStream, LTargetStream, CFileCopyBufferSize);
							}
							
							File.SetLastWriteTimeUtc(LFullFileName, AFileDate);
						}
						catch (IOException E)
						{
							FInternalServer.LogError(E);
						}
					}
					
					#if LOGFILECACHEEVENTS
					FInternalServer.LogMessage("Download complete");
					#endif
				}
				
				FFilesCached.Add(AFileName);
			}
		}
		
		public void ClassLoaderMissed(LocalProcess AProcess, Schema.ClassLoader AClassLoader, ClassDefinition AClassDefinition)
		{
			AcquireCacheLock(AProcess, LockMode.Exclusive);
			try
			{
				if (!AClassLoader.Classes.Contains(AClassDefinition.ClassName))
				{
					// The local process has attempted to create an object from an unknown class alias.
					// Use the remote server to attempt to download and install the necessary assemblies.
					//string AFullClassName = AProcess.RemoteProcess.GetClassName(AClassDefinition.ClassName); // BTR 5/17/2004 -> As far as I can tell, this doesn't do anything

					// Retrieve the list of all files required to load the assemblies required to load the class.
					string[] LLibraryNames;
					string[] LFileNames;
					DateTime[] LFileDates;
					string[] LAssemblyFileNames;
					AProcess.RemoteProcess.GetFileNames(AClassDefinition.ClassName, out LLibraryNames, out LFileNames, out LFileDates, out LAssemblyFileNames);
					for (int LIndex = 0; LIndex < LFileNames.Length; LIndex++)
						GetFile(AProcess, LLibraryNames[LIndex], LFileNames[LIndex], LFileDates[LIndex]);
					
					// Retrieve the list of all assemblies required to load the class.
					foreach (string LAssemblyFileName in LAssemblyFileNames)
					{
						if (!FAssembliesCached.Contains(LAssemblyFileName))
						{
							Assembly LAssembly = Assembly.LoadFrom(GetLocalFileName(LLibraryNames[Array.IndexOf<string>(LFileNames, LAssemblyFileName)], LAssemblyFileName));
							if (!AClassLoader.Assemblies.Contains(LAssembly.GetName()))
								AClassLoader.RegisterAssembly(Catalog.LoadedLibraries[Server.CSystemLibraryName], LAssembly);
							FAssembliesCached.Add(LAssemblyFileName);
						}
					}
				}
			}
			finally
			{
				ReleaseCacheLock(AProcess, LockMode.Exclusive);
			}
		}

        /// <summary>Starts the server instance.  If it is already running, the call has no effect.</summary>
        public void Start()
        {
			CheckServerConnection();
			FServer.Start();
		}
		
        /// <summary>Stops the server instance.  If it is not running, the call has no effect.</summary>
        public void Stop()
        {
			CheckServerConnection();
			FServer.Stop();
        }

		public Schema.Catalog Catalog { get { return FInternalServer.Catalog; } }
		
		/// <value>Returns the state of the server. </value>
		public ServerState State 
		{ 
			get 
			{ 
				CheckServerConnection();
				return FServer.State; 
			} 
		}
        
		/// <summary>
        ///     Connects to the server using the given parameters and returns an interface to the connection.
        ///     Will raise if the server is not running.
        /// </summary>
        /// <param name='ASessionInfo'>A <see cref="SessionInfo"/> object describing session configuration information for the connection request.</param>
        /// <returns>An <see cref="IServerSession"/> interface to the open session.</returns>
        public IServerSession Connect(SessionInfo ASessionInfo)
        {
			CheckServerConnection();
			if ((ASessionInfo.HostName == null) || (ASessionInfo.HostName == ""))
				ASessionInfo.HostName = FHostName;
			ASessionInfo.CatalogCacheName = FInternalServer.Name;
			IRemoteServerSession LSession = FServerConnection.Connect(ASessionInfo);
			return new LocalSession(this, ASessionInfo, LSession);
        }
        
        /// <summary>
        ///     Disconnects an active session.  If the server is not running, an exception will be raised.
        ///     If the given session is not a valid session for this server, an exception will be raised.
        /// </summary>
        public void Disconnect(IServerSession ASession)
        {
			try
			{
				CheckServerConnection();
				FServerConnection.Disconnect(((LocalSession)ASession).RemoteSession);
			}
			catch
			{
				// do nothing on an exception here
			}
			((LocalSession)ASession).Dispose();
        }
    }
    
    public class LocalSession : LocalServerChildObject, IServerSession
    {
		public LocalSession(LocalServer AServer, SessionInfo ASessionInfo, IRemoteServerSession ASession) : base()
		{
			FServer = AServer;
			FSession = ASession;
			FSessionInfo = ASessionInfo;
			FSessionID = ASession.SessionID;
			FInternalSession = ((IServer)FServer.FInternalServer).Connect(new SessionInfo(Alphora.Dataphor.DAE.Server.Server.CSystemUserID, String.Empty, DAE.Server.Server.CSystemLibraryName, false));
			StartKeepAlive();
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				StopKeepAlive();
			}
			finally
			{
				try
				{
					if (FInternalSession != null)
					{
						((IServer)FServer.FInternalServer).Disconnect(FInternalSession);
						FInternalSession = null;
					}
				}
				finally
				{
					FSession = null;
					FServer = null;
					base.Dispose(ADisposing);
				}
			}
		}

		protected internal LocalServer FServer;
        /// <value>Returns the <see cref="IServer"/> instance for this session.</value>
        public IServer Server { get { return FServer; } }
        
		private int FSessionID;
		public int SessionID { get { return FSessionID; } } 
		
		protected internal IServerSession FInternalSession;

		protected IRemoteServerSession FSession;
		public IRemoteServerSession RemoteSession { get { return FSession; } }
		
		public IServerProcess StartProcess(ProcessInfo AProcessInfo)
		{
			int LProcessID;
			IRemoteServerProcess LProcess = FSession.StartProcess(AProcessInfo, out LProcessID);
			return new LocalProcess(this, AProcessInfo, LProcessID, LProcess);
		}
		
		public void StopProcess(IServerProcess AProcess)
		{
			IRemoteServerProcess LRemoteProcess = ((LocalProcess)AProcess).RemoteProcess;
			((LocalProcess)AProcess).Dispose();
			try
			{
				FSession.StopProcess(LRemoteProcess);
			}
			catch
			{
				// ignore exceptions here
			}
		}
		
        private SessionInfo FSessionInfo;
        /// <value>Returns the <see cref="SessionInfo"/> object for this session.</value>
        public SessionInfo SessionInfo {  get { return FSessionInfo; } }

		#region Keep Alive

		/*
			Strategy: The client sends a keep alive message to the server connection every couple 
			 minutes.  This allows for very simple keep alive logic on the server:  If the server 
			 has not received a message from the client in n minutes, disconnect the client.
			 See the comment in the server connection for more details.
		*/

		public const int CLocalKeepAliveInterval = 120;	// 2 Minutes

		/// <summary> Signal used to indicate that the keep-alive thread can terminate. </summary>
		private ManualResetEvent FKeepAliveSignal;
		private Object FKeepAliveReferenceLock = new Object();

		private void StartKeepAlive()
		{
			lock (FKeepAliveReferenceLock)
			{
				if (FKeepAliveSignal != null)
					Error.Fail("Keep alive started more than once");
				FKeepAliveSignal = new ManualResetEvent(false);
			}
			new Thread(new ThreadStart(KeepAlive)).Start();	// Don't use the thread pool... long running thread
		}

		private void KeepAlive()
		{
			try
			{
				bool LSignaled = false;
				while (!LSignaled)
				{
					// Wait for either a signal or a time-out
					LSignaled = FKeepAliveSignal.WaitOne(CLocalKeepAliveInterval * 1000, false);

					// If WaitOne ended due to timeout, send a keep-alive message (then wait again)
					if (!LSignaled)
						FServer.ServerConnection.Ping();
				}

				// The keep alive processing is complete.  Clean up...
				lock (FKeepAliveReferenceLock)
				{
					((IDisposable)FKeepAliveSignal).Dispose();
					FKeepAliveSignal = null;	// Free the reference 
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private void StopKeepAlive()
		{
			lock (FKeepAliveReferenceLock)
			{
				if (FKeepAliveSignal != null)
					FKeepAliveSignal.Set();
			}
		}
		
		#endregion
	}
    
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
								AParams[LIndex].Value = LRow[LIndex].Copy();
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
    
    public class LocalScript : LocalServerChildObject, IServerScript
    {
		public LocalScript(LocalProcess AProcess, IRemoteServerScript AScript) : base()
		{
			FProcess = AProcess;
			FScript = AScript;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FBatches != null)
			{
				FBatches.Dispose();
				FBatches = null;
			}

			FScript = null;
			FProcess = null;
			
			base.Dispose(ADisposing);
		}

		protected IRemoteServerScript FScript;
		public IRemoteServerScript RemoteScript { get { return FScript; } }
		
		protected internal LocalProcess FProcess;
		public IServerProcess Process { get { return FProcess; } }
		
		public void Execute(DataParams AParams)
		{
			RemoteParamData LParams = ((LocalProcess)FProcess).DataParamsToRemoteParamData(AParams);
			FScript.Execute(ref LParams, FProcess.GetProcessCallInfo());
			((LocalProcess)FProcess).RemoteParamDataToDataParams(AParams, LParams);
		}

		private ParserMessages FMessages;		
		public ParserMessages Messages
		{
			get
			{
				if (FMessages == null)
				{
					FMessages = new ParserMessages();
					FMessages.AddRange(FScript.Messages);
				}
				return FMessages;
			}
		}
		
		protected LocalBatches FBatches;
		public IServerBatches Batches
		{
			get
			{
				if (FBatches == null)
				{
					FBatches = new LocalBatches();
					foreach (object LBatch in FScript.Batches)
						FBatches.Add(new LocalBatch(this, (IRemoteServerBatch)LBatch));
				}
				return (IServerBatches)FBatches;
			}
		}
    }
    
    public class LocalBatch : LocalServerChildObject, IServerBatch
    {
		public LocalBatch(LocalScript AScript, IRemoteServerBatch ABatch) : base()
		{
			FScript = AScript;
			FBatch = ABatch;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FBatch = null;
			FScript = null;
			base.Dispose(ADisposing);
		}

		protected IRemoteServerBatch FBatch;
		public IRemoteServerBatch RemoteBatch { get { return FBatch; } }
		
		protected internal LocalScript FScript;
		public IServerScript ServerScript { get { return FScript; } }
		
		public IServerProcess ServerProcess { get { return FScript.Process; } }
		
		public void Execute(DataParams AParams)
		{
			RemoteParamData LParams = FScript.FProcess.DataParamsToRemoteParamData(AParams);
			FBatch.Execute(ref LParams, FScript.FProcess.GetProcessCallInfo());
			FScript.FProcess.RemoteParamDataToDataParams(AParams, LParams);
		}
		
		public bool IsExpression()
		{
			return FBatch.IsExpression();
		}
		
		public string GetText()
		{
			return FBatch.GetText();
		}
		
		public int Line { get { return FBatch.Line; } }

		public IServerPlan Prepare(DataParams AParams)
		{
			if (IsExpression())
				return PrepareExpression(AParams);
			else
				return PrepareStatement(AParams);
		}
		
		public void Unprepare(IServerPlan APlan)
		{
			if (APlan is IServerExpressionPlan)
				UnprepareExpression((IServerExpressionPlan)APlan);
			else
				UnprepareStatement((IServerStatementPlan)APlan);
		}
		
		public IServerExpressionPlan PrepareExpression(DataParams AParams)
		{
			#if LOGCACHEEVENTS
			FScript.FProcess.FSession.FServer.FInternalServer.LogMessage(String.Format("Thread {0} preparing batched expression '{1}'.", Thread.CurrentThread.GetHashCode(), GetText()));
			#endif
			
			PlanDescriptor LPlanDescriptor;
			IRemoteServerExpressionPlan LPlan = FBatch.PrepareExpression(((LocalProcess)ServerProcess).DataParamsToRemoteParams(AParams), out LPlanDescriptor);
			return new LocalExpressionPlan(FScript.FProcess, LPlan, LPlanDescriptor, AParams);
		}
		
		public void UnprepareExpression(IServerExpressionPlan APlan)
		{
			try
			{
				FBatch.UnprepareExpression(((LocalExpressionPlan)APlan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalExpressionPlan)APlan).Dispose();
		}
		
		public IServerStatementPlan PrepareStatement(DataParams AParams)
		{
			PlanDescriptor LPlanDescriptor;
			IRemoteServerStatementPlan LPlan = FBatch.PrepareStatement(((LocalProcess)ServerProcess).DataParamsToRemoteParams(AParams), out LPlanDescriptor);
			return new LocalStatementPlan(FScript.FProcess, LPlan, LPlanDescriptor);
		}
		
		public void UnprepareStatement(IServerStatementPlan APlan)
		{
			try
			{
				FBatch.UnprepareStatement(((LocalStatementPlan)APlan).RemotePlan);
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalStatementPlan)APlan).Dispose();
		}
    }
    
	// TODO: Protect LocalBatches and other DisposableTypedList CLI descendants from "unauthorized" disposal

    public class LocalBatches : DisposableTypedList, IServerBatches
    {
		public LocalBatches() : base(typeof(LocalBatch), true, false) {}
		
		public new LocalBatch this[int AIndex]
		{
			get { return (LocalBatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		IServerBatch IServerBatches.this[int AIndex]
		{
			get { return (IServerBatch)base[AIndex]; }
			set { base[AIndex] = value; }
		}
    }
    
    public class LocalPlan : LocalServerChildObject, IServerPlan
    {
		public LocalPlan(LocalProcess AProcess, IRemoteServerPlan APlan, PlanDescriptor APlanDescriptor) : base()
		{
			FProcess = AProcess;
			FPlan = APlan;
			FDescriptor = APlanDescriptor;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FDescriptor = null;
			FProcess = null;
			FPlan = null;
			base.Dispose(ADisposing);
		}
		
		private IRemoteServerPlan FPlan;
		
		protected PlanDescriptor FDescriptor;
		
		protected internal LocalProcess FProcess;
        /// <value>Returns the <see cref="IServerProcess"/> instance for this plan.</value>
        public IServerProcess Process { get { return FProcess; } } 

		public LocalServer LocalServer { get { return FProcess.FSession.FServer; } }
		
		public Guid ID { get { return FDescriptor.ID; } }
		
		private CompilerMessages FMessages;
		public CompilerMessages Messages
		{
			get
			{
				if (FMessages == null)
				{
					FMessages = new CompilerMessages();
					FMessages.AddRange(FDescriptor.Messages);
				}
				return FMessages;
			}
		}
		
		public void CheckCompiled()
		{
			FPlan.CheckCompiled();
		}
		
		// Statistics
		internal bool FStatisticsCached = true;
		public PlanStatistics Statistics 
		{ 
			get 
			{ 
				if (!FStatisticsCached)
				{
					FDescriptor.Statistics = FPlan.Statistics;
					FStatisticsCached = true;
				}
				return FDescriptor.Statistics; 
			} 
		}
	}
    
    public class LocalExpressionPlan : LocalPlan, IServerExpressionPlan
    {
		public LocalExpressionPlan(LocalProcess AProcess, IRemoteServerExpressionPlan APlan, PlanDescriptor APlanDescriptor, DataParams AParams) : base(AProcess, APlan, APlanDescriptor)
		{
			FPlan = APlan;
			FParams = AParams;
			GetDataType();
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FDataType != null)
					DropDataType();
			}
			finally
			{
				FPlan = null;
				FParams = null;
				FDataType = null;
				base.Dispose(ADisposing);
			}
		}

		protected DataParams FParams;
		protected IRemoteServerExpressionPlan FPlan;
		public IRemoteServerExpressionPlan RemotePlan { get { return FPlan; } }
		
		// Isolation
		public CursorIsolation Isolation { get { return FDescriptor.CursorIsolation; } }
		
		// CursorType
		public CursorType CursorType { get { return FDescriptor.CursorType; } }
		
		// Capabilities		
		public CursorCapability Capabilities { get { return FDescriptor.Capabilities; } }

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}
		
		public DataValue Evaluate(DataParams AParams)
		{
			RemoteParamData LParams = FProcess.DataParamsToRemoteParamData(AParams);
			byte[] LResult = FPlan.Evaluate(ref LParams, out FDescriptor.Statistics.ExecuteTime, FProcess.GetProcessCallInfo());
			FStatisticsCached = false;
			FProcess.RemoteParamDataToDataParams(AParams, LParams);
			return LResult == null ? null : DataValue.FromPhysical(FProcess, DataType, LResult, 0);
		}

        /// <summary>Opens a server-side cursor based on the prepared statement this plan represents.</summary>        
        /// <returns>An <see cref="IServerCursor"/> instance for the prepared statement.</returns>
        public IServerCursor Open(DataParams AParams)
        {
			RemoteParamData LParams = ((LocalProcess)FProcess).DataParamsToRemoteParamData(AParams);
			IRemoteServerCursor LServerCursor;
			LocalCursor LCursor;
			if (FProcess.ProcessInfo.FetchAtOpen && (FProcess.ProcessInfo.FetchCount > 1) && Supports(CursorCapability.Bookmarkable))
			{
				Guid[] LBookmarks;
				RemoteFetchData LFetchData;
				LServerCursor = FPlan.Open(ref LParams, out FDescriptor.Statistics.ExecuteTime, out LBookmarks, FProcess.ProcessInfo.FetchCount, out LFetchData, FProcess.GetProcessCallInfo());
				FStatisticsCached = false;
				LCursor = new LocalCursor(this, LServerCursor);
				LCursor.ProcessFetchData(LFetchData, LBookmarks, true);
			}
			else
			{
				LServerCursor = FPlan.Open(ref LParams, out FDescriptor.Statistics.ExecuteTime, FProcess.GetProcessCallInfo());
				FStatisticsCached = false;
				LCursor = new LocalCursor(this, LServerCursor);
			}
			((LocalProcess)FProcess).RemoteParamDataToDataParams(AParams, LParams);
			return LCursor;
		}
		
        /// <summary>Closes a server-side cursor previously created using Open.</summary>
        /// <param name="ACursor">The cursor to close.</param>
        public void Close(IServerCursor ACursor)
        {
			try
			{
				FPlan.Close(((LocalCursor)ACursor).RemoteCursor, FProcess.GetProcessCallInfo());
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalCursor)ACursor).Dispose();
		}
		
		private void DropDataType()
		{
			if (FTableVar is Schema.DerivedTableVar)
			{
				LocalServer.AcquireCacheLock(FProcess, LockMode.Exclusive);
				try
				{
					(new DropViewNode((Schema.DerivedTableVar)FTableVar)).Execute(FProcess.FInternalProcess);
				}
				finally
				{
					LocalServer.ReleaseCacheLock(FProcess, LockMode.Exclusive);
				}
			}
		}
		
		private Schema.IDataType GetDataType()
		{
			bool LTimeStampSet = false;
			try
			{
				LocalServer.WaitForCacheTimeStamp(FProcess, FDescriptor.CacheChanged ? FDescriptor.ClientCacheTimeStamp - 1 : FDescriptor.ClientCacheTimeStamp);
				LocalServer.AcquireCacheLock(FProcess, FDescriptor.CacheChanged ? LockMode.Exclusive : LockMode.Shared);
				try
				{
					FProcess.FInternalProcess.Context.PushWindow(0);
					try
					{
						if (FDescriptor.CacheChanged)
						{
							LocalServer.EnsureCacheConsistent(FDescriptor.CacheTimeStamp);
							try
							{
								if (FDescriptor.Catalog != String.Empty)
								{
									IServerScript LScript = ((IServerProcess)FProcess.FInternalProcess).PrepareScript(FDescriptor.Catalog);
									try
									{
										LScript.Execute(FParams);
									}
									finally
									{
										((IServerProcess)FProcess.FInternalProcess).UnprepareScript(LScript);
									}
								}
							}
							finally
							{
								LocalServer.SetCacheTimeStamp(FProcess, FDescriptor.ClientCacheTimeStamp);
								LTimeStampSet = true;
							}
						}
						
						if (LocalServer.Catalog.ContainsName(FDescriptor.ObjectName))
						{
							Schema.Object LObject = LocalServer.Catalog[FDescriptor.ObjectName];
							if (LObject is Schema.TableVar)
							{
								FTableVar = (Schema.TableVar)LObject;
								Plan LPlan = new Plan(FProcess.FInternalProcess);
								try
								{
									if (FParams != null)
										foreach (DataParam LParam in FParams)
											LPlan.Symbols.Push(new DataVar(LParam.Name, LParam.DataType));
											
									for (int LIndex = FProcess.FInternalProcess.Context.Count - 1; LIndex >= 0; LIndex--)
										LPlan.Symbols.Push(FProcess.FInternalProcess.Context[LIndex]);
									FTableNode = (TableNode)Compiler.EmitTableVarNode(LPlan, FTableVar);
								}
								finally
								{
									LPlan.Dispose();
								}
								FDataType = FTableVar.DataType;			
							}
							else
								FDataType = (Schema.IDataType)LObject;
						}
						else
						{
							try
							{
								Plan LPlan = new Plan(FProcess.FInternalProcess);
								try
								{
									FDataType = Compiler.CompileTypeSpecifier(LPlan, new DAE.Language.D4.Parser().ParseTypeSpecifier(FDescriptor.ObjectName));
								}
								finally
								{
									LPlan.Dispose();
								}
							}
							catch
							{
								// Notify the server that the client cache is out of sync
								Process.Execute(".System.UpdateTimeStamps();", null);
								throw;
							}
						}
						
						return FDataType;
					}
					finally
					{
						FProcess.FInternalProcess.Context.PopWindow();
					}
				}
				finally
				{
					LocalServer.ReleaseCacheLock(FProcess, FDescriptor.CacheChanged ? LockMode.Exclusive : LockMode.Shared);
				}
			}
			catch (Exception E)
			{
				// Notify the server that the client cache is out of sync
				Process.Execute(".System.UpdateTimeStamps();", null);
				E = new ServerException(ServerException.Codes.CacheDeserializationError, E, FDescriptor.ClientCacheTimeStamp);
				LocalServer.FInternalServer.LogError(E);
				throw E;
			}
			finally
			{
				if (!LTimeStampSet)
					LocalServer.SetCacheTimeStamp(FProcess, FDescriptor.ClientCacheTimeStamp);
			}
		}
		
		public Schema.Catalog Catalog 
		{ 
			get 
			{ 
				if (FDataType == null)
					GetDataType();
				return LocalServer.Catalog;
			} 
		}

		protected Schema.TableVar FTableVar;		
		public Schema.TableVar TableVar
		{
			get
			{
				if (FDataType == null)
					GetDataType();
				return FTableVar;
			}
		}
		
		protected TableNode FTableNode;
		public TableNode TableNode
		{
			get
			{
				if (FDataType == null)
					GetDataType();
				return FTableNode;
			}
		}
		
		protected Schema.IDataType FDataType;
		public Schema.IDataType DataType
		{
			get
			{
				if (FDataType == null)
					return GetDataType(); 
				return FDataType;
			}
		}

		private Schema.Order FOrder;
		public Schema.Order Order
        {
			get 
			{
				if ((FOrder == null) && (FDescriptor.Order != String.Empty))
					FOrder = Compiler.CompileOrderDefinition(FProcess.FInternalProcess.Plan, TableVar, new Parser().ParseOrderDefinition(FDescriptor.Order), false);
				return FOrder; 
			}
		}
		
		Statement IServerExpressionPlan.EmitStatement()
		{
			throw new ServerException(ServerException.Codes.Unsupported);
		}
		
		public Row RequestRow()
		{
			return new Row(FProcess, TableVar.DataType.RowType);
		}
		
		public void ReleaseRow(Row ARow)
		{
			ARow.Dispose();
		}
    }
    
    public class LocalStatementPlan : LocalPlan, IServerStatementPlan
    {
		public LocalStatementPlan(LocalProcess AProcess, IRemoteServerStatementPlan APlan, PlanDescriptor APlanDescriptor) : base(AProcess, APlan, APlanDescriptor)
		{
			FPlan = APlan;
		}

		protected override void Dispose(bool ADisposing)
		{
			FPlan = null;
			base.Dispose(ADisposing);
		}

		protected IRemoteServerStatementPlan FPlan;
		public IRemoteServerStatementPlan RemotePlan { get { return FPlan; } }
		
        /// <summary>Executes the prepared statement this plan represents.</summary>
        public void Execute(DataParams AParams)
        {
			RemoteParamData LParams = FProcess.DataParamsToRemoteParamData(AParams);
			FPlan.Execute(ref LParams, out FDescriptor.Statistics.ExecuteTime, FProcess.GetProcessCallInfo());
			FStatisticsCached = false;
			FProcess.RemoteParamDataToDataParams(AParams, LParams);
		}
    }
    
    public class LocalBookmark : System.Object
    {
		public LocalBookmark(Guid ABookmark)
		{
			FBookmark = ABookmark;
			ReferenceCount = 1;
		}
		
		private Guid FBookmark;
		public Guid Bookmark { get { return FBookmark; } }
		
		public int ReferenceCount;
    }
    
    public class LocalBookmarks : Hashtable
    {
		public void Add(LocalBookmark ABookmark)
		{
			Add(ABookmark.Bookmark, ABookmark);
		}
		
		public LocalBookmark this[Guid AGuid] { get { return (LocalBookmark)base[AGuid]; } }
    }
    
    public class LocalRow : Disposable
    {
		public LocalRow(Row ARow, Guid ABookmark)
		{
			FRow = ARow;
			FBookmark = ABookmark;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRow != null)
			{
				FRow.Dispose();
				FRow = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		protected Row FRow;
		public Row Row { get { return FRow; } }

		protected Guid FBookmark;
		public Guid Bookmark { get { return FBookmark; } }
    }

    public class LocalRows : DisposableTypedList
    {
		public LocalRows() : base(typeof(LocalRow), true, false){}

		public new LocalRow this[int AIndex]
		{
			get { return (LocalRow)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public BufferDirection BufferDirection;
    }
    
    public enum BufferDirection { Forward = 1, Backward = -1 }
    
    public class LocalCursor : LocalServerChildObject, IServerCursor
    {	
		public LocalCursor(LocalExpressionPlan APlan, IRemoteServerCursor ACursor) : base()
		{
			FPlan = APlan;
			FCursor = ACursor;
			FInternalProcess = FPlan.FProcess.FInternalProcess;
			FBuffer = new LocalRows();
			FBookmarks = new LocalBookmarks();
			FFetchCount = FPlan.FProcess.ProcessInfo.FetchCount;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FBuffer != null)
			{
				FBuffer.Dispose();
				FBuffer = null;
			}
			
			FInternalProcess = null;
			FCursor = null;
			FPlan = null;
			base.Dispose(ADisposing);
		}
		
		private ServerProcess FInternalProcess;

		protected LocalExpressionPlan FPlan;
        /// <value>Returns the <see cref="IServerExpressionPlan"/> instance for this cursor.</value>
        public IServerExpressionPlan Plan { get { return FPlan; } }
		
		protected IRemoteServerCursor FCursor;
		public IRemoteServerCursor RemoteCursor { get { return FCursor; } }

		// CursorType
		public CursorType CursorType { get { return FPlan.CursorType; } }

		// Isolation
		public CursorIsolation Isolation { get { return FPlan.Isolation; } }

		// Capabilites
		public CursorCapability Capabilities { get { return FPlan.Capabilities; } }
		
		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}

		protected int FFetchCount = SessionInfo.CDefaultFetchCount;
		/// <value>Gets or sets the number of rows to fetch at a time.  </value>
		/// <remarks>
		/// FetchCount must be greater than or equal to 1.  
		/// This setting is only valid for cursors which support Bookmarkable.
		/// A setting of 1 disables fetching behavior.
		/// </remarks>
		public int FetchCount 
		{
			get { return FFetchCount; } 
			set { FFetchCount = ((value < 1) ? 1 : value); } 
		}
		
		/*
			Fetch Behavior ->
			
				The buffer is fetched in the direction of the last navigation call
			
				Select ->
					if the cache is populated
						return the row from the cache
					else
						populate the cache from the underlying cursor in the direction indicated by the buffer direction variable
					
				Next ->
					if the cache is populated,
						increment current
						if current >= buffer count
							goto current - 1
							clear the buffer
							set buffer direction forward
							move next on the underlying cursor
					else
						move next on the underlying cursor
							
				Prior ->
					if the cache is populated,
						decrement current
						if current < 0
							goto current + 1
							clear the buffer
							set buffer direction backwards
							move prior on the underlying cursor
					else
						move prior on the underlying cursor
						
				First ->
					if the cache is populated,
						clear it
						set buffer direction forward
					call first on the source cursor and set the cache flags based on it
				
				Last ->
					if the cache is populated,
						clear it
						set buffer direction backwards
					call last on the source cursor and set the cache flags based on it
					
				BOF ->
					if the cache is populated
						FBOF = FFlags.BOF && FBufferIndex < 0;
					else
						FBOF = FFlags.BOF
				
				EOF ->
					if the cache is populated
						FEOF = FFlags.EOF && FBufferIndex >= FBuffer.Count;
					else
						FEOF = FFlags.EOF;
				
				GetBookmark() ->
					if the cache is populated
						return the bookmark for the current row
					else
						populate the cache based on the buffer direction
						set FBOF and FEOF based on the result flags and the number of rows returned
				
				GotoBookmark() ->
					if the cache is populated
						if the bookmark is in the cache
							set current to the bookmark
						else
							clear the cache
							set buffer direction forward
							goto the bookmark on the underlying cursor
					else
						goto the bookmark on the underlying cursor
				
				CompareBookmarks() ->
					if the bookmarks are equal,
						return 0
					else
						return compare bookmarks on the underlying cursor
				
				GetKey() ->
					if the cache is populated
						goto current
					return get key on the underlying cursor

				FindKey() ->
					execute find key on the underlying cursor
					if the key was found
						if the cache is populated
							clear it
							set buffer direction backwards // this is to take advantage of the way the view fills the buffer and preserves an optimization based on the underlying browse cursor behavior

				FindNearest() ->
					if the cache is populated
						clear it
						set buffer direction backwards
					execute find nearest on the underlying cursor
				
				Refresh() ->
					if the cache is populated
						clear it
						set buffer direction backwards
					execute refresh on the underlying cursor
				
				Insert() ->
					execute the insert on the underlying cursor
					if the cache is populated
						clear it
						set buffer direction backwards
				
				Update() ->
					if the cache is populated
						goto current
					execute the update on the underlying cursor
					if the cache is populated
						clear it
						set buffer direction backwards
				
				Delete() ->
					if the cache is populated
						goto current
					execute the delete on the underlying cursor
					if the cache is populated
						clear it
						set buffer direction backwards
				
				Default() ->
				Change() ->
				Validate() ->
					The proposable interfaces do not require a located cursor so they have no effect on the caching
		*/
		
		protected LocalRows FBuffer;
		protected LocalBookmarks FBookmarks;
		protected int FBufferIndex = -1; 
		protected bool FBufferFull;
		//protected bool FBufferFirst;
		protected BufferDirection FBufferDirection = BufferDirection.Forward;
		
		protected bool BufferActive()
		{
			return UseBuffer() && FBufferFull;
		}
		
		protected bool UseBuffer()
		{
			return (FFetchCount > 1) && Supports(CursorCapability.Bookmarkable);
		}
		
		protected void ClearBuffer()
		{
			for (int LIndex = 0; LIndex < FBuffer.Count; LIndex++)
				BufferDisposeBookmark(FBuffer[LIndex].Bookmark);
			FBuffer.Clear();
			FBufferIndex = -1;
			FBufferFull = false;
		}
		
		protected void SetBufferDirection(BufferDirection ABufferDirection)
		{
			FBufferDirection = ABufferDirection;
		}
		
        public void Open()
        {
			FCursor.Open();
		}
		
        public void Close()
        {
			if (BufferActive())
				ClearBuffer();
			FCursor.Close();
		}
		
        public bool Active
        {
			get { return FCursor.Active; }
			set { FCursor.Active = value; }
		}

		// Flags tracks the current status of the remote cursor, BOF, EOF, none, or both
		protected bool FFlagsCached;
		protected RemoteCursorGetFlags FFlags;
		
		protected void SetFlags(RemoteCursorGetFlags AFlags)
		{
			FFlags = AFlags;
			FFlagsCached = true;
		}
		
		protected RemoteCursorGetFlags GetFlags()
		{
			if (!FFlagsCached)
			{
				SetFlags(FCursor.GetFlags(FPlan.FProcess.GetProcessCallInfo()));
				FPlan.FStatisticsCached = false;
			}
			return FFlags;
		}

		public void Reset()
        {
			if (BufferActive())
				ClearBuffer();
			SetFlags(FCursor.Reset(FPlan.FProcess.GetProcessCallInfo()));
			SetBufferDirection(BufferDirection.Forward);
			FPlan.FStatisticsCached = false;
		}

        public Row Select()
        {
			Row LRow = new Row(FPlan.FProcess, ((Schema.TableType)FPlan.DataType).RowType);
			try
			{
				Select(LRow);
			}
			catch
			{
				LRow.Dispose();
				throw;
			}
			return LRow;
		}
		
		private void SourceSelect(Row ARow)
		{
			RemoteRowHeader LHeader = new RemoteRowHeader();
			LHeader.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LHeader.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			ARow.ValuesOwned = false;
			byte[] AData = FCursor.Select(LHeader, FPlan.FProcess.GetProcessCallInfo()).Data;
			ARow.AsPhysical = AData;
			FPlan.FStatisticsCached = false;
		}
		
		private void BufferSelect(Row ARow)
		{
			// TODO: implement a version of CopyTo that does not copy overflow 
			// problem is that this requires a row type of exactly the same type as the cursor table type
			FBuffer[FBufferIndex].Row.CopyTo(ARow);
		}
		
        public void Select(Row ARow)
        {
			if (BufferActive())
				BufferSelect(ARow);
			else
			{
				if (UseBuffer())
				{
					SourceFetch(false);
					BufferSelect(ARow);
				}
				else
					SourceSelect(ARow);
			}
		}
		
		protected void SourceFetch(bool AIsFirst)
		{
			// Execute fetch on the remote cursor, selecting all columns, requesting FFetchCount rows from the current position
			Guid[] LBookmarks;
			RemoteFetchData LFetchData = FCursor.Fetch(out LBookmarks, FFetchCount * (int)FBufferDirection, FPlan.FProcess.GetProcessCallInfo());
			ProcessFetchData(LFetchData, LBookmarks, AIsFirst);
			FPlan.FStatisticsCached = false;
		}
		
		public void ProcessFetchData(RemoteFetchData AFetchData, Guid[] ABookmarks, bool AIsFirst)
		{
			FBuffer.BufferDirection = FBufferDirection;
			Schema.IRowType LRowType = DataType.RowType;
			if (FBufferDirection == BufferDirection.Forward)
			{
				for (int LIndex = 0; LIndex < AFetchData.Body.Length; LIndex++)
				{
					LocalRow LRow = new LocalRow(new Row(FPlan.FProcess, LRowType), ABookmarks[LIndex]);
					LRow.Row.AsPhysical = AFetchData.Body[LIndex].Data;
					FBuffer.Add(LRow);
					FBookmarks.Add(new LocalBookmark(ABookmarks[LIndex]));
				}
				
				if ((AFetchData.Body.Length > 0) && !AIsFirst)
					FBufferIndex = 0;
				else
					FBufferIndex = -1;
			}
			else
			{
				for (int LIndex = 0; LIndex < AFetchData.Body.Length; LIndex++)
				{
					LocalRow LRow = new LocalRow(new Row(FPlan.FProcess, LRowType), ABookmarks[LIndex]);
					LRow.Row.AsPhysical = AFetchData.Body[LIndex].Data;
					FBuffer.Insert(0, LRow);
					FBookmarks.Add(new LocalBookmark(ABookmarks[LIndex]));
				}
				
				if ((AFetchData.Body.Length > 0) && !AIsFirst)
					FBufferIndex = FBuffer.Count - 1;
				else
					FBufferIndex = -1;
			}
			
			SetFlags((RemoteCursorGetFlags)AFetchData.Flags);//hack: cast to flag to fix MS bug
			FBufferFull = true;
		}
		
		protected bool SourceNext()
		{
			RemoteMoveData LMoveData = FCursor.MoveBy(1, FPlan.FProcess.GetProcessCallInfo());
			FPlan.FStatisticsCached = false;
			SetFlags(LMoveData.Flags);
			return LMoveData.Flags == RemoteCursorGetFlags.None;
		}
		
		protected void SyncSource(bool AForward)
		{
			if (!SourceGotoBookmark(FBuffer[FBufferIndex].Bookmark, AForward))
				throw new ServerException(ServerException.Codes.CursorSyncError);
		}
		
        public bool Next()
        {
			SetBufferDirection(BufferDirection.Forward);
			if (UseBuffer())
			{
				if (!BufferActive())
					SourceFetch(SourceBOF());
					
				if (FBufferIndex >= FBuffer.Count - 1)
				{
					if (SourceEOF())
					{
						FBufferIndex++;
						return false;
					}

					SyncSource(true);
					ClearBuffer();
					return SourceNext();
				}
				FBufferIndex++;
				return true;
			}

			if (FFlagsCached && SourceEOF())
				return false;
			return SourceNext();
		}
		
		protected void SourceLast()
		{
			SetFlags(FCursor.Last(FPlan.FProcess.GetProcessCallInfo()));
			FPlan.FStatisticsCached = false;
		}
		
        public void Last()
        {
			SetBufferDirection(BufferDirection.Backward);
			if (BufferActive())
				ClearBuffer();
			SourceLast();					
		}
		
		protected bool SourceBOF()
		{
			return (GetFlags() & RemoteCursorGetFlags.BOF) != 0;
		}

        public bool BOF()
		{
			if (BufferActive())
				return SourceBOF() && (FBufferIndex < 0);
			else
				return SourceBOF();
		}
		
		protected bool SourceEOF()
		{
			return (GetFlags() & RemoteCursorGetFlags.EOF) != 0;
		}
		
        public bool EOF()
        {
			if (BufferActive())
				return SourceEOF() && (FBufferIndex >= FBuffer.Count);
			else
				return SourceEOF();
		}
		
        public bool IsEmpty()
        {
			return BOF() && EOF();
		}
		
		public void Insert(Row ARow)
		{
			Insert(ARow, null);
		}
		
        public void Insert(Row ARow, BitArray AValueFlags)
        {
			RemoteRow LRow = new RemoteRow();
			FPlan.FProcess.EnsureOverflowReleased(ARow);
			LRow.Header = new RemoteRowHeader();
			LRow.Header.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LRow.Header.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			LRow.Body = new RemoteRowBody();
			LRow.Body.Data = ARow.AsPhysical;
			FCursor.Insert(LRow, AValueFlags, FPlan.FProcess.GetProcessCallInfo());
			FFlagsCached = false;
			FPlan.FStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
		public void Update(Row ARow)
		{
			Update(ARow, null);
		}
		
        public void Update(Row ARow, BitArray AValueFlags)
        {
			RemoteRow LRow = new RemoteRow();
			FPlan.FProcess.EnsureOverflowReleased(ARow);
			LRow.Header = new RemoteRowHeader();
			LRow.Header.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LRow.Header.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			LRow.Body = new RemoteRowBody();
			LRow.Body.Data = ARow.AsPhysical;
			if (BufferActive())
				SyncSource(true);
			FCursor.Update(LRow, AValueFlags, FPlan.FProcess.GetProcessCallInfo());
			FFlagsCached = false;
			FPlan.FStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
        public void Delete()
        {
			if (BufferActive())
				SyncSource(true);
			FCursor.Delete(FPlan.FProcess.GetProcessCallInfo());
			FFlagsCached = false;
			FPlan.FStatisticsCached = false;
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
		}
		
		protected void SourceFirst()
		{
			SetFlags(FCursor.First(FPlan.FProcess.GetProcessCallInfo()));
			FPlan.FStatisticsCached = false;
		}

        public void First()
        {
			SetBufferDirection(BufferDirection.Forward);
			if (BufferActive())
				ClearBuffer();
			SourceFirst();
		}
		
		protected bool SourcePrior()
		{
			RemoteMoveData LMoveData = FCursor.MoveBy(-1, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LMoveData.Flags);
			FPlan.FStatisticsCached = false;
			return LMoveData.Flags == RemoteCursorGetFlags.None;
		}
		
        public bool Prior()
        {
			SetBufferDirection(BufferDirection.Backward);
			if (BufferActive())
			{
				if (FBufferIndex <= 0)
				{
					if (SourceBOF())
					{
						FBufferIndex--;
						return false;
					}

					SyncSource(false);
					ClearBuffer();
					return SourcePrior();
				}
				FBufferIndex--;
				return true;
			}
			if (FFlagsCached && SourceBOF())
				return false;
			return SourcePrior();
		}
		
		protected Guid SourceGetBookmark()
		{
			FPlan.FStatisticsCached = false;
			return FCursor.GetBookmark(FPlan.FProcess.GetProcessCallInfo());
		}

        public Guid GetBookmark()
        {
			if (BufferActive())
			{
				FBookmarks[FBuffer[FBufferIndex].Bookmark].ReferenceCount++;
				return FBuffer[FBufferIndex].Bookmark;
			}

			if (UseBuffer())
			{
				SourceFetch(FBuffer.BufferDirection == BufferDirection.Forward ? SourceBOF() : SourceEOF());
				FBookmarks[FBuffer[FBufferIndex].Bookmark].ReferenceCount++;
				return FBuffer[FBufferIndex].Bookmark;
			}

			return SourceGetBookmark();
        }

		protected bool SourceGotoBookmark(Guid ABookmark, bool AForward)
        {
			RemoteGotoData LGotoData = FCursor.GotoBookmark(ABookmark, AForward, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LGotoData.Flags);
			FPlan.FStatisticsCached = false;
			return LGotoData.Success;
        }

		public bool GotoBookmark(Guid ABookmark, bool AForward)
        {
			SetBufferDirection((AForward ? BufferDirection.Forward : BufferDirection.Backward));
			if (UseBuffer())
			{
				for (int LIndex = 0; LIndex < FBuffer.Count; LIndex++)
					if (FBuffer[LIndex].Bookmark == ABookmark)
					{
						FBufferIndex = LIndex;
						return true;
					}
				
				ClearBuffer();
				return SourceGotoBookmark(ABookmark, AForward);
			}
			return SourceGotoBookmark(ABookmark, AForward);
		}
		
        public int CompareBookmarks(Guid ABookmark1, Guid ABookmark2)
        {
			FPlan.FStatisticsCached = false;
			return FCursor.CompareBookmarks(ABookmark1, ABookmark2, FPlan.FProcess.GetProcessCallInfo());
		}
		
		protected void SourceDisposeBookmark(Guid ABookmark)
		{
			FCursor.DisposeBookmark(ABookmark, FPlan.FProcess.GetProcessCallInfo());
			FPlan.FStatisticsCached = false;
		}
		
		protected void BufferDisposeBookmark(Guid ABookmark)
		{
			if (ABookmark != Guid.Empty)
			{
				LocalBookmark LBookmark = FBookmarks[ABookmark];
				if (LBookmark == null)
					throw new ServerException(ServerException.Codes.InvalidBookmark, ABookmark.ToString());
					
				LBookmark.ReferenceCount--;
				
				if (LBookmark.ReferenceCount <= 0)
				{
					SourceDisposeBookmark(LBookmark.Bookmark);
					FBookmarks.Remove(LBookmark.Bookmark);
				}
			}
		}

		public void DisposeBookmark(Guid ABookmark)
		{
			if (BufferActive())
				BufferDisposeBookmark(ABookmark);
			else
				SourceDisposeBookmark(ABookmark);
		}
		
		protected void SourceDisposeBookmarks(Guid[] ABookmarks)
		{
			FCursor.DisposeBookmarks(ABookmarks, FPlan.FProcess.GetProcessCallInfo());
			FPlan.FStatisticsCached = false;
		}
		
		protected void BufferDisposeBookmarks(Guid[] ABookmarks)
		{
			for (int LIndex = 0; LIndex < ABookmarks.Length; LIndex++)
				BufferDisposeBookmark(ABookmarks[LIndex]);
		}

		public void DisposeBookmarks(Guid[] ABookmarks)
		{
			if (BufferActive())
				BufferDisposeBookmarks(ABookmarks);
			else
				SourceDisposeBookmarks(ABookmarks);
		}
		
		public Schema.Order Order { get { return FPlan.Order; } }
		
        public Row GetKey()
        {
			if (BufferActive())
				SyncSource(true);
			RemoteRow LKey = FCursor.GetKey(FPlan.FProcess.GetProcessCallInfo());
			FPlan.FStatisticsCached = false;
			Row LRow;
			Schema.RowType LType = new Schema.RowType();
			foreach (string LString in LKey.Header.Columns)
				LType.Columns.Add(((Schema.TableType)FPlan.DataType).Columns[LString].Copy());
			LRow = new Row(FPlan.FProcess, LType);
			LRow.ValuesOwned = false;
			LRow.AsPhysical = LKey.Body.Data;
			return LRow;
		}
		
        public bool FindKey(Row AKey)
        {
			RemoteRow LKey = new RemoteRow();
			FPlan.FProcess.EnsureOverflowConsistent(AKey);
			LKey.Header = new RemoteRowHeader();
			LKey.Header.Columns = new string[AKey.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
				LKey.Header.Columns[LIndex] = AKey.DataType.Columns[LIndex].Name;
			LKey.Body = new RemoteRowBody();
			LKey.Body.Data = AKey.AsPhysical;
			RemoteGotoData LGotoData = FCursor.FindKey(LKey, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LGotoData.Flags);
			FPlan.FStatisticsCached = false;
			if (LGotoData.Success && BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			return LGotoData.Success;
		}
		
        public void FindNearest(Row AKey)
        {
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			FPlan.FProcess.EnsureOverflowConsistent(AKey);
			RemoteRow LKey = new RemoteRow();
			LKey.Header = new RemoteRowHeader();
			LKey.Header.Columns = new string[AKey.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
				LKey.Header.Columns[LIndex] = AKey.DataType.Columns[LIndex].Name;
			LKey.Body = new RemoteRowBody();
			LKey.Body.Data = AKey.AsPhysical;
			SetFlags(FCursor.FindNearest(LKey, FPlan.FProcess.GetProcessCallInfo()));
			FPlan.FStatisticsCached = false;
		}
		
        public bool Refresh(Row ARow)
        {
			if (BufferActive())
				ClearBuffer();
			SetBufferDirection(BufferDirection.Backward);
			RemoteRow LRow = new RemoteRow();
			FPlan.FProcess.EnsureOverflowConsistent(ARow);
			LRow.Header = new RemoteRowHeader();
			LRow.Header.Columns = new string[ARow.DataType.Columns.Count];
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				LRow.Header.Columns[LIndex] = ARow.DataType.Columns[LIndex].Name;
			LRow.Body = new RemoteRowBody();
			LRow.Body.Data = ARow.AsPhysical;
			RemoteGotoData LGotoData = FCursor.Refresh(LRow, FPlan.FProcess.GetProcessCallInfo());
			SetFlags(LGotoData.Flags);
			FPlan.FStatisticsCached = false;
			return LGotoData.Success;
		}
		
        public int RowCount()
        {
			FPlan.FStatisticsCached = false;
			return FCursor.RowCount(FPlan.FProcess.GetProcessCallInfo());
		}
		
		public Schema.TableType DataType { get { return (Schema.TableType)FPlan.DataType; } }
		public Schema.TableVar TableVar { get { return FPlan.TableVar; } }
		public TableNode TableNode { get { return FPlan.TableNode; } }
		
		// Copies the values from source row to the given target row, without using stream referencing
		// ASourceRow and ATargetRow must be of equivalent row types.
		protected void MarshalRow(Row ASourceRow, Row ATargetRow)
		{
			for (int LIndex = 0; LIndex < ASourceRow.DataType.Columns.Count; LIndex++)
				if (ASourceRow.HasValue(LIndex))
				{
					if (ASourceRow.HasNonNativeValue(LIndex))
					{
						Scalar LScalar = new Scalar(ATargetRow.Process, (Schema.ScalarType)ASourceRow[LIndex].DataType);
						Stream LSourceStream = ASourceRow[LIndex].OpenStream();
						try
						{
							Stream LTargetStream = LScalar.OpenStream();
							try
							{
								StreamUtility.CopyStream(LSourceStream, LTargetStream);
							}
							finally
							{
								LTargetStream.Close();
							}
						}
						finally
						{
							LSourceStream.Close();
						}
						ATargetRow[LIndex] = LScalar;
					}
					else
						ATargetRow[LIndex] = ASourceRow[LIndex];
				}
				else
					ATargetRow.ClearValue(LIndex);
		}
		
        /// <summary>Requests the default values for a new row in the cursor.</summary>        
        /// <param name='ARow'>A <see cref="Row"/> to be filled in with default values.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="ARow"/>.</returns>
        public bool Default(Row ARow, string AColumnName)
        {
			if ((FInternalProcess != null) && TableVar.IsDefaultCallRemotable(AColumnName))
			{
				// create a new row based on FInternalProcess, and copy the data from 
				Row LRow = ARow;
				if (ARow.HasNonNativeValues())
				{
					LRow = new Row(FInternalProcess, ARow.DataType);
					MarshalRow(ARow, LRow);
				}

				FPlan.FProcess.FSession.FServer.AcquireCacheLock(FPlan.FProcess, LockMode.Shared);
				try
				{
					bool LChanged = TableNode.Default(FInternalProcess, null, LRow, null, AColumnName);
					if (LChanged && !Object.ReferenceEquals(LRow, ARow))
						MarshalRow(LRow, ARow);
					return LChanged;
				}
				finally
				{
					FPlan.FProcess.FSession.FServer.ReleaseCacheLock(FPlan.FProcess, LockMode.Shared);
				}
			}
			else
			{
				FPlan.FProcess.EnsureOverflowReleased(ARow);
				RemoteRowBody LBody = new RemoteRowBody();
				LBody.Data = ARow.AsPhysical;

				RemoteProposeData LProposeData = FCursor.Default(LBody, AColumnName, FPlan.FProcess.GetProcessCallInfo());
				FPlan.FStatisticsCached = false;

				if (LProposeData.Success)
				{
					ARow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server for the course of the default call.
					ARow.AsPhysical = LProposeData.Body.Data;
					ARow.ValuesOwned = true;
				}
				return LProposeData.Success;
			}
        }
        
        /// <summary>Requests the affect of a change to the given row.</summary>
        /// <param name='AOldRow'>A <see cref="Row"/> containing the original values for the row.</param>
        /// <param name='ANewRow'>A <see cref="Row"/> containing the changed values for the row.</param>
        /// <param name='AColumnName'>The name of the column which changed in <paramref name="ANewRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="ANewRow"/>.</returns>
        public bool Change(Row AOldRow, Row ANewRow, string AColumnName)
        {
			// if the table level change is remotable and the named column is remotable or no column is named and all columns are remotable
				// the change can be evaluated locally, otherwise a remote call is required
			if ((FInternalProcess != null) && TableVar.IsChangeCallRemotable(AColumnName))
			{
				Row LOldRow = AOldRow;
				if (AOldRow.HasNonNativeValues())
				{
					LOldRow = new Row(FInternalProcess, AOldRow.DataType);
					MarshalRow(AOldRow, LOldRow);
				}
				
				Row LNewRow = ANewRow;
				if (ANewRow.HasNonNativeValues())
				{
					LNewRow = new Row(FInternalProcess, ANewRow.DataType);
					MarshalRow(ANewRow, LNewRow);
				}

				FPlan.FProcess.FSession.FServer.AcquireCacheLock(FPlan.FProcess, LockMode.Shared);
				try
				{
					bool LChanged = TableNode.Change(FInternalProcess, LOldRow, LNewRow, null, AColumnName);
					if (LChanged && !Object.ReferenceEquals(LNewRow, ANewRow))
						MarshalRow(LNewRow, ANewRow);
					return LChanged;
				}
				finally
				{
					FPlan.FProcess.FSession.FServer.ReleaseCacheLock(FPlan.FProcess, LockMode.Shared);
				}
			}
			else
			{			
				FPlan.FProcess.EnsureOverflowReleased(AOldRow);
				RemoteRowBody LOldBody = new RemoteRowBody();
				LOldBody.Data = AOldRow.AsPhysical;
				
				FPlan.FProcess.EnsureOverflowReleased(ANewRow);
				RemoteRowBody LNewBody = new RemoteRowBody();
				LNewBody.Data = ANewRow.AsPhysical;

				RemoteProposeData LProposeData = FCursor.Change(LOldBody, LNewBody, AColumnName, FPlan.FProcess.GetProcessCallInfo());
				FPlan.FStatisticsCached = false;

				if (LProposeData.Success)
				{
					ANewRow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server during the change call
					ANewRow.AsPhysical = LProposeData.Body.Data;
					ANewRow.ValuesOwned = true;
				}

				return LProposeData.Success;
			}
        }
        
        /// <summary>Ensures that the given row is valid.</summary>
        /// <param name='AOldRow'>A <see cref="Row"/> containing the original values for the row.</param>
        /// <param name='ANewRow'>A <see cref="Row"/> containing the changed values for the row.</param>
        /// <param name='AColumnName'>The name of the column which changed in <paramref name="ANewRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any change was made to <paramref name="ANewRow"/>.</returns>
        public bool Validate(Row AOldRow, Row ANewRow, string AColumnName)
        {
			if ((FInternalProcess != null) && TableVar.IsValidateCallRemotable(AColumnName))
			{
				Row LOldRow = AOldRow;
				if ((AOldRow != null) && AOldRow.HasNonNativeValues())
				{
					LOldRow = new Row(FInternalProcess, AOldRow.DataType);
					MarshalRow(AOldRow, LOldRow);
				}
				
				Row LNewRow = ANewRow;
				if (ANewRow.HasNonNativeValues())
				{
					LNewRow = new Row(FInternalProcess, ANewRow.DataType);
					MarshalRow(ANewRow, LNewRow);
				}

				FPlan.FProcess.FSession.FServer.AcquireCacheLock(FPlan.FProcess, LockMode.Shared);
				try
				{
					bool LChanged = TableNode.Validate(FInternalProcess, LOldRow, LNewRow, null, AColumnName);
					if (LChanged && !Object.ReferenceEquals(ANewRow, LNewRow))
						MarshalRow(LNewRow, ANewRow);
					return LChanged;
				}
				finally
				{
					FPlan.FProcess.FSession.FServer.ReleaseCacheLock(FPlan.FProcess, LockMode.Shared);
				}
			}
			else
			{
				RemoteRowBody LOldBody = new RemoteRowBody();
				if (AOldRow != null)
				{
					FPlan.FProcess.EnsureOverflowReleased(AOldRow);
					LOldBody.Data = AOldRow.AsPhysical;
				}
				
				FPlan.FProcess.EnsureOverflowReleased(ANewRow);
				RemoteRowBody LNewBody = new RemoteRowBody();
				LNewBody.Data = ANewRow.AsPhysical;
				
				RemoteProposeData LProposeData = FCursor.Validate(LOldBody, LNewBody, AColumnName, FPlan.FProcess.GetProcessCallInfo());
				FPlan.FStatisticsCached = false;

				if (LProposeData.Success)
				{
					ANewRow.ValuesOwned = false; // do not clear the overflow streams because the row is effectively owned by the server during the validate call
					ANewRow.AsPhysical = LProposeData.Body.Data;
					ANewRow.ValuesOwned = true;
				}
				return LProposeData.Success;
			}
        }
    }
}

