/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
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
		[DefaultValue(true)]
		public bool AutoReset
		{
			get { return FAutoReset; }
			set { FAutoReset = value; }
		}
		
		// Enabled
		private bool FEnabled;
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