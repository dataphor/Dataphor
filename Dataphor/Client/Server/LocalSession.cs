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
		public LocalSession(LocalServer AServer, SessionInfo ASessionInfo, IRemoteServerSession ASession) : base()
		{
			FServer = AServer;
			FSession = ASession;
			FSessionInfo = ASessionInfo;
			FSessionID = ASession.SessionID;
			FInternalSession = ((IServer)FServer.FInternalServer).Connect(new SessionInfo(Alphora.Dataphor.DAE.Server.Engine.CSystemUserID, String.Empty, DAE.Server.Engine.CSystemLibraryName, false));
			StartKeepAlive();
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				StopKeepAlive();
			}
			finally
			{
				try
				{
					if (FInternalSession != null)
					{
						((IServer)FServer.FInternalServer).Disconnect(FInternalSession);
						FInternalSession = null;
					}
				}
				finally
				{
					FSession = null;
					FServer = null;
					base.Dispose(ADisposing);
				}
			}
		}

		protected internal LocalServer FServer;
        /// <value>Returns the <see cref="IServer"/> instance for this session.</value>
        public IServer Server { get { return FServer; } }
        
		private int FSessionID;
		public int SessionID { get { return FSessionID; } } 
		
		protected internal IServerSession FInternalSession;

		protected IRemoteServerSession FSession;
		public IRemoteServerSession RemoteSession { get { return FSession; } }
		
		public IServerProcess StartProcess(ProcessInfo AProcessInfo)
		{
			int LProcessID;
			IRemoteServerProcess LProcess = FSession.StartProcess(AProcessInfo, out LProcessID);
			return new LocalProcess(this, AProcessInfo, LProcessID, LProcess);
		}
		
		public void StopProcess(IServerProcess AProcess)
		{
			IRemoteServerProcess LRemoteProcess = ((LocalProcess)AProcess).RemoteProcess;
			((LocalProcess)AProcess).Dispose();
			try
			{
				FSession.StopProcess(LRemoteProcess);
			}
			catch
			{
				// ignore exceptions here
			}
		}
		
        private SessionInfo FSessionInfo;
        /// <value>Returns the <see cref="SessionInfo"/> object for this session.</value>
        public SessionInfo SessionInfo {  get { return FSessionInfo; } }

		#region Keep Alive

		/*
			Strategy: The client sends a keep alive message to the server connection every couple 
			 minutes.  This allows for very simple keep alive logic on the server:  If the server 
			 has not received a message from the client in n minutes, disconnect the client.
			 See the comment in the server connection for more details.
		*/

		public const int CLocalKeepAliveInterval = 120;	// 2 Minutes

		/// <summary> Signal used to indicate that the keep-alive thread can terminate. </summary>
		private ManualResetEvent FKeepAliveSignal;
		private Object FKeepAliveReferenceLock = new Object();

		private void StartKeepAlive()
		{
			lock (FKeepAliveReferenceLock)
			{
				if (FKeepAliveSignal != null)
					Error.Fail("Keep alive started more than once");
				FKeepAliveSignal = new ManualResetEvent(false);
			}
			new Thread(new ThreadStart(KeepAlive)).Start();	// Don't use the thread pool... long running thread
		}

		private void KeepAlive()
		{
			try
			{
				bool LSignaled = false;
				while (!LSignaled)
				{
					// Wait for either a signal or a time-out
					LSignaled = FKeepAliveSignal.WaitOne(CLocalKeepAliveInterval * 1000);

					// If WaitOne ended due to timeout, send a keep-alive message (then wait again)
					if (!LSignaled)
						FServer.ServerConnection.Ping();
				}

				// The keep alive processing is complete.  Clean up...
				lock (FKeepAliveReferenceLock)
				{
					((IDisposable)FKeepAliveSignal).Dispose();
					FKeepAliveSignal = null;	// Free the reference 
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private void StopKeepAlive()
		{
			lock (FKeepAliveReferenceLock)
			{
				if (FKeepAliveSignal != null)
					FKeepAliveSignal.Set();
			}
		}
		
		#endregion
	}
}
