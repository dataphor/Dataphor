/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalScript : LocalServerChildObject, IServerScript
    {
		public LocalScript(LocalProcess AProcess, IRemoteServerScript AScript) : base()
		{
			FProcess = AProcess;
			FScript = AScript;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FBatches != null)
			{
				FBatches.Dispose();
				FBatches = null;
			}

			FScript = null;
			FProcess = null;
			
			base.Dispose(ADisposing);
		}

		protected IRemoteServerScript FScript;
		public IRemoteServerScript RemoteScript { get { return FScript; } }
		
		protected internal LocalProcess FProcess;
		public IServerProcess Process { get { return FProcess; } }
		
		public void Execute(DataParams AParams)
		{
			RemoteParamData LParams = ((LocalProcess)FProcess).DataParamsToRemoteParamData(AParams);
			FScript.Execute(ref LParams, FProcess.GetProcessCallInfo());
			((LocalProcess)FProcess).RemoteParamDataToDataParams(AParams, LParams);
		}

		private ParserMessages FMessages;		
		public ParserMessages Messages
		{
			get
			{
				if (FMessages == null)
				{
					FMessages = new ParserMessages();
					FMessages.AddRange(FScript.Messages);
				}
				return FMessages;
			}
		}
		
		protected LocalBatches FBatches;
		public IServerBatches Batches
		{
			get
			{
				if (FBatches == null)
				{
					FBatches = new LocalBatches();
					foreach (object LBatch in FScript.Batches)
						FBatches.Add(new LocalBatch(this, (IRemoteServerBatch)LBatch));
				}
				return (IServerBatches)FBatches;
			}
		}
    }
}
