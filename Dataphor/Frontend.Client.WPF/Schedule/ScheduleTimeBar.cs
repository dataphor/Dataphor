using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> A time of day bar to display time within a day. </summary>
	public class ScheduleTimeBar : Panel
	{
		static ScheduleTimeBar()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleTimeBar), new FrameworkPropertyMetadata(typeof(ScheduleTimeBar)));
		}

		// StartTime
		
		public static readonly DependencyProperty StartTimeProperty =
			DependencyProperty.Register("StartTime", typeof(DateTime), typeof(ScheduleTimeBar), new PropertyMetadata(new DateTime(TimeSpan.FromHours(8).Ticks), new PropertyChangedCallback(UpdateBlocks)));

		/// <summary> The first visible time value </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
			set { SetValue(StartTimeProperty, value); }
		}

		// VisibleMinutes
		
		private static readonly DependencyPropertyKey VisibleMinutesPropertyKey =
			DependencyProperty.RegisterReadOnly("VisibleMinutes", typeof(int), typeof(ScheduleTimeBar), new FrameworkPropertyMetadata(0));

		public static readonly DependencyProperty VisibleMinutesProperty = VisibleMinutesPropertyKey.DependencyProperty;

		/// <summary> Gets the VisibleMinutes property.  This dependency property 
		/// indicates the number of minutes visible in the ScheduleTimeBar. </summary>
		public int VisibleMinutes
		{
			get { return (int)GetValue(VisibleMinutesProperty); }
		}

		/// <summary> Provides a secure method for setting the VisibleMinutes property.  
		/// This dependency property indicates the number of minutes visible in the ScheduleTimeBar. </summary>
		protected void SetVisibleMinutes(int value)
		{
			SetValue(VisibleMinutesPropertyKey, value);
		}

		// BlockHeight

		public const double DefaultBlockHeight = 20d;
		
		private static readonly DependencyPropertyKey BlockHeightPropertyKey =
			DependencyProperty.RegisterReadOnly("BlockHeight", typeof(double), typeof(ScheduleTimeBar), new FrameworkPropertyMetadata(DefaultBlockHeight));

		public static readonly DependencyProperty BlockHeightProperty = BlockHeightPropertyKey.DependencyProperty;

		/// <summary> Gets the BlockHeight property.  This dependency property 
		/// indicates the height of a time block. </summary>
		public double BlockHeight
		{
			get { return (double)GetValue(BlockHeightProperty); }
		}

		/// <summary> Provides a secure method for setting the BlockHeight property.  
		/// This dependency property indicates the height of a time block. </summary>
		protected void SetBlockHeight(double value)
		{
			SetValue(BlockHeightPropertyKey, value);
		}
		
		// Granularity
		
		public static readonly DependencyProperty GranularityProperty =
			DependencyProperty.Register("Granularity", typeof(int), typeof(ScheduleTimeBar), new PropertyMetadata(15, new PropertyChangedCallback(UpdateBlocks), new CoerceValueCallback(CoerceGranularity)));

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

		private static void UpdateBlocks(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleTimeBar)sender).InvalidateMeasure();
		}

		// HighlightedTime
		
		public static readonly DependencyProperty HighlightedTimeProperty =
			DependencyProperty.Register("HighlightedTime", typeof(DateTime?), typeof(ScheduleTimeBar), new PropertyMetadata(null, new PropertyChangedCallback(HighlightedTimeChanged)));

		/// <summary> The time block that is currently highlighted. </summary>
		public DateTime? HighlightedTime
		{
			get { return (DateTime?)GetValue(HighlightedTimeProperty); }
			set { SetValue(HighlightedTimeProperty, value); }
		}

		private static void HighlightedTimeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((ScheduleTimeBar)sender).HighlightedTimeChanged((DateTime?)args.OldValue, (DateTime?)args.NewValue);
		}

		private void HighlightedTimeChanged(DateTime? old, DateTime? newValue)
		{
			int blockIndex;
			if (old.HasValue)
			{
				blockIndex = FindBlock(old.Value);
				if (blockIndex >= 0)
					((ScheduleTimeBlock)Children[blockIndex]).IsHighlighted = false;
			}
			if (newValue.HasValue)
			{
				blockIndex = FindBlock(newValue.Value);
				if (blockIndex >= 0)
					((ScheduleTimeBlock)Children[blockIndex]).IsHighlighted = true;
			}
		}
		
		private void ChildHighlightChanged(object sender, EventArgs args)
		{
			var child = (ScheduleTimeBlock)sender;
			if (child.IsHighlighted)
				HighlightedTime = child.Time;
			else
				HighlightedTime = null;
		}

		// SelectedTimeStart
		
		public static readonly DependencyProperty SelectedTimeStartProperty =
			DependencyProperty.Register("SelectedTimeStart", typeof(DateTime?), typeof(ScheduleTimeBar), new PropertyMetadata(null));

		/// <summary> The start of the time selection. </summary>
		public DateTime? SelectedTimeStart
		{
			get { return (DateTime?)GetValue(SelectedTimeStartProperty); }
			set { SetValue(SelectedTimeStartProperty, value); }
		}

		// SelectedTimeEnd
		
		public static readonly DependencyProperty SelectedTimeEndProperty =
			DependencyProperty.Register("SelectedTimeEnd", typeof(DateTime?), typeof(ScheduleTimeBar), new PropertyMetadata(null));

		/// <summary> The end of the time selection. </summary>
		public DateTime? SelectedTimeEnd
		{
			get { return (DateTime?)GetValue(SelectedTimeEndProperty); }
			set { SetValue(SelectedTimeEndProperty, value); }
		}

		// TimeBlockStyle

		public static readonly DependencyProperty TimeBlockStyleProperty =
			DependencyProperty.Register("TimeBlockStyle", typeof(Style), typeof(ScheduleTimeBar), new PropertyMetadata(null));

		/// <summary> The style to use for child time blocks. </summary>
		public Style TimeBlockStyle
		{
			get { return (Style)GetValue(TimeBlockStyleProperty); }
			set { SetValue(TimeBlockStyleProperty, value); }
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			List<UIElement> toDelete = new List<UIElement>(Children.Count);
			foreach (UIElement child in Children)
				toDelete.Add(child);

			// Eliminate any blocks that are now before the start time
			while (Children.Count > 0 && ((ScheduleTimeBlock)Children[0]).Time < StartTime)
				RemoveChild(0);

			Size result = new Size(0d, 0d);
			var sequence = 0;
			var visibleMinutes = 0;
			for (DateTime time = StartTime; time < new DateTime(TimeSpan.FromHours(24).Ticks) && result.Height < availableSize.Height; time += TimeSpan.FromMinutes(Granularity))
			{
				ScheduleTimeBlock block = (Children.Count > sequence && ((ScheduleTimeBlock)Children[sequence]).Time == time) ? (ScheduleTimeBlock)Children[sequence] : null;
				if (block == null)
				{
					block = new ScheduleTimeBlock { Time = time };
					if (TimeBlockStyle != null)
						block.Style = TimeBlockStyle;
					DependencyPropertyDescriptor.FromProperty(ScheduleTimeBlock.IsHighlightedProperty, typeof(ScheduleTimeBlock)).AddValueChanged(block, new EventHandler(ChildHighlightChanged));
					Children.Insert(sequence, block);
				}
				block.Measure(availableSize);
				result.Height += block.DesiredSize.Height;
				result.Width = Math.Max(result.Width, block.DesiredSize.Width);
				sequence++;
				visibleMinutes = (int)new TimeSpan(time.Ticks).TotalMinutes - (int)new TimeSpan(StartTime.Ticks).TotalMinutes;
			}
			
			// Eliminate any blocks that are off the end
			for (var i = sequence; i < Children.Count; i++)
				RemoveChild(Children.Count - 1);
			
			SetBlockHeight(Math.Max(sequence > 0 ? (result.Height / sequence) : DefaultBlockHeight, 1d));
			SetVisibleMinutes(visibleMinutes + (int)((availableSize.Height - result.Height) / BlockHeight) * Granularity);
			
			return result;
		}
		
		private void RemoveChild(int index)
		{
			DependencyPropertyDescriptor.FromProperty(ScheduleTimeBlock.IsHighlightedProperty, typeof(ScheduleTimeBlock)).RemoveValueChanged(Children[index], new EventHandler(ChildHighlightChanged));
			Children.RemoveAt(index);
		}
		
		/// <remarks> Assumes the child blocks are in time order. </remarks>
		private int FindBlock(DateTime time)
		{
			// TODO: Rewrite to use binary search
			var foundTime = DateTime.MinValue;
			for (int i = 0; i < Children.Count && time > foundTime; i++)
			{
				foundTime = ((ScheduleTimeBlock)Children[i]).Time;
				if (foundTime == time)
					return i;
			}
			return -1;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double yOffset = 0d;
			foreach (FrameworkElement child in Children)
			{
				child.Arrange(new Rect(0d, yOffset, finalSize.Width, Math.Max(0, Math.Min(child.DesiredSize.Height, finalSize.Height - yOffset))));
				yOffset += child.DesiredSize.Height;
			}
			return new Size(finalSize.Width, yOffset);
		}
	}
}
