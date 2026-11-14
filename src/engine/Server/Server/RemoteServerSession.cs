/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	public class RemoteServerSession : RemoteServerChildObject, IRemoteServerSession
	{		
		internal RemoteServerSession
		(
			RemoteServer server, 
			ServerSession serverSession
		) : base()
		{
			_server = server;
			_serverSession = serverSession;
			AttachServerSession();
			_catalogCacheName = SessionInfo == null ? null : SessionInfo.CatalogCacheName;
		}
		
		private void AttachServerSession()
		{
			_serverSession.Disposed += new EventHandler(FServerSessionDisposed);
		}
		
		private void DetachServerSession()
		{
			_serverSession.Disposed -= new EventHandler(FServerSessionDisposed);
		}

		private void FServerSessionDisposed(object sender, EventArgs args)
		{
			DetachServerSession();
			_serverSession = null;
			Dispose();
		}
		
		private string _catalogCacheName;
		public string CatalogCacheName { get { return _catalogCacheName; } }
		
		// Dispose
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_serverSession != null)
				{
					DetachServerSession();
					_serverSession.Dispose();
					_serverSession = null;
				}

                if (!String.IsNullOrEmpty(CatalogCacheName) && (_server != null) && (_server.CatalogCaches != null))
                {
                    _server.CatalogCaches.RemoveSession(this);
                    _catalogCacheName = null;
                }

				_server = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
        
		// Server
		private RemoteServer _server;
		public RemoteServer Server { get { return _server; } }
		
		IRemoteServer IRemoteServerSession.Server { get { return _server; } }
		
		private ServerSession _serverSession;
		internal ServerSession ServerSession { get { return _serverSession; } }
        
		// SessionID
		public int SessionID { get { return _serverSession.SessionID; } }
		
		// SessionInfo
		public SessionInfo SessionInfo { get { return _serverSession.SessionInfo; } }
        
		// Execution
		internal Exception WrapException(Exception exception)
		{
			return RemoteServer.WrapException(exception);
		}

		// StartProcess
		public IRemoteServerProcess StartProcess(ProcessInfo processInfo, out int processID)
		{
			ServerProcess serverProcess = (ServerProcess)_serverSession.StartProcess(processInfo);
			processID = serverProcess.ProcessID;
			return new RemoteServerProcess(this, serverProcess);
		}
		
		// StopProcess
		public void StopProcess(IRemoteServerProcess process)
		{
			_serverSession.StopProcess(((RemoteServerProcess)process).ServerProcess);
		}
	}
	
	// RemoteServerSessions
	public class RemoteServerSessions : RemoteServerChildObjects
	{		
		public RemoteServerSessions() : base() {}
		public RemoteServerSessions(bool isOwner) : base(isOwner) {}
		
		protected override void Validate(RemoteServerChildObject objectValue)
		{
			if (!(objectValue is RemoteServerSession))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerSession");
		}
		
		public new RemoteServerSession this[int index]
		{
			get { return (RemoteServerSession)base[index]; } 
			set { base[index] = value; } 
		}
		
		public RemoteServerSession GetSession(int sessionID)
		{
			foreach (RemoteServerSession session in this)
				if (session.SessionID == sessionID)
					return session;
			throw new ServerException(ServerException.Codes.SessionNotFound, sessionID);
		}
	}
}
