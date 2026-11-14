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
		private const int InitialSignalPoolCapacity = 10;
		
		private ManualResetEvent[] _signalPool = new ManualResetEvent[InitialSignalPoolCapacity];
		private int _signalPoolCount;
		private int _signalPoolInUse;

		public ManualResetEvent Acquire()
		{
			// Spin until we have exlusive
			while (Interlocked.CompareExchange(ref _signalPoolInUse, 1, 0) == 1);

			ManualResetEvent signal = null;
			if (_signalPoolCount > 0)
			{
				try
				{
					_signalPoolCount--;
					signal = _signalPool[_signalPoolCount];
				}
				finally
				{
					Interlocked.Decrement(ref _signalPoolInUse);
				}
				signal.Reset();
				return signal;
			}
			else
			{
				Interlocked.Decrement(ref _signalPoolInUse);
				return new ManualResetEvent(false);
			}
		}

		public void Relinquish(ManualResetEvent eventValue)
		{
			// Spin until we have exlusive
			while (Interlocked.CompareExchange(ref _signalPoolInUse, 1, 0) == 1);
			try
			{
				// Grow the capacity if necessary
				if (_signalPool.Length <= _signalPoolCount)
				{
					ManualResetEvent[] newList = new ManualResetEvent[Math.Min(_signalPool.Length * 2, _signalPool.Length + 512)];
					Array.Copy(_signalPool, 0, newList, 0, _signalPool.Length);
					_signalPool = newList;
				}

				// Add to the pool
				_signalPool[_signalPoolCount] = eventValue;
				_signalPoolCount++;
			}
			finally
			{
				Interlocked.Decrement(ref _signalPoolInUse);
			}
		}
	}
}