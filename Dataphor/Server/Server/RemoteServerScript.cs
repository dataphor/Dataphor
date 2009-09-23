/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	public class RemoteServerScript : RemoteServerChildObject, IRemoteServerScript
	{
		internal RemoteServerScript(RemoteServerProcess AProcess, ServerScript AServerScript) : base()
		{
			FProcess = AProcess;
			FServerScript = AServerScript;

			FBatches = new RemoteServerBatches();			
			foreach (ServerBatch LBatch in AServerScript.Batches)
				FBatches.Add(new RemoteServerBatch(this, LBatch));
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FServerScript = null;
			FProcess = null;
		
			base.Dispose(ADisposing);
		}
		
		public void Unprepare()
		{
			FProcess.UnprepareScript(this);
		}
		
		// Process        
		private RemoteServerProcess FProcess;
		public RemoteServerProcess Process { get { return FProcess; } }
		
		IRemoteServerProcess IRemoteServerScript.Process { get { return FProcess; } }
		
		private RemoteServerBatches FBatches;
		public RemoteServerBatches Batches { get { return FBatches; } }
		
		IRemoteServerBatches IRemoteServerScript.Batches { get { return FBatches; } }
		
		private ServerScript FServerScript;
		internal ServerScript ServerScript { get { return FServerScript; } }
		
		// Messages
		public Exception[] Messages
		{
			get
			{
				Exception[] LMessages = new Exception[FServerScript.Messages.Count];
				FServerScript.Messages.CopyTo(LMessages);
				return LMessages;
			}
		}
		
		public void CheckParsed()
		{
			if (FServerScript.Messages.HasErrors())
				throw new ServerException(ServerException.Codes.UnparsedScript, FServerScript.Messages.ToString());
		}
		
		public void Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			
			foreach (RemoteServerBatch LBatch in FBatches)
				LBatch.Execute(ref AParams, FProcess.EmptyCallInfo());
		}
	}
	
	// RemoteServerScripts
	public class RemoteServerScripts : RemoteServerChildObjects
	{		
		protected override void Validate(RemoteServerChildObject AObject)
		{
			if (!(AObject is RemoteServerScript))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerScript");
		}
		
		public new RemoteServerScript this[int AIndex]
		{
			get { return (RemoteServerScript)base[AIndex]; } 
			set { base[AIndex] = value; }
		}
	}
}
