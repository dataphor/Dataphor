using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> Converts from a null reference to visibility (null = Collapsed). </summary>
	/// <remarks> The parameter can be set to a bool value to invert the affect of the converter (null = Visible). </remarks>
	public class NullToVisibilityConverter : IValueConverter
	{
		public object Convert(object tempValue, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof(Visibility))
				throw new InvalidOperationException("The target of the NullToVisiblity converter must be of type Visibility.");

			bool isInverted = false;
			if (parameter != null)
				isInverted = System.Convert.ToBoolean(parameter);

			return (tempValue == null) ^ isInverted ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException("ConvertBack not implemented on NullToVisibilityConverter.");
		}
	}
}
