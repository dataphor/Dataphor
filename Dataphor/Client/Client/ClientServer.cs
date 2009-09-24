/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;

namespace Client.Server
{
	public class ClientServer : IRemoteServer
	{
		#region IRemoteServer Members

		public IRemoteServerConnection Establish(string AConnectionName, string AHostName)
		{
			throw new NotImplementedException();
		}

		public void Relinquish(IRemoteServerConnection AConnection)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IServerBase Members

		public string Name
		{
			get { throw new NotImplementedException(); }
		}

		public void Start()
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}

		public ServerState State
		{
			get { throw new NotImplementedException(); }
		}

		public Guid InstanceID
		{
			get { throw new NotImplementedException(); }
		}

		public long CacheTimeStamp
		{
			get { throw new NotImplementedException(); }
		}

		public long DerivationTimeStamp
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region IDisposableNotify Members

		public event EventHandler Disposed;

		#endregion
	}
}
