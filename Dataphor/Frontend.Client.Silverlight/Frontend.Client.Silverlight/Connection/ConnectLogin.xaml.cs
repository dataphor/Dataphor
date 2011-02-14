using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Threading;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
    public partial class ConnectLogin : UserControl
    {
        public ConnectLogin(ConnectWorkItem workItem)
        {
			_workItem = workItem;
			_workItem.PropertyChanged += new PropertyChangedEventHandler(WorkItemPropertyChanged);
			DataContext = workItem;
            InitializeComponent();
            UpdateLoginEnabled();
        }

		private void WorkItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "UserName")
				UpdateLoginEnabled();
		}
        
        private ConnectWorkItem _workItem;

        private void Button_Click(object sender, System.Windows.RoutedEventArgs args)
        {
			BindingUtility.UpdateTextBoxBindingSources(this);
			_workItem.BeginLogin();
        }
        
		private void UpdateLoginEnabled()
		{
			LoginButton.IsEnabled = !String.IsNullOrEmpty(_workItem.HostName) && !String.IsNullOrEmpty(_workItem.InstanceName) 
				&& !String.IsNullOrEmpty(_workItem.UserName);
		}

		private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			// Update with every change (in WPF, this could be accomplished through the UpdateSourceTrigger enum)
			UserNameBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
		}

		private void BackClicked(object sender, System.Windows.RoutedEventArgs e)
		{
        	_workItem.Back();
		}
    }
}
