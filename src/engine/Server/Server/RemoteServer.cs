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
using System.Threading;
using System.Reflection;
using System.Resources;
using System.Security.Principal;

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
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	public class RemoteServer : RemoteServerObject, IDisposable, IRemoteServer
	{
		// constructor		
		/// <summary>Wraps a server in the remote (network-optimized) CLI the service contract is written against.</summary>
		/// <remarks>
		/// Public, not internal: DataphorService's only constructor takes a RemoteServer, so a host outside this assembly
		/// could not build one - and since protocol-corewcf-host the host is outside it, in
		/// Alphora.Dataphor.DAE.Server.Hosting, as is the protocol test.
		/// </remarks>
		public RemoteServer(Server server) : base()
		{
			_server = server;
			_connections = new RemoteServerConnections(false);
			InitializeCatalogCaches();
		}
        
		public void Dispose()
		{
			Dispose(true);
		}
		
		// NOTE: nothing calls AttachServer, so FServerDisposed never runs and a RemoteServer does not learn that its engine
		// has been disposed - it finds out only by being disposed itself, which DataphorServiceHost.Stop does in that order.
		// Kept rather than deleted because the hookup is the intent and DetachServer (called from Dispose) is written against
		// it; unsubscribing a handler that was never added is a no-op. Nothing reads Server expecting it to be nulled on the
		// engine's disposal: the one reader outside this class, the idle watcher's sweep, is guarded for null either way.
		// Revisit if a second thing starts depending on Server being null once the engine is gone - then wire the hookup in
		// RemoteServer's constructor rather than dropping the guards.
		private void AttachServer()
		{
			_server.Disposed += new EventHandler(FServerDisposed);
		}

		private void FServerDisposed(object sender, EventArgs args)
		{
			DetachServer();
			_server = null;
			Dispose();
		}
		
		private void DetachServer()
		{
			_server.Disposed -= new EventHandler(FServerDisposed);
		}
        
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_connections != null)
				{
					CloseConnections();
					_connections.Dispose();
					_connections = null;
				}
				
				if (_server != null)
				{
					DetachServer();
					_server = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		// Server
		private Server _server;
		internal Server Server { get { return _server; } }

		// Catalog Caches
		private CatalogCaches _catalogCaches;
		internal CatalogCaches CatalogCaches { get { return _catalogCaches; } }
		
		// Name 
		public string Name
		{
			get { return _server.Name; } 
			set { _server.Name = value; }
		}
		
		public static bool IsRemotableExceptionClass(Exception exception)
		{
			return (exception is DAEException) || (exception.GetType() == typeof(DataphorException));
		}
		
		public static bool IsRemotableException(Exception exception)
		{
			if (!IsRemotableExceptionClass(exception))
				return false;
				
			Exception localException = exception;
			while (localException != null)
			{
				if (!IsRemotableExceptionClass(localException))
					return false;
				localException = localException.InnerException;
			}
			return true;
		}
		
		public static Exception EnsureRemotableException(Exception exception)
		{
			if (!IsRemotableException(exception))
			{
                Exception innerException = null;
				if (exception.InnerException != null)
					innerException = EnsureRemotableException(exception.InnerException);
					
				exception = new DataphorException(exception, innerException);
			}
			
			return exception;
		}
		
		public static Exception WrapException(Exception exception)
		{			
			return EnsureRemotableException(exception);
		}
		
		private void EnsureCatalogCaches()
		{
			if (_catalogCaches == null)
				InitializeCatalogCaches();
		}
		
		private void InitializeCatalogCaches()
		{
			if (_server.State == ServerState.Started)
			{
				_catalogCaches = new CatalogCaches();
				CatalogCaches.GatherDefaultCachedObjects(_server.GetBaseCatalogObjects());
			}
		}

		// Start
		public void Start()
		{
			try
			{
				_server.Start();
				InitializeCatalogCaches();
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
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
					_server.Stop();
				}
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
		}
		
		// State
		public ServerState State { get { return _server.State; } }
		
		// Execution
		private object _syncHandle = new System.Object();
		private void BeginCall()
		{
			Monitor.Enter(_syncHandle);
		}
		
		private void EndCall()
		{
			Monitor.Exit(_syncHandle);
		}
		
		// Connections
		private RemoteServerConnections _connections;
		internal RemoteServerConnections Connections { get { return _connections; } }
		
		internal RemoteServerConnection[] GetCurrentConnections()
		{
			BeginCall();
			try
			{
				RemoteServerConnection[] result = new RemoteServerConnection[_connections == null ? 0 : _connections.Count];
				if (_connections != null)
					_connections.CopyTo(result, 0);
				return result;
			}
			finally
			{
				EndCall();
			}
		}

		public IRemoteServerConnection Establish(string connectionName, string hostName)
		{
			BeginCall();
			try
			{
				EnsureCatalogCaches();
				RemoteServerConnection connection = new RemoteServerConnection(this, connectionName, hostName);
				try
				{
					_connections.Add(connection);
					return connection;
				}
				catch
				{
					connection.Dispose();
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
        
		/// <summary>Closes a connection's sessions and releases it; a connection already released is left alone.</summary>
		/// <remarks>
		/// <para>
		/// Idempotent, because two things relinquish a connection and they can arrive together: the server's idle watcher
		/// (<see cref="Alphora.Dataphor.DAE.Service.ConnectionManager"/>) on one side, and the client's own CloseConnection
		/// or <see cref="CloseConnections"/> at shutdown on the other. Each step below tolerates having been taken already -
		/// the connection has no sessions left, the collection no longer holds it, and
		/// RemoteServerConnection.Dispose returns at its guard - so the second call does nothing.
		/// </para>
		/// <para>
		/// CloseSessions runs outside the server's lock, as it always has: closing a session can take as long as a process
		/// termination, and must not hold up <see cref="Establish"/> for every other client.
		/// </para>
		/// </remarks>
        public void Relinquish(IRemoteServerConnection connection)
        {
			// Checked rather than cast blind: the cast was with `as` and the result dereferenced unchecked, so a connection
			// object from somewhere else - a client-side decorator, say - raised a NullReferenceException here instead of an
			// error a caller can read. The same typed error a RemoteServerConnections container raises for the same mismatch.
			RemoteServerConnection localConnection = connection as RemoteServerConnection;
			if (localConnection == null)
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerConnection");

			localConnection.CloseSessions();

			BeginCall();
			try
			{
				_connections.SafeDisown(localConnection);
			}
			finally
			{
				EndCall();
			}
			try
			{
				localConnection.Dispose();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
        }
        
		private void CloseConnections()
		{
			if (_connections != null)
			{
				while (_connections.Count > 0)
				{
					try
					{
						Relinquish(_connections[0]);
					}
					catch (Exception E)
					{
						_server.LogError(E);
					}
				}
			}
		}
		
		// CacheTimeStamp
		public long CacheTimeStamp { get { return _server.CacheTimeStamp; } }
		
		// PlanCacheTimeStamp
		public long PlanCacheTimeStamp { get { return _server.PlanCacheTimeStamp; } }
		
		// DerivationTimeStamp
		public long DerivationTimeStamp { get { return _server.DerivationTimeStamp; } }
	}
}
