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
        public DesignerNode(INode node, DesignerTree tree, bool readOnly)
        {
            InitializeNode(node, tree, readOnly);
        }

        /// <summary> Constructs a node and builds its children. </summary>
        public DesignerNode(INode node, DesignerTree tree)
        {
            InitializeNode(node, tree, false);
            foreach (INode child in _node.Children) 
                AddNode(child);
        }

        private void InitializeNode(INode node, DesignerTree tree, bool readOnly)
        {
            _node = node;
            _node.Site = this;

            _designerTree = tree;
            _readOnly = readOnly;

            UpdateText(false);

            ImageIndex = tree.FormDesigner.GetDesignerImage(node.GetType());
            SelectedImageIndex = ImageIndex;
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach (DesignerNode child in Nodes)
                child.Dispose();
            _node.Site = null;
        }

        public void Dispose()
        {
            Dispose(false);
        }

        // Node

        private INode _node;
        public INode Node
        {
            get	{ return _node; }
        }

        // ReadOnly

        private bool _readOnly;
        public bool ReadOnly
        {
            get { return _readOnly; }
        }

        public void SetReadOnly(bool tempValue, bool recursive)
        {
            _readOnly = tempValue;
            if (recursive)
                foreach (DesignerNode child in Nodes) 
                    child.SetReadOnly(tempValue, true);
        }

        // DesignerTree

        private DesignerTree _designerTree;
        public DesignerTree DesignerTree
        {
            get { return _designerTree; }
        }

        // Parent

        public new DesignerNode Parent
        {
            get { return (DesignerNode)base.Parent; }
        }

        // Node Operations

        private DesignerNode Copy()
        {
            DesignerNode copy = new DesignerNode(Node, _designerTree, ReadOnly);
            foreach (DesignerNode node in Nodes)
                copy.AddBaseNode(node.Copy());
            return copy;
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
            using (MemoryStream stream = new MemoryStream())
            {
                BOP.Serializer serializer = DesignerTree.FormDesigner.FrontendSession.CreateSerializer();
                serializer.RemoveReferencesToObjectsNotSerialized = false;
                serializer.Serialize(stream, Node);
                DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, serializer.Errors, true);
                stream.Position = 0;
                Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, new StreamReader(stream).ReadToEnd()));
            }
        }

        public void CutToClipboard()
        {
            CopyToClipboard();
            InternalDelete();
        }

        private void RecursiveGetUniqueName(INode node) 
        {
            Node.HostNode.GetUniqueName(node);
            foreach (INode child in node.Children) 
                RecursiveGetUniqueName(child);
        }

        public void PasteFromClipboard()
        {
            INode node;
            using (MemoryStream stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write((string)Clipboard.GetDataObject().GetData(DataFormats.UnicodeText, true));
                writer.Flush();
                stream.Position = 0;
                BOP.Deserializer deserializer = DesignerTree.FormDesigner.FrontendSession.CreateDeserializer();
                deserializer.FindReference += new BOP.FindReferenceHandler(DeserializeFindReference);
                node = (INode)deserializer.Deserialize(stream, null);
                DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, deserializer.Errors, true);
            }

            Node.Children.Add(node);

            RecursiveGetUniqueName(node);	// make names unique after adding the node in order to properly account for all nodes

            DesignerNode designerNode = AddNode(node);
            TreeView.SelectedNode = designerNode;
            designerNode.ExpandAll();
            DesignerTree.Modified();
        }

        private DesignerNode PlaceNewNode(INode node, DropLinePosition position)
        {
            DesignerNode designerNode;
            int index;
            switch (position)
            {
                case DropLinePosition.OnNode :
                    Node.Children.Add(node);
                    designerNode = AddNode(node);
                    break;
                case DropLinePosition.AboveNode :
                    index = Parent.Nodes.IndexOf(this);
                    Node.Parent.Children.Insert(index, node);
                    designerNode = Parent.InsertNode(index, node);
                    break;
                case DropLinePosition.BelowNode :
                    index = Parent.Nodes.IndexOf(this) + 1;
                    Node.Parent.Children.Insert(index, node);
                    designerNode = Parent.InsertNode(index, node);
                    break;
                default :
                    System.Diagnostics.Debug.Fail("Invalid DropLinePosition passed to CopyFromNode()");
                    designerNode = null;
                    break;
            }
            TreeView.SelectedNode = designerNode;
            designerNode.ExpandAll();
            return designerNode;
        }

        public void CopyFromNode(DesignerNode source, DropLinePosition position)
        {
            INode node;
            using (MemoryStream stream = new MemoryStream())
            {
                BOP.Serializer serializer = DesignerTree.FormDesigner.FrontendSession.CreateSerializer();
                serializer.Serialize(stream, source);
                DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, serializer.Errors, true);
                stream.Position = 0;
                BOP.Deserializer deserializer = DesignerTree.FormDesigner.FrontendSession.CreateDeserializer();
                deserializer.FindReference += new BOP.FindReferenceHandler(DeserializeFindReference);
                node = (INode)deserializer.Deserialize(stream, null);
                DesignerTree.FormDesigner.Dataphoria.Warnings.AppendErrors(DesignerTree.FormDesigner, deserializer.Errors, true);
            }
            RecursiveGetUniqueName(node);

            PlaceNewNode(node, position);
            DesignerTree.Modified();
        }

        private void InternalMoveNode(INode node, INode newParent, int newIndex)
        {
            int oldIndex = node.Parent.Children.IndexOf(node);
            INode oldParent = node.Parent;
            oldParent.Children.DisownAt(oldIndex);
            try
            {
                newParent.Children.Insert(newIndex, node);
            }
            catch
            {
                oldParent.Children.Insert(oldIndex, node);		// attempt recovery
                throw;
            }
        }

        private void MoveIntoSibling(DesignerNode source, int index)
        {
            int siblingIndex = Parent.Nodes.IndexOf(source);
            if ((siblingIndex >= 0) && (siblingIndex < index))
                index--;
            InternalMoveNode(source.Node, Node.Parent, index);
            // Remove and recreate the node -- the tree draws the lines improperly if we just move the node, and ASource.Reposition raises a null reference exception
            source.Remove();
            DesignerNode newNode = source.Copy();
            Parent.InsertBaseNode(index, newNode);
            TreeView.SelectedNode = newNode;
            Parent.ExpandAll();
        }

        public void MoveFromNode(DesignerNode source, DropLinePosition position)
        {
            switch (position)
            {
                case DropLinePosition.OnNode :
                    int newIndex = Node.Children.Count;
                    if (Node.Children.Contains(source.Node))
                        newIndex--;
                    InternalMoveNode(source.Node, Node, newIndex);
                    // Remove and recreate the designer node -- the tree draws the lines improperly if we just move the node, and ASource.Reposition raises a null reference exception
                    source.Remove();
                    DesignerNode newNode = source.Copy();
                    AddBaseNode(newNode);
                    TreeView.SelectedNode = newNode;
                    if (Parent != null)
                        Parent.ExpandAll();
                    break;
                case DropLinePosition.AboveNode :
                    MoveIntoSibling(source, Node.Parent.Children.IndexOf(Node));
                    break;
                case DropLinePosition.BelowNode :
                    MoveIntoSibling(source, Node.Parent.Children.IndexOf(Node) + 1);
                    break;
            }
            DesignerTree.Modified();
        }

        public void AddNew(PaletteItem item, DropLinePosition position)
        {
            INode node = (INode)DesignerTree.FormDesigner.FrontendSession.NodeTypeTable.CreateInstance(item.ClassName);
            try
            {
                Node.HostNode.GetUniqueName(node);
                PlaceNewNode(node, position).Rename();
            }
            catch
            {
                node.Dispose();
                throw;
            }
            DesignerTree.Modified();
        }

        private object DeserializeFindReference(string stringValue)
        {
            return Node.HostNode.FindNode(stringValue);
        }

        /// <remarks> This method does not change the modified state of the editor. </remarks>
        public DesignerNode AddNode(INode node)
        {
            DesignerNode newNode = new DesignerNode(node, _designerTree);
            AddBaseNode(newNode);
            if (TreeView != null)	// throws if node is not inserted
                ExpandAll();
            return newNode;
        }

        /// <remarks> This method does not change the modified state of the editor. </remarks>
        public DesignerNode InsertNode(int index, INode node)
        {
            DesignerNode newNode = new DesignerNode(node, _designerTree);
            InsertBaseNode(index, newNode);
            if (TreeView != null)	// throws if node is not inserted
                ExpandAll();
            return newNode;
        }

        public void UpdateText(bool editText)
        {
            if (editText)
                Text = Node.Name;
            else
            {
                string name = _node.GetType().Name;
                if ((_node.Name == null) || (_node.Name == String.Empty))
                    Text = String.Format("[{0}]", name);
                else
                    Text = String.Format("{0}  [{1}]", _node.Name, name);
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
        public static bool IsNodeContained(INode node, INode queryNode, bool checkNode) 
        {
            if (checkNode && Object.ReferenceEquals(node, queryNode))
                return true;

            foreach (INode child in node.Children) 
                if (IsNodeContained(child, queryNode, true))
                    return true;

            return false;
        }

        public void QueryAllowedPalettePositions(QueryAllowedPositionsEventArgs args)
        {
            args.AllowedCopyPositions = DropLinePosition.None;
            args.AllowedMovePositions = DropLinePosition.None;
            PaletteItem item = args.Source as PaletteItem;
            if (item != null)
            {
                INode target = ((DesignerNode)args.TargetNode).Node;
                Type type = DesignerTree.FormDesigner.FrontendSession.NodeTypeTable.GetClassType(item.ClassName);
                if (target.IsValidChild(type))
                    args.AllowedCopyPositions |= DropLinePosition.OnNode;
                if ((args.TargetNode.Parent != null) && target.Parent.IsValidChild(type))
                    args.AllowedCopyPositions |= (DropLinePosition.AboveNode | DropLinePosition.BelowNode);
            }
        }

        public void QueryAllowedDragPositions(QueryAllowedPositionsEventArgs args)
        {
            args.AllowedCopyPositions = DropLinePosition.None;
            args.AllowedMovePositions = DropLinePosition.None;
            DesignerNodeData sourceNodeData = args.Source as DesignerNodeData;
            if (sourceNodeData != null)
            {
                INode source = sourceNodeData.TreeNode.Node;
                INode target = ((DesignerNode)args.TargetNode).Node;
                if (target.IsValidChild(source))
                {
                    args.AllowedCopyPositions |= DropLinePosition.OnNode;
                    if (!IsNodeContained(source, target, true))
                        args.AllowedMovePositions |= DropLinePosition.OnNode;
                }

                if ((args.TargetNode.Parent != null) && target.Parent.IsValidChild(source))
                {
                    args.AllowedCopyPositions |= (DropLinePosition.AboveNode | DropLinePosition.BelowNode);
                    if (!IsNodeContained(source, target.Parent, true) && !Object.ReferenceEquals(target, source))
                    {
                        int index = target.Parent.Children.IndexOf(target);
                        if 
                            (
                            (index == 0) || 
                            ((index > 0) && !ReferenceEquals(target.Parent.Children[index - 1], source))
                            )
                            args.AllowedMovePositions |= DropLinePosition.AboveNode;
                        if
                            (
                            (index == target.Parent.Children.Count - 1) ||
                            ((index < target.Parent.Children.Count - 1) && !ReferenceEquals(target.Parent.Children[index + 1], source))
                            )
                            args.AllowedMovePositions |= DropLinePosition.BelowNode;
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

        public object GetService(Type serviceType)
        {
            return DesignerTree.FormDesigner.GetService(serviceType);
        }

        #endregion
    }
}