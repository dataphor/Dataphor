using System;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	public class NonMouseWheelScrollViewer : ScrollViewer
	{
		protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
		{
			var handled = e.Handled;
			base.OnMouseWheel(e);
			e.Handled = handled;	// Don't handle the mouse wheel
		}
	}
}
