/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Globalization;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Introduces increment-decrement capabilities using mouse events, or the keyboard. </summary>
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.NumericScrollBox),"Icons.DBNumericScrollBox.bmp")]
	public class NumericScrollBox : Control
	{
		public const int CSpacerHeight = 4;

		/// <summary> Initializes a new instance of a DBNumericScrollBox. </summary>
		public NumericScrollBox()
		{
			SuspendLayout();

			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.Selectable, true);
			TabStop = false;

			InitializeTextBox();
			InitializeScrollBox();
			InitializeUpButton();
			InitializeDownButton();
			InitializeScrollTimer();

			Size = FTextBox.Size + (DisplayRectangle.Size - Size);

			ResumeLayout(false);
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				DisposeScrollTimer();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		#region SmallIncrement

		protected bool ShouldSerializeSmallIncrement()
		{
			return FSmallIncrement != 1m;
		}
		
		private decimal FSmallIncrement = 1m;
		/// <summary> Gets or sets the Small Increment value for the Arrow Keys, Up-Down Buttons, and Scroll Feature.</summary>
		public decimal SmallIncrement
		{
			get { return FSmallIncrement; }
			set { FSmallIncrement = Math.Max(1, value);	}
		}

		#endregion

		#region LargeIncrement

		protected bool ShouldSerializeLargeIncrement()
		{
			return FLargeIncrement != 10m;
		}

		private decimal FLargeIncrement = 10m;
		/// <summary> Gets or sets the Large Increment value for the Arrow Keys, Up-Down Buttons, and Scrolling Feature.</summary>
		public decimal LargeIncrement
		{
			get { return FLargeIncrement; }
			set	{ FLargeIncrement = Math.Max(1, value);	}
		}

		#endregion

		#region MinimumValue

		protected bool ShouldSerializeMinimumValue()
		{
			return FMinimumValue != System.Decimal.MinValue;
		}

		private decimal FMinimumValue = System.Decimal.MinValue;
		/// <summary> Gets or sets the Minimum value allowed in the Numeric Scroll Box.</summary>
		public decimal MinimumValue
		{
			get	{ return FMinimumValue;	}
			set	{ FMinimumValue = value; }
		}

		#endregion

		#region MaximumValue

		protected bool ShouldSerializeMaximumValue()
		{
			return FMaximumValue != System.Decimal.MaxValue;
		}

		private decimal FMaximumValue = System.Decimal.MaxValue;
		/// <summary> Gets or sets the MaximumValue allowed for the Numeric Scroll Box.</summary>
		public decimal MaximumValue
		{
			get	{ return FMaximumValue;	}
			set	{ FMaximumValue =  value; }
		}

		#endregion

		#region TextBox

		private DBNumericTextBox FTextBox;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public DBNumericTextBox TextBox
		{
			get { return FTextBox; }
		}

		private void InitializeTextBox()
		{
			FTextBox = new DBNumericTextBox();
			FTextBox.Parent = this;
			FTextBox.Location = Point.Empty;
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			if (FTextBox != null)
				FTextBox.Focus();
		}

		#endregion

		#region ScrollBox

		private Panel FScrollBox;

		private void InitializeScrollBox()
		{
			FScrollBox = new System.Windows.Forms.Panel();
			FScrollBox.Parent = this;
			FScrollBox.BackColor = SystemColors.ControlDark;
			FScrollBox.MouseDown += new MouseEventHandler(DoScrollMouseDown);
			FScrollBox.MouseUp += new MouseEventHandler(DoScrollMouseUp);
			FScrollBox.MouseMove += new MouseEventHandler(DoScrollMouseMove);
			FScrollBox.Cursor = Cursors.HSplit;
		}
		
		private Point FMouseDownLocation;

		/// <summary> Gets initial cursor coordinates for mouse movement calculation.</summary>
		private void DoScrollMouseDown(object sender, MouseEventArgs AArgs)
		{
			FMouseDownLocation = new Point(AArgs.X, AArgs.Y);
			FScrollBox.Capture = true;
		}

		/// <summary> Resets the initial point to empty.</summary>
		protected virtual void DoScrollMouseUp(object sender, MouseEventArgs AArgs)
		{
			FScrollBox.Capture = false;
		}

		/// <summary> Calls Increment or Decrement based difference between mouse movement and initial cursor coordinates.</summary>
		protected virtual void DoScrollMouseMove(object sender, MouseEventArgs AArgs)
		{
			if (FScrollBox.Capture)
			{
				decimal LDeltaY = FMouseDownLocation.Y - AArgs.Y;
				if (LDeltaY != 0)
				{
					LDeltaY *= (Control.ModifierKeys == Keys.Control ? FLargeIncrement : FSmallIncrement);
					Increment(LDeltaY);
					Cursor.Position = FScrollBox.PointToScreen(FMouseDownLocation);	// keep the mouse at it original location
				}
			}
		}

		#endregion

		#region Up Button

		private Button FUpButton;

		private void InitializeUpButton()
		{
			FUpButton = new SpeedButton();
			FUpButton.BackColor = SystemColors.Control;
			FUpButton.Parent = FScrollBox;
			FUpButton.Cursor = Cursors.Arrow;
			FUpButton.MouseDown += new MouseEventHandler(DoUpButtonMouseDown);
			FUpButton.MouseUp += new MouseEventHandler(DoUpButtonMouseUp);
			FUpButton.Image = SpeedButton.ResourceBitmap(typeof(NumericScrollBox), "Alphora.Dataphor.DAE.Client.Controls.Images.UpButton.bmp");
			FUpButton.Width = FUpButton.Image.Width + 8;
		}

		private void DoUpButtonMouseDown(object sender, System.Windows.Forms.MouseEventArgs AArgs)
		{
			Increment((Control.ModifierKeys != Keys.Control) ? FSmallIncrement : FLargeIncrement);
			FUpButton.Capture = true;
			StartTimer();
		}

		private void DoUpButtonMouseUp(object sender, System.Windows.Forms.MouseEventArgs AArgs)
		{
			FUpButton.Capture = false;
			StopTimer();
		}
		
		#endregion

		#region Down Button

		private Button FDownButton;

		private void InitializeDownButton()
		{
			FDownButton = new SpeedButton();
			FDownButton.BackColor = SystemColors.Control;
			FDownButton.Parent = FScrollBox;
			FDownButton.Cursor = Cursors.Arrow;
			FDownButton.MouseDown += new MouseEventHandler(DoDownButtonMouseDown);
			FDownButton.MouseUp += new MouseEventHandler(DoDownButtonMouseUp);	
			FDownButton.Image = SpeedButton.ResourceBitmap(typeof(NumericScrollBox), "Alphora.Dataphor.DAE.Client.Controls.Images.DownButton.bmp");
			FDownButton.Width = FDownButton.Image.Width + 8;
		}

		/// <summary> DownButtonMouseDown event calls Increment or Decrement method and starts timer.</summary>
		protected virtual void DoDownButtonMouseDown(object sender, System.Windows.Forms.MouseEventArgs AArgs)
		{
			Increment(-((Control.ModifierKeys != Keys.Control) ? FSmallIncrement : FLargeIncrement));
			FDownButton.Capture = true;
			StartTimer();
		}

		/// <summary> DownButtonMouseUp event stops timer, and resets timer interval to it's initial value.</summary>
		protected virtual void DoDownButtonMouseUp(object sender, System.Windows.Forms.MouseEventArgs AArgs)
		{
			FDownButton.Capture = false;
			StopTimer();
		}

		#endregion

		#region Scroll Timer

		private Timer FScrollTimer;

		private void InitializeScrollTimer()
		{
			FScrollTimer = new Timer();
			FScrollTimer.Tick += new EventHandler(DoTimerTick);
		}

		private void DisposeScrollTimer()
		{
			if (FScrollTimer != null)
			{
				FScrollTimer.Tick -= new EventHandler(DoTimerTick);
				FScrollTimer.Dispose();
				FScrollTimer = null;
			}
		}

		private void StartTimer()
		{
			FScrollTimer.Interval = 200;
			FScrollTimer.Start();
		}

		private void StopTimer()
		{
			FScrollTimer.Stop();
		}

		/// <summary> Timer event that continues as long as the up or down button is held down.</summary>
		protected virtual void DoTimerTick(object sender, System.EventArgs AArgs)
		{
			decimal LDelta = ((Control.ModifierKeys != Keys.Control) ? FSmallIncrement : FLargeIncrement);
			if (FDownButton.Capture)
				LDelta *= -1;
			Increment(LDelta);
			FScrollTimer.Interval = Math.Max(20, FScrollTimer.Interval - 4);
		}
		
		#endregion

		#region Layout

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);
			Size LExtent = FTextBox.Size;
			int LMinHeight = FUpButton.Image.Height + FDownButton.Image.Height + 4 + CSpacerHeight;
			if (LExtent.Height < LMinHeight)
				LExtent.Height = LMinHeight;
			FScrollBox.Bounds =
				new Rectangle
				(
					LExtent.Width,
					0,
					Math.Max(FUpButton.Width, FDownButton.Width),
					LExtent.Height
				);
			LExtent.Width += FScrollBox.Width;
			ClientSize = LExtent;
			int LUpDownButtonHeight = (LExtent.Height - CSpacerHeight) / 2;
			FUpButton.Bounds =
				new Rectangle
				(
					0,
					0,
					FScrollBox.Width,
					LUpDownButtonHeight
				);
			FDownButton.Bounds =
				new Rectangle
				(
					0,
					LExtent.Height - LUpDownButtonHeight,
					FScrollBox.Width,
					LUpDownButtonHeight
				);
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				LBounds.Width -= Math.Max(FUpButton.Width, FDownButton.Width);
				return LBounds;
			}
		}

		#endregion

		#region Mouse

		/// <summary> On Mouse Wheel Event calls Increment or Decrement method.</summary>
		protected override void OnMouseWheel(MouseEventArgs AArgs)
		{
			base.OnMouseWheel(AArgs);
			decimal LDelta = ((Control.ModifierKeys != Keys.Control) ? FSmallIncrement : FLargeIncrement);
			if (AArgs.Delta < 0)
				LDelta *= -1;
			Increment(LDelta);
		}
		
		#endregion

		#region Value & Increment

		/// <summary> Gets the value of Value, which is decimal representation of the contents of the NumericScrollBox.</summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		protected decimal Value
		{
			get	{ return (FTextBox.Text != String.Empty) ? Convert.ToDecimal(FTextBox.Text) : 0; }
		}

		/// <summary> Increment method increases numeric value by the amount indicated in the parameter.</summary>
		private void Increment(decimal AValue)
		{
			if (FTextBox.Link.Active && FTextBox.Link.Edit())
			{
				FTextBox.Focus();
				if (!FTextBox.ContainsFocus)	// Don't do anything if the textbox was unable to receive focus
					throw new AbortException();
				AValue += Value;
				if (AValue > FMaximumValue)
					AValue = FMaximumValue;
				if (AValue < FMinimumValue)
					AValue = FMinimumValue;
				FTextBox.InternalSetText(Convert.ToString(AValue));
			}		
		}
		
		#endregion

		#region Keyboard handling

		protected override bool ProcessDialogKey(Keys AKey)
		{
			switch (AKey)
			{
				case System.Windows.Forms.Keys.Up : Increment(FSmallIncrement); break;
				case System.Windows.Forms.Keys.PageUp : Increment(FLargeIncrement); break;
				case System.Windows.Forms.Keys.Down : Increment(-FSmallIncrement); break;
				case System.Windows.Forms.Keys.PageDown : Increment(-FLargeIncrement); break;
				default : return base.ProcessDialogKey(AKey);
			}		
			return true;
		}

		#endregion
	}
}
