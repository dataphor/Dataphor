using System;

namespace System.Diagnostics
{
	/// <summary> Replacement for .NET Stopwatch. </summary>
	/// <remarks> Adapted from http://www.wiredprairie.us/blog/index.php/archives/723 </remarks>
	public sealed class Stopwatch
	{
		/// <summary> Creates a new instance of the class and starts the watch immediately. </summary>
		/// <returns>An instance of Stopwatch, running.</returns>
		public static Stopwatch StartNew()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			return sw;
		}

		private int FStartTick;
		private long FElapsed;
		private bool FIsRunning;

		/// <summary> Completely resets and deactivates the timer. </summary>
		public void Reset()
		{
			FElapsed = 0;
			FIsRunning = false;
			FStartTick = 0;
		}

		/// <summary> Begins or restarts the timer. </summary>
		/// <remarks> Accumulated time remains. </remarks>
		public void Start()
		{
			if (!FIsRunning)
			{
				FStartTick = Environment.TickCount;
				FIsRunning = true;
			}
		}

		/// <summary> Stops the current timer. </summary>
		public void Stop()
		{
			if (FIsRunning)
			{
				FElapsed += Environment.TickCount - FStartTick;
				FIsRunning = false;
			}
		}

		/// <summary> Gets a value indicating whether the instance is currently recording. </summary>
		public bool IsRunning
		{
			get { return FIsRunning; }
		}

		/// <summary> Gets the Ellapsed time as a Timespan. </summary>
		public TimeSpan Ellapsed
		{
			get { return new TimeSpan(EllapsedTicks); }
		}

		/// <summary> Gets the Ellapsed time as the total number of milliseconds. </summary>
		public long EllapsedMilliseconds
		{
			get { return GetCurrentElapsed(); }
		}

		/// <summary> Gets the Ellapsed time as the total number of ticks. </summary>
		public long EllapsedTicks
		{
			get { return GetCurrentElapsed() * 10000; }
		}

		private long GetCurrentElapsed()
		{
			return FElapsed + (IsRunning ? (Environment.TickCount - FStartTick) : 0);
		}
	}
}