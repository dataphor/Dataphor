/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
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
		protected override void Dispose(bool disposing)
		{
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
				RemoteServerSession localSession = session as RemoteServerSession;
				if (session != null)
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

		/*
			Strategy: The client has a ping thread that will execute every couple minutes, so long as
			 the server's initial and renewal time is longer than the client's ping time, no sponsorhip
			 mechanism is necessary; the client ensures that the server recognizes that it is responsive.
		*/

		public const int InitialLeaseTimeSeconds = 300;	// 5 Minutes... must be longer than the client side ping
		public const int RenewOnCallTimeSeconds = 300;

		public override object InitializeLifetimeService()
		{
			ILease lease = (ILease)base.InitializeLifetimeService();
			if (lease.CurrentState == LeaseState.Initial)
			{
				lease.InitialLeaseTime = TimeSpan.FromSeconds(InitialLeaseTimeSeconds);
				lease.SponsorshipTimeout = TimeSpan.Zero;
				lease.RenewOnCallTime = TimeSpan.FromSeconds(RenewOnCallTimeSeconds);
			}
			return lease;
		}
		
		private DateTime _lastActivityTime;
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
