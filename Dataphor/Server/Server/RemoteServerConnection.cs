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
using Alphora.Dataphor.Logging;
using System.Diagnostics;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerConnection
	public class RemoteServerConnection : RemoteServerChildObject, IRemoteServerConnection
	{
        private static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof(RemoteServer));
		
		internal RemoteServerConnection(RemoteServer AServer, string AConnectionName, string AHostName)
		{
			FServer = AServer;
			FHostName = AHostName;
			FConnectionName = AConnectionName;
			FSessions = new RemoteServerSessions(false);

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
		private RemoteServer FServer;
		public RemoteServer Server { get { return FServer; } }
		
		// Sessions
		private RemoteServerSessions FSessions;
		public RemoteServerSessions Sessions { get { return FSessions; } }
		
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
		public IRemoteServerSession Connect(SessionInfo ASessionInfo)
		{
			BeginCall();
			try
			{
				RemoteServerSession LSession = new RemoteServerSession(FServer, (ServerSession)FServer.Server.Connect(ASessionInfo));
				if (ASessionInfo.CatalogCacheName != String.Empty)
					FServer.CatalogCaches.AddSession(LSession);
				FSessions.Add(LSession);
				return LSession;
			}
			finally
			{
				EndCall();
			}
		}
        
		// IRemoteServer.Disconnect
		public void Disconnect(IRemoteServerSession ASession)
		{
			BeginCall();
			try
			{
				RemoteServerSession LSession = ASession as RemoteServerSession;
				if (ASession != null)
					LSession.Dispose();
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
					RemoteServerSession LSession = (RemoteServerSession)FSessions.DisownAt(0);
					try
					{
						LSession.Dispose();
					}
					catch (Exception E)
					{
						SRFLogger.WriteLine(TraceLevel.Error, "Error occurred closing connection sessions: {0}", E.Message);
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
	
	// RemoteServerConnections
	public class RemoteServerConnections : RemoteServerChildObjects
	{		
		public RemoteServerConnections() : base() {}
		public RemoteServerConnections(bool AIsOwner) : base(AIsOwner) {}
		
		protected override void Validate(RemoteServerChildObject AObject)
		{
			if (!(AObject is RemoteServerConnection))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerConnection");
		}
		
		public new RemoteServerConnection this[int AIndex]
		{
			get { return (RemoteServerConnection)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
	}
}
