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
			// Stamped here so that a connection nothing has pinged yet is never already idle.
			_lastActivityTicks = Environment.TickCount64;
		}
		
		protected bool _disposed;
		/// <summary>Serializes the two things that tear this connection down: see <see cref="Dispose(bool)"/>.</summary>
		private readonly object _teardownSyncHandle = new object();

		/// <summary>Releases the connection's sessions and its catalog cache; a second call does nothing.</summary>
		/// <remarks>
		/// The guard is what makes relinquishing a connection twice safe, and two things do it concurrently: the server's
		/// idle watcher (<see cref="Alphora.Dataphor.DAE.Service.ConnectionManager"/>) and the client's own CloseConnection,
		/// and the watcher and RemoteServer.CloseConnections at shutdown. Without it the second call dereferenced the
		/// fields the first had nulled and raised a NullReferenceException, and it re-raised <see cref="Disposed"/>.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Claimed under a lock, and _disposed set here rather than in the finally below: the two callers above arrive on
			// different threads, so a guard they could both pass would let the second dereference the fields the first had
			// nulled - the defect the guard exists to prevent, one thread later. The finally still runs on whichever thread
			// claimed the teardown, so Disposed is raised, and the service's handle withdrawn, even if a step of it throws.
			lock (_teardownSyncHandle)
			{
				if (_disposed)
					return;
				_disposed = true;
			}

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
		
		/// <summary>Disposes every session on the connection; a connection whose sessions are already closed is left alone.</summary>
		/// <remarks>
		/// Under the same lock as <see cref="Dispose(bool)"/>, and for the same reason: Relinquish calls this before it
		/// disposes, and two Relinquish calls on one connection arrive on different threads. Unserialized, both read a
		/// count of one and both took session nought, and the loser of that race removed an item the list no longer held.
		/// The fields are read into locals because the thread that disposes nulls them.
		/// </remarks>
		internal void CloseSessions()
		{
			lock (_teardownSyncHandle)
			{
				RemoteServerSessions sessions = _sessions;
				RemoteServer server = _server;
				if (sessions != null)
				{
					while (sessions.Count > 0)
					{
						RemoteServerSession session = (RemoteServerSession)sessions.DisownAt(0);
						try
						{
							session.Dispose();
						}
						catch (Exception E)
						{
							if ((server != null) && (server.Server != null))
								server.Server.LogError(E);
						}
					}
				}
			}
		}
        
		#region Lifetime Management

		/// <summary>When this connection was last pinged, in milliseconds since the machine started.</summary>
		/// <remarks>
		/// <c>Environment.TickCount64</c> and not <c>DateTime.Now</c>, because the only thing anyone asks of this field is
		/// how long ago it was stamped, and the difference between two readings of a wall clock is not the time that
		/// passed: it is that plus however much the clock itself moved in between, which a daylight-saving change, an
		/// operator correcting the time and a large time-synchronization step all do. A watcher subtracting wall-clock
		/// readings finds every live connection an hour idle in the first sweep after such a step and relinquishes all of
		/// them at once. The native CLI's session clock
		/// (<see cref="Alphora.Dataphor.DAE.NativeCLI.NativeSession"/>) is the same count for the same reason, and its
		/// remarks carry the rest of the argument - including why a signed 64-bit millisecond count needs no wrap guard.
		/// </remarks>
		private long _lastActivityTicks;

		/// <summary>How long since this connection was last pinged, or since it was established if it never has been.</summary>
		/// <remarks>
		/// A time since the last <em>ping</em>, not since the last call: no ordinary operation moves the clock, so the
		/// client's keep-alive is the only thing that does, and the server's idle watcher
		/// (<see cref="Alphora.Dataphor.DAE.Service.ConnectionManager"/>) is the only thing that reads it. Nothing else
		/// <em>can</em> move it without new plumbing - the service's handle table is flat, and a RemoteServerSession holds
		/// a reference to the RemoteServer rather than to the connection it belongs to, so there is no path from a handle
		/// back to the connection to stamp.
		/// </remarks>
		public TimeSpan IdleTime { get { return TimeSpan.FromMilliseconds(Environment.TickCount64 - Volatile.Read(ref _lastActivityTicks)); } }

		/// <remarks> Provides a way to check that the session is still available (the message will reset the server's keep alive timeout). </remarks>
		public void Ping()
		{
			// Volatile because ping threads write this field and the idle watcher's thread reads it, with nothing
			// synchronizing the two. On the 64-bit runtime this product runs on an aligned 8-byte access is already atomic,
			// so this is ordering and intent rather than a fix for an observed defect. Deliberately not the connection's
			// _syncHandle: BeginCall holds that for the whole of Connect and Disconnect, so a sweep that took it would
			// serialize itself against clients' in-flight calls.
			Volatile.Write(ref _lastActivityTicks, Environment.TickCount64);
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
