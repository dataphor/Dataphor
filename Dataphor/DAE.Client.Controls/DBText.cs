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
			FLink = new FieldDataLink();
			FLink.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			AutoSize = false;
			FWidthRange = new WidthRange(this);
			FWidthRange.OnInternalSetWidth += new InternalSetWidthEventHandler(InternalSetWidth);
			FWidthRange.OnMeasureWidth += new MeasureWidthEventHandler(MeasureWidth);
			ForeColor = Color.Navy;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				FLink.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
				FLink.Dispose();
				FLink = null;
			}
			base.Dispose(ADisposing);
		}

		private FieldDataLink FLink;
		/// <summary> Links this control to a view's DataField. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		protected FieldDataLink Link
		{
			get { return FLink; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the text is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return FLink.Source; }
			set
			{
				if (FLink.Source != value)
					FLink.Source = value;
			}
		}

		/// <summary> Gets or sets a value indicating the column name from which the text displays data. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FLink.ColumnName; }
			set { FLink.ColumnName = value; }
		}

		/// <summary> Gets the DataField the text represents. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		[Browsable(false)]
		public DataField DataField
		{
			get { return FLink.DataField; }
		}

		/// <summary> Gets a string that represents the field value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBText.dxd"/>
		protected virtual string FieldValue
		{
			get { return ((DataField == null) || (!DataField.HasValue())) ? String.Empty : DataField.AsDisplayString; }
		}

		private void FieldChanged(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			base.Text = FieldValue;
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text { get { return base.Text; } }

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
