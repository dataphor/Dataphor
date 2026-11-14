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
			StartKeepAlive();
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				StopKeepAlive();
			}
			finally
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

		#region Keep Alive

		/*
			Strategy: The client sends a keep alive message to the server connection every couple 
			 minutes.  This allows for very simple keep alive logic on the server:  If the server 
			 has not received a message from the client in n minutes, disconnect the client.
			 See the comment in the server connection for more details.
		*/

		public const int LocalKeepAliveInterval = 120;	// 2 Minutes

		/// <summary> Signal used to indicate that the keep-alive thread can terminate. </summary>
		private ManualResetEvent _keepAliveSignal;
		private Object _keepAliveReferenceLock = new Object();

		private void StartKeepAlive()
		{
			lock (_keepAliveReferenceLock)
			{
				if (_keepAliveSignal != null)
					Error.Fail("Keep alive started more than once");
				_keepAliveSignal = new ManualResetEvent(false);
			}
			new Thread(new ThreadStart(KeepAlive)).Start();	// Don't use the thread pool... long running thread
		}

		private void KeepAlive()
		{
			try
			{
				bool signaled = false;
				while (!signaled)
				{
					// Wait for either a signal or a time-out
					signaled = _keepAliveSignal.WaitOne(LocalKeepAliveInterval * 1000);

					// If WaitOne ended due to timeout, send a keep-alive message (then wait again)
					if (!signaled)
						_server.ServerConnection.Ping();
				}

				// The keep alive processing is complete.  Clean up...
				lock (_keepAliveReferenceLock)
				{
					((IDisposable)_keepAliveSignal).Dispose();
					_keepAliveSignal = null;	// Free the reference 
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private void StopKeepAlive()
		{
			lock (_keepAliveReferenceLock)
			{
				if (_keepAliveSignal != null)
					_keepAliveSignal.Set();
			}
		}
		
		#endregion
	}
}
