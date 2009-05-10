/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

using Alphora.Dataphor;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
	public class DesignerTree : BaseTree
	{
		public DesignerTree()
		{
			FDropFilter = new DropHighlightDrawFilter(this);
			LabelEdit = false;
		}

		private DropHighlightDrawFilter FDropFilter;
		[Browsable(false)]
		public DropHighlightDrawFilter DropFilter
		{
			get { return FDropFilter; }
		}

		private FormDesigner FFormDesigner;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FormDesigner FormDesigner
		{
			get { return FFormDesigner; }
			set { FFormDesigner = value; }
		}

		public void Modified()
		{
			FormDesigner.Service.SetModified(true);
		}

		public DesignerNode AddNode(INode ANode)
		{
			DesignerNode LNewNode = new DesignerNode(ANode, this);
			AddBaseNode(LNewNode);
			ExpandAll();
			return LNewNode;
		}

		public DesignerNode InsertNode(int AIndex, INode ANode)
		{
			DesignerNode LNewNode = new DesignerNode(ANode, this);
			InsertBaseNode(AIndex, LNewNode);
			ExpandAll();
			return LNewNode;
		}

		// SelectedNode

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new DesignerNode SelectedNode
		{
			get	{ return (DesignerNode)base.SelectedNode; }
			set { base.SelectedNode = value; }
		}

		// Labels

		protected override void OnAfterLabelEdit(NodeLabelEditEventArgs AArgs)
		{
			DesignerNode LNode = (DesignerNode)AArgs.Node;
			try
			{
				if (LNode.Node.Name != AArgs.Label)
				{
					IHost LHost = LNode.Node.HostNode;
					if (LHost.GetNode(AArgs.Label) != null)
						FormDesigner.Dataphoria.Warnings.AppendError(FormDesigner, new DataphoriaException(DataphoriaException.Codes.InvalidRename, AArgs.Label), false);
					else
					{
						if (AArgs.Label != null)
						{
							Modified();
							LNode.Node.Name = AArgs.Label;
						}
					}
				}
			}
			finally
			{
				LabelEdit = false;
				AArgs.CancelEdit = true;	// always cancel so that the edit doesn't overwrite our update
				LNode.UpdateText(false);
			}
		}

		// Palette

		private PaletteItem FPaletteItem;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PaletteItem PaletteItem
		{
			get { return FPaletteItem; }
			set
			{
				if (value != FPaletteItem)
				{
					if (FPaletteItem == null)
					{
						FDropFilter.QueryAllowedPositions = new QueryAllowedPositionsHandler(QueryAllowedPalettePositions);
						UpdateDropHighlight();
						Cursor = Cursors.Cross;
					}
					FPaletteItem = value;
					if (FPaletteItem == null)
					{
						Cursor = Cursors.Default;
						FDropFilter.ClearDropHighlight();
						FDropFilter.QueryAllowedPositions = null;
					}
				}
			}
		}

		public void UpdateDropHighlight()
		{
			FDropFilter.SetDropHighlight(PointToClient(Control.MousePosition), FPaletteItem, DropOperation.Copy);
		}

		protected override bool IsInputKey(Keys AKey)
		{
			return 
				(
					(SelectedNode != null) 
						&&
						(
							(
								((AKey == Keys.Delete) || (AKey == Keys.F2)) && !SelectedNode.ReadOnly
							)
								|| (AKey == (Keys.X | Keys.Control))
								|| (AKey == (Keys.C | Keys.Control))
								|| (AKey == (Keys.V | Keys.Control))
						)
				)
					|| base.IsInputKey(AKey);
		}

		protected override void OnKeyDown(KeyEventArgs AArgs)
		{
			if (SelectedNode != null)
			{
				switch (AArgs.KeyData)
				{
					case Keys.Delete : SelectedNode.Delete(); break;
					case Keys.X | Keys.Control : SelectedNode.CutToClipboard(); break;
					case Keys.C | Keys.Control : SelectedNode.CopyToClipboard(); break;
					case Keys.V | Keys.Control : SelectedNode.PasteFromClipboard(); break;
					case Keys.F2 : SelectedNode.Rename(); break;
					default : base.OnKeyDown(AArgs); return;
				}
				AArgs.Handled = true;
			}
			else
				base.OnKeyDown(AArgs);
		}

		protected override void OnMouseMove(MouseEventArgs AArgs)
		{
			base.OnMouseMove(AArgs);
			if (FPaletteItem != null)
				UpdateDropHighlight();
			else
			{
				TreeNode LNode = GetNodeAt(AArgs.X, AArgs.Y);
				if ((AArgs.Button != MouseButtons.None) && (SelectedNode != null) && (LNode != null))
					BeginDrag();
			}
		}

		protected override void OnMouseLeave(EventArgs AArgs)
		{
			base.OnMouseLeave(AArgs);
			if (FPaletteItem != null)
				FDropFilter.ClearDropHighlight();
		}

		protected override void WndProc(ref Message AMessage)
		{
			if (AMessage.Msg == NativeMethods.WM_LBUTTONDOWN)
			{
				int LParam = (int)AMessage.LParam;
				if ((FPaletteItem != null) && (this.GetNodeAt(LParam & 0xFFFF, LParam >> 16) != null))
				{
					if (FDropFilter.DropHighlightNode != null)
						((DesignerNode)FDropFilter.DropHighlightNode).AddNew(FPaletteItem, FDropFilter.DropLinePosition);
					FormDesigner.PaletteItemDropped();
					AMessage.Result = IntPtr.Zero;
					return;
				}
			}
			base.WndProc(ref AMessage);
		}


		private void QueryAllowedPalettePositions(object ASender, QueryAllowedPositionsEventArgs AArgs)
		{
			((DesignerNode)AArgs.TargetNode).QueryAllowedPalettePositions(AArgs);
		}

		// Drag & drop

		private void QueryAllowedDragPositions(object ASender, QueryAllowedPositionsEventArgs AArgs)
		{
			((DesignerNode)AArgs.TargetNode).QueryAllowedDragPositions(AArgs);
		}

		private void BeginDrag()
		{
			if (SelectedNode.Parent != null) // not the root node
			{
				FDropFilter.QueryAllowedPositions += new QueryAllowedPositionsHandler(QueryAllowedDragPositions);
				DoDragDrop(new DesignerNodeData(SelectedNode), DragDropEffects.Move | DragDropEffects.Copy);
			}
		}

		private void EndDrag()
		{
			FDropFilter.QueryAllowedPositions -= new QueryAllowedPositionsHandler(QueryAllowedDragPositions);
			FDropFilter.ClearDropHighlight();;
		}

		private void InternalEnterOrOver(DragEventArgs AArgs)
		{
			DropOperation LRequestedOperation;
			if (Control.MouseButtons == MouseButtons.Right)
				LRequestedOperation = DropOperation.Both;
			else
			{
				if (Control.ModifierKeys == Keys.Control)
					LRequestedOperation = DropOperation.Copy;
				else
					LRequestedOperation = DropOperation.Move;
			}

			FDropFilter.SetDropHighlight
			(
				PointToClient(new Point(AArgs.X, AArgs.Y)), 
				AArgs.Data,
				LRequestedOperation
			);

			AArgs.Effect = DragDropEffects.None;
			if ((FDropFilter.DropOperation & DropOperation.Copy) != 0)
				AArgs.Effect |= DragDropEffects.Copy;
			if ((FDropFilter.DropOperation & DropOperation.Move) != 0)
				AArgs.Effect |= DragDropEffects.Move;
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

		protected override void OnDragDrop(DragEventArgs AArgs)
		{
			if (AArgs.Effect != DragDropEffects.None)
			{
				try
				{
					base.OnDragDrop(AArgs);
					DesignerNodeData LData = AArgs.Data as DesignerNodeData;
					if 
					(
						(FDropFilter.DropHighlightNode != null) && 
						(FDropFilter.DropLinePosition != DropLinePosition.None) &&
						(LData != null)
					)
					{
						if (LData.RightButton)
							new DesignerTreeDropMenu(LData.TreeNode, (DesignerNode)FDropFilter.DropHighlightNode, FDropFilter.DropLinePosition, FDropFilter.DropOperation).Show(this, PointToClient(Control.MousePosition));
						else
						{
							if (Control.ModifierKeys == Keys.Control)
								((DesignerNode)FDropFilter.DropHighlightNode).CopyFromNode(LData.TreeNode, FDropFilter.DropLinePosition);
							else
								((DesignerNode)FDropFilter.DropHighlightNode).MoveFromNode(LData.TreeNode, FDropFilter.DropLinePosition);
						}
					}
					EndDrag();
				}
				catch (Exception LException)
				{
					// must handle exceptions because the framework ignores them
					FormDesigner.Dataphoria.Warnings.AppendError(FormDesigner, LException, false);
					// do not re-throw
				}
			}
		}

		protected override void OnQueryContinueDrag(QueryContinueDragEventArgs AArgs)
		{
			if (AArgs.EscapePressed)
			{
				AArgs.Action = DragAction.Cancel;
				EndDrag();
			}
		}
	}
}
