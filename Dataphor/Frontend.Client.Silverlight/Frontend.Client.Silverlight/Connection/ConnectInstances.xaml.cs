using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;

using Alphora.Dataphor.DAE.Listener;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
    public partial class ConnectInstances : UserControl
    {
        public ConnectInstances(ConnectWorkItem workItem)
        {
			_workItem = workItem;
			DataContext = _workItem;
            InitializeComponent();
            
            // TODO: update button enabled
        }

		private ConnectWorkItem _workItem;
				
		private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (InstanceListBox.SelectedIndex >= 0)
			{
				_workItem.InstanceName = _workItem.Instances[InstanceListBox.SelectedIndex];
				_workItem.Status = ConnectStatus.Login;
			}
		}

		private void BackClicked(object sender, System.Windows.RoutedEventArgs e)
		{
        	_workItem.Back();
		}  
	}
}
