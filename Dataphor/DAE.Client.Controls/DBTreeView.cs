/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Client.Controls
{
	using System;
	using System.Collections;
	using System.Drawing;
	using System.Windows.Forms;
	using System.ComponentModel;

	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Client;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBTreeView),"Icons.DBTreeView.bmp")]
	public class DBTreeView : TreeView, IDataSourceReference, IReadOnly, IColumnNameReference
	{
		public const string NoValueText = "<no value>";
		
		/// <summary> Initializes a new instance of a DBTreeView. </summary>
		public DBTreeView() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			CausesValidation = false;
			_parser = new Parser();
			HideSelection = false;
			_link = new FieldDataLink();
			_link.OnDataChanged += new DataLinkHandler(DataChanged);
			_link.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			_link.OnRowChanged += new DataLinkFieldHandler(RowChanged);
			_link.OnUpdateReadOnly += new System.EventHandler(UpdateReadOnly);
			_link.OnStateChanged += new DataLinkHandler(StateChanged);
			_link.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			UpdateReadOnly(this, EventArgs.Empty);
			_autoRefresh = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				try
				{
					StopProcess();
				}
				finally
				{
					_link.OnDataChanged -= new DataLinkHandler(DataChanged);
					_link.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
					_link.OnRowChanged -= new DataLinkFieldHandler(RowChanged);
					_link.OnUpdateReadOnly -= new System.EventHandler(UpdateReadOnly);
					_link.OnStateChanged -= new DataLinkHandler(StateChanged);
					_link.OnFocusControl -= new DataLinkFieldHandler(FocusControl);
					_link.Dispose();
					_link = null;
					_parser = null;
				}
			}
			base.Dispose(disposing);
		}
		
		internal IServerProcess _process;
		private bool _aTJoined;
		private Parser _parser;
		
		private void StartProcess()
		{
			DataSession session = ((DAEDataSet)_link.DataSet).Session;
			_process = session.ServerSession.StartProcess(new ProcessInfo(session.ServerSession.SessionInfo));
			DataView aTServer = _link.DataSet is DataView ? ((DataView)_link.DataSet).ApplicationTransactionServer : null;
			if (aTServer != null)
			{
				_process.JoinApplicationTransaction(aTServer.ApplicationTransactionID, false);
				_aTJoined = true;
			}
			else
				_aTJoined = false;
		}
		
		private void StopProcess()
		{
			if (_process != null)
			{
				UnprepareParentPlan();
				UnprepareChildPlan();
				UnprepareRootPlan();
				UnprepareParams();
				ClearTree();
				_process.Session.StopProcess(_process);
				_process = null;
			}
		}
		
		private void ClearTree()
		{
			BeginUpdate();
			try
			{
				SelectedNode = null;
				ClearNodes();
			}
			finally
			{
				EndUpdate();
			}
		}
		
		private FieldDataLink _link;
		protected internal FieldDataLink Link { get { return _link; } }

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return _link.Source; }
			set
			{
				if (_link.Source != value)
				{
					_link.Source = value;
					UpdateTree();
				}
			}
		}
		
		private string _rootExpression;
		[DefaultValue("")]
		[Category("Data")]
		[Description("The expression defining the root set of nodes to display. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.  The master key and other parameters of the associated DataView are available as variables such as MasterDataViewXXX (where XXX is the name of the master column with '.'s changed to '_'s). ")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string RootExpression
		{
			get { return _rootExpression; }
			set 
			{ 
				if (_rootExpression != value)
				{
					_rootExpression = (value == null) ? String.Empty : value; 
					UpdateTree();
				}
			}
		}

		private string _childExpression;
		[DefaultValue("")]
		[Category("Data")]
		[Description("The expression defining the set of child nodes for a given parent node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ChildExpression
		{
			get { return _childExpression; }
			set 
			{ 
				if (_childExpression != value)
				{
					_childExpression = (value == null) ? String.Empty : value; 
					UpdateTree();
				}
			}
		}
		
		private string _parentExpression;
		[DefaultValue("")]
		[Category("Data")]
		[Description("The expression defining the parent node for a given child node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName. If this result returns more than one row, only the first row will be used.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ParentExpression
		{
			get { return _parentExpression; }
			set 
			{ 
				if (_parentExpression != value)
				{
					_parentExpression = (value == null) ? String.Empty : value; 
					UpdateTree();
				}
			}
		}
		
		[DefaultValue("")]
		[Category("Data")]
		[Description("The column to display in each node of the TreeView.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Design.ColumnNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ColumnName
		{
			get { return _link.ColumnName; }
			set
			{ 
				if (_link.ColumnName != value)
				{
					_link.ColumnName = value;
					UpdateTree();
				}
			}
		}

		[Category("Data")]
		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return _link.ReadOnly; }
			set { _link.ReadOnly = value; }
		}

		[DefaultValue(false)]
		[Category("Behavior")]
		public new bool Enabled
		{
			get { return base.Enabled; }
			set { base.Enabled = value; }
		}

		[Browsable(false)]
		[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public new TreeNodeCollection Nodes	{ get { return base.Nodes; } }

		private bool _autoRefresh;
		/// <summary> If true the TreeView is rebuilt on DataChanged. </summary>
		[Category("Data")]
		[DefaultValue(true)]
		public bool AutoRefresh
		{
			get { return _autoRefresh; }
			set	{ _autoRefresh = value; }
		}

		protected void UpdateReadOnly(object sender, EventArgs args)
		{
			if (!DesignMode)
				base.Enabled = _link.Active && !_link.ReadOnly;
		}

		protected virtual void FieldChanged(DataLink dataLink, DataSet dataSet, DataField field)
		{
			if (dataLink.Active && !_link.Modified && !dataSet.IsEmpty() && IsSetup())
				using (IRow key = dataSet.GetKey())
				{
					SelectNode(key);
				}
		}

		/// <summary>
		/// Used to check if the proper settings are set.  Should be called before the data is accessed.
		/// </summary>
		private bool IsSetup() 
		{
			return 
				(ColumnName != String.Empty) && 
				(_rootExpression != String.Empty) && 
				(_childExpression != String.Empty) &&
				(_parentExpression != String.Empty);
		}

		protected virtual void DataChanged(DataLink dataLink, DataSet dataSet)
		{
			if (_link.Active)
			{
				if ((dataSet is DataView) && (!_aTJoined ^ (((DataView)dataSet).ApplicationTransactionServer == null)))
				{
					StopProcess();
					StartProcess();
				}

				if (!_link.Modified && _autoRefresh)
					UpdateTree();
			}
		}

		protected virtual void RowChanged(DataLink dataLink, DataSet dataSet, DataField field)
		{
			if (((field == null) || (field == _link.DataField)) && !_link.Modified && IsSetup())
			{
				if ((field != null) && (field.ColumnName == ColumnName))
				{
					using (IRow key = dataSet.GetKey())
					{
						DBTreeNode node = FindNode(key);
						if (node != null)
							node.Update(dataSet);
						else
							DataChanged(dataLink, dataSet);
					}
				}
				else
					DataChanged(dataLink, dataSet);
			}
		}
		
		protected virtual void StateChanged(DataLink link, DataSet dataSet)
		{
			if (_link.Active)
			{
				if (_process == null)
					StartProcess();
				else
				{
					if ((dataSet is DataView) && (!_aTJoined ^ (((DataView)dataSet).ApplicationTransactionServer == null)))
					{
						StopProcess();
						StartProcess();
					}
				}
			}
			else
				StopProcess();

			if (!_autoRefresh)
				UpdateTree();
		}

		/// <summary> Returns a root DBTreeNode given its key. </summary>
		protected DBTreeNode FindChild(IRow key)
		{
			foreach (DBTreeNode node in Nodes)
				if (node.KeyEquals(key))
					return node;
			return null;
		}

		/// <summary> Returns a tree node given a key, recursive. </summary>
		protected internal DBTreeNode FindNode(IRow key)
		{
			DBTreeNode result = null;
			foreach (DBTreeNode node in Nodes)
			{
				result = node.FindNode(key);
				if (result != null)
					break;
			}
			return result;
		}

		private bool _updatingTree;
		protected bool UpdatingTree { get { return _updatingTree; } }

		/// <summary> Rebuilds the entire tree. Property UpdatingTree is true during this operation. </summary>
		public virtual void UpdateTree()
		{
			if (!_findingKey)
			{
				BeginUpdate();
				try
				{
					_updatingTree = true;
					try
					{
						if (!_link.Active || _link.DataSet.State != DataSetState.Insert)
							ClearNodes();
						BuildTree();
					}
					finally
					{
						_updatingTree = false;
					}
				}
				finally
				{
					EndUpdate();
				}
			}
		}
		
		protected string GetExpression(string expression)
		{
			CursorDefinition definition = (CursorDefinition)_parser.ParseCursorDefinition(expression);
			definition.Capabilities = CursorCapability.Navigable;
			definition.CursorType = DAE.CursorType.Dynamic;
			definition.Isolation = CursorIsolation.Browse;
			return new D4TextEmitter().Emit(definition);
		}
		
		protected IServerExpressionPlan _rootPlan;
		protected Runtime.DataParams _rootParams;
		protected IServerExpressionPlan _childPlan;
		protected IServerExpressionPlan _parentPlan;
		
		protected IServerExpressionPlan PrepareRootPlan()
		{
			if (_rootPlan == null)
			{
				_rootParams = new Runtime.DataParams();
				((DAEDataSet)_link.DataSet).GetAllParams(_rootParams);
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
		
		protected internal IServerCursor OpenChildCursor(Row key)
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
		
		protected  internal IServerCursor OpenParentCursor(IRow key)
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

		Runtime.DataParams _params;		
		protected void PrepareParams()
		{
			if (_params == null)
			{
				_params = new Runtime.DataParams();
				foreach (Schema.OrderColumn orderColumn in ((TableDataSet)_link.DataSet).Order.Columns)
					_params.Add(new Runtime.DataParam("ACurrent" + orderColumn.Column.Name, orderColumn.Column.DataType, Modifier.Const));
			}
		}
		
		protected void EnsureParamsValid()
		{
			if (_params != null)
			{
				for (int index = 0; index < ((TableDataSet)_link.DataSet).Order.Columns.Count; index++)
					if ((_params.Count >= index) || (_params[index].Name != "ACurrent" + ((TableDataSet)_link.DataSet).Order.Columns[index].Column.Name))
					{
						UnprepareParams();
						break;
					}
			}
		}
		
		protected void UnprepareParams()
		{
			if (_params != null)
			{
				ClearParams();
				_params = null;
			}
		}
		
		protected void SetParams(IRow key)
		{
			for (int index = 0; index < ((TableDataSet)_link.DataSet).Order.Columns.Count; index++)
				if (key.HasValue(index))
					_params[index].Value = key[index];
		}
		
		protected void ClearParams()
		{
			foreach (Runtime.DataParam param in _params)
				param.Value = null;
		}
		
		private bool _clearingNodes;

		protected void ClearNodes()
		{
			foreach (DBTreeNode node in Nodes)
			{
				node.ClearChildren();
				node.DisposeKey();
			}
			// this is here because OnBeforeExpand is being called for Nodes.Clear()
			_clearingNodes = true;
			try
			{
				Nodes.Clear();
			}
			finally
			{
				_clearingNodes = false;
			}
		}
		
		/// <summary> Creates root nodes and their immediate children. </summary>
		protected void BuildTree()
		{
			if ((!_link.Active) || _link.DataSet.IsEmpty() || !IsSetup())
				ClearTree();
			else
			{
				if (_link.DataSet.State != DataSetState.Insert)
				{
					BeginUpdate();
					try
					{
						EnsureParamsValid();

						// Open a dynamic navigable browse cursor on the root expression
						PrepareRootPlan();
						IServerCursor cursor = _rootPlan.Open(_rootParams);
						try
						{
							Row key;
							int columnIndex;
							string text;
							while (cursor.Next())
							{
								key = new Row(_process.ValueManager, new Schema.RowType(((TableDataSet)_link.DataSet).Order.Columns));
								try
								{
									using (IRow row = cursor.Select())
									{
										row.CopyTo(key);
										columnIndex = row.DataType.Columns.IndexOf(ColumnName);
										if (row.HasValue(columnIndex))
											text = ((IScalar)row.GetValue(columnIndex)).AsDisplayString;
										else
											text = NoValueText;
									}
									Nodes.Add(new DBTreeNode(text, key));
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
						
						foreach (DBTreeNode node in Nodes)
							node.BuildChildren();
					}
					finally
					{
						EndUpdate();
					}
				}
			}
		}

		private bool _dataSettingSelected;
		/// <summary> True when the DataView causes the treeview to change the selected node.</summary>
		protected bool DataSettingSelected { get { return _dataSettingSelected; } }

		private bool CompareKeys(IRow key1, IRow key2)
		{
			if (key2.DataType.Equals(key1.DataType))
			{
				string compareValue;
				for (int index = 0; index < key2.DataType.Columns.Count; index++)
				{
					compareValue = String.Empty;
					if (key1.HasValue(key2.DataType.Columns[index].Name))
						compareValue = ((IScalar)key1.GetValue(key2.DataType.Columns[index].Name)).AsDisplayString;
					
					if (((IScalar)key2.GetValue(index)).AsDisplayString != compareValue)
						return false;
				}
				return true;
			}
			return false;
		}

		protected void BuildParentPath(IRow key, ArrayList path)
		{
			foreach (IRow localKey in path)
			{
				if (CompareKeys(key, localKey))
					throw new ControlsException(ControlsException.Codes.TreeViewInfiniteLoop);
			}
			path.Add(key);
			IServerCursor cursor = OpenParentCursor(key);
			try
			{
				if (cursor.Next())
				{
					key = new Row(_process.ValueManager, new RowType(((TableDataSet)Source.DataSet).Order.Columns));
					cursor.Select().CopyTo(key);
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

		protected void SelectNode(IRow key)
		{
			if (_findingKey || (_link.DataSet.State == DataSetState.Insert) || ((SelectedNode != null) && ((DBTreeNode)SelectedNode).KeyEquals(key)))
				return;

			_dataSettingSelected = true;
			try
			{
				// Given a key value, build only that portion of the tree required to discover the location of the node
				DBTreeNode node = FindNode(key);
				if (node != null)
					SelectedNode = node;
				else
				{
					ArrayList path = new ArrayList();
					BuildParentPath((IRow)key.Copy(), path);
					try
					{
						BeginUpdate();
						try
						{
							for (int index = path.Count - 1; index >= 0; index--)
							{
								if (index == path.Count - 1)
								{
									node = FindChild((IRow)path[index]);
									if ((node == null) && !_selecting)
									{
										UpdateTree();
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
									node = node.FindChild((IRow)path[index]);

								if (node == null)
									throw new ControlsException(ControlsException.Codes.TreeViewUnconnected);

								if (index == 0)
									SelectedNode = node;
								else							
									node.BuildChildren();
							}
						}
						finally
						{
							EndUpdate();
						}
					}
					finally
					{
						for (int index = 0; index < path.Count; index++)
							((IRow)path[index]).Dispose();
					}
				}
			}
			finally
			{
				_dataSettingSelected = false;
			}
		}
		
		protected override void OnBeforeExpand(TreeViewCancelEventArgs args)
		{
			base.OnBeforeExpand(args);
			// HACK: The check for FKey != null was added because this method is being called as a result of Nodes.Clear.
			if (!args.Cancel && (args.Node != null) && !_clearingNodes)
			{
				foreach (DBTreeNode node in args.Node.Nodes)
					node.BuildChildren();
			}
		}

		protected override void OnBeforeSelect(TreeViewCancelEventArgs args)
		{
			base.OnBeforeSelect(args);
			if (!args.Cancel && (args.Node != null) && !_updatingTree && !_dataSettingSelected && _link.Active)
				try
				{
					args.Cancel = !FindRow((DBTreeNode)args.Node);
				}
				catch
				{
					args.Cancel = true;
					throw;
				}
		}

		private bool _findingKey;
		/// <summary> True when the TreeView is updating the active row in the DataView. </summary>
		public bool FindingKey { get { return _findingKey; } }

		/// <summary> Positions the DataView on the row given a node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected bool FindRow(DBTreeNode node)
		{
			_findingKey = true;
			try
			{
				return _link.DataSet.FindKey(node.Key);
			}
			finally
			{
				_findingKey = false;
			}
		}

		private void FocusControl(DataLink link, DataSet dataSet, DataField field)
		{
			Focus();
		}
	}

	/// <summary> Not all nodes are added to the tree. Nodes are added by the DBTreeView as they are needed. </summary>
	public class DBTreeNode : TreeNode
	{
		public DBTreeNode(string text, Row key)
		{
			Text = text.Trim();
			_key = key;
		}

		private Row _key;
		/// <summary> The Key which this node represents. </summary>
		public Row Key { get { return _key; } }

		public DBTreeView DBTreeView { get { return (DBTreeView)TreeView; } }

		public int Depth { get { return Parent != null ? ((DBTreeNode)Parent).Depth + 1 : 0; } }

		/// <summary> True if the key for this node is the same as the given key. </summary>
		public bool KeyEquals(IRow key)
		{
			if (_key.DataType.Equals(key.DataType))
			{
				string compareValue;
				for (int index = 0; index < _key.DataType.Columns.Count; index++)
				{
					compareValue = String.Empty;
					if (key.HasValue(_key.DataType.Columns[index].Name))
						compareValue = ((IScalar)key.GetValue(_key.DataType.Columns[index].Name)).AsDisplayString;
					
					if (((IScalar)_key.GetValue(index)).AsDisplayString != compareValue)
						return false;
				}
				return true;
			}
			return false;
		}

		/// <summary> Returns the immediate child that matches the key. </summary>
		public DBTreeNode FindChild(IRow key)
		{
			foreach (DBTreeNode node in Nodes)
				if (node.KeyEquals(key))
					return node;
			return null;
		}
		
		/// <summary> Returns this node or any child, recursively, matching the key. </summary>
		public DBTreeNode FindNode(IRow key)
		{
			DBTreeNode result = null;
			if (KeyEquals(key))
				result = this;
			else
				foreach (DBTreeNode node in Nodes)
				{
					result = node.FindNode(key);
					if (result != null)
						break;
				}
			return result;
		}

		/// <summary> Updates data values for this node from the current row of the DataView. </summary>
		/// <param name="AView"></param>
		protected internal virtual void Update(DataSet dataSet)
		{
			DataField field = dataSet.Fields[DBTreeView.ColumnName];
			Text = field.HasValue() ? field.AsDisplayString : DBTreeView.NoValueText;
			Text = Text.Trim();
			using (IRow key = DBTreeView.Source.DataSet.GetKey())
			{
				_key.ClearValues();
				key.CopyTo(_key);
			}
		}

		public void ClearChildren()
		{
			foreach (DBTreeNode node in Nodes)
			{
				node.ClearChildren();
				node.DisposeKey();
			}
			Nodes.Clear();
		}
		
		public void SetKey(Row key)
		{
			DisposeKey();
			_key = key;
		}
		
		public void DisposeKey()
		{
			if (_key != null)
			{
				_key.Dispose();
				_key = null;
			}
		}

		/// <summary> Creates this nodes immediate children. Avoids duplication. </summary>
		public void BuildChildren()
		{
			// Open a dynamic navigable browse cursor on the child expression
			IServerCursor cursor = DBTreeView.OpenChildCursor(_key);
			try
			{
				DBTreeNode existingNode;
				Row key;
				string text;
				int index = 0;
				int columnIndex;
				while (cursor.Next())
				{
					key = new Row(DBTreeView._process.ValueManager, new Schema.RowType(((TableDataSet)DBTreeView.Source.DataSet).Order.Columns));
					try
					{
						using (IRow row = cursor.Select())
						{
							row.CopyTo(key);
							columnIndex = row.DataType.Columns.IndexOf(DBTreeView.ColumnName);
							if (columnIndex < 0)
								throw new ControlsException(ControlsException.Codes.DataColumnNotFound, DBTreeView.ColumnName);
							if (row.HasValue(columnIndex))
								text = ((IScalar)row.GetValue(columnIndex)).AsDisplayString;
							else
								text = DBTreeView.NoValueText;
								
							existingNode = FindChild(key);
							if (existingNode != null)
							{
								existingNode.Text = text;
								existingNode.SetKey(key);
								index = existingNode.Index;
							}
							else
								Nodes.Insert(index, new DBTreeNode(text, key));
							index++;
						}
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
				DBTreeView.CloseChildCursor(cursor);
			}
		}
	}
}
