/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Design;
using WinForms = System.Windows.Forms;

using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.GridColumn')")]
	[DesignerCategory("Data Controls")]
	public abstract class GridColumn : Node
	{
		[Browsable(false)]
		protected internal DAE.Client.Controls.DBGrid GridControl
		{
			get { return ((Grid)Parent).GridControl; }
		}
		
		[Browsable(false)]
		protected ISource Source 
		{
			get { return ((Grid)Parent).Source; }
		}

		private DAE.Client.Controls.GridColumn FColumn;
		[Browsable(false)]
		public DAE.Client.Controls.GridColumn Column
		{
			get { return FColumn; }
		}

		protected abstract DAE.Client.Controls.GridColumn CreateColumn();

		protected virtual void SetColumn(DAE.Client.Controls.GridColumn AValue)
		{
			if (FColumn != null)
			{
				FColumn.Dispose();
				FColumn = null;
			}
			FColumn = AValue;
		}

		// Hint

		// BTR -> This doesn't do anything, hint is an Dataphor common property		
		private string FHint = String.Empty;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public string Hint
		{
			get { return FHint; }
			set { FHint = value; }
		}

		// Visible

		private bool FVisible = true;
		[DefaultValue(true)]
		[Description("When set to false the menu will not be shown.")]
		public bool Visible
		{
			get { return FVisible; }
			set
			{
				if (FVisible != value)
				{
					FVisible = value;
					UpdateColumn();
				}
			}
		}

		public virtual bool GetVisible() 
		{
			return FVisible;
		}

		// Title

		private string FActualTitle = String.Empty;
		private string FTitle = String.Empty;
		[DefaultValue("")]
		[Description("This text will be displayed for this grid column.")]
		public string Title
		{
			get { return FTitle; }
			set
			{
				if (FTitle != value)
				{
					FTitle = value == null ? String.Empty : value;
					FActualTitle = 
						(
							FTitle.Length > 0 
								? 
								(
									FTitle[0] == '~' 
										? FTitle.Substring(1) 
										: TitleUtility.RemoveAccellerators(FTitle)
								) 
								: String.Empty
						);

					UpdateColumn();
				}
			}
		}

		// Width

		private int FWidth = 10;
		[DefaultValue(10)]
		[Description("The width, in characters, that this column will be initially displayed as.")]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					FWidth = value;
					UpdateColumn();
				}
			}
		}

		[Browsable(false)]
		protected internal int GetPixelWidth()
		{
			int LTitleWidth;
			DAE.Client.Controls.DBGrid LGrid = ((Grid)Parent).GridControl;
			using (System.Drawing.Graphics LGraphics = LGrid.CreateGraphics())
			{
				LTitleWidth = (int)LGraphics.MeasureString(FActualTitle, LGrid.Font).Width;
			}
			return Math.Max
			(
				FWidth * ((Grid)Parent).AverageCharPixelWidth,
				LTitleWidth + LGrid.CellPadding.Width
			);
		}

		protected virtual void InternalUpdateColumn()
		{
			FColumn.Title = FActualTitle;
			FColumn.Width = GetPixelWidth();
			FColumn.Visible = GetVisible();
		}

		protected void UpdateColumn()
		{
			if (Active)
			{
				InternalUpdateColumn();
				((Grid)Parent).UpdateLayout();
			}
		}

		// Node

		protected override void Activate()
		{
			SetColumn(CreateColumn());
			try
			{
				InternalUpdateColumn();
				if (Parent != null)
					GridControl.Columns.Insert(Parent.Children.IndexOf(this), FColumn);
				else
					GridControl.Columns.Add(FColumn);
				base.Activate();
			}
			catch
			{
				SetColumn(null);
				throw;
			}
		}
		
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				SetColumn(null);
			}
		}

		public override bool IsValidOwner(Type AOwnerType)
		{
			return typeof(IGrid).IsAssignableFrom(AOwnerType);
		}
	}

	public class TriggerColumn : GridColumn, ITriggerColumn
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}

		// Action

		protected IAction FAction;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Associated Action node.")]
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (FAction != value)
				{
					if (FAction != null)
					{
						FAction.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged -= new EventHandler(ActionTextChanged);
						FAction.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						FAction.Disposed -= new EventHandler(ActionDisposed);
					}
					FAction = value;
					if (FAction != null)
					{
						FAction.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged += new EventHandler(ActionTextChanged);
						FAction.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						FAction.Disposed += new EventHandler(ActionDisposed);
					}
					if (Active)
					{
						InternalUpdateEnabled();
						InternalUpdateText();
					}
				}
			}
		}
		
		private void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}
		
		private void ActionVisibleChanged(object sender, EventArgs e)
		{
			UpdateColumn();
		}

		// Enabled

		private bool FEnabled = true;
		[DefaultValue(true)]
		[Description("When false, the node is disabled.")]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (FEnabled != value)
				{
					FEnabled = value;
					if (Active)
						InternalUpdateEnabled();
				}
			}
		}

		public virtual bool GetEnabled()
		{
			return ( FAction == null ? false : FAction.GetEnabled() ) && FEnabled;
		}
		
		private void ActionEnabledChanged(object ASender, EventArgs AArgs)
		{
			if (Active)
				InternalUpdateEnabled();
		}
		
		protected virtual void InternalUpdateEnabled() 
		{
			if (Column != null)
				((DAE.Client.Controls.ActionColumn)Column).Enabled = GetEnabled();
		}

		// Text

		private string FText = String.Empty;
		[DefaultValue("")]
		[Description("A text string that will be used by this node.  If this is not set the text property of the action will be used.")]
		public virtual string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					if (Active)
						InternalUpdateText();
				}
			}
		}

		private void ActionTextChanged(object ASender, EventArgs AArgs)
		{
			if ((FText == String.Empty) && Active)
				InternalUpdateText();
		}

		public virtual string GetText()
		{
			if (Text != String.Empty)
				return Text;
			else
				return ( Action != null ? Action.Text : String.Empty );
		}

		protected virtual void InternalUpdateText() 
		{
			if (Column != null)
				((DAE.Client.Controls.ActionColumn)Column).ControlText = GetText();
		}

		// GridColumn

		public override bool GetVisible()
		{
			return base.GetVisible() && ((FAction == null) || FAction.Visible);
		}

		protected override DAE.Client.Controls.GridColumn CreateColumn()
		{
			return new DAE.Client.Controls.ActionColumn();
		}

		protected void ColumnClicked(object ASender, EventArgs AArgs)
		{
			try
			{
				if ((Action != null) && GetEnabled())
					Action.Execute(this, new EventParams());
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);	// Don't rethrow
			}
		}

		protected override void SetColumn(DAE.Client.Controls.GridColumn AValue)
		{
			if (Column != null)
			{
				Column.OnClick -= new EventHandler(ColumnClicked);
				((DAE.Client.Controls.ActionColumn)Column).OnExecuteAction -= new EventHandler(ColumnClicked);
			}
			base.SetColumn(AValue);
			if (AValue != null)
			{
				Column.OnClick += new EventHandler(ColumnClicked);
				((DAE.Client.Controls.ActionColumn)AValue).OnExecuteAction += new EventHandler(ColumnClicked);
			}
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			InternalUpdateEnabled();
			InternalUpdateText();
		}
	}

	public class SequenceColumn : GridColumn, ISequenceColumn
	{
		// GridColumn

		protected override DAE.Client.Controls.GridColumn CreateColumn()
		{
			return new DAE.Client.Controls.SequenceColumn();
		}

		protected virtual void SequenceChange
		(
			object ASender,
			DAE.Runtime.Data.Row AFromRow,
			DAE.Runtime.Data.Row AToRow,
			bool AAbove,
			EventArgs AArgs
		)
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			foreach (DAE.Schema.Column LColumn in AFromRow.DataType.Columns) 
			{
				LParams.Add(new DAE.Runtime.DataParam("AFromRow." + LColumn.Name, LColumn.DataType, DAE.Language.Modifier.In, AFromRow[LColumn.Name]));
				LParams.Add(new DAE.Runtime.DataParam("AToRow." + LColumn.Name, LColumn.DataType, DAE.Language.Modifier.In, AToRow[LColumn.Name]));
			}
			LParams.Add(new DAE.Runtime.DataParam("AAbove", Source.DataView.Process.DataTypes.SystemBoolean, DAE.Language.Modifier.In, new DAE.Runtime.Data.Scalar(Source.DataView.Process, Source.DataView.Process.DataTypes.SystemBoolean, AAbove)));
			HostNode.Session.ExecuteScript(FScript, LParams);
			Source.DataView.Refresh();
		}

		protected override void SetColumn(DAE.Client.Controls.GridColumn AValue)
		{
			if (Column != null)
				((DAE.Client.Controls.SequenceColumn)Column).OnSequenceChange -= new DAE.Client.Controls.SequenceChangeEventHandler(SequenceChange);
			base.SetColumn(AValue);
			if (Column != null)
				((DAE.Client.Controls.SequenceColumn)AValue).OnSequenceChange += new DAE.Client.Controls.SequenceChangeEventHandler(SequenceChange);
		}

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			((DAE.Client.Controls.SequenceColumn)Column).Image = FInternalImage;
		}

		private System.Drawing.Image FInternalImage = null;

		private string FImage = String.Empty;
		[DefaultValue(null)]
		[Description("An image to paint in each cell.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public string Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					if (Active)
						UpdateImage();
				}
			}
		}

		protected PipeRequest FImageRequest;

		private void ClearImage()
		{
			if (FInternalImage != null)
			{
				FInternalImage.Dispose();
				FInternalImage = null;
			}
		}

		private void CancelImageRequest()
		{
			if (FImageRequest != null)
			{
				HostNode.Pipe.CancelRequest(FImageRequest);
				FImageRequest = null;
			}
		}

		private void UpdateImage()
		{
			CancelImageRequest();
			if (FImage == String.Empty)
				ClearImage();
			else
			{
				// Queue up an asynchronous request
				FImageRequest = new PipeRequest(FImage, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				HostNode.Pipe.QueueRequest(FImageRequest);
			}
		}

		protected void ImageRead(PipeRequest ARequest, Pipe APipe)
		{
			if (Active)
				try
				{
					FImageRequest = null;
					try
					{
						if (ARequest.Result.IsNative)
						{
							byte[] LResultBytes = ARequest.Result.AsByteArray;
							FInternalImage = System.Drawing.Image.FromStream(new MemoryStream(LResultBytes, 0, LResultBytes.Length, false, true));
						}
						else
						{
							using (Stream LStream = ARequest.Result.OpenStream())
							{
								MemoryStream LCopyStream = new MemoryStream();
								StreamUtility.CopyStream(LStream, LCopyStream);
								FInternalImage = System.Drawing.Image.FromStream(LCopyStream);
							}
						}
					}
					catch
					{
						FInternalImage = ImageUtility.GetErrorImage();
					}
				}
				finally
				{
					UpdateColumn();
				}
		}

		protected void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			// TODO: On image error, somehow update the error (warnings) list for the form
			FImageRequest = null;
			if (FInternalImage != null)
				FInternalImage = ImageUtility.GetErrorImage();
			UpdateColumn();
		}

		// Script

		private string FScript = String.Empty;
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Description("The script that is executed when a row is relocated.  The source row (being dragged) values are available as AFromRow.XXX, the target row (dropped over) values are available as AToRow.XXX.  AAbove will be true if the source row is to be placed above the target row, otherwise it will be false.")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Script
		{
			get { return FScript; }
			set
			{
				if (FScript != value)
					FScript = (value == null ? String.Empty : value);
			}
		}

		// Node

		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				try
				{
					CancelImageRequest();
				}
				finally
				{
					ClearImage();
				}
			}
		}

		protected override void AfterActivate()
		{
			UpdateImage();
			base.AfterActivate();
		}
	}

	public class DataGridColumn : GridColumn, IDataGridColumn
	{
		// ColumnName

		private string FColumnName = String.Empty;
		[TypeConverter(typeof(ColumnNameConverter))]
		[DefaultValue("")]
		[Description("The name of the column that this node will be applied to.")]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FColumnName != value)
				{
					FColumnName = value == null ? String.Empty : value;
					UpdateColumn();
				}
			}
		}

		// GridColumn

		protected override DAE.Client.Controls.GridColumn CreateColumn()
		{
			return new DAE.Client.Controls.TextColumn();
		}

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			((DAE.Client.Controls.DataColumn)Column).ColumnName = FColumnName;
		}
	}

	public class TextColumn : DataGridColumn, ITextColumn
	{
		// TextAlignment

		private HorizontalAlignment FTextAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("Sets the horizontal alignment of the data inside of the column.")]
		public virtual HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set
			{
				if (FTextAlignment != value)
				{
					FTextAlignment = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// VerticalAlignment

		private VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("Sets the vertical alignment of the data inside of the column.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		//WordWrap

		private bool FWordWrap = false;
		[DefaultValue(false)]
		[Description("Indicates if text is automatically word-wrapped for multi-line rows.")]
		public bool WordWrap
		{
			get { return FWordWrap; }
			set 
			{
				if (FWordWrap != value)
				{
					FWordWrap = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		//VerticalText

		private bool FVerticalText = false;
		[DefaultValue(false)]
		[Description("Indicates if text is to be painted vertical or horizontal.")]
		public bool VerticalText
		{
			get { return FVerticalText; }
			set
			{
				if (FVerticalText != value)
				{
					FVerticalText = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// MaxRows

		private int FMaxRows = -1;
		[DefaultValue(-1)]
		[Description("If -1 then calculated, else restrict the row height to not exceed MaxRows tall.")]
		public int MaxRows
		{
			get { return FMaxRows; }
			set
			{
				if (FMaxRows != value)
				{
					FMaxRows = value;
					UpdateColumn();
				}
			}
		}

		private int RowsToPixelHeight(int ARows)
		{
			return ARows < 0 ? ARows : Column.MinRowHeight * ARows;
		}

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			Column.HorizontalAlignment = (WinForms.HorizontalAlignment)FTextAlignment;
			((DAE.Client.Controls.TextColumn)Column).WordWrap = FWordWrap;
			((DAE.Client.Controls.TextColumn)Column).VerticalText = FVerticalText;
			Column.VerticalAlignment = (DAE.Client.Controls.VerticalAlignment)FVerticalAlignment;
			Column.MaxRowHeight = RowsToPixelHeight(FMaxRows);
		}
	}

	public class ImageColumn : DataGridColumn, IImageColumn
	{
		// HorizontalAlignment

		private HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Center;
		[DefaultValue(HorizontalAlignment.Center)]
		[Description("Sets the horizontal alignment of the image.")]
		public virtual HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set
			{
				if (FHorizontalAlignment != value)
				{
					FHorizontalAlignment = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// MaxRowHeight

		private int FMaxRowHeight = -1;
		[DefaultValue(-1)]
		[Description("Restricts the height of rows in pixels not to exceed MaxRowHeight tall. If -1 then calculated.")]
		public int MaxRowHeight
		{
			get { return FMaxRowHeight; }
			set
			{
				if (FMaxRowHeight != value)
				{
					FMaxRowHeight = value;
					UpdateColumn();
				}
			}
		}

		// GridColumn

		protected override DAE.Client.Controls.GridColumn CreateColumn()
		{
			return new DAE.Client.Controls.ImageColumn();
		}

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			Column.HorizontalAlignment = (WinForms.HorizontalAlignment)FHorizontalAlignment;
			Column.MaxRowHeight = FMaxRowHeight;
		}
	}

	public class CheckBoxColumn : DataGridColumn, ICheckBoxColumn
	{
		// ReadOnly

		private bool FReadOnly = true;

		[Browsable(true)]
		[DefaultValue(true)]
		[Description("When true the control will not modify the data in the data source.")]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set
			{
				if (FReadOnly != value)
				{
					FReadOnly = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// GridColumn

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			DAE.Client.Controls.CheckBoxColumn LColumn = (DAE.Client.Controls.CheckBoxColumn)Column;
			LColumn.ReadOnly = ReadOnly;
			LColumn.AutoUpdateInterval = 200;
		}

		protected override DAE.Client.Controls.GridColumn CreateColumn()
		{
			return new DAE.Client.Controls.CheckBoxColumn();
		}
	}

	/// <remarks> The Grid node class requires it's children to be GridColumn descendants. </remarks>
	[DesignerImage("Image('Frontend', 'Nodes.Grid')")]
	[DesignerCategory("Data Controls")]
	public class Grid : ControlElement, IGrid
	{
		public const int CMinWidth = 100;
		public const int CMinHeight = 50;

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			OnDoubleClick = null;
		}

		// GridControl

		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DAE.Client.Controls.DBGrid GridControl
		{
			get { return (DAE.Client.Controls.DBGrid)Control; }
		}

		// RowCount

		private int FRowCount = 10;
		[DefaultValue(10)]
		[Description("The number of rows to show.  This directly controls the height of the grid.")]
		public int RowCount
		{
			get { return FRowCount; }
			set
			{
				if (FRowCount != value)
				{
					FRowCount = value;
					UpdateLayout();
				}
			}
		}
		
		// ColumnCount
		
		private int FColumnCount = -1;
		[DefaultValue(-1)]
		[Description("The natural number of columns to show without scrolling (if possible).")]
		public int ColumnCount
		{
			get { return FColumnCount; }
			set
			{
				if (value < 1)
					value = -1;
				if (FColumnCount != value)
				{
					FColumnCount = value;
					UpdateLayout();
				}
			}
		}

// TODO: This cannot be put in until it is in the interface and hence the web client (common GridLines enum needed)
//		// GridLines
//
//		private DAE.Client.Controls.GridLines FGridLines;
//		[DefaultValue(DAE.Client.Controls.GridLines.Both)]
//		[Description("Paintable grid lines.")]
//		public DAE.Client.Controls.GridLines GridLines
//		{
//			get { return FGridLines; }
//			set
//			{
//				if (FGridLines != value)
//				{
//					FGridLines = value;
//					if (Active)
//						InternalUpdateGrid();
//				}	
//			}
//		}

		// OnDoubleClick
		
		private IAction FOnDoubleClick;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Action that takes place when the grid is double clicked.")]
		public IAction OnDoubleClick 
		{ 
			get { return FOnDoubleClick; }
			set
			{
				if (FOnDoubleClick != value)
				{
					if (FOnDoubleClick != null)
						FOnDoubleClick.Disposed -= new EventHandler(DoubleClickActionDisposed);
					FOnDoubleClick = value;
					if (FOnDoubleClick != null)
						FOnDoubleClick.Disposed += new EventHandler(DoubleClickActionDisposed);
				}
			}
		}

		private void DoubleClickActionDisposed(object ASender, EventArgs AArgs)
		{
			OnDoubleClick = null;
		}

		private void GridDoubleClicked(object ASender, EventArgs AArgs)
		{
			if (OnDoubleClick != null)
				OnDoubleClick.Execute(this, new EventParams());
			else
				((IInterface)FindParent(typeof(IInterface))).PerformDefaultAction();
		}

		// AverageCharPixelWidth

		private int FAverageCharPixelWidth;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public int AverageCharPixelWidth
		{
			get { return FAverageCharPixelWidth; }
		}
		
		// DataElement

//	This shouldn't be necessary because changing the children should cause an updatelayout
//		protected override void InternalUpdateSource()
//		{
//			base.InternalUpdateSource();
//			UpdateLayout();
//		}

		// ControlElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBGrid();
		}

		protected virtual void InternalUpdateGrid()
		{
//			See comment for GridLines property
//			GridControl.GridLines = FGridLines;
		}

		protected override void InitializeControl()
		{
			GridControl.BackColor = ((Session)HostNode.Session).Theme.GridColor;
			GridControl.FixedColor = ((Session)HostNode.Session).Theme.FixedColor;
			GridControl.HighlightColor = ((Session)HostNode.Session).Theme.HighlightColor;
			GridControl.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			GridControl.DoubleClick += new EventHandler(GridDoubleClicked);
			FAverageCharPixelWidth = Element.GetAverageCharPixelWidth(GridControl);
			InternalUpdateGrid();
			base.InitializeControl();
		}

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(GridColumn).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}
		
		// Element

		protected override Size InternalMinSize
		{
			get
			{
				return new Size(GridControl.BorderSize.Width, GridControl.HeaderHeight + GridControl.MinRowHeight);
			}
		}
		
		protected override Size InternalMaxSize
		{
            get { return FUseNaturalMaxWidth ? new Size(InternalNaturalSize.Width, WinForms.Screen.FromControl(Control).WorkingArea.Size.Width) : WinForms.Screen.FromControl(Control).WorkingArea.Size; }
		}

        [DefaultValue(false)]
        [Description("Use Natural Width as the Max Width.")]
        private bool FUseNaturalMaxWidth;
        public bool UseNaturalMaxWidth
        {
            get { return FUseNaturalMaxWidth; }
            set { FUseNaturalMaxWidth = value; }
        }

		protected override Size InternalNaturalSize
		{
			get
			{
				int LPixelWidth = 1;
				int LColumnCount = FColumnCount == -1 ? GridControl.Columns.Count : Math.Min(GridControl.Columns.Count, FColumnCount);
				for (int i = 0; i < LColumnCount; i++)
				{
					DAE.Client.Controls.GridColumn LColumn = GridControl.Columns[i];
					if (LColumn.Visible)
						LPixelWidth += LColumn.Width + 4;
				}
				return MinSize + new Size(LPixelWidth, (GridControl.MinRowHeight * RowCount));
			}
		}
	}   
}