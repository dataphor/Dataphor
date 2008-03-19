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
		public const int CButtonMarginWidth = 2;

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

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				base.Dispose(ADisposing);
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

		private DataLink FLink;
		protected DataLink Link
		{
			get { return FLink; }
		}

		private void InitializeLink()
		{
			FLink = new DataLink();
			FLink.OnActiveChanged += new DataLinkHandler(DataActiveChange);
			FLink.OnDataChanged += new DataLinkHandler(DataChange);
		}

		private void DisposeLink()
		{
			if (FLink != null)
			{
				FLink.OnActiveChanged -= new DataLinkHandler(DataActiveChange);
				FLink.OnDataChanged -= new DataLinkHandler(DataChange);
				FLink.Dispose();
				FLink = null;
			}
		}

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get	{ return FLink.Source; }
			set	{ FLink.Source = value;	}
		}

		private Order FOrder;
		protected Order Order { get { return FOrder; } }

		private void SetOrder(Order AOrder)
		{
			if (AOrder != FOrder)
			{
				FOrder = AOrder;
				UpdateControls();
			}
		}

		protected virtual void DataActiveChange(DataLink ADataLink, DataSet ADataSet)
		{
			if (ADataLink.Active && (ADataSet is TableDataSet))
				SetOrder(((TableDataSet)ADataSet).Order);
			else
				SetOrder(null);
		}

		protected virtual void DataChange(DataLink ADataLink, DataSet ADataSet)
		{
			TableDataSet LDataSet = ADataSet as TableDataSet;
			if (ADataLink.Active && (LDataSet != null))
			{
				if (FOrder != LDataSet.Order)
					SetOrder(LDataSet.Order);
				else
					if (!FSearching)
					{
						CancelPending();	// Don't search if the set has already changed

						Control LControl = FControlPanel.GetFocusedControl();
						if (LControl != null)
							SyncronizeControls((IIncrementalControl)LControl, true);
						else
							Reset();
					}
			}
		}

		#endregion

		#region Control Panel

		private IncrementalControlPanel FControlPanel;

		private void InitializeControlPanel()
		{
			FControlPanel = new IncrementalControlPanel();
			FControlPanel.Parent = this;
			FControlPanel.ClippingChanged += new System.EventHandler(ControlPanelClippingChanged);
		}

		private void DisposeControlPanel()
		{
			if (FControlPanel != null)
			{
				FControlPanel.Dispose();
				FControlPanel = null;
			}
		}

		protected virtual Control CreateIncrementalControl(OrderColumn AColumn)
		{
			Control LControl;
			// TODO: Make incremental control selection extensible (or at least more controllable)
			if (((DAE.Schema.ScalarType)AColumn.Column.DataType).NativeType == DAE.Runtime.Data.NativeAccessors.AsBoolean.NativeType)
				LControl = new IncrementalCheckBox();
			else
				LControl = new IncrementalTextBox();
			IIncrementalControl LIncremental = ((IIncrementalControl)LControl);
			LIncremental.ColumnName = AColumn.Column.Name;
			LIncremental.OnChanged += new System.EventHandler(ControlChanged);
			LControl.Leave += new System.EventHandler(ControlLeave);
			LControl.Enter += new System.EventHandler(ControlEnter);

			// Search for this column in the Columns list for more information
			IncrementalSearchColumn LColumn = FColumns[AColumn.Column.Name];
			if (LColumn != null)
				LIncremental.Initialize(LColumn);

			return LControl;
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			if (FControlPanel.Controls.Count > 0)
				FControlPanel.Controls[0].Focus();
		}

		private bool FAutoFocus;

		protected virtual void UpdateControls()
		{
			CancelPending();

			FControlPanel.SuspendLayout();
			try
			{
				FControlPanel.Clear();

				if (FLink.Active && (Order != null))
				{
					foreach (OrderColumn LColumn in Order.Columns)
					{
						if (!((TableDataSet)FLink.DataSet).IsDetailKey(LColumn.Column.Name))
							FControlPanel.Controls.Add(CreateIncrementalControl(LColumn));
					}
					UpdateControlStyles();
				}
			}
			finally
			{
				FControlPanel.ResumeLayout(true);
			}

			if (FAutoFocus)
			{
				FAutoFocus = false;
				if (FControlPanel.Controls.Count > 0)
					FControlPanel.Controls[0].Focus();
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
		private void SyncronizeControls(IIncrementalControl ASelectedControl, bool AResetSelected)
		{
			FSetting = true;
			try
			{
				bool LPassed;
				if (ASelectedControl == null)
					LPassed = true;
				else
				{
					LPassed = false;
					if (AResetSelected)
						ASelectedControl.Reset();
				}
				foreach (IIncrementalControl LControl in FControlPanel.Controls)
				{
					if (LControl == ASelectedControl)
						LPassed = true;
					else
					{
						if (LPassed)
							LControl.Reset();
						else
							if (FLink.Active && !FLink.DataSet.IsEmpty())
								LControl.InjectValue(FLink.DataSet.ActiveRow, true);
					}
				}
			}
			finally
			{
				FSetting = false;
			}
		}

		/// <summary> Auto fill-in more significant search controls and clear less significant ones when focus changes. </summary>
		private void ControlEnter(object ASender, EventArgs AArgs)
		{
			SyncronizeControls((IIncrementalControl)ASender, false);
		}

		#endregion

		#region Columns

		private IncrementalColumns FColumns;
		[Category("Columns")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public IncrementalColumns Columns
		{
			get { return FColumns; }
		}

		private void InitializeColumns()
		{
			FColumns = new IncrementalColumns();
			FColumns.ColumnChanged += new System.EventHandler(ColumnChanged);
		}

		private void DisposeColumns()
		{
			if (FColumns != null)
			{
				FColumns.Dispose();
				FColumns = null;
			}
		}

		private void ColumnChanged(object ASender, EventArgs AArgs)
		{
			if ((FLink != null) && FLink.Active && FControlPanel.Contains(((IncrementalSearchColumn)ASender).ColumnName))
				UpdateControls();
		}

		#endregion

		#region Cosmetics

		private Color FNoValueBackColor = ControlColor.NoValueBackColor;
		/// <summary> BackColor for edit controls that support NoValueBackColor. </summary>
		[Category("Appearance")]
		[Description("BackColor for edit controls that support NoValueBackColor.")]
		public Color NoValueBackColor
		{
			get { return FNoValueBackColor; }
			set
			{
				if (FNoValueBackColor != value)
				{
					FNoValueBackColor = value;
					UpdateControlStyles();
				}
			}
		}

		private Color FInvalidValueBackColor = ControlColor.ConversionFailBackColor;
		/// <summary> BackColor of controls that fail to convert their value to the columns DataType. </summary>
		[Category("Appearance")]
		[Description("BackColor of controls that fail to convert their value to the columns DataType.")]
		public Color InvalidValueBackColor
		{
			get { return FInvalidValueBackColor; }
			set
			{
				if (FInvalidValueBackColor != value)
				{
					FInvalidValueBackColor = value;
					UpdateControlStyles();
				}
			}
		}

		private void UpdateControlStyles()
		{
			foreach (IIncrementalControl LControl in FControlPanel.Controls)
				LControl.UpdateStyle(this);
		}

		private TitleAlignment FTitleAlignment = TitleAlignment.Left;
		[Category("Appearance")]
		[DefaultValue(TitleAlignment.Left)]
		[Description("Location of the column label.")]
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set
			{
				if (FTitleAlignment != value)
				{
					FTitleAlignment = value;
					SuspendLayout();
					try
					{
						FControlPanel.TitleAlignment = value;
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

		private System.Windows.Forms.Timer FSearchTimer;
		protected System.Windows.Forms.Timer SearchTimer
		{
			get { return FSearchTimer; }
		}

		private void InitializeSearchTimer()
		{
			FSearchTimer = new System.Windows.Forms.Timer();
			FSearchTimer.Interval = 600;
			FSearchTimer.Tick += new System.EventHandler(SearchTimerElapsed);
		}

		private void DisposeSearchTimer()
		{
			if (FSearchTimer != null)
			{
				FSearchTimer.Tick -= new System.EventHandler(SearchTimerElapsed);
				FSearchTimer.Dispose();
				FSearchTimer = null;
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
			get { return FSearchTimer.Interval; }
			set
			{
				if (value < 0)
					value = 0;
				if (FSearchTimer.Interval != value)
					FSearchTimer.Interval = value;
			}
		}

		protected void SearchTimerElapsed(object ASender, EventArgs AArgs)
		{
			ProcessPending();
		}

		#endregion

		#region Search Pending

		/// <summary> The control (if any) that has changed, cuasing a pending search. </summary>
		private Control FPendingControl;

		public bool IsSearchPending()
		{
			return FPendingControl != null;
		}

		public void CancelPending()
		{
			FSearchTimer.Stop();
			FPendingControl = null;
		}

		/// <summary> Processes the pending search request (if there is one). </summary>
		public void ProcessPending()
		{
			FSearchTimer.Stop();
			if (FPendingControl != null)
				try
				{
					InternalSearch();
				}
				finally
				{
					FPendingControl = null;
				}
		}

		/// <summary> Processes any existing pending search and makes the specified </summary>
		private void MakePending(Control AControl)
		{
			if (AControl == null)
				ProcessPending();
			else
			{
				CancelPending();
				FSearchTimer.Start();
				FPendingControl = AControl;
			}
		}

		private void ControlChanged(object ASender, EventArgs AArgs)
		{
			// If a control changes value (besides us setting it), then make it pending for search
			if (!FSetting)
				MakePending((Control)ASender);
		}

		private void ControlLeave(object ASender, EventArgs AArgs)
		{
			// Make sure that any pending search is performed before leaving the search control.
			if (!Disposing && !IsDisposed)
				ProcessPending();
		}

		#endregion

		#region Searching

		/// <summary> Indicates that we are updateing the values of the controls (we should thus ignore their change notifications). </summary>
		private bool FSetting;

		/// <summary> Cancels any pending search and clears and specified search criteria. </summary>
		public void Reset()
		{
			CancelPending();
			FSetting = true;
			try
			{
				foreach (IIncrementalControl LIncremental in FControlPanel.Controls)
					LIncremental.Reset();
			}
			finally
			{
				FSetting = false;
			}
		}

		/// <summary> Creates a row to search for based on the master columns (if applicable) and the search criteria up to the pending control. </summary>
		private DAE.Runtime.Data.Row CreateSearchRow()
		{
			IIncrementalControl LPendingIncremental = (IIncrementalControl)FPendingControl;

			// Build a row consisting of order columns up to and including the pending control
			RowType LRowType = new RowType();	
			foreach (OrderColumn LColumn in FOrder.Columns)
			{
				LRowType.Columns.Add(new Column(LColumn.Column.Name, LColumn.Column.DataType));
				if (LColumn.Column.Name == LPendingIncremental.ColumnName)
					break;
			}

			TableDataSet LDataSet = (TableDataSet)FLink.DataSet;
			
			Row LRow = new Row(FLink.DataSet.Process, LRowType);
			try
			{
				LDataSet.InitializeFromMaster(LRow);
				foreach (Schema.Column LColumn in LRowType.Columns)
				{
					if (!LDataSet.IsDetailKey(LColumn.Name))
						if (!((IIncrementalControl)FControlPanel[LColumn.Name]).ExtractValue(LRow))
							return null;
				}
				return LRow;
			}
			catch
			{
				LRow.Dispose();
				throw;
			}
		}

		private bool FSearching;

		/// <summary> Performs the search, using pending search control. </summary>
		private void InternalSearch()
		{
			if (FLink.Active)
			{
				if (!FLink.DataSet.IsEmpty())	// When the view is empty there is nothing to find so don't bother.
				{
					FSearching = true;
					try
					{
						// Perform the search
						System.Windows.Forms.Cursor LOldCursor = this.Cursor;
						this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
						try
						{
							using (Row LRow = CreateSearchRow())
							{
								if (LRow == null)
									return;
								FLink.DataSet.FindNearest(LRow);
							}
						}
						finally
						{
							this.Cursor = LOldCursor;
						}
					}
					finally
					{
						FSearching = false;
					}

					// Provide nearest match feedback
					if (!FLink.DataSet.IsEmpty())
					{
						FSetting = true;
						try
						{
							((IIncrementalControl)FPendingControl).InjectValue(FLink.DataSet.ActiveRow, false);
						}
						finally
						{
							FSetting = false;
						}
					}
				}
			}
		}
		
		#endregion

		#region SearchBy Button

		private BitmapButton FButton;

		private void InitializeButton()
		{
			FButton = new SpeedButton();
			FButton.Image = SpeedButton.ResourceBitmap(typeof(IncrementalSearch), "Alphora.Dataphor.DAE.Client.Controls.Images.SortBy.png");
			FButton.Click += new System.EventHandler(SearchByButtonPressed);
			FButton.Parent = this;
			FButton.Size = MinButtonSize();
		}

		private void DisposeButton()
		{
			if (FButton != null)
			{
				FButton.Click -= new System.EventHandler(SearchByButtonPressed);
				FButton.Dispose();
				FButton = null;
			}
		}

		[DefaultValue(false)]
		[Category("Appearance")]
		public bool HideSearchByButton
		{
			get { return !FButton.Visible; }
			set { FButton.Visible = !value; }
		}

		private SearchByDropDown FSearchByDropDown;

		public void SelectSearchBy()
		{
			if (FLink.Active && (FSearchByDropDown == null))
			{
				FSearchByDropDown = new SearchByDropDown(this, (TableDataSet)FLink.DataSet);
				FSearchByDropDown.Closed += new System.EventHandler(SearchByDropDownClosed);
				FSearchByDropDown.Show();
			}
		}

		private void SearchByDropDownClosed(object ASender, EventArgs AArgs)
		{
			FSearchByDropDown = null;
			Form LForm = FindForm();
			if (LForm != null)
				LForm.Focus();
			FAutoFocus = true;
		}

		private void SearchByButtonPressed(object ASender, EventArgs AArgs)
		{
			SelectSearchBy();
		}

		protected override bool IsInputKey(System.Windows.Forms.Keys AKey)
		{
			return 
				(AKey != System.Windows.Forms.Keys.Up) 
					&& (AKey != System.Windows.Forms.Keys.Down) 
					&&
					(
						(AKey == System.Windows.Forms.Keys.Insert) 
							|| (AKey == (System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Down)) 
							|| base.IsInputKey(AKey)
					);
		}

		protected override bool ProcessDialogKey(System.Windows.Forms.Keys AKey)
		{
			switch (AKey)
			{
				case System.Windows.Forms.Keys.Insert : SelectSearchBy(); break;
				default : return base.ProcessDialogKey(AKey);
			}
			return true;
		}

		#endregion

		#region Layout

		private Point FRightClipLocation;
		private Point FLeftClipLocation;

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			if (!Disposing)
			{
				base.OnLayout(AArgs);
				// Size the control
				Height = NaturalHeight();
				Rectangle LBounds = DisplayRectangle;
				LBounds.Inflate(-2, -2);
				
				// Layout the button
				if (!HideSearchByButton)
				{
					FButton.Bounds =
						new Rectangle
						(
							LBounds.Right - FButton.Width,
							LBounds.Top,
							FButton.Width,
							LBounds.Height
						);
					LBounds.Width -= FButton.Width + CButtonMarginWidth;
				}

				FLeftClipLocation = new Point(0, (LBounds.Height - FLeftClipBitmap.Height) / 2);
				FRightClipLocation = new Point(LBounds.Right - FRightClipBitmap.Width, (LBounds.Height - FRightClipBitmap.Height) / 2);

				// Layout the panel assuming space will be needed for both "clippers"
				LBounds.X += FLeftClipBitmap.Width;
				LBounds.Width -= FLeftClipBitmap.Width + FRightClipBitmap.Width;
				FControlPanel.Bounds = LBounds;
			}
		}

		private Size MinButtonSize()
		{
			return FButton.Image.Size + new Size(8, 8);
		}

		public int NaturalHeight()
		{
			return Math.Max(MinButtonSize().Height, FControlPanel.NaturalHeight()) + 2;
		}

		#endregion

		#region Painting

		private Bitmap FLeftClipBitmap;
		private Bitmap FRightClipBitmap;

		private void InitializePainting()
		{
			FLeftClipBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			FRightClipBitmap = IncrementalControlPanel.LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.Clip.png");
			FRightClipBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
		}

		private void DisposePainting()
		{
			if (FLeftClipBitmap != null)
			{
				FLeftClipBitmap.Dispose();
				FLeftClipBitmap = null;
			}
			if (FRightClipBitmap != null)
			{
				FRightClipBitmap.Dispose();
				FRightClipBitmap = null;
			}
		}

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			Rectangle LBounds = DisplayRectangle;
			LBounds.Inflate(-1, -1);

			// Paint the border
			using (Pen LPen = new Pen(SystemColors.ActiveBorder))
			{
				AArgs.Graphics.DrawRectangle(LPen, LBounds);
			}

			LBounds.Inflate(-1, -1);

			// Paint the left clip indicator
			if (FControlPanel.ClipOffset > 0)
				AArgs.Graphics.DrawImage(FLeftClipBitmap, FLeftClipLocation.X, FLeftClipLocation.Y, FLeftClipBitmap.Width, FLeftClipBitmap.Height);

			// Paint the right clip indicator
			if (FControlPanel.Overflows)
				AArgs.Graphics.DrawImage(FRightClipBitmap, FRightClipLocation.X, FRightClipLocation.Y, FRightClipBitmap.Width, FRightClipBitmap.Height);
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
		private const int CTitleVSpacing = 4;
		private const int CMinControlWidth = 8;

		protected internal IncrementalControlPanel() : base()
		{
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.CacheText, false);
			FGripperBitmap = LoadResourceBitmap("Alphora.Dataphor.DAE.Client.Controls.Images.ThinGripper.png");
		}

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			if (FGripperBitmap != null)
			{
				FGripperBitmap.Dispose();
				FGripperBitmap = null;
			}
		}

		public static Bitmap LoadResourceBitmap(string AResourceName)
		{
			using (Stream LStream = typeof(IncrementalControlPanel).Assembly.GetManifestResourceStream(AResourceName))
			{
				return new Bitmap(LStream);
			}
		}

		/// <summary> Locates a control by its column name. </summary>
		public Control this[string AColumnName]
		{
			get
			{
				foreach (IIncrementalControl LIncremental in Controls)
					if (LIncremental.ColumnName == AColumnName)
						return (Control)LIncremental;
				return null;
			}
		}

		/// <summary> Returns true if the panel contains a control for the given column name.</summary>
		public bool Contains(string AColumnName)
		{
			return this[AColumnName] != null;
		}

		private TitleAlignment FTitleAlignment = TitleAlignment.Left;
		[DefaultValue(TitleAlignment.Left)]
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set
			{
				if (FTitleAlignment != value)
				{
					FTitleAlignment = value;
					PerformLayout();
				}
			}
		}

		#region Focus

		public Control GetFocusedControl()
		{
			if (ContainsFocus)
				foreach (Control LControl in Controls)
					if (LControl.ContainsFocus)
						return LControl;
			return null;
		}

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			if (Controls.Count > 0)
				Controls[0].Focus();
		}

		#endregion

		#region Layout

		private Rectangle[] FTitleBounds;
		private Point[] FGripperLocations;

		public event System.EventHandler ClippingChanged;
		protected virtual void OnClippingChanged()
		{
			if (ClippingChanged != null)
				ClippingChanged(this, EventArgs.Empty);
		}

		private int FClipOffset;
		public int ClipOffset
		{
			get { return FClipOffset; }
		}
		private void SetClipOffset(int AValue)
		{
			AValue = Math.Max(0, AValue);
			if (AValue != FClipOffset)
			{
				FClipOffset = AValue;
				PerformLayout();
				OnClippingChanged();
			}
		}

		public void Clear()
		{
			Controls.Clear();
			SetClipOffset(0);
		}

		private bool FOverflows;
		public bool Overflows
		{
			get { return FOverflows; }
		}
		private void SetOverflows(bool AValue)
		{
			if (AValue != FOverflows)
			{
				FOverflows = AValue;
				OnClippingChanged();
			}
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			// TODO: possibly put in code limiting the width of the title text
			base.OnLayout(AArgs);
			FTitleBounds = new Rectangle[Controls.Count];
			FGripperLocations = new Point[Controls.Count];
			Rectangle LBounds = DisplayRectangle;
			Control LControl;

			int LLeftOffset = -FClipOffset;
			using (Graphics LGraphics = CreateGraphics())
			{
				for (int i = 0; i < Controls.Count; i++)
				{
					LControl = Controls[i];
					switch (FTitleAlignment)
					{
						case TitleAlignment.Top :
							LControl.Location = new Point(LLeftOffset, Font.Height + CTitleVSpacing);
							FTitleBounds[i] = 
								new Rectangle
								(
									LLeftOffset, 
									0, 
									Size.Ceiling(LGraphics.MeasureString(((IIncrementalControl)LControl).Title, Font)).Width, 
									Font.Height
								);
							FGripperLocations[i] = new Point(LLeftOffset + LControl.Width, Font.Height + CTitleVSpacing + ((LControl.Height - FGripperBitmap.Height) / 2));
							LLeftOffset += Math.Max(FTitleBounds[i].Width, LControl.Width) + FGripperBitmap.Width;
							break;
						case TitleAlignment.Left :
							FTitleBounds[i] =
								new Rectangle
								(
									LLeftOffset,
									Math.Max(0, (LBounds.Height - Font.Height) / 2),
									Size.Ceiling(LGraphics.MeasureString(((IIncrementalControl)LControl).Title, Font)).Width,
									Font.Height
								);
							LLeftOffset += FTitleBounds[i].Width;
							goto case TitleAlignment.None;
						case TitleAlignment.None :
							LControl.Location = new Point(LLeftOffset, (LBounds.Height - LControl.Height) / 2);
							LLeftOffset += LControl.Width;
							FGripperLocations[i] = new Point(LLeftOffset, LControl.Top + (LControl.Height - FGripperBitmap.Height) / 2);
							LLeftOffset += FGripperBitmap.Width;
							break;
					}
				}
			}
			SetOverflows(LLeftOffset > LBounds.Right);
			Invalidate();
		}

		public int NaturalHeight()
		{
			Control LControl = new IncrementalCheckBox();
			int LMaxControlHeight = LControl.Height;
			LControl.Dispose();
			LControl = new IncrementalTextBox();
			if (LMaxControlHeight < LControl.Height)
				LMaxControlHeight = LControl.Height;
			LControl.Dispose();

			switch (FTitleAlignment)
			{
				case TitleAlignment.Top :
					return Font.Height + CTitleVSpacing + LMaxControlHeight;
				case TitleAlignment.Left :
					return Math.Max(LMaxControlHeight, Font.Height);
				default :
					return LMaxControlHeight;
			}
		}

		protected override void OnControlAdded(ControlEventArgs AArgs)
		{
			base.OnControlAdded(AArgs);
			AArgs.Control.Enter += new System.EventHandler(ControlEnter);
		}

		protected override void OnControlRemoved(ControlEventArgs AArgs)
		{
			AArgs.Control.Enter -= new System.EventHandler(ControlEnter);
			base.OnControlRemoved(AArgs);
		}

		/// <summary> Ensures that the focused control is scrolled into view. </summary>
		private void ControlEnter(object ASender, EventArgs AArgs)
		{
			Control LControl = (Control)ASender;
			int LControlIndex = Controls.IndexOf(LControl);
			Rectangle LBounds = DisplayRectangle;
			int LOffset = 0;	// The proposed shift of the controls)
			int LRightExtent = FGripperLocations[LControlIndex].X + FGripperBitmap.Width;

			switch (FTitleAlignment)
			{
				case TitleAlignment.Left :
					Rectangle LTitleBounds = FTitleBounds[LControlIndex];
					// Attempt to bring the left of the text into view
					LOffset = Math.Max(0, LBounds.Left - LTitleBounds.Left);
					// Attempt to bring the right of the control into view
					LOffset += Math.Min(0, LBounds.Right - (LRightExtent + LOffset));
					// Regardless, make sure that the left of the control is in view
					LOffset += Math.Max(0, LBounds.Left - (LControl.Left + LOffset));
					break;
				case TitleAlignment.Top :
					LRightExtent = Math.Max(LRightExtent, FTitleBounds[LControlIndex].Right);
					goto case TitleAlignment.None;
				case TitleAlignment.None :	
					// Attempt to bring the right of the control/title into view
					LOffset = Math.Min(0, LBounds.Right - LRightExtent);
					// Regardless, make sure that the left of the control is in view
					LOffset += Math.Max(0, LBounds.Left - (LControl.Left + LOffset));
					break;
			}
			SetClipOffset(FClipOffset - LOffset);
		}

		#endregion

		#region Painting

		private Bitmap FGripperBitmap;

		protected override void OnPaint(PaintEventArgs AArgs)
		{
			base.OnPaint(AArgs);
			using (SolidBrush LTextBrush = new SolidBrush(ForeColor))
			{
				if ((FTitleBounds != null) && (FTitleBounds.Length == Controls.Count))
				{
					IIncrementalControl LControl;
					Rectangle LBounds;
					
					// Draw the titles
					for (int i = 0; i < Controls.Count; i++)
					{
						LControl = (IIncrementalControl)Controls[i];
						LBounds = FTitleBounds[i];
						if (AArgs.Graphics.IsVisible(LBounds))
							AArgs.Graphics.DrawString(LControl.Title, Font, LTextBrush, LBounds);
					}
					
					// Draw the grippers
					foreach (Point LLocation in FGripperLocations)
					{
						if (AArgs.Graphics.IsVisible(new Rectangle(LLocation, FGripperBitmap.Size)))
							AArgs.Graphics.DrawImage(FGripperBitmap, LLocation.X, LLocation.Y, FGripperBitmap.Width, FGripperBitmap.Height);
					}
				}
			}
		}

		#endregion
		
		#region Resizing

		private Point FMouseLocation;
		private int FOriginalWidth;
		private int FResizingControlIndex = -1;

		/// <summary> Returns the index of the gripper that contains the specified point, or -1. </summary>
		private int GripperContainingPoint(Point ALocation)
		{
			for (int i = 0; i < FGripperLocations.Length; i++)
				if (new Rectangle(FGripperLocations[i], FGripperBitmap.Size).Contains(ALocation))
					return i;
			return -1;
		}

		protected override void OnMouseDown(MouseEventArgs AArgs)
		{
			base.OnMouseDown(AArgs);
			FMouseLocation = new Point(AArgs.X, AArgs.Y);
			FResizingControlIndex = GripperContainingPoint(FMouseLocation);
			if (FResizingControlIndex > -1)
			{
				FOriginalWidth = Controls[FResizingControlIndex].Width;
				Capture = true;
			}
		}

		protected override void OnMouseMove(MouseEventArgs AArgs)
		{
			base.OnMouseMove(AArgs);
			Point LNewLocation = new Point(AArgs.X, AArgs.Y);
			if (FResizingControlIndex >= 0)
			{
				Control LControl = Controls[FResizingControlIndex];
				LControl.Width = Math.Max(CMinControlWidth, FOriginalWidth + (LNewLocation.X - FMouseLocation.X));
			}
			else
			{
				if (GripperContainingPoint(LNewLocation) > -1)
					this.Cursor = System.Windows.Forms.Cursors.VSplit;
				else
					this.Cursor = System.Windows.Forms.Cursors.Default;
			}
		}

		protected override void OnMouseUp(MouseEventArgs AArgs)
		{
			base.OnMouseUp(AArgs);
			FResizingControlIndex = -1;
			Capture = false;
		}

		#endregion
	}

	[ToolboxItem(false)]
	public class IncrementalColumns : DisposableList
	{
		protected override void Validate(object AValue)
		{
			if (!(AValue is IncrementalSearchColumn))
				throw new ControlsException(ControlsException.Codes.InvalidColumnChild);
		}

		protected override void Adding(object AValue, int AIndex)
		{
			base.Adding(AValue, AIndex);
			((IncrementalSearchColumn)AValue).Changed += new System.EventHandler(ChildColumnChanged);
		}

		protected override void Removing(object AValue, int AIndex)
		{
			base.Removing(AValue, AIndex);
			((IncrementalSearchColumn)AValue).Changed += new System.EventHandler(ChildColumnChanged);
		}

		public event System.EventHandler ColumnChanged;

		private void ChildColumnChanged(object ASender, EventArgs AArgs)
		{
			if (ColumnChanged != null)
				ColumnChanged(ASender, AArgs);
		}

		public IncrementalSearchColumn this[string AColumnName]
		{
			get
			{
				foreach (IncrementalSearchColumn LColumn in this)
					if (LColumn.ColumnName == AColumnName)
						return LColumn;
				return null;
			}
		}
	}

	[ToolboxItem(false)]
	public class IncrementalSearchColumn : MarshalByRefObject
	{
		public const int CDefaultPixelWidth = 100;

		private string FColumnName = String.Empty;
		[Category("Data")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (FColumnName != value)
				{
					FColumnName = value;
					OnChanged();
				}
			}
		}

		private string FTitle = String.Empty;
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("Column title.")]
		public string Title
		{
			get { return FTitle; }
			set
			{
				if (value == null)
					value = String.Empty;
				if (FTitle != value)
				{
					FTitle = value;
					OnChanged();
				}
			}
		}

		private HorizontalAlignment FTextAlignment;
		[Category("Appearance")]
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set
			{
				if (FTextAlignment != value)
				{
					FTextAlignment = value;
					OnChanged();
				}
			}
		}

		private int FControlWidth = CDefaultPixelWidth;
		[DefaultValue(CDefaultPixelWidth)]
		[Category("Appearance")]
		[Description("Pixel width of the control.")]
		public int ControlWidth
		{
			get { return FControlWidth; }
			set
			{
				if (FControlWidth != value)
				{
					FControlWidth = value;
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
		bool ExtractValue(DAE.Runtime.Data.Row ARow);

		/// <summary> Provides feedback from the search (the nearest match) to the control. </summary>
		void InjectValue(DAE.Runtime.Data.Row ARow, bool AOverwrite);

		/// <summary> The name of the column with which this control is associated. </summary>
		string ColumnName { get; set; }

		/// <summary> The title of the control as it is to be displayed. </summary>
		string Title { get; }
	}

	internal class SearchByDropDown : Form
	{
		public static int CMaxItems = 5;

		public SearchByDropDown(Control AOwner, TableDataSet ADataSet)
		{
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			ShowInTaskbar = false;

			FDataSet = ADataSet;

			// Construct the a complete list of possible orderings including non-sparse keys
			Schema.Orders LOrders = new Schema.Orders();
			LOrders.AddRange(ADataSet.TableVar.Orders);
			Schema.Order LOrderForKey;
			foreach (Schema.Key LKey in ADataSet.TableVar.Keys)
				if (!LKey.IsSparse)
				{
					LOrderForKey = new Schema.Order(LKey);
					if (!LOrders.Contains(LOrderForKey))
						LOrders.Add(LOrderForKey);
				}

			FListBox = new ListBox();

			// Populate the listbox with the appropriate order wrappers
			OrderWrapper LWrapper;
			foreach (Schema.Order LOrder in LOrders)
				if (IsOrderVisible(LOrder))
				{
					LWrapper = new OrderWrapper(LOrder, ADataSet);
					FListBox.Items.Add(LWrapper);
					if (LOrder.Equals(ADataSet.Order))
						FListBox.SelectedItem = LWrapper;
				}

			FListBox.Dock = DockStyle.Fill;
			FListBox.Parent = this;
			FListBox.Click += new System.EventHandler(ListBoxClick);
			FListBox.BorderStyle = BorderStyle.FixedSingle;
			
			Bounds =
				LookupBoundsUtility.DetermineBounds
				(
					new Size
					(
						AOwner.Width,
						(Math.Min(CMaxItems, FListBox.Items.Count) * Font.Height) + (Height - DisplayRectangle.Height)
					),
					new Size(100, Font.Height + (Height - DisplayRectangle.Height)),
					AOwner
				);
		}

		private class OrderWrapper
		{
			public OrderWrapper(Schema.Order AOrder, TableDataSet ADataSet)
			{
				FOrder = AOrder;
				FDataSet = ADataSet;
			}

			private Schema.Order FOrder;
			public Schema.Order Order { get { return FOrder; } }

			private TableDataSet FDataSet;

			public override string ToString()
			{
				return Language.D4.MetaData.GetTag(FOrder.MetaData, "Frontend.Title", GetDefaultTitle());
			}
			
			private static string GetColumnTitle(Schema.TableVarColumn AColumn)
			{
				return RemoveAccellerators(Language.D4.MetaData.GetTag(AColumn.MetaData, "Frontend.Title", Schema.Object.Unqualify(AColumn.Name)));
			}

			// COPY: RemoveAccellerators is copied from Frontend.Client.Utility
			public static string RemoveAccellerators(string ASource)
			{
				System.Text.StringBuilder LResult = new System.Text.StringBuilder(ASource.Length);
				for (int i = 0; i < ASource.Length; i++)
				{
					if (ASource[i] == '&') 
					{
						if ((i < (ASource.Length - 1)) && (ASource[i + 1] == '&'))
							i++;
						else
							continue;
					}
					LResult.Append(ASource[i]);
				}
				return LResult.ToString();
			}

			public static bool IsColumnVisible(Schema.TableVarColumn AColumn)
			{
				return Convert.ToBoolean(Language.D4.MetaData.GetTag(AColumn.MetaData, "Frontend.Visible", "true"));
			}

			private string GetDefaultTitle()
			{
				System.Text.StringBuilder LName = new System.Text.StringBuilder();
				foreach (Schema.OrderColumn LColumn in FOrder.Columns)
				{
					if (IsColumnVisible(LColumn.Column) && !FDataSet.IsDetailKey(LColumn.Column.Name))
					{
						if (LName.Length > 0)
							LName.Append(", ");
						LName.Append(GetColumnTitle(LColumn.Column));
						if (!LColumn.Ascending)
							LName.Append(" (descending)");	// TODO: localize
					}
				}
				return "by " + LName.ToString();
			}
		}

		private ListBox FListBox;
		private TableDataSet FDataSet;

		public void Accept()
		{
			if (FListBox.SelectedItem != null)
			{
				FDataSet.Order = ((OrderWrapper)FListBox.SelectedItem).Order;
				Close();
			}
		}

		public void Reject()
		{
			Close();
		}

		protected bool IsOrderVisible(Schema.Order AOrder)
		{
			bool LIsVisible = Convert.ToBoolean(Language.D4.MetaData.GetTag(AOrder.MetaData, "Frontend.Visible", "true"));
			bool LHasVisibleColumns = false;
			if (LIsVisible)
			{
				bool LIsColumnVisible;
				bool LHasInvisibleColumns = false;
				foreach (Schema.OrderColumn LColumn in AOrder.Columns)
				{
					LIsColumnVisible = OrderWrapper.IsColumnVisible(LColumn.Column);
					if (LIsColumnVisible)
						LHasVisibleColumns = true;
					if (LHasInvisibleColumns && LIsColumnVisible)
					{
						LIsVisible = false;
						break;
					}
					
					if (!LIsColumnVisible)
						LHasInvisibleColumns = true;
				}
			}
			return LHasVisibleColumns && LIsVisible;
		}

		private void ListBoxClick(object ASender, EventArgs AArgs)
		{
			Application.Idle += new System.EventHandler(ProcessAccept);
		}

		private void ProcessAccept(object ASender, EventArgs AArgs)
		{
			Application.Idle -= new System.EventHandler(ProcessAccept);
			Accept();
		}

		protected override bool IsInputKey(System.Windows.Forms.Keys AKey)
		{
			return (AKey == System.Windows.Forms.Keys.Escape) || (AKey == System.Windows.Forms.Keys.Enter) || base.IsInputKey(AKey);
		}

		protected override bool ProcessDialogKey(System.Windows.Forms.Keys AKey)
		{
			switch (AKey)
			{
				case System.Windows.Forms.Keys.Enter : Accept(); break;
				case System.Windows.Forms.Keys.Escape : Reject(); break;
				default : return base.ProcessDialogKey(AKey);
			}
			return true;
		}

		private bool FIsClosing;

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			FIsClosing = true;
		}

		protected override void OnDeactivate(EventArgs AArgs)
		{
			base.OnDeactivate(AArgs);
			if (!FIsClosing)
				Reject();
		}

	}
}