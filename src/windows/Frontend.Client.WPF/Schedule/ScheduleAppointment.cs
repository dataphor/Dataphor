using System;
using System.Windows;
using System.Windows.Controls;

namespace Alphora.Dataphor.Frontend.Client.WPF
{
	/// <summary> Scheduled appointment. </summary>
	public class ScheduleAppointment : ListBoxItem
	{
		static ScheduleAppointment()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ScheduleAppointment), new FrameworkPropertyMetadata(typeof(ScheduleAppointment)));
		}
	}
}
