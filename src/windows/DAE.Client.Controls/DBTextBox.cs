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
			_link = new FieldDataLink();
			_link.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			_link.OnUpdateReadOnly += new EventHandler(UpdateReadOnly);
			_link.OnSaveRequested += new DataLinkHandler(SaveRequested);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			_disableWhenReadOnly = false;
			_autoUpdateInterval = 200;
			_autoUpdateTimer = new System.Windows.Forms.Timer();
			_autoUpdateTimer.Interval = _autoUpdateInterval;
			_autoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			_autoUpdateTimer.Enabled = false;
			UpdateReadOnly(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			try
			{
				_autoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
				_autoUpdateTimer.Dispose();
				_autoUpdateTimer = null;
			}
			finally
			{
				if (_link != null)
				{
					_link.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
					_link.OnUpdateReadOnly -= new EventHandler(UpdateReadOnly);
					_link.OnSaveRequested -= new DataLinkHandler(SaveRequested);
					_link.OnFocusControl -= new DataLinkFieldHandler(FocusControl);
					_link.Dispose();
					_link = null;
				}
			}
		}

		private FieldDataLink _link;
		/// <summary> Links this control to a DataField in a DataView. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		internal protected FieldDataLink Link
		{
			get { return _link; }
		}

		/// <summary> Gets or sets a value indicating whether to allow the user to modify the Text. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		public new bool ReadOnly 
		{
			get { return _link.ReadOnly; }
			set { _link.ReadOnly = value; }
		}
		
		private System.Windows.Forms.Timer _autoUpdateTimer;

		private bool _autoUpdate;
		/// <summary> Determines if the control should automatically update the DataField's value on a given interval. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				if (_autoUpdate != value)
					_autoUpdate = value;
			}
		}
		
		private int _autoUpdateInterval;
		/// <summary> Determines the amount of time to wait before updating a DataField's value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(200)]
		[Category("Behavior")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				if (_autoUpdateInterval != value)
					_autoUpdateInterval = value;
			}
		}

		/// <summary> Gets or sets a value indicating the column in the DataView to link to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue("")]
		[Category("Data")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set { _link.ColumnName = value; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value; }
		}

		/// <summary> Gets the DataField this control links to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Browsable(false)]
		public DataField DataField
		{
			get { return _link == null ? null : _link.DataField; }
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

		private bool _disableWhenReadOnly;
		/// <summary> Gets or sets a value indicating whether the control is disabled when it's read-only.</summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("When ReadOnly is true, disable the control?")]
		public bool DisableWhenReadOnly
		{
			get { return _disableWhenReadOnly; }
			set
			{
				if (_disableWhenReadOnly != value)
				{
					_disableWhenReadOnly = value;
					base.Enabled = !(ReadOnly && _disableWhenReadOnly);
					UpdateReadOnly(this, EventArgs.Empty);
				}
			}
		}
		
		private bool _nilIfBlank = true;
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("When true, a '' value entered is considered nil")]
		public bool NilIfBlank
		{
			get { return _nilIfBlank; }
			set { _nilIfBlank = value; }
		}
		
		[DefaultValue(false)]
		public new bool Enabled
		{
			get { return base.Enabled; }
			set { base.Enabled = value; }
		}

		private bool InternalGetReadOnly()
		{
			return _link.ReadOnly || !_link.Active;
		}

		private void UpdateReadOnly(object sender, EventArgs args)
		{
			if (!DesignMode)
			{
				base.ReadOnly = InternalGetReadOnly();
				if (_disableWhenReadOnly)
					base.Enabled = !InternalGetReadOnly();
				InternalUpdateBackColor();
			}
		}

		private bool _hasValue;
		public override bool HasValue {	get { return _hasValue; } }
		private void SetHasValue(bool value)
		{
			_hasValue = value;
			UpdateBackColor();
		}

		/// <summary> Update the background color of the control if the link is active. </summary>
		protected override void InternalUpdateBackColor()
		{
			if (!_link.Active)
				BackColor = ReadOnlyBackColor;
			else
				base.InternalUpdateBackColor();
		}

		private bool _requestSaveOnChange;
		/// <summary> Gets or sets a value indicating whether the control should request a save on every change.</summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Save on every change?")]			
		public bool RequestSaveOnChange
		{
			get { return _requestSaveOnChange; }
			set { _requestSaveOnChange = value; }
		}
			
		/// <summary> Called when the AutoUpdateTimer has elapsed. </summary>
		/// <param name="sender"> The object whose delegate is called. </param>
		/// <param name="args"> An EventArgs that contains data related to this event. </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected virtual void AutoUpdateElapsed(object sender, EventArgs args)
		{
			_autoUpdateTimer.Stop();
			_link.SaveRequested();
		}
		
		protected virtual void InternalAutoUpdate()
		{
			if (_autoUpdate)
			{
				if (_autoUpdateInterval <= 0)
					_link.SaveRequested();
				else
				{
					_autoUpdateTimer.Interval = _autoUpdateInterval;
					_autoUpdateTimer.Start();
				}
			};
		}
		
		private bool _setting;
		protected override void OnTextChanged(EventArgs args)
		{
			base.OnTextChanged(args);
			SetHasValue((base.Text != String.Empty) || !NilIfBlank);
			if (!_setting)
			{
				EnsureEdit();
				InternalAutoUpdate();
			}
		}

		/// <summary> Directly sets the value of the text property without updating the DataField's value. </summary>
		/// <param name="value">The value to assign to the Text property. </param>
		internal protected void InternalSetText(string value)
		{
			_setting = true;
			try
			{
				base.Text = value;
			}
			finally
			{
				_setting = false;
			}
		}

		private void FieldChanged(DataLink dataLink, DataSet dataSet, DataField field)
		{
			InternalSetText(FieldValue);
			SetHasValue((DataField != null) && DataField.HasValue());
		}

		private void SaveRequested(DataLink link, DataSet dataSet)
		{
			if (_link.DataField != null)
			{
				if (!HasValue)
					DataField.ClearValue();
				else
					FieldValue = Text;
			}
		}

		private void EnsureEdit()
		{
			if (!_link.Edit())
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
					_link.SaveRequested();
				}
			}
		}

		protected void Reset()
		{
			_link.Reset();
			SelectAll();
		}

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData) 
			{
				case Keys.Escape:
					if (_link.Modified)
						return true;
					break;

				case Keys.Control | Keys.Back:
				case Keys.Control | Keys.Space:
					return false;
			}
			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			switch (args.KeyData)
			{
				case Keys.Escape:
					Reset();
					args.Handled = true;
					break;
			}
			base.OnKeyDown(args);
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
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
			return base.ProcessDialogKey(key);
		}

		private void UpdateFieldValue()
		{
			if (!Disposing && !IsDisposed)
			{
				try
				{
					_link.SaveRequested();
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
		
		protected override void OnGotFocus(EventArgs eventArgs)
		{
			base.OnGotFocus(eventArgs);
			if (!_link.Modified)
			{
				FormatChanged();
				// HACK: Sometimes the textbox text isn't selected
				SelectAll();	
			}
		}

		protected override void OnLeave(EventArgs eventArgs)
		{
			if (!Disposing && !IsDisposed)
				UpdateFieldValue();
			base.OnLeave(eventArgs);
		}

		protected override void OnLostFocus(EventArgs args)
		{
			base.OnLostFocus(args);
			if (!Disposing && !IsDisposed && !_link.Modified)
				FormatChanged();
		}

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			if (field == DataField)
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

		public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
		{
			return new Range((int)propertyValues["Minimum"], (int)propertyValues["Maximum"]);
		}

		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties
			(
			ITypeDescriptorContext context,
			object value,
			Attribute[] attributes
			)
		{
			if (value is Range)
				return TypeDescriptor.GetProperties(value, attributes);
			return base.GetProperties(context, value, attributes);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(InstanceDescriptor) ? true : base.CanConvertTo(context, destinationType);
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(Range range)
		{
			ConstructorInfo info;
			Type type = range.GetType();
			info = type.GetConstructor
				(
				new Type[] 
						{
							typeof(int),
							typeof(int)
						}
				);
			return new InstanceDescriptor
				(
				info,
				new object[]
						{
							range.Minimum,
							range.Maximum
						}
				);

		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == typeof(string)) && (value is Range))
				return ((Range)value).Minimum.ToString() + culture.TextInfo.ListSeparator + ((Range)value).Maximum.ToString();
			if 
				(
				(value is Range) &&
				(destinationType == typeof(InstanceDescriptor))
				)
				return GetInstanceDescriptor((Range)value);	
			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo info, object value)
		{
			if (value is string)
			{
				string localValue = (string)value;
				string[] maxMin = localValue.Split(info.TextInfo.ListSeparator.ToCharArray());
				if (maxMin.Length < 2)
					throw new Exception("String to Range conversion must be in the format Minimum " + info.TextInfo.ListSeparator + "Maximum");
				return new Range(Convert.ToInt32(maxMin[0].Trim()), Convert.ToInt32(maxMin[1].Trim()));
			}
			if (value is Range)
				return new Range(((Range)value).Minimum, ((Range)value).Maximum);
			return base.ConvertFrom(context, info, value);
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
		public WidthRange(Control control)
		{
			_control = control;
			_saveSize = _control.Size;
			_range = new Range(_control.Width, _control.Width);
			_control.SizeChanged += new EventHandler(ControlSizeChanged);
			_control.HandleCreated += new EventHandler(ControlHandleCreated);
		}

		private System.Windows.Forms.Control _control;

		private bool ShouldSerializeRange() { return _range.Length != 0; }

		private Range _range;
		public Range Range
		{
			get { return _range; }
			set
			{
				if (_range != value)
				{
					_range = value;
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

		protected virtual void InternalSetWidth(int newWidth, ref bool handled, EventArgs args)
		{
			if (OnInternalSetWidth != null)
				OnInternalSetWidth(this, newWidth, ref handled, args);
		}

		public event MeasureWidthEventHandler OnMeasureWidth;

		protected virtual void MeasureWidth(ref int newWidth)
		{
			if (OnMeasureWidth != null)
				OnMeasureWidth(this, ref newWidth, EventArgs.Empty);
		}

		protected bool _inUpdateWidth;
		public void UpdateWidth()
		{
			_inUpdateWidth = true;
			try
			{
				if
					(
					_control.IsHandleCreated &&
					(_range.Length != 0) &&
					(
					(_control.Dock == DockStyle.None) ||
					(_control.Dock == DockStyle.Left) ||
					(_control.Dock == DockStyle.Right)
					)
					)
				{
					int newWidth = _control.Width;
					MeasureWidth(ref newWidth);
					if (newWidth != _control.Width)
					{
						bool handled = false;
						InternalSetWidth(newWidth, ref handled, EventArgs.Empty);
						//Only change width if not already handled
						if (!handled)
							_control.Width = newWidth;
					}
				}
				if (OnUpdateWidth != null)
					OnUpdateWidth(this, EventArgs.Empty);
			}
			finally
			{
				_inUpdateWidth = false;
			}
		}

		private Size _saveSize;
		private void ControlSizeChanged(object sender, EventArgs args)
		{
			if (!_inUpdateWidth)
			{
				int newMin = _range.Minimum;
				int newMax = _range.Maximum;
				if (_range.Minimum == _saveSize.Width)
					newMin = _control.Width;
				if (_range.Maximum == _saveSize.Width)
					newMax = _control.Width;
				Range = new Range(newMin, newMax);
			}
			_saveSize = _control.Size;
		}

		private void ControlHandleCreated(object sender, EventArgs args)
		{
			UpdateWidth();
		}

		public override string ToString()
		{
			return _range.ToString();
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
			_valueBackColor = BackColor;
			_readOnlyBackColor = SystemColors.InactiveBorder;
			_widthRange = new WidthRange(this);
			_widthRange.OnUpdateWidth += new EventHandler(WidthRangeUpdate);
			_widthRange.OnInternalSetWidth += new InternalSetWidthEventHandler(InternalSetWidth);
			_widthRange.OnMeasureWidth += new MeasureWidthEventHandler(MeasureWidth);
		}

		/// <summary> Determines whether the TextBox has a value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Browsable(false)]
		public abstract bool HasValue { get; }

		private Color _noValueBackColor = ControlColor.NoValueBackColor;
		/// <summary> Gets or sets the background color of the control when it holds no value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Category("Colors")]
		[Description("Background color of the control when it has no value.")]
		public Color NoValueBackColor
		{
			get { return _noValueBackColor; }
			set
			{
				if (_noValueBackColor != value)
				{
					_noValueBackColor = value;
					UpdateBackColor();
				}
			}
		}

		private Color _noValueReadOnlyBackColor = ControlColor.NoValueReadOnlyBackColor;
		/// <summary> Gets or sets the background color of the TextBox when it holds no value and it's value is ReadOnly. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		[Category("Colors")]
		[Description("Background color of the control when it has no value and ReadOnly.")]
		public Color NoValueReadOnlyBackColor
		{
			get { return _noValueReadOnlyBackColor; }
			set
			{
				if (_noValueReadOnlyBackColor != value)
				{
					_noValueReadOnlyBackColor = value;
					UpdateBackColor();
				}
			}
		}

		protected override bool IsInputKey(Keys key)
		{
			if 
			(
				!Multiline
					&&
					(
						(key == Keys.Up) 
							|| (key == Keys.Down) 
							|| (key == Keys.PageUp)
							|| (key == Keys.PageDown)
							|| (key == (Keys.Control | Keys.Home))
							|| (key == (Keys.Control | Keys.End))
					)
			)
				return false;
			else
				return base.IsInputKey(key);
		}

		protected override bool ProcessCmdKey(ref Message message, Keys key)
		{
			if (key == (Keys.Control | Keys.A))
			{
				SelectAll();
				return true;
			}
			else
				return base.ProcessCmdKey(ref message, key);
		}

		private Color _valueBackColor;
		/// <summary> Gets the background color that is used when the TextBox holds a value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected Color ValueBackColor { get { return _valueBackColor; } }

		private Color _readOnlyBackColor;
		protected Color ReadOnlyBackColor { get { return _readOnlyBackColor; } }

		private bool _updatingBackColor;
		/// <summary> Determines if the BackColor is being updated by a change in the HasValue state. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected bool UpdatingBackColor { get { return _updatingBackColor; } }

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
						BackColor = _valueBackColor; //Default BackColor
					else
						BackColor = _readOnlyBackColor;
				}
					
			}
		}

		/// <summary> Updates the background color based on the HasValue and ReadOnly states of the TextBox. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBTextBox.dxd"/>
		protected void UpdateBackColor()
		{
			_updatingBackColor = true;
			try
			{
				InternalUpdateBackColor();
			}
			finally
			{
				_updatingBackColor = false;
			}
		}

		protected override void OnReadOnlyChanged(EventArgs args)
		{
			base.OnReadOnlyChanged(args);
			UpdateBackColor();
		}

		protected override void OnBackColorChanged(EventArgs args)
		{
			base.OnBackColorChanged(args);
			if (!UpdatingBackColor)
				_valueBackColor = BackColor;
		}

		//Width Range

		private WidthRange _widthRange;
		[Category("Layout")]
		[DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
		public WidthRange WidthRange
		{
			get { return _widthRange; }
		}

		private void MeasureWidth(object sender, ref int newWidth, EventArgs args)
		{
			StringFormat format = StringFormat.GenericTypographic;
			format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
			Size size;
			using (Graphics graphics = CreateGraphics())
			{
				size = Size.Round(graphics.MeasureString(Text + 'W', Font, _widthRange.Range.Maximum, format));
			}
			if (size.Width > Width)
				newWidth = size.Width <= _widthRange.Range.Maximum ? size.Width : _widthRange.Range.Maximum;
			else if (size.Width < Width)
				newWidth = size.Width >= _widthRange.Range.Minimum ? size.Width : _widthRange.Range.Minimum;
			else
				newWidth = Width;
		}

		protected override void OnTextChanged(EventArgs args)
		{
			base.OnTextChanged(args);
			_widthRange.UpdateWidth();
		}

		private void WidthRangeUpdate(object sender, EventArgs args)
		{
			//Hack to fix TextBox control not scrolling ...
			//Ensures that as much of the text as possible shows in the client area.
			if (IsHandleCreated && (_widthRange.Range.Length != 0))
			{
				int saveStart = SelectionStart;
				try
				{
					SelectionStart = 0;
				}
				finally
				{
					SelectionStart = saveStart;
				}
			}
		}

		public event InternalSetWidthEventHandler OnInternalSetWidth;
		protected virtual void InternalSetWidth
			(
			object sender,
			int newWidth,
			ref bool handled,
			EventArgs args
			)
		{
			handled = DesignMode;
			if (OnInternalSetWidth != null) 
				OnInternalSetWidth(this, newWidth, ref handled, args);
		}

	}
}
