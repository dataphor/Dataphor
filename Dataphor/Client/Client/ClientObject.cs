/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientObject : IDisposable, IDisposableNotify
	{
		#region IDisposable Members
		
		protected virtual void InternalDispose()
		{
		}

		public void Dispose()
		{
			DoDisposed();
			InternalDispose();
		}

		#endregion

		#region IDisposableNotify Members
		
		private void DoDisposed()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

		public event EventHandler Disposed;

		#endregion
	}
}
