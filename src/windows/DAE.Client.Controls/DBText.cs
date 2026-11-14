/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	using System.ComponentModel;
	using Alphora.Dataphor.DAE.Client;

	/// <summary> Represents data-aware read-only text control. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBText),"Icons.DBText.bmp")]
	public class DBText	: Label, IDataSourceReference, IColumnNameReference, IWidthRange
	{
		/// <summary> Initializes a new instance of a DBText control. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		public DBText()	: base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			CausesValidation = false;
			_link = new FieldDataLink();
			_link.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			AutoSize = false;
			_widthRange = new WidthRange(this);
			_widthRange.OnInternalSetWidth += new InternalSetWidthEventHandler(InternalSetWidth);
			_widthRange.OnMeasureWidth += new MeasureWidthEventHandler(MeasureWidth);
			ForeColor = Color.Navy;
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				_link.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
				_link.Dispose();
				_link = null;
			}
			base.Dispose(disposing);
		}

		private FieldDataLink _link;
		/// <summary> Links this control to a view's DataField. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		protected FieldDataLink Link
		{
			get { return _link; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the text is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return _link.Source; }
			set
			{
				if (_link.Source != value)
					_link.Source = value;
			}
		}

		/// <summary> Gets or sets a value indicating the column name from which the text displays data. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set { _link.ColumnName = value; }
		}

		/// <summary> Gets the DataField the text represents. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		[Browsable(false)]
		public DataField DataField
		{
			get { return _link.DataField; }
		}

		/// <summary> Gets a string that represents the field value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		protected virtual string FieldValue
		{
			get { return ((DataField == null) || (!DataField.HasValue())) ? String.Empty : DataField.AsDisplayString; }
		}

		private void FieldChanged(DataLink link, DataSet dataSet, DataField field)
		{
			base.Text = FieldValue;
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text { get { return base.Text; } }

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
