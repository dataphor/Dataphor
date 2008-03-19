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
		public const int CMinAutoSize = 80;	// Minimum auto-size (after padding)

		public Table() : base()
		{
			SetStyle(ControlStyles.ResizeRedraw, false);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.Selectable, false);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			TabStop = false;

			FColumns = new TableColumns(this);
			FRows = new TableRows(this);
			FRowHeight = Font.Height + 8;
			FHeaderFont = new Font(this.Font, FontStyle.Bold);
			FHeaderForeColor = Color.Black;
			FHeaderHeight = FHeaderFont.Height + 8;

			SuspendLayout();
			try
			{
				FHScrollBar = new HScrollBar();
				FHScrollBar.SmallChange = 1;
				FHScrollBar.LargeChange = 1;
				FHScrollBar.Minimum = 0;
				FHScrollBar.TabStop = false;
				FHScrollBar.Scroll += new ScrollEventHandler(HScrollBarScrolled);
				Controls.Add(FHScrollBar);

				FVScrollBar = new VScrollBar();
				FVScrollBar.SmallChange = 1;
				FVScrollBar.Minimum = 0;
				FVScrollBar.TabStop = false;
				FVScrollBar.Scroll += new ScrollEventHandler(VScrollBarScrolled);
				Controls.Add(FVScrollBar);
			}
			finally
			{
				ResumeLayout(false);
			}
		}

		#region Cosmetic Properties

		private int FRowHeight;
		public int RowHeight
		{
			get { return FRowHeight; }
			set
			{
				if (value < 1)
					value = 1;
				if (FRowHeight != value)
				{
					FRowHeight = value;
					PerformLayout();
					Invalidate(false);
				}
			}
		}

		private int FHeaderHeight;
		public int HeaderHeight
		{
			get { return FHeaderHeight; }
			set
			{
				if (value < 1)
					value = 1;
				if (FHeaderHeight != value)
				{
					FHeaderHeight = value;
					PerformLayout();
					Invalidate(false);
				}
			}
		}

		private Font FHeaderFont;
		public Font HeaderFont
		{
			get { return FHeaderFont; }
			set 
			{ 
				FHeaderFont = value; 
				InvalidateHeader();
			}
		}

		private Color FHeaderForeColor;
		public Color HeaderForeColor
		{
			get { return FHeaderForeColor; }
			set
			{
				FHeaderForeColor = value;
				InvalidateHeader();
			}
		}

		private Color FShadowColor = Color.FromArgb(180, Color.Black);
		public Color ShadowColor
		{
			get { return FShadowColor; }
			set
			{
				FShadowColor = value;
				InvalidateHeader();
			}
		}

		private Color FLineColor = Color.White;
		public Color LineColor
		{
			get { return FLineColor; }
			set
			{
				FLineColor = value;
				InvalidateHeader();
			}
		}

		private int FHorizontalPadding = 2;
		public int HorizontalPadding
		{
			get { return FHorizontalPadding; }
			set 
			{ 
				FHorizontalPadding = value;
				Invalidate(false);
				PerformLayout();
			}
		}

		private void InvalidateHeader()
		{
			Rectangle LBounds = base.DisplayRectangle;
			LBounds.Height = FHeaderHeight;
			Invalidate(LBounds, false);
		}

		#endregion

		#region Scrolling & Layout

		private HScrollBar FHScrollBar;
		private VScrollBar FVScrollBar;

		private int FVisibleRows;

		private void HScrollBarScrolled(object ASender, ScrollEventArgs AArgs)
		{
			NavigateTo(new Point(AArgs.NewValue, FLocation.Y));
		}

		private void VScrollBarScrolled(object ASender, ScrollEventArgs AArgs)
		{
			NavigateTo(new Point(FLocation.X, AArgs.NewValue));
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			switch (AKey)
			{
				case Keys.Left :
					NavigateTo(new Point(FLocation.X - 1, FLocation.Y));
					break;
				case Keys.Up :
					NavigateTo(new Point(FLocation.X, FLocation.Y - 1));
					break;
				case Keys.Right :
					NavigateTo(new Point(FLocation.X + 1, FLocation.Y));
					break;
				case Keys.Down :
					NavigateTo(new Point(FLocation.X, FLocation.Y + 1));
					break;
				default :
					return base.ProcessDialogKey(AKey);
			}
			return true;
		}

		private static int Constrain(int AValue, int AMin, int AMax)
		{
			if (AValue > AMax)
				AValue = AMax;
			if (AValue < AMin)
				AValue = AMin;
			return AValue;
		}

		private Point FLocation;

		private void NavigateTo(Point ALocation)
		{
			ALocation.X = Constrain(ALocation.X, FHScrollBar.Minimum, FHScrollBar.Maximum);
			ALocation.Y = Constrain(ALocation.Y, FVScrollBar.Minimum, FVScrollBar.Maximum);
			if (ALocation != FLocation)
			{
				Point LDelta = new Point(SumWidths(FLocation.X, ALocation.X), (FLocation.Y - ALocation.Y) * FRowHeight);

				FLocation = ALocation;
				
				FHScrollBar.Value = FLocation.X;
				FVScrollBar.Value = FLocation.Y;

				// Scroll vertically (seperate from horizontal because it does not affect header
				RECT LRect = UnsafeUtilities.RECTFromRectangle(DataRectangle);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, 0, LDelta.Y, ref LRect, ref LRect, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */);

				// Scroll horizontally
				LRect = UnsafeUtilities.RECTFromRectangle(DisplayRectangle);
				UnsafeNativeMethods.ScrollWindowEx(this.Handle, LDelta.X, 0, ref LRect, ref LRect, IntPtr.Zero, IntPtr.Zero, 2 /* SW_INVALIDATE */);

				PerformLayout();
			}
		}

		private int SumWidths(int AStartIndex, int AEndIndex)
		{
			int LResult = 0;
			int LPolarity = (AStartIndex > AEndIndex ? 1 : -1);
			for (int i = AStartIndex; i != AEndIndex; i -= LPolarity)
				LResult += FColumns[i].Width * LPolarity;
			return LResult;
		}

		/// <summary> The number of columns (starting from the last) that would completely fit in the display area. </summary>
		private int FittingColumns()
		{
			Rectangle LBounds = DataRectangle;
			int LExtentX = LBounds.X;
			int LCount = 0;
			for (int LX = FColumns.Count - 1; LX >= 0; LX--)
			{
				LExtentX += FColumns[LX].Width;
				if (LExtentX <= LBounds.Width)
					LCount++;
				else
					break;
			}
			return LCount;
		}

		public virtual void AutoSizeColumns(int AWidth)
		{
			// Calculate the size (in excess of the min auto size for each auto-sized column
			int[] LSizes = new int[FColumns.Count];
			int LMax;
			int LCurrent;
			int LY;
			using (Graphics LGraphics = CreateGraphics())
			{
				for (int LX = 0; LX < FColumns.Count; LX++)
				{
					LMax = 0;
					if (FColumns[LX].AutoSize)
					{
						for (LY = 0; LY < FRows.Count; LY++)
						{
							LCurrent = Size.Ceiling(LGraphics.MeasureString(GetValue(new Point(LX, LY)), Font)).Width + (FHorizontalPadding * 2);
							if (LCurrent > LMax)
								LMax = LCurrent;
						}
						LSizes[LX] = Math.Max(0, LMax - CMinAutoSize);
					}
				}
			}

			// Sum the column sizes
			int LAutoSum = 0;	// Sum of auto sized columns excesses
			int LFixedSum = 0;	// Sum of fixed sized columns widths
			int LAutoCount = 0;	// Number of auto sized columns
			for (int LX = 0; LX < FColumns.Count; LX++)
			{
				if (FColumns[LX].AutoSize)
				{
					LAutoSum += LSizes[LX];
					LAutoCount++;
				}
				else
					LFixedSum += FColumns[LX].Width;
			}

			int LExcessWidth = Math.Max(0, AWidth - (LFixedSum + (LAutoCount * CMinAutoSize)));

			// Distribute the excess to the scrunched auto-sized columns based on their size proportion over the minumum auto-size
			for (int LX = 0; LX < FColumns.Count; LX++)
				if (FColumns[LX].AutoSize)
					FColumns[LX].FWidth = CMinAutoSize + (LAutoSum == 0 ? 0 : Math.Min(LSizes[LX], (int)(((double)LSizes[LX] / (double)LAutoSum) * LExcessWidth)));
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			// Don't call base

			Rectangle LBounds = DataRectangle;
			
			AutoSizeColumns(LBounds.Width);

			FVisibleRows = (LBounds.Height / FRowHeight) + (((LBounds.Height % FRowHeight) == 0) ? 0 : 1);
			FVScrollBar.LargeChange = Math.Max((FVisibleRows - 1), 1);

			LBounds = base.DisplayRectangle;

			FHScrollBar.Maximum = Math.Max(0, FColumns.Count - Math.Max(FittingColumns(), 1));
			FVScrollBar.Maximum = Math.Max(0, FRows.Count - 1);

			// Scroll off wasted vertical space
			Point LNew = 
				new Point
				(
					Math.Min(FLocation.X, FHScrollBar.Maximum), 
					Math.Max(0, Math.Min(FRows.Count, FLocation.Y + (FVisibleRows - 1)) - (FVisibleRows - 1))
				);
			if (LNew != FLocation)
				NavigateTo(LNew);

			FVScrollBar.Visible = (FVScrollBar.Maximum > 0) && (FVScrollBar.LargeChange <= FVScrollBar.Maximum);
			FHScrollBar.Visible = (FHScrollBar.Maximum > 0);

			FVScrollBar.Bounds =
				new Rectangle
				(
					LBounds.Right - FVScrollBar.Width,
					FHeaderHeight,
					FVScrollBar.Width,
					LBounds.Height - (FHeaderHeight + (FHScrollBar.Visible ? FHScrollBar.Height : 0))
				);

			FHScrollBar.Top = LBounds.Bottom - FHScrollBar.Height;
			FHScrollBar.Width = LBounds.Width - (FVScrollBar.Visible ? FVScrollBar.Width : 0);

			UpdateDesigners();

			// Position the designers
			LBounds = DataRectangle;
			IElementDesigner LDesigner;
			int LExtentX = LBounds.X;
			TableColumn LColumn;
			int LMaxY = Math.Min(FRows.Count, FLocation.Y + FVisibleRows);
			for (int LX = FLocation.X; (LExtentX <= LBounds.Width) && (LX < FColumns.Count); LX++)
			{
				LColumn = FColumns[LX];

				for (int LY = FLocation.Y; LY < LMaxY; LY++)
				{
					LDesigner = (IElementDesigner)FDesigners[new Point(LX, LY)];
					if (LDesigner != null)
						LDesigner.Bounds =
							new Rectangle
							(
								LExtentX + FHorizontalPadding, 
								LBounds.Y + ((LY - FLocation.Y) * FRowHeight), 
								LColumn.Width - (FHorizontalPadding * 2), 
								FRowHeight
							);
				}

				LExtentX += LColumn.Width;
			}
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				if (FVScrollBar.Visible)
					LBounds.Width -= FVScrollBar.Width;
				if (FHScrollBar.Visible)
					LBounds.Height -= FHScrollBar.Height;
				return LBounds;
			}
		}

		public virtual Rectangle DataRectangle
		{
			get
			{
				Rectangle LBounds = DisplayRectangle;
				LBounds.Y += FHeaderHeight;
				LBounds.Height -= FHeaderHeight;
				return LBounds;
			}
		}

		private int FUpdateCount = 0;

		public void BeginUpdate()
		{
			FUpdateCount++;
			SuspendLayout();
		}

		public void EndUpdate()
		{
			FUpdateCount = Math.Max(0, FUpdateCount - 1);
			if (IsHandleCreated && (FUpdateCount == 0))
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
		private Hashtable FDesigners = new Hashtable();

		/// <summary> Reconciles the set of visible designers with the set of visible cells. </summary>
		private void UpdateDesigners()
		{
			if (FUpdateCount == 0)
			{
				// List of unused designers (initially full)
				ArrayList LDesigned = new ArrayList(FDesigners.Count);
				LDesigned.AddRange(FDesigners.Keys);
				IElementDesigner LDesigner;

				SuspendLayout();
				try
				{
					Rectangle LBounds = DataRectangle;
					int LExtentX = LBounds.X;
					TableColumn LColumn;
					int LMaxY = Math.Min(FRows.Count, FLocation.Y + FVisibleRows);
					using (Graphics LGraphics = CreateGraphics())
					{
						for (int LX = FLocation.X; (LExtentX <= LBounds.Width) && (LX < FColumns.Count); LX++)
						{
							LColumn = FColumns[LX];

							for (int LY = FLocation.Y; LY < LMaxY; LY++)
							{
								if
								(
									GetDesignerRequired
									(
										new Point(LX, LY),
										LGraphics,
										new Rectangle
										(
											LExtentX + FHorizontalPadding, 
											LBounds.Y + ((LY - FLocation.Y) * FRowHeight), 
											LColumn.Width - (FHorizontalPadding * 2), 
											FRowHeight
										)
									)
								)
								{
									LDesigner = (IElementDesigner)FDesigners[new Point(LX, LY)];
									if (LDesigner == null)
										LDesigner = AddDesigner(new Point(LX, LY));
									else
										LDesigned.Remove(new Point(LX, LY));
									((Control)LDesigner).TabIndex = LX + (LY * FColumns.Count);
								}
							}

							LExtentX += LColumn.Width;
						}
					}

					// Remove unused designers
					foreach (Point LCell in LDesigned)
						RemoveDesigner(LCell);
				}
				finally
				{
					ResumeLayout(false);
				}
			}
		}

		public event GetTableDesignerHandler OnGetDesigner;

		protected virtual IElementDesigner GetDesigner(Point ACell)
		{
			if (OnGetDesigner != null)
				return OnGetDesigner(this, ACell);
			else
				return null;
		}

		public event GetTableDesignerRequiredHandler OnGetDesignerRequired;

		protected virtual bool GetDesignerRequired(Point ACell, Graphics AGraphics, Rectangle ABounds)
		{
			if (OnGetDesignerRequired != null)
				return OnGetDesignerRequired(this, ACell, AGraphics, ABounds);
			else
				return false;
		}

		private IElementDesigner AddDesigner(Point ACell)
		{
			IElementDesigner LDesigner = GetDesigner(ACell);
			if (LDesigner != null)
			{
				try
				{
					FDesigners.Add(ACell, LDesigner);
					Controls.Add((Control)LDesigner);
				}
				catch
				{
					LDesigner.Dispose();
					throw;
				}
			}
			return LDesigner;
		}

		private void RemoveDesigner(Point ACell)
		{
			IElementDesigner LDesigner = (IElementDesigner)FDesigners[ACell];
			if (LDesigner != null)
			{
				Controls.Remove((Control)LDesigner);
				FDesigners.Remove(ACell);
			}
		}

		#endregion

		#region Painting

		private StringFormat FHeaderStringFormat;
		protected virtual StringFormat GetHeaderStringFormat()
		{
			if (FHeaderStringFormat == null)
			{
				FHeaderStringFormat = new StringFormat();
				FHeaderStringFormat.Trimming = StringTrimming.EllipsisCharacter;
				FHeaderStringFormat.FormatFlags &= ~StringFormatFlags.NoWrap;
			}
			return FHeaderStringFormat;
		}

		private StringFormat FDataStringFormat;
		protected virtual StringFormat GetDataStringFormat()
		{
			if (FDataStringFormat == null)
			{
				FDataStringFormat = new StringFormat();
				FDataStringFormat.Trimming = StringTrimming.EllipsisCharacter;
				FDataStringFormat.FormatFlags &= ~StringFormatFlags.NoWrap;
			}
			return FDataStringFormat;
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);

			Rectangle LBounds = DisplayRectangle;

			using (Pen LPen = new Pen(LineColor))
			{
				using (Pen LShadowPen = new Pen(ShadowColor))
				{
					using (SolidBrush LBrush = new SolidBrush(FHeaderForeColor))
					{
						int LExtentX = LBounds.X;
						TableColumn LColumn;
						int LMaxY = Math.Min(FRows.Count, FLocation.Y + FVisibleRows);
						for (int LX = FLocation.X; (LExtentX <= LBounds.Width) && (LX < FColumns.Count); LX++)
						{
							LColumn = FColumns[LX];

							// Paint the header
							LBrush.Color = FHeaderForeColor;
							AArgs.Graphics.DrawString
							(
								LColumn.Title, 
								FHeaderFont, 
								LBrush, 
								new Rectangle
								(
									LExtentX + FHorizontalPadding, 
									LBounds.Y + ((FHeaderHeight / 2) - (this.Font.Height / 2)),
									LColumn.Width - (FHorizontalPadding * 2), 
									this.Font.Height
								), 
								GetHeaderStringFormat()
							);
							AArgs.Graphics.DrawLine(LPen, new Point(LExtentX, LBounds.Y + FHeaderHeight - 2), new Point(LExtentX + LColumn.Width - 2, LBounds.Y + FHeaderHeight - 2));
							AArgs.Graphics.DrawLine(LShadowPen, new Point(LExtentX + 1, LBounds.Y + FHeaderHeight - 1), new Point(LExtentX + LColumn.Width - 1, LBounds.Y + FHeaderHeight - 1));

							if (LX < (FColumns.Count - 1))
							{
								AArgs.Graphics.DrawLine(LPen, new Point(LExtentX + LColumn.Width - 2, LBounds.Y + FHeaderHeight), new Point(LExtentX + LColumn.Width - 2, LBounds.Y + FHeaderHeight + (LMaxY * FRowHeight) - 1));
								AArgs.Graphics.DrawLine(LShadowPen, new Point(LExtentX + LColumn.Width - 1, LBounds.Y + FHeaderHeight + 1), new Point(LExtentX + LColumn.Width - 1, LBounds.Y + FHeaderHeight + (LMaxY * FRowHeight)));
							}

							LBrush.Color = ForeColor;
							// Paint the data cells
							for (int LY = FLocation.Y; LY < LMaxY; LY++)
							{
								AArgs.Graphics.DrawString
								(
									GetValue(new Point(LX, LY)), 
									this.Font, 
									LBrush, 
									new Rectangle
									(
										LExtentX + FHorizontalPadding, 
										LBounds.Y + FHeaderHeight + (((LY - FLocation.Y) * FRowHeight) + ((FRowHeight / 2) - (this.Font.Height / 2))), 
										LColumn.Width - (FHorizontalPadding * 2), 
										this.Font.Height
									),
									GetDataStringFormat()
								);
							}

							LExtentX += LColumn.Width;
						}
					}
				}
			}
		}

		#endregion

		#region Columns

		private TableColumns FColumns;
		public TableColumns Columns { get { return FColumns; } }

		internal void ColumnChanged()
		{
			// NOTE: This control is not optimized for dynamic row/column changes
			if (FUpdateCount == 0)
			{
				Invalidate(false);
				PerformLayout();
			}
		}

		#endregion

		#region Rows

		private TableRows FRows;
		public TableRows Rows { get { return FRows; } }

		public event GetTableValueHandler OnGetValue;

		protected virtual string GetValue(Point ACell)
		{
			if (OnGetValue != null)
				return OnGetValue(this, ACell);
			return String.Empty;
		}

		internal void RowChanged()
		{
			// NOTE: This control is not optimized for dynamic row/column changes
			if (FUpdateCount == 0)
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

	public class TableColumns : TypedList
	{
		public TableColumns(Table ATable) : base(typeof(TableColumn))
		{
			FTable = ATable;
		}

		private Table FTable;

		public new TableColumn this[int AIndex]
		{
			get { return (TableColumn)base[AIndex]; }
		}

		public TableColumn this[string AName]
		{
			get
			{
				for (int i = 0; i < Count; i++)
					if (this[i].Name == AName)
						return this[i];
				return null;
			}
		}

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			FTable.ColumnChanged();
		}

		protected override void Removing(object AValue, int AIndex)
		{
			base.Removing(AValue, AIndex);
			FTable.ColumnChanged();
		}
	}

	public class TableColumn
	{
		public TableColumn(Table ATable)
		{
			FTable = ATable;
		}

		private Table FTable;
		public Table Table { get { return FTable; } }

		private string FName = String.Empty;
		public string Name
		{
			get { return FName; }
			set { FName = (value == null ? String.Empty : value); }
		}

		private string FTitle = String.Empty;
		public string Title
		{
			get { return FTitle; }
			set 
			{ 
				if (value != FTitle)
				{
					FTitle = (value == null ? String.Empty : value);
					FTable.ColumnChanged();
				}
			}
		}

		internal int FWidth = 100;
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (value != FWidth)
				{
					FWidth = value;
					FTable.ColumnChanged();
				}
			}
		}

		private bool FAutoSize = true;
		public bool AutoSize
		{
			get { return FAutoSize; }
			set
			{
				if (value != FAutoSize)
				{
					FAutoSize = value;
					FTable.ColumnChanged();
				}
			}
		}
	}

	public class TableRows : List
	{
		public TableRows(Table ATable)
		{
			FTable = ATable;
		}

		private Table FTable;

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			FTable.RowChanged();
		}

		protected override void Removing(object AValue, int AIndex)
		{
			base.Removing(AValue, AIndex);
			FTable.RowChanged();
		}
	}
}
