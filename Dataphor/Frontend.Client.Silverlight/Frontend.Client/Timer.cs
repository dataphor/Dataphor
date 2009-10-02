/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace Alphora.Dataphor.Frontend.Client
{
	[DesignerImage("Image('Frontend', 'Nodes.Timer')")]
	[DesignerCategory("Non Visual")]
	public class Timer : Node, ITimer
	{
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				OnElapsed = null;
				Stop();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		private DispatcherTimer FTimer;
		
		private void TimerElapsed(object ASource, EventArgs AArgs)
		{
			DoOnElapsed();
			if (!FAutoReset)
				Stop();
		}
		
		// AutoReset
		private bool FAutoReset = true;
		[Description("Indicates whether the timer will run continously, or only once.")]
		[DefaultValue(true)]
		public bool AutoReset
		{
			get { return FAutoReset; }
			set { FAutoReset = value; }
		}
		
		// Enabled
		private bool FEnabled;
		[Description("Indicates whether the timer is running.  Setting this property to true will start the timer, setting it to false will stop it.")]
		[DefaultValue(false)]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (value)
					Start();
				else
					Stop();
			}
		}
		
		public void Start()
		{
			if (!FEnabled)
			{
				FTimer = new DispatcherTimer();
				FTimer.Interval = TimeSpan.FromMilliseconds(FInterval);
				FTimer.Tick += new EventHandler(TimerElapsed);
				FEnabled = true;
			}

			FTimer.Start();
		}
		
		public void Stop()
		{
			if (FEnabled)
			{
				FTimer.Stop();
				FTimer.Tick -= new EventHandler(TimerElapsed);
				FTimer = null;
				FEnabled = false;
			}
		}
		
		// Interval
		private int FInterval = 100;
		[Description("The interval, in milliseconds between each OnElapsed call.  Setting the interval when the timer is enabled has the effect of resetting the interval count.")]
		[DefaultValue(100)]
		public int Interval
		{
			get { return FInterval; }
			set
			{
				if (FInterval != value)
				{
					FInterval = value;
					if (Enabled)
						FTimer.Interval = TimeSpan.FromMilliseconds(FInterval);
				}
			}
		}
		
		// OnElapsed		
		private IAction FOnElapsed;
		[Description("Action to be called when the timer elapses.")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		public IAction OnElapsed
		{
			get { return FOnElapsed; }
			set
			{
				if (FOnElapsed != value)
				{
					if (FOnElapsed != null)
						FOnElapsed.Disposed -= new EventHandler(OnElapsedDisposed);
					FOnElapsed = value;
					if (FOnElapsed != null)
						FOnElapsed.Disposed += new EventHandler(OnElapsedDisposed);
				}
			}
		}

		private void OnElapsedDisposed(object ASender, EventArgs AArgs)
		{
			OnElapsed = null;
		}
		
		private void DoOnElapsed()
		{
			if (FOnElapsed != null)
				FOnElapsed.Execute(this, new EventParams());
		}
	}
}