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

		protected override void OnAfterSelect(TreeViewEventArgs args)
		{
			base.OnAfterSelect(args);
			if (args.Node != null)
				ContextMenu = ((BaseNode)args.Node).ContextMenu;
			else
				ContextMenu = null;
		}

		protected override void OnMouseDown(MouseEventArgs args)
		{
			base.OnMouseDown(args);
			TreeNode node = GetNodeAt(args.X, args.Y);
			if (node != null)
				SelectedNode = node;
		}

		private bool ExecuteDefaultMenuItem()
		{
			if (ContextMenu != null)
			{
				foreach (MenuItem item in ContextMenu.MenuItems)
				{
					if (item.DefaultItem && item.Enabled)
					{
						item.PerformClick();
						return true;
					}
				}
			}
			return false;
		}

		public BaseNode GetNodeAtScreen(Point point)
		{
			point = PointToClient(point);
			return (BaseNode)GetNodeAt(point.X, point.Y);
		}

		protected override void OnDoubleClick(EventArgs args)
		{
			TreeNode node = GetNodeAtScreen(Control.MousePosition);
			if (node != null)
				ExecuteDefaultMenuItem();
			else
				base.OnDoubleClick(args);
		}

		protected override bool ProcessDialogKey(Keys key)
		{
			if (key == Keys.Enter)
				return ExecuteDefaultMenuItem() || base.ProcessDialogKey(key);
			else
				return base.ProcessDialogKey(key);
		}

		public static void ProcessMode(BaseNode node)
		{
			switch (node.BuildMode)
			{
				case BuildMode.OnDisplay :
					node.Build();
					break;
				case BuildMode.OnExpand :
					if (!node.Built)
						node.Nodes.Add(new TreeNode());	// HACK: to show an expansion indicator, a child node must be present
					break;
			}
		}

		protected override void OnBeforeExpand(TreeViewCancelEventArgs args)
		{
			BaseNode node = (BaseNode)args.Node;
			node.Build();
			foreach (BaseNode childNode in node.Nodes)
				ProcessMode(childNode);
		}

		public void AddBaseNode(BaseNode node)
		{
			Nodes.Add(node);
			ProcessMode(node);
		}

		public void InsertBaseNode(int index, BaseNode node)
		{
			Nodes.Insert(index, node);
			ProcessMode(node);
		}
	}

	public class BaseNode : TreeNode
	{
		private ContextMenu _contextMenu;
		public override ContextMenu ContextMenu
		{
			get 
			{ 
				if (_contextMenu == null)
				{
					_contextMenu = GetContextMenu();
					if (_contextMenu != null)
					{
						_contextMenu.Popup += new EventHandler(ContextMenuPopup);
						UpdateContextMenu(_contextMenu);
					}
				}
				return _contextMenu; 
			}
		}

		protected virtual ContextMenu GetContextMenu()
		{
			return null;
		}

		private void ContextMenuPopup(object sender, EventArgs args)
		{
			UpdateContextMenu((ContextMenu)sender);
		}

		protected virtual void UpdateContextMenu(ContextMenu menu)
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
			if (!_built)
				Build();
			else
				InternalReconcileChildren();
		}
		
		private bool _built;
		/// <summary> Indicates that the node has built its children. </summary>
		public bool Built
		{
			get { return _built; }
		}

		/// <summary> Builds the node's children if they are not already built. </summary>
		public void Build()
		{
			if (!_built)
			{
				if (BuildMode == BuildMode.OnExpand)
					Nodes.Clear();
				InternalReconcileChildren();
				_built = true;
			}
		}

		public void AddBaseNode(BaseNode node)
		{
			Nodes.Add(node);
			if (IsExpanded)
				BaseTree.ProcessMode(node);
		}

		public void InsertBaseNode(int index, BaseNode node)
		{
			Nodes.Insert(index, node);
			if (IsExpanded)
				BaseTree.ProcessMode(node);
		}
	}

	public enum BuildMode { Never, OnExpand, OnDisplay };
}
