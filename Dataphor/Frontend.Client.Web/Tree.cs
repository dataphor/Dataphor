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

		private string _rootExpression = String.Empty;
		public string RootExpression
		{
			get { return _rootExpression; }
			set
			{
				if (_rootExpression != value)
				{
					_rootExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ChildExpression
		
		private string _childExpression = String.Empty;
		public string ChildExpression
		{
			get { return _childExpression; }
			set
			{
				if (_childExpression != value)
				{
					_childExpression = ( value == null ? String.Empty : value );
					if (Active)
						InternalUpdateTreeView();
				}
			}
		}

		// ParentExpression

		private string _parentExpression = String.Empty;
		public string ParentExpression
		{
			get { return _parentExpression; }
			set
			{
				if (_parentExpression != value)
				{
					_parentExpression = ( value == null ? String.Empty : value );
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

		private int _width = 25;
		[DefaultValue(25)]
		public int Width
		{
			get { return _width; }
			set { _width = value; }
		}

		// Height

		private int _height = 20;
		[DefaultValue(20)]
		[Description("Height (in rows) of the control.")]
		public int Height
		{
			get { return _height; }
			set { _height = value; }
		}

		// Indent

		private int _indent = 10;
		/// <summary> The number of pixels to indent each level of the tree. </summary>
		public int Indent
		{
			get { return _indent; }
			set { _indent = value; }
		}

		// Nodes

		private TreeNodes _nodes = new TreeNodes();
		public TreeNodes Nodes { get { return _nodes; } }

		// SelectedNode

		private TreeNode _selectedNode;
		public TreeNode SelectedNode
		{
			get { return _selectedNode; }
		}

		private bool _internalSelecting;

		internal void InternalSelect(TreeNode node)
		{
			_internalSelecting = true;
			try
			{
				Source.DataView.FindKey(node.Key);
				_selectedNode = node;
			}
			finally
			{
				_internalSelecting = false;
			}
		}

		// DataElement

		protected override void SourceChanged(ISource oldSource) 
		{
			if (oldSource != null) 
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

		protected void DataLinkDataChanged(DAE.Client.DataLink link, DAE.Client.DataSet dataSet) 
		{
			if (link.Active && !_internalSelecting)
			{
				ClearNodes();
				BuildTree();
			}
		}

		protected void DataLinkActiveChanged(DAE.Client.DataLink link, DAE.Client.DataSet dataSet) 
		{
			if (!link.Active)
			{
				ClearNodes();
				StopProcess();
			}
			else
			{
				StartProcess();
				if (!link.DataSet.IsEmpty())
					BuildTree();
			}
		}

		#region Plans, Processes, & Params

		// GetExpression()

		private Parser _parser = new Parser();
		
		protected string GetExpression(string expression)
		{
			CursorDefinition definition = (CursorDefinition)_parser.ParseCursorDefinition(expression);
			definition.Capabilities = CursorCapability.Navigable;
			definition.CursorType = DAE.CursorType.Dynamic;
			definition.Isolation = CursorIsolation.Browse;
			return new D4TextEmitter().Emit(definition);
		}

		// RootPlan

		protected IServerExpressionPlan _rootPlan;
		protected DAE.Runtime.DataParams _rootParams;

		protected IServerExpressionPlan PrepareRootPlan()
		{
			if (_rootPlan == null)
			{
				_rootParams = new DAE.Runtime.DataParams();
				Source.DataView.GetAllParams(_rootParams);
				_rootPlan = _process.PrepareExpression(GetExpression(_rootExpression), _rootParams);
			}
			return _rootPlan;
		}
		
		protected void UnprepareRootPlan()
		{
			if (_rootPlan != null)
			{
				_process.UnprepareExpression(_rootPlan);
				_rootParams = null;
				_rootPlan = null;
			}
		}

		// ChildPlan

		protected IServerExpressionPlan _childPlan;

		protected IServerExpressionPlan PrepareChildPlan()
		{
			PrepareParams();
			if (_childPlan == null)
				_childPlan = _process.PrepareExpression(GetExpression(_childExpression), _params);
			return _childPlan;
		}
		
		protected void UnprepareChildPlan()
		{
			if (_childPlan != null)
			{
				_process.UnprepareExpression(_childPlan);
				_childPlan = null;
			}
		}

		protected internal IServerCursor OpenChildCursor(DAE.Runtime.Data.IRow key)
		{
			PrepareChildPlan();
			SetParams(key);
			return _childPlan.Open(_params);
		}
		
		protected internal void CloseChildCursor(IServerCursor cursor)
		{
			_childPlan.Close(cursor);
			ClearParams();
		}
		
		// ParentPlan

		protected IServerExpressionPlan _parentPlan;
		
		protected IServerExpressionPlan PrepareParentPlan()
		{
			PrepareParams();
			if (_parentPlan == null)
				_parentPlan = _process.PrepareExpression(GetExpression(_parentExpression), _params);
			return _parentPlan;
		}
		
		protected void UnprepareParentPlan()
		{
			if (_parentPlan != null)
			{
				_process.UnprepareExpression(_parentPlan);
				_parentPlan = null;
			}
		}
		
		protected  internal IServerCursor OpenParentCursor(DAE.Runtime.Data.IRow key)
		{
			PrepareParentPlan();
			SetParams(key);
			return _parentPlan.Open(_params);
		}
		
		protected internal void CloseParentCursor(IServerCursor cursor)
		{
			_parentPlan.Close(cursor);
			ClearParams();
		}

		// Process

		private IServerProcess _process;
		public IServerProcess Process { get { return _process; } }

		private void StartProcess()
		{
			_process = Source.DataView.Session.ServerSession.StartProcess(new DAE.ProcessInfo(Source.DataView.Session.SessionInfo));
		}
		
		private void StopProcess()
		{
			if (_process != null)
			{
				UnprepareParentPlan();
				UnprepareChildPlan();
				UnprepareRootPlan();
				_process.Session.StopProcess(_process);
				_process = null;
			}
		}

		// Params

		DAE.Runtime.DataParams _params;

		protected void PrepareParams()
		{
			if (_params == null)
			{
				_params = new DAE.Runtime.DataParams();
				foreach (DAE.Schema.OrderColumn orderColumn in Source.DataView.Order.Columns)
					_params.Add(new DAE.Runtime.DataParam("ACurrent" + orderColumn.Column.Name, orderColumn.Column.DataType, Modifier.Const));
			}
		}
		
		protected void SetParams(DAE.Runtime.Data.IRow key)
		{
			for (int index = 0; index < Source.DataView.Order.Columns.Count; index++)
				if (key.HasValue(index))
					_params[index].Value = key[index];
		}
		
		protected void ClearParams()
		{
			foreach (DAE.Runtime.DataParam param in _params)
				param.Value = null;
		}

		#endregion

		private void ClearNodes()
		{
			foreach (TreeNode node in Nodes)
				node.Dispose();
			Nodes.Clear();
		}

		public void BuildTree()
		{
			ClearNodes();

			if (IsFieldActive() && (Source.DataView.State != DAE.Client.DataSetState.Insert)) 
			{
				// Open a dynamic navigable browse cursor on the root expression
				PrepareRootPlan();
				IServerCursor cursor = _rootPlan.Open(_rootParams);
				try
				{
					DAE.Runtime.Data.IRow key;
					int columnIndex;
					string text;
					while (cursor.Next())
					{
						key = new DAE.Runtime.Data.Row(_process.ValueManager, new DAE.Schema.RowType(Source.DataView.Order.Columns));
						try
						{
							using (DAE.Runtime.Data.IRow row = cursor.Select())
							{
								row.CopyTo(key);
								columnIndex = row.DataType.Columns.IndexOf(ColumnName);
								if (row.HasValue(columnIndex))
									text = ((DAE.Runtime.Data.Scalar)row.GetValue(columnIndex)).AsDisplayString;
								else
									text = Strings.Get("NoValue");
							}
							Nodes.Add(new TreeNode(this, text, key, 0, null));
						}
						catch
						{
							key.Dispose();
							throw;
						}
					}
				}
				finally
				{
					_rootPlan.Close(cursor);
				}
				
				foreach (TreeNode node in Nodes)
					node.BuildChildren();

				SelectNode(Source.DataView.GetKey());
			}
		}

		protected internal TreeNode FindChild(DAE.Runtime.Data.IRow childKey)
		{
			foreach (TreeNode node in Nodes)
			{
				if (node.IsRow(childKey))
					return node;
			}
			return null;
		}

		protected internal TreeNode Search(DAE.Runtime.Data.IRow childKey)
		{
			TreeNode result = FindChild(childKey);
			if (result == null)
				foreach (TreeNode node in Nodes)
				{
					result = node.Search(childKey);
					if (result != null)
						break;
				}
			return result;
		}

		private bool KeysEqual(DAE.Runtime.Data.IRow key1, DAE.Runtime.Data.IRow key2)
		{
			if (key2.DataType.Equals(key1.DataType))
			{
				string compareValue;
				string name2;
				for (int index = 0; index < key2.DataType.Columns.Count; index++)
				{
					name2 = key2.DataType.Columns[index].Name;
					if (key1.HasValue(name2))
						compareValue = ((DAE.Runtime.Data.Scalar)key1.GetValue(name2)).AsString;
					else
						compareValue = String.Empty;

					if (((DAE.Runtime.Data.Scalar)key2.GetValue(index)).AsString != compareValue)
						return false;
				}
				return true;
			}
			else
				return false;
		}

		protected void BuildParentPath(DAE.Runtime.Data.IRow key, ArrayList path)
		{
			foreach (DAE.Runtime.Data.Row localKey in path)
			{
				if (KeysEqual(key, localKey))
					throw new WebClientException(WebClientException.Codes.TreeViewInfiniteLoop);
			}
			path.Add(key);
			IServerCursor cursor = OpenParentCursor(key);
			try
			{
				if (cursor.Next())
				{
					key = new DAE.Runtime.Data.Row(_process.ValueManager, new RowType(Source.DataView.Order.Columns));
					using (DAE.Runtime.Data.IRow selected = cursor.Select())
						selected.CopyTo(key);
				}
				else
					key = null;
			}
			finally
			{
				CloseParentCursor(cursor);
			}
			
			if (key != null)
				if (FindChild(key) == null)
					BuildParentPath(key, path);
				else
					path.Add(key);
		}

		private bool _selecting;

		/// <summary> Sets the selected node of the tree. Builds children as needed.</summary>
		protected internal void SelectNode(DAE.Runtime.Data.IRow key)
		{
			TreeNode node = Search(key);
			if (node == null)
			{
				ArrayList path = new ArrayList();
				BuildParentPath((DAE.Runtime.Data.Row)key.Copy(), path);
				try
				{
					for (int index = path.Count - 1; index >= 0; index--)
					{
						if (index == path.Count - 1)
						{
							node = FindChild((DAE.Runtime.Data.Row)path[index]);
							if ((node == null) && !_selecting)
							{
								BuildTree();
								_selecting = true;
								try
								{
									SelectNode(key);
								}
								finally
								{
									_selecting = false;
								}
								return;
							}
						}
						else
							node = node.FindChild((DAE.Runtime.Data.Row)path[index]);

						if (node == null)
							throw new WebClientException(WebClientException.Codes.TreeViewUnconnected);

						if (index != 0)
							node.BuildChildren();
					}
				}
				finally
				{
					for (int index = 0; index < path.Count; index++)
						((DAE.Runtime.Data.Row)path[index]).Dispose();
				}
			} 

			// make sure the node is visible
			if ((node.Parent != null) && (!node.Parent.IsExpanded))
				node.Parent.Expand();

			_selectedNode = node;
		}

		// Element

		protected override void InternalRender(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "tree");
			writer.RenderBeginTag(HtmlTextWriterTag.Div);

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "tree");
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "1");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			string hint = GetHint();
			if (hint != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Title, hint, true);
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			if (Nodes.Count == 0)
			{
				// Ensure that the browser doesn't eliminate the table
				writer.RenderBeginTag(HtmlTextWriterTag.Tr);
				writer.RenderBeginTag(HtmlTextWriterTag.Td);
				Session.RenderDummyImage(writer, "1", "20");
				writer.RenderEndTag();
				writer.RenderEndTag();
			}
			else
			{
				HttpContext context = Session.Get(this).Context;
				foreach (TreeNode child in Nodes)
					child.Render(context, writer);
			}

			writer.RenderEndTag();	// TABLE
			writer.RenderEndTag();	// DIV
		}

		// IWebHandler

		public override bool ProcessRequest(HttpContext context)
		{
			if (base.ProcessRequest(context))
				return true;
			else
			{
				foreach (TreeNode node in _nodes)
					if (node.ProcessRequest(context))
						return true;
				return false;
			}
		}
	}

	public class TreeNodes : List {}

	public class TreeNode : Disposable
	{
		public TreeNode(Tree tree, string text, DAE.Runtime.Data.IRow key, int depth, TreeNode parent)
		{
			_iD = Session.GenerateID();
			_buttonID = Session.GenerateID();
			_tree = tree;
			_text = text;
			_key = key;
			_depth = depth;
			_parent = parent;
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					foreach (TreeNode child in Nodes)
						child.Dispose();
				}
				finally
				{
					Nodes.Clear();
				}
				base.Dispose(disposing);
			}
			finally
			{
				if (_key != null)
					_key.Dispose();
			}
		}

		// Parent

		private TreeNode _parent;
		public TreeNode Parent { get { return _parent; } }

		// ID

		private string _iD;
		public string ID { get { return _iD; } }

		// ButtonID

		private string _buttonID;
		public string ButtonID { get { return _buttonID; } }

		// Depth

		private int _depth;
		public int Depth { get { return _depth; } }

		// Tree

		private Tree _tree;
		public Tree Tree { get { return _tree; } }

		// Text

		private string _text;
		public string Text { get { return _text; } }

		// Key

		/// <summary> This Key is used by FindKey when it is selected. Not Parent/Child relationship. </summary>
		private DAE.Runtime.Data.IRow _key;
		public DAE.Runtime.Data.IRow Key { get { return _key; } }

		// Nodes

		private TreeNodes _nodes = new TreeNodes();
		public TreeNodes Nodes { get { return _nodes; } }

		/// <summary> Returns a child node matching the key (non reflectively or recursively). </summary>
		public TreeNode FindChild(DAE.Runtime.Data.IRow key)
		{
			foreach (TreeNode node in Nodes)
			{
				if (node.IsRow(key))
					return node;
			}
			return null;
		}

		/// <summary> Returns this node or any child node matching the key (recursively). </summary>
		public TreeNode Search(DAE.Runtime.Data.IRow key)
		{
			if (IsRow(key))
				return this;
			TreeNode result = FindChild(key);
			if (result != null)
				return result;
			foreach (TreeNode node in Nodes)
			{
				result = node.Search(key);
				if (result != null)
					return result;
			}
			return null;
		}

		// IsExpanded

		private bool _isExpanded = false;
		public bool IsExpanded
		{
			get { return _isExpanded && ((Parent == null) || Parent.IsExpanded); }
			set
			{
				if (value != _isExpanded)
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
			if (!_isExpanded)
			{
				if ((Parent != null) && (!Parent.IsExpanded))
					Parent.Expand();
				if (!_isExpanded)
				{
					foreach (TreeNode treeNode in Nodes)
						treeNode.BuildChildren();
					_isExpanded = true;
				}
			}
		}

		public void Collapse()
		{
			if (_isExpanded)
			{
				if (Search(Tree.SelectedNode._key) != null)
					Tree.InternalSelect(this);
				_isExpanded = false;
			}
		}

		/// <summary> Creates this nodes immediate children. Avoids duplication. </summary>
		public void BuildChildren()
		{
			// Open a dynamic navigable browse cursor on the child expression
			IServerCursor cursor = Tree.OpenChildCursor(_key);
			try
			{
				DAE.Runtime.Data.Row key;
				string text;
				int index = 0;
				int columnIndex;
				while (cursor.Next())
				{
					key = new DAE.Runtime.Data.Row(Tree.Process.ValueManager, new RowType(Tree.Source.DataView.Order.Columns));
					try
					{
						using (DAE.Runtime.Data.IRow row = cursor.Select())
						{
							row.CopyTo(key);
							columnIndex = row.DataType.Columns.IndexOf(Tree.ColumnName);
							if (row.HasValue(columnIndex))
								text = ((DAE.Runtime.Data.Scalar)row.GetValue(columnIndex)).AsDisplayString;
							else
								text = Strings.Get("NoValue");
						}
								
						if (FindChild(key) == null)
							Nodes.Insert(index, new TreeNode(Tree, text, key, _depth + 1, this));
						index++;
					}
					catch
					{
						key.Dispose();
						throw;
					}
				}
			}
			finally
			{
		  		Tree.CloseChildCursor(cursor);
			}
		}

		/// <summary> True if this node is the row(has the key). </summary>
		public bool IsRow(DAE.Runtime.Data.IRow key)
		{
			if (_key.DataType.Equals(key.DataType))
			{
				string compareValue;
				string name;
				for (int index = 0; index < _key.DataType.Columns.Count; index++)
				{
					name = _key.DataType.Columns[index].Name;
					if (key.HasValue(name))
						compareValue = ((DAE.Runtime.Data.Scalar)key.GetValue(name)).AsString;
					else
						compareValue = String.Empty;
					if (((DAE.Runtime.Data.Scalar)_key.GetValue(index)).AsString != compareValue)
						return false;
				}
				return true;
			}
			return false;
		}

		public void Render(HttpContext context, HtmlTextWriter writer)
		{
			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "treenode");
			writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);

			Session.RenderDummyImage(writer, (_depth * Tree.Indent).ToString(), "1");
			
			if (Nodes.Count > 0)
			{
				if (_isExpanded)
					writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/collapse.png");
				else
					writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/expand.png");
				writer.AddAttribute(HtmlTextWriterAttribute.Width, "10");
				writer.AddAttribute(HtmlTextWriterAttribute.Height, "10");
				writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(context, _buttonID));
				writer.RenderBeginTag(HtmlTextWriterTag.Img);
				writer.RenderEndTag();
				Session.RenderDummyImage(writer, "4", "1");
			}
			else
				Session.RenderDummyImage(writer, "14", "1");

			if (_tree.SelectedNode == this)
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "treenodeselected");
			else
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "treenode");
			writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(context, _iD));
			writer.RenderBeginTag(HtmlTextWriterTag.Font);
			writer.Write(HttpUtility.HtmlEncode(Text.Trim()));
			writer.RenderEndTag();

			writer.RenderEndTag();	// TD
			writer.RenderEndTag();	// TR

			if (_isExpanded)
				foreach (TreeNode child in Nodes)
					child.Render(context, writer);
		}

		public bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, _buttonID))
				IsExpanded = !IsExpanded;
			else if (Session.IsActionLink(context, _iD))
				_tree.InternalSelect(this);
			else
			{
				foreach (TreeNode child in Nodes)
					if (child.ProcessRequest(context))
						return true;
				return false;
			}
			return true;
		}
	}

}