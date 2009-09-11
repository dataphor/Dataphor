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

namespace Alphora.Dataphor.DAE.Server
{
	public class RemoteServerSession : RemoteServerChildObject, IRemoteServerSession
	{		
		internal RemoteServerSession
		(
			RemoteServer AServer, 
			ServerSession AServerSession
		) : base()
		{
			FServer = AServer;
			FServerSession = AServerSession;
			AttachServerSession();
			FCatalogCacheName = SessionInfo == null ? null : SessionInfo.CatalogCacheName;
		}
		
		private void AttachServerSession()
		{
			FServerSession.Disposed += new EventHandler(FServerSessionDisposed);
		}
		
		private void DetachServerSession()
		{
			FServerSession.Disposed -= new EventHandler(FServerSessionDisposed);
		}

		private void FServerSessionDisposed(object ASender, EventArgs AArgs)
		{
			DetachServerSession();
			FServerSession = null;
			Dispose();
		}
		
		private string FCatalogCacheName;
		public string CatalogCacheName { get { return FCatalogCacheName; } }
		
		// Dispose
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (!String.IsNullOrEmpty(CatalogCacheName) && (FServer != null) && (FServer.CatalogCaches != null))
				{
					FServer.CatalogCaches.RemoveSession(this);
					FCatalogCacheName = null;
				}

				if (FServerSession != null)
				{
					DetachServerSession();
					FServerSession.Dispose();
					FServerSession = null;
				}
				
				FServer = null;
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
        
		// Server
		private RemoteServer FServer;
		public RemoteServer Server { get { return FServer; } }
		
		IRemoteServer IRemoteServerSession.Server { get { return FServer; } }
		
		private ServerSession FServerSession;
		internal ServerSession ServerSession { get { return FServerSession; } }
        
		// SessionID
		public int SessionID { get { return FServerSession.SessionID; } }
		
		// SessionInfo
		public SessionInfo SessionInfo { get { return FServerSession.SessionInfo; } }
        
		// Execution
		internal Exception WrapException(Exception AException)
		{
			return RemoteServer.WrapException(AException);
		}

		// StartProcess
		public IRemoteServerProcess StartProcess(ProcessInfo AProcessInfo, out int AProcessID)
		{
			ServerProcess LServerProcess = (ServerProcess)FServerSession.StartProcess(AProcessInfo);
			AProcessID = LServerProcess.ProcessID;
			return new RemoteServerProcess(this, LServerProcess);
		}
		
		// StopProcess
		public void StopProcess(IRemoteServerProcess AProcess)
		{
			FServerSession.StopProcess(((RemoteServerProcess)AProcess).ServerProcess);
		}
	}
	
	// RemoteServerSessions
	public class RemoteServerSessions : RemoteServerChildObjects
	{		
		public RemoteServerSessions() : base() {}
		public RemoteServerSessions(bool AIsOwner) : base(AIsOwner) {}
		
		protected override void Validate(RemoteServerChildObject AObject)
		{
			if (!(AObject is RemoteServerSession))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerSession");
		}
		
		public new RemoteServerSession this[int AIndex]
		{
			get { return (RemoteServerSession)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
		
		public RemoteServerSession GetSession(int ASessionID)
		{
			foreach (RemoteServerSession LSession in this)
				if (LSession.SessionID == ASessionID)
					return LSession;
			throw new ServerException(ServerException.Codes.SessionNotFound, ASessionID);
		}
	}
}
