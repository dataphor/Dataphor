using System.ComponentModel;
using System.Windows;
using Alphora.Dataphor.Frontend.Client.WPF;
using System;
using System.Windows.Data;
using System.Collections.Generic;

namespace Frontend.Client.WPF.Tests
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();
			
			var LAppt = new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Do thing1", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(8.3d), EndTime = DateTime.MinValue + TimeSpan.FromHours(9d) };
			
			Week.AppointmentSource = 
				new List<ScheduleData>
				{
					LAppt,
					new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Go to town", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(11d), EndTime = DateTime.MinValue + TimeSpan.FromHours(12.5d) },
					new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Ride high", Group = "KarenB", StartTime = DateTime.MinValue + TimeSpan.FromHours(8.75d), EndTime = DateTime.MinValue + TimeSpan.FromHours(9d) },
					new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Go to to the place with the stuff", Group = "KarenB", StartTime = DateTime.MinValue + TimeSpan.FromHours(4d), EndTime = DateTime.MinValue + TimeSpan.FromHours(13.5d) },
					new ScheduleData { Date = DateTime.Parse("12/2/2009"), Description = "Smile", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(9.25d), EndTime = DateTime.MinValue + TimeSpan.FromHours(9.25d) },
					new ScheduleData { Date = DateTime.Parse("12/3/2009"), Description = "Jump up", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(13d), EndTime = DateTime.MinValue + TimeSpan.FromHours(13.75d) },
					new ScheduleData { Date = DateTime.Parse("12/4/2009"), Description = "Dop the wallup", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(6d), EndTime = DateTime.MinValue + TimeSpan.FromHours(8.5d) }
				};
			
			Week.GroupSource =
				new List<ScheduleGroupData>
				{
					new ScheduleGroupData { Group = "AdrianL", Description = "Adrian Lewis" },
					new ScheduleGroupData { Group = "KarenB", Description = "Karen Bolton" }
				};

			Week.ShiftSource =
				new List<ScheduleData>
				{
					LAppt,
					new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Go to town", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(11d), EndTime = DateTime.MinValue + TimeSpan.FromHours(5d) },
					new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Ride high", Group = "KarenB", StartTime = DateTime.MinValue + TimeSpan.FromHours(6d), EndTime = DateTime.MinValue + TimeSpan.FromHours(10d) },
					new ScheduleData { Date = DateTime.Parse("12/1/2009"), Description = "Go to to the place with the stuff", Group = "KarenB", StartTime = DateTime.MinValue + TimeSpan.FromHours(4d), EndTime = DateTime.MinValue + TimeSpan.FromHours(16d) },
					new ScheduleData { Date = DateTime.Parse("12/2/2009"), Description = "Smile", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(8d), EndTime = DateTime.MinValue + TimeSpan.FromHours(13d) },
					new ScheduleData { Date = DateTime.Parse("12/3/2009"), Description = "Jump up", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(13d), EndTime = DateTime.MinValue + TimeSpan.FromHours(17d) },
					new ScheduleData { Date = DateTime.Parse("12/4/2009"), Description = "Dop the wallup", Group = "AdrianL", StartTime = DateTime.MinValue + TimeSpan.FromHours(6d), EndTime = DateTime.MinValue + TimeSpan.FromHours(17.5d) }
				};

			Week.SelectedAppointment = LAppt;
		}
		
		private void ClearClicked(Object ASender, RoutedEventArgs AArgs)
		{
			Week.AppointmentSource = null;
		}

	}

	public class ScheduleGroupData : INotifyPropertyChanged
	{
		private object FGroup;
		/// <summary> Gets and sets the object data used to group items under this grouping. </summary>
		public object Group
		{
			get { return FGroup; }
			set
			{
				if (FGroup != value)
				{
					FGroup = value;
					NotifyPropertyChanged("Group");
				}
			}
		}

		private string FDescription;
		public string Description
		{
			get { return FDescription; }
			set
			{
				if (FDescription != value)
				{
					FDescription = value;
					NotifyPropertyChanged("Description");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string APropertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(APropertyName));
		}
	}

	public class ScheduleData : INotifyPropertyChanged
	{
		private DateTime FDate;
		public DateTime Date
		{
			get { return FDate; }
			set
			{
				if (FDate != value)
				{
					FDate = value;
					NotifyPropertyChanged("Date");
				}
			}
		}

		private DateTime FStartTime;
		public DateTime StartTime
		{
			get { return FStartTime; }
			set
			{
				if (FStartTime != value)
				{
					FStartTime = value;
					NotifyPropertyChanged("StartTime");
				}
			}
		}

		private DateTime FEndTime;
		public DateTime EndTime
		{
			get { return FEndTime; }
			set
			{
				if (FEndTime != value)
				{
					FEndTime = value;
					NotifyPropertyChanged("EndTime");
				}
			}
		}

		private object FGroup;
		/// <summary> Gets and sets the object data used to group this item. </summary>
		public object Group
		{
			get { return FGroup; }
			set
			{
				if (FGroup != value)
				{
					FGroup = value;
					NotifyPropertyChanged("Group");
				}
			}
		}

		private string FDescription;
		/// <summary> Gets and sets the textual description of the item. </summary>
		public string Description
		{
			get { return FDescription; }
			set
			{
				if (FDescription != value)
				{
					FDescription = value;
					NotifyPropertyChanged("Description");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string APropertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(APropertyName));
		}
	}
	
	public class FullDateToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return ((DateTime)value).ToString("dddd dd MMMM yyyy");
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class IsSelectedToBorderThicknessConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (bool)value ? new Thickness(2) : new Thickness(1);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
