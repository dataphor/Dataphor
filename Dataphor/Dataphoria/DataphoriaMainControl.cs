using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.Dataphoria.ObjectTree;
using Alphora.Dataphor.Frontend.Client.Windows;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria
{    
    
    public partial class DataphoriaMainControl : UserControl
    {

        private DataTree FExplorer;
        private ErrorListView FErrorListView;

        private DockContent FDockContentFExplorer;
        private DockContent FDockContentErrorListView;


        public DataphoriaMainControl()
        {
            InitializeComponent();

            FExplorer = new DataTree();
            FErrorListView = new ErrorListView();

            FDockContentFExplorer= new DockContent();

            FExplorer.Dock = DockStyle.Fill;
            FDockContentFExplorer.Controls.Add(FExplorer);
            FDockContentFExplorer.TabText = "Dataphoria Explorer";
            FDockContentFExplorer.Text = "DataTree Explorer - Dataphoria";
            FDockContentFExplorer.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockLeft;


            FDockContentErrorListView = new DockContent();

            FErrorListView.Dock = DockStyle.Fill;
            FDockContentErrorListView.Controls.Add(FErrorListView);
            FDockContentErrorListView.TabText = "Dataphoria Error List";
            FDockContentErrorListView.Text = "Error List - Dataphoria";
            FDockContentErrorListView.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide;
            
            FDockContentFExplorer.Show(this.FDockPanel);
            FDockContentErrorListView.Show(this.FDockPanel);
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
    }
}
