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
			var LConnectItem = new ConnectWorkItem(RootContent);
			
			#if DEBUG
			LConnectItem.HostName = "localhost";
			LConnectItem.InstanceName = "Draft";
			LConnectItem.Status = ConnectStatus.Login;
			LConnectItem.BeginLogin();
			#endif
			
			var LConnectForm = new ConnectForm(LConnectItem);
			RootContent.Content = LConnectForm;
			LConnectForm.Complete +=
				delegate
				{
					LConnectItem.Session.OnComplete += SessionComplete;
				};
		}
		
		private void SessionComplete(object sender, EventArgs e)
		{
 			Session.DispatcherInvoke((System.Action)(() => { StartConnection(); }));
		}
	}
}
