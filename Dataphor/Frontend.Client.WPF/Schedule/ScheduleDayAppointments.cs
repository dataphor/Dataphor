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
			DependencyProperty.RegisterAttached("Start", typeof(TimeSpan), typeof(ScheduleDayAppointments), new FrameworkPropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnTimesChanged)));

		/// <summary> Gets the Start property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static TimeSpan GetStart(DependencyObject AObject)
		{
			return (TimeSpan)AObject.GetValue(StartProperty);
		}

		/// <summary> Sets the Start property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static void SetStart(DependencyObject AObject, TimeSpan value)
		{
			AObject.SetValue(StartProperty, value);
		}

		// End
		
		/// <summary> End Attached Dependency Property. </summary>
		public static readonly DependencyProperty EndProperty =
			DependencyProperty.RegisterAttached("End", typeof(TimeSpan), typeof(ScheduleDayAppointments), new FrameworkPropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnTimesChanged)));

		/// <summary> Gets the End property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static TimeSpan GetEnd(DependencyObject AObject)
		{
			return (TimeSpan)AObject.GetValue(EndProperty);
		}

		/// <summary> Sets the End property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static void SetEnd(DependencyObject AObject, TimeSpan value)
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
			DependencyProperty.Register("StartTime", typeof(TimeSpan), typeof(ScheduleDayAppointments), new PropertyMetadata(TimeSpan.FromHours(8), new PropertyChangedCallback(UpdateAppointments)));

		/// <summary> The first visible time value </summary>
		public TimeSpan StartTime
		{
			get { return (TimeSpan)GetValue(StartTimeProperty); }
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
				var LDuration = Math.Max((int)GetEnd(LChild).TotalMinutes - (int)GetStart(LChild).TotalMinutes, 0);
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
				var LStart = (int)GetStart(LChild).TotalMinutes;
				var LEnd = (int)GetEnd(LChild).TotalMinutes;
				LChild.Arrange
				(
					new Rect
					(
						0,
						(LStart / Granularity * BlockHeight) - (int)StartTime.TotalMinutes,
						AFinalSize.Width,
						Math.Max((LEnd - LStart) / Granularity, 1) * BlockHeight
					)
				);
			}
			return AFinalSize;
		}
	}
}
