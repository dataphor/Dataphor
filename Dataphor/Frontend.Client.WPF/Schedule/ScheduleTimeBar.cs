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

		public const double CDefaultBlockHeight = 20d;
		
		private static readonly DependencyPropertyKey BlockHeightPropertyKey =
			DependencyProperty.RegisterReadOnly("BlockHeight", typeof(double), typeof(ScheduleTimeBar), new FrameworkPropertyMetadata(CDefaultBlockHeight));

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

		private static object CoerceGranularity(DependencyObject ASender, object AValue)
		{
			return Math.Max(1, Math.Min(60, (int)AValue));
		}

		private static void UpdateBlocks(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((ScheduleTimeBar)ASender).InvalidateMeasure();
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

		private static void HighlightedTimeChanged(DependencyObject ASender, DependencyPropertyChangedEventArgs AArgs)
		{
			((ScheduleTimeBar)ASender).HighlightedTimeChanged((DateTime?)AArgs.OldValue, (DateTime?)AArgs.NewValue);
		}

		private void HighlightedTimeChanged(DateTime? AOld, DateTime? ANew)
		{
			int LBlockIndex;
			if (AOld.HasValue)
			{
				LBlockIndex = FindBlock(AOld.Value);
				if (LBlockIndex >= 0)
					((ScheduleTimeBlock)Children[LBlockIndex]).IsHighlighted = false;
			}
			if (ANew.HasValue)
			{
				LBlockIndex = FindBlock(ANew.Value);
				if (LBlockIndex >= 0)
					((ScheduleTimeBlock)Children[LBlockIndex]).IsHighlighted = true;
			}
		}
		
		private void ChildHighlightChanged(object ASender, EventArgs AArgs)
		{
			var LChild = (ScheduleTimeBlock)ASender;
			if (LChild.IsHighlighted)
				HighlightedTime = LChild.Time;
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

		protected override Size MeasureOverride(Size AAvailableSize)
		{
			List<UIElement> LToDelete = new List<UIElement>(Children.Count);
			foreach (UIElement LChild in Children)
				LToDelete.Add(LChild);

			// Eliminate any blocks that are now before the start time
			while (Children.Count > 0 && ((ScheduleTimeBlock)Children[0]).Time < StartTime)
				RemoveChild(0);

			Size LResult = new Size(0d, 0d);
			var LSequence = 0;
			var LVisibleMinutes = 0;
			for (DateTime LTime = StartTime; LTime < new DateTime(TimeSpan.FromHours(24).Ticks) && LResult.Height < AAvailableSize.Height; LTime += TimeSpan.FromMinutes(Granularity))
			{
				ScheduleTimeBlock LBlock = (Children.Count > LSequence && ((ScheduleTimeBlock)Children[LSequence]).Time == LTime) ? (ScheduleTimeBlock)Children[LSequence] : null;
				if (LBlock == null)
				{
					LBlock = new ScheduleTimeBlock { Time = LTime };
					if (TimeBlockStyle != null)
						LBlock.Style = TimeBlockStyle;
					DependencyPropertyDescriptor.FromProperty(ScheduleTimeBlock.IsHighlightedProperty, typeof(ScheduleTimeBlock)).AddValueChanged(LBlock, new EventHandler(ChildHighlightChanged));
					Children.Insert(LSequence, LBlock);
				}
				LBlock.Measure(AAvailableSize);
				LResult.Height += LBlock.DesiredSize.Height;
				LResult.Width = Math.Max(LResult.Width, LBlock.DesiredSize.Width);
				LSequence++;
				LVisibleMinutes = (int)new TimeSpan(LTime.Ticks).TotalMinutes - (int)new TimeSpan(StartTime.Ticks).TotalMinutes;
			}
			
			// Eliminate any blocks that are off the end
			for (var i = LSequence; i < Children.Count; i++)
				RemoveChild(Children.Count - 1);
			
			SetBlockHeight(Math.Max(LSequence > 0 ? (LResult.Height / LSequence) : CDefaultBlockHeight, 1d));
			SetVisibleMinutes(LVisibleMinutes + (int)((AAvailableSize.Height - LResult.Height) / BlockHeight) * Granularity);
			
			return LResult;
		}
		
		private void RemoveChild(int AIndex)
		{
			DependencyPropertyDescriptor.FromProperty(ScheduleTimeBlock.IsHighlightedProperty, typeof(ScheduleTimeBlock)).RemoveValueChanged(Children[AIndex], new EventHandler(ChildHighlightChanged));
			Children.RemoveAt(AIndex);
		}
		
		/// <remarks> Assumes the child blocks are in time order. </remarks>
		private int FindBlock(DateTime ATime)
		{
			// TODO: Rewrite to use binary search
			var LFoundTime = DateTime.MinValue;
			for (int i = 0; i < Children.Count && ATime > LFoundTime; i++)
			{
				LFoundTime = ((ScheduleTimeBlock)Children[i]).Time;
				if (LFoundTime == ATime)
					return i;
			}
			return -1;
		}

		protected override Size ArrangeOverride(Size AFinalSize)
		{
			double LYOffset = 0d;
			foreach (FrameworkElement LChild in Children)
			{
				LChild.Arrange(new Rect(0d, LYOffset, AFinalSize.Width, Math.Max(0, Math.Min(LChild.DesiredSize.Height, AFinalSize.Height - LYOffset))));
				LYOffset += LChild.DesiredSize.Height;
			}
			return new Size(AFinalSize.Width, LYOffset);
		}
	}
}
