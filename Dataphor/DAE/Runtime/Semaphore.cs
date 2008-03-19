/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
//#define UseReaderWriterLock

namespace Alphora.Dataphor.DAE.Runtime
{
	using System;
	using System.Threading;
	using System.Collections;
	
	/*
		Note regarding semaphore implementation ->
			The traditional method of implementing a shared semaphore of this type involves using
			Interlocked.CompareExchange.  This method would have worked (although I have questions
			about the implementation of such an operation given an argument larger than an int) but
			it required that a list of waiting threads be built, which is traditionally accomplished
			using a pointer in the process block or TLS, but I could not find a way, even in the
			win api to access another threads TLS, only your own.  Given the impossibility of 
			implementing a simple linked list using TLS, and the arguable efficiency of CompareExchange
			on arbitrary size objects, I opted for the built-in lock construct (critical sections) to
			protect the semaphore during the Acquire and Release methods.  
		
		Note regarding reader/writer locks ->
			The ReaderWriterLock class cannot be used because it assumes the thread is the owner of the lock.
			The Semaphore implementation uses CurrentContext.ContextID as the key for lock ownership.
	*/
	
	public enum LockMode {Free, Shared, Exclusive}

	#if UseReaderWriterLock	
	public class Semaphore : Object
	{
		private ReaderWriterLock FLock = new ReaderWriterLock();
		private LockMode FMode;
		private int FCount;
		public LockMode Mode
		{
			get { return FMode; }
		}
		
		public void Acquire(LockMode AMode)
		{
			switch (AMode)
			{
				case LockMode.Shared: FLock.AcquireReaderLock(-1); break;
				case LockMode.Exclusive: FLock.AcquireWriterLock(-1); break;
				default: throw new RuntimeException(RuntimeException.Codes.InvalidLockMode, AMode.ToString());
			}
			FCount++;
			FMode = AMode;
		}
		
		public bool AcquireImmediate(LockMode AMode)
		{
			try
			{
				switch (AMode)
				{
					case LockMode.Shared: FLock.AcquireReaderLock(0); break;
					case LockMode.Exclusive: FLock.AcquireWriterLock(0); break;
				default: throw new RuntimeException(RuntimeException.Codes.InvalidLockMode, AMode.ToString());
				}
				FCount++;
				FMode = AMode;
				return true;
			}
			catch (ApplicationException)
			{
				return false;
			}
		}
		
		public void Release()
		{
			switch (FMode)
			{
				case LockMode.Shared: FLock.ReleaseReaderLock(); break;
				case LockMode.Exclusive: FLock.ReleaseWriterLock(); break;
				default: throw new RuntimeException(RuntimeException.Codes.InvalidLockMode, AMode.ToString());
			}
			FCount--;
			if (FCount == 0)
				FMode = LockMode.Free;
		}
	}
	#else
	public class WaitList : List
	{
		public new Thread this[int AIndex] { get { return (Thread)base[AIndex]; } }
	}
	
	/// <remarks>
	/// Provides a read/write lock implementation.
	/// </remarks>	
	public class Semaphore : Object
	{
		private WaitList FWaitList = new WaitList();
		private ArrayList FOwnerList = new ArrayList();
		private int FExclusiveIndex = -1; // the index at which the lock was acquired in exclusive mode

		private LockMode FMode = LockMode.Free;
		public LockMode Mode { get { return FMode; } }
		
		public bool IsSemaphoreOwned(int AOwnerID)
		{
			for (int LIndex = 0; LIndex < FOwnerList.Count; LIndex++)
				if ((int)FOwnerList[LIndex] != AOwnerID)
					return false;
			return true;
		}

		/// <summary>Acquires the lock in the requested mode, waiting if necessary</summary>
		public void Acquire(int AOwnerID, LockMode AMode, int ATimeout)
		{
			DateTime LNow = DateTime.Now;
			while (true)
			{
				lock (this)
				{
					if 
						(
							(FMode == LockMode.Free) || 
							(
								(FMode == LockMode.Shared) && 
								(AMode == FMode)
							) ||
							IsSemaphoreOwned(AOwnerID)
						)
					{
						FMode = AMode;
						if ((FMode == LockMode.Exclusive) && (FExclusiveIndex == -1))
							FExclusiveIndex = FOwnerList.Add(AOwnerID);
						else
							FOwnerList.Add(AOwnerID);
						return;
					}
					else
						FWaitList.Add(Thread.CurrentThread);
				}
				Thread.Sleep(ATimeout);
				if (DateTime.Now > LNow.AddMilliseconds(ATimeout))
				{
					lock (this)
					{
						if (FWaitList.Contains(Thread.CurrentThread))
							FWaitList.Remove(Thread.CurrentThread);
					}
					throw new RuntimeException(RuntimeException.Codes.SemaphoreTimeout);
				}
			}
		}
		
		public void Acquire(int AOwnerID, LockMode AMode)
		{
			Acquire(AOwnerID, AMode, Timeout.Infinite);
		}
		
		/// <summary>Acquires the lock in the requested mode if possible, otherwise returns false</summary>
		public bool AcquireImmediate(int AOwnerID, LockMode AMode)
		{
			lock (this)
			{
				if
					(
						(FMode == LockMode.Free) ||
						(
							(FMode == LockMode.Shared) &&
							(AMode == FMode)
						) ||
						IsSemaphoreOwned(AOwnerID)
					)
				{
					FMode = AMode;
					if ((FMode == LockMode.Exclusive) && (FExclusiveIndex == -1))
						FExclusiveIndex = FOwnerList.Add(AOwnerID);
					else
						FOwnerList.Add(AOwnerID);
					return true;
				}
				else
					return false;
			}
		}
	
		/// <summary>Releases the lock</summary>
		public void Release(int AOwnerID)
		{
			lock (this)
			{
				#if FINDLEAKS
				bool LReleased = false;
				#endif
				for (int LIndex = FOwnerList.Count - 1; LIndex >= 0; LIndex--)
				{
					if ((int)FOwnerList[LIndex] == AOwnerID)
					{
						#if FINDLEAKS
						LReleased = true;
						#endif
						FOwnerList.RemoveAt(LIndex);
						
						if (FMode == LockMode.Exclusive)
						{
							if (LIndex == FExclusiveIndex)
							{
								if (LIndex > 0)
									FMode = LockMode.Shared;
								else
									FMode = LockMode.Free;
								FExclusiveIndex = -1;
							}
						}
						else
						{
							if (LIndex == 0)
								FMode = LockMode.Free;
						}

						break;
					}
				}

				#if FINDLEAKS				
				if (!LReleased)
					throw new RuntimeException(RuntimeException.Codes.NotAcquired);
				#endif

				// Wakeup waiting threads				
				while (FWaitList.Count > 0)
				{
					FWaitList[0].Interrupt();
					FWaitList.RemoveAt(0);
				}
			}
		}
		
		public int GrantCount()
		{
			return FOwnerList.Count;
		}
		
		public int WaitCount()
		{
			return FWaitList.Count;
		}
	}
	#endif
}

