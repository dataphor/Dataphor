/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Server
{
	// FrontendServer class
	// manages frontend sessions
	public class FrontendServer : System.Object
	{
		private static Hashtable FServers = new Hashtable();
		
		// returns the frontend server for the given DAE instance
		public static FrontendServer GetFrontendServer(DAE.Server.Server AServer)
		{
			lock (FServers)
			{
				FrontendServer LServer = FServers[AServer] as FrontendServer;
				if (LServer == null)
				{
					LServer = new FrontendServer(AServer);
					FServers.Add(AServer, LServer);
				}
				return LServer;
			}
		}

		public FrontendServer(DAE.Server.Server AServer)
		{
			FServer = AServer;
			FServer.Disposed += new EventHandler(ServerDisposed);
		}
		
		[Reference]
		private DAE.Server.Server FServer;
		public DAE.Server.Server Server { get { return FServer; } }
		
		private void ServerDisposed(object ASender, EventArgs AArgs)
		{
			if (FServer != null)
			{
				lock (FServers)
				{
					FServer.Disposed -= new EventHandler(ServerDisposed);
					FServers.Remove(FServer);
					FServer = null;
				}
			}
		}
		
		internal Hashtable FSessions = new Hashtable();
		
		public FrontendSession GetFrontendSession(ServerSession ASession)
		{
			lock (FSessions)
			{
				FrontendSession LSession = FSessions[ASession] as FrontendSession;
				if (LSession == null)
				{
					LSession = new FrontendSession(this, ASession);
					FSessions.Add(ASession, LSession);
				}
				return LSession;
			}
		}
		
		public void ClearDerivationCache()
		{
			lock (FSessions)
			{
				foreach (FrontendSession LSession in FSessions.Values)
					LSession.ClearDerivationCache();
			}
		}
	}
	
	// FrontendSession class
	// manages derivation cache and other session specific settings
	public class FrontendSession : System.Object
	{
		public const int CDefaultDerivationCacheSize = 150;
		
		public FrontendSession(FrontendServer AFrontendServer, ServerSession ASession)
		{
			FFrontendServer = AFrontendServer;
			FSession = ASession;
			FSession.Disposed += new EventHandler(SessionDisposed);
		}

		[Reference]
		private FrontendServer FFrontendServer;		
		public FrontendServer FrontendServer { get { return FFrontendServer; } }
		
		[Reference]
		private ServerSession FSession;
		public ServerSession Session { get { return FSession; } }
		
		private void SessionDisposed(object ASender, EventArgs AArgs)
		{
			if (FSession != null)
			{
				FSession.Disposed -= new EventHandler(SessionDisposed);
				FFrontendServer.FSessions.Remove(FSession);
				FSession = null;
				FFrontendServer = null;
			}
		}
		
		private bool FUseDerivationCache = true;
		public bool UseDerivationCache
		{
			get { return FUseDerivationCache; }
			set { FUseDerivationCache = value; }
		}
		
		private FixedSizeCache FDerivationCache = new FixedSizeCache(CDefaultDerivationCacheSize);
		public FixedSizeCache DerivationCache { get { return FDerivationCache; } }
		
		private long FDerivationTimeStamp;
		
		public void EnsureDerivationCacheConsistent()
		{
			lock (FDerivationCache)
			{
				if (FDerivationTimeStamp < FFrontendServer.Server.DerivationTimeStamp)
				{
					FDerivationCache.Clear();
					FDerivationTimeStamp = FFrontendServer.Server.DerivationTimeStamp;
				}
			}
		}
		
		public void ClearDerivationCache()
		{
			lock (FDerivationCache)
			{
				FDerivationCache.Clear();
			}
		}
	}
}

