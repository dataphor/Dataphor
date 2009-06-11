using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree
{
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
}