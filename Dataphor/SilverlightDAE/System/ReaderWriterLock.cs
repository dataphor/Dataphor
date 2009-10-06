using System;

namespace System.Threading
{
	/// <remarks>
	///		Adapted from: http://stackoverflow.com/questions/612765/silverlight-readerwriterlock-implementation-good-bad
	/// </remarks>
	public sealed class ReaderWriterLock
	{
		private readonly object FInternalLock = new Object();
		private int FActiveReaders = 0;
		private bool FActiveWriter = false;
		
		public void AcquireReaderLock(int ATimeout)
		{
			lock (FInternalLock)
			{
				while (FActiveWriter)
					if (!Monitor.Wait(FInternalLock, ATimeout))
						throw new Exception("Timeout waiting for reader lock.");
				++FActiveReaders;
			}
		}

		public void AcquireReaderLock()
		{
			AcquireReaderLock(Timeout.Infinite);
		}

		public void ReleaseReaderLock()
		{
			lock (FInternalLock)
			{            
				// TODO: if FActiveReaders <= 0 do some error handling
				--FActiveReaders;
				Monitor.PulseAll(FInternalLock);
			}
		}
		
		public void AcquireWriterLock(int ATimeout)
		{
			lock (FInternalLock)
			{
				// First wait for any writers to clear             
				// This assumes writers have a higher priority than readers            
				// as it will force the readers to wait until all writers are done.            
				// you can change the conditionals in here to change that behavior.            
				while (FActiveWriter)
					if (!Monitor.Wait(FInternalLock, ATimeout))
						throw new Exception("Timeout waiting for writer lock.");
				// There are no more writers, set this to true to block further readers from acquiring the lock            
				FActiveWriter = true;				
				// Now wait till all readers have completed.            
				while (FActiveReaders > 0)
					if (!Monitor.Wait(FInternalLock, ATimeout))
						throw new Exception("Timeout waiting for writer lock.");
				// The writer now has the lock
			}
		}

		public void AcquireWriterLock()
		{
			AcquireWriterLock(Timeout.Infinite);
		}
		
		public void ReleaseWriterLock()
		{
			lock (FInternalLock)
			{
				// if activeWriter != true handle the error            
				FActiveWriter = false;
				Monitor.PulseAll(FInternalLock);
			}
		}
	}
}
