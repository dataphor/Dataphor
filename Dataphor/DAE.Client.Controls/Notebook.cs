/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	public class Notebook : Control
	{
		public const TabAlignment DefaultTabAlignment = TabAlignment.Top;
		public const int DefaultTabPadding = 4;
		public const int ScrollerOffset = 8;
		public const int InitialPagesCapacity = 16;

		public Notebook()
		{
			SuspendLayout();
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.CacheText, false);
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			TabStop = true;
			ResumeLayout(false);
			_tabColor = BackColor;
			BackColor = Color.Transparent;
			InitializePainting();
			_pages = new PageCollection(this);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			DisposePainting();
		}

		#region Selection

		private NotebookPage _selected;
		public NotebookPage Selected
		{
			get { return _selected; }
			set
			{
				// Ensure that there is a selection if there is at least one tab
				if ((value == null) && (Pages.Count > 0))
					value = (NotebookPage)Pages[0];

				if (_selected != value)
				{
					SelectionChanging(value);
					_selected = value;
					SuspendLayout();
					try
					{
						// Show the active page
						if (_selected != null)
							_selected.Visible = true;

						// Hide the inactive pages
						foreach (NotebookPage page in Pages)
							if (page != _selected)
								page.Visible = false;
					}
					finally
					{
						ResumeLayout(true);
					}
					ScrollIntoView(_selected);
					SelectionChanged();
				}
			}
		}

		public event EventHandler OnSelectionChanged;

		protected virtual void SelectionChanged()
		{
			if (OnSelectionChanged != null)
				OnSelectionChanged(this, EventArgs.Empty);
		}

		public event NotebookTabChangeHandler OnSelectionChanging;

		protected virtual void SelectionChanging(NotebookPage page)
		{
			if (OnSelectionChanging != null)
				OnSelectionChanging(this, page) ;
		}

		internal void UpdateSelection()
		{
			// Clear the selection if the selected control is no longer a child of this control
			if ((_selected != null) && (_selected.Parent != this))
				Selected = null;
			else
			{
				// Ensure that there is a selection if there is at least one tab
				if ((_selected == null) && (Pages.Count > 0))
					Selected = (NotebookPage)Pages[0];
			}
		}

		/// <summary> Makes the next tab the selected one. </summary>
		/// <returns> True if the selection changed. </returns>
		public bool SelectNextPage()
		{
			if (_selected != null)
			{
				int index = Pages.IndexOf(_selected);
				if (index >= 0)
				{
					if ((index < (Pages.Count - 1)))
						Selected = (NotebookPage)Pages[index + 1];
					else
						Selected = (NotebookPage)Pages[0];
					return true;
				}
			}
			return false;
		}

		/// <summary> Makes the prior tab the selected one. </summary>
		/// <returns> True if the selection changed. </returns>
		public bool SelectPriorPage()
		{
			if (_selected != null)
			{
				int index = Pages.IndexOf(_selected);
				if (index > 0)
					Selected = (NotebookPage)Pages[index - 1];
				else
					Selected = (NotebookPage)Pages[Pages.Count - 1];
				return true;
			}
			return false;
		}

		#endregion

		#region Appearance

		private int _tabAreaHeight;

		private int _tabPadding = DefaultTabPadding;
		public int TabPadding
		{
			get { return _tabPadding; }
			set
			{
				if (_tabPadding != value)
				{
					if (_tabPadding < 0)
						throw new ArgumentOutOfRangeException("FTabPadding");
					_tabPadding = value;
					UpdateTabAreaHeight();
					PerformLayout();
					Invalidate(false);
				}
			}
		}

		protected override void OnFontChanged(EventArgs args)
		{
			base.OnFontChanged(args);
			UpdateTabAreaHeight();
			PerformLayout();
		}

		protected override void CreateHandle()
		{
			base.CreateHandle();
			UpdateTabAreaHeight();
		}

		private void UpdateTabAreaHeight()
		{
			_tabAreaHeight = _tabPadding + Font.Height;
		}

		private TabAlignment _tabAlignment = DefaultTabAlignment;
		public TabAlignment TabAlignment
		{
			get { return _tabAlignment; }
			set
			{
				if (_tabAlignment != value)
				{
					if ((value != TabAlignment.Top))
						throw new ControlsException(ControlsException.Codes.InvalidTabAlignment, value.ToString());
					_tabAlignment = value;
					Invalidate();
					PerformLayout(this, "TabAlignment");
				}
			}
		}

		private Color _tabColor;
		public Color TabColor
		{
			get { return _tabColor; }
			set
			{
				if (_tabColor != value)
				{
					_tabColor = value;
					InvalidateTabs();
				}
			}
		}

		private Color _tabOriginColor = Color.Wheat;
		public Color TabOriginColor
		{
			get { return _tabOriginColor; }
			set
			{
				if (_tabOriginColor != value)
				{
					_tabOriginColor = value;
					InvalidateTabs();
				}
			}
		}
		
		private Color _lineColor = Color.Gray;
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				if (_lineColor != value)
				{
					_lineColor = value;
					Invalidate();
				}
			}
		}

		private Color _bodyColor = Color.Gray;
		public Color BodyColor
		{
			get { return _bodyColor; }
			set
			{
				if (_bodyColor != value)
				{
					_bodyColor = value;
					Invalidate();
				}
			}
		}

		#endregion

		#region Painting

		private Bitmap _leftScrollerBitmap;
		private Bitmap _rightScrollerBitmap;

		private void InitializePainting()
		{
			_leftScrollerBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			_rightScrollerBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			_rightScrollerBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
		}

		private void DisposePainting()
		{
			if (_leftScrollerBitmap != null)
			{
				_leftScrollerBitmap.Dispose();
				_leftScrollerBitmap = null;
			}
			if (_rightScrollerBitmap != null)
			{
				_rightScrollerBitmap.Dispose();
				_rightScrollerBitmap = null;
			}
		}

		private static StringFormat _stringFormat;
		private static StringFormat GetStringFormat()
		{
			if (_stringFormat == null)
			{
				_stringFormat = new StringFormat();
				_stringFormat.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
			}
			return _stringFormat;
		}

		private int GetRounding()
		{
			return _tabAreaHeight / 3;
		}

		protected virtual void PaintTabText(Graphics graphics, Rectangle bounds, NotebookPage page, StringFormat format)
		{
			if (page.Enabled)
				using (Brush textBrush = new SolidBrush(ForeColor))
				{
					graphics.DrawString(page.Text, Font, textBrush, bounds, format);
				}
			else
				ControlPaint.DrawStringDisabled(graphics, page.Text, Font, SystemColors.InactiveCaptionText, bounds, format);
		}

		protected virtual void PaintTab(Graphics graphics, int offset, NotebookPage page, int tabWidth, bool isActive)
		{
			Rectangle bounds = base.DisplayRectangle;
			StringFormat format = GetStringFormat();
			int rounding = GetRounding();
			if (_tabAlignment == TabAlignment.Top)
			{
				using (GraphicsPath path = new GraphicsPath())
				{
					// Create path for tab fill
					path.AddBezier(offset, _tabAreaHeight, offset + (_tabAreaHeight / 2), _tabAreaHeight / 2, offset + (_tabAreaHeight / 2), 0, offset + _tabAreaHeight, 0);
					path.AddLine(offset + _tabAreaHeight, 0, offset + (tabWidth - rounding), 0);
					path.AddBezier(offset + (tabWidth - rounding), 0, offset + tabWidth, 0, offset + tabWidth, 0, offset + tabWidth, rounding);
					path.AddLine(offset + tabWidth, rounding, offset + tabWidth, _tabAreaHeight);
					path.CloseFigure();

					// Fill the tab
					using (Brush brush = new LinearGradientBrush(new Rectangle(0, 0, bounds.Width, _tabAreaHeight), _tabOriginColor, _tabColor, 90f, true))
					{
						graphics.FillPath(brush, path);
					}

					using (Pen pen = new Pen(_lineColor))
					{
						// Draw the tab border line
						graphics.DrawBezier(pen, offset, _tabAreaHeight, offset + (_tabAreaHeight / 2), _tabAreaHeight / 2, offset + (_tabAreaHeight / 2), 0, offset + _tabAreaHeight, 0);
						graphics.DrawLine(pen, offset + _tabAreaHeight, 0, offset + (tabWidth - rounding), 0);
						graphics.DrawBezier(pen, offset + (tabWidth - rounding), 0, offset + tabWidth, 0, offset + tabWidth, 0, offset + tabWidth, rounding);
						graphics.DrawLine(pen, offset + tabWidth, rounding, offset + tabWidth, _tabAreaHeight);

						// Draw the highlight
						pen.Color = Color.White;
						graphics.DrawBezier(pen, 1 + offset, _tabAreaHeight, 1 + offset + (_tabAreaHeight / 2), 1 + (_tabAreaHeight / 2), 1 + offset + (_tabAreaHeight / 2), 1, 1 + offset + _tabAreaHeight, 1);
						graphics.DrawLine(pen, 1 + offset + _tabAreaHeight, 1, offset + (tabWidth - rounding), 1);
						graphics.DrawBezier(pen, offset + (tabWidth - rounding), 1, (offset + tabWidth) - 1, 1, (offset + tabWidth) - 1, 1, (offset + tabWidth) - 1, rounding / 2);
					}
				}

				PaintTabText(graphics, new Rectangle((offset + _tabAreaHeight) - (_tabPadding / 2), (_tabAreaHeight - Font.Height) / 2, tabWidth, Font.Height), page, format);
			}
			else
			{
				// TODO: Paint TabAlignment = Bottom
			}
		}

		protected virtual int GetTabOverlap()
		{
			return GetRounding() + (_tabPadding / 2);
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			Rectangle bounds = base.DisplayRectangle;
			bounds.Width--;
			bounds.Height--;

			Rectangle tabBounds;
			Rectangle bodyBounds;
			int tabLineY;
			int otherLineY;
			if (_tabAlignment == TabAlignment.Top)
			{
				tabBounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width, _tabAreaHeight);
				bodyBounds = new Rectangle(bounds.Left, bounds.Top + _tabAreaHeight, bounds.Width, bounds.Height - _tabAreaHeight);
				tabLineY = _tabAreaHeight;
				otherLineY = bodyBounds.Bottom;
			}
			else
			{
				tabBounds = new Rectangle(bounds.Left, bounds.Bottom - _tabAreaHeight, bounds.Width, bounds.Bottom);
				bodyBounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height - _tabAreaHeight);
				tabLineY = bodyBounds.Bottom;
				otherLineY = bounds.Top;
			}

			// Paint the body background
			using (Brush brush = new SolidBrush(_bodyColor))
			{
				args.Graphics.FillRectangle(brush, bodyBounds);
			}

			int xOffset = tabBounds.Left + ScrollerOffset;
			int selectedOffset = -1;
			int selectedWidth = 0;
			int tabOverlap = GetTabOverlap();
			int[] tabWidths = GetTabWidths(args.Graphics);

			// Calculate the selected tab's offset and width
			for (int i = _scrollOffset; (i < Pages.Count) && (xOffset < tabBounds.Right); i++)
			{
				int tabWidth = tabWidths[i];
				if (Pages[i] == _selected)
				{
					// Leave room for but do not paint the selected tab
					selectedWidth = tabWidth;
					selectedOffset = xOffset;	
				}
				xOffset += tabWidth - tabOverlap;
			}

			if (args.Graphics.IsVisible(tabBounds))
			{
				GraphicsState state = args.Graphics.Save();
				args.Graphics.SetClip(new Rectangle(tabBounds.Left + ScrollerOffset, tabBounds.Top, tabBounds.Width - (ScrollerOffset * 2), tabBounds.Height), CombineMode.Intersect);

				// Paint the unselected tabs
				xOffset = tabBounds.Left + ScrollerOffset;
				for (int i = _scrollOffset; (i < Pages.Count) && (xOffset < tabBounds.Right); i++)
				{
					int tabWidth = tabWidths[i];
					if (Pages[i] != _selected)
						PaintTab(args.Graphics, xOffset, (NotebookPage)Pages[i], tabWidth, false);
					xOffset += tabWidth - tabOverlap;
				}

				// Paint the selected tab
				if (selectedOffset >= 0)
				{
					PaintTab(args.Graphics, selectedOffset, _selected, selectedWidth, true);
					if (Focused)
						ControlPaint.DrawFocusRectangle(args.Graphics, new Rectangle(selectedOffset + _tabAreaHeight, tabBounds.Y + 2, selectedWidth - _tabAreaHeight - GetRounding(), tabBounds.Height - 4));
				}

				args.Graphics.Restore(state);

				// Draw the left/right scrollers
				if (_scrollOffset > 0)
					args.Graphics.DrawImage(_leftScrollerBitmap, tabBounds.Left, tabBounds.Top + ((tabBounds.Height - _leftScrollerBitmap.Height) / 2), _leftScrollerBitmap.Width, _leftScrollerBitmap.Height);
				if (xOffset > (tabBounds.Width - ScrollerOffset))
					args.Graphics.DrawImage(_rightScrollerBitmap, tabBounds.Right - ScrollerOffset, tabBounds.Top + ((tabBounds.Height - _rightScrollerBitmap.Height) / 2), _rightScrollerBitmap.Width, _rightScrollerBitmap.Height);
			}
			// Paint the border lines
			using (Pen pen = new Pen(_lineColor))
			{
				if (selectedOffset >= 0)
				{
					args.Graphics.DrawLine(pen, bounds.Left, tabLineY, selectedOffset, tabLineY);
					args.Graphics.DrawLine(pen, selectedOffset + selectedWidth, tabLineY, bounds.Right, tabLineY);
				}
				else
					args.Graphics.DrawLine(pen, bounds.Left, tabLineY, bounds.Right, tabLineY);
				args.Graphics.DrawLine(pen, bounds.Left, tabLineY, bounds.Left, otherLineY);
				args.Graphics.DrawLine(pen, bounds.Left, otherLineY, bounds.Right, otherLineY);
				args.Graphics.DrawLine(pen, bounds.Right, otherLineY, bounds.Right, tabLineY);
			}
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			InvalidateTabs();
		}

		protected override void OnLostFocus(EventArgs args)
		{
			base.OnLostFocus(args);
			InvalidateTabs();
		}

		private void InvalidateTabs()
		{
			Rectangle bounds = GetTabBounds();
			bounds.Height++;
			if (_tabAlignment == TabAlignment.Bottom)
				bounds.Y--;
			Invalidate(bounds, false);
		}

		#endregion

		#region Layout

		private int _scrollOffset;
		public int ScrollOffset
		{
			get { return _scrollOffset; }
			set
			{
				if (value < 0)
					value = 0;
				if (value >= Pages.Count)
					value = Math.Max(Pages.Count - 1, 0);

				if (_scrollOffset != value)
				{
					_scrollOffset = value;
					InvalidateTabs();
					MinimizeScrolling();
				}
			}
		}

		private void MinimizeScrolling()
		{
			int[] tabWidths = GetTabWidths();
			// Minimize scrolling
			if (tabWidths.Length > 0)
			{
				int tabWidth = GetTabBounds().Width - (ScrollerOffset * 2);
				int tabOverlap = GetTabOverlap();
				int accumulated = tabWidths[tabWidths.Length - 1];

				// Don't scroll above the LMax offset, doing so would just waste tab space
				int max;
				for (max = tabWidths.Length - 1; (max > 0) && ((accumulated + (tabWidths[max - 1] - tabOverlap)) <= tabWidth); max--)
					accumulated += tabWidths[max - 1] - tabOverlap;

				ScrollOffset = Math.Min(ScrollOffset, max);
			}
			else
				ScrollOffset = 0;
		}

		public override System.Drawing.Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				if (_tabAlignment == TabAlignment.Top)
				{
					bounds.Y += _tabAreaHeight;
					bounds.Height -= _tabAreaHeight;
				}
				else
					bounds.Height -= _tabAreaHeight;
				bounds.Inflate(-2, -2);
				return bounds;
			}
		}

		protected virtual int GetTabWidth(Graphics graphics, NotebookPage page)
		{
			return (_tabAreaHeight / 4) + _tabAreaHeight + Size.Ceiling(graphics.MeasureString(page.Text, Font, PointF.Empty, GetStringFormat())).Width;
		}

		private int[] GetTabWidths(Graphics graphics)
		{
			// Calculate the tab widths
			int[] result = new int[Pages.Count];
			for (int i = 0; i < Pages.Count; i++)
				result[i] = GetTabWidth(graphics, (NotebookPage)Pages[i]);
			return result;
		}

		private int[] GetTabWidths()
		{
			using (Graphics graphics = this.CreateGraphics())
			{
				return GetTabWidths(graphics);
			}
		}

		private Rectangle GetTabBounds()
		{
			Rectangle bounds = base.DisplayRectangle;
			if (_tabAlignment == TabAlignment.Top)
				return new Rectangle(bounds.Left, bounds.Top, bounds.Width, _tabAreaHeight);
			else
				return new Rectangle(bounds.Left, bounds.Bottom - _tabAreaHeight, bounds.Width, bounds.Bottom);
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			if (!Disposing && IsHandleCreated)
			{
				base.OnLayout(args);
				
				// Layout the selected page
				if (_selected != null)
					_selected.Bounds = DisplayRectangle;

				MinimizeScrolling();
			}
		}

		public void ScrollRight()
		{
			ScrollOffset++;
		}

		public void ScrollLeft()
		{
			ScrollOffset--;
		}

		/// <summary> Attempts to scroll the specified page into view. </summary>
		/// <remarks> If the given page is not a valid child page, nothing happens. </remarks>
		public void ScrollIntoView(NotebookPage page)
		{
			int[] tabWidths = GetTabWidths();
			int pageIndex = Pages.IndexOf(page);
			if (pageIndex >= 0)
			{
				int tabWidth = GetTabBounds().Width - (ScrollerOffset * 2);
				int tabOverlap = GetTabOverlap();
				int accumulated = tabWidths[pageIndex];

				// Don't scroll below LMin offset, doing so would hide the specified page
				int min;
				for (min = pageIndex; (min > 0) && ((accumulated + (tabWidths[min - 1] - tabOverlap)) <= tabWidth); min--)
					accumulated += tabWidths[min - 1] - tabOverlap;

				ScrollOffset = Math.Min(pageIndex, Math.Max(ScrollOffset, min));
			}
		}

		#endregion

		#region Mouse

		protected override void OnMouseDown(MouseEventArgs args)
		{
			base.OnMouseDown(args);

			Focus();
			
			Rectangle tabBounds = GetTabBounds();
			if (tabBounds.Contains(args.X, args.Y))
			{
				if (args.X < ScrollerOffset)
					ScrollLeft();
				else if (args.X > (tabBounds.Right - ScrollerOffset))
					ScrollRight();
				else
				{
					NotebookPage page = GetTabAt(new Point(args.X, args.Y));
					if (page != null)
						Selected = page;
				}
			}
		}
 
		protected virtual NotebookPage GetTabAt(Point location)
		{
			Rectangle tabBounds = GetTabBounds();
			tabBounds.Inflate(-ScrollerOffset, 0);
			if (tabBounds.Contains(location))
			{
				int xOffset = tabBounds.Left;
				int tabOverlap = GetTabOverlap();
				int[] tabWidths = GetTabWidths();
				for (int i = _scrollOffset; (i < tabWidths.Length) && (xOffset <= tabBounds.Right); i++)
				{
					int width = tabWidths[i];
					if ((location.X >= xOffset) && (location.X <= (xOffset + width)))
						return (NotebookPage)Pages[i];
					xOffset += (width - tabOverlap);
				}
			}
			return null;
		}

		#endregion

		#region Keyboard

		protected override bool ProcessMnemonic(char charCode)
		{
			if (this.Enabled)
				foreach (NotebookPage page in Pages)
				{
					if (Control.IsMnemonic(charCode, page.Text))
					{
						Selected = page;
						return true;
					}
				}
			return base.ProcessMnemonic(charCode);
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			switch (key)
			{
				case Keys.Control | Keys.Tab :
					Focus();
					if (SelectNextPage())
						return true;
					break;
				case Keys.Shift | Keys.Control | Keys.Tab :
					Focus();
					if (SelectPriorPage())
						return true;
					break;
				case Keys.Left :
					if (Focused)
						if (SelectPriorPage())
							return true;
					break;
				case Keys.Right :
					if (Focused)
						if (SelectNextPage())
							return true;
					break;
				case Keys.Control | Keys.Right :
					if (Focused)
					{
						ScrollRight();
						return true;
					}
					break;
				case Keys.Control | Keys.Left :
					if (Focused)
					{
						ScrollLeft();
						return true;
					}
					break;
			}
			return base.ProcessDialogKey(key);
		}

		#endregion

		#region Pages & Controls

		private PageCollection _pages;
		public PageCollection Pages
		{
			get { return _pages; }
		}

		// Maintain a correctly ordered list of pages (the Controls list is reordered as controls are made visible/invisible)
		public class PageCollection : IList, ICollection, IEnumerable
		{
			public PageCollection(Notebook notebook)
			{
				_notebook = notebook;
			}

			private Notebook _notebook;

			private void UpdateNotebook()
			{
				_notebook.UpdateSelection();
				_notebook.InvalidateTabs();
			}

			private NotebookPage[] _pages = new NotebookPage[InitialPagesCapacity];

			private int _count;
			public int Count { get { return _count; } }
		
			public NotebookPage this[int index]
			{
				get 
				{ 
					if (index >= _count)
						throw new ArgumentOutOfRangeException("this");
					return _pages[index]; 
				}
				set 
				{
					RemoveAt(index);
					Insert(index, value);
				}
			}

			private bool _updatingControls;

			private void InternalAdd(int index, NotebookPage page)
			{
				page.Visible = false;
				page.TextChanged += new EventHandler(PageTextChanged);
				page.EnabledChanged += new EventHandler(PageEnabledChanged);

				// Grow the capacity of FPages if necessary
				if (_count == _pages.Length)
				{
					NotebookPage[] pages = new NotebookPage[Math.Min(_pages.Length * 2, _pages.Length + 512)];
					Array.Copy(_pages, pages, _pages.Length);
					_pages = pages;
				}
				
				// Shift the items
				Array.Copy(_pages, index, _pages, index + 1, _count - index);

				// Set the inserted item
				_pages[index] = page;
				_count++;

				UpdateNotebook();
			}

			public int Add(NotebookPage page)
			{
				_updatingControls = true;
				try
				{
					_notebook.Controls.Add(page);
				}
				finally
				{
					_updatingControls = false;
				}
				InternalAdd(_count, page);
				return _count - 1;
			}

			internal void ControlAdd(NotebookPage page)
			{
				if (!_updatingControls)
					InternalAdd(_count, page);
			}

			public void Insert(int index, NotebookPage page)
			{
				_updatingControls = true;
				try
				{
					_notebook.Controls.Add(page);
				}
				finally
				{
					_updatingControls = false;
				}
				InternalAdd(index, page);
			}

			public void AddRange(NotebookPage[] pages)
			{
				foreach (NotebookPage page in pages)
					Add(page);
			}

			public void Clear()
			{
				while (_count > 0)
					RemoveAt(_count - 1);
			}

			public bool Contains(NotebookPage page)
			{
				return IndexOf(page) >= 0;
			}

			public int IndexOf(NotebookPage page)
			{
				for (int i = 0; i < _count; i++)
					if (_pages[i] == page)
						return i;
				return -1;
			}

			private NotebookPage InternalRemoveAt(int index)
			{
				if (index >= _count)
					throw new ArgumentOutOfRangeException("AIndex");

				NotebookPage result = _pages[index];

                _count--;
                Array.Copy(_pages, index + 1, _pages, index, _count - index);

				result.TextChanged -= new EventHandler(PageTextChanged);
				result.EnabledChanged -= new EventHandler(PageEnabledChanged);
				
				UpdateNotebook();

				return result;
			}

			public void RemoveAt(int index)
			{
				NotebookPage result = InternalRemoveAt(index);
				_updatingControls = true;
				try
				{
					_notebook.Controls.Remove(result);
				}
				finally
				{
					_updatingControls = false;
				}
			}

			internal void ControlRemove(NotebookPage page)
			{
				if (!_updatingControls)
					InternalRemoveAt(IndexOf(page));
			}

			public void Remove(NotebookPage page)
			{
				InternalRemoveAt(IndexOf(page));
				_updatingControls = true;
				try
				{
					_notebook.Controls.Remove(page);
				}
				finally
				{
					_updatingControls = false;
				}
			}

			public IEnumerator GetEnumerator()
			{
				return new PageCollectionEnumerator(this);
			}

			public class PageCollectionEnumerator : IEnumerator
			{
				public PageCollectionEnumerator(PageCollection collection)
				{
					_collection = collection;
				}

				private PageCollection _collection;
				private int _index = -1;

				public void Reset()
				{
					_index = -1;
				}

				object IEnumerator.Current
				{
					get { return _collection[_index]; }
				}

				public NotebookPage Current
				{
					get { return _collection[_index]; }
				}

				public bool MoveNext()
				{
					_index++;
					return _index < _collection.Count;
				}
			}

			#region ICollection / IList

			void ICollection.CopyTo(Array target, int index)
			{
				for (int i = 0; i < _count; i++)
					target.SetValue(_pages[i], i + index);
			}

			bool ICollection.IsSynchronized
			{
				get { return false; }
			}

			object ICollection.SyncRoot
			{
				get { return this; }
			}

			int IList.Add(object value)
			{
				return this.Add((NotebookPage)value);
			}

			bool IList.Contains(object page)
			{
				return this.Contains((NotebookPage)page);
			}

			bool IList.IsFixedSize { get { return false; } }

			object IList.this[int index] 
			{ 
				get { return this[index]; } 
				set { this[index] = (NotebookPage)value; }
			}

			int IList.IndexOf(object page)
			{
				return IndexOf((NotebookPage)page);
			}

			void IList.Insert(int index, object value)
			{
				Insert(index, (NotebookPage)value);
			}

			void IList.Remove(object value)
			{
				Remove((NotebookPage)value);
			}

			public bool IsReadOnly { get { return false; } }

			private void PageTextChanged(object sender, EventArgs args)
			{
				_notebook.InvalidateTabs();
			}

			private void PageEnabledChanged(object sender, EventArgs args)
			{
				_notebook.InvalidateTabs();
			}

			#endregion
		}

		protected override System.Windows.Forms.Control.ControlCollection CreateControlsInstance()
		{
			return new NotebookControlCollection(this);
		}

		public class NotebookControlCollection : Control.ControlCollection
		{
			public NotebookControlCollection(Notebook notebook) : base(notebook)
			{
				_notebook = notebook;
			}

			private Notebook _notebook;

			public override void Add(Control control)
			{
				if (!(control is NotebookPage))
					throw new ControlsException(ControlsException.Codes.InvalidNotebookChild, (control == null ? "<null>" : control.GetType().Name));
				_notebook.Pages.ControlAdd((NotebookPage)control);
				base.Add(control);
			}

			public override void Remove(Control control)
			{
				base.Remove(control);
				_notebook.Pages.ControlRemove((NotebookPage)control);
			}
		}

		#endregion
	}

	public class NotebookPage : Control
	{
		public NotebookPage()
		{
			SetStyle(ControlStyles.Selectable, false);
			SetStyle(ControlStyles.CacheText, true);
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.DoubleBuffer, false);
			SetStyle(ControlStyles.Opaque, false);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Color.Transparent;
		}
	}

	public delegate void NotebookTabChangeHandler(Notebook ANotebook, NotebookPage APage);
}
