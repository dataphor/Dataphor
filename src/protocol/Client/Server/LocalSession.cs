/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalSession : LocalServerChildObject, IServerSession
    {
		public LocalSession(LocalServer server, SessionInfo sessionInfo, IRemoteServerSession session) : base()
		{
			_server = server;
			_session = session;
			_sessionInfo = sessionInfo;
			_sessionID = session.SessionID;
			_internalSession = ((IServer)_server._internalServer).Connect(new SessionInfo(Engine.SystemUserID, String.Empty, Engine.SystemLibraryName, false));
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_internalSession != null)
				{
					((IServer)_server._internalServer).Disconnect(_internalSession);
					_internalSession = null;
				}
			}
			finally
			{
				_session = null;
				_server = null;
				base.Dispose(disposing);
			}
		}

		protected internal LocalServer _server;
        /// <value>Returns the <see cref="IServer"/> instance for this session.</value>
        public IServer Server { get { return _server; } }
        
		private int _sessionID;
		public int SessionID { get { return _sessionID; } } 
		
		protected internal IServerSession _internalSession;

		protected IRemoteServerSession _session;
		public IRemoteServerSession RemoteSession { get { return _session; } }
		
		public IServerProcess StartProcess(ProcessInfo processInfo)
		{
			int processID;
			IRemoteServerProcess process = _session.StartProcess(processInfo, out processID);
			return new LocalProcess(this, processInfo, processID, process);
		}
		
		public void StopProcess(IServerProcess process)
		{
			IRemoteServerProcess remoteProcess = ((LocalProcess)process).RemoteProcess;
			((LocalProcess)process).Dispose();
			try
			{
				_session.StopProcess(remoteProcess);
			}
			catch
			{
				// ignore exceptions here
			}
		}
		
        private SessionInfo _sessionInfo;
        /// <value>Returns the <see cref="SessionInfo"/> object for this session.</value>
        public SessionInfo SessionInfo {  get { return _sessionInfo; } }

		// The keep-alive ping is LocalServer's: the server measures the connection, not the session. See the
		// keep-alive region of LocalServer.
	}
}
