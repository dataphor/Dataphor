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
		public const string CNoValueText = "<no value>";
		
		/// <summary> Initializes a new instance of a DBTreeView. </summary>
		public DBTreeView() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			CausesValidation = false;
			FParser = new Parser();
			HideSelection = false;
			FLink = new FieldDataLink();
			FLink.OnDataChanged += new DataLinkHandler(DataChanged);
			FLink.OnFieldChanged += new DataLinkFieldHandler(FieldChanged);
			FLink.OnRowChanged += new DataLinkFieldHandler(RowChanged);
			FLink.OnUpdateReadOnly += new System.EventHandler(UpdateReadOnly);
			FLink.OnStateChanged += new DataLinkHandler(StateChanged);
			FLink.OnFocusControl += new DataLinkFieldHandler(FocusControl);
			UpdateReadOnly(this, EventArgs.Empty);
			FAutoRefresh = true;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				try
				{
					StopProcess();
				}
				finally
				{
					FLink.OnDataChanged -= new DataLinkHandler(DataChanged);
					FLink.OnFieldChanged -= new DataLinkFieldHandler(FieldChanged);
					FLink.OnRowChanged -= new DataLinkFieldHandler(RowChanged);
					FLink.OnUpdateReadOnly -= new System.EventHandler(UpdateReadOnly);
					FLink.OnStateChanged -= new DataLinkHandler(StateChanged);
					FLink.OnFocusControl -= new DataLinkFieldHandler(FocusControl);
					FLink.Dispose();
					FLink = null;
					FParser = null;
				}
			}
			base.Dispose(ADisposing);
		}
		
		internal IServerProcess FProcess;
		private bool FATJoined;
		private Parser FParser;
		
		private void StartProcess()
		{
			DataSessionBase LSession = ((DAEDataSet)FLink.DataSet).Session;
			FProcess = LSession.ServerSession.StartProcess(new ProcessInfo(LSession.ServerSession.SessionInfo));
			DataView LATServer = FLink.DataSet is DataView ? ((DataView)FLink.DataSet).ApplicationTransactionServer : null;
			if (LATServer != null)
			{
				FProcess.JoinApplicationTransaction(LATServer.ApplicationTransactionID, false);
				FATJoined = true;
			}
			else
				FATJoined = false;
		}
		
		private void StopProcess()
		{
			if (FProcess != null)
			{
				UnprepareParentPlan();
				UnprepareChildPlan();
				UnprepareRootPlan();
				UnprepareParams();
				ClearTree();
				FProcess.Session.StopProcess(FProcess);
				FProcess = null;
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
		
		private FieldDataLink FLink;
		protected internal FieldDataLink Link { get { return FLink; } }

		[Category("Data")]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.All)]
		[Description("The DataSource for this control")]
		public DataSource Source
		{
			get { return FLink.Source; }
			set
			{
				if (FLink.Source != value)
				{
					FLink.Source = value;
					UpdateTree();
				}
			}
		}
		
		private string FRootExpression;
		[DefaultValue("")]
		[Category("Data")]
		[Description("The expression defining the root set of nodes to display. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.  The master key and other parameters of the associated DataView are available as variables such as MasterDataViewXXX (where XXX is the name of the master column with '.'s changed to '_'s). ")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string RootExpression
		{
			get { return FRootExpression; }
			set 
			{ 
				if (FRootExpression != value)
				{
					FRootExpression = (value == null) ? String.Empty : value; 
					UpdateTree();
				}
			}
		}

		private string FChildExpression;
		[DefaultValue("")]
		[Category("Data")]
		[Description("The expression defining the set of child nodes for a given parent node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ChildExpression
		{
			get { return FChildExpression; }
			set 
			{ 
				if (FChildExpression != value)
				{
					FChildExpression = (value == null) ? String.Empty : value; 
					UpdateTree();
				}
			}
		}
		
		private string FParentExpression;
		[DefaultValue("")]
		[Category("Data")]
		[Description("The expression defining the parent node for a given child node. The values for the current key are available as variables named ACurrentXXX, where XXX is the name of the key column, within this expression. The columns in this result must include the order columns for the data source of the tree, and the ColumnName. If this result returns more than one row, only the first row will be used.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DAE.Client.Design.EditorDocumentType("d4")]
		public string ParentExpression
		{
			get { return FParentExpression; }
			set 
			{ 
				if (FParentExpression != value)
				{
					FParentExpression = (value == null) ? String.Empty : value; 
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
			get { return FLink.ColumnName; }
			set
			{ 
				if (FLink.ColumnName != value)
				{
					FLink.ColumnName = value;
					UpdateTree();
				}
			}
		}

		[Category("Data")]
		[DefaultValue(false)]
		public bool ReadOnly
		{
			get { return FLink.ReadOnly; }
			set { FLink.ReadOnly = value; }
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

		private bool FAutoRefresh;
		/// <summary> If true the TreeView is rebuilt on DataChanged. </summary>
		[Category("Data")]
		[DefaultValue(true)]
		public bool AutoRefresh
		{
			get { return FAutoRefresh; }
			set	{ FAutoRefresh = value; }
		}

		protected void UpdateReadOnly(object ASender, EventArgs AArgs)
		{
			if (!DesignMode)
				base.Enabled = FLink.Active && !FLink.ReadOnly;
		}

		protected virtual void FieldChanged(DataLink ADataLink, DataSet ADataSet, DataField AField)
		{
			if (ADataLink.Active && !FLink.Modified && !ADataSet.IsEmpty() && IsSetup())
				using (Row LKey = ADataSet.GetKey())
				{
					SelectNode(LKey);
				}
		}

		/// <summary>
		/// Used to check if the proper settings are set.  Should be called before the data is accessed.
		/// </summary>
		private bool IsSetup() 
		{
			return 
				(ColumnName != String.Empty) && 
				(FRootExpression != String.Empty) && 
				(FChildExpression != String.Empty) &&
				(FParentExpression != String.Empty);
		}

		protected virtual void DataChanged(DataLink ADataLink, DataSet ADataSet)
		{
			if (FLink.Active)
			{
				if ((ADataSet is DataView) && (!FATJoined ^ (((DataView)ADataSet).ApplicationTransactionServer == null)))
				{
					StopProcess();
					StartProcess();
				}

				if (!FLink.Modified && FAutoRefresh)
					UpdateTree();
			}
		}

		protected virtual void RowChanged(DataLink ADataLink, DataSet ADataSet, DataField AField)
		{
			if (((AField == null) || (AField == FLink.DataField)) && !FLink.Modified && IsSetup())
			{
				if ((AField != null) && (AField.ColumnName == ColumnName))
				{
					using (Row LKey = ADataSet.GetKey())
					{
						DBTreeNode LNode = FindNode(LKey);
						if (LNode != null)
							LNode.Update(ADataSet);
						else
							DataChanged(ADataLink, ADataSet);
					}
				}
				else
					DataChanged(ADataLink, ADataSet);
			}
		}
		
		protected virtual void StateChanged(DataLink ALink, DataSet ADataSet)
		{
			if (FLink.Active)
			{
				if (FProcess == null)
					StartProcess();
				else
				{
					if ((ADataSet is DataView) && (!FATJoined ^ (((DataView)ADataSet).ApplicationTransactionServer == null)))
					{
						StopProcess();
						StartProcess();
					}
				}
			}
			else
				StopProcess();

			if (!FAutoRefresh)
				UpdateTree();
		}

		/// <summary> Returns a root DBTreeNode given its key. </summary>
		protected DBTreeNode FindChild(Row AKey)
		{
			foreach (DBTreeNode LNode in Nodes)
				if (LNode.KeyEquals(AKey))
					return LNode;
			return null;
		}

		/// <summary> Returns a tree node given a key, recursive. </summary>
		protected internal DBTreeNode FindNode(Row AKey)
		{
			DBTreeNode LResult = null;
			foreach (DBTreeNode LNode in Nodes)
			{
				LResult = LNode.FindNode(AKey);
				if (LResult != null)
					break;
			}
			return LResult;
		}

		private bool FUpdatingTree;
		protected bool UpdatingTree { get { return FUpdatingTree; } }

		/// <summary> Rebuilds the entire tree. Property UpdatingTree is true during this operation. </summary>
		public virtual void UpdateTree()
		{
			if (!FFindingKey)
			{
				BeginUpdate();
				try
				{
					FUpdatingTree = true;
					try
					{
						if (!FLink.Active || FLink.DataSet.State != DataSetState.Insert)
							ClearNodes();
						BuildTree();
					}
					finally
					{
						FUpdatingTree = false;
					}
				}
				finally
				{
					EndUpdate();
				}
			}
		}
		
		protected string GetExpression(string AExpression)
		{
			CursorDefinition LDefinition = (CursorDefinition)FParser.ParseCursorDefinition(AExpression);
			LDefinition.Capabilities = CursorCapability.Navigable;
			LDefinition.CursorType = DAE.CursorType.Dynamic;
			LDefinition.Isolation = CursorIsolation.Browse;
			return new D4TextEmitter().Emit(LDefinition);
		}
		
		protected IServerExpressionPlan FRootPlan;
		protected Runtime.DataParams FRootParams;
		protected IServerExpressionPlan FChildPlan;
		protected IServerExpressionPlan FParentPlan;
		
		protected IServerExpressionPlan PrepareRootPlan()
		{
			if (FRootPlan == null)
			{
				FRootParams = new Runtime.DataParams();
				((DAEDataSet)FLink.DataSet).GetAllParams(FRootParams);
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
		
		protected internal IServerCursor OpenChildCursor(Row AKey)
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
		
		protected  internal IServerCursor OpenParentCursor(Row AKey)
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

		Runtime.DataParams FParams;		
		protected void PrepareParams()
		{
			if (FParams == null)
			{
				FParams = new Runtime.DataParams();
				foreach (Schema.OrderColumn LOrderColumn in ((TableDataSet)FLink.DataSet).Order.Columns)
					FParams.Add(new Runtime.DataParam("ACurrent" + LOrderColumn.Column.Name, LOrderColumn.Column.DataType, Modifier.Const));
			}
		}
		
		protected void EnsureParamsValid()
		{
			if (FParams != null)
			{
				for (int LIndex = 0; LIndex < ((TableDataSet)FLink.DataSet).Order.Columns.Count; LIndex++)
					if ((FParams.Count >= LIndex) || (FParams[LIndex].Name != "ACurrent" + ((TableDataSet)FLink.DataSet).Order.Columns[LIndex].Column.Name))
					{
						UnprepareParams();
						break;
					}
			}
		}
		
		protected void UnprepareParams()
		{
			if (FParams != null)
			{
				ClearParams();
				FParams = null;
			}
		}
		
		protected void SetParams(Row AKey)
		{
			for (int LIndex = 0; LIndex < ((TableDataSet)FLink.DataSet).Order.Columns.Count; LIndex++)
				if (AKey.HasValue(LIndex))
					FParams[LIndex].Value = AKey[LIndex];
		}
		
		protected void ClearParams()
		{
			foreach (Runtime.DataParam LParam in FParams)
				LParam.Value = null;
		}
		
		private bool FClearingNodes;

		protected void ClearNodes()
		{
			foreach (DBTreeNode LNode in Nodes)
			{
				LNode.ClearChildren();
				LNode.DisposeKey();
			}
			// this is here because OnBeforeExpand is being called for Nodes.Clear()
			FClearingNodes = true;
			try
			{
				Nodes.Clear();
			}
			finally
			{
				FClearingNodes = false;
			}
		}
		
		/// <summary> Creates root nodes and their immediate children. </summary>
		protected void BuildTree()
		{
			if ((!FLink.Active) || FLink.DataSet.IsEmpty() || !IsSetup())
				ClearTree();
			else
			{
				if (FLink.DataSet.State != DataSetState.Insert)
				{
					BeginUpdate();
					try
					{
						EnsureParamsValid();

						// Open a dynamic navigable browse cursor on the root expression
						PrepareRootPlan();
						IServerCursor LCursor = FRootPlan.Open(FRootParams);
						try
						{
							Row LKey;
							int LColumnIndex;
							string LText;
							while (LCursor.Next())
							{
								LKey = new Row(FProcess.ValueManager, new Schema.RowType(((TableDataSet)FLink.DataSet).Order.Columns));
								try
								{
									using (Row LRow = LCursor.Select())
									{
										LRow.CopyTo(LKey);
										LColumnIndex = LRow.DataType.Columns.IndexOf(ColumnName);
										if (LRow.HasValue(LColumnIndex))
											LText = ((Scalar)LRow.GetValue(LColumnIndex)).AsDisplayString;
										else
											LText = CNoValueText;
									}
									Nodes.Add(new DBTreeNode(LText, LKey));
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
						
						foreach (DBTreeNode LNode in Nodes)
							LNode.BuildChildren();
					}
					finally
					{
						EndUpdate();
					}
				}
			}
		}

		private bool FDataSettingSelected;
		/// <summary> True when the DataView causes the treeview to change the selected node.</summary>
		protected bool DataSettingSelected { get { return FDataSettingSelected; } }

		private bool CompareKeys(Row AKey1, Row AKey2)
		{
			if (AKey2.DataType.Equals(AKey1.DataType))
			{
				string LCompareValue;
				for (int LIndex = 0; LIndex < AKey2.DataType.Columns.Count; LIndex++)
				{
					LCompareValue = String.Empty;
					if (AKey1.HasValue(AKey2.DataType.Columns[LIndex].Name))
						LCompareValue = ((Scalar)AKey1.GetValue(AKey2.DataType.Columns[LIndex].Name)).AsDisplayString;
					
					if (((Scalar)AKey2.GetValue(LIndex)).AsDisplayString != LCompareValue)
						return false;
				}
				return true;
			}
			return false;
		}

		protected void BuildParentPath(Row AKey, ArrayList APath)
		{
			foreach (Row LKey in APath)
			{
				if (CompareKeys(AKey, LKey))
					throw new ControlsException(ControlsException.Codes.TreeViewInfiniteLoop);
			}
			APath.Add(AKey);
			IServerCursor LCursor = OpenParentCursor(AKey);
			try
			{
				if (LCursor.Next())
				{
					AKey = new Row(FProcess.ValueManager, new RowType(((TableDataSet)Source.DataSet).Order.Columns));
					LCursor.Select().CopyTo(AKey);
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

		protected void SelectNode(Row AKey)
		{
			if (FFindingKey || (FLink.DataSet.State == DataSetState.Insert) || ((SelectedNode != null) && ((DBTreeNode)SelectedNode).KeyEquals(AKey)))
				return;

			FDataSettingSelected = true;
			try
			{
				// Given a key value, build only that portion of the tree required to discover the location of the node
				DBTreeNode LNode = FindNode(AKey);
				if (LNode != null)
					SelectedNode = LNode;
				else
				{
					ArrayList LPath = new ArrayList();
					BuildParentPath((Row)AKey.Copy(), LPath);
					try
					{
						BeginUpdate();
						try
						{
							for (int LIndex = LPath.Count - 1; LIndex >= 0; LIndex--)
							{
								if (LIndex == LPath.Count - 1)
								{
									LNode = FindChild((Row)LPath[LIndex]);
									if ((LNode == null) && !FSelecting)
									{
										UpdateTree();
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
									LNode = LNode.FindChild((Row)LPath[LIndex]);

								if (LNode == null)
									throw new ControlsException(ControlsException.Codes.TreeViewUnconnected);

								if (LIndex == 0)
									SelectedNode = LNode;
								else							
									LNode.BuildChildren();
							}
						}
						finally
						{
							EndUpdate();
						}
					}
					finally
					{
						for (int LIndex = 0; LIndex < LPath.Count; LIndex++)
							((Row)LPath[LIndex]).Dispose();
					}
				}
			}
			finally
			{
				FDataSettingSelected = false;
			}
		}
		
		protected override void OnBeforeExpand(TreeViewCancelEventArgs AArgs)
		{
			base.OnBeforeExpand(AArgs);
			// HACK: The check for FKey != null was added because this method is being called as a result of Nodes.Clear.
			if (!AArgs.Cancel && (AArgs.Node != null) && !FClearingNodes)
			{
				foreach (DBTreeNode LNode in AArgs.Node.Nodes)
					LNode.BuildChildren();
			}
		}

		protected override void OnBeforeSelect(TreeViewCancelEventArgs AArgs)
		{
			base.OnBeforeSelect(AArgs);
			if (!AArgs.Cancel && (AArgs.Node != null) && !FUpdatingTree && !FDataSettingSelected && FLink.Active)
				try
				{
					AArgs.Cancel = !FindRow((DBTreeNode)AArgs.Node);
				}
				catch
				{
					AArgs.Cancel = true;
					throw;
				}
		}

		private bool FFindingKey;
		/// <summary> True when the TreeView is updating the active row in the DataView. </summary>
		public bool FindingKey { get { return FFindingKey; } }

		/// <summary> Positions the DataView on the row given a node.
		/// </summary>
		/// <param name="ANode"></param>
		/// <returns></returns>
		protected bool FindRow(DBTreeNode ANode)
		{
			FFindingKey = true;
			try
			{
				return FLink.DataSet.FindKey(ANode.Key);
			}
			finally
			{
				FFindingKey = false;
			}
		}

		private void FocusControl(DataLink ALink, DataSet ADataSet, DataField AField)
		{
			Focus();
		}
	}

	/// <summary> Not all nodes are added to the tree. Nodes are added by the DBTreeView as they are needed. </summary>
	public class DBTreeNode : TreeNode
	{
		public DBTreeNode(string AText, Row AKey)
		{
			Text = AText.Trim();
			FKey = AKey;
		}

		private Row FKey;
		/// <summary> The Key which this node represents. </summary>
		public Row Key { get { return FKey; } }

		public DBTreeView DBTreeView { get { return (DBTreeView)TreeView; } }

		public int Depth { get { return Parent != null ? ((DBTreeNode)Parent).Depth + 1 : 0; } }

		/// <summary> True if the key for this node is the same as the given key. </summary>
		public bool KeyEquals(Row AKey)
		{
			if (FKey.DataType.Equals(AKey.DataType))
			{
				string LCompareValue;
				for (int LIndex = 0; LIndex < FKey.DataType.Columns.Count; LIndex++)
				{
					LCompareValue = String.Empty;
					if (AKey.HasValue(FKey.DataType.Columns[LIndex].Name))
						LCompareValue = ((Scalar)AKey.GetValue(FKey.DataType.Columns[LIndex].Name)).AsDisplayString;
					
					if (((Scalar)FKey.GetValue(LIndex)).AsDisplayString != LCompareValue)
						return false;
				}
				return true;
			}
			return false;
		}

		/// <summary> Returns the immediate child that matches the key. </summary>
		public DBTreeNode FindChild(Row AKey)
		{
			foreach (DBTreeNode LNode in Nodes)
				if (LNode.KeyEquals(AKey))
					return LNode;
			return null;
		}
		
		/// <summary> Returns this node or any child, recursively, matching the key. </summary>
		public DBTreeNode FindNode(Row AKey)
		{
			DBTreeNode LResult = null;
			if (KeyEquals(AKey))
				LResult = this;
			else
				foreach (DBTreeNode LNode in Nodes)
				{
					LResult = LNode.FindNode(AKey);
					if (LResult != null)
						break;
				}
			return LResult;
		}

		/// <summary> Updates data values for this node from the current row of the DataView. </summary>
		/// <param name="AView"></param>
		protected internal virtual void Update(DataSet ADataSet)
		{
			DataField LField = ADataSet.Fields[DBTreeView.ColumnName];
			Text = LField.HasValue() ? LField.AsDisplayString : DBTreeView.CNoValueText;
			Text = Text.Trim();
			using (Row LKey = DBTreeView.Source.DataSet.GetKey())
			{
				FKey.ClearValues();
				LKey.CopyTo(FKey);
			}
		}

		public void ClearChildren()
		{
			foreach (DBTreeNode LNode in Nodes)
			{
				LNode.ClearChildren();
				LNode.DisposeKey();
			}
			Nodes.Clear();
		}
		
		public void SetKey(Row AKey)
		{
			DisposeKey();
			FKey = AKey;
		}
		
		public void DisposeKey()
		{
			if (FKey != null)
			{
				FKey.Dispose();
				FKey = null;
			}
		}

		/// <summary> Creates this nodes immediate children. Avoids duplication. </summary>
		public void BuildChildren()
		{
			// Open a dynamic navigable browse cursor on the child expression
			IServerCursor LCursor = DBTreeView.OpenChildCursor(FKey);
			try
			{
				DBTreeNode LExistingNode;
				Row LKey;
				string LText;
				int LIndex = 0;
				int LColumnIndex;
				while (LCursor.Next())
				{
					LKey = new Row(DBTreeView.FProcess.ValueManager, new Schema.RowType(((TableDataSet)DBTreeView.Source.DataSet).Order.Columns));
					try
					{
						using (Row LRow = LCursor.Select())
						{
							LRow.CopyTo(LKey);
							LColumnIndex = LRow.DataType.Columns.IndexOf(DBTreeView.ColumnName);
							if (LColumnIndex < 0)
								throw new ControlsException(ControlsException.Codes.DataColumnNotFound, DBTreeView.ColumnName);
							if (LRow.HasValue(LColumnIndex))
								LText = ((Scalar)LRow.GetValue(LColumnIndex)).AsDisplayString;
							else
								LText = DBTreeView.CNoValueText;
								
							LExistingNode = FindChild(LKey);
							if (LExistingNode != null)
							{
								LExistingNode.Text = LText;
								LExistingNode.SetKey(LKey);
								LIndex = LExistingNode.Index;
							}
							else
								Nodes.Insert(LIndex, new DBTreeNode(LText, LKey));
							LIndex++;
						}
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
				DBTreeView.CloseChildCursor(LCursor);
			}
		}
	}
}
