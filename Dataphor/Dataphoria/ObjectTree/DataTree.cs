/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Dataphoria.ObjectTree.Nodes;

namespace Alphora.Dataphor.Dataphoria.ObjectTree
{
	/// <summary> TreeView descendant for Dataphoria. </summary>
	public class DataTree : BaseTree
	{
		public DataTree() {}

		private IDataphoria _dataphoria;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IDataphoria Dataphoria
		{
			get { return _dataphoria; }
			set { _dataphoria = value; }
		}

		public DataNode GetNodeAtPoint(Point point)
		{
			return (DataNode)GetNodeAt(point.X, point.Y);
		}

		protected override void OnMouseMove(MouseEventArgs args)
		{
			base.OnMouseMove(args);
			if ((args.Button != MouseButtons.None) && (SelectedNode != null))
			{
				TreeNode node = GetNodeAt(args.X, args.Y);
				if (node != null)
					((DataNode)SelectedNode).ItemDrag();
			}
		}

		protected override void OnDragDrop(DragEventArgs args)
		{
			try
			{
				base.OnDragDrop(args);
				DataNode node = GetNodeAtScreen(new Point(args.X, args.Y)) as DataNode;
				if (node != null)
					node.DragDrop(args);
			}
			catch (Exception exception)
			{
				// must show exceptions because the framework ignores them
				Frontend.Client.Windows.Session.HandleException(exception);
			}
		}

		private DataNode _dragTarget;

		private void InternalEnterOrOver(DragEventArgs args)
		{
			DataNode node = (DataNode)GetNodeAtScreen(new Point(args.X, args.Y));
			if (_dragTarget != node)
			{
				if (_dragTarget != null)
					_dragTarget.DragLeave();
				_dragTarget = node;
			}
			if (node != null)
				node.DragOver(args);
			else
				args.Effect = DragDropEffects.None;
		}

		protected override void OnDragOver(DragEventArgs args)
		{
			base.OnDragOver(args);
			InternalEnterOrOver(args);
		}

		protected override void OnDragEnter(DragEventArgs args)
		{
			base.OnDragEnter(args);
			InternalEnterOrOver(args);
		}

		protected override void OnDragLeave(EventArgs args)
		{
			base.OnDragLeave(args);
			if (_dragTarget != null)
			{
				_dragTarget.DragLeave();
				_dragTarget = null;
			}
		}
	}
}
