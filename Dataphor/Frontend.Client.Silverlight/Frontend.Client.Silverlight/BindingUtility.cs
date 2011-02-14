using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public static class BindingUtility
	{
		/// <summary> Updates the Text binding associated with any TextBox controls found in the visual 
		/// tree rooted by the given object. </summary>
		/// <remarks> Because TextBox controls do not update their binding until focus exits, is is 
		/// sometimes necessary to force binding updates before acting upon the data bound to by the form. </remarks>
		public static void UpdateTextBoxBindingSources(DependencyObject objectValue)
		{
			var count = VisualTreeHelper.GetChildrenCount(objectValue);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(objectValue, i);
				var textBox = child as TextBox;
				if (textBox != null)
				{
					var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
					if (bindingExpression != null)
						bindingExpression.UpdateSource();
				}
				else
					UpdateTextBoxBindingSources(child);
			}
		}
	}
}
