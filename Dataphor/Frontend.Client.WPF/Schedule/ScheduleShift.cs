using System;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Shapes;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	public class ScheduleShift : Panel
	{
		// BlockHeight

		public static readonly DependencyProperty BlockHeightProperty =
			DependencyProperty.Register("BlockHeight", typeof(double), typeof(ScheduleShift), new FrameworkPropertyMetadata(20d, FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary> The height of each time block. </summary>
		public double BlockHeight
		{
			get { return (double)GetValue(BlockHeightProperty); }
			set { SetValue(BlockHeightProperty, value); }
		}

		// Granularity

		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleShift), new FrameworkPropertyMetadata(15, FrameworkPropertyMetadataOptions.AffectsMeasure, null, new CoerceValueCallback(CoerceGranularity)));

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

		// HighlightInterval

		public static readonly DependencyProperty HighlightIntervalProperty =
			DependencyProperty.Register("HighlightInterval", typeof(int?), typeof(ScheduleShift), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary> The interval between highlight markers. </summary>
		public int? HighlightInterval
		{
			get { return (int?)GetValue(HighlightIntervalProperty); }
			set { SetValue(HighlightIntervalProperty, value); }
		}

		// HighlightStyle

		public static readonly DependencyProperty HighlightStyleProperty =
			DependencyProperty.Register("HighlightStyle", typeof(Style), typeof(ScheduleShift), new FrameworkPropertyMetadata(null));

		/// <summary> The style to use for highlighters. </summary>
		public Style HighlightStyle
		{
			get { return (Style)GetValue(HighlightStyleProperty); }
			set { SetValue(HighlightStyleProperty, value); }
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			const double CMaxHeight = 15000d;	// Keep the number of highlights finite
			
			if (HighlightInterval != null && HighlightInterval.HasValue)
			{
				var index = 0;
				var yOffset = 0d;
				var interval = HighlightInterval;
				while (yOffset < Math.Min(CMaxHeight, availableSize.Height))
				{
					if (index >= Children.Count)
					{
						var highlight = new Rectangle();
						if (HighlightStyle != null)
							highlight.Style = HighlightStyle;
						Children.Add(highlight);
					}
					Children[index].Measure(availableSize);
					yOffset += BlockHeight * ((double)interval / Granularity);
					index++;
				}
				return new Size(0d, 0d);
			}
			else
				return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (HighlightInterval != null && HighlightInterval.HasValue)
			{
				var yOffset = 0d;
				for (int index = 0; index < Children.Count; index++)
				{
					var highlightHeight = BlockHeight * ((double)HighlightInterval / Granularity);
					var child = Children[index];
					child.Arrange(new Rect(0d, yOffset, finalSize.Width, highlightHeight));
					yOffset += highlightHeight;
				}
				return finalSize;
			}
			else
				return base.ArrangeOverride(finalSize);
		}
	}
}
