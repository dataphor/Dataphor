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
		public ConnectApplication(ConnectWorkItem workItem)
		{
			InitializeComponent();
			
			_workItem = workItem;
			DataContext = workItem;
		}
		
		private ConnectWorkItem _workItem;

		private void StartButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ApplicationsGrid.SelectedItem != null)
				_workItem.ApplicationID = (string)((object[])ApplicationsGrid.SelectedItem)[0];
			_workItem.BeginStartApplication();
		}

		private void BackClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			_workItem.Back();
		}

		private void ApplicationsGridLoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs args)
		{
			var column = ApplicationsGrid.Columns[0];
			var cell = column.GetCellContent(args.Row).Parent as DataGridCell;
			if (cell != null)
			    cell.DataContext = ((object[])args.Row.DataContext)[1];
		}
	}
}
