using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> A block of time within a schedule time bar. </summary>
	public class ScheduleTimeBlock : Control
	{
		static ScheduleTimeBlock()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleTimeBlock), new FrameworkPropertyMetadata(typeof(ScheduleTimeBlock)));
		}

		public static readonly DependencyProperty TimeProperty =
			DependencyProperty.Register("Time", typeof(DateTime), typeof(ScheduleTimeBlock), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(TimeChanged)));

		/// <summary> The starting time of the time block represented by this time control. </summary>
		public DateTime Time
		{
			get { return (DateTime)GetValue(TimeProperty); }
			set { SetValue(TimeProperty, value); }
		}
		
		private static void TimeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleTimeBlock)sender).UpdateIsHourMarker();
		}

		private void UpdateIsHourMarker()
		{
			SetIsHourMarker(Time.Minute == 0);
		}

		public override void OnApplyTemplate()
		{
			UpdateIsHourMarker();
			base.OnApplyTemplate();
		}

		private static readonly DependencyPropertyKey IsHourMarkerPropertyKey = 
			DependencyProperty.RegisterReadOnly("IsHourMarker", typeof(bool), typeof(ScheduleTimeBlock), new FrameworkPropertyMetadata(false));

		public static readonly DependencyProperty IsHourMarkerProperty = IsHourMarkerPropertyKey.DependencyProperty;

		/// <summary> Gets the IsHourMarker property.  This dependency property 
		/// indicates that the time designated by the Time property falls evenly on an hour. </summary>
		public bool IsHourMarker
		{
			get { return (bool)GetValue(IsHourMarkerProperty); }
		}

		/// <summary> Provides a secure method for setting the  IsHourMarker property.  
		/// This dependency property indicates that the time designated by the Time property falls evenly on an hour. </summary>
		/// <param name="value">The new value for the property.</param>
		protected void SetIsHourMarker(bool value)
		{
			SetValue(IsHourMarkerPropertyKey, value);
		}

		public static readonly DependencyProperty IsHighlightedProperty =
			DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(ScheduleTimeBlock), new FrameworkPropertyMetadata(false));

		/// <summary> Gets the IsHighlighted property.  This dependency property 
		/// indicates that the current time block is highlighted. </summary>
		public bool IsHighlighted
		{
			get { return (bool)GetValue(IsHighlightedProperty); }
			set { SetValue(IsHighlightedProperty, value); }
		}

		protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			IsHighlighted = true;
		}

		protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			IsHighlighted = false;
		}

		private static readonly DependencyPropertyKey IsSelectedPropertyKey =
			DependencyProperty.RegisterReadOnly("IsSelected", typeof(bool), typeof(ScheduleTimeBlock), new FrameworkPropertyMetadata(false));

		public static readonly DependencyProperty IsSelectedProperty = IsSelectedPropertyKey.DependencyProperty;

		/// <summary> Gets the IsSelected property.  This dependency property 
		/// indicates that the current time block is Selected. </summary>
		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
		}

		/// <summary> Provides a secure method for setting the IsSelected property.  
		/// This dependency property indicates that the current time block is Selected. </summary>
		protected internal void SetIsSelected(bool value)
		{
			SetValue(IsSelectedPropertyKey, value);
		}

		protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
		}
	}

	public class ScheduleTimeBlockHourConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value != null && value is DateTime)
			{
				var tempValue = (DateTime)value;
				switch (tempValue.Hour)
				{
					case 0 : return "12m";
					case 12 : return "12n";
					default : return tempValue.Hour < 12 ? (tempValue.Hour.ToString() + "am") : ((tempValue.Hour - 12).ToString() + "pm");
				}
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
