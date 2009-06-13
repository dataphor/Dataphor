using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class D4DocumentNode : DocumentNode
    {
        public D4DocumentNode(DocumentListNode AParent, string ADocumentName, string ADocumentType) : base(AParent, ADocumentName, ADocumentType) 
        {
            ImageIndex = 9;
            SelectedImageIndex = ImageIndex;
        }

        protected override ContextMenu GetContextMenu()
        {
            ContextMenu LMenu = base.GetContextMenu();
			
            LMenu.MenuItems.Add(2, new MenuItem(Strings.ObjectTree_ExecuteMenuText, new EventHandler(ExecuteClicked), Shortcut.F9));

            return LMenu;
        }

        private void ExecuteClicked(object ASender, EventArgs AArgs)
        {
            using 
                (
                DAE.Runtime.Data.DataValue LScript = 
                    Dataphoria.FrontendSession.Pipe.RequestDocument
                        (
                        String.Format
                            (
                            ".Frontend.Load('{0}', '{1}')", 
                            DAE.Schema.Object.EnsureRooted(LibraryName), 
                            DAE.Schema.Object.EnsureRooted(DocumentName)
                            )
                        )
                )
            {
                Dataphoria.ExecuteScript(LScript.AsString);
            }
        }
    }
}