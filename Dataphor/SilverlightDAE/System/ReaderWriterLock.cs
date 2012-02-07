using System;

namespace System.Threading
{
	/// <remarks>
	///		Adapted from: http://stackoverflow.com/questions/612765/silverlight-readerwriterlock-implementation-good-bad
	/// </remarks>
	public sealed class ReaderWriterLock
	{
		private readonly object FInternalLock = new Object();
		private int _activeReaders = 0;
		private bool _activeWriter = false;
		
		public void AcquireReaderLock(int timeout)
		{
			lock (FInternalLock)
			{
				while (_activeWriter)
					if (!Monitor.Wait(FInternalLock, timeout))
						throw new Exception("Timeout waiting for reader lock.");
				++_activeReaders;
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
				--_activeReaders;
				Monitor.PulseAll(FInternalLock);
			}
		}
		
		public void AcquireWriterLock(int timeout)
		{
			lock (FInternalLock)
			{
				// First wait for any writers to clear             
				// This assumes writers have a higher priority than readers            
				// as it will force the readers to wait until all writers are done.            
				// you can change the conditionals in here to change that behavior.            
				while (_activeWriter)
					if (!Monitor.Wait(FInternalLock, timeout))
						throw new Exception("Timeout waiting for writer lock.");
				// There are no more writers, set this to true to block further readers from acquiring the lock            
				_activeWriter = true;				
				// Now wait till all readers have completed.            
				while (_activeReaders > 0)
					if (!Monitor.Wait(FInternalLock, timeout))
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
				_activeWriter = false;
				Monitor.PulseAll(FInternalLock);
			}
		}
	}
}
