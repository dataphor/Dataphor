using System;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
    public class PaletteItem : ListViewItem
    {
        private string _className;
        private string _description = String.Empty;

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string ClassName
        {
            get { return _className; }
            set { _className = value; }
        }
    }
}