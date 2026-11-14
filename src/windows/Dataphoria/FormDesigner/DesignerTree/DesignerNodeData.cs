using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree
{
    public class DesignerNodeData : DataObject
    {
        public DesignerNodeData(DesignerNode treeNode)
        {
            _treeNode = treeNode;
            _rightButton = Control.MouseButtons == MouseButtons.Right;
        }

        private DesignerNode _treeNode;
        public DesignerNode TreeNode
        {
            get { return _treeNode; }
        }

        private bool _rightButton;
        public bool RightButton
        {
            get { return _rightButton; }
        }
    }
}