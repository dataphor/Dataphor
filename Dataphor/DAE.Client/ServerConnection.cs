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
		public ServerConnection(ServerAlias serverAlias) : this(serverAlias, true) {}
		
		public ServerConnection(ServerAlias serverAlias, bool autoStart)
		{
			if (serverAlias == null)
				throw new ClientException(ClientException.Codes.ServerAliasRequired);
				
			_serverAlias = serverAlias;
			InProcessAlias inProcessAlias = _serverAlias as InProcessAlias;
			ConnectionAlias connectionAlias = _serverAlias as ConnectionAlias;
			try
			{
				if (inProcessAlias != null)
				{
					#if !SILVERLIGHT
					if (inProcessAlias.IsEmbedded)
					{
						ServerConfiguration configuration = InstanceManager.GetInstance(inProcessAlias.InstanceName);
						_hostedServer = new Server.Server();
						configuration.ApplyTo(_hostedServer);
						if (autoStart)
							_hostedServer.Start();
					}
					else
					{
						_serviceHost = new DataphorServiceHost();
						_serviceHost.InstanceName = inProcessAlias.InstanceName;
						if (autoStart)
							_serviceHost.Start();
					}
					#else
					throw new NotSupportedException("In-process aliases are not supported in Silverlight");
					#endif
				}
				else
				{
					_clientServer = 
						String.IsNullOrEmpty(connectionAlias.ClientConfigurationName)
							? 
								new ClientServer
								(
									connectionAlias.HostName, 
									connectionAlias.InstanceName, 
									connectionAlias.OverridePortNumber, 
									connectionAlias.SecurityMode, 
									connectionAlias.OverrideListenerPortNumber, 
									connectionAlias.ListenerSecurityMode
								)
							:
								new ClientServer
								(
									connectionAlias.HostName, 
									connectionAlias.ClientConfigurationName
								);

					_localServer = new LocalServer(_clientServer, connectionAlias.ClientSideLoggingEnabled, TerminalServiceUtility.ClientName);
				}
			}
			catch
			{
				CleanUp();
				throw;
			}
		}

		protected override void Dispose(bool disposed)
		{
			base.Dispose(disposed);
			CleanUp();
		}

		private void CleanUp()
		{
			#if !SILVERLIGHT
			if (_hostedServer != null)
			{
				_hostedServer.Stop();
				_hostedServer = null;
			}
			
			if (_serviceHost != null)
			{
				_serviceHost.Stop();
				_serviceHost = null;
			}
			#endif
			
			if (_localServer != null)
			{
				_localServer.Dispose();
				_localServer = null;
			}
			
			if (_clientServer != null)
			{
				_clientServer.Close();
				_clientServer = null;
			}
		}

		private ServerAlias _serverAlias;
		/// <summary>The alias used to establish this connection.</summary>
		public ServerAlias Alias { get { return _serverAlias; } }
		
		#if !SILVERLIGHT
		private DataphorServiceHost _serviceHost; // Used for non-embedded in-process server
		private Server.Server _hostedServer; // Used for embedded in-process server
		#endif
		private ClientServer _clientServer; // Used for out-of-process server
		private LocalServer _localServer; // Used for out-of-process server

		/// <summary>The IServer interface for the server connection.</summary>
		#if SILVERLIGHT
		public IServer Server { get { return _localServer; } }
		#else
		public IServer Server { get { return _hostedServer != null ? _hostedServer : (_serviceHost != null ? _serviceHost.Server : _localServer); } }
		#endif

		public override string ToString()
		{
			return _serverAlias.ToString();
		}
	}
}