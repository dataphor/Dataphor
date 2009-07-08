/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;

namespace Alphora.Dataphor.DAE.Server
{
	// ServerConnection
	public class ServerConnection : ServerChildObject, IRemoteServerConnection
	{
		internal ServerConnection(Server AServer, string AConnectionName, string AHostName)
		{
			FServer = AServer;
			FHostName = AHostName;
			FConnectionName = AConnectionName;
			FSessions = new ServerSessions(false);

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FServer.FConnectionCounter != null)
				FServer.FConnectionCounter.Increment();
			#endif
		}
		
		protected bool FDisposed;
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					FSessions.Dispose();
					FSessions = null;
				}
				finally
				{
					FServer.CatalogCaches.RemoveCache(FConnectionName);
				}

				if (!FDisposed)
				{
					#if !DISABLE_PERFORMANCE_COUNTERS
					if (FServer.FConnectionCounter != null)
						FServer.FConnectionCounter.Decrement();
					#endif
				}
			}
			finally
			{
				FServer = null;
				FDisposed = true;
				base.Dispose(ADisposing);
			}
		}

		// Server
		private Server FServer;
		public Server Server { get { return FServer; } }
		
		// Sessions
		private ServerSessions FSessions;
		public ServerSessions Sessions { get { return FSessions; } }
		
		// ConnectionName
		private string FConnectionName;
		public string ConnectionName { get { return FConnectionName; } }
		
		// HostName
		private string FHostName;
		public string HostName { get { return FHostName; } }
		
		// Execution
		private object FSyncHandle = new System.Object();
		private void BeginCall()
		{
			Monitor.Enter(FSyncHandle);
		}
		
		private void EndCall()
		{
			Monitor.Exit(FSyncHandle);
		}
		
		// IRemoteServer.Connect
		IRemoteServerSession IRemoteServerConnection.Connect(SessionInfo ASessionInfo)
		{
			BeginCall();
			try
			{
				ServerSession LSession = FServer.RemoteConnect(ASessionInfo);
				FSessions.Add(LSession);
				return LSession;
			}
			finally
			{
				EndCall();
			}
		}
        
		// IRemoteServer.Disconnect
		void IRemoteServerConnection.Disconnect(IRemoteServerSession ASession)
		{
			BeginCall();
			try
			{
				FServer.RemoteDisconnect((ServerSession)ASession);
			}
			finally
			{
				EndCall();
			}
		}
		
		internal void CloseSessions()
		{
			if (FSessions != null)
			{
				while (FSessions.Count > 0)
				{
					try
					{
						FServer.RemoteDisconnect((ServerSession)FSessions.DisownAt(0));
					}
					catch (Exception E)
					{
						if (FServer != null)
							FServer.LogError(E);
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

		public const int CInitialLeaseTimeSeconds = 300;	// 5 Minutes... must be longer than the client side ping
		public const int CRenewOnCallTimeSeconds = 300;

		public override object InitializeLifetimeService()
		{
			ILease LLease = (ILease)base.InitializeLifetimeService();
			if (LLease.CurrentState == LeaseState.Initial)
			{
				LLease.InitialLeaseTime = TimeSpan.FromSeconds(CInitialLeaseTimeSeconds);
				LLease.SponsorshipTimeout = TimeSpan.Zero;
				LLease.RenewOnCallTime = TimeSpan.FromSeconds(CRenewOnCallTimeSeconds);
			}
			return LLease;
		}

		/// <remarks> Provides a way to check that the session is still available (the message will reset the server's keep alive timeout). </remarks>
		public void Ping()
		{
			// intentionally left blank
		}

		#endregion
	}
	
	// ServerConnections
	public class ServerConnections : ServerChildObjects
	{		
		public ServerConnections() : base() {}
		public ServerConnections(bool AIsOwner) : base(AIsOwner) {}
		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerConnection))
				throw new ServerException(ServerException.Codes.ServerConnectionContainer);
		}
		
		public new ServerConnection this[int AIndex]
		{
			get { return (ServerConnection)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
	}
}
