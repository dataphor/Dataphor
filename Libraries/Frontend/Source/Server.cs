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
using Alphora.Dataphor.Frontend.Server.Derivation;

namespace Alphora.Dataphor.Frontend.Server
{
	// FrontendServer class
	// manages frontend sessions
	public class FrontendServer : System.Object
	{
		private static Hashtable _servers = new Hashtable();
		
		// returns the frontend server for the given DAE instance
		public static FrontendServer GetFrontendServer(DAE.Server.Engine server)
		{
			lock (_servers)
			{
				FrontendServer localServer = _servers[server] as FrontendServer;
				if (localServer == null)
				{
					localServer = new FrontendServer(server);
					_servers.Add(server, localServer);
				}
				return localServer;
			}
		}

		public FrontendServer(DAE.Server.Engine server)
		{
			_server = server;
			_server.Disposed += new EventHandler(ServerDisposed);
		}
		
		[Reference]
		private DAE.Server.Engine _server;
		public DAE.Server.Engine Server { get { return _server; } }
		
		private void ServerDisposed(object sender, EventArgs args)
		{
			if (_server != null)
			{
				lock (_servers)
				{
					_server.Disposed -= new EventHandler(ServerDisposed);
					_servers.Remove(_server);
					_server = null;
				}
			}
		}
		
		internal Hashtable _sessions = new Hashtable();
		
		public FrontendSession GetFrontendSession(ServerSession session)
		{
			lock (_sessions)
			{
				FrontendSession localSession = _sessions[session] as FrontendSession;
				if (localSession == null)
				{
					localSession = new FrontendSession(this, session);
					_sessions.Add(session, localSession);
				}
				return localSession;
			}
		}
		
		public void ClearDerivationCache()
		{
			lock (_sessions)
			{
				foreach (FrontendSession session in _sessions.Values)
					session.ClearDerivationCache();
			}
		}
	}
	
	// FrontendSession class
	// manages derivation cache and other session specific settings
	public class FrontendSession : System.Object
	{
		public const int DefaultDerivationCacheSize = 50;
		
		public FrontendSession(FrontendServer frontendServer, ServerSession session)
		{
			_frontendServer = frontendServer;
			_session = session;
			_session.Disposed += new EventHandler(SessionDisposed);
		}

		[Reference]
		private FrontendServer _frontendServer;		
		public FrontendServer FrontendServer { get { return _frontendServer; } }
		
		[Reference]
		private ServerSession _session;
		public ServerSession Session { get { return _session; } }
		
		private void SessionDisposed(object sender, EventArgs args)
		{
			if (_session != null)
			{
				_session.Disposed -= new EventHandler(SessionDisposed);
				_frontendServer._sessions.Remove(_session);
				_session = null;
				_frontendServer = null;
			}
		}
		
		private bool _useDerivationCache = true;
		public bool UseDerivationCache
		{
			get { return _useDerivationCache; }
			set { _useDerivationCache = value; }
		}

		private FixedSizeCache<DerivationSeed, DerivationCacheItem> _derivationCache = new FixedSizeCache<DerivationSeed, DerivationCacheItem>(DefaultDerivationCacheSize);
		public FixedSizeCache<DerivationSeed, DerivationCacheItem> DerivationCache { get { return _derivationCache; } }
		
		private long _derivationTimeStamp;
		
		public void EnsureDerivationCacheConsistent()
		{
			lock (_derivationCache)
			{
				if (_derivationTimeStamp < _frontendServer.Server.DerivationTimeStamp)
				{
					_derivationCache.Clear();
					_derivationTimeStamp = _frontendServer.Server.DerivationTimeStamp;
				}
			}
		}
		
		public void ClearDerivationCache()
		{
			lock (_derivationCache)
			{
				_derivationCache.Clear();
			}
		}
	}
}

