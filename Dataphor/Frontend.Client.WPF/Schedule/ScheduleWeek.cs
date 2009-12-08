using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> A week's schedule. </summary>
	[TemplatePart(Name = "TimeBar", Type = typeof(ScheduleTimeBar))]
	public class ScheduleWeek : Control
	{
		public const int CMinutesPerDay = 1440;
		
		static ScheduleWeek()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleWeek), new FrameworkPropertyMetadata(typeof(ScheduleWeek)));
		}

		// StartTime
		
		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(DateTime), typeof(ScheduleWeek), new PropertyMetadata(new DateTime(TimeSpan.FromHours(8).Ticks), null, new CoerceValueCallback(CoerceStartTime)));

		/// <summary> The first visible time value. </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		private static object CoerceStartTime(DependencyObject ASender, object AValue)
		{
			var LWeek = (ScheduleWeek)ASender;
			var LDateTime = (DateTime)AValue;
			var LMinutes = LDateTime.Minute + (LDateTime.Hour * 60);
			return 
				new DateTime
				(
					TimeSpan.FromMinutes
					(
						Math.Min
						(
							(LMinutes + (LWeek.Granularity / 2)) / LWeek.Granularity * LWeek.Granularity, 
							CMinutesPerDay - (LWeek.TimeBarElement == null ? 0 : LWeek.TimeBarElement.VisibleMinutes)
						)
					).Ticks
				);
		}

		// Granularity
		
		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleWeek), new PropertyMetadata(15, null, new CoerceValueCallback(CoerceGranularity)));

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

		// StartDate
		
		public static readonly DependencyProperty StartDateProperty =
			DependencyProperty.Register("StartDate", typeof(DateTime), typeof(ScheduleWeek), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(UpdateDays)));
																					
		/// <summary> The date of the first date entry in the calendar. </summary>
		public DateTime StartDate
		{
			get { return (DateTime)GetValue(StartDateProperty); }
			set { SetValue(StartDateProperty, value); }
		}

		private static void UpdateDays(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((ScheduleWeek)ASender).UpdateDays();
		}

		// Days
		
		private static readonly DependencyPropertyKey DaysPropertyKey =
			DependencyProperty.RegisterReadOnly("Days", typeof(DateTime[]), typeof(ScheduleWeek), new FrameworkPropertyMetadata(null));

		public static readonly DependencyProperty DaysProperty = DaysPropertyKey.DependencyProperty;

		/// <summary> Gets the Days property.  This dependency property 
		/// indicates the date of each day represented in the week. </summary>
		public DateTime[] Days
		{
			get { return (DateTime[])GetValue(DaysProperty); }
		}

		/// <summary> Provides a secure method for setting the Days property.  
		/// This dependency property indicates the date of each day represented in the week. </summary>
		protected void SetDays(DateTime[] value)
		{
			SetValue(DaysPropertyKey, value);
		}

		private void UpdateDays()
		{
			var LDays = new DateTime[7];
			for (int i = 0; i < 7; i++)
				LDays[i] = StartDate + TimeSpan.FromDays(i);
			SetDays(LDays);
		}

		// HighlightedTime
		
		public static readonly DependencyProperty HighlightedTimeProperty =
			DependencyProperty.Register("HighlightedTime", typeof(DateTime?), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The currently highlighted time. </summary>
		public DateTime? HighlightedTime
		{
			get { return (DateTime?)GetValue(HighlightedTimeProperty); }
			set { SetValue(HighlightedTimeProperty, value); }
		}

		// GroupSource
		
		public static readonly DependencyProperty GroupSourceProperty =
			DependencyProperty.Register("GroupSource", typeof(object), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The source containing the set of group items. </summary>
		public object GroupSource
		{
			get { return (object)GetValue(GroupSourceProperty); }
			set { SetValue(GroupSourceProperty, value); }
		}

		// AppointmentSource
		
		public static readonly DependencyProperty AppointmentSourceProperty =
			DependencyProperty.Register("AppointmentSource", typeof(IEnumerable), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The list of appointments to display in this scheduler. </summary>
		public IEnumerable AppointmentSource
		{
			get { return (IEnumerable)GetValue(AppointmentSourceProperty); }
			set { SetValue(AppointmentSourceProperty, value); }
		}
		
		// SelectedAppointment

		public static readonly DependencyProperty SelectedAppointmentProperty =
			DependencyProperty.Register("SelectedAppointment", typeof(object), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The currently selected appointment. </summary>
		public object SelectedAppointment
		{
			get { return (object)GetValue(SelectedAppointmentProperty); }
			set { SetValue(SelectedAppointmentProperty, value); }
		}

		// AppointmentItemTemplate
		
		public static readonly DependencyProperty AppointmentItemTemplateProperty =
			DependencyProperty.Register("AppointmentItemTemplate", typeof(DataTemplate), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The data template for appointment items. </summary>
		public DataTemplate AppointmentItemTemplate
		{
			get { return (DataTemplate)GetValue(AppointmentItemTemplateProperty); }
			set { SetValue(AppointmentItemTemplateProperty, value); }
		}

		// GroupHeaderMemberPath
		
		public static readonly DependencyProperty GroupHeaderMemberPathProperty =
			DependencyProperty.Register("GroupHeaderMemberPath", typeof(string), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The data binding path to the display member within the grouping items. </summary>
		public string GroupHeaderMemberPath
		{
			get { return (string)GetValue(GroupHeaderMemberPathProperty); }
			set { SetValue(GroupHeaderMemberPathProperty, value); }
		}

		// GroupIDMemberPath

		public static readonly DependencyProperty GroupIDMemberPathProperty =
			DependencyProperty.Register("GroupIDMemberPath", typeof(string), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The data binding path to the ID member within the grouping items. </summary>
		public string GroupIDMemberPath
		{
			get { return (string)GetValue(GroupIDMemberPathProperty); }
			set { SetValue(GroupIDMemberPathProperty, value); }
		}
		
		// AppointmentDateMemberPath
		
		public static readonly DependencyProperty AppointmentDateMemberPathProperty =
			DependencyProperty.Register("AppointmentDateMemberPath", typeof(string), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The path to the Date data member within the appointment items. </summary>
		public string AppointmentDateMemberPath
		{
			get { return (string)GetValue(AppointmentDateMemberPathProperty); }
			set { SetValue(AppointmentDateMemberPathProperty, value); }
		}

		// AppointmentGroupIDMemberPath
		
		public static readonly DependencyProperty AppointmentGroupIDMemberPathProperty =
			DependencyProperty.Register("AppointmentGroupIDMemberPath", typeof(string), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> A description of the property. </summary>
		public string AppointmentGroupIDMemberPath
		{
			get { return (string)GetValue(AppointmentGroupIDMemberPathProperty); }
			set { SetValue(AppointmentGroupIDMemberPathProperty, value); }
		}

		// AppointmentContainerStyle
		
		public static readonly DependencyProperty AppointmentContainerStyleProperty =
			DependencyProperty.Register("AppointmentContainerStyle", typeof(Style), typeof(ScheduleWeek), new PropertyMetadata(null));

		/// <summary> The style to apply to an appointment item container. </summary>
		public Style AppointmentContainerStyle
		{
			get { return (Style)GetValue(AppointmentContainerStyleProperty); }
			set { SetValue(AppointmentContainerStyleProperty, value); }
		}

		// TimeBarElement
		
		private ScheduleTimeBar TimeBarElement { get; set; }
		
		public override void OnApplyTemplate()
		{
			UpdateDays();
			TimeBarElement = GetTemplateChild("TimeBar") as ScheduleTimeBar;
			base.OnApplyTemplate();
		}

		protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs AArgs)
		{
			base.OnMouseWheel(AArgs);
			if (AArgs.Delta < 0)
				StartTime += TimeSpan.FromHours(1);
			else if (AArgs.Delta > 0)
				StartTime = new DateTime(Math.Max(0, (new TimeSpan(StartTime.Ticks) - TimeSpan.FromHours(1)).Ticks));
			else
				return;
			AArgs.Handled = true;
		}
	}

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
			return ScheduleWeek.CMinutesPerDay - (value is int ? (int)value : 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
