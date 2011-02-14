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

namespace Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree
{
	public class DesignerTree : BaseTree
	{
		public DesignerTree()
		{
			_dropFilter = new DropHighlightDrawFilter(this);
			LabelEdit = false;
		}

		private DropHighlightDrawFilter _dropFilter;
		[Browsable(false)]
		public DropHighlightDrawFilter DropFilter
		{
			get { return _dropFilter; }
		}

		private FormDesigner _formDesigner;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FormDesigner FormDesigner
		{
			get { return _formDesigner; }
			set { _formDesigner = value; }
		}

		public void Modified()
		{
			FormDesigner.Service.SetModified(true);
		}

		public DesignerNode AddNode(INode node)
		{
			DesignerNode newNode = new DesignerNode(node, this);
			AddBaseNode(newNode);
			ExpandAll();
			return newNode;
		}

		public DesignerNode InsertNode(int index, INode node)
		{
			DesignerNode newNode = new DesignerNode(node, this);
			InsertBaseNode(index, newNode);
			ExpandAll();
			return newNode;
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

		protected override void OnAfterLabelEdit(NodeLabelEditEventArgs args)
		{
			DesignerNode node = (DesignerNode)args.Node;
			try
			{
				if (node.Node.Name != args.Label)
				{
					IHost host = node.Node.HostNode;
					if (host.GetNode(args.Label) != null)
						FormDesigner.Dataphoria.Warnings.AppendError(FormDesigner, new DataphoriaException(DataphoriaException.Codes.InvalidRename, args.Label), false);
					else
					{
						if (args.Label != null)
						{
							Modified();
							node.Node.Name = args.Label;
						}
					}
				}
			}
			finally
			{
				LabelEdit = false;
				args.CancelEdit = true;	// always cancel so that the edit doesn't overwrite our update
				node.UpdateText(false);
			}
		}

		// Palette

		private PaletteItem _paletteItem;
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PaletteItem PaletteItem
		{
			get { return _paletteItem; }
			set
			{
				if (value != _paletteItem)
				{
					if (_paletteItem == null)
					{
						_dropFilter.QueryAllowedPositions = new QueryAllowedPositionsHandler(QueryAllowedPalettePositions);
						UpdateDropHighlight();
						Cursor = Cursors.Cross;
					}
					_paletteItem = value;
					if (_paletteItem == null)
					{
						Cursor = Cursors.Default;
						_dropFilter.ClearDropHighlight();
						_dropFilter.QueryAllowedPositions = null;
					}
				}
			}
		}

		public void UpdateDropHighlight()
		{
			_dropFilter.SetDropHighlight(PointToClient(Control.MousePosition), _paletteItem, DropOperation.Copy);
		}

		protected override bool IsInputKey(Keys key)
		{
			return 
				(
					(SelectedNode != null) 
						&&
						(
							(
								((key == Keys.Delete) || (key == Keys.F2)) && !SelectedNode.ReadOnly
							)
								|| (key == (Keys.X | Keys.Control))
								|| (key == (Keys.C | Keys.Control))
								|| (key == (Keys.V | Keys.Control))
						)
				)
					|| base.IsInputKey(key);
		}

		protected override void OnKeyDown(KeyEventArgs args)
		{
			if (SelectedNode != null)
			{
				switch (args.KeyData)
				{
					case Keys.Delete : SelectedNode.Delete(); break;
					case Keys.X | Keys.Control : SelectedNode.CutToClipboard(); break;
					case Keys.C | Keys.Control : SelectedNode.CopyToClipboard(); break;
					case Keys.V | Keys.Control : SelectedNode.PasteFromClipboard(); break;
					case Keys.F2 : SelectedNode.Rename(); break;
					default : base.OnKeyDown(args); return;
				}
				args.Handled = true;
			}
			else
				base.OnKeyDown(args);
		}

		protected override void OnMouseMove(MouseEventArgs args)
		{
			base.OnMouseMove(args);
			if (_paletteItem != null)
				UpdateDropHighlight();
			else
			{
				TreeNode node = GetNodeAt(args.X, args.Y);
				if ((args.Button != MouseButtons.None) && (SelectedNode != null) && (node != null))
					BeginDrag();
			}
		}

		protected override void OnMouseLeave(EventArgs args)
		{
			base.OnMouseLeave(args);
			if (_paletteItem != null)
				_dropFilter.ClearDropHighlight();
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == NativeMethods.WM_LBUTTONDOWN)
			{
				int param = (int)message.LParam;
				if ((_paletteItem != null) && (this.GetNodeAt(param & 0xFFFF, param >> 16) != null))
				{
					if (_dropFilter.DropHighlightNode != null)
						((DesignerNode)_dropFilter.DropHighlightNode).AddNew(_paletteItem, _dropFilter.DropLinePosition);
					FormDesigner.PaletteItemDropped();
					message.Result = IntPtr.Zero;
					return;
				}
			}
			base.WndProc(ref message);
		}


