using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes.Menus
{
    public class DocumentListFileDropMenu : ContextMenu
    {
        public DocumentListFileDropMenu(Array source, DocumentListNode target)
        {
            _source = source;
            _target = target;

            MenuItems.Add(new MenuItem(Strings.DropMenu_Copy, new EventHandler(CopyClick)));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Move, new EventHandler(MoveClick)));
            MenuItems.Add(new MenuItem("-"));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Cancel));
        }

        private Array _source;
        private DocumentListNode _target;

        private void CopyClick(object sender, EventArgs args)
        {
            _target.CopyFromFiles(_source);
        }

        private void MoveClick(object sender, EventArgs args)
        {
            _target.MoveFromFiles(_source);
        }
    }
}