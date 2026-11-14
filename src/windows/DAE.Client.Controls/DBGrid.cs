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
using System.Collections.Generic;

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
		public const int AutoScrollWidth = 30;
		public const byte DefaultHighlightAlpha = 60;	// Default transparency of highlight bar
		public const byte DefaultDragImageAlpha = 60;
		private Size _beforeDragSize = WinForms.SystemInformation.DragSize;

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
			_columns = new GridColumns(this);
			_header = new ArrayList();
			_rows = new GridRows(this);
			_firstColumnIndex = 0;
			_link = new GridDataLink();
			_link.OnActiveChanged += new DAEClient.DataLinkHandler(ActiveChanged);
			_link.OnRowChanged += new DAEClient.DataLinkFieldHandler(RowChanged);
			_link.OnDataChanged += new DAEClient.DataLinkHandler(DataChanged);
			_readOnly = true;
			Size = new Size(185, 89);
			_defaultColumnWidth = 100;
			_cellPadding = DefaultCellPadding;
			_lineWidth = 1;
			_minRowHeight = DefaultRowHeight;
			_noValueBackColor = ControlColor.NoValueBackColor;
			_fixedColor = DBGrid.DefaultBackColor;
			_saveBackColor = BackColor;
			_highlightColor = Color.FromArgb(DefaultHighlightAlpha, SystemColors.Highlight);
			_axisLineColor = Color.Silver;
			_defaultHeader3DStyle = WinForms.Border3DStyle.RaisedInner;
			_defaultHeader3DSide = WinForms.Border3DSide.Top | WinForms.Border3DSide.Left | WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Right;
			_scrollBars = WinForms.ScrollBars.Both;
			_previousGridColumn = null;
			_columnResizing = true;
			_resizingColumn = null;
			_dragScrollTimer = null;
			_dragDropScrollInterval = 400;
			_orderIndicatorColor = SystemColors.ControlDark;
			_clickSelectsRow = ClickSelectsRow.LeftClick | ClickSelectsRow.RightClick;
			_orderChange = OrderChange;
			_saveFont = Font;
			_saveForeColor = ForeColor;
			_extraRowColor = Color.FromArgb(60, SystemColors.ControlDark);
			_hideHighlightBar = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				try
				{
					try
					{
						_link.OnActiveChanged -= new DAEClient.DataLinkHandler(ActiveChanged);
						_link.OnRowChanged -= new DAEClient.DataLinkFieldHandler(RowChanged);
						_link.OnDataChanged -= new DAEClient.DataLinkHandler(DataChanged);
						_link.Dispose();
						_link = null;
					}
					finally
					{
						_columns.Dispose();
						_columns = null;
					}
				}
				finally
				{
					_header = null;
					_rows = null;
				}
			}
			base.Dispose(disposing);
		}

		protected override WinForms.CreateParams CreateParams
		{
			get
			{
				WinForms.CreateParams paramsValue = base.CreateParams;
				switch (_scrollBars)
				{
					case WinForms.ScrollBars.Both :
						paramsValue.Style |= NativeMethods.WS_HSCROLL | NativeMethods.WS_VSCROLL;
						break;
					case WinForms.ScrollBars.Vertical :
						paramsValue.Style |= NativeMethods.WS_VSCROLL;
						break;
					case WinForms.ScrollBars.Horizontal :
						paramsValue.Style |= NativeMethods.WS_HSCROLL;
						break;
				}
				return paramsValue;
			}
		}
		
		/// <summary> Gets the grids internal DataLink to a view. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		private GridDataLink _link;
		protected internal GridDataLink Link
		{
			get { return _link; }
		}

		/// <summary> Gets or sets a value indicating the DataSource the control is linked to. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("DataSource for this control")]
		public DAEClient.DataSource Source
		{
			get { return _link.Source; }
			set { _link.Source = value; }
		}

		private bool _readOnly;
		/// <summary> Gets and sets a value indicating the read-only state of the grid. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		[Category("Data")]
		[DefaultValue(true)]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set
			{
				if (_readOnly != value)
				{
					_readOnly = value;
					UpdateColumns();
					InternalInvalidate(Region, true);
				}
			}
		}

		private GridColumns _columns;
		/// <summary> Collection of GridColumn's. </summary>
		[Category("Columns")]
		[Description("Collection of GridColumn's")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public virtual GridColumns Columns
		{
			get { return _columns; }
		}
	
		/// <summary> Total width and height of non data area includs the header height and one row of minimum height. </summary>
		[Browsable(false)]
		public Size BorderSize
		{
			get { return (this.Size - this.ClientRectangle.Size) + new Size(0, _headerHeight); }
		}

		/// <summary> Size of data area. </summary>
		[Browsable(false)]
		public Size DataSize
		{
			get
			{
				if (_rows.Count > 0)
				{
					Rectangle rect = _rows[_rows.Count - 1].ClientRectangle;
					return new Size(rect.X + rect.Width, rect.Y + rect.Height);
				}
				else
					return Size.Empty;
			}
		}

		private OrderChange _orderChange;
		/// <summary> Determines how the views order changes when clicking on a column. </summary>
		[Category("Order")]
		[DefaultValue(OrderChange.Key)]
		[Description("Method for handling order changes by the DBGrid.")]
		public OrderChange OrderChange
		{
			get { return _orderChange; }
			set
			{
				if (_orderChange != value)
					_orderChange = value;
			}
		}

		private bool ShouldSerializeOrderIndicatorColor() { return _orderIndicatorColor != SystemColors.ControlDark; }
		private Color _orderIndicatorColor;
		/// <summary> Color of order indicators on column headers. </summary>
		[Category("Order")]
		[Description("Color of order indicators on column headers.")]
		public Color OrderIndicatorColor
		{
			get { return _orderIndicatorColor; }
			set
			{
				if (_orderIndicatorColor != value)
				{
					_orderIndicatorColor = value;
					InvalidateHeader();
				}
			}
		}

		private bool ShouldSerializeNoValueBackColor() { return _noValueBackColor != ControlColor.NoValueBackColor; }
		private Color _noValueBackColor;
		/// <summary> Gets or sets the background color of cells that have no value. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\DBGrid.dxd"/>
		[Category("Appearance")]
		[Description("Background color of a cell that has no value")]
		public Color NoValueBackColor
		{
			get { return _noValueBackColor; }
			set 
			{ 
				if (_noValueBackColor != value)
				{
					_noValueBackColor = value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool ShouldSerializeFixedColor() { return _fixedColor != DBGrid.DefaultBackColor; }
		private Color _fixedColor;
		/// <summary> Color of the column headers and other fixed areas. </summary>
		[Category("Appearance")]
		[Description("Color of the column headers and other fixed areas")]
		public Color FixedColor
		{
			get { return _fixedColor; }
			set
			{
				if (_fixedColor != value)
				{
					_fixedColor = value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool _hideHighlightBar;
		/// <summary> Hides the highlight bar. </summary>
		[DefaultValue(false)]
		[Category("Appearance")]
		[Description("Hides the highlight bar.")]
		public bool HideHighlightBar
		{
			get { return _hideHighlightBar; }
			set
			{
				if (_hideHighlightBar != value)
				{
					_hideHighlightBar = value;
					InvalidateHighlightBar();
				}
			}
		}

		private bool ShouldSerializeHighlightColor() { return _highlightColor != Color.FromArgb(DefaultHighlightAlpha, SystemColors.Highlight); }
		private Color _highlightColor;
		/// <summary> Color of the highlight bar. </summary>
		[Category("Appearance")]
		[Description("Color of the highlighted row bar.  Should have alpha contingent")]
		public Color HighlightColor
		{
			get { return _highlightColor; }
			set
			{
				if (_highlightColor != value)
				{
					_highlightColor = (value.A == 255) ? Color.FromArgb(_highlightColor.A, value) : value; 
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool ShouldSerializeAxisLineColor() { return _axisLineColor != Color.Silver; }
		private Color _axisLineColor;
		/// <summary> Color of the axis lines. </summary>
		[Category("Appearance")]
		[Description("Color of the axis lines.")]
		public Color AxisLineColor
		{
			get { return _axisLineColor; }
			set
			{
				if (_axisLineColor != value)
				{
					_axisLineColor = value;
					InternalInvalidate(Region, true);
				}		
			}
		}

		private Font _saveFont;
		protected override void OnFontChanged(EventArgs args)
		{
			base.OnFontChanged(args);
			foreach (GridColumn column in _columns)
				if (column.Font == _saveFont)
					column.Font = Font;
			_saveFont = Font;
		}

		private Color _saveForeColor;
		protected override void OnForeColorChanged(EventArgs args)
		{
			base.OnForeColorChanged(args);
			foreach (GridColumn column in _columns)
				if (column.ForeColor == _saveForeColor)
					column.ForeColor = ForeColor;
			_saveForeColor = ForeColor;
		}

		private Color _saveBackColor;
		protected override void OnBackColorChanged(EventArgs args)
		{
			base.OnBackColorChanged(args);
			NoValueBackColor = Color.FromArgb(BackColor.A, NoValueBackColor);
			foreach(GridColumn column in _columns)
				if (column.BackColor == _saveBackColor)
					column.BackColor = BackColor;
			_saveBackColor = BackColor;
		}

		private GridLines _gridLines;
		/// <summary> Axis lines to paint. </summary>
		[Category("Appearance")]
		[DefaultValue(GridLines.Both)]
		[Description("Axis lines to paint.")]
		public GridLines GridLines
		{
			get { return _gridLines; }
			set
			{
				if (_gridLines != value)
				{
					_gridLines = value;
					InternalInvalidate(Region, true);
				}	
			}
		}

		private bool _columnResizing;
		/// <summary> Enables or disables column resizing. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Disable column resizing.")]
		public bool ColumnResizing
		{
			get { return _columnResizing; }
			set
			{
				if (_columnResizing != value)
					_columnResizing = value;
			}
		}

		private int _dragDropScrollInterval;
		/// <summary> The ampount of time in miliseconds between scrolls during drag and drop. </summary>
		[DefaultValue(400)]
		[Category("Behavior")]
		[Description("The time in miliseconds between scrolls during drag and drop.")]
		public int DragDropScrollInterval
		{
			get { return _dragDropScrollInterval; }
			set
			{
				if (value < 1)
					throw new ControlsException(ControlsException.Codes.InvalidInterval);
				if (_dragDropScrollInterval != value)
				{
					_dragDropScrollInterval = value;
					if ((_dragScrollTimer != null) && (_dragScrollTimer.Interval != value))
						_dragScrollTimer.Interval = value;
				}
			}
		}

		private WinForms.Border3DStyle _defaultHeader3DStyle;
		/// <summary> The default 3D border style of a columns header. </summary>
		[Category("Appearance")]
		[DefaultValue(WinForms.Border3DStyle.RaisedInner)]
		[Description("Default Border3DStyle for column header.")]
		public WinForms.Border3DStyle DefaultHeader3DStyle
		{
			get { return _defaultHeader3DStyle; }
			set
			{
				if (_defaultHeader3DStyle != value)
				{
					BeginUpdate();
					try
					{
						WinForms.Border3DStyle previousHeader3DStyle = _defaultHeader3DStyle;
						_defaultHeader3DStyle = value;
						foreach (GridColumn column in Columns)
							if (column.Header3DStyle == previousHeader3DStyle)
								column.Header3DStyle = value;
					}
					finally
					{
						EndUpdate();
					}
				}
			}
		}

		private WinForms.Border3DSide _defaultHeader3DSide;
		/// <summary> The default sides of the header to paint the border. </summary>
		[Category("Appearance")]
		[DefaultValue(WinForms.Border3DSide.Top | WinForms.Border3DSide.Left | WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Right)]
		[Description("Default Border3DSide for column header.")]
		public WinForms.Border3DSide DefaultHeader3DSide
		{
			get { return _defaultHeader3DSide; }
			set
			{
				if (_defaultHeader3DSide != value)
				{
					BeginUpdate();
					try
					{
						WinForms.Border3DSide previousHeader3DSide = _defaultHeader3DSide;
						_defaultHeader3DSide = value;
						foreach (GridColumn column in Columns)
							if (column.Header3DSide == previousHeader3DSide)
								column.Header3DSide = value;
					}
					finally
					{
						EndUpdate();
					}
				}
			}
		}

		private int _lineWidth;
		/// <summary> Line width of the axis in pixels. </summary>
		[DefaultValue(1)]
		[Category("Appearance")]
		[Description("Grid lines width")]
		public int LineWidth
		{
			get { return _lineWidth; }
			set 
			{
				if (_lineWidth != value)
				{
					_lineWidth = value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private int _defaultColumnWidth;
		/// <summary> Default width for columns (in characters). </summary>
		[DefaultValue(100)]
		[Category("Appearance")]
		[Description("Default width for columns (in characters).")]
		public int DefaultColumnWidth
		{
			get { return _defaultColumnWidth; }
			set
			{
				if (_defaultColumnWidth != value)
				{
					int previousDefaultWidth = _defaultColumnWidth;
					if (value >= 0)
						_defaultColumnWidth = value;
					else
						_defaultColumnWidth = 0;
					foreach (GridColumn column in Columns)
						if (column.Width == previousDefaultWidth)
							column.Width = _defaultColumnWidth;
				}
			}
		}

		private ClickSelectsRow _clickSelectsRow;
		/// <summary> Method of selecting a row with the mouse. </summary>
		[Category("Behavior")]
		[DefaultValue(ClickSelectsRow.LeftClick | ClickSelectsRow.RightClick)]
		[Description("Allow right click to select a row.")]
		public ClickSelectsRow ClickSelectsRow
		{
			get { return _clickSelectsRow; }
			set { _clickSelectsRow = value; }
		}

		private bool ShouldSerializeHeaderHeight() { return _headerHeight != DefaultRowHeight; }
		private int _headerHeight = DefaultRowHeight;
		/// <summary> Pixel height of the header row. </summary>
		[Category("Appearance")]
		[Description("Pixel height of the header row.")]
		public int HeaderHeight
		{
			get { return _headerHeight; }
			set
			{
				if (_headerHeight != value)
				{
					_headerHeight = value;
					UpdateBufferCount();
					InternalInvalidate(Region, true);
				}	
			}
		}

		public static int DefaultRowHeight { get { return 19; } }
		private bool ShouldSerializeMinRowHeight() { return _minRowHeight != DefaultRowHeight; }
		private int _minRowHeight;
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
			get { return _minRowHeight; }
			set
			{
				if (_minRowHeight != value)
				{
					if (value <= 0)
						throw new ControlsException(ControlsException.Codes.ZeroMinRowHeight);
					if ((_maxRowHeight >= 0) && (_maxRowHeight > -1) && (value > _maxRowHeight))
						throw new ControlsException(ControlsException.Codes.InvalidMinRowHeight);
					_minRowHeight = value;
					UpdateBufferCount();
					InternalInvalidate(Region, true);
				}		
			}
		}

		private int _maxRowHeight = -1;
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
			get { return _maxRowHeight; }
			set
			{
				if (_maxRowHeight != value)
				{
					if ((value >= 0) && (value < _minRowHeight))
						throw new ControlsException(ControlsException.Codes.InvalidMaxRowHeight);
					_maxRowHeight = value;
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
				if (_maxRowHeight < 0)
					return DataRectangle.Height < _minRowHeight ? _minRowHeight : DataRectangle.Height;
				return _maxRowHeight > DataRectangle.Height ? DataRectangle.Height : _maxRowHeight;
			}
			return _minRowHeight;
		}

		private int _firstColumnIndex;
		/// <summary> Index of the first visible column in the columns collection. </summary>
		protected internal int FirstColumnIndex { get { return _firstColumnIndex; } }

		/// <summary> Sets the FirstColumnIndex property. </summary>
		/// <param name="index"></param>
		protected virtual void SetFirstColumnIndex(int index)
		{
			int newIndex = index;
			if (newIndex >= _columns.Count)
				newIndex = _columns.Count - 1;
			if (newIndex < 0)
				newIndex = 0;
			if (_firstColumnIndex != newIndex)
				_firstColumnIndex = newIndex;
		}

		private WinForms.ScrollBars _scrollBars;
		/// <summary> Scroll bars for this control. </summary>
		[Category("Appearance")]
		[DefaultValue(WinForms.ScrollBars.Both)]
		[Description("Scroll bars for this control.")]
		public WinForms.ScrollBars ScrollBars
		{
			get { return _scrollBars; }
			set
			{
				if (_scrollBars != value)
				{
					_scrollBars = value;
					if (IsHandleCreated)
						RecreateHandle();
				}
			}
		}

		private WinForms.ContextMenu _headerContextMenu;
		/// <summary> Context menu of the header. </summary>
		[DefaultValue(null)]
		[Category("Behavior")]
		[Description("Context menu of the header.")]
		public WinForms.ContextMenu HeaderContextMenu
		{
			get { return _headerContextMenu; }
			set { _headerContextMenu = value; }
		}

		/// <summary> Calculates the length of the entire header in pixels. </summary>
		/// <returns> The length of the entire header in pixels. </returns>
		protected int HeaderPixelWidth()
		{
			if (_header != null && _header.Count > 0)
			{
				HeaderColumn lastColumn = (HeaderColumn)_header[_header.Count - 1];
				return lastColumn.Offset + lastColumn.Width;
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
			NativeMethods.SCROLLINFO scrollInfo;
			if (IsHandleCreated && ((_scrollBars == WinForms.ScrollBars.Vertical) || (_scrollBars == WinForms.ScrollBars.Both)))
			{
				scrollInfo = new NativeMethods.SCROLLINFO();
				scrollInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(scrollInfo);
				scrollInfo.fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE | NativeMethods.SIF_POS | NativeMethods.SIF_TRACKPOS;
				scrollInfo.nMin = 0;
				scrollInfo.nMax = 2;
				scrollInfo.nPage = 1;
				if (_link.Active && _link.DataSet.IsLastRow && !_link.DataSet.IsFirstRow)
					scrollInfo.nPos = 2;
				else if (_link.Active && !_link.DataSet.IsLastRow && !_link.DataSet.IsFirstRow)
					scrollInfo.nPos = 1;
				else
					scrollInfo.nPos = 0;
				UnsafeNativeMethods.SetScrollInfo(Handle, NativeMethods.SB_VERT, scrollInfo, true);
			}
		}

		private void UpdateHScrollInfo()
		{
			NativeMethods.SCROLLINFO scrollInfo;
			if (IsHandleCreated && ((_scrollBars == WinForms.ScrollBars.Horizontal) || (_scrollBars == WinForms.ScrollBars.Both)))
			{
				scrollInfo = new NativeMethods.SCROLLINFO();
				scrollInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(scrollInfo);
				scrollInfo.fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE | NativeMethods.SIF_POS | NativeMethods.SIF_TRACKPOS;
				scrollInfo.nMin = 0;
				scrollInfo.nMax = _columns.MaxScrollIndex(ClientRectangle.Width, (_cellPadding.Width << 1));
				scrollInfo.nPos = _firstColumnIndex;
				scrollInfo.nPage = 1;
				UnsafeNativeMethods.SetScrollInfo(Handle, NativeMethods.SB_HORZ, scrollInfo, true);
			}
		}

		/// <summary> Invokes the Update method for each column in the Grid </summary>
		protected void UpdateColumns()
		{
			_columns.UpdateColumns(_columns.ShowDefaultColumns);
			UpdateRows();
		}
		
		/// <summary> Occures when a column is changed. </summary>
		[Category("Column")]
		public event ColumnEventHandler OnColumnChanged;

		/// <summary> Raises the OnColumnChanged event. </summary>
		/// <param name="column"> The GridColumn that changed. </param>
		protected virtual void ColumnChanged(GridColumn column)
		{
			if (OnColumnChanged != null)
				OnColumnChanged(this, column, EventArgs.Empty);
		}

		internal void InternalColumnChanged(GridColumn column)
		{
			if (!_columns.UpdatingColumns && !Disposing && !IsDisposed)
			{
				_columns.InternalColumnChanged(column);
				if (column is DataColumn)
					((DataColumn)column).InternalCheckColumnExists(_link);
				UpdateHeader();
				UpdateBufferCount();
				UpdateScrollBars();
				InternalInvalidate(Region, true);
			}
			ColumnChanged(column);
		}

		/// <summary> Occures on non-scrolling navigation or refresh of the view. </summary>
		/// <param name="link"> The DataLink between this control and the view. </param>
		/// <param name="AView"> The DataView this control is linked to. </param>
		protected void DataChanged(DAEClient.DataLink link, DAEClient.DataSet dataSet)
		{
			if (link.Active && !((dataSet.State == DataSetState.Insert) || (dataSet.State == DataSetState.Edit)))
			{
				UpdateBufferCount();
				UpdateScrollBars();
				InternalInvalidate(Region, true);
			}
		}

		/// <summary> Occures whenever the active state of the view changes. </summary>
		/// <param name="link"> The DataLink between this control and the view. </param>
		/// <param name="AView"> The DataView this control is linked to. </param>
		protected void ActiveChanged(DAEClient.DataLink link, DAEClient.DataSet dataSet)
		{
			InternalUpdateGrid(true, true);
		}

		/// <summary> Refresh all aspects of the grid including the header, scrollbars, columns and buffers.  </summary>
		/// <param name="updateFirstColumnIndex"> When true updates the FirstColumnIndex. </param>
		/// <param name="invalidateRegion"> When true invalidates the entire client region. </param>
		protected internal void InternalUpdateGrid(bool updateFirstColumnIndex, bool invalidateRegion)
		{
			if (!Disposing && !IsDisposed && !_columns.UpdatingColumns)
			{
				UpdateColumns();
				if (updateFirstColumnIndex && _link.Active)
					SetFirstColumnIndex(_firstColumnIndex);
				UpdateHeader();
				UpdateBufferCount();
				UpdateScrollBars();
				if (invalidateRegion)
					InternalInvalidate(Region, true);
			}
		}

		protected void RowChanged(DAEClient.DataLink link, DAEClient.DataSet dataSet, DAEClient.DataField field)
		{
			if (link.Active && (_rows.Count > 0))
			{
				Rectangle saveActiveClientRectangle = _rows[_link.ActiveOffset].ClientRectangle;
				UpdateBufferCount();
				InternalInvalidate(Rectangle.Union(_rows[_link.ActiveOffset].ClientRectangle, saveActiveClientRectangle), true);
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
				Rectangle result = ClientRectangle;
				result.Y += _headerHeight;
				result.Height -= _headerHeight;
				return result;
			}
		}

		/// <summary> Occures whenever the rows client area changes. </summary>
		protected virtual void RowChanged(object value, Rectangle rowRectangle, int rowIndex)
		{
			foreach (GridColumn column in _columns)
				column.RowChanged(value, rowRectangle, rowIndex);
		}

		/// <summary> Called whenever the rows collection has changed. </summary>
		protected virtual void RowsChanged()
		{
			foreach (GridColumn column in _columns)
				column.RowsChanged();
		}

		private void InternalRowsChanged()
		{
			GridRow row;
			for (int i = 0; i < _rows.Count; i++)
			{
				row = _rows[i];
				RowChanged(row.Row, row.ClientRectangle, i);
			}
			RowsChanged();
		}
		
		private GridRow _saveLastRow = null;

		protected virtual void BeforeRowsChange() {}

		/// <summary> Updates rows and the link's buffer count. </summary>
		private void UpdateRows()
		{
			_saveLastRow = _rows.Count > 0 ? new GridRow(_rows[_rows.Count - 1].Row, _rows[_rows.Count - 1].ClientRectangle) : null;
			BeforeRowsChange();
			_rows.Clear();

			if (_link.Active && (_link.LastOffset >= 0))
			{
				int width = HeaderPixelWidth();
				int height;
				int totalHeight = _headerHeight;
				int maxHeight = ClientRectangle.Height;
				using (Graphics graphics = CreateGraphics())
				{
					int lastOffset = _link.LastOffset;
					DAEData.IRow row;
					
					for (int i = _link.ActiveOffset; i >= 0; i--)
					{
						row = _link.Buffer(i);
						height = MeasureRowHeight(row, graphics);
						if ((height + totalHeight) <= maxHeight)
						{
							_rows.Insert(0, new GridRow(row, new Rectangle(Point.Empty, new Size(width, height))));
							totalHeight += height;
						}
						else
						{
							_link.BufferCount = _rows.Count;
							lastOffset = _link.LastOffset;
							break;
						}
					}

					int j = _link.ActiveOffset;
					while (totalHeight <= maxHeight)
					{
						//Add rows to the bottom
						++j;
						if (j <= lastOffset)
						{
							row = _link.Buffer(j);
							height = MeasureRowHeight(row, graphics);
							if ((totalHeight + height) <= maxHeight)
							{
								_rows.Add(new GridRow(row, new Rectangle(Point.Empty, new Size(width, height))));
								totalHeight += height;
							}
							else
								break;
						}
						else
						{
							int saveLastOffset = _link.LastOffset;
							int saveActiveOffset = _link.ActiveOffset;
							_link.BufferCount++;
							lastOffset = _link.LastOffset;
							if (saveLastOffset < lastOffset)
							{
								if (saveActiveOffset == _link.ActiveOffset)
								{
									row = _link.Buffer(lastOffset);
									height = MeasureRowHeight(row, graphics);
									if ((height + totalHeight) < maxHeight)
									{
										_rows.Add(new GridRow(row, new Rectangle(Point.Empty, new Size(width, height))));
										totalHeight += height;
									}
									else
										break; //Over the bottom edge.
								}
								else
								{
									row = _link.Buffer(0);
									height = MeasureRowHeight(row, graphics);
									if ((height + totalHeight) < maxHeight)
									{
										_rows.Insert(0, new GridRow(row, new Rectangle(Point.Empty, new Size(width, height))));
										totalHeight += height;
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
				int top = _headerHeight;
				Rectangle newRect;
				foreach (GridRow gridRow in _rows)
				{
					newRect = gridRow.ClientRectangle;
					newRect.Y = top;
					top += newRect.Height;
					gridRow.ClientRectangle = newRect;
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
		private bool ShouldSerializeCellPadding() { return !_cellPadding.Equals(DefaultCellPadding); }
		private Size _cellPadding;
		/// <summary> Amount to pad a cells. </summary>
		[Category("Appearance")]
		[Description("Amount to pad cells.")]
		public Size CellPadding
		{
			get { return _cellPadding; }
			set
			{
				_cellPadding = value;
				if (!Disposing && !IsDisposed)
				{
					UpdateHeader();
					UpdateBufferCount();
					UpdateScrollBars();
					InternalInvalidate(Region, true);
				}
			}
		}

		private bool ShouldSerializeExtraRowColor() { return _extraRowColor != Color.FromArgb(60, SystemColors.ControlDark); }
		private Color _extraRowColor;
		[Category("Appearance")]
		[Description("Color for extra row indicator.")]
		public Color ExtraRowColor
		{
			get { return _extraRowColor; }
			set
			{
				if (_extraRowColor != value)
				{
					_extraRowColor = (value.A == 255) ? Color.FromArgb(_extraRowColor.A, value) : value;
					InternalInvalidate(Region, true);
				}
			}
		}

		private GridRows _rows;
		internal GridRows Rows { get { return _rows; } }
		
		private ArrayList _header;
		protected internal ArrayList Header { get { return _header; } }

		protected void UpdateHeader()
		{
			GridColumn column;
			int offsetX = 0;
			if (_resizing && (_columns.VisibleColumns.Count > 0) && (((GridColumn)_columns.VisibleColumns[_columns.VisibleColumns.Count - 1]).IsLastHeaderColumn) && (ClientRectangle.Width >= HeaderPixelWidth()))
				SetFirstColumnIndex(_columns.MaxScrollIndex(ClientRectangle.Width, _cellPadding.Width << 1));
			int columnIndex = _firstColumnIndex;
			Header.Clear();
			while ((offsetX < ClientSize.Width) && (columnIndex < _columns.VisibleColumns.Count))
			{
				column = (GridColumn)_columns.VisibleColumns[columnIndex];
				Header.Add(new HeaderColumn(column, offsetX, column.Width + (_cellPadding.Width << 1)));
				offsetX += column.Width + (_cellPadding.Width << 1);
				columnIndex++;
			}
		}

		[Category("Paint")]
		public event PaintCellEventHandler PaintHeaderCell;
		[Category("Paint")]
		public event PaintCellEventHandler PaintDataCell;

		private HeaderColumn _headerColumnToPaint;
	
		protected virtual void OnPaintHeaderCell(Graphics graphics, GridColumn column, Rectangle cellRect, StringFormat format)
		{
			_headerColumnToPaint.Paint(graphics, cellRect, format, _fixedColor);
			if (PaintHeaderCell != null)
				PaintHeaderCell(this, column, cellRect, graphics, -1);
		}

		protected virtual void OnPaintDataCell
			(
			Graphics graphics,
			GridColumn column,
			Rectangle cellRect,
			DAEData.IRow row, 
			bool isSelected,
			Pen linePen,
			int rowIndex
			)
		{
			column.InternalPaintCell
				(
				graphics,
				cellRect,
				row,
				isSelected,
				linePen,
				rowIndex
				);
			if (PaintDataCell != null)
				PaintDataCell(this, column, cellRect, graphics, rowIndex); 
		}

		/// <summary> Override this method to custom paint the highlighted area. </summary>
		/// <param name="graphics">The Graphics instance to paint with.</param>
		/// <param name="highlightRect">The highlight area.</param>
		protected virtual void PaintHighlightBar(Graphics graphics, Rectangle highlightRect)
		{
			using (SolidBrush highlightBrush = new SolidBrush(HighlightColor))
			{
				using (Pen highlightPen = new Pen(SystemColors.Highlight))
				{
					graphics.FillRectangle(highlightBrush, highlightRect);
					if (Focused)
						graphics.DrawRectangle(highlightPen, highlightRect);
				}
			}
		}

		private int GetMinRowHeight()
		{
			int minHeight = -1;
			foreach (GridColumn column in _columns)
			{
				if (column.MinRowHeight > -1)
					if ((minHeight < 0) || (column.MinRowHeight > minHeight))
						minHeight = column.MinRowHeight;
			}
			return minHeight;
		}

		private int GetMaxRowHeight()
		{
			int maxHeight = -1;
			foreach (GridColumn column in _columns)
			{
				if (column.MaxRowHeight > -1)
					if ((maxHeight < 0) || (column.MaxRowHeight < maxHeight))
						maxHeight = column.MaxRowHeight;
			}
			return maxHeight;
		}

		private int GetMaxNaturalHeight(DAEData.IRow row, Graphics graphics)
		{
			int naturalRowHeight;
			int maxRowHeight = _minRowHeight;
			foreach (GridColumn column in _columns)
			{
				if (column is DataColumn)
				{
					int columnIndex = ((DataColumn)column).ColumnIndex(_link);

					naturalRowHeight = 
						column.NaturalHeight
						(
							columnIndex >= 0 && row.HasValue(columnIndex) ? row[columnIndex] : null,
							graphics
						);
				}
				else
					naturalRowHeight = column.NaturalHeight(null, graphics);
				if (naturalRowHeight > maxRowHeight)
					maxRowHeight = naturalRowHeight;
			}
			return maxRowHeight;
		}

		/// <summary> Measures the height of a row in pixels. </summary>
		/// <param name="row"></param>
		/// <returns> The height of a row in pixels. </returns>
		protected int MeasureRowHeight(DAEData.IRow row, Graphics graphics)
		{
			int minRowHeight = GetMinRowHeight();
			int maxRowHeight = GetMaxRowHeight();
			int gridMaxRowHeight = CalcMaxRowHeight();
			if ((maxRowHeight >= 0) && (minRowHeight >= 0) && (maxRowHeight == minRowHeight))
				return minRowHeight > gridMaxRowHeight ? gridMaxRowHeight : minRowHeight;
			if (row == null)
				return _minRowHeight;
			int naturalHeight = GetMaxNaturalHeight(row, graphics);
			if ((maxRowHeight < 0) && (minRowHeight < 0))
				return naturalHeight > gridMaxRowHeight ? gridMaxRowHeight : naturalHeight;
			if ((minRowHeight > -1) && naturalHeight < minRowHeight)
				return minRowHeight > gridMaxRowHeight ? gridMaxRowHeight : minRowHeight;
			if ((maxRowHeight > -1) && (naturalHeight > maxRowHeight))
				return maxRowHeight > gridMaxRowHeight ? gridMaxRowHeight : maxRowHeight;
			return naturalHeight > gridMaxRowHeight ? gridMaxRowHeight : naturalHeight;
		}

		private Rectangle _priorHighlightRect = Rectangle.Empty;
		public void DrawCells(Graphics graphics, Rectangle rect, bool paintHighlightBar)
		{
			using (StringFormat format = new StringFormat(StringFormatFlags.NoWrap))
			{
				using (Pen linePen = new Pen(_axisLineColor, _lineWidth))
				{
					using (SolidBrush textBrush = new SolidBrush(ForeColor))
					{
						using (SolidBrush backBrush = new SolidBrush(BackColor))
						{
							Rectangle cellRect = Rectangle.Empty;
							// Draw header cells...
							if (rect.IntersectsWith(new Rectangle(0, 0, HeaderPixelWidth(), _headerHeight)))
								foreach (HeaderColumn header in _header)
								{
									cellRect = new Rectangle(header.Offset, 0, header.Width, _headerHeight);
									format.Alignment = AlignmentConverter.ToStringAlignment(header.Column.HeaderTextAlignment);
									_headerColumnToPaint = header;
									if (rect.IntersectsWith(cellRect) && (header.Column.Grid != null))
										OnPaintHeaderCell(graphics, header.Column, cellRect, format);
								}

							// Draw data cells...
							Rectangle highlightRect = Rectangle.Empty;
							int rowIndex = 0;
							foreach (GridRow gridRow in _rows)
							{
								cellRect = gridRow.ClientRectangle;
								if (rowIndex == _link.ActiveOffset)
								{
									highlightRect = gridRow.ClientRectangle;
									highlightRect.Y -= 1;
									highlightRect.Width -= 1;
								}
								if (rect.IntersectsWith(gridRow.ClientRectangle))
								{
									DAEData.IRow row = gridRow.Row;
									foreach (HeaderColumn headerColumn in _header)
									{
										cellRect.X = headerColumn.Offset;
										cellRect.Width = headerColumn.Width;
										if (rect.IntersectsWith(cellRect) && (headerColumn.Column.Grid != null))
											OnPaintDataCell
												(
												graphics,
												headerColumn.Column,
												cellRect,
												row,
												rowIndex == _link.ActiveOffset,
												linePen,
												rowIndex
												);	

									}
								}
								++rowIndex;
							}

							//Draw highlight for extra row.
							if (_link.LastOffset >= _rows.Count)
							{
								if (_rows.Count == 0)
									cellRect = DataRectangle;
								else
								{
									Rectangle lastRowRect = _rows[_rows.Count - 1].ClientRectangle;
									cellRect = new Rectangle(lastRowRect.X, lastRowRect.Bottom, lastRowRect.Width, this.MeasureRowHeight(_link.Buffer(_link.LastOffset), graphics));
								}
								if (rect.IntersectsWith(cellRect))
									using (SolidBrush extraRowBrush = new SolidBrush(_extraRowColor))
										graphics.FillRectangle(extraRowBrush, cellRect);
							}

							// Draw highlight bar...
							if (paintHighlightBar && rect.IntersectsWith(highlightRect))
							{
								_priorHighlightRect = highlightRect;
								PaintHighlightBar(graphics, highlightRect);
							}
								
							// Fill remainder of client area...
							using (Region region = new Region(new Rectangle(Point.Empty, new Size(cellRect.X + cellRect.Width, cellRect.Y + cellRect.Height))))
							{
								region.Complement(ClientRectangle);
								backBrush.Color = BackColor;
								region.Intersect(rect);
								graphics.FillRegion(backBrush, region);
							}
						}
					}
				}
			}
		}

		protected virtual void DrawDragImage(WinForms.PaintEventArgs args, ColumnDragObject dragObject)
		{
			if (args.ClipRectangle.IntersectsWith(dragObject.HighlightRect))
				using (SolidBrush betweenColumnBrush = new SolidBrush(SystemColors.Highlight))
					args.Graphics.FillRectangle(betweenColumnBrush, dragObject.HighlightRect);
			if (
				(dragObject.DragImage != null) &&
				(args.ClipRectangle.IntersectsWith(new Rectangle(dragObject.ImageLocation, dragObject.DragImage.Size)))
			   )
				args.Graphics.DrawImage(dragObject.DragImage, dragObject.ImageLocation);
		}

		protected override void OnPaint(WinForms.PaintEventArgs args)
		{
			// Before paint
			foreach (HeaderColumn column in _header)
				column.Column.BeforePaint(this, args);

			if (!IsDisposed && _link.Active && !args.ClipRectangle.IsEmpty)
			{
				DrawCells(args.Graphics, args.ClipRectangle, !_hideHighlightBar);
				if ((_dragObject != null) && (_dragObject.DragImage != null))
					DrawDragImage(args, _dragObject);
			}
			else
				base.OnPaint(args);

			// After paint
			foreach (HeaderColumn column in _header)
				column.Column.AfterPaint(this, args);
		}

		protected internal void InternalInvalidate(Rectangle rect, bool doubleBuffered)
		{
			SetStyle(WinForms.ControlStyles.DoubleBuffer, doubleBuffered);
			if (!rect.IsEmpty)
				Invalidate(rect);
		}

		protected void InternalInvalidate(Region region, bool doubleBuffered)
		{
			SetStyle(WinForms.ControlStyles.DoubleBuffer, doubleBuffered);
			Invalidate(region);
		}

		protected void InvalidateHighlightBar()
		{
			if (!Disposing && (_rows.Count > _link.ActiveOffset))
				InternalInvalidate(Rectangle.Inflate(_rows[_link.ActiveOffset].ClientRectangle, 1, 1), false);
		}

		protected internal void InvalidateHeader()
		{
			InternalInvalidate(new Rectangle(0,0,ClientRectangle.Width,_headerHeight), false);
		}

		private int ScrollWindowEx(int Adx, int Ady, Rectangle scrollRect, Rectangle clipRect)
		{
			NativeMethods.RECT scroll = NativeMethods.RECT.FromRectangle(scrollRect);
			NativeMethods.RECT clip = NativeMethods.RECT.FromRectangle(clipRect);
			return UnsafeNativeMethods.ScrollWindowEx(Handle, Adx, Ady, ref scroll, ref clip, IntPtr.Zero, IntPtr.Zero, NativeMethods.SW_INVALIDATE);
		}

		private void IvalidateResized(int priorActiveOffset, int priorLastOffset)
		{
			if (_link.Active && (_header.Count > 0))
			{
				int headerWidth = HeaderPixelWidth();
				int bottom = DataSize.Height;
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

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			if ((_link != null) && _link.Active)
				InvalidateHighlightBar();
		}

		protected override void OnLostFocus(EventArgs args)
		{
			base.OnLostFocus(args);
			if ((_link != null) && _link.Active)
				InvalidateHighlightBar();
		}

		private bool _resizing;
		/// <summary> True when the grid is being resized. </summary>
		[Browsable(false)]
		public bool Resizing { get { return _resizing; } }

		private Rectangle _oldClientRect = Rectangle.Empty;
		protected override void OnResize(EventArgs eventArgs)
		{
			_resizing = true;
			try
			{
				base.OnResize(eventArgs);
				if (IsHandleCreated)
				{
					int saveActiveOffset = _link.Active ? _link.ActiveOffset : 0;
					int saveLastOffset = _link.Active ? _link.LastOffset : 0;
					UpdateHeader();
					UpdateBufferCount();
					UpdateScrollBars();
					Invalidate();
//					IvalidateResized(LSaveActiveOffset, LSaveLastOffset);
				}
			}
			finally
			{
				_resizing = false;
				_oldClientRect = ClientRectangle;
			}
		}

		[Category("Scroll")]
		public event HorizontalScrollHandler BeforeHorizontalScroll;
		[Category("Scroll")]
		public event HorizontalScrollHandler AfterHorizontalScroll;

		protected virtual void DoBeforeHorizontalScroll(int offset)
		{
			if ((offset != 0) && (BeforeHorizontalScroll != null))
				BeforeHorizontalScroll(this, _firstColumnIndex, offset, EventArgs.Empty);
		}

		protected virtual void DoAfterHorizontalScroll(int scrolledBy)
		{
			if (AfterHorizontalScroll != null)
				AfterHorizontalScroll(this, _firstColumnIndex, scrolledBy, EventArgs.Empty);
		}

		private void CheckActive()
		{
			if (!_link.Active)
				throw new ControlsException(ControlsException.Codes.ViewNotActive);
		}
		
		protected internal int InternalScrollBy(int value)
		{
			int scrolledBy = 0;
			if (value < 0)
				scrolledBy = (_firstColumnIndex + value) >= 0 ? value : _firstColumnIndex * -1;
			else
			{
				int maxIndex = _columns.MaxScrollIndex(ClientRectangle.Width, _cellPadding.Width << 1);
				scrolledBy = (_firstColumnIndex + value) <= maxIndex ? value : maxIndex - _firstColumnIndex;
			}
			DoBeforeHorizontalScroll(scrolledBy);
			SetFirstColumnIndex(_firstColumnIndex + scrolledBy);
			if (scrolledBy != 0)
			{
				UpdateHeader();
				UpdateRows();
				UpdateHScrollInfo();
				InternalInvalidate(Region, true);
				DoAfterHorizontalScroll(scrolledBy);
			}
			return scrolledBy;
		}

		/// <summary> Scrolls columns right or left by column count. </summary>
		/// <param name="value"> The number of columns to scroll horizontally from the current position </param>
		/// <returns> Number of columns scrolled. </returns>
		public int ScrollBy(int value)
		{
			CheckActive();
			return InternalScrollBy(value);
		}

		/// <summary> Scrolls to the last column in the grid. </summary>
		/// <returns> The number of columns scrolled. </returns>
		protected internal int InternalLastColumn()
		{
			return InternalScrollBy(_columns.VisibleColumns.Count - _firstColumnIndex);
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
			return InternalScrollBy(_firstColumnIndex * -1);
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
			if (_header.Count > 1)
				return InternalScrollBy((_header.Count - 1) * -1);
			else
				return InternalScrollBy(_header.Count * -1);
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
			if (_header.Count > 1)
				return InternalScrollBy(_header.Count - 1);
			else
				return InternalScrollBy(_header.Count);
		}

		/// <summary> Pages right by columns. </summary>
		/// <returns> The number of colums scrolled. </returns>
		public int PageRight()
		{
			CheckActive();
			return InternalPageRight();
		}

		protected virtual void WMHScroll(ref WinForms.Message message)
		{
			if (_link.Active)
			{
				int pos = NativeMethods.Utilities.HiWord(message.WParam);
				int command = NativeMethods.Utilities.LowWord(message.WParam);
				switch (command)
				{
					case NativeMethods.SB_LINELEFT : InternalScrollLeft();
						break;
					case NativeMethods.SB_LINERIGHT : InternalScrollRight();
						break;
					case NativeMethods.SB_LEFT : InternalScrollBy(_firstColumnIndex * -1);
						break;
					case NativeMethods.SB_RIGHT : InternalScrollBy(_columns.MaxScrollIndex(ClientRectangle.Width, _cellPadding.Width << 1));
						break;
					case NativeMethods.SB_PAGELEFT : InternalPageLeft();
						break;
					case NativeMethods.SB_PAGERIGHT : InternalPageRight();
						break;
					case NativeMethods.SB_THUMBPOSITION:
					case NativeMethods.SB_THUMBTRACK : InternalScrollBy(pos - _firstColumnIndex);
						break;
					default:
						break;
				}
			}
		}

		protected internal void InternalPageUp()
		{
			_link.DataSet.MoveBy(-_rows.Count);
		}
		
		/// <summary> Attempts to navigates the view's cursor backward the number of rows visible in grid. </summary>
		public void PageUp()
		{
			CheckActive();
			InternalPageUp();
		}

		protected internal void InternalPageDown()
		{
			_link.DataSet.MoveBy(_rows.Count);
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
			_link.DataSet.MoveBy(-1);
		}

		/// <summary> Attempts to navigates the view's cursor one row past current row. </summary>
		protected void Next()
		{
			_link.DataSet.MoveBy(1);
		}

		/// <summary> Navigates the view's cursor to the first row. </summary>
		protected void First()
		{
			_link.DataSet.First();
		}

		/// <summary> Navigates the view's cursor to the last row. </summary>
		protected void Last()
		{
			_link.DataSet.Last();
		}

		protected internal void InternalFirstVisibleRow()
		{
			_link.DataSet.MoveBy(-_link.ActiveOffset);
		}

		/// <summary> Navigates the view's cursor to the first visible row of the grid. </summary>
		public void Home()
		{
			CheckActive();
			InternalFirstVisibleRow();
		}

		protected internal void InternalLastVisibleRow()
		{
			if (_rows.Count > 0)
				_link.DataSet.MoveBy((_rows.Count - 1) - _link.ActiveOffset);
		}

		/// <summary> Navigates the view's cursor to the last visible row of the grid. </summary>
		public void End()
		{
			CheckActive();
			InternalLastVisibleRow();
		}

		protected virtual void WMVScroll(ref WinForms.Message message)
		{
			if (_link.Active)
			{
				int pos = NativeMethods.Utilities.HiWord(message.WParam); //High order word is position.
				int command = NativeMethods.Utilities.LowWord(message.WParam); //Low order word is command
				switch (command)
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
						if (pos == 0)
							First();
						else if (pos == 2)
							Last();
						break;
					default:
						break;
				}
			}
		}

		private GridColumn _previousGridColumn;
		private WinForms.Cursor _previousCursor;
		private HeaderColumn _resizingColumn;

		private void UpdateCursor()
		{
			if (_columnResizing && (Cursor != _previousCursor) && (_resizingColumn == null))
				ResetCursor();
		}

		private HeaderColumn GetResizeableColumn(int xPos, int yPos)
		{
			if (!_columnResizing)
				return null;
			HeaderColumn headerColumn = GetHeaderColumnAt(xPos);
			if ((headerColumn != null) && InRowRange(yPos))
			{
				if (!headerColumn.InResizeArea(xPos))
				{
					int headerIndex = _header.IndexOf(headerColumn);
					if ((headerIndex > 0) && (((HeaderColumn)_header[--headerIndex]).InResizeArea(xPos)))
						headerColumn = (HeaderColumn)_header[headerIndex];
					else
						headerColumn = null;
				}
			}
			else
				return null;
			return headerColumn;
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

		protected internal virtual void DoColumnResize(object sender, EventArgs args)
		{
			if (OnColumnResize != null)
				OnColumnResize(sender, args);
		}

		protected virtual void DoColumnMouseLeave(GridColumn column)
		{
			if (OnColumnMouseLeave != null)
				OnColumnMouseLeave(this, column, EventArgs.Empty);
		}

		protected virtual void DoColumnMouseEnter(GridColumn column, int xPos, int yPos)
		{
			if (OnColumnMouseEnter != null)
				OnColumnMouseEnter(this, column, xPos, yPos, EventArgs.Empty);
		}

		protected virtual void DoColumnMouseMove(GridColumn column, Point location)
		{
			UpdateCursor();
			column.MouseMove(location, GetRowAt(location.Y));
			if (OnColumnMouseMove != null)
				OnColumnMouseMove(this, column, location.X, location.Y, EventArgs.Empty);
		}

		protected virtual void DoHeaderMouseMove(GridColumn column, int xPos, int yPos)
		{
			if (_columnResizing && (GetResizeableColumn(xPos, yPos) != null))
			{
				if (Cursor != WinForms.Cursors.VSplit)
				{
					_previousCursor = Cursor;
					Cursor = WinForms.Cursors.VSplit;
				}
			}
			else
				UpdateCursor();
			if (OnHeaderMouseMove != null)
				OnHeaderMouseMove(this, column, xPos, yPos, EventArgs.Empty);
		}

		protected void DoMouseLeaveGrid()
		{
			try
			{
				UpdateCursor();
				if (_previousGridColumn != null)
					DoColumnMouseLeave(_previousGridColumn);
			}
			finally
			{
				_previousGridColumn = null;
			}
		}

		private bool InRowRange(int pixelY)
		{
			if (pixelY <= _headerHeight)
				return true;
			int row = GetRowAt(pixelY);
			return ((row >= 0) && _link.Active && (row <= _link.LastOffset));
		}

		protected internal void WMMouseMove(ref WinForms.Message message)
		{
			int yPos = NativeMethods.Utilities.HiWord(message.LParam);
			int xPos = NativeMethods.Utilities.LowWord(message.LParam);
			int key = NativeMethods.Utilities.LowWord(message.WParam);
			if
				(
				(((key | NativeMethods.MK_LBUTTON) != 0) || ((key | NativeMethods.MK_RBUTTON) != 0)) &&
				(_dragObject != null) &&
				!_draggingColumn &&
				((Math.Abs(_dragObject.StartDragPoint.X - xPos) > _beforeDragSize.Width) || ((Math.Abs(_dragObject.StartDragPoint.Y - yPos) > _beforeDragSize.Height)))
				)
				DoGridColumnDragDrop();
		
			if (_resizingColumn != null)
			{
				int newColumnWidth = xPos - _resizingColumn.Offset - (_cellPadding.Width << 1);
				if (newColumnWidth >= ClientRectangle.Width)
					newColumnWidth = ClientRectangle.Width - HeaderColumn.ResizePixelWidth;
				_resizingColumn.Column.Width = Math.Max(1, newColumnWidth);
			}
		
			HeaderColumn headerColumn = GetHeaderColumnAt(xPos);
			if ((headerColumn != null) && InRowRange(yPos))
			{
				if (_previousGridColumn != headerColumn.Column)
				{
					try
					{
						if (_previousGridColumn != null)
							DoColumnMouseLeave(_previousGridColumn);
						DoColumnMouseEnter(headerColumn.Column, xPos, yPos);
					}
					finally
					{
						_previousGridColumn = headerColumn.Column;
					}
				}
				if (yPos <= _headerHeight)
					DoHeaderMouseMove(headerColumn.Column, xPos, yPos);
				else
				{
					DoColumnMouseMove(headerColumn.Column, new Point(xPos, yPos));
				}
			}
			else
				DoMouseLeaveGrid();
		}

		protected override void OnMouseLeave(EventArgs args)
		{
			base.OnMouseLeave(args);
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

		protected internal GridColumn InternalGetColumnAt(int xPos)
		{
			HeaderColumn headerColumn = GetHeaderColumnAt(xPos);
			if (headerColumn != null)
				return headerColumn.Column;
			else
				return null;
		}

		/// <summary> Returns GridColumn at specific pixel x position. </summary>
		/// <param name="xPos"> The x location of the pixel. </param>
		/// <returns>GridColumn</returns>
		public GridColumn GetColumnAt(int xPos)
		{
			return InternalGetColumnAt(xPos);	
		}

		protected virtual void DoBeforeContextPopup(Point popupPoint)
		{
			if (BeforeContextPopup != null)
				BeforeContextPopup(this, InternalGetColumnAt(popupPoint.X), popupPoint, EventArgs.Empty);
		}

		protected virtual void DoAfterContextPopup(Point popupPoint)
		{
			if (AfterContextPopup != null)
				AfterContextPopup(this, InternalGetColumnAt(popupPoint.X), popupPoint, EventArgs.Empty);
		}

		protected virtual void DoBeforeHeaderContextPopup(Point popupPoint)
		{
			if (BeforeHeaderContextPopup != null)
				BeforeHeaderContextPopup(this, InternalGetColumnAt(popupPoint.X), popupPoint, EventArgs.Empty);
		}

		protected virtual void DoAfterHeaderContextPopup(Point popupPoint)
		{
			if (AfterHeaderContextPopup != null)
				AfterHeaderContextPopup(this, InternalGetColumnAt(popupPoint.X), popupPoint, EventArgs.Empty);
		}

		protected internal virtual void WMContextMenu(ref WinForms.Message message)
		{
			int yPos = NativeMethods.Utilities.HiWord(message.LParam);
			int xPos = NativeMethods.Utilities.LowWord(message.LParam);
			Point popupPoint = PointToClient(new Point(xPos, yPos));
			if (popupPoint.Y <= _headerHeight)
			{
				DoBeforeHeaderContextPopup(popupPoint);
				if (HeaderContextMenu != null)
					HeaderContextMenu.Show(this, popupPoint); 
				DoAfterHeaderContextPopup(popupPoint);
			}
			else
			{
				DoBeforeContextPopup(popupPoint);
				if (ContextMenu != null)
					ContextMenu.Show(this, popupPoint);
				DoAfterContextPopup(popupPoint);
			}
		}

		protected override void WndProc(ref WinForms.Message message)
		{
			try
			{
				bool handled = false;
				switch (message.Msg)
				{
					case NativeMethods.WM_HSCROLL :
						WMHScroll(ref message);
						break;
					case NativeMethods.WM_VSCROLL :
						WMVScroll(ref message);
						break;
					case NativeMethods.WM_MOUSEMOVE :
						WMMouseMove(ref message);
						break;
					case NativeMethods.WM_CONTEXTMENU :
						WMContextMenu(ref message);
						handled = true;
						break;
				}
				if (!handled)
					base.WndProc(ref message);
			}
			catch (Exception exception)
			{
				WinForms.Application.OnThreadException(exception);
				// don't rethrow
			}
		}

		protected override void OnClick(EventArgs args)
		{
			base.OnClick(args);
			if (CanFocus)
				Focus();
			if (_columnMouseDown != null)
				_columnMouseDown.Column.MouseClick(args, _rowIndexMouseDown);
		}

		protected internal int GetRowAt(int offsetY)
		{
			if ((offsetY > _headerHeight) && !Disposing && !IsDisposed)
			{
				int i = 0;
				foreach (GridRow row in _rows)
				{
					if ((row.ClientRectangle.Top <= offsetY) && (row.ClientRectangle.Bottom > offsetY))
						return i;
					++i;
				}
			}
			return -1;
		}

		protected internal void SelectRow(int row)
		{
			_link.DataSet.MoveBy(row - _link.ActiveOffset);
		}

		private HeaderColumn _columnMouseDown;
		private int _rowIndexMouseDown;
		private Point _mouseDownAt;
		protected override void OnMouseDown(WinForms.MouseEventArgs args)
		{
			base.OnMouseDown(args);
			if (CanFocus)
				Focus();
			_mouseDownAt = new Point(args.X, args.Y);
			_columnMouseDown = GetHeaderColumnAt(_mouseDownAt.X);
			_rowIndexMouseDown = GetRowAt(_mouseDownAt.Y);
			if (_link.Active)
			{
				if (_mouseDownAt.Y <= _headerHeight)
				{
					_resizingColumn = GetResizeableColumn(_mouseDownAt.X, _mouseDownAt.Y);
					if ((_columnMouseDown != null) && (_resizingColumn == null) && (args.Clicks == 1))
					{
						if (args.Button == WinForms.MouseButtons.Left)
							_dragObject = new ColumnDragObject(this, _columnMouseDown.Column, _mouseDownAt, new Size(_mouseDownAt.X - _columnMouseDown.Offset, _mouseDownAt.Y), GetDragImage(_mouseDownAt)); 
						_columnMouseDown.Column.HeaderMouseDown(args);
					}
				}
				
				try
				{
					if (_columnMouseDown != null)
						_columnMouseDown.Column.MouseDown(args, _rowIndexMouseDown);
				}
				finally
				{
					if (
						((args.Button == WinForms.MouseButtons.Left) && ((_clickSelectsRow & ClickSelectsRow.LeftClick) != 0)) ||
						((args.Button == WinForms.MouseButtons.Right) && ((_clickSelectsRow & ClickSelectsRow.RightClick) != 0)) ||
						((args.Button == WinForms.MouseButtons.Middle) && ((_clickSelectsRow & ClickSelectsRow.MiddleClick) != 0))
						)
					{
						if ((_rowIndexMouseDown >= 0) && (_rowIndexMouseDown <= _link.LastOffset))
							SelectRow(_rowIndexMouseDown);
					}
				}

			}
		}
		
		protected Schema.Order FindOrderForColumn(DataColumn column)
		{
			TableDataSet dataSet = _link.DataSet as TableDataSet;
			if (dataSet != null)
			{
				// Returns the order that has the given column as the first column
				if ((dataSet.Order != null) && (dataSet.Order.Columns.Count >= 1) && (dataSet.Order.Columns[0].Column.Name == column.ColumnName))
					return dataSet.Order;
					
				foreach (Schema.Order order in _link.DataSet.TableVar.Orders)
					if ((order.Columns.Count >= 1) && (order.Columns[0].Column.Name == column.ColumnName))
						return order;
					
				foreach (Schema.Key key in _link.DataSet.TableVar.Keys)
					if (!key.IsSparse && (key.Columns.Count >= 1) && (key.Columns[0].Name == column.ColumnName))
						return new Schema.Order(key);
			}
					
			return null;
		}

		protected virtual void ChangeOrderTo(DataColumn column)
		{
			if (_link.Active)
			{
				TableDataSet dataSet = _link.DataSet as TableDataSet;
				if (dataSet != null)
				{
					Schema.Order order = FindOrderForColumn(column);
					if (order == null)
					{
						dataSet.OrderString = "order { " + column.ColumnName + " asc }";
						InvalidateHeader();
					}
					else
					{
						Schema.Order currentOrder = dataSet.Order;
						int currentColumnIndex = currentOrder == null ? -1 : currentOrder.Columns.IndexOf(column.ColumnName);
						
						bool descending = (currentColumnIndex >= 0) && currentOrder.Columns[currentColumnIndex].Ascending;
						if (!descending ^ order.Columns[column.ColumnName].Ascending)
							order = new Schema.Order(order, true);
							
						dataSet.Order = order;
						InvalidateHeader();
					}
				}
			}
		}

		private bool ColumnInKey(DataColumn column)
		{
			if (_link.Active)
				foreach (Schema.Key key in _link.DataSet.TableVar.Keys)
					if (!key.IsSparse && (key.Columns.IndexOfName(column.ColumnName) >= 0))
						return true;
			return false;
		}

		private bool ColumnInOrder(DataColumn column)
		{
			if (_link.Active)
				foreach (Schema.Order order in _link.DataSet.TableVar.Orders)
					if (order.Columns.IndexOf(column.ColumnName) >= 0)
						return true;
			return false;
		}

		protected void RebuildViewOrder(DataColumn column)
		{
			if ((_orderChange == OrderChange.Key) && (ColumnInKey(column) || ColumnInOrder(column)))
				ChangeOrderTo(column);
			if (_orderChange == OrderChange.Force)
				ChangeOrderTo(column);
		}

		protected override void OnMouseUp(WinForms.MouseEventArgs args)
		{
			base.OnMouseUp(args);
			try
			{
				bool wasResizingColumn = _resizingColumn != null;
				_resizingColumn = null;
				_dragObject = null;
				int rowIndexMouseUp = GetRowAt(args.Y);
				if (_columnMouseDown != null)
				{
					if ((_mouseDownAt.Y >= 0) && (_mouseDownAt.Y <= _headerHeight))
					{
						if 
							(
							(_columnMouseDown.Column is DataColumn) &&
							(args.Button == System.Windows.Forms.MouseButtons.Left) &&
							!wasResizingColumn &&
							!_draggingColumn &&
							(args.Y <= _headerHeight)
							)
							RebuildViewOrder((DataColumn)_columnMouseDown.Column);
						_columnMouseDown.Column.HeaderMouseUp(args);
					}
					_columnMouseDown.Column.MouseUp(args, rowIndexMouseUp);
				}
			}
			finally
			{
				_columnMouseDown = null;
			}
		}

		protected override void OnMouseWheel(WinForms.MouseEventArgs args)
		{
			base.OnMouseWheel(args);
			if (_link.Active)
			{
				if (args.Delta > 0)
					Prior();
				else if (args.Delta < 0)
					Next();
			}
		}

		private HeaderColumn GetHeaderColumnAt(int offsetX)
		{
			HeaderColumn headerColumn = null;
			int headerIndex = 0;
			bool found = false;
			while (!found && (headerIndex < _header.Count))
			{
				headerColumn = (HeaderColumn)_header[headerIndex++];
				found = ((offsetX >= headerColumn.Offset) && (offsetX < (headerColumn.Offset + headerColumn.Width + _cellPadding.Width)));
			}
			return found ? headerColumn : null;
		}

		private bool _draggingColumn;
		private ColumnDragObject _dragObject;
		private DragDropTimer _dragScrollTimer;

		private void OnDragScrollRightElapsed(object sender, EventArgs args)
		{
			InternalScrollRight();
		}

		private void OnDragScrollLeftElapsed(object sender, EventArgs args)
		{
			InternalScrollLeft();
		}

		protected virtual Image GetDragImage(Point location)
		{
			HeaderColumn headerColumn = GetHeaderColumnAt(location.X);
			Bitmap bitmap;
			using (Graphics graphics = CreateGraphics())
			{
				bitmap = new Bitmap(headerColumn.Width, _headerHeight, graphics);
			}
			using (Graphics graphics = Graphics.FromImage(bitmap))
				using (StringFormat format = new StringFormat(StringFormatFlags.NoWrap))
				{
					format.Alignment = AlignmentConverter.ToStringAlignment(headerColumn.Column.HeaderTextAlignment);
					headerColumn.Paint(graphics, new Rectangle(new Point(0,0), bitmap.Size), format, Color.FromArgb(DefaultDragImageAlpha, FixedColor));
				}
			return bitmap;
		}

		protected void DoGridColumnDragDrop()
		{
			if (_dragObject != null)
			{
				bool allowDrop = AllowDrop;
				_draggingColumn = true;
				try
				{
					AllowDrop = true;
					WinForms.DataObject dataObject = new WinForms.DataObject();
					dataObject.SetData(_dragObject);
					DoDragDrop(dataObject, WinForms.DragDropEffects.Move | WinForms.DragDropEffects.Scroll);
				}
				finally
				{
					DisposeDragDropTimer();
					_draggingColumn = false;
					_dragObject = null;
					AllowDrop = allowDrop;
					InvalidateHeader();	//Hide the drag image.
				}
			}
		}

		private HeaderColumn GetDropTarget(int xPos)
		{
			HeaderColumn headerColumn = GetHeaderColumnAt(xPos);
			if (headerColumn != null)
				return headerColumn;
			else
				return (HeaderColumn)_header[_header.Count - 1];
		}

		protected override void OnDragEnter(WinForms.DragEventArgs args)
		{
			if (_draggingColumn)
			{
				args.Effect = WinForms.DragDropEffects.Move;
				Point point = PointToClient(new Point(args.X, args.Y));
				UpdateDragTimer(point);
				UpdateDragObject(point, args.Data);
			}
			else
				args.Effect = WinForms.DragDropEffects.None;
			base.OnDragEnter(args);
		}

		protected override void OnDragOver(WinForms.DragEventArgs args)
		{
			if (_draggingColumn)
			{
				Point point = PointToClient(new Point(args.X, args.Y));
				args.Effect = WinForms.DragDropEffects.Move;
				UpdateDragTimer(point);
				UpdateDragObject(point, args.Data);
			}
			base.OnDragOver(args);
		}

		protected void UpdateDragObject(Point cursorPosition, WinForms.IDataObject dataObject)
		{
			WinForms.DataObject draggedData = new WinForms.DataObject(dataObject);
			if (draggedData.GetDataPresent(typeof(ColumnDragObject)))
			{
				ColumnDragObject dragObject = (ColumnDragObject)draggedData.GetData(_dragObject.GetType().ToString());
				if (dragObject != null)
				{
					Rectangle invalidateRect = dragObject.HighlightRect;
					HeaderColumn targetColumn = GetDropTarget(cursorPosition.X);
					bool isRightOfCenter = (cursorPosition.X > (targetColumn.Offset + targetColumn.Width - (targetColumn.Width / 2)));
					if (isRightOfCenter)
						dragObject.HighlightRect = new Rectangle(targetColumn.Offset + targetColumn.Width - 2, 0, 4, _headerHeight); 
					else
						dragObject.HighlightRect = new Rectangle(targetColumn.Offset - 2, 0, 4, _headerHeight); 

					if 
						(
						dragObject.HighlightRect.Equals(invalidateRect) ||
						invalidateRect.Contains(dragObject.HighlightRect)
						)
						invalidateRect = Rectangle.Empty;

					if (targetColumn.Column != null)
					{
						int targetIndex = _columns.IndexOf(targetColumn.Column);
						int dragIndex = _columns.IndexOf(dragObject.DragColumn);
						if (dragIndex < targetIndex)
						{
							if (isRightOfCenter)
								++targetIndex;
							--targetIndex;
						}
						else if (dragIndex > targetIndex)
						{
							if (isRightOfCenter)
								++targetIndex;
						}
						dragObject.DropTargetIndex = targetIndex;
						invalidateRect = Rectangle.Union(invalidateRect, dragObject.HighlightRect); 
					}

					if (dragObject.DragImage != null)
					{
						Rectangle oldImageLocRect = new Rectangle(dragObject.ImageLocation, dragObject.DragImage.Size);
						Rectangle newImageLocRect = new Rectangle(cursorPosition.X - dragObject.CursorCenterOffset.Width, 0, dragObject.DragImage.Width, dragObject.DragImage.Height); 
						if (!newImageLocRect.Location.Equals(dragObject.ImageLocation))
						{
							dragObject.ImageLocation = newImageLocRect.Location;
							if (!invalidateRect.IsEmpty)
								invalidateRect = Rectangle.Union(invalidateRect,Rectangle.Union(oldImageLocRect, newImageLocRect));
							else
								invalidateRect = Rectangle.Union(oldImageLocRect, newImageLocRect);
						}
					}
					if (!invalidateRect.IsEmpty)
						InternalInvalidate(invalidateRect, true);
				}
			}
		}

		private void DisposeDragDropTimer()
		{
			if (_dragScrollTimer != null)
			{
				try
				{
					_dragScrollTimer.Tick -= new EventHandler(OnDragScrollLeftElapsed);
					_dragScrollTimer.Tick -= new EventHandler(OnDragScrollRightElapsed);
					_dragScrollTimer.Dispose();
				}
				finally
				{
					_dragScrollTimer = null;
				}
			}
		}

		private void UpdateDragTimer(Point point)
		{
			if (ClientRectangle.Width < (2 * AutoScrollWidth + 1))
				return;
			if (point.X >= (ClientRectangle.Width - AutoScrollWidth))
			{
				if (_dragScrollTimer == null)
				{
					if (point.X < ClientRectangle.Width)
						_dragScrollTimer = new DragDropTimer(_dragDropScrollInterval);
					else
						_dragScrollTimer = new DragDropTimer(_dragDropScrollInterval - (_dragDropScrollInterval / 2));
					_dragScrollTimer.Tick += new EventHandler(OnDragScrollRightElapsed);
					_dragScrollTimer.ScrollDirection = ScrollDirection.Right;
					_dragScrollTimer.Enabled = true;
				}
				else 
				{
					if (_dragScrollTimer.ScrollDirection == ScrollDirection.Left)
					{
						_dragScrollTimer.Tick -= new EventHandler(OnDragScrollLeftElapsed);
						_dragScrollTimer.Tick += new EventHandler(OnDragScrollRightElapsed);
						_dragScrollTimer.ScrollDirection = ScrollDirection.Right;
					}
				}
			}
			else if (point.X <= AutoScrollWidth)
			{
				if (_dragScrollTimer == null)
				{
					if (point.X > 0)
						_dragScrollTimer = new DragDropTimer(DragDropScrollInterval);
					else
						_dragScrollTimer = new DragDropTimer(_dragDropScrollInterval - (_dragDropScrollInterval / 2));
					_dragScrollTimer.Tick += new EventHandler(OnDragScrollLeftElapsed);
					_dragScrollTimer.ScrollDirection = ScrollDirection.Left;
					_dragScrollTimer.Enabled = true;
				}
				else 
				{
					if (_dragScrollTimer.ScrollDirection == ScrollDirection.Right)
					{
						_dragScrollTimer.Tick -= new EventHandler(OnDragScrollRightElapsed);
						_dragScrollTimer.Tick += new EventHandler(OnDragScrollLeftElapsed);
						_dragScrollTimer.ScrollDirection = ScrollDirection.Left;
					}
				}
			}
			else
				DisposeDragDropTimer();
		}

		protected override void OnDragDrop(WinForms.DragEventArgs args)
		{
			try
			{
				try
				{
					DisposeDragDropTimer();
					Point point = PointToClient(new Point(args.X, args.Y));
					UpdateDragObject(point, args.Data);
					HeaderColumn targetColumn = GetDropTarget(point.X);
					if (targetColumn.Column != null)
					{
						WinForms.DataObject draggedData = new WinForms.DataObject(args.Data);
						if (draggedData.GetDataPresent(typeof(ColumnDragObject)))
						{
							ColumnDragObject dragObject = (ColumnDragObject)draggedData.GetData(_dragObject.GetType().ToString());
							if ((dragObject != null) && (dragObject.DragColumn != targetColumn.Column))
							{
								_columns.Remove(dragObject.DragColumn);
								_columns.Insert(dragObject.DropTargetIndex, dragObject.DragColumn);
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
					base.OnDragDrop(args);
				}
			}
			catch (Exception exception)
			{
				WinForms.Application.OnThreadException(exception);
			}
		}

		protected override bool IsInputKey(WinForms.Keys keyData)
		{
			switch (keyData)
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
			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(WinForms.KeyEventArgs args)
		{
			if (_link.Active)
			{
				switch (args.KeyData)
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
						base.OnKeyDown(args);
						return;
				}
				args.Handled = true;
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
			foreach (GridColumn column in _columns)
				if (column.CanToggle())
				{
					column.Toggle();
					break;
				}
		}
	}

	internal class GridRows : List
	{
		public GridRows(DBGrid owner)
		{
			AllowNulls = false;
			_grid = owner;
		}

		private DBGrid _grid;
		protected DBGrid Grid { get { return _grid; } } 

		protected override void Validate(object value)
		{
			base.Validate(value);
			if (!(value is GridRow))
				throw new ControlsException(ControlsException.Codes.GridRowsOnly);
		}

		public new GridRow this[int index]
		{
			get { return (GridRow)base[index]; }
		}
	}

	internal class GridRow
	{
		internal GridRow(DAEData.IRow row, Rectangle clientRect)
		{
			_row = row;
			_clientRectangle = clientRect;
		}

		private DAEData.IRow _row;
		public DAEData.IRow Row { get { return _row; } }

		private Rectangle _clientRectangle;
		public Rectangle ClientRectangle
		{
			get { return _clientRectangle; }
			set	{ _clientRectangle = value;	}
		}
	}

	internal class HeaderColumn
	{
		public const int ResizePixelWidth = 4;

		internal HeaderColumn(GridColumn column, int offset, int width)
		{
			Column = column;
			Offset = offset;
			Width = width;
		}
		
		public GridColumn Column;
		public int Offset;
		public int Width;

		/// <summary> Determines if the cursor is in the resize area of a column. </summary>
		/// <param name="xPos"> X position of the cursor. </param>
		/// <returns> True if the cursors x position is in the resize area between two columns, otherwise false. </returns>
		public bool InResizeArea(int xPos)
		{
			int offsetX = Offset + Width;
			return ((xPos >= offsetX - ResizePixelWidth) && (xPos <= offsetX + ResizePixelWidth));
		}
		
		/// <summary> Paints the header of a column. </summary>
		/// <param name="graphics"> The graphics object for the Grid control. </param>
		/// <param name="cellRect"> The area to paint on. </param>
		/// <param name="format"> The StringFormat to use when painting the title. </param>
		/// <param name="fixedColor"> The background color for the header. </param>
		protected internal void Paint(Graphics graphics, Rectangle cellRect, StringFormat format, System.Drawing.Color fixedColor)
		{
			Column.PaintHeaderCell(graphics, cellRect, format, fixedColor);	
		}
	}

	[ToolboxItem(false)]
	[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.GridColumnCollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
	[DesignerSerializer(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.GridColumnsSerializer), typeof(CodeDomSerializer))]
	public class GridColumns : DisposableList<GridColumn>
	{

		protected internal GridColumns(DBGrid grid) : base()
		{
			_grid = grid;
			_visibleColumns = new List<GridColumn>();
		}
		
		private DBGrid _grid;
		/// <summary> Owner of columns. </summary>
		public DBGrid Grid { get { return _grid; } }

		/// <summary> Returns all grid columns as an array of grid columns. </summary>
		public GridColumn[] All
		{
			get
			{
				GridColumn[] columns = new GridColumn[Count];
				for(int i = 0; i < Count; i++)
					columns[i] = this[i];
				return columns;
			}
		}

		protected override void Adding(GridColumn value, int index)
		{
			ShowDefaultColumns = value is DataColumn ? ((DataColumn)value).IsDefaultGridColumn : false;
			UpdateVisibleColumns(value, true);
			base.Adding(value, index);
			((GridColumn)value).AddingToGrid(this);
			Grid.InternalUpdateGrid(false, true);
		}

		protected override void Removed(GridColumn value, int index)
		{
			UpdateVisibleColumns(value, false);
			base.Removed(value, index);
			((GridColumn)value).RemovingFromGrid(this);
			if (!IsDisposed)
			{
				Grid.InternalUpdateGrid(false, true);
				if ((Count == 0) || !HasDefaultColumn())
					RestoreDefaultColumns();
			}
		}

		private void DisposeDefaultGridColumns()
		{
			for (int i = Count - 1; i >= 0; i--)
			{
				DataColumn gridColumn = this[i] as DataColumn;
				if (gridColumn != null && gridColumn.IsDefaultGridColumn)
					RemoveAt(i);
			}
		}

		private void BuildDefaultGridColumns()
		{
			DataColumn gridColumn;
			foreach (DAESchema.TableVarColumn column in Grid.Link.DataSet.TableVar.Columns)
			{
				gridColumn = this[column.Name];
				if (gridColumn == null)
				{
					DAEClient.DataField field = Grid.Link.DataSet.Fields[column.Name];
					GridColumn newGridColumn;
					if (field.DataType.Is(Grid.Link.DataSet.Process.DataTypes.SystemBoolean))
						newGridColumn = new CheckBoxColumn(Grid, column.Name);
					else
						newGridColumn = new TextColumn(Grid, column.Name);
					Add(newGridColumn);
					newGridColumn.OnAfterWidthChanged += new EventHandler(Grid.DoColumnResize);
				}
			}
		}

		private void UpdateDefaultColumns()
		{
			BuildDefaultGridColumns();
			//Remove any default grid columns that do not have valid DataBufferColumn name.
			for (int i = Count - 1; i >= 0; i--)
			{
				DataColumn gridColumn = this[i] as DataColumn;
				if (gridColumn != null && gridColumn.IsDefaultGridColumn && !Grid.Link.DataSet.TableType.Columns.Contains(gridColumn.ColumnName))
					RemoveAt(i);
			}
		}

		private bool _updatingColumns;
		/// <summary> Update state of the columns collection. </summary>
		public bool UpdatingColumns { get { return _updatingColumns; } }

		protected internal void UpdateColumns(bool showDefaultColumns)
		{
			_updatingColumns = true;
			try
			{
				if (!showDefaultColumns)
					DisposeDefaultGridColumns();
				else
				{
					if (Grid.Link.Active && showDefaultColumns)
						UpdateDefaultColumns();
				}
				foreach (GridColumn column in this)
					if (column is DataColumn)
						((DataColumn)column).InternalCheckColumnExists(Grid != null ? Grid.Link : null);
			}
			finally
			{
				_updatingColumns = false;
			}
		}

		public bool HasDefaultColumn()
		{
			foreach (GridColumn column in this)
				if ((column is DataColumn) && ((DataColumn)column).IsDefaultGridColumn)
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

		private int GetVisibleInsertIndex(GridColumn value)
		{
			int startIndex = IndexOf(value);
			while (startIndex > 0)
				if (this[--startIndex].Visible)
					return _visibleColumns.IndexOf(this[startIndex]) + 1;
			return 0;
		}

		private void AddVisibleColumn(GridColumn column)
		{
			if (column.Visible)
			{
				if ((column is DataColumn) && (((DataColumn)column).ColumnName != String.Empty))
					_visibleColumns.Insert(GetVisibleInsertIndex(column), column);
				else
					_visibleColumns.Insert(GetVisibleInsertIndex(column), column);
			}
		}

		private void RemoveVisibleColumn(GridColumn column, bool conditional)
		{
			if (_visibleColumns.IndexOf(column) >= 0)
			{
				if (!conditional)
					_visibleColumns.Remove(column);
				else
				{
					if (!column.Visible || ((column is DataColumn) && (((DataColumn)column).ColumnName == String.Empty)))
						_visibleColumns.Remove(column);
				}
			}
		}

		private void ColumnChanged(GridColumn column)
		{
			if (_visibleColumns.IndexOf(column) < 0)
				AddVisibleColumn(column);
			else
				RemoveVisibleColumn(column, true);
		}

		internal void InternalColumnChanged(GridColumn column)
		{
			ColumnChanged(column);
		}

		protected void UpdateVisibleColumns(GridColumn value, bool inserting)
		{
			if (_visibleColumns != null)
			{
				if (inserting)
					AddVisibleColumn(value);
				else
					RemoveVisibleColumn(value, false);
			}
		}

		/// <summary> Returns a data-aware grid column given the column name. </summary>
		public DataColumn this[string columnName]
		{
			get
			{
				for (int i = Count - 1; i >= 0; i--)
					if ((this[i] is DataColumn) && (((DataColumn)this[i]).ColumnName == columnName))
						return (DataColumn)this[i];
				return null;
			}
		}

		private List<GridColumn> _visibleColumns;
		/// <summary> Collection of visible grid columns. </summary>
		public List<GridColumn> VisibleColumns
		{
			get { return _visibleColumns; }
		}

		/// <summary> The number of columns not visible in a given range. </summary>
		/// <param name="startIndex"> Start of range. </param>
		/// <param name="endIndex"> End of range. </param>
		/// <returns> The number of columns not visible in the range. </returns>
		public int InvisibleColumnCount(int startIndex, int endIndex)
		{
			if (startIndex > endIndex)
				throw new ControlsException(ControlsException.Codes.InvalidRange);
			if ((startIndex == 0) && (endIndex >= (Count - 1)))
				return Count - _visibleColumns.Count;
			int count = 0;
			for (int i = startIndex; i <= endIndex; ++i)
				if (!((GridColumn)this[i]).Visible)
					++count;
			return count;
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

		public int MaxScrollIndex(int clientWidth, int padding)
		{
			int lastWidth = 0;
			int lastColumnIndex = VisibleColumns.Count; 
			while ((lastColumnIndex > 0) && (lastWidth < clientWidth))
				lastWidth += ((GridColumn)VisibleColumns[--lastColumnIndex]).Width + padding;
				
			if (lastWidth > clientWidth)
				return ++lastColumnIndex;
			else
				return lastColumnIndex;
		}
	}

	/// <summary> Gets an instance descriptor to seraialize grid columns. </summary>
	public class DataColumnTypeConverter : TypeConverter
	{
		public DataColumnTypeConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(context, destinationType);
		}

		protected virtual int ShouldSerializeSignature(DataColumn column)
		{
			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(column);
			int serializeSignature = 0;
			if (propertyDescriptors["ColumnName"].ShouldSerializeValue(column))
				serializeSignature |= 1;

			if (propertyDescriptors["Title"].ShouldSerializeValue(column))
				serializeSignature |= 2;
			
			if (propertyDescriptors["Width"].ShouldSerializeValue(column))
				serializeSignature |= 4;

			if (propertyDescriptors["HorizontalAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 8;

			if (propertyDescriptors["HeaderTextAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 16;

			if (propertyDescriptors["VerticalAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 32;

			if (propertyDescriptors["BackColor"].ShouldSerializeValue(column))
				serializeSignature |= 64;

			if (propertyDescriptors["Visible"].ShouldSerializeValue(column))
				serializeSignature |= 128;

			if (propertyDescriptors["Header3DStyle"].ShouldSerializeValue(column))
				serializeSignature |= 256;

			if (propertyDescriptors["Header3DSide"].ShouldSerializeValue(column))
				serializeSignature |= 512;

			if (propertyDescriptors["MinRowHeight"].ShouldSerializeValue(column))
				serializeSignature |= 1024;

			if (propertyDescriptors["MaxRowHeight"].ShouldSerializeValue(column))
				serializeSignature |= 2048;

			if (propertyDescriptors["Font"].ShouldSerializeValue(column))
				serializeSignature |= 4096;

			if (propertyDescriptors["ForeColor"].ShouldSerializeValue(column))
				serializeSignature |= 8192;

			return serializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(DataColumn column)
		{
			ConstructorInfo info;
			Type type = column.GetType();
			switch (ShouldSerializeSignature(column))
			{
				case 0:
					info = type.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( info, new object[] {} );
				case 1:
					info = type.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						info,
						new object[] { column.ColumnName }
						);
				case 2:
				case 3:
					info = type.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						info,
						new object[]
							{
								column.ColumnName,
								column.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								column.ColumnName,
								column.Title,
								column.Width
							}
						);
				default:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								column.ColumnName,
								column.Title,
								column.Width,
								column.HorizontalAlignment,
								column.HeaderTextAlignment,
								column.VerticalAlignment,
								column.BackColor,
								column.Visible,
								column.Header3DStyle,
								column.Header3DSide,
								column.MinRowHeight,
								column.MaxRowHeight,
								column.Font,
								column.ForeColor
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			DataColumn column = value as DataColumn;
			if 
				(
				(destinationType == typeof(InstanceDescriptor)) &&
				(column != null)
				)
				return GetInstanceDescriptor(column);	
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	/// <summary> Gets an instance descriptor to seraialize grid columns. </summary>
	public class TextColumnTypeConverter : DataColumnTypeConverter
	{
		public TextColumnTypeConverter() : base() {}
		
		protected override int ShouldSerializeSignature(DataColumn column)
		{
			TextColumn localColumn = column as TextColumn;
			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(column);
			int serializeSignature = base.ShouldSerializeSignature(localColumn);
			//ToDo: Should be a better way to get next should serialize constant.
			//Because, if the parent class SerializeSignature changes all descendants will need to change as well.
			if (propertyDescriptors["WordWrap"].ShouldSerializeValue(localColumn))
				serializeSignature |= 16384 ;
			if (propertyDescriptors["VerticalText"].ShouldSerializeValue(localColumn))
				serializeSignature |= 32768;
			return serializeSignature;
		}

		protected override InstanceDescriptor GetInstanceDescriptor(DataColumn column)
		{
			TextColumn localColumn = column as TextColumn;
			ConstructorInfo info;
			Type type = localColumn.GetType();
			switch (ShouldSerializeSignature(localColumn))
			{
				case 0:
					info = type.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( info, new object[] {} );
				case 1:
					info = type.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						info,
						new object[] { localColumn.ColumnName }
						);
				case 2:
				case 3:
					info = type.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.Width
							}
						);
				default:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.Width,
								localColumn.HorizontalAlignment,
								localColumn.HeaderTextAlignment,
								localColumn.VerticalAlignment,
								localColumn.BackColor,
								localColumn.Visible,
								localColumn.Header3DStyle,
								localColumn.Header3DSide,
								localColumn.MinRowHeight,
								localColumn.MaxRowHeight,
								localColumn.Font,
								localColumn.ForeColor,
								localColumn.WordWrap,
								localColumn.VerticalText
							}
						);
			}
		}
	}

	/// <summary> Gets an instance descriptor to seraialize grid columns. </summary>
	public class LinkColumnTypeConverter : DataColumnTypeConverter
	{
		public LinkColumnTypeConverter() : base() {}
		
		protected override int ShouldSerializeSignature(DataColumn column)
		{
			LinkColumn localColumn = column as LinkColumn;
			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(column);
			int serializeSignature = base.ShouldSerializeSignature(localColumn);
			//ToDo: Should be a better way to get next should serialize constant.
			//Because, if the parent class SerializeSignature changes all descendants will need to change as well.
			if (propertyDescriptors["WordWrap"].ShouldSerializeValue(localColumn))
				serializeSignature |= 16384 ;
			return serializeSignature;
		}

		protected override InstanceDescriptor GetInstanceDescriptor(DataColumn column)
		{
			LinkColumn localColumn = column as LinkColumn;
			ConstructorInfo info;
			Type type = localColumn.GetType();
			switch (ShouldSerializeSignature(localColumn))
			{
				case 0:
					info = type.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( info, new object[] {} );
				case 1:
					info = type.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						info,
						new object[] { localColumn.ColumnName }
						);
				case 2:
				case 3:
					info = type.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.Width
							}
						);
				default:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.Width,
								localColumn.HorizontalAlignment,
								localColumn.HeaderTextAlignment,
								localColumn.VerticalAlignment,
								localColumn.BackColor,
								localColumn.Visible,
								localColumn.Header3DStyle,
								localColumn.Header3DSide,
								localColumn.MinRowHeight,
								localColumn.MaxRowHeight,
								localColumn.Font,
								localColumn.ForeColor,
								localColumn.WordWrap
							}
						);
			}
		}
	}

	/// <summary> Gets an instance descriptor to seraialize CheckBox grid columns. </summary>
	public class CheckBoxColumnTypeConverter : DataColumnTypeConverter
	{
		public CheckBoxColumnTypeConverter() : base() {}

		protected override int ShouldSerializeSignature(DataColumn column)
		{
			return base.ShouldSerializeSignature(column) | 512;
		}

		protected override InstanceDescriptor GetInstanceDescriptor(DataColumn column)
		{
			CheckBoxColumn localColumn = column as CheckBoxColumn;
			ConstructorInfo info;
			Type type = localColumn.GetType();
			switch (ShouldSerializeSignature(localColumn))
			{
				case 512:
				case 513:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.AutoUpdateInterval,
								localColumn.ReadOnly
							}
						);
				case 514 :
				case 515 : 
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.AutoUpdateInterval,
								localColumn.ReadOnly
							}
						);
				case 516 :
				case 517 :
				case 518 :
				case 519 :
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.Width,
								localColumn.AutoUpdateInterval,
								localColumn.ReadOnly
							}
						);
				default : 
					info = type.GetConstructor
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
						info,
						new object[]
							{
								localColumn.ColumnName,
								localColumn.Title,
								localColumn.Width,
								localColumn.HorizontalAlignment,
								localColumn.HeaderTextAlignment,
								localColumn.VerticalAlignment,
								localColumn.BackColor,
								localColumn.Visible,
								localColumn.Header3DStyle,
								localColumn.Header3DSide,
								localColumn.MinRowHeight,
								localColumn.MaxRowHeight,
								localColumn.Font,
								localColumn.ForeColor,
								localColumn.AutoUpdateInterval,
								localColumn.ReadOnly
							}
						);
				
			}
		}
	}

	public class ActionColumnTypeConverter : TypeConverter
	{
		public ActionColumnTypeConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(context, destinationType);
		}

		protected virtual int ShouldSerializeSignature(ActionColumn column)
		{
			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(column);
			int serializeSignature = 0;
			if (propertyDescriptors["ControlText"].ShouldSerializeValue(column))
				serializeSignature |= 1;

			if (propertyDescriptors["Title"].ShouldSerializeValue(column))
				serializeSignature |= 2;
			
			if (propertyDescriptors["Width"].ShouldSerializeValue(column))
				serializeSignature |= 4;

			if (propertyDescriptors["HorizontalAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 8;

			if (propertyDescriptors["HeaderTextAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 16;

			if (propertyDescriptors["VerticalAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 32;

			if (propertyDescriptors["BackColor"].ShouldSerializeValue(column))
				serializeSignature |= 64;

			if (propertyDescriptors["Visible"].ShouldSerializeValue(column))
				serializeSignature |= 128;

			if (propertyDescriptors["Header3DStyle"].ShouldSerializeValue(column))
				serializeSignature |= 256;

			if (propertyDescriptors["Header3DSide"].ShouldSerializeValue(column))
				serializeSignature |= 512;

			if (propertyDescriptors["MinRowHeight"].ShouldSerializeValue(column))
				serializeSignature |= 1024;

			if (propertyDescriptors["MaxRowHeight"].ShouldSerializeValue(column))
				serializeSignature |= 2048;

			if (propertyDescriptors["Font"].ShouldSerializeValue(column))
				serializeSignature |= 4096;

			if (propertyDescriptors["ForeColor"].ShouldSerializeValue(column))
				serializeSignature |= 8192;

			if (propertyDescriptors["ControlClassName"].ShouldSerializeValue(column))
				serializeSignature |= 16384;
				
			return serializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(ActionColumn column)
		{
			ConstructorInfo info;
			Type type = column.GetType();
			switch (ShouldSerializeSignature(column))
			{
				case 0:
					info = type.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( info, new object[] {} );
				case 1:
					info = type.GetConstructor
						(
						new Type[] { typeof(string) }
						);
					return new InstanceDescriptor
						(
						info,
						new object[] { column.ControlText }
						);
				case 2:
				case 3:
					info = type.GetConstructor
						(
						new Type[] 
							{
								typeof(string),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						info,
						new object[]
							{
								column.ControlText,
								column.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								column.ControlText,
								column.Title,
								column.Width
							}
						);
				default:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								column.ControlText,
								column.Title,
								column.Width,
								column.HorizontalAlignment,
								column.HeaderTextAlignment,
								column.VerticalAlignment,
								column.BackColor,
								column.Visible,
								column.Header3DStyle,
								column.Header3DSide,
								column.MinRowHeight,
								column.MaxRowHeight,
								column.Font,
								column.ForeColor,
								column.ControlClassName
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			ActionColumn column = value as ActionColumn;
			if 
				(
				(destinationType == typeof(InstanceDescriptor)) &&
				(column != null)
				)
				return GetInstanceDescriptor(column);	
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	public class SequenceColumnTypeConverter : TypeConverter
	{
		public SequenceColumnTypeConverter() : base() {}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(InstanceDescriptor))
				return true;
			return base.CanConvertTo(context, destinationType);
		}

		protected virtual int ShouldSerializeSignature(SequenceColumn column)
		{
			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(column);
			int serializeSignature = 0;
			if (propertyDescriptors["Image"].ShouldSerializeValue(column))
				serializeSignature |= 1;

			if (propertyDescriptors["Title"].ShouldSerializeValue(column))
				serializeSignature |= 2;
			
			if (propertyDescriptors["Width"].ShouldSerializeValue(column))
				serializeSignature |= 4;

			if (propertyDescriptors["HorizontalAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 8;

			if (propertyDescriptors["HeaderTextAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 16;

			if (propertyDescriptors["VerticalAlignment"].ShouldSerializeValue(column))
				serializeSignature |= 32;

			if (propertyDescriptors["BackColor"].ShouldSerializeValue(column))
				serializeSignature |= 64;

			if (propertyDescriptors["Visible"].ShouldSerializeValue(column))
				serializeSignature |= 128;

			if (propertyDescriptors["Header3DStyle"].ShouldSerializeValue(column))
				serializeSignature |= 256;

			if (propertyDescriptors["Header3DSide"].ShouldSerializeValue(column))
				serializeSignature |= 512;

			if (propertyDescriptors["MinRowHeight"].ShouldSerializeValue(column))
				serializeSignature |= 1024;

			if (propertyDescriptors["MaxRowHeight"].ShouldSerializeValue(column))
				serializeSignature |= 2048;

			if (propertyDescriptors["Font"].ShouldSerializeValue(column))
				serializeSignature |= 4096;

			if (propertyDescriptors["ForeColor"].ShouldSerializeValue(column))
				serializeSignature |= 8192;

			return serializeSignature;
		}

		protected virtual InstanceDescriptor GetInstanceDescriptor(SequenceColumn column)
		{
			ConstructorInfo info;
			Type type = column.GetType();
			switch (ShouldSerializeSignature(column))
			{
				case 0:
					info = type.GetConstructor( new Type[] {} );
					return new InstanceDescriptor ( info, new object[] {} );
				case 1:
					info = type.GetConstructor
						(
						new Type[] { typeof(Image) }
						);
					return new InstanceDescriptor
						(
						info,
						new object[] { column.Image }
						);
				case 2:
				case 3:
					info = type.GetConstructor
						(
						new Type[] 
							{
								typeof(Image),
								typeof(string)
							}
						);
					return new InstanceDescriptor
						(
						info,
						new object[]
							{
								column.Image,
								column.Title
							}
						);
				case 4:
				case 5:
				case 6:
				case 7:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								column.Image,
								column.Title,
								column.Width
							}
						);
				default:
					info = type.GetConstructor
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
						info,
						new object[]
							{
								column.Image,
								column.Title,
								column.Width,
								column.HorizontalAlignment,
								column.HeaderTextAlignment,
								column.VerticalAlignment,
								column.BackColor,
								column.Visible,
								column.Header3DStyle,
								column.Header3DSide,
								column.MinRowHeight,
								column.MaxRowHeight,
								column.Font,
								column.ForeColor
							}
						);
			}
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			SequenceColumn column = value as SequenceColumn;
			if 
				(
				(destinationType == typeof(InstanceDescriptor)) &&
				(column != null)
				)
				return GetInstanceDescriptor(column);	
			return base.ConvertTo(context, culture, value, destinationType);
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
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		/// <param name="horizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="headerTextAlignment"> Header contents horizontal alignment. </param>
		/// <param name="verticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="backColor"> Background color for this column. </param>
		/// <param name="visible"> Column is visible. </param>
		/// <param name="header3DStyle"> Header paint style. </param>
		/// <param name="header3DSide"> Header paint side. </param>
		/// <param name="minRowHeight"> Minimum height of a row. </param>
		/// <param name="maxRowHeight"> Maximum height of a row. </param>
		/// <param name="font"> Column Font </param>
		/// <param name="foreColor"> Foreground color </param>
		public GridColumn
			(
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment headerTextAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor
			) : base()
		{
			InitializeColumn();
			_title = title;
			_width = width;
			_backColor = backColor;
			_visible = visible;
			_horizontalAlignment = horizontalAlignment;
			_headerTextAlignment = headerTextAlignment;
			_verticalAlignment = verticalAlignment;
			_header3DStyle = header3DStyle;
			_header3DSide = header3DSide;
			_minRowHeight = minRowHeight;
			_maxRowHeight = maxRowHeight;
			_font = font;
			_foreColor = foreColor;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title, and width. </summary>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		public GridColumn (string title, int width) : base()
		{
			InitializeColumn();
			_title = title;
			_width = width;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title. </summary>
		/// <param name="title"> Column title. </param>
		public GridColumn (string title) : base()
		{
			InitializeColumn();
			_title = title;
		}

		/// <summary> Used by the grid to create default columns. </summary>
		/// <param name="grid"> The DBGrid the collection belongs to. </param>
		protected internal GridColumn(DBGrid grid) : base()
		{
			InitializeColumn();
			_gridColumns = grid.Columns;
			_backColor = grid.BackColor;
			if (TitlePixelWidth > grid.DefaultColumnWidth)
				_width = TitlePixelWidth;
			else
				_width = grid.DefaultColumnWidth;
			_header3DStyle = grid.DefaultHeader3DStyle;
			_header3DSide = grid.DefaultHeader3DSide;
		}

		private void InitializeColumn()
		{
			_gridColumns = null;
			_title = string.Empty;
			_width = 100;
			_backColor = DBGrid.DefaultBackColor;
			_visible = true;
			_horizontalAlignment = WinForms.HorizontalAlignment.Left;
			_headerTextAlignment = WinForms.HorizontalAlignment.Left;
			_verticalAlignment = VerticalAlignment.Top;
			_header3DStyle = WinForms.Border3DStyle.RaisedInner;
			_header3DSide = WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Left | WinForms.Border3DSide.Right | WinForms.Border3DSide.Top;
			_minRowHeight = -1;
			_maxRowHeight = -1;
			_font = DBGrid.DefaultFont;
			_foreColor = DBGrid.DefaultForeColor;
		}

		public void Dispose()
		{
			this.Dispose(true);
			System.GC.SuppressFinalize(this);
		}

		private bool _isDisposed;
		protected bool IsDisposed { get { return _isDisposed; } }

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				_gridColumns = null;
				_isDisposed = true;
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

		protected void SelectRow(int rowIndex)
		{
			if (Grid != null)
				Grid.SelectRow(rowIndex);
		}

		protected Rectangle RowRectangleAt(int rowIndex)
		{
			return Grid != null ? Grid.Rows[rowIndex].ClientRectangle : Rectangle.Empty;
		}

		protected object RowValueAt(int rowIndex)
		{
			return Grid != null ? Grid.Rows[rowIndex].Row : null;
		}

		protected Rectangle PaddedRectangle(Rectangle rectangle)
		{
			return Grid != null ? Rectangle.Inflate(rectangle, -Grid.CellPadding.Width, -Grid.CellPadding.Height) :  rectangle;
		}

		protected virtual void SetDefaults()
		{
			if (BackColor == DBGrid.DefaultBackColor)
				BackColor = _gridColumns.Grid.BackColor;
			if (Font == DBGrid.DefaultFont)
				Font = _gridColumns.Grid.Font;
			if (ForeColor == DBGrid.DefaultForeColor)
				ForeColor = _gridColumns.Grid.ForeColor;
		}

		public event EventHandler OnInsertIntoGrid;
		protected internal virtual void AddingToGrid(GridColumns gridColumns)
		{
			_gridColumns = gridColumns;
			SetDefaults();
			if (OnInsertIntoGrid != null)
				OnInsertIntoGrid(this, EventArgs.Empty);
		}

		public event EventHandler OnRemoveFromGrid;
		protected internal virtual void RemovingFromGrid(GridColumns gridColumns)
		{
			_gridColumns = null;
			if (OnRemoveFromGrid != null)
				OnRemoveFromGrid(this, EventArgs.Empty);
		}

		private GridColumns _gridColumns;
		/// <summary>
		/// Reference to an DBGrid.GridColumns that include this column.
		/// </summary>
		protected GridColumns GridColumns { get { return _gridColumns; } }

		/// <summary> The DBGrid this column belongs to. </summary>
		public DBGrid Grid
		{
			get { return _gridColumns != null ? _gridColumns.Grid : null; }
		}

		/// <summary> The grids maximum pixel height of a row. </summary>
		protected int GridMaxRowHeight { get { return Grid != null ? Grid.CalcMaxRowHeight() : 0; } }

		private int _minRowHeight;
		/// <summary> Minimum height of a row in pixels. </summary>
		[Category("Appearance")]
		[DefaultValue(-1)]
		[Description("The minimum height of a row in pixels.")]
		public int MinRowHeight
		{
			get { return _minRowHeight; }
			set
			{
				if (_minRowHeight != value)
				{
					if ((value >= 0) && (_maxRowHeight > -1) && (value > _maxRowHeight))						
						throw new ControlsException(ControlsException.Codes.InvalidMinRowHeight);
					_minRowHeight = value;
					Changed();
				}
			}
		}

		private int _maxRowHeight;
		/// <summary> Maximum height of a row in pixels. </summary>
		[Category("Appearance")]
		[DefaultValue(-1)]
		[Description("The maximum height of a row in pixels.")]
		public int MaxRowHeight
		{
			get { return _maxRowHeight; }
			set
			{
				if (_maxRowHeight != value)
				{
					if ((value >= 0) && (_minRowHeight > -1) && (value < _minRowHeight))
						throw new ControlsException(ControlsException.Codes.InvalidMaxRowHeight);
					_maxRowHeight = value;
					Changed();
				}
			}
		}

		public int MeasureRowHeight(object value, Graphics graphics)
		{
			if ((Grid != null) && (Grid.IsHandleCreated))
			{
				if ((_maxRowHeight >= 0) && (_minRowHeight >= 0) && (_maxRowHeight == _minRowHeight))
					return _minRowHeight > GridMaxRowHeight ? GridMaxRowHeight : _minRowHeight;
				if (value == null)
					return Grid.MinRowHeight;
				int naturalHeight = NaturalHeight(value, graphics);
				if ((_maxRowHeight < 0) && (_minRowHeight < 0))
					return naturalHeight > GridMaxRowHeight ? GridMaxRowHeight : naturalHeight;
				if ((_minRowHeight > -1) && naturalHeight < _minRowHeight)
					return _minRowHeight > GridMaxRowHeight ? GridMaxRowHeight : _minRowHeight;
				if ((_maxRowHeight > -1) && (naturalHeight > _maxRowHeight))
					return _maxRowHeight > GridMaxRowHeight ? GridMaxRowHeight : _maxRowHeight;
				return naturalHeight > GridMaxRowHeight ? GridMaxRowHeight : naturalHeight;
			}
			else
				return DBGrid.DefaultRowHeight;
		}		

		private bool ShouldSerializeFont() { return _font != DBGrid.DefaultFont; }		
		private System.Drawing.Font _font;
		[Category("Appearance")]
		public System.Drawing.Font Font
		{
			get { return _font; }
			set
			{
				if (_font != value)
				{
					_font = value;
					Changed();
				}
			}
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="value"> The value to measure. </param>
		/// <param name="graphics"> The grids graphics object. </param>
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
		public abstract int NaturalHeight(object value, Graphics graphics);

		protected internal virtual void RowsChanged() {}
		protected internal virtual void RowChanged(object value, Rectangle rowRectangle, int rowIndex) {}

		/// <summary> Paints the column title in the header cell. </summary>
		/// <remarks> Override this method to custom paint the header cell. </remarks>
		protected internal virtual void PaintHeaderCell(Graphics graphics, Rectangle cellRect, StringFormat format, Color fixedColor)
		{
			using (SolidBrush brush = new SolidBrush(fixedColor))
			{
				graphics.FillRectangle(brush, cellRect);
				WinForms.ControlPaint.DrawBorder3D(graphics, cellRect, _header3DStyle, _header3DSide);
				brush.Color = Grid.ForeColor;
				Rectangle cellWorkArea = Rectangle.Inflate(cellRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height);
				graphics.DrawString
					(
					Title,
					Grid.Font,
					brush,
					cellWorkArea,
					format
					);	
			}
		}

		protected virtual void PaintLines(Graphics graphics, Rectangle cellRect, Pen linePen)
		{
			if ((Grid.GridLines == GridLines.Horizontal) || (Grid.GridLines == GridLines.Both))
			{
				graphics.DrawLine(linePen, cellRect.X, cellRect.Y + cellRect.Height - 1, cellRect.X + cellRect.Width, cellRect.Y + cellRect.Height - 1);
				if ((Grid.GridLines == GridLines.Horizontal) && this.IsLastHeaderColumn)
					graphics.DrawLine(linePen, cellRect.X + cellRect.Width - 1, cellRect.Y, cellRect.X + cellRect.Width - 1, cellRect.Y + cellRect.Height);
			}
			if ((Grid.GridLines == GridLines.Vertical) || (Grid.GridLines == GridLines.Both))
			{
				graphics.DrawLine(linePen, cellRect.X + cellRect.Width - 1, cellRect.Y, cellRect.X + cellRect.Width - 1, cellRect.Y + cellRect.Height);
				if ((Grid.GridLines == GridLines.Vertical) && Grid.GetRowAt(cellRect.Y + 2) == Grid.Link.LastOffset)
					graphics.DrawLine(linePen, cellRect.X, cellRect.Y + cellRect.Height - 1, cellRect.X + cellRect.Width, cellRect.Y + cellRect.Height - 1);
			}
			if (Grid.GridLines != GridLines.None)
			{
				if (this.IsFirstHeaderColumn)
					graphics.DrawLine(linePen, cellRect.X, cellRect.Y - 1, cellRect.X, cellRect.Y + cellRect.Height - 1);
			}
		}

		protected virtual Rectangle MeasureVerticalRect
			(
			Rectangle rect,
			VerticalAlignment alignment,
			object value,
			Graphics graphics
			)
		{
			switch (alignment)
			{
				case VerticalAlignment.Bottom :
				{
					int rowHeight = MeasureRowHeight(value, graphics);
					if (rowHeight < rect.Height)
						rect.Y = rect.Bottom - rowHeight;
					break;
				}
				case VerticalAlignment.Middle :
				{
					int rowHeight = MeasureRowHeight(value, graphics);
					if (rowHeight < rect.Height)
						rect.Y += ((rect.Height + rowHeight) / 2) - rowHeight;
					break;
				}
			}
			return rect;
		}

		/// <summary> Override PaintCell to paint a value in a cell. </summary>
		/// <param name="value"> The value to paint. </param>
		/// <param name="isSelected"> True if the cell is currently selected. </param>
		/// <param name="rowIndex"> Index of this row in the grids row buffer. </param>
		/// <param name="graphics"> The grids graphics object. </param>
		/// <param name="cellRect"> Client area allocated to this cell. </param>
		/// <param name="paddedRect"> Client area allocated to the cell after padding the CellRect. </param>
		/// <param name="verticalMeasuredRect"> Client area allocated based on the vertical alignment of the value in the cell. </param>
		/// <remarks> Override PaintCell to custom paint a cell. </remarks>
		protected abstract void PaintCell
			(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect
			);

		protected internal virtual void BeforePaint(object sender, WinForms.PaintEventArgs args) {}
		protected internal virtual void AfterPaint(object sender, WinForms.PaintEventArgs args) {}

		protected internal void InternalPaintCell
			(
			Graphics graphics, 
			Rectangle cellRect,
			object value,
			bool isSelected,
			Pen linePen,
			int rowIndex
			)
		{
			Rectangle paddedRect = Rectangle.Inflate(cellRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height);
			Rectangle verticalMeasuredRect = paddedRect;
			if (value != null)
				verticalMeasuredRect = MeasureVerticalRect(paddedRect, _verticalAlignment, value, graphics);
			PaintCell(value, isSelected, rowIndex, graphics, cellRect, paddedRect, verticalMeasuredRect);
			PaintLines(graphics, cellRect, linePen);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnMouseMove;
		protected internal virtual void MouseMove(Point point, int rowIndex)
		{
			if (OnMouseMove != null)
				OnMouseMove(this, new WinForms.MouseEventArgs(WinForms.MouseButtons.None, 0, point.X, point.Y, 0));
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnMouseDown;
		protected internal virtual void MouseDown(WinForms.MouseEventArgs args, int rowIndex)
		{
			if (OnMouseDown != null)
				OnMouseDown(this, args);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnMouseUp;
		protected internal virtual void MouseUp(WinForms.MouseEventArgs args, int rowIndex)
		{
			if (OnMouseUp != null)
				OnMouseUp(this, args);
		}

		[Category("Mouse")]
		public event EventHandler OnClick;
		protected internal virtual void MouseClick(EventArgs args, int rowIndex)
		{
			if (OnClick != null)
				OnClick(this, args);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnHeaderMouseDown;
		protected internal virtual void HeaderMouseDown(WinForms.MouseEventArgs args)
		{
			if (OnHeaderMouseDown != null)
				OnHeaderMouseDown(this, args);
		}

		[Category("Mouse")]
		public event WinForms.MouseEventHandler OnHeaderMouseUp;
		protected internal virtual void HeaderMouseUp(WinForms.MouseEventArgs args)
		{
			if (OnHeaderMouseUp != null)
				OnHeaderMouseUp(this, args);
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

		private WinForms.Border3DStyle _header3DStyle;
		
		[Category("Appearance")]
		[Description("3D style of the column header.")]
		[DefaultValue(WinForms.Border3DStyle.RaisedInner)]
		public WinForms.Border3DStyle Header3DStyle
		{
			get { return _header3DStyle; }
			set
			{
				if (_header3DStyle != value)
				{
					_header3DStyle = value;
					Changed();
				}
			}
		}

		private WinForms.Border3DSide _header3DSide;
		[Category("Appearance")]
		[DefaultValue(WinForms.Border3DSide.Bottom | WinForms.Border3DSide.Left | WinForms.Border3DSide.Right | WinForms.Border3DSide.Top)]
		[Description("3D sides of the column header.")]
		public WinForms.Border3DSide Header3DSide
		{
			get { return _header3DSide; }
			set
			{
				if (_header3DSide != value)
				{
					_header3DSide = value;
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
		
		private int _width;
		[DefaultValue(100)]
		[Category("Appearance")]
		[Description("Pixel width of the column.")]
		public int Width
		{
			get { return _width; }
			set
			{
				if ((_width != value) && (value >= 0))
				{
					BeforeWidthChange();
					_width = value;
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
				using (Graphics graphics = Grid.CreateGraphics())
				{
					return (Size.Round(graphics.MeasureString(_title, Grid.Font))).Width;
				}
			}
		}

		protected virtual bool ShouldSerializeTitle() { return Title != String.Empty; }
		private string _title;
		[Category("Appearance")]
		[Description("Text in the column header.")]
		public virtual string Title
		{
			get { return _title; }
			set
			{
				if (_title != value)
				{
					_title = value == null ? String.Empty : value;
					Changed();
				}
			}
		}

		private WinForms.HorizontalAlignment _horizontalAlignment;
		[Category("Appearance")]
		[DefaultValue(WinForms.HorizontalAlignment.Left)]
		[Description("Horizontal alignment of the text.")]
		public virtual WinForms.HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set
			{
				if (_horizontalAlignment != value)
				{
					_horizontalAlignment = value;
					Changed();
				}
			}
		}

		private VerticalAlignment _verticalAlignment;
		[Category("Appearance")]
		[DefaultValue(VerticalAlignment.Top)]
		[Description("The vertical alignment of the contents of a cell.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					Changed();
				}
			}
		}

		private WinForms.HorizontalAlignment _headerTextAlignment;
		[Category("Appearance")]
		[DefaultValue(WinForms.HorizontalAlignment.Left)]
		[Description("Horizontal alignment of header text.")]
		public WinForms.HorizontalAlignment HeaderTextAlignment
		{
			get { return _headerTextAlignment; }
			set
			{
				if (_headerTextAlignment != value)
				{
					_headerTextAlignment = value;
					Changed();
				}
			}
		}

		private bool ShouldSerializeForeColor() { return _foreColor != DBGrid.DefaultForeColor; }
		private Color _foreColor;
		[Category("Appearance")]
		[Description("Column foreground color.")]
		public Color ForeColor
		{
			get { return _foreColor; } 
			set
			{
				if (_foreColor != value)
				{
					_foreColor = value;
					Changed();
				}
			}
		}

		private bool ShouldSerializeBackColor() { return _backColor != DBGrid.DefaultBackColor; }
		private Color _backColor;
		[Category("Appearance")]
		[Description("Column background color.")]
		public Color BackColor
		{
			get { return _backColor; }
			set
			{
				if (_backColor != value)
				{
					_backColor = value;
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

		private bool _visible;
		[DefaultValue(true)]
		[Category("Appearance")]
		public virtual bool Visible
		{
			get { return _visible; }
			set
			{
				if (_visible != value)
				{
					_visible = value;
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
					foreach (HeaderColumn header in Grid.Header)
						if (header.Column == this)
							return new Rectangle(header.Offset, Grid.HeaderHeight, header.Width, Grid.Height);
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

		protected Rectangle CellRectangleAt(int rowIndex)
		{
			if (rowIndex >= 0)
			{
				Rectangle columnRect = ClientRectangle;
				GridRow row = Grid.Rows[rowIndex];
				return new Rectangle(columnRect.X, row.ClientRectangle.Y, columnRect.Width, row.ClientRectangle.Height);
			}
			else
				throw new ControlsException(ControlsException.Codes.RowIndexOutOfRange);
		}

		//If we had multiple inheritance these would not be needed.
		protected internal virtual void AddingControl(WinForms.Control control, int index) {}

		protected internal virtual void RemovingControl(WinForms.Control control, int index) {}

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
		public ControlsList(GridColumn column): base(true, false)
		{
			_column = column;
		}

		private GridColumn _column;

		protected override void Adding(object value, int index)
		{
			base.Adding(value, index);
			_column.AddingControl((WinForms.Control)value, index);
		}

		protected override void Removing(object value, int index)
		{
			_column.RemovingControl((WinForms.Control)value, index);
			base.Removing(value, index);
		}
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.ActionColumnTypeConverter))]
	public class ActionColumn : GridColumn
	{
		public const string DefaultControlClassName = "Alphora.Dataphor.DAE.Client.Controls.SpeedButton,Alphora.Dataphor.DAE.Client.Controls";
		/// <summary> Initializes a new instance of a non data-bound ActionColumn. </summary>
		public ActionColumn() : base()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of an ActionColumn. </summary>
		/// <param name="controlText"> Text for the control. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		/// <param name="horizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="titleAlignment"> Header contents horizontal alignment. </param>
		/// <param name="verticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="backColor"> Background color for this column. </param>
		/// <param name="visible"> Column is visible. </param>
		/// <param name="header3DStyle"> Header paint style. </param>
		/// <param name="header3DSide"> Header paint side. </param>
		/// <param name="minRowHeight"> Minimum height of a row. </param>
		/// <param name="maxRowHeight"> Maximum height of a row. </param>
		/// <param name="font"> Column Font </param>
		/// <param name="foreColor"> Foreground color </param>
		public ActionColumn
			(
			string controlText,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment titleAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor,
			string controlClassName
			) : base(title, width, horizontalAlignment, titleAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
		{
			InitializeColumn();
			_controlText = controlText;
			_controlClassName = controlClassName;
		}

		/// <summary> Initializes a new instance of a ActionColumn given a title, and width. </summary>
		/// <param name="controlText"> Text for the control. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		public ActionColumn (string controlText, string title, int width) : base(title, width)
		{
			InitializeColumn();
			_controlText = controlText;
		}

		/// <summary> Initializes a new instance of a ActionColumn given a title. </summary>
		/// <param name="controlText"> Text for the control. </param>
		public ActionColumn (string controlText) : base()
		{
			InitializeColumn();
			_controlText = controlText;
		}

		/// <summary> Initializes a new instance of a ActionColumn given a button text and title. </summary>
		/// <param name="controlText"> Text for the control. </param>
		/// <param name="title"> Column title. </param>
		public ActionColumn (string controlText, string title) : base(title)
		{
			InitializeColumn();
			_controlText = controlText;
		}

		private void InitializeColumn()
		{
			_controlClassName = DefaultControlClassName;
			_controlText = String.Empty;
		}

		protected override void Dispose(bool disposing)
		{
			if (_controls != null)
			{
				_controls.Clear();
				_controls = null;
			}
			base.Dispose(disposing);
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="value"> The value to measure. </param>
		/// <param name="graphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object value, Graphics graphics)
		{
			if (Grid != null)
			{
				SizeF size;
				if ((value != null) && (value is string))
					size = graphics.MeasureString((string)value, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				else
					size = graphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				if ((int)size.Height > Grid.MinRowHeight)
					return (int)size.Height + (Grid.Font.Height / 2);
				return (int)size.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		private ControlsList _controls;
		protected ControlsList Controls
		{
			get
			{
				if (_controls == null)
					_controls = new ControlsList(this);
				return _controls;
			}
		}

		private string _controlText;
		[Category("Appearance")]
		[Description("Text value for the control.")]
		public string ControlText
		{
			get { return _controlText; }
			set
			{
				if (_controlText != value)
				{
					_controlText = value;
					Changed();
				}
			}
		}

		private bool _enabled = true;
		[DefaultValue(true)]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					Changed();
				}
			}
		}

		public event EventHandler OnExecuteAction;

		protected virtual void ExecuteAction(object sender, EventArgs args)
		{
			SelectRow(Controls.IndexOf(sender));
			if (OnExecuteAction != null)
				OnExecuteAction(this, args);
		}

		private void ControlEnter(object sender, EventArgs args)
		{
			SelectRow(Controls.IndexOf(sender));
		}

		private string _controlClassName;
		[Category("Design")]
		[DefaultValue(DefaultControlClassName)]
		public string ControlClassName
		{
			get	{ return _controlClassName; }
			set
			{
				if (_controlClassName != value)
				{
					RecreateControls(value);
					_controlClassName = value;
				}
			}
		}

		private void RecreateControls(string className)
		{
			int controlCount = Controls.Count;
			Rectangle[] rects = new Rectangle[controlCount];
			WinForms.Control control;
			for (int i = controlCount - 1; i >= 0; i--)
			{
				control = (WinForms.Control)Controls[i];
				rects[i] = control.Bounds;
				Controls.RemoveAt(i);
			}
			for (int j = 0; j < controlCount; j++)
			{
				AddControl(rects[j]);
				((WinForms.Control)Controls[j]).Bounds = rects[j];
			}
		}

		/// <summary> Creates an instance given a class name. </summary>
		/// <param name="className"> The name of the class to instantiate</param>
		private object CreateObject(string className)
		{
			return Activator.CreateInstance(Type.GetType(className, true, true));
		}

		private WinForms.Control CreateControl(string className)
		{
			return (WinForms.Control)CreateObject(className);
		}

		protected virtual void SetControlProperties(WinForms.Control control)
		{
			control.Parent = Grid;			
			control.TabStop = false;
			if (control is WinForms.Button)
			{
				if (control.BackColor.A < 255)
					((WinForms.Button)control).FlatStyle = WinForms.FlatStyle.Popup;
				else
					((WinForms.Button)control).FlatStyle = WinForms.FlatStyle.System;
			}
			control.ForeColor = ForeColor;
			control.BackColor = BackColor;
			control.Font = Font;
			control.Text = _controlText;
			control.Enabled = _enabled;
		}

		private void AddControl(Rectangle clientRect)
		{
			WinForms.Control control = CreateControl(_controlClassName);
			control.Bounds = clientRect;
			control.Enabled = _enabled;
			Controls.Add(control);
		}

		private void UpdateControl(Rectangle clientRect, int index)
		{
			WinForms.Control control = (WinForms.Control)Controls[index];
			control.Location = clientRect.Location;
			control.Size = clientRect.Size;
			SetControlProperties(control);
		}

		protected internal override void AddingControl(WinForms.Control control, int index)
		{
			base.AddingControl(control, index);
			control.Enter += new EventHandler(ControlEnter);
			control.Click += new EventHandler(ExecuteAction);
			SetControlProperties(control);
		}

		protected internal override void RemovingControl(WinForms.Control control, int index)
		{
			if (control.ContainsFocus)
				if ((Grid != null) && (Grid.CanFocus))
					Grid.Focus();
			control.Enter -= new EventHandler(ControlEnter);
			control.Click -= new EventHandler(ExecuteAction);
			control.Parent = null;
			base.RemovingControl(control, index);
		}

		protected internal override void RowChanged(object value, Rectangle rowRectangle, int rowIndex)
		{
			base.RowChanged(value, rowRectangle, rowIndex);
			Rectangle columnRect = ClientRectangle;
			if (!columnRect.IsEmpty)
			{
				Rectangle controlRect = new Rectangle(columnRect.X, rowRectangle.Y, columnRect.Width - Grid.LineWidth, rowRectangle.Height - Grid.LineWidth);
				if (rowIndex >= Controls.Count)
					AddControl(controlRect);
				else
					UpdateControl(controlRect, rowIndex);
			}
			else
				Controls.Clear();
		}

		protected internal override void RowsChanged()
		{
			base.RowsChanged();
			if (_controls != null)
			{
				if ((Grid != null) && (Grid.Link != null) && (Grid.Rows.Count > 0))
					while (_controls.Count > Grid.Rows.Count)
						_controls.Remove(_controls[_controls.Count - 1]);
				else
					_controls.Clear();
			}
		}

		protected override void PaintCell
			(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect
			)
		{
			using (SolidBrush backBrush = new SolidBrush(value != null ? BackColor : Grid.NoValueBackColor))
				graphics.FillRectangle(backBrush, cellRect);
		}
		
	}

	public class RowDragObject : MarshalByRefObject , IDisposableNotify
	{
		public RowDragObject(GridColumn column, DAEData.Row startRow, Point startDragPoint, int targetIndex, Size centerOffset, Image image) : base()
		{
			_column = column;
			_startDragPoint = startDragPoint;
			_startRow = startRow;
			_image = image;
			_cursorCenterOffset = centerOffset;
			DropTargetIndex = targetIndex;
		}
        
		protected virtual void Dispose(bool disposing)
		{
			if (_startRow != null)
				_startRow.Dispose();
			if (_image != null)
				_image.Dispose();
			_startRow = null;
			_image = null;
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

		private GridColumn _column;
		public GridColumn Column { get { return _column; } }

		private Point _startDragPoint;
		public Point StartDragPoint
		{
			get { return _startDragPoint; }
		}

		private DAEData.Row _startRow;
		public DAEData.Row StartRow { get { return _startRow; } }

		private Size _cursorCenterOffset;
		public Size CursorCenterOffset { get { return _cursorCenterOffset; } }

		private int _dropTargetIndex;
		public int DropTargetIndex
		{
			get { return _dropTargetIndex; }
			set { _dropTargetIndex = value; }
		}

		private bool _targetAbove;
		public bool TargetAbove
		{
			get { return _targetAbove; }
			set { _targetAbove = value; }
		}

		private Rectangle _highlightRect;
		public Rectangle HighlightRect
		{
			get { return _highlightRect; }
			set { _highlightRect = value; }
		}

		private Point _imageLocation;
		public Point ImageLocation
		{
			get { return _imageLocation; }
			set { _imageLocation = value; }
		}

		private Image _image;
		public Image Image { get { return _image; } }
	}

	public delegate void SequenceChangeEventHandler(object ASender, DAEData.Row AFromRow, DAEData.Row AToRow, bool ABeforeRow, EventArgs AArgs);
	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.SequenceColumnTypeConverter))]
	public class SequenceColumn : GridColumn
	{
		public const int AutoScrollHeight = 24;
		public const int StartScrollInterval = 400;
		private Size _beforeDragSize = WinForms.SystemInformation.DragSize;
		/// <summary> Initializes a new instance of a non data-bound ButtonColumn. </summary>
		public SequenceColumn() : base()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a SequenceColumn. </summary>
		/// <param name="image"> Cell image </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		/// <param name="horizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="titleAlignment"> Header contents horizontal alignment. </param>
		/// <param name="verticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="backColor"> Background color for this column. </param>
		/// <param name="visible"> Column is visible. </param>
		/// <param name="header3DStyle"> Header paint style. </param>
		/// <param name="header3DSide"> Header paint side. </param>
		/// <param name="minRowHeight"> Minimum height of a row. </param>
		/// <param name="maxRowHeight"> Maximum height of a row. </param>
		/// <param name="font"> Column Font </param>
		/// <param name="foreColor"> Foreground color </param>
		public SequenceColumn
			(
			Image image,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment titleAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor
			) : base(title, width, horizontalAlignment, titleAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
		{
			InitializeColumn();
			_image = image;
		}

		/// <summary> Initializes a new instance of a SequenceColumn given a title, and width. </summary>
		/// <param name="image"> Cell image. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		public SequenceColumn (Image image, string title, int width) : base(title, width)
		{
			InitializeColumn();
			_image = image;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title. </summary>
		/// <param name="image"> Cell image. </param>
		public SequenceColumn (Image image) : base()
		{
			InitializeColumn();
			_image = image;
		}

		/// <summary> Initializes a new instance of a GridColumn given a button text and title. </summary>
		/// <param name="image"> Cell image. </param>
		/// <param name="title"> Column title. </param>
		public SequenceColumn (Image image, string title) : base(title)
		{
			InitializeColumn();
			_image = image;
		}

		private void InitializeColumn()
		{
			_image = null;
			HorizontalAlignment = WinForms.HorizontalAlignment.Center;
		}

		protected override void Dispose(bool disposing)
		{
			if (_image != null)
			{
				_image.Dispose();
				_image = null;
			}
			base.Dispose(disposing);
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="value"> The value to measure. </param>
		/// <param name="graphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object value, Graphics graphics)
		{
			if (Grid != null)
			{
				SizeF size;
				if ((value != null) && (value is string))
					size = graphics.MeasureString((string)value, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				else
					size = graphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				if ((int)size.Height > Grid.MinRowHeight)
					return (int)size.Height + (Grid.Font.Height / 2);
				return (int)size.Height;			
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

		private System.Drawing.Image _image;
		[DefaultValue(null)]
		[Category("Appearance")]
		public Image Image
		{
			get { return _image; }
			set
			{
				if (_image != value)
				{
					_image = value;
					Changed();
				}
			}
		}

		private RowDragObject _rowDragObject;

		private void DrawDragImage(Graphics graphics, RowDragObject dragObject)
		{
			using (SolidBrush betweenRowBrush = new SolidBrush(SystemColors.Highlight))
				graphics.FillRectangle(betweenRowBrush, dragObject.HighlightRect);
			graphics.DrawImage(dragObject.Image, dragObject.ImageLocation);
		}

		protected internal override void AfterPaint(object sender, WinForms.PaintEventArgs args)
		{
			base.AfterPaint(sender, args);
			if (_draggingRow && (_rowDragObject != null) && (_rowDragObject.Image != null))
				DrawDragImage(args.Graphics, _rowDragObject);
		}

		protected override void PaintCell
			(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect 
			)
		{
			if (_image != null)
			{
				Rectangle imageRect = new Rectangle(0 , 0, _image.Width, _image.Height);
				imageRect = ImageAspect.ImageAspectRectangle(imageRect, paddedRect);

				switch (HorizontalAlignment)
				{
					case WinForms.HorizontalAlignment.Center :
						imageRect.Offset
							(
							(paddedRect.Width - imageRect.Width) / 2,
							(paddedRect.Height - imageRect.Height) / 2
							);
						break;
					case WinForms.HorizontalAlignment.Right :
						imageRect.Offset
							(
							(paddedRect.Width - imageRect.Width),
							(paddedRect.Height - imageRect.Height)
							);
						break;
				}
				
				imageRect.X += paddedRect.X;
				imageRect.Y += paddedRect.Y;
				graphics.DrawImage(_image, imageRect);
			}
		}

		protected virtual Image GetDragImage(int rowIndex, bool paintBorder)
		{
			Rectangle rowRect = RowRectangleAt(rowIndex);
			Bitmap rowBitmap;
			using (Graphics gridGraphics = Grid.CreateGraphics())
			{
				using (Bitmap bitmap = new Bitmap(rowRect.Width, rowRect.Bottom, gridGraphics))
				{
					using (Graphics graphics = Graphics.FromImage(bitmap))
						Grid.DrawCells(graphics, rowRect, false);

					rowBitmap = new Bitmap(rowRect.Width + 1, rowRect.Height + 1, gridGraphics);
					using (Graphics rowGraphics = Graphics.FromImage(rowBitmap))
					{
						Rectangle imageRect = new Rectangle(Point.Empty, rowRect.Size);
						rowGraphics.DrawImage(bitmap, imageRect, rowRect, GraphicsUnit.Pixel);
						if (paintBorder)
						{
							using (Pen pen = new Pen(ForeColor, 1))
								rowGraphics.DrawRectangle(pen, imageRect);
						}
					}
				}
			}
			return rowBitmap;
		}

		public SequenceChangeEventHandler OnSequenceChange;
		protected virtual void SequenceChanged(DAEData.Row fromRow, DAEData.Row toRow, bool beforeRow)
		{
			_bookMark = null;
			if (OnSequenceChange != null)
				OnSequenceChange(this, fromRow, toRow, beforeRow, EventArgs.Empty);
		}

		private DAEData.Row CreateSequenceRow(int rowIndex)
		{
			TableDataSet dataSet = Grid.Link.DataSet as TableDataSet;
			if (dataSet != null)
			{
				string columnName;
				DAESchema.RowType rowType = new DAESchema.RowType();	
				foreach (DAESchema.OrderColumn column in dataSet.Order.Columns)
				{
					columnName = column.Column.Name;
					rowType.Columns.Add(new DAESchema.Column(columnName, dataSet.TableVar.Columns[columnName].DataType));
				}

				DAEData.Row viewRow = (DAEData.Row)RowValueAt(rowIndex);
				DAEData.Row row = new DAEData.Row(dataSet.Process.ValueManager, rowType);
				foreach (DAESchema.OrderColumn column in dataSet.Order.Columns)
				{
					columnName = column.Column.Name;
					row[columnName] = viewRow[columnName];
				}
				return row;
			}
			return null;
		}

		private bool _draggingRow;
		protected void DoRowDragDrop()
		{
			TableDataSet dataSet = Grid.Link.DataSet as TableDataSet;
			if ((_rowDragObject != null) && (dataSet != null) && (dataSet.Order != null))
			{
				bool saveAllowDrop = Grid.AllowDrop;
				_draggingRow = true;
				try
				{
					Grid.AllowDrop = true;
					WinForms.DataObject dataObject = new WinForms.DataObject();
					dataObject.SetData(_rowDragObject);
					Grid.DoDragDrop(dataObject, WinForms.DragDropEffects.Move | WinForms.DragDropEffects.Scroll);
				}
				finally
				{
					_draggingRow = false;
					DisposeDragDropTimer();
					Grid.AllowDrop = saveAllowDrop;
					if (_bookMark != null)
					{
						Grid.Link.DataSet.FindNearest(_bookMark);
						_bookMark = null;
					}
					DestroyDragObject();
					Grid.Invalidate(Grid.Region); //Hide the drag image.
				}
			}
		}

		private void DestroyDragObject()
		{
			if (_rowDragObject != null)
			{
				if (Grid != null)
				{
					Grid.DragEnter -= new WinForms.DragEventHandler(DragEnter);
					Grid.DragOver -= new WinForms.DragEventHandler(DragOver);
					Grid.DragDrop -= new WinForms.DragEventHandler(DragDrop);
				}
				_rowDragObject.Dispose();
				_rowDragObject = null;
			}
		}

		private void CreateDragObject(WinForms.MouseEventArgs args, int rowIndex)
		{
			if (rowIndex >= 0)
			{
				TableDataSet dataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (dataSet != null) && (dataSet.Order != null))
				{
					Rectangle rowRect = RowRectangleAt(rowIndex);
					_rowDragObject = new RowDragObject(this, CreateSequenceRow(rowIndex), new Point(args.X, args.Y), -1, new Size(args.X - rowRect.X, 4), GetDragImage(rowIndex, true));
					Grid.DragDrop += new WinForms.DragEventHandler(DragDrop);
					Grid.DragEnter += new WinForms.DragEventHandler(DragEnter);
					Grid.DragOver += new WinForms.DragEventHandler(DragOver);
					WinForms.DataObject dataObject = new WinForms.DataObject(_rowDragObject);
					UpdateDragObject(new Point(args.X, args.Y), dataObject);
				}
			}
		}

		protected internal override void MouseDown(WinForms.MouseEventArgs args, int rowIndex)
		{
			base.MouseDown(args, rowIndex);
			CreateDragObject(args, rowIndex);
		}

		protected internal override void MouseMove(Point point, int rowIndex)
		{
			base.MouseMove(point, rowIndex);
			if
				(
				(_rowDragObject != null) &&
				!_draggingRow &&
				((Math.Abs(_rowDragObject.StartDragPoint.X - point.X) > _beforeDragSize.Width) || ((Math.Abs(_rowDragObject.StartDragPoint.Y - point.Y) > _beforeDragSize.Height)))
				)
				DoRowDragDrop();
		}

		protected internal override void MouseUp(WinForms.MouseEventArgs args, int rowIndex)
		{
			base.MouseUp(args, rowIndex);
			DestroyDragObject();
		}

		protected virtual void DragEnter(object sender, WinForms.DragEventArgs args)
		{
			if (_draggingRow)
			{
				args.Effect = WinForms.DragDropEffects.Move;
				Point point = ((WinForms.Control)sender).PointToClient(new Point(args.X, args.Y));
				UpdateDragTimer(point);
				UpdateDragObject(point, args.Data);
			}
		}

		protected virtual void DragOver(object sender, WinForms.DragEventArgs args)
		{
			if (_draggingRow)
			{
				Point point = ((WinForms.Control)sender).PointToClient(new Point(args.X, args.Y));
				args.Effect = WinForms.DragDropEffects.Move;
				UpdateDragTimer(point);
				UpdateDragObject(point, args.Data);
			}
		}

		protected virtual void DragDrop(object sender, WinForms.DragEventArgs args)
		{
			DisposeDragDropTimer();
			WinForms.DataObject draggedData = new WinForms.DataObject(args.Data);
			if (draggedData.GetDataPresent(typeof(RowDragObject)))
			{
				Point point = ((WinForms.Control)sender).PointToClient(new Point(args.X, args.Y));
				UpdateDragObject(point, args.Data);

				RowDragObject dragObject = (RowDragObject)draggedData.GetData(typeof(RowDragObject));
				if ((dragObject != null) && (dragObject.DropTargetIndex >= 0))
					SequenceChanged(_rowDragObject.StartRow, CreateSequenceRow(dragObject.DropTargetIndex), dragObject.TargetAbove);
			}
		}

		private int GetDropTargetIndex(Point location)
		{
			if (location.Y > Grid.HeaderHeight)
			{
				for (int i = 0; i < Grid.Rows.Count; i++)
				{
					Rectangle rowRect = RowRectangleAt(i);
					if ((location.Y > rowRect.Y) && (location.Y <= rowRect.Bottom))
						return i;
				}
			}
			return -1;
		}

		private void UpdateDragObject(Point location, WinForms.IDataObject data)
		{

			WinForms.DataObject draggedData = new WinForms.DataObject(data);
			if (draggedData.GetDataPresent(typeof(RowDragObject)))
			{
				RowDragObject dragObject = (RowDragObject)draggedData.GetData(typeof(RowDragObject));
				if (dragObject != null)
				{
					Rectangle invalidateRect = dragObject.HighlightRect;					
					int target = GetDropTargetIndex(location);
					bool above = false;
					Rectangle targetRect = Rectangle.Empty;
					if (target < 0)
					{
						if (location.Y <= Grid.HeaderHeight)
						{
							if (Grid.Rows.Count >= 0)
							{
								target = 0;
								above = true;
								targetRect = RowRectangleAt(target);
							}
						}
						else
						{
							if (Grid.Rows.Count >= 0)
							{
								target = Grid.Rows.Count - 1;
								above = false;
								targetRect = RowRectangleAt(target);
							}
							
						}
					}
					else
					{
						targetRect = RowRectangleAt(target);
						above = location.Y < (targetRect.Top + (targetRect.Height / 2));
					}

					dragObject.TargetAbove = above;
					dragObject.DropTargetIndex = target;
					if (target >= 0)
					{
						if (above)
							dragObject.HighlightRect = new Rectangle(targetRect.X, targetRect.Y - 2, targetRect.Width, 4);
						else
							dragObject.HighlightRect = new Rectangle(targetRect.X, targetRect.Bottom - 2, targetRect.Width, 4);
						invalidateRect = Rectangle.Union(invalidateRect, dragObject.HighlightRect); 
					}

					if (dragObject.Image != null)
					{
						Rectangle oldImageLocRect = new Rectangle(dragObject.ImageLocation, dragObject.Image.Size);
						Rectangle newImageLocRect = new Rectangle(location.X - dragObject.CursorCenterOffset.Width, location.Y + dragObject.CursorCenterOffset.Height, dragObject.Image.Width, dragObject.Image.Height); 
						if (!newImageLocRect.Location.Equals(dragObject.ImageLocation))
						{
							dragObject.ImageLocation = newImageLocRect.Location;
							invalidateRect = Rectangle.Union(invalidateRect, Rectangle.Union(oldImageLocRect, newImageLocRect));
						}
					}
					if (!invalidateRect.IsEmpty)
						Grid.InternalInvalidate(invalidateRect, true);
				}
			}

		}

		private DragDropTimer _scrollTimer = null;

		protected void CreateDragDropTimer(ScrollDirection direction, bool enable)
		{
			DisposeDragDropTimer();
			ScrollTimerInterval = StartScrollInterval;
			_scrollTimer = new DragDropTimer(ScrollTimerInterval);
			_scrollTimer.Tick += new EventHandler(ScrollTimerTick);
			_scrollTimer.ScrollDirection = direction;
			_scrollTimer.Enabled = enable;

		}

		protected void DisposeDragDropTimer()
		{
			if (_scrollTimer != null)
			{
				try
				{
					_scrollTimer.Tick -= new EventHandler(ScrollTimerTick);
					_scrollTimer.Dispose();
				}
				finally
				{
					_scrollTimer = null;
				}
				ScrollTimerInterval = StartScrollInterval;
			}
		}

		DAEData.Row _bookMark = null;
		private void ScrollTimerTick(object sender, EventArgs args)
		{
			if ((_bookMark == null) && (_rowDragObject != null))
				_bookMark = _rowDragObject.StartRow;

			if (_scrollTimer.ScrollDirection == ScrollDirection.Up)
			{
				if (Grid.Link.ActiveOffset > 0)
					Grid.Home();
				Grid.Link.DataSet.Prior();
			}
			else if (_scrollTimer.ScrollDirection == ScrollDirection.Down)
			{
				if (Grid.Link.ActiveOffset < (Grid.Rows.Count - 1))
					Grid.End();
				Grid.Link.DataSet.Next();
			}
			if (ScrollTimerInterval > 1)
				ScrollTimerInterval = _scrollTimer.Interval - (StartScrollInterval / 2) > 1 ? _scrollTimer.Interval - (StartScrollInterval / 2) : 1;
		}

		private int GetUpperAutoScrollHeight()
		{
			int UpperScrollHeight = AutoScrollHeight;
			if (Grid.HeaderHeight > 0)
				UpperScrollHeight = Math.Min(AutoScrollHeight, Grid.HeaderHeight);
			else
			{
				if (Grid.Rows.Count > 0)
					UpperScrollHeight = Math.Min(AutoScrollHeight, (RowRectangleAt(0).Height / 2));
			}
			return UpperScrollHeight;
		}

		private int _scrollTimerInterval = StartScrollInterval;

		protected internal int ScrollTimerInterval
		{
			get { return _scrollTimerInterval; }
			set
			{
				if (_scrollTimerInterval != value)
				{
					_scrollTimerInterval = value;
					if (_scrollTimer != null)
						_scrollTimer.Interval = value;
				}
			}
		}

		private void UpdateDragTimer(Point point)
		{
			Rectangle columnRectangle = ClientRectangle;
			if (columnRectangle.Height < (2 * AutoScrollHeight + 1))
				return;

			if (point.Y >= (Grid.Height - AutoScrollHeight))
			{
				if (_scrollTimer == null)
					CreateDragDropTimer(ScrollDirection.Down, true);
				else 
				{
					if (_scrollTimer.ScrollDirection != ScrollDirection.Down)
						_scrollTimer.ScrollDirection = ScrollDirection.Down;
				}
			}
			else if (point.Y < GetUpperAutoScrollHeight())
			{
				if (_scrollTimer == null)
					CreateDragDropTimer(ScrollDirection.Up, true);
				else 
				{
					if (_scrollTimer.ScrollDirection != ScrollDirection.Up)
						_scrollTimer.ScrollDirection = ScrollDirection.Up;
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
		public const int OrderIndicatorSpacing = 2; //Pixels.
		public const int OrderIndicatorPixelWidth = 10;

		public DataColumn()
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a GridColumn. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		/// <param name="horizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="titleAlignment"> Header contents alignment. </param>
		/// <param name="verticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="backColor"> Background color for this column. </param>
		/// <param name="visible"> Column is visible. </param>
		/// <param name="header3DStyle"> Header paint style. </param>
		/// <param name="header3DSide"> Header paint side. </param>
		/// <param name="minRowHeight"> Minimum height of a row. </param>
		/// <param name="maxRowHeight"> Maximum height of a row. </param>
		/// <param name="font"> Column Font </param>
		/// <param name="foreColor"> Foreground color of this column. </param> 
		public DataColumn
			(
			string columnName,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment titleAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor
			) : base(title, width, horizontalAlignment, titleAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
		{
			InitializeColumn();
			_columnName = columnName;
		}

		/// <summary> Used by the grid to create default columns. </summary>
		/// <param name="grid"> The DBGrid the collection belongs to. </param>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		protected internal DataColumn(DBGrid grid, string columnName) : base(grid)
		{
			InitializeColumn();
			_isDefaultGridColumn = true;
			_columnName = columnName;
		}

		/// <summary> Initializes a new instance of a GridColumn given a column name. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		public DataColumn (string columnName) : base(columnName)
		{
			InitializeColumn();
			_columnName = columnName;
		}

		/// <summary> Initializes a new instance of a GridColumn given a column name and title. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		public DataColumn (string columnName, string title) : base(title)
		{
			InitializeColumn();
			_columnName = columnName;
		}

		/// <summary> Initializes a new instance of a GridColumn given a title, and width. </summary>
		/// <param name="columnName"> Column to link to. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		public DataColumn (string columnName, string title, int width) : base(title, width)
		{
			InitializeColumn();
			_columnName = columnName;
		}

		private void InitializeColumn()
		{
			_isDefaultGridColumn = false;
			_columnName = String.Empty;
		}

		protected override bool ShouldSerializeTitle() { return Title != ColumnName; }
		[Category("Appearance")]
		[Description("Text in the column header.")]
		public override string Title
		{
			get { return base.Title == String.Empty ? Schema.Object.Dequalify(_columnName) : base.Title; }
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
		protected internal int ColumnIndex(DataLink link)
		{
			return (ColumnName != String.Empty) && (link != null) && link.Active ? link.DataSet.TableType.Columns.IndexOfName(ColumnName) : -1;
		}

		internal void InternalCheckColumnExists(DataLink link)
		{
			if ((ColumnName != String.Empty) && ((link != null) && link.Active))
			{
				if (ColumnIndex(link) < 0)
					throw new ControlsException(ControlsException.Codes.DataColumnNotFound, ColumnName);
			}
		}

		private string _columnName;
		/// <summary> Name of the view's column this column represents. </summary>
		[Category("Data")]
		[Description("Name of the column.")]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.GridColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (_isDefaultGridColumn)
					throw new ControlsException(ControlsException.Codes.InvalidUpdate);
				if (_columnName != value)
				{
					_columnName = value;
					InternalCheckColumnExists(Link);
					Changed();
				}
			}
		}

		protected internal bool _isDefaultGridColumn;
		[Browsable(false)]
		public bool IsDefaultGridColumn { get { return _isDefaultGridColumn; } }
		
		protected internal override void AddingToGrid(GridColumns gridColumns)
		{
			InternalCheckColumnExists(gridColumns.Grid != null ? gridColumns.Grid.Link : null);
			base.AddingToGrid(gridColumns);
		}

		[Browsable(false)]
		public bool InAscendingOrder
		{
			get
			{
				TableDataSet dataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (dataSet != null) && (dataSet.Order != null))
				{
					Schema.Order order = dataSet.Order;
					if (order.Columns.IndexOf(ColumnName) > -1)
						return ((Schema.OrderColumn)order.Columns[ColumnName]).Ascending;
				}
				return false;
			}
		}

		[Browsable(false)]
		public bool InDescendingOrder
		{
			get
			{
				TableDataSet dataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (dataSet != null) && (dataSet.Order != null))
				{
					Schema.Order order = dataSet.Order;
					if (order.Columns.IndexOf(ColumnName) > -1)
						return !((Schema.OrderColumn)order.Columns[ColumnName]).Ascending;
				}
				return false;
			}
		}

		[Browsable(false)]
		public bool InOrder
		{
			get
			{
				TableDataSet dataSet = Grid.Link.DataSet as TableDataSet;
				if ((Grid.Link.Active) && (dataSet != null) && (dataSet.Order != null))
					return dataSet.Order.Columns.IndexOf(ColumnName) >= 0;
				return false;
			}
		}

		public override int TitlePixelWidth
		{
			get
			{
				return !InOrder ? base.TitlePixelWidth : base.TitlePixelWidth + OrderIndicatorPixelWidth;
			}
		}

		public override int NaturalHeight(object value, Graphics graphics)
		{
			//Default measure as text without wordwrap.
			if ((Grid != null) && (value != null) && (value is DAEData.Scalar))
			{
				SizeF size;
				if ((value != null) && (value is string))
				{
					DAEData.Scalar scalar = (DAEData.Scalar)value;
					size = graphics.MeasureString(scalar.AsDisplayString, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				}
				else
					size = graphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight));
				if ((int)size.Height > Grid.MinRowHeight)
					return (int)size.Height + (Grid.Font.Height / 2);
				return (int)size.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		protected internal override void PaintHeaderCell(Graphics graphics, Rectangle cellRect, StringFormat format, System.Drawing.Color fixedColor)
		{
			base.PaintHeaderCell(graphics, cellRect, format, fixedColor);
			//Paint the order indicator if applicable...
			if ((InAscendingOrder) || (InDescendingOrder))
			{
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, fixedColor)))
				{
					Rectangle indicatorRect;
					Rectangle cellWorkArea = Rectangle.Inflate(cellRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height);
					Size textSize = Size.Round(graphics.MeasureString(Title, Grid.Font));
					if ((format.Alignment == StringAlignment.Near) || (format.Alignment == StringAlignment.Center))
					{
						if ((textSize.Width + OrderIndicatorSpacing) > (cellWorkArea.Width - OrderIndicatorPixelWidth))
							indicatorRect = new Rectangle(cellWorkArea.Right - OrderIndicatorPixelWidth, cellWorkArea.Top, OrderIndicatorPixelWidth, cellWorkArea.Height);
						else
							indicatorRect = new Rectangle(cellWorkArea.Left + (textSize.Width + OrderIndicatorSpacing), cellWorkArea.Top, OrderIndicatorPixelWidth, cellWorkArea.Height);
					}
					else
					{
						if ((textSize.Width + OrderIndicatorSpacing) > (cellWorkArea.Width - OrderIndicatorPixelWidth))
							indicatorRect = new Rectangle(cellWorkArea.Left, cellWorkArea.Top, OrderIndicatorPixelWidth, cellWorkArea.Height);
						else
						{
							int proposedLeft = cellWorkArea.Right - (textSize.Width + OrderIndicatorSpacing) - OrderIndicatorPixelWidth;
							indicatorRect = new Rectangle(proposedLeft >= cellWorkArea.Left ? proposedLeft : cellWorkArea.Left, cellWorkArea.Top, OrderIndicatorPixelWidth, cellWorkArea.Height);
						}	
					}
					indicatorRect.Height = Grid.Font.Height <= indicatorRect.Height ? Grid.Font.Height : indicatorRect.Height;
					if (indicatorRect.Width < Width)
					{
						graphics.FillRectangle(brush, indicatorRect);
						indicatorRect = Rectangle.Inflate(indicatorRect, 0, -4);
						if (InAscendingOrder)
						{
							Point[] triangle =
							{
								new Point(indicatorRect.Left + (indicatorRect.Width / 2), indicatorRect.Top),
								new Point(indicatorRect.Left, indicatorRect.Bottom),
								new Point(indicatorRect.Right, indicatorRect.Bottom)
							};
							brush.Color = Grid.OrderIndicatorColor;
							graphics.FillPolygon(brush, triangle);
						}
						else if (InDescendingOrder)
						{
							indicatorRect.Height -= 1;
							indicatorRect.Width -= 2;
							Point[] triangle =
							{	
								new Point(indicatorRect.Left + (indicatorRect.Width / 2), indicatorRect.Bottom),
								new Point(indicatorRect.Left, indicatorRect.Top),
								new Point(indicatorRect.Right, indicatorRect.Top)
							};
							brush.Color = Grid.OrderIndicatorColor;
							graphics.FillPolygon(brush, triangle);
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
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		/// <param name="horizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="titleAlignment"> Header contents alignment. </param>
		/// <param name="verticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="backColor"> Background color for this column. </param>
		/// <param name="visible"> Column is visible. </param>
		/// <param name="header3DStyle"> Header paint style. </param>
		/// <param name="header3DSide"> Header paint side. </param>
		/// <param name="minRowHeight"> Minimum height of a row. </param>
		/// <param name="maxRowHeight"> Maximum height of a row. </param>		
		/// <param name="font"> Column Font </param>
		/// <param name="foreColor"> Foreground color of this column. </param> 
		/// <param name="wordWrap"> Automatically wrap words to the next line in a row when necessary. </param>
		/// <param name="verticalText"> Layout text vertically in each row. </param>
		public TextColumn
			(
			string columnName,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment titleAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor,
			bool wordWrap,
			bool verticalText
			) : base(columnName, title, width, horizontalAlignment, titleAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
		{
			InitializeColumn();
			_wordWrap = wordWrap;
			_verticalText = verticalText;
		}

		/// <summary> Used by the grid to create default columns. </summary>
		/// <param name="grid"> The DBGrid the collection belongs to. </param>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		protected internal TextColumn(DBGrid grid, string columnName) : base(grid, columnName)
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a Text GridColumn given a column name. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		public TextColumn (string columnName) : base(columnName)
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a Text GridColumn given a column name and title. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		public TextColumn (string columnName, string title) : base(columnName, title)
		{
			InitializeColumn();
		}

		/// <summary> Initializes a new instance of a Text GridColumn given a title, and width. </summary>
		/// <param name="columnName"> Column to link to. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		public TextColumn (string columnName, string title, int width) : base(columnName, title, width)
		{
			InitializeColumn();
		}

		private void InitializeColumn()
		{
			_wordWrap = false;
			_verticalText = false;
		}

		protected virtual StringFormat GetStringFormat()
		{
			StringFormat stringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			if (!_wordWrap)
				stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
			if (_verticalText)
				stringFormat.FormatFlags |= StringFormatFlags.DirectionVertical;
			stringFormat.Alignment = AlignmentConverter.ToStringAlignment(HorizontalAlignment);
			return stringFormat;
		}

		private bool _wordWrap;
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool WordWrap
		{
			get { return _wordWrap; }
			set
			{
				if (_wordWrap != value)
				{
					_wordWrap = value;
					Changed();
				}
			}
		}

		private bool _verticalText;
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool VerticalText
		{
			get { return _verticalText; }
			set
			{
				if (_verticalText != value)
				{
					_verticalText = value;
					Changed();
				}
			}
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="value"> The value to measure. </param>
		/// <param name="graphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object value, Graphics graphics)
		{
			if ((Grid != null) && (value != null) && (value is DAEData.Scalar))
			{
				SizeF size;
				if (value != null)
				{
					DAEData.Scalar scalar = (DAEData.Scalar)value;
					size = graphics.MeasureString(scalar.AsDisplayString, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				}
				else
					size = graphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				if ((int)size.Height > Grid.MinRowHeight)
					return (int)size.Height + (Grid.Font.Height / 2);
				return (int)size.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}
		
		private void DoPaintCell
			(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect
			)
		{
			using (SolidBrush backBrush = new SolidBrush(value != null ? BackColor : Grid.NoValueBackColor))
			{
				using (SolidBrush foreBrush = new SolidBrush(ForeColor))
				{
					graphics.FillRectangle(backBrush, cellRect);
					string text = String.Empty;
					if (value is string)
						text = (string)value;
					else
					{
						if (value != null)
							text = value.ToString();
					}
					graphics.DrawString
						(
						#if DEBUGOFFSETS
						"VAO=" + Link.DataSet.ActiveOffset.ToString() + ',' + "LFO=" + Link.FirstOffset.ToString() + ',' + "LLO=" + Link.LastOffset.ToString() + ',' + "LBC=" + Link.BufferCount.ToString() + ',' + "LAO=" + Link.ActiveOffset.ToString() + ',' + "GRI=" + ARowIndex.ToString() + ',' + "ExtraRowOffset=" + ((GridDataLink)Link).ExtraRowOffset.ToString() + ',' + text,
						#else
						text,
						#endif
						Font,
						foreBrush,
						verticalMeasuredRect,
						GetStringFormat()
						);
				}
			}
		}

		protected override void PaintCell
			(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect
			)
		{
			if (value is DAEData.Row)
			{
				DAEData.Row row = (DAEData.Row)value;
				int columnIndex = ColumnIndex(Link);
				if ((columnIndex >= 0) && row.HasValue(columnIndex))
					DoPaintCell(((DAEData.Scalar)row.GetValue(columnIndex)).AsDisplayString, isSelected, rowIndex, graphics, cellRect, paddedRect, verticalMeasuredRect); 
				else
					DoPaintCell(null, isSelected, rowIndex, graphics, cellRect, paddedRect, verticalMeasuredRect);
			}
			else
				DoPaintCell(value, isSelected, rowIndex, graphics, cellRect, paddedRect, verticalMeasuredRect);
		}
		
	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.LinkColumnTypeConverter))]
	public class LinkColumn : DataColumn
	{
		/// <summary> Initializes a new instance of a GridColumn. </summary>
		public LinkColumn() : base() {}

		/// <summary> Initializes a new instance of a LinkColumn. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		/// <param name="horizontalAlignment"> Horizontal column contents alignment. </param>
		/// <param name="headerTextAlignment"> Header contents alignment. </param>
		/// <param name="verticalAlignment"> Vertical column contents alignment. </param>
		/// <param name="backColor"> Background color for this column. </param>
		/// <param name="visible"> Column is visible. </param>
		/// <param name="header3DStyle"> Header paint style. </param>
		/// <param name="header3DSide"> Header paint side. </param>
		/// <param name="minRowHeight"> Minimum height of a row. </param>
		/// <param name="maxRowHeight"> Maximum height of a row. </param>
		/// <param name="font"> Column Font </param>
		/// <param name="foreColor"> Foreground color of this column. </param> 
		/// <param name="wordWrap"> Automatically wrap words to the next line in a row when necessary. </param>
		public LinkColumn
			(
			string columnName,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment headerTextAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor,
			bool wordWrap
			) : base(columnName, title, width, horizontalAlignment, headerTextAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
		{
			_wordWrap = wordWrap;
		}

		/// <summary> Initializes a new instance of a LinkColumn given a column name, title, and width. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		/// <param name="title"> Column title. </param>
		/// <param name="width"> Column width in pixels. </param>
		public LinkColumn (string columnName, string title, int width) : base(columnName, title, width) {}

		/// <summary> Initializes a new instance of a LinkColumn given a column name and title. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		/// <param name="title"> Column title. </param>
		public LinkColumn (string columnName, string title) : base(columnName, title) {}

		/// <summary> Initializes a new instance of a GridColumn given a column name. </summary>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		public LinkColumn (string columnName) : base(columnName) {}
		
		///<summary> Used by the grid to create default columns. </summary>
		/// <param name="grid"> The DBGrid the collection belongs to. </param>
		/// <param name="columnName"> Name of the view's column this column represents. </param>
		protected internal LinkColumn(DBGrid grid, string columnName) : base(grid, columnName) {}

		protected override void Dispose(bool disposing)
		{
			if (_links != null)
			{
				_links.Clear();
				_links = null;
			}
			base.Dispose(disposing);
		}

		protected virtual StringFormat GetStringFormat()
		{
			StringFormat stringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			if (!_wordWrap)
				stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
			stringFormat.Alignment = AlignmentConverter.ToStringAlignment(HorizontalAlignment);
			return stringFormat;
		}

		private bool _wordWrap = false;
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool WordWrap
		{
			get { return _wordWrap; }
			set
			{
				if (_wordWrap != value)
				{
					_wordWrap = value;
					Changed();
				}
			}
		}

		/// <summary> Natural height of a row in pixels. </summary>
		/// <param name="value"> The value to measure. </param>
		/// <param name="graphics"> The grids graphics object. </param>
		/// <returns> The calculated height of the row. </returns>
		/// <remarks> Override this method to calculate the natural height of a row. </remarks>
		public override int NaturalHeight(object value, Graphics graphics)
		{
			if ((Grid != null) && (value is DAEData.Scalar))
			{
				SizeF size;
				if (value != null)
					size = graphics.MeasureString(((DAEData.Scalar)value).AsDisplayString, Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				else
					size = graphics.MeasureString("Hg", Font, new SizeF(Width, MaxRowHeight > -1 ? MaxRowHeight : GridMaxRowHeight), GetStringFormat());
				if ((int)size.Height > Grid.MinRowHeight)
					return (int)size.Height + (Grid.Font.Height / 2);
				return (int)size.Height;			
			}
			else
				return DBGrid.DefaultRowHeight;
		}

		private ControlsList _links;
		protected ControlsList Links
		{
			get
			{
				if (_links == null)
					_links = new ControlsList(this);
				return _links;
			}
		}

		public event WinForms.LinkLabelLinkClickedEventHandler OnExecuteLink;

		protected virtual void ExecuteLink(object sender, WinForms.LinkLabelLinkClickedEventArgs args)
		{
			SelectRow(Links.IndexOf(sender));
			if (OnExecuteLink != null)
				OnExecuteLink(this, args);
		}

		private void ControlClick(object sender, EventArgs args)
		{
			SelectRow(Links.IndexOf(sender));
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

		protected virtual void SetControlProperties(WinForms.Control control)
		{
			control.ForeColor = ForeColor;
			if (BackColor.A < 255)
				control.BackColor = BackColor;
			else
				control.BackColor = Color.FromArgb(60, BackColor);
			control.Font = Font;
			((WinForms.LinkLabel)control).TextAlign = GetContentAlignment();
		}
		
		protected internal override void AddingControl(WinForms.Control control, int index)
		{
			base.AddingControl(control, index);
			control.Click += new EventHandler(ControlClick);
			((WinForms.LinkLabel)control).LinkClicked += new WinForms.LinkLabelLinkClickedEventHandler(ExecuteLink);
			control.Parent = Grid;
			control.TabStop = false;
			SetControlProperties(control);
		}

		protected internal override void RemovingControl(WinForms.Control control, int index)
		{
			if (control.ContainsFocus)
				if ((Grid != null) && (Grid.CanFocus))
					Grid.Focus();
			control.Click -= new EventHandler(ControlClick);
			((WinForms.LinkLabel)control).LinkClicked -= new WinForms.LinkLabelLinkClickedEventHandler(ExecuteLink);
			control.Parent = null;
			base.RemovingControl(control, index);
		}

		private void AddControl(Rectangle clientRect, object value)
		{
			WinForms.Control control = new WinForms.LinkLabel();
			control.Bounds = clientRect;
			control.Text = (string)value;
			Links.Add(control);
		}

		private void UpdateControl(Rectangle clientRect, int index, object value)
		{
			WinForms.Control control = (WinForms.Control)Links[index];
			control.Bounds = clientRect;
			control.Text = (string)value;
			SetControlProperties(control);
		}

		protected internal override void RowChanged(object value, Rectangle rowRectangle, int rowIndex)
		{
			base.RowChanged(value, rowRectangle, rowIndex);
			Rectangle columnRect = ClientRectangle;
			if (!columnRect.IsEmpty)
			{
				string localValue = String.Empty;
				if (value is DAEData.Row)
				{
					DAEData.Row row = (DAEData.Row)value;
					int columnIndex = ColumnIndex(Link);
					if (row.HasValue(columnIndex))
						localValue = ((DAEData.Scalar)row.GetValue(columnIndex)).AsDisplayString;
				}

				Rectangle controlRect = new Rectangle(columnRect.X, rowRectangle.Y, columnRect.Width, rowRectangle.Height);
				if (rowIndex >= Links.Count)
					AddControl(Rectangle.Inflate(controlRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height), localValue);
				else
					UpdateControl(Rectangle.Inflate(controlRect, -Grid.CellPadding.Width, -Grid.CellPadding.Height), rowIndex, localValue);
			}
			else
				Links.Clear();
		}

		protected internal override void RowsChanged()
		{
			base.RowsChanged();
			//Dispose extra LinkLabels.
			if (_links != null)
			{
				if ((Grid != null) && (Grid.Link != null) && (Grid.Rows.Count > 0))
					while (_links.Count > Grid.Rows.Count)
						_links.Remove(_links[_links.Count - 1]);
				else
					_links.Clear();
			}
		}

		protected override void PaintCell
		(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect
		)
		{
			using (SolidBrush backBrush = new SolidBrush(value != null ? BackColor : Grid.NoValueBackColor))
				graphics.FillRectangle(backBrush, cellRect);
		}

	}

	[TypeConverter(typeof(Alphora.Dataphor.DAE.Client.Controls.CheckBoxColumnTypeConverter))]
	public class CheckBoxColumn : DataColumn
	{
		public const int StandardCheckSize = 13;

		public CheckBoxColumn() : base()
		{
			InitializeColumn();
		}

		protected internal CheckBoxColumn(DBGrid grid, string columnName) : base(grid, columnName)
		{
			InitializeColumn();
		}

		public CheckBoxColumn
		(
			string columnName,
			string title,
			int width,
			int autoUpdateInterval,
			bool readOnly
		) : base(columnName, title, width)
		{
			InitializeColumn();
			_readOnly = readOnly;
			_autoUpdateInterval = autoUpdateInterval;
		}

		public CheckBoxColumn
		(
			string columnName,
			string title,
			int autoUpdateInterval,
			bool readOnly
		) : base(columnName, title)
		{
			InitializeColumn();
			_readOnly = readOnly;
			_autoUpdateInterval = autoUpdateInterval;
		}

		public CheckBoxColumn
		(
			string columnName,
			int autoUpdateInterval,
			bool readOnly
		) : base(columnName)
		{
			InitializeColumn();
			_readOnly = readOnly;
			_autoUpdateInterval = autoUpdateInterval;
		}

		public CheckBoxColumn
		(
			string columnName,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment headerTextAlignment,
			VerticalAlignment verticalAlignment, 
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor,
			int autoUpdateInterval,
			bool readOnly
		) : base (columnName, title, width, horizontalAlignment, headerTextAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
		{
			InitializeColumn();
			_readOnly = readOnly;
			_autoUpdateInterval = autoUpdateInterval;
		}

		private void InitializeColumn()
		{
			_readOnly = true;
			_autoUpdateInterval = 200;
			_autoUpdateTimer = new WinForms.Timer();
			_autoUpdateTimer.Tick += new EventHandler(AutoUpdateElapsed);
			_autoUpdateTimer.Enabled = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				_autoUpdateTimer.Tick -= new EventHandler(AutoUpdateElapsed);
				_autoUpdateTimer.Dispose();
				_autoUpdateTimer = null;
			}
			base.Dispose(disposing);
		}

		private WinForms.Timer _autoUpdateTimer;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected WinForms.Timer AutoUpdateTimer
		{
			get { return _autoUpdateTimer; }
		}

		private int _autoUpdateInterval;
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

		private bool _readOnly;
		/// <summary> Read only state of the column. </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set 
			{
				if (value != _readOnly)
				{
					if (Grid != null)
						Grid.Invalidate();
					_readOnly = value; 
				}
			}
		}

		public virtual bool GetReadOnly()
		{
			return _readOnly || ((Grid != null) && Grid.ReadOnly);
		}

		protected Rectangle GetCheckboxRectAt(int rowIndex)
		{
			Rectangle cellRect = Rectangle.Inflate(CellRectangleAt(rowIndex), -Grid.CellPadding.Width, -Grid.CellPadding.Height);
			int halfHeightOfCheckBox = (Grid.MinRowHeight / 4) + 1;
			return new Rectangle
			(
				cellRect.X + (cellRect.Width / 2) - halfHeightOfCheckBox,
				cellRect.Y + (cellRect.Height / 2) - halfHeightOfCheckBox,
				2 * halfHeightOfCheckBox,
				2 * halfHeightOfCheckBox
			);
		}

		private void PaintReadOnlyChecked(Graphics graphics, Rectangle rect, WinForms.ButtonState state)
		{
			WinForms.ControlPaint.DrawBorder3D(graphics, rect, WinForms.Border3DStyle.SunkenOuter);
			rect.Inflate(-1, -1);
			using (SolidBrush brush = new SolidBrush(SystemColors.ControlLight))
			{
				graphics.FillRectangle(brush, rect);
				rect.Inflate(-2, -2);
				if ((state & WinForms.ButtonState.Inactive) == 0)
				{
					WinForms.ControlPaint.DrawBorder3D(graphics, rect, WinForms.Border3DStyle.RaisedOuter);
					rect.Inflate(-1, -1);
					if ((state & WinForms.ButtonState.Checked) != 0)
						brush.Color = Color.Navy;
					else
						brush.Color = SystemColors.ControlDark;
					graphics.FillRectangle(brush, rect);
				}
			}
		}

		protected override void PaintCell
		(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect
		)
		{
			DAEData.Row row = value as DAEData.Row;
			if (row != null)
			{
				WinForms.ButtonState buttonState;
				int columnIndex = ColumnIndex(Link);
				if (row.HasValue(columnIndex))
					buttonState = ((DAEData.Scalar)row.GetValue(columnIndex)).AsBoolean ? WinForms.ButtonState.Checked : WinForms.ButtonState.Normal;
				else
					buttonState = WinForms.ButtonState.Inactive;

				Size checkSize = WinForms.SystemInformation.MenuCheckSize;
				Rectangle paintRect = 
					new Rectangle
					(
						new Point
						(
							cellRect.X + ((cellRect.Width - checkSize.Width) / 2),
							cellRect.Y + ((cellRect.Height - checkSize.Height) / 2)
						),
						checkSize
					);

				if (GetReadOnly())
					PaintReadOnlyChecked(graphics, paintRect, buttonState);
				else
					WinForms.ControlPaint.DrawCheckBox(graphics, paintRect, buttonState);
			}

		}

		protected void SaveRequested(DAEClient.DataLink link, DAEClient.DataSet dataSet)
		{
			if (_autoUpdateTimer.Enabled)
				_autoUpdateTimer.Stop();

			Link.OnSaveRequested -= new DAEClient.DataLinkHandler(SaveRequested);

			if ((link != null) && link.Active && (ColumnName != String.Empty))
			{
				DAEClient.DataField field = Link.DataSet.Fields[ColumnName];

				field.AsBoolean = !(bool)field.AsBoolean;
				if (_oldState == DAEClient.DataSetState.Browse)
				{
					try
					{
						dataSet.Post();
					}
					catch
					{
						dataSet.Cancel();
						throw;
					}
				}
			}
		}

		protected void AutoUpdateElapsed(object sender, EventArgs args)
		{
			_autoUpdateTimer.Stop();
			if (Link.Active)
				Link.DataSet.RequestSave();
		}

		private bool _allowEdit = false;
		private object _rowMouseDownOver;
		protected internal override void MouseDown(WinForms.MouseEventArgs args, int rowIndex)
		{
			base.MouseDown(args, rowIndex);
			if (rowIndex >= 0)
			{
				_rowMouseDownOver = RowValueAt(rowIndex);
				Rectangle boxRect = GetCheckboxRectAt(rowIndex);
				_allowEdit = CanToggle() && boxRect.Contains(args.X, args.Y);
			}
		}

		private DAEClient.DataSetState _oldState;
		protected internal override void MouseUp(WinForms.MouseEventArgs args, int rowIndex)
		{
			base.MouseUp(args, rowIndex);
			try
			{
				if (_allowEdit && (rowIndex >= 0) && (RowValueAt(rowIndex) == _rowMouseDownOver))
				{
					Rectangle boxRect = Rectangle.Inflate(GetCheckboxRectAt(rowIndex), 1, 1);
					if (boxRect.Contains(args.X, args.Y))
						Toggle();
				}
			}
			finally
			{
				_allowEdit = false;
				_rowMouseDownOver = null;
			}
		}

		public override bool CanToggle()
		{
			return !GetReadOnly() && Link.Active && !Link.DataSet.IsReadOnly;
		}

		public override void Toggle()
		{
			_oldState = Link.DataSet.State;
			Link.DataSet.Edit();
			if ((Link.DataSet.State == DAEClient.DataSetState.Insert) || (Link.DataSet.State == DAEClient.DataSetState.Edit))
			{
				Link.OnSaveRequested += new DAEClient.DataLinkHandler(SaveRequested);
				_autoUpdateTimer.Interval = _autoUpdateInterval;
				_autoUpdateTimer.Start();
			}
		}
	}

	[ToolboxItem(false)]
	public class ColumnDragObject
	{
		public ColumnDragObject(DBGrid grid, GridColumn columnToDrag, Point startDragPoint, Size centerOffset, Image dragImage) : base()
		{
			_grid = grid;
			_dragColumn = columnToDrag;
			_startDragPoint = startDragPoint;
			_dragImage = dragImage;
			_cursorCenterOffset = centerOffset;
			DropTargetIndex = _grid.Columns.IndexOf(_dragColumn);
		}

		private DBGrid _grid;
		public DBGrid Grid
		{
			get { return _grid; }
		}

		private GridColumn _dragColumn;
		public GridColumn DragColumn
		{
			get { return _dragColumn; }
		}

		private Point _startDragPoint;
		public Point StartDragPoint
		{
			get { return _startDragPoint; }
		}

		private Size _cursorCenterOffset;
		public Size CursorCenterOffset
		{
			get { return _cursorCenterOffset; }
		}

		private int _dropTargetIndex;
		public int DropTargetIndex
		{
			get { return _dropTargetIndex; }
			set { _dropTargetIndex = value; }
		}

		private Rectangle _highlightRect;
		public Rectangle HighlightRect
		{
			get { return _highlightRect; }
			set { _highlightRect = value; }
		}

		private Point _imageLocation;
		public Point ImageLocation
		{
			get { return _imageLocation; }
			set { _imageLocation = value; }
		}

		private Image _dragImage;
		public Image DragImage
		{
			get { return _dragImage; }
		}
	}

	/// <summary> Scrolling directions. </summary>
	/// <remarks> Valid combinations are Up Left, Up Right, Down Left, Down Right and each individual value. </remarks>
	[Flags]
	public enum ScrollDirection { Left, Right, Up, Down }

	[ToolboxItem(false)]
	public class DragDropTimer : WinForms.Timer
	{
		public DragDropTimer(int interval) : base()
		{
			Interval = interval;
		}

		private ScrollDirection _scrollDirection = ScrollDirection.Left;
		public ScrollDirection ScrollDirection
		{
			get { return _scrollDirection; }
			set { _scrollDirection = value; }
		}
	}

	public class ImageColumn : DataColumn
	{
		public ImageColumn() : base()
		{
			InitializeColumn();
		}

		public ImageColumn (string columnName) : base(columnName)
		{
			InitializeColumn();
		}

		public ImageColumn
			(
			string columnName,
			string title,
			int width
			) : base(columnName, title, width)
		{
			InitializeColumn();
		}

		public ImageColumn
			(
			string columnName,
			string title
			) : base(columnName, title)
		{
			InitializeColumn();
		}

		public ImageColumn
			(
			string columnName,
			string title,
			int width,
			WinForms.HorizontalAlignment horizontalAlignment,
			WinForms.HorizontalAlignment headerTextAlignment,
			VerticalAlignment verticalAlignment,
			Color backColor,
			bool visible,
			WinForms.Border3DStyle header3DStyle,
			WinForms.Border3DSide header3DSide,
			int minRowHeight,
			int maxRowHeight,
			Font font,
			Color foreColor
			) : base (columnName, title, width, horizontalAlignment, headerTextAlignment, verticalAlignment, backColor, visible, header3DStyle, header3DSide, minRowHeight, maxRowHeight, font, foreColor)
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

		public override int NaturalHeight(object value, Graphics graphics)
		{
			if ((value == null) || !(value is DAEData.DataValue))
				return DBGrid.DefaultRowHeight;
			Image image;
			Stream stream = ((DAEData.DataValue)value).OpenStream();
			try
			{
				MemoryStream copyStream = new MemoryStream();
				StreamUtility.CopyStream(stream, copyStream);
				image = Image.FromStream(copyStream);
			}
			finally
			{
				stream.Close();
			}
			Rectangle imageRectangle = new Rectangle(0, 0, image.Width, image.Height);
			Rectangle clientRect = new Rectangle(0, 0, Width, image.Height);
			imageRectangle = ImageAspect.ImageAspectRectangle(imageRectangle, clientRect);
			return imageRectangle.Height < image.Height ? imageRectangle.Height : image.Height;
		}

		protected override void PaintCell
			(
			object value,
			bool isSelected,
			int rowIndex,
			Graphics graphics,
			Rectangle cellRect,
			Rectangle paddedRect,
			Rectangle verticalMeasuredRect 
			)
		{
			if (value is DAEData.Row)
			{
				DAEData.Row row = (DAEData.Row)value;
				Image image = null;
				int columnIndex = ColumnIndex(Link);
				if (row.HasValue(columnIndex))
				{
					Stream stream = row.GetValue(columnIndex).OpenStream();
					try
					{
						MemoryStream copyStream = new MemoryStream();
						StreamUtility.CopyStream(stream, copyStream);
						image = Image.FromStream(copyStream);
					}
					finally
					{
						stream.Close();
					}
				}
				if (image != null)
				{
					Rectangle imageRect = new Rectangle(0 , 0, image.Width, image.Height);
					imageRect = ImageAspect.ImageAspectRectangle(imageRect, paddedRect);
 
					switch (HorizontalAlignment)
					{
						case WinForms.HorizontalAlignment.Center :
							imageRect.Offset
								(
								(paddedRect.Width - imageRect.Width) / 2,
								(paddedRect.Height - imageRect.Height) / 2
								);
							break;
						case WinForms.HorizontalAlignment.Right :
							imageRect.Offset
								(
								(paddedRect.Width - imageRect.Width),
								(paddedRect.Height - imageRect.Height)
								);
							break;
					}
				 
					imageRect.X += paddedRect.X;
					imageRect.Y += paddedRect.Y;
					graphics.DrawImage(image, imageRect);
				}
			}

		}
	}

	/// <summary> Converts StringAlignment to HorizontalAlignment and HorizontalAlignment to StringAlignment. </summary>
	public class AlignmentConverter
	{
		public static WinForms.HorizontalAlignment ToHorizontalAlignment(StringAlignment alignment)
		{
			switch (alignment)
			{
				case StringAlignment.Near : return WinForms.HorizontalAlignment.Left;
				case StringAlignment.Center : return WinForms.HorizontalAlignment.Center;
				case StringAlignment.Far : return WinForms.HorizontalAlignment.Right;
				default : return WinForms.HorizontalAlignment.Left;
			}
		}

		public static StringAlignment ToStringAlignment(WinForms.HorizontalAlignment alignment)
		{
			switch (alignment)
			{
				case WinForms.HorizontalAlignment.Left : return StringAlignment.Near;
				case WinForms.HorizontalAlignment.Center : return StringAlignment.Center;
				case WinForms.HorizontalAlignment.Right : return StringAlignment.Far;
				default : return StringAlignment.Near;
			}
		}
	}
}