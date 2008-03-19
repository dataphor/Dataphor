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

		private string FTitle = String.Empty;
		[DefaultValue("")]
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value == null ? String.Empty : value; }
		}

		// Hint

		private string FHint = String.Empty;
		[DefaultValue("")]
		public string Hint
		{
			get { return FHint; }
			set { FHint = value; }
		}

		// ReadOnly

		private bool FReadOnly;
		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set { FReadOnly = value; }
		}

		// ColumnName

		private string FColumnName = String.Empty;
		[DefaultValue("")]
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = (value == null ? String.Empty : value); }
		}

		// Width

		private int FWidth = 15;
		[DefaultValue(15)]
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}
		
		// TextAlignment

		private HorizontalAlignment FTextAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		public HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set { FTextAlignment = value; }
		}
	}

	internal class SearchControl
	{
		public SearchControl(string AID)
		{
			FID = AID;
		}

		private string FID;

		// Source

		private ISource FSource;
		public ISource Source
		{
			get { return FSource; }
			set { FSource = value; }
		}
		
		// ColumnName

		private string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = (value == null ? String.Empty : value); }
		}

		// Value

		private string FValue = null;
		public string Value
		{
			get { return FValue; }
		}

		public void Reset()
		{
			FValue = null;
			FConversionFailed = false;
		}

		public bool ExtractValue(DAE.Runtime.Data.Row ARow)
		{
			try
			{
				if (FValue != null)
					ARow[FColumnName].AsString = FValue;
				else
					ARow[FColumnName].AsString = String.Empty;	// Assume that if we are included in the search, that we are blank
				FConversionFailed = false;
				return true;
			}
			catch
			{
				FConversionFailed = true;
				return false;
			}
		}

		// ConversionFailed

		private bool FConversionFailed;

		// SearchColumn

		private SearchColumn FColumn;
		/// <summary> The search control can optionally be associated with a SearchColumn. </summary>
		public SearchColumn Column
		{
			get { return FColumn; }
			set { FColumn = value; }
		}

		public string GetTitle()
		{
			if ((FColumn == null) || (FColumn.Title == String.Empty))
				return FColumnName;
			else
				return FColumn.Title;
		}

		private HorizontalAlignment GetTextAlignment()
		{
			if (FColumn == null)
				return HorizontalAlignment.Left;
			else
				return FColumn.TextAlignment;
		}

		private int GetWidth()
		{
			if (FColumn == null)
				return 15;
			else
				return FColumn.Width;
		}

		// PreProcessing

		public void PreprocessRequest(HttpContext AContext) // Do not implement IWebPrehandler, the Search node calls this directly
		{
			string LValue = AContext.Request.Form[FID];
			if ((LValue != null) && (LValue != String.Empty))	// Treats blank as null
				FValue = LValue;
			else
				FValue = null;
		}

		// Render

		public virtual void Render(HtmlTextWriter AWriter) 
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Name, FID);
			string LClass;
			if (FValue != null)
			{
				if (FConversionFailed)
					LClass = "textboxfailed";
				else
					LClass = "textbox";
				AWriter.AddAttribute(HtmlTextWriterAttribute.Value, FValue);
			}
			else
				LClass = "textboxnull";
			FConversionFailed = false;
			if (LClass != "textbox")
			{
				AWriter.AddAttribute("onchange", "NotNull(this, null, 'textbox')");
				AWriter.AddAttribute("onkeypress", "NotNull(this, null, 'textbox')");
			}
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, LClass);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Size, GetWidth().ToString());
			AWriter.AddAttribute(HtmlTextWriterAttribute.Style, "textalign: " + GetTextAlignment().ToString().ToLower());
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.RenderEndTag();
		}
	}

	/// <summary> Incremental search node. </summary>
	public class Search : DataElement, ISearch, IWebPrehandler
	{
		// TitleAlignment

		private TitleAlignment FTitleAlignment = TitleAlignment.Left;
		[DefaultValue(TitleAlignment.Left)]
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set { FTitleAlignment = value; }
		}

		#region Search Controls

		private ArrayList FSearchControls = new ArrayList();

		private SearchColumn FindSearchColumn(string AColumnName)
		{
			foreach (SearchColumn LNode in Children) 
				if (LNode.ColumnName == AColumnName)
					return LNode;
			return null;
		}

		private SearchControl FindSearchControl(string AColumnName)
		{
			foreach (SearchControl LControl in FSearchControls)
				if (LControl.ColumnName == AColumnName)
					return LControl;
			return null;
		}

		private SearchControl CreateSearchControl(DAE.Schema.OrderColumn AColumn)
		{
			SearchControl LControl = new SearchControl(Session.GenerateID());
			LControl.Column = FindSearchColumn(AColumn.Column.Name);
			LControl.ColumnName = AColumn.Column.Name;
			LControl.Source = Source;
			return LControl;
		}

		protected void UpdateControls()
		{
			FSearchControls.Clear();
			if ((Source != null) && (Source.DataView != null) && (Source.DataView.Order != null))
			{
				foreach (DAE.Schema.OrderColumn LColumn in Source.DataView.Order.Columns) 
				{
					if (!((DAE.Client.TableDataSet)Source.DataSource.DataSet).IsDetailKey(LColumn.Column.Name))
						FSearchControls.Add(CreateSearchControl(LColumn));
				}
			}
		}

		// IWebPrehandler

		public virtual void PreprocessRequest(HttpContext AContext)
		{
			foreach (SearchControl LControl in FSearchControls)
				LControl.PreprocessRequest(AContext);
		}

		#endregion

		#region Data Source

		protected override void SourceChanged(ISource AOldSource) 
		{
			if (AOldSource != null) 
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

		private Order FOrder;

		private void SetOrder(Order AOrder)
		{
			if (AOrder != FOrder)
			{
				FOrder = AOrder;
				UpdateControls();
			}
		}

		protected virtual void DataActiveChange(DAE.Client.DataLink ADataLink, DAE.Client.DataSet ADataSet)
		{
			if (ADataLink.Active && (ADataSet is DAE.Client.TableDataSet))
				SetOrder(((DAE.Client.TableDataSet)ADataSet).Order);
			else
				SetOrder(null);
		}

		public void Reset()
		{
			foreach (SearchControl LControl in FSearchControls)
				LControl.Reset();
		}

		protected virtual void DataChange(DAE.Client.DataLink ADataLink, DAE.Client.DataSet ADataSet)
		{
			if (ADataLink.Active && (ADataSet is DAE.Client.TableDataSet))
			{
				DAE.Client.TableDataSet LTable = (DAE.Client.TableDataSet)ADataSet;
				if (FOrder != LTable.Order)
					SetOrder(LTable.Order);
				else
					if (!FSearching)
						Reset();
			}
		}

		#endregion

		#region Searching

		private bool FSearching;

		private void PerformSearch()
		{
			if ((Source != null) && (Source.DataView != null) && !Source.DataView.IsEmpty())
			{
				using (DAE.Runtime.Data.Row LRow = CreateSearchRow())
				{
					if (LRow == null)
						return;
					FSearching = true;
					try
					{
						Source.DataView.FindNearest(LRow);
					}
					finally
					{
						FSearching = false;
					}
				}
			}
		}

		private DAE.Runtime.Data.Row CreateSearchRow()
		{
			// Determine the last search control with a search value
			SearchControl LPendingIncremental = null;
			SearchControl LControl;
			for (int i = FSearchControls.Count - 1; i >= 0; i--)
			{
				LControl = (SearchControl)FSearchControls[i];
				if (LControl.Value != null)
				{
					LPendingIncremental = LControl;
					break;
				}
			}

			if (LPendingIncremental != null)
			{
				// FROM WINDOWS CLIENT

				// Build a row consisting of order columns up to and including the pending control
				RowType LRowType = new RowType();	
				foreach (OrderColumn LColumn in FOrder.Columns)
				{
					LRowType.Columns.Add(new DAE.Schema.Column(LColumn.Column.Name, LColumn.Column.DataType));
					if (LColumn.Column.Name == LPendingIncremental.ColumnName)
						break;
				}

				DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(Source.Process, LRowType);
				try
				{
					Source.DataView.InitializeFromMaster(LRow);
					foreach (DAE.Schema.Column LColumn in LRowType.Columns)
					{
						if (!Source.DataView.IsDetailKey(LColumn.Name))
							if (!FindSearchControl(LColumn.Name).ExtractValue(LRow))
								return null;
					}
					return LRow;
				}
				catch
				{
					LRow.Dispose();
					throw;
				}

				// END FROM WINDOWS CLIENT
			}
			else
				return null;
		}

		#endregion

		// IWebHandler

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			else
			{
				try
				{
					if (Session.IsActionLink(AContext, ID + "Search"))
						PerformSearch();
					else if (Session.IsActionLink(AContext, ID + "SortBy"))
					{
						string LSort = AContext.Request.QueryString["Sort"];
						if ((LSort != null) && (LSort != String.Empty))
							Source.OrderString = LSort;
					}
					else
						return false;
				}
				catch (Exception LException)
				{
					HandleElementException(LException);
				}
				return true;
			}
		}

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(ISearchColumn).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		#region Rendering

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "search");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "2");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "auto");
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			bool LFirst = true;
			foreach (SearchControl LControl in FSearchControls)
			{
				if (LFirst)
					LFirst = false;
				else
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
					AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
					AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/searcharrow.gif");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "10");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
					AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
					AWriter.RenderEndTag();
					AWriter.RenderEndTag();
				}

				AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

				if (FTitleAlignment != TitleAlignment.None)
				{
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(LControl.GetTitle())));
					if (FTitleAlignment != TitleAlignment.Top)
						Session.RenderDummyImage(AWriter, "4px", "0");
				}

				if (FTitleAlignment == TitleAlignment.Top)
					AWriter.Write("<br>");

				LControl.Render(AWriter);

				AWriter.RenderEndTag();
			}

			AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "right");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			
			Session.RenderDummyImage(AWriter, "5", "1");
			
			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "button");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Value, Strings.Get("FindButtonText"));
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, ID + "Search"));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.RenderEndTag();

			Session.RenderDummyImage(AWriter, "5", "1");
			
			AWriter.RenderEndTag();	// TD

			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "right");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			
			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/lookup.png");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "ShowDropDown('" + ID + "SearchBy', GetParentTable(this))", true);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			Session.RenderDummyImage(AWriter, "5", "1");
			
			AWriter.RenderEndTag();	// TD
			AWriter.RenderEndTag();	// TR
			AWriter.RenderEndTag();	// TABLE

			// Render search-by drop-down
			if (Source != null)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "searchby");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Id, ID + "SearchBy");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

				// Construct the a complete list of possible orderings including non-sparse keys
				Orders LOrders = new Orders();
				LOrders.AddRange(Source.TableVar.Orders);
				DAE.Schema.Order LOrderForKey;
				foreach (Key LKey in Source.TableVar.Keys)
					if (!LKey.IsSparse)
					{
						LOrderForKey = new Order(LKey);
						if (!LOrders.Contains(LOrderForKey))
							LOrders.Add(LOrderForKey);
					}

				foreach (Order LOrder in LOrders)
					if (IsOrderVisible(LOrder) && !LOrder.Equals(Source.Order))
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&Sort={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], ID + "SortBy", HttpUtility.UrlEncode(LOrder.ToString())), true);
						AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
						AWriter.AddAttribute("onmouseover", @"className='highlightedmenuitem'");
						AWriter.AddAttribute("onmouseout", @"className=''");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
						AWriter.Write(HttpUtility.HtmlEncode(GetOrderTitle(LOrder)));
						AWriter.RenderEndTag();	// TD
						AWriter.RenderEndTag();	// TR
					}
				

				AWriter.RenderEndTag();	// TABLE

				AWriter.RenderEndTag();	// DIV
			}
		}

		#endregion

		#region Order Handling

		// FROM WINDOWS CLIENT

		private string GetDefaultOrderTitle(Order AOrder)
		{
			System.Text.StringBuilder LName = new System.Text.StringBuilder();
			foreach (OrderColumn LColumn in AOrder.Columns)
			{
				if (IsColumnVisible(LColumn.Column) && !Source.DataView.IsDetailKey(LColumn.Column.Name))
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

		private string GetOrderTitle(Order AOrder)
		{
			return DAE.Language.D4.MetaData.GetTag(AOrder.MetaData, "Frontend.Title", GetDefaultOrderTitle(AOrder));
		}

		protected bool IsOrderVisible(Order AOrder)
		{
			bool LIsVisible = Convert.ToBoolean(DAE.Language.D4.MetaData.GetTag(AOrder.MetaData, "Frontend.Visible", "true"));
			bool LHasVisibleColumns = false;
			if (LIsVisible)
			{
				bool LIsColumnVisible;
				bool LHasInvisibleColumns = false;
				foreach (OrderColumn LColumn in AOrder.Columns)
				{
					LIsColumnVisible = IsColumnVisible(LColumn.Column);
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

		private static string GetColumnTitle(TableVarColumn AColumn)
		{
			return DAE.Language.D4.MetaData.GetTag(AColumn.MetaData, "Frontend.Title", DAE.Schema.Object.Unqualify(AColumn.Name));
		}
		
		public static bool IsColumnVisible(TableVarColumn AColumn)
		{
			return Convert.ToBoolean(DAE.Language.D4.MetaData.GetTag(AColumn.MetaData, "Frontend.Visible", "true"));
		}
		// END FROM WINDOWS CLIENT

		#endregion
	}
}
