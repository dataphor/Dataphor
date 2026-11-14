/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using WinForms=System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client
{
	[DesignerImage("Image('Frontend', 'Nodes.Timer')")]
	[DesignerCategory("Non Visual")]
	public class Timer : Node, ITimer
	{
		protected override void Dispose(bool disposing)
		{
			try
			{
				OnElapsed = null;
				Stop();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private WinForms.Timer _timer;
		
		private void TimerElapsed(object source, EventArgs args)
		{
			DoOnElapsed();
			if (!_autoReset)
				Stop();
		}
		
		// AutoReset
		private bool _autoReset = true;
		[Description("Indicates whether the timer will run continously, or only once.")]
		[DefaultValue(true)]
		public bool AutoReset
		{
			get { return _autoReset; }
			set { _autoReset = value; }
		}
		
		// Enabled
		private bool _enabled;
		[Description("Indicates whether the timer is running.  Setting this property to true will start the timer, setting it to false will stop it.")]
		[DefaultValue(false)]
		public bool Enabled
		{
			get { return _enabled; }
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
			if (!_enabled)
			{
				_timer = new WinForms.Timer();
				_timer.Interval = _interval;
				_timer.Tick += new EventHandler(TimerElapsed);
				_enabled = true;
			}

			_timer.Start();
		}
		
		public void Stop()
		{
			if (_enabled)
			{
				_timer.Stop();
				_timer.Tick -= new EventHandler(TimerElapsed);
				_timer.Dispose();
				_timer = null;
				_enabled = false;
			}
		}
		
		// Interval
		private int _interval = 100;
		[Description("The interval, in milliseconds between each OnElapsed call.  Setting the interval when the timer is enabled has the effect of resetting the interval count.")]
		[DefaultValue(100)]
		public int Interval
		{
			get { return _interval; }
			set
			{
				if (_interval != value)
				{
					_interval = value;
					if (Enabled)
						_timer.Interval = _interval;
				}
			}
		}
		
		// OnElapsed		
		private IAction _onElapsed;
		[Description("Action to be called when the timer elapses.")]
		[TypeConverter("Alphora.Dataphor.Frontend.Client.NodeReferenceConverter,Alphora.Dataphor.Frontend.Client")]
		public IAction OnElapsed
		{
			get { return _onElapsed; }
			set
			{
				if (_onElapsed != value)
				{
					if (_onElapsed != null)
						_onElapsed.Disposed -= new EventHandler(OnElapsedDisposed);
					_onElapsed = value;
					if (_onElapsed != null)
						_onElapsed.Disposed += new EventHandler(OnElapsedDisposed);
				}
			}
		}

		private void OnElapsedDisposed(object sender, EventArgs args)
		{
			OnElapsed = null;
		}
		
		private void DoOnElapsed()
		{
			if (_onElapsed != null)
				_onElapsed.Execute(this, new EventParams());
		}
	}
}