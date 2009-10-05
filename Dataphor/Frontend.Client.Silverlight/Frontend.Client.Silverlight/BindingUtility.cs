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
		public static void UpdateTextBoxBindingSources(DependencyObject AObject)
		{
			var LCount = VisualTreeHelper.GetChildrenCount(AObject);
			for (int i = 0; i < LCount; i++)
			{
				var LChild = VisualTreeHelper.GetChild(AObject, i);
				var LTextBox = LChild as TextBox;
				if (LTextBox != null)
				{
					var LBindingExpression = LTextBox.GetBindingExpression(TextBox.TextProperty);
					if (LBindingExpression != null)
						LBindingExpression.UpdateSource();
				}
				else
					UpdateTextBoxBindingSources(LChild);
			}
		}
	}
}
