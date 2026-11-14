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

		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);
			}
			finally
			{
				DisposeButton();
			}
		}

		#region Button

		private BitmapButton _button;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BitmapButton Button 
		{ 
			get { return _button; } 
		}

		protected virtual void InitializeButton()
		{
			_button = new SpeedButton();
			_button.Image = SpeedButton.ResourceBitmap(typeof(LookupPanel), "Alphora.Dataphor.DAE.Client.Controls.Images.Lookup.bmp");
			_button.Parent = this;
			_button.Size = MinButtonSize();
			_button.Click += new EventHandler(LookupButtonClick);
		}

		protected virtual void DisposeButton()
		{
			if (_button != null)
			{
				_button.Click -= new EventHandler(LookupButtonClick);
				_button.Dispose();
				_button = null;
			}
		}

		private bool _inButtonClick;

		private void LookupButtonClick(object sender, EventArgs args)
		{
			_inButtonClick = true;
			try
			{
				if (FocusControl())		// Don't proceed unless we take focus (in case another control fails validation)
					PerformLookup();
			}
			finally
			{
				_inButtonClick = false;
			}
		}

		protected Size MinButtonSize()
		{
			return _button.Image.Size + new Size(5, 5);
		}

		#endregion

		#region Lookup

		public event LookupEventHandler Lookup;

		protected virtual void OnLookup(LookupEventArgs args)
		{
			if (Enabled && (Lookup != null))
				Lookup(this, args);
		}

		private void PerformLookup()
		{
			OnLookup(new LookupEventArgs());
		}

		#endregion

		#region AutoLookup

		private bool _autoLookup;
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AutoLookup
		{
			get { return _autoLookup; }
			set { _autoLookup = value; }
		}

		private bool _autoLookupPerformed = false;	// Only perform auto lookup once per instance

		/// <summary> Resets the auto-lookup feature, allowing auto-lookup to take place when focus is navigated to the control again. </summary>
		public void ResetAutoLookup()
		{
			_autoLookupPerformed = false;
		}

		protected override void OnEnter(EventArgs args)
		{
			base.OnEnter(args);
			if (_autoLookup && !_inButtonClick && !_autoLookupPerformed && OnQueryAutoLookupEnabled())
			{
				_autoLookupPerformed = true;
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

		private Keys _lookupKey = Keys.Insert;
		[Category("Behavior")]
		[DefaultValue(Keys.Insert)]
		[Description("Shortcut key to exec lookup.")]
		public virtual Keys LookupKey
		{
			get { return _lookupKey; }
			set { _lookupKey = value; }
		}

		protected virtual bool IsLookupKey(Keys key)
		{
			return (key == _lookupKey) || (key == (Keys.Alt | Keys.Down));
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			if (IsLookupKey(key))
			{
				OnLookup(new LookupEventArgs());
				return true;
			}
			else
				return base.ProcessDialogKey(key);
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

		public LookupEventArgs(Keys keyData)
		{
			_keyData = keyData;
		}

		private Keys _keyData = Keys.None;
		/// <summary> The key that invoked the lookup. </summary>
		public Keys KeyData
		{
			get { return _keyData; }
		}
	}

	public class LookupBoundsUtility
	{
		public static void ConstrainMin(ref int value, int min)
		{
			if (value < min)
				value = min;
		}

		public static void ConstrainMax(ref int value, int max)
		{
			if (value > max)
				value = max;
		}

		// Returns delta
		private static int CalcOffset(ref int used, int min, int max)
		{
			int original = used;
			if (used < min)
				used = min;
			if (used > max)
				used = max;
			return original - used;
		}

		public static Rectangle DetermineBounds(Size natural, Size min, Control lookupControl)
		{
			int above;
			int below;
			int used;

			Rectangle result = new Rectangle(lookupControl.PointToScreen(Point.Empty), lookupControl.Size);
			Rectangle working = Screen.FromRectangle(result).WorkingArea;

			// Figure the horizonal component
			used = working.Right - result.Left;
			result.X += Math.Min(0, CalcOffset(ref used, min.Width, natural.Width));
			result.Width = used;

			// Figure the vertical component
			below = working.Bottom - result.Bottom;
			above = result.Top - working.Top;
			if ((below < natural.Height) && (above > natural.Height))
			{
				// Arrange above control
				used = above;
				result.Y = working.Top + Math.Max(0, CalcOffset(ref used, min.Height, natural.Height));
			}
			else
			{
				// Arrange below control
				used = below;
				result.Y += result.Height + Math.Min(0, CalcOffset(ref used, min.Height, natural.Height));
			}
			result.Height = used;

			return result;
		}
	}
}
