using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public partial class ConnectForm : UserControl
	{
		public ConnectForm(ConnectWorkItem AWorkItem)
		{
			InitializeComponent();
			
			FWorkItem = AWorkItem;
			FWorkItem.PropertyChanged += new PropertyChangedEventHandler(WorkItemPropertyChanged);
			DataContext = AWorkItem;
			SetContent();
		}

		private void WorkItemPropertyChanged(object ASender, PropertyChangedEventArgs AArgs)
		{
			if (AArgs.PropertyName == "Status")
				SetContent();
		}

		private void SetContent()
		{
			switch (FWorkItem.Status)
			{
				case ConnectStatus.SelectHost : ConnectContent.Content = new ConnectHost(FWorkItem); break;
				case ConnectStatus.LoadingInstances : ConnectContent.Content = "Loading instances..."; break;
				case ConnectStatus.Complete : OnComplete(); break;
			}
		}
		
		private ConnectWorkItem FWorkItem;
		
		public event EventHandler Complete;
		
		private void OnComplete()
		{
			if (Complete != null)
				Complete(this, EventArgs.Empty);
		}
	}
}
