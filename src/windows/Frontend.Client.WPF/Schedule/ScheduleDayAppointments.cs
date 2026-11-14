using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	public class ScheduleDayAppointments : Panel
	{
		// Start
		
		/// <summary> Start Attached Dependency Property. </summary>
		public static readonly DependencyProperty StartProperty =
			DependencyProperty.RegisterAttached("Start", typeof(DateTime), typeof(ScheduleDayAppointments), new FrameworkPropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(OnTimesChanged)));

		/// <summary> Gets the Start property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static DateTime GetStart(DependencyObject objectValue)
		{
			return (DateTime)objectValue.GetValue(StartProperty);
		}

		/// <summary> Sets the Start property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static void SetStart(DependencyObject objectValue, DateTime value)
		{
			objectValue.SetValue(StartProperty, value);
		}

		// End
		
		/// <summary> End Attached Dependency Property. </summary>
		public static readonly DependencyProperty EndProperty =
			DependencyProperty.RegisterAttached("End", typeof(DateTime), typeof(ScheduleDayAppointments), new FrameworkPropertyMetadata(DateTime.MinValue, new PropertyChangedCallback(OnTimesChanged)));

		/// <summary> Gets the End property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static DateTime GetEnd(DependencyObject objectValue)
		{
			return (DateTime)objectValue.GetValue(EndProperty);
		}

		/// <summary> Sets the End property.  This dependency property specifies the time position of child elements of this panel. </summary>
		public static void SetEnd(DependencyObject objectValue, DateTime value)
		{
			objectValue.SetValue(EndProperty, value);
		}

		/// <summary> Handles changes to the StartTime property. </summary>
		private static void OnTimesChanged(DependencyObject objectValue, DependencyPropertyChangedEventArgs args)
		{
			var element = objectValue as UIElement;
			if (element != null)
			{
				var panel = VisualTreeHelper.GetParent(element) as ScheduleDayAppointments;
				if (panel != null)
					panel.InvalidateMeasure();
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

		private static object CoerceGranularity(DependencyObject sender, object tempValue)
		{
			return Math.Max(1, Math.Min(60, (int)tempValue));
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

		protected override Size MeasureOverride(Size availableSize)
		{
			var maxWidth = 0d;
			foreach (UIElement child in Children)
			{
				var duration = Math.Max((int)new TimeSpan(GetEnd(child).Ticks).TotalMinutes - (int)new TimeSpan(GetStart(child).Ticks).TotalMinutes, 0);
				child.Measure
				(
					new Size
					(
						availableSize.Width,
						Math.Max(duration / Granularity, 1) * BlockHeight
					)
				);
				maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
			}
				
			return 
				new Size
				(
					maxWidth,
					BlockHeight * (1440 / Granularity)
				);
		}
		
		private class Appointment
		{
			public Appointment(UIElement child, int start, int end)
			{
				_child = child;
				_start = start;
				_end = end;
			}
			
			private UIElement _child;
			public UIElement Child { get { return _child; } }
			
			private int _start;
			public int Start { get { return _start; } }
			
			private int _end;
			public int End { get { return _end; } }
		}
		
		private class Slot
		{
			private List<Appointment> _appointments = new List<Appointment>();
			public List<Appointment> Appointments { get { return _appointments; } }
			
			/// <summary>
			/// Determines the index at which the appointment could be inserted if it will fit in the slot, -1 otherwise.
			/// </summary>
			/// <param name="appointment">The appointment to be tested.</param>
			/// <returns>The index at which the appointment could be inserted if it will fit in the slot, -1 otherwise.</returns>
			public int FitIndex(Appointment appointment)
			{
				var lastEnd = 0;
				for (int index = 0; index < _appointments.Count; index++)
				{
					if ((appointment.Start >= lastEnd) && (appointment.End <= _appointments[index].Start))
						return index;
					
					if (lastEnd > appointment.Start)
						break;
					
					lastEnd = _appointments[index].End;
				}
				
				if (appointment.Start >= lastEnd)
					return _appointments.Count;
				
				return -1;
			}
			
			/// <summary>
			/// Inserts an appointment in the slot if there is an available time-slot.
			/// </summary>
			/// <param name="appointment">The appointment to be inserted.</param>
			/// <returns>The index of the appointment in the list if it could be inserted, -1 otherwise.</returns>
			public int Add(Appointment appointment)
			{
				int index = FitIndex(appointment);
				if (index >= 0)
					_appointments.Insert(index, appointment);
					
				return index;
			}
		}
		
		protected override Size ArrangeOverride(Size finalSize)
		{
			var slots = new List<Slot>();
			slots.Add(new Slot());
			
			foreach (UIElement child in Children)
			{
				var appointment = 
					new Appointment
					(
						child,
						(int)new TimeSpan(GetStart(child).Ticks).TotalMinutes,
						(int)new TimeSpan(GetEnd(child).Ticks).TotalMinutes
					);
					
				var slotIndex = 0;
				while (slots[slotIndex].Add(appointment) < 0)
				{
					slotIndex++;
					if (slots.Count <= slotIndex)
						slots.Add(new Slot());
				}
			}
			
			var defaultWidth = finalSize.Width / slots.Count;
			for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
			{
				foreach (Appointment appointment in slots[slotIndex].Appointments)
				{
					int nextSlotIndex = slotIndex + 1;
					while ((nextSlotIndex < slots.Count) && (slots[nextSlotIndex].FitIndex(appointment) >= 0))
						nextSlotIndex++;
						
					appointment.Child.Arrange
					(
						new Rect
						(
							defaultWidth * slotIndex,
							(appointment.Start - (int)new TimeSpan(StartTime.Ticks).TotalMinutes) / Granularity * BlockHeight,
							defaultWidth * (nextSlotIndex - slotIndex),
							Math.Max((appointment.End - appointment.Start) / Granularity, 1) * BlockHeight
						)
					);
				}
			}
			
			return finalSize;
		}
	}
}
