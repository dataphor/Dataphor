using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
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