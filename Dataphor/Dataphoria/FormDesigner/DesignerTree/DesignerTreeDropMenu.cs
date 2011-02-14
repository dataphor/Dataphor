using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner.DesignerTree
{
    public class DesignerTreeDropMenu : ContextMenu
    {
        public DesignerTreeDropMenu(DesignerNode source, DesignerNode target, DropLinePosition position, DropOperation supportedOperations)
        {
            _source = source;
            _target = target;
            _position = position;

            if ((supportedOperations & DropOperation.Copy) != 0)
                MenuItems.Add(new MenuItem(Strings.DropMenu_Copy, new EventHandler(CopyClick)));
            if ((supportedOperations & DropOperation.Move) != 0)
                MenuItems.Add(new MenuItem(Strings.DropMenu_Move, new EventHandler(MoveClick)));
            MenuItems.Add(new MenuItem("-"));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Cancel));
        }

        private DesignerNode _source;
        private DesignerNode _target;
        DropLinePosition _position;

        private void CopyClick(object sender, EventArgs args)
        {
            _target.CopyFromNode(_source, _position);
        }

        private void MoveClick(object sender, EventArgs args)
        {
            _target.MoveFromNode(_source, _position);
        }
    }
}