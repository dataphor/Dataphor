using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Converts from a ListBoxItem to visibility (1st item = Collapsed). </summary>
	public class FirstItemInvisibleConverter : IValueConverter
	{
		public object Convert(object tempValue, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof(Visibility))
				throw new InvalidOperationException("The target of the NullToVisiblity converter must be of type Visibility.");

			bool isInverted = false;
			if (parameter != null)
				isInverted = System.Convert.ToBoolean(parameter);

			var listBoxItem = (ListBoxItem)tempValue;
			var listBox = SilverlightUtility.FindVisualParent(listBoxItem, typeof(ListBox)) as ListBox;
			if (listBox != null)
			{
				var index = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
				return (tempValue != null && index == 0) ^ isInverted ? Visibility.Collapsed : Visibility.Visible;
			}
			return isInverted;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException("ConvertBack not implemented on FirstItemInvisibleConverter.");
		}
	}
}
