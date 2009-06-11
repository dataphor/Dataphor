using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree
{
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
}