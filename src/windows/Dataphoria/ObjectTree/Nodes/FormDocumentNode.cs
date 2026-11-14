using System;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.Designers;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class FormDocumentNode : DocumentNode
    {
        public FormDocumentNode(DocumentListNode parent, string documentName, string documentType) : base(parent, documentName, documentType) 
        {
            ImageIndex = 10;
            SelectedImageIndex = ImageIndex;
        }

        protected override ContextMenu GetContextMenu()
        {
            ContextMenu menu = base.GetContextMenu();
			
            menu.MenuItems.Add(2, new MenuItem(Strings.ObjectTree_CustomizeMenuText, new EventHandler(CustomizeClicked)));
            menu.MenuItems.Add(3, new MenuItem(Strings.ObjectTree_ShowMenuText, new EventHandler(StartClicked), Shortcut.F9));

            return menu;
        }

        private string GetDocumentExpression()
        {
            return 
                String.Format
                    (
                    ".Frontend.Form('{0}', '{1}')",
                    DAE.Schema.Object.EnsureRooted(LibraryName),
                    DAE.Schema.Object.EnsureRooted(DocumentName)
                    );
        }

        private void CustomizeClicked(object sender, EventArgs args)
        {
            Dataphor.Dataphoria.FormDesigner.CustomFormDesigner designer = new Dataphor.Dataphoria.FormDesigner.CustomFormDesigner(Dataphoria, "DFDX");
            try
            {
                BOP.Ancestors ancestors = new BOP.Ancestors();
                ancestors.Add(GetDocumentExpression());
                designer.New(ancestors);
                ((IDesigner)designer).Show();
            }
            catch
            {
                designer.Dispose();
                throw;
            }
			
        }

        private void StartClicked(object sender, EventArgs args)
        {
            Frontend.Client.Windows.Session session = Dataphoria.GetLiveDesignableFrontendSession();
            try
            {
                session.SetLibrary(LibraryName);
                session.StartCallback(GetDocumentExpression(), null);
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }
    }
}