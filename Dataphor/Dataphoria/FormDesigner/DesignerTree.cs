/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
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

	public class DesignerNode : BaseNode, ISite, IDisposable
	{
		/// <summary> Constructs a node, but does not build its children. </summary>
		public DesignerNode(INode ANode, DesignerTree ATree, bool AReadOnly)
		{
			InitializeNode(ANode, ATree, AReadOnly);
		}

		/// <summary> Constructs a node and builds its children. </summary>
		public DesignerNode(INode ANode, DesignerTree ATree)
		{
			InitializeNode(ANode, ATree, false);
			foreach (INode LChild in FNode.Children) 
				AddNode(LChild);
		}

		private void InitializeNode(INode ANode, DesignerTree ATree, bool AReadOnly)
		{
			FNode = ANode;
			FNode.Site = this;

			FDesignerTree = ATree;
			FReadOnly = AReadOnly;

			UpdateText(false);

			ImageIndex = ATree.FormDesigner.GetDesignerImage(ANode.GetType());
			SelectedImageIndex = ImageIndex;
		}

		protected virtual void Dispose(bool ADisposing)
		{
			foreach (DesignerNode LChild in Nodes)
				LChild.Dispose();
			FNode.Site = null;
		}

		public void Dispose()
		{
			Dispose(false);
		}

		// Node

		private INode FNode;
		public INode Node
		{
			get	{ return FNode; }
		}

		// ReadOnly

		private bool FReadOnly;
		public bool ReadOnly
		{
			get { return FReadOnly; }
		}

		public void SetReadOnly(bool AValue, bool ARecursive)
		{
			FReadOnly = AValue;
			if (ARecursive)
				foreach (DesignerNode LChild in Nodes) 
					LChild.SetReadOnly(AValue, true);
		}

		// DesignerTree

		private DesignerTree FDesignerTree;
		public DesignerTree DesignerTree
		{
			get { return FDesignerTree; }
		}

		// Parent

		public new DesignerNode Parent
		{
			get { return (DesignerNode)base.Parent; }
		}

		// Node Operations

		private DesignerNode Copy()
		{
			DesignerNode LCopy = new DesignerNode(Node, FDesignerTree, ReadOnly);
			foreach (DesignerNode LNode in Nodes)
				LCopy.AddBaseNode(LNode.Copy());
			return LCopy;
		}

		public void DeleteNode()
		{
			if (Node != null)
				Node.Dispose();
		}

		private void InternalDelete()
		{
			if (IsEditing)
				EndEdit(true);
			DeleteNode();
			Remove();
		}

		public bool Delete()
		{
			if (ReadOnly)
				return false;
			else
			{
				InternalDelete();
				DesignerTree.Modified();
				DesignerTree.FormDesigner.ActivateNode(null);
				return true;
			}
		}

		public void CopyToClipboard()
		{
			using (MemoryStream LStream = new MemoryStream())
			{
				BOP.Serializer LSerializer = DesignerTree.FormDesigner.FrontendSession.CreateSerializer();
				LSerializer.RemoveReferencesToObjectsNotSerialized = false;
				LSerializer.Serialize(LStream, Node);
				DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, LSerializer.Errors, true);
				LStream.Position = 0;
				Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, new StreamReader(LStream).ReadToEnd()));
			}
		}

		public void CutToClipboard()
		{
			CopyToClipboard();
			InternalDelete();
		}

		private void RecursiveGetUniqueName(INode ANode) 
		{
			Node.HostNode.GetUniqueName(ANode);
			foreach (INode LChild in ANode.Children) 
				RecursiveGetUniqueName(LChild);
		}

		public void PasteFromClipboard()
		{
			INode LNode;
			using (MemoryStream LStream = new MemoryStream())
			{
				StreamWriter LWriter = new StreamWriter(LStream);
				LWriter.Write((string)Clipboard.GetDataObject().GetData(DataFormats.UnicodeText, true));
				LWriter.Flush();
				LStream.Position = 0;
				BOP.Deserializer LDeserializer = DesignerTree.FormDesigner.FrontendSession.CreateDeserializer();
				LDeserializer.FindReference += new BOP.FindReferenceHandler(DeserializeFindReference);
                LNode = (INode)LDeserializer.Deserialize(LStream, null);
				DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, LDeserializer.Errors, true);
			}

			Node.Children.Add(LNode);

			RecursiveGetUniqueName(LNode);	// make names unique after adding the node in order to properly account for all nodes

			DesignerNode LDesignerNode = AddNode(LNode);
			TreeView.SelectedNode = LDesignerNode;
			LDesignerNode.ExpandAll();
			DesignerTree.Modified();
		}

		private DesignerNode PlaceNewNode(INode ANode, DropLinePosition APosition)
		{
			DesignerNode LDesignerNode;
			int LIndex;
			switch (APosition)
			{
				case DropLinePosition.OnNode :
					Node.Children.Add(ANode);
					LDesignerNode = AddNode(ANode);
					break;
				case DropLinePosition.AboveNode :
					LIndex = Parent.Nodes.IndexOf(this);
					Node.Parent.Children.Insert(LIndex, ANode);
					LDesignerNode = Parent.InsertNode(LIndex, ANode);
					break;
				case DropLinePosition.BelowNode :
					LIndex = Parent.Nodes.IndexOf(this) + 1;
					Node.Parent.Children.Insert(LIndex, ANode);
					LDesignerNode = Parent.InsertNode(LIndex, ANode);
					break;
				default :
					System.Diagnostics.Debug.Fail("Invalid DropLinePosition passed to CopyFromNode()");
					LDesignerNode = null;
					break;
			}
			TreeView.SelectedNode = LDesignerNode;
			LDesignerNode.ExpandAll();
			return LDesignerNode;
		}

		public void CopyFromNode(DesignerNode ASource, DropLinePosition APosition)
		{
			INode LNode;
			using (MemoryStream LStream = new MemoryStream())
			{
				BOP.Serializer LSerializer = DesignerTree.FormDesigner.FrontendSession.CreateSerializer();
				LSerializer.Serialize(LStream, ASource);
				DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, LSerializer.Errors, true);
				LStream.Position = 0;
				BOP.Deserializer LDeserializer = DesignerTree.FormDesigner.FrontendSession.CreateDeserializer();
				LDeserializer.FindReference += new BOP.FindReferenceHandler(DeserializeFindReference);
				LNode = (INode)LDeserializer.Deserialize(LStream, null);
				DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, LDeserializer.Errors, true);
			}
			RecursiveGetUniqueName(LNode);

			PlaceNewNode(LNode, APosition);
			DesignerTree.Modified();
		}

		private void InternalMoveNode(INode ANode, INode ANewParent, int ANewIndex)
		{
			int LOldIndex = ANode.Parent.Children.IndexOf(ANode);
			INode LOldParent = ANode.Parent;
			LOldParent.Children.DisownAt(LOldIndex);
			try
			{
				ANewParent.Children.Insert(ANewIndex, ANode);
			}
			catch
			{
				LOldParent.Children.Insert(LOldIndex, ANode);		// attempt recovery
				throw;
			}
		}

		private void MoveIntoSibling(DesignerNode ASource, int AIndex)
		{
			int LSiblingIndex = Parent.Nodes.IndexOf(ASource);
			if ((LSiblingIndex >= 0) && (LSiblingIndex < AIndex))
				AIndex--;
			InternalMoveNode(ASource.Node, Node.Parent, AIndex);
			// Remove and recreate the node -- the tree draws the lines improperly if we just move the node, and ASource.Reposition raises a null reference exception
			ASource.Remove();
			DesignerNode LNewNode = ASource.Copy();
			Parent.InsertBaseNode(AIndex, LNewNode);
			TreeView.SelectedNode = LNewNode;
			Parent.ExpandAll();
		}

		public void MoveFromNode(DesignerNode ASource, DropLinePosition APosition)
		{
			switch (APosition)
			{
				case DropLinePosition.OnNode :
					int LNewIndex = Node.Children.Count;
					if (Node.Children.Contains(ASource.Node))
						LNewIndex--;
					InternalMoveNode(ASource.Node, Node, LNewIndex);
					// Remove and recreate the designer node -- the tree draws the lines improperly if we just move the node, and ASource.Reposition raises a null reference exception
					ASource.Remove();
					DesignerNode LNewNode = ASource.Copy();
					AddBaseNode(LNewNode);
					TreeView.SelectedNode = LNewNode;
					if (Parent != null)
						Parent.ExpandAll();
					break;
				case DropLinePosition.AboveNode :
					MoveIntoSibling(ASource, Node.Parent.Children.IndexOf(Node));
					break;
				case DropLinePosition.BelowNode :
					MoveIntoSibling(ASource, Node.Parent.Children.IndexOf(Node) + 1);
					break;
			}
			DesignerTree.Modified();
		}

		public void AddNew(PaletteItem AItem, DropLinePosition APosition)
		{
			INode LNode = (INode)DesignerTree.FormDesigner.FrontendSession.NodeTypeTable.CreateInstance(AItem.ClassName);
			try
			{
				Node.HostNode.GetUniqueName(LNode);
				PlaceNewNode(LNode, APosition).Rename();
			}
			catch
			{
				LNode.Dispose();
				throw;
			}
			DesignerTree.Modified();
		}

		private object DeserializeFindReference(string AString)
		{
			return Node.HostNode.FindNode(AString);
		}

		/// <remarks> This method does not change the modified state of the editor. </remarks>
		public DesignerNode AddNode(INode ANode)
		{
			DesignerNode LNewNode = new DesignerNode(ANode, FDesignerTree);
			AddBaseNode(LNewNode);
			if (TreeView != null)	// throws if node is not inserted
				ExpandAll();
			return LNewNode;
		}

		/// <remarks> This method does not change the modified state of the editor. </remarks>
		public DesignerNode InsertNode(int AIndex, INode ANode)
		{
			DesignerNode LNewNode = new DesignerNode(ANode, FDesignerTree);
			InsertBaseNode(AIndex, LNewNode);
			if (TreeView != null)	// throws if node is not inserted
				ExpandAll();
			return LNewNode;
		}

		public void UpdateText(bool AEditText)
		{
			if (AEditText)
				Text = Node.Name;
			else
			{
				string LName = FNode.GetType().Name;
				if ((FNode.Name == null) || (FNode.Name == String.Empty))
					Text = String.Format("[{0}]", LName);
				else
					Text = String.Format("{0}  [{1}]", FNode.Name, LName);
			}
		}

		public void Rename()
		{
			if (!ReadOnly)
			{
				TreeView.LabelEdit = true;
				UpdateText(true);
				BeginEdit();
			}
		}

		/// <summary> Searches for AQueryNode optionally in ANode and always in ANode's children. </summary>
		public static bool IsNodeContained(INode ANode, INode AQueryNode, bool ACheckNode) 
		{
			if (ACheckNode && Object.ReferenceEquals(ANode, AQueryNode))
				return true;

			foreach (INode LChild in ANode.Children) 
				if (IsNodeContained(LChild, AQueryNode, true))
					return true;

			return false;
		}

		public void QueryAllowedPalettePositions(QueryAllowedPositionsEventArgs AArgs)
		{
			AArgs.AllowedCopyPositions = DropLinePosition.None;
			AArgs.AllowedMovePositions = DropLinePosition.None;
			PaletteItem LItem = AArgs.Source as PaletteItem;
			if (LItem != null)
			{
				INode LTarget = ((DesignerNode)AArgs.TargetNode).Node;
				Type LType = DesignerTree.FormDesigner.FrontendSession.NodeTypeTable.GetClassType(LItem.ClassName);
				if (LTarget.IsValidChild(LType))
					AArgs.AllowedCopyPositions |= DropLinePosition.OnNode;
				if ((AArgs.TargetNode.Parent != null) && LTarget.Parent.IsValidChild(LType))
					AArgs.AllowedCopyPositions |= (DropLinePosition.AboveNode | DropLinePosition.BelowNode);
			}
		}

		public void QueryAllowedDragPositions(QueryAllowedPositionsEventArgs AArgs)
		{
			AArgs.AllowedCopyPositions = DropLinePosition.None;
			AArgs.AllowedMovePositions = DropLinePosition.None;
			DesignerNodeData LSourceNodeData = AArgs.Source as DesignerNodeData;
			if (LSourceNodeData != null)
			{
				INode LSource = LSourceNodeData.TreeNode.Node;
				INode LTarget = ((DesignerNode)AArgs.TargetNode).Node;
				if (LTarget.IsValidChild(LSource))
				{
					AArgs.AllowedCopyPositions |= DropLinePosition.OnNode;
					if (!IsNodeContained(LSource, LTarget, true))
						AArgs.AllowedMovePositions |= DropLinePosition.OnNode;
				}

				if ((AArgs.TargetNode.Parent != null) && LTarget.Parent.IsValidChild(LSource))
				{
					AArgs.AllowedCopyPositions |= (DropLinePosition.AboveNode | DropLinePosition.BelowNode);
					if (!IsNodeContained(LSource, LTarget.Parent, true) && !Object.ReferenceEquals(LTarget, LSource))
					{
						int LIndex = LTarget.Parent.Children.IndexOf(LTarget);
						if 
						(
							(LIndex == 0) || 
							((LIndex > 0) && !ReferenceEquals(LTarget.Parent.Children[LIndex - 1], LSource))
						)
							AArgs.AllowedMovePositions |= DropLinePosition.AboveNode;
						if
						(
							(LIndex == LTarget.Parent.Children.Count - 1) ||
							((LIndex < LTarget.Parent.Children.Count - 1) && !ReferenceEquals(LTarget.Parent.Children[LIndex + 1], LSource))
						)
							AArgs.AllowedMovePositions |= DropLinePosition.BelowNode;
					}
				}
			}
		}

		#region ISite Members

		public IComponent Component
		{
			get { return Node; }
		}

		public IContainer Container
		{
			get { return DesignerTree.FormDesigner; }
		}

		public bool DesignMode
		{
			get { return true; }
		}

		#endregion

		#region IServiceProvider Members

		public object GetService(Type AServiceType)
		{
			return DesignerTree.FormDesigner.GetService(AServiceType);
		}

		#endregion
	}

	public class DesignerTreeDropMenu : ContextMenu
	{
		public DesignerTreeDropMenu(DesignerNode ASource, DesignerNode ATarget, DropLinePosition APosition, DropOperation ASupportedOperations)
		{
			FSource = ASource;
			FTarget = ATarget;
			FPosition = APosition;

			if ((ASupportedOperations & DropOperation.Copy) != 0)
				MenuItems.Add(new MenuItem(Strings.DropMenu_Copy, new EventHandler(CopyClick)));
			if ((ASupportedOperations & DropOperation.Move) != 0)
				MenuItems.Add(new MenuItem(Strings.DropMenu_Move, new EventHandler(MoveClick)));
			MenuItems.Add(new MenuItem("-"));
			MenuItems.Add(new MenuItem(Strings.DropMenu_Cancel));
		}

		private DesignerNode FSource;
		private DesignerNode FTarget;
		DropLinePosition FPosition;

		private void CopyClick(object ASender, EventArgs AArgs)
		{
			FTarget.CopyFromNode(FSource, FPosition);
		}

		private void MoveClick(object ASender, EventArgs AArgs)
		{
			FTarget.MoveFromNode(FSource, FPosition);
		}
	}

	public class DesignerNodeData : DataObject
	{
		public DesignerNodeData(DesignerNode ATreeNode)
		{
			FTreeNode = ATreeNode;
			FRightButton = Control.MouseButtons == MouseButtons.Right;
		}

		private DesignerNode FTreeNode;
		public DesignerNode TreeNode
		{
			get { return FTreeNode; }
		}

		private bool FRightButton;
		public bool RightButton
		{
			get { return FRightButton; }
		}
	}
}
