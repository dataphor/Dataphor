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
		/// could not build one. protocol-corewcf-proof needs it from the test project, and protocol-corewcf-host needs it if
		/// hosting moves to an assembly of its own.
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
        
        public void Relinquish(IRemoteServerConnection connection)
        {
			RemoteServerConnection localConnection = connection as RemoteServerConnection;
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
