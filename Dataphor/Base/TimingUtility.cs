/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
#if !SILVERLIGHT
	[System.Security.SuppressUnmanagedCodeSecurity()]
#endif
	public sealed class TimingUtility
	{
#if SILVERLIGHT
		public static bool QueryPerformanceCounter(out long lpPerformanceCount)
		{
			lpPerformanceCount = Environment.TickCount;
			return true;
		}
		
		public static bool QueryPerformanceFrequency(out long lpFrequency)
		{
			lpFrequency = 1000;
			return true;
		}
#else
		[DllImport("kernel32.dll", EntryPoint = "QueryPerformanceCounter", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[DllImport("kernel32.dll", EntryPoint = "QueryPerformanceFrequency", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool QueryPerformanceFrequency(out long lpFrequency);
#endif
		static TimingUtility()
		{
			QueryPerformanceFrequency(out _ticksPerSecond);
		}

		private static long _ticksPerSecond;
		public static long TicksPerSecond { get { return _ticksPerSecond; } }

		public static long CurrentTicks
		{
			get
			{
				long result;
				QueryPerformanceCounter(out result);
				return result;
			}
		}
		
		public static TimeSpan TimeSpanFromTicks(long startTicks)
		{
			long currentTicks;
			QueryPerformanceCounter(out currentTicks);
			return new TimeSpan((long)((((double)(currentTicks - startTicks)) / TicksPerSecond) * TimeSpan.TicksPerSecond));
		}

		private static Timing _currentTiming;

		[Conditional("TIMING")]
		public static void PushTimer(string description)
		{
			Timing timing = new Timing();
			timing._prior = _currentTiming;
			_currentTiming = timing;
			timing._description = description;
			Debug.WriteLine(String.Format("Timer: '{0}' -- started", description));
			QueryPerformanceCounter(out timing._start);
		}

		[Conditional("TIMING")]
		public static void PopTimer()
		{
			long endTime;
			QueryPerformanceCounter(out endTime);
			// TODO: account for overhead
			long elapsed = endTime - _currentTiming._start;
			Debug.WriteLine(String.Format("Timer: '{0}' -- ticks: {1}  secs: {2}", _currentTiming._description, elapsed, (decimal)elapsed / (decimal)_ticksPerSecond));
			if (_currentTiming._accumulations != null)
			{
				foreach (KeyValuePair<string, long> item in _currentTiming._accumulations)
				{
					System.Diagnostics.Debug.WriteLine
					(
						String.Format
						(
							"  Accumulator: '{0}' -- ticks: {1}  secs: {2}   timerdiff: {3}", 
							item.Key,
							item.Value,
							(decimal)item.Value / (decimal)_ticksPerSecond, 
							(decimal)(elapsed - item.Value) / (decimal)_ticksPerSecond
						)
					);
					if (_currentTiming._prior != null)
						_currentTiming._prior.Accumulate(item.Key, item.Value);
				}
			}
			_currentTiming = _currentTiming._prior;
		}

		[Conditional("TIMING")]
		public static void PushAccumulator(string description)
		{
			System.Diagnostics.Debug.Assert(_currentTiming != null, "No current timer to accumulate into");
			Accumulator accum = new Accumulator();
			accum._prior = _currentTiming._accumulator;
			accum._description = description;
			_currentTiming._accumulator = accum;
			QueryPerformanceCounter(out accum._start);
		}

		[Conditional("TIMING")]
		public static void PopAccumulator()
		{
			long endTime;
			QueryPerformanceCounter(out endTime);
			Accumulator accum = _currentTiming._accumulator;
			_currentTiming.Accumulate(accum._description, endTime - accum._start);
			_currentTiming._accumulator = accum._prior;
		}
	}

	internal class Timing
	{
		public Timing _prior;
		public long _start;
		public string _description;
		public Dictionary<string, long> _accumulations;
		public Accumulator _accumulator;

		public void Accumulate(string description, long duration)
		{
			long current = 0;
			if (_accumulations == null)
				_accumulations = new Dictionary<string, long>();
			else
				_accumulations.TryGetValue(description, out current);
			_accumulations[description] = current + duration;
		}
	}

	internal class Accumulator
	{
		public string _description;
		public Accumulator _prior;
		public long _start;
	}
}
