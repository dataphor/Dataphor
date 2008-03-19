/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;

namespace Alphora.Dataphor.DAE.Server
{
	public class SignalPool
	{
		private const int CInitialSignalPoolCapacity = 10;
		
		private ManualResetEvent[] FSignalPool = new ManualResetEvent[CInitialSignalPoolCapacity];
		private int FSignalPoolCount;
		private int FSignalPoolInUse;

		public ManualResetEvent Acquire()
		{
			// Spin until we have exlusive
			while (Interlocked.CompareExchange(ref FSignalPoolInUse, 1, 0) == 1);

			ManualResetEvent LSignal = null;
			if (FSignalPoolCount > 0)
			{
				try
				{
					FSignalPoolCount--;
					LSignal = FSignalPool[FSignalPoolCount];
				}
				finally
				{
					Interlocked.Decrement(ref FSignalPoolInUse);
				}
				LSignal.Reset();
				return LSignal;
			}
			else
			{
				Interlocked.Decrement(ref FSignalPoolInUse);
				return new ManualResetEvent(false);
			}
		}

		public void Relinquish(ManualResetEvent AEvent)
		{
			// Spin until we have exlusive
			while (Interlocked.CompareExchange(ref FSignalPoolInUse, 1, 0) == 1);
			try
			{
				// Grow the capacity if necessary
				if (FSignalPool.Length <= FSignalPoolCount)
				{
					ManualResetEvent[] LNewList = new ManualResetEvent[Math.Min(FSignalPool.Length * 2, FSignalPool.Length + 512)];
					Array.Copy(FSignalPool, 0, LNewList, 0, FSignalPool.Length);
					FSignalPool = LNewList;
				}

				// Add to the pool
				FSignalPool[FSignalPoolCount] = AEvent;
				FSignalPoolCount++;
			}
			finally
			{
				Interlocked.Decrement(ref FSignalPoolInUse);
			}
		}
	}
}