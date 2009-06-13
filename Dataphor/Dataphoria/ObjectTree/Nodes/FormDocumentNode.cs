using System;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.Designers;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class FormDocumentNode : DocumentNode
    {
        public FormDocumentNode(DocumentListNode AParent, string ADocumentName, string ADocumentType) : base(AParent, ADocumentName, ADocumentType) 
        {
            ImageIndex = 10;
            SelectedImageIndex = ImageIndex;
        }

        protected override ContextMenu GetContextMenu()
        {
            ContextMenu LMenu = base.GetContextMenu();
			
            LMenu.MenuItems.Add(2, new MenuItem(Strings.ObjectTree_CustomizeMenuText, new EventHandler(CustomizeClicked)));
            LMenu.MenuItems.Add(3, new MenuItem(Strings.ObjectTree_ShowMenuText, new EventHandler(StartClicked), Shortcut.F9));

            return LMenu;
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

        private void CustomizeClicked(object ASender, EventArgs AArgs)
        {
            Dataphor.Dataphoria.FormDesigner.CustomFormDesigner LDesigner = new Dataphor.Dataphoria.FormDesigner.CustomFormDesigner(Dataphoria, "DFDX");
            try
            {
                BOP.Ancestors LAncestors = new BOP.Ancestors();
                LAncestors.Add(GetDocumentExpression());
                LDesigner.New(LAncestors);
                ((IDesigner)LDesigner).Show();
            }
            catch
            {
                LDesigner.Dispose();
                throw;
            }
			
        }

        private void StartClicked(object ASender, EventArgs AArgs)
        {
            Frontend.Client.Windows.Session LSession = Dataphoria.GetLiveDesignableFrontendSession();
            try
            {
                LSession.SetLibrary(LibraryName);
                LSession.StartCallback(GetDocumentExpression(), null);
            }
            catch
            {
                LSession.Dispose();
                throw;
            }
        }
    }
}