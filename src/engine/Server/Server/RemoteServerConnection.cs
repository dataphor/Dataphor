/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerConnection
	public class RemoteServerConnection : RemoteServerChildObject, IRemoteServerConnection
	{
		internal RemoteServerConnection(RemoteServer server, string connectionName, string hostName)
		{
			_server = server;
			_hostName = hostName;
			_connectionName = connectionName;
			_sessions = new RemoteServerSessions(false);
			_lastActivityTime = DateTime.Now;
		}
		
		protected bool _disposed;
		/// <summary>Releases the connection's sessions and its catalog cache; a second call does nothing.</summary>
		/// <remarks>
		/// The guard is what makes relinquishing a connection twice safe, and two things do it concurrently: the server's
		/// idle watcher (<see cref="Alphora.Dataphor.DAE.Service.ConnectionManager"/>) and the client's own CloseConnection,
		/// and the watcher and RemoteServer.CloseConnections at shutdown. Without it the second call dereferenced the
		/// fields the first had nulled and raised a NullReferenceException, and it re-raised <see cref="Disposed"/>.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			try
			{
				try
				{
					_sessions.Dispose();
					_sessions = null;
				}
				finally
				{
					_server.CatalogCaches.RemoveCache(_connectionName);
				}
			}
			finally
			{
				_server = null;
				_disposed = true;
				base.Dispose(disposing);
			}
		}

		// Server
		private RemoteServer _server;
		public RemoteServer Server { get { return _server; } }
		
		// Sessions
		private RemoteServerSessions _sessions;
		public RemoteServerSessions Sessions { get { return _sessions; } }
		
		// ConnectionName
		private string _connectionName;
		public string ConnectionName { get { return _connectionName; } }
		
		// HostName
		private string _hostName;
		public string HostName { get { return _hostName; } }
		
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
		
		// IRemoteServer.Connect
		public IRemoteServerSession Connect(SessionInfo sessionInfo)
		{
			BeginCall();
			try
			{
				RemoteServerSession session = new RemoteServerSession(_server, (ServerSession)_server.Server.Connect(sessionInfo));
				if (sessionInfo.CatalogCacheName != String.Empty)
					_server.CatalogCaches.AddSession(session);
				_sessions.Add(session);
				return session;
			}
			finally
			{
				EndCall();
			}
		}
        
		// IRemoteServer.Disconnect
		public void Disconnect(IRemoteServerSession session)
		{
			BeginCall();
			try
			{
				// Tested on the cast result, not on the parameter: the test was on session, so a session object from
				// somewhere else passed it and then raised a NullReferenceException on localSession. Same defect, and the
				// same typed error for it, as RemoteServer.Relinquish. A null session stays a no-op, as it was.
				if (session == null)
					return;

				RemoteServerSession localSession = session as RemoteServerSession;
				if (localSession == null)
					throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerSession");

				localSession.Dispose();
			}
			finally
			{
				EndCall();
			}
		}
		
		internal void CloseSessions()
		{
			if (_sessions != null)
			{
				while (_sessions.Count > 0)
				{
					RemoteServerSession session = (RemoteServerSession)_sessions.DisownAt(0);
					try
					{
						session.Dispose();
					}
					catch (Exception E)
					{
						_server.Server.LogError(E);
					}
				}
			}
		}
        
		#region Lifetime Management

		private DateTime _lastActivityTime;
		/// <summary>When this connection was last pinged, or established if it never has been.</summary>
		/// <remarks>
		/// A last-<em>ping</em> time, not a last-call time: no ordinary operation moves it, so the client's keep-alive is
		/// the only thing that does, and the server's idle watcher
		/// (<see cref="Alphora.Dataphor.DAE.Service.ConnectionManager"/>) is the only thing that reads it. Nothing else
		/// <em>can</em> move it without new plumbing - the service's handle table is flat, and a RemoteServerSession holds
		/// a reference to the RemoteServer rather than to the connection it belongs to, so there is no path from a handle
		/// back to the connection to stamp.
		/// </remarks>
		public DateTime LastActivityTime { get { return _lastActivityTime; } }

		/// <remarks> Provides a way to check that the session is still available (the message will reset the server's keep alive timeout). </remarks>
		public void Ping()
		{
			_lastActivityTime = DateTime.Now;
		}

		#endregion
	}
	
	// RemoteServerConnections
	public class RemoteServerConnections : RemoteServerChildObjects
	{		
		public RemoteServerConnections() : base() {}
		public RemoteServerConnections(bool isOwner) : base(isOwner) {}
		
		protected override void Validate(RemoteServerChildObject objectValue)
		{
			if (!(objectValue is RemoteServerConnection))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerConnection");
		}
		
		public new RemoteServerConnection this[int index]
		{
			get { return (RemoteServerConnection)base[index]; } 
			set { base[index] = value; } 
		}
	}
}
