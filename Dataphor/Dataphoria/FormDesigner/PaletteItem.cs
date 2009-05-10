using System;
using Syncfusion.Windows.Forms.Tools;

namespace Alphora.Dataphor.Dataphoria.FormDesigner
{
    public class PaletteItem : GroupViewItem
    {
        private string FClassName;
        private string FDescription = String.Empty;

        public string Description
        {
            get { return FDescription; }
            set { FDescription = value; }
        }

        public string ClassName
        {
            get { return FClassName; }
            set { FClassName = value; }
        }
    }
}