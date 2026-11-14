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
		internal RemoteServerScript(RemoteServerProcess process, ServerScript serverScript) : base()
		{
			_process = process;
			_serverScript = serverScript;

			_batches = new RemoteServerBatches();			
			foreach (ServerBatch batch in serverScript.Batches)
				_batches.Add(new RemoteServerBatch(this, batch));
		}
		
		protected override void Dispose(bool disposing)
		{
			_serverScript = null;
			_process = null;
		
			base.Dispose(disposing);
		}
		
		public void Unprepare()
		{
			_process.UnprepareScript(this);
		}
		
		// Process        
		private RemoteServerProcess _process;
		public RemoteServerProcess Process { get { return _process; } }
		
		IRemoteServerProcess IRemoteServerScript.Process { get { return _process; } }
		
		private RemoteServerBatches _batches;
		public RemoteServerBatches Batches { get { return _batches; } }
		
		IRemoteServerBatches IRemoteServerScript.Batches { get { return _batches; } }
		
		private ServerScript _serverScript;
		internal ServerScript ServerScript { get { return _serverScript; } }
		
		// Messages
		public Exception[] Messages
		{
			get
			{
				Exception[] messages = new Exception[_serverScript.Messages.Count];
				_serverScript.Messages.CopyTo(messages);
				return messages;
			}
		}
		
		public void CheckParsed()
		{
			if (_serverScript.Messages.HasErrors())
				throw new ServerException(ServerException.Codes.UnparsedScript, _serverScript.Messages.ToString());
		}
		
		public void Execute(ref RemoteParamData paramsValue, ProcessCallInfo callInfo)
		{
			_process.ProcessCallInfo(callInfo);
			
			foreach (RemoteServerBatch batch in _batches)
				batch.Execute(ref paramsValue, _process.EmptyCallInfo());
		}
	}
	
	// RemoteServerScripts
	public class RemoteServerScripts : RemoteServerChildObjects
	{		
		protected override void Validate(RemoteServerChildObject objectValue)
		{
			if (!(objectValue is RemoteServerScript))
				throw new ServerException(ServerException.Codes.TypedObjectContainer, "RemoteServerScript");
		}
		
		public new RemoteServerScript this[int index]
		{
			get { return (RemoteServerScript)base[index]; } 
			set { base[index] = value; }
		}
	}
}
