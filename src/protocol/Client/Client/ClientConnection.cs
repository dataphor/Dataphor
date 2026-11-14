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
		public ClientConnection(ClientServer clientServer, string connectionName, string clientHostName, int connectionHandle)
		{
			_clientServer = clientServer;
			_connectionName = connectionName;
			_clientHostName = clientHostName;
			_connectionHandle = connectionHandle;
		}
		
		private ClientServer _clientServer;
		public ClientServer ClientServer { get { return _clientServer; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientServer.GetServiceInterface();
		}

		private void ReportCommunicationError()
		{
			_clientServer.ReportCommunicationError();
		}
		
		private string _clientHostName;
		public string ClientHostName { get { return _clientHostName; } }
		
		private int _connectionHandle;
		public int ConnectionHandle { get { return _connectionHandle; } }
		
		#region IRemoteServerConnection Members

		private string _connectionName;
		public string ConnectionName { get { return _connectionName; } }

		public IRemoteServerSession Connect(SessionInfo sessionInfo)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginConnect(_connectionHandle, sessionInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientSession(this, sessionInfo, channel.EndConnect(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Disconnect(IRemoteServerSession session)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginDisconnect(((ClientSession)session).SessionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndDisconnect(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		#endregion

		#region IPing Members

		public void Ping()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginPingConnection(_connectionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndPingConnection(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		#endregion
	}
}
