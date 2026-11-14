/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Client
{
	public class DataphorConnectionFactory
	{
		private class ServerInfo
		{
			public ServerAlias Alias;
			public string Login;
			public string Password;

			public ServerConnection ServerConnection;

			public bool LastTryFailed = false;

			public ServerInfo(ServerAlias alias, string login, string password)
			{
				Alias = alias;
				Login = login;
				Password = password;
			}
		}

		private List<ServerInfo> ServerList = new List<ServerInfo>();
		private Dictionary<string, ServerInfo> ServerHash = new Dictionary<string, ServerInfo>();

		public void AddServer(ServerAlias alias, string login, string password)
		{
			if (ServerHash.ContainsKey(alias.Name))
				throw new ClientException(ClientException.Codes.DuplicateAlias, alias.Name);

			ServerInfo serverInfo = new ServerInfo(alias, login, password);
			ServerList.Add(serverInfo);
			ServerHash.Add(serverInfo.Alias.Name, serverInfo);
		}

		private Random _random = new Random();

		public DataSession GetConnection()
		{
			int startIndex = _random.Next(ServerList.Count);

			// only try to hit a down server every once in a while so that there isn't a 2 second delay on half the pages requests when a server is down.
			if ((ServerList[startIndex]).LastTryFailed)
			{
				startIndex = _random.Next(ServerList.Count);
				if ((ServerList[startIndex]).LastTryFailed)
				{
					startIndex = _random.Next(ServerList.Count);
					if ((ServerList[startIndex]).LastTryFailed)
					{
						startIndex = _random.Next(ServerList.Count);
					}
				}
			}
			
			int serverIndex = startIndex;
			ServerInfo serverInfo = ServerList[serverIndex];
			while (true)
			{
				try
				{
					if (!IsServerAlive(serverInfo.ServerConnection))
						serverInfo.ServerConnection = new ServerConnection(serverInfo.Alias);
				
					DataSession dataphorConnection = new DataSession();
					dataphorConnection.Alias = serverInfo.Alias;
					dataphorConnection.SessionInfo.UserID = serverInfo.Login;
					dataphorConnection.SessionInfo.Password = serverInfo.Password;
					dataphorConnection.ServerConnection = serverInfo.ServerConnection;
					dataphorConnection.Open();
					return dataphorConnection;
				} 
				catch (Exception E)
				{
					serverInfo.LastTryFailed = true;

					//  Try next server until all exhausted
					serverIndex++;
					serverIndex %= ServerList.Count;
					if (serverIndex != startIndex)
						serverInfo = ServerList[serverIndex];
					else
						throw E;
				}
			}
		}

		public void EnsureConnection(ref DataSession connection)
		{
			if ((connection != null) && !IsServerAlive(connection.ServerConnection))
			{
				ServerHash[connection.Alias.Name].ServerConnection = null;

				try
				{
					connection.Dispose();
				}
				catch {} // throw any exception away since we are already trying to clean up a bad state

				connection = null;
			}
		}

		private bool IsServerAlive(ServerConnection serverConnection)
		{
			if (serverConnection == null)
				return false;
				
			if (serverConnection.Server == null)
				return false;
			
			try
			{
				long cacheTimestamp = serverConnection.Server.CacheTimeStamp;
			} 
			catch
			{
				try
				{
					serverConnection.Dispose();
				}
				catch {} // throw any exception away since we are already trying to clean up a bad state

				return false;
			}
			return true;
		}
	}
}

