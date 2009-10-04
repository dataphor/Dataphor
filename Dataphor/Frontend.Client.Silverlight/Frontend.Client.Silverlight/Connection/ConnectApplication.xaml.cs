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
	}
}
