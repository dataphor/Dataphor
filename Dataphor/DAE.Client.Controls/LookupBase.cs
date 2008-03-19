/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	public abstract class LookupBase : Control
	{
		public LookupBase()
		{
			SuspendLayout();

			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			InitializeButton();

			ResumeLayout(false);
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				base.Dispose(ADisposing);
			}
			finally
			{
				DisposeButton();
			}
		}

		#region Button

		private BitmapButton FButton;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BitmapButton Button 
		{ 
			get { return FButton; } 
		}

		protected virtual void InitializeButton()
		{
			FButton = new SpeedButton();
			FButton.Image = SpeedButton.ResourceBitmap(typeof(LookupPanel), "Alphora.Dataphor.DAE.Client.Controls.Images.Lookup.bmp");
			FButton.Parent = this;
			FButton.Size = MinButtonSize();
			FButton.Click += new EventHandler(LookupButtonClick);
		}

		protected virtual void DisposeButton()
		{
			if (FButton != null)
			{
				FButton.Click -= new EventHandler(LookupButtonClick);
				FButton.Dispose();
				FButton = null;
			}
		}

		private bool FInButtonClick;

		private void LookupButtonClick(object ASender, EventArgs AArgs)
		{
			FInButtonClick = true;
			try
			{
				if (FocusControl())		// Don't proceed unless we take focus (in case another control fails validation)
					PerformLookup();
			}
			finally
			{
				FInButtonClick = false;
			}
		}

		protected Size MinButtonSize()
		{
			return FButton.Image.Size + new Size(5, 5);
		}

		#endregion

		#region Lookup

		public event LookupEventHandler Lookup;

		protected virtual void OnLookup(LookupEventArgs AArgs)
		{
			if (Enabled && (Lookup != null))
				Lookup(this, AArgs);
		}

		private void PerformLookup()
		{
			OnLookup(new LookupEventArgs());
		}

		#endregion

		#region AutoLookup

		private bool FAutoLookup;
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AutoLookup
		{
			get { return FAutoLookup; }
			set { FAutoLookup = value; }
		}

		private bool FAutoLookupPerformed = false;	// Only perform auto lookup once per instance

		/// <summary> Resets the auto-lookup feature, allowing auto-lookup to take place when focus is navigated to the control again. </summary>
		public void ResetAutoLookup()
		{
			FAutoLookupPerformed = false;
		}

		protected override void OnEnter(EventArgs AArgs)
		{
			base.OnEnter(AArgs);
			if (FAutoLookup && !FInButtonClick && !FAutoLookupPerformed && OnQueryAutoLookupEnabled())
			{
				FAutoLookupPerformed = true;
				PerformLookup();
			}
		}

		public event QueryAutoLookupEnabledHandler QueryAutoLookupEnabled;

		/// <returns>True if any data-aware control is empty, otherwise false.</returns>
		protected virtual bool OnQueryAutoLookupEnabled()
		{
			if (QueryAutoLookupEnabled != null)
				return QueryAutoLookupEnabled(this);
			return true;
		}

		#endregion

		#region Keyboard

		private Keys FLookupKey = Keys.Insert;
		[Category("Behavior")]
		[DefaultValue(Keys.Insert)]
		[Description("Shortcut key to exec lookup.")]
		public virtual Keys LookupKey
		{
			get { return FLookupKey; }
			set { FLookupKey = value; }
		}

		protected virtual bool IsLookupKey(Keys AKey)
		{
			return (AKey == FLookupKey) || (AKey == (Keys.Alt | Keys.Down));
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if (IsLookupKey(AKey))
			{
				OnLookup(new LookupEventArgs());
				return true;
			}
			else
				return base.ProcessDialogKey(AKey);
		}

		#endregion

		/// <summary> Minimimum DisplayRectangle height at which the control remains non-distored. </summary>
		public virtual int MinDisplayHeight()
		{
			return MinButtonSize().Height;
		}

		/// <summary> Performs the action necessary to focus this control (or child control). </summary>
		public abstract bool FocusControl();
	}

	public delegate bool QueryAutoLookupEnabledHandler(LookupBase ALookup);

	public delegate void LookupEventHandler(object ASender, LookupEventArgs AArgs);

	/// <summary> Provides data for the Lookup event. </summary>
	public class LookupEventArgs : EventArgs
	{
		public LookupEventArgs() {}

		public LookupEventArgs(Keys AKeyData)
		{
			FKeyData = AKeyData;
		}

		private Keys FKeyData = Keys.None;
		/// <summary> The key that invoked the lookup. </summary>
		public Keys KeyData
		{
			get { return FKeyData; }
		}
	}

	public class LookupBoundsUtility
	{
		public static void ConstrainMin(ref int AValue, int AMin)
		{
			if (AValue < AMin)
				AValue = AMin;
		}

		public static void ConstrainMax(ref int AValue, int AMax)
		{
			if (AValue > AMax)
				AValue = AMax;
		}

		// Returns delta
		private static int CalcOffset(ref int AUsed, int AMin, int AMax)
		{
			int LOriginal = AUsed;
			if (AUsed < AMin)
				AUsed = AMin;
			if (AUsed > AMax)
				AUsed = AMax;
			return LOriginal - AUsed;
		}

		public static Rectangle DetermineBounds(Size ANatural, Size AMin, Control ALookupControl)
		{
			int LAbove;
			int LBelow;
			int LUsed;

			Rectangle LResult = new Rectangle(ALookupControl.PointToScreen(Point.Empty), ALookupControl.Size);
			Rectangle LWorking = Screen.FromRectangle(LResult).WorkingArea;

			// Figure the horizonal component
			LUsed = LWorking.Right - LResult.Left;
			LResult.X += Math.Min(0, CalcOffset(ref LUsed, AMin.Width, ANatural.Width));
			LResult.Width = LUsed;

			// Figure the vertical component
			LBelow = LWorking.Bottom - LResult.Bottom;
			LAbove = LResult.Top - LWorking.Top;
			if ((LBelow < ANatural.Height) && (LAbove > ANatural.Height))
			{
				// Arrange above control
				LUsed = LAbove;
				LResult.Y = LWorking.Top + Math.Max(0, CalcOffset(ref LUsed, AMin.Height, ANatural.Height));
			}
			else
			{
				// Arrange below control
				LUsed = LBelow;
				LResult.Y += LResult.Height + Math.Min(0, CalcOffset(ref LUsed, AMin.Height, ANatural.Height));
			}
			LResult.Height = LUsed;

			return LResult;
		}
	}
}
