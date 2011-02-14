using System;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public partial class Main : UserControl
	{
		public Main()
		{
			InitializeComponent();

			StartConnection();
		}

		private void StartConnection()
		{
			var connectItem = new ConnectWorkItem(RootContent);
			
			#if DEBUG
			connectItem.HostName = "localhost";
			connectItem.InstanceName = "Draft";
			connectItem.Status = ConnectStatus.Login;
			connectItem.BeginLogin();
			#endif
			
			var connectForm = new ConnectForm(connectItem);
			RootContent.Content = connectForm;
			connectForm.Complete +=
				delegate
				{
					connectItem.Session.OnComplete += SessionComplete;
				};
		}
		
		private void SessionComplete(object sender, EventArgs e)
		{
 			Session.DispatcherInvoke((System.Action)(() => { StartConnection(); }));
		}
	}
}
