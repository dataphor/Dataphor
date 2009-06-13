using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DocumentListFileDropMenu : ContextMenu
    {
        public DocumentListFileDropMenu(Array ASource, DocumentListNode ATarget)
        {
            FSource = ASource;
            FTarget = ATarget;

            MenuItems.Add(new MenuItem(Strings.DropMenu_Copy, new EventHandler(CopyClick)));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Move, new EventHandler(MoveClick)));
            MenuItems.Add(new MenuItem("-"));
            MenuItems.Add(new MenuItem(Strings.DropMenu_Cancel));
        }

        private Array FSource;
        private DocumentListNode FTarget;

        private void CopyClick(object ASender, EventArgs AArgs)
        {
            FTarget.CopyFromFiles(FSource);
        }

        private void MoveClick(object ASender, EventArgs AArgs)
        {
            FTarget.MoveFromFiles(FSource);
        }
    }
}