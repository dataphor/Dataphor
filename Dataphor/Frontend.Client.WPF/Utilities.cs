using System;
using System.Windows;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> WPF helpers. </summary>
	public static class Utilities
	{
		public static T GetChildOfTypeInVisualTree<T>(DependencyObject AElement) 
			where T : DependencyObject
		{
			int LCount = VisualTreeHelper.GetChildrenCount(AElement);
			for (int i = 0; i < LCount; i++)
			{
				T LChild = VisualTreeHelper.GetChild(AElement, i) as T;
				if (LChild != null)
					return LChild;
				else
				{
					LChild = GetChildOfTypeInVisualTree<T>(LChild);
					if (LChild != null)
						return LChild;
				}
			}
			return null;
		}

		public static T GetAncestorOfTypeInVisualTree<T>(DependencyObject AElement) 
			where T : FrameworkElement
		{
			if (AElement != null)
			{
				T LItem = AElement as T;
				if (LItem != null)
					return LItem;
				else
				{
					var LParent = VisualTreeHelper.GetParent(AElement);
					if (LParent != null)
						return GetAncestorOfTypeInVisualTree<T>(LParent);
				}
			}
			return null;
		}
	}
}
