/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;

using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.IncrementalSearch),"Icons.IncrementalSearch.bmp")]
	public class IncrementalSearch : Control, IDataSourceReference
	{
		public const int ButtonMarginWidth = 2;

		public IncrementalSearch() : base()
		{
			SuspendLayout();

			SetStyle(ControlStyles.FixedHeight, true);
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.Selectable, true);	   // Must be selectable (will focus child if focused)
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.CacheText, false);
			Size = new Size(200, 60);
			Text = String.Empty;

			InitializeControlPanel();
			InitializeLink();
			InitializeButton();
			InitializeSearchTimer();
			InitializeColumns();
			InitializePainting();

			ResumeLayout(false);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				base.Dispose(disposing);
			}
			finally
			{
				try
				{
					DisposePainting();
				}
				finally
				{
					try
					{
						DisposeSearchTimer();
					}
					finally
					{
						try
						{
							DisposeLink();
						}
						finally
						{
							try
							{
								DisposeControlPanel();
							}
							finally
							{
								try
								{
									DisposeButton();
								}
								finally
								{
									DisposeColumns();
								}
							}
						}
					}
				}
			}
		}

		#region Data Source

		private DataLink _link;
		protected DataLink Link
		{
			get { return _link; }
		}

		private void InitializeLink()
		{
			_link = new DataLink();
			_link.OnActiveChanged += new DataLinkHandler(DataActiveChange);
			_link.OnDataChanged += new DataLinkHandler(DataChange);
		}

		private void DisposeLink()
		{
			if (_link != null)
			{
				_link.OnActiveChanged -= new DataLinkHandler(DataActiveChange);
				_link.OnDataChanged -= new DataLinkHandler(DataChange);
				_link.Dispose();
				_link = null;
			}
		}

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get	{ return _link.Source; }
			set	{ _link.Source = value;	}
		}

		private Order _order;
		protected Order Order { get { return _order; } }

		private void SetOrder(Order order)
		{
			if (order != _order)
			{
				_order = order;
				UpdateControls();
			}
		}

		protected virtual void DataActiveChange(DataLink dataLink, DataSet dataSet)
		{
			if (dataLink.Active && (dataSet is TableDataSet))
				SetOrder(((TableDataSet)dataSet).Order);
			else
				SetOrder(null);
		}

		protected virtual void DataChange(DataLink dataLink, DataSet dataSet)
		{
			TableDataSet localDataSet = dataSet as TableDataSet;
			if (dataLink.Active && (localDataSet != null))
			{
				if (_order != localDataSet.Order)
					SetOrder(localDataSet.Order);
				else
					if (!_searching)
					{
						CancelPending();	// Don't search if the set has already changed

						Control control = _controlPanel.GetFocusedControl();
						if (control != null)
							SyncronizeControls((IIncrementalControl)control, true);
						else
							Reset();
					}
			}
		}

		#endregion

		#region Control Panel

		private IncrementalControlPanel _controlPanel;

		private void InitializeControlPanel()
		{
			_controlPanel = new IncrementalControlPanel();
			_controlPanel.Parent = this;
			_controlPanel.ClippingChanged += new System.EventHandler(ControlPanelClippingChanged);
		}

		private void DisposeControlPanel()
		{
			if (_controlPanel != null)
			{
				_controlPanel.Dispose();
				_controlPanel = null;
			}
		}

		protected virtual Control CreateIncrementalControl(OrderColumn column)
		{
			Control control;
			// TODO: Make incremental control selection extensible (or at least more controllable)
			if (((DAE.Schema.ScalarType)column.Column.DataType).NativeType == DAE.Runtime.Data.NativeAccessors.AsBoolean.NativeType)
				control = new IncrementalCheckBox();
			else
				control = new IncrementalTextBox();
			IIncrementalControl incremental = ((IIncrementalControl)control);
			incremental.ColumnName = column.Column.Name;
			incremental.OnChanged += new System.EventHandler(ControlChanged);
			control.Leave += new System.EventHandler(ControlLeave);
			control.Enter += new System.EventHandler(ControlEnter);

			// Search for this column in the Columns list for more information
			IncrementalSearchColumn localColumn = _columns[column.Column.Name];
			if (localColumn != null)
				incremental.Initialize(localColumn);

			return control;
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			if (_controlPanel.Controls.Count > 0)
				_controlPanel.Controls[0].Focus();
		}

		private bool _autoFocus;

		protected virtual void UpdateControls()
		{
			CancelPending();

			_controlPanel.SuspendLayout();
			try
			{
				_controlPanel.Clear();

				if (_link.Active && (Order != null))
				{
					foreach (OrderColumn column in Order.Columns)
					{
						if (!((TableDataSet)_link.DataSet).IsDetailKey(column.Column.Name))
							_controlPanel.Controls.Add(CreateIncrementalControl(column));
					}
					UpdateControlStyles();
				}
			}
			finally
			{
				_controlPanel.ResumeLayout(true);
			}

			if (_autoFocus)
			{
				_autoFocus = false;
				if (_controlPanel.Controls.Count > 0)
					_controlPanel.Controls[0].Focus();
			}
		}

		/// <summary> 
		///		Given a control, syncronizes the more significant controls to the active row of the 
		///		DataView, optionally resets the given control, and always resets less significant controls.
		///	</summary>
		///	<remarks>
		///		These changes will not cause searching.  If the given control is null, then all controls 
		///		are reset.
		///	</remarks>
		private void SyncronizeControls(IIncrementalControl selectedControl, bool resetSelected)
		{
			_setting = true;
			try
			{
				bool passed;
				if (selectedControl == null)
					passed = true;
				else
				{
					passed = false;
					if (resetSelected)
						selectedControl.Reset();
				}
				foreach (IIncrementalControl control in _controlPanel.Controls)
				{
					if (control == selectedControl)
						passed = true;
					else
					{
						if (passed)
							control.Reset();
						else
							if (_link.Active && !_link.DataSet.IsEmpty())
								control.InjectValue(_link.DataSet.ActiveRow, true);
					}
				}
			}
			finally
			{
				_setting = false;
			}
		}

		/// <summary> Auto fill-in more significant search controls and clear less significant ones when focus changes. </summary>
		private void ControlEnter(object sender, EventArgs args)
		{
			SyncronizeControls((IIncrementalControl)sender, false);
		}

		#endregion

		#region Columns

		private IncrementalColumns _columns;
		[Category("Columns")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public IncrementalColumns Columns
		{
			get { return _columns; }
		}

		private void InitializeColumns()
		{
			_columns = new IncrementalColumns();
			_columns.ColumnChanged += new System.EventHandler(ColumnChanged);
		}

		private void DisposeColumns()
		{
			if (_columns != null)
			{
				_columns.Dispose();
				_columns = null;
			}
		}

		private void ColumnChanged(object sender, EventArgs args)
		{
			if ((_link != null) && _link.Active && _controlPanel.Contains(((IncrementalSearchColumn)sender).ColumnName))
				UpdateControls();
		}

		#endregion

		#region Cosmetics

		private Color _noValueBackColor = ControlColor.NoValueBackColor;
		/// <summary> BackColor for edit controls that support NoValueBackColor. </summary>
		[Category("Appearance")]
		[Description("BackColor for edit controls that support NoValueBackColor.")]
		public Color NoValueBackColor
		{
			get { return _noValueBackColor; }
			set
			{
				if (_noValueBackColor != value)
				{
					_noValueBackColor = value;
					UpdateControlStyles();
				}
			}
		}

		private Color _invalidValueBackColor = ControlColor.ConversionFailBackColor;
		/// <summary> BackColor of controls that fail to convert their value to the columns DataType. </summary>
		[Category("Appearance")]
		[Description("BackColor of controls that fail to convert their value to the columns DataType.")]
		public Color InvalidValueBackColor
		{
			get { return _invalidValueBackColor; }
			set
			{
				if (_invalidValueBackColor != value)
				{
					_invalidValueBackColor = value;
					UpdateControlStyles();
				}
			}
		}

		private void UpdateControlStyles()
		{
			foreach (IIncrementalControl control in _controlPanel.Controls)
				control.UpdateStyle(this);
		}

		private TitleAlignment _titleAlignment = TitleAlignment.Left;
		[Category("Appearance")]
		[DefaultValue(TitleAlignment.Left)]
		[Description("Location of the column label.")]
		public TitleAlignment TitleAlignment
		{
			get { return _titleAlignment; }
			set
			{
				if (_titleAlignment != value)
				{
					_titleAlignment = value;
					SuspendLayout();
					try
					{
						_controlPanel.TitleAlignment = value;
					}
					finally
					{
						ResumeLayout(true);
					}
				}
			}
		}

		#endregion

		#region Search Timer

		private System.Windows.Forms.Timer _searchTimer;
		protected System.Windows.Forms.Timer SearchTimer
		{
			get { return _searchTimer; }
		}

		private void InitializeSearchTimer()
		{
			_searchTimer = new System.Windows.Forms.Timer();
			_searchTimer.Interval = 600;
			_searchTimer.Tick += new System.EventHandler(SearchTimerElapsed);
		}

		private void DisposeSearchTimer()
		{
			if (_searchTimer != null)
			{
				_searchTimer.Tick -= new System.EventHandler(SearchTimerElapsed);
				_searchTimer.Dispose();
				_searchTimer = null;
			}
		}

		/// <summary> The search delay interval in milliseconds after the most recent keystroke. </summary>
		/// <remarks>
		///		This interval ensures that searching does not occur while the 
		///		user is actively typing search criteria. The default is 700 (ms).
		///	</remarks>
		[DefaultValue(600)]
		[Category("Behavior")]
		public int TimerInterval
		{
			get { return _searchTimer.Interval; }
			set
			{
				if (value < 0)
					value = 0;
				if (_searchTimer.Interval != value)
					_searchTimer.Interval = value;
			}
		}

		protected void SearchTimerElapsed(object sender, EventArgs args)
		{
			ProcessPending();
		}

		#endregion

		#region Search Pending

		/// <summary> The control (if any) that has changed, cuasing a pending search. </summary>
		private Control _pendingControl;

		public bool IsSearchPending()
		{
			return _pendingControl != null;
		}

		public void CancelPending()
		{
			_searchTimer.Stop();
			_pendingControl = null;
		}

		/// <summary> Processes the pending search request (if there is one). </summary>
		public void ProcessPending()
		{
			_searchTimer.Stop();
			if (_pendingControl != null)
				try
				{
					InternalSearch();
				}
				finally
				{
					_pendingControl = null;
				}
		}

		/// <summary> Processes any existing pending search and makes the specified </summary>
		private void MakePending(Control control)
		{
			if (control == null)
				ProcessPending();
			else
			{
				CancelPending();
				_searchTimer.Start();
				_pendingControl = control;
			}
		}

		private void ControlChanged(object sender, EventArgs args)
		{
			// If a control changes value (besides us setting it), then make it pending for search
			if (!_setting)
				MakePending((Control)sender);
		}

		private void ControlLeave(object sender, EventArgs args)
		{
			// Make sure that any pending search is performed before leaving the search control.
			if (!Disposing && !IsDisposed)
				ProcessPending();
		}

		#endregion

		#region Searching

		/// <summary> Indicates that we are updateing the values of the controls (we should thus ignore their change notifications). </summary>
		private bool _setting;

		/// <summary> Cancels any pending search and clears and specified search criteria. </summary>
		public void Reset()
		{
			CancelPending();
			_setting = true;
			try
			{
				foreach (IIncrementalControl incremental in _controlPanel.Controls)
					incremental.Reset();
			}
			finally
			{
				_setting = false;
			}
		}

		/// <summary> Creates a row to search for based on the master columns (if applicable) and the search criteria up to the pending control. </summary>
		private DAE.Runtime.Data.Row CreateSearchRow()
		{
			IIncrementalControl pendingIncremental = (IIncrementalControl)_pendingControl;

			// Build a row consisting of order columns up to and including the pending control
			RowType rowType = new RowType();	
			foreach (OrderColumn column in _order.Columns)
			{
				rowType.Columns.Add(new Column(column.Column.Name, column.Column.DataType));
				if (column.Column.Name == pendingIncremental.ColumnName)
					break;
			}

			TableDataSet dataSet = (TableDataSet)_link.DataSet;
			
			Row row = new Row(_link.DataSet.Process.ValueManager, rowType);
			try
			{
				dataSet.InitializeFromMaster(row);
				foreach (Schema.Column column in rowType.Columns)
				{
					if (!dataSet.IsDetailKey(column.Name))
						if (!((IIncrementalControl)_controlPanel[column.Name]).ExtractValue(row))
							return null;
				}
				return row;
			}
			catch
			{
				row.Dispose();
				throw;
			}
		}

		private bool _searching;

		/// <summary> Performs the search, using pending search control. </summary>
		private void InternalSearch()
		{
			if (_link.Active)
			{
				if (!_link.DataSet.IsEmpty())	// When the view is empty there is nothing to find so don't bother.
				{
					_searching = true;
					try
					{
						// Perform the search
						System.Windows.Forms.Cursor oldCursor = this.Cursor;
						this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
						try
						{
							using (Row row = CreateSearchRow())
							{
								if (row == null)
									return;
								_link.DataSet.FindNearest(row);
							}
						}
						finally
						{
							this.Cursor = oldCursor;
						}
					}
					finally
					{
						_searching = false;
					}

					// Provide nearest match feedback
					if (!_link.DataSet.IsEmpty())
					{
						_setting = true;
						try
						{
							((IIncrementalControl)_pendingControl).InjectValue(_link.DataSet.ActiveRow, false);
						}
						finally
						{
							_setting = false;
						}
					}
				}
			}
		}
		
		#endregion

		#region SearchBy Button

		private BitmapButton _button;

		private void InitializeButton()
		{
			_button = new SpeedButton();
			_button.Image = SpeedButton.ResourceBitmap(typeof(IncrementalSearch), "Alphora.Dataphor.DAE.Client.Controls.Images.SortBy.png");
			_button.Click += new System.EventHandler(SearchByButtonPressed);
			_button.Parent = this;
			_button.Size = MinButtonSize();
		}

		private void DisposeButton()
		{
			if (_button != null)
			{
				_button.Click -= new System.EventHandler(SearchByButtonPressed);
				_button.Dispose();
				_button = null;
			}
		}

		[DefaultValue(false)]
		[Category("Appearance")]
		public bool HideSearchByButton
		{
			get { return !_button.Visible; }
			set { _button.Visible = !value; }
		}

		private SearchByDropDown _searchByDropDown;

		public void SelectSearchBy()
		{
			if (_link.Active && (_searchByDropDown == null))
			{
				_searchByDropDown = new SearchByDropDown(this, (TableDataSet)_link.DataSet);
				_searchByDropDown.Closed += new System.EventHandler(SearchByDropDownClosed);
				_searchByDropDown.Show();
			}
		}

		private void SearchByDropDownClosed(object sender, EventArgs args)
		{
			_searchByDropDown = null;
			Form form = FindForm();
			if (form != null)
				form.Focus();
			_autoFocus = true;
		}

		private void SearchByButtonPressed(object sender, EventArgs args)
		{
			SelectSearchBy();
		}

		protected override bool IsInputKey(System.Windows.Forms.Keys key)
		{
			return 
				(key != System.Windows.Forms.Keys.Up) 
					&& (key != System.Windows.Forms.Keys.Down) 
					&&
					(
						(key == System.Windows.Forms.Keys.Insert) 
							|| (key == (System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Down)) 
							|| base.IsInputKey(key)
					);
		}

		protected override bool ProcessDialogKey(System.Windows.Forms.Keys key)
		{
			switch (key)
			{
				case System.Windows.Forms.Keys.Insert : SelectSearchBy(); break;
				default : return base.ProcessDialogKey(key);
			}
			return true;
		}

		#endregion

		#region Layout

		private Point _rightClipLocation;
		private Point _leftClipLocation;

		protected override void OnLayout(LayoutEventArgs args)
		{
			if (!Disposing)
			{
				base.OnLayout(args);
				// Size the control
				Height = NaturalHeight();
				Rectangle bounds = DisplayRectangle;
				bounds.Inflate(-2, -2);
				
				// Layout the button
				if (!HideSearchByButton)
				{
					_button.Bounds =
						new Rectangle
						(
							bounds.Right - _button.Width,
							bounds.Top,
							_button.Width,
							bounds.Height
						);
					bounds.Width -= _button.Width + ButtonMarginWidth;
				}

				_leftClipLocation = new Point(0, (bounds.Height - _leftClipBitmap.Height) / 2);
				_rightClipLocation = new Point(bounds.Right - _rightClipBitmap.Width, (bounds.Height - _rightClipBitmap.Height) / 2);

				// Layout the panel assuming space will be needed for both "clippers"
				bounds.X += _leftClipBitmap.Width;
				bounds.Width -= _leftClipBitmap.Width + _rightClipBitmap.Width;
				_controlPanel.Bounds = bounds;
			}
		}

		private Size MinButtonSize()
		{
			return _button.Image.Size + new Size(8, 8);
		}

		public int NaturalHeight()
		{
			return Math.Max(MinButtonSize().Height, _controlPanel.NaturalHeight()) + 2;
		}

		#endregion

		#region Painting

		private Bitmap _leftClipBitmap;
		private Bitmap _rightClipBitmap;

		private void InitializePainting()
		{
			_leftClipBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			_rightClipBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			_rightClipBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
		}

		private void DisposePainting()
		{
			if (_leftClipBitmap != null)
			{
				_leftClipBitmap.Dispose();
				_leftClipBitmap = null;
			}
			if (_rightClipBitmap != null)
			{
				_rightClipBitmap.Dispose();
				_rightClipBitmap = null;
			}
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			Rectangle bounds = DisplayRectangle;
			bounds.Inflate(-1, -1);

			// Paint the border
			using (Pen pen = new Pen(SystemColors.ActiveBorder))
			{
				args.Graphics.DrawRectangle(pen, bounds);
			}

			bounds.Inflate(-1, -1);

			// Paint the left clip indicator
			if (_controlPanel.ClipOffset > 0)
				args.Graphics.DrawImage(_leftClipBitmap, _leftClipLocation.X, _leftClipLocation.Y, _leftClipBitmap.Width, _leftClipBitmap.Height);

			// Paint the right clip indicator
			if (_controlPanel.Overflows)
				args.Graphics.DrawImage(_rightClipBitmap, _rightClipLocation.X, _rightClipLocation.Y, _rightClipBitmap.Width, _rightClipBitmap.Height);
		}

		private void ControlPanelClippingChanged(object sender, EventArgs e)
		{
			Invalidate();
		}

		#endregion
	}

	/// <summary> Encompasses a set of incremental search controls. </summary>
	/// <remarks> The contained controls must implement IIncrementalControl. </remarks>
	[ToolboxItem(false)]
	internal class IncrementalControlPanel : Control
	{
		private const int WM_MOUSEMOVE	= 0x0200;
		private const int TitleVSpacing = 4;
		private const int MinControlWidth = 8;

		protected internal IncrementalControlPanel() : base()
		{
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.CacheText, false);
			_gripperBitmap = LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.ThinGripper.png");
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (_gripperBitmap != null)
			{
				_gripperBitmap.Dispose();
				_gripperBitmap = null;
			}
		}

		public static Bitmap LoadResourceBitmap(string resourceName)
		{
			using (Stream stream = typeof(IncrementalControlPanel).Assembly.GetManifestResourceStream(resourceName))
			{
				return new Bitmap(stream);
			}
		}

		/// <summary> Locates a control by its column name. </summary>
		public Control this[string columnName]
		{
			get
			{
				foreach (IIncrementalControl incremental in Controls)
					if (incremental.ColumnName == columnName)
						return (Control)incremental;
				return null;
			}
		}

		/// <summary> Returns true if the panel contains a control for the given column name.</summary>
		public bool Contains(string columnName)
		{
			return this[columnName] != null;
		}

		private TitleAlignment _titleAlignment = TitleAlignment.Left;
		[DefaultValue(TitleAlignment.Left)]
		public TitleAlignment TitleAlignment
		{
			get { return _titleAlignment; }
			set
			{
				if (_titleAlignment != value)
				{
					_titleAlignment = value;
					PerformLayout();
				}
			}
		}

		#region Focus

		public Control GetFocusedControl()
		{
			if (ContainsFocus)
				foreach (Control control in Controls)
					if (control.ContainsFocus)
						return control;
			return null;
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			if (Controls.Count > 0)
				Controls[0].Focus();
		}

		#endregion

		#region Layout

		private Rectangle[] _titleBounds;
		private Point[] _gripperLocations;

		public event System.EventHandler ClippingChanged;
		protected virtual void OnClippingChanged()
		{
			if (ClippingChanged != null)
				ClippingChanged(this, EventArgs.Empty);
		}

		private int _clipOffset;
		public int ClipOffset
		{
			get { return _clipOffset; }
		}
		private void SetClipOffset(int value)
		{
			value = Math.Max(0, value);
			if (value != _clipOffset)
			{
				_clipOffset = value;
				PerformLayout();
				OnClippingChanged();
			}
		}

		public void Clear()
		{
			Controls.Clear();
			SetClipOffset(0);
		}

		private bool _overflows;
		public bool Overflows
		{
			get { return _overflows; }
		}
		private void SetOverflows(bool value)
		{
			if (value != _overflows)
			{
				_overflows = value;
				OnClippingChanged();
			}
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			// TODO: possibly put in code limiting the width of the title text
			base.OnLayout(args);
			_titleBounds = new Rectangle[Controls.Count];
			_gripperLocations = new Point[Controls.Count];
			Rectangle bounds = DisplayRectangle;
			Control control;

			int leftOffset = -_clipOffset;
			using (Graphics graphics = CreateGraphics())
			{
				for (int i = 0; i < Controls.Count; i++)
				{
					control = Controls[i];
					switch (_titleAlignment)
					{
						case TitleAlignment.Top :
							control.Location = new Point(leftOffset, Font.Height + TitleVSpacing);
							_titleBounds[i] = 
								new Rectangle
								(
									leftOffset, 
									0, 
									Size.Ceiling(graphics.MeasureString(((IIncrementalControl)control).Title, Font)).Width, 
									Font.Height
								);
							_gripperLocations[i] = new Point(leftOffset + control.Width, Font.Height + TitleVSpacing + ((control.Height - _gripperBitmap.Height) / 2));
							leftOffset += Math.Max(_titleBounds[i].Width, control.Width) + _gripperBitmap.Width;
							break;
						case TitleAlignment.Left :
							_titleBounds[i] =
								new Rectangle
								(
									leftOffset,
									Math.Max(0, (bounds.Height - Font.Height) / 2),
									Size.Ceiling(graphics.MeasureString(((IIncrementalControl)control).Title, Font)).Width,
									Font.Height
								);
							leftOffset += _titleBounds[i].Width;
							goto case TitleAlignment.None;
						case TitleAlignment.None :
							control.Location = new Point(leftOffset, (bounds.Height - control.Height) / 2);
							leftOffset += control.Width;
							_gripperLocations[i] = new Point(leftOffset, control.Top + (control.Height - _gripperBitmap.Height) / 2);
							leftOffset += _gripperBitmap.Width;
							break;
					}
				}
			}
			SetOverflows(leftOffset > bounds.Right);
			Invalidate();
		}

		public int NaturalHeight()
		{
			Control control = new IncrementalCheckBox();
			int maxControlHeight = control.Height;
			control.Dispose();
			control = new IncrementalTextBox();
			if (maxControlHeight < control.Height)
				maxControlHeight = control.Height;
			control.Dispose();

			switch (_titleAlignment)
			{
				case TitleAlignment.Top :
					return Font.Height + TitleVSpacing + maxControlHeight;
				case TitleAlignment.Left :
					return Math.Max(maxControlHeight, Font.Height);
				default :
					return maxControlHeight;
			}
		}

		protected override void OnControlAdded(ControlEventArgs args)
		{
			base.OnControlAdded(args);
			args.Control.Enter += new System.EventHandler(ControlEnter);
		}

		protected override void OnControlRemoved(ControlEventArgs args)
		{
			args.Control.Enter -= new System.EventHandler(ControlEnter);
			base.OnControlRemoved(args);
		}

		/// <summary> Ensures that the focused control is scrolled into view. </summary>
		private void ControlEnter(object sender, EventArgs args)
		{
			Control control = (Control)sender;
			int controlIndex = Controls.IndexOf(control);
			Rectangle bounds = DisplayRectangle;
			int offset = 0;	// The proposed shift of the controls)
			int rightExtent = _gripperLocations[controlIndex].X + _gripperBitmap.Width;

			switch (_titleAlignment)
			{
				case TitleAlignment.Left :
					Rectangle titleBounds = _titleBounds[controlIndex];
					// Attempt to bring the left of the text into view
					offset = Math.Max(0, bounds.Left - titleBounds.Left);
					// Attempt to bring the right of the control into view
					offset += Math.Min(0, bounds.Right - (rightExtent + offset));
					// Regardless, make sure that the left of the control is in view
					offset += Math.Max(0, bounds.Left - (control.Left + offset));
					break;
				case TitleAlignment.Top :
					rightExtent = Math.Max(rightExtent, _titleBounds[controlIndex].Right);
					goto case TitleAlignment.None;
				case TitleAlignment.None :	
					// Attempt to bring the right of the control/title into view
					offset = Math.Min(0, bounds.Right - rightExtent);
					// Regardless, make sure that the left of the control is in view
					offset += Math.Max(0, bounds.Left - (control.Left + offset));
					break;
			}
			SetClipOffset(_clipOffset - offset);
		}

		#endregion

		#region Painting

		private Bitmap _gripperBitmap;

		protected override void OnPaint(PaintEventArgs args)
		{
			base.OnPaint(args);
			using (SolidBrush textBrush = new SolidBrush(ForeColor))
			{
				if ((_titleBounds != null) && (_titleBounds.Length == Controls.Count))
				{
					IIncrementalControl control;
					Rectangle bounds;
					
					// Draw the titles
					for (int i = 0; i < Controls.Count; i++)
					{
						control = (IIncrementalControl)Controls[i];
						bounds = _titleBounds[i];
						if (args.Graphics.IsVisible(bounds))
							args.Graphics.DrawString(control.Title, Font, textBrush, bounds);
					}
					
					// Draw the grippers
					foreach (Point location in _gripperLocations)
					{
						if (args.Graphics.IsVisible(new Rectangle(location, _gripperBitmap.Size)))
							args.Graphics.DrawImage(_gripperBitmap, location.X, location.Y, _gripperBitmap.Width, _gripperBitmap.Height);
					}
				}
			}
		}

		#endregion
		
		#region Resizing

		private Point _mouseLocation;
		private int _originalWidth;
		private int _resizingControlIndex = -1;

		/// <summary> Returns the index of the gripper that contains the specified point, or -1. </summary>
		private int GripperContainingPoint(Point location)
		{
			for (int i = 0; i < _gripperLocations.Length; i++)
				if (new Rectangle(_gripperLocations[i], _gripperBitmap.Size).Contains(location))
					return i;
			return -1;
		}

		protected override void OnMouseDown(MouseEventArgs args)
		{
			base.OnMouseDown(args);
			_mouseLocation = new Point(args.X, args.Y);
			_resizingControlIndex = GripperContainingPoint(_mouseLocation);
			if (_resizingControlIndex > -1)
			{
				_originalWidth = Controls[_resizingControlIndex].Width;
				Capture = true;
			}
		}

		protected override void OnMouseMove(MouseEventArgs args)
		{
			base.OnMouseMove(args);
			Point newLocation = new Point(args.X, args.Y);
			if (_resizingControlIndex >= 0)
			{
				Control control = Controls[_resizingControlIndex];
				control.Width = Math.Max(MinControlWidth, _originalWidth + (newLocation.X - _mouseLocation.X));
			}
			else
			{
				if (GripperContainingPoint(newLocation) > -1)
					this.Cursor = System.Windows.Forms.Cursors.VSplit;
				else
					this.Cursor = System.Windows.Forms.Cursors.Default;
			}
		}

		protected override void OnMouseUp(MouseEventArgs args)
		{
			base.OnMouseUp(args);
			_resizingControlIndex = -1;
			Capture = false;
		}

		#endregion
	}

	[ToolboxItem(false)]
	public class IncrementalColumns : DisposableList
	{
		protected override void Validate(object value)
		{
			if (!(value is IncrementalSearchColumn))
				throw new ControlsException(ControlsException.Codes.InvalidColumnChild);
		}

		protected override void Adding(object value, int index)
		{
			base.Adding(value, index);
			((IncrementalSearchColumn)value).Changed += new System.EventHandler(ChildColumnChanged);
		}

		protected override void Removing(object value, int index)
		{
			base.Removing(value, index);
			((IncrementalSearchColumn)value).Changed += new System.EventHandler(ChildColumnChanged);
		}

		public event System.EventHandler ColumnChanged;

		private void ChildColumnChanged(object sender, EventArgs args)
		{
			if (ColumnChanged != null)
				ColumnChanged(sender, args);
		}

		public IncrementalSearchColumn this[string columnName]
		{
			get
			{
				foreach (IncrementalSearchColumn column in this)
					if (column.ColumnName == columnName)
						return column;
				return null;
			}
		}
	}

	[ToolboxItem(false)]
	public class IncrementalSearchColumn : MarshalByRefObject
	{
		public const int DefaultPixelWidth = 100;

		private string _columnName = String.Empty;
		[Category("Data")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (_columnName != value)
				{
					_columnName = value;
					OnChanged();
				}
			}
		}

		private string _title = String.Empty;
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("Column title.")]
		public string Title
		{
			get { return _title; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (_title != value)
				{
					_title = value;
					OnChanged();
				}
			}
		}

		private HorizontalAlignment _textAlignment;
		[Category("Appearance")]
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment TextAlignment
		{
			get { return _textAlignment; }
			set
			{
				if (_textAlignment != value)
				{
					_textAlignment = value;
					OnChanged();
				}
			}
		}

		private int _controlWidth = DefaultPixelWidth;
		[DefaultValue(DefaultPixelWidth)]
		[Category("Appearance")]
		[Description("Pixel width of the control.")]
		public int ControlWidth
		{
			get { return _controlWidth; }
			set
			{
				if (_controlWidth != value)
				{
					_controlWidth = value;
					OnChanged();
				}
			}
		}

		public event System.EventHandler Changed;

		protected virtual void OnChanged()
		{
			if (Changed != null)
				Changed(this, EventArgs.Empty);
		}
	}

	public enum TitleAlignment {Top, Left, None};

	public interface IIncrementalControl
	{
		/// <summary> Notifies of a change to the control indicating a search should become pending. </summary>
		event System.EventHandler OnChanged;
		
		/// <summary> Clears the state of the control, restoring it to a non-searched, non-error condition. </summary>
		void Reset();

		/// <summary> Initializes the control's properties from those of the column settings. </summary>
		void Initialize(IncrementalSearchColumn AColumn);

		/// <summary> Updates the control's vistual settings from those set on the incremental search control. </summary>
		void UpdateStyle(IncrementalSearch ASearch);

		/// <summary> Sets (or clears) the value on the given row for the associated column from the current value of the control </summary>
		/// <returns> True if the value was able to be extracted.  False if there was a problem. </returns>
		bool ExtractValue(DAE.Runtime.Data.IRow ARow);

		/// <summary> Provides feedback from the search (the nearest match) to the control. </summary>
		void InjectValue(DAE.Runtime.Data.IRow ARow, bool AOverwrite);

		/// <summary> The name of the column with which this control is associated. </summary>
		string ColumnName { get; set; }

		/// <summary> The title of the control as it is to be displayed. </summary>
		string Title { get; }
	}

	internal class SearchByDropDown : Form
	{
		public static int CMaxItems = 5;

		public SearchByDropDown(Control owner, TableDataSet dataSet)
		{
			Owner = owner.FindForm();
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			ShowInTaskbar = false;

			_dataSet = dataSet;

			// Construct the a complete list of possible orderings including non-sparse keys
			Schema.Orders orders = new Schema.Orders();
			orders.AddRange(dataSet.TableVar.Orders);
			Schema.Order orderForKey;
			foreach (Schema.Key key in dataSet.TableVar.Keys)
				if (!key.IsSparse)
				{
					orderForKey = new Schema.Order(key);
					if (!orders.Contains(orderForKey))
						orders.Add(orderForKey);
				}

			_listBox = new ListBox();

			// Populate the listbox with the appropriate order wrappers
			OrderWrapper wrapper;
			foreach (Schema.Order order in orders)
				if (IsOrderVisible(order))
				{
					wrapper = new OrderWrapper(order, dataSet);
					_listBox.Items.Add(wrapper);
					if (order.Equals(dataSet.Order))
						_listBox.SelectedItem = wrapper;
				}

			_listBox.Dock = DockStyle.Fill;
			_listBox.Parent = this;
			_listBox.Click += new System.EventHandler(ListBoxClick);
			_listBox.BorderStyle = BorderStyle.FixedSingle;
			
			Bounds =
				LookupBoundsUtility.DetermineBounds
				(
					new Size
					(
						owner.Width,
						(Math.Min(CMaxItems, _listBox.Items.Count) * Font.Height) + (Height - DisplayRectangle.Height)
					),
					new Size(100, Font.Height + (Height - DisplayRectangle.Height)),
					owner
				);
		}

		private class OrderWrapper
		{
			public OrderWrapper(Schema.Order order, TableDataSet dataSet)
			{
				_order = order;
				_dataSet = dataSet;
			}

			private Schema.Order _order;
			public Schema.Order Order { get { return _order; } }

			private TableDataSet _dataSet;

			public override string ToString()
			{
				return Language.D4.MetaData.GetTag(_order.MetaData, "Frontend.Title", GetDefaultTitle());
			}
			
			private static string GetColumnTitle(Schema.TableVarColumn column)
			{
				return RemoveAccellerators(Language.D4.MetaData.GetTag(column.MetaData, "Frontend.Title", Schema.Object.Unqualify(column.Name)));
			}

			// COPY: RemoveAccellerators is copied from Frontend.Client.Utility
			public static string RemoveAccellerators(string source)
			{
				System.Text.StringBuilder result = new System.Text.StringBuilder(source.Length);
				for (int i = 0; i < source.Length; i++)
				{
					if (source[i] == '&') 
					{
						if ((i < (source.Length - 1)) && (source[i + 1] == '&'))
							i++;
						else
							continue;
					}
					result.Append(source[i]);
				}
				return result.ToString();
			}

			public static bool IsColumnVisible(Schema.TableVarColumn column)
			{
				return Convert.ToBoolean(Language.D4.MetaData.GetTag(column.MetaData, "Frontend.Visible", "true"));
			}

			private string GetDefaultTitle()
			{
				System.Text.StringBuilder name = new System.Text.StringBuilder();
				foreach (Schema.OrderColumn column in _order.Columns)
				{
					if (IsColumnVisible(column.Column) && !_dataSet.IsDetailKey(column.Column.Name))
					{
						if (name.Length > 0)
							name.Append(", ");
						name.Append(GetColumnTitle(column.Column));
						if (!column.Ascending)
							name.Append(" (descending)");	// TODO: localize
					}
				}
				return "by " + name.ToString();
			}
		}

		private ListBox _listBox;
		private TableDataSet _dataSet;

		public void Accept()
		{
			if (_listBox.SelectedItem != null)
			{
				_dataSet.Order = ((OrderWrapper)_listBox.SelectedItem).Order;
				Close();
			}
		}

		public void Reject()
		{
			Close();
		}

		protected bool IsOrderVisible(Schema.Order order)
		{
			bool isVisible = Convert.ToBoolean(Language.D4.MetaData.GetTag(order.MetaData, "Frontend.Visible", "true"));
			bool hasVisibleColumns = false;
			if (isVisible)
			{
				bool isColumnVisible;
				bool hasInvisibleColumns = false;
				foreach (Schema.OrderColumn column in order.Columns)
				{
					isColumnVisible = OrderWrapper.IsColumnVisible(column.Column);
					if (isColumnVisible)
						hasVisibleColumns = true;
					if (hasInvisibleColumns && isColumnVisible)
					{
						isVisible = false;
						break;
					}
					
					if (!isColumnVisible)
						hasInvisibleColumns = true;
				}
			}
			return hasVisibleColumns && isVisible;
		}

		private void ListBoxClick(object sender, EventArgs args)
		{
			Application.Idle += new System.EventHandler(ProcessAccept);
		}

		private void ProcessAccept(object sender, EventArgs args)
		{
			Application.Idle -= new System.EventHandler(ProcessAccept);
			Accept();
		}

		protected override bool IsInputKey(System.Windows.Forms.Keys key)
		{
			return (key == System.Windows.Forms.Keys.Escape) || (key == System.Windows.Forms.Keys.Enter) || base.IsInputKey(key);
		}

		protected override bool ProcessDialogKey(System.Windows.Forms.Keys key)
		{
			switch (key)
			{
				case System.Windows.Forms.Keys.Enter : Accept(); break;
				case System.Windows.Forms.Keys.Escape : Reject(); break;
				default : return base.ProcessDialogKey(key);
			}
			return true;
		}

		private bool _isClosing;

		protected override void OnClosing(CancelEventArgs args)
		{
			base.OnClosing(args);
			_isClosing = true;
		}

		protected override void OnDeactivate(EventArgs args)
		{
			base.OnDeactivate(args);
			if (!_isClosing)
				Reject();
		}

	}
}