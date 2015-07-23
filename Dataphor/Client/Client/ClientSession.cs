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
	public class ClientSession : ClientObject, IRemoteServerSession
	{
		public ClientSession(ClientConnection clientConnection, SessionInfo sessionInfo, SessionDescriptor sessionDescriptor)
		{
			_clientConnection = clientConnection;
			_sessionInfo = sessionInfo;
			_sessionDescriptor = sessionDescriptor;
		}
		
		private ClientConnection _clientConnection;
		public ClientConnection ClientConnection { get { return _clientConnection; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientConnection.ClientServer.GetServiceInterface();
		}

		private void ReportCommunicationError()
		{
			_clientConnection.ClientServer.ReportCommunicationError();
		}
		
		private SessionInfo _sessionInfo;
		
		private SessionDescriptor _sessionDescriptor;

		public int SessionHandle { get { return _sessionDescriptor.Handle; } }
		
		#region IRemoteServerSession Members

		public IRemoteServer Server
		{
			get { return _clientConnection.ClientServer; }
		}

		public IRemoteServerProcess StartProcess(ProcessInfo processInfo, out int processID)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginStartProcess(_sessionDescriptor.Handle, processInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				ProcessDescriptor processDescriptor = channel.EndStartProcess(result);
				processID = processDescriptor.ID;
				return new ClientProcess(this, processInfo, processDescriptor);
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

		public void StopProcess(IRemoteServerProcess process)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginStopProcess(((ClientProcess)process).ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndStopProcess(result);
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

		#region IServerSessionBase Members

		public int SessionID
		{
			get { return _sessionDescriptor.ID; }
		}

		public SessionInfo SessionInfo
		{
			get { return _sessionInfo; }
		}

		#endregion
	}
}
