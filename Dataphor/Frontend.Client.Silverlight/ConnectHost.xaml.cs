using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;

using Alphora.Dataphor.DAE.Listener;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
    public partial class ConnectHost : Page
    {
        public ConnectHost()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			NavigationService.Navigate
			(
				new Uri
				(
					"ConnectInstances.xaml?HostName=" + Uri.EscapeUriString(HostNameTextBox.Text), 
					UriKind.Relative
				)
			);
        }
    }
}
