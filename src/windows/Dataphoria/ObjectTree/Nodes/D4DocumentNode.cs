using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class D4DocumentNode : DocumentNode
    {
        public D4DocumentNode(DocumentListNode parent, string documentName, string documentType) : base(parent, documentName, documentType) 
        {
            ImageIndex = 9;
            SelectedImageIndex = ImageIndex;
        }

        protected override ContextMenu GetContextMenu()
        {
            ContextMenu menu = base.GetContextMenu();
			
            menu.MenuItems.Add(2, new MenuItem(Strings.ObjectTree_ExecuteMenuText, new EventHandler(ExecuteClicked), Shortcut.F9));

            return menu;
        }

        private void ExecuteClicked(object sender, EventArgs args)
        {
            using 
			(
				DAE.Runtime.Data.IScalar script = 
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
                Dataphoria.ExecuteScript(script.AsString);
            }
        }
    }
}