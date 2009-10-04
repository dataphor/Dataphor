using System;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public partial class Main : UserControl
	{
		public Main()
		{
			InitializeComponent();
			
			var LConnectItem = new ConnectWorkItem();
			var LConnectForm = new ConnectForm(LConnectItem);
			RootContent.Content = LConnectForm;
			LConnectForm.Complete += ConnectionComplete;
		}
		
		private void ConnectionComplete(object ASender, EventArgs AArgs)
		{
		}
	}
}
