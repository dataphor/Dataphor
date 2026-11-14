/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientScript : ClientObject, IRemoteServerScript
	{
		public ClientScript(ClientProcess clientProcess, ScriptDescriptor scriptDescriptor)
		{
			_clientProcess = clientProcess;
			_scriptDescriptor = scriptDescriptor;
			
			_messages = new Exception[_scriptDescriptor.Messages.Count];
			for (int index = 0; index < _scriptDescriptor.Messages.Count; index++)
				_messages[index] = DataphorFaultUtility.FaultToException(_scriptDescriptor.Messages[index]);
			
			foreach (BatchDescriptor batchDescriptor in _scriptDescriptor.Batches)
				_clientBatches.Add(new ClientBatch(this, batchDescriptor));
		}
		
		private ClientProcess _clientProcess;
		public ClientProcess ClientProcess { get { return _clientProcess; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}	

		private void ReportCommunicationError()
		{
			_clientProcess.ClientSession.ClientConnection.ClientServer.ReportCommunicationError();
		}
		
		private ScriptDescriptor _scriptDescriptor;
		
		public int ScriptHandle { get { return _scriptDescriptor.Handle; } }
		
		private Exception[] _messages;
		
		private ClientBatches _clientBatches = new ClientBatches();
		
		#region IRemoteServerScript Members

		public IRemoteServerProcess Process
		{
			get { return _clientProcess; }
		}

		public void Execute(ref RemoteParamData paramsValue, ProcessCallInfo callInfo)
		{
			foreach (ClientBatch batch in _clientBatches)
				batch.Execute(ref paramsValue, callInfo);
		}

		public Exception[] Messages
		{
			get { return _messages; }
		}

		public IRemoteServerBatches Batches
		{
			get { return _clientBatches; }
		}

		#endregion
	}
}
