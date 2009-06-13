using System;
using System.IO;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.Designers;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DocumentData : DataObject
    {
        public DocumentData(DocumentNode ANode)
        {
            FNode = ANode;
        }

        private DocumentNode FNode;
        public DocumentNode Node
        {
            get { return FNode; }
        }

        public override object GetData(string AFormat)
        {
            if (AFormat == DataFormats.FileDrop)
            {
                // produce a file for copy move operation
                // TODO: figure out how to delete this file after the caller is done with it (if they don't move it)
                DocumentDesignBuffer LBuffer = new DocumentDesignBuffer(FNode.Dataphoria, FNode.LibraryName, FNode.DocumentName);
                string LFileName = String.Format("{0}{1}.{2}", Path.GetTempPath(), LBuffer.DocumentName, FNode.DocumentType);
                using (FileStream LStream = new FileStream(LFileName, FileMode.Create, FileAccess.Write))
                {
                    LBuffer.LoadData(LStream);
                }
                return new string[] {LFileName};
            }
            else
                return null;
        }

        public override object GetData(string AFormat, bool AAutoConvert)
        {
            return GetData(AFormat);
        }

        public override bool GetDataPresent(string AFormat)
        {
            return AFormat == DataFormats.FileDrop;
        }

        public override bool GetDataPresent(string AFormat, bool AAutoConvert)
        {
            return GetDataPresent(AFormat);
        }

        public override string[] GetFormats()
        {
            return new string[] {DataFormats.FileDrop};
        }

        public override string[] GetFormats(bool AAutoConvert)
        {
            return GetFormats();
        }
    }
}