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

namespace Alphora.Dataphor
{
	[System.Security.SuppressUnmanagedCodeSecurity()]
	public sealed class TimingUtility
	{
		[DllImport("kernel32.dll", EntryPoint = "QueryPerformanceCounter", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[DllImport("kernel32.dll", EntryPoint = "QueryPerformanceFrequency", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		public static extern bool QueryPerformanceFrequency(out long lpFrequency);

		static TimingUtility()
		{
			QueryPerformanceFrequency(out FTicksPerSecond);
		}

		private static long FTicksPerSecond;
		public static long TicksPerSecond { get { return FTicksPerSecond; } }

		public static long CurrentTicks
		{
			get
			{
				long LResult;
				QueryPerformanceCounter(out LResult);
				return LResult;
			}
		}
		
		public static TimeSpan TimeSpanFromTicks(long AStartTicks)
		{
			long LCurrentTicks;
			QueryPerformanceCounter(out LCurrentTicks);
			return new TimeSpan((long)((((double)(LCurrentTicks - AStartTicks)) / TicksPerSecond) * TimeSpan.TicksPerSecond));
		}

		private static Timing FCurrentTiming;

		[Conditional("TIMING")]
		public static void PushTimer(string ADescription)
		{
			Timing LTiming = new Timing();
			LTiming.FPrior = FCurrentTiming;
			FCurrentTiming = LTiming;
			LTiming.FDescription = ADescription;
			Debug.WriteLine(String.Format("Timer: '{0}' -- started", ADescription));
			QueryPerformanceCounter(out LTiming.FStart);
		}

		[Conditional("TIMING")]
		public static void PopTimer()
		{
			long LEndTime;
			QueryPerformanceCounter(out LEndTime);
			// TODO: account for overhead
			long LElapsed = LEndTime - FCurrentTiming.FStart;
			Debug.WriteLine(String.Format("Timer: '{0}' -- ticks: {1}  secs: {2}", FCurrentTiming.FDescription, LElapsed, (decimal)LElapsed / (decimal)FTicksPerSecond));
			if (FCurrentTiming.FAccumulations != null)
			{
				long LAccum;
				foreach (DictionaryEntry LItem in FCurrentTiming.FAccumulations)
				{
					LAccum = (long)LItem.Value;
					System.Diagnostics.Debug.WriteLine(String.Format("  Accumulator: '{0}' -- ticks: {1}  secs: {2}   timerdiff: {3}", LItem.Key, LAccum, (decimal)LAccum / (decimal)FTicksPerSecond, (decimal)(LElapsed - LAccum) / (decimal)FTicksPerSecond));
					if (FCurrentTiming.FPrior != null)
						FCurrentTiming.FPrior.Accumulate((string)LItem.Key, LAccum);
				}
			}
			FCurrentTiming = FCurrentTiming.FPrior;
		}

		[Conditional("TIMING")]
		public static void PushAccumulator(string ADescription)
		{
			System.Diagnostics.Debug.Assert(FCurrentTiming != null, "No current timer to accumulate into");
			Accumulator LAccum = new Accumulator();
			LAccum.FPrior = FCurrentTiming.FAccumulator;
			LAccum.FDescription = ADescription;
			FCurrentTiming.FAccumulator = LAccum;
			QueryPerformanceCounter(out LAccum.FStart);
		}

		[Conditional("TIMING")]
		public static void PopAccumulator()
		{
			long LEndTime;
			QueryPerformanceCounter(out LEndTime);
			Accumulator LAccum = FCurrentTiming.FAccumulator;
			FCurrentTiming.Accumulate(LAccum.FDescription, LEndTime - LAccum.FStart);
			FCurrentTiming.FAccumulator = LAccum.FPrior;
		}
	}

	internal class Timing
	{
		public Timing FPrior;
		public long FStart;
		public string FDescription;
		public Hashtable FAccumulations;
		public Accumulator FAccumulator;

		public void Accumulate(string ADescription, long ADuration)
		{
			if (FAccumulations == null)
				FAccumulations = new Hashtable();
			else
				if (FAccumulations.ContainsKey(ADescription))
				{	
					FAccumulations[ADescription] = (long)FAccumulations[ADescription] + ADuration;
					return;
				}
			FAccumulations[ADescription] = ADuration;
		}
	}

	internal class Accumulator
	{
		public string FDescription;
		public Accumulator FPrior;
		public long FStart;
	}
}
