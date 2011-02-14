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

		private DAE.Client.Controls.GridColumn _column;
		[Browsable(false)]
		public DAE.Client.Controls.GridColumn Column
		{
			get { return _column; }
		}

		protected abstract DAE.Client.Controls.GridColumn CreateColumn();

		protected virtual void SetColumn(DAE.Client.Controls.GridColumn value)
		{
			if (_column != null)
			{
				_column.Dispose();
				_column = null;
			}
			_column = value;
		}

		// Hint

		// BTR -> This doesn't do anything, hint is an Dataphor common property		
		private string _hint = String.Empty;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public string Hint
		{
			get { return _hint; }
			set { _hint = value; }
		}

		// Visible

		private bool _visible = true;
		[DefaultValue(true)]
		[Description("When set to false the menu will not be shown.")]
		public bool Visible
		{
			get { return _visible; }
			set
			{
				if (_visible != value)
				{
					_visible = value;
					UpdateColumn();
				}
			}
		}

		public virtual bool GetVisible() 
		{
			return _visible;
		}

		// Title

		private string _actualTitle = String.Empty;
		private string _title = String.Empty;
		[DefaultValue("")]
		[Description("This text will be displayed for this grid column.")]
		public string Title
		{
			get { return _title; }
			set
			{
				if (_title != value)
				{
					_title = value == null ? String.Empty : value;
					_actualTitle = 
						(
							_title.Length > 0 
								? 
								(
									_title[0] == '~' 
										? _title.Substring(1) 
										: TitleUtility.RemoveAccellerators(_title)
								) 
								: String.Empty
						);

					UpdateColumn();
				}
			}
		}

		// Width

		private int _width = 10;
		[DefaultValue(10)]
		[Description("The width, in characters, that this column will be initially displayed as.")]
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					_width = value;
					UpdateColumn();
				}
			}
		}

		[Browsable(false)]
		protected internal int GetPixelWidth()
		{
			int titleWidth;
			DAE.Client.Controls.DBGrid grid = ((Grid)Parent).GridControl;
			using (System.Drawing.Graphics graphics = grid.CreateGraphics())
			{
				titleWidth = (int)graphics.MeasureString(_actualTitle, grid.Font).Width;
			}
			return Math.Max
			(
				_width * ((Grid)Parent).AverageCharPixelWidth,
				titleWidth + grid.CellPadding.Width
			);
		}

		protected virtual void InternalUpdateColumn()
		{
			_column.Title = _actualTitle;
			_column.Width = GetPixelWidth();
			_column.Visible = GetVisible();
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
					GridControl.Columns.Insert(Parent.Children.IndexOf(this), _column);
				else
					GridControl.Columns.Add(_column);
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

		public override bool IsValidOwner(Type ownerType)
		{
			return typeof(IGrid).IsAssignableFrom(ownerType);
		}
	}

	public class TriggerColumn : GridColumn, ITriggerColumn
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}

		// Action

		protected IAction _action;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Associated Action node.")]
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (_action != value)
				{
					if (_action != null)
					{
						_action.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged -= new EventHandler(ActionTextChanged);
						_action.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						_action.Disposed -= new EventHandler(ActionDisposed);
					}
					_action = value;
					if (_action != null)
					{
						_action.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged += new EventHandler(ActionTextChanged);
						_action.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						_action.Disposed += new EventHandler(ActionDisposed);
					}
					if (Active)
					{
						InternalUpdateEnabled();
						InternalUpdateText();
					}
				}
			}
		}
		
		private void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}
		
		private void ActionVisibleChanged(object sender, EventArgs e)
		{
			UpdateColumn();
		}

		// Enabled

		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("When false, the node is disabled.")]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					if (Active)
						InternalUpdateEnabled();
				}
			}
		}

		public virtual bool GetEnabled()
		{
			return ( _action == null ? false : _action.GetEnabled() ) && _enabled;
		}
		
		private void ActionEnabledChanged(object sender, EventArgs args)
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

		private string _text = String.Empty;
		[DefaultValue("")]
		[Description("A text string that will be used by this node.  If this is not set the text property of the action will be used.")]
		public virtual string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					if (Active)
						InternalUpdateText();
				}
			}
		}

		private void ActionTextChanged(object sender, EventArgs args)
		{
			if ((_text == String.Empty) && Active)
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
			return base.GetVisible() && ((_action == null) || _action.Visible);
		}

		protected override DAE.Client.Controls.GridColumn CreateColumn()
		{
			return new DAE.Client.Controls.ActionColumn();
		}

		protected void ColumnClicked(object sender, EventArgs args)
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

		protected override void SetColumn(DAE.Client.Controls.GridColumn value)
		{
			if (Column != null)
			{
				Column.OnClick -= new EventHandler(ColumnClicked);
				((DAE.Client.Controls.ActionColumn)Column).OnExecuteAction -= new EventHandler(ColumnClicked);
			}
			base.SetColumn(value);
			if (value != null)
			{
				Column.OnClick += new EventHandler(ColumnClicked);
				((DAE.Client.Controls.ActionColumn)value).OnExecuteAction += new EventHandler(ColumnClicked);
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
			object sender,
			DAE.Runtime.Data.Row fromRow,
			DAE.Runtime.Data.Row toRow,
			bool above,
			EventArgs args
		)
		{
			SequenceColumnUtility.SequenceChange(HostNode.Session, Source, _shouldEnlist, fromRow, toRow, above, _script);
		}

		private bool _shouldEnlist = true;
		[DefaultValue(true)]
		[Description("Enlist with the application transaction when running Script, if there is one active.")]
		public bool ShouldEnlist
		{
			get { return _shouldEnlist; }
			set { _shouldEnlist = value; }
		}

		protected override void SetColumn(DAE.Client.Controls.GridColumn value)
		{
			if (Column != null)
				((DAE.Client.Controls.SequenceColumn)Column).OnSequenceChange -= new DAE.Client.Controls.SequenceChangeEventHandler(SequenceChange);
			base.SetColumn(value);
			if (Column != null)
				((DAE.Client.Controls.SequenceColumn)value).OnSequenceChange += new DAE.Client.Controls.SequenceChangeEventHandler(SequenceChange);
		}

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			((DAE.Client.Controls.SequenceColumn)Column).Image = _internalImage;
		}

		private System.Drawing.Image _internalImage = null;

		private string _image = String.Empty;
		[DefaultValue(null)]
		[Description("An image to paint in each cell.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Image")]
		public string Image
		{
			get { return _image; }
			set
			{
				if (_image != value)
				{
					_image = value;
					if (Active)
						UpdateImage();
				}
			}
		}

		protected PipeRequest _imageRequest;

		private void ClearImage()
		{
			if (_internalImage != null)
			{
				_internalImage.Dispose();
				_internalImage = null;
			}
		}

		private void CancelImageRequest()
		{
			if (_imageRequest != null)
			{
				HostNode.Pipe.CancelRequest(_imageRequest);
				_imageRequest = null;
			}
		}

		private void UpdateImage()
		{
			CancelImageRequest();
			if (_image == String.Empty)
				ClearImage();
			else
			{
				// Queue up an asynchronous request
				_imageRequest = new PipeRequest(_image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				HostNode.Pipe.QueueRequest(_imageRequest);
			}
		}

		protected void ImageRead(PipeRequest request, Pipe pipe)
		{
			if (Active)
				try
				{
					_imageRequest = null;
					try
					{
						if (request.Result.IsNative)
						{
							byte[] resultBytes = request.Result.AsByteArray;
							_internalImage = System.Drawing.Image.FromStream(new MemoryStream(resultBytes, 0, resultBytes.Length, false, true));
						}
						else
						{
							using (Stream stream = request.Result.OpenStream())
							{
								MemoryStream copyStream = new MemoryStream();
								StreamUtility.CopyStream(stream, copyStream);
								_internalImage = System.Drawing.Image.FromStream(copyStream);
							}
						}
					}
					catch
					{
						_internalImage = ImageUtility.GetErrorImage();
					}
				}
				finally
				{
					UpdateColumn();
				}
		}

		protected void ImageError(PipeRequest request, Pipe pipe, Exception exception)
		{
			// TODO: On image error, somehow update the error (warnings) list for the form
			_imageRequest = null;
			if (_internalImage != null)
				_internalImage = ImageUtility.GetErrorImage();
			UpdateColumn();
		}

		// Script

		private string _script = String.Empty;
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Description("The script that is executed when a row is relocated.  The source row (being dragged) values are available as AFromRow.XXX, the target row (dropped over) values are available as AToRow.XXX.  AAbove will be true if the source row is to be placed above the target row, otherwise it will be false.")]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string Script
		{
			get { return _script; }
			set
			{
				if (_script != value)
					_script = (value == null ? String.Empty : value);
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

		private string _columnName = String.Empty;
		[TypeConverter(typeof(ColumnNameConverter))]
		[DefaultValue("")]
		[Description("The name of the column that this node will be applied to.")]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (_columnName != value)
				{
					_columnName = value == null ? String.Empty : value;
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
			((DAE.Client.Controls.DataColumn)Column).ColumnName = _columnName;
		}
	}

	public class TextColumn : DataGridColumn, ITextColumn
	{
		// TextAlignment

		private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("Sets the horizontal alignment of the data inside of the column.")]
		public virtual HorizontalAlignment TextAlignment
		{
			get { return _textAlignment; }
			set
			{
				if (_textAlignment != value)
				{
					_textAlignment = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// VerticalAlignment

		private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("Sets the vertical alignment of the data inside of the column.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		//WordWrap

		private bool _wordWrap = false;
		[DefaultValue(false)]
		[Description("Indicates if text is automatically word-wrapped for multi-line rows.")]
		public bool WordWrap
		{
			get { return _wordWrap; }
			set 
			{
				if (_wordWrap != value)
				{
					_wordWrap = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		//VerticalText

		private bool _verticalText = false;
		[DefaultValue(false)]
		[Description("Indicates if text is to be painted vertical or horizontal.")]
		public bool VerticalText
		{
			get { return _verticalText; }
			set
			{
				if (_verticalText != value)
				{
					_verticalText = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// MaxRows

		private int _maxRows = -1;
		[DefaultValue(-1)]
		[Description("If -1 then calculated, else restrict the row height to not exceed MaxRows tall.")]
		public int MaxRows
		{
			get { return _maxRows; }
			set
			{
				if (_maxRows != value)
				{
					_maxRows = value;
					UpdateColumn();
				}
			}
		}

		private int RowsToPixelHeight(int rows)
		{
			return rows < 0 ? rows : Column.MinRowHeight * rows;
		}

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			Column.HorizontalAlignment = (WinForms.HorizontalAlignment)_textAlignment;
			((DAE.Client.Controls.TextColumn)Column).WordWrap = _wordWrap;
			((DAE.Client.Controls.TextColumn)Column).VerticalText = _verticalText;
			Column.VerticalAlignment = (DAE.Client.Controls.VerticalAlignment)_verticalAlignment;
			Column.MaxRowHeight = RowsToPixelHeight(_maxRows);
		}
	}

	public class ImageColumn : DataGridColumn, IImageColumn
	{
		// HorizontalAlignment

		private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;
		[DefaultValue(HorizontalAlignment.Center)]
		[Description("Sets the horizontal alignment of the image.")]
		public virtual HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set
			{
				if (_horizontalAlignment != value)
				{
					_horizontalAlignment = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// MaxRowHeight

		private int _maxRowHeight = -1;
		[DefaultValue(-1)]
		[Description("Restricts the height of rows in pixels not to exceed MaxRowHeight tall. If -1 then calculated.")]
		public int MaxRowHeight
		{
			get { return _maxRowHeight; }
			set
			{
				if (_maxRowHeight != value)
				{
					_maxRowHeight = value;
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
			Column.HorizontalAlignment = (WinForms.HorizontalAlignment)_horizontalAlignment;
			Column.MaxRowHeight = _maxRowHeight;
		}
	}

	public class CheckBoxColumn : DataGridColumn, ICheckBoxColumn
	{
		// ReadOnly

		private bool _readOnly = true;

		[Browsable(true)]
		[DefaultValue(true)]
		[Description("When true the control will not modify the data in the data source.")]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set
			{
				if (_readOnly != value)
				{
					_readOnly = value;
					if (Active)
						InternalUpdateColumn();
				}
			}
		}

		// GridColumn

		protected override void InternalUpdateColumn()
		{
			base.InternalUpdateColumn();
			DAE.Client.Controls.CheckBoxColumn column = (DAE.Client.Controls.CheckBoxColumn)Column;
			column.ReadOnly = ReadOnly;
			column.AutoUpdateInterval = 200;
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
		public const int MinWidth = 100;
		public const int MinHeight = 50;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
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

		private int _rowCount = 10;
		[DefaultValue(10)]
		[Description("The number of rows to show.  This directly controls the height of the grid.")]
		public int RowCount
		{
			get { return _rowCount; }
			set
			{
				if (_rowCount != value)
				{
					_rowCount = value;
					UpdateLayout();
				}
			}
		}
		
		// ColumnCount
		
		private int _columnCount = -1;
		[DefaultValue(-1)]
		[Description("The natural number of columns to show without scrolling (if possible).")]
		public int ColumnCount
		{
			get { return _columnCount; }
			set
			{
				if (value < 1)
					value = -1;
				if (_columnCount != value)
				{
					_columnCount = value;
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
		
		private IAction _onDoubleClick;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Action that takes place when the grid is double clicked.")]
		public IAction OnDoubleClick 
		{ 
			get { return _onDoubleClick; }
			set
			{
				if (_onDoubleClick != value)
				{
					if (_onDoubleClick != null)
						_onDoubleClick.Disposed -= new EventHandler(DoubleClickActionDisposed);
					_onDoubleClick = value;
					if (_onDoubleClick != null)
						_onDoubleClick.Disposed += new EventHandler(DoubleClickActionDisposed);
				}
			}
		}

		private void DoubleClickActionDisposed(object sender, EventArgs args)
		{
			OnDoubleClick = null;
		}

		private void GridDoubleClicked(object sender, EventArgs args)
		{
			if (OnDoubleClick != null)
				OnDoubleClick.Execute(this, new EventParams());
			else
				((IInterface)FindParent(typeof(IInterface))).PerformDefaultAction();
		}

		// AverageCharPixelWidth

		private int _averageCharPixelWidth;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public int AverageCharPixelWidth
		{
			get { return _averageCharPixelWidth; }
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
			_averageCharPixelWidth = Element.GetAverageCharPixelWidth(GridControl);
			InternalUpdateGrid();
			base.InitializeControl();
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(GridColumn).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
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
            get 
            { 
				return _useNaturalMaxWidth 
					? new Size(InternalNaturalSize.Width, WinForms.Screen.FromControl(Control).WorkingArea.Size.Width) 
					: WinForms.Screen.FromControl(Control).WorkingArea.Size; 
			}
		}

        [DefaultValue(false)]
        [Description("Use Natural Width as the Max Width, otherwise the grid will us all available space.")]
        private bool _useNaturalMaxWidth;
        public bool UseNaturalMaxWidth
        {
            get { return _useNaturalMaxWidth; }
            set 
            { 
				if (_useNaturalMaxWidth != value)
				{
					_useNaturalMaxWidth = value;
					UpdateLayout();
				}
			}
        }

		protected override Size InternalNaturalSize
		{
			get
			{
				int pixelWidth = 1;
				int columnCount = _columnCount == -1 ? GridControl.Columns.Count : Math.Min(GridControl.Columns.Count, _columnCount);
				for (int i = 0; i < columnCount; i++)
				{
					DAE.Client.Controls.GridColumn column = GridControl.Columns[i];
					if (column.Visible)
						pixelWidth += column.Width + 4;
				}
				return MinSize + new Size(pixelWidth, (GridControl.MinRowHeight * RowCount));
			}
		}
	}   
}