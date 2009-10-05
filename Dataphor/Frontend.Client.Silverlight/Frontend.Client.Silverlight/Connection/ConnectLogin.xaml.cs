using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Threading;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
    public partial class ConnectLogin : UserControl
    {
        public ConnectLogin(ConnectWorkItem AWorkItem)
        {
			FWorkItem = AWorkItem;
			FWorkItem.PropertyChanged += new PropertyChangedEventHandler(WorkItemPropertyChanged);
			DataContext = AWorkItem;
            InitializeComponent();
            UpdateLoginEnabled();
        }

		private void WorkItemPropertyChanged(object ASender, PropertyChangedEventArgs AArgs)
		{
			if (AArgs.PropertyName == "UserName")
				UpdateLoginEnabled();
		}
        
        private ConnectWorkItem FWorkItem;

        private void Button_Click(object ASender, System.Windows.RoutedEventArgs AArgs)
        {
			BindingUtility.UpdateTextBoxBindingSources(this);
			FWorkItem.BeginLogin();
        }
        
		private void UpdateLoginEnabled()
		{
			LoginButton.IsEnabled = !String.IsNullOrEmpty(FWorkItem.HostName) && !String.IsNullOrEmpty(FWorkItem.InstanceName) 
				&& !String.IsNullOrEmpty(FWorkItem.UserName);
		}

		private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			// Update with every change (in WPF, this could be accomplished through the UpdateSourceTrigger enum)
			UserNameBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
		}
    }
}
