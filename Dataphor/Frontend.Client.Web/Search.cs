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

using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class SearchColumn : Node, ISearchColumn
	{
		// Title

		private string _title = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get { return _title; }
			set { _title = value == null ? String.Empty : value; }
		}

		// Hint

		private string _hint = String.Empty;
		[DefaultValue("")]
		public string Hint
		{
			get { return _hint; }
			set { _hint = value; }
		}

		// ReadOnly

		private bool _readOnly;
		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set { _readOnly = value; }
		}

		// ColumnName

		private string _columnName = String.Empty;
		[DefaultValue("")]
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = (value == null ? String.Empty : value); }
		}

		// Width

		private int _width = 15;
		[DefaultValue(15)]
		public int Width
		{
			get { return _width; }
			set { _width = value; }
		}
		
		// TextAlignment

		private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment TextAlignment
		{
			get { return _textAlignment; }
			set { _textAlignment = value; }
		}
	}

	internal class SearchControl
	{
		public SearchControl(string iD)
		{
			_iD = iD;
		}

		private string _iD;

		// Source

		private ISource _source;
		public ISource Source
		{
			get { return _source; }
			set { _source = value; }
		}
		
		// ColumnName

		private string _columnName = String.Empty;
		public string ColumnName
		{
			get { return _columnName; }
			set { _columnName = (value == null ? String.Empty : value); }
		}

		// Value

		private string _value = null;
		public string Value
		{
			get { return _value; }
		}

		public void Reset()
		{
			_value = null;
			_conversionFailed = false;
		}

		public bool ExtractValue(DAE.Runtime.Data.Row row)
		{
			try
			{
				if (_value != null)
					((DAE.Runtime.Data.Scalar)row.GetValue(_columnName)).AsString = _value;
				else
					((DAE.Runtime.Data.Scalar)row.GetValue(_columnName)).AsString = String.Empty;	// Assume that if we are included in the search, that we are blank
				_conversionFailed = false;
				return true;
			}
			catch
			{
				_conversionFailed = true;
				return false;
			}
		}

		// ConversionFailed

		private bool _conversionFailed;

		// SearchColumn

		private SearchColumn _column;
		/// <summary> The search control can optionally be associated with a SearchColumn. </summary>
		public SearchColumn Column
		{
			get { return _column; }
			set { _column = value; }
		}

		public string GetTitle()
		{
			if ((_column == null) || (_column.Title == String.Empty))
				return _columnName;
			else
				return _column.Title;
		}

		private HorizontalAlignment GetTextAlignment()
		{
			if (_column == null)
				return HorizontalAlignment.Left;
			else
				return _column.TextAlignment;
		}

		private int GetWidth()
		{
			if (_column == null)
				return 15;
			else
				return _column.Width;
		}

		// PreProcessing

		public void PreprocessRequest(HttpContext context) // Do not implement IWebPrehandler, the Search node calls this directly
		{
			string tempValue = context.Request.Form[_iD];
			if ((tempValue != null) && (tempValue != String.Empty))	// Treats blank as null
				_value = tempValue;
			else
				_value = null;
		}

		// Render

		public virtual void Render(HtmlTextWriter writer) 
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
			writer.AddAttribute(HtmlTextWriterAttribute.Name, _iD);
			string classValue;
			if (_value != null)
			{
				if (_conversionFailed)
					classValue = "textboxfailed";
				else
					classValue = "textbox";
				writer.AddAttribute(HtmlTextWriterAttribute.Value, _value);
			}
			else
				classValue = "textboxnull";
			_conversionFailed = false;
			if (classValue != "textbox")
			{
				writer.AddAttribute("onchange", "NotNull(this, null, 'textbox')");
				writer.AddAttribute("onkeypress", "NotNull(this, null, 'textbox')");
			}
			writer.AddAttribute(HtmlTextWriterAttribute.Class, classValue);

			writer.AddAttribute(HtmlTextWriterAttribute.Size, GetWidth().ToString());
			writer.AddAttribute(HtmlTextWriterAttribute.Style, "textalign: " + GetTextAlignment().ToString().ToLower());
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();
		}
	}

	/// <summary> Incremental search node. </summary>
	public class Search : DataElement, ISearch, IWebPrehandler
	{
		// TitleAlignment

		private TitleAlignment _titleAlignment = TitleAlignment.Top;
		[DefaultValue(TitleAlignment.Top)]
		public TitleAlignment TitleAlignment
		{
			get { return _titleAlignment; }
			set { _titleAlignment = value; }
		}

		#region Search Controls

		private ArrayList _searchControls = new ArrayList();

		private SearchColumn FindSearchColumn(string columnName)
		{
			foreach (SearchColumn node in Children) 
				if (node.ColumnName == columnName)
					return node;
			return null;
		}

		private SearchControl FindSearchControl(string columnName)
		{
			foreach (SearchControl control in _searchControls)
				if (control.ColumnName == columnName)
					return control;
			return null;
		}

		private SearchControl CreateSearchControl(DAE.Schema.OrderColumn column)
		{
			SearchControl control = new SearchControl(Session.GenerateID());
			control.Column = FindSearchColumn(column.Column.Name);
			control.ColumnName = column.Column.Name;
			control.Source = Source;
			return control;
		}

		protected void UpdateControls()
		{
			_searchControls.Clear();
			if ((Source != null) && (Source.DataView != null) && (Source.DataView.Order != null))
			{
				foreach (DAE.Schema.OrderColumn column in Source.DataView.Order.Columns) 
				{
					if (!((DAE.Client.TableDataSet)Source.DataSource.DataSet).IsDetailKey(column.Column.Name))
						_searchControls.Add(CreateSearchControl(column));
				}
			}
		}

		// IWebPrehandler

		public virtual void PreprocessRequest(HttpContext context)
		{
			foreach (SearchControl control in _searchControls)
				control.PreprocessRequest(context);
		}

		#endregion

		#region Data Source

		protected override void SourceChanged(ISource oldSource) 
		{
			if (oldSource != null) 
			{
				Source.ActiveChanged -= new DAE.Client.DataLinkHandler(DataActiveChange);
				Source.DataChanged -= new DAE.Client.DataLinkHandler(DataChange);
			}
			if (Source != null)
			{
				Source.ActiveChanged += new DAE.Client.DataLinkHandler(DataActiveChange);
				Source.DataChanged += new DAE.Client.DataLinkHandler(DataChange);
			}
		}

		private Order _order;

		private void SetOrder(Order order)
		{
			if (order != _order)
			{
				_order = order;
				UpdateControls();
			}
		}

		protected virtual void DataActiveChange(DAE.Client.DataLink dataLink, DAE.Client.DataSet dataSet)
		{
			if (dataLink.Active && (dataSet is DAE.Client.TableDataSet))
				SetOrder(((DAE.Client.TableDataSet)dataSet).Order);
			else
				SetOrder(null);
		}

		public void Reset()
		{
			foreach (SearchControl control in _searchControls)
				control.Reset();
		}

		protected virtual void DataChange(DAE.Client.DataLink dataLink, DAE.Client.DataSet dataSet)
		{
			if (dataLink.Active && (dataSet is DAE.Client.TableDataSet))
			{
				DAE.Client.TableDataSet table = (DAE.Client.TableDataSet)dataSet;
				if (_order != table.Order)
					SetOrder(table.Order);
				else
					if (!_searching)
						Reset();
			}
		}

		#endregion

		#region Searching

		private bool _searching;

		private void PerformSearch()
		{
			if ((Source != null) && (Source.DataView != null) && !Source.DataView.IsEmpty())
			{
				using (DAE.Runtime.Data.Row row = CreateSearchRow())
				{
					if (row == null)
						return;
					_searching = true;
					try
					{
						Source.DataView.FindNearest(row);
					}
					finally
					{
						_searching = false;
					}
				}
			}
		}

		private DAE.Runtime.Data.Row CreateSearchRow()
		{
			// Determine the last search control with a search value
			SearchControl pendingIncremental = null;
			SearchControl control;
			for (int i = _searchControls.Count - 1; i >= 0; i--)
			{
				control = (SearchControl)_searchControls[i];
				if (control.Value != null)
				{
					pendingIncremental = control;
					break;
				}
			}

			if (pendingIncremental != null)
			{
				// FROM WINDOWS CLIENT

				// Build a row consisting of order columns up to and including the pending control
				RowType rowType = new RowType();	
				foreach (OrderColumn column in _order.Columns)
				{
					rowType.Columns.Add(new DAE.Schema.Column(column.Column.Name, column.Column.DataType));
					if (column.Column.Name == pendingIncremental.ColumnName)
						break;
				}

				DAE.Runtime.Data.Row row = new DAE.Runtime.Data.Row(Source.Process.ValueManager, rowType);
				try
				{
					Source.DataView.InitializeFromMaster(row);
					foreach (DAE.Schema.Column column in rowType.Columns)
					{
						if (!Source.DataView.IsDetailKey(column.Name))
							if (!FindSearchControl(column.Name).ExtractValue(row))
								return null;
					}
					return row;
				}
				catch
				{
					row.Dispose();
					throw;
				}

				// END FROM WINDOWS CLIENT
			}
			else
				return null;
		}

		#endregion

		// IWebHandler

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			else
			{
				try
				{
					if (Session.IsActionLink(context, ID + "Search"))
						PerformSearch();
					else if (Session.IsActionLink(context, ID + "SortBy"))
					{
						string sort = context.Request.QueryString["Sort"];
						if ((sort != null) && (sort != String.Empty))
							Source.OrderString = sort;
					}
					else
						return false;
				}
				catch (Exception exception)
				{
					HandleElementException(exception);
				}
				return true;
			}
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(ISearchColumn).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		#region Rendering

		protected override void InternalRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "search");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "2");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "auto");
            string hint = GetHint();
            if (hint != String.Empty)
                writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            bool first = true;
            foreach (SearchControl control in _searchControls)
            {
                if (first)
                    first = false;
                else
                {
                    /*
                    writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/searcharrow.gif");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "searcharrow");
                        writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
                        writer.AddAttribute(HtmlTextWriterAttribute.Height, "10");
                        writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                        writer.RenderBeginTag(HtmlTextWriterTag.Img);
                        writer.RenderEndTag();// IMG
                    writer.RenderEndTag();// TD
                    */
                }

                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
                writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                { 
                    if (_titleAlignment != TitleAlignment.None)
                    {
                        writer.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(control.GetTitle())));
                        if (_titleAlignment != TitleAlignment.Top)
                            Session.RenderDummyImage(writer, "10px", "0");
                    }

                    if (_titleAlignment == TitleAlignment.Top)
                        writer.Write("<br>");

                    control.Render(writer);
                }
                writer.RenderEndTag();//TD
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                Session.RenderDummyImage(writer, "5", "1");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, Strings.Get("FindButtonText"));
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "trigger");
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID + "Search"));
                
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag();// INPUT

                Session.RenderDummyImage(writer, "5", "1");

            writer.RenderEndTag();	// TD

            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
            writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
            
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "lookup");
                    writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/lookup.png");
                    writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "ShowDropDown('" + ID + "SearchBy', GetParentTable(this))", true);
                    writer.RenderBeginTag(HtmlTextWriterTag.Img);
                    writer.RenderEndTag();
                }
                Session.RenderDummyImage(writer, "5", "1");
                writer.RenderEndTag();	// TD
            
            writer.RenderEndTag();	// TR
            writer.RenderEndTag();	// TABLE

            // Render search-by drop-down
            if (Source != null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "searchby");
                writer.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, ID + "SearchBy");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);

                // Construct the a complete list of possible orderings including non-sparse keys
                Orders orders = new Orders();
                orders.AddRange(Source.TableVar.Orders);
                DAE.Schema.Order orderForKey;
                foreach (Key key in Source.TableVar.Keys)
                    if (!key.IsSparse)
                    {
                        orderForKey = new Order(key);
                        if (!orders.Contains(orderForKey))
                            orders.Add(orderForKey);
                    }

                foreach (Order order in orders)
                    if (IsOrderVisible(order) && !order.Equals(Source.Order))
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&Sort={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], ID + "SortBy", HttpUtility.UrlEncode(order.ToString())), true);
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute("onmouseover", @"className='highlightedmenuitem'");
                        writer.AddAttribute("onmouseout", @"className=''");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(HttpUtility.HtmlEncode(GetOrderTitle(order)));
                        writer.RenderEndTag();	// TD
                        writer.RenderEndTag();	// TR
                    }


                writer.RenderEndTag();	// TABLE

                writer.RenderEndTag();	// DIV
            }
        }

		#endregion

		#region Order Handling

		// FROM WINDOWS CLIENT

		private string GetDefaultOrderTitle(Order order)
		{
			System.Text.StringBuilder name = new System.Text.StringBuilder();
			foreach (OrderColumn column in order.Columns)
			{
				if (IsColumnVisible(column.Column) && !Source.DataView.IsDetailKey(column.Column.Name))
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

		private string GetOrderTitle(Order order)
		{
			return DAE.Language.D4.MetaData.GetTag(order.MetaData, "Frontend.Title", GetDefaultOrderTitle(order));
		}

		protected bool IsOrderVisible(Order order)
		{
			bool isVisible = Convert.ToBoolean(DAE.Language.D4.MetaData.GetTag(order.MetaData, "Frontend.Visible", "true"));
			bool hasVisibleColumns = false;
			if (isVisible)
			{
				bool isColumnVisible;
				bool hasInvisibleColumns = false;
				foreach (OrderColumn column in order.Columns)
				{
					isColumnVisible = IsColumnVisible(column.Column);
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

		private static string GetColumnTitle(TableVarColumn column)
		{
			return DAE.Language.D4.MetaData.GetTag(column.MetaData, "Frontend.Title", DAE.Schema.Object.Unqualify(column.Name));
		}
		
		public static bool IsColumnVisible(TableVarColumn column)
		{
			return Convert.ToBoolean(DAE.Language.D4.MetaData.GetTag(column.MetaData, "Frontend.Visible", "true"));
		}
		// END FROM WINDOWS CLIENT

		#endregion
	}
}
