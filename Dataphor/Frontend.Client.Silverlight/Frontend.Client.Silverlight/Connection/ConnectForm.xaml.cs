using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public partial class ConnectForm : UserControl
	{
		public ConnectForm(ConnectWorkItem workItem)
		{
			InitializeComponent();
			
			_workItem = workItem;
			_workItem.PropertyChanged += new PropertyChangedEventHandler(WorkItemPropertyChanged);
			DataContext = workItem;
			SetContent();
		}

		private void WorkItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Status")
				SetContent();
		}

		private void SetContent()
		{
			switch (_workItem.Status)
			{
				case ConnectStatus.SelectHost : ConnectContent.Content = new ConnectHost(_workItem); break;
				case ConnectStatus.LoadingInstances : ConnectContent.Content = "Loading instances..."; break;
				case ConnectStatus.SelectInstance : ConnectContent.Content = new ConnectInstances(_workItem); break;
				case ConnectStatus.Login : ConnectContent.Content = new ConnectLogin(_workItem); break;
				case ConnectStatus.Connecting : ConnectContent.Content = "Connecting..."; break;
				case ConnectStatus.SelectApplication : ConnectContent.Content = new ConnectApplication(_workItem); break;
				case ConnectStatus.StartingApplication : ConnectContent.Content = "Starting session..."; break;
				case ConnectStatus.Complete : OnComplete(); break;
			}
		}
		
		private ConnectWorkItem _workItem;
		
		public event EventHandler Complete;
		
		private void OnComplete()
		{
			if (Complete != null)
				Complete(this, EventArgs.Empty);
		}
	}
}
