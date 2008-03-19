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

			public ServerInfo(ServerAlias AAlias, string ALogin, string APassword)
			{
				Alias = AAlias;
				Login = ALogin;
				Password = APassword;
			}
		}

		public ArrayList ServerList = new ArrayList();
		public Hashtable ServerHash = new Hashtable();

		public void AddServer(ServerAlias AAlias, string ALogin, string APassword)
		{
			if (ServerHash.ContainsKey(AAlias.Name))
				throw new ClientException(ClientException.Codes.DuplicateAlias, AAlias.Name);

			ServerInfo LServerInfo = new ServerInfo(AAlias, ALogin, APassword);
			ServerList.Add(LServerInfo);
			ServerHash.Add(LServerInfo.Alias.Name, LServerInfo);
		}

		private Random FRandom = new Random();

		public DataSession GetConnection()
		{
			int LStartIndex = FRandom.Next(ServerList.Count);

			// only try to hit a down server every once in a while so that there isn't a 2 second delay on half the pages requests when a server is down.
			if (((ServerInfo)ServerList[LStartIndex]).LastTryFailed)
			{
				LStartIndex = FRandom.Next(ServerList.Count);
				if (((ServerInfo)ServerList[LStartIndex]).LastTryFailed)
				{
					LStartIndex = FRandom.Next(ServerList.Count);
					if (((ServerInfo)ServerList[LStartIndex]).LastTryFailed)
					{
						LStartIndex = FRandom.Next(ServerList.Count);
					}
				}
			}
			
			int LServerIndex = LStartIndex;
			ServerInfo LServerInfo = (ServerInfo)ServerList[LServerIndex];
			while (true)
			{
				try
				{
					if (!IsServerAlive(LServerInfo.ServerConnection))
						LServerInfo.ServerConnection = new ServerConnection(LServerInfo.Alias);
				
					DataSession LDataphorConnection = new DataSession();
					LDataphorConnection.Alias = LServerInfo.Alias;
					LDataphorConnection.SessionInfo.UserID = LServerInfo.Login;
					LDataphorConnection.SessionInfo.Password = LServerInfo.Password;
					LDataphorConnection.ServerConnection = LServerInfo.ServerConnection;
					LDataphorConnection.Open();
					return LDataphorConnection;
				} 
				catch (Exception E)
				{
					LServerInfo.LastTryFailed = true;

					//  Try next server until all exhausted
					LServerIndex++;
					LServerIndex %= ServerList.Count;
					if (LServerIndex != LStartIndex)
						LServerInfo = (ServerInfo)ServerList[LServerIndex];
					else
						throw E;
				}
			}
		}

		public void EnsureConnection(ref DataSession AConnection)
		{
			if ((AConnection != null) && !IsServerAlive(AConnection.ServerConnection))
			{
				((ServerInfo)ServerHash[AConnection.Alias.Name]).ServerConnection = null;

				try
				{
					AConnection.Dispose();
				}
				catch {} // throw any exception away since we are already trying to clean up a bad state

				AConnection = null;
			}
		}

		private bool IsServerAlive(ServerConnection AServerConnection)
		{
			if (AServerConnection == null)
				return false;
				
			if (AServerConnection.Server == null)
				return false;
			
			try
			{
				long LCacheTimestamp = AServerConnection.Server.CacheTimeStamp;
			} 
			catch
			{
				try
				{
					AServerConnection.Dispose();
				}
				catch {} // throw any exception away since we are already trying to clean up a bad state

				return false;
			}
			return true;
		}
	}
}

