using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

using Alphora.Dataphor.DAE.Listener;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
    public partial class ConnectHost : UserControl
    {
        public ConnectHost(ConnectWorkItem AWorkItem)
        {
            DataContext = AWorkItem;
            FWorkItem = AWorkItem;
            InitializeComponent();
        }
        
        private ConnectWorkItem FWorkItem;

        private void ConnectClicked(object sender, System.Windows.RoutedEventArgs e)
        {
			BindingUtility.UpdateTextBoxBindingSources(this);
			FWorkItem.BeginLoadInstances();
        }
    }
}
