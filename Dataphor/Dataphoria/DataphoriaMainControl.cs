using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.ObjectTree;
using Alphora.Dataphor.Frontend.Client.Windows;
using Darwen.Windows.Forms.Controls.Docking;

namespace Alphora.Dataphor.Dataphoria
{

    public partial class DataphoriaMainControl : Darwen.Windows.Forms.Controls.Docking.DockingManagerControl
    {

        private DataTree FExplorer;
        private ErrorListView FErrorListView;



        public DataphoriaMainControl()
        {
            InitializeComponent();

            FExplorer = new DataTree();
            FErrorListView = new ErrorListView();

            IDockingPanel LLeftPanel = this.Panels[DockingType.Left].InsertPanel(0);
            LLeftPanel.DockedControls.Add("Explorer", FExplorer);

            IDockingPanel LBottomPanel = this.Panels[DockingType.Bottom].InsertPanel(0);
            LBottomPanel.DockedControls.Add("Error List", FErrorListView);
            
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
