using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Converts from a bool to visibility (false = Collapsed). </summary>
	/// <remarks> The parameter can be set to a bool value to invert the affect of the converter (false = Visible). </remarks>
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object AValue, Type ATargetType, object AParameter, CultureInfo ACulture)
		{
			if (ATargetType != typeof(Visibility))
				throw new InvalidOperationException("The target of the BooleanToVisiblityConverter must be of type Visibility.");

			bool LIsInverted = false;
			if (AParameter != null)
				LIsInverted = System.Convert.ToBoolean(AParameter);

			return ((bool)AValue ^ LIsInverted) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException("ConvertBack not implemented on NullToVisibilityConverter.");
		}
	}
}
