using System.ComponentModel;
using System.Windows;
using Alphora.Dataphor.Frontend.Client.WPF;
using System;
using System.Windows.Data;

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
			
			Week.AppointmentSource = 
				new Appointment[]
				{
					new Appointment { Date = DateTime.Parse("12/1/2009"), Description = "Do thing1", ProviderID = "AdrianL", StartTime = TimeSpan.FromHours(8.3d), EndTime = TimeSpan.FromHours(9d) },
					new Appointment { Date = DateTime.Parse("12/1/2009"), Description = "Go to town", ProviderID = "AdrianL", StartTime = TimeSpan.FromHours(11d), EndTime = TimeSpan.FromHours(12.5d) },
					new Appointment { Date = DateTime.Parse("12/1/2009"), Description = "Ride high", ProviderID = "KarenB", StartTime = TimeSpan.FromHours(8.75d), EndTime = TimeSpan.FromHours(9d) },
					new Appointment { Date = DateTime.Parse("12/1/2009"), Description = "Go to to the place with the stuff", ProviderID = "KarenB", StartTime = TimeSpan.FromHours(4d), EndTime = TimeSpan.FromHours(13.5d) },
					new Appointment { Date = DateTime.Parse("12/2/2009"), Description = "Smile", ProviderID = "AdrianL", StartTime = TimeSpan.FromHours(9.25d), EndTime = TimeSpan.FromHours(9.25d) },
					new Appointment { Date = DateTime.Parse("12/3/2009"), Description = "Jump up", ProviderID = "AdrianL", StartTime = TimeSpan.FromHours(13d), EndTime = TimeSpan.FromHours(13.75d) },
					new Appointment { Date = DateTime.Parse("12/4/2009"), Description = "Dop the wallup", ProviderID = "AdrianL", StartTime = TimeSpan.FromHours(6d), EndTime = TimeSpan.FromHours(8.5d) }
				};
			
			Week.GroupSource =
				new Provider[]
				{
					new Provider { ProviderID = "AdrianL", Description = "Adrian Lewis" },
					new Provider { ProviderID = "KarenB", Description = "Karen Bolton" }
				};
		}
	}
	
	public class Provider : INotifyPropertyChanged
	{
		private string FProviderID;
		public string ProviderID
		{
			get { return FProviderID; }
			set
			{
				if (FProviderID != value)
				{
					FProviderID = value;
					NotifyPropertyChanged("ProviderID");
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
	
	public class Appointment : IDayGroupItem, INotifyPropertyChanged
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

		private string FProviderID;
		public string ProviderID
		{
			get { return FProviderID; }
			set
			{
				if (FProviderID != value)
				{
					FProviderID = value;
					NotifyPropertyChanged("ProviderID");
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

		private TimeSpan FStartTime;
		public TimeSpan StartTime
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

		private TimeSpan FEndTime;
		public TimeSpan EndTime
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
