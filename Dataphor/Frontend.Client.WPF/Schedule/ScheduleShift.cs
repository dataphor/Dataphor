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

		private static object CoerceGranularity(DependencyObject ASender, object AValue)
		{
			return Math.Max(1, Math.Min(60, (int)AValue));
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

		protected override Size MeasureOverride(Size AAvailableSize)
		{
			const double CMaxHeight = 15000d;	// Keep the number of highlights finite
			
			if (HighlightInterval != null && HighlightInterval.HasValue)
			{
				var LIndex = 0;
				var LYOffset = 0d;
				var LInterval = HighlightInterval;
				while (LYOffset < Math.Min(CMaxHeight, AAvailableSize.Height))
				{
					if (LIndex >= Children.Count)
					{
						var LHighlight = new Rectangle();
						if (HighlightStyle != null)
							LHighlight.Style = HighlightStyle;
						Children.Add(LHighlight);
					}
					Children[LIndex].Measure(AAvailableSize);
					LYOffset += BlockHeight * ((double)LInterval / Granularity);
					LIndex++;
				}
				return new Size(0d, 0d);
			}
			else
				return base.MeasureOverride(AAvailableSize);
		}

		protected override Size ArrangeOverride(Size AFinalSize)
		{
			if (HighlightInterval != null && HighlightInterval.HasValue)
			{
				var LYOffset = 0d;
				for (int LIndex = 0; LIndex < Children.Count; LIndex++)
				{
					var LHighlightHeight = BlockHeight * ((double)HighlightInterval / Granularity);
					var LChild = Children[LIndex];
					LChild.Arrange(new Rect(0d, LYOffset, AFinalSize.Width, LHighlightHeight));
					LYOffset += LHighlightHeight;
				}
				return AFinalSize;
			}
			else
				return base.ArrangeOverride(AFinalSize);
		}
	}
}
