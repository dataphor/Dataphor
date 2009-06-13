using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DataNode : BaseNode
    {
        public IDataphoria Dataphoria
        {
            get
            {
                if (TreeView == null)
                    return null;
                else
                    return ((DataTree)TreeView).Dataphoria; 
            }
        }

        public virtual void ItemDrag()
        {
        }

        public virtual void DragDrop(DragEventArgs AArgs)
        {
        }

        public virtual void DragOver(DragEventArgs AArgs)
        {
            AArgs.Effect = DragDropEffects.None;
        }

        public virtual void DragLeave()
        {
        }
    }
}