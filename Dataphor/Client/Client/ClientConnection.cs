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
	public class ClientConnection : IRemoteServerConnection
	{
		#region IRemoteServerConnection Members

		public string ConnectionName
		{
			get { throw new NotImplementedException(); }
		}

		public IRemoteServerSession Connect(SessionInfo ASessionInfo)
		{
			throw new NotImplementedException();
		}

		public void Disconnect(IRemoteServerSession ASession)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion

		#region IPing Members

		public void Ping()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
