using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Navigation;

using Alphora.Dataphor.DAE.Listener;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
    public partial class ConnectInstances : UserControl
    {
        public ConnectInstances(ConnectWorkItem AWorkItem)
        {
			FWorkItem = AWorkItem;
			DataContext = FWorkItem;
            InitializeComponent();
            
            // TODO: update button enabled
        }

		private ConnectWorkItem FWorkItem;
				
		private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (InstanceListBox.SelectedIndex >= 0)
			{
				FWorkItem.InstanceName = FWorkItem.Instances[InstanceListBox.SelectedIndex];
				FWorkItem.Status = ConnectStatus.Login;
			}
		}

		private void BackClicked(object sender, System.Windows.RoutedEventArgs e)
		{
        	FWorkItem.Back();
		}  
	}
}
