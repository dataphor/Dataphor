using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> A week's schedule. </summary>
	public class ScheduleWeek : Scheduler
	{
		static ScheduleWeek()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleWeek), new FrameworkPropertyMetadata(typeof(ScheduleWeek)));
		}

		// Days

		private static readonly DependencyPropertyKey DaysPropertyKey =
			DependencyProperty.RegisterReadOnly("Days", typeof(DateTime[]), typeof(Scheduler), new FrameworkPropertyMetadata(null));

		public static readonly DependencyProperty DaysProperty = DaysPropertyKey.DependencyProperty;

		/// <summary> Gets the Days property.  This dependency property 
		/// indicates the date of each day represented in the week. </summary>
		public DateTime[] Days
		{
			get { return (DateTime[])GetValue(DaysProperty); }
		}

		/// <summary> Provides a secure method for setting the Days property.  
		/// This dependency property indicates the date of each day represented in the week. </summary>
		protected void SetDays(DateTime[] value)
		{
			SetValue(DaysPropertyKey, value);
		}

		protected override void StartDateChanged(DateTime oldValue, DateTime newValue)
		{
			UpdateDays();
		}

		private void UpdateDays()
		{
			var days = new DateTime[7];
			for (int i = 0; i < 7; i++)
				days[i] = StartDate + TimeSpan.FromDays(i);
			SetDays(days);
		}

		public override void OnApplyTemplate()
		{
			UpdateDays();
			base.OnApplyTemplate();
		}
	}
}