		private void QueryAllowedPalettePositions(object sender, QueryAllowedPositionsEventArgs args)
		{
			((DesignerNode)args.TargetNode).QueryAllowedPalettePositions(args);
		}

		// Drag & drop

		private void QueryAllowedDragPositions(object sender, QueryAllowedPositionsEventArgs args)
		{
			((DesignerNode)args.TargetNode).QueryAllowedDragPositions(args);
		}

		private void BeginDrag()
		{
			if (SelectedNode.Parent != null) // not the root node
			{
				_dropFilter.QueryAllowedPositions += new QueryAllowedPositionsHandler(QueryAllowedDragPositions);
				DoDragDrop(new DesignerNodeData(SelectedNode), DragDropEffects.Move | DragDropEffects.Copy);
			}
		}

		private void EndDrag()
		{
			_dropFilter.QueryAllowedPositions -= new QueryAllowedPositionsHandler(QueryAllowedDragPositions);
			_dropFilter.ClearDropHighlight();;
		}

		private void InternalEnterOrOver(DragEventArgs args)
		{
			DropOperation requestedOperation;
			if (Control.MouseButtons == MouseButtons.Right)
				requestedOperation = DropOperation.Both;
			else
			{
				if (Control.ModifierKeys == Keys.Control)
					requestedOperation = DropOperation.Copy;
				else
					requestedOperation = DropOperation.Move;
			}

			_dropFilter.SetDropHighlight
			(
				PointToClient(new Point(args.X, args.Y)), 
				args.Data,
				requestedOperation
			);

			args.Effect = DragDropEffects.None;
			if ((_dropFilter.DropOperation & DropOperation.Copy) != 0)
				args.Effect |= DragDropEffects.Copy;
			if ((_dropFilter.DropOperation & DropOperation.Move) != 0)
				args.Effect |= DragDropEffects.Move;
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

		protected override void OnDragDrop(DragEventArgs args)
		{
			if (args.Effect != DragDropEffects.None)
			{
				try
				{
					base.OnDragDrop(args);
					DesignerNodeData data = args.Data as DesignerNodeData;
					if 
					(
						(_dropFilter.DropHighlightNode != null) && 
						(_dropFilter.DropLinePosition != DropLinePosition.None) &&
						(data != null)
					)
					{
						if (data.RightButton)
							new DesignerTreeDropMenu(data.TreeNode, (DesignerNode)_dropFilter.DropHighlightNode, _dropFilter.DropLinePosition, _dropFilter.DropOperation).Show(this, PointToClient(Control.MousePosition));
						else
						{
							if (Control.ModifierKeys == Keys.Control)
								((DesignerNode)_dropFilter.DropHighlightNode).CopyFromNode(data.TreeNode, _dropFilter.DropLinePosition);
							else
								((DesignerNode)_dropFilter.DropHighlightNode).MoveFromNode(data.TreeNode, _dropFilter.DropLinePosition);
						}
					}
					EndDrag();
				}
				catch (Exception exception)
				{
					// must handle exceptions because the framework ignores them
					FormDesigner.Dataphoria.Warnings.AppendError(FormDesigner, exception, false);
					// do not re-throw
				}
			}
		}

		protected override void OnQueryContinueDrag(QueryContinueDragEventArgs args)
		{
			if (args.EscapePressed)
			{
				args.Action = DragAction.Cancel;
				EndDrag();
			}
		}
	}
}
