using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.ObjectTree;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria
{    
    
    public partial class DataphoriaMainControl : UserControl
    {

        private DataTree FExplorer;
        private ErrorListView FErrorListView;



        public DataphoriaMainControl()
        {
            InitializeComponent();

            FExplorer = new DataTree();
            FErrorListView = new ErrorListView();

        }


        public DataTree Explorer {
            get {
                return FExplorer;
            }
        }

        public ErrorListView ErrorListView {
            get
            {
                return FErrorListView;
            }
        }


        internal void AttachForm(Form AForm)
        {
            throw new NotImplementedException("Not Implemented");
        }
    }
}
