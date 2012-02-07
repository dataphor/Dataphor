using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public static class SilverlightUtility
	{
		public static DependencyObject FindVisualParent(DependencyObject objectValue, Type type)
		{
			var parent = VisualTreeHelper.GetParent(objectValue);
			if (parent != null)
			{
				if (type.IsAssignableFrom(parent.GetType()))
					return parent;
				else
					return FindVisualParent(parent, type);
			}
			else
				return null;
		}
	}
}
