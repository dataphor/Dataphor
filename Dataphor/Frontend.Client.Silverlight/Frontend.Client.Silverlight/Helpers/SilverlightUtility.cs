using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public static class SilverlightUtility
	{
		public static DependencyObject FindVisualParent(DependencyObject AObject, Type AType)
		{
			var LParent = VisualTreeHelper.GetParent(AObject);
			if (LParent != null)
			{
				if (AType.IsAssignableFrom(LParent.GetType()))
					return LParent;
				else
					return FindVisualParent(LParent, AType);
			}
			else
				return null;
		}
	}
}
