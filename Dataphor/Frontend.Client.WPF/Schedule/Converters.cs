using System;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> Converts to and from a number of minutes (int) from a DateTime. </summary>
	public class DateTimeToMinutesConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is DateTime)
				return ((DateTime)value).Minute + ((DateTime)value).Hour * 60;
			else
				return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is int)
				return new DateTime(TimeSpan.FromMinutes((int)value).Ticks);
			else if (value is double)
				return new DateTime(TimeSpan.FromMinutes((int)(double)value).Ticks);
			else
				return null;
		}
	}

	/// <summary> Converts to the number of minutes left in a day minus the given number of minutes. </summary>
	public class RemainingMinutesConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Scheduler.MinutesPerDay - (value is int ? (int)value : 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class DateToDayOfWeekConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is DateTime)
			{
				var date = (DateTime)value;
				return date.ToString("dddd M/d");
			}
			else
				return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class ScheduleTimeBlockTimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value != null && value is DateTime)
			{
				var tempValue = (DateTime)value;
				return
					String.Format
					(
						"{0}:{1:D2}{2}",
						tempValue.Hour == 0 ? 12 : (tempValue.Hour <= 12 ? tempValue.Hour : (tempValue.Hour - 12)),
						tempValue.Minute,
						tempValue.Hour < 12 ? "am" : "pm"
					);
			}
			else
				return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
