/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Web;
using System.Web.UI;
using System.Drawing;
using System.ComponentModel;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class Tree : ColumnElement, ITree
	{
		// RootExpression

		private string FRootExpression = String.Empty;
		public string RootExpression
		{
			get { return FRootExpression; }
			set
			{
				if (FRootExpression != value)
				{
					FRootExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ChildExpression
		
		private string FChildExpression = String.Empty;
		public string ChildExpression
		{
			get { return FChildExpression; }
			set
			{
				if (FChildExpression != value)
				{
					FChildExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ParentExpression

		private string FParentExpression = String.Empty;
		public string ParentExpression
		{
			get { return FParentExpression; }
			set
			{
				if (FParentExpression != value)
				{
					FParentExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		protected virtual void InternalUpdateTreeView()
		{
			BuildTree();
		}

		// Width

		// TODO: Define what Width and Height mean to the web client

		private int FWidth = 25;
		[DefaultValue(25)]
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}

		// Height

		private int FHeight = 20;
		[DefaultValue(20)]
		[Description("Height (in rows) of the control.")]
		public int Height
		{
			get { return FHeight; }
			set { FHeight = value; }
		}

		// Indent

		private int FIndent = 10;
		/// <summary> The number of pixels to indent each level of the tree. </summary>
		public int Indent
		{
			get { return FIndent; }
			set { FIndent = value; }
		}

		// Nodes

		private TreeNodes FNodes = new TreeNodes();
		public TreeNodes Nodes { get { return FNodes; } }

		// SelectedNode

		private TreeNode FSelectedNode;
		public TreeNode SelectedNode
		{
			get { return FSelectedNode; }
		}

		private bool FInternalSelecting;

		internal void InternalSelect(TreeNode ANode)
		{
			FInternalSelecting = true;
			try
			{
				Source.DataView.FindKey(ANode.Key);
				FSelectedNode = ANode;
			}
			finally
			{
				FInternalSelecting = false;
			}
		}

		// DataElement

		protected override void SourceChanged(ISource AOldSource) 
		{
			if (AOldSource != null) 
			{
				Source.DataChanged -= new DAE.Client.DataLinkHandler(DataLinkDataChanged);
				Source.ActiveChanged -= new DAE.Client.DataLinkHandler(DataLinkActiveChanged);
			}
			if (Source != null)
			{
				Source.DataChanged += new DAE.Client.DataLinkHandler(DataLinkDataChanged);
				Source.ActiveChanged += new DAE.Client.DataLinkHandler(DataLinkActiveChanged);
			}
		}

		protected void DataLinkDataChanged(DAE.Client.DataLink ALink, DAE.Client.DataSet ADataSet) 
		{
			if (ALink.Active && !FInternalSelecting)
			{
				ClearNodes();
				BuildTree();
			}
		}

		protected void DataLinkActiveChanged(DAE.Client.DataLink ALink, DAE.Client.DataSet ADataSet) 
		{
			if (!ALink.Active)
			{
				ClearNodes();
				StopProcess();
			}
			else
			{
				StartProcess();
				if (!ALink.DataSet.IsEmpty())
					BuildTree();
			}
		}

		#region Plans, Processes, & Params

		// GetExpression()

		private Parser FParser = new Parser();
		
		protected string GetExpression(string AExpression)
		{
			CursorDefinition LDefinition = (CursorDefinition)FParser.ParseCursorDefinition(AExpression);
			LDefinition.Capabilities = CursorCapability.Navigable;
			LDefinition.CursorType = DAE.CursorType.Dynamic;
			LDefinition.Isolation = CursorIsolation.Browse;
			return new D4TextEmitter().Emit(LDefinition);
		}

		// RootPlan

		protected IServerExpressionPlan FRootPlan;
		protected DAE.Runtime.DataParams FRootParams;

		protected IServerExpressionPlan PrepareRootPlan()
		{
			if (FRootPlan == null)
			{
				FRootParams = new DAE.Runtime.DataParams();
				Source.DataView.GetAllParams(FRootParams);
				FRootPlan = FProcess.PrepareExpression(GetExpression(FRootExpression), FRootParams);
			}
			return FRootPlan;
		}
		
		protected void UnprepareRootPlan()
		{
			if (FRootPlan != null)
			{
				FProcess.UnprepareExpression(FRootPlan);
				FRootParams = null;
				FRootPlan = null;
			}
		}

		// ChildPlan

		protected IServerExpressionPlan FChildPlan;

		protected IServerExpressionPlan PrepareChildPlan()
		{
			PrepareParams();
			if (FChildPlan == null)
				FChildPlan = FProcess.PrepareExpression(GetExpression(FChildExpression), FParams);
			return FChildPlan;
		}
		
		protected void UnprepareChildPlan()
		{
			if (FChildPlan != null)
			{
				FProcess.UnprepareExpression(FChildPlan);
				FChildPlan = null;
			}
		}

		protected internal IServerCursor OpenChildCursor(DAE.Runtime.Data.Row AKey)
		{
			PrepareChildPlan();
			SetParams(AKey);
			return FChildPlan.Open(FParams);
		}
		
		protected internal void CloseChildCursor(IServerCursor ACursor)
		{
			FChildPlan.Close(ACursor);
			ClearParams();
		}
		
		// ParentPlan

		protected IServerExpressionPlan FParentPlan;
		
		protected IServerExpressionPlan PrepareParentPlan()
		{
			PrepareParams();
			if (FParentPlan == null)
				FParentPlan = FProcess.PrepareExpression(GetExpression(FParentExpression), FParams);
			return FParentPlan;
		}
		
		protected void UnprepareParentPlan()
		{
			if (FParentPlan != null)
			{
				FProcess.UnprepareExpression(FParentPlan);
				FParentPlan = null;
			}
		}
		
		protected  internal IServerCursor OpenParentCursor(DAE.Runtime.Data.Row AKey)
		{
			PrepareParentPlan();
			SetParams(AKey);
			return FParentPlan.Open(FParams);
		}
		
		protected internal void CloseParentCursor(IServerCursor ACursor)
		{
			FParentPlan.Close(ACursor);
			ClearParams();
		}

		// Process

		private IServerProcess FProcess;
		public IServerProcess Process { get { return FProcess; } }

		private void StartProcess()
		{
			FProcess = Source.DataView.Session.ServerSession.StartProcess(new DAE.ProcessInfo(Source.DataView.Session.SessionInfo));
		}
		
		private void StopProcess()
		{
			if (FProcess != null)
			{
				UnprepareParentPlan();
				UnprepareChildPlan();
				UnprepareRootPlan();
				FProcess.Session.StopProcess(FProcess);
				FProcess = null;
			}
		}

		// Params

		DAE.Runtime.DataParams FParams;

		protected void PrepareParams()
		{
			if (FParams == null)
			{
				FParams = new DAE.Runtime.DataParams();
				foreach (DAE.Schema.OrderColumn LOrderColumn in Source.DataView.Order.Columns)
					FParams.Add(new DAE.Runtime.DataParam("ACurrent" + LOrderColumn.Column.Name, LOrderColumn.Column.DataType, Modifier.Const));
			}
		}
		
		protected void SetParams(DAE.Runtime.Data.Row AKey)
		{
			for (int LIndex = 0; LIndex < Source.DataView.Order.Columns.Count; LIndex++)
				if (AKey.HasValue(LIndex))
					FParams[LIndex].Value = AKey[LIndex];
		}
		
		protected void ClearParams()
		{
			foreach (DAE.Runtime.DataParam LParam in FParams)
				LParam.Value = null;
		}

		#endregion

		private void ClearNodes()
		{
			foreach (TreeNode LNode in Nodes)
				LNode.Dispose();
			Nodes.Clear();
		}

		public void BuildTree()
		{
			ClearNodes();

			if (IsFieldActive() && (Source.DataView.State != DAE.Client.DataSetState.Insert)) 
			{
				// Open a dynamic navigable browse cursor on the root expression
				PrepareRootPlan();
				IServerCursor LCursor = FRootPlan.Open(FRootParams);
				try
				{
					DAE.Runtime.Data.Row LKey;
					int LColumnIndex;
					string LText;
					while (LCursor.Next())
					{
						LKey = new DAE.Runtime.Data.Row(FProcess, new DAE.Schema.RowType(Source.DataView.Order.Columns));
						try
						{
							using (DAE.Runtime.Data.Row LRow = LCursor.Select())
							{
								LRow.CopyTo(LKey);
								LColumnIndex = LRow.DataType.Columns.IndexOf(ColumnName);
								if (LRow.HasValue(LColumnIndex))
									LText = LRow[LColumnIndex].AsDisplayString;
								else
									LText = Strings.Get("NoValue");
							}
							Nodes.Add(new TreeNode(this, LText, LKey, 0, null));
						}
						catch
						{
							LKey.Dispose();
							throw;
						}
					}
				}
				finally
				{
					FRootPlan.Close(LCursor);
				}
				
				foreach (TreeNode LNode in Nodes)
					LNode.BuildChildren();

				SelectNode(Source.DataView.GetKey());
			}
		}

		protected internal TreeNode FindChild(DAE.Runtime.Data.Row AChildKey)
		{
			foreach (TreeNode LNode in Nodes)
			{
				if (LNode.IsRow(AChildKey))
					return LNode;
			}
			return null;
		}

		protected internal TreeNode Search(DAE.Runtime.Data.Row AChildKey)
		{
			TreeNode LResult = FindChild(AChildKey);
			if (LResult == null)
				foreach (TreeNode LNode in Nodes)
				{
					LResult = LNode.Search(AChildKey);
					if (LResult != null)
						break;
				}
			return LResult;
		}

		private bool KeysEqual(DAE.Runtime.Data.Row AKey1, DAE.Runtime.Data.Row AKey2)
		{
			if (AKey2.DataType.Equals(AKey1.DataType))
			{
				string LCompareValue;
				string LName2;
				for (int LIndex = 0; LIndex < AKey2.DataType.Columns.Count; LIndex++)
				{
					LName2 = AKey2.DataType.Columns[LIndex].Name;
					if (AKey1.HasValue(LName2))
						LCompareValue = AKey1[LName2].AsString;
					else
						LCompareValue = String.Empty;

					if (AKey2[LIndex].AsString != LCompareValue)
						return false;
				}
				return true;
			}
			else
				return false;
		}

		protected void BuildParentPath(DAE.Runtime.Data.Row AKey, ArrayList APath)
		{
			foreach (DAE.Runtime.Data.Row LKey in APath)
			{
				if (KeysEqual(AKey, LKey))
					throw new WebClientException(WebClientException.Codes.TreeViewInfiniteLoop);
			}
			APath.Add(AKey);
			IServerCursor LCursor = OpenParentCursor(AKey);
			try
			{
				if (LCursor.Next())
				{
					AKey = new DAE.Runtime.Data.Row(FProcess, new RowType(Source.DataView.Order.Columns));
					using (DAE.Runtime.Data.Row LSelected = LCursor.Select())
						LSelected.CopyTo(AKey);
				}
				else
					AKey = null;
			}
			finally
			{
				CloseParentCursor(LCursor);
			}
			
			if (AKey != null)
				if (FindChild(AKey) == null)
					BuildParentPath(AKey, APath);
				else
					APath.Add(AKey);
		}

		private bool FSelecting;

		/// <summary> Sets the selected node of the tree. Builds children as needed.</summary>
		protected internal void SelectNode(DAE.Runtime.Data.Row AKey)
		{
			TreeNode LNode = Search(AKey);
			if (LNode == null)
			{
				ArrayList LPath = new ArrayList();
				BuildParentPath((DAE.Runtime.Data.Row)AKey.Copy(), LPath);
				try
				{
					for (int LIndex = LPath.Count - 1; LIndex >= 0; LIndex--)
					{
						if (LIndex == LPath.Count - 1)
						{
							LNode = FindChild((DAE.Runtime.Data.Row)LPath[LIndex]);
							if ((LNode == null) && !FSelecting)
							{
								BuildTree();
								FSelecting = true;
								try
								{
									SelectNode(AKey);
								}
								finally
								{
									FSelecting = false;
								}
								return;
							}
						}
						else
							LNode = LNode.FindChild((DAE.Runtime.Data.Row)LPath[LIndex]);

						if (LNode == null)
							throw new WebClientException(WebClientException.Codes.TreeViewUnconnected);

						if (LIndex != 0)
							LNode.BuildChildren();
					}
				}
				finally
				{
					for (int LIndex = 0; LIndex < LPath.Count; LIndex++)
						((DAE.Runtime.Data.Row)LPath[LIndex]).Dispose();
				}
			} 

			// make sure the node is visible
			if ((LNode.Parent != null) && (!LNode.Parent.IsExpanded))
				LNode.Parent.Expand();

			FSelectedNode = LNode;
		}

		// Element

		protected override void InternalRender(HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "tree");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Div);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "tree");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			string LHint = GetHint();
			if (LHint != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Title, LHint, true);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			if (Nodes.Count == 0)
			{
				// Ensure that the browser doesn't eliminate the table
				AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
				Session.RenderDummyImage(AWriter, "1", "20");
				AWriter.RenderEndTag();
				AWriter.RenderEndTag();
			}
			else
			{
				HttpContext LContext = Session.Get(this).Context;
				foreach (TreeNode LChild in Nodes)
					LChild.Render(LContext, AWriter);
			}

			AWriter.RenderEndTag();	// TABLE
			AWriter.RenderEndTag();	// DIV
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext AContext)
		{
			if (base.ProcessRequest(AContext))
				return true;
			else
			{
				foreach (TreeNode LNode in FNodes)
					if (LNode.ProcessRequest(AContext))
						return true;
				return false;
			}
		}
	}

	public class TreeNodes : List {}

	public class TreeNode : Disposable
	{
		public TreeNode(Tree ATree, string AText, DAE.Runtime.Data.Row AKey, int ADepth, TreeNode AParent)
		{
			FID = Session.GenerateID();
			FButtonID = Session.GenerateID();
			FTree = ATree;
			FText = AText;
			FKey = AKey;
			FDepth = ADepth;
			FParent = AParent;
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					foreach (TreeNode LChild in Nodes)
						LChild.Dispose();
				}
				finally
				{
					Nodes.Clear();
				}
				base.Dispose(ADisposing);
			}
			finally
			{
				if (FKey != null)
					FKey.Dispose();
			}
		}

		// Parent

		private TreeNode FParent;
		public TreeNode Parent { get { return FParent; } }

		// ID

		private string FID;
		public string ID { get { return FID; } }

		// ButtonID

		private string FButtonID;
		public string ButtonID { get { return FButtonID; } }

		// Depth

		private int FDepth;
		public int Depth { get { return FDepth; } }

		// Tree

		private Tree FTree;
		public Tree Tree { get { return FTree; } }

		// Text

		private string FText;
		public string Text { get { return FText; } }

		// Key

		/// <summary> This Key is used by FindKey when it is selected. Not Parent/Child relationship. </summary>
		private DAE.Runtime.Data.Row FKey;
		public DAE.Runtime.Data.Row Key { get { return FKey; } }

		// Nodes

		private TreeNodes FNodes = new TreeNodes();
		public TreeNodes Nodes { get { return FNodes; } }

		/// <summary> Returns a child node matching the key (non reflectively or recursively). </summary>
		public TreeNode FindChild(DAE.Runtime.Data.Row AKey)
		{
			foreach (TreeNode LNode in Nodes)
			{
				if (LNode.IsRow(AKey))
					return LNode;
			}
			return null;
		}

		/// <summary> Returns this node or any child node matching the key (recursively). </summary>
		public TreeNode Search(DAE.Runtime.Data.Row AKey)
		{
			if (IsRow(AKey))
				return this;
			TreeNode LResult = FindChild(AKey);
			if (LResult != null)
				return LResult;
			foreach (TreeNode LNode in Nodes)
			{
				LResult = LNode.Search(AKey);
				if (LResult != null)
					return LResult;
			}
			return null;
		}

		// IsExpanded

		private bool FIsExpanded = false;
		public bool IsExpanded
		{
			get { return FIsExpanded && ((Parent == null) || Parent.IsExpanded); }
			set
			{
				if (value != FIsExpanded)
				{
					if (value)
						Expand();
					else
						Collapse();
				}
			}
		}

		public void Expand()
		{
			if (!FIsExpanded)
			{
				if ((Parent != null) && (!Parent.IsExpanded))
					Parent.Expand();
				if (!FIsExpanded)
				{
					foreach (TreeNode LTreeNode in Nodes)
						LTreeNode.BuildChildren();
					FIsExpanded = true;
				}
			}
		}

		public void Collapse()
		{
			if (FIsExpanded)
			{
				if (Search(Tree.SelectedNode.FKey) != null)
					Tree.InternalSelect(this);
				FIsExpanded = false;
			}
		}

		/// <summary> Creates this nodes immediate children. Avoids duplication. </summary>
		public void BuildChildren()
		{
			// Open a dynamic navigable browse cursor on the child expression
			IServerCursor LCursor = Tree.OpenChildCursor(FKey);
			try
			{
				DAE.Runtime.Data.Row LKey;
				string LText;
				int LIndex = 0;
				int LColumnIndex;
				while (LCursor.Next())
				{
					LKey = new DAE.Runtime.Data.Row(Tree.Process, new RowType(Tree.Source.DataView.Order.Columns));
					try
					{
						using (DAE.Runtime.Data.Row LRow = LCursor.Select())
						{
							LRow.CopyTo(LKey);
							LColumnIndex = LRow.DataType.Columns.IndexOf(Tree.ColumnName);
							if (LRow.HasValue(LColumnIndex))
								LText = LRow[LColumnIndex].AsDisplayString;
							else
								LText = Strings.Get("NoValue");
						}
								
						if (FindChild(LKey) == null)
							Nodes.Insert(LIndex, new TreeNode(Tree, LText, LKey, FDepth + 1, this));
						LIndex++;
					}
					catch
					{
						LKey.Dispose();
						throw;
					}
				}
			}
			finally
			{
		  		Tree.CloseChildCursor(LCursor);
			}
		}

		/// <summary> True if this node is the row(has the key). </summary>
		public bool IsRow(DAE.Runtime.Data.Row AKey)
		{
			if (FKey.DataType.Equals(AKey.DataType))
			{
				string LCompareValue;
				string LName;
				for (int LIndex = 0; LIndex < FKey.DataType.Columns.Count; LIndex++)
				{
					LName = FKey.DataType.Columns[LIndex].Name;
					if (AKey.HasValue(LName))
						LCompareValue = AKey[LName].AsString;
					else
						LCompareValue = String.Empty;
					if (FKey[LIndex].AsString != LCompareValue)
						return false;
				}
				return true;
			}
			return false;
		}

		public void Render(HttpContext AContext, HtmlTextWriter AWriter)
		{
			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "treenode");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

			Session.RenderDummyImage(AWriter, (FDepth * Tree.Indent).ToString(), "1");
			
			if (Nodes.Count > 0)
			{
				if (FIsExpanded)
					AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/collapse.png");
				else
					AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/expand.png");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "10");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "10");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(AContext, FButtonID));
				AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
				AWriter.RenderEndTag();
				Session.RenderDummyImage(AWriter, "4", "1");
			}
			else
				Session.RenderDummyImage(AWriter, "14", "1");

			if (FTree.SelectedNode == this)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "treenodeselected");
			else
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "treenode");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(AContext, FID));
			AWriter.RenderBeginTag(HtmlTextWriterTag.Font);
			AWriter.Write(HttpUtility.HtmlEncode(Text.Trim()));
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();	// TD
			AWriter.RenderEndTag();	// TR

			if (FIsExpanded)
				foreach (TreeNode LChild in Nodes)
					LChild.Render(AContext, AWriter);
		}

		public bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, FButtonID))
				IsExpanded = !IsExpanded;
			else if (Session.IsActionLink(AContext, FID))
				FTree.InternalSelect(this);
			else
			{
				foreach (TreeNode LChild in Nodes)
					if (LChild.ProcessRequest(AContext))
						return true;
				return false;
			}
			return true;
		}
	}

}