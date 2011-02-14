using System;
using System.Windows;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> WPF helpers. </summary>
	public static class Utilities
	{
		public static T GetChildOfTypeInVisualTree<T>(DependencyObject element) 
			where T : DependencyObject
		{
			int count = VisualTreeHelper.GetChildrenCount(element);
			for (int i = 0; i < count; i++)
			{
				T child = VisualTreeHelper.GetChild(element, i) as T;
				if (child != null)
					return child;
				else
				{
					child = GetChildOfTypeInVisualTree<T>(child);
					if (child != null)
						return child;
				}
			}
			return null;
		}

		public static T GetAncestorOfTypeInVisualTree<T>(DependencyObject element) 
			where T : FrameworkElement
		{
			if (element != null)
			{
				T item = element as T;
				if (item != null)
					return item;
				else
				{
					var parent = VisualTreeHelper.GetParent(element);
					if (parent != null)
						return GetAncestorOfTypeInVisualTree<T>(parent);
				}
			}
			return null;
		}
	}
}
