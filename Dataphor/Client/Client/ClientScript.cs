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
		public ClientScript(ClientProcess AClientProcess, ScriptDescriptor AScriptDescriptor)
		{
			FClientProcess = AClientProcess;
			FScriptDescriptor = AScriptDescriptor;
			
			FMessages = new Exception[FScriptDescriptor.Messages.Count];
			for (int LIndex = 0; LIndex < FScriptDescriptor.Messages.Count; LIndex++)
				FMessages[LIndex] = DataphorFaultUtility.FaultToException(FScriptDescriptor.Messages[LIndex]);
			
			foreach (BatchDescriptor LBatchDescriptor in FScriptDescriptor.Batches)
				FClientBatches.Add(new ClientBatch(this, LBatchDescriptor));
		}
		
		private ClientProcess FClientProcess;
		public ClientProcess ClientProcess { get { return FClientProcess; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return FClientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}	
		
		private ScriptDescriptor FScriptDescriptor;
		
		public int ScriptHandle { get { return FScriptDescriptor.Handle; } }
		
		private Exception[] FMessages;
		
		private ClientBatches FClientBatches = new ClientBatches();
		
		#region IRemoteServerScript Members

		public IRemoteServerProcess Process
		{
			get { return FClientProcess; }
		}

		public void Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
		{
			foreach (ClientBatch LBatch in FClientBatches)
				LBatch.Execute(ref AParams, ACallInfo);
		}

		public Exception[] Messages
		{
			get { return FMessages; }
		}

		public IRemoteServerBatches Batches
		{
			get { return FClientBatches; }
		}

		#endregion
	}
}
