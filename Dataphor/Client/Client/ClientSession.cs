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
				IAsyncResult result = GetServiceInterface().BeginStartProcess(_sessionDescriptor.Handle, processInfo, null, null);
				result.AsyncWaitHandle.WaitOne();
				ProcessDescriptor processDescriptor = GetServiceInterface().EndStartProcess(result);
				processID = processDescriptor.ID;
				return new ClientProcess(this, processInfo, processDescriptor);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void StopProcess(IRemoteServerProcess process)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginStopProcess(((ClientProcess)process).ProcessHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndStopProcess(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
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
