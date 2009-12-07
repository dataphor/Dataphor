using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	public class ScheduleDayAppointments : Panel
	{
		// Start
		
		/// <summary> Start Attached Dependency Property. </summary>
		public static readonly DependencyProperty StartProperty =
			DependencyProperty.RegisterAttached("Start", typeof(DateTime), typeof(ScheduleDayAppointments), new FrameworkPropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(OnTimesChanged)));

		/// <summary> Gets the Start property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static DateTime GetStart(DependencyObject AObject)
		{
			return (DateTime)AObject.GetValue(StartProperty);
		}

		/// <summary> Sets the Start property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static void SetStart(DependencyObject AObject, DateTime value)
		{
			AObject.SetValue(StartProperty, value);
		}

		// End
		
		/// <summary> End Attached Dependency Property. </summary>
		public static readonly DependencyProperty EndProperty =
			DependencyProperty.RegisterAttached("End", typeof(DateTime), typeof(ScheduleDayAppointments), new FrameworkPropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(OnTimesChanged)));

		/// <summary> Gets the End property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static DateTime GetEnd(DependencyObject AObject)
		{
			return (DateTime)AObject.GetValue(EndProperty);
		}

		/// <summary> Sets the End property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static void SetEnd(DependencyObject AObject, DateTime value)
		{
			AObject.SetValue(EndProperty, value);
		}

		/// <summary> Handles changes to the StartTime property. </summary>
		private static void OnTimesChanged(DependencyObject AObject, DependencyPropertyChangedEventArgs AArgs)
		{
			var LElement = AObject as UIElement;
			if (LElement != null)
			{
				var LPanel = VisualTreeHelper.GetParent(LElement) as ScheduleDayAppointments;
				if (LPanel != null)
					LPanel.InvalidateMeasure();
			}
		}

		// StartTime
		
		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(DateTime), typeof(ScheduleDayAppointments), new PropertyMetadata(new DateTime(TimeSpan.FromHours(8).Ticks), new PropertyChangedCallback(UpdateAppointments)));

		/// <summary> The first visible time value </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		// Granularity

		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleDayAppointments), new PropertyMetadata(15, new PropertyChangedCallback(UpdateAppointments), new CoerceValueCallback(CoerceGranularity)));

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

		// BlockHeight
		
		public static readonly DependencyProperty BlockHeightProperty =
			DependencyProperty.Register("BlockHeight", typeof(double), typeof(ScheduleDayAppointments), new PropertyMetadata(20d, new PropertyChangedCallback(UpdateAppointments)));

		/// <summary> The height of each time block. </summary>
		public double BlockHeight
		{
			get { return (double)GetValue(BlockHeightProperty); }
			set { SetValue(BlockHeightProperty, value); }
		}

		private static void UpdateAppointments(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((ScheduleDayAppointments)d).InvalidateMeasure();
		}

		protected override Size MeasureOverride(Size AAvailableSize)
		{
			var LMaxWidth = 0d;
			foreach (UIElement LChild in Children)
			{
				var LDuration = Math.Max((int)new TimeSpan(GetEnd(LChild).Ticks).TotalMinutes - (int)new TimeSpan(GetStart(LChild).Ticks).TotalMinutes, 0);
				LChild.Measure
				(
					new Size
					(
						AAvailableSize.Width,
						Math.Max(LDuration / Granularity, 1) * BlockHeight
					)
				);
				LMaxWidth = Math.Max(LMaxWidth, LChild.DesiredSize.Width);
			}
				
			return 
				new Size
				(
					LMaxWidth,
					BlockHeight * (1440 / Granularity)
				);
		}

		protected override Size ArrangeOverride(Size AFinalSize)
		{
			foreach (UIElement LChild in Children)
			{
				var LStart = (int)new TimeSpan(GetStart(LChild).Ticks).TotalMinutes;
				var LEnd = (int)new TimeSpan(GetEnd(LChild).Ticks).TotalMinutes;
				LChild.Arrange
				(
					new Rect
					(
						0,
						(LStart - (int)new TimeSpan(StartTime.Ticks).TotalMinutes) / Granularity * BlockHeight,
						AFinalSize.Width,
						Math.Max((LEnd - LStart) / Granularity, 1) * BlockHeight
					)
				);
			}
			return AFinalSize;
		}
	}
}
