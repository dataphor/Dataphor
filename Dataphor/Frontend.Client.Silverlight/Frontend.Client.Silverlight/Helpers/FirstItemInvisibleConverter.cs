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
		public object Convert(object AValue, Type ATargetType, object AParameter, CultureInfo ACulture)
		{
			if (ATargetType != typeof(Visibility))
				throw new InvalidOperationException("The target of the NullToVisiblity converter must be of type Visibility.");

			bool LIsInverted = false;
			if (AParameter != null)
				LIsInverted = System.Convert.ToBoolean(AParameter);

			var LListBoxItem = (ListBoxItem)AValue;
			var LListBox = SilverlightUtility.FindVisualParent(LListBoxItem, typeof(ListBox)) as ListBox;
			if (LListBox != null)
			{
				var LIndex = LListBox.ItemContainerGenerator.IndexFromContainer(LListBoxItem);
				return (AValue != null && LIndex == 0) ^ LIsInverted ? Visibility.Collapsed : Visibility.Visible;
			}
			return LIsInverted;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException("ConvertBack not implemented on NullToVisibilityConverter.");
		}
	}
}
