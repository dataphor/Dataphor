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
	public class ClientSession : IRemoteServerSession
	{
		#region IRemoteServerSession Members

		public IRemoteServer Server
		{
			get { throw new NotImplementedException(); }
		}

		public IRemoteServerProcess StartProcess(ProcessInfo AProcessInfo, out int AProcessID)
		{
			throw new NotImplementedException();
		}

		public void StopProcess(IRemoteServerProcess AProcess)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IServerSessionBase Members

		public int SessionID
		{
			get { throw new NotImplementedException(); }
		}

		public SessionInfo SessionInfo
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}
}
