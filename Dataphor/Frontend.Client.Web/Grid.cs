/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;
using System.Web.UI;
using System.Drawing;
using System.ComponentModel;
using System.Collections;
using System.IO;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public abstract class GridColumn : Node, IWebGridColumn
	{
		// Hint

		// Does nothing (common property)
		private string _hint = String.Empty;
		public string Hint
		{
			get { return _hint; }
			set { _hint = value; }
		}

		// Title

		private string _title = String.Empty;
		public string Title
		{
			get { return _title; }
			set { _title = value == null ? String.Empty : value; }
		}

		public virtual string GetTitle()
		{
			return _title;
		}
		
		// Width

		private int _width = 10;
		public int Width
		{
			get { return _width; }
			set { _width = value; }
		}

		// MinRowHeight

		private int _minRowHeight = -1;
		public int MinRowHeight
		{
			get { return _minRowHeight; }
			set { _minRowHeight = value; }
		}

		// MaxRowHeight

		private int _maxRowHeight = -1;
		public int MaxRowHeight
		{
			get { return _maxRowHeight; }
			set { _maxRowHeight = value; }
		}

		// Visible

		private bool _visible = true;
		public bool Visible
		{
			get { return _visible; }
			set { _visible = value; }
		}

		public virtual bool GetVisible()
		{
			return _visible;
		}

		// ParentGrid

		public IWebGrid ParentGrid
		{
			get { return (IWebGrid)Parent; }
		}

		// Render

		public virtual void RenderCell(HtmlTextWriter writer, DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex)
		{
			if (GetVisible())
				InternalRenderCell(writer, currentRow, isActiveRow, rowIndex);
		}

		public abstract void InternalRenderCell(HtmlTextWriter writer, DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex);

		public virtual void RenderHeader(HtmlTextWriter writer) 
		{
			if (GetVisible())
				InternalRenderHeader(writer);
		}

		public virtual void InternalRenderHeader(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridheadercell");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			if (_title == String.Empty)
				writer.Write("&nbsp;");
			else
				writer.Write(HttpUtility.HtmlEncode(Title));
			writer.RenderEndTag();
		}
	}

	public abstract class DataGridColumn : GridColumn, IDataGridColumn, IWebHandler
	{
		public DataGridColumn()
		{
			_headerID = Session.GenerateID();
		}

		// ColumnName

		private string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = value == null ? String.Empty : value; }
		}

		// HeaderID

		private string _headerID;
		public string HeaderID { get { return _headerID; } }

		// GridColumn

		public override string GetTitle()
		{
			if (Title == String.Empty)
				return ColumnName;
			else
				return base.GetTitle();
		}

		protected virtual string GetClass(DAE.Runtime.Data.IRow currentRow, bool isActiveRow)
		{
			string classValue;
			if (isActiveRow)
				classValue = "gridcellcurrent";
			else
				classValue = "gridcell";
			if ((ColumnName != String.Empty) && !currentRow.HasValue(ColumnName))
				classValue = classValue + "null";
			return classValue;
		}

		public override void InternalRenderHeader(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridheaderdatacell");
			writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, _headerID));
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			string title = GetTitle();
			if (title == String.Empty)
				writer.Write("&nbsp;");
			else
				writer.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(title)));

			DAE.Client.DataLink link = ParentGrid.DataLink;
			if (link.Active)
			{
				DAE.Client.TableDataSet dataSet = ParentGrid.DataLink.DataSet as DAE.Client.TableDataSet;
				if (dataSet != null)
				{
					Schema.Order order = dataSet.Order;
					if ((order != null) && (order.Columns.IndexOf(ColumnName) > -1))
					{
						writer.Write("&nbsp;");
						if (((Schema.OrderColumn)order.Columns[ColumnName]).Ascending)
							writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/downarrow.gif");
						else
							writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/uparrow.gif");
						writer.RenderBeginTag(HtmlTextWriterTag.Img);
						writer.RenderEndTag();
						writer.Write("&nbsp;");
					}
				}
			}
			writer.RenderEndTag();
		}

		// IWebHandler

		public virtual bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, _headerID))
			{
				if (InKey() || InOrder())
					ChangeOrderTo();
				return true;
			}
			else
				return false;
		}

		// BEGIN ADAPTED FROM WINDOWS GRID

		protected Schema.Order FindOrderForColumn()
		{
			DAE.Client.TableDataSet dataSet = ParentGrid.DataLink.DataSet as DAE.Client.TableDataSet;
			if (dataSet != null)
			{
				// Returns the order that has the given column as the first column
				if ((dataSet.Order != null) && (dataSet.Order.Columns.Count >= 1) && (dataSet.Order.Columns[0].Column.Name == ColumnName))
					return dataSet.Order;

				foreach (Schema.Order order in ParentGrid.DataLink.DataSet.TableVar.Orders)
					if ((order.Columns.Count >= 1) && (order.Columns[0].Column.Name == ColumnName))
						return order;

				foreach (Schema.Key key in ParentGrid.DataLink.DataSet.TableVar.Keys)
					if (!key.IsSparse && (key.Columns.Count >= 1) && (key.Columns[0].Name == ColumnName))
						return new Schema.Order(key);
			}		
			return null;
		}

		protected virtual void ChangeOrderTo()
		{
			if (ParentGrid.DataLink.Active)
			{
				DAE.Client.TableDataSet dataSet = ParentGrid.DataLink.DataSet as DAE.Client.TableDataSet;
				if (dataSet != null)
				{
					Schema.Order order = FindOrderForColumn();
					if (order == null)
						dataSet.OrderString = "order { " + ColumnName + " asc }";
					else
					{
						Schema.Order currentOrder = dataSet.Order;
						int currentColumnIndex = currentOrder == null ? -1 : currentOrder.Columns.IndexOf(ColumnName);

						bool descending = (currentColumnIndex >= 0) && currentOrder.Columns[currentColumnIndex].Ascending;
						if (!descending ^ order.Columns[ColumnName].Ascending)
							order = new Schema.Order(order, true);

						dataSet.Order = order;
					}
				}
			}
		}

		private bool InKey()
		{
			DAE.Client.DataLink link = ParentGrid.DataLink;
			if (link.Active)
				foreach (Schema.Key key in link.DataSet.TableVar.Keys)
					if (!key.IsSparse && (key.Columns.IndexOfName(ColumnName) >= 0))
						return true;
			return false;
		}

		private bool InOrder()
		{
			DAE.Client.DataLink link = ParentGrid.DataLink;
			if (link.Active)
				foreach (Schema.Order order in link.DataSet.TableVar.Orders)
					if (order.Columns.IndexOf(ColumnName) >= 0)
						return true;
			return false;
		}

		// END ADAPTED FROM WINDOWS GRID
	}

	public class TextColumn : DataGridColumn, ITextColumn
	{
		// TextAlignment

		private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
		public HorizontalAlignment TextAlignment
		{
			get { return _textAlignment; }
			set { _textAlignment = value; }
		}

		// VerticalAlignment

		private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set { _verticalAlignment = value; }
		}

		// MaxRows

		private int _maxRows = -1;
		public int MaxRows
		{
			get { return _maxRows; }
			set { _maxRows = value; }
		}

		// WordWrap

		private bool _wordWrap = false;
		public bool WordWrap
		{
			get { return _wordWrap; }
			set { _wordWrap = value; }
		}

		// VerticalText

		private bool _verticalText = false;
		public bool VerticalText
		{
			get { return _verticalText; }
			set { _verticalText = value; }
		}

		public override void InternalRenderCell(HtmlTextWriter writer, Alphora.Dataphor.DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex)
		{
			string tempValue;
			if ((ColumnName == String.Empty) || !currentRow.HasValue(ColumnName))
				tempValue = String.Empty;
			else
				tempValue = ((DAE.Runtime.Data.Scalar)currentRow.GetValue(ColumnName)).AsDisplayString;
			writer.AddAttribute(HtmlTextWriterAttribute.Class, GetClass(currentRow, isActiveRow));

			if (!_wordWrap)
				writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);

			writer.AddAttribute(HtmlTextWriterAttribute.Align, TextAlignment.ToString());
			writer.AddAttribute(HtmlTextWriterAttribute.Valign, VerticalAlignment.ToString());
			if (_verticalText)
				writer.AddAttribute(HtmlTextWriterAttribute.Style, "mso-rotate:-90");
			if (_maxRows >= 1)
				writer.AddAttribute(HtmlTextWriterAttribute.Rows, _maxRows.ToString());
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			if (tempValue == String.Empty)
				writer.Write("&nbsp;");
			else
				writer.Write(HttpUtility.HtmlEncode(Session.TruncateTitle(tempValue, Width)).Replace("\n","<br>"));

			writer.RenderEndTag();
		}
	}

	public class TriggerColumn : GridColumn, ITriggerColumn, IWebHandler
	{
		public TriggerColumn()
		{
			_triggerID = Session.GenerateID();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}

		// Action

		private IAction _action;
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (value != _action)
				{
					if (_action != null)
						_action.Disposed -= new EventHandler(ActionDisposed);
					_action = value;
					if (_action != null)
						_action.Disposed += new EventHandler(ActionDisposed);
				}
			}
		}

		private void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}

		public virtual bool GetEnabled()
		{
			return (_action != null) && _action.GetEnabled();
		}

		// Text

		private string _text = String.Empty;

		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		public virtual string GetText()
		{
			if ((_text != String.Empty) || (_action == null))
				return _text;
			else
				return _action.GetText();
		}

		// TriggerID

		private string _triggerID;
		public string TriggerID { get { return _triggerID; } }

		// GridColumn

		public override void InternalRenderCell(HtmlTextWriter writer, Alphora.Dataphor.DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex)
		{
			string classValue;
			if (isActiveRow)
				classValue = "gridcellcurrent";
			else
				classValue = "gridcell";
			writer.AddAttribute(HtmlTextWriterAttribute.Class, classValue);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "trigger");
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
			string hint = String.Empty;
			if (!GetEnabled())
				writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
			else
				hint = Action.Hint;
			if (hint != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
			writer.AddAttribute(HtmlTextWriterAttribute.Value, Session.RemoveAccellerator(GetText()), true);
			writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit({0}?ActionID={1}&RowIndex={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], _triggerID, rowIndex.ToString()));
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();

			writer.RenderEndTag();
		}

		// IWebHandler

		public virtual bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, _triggerID))
			{
				string rowIndex = context.Request.QueryString["RowIndex"];
				if (rowIndex != null)
				{
					ParentGrid.MoveTo(Int32.Parse(rowIndex));
					if (GetEnabled())
						Action.Execute();
				}
				return true;
			}
			else
				return false;
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((_action == null) || _action.Visible);
		}
	}

	public class ImageColumn : DataGridColumn, IImageColumn
	{
		// HorizontalAlignment

		private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;
		public virtual HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set { _horizontalAlignment = value; }
		}

		// ImageHandlerID

		private string _imageHandlerID;
		public string ImageHandlerID { get { return _imageHandlerID; } }

		// GridColumn

		public override void InternalRenderCell(HtmlTextWriter writer, Alphora.Dataphor.DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, GetClass(currentRow, isActiveRow));
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			writer.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?HandlerID=" + _imageHandlerID + "&RowIndex=" + rowIndex);
			if (MaxRowHeight > 0)
				writer.AddAttribute(HtmlTextWriterAttribute.Height, MaxRowHeight.ToString());
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			writer.RenderEndTag();
		}

		private void LoadImage(HttpContext context, string iD, Stream stream)
		{
			string rowIndex = context.Request.QueryString["RowIndex"];
			if ((rowIndex != String.Empty) && (ColumnName != String.Empty))
			{
				DAE.Runtime.Data.IRow row = ParentGrid.DataLink.Buffer(Int32.Parse(rowIndex));
				if ((row != null) && row.HasValue(ColumnName))
					using (Stream source = row.GetValue(ColumnName).OpenStream())
					{
						StreamUtility.CopyStream(source, stream);
					}
			}
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			_imageHandlerID = WebSession.ImageCache.RegisterImageHandler(new LoadImageHandler(LoadImage));
		}

		protected override void Deactivate()
		{
			try
			{
				WebSession.ImageCache.UnregisterImageHandler(_imageHandlerID);
			}
			finally
			{
				base.Deactivate();
			}
		}

	}

	public class CheckBoxColumn : DataGridColumn, ICheckBoxColumn, IWebHandler
	{
		public CheckBoxColumn()
		{
			_checkID = Session.GenerateID();
		}

		// ReadOnly

		private bool _readOnly = true;
		public bool ReadOnly
		{
			get { return _readOnly; }
			set { _readOnly = value; }
		}

		// CheckID

		private string _checkID;
		public string CheckID { get { return _checkID; } }

		// IWebPrehander

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			ISource source = ParentGrid.Source;
			if (!ReadOnly && (ColumnName != String.Empty) && Session.IsActionLink(context, _checkID) && ParentGrid.DataLink.Active && !source.DataView.IsEmpty())
			{
				string rowIndex = context.Request.QueryString["RowIndex"];
				if (rowIndex != null)
				{
					ParentGrid.MoveTo(Int32.Parse(rowIndex));
					DAE.Client.DataField field = source.DataView.Fields[ColumnName];
					DAE.Client.DataSetState oldState = source.DataView.State;
					field.AsBoolean = !(field.HasValue() && field.AsBoolean);
					if (oldState == DAE.Client.DataSetState.Browse)
					{
						try
						{
							source.DataView.Post();
						}
						catch
						{
							source.DataView.Cancel();
							throw;
						}
					}
					return true;
				}
			}
			return false;
		}

		public override void InternalRenderCell(HtmlTextWriter writer, Alphora.Dataphor.DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex)
		{
			bool hasValue = currentRow.HasValue(ColumnName);
			bool tempValue = false;
			if (hasValue)
				tempValue = ((DAE.Runtime.Data.Scalar)currentRow.GetValue(ColumnName)).AsBoolean;

			writer.AddAttribute(HtmlTextWriterAttribute.Class, GetClass(currentRow, isActiveRow));
			if (!ReadOnly)
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&RowIndex={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], CheckID, rowIndex));
			writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
			writer.AddAttribute(HtmlTextWriterAttribute.Align, "center");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "10");
			writer.AddAttribute(HtmlTextWriterAttribute.Height, "10");
			writer.AddAttribute
			(
				HtmlTextWriterAttribute.Src, 
				String.Format
				(
					"images/{0}{1}.gif", 
					tempValue.ToString().ToLower(), 
					ReadOnly ? "readonly" : ""
				)
			);
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			writer.RenderEndTag();	// TD
		}
	}

	public class SequenceColumn : GridColumn, ISequenceColumn, IWebHandler
	{
		public SequenceColumn()
		{
			_moveID = Session.GenerateID();
		}

		protected override void Dispose(bool disposing)
		{
			CancelMove();
			base.Dispose(disposing);
		}

		// Image

		private string _image = String.Empty;
		public string Image
		{
			get { return _image; }
			set 
			{ 
				if (_image != value)
				{
					_image = value;
					DeallocateImage();
					if (Active)
						AllocateImage();
				}
			}
		}

		protected void AllocateImage()
		{
			_imageID = WebSession.ImageCache.Allocate(Image);
		}

		protected void DeallocateImage()
		{
			if (_imageID != String.Empty)
			{
				WebSession.ImageCache.Deallocate(_imageID);
				_imageID = String.Empty;
			}
		}

		// ImageID

		private string _imageID = String.Empty;
		public string ImageID
		{
			get { return _imageID; }
		}

		// MoveID

		private string _moveID;
		public string MoveID { get { return _moveID; } }

		// Script

		private string _script = String.Empty;
		public string Script
		{
			get { return _script; }
			set { _script = (value == null ? String.Empty : value); }
		}

		// GridColumn

		public override void InternalRenderCell(HtmlTextWriter writer, Alphora.Dataphor.DAE.Runtime.Data.IRow currentRow, bool isActiveRow, int rowIndex)
		{
			string classValue;
			if (isActiveRow)
				classValue = "gridcellcurrent";
			else
				classValue = "gridcell";
			writer.AddAttribute(HtmlTextWriterAttribute.Class, classValue);
			writer.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			string imageLink;
			if (_movingRow != null)
			{
				imageLink = "images/place.png";
				writer.AddAttribute(HtmlTextWriterAttribute.Width, "18");
				writer.AddAttribute(HtmlTextWriterAttribute.Height, "19");
			}
			else
			{
				if (_imageID != String.Empty)
					imageLink = "ViewImage.aspx?ImageID=" + _imageID;
				else
					imageLink = "images/move.png";
			}
			writer.AddAttribute(HtmlTextWriterAttribute.Src, imageLink);
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&RowIndex={2}&Y='+event.offsetY,event)", (string)Session.Get(this).Context.Session["DefaultPage"], _moveID, rowIndex));
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			writer.RenderEndTag();
		}

		protected virtual void SequenceChange(DAE.Runtime.Data.IRow fromRow, DAE.Runtime.Data.IRow toRow, bool above)
		{
			SequenceColumnUtility.SequenceChange(HostNode.Session, ParentGrid.Source, _shouldEnlist, fromRow, toRow, above, _script);
		}

		private bool _shouldEnlist = true;
		[DefaultValue(true)]
		[Description("Enlist with the application transaction if there is one active.")]
		public bool ShouldEnlist
		{
			get { return _shouldEnlist; }
			set { _shouldEnlist = value; }
		}

		private DAE.Runtime.Data.Row _movingRow = null;

		private void CancelMove()
		{
			if (_movingRow != null)
			{
				_movingRow.Dispose();
				_movingRow = null;
			}
		}

		// IWebHandler

		public virtual bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, _moveID))
			{
				string rowIndex = context.Request.QueryString["RowIndex"];
				if (rowIndex != null)
				{
					if (_movingRow != null)
					{
						string posY = context.Request.QueryString["Y"];
						if (posY != null)
						{
							DAE.Runtime.Data.IRow target = ParentGrid.DataLink.Buffer(Int32.Parse(rowIndex));
							SequenceChange(_movingRow, target, Int32.Parse(posY) < 10);
						}
					}
					else
					{
						_movingRow = (DAE.Runtime.Data.Row)ParentGrid.DataLink.Buffer(Int32.Parse(rowIndex)).Copy();
						return true;
					}
				}
				CancelMove();
				return true;
			}
			else
				return false;
		}

		// Node

		protected override void Activate()
		{
			base.Activate ();
			AllocateImage();
		}

		protected override void Deactivate()
		{
			DeallocateImage();
			base.Deactivate ();
		}

	}

	public class Grid : DataElement, IWebGrid
	{
		public Grid() : base()
		{
			_pageUpID = Session.GenerateID();
			_pageDownID = Session.GenerateID();
			_firstID = Session.GenerateID();
			_lastID = Session.GenerateID();
			_dataLink = new DAE.Client.DataLink();
			_dataLink.OnActiveChanged += new DAE.Client.DataLinkHandler(DataLinkActiveChanged);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			OnDoubleClick = null;
		}

		// RowCount

		private int _rowCount = 10;
		public int RowCount
		{
			get { return _rowCount; }
			set
			{
				_rowCount = value;
				_dataLink.BufferCount = _rowCount;
			}
		}

        //ColumnCount

        private int _columnCount = -1;
        public int ColumnCount
        {
            get { return _columnCount; }
            set
            {
                if (value < 1)
                    _columnCount = -1;
                else
                    _columnCount = value;
            }
        }

		// OnDoubleClick
	
		// TODO: Support for double-click in the web client
		
		private IAction _onDoubleClick;
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

		// PageUpID

		private string _pageUpID;
		public string PageUpID { get { return _pageUpID; } }
		
		// PageDownID

		private string _pageDownID;
		public string PageDownID { get { return _pageDownID; } }

		// FirstID

		private string _firstID;
		public string FirstID { get { return _firstID; } }

		// LastID

		private string _lastID;
		public string LastID { get { return _lastID; } }

		// DataElement

		protected override void SourceChanged(ISource oldSource)
		{
			_dataLink.Source = ( Source == null ? null : Source.DataSource );
		}

		// Element

		protected virtual void RenderNavButton(HtmlTextWriter writer, string imageSrc, string iD, bool enabled)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridnav");
			if (enabled)
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, iD));
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			writer.AddAttribute(HtmlTextWriterAttribute.Height, "16");
			writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			if (enabled)
				writer.AddAttribute(HtmlTextWriterAttribute.Src, imageSrc);
			else
				writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/pixel.gif");
			writer.RenderBeginTag(HtmlTextWriterTag.Img);
			writer.RenderEndTag();

			writer.RenderEndTag();	// TD
		}

		protected override void InternalRender(HtmlTextWriter writer)
		{
			bool bOF = !_dataLink.Active || _dataLink.DataSet.BOF;
			bool eOF = !_dataLink.Active || _dataLink.DataSet.EOF;

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "grid");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, "4");
			writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			RenderGrid(writer);
			writer.RenderEndTag();

			RenderNavButton(writer, "images/first.png", _firstID, !bOF);
			writer.RenderEndTag();

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderNavButton(writer, "images/pageup.png", _pageUpID, !bOF);
			writer.RenderEndTag();

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderNavButton(writer, "images/pagedown.png", _pageDownID, !eOF);
			writer.RenderEndTag();

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderNavButton(writer, "images/last.png", _lastID, !eOF);
			writer.RenderEndTag();

			writer.RenderEndTag();	// TABLE
		}

		protected virtual void RenderGrid(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "innergrid");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			string hint = GetHint();
			if (hint != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			// Header

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridheaderrow");
			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			foreach (IWebGridColumn column in Children)
				column.RenderHeader(writer);

			writer.RenderEndTag();

			// Rows

			bool current;
			DAE.Runtime.Data.IRow currentRow;
			int count = _dataLink.LastOffset + 1;
			for (int rowIndex = 0; rowIndex < count; rowIndex++) 
			{
				current = rowIndex == _dataLink.ActiveOffset;
				if (current)
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridrowcurrent");
				else
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&RowIndex={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], ID, rowIndex.ToString()));
					if ((rowIndex % 2) == 1)
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridrowalt");
					else
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridrow");
				}
				writer.RenderBeginTag(HtmlTextWriterTag.Tr);

				currentRow = _dataLink.Buffer(rowIndex);
				foreach (IWebGridColumn column in Children)
					column.RenderCell(writer, currentRow, current, rowIndex);

				writer.RenderEndTag();
			}

			writer.RenderEndTag();	// TABLE
		}

		private bool _autoBuiltColumns;

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IWebGridColumn).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}
		
		protected override void Activate()
		{
			_dataLink.BufferCount = _rowCount;
			base.Activate();
		}

		// DataLink

		protected DAE.Client.DataLink _dataLink;
		public DAE.Client.DataLink DataLink 
		{
			get { return _dataLink; }
		}

        //this is a no-op in the web client
        private bool _useNaturalMaxWidth;
        public bool UseNaturalMaxWidth
        {
            get { return _useNaturalMaxWidth; }
            set { _useNaturalMaxWidth = value; }
        }

		private void DataLinkActiveChanged(DAE.Client.DataLink link, DAE.Client.DataSet dataSet)
		{
			if (link.Active)
			{
				// if no children, then default to a full set of columns.
				if (Children.Count == 0) 
				{
					foreach (DAE.Client.DataField column in Source.DataView.Fields)
					{
						TextColumn newColumn = new TextColumn();
						newColumn.ColumnName = column.ColumnName;
						newColumn.Title = column.ColumnName;
						Children.Add(newColumn);
					}
					_autoBuiltColumns = true;
				}
					_autoBuiltColumns = false;
			}
			else
			{
				if (_autoBuiltColumns)
					Children.Clear();
			}
		}

		public void MoveTo(int index) 
		{
			_dataLink.DataSet.MoveBy(index - _dataLink.ActiveOffset);
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			else
			{
				if (Session.IsActionLink(context, _pageUpID))
					Source.DataView.MoveBy(-RowCount);
				else if (Session.IsActionLink(context, _pageDownID))
					Source.DataView.MoveBy(RowCount);
				else if (Session.IsActionLink(context, _firstID))
					Source.DataView.First();
				else if (Session.IsActionLink(context, _lastID))
					Source.DataView.Last();
				else if (Session.IsActionLink(context, ID))
				{
					string rowIndex = context.Request.QueryString["RowIndex"];
					if ((rowIndex != null) && (rowIndex != String.Empty))
						MoveTo(Int32.Parse(rowIndex));
				}
				else
					return false;
				return true;
			}
		}

	}

	public class EditFilterAction : DataAction, IEditFilterAction
	{
		// Action

		protected override void InternalExecute(Alphora.Dataphor.Frontend.Client.INode sender, Alphora.Dataphor.Frontend.Client.EventParams paramsValue)
		{
			if (Source != null)
			{
				Host host = (Host)HostNode.Session.CreateHost();
				try
				{
					EditFilterForm form = new EditFilterForm();
					try
					{
						host.Children.Add(form);
					}
					catch
					{
						form.Dispose();
						throw;
					}
					form.Text = Strings.Get("EditFilterFormTitle");
					form.FilterExpression = Source.DataView.Filter;
					host.Open();
					form.Show((IFormInterface)FindParent(typeof(IFormInterface)), new FormInterfaceHandler(FilterAccepted), null, FormMode.None);
				}
				catch
				{
					host.Dispose();
					throw;
				}
			}
		}

		private void FilterAccepted(IFormInterface form)
		{
			if ((Source != null) && (Source.DataView != null))
			{
				Source.DataView.Filter = ((EditFilterForm)form).FilterExpression;
				Source.DataView.Open();		// Ensure the DataView is open in case a previous filter change caused it to close
			}
		}
	}

	internal class EditFilterForm : FormInterface
	{
		public EditFilterForm()
		{
			_nameID = Session.GenerateID();
		}

		// NameID

		private string _nameID;
		public string NameID { get { return _nameID; } }

		// FilterExpression

		private string _filterExpression = String.Empty;
		public string FilterExpression
		{
			get { return _filterExpression; }
			set { _filterExpression = ( value == null ? String.Empty : value ); }
		}

		// Element

		protected override void InternalRender(HtmlTextWriter writer)
		{
			base.InternalRender(writer);
			writer.Write(HttpUtility.HtmlEncode(Strings.Get("FilterExpression")));
			writer.Write("<br>");
			
			writer.AddAttribute(HtmlTextWriterAttribute.Name, _nameID);
			writer.AddAttribute(HtmlTextWriterAttribute.Cols, "60");
			writer.AddAttribute(HtmlTextWriterAttribute.Rows, "4");
			writer.AddAttribute(HtmlTextWriterAttribute.Wrap, "on");
			writer.RenderBeginTag(HtmlTextWriterTag.Textarea);

			writer.Write(HttpUtility.HtmlEncode(_filterExpression));

			writer.RenderEndTag();
		}

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			string tempValue = context.Request.Form[_nameID];
			if (tempValue != null)
			{
				_filterExpression = tempValue;
				return true;
			}
			else
				return false;
		}
	}
}