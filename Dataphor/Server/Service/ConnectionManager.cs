/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	public class ConnectionManager
	{
		public ConnectionManager(RemoteServer ARemoteServer)
		{
			FRemoteServer = ARemoteServer;
		}
		
		private RemoteServer FRemoteServer;
		
		private Dictionary<string, RemoteServerConnection> FConnections = new Dictionary<string, RemoteServerConnection>();
		private Dictionary<RemoteServerConnection, string> FConnectionIndex = new Dictionary<RemoteServerConnection, string>();
		
		public RemoteServerConnection GetConnection(string AConnectionName, string AHostName)
		{
			lock (FConnections)
			{
				RemoteServerConnection LConnection;
				if (!FConnections.TryGetValue(AConnectionName, out LConnection))
				{
					LConnection = (RemoteServerConnection)FRemoteServer.Establish(AConnectionName, AHostName);
					LConnection.Disposed += new EventHandler(ConnectionDisposed);
					FConnections.Add(AConnectionName, LConnection);
					FConnectionIndex.Add(LConnection, AConnectionName);
				}
				
				return LConnection;
			}
		}

		private void ConnectionDisposed(object ASender, EventArgs AArgs)
		{
			((RemoteServerConnection)ASender).Disposed -= new EventHandler(ConnectionDisposed);

			lock (FConnections)
			{
				string LConnectionName;
				if (FConnectionIndex.TryGetValue((RemoteServerConnection)ASender, out LConnectionName))
				{
					FConnectionIndex.Remove((RemoteServerConnection)ASender);
					FConnections.Remove(LConnectionName);
				}
			}
		}
	}
}
