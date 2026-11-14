/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalScript : LocalServerChildObject, IServerScript
    {
		public LocalScript(LocalProcess process, IRemoteServerScript script) : base()
		{
			_process = process;
			_script = script;
		}
		
		protected override void Dispose(bool disposing)
		{
			_batches = null;
			_script = null;
			_process = null;
			
			base.Dispose(disposing);
		}

		protected IRemoteServerScript _script;
		public IRemoteServerScript RemoteScript { get { return _script; } }
		
		protected internal LocalProcess _process;
		public IServerProcess Process { get { return _process; } }
		
		public void Execute(DataParams paramsValue)
		{
			RemoteParamData localParamsValue = ((LocalProcess)_process).DataParamsToRemoteParamData(paramsValue);
			_script.Execute(ref localParamsValue, _process.GetProcessCallInfo());
			((LocalProcess)_process).RemoteParamDataToDataParams(paramsValue, localParamsValue);
		}

		private ParserMessages _messages;		
		public ParserMessages Messages
		{
			get
			{
				if (_messages == null)
				{
					_messages = new ParserMessages();
					_messages.AddRange(_script.Messages);
				}
				return _messages;
			}
		}
		
		protected LocalBatches _batches;
		public IServerBatches Batches
		{
			get
			{
				if (_batches == null)
				{
					_batches = new LocalBatches();
					foreach (object batch in _script.Batches)
						_batches.Add(new LocalBatch(this, (IRemoteServerBatch)batch));
				}
				return (IServerBatches)_batches;
			}
		}
    }
}
