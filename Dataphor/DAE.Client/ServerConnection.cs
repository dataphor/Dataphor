/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;

#if !SILVERLIGHT
using Alphora.Dataphor.DAE.Service;
#endif

namespace Alphora.Dataphor.DAE.Client
{
	/// <summary>Manages the connection to a server based on a given server alias definition.</summary>
	/// <remarks>The ServerConnection class manages the differences between in-process and out-of-process
	/// aliases, allowing clients to connect using only an alias name and a server connection. This
	/// class is used by the DataSession to establish a connection to a server based on an alias definition.
	/// </remarks>
	public class ServerConnection : Disposable
	{
		public ServerConnection(ServerAlias AServerAlias) : this(AServerAlias, true) {}
		
		public ServerConnection(ServerAlias AServerAlias, bool AAutoStart)
		{
			if (AServerAlias == null)
				throw new ClientException(ClientException.Codes.ServerAliasRequired);
				
			FServerAlias = AServerAlias;
			InProcessAlias LInProcessAlias = FServerAlias as InProcessAlias;
			ConnectionAlias LConnectionAlias = FServerAlias as ConnectionAlias;
			try
			{
				if (LInProcessAlias != null)
				{
					#if !SILVERLIGHT
					if (LInProcessAlias.IsEmbedded)
					{
						ServerConfiguration LConfiguration = InstanceManager.GetInstance(LInProcessAlias.InstanceName);
						FHostedServer = new Server.Server();
						LConfiguration.ApplyTo(FHostedServer);
						if (AAutoStart)
							FHostedServer.Start();
					}
					else
					{
						FServiceHost = new DataphorServiceHost();
						FServiceHost.InstanceName = LInProcessAlias.InstanceName;
						if (AAutoStart)
							FServiceHost.Start();
					}
					#else
					throw new NotSupportedException("In-process aliases are not supported in Silverlight");
					#endif
				}
				else
				{
					FClientServer = new ClientServer(LConnectionAlias.HostName, LConnectionAlias.OverridePortNumber, LConnectionAlias.InstanceName);
					FLocalServer = new LocalServer(FClientServer, LConnectionAlias.ClientSideLoggingEnabled, TerminalServiceUtility.ClientName);
				}
			}
			catch
			{
				CleanUp();
				throw;
			}
		}

		protected override void Dispose(bool ADisposed)
		{
			base.Dispose(ADisposed);
			CleanUp();
		}

		private void CleanUp()
		{
			#if !SILVERLIGHT
			if (FHostedServer != null)
			{
				FHostedServer.Stop();
				FHostedServer = null;
			}
			
			if (FServiceHost != null)
			{
				FServiceHost.Stop();
				FServiceHost = null;
			}
			#endif
			
			if (FLocalServer != null)
			{
				FLocalServer.Dispose();
				FLocalServer = null;
			}
			
			if (FClientServer != null)
			{
				FClientServer.Close();
				FClientServer = null;
			}
		}

		private ServerAlias FServerAlias;
		/// <summary>The alias used to establish this connection.</summary>
		public ServerAlias Alias { get { return FServerAlias; } }
		
		#if !SILVERLIGHT
		private DataphorServiceHost FServiceHost; // Used for non-embedded in-process server
		private Server.Server FHostedServer; // Used for embedded in-process server
		#endif
		private ClientServer FClientServer; // Used for out-of-process server
		private LocalServer FLocalServer; // Used for out-of-process server

		/// <summary>The IServer interface for the server connection.</summary>
		#if SILVERLIGHT
		public IServer Server { get { return FLocalServer; } }
		#else
		public IServer Server { get { return FHostedServer != null ? FHostedServer : (FServiceHost != null ? FServiceHost.Server : FLocalServer); } }
		#endif

		public override string ToString()
		{
			return FServerAlias.ToString();
		}
	}
}