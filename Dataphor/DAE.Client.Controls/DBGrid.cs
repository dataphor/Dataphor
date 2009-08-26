/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;
using System.ComponentModel.Design.Serialization;

using DAEClient = Alphora.Dataphor.DAE.Client;
using DAESchema = Alphora.Dataphor.DAE.Schema;
using DAEStreams = Alphora.Dataphor.DAE.Streams;
using DAEData = Alphora.Dataphor.DAE.Runtime.Data;

/*
	Grid Columns Hierarchy:
	
		MarshalByRefObject
			|- GridColumn				(Abstract non data-aware column)
				|- ActionColumn
				|- DataColumn			(Abstract data-aware column)
					|- CheckBoxColumn
					|- ImageColumn
					|- LinkColumn	
					|- TextColumn		
				|- SequenceColumn	
					
	Grid row height management:
		The height of a row in the grid is determined by the following:
			MinRowHeight:
				The minimum height of a row in pixels.  If -1 then calculated.
				Calculation: The MinRowHeight = if the grids MinRowHeight property equals -1 then the maximum of the MinRowHeight of each column else the grids MinRowHeight property value.
			MaxRowHeight:
				The maximum height of a row in pixels. If -1 then calculated.
				Calculation: The MaxRowHeight = if the grids MaxRowHeight property = -1 then minimum of the MaxRowHeight of each column else the grids MaxRowHeight property value.
			NaturalHeight (Abstract and defined by each column):
				The natural height of a row in pixels.  Always calculated.  
				Calculation: Calls the NaturalHeight method of each colum in the grid, which returns the desired height of a row given a value(see the examples section below).
				If this value exceeds the calculated MaxRowHeight then MaxRowHeight is the actual row height.
				If this value is less than the calculated MinRowHeight then MinRowHeight is the actual row height.
				else NaturalHeight is the actual height of the row.
			Examples:
				The ImageColumn natural height is the height of the original image.
				The TextColumn natural Height when wordwrap is on. Given the width of the column, calculates how tall the cell needs to be to contain the wrapped text.
*/

namespace Alphora.Dataphor.DAE.Client.Controls
{

	public class GridDataLink : DataLink
	{
	}

	/// <summary> Style of grid lines in a <see cref="T:Alphora.Dataphor.DAE.Client.Controls.DBGrid"/>. </summary>
	public enum GridLines {Both, Horizontal, None, Vertical}
	/// <summary> Method of row selection in the <see cref="T:Alphora.Dataphor.DAE.Client.Controls.DBGrid"/>. </summary>
	[Flags]
	public enum ClickSelectsRow {None = 0, LeftClick = 1, RightClick = 2, MiddleClick = 4}
	/// <summary> Specifies the behavior for changing the order of data in a <see cref="T:Alphora.Dataphor.DAE.Client.Controls.DBGrid"/>. </summary>
	public enum OrderChange
	{
		/// <summary> Order by the selected column only if it is in a key or order. </summary>
		Key,
		/// <summary>
		/// Even if the column is not a key force the order.
		/// May cause performance hits when working with large amounts of data.
		/// </summary>
		Force,
		/// <summary> Order remains unchanged when clicking on a column header. </summary>
		None
	}

	/// <summary> Represents a method that handles the Horizontal scroll event of a <see cref="DBGrid"/>. </summary>
	public delegate void HorizontalScrollHandler(object ASender, int AVisibleColumnIndex, int ADelta, EventArgs AArgs);
	/// <summary> Represents a method that handles mouse events for a <see cref="GridColumn"/> </summary>
	public delegate void ColumnMouseEventHandler(object ASender, GridColumn AColumn, int AXPos, int AYPos, EventArgs AArgs);
	/// <summary> Represents a method that handles basic column events for a <see cref="DBGrid"/> </summary>
	public delegate void ColumnEventHandler(object ASender, GridColumn AColumn, EventArgs AArgs);
	/// <summary> Represents a method that handles context menu popup events for a <see cref="DBGrid"/> </summary>
	public delegate void ContextMenuEventHandler(object ASender, GridColumn AColumn, System.Drawing.Point APopupPoint, EventArgs AArgs);
	/// <summary> Represents a method that handles cell painting events for a <see cref="DBGrid"/> </summary>
	public delegate void PaintCellEventHandler(object ASender, GridColumn AColumn, Rectangle ARect, Graphics AGraphics, int LRowIndex); 

	/// <summary> Provides a two dimensional view of data from a data source. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBGrid),"Icons.DBGrid.bmp")]
	public class DBGrid : BorderedControl, DAEClient.IReadOnly, IDataSourceReference
	{
		public const int CAutoScrollWidth = 30;
		public const byte CDefaultHighlightAlpha = 60;	// Default transparency of highlight bar
		public const byte CDefaultDragImageAlpha = 60;
		private Size FBeforeDragSize = WinForms.SystemInformation.DragSize;

		/// <summary> Initializes a new instance of the <see cref="T:Alphora.Dataphor.DAE.Client.Controls.DbGrid"/> class. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		public DBGrid() : base()
		{
			InitializeGrid();
		}

		private void InitializeGrid()
		{
			SetStyle(WinForms.ControlStyles.SupportsTransparentBackColor, true);
			CausesValidation = false;
			FColumns = new GridColumns(this);
			FHeader = new ArrayList();
			FRows = new GridRows(this);
			FFirstColumnIndex = 0;
			FLink = new GridDataLink();
			FLink.OnActiveChanged += new DAEClient.DataLinkHandler(ActiveChanged);
			FLink.OnRowChanged += new DAEClient.DataLinkFieldHandler(RowChanged);
			FLink.OnDataChanged += new DAEClient.DataLinkHandler(DataChanged);
			FReadOnly = true;
			Size = new Size(185, 89);
			FDefaultColumnWidth = 100;
			FCellPadding = DefaultCellPadding;
			FLineWidth = 1;
			FMinRowHeight = DefaultRowHeight;
			FNoValueBackColor = ControlColor.NoValueBackColor;
			FFixedColor = DBGrid.DefaultBackColor;
			FSaveBackColor = BackColor;
			FHighlightColor = Color.FromArgb(CDefaultHighlightAlpha, SystemColors.Highlight);
			FAxisLineColor = Color.Silver;
			FDefaultHeader3DStyle = WinForms.Border3DStyle.RaisedInner;
			FDefaultHeader3DSide = WinForms.Border3DSide.Top | WinForms.Border3DSide.Left | WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Right;
			FScrollBars = WinForms.ScrollBars.Both;
			FPreviousGridColumn = null;
			FColumnResizing = true;
			FResizingColumn = null;
			FDragScrollTimer = null;
			FDragDropScrollInterval = 400;
			FOrderIndicatorColor = SystemColors.ControlDark;
			FClickSelectsRow = ClickSelectsRow.LeftClick | ClickSelectsRow.RightClick;
			FOrderChange = OrderChange;
			FSaveFont = Font;
			FSaveForeColor = ForeColor;
			FExtraRowColor = Color.FromArgb(60, SystemColors.ControlDark);
			FHideHighlightBar = false;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				try
				{
					try
					{
						FLink.OnActiveChanged -= new DAEClient.DataLinkHandler(ActiveChanged);
						FLink.OnRowChanged -= new DAEClient.DataLinkFieldHandler(RowChanged);
						FLink.OnDataChanged -= new DAEClient.DataLinkHandler(DataChanged);
						FLink.Dispose();
						FLink = null;
					}
					finally
					{
						FColumns.Dispose();
						FColumns = null;
					}
				}
				finally
				{
					FHeader = null;
					FRows = null;
				}
			}
			base.Dispose(ADisposing);
		}

		protected override WinForms.CreateParams CreateParams
		{
			get
			{
				WinForms.CreateParams LParams = base.CreateParams;
				switch (FScrollBars)
				{
					case WinForms.ScrollBars.Both :
						LParams.Style |= NativeMethods.WS_HSCROLL | NativeMethods.WS_VSCROLL;
						break;
					case WinForms.ScrollBars.Vertical :
						LParams.Style |= NativeMethods.WS_VSCROLL;
						break;
					case WinForms.ScrollBars.Horizontal :
						LParams.Style |= NativeMethods.WS_HSCROLL;
						break;
				}
				return LParams;
			}
		}
		
		/// <summary> Gets the grids internal DataLink to a view. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		private GridDataLink FLink;
		protected internal GridDataLink Link
		{
			get { return FLink; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("DataSource for this control")]
		public DAEClient.DataSource Source
		{
			get { return FLink.Source; }
			set { FLink.Source = value; }
		}

		private bool FReadOnly;
		/// <summary> Gets and sets a value indicating the read-only state of the grid. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		[Category("Data")]
		[DefaultValue(true)]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set
			{
				if (FReadOnly != value)
				{
					FReadOnly = value;
					UpdateColumns();
					InternalInvalidate(Region, true);
				}
			}
		}

