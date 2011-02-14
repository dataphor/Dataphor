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
		public const int SpacerHeight = 4;

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

			Size = _textBox.Size + (DisplayRectangle.Size - Size);

			ResumeLayout(false);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				DisposeScrollTimer();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		#region SmallIncrement

		protected bool ShouldSerializeSmallIncrement()
		{
			return _smallIncrement != 1m;
		}
		
		private decimal _smallIncrement = 1m;
		/// <summary> Gets or sets the Small Increment value for the Arrow Keys, Up-Down Buttons, and Scroll Feature.</summary>
		public decimal SmallIncrement
		{
			get { return _smallIncrement; }
			set { _smallIncrement = Math.Max(1, value);	}
		}

		#endregion

		#region LargeIncrement

		protected bool ShouldSerializeLargeIncrement()
		{
			return _largeIncrement != 10m;
		}

		private decimal _largeIncrement = 10m;
		/// <summary> Gets or sets the Large Increment value for the Arrow Keys, Up-Down Buttons, and Scrolling Feature.</summary>
		public decimal LargeIncrement
		{
			get { return _largeIncrement; }
			set	{ _largeIncrement = Math.Max(1, value);	}
		}

		#endregion

		#region MinimumValue

		protected bool ShouldSerializeMinimumValue()
		{
			return _minimumValue != System.Decimal.MinValue;
		}

		private decimal _minimumValue = System.Decimal.MinValue;
		/// <summary> Gets or sets the Minimum value allowed in the Numeric Scroll Box.</summary>
		public decimal MinimumValue
		{
			get	{ return _minimumValue;	}
			set	{ _minimumValue = value; }
		}

		#endregion

		#region MaximumValue

		protected bool ShouldSerializeMaximumValue()
		{
			return _maximumValue != System.Decimal.MaxValue;
		}

		private decimal _maximumValue = System.Decimal.MaxValue;
		/// <summary> Gets or sets the MaximumValue allowed for the Numeric Scroll Box.</summary>
		public decimal MaximumValue
		{
			get	{ return _maximumValue;	}
			set	{ _maximumValue =  value; }
		}

		#endregion

		#region TextBox

		private DBNumericTextBox _textBox;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public DBNumericTextBox TextBox
		{
			get { return _textBox; }
		}

		private void InitializeTextBox()
		{
			_textBox = new DBNumericTextBox();
			_textBox.Parent = this;
			_textBox.Location = Point.Empty;
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			if (_textBox != null)
				_textBox.Focus();
		}

		#endregion

		#region ScrollBox

		private Panel _scrollBox;

		private void InitializeScrollBox()
		{
			_scrollBox = new System.Windows.Forms.Panel();
			_scrollBox.Parent = this;
			_scrollBox.BackColor = SystemColors.ControlDark;
			_scrollBox.MouseDown += new MouseEventHandler(DoScrollMouseDown);
			_scrollBox.MouseUp += new MouseEventHandler(DoScrollMouseUp);
			_scrollBox.MouseMove += new MouseEventHandler(DoScrollMouseMove);
			_scrollBox.Cursor = Cursors.HSplit;
		}
		
		private Point _mouseDownLocation;

		/// <summary> Gets initial cursor coordinates for mouse movement calculation.</summary>
		private void DoScrollMouseDown(object sender, MouseEventArgs args)
		{
			_mouseDownLocation = new Point(args.X, args.Y);
			_scrollBox.Capture = true;
		}

		/// <summary> Resets the initial point to empty.</summary>
		protected virtual void DoScrollMouseUp(object sender, MouseEventArgs args)
		{
			_scrollBox.Capture = false;
		}

		/// <summary> Calls Increment or Decrement based difference between mouse movement and initial cursor coordinates.</summary>
		protected virtual void DoScrollMouseMove(object sender, MouseEventArgs args)
		{
			if (_scrollBox.Capture)
			{
				decimal deltaY = _mouseDownLocation.Y - args.Y;
				if (deltaY != 0)
				{
					deltaY *= (Control.ModifierKeys == Keys.Control ? _largeIncrement : _smallIncrement);
					Increment(deltaY);
					Cursor.Position = _scrollBox.PointToScreen(_mouseDownLocation);	// keep the mouse at it original location
				}
			}
		}

		#endregion

		#region Up Button

		private Button _upButton;

		private void InitializeUpButton()
		{
			_upButton = new SpeedButton();
			_upButton.BackColor = SystemColors.Control;
			_upButton.Parent = _scrollBox;
			_upButton.Cursor = Cursors.Arrow;
			_upButton.MouseDown += new MouseEventHandler(DoUpButtonMouseDown);
			_upButton.MouseUp += new MouseEventHandler(DoUpButtonMouseUp);
			_upButton.Image = SpeedButton.ResourceBitmap(typeof(NumericScrollBox), "Alphora.Dataphor.DAE.Client.Controls.Images.UpButton.bmp");
			_upButton.Width = _upButton.Image.Width + 8;
		}

		private void DoUpButtonMouseDown(object sender, System.Windows.Forms.MouseEventArgs args)
		{
			Increment((Control.ModifierKeys != Keys.Control) ? _smallIncrement : _largeIncrement);
			_upButton.Capture = true;
			StartTimer();
		}

		private void DoUpButtonMouseUp(object sender, System.Windows.Forms.MouseEventArgs args)
		{
			_upButton.Capture = false;
			StopTimer();
		}
		
		#endregion

		#region Down Button

		private Button _downButton;

		private void InitializeDownButton()
		{
			_downButton = new SpeedButton();
			_downButton.BackColor = SystemColors.Control;
			_downButton.Parent = _scrollBox;
			_downButton.Cursor = Cursors.Arrow;
			_downButton.MouseDown += new MouseEventHandler(DoDownButtonMouseDown);
			_downButton.MouseUp += new MouseEventHandler(DoDownButtonMouseUp);	
			_downButton.Image = SpeedButton.ResourceBitmap(typeof(NumericScrollBox), "Alphora.Dataphor.DAE.Client.Controls.Images.DownButton.bmp");
			_downButton.Width = _downButton.Image.Width + 8;
		}

		/// <summary> DownButtonMouseDown event calls Increment or Decrement method and starts timer.</summary>
		protected virtual void DoDownButtonMouseDown(object sender, System.Windows.Forms.MouseEventArgs args)
		{
			Increment(-((Control.ModifierKeys != Keys.Control) ? _smallIncrement : _largeIncrement));
			_downButton.Capture = true;
			StartTimer();
		}

		/// <summary> DownButtonMouseUp event stops timer, and resets timer interval to it's initial value.</summary>
		protected virtual void DoDownButtonMouseUp(object sender, System.Windows.Forms.MouseEventArgs args)
		{
			_downButton.Capture = false;
			StopTimer();
		}

		#endregion

		#region Scroll Timer

		private Timer _scrollTimer;

		private void InitializeScrollTimer()
		{
			_scrollTimer = new Timer();
			_scrollTimer.Tick += new EventHandler(DoTimerTick);
		}

		private void DisposeScrollTimer()
		{
			if (_scrollTimer != null)
			{
				_scrollTimer.Tick -= new EventHandler(DoTimerTick);
				_scrollTimer.Dispose();
				_scrollTimer = null;
			}
		}

		private void StartTimer()
		{
			_scrollTimer.Interval = 200;
			_scrollTimer.Start();
		}

		private void StopTimer()
		{
			_scrollTimer.Stop();
		}

		/// <summary> Timer event that continues as long as the up or down button is held down.</summary>
		protected virtual void DoTimerTick(object sender, System.EventArgs args)
		{
			decimal delta = ((Control.ModifierKeys != Keys.Control) ? _smallIncrement : _largeIncrement);
			if (_downButton.Capture)
				delta *= -1;
			Increment(delta);
			_scrollTimer.Interval = Math.Max(20, _scrollTimer.Interval - 4);
		}
		
		#endregion

		#region Layout

		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);
			Size extent = _textBox.Size;
			int minHeight = _upButton.Image.Height + _downButton.Image.Height + 4 + SpacerHeight;
			if (extent.Height < minHeight)
				extent.Height = minHeight;
			_scrollBox.Bounds =
				new Rectangle
				(
					extent.Width,
					0,
					Math.Max(_upButton.Width, _downButton.Width),
					extent.Height
				);
			extent.Width += _scrollBox.Width;
			ClientSize = extent;
			int upDownButtonHeight = (extent.Height - SpacerHeight) / 2;
			_upButton.Bounds =
				new Rectangle
				(
					0,
					0,
					_scrollBox.Width,
					upDownButtonHeight
				);
			_downButton.Bounds =
				new Rectangle
				(
					0,
					extent.Height - upDownButtonHeight,
					_scrollBox.Width,
					upDownButtonHeight
				);
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				bounds.Width -= Math.Max(_upButton.Width, _downButton.Width);
				return bounds;
			}
		}

		#endregion

		#region Mouse

		/// <summary> On Mouse Wheel Event calls Increment or Decrement method.</summary>
		protected override void OnMouseWheel(MouseEventArgs args)
		{
			base.OnMouseWheel(args);
			decimal delta = ((Control.ModifierKeys != Keys.Control) ? _smallIncrement : _largeIncrement);
			if (args.Delta < 0)
				delta *= -1;
			Increment(delta);
		}
		
		#endregion

		#region Value & Increment

		/// <summary> Gets the value of Value, which is decimal representation of the contents of the NumericScrollBox.</summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		protected decimal Value
		{
			get	{ return (_textBox.Text != String.Empty) ? Convert.ToDecimal(_textBox.Text) : 0; }
		}

		/// <summary> Increment method increases numeric value by the amount indicated in the parameter.</summary>
		private void Increment(decimal value)
		{
			if (_textBox.Link.Active && _textBox.Link.Edit())
			{
				_textBox.Focus();
				if (!_textBox.ContainsFocus)	// Don't do anything if the textbox was unable to receive focus
					throw new AbortException();
				value += Value;
				if (value > _maximumValue)
					value = _maximumValue;
				if (value < _minimumValue)
					value = _minimumValue;
				_textBox.InternalSetText(Convert.ToString(value));
			}		
		}
		
		#endregion

		#region Keyboard handling

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
			{
				case System.Windows.Forms.Keys.Up : Increment(_smallIncrement); break;
				case System.Windows.Forms.Keys.PageUp : Increment(_largeIncrement); break;
				case System.Windows.Forms.Keys.Down : Increment(-_smallIncrement); break;
				case System.Windows.Forms.Keys.PageDown : Increment(-_largeIncrement); break;
				default : return base.ProcessDialogKey(key);
			}		
			return true;
		}

		#endregion
	}
}
