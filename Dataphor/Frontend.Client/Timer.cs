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
	/// <summary> A Timer executes an action at regular intervals. </summary> <doc/>
	/// <example> An example can be seen in the Sample.Components library
	/// in the Sample10 [dfd] document.</example>
	public interface ITimer : INode
	{
		/// <summary>Indicates whether the timer will run continuously, 
		/// or only once.</summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: True</para></value>
		/// <remarks>
		/// Setting this property to true will cause the timer to reset 
		/// after each call to OnElapsed, meaning the event will be raised 
		/// again after Interval milliseconds.  If this property is set to
		/// false, the event will be raised only once, and the timer will stop.
		/// </remarks>
		bool AutoReset { get; set; }
		
		/// <summary>Indicates whether the timer is running.</summary> <doc/>
		/// <value><para>Boolean: True|False</para>
		/// <para>Default: False</para></value>
		/// <remarks>
		///	Setting this property to true will start the timer, setting 
		///	it to false will stop it.  If the timer is already enabled, 
		///	setting enabled to true will reset the interval.
		/// </remarks>
		bool Enabled { get; set; }
		
		/// <summary>The interval, in milliseconds between each OnElapsed 
		/// call.</summary> <doc/>
		/// <value><para>Double</para>
		/// <para>Default: 100</para></value>
		/// <remarks>Setting the interval when the timer is enabled has 
		/// the effect of resetting the interval count.</remarks>
		int Interval { get; set; }
		
		/// <summary>The action to bo executed when the timer elapses.</summary> <doc/>
		/// <value><para>IAction: An action in the same node tree.</para>
		/// <para>Default: (None)</para></value>
		IAction OnElapsed { get; set; }

		/// <summary>Starts the timer by setting enabled to true.</summary> <doc/>		
		/// <remarks>Note that if the timer is already started, calling 
		/// Start() will reset the interval.</remarks>
		void Start();
		
		/// <summary>Stops the timer by setting enabled to false.</summary> <doc/>
		void Stop();
	}
	
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

		private WinForms.Timer FTimer;
		
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
				FTimer = new WinForms.Timer();
				FTimer.Interval = FInterval;
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
				FTimer.Dispose();
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
						FTimer.Interval = FInterval;
				}
			}
		}
		
		// OnElapsed		
		private IAction FOnElapsed;
		[Description("Action to be called when the timer elapses.")]
		[TypeConverter(typeof(NodeReferenceConverter))]
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