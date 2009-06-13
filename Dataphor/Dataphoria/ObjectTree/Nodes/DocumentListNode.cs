using System;
using System.IO;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.ObjectTree.Nodes.Menus;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DocumentListNode : BrowseNode
    {
        public DocumentListNode(string ALibraryName)
        {
            Text = "Documents";
            ImageIndex = 7;
            SelectedImageIndex = ImageIndex;

            FLibraryName = ALibraryName;
        }

        private string FLibraryName;
        public string LibraryName { get { return FLibraryName; } }

        protected override string GetChildExpression()
        {
            return ".Frontend.Documents where Library_Name = ALibraryName over { Name, Type_ID } rename Main";
        }
		
        protected override Alphora.Dataphor.DAE.Runtime.DataParams GetParams()
        {
            DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
            LParams.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "ALibraryName", LibraryName));
            return LParams;
        }

        protected override BaseNode CreateChildNode(DAE.Runtime.Data.Row ARow)
        {
            string LName = ARow["Main.Name"].AsString;
            string LType = ARow["Type_ID"].AsString;
            switch (LType)
            {
                case "d4" : return new D4DocumentNode(this, LName, LType);
                case "dfd" : 
                case "dfdx" : return new FormDocumentNode(this, LName, LType);
                default : return new DocumentNode(this, LName, LType);
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
            ContextMenu LMenu = base.GetContextMenu();
			
            FAddSeparator.Visible = false;
            FAddMenuItem.Visible = false;
			
            return LMenu;
        }			
		
        protected override void InternalRefresh()
        {
            Dataphoria.ExecuteScript(String.Format(".Frontend.RefreshDocuments('{0}');", LibraryName));
            base.InternalRefresh();
        }

        private string FindUniqueDocumentName(string ALibraryName, string ADocumentName)
        {
            if (!Dataphoria.DocumentExists(ALibraryName, ADocumentName))
                return ADocumentName;

            int LCount = 1;

            int LNumIndex = ADocumentName.Length - 1;
            while ((LNumIndex >= 0) && Char.IsNumber(ADocumentName, LNumIndex))
                LNumIndex--;
            if (LNumIndex < (ADocumentName.Length - 1))
            {
                LCount = Int32.Parse(ADocumentName.Substring(LNumIndex + 1));
                ADocumentName = ADocumentName.Substring(0, LNumIndex + 1);
            }

            string LName;
            do
            {
                LName = ADocumentName + LCount.ToString();
                LCount++;
            }  while (Dataphoria.DocumentExists(ALibraryName, LName));

            return LName;
        }

        public void CopyFromDocument(string ALibraryName, string ADocumentName)
        {
            string LNewDocumentName = FindUniqueDocumentName(LibraryName, ADocumentName);
            Dataphoria.ExecuteScript
                (
                String.Format
                    (
                    ".Frontend.CopyDocument('{0}', '{1}', '{2}', '{3}');",
                    DAE.Schema.Object.EnsureRooted(ALibraryName),
                    DAE.Schema.Object.EnsureRooted(ADocumentName),
                    DAE.Schema.Object.EnsureRooted(LibraryName),
                    LNewDocumentName
                    )
                );
            ReconcileChildren();
        }

        public void MoveFromDocument(string ALibraryName, string ADocumentName)
        {
            Dataphoria.CheckDocumentOverwrite(LibraryName, ADocumentName);
            Dataphoria.ExecuteScript
                (
                String.Format
                    (
                    ".Frontend.MoveDocument('{0}', '{1}', '{2}', '{3}');",
                    DAE.Schema.Object.EnsureRooted(ALibraryName),
                    DAE.Schema.Object.EnsureRooted(ADocumentName),
                    DAE.Schema.Object.EnsureRooted(LibraryName),
                    ADocumentName
                    )
                );
            ReconcileChildren();
        }

        public void CopyFromFiles(Array AFileList)
        {
            FileStream LStream;
            DocumentDesignBuffer LBuffer;
            foreach (string LFileName in AFileList)
            {
                LBuffer = new DocumentDesignBuffer(Dataphoria, LibraryName, Path.GetFileNameWithoutExtension(LFileName));
                Dataphoria.CheckDocumentOverwrite(LBuffer.LibraryName, LBuffer.DocumentName);
                LBuffer.DocumentType = Program.DocumentTypeFromFileName(LFileName);
                LStream = new FileStream(LFileName, FileMode.Open, FileAccess.Read);
                try
                {
                    LBuffer.SaveBinaryData(LStream);
                }
                finally
                {
                    LStream.Close();
                }
            }
            ReconcileChildren();
        }

        public void MoveFromFiles(Array AFileList)
        {
            CopyFromFiles(AFileList);
            foreach (string LFileName in AFileList)
                File.Delete(LFileName);
        }

        public override void DragDrop(DragEventArgs AArgs)
        {
            DocumentData LSource = AArgs.Data as DocumentData;
            if (LSource != null)
            {
                switch (AArgs.Effect)
                {
                    case DragDropEffects.Copy | DragDropEffects.Move :
                        new DocumentListDropMenu(LSource, this).Show(TreeView, TreeView.PointToClient(System.Windows.Forms.Control.MousePosition));
                        break;
                    case DragDropEffects.Copy :
                        CopyFromDocument(LSource.Node.LibraryName, LSource.Node.DocumentName);
                        break;
                    case DragDropEffects.Move :
                        MoveFromDocument(LSource.Node.LibraryName, LSource.Node.DocumentName);
                        ((DocumentListNode)LSource.Node.Parent).ReconcileChildren();
                        break;
                }
            }
            else if (AArgs.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Array LFileList = (Array)AArgs.Data.GetData(DataFormats.FileDrop);
                switch (AArgs.Effect)
                {
                    case DragDropEffects.Copy | DragDropEffects.Move :
                        new DocumentListFileDropMenu(LFileList, this).Show(TreeView, TreeView.PointToClient(System.Windows.Forms.Control.MousePosition));
                        break;
                    case DragDropEffects.Copy :
                        CopyFromFiles(LFileList);
                        break;
                    case DragDropEffects.Move :
                        MoveFromFiles(LFileList);
                        break;
                }
            }
        }

        public override void DragOver(DragEventArgs AArgs)
        {
            base.DragOver(AArgs);
            DocumentData LSource = AArgs.Data as DocumentData;
            if (LSource != null)
            {
                if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
                {
                    if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                        AArgs.Effect = DragDropEffects.Copy;
                    else
                        if (LSource.Node.LibraryName != LibraryName)
                            AArgs.Effect = DragDropEffects.Move;
                }
                else if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Right)
                    AArgs.Effect = DragDropEffects.Copy | DragDropEffects.Move;
            }
            else if (AArgs.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (System.Windows.Forms.Control.MouseButtons == MouseButtons.Left)
                    if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
                        AArgs.Effect = DragDropEffects.Move;
                    else
                        AArgs.Effect = DragDropEffects.Copy;
                else
                    AArgs.Effect = DragDropEffects.Copy | DragDropEffects.Move;
            }
            if (AArgs.Effect != DragDropEffects.None)
                TreeView.SelectedNode = this;
        }
    }
}