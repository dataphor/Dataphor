using System;
using System.IO;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.Designers;

namespace Alphora.Dataphor.Dataphoria.ObjectTree.Nodes
{
    public class DocumentData : DataObject
    {
        public DocumentData(DocumentNode node)
        {
            _node = node;
        }

        private DocumentNode _node;
        public DocumentNode Node
        {
            get { return _node; }
        }

        public override object GetData(string format)
        {
            if (format == DataFormats.FileDrop)
            {
                // produce a file for copy move operation
                // TODO: figure out how to delete this file after the caller is done with it (if they don't move it)
                DocumentDesignBuffer buffer = new DocumentDesignBuffer(_node.Dataphoria, _node.LibraryName, _node.DocumentName);
                string fileName = String.Format("{0}{1}.{2}", Path.GetTempPath(), buffer.DocumentName, _node.DocumentType);
                using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    buffer.LoadData(stream);
                }
                return new string[] {fileName};
            }
            else
                return null;
        }

        public override object GetData(string format, bool autoConvert)
        {
            return GetData(format);
        }

        public override bool GetDataPresent(string format)
        {
            return format == DataFormats.FileDrop;
        }

        public override bool GetDataPresent(string format, bool autoConvert)
        {
            return GetDataPresent(format);
        }

        public override string[] GetFormats()
        {
            return new string[] {DataFormats.FileDrop};
        }

        public override string[] GetFormats(bool autoConvert)
        {
            return GetFormats();
        }
    }
}