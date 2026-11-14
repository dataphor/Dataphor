/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Device.Catalog;

namespace Alphora.Dataphor.DAE.Server
{
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
	
    public class LocalServer : LocalServerObject, IServer, IDisposable
    {
		/// <remarks>The buffer size to use when copying library files over the CLI.</remarks>
		public const int FileCopyBufferSize = 16384;
		
		public LocalServer(IRemoteServer server, bool clientSideLoggingEnabled, string hostName) : base()
		{
			_server = server;
			_hostName = hostName;
			_referenceCount = 1;
			_internalServer = new Engine();
			_internalServer.Name = Schema.Object.NameFromGuid(Guid.NewGuid());
			_internalServer.LoggingEnabled = clientSideLoggingEnabled;
			_internalServer.Start();
			_serverConnection = _server.Establish(_internalServer.Name, _hostName);
			_serverCacheTimeStamp = server.CacheTimeStamp;
			_clientCacheTimeStamp = 1;
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
					if (_internalServer != null)
					{
						_internalServer.Dispose();
						_internalServer = null;
					}
				}
				finally
				{
					if (_server != null)
					{
						try
						{
							if (_serverConnection != null)
								_server.Relinquish(_serverConnection);
						}
						finally
						{
							_serverConnection = null;
							_server = null;
						}
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		// Reference Counting
		protected int _referenceCount;

		/// <summary> Increments the reference counter. </summary>
		internal protected void AddReference()
		{
			_referenceCount++;
		}

		/// <summary> Decrements the reference counter, disposes when no references. </summary>
		internal protected void RemoveReference()
		{
			_referenceCount--;
			if (_referenceCount == 0)
				Dispose();
		}

		private string _serverName;
		public string Name 
		{ 
			get 
			{ 
				if (_serverName == null)
					_serverName = _server.Name;
				return _serverName;
			} 
		}
		
		protected IRemoteServer _server;
		public IRemoteServer RemoteServer { get	{ return _server; } }
		
		protected IRemoteServerConnection _serverConnection;
		public IRemoteServerConnection ServerConnection { get { return _serverConnection; } }

		// An internal server used to evaluate remote proposable calls
		protected internal Engine _internalServer;
		
		private string _hostName;
		
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
				
		protected internal long _serverCacheTimeStamp;
		protected internal long _clientCacheTimeStamp;

		#if USESPINLOCK		
		private int _cacheSyncRoot = 0;
		#else
		private object FCacheSyncRoot = new object();
		#endif
		
		private SignalPool _cacheSignalPool = new SignalPool();
		private Dictionary<long, ManualResetEvent> _cacheSignals = new Dictionary<long, ManualResetEvent>();

		protected internal void WaitForCacheTimeStamp(IServerProcess process, long clientCacheTimeStamp)
		{
			#if LOGCACHEEVENTS
			FInternalServer.LogMessage(LogEntryType.Information, String.Format("Thread {0} checking for cache time stamp {1}", Thread.CurrentThread.GetHashCode(), AClientCacheTimeStamp.ToString()));
			#endif
			try
			{
				ManualResetEvent signal = null;
				bool signalAdded = false;
				
				#if USESPINLOCK
				while (Interlocked.CompareExchange(ref _cacheSyncRoot, 1, 0) == 1)
					Thread.SpinWait(100);  // Prevents CPU starvation
				try
				#else
				lock (FCacheSyncRoot)
				#endif
				{
					if (_clientCacheTimeStamp == clientCacheTimeStamp)
						return;
						
					if (_clientCacheTimeStamp > clientCacheTimeStamp)
					{
						process.Execute(".System.UpdateTimeStamps();", null);
						throw new ServerException(ServerException.Codes.CacheSerializationError, clientCacheTimeStamp, _clientCacheTimeStamp);
					}
					
					signalAdded = !_cacheSignals.TryGetValue(clientCacheTimeStamp, out signal);
					if (signalAdded)
					{
						signal = _cacheSignalPool.Acquire();
						_cacheSignals.Add(clientCacheTimeStamp, signal);
					}

					//Error.AssertFail(FCacheSyncEvent.Reset(), "Internal error: CacheSyncEvent reset failed");
				}
				#if USESPINLOCK
				finally
				{
					Interlocked.Decrement(ref _cacheSyncRoot);
				}
				#endif

				#if LOGCACHEEVENTS
				FInternalServer.LogMessage(LogEntryType.Information, String.Format("Thread {0} waiting for cache time stamp {1}", Thread.CurrentThread.GetHashCode(), AClientCacheTimeStamp.ToString()));
				#endif
				
				try
				{
					if (!(signal.WaitOne(CacheSerializationTimeout)))
						throw new ServerException(ServerException.Codes.CacheSerializationTimeout);
				}
				finally
				{
					if (signalAdded)
					{
						#if USESPINLOCK
						while (Interlocked.CompareExchange(ref _cacheSyncRoot, 1, 0) == 1)
							Thread.SpinWait(100); // Prevents CPU starvation
						try
						#else
						lock (FCacheSyncRoot)
						#endif
						{
							_cacheSignals.Remove(clientCacheTimeStamp);
						}
						#if USESPINLOCK
						finally
						{
							Interlocked.Decrement(ref _cacheSyncRoot);
						}
						#endif
						
						_cacheSignalPool.Relinquish(signal);
					}
				}
			}
			catch (Exception E)
			{
				_internalServer.LogError(E);
				throw E;
			}
		}
		
		protected internal void SetCacheTimeStamp(IServerProcess process, long clientCacheTimeStamp)
		{
			#if LOGCACHEEVENTS
			FInternalServer.LogMessage(LogEntryType.Information, String.Format("Thread {0} updating cache time stamp to {1}", Thread.CurrentThread.GetHashCode(), AClientCacheTimeStamp.ToString()));
			#endif
			
			#if USESPINLOCK
			while (Interlocked.CompareExchange(ref _cacheSyncRoot, 1, 0) == 1)
				Thread.SpinWait(100);
			try
			#else
			lock (FCacheSyncRoot)
			#endif
			{
				_clientCacheTimeStamp = clientCacheTimeStamp;
				
				ManualResetEvent signal;
				if (_cacheSignals.TryGetValue(clientCacheTimeStamp, out signal))
					signal.Set();
			}
			#if USESPINLOCK
			finally
			{
				Interlocked.Decrement(ref _cacheSyncRoot);
			}
			#endif
		}
		
		protected ReaderWriterLock _cacheLock = new ReaderWriterLock();
		
		public const int CacheLockTimeout = 10000;
		public const int CacheSerializationTimeout = 30000; // Wait at most 30 seconds for a timestamp to be deserialized
		
		protected internal void AcquireCacheLock(IServerProcess process, LockMode mode)
		{
			try
			{
				if (mode == LockMode.Exclusive)
					_cacheLock.AcquireWriterLock(CacheLockTimeout);
				else
					_cacheLock.AcquireReaderLock(CacheLockTimeout);
			}
			catch
			{
				throw new ServerException(ServerException.Codes.CacheLockTimeout);
			}
		}
		
		protected internal void ReleaseCacheLock(IServerProcess process, LockMode mode)
		{
			if (mode == LockMode.Exclusive)
				_cacheLock.ReleaseWriterLock();
			else
				_cacheLock.ReleaseReaderLock();
		}

		public long CacheTimeStamp { get { return _server.CacheTimeStamp; } }
		
		public long DerivationTimeStamp { get { return _server.DerivationTimeStamp; } }
		
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
		
		public void EnsureCacheConsistent(long serverCacheTimeStamp)
		{
			if (_serverCacheTimeStamp < serverCacheTimeStamp)
			{
				DoCacheClearing();
				Schema.Catalog catalog = _internalServer.Catalog;
				var catalogDevice = _internalServer.CatalogDevice;
				_internalServer.ClearCatalog();
				foreach (Schema.RegisteredAssembly assembly in catalog.ClassLoader.Assemblies)
					if (assembly.Library.Name != Engine.SystemLibraryName)
						_internalServer.Catalog.ClassLoader.Assemblies.Add(assembly);
				foreach (Schema.RegisteredClass classValue in catalog.ClassLoader.Classes)
					if (!_internalServer.Catalog.ClassLoader.Classes.Contains(classValue))
						_internalServer.Catalog.ClassLoader.Classes.Add(classValue);
				foreach (ServerSession session in _internalServer.Sessions)
				{
					if (session.User.ID == _internalServer.SystemUser.ID)
						session.SetUser(_internalServer.SystemUser);
					if (session.User.ID == _internalServer.AdminUser.ID)
						session.SetUser(_internalServer.AdminUser);
					session.SessionObjects.Clear();
					foreach (ServerProcess process in session.Processes)
						process.DeviceDisconnect(catalogDevice);
				}
				_serverCacheTimeStamp = serverCacheTimeStamp;
				DoCacheCleared();
			}
		}
		
		private List<string> _filesCached = new List<string>();
		private List<string> _assembliesCached = new List<string>();
		
		#if SILVERLIGHT

		public static Assembly LoadAssembyFromRemote(LocalProcess process, string libraryName, string fileName)
		{
			using (Stream sourceStream = new RemoteStreamWrapper(process.RemoteProcess.GetFile(libraryName, fileName)))
			{
				var assembly = new System.Windows.AssemblyPart().Load(sourceStream);
				ReflectionUtility.RegisterAssembly(assembly);
				return assembly;
			}
		}
		
		public void ClassLoaderMissed(LocalProcess process, ClassLoader classLoader, ClassDefinition classDefinition)
		{
			AcquireCacheLock(process, LockMode.Exclusive);
			try
			{
				if (!classLoader.Classes.Contains(classDefinition.ClassName))
				{
					// The local process has attempted to create an object from an unknown class alias.
					// Use the remote server to attempt to download and install the necessary assemblies.

					// Retrieve the list of all files required to load the assemblies required to load the class.
					ServerFileInfo[] fileInfos = process.RemoteProcess.GetFileNames(classDefinition.ClassName, process.Session.SessionInfo.Environment);

					for (int index = 0; index < fileInfos.Length; index++)
						if (fileInfos[index].IsDotNetAssembly)
							LoadAndRegister(process, classLoader, fileInfos[index].LibraryName, fileInfos[index].FileName, fileInfos[index].ShouldRegister);
				}
			}
			finally
			{
				ReleaseCacheLock(process, LockMode.Exclusive);
			}
		}

		public void LoadAndRegister(LocalProcess process, ClassLoader classLoader, string libraryName, string fileName, bool shouldRegister)
		{
			try
			{
				if (!_filesCached.Contains(fileName))
				{
					Assembly assembly = LoadAssembyFromRemote(process, libraryName, fileName);
					if (shouldRegister && !classLoader.Assemblies.Contains(assembly.FullName))
						classLoader.RegisterAssembly(Catalog.LoadedLibraries[Engine.SystemLibraryName], assembly);
					_assembliesCached.Add(fileName);
					_filesCached.Add(fileName);
				}
			}
			catch (IOException E)
			{
				_internalServer.LogError(E);
			}
		}
		
		#else
		private string FLocalBinDirectory;
		private string LocalBinDirectory
		{
			get
			{
				if (FLocalBinDirectory == null)
				{
					FLocalBinDirectory = Path.Combine(Path.Combine(Alphora.Dataphor.Windows.PathUtility.GetBinDirectory(), "LocalBin"), _server.Name);
					Directory.CreateDirectory(FLocalBinDirectory);
				}
				return FLocalBinDirectory;
			}
		}
		
		private string GetLocalFileName(string ALibraryName, string AFileName, bool AIsDotNetAssembly)
		{
			return Path.Combine(((ALibraryName == Engine.SystemLibraryName) || !AIsDotNetAssembly) ? Alphora.Dataphor.Windows.PathUtility.GetBinDirectory() : LocalBinDirectory, Path.GetFileName(AFileName));
		}
		
		public string GetFile(LocalProcess AProcess, string ALibraryName, string AFileName, DateTime AFileDate, bool AIsDotNetAssembly, out bool AShouldLoad)
		{
			AShouldLoad = false;
			string LFullFileName = GetLocalFileName(ALibraryName, AFileName, AIsDotNetAssembly);

			if (!_filesCached.Contains(AFileName))
			{
				if (!File.Exists(LFullFileName) || (File.GetLastWriteTimeUtc(LFullFileName) < AFileDate))
				{
					#if LOGFILECACHEEVENTS
					if (!File.Exists(LFullFileName))
						_internalServer.LogMessage(String.Format(@"Downloading file ""{0}"" from server because it does not exist on the client.", LFullFileName));
					else
						_internalServer.LogMessage(String.Format(@"Downloading newer version of file ""{0}"" from server. Client write time: ""{1}"". Server write time: ""{2}"".", LFullFileName, File.GetLastWriteTimeUtc(LFullFileName), AFileDate.ToString()));
					#endif
					using (Stream LSourceStream = new RemoteStreamWrapper(AProcess.RemoteProcess.GetFile(ALibraryName, AFileName)))
					{
						Alphora.Dataphor.Windows.FileUtility.EnsureWriteable(LFullFileName);
						try
						{
							using (FileStream LTargetStream = File.Open(LFullFileName, FileMode.Create, FileAccess.Write, FileShare.None))
							{
								StreamUtility.CopyStreamWithBufferSize(LSourceStream, LTargetStream, FileCopyBufferSize);
							}
							
							File.SetLastWriteTimeUtc(LFullFileName, AFileDate);
						}
						catch (IOException E)
						{
							_internalServer.LogError(E);
						}
					}
					
					#if LOGFILECACHEEVENTS
					_internalServer.LogMessage("Download complete");
					#endif
				}
				
				_filesCached.Add(AFileName);

				// Indicate that the assembly should be loaded
				if (AIsDotNetAssembly)
					AShouldLoad = true;
			}
			
			return LFullFileName;
		}
		
		public void ClassLoaderMissed(LocalProcess AProcess, ClassLoader AClassLoader, ClassDefinition AClassDefinition)
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
					ServerFileInfo[] LFileInfos = AProcess.RemoteProcess.GetFileNames(AClassDefinition.ClassName, AProcess.Session.SessionInfo.Environment);

					string[] LFullFileNames = new string[LFileInfos.Length];
					for (int LIndex = 0; LIndex < LFileInfos.Length; LIndex++)
					{
						bool LShouldLoad;
						string LFullFileName = GetFile(AProcess, LFileInfos[LIndex].LibraryName, LFileInfos[LIndex].FileName, LFileInfos[LIndex].FileDate, LFileInfos[LIndex].IsDotNetAssembly, out LShouldLoad);

						// Register/Load the assembly if necessary
						if ((LFileInfos[LIndex].ShouldRegister || LShouldLoad) && !_assembliesCached.Contains(LFileInfos[LIndex].FileName))
						{
							Assembly LAssembly = Assembly.LoadFrom(LFullFileName);
							ReflectionUtility.RegisterAssembly(LAssembly);
							if (LFileInfos[LIndex].ShouldRegister && !AClassLoader.Assemblies.Contains(LAssembly.FullName))
								AClassLoader.RegisterAssembly(Catalog.LoadedLibraries[Engine.SystemLibraryName], LAssembly);
							_assembliesCached.Add(LFileInfos[LIndex].FileName);
						}
					}
				}
			}
			finally
			{
				ReleaseCacheLock(AProcess, LockMode.Exclusive);
			}
		}
		#endif

        /// <summary>Starts the server instance.  If it is already running, the call has no effect.</summary>
        public void Start()
        {
			_server.Start();
		}
		
        /// <summary>Stops the server instance.  If it is not running, the call has no effect.</summary>
        public void Stop()
        {
			_server.Stop();
        }

		public Schema.Catalog Catalog { get { return _internalServer.Catalog; } }
		
		/// <value>Returns the state of the server. </value>
		public ServerState State { get { return _server.State; } }
        
		/// <summary>
        ///     Connects to the server using the given parameters and returns an interface to the connection.
        ///     Will raise if the server is not running.
        /// </summary>
        /// <param name='sessionInfo'>A <see cref="SessionInfo"/> object describing session configuration information for the connection request.</param>
        /// <returns>An <see cref="IServerSession"/> interface to the open session.</returns>
        public IServerSession Connect(SessionInfo sessionInfo)
        {
			if ((sessionInfo.HostName == null) || (sessionInfo.HostName == ""))
				sessionInfo.HostName = _hostName;
			sessionInfo.CatalogCacheName = _internalServer.Name;
			IRemoteServerSession session = _serverConnection.Connect(sessionInfo);
			return new LocalSession(this, sessionInfo, session);
        }
        
        /// <summary>
        ///     Disconnects an active session.  If the server is not running, an exception will be raised.
        ///     If the given session is not a valid session for this server, an exception will be raised.
        /// </summary>
        public void Disconnect(IServerSession session)
        {
			try
			{
				_serverConnection.Disconnect(((LocalSession)session).RemoteSession);
			}
			catch
			{
				// do nothing on an exception here
			}
			((LocalSession)session).Dispose();
        }
    }
}

