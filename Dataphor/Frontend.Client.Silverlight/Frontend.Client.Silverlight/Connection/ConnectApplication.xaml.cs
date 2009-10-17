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
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public partial class ConnectApplication : UserControl
	{
		public ConnectApplication(ConnectWorkItem AWorkItem)
		{
			InitializeComponent();
			
			FWorkItem = AWorkItem;
			DataContext = AWorkItem;
		}
		
		private ConnectWorkItem FWorkItem;

		private void StartButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ApplicationsGrid.SelectedItem != null)
				FWorkItem.ApplicationID = (string)((object[])ApplicationsGrid.SelectedItem)[0];
			FWorkItem.BeginStartApplication();
		}

		private void BackClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			FWorkItem.Back();
		}

		private void ApplicationsGridLoadingRow(object ASender, System.Windows.Controls.DataGridRowEventArgs AArgs)
		{
			var LColumn = ApplicationsGrid.Columns[0];
			var LCell = LColumn.GetCellContent(AArgs.Row).Parent as DataGridCell;
			if (LCell != null)
			    LCell.DataContext = ((object[])AArgs.Row.DataContext)[1];
		}
	}
}
