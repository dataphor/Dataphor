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
		public const char CPasswordChar = '*';

		// TextBoxControl

		protected DAE.Client.Controls.DBTextBox TextBoxControl
		{
			get { return (DAE.Client.Controls.DBTextBox)Control; }
		}
		
		private int FRowHeight;

		private void InternalUpdateTextBox()
		{
			DAE.Client.Controls.DBTextBox LControl = TextBoxControl;
			if (IsPassword)
				LControl.PasswordChar = CPasswordChar;
			else
				LControl.PasswordChar = (char)0;
			LControl.WordWrap = FWordWrap;
			LControl.Multiline = FHeight > 1;
			LControl.AcceptsReturn = !LControl.Multiline || FAcceptsReturn;
			LControl.AcceptsTab = !LControl.Multiline || FAcceptsTabs;
			LControl.NilIfBlank = FNilIfBlank;
			LControl.AutoUpdate = FAutoUpdate;
			LControl.AutoUpdateInterval = FAutoUpdateInterval;
			if (LControl.Multiline)
				if (FWordWrap)
					LControl.ScrollBars = WinForms.ScrollBars.Vertical;
				else
					LControl.ScrollBars = WinForms.ScrollBars.Both;
			else
				LControl.ScrollBars = WinForms.ScrollBars.None;
			if (FMaxLength >= 0)
				LControl.MaxLength = FMaxLength;
			else
				LControl.MaxLength = 32767;
		}
		
   		// IsPassword

		private bool FIsPassword;
		[DefaultValue(false)]
		[Description("When set to true a password masking character will be displayed in the textbox instead of actual characters.")]
		public bool IsPassword
		{
			get { return FIsPassword; }
			set
			{
				if (FIsPassword != value)
				{
					FIsPassword = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// WordWrap

		private bool FWordWrap;
		[DefaultValue(false)]
		[Description("When set to true, line breaks will be inserted into the entered text.  This is only cosmetic and will not affect the actual data.")]
		public bool WordWrap
		{
			get { return FWordWrap; }
			set
			{
				if (FWordWrap != value)
				{
					FWordWrap = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// Height

		private int FHeight = 1;
		[DefaultValue(1)]
		[Description("The height in lines of the text box.")]
		public int Height
		{
			get { return FHeight; }
			set
			{
				if (FHeight != value)
				{
					if (value < 1)
						throw new ClientException(ClientException.Codes.HeightMinimum);
					FHeight = value;
					if (Active)
					{
						InternalUpdateTextBox();
						UpdateLayout();
					}
				}
			}
		}
		
		// MaxLength
		
		private int FMaxLength = -1;
		[DefaultValue(-1)]
		[Description("The maximum number of characters that can be entered into the text box.  -1 = underlying control's default, 0 = underlying control's maximum, or a positive number up to the underlying control's maximum.")]
		public int MaxLength
		{
			get { return FMaxLength; }
			set
			{
				if (FMaxLength != value)
				{
					FMaxLength = value;
					if (Active)
					{
						InternalUpdateTextBox();
						UpdateLayout();
					}
				}
			}
		}

		// AcceptsReturn

		private bool FAcceptsReturn = true;
		[DefaultValue(true)]
		[Description("When true and the textbox height is > 1, the textbox will accept carrage return characters.")]
		public bool AcceptsReturn
		{
			get { return FAcceptsReturn; }
			set
			{
				if (FAcceptsReturn != value)
				{
					FAcceptsReturn = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// AcceptsTabs

		private bool FAcceptsTabs = false;
		[DefaultValue(false)]
		[Description("When true and the textbox height is > 1, the textbox will accept tab characters.")]
		public bool AcceptsTabs
		{
			get { return FAcceptsTabs; }
			set
			{
				if (FAcceptsTabs != value)
				{
					FAcceptsTabs = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		// NilIfBlank

		private bool FNilIfBlank = true;
		[DefaultValue(true)]
		[Description("When true, setting the contents of the control to '' (blank), will clear (set to nil) the data field.")]
		public bool NilIfBlank
		{
			get { return FNilIfBlank; }
			set 
			{ 
				if (FNilIfBlank != value)
				{
					FNilIfBlank = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private bool FAutoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				if (FAutoUpdate != value)
				{
					FAutoUpdate = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}  		   
		
		// AutoUpdateInterval
		
		private int FAutoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set
			{
				if (FAutoUpdateInterval != value)
				{
					FAutoUpdateInterval = value; 
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
			FRowHeight = Control.Font.Height + 1;
			InternalUpdateTextBox();
			base.InitializeControl();
		}

		// TitledElement

		protected override bool EnforceMaxHeight()
		{
			return (FHeight == 1);
		}

		protected override int GetControlNaturalHeight()
		{
			// Can't seem to rely on the control's DisplayRectangle to always include the scrollbar size so we must manually accomidate
			int LResult = (SystemInformation.Border3DSize.Height * 2) + (FRowHeight * FHeight);
			if ((TextBoxControl.ScrollBars & ScrollBars.Horizontal) != 0)
				LResult += SystemInformation.HorizontalScrollBarHeight;
			return LResult;
		}
		
		protected override int GetControlMinHeight()
		{
			int LResult = (SystemInformation.Border3DSize.Height * 2) + FRowHeight;
			if ((TextBoxControl.ScrollBars & ScrollBars.Horizontal) != 0)
				LResult += SystemInformation.HorizontalScrollBarHeight;
			return LResult;
		}

		protected override int GetControlMaxHeight()
		{
			if (FHeight == 1)
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

		protected override void LayoutControl(Rectangle ABounds)
		{
			Control.Location = ABounds.Location;
			Control.Height = ABounds.Height;
			if (FHeight == 1)
			{
				// Only range the width if it is a single line textbox
				DAE.Client.Controls.Range LRange = new DAE.Client.Controls.Range(Math.Min(ABounds.Width, GetControlNaturalWidth()), ABounds.Width);
				Control.Width = Math.Min(LRange.Maximum, Math.Max(LRange.Minimum, Control.Width));
				((DAE.Client.Controls.IWidthRange)Control).WidthRange.Range = LRange;
			}
			else
			{
				((DAE.Client.Controls.IWidthRange)Control).WidthRange.Range = new DAE.Client.Controls.Range(ABounds.Width, ABounds.Width);
				Control.Width = ABounds.Width;
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

		private int FSmallIncrement = 1;
		[DefaultValue(1)]
		[Description("The increment of the control when spinning slow.  Does not restrict possible values.")]
		public int SmallIncrement
		{
			get { return FSmallIncrement; }
			set
			{
				if (FSmallIncrement != value)
				{
					FSmallIncrement = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// LargeIncrement

		private int FLargeIncrement = 10;
		[DefaultValue(10)]
		[Description("The increment of the control when spinning fast.  Does not restrict possible values.")]
		public int LargeIncrement
		{
			get { return FLargeIncrement; }
			set
			{
				if (FLargeIncrement != value)
				{
					FLargeIncrement = value;
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

		// MinimumValue

		private decimal FMinimumValue = Decimal.MinValue;
		[DefaultValueMember("DefaultMinimumValue")]
		public decimal MinimumValue
		{
			get { return FMinimumValue; }
			set 
			{ 
				if (FMinimumValue != value)
				{
					FMinimumValue = value;
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

		private decimal FMaximumValue = Decimal.MaxValue;
		[DefaultValueMember("DefaultMaximumValue")]
		public decimal MaximumValue
		{
			get { return FMaximumValue; }
			set 
			{ 
				if (FMaximumValue != value)
				{
					FMaximumValue = value;
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

		private bool FNilIfBlank = true;
		[DefaultValue(true)]
		[Description("When true, setting the contents of the control to '' (blank), will clear (set to nil) the data field.")]
		public bool NilIfBlank
		{
			get { return FNilIfBlank; }
			set 
			{ 
				if (FNilIfBlank != value)
				{
					FNilIfBlank = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private bool FAutoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				if (FAutoUpdate != value)
				{
					FAutoUpdate = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
			   		
		// AutoUpdateInterval
		
		private int FAutoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set
			{
				if (FAutoUpdateInterval != value)
				{
					FAutoUpdateInterval = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}		   

		private void InternalUpdateTextBox()
		{
			DAE.Client.Controls.NumericScrollBox LControl = NumericControl;
			LControl.SmallIncrement = FSmallIncrement;
			LControl.LargeIncrement = FLargeIncrement;
			LControl.MinimumValue = FMinimumValue;
			LControl.MaximumValue = FMaximumValue;
			LControl.TextBox.NilIfBlank = FNilIfBlank;
			LControl.TextBox.AutoUpdate = FAutoUpdate;
			LControl.TextBox.AutoUpdateInterval = FAutoUpdateInterval;
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
			NumericControl.TextBox.Enter += new EventHandler(ControlGotFocus); 			
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

		protected override void LayoutControl(Rectangle ABounds)
		{
			Control.Location = ABounds.Location;
			int LTextBoxWidth = ABounds.Width - (NumericControl.Width - NumericControl.DisplayRectangle.Width);
			DAE.Client.Controls.Range LRange = new DAE.Client.Controls.Range(Math.Min(LTextBoxWidth, base.GetControlNaturalWidth()), LTextBoxWidth);
			NumericControl.TextBox.WidthRange.Range = LRange;
			if (NumericControl.TextBox.Width > LRange.Maximum)
				NumericControl.TextBox.Width = LRange.Maximum;
			if (NumericControl.TextBox.Width < LRange.Minimum)
				NumericControl.TextBox.Width = LRange.Minimum;
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

		private bool FNilIfBlank = true;
		[DefaultValue(true)]
		[Description("When true, setting the contents of the control to '' (blank), will clear (set to nil) the data field.")]
		public bool NilIfBlank
		{
			get { return FNilIfBlank; }
			set 
			{ 
				if (FNilIfBlank != value)
				{
					FNilIfBlank = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
		
		private bool FAutoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				if (FAutoUpdate != value)
				{
					FAutoUpdate = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}
			   		
		// AutoUpdateInterval
		
		private int FAutoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set
			{
				if (FAutoUpdateInterval != value)
				{
					FAutoUpdateInterval = value; 
					if (Active)
						InternalUpdateTextBox();
				}
			}
		}

        private bool FHideDate = false;
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
                    return FHideDate;
                return DateLookupControl.TextBox.HideDate;
            }
            set
            {
                if (DateLookupControl == null)
                    FHideDate= value;
                else
                    DateLookupControl.TextBox.HideDate = value;
            }
        }

        private bool FHideTime = true;
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
                    return FHideTime;
                return DateLookupControl.TextBox.HideTime; 
            }
            set 
            {
                if (DateLookupControl == null)
                    FHideTime = value;
                else
                    DateLookupControl.TextBox.HideTime = value; 
            }
        }
		
		protected virtual void InternalUpdateTextBox()
		{
			DateLookupControl.TextBox.NilIfBlank = FNilIfBlank;
			DateLookupControl.TextBox.AutoUpdate = FAutoUpdate;
			DateLookupControl.TextBox.AutoUpdateInterval = FAutoUpdateInterval;
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
            DAE.Client.Controls.DateLookup LDateLookup = new DAE.Client.Controls.DateLookup();
            LDateLookup.TextBox.HideDate = FHideDate;
            LDateLookup.TextBox.HideTime = FHideTime;
            return LDateLookup;
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

		protected override void LayoutControl(Rectangle ABounds)
		{
			Control.Location = ABounds.Location;
			int LTextBoxWidth = ABounds.Width - (Control.Width - Control.DisplayRectangle.Width);
			DAE.Client.Controls.Range LRange = new DAE.Client.Controls.Range(Math.Min(LTextBoxWidth, base.GetControlNaturalWidth()), LTextBoxWidth);
			DateLookupControl.TextBox.WidthRange.Range = LRange;
			if (DateLookupControl.TextBox.Width > LRange.Maximum)
				DateLookupControl.TextBox.Width = LRange.Maximum;
			if (DateLookupControl.TextBox.Width < LRange.Minimum)
				DateLookupControl.TextBox.Width = LRange.Minimum;
		}

		// DataElement

		protected override void InternalUpdateSource()
		{
			DateLookupControl.TextBox.Source = (Source == null ? null : Source.DataSource);
		}
	}
}
