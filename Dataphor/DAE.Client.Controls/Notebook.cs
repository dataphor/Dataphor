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
		public const TabAlignment CDefaultTabAlignment = TabAlignment.Top;
		public const int CDefaultTabPadding = 4;
		public const int CScrollerOffset = 8;
		public const int CInitialPagesCapacity = 16;

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
			FTabColor = BackColor;
			BackColor = Color.Transparent;
			InitializePainting();
			FPages = new PageCollection(this);
		}

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			DisposePainting();
		}

		#region Selection

		private NotebookPage FSelected;
		public NotebookPage Selected
		{
			get { return FSelected; }
			set
			{
				// Ensure that there is a selection if there is at least one tab
				if ((value == null) && (Pages.Count > 0))
					value = (NotebookPage)Pages[0];

				if (FSelected != value)
				{
					SelectionChanging(value);
					FSelected = value;
					SuspendLayout();
					try
					{
						// Show the active page
						if (FSelected != null)
							FSelected.Visible = true;

						// Hide the inactive pages
						foreach (NotebookPage LPage in Pages)
							if (LPage != FSelected)
								LPage.Visible = false;
					}
					finally
					{
						ResumeLayout(true);
					}
					ScrollIntoView(FSelected);
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

		protected virtual void SelectionChanging(NotebookPage APage)
		{
			if (OnSelectionChanging != null)
				OnSelectionChanging(this, APage) ;
		}

		internal void UpdateSelection()
		{
			// Clear the selection if the selected control is no longer a child of this control
			if ((FSelected != null) && (FSelected.Parent != this))
				Selected = null;
			else
			{
				// Ensure that there is a selection if there is at least one tab
				if ((FSelected == null) && (Pages.Count > 0))
					Selected = (NotebookPage)Pages[0];
			}
		}

		/// <summary> Makes the next tab the selected one. </summary>
		/// <returns> True if the selection changed. </returns>
		public bool SelectNextPage()
		{
			if (FSelected != null)
			{
				int LIndex = Pages.IndexOf(FSelected);
				if (LIndex >= 0)
				{
					if ((LIndex < (Pages.Count - 1)))
						Selected = (NotebookPage)Pages[LIndex + 1];
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
			if (FSelected != null)
			{
				int LIndex = Pages.IndexOf(FSelected);
				if (LIndex > 0)
					Selected = (NotebookPage)Pages[LIndex - 1];
				else
					Selected = (NotebookPage)Pages[Pages.Count - 1];
				return true;
			}
			return false;
		}

		#endregion

		#region Appearance

		private int FTabAreaHeight;

		private int FTabPadding = CDefaultTabPadding;
		public int TabPadding
		{
			get { return FTabPadding; }
			set
			{
				if (FTabPadding != value)
				{
					if (FTabPadding < 0)
						throw new ArgumentOutOfRangeException("FTabPadding");
					FTabPadding = value;
					UpdateTabAreaHeight();
					PerformLayout();
					Invalidate(false);
				}
			}
		}

		protected override void OnFontChanged(EventArgs AArgs)
		{
			base.OnFontChanged(AArgs);
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
			FTabAreaHeight = FTabPadding + Font.Height;
		}

		private TabAlignment FTabAlignment = CDefaultTabAlignment;
		public TabAlignment TabAlignment
		{
			get { return FTabAlignment; }
			set
			{
				if (FTabAlignment != value)
				{
					if ((value != TabAlignment.Top))
						throw new ControlsException(ControlsException.Codes.InvalidTabAlignment, value.ToString());
					FTabAlignment = value;
					Invalidate();
					PerformLayout(this, "TabAlignment");
				}
			}
		}

		private Color FTabColor;
		public Color TabColor
		{
			get { return FTabColor; }
			set
			{
				if (FTabColor != value)
				{
					FTabColor = value;
					InvalidateTabs();
				}
			}
		}

		private Color FTabOriginColor = Color.Wheat;
		public Color TabOriginColor
		{
			get { return FTabOriginColor; }
			set
			{
				if (FTabOriginColor != value)
				{
					FTabOriginColor = value;
					InvalidateTabs();
				}
			}
		}
		
		private Color FLineColor = Color.Gray;
		public Color LineColor
		{
			get { return FLineColor; }
			set
			{
				if (FLineColor != value)
				{
					FLineColor = value;
					Invalidate();
				}
			}
		}

		private Color FBodyColor = Color.Gray;
		public Color BodyColor
		{
			get { return FBodyColor; }
			set
			{
				if (FBodyColor != value)
				{
					FBodyColor = value;
					Invalidate();
				}
			}
		}

		#endregion

		#region Painting

		private Bitmap FLeftScrollerBitmap;
		private Bitmap FRightScrollerBitmap;

		private void InitializePainting()
		{
			FLeftScrollerBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			FRightScrollerBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			FRightScrollerBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
		}

		private void DisposePainting()
		{
			if (FLeftScrollerBitmap != null)
			{
				FLeftScrollerBitmap.Dispose();
				FLeftScrollerBitmap = null;
			}
			if (FRightScrollerBitmap != null)
			{
				FRightScrollerBitmap.Dispose();
				FRightScrollerBitmap = null;
			}
		}

		private static StringFormat FStringFormat;
		private static StringFormat GetStringFormat()
		{
			if (FStringFormat == null)
			{
				FStringFormat = new StringFormat();
				FStringFormat.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
			}
			return FStringFormat;
		}

		private int GetRounding()
		{
			return FTabAreaHeight / 3;
		}

		protected virtual void PaintTabText(Graphics AGraphics, Rectangle ABounds, NotebookPage APage, StringFormat AFormat)
		{
			if (APage.Enabled)
				using (Brush LTextBrush = new SolidBrush(ForeColor))
				{
					AGraphics.DrawString(APage.Text, Font, LTextBrush, ABounds, AFormat);
				}
			else
				ControlPaint.DrawStringDisabled(AGraphics, APage.Text, Font, SystemColors.InactiveCaptionText, ABounds, AFormat);
		}

		protected virtual void PaintTab(Graphics AGraphics, int AOffset, NotebookPage APage, int ATabWidth, bool AIsActive)
		{
			Rectangle LBounds = base.DisplayRectangle;
			StringFormat LFormat = GetStringFormat();
			int LRounding = GetRounding();
			if (FTabAlignment == TabAlignment.Top)
			{
				using (GraphicsPath LPath = new GraphicsPath())
				{
					// Create path for tab fill
					LPath.AddBezier(AOffset, FTabAreaHeight, AOffset + (FTabAreaHeight / 2), FTabAreaHeight / 2, AOffset + (FTabAreaHeight / 2), 0, AOffset + FTabAreaHeight, 0);
					LPath.AddLine(AOffset + FTabAreaHeight, 0, AOffset + (ATabWidth - LRounding), 0);
					LPath.AddBezier(AOffset + (ATabWidth - LRounding), 0, AOffset + ATabWidth, 0, AOffset + ATabWidth, 0, AOffset + ATabWidth, LRounding);
					LPath.AddLine(AOffset + ATabWidth, LRounding, AOffset + ATabWidth, FTabAreaHeight);
					LPath.CloseFigure();

					// Fill the tab
					using (Brush LBrush = new LinearGradientBrush(new Rectangle(0, 0, LBounds.Width, FTabAreaHeight), FTabOriginColor, FTabColor, 90f, true))
					{
						AGraphics.FillPath(LBrush, LPath);
					}

					using (Pen LPen = new Pen(FLineColor))
					{
						// Draw the tab border line
						AGraphics.DrawBezier(LPen, AOffset, FTabAreaHeight, AOffset + (FTabAreaHeight / 2), FTabAreaHeight / 2, AOffset + (FTabAreaHeight / 2), 0, AOffset + FTabAreaHeight, 0);
						AGraphics.DrawLine(LPen, AOffset + FTabAreaHeight, 0, AOffset + (ATabWidth - LRounding), 0);
						AGraphics.DrawBezier(LPen, AOffset + (ATabWidth - LRounding), 0, AOffset + ATabWidth, 0, AOffset + ATabWidth, 0, AOffset + ATabWidth, LRounding);
						AGraphics.DrawLine(LPen, AOffset + ATabWidth, LRounding, AOffset + ATabWidth, FTabAreaHeight);

						// Draw the highlight
						LPen.Color = Color.White;
						AGraphics.DrawBezier(LPen, 1 + AOffset, FTabAreaHeight, 1 + AOffset + (FTabAreaHeight / 2), 1 + (FTabAreaHeight / 2), 1 + AOffset + (FTabAreaHeight / 2), 1, 1 + AOffset + FTabAreaHeight, 1);
						AGraphics.DrawLine(LPen, 1 + AOffset + FTabAreaHeight, 1, AOffset + (ATabWidth - LRounding), 1);
						AGraphics.DrawBezier(LPen, AOffset + (ATabWidth - LRounding), 1, (AOffset + ATabWidth) - 1, 1, (AOffset + ATabWidth) - 1, 1, (AOffset + ATabWidth) - 1, LRounding / 2);
					}
				}

				PaintTabText(AGraphics, new Rectangle((AOffset + FTabAreaHeight) - (FTabPadding / 2), (FTabAreaHeight - Font.Height) / 2, ATabWidth, Font.Height), APage, LFormat);
			}
			else
			{
				// TODO: Paint TabAlignment = Bottom
			}
		}

		protected virtual int GetTabOverlap()
		{
			return GetRounding() + (FTabPadding / 2);
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			Rectangle LBounds = base.DisplayRectangle;
			LBounds.Width--;
			LBounds.Height--;

			Rectangle LTabBounds;
			Rectangle LBodyBounds;
			int LTabLineY;
			int LOtherLineY;
			if (FTabAlignment == TabAlignment.Top)
			{
				LTabBounds = new Rectangle(LBounds.Left, LBounds.Top, LBounds.Width, FTabAreaHeight);
				LBodyBounds = new Rectangle(LBounds.Left, LBounds.Top + FTabAreaHeight, LBounds.Width, LBounds.Height - FTabAreaHeight);
				LTabLineY = FTabAreaHeight;
				LOtherLineY = LBodyBounds.Bottom;
			}
			else
			{
				LTabBounds = new Rectangle(LBounds.Left, LBounds.Bottom - FTabAreaHeight, LBounds.Width, LBounds.Bottom);
				LBodyBounds = new Rectangle(LBounds.Left, LBounds.Top, LBounds.Width, LBounds.Height - FTabAreaHeight);
				LTabLineY = LBodyBounds.Bottom;
				LOtherLineY = LBounds.Top;
			}

			// Paint the body background
			using (Brush LBrush = new SolidBrush(FBodyColor))
			{
				AArgs.Graphics.FillRectangle(LBrush, LBodyBounds);
			}

			int LXOffset = LTabBounds.Left + CScrollerOffset;
			int LSelectedOffset = -1;
			int LSelectedWidth = 0;
			int LTabOverlap = GetTabOverlap();
			int[] LTabWidths = GetTabWidths(AArgs.Graphics);

			// Calculate the selected tab's offset and width
			for (int i = FScrollOffset; (i < Pages.Count) && (LXOffset < LTabBounds.Right); i++)
			{
				int LTabWidth = LTabWidths[i];
				if (Pages[i] == FSelected)
				{
					// Leave room for but do not paint the selected tab
					LSelectedWidth = LTabWidth;
					LSelectedOffset = LXOffset;	
				}
				LXOffset += LTabWidth - LTabOverlap;
			}

			if (AArgs.Graphics.IsVisible(LTabBounds))
			{
				GraphicsState LState = AArgs.Graphics.Save();
				AArgs.Graphics.SetClip(new Rectangle(LTabBounds.Left + CScrollerOffset, LTabBounds.Top, LTabBounds.Width - (CScrollerOffset * 2), LTabBounds.Height), CombineMode.Intersect);

				// Paint the unselected tabs
				LXOffset = LTabBounds.Left + CScrollerOffset;
				for (int i = FScrollOffset; (i < Pages.Count) && (LXOffset < LTabBounds.Right); i++)
				{
					int LTabWidth = LTabWidths[i];
					if (Pages[i] != FSelected)
						PaintTab(AArgs.Graphics, LXOffset, (NotebookPage)Pages[i], LTabWidth, false);
					LXOffset += LTabWidth - LTabOverlap;
				}

				// Paint the selected tab
				if (LSelectedOffset >= 0)
				{
					PaintTab(AArgs.Graphics, LSelectedOffset, FSelected, LSelectedWidth, true);
					if (Focused)
						ControlPaint.DrawFocusRectangle(AArgs.Graphics, new Rectangle(LSelectedOffset + FTabAreaHeight, LTabBounds.Y + 2, LSelectedWidth - FTabAreaHeight - GetRounding(), LTabBounds.Height - 4));
				}

				AArgs.Graphics.Restore(LState);

				// Draw the left/right scrollers
				if (FScrollOffset > 0)
					AArgs.Graphics.DrawImage(FLeftScrollerBitmap, LTabBounds.Left, LTabBounds.Top + ((LTabBounds.Height - FLeftScrollerBitmap.Height) / 2), FLeftScrollerBitmap.Width, FLeftScrollerBitmap.Height);
				if (LXOffset > (LTabBounds.Width - CScrollerOffset))
					AArgs.Graphics.DrawImage(FRightScrollerBitmap, LTabBounds.Right - CScrollerOffset, LTabBounds.Top + ((LTabBounds.Height - FRightScrollerBitmap.Height) / 2), FRightScrollerBitmap.Width, FRightScrollerBitmap.Height);
			}
			// Paint the border lines
			using (Pen LPen = new Pen(FLineColor))
			{
				if (LSelectedOffset >= 0)
				{
					AArgs.Graphics.DrawLine(LPen, LBounds.Left, LTabLineY, LSelectedOffset, LTabLineY);
					AArgs.Graphics.DrawLine(LPen, LSelectedOffset + LSelectedWidth, LTabLineY, LBounds.Right, LTabLineY);
				}
				else
					AArgs.Graphics.DrawLine(LPen, LBounds.Left, LTabLineY, LBounds.Right, LTabLineY);
				AArgs.Graphics.DrawLine(LPen, LBounds.Left, LTabLineY, LBounds.Left, LOtherLineY);
				AArgs.Graphics.DrawLine(LPen, LBounds.Left, LOtherLineY, LBounds.Right, LOtherLineY);
				AArgs.Graphics.DrawLine(LPen, LBounds.Right, LOtherLineY, LBounds.Right, LTabLineY);
			}
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			InvalidateTabs();
		}

		protected override void OnLostFocus(EventArgs AArgs)
		{
			base.OnLostFocus(AArgs);
			InvalidateTabs();
		}

		private void InvalidateTabs()
		{
			Rectangle LBounds = GetTabBounds();
			LBounds.Height++;
			if (FTabAlignment == TabAlignment.Bottom)
				LBounds.Y--;
			Invalidate(LBounds, false);
		}

		#endregion

		#region Layout

		private int FScrollOffset;
		public int ScrollOffset
		{
			get { return FScrollOffset; }
			set
			{
				if (value < 0)
					value = 0;
				if (value >= Pages.Count)
					value = Math.Max(Pages.Count - 1, 0);

				if (FScrollOffset != value)
				{
					FScrollOffset = value;
					InvalidateTabs();
					MinimizeScrolling();
				}
			}
		}

		private void MinimizeScrolling()
		{
			int[] LTabWidths = GetTabWidths();
			// Minimize scrolling
			if (LTabWidths.Length > 0)
			{
				int LTabWidth = GetTabBounds().Width - (CScrollerOffset * 2);
				int LTabOverlap = GetTabOverlap();
				int LAccumulated = LTabWidths[LTabWidths.Length - 1];

				// Don't scroll above the LMax offset, doing so would just waste tab space
				int LMax;
				for (LMax = LTabWidths.Length - 1; (LMax > 0) && ((LAccumulated + (LTabWidths[LMax - 1] - LTabOverlap)) <= LTabWidth); LMax--)
					LAccumulated += LTabWidths[LMax - 1] - LTabOverlap;

				ScrollOffset = Math.Min(ScrollOffset, LMax);
			}
			else
				ScrollOffset = 0;
		}

		public override System.Drawing.Rectangle DisplayRectangle
		{
			get
			{
				Rectangle LBounds = base.DisplayRectangle;
				if (FTabAlignment == TabAlignment.Top)
				{
					LBounds.Y += FTabAreaHeight;
					LBounds.Height -= FTabAreaHeight;
				}
				else
					LBounds.Height -= FTabAreaHeight;
				LBounds.Inflate(-2, -2);
				return LBounds;
			}
		}

		protected virtual int GetTabWidth(Graphics AGraphics, NotebookPage APage)
		{
			return (FTabAreaHeight / 4) + FTabAreaHeight + Size.Ceiling(AGraphics.MeasureString(APage.Text, Font, PointF.Empty, GetStringFormat())).Width;
		}

		private int[] GetTabWidths(Graphics AGraphics)
		{
			// Calculate the tab widths
			int[] LResult = new int[Pages.Count];
			for (int i = 0; i < Pages.Count; i++)
				LResult[i] = GetTabWidth(AGraphics, (NotebookPage)Pages[i]);
			return LResult;
		}

		private int[] GetTabWidths()
		{
			using (Graphics LGraphics = this.CreateGraphics())
			{
				return GetTabWidths(LGraphics);
			}
		}

		private Rectangle GetTabBounds()
		{
			Rectangle LBounds = base.DisplayRectangle;
			if (FTabAlignment == TabAlignment.Top)
				return new Rectangle(LBounds.Left, LBounds.Top, LBounds.Width, FTabAreaHeight);
			else
				return new Rectangle(LBounds.Left, LBounds.Bottom - FTabAreaHeight, LBounds.Width, LBounds.Bottom);
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			if (!Disposing && IsHandleCreated)
			{
				base.OnLayout(AArgs);
				
				// Layout the selected page
				if (FSelected != null)
					FSelected.Bounds = DisplayRectangle;

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
		public void ScrollIntoView(NotebookPage APage)
		{
			int[] LTabWidths = GetTabWidths();
			int LPageIndex = Pages.IndexOf(APage);
			if (LPageIndex >= 0)
			{
				int LTabWidth = GetTabBounds().Width - (CScrollerOffset * 2);
				int LTabOverlap = GetTabOverlap();
				int LAccumulated = LTabWidths[LPageIndex];

				// Don't scroll below LMin offset, doing so would hide the specified page
				int LMin;
				for (LMin = LPageIndex; (LMin > 0) && ((LAccumulated + (LTabWidths[LMin - 1] - LTabOverlap)) <= LTabWidth); LMin--)
					LAccumulated += LTabWidths[LMin - 1] - LTabOverlap;

				ScrollOffset = Math.Min(LPageIndex, Math.Max(ScrollOffset, LMin));
			}
		}

		#endregion

		#region Mouse

		protected override void OnMouseDown(MouseEventArgs AArgs)
		{
			base.OnMouseDown(AArgs);

			Focus();
			
			Rectangle LTabBounds = GetTabBounds();
			if (LTabBounds.Contains(AArgs.X, AArgs.Y))
			{
				if (AArgs.X < CScrollerOffset)
					ScrollLeft();
				else if (AArgs.X > (LTabBounds.Right - CScrollerOffset))
					ScrollRight();
				else
				{
					NotebookPage LPage = GetTabAt(new Point(AArgs.X, AArgs.Y));
					if (LPage != null)
						Selected = LPage;
				}
			}
		}
 
		protected virtual NotebookPage GetTabAt(Point ALocation)
		{
			Rectangle LTabBounds = GetTabBounds();
			LTabBounds.Inflate(-CScrollerOffset, 0);
			if (LTabBounds.Contains(ALocation))
			{
				int LXOffset = LTabBounds.Left;
				int LTabOverlap = GetTabOverlap();
				int[] LTabWidths = GetTabWidths();
				for (int i = FScrollOffset; (i < LTabWidths.Length) && (LXOffset <= LTabBounds.Right); i++)
				{
					int LWidth = LTabWidths[i];
					if ((ALocation.X >= LXOffset) && (ALocation.X <= (LXOffset + LWidth)))
						return (NotebookPage)Pages[i];
					LXOffset += (LWidth - LTabOverlap);
				}
			}
			return null;
		}

		#endregion

		#region Keyboard

		protected override bool ProcessMnemonic(char ACharCode)
		{
			if (this.Enabled)
				foreach (NotebookPage LPage in Pages)
				{
					if (Control.IsMnemonic(ACharCode, LPage.Text))
					{
						Selected = LPage;
						return true;
					}
				}
			return base.ProcessMnemonic(ACharCode);
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			switch (AKey)
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
			return base.ProcessDialogKey(AKey);
		}

		#endregion

		#region Pages & Controls

		private PageCollection FPages;
		public PageCollection Pages
		{
			get { return FPages; }
		}

		// Maintain a correctly ordered list of pages (the Controls list is reordered as controls are made visible/invisible)
		public class PageCollection : IList, ICollection, IEnumerable
		{
			public PageCollection(Notebook ANotebook)
			{
				FNotebook = ANotebook;
			}

			private Notebook FNotebook;

			private void UpdateNotebook()
			{
				FNotebook.UpdateSelection();
				FNotebook.InvalidateTabs();
			}

			private NotebookPage[] FPages = new NotebookPage[CInitialPagesCapacity];

			private int FCount;
			public int Count { get { return FCount; } }
		
			public NotebookPage this[int AIndex]
			{
				get 
				{ 
					if (AIndex >= FCount)
						throw new ArgumentOutOfRangeException("this");
					return FPages[AIndex]; 
				}
				set 
				{
					RemoveAt(AIndex);
					Insert(AIndex, value);
				}
			}

			private bool FUpdatingControls;

			private void InternalAdd(int AIndex, NotebookPage APage)
			{
				APage.Visible = false;
				APage.TextChanged += new EventHandler(PageTextChanged);
				APage.EnabledChanged += new EventHandler(PageEnabledChanged);

				// Grow the capacity of FPages if necessary
				if (FCount == FPages.Length)
				{
					NotebookPage[] LPages = new NotebookPage[Math.Min(FPages.Length * 2, FPages.Length + 512)];
					Array.Copy(FPages, LPages, FPages.Length);
					FPages = LPages;
				}
				
				// Shift the items
				Array.Copy(FPages, AIndex, FPages, AIndex + 1, FCount - AIndex);

				// Set the inserted item
				FPages[AIndex] = APage;
				FCount++;

				UpdateNotebook();
			}

			public int Add(NotebookPage APage)
			{
				FUpdatingControls = true;
				try
				{
					FNotebook.Controls.Add(APage);
				}
				finally
				{
					FUpdatingControls = false;
				}
				InternalAdd(FCount, APage);
				return FCount - 1;
			}

			internal void ControlAdd(NotebookPage APage)
			{
				if (!FUpdatingControls)
					InternalAdd(FCount, APage);
			}

			public void Insert(int AIndex, NotebookPage APage)
			{
				FUpdatingControls = true;
				try
				{
					FNotebook.Controls.Add(APage);
				}
				finally
				{
					FUpdatingControls = false;
				}
				InternalAdd(AIndex, APage);
			}

			public void AddRange(NotebookPage[] APages)
			{
				foreach (NotebookPage LPage in APages)
					Add(LPage);
			}

			public void Clear()
			{
				while (FCount > 0)
					RemoveAt(FCount - 1);
			}

			public bool Contains(NotebookPage APage)
			{
				return IndexOf(APage) >= 0;
			}

			public int IndexOf(NotebookPage APage)
			{
				for (int i = 0; i < FCount; i++)
					if (FPages[i] == APage)
						return i;
				return -1;
			}

			private NotebookPage InternalRemoveAt(int AIndex)
			{
				if (AIndex >= FCount)
					throw new ArgumentOutOfRangeException("AIndex");

				NotebookPage LResult = FPages[AIndex];

                FCount--;
                Array.Copy(FPages, AIndex + 1, FPages, AIndex, FCount - AIndex);

				LResult.TextChanged -= new EventHandler(PageTextChanged);
				LResult.EnabledChanged -= new EventHandler(PageEnabledChanged);
				
				UpdateNotebook();

				return LResult;
			}

			public void RemoveAt(int AIndex)
			{
				NotebookPage LResult = InternalRemoveAt(AIndex);
				FUpdatingControls = true;
				try
				{
					FNotebook.Controls.Remove(LResult);
				}
				finally
				{
					FUpdatingControls = false;
				}
			}

			internal void ControlRemove(NotebookPage APage)
			{
				if (!FUpdatingControls)
					InternalRemoveAt(IndexOf(APage));
			}

			public void Remove(NotebookPage APage)
			{
				InternalRemoveAt(IndexOf(APage));
				FUpdatingControls = true;
				try
				{
					FNotebook.Controls.Remove(APage);
				}
				finally
				{
					FUpdatingControls = false;
				}
			}

			public IEnumerator GetEnumerator()
			{
				return new PageCollectionEnumerator(this);
			}

			public class PageCollectionEnumerator : IEnumerator
			{
				public PageCollectionEnumerator(PageCollection ACollection)
				{
					FCollection = ACollection;
				}

				private PageCollection FCollection;
				private int FIndex = -1;

				public void Reset()
				{
					FIndex = -1;
				}

				object IEnumerator.Current
				{
					get { return FCollection[FIndex]; }
				}

				public NotebookPage Current
				{
					get { return FCollection[FIndex]; }
				}

				public bool MoveNext()
				{
					FIndex++;
					return FIndex < FCollection.Count;
				}
			}

			#region ICollection / IList

			void ICollection.CopyTo(Array ATarget, int AIndex)
			{
				for (int i = 0; i < FCount; i++)
					ATarget.SetValue(FPages[i], i + AIndex);
			}

			bool ICollection.IsSynchronized
			{
				get { return false; }
			}

			object ICollection.SyncRoot
			{
				get { return this; }
			}

			int IList.Add(object AValue)
			{
				return this.Add((NotebookPage)AValue);
			}

			bool IList.Contains(object APage)
			{
				return this.Contains((NotebookPage)APage);
			}

			bool IList.IsFixedSize { get { return false; } }

			object IList.this[int AIndex] 
			{ 
				get { return this[AIndex]; } 
				set { this[AIndex] = (NotebookPage)value; }
			}

			int IList.IndexOf(object APage)
			{
				return IndexOf((NotebookPage)APage);
			}

			void IList.Insert(int AIndex, object AValue)
			{
				Insert(AIndex, (NotebookPage)AValue);
			}

			void IList.Remove(object AValue)
			{
				Remove((NotebookPage)AValue);
			}

			public bool IsReadOnly { get { return false; } }

			private void PageTextChanged(object ASender, EventArgs AArgs)
			{
				FNotebook.InvalidateTabs();
			}

			private void PageEnabledChanged(object ASender, EventArgs AArgs)
			{
				FNotebook.InvalidateTabs();
			}

			#endregion
		}

		protected override System.Windows.Forms.Control.ControlCollection CreateControlsInstance()
		{
			return new NotebookControlCollection(this);
		}

		public class NotebookControlCollection : Control.ControlCollection
		{
			public NotebookControlCollection(Notebook ANotebook) : base(ANotebook)
			{
				FNotebook = ANotebook;
			}

			private Notebook FNotebook;

			public override void Add(Control AControl)
			{
				if (!(AControl is NotebookPage))
					throw new ControlsException(ControlsException.Codes.InvalidNotebookChild, (AControl == null ? "<null>" : AControl.GetType().Name));
				FNotebook.Pages.ControlAdd((NotebookPage)AControl);
				base.Add(AControl);
			}

			public override void Remove(Control AControl)
			{
				base.Remove(AControl);
				FNotebook.Pages.ControlRemove((NotebookPage)AControl);
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
