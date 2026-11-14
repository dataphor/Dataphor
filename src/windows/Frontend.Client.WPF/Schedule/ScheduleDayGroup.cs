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

		// Date
		
		public static readonly DependencyProperty DateProperty =
			DependencyProperty.Register("Date", typeof(DateTime), typeof(ScheduleDayGroup), new PropertyMetadata(DateTime.MinValue));

		/// <summary> The date represented by this date group. </summary>
		public DateTime Date
		{
			get { return (DateTime)GetValue(DateProperty); }
			set { SetValue(DateProperty, value); }
		}

		// StartTime
		
		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(DateTime), typeof(ScheduleDayGroup), new PropertyMetadata(new DateTime(TimeSpan.FromHours(8).Ticks)));

		/// <summary> The first visible time value. </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		// Granularity
		
		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleDayGroup), new PropertyMetadata(15, null, new CoerceValueCallback(CoerceGranularity)));

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

		// AppointmentSource
		
		public static readonly DependencyProperty AppointmentSourceProperty =
			DependencyProperty.Register("AppointmentSource", typeof(IEnumerable), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The source containing the set of Appointment items. </summary>
		public IEnumerable AppointmentSource
		{
			get { return (IEnumerable)GetValue(AppointmentSourceProperty); }
			set { SetValue(AppointmentSourceProperty, value); }
		}

		// SelectedAppointment

		public static readonly DependencyProperty SelectedAppointmentProperty =
			DependencyProperty.Register("SelectedAppointment", typeof(object), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The currently selected appointment. </summary>
		public object SelectedAppointment
		{
			get { return (object)GetValue(SelectedAppointmentProperty); }
			set { SetValue(SelectedAppointmentProperty, value); }
		}

		// AppointmentItemTemplate
		
		public static readonly DependencyProperty AppointmentItemTemplateProperty =
			DependencyProperty.Register("AppointmentItemTemplate", typeof(DataTemplate), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The data template for appointment items. </summary>
		public DataTemplate AppointmentItemTemplate
		{
			get { return (DataTemplate)GetValue(AppointmentItemTemplateProperty); }
			set { SetValue(AppointmentItemTemplateProperty, value); }
		}

		// AppointmentContainerStyle
		
		public static readonly DependencyProperty AppointmentContainerStyleProperty =
			DependencyProperty.Register("AppointmentContainerStyle", typeof(Style), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The style to apply to an appointment item container. </summary>
		public Style AppointmentContainerStyle
		{
			get { return (Style)GetValue(AppointmentContainerStyleProperty); }
			set { SetValue(AppointmentContainerStyleProperty, value); }
		}

		// HighlightedTimeProperty
		
		public static readonly DependencyProperty HighlightedTimeProperty =
			DependencyProperty.Register("HighlightedTime", typeof(DateTime?), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The currently highlighted time. </summary>
		public DateTime? HighlightedTime
		{
			get { return (DateTime?)GetValue(HighlightedTimeProperty); }
			set { SetValue(HighlightedTimeProperty, value); }
		}

		// GroupIDMemberPath

		public static readonly DependencyProperty GroupIDMemberPathProperty =
			DependencyProperty.Register("GroupIDMemberPath", typeof(string), typeof(ScheduleDayGroup), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

		/// <summary> The data binding path to the ID member within the grouping items. </summary>
		public string GroupIDMemberPath
		{
			get { return (string)GetValue(GroupIDMemberPathProperty); }
			set { SetValue(GroupIDMemberPathProperty, value); }
		}

		// AppointmentDateMemberPath
		
		public static readonly DependencyProperty AppointmentDateMemberPathProperty =
			DependencyProperty.Register("AppointmentDateMemberPath", typeof(string), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The style to apply to an appointment item container. </summary>
		public string AppointmentDateMemberPath
		{
			get { return (string)GetValue(AppointmentDateMemberPathProperty); }
			set { SetValue(AppointmentDateMemberPathProperty, value); }
		}

		// AppointmentGroupIDMemberPath

		public static readonly DependencyProperty AppointmentGroupIDMemberPathProperty =
			DependencyProperty.Register("AppointmentGroupIDMemberPath", typeof(string), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> A description of the property. </summary>
		public string AppointmentGroupIDMemberPath
		{
			get { return (string)GetValue(AppointmentGroupIDMemberPathProperty); }
			set { SetValue(AppointmentGroupIDMemberPathProperty, value); }
		}

		// ShiftSource

		public static readonly DependencyProperty ShiftSourceProperty =
			DependencyProperty.Register("ShiftSource", typeof(IEnumerable), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The source containing the set of Shift items. </summary>
		public IEnumerable ShiftSource
		{
			get { return (IEnumerable)GetValue(ShiftSourceProperty); }
			set { SetValue(ShiftSourceProperty, value); }
		}

		// ShiftDateMemberPath

		public static readonly DependencyProperty ShiftDateMemberPathProperty =
			DependencyProperty.Register("ShiftDateMemberPath", typeof(string), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The style to apply to an Shift item container. </summary>
		public string ShiftDateMemberPath
		{
			get { return (string)GetValue(ShiftDateMemberPathProperty); }
			set { SetValue(ShiftDateMemberPathProperty, value); }
		}

		// ShiftGroupIDMemberPath

		public static readonly DependencyProperty ShiftGroupIDMemberPathProperty =
			DependencyProperty.Register("ShiftGroupIDMemberPath", typeof(string), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> A description of the property. </summary>
		public string ShiftGroupIDMemberPath
		{
			get { return (string)GetValue(ShiftGroupIDMemberPathProperty); }
			set { SetValue(ShiftGroupIDMemberPathProperty, value); }
		}

		// ShiftItemTemplate

		public static readonly DependencyProperty ShiftItemTemplateProperty =
			DependencyProperty.Register("ShiftItemTemplate", typeof(DataTemplate), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The data template used to display a shift item. </summary>
		public DataTemplate ShiftItemTemplate
		{
			get { return (DataTemplate)GetValue(ShiftItemTemplateProperty); }
			set { SetValue(ShiftItemTemplateProperty, value); }
		}

		// ShiftContainerStyle

		public static readonly DependencyProperty ShiftContainerStyleProperty =
			DependencyProperty.Register("ShiftContainerStyle", typeof(Style), typeof(ScheduleDayGroup), new PropertyMetadata(null));

		/// <summary> The style to apply to an Shift item container. </summary>
		public Style ShiftContainerStyle
		{
			get { return (Style)GetValue(ShiftContainerStyleProperty); }
			set { SetValue(ShiftContainerStyleProperty, value); }
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ScheduleDay();
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			var day = element as ScheduleDay;
			if (day != null)
			{
				var binding = new Binding("Date");
				binding.Source = this;
				day.SetBinding(ScheduleDay.DateProperty, binding);
				
				day.ItemContainerStyle = AppointmentContainerStyle;
				day.ItemTemplate = AppointmentItemTemplate;

				if (!String.IsNullOrEmpty(DisplayMemberPath))
				{
					binding = new Binding(DisplayMemberPath);
					binding.Source = item;
					day.SetBinding(ScheduleDay.HeaderProperty, binding);
				}

				if (!String.IsNullOrEmpty(GroupIDMemberPath))
				{
					binding = new Binding(GroupIDMemberPath);
					binding.Source = item;
					day.SetBinding(ScheduleDay.GroupIDProperty, binding);
				}

				binding = new Binding("AppointmentDateMemberPath");
				binding.Source = this;
				day.SetBinding(ScheduleDay.AppointmentDateMemberPathProperty, binding);
				
				binding = new Binding("AppointmentGroupIDMemberPath");
				binding.Source = this;
				day.SetBinding(ScheduleDay.AppointmentGroupIDMemberPathProperty, binding);

				binding = new Binding("ShiftDateMemberPath");
				binding.Source = this;
				day.SetBinding(ScheduleDay.ShiftDateMemberPathProperty, binding);

				binding = new Binding("ShiftGroupIDMemberPath");
				binding.Source = this;
				day.SetBinding(ScheduleDay.ShiftGroupIDMemberPathProperty, binding);

				binding = new Binding("ShiftItemTemplate");
				binding.Source = this;
				day.SetBinding(ScheduleDay.ShiftItemTemplateProperty, binding);

				binding = new Binding("ShiftContainerStyle");
				binding.Source = this;
				day.SetBinding(ScheduleDay.ShiftContainerStyleProperty, binding);

				binding = new Binding("HighlightedTime");
				binding.Source = this;
				binding.Mode = BindingMode.TwoWay;
				day.SetBinding(ScheduleDay.HighlightedTimeProperty, binding);
				
				binding = new Binding("StartTime");
				binding.Source = this;
				day.SetBinding(ScheduleDay.StartTimeProperty, binding);
				
				binding = new Binding("SelectedAppointment");
				binding.Source = this;
				binding.Mode = BindingMode.TwoWay;
				day.SetBinding(ScheduleDay.SelectedAppointmentProperty, binding);
				
				binding = new Binding("Granularity");
				binding.Source = this;
				day.SetBinding(ScheduleDay.GranularityProperty, binding);

				binding = new Binding("AppointmentSource");
				binding.Source = this;
				day.SetBinding(ScheduleDay.AppointmentSourceProperty, binding);

				binding = new Binding("ShiftSource");
				binding.Source = this;
				day.SetBinding(ScheduleDay.ShiftSourceProperty, binding);
			}
			base.PrepareContainerForItemOverride(element, item);
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			var day = element as ScheduleDay;
			if (day != null)
				BindingOperations.ClearBinding(day, ScheduleDay.HighlightedTimeProperty);
			base.ClearContainerForItemOverride(element, item);
		}
	}
}
