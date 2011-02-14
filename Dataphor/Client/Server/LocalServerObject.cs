/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Server
{
	public class LocalServerObject : IDisposableNotify
	{
		#if USEFINALIZER
		~LocalServerObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif

		protected virtual void Dispose(bool disposing)
		{
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			DoDispose();
		}
		
		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}
	}
	
	public class LocalServerChildObject : IDisposableNotify
	{
		#if USEFINALIZER
		~LocalServerChildObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif

		protected internal void Dispose()
		{
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			DoDispose();
		}
		
		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}
	}
}
