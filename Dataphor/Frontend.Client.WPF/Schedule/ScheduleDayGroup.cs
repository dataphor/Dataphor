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

		private static object CoerceGranularity(DependencyObject ASender, object AValue)
		{
			return Math.Max(1, Math.Min(60, (int)AValue));
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

		protected override void PrepareContainerForItemOverride(DependencyObject AElement, object AItem)
		{
			var LDay = AElement as ScheduleDay;
			if (LDay != null)
			{
				var LBinding = new Binding("Date");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.DateProperty, LBinding);
				
				LDay.ItemContainerStyle = AppointmentContainerStyle;
				LDay.ItemTemplate = AppointmentItemTemplate;

				if (!String.IsNullOrEmpty(DisplayMemberPath))
				{
					LBinding = new Binding(DisplayMemberPath);
					LBinding.Source = AItem;
					LDay.SetBinding(ScheduleDay.HeaderProperty, LBinding);
				}

				if (!String.IsNullOrEmpty(GroupIDMemberPath))
				{
					LBinding = new Binding(GroupIDMemberPath);
					LBinding.Source = AItem;
					LDay.SetBinding(ScheduleDay.GroupIDProperty, LBinding);
				}

				LBinding = new Binding("AppointmentDateMemberPath");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.AppointmentDateMemberPathProperty, LBinding);
				
				LBinding = new Binding("AppointmentGroupIDMemberPath");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.AppointmentGroupIDMemberPathProperty, LBinding);

				LBinding = new Binding("ShiftDateMemberPath");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.ShiftDateMemberPathProperty, LBinding);

				LBinding = new Binding("ShiftGroupIDMemberPath");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.ShiftGroupIDMemberPathProperty, LBinding);

				LBinding = new Binding("ShiftItemTemplate");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.ShiftItemTemplateProperty, LBinding);

				LBinding = new Binding("ShiftContainerStyle");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.ShiftContainerStyleProperty, LBinding);

				LBinding = new Binding("HighlightedTime");
				LBinding.Source = this;
				LBinding.Mode = BindingMode.TwoWay;
				LDay.SetBinding(ScheduleDay.HighlightedTimeProperty, LBinding);
				
				LBinding = new Binding("StartTime");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.StartTimeProperty, LBinding);
				
				LBinding = new Binding("SelectedAppointment");
				LBinding.Source = this;
				LBinding.Mode = BindingMode.TwoWay;
				LDay.SetBinding(ScheduleDay.SelectedAppointmentProperty, LBinding);
				
				LBinding = new Binding("Granularity");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.GranularityProperty, LBinding);

				LBinding = new Binding("AppointmentSource");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.AppointmentSourceProperty, LBinding);

				LBinding = new Binding("ShiftSource");
				LBinding.Source = this;
				LDay.SetBinding(ScheduleDay.ShiftSourceProperty, LBinding);
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
}
