using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> A group of day schedules. </summary>
	public class ScheduleDayGroup : ItemsControl
	{
		static ScheduleDayGroup()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleDayGroup), new FrameworkPropertyMetadata(typeof(ScheduleDayGroup)));
		}

		public static readonly DependencyProperty DateProperty =
			DependencyProperty.Register("Date", typeof(DateTime), typeof(ScheduleDayGroup), new PropertyMetadata(DateTime.MinValue));

		/// <summary> The date represented by this date group. </summary>
		public DateTime Date
		{
			get { return (DateTime)GetValue(DateProperty); }
			set { SetValue(DateProperty, value); }
		}

		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(TimeSpan), typeof(ScheduleDayGroup), new PropertyMetadata(TimeSpan.FromHours(8)));

		/// <summary> The first visible time value. </summary>
		public TimeSpan StartTime
		{
			get { return (TimeSpan)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleDayGroup), new PropertyMetadata(15, null, new CoerceValueCallback(CoerceGranularity)));

		/// <summary> The granularity (in minutes) of time markers. </summary>
		public int Granularity
		{
			get { return (int)GetValue(GranularityProperty); }
			set { SetValue(GranularityProperty, value); }
		}

		private static object CoerceGranularity(DependencyObject ASender, object AValue)
		{
			return Math.Max(1, Math.Min(60, (int)AValue));
		}

		public static readonly DependencyProperty AppointmentSourceProperty =
			DependencyProperty.Register("AppointmentSource", typeof(IEnumerable), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The source containing the set of Appointment items. </summary>
		public IEnumerable AppointmentSource
		{
			get { return (IEnumerable)GetValue(AppointmentSourceProperty); }
			set { SetValue(AppointmentSourceProperty, value); }
		}

		public static readonly DependencyProperty AppointmentItemTemplateProperty =
			DependencyProperty.Register("AppointmentItemTemplate", typeof(DataTemplate), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The data template for appointment items. </summary>
		public DataTemplate AppointmentItemTemplate
		{
			get { return (DataTemplate)GetValue(AppointmentItemTemplateProperty); }
			set { SetValue(AppointmentItemTemplateProperty, value); }
		}

		public static readonly DependencyProperty AppointmentContainerStyleProperty =
			DependencyProperty.Register("AppointmentContainerStyle", typeof(Style), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The style to apply to an appointment item container. </summary>
		public Style AppointmentContainerStyle
		{
			get { return (Style)GetValue(AppointmentContainerStyleProperty); }
			set { SetValue(AppointmentContainerStyleProperty, value); }
		}

		public static readonly DependencyProperty HighlightedTimeProperty =
			DependencyProperty.Register("HighlightedTime", typeof(TimeSpan?), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The currently highlighted time. </summary>
		public TimeSpan? HighlightedTime
		{
			get { return (TimeSpan?)GetValue(HighlightedTimeProperty); }
			set { SetValue(HighlightedTimeProperty, value); }
		}

		public static readonly DependencyProperty AppointmentDateMemberPathProperty =
			DependencyProperty.Register("AppointmentDateMemberPath", typeof(string), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The style to apply to an appointment item container. </summary>
		public string AppointmentDateMemberPath
		{
			get { return (string)GetValue(AppointmentDateMemberPathProperty); }
			set { SetValue(AppointmentDateMemberPathProperty, value); }
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ScheduleDay();
		}

		protected override void PrepareContainerForItemOverride(DependencyObject AElement, object AItem)
		{
			var LDay = AElement as ScheduleDay;
			if (LDay != null)
			{
				LDay.Date = Date;
				LDay.AppointmentSource = AppointmentSource;
				LDay.ItemContainerStyle = AppointmentContainerStyle;
				LDay.ItemTemplate = AppointmentItemTemplate;
				LDay.AppointmentDateMemberPath = AppointmentDateMemberPath;
				
				var LBinding = new Binding("HighlightedTime");
				LBinding.Source = this;
				LBinding.Mode = BindingMode.TwoWay;
				LDay.SetBinding(ScheduleDay.HighlightedTimeProperty, LBinding);
				
				LBinding = new Binding("StartTime");
				LBinding.Source = this;
				LBinding.Mode = BindingMode.TwoWay;
				LDay.SetBinding(ScheduleDay.StartTimeProperty, LBinding);
				
				if (!String.IsNullOrEmpty(DisplayMemberPath))
				{
					LBinding = new Binding(DisplayMemberPath);
					LBinding.Source = AItem;
					LDay.SetBinding(ScheduleDay.HeaderProperty, LBinding);
				}
			}
			base.PrepareContainerForItemOverride(AElement, AItem);
		}

		protected override void ClearContainerForItemOverride(DependencyObject AElement, object AItem)
		{
			var LDay = AElement as ScheduleDay;
			if (LDay != null)
				BindingOperations.ClearBinding(LDay, ScheduleDay.HighlightedTimeProperty);
			base.ClearContainerForItemOverride(AElement, AItem);
		}
	}

	public class DateToDayOfWeekConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is DateTime)
			{
				var LDate = (DateTime)value;
				return LDate.ToString("dddd d/M");
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
