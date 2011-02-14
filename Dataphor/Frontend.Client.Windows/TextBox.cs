/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;
using WinForms = System.Windows.Forms;
using System.Windows.Forms;
using System.Drawing.Design;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Frontend.Client;


namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.TextBox')")]
	[DesignerCategory("Data Controls")]
	public class TextBox : AlignedElement, ITextBox
	{
		public const char PasswordChar = '*';

		// TextBoxControl

		protected DAE.Client.Controls.DBTextBox TextBoxControl
		{
			get { return (DAE.Client.Controls.DBTextBox)Control; }
		}
		
		private int _rowHeight;

		private void InternalUpdateTextBox()
		{
			DAE.Client.Controls.DBTextBox control = TextBoxControl;
			if (IsPassword)
				control.PasswordChar = PasswordChar;
			else
				control.PasswordChar = (char)0;
			control.WordWrap = _wordWrap;
			control.Multiline = _height > 1;
			control.AcceptsReturn = !control.Multiline || _acceptsReturn;
			control.AcceptsTab = !control.Multiline || _acceptsTabs;
			control.NilIfBlank = _nilIfBlank;
			control.AutoUpdate = _autoUpdate;
			control.AutoUpdateInterval = _autoUpdateInterval;
			if (control.Multiline)
				if (_wordWrap)
					control.ScrollBars = WinForms.ScrollBars.Vertical;
				else
					control.ScrollBars = WinForms.ScrollBars.Both;
			else
				control.ScrollBars = WinForms.ScrollBars.None;
			if (_maxLength >= 0)
				control.MaxLength = _maxLength;
			else
				control.MaxLength = 32767;
		}
		
   		// IsPassword

		private bool _isPassword;
		[DefaultValue(false)]
		[Description("When set to true a password masking character will be displayed in the textbox instead of actual characters.")]
		public bool IsPassword
		{
			get { return _isPassword; }
			set
			{
				if (_isPassword != value)
				{
					_isPassword = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// WordWrap

		private bool _wordWrap;
		[DefaultValue(false)]
		[Description("When set to true, line breaks will be inserted into the entered text.  This is only cosmetic and will not affect the actual data.")]
		public bool WordWrap
		{
			get { return _wordWrap; }
			set
			{
				if (_wordWrap != value)
				{
					_wordWrap = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// Height

		private int _height = 1;
		[DefaultValue(1)]
		[Description("The height in lines of the text box.")]
		public int Height
		{
			get { return _height; }
			set
			{
				if (_height != value)
				{
					if (value < 1)
						throw new ClientException(ClientException.Codes.HeightMinimum);
					_height = value;
					if (Active)
					{
						InternalUpdateTextBox();
						UpdateLayout();
					}
				}
			}
		}
		
		// MaxLength
		
		private int _maxLength = -1;
		[DefaultValue(-1)]
		[Description("The maximum number of characters that can be entered into the text box.  -1 = underlying control's default, 0 = underlying control's maximum, or a positive number up to the underlying control's maximum.")]
		public int MaxLength
		{
			get { return _maxLength; }
			set
			{
				if (_maxLength != value)
				{
					_maxLength = value;
					if (Active)
					{
						InternalUpdateTextBox();
						UpdateLayout();
					}
				}
			}
		}

		// AcceptsReturn

		private bool _acceptsReturn = true;
		[DefaultValue(true)]
		[Description("When true and the textbox height is > 1, the textbox will accept carrage return characters.")]
		public bool AcceptsReturn
		{
			get { return _acceptsReturn; }
			set
			{
				if (_acceptsReturn != value)
				{
					_acceptsReturn = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// AcceptsTabs

		private bool _acceptsTabs = false;
		[DefaultValue(false)]
		[Description("When true and the textbox height is > 1, the textbox will accept tab characters.")]
		public bool AcceptsTabs
		{
			get { return _acceptsTabs; }
			set
			{
				if (_acceptsTabs != value)
				{
					_acceptsTabs = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		// NilIfBlank

		private bool _nilIfBlank = true;
		[DefaultValue(true)]
		[Description("When true, setting the contents of the control to '' (blank), will clear (set to nil) the data field.")]
		public bool NilIfBlank
		{
			get { return _nilIfBlank; }
			set 
			{ 
				if (_nilIfBlank != value)
				{
					_nilIfBlank = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private bool _autoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				if (_autoUpdate != value)
				{
					_autoUpdate = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}  		   
		
		// AutoUpdateInterval
		
		private int _autoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				if (_autoUpdateInterval != value)
				{
					_autoUpdateInterval = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}  		
		
		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBTextBox();
		}

		protected override void InitializeControl()
		{
			TextBoxControl.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			TextBoxControl.NoValueReadOnlyBackColor = ((Session)HostNode.Session).Theme.NoValueReadOnlyBackColor;
			_rowHeight = Control.Font.Height + 1;
			InternalUpdateTextBox();
			base.InitializeControl();
		}

		// TitledElement

		protected override bool EnforceMaxHeight()
		{
			return (_height == 1);
		}

		protected override int GetControlNaturalHeight()
		{
			// Can't seem to rely on the control's DisplayRectangle to always include the scrollbar size so we must manually accomidate
			int result = (SystemInformation.Border3DSize.Height * 2) + (_rowHeight * _height);
			if ((TextBoxControl.ScrollBars & ScrollBars.Horizontal) != 0)
				result += SystemInformation.HorizontalScrollBarHeight;
			return result;
		}
		
		protected override int GetControlMinHeight()
		{
			int result = (SystemInformation.Border3DSize.Height * 2) + _rowHeight;
			if ((TextBoxControl.ScrollBars & ScrollBars.Horizontal) != 0)
				result += SystemInformation.HorizontalScrollBarHeight;
			return result;
		}

		protected override int GetControlMaxHeight()
		{
			if (_height == 1)
				return base.GetControlMaxHeight();
			else
				return WinForms.Screen.FromControl(Control).WorkingArea.Height;
		}

		// AlignedElement

		protected override void InternalUpdateTextAlignment()
		{
			TextBoxControl.TextAlign = (WinForms.HorizontalAlignment)TextAlignment;
		}

		// ControlElement

		protected override void LayoutControl(Rectangle bounds)
		{
			Control.Location = bounds.Location;
			Control.Height = bounds.Height;
			if (_height == 1)
			{
				// Only range the width if it is a single line textbox
				DAE.Client.Controls.Range range = new DAE.Client.Controls.Range(Math.Min(bounds.Width, GetControlNaturalWidth()), bounds.Width);
				Control.Width = Math.Min(range.Maximum, Math.Max(range.Minimum, Control.Width));
				((DAE.Client.Controls.IWidthRange)Control).WidthRange.Range = range;
			}
			else
			{
				((DAE.Client.Controls.IWidthRange)Control).WidthRange.Range = new DAE.Client.Controls.Range(bounds.Width, bounds.Width);
				Control.Width = bounds.Width;
			}
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.NumericTextBox')")]
	[DesignerCategory("Data Controls")]
	public class NumericTextBox : AlignedElement, INumericTextBox
	{
		// NumericControl

		protected DAE.Client.Controls.NumericScrollBox NumericControl
		{
			get { return (DAE.Client.Controls.NumericScrollBox)Control; }
		}

		// SmallIncrement

		private int _smallIncrement = 1;
		[DefaultValue(1)]
		[Description("The increment of the control when spinning slow.  Does not restrict possible values.")]
		public int SmallIncrement
		{
			get { return _smallIncrement; }
			set
			{
				if (_smallIncrement != value)
				{
					_smallIncrement = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// LargeIncrement

		private int _largeIncrement = 10;
		[DefaultValue(10)]
		[Description("The increment of the control when spinning fast.  Does not restrict possible values.")]
		public int LargeIncrement
		{
			get { return _largeIncrement; }
			set
			{
				if (_largeIncrement != value)
				{
					_largeIncrement = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// MinimumValue

		private decimal _minimumValue = Decimal.MinValue;
		[DefaultValueMember("DefaultMinimumValue")]
		public decimal MinimumValue
		{
			get { return _minimumValue; }
			set 
			{ 
				if (_minimumValue != value)
				{
					_minimumValue = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private decimal DefaultMinimumValue()
		{
			return Decimal.MinValue;
		}

		// MaximumValue

		private decimal _maximumValue = Decimal.MaxValue;
		[DefaultValueMember("DefaultMaximumValue")]
		public decimal MaximumValue
		{
			get { return _maximumValue; }
			set 
			{ 
				if (_maximumValue != value)
				{
					_maximumValue = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private decimal DefaultMaximumValue()
		{
			return Decimal.MaxValue;
		}

		// NilIfBlank

		private bool _nilIfBlank = true;
		[DefaultValue(true)]
		[Description("When true, setting the contents of the control to '' (blank), will clear (set to nil) the data field.")]
		public bool NilIfBlank
		{
			get { return _nilIfBlank; }
			set 
			{ 
				if (_nilIfBlank != value)
				{
					_nilIfBlank = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private bool _autoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				if (_autoUpdate != value)
				{
					_autoUpdate = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
			   		
		// AutoUpdateInterval
		
		private int _autoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				if (_autoUpdateInterval != value)
				{
					_autoUpdateInterval = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}		   

		private void InternalUpdateTextBox()
		{
			DAE.Client.Controls.NumericScrollBox control = NumericControl;
			control.SmallIncrement = _smallIncrement;
			control.LargeIncrement = _largeIncrement;
			control.MinimumValue = _minimumValue;
			control.MaximumValue = _maximumValue;
			control.TextBox.NilIfBlank = _nilIfBlank;
			control.TextBox.AutoUpdate = _autoUpdate;
			control.TextBox.AutoUpdateInterval = _autoUpdateInterval;
		}

		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.NumericScrollBox();
		}

		protected override void InitializeControl()
		{
			NumericControl.TextBox.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			NumericControl.TextBox.NoValueReadOnlyBackColor = ((Session)HostNode.Session).Theme.NoValueReadOnlyBackColor;
			InternalUpdateTextBox();
			base.InitializeControl();
		}

		// AlignedElement

		protected override void InternalUpdateTextAlignment()
		{
			NumericControl.TextBox.TextAlign = (WinForms.HorizontalAlignment)TextAlignment;
		}

		// TitledElement

		protected override int GetControlMinWidth()
		{
			return base.GetControlMinWidth() + (Control.Width - Control.DisplayRectangle.Width);
		}

		protected override int GetControlNaturalWidth()
		{
			return base.GetControlNaturalWidth() + (Control.Width - Control.DisplayRectangle.Width);
		}

		protected override int GetControlMaxWidth()
		{
			return base.GetControlMaxWidth() + (Control.Width - Control.DisplayRectangle.Width);
		}

		protected override bool EnforceMaxHeight()
		{
			return true;
		}
	
		// ControlElement

		protected override void InternalUpdateReadOnly()
		{
			NumericControl.TextBox.ReadOnly = GetReadOnly();
		}

		protected override void InternalUpdateColumnName()
		{
			NumericControl.TextBox.ColumnName = ColumnName;
			base.InternalUpdateColumnName();
		}

		protected override void InternalUpdateTabStop()
		{
			NumericControl.TextBox.TabStop = GetTabStop();
		}

		protected override void LayoutControl(Rectangle bounds)
		{
			Control.Location = bounds.Location;
			int textBoxWidth = bounds.Width - (NumericControl.Width - NumericControl.DisplayRectangle.Width);
			DAE.Client.Controls.Range range = new DAE.Client.Controls.Range(Math.Min(textBoxWidth, base.GetControlNaturalWidth()), textBoxWidth);
			NumericControl.TextBox.WidthRange.Range = range;
			if (NumericControl.TextBox.Width > range.Maximum)
				NumericControl.TextBox.Width = range.Maximum;
			if (NumericControl.TextBox.Width < range.Minimum)
				NumericControl.TextBox.Width = range.Minimum;
		}

		// DataElement

		protected override void InternalUpdateSource()
		{
			NumericControl.TextBox.Source = (Source == null ? null : Source.DataSource);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.DateTimeBox')")]
	[DesignerCategory("Data Controls")]
	public class DateTimeBox : AlignedElement, IDateTimeBox
	{
		// NilIfBlank

		private bool _nilIfBlank = true;
		[DefaultValue(true)]
		[Description("When true, setting the contents of the control to '' (blank), will clear (set to nil) the data field.")]
		public bool NilIfBlank
		{
			get { return _nilIfBlank; }
			set 
			{ 
				if (_nilIfBlank != value)
				{
					_nilIfBlank = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private bool _autoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				if (_autoUpdate != value)
				{
					_autoUpdate = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
			   		
		// AutoUpdateInterval
		
		private int _autoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				if (_autoUpdateInterval != value)
				{
					_autoUpdateInterval = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

        private bool _hideDate = false;
        /// <summary> Do not display the date part. </summary>
        /// <remarks> Will display an empty string if then time part is not displayed. </remarks>
        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Do not display the date part.")]
        public bool HideDate
        {
            get
            {
                if (DateLookupControl == null)
                    return _hideDate;
                return DateLookupControl.TextBox.HideDate;
            }
            set
            {
                if (DateLookupControl == null)
                    _hideDate= value;
                else
                    DateLookupControl.TextBox.HideDate = value;
            }
        }

        private bool _hideTime = true;
        /// <summary> Do not display the time part. </summary>
        /// <remarks> Will display an empty string if the date part is not displayed. </remarks>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Do not display the time part.")]
        public bool HideTime
        {
            get 
            {
                if (DateLookupControl == null)
                    return _hideTime;
                return DateLookupControl.TextBox.HideTime; 
            }
            set 
            {
                if (DateLookupControl == null)
                    _hideTime = value;
                else
                    DateLookupControl.TextBox.HideTime = value; 
            }
        }
		
		protected virtual void InternalUpdateTextBox()
		{
			DateLookupControl.TextBox.NilIfBlank = _nilIfBlank;
			DateLookupControl.TextBox.AutoUpdate = _autoUpdate;
			DateLookupControl.TextBox.AutoUpdateInterval = _autoUpdateInterval;
		}

		// LookupEditControl

		protected DAE.Client.Controls.DateLookup DateLookupControl
		{
			get { return (DAE.Client.Controls.DateLookup)Control; }
		}

		// AlignedElement

		protected override void InternalUpdateTextAlignment()
		{
			DateLookupControl.TextBox.TextAlign = (WinForms.HorizontalAlignment)TextAlignment;
		}

		// TitledElement

		protected override int GetControlMinWidth()
		{
			return base.GetControlMinWidth() + (Control.Width - Control.DisplayRectangle.Width);
		}

		protected override int GetControlNaturalWidth()
		{
			return base.GetControlNaturalWidth() + (Control.Width - Control.DisplayRectangle.Width);
		}

		protected override int GetControlMaxWidth()
		{
			return base.GetControlMaxWidth() + (Control.Width - Control.DisplayRectangle.Width);
		}

		protected override bool EnforceMaxHeight()
		{
			return true;
		}

		// ColumnElement

		protected override WinForms.Control CreateControl()
		{
            DAE.Client.Controls.DateLookup dateLookup = new DAE.Client.Controls.DateLookup();
            dateLookup.TextBox.HideDate = _hideDate;
            dateLookup.TextBox.HideTime = _hideTime;
            return dateLookup;
		}

		protected override void InitializeControl()
		{
			DateLookupControl.TextBox.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			DateLookupControl.TextBox.NoValueReadOnlyBackColor = ((Session)HostNode.Session).Theme.NoValueReadOnlyBackColor;
			InternalUpdateTextBox();
			base.InitializeControl();
		}

		protected override void InternalUpdateReadOnly()
		{
			DateLookupControl.TextBox.ReadOnly = GetReadOnly();
		}

		protected override void InternalUpdateColumnName()
		{
			DateLookupControl.TextBox.ColumnName = ColumnName;
			base.InternalUpdateColumnName();
		}

		protected override void InternalUpdateTabStop()
		{
			DateLookupControl.TextBox.TabStop = GetTabStop();
		}

		protected override void LayoutControl(Rectangle bounds)
		{
			Control.Location = bounds.Location;
			int textBoxWidth = bounds.Width - (Control.Width - Control.DisplayRectangle.Width);
			DAE.Client.Controls.Range range = new DAE.Client.Controls.Range(Math.Min(textBoxWidth, base.GetControlNaturalWidth()), textBoxWidth);
			DateLookupControl.TextBox.WidthRange.Range = range;
			if (DateLookupControl.TextBox.Width > range.Maximum)
				DateLookupControl.TextBox.Width = range.Maximum;
			if (DateLookupControl.TextBox.Width < range.Minimum)
				DateLookupControl.TextBox.Width = range.Minimum;
		}

		// DataElement

		protected override void InternalUpdateSource()
		{
			DateLookupControl.TextBox.Source = (Source == null ? null : Source.DataSource);
		}
	}
}
