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
		private string FHint = String.Empty;
		public string Hint
		{
			get { return FHint; }
			set { FHint = value; }
		}

		// Title

		private string FTitle = String.Empty;
		public string Title
		{
			get { return FTitle; }
			set { FTitle = value == null ? String.Empty : value; }
		}

		public virtual string GetTitle()
		{
			return FTitle;
		}
		
		// Width

		private int FWidth = 10;
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}

		// MinRowHeight

		private int FMinRowHeight = -1;
		public int MinRowHeight
		{
			get { return FMinRowHeight; }
			set { FMinRowHeight = value; }
		}

		// MaxRowHeight

		private int FMaxRowHeight = -1;
		public int MaxRowHeight
		{
			get { return FMaxRowHeight; }
			set { FMaxRowHeight = value; }
		}

		// Visible

		private bool FVisible = true;
		public bool Visible
		{
			get { return FVisible; }
			set { FVisible = value; }
		}

		public virtual bool GetVisible()
		{
			return FVisible;
		}

		// ParentGrid

		public IWebGrid ParentGrid
		{
			get { return (IWebGrid)Parent; }
		}

		// Render

		public virtual void RenderCell(HtmlTextWriter AWriter, DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex)
		{
			if (GetVisible())
				InternalRenderCell(AWriter, ACurrentRow, AIsActiveRow, ARowIndex);
		}

		public abstract void InternalRenderCell(HtmlTextWriter AWriter, DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex);

		public virtual void RenderHeader(HtmlTextWriter AWriter) 
		{
			if (GetVisible())
				InternalRenderHeader(AWriter);
		}

		public virtual void InternalRenderHeader(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridheadercell");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			if (FTitle == String.Empty)
				AWriter.Write("&nbsp;");
			else
				AWriter.Write(HttpUtility.HtmlEncode(Title));
			AWriter.RenderEndTag();
		}
	}

	public abstract class DataGridColumn : GridColumn, IDataGridColumn, IWebHandler
	{
		public DataGridColumn()
		{
			FHeaderID = Session.GenerateID();
		}

		// ColumnName

		private string FColumnName = String.Empty;
		public string ColumnName
		{
			get { return FColumnName; }
			set { FColumnName = value == null ? String.Empty : value; }
		}

		// HeaderID

		private string FHeaderID;
		public string HeaderID { get { return FHeaderID; } }

		// GridColumn

		public override string GetTitle()
		{
			if (Title == String.Empty)
				return ColumnName;
			else
				return base.GetTitle();
		}

		protected virtual string GetClass(DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow)
		{
			string LClass;
			if (AIsActiveRow)
				LClass = "gridcellcurrent";
			else
				LClass = "gridcell";
			if ((ColumnName != String.Empty) && !ACurrentRow.HasValue(ColumnName))
				LClass = LClass + "null";
			return LClass;
		}

		public override void InternalRenderHeader(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridheaderdatacell");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, FHeaderID));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			string LTitle = GetTitle();
			if (LTitle == String.Empty)
				AWriter.Write("&nbsp;");
			else
				AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(LTitle)));

			DAE.Client.DataLink LLink = ParentGrid.DataLink;
			if (LLink.Active)
			{
				DAE.Client.TableDataSet LDataSet = ParentGrid.DataLink.DataSet as DAE.Client.TableDataSet;
				if (LDataSet != null)
				{
					Schema.Order LOrder = LDataSet.Order;
					if ((LOrder != null) && (LOrder.Columns.IndexOf(ColumnName) > -1))
					{
						AWriter.Write("&nbsp;");
						if (((Schema.OrderColumn)LOrder.Columns[ColumnName]).Ascending)
							AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/downarrow.gif");
						else
							AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/uparrow.gif");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
						AWriter.RenderEndTag();
						AWriter.Write("&nbsp;");
					}
				}
			}
			AWriter.RenderEndTag();
		}

		// IWebHandler

		public virtual bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, FHeaderID))
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
			DAE.Client.TableDataSet LDataSet = ParentGrid.DataLink.DataSet as DAE.Client.TableDataSet;
			if (LDataSet != null)
			{
				// Returns the order that has the given column as the first column
				if ((LDataSet.Order != null) && (LDataSet.Order.Columns.Count >= 1) && (LDataSet.Order.Columns[0].Column.Name == ColumnName))
					return LDataSet.Order;

				foreach (Schema.Order LOrder in ParentGrid.DataLink.DataSet.TableVar.Orders)
					if ((LOrder.Columns.Count >= 1) && (LOrder.Columns[0].Column.Name == ColumnName))
						return LOrder;

				foreach (Schema.Key LKey in ParentGrid.DataLink.DataSet.TableVar.Keys)
					if (!LKey.IsSparse && (LKey.Columns.Count >= 1) && (LKey.Columns[0].Name == ColumnName))
						return new Schema.Order(LKey);
			}		
			return null;
		}

		protected virtual void ChangeOrderTo()
		{
			if (ParentGrid.DataLink.Active)
			{
				DAE.Client.TableDataSet LDataSet = ParentGrid.DataLink.DataSet as DAE.Client.TableDataSet;
				if (LDataSet != null)
				{
					Schema.Order LOrder = FindOrderForColumn();
					if (LOrder == null)
						LDataSet.OrderString = "order { " + ColumnName + " asc }";
					else
					{
						Schema.Order LCurrentOrder = LDataSet.Order;
						int LCurrentColumnIndex = LCurrentOrder == null ? -1 : LCurrentOrder.Columns.IndexOf(ColumnName);

						bool LDescending = (LCurrentColumnIndex >= 0) && LCurrentOrder.Columns[LCurrentColumnIndex].Ascending;
						if (!LDescending ^ LOrder.Columns[ColumnName].Ascending)
							LOrder = new Schema.Order(LOrder, true);

						LDataSet.Order = LOrder;
					}
				}
			}
		}

		private bool InKey()
		{
			DAE.Client.DataLink LLink = ParentGrid.DataLink;
			if (LLink.Active)
				foreach (Schema.Key LKey in LLink.DataSet.TableVar.Keys)
					if (!LKey.IsSparse && (LKey.Columns.IndexOfName(ColumnName) >= 0))
						return true;
			return false;
		}

		private bool InOrder()
		{
			DAE.Client.DataLink LLink = ParentGrid.DataLink;
			if (LLink.Active)
				foreach (Schema.Order LOrder in LLink.DataSet.TableVar.Orders)
					if (LOrder.Columns.IndexOf(ColumnName) >= 0)
						return true;
			return false;
		}

		// END ADAPTED FROM WINDOWS GRID
	}

	public class TextColumn : DataGridColumn, ITextColumn
	{
		// TextAlignment

		private HorizontalAlignment FTextAlignment = HorizontalAlignment.Left;
		public HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set { FTextAlignment = value; }
		}

		// VerticalAlignment

		private VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set { FVerticalAlignment = value; }
		}

		// MaxRows

		private int FMaxRows = -1;
		public int MaxRows
		{
			get { return FMaxRows; }
			set { FMaxRows = value; }
		}

		// WordWrap

		private bool FWordWrap = false;
		public bool WordWrap
		{
			get { return FWordWrap; }
			set { FWordWrap = value; }
		}

		// VerticalText

		private bool FVerticalText = false;
		public bool VerticalText
		{
			get { return FVerticalText; }
			set { FVerticalText = value; }
		}

		public override void InternalRenderCell(HtmlTextWriter AWriter, Alphora.Dataphor.DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex)
		{
			string LValue;
			if ((ColumnName == String.Empty) || !ACurrentRow.HasValue(ColumnName))
				LValue = String.Empty;
			else
				LValue = ACurrentRow[ColumnName].AsDisplayString;
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, GetClass(ACurrentRow, AIsActiveRow));

			if (!FWordWrap)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Align, TextAlignment.ToString());
			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, VerticalAlignment.ToString());
			if (FVerticalText)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Style, "mso-rotate:-90");
			if (FMaxRows >= 1)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Rows, FMaxRows.ToString());
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			if (LValue == String.Empty)
				AWriter.Write("&nbsp;");
			else
				AWriter.Write(HttpUtility.HtmlEncode(Session.TruncateTitle(LValue, Width)).Replace("\n","<br>"));

			AWriter.RenderEndTag();
		}
	}

	public class TriggerColumn : GridColumn, ITriggerColumn, IWebHandler
	{
		public TriggerColumn()
		{
			FTriggerID = Session.GenerateID();
		}

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}

		// Action

		private IAction FAction;
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (value != FAction)
				{
					if (FAction != null)
						FAction.Disposed -= new EventHandler(ActionDisposed);
					FAction = value;
					if (FAction != null)
						FAction.Disposed += new EventHandler(ActionDisposed);
				}
			}
		}

		private void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}

		public virtual bool GetEnabled()
		{
			return (FAction != null) && FAction.GetEnabled();
		}

		// Text

		private string FText = String.Empty;

		public string Text
		{
			get { return FText; }
			set { FText = value; }
		}

		public virtual string GetText()
		{
			if ((FText != String.Empty) || (FAction == null))
				return FText;
			else
				return FAction.GetText();
		}

		// TriggerID

		private string FTriggerID;
		public string TriggerID { get { return FTriggerID; } }

		// GridColumn

		public override void InternalRenderCell(HtmlTextWriter AWriter, Alphora.Dataphor.DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex)
		{
			string LClass;
			if (AIsActiveRow)
				LClass = "gridcellcurrent";
			else
				LClass = "gridcell";
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, LClass);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "trigger");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Type, "button");
			string LHint = String.Empty;
			if (!GetEnabled())
				AWriter.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
			else
				LHint = Action.Hint;
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Value, Session.RemoveAccellerator(GetText()), true);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit({0}?ActionID={1}&RowIndex={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], FTriggerID, ARowIndex.ToString()));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Input);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();
		}

		// IWebHandler

		public virtual bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, FTriggerID))
			{
				string LRowIndex = AContext.Request.QueryString["RowIndex"];
				if (LRowIndex != null)
				{
					ParentGrid.MoveTo(Int32.Parse(LRowIndex));
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
			return base.GetVisible() && ((FAction == null) || FAction.Visible);
		}
	}

	public class ImageColumn : DataGridColumn, IImageColumn
	{
		// HorizontalAlignment

		private HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Center;
		public virtual HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set { FHorizontalAlignment = value; }
		}

		// ImageHandlerID

		private string FImageHandlerID;
		public string ImageHandlerID { get { return FImageHandlerID; } }

		// GridColumn

		public override void InternalRenderCell(HtmlTextWriter AWriter, Alphora.Dataphor.DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, GetClass(ACurrentRow, AIsActiveRow));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?HandlerID=" + FImageHandlerID + "&RowIndex=" + ARowIndex);
			if (MaxRowHeight > 0)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Height, MaxRowHeight.ToString());
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();
		}

		private void LoadImage(HttpContext AContext, string AID, Stream AStream)
		{
			string LRowIndex = AContext.Request.QueryString["RowIndex"];
			if ((LRowIndex != String.Empty) && (ColumnName != String.Empty))
			{
				DAE.Runtime.Data.Row LRow = ParentGrid.DataLink.Buffer(Int32.Parse(LRowIndex));
				if ((LRow != null) && LRow.HasValue(ColumnName))
					using (Stream LSource = LRow[ColumnName].OpenStream())
					{
						StreamUtility.CopyStream(LSource, AStream);
					}
			}
		}

		// Node

		protected override void Activate()
		{
			base.Activate();
			FImageHandlerID = WebSession.ImageCache.RegisterImageHandler(new LoadImageHandler(LoadImage));
		}

		protected override void Deactivate()
		{
			try
			{
				WebSession.ImageCache.UnregisterImageHandler(FImageHandlerID);
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
			FCheckID = Session.GenerateID();
		}

		// ReadOnly

		private bool FReadOnly = true;
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set { FReadOnly = value; }
		}

		// CheckID

		private string FCheckID;
		public string CheckID { get { return FCheckID; } }

		// IWebPrehander

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			ISource LSource = ParentGrid.Source;
			if (!ReadOnly && (ColumnName != String.Empty) && Session.IsActionLink(AContext, FCheckID) && ParentGrid.DataLink.Active && !LSource.DataView.IsEmpty())
			{
				string LRowIndex = AContext.Request.QueryString["RowIndex"];
				if (LRowIndex != null)
				{
					ParentGrid.MoveTo(Int32.Parse(LRowIndex));
					DAE.Client.DataField LField = LSource.DataView.Fields[ColumnName];
					DAE.Client.DataSetState LOldState = LSource.DataView.State;
					LField.AsBoolean = !(LField.HasValue() && LField.AsBoolean);
					if (LOldState == DAE.Client.DataSetState.Browse)
					{
						try
						{
							LSource.DataView.Post();
						}
						catch
						{
							LSource.DataView.Cancel();
							throw;
						}
					}
					return true;
				}
			}
			return false;
		}

		public override void InternalRenderCell(HtmlTextWriter AWriter, Alphora.Dataphor.DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex)
		{
			bool LHasValue = ACurrentRow.HasValue(ColumnName);
			bool LValue = false;
			if (LHasValue)
				LValue = ACurrentRow[ColumnName].AsBoolean;

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, GetClass(ACurrentRow, AIsActiveRow));
			if (!ReadOnly)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&RowIndex={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], CheckID, ARowIndex));
			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "middle");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "center");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "10");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "10");
			AWriter.AddAttribute
			(
				HtmlTextWriterAttribute.Src, 
				String.Format
				(
					"images/{0}{1}.gif", 
					LValue.ToString().ToLower(), 
					ReadOnly ? "readonly" : ""
				)
			);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TD
		}
	}

	public class SequenceColumn : GridColumn, ISequenceColumn, IWebHandler
	{
		public SequenceColumn()
		{
			FMoveID = Session.GenerateID();
		}

		protected override void Dispose(bool ADisposing)
		{
			CancelMove();
			base.Dispose(ADisposing);
		}

		// Image

		private string FImage = String.Empty;
		public string Image
		{
			get { return FImage; }
			set 
			{ 
				if (FImage != value)
				{
					FImage = value;
					DeallocateImage();
					if (Active)
						AllocateImage();
				}
			}
		}

		protected void AllocateImage()
		{
			FImageID = WebSession.ImageCache.Allocate(Image);
		}

		protected void DeallocateImage()
		{
			if (FImageID != String.Empty)
			{
				WebSession.ImageCache.Deallocate(FImageID);
				FImageID = String.Empty;
			}
		}

		// ImageID

		private string FImageID = String.Empty;
		public string ImageID
		{
			get { return FImageID; }
		}

		// MoveID

		private string FMoveID;
		public string MoveID { get { return FMoveID; } }

		// Script

		private string FScript = String.Empty;
		public string Script
		{
			get { return FScript; }
			set { FScript = (value == null ? String.Empty : value); }
		}

		// GridColumn

		public override void InternalRenderCell(HtmlTextWriter AWriter, Alphora.Dataphor.DAE.Runtime.Data.Row ACurrentRow, bool AIsActiveRow, int ARowIndex)
		{
			string LClass;
			if (AIsActiveRow)
				LClass = "gridcellcurrent";
			else
				LClass = "gridcell";
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, LClass);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "bottom");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			string LImageLink;
			if (FMovingRow != null)
			{
				LImageLink = "images/place.png";
				AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "18");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "19");
			}
			else
			{
				if (FImageID != String.Empty)
					LImageLink = "ViewImage.aspx?ImageID=" + FImageID;
				else
					LImageLink = "images/move.png";
			}
			AWriter.AddAttribute(HtmlTextWriterAttribute.Src, LImageLink);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&RowIndex={2}&Y='+event.offsetY,event)", (string)Session.Get(this).Context.Session["DefaultPage"], FMoveID, ARowIndex));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();
		}

		protected virtual void SequenceChange(DAE.Runtime.Data.Row AFromRow, DAE.Runtime.Data.Row AToRow, bool AAbove)
		{
			if (ParentGrid.DataLink.Active)
			{
				DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
				try
				{
					foreach (DAE.Schema.Column LColumn in AFromRow.DataType.Columns) 
					{
						LParams.Add(new DAE.Runtime.DataParam("AFromRow." + LColumn.Name, LColumn.DataType, DAE.Language.Modifier.In, AFromRow[LColumn.Name]));
						LParams.Add(new DAE.Runtime.DataParam("AToRow." + LColumn.Name, LColumn.DataType, DAE.Language.Modifier.In, AToRow[LColumn.Name]));
					}
					LParams.Add(new DAE.Runtime.DataParam("AAbove", AFromRow.Process.DataTypes.SystemBoolean, DAE.Language.Modifier.In, new DAE.Runtime.Data.Scalar(AFromRow.Process, AFromRow.Process.DataTypes.SystemBoolean, AAbove)));
					HostNode.Session.ExecuteScript(FScript, LParams);
					ParentGrid.DataLink.DataSet.Refresh();
				}
				finally
				{
					foreach (DAE.Runtime.DataParam LParam in LParams)
						LParam.Value.Dispose();
				}
			}
		}

		private DAE.Runtime.Data.Row FMovingRow = null;

		private void CancelMove()
		{
			if (FMovingRow != null)
			{
				FMovingRow.Dispose();
				FMovingRow = null;
			}
		}

		// IWebHandler

		public virtual bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, FMoveID))
			{
				string LRowIndex = AContext.Request.QueryString["RowIndex"];
				if (LRowIndex != null)
				{
					if (FMovingRow != null)
					{
						string LPosY = AContext.Request.QueryString["Y"];
						if (LPosY != null)
						{
							DAE.Runtime.Data.Row LTarget = ParentGrid.DataLink.Buffer(Int32.Parse(LRowIndex));
							SequenceChange(FMovingRow, LTarget, Int32.Parse(LPosY) < 10);
						}
					}
					else
					{
						FMovingRow = (DAE.Runtime.Data.Row)ParentGrid.DataLink.Buffer(Int32.Parse(LRowIndex)).Copy();
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
			FPageUpID = Session.GenerateID();
			FPageDownID = Session.GenerateID();
			FFirstID = Session.GenerateID();
			FLastID = Session.GenerateID();
			FDataLink = new DAE.Client.DataLink();
			FDataLink.OnActiveChanged += new DAE.Client.DataLinkHandler(DataLinkActiveChanged);
		}

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			OnDoubleClick = null;
		}

		// RowCount

		private int FRowCount = 10;
		public int RowCount
		{
			get { return FRowCount; }
			set
			{
				FRowCount = value;
				FDataLink.BufferCount = FRowCount;
			}
		}

        //ColumnCount

        private int FColumnCount = -1;
        public int ColumnCount
        {
            get { return FColumnCount; }
            set
            {
                if (value < 1)
                    FColumnCount = -1;
                else
                    FColumnCount = value;
            }
        }

		// OnDoubleClick
	
		// TODO: Support for double-click in the web client
		
		private IAction FOnDoubleClick;
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

		// PageUpID

		private string FPageUpID;
		public string PageUpID { get { return FPageUpID; } }
		
		// PageDownID

		private string FPageDownID;
		public string PageDownID { get { return FPageDownID; } }

		// FirstID

		private string FFirstID;
		public string FirstID { get { return FFirstID; } }

		// LastID

		private string FLastID;
		public string LastID { get { return FLastID; } }

		// DataElement

		protected override void SourceChanged(ISource AOldSource)
		{
			FDataLink.Source = ( Source == null ? null : Source.DataSource );
		}

		// Element

		protected virtual void RenderNavButton(HtmlTextWriter AWriter, string AImageSrc, string AID, bool AEnabled)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridnav");
			if (AEnabled)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(Session.Get(this).Context, AID));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "16");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			if (AEnabled)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Src, AImageSrc);
			else
				AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/pixel.gif");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TD
		}

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			bool LBOF = !FDataLink.Active || FDataLink.DataSet.BOF;
			bool LEOF = !FDataLink.Active || FDataLink.DataSet.EOF;

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "grid");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Rowspan, "4");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			RenderGrid(AWriter);
			AWriter.RenderEndTag();

			RenderNavButton(AWriter, "images/first.png", FFirstID, !LBOF);
			AWriter.RenderEndTag();

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderNavButton(AWriter, "images/pageup.png", FPageUpID, !LBOF);
			AWriter.RenderEndTag();

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderNavButton(AWriter, "images/pagedown.png", FPageDownID, !LEOF);
			AWriter.RenderEndTag();

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderNavButton(AWriter, "images/last.png", FLastID, !LEOF);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TABLE
		}

		protected virtual void RenderGrid(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "innergrid");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			// Header

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridheaderrow");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			foreach (IWebGridColumn LColumn in Children)
				LColumn.RenderHeader(AWriter);

			AWriter.RenderEndTag();

			// Rows

			bool LCurrent;
			DAE.Runtime.Data.Row LCurrentRow;
			int LCount = FDataLink.LastOffset + 1;
			for (int LRowIndex = 0; LRowIndex < LCount; LRowIndex++) 
			{
				LCurrent = LRowIndex == FDataLink.ActiveOffset;
				if (LCurrent)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridrowcurrent");
				else
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('{0}?ActionID={1}&RowIndex={2}',event)", (string)Session.Get(this).Context.Session["DefaultPage"], ID, LRowIndex.ToString()));
					if ((LRowIndex % 2) == 1)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridrowalt");
					else
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridrow");
				}
				AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

				LCurrentRow = FDataLink.Buffer(LRowIndex);
				foreach (IWebGridColumn LColumn in Children)
					LColumn.RenderCell(AWriter, LCurrentRow, LCurrent, LRowIndex);

				AWriter.RenderEndTag();
			}

			AWriter.RenderEndTag();	// TABLE
		}

		private bool FAutoBuiltColumns;

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IWebGridColumn).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}
		
		protected override void Activate()
		{
			FDataLink.BufferCount = FRowCount;
			base.Activate();
		}

		// DataLink

		protected DAE.Client.DataLink FDataLink;
		public DAE.Client.DataLink DataLink 
		{
			get { return FDataLink; }
		}

        //this is a no-op in the web client
        private bool FUseNaturalMaxWidth;
        public bool UseNaturalMaxWidth
        {
            get { return FUseNaturalMaxWidth; }
            set { FUseNaturalMaxWidth = value; }
        }

		private void DataLinkActiveChanged(DAE.Client.DataLink ALink, DAE.Client.DataSet ADataSet)
		{
			if (ALink.Active)
			{
				// if no children, then default to a full set of columns.
				if (Children.Count == 0) 
				{
					foreach (DAE.Client.DataField LColumn in Source.DataView.Fields)
					{
						TextColumn LNewColumn = new TextColumn();
						LNewColumn.ColumnName = LColumn.ColumnName;
						LNewColumn.Title = LColumn.ColumnName;
						Children.Add(LNewColumn);
					}
					FAutoBuiltColumns = true;
				}
					FAutoBuiltColumns = false;
			}
			else
			{
				if (FAutoBuiltColumns)
					Children.Clear();
			}
		}

		public void MoveTo(int AIndex) 
		{
			FDataLink.DataSet.MoveBy(AIndex - FDataLink.ActiveOffset);
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			else
			{
				if (Session.IsActionLink(AContext, FPageUpID))
					Source.DataView.MoveBy(-RowCount);
				else if (Session.IsActionLink(AContext, FPageDownID))
					Source.DataView.MoveBy(RowCount);
				else if (Session.IsActionLink(AContext, FFirstID))
					Source.DataView.First();
				else if (Session.IsActionLink(AContext, FLastID))
					Source.DataView.Last();
				else if (Session.IsActionLink(AContext, ID))
				{
					string LRowIndex = AContext.Request.QueryString["RowIndex"];
					if ((LRowIndex != null) && (LRowIndex != String.Empty))
						MoveTo(Int32.Parse(LRowIndex));
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

		protected override void InternalExecute(Alphora.Dataphor.Frontend.Client.INode ASender, Alphora.Dataphor.Frontend.Client.EventParams AParams)
		{
			if (Source != null)
			{
				Host LHost = (Host)HostNode.Session.CreateHost();
				try
				{
					EditFilterForm LForm = new EditFilterForm();
					try
					{
						LHost.Children.Add(LForm);
					}
					catch
					{
						LForm.Dispose();
						throw;
					}
					LForm.Text = Strings.Get("EditFilterFormTitle");
					LForm.FilterExpression = Source.DataView.Filter;
					LHost.Open();
					LForm.Show((IFormInterface)FindParent(typeof(IFormInterface)), new FormInterfaceHandler(FilterAccepted), null, FormMode.None);
				}
				catch
				{
					LHost.Dispose();
					throw;
				}
			}
		}

		private void FilterAccepted(IFormInterface AForm)
		{
			if ((Source != null) && (Source.DataView != null))
			{
				Source.DataView.Filter = ((EditFilterForm)AForm).FilterExpression;
				Source.DataView.Open();		// Ensure the DataView is open in case a previous filter change caused it to close
			}
		}
	}

	internal class EditFilterForm : FormInterface
	{
		public EditFilterForm()
		{
			FNameID = Session.GenerateID();
		}

		// NameID

		private string FNameID;
		public string NameID { get { return FNameID; } }

		// FilterExpression

		private string FFilterExpression = String.Empty;
		public string FilterExpression
		{
			get { return FFilterExpression; }
			set { FFilterExpression = ( value == null ? String.Empty : value ); }
		}

		// Element

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			base.InternalRender(AWriter);
			AWriter.Write(HttpUtility.HtmlEncode(Strings.Get("FilterExpression")));
			AWriter.Write("<br>");
			
			AWriter.AddAttribute(HtmlTextWriterAttribute.Name, FNameID);
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cols, "60");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Rows, "4");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Wrap, "on");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Textarea);

			AWriter.Write(HttpUtility.HtmlEncode(FFilterExpression));

			AWriter.RenderEndTag();
		}

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			string LValue = AContext.Request.Form[FNameID];
			if (LValue != null)
			{
				FFilterExpression = LValue;
				return true;
			}
			else
				return false;
		}
	}
}