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
		
		public RemoteServerConnection GetConnection(string AConnectionName, string AHostName)
		{
			lock (FConnections)
			{
				RemoteServerConnection LConnection;
				if (!FConnections.TryGetValue(AConnectionName, out LConnection))
				{
					LConnection = (RemoteServerConnection)FRemoteServer.Establish(AConnectionName, AHostName);
					FConnections.Add(AConnectionName, LConnection);
				}
				
				return LConnection;
			}
		}
	}
}
