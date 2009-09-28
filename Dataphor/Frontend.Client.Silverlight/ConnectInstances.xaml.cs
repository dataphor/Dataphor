using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Navigation;

using Alphora.Dataphor.DAE.Listener;

namespace Frontend.Client.Silverlight
{
    public partial class ConnectInstances : Page, INotifyPropertyChanged
    {
        public ConnectInstances()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
			string LHostName;
			if (NavigationContext.QueryString.TryGetValue("HostName", out LHostName))
				HostName = LHostName;
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
					
					// Asynchronously load the instances
					new Thread
					(
						new ThreadStart
						(
							delegate
							{
								var LInstances = ListenerFactory.EnumerateInstances(value);
								Dispatcher.BeginInvoke
								(
									delegate 
									{
										if (FHostName == value)
										{
											if (LInstances.Length == 0)
												NavigationService.Navigate(new Uri("ConnectHost.xaml"));
											else
												Instances = LInstances;
										}
									}
								);
							}
						)
					).Start();
				}
			}
		}
				
		private string[] FInstances;
		
		public string[] Instances
		{
			get { return FInstances; }
			set 
			{ 
				if (FInstances != value)
				{
					FInstances = value; 
					NotifyPropertyChanged("Instances");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	
		public void NotifyPropertyChanged(string AName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(AName));
		}

		private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (InstanceListBox.SelectedItem != null)
				NavigationService.Navigate
				(
					new Uri
					(
						"ConnectLogin.xaml?HostName=" + Uri.EscapeUriString(FHostName) 
							+ "&InstanceName=" + Uri.EscapeUriString((string)InstanceListBox.SelectedItem)
					)
				);
		}  
	}
}
