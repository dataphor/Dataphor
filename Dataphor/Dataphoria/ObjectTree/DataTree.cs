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

		private IDataphoria FDataphoria;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IDataphoria Dataphoria
		{
			get { return FDataphoria; }
			set { FDataphoria = value; }
		}

		public DataNode GetNodeAtPoint(Point APoint)
		{
			return (DataNode)GetNodeAt(APoint.X, APoint.Y);
		}

		protected override void OnMouseMove(MouseEventArgs AArgs)
		{
			base.OnMouseMove(AArgs);
			if ((AArgs.Button != MouseButtons.None) && (SelectedNode != null))
			{
				TreeNode LNode = GetNodeAt(AArgs.X, AArgs.Y);
				if (LNode != null)
					((DataNode)SelectedNode).ItemDrag();
			}
		}

		protected override void OnDragDrop(DragEventArgs AArgs)
		{
			try
			{
				base.OnDragDrop(AArgs);
				DataNode LNode = GetNodeAtScreen(new Point(AArgs.X, AArgs.Y)) as DataNode;
				if (LNode != null)
					LNode.DragDrop(AArgs);
			}
			catch (Exception LException)
			{
				// must show exceptions because the framework ignores them
				Frontend.Client.Windows.Session.HandleException(LException);
			}
		}

		private DataNode FDragTarget;

		private void InternalEnterOrOver(DragEventArgs AArgs)
		{
			DataNode LNode = (DataNode)GetNodeAtScreen(new Point(AArgs.X, AArgs.Y));
			if (FDragTarget != LNode)
			{
				if (FDragTarget != null)
					FDragTarget.DragLeave();
				FDragTarget = LNode;
			}
			if (LNode != null)
				LNode.DragOver(AArgs);
			else
				AArgs.Effect = DragDropEffects.None;
		}

		protected override void OnDragOver(DragEventArgs AArgs)
		{
			base.OnDragOver(AArgs);
			InternalEnterOrOver(AArgs);
		}

		protected override void OnDragEnter(DragEventArgs AArgs)
		{
			base.OnDragEnter(AArgs);
			InternalEnterOrOver(AArgs);
		}

		protected override void OnDragLeave(EventArgs AArgs)
		{
			base.OnDragLeave(AArgs);
			if (FDragTarget != null)
			{
				FDragTarget.DragLeave();
				FDragTarget = null;
			}
		}
	}
}
