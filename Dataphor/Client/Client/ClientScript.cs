/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientScript : IRemoteServerScript
	{
		#region IRemoteServerScript Members

		public IRemoteServerProcess Process
		{
			get { throw new NotImplementedException(); }
		}

		public void Execute(ref Alphora.Dataphor.DAE.Contracts.RemoteParamData AParams, Alphora.Dataphor.DAE.Contracts.ProcessCallInfo ACallInfo)
		{
			throw new NotImplementedException();
		}

		public Exception[] Messages
		{
			get { throw new NotImplementedException(); }
		}

		public IRemoteServerBatches Batches
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}
}
