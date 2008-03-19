/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Threading;

namespace Alphora.Dataphor.DAE.Storage
{
	/// <summary> Simple page-level, read/write lock manager. </summary>
	public class LockManager
	{
		private Hashtable FLocks = new Hashtable();

		public void Lock(PageID APageID, bool AExclusive)
		{
			lock(FLocks)
			{
				ReaderWriterLock LLock = (ReaderWriterLock)FLocks[APageID];
				if (LLock == null)
				{
					LLock = new ReaderWriterLock();
					FLocks.Add(APageID, LLock);
				}
				if (AExclusive)
					LLock.AcquireWriterLock(-1);
				else
					LLock.AcquireReaderLock(-1);
			}
		}

		public bool LockImmediate(PageID APageID, bool AExclusive)
		{
			lock(FLocks)
			{
				ReaderWriterLock LLock = (ReaderWriterLock)FLocks[APageID];
				if (LLock == null)
				{
					LLock = new ReaderWriterLock();
					FLocks.Add(APageID, LLock);
				}
				try
				{
					if (AExclusive)
						LLock.AcquireWriterLock(0);
					else
						LLock.AcquireReaderLock(0);
					return true;
				}
				catch (ApplicationException)
				{
					// Nothing
				}
				return false;
			}
		}

		public void Release(PageID APageID)
		{
			lock(FLocks)
			{
				ReaderWriterLock LLock = (ReaderWriterLock)FLocks[APageID];
				if (LLock != null)
				{
					if (LLock.IsWriterLockHeld)
						LLock.ReleaseWriterLock();
					else
						LLock.ReleaseReaderLock();
					try
					{
						// Test to see if we are the last one out
						LLock.AcquireWriterLock(0);
					}
					catch (ApplicationException)
					{
						return;
					}
					LLock.ReleaseWriterLock();
					FLocks.Remove(APageID);
				}
			}
		}
	}
}
