using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> Scheduler control. </summary>
	[TemplatePart(Name = "TimeBar", Type = typeof(ScheduleTimeBar))]
	public class Scheduler : Control
	{
		public const int MinutesPerDay = 1440;

		static Scheduler()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Scheduler), new FrameworkPropertyMetadata(typeof(Scheduler)));
		}

		// StartTime

		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(DateTime), typeof(Scheduler), new PropertyMetadata(new DateTime(TimeSpan.FromHours(8).Ticks), null, new CoerceValueCallback(CoerceStartTime)));

		/// <summary> The first visible time value. </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		private static object CoerceStartTime(DependencyObject sender, object tempValue)
		{
			var week = (Scheduler)sender;
			var dateTime = (DateTime)tempValue;
			var minutes = dateTime.Minute + (dateTime.Hour * 60);
			return
				new DateTime
				(
					TimeSpan.FromMinutes
					(
						Math.Min
						(
							(minutes + (week.Granularity / 2)) / week.Granularity * week.Granularity,
							MinutesPerDay - (week.TimeBarElement == null ? 0 : week.TimeBarElement.VisibleMinutes)
						)
					).Ticks
				);
		}

		// Granularity

		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(Scheduler), new PropertyMetadata(15, null, new CoerceValueCallback(CoerceGranularity)));

		/// <summary> The granularity (in minutes) of time markers. </summary>
		public int Granularity
		{
			get { return (int)GetValue(GranularityProperty); }
			set { SetValue(GranularityProperty, value); }
		}

		private static object CoerceGranularity(DependencyObject sender, object tempValue)
		{
			return Math.Max(1, Math.Min(60, (int)tempValue));
		}

		// StartDate

		public static readonly DependencyProperty StartDateProperty =
			DependencyProperty.Register("StartDate", typeof(DateTime), typeof(Scheduler), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(StartDateChanged)));

		/// <summary> The date of the first date entry in the calendar. </summary>
		public DateTime StartDate
		{
			get { return (DateTime)GetValue(StartDateProperty); }
			set { SetValue(StartDateProperty, value); }
		}

		private static void StartDateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((Scheduler)sender).StartDateChanged((DateTime)args.OldValue, (DateTime)args.NewValue);
		}
		
		protected virtual void StartDateChanged(DateTime oldValue, DateTime newValue)
		{
		}

		// HighlightedTime

		public static readonly DependencyProperty HighlightedTimeProperty =
			DependencyProperty.Register("HighlightedTime", typeof(DateTime?), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The currently highlighted time. </summary>
		public DateTime? HighlightedTime
		{
			get { return (DateTime?)GetValue(HighlightedTimeProperty); }
			set { SetValue(HighlightedTimeProperty, value); }
		}

		// GroupSource

		public static readonly DependencyProperty GroupSourceProperty =
			DependencyProperty.Register("GroupSource", typeof(object), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The source containing the set of group items. </summary>
		public object GroupSource
		{
			get { return (object)GetValue(GroupSourceProperty); }
			set { SetValue(GroupSourceProperty, value); }
		}

		// AppointmentSource

		public static readonly DependencyProperty AppointmentSourceProperty =
			DependencyProperty.Register("AppointmentSource", typeof(IEnumerable), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The list of appointments to display in this scheduler. </summary>
		public IEnumerable AppointmentSource
		{
			get { return (IEnumerable)GetValue(AppointmentSourceProperty); }
			set { SetValue(AppointmentSourceProperty, value); }
		}

		// SelectedAppointment

		public static readonly DependencyProperty SelectedAppointmentProperty =
			DependencyProperty.Register("SelectedAppointment", typeof(object), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The currently selected appointment. </summary>
		public object SelectedAppointment
		{
			get { return (object)GetValue(SelectedAppointmentProperty); }
			set { SetValue(SelectedAppointmentProperty, value); }
		}

		// AppointmentItemTemplate

		public static readonly DependencyProperty AppointmentItemTemplateProperty =
			DependencyProperty.Register("AppointmentItemTemplate", typeof(DataTemplate), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The data template for appointment items. </summary>
		public DataTemplate AppointmentItemTemplate
		{
			get { return (DataTemplate)GetValue(AppointmentItemTemplateProperty); }
			set { SetValue(AppointmentItemTemplateProperty, value); }
		}

		// GroupHeaderMemberPath

		public static readonly DependencyProperty GroupHeaderMemberPathProperty =
			DependencyProperty.Register("GroupHeaderMemberPath", typeof(string), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The data binding path to the display member within the grouping items. </summary>
		public string GroupHeaderMemberPath
		{
			get { return (string)GetValue(GroupHeaderMemberPathProperty); }
			set { SetValue(GroupHeaderMemberPathProperty, value); }
		}

		// GroupIDMemberPath

		public static readonly DependencyProperty GroupIDMemberPathProperty =
			DependencyProperty.Register("GroupIDMemberPath", typeof(string), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The data binding path to the ID member within the grouping items. </summary>
		public string GroupIDMemberPath
		{
			get { return (string)GetValue(GroupIDMemberPathProperty); }
			set { SetValue(GroupIDMemberPathProperty, value); }
		}

		// AppointmentDateMemberPath

		public static readonly DependencyProperty AppointmentDateMemberPathProperty =
			DependencyProperty.Register("AppointmentDateMemberPath", typeof(string), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The path to the Date data member within the appointment items. </summary>
		public string AppointmentDateMemberPath
		{
			get { return (string)GetValue(AppointmentDateMemberPathProperty); }
			set { SetValue(AppointmentDateMemberPathProperty, value); }
		}

		// AppointmentGroupIDMemberPath

		public static readonly DependencyProperty AppointmentGroupIDMemberPathProperty =
			DependencyProperty.Register("AppointmentGroupIDMemberPath", typeof(string), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> A description of the property. </summary>
		public string AppointmentGroupIDMemberPath
		{
			get { return (string)GetValue(AppointmentGroupIDMemberPathProperty); }
			set { SetValue(AppointmentGroupIDMemberPathProperty, value); }
		}

		// AppointmentContainerStyle

		public static readonly DependencyProperty AppointmentContainerStyleProperty =
			DependencyProperty.Register("AppointmentContainerStyle", typeof(Style), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The style to apply to an appointment item container. </summary>
		public Style AppointmentContainerStyle
		{
			get { return (Style)GetValue(AppointmentContainerStyleProperty); }
			set { SetValue(AppointmentContainerStyleProperty, value); }
		}

		// ShiftSource

		public static readonly DependencyProperty ShiftSourceProperty =
			DependencyProperty.Register("ShiftSource", typeof(IEnumerable), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The list of Shifts to display in this scheduler. </summary>
		public IEnumerable ShiftSource
		{
			get { return (IEnumerable)GetValue(ShiftSourceProperty); }
			set { SetValue(ShiftSourceProperty, value); }
		}

		// ShiftDateMemberPath

		public static readonly DependencyProperty ShiftDateMemberPathProperty =
			DependencyProperty.Register("ShiftDateMemberPath", typeof(string), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The path to the Date data member within the Shift items. </summary>
		public string ShiftDateMemberPath
		{
			get { return (string)GetValue(ShiftDateMemberPathProperty); }
			set { SetValue(ShiftDateMemberPathProperty, value); }
		}

		// ShiftGroupIDMemberPath

		public static readonly DependencyProperty ShiftGroupIDMemberPathProperty =
			DependencyProperty.Register("ShiftGroupIDMemberPath", typeof(string), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> A description of the property. </summary>
		public string ShiftGroupIDMemberPath
		{
			get { return (string)GetValue(ShiftGroupIDMemberPathProperty); }
			set { SetValue(ShiftGroupIDMemberPathProperty, value); }
		}

		// ShiftItemTemplate

		public static readonly DependencyProperty ShiftItemTemplateProperty =
			DependencyProperty.Register("ShiftItemTemplate", typeof(DataTemplate), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The data template used to display a shift item. </summary>
		public DataTemplate ShiftItemTemplate
		{
			get { return (DataTemplate)GetValue(ShiftItemTemplateProperty); }
			set { SetValue(ShiftItemTemplateProperty, value); }
		}

		// ShiftContainerStyle

		public static readonly DependencyProperty ShiftContainerStyleProperty =
			DependencyProperty.Register("ShiftContainerStyle", typeof(Style), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The style to apply to an Shift item container. </summary>
		public Style ShiftContainerStyle
		{
			get { return (Style)GetValue(ShiftContainerStyleProperty); }
			set { SetValue(ShiftContainerStyleProperty, value); }
		}

		// DayStyle

		public static readonly DependencyProperty DayStyleProperty =
			DependencyProperty.Register("DayStyle", typeof(Style), typeof(Scheduler), new PropertyMetadata(null));

		/// <summary> The style to apply to a ScheduleDay within this control's template. </summary>
		public Style DayStyle
		{
			get { return (Style)GetValue(DayStyleProperty); }
			set { SetValue(DayStyleProperty, value); }
		}

		// TimeBarElement

		private ScheduleTimeBar TimeBarElement { get; set; }

		public override void OnApplyTemplate()
		{
			TimeBarElement = GetTemplateChild("TimeBar") as ScheduleTimeBar;
			base.OnApplyTemplate();
		}

		protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs args)
		{
			base.OnMouseWheel(args);
			if (args.Delta < 0)
				StartTime += TimeSpan.FromHours(1);
			else if (args.Delta > 0)
				StartTime = new DateTime(Math.Max(0, (new TimeSpan(StartTime.Ticks) - TimeSpan.FromHours(1)).Ticks));
			else
				return;
			args.Handled = true;
		}
	}
}
