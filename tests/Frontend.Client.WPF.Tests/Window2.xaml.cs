using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Frontend.Client.WPF.Tests
{
	/// <summary>
	/// Interaction logic for Window2.xaml
	/// </summary>
	public partial class Window2 : Window
	{
		public Window2()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
		}
		
		private void TestClick(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("TestClicked");
		}
		
	}
}