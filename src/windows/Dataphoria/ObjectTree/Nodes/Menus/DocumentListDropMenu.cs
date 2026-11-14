using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes.Menus
{
    public class DocumentListDropMenu : ContextMenu
    {
        public DocumentListDropMenu(DocumentData source, DocumentListNode target)
        {
            _source = source;
            _target = target;

            MenuItems.Add(new MenuItem(Strings.DropMenu_Copy, new EventHandler(CopyClick)));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Move, new EventHandler(MoveClick)));
            MenuItems.Add(new MenuItem("-"));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Cancel));
        }

        private DocumentData _source;
        private DocumentListNode _target;

        private void CopyClick(object sender, EventArgs args)
        {
            _target.CopyFromDocument(_source.Node.LibraryName, _source.Node.DocumentName);
        }

        private void MoveClick(object sender, EventArgs args)
        {
            _target.MoveFromDocument(_source.Node.LibraryName, _source.Node.DocumentName);
            ((DocumentListNode)_source.Node.Parent).ReconcileChildren();
        }
    }
}