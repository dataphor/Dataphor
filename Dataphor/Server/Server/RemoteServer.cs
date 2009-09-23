/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
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
using System.Security.Principal;

using Alphora.Dataphor;
using Alphora.Dataphor.Logging;
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

namespace Alphora.Dataphor.DAE.Server
{
	public class RemoteServer : RemoteServerObject, IDisposable, IRemoteServer, ITrackingHandler
	{
        private static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof(RemoteServer));
		
		// constructor		
		internal RemoteServer(Engine AServer) : base()
		{
			FServer = AServer;
			FConnections = new RemoteServerConnections(false);
			InitializeCatalogCaches();
			TrackingServices.RegisterTrackingHandler(this);
		}
        
		public void Dispose()
		{
			Dispose(true);
		}
		
		private void AttachServer()
		{
			FServer.Disposed += new EventHandler(FServerDisposed);
		}

		private void FServerDisposed(object ASender, EventArgs AArgs)
		{
			DetachServer();
			FServer = null;
			Dispose();
		}
		
		private void DetachServer()
		{
			FServer.Disposed -= new EventHandler(FServerDisposed);
		}
        
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FConnections != null)
				{
					CloseConnections();
					FConnections.Dispose();
					FConnections = null;
				}
				
				if (FServer != null)
				{
					DetachServer();
					FServer.Dispose();
					FServer = null;
				}
			}
			finally
			{
				TrackingServices.UnregisterTrackingHandler(this);
				base.Dispose(ADisposing);
			}
		}
		
		// Server
		private Engine FServer;
		internal Engine Server { get { return FServer; } }

		// Catalog Caches
		private CatalogCaches FCatalogCaches;
		internal CatalogCaches CatalogCaches { get { return FCatalogCaches; } }
		
		// Name 
		public string Name
		{
			get { return FServer.Name; } 
			set { FServer.Name = value; }
		}
		
		public static bool IsRemotableExceptionClass(Exception AException)
		{
			return (AException is DAEException) || (AException.GetType() == typeof(DataphorException));
		}
		
		public static bool IsRemotableException(Exception AException)
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
		
		public static Exception EnsureRemotableException(Exception AException)
		{
			if (!IsRemotableException(AException))
			{
                SRFLogger.WriteLine(TraceLevel.Warning,"Exception is not remotable: {0}", AException);
                Exception LInnerException = null;
				if (AException.InnerException != null)
					LInnerException = EnsureRemotableException(AException.InnerException);
					
				AException = new DataphorException(AException, LInnerException);
			}
			
			return AException;
		}
		
		public static Exception WrapException(Exception AException)
		{			
			return EnsureRemotableException(AException);
		}
		
		private void EnsureCatalogCaches()
		{
			if (FCatalogCaches == null)
				InitializeCatalogCaches();
		}
		
		private void InitializeCatalogCaches()
		{
			if (FServer.State == ServerState.Started)
			{
				FCatalogCaches = new CatalogCaches();
				CatalogCaches.GatherDefaultCachedObjects(FServer.GetBaseCatalogObjects());
			}
		}

		// Start
		public void Start()
		{
			try
			{
				FServer.Start();
				InitializeCatalogCaches();
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}
		}

		// Stop
		public void Stop()
		{
			try
			{
				try
				{
					CloseConnections();
				}
				finally
				{
					FServer.Stop();
				}
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}
		}
		
		// State
		public ServerState State { get { return FServer.State; } }
		
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
		
		// Connections
		private RemoteServerConnections FConnections;
		internal RemoteServerConnections Connections { get { return FConnections; } }

		public void DisconnectedObject(object AObject)
		{
			// Check if this object is a connection that needs to be disposed and dispose it if so
			// This should not recurse because the RemotingServices.Disconnect call in the connection dispose
			//  should not notify TrackingHandlers if the object has already been disconnected, and if the 
			//  connection has already been disposed than it should no longer be associated with this server
			// BTR 5/12/2005 -> Use a thread pool to perform this call so that there is no chance of blocking
			// the lease manager.
			RemoteServerConnection LConnection = AObject as RemoteServerConnection;
			if ((LConnection != null) && (LConnection.Server == this))
				ThreadPool.QueueUserWorkItem(new WaitCallback(DisposeConnection), LConnection);
		}
		
		private void DisposeConnection(Object AStateInfo)
		{
			try
			{
				RemoteServerConnection LConnection = AStateInfo as RemoteServerConnection;
				
				if (LConnection != null)
				{
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
					
					LConnection.Dispose();
				}
			}
			catch (Exception E)
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
				SRFLogger.WriteLine(TraceLevel.Error, "Unhandled exception disposing connection: {0}", E.Message);
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

		public IRemoteServerConnection Establish(string AConnectionName, string AHostName)
		{
			BeginCall();
			try
			{
				EnsureCatalogCaches();
				RemoteServerConnection LConnection = new RemoteServerConnection(this, AConnectionName, AHostName);
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
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
        public void Relinquish(IRemoteServerConnection AConnection)
        {
			RemoteServerConnection LConnection = AConnection as RemoteServerConnection;
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
				throw WrapException(E);
			}
        }
        
        public RemoteServerConnection GetConnectionForSession(RemoteServerSession ASession)
        {
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
						SRFLogger.WriteLine(TraceLevel.Error, "Error occurred relinquishing connections: {0}", E.Message);
					}
				}
			}
		}
		
		// InstanceID
		public Guid InstanceID { get { return FServer.InstanceID; } }
		
		// CacheTimeStamp
		public long CacheTimeStamp { get { return FServer.CacheTimeStamp; } }
		
		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp { get { return FServer.PlanCacheTimeStamp; } }
		
		// DerivationTimeStamp
		public long DerivationTimeStamp { get { return FServer.DerivationTimeStamp; } }
	}
}