		private GridColumns FColumns;
		/// <summary> Collection of GridColumn's. </summary>
		[Category("Columns")]
		[Description("Collection of GridColumn's")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual GridColumns Columns
		{
			get { return FColumns; }
		}
	
		/// <summary> Total width and height of non data area includs the header height and one row of minimum height. </summary>
		[Browsable(false)]
		public Size BorderSize
		{
			get { return (this.Size - this.ClientRectangle.Size) + new Size(0, FHeaderHeight); }
		}

		/// <summary> Size of data area. </summary>
		[Browsable(false)]
		public Size DataSize
		{
			get
			{
				if (FRows.Count > 0)
				{
					Rectangle LRect = FRows[FRows.Count - 1].ClientRectangle;
					return new Size(LRect.X + LRect.Width, LRect.Y + LRect.Height);
				}
				else
					return Size.Empty;
			}
		}

		private OrderChange FOrderChange;
		/// <summary> Determines how the views order changes when clicking on a column. </summary>
		[Category("Order")]
		[DefaultValue(OrderChange.Key)]
		[Description("Method for handling order changes by the DBGrid.")]
		public OrderChange OrderChange
		{
			get { return FOrderChange; }
			set
			{
				if (FOrderChange != value)
					FOrderChange = value;
			}
		}

		private bool ShouldSerializeOrderIndicatorColor() { return FOrderIndicatorColor != SystemColors.ControlDark; }
		private Color FOrderIndicatorColor;
		/// <summary> Color of order indicators on column headers. </summary>
		[Category("Order")]
		[Description("Color of order indicators on column headers.")]
		public Color OrderIndicatorColor
		{
			get { return FOrderIndicatorColor; }
			set
			{
				if (FOrderIndicatorColor != value)
				{
					FOrderIndicatorColor = value;
					InvalidateHeader();
				}
			}
		}

		private bool ShouldSerializeNoValueBackColor() { return FNoValueBackColor != ControlColor.NoValueBackColor; }
		private Color FNoValueBackColor;
		/// <summary> Gets or sets the background color of cells that have no value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		[Category("Appearance")]
		[Description("Background color of a cell that has no value")]
		public Color NoValueBackColor
		{
			get { return FNoValueBackColor; }
			set 
			{ 
				if (FNoValueBackColor != value)
				{
					FNoValueBackColor = value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool ShouldSerializeFixedColor() { return FFixedColor != DBGrid.DefaultBackColor; }
		private Color FFixedColor;
		/// <summary> Color of the column headers and other fixed areas. </summary>
		[Category("Appearance")]
		[Description("Color of the column headers and other fixed areas")]
		public Color FixedColor
		{
			get { return FFixedColor; }
			set
			{
				if (FFixedColor != value)
				{
					FFixedColor = value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool FHideHighlightBar;
		/// <summary> Hides the highlight bar. </summary>
		[DefaultValue(false)]
		[Category("Appearance")]
		[Description("Hides the highlight bar.")]
		public bool HideHighlightBar
		{
			get { return FHideHighlightBar; }
			set
			{
				if (FHideHighlightBar != value)
				{
					FHideHighlightBar = value;
					InvalidateHighlightBar();
				}
			}
		}

		private bool ShouldSerializeHighlightColor() { return FHighlightColor != Color.FromArgb(CDefaultHighlightAlpha, SystemColors.Highlight); }
		private Color FHighlightColor;
		/// <summary> Color of the highlight bar. </summary>
		[Category("Appearance")]
		[Description("Color of the highlighted row bar.  Should have alpha contingent")]
		public Color HighlightColor
		{
			get { return FHighlightColor; }
			set
			{
				if (FHighlightColor != value)
				{
					FHighlightColor = (value.A == 255) ? Color.FromArgb(FHighlightColor.A, value) : value; 
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool ShouldSerializeAxisLineColor() { return FAxisLineColor != Color.Silver; }
		private Color FAxisLineColor;
		/// <summary> Color of the axis lines. </summary>
		[Category("Appearance")]
		[Description("Color of the axis lines.")]
		public Color AxisLineColor
		{
			get { return FAxisLineColor; }
			set
			{
				if (FAxisLineColor != value)
				{
					FAxisLineColor = value;
					InternalInvalidate(Region, true);
				}		
			}
		}

		private Font FSaveFont;
		protected override void OnFontChanged(EventArgs AArgs)
		{
			base.OnFontChanged(AArgs);
			foreach (GridColumn LColumn in FColumns)
				if (LColumn.Font == FSaveFont)
					LColumn.Font = Font;
			FSaveFont = Font;
		}

		private Color FSaveForeColor;
		protected override void OnForeColorChanged(EventArgs AArgs)
		{
			base.OnForeColorChanged(AArgs);
			foreach (GridColumn LColumn in FColumns)
				if (LColumn.ForeColor == FSaveForeColor)
					LColumn.ForeColor = ForeColor;
			FSaveForeColor = ForeColor;
		}

		private Color FSaveBackColor;
		protected override void OnBackColorChanged(EventArgs AArgs)
		{
			base.OnBackColorChanged(AArgs);
			NoValueBackColor = Color.FromArgb(BackColor.A, NoValueBackColor);
			foreach(GridColumn LColumn in FColumns)
				if (LColumn.BackColor == FSaveBackColor)
					LColumn.BackColor = BackColor;
			FSaveBackColor = BackColor;
		}

		private GridLines FGridLines;
		/// <summary> Axis lines to paint. </summary>
		[Category("Appearance")]
		[DefaultValue(GridLines.Both)]
		[Description("Axis lines to paint.")]
		public GridLines GridLines
		{
			get { return FGridLines; }
			set
			{
				if (FGridLines != value)
				{
					FGridLines = value;
					InternalInvalidate(Region, true);
				}	
			}
		}

		private bool FColumnResizing;
		/// <summary> Enables or disables column resizing. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Disable column resizing.")]
		public bool ColumnResizing
		{
			get { return FColumnResizing; }
			set
			{
				if (FColumnResizing != value)
					FColumnResizing = value;
			}
		}

		private int FDragDropScrollInterval;
		/// <summary> The ampount of time in miliseconds between scrolls during drag and drop. </summary>
		[DefaultValue(400)]
		[Category("Behavior")]
		[Description("The time in miliseconds between scrolls during drag and drop.")]
		public int DragDropScrollInterval
		{
			get { return FDragDropScrollInterval; }
			set
			{
				if (value < 1)
					throw new ControlsException(ControlsException.Codes.InvalidInterval);
				if (FDragDropScrollInterval != value)
				{
					FDragDropScrollInterval = value;
					if ((FDragScrollTimer != null) && (FDragScrollTimer.Interval != value))
						FDragScrollTimer.Interval = value;
				}
			}
		}

		private WinForms.Border3DStyle FDefaultHeader3DStyle;
		/// <summary> The default 3D border style of a columns header. </summary>
		[Category("Appearance")]
		[DefaultValue(WinForms.Border3DStyle.RaisedInner)]
		[Description("Default Border3DStyle for column header.")]
		public WinForms.Border3DStyle DefaultHeader3DStyle
		{
			get { return FDefaultHeader3DStyle; }
			set
			{
				if (FDefaultHeader3DStyle != value)
				{
					BeginUpdate();
					try
					{
						WinForms.Border3DStyle LPreviousHeader3DStyle = FDefaultHeader3DStyle;
						FDefaultHeader3DStyle = value;
						foreach (GridColumn LColumn in Columns)
							if (LColumn.Header3DStyle == LPreviousHeader3DStyle)
								LColumn.Header3DStyle = value;
					}
					finally
					{
						EndUpdate();
					}
				}
			}
		}

		private WinForms.Border3DSide FDefaultHeader3DSide;
		/// <summary> The default sides of the header to paint the border. </summary>
		[Category("Appearance")]
		[DefaultValue(WinForms.Border3DSide.Top | WinForms.Border3DSide.Left | WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Right)]
		[Description("Default Border3DSide for column header.")]
		public WinForms.Border3DSide DefaultHeader3DSide
		{
			get { return FDefaultHeader3DSide; }
			set
			{
				if (FDefaultHeader3DSide != value)
				{
					BeginUpdate();
					try
					{
						WinForms.Border3DSide LPreviousHeader3DSide = FDefaultHeader3DSide;
						FDefaultHeader3DSide = value;
						foreach (GridColumn LColumn in Columns)
							if (LColumn.Header3DSide == LPreviousHeader3DSide)
								LColumn.Header3DSide = value;
					}
					finally
					{
						EndUpdate();
					}
				}
			}
		}

		private int FLineWidth;
		/// <summary> Line width of the axis in pixels. </summary>
		[DefaultValue(1)]
		[Category("Appearance")]
		[Description("Grid lines width")]
		public int LineWidth
		{
			get { return FLineWidth; }
			set 
			{
				if (FLineWidth != value)
				{
					FLineWidth = value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private int FDefaultColumnWidth;
		/// <summary> Default width for columns (in characters). </summary>
		[DefaultValue(100)]
		[Category("Appearance")]
		[Description("Default width for columns (in characters).")]
		public int DefaultColumnWidth
		{
			get { return FDefaultColumnWidth; }
			set
			{
				if (FDefaultColumnWidth != value)
				{
					int LPreviousDefaultWidth = FDefaultColumnWidth;
					if (value >= 0)
						FDefaultColumnWidth = value;
					else
						FDefaultColumnWidth = 0;
					foreach (GridColumn LColumn in Columns)
						if (LColumn.Width == LPreviousDefaultWidth)
							LColumn.Width = FDefaultColumnWidth;
				}
			}
		}

		private ClickSelectsRow FClickSelectsRow;
		/// <summary> Method of selecting a row with the mouse. </summary>
		[Category("Behavior")]
		[DefaultValue(ClickSelectsRow.LeftClick | ClickSelectsRow.RightClick)]
		[Description("Allow right click to select a row.")]
		public ClickSelectsRow ClickSelectsRow
		{
			get { return FClickSelectsRow; }
			set { FClickSelectsRow = value; }
		}

		private bool ShouldSerializeHeaderHeight() { return FHeaderHeight != DefaultRowHeight; }
		private int FHeaderHeight = DefaultRowHeight;
		/// <summary> Pixel height of the header row. </summary>
		[Category("Appearance")]
		[Description("Pixel height of the header row.")]
		public int HeaderHeight
		{
			get { return FHeaderHeight; }
			set
			{
				if (FHeaderHeight != value)
				{
					FHeaderHeight = value;
					UpdateBufferCount();
					InternalInvalidate(Region, true);
				}	
			}
		}

		public static int DefaultRowHeight { get { return 19; } }
		private bool ShouldSerializeMinRowHeight() { return FMinRowHeight != DefaultRowHeight; }
		private int FMinRowHeight;
		/// <summary> Minimum pixel height of an individual row in the grid. </summary>
		/// <value> If equals MaxRowHeight then the height is fixed to this value. </value>
		/// <remarks>
		///	Grid Row height management:
		///		The height of a row in the grid is determined by the following:
		///		MinRowHeight:
		///			The minimum height of a row in pixels.  If -1 then calculated.
		///			Calculation: The MinRowHeight = if the grids MinRowHeight property equals -1 then the maximum of the MinRowHeight of each column else the grids MinRowHeight property value.
		///		MaxRowHeight:
		///			The maximum height of a row in pixels. If -1 then calculated.
		///			Calculation: The MaxRowHeight = if the grids MaxRowHeight property = -1 then minimum of the MaxRowHeight of each column else the grids MaxRowHeight property value.
		///		NaturalHeight (Abstract and defined by each column):
		///			The natural height of a row in pixels.  Always calculated.  
		///			Calculation: Calls the NaturalHeight method of each colum in the grid, which returns the desired height of a row given a value(see examples section below).
		///			If this value exceeds the calculated MaxRowHeight then MaxRowHeight is the actual row height.
		///			If this value is less than the calculated MinRowHeight then MinRowHeight is the actual row height.
		///			else NaturalHeight is the actual height of the row.
		///			
		///		Examples:
		///			The ImageColumn natural height is the height of the original image.
		///			The TextColumn natural Height when wordwrap is on. Given the width of the column it calculates how tall the cell needs to be to contain the wrapped text. 
		/// </remarks>
		[Category("Appearance")]
		[Description("Minimum pixel height of a row.")]
		public int MinRowHeight
		{
			get { return FMinRowHeight; }
			set
			{
				if (FMinRowHeight != value)
				{
					if (value <= 0)
						throw new ControlsException(ControlsException.Codes.ZeroMinRowHeight);
					if ((FMaxRowHeight >= 0) && (FMaxRowHeight > -1) && (value > FMaxRowHeight))
						throw new ControlsException(ControlsException.Codes.InvalidMinRowHeight);
					FMinRowHeight = value;
					UpdateBufferCount();
					InternalInvalidate(Region, true);
				}		
			}
		}

		private int FMaxRowHeight = -1;
		/// <summary> Maximum pixel height of an individual row in the grid. </summary>
		/// <value> -1 means calculate max possible based on the client area and the (minimum of)individual columns max. </value>
		/// <remarks>
		///	Grid Row height management:
		///		The height of a row in the grid is determined by the following:
		///		MinRowHeight:
		///			The minimum height of a row in pixels.  If -1 then calculated.
		///			Calculation: The MinRowHeight = if the grids MinRowHeight property equals -1 then the maximum of the MinRowHeight of each column else the grids MinRowHeight property value.
		///		MaxRowHeight:
		///			The maximum height of a row in pixels. If -1 then calculated.
		///			Calculation: The MaxRowHeight = if the grids MaxRowHeight property = -1 then minimum of the MaxRowHeight of each column else the grids MaxRowHeight property value.
		///		NaturalHeight (Abstract and defined by each column):
		///			The natural height of a row in pixels.  Always calculated.  
		///			Calculation: Calls the NaturalHeight method of each colum in the grid, which returns the desired height of a row given a value(see examples section below).
		///			If this value exceeds the calculated MaxRowHeight then MaxRowHeight is the actual row height.
		///			If this value is less than the calculated MinRowHeight then MinRowHeight is the actual row height.
		///			else NaturalHeight is the actual height of the row.
		///		Examples:
		///			The ImageColumn natural height is the height of the original image.
		///			The TextColumn natural Height when wordwrap is on. Given the width of the column it calculates how tall the cell needs to be to contain the wrapped text. 
		/// </remarks>
		[Category("Appearance")]
		[DefaultValue(-1)]
		[Description("Maximum pixel height of a row. -1 means max possible based on the client area.")]
		public int MaxRowHeight
		{
			get { return FMaxRowHeight; }
			set
			{
				if (FMaxRowHeight != value)
				{
					if ((value >= 0) && (value < FMinRowHeight))
						throw new ControlsException(ControlsException.Codes.InvalidMaxRowHeight);
					FMaxRowHeight = value;
					UpdateBufferCount();
					InternalInvalidate(Region, true);
				}	
			}
		}

		/// <summary> Calculates the maximum pixel height of a row. </summary>
		public int CalcMaxRowHeight()
		{
			if (IsHandleCreated)
			{
				if (FMaxRowHeight < 0)
					return DataRectangle.Height < FMinRowHeight ? FMinRowHeight : DataRectangle.Height;
				return FMaxRowHeight > DataRectangle.Height ? DataRectangle.Height : FMaxRowHeight;
			}
			return FMinRowHeight;
		}

		private int FFirstColumnIndex;
		/// <summary> Index of the first visible column in the columns collection. </summary>
		protected internal int FirstColumnIndex { get { return FFirstColumnIndex; } }

		/// <summary> Sets the FirstColumnIndex property. </summary>
		/// <param name="AIndex"></param>
		protected virtual void SetFirstColumnIndex(int AIndex)
		{
			int LNewIndex = AIndex;
			if (LNewIndex >= FColumns.Count)
				LNewIndex = FColumns.Count - 1;
			if (LNewIndex < 0)
				LNewIndex = 0;
			if (FFirstColumnIndex != LNewIndex)
				FFirstColumnIndex = LNewIndex;
		}

		private WinForms.ScrollBars FScrollBars;
		/// <summary> Scroll bars for this control. </summary>
		[Category("Appearance")]
		[DefaultValue(WinForms.ScrollBars.Both)]
		[Description("Scroll bars for this control.")]
		public WinForms.ScrollBars ScrollBars
		{
			get { return FScrollBars; }
			set
			{
				if (FScrollBars != value)
				{
					FScrollBars = value;
					if (IsHandleCreated)
						RecreateHandle();
				}
			}
		}

		private WinForms.ContextMenu FHeaderContextMenu;
		/// <summary> Context menu of the header. </summary>
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Context menu of the header.")]
		public WinForms.ContextMenu HeaderContextMenu
		{
			get { return FHeaderContextMenu; }
			set { FHeaderContextMenu = value; }
		}

		/// <summary> Calculates the length of the entire header in pixels. </summary>
		/// <returns> The length of the entire header in pixels. </returns>
		protected int HeaderPixelWidth()
		{
			if (FHeader != null && FHeader.Count > 0)
			{
				HeaderColumn LLastColumn = (HeaderColumn)FHeader[FHeader.Count - 1];
				return LLastColumn.Offset + LLastColumn.Width;
			}
			return 0;
		}

		/// <summary> Updates both the vertical and horizontal scroll bar positions. </summary>
		protected void UpdateScrollBars()
		{
			UpdateVScrollInfo();
			UpdateHScrollInfo();
		}

		private void UpdateVScrollInfo()
		{
			NativeMethods.SCROLLINFO LScrollInfo;
			if (IsHandleCreated && ((FScrollBars == WinForms.ScrollBars.Vertical) || (FScrollBars == WinForms.ScrollBars.Both)))
			{
				LScrollInfo = new NativeMethods.SCROLLINFO();
				LScrollInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(LScrollInfo);
				LScrollInfo.fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE | NativeMethods.SIF_POS | NativeMethods.SIF_TRACKPOS;
				LScrollInfo.nMin = 0;
				LScrollInfo.nMax = 2;
				LScrollInfo.nPage = 1;
				if (FLink.Active && FLink.DataSet.IsLastRow && !FLink.DataSet.IsFirstRow)
					LScrollInfo.nPos = 2;
				else if (FLink.Active && !FLink.DataSet.IsLastRow && !FLink.DataSet.IsFirstRow)
					LScrollInfo.nPos = 1;
				else
					LScrollInfo.nPos = 0;
				UnsafeNativeMethods.SetScrollInfo(Handle, NativeMethods.SB_VERT, LScrollInfo, true);
			}
		}

		private void UpdateHScrollInfo()
		{
			NativeMethods.SCROLLINFO LScrollInfo;
			if (IsHandleCreated && ((FScrollBars == WinForms.ScrollBars.Horizontal) || (FScrollBars == WinForms.ScrollBars.Both)))
			{
				LScrollInfo = new NativeMethods.SCROLLINFO();
				LScrollInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(LScrollInfo);
				LScrollInfo.fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE | NativeMethods.SIF_POS | NativeMethods.SIF_TRACKPOS;
				LScrollInfo.nMin = 0;
				LScrollInfo.nMax = FColumns.MaxScrollIndex(ClientRectangle.Width, (FCellPadding.Width << 1));
				LScrollInfo.nPos = FFirstColumnIndex;
				LScrollInfo.nPage = 1;
				UnsafeNativeMethods.SetScrollInfo(Handle, NativeMethods.SB_HORZ, LScrollInfo, true);
			}
		}

		/// <summary> Invokes the Update method for each column in the Grid </summary>
		protected void UpdateColumns()
		{
			FColumns.UpdateColumns(FColumns.ShowDefaultColumns);
			UpdateRows();
		}
		
		/// <summary> Occures when a column is changed. </summary>
		[Category("Column")]
		public event ColumnEventHandler OnColumnChanged;

		/// <summary> Raises the OnColumnChanged event. </summary>
		/// <param name="AColumn"> The GridColumn that changed. </param>
		protected virtual void ColumnChanged(GridColumn AColumn)
		{
			if (OnColumnChanged != null)
				OnColumnChanged(this, AColumn, EventArgs.Empty);
		}

		internal void InternalColumnChanged(GridColumn AColumn)
		{
			if (!FColumns.UpdatingColumns && !Disposing && !IsDisposed)
			{
				FColumns.InternalColumnChanged(AColumn);
				if (AColumn is DataColumn)
					((DataColumn)AColumn).InternalCheckColumnExists(FLink);
				UpdateHeader();
				UpdateBufferCount();
				UpdateScrollBars();
				InternalInvalidate(Region, true);
			}
			ColumnChanged(AColumn);
		}

		/// <summary> Occures on non-scrolling navigation or refresh of the view. </summary>
		/// <param name="ALink"> The DataLink between this control and the view. </param>
		/// <param name="AView"> The DataView this control is linked to. </param>
		protected void DataChanged(DAEClient.DataLink ALink, DAEClient.DataSet ADataSet)
		{
			if (ALink.Active && !((ADataSet.State == DataSetState.Insert) || (ADataSet.State == DataSetState.Edit)))
			{
				UpdateBufferCount();
				UpdateScrollBars();
				InternalInvalidate(Region, true);
			}
		}

		/// <summary> Occures whenever the active state of the view changes. </summary>
		/// <param name="ALink"> The DataLink between this control and the view. </param>
		/// <param name="AView"> The DataView this control is linked to. </param>
		protected void ActiveChanged(DAEClient.DataLink ALink, DAEClient.DataSet ADataSet)
		{
			InternalUpdateGrid(true, true);
		}

		/// <summary> Refresh all aspects of the grid including the header, scrollbars, columns and buffers.  </summary>
		/// <param name="AUpdateFirstColumnIndex"> When true updates the FirstColumnIndex. </param>
		/// <param name="AInvalidateRegion"> When true invalidates the entire client region. </param>
		protected internal void InternalUpdateGrid(bool AUpdateFirstColumnIndex, bool AInvalidateRegion)
		{
			if (!Disposing && !IsDisposed && !FColumns.UpdatingColumns)
			{
				UpdateColumns();
				if (AUpdateFirstColumnIndex && FLink.Active)
					SetFirstColumnIndex(FFirstColumnIndex);
				UpdateHeader();
				UpdateBufferCount();
				UpdateScrollBars();
				if (AInvalidateRegion)
					InternalInvalidate(Region, true);
			}
		}

		protected void RowChanged(DAEClient.DataLink ALink, DAEClient.DataSet ADataSet, DAEClient.DataField AField)
		{
			if (ALink.Active && (FRows.Count > 0))
			{
				Rectangle LSaveActiveClientRectangle = FRows[FLink.ActiveOffset].ClientRectangle;
				UpdateBufferCount();
				InternalInvalidate(Rectangle.Union(FRows[FLink.ActiveOffset].ClientRectangle, LSaveActiveClientRectangle), true);
			}
		}

//		/// <summary> Occures when the DataSet's buffer has scrolled.  Or when only the active offset changed resulting in a Delta of 0. </summary>
//		/// <param name="ALink"> The DataLink between this control and the view. </param>
//		/// <param name="AView"> The DataView this control is linked to. </param>
//		/// <param name="ADelta"> The amount and direction scrolled. If 0 then only the ActiveOffset changed. </param>
//		protected virtual void DataScrolled(DAEClient.DataLink ALink, DAEClient.DataSet ADataSet, int ADelta)
//		{	
//			int LPixelsToScroll = 0;
//			if (!FResizing)
//			{
//				if (ADelta != 0)
//				{
//					/*
//					//Save Y's of remaining rows...
//					int LMaxRow = Math.Abs(ADelta) <= FRows.Count ? Math.Abs(ADelta) : FRows.Count;
//					for(int i = 0; i < LMaxRow; i++)
//						LPixelsToScroll += FRows[i].ClientRectangle.Height;
//					*/
//					UpdateBufferCount();
//				}
//				UpdateScrollBars();
//			}
//
//			if (ADelta == 0) 
//			{
//				//Invalidate the prior selected row and the selected row.
//				InternalInvalidate(Rectangle.Inflate(FPriorHighlightRect, FLineWidth, FLineWidth), false);
//				InvalidateHighlightBar();
//				return;
//			}
//
//			if (FRows.Count <= 1)
//			{
//				//Area too small to scroll.
//				InternalInvalidate(Region, true);
//				return;
//			}
//
//			//Save the current dirty region.
//			NativeMethods.RECT LSaveDirtyRect = NativeMethods.RECT.EmptyRECT();
//			UnsafeNativeMethods.GetUpdateRect(Handle, ref LSaveDirtyRect, false);
//
//			Rectangle LAreaToScroll;
//			Rectangle LClipRect;
//			if (ADelta > 0)
//			{
//				LPixelsToScroll = 0;
//				int LMax = ADelta <= FRows.Count ? ADelta : FRows.Count;
//				for(int i = 0; i < LMax; i++)
//					LPixelsToScroll += FRows[i].ClientRectangle.Height;
//				if (LPixelsToScroll >= DataSize.Height)
//				{
//					//Area too large to scroll.
//					InternalInvalidate(Region, true);
//					return;
//				}
//
//				LAreaToScroll = new Rectangle(0, FHeaderHeight, ClientRectangle.Width, FRows[FRows.Count - 1].ClientRectangle.Bottom - FHeaderHeight);
//				LClipRect = ADelta < FRows.Count ? new Rectangle(0, FRows[ADelta].ClientRectangle.Bottom, ClientRectangle.Width, FRows[FRows.Count - 1].ClientRectangle.Bottom - FRows[ADelta].ClientRectangle.Bottom) : LAreaToScroll;
//				ScrollWindowEx(0, LPixelsToScroll, LAreaToScroll, LClipRect);
//
//				//Invalidate the new area.
//				InternalInvalidate(new Rectangle(0, FHeaderHeight - FLineWidth, ClientRectangle.Width, LPixelsToScroll + FLineWidth), false);
//
//				//Invalidate prior highlighted area if applicable.
//				if (ADelta < FRows.Count)
//					Invalidate(FRows[ADelta].ClientRectangle);
//
//				//Invalidate under last row.
//				Invalidate(new Rectangle(0, DataSize.Height - FLineWidth, ClientRectangle.Width, ClientRectangle.Height - DataSize.Height + FLineWidth));
//			}
//			else
//			{
//				InternalInvalidate(Region, true);
//				/*
//				int LStartNewRowIndex = (FRows.Count + ADelta) >= 0 ? FRows.Count + ADelta : 0;
//				if (FResizing || (LStartNewRowIndex <= 0) || (FSaveLastRow == null))
//				{
//					InternalInvalidate(Region, true);
//					return;
//				}
//
//				if (LPixelsToScroll >= DataSize.Height)
//				{
//					//Area too large to scroll.
//					InternalInvalidate(Region, true);
//					return;
//				}
//				
//				LAreaToScroll = new Rectangle(0, FHeaderHeight, ClientRectangle.Width, FSaveLastRow.ClientRectangle.Y - FHeaderHeight);
//				//LClipRect = (FRows.Count + ADelta) > 0 ? new Rectangle(0, FHeaderHeight, ClientRectangle.Width, FSaveLastRow.ClientRectangle.Y - FHeaderHeight) : LAreaToScroll;
//				LClipRect = LAreaToScroll;
//				ScrollWindowEx(0, -LPixelsToScroll, LAreaToScroll, LClipRect);
//
//				//Invalidate new bottom rows.
//				InternalInvalidate(new Rectangle(0, FRows[LStartNewRowIndex].ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height - FRows[LStartNewRowIndex].ClientRectangle.Y), false);
//				*/
//			}
//
//			//Force paint if and only if there exists a prior dirty region.
//			if (!NativeMethods.RECT.ToRectangle(LSaveDirtyRect).IsEmpty)
//				UnsafeNativeMethods.UpdateWindow(Handle);
//		}

		/// <summary> Rectangle bounding the data area. </summary>
		protected internal Rectangle DataRectangle
		{
			get
			{
				Rectangle LResult = ClientRectangle;
				LResult.Y += FHeaderHeight;
				LResult.Height -= FHeaderHeight;
				return LResult;
			}
		}

		/// <summary> Occures whenever the rows client area changes. </summary>
		protected virtual void RowChanged(object AValue, Rectangle ARowRectangle, int ARowIndex)
		{
			foreach (GridColumn LColumn in FColumns)
				LColumn.RowChanged(AValue, ARowRectangle, ARowIndex);
		}

		/// <summary> Called whenever the rows collection has changed. </summary>
		protected virtual void RowsChanged()
		{
			foreach (GridColumn LColumn in FColumns)
				LColumn.RowsChanged();
		}

		private void InternalRowsChanged()
		{
			GridRow LRow;
			for (int i = 0; i < FRows.Count; i++)
			{
				LRow = FRows[i];
				RowChanged(LRow.Row, LRow.ClientRectangle, i);
			}
			RowsChanged();
		}
		
		private GridRow FSaveLastRow = null;

		protected virtual void BeforeRowsChange() {}

		/// <summary> Updates rows and the link's buffer count. </summary>
		private void UpdateRows()
		{
			FSaveLastRow = FRows.Count > 0 ? new GridRow(FRows[FRows.Count - 1].Row, FRows[FRows.Count - 1].ClientRectangle) : null;
			BeforeRowsChange();
			FRows.Clear();

			if (FLink.Active && (FLink.LastOffset >= 0))
			{
				int LWidth = HeaderPixelWidth();
				int LHeight;
				int LTotalHeight = FHeaderHeight;
				int LMaxHeight = ClientRectangle.Height;
				using (Graphics LGraphics = CreateGraphics())
				{
					int LLastOffset = FLink.LastOffset;
					DAEData.Row LRow;
					
					for (int i = FLink.ActiveOffset; i >= 0; i--)
					{
						LRow = FLink.Buffer(i);
						LHeight = MeasureRowHeight(LRow, LGraphics);
						if ((LHeight + LTotalHeight) <= LMaxHeight)
						{
							FRows.Insert(0, new GridRow(LRow, new Rectangle(Point.Empty, new Size(LWidth, LHeight))));
							LTotalHeight += LHeight;
						}
						else
						{
							FLink.BufferCount = FRows.Count;
							LLastOffset = FLink.LastOffset;
							break;
						}
					}

					int j = FLink.ActiveOffset;
					while (LTotalHeight <= LMaxHeight)
					{
						//Add rows to the bottom
						++j;
						if (j <= LLastOffset)
						{
							LRow = FLink.Buffer(j);
							LHeight = MeasureRowHeight(LRow, LGraphics);
							if ((LTotalHeight + LHeight) <= LMaxHeight)
							{
								FRows.Add(new GridRow(LRow, new Rectangle(Point.Empty, new Size(LWidth, LHeight))));
								LTotalHeight += LHeight;
							}
							else
								break;
						}
						else
						{
							int LSaveLastOffset = FLink.LastOffset;
							int LSaveActiveOffset = FLink.ActiveOffset;
							FLink.BufferCount++;
							LLastOffset = FLink.LastOffset;
							if (LSaveLastOffset < LLastOffset)
							{
								if (LSaveActiveOffset == FLink.ActiveOffset)
								{
									LRow = FLink.Buffer(LLastOffset);
									LHeight = MeasureRowHeight(LRow, LGraphics);
									if ((LHeight + LTotalHeight) < LMaxHeight)
									{
										FRows.Add(new GridRow(LRow, new Rectangle(Point.Empty, new Size(LWidth, LHeight))));
										LTotalHeight += LHeight;
									}
									else
										break; //Over the bottom edge.
								}
								else
								{
									LRow = FLink.Buffer(0);
									LHeight = MeasureRowHeight(LRow, LGraphics);
									if ((LHeight + LTotalHeight) < LMaxHeight)
									{
										FRows.Insert(0, new GridRow(LRow, new Rectangle(Point.Empty, new Size(LWidth, LHeight))));
										LTotalHeight += LHeight;
									}
									else
										break; //Over the top edge.
								}
							}
							else
								break; //At max rows.
						}					
					}
				}

				//Update tops
				int LTop = FHeaderHeight;
				Rectangle LNewRect;
				foreach (GridRow LGridRow in FRows)
				{
					LNewRect = LGridRow.ClientRectangle;
					LNewRect.Y = LTop;
					LTop += LNewRect.Height;
					LGridRow.ClientRectangle = LNewRect;
				}
			}
			InternalRowsChanged();
		}
		
		/// <summary> Updates the link's buffer count </summary>
		protected virtual void UpdateBufferCount()
		{
			if (IsHandleCreated && !IsDisposed)
				UpdateRows();
		}

		public static Size DefaultCellPadding { get { return new Size(2, 3); } }
		private bool ShouldSerializeCellPadding() { return !FCellPadding.Equals(DefaultCellPadding); }
		private Size FCellPadding;
		/// <summary> Amount to pad a cells. </summary>
		[Category("Appearance")]
		[Description("Amount to pad cells.")]
		public Size CellPadding
		{
			get { return FCellPadding; }
			set
			{
				FCellPadding = value;
				if (!Disposing && !IsDisposed)
				{
					UpdateHeader();
					UpdateBufferCount();
					UpdateScrollBars();
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool ShouldSerializeExtraRowColor() { return FExtraRowColor != Color.FromArgb(60, SystemColors.ControlDark); }
		private Color FExtraRowColor;
		[Category("Appearance")]
		[Description("Color for extra row indicator.")]
		public Color ExtraRowColor
		{
			get { return FExtraRowColor; }
			set
			{
				if (FExtraRowColor != value)
				{
					FExtraRowColor = (value.A == 255) ? Color.FromArgb(FExtraRowColor.A, value) : value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private GridRows FRows;
		internal GridRows Rows { get { return FRows; } }
		
		private ArrayList FHeader;
		protected internal ArrayList Header { get { return FHeader; } }

		protected void UpdateHeader()
		{
			GridColumn LColumn;
			int LOffsetX = 0;
			if (FResizing && (FColumns.VisibleColumns.Count > 0) && (((GridColumn)FColumns.VisibleColumns[FColumns.VisibleColumns.Count - 1]).IsLastHeaderColumn) && (ClientRectangle.Width >= HeaderPixelWidth()))
				SetFirstColumnIndex(FColumns.MaxScrollIndex(ClientRectangle.Width, FCellPadding.Width << 1));
			int LColumnIndex = FFirstColumnIndex;
			Header.Clear();
			while ((LOffsetX < ClientSize.Width) && (LColumnIndex < FColumns.VisibleColumns.Count))
			{
				LColumn = (GridColumn)FColumns.VisibleColumns[LColumnIndex];
				Header.Add(new HeaderColumn(LColumn, LOffsetX, LColumn.Width + (FCellPadding.Width << 1)));
				LOffsetX += LColumn.Width + (FCellPadding.Width << 1);
				LColumnIndex++;
			}
		}

		[Category("Paint")]
		public event PaintCellEventHandler PaintHeaderCell;
		[Category("Paint")]
		public event PaintCellEventHandler PaintDataCell;

		private HeaderColumn FHeaderColumnToPaint;
	
		protected virtual void OnPaintHeaderCell(Graphics AGraphics, GridColumn AColumn, Rectangle ACellRect, StringFormat AFormat)
		{
			FHeaderColumnToPaint.Paint(AGraphics, ACellRect, AFormat, FFixedColor);
			if (PaintHeaderCell != null)
				PaintHeaderCell(this, AColumn, ACellRect, AGraphics, -1);
		}

		protected virtual void OnPaintDataCell
			(
			Graphics AGraphics,
			GridColumn AColumn,
			Rectangle ACellRect,
			DAEData.Row ARow, 
			bool AIsSelected,
			Pen ALinePen,
			int ARowIndex
			)
		{
			AColumn.InternalPaintCell
				(
				AGraphics,
				ACellRect,
				ARow,
				AIsSelected,
				ALinePen,
				ARowIndex
				);
			if (PaintDataCell != null)
				PaintDataCell(this, AColumn, ACellRect, AGraphics, ARowIndex); 
		}

		/// <summary> Override this method to custom paint the highlighted area. </summary>
		/// <param name="AGraphics">The Graphics instance to paint with.</param>
		/// <param name="AHighlightRect">The highlight area.</param>
		protected virtual void PaintHighlightBar(Graphics AGraphics, Rectangle AHighlightRect)
		{
			using (SolidBrush LHighlightBrush = new SolidBrush(HighlightColor))
			{
				using (Pen LHighlightPen = new Pen(SystemColors.Highlight))
				{
					AGraphics.FillRectangle(LHighlightBrush, AHighlightRect);
					if (Focused)
						AGraphics.DrawRectangle(LHighlightPen, AHighlightRect);
				}
			}
		}

		private int GetMinRowHeight()
		{
			int LMinHeight = -1;
			foreach (GridColumn LColumn in FColumns)
			{
				if (LColumn.MinRowHeight > -1)
					if ((LMinHeight < 0) || (LColumn.MinRowHeight > LMinHeight))
						LMinHeight = LColumn.MinRowHeight;
			}
			return LMinHeight;
		}

		private int GetMaxRowHeight()
		{
			int LMaxHeight = -1;
			foreach (GridColumn LColumn in FColumns)
			{
				if (LColumn.MaxRowHeight > -1)
					if ((LMaxHeight < 0) || (LColumn.MaxRowHeight < LMaxHeight))
						LMaxHeight = LColumn.MaxRowHeight;
			}
			return LMaxHeight;
		}

		private int GetMaxNaturalHeight(DAEData.Row ARow, Graphics AGraphics)
		{
			int LNaturalRowHeight;
			int LMaxRowHeight = FMinRowHeight;
			foreach (GridColumn LColumn in FColumns)
			{
				if (LColumn is DataColumn)
				{
					int LColumnIndex = ((DataColumn)LColumn).ColumnIndex(FLink);

					LNaturalRowHeight = 
						LColumn.NaturalHeight
						(
							LColumnIndex >= 0 && ARow.HasValue(LColumnIndex) ? ARow[LColumnIndex] : null,
							AGraphics
						);
				}
				else
					LNaturalRowHeight = LColumn.NaturalHeight(null, AGraphics);
				if (LNaturalRowHeight > LMaxRowHeight)
					LMaxRowHeight = LNaturalRowHeight;
			}
			return LMaxRowHeight;
		}

		/// <summary> Measures the height of a row in pixels. </summary>
		/// <param name="ARow"></param>
		/// <returns> The height of a row in pixels. </returns>
		protected int MeasureRowHeight(DAEData.Row ARow, Graphics AGraphics)
		{
			int LMinRowHeight = GetMinRowHeight();
			int LMaxRowHeight = GetMaxRowHeight();
			int LGridMaxRowHeight = CalcMaxRowHeight();
			if ((LMaxRowHeight >= 0) && (LMinRowHeight >= 0) && (LMaxRowHeight == LMinRowHeight))
				return LMinRowHeight > LGridMaxRowHeight ? LGridMaxRowHeight : LMinRowHeight;
			if (ARow == null)
				return FMinRowHeight;
			int LNaturalHeight = GetMaxNaturalHeight(ARow, AGraphics);
			if ((LMaxRowHeight < 0) && (LMinRowHeight < 0))
				return LNaturalHeight > LGridMaxRowHeight ? LGridMaxRowHeight : LNaturalHeight;
			if ((LMinRowHeight > -1) && LNaturalHeight < LMinRowHeight)
				return LMinRowHeight > LGridMaxRowHeight ? LGridMaxRowHeight : LMinRowHeight;
			if ((LMaxRowHeight > -1) && (LNaturalHeight > LMaxRowHeight))
				return LMaxRowHeight > LGridMaxRowHeight ? LGridMaxRowHeight : LMaxRowHeight;
			return LNaturalHeight > LGridMaxRowHeight ? LGridMaxRowHeight : LNaturalHeight;
		}

		private Rectangle FPriorHighlightRect = Rectangle.Empty;
		public void DrawCells(Graphics AGraphics, Rectangle ARect, bool APaintHighlightBar)
		{
			using (StringFormat LFormat = new StringFormat(StringFormatFlags.NoWrap))
			{
				using (Pen LLinePen = new Pen(FAxisLineColor, FLineWidth))
				{
					using (SolidBrush LTextBrush = new SolidBrush(ForeColor))
					{
						using (SolidBrush LBackBrush = new SolidBrush(BackColor))
						{
							Rectangle LCellRect = Rectangle.Empty;
							// Draw header cells...
							if (ARect.IntersectsWith(new Rectangle(0, 0, HeaderPixelWidth(), FHeaderHeight)))
								foreach (HeaderColumn LHeader in FHeader)
								{
									LCellRect = new Rectangle(LHeader.Offset, 0, LHeader.Width, FHeaderHeight);
									LFormat.Alignment = AlignmentConverter.ToStringAlignment(LHeader.Column.HeaderTextAlignment);
									FHeaderColumnToPaint = LHeader;
									if (ARect.IntersectsWith(LCellRect) && (LHeader.Column.Grid != null))
										OnPaintHeaderCell(AGraphics, LHeader.Column, LCellRect, LFormat);
								}

							// Draw data cells...
							Rectangle LHighlightRect = Rectangle.Empty;
							int LRowIndex = 0;
							foreach (GridRow LGridRow in FRows)
							{
								LCellRect = LGridRow.ClientRectangle;
								if (LRowIndex == FLink.ActiveOffset)
								{
									LHighlightRect = LGridRow.ClientRectangle;
									LHighlightRect.Y -= 1;
									LHighlightRect.Width -= 1;
								}
								if (ARect.IntersectsWith(LGridRow.ClientRectangle))
								{
									DAEData.Row LRow = LGridRow.Row;
									foreach (HeaderColumn LHeaderColumn in FHeader)
									{
										LCellRect.X = LHeaderColumn.Offset;
										LCellRect.Width = LHeaderColumn.Width;
										if (ARect.IntersectsWith(LCellRect) && (LHeaderColumn.Column.Grid != null))
											OnPaintDataCell
												(
												AGraphics,
												LHeaderColumn.Column,
												LCellRect,
												LRow,
												LRowIndex == FLink.ActiveOffset,
												LLinePen,
												LRowIndex
												);	

									}
								}
								++LRowIndex;
							}

							//Draw highlight for extra row.
							if (FLink.LastOffset >= FRows.Count)
							{
								if (FRows.Count == 0)
									LCellRect = DataRectangle;
								else
								{
									Rectangle LLastRowRect = FRows[FRows.Count - 1].ClientRectangle;
									LCellRect = new Rectangle(LLastRowRect.X, LLastRowRect.Bottom, LLastRowRect.Width, this.MeasureRowHeight(FLink.Buffer(FLink.LastOffset), AGraphics));
								}
								if (ARect.IntersectsWith(LCellRect))
									using (SolidBrush LExtraRowBrush = new SolidBrush(FExtraRowColor))
										AGraphics.FillRectangle(LExtraRowBrush, LCellRect);
							}

							// Draw highlight bar...
							if (APaintHighlightBar && ARect.IntersectsWith(LHighlightRect))
							{
								FPriorHighlightRect = LHighlightRect;
								PaintHighlightBar(AGraphics, LHighlightRect);
							}
								
							// Fill remainder of client area...
							using (Region LRegion = new Region(new Rectangle(Point.Empty, new Size(LCellRect.X + LCellRect.Width, LCellRect.Y + LCellRect.Height))))
							{
								LRegion.Complement(ClientRectangle);
								LBackBrush.Color = BackColor;
								LRegion.Intersect(ARect);
								AGraphics.FillRegion(LBackBrush, LRegion);
							}
						}
					}
				}
			}
		}

		protected virtual void DrawDragImage(WinForms.PaintEventArgs AArgs, ColumnDragObject ADragObject)
		{
			if (AArgs.ClipRectangle.IntersectsWith(ADragObject.HighlightRect))
				using (SolidBrush LBetweenColumnBrush = new SolidBrush(SystemColors.Highlight))
					AArgs.Graphics.FillRectangle(LBetweenColumnBrush, ADragObject.HighlightRect);
			if (
				(ADragObject.DragImage != null) &&
				(AArgs.ClipRectangle.IntersectsWith(new Rectangle(ADragObject.ImageLocation, ADragObject.DragImage.Size)))
			   )
				AArgs.Graphics.DrawImage(ADragObject.DragImage, ADragObject.ImageLocation);
		}

		protected override void OnPaint(WinForms.PaintEventArgs AArgs)
		{
			// Before paint
			foreach (HeaderColumn LColumn in FHeader)
				LColumn.Column.BeforePaint(this, AArgs);

			if (!IsDisposed && FLink.Active && !AArgs.ClipRectangle.IsEmpty)
			{
				DrawCells(AArgs.Graphics, AArgs.ClipRectangle, !FHideHighlightBar);
				if ((FDragObject != null) && (FDragObject.DragImage != null))
					DrawDragImage(AArgs, FDragObject);
			}
			else
				base.OnPaint(AArgs);

			// After paint
			foreach (HeaderColumn LColumn in FHeader)
				LColumn.Column.AfterPaint(this, AArgs);
		}

		protected internal void InternalInvalidate(Rectangle ARect, bool ADoubleBuffered)
		{
			SetStyle(WinForms.ControlStyles.DoubleBuffer, ADoubleBuffered);
			if (!ARect.IsEmpty)
				Invalidate(ARect);
		}

		protected void InternalInvalidate(Region ARegion, bool ADoubleBuffered)
		{
			SetStyle(WinForms.ControlStyles.DoubleBuffer, ADoubleBuffered);
			Invalidate(ARegion);
		}

		protected void InvalidateHighlightBar()
		{
			if (!Disposing && (FRows.Count > FLink.ActiveOffset))
				InternalInvalidate(Rectangle.Inflate(FRows[FLink.ActiveOffset].ClientRectangle, 1, 1), false);
		}

		protected internal void InvalidateHeader()
		{
			InternalInvalidate(new Rectangle(0,0,ClientRectangle.Width,FHeaderHeight), false);
		}

		private int ScrollWindowEx(int Adx, int Ady, Rectangle AScrollRect, Rectangle AClipRect)
		{
			NativeMethods.RECT LScroll = NativeMethods.RECT.FromRectangle(AScrollRect);
			NativeMethods.RECT LClip = NativeMethods.RECT.FromRectangle(AClipRect);
			return UnsafeNativeMethods.ScrollWindowEx(Handle, Adx, Ady, ref LScroll, ref LClip, IntPtr.Zero, IntPtr.Zero, NativeMethods.SW_INVALIDATE);
		}

		private void IvalidateResized(int APriorActiveOffset, int APriorLastOffset)
		{
			if (FLink.Active && (FHeader.Count > 0))
			{
				int LHeaderWidth = HeaderPixelWidth();
				int LBottom = DataSize.Height;
//				if (APriorActiveOffset != FLink.ActiveOffset)
//				{
//					//Do DataScrolled on active offset changed.
//					if (APriorActiveOffset > FLink.ActiveOffset)
//						InternalInvalidate(Rectangle.Inflate(FPriorHighlightRect, FLineWidth, FLineWidth), false);
//					DataScrolled(FLink, FLink.DataSet, FLink.ActiveOffset - APriorActiveOffset);
//					return;
//				}

//				int LCurrentLastOffset = FLink.LastOffset;
//				int LYmin = FRows.Count > 0 ? FRows[FRows.Count - 1].ClientRectangle.Y : 0;
//				if (APriorLastOffset < LCurrentLastOffset)
//				{
//					//Invlidate the last n rows where n = delta of the LastOffset.
//					int LIndex = (FRows.Count - 1) - (LCurrentLastOffset - APriorLastOffset);
//					LIndex = LIndex < 0 ? 0 : LIndex;
//					int LTop = FRows[LIndex].ClientRectangle.Y;
//					InternalInvalidate(new Rectangle(0, LTop, LHeaderWidth, ClientRectangle.Height - LTop), false); 
//				}
//				else if (APriorLastOffset > LCurrentLastOffset)
//				{
//					//Invalidate the last row.
//					InternalInvalidate(new Rectangle(0, LYmin, LHeaderWidth, ClientRectangle.Height - LYmin), false); 
//				}
//				else
//				{
//					if (FOldClientRect.Height < ClientRectangle.Height)
//					{
//						int LTop = FRows.Count > 0 ? FRows[FRows.Count - 1].ClientRectangle.Top : FOldClientRect.Height;
//						Rectangle LAreaToRePaint = new Rectangle(0, LTop, LHeaderWidth, ClientRectangle.Height - LTop);
//						InternalInvalidate(LAreaToRePaint, true);
//					}
//				}
			}
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			if ((FLink != null) && FLink.Active)
				InvalidateHighlightBar();
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
			if ((FLink != null) && FLink.Active)
				InvalidateHighlightBar();
		}

		private bool FResizing;
		/// <summary> True when the grid is being resized. </summary>
		[Browsable(false)]
		public bool Resizing { get { return FResizing; } }

		private Rectangle FOldClientRect = Rectangle.Empty;
		protected override void OnResize(EventArgs AEventArgs)
		{
			FResizing = true;
			try
			{
				base.OnResize(AEventArgs);
				if (IsHandleCreated)
				{
					int LSaveActiveOffset = FLink.Active ? FLink.ActiveOffset : 0;
					int LSaveLastOffset = FLink.Active ? FLink.LastOffset : 0;
					UpdateHeader();
					UpdateBufferCount();
					UpdateScrollBars();
					Invalidate();
//					IvalidateResized(LSaveActiveOffset, LSaveLastOffset);
				}
			}
			finally
			{
				FResizing = false;
				FOldClientRect = ClientRectangle;
			}
		}

		[Category("Scroll")]
		public event HorizontalScrollHandler BeforeHorizontalScroll;
		[Category("Scroll")]
		public event HorizontalScrollHandler AfterHorizontalScroll;

		protected virtual void DoBeforeHorizontalScroll(int AOffset)
		{
			if ((AOffset != 0) && (BeforeHorizontalScroll != null))
				BeforeHorizontalScroll(this, FFirstColumnIndex, AOffset, EventArgs.Empty);
		}

		protected virtual void DoAfterHorizontalScroll(int AScrolledBy)
		{
			if (AfterHorizontalScroll != null)
				AfterHorizontalScroll(this, FFirstColumnIndex, AScrolledBy, EventArgs.Empty);
		}

		private void CheckActive()
		{
			if (!FLink.Active)
				throw new ControlsException(ControlsException.Codes.ViewNotActive);
		}
		
		protected internal int InternalScrollBy(int AValue)
		{
			int LScrolledBy = 0;
			if (AValue < 0)
				LScrolledBy = (FFirstColumnIndex + AValue) >= 0 ? AValue : FFirstColumnIndex * -1;
			else
			{
				int LMaxIndex = FColumns.MaxScrollIndex(ClientRectangle.Width, FCellPadding.Width << 1);
				LScrolledBy = (FFirstColumnIndex + AValue) <= LMaxIndex ? AValue : LMaxIndex - FFirstColumnIndex;
			}
			DoBeforeHorizontalScroll(LScrolledBy);
			SetFirstColumnIndex(FFirstColumnIndex + LScrolledBy);
			if (LScrolledBy != 0)
			{
				UpdateHeader();
				UpdateRows();
				UpdateHScrollInfo();
				InternalInvalidate(Region, true);
				DoAfterHorizontalScroll(LScrolledBy);
			}
			return LScrolledBy;
		}

		/// <summary> Scrolls columns right or left by column count. </summary>
		/// <param name="AValue"> The number of columns to scroll horizontally from the current position </param>
		/// <returns> Number of columns scrolled. </returns>
		public int ScrollBy(int AValue)
		{
			CheckActive();
			return InternalScrollBy(AValue);
		}

		/// <summary> Scrolls to the last column in the grid. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalLastColumn()
		{
			return InternalScrollBy(FColumns.VisibleColumns.Count - FFirstColumnIndex);
		}

		/// <summary> Scrolls to the last column in the grid. </summary>
		/// <returns> The number of columns scrolled. </returns>
		public int LastColumn()
		{
			CheckActive();
			return InternalLastColumn();
		}

		/// <summary> Scrolls to the first column in the grid. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalFirstColumn()
		{
			return InternalScrollBy(FFirstColumnIndex * -1);
		}

		/// <summary> Scrolls to the first column in the grid. </summary>
		/// <returns> The number of columns scrolled. </returns>
		public int FirstColumn()
		{
			CheckActive();
			return InternalFirstColumn();
		}

		/// <summary> Scrolls the grid one column to the left. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalScrollLeft()
		{
			return InternalScrollBy(-1);
		}

		/// <summary> Scrolls the grid one column to the left. </summary>
		/// <returns> The number of columns scrolled. </returns>
		public int ScrollLeft()
		{
			CheckActive();
			return InternalScrollLeft();
		}

		/// <summary> Pages right by columns. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalScrollRight()
		{
			return InternalScrollBy(1);
		}

		/// <summary> Scrolls the grid one column to the right. </summary>
		/// <returns> The number of columns scrolled. </returns>
		public int ScrollRight()
		{
			CheckActive();
			return InternalScrollRight();
		}
		
		/// <summary> Pages left by columns. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalPageLeft()
		{
			if (FHeader.Count > 1)
				return InternalScrollBy((FHeader.Count - 1) * -1);
			else
				return InternalScrollBy(FHeader.Count * -1);
		}

		/// <summary> Pages left by columns. </summary>
		/// <returns> The number of colums scrolled. </returns>
		public int PageLeft()
		{
			CheckActive();
			return InternalPageLeft();
		}

		/// <summary> Pages right by columns. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalPageRight()
		{
			if (FHeader.Count > 1)
				return InternalScrollBy(FHeader.Count - 1);
			else
				return InternalScrollBy(FHeader.Count);
		}

		/// <summary> Pages right by columns. </summary>
		/// <returns> The number of colums scrolled. </returns>
		public int PageRight()
		{
			CheckActive();
			return InternalPageRight();
		}

		protected virtual void WMHScroll(ref WinForms.Message AMessage)
		{
			if (FLink.Active)
			{
				int LPos = NativeMethods.Utilities.HiWord(AMessage.WParam);
				int LCommand = NativeMethods.Utilities.LowWord(AMessage.WParam);
				switch (LCommand)
				{
					case NativeMethods.SB_LINELEFT : InternalScrollLeft();
						break;
					case NativeMethods.SB_LINERIGHT : InternalScrollRight();
						break;
					case NativeMethods.SB_LEFT : InternalScrollBy(FFirstColumnIndex * -1);
						break;
					case NativeMethods.SB_RIGHT : InternalScrollBy(FColumns.MaxScrollIndex(ClientRectangle.Width, FCellPadding.Width << 1));
						break;
					case NativeMethods.SB_PAGELEFT : InternalPageLeft();
						break;
					case NativeMethods.SB_PAGERIGHT : InternalPageRight();
						break;
					case NativeMethods.SB_THUMBPOSITION:
					case NativeMethods.SB_THUMBTRACK : InternalScrollBy(LPos - FFirstColumnIndex);
						break;
					default:
						break;
				}
			}
		}

		protected internal void InternalPageUp()
		{
			FLink.DataSet.MoveBy(-FRows.Count);
		}
		
		/// <summary> Attempts to navigates the view's cursor backward the number of rows visible in grid. </summary>
		public void PageUp()
		{
			CheckActive();
			InternalPageUp();
		}

		protected internal void InternalPageDown()
		{
			FLink.DataSet.MoveBy(FRows.Count);
		}

		/// <summary> Attempts to navigates the view's cursor forward the number of rows visible in grid. </summary>
		public void PageDown()
		{
			CheckActive();
			InternalPageDown(); 
		}

		/// <summary> Attempts to navigates the view's cursor one row prior current row. </summary>
		protected void Prior()
		{
			FLink.DataSet.MoveBy(-1);
		}

		/// <summary> Attempts to navigates the view's cursor one row past current row. </summary>
		protected void Next()
		{
			FLink.DataSet.MoveBy(1);
		}

		/// <summary> Navigates the view's cursor to the first row. </summary>
		protected void First()
		{
			FLink.DataSet.First();
		}

		/// <summary> Navigates the view's cursor to the last row. </summary>
		protected void Last()
		{
			FLink.DataSet.Last();
		}

		protected internal void InternalFirstVisibleRow()
		{
			FLink.DataSet.MoveBy(-FLink.ActiveOffset);
		}

		/// <summary> Navigates the view's cursor to the first visible row of the grid. </summary>
		public void Home()
		{
			CheckActive();
			InternalFirstVisibleRow();
		}

		protected internal void InternalLastVisibleRow()
		{
			if (FRows.Count > 0)
				FLink.DataSet.MoveBy((FRows.Count - 1) - FLink.ActiveOffset);
		}

		/// <summary> Navigates the view's cursor to the last visible row of the grid. </summary>
		public void End()
		{
			CheckActive();
			InternalLastVisibleRow();
		}

		protected virtual void WMVScroll(ref WinForms.Message AMessage)
		{
			if (FLink.Active)
			{
				int LPos = NativeMethods.Utilities.HiWord(AMessage.WParam); //High order word is position.
				int LCommand = NativeMethods.Utilities.LowWord(AMessage.WParam); //Low order word is command
				switch (LCommand)
				{
					case NativeMethods.SB_LINEDOWN : Next();
						break;
					case NativeMethods.SB_LINEUP : Prior();
						break;
					case NativeMethods.SB_TOP : First();
						break;
					case NativeMethods.SB_BOTTOM : Last();
						break;
					case NativeMethods.SB_PAGEUP : InternalPageUp();
						break;
					case NativeMethods.SB_PAGEDOWN : InternalPageDown();
						break;
					case NativeMethods.SB_THUMBPOSITION :
						if (LPos == 0)
							First();
						else if (LPos == 2)
							Last();
						break;
					default:
						break;
				}
			}
		}

		private GridColumn FPreviousGridColumn;
		private WinForms.Cursor FPreviousCursor;
		private HeaderColumn FResizingColumn;

		private void UpdateCursor()
		{
			if (FColumnResizing && (Cursor != FPreviousCursor) && (FResizingColumn == null))
				ResetCursor();
		}

		private HeaderColumn GetResizeableColumn(int AXPos, int AYPos)
		{
			if (!FColumnResizing)
				return null;
			HeaderColumn LHeaderColumn = GetHeaderColumnAt(AXPos);
			if ((LHeaderColumn != null) && InRowRange(AYPos))
			{
				if (!LHeaderColumn.InResizeArea(AXPos))
				{
					int LHeaderIndex = FHeader.IndexOf(LHeaderColumn);
					if ((LHeaderIndex > 0) && (((HeaderColumn)FHeader[--LHeaderIndex]).InResizeArea(AXPos)))
						LHeaderColumn = (HeaderColumn)FHeader[LHeaderIndex];
					else
						LHeaderColumn = null;
				}
			}
			else
				return null;
			return LHeaderColumn;
		}

		[Category("Column")]
		public event EventHandler OnColumnResize;
		[Category("Column")]
		public event ColumnMouseEventHandler OnHeaderMouseMove;
		[Category("Column")]
		public event ColumnMouseEventHandler OnColumnMouseMove;
		[Category("Column")]
		public event ColumnMouseEventHandler OnColumnMouseEnter;
		[Category("Column")]
		public event ColumnEventHandler OnColumnMouseLeave;

		protected internal virtual void DoColumnResize(object ASender, EventArgs AArgs)
		{
			if (OnColumnResize != null)
				OnColumnResize(ASender, AArgs);
		}

		protected virtual void DoColumnMouseLeave(GridColumn AColumn)
		{
			if (OnColumnMouseLeave != null)
				OnColumnMouseLeave(this, AColumn, EventArgs.Empty);
		}

		protected virtual void DoColumnMouseEnter(GridColumn AColumn, int AXPos, int AYPos)
		{
			if (OnColumnMouseEnter != null)
				OnColumnMouseEnter(this, AColumn, AXPos, AYPos, EventArgs.Empty);
		}

		protected virtual void DoColumnMouseMove(GridColumn AColumn, Point ALocation)
		{
			UpdateCursor();
			AColumn.MouseMove(ALocation, GetRowAt(ALocation.Y));
			if (OnColumnMouseMove != null)
				OnColumnMouseMove(this, AColumn, ALocation.X, ALocation.Y, EventArgs.Empty);
		}

		protected virtual void DoHeaderMouseMove(GridColumn AColumn, int AXPos, int AYPos)
		{
			if (FColumnResizing && (GetResizeableColumn(AXPos, AYPos) != null))
			{
				if (Cursor != WinForms.Cursors.VSplit)
				{
					FPreviousCursor = Cursor;
					Cursor = WinForms.Cursors.VSplit;
				}
			}
			else
				UpdateCursor();
			if (OnHeaderMouseMove != null)
				OnHeaderMouseMove(this, AColumn, AXPos, AYPos, EventArgs.Empty);
		}

		protected void DoMouseLeaveGrid()
		{
			try
			{
				UpdateCursor();
				if (FPreviousGridColumn != null)
					DoColumnMouseLeave(FPreviousGridColumn);
			}
			finally
			{
				FPreviousGridColumn = null;
			}
		}

		private bool InRowRange(int APixelY)
		{
			if (APixelY <= FHeaderHeight)
				return true;
			int LRow = GetRowAt(APixelY);
			return ((LRow >= 0) && FLink.Active && (LRow <= FLink.LastOffset));
		}

		protected internal void WMMouseMove(ref WinForms.Message AMessage)
		{
			int LYPos = NativeMethods.Utilities.HiWord(AMessage.LParam);
			int LXPos = NativeMethods.Utilities.LowWord(AMessage.LParam);
			int LKey = NativeMethods.Utilities.LowWord(AMessage.WParam);
			if
				(
				(((LKey | NativeMethods.MK_LBUTTON) != 0) || ((LKey | NativeMethods.MK_RBUTTON) != 0)) &&
				(FDragObject != null) &&
				!FDraggingColumn &&
				((Math.Abs(FDragObject.StartDragPoint.X - LXPos) > FBeforeDragSize.Width) || ((Math.Abs(FDragObject.StartDragPoint.Y - LYPos) > FBeforeDragSize.Height)))
				)
				DoGridColumnDragDrop();
		
			if (FResizingColumn != null)
			{
				int LNewColumnWidth = LXPos - FResizingColumn.Offset - (FCellPadding.Width << 1);
				if (LNewColumnWidth >= ClientRectangle.Width)
					LNewColumnWidth = ClientRectangle.Width - HeaderColumn.CResizePixelWidth;
				FResizingColumn.Column.Width = Math.Max(1, LNewColumnWidth);
			}
		
			HeaderColumn LHeaderColumn = GetHeaderColumnAt(LXPos);
			if ((LHeaderColumn != null) && InRowRange(LYPos))
			{
				if (FPreviousGridColumn != LHeaderColumn.Column)
				{
					try
					{
						if (FPreviousGridColumn != null)
							DoColumnMouseLeave(FPreviousGridColumn);
						DoColumnMouseEnter(LHeaderColumn.Column, LXPos, LYPos);
					}
					finally
					{
						FPreviousGridColumn = LHeaderColumn.Column;
					}
				}
				if (LYPos <= FHeaderHeight)
					DoHeaderMouseMove(LHeaderColumn.Column, LXPos, LYPos);
				else
				{
					DoColumnMouseMove(LHeaderColumn.Column, new Point(LXPos, LYPos));
				}
			}
			else
				DoMouseLeaveGrid();
		}

		protected override void OnMouseLeave(EventArgs AArgs)
		{
			base.OnMouseLeave(AArgs);
			DoMouseLeaveGrid();
		}

		[Category("ContextMenu")]
		public event ContextMenuEventHandler BeforeHeaderContextPopup;
		[Category("ContextMenu")]
		public event ContextMenuEventHandler AfterHeaderContextPopup;
		[Category("ContextMenu")]
		public event ContextMenuEventHandler BeforeContextPopup;
		[Category("ContextMenu")]
		public event ContextMenuEventHandler AfterContextPopup;

		protected internal GridColumn InternalGetColumnAt(int AXPos)
		{
			HeaderColumn LHeaderColumn = GetHeaderColumnAt(AXPos);
			if (LHeaderColumn != null)
				return LHeaderColumn.Column;
			else
				return null;
		}

		/// <summary> Returns GridColumn at specific pixel x position. </summary>
		/// <param name="AXPos"> The x location of the pixel. </param>
		/// <returns>GridColumn</returns>
		public GridColumn GetColumnAt(int AXPos)
		{
			return InternalGetColumnAt(AXPos);	
		}

		protected virtual void DoBeforeContextPopup(Point APopupPoint)
		{
			if (BeforeContextPopup != null)
				BeforeContextPopup(this, InternalGetColumnAt(APopupPoint.X), APopupPoint, EventArgs.Empty);
		}

		protected virtual void DoAfterContextPopup(Point APopupPoint)
		{
			if (AfterContextPopup != null)
				AfterContextPopup(this, InternalGetColumnAt(APopupPoint.X), APopupPoint, EventArgs.Empty);
		}

		protected virtual void DoBeforeHeaderContextPopup(Point APopupPoint)
		{
			if (BeforeHeaderContextPopup != null)
				BeforeHeaderContextPopup(this, InternalGetColumnAt(APopupPoint.X), APopupPoint, EventArgs.Empty);
		}

		protected virtual void DoAfterHeaderContextPopup(Point APopupPoint)
		{
			if (AfterHeaderContextPopup != null)
				AfterHeaderContextPopup(this, InternalGetColumnAt(APopupPoint.X), APopupPoint, EventArgs.Empty);
		}

		protected internal virtual void WMContextMenu(ref WinForms.Message AMessage)
		{
			int LYPos = NativeMethods.Utilities.HiWord(AMessage.LParam);
			int LXPos = NativeMethods.Utilities.LowWord(AMessage.LParam);
			Point LPopupPoint = PointToClient(new Point(LXPos, LYPos));
			if (LPopupPoint.Y <= FHeaderHeight)
			{
				DoBeforeHeaderContextPopup(LPopupPoint);
				if (HeaderContextMenu != null)
					HeaderContextMenu.Show(this, LPopupPoint); 
				DoAfterHeaderContextPopup(LPopupPoint);
			}
			else
			{
				DoBeforeContextPopup(LPopupPoint);
				if (ContextMenu != null)
					ContextMenu.Show(this, LPopupPoint);
				DoAfterContextPopup(LPopupPoint);
			}
		}

		protected override void WndProc(ref WinForms.Message AMessage)
		{
			try
			{
				bool LHandled = false;
				switch (AMessage.Msg)
				{
					case NativeMethods.WM_HSCROLL :
						WMHScroll(ref AMessage);
						break;
					case NativeMethods.WM_VSCROLL :
						WMVScroll(ref AMessage);
						break;
					case NativeMethods.WM_MOUSEMOVE :
						WMMouseMove(ref AMessage);
						break;
					case NativeMethods.WM_CONTEXTMENU :
						WMContextMenu(ref AMessage);
						LHandled = true;
						break;
				}
				if (!LHandled)
					base.WndProc(ref AMessage);
			}
			catch (Exception LException)
			{
				WinForms.Application.OnThreadException(LException);
				// don't rethrow
			}
		}

		protected override void OnClick(EventArgs AArgs)
		{
			base.OnClick(AArgs);
			if (CanFocus)
				Focus();
			if (FColumnMouseDown != null)
				FColumnMouseDown.Column.MouseClick(AArgs, FRowIndexMouseDown);
		}

		protected internal int GetRowAt(int AOffsetY)
		{
			if ((AOffsetY > FHeaderHeight) && !Disposing && !IsDisposed)
			{
				int i = 0;
				foreach (GridRow LRow in FRows)
				{
					if ((LRow.ClientRectangle.Top <= AOffsetY) && (LRow.ClientRectangle.Bottom > AOffsetY))
						return i;
					++i;
				}
			}
			return -1;
		}

		protected internal void SelectRow(int ARow)
		{
			FLink.DataSet.MoveBy(ARow - FLink.ActiveOffset);
		}

		private HeaderColumn FColumnMouseDown;
		private int FRowIndexMouseDown;
		private Point FMouseDownAt;
		protected override void OnMouseDown(WinForms.MouseEventArgs AArgs)
		{
			base.OnMouseDown(AArgs);
			if (CanFocus)
				Focus();
			FMouseDownAt = new Point(AArgs.X, AArgs.Y);
			FColumnMouseDown = GetHeaderColumnAt(FMouseDownAt.X);
			FRowIndexMouseDown = GetRowAt(FMouseDownAt.Y);
			if (FLink.Active)
			{
				if (FMouseDownAt.Y <= FHeaderHeight)
				{
					FResizingColumn = GetResizeableColumn(FMouseDownAt.X, FMouseDownAt.Y);
					if ((FColumnMouseDown != null) && (FResizingColumn == null) && (AArgs.Clicks == 1))
					{
						if (AArgs.Button == WinForms.MouseButtons.Left)
							FDragObject = new ColumnDragObject(this, FColumnMouseDown.Column, FMouseDownAt, new Size(FMouseDownAt.X - FColumnMouseDown.Offset, FMouseDownAt.Y), GetDragImage(FMouseDownAt)); 
						FColumnMouseDown.Column.HeaderMouseDown(AArgs);
					}
				}
				
				try
				{
					if (FColumnMouseDown != null)
						FColumnMouseDown.Column.MouseDown(AArgs, FRowIndexMouseDown);
				}
				finally
				{
					if (
						((AArgs.Button == WinForms.MouseButtons.Left) && ((FClickSelectsRow & ClickSelectsRow.LeftClick) != 0)) ||
						((AArgs.Button == WinForms.MouseButtons.Right) && ((FClickSelectsRow & ClickSelectsRow.RightClick) != 0)) ||
						((AArgs.Button == WinForms.MouseButtons.Middle) && ((FClickSelectsRow & ClickSelectsRow.MiddleClick) != 0))
						)
					{
						if ((FRowIndexMouseDown >= 0) && (FRowIndexMouseDown <= FLink.LastOffset))
							SelectRow(FRowIndexMouseDown);
					}
				}

			}
		}
		
		protected Schema.Order FindOrderForColumn(DataColumn AColumn)
		{
			TableDataSet LDataSet = FLink.DataSet as TableDataSet;
			if (LDataSet != null)
			{
				// Returns the order that has the given column as the first column
				if ((LDataSet.Order != null) && (LDataSet.Order.Columns.Count >= 1) && (LDataSet.Order.Columns[0].Column.Name == AColumn.ColumnName))
					return LDataSet.Order;
					
				foreach (Schema.Order LOrder in FLink.DataSet.TableVar.Orders)
					if ((LOrder.Columns.Count >= 1) && (LOrder.Columns[0].Column.Name == AColumn.ColumnName))
						return LOrder;
					
				foreach (Schema.Key LKey in FLink.DataSet.TableVar.Keys)
					if (!LKey.IsSparse && (LKey.Columns.Count >= 1) && (LKey.Columns[0].Name == AColumn.ColumnName))
						return new Schema.Order(LKey);
			}
					
			return null;
		}

		protected virtual void ChangeOrderTo(DataColumn AColumn)
		{
			if (FLink.Active)
			{
				TableDataSet LDataSet = FLink.DataSet as TableDataSet;
				if (LDataSet != null)
				{
					Schema.Order LOrder = FindOrderForColumn(AColumn);
					if (LOrder == null)
					{
						LDataSet.OrderString = "order { " + AColumn.ColumnName + " asc }";
						InvalidateHeader();
					}
					else
					{
						Schema.Order LCurrentOrder = LDataSet.Order;
						int LCurrentColumnIndex = LCurrentOrder == null ? -1 : LCurrentOrder.Columns.IndexOf(AColumn.ColumnName);
						
						bool LDescending = (LCurrentColumnIndex >= 0) && LCurrentOrder.Columns[LCurrentColumnIndex].Ascending;
						if (!LDescending ^ LOrder.Columns[AColumn.ColumnName].Ascending)
							LOrder = new Schema.Order(LOrder, true);
							
						LDataSet.Order = LOrder;
						InvalidateHeader();
					}
				}
			}
		}

		private bool ColumnInKey(DataColumn AColumn)
		{
			if (FLink.Active)
				foreach (Schema.Key LKey in FLink.DataSet.TableVar.Keys)
					if (!LKey.IsSparse && (LKey.Columns.IndexOfName(AColumn.ColumnName) >= 0))
						return true;
			return false;
		}

		private bool ColumnInOrder(DataColumn AColumn)
		{
			if (FLink.Active)
				foreach (Schema.Order LOrder in FLink.DataSet.TableVar.Orders)
					if (LOrder.Columns.IndexOf(AColumn.ColumnName) >= 0)
						return true;
			return false;
		}

		protected void RebuildViewOrder(DataColumn AColumn)
		{
			if ((FOrderChange == OrderChange.Key) && (ColumnInKey(AColumn) || ColumnInOrder(AColumn)))
				ChangeOrderTo(AColumn);
			if (FOrderChange == OrderChange.Force)
				ChangeOrderTo(AColumn);
		}

		protected override void OnMouseUp(WinForms.MouseEventArgs AArgs)
		{
			base.OnMouseUp(AArgs);
			try
			{
				bool LWasResizingColumn = FResizingColumn != null;
				FResizingColumn = null;
				FDragObject = null;
				int LRowIndexMouseUp = GetRowAt(AArgs.Y);
				if (FColumnMouseDown != null)
				{
					if ((FMouseDownAt.Y >= 0) && (FMouseDownAt.Y <= FHeaderHeight))
					{
						if 
							(
							(FColumnMouseDown.Column is DataColumn) &&
							(AArgs.Button == System.Windows.Forms.MouseButtons.Left) &&
							!LWasResizingColumn &&
							!FDraggingColumn &&
							(AArgs.Y <= FHeaderHeight)
							)
							RebuildViewOrder((DataColumn)FColumnMouseDown.Column);
						FColumnMouseDown.Column.HeaderMouseUp(AArgs);
					}
					FColumnMouseDown.Column.MouseUp(AArgs, LRowIndexMouseUp);
				}
			}
			finally
			{
				FColumnMouseDown = null;
			}
		}

		protected override void OnMouseWheel(WinForms.MouseEventArgs AArgs)
		{
			base.OnMouseWheel(AArgs);
			if (FLink.Active)
			{
				if (AArgs.Delta > 0)
					Prior();
				else if (AArgs.Delta < 0)
					Next();
			}
		}

		private HeaderColumn GetHeaderColumnAt(int AOffsetX)
		{
			HeaderColumn LHeaderColumn = null;
			int LHeaderIndex = 0;
			bool LFound = false;
			while (!LFound && (LHeaderIndex < FHeader.Count))
			{
				LHeaderColumn = (HeaderColumn)FHeader[LHeaderIndex++];
				LFound = ((AOffsetX >= LHeaderColumn.Offset) && (AOffsetX < (LHeaderColumn.Offset + LHeaderColumn.Width + FCellPadding.Width)));
			}
			return LFound ? LHeaderColumn : null;
		}

		private bool FDraggingColumn;
		private ColumnDragObject FDragObject;
		private DragDropTimer FDragScrollTimer;

		private void OnDragScrollRightElapsed(object ASender, EventArgs AArgs)
		{
			InternalScrollRight();
		}

		private void OnDragScrollLeftElapsed(object ASender, EventArgs AArgs)
		{
			InternalScrollLeft();
		}

		protected virtual Image GetDragImage(Point ALocation)
		{
			HeaderColumn LHeaderColumn = GetHeaderColumnAt(ALocation.X);
			Bitmap LBitmap;
			using (Graphics LGraphics = CreateGraphics())
			{
				LBitmap = new Bitmap(LHeaderColumn.Width, FHeaderHeight, LGraphics);
			}
			using (Graphics LGraphics = Graphics.FromImage(LBitmap))
				using (StringFormat LFormat = new StringFormat(StringFormatFlags.NoWrap))
				{
					LFormat.Alignment = AlignmentConverter.ToStringAlignment(LHeaderColumn.Column.HeaderTextAlignment);
					LHeaderColumn.Paint(LGraphics, new Rectangle(new Point(0,0), LBitmap.Size), LFormat, Color.FromArgb(CDefaultDragImageAlpha, FixedColor));
				}
			return LBitmap;
		}

		protected void DoGridColumnDragDrop()
		{
			if (FDragObject != null)
			{
				bool LAllowDrop = AllowDrop;
				FDraggingColumn = true;
				try
				{
					AllowDrop = true;
					WinForms.DataObject LDataObject = new WinForms.DataObject();
					LDataObject.SetData(FDragObject);
					DoDragDrop(LDataObject, WinForms.DragDropEffects.Move | WinForms.DragDropEffects.Scroll);
				}
				finally
				{
					DisposeDragDropTimer();
					FDraggingColumn = false;
					FDragObject = null;
					AllowDrop = LAllowDrop;
					InvalidateHeader();	//Hide the drag image.
				}
			}
		}

		private HeaderColumn GetDropTarget(int AXPos)
		{
			HeaderColumn LHeaderColumn = GetHeaderColumnAt(AXPos);
			if (LHeaderColumn != null)
				return LHeaderColumn;
			else
				return (HeaderColumn)FHeader[FHeader.Count - 1];
		}

		protected override void OnDragEnter(WinForms.DragEventArgs AArgs)
		{
			if (FDraggingColumn)
			{
				AArgs.Effect = WinForms.DragDropEffects.Move;
				Point LPoint = PointToClient(new Point(AArgs.X, AArgs.Y));
				UpdateDragTimer(LPoint);
				UpdateDragObject(LPoint, AArgs.Data);
			}
			else
				AArgs.Effect = WinForms.DragDropEffects.None;
			base.OnDragEnter(AArgs);
		}

		protected override void OnDragOver(WinForms.DragEventArgs AArgs)
		{
			if (FDraggingColumn)
			{
				Point LPoint = PointToClient(new Point(AArgs.X, AArgs.Y));
				AArgs.Effect = WinForms.DragDropEffects.Move;
				UpdateDragTimer(LPoint);
				UpdateDragObject(LPoint, AArgs.Data);
			}
			base.OnDragOver(AArgs);
		}

		protected void UpdateDragObject(Point ACursorPosition, WinForms.IDataObject ADataObject)
		{
			WinForms.DataObject LDraggedData = new WinForms.DataObject(ADataObject);
			if (LDraggedData.GetDataPresent(typeof(ColumnDragObject)))
			{
				ColumnDragObject LDragObject = (ColumnDragObject)LDraggedData.GetData(FDragObject.GetType().ToString());
				if (LDragObject != null)
				{
					Rectangle LInvalidateRect = LDragObject.HighlightRect;
					HeaderColumn LTargetColumn = GetDropTarget(ACursorPosition.X);
					bool LIsRightOfCenter = (ACursorPosition.X > (LTargetColumn.Offset + LTargetColumn.Width - (LTargetColumn.Width / 2)));
					if (LIsRightOfCenter)
						LDragObject.HighlightRect = new Rectangle(LTargetColumn.Offset + LTargetColumn.Width - 2, 0, 4, FHeaderHeight); 
					else
						LDragObject.HighlightRect = new Rectangle(LTargetColumn.Offset - 2, 0, 4, FHeaderHeight); 

					if 
						(
						LDragObject.HighlightRect.Equals(LInvalidateRect) ||
						LInvalidateRect.Contains(LDragObject.HighlightRect)
						)
						LInvalidateRect = Rectangle.Empty;

					if (LTargetColumn.Column != null)
					{
						int LTargetIndex = FColumns.IndexOf(LTargetColumn.Column);
						int LDragIndex = FColumns.IndexOf(LDragObject.DragColumn);
						if (LDragIndex < LTargetIndex)
						{
							if (LIsRightOfCenter)
								++LTargetIndex;
							--LTargetIndex;
						}
						else if (LDragIndex > LTargetIndex)
						{
							if (LIsRightOfCenter)
								++LTargetIndex;
						}
						LDragObject.DropTargetIndex = LTargetIndex;
						LInvalidateRect = Rectangle.Union(LInvalidateRect, LDragObject.HighlightRect); 
					}

					if (LDragObject.DragImage != null)
					{
						Rectangle LOldImageLocRect = new Rectangle(LDragObject.ImageLocation, LDragObject.DragImage.Size);
						Rectangle LNewImageLocRect = new Rectangle(ACursorPosition.X - LDragObject.CursorCenterOffset.Width, 0, LDragObject.DragImage.Width, LDragObject.DragImage.Height); 
						if (!LNewImageLocRect.Location.Equals(LDragObject.ImageLocation))
						{
							LDragObject.ImageLocation = LNewImageLocRect.Location;
							if (!LInvalidateRect.IsEmpty)
								LInvalidateRect = Rectangle.Union(LInvalidateRect,Rectangle.Union(LOldImageLocRect, LNewImageLocRect));
							else
								LInvalidateRect = Rectangle.Union(LOldImageLocRect, LNewImageLocRect);
						}
					}
					if (!LInvalidateRect.IsEmpty)
						InternalInvalidate(LInvalidateRect, true);
				}
			}
		}

		private void DisposeDragDropTimer()
		{
			if (FDragScrollTimer != null)
			{
				try
				{
					FDragScrollTimer.Tick -= new EventHandler(OnDragScrollLeftElapsed);
					FDragScrollTimer.Tick -= new EventHandler(OnDragScrollRightElapsed);
					FDragScrollTimer.Dispose();
				}
				finally
				{
					FDragScrollTimer = null;
				}
			}
		}

		private void UpdateDragTimer(Point APoint)
		{
			if (ClientRectangle.Width < (2 * CAutoScrollWidth + 1))
				return;
			if (APoint.X >= (ClientRectangle.Width - CAutoScrollWidth))
			{
				if (FDragScrollTimer == null)
				{
					if (APoint.X < ClientRectangle.Width)
						FDragScrollTimer = new DragDropTimer(FDragDropScrollInterval);
					else
						FDragScrollTimer = new DragDropTimer(FDragDropScrollInterval - (FDragDropScrollInterval / 2));
					FDragScrollTimer.Tick += new EventHandler(OnDragScrollRightElapsed);
					FDragScrollTimer.ScrollDirection = ScrollDirection.Right;
					FDragScrollTimer.Enabled = true;
				}
				else 
				{
					if (FDragScrollTimer.ScrollDirection == ScrollDirection.Left)
					{
						FDragScrollTimer.Tick -= new EventHandler(OnDragScrollLeftElapsed);
						FDragScrollTimer.Tick += new EventHandler(OnDragScrollRightElapsed);
						FDragScrollTimer.ScrollDirection = ScrollDirection.Right;
					}
				}
			}
			else if (APoint.X <= CAutoScrollWidth)
			{
				if (FDragScrollTimer == null)
				{
					if (APoint.X > 0)
						FDragScrollTimer = new DragDropTimer(DragDropScrollInterval);
					else
						FDragScrollTimer = new DragDropTimer(FDragDropScrollInterval - (FDragDropScrollInterval / 2));
					FDragScrollTimer.Tick += new EventHandler(OnDragScrollLeftElapsed);
					FDragScrollTimer.ScrollDirection = ScrollDirection.Left;
					FDragScrollTimer.Enabled = true;
				}
				else 
				{
					if (FDragScrollTimer.ScrollDirection == ScrollDirection.Right)
					{
						FDragScrollTimer.Tick -= new EventHandler(OnDragScrollRightElapsed);
						FDragScrollTimer.Tick += new EventHandler(OnDragScrollLeftElapsed);
						FDragScrollTimer.ScrollDirection = ScrollDirection.Left;
					}
				}
			}
			else
				DisposeDragDropTimer();
		}

		protected override void OnDragDrop(WinForms.DragEventArgs AArgs)
		{
			try
			{
				try
				{
					DisposeDragDropTimer();
					Point LPoint = PointToClient(new Point(AArgs.X, AArgs.Y));
					UpdateDragObject(LPoint, AArgs.Data);
					HeaderColumn LTargetColumn = GetDropTarget(LPoint.X);
					if (LTargetColumn.Column != null)
					{
						WinForms.DataObject LDraggedData = new WinForms.DataObject(AArgs.Data);
						if (LDraggedData.GetDataPresent(typeof(ColumnDragObject)))
						{
							ColumnDragObject LDragObject = (ColumnDragObject)LDraggedData.GetData(FDragObject.GetType().ToString());
							if ((LDragObject != null) && (LDragObject.DragColumn != LTargetColumn.Column))
							{
								FColumns.Remove(LDragObject.DragColumn);
								FColumns.Insert(LDragObject.DropTargetIndex, LDragObject.DragColumn);
								UpdateHeader();
								UpdateRows();
								UpdateHScrollInfo();
								InternalInvalidate(Region, true);
							}
						}
					}
				}
				finally
				{
					base.OnDragDrop(AArgs);
				}
			}
			catch (Exception LException)
			{
				WinForms.Application.OnThreadException(LException);
			}
		}

		protected override bool IsInputKey(WinForms.Keys AKeyData)
		{
			switch (AKeyData)
			{
				case WinForms.Keys.Left :
				case WinForms.Keys.Right :
				case WinForms.Keys.Control | WinForms.Keys.Left :
				case WinForms.Keys.Control | WinForms.Keys.Right :
				case WinForms.Keys.Shift | WinForms.Keys.Control | WinForms.Keys.Left :
				case WinForms.Keys.Shift | WinForms.Keys.Control | WinForms.Keys.Right :
				case WinForms.Keys.Up :
				case WinForms.Keys.Down :
				case WinForms.Keys.PageUp :
				case WinForms.Keys.PageDown :
				case WinForms.Keys.Control | WinForms.Keys.Home :
				case WinForms.Keys.Home :
				case WinForms.Keys.Control | WinForms.Keys.End :
				case WinForms.Keys.End :
					return true;
			}
			return base.IsInputKey(AKeyData);
		}

		protected override void OnKeyDown(WinForms.KeyEventArgs AArgs)
		{
			if (FLink.Active)
			{
				switch (AArgs.KeyData)
				{
					case WinForms.Keys.Left : InternalScrollLeft(); break;
					case WinForms.Keys.Right : InternalScrollRight(); break;
					case WinForms.Keys.Control | WinForms.Keys.Left : InternalPageLeft(); break;
					case WinForms.Keys.Control | WinForms.Keys.Right : InternalPageRight(); break;
					case WinForms.Keys.Shift | WinForms.Keys.Control | WinForms.Keys.Left : InternalFirstColumn(); break;
					case WinForms.Keys.Shift | WinForms.Keys.Control | WinForms.Keys.Right : InternalLastColumn(); break;
					case WinForms.Keys.Control | WinForms.Keys.Home : First(); break;
					case WinForms.Keys.Home : InternalFirstVisibleRow(); break;
					case WinForms.Keys.Control | WinForms.Keys.End : Last(); break;
					case WinForms.Keys.End : InternalLastVisibleRow(); break;
					case WinForms.Keys.Up : Prior(); break;
					case WinForms.Keys.Down : Next(); break;
					case WinForms.Keys.PageUp : InternalPageUp(); break;
					case WinForms.Keys.PageDown : InternalPageDown(); break;
					case WinForms.Keys.Space : Toggle(); break;
					default:
						base.OnKeyDown(AArgs);
						return;
				}
				AArgs.Handled = true;
			}
		}

		public event EventHandler OnToggle;
		
		/// <summary> Toggles the "selected" value of the grid. </summary>
		/// <remarks> Toggle is invoked when the user presses the space bar.  The default behavior is to toggle the first encountered CheckBoxColumn. </remarks>
		public virtual void Toggle()
		{
			if (OnToggle != null)
				OnToggle(this, EventArgs.Empty);
			else
				InternalToggle();
		}

		protected virtual void InternalToggle()
		{
			foreach (GridColumn LColumn in FColumns)
				if (LColumn.CanToggle())
				{
					LColumn.Toggle();
					break;
				}
		}
	}

	internal class GridRows : List
	{
		public GridRows(DBGrid AOwner)
		{
			AllowNulls = false;
			FGrid = AOwner;
		}

		private DBGrid FGrid;
		protected DBGrid Grid { get { return FGrid; } } 

		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			if (!(AValue is GridRow))
				throw new ControlsException(ControlsException.Codes.GridRowsOnly);
		}

		public new GridRow this[int AIndex]
		{
			get { return (GridRow)base[AIndex]; }
		}
	}

	internal class GridRow
	{
		internal GridRow(DAEData.Row ARow, Rectangle AClientRect)
		{
			FRow = ARow;
			FClientRectangle = AClientRect;
		}

		private DAEData.Row FRow;
		public DAEData.Row Row { get { return FRow; } }

		private Rectangle FClientRectangle;
		public Rectangle ClientRectangle
		{
			get { return FClientRectangle; }
			set	{ FClientRectangle = value;	}
		}
	}

	internal class HeaderColumn
	{
		public const int CResizePixelWidth = 4;

		internal HeaderColumn(GridColumn AColumn, int AOffset, int AWidth)
		{
			Column = AColumn;
			Offset = AOffset;
			Width = AWidth;
		}
		
		public GridColumn Column;
		public int Offset;
		public int Width;

		/// <summary> Determines if the cursor is in the resize area of a column. </summary>
		/// <param name="AXPos"> X position of the cursor. </param>
		/// <returns> True if the cursors x position is in the resize area between two columns, otherwise false. </returns>
		public bool InResizeArea(int AXPos)
		{
			int LOffsetX = Offset + Width;
			return ((AXPos >= LOffsetX - CResizePixelWidth) && (AXPos <= LOffsetX + CResizePixelWidth));
		}
		
		/// <summary> Paints the header of a column. </summary>
		/// <param name="AGraphics"> The graphics object for the Grid control. </param>
		/// <param name="ACellRect"> The area to paint on. </param>
		/// <param name="AFormat"> The StringFormat to use when painting the title. </param>
		/// <param name="AFixedColor"> The background color for the header. </param>
		protected internal void Paint(Graphics AGraphics, Rectangle ACellRect, StringFormat AFormat, System.Drawing.Color AFixedColor)
		{
			Column.PaintHeaderCell(AGraphics, ACellRect, AFormat, AFixedColor);	
		}
	}

	[ToolboxItem(false)]
	[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.GridColumnCollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
	[DesignerSerializer(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.GridColumnsSerializer), typeof(CodeDomSerializer))]
	public class GridColumns : DisposableList
	{

		protected internal GridColumns(DBGrid AGrid) : base()
		{
			FGrid = AGrid;
			FVisibleColumns = new ArrayList();
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FVisibleColumns = null;
			base.Dispose(ADisposing);
		}

		private DBGrid FGrid;
		/// <summary> Owner of columns. </summary>
		public DBGrid Grid { get { return FGrid; } }

		/// <summary> Returns all grid columns as an array of grid columns. </summary>
		public GridColumn[] All
		{
			get
			{
				GridColumn[] LColumns = new GridColumn[Count];
				for(int i = 0; i < Count; i++)
					LColumns[i] = this[i];
				return LColumns;
			}
		}

		protected override void Validate(object AValue)
		{
			if (!(AValue is GridColumn))
				throw new ControlsException(ControlsException.Codes.InvalidGridChild);
		}

		/// <summary> Inserts a grid column at a given index. </summary>
		/// <param name="AIndex"> The index to insert the column to. </param>
		/// <param name="AValue"> The GridColumn instance to insert. </param>
		public override void Insert(int AIndex, object AValue)
		{
			base.Insert(AIndex, AValue);
			if (FVisibleColumns.IndexOf(AValue) >= 0)
			{
				FVisibleColumns.Remove(AValue);
				FVisibleColumns.Insert(GetInsertIndex(AValue), AValue);
			}
		}
		
		protected override void Removing(object AValue, int AIndex)
		{
			UpdateVisibleColumns((GridColumn)AValue, false);
			base.Removing(AValue, AIndex);
			((GridColumn)AValue).RemovingFromGrid(this);
			if (!IsDisposed)
				Grid.InternalUpdateGrid(false, true);
		}

		private void DisposeDefaultGridColumns()
		{
			int i = 0;
			DataColumn LGridColumn;
			while (i <= (Count - 1))
			{
				if (this[i] is DataColumn)
				{
					LGridColumn = (DataColumn)this[i];
					if (LGridColumn.IsDefaultGridColumn)
					{
						Remove(LGridColumn);
						LGridColumn.Dispose();
						LGridColumn = null;
					}
					else
						++i;
				}
				else
					++i;
			}
		}

		private void BuildDefaultGridColumns()
		{
			DataColumn LGridColumn;
			foreach (DAESchema.TableVarColumn LColumn in Grid.Link.DataSet.TableVar.Columns)
			{
				LGridColumn = this[LColumn.Name];
				if (LGridColumn == null)
				{
					DAEClient.DataField LField = Grid.Link.DataSet.Fields[LColumn.Name];
					GridColumn LNewGridColumn;
					if (LField.DataType.Is(Grid.Link.DataSet.Process.DataTypes.SystemBoolean))
						LNewGridColumn = new CheckBoxColumn(Grid, LColumn.Name);
					else
						LNewGridColumn = new TextColumn(Grid, LColumn.Name);
					Add(LNewGridColumn);
					LNewGridColumn.OnAfterWidthChanged += new EventHandler(Grid.DoColumnResize);
				}
			}
		}

		private void UpdateDefaultColumns()
		{
			BuildDefaultGridColumns();
			//Remove any default grid columns that do not have valid DataBufferColumn name.
			DataColumn LGridColumn;
			int i = 0;
			while (i <= (Count - 1))
			{
				if (this[i] is DataColumn)
				{
					LGridColumn = (DataColumn)this[i];
					if (LGridColumn.IsDefaultGridColumn && !Grid.Link.DataSet.TableType.Columns.Contains(LGridColumn.ColumnName))
						LGridColumn.Dispose();
					else
						++i;
				}
				else
					++i;
			}
		}

		private bool FUpdatingColumns;
		/// <summary> Update state of the columns collection. </summary>
		public bool UpdatingColumns { get { return FUpdatingColumns; } }

		protected internal void UpdateColumns(bool AShowDefaultColumns)
		{
			FUpdatingColumns = true;
			try
			{
				if (!AShowDefaultColumns)
					DisposeDefaultGridColumns();
				else
				{
					if (Grid.Link.Active && AShowDefaultColumns)
						UpdateDefaultColumns();
				}
				foreach (GridColumn LColumn in this)
					if (LColumn is DataColumn)
						((DataColumn)LColumn).InternalCheckColumnExists(Grid != null ? Grid.Link : null);
			}
			finally
			{
				FUpdatingColumns = false;
			}
		}

		public bool HasDefaultColumn()
		{
			foreach (GridColumn LColumn in this)
				if ((LColumn is DataColumn) && ((DataColumn)LColumn).IsDefaultGridColumn)
					return true;
			return false;
		}

		/// <summary> Determines the type(Default or Custom) of grid columns belonging to the grid. </summary>
		protected internal bool ShowDefaultColumns
		{
			get { return (Count == 0) || HasDefaultColumn(); }
			set
			{
				if (ShowDefaultColumns != value)
					UpdateColumns(value);
			}
		}

		private void RestoreDefaultColumns()
		{
			if (Grid != null)
				Grid.InternalUpdateGrid(false, true);
		}

		/// <summary> Removes all grid columns from the collection and restores default columns. </summary>
		public override void Clear()
		{
			base.Clear();
			if (!IsDisposed)
				RestoreDefaultColumns();
		}

		/// <summary> Removes a grid column at a given index. </summary>
		/// <param name="AIndex"> Index of the grid column to remove. </param>
		/// <returns> The column removed. </returns>
		public override object RemoveItemAt(int AIndex)
		{
			object LRemoved = base.RemoveItemAt(AIndex);
			if (!IsDisposed && ((Count == 0) || !HasDefaultColumn()))
				RestoreDefaultColumns();
			return LRemoved;
		}

		protected override void Adding(object AValue, int AIndex)
		{
			ShowDefaultColumns = AValue is DataColumn ? ((DataColumn)AValue).IsDefaultGridColumn : false;
			UpdateVisibleColumns((GridColumn)AValue, true);
			base.Adding(AValue, AIndex);
			((GridColumn)AValue).AddingToGrid(this);
			Grid.InternalUpdateGrid(false, true);
		}

		private int GetInsertIndex(object AValue)
		{
			int LStartIndex = IndexOf(AValue);
			while (LStartIndex > 0)
				if (((GridColumn)this[--LStartIndex]).Visible)
					return FVisibleColumns.IndexOf(this[LStartIndex]) + 1;
			return 0;
		}

		private void AddVisibleColumn(GridColumn AColumn)
		{
			if (AColumn.Visible)
			{
				if ((AColumn is DataColumn) && (((DataColumn)AColumn).ColumnName != String.Empty))
					FVisibleColumns.Insert(GetInsertIndex(AColumn), AColumn);
				else
					FVisibleColumns.Insert(GetInsertIndex(AColumn), AColumn);
			}
		}

		private void RemoveVisibleColumn(GridColumn AColumn, bool AConditional)
		{
			if (FVisibleColumns.IndexOf(AColumn) >= 0)
			{
				if (!AConditional)
					FVisibleColumns.Remove(AColumn);
				else
				{
					if (!AColumn.Visible || ((AColumn is DataColumn) && (((DataColumn)AColumn).ColumnName == String.Empty)))
						FVisibleColumns.Remove(AColumn);
				}
			}
		}

		private void ColumnChanged(GridColumn AColumn)
		{
			if (FVisibleColumns.IndexOf(AColumn) < 0)
				AddVisibleColumn(AColumn);
			else
				RemoveVisibleColumn(AColumn, true);
		}

		internal void InternalColumnChanged(GridColumn AColumn)
		{
			ColumnChanged(AColumn);
		}

		protected void UpdateVisibleColumns(GridColumn AValue, bool AInserting)
		{
			if (FVisibleColumns != null)
			{
				if (AInserting)
					AddVisibleColumn(AValue);
				else
					RemoveVisibleColumn(AValue, false);
			}
		}

		/// <summary> Returns a grid column at the given index. </summary>
		public new GridColumn this[int AIndex]
		{
			get { return (GridColumn)base[AIndex]; }
		}

		/// <summary> Returns a data-aware grid column given the column name. </summary>
		public DataColumn this[string AColumnName]
		{
			get
			{
				for (int i = Count - 1; i >= 0; i--)
					if ((this[i] is DataColumn) && (((DataColumn)this[i]).ColumnName == AColumnName))
						return (DataColumn)this[i];
				return null;
			}
		}

		private ArrayList FVisibleColumns;
		/// <summary> Collection of visible grid columns. </summary>
		public ArrayList VisibleColumns
		{
			get { return FVisibleColumns; }
		}

		/// <summary> The number of columns not visible in a given range. </summary>
		/// <param name="AStartIndex"> Start of range. </param>
		/// <param name="AEndIndex"> End of range. </param>
		/// <returns> The number of columns not visible in the range. </returns>
		public int InvisibleColumnCount(int AStartIndex, int AEndIndex)
		{
			if (AStartIndex > AEndIndex)
				throw new ControlsException(ControlsException.Codes.InvalidRange);
			if ((AStartIndex == 0) && (AEndIndex >= (Count - 1)))
				return Count - FVisibleColumns.Count;
			int LCount = 0;
			for (int i = AStartIndex; i <= AEndIndex; ++i)
				if (!((GridColumn)this[i]).Visible)
					++LCount;
			return LCount;
		}

		/// <summary> Returns index of the first visible column in the grid. </summary>
		public int FirstVisibleColumnIndex
		{
			get
			{
				int i;
				for (i = 0; i < Count; ++i)
					if (((GridColumn)this[i]).Visible)
						return i;
				return -1;
			}
		}

		/// <summary> Returns index of the last visible column in the grid. </summary>
		public int LastVisibleColumnIndex
		{
			get
			{
					int i;
				for (i = Count -1; i >= 0; --i)
					if (((GridColumn)this[i]).Visible)
						break;
				return i;
			}
		}

		public int MaxScrollIndex(int AClientWidth, int APadding)
		{
			int LLastWidth = 0;
			int LLastColumnIndex = VisibleColumns.Count; 
			while ((LLastColumnIndex > 0) && (LLastWidth < AClientWidth))
				LLastWidth += ((GridColumn)VisibleColumns[--LLastColumnIndex]).Width + APadding;
				
			if (LLastWidth > AClientWidth)
				return ++LLastColumnIndex;
			else
				return LLastColumnIndex;
		}
	}

	/// <summary> Gets an instance descriptor to seraialize grid columns. </summary>
	public class DataColumnTypeConverter : TypeConverter
	{
		public DataColumnTypeConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext AContext, Type ADestinationType)
		{
			if (ADestinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(AContext, ADestinationType);
		}

		protected virtual int ShouldSerializeSignature(DataColumn AColumn)
		{
			PropertyDescriptorCollection LPropertyDescriptors = TypeDescriptor.GetProperties(AColumn);
			int LSerializeSignature = 0;
			if (LPropertyDescriptors["ColumnName"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 1;

			if (LPropertyDescriptors["Title"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 2;
			
			if (LPropertyDescriptors["Width"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 4;

			if (LPropertyDescriptors["HorizontalAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 8;

			if (LPropertyDescriptors["HeaderTextAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 16;

			if (LPropertyDescriptors["VerticalAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 32;

			if (LPropertyDescriptors["BackColor"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 64;

			if (LPropertyDescriptors["Visible"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 128;

			if (LPropertyDescriptors["Header3DStyle"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 256;

			if (LPropertyDescriptors["Header3DSide"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 512;

			if (LPropertyDescriptors["MinRowHeight"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 1024;

			if (LPropertyDescriptors["MaxRowHeight"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 2048;

			if (LPropertyDescriptors["Font"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 4096;

			if (LPropertyDescriptors["ForeColor"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 8192;

			return LSerializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(DataColumn AColumn)
		{
			ConstructorInfo LInfo;
			Type LType = AColumn.GetType();
			switch (ShouldSerializeSignature(AColumn))
			{
				case 0:
					LInfo = LType.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( LInfo, new object[] {} );
				case 1:
					LInfo = LType.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[] { AColumn.ColumnName }
						);
				case 2:
				case 3:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.ColumnName,
								AColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.ColumnName,
								AColumn.Title,
								AColumn.Width
							}
						);
				default:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(WinForms.HorizontalAlignment),
								typeof(WinForms.HorizontalAlignment),
								typeof(VerticalAlignment),
								typeof(Color),
								typeof(bool),
								typeof(WinForms.Border3DStyle),
								typeof(WinForms.Border3DSide),
								typeof(int),
								typeof(int),
								typeof(Font),
								typeof(Color)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.ColumnName,
								AColumn.Title,
								AColumn.Width,
								AColumn.HorizontalAlignment,
								AColumn.HeaderTextAlignment,
								AColumn.VerticalAlignment,
								AColumn.BackColor,
								AColumn.Visible,
								AColumn.Header3DStyle,
								AColumn.Header3DSide,
								AColumn.MinRowHeight,
								AColumn.MaxRowHeight,
								AColumn.Font,
								AColumn.ForeColor
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, System.Globalization.CultureInfo ACulture, object AValue, Type ADestinationType)
		{
			DataColumn LColumn = AValue as DataColumn;
			if 
				(
				(ADestinationType == typeof(InstanceDescriptor)) &&
				(LColumn != null)
				)
				return GetInstanceDescriptor(LColumn);	
			return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}
	}

	/// <summary> Gets an instance descriptor to seraialize grid columns. </summary>
	public class TextColumnTypeConverter : DataColumnTypeConverter
	{
		public TextColumnTypeConverter() : base() {}
		
		protected override int ShouldSerializeSignature(DataColumn AColumn)
		{
			TextColumn LColumn = AColumn as TextColumn;
			PropertyDescriptorCollection LPropertyDescriptors = TypeDescriptor.GetProperties(AColumn);
			int LSerializeSignature = base.ShouldSerializeSignature(LColumn);
			//ToDo: Should be a better way to get next should serialize constant.
			//Because, if the parent class SerializeSignature changes all descendants will need to change as well.
			if (LPropertyDescriptors["WordWrap"].ShouldSerializeValue(LColumn))
				LSerializeSignature |= 16384 ;
			if (LPropertyDescriptors["VerticalText"].ShouldSerializeValue(LColumn))
				LSerializeSignature |= 32768;
			return LSerializeSignature;
		}

		protected override InstanceDescriptor GetInstanceDescriptor(DataColumn AColumn)
		{
			TextColumn LColumn = AColumn as TextColumn;
			ConstructorInfo LInfo;
			Type LType = LColumn.GetType();
			switch (ShouldSerializeSignature(LColumn))
			{
				case 0:
					LInfo = LType.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( LInfo, new object[] {} );
				case 1:
					LInfo = LType.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[] { LColumn.ColumnName }
						);
				case 2:
				case 3:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.Width
							}
						);
				default:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(WinForms.HorizontalAlignment),
								typeof(WinForms.HorizontalAlignment),
								typeof(VerticalAlignment),
								typeof(Color),
								typeof(bool),
								typeof(WinForms.Border3DStyle),
								typeof(WinForms.Border3DSide),
								typeof(int),
								typeof(int),
								typeof(Font),
								typeof(Color),
								typeof(bool),
								typeof(bool)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.Width,
								LColumn.HorizontalAlignment,
								LColumn.HeaderTextAlignment,
								LColumn.VerticalAlignment,
								LColumn.BackColor,
								LColumn.Visible,
								LColumn.Header3DStyle,
								LColumn.Header3DSide,
								LColumn.MinRowHeight,
								LColumn.MaxRowHeight,
								LColumn.Font,
								LColumn.ForeColor,
								LColumn.WordWrap,
								LColumn.VerticalText
							}
						);
			}
		}
	}

	/// <summary> Gets an instance descriptor to seraialize grid columns. </summary>
	public class LinkColumnTypeConverter : DataColumnTypeConverter
	{
		public LinkColumnTypeConverter() : base() {}
		
		protected override int ShouldSerializeSignature(DataColumn AColumn)
		{
			LinkColumn LColumn = AColumn as LinkColumn;
			PropertyDescriptorCollection LPropertyDescriptors = TypeDescriptor.GetProperties(AColumn);
			int LSerializeSignature = base.ShouldSerializeSignature(LColumn);
			//ToDo: Should be a better way to get next should serialize constant.
			//Because, if the parent class SerializeSignature changes all descendants will need to change as well.
			if (LPropertyDescriptors["WordWrap"].ShouldSerializeValue(LColumn))
				LSerializeSignature |= 16384 ;
			return LSerializeSignature;
		}

		protected override InstanceDescriptor GetInstanceDescriptor(DataColumn AColumn)
		{
			LinkColumn LColumn = AColumn as LinkColumn;
			ConstructorInfo LInfo;
			Type LType = LColumn.GetType();
			switch (ShouldSerializeSignature(LColumn))
			{
				case 0:
					LInfo = LType.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( LInfo, new object[] {} );
				case 1:
					LInfo = LType.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[] { LColumn.ColumnName }
						);
				case 2:
				case 3:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.Width
							}
						);
				default:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(WinForms.HorizontalAlignment),
								typeof(WinForms.HorizontalAlignment),
								typeof(VerticalAlignment),
								typeof(Color),
								typeof(bool),
								typeof(WinForms.Border3DStyle),
								typeof(WinForms.Border3DSide),
								typeof(int),
								typeof(int),
								typeof(Font),
								typeof(Color),
								typeof(bool)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.Width,
								LColumn.HorizontalAlignment,
								LColumn.HeaderTextAlignment,
								LColumn.VerticalAlignment,
								LColumn.BackColor,
								LColumn.Visible,
								LColumn.Header3DStyle,
								LColumn.Header3DSide,
								LColumn.MinRowHeight,
								LColumn.MaxRowHeight,
								LColumn.Font,
								LColumn.ForeColor,
								LColumn.WordWrap
							}
						);
			}
		}
	}

	/// <summary> Gets an instance descriptor to seraialize CheckBox grid columns. </summary>
	public class CheckBoxColumnTypeConverter : DataColumnTypeConverter
	{
		public CheckBoxColumnTypeConverter() : base() {}

		protected override int ShouldSerializeSignature(DataColumn AColumn)
		{
			return base.ShouldSerializeSignature(AColumn) | 512;
		}

		protected override InstanceDescriptor GetInstanceDescriptor(DataColumn AColumn)
		{
			CheckBoxColumn LColumn = AColumn as CheckBoxColumn;
			ConstructorInfo LInfo;
			Type LType = LColumn.GetType();
			switch (ShouldSerializeSignature(LColumn))
			{
				case 512:
				case 513:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(int),
								typeof(bool)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.AutoUpdateInterval,
								LColumn.ReadOnly
							}
						);
				case 514 :
				case 515 : 
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(bool)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.AutoUpdateInterval,
								LColumn.ReadOnly
							}
						);
				case 516 :
				case 517 :
				case 518 :
				case 519 :
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(int),
								typeof(bool)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.Width,
								LColumn.AutoUpdateInterval,
								LColumn.ReadOnly
							}
						);
				default : 
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(WinForms.HorizontalAlignment),
								typeof(WinForms.HorizontalAlignment),
								typeof(VerticalAlignment),
								typeof(Color),
								typeof(bool),
								typeof(WinForms.Border3DStyle),
								typeof(WinForms.Border3DSide),
								typeof(int),
								typeof(int),
								typeof(Font),
								typeof(Color),
								typeof(int)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								LColumn.ColumnName,
								LColumn.Title,
								LColumn.Width,
								LColumn.HorizontalAlignment,
								LColumn.HeaderTextAlignment,
								LColumn.VerticalAlignment,
								LColumn.BackColor,
								LColumn.Visible,
								LColumn.Header3DStyle,
								LColumn.Header3DSide,
								LColumn.MinRowHeight,
								LColumn.MaxRowHeight,
								LColumn.Font,
								LColumn.ForeColor,
								LColumn.AutoUpdateInterval,
								LColumn.ReadOnly
							}
						);
				
			}
		}
	}

	public class ActionColumnTypeConverter : TypeConverter
	{
		public ActionColumnTypeConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext AContext, Type ADestinationType)
		{
			if (ADestinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(AContext, ADestinationType);
		}

		protected virtual int ShouldSerializeSignature(ActionColumn AColumn)
		{
			PropertyDescriptorCollection LPropertyDescriptors = TypeDescriptor.GetProperties(AColumn);
			int LSerializeSignature = 0;
			if (LPropertyDescriptors["ControlText"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 1;

			if (LPropertyDescriptors["Title"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 2;
			
			if (LPropertyDescriptors["Width"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 4;

			if (LPropertyDescriptors["HorizontalAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 8;

			if (LPropertyDescriptors["HeaderTextAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 16;

			if (LPropertyDescriptors["VerticalAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 32;

			if (LPropertyDescriptors["BackColor"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 64;

			if (LPropertyDescriptors["Visible"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 128;

			if (LPropertyDescriptors["Header3DStyle"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 256;

			if (LPropertyDescriptors["Header3DSide"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 512;

			if (LPropertyDescriptors["MinRowHeight"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 1024;

			if (LPropertyDescriptors["MaxRowHeight"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 2048;

			if (LPropertyDescriptors["Font"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 4096;

			if (LPropertyDescriptors["ForeColor"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 8192;

			if (LPropertyDescriptors["ControlClassName"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 16384;
				
			return LSerializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(ActionColumn AColumn)
		{
			ConstructorInfo LInfo;
			Type LType = AColumn.GetType();
			switch (ShouldSerializeSignature(AColumn))
			{
				case 0:
					LInfo = LType.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( LInfo, new object[] {} );
				case 1:
					LInfo = LType.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[] { AColumn.ControlText }
						);
				case 2:
				case 3:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.ControlText,
								AColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.ControlText,
								AColumn.Title,
								AColumn.Width
							}
						);
				default:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string),
								typeof(int),
								typeof(WinForms.HorizontalAlignment),
								typeof(WinForms.HorizontalAlignment),
								typeof(VerticalAlignment),
								typeof(Color),
								typeof(bool),
								typeof(WinForms.Border3DStyle),
								typeof(WinForms.Border3DSide),
								typeof(int),
								typeof(int),
								typeof(Font),
								typeof(Color),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.ControlText,
								AColumn.Title,
								AColumn.Width,
								AColumn.HorizontalAlignment,
								AColumn.HeaderTextAlignment,
								AColumn.VerticalAlignment,
								AColumn.BackColor,
								AColumn.Visible,
								AColumn.Header3DStyle,
								AColumn.Header3DSide,
								AColumn.MinRowHeight,
								AColumn.MaxRowHeight,
								AColumn.Font,
								AColumn.ForeColor,
								AColumn.ControlClassName
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, System.Globalization.CultureInfo ACulture, object AValue, Type ADestinationType)
		{
			ActionColumn LColumn = AValue as ActionColumn;
			if 
				(
				(ADestinationType == typeof(InstanceDescriptor)) &&
				(LColumn != null)
				)
				return GetInstanceDescriptor(LColumn);	
			return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}
	}

	public class SequenceColumnTypeConverter : TypeConverter
	{
		public SequenceColumnTypeConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext AContext, Type ADestinationType)
		{
			if (ADestinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(AContext, ADestinationType);
		}

		protected virtual int ShouldSerializeSignature(SequenceColumn AColumn)
		{
			PropertyDescriptorCollection LPropertyDescriptors = TypeDescriptor.GetProperties(AColumn);
			int LSerializeSignature = 0;
			if (LPropertyDescriptors["Image"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 1;

			if (LPropertyDescriptors["Title"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 2;
			
			if (LPropertyDescriptors["Width"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 4;

			if (LPropertyDescriptors["HorizontalAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 8;

			if (LPropertyDescriptors["HeaderTextAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 16;

			if (LPropertyDescriptors["VerticalAlignment"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 32;

			if (LPropertyDescriptors["BackColor"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 64;

			if (LPropertyDescriptors["Visible"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 128;

			if (LPropertyDescriptors["Header3DStyle"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 256;

			if (LPropertyDescriptors["Header3DSide"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 512;

			if (LPropertyDescriptors["MinRowHeight"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 1024;

			if (LPropertyDescriptors["MaxRowHeight"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 2048;

			if (LPropertyDescriptors["Font"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 4096;

			if (LPropertyDescriptors["ForeColor"].ShouldSerializeValue(AColumn))
				LSerializeSignature |= 8192;

			return LSerializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(SequenceColumn AColumn)
		{
			ConstructorInfo LInfo;
			Type LType = AColumn.GetType();
			switch (ShouldSerializeSignature(AColumn))
			{
				case 0:
					LInfo = LType.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( LInfo, new object[] {} );
				case 1:
					LInfo = LType.GetConstructor
						(
						new Type[] { typeof(Image) }
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[] { AColumn.Image }
						);
				case 2:
				case 3:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(Image),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.Image,
								AColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(Image),
								typeof(string),
								typeof(int)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.Image,
								AColumn.Title,
								AColumn.Width
							}
						);
				default:
					LInfo = LType.GetConstructor
						(
						new Type[] 
							{
								typeof(Image),
								typeof(string),
								typeof(int),
								typeof(WinForms.HorizontalAlignment),
								typeof(WinForms.HorizontalAlignment),
								typeof(VerticalAlignment),
								typeof(Color),
								typeof(bool),
								typeof(WinForms.Border3DStyle),
								typeof(WinForms.Border3DSide),
								typeof(int),
								typeof(int),
								typeof(Font),
								typeof(Color)
							}
						);
					return new InstanceDescriptor
						(
						LInfo,
						new object[]
							{
								AColumn.Image,
								AColumn.Title,
								AColumn.Width,
								AColumn.HorizontalAlignment,
								AColumn.HeaderTextAlignment,
								AColumn.VerticalAlignment,
								AColumn.BackColor,
								AColumn.Visible,
								AColumn.Header3DStyle,
								AColumn.Header3DSide,
								AColumn.MinRowHeight,
								AColumn.MaxRowHeight,
								AColumn.Font,
								AColumn.ForeColor
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext AContext, System.Globalization.CultureInfo ACulture, object AValue, Type ADestinationType)
		{
			SequenceColumn LColumn = AValue as SequenceColumn;
			if 
				(
				(ADestinationType == typeof(InstanceDescriptor)) &&
				(LColumn != null)
				)
				return GetInstanceDescriptor(LColumn);	
			return base.ConvertTo(AContext, ACulture, AValue, ADestinationType);
		}
	}

	public enum VerticalAlignment {Top, Middle, Bottom};

	/// <summary> Base class for all grid columns. </summary>
	public abstract class GridColumn : MarshalByRefObject, IDisposable, IDisposableNotify, ICloneable
	{
		/// <summary> Initializes a new instance of a GridColumn. </summary>
		public GridColumn() : base()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a GridColumn. </summary>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		/// <param name="AHorizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="AHeaderTextAlignment"> Header contents horizontal alignment. </param>
		/// <param name="AVerticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="ABackColor"> Background color for this column. </param>
		/// <param name="AVisible"> Column is visible. </param>
		/// <param name="AHeader3DStyle"> Header paint style. </param>
		/// <param name="AHeader3DSide"> Header paint side. </param>
		/// <param name="AMinRowHeight"> Minimum height of a row. </param>
		/// <param name="AMaxRowHeight"> Maximum height of a row. </param>
		/// <param name="AFont"> Column Font </param>
		/// <param name="AForeColor"> Foreground color </param>
		public GridColumn
			(
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment AHeaderTextAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor
			) : base()
		{
			InitializeColumn();
			FTitle = ATitle;
			FWidth = AWidth;
			FBackColor = ABackColor;
			FVisible = AVisible;
			FHorizontalAlignment = AHorizontalAlignment;
			FHeaderTextAlignment = AHeaderTextAlignment;
			FVerticalAlignment = AVerticalAlignment;
			FHeader3DStyle = AHeader3DStyle;
			FHeader3DSide = AHeader3DSide;
			FMinRowHeight = AMinRowHeight;
			FMaxRowHeight = AMaxRowHeight;
			FFont = AFont;
			FForeColor = AForeColor;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title, and width. </summary>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		public GridColumn (string ATitle, int AWidth) : base()
		{
			InitializeColumn();
			FTitle = ATitle;
			FWidth = AWidth;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title. </summary>
		/// <param name="ATitle"> Column title. </param>
		public GridColumn (string ATitle) : base()
		{
			InitializeColumn();
			FTitle = ATitle;
		}

		/// <summary> Used by the grid to create default columns. </summary>
		/// <param name="AGrid"> The DBGrid the collection belongs to. </param>
		protected internal GridColumn(DBGrid AGrid) : base()
		{
			InitializeColumn();
			FGridColumns = AGrid.Columns;
			FBackColor = AGrid.BackColor;
			if (TitlePixelWidth > AGrid.DefaultColumnWidth)
				FWidth = TitlePixelWidth;
			else
				FWidth = AGrid.DefaultColumnWidth;
			FHeader3DStyle = AGrid.DefaultHeader3DStyle;
			FHeader3DSide = AGrid.DefaultHeader3DSide;
		}

		private void InitializeColumn()
		{
			FGridColumns = null;
			FTitle = string.Empty;
			FWidth = 100;
			FBackColor = DBGrid.DefaultBackColor;
			FVisible = true;
			FHorizontalAlignment = WinForms.HorizontalAlignment.Left;
			FHeaderTextAlignment = WinForms.HorizontalAlignment.Left;
			FVerticalAlignment = VerticalAlignment.Top;
			FHeader3DStyle = WinForms.Border3DStyle.RaisedInner;
			FHeader3DSide = WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Left | WinForms.Border3DSide.Right | WinForms.Border3DSide.Top;
			FMinRowHeight = -1;
			FMaxRowHeight = -1;
			FFont = DBGrid.DefaultFont;
			FForeColor = DBGrid.DefaultForeColor;
		}

		public void Dispose()
		{
			this.Dispose(true);
			System.GC.SuppressFinalize(this);
		}

		private bool FIsDisposed;
		protected bool IsDisposed { get { return FIsDisposed; } }

		protected virtual void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				FGridColumns = null;
				FIsDisposed = true;
				DoDispose();
			}
		}

		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

		// ICloneable
		public virtual object Clone()
		{
			return MemberwiseClone();
		}

		protected void SelectRow(int ARowIndex)
		{
			if (Grid != null)
				Grid.SelectRow(ARowIndex);
		}

		protected Rectangle RowRectangleAt(int ARowIndex)
		{
			return Grid != null ? Grid.Rows[ARowIndex].ClientRectangle : Rectangle.Empty;
		}

		protected object RowValueAt(int ARowIndex)
		{
			return Grid != null ? Grid.Rows[ARowIndex].Row : null;
		}

		protected Rectangle PaddedRectangle(Rectangle ARectangle)
		{
			return Grid != null ? Rectangle.Inflate(ARectangle, -Grid.CellPadding.Width, -Grid.CellPadding.Height) :  ARectangle;
		}

		protected virtual void SetDefaults()
		{
			if (BackColor == DBGrid.DefaultBackColor)
				BackColor = FGridColumns.Grid.BackColor;
			if (Font == DBGrid.DefaultFont)
				Font = FGridColumns.Grid.Font;
			if (ForeColor == DBGrid.DefaultForeColor)
				ForeColor = FGridColumns.Grid.ForeColor;
		}

		public event EventHandler OnInsertIntoGrid;
		protected internal virtual void AddingToGrid(GridColumns AGridColumns)
		{
			FGridColumns = AGridColumns;
			SetDefaults();
			if (OnInsertIntoGrid != null)
				OnInsertIntoGrid(this, EventArgs.Empty);
		}

		public event EventHandler OnRemoveFromGrid;
		protected internal virtual void RemovingFromGrid(GridColumns AGridColumns)
		{
			FGridColumns = null;
			if (OnRemoveFromGrid != null)
				OnRemoveFromGrid(this, EventArgs.Empty);
		}

		private GridColumns FGridColumns;
		/// <summary>
		/// Reference to an DBGrid.GridColumns that include this column.
		/// </summary>
		protected GridColumns GridColumns { get { return FGridColumns; } }

		/// <summary> The DBGrid this column belongs to. </summary>
		public DBGrid Grid
		{
			get { return FGridColumns != null ? FGridColumns.Grid : null; }
		}

		/// <summary> The grids maximum pixel height of a row. </summary>
		protected int GridMaxRowHeight { get { return Grid != null ? Grid.CalcMaxRowHeight() : 0; } }

		private int FMinRowHeight;
		/// <summary> Minimum height of a row in pixels. </summary>
		[Category("Appearance")]
		[DefaultValue(-1)]
		[Description("The minimum height of a row in pixels.")]
		public int MinRowHeight
		{
			get { return FMinRowHeight; }
			set
			{
				if (FMinRowHeight != value)
				{
					if ((value >= 0) && (FMaxRowHeight > -1) && (value > FMaxRowHeight))						
						throw new ControlsException(ControlsException.Codes.InvalidMinRowHeight);
					FMinRowHeight = value;
					Changed();
				}
			}
		}

		private int FMaxRowHeight;
		/// <summary> Maximum height of a row in pixels. </summary>
		[Category("Appearance")]
		[DefaultValue(-1)]
		[Description("The maximum height of a row in pixels.")]
		public int MaxRowHeight
		{
			get { return FMaxRowHeight; }
			set
			{
				if (FMaxRowHeight != value)
				{
					if ((value >= 0) && (FMinRowHeight > -1) && (value < FMinRowHeight))
						throw new ControlsException(ControlsException.Codes.InvalidMaxRowHeight);
					FMaxRowHeight = value;
					Changed();
				}
			}
		}

		public int MeasureRowHeight(object AValue, Graphics AGraphics)
		{
			if ((Grid != null) && (Grid.IsHandleCreated))
			{
				if ((FMaxRowHeight >= 0) && (FMinRowHeight >= 0) && (FMaxRowHeight == FMinRowHeight))
					return FMinRowHeight > GridMaxRowHeight ? GridMaxRowHeight : FMinRowHeight;
				if (AValue == null)
					return Grid.MinRowHeight;
				int LNaturalHeight = NaturalHeight(AValue, AGraphics);
				if ((FMaxRowHeight < 0) && (FMinRowHeight < 0))
					return LNaturalHeight > GridMaxRowHeight ? GridMaxRowHeight : LNaturalHeight;
				if ((FMinRowHeight > -1) && LNaturalHeight < FMinRowHeight)
					return FMinRowHeight > GridMaxRowHeight ? GridMaxRowHeight : FMinRowHeight;
				if ((FMaxRowHeight > -1) && (LNaturalHeight > FMaxRowHeight))
					return FMaxRowHeight > GridMaxRowHeight ? GridMaxRowHeight : FMaxRowHeight;
				return LNaturalHeight > GridMaxRowHeight ? GridMaxRowHeight : LNaturalHeight;
			}
			else
				return DBGrid.DefaultRowHeight;
		}		

		private bool ShouldSerializeFont() { return FFont != DBGrid.DefaultFont; }		
		private System.Drawing.Font FFont;
		[Category("Appearance")]
		public System.Drawing.Font Font
		{
			get { return FFont; }
			set
			{
				if (FFont != value)
				{
					FFont = value;
					Changed();
				}
			}
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="AValue"> The value to measure. </param>
		/// <param name="AGraphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row in pixels. </returns>
		/// <summary> Minimum pixel height of an individual row in the grid. </summary>
		/// <value> If equals MaxRowHeight then the height is fixed to this value. </value>
		/// <remarks>
		/// Override this method to return the natural height of a row given a value to paint in the cell.
		///	Grid Row height management:
		///		The height of a row in the grid is determined by the following:
		///		MinRowHeight:
		///			The minimum height of a row in pixels.  If -1 then calculated.
		///			Calculation: The MinRowHeight = if the grids MinRowHeight property equals -1 then the maximum of the MinRowHeight of each column else the grids MinRowHeight property value.
		///		MaxRowHeight:
		///			The maximum height of a row in pixels. If -1 then calculated.
		///			Calculation: The MaxRowHeight = if the grids MaxRowHeight property = -1 then minimum of the MaxRowHeight of each column else the grids MaxRowHeight property value.
		///		NaturalHeight (Abstract and defined by each column):
		///			The natural height of a row in pixels.  Always calculated.  
		///			Calculation: Calls the NaturalHeight method of each colum in the grid, which returns the desired height of a row given a value(see examples section below).
		///			If this value exceeds the calculated MaxRowHeight then MaxRowHeight is the actual row height.
		///			If this value is less than the calculated MinRowHeight then MinRowHeight is the actual row height.
		///			else NaturalHeight is the actual height of the row.
		///			
		///		Examples:
		///			The ImageColumn natural height is the height of the original image.
		///			The TextColumn natural Height when wordwrap is on. Given the width of the column it calculates how tall the cell needs to be to contain the wrapped text. 
		/// </remarks>
		public abstract int NaturalHeight(object AValue, Graphics AGraphics);

		protected internal virtual void RowsChanged() {}
		protected internal virtual void RowChanged(object AValue, Rectangle ARowRectangle, int ARowIndex) {}

		/// <summary> Paints the column title in the header cell. </summary>
		/// <remarks> Override this method to custom paint the header cell. </remarks>
		protected internal virtual void PaintHeaderCell(Graphics AGraphics, Rectangle ACellRect, StringFormat AFormat, Color AFixedColor)
		{
			using (SolidBrush LBrush = new SolidBrush(AFixedColor))
			{
				AGraphics.FillRectangle(LBrush, ACellRect);
				WinForms.ControlPaint.DrawBorder3D(AGraphics, ACellRect, FHeader3DStyle, FHeader3DSide);
				LBrush.Color = Grid.ForeColor;
				Rectangle LCellWorkArea = Rectangle.Inflate(ACellRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height);
				AGraphics.DrawString
					(
					Title,
					Grid.Font,
					LBrush,
					LCellWorkArea,
					AFormat
					);	
			}
		}

		protected virtual void PaintLines(Graphics AGraphics, Rectangle ACellRect, Pen ALinePen)
		{
			if ((Grid.GridLines == GridLines.Horizontal) || (Grid.GridLines == GridLines.Both))
			{
				AGraphics.DrawLine(ALinePen, ACellRect.X, ACellRect.Y + ACellRect.Height - 1, ACellRect.X + ACellRect.Width, ACellRect.Y + ACellRect.Height - 1);
				if ((Grid.GridLines == GridLines.Horizontal) && this.IsLastHeaderColumn)
					AGraphics.DrawLine(ALinePen, ACellRect.X + ACellRect.Width - 1, ACellRect.Y, ACellRect.X + ACellRect.Width - 1, ACellRect.Y + ACellRect.Height);
			}
			if ((Grid.GridLines == GridLines.Vertical) || (Grid.GridLines == GridLines.Both))
			{
				AGraphics.DrawLine(ALinePen, ACellRect.X + ACellRect.Width - 1, ACellRect.Y, ACellRect.X + ACellRect.Width - 1, ACellRect.Y + ACellRect.Height);
				if ((Grid.GridLines == GridLines.Vertical) && Grid.GetRowAt(ACellRect.Y + 2) == Grid.Link.LastOffset)
					AGraphics.DrawLine(ALinePen, ACellRect.X, ACellRect.Y + ACellRect.Height - 1, ACellRect.X + ACellRect.Width, ACellRect.Y + ACellRect.Height - 1);
			}
			if (Grid.GridLines != GridLines.None)
			{
				if (this.IsFirstHeaderColumn)
					AGraphics.DrawLine(ALinePen, ACellRect.X, ACellRect.Y - 1, ACellRect.X, ACellRect.Y + ACellRect.Height - 1);
			}
		}

		protected virtual Rectangle MeasureVerticalRect
			(
			Rectangle ARect,
			VerticalAlignment AAlignment,
			object AValue,
			Graphics AGraphics
			)
		{
			switch (AAlignment)
			{
				case VerticalAlignment.Bottom :
				{
					int LRowHeight = MeasureRowHeight(AValue, AGraphics);
					if (LRowHeight < ARect.Height)
						ARect.Y = ARect.Bottom - LRowHeight;
					break;
				}
				case VerticalAlignment.Middle :
				{
					int LRowHeight = MeasureRowHeight(AValue, AGraphics);
					if (LRowHeight < ARect.Height)
						ARect.Y += ((ARect.Height + LRowHeight) / 2) - LRowHeight;
					break;
				}
			}
			return ARect;
		}

		/// <summary> Override PaintCell to paint a value in a cell. </summary>
		/// <param name="AValue"> The value to paint. </param>
		/// <param name="AIsSelected"> True if the cell is currently selected. </param>
		/// <param name="ARowIndex"> Index of this row in the grids row buffer. </param>
		/// <param name="AGraphics"> The grids graphics object. </param>
		/// <param name="ACellRect"> Client area allocated to this cell. </param>
		/// <param name="APaddedRect"> Client area allocated to the cell after padding the CellRect. </param>
		/// <param name="AVerticalMeasuredRect"> Client area allocated based on the vertical alignment of the value in the cell. </param>
		/// <remarks> Override PaintCell to custom paint a cell. </remarks>
		protected abstract void PaintCell
			(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect
			);

		protected internal virtual void BeforePaint(object ASender, WinForms.PaintEventArgs AArgs) {}
		protected internal virtual void AfterPaint(object ASender, WinForms.PaintEventArgs AArgs) {}

		protected internal void InternalPaintCell
			(
			Graphics AGraphics, 
			Rectangle ACellRect,
			object AValue,
			bool AIsSelected,
			Pen ALinePen,
			int ARowIndex
			)
		{
			Rectangle LPaddedRect = Rectangle.Inflate(ACellRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height);
			Rectangle LVerticalMeasuredRect = LPaddedRect;
			if (AValue != null)
				LVerticalMeasuredRect = MeasureVerticalRect(LPaddedRect, FVerticalAlignment, AValue, AGraphics);
			PaintCell(AValue, AIsSelected, ARowIndex, AGraphics, ACellRect, LPaddedRect, LVerticalMeasuredRect);
			PaintLines(AGraphics, ACellRect, ALinePen);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnMouseMove;
		protected internal virtual void MouseMove(Point APoint, int ARowIndex)
		{
			if (OnMouseMove != null)
				OnMouseMove(this, new WinForms.MouseEventArgs(WinForms.MouseButtons.None, 0, APoint.X, APoint.Y, 0));
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnMouseDown;
		protected internal virtual void MouseDown(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			if (OnMouseDown != null)
				OnMouseDown(this, AArgs);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnMouseUp;
		protected internal virtual void MouseUp(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			if (OnMouseUp != null)
				OnMouseUp(this, AArgs);
		}

		[Category("Mouse")]
		public event EventHandler OnClick;
		protected internal virtual void MouseClick(EventArgs AArgs, int ARowIndex)
		{
			if (OnClick != null)
				OnClick(this, AArgs);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnHeaderMouseDown;
		protected internal virtual void HeaderMouseDown(WinForms.MouseEventArgs AArgs)
		{
			if (OnHeaderMouseDown != null)
				OnHeaderMouseDown(this, AArgs);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnHeaderMouseUp;
		protected internal virtual void HeaderMouseUp(WinForms.MouseEventArgs AArgs)
		{
			if (OnHeaderMouseUp != null)
				OnHeaderMouseUp(this, AArgs);
		}

		[Browsable(false)]
		public bool IsLastVisibleColumn
		{
			get { return ((Grid.Columns.LastVisibleColumnIndex) == Grid.Columns.IndexOf(this)); }
		}

		[Browsable(false)]
		public bool IsLastHeaderColumn
		{
			get { return ((Grid.Header.Count > 0) && (((HeaderColumn)Grid.Header[Grid.Header.Count - 1]).Column == this)); }
		}

		[Browsable(false)]
		public bool IsFirstHeaderColumn
		{
			get { return ((Grid.Header.Count > 0) && (((HeaderColumn)Grid.Header[0]).Column == this)); }
		}

		[Browsable(false)]
		public bool IsFirstVisibleColumn
		{
			get { return ((Grid.Columns.FirstVisibleColumnIndex) == Grid.Columns.IndexOf(this)); }
		}

		private WinForms.Border3DStyle FHeader3DStyle;
		
		[Category("Appearance")]
		[Description("3D style of the column header.")]
		[DefaultValue(WinForms.Border3DStyle.RaisedInner)]
		public WinForms.Border3DStyle Header3DStyle
		{
			get { return FHeader3DStyle; }
			set
			{
				if (FHeader3DStyle != value)
				{
					FHeader3DStyle = value;
					Changed();
				}
			}
		}

		private WinForms.Border3DSide FHeader3DSide;
		[Category("Appearance")]
		[DefaultValue(WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Left | WinForms.Border3DSide.Right | WinForms.Border3DSide.Top)]
		[Description("3D sides of the column header.")]
		public WinForms.Border3DSide Header3DSide
		{
			get { return FHeader3DSide; }
			set
			{
				if (FHeader3DSide != value)
				{
					FHeader3DSide = value;
					Changed();
				}
			}
		}

		protected void AfterWidthChanged()
		{
			if (OnAfterWidthChanged != null)
				OnAfterWidthChanged(this, EventArgs.Empty);
		}

		protected void BeforeWidthChange()
		{
			if (OnBeforeWidthChange != null)
				OnBeforeWidthChange(this, EventArgs.Empty);
		}

		[Category("Column")]
		public event EventHandler OnAfterWidthChanged;
		[Category("Column")]
		public event EventHandler OnBeforeWidthChange;
		
		private int FWidth;
		[DefaultValue(100)]
		[Category("Appearance")]
		[Description("Pixel width of the column.")]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if ((FWidth != value) && (value >= 0))
				{
					BeforeWidthChange();
					FWidth = value;
					AfterWidthChanged();
					Changed();
				}
			}
		}
		
		[Browsable(false)]
		public virtual int TitlePixelWidth
		{
			get
			{
				using (Graphics LGraphics = Grid.CreateGraphics())
				{
					return (Size.Round(LGraphics.MeasureString(FTitle, Grid.Font))).Width;
				}
			}
		}

		protected virtual bool ShouldSerializeTitle() { return Title != String.Empty; }
		private string FTitle;
		[Category("Appearance")]
		[Description("Text in the column header.")]
		public virtual string Title
		{
			get { return FTitle; }
			set
			{
				if (FTitle != value)
				{
					FTitle = value == null ? String.Empty : value;
					Changed();
				}
			}
		}

		private WinForms.HorizontalAlignment FHorizontalAlignment;
		[Category("Appearance")]
		[DefaultValue(WinForms.HorizontalAlignment.Left)]
		[Description("Horizontal alignment of the text.")]
		public virtual WinForms.HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set
			{
				if (FHorizontalAlignment != value)
				{
					FHorizontalAlignment = value;
					Changed();
				}
			}
		}

		private VerticalAlignment FVerticalAlignment;
		[Category("Appearance")]
		[DefaultValue(VerticalAlignment.Top)]
		[Description("The vertical alignment of the contents of a cell.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					Changed();
				}
			}
		}

		private WinForms.HorizontalAlignment FHeaderTextAlignment;
		[Category("Appearance")]
		[DefaultValue(WinForms.HorizontalAlignment.Left)]
		[Description("Horizontal alignment of header text.")]
		public WinForms.HorizontalAlignment HeaderTextAlignment
		{
			get { return FHeaderTextAlignment; }
			set
			{
				if (FHeaderTextAlignment != value)
				{
					FHeaderTextAlignment = value;
					Changed();
				}
			}
		}

		private bool ShouldSerializeForeColor() { return FForeColor != DBGrid.DefaultForeColor; }
		private Color FForeColor;
		[Category("Appearance")]
		[Description("Column foreground color.")]
		public Color ForeColor
		{
			get { return FForeColor; } 
			set
			{
				if (FForeColor != value)
				{
					FForeColor = value;
					Changed();
				}
			}
		}

		private bool ShouldSerializeBackColor() { return FBackColor != DBGrid.DefaultBackColor; }
		private Color FBackColor;
		[Category("Appearance")]
		[Description("Column background color.")]
		public Color BackColor
		{
			get { return FBackColor; }
			set
			{
				if (FBackColor != value)
				{
					FBackColor = value;
					Changed();
				}
			}
		}

		protected void VisibleChanged()
		{
			if (OnVisibleChanged != null)
				OnVisibleChanged(this, EventArgs.Empty);
		}

		public event EventHandler OnVisibleChanged;

		private bool FVisible;
		[DefaultValue(true)]
		[Category("Appearance")]
		public virtual bool Visible
		{
			get { return FVisible; }
			set
			{
				if (FVisible != value)
				{
					FVisible = value;
					VisibleChanged();
					Changed();
				}	
			}
		}

		[Browsable(false)]
		public Rectangle ClientRectangle
		{
			get
			{
				if (Grid != null)
					foreach (HeaderColumn LHeader in Grid.Header)
						if (LHeader.Column == this)
							return new Rectangle(LHeader.Offset, Grid.HeaderHeight, LHeader.Width, Grid.Height);
				return Rectangle.Empty;
			}
		}

		public event EventHandler OnChanged;
		protected virtual void Changed()
		{
			if (Grid != null)
				Grid.InternalColumnChanged(this);
			if (OnChanged != null)
				OnChanged(this, EventArgs.Empty);
		}

		protected Rectangle CellRectangleAt(int ARowIndex)
		{
			if (ARowIndex >= 0)
			{
				Rectangle LColumnRect = ClientRectangle;
				GridRow LRow = Grid.Rows[ARowIndex];
				return new Rectangle(LColumnRect.X, LRow.ClientRectangle.Y, LColumnRect.Width, LRow.ClientRectangle.Height);
			}
			else
				throw new ControlsException(ControlsException.Codes.RowIndexOutOfRange);
		}

		//If we had multiple inheritance these would not be needed.
		protected internal virtual void AddingControl(WinForms.Control AControl, int AIndex) {}

		protected internal virtual void RemovingControl(WinForms.Control AControl, int AIndex) {}

		public virtual bool CanToggle()
		{
			return false;
		}

		public virtual void Toggle()
		{
			// abstract
		}
	}

	public class ControlsList : DisposableList
	{
		public ControlsList(GridColumn AColumn): base(true, false)
		{
			FColumn = AColumn;
		}

		private GridColumn FColumn;

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			FColumn.AddingControl((WinForms.Control)AValue, AIndex);
		}

		protected override void Removing(object AValue, int AIndex)
		{
			FColumn.RemovingControl((WinForms.Control)AValue, AIndex);
			base.Removing(AValue, AIndex);
		}
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.ActionColumnTypeConverter))]
	public class ActionColumn : GridColumn
	{
		public const string CDefaultControlClassName = "Alphora.Dataphor.DAE.Client.Controls.SpeedButton,Alphora.Dataphor.DAE.Client.Controls";
		/// <summary> Initializes a new instance of a non data-bound ActionColumn. </summary>
		public ActionColumn() : base()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of an ActionColumn. </summary>
		/// <param name="AControlText"> Text for the control. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		/// <param name="AHorizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="ATitleAlignment"> Header contents horizontal alignment. </param>
		/// <param name="AVerticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="ABackColor"> Background color for this column. </param>
		/// <param name="AVisible"> Column is visible. </param>
		/// <param name="AHeader3DStyle"> Header paint style. </param>
		/// <param name="AHeader3DSide"> Header paint side. </param>
		/// <param name="AMinRowHeight"> Minimum height of a row. </param>
		/// <param name="AMaxRowHeight"> Maximum height of a row. </param>
		/// <param name="AFont"> Column Font </param>
		/// <param name="AForeColor"> Foreground color </param>
		public ActionColumn
			(
			string AControlText,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment ATitleAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor,
			string AControlClassName
			) : base(ATitle, AWidth, AHorizontalAlignment, ATitleAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			InitializeColumn();
			FControlText = AControlText;
			FControlClassName = AControlClassName;
		}

		/// <summary> Initializes a new instance of a ActionColumn given a title, and width. </summary>
		/// <param name="AControlText"> Text for the control. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		public ActionColumn (string AControlText, string ATitle, int AWidth) : base(ATitle, AWidth)
		{
			InitializeColumn();
			FControlText = AControlText;
		}

		/// <summary> Initializes a new instance of a ActionColumn given a title. </summary>
		/// <param name="AControlText"> Text for the control. </param>
		public ActionColumn (string AControlText) : base()
		{
			InitializeColumn();
			FControlText = AControlText;
		}

		/// <summary> Initializes a new instance of a ActionColumn given a button text and title. </summary>
		/// <param name="AControlText"> Text for the control. </param>
		/// <param name="ATitle"> Column title. </param>
		public ActionColumn (string AControlText, string ATitle) : base(ATitle)
		{
			InitializeColumn();
			FControlText = AControlText;
		}

		private void InitializeColumn()
		{
			FControlClassName = CDefaultControlClassName;
			FControlText = String.Empty;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FControls != null)
			{
				FControls.Clear();
				FControls = null;
			}
			base.Dispose(ADisposing);
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="AValue"> The value to measure. </param>
		/// <param name="AGraphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object AValue, Graphics AGraphics)
		{
			if (Grid != null)
			{
				SizeF LSize;
				if ((AValue != null) && (AValue is string))
					LSize = AGraphics.MeasureString((string)AValue, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				else
					LSize = AGraphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				if ((int)LSize.Height > Grid.MinRowHeight)
					return (int)LSize.Height + (Grid.Font.Height / 2);
				return (int)LSize.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		private ArrayList FControls;
		protected ArrayList Controls
		{
			get
			{
				if (FControls == null)
					FControls = new ControlsList(this);
				return FControls;
			}
		}

		private string FControlText;
		[Category("Appearance")]
		[Description("Text value for the control.")]
		public string ControlText
		{
			get { return FControlText; }
			set
			{
				if (FControlText != value)
				{
					FControlText = value;
					Changed();
				}
			}
		}

		private bool FEnabled = true;
		[DefaultValue(true)]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (FEnabled != value)
				{
					FEnabled = value;
					Changed();
				}
			}
		}

		public event EventHandler OnExecuteAction;

		protected virtual void ExecuteAction(object ASender, EventArgs AArgs)
		{
			SelectRow(Controls.IndexOf(ASender));
			if (OnExecuteAction != null)
				OnExecuteAction(this, AArgs);
		}

		private void ControlEnter(object ASender, EventArgs AArgs)
		{
			SelectRow(Controls.IndexOf(ASender));
		}

		private string FControlClassName;
		[Category("Design")]
		[DefaultValue(CDefaultControlClassName)]
		public string ControlClassName
		{
			get	{ return FControlClassName; }
			set
			{
				if (FControlClassName != value)
				{
					RecreateControls(value);
					FControlClassName = value;
				}
			}
		}

		private void RecreateControls(string AClassName)
		{
			int LControlCount = Controls.Count;
			Rectangle[] LRects = new Rectangle[LControlCount];
			WinForms.Control LControl;
			for (int i = LControlCount - 1; i >= 0; i--)
			{
				LControl = (WinForms.Control)Controls[i];
				LRects[i] = LControl.Bounds;
				Controls.RemoveAt(i);
			}
			for (int j = 0; j < LControlCount; j++)
			{
				AddControl(LRects[j]);
				((WinForms.Control)Controls[j]).Bounds = LRects[j];
			}
		}

		/// <summary> Creates an instance given a class name. </summary>
		/// <param name="AClassName"> The name of the class to instantiate</param>
		private object CreateObject(string AClassName)
		{
			return Activator.CreateInstance(AssemblyUtility.GetType(AClassName, true, true));
		}

		private WinForms.Control CreateControl(string AClassName)
		{
			return (WinForms.Control)CreateObject(AClassName);
		}

		protected virtual void SetControlProperties(WinForms.Control AControl)
		{
			AControl.Parent = Grid;			
			AControl.TabStop = false;
			if (AControl is WinForms.Button)
			{
				if (AControl.BackColor.A < 255)
					((WinForms.Button)AControl).FlatStyle = WinForms.FlatStyle.Popup;
				else
					((WinForms.Button)AControl).FlatStyle = WinForms.FlatStyle.System;
			}
			AControl.ForeColor = ForeColor;
			AControl.BackColor = BackColor;
			AControl.Font = Font;
			AControl.Text = FControlText;
			AControl.Enabled = FEnabled;
		}

		private void AddControl(Rectangle AClientRect)
		{
			WinForms.Control LControl = CreateControl(FControlClassName);
			LControl.Bounds = AClientRect;
			LControl.Enabled = FEnabled;
			Controls.Add(LControl);
		}

		private void UpdateControl(Rectangle AClientRect, int AIndex)
		{
			WinForms.Control LControl = (WinForms.Control)Controls[AIndex];
			LControl.Location = AClientRect.Location;
			LControl.Size = AClientRect.Size;
			SetControlProperties(LControl);
		}

		protected internal override void AddingControl(WinForms.Control AControl, int AIndex)
		{
			base.AddingControl(AControl, AIndex);
			AControl.Enter += new EventHandler(ControlEnter);
			AControl.Click += new EventHandler(ExecuteAction);
			SetControlProperties(AControl);
		}

		protected internal override void RemovingControl(WinForms.Control AControl, int AIndex)
		{
			if (AControl.ContainsFocus)
				if ((Grid != null) && (Grid.CanFocus))
					Grid.Focus();
			AControl.Enter -= new EventHandler(ControlEnter);
			AControl.Click -= new EventHandler(ExecuteAction);
			AControl.Parent = null;
			base.RemovingControl(AControl, AIndex);
		}

		protected internal override void RowChanged(object AValue, Rectangle ARowRectangle, int ARowIndex)
		{
			base.RowChanged(AValue, ARowRectangle, ARowIndex);
			Rectangle LColumnRect = ClientRectangle;
			if (!LColumnRect.IsEmpty)
			{
				Rectangle LControlRect = new Rectangle(LColumnRect.X, ARowRectangle.Y, LColumnRect.Width - Grid.LineWidth, ARowRectangle.Height - Grid.LineWidth);
				if (ARowIndex >= Controls.Count)
					AddControl(LControlRect);
				else
					UpdateControl(LControlRect, ARowIndex);
			}
			else
				Controls.Clear();
		}

		protected internal override void RowsChanged()
		{
			base.RowsChanged();
			if (FControls != null)
			{
				if ((Grid != null) && (Grid.Link != null) && (Grid.Rows.Count > 0))
					while (FControls.Count > Grid.Rows.Count)
						FControls.Remove(FControls[FControls.Count - 1]);
				else
					FControls.Clear();
			}
		}

		protected override void PaintCell
			(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect
			)
		{
			using (SolidBrush LBackBrush = new SolidBrush(AValue != null ? BackColor : Grid.NoValueBackColor))
				AGraphics.FillRectangle(LBackBrush, ACellRect);
		}
		
	}

	public class RowDragObject : MarshalByRefObject , IDisposableNotify
	{
		public RowDragObject(GridColumn AColumn, DAEData.Row AStartRow, Point AStartDragPoint, int ATargetIndex, Size ACenterOffset, Image AImage) : base()
		{
			FColumn = AColumn;
			FStartDragPoint = AStartDragPoint;
			FStartRow = AStartRow;
			FImage = AImage;
			FCursorCenterOffset = ACenterOffset;
			DropTargetIndex = ATargetIndex;
		}
        
		protected virtual void Dispose(bool ADisposing)
		{
			if (FStartRow != null)
				FStartRow.Dispose();
			if (FImage != null)
				FImage.Dispose();
			FStartRow = null;
			FImage = null;
			DoDispose();
		}
		
		public void Dispose()
		{
			Dispose(true);
		}

		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

		private GridColumn FColumn;
		public GridColumn Column { get { return FColumn; } }

		private Point FStartDragPoint;
		public Point StartDragPoint
		{
			get { return FStartDragPoint; }
		}

		private DAEData.Row FStartRow;
		public DAEData.Row StartRow { get { return FStartRow; } }

		private Size FCursorCenterOffset;
		public Size CursorCenterOffset { get { return FCursorCenterOffset; } }

		private int FDropTargetIndex;
		public int DropTargetIndex
		{
			get { return FDropTargetIndex; }
			set { FDropTargetIndex = value; }
		}

		private bool FTargetAbove;
		public bool TargetAbove
		{
			get { return FTargetAbove; }
			set { FTargetAbove = value; }
		}

		private Rectangle FHighlightRect;
		public Rectangle HighlightRect
		{
			get { return FHighlightRect; }
			set { FHighlightRect = value; }
		}

		private Point FImageLocation;
		public Point ImageLocation
		{
			get { return FImageLocation; }
			set { FImageLocation = value; }
		}

		private Image FImage;
		public Image Image { get { return FImage; } }
	}

	public delegate void SequenceChangeEventHandler(object ASender, DAEData.Row AFromRow, DAEData.Row AToRow, bool ABeforeRow, EventArgs AArgs);
	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.SequenceColumnTypeConverter))]
	public class SequenceColumn : GridColumn
	{
		public const int CAutoScrollHeight = 24;
		public const int CStartScrollInterval = 400;
		private Size FBeforeDragSize = WinForms.SystemInformation.DragSize;
		/// <summary> Initializes a new instance of a non data-bound ButtonColumn. </summary>
		public SequenceColumn() : base()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a SequenceColumn. </summary>
		/// <param name="AImage"> Cell image </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		/// <param name="AHorizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="ATitleAlignment"> Header contents horizontal alignment. </param>
		/// <param name="AVerticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="ABackColor"> Background color for this column. </param>
		/// <param name="AVisible"> Column is visible. </param>
		/// <param name="AHeader3DStyle"> Header paint style. </param>
		/// <param name="AHeader3DSide"> Header paint side. </param>
		/// <param name="AMinRowHeight"> Minimum height of a row. </param>
		/// <param name="AMaxRowHeight"> Maximum height of a row. </param>
		/// <param name="AFont"> Column Font </param>
		/// <param name="AForeColor"> Foreground color </param>
		public SequenceColumn
			(
			Image AImage,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment ATitleAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor
			) : base(ATitle, AWidth, AHorizontalAlignment, ATitleAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			InitializeColumn();
			FImage = AImage;
		}

		/// <summary> Initializes a new instance of a SequenceColumn given a title, and width. </summary>
		/// <param name="AImage"> Cell image. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		public SequenceColumn (Image AImage, string ATitle, int AWidth) : base(ATitle, AWidth)
		{
			InitializeColumn();
			FImage = AImage;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title. </summary>
		/// <param name="AImage"> Cell image. </param>
		public SequenceColumn (Image AImage) : base()
		{
			InitializeColumn();
			FImage = AImage;
		}

		/// <summary> Initializes a new instance of a GridColumn given a button text and title. </summary>
		/// <param name="AImage"> Cell image. </param>
		/// <param name="ATitle"> Column title. </param>
		public SequenceColumn (Image AImage, string ATitle) : base(ATitle)
		{
			InitializeColumn();
			FImage = AImage;
		}

		private void InitializeColumn()
		{
			FImage = null;
			HorizontalAlignment = WinForms.HorizontalAlignment.Center;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FImage != null)
			{
				FImage.Dispose();
				FImage = null;
			}
			base.Dispose(ADisposing);
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="AValue"> The value to measure. </param>
		/// <param name="AGraphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object AValue, Graphics AGraphics)
		{
			if (Grid != null)
			{
				SizeF LSize;
				if ((AValue != null) && (AValue is string))
					LSize = AGraphics.MeasureString((string)AValue, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				else
					LSize = AGraphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				if ((int)LSize.Height > Grid.MinRowHeight)
					return (int)LSize.Height + (Grid.Font.Height / 2);
				return (int)LSize.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		[DefaultValue(WinForms.HorizontalAlignment.Center)]
		public override WinForms.HorizontalAlignment HorizontalAlignment
		{
			get { return base.HorizontalAlignment; }
			set { base.HorizontalAlignment = value; }
		}

		private System.Drawing.Image FImage;
		[DefaultValue(null)]
		[Category("Appearance")]
		public Image Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					Changed();
				}
			}
		}

		private RowDragObject FRowDragObject;

		private void DrawDragImage(Graphics AGraphics, RowDragObject ADragObject)
		{
			using (SolidBrush LBetweenRowBrush = new SolidBrush(SystemColors.Highlight))
				AGraphics.FillRectangle(LBetweenRowBrush, ADragObject.HighlightRect);
			AGraphics.DrawImage(ADragObject.Image, ADragObject.ImageLocation);
		}

		protected internal override void AfterPaint(object ASender, WinForms.PaintEventArgs AArgs)
		{
			base.AfterPaint(ASender, AArgs);
			if (FDraggingRow && (FRowDragObject != null) && (FRowDragObject.Image != null))
				DrawDragImage(AArgs.Graphics, FRowDragObject);
		}

		protected override void PaintCell
			(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect 
			)
		{
			if (FImage != null)
			{
				Rectangle LImageRect = new Rectangle(0 , 0, FImage.Width, FImage.Height);
				LImageRect = ImageAspect.ImageAspectRectangle(LImageRect, APaddedRect);

				switch (HorizontalAlignment)
				{
					case WinForms.HorizontalAlignment.Center :
						LImageRect.Offset
							(
							(APaddedRect.Width - LImageRect.Width) / 2,
							(APaddedRect.Height - LImageRect.Height) / 2
							);
						break;
					case WinForms.HorizontalAlignment.Right :
						LImageRect.Offset
							(
							(APaddedRect.Width - LImageRect.Width),
							(APaddedRect.Height - LImageRect.Height)
							);
						break;
				}
				
				LImageRect.X += APaddedRect.X;
				LImageRect.Y += APaddedRect.Y;
				AGraphics.DrawImage(FImage, LImageRect);
			}
		}

		protected virtual Image GetDragImage(int ARowIndex, bool APaintBorder)
		{
			Rectangle LRowRect = RowRectangleAt(ARowIndex);
			Bitmap LRowBitmap;
			using (Graphics LGridGraphics = Grid.CreateGraphics())
			{
				using (Bitmap LBitmap = new Bitmap(LRowRect.Width, LRowRect.Bottom, LGridGraphics))
				{
					using (Graphics LGraphics = Graphics.FromImage(LBitmap))
						Grid.DrawCells(LGraphics, LRowRect, false);

					LRowBitmap = new Bitmap(LRowRect.Width + 1, LRowRect.Height + 1, LGridGraphics);
					using (Graphics LRowGraphics = Graphics.FromImage(LRowBitmap))
					{
						Rectangle LImageRect = new Rectangle(Point.Empty, LRowRect.Size);
						LRowGraphics.DrawImage(LBitmap, LImageRect, LRowRect, GraphicsUnit.Pixel);
						if (APaintBorder)
						{
							using (Pen LPen = new Pen(ForeColor, 1))
								LRowGraphics.DrawRectangle(LPen, LImageRect);
						}
					}
				}
			}
			return LRowBitmap;
		}

		public SequenceChangeEventHandler OnSequenceChange;
		protected virtual void SequenceChanged(DAEData.Row AFromRow, DAEData.Row AToRow, bool ABeforeRow)
		{
			FBookMark = null;
			if (OnSequenceChange != null)
				OnSequenceChange(this, AFromRow, AToRow, ABeforeRow, EventArgs.Empty);
		}

		private DAEData.Row CreateSequenceRow(int ARowIndex)
		{
			TableDataSet LDataSet = Grid.Link.DataSet as TableDataSet;
			if (LDataSet != null)
			{
				string LColumnName;
				DAESchema.RowType LRowType = new DAESchema.RowType();	
				foreach (DAESchema.OrderColumn LColumn in LDataSet.Order.Columns)
				{
					LColumnName = LColumn.Column.Name;
					LRowType.Columns.Add(new DAESchema.Column(LColumnName, LDataSet.TableVar.Columns[LColumnName].DataType));
				}

				DAEData.Row LViewRow = (DAEData.Row)RowValueAt(ARowIndex);
				DAEData.Row LRow = new DAEData.Row(LDataSet.Process.ValueManager, LRowType);
				foreach (DAESchema.OrderColumn LColumn in LDataSet.Order.Columns)
				{
					LColumnName = LColumn.Column.Name;
					LRow[LColumnName] = LViewRow[LColumnName];
				}
				return LRow;
			}
			return null;
		}

		private bool FDraggingRow;
		protected void DoRowDragDrop()
		{
			TableDataSet LDataSet = Grid.Link.DataSet as TableDataSet;
			if ((FRowDragObject != null) && (LDataSet != null) && (LDataSet.Order != null))
			{
				bool LSaveAllowDrop = Grid.AllowDrop;
				FDraggingRow = true;
				try
				{
					Grid.AllowDrop = true;
					WinForms.DataObject LDataObject = new WinForms.DataObject();
					LDataObject.SetData(FRowDragObject);
					Grid.DoDragDrop(LDataObject, WinForms.DragDropEffects.Move | WinForms.DragDropEffects.Scroll);
				}
				finally
				{
					FDraggingRow = false;
					DisposeDragDropTimer();
					Grid.AllowDrop = LSaveAllowDrop;
					if (FBookMark != null)
					{
						Grid.Link.DataSet.FindNearest(FBookMark);
						FBookMark = null;
					}
					DestroyDragObject();
					Grid.Invalidate(Grid.Region); //Hide the drag image.
				}
			}
		}

		private void DestroyDragObject()
		{
			if (FRowDragObject != null)
			{
				if (Grid != null)
				{
					Grid.DragEnter -= new WinForms.DragEventHandler(DragEnter);
					Grid.DragOver -= new WinForms.DragEventHandler(DragOver);
					Grid.DragDrop -= new WinForms.DragEventHandler(DragDrop);
				}
				FRowDragObject.Dispose();
				FRowDragObject = null;
			}
		}

		private void CreateDragObject(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			if (ARowIndex >= 0)
			{
				TableDataSet LDataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (LDataSet != null) && (LDataSet.Order != null))
				{
					Rectangle LRowRect = RowRectangleAt(ARowIndex);
					FRowDragObject = new RowDragObject(this, CreateSequenceRow(ARowIndex), new Point(AArgs.X, AArgs.Y), -1, new Size(AArgs.X - LRowRect.X, 4), GetDragImage(ARowIndex, true));
					Grid.DragDrop += new WinForms.DragEventHandler(DragDrop);
					Grid.DragEnter += new WinForms.DragEventHandler(DragEnter);
					Grid.DragOver += new WinForms.DragEventHandler(DragOver);
					WinForms.DataObject LDataObject = new WinForms.DataObject(FRowDragObject);
					UpdateDragObject(new Point(AArgs.X, AArgs.Y), LDataObject);
				}
			}
		}

		protected internal override void MouseDown(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			base.MouseDown(AArgs, ARowIndex);
			CreateDragObject(AArgs, ARowIndex);
		}

		protected internal override void MouseMove(Point APoint, int ARowIndex)
		{
			base.MouseMove(APoint, ARowIndex);
			if
				(
				(FRowDragObject != null) &&
				!FDraggingRow &&
				((Math.Abs(FRowDragObject.StartDragPoint.X - APoint.X) > FBeforeDragSize.Width) || ((Math.Abs(FRowDragObject.StartDragPoint.Y - APoint.Y) > FBeforeDragSize.Height)))
				)
				DoRowDragDrop();
		}

		protected internal override void MouseUp(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			base.MouseUp(AArgs, ARowIndex);
			DestroyDragObject();
		}

		protected virtual void DragEnter(object ASender, WinForms.DragEventArgs AArgs)
		{
			if (FDraggingRow)
			{
				AArgs.Effect = WinForms.DragDropEffects.Move;
				Point LPoint = ((WinForms.Control)ASender).PointToClient(new Point(AArgs.X, AArgs.Y));
				UpdateDragTimer(LPoint);
				UpdateDragObject(LPoint, AArgs.Data);
			}
		}

		protected virtual void DragOver(object ASender, WinForms.DragEventArgs AArgs)
		{
			if (FDraggingRow)
			{
				Point LPoint = ((WinForms.Control)ASender).PointToClient(new Point(AArgs.X, AArgs.Y));
				AArgs.Effect = WinForms.DragDropEffects.Move;
				UpdateDragTimer(LPoint);
				UpdateDragObject(LPoint, AArgs.Data);
			}
		}

		protected virtual void DragDrop(object ASender, WinForms.DragEventArgs AArgs)
		{
			DisposeDragDropTimer();
			WinForms.DataObject LDraggedData = new WinForms.DataObject(AArgs.Data);
			if (LDraggedData.GetDataPresent(typeof(RowDragObject)))
			{
				Point LPoint = ((WinForms.Control)ASender).PointToClient(new Point(AArgs.X, AArgs.Y));
				UpdateDragObject(LPoint, AArgs.Data);

				RowDragObject LDragObject = (RowDragObject)LDraggedData.GetData(typeof(RowDragObject));
				if ((LDragObject != null) && (LDragObject.DropTargetIndex >= 0))
					SequenceChanged(FRowDragObject.StartRow, CreateSequenceRow(LDragObject.DropTargetIndex), LDragObject.TargetAbove);
			}
		}

		private int GetDropTargetIndex(Point ALocation)
		{
			if (ALocation.Y > Grid.HeaderHeight)
			{
				for (int i = 0; i < Grid.Rows.Count; i++)
				{
					Rectangle LRowRect = RowRectangleAt(i);
					if ((ALocation.Y > LRowRect.Y) && (ALocation.Y <= LRowRect.Bottom))
						return i;
				}
			}
			return -1;
		}

		private void UpdateDragObject(Point ALocation, WinForms.IDataObject AData)
		{

			WinForms.DataObject LDraggedData = new WinForms.DataObject(AData);
			if (LDraggedData.GetDataPresent(typeof(RowDragObject)))
			{
				RowDragObject LDragObject = (RowDragObject)LDraggedData.GetData(typeof(RowDragObject));
				if (LDragObject != null)
				{
					Rectangle LInvalidateRect = LDragObject.HighlightRect;					
					int LTarget = GetDropTargetIndex(ALocation);
					bool LAbove = false;
					Rectangle LTargetRect = Rectangle.Empty;
					if (LTarget < 0)
					{
						if (ALocation.Y <= Grid.HeaderHeight)
						{
							if (Grid.Rows.Count >= 0)
							{
								LTarget = 0;
								LAbove = true;
								LTargetRect = RowRectangleAt(LTarget);
							}
						}
						else
						{
							if (Grid.Rows.Count >= 0)
							{
								LTarget = Grid.Rows.Count - 1;
								LAbove = false;
								LTargetRect = RowRectangleAt(LTarget);
							}
							
						}
					}
					else
					{
						LTargetRect = RowRectangleAt(LTarget);
						LAbove = ALocation.Y < (LTargetRect.Top + (LTargetRect.Height / 2));
					}

					LDragObject.TargetAbove = LAbove;
					LDragObject.DropTargetIndex = LTarget;
					if (LTarget >= 0)
					{
						if (LAbove)
							LDragObject.HighlightRect = new Rectangle(LTargetRect.X, LTargetRect.Y - 2, LTargetRect.Width, 4);
						else
							LDragObject.HighlightRect = new Rectangle(LTargetRect.X, LTargetRect.Bottom - 2, LTargetRect.Width, 4);
						LInvalidateRect = Rectangle.Union(LInvalidateRect, LDragObject.HighlightRect); 
					}

					if (LDragObject.Image != null)
					{
						Rectangle LOldImageLocRect = new Rectangle(LDragObject.ImageLocation, LDragObject.Image.Size);
						Rectangle LNewImageLocRect = new Rectangle(ALocation.X - LDragObject.CursorCenterOffset.Width, ALocation.Y + LDragObject.CursorCenterOffset.Height, LDragObject.Image.Width, LDragObject.Image.Height); 
						if (!LNewImageLocRect.Location.Equals(LDragObject.ImageLocation))
						{
							LDragObject.ImageLocation = LNewImageLocRect.Location;
							LInvalidateRect = Rectangle.Union(LInvalidateRect, Rectangle.Union(LOldImageLocRect, LNewImageLocRect));
						}
					}
					if (!LInvalidateRect.IsEmpty)
						Grid.InternalInvalidate(LInvalidateRect, true);
				}
			}

		}

		private DragDropTimer FScrollTimer = null;

		protected void CreateDragDropTimer(ScrollDirection ADirection, bool AEnable)
		{
			DisposeDragDropTimer();
			ScrollTimerInterval = CStartScrollInterval;
			FScrollTimer = new DragDropTimer(ScrollTimerInterval);
			FScrollTimer.Tick += new EventHandler(ScrollTimerTick);
			FScrollTimer.ScrollDirection = ADirection;
			FScrollTimer.Enabled = AEnable;

		}

		protected void DisposeDragDropTimer()
		{
			if (FScrollTimer != null)
			{
				try
				{
					FScrollTimer.Tick -= new EventHandler(ScrollTimerTick);
					FScrollTimer.Dispose();
				}
				finally
				{
					FScrollTimer = null;
				}
				ScrollTimerInterval = CStartScrollInterval;
			}
		}

		DAEData.Row FBookMark = null;
		private void ScrollTimerTick(object ASender, EventArgs AArgs)
		{
			if ((FBookMark == null) && (FRowDragObject != null))
				FBookMark = FRowDragObject.StartRow;

			if (FScrollTimer.ScrollDirection == ScrollDirection.Up)
			{
				if (Grid.Link.ActiveOffset > 0)
					Grid.Home();
				Grid.Link.DataSet.Prior();
			}
			else if (FScrollTimer.ScrollDirection == ScrollDirection.Down)
			{
				if (Grid.Link.ActiveOffset < (Grid.Rows.Count - 1))
					Grid.End();
				Grid.Link.DataSet.Next();
			}
			if (ScrollTimerInterval > 1)
				ScrollTimerInterval = FScrollTimer.Interval - (CStartScrollInterval / 2) > 1 ? FScrollTimer.Interval - (CStartScrollInterval / 2) : 1;
		}

		private int GetUpperAutoScrollHeight()
		{
			int UpperScrollHeight = CAutoScrollHeight;
			if (Grid.HeaderHeight > 0)
				UpperScrollHeight = Math.Min(CAutoScrollHeight, Grid.HeaderHeight);
			else
			{
				if (Grid.Rows.Count > 0)
					UpperScrollHeight = Math.Min(CAutoScrollHeight, (RowRectangleAt(0).Height / 2));
			}
			return UpperScrollHeight;
		}

		private int FScrollTimerInterval = CStartScrollInterval;

		protected internal int ScrollTimerInterval
		{
			get { return FScrollTimerInterval; }
			set
			{
				if (FScrollTimerInterval != value)
				{
					FScrollTimerInterval = value;
					if (FScrollTimer != null)
						FScrollTimer.Interval = value;
				}
			}
		}

		private void UpdateDragTimer(Point APoint)
		{
			Rectangle LColumnRectangle = ClientRectangle;
			if (LColumnRectangle.Height < (2 * CAutoScrollHeight + 1))
				return;

			if (APoint.Y >= (Grid.Height - CAutoScrollHeight))
			{
				if (FScrollTimer == null)
					CreateDragDropTimer(ScrollDirection.Down, true);
				else 
				{
					if (FScrollTimer.ScrollDirection != ScrollDirection.Down)
						FScrollTimer.ScrollDirection = ScrollDirection.Down;
				}
			}
			else if (APoint.Y < GetUpperAutoScrollHeight())
			{
				if (FScrollTimer == null)
					CreateDragDropTimer(ScrollDirection.Up, true);
				else 
				{
					if (FScrollTimer.ScrollDirection != ScrollDirection.Up)
						FScrollTimer.ScrollDirection = ScrollDirection.Up;
				}
			}
			else
				DisposeDragDropTimer();
		}
	}

	/// <summary> Base class for all data-aware grid columns. </summary>
	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.DataColumnTypeConverter))]
	public abstract class DataColumn : GridColumn, IColumnNameReference
	{
		public const int COrderIndicatorSpacing = 2; //Pixels.
		public const int COrderIndicatorPixelWidth = 10;

		public DataColumn()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a GridColumn. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		/// <param name="AHorizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="ATitleAlignment"> Header contents alignment. </param>
		/// <param name="AVerticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="ABackColor"> Background color for this column. </param>
		/// <param name="AVisible"> Column is visible. </param>
		/// <param name="AHeader3DStyle"> Header paint style. </param>
		/// <param name="AHeader3DSide"> Header paint side. </param>
		/// <param name="AMinRowHeight"> Minimum height of a row. </param>
		/// <param name="AMaxRowHeight"> Maximum height of a row. </param>
		/// <param name="AFont"> Column Font </param>
		/// <param name="AForeColor"> Foreground color of this column. </param> 
		public DataColumn
			(
			string AColumnName,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment ATitleAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor
			) : base(ATitle, AWidth, AHorizontalAlignment, ATitleAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			InitializeColumn();
			FColumnName = AColumnName;
		}

		/// <summary> Used by the grid to create default columns. </summary>
		/// <param name="AGrid"> The DBGrid the collection belongs to. </param>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		protected internal DataColumn(DBGrid AGrid, string AColumnName) : base(AGrid)
		{
			InitializeColumn();
			FIsDefaultGridColumn = true;
			FColumnName = AColumnName;
		}

		/// <summary> Initializes a new instance of a GridColumn given a column name. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		public DataColumn (string AColumnName) : base(AColumnName)
		{
			InitializeColumn();
			FColumnName = AColumnName;
		}

		/// <summary> Initializes a new instance of a GridColumn given a column name and title. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		public DataColumn (string AColumnName, string ATitle) : base(ATitle)
		{
			InitializeColumn();
			FColumnName = AColumnName;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title, and width. </summary>
		/// <param name="AColumnName"> Column to link to. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		public DataColumn (string AColumnName, string ATitle, int AWidth) : base(ATitle, AWidth)
		{
			InitializeColumn();
			FColumnName = AColumnName;
		}

		private void InitializeColumn()
		{
			FIsDefaultGridColumn = false;
			FColumnName = String.Empty;
		}

		protected override bool ShouldSerializeTitle() { return Title != ColumnName; }
		[Category("Appearance")]
		[Description("Text in the column header.")]
		public override string Title
		{
			get { return base.Title == String.Empty ? Schema.Object.Dequalify(FColumnName) : base.Title; }
			set
			{
				if (Title != value)
				{
					base.Title = value;
					if ((Grid != null) && (Width == Grid.DefaultColumnWidth) && (TitlePixelWidth > Width))
					{
						Width = TitlePixelWidth;
						Changed();
					}
				}
			}
		}

		protected internal GridDataLink Link
		{
			get { return (GridColumns != null) && (GridColumns.Grid != null) ? GridColumns.Grid.Link : null; }
		}

		/// <summary> Index of the underlying views buffer column. </summary>
		protected internal int ColumnIndex(DataLink ALink)
		{
			return (ColumnName != String.Empty) && (ALink != null) && ALink.Active ? ALink.DataSet.TableType.Columns.IndexOfName(ColumnName) : -1;
		}

		internal void InternalCheckColumnExists(DataLink ALink)
		{
			if ((ColumnName != String.Empty) && ((ALink != null) && ALink.Active))
			{
				if (ColumnIndex(ALink) < 0)
					throw new ControlsException(ControlsException.Codes.DataColumnNotFound, ColumnName);
			}
		}

		private string FColumnName;
		/// <summary> Name of the view's column this column represents. </summary>
		[Category("Data")]
		[Description("Name of the column.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.GridColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FIsDefaultGridColumn)
					throw new ControlsException(ControlsException.Codes.InvalidUpdate);
				if (FColumnName != value)
				{
					FColumnName = value;
					InternalCheckColumnExists(Link);
					Changed();
				}
			}
		}

		protected internal bool FIsDefaultGridColumn;
		[Browsable(false)]
		public bool IsDefaultGridColumn { get { return FIsDefaultGridColumn; } }
		
		protected internal override void AddingToGrid(GridColumns AGridColumns)
		{
			InternalCheckColumnExists(AGridColumns.Grid != null ? AGridColumns.Grid.Link : null);
			base.AddingToGrid(AGridColumns);
		}

		[Browsable(false)]
		public bool InAscendingOrder
		{
			get
			{
				TableDataSet LDataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (LDataSet != null) && (LDataSet.Order != null))
				{
					Schema.Order LOrder = LDataSet.Order;
					if (LOrder.Columns.IndexOf(ColumnName) > -1)
						return ((Schema.OrderColumn)LOrder.Columns[ColumnName]).Ascending;
				}
				return false;
			}
		}

		[Browsable(false)]
		public bool InDescendingOrder
		{
			get
			{
				TableDataSet LDataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (LDataSet != null) && (LDataSet.Order != null))
				{
					Schema.Order LOrder = LDataSet.Order;
					if (LOrder.Columns.IndexOf(ColumnName) > -1)
						return !((Schema.OrderColumn)LOrder.Columns[ColumnName]).Ascending;
				}
				return false;
			}
		}

		[Browsable(false)]
		public bool InOrder
		{
			get
			{
				TableDataSet LDataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (LDataSet != null) && (LDataSet.Order != null))
					return LDataSet.Order.Columns.IndexOf(ColumnName) >= 0;
				return false;
			}
		}

		public override int TitlePixelWidth
		{
			get
			{
				return !InOrder ? base.TitlePixelWidth : base.TitlePixelWidth + COrderIndicatorPixelWidth;
			}
		}

		public override int NaturalHeight(object AValue, Graphics AGraphics)
		{
			//Default measure as text without wordwrap.
			if ((Grid != null) && (AValue != null) && (AValue is DAEData.Scalar))
			{
				SizeF LSize;
				if ((AValue != null) && (AValue is string))
				{
					DAEData.Scalar LScalar = (DAEData.Scalar)AValue;
					LSize = AGraphics.MeasureString(LScalar.AsDisplayString, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				}
				else
					LSize = AGraphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				if ((int)LSize.Height > Grid.MinRowHeight)
					return (int)LSize.Height + (Grid.Font.Height / 2);
				return (int)LSize.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		protected internal override void PaintHeaderCell(Graphics AGraphics, Rectangle ACellRect, StringFormat AFormat, System.Drawing.Color AFixedColor)
		{
			base.PaintHeaderCell(AGraphics, ACellRect, AFormat, AFixedColor);
			//Paint the order indicator if applicable...
			if ((InAscendingOrder) || (InDescendingOrder))
			{
				using (SolidBrush LBrush = new SolidBrush(Color.FromArgb(0, AFixedColor)))
				{
					Rectangle LIndicatorRect;
					Rectangle LCellWorkArea = Rectangle.Inflate(ACellRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height);
					Size LTextSize = Size.Round(AGraphics.MeasureString(Title, Grid.Font));
					if ((AFormat.Alignment == StringAlignment.Near) || (AFormat.Alignment == StringAlignment.Center))
					{
						if ((LTextSize.Width + COrderIndicatorSpacing) > (LCellWorkArea.Width - COrderIndicatorPixelWidth))
							LIndicatorRect = new Rectangle(LCellWorkArea.Right - COrderIndicatorPixelWidth, LCellWorkArea.Top, COrderIndicatorPixelWidth, LCellWorkArea.Height);
						else
							LIndicatorRect = new Rectangle(LCellWorkArea.Left + (LTextSize.Width + COrderIndicatorSpacing), LCellWorkArea.Top, COrderIndicatorPixelWidth, LCellWorkArea.Height);
					}
					else
					{
						if ((LTextSize.Width + COrderIndicatorSpacing) > (LCellWorkArea.Width - COrderIndicatorPixelWidth))
							LIndicatorRect = new Rectangle(LCellWorkArea.Left, LCellWorkArea.Top, COrderIndicatorPixelWidth, LCellWorkArea.Height);
						else
						{
							int LProposedLeft = LCellWorkArea.Right - (LTextSize.Width + COrderIndicatorSpacing) - COrderIndicatorPixelWidth;
							LIndicatorRect = new Rectangle(LProposedLeft >= LCellWorkArea.Left ? LProposedLeft : LCellWorkArea.Left, LCellWorkArea.Top, COrderIndicatorPixelWidth, LCellWorkArea.Height);
						}	
					}
					LIndicatorRect.Height = Grid.Font.Height <= LIndicatorRect.Height ? Grid.Font.Height : LIndicatorRect.Height;
					if (LIndicatorRect.Width < Width)
					{
						AGraphics.FillRectangle(LBrush, LIndicatorRect);
						LIndicatorRect = Rectangle.Inflate(LIndicatorRect, 0, -4);
						if (InAscendingOrder)
						{
							Point[] LTriangle =
							{
								new Point(LIndicatorRect.Left + (LIndicatorRect.Width / 2), LIndicatorRect.Top),
								new Point(LIndicatorRect.Left, LIndicatorRect.Bottom),
								new Point(LIndicatorRect.Right, LIndicatorRect.Bottom)
							};
							LBrush.Color = Grid.OrderIndicatorColor;
							AGraphics.FillPolygon(LBrush, LTriangle);
						}
						else if (InDescendingOrder)
						{
							LIndicatorRect.Height -= 1;
							LIndicatorRect.Width -= 2;
							Point[] LTriangle =
							{	
								new Point(LIndicatorRect.Left + (LIndicatorRect.Width / 2), LIndicatorRect.Bottom),
								new Point(LIndicatorRect.Left, LIndicatorRect.Top),
								new Point(LIndicatorRect.Right, LIndicatorRect.Top)
							};
							LBrush.Color = Grid.OrderIndicatorColor;
							AGraphics.FillPolygon(LBrush, LTriangle);
						}
					}
				}
			}
		}
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.TextColumnTypeConverter))]
	public class TextColumn : DataColumn
	{
		public TextColumn() : base() {}

		/// <summary> Initializes a new instance of a GridColumn. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		/// <param name="AHorizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="ATitleAlignment"> Header contents alignment. </param>
		/// <param name="AVerticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="ABackColor"> Background color for this column. </param>
		/// <param name="AVisible"> Column is visible. </param>
		/// <param name="AHeader3DStyle"> Header paint style. </param>
		/// <param name="AHeader3DSide"> Header paint side. </param>
		/// <param name="AMinRowHeight"> Minimum height of a row. </param>
		/// <param name="AMaxRowHeight"> Maximum height of a row. </param>		
		/// <param name="AFont"> Column Font </param>
		/// <param name="AForeColor"> Foreground color of this column. </param> 
		/// <param name="AWordWrap"> Automatically wrap words to the next line in a row when necessary. </param>
		/// <param name="AVerticalText"> Layout text vertically in each row. </param>
		public TextColumn
			(
			string AColumnName,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment ATitleAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor,
			bool AWordWrap,
			bool AVerticalText
			) : base(AColumnName, ATitle, AWidth, AHorizontalAlignment, ATitleAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			InitializeColumn();
			FWordWrap = AWordWrap;
			FVerticalText = AVerticalText;
		}

		/// <summary> Used by the grid to create default columns. </summary>
		/// <param name="AGrid"> The DBGrid the collection belongs to. </param>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		protected internal TextColumn(DBGrid AGrid, string AColumnName) : base(AGrid, AColumnName)
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a Text GridColumn given a column name. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		public TextColumn (string AColumnName) : base(AColumnName)
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a Text GridColumn given a column name and title. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		public TextColumn (string AColumnName, string ATitle) : base(AColumnName, ATitle)
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a Text GridColumn given a title, and width. </summary>
		/// <param name="AColumnName"> Column to link to. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		public TextColumn (string AColumnName, string ATitle, int AWidth) : base(AColumnName, ATitle, AWidth)
		{
			InitializeColumn();
		}

		private void InitializeColumn()
		{
			FWordWrap = false;
			FVerticalText = false;
		}

		protected virtual StringFormat GetStringFormat()
		{
			StringFormat LStringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			if (!FWordWrap)
				LStringFormat.FormatFlags |= StringFormatFlags.NoWrap;
			if (FVerticalText)
				LStringFormat.FormatFlags |= StringFormatFlags.DirectionVertical;
			LStringFormat.Alignment = AlignmentConverter.ToStringAlignment(HorizontalAlignment);
			return LStringFormat;
		}

		private bool FWordWrap;
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool WordWrap
		{
			get { return FWordWrap; }
			set
			{
				if (FWordWrap != value)
				{
					FWordWrap = value;
					Changed();
				}
			}
		}

		private bool FVerticalText;
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool VerticalText
		{
			get { return FVerticalText; }
			set
			{
				if (FVerticalText != value)
				{
					FVerticalText = value;
					Changed();
				}
			}
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="AValue"> The value to measure. </param>
		/// <param name="AGraphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object AValue, Graphics AGraphics)
		{
			if ((Grid != null) && (AValue != null) && (AValue is DAEData.Scalar))
			{
				SizeF LSize;
				if (AValue != null)
				{
					DAEData.Scalar LScalar = (DAEData.Scalar)AValue;
					LSize = AGraphics.MeasureString(LScalar.AsDisplayString, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				}
				else
					LSize = AGraphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				if ((int)LSize.Height > Grid.MinRowHeight)
					return (int)LSize.Height + (Grid.Font.Height / 2);
				return (int)LSize.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}
		
		private void DoPaintCell
			(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect
			)
		{
			using (SolidBrush LBackBrush = new SolidBrush(AValue != null ? BackColor : Grid.NoValueBackColor))
			{
				using (SolidBrush LForeBrush = new SolidBrush(ForeColor))
				{
					AGraphics.FillRectangle(LBackBrush, ACellRect);
					string LText = String.Empty;
					if (AValue is string)
						LText = (string)AValue;
					else
					{
						if (AValue != null)
							LText = AValue.ToString();
					}
					AGraphics.DrawString
						(
						#if DEBUGOFFSETS
						"VAO=" + Link.DataSet.ActiveOffset.ToString() + ',' + "LFO=" + Link.FirstOffset.ToString() + ',' + "LLO=" + Link.LastOffset.ToString() + ',' + "LBC=" + Link.BufferCount.ToString() + ',' + "LAO=" + Link.ActiveOffset.ToString() + ',' + "GRI=" + ARowIndex.ToString() + ',' + "ExtraRowOffset=" + ((GridDataLink)Link).ExtraRowOffset.ToString() + ',' + LText,
						#else
						LText,
						#endif
						Font,
						LForeBrush,
						AVerticalMeasuredRect,
						GetStringFormat()
						);
				}
			}
		}

		protected override void PaintCell
			(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect
			)
		{
			if (AValue is DAEData.Row)
			{
				DAEData.Row LRow = (DAEData.Row)AValue;
				int LColumnIndex = ColumnIndex(Link);
				if ((LColumnIndex >= 0) && LRow.HasValue(LColumnIndex))
					DoPaintCell(((DAEData.Scalar)LRow.GetValue(LColumnIndex)).AsDisplayString, AIsSelected, ARowIndex, AGraphics, ACellRect, APaddedRect, AVerticalMeasuredRect); 
				else
					DoPaintCell(null, AIsSelected, ARowIndex, AGraphics, ACellRect, APaddedRect, AVerticalMeasuredRect);
			}
			else
				DoPaintCell(AValue, AIsSelected, ARowIndex, AGraphics, ACellRect, APaddedRect, AVerticalMeasuredRect);
		}
		
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.LinkColumnTypeConverter))]
	public class LinkColumn : DataColumn
	{
		/// <summary> Initializes a new instance of a GridColumn. </summary>
		public LinkColumn() : base() {}

		/// <summary> Initializes a new instance of a LinkColumn. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		/// <param name="AHorizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="AHeaderTextAlignment"> Header contents alignment. </param>
		/// <param name="AVerticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="ABackColor"> Background color for this column. </param>
		/// <param name="AVisible"> Column is visible. </param>
		/// <param name="AHeader3DStyle"> Header paint style. </param>
		/// <param name="AHeader3DSide"> Header paint side. </param>
		/// <param name="AMinRowHeight"> Minimum height of a row. </param>
		/// <param name="AMaxRowHeight"> Maximum height of a row. </param>
		/// <param name="AFont"> Column Font </param>
		/// <param name="AForeColor"> Foreground color of this column. </param> 
		/// <param name="AWordWrap"> Automatically wrap words to the next line in a row when necessary. </param>
		public LinkColumn
			(
			string AColumnName,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment AHeaderTextAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor,
			bool AWordWrap
			) : base(AColumnName, ATitle, AWidth, AHorizontalAlignment, AHeaderTextAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			FWordWrap = AWordWrap;
		}

		/// <summary> Initializes a new instance of a LinkColumn given a column name, title, and width. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		/// <param name="ATitle"> Column title. </param>
		/// <param name="AWidth"> Column width in pixels. </param>
		public LinkColumn (string AColumnName, string ATitle, int AWidth) : base(AColumnName, ATitle, AWidth) {}

		/// <summary> Initializes a new instance of a LinkColumn given a column name and title. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		/// <param name="ATitle"> Column title. </param>
		public LinkColumn (string AColumnName, string ATitle) : base(AColumnName, ATitle) {}

		/// <summary> Initializes a new instance of a GridColumn given a column name. </summary>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		public LinkColumn (string AColumnName) : base(AColumnName) {}
		
		///<summary> Used by the grid to create default columns. </summary>
		/// <param name="AGrid"> The DBGrid the collection belongs to. </param>
		/// <param name="AColumnName"> Name of the view's column this column represents. </param>
		protected internal LinkColumn(DBGrid AGrid, string AColumnName) : base(AGrid, AColumnName) {}

		protected override void Dispose(bool ADisposing)
		{
			if (FLinks != null)
			{
				FLinks.Clear();
				FLinks = null;
			}
			base.Dispose(ADisposing);
		}

		protected virtual StringFormat GetStringFormat()
		{
			StringFormat LStringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			if (!FWordWrap)
				LStringFormat.FormatFlags |= StringFormatFlags.NoWrap;
			LStringFormat.Alignment = AlignmentConverter.ToStringAlignment(HorizontalAlignment);
			return LStringFormat;
		}

		private bool FWordWrap = false;
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool WordWrap
		{
			get { return FWordWrap; }
			set
			{
				if (FWordWrap != value)
				{
					FWordWrap = value;
					Changed();
				}
			}
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="AValue"> The value to measure. </param>
		/// <param name="AGraphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object AValue, Graphics AGraphics)
		{
			if ((Grid != null) && (AValue is DAEData.Scalar))
			{
				SizeF LSize;
				if (AValue != null)
					LSize = AGraphics.MeasureString(((DAEData.Scalar)AValue).AsDisplayString, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				else
					LSize = AGraphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				if ((int)LSize.Height > Grid.MinRowHeight)
					return (int)LSize.Height + (Grid.Font.Height / 2);
				return (int)LSize.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		private ArrayList FLinks;
		protected ArrayList Links
		{
			get
			{
				if (FLinks == null)
					FLinks = new ControlsList(this);
				return FLinks;
			}
		}

		public event WinForms.LinkLabelLinkClickedEventHandler OnExecuteLink;

		protected virtual void ExecuteLink(object ASender, WinForms.LinkLabelLinkClickedEventArgs AArgs)
		{
			SelectRow(Links.IndexOf(ASender));
			if (OnExecuteLink != null)
				OnExecuteLink(this, AArgs);
		}

		private void ControlClick(object ASender, EventArgs AArgs)
		{
			SelectRow(Links.IndexOf(ASender));
		}

		private ContentAlignment GetContentAlignment()
		{
			if (HorizontalAlignment == WinForms.HorizontalAlignment.Left)
			{
				switch (VerticalAlignment)
				{
					case VerticalAlignment.Top : return ContentAlignment.TopLeft;
					case VerticalAlignment.Middle : return ContentAlignment.MiddleLeft;
					case VerticalAlignment.Bottom : return ContentAlignment.BottomLeft;
				}					
			}
			else if (HorizontalAlignment == WinForms.HorizontalAlignment.Center)
			{
				switch (VerticalAlignment)
				{
					case VerticalAlignment.Top : return ContentAlignment.TopCenter;
					case VerticalAlignment.Middle : return ContentAlignment.MiddleCenter;
					case VerticalAlignment.Bottom : return ContentAlignment.BottomCenter;
				}
			}
			else if (HorizontalAlignment == WinForms.HorizontalAlignment.Right)
			{
				switch (VerticalAlignment)
				{
					case VerticalAlignment.Top : return ContentAlignment.TopRight;
					case VerticalAlignment.Middle : return ContentAlignment.MiddleRight;
					case VerticalAlignment.Bottom : return ContentAlignment.BottomRight;
				}
			}
			return ContentAlignment.MiddleCenter;
		}

		protected virtual void SetControlProperties(WinForms.Control AControl)
		{
			AControl.ForeColor = ForeColor;
			if (BackColor.A < 255)
				AControl.BackColor = BackColor;
			else
				AControl.BackColor = Color.FromArgb(60, BackColor);
			AControl.Font = Font;
			((WinForms.LinkLabel)AControl).TextAlign = GetContentAlignment();
		}
		
		protected internal override void AddingControl(WinForms.Control AControl, int AIndex)
		{
			base.AddingControl(AControl, AIndex);
			AControl.Click += new EventHandler(ControlClick);
			((WinForms.LinkLabel)AControl).LinkClicked += new WinForms.LinkLabelLinkClickedEventHandler(ExecuteLink);
			AControl.Parent = Grid;
			AControl.TabStop = false;
			SetControlProperties(AControl);
		}

		protected internal override void RemovingControl(WinForms.Control AControl, int AIndex)
		{
			if (AControl.ContainsFocus)
				if ((Grid != null) && (Grid.CanFocus))
					Grid.Focus();
			AControl.Click -= new EventHandler(ControlClick);
			((WinForms.LinkLabel)AControl).LinkClicked -= new WinForms.LinkLabelLinkClickedEventHandler(ExecuteLink);
			AControl.Parent = null;
			base.RemovingControl(AControl, AIndex);
		}

		private void AddControl(Rectangle AClientRect, object AValue)
		{
			WinForms.Control LControl = new WinForms.LinkLabel();
			LControl.Bounds = AClientRect;
			LControl.Text = (string)AValue;
			Links.Add(LControl);
		}

		private void UpdateControl(Rectangle AClientRect, int AIndex, object AValue)
		{
			WinForms.Control LControl = (WinForms.Control)Links[AIndex];
			LControl.Bounds = AClientRect;
			LControl.Text = (string)AValue;
			SetControlProperties(LControl);
		}

		protected internal override void RowChanged(object AValue, Rectangle ARowRectangle, int ARowIndex)
		{
			base.RowChanged(AValue, ARowRectangle, ARowIndex);
			Rectangle LColumnRect = ClientRectangle;
			if (!LColumnRect.IsEmpty)
			{
				string LValue = String.Empty;
				if (AValue is DAEData.Row)
				{
					DAEData.Row LRow = (DAEData.Row)AValue;
					int LColumnIndex = ColumnIndex(Link);
					if (LRow.HasValue(LColumnIndex))
						LValue = ((DAEData.Scalar)LRow.GetValue(LColumnIndex)).AsDisplayString;
				}

				Rectangle LControlRect = new Rectangle(LColumnRect.X, ARowRectangle.Y, LColumnRect.Width, ARowRectangle.Height);
				if (ARowIndex >= Links.Count)
					AddControl(Rectangle.Inflate(LControlRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height), LValue);
				else
					UpdateControl(Rectangle.Inflate(LControlRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height), ARowIndex, LValue);
			}
			else
				Links.Clear();
		}

		protected internal override void RowsChanged()
		{
			base.RowsChanged();
			//Dispose extra LinkLabels.
			if (FLinks != null)
			{
				if ((Grid != null) && (Grid.Link != null) && (Grid.Rows.Count > 0))
					while (FLinks.Count > Grid.Rows.Count)
						FLinks.Remove(FLinks[FLinks.Count - 1]);
				else
					FLinks.Clear();
			}
		}

		protected override void PaintCell
		(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect
		)
		{
			using (SolidBrush LBackBrush = new SolidBrush(AValue != null ? BackColor : Grid.NoValueBackColor))
				AGraphics.FillRectangle(LBackBrush, ACellRect);
		}

	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.CheckBoxColumnTypeConverter))]
	public class CheckBoxColumn : DataColumn
	{
		public const int CStandardCheckSize = 13;

		public CheckBoxColumn() : base()
		{
			InitializeColumn();
		}

		protected internal CheckBoxColumn(DBGrid AGrid, string AColumnName) : base(AGrid, AColumnName)
		{
			InitializeColumn();
		}

		public CheckBoxColumn
		(
			string AColumnName,
			string ATitle,
			int AWidth,
			int AAutoUpdateInterval,
			bool AReadOnly
		) : base(AColumnName, ATitle, AWidth)
		{
			InitializeColumn();
			FReadOnly = AReadOnly;
			FAutoUpdateInterval = AAutoUpdateInterval;
		}

		public CheckBoxColumn
		(
			string AColumnName,
			string ATitle,
			int AAutoUpdateInterval,
			bool AReadOnly
		) : base(AColumnName, ATitle)
		{
			InitializeColumn();
			FReadOnly = AReadOnly;
			FAutoUpdateInterval = AAutoUpdateInterval;
		}

		public CheckBoxColumn
		(
			string AColumnName,
			int AAutoUpdateInterval,
			bool AReadOnly
		) : base(AColumnName)
		{
			InitializeColumn();
			FReadOnly = AReadOnly;
			FAutoUpdateInterval = AAutoUpdateInterval;
		}

		public CheckBoxColumn
		(
			string AColumnName,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment AHeaderTextAlignment,
			VerticalAlignment AVerticalAlignment, 
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor,
			int AAutoUpdateInterval,
			bool AReadOnly
		) : base (AColumnName, ATitle, AWidth, AHorizontalAlignment, AHeaderTextAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			InitializeColumn();
			FReadOnly = AReadOnly;
			FAutoUpdateInterval = AAutoUpdateInterval;
		}

		private void InitializeColumn()
		{
			FReadOnly = true;
			FAutoUpdateInterval = 200;
			FAutoUpdateTimer = new WinForms.Timer();
			FAutoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			FAutoUpdateTimer.Enabled = false;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				FAutoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
				FAutoUpdateTimer.Dispose();
				FAutoUpdateTimer = null;
			}
			base.Dispose(ADisposing);
		}

		private WinForms.Timer FAutoUpdateTimer;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected WinForms.Timer AutoUpdateTimer
		{
			get { return FAutoUpdateTimer; }
		}

		private int FAutoUpdateInterval;
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

		private bool FReadOnly;
		/// <summary> Read only state of the column. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set 
			{
				if (value != FReadOnly)
				{
					if (Grid != null)
						Grid.Invalidate();
					FReadOnly = value; 
				}
			}
		}

		public virtual bool GetReadOnly()
		{
			return FReadOnly || ((Grid != null) && Grid.ReadOnly);
		}

		protected Rectangle GetCheckboxRectAt(int ARowIndex)
		{
			Rectangle LCellRect = Rectangle.Inflate(CellRectangleAt(ARowIndex), -Grid.CellPadding.Width, -Grid.CellPadding.Height);
			int LHalfHeightOfCheckBox = (Grid.MinRowHeight / 4) + 1;
			return new Rectangle
			(
				LCellRect.X + (LCellRect.Width / 2) - LHalfHeightOfCheckBox,
				LCellRect.Y + (LCellRect.Height / 2) - LHalfHeightOfCheckBox,
				2 * LHalfHeightOfCheckBox,
				2 * LHalfHeightOfCheckBox
			);
		}

		private void PaintReadOnlyChecked(Graphics AGraphics, Rectangle ARect, WinForms.ButtonState AState)
		{
			WinForms.ControlPaint.DrawBorder3D(AGraphics, ARect, WinForms.Border3DStyle.SunkenOuter);
			ARect.Inflate(-1, -1);
			using (SolidBrush LBrush = new SolidBrush(SystemColors.ControlLight))
			{
				AGraphics.FillRectangle(LBrush, ARect);
				ARect.Inflate(-2, -2);
				if ((AState & WinForms.ButtonState.Inactive) == 0)
				{
					WinForms.ControlPaint.DrawBorder3D(AGraphics, ARect, WinForms.Border3DStyle.RaisedOuter);
					ARect.Inflate(-1, -1);
					if ((AState & WinForms.ButtonState.Checked) != 0)
						LBrush.Color = Color.Navy;
					else
						LBrush.Color = SystemColors.ControlDark;
					AGraphics.FillRectangle(LBrush, ARect);
				}
			}
		}

		protected override void PaintCell
		(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect
		)
		{
			DAEData.Row LRow = AValue as DAEData.Row;
			if (LRow != null)
			{
				WinForms.ButtonState LButtonState;
				int LColumnIndex = ColumnIndex(Link);
				if (LRow.HasValue(LColumnIndex))
					LButtonState = ((DAEData.Scalar)LRow.GetValue(LColumnIndex)).AsBoolean ? WinForms.ButtonState.Checked : WinForms.ButtonState.Normal;
				else
					LButtonState = WinForms.ButtonState.Inactive;

				Size LCheckSize = WinForms.SystemInformation.MenuCheckSize;
				Rectangle LPaintRect = 
					new Rectangle
					(
						new Point
						(
							ACellRect.X + ((ACellRect.Width - LCheckSize.Width) / 2),
							ACellRect.Y + ((ACellRect.Height - LCheckSize.Height) / 2)
						),
						LCheckSize
					);

				if (GetReadOnly())
					PaintReadOnlyChecked(AGraphics, LPaintRect, LButtonState);
				else
					WinForms.ControlPaint.DrawCheckBox(AGraphics, LPaintRect, LButtonState);
			}

		}

		protected void SaveRequested(DAEClient.DataLink ALink, DAEClient.DataSet ADataSet)
		{
			if (FAutoUpdateTimer.Enabled)
				FAutoUpdateTimer.Stop();

			Link.OnSaveRequested -= new DAEClient.DataLinkHandler(SaveRequested);

			if ((ALink != null) && ALink.Active && (ColumnName != String.Empty))
			{
				DAEClient.DataField LField = Link.DataSet.Fields[ColumnName];

				LField.AsBoolean = !(bool)LField.AsBoolean;
				if (FOldState == DAEClient.DataSetState.Browse)
				{
					try
					{
						ADataSet.Post();
					}
					catch
					{
						ADataSet.Cancel();
						throw;
					}
				}
			}
		}

		protected void AutoUpdateElapsed(object ASender, EventArgs AArgs)
		{
			FAutoUpdateTimer.Stop();
			if (Link.Active)
				Link.DataSet.RequestSave();
		}

		private bool FAllowEdit = false;
		private object FRowMouseDownOver;
		protected internal override void MouseDown(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			base.MouseDown(AArgs, ARowIndex);
			if (ARowIndex >= 0)
			{
				FRowMouseDownOver = RowValueAt(ARowIndex);
				Rectangle LBoxRect = GetCheckboxRectAt(ARowIndex);
				FAllowEdit = CanToggle() && LBoxRect.Contains(AArgs.X, AArgs.Y);
			}
		}

		private DAEClient.DataSetState FOldState;
		protected internal override void MouseUp(WinForms.MouseEventArgs AArgs, int ARowIndex)
		{
			base.MouseUp(AArgs, ARowIndex);
			try
			{
				if (FAllowEdit && (ARowIndex >= 0) && (RowValueAt(ARowIndex) == FRowMouseDownOver))
				{
					Rectangle LBoxRect = Rectangle.Inflate(GetCheckboxRectAt(ARowIndex), 1, 1);
					if (LBoxRect.Contains(AArgs.X, AArgs.Y))
						Toggle();
				}
			}
			finally
			{
				FAllowEdit = false;
				FRowMouseDownOver = null;
			}
		}

		public override bool CanToggle()
		{
			return !GetReadOnly() && Link.Active && !Link.DataSet.IsReadOnly;
		}

		public override void Toggle()
		{
			FOldState = Link.DataSet.State;
			Link.DataSet.Edit();
			if ((Link.DataSet.State == DAEClient.DataSetState.Insert) || (Link.DataSet.State == DAEClient.DataSetState.Edit))
			{
				Link.OnSaveRequested += new DAEClient.DataLinkHandler(SaveRequested);
				FAutoUpdateTimer.Interval = FAutoUpdateInterval;
				FAutoUpdateTimer.Start();
			}
		}
	}

	[ToolboxItem(false)]
	public class ColumnDragObject
	{
		public ColumnDragObject(DBGrid AGrid, GridColumn AColumnToDrag, Point AStartDragPoint, Size ACenterOffset, Image ADragImage) : base()
		{
			FGrid = AGrid;
			FDragColumn = AColumnToDrag;
			FStartDragPoint = AStartDragPoint;
			FDragImage = ADragImage;
			FCursorCenterOffset = ACenterOffset;
			DropTargetIndex = FGrid.Columns.IndexOf(FDragColumn);
		}

		private DBGrid FGrid;
		public DBGrid Grid
		{
			get { return FGrid; }
		}

		private GridColumn FDragColumn;
		public GridColumn DragColumn
		{
			get { return FDragColumn; }
		}

		private Point FStartDragPoint;
		public Point StartDragPoint
		{
			get { return FStartDragPoint; }
		}

		private Size FCursorCenterOffset;
		public Size CursorCenterOffset
		{
			get { return FCursorCenterOffset; }
		}

		private int FDropTargetIndex;
		public int DropTargetIndex
		{
			get { return FDropTargetIndex; }
			set { FDropTargetIndex = value; }
		}

		private Rectangle FHighlightRect;
		public Rectangle HighlightRect
		{
			get { return FHighlightRect; }
			set { FHighlightRect = value; }
		}

		private Point FImageLocation;
		public Point ImageLocation
		{
			get { return FImageLocation; }
			set { FImageLocation = value; }
		}

		private Image FDragImage;
		public Image DragImage
		{
			get { return FDragImage; }
		}
	}

	/// <summary> Scrolling directions. </summary>
	/// <remarks> Valid combinations are Up Left, Up Right, Down Left, Down Right and each individual value. </remarks>
	[Flags]
	public enum ScrollDirection { Left, Right, Up, Down }

	[ToolboxItem(false)]
	public class DragDropTimer : WinForms.Timer
	{
		public DragDropTimer(int AInterval) : base()
		{
			Interval = AInterval;
		}

		private ScrollDirection FScrollDirection = ScrollDirection.Left;
		public ScrollDirection ScrollDirection
		{
			get { return FScrollDirection; }
			set { FScrollDirection = value; }
		}
	}

	public class ImageColumn : DataColumn
	{
		public ImageColumn() : base()
		{
			InitializeColumn();
		}

		public ImageColumn (string AColumnName) : base(AColumnName)
		{
			InitializeColumn();
		}

		public ImageColumn
			(
			string AColumnName,
			string ATitle,
			int AWidth
			) : base(AColumnName, ATitle, AWidth)
		{
			InitializeColumn();
		}

		public ImageColumn
			(
			string AColumnName,
			string ATitle
			) : base(AColumnName, ATitle)
		{
			InitializeColumn();
		}

		public ImageColumn
			(
			string AColumnName,
			string ATitle,
			int AWidth,
			WinForms.HorizontalAlignment AHorizontalAlignment,
			WinForms.HorizontalAlignment AHeaderTextAlignment,
			VerticalAlignment AVerticalAlignment,
			Color ABackColor,
			bool AVisible,
			WinForms.Border3DStyle AHeader3DStyle,
			WinForms.Border3DSide AHeader3DSide,
			int AMinRowHeight,
			int AMaxRowHeight,
			Font AFont,
			Color AForeColor
			) : base (AColumnName, ATitle, AWidth, AHorizontalAlignment, AHeaderTextAlignment, AVerticalAlignment, ABackColor, AVisible, AHeader3DStyle, AHeader3DSide, AMinRowHeight, AMaxRowHeight, AFont, AForeColor)
		{
			InitializeColumn();
		}

		private void InitializeColumn()
		{
			HorizontalAlignment = WinForms.HorizontalAlignment.Center;
		}

		[DefaultValue(WinForms.HorizontalAlignment.Center)]
		public override WinForms.HorizontalAlignment HorizontalAlignment
		{
			get { return base.HorizontalAlignment; }
			set { base.HorizontalAlignment = value; }
		}

		public override int NaturalHeight(object AValue, Graphics AGraphics)
		{
			if ((AValue == null) || !(AValue is DAEData.DataValue))
				return DBGrid.DefaultRowHeight;
			Image LImage;
			Stream LStream = ((DAEData.DataValue)AValue).OpenStream();
			try
			{
				MemoryStream LCopyStream = new MemoryStream();
				StreamUtility.CopyStream(LStream, LCopyStream);
				LImage = Image.FromStream(LCopyStream);
			}
			finally
			{
				LStream.Close();
			}
			Rectangle LImageRectangle = new Rectangle(0, 0, LImage.Width, LImage.Height);
			Rectangle LClientRect = new Rectangle(0, 0, Width, LImage.Height);
			LImageRectangle = ImageAspect.ImageAspectRectangle(LImageRectangle, LClientRect);
			return LImageRectangle.Height < LImage.Height ? LImageRectangle.Height : LImage.Height;
		}

		protected override void PaintCell
			(
			object AValue,
			bool AIsSelected,
			int ARowIndex,
			Graphics AGraphics,
			Rectangle ACellRect,
			Rectangle APaddedRect,
			Rectangle AVerticalMeasuredRect 
			)
		{
			if (AValue is DAEData.Row)
			{
				DAEData.Row LRow = (DAEData.Row)AValue;
				Image LImage = null;
				int LColumnIndex = ColumnIndex(Link);
				if (LRow.HasValue(LColumnIndex))
				{
					Stream LStream = LRow.GetValue(LColumnIndex).OpenStream();
					try
					{
						MemoryStream LCopyStream = new MemoryStream();
						StreamUtility.CopyStream(LStream, LCopyStream);
						LImage = Image.FromStream(LCopyStream);
					}
					finally
					{
						LStream.Close();
					}
				}
				if (LImage != null)
				{
					Rectangle LImageRect = new Rectangle(0 , 0, LImage.Width, LImage.Height);
					LImageRect = ImageAspect.ImageAspectRectangle(LImageRect, APaddedRect);
 
					switch (HorizontalAlignment)
					{
						case WinForms.HorizontalAlignment.Center :
							LImageRect.Offset
								(
								(APaddedRect.Width - LImageRect.Width) / 2,
								(APaddedRect.Height - LImageRect.Height) / 2
								);
							break;
						case WinForms.HorizontalAlignment.Right :
							LImageRect.Offset
								(
								(APaddedRect.Width - LImageRect.Width),
								(APaddedRect.Height - LImageRect.Height)
								);
							break;
					}
				 
					LImageRect.X += APaddedRect.X;
					LImageRect.Y += APaddedRect.Y;
					AGraphics.DrawImage(LImage, LImageRect);
				}
			}

		}
	}

	/// <summary> Converts StringAlignment to HorizontalAlignment and HorizontalAlignment to StringAlignment. </summary>
	public class AlignmentConverter
	{
		public static WinForms.HorizontalAlignment ToHorizontalAlignment(StringAlignment AAlignment)
		{
			switch (AAlignment)
			{
				case StringAlignment.Near : return WinForms.HorizontalAlignment.Left;
				case StringAlignment.Center : return WinForms.HorizontalAlignment.Center;
				case StringAlignment.Far : return WinForms.HorizontalAlignment.Right;
				default : return WinForms.HorizontalAlignment.Left;
			}
		}

		public static StringAlignment ToStringAlignment(WinForms.HorizontalAlignment AAlignment)
		{
			switch (AAlignment)
			{
				case WinForms.HorizontalAlignment.Left : return StringAlignment.Near;
				case WinForms.HorizontalAlignment.Center : return StringAlignment.Center;
				case WinForms.HorizontalAlignment.Right : return StringAlignment.Far;
				default : return StringAlignment.Near;
			}
		}
	}
}