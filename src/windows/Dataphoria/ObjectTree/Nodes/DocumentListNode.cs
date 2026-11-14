using System;
using System.IO;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.ObjectTree.Nodes.Menus;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DocumentListNode : BrowseNode
    {
        public DocumentListNode(string libraryName)
        {
            Text = "Documents";
            ImageIndex = 7;
            SelectedImageIndex = ImageIndex;

            _libraryName = libraryName;
        }

        private string _libraryName;
        public string LibraryName { get { return _libraryName; } }

        protected override string GetChildExpression()
        {
            return ".Frontend.Documents where Library_Name = ALibraryName over { Name, Type_ID } rename Main";
        }
		
        protected override Alphora.Dataphor.DAE.Runtime.DataParams GetParams()
        {
            DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
            paramsValue.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "ALibraryName", LibraryName));
            return paramsValue;
        }

        protected override BaseNode CreateChildNode(DAE.Runtime.Data.IRow row)
        {
            string name = (string)row["Main.Name"];
            string type = (string)row["Type_ID"];
            switch (type)
            {
                case "d4" : return new D4DocumentNode(this, name, type);
                case "dfd" : 
                case "dfdx" : return new FormDocumentNode(this, name, type);
                default : return new DocumentNode(this, name, type);
            }
        }
		
        protected override string AddDocument()
        {
            return 
                String.Format
                    (
                    "Frontend.Derive('.Frontend.Documents adorn {{ Library_Name {{ default ''{0}'' }} }} tags {{ Frontend.Title = ''Document'' }}', 'Add', 'Main.Library_Name', 'Main.Library_Name')",
                    LibraryName
                    );
        }
		
        protected override ContextMenu GetContextMenu()
        {
            ContextMenu menu = base.GetContextMenu();
			
            _addSeparator.Visible = false;
            _addMenuItem.Visible = false;
			
            return menu;
        }			
		
        protected override void InternalRefresh()
        {
            Dataphoria.ExecuteScript(String.Format(".Frontend.RefreshDocuments('{0}');", LibraryName));
            base.InternalRefresh();
        }

        private string FindUniqueDocumentName(string libraryName, string documentName)
        {
            if (!Dataphoria.DocumentExists(libraryName, documentName))
                return documentName;

            int count = 1;

            int numIndex = documentName.Length - 1;
            while ((numIndex >= 0) && Char.IsNumber(documentName, numIndex))
                numIndex--;
            if (numIndex < (documentName.Length - 1))
            {
                count = Int32.Parse(documentName.Substring(numIndex + 1));
                documentName = documentName.Substring(0, numIndex + 1);
            }

            string name;
            do
            {
                name = documentName + count.ToString();
                count++;
            }  while (Dataphoria.DocumentExists(libraryName, name));

            return name;
        }

        public void CopyFromDocument(string libraryName, string documentName)
        {
            string newDocumentName = FindUniqueDocumentName(LibraryName, documentName);
            Dataphoria.ExecuteScript
                (
                String.Format
                    (
                    ".Frontend.CopyDocument('{0}', '{1}', '{2}', '{3}');",
                    DAE.Schema.Object.EnsureRooted(libraryName),
                    DAE.Schema.Object.EnsureRooted(documentName),
                    DAE.Schema.Object.EnsureRooted(LibraryName),
                    newDocumentName
                    )
                );
            ReconcileChildren();
        }

        public void MoveFromDocument(string libraryName, string documentName)
        {
            Dataphoria.CheckDocumentOverwrite(LibraryName, documentName);
            Dataphoria.ExecuteScript
                (
                String.Format
                    (
                    ".Frontend.MoveDocument('{0}', '{1}', '{2}', '{3}');",
                    DAE.Schema.Object.EnsureRooted(libraryName),
                    DAE.Schema.Object.EnsureRooted(documentName),
                    DAE.Schema.Object.EnsureRooted(LibraryName),
                    documentName
                    )
                );
            ReconcileChildren();
        }

        public void CopyFromFiles(Array fileList)
        {
            FileStream stream;
            DocumentDesignBuffer buffer;
            foreach (string fileName in fileList)
            {
                buffer = new DocumentDesignBuffer(Dataphoria, LibraryName, Path.GetFileNameWithoutExtension(fileName));
                Dataphoria.CheckDocumentOverwrite(buffer.LibraryName, buffer.DocumentName);
                buffer.DocumentType = Program.DocumentTypeFromFileName(fileName);
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                try
                {
                    buffer.SaveBinaryData(stream);
                }
                finally
                {
                    stream.Close();
                }
            }
            ReconcileChildren();
        }

        public void MoveFromFiles(Array fileList)
        {
            CopyFromFiles(fileList);
            foreach (string fileName in fileList)
                File.Delete(fileName);
        }

        public override void DragDrop(DragEventArgs args)
        {
            DocumentData source = args.Data as DocumentData;
            if (source != null)
            {
                switch (args.Effect)
                {
                    case DragDropEffects.Copy | DragDropEffects.Move :
                        new DocumentListDropMenu(source, this).Show(TreeView, TreeView.PointToClient(System.Windows.Forms.Control.MousePosition));
                        break;
                    case DragDropEffects.Copy :
                        CopyFromDocument(source.Node.LibraryName, source.Node.DocumentName);
                        break;
                    case DragDropEffects.Move :
                        MoveFromDocument(source.Node.LibraryName, source.Node.DocumentName);
                        ((DocumentListNode)source.Node.Parent).ReconcileChildren();
                        break;
                }
            }
            else if (args.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Array fileList = (Array)args.Data.GetData(DataFormats.FileDrop);
                switch (args.Effect)
                {
                    case DragDropEffects.Copy | DragDropEffects.Move :
                        new DocumentListFileDropMenu(fileList, this).Show(TreeView, TreeView.PointToClient(System.Windows.Forms.Control.MousePosition));
                        break;
                    case DragDropEffects.Copy :
                        CopyFromFiles(fileList);
                        break;
                    case DragDropEffects.Move :
                        MoveFromFiles(fileList);
                        break;
                }
            }
        }

        public override void DragOver(DragEventArgs args)
        {
            base.DragOver(args);
            DocumentData source = args.Data as DocumentData;
            if (source != null)
            {
                if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
                {
                    if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                        args.Effect = DragDropEffects.Copy;
                    else
                        if (source.Node.LibraryName != LibraryName)
                            args.Effect = DragDropEffects.Move;
                }
                else if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Right)
                    args.Effect = DragDropEffects.Copy | DragDropEffects.Move;
            }
            else if (args.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
                    if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
                        args.Effect = DragDropEffects.Move;
                    else
                        args.Effect = DragDropEffects.Copy;
                else
                    args.Effect = DragDropEffects.Copy | DragDropEffects.Move;
            }
            if (args.Effect != DragDropEffects.None)
                TreeView.SelectedNode = this;
        }
    }
}