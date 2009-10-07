/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientConnection : ClientObject, IRemoteServerConnection
	{
		public ClientConnection(ClientServer AClientServer, string AConnectionName, string AClientHostName, int AConnectionHandle)
		{
			FClientServer = AClientServer;
			FConnectionName = AConnectionName;
			FClientHostName = AClientHostName;
			FConnectionHandle = AConnectionHandle;
		}
		
		private ClientServer FClientServer;
		public ClientServer ClientServer { get { return FClientServer; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return FClientServer.GetServiceInterface();
		}
		
		private string FClientHostName;
		public string ClientHostName { get { return FClientHostName; } }
		
		private int FConnectionHandle;
		public int ConnectionHandle { get { return FConnectionHandle; } }
		
		#region IRemoteServerConnection Members

		private string FConnectionName;
		public string ConnectionName { get { return FConnectionName; } }

		public IRemoteServerSession Connect(SessionInfo ASessionInfo)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginConnect(FConnectionHandle, ASessionInfo, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return new ClientSession(this, ASessionInfo, GetServiceInterface().EndConnect(LResult));
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Disconnect(IRemoteServerSession ASession)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginDisconnect(((ClientSession)ASession).SessionHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndDisconnect(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		#endregion

		#region IPing Members

		public void Ping()
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginPingConnection(FConnectionHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndPingConnection(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		#endregion
	}
}
