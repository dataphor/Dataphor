/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Windows.Forms;
using System.Drawing;

namespace Alphora.Dataphor.Dataphoria
{
	public class BaseTree : TreeView
	{
		public BaseTree()
		{
			AllowDrop = true;
			BorderStyle = BorderStyle.None;
			LabelEdit = false;
			ShowRootLines = false;
			CausesValidation = false;
			HideSelection = false;
		}

		protected override void OnAfterSelect(TreeViewEventArgs AArgs)
		{
			base.OnAfterSelect(AArgs);
			if (AArgs.Node != null)
				ContextMenu = ((BaseNode)AArgs.Node).ContextMenu;
			else
				ContextMenu = null;
		}

		protected override void OnMouseDown(MouseEventArgs AArgs)
		{
			base.OnMouseDown(AArgs);
			TreeNode LNode = GetNodeAt(AArgs.X, AArgs.Y);
			if (LNode != null)
				SelectedNode = LNode;
		}

		private bool ExecuteDefaultMenuItem()
		{
			if (ContextMenu != null)
			{
				foreach (MenuItem LItem in ContextMenu.MenuItems)
				{
					if (LItem.DefaultItem && LItem.Enabled)
					{
						LItem.PerformClick();
						return true;
					}
				}
			}
			return false;
		}

		public BaseNode GetNodeAtScreen(Point APoint)
		{
			APoint = PointToClient(APoint);
			return (BaseNode)GetNodeAt(APoint.X, APoint.Y);
		}

		protected override void OnDoubleClick(EventArgs AArgs)
		{
			TreeNode LNode = GetNodeAtScreen(Control.MousePosition);
			if (LNode != null)
				ExecuteDefaultMenuItem();
			else
				base.OnDoubleClick(AArgs);
		}

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if (AKey == Keys.Enter)
				return ExecuteDefaultMenuItem() || base.ProcessDialogKey(AKey);
			else
				return base.ProcessDialogKey(AKey);
		}

		public static void ProcessMode(BaseNode ANode)
		{
			switch (ANode.BuildMode)
			{
				case BuildMode.OnDisplay :
					ANode.Build();
					break;
				case BuildMode.OnExpand :
					if (!ANode.Built)
						ANode.Nodes.Add(new TreeNode());	// HACK: to show an expansion indicator, a child node must be present
					break;
			}
		}

		protected override void OnBeforeExpand(TreeViewCancelEventArgs AArgs)
		{
			BaseNode LNode = (BaseNode)AArgs.Node;
			LNode.Build();
			foreach (BaseNode LChildNode in LNode.Nodes)
				ProcessMode(LChildNode);
		}

		public void AddBaseNode(BaseNode ANode)
		{
			Nodes.Add(ANode);
			ProcessMode(ANode);
		}

		public void InsertBaseNode(int AIndex, BaseNode ANode)
		{
			Nodes.Insert(AIndex, ANode);
			ProcessMode(ANode);
		}
	}

	public class BaseNode : TreeNode
	{
		private ContextMenu FContextMenu;
		public override ContextMenu ContextMenu
		{
			get 
			{ 
				if (FContextMenu == null)
				{
					FContextMenu = GetContextMenu();
					if (FContextMenu != null)
					{
						FContextMenu.Popup += new EventHandler(ContextMenuPopup);
						UpdateContextMenu(FContextMenu);
					}
				}
				return FContextMenu; 
			}
		}

		protected virtual ContextMenu GetContextMenu()
		{
			return null;
		}

		private void ContextMenuPopup(object ASender, EventArgs AArgs)
		{
			UpdateContextMenu((ContextMenu)ASender);
		}

		protected virtual void UpdateContextMenu(ContextMenu AMenu)
		{
			// nothing
		}

		// Child node building

		protected virtual void InternalRefresh()
		{
			ReconcileChildren();
		}

		/// <summary> Refreshes the child nodes if the node is built. </summary>
		public void Refresh()
		{
			if (Built)
				InternalRefresh();
		}

		protected virtual void InternalReconcileChildren()
		{
			// abstract
		}

		public virtual BuildMode BuildMode
		{
			get { return BuildMode.OnDisplay; }
		}

		/// <summary> Builds or refreshes (unconditionally) the child nodes. </summary>
		public void ReconcileChildren()
		{
			if (!FBuilt)
				Build();
			else
				InternalReconcileChildren();
		}
		
		private bool FBuilt;
		/// <summary> Indicates that the node has built its children. </summary>
		public bool Built
		{
			get { return FBuilt; }
		}

		/// <summary> Builds the node's children if they are not already built. </summary>
		public void Build()
		{
			if (!FBuilt)
			{
				if (BuildMode == BuildMode.OnExpand)
					Nodes.Clear();
				InternalReconcileChildren();
				FBuilt = true;
			}
		}

		public void AddBaseNode(BaseNode ANode)
		{
			Nodes.Add(ANode);
			if (IsExpanded)
				BaseTree.ProcessMode(ANode);
		}

		public void InsertBaseNode(int AIndex, BaseNode ANode)
		{
			Nodes.Insert(AIndex, ANode);
			if (IsExpanded)
				BaseTree.ProcessMode(ANode);
		}
	}

	public enum BuildMode { Never, OnExpand, OnDisplay };
}
