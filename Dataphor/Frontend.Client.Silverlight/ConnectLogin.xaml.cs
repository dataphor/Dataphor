using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Frontend.Client.Silverlight
{
    public partial class ConnectLogin : Page
    {
        public ConnectLogin()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	
        }
		
		public event PropertyChangedEventHandler PropertyChanged;
	
		public void NotifyPropertyChanged(string AName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(AName));
		}

		private string FHostName;
		
		public string HostName
		{
			get { return FHostName; }
			set
			{
				if (FHostName != value)
				{
					FHostName = value;
					UpdateLoginEnabled();
					NotifyPropertyChanged("HostName");
				}
			}
		}
		
		private string FInstanceName;
		
		public string InstanceName
		{
			get { return FInstanceName; }
			set
			{
				if (FInstanceName != value)
				{
					FInstanceName = value;
					UpdateLoginEnabled();
					NotifyPropertyChanged("InstanceName");
				}
			}
		}

		private void UserNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			UpdateLoginEnabled();
		}

		private void PasswordTextBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
		{
			UpdateLoginEnabled();
		}

		private void UpdateLoginEnabled()
		{
			LoginButton.IsEnabled = !String.IsNullOrEmpty(HostName) && !String.IsNullOrEmpty(InstanceName) 
				&& !String.IsNullOrEmpty(UserNameTextBox.Text) && !String.IsNullOrEmpty(PasswordTextBox.Password);
		}
    }
}
