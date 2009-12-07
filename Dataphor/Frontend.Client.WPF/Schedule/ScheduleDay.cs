using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections;
using System.Reflection;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> A day's schedule. </summary>
	[TemplatePart(Name = "TimeBar", Type = typeof(ScheduleTimeBar))]
	public class ScheduleDay : ListBox
	{
		static ScheduleDay()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleDay), new FrameworkPropertyMetadata(typeof(ScheduleDay)));
		}
		
		public ScheduleDay()
		{
			FAppointmentViewSource = new CollectionViewSource();
			FAppointmentViewSource.Filter += new FilterEventHandler(AppointmentViewFilter);
		}

		// Date
		
		public static readonly DependencyProperty DateProperty =
			DependencyProperty.Register("Date", typeof(DateTime), typeof(ScheduleDay), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(AppointmentViewAffectingPropertyChanged)));

		/// <summary> The date represented by this date group. </summary>
		public DateTime Date
		{
			get { return (DateTime)GetValue(DateProperty); }
			set { SetValue(DateProperty, value); }
		}

		// StartTime

		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(DateTime), typeof(ScheduleDay), new PropertyMetadata(DateTime.MinValue));

		/// <summary> The first visible time value. </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		// Granularity
		
		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleDay), new PropertyMetadata(15, null, new CoerceValueCallback(CoerceGranularity)));

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
		
		// Header

		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register("Header", typeof(object), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> Descriptive header to display for the day. </summary>
		public object Header
		{
			get { return (object)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}

		// AppointmentDateMemberPath

		public static readonly DependencyProperty AppointmentDateMemberPathProperty =
			DependencyProperty.Register("AppointmentDateMemberPath", typeof(string), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(AppointmentViewAffectingPropertyChanged)));

		/// <summary> The style to apply to an appointment item container. </summary>
		public string AppointmentDateMemberPath
		{
			get { return (string)GetValue(AppointmentDateMemberPathProperty); }
			set { SetValue(AppointmentDateMemberPathProperty, value); }
		}
		
		private static void AppointmentViewAffectingPropertyChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((ScheduleDay)ASender).UpdateAppointmentView();
		}
		
		// GroupID

		public static readonly DependencyProperty GroupIDProperty =
			DependencyProperty.Register("GroupID", typeof(object), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(AppointmentViewAffectingPropertyChanged)));

		/// <summary> The value being grouped by. </summary>
		public object GroupID
		{
			get { return (object)GetValue(GroupIDProperty); }
			set { SetValue(GroupIDProperty, value); }
		}
		
		// AppointmentGroupIDMemberPath

		public static readonly DependencyProperty AppointmentGroupIDMemberPathProperty =
			DependencyProperty.Register("AppointmentGroupIDMemberPath", typeof(string), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(AppointmentViewAffectingPropertyChanged)));

		/// <summary> A description of the property. </summary>
		public string AppointmentGroupIDMemberPath
		{
			get { return (string)GetValue(AppointmentGroupIDMemberPathProperty); }
			set { SetValue(AppointmentGroupIDMemberPathProperty, value); }
		}

		// AppointmentSource
		
		public static readonly DependencyProperty AppointmentSourceProperty =
			DependencyProperty.Register("AppointmentSource", typeof(IEnumerable), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(OnAppointmentSourceChanged)));

		/// <summary> The source containing the set of Appointment items. </summary>
		public IEnumerable AppointmentSource
		{
			get { return (IEnumerable)GetValue(AppointmentSourceProperty); }
			set { SetValue(AppointmentSourceProperty, value); }
		}

		private static void OnAppointmentSourceChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((ScheduleDay)ASender).UpdateAppointmentView();
		}

		// AppointmentViewSource
		
		private CollectionViewSource FAppointmentViewSource;

		private void UpdateAppointmentView()
		{
			if (AppointmentSource != null && !String.IsNullOrEmpty(AppointmentDateMemberPath) && !String.IsNullOrEmpty(AppointmentGroupIDMemberPath) && Date != DateTime.MinValue && GroupID != null)
			{
				FAppointmentViewSource.Filter -= new FilterEventHandler(AppointmentViewFilter);
				FAppointmentViewSource.Filter += new FilterEventHandler(AppointmentViewFilter);
				FAppointmentViewSource.Source = AppointmentSource;
				ItemsSource = FAppointmentViewSource.View;
				// TODO: Doesn't seem to set selection correctly after refresh, even if UpdateSelection() is called here.
			}
		}

		private void AppointmentViewFilter(object sender, FilterEventArgs AArgs)
		{
			if (AArgs.Item != null)
			{
				var LType = AArgs.Item.GetType();
				var LDate = (DateTime)LType.GetProperty(AppointmentDateMemberPath).GetValue(AArgs.Item, new object[] {});
				var LGroupID = LType.GetProperty(AppointmentGroupIDMemberPath).GetValue(AArgs.Item, new object[] { });
				AArgs.Accepted = LDate == Date && LGroupID.Equals(GroupID);
			}
			else
				AArgs.Accepted = true;
		}

		// SelectedAppointment

		public static readonly DependencyProperty SelectedAppointmentProperty =
			DependencyProperty.Register("SelectedAppointment", typeof(object), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(OnSelectedAppointmentChanged)));

		/// <summary> The currently selected appointment. </summary>
		public object SelectedAppointment
		{
			get { return (object)GetValue(SelectedAppointmentProperty); }
			set { SetValue(SelectedAppointmentProperty, value); }
		}

		private static void OnSelectedAppointmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((ScheduleDay)d).OnSelectedAppointmentChanged(e);
		}

		protected virtual void OnSelectedAppointmentChanged(DependencyPropertyChangedEventArgs e)
		{
			SelectedIndex = Items.IndexOf(e.NewValue);
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			UpdateSelection();
		}

		private void UpdateSelection()
		{
			if (SelectedItem != null)
				SelectedAppointment = SelectedItem;
		}

		// HighlightedTime
		
		public static readonly DependencyProperty HighlightedTimeProperty =
			DependencyProperty.Register("HighlightedTime", typeof(DateTime?), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> The time that is currently highlighted. </summary>
		public DateTime? HighlightedTime
		{
			get { return (DateTime?)GetValue(HighlightedTimeProperty); }
			set { SetValue(HighlightedTimeProperty, value); }
		}

		// TimeBar
		
		public static readonly DependencyProperty TimeBarProperty =
			DependencyProperty.Register("TimeBar", typeof(ScheduleTimeBar), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> The templated time bar component. </summary>
		public ScheduleTimeBar TimeBar
		{
			get { return (ScheduleTimeBar)GetValue(TimeBarProperty); }
			set { SetValue(TimeBarProperty, value); }
		}

		public override void OnApplyTemplate()
		{
			TimeBar = GetTemplateChild("TimeBar") as ScheduleTimeBar;
			base.OnApplyTemplate();
		}
	}
}
