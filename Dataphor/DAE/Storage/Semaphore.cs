/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;
using System.Collections;

namespace Alphora.Dataphor.DAE.Storage
{
	/*
		Note regarding semaphore implementation ->
			The traditional method of implementing a shared semaphore of this type involves using
			Interlocked.CompareExchange.  This method would have worked (although I have questions
			about the implementation of such an operation given an argument larger than an int) but
			it required that a queue of waiting threads be built, which is traditionally accomplished
			using a pointer in the process block or TLS, but I could not find a way, even in the
			win api to access another threads TLS, only your own.  Given the impossibility of 
			implementing a simple linked queue using TLS, and the arguable efficiency of CompareExchange
			on arbitrary size objects, I opted for the built-in lock construct (critical sections) to
			protect the semaphore during the Acquire and Release methods.  
	*/
	
	public enum LockMode {Free, Shared, Exclusive}
	
	/// <remarks>
	/// Provides a read/write lock implementation.
	/// </remarks>	
	public class Semaphore : Object
	{
		private int FCount = 0;
		private Queue FWaitQueue = new Queue();

		private LockMode FMode = LockMode.Free;
		public LockMode Mode
		{
			get
			{
				return FMode;
			}
		}

		/// <summary>Acquires the lock in the requested mode, waiting if necessary</summary>
		public void Acquire(LockMode AMode)
		{
			while (true)
			{
				lock (this)
				{
					if ((FMode == LockMode.Free) || ((FMode == LockMode.Shared) && (AMode == FMode)))
					{
						FMode = AMode;
						FCount++;
						return;
					}
					else
						FWaitQueue.Enqueue(Thread.CurrentThread);
				}
				Thread.CurrentThread.Suspend();
			}
		}
		
		/// <summary>Acquires the lock in the requested mode if possible, otherwise returns false</summary>
		public bool AcquireImmediate(LockMode AMode)
		{
			lock (this)
			{
				if ((FMode == LockMode.Free) || ((FMode == LockMode.Shared) && (AMode == FMode)))
				{
					FMode = AMode;
					FCount++;
					return true;
				}
				else
					return false;
			}
		}
	
		/// <summary>Releases the lock</summary>
		public void Release()
		{
			lock (this)
			{
				if (FCount == 1)
					FMode = LockMode.Free;
				FCount--;
				while (FWaitQueue.Count > 0)
					((Thread)FWaitQueue.Dequeue()).Resume();
			}
		}
	}
}

