using System;

namespace System.Threading
{
	public sealed class ReaderWriterLock
	{
		private readonly object internalLock = new object();
		private int activeReaders = 0;
		private bool activeWriter = false;

		public void AcquireReaderLock()
		{
			lock (internalLock)
			{
				while (activeWriter)
					Monitor.Wait(internalLock);
				++activeReaders;
			}
		}

		public void ReleaseReaderLock()
		{
			lock (internalLock)
			{
				// if activeReaders <= 0 do some error handling
				--activeReaders;
				Monitor.PulseAll(internalLock);
			}
		}

		public void AcquireWriterLock()
		{
			lock (internalLock)
			{
				// first wait for any writers to clear 
				// This assumes writers have a higher priority than readers
				// as it will force the readers to wait until all writers are done.
				// you can change the conditionals in here to change that behavior.
				while (activeWriter)
					Monitor.Wait(internalLock);

				// There are no more writers, set this to true to block further readers from acquiring the lock
				activeWriter = true;

				// Now wait till all readers have completed.
				while (activeReaders > 0)
					Monitor.Wait(internalLock);

				// The writer now has the lock
			}
		}

		public void ReleaseWriterLock()
		{
			lock (internalLock)
			{
				// if activeWriter != true handle the error
				activeWriter = false;
				Monitor.PulseAll(internalLock);
			}
		}
	}
}
