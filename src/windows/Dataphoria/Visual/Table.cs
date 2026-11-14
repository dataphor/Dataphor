/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public class Table : Control
	{
		public const int MinAutoSize = 80;	// Minimum auto-size (after padding)

		public Table() : base()
		{
			SetStyle(ControlStyles.ResizeRedraw, false);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.Selectable, false);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			TabStop = false;

			_columns = new TableColumns(this);
			_rows = new TableRows(this);
			_rowHeight = Font.Height + 8;
			_headerFont = new Font(this.Font, FontStyle.Bold);
			_headerForeColor = Color.Black;
			_headerHeight = _headerFont.Height + 8;

			SuspendLayout();
			try
			{
				_hScrollBar = new HScrollBar();
				_hScrollBar.SmallChange = 1;
				_hScrollBar.LargeChange = 1;
				_hScrollBar.Minimum = 0;
				_hScrollBar.TabStop = false;
				_hScrollBar.Scroll += new ScrollEventHandler(HScrollBarScrolled);
				Controls.Add(_hScrollBar);

				_vScrollBar = new VScrollBar();
				_vScrollBar.SmallChange = 1;
				_vScrollBar.Minimum = 0;
				_vScrollBar.TabStop = false;
				_vScrollBar.Scroll += new ScrollEventHandler(VScrollBarScrolled);
				Controls.Add(_vScrollBar);
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		#region Cosmetic Properties

		private int _rowHeight;
		public int RowHeight
		{
			get { return _rowHeight; }
			set
			{
				if (value < 1)
					value = 1;
				if (_rowHeight != value)
				{
					_rowHeight = value;
					PerformLayout();
					Invalidate(false);
				}
			}
		}

		private int _headerHeight;
		public int HeaderHeight
		{
			get { return _headerHeight; }
			set
			{
				if (value < 1)
					value = 1;
				if (_headerHeight != value)
				{
					_headerHeight = value;
					PerformLayout();
					Invalidate(false);
				}
			}
		}

		private Font _headerFont;
		public Font HeaderFont
		{
			get { return _headerFont; }
			set 
			{ 
				_headerFont = value; 
				InvalidateHeader();
			}
		}

		private Color _headerForeColor;
		public Color HeaderForeColor
		{
			get { return _headerForeColor; }
			set
			{
				_headerForeColor = value;
				InvalidateHeader();
			}
		}

		private Color _shadowColor = Color.FromArgb(180, Color.Black);
		public Color ShadowColor
		{
			get { return _shadowColor; }
			set
			{
				_shadowColor = value;
				InvalidateHeader();
			}
		}

		private Color _lineColor = Color.White;
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				_lineColor = value;
				InvalidateHeader();
			}
		}

		private int _horizontalPadding = 2;
		public int HorizontalPadding
		{
			get { return _horizontalPadding; }
			set 
			{ 
				_horizontalPadding = value;
				Invalidate(false);
				PerformLayout();
			}
		}

		private void InvalidateHeader()
		{
			Rectangle bounds = base.DisplayRectangle;
			bounds.Height = _headerHeight;
			Invalidate(bounds, false);
		}

		#endregion

		#region Scrolling & Layout

		private HScrollBar _hScrollBar;
		private VScrollBar _vScrollBar;

		private int _visibleRows;

		private void HScrollBarScrolled(object sender, ScrollEventArgs args)
		{
			NavigateTo(new Point(args.NewValue, _location.Y));
		}

		private void VScrollBarScrolled(object sender, ScrollEventArgs args)
		{
			NavigateTo(new Point(_location.X, args.NewValue));
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
			{
				case Keys.Left :
					NavigateTo(new Point(_location.X - 1, _location.Y));
					break;
				case Keys.Up :
					NavigateTo(new Point(_location.X, _location.Y - 1));
					break;
				case Keys.Right :
					NavigateTo(new Point(_location.X + 1, _location.Y));
					break;
				case Keys.Down :
					NavigateTo(new Point(_location.X, _location.Y + 1));
					break;
				default :
					return base.ProcessDialogKey(key);
			}
			return true;
		}

		private static int Constrain(int tempValue, int min, int max)
		{
			if (tempValue > max)
				tempValue = max;
			if (tempValue < min)
				tempValue = min;
			return tempValue;
		}

		private Point _location;

		private void NavigateTo(Point location)
		{
			location.X = Constrain(location.X, _hScrollBar.Minimum, _hScrollBar.Maximum);
			location.Y = Constrain(location.Y, _vScrollBar.Minimum, _vScrollBar.Maximum);
			if (location != _location)
			{
				Point delta = new Point(SumWidths(_location.X, location.X), (_location.Y - location.Y) * _rowHeight);

				_location = location;
				
				_hScrollBar.Value = _location.X;
				_vScrollBar.Value = _location.Y;

				// Scroll vertically (seperate from horizontal because it does not affect header
				RECT rect = UnsafeUtilities.RECTFromRectangle(DataRectangle);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, 0, delta.Y, ref rect, ref rect, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */);

				// Scroll horizontally
				rect = UnsafeUtilities.RECTFromRectangle(DisplayRectangle);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, delta.X, 0, ref rect, ref rect, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */);

				PerformLayout();
			}
		}

		private int SumWidths(int startIndex, int endIndex)
		{
			int result = 0;
			int polarity = (startIndex > endIndex ? 1 : -1);
			for (int i = startIndex; i != endIndex; i -= polarity)
				result += _columns[i].Width * polarity;
			return result;
		}

		/// <summary> The number of columns (starting from the last) that would completely fit in the display area. </summary>
		private int FittingColumns()
		{
			Rectangle bounds = DataRectangle;
			int extentX = bounds.X;
			int count = 0;
			for (int x = _columns.Count - 1; x >= 0; x--)
			{
				extentX += _columns[x].Width;
				if (extentX <= bounds.Width)
					count++;
				else
					break;
			}
			return count;
		}

		public virtual void AutoSizeColumns(int width)
		{
			// Calculate the size (in excess of the min auto size for each auto-sized column
			int[] sizes = new int[_columns.Count];
			int max;
			int current;
			int y;
			using (Graphics graphics = CreateGraphics())
			{
				for (int x = 0; x < _columns.Count; x++)
				{
					max = 0;
					if (_columns[x].AutoSize)
					{
						for (y = 0; y < _rows.Count; y++)
						{
							current = Size.Ceiling(graphics.MeasureString(GetValue(new Point(x, y)), Font)).Width + (_horizontalPadding * 2);
							if (current > max)
								max = current;
						}
						sizes[x] = Math.Max(0, max - MinAutoSize);
					}
				}
			}

			// Sum the column sizes
			int autoSum = 0;	// Sum of auto sized columns excesses
			int fixedSum = 0;	// Sum of fixed sized columns widths
			int autoCount = 0;	// Number of auto sized columns
			for (int x = 0; x < _columns.Count; x++)
			{
				if (_columns[x].AutoSize)
				{
					autoSum += sizes[x];
					autoCount++;
				}
				else
					fixedSum += _columns[x].Width;
			}

			int excessWidth = Math.Max(0, width - (fixedSum + (autoCount * MinAutoSize)));

			// Distribute the excess to the scrunched auto-sized columns based on their size proportion over the minumum auto-size
			for (int x = 0; x < _columns.Count; x++)
				if (_columns[x].AutoSize)
					_columns[x]._width = MinAutoSize + (autoSum == 0 ? 0 : Math.Min(sizes[x], (int)(((double)sizes[x] / (double)autoSum) * excessWidth)));
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			// Don't call base

			Rectangle bounds = DataRectangle;
			
			AutoSizeColumns(bounds.Width);

			_visibleRows = (bounds.Height / _rowHeight) + (((bounds.Height % _rowHeight) == 0) ? 0 : 1);
			_vScrollBar.LargeChange = Math.Max((_visibleRows - 1), 1);

			bounds = base.DisplayRectangle;

			_hScrollBar.Maximum = Math.Max(0, _columns.Count - Math.Max(FittingColumns(), 1));
			_vScrollBar.Maximum = Math.Max(0, _rows.Count - 1);

			// Scroll off wasted vertical space
			Point newValue = 
				new Point
				(
					Math.Min(_location.X, _hScrollBar.Maximum), 
					Math.Max(0, Math.Min(_rows.Count, _location.Y + (_visibleRows - 1)) - (_visibleRows - 1))
				);
			if (newValue != _location)
				NavigateTo(newValue);

			_vScrollBar.Visible = (_vScrollBar.Maximum > 0) && (_vScrollBar.LargeChange <= _vScrollBar.Maximum);
			_hScrollBar.Visible = (_hScrollBar.Maximum > 0);

			_vScrollBar.Bounds =
				new Rectangle
				(
					bounds.Right - _vScrollBar.Width,
					_headerHeight,
					_vScrollBar.Width,
					bounds.Height - (_headerHeight + (_hScrollBar.Visible ? _hScrollBar.Height : 0))
				);

			_hScrollBar.Top = bounds.Bottom - _hScrollBar.Height;
			_hScrollBar.Width = bounds.Width - (_vScrollBar.Visible ? _vScrollBar.Width : 0);

			UpdateDesigners();

			// Position the designers
			bounds = DataRectangle;
			IElementDesigner designer;
			int extentX = bounds.X;
			TableColumn column;
			int maxY = Math.Min(_rows.Count, _location.Y + _visibleRows);
			for (int x = _location.X; (extentX <= bounds.Width) && (x < _columns.Count); x++)
			{
				column = _columns[x];

				for (int y = _location.Y; y < maxY; y++)
				{
					designer = (IElementDesigner)_designers[new Point(x, y)];
					if (designer != null)
						designer.Bounds =
							new Rectangle
							(
								extentX + _horizontalPadding, 
								bounds.Y + ((y - _location.Y) * _rowHeight), 
								column.Width - (_horizontalPadding * 2), 
								_rowHeight
							);
				}

				extentX += column.Width;
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				if (_vScrollBar.Visible)
					bounds.Width -= _vScrollBar.Width;
				if (_hScrollBar.Visible)
					bounds.Height -= _hScrollBar.Height;
				return bounds;
			}
		}

		public virtual Rectangle DataRectangle
		{
			get
			{
				Rectangle bounds = DisplayRectangle;
				bounds.Y += _headerHeight;
				bounds.Height -= _headerHeight;
				return bounds;
			}
		}

		private int _updateCount = 0;

		public void BeginUpdate()
		{
			_updateCount++;
			SuspendLayout();
		}

		public void EndUpdate()
		{
			_updateCount = Math.Max(0, _updateCount - 1);
			if (IsHandleCreated && (_updateCount == 0))
			{
				Invalidate(false);
				ResumeLayout(true);
			}
			else
				ResumeLayout(false);
		}

		#endregion

		#region Designers

		// List of active designers by their cell coordinate
		private Hashtable _designers = new Hashtable();

		/// <summary> Reconciles the set of visible designers with the set of visible cells. </summary>
		private void UpdateDesigners()
		{
			if (_updateCount == 0)
			{
				// List of unused designers (initially full)
				ArrayList designed = new ArrayList(_designers.Count);
				designed.AddRange(_designers.Keys);
				IElementDesigner designer;

				SuspendLayout();
				try
				{
					Rectangle bounds = DataRectangle;
					int extentX = bounds.X;
					TableColumn column;
					int maxY = Math.Min(_rows.Count, _location.Y + _visibleRows);
					using (Graphics graphics = CreateGraphics())
					{
						for (int x = _location.X; (extentX <= bounds.Width) && (x < _columns.Count); x++)
						{
							column = _columns[x];

							for (int y = _location.Y; y < maxY; y++)
							{
								if
								(
									GetDesignerRequired
									(
										new Point(x, y),
										graphics,
										new Rectangle
										(
											extentX + _horizontalPadding, 
											bounds.Y + ((y - _location.Y) * _rowHeight), 
											column.Width - (_horizontalPadding * 2), 
											_rowHeight
										)
									)
								)
								{
									designer = (IElementDesigner)_designers[new Point(x, y)];
									if (designer == null)
										designer = AddDesigner(new Point(x, y));
									else
										designed.Remove(new Point(x, y));
									((Control)designer).TabIndex = x + (y * _columns.Count);
								}
							}

							extentX += column.Width;
						}
					}

					// Remove unused designers
					foreach (Point cell in designed)
						RemoveDesigner(cell);
				}
				finally
				{
					ResumeLayout(false);
				}
			}
		}

		public event GetTableDesignerHandler OnGetDesigner;

		protected virtual IElementDesigner GetDesigner(Point cell)
		{
			if (OnGetDesigner != null)
				return OnGetDesigner(this, cell);
			else
				return null;
		}

		public event GetTableDesignerRequiredHandler OnGetDesignerRequired;

		protected virtual bool GetDesignerRequired(Point cell, Graphics graphics, Rectangle bounds)
		{
			if (OnGetDesignerRequired != null)
				return OnGetDesignerRequired(this, cell, graphics, bounds);
			else
				return false;
		}

		private IElementDesigner AddDesigner(Point cell)
		{
			IElementDesigner designer = GetDesigner(cell);
			if (designer != null)
			{
				try
				{
					_designers.Add(cell, designer);
					Controls.Add((Control)designer);
				}
				catch
				{
					designer.Dispose();
					throw;
				}
			}
			return designer;
		}

		private void RemoveDesigner(Point cell)
		{
			IElementDesigner designer = (IElementDesigner)_designers[cell];
			if (designer != null)
			{
				Controls.Remove((Control)designer);
				_designers.Remove(cell);
			}
		}

		#endregion

		#region Painting

		private StringFormat _headerStringFormat;
		protected virtual StringFormat GetHeaderStringFormat()
		{
			if (_headerStringFormat == null)
			{
				_headerStringFormat = new StringFormat();
				_headerStringFormat.Trimming = StringTrimming.EllipsisCharacter;
				_headerStringFormat.FormatFlags &= ~StringFormatFlags.NoWrap;
			}
			return _headerStringFormat;
		}

		private StringFormat _dataStringFormat;
		protected virtual StringFormat GetDataStringFormat()
		{
			if (_dataStringFormat == null)
			{
				_dataStringFormat = new StringFormat();
				_dataStringFormat.Trimming = StringTrimming.EllipsisCharacter;
				_dataStringFormat.FormatFlags &= ~StringFormatFlags.NoWrap;
			}
			return _dataStringFormat;
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);

			Rectangle bounds = DisplayRectangle;

			using (Pen pen = new Pen(LineColor))
			{
				using (Pen shadowPen = new Pen(ShadowColor))
				{
					using (SolidBrush brush = new SolidBrush(_headerForeColor))
					{
						int extentX = bounds.X;
						TableColumn column;
						int maxY = Math.Min(_rows.Count, _location.Y + _visibleRows);
						for (int x = _location.X; (extentX <= bounds.Width) && (x < _columns.Count); x++)
						{
							column = _columns[x];

							// Paint the header
							brush.Color = _headerForeColor;
							args.Graphics.DrawString
							(
								column.Title, 
								_headerFont, 
								brush, 
								new Rectangle
								(
									extentX + _horizontalPadding, 
									bounds.Y + ((_headerHeight / 2) - (this.Font.Height / 2)),
									column.Width - (_horizontalPadding * 2), 
									this.Font.Height
								), 
								GetHeaderStringFormat()
							);
							args.Graphics.DrawLine(pen, new Point(extentX, bounds.Y + _headerHeight - 2), new Point(extentX + column.Width - 2, bounds.Y + _headerHeight - 2));
							args.Graphics.DrawLine(shadowPen, new Point(extentX + 1, bounds.Y + _headerHeight - 1), new Point(extentX + column.Width - 1, bounds.Y + _headerHeight - 1));

							if (x < (_columns.Count - 1))
							{
								args.Graphics.DrawLine(pen, new Point(extentX + column.Width - 2, bounds.Y + _headerHeight), new Point(extentX + column.Width - 2, bounds.Y + _headerHeight + (maxY * _rowHeight) - 1));
								args.Graphics.DrawLine(shadowPen, new Point(extentX + column.Width - 1, bounds.Y + _headerHeight + 1), new Point(extentX + column.Width - 1, bounds.Y + _headerHeight + (maxY * _rowHeight)));
							}

							brush.Color = ForeColor;
							// Paint the data cells
							for (int y = _location.Y; y < maxY; y++)
							{
								args.Graphics.DrawString
								(
									GetValue(new Point(x, y)), 
									this.Font, 
									brush, 
									new Rectangle
									(
										extentX + _horizontalPadding, 
										bounds.Y + _headerHeight + (((y - _location.Y) * _rowHeight) + ((_rowHeight / 2) - (this.Font.Height / 2))), 
										column.Width - (_horizontalPadding * 2), 
										this.Font.Height
									),
									GetDataStringFormat()
								);
							}

							extentX += column.Width;
						}
					}
				}
			}
		}

		#endregion

		#region Columns

		private TableColumns _columns;
		public TableColumns Columns { get { return _columns; } }

		internal void ColumnChanged()
		{
			// NOTE: This control is not optimized for dynamic row/column changes
			if (_updateCount == 0)
			{
				Invalidate(false);
				PerformLayout();
			}
		}

		#endregion

		#region Rows

		private TableRows _rows;
		public TableRows Rows { get { return _rows; } }

		public event GetTableValueHandler OnGetValue;

		protected virtual string GetValue(Point cell)
		{
			if (OnGetValue != null)
				return OnGetValue(this, cell);
			return String.Empty;
		}

		internal void RowChanged()
		{
			// NOTE: This control is not optimized for dynamic row/column changes
			if (_updateCount == 0)
			{
				Invalidate(false);
				PerformLayout();
			}
		}

		#endregion
	}

	public delegate string GetTableValueHandler(Table ATable, Point ACell);

	public delegate IElementDesigner GetTableDesignerHandler(Table ATable, Point ACell);
	
	public delegate bool GetTableDesignerRequiredHandler(Table ATable, Point ACell, Graphics AGraphics, Rectangle ABounds);

	public class TableColumns : NotifyingBaseList<TableColumn>
	{
		public TableColumns(Table table)
		{
			_table = table;
		}

		private Table _table;

		public TableColumn this[string name]
		{
			get
			{
				for (int i = 0; i < Count; i++)
					if (this[i].Name == name)
						return this[i];
				return null;
			}
		}

		protected override void Adding(TableColumn tempValue, int index)
		{
			base.Adding(tempValue, index);
			_table.ColumnChanged();
		}

		protected override void Removing(TableColumn tempValue, int index)
		{
			base.Removing(tempValue, index);
			_table.ColumnChanged();
		}
	}

	public class TableColumn
	{
		public TableColumn(Table table)
		{
			_table = table;
		}

		private Table _table;
		public Table Table { get { return _table; } }

		private string _name = String.Empty;
		public string Name
		{
			get { return _name; }
			set { _name = (value == null ? String.Empty : value); }
		}

		private string _title = String.Empty;
		public string Title
		{
			get { return _title; }
			set 
			{ 
				if (value != _title)
				{
					_title = (value == null ? String.Empty : value);
					_table.ColumnChanged();
				}
			}
		}

		internal int _width = 100;
		public int Width
		{
			get { return _width; }
			set
			{
				if (value != _width)
				{
					_width = value;
					_table.ColumnChanged();
				}
			}
		}

		private bool _autoSize = true;
		public bool AutoSize
		{
			get { return _autoSize; }
			set
			{
				if (value != _autoSize)
				{
					_autoSize = value;
					_table.ColumnChanged();
				}
			}
		}
	}

	public class TableRows : List
	{
		public TableRows(Table table)
		{
			_table = table;
		}

		private Table _table;

		protected override void Adding(object tempValue, int index)
		{
			base.Adding(tempValue, index);
			_table.RowChanged();
		}

		protected override void Removing(object tempValue, int index)
		{
			base.Removing(tempValue, index);
			_table.RowChanged();
		}
	}
}
