using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes.Menus
{
    public class DocumentListDropMenu : ContextMenu
    {
        public DocumentListDropMenu(DocumentData ASource, DocumentListNode ATarget)
        {
            FSource = ASource;
            FTarget = ATarget;

            MenuItems.Add(new MenuItem(Strings.DropMenu_Copy, new EventHandler(CopyClick)));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Move, new EventHandler(MoveClick)));
            MenuItems.Add(new MenuItem("-"));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Cancel));
        }

        private DocumentData FSource;
        private DocumentListNode FTarget;

        private void CopyClick(object ASender, EventArgs AArgs)
        {
            FTarget.CopyFromDocument(FSource.Node.LibraryName, FSource.Node.DocumentName);
        }

        private void MoveClick(object ASender, EventArgs AArgs)
        {
            FTarget.MoveFromDocument(FSource.Node.LibraryName, FSource.Node.DocumentName);
            ((DocumentListNode)FSource.Node.Parent).ReconcileChildren();
        }
    }
}