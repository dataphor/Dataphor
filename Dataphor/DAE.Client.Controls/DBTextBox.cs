/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Base class for data-aware TextBox controls. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBTextBox),"Icons.DBTextBox.bmp")]
	public class DBTextBox : ExtendedTextBox, IDataSourceReference, IColumnNameReference, IReadOnly
	{
		public DBTextBox() : base()
		{
			FLink = new FieldDataLink();
			FLink.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			FLink.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			FLink.OnSaveRequested += new DataLinkHandler(SaveRequested);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			FDisableWhenReadOnly = false;
			FAutoUpdateInterval = 200;
			FAutoUpdateTimer = new System.Windows.Forms.Timer();
			FAutoUpdateTimer.Interval = FAutoUpdateInterval;
			FAutoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			FAutoUpdateTimer.Enabled = false;
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			try
			{
				FAutoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
				FAutoUpdateTimer.Dispose();
				FAutoUpdateTimer = null;
			}
			finally
			{
				if (FLink != null)
				{
					FLink.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
					FLink.OnUpdateReadOnly -= new EventHandler(UpdateReadOnly);
					FLink.OnSaveRequested -= new DataLinkHandler(SaveRequested);
					FLink.OnFocusControl -= new DataLinkFieldHandler(FocusControl);
					FLink.Dispose();
					FLink = null;
				}
			}
		}

		private FieldDataLink FLink;
		/// <summary> Links this control to a DataField in a DataView. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		internal protected FieldDataLink Link
		{
			get { return FLink; }
		}

		/// <summary> Gets or sets a value indicating whether to allow the user to modify the Text. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		public new bool ReadOnly 
		{
			get { return FLink.ReadOnly; }
			set { FLink.ReadOnly = value; }
		}
		
		private System.Windows.Forms.Timer FAutoUpdateTimer;

		private bool FAutoUpdate;
		/// <summary> Determines if the control should automatically update the DataField's value on a given interval. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				if (FAutoUpdate != value)
					FAutoUpdate = value;
			}
		}
		
		private int FAutoUpdateInterval;
		/// <summary> Determines the amount of time to wait before updating a DataField's value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(200)]
		[Category("Behavior")]
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set
			{
				if (FAutoUpdateInterval != value)
					FAutoUpdateInterval = value;
			}
		}

		/// <summary> Gets or sets a value indicating the column in the DataView to link to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FLink.ColumnName; }
			set { FLink.ColumnName = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value; }
		}

		/// <summary> Gets the DataField this control links to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Browsable(false)]
		public DataField DataField
		{
			get { return FLink == null ? null : FLink.DataField; }
		}

		/// <summary> Determines how the DataFields value is updated and retrieved. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		/// <summary>Gets or sets the Data Field value.</summary>			
		protected virtual string FieldValue
		{
			get
			{  
				if ((DataField != null) && DataField.HasValue())
					return Focused ? DataField.AsString : DataField.AsDisplayString;
				else
					return String.Empty; 
			}
			set { DataField.AsString = value; }
		}

		private bool FDisableWhenReadOnly;
		/// <summary> Gets or sets a value indicating whether the control is disabled when it's read-only.</summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("When ReadOnly is true, disable the control?")]
		public bool DisableWhenReadOnly
		{
			get { return FDisableWhenReadOnly; }
			set
			{
				if (FDisableWhenReadOnly != value)
				{
					FDisableWhenReadOnly = value;
					base.Enabled = !(ReadOnly && FDisableWhenReadOnly);
					UpdateReadOnly(this, EventArgs.Empty);
				}
			}
		}
		
		private bool FNilIfBlank = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When true, a '' value entered is considered nil")]
		public bool NilIfBlank
		{
			get { return FNilIfBlank; }
			set { FNilIfBlank = value; }
		}
		
		[DefaultValue(false)]
		public new bool Enabled
		{
			get { return base.Enabled; }
			set { base.Enabled = value; }
		}

		private bool InternalGetReadOnly()
		{
			return FLink.ReadOnly || !FLink.Active;
		}

		private void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			if (!DesignMode)
			{
				base.ReadOnly = InternalGetReadOnly();
				if (FDisableWhenReadOnly)
					base.Enabled = !InternalGetReadOnly();
				InternalUpdateBackColor();
			}
		}

		private bool FHasValue;
		public override bool HasValue {	get { return FHasValue; } }
		private void SetHasValue(bool AValue)
		{
			FHasValue = AValue;
			UpdateBackColor();
		}

		/// <summary> Update the background color of the control if the link is active. </summary>
		protected override void InternalUpdateBackColor()
		{
			if (!FLink.Active)
				BackColor = ReadOnlyBackColor;
			else
				base.InternalUpdateBackColor();
		}

		private bool FRequestSaveOnChange;
		/// <summary> Gets or sets a value indicating whether the control should request a save on every change.</summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Save on every change?")]			
		public bool RequestSaveOnChange
		{
			get { return FRequestSaveOnChange; }
			set { FRequestSaveOnChange = value; }
		}
			
		/// <summary> Called when the AutoUpdateTimer has elapsed. </summary>
		/// <param name="ASender"> The object whose delegate is called. </param>
		/// <param name="AArgs"> An EventArgs that contains data related to this event. </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected virtual void AutoUpdateElapsed(object ASender, EventArgs AArgs)
		{
			FAutoUpdateTimer.Stop();
			FLink.SaveRequested();
		}
		
		private bool FSetting;
		protected override void OnTextChanged(EventArgs AArgs)
		{
			base.OnTextChanged(AArgs);
			SetHasValue((base.Text != String.Empty) || !NilIfBlank);
			if (!FSetting)
			{
				EnsureEdit();
				if (FAutoUpdate)
				{
					if (FAutoUpdateInterval <= 0)
						FLink.SaveRequested();
					else
					{
						FAutoUpdateTimer.Interval = FAutoUpdateInterval;
						FAutoUpdateTimer.Start();
					}
				}
			}
		}

		/// <summary> Directly sets the value of the text property without updating the DataField's value. </summary>
		/// <param name="AValue">The value to assign to the Text property. </param>
		internal protected void InternalSetText(string AValue)
		{
			FSetting = true;
			try
			{
				base.Text = AValue;
			}
			finally
			{
				FSetting = false;
			}
		}

		private void FieldChanged(DataLink ADataLink, DataSet ADataSet, DataField AField)
		{
			InternalSetText(FieldValue);
			SetHasValue((DataField != null) && DataField.HasValue());
		}

		private void SaveRequested(DataLink ALink, DataSet ADataSet)
		{
			if (FLink.DataField != null)
			{
				if (!HasValue)
					DataField.ClearValue();
				else
					FieldValue = Text;
			}
		}

		private void EnsureEdit()
		{
			if (!FLink.Edit())
				throw new ControlsException(ControlsException.Codes.InvalidViewState);
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text
		{
			get { return base.Text; }
			set
			{
				if (base.Text != value)
				{
					EnsureEdit();
					base.Text = value;
					FLink.SaveRequested();
				}
			}
		}

		protected void Reset()
		{
			FLink.Reset();
			SelectAll();
		}

		protected override bool IsInputKey(Keys AKeyData)
		{
			switch (AKeyData) 
			{
				case Keys.Escape:
					if (FLink.Modified)
						return true;
					break;
			}
			return base.IsInputKey(AKeyData);
		}

		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			switch (AArgs.KeyData)
			{
				case Keys.Escape:
					Reset();
					AArgs.Handled = true;
					break;
			}
			base.OnKeyDown(AArgs);
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			switch (AKey)
			{
				case Keys.Control | Keys.Back:
					if (HasValue && !ReadOnly)
					{
						EnsureEdit();
						InternalSetText(String.Empty);
						SetHasValue(false);
					}
					else
						System.Media.SystemSounds.Beep.Play();
					return true;
				case Keys.Control | Keys.Space:
					if (!ReadOnly)
					{
						EnsureEdit();
						InternalSetText(String.Empty);
						SetHasValue(true);
					}
					else
						System.Media.SystemSounds.Beep.Play();
					return true;
			}
			return base.ProcessDialogKey(AKey);
		}

		private void UpdateFieldValue()
		{
			if (!Disposing && !IsDisposed)
			{
				try
				{
					FLink.SaveRequested();
				}
				catch
				{	  					
					SelectAll();
					Focus();
					throw;
				}
				ResetCursor();
			}
		}

		protected virtual void FormatChanged()
		{
			//Use the display conveyor when focused.
			InternalSetText(FieldValue);
		}
		
		protected override void OnGotFocus(EventArgs AEventArgs)
		{
			base.OnGotFocus(AEventArgs);
			if (!FLink.Modified)
			{
				FormatChanged();
				// HACK: Sometimes the textbox text isn't selected
				SelectAll();	
			}
		}

		protected override void OnLeave(EventArgs AEventArgs)
		{
			if (!Disposing && !IsDisposed)
				UpdateFieldValue();
			base.OnLeave(AEventArgs);
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
			if (!Disposing && !IsDisposed && !FLink.Modified)
				FormatChanged();
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			if (AField == DataField)
				Focus();
		}
	}

	public delegate void InternalSetWidthEventHandler(object ASender, int ANewWidth, ref bool AHandled, EventArgs AArgs);
	public delegate void MeasureWidthEventHandler(object ASender, ref int ANewWidth, EventArgs AArgs);

	public interface IWidthRange
	{
		WidthRange WidthRange { get; }
		int Width { get; set; }
		event InternalSetWidthEventHandler OnInternalSetWidth;
	}

	public class RangeTypeConverter : TypeConverter
	{
		public RangeTypeConverter(){}

		public override object CreateInstance(ITypeDescriptorContext AContext, IDictionary APropertyValues)
		{
			return new Range((int)APropertyValues["Minimum"], (int)APropertyValues["Maximum"]);
		}

		public override bool GetCreateInstanceSupported(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override bool GetPropertiesSupported(ITypeDescriptorContext AContext)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties
			(
			ITypeDescriptorContext AContext,
			object AValue,
			Attribute[] AAttributes
			)
		{
			if (AValue is Range)
				return TypeDescriptor.GetProperties(AValue, AAttributes);
			return base.GetProperties(AContext, AValue, AAttributes);
		}

		public override bool CanConvertTo(ITypeDescriptorContext AContext, Type ADestinationType)
		{
			return ADestinationType == typeof(InstanceDescriptor) ? true : base.CanConvertTo(AContext, ADestinationType);
		}

		public override bool CanConvertFrom(ITypeDescriptorContext AContext, Type ASourceType)
		{
			return ASourceType == typeof(string) ? true : base.CanConvertFrom(AContext, ASourceType);
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(Range ARange)
		{
			ConstructorInfo LInfo;
			Type LType = ARange.GetType();
			LInfo = LType.GetConstructor
				(
				new Type[] 
						{
							typeof(int),
							typeof(int)
						}
				);
			return new InstanceDescriptor
				(
				LInfo,
				new object[]
						{
							ARange.Minimum,
							ARange.Maximum
						}
				);

		}

		public override object ConvertTo(ITypeDescriptorContext AContext, System.Globalization.CultureInfo ACulture, object AValue, Type ADestinationType)
		{
			if ((ADestinationType == typeof(string)) && (AValue is Range))
				return ((Range)AValue).Minimum.ToString() + ACulture.TextInfo.ListSeparator + ((Range)AValue).Maximum.ToString();
			if 
				(
				(AValue is Range) &&
				(ADestinationType == typeof(InstanceDescriptor))
				)
				return GetInstanceDescriptor((Range)AValue);	
			return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext AContext, CultureInfo AInfo, object AValue)
		{
			if (AValue is string)
			{
				string LValue = (string)AValue;
				string[] LMaxMin = LValue.Split(AInfo.TextInfo.ListSeparator.ToCharArray());
				if (LMaxMin.Length < 2)
					throw new Exception("String to Range conversion must be in the format Minimum " + AInfo.TextInfo.ListSeparator + "Maximum");
				return new Range(Convert.ToInt32(LMaxMin[0].Trim()), Convert.ToInt32(LMaxMin[1].Trim()));
			}
			if (AValue is Range)
				return new Range(((Range)AValue).Minimum, ((Range)AValue).Maximum);
			return base.ConvertFrom(AContext, AInfo, AValue);
		}
	}

	[Serializable]
	[System.Runtime.InteropServices.ComVisible(true)]
	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.RangeTypeConverter))]
	public struct Range
	{
		private int FMinimum, FMaximum;
		public Range(int AMinimum, int AMaximum)
		{
			FMinimum = AMinimum;
			FMaximum = AMaximum;
		}

		public int Minimum
		{
			get { return FMinimum; }
			set { FMaximum = value; }
		}

		public int Maximum
		{
			get { return FMaximum; }
			set { FMaximum = value; }
		}

		public override string ToString()
		{
			return "{Minimum=" + Minimum.ToString() + ",Maximum=" + Maximum.ToString() + "}";
		}

		[Browsable(false)]
		public int Length { get { return Math.Abs(Maximum - Minimum); } }

		public static bool operator ==(Range ALeft, Range ARight)
		{
			return (ALeft.Minimum == ARight.Minimum) && (ALeft.Maximum == ARight.Maximum);
		}

		public static bool operator !=(Range ALeft, Range ARight)
		{
			return (ALeft.Minimum != ARight.Minimum) || (ALeft.Maximum != ARight.Maximum);
		}

		public override bool Equals(object AObject)
		{
			return (AObject is Range) && (((Range)AObject).Minimum == Minimum) && (((Range)AObject).Maximum == Maximum);
		}

		public override int GetHashCode()
		{
			return Minimum ^ Maximum;
		}

		public bool Contains(Range ARange)
		{
			return (Minimum <= ARange.Minimum) && (Maximum >= ARange.Maximum);
		}

		public bool In(Range ARange)
		{
			return (ARange.Minimum <= Minimum) && (ARange.Maximum >= Maximum);
		}
	}

	[ToolboxItem(false)]
	public class WidthRange : Object
	{
		public WidthRange(Control AControl)
		{
			FControl = AControl;
			FSaveSize = FControl.Size;
			FRange = new Range(FControl.Width, FControl.Width);
			FControl.SizeChanged += new EventHandler(ControlSizeChanged);
			FControl.HandleCreated += new EventHandler(ControlHandleCreated);
		}

		private System.Windows.Forms.Control FControl;

		private bool ShouldSerializeRange() { return FRange.Length != 0; }

		private Range FRange;
		public Range Range
		{
			get { return FRange; }
			set
			{
				if (FRange != value)
				{
					FRange = value;
					RangeChanged();
				}
			}
		}

		public event EventHandler OnRangeChanged;

		protected virtual void RangeChanged()
		{
			UpdateWidth();
			if (OnRangeChanged != null)
				OnRangeChanged(this, EventArgs.Empty);
		}

		public event EventHandler OnUpdateWidth;

		public event InternalSetWidthEventHandler OnInternalSetWidth;

		protected virtual void InternalSetWidth(int ANewWidth, ref bool AHandled, EventArgs AArgs)
		{
			if (OnInternalSetWidth != null)
				OnInternalSetWidth(this, ANewWidth, ref AHandled, AArgs);
		}

		public event MeasureWidthEventHandler OnMeasureWidth;

		protected virtual void MeasureWidth(ref int ANewWidth)
		{
			if (OnMeasureWidth != null)
				OnMeasureWidth(this, ref ANewWidth, EventArgs.Empty);
		}

		protected bool FInUpdateWidth;
		public void UpdateWidth()
		{
			FInUpdateWidth = true;
			try
			{
				if
					(
					FControl.IsHandleCreated &&
					(FRange.Length != 0) &&
					(
					(FControl.Dock == DockStyle.None) ||
					(FControl.Dock == DockStyle.Left) ||
					(FControl.Dock == DockStyle.Right)
					)
					)
				{
					int LNewWidth = FControl.Width;
					MeasureWidth(ref LNewWidth);
					if (LNewWidth != FControl.Width)
					{
						bool LHandled = false;
						InternalSetWidth(LNewWidth, ref LHandled, EventArgs.Empty);
						//Only change width if not already handled
						if (!LHandled)
							FControl.Width = LNewWidth;
					}
				}
				if (OnUpdateWidth != null)
					OnUpdateWidth(this, EventArgs.Empty);
			}
			finally
			{
				FInUpdateWidth = false;
			}
		}

		private Size FSaveSize;
		private void ControlSizeChanged(object ASender, EventArgs AArgs)
		{
			if (!FInUpdateWidth)
			{
				int LNewMin = FRange.Minimum;
				int LNewMax = FRange.Maximum;
				if (FRange.Minimum == FSaveSize.Width)
					LNewMin = FControl.Width;
				if (FRange.Maximum == FSaveSize.Width)
					LNewMax = FControl.Width;
				Range = new Range(LNewMin, LNewMax);
			}
			FSaveSize = FControl.Size;
		}

		private void ControlHandleCreated(object ASender, EventArgs AArgs)
		{
			UpdateWidth();
		}

		public override string ToString()
		{
			return FRange.ToString();
		}
	}

	/// <summary> Introduces different background colors when the control has no value. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
	[ToolboxItem(false)]
	public abstract class ExtendedTextBox : TextBox, IWidthRange
	{
		/// <summary> Initializes an new instance of an ExtendedTextBox. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		public ExtendedTextBox() : base()
		{
			CausesValidation = false;
			FValueBackColor = BackColor;
			FReadOnlyBackColor = SystemColors.InactiveBorder;
			FWidthRange = new WidthRange(this);
			FWidthRange.OnUpdateWidth += new EventHandler(WidthRangeUpdate);
			FWidthRange.OnInternalSetWidth += new InternalSetWidthEventHandler(InternalSetWidth);
			FWidthRange.OnMeasureWidth += new MeasureWidthEventHandler(MeasureWidth);
		}

		/// <summary> Determines whether the TextBox has a value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Browsable(false)]
		public abstract bool HasValue { get; }

		private Color FNoValueBackColor = ControlColor.NoValueBackColor;
		/// <summary> Gets or sets the background color of the control when it holds no value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Category("Colors")]
		[Description("Background color of the control when it has no value.")]
		public Color NoValueBackColor
		{
			get { return FNoValueBackColor; }
			set
			{
				if (FNoValueBackColor != value)
				{
					FNoValueBackColor = value;
					UpdateBackColor();
				}
			}
		}

		private Color FNoValueReadOnlyBackColor = ControlColor.NoValueReadOnlyBackColor;
		/// <summary> Gets or sets the background color of the TextBox when it holds no value and it's value is ReadOnly. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Category("Colors")]
		[Description("Background color of the control when it has no value and ReadOnly.")]
		public Color NoValueReadOnlyBackColor
		{
			get { return FNoValueReadOnlyBackColor; }
			set
			{
				if (FNoValueReadOnlyBackColor != value)
				{
					FNoValueReadOnlyBackColor = value;
					UpdateBackColor();
				}
			}
		}

		protected override bool IsInputKey(Keys AKey)
		{
			if 
			(
				!Multiline
					&&
					(
						(AKey == Keys.Up) 
							|| (AKey == Keys.Down) 
							|| (AKey == Keys.PageUp)
							|| (AKey == Keys.PageDown)
							|| (AKey == (Keys.Control | Keys.Home))
							|| (AKey == (Keys.Control | Keys.End))
					)
			)
				return false;
			else
				return base.IsInputKey(AKey);
		}

		protected override bool ProcessCmdKey(ref Message AMessage, Keys AKey)
		{
			if (AKey == (Keys.Control | Keys.A))
			{
				SelectAll();
				return true;
			}
			else
				return base.ProcessCmdKey(ref AMessage, AKey);
		}

		private Color FValueBackColor;
		/// <summary> Gets the background color that is used when the TextBox holds a value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected Color ValueBackColor { get { return FValueBackColor; } }

		private Color FReadOnlyBackColor;
		protected Color ReadOnlyBackColor { get { return FReadOnlyBackColor; } }

		private bool FUpdatingBackColor;
		/// <summary> Determines if the BackColor is being updated by a change in the HasValue state. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected bool UpdatingBackColor { get { return FUpdatingBackColor; } }

		/// <summary> Changes the background color of the control based on the HasValue and ReadOnly states. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected virtual void InternalUpdateBackColor()
		{
			if (!DesignMode)
			{
				if (!HasValue)
				{
					if (!ReadOnly)
						BackColor = NoValueBackColor;
					else
						BackColor = NoValueReadOnlyBackColor;
				}
				else 
				{
					if (!ReadOnly)
						BackColor = FValueBackColor; //Default BackColor
					else
						BackColor = FReadOnlyBackColor;
				}
					
			}
		}

		/// <summary> Updates the background color based on the HasValue and ReadOnly states of the TextBox. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected void UpdateBackColor()
		{
			FUpdatingBackColor = true;
			try
			{
				InternalUpdateBackColor();
			}
			finally
			{
				FUpdatingBackColor = false;
			}
		}

		protected override void OnReadOnlyChanged(EventArgs AArgs)
		{
			base.OnReadOnlyChanged(AArgs);
			UpdateBackColor();
		}

		protected override void OnBackColorChanged(EventArgs AArgs)
		{
			base.OnBackColorChanged(AArgs);
			if (!UpdatingBackColor)
				FValueBackColor = BackColor;
		}

		//Width Range

		private WidthRange FWidthRange;
		[Category("Layout")]
		[DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
		public WidthRange WidthRange
		{
			get { return FWidthRange; }
		}

		private void MeasureWidth(object ASender, ref int ANewWidth, EventArgs AArgs)
		{
			StringFormat LFormat = StringFormat.GenericTypographic;
			LFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
			Size LSize;
			using (Graphics LGraphics = CreateGraphics())
			{
				LSize = Size.Round(LGraphics.MeasureString(Text + 'W', Font, FWidthRange.Range.Maximum, LFormat));
			}
			if (LSize.Width > Width)
				ANewWidth = LSize.Width <= FWidthRange.Range.Maximum ? LSize.Width : FWidthRange.Range.Maximum;
			else if (LSize.Width < Width)
				ANewWidth = LSize.Width >= FWidthRange.Range.Minimum ? LSize.Width : FWidthRange.Range.Minimum;
			else
				ANewWidth = Width;
		}

		protected override void OnTextChanged(EventArgs AArgs)
		{
			base.OnTextChanged(AArgs);
			FWidthRange.UpdateWidth();
		}

		private void WidthRangeUpdate(object ASender, EventArgs AArgs)
		{
			//Hack to fix TextBox control not scrolling ...
			//Ensures that as much of the text as possible shows in the client area.
			if (IsHandleCreated && (FWidthRange.Range.Length != 0))
			{
				int LSaveStart = SelectionStart;
				try
				{
					SelectionStart = 0;
				}
				finally
				{
					SelectionStart = LSaveStart;
				}
			}
		}

		public event InternalSetWidthEventHandler OnInternalSetWidth;
		protected virtual void InternalSetWidth
			(
			object ASender,
			int ANewWidth,
			ref bool AHandled,
			EventArgs AArgs
			)
		{
			AHandled = DesignMode;
			if (OnInternalSetWidth != null) 
				OnInternalSetWidth(this, ANewWidth, ref AHandled, AArgs);
		}

	}
}
