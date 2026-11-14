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
		
		// Date
		
		public static readonly DependencyProperty DateProperty =
			DependencyProperty.Register("Date", typeof(DateTime), typeof(ScheduleDay), new PropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(ViewAffectingPropertyChanged)));

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

		private static object CoerceGranularity(DependencyObject sender, object tempValue)
		{
			return Math.Max(1, Math.Min(60, (int)tempValue));
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

		// HeaderStyle

		public static readonly DependencyProperty HeaderStyleProperty =
			DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> The style to apply to the header. </summary>
		public Style HeaderStyle
		{
			get { return (Style)GetValue(HeaderStyleProperty); }
			set { SetValue(HeaderStyleProperty, value); }
		}

		// GroupID

		public static readonly DependencyProperty GroupIDProperty =
			DependencyProperty.Register("GroupID", typeof(object), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(ViewAffectingPropertyChanged)));

		/// <summary> The value being grouped by. </summary>
		public object GroupID
		{
			get { return (object)GetValue(GroupIDProperty); }
			set { SetValue(GroupIDProperty, value); }
		}

		private static void ViewAffectingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var day = (ScheduleDay)sender;
			day.UpdateAppointmentView();
			day.UpdateShiftView();
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

		private static void AppointmentViewAffectingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleDay)sender).UpdateAppointmentView();
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

		private static void OnAppointmentSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleDay)sender).UpdateAppointmentView();
		}

		// AppointmentViewSource
		
		private void UpdateAppointmentView()
		{
			if (AppointmentSource != null && !String.IsNullOrEmpty(AppointmentDateMemberPath) && Date != DateTime.MinValue && (String.IsNullOrEmpty(AppointmentGroupIDMemberPath) || GroupID != null))
			{
				var source = new CollectionViewSource();
				source.Filter += new FilterEventHandler(AppointmentViewFilter);
				source.Source = AppointmentSource;
				ItemsSource = source.View;
				UpdateSelection();
			}
			else
				ItemsSource = null;
		}

		private void AppointmentViewFilter(object sender, FilterEventArgs args)
		{
			if (args.Item != null)
			{
				var type = args.Item.GetType();
				var date = (DateTime)type.GetProperty(AppointmentDateMemberPath).GetValue(args.Item, new object[] {});
				args.Accepted = date == Date;
				
				if (!String.IsNullOrEmpty(AppointmentGroupIDMemberPath))
				{
					var groupID = type.GetProperty(AppointmentGroupIDMemberPath).GetValue(args.Item, new object[] { });
					args.Accepted = args.Accepted && groupID.Equals(GroupID);
				}
			}
			else
				args.Accepted = true;
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
			UpdateSelectedIndex();
		}

		protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			UpdateSelectedIndex();
		}
		
		private void UpdateSelectedIndex()
		{
			SelectedIndex = Items.IndexOf(SelectedAppointment);
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

		// ShiftDateMemberPath

		public static readonly DependencyProperty ShiftDateMemberPathProperty =
			DependencyProperty.Register("ShiftDateMemberPath", typeof(string), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(ShiftViewAffectingPropertyChanged)));

		/// <summary> The style to apply to an Shift item container. </summary>
		public string ShiftDateMemberPath
		{
			get { return (string)GetValue(ShiftDateMemberPathProperty); }
			set { SetValue(ShiftDateMemberPathProperty, value); }
		}

		private static void ShiftViewAffectingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleDay)sender).UpdateShiftView();
		}

		// ShiftGroupIDMemberPath

		public static readonly DependencyProperty ShiftGroupIDMemberPathProperty =
			DependencyProperty.Register("ShiftGroupIDMemberPath", typeof(string), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(ShiftViewAffectingPropertyChanged)));

		/// <summary> A description of the property. </summary>
		public string ShiftGroupIDMemberPath
		{
			get { return (string)GetValue(ShiftGroupIDMemberPathProperty); }
			set { SetValue(ShiftGroupIDMemberPathProperty, value); }
		}

		// ShiftItemTemplate

		public static readonly DependencyProperty ShiftItemTemplateProperty =
			DependencyProperty.Register("ShiftItemTemplate", typeof(DataTemplate), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> The data template used to display a shift item. </summary>
		public DataTemplate ShiftItemTemplate
		{
			get { return (DataTemplate)GetValue(ShiftItemTemplateProperty); }
			set { SetValue(ShiftItemTemplateProperty, value); }
		}

		// ShiftContainerStyle

		public static readonly DependencyProperty ShiftContainerStyleProperty =
			DependencyProperty.Register("ShiftContainerStyle", typeof(Style), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> The style to apply to an Shift item container. </summary>
		public Style ShiftContainerStyle
		{
			get { return (Style)GetValue(ShiftContainerStyleProperty); }
			set { SetValue(ShiftContainerStyleProperty, value); }
		}

		// ShiftSource

		public static readonly DependencyProperty ShiftSourceProperty =
			DependencyProperty.Register("ShiftSource", typeof(IEnumerable), typeof(ScheduleDay), new PropertyMetadata(null, new PropertyChangedCallback(OnShiftSourceChanged)));

		/// <summary> The source containing the set of Shift items. </summary>
		public IEnumerable ShiftSource
		{
			get { return (IEnumerable)GetValue(ShiftSourceProperty); }
			set { SetValue(ShiftSourceProperty, value); }
		}

		private static void OnShiftSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleDay)sender).UpdateShiftView();
		}

		// ShiftView

		public static readonly DependencyProperty ShiftViewProperty =
			DependencyProperty.Register("ShiftView", typeof(IEnumerable), typeof(ScheduleDay), new PropertyMetadata(null));

		/// <summary> The source containing the set of Shift items. </summary>
		public IEnumerable ShiftView
		{
			get { return (IEnumerable)GetValue(ShiftViewProperty); }
			set { SetValue(ShiftViewProperty, value); }
		}

		// ShiftViewSource

		private void UpdateShiftView()
		{
			if (ShiftSource != null && !String.IsNullOrEmpty(ShiftDateMemberPath) && Date != DateTime.MinValue && (String.IsNullOrEmpty(ShiftGroupIDMemberPath) || GroupID != null))
			{
				var source = new CollectionViewSource();
				source.Filter += new FilterEventHandler(ShiftViewFilter);
				source.Source = ShiftSource;
				ShiftView = source.View;
				UpdateSelection();
			}
			else
				ShiftView = null;
		}

		private void ShiftViewFilter(object sender, FilterEventArgs args)
		{
			if (args.Item != null)
			{
				var type = args.Item.GetType();
				var date = (DateTime)type.GetProperty(ShiftDateMemberPath).GetValue(args.Item, new object[] { });
				args.Accepted = date == Date;
				
				if (!String.IsNullOrEmpty(ShiftGroupIDMemberPath))
				{
					var groupID = type.GetProperty(ShiftGroupIDMemberPath).GetValue(args.Item, new object[] { });
					args.Accepted = args.Accepted & groupID.Equals(GroupID);
				}
			}
			else
				args.Accepted = true;
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

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ScheduleAppointment();
		}
	}
}
