using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms.Integration;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Frontend.Client.WPF;
using System.Windows.Data;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.Schedule')")]
	[DesignerCategory("Data Controls")]
	public class ScheduleDayGrouped : Element
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			AppointmentSource = null;
			ShiftSource = null;
			GroupSource = null;
		}

		// StartDate

		private DateTime FStartDate = DateTime.MinValue;
		[Alphora.Dataphor.BOP.DefaultValueMember("StartDateDefault")]
		[Description("The date of the first day shown in the schedule.")]
		public DateTime StartDate
		{
			get { return FStartDate; }
			set
			{
				if (FStartDate != value)
				{
					FStartDate = value;
					if (Active)
						InternalUpdateStartDate();
				}
			}
		}
		
		public DateTime StartDateDefault
		{
			get { return DateTime.MinValue; }
		}

		private void InternalUpdateStartDate()
		{
			FControl.StartDate = FStartDate;
		}

		// AppointmentSource

		private ISource FAppointmentSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the AppointmentSource node the control will be attached to.")]
		public ISource AppointmentSource
		{
			get { return FAppointmentSource; }
			set
			{
				if (FAppointmentSource != value)
					SetAppointmentSource(value);
			}
		}

		protected virtual void SetAppointmentSource(ISource AAppointmentSource)
		{
			if (FAppointmentSource != null)
				FAppointmentSource.Disposed -= new EventHandler(AppointmentSourceDisposed);
			FAppointmentSource = AAppointmentSource;
			if (FAppointmentSource != null)
				FAppointmentSource.Disposed += new EventHandler(AppointmentSourceDisposed);
			if (Active)
				InternalUpdateAppointmentSource();
		}

		protected virtual void AppointmentSourceDisposed(object ASender, EventArgs AArgs)
		{
			AppointmentSource = null;
		}

		private DataLink FAppointmentSourceLink;

		protected virtual void InternalUpdateAppointmentSource()
		{
			FAppointmentSourceLink.Source = AppointmentSource == null ? null : AppointmentSource.DataSource;
		}

		private void AppointmentSourceLinkRowChanged(DataLink ALInk, DataSet ADataSet, DataField AField)
		{
			UpdateAppointmentData();
		}

		private void AppointmentSourceLinkChanged(DataLink ALink, DataSet ADataSet)
		{
			if (!FNavigatingSelection)
				UpdateAppointmentData();
		}

		private bool FSettingSelection;
		private bool FNavigatingSelection;
		
		private void SelectedAppointmentChanged(object ASender, EventArgs AArgs)
		{
			if (!FSettingSelection && FAppointmentSourceLink != null && FAppointmentSourceLink.Active)
			{
				FNavigatingSelection = true;
				try
				{
					var LNewOffset = Array.IndexOf<ScheduleData>((ScheduleData[])FControl.AppointmentSource, (ScheduleData)FControl.SelectedAppointment);
					if (LNewOffset >= 0)
						FAppointmentSourceLink.DataSet.MoveBy(LNewOffset - FAppointmentSourceLink.ActiveOffset);
				}
				finally
				{
					FNavigatingSelection = false;
				}
			}
		}
		
		// AppointmentDateColumn

		private string FAppointmentDateColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the date.")]
		public string AppointmentDateColumn
		{
			get { return FAppointmentDateColumn; }
			set
			{
				if (FAppointmentDateColumn != value)
				{
					FAppointmentDateColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentStartTimeColumn

		private string FAppointmentStartTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the StartTime.")]
		public string AppointmentStartTimeColumn
		{
			get { return FAppointmentStartTimeColumn; }
			set
			{
				if (FAppointmentStartTimeColumn != value)
				{
					FAppointmentStartTimeColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentEndTimeColumn

		private string FAppointmentEndTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the EndTime.")]
		public string AppointmentEndTimeColumn
		{
			get { return FAppointmentEndTimeColumn; }
			set
			{
				if (FAppointmentEndTimeColumn != value)
				{
					FAppointmentEndTimeColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentGroupColumn

		private string FAppointmentGroupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the Group.")]
		public string AppointmentGroupColumn
		{
			get { return FAppointmentGroupColumn; }
			set
			{
				if (FAppointmentGroupColumn != value)
				{
					FAppointmentGroupColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentDescriptionColumn

		private string FAppointmentDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the Description.")]
		public string AppointmentDescriptionColumn
		{
			get { return FAppointmentDescriptionColumn; }
			set
			{
				if (FAppointmentDescriptionColumn != value)
				{
					FAppointmentDescriptionColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentData
		
		protected void UpdateAppointmentData()
		{
			if 
			(
				Active && FAppointmentSourceLink.Active && !FAppointmentSourceLink.DataSet.IsEmpty()
					&& !String.IsNullOrEmpty(FAppointmentDateColumn) && !String.IsNullOrEmpty(FAppointmentStartTimeColumn)
					&& !String.IsNullOrEmpty(FAppointmentEndTimeColumn) && !String.IsNullOrEmpty(FAppointmentDescriptionColumn)
			)
				ReconcileAppointmentData();
			else
				if (FControl != null)
					FControl.AppointmentSource = null;
		}

		private void ReconcileAppointmentData()
		{
			if (FControl != null)
			{
				// Expand the buffer to capture all rows in the set
				while (FAppointmentSourceLink.LastOffset == FAppointmentSourceLink.BufferCount - 1)
					FAppointmentSourceLink.BufferCount++;

				// Replace the appointment source
				ScheduleData LActiveItem = null;
				var LItems = new ScheduleData[FAppointmentSourceLink.LastOffset + 1];
				for (int i = 0; i <= FAppointmentSourceLink.LastOffset; i++)
				{
					var LRow = FAppointmentSourceLink.Buffer(i);
					var LItem =
						new ScheduleData
						{
							Date = ((Scalar)LRow.GetValue(FAppointmentDateColumn)).AsDateTime,
							StartTime = ((Scalar)LRow.GetValue(FAppointmentStartTimeColumn)).AsDateTime,
							EndTime = ((Scalar)LRow.GetValue(FAppointmentEndTimeColumn)).AsDateTime,
							Description = ((Scalar)LRow.GetValue(FAppointmentDescriptionColumn)).AsString,
							Group = (String.IsNullOrEmpty(FAppointmentGroupColumn) ? null : ((Scalar)LRow.GetValue(FAppointmentGroupColumn)).AsNative)
						};
					LItems[i] = LItem;
					if (i == FAppointmentSourceLink.ActiveOffset)
						LActiveItem = LItem;
				}
				FControl.AppointmentSource = LItems;
				FSettingSelection = true;
				try
				{
					FControl.SelectedAppointment = LActiveItem;
				}
				finally
				{
					FSettingSelection = false;
				}
			}
		}

		// ShiftSource

		private ISource FShiftSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the ShiftSource node the control will be attached to.")]
		public ISource ShiftSource
		{
			get { return FShiftSource; }
			set
			{
				if (FShiftSource != value)
					SetShiftSource(value);
			}
		}

		protected virtual void SetShiftSource(ISource AShiftSource)
		{
			if (FShiftSource != null)
				FShiftSource.Disposed -= new EventHandler(ShiftSourceDisposed);
			FShiftSource = AShiftSource;
			if (FShiftSource != null)
				FShiftSource.Disposed += new EventHandler(ShiftSourceDisposed);
			if (Active)
				InternalUpdateShiftSource();
		}

		protected virtual void ShiftSourceDisposed(object ASender, EventArgs AArgs)
		{
			ShiftSource = null;
		}

		private DataLink FShiftSourceLink;

		protected virtual void InternalUpdateShiftSource()
		{
			FShiftSourceLink.Source = ShiftSource == null ? null : ShiftSource.DataSource;
		}

		// ShiftDateColumn

		private string FShiftDateColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the date.")]
		public string ShiftDateColumn
		{
			get { return FShiftDateColumn; }
			set
			{
				if (FShiftDateColumn != value)
				{
					FShiftDateColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftStartTimeColumn

		private string FShiftStartTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Time.")]
		public string ShiftStartTimeColumn
		{
			get { return FShiftStartTimeColumn; }
			set
			{
				if (FShiftStartTimeColumn != value)
				{
					FShiftStartTimeColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftEndTimeColumn

		private string FShiftEndTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Time.")]
		public string ShiftEndTimeColumn
		{
			get { return FShiftEndTimeColumn; }
			set
			{
				if (FShiftEndTimeColumn != value)
				{
					FShiftEndTimeColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftGroupColumn

		private string FShiftGroupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Group.")]
		public string ShiftGroupColumn
		{
			get { return FShiftGroupColumn; }
			set
			{
				if (FShiftGroupColumn != value)
				{
					FShiftGroupColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftDescriptionColumn

		private string FShiftDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Description.")]
		public string ShiftDescriptionColumn
		{
			get { return FShiftDescriptionColumn; }
			set
			{
				if (FShiftDescriptionColumn != value)
				{
					FShiftDescriptionColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftData
		
		protected void UpdateShiftData()
		{
			var LActive = Active
				&& (ShiftSource != null) && (ShiftSource.DataView != null) && !ShiftSource.IsEmpty
				&& !String.IsNullOrEmpty(FShiftDateColumn) && !String.IsNullOrEmpty(FShiftStartTimeColumn)
				&& !String.IsNullOrEmpty(FShiftEndTimeColumn) && !String.IsNullOrEmpty(FShiftGroupColumn);
			if (LActive)
				ReconcileShiftData();
			//else
			//    if (FControl != null)
			//        FControl.ShiftSource = null;
		}

		private void ReconcileShiftData()
		{
			
		}

		// GroupSource

		private ISource FGroupSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the GroupSource node the control will be attached to.")]
		public ISource GroupSource
		{
			get { return FGroupSource; }
			set
			{
				if (FGroupSource != value)
					SetGroupSource(value);
			}
		}

		protected virtual void SetGroupSource(ISource AGroupSource)
		{
			if (FGroupSource != null)
				FGroupSource.Disposed -= new EventHandler(GroupSourceDisposed);
			FGroupSource = AGroupSource;
			if (FGroupSource != null)
				FGroupSource.Disposed += new EventHandler(GroupSourceDisposed);
			if (Active)
				InternalUpdateGroupSource();
		}

		protected virtual void GroupSourceDisposed(object ASender, EventArgs AArgs)
		{
			GroupSource = null;
		}

		private DataLink FGroupSourceLink;

		protected virtual void InternalUpdateGroupSource()
		{
			FGroupSourceLink.Source = GroupSource == null ? null : GroupSource.DataSource;
		}

		private void GroupSourceLinkRowChanged(DataLink ALInk, DataSet ADataSet, DataField AField)
		{
			UpdateGroupData();
		}

		private void GroupSourceLinkChanged(DataLink ALink, DataSet ADataSet)
		{
			UpdateGroupData();
		}

		// GroupColumn

		private string FGroupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("GroupSource")]
		[Description("The column in the Group source that represents the Group.")]
		public string GroupColumn
		{
			get { return FGroupColumn; }
			set
			{
				if (FGroupColumn != value)
				{
					FGroupColumn = value;
					UpdateGroupData();
				}
			}
		}

		// GroupDescriptionColumn

		private string FGroupDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("GroupSource")]
		[Description("The column in the Group source that represents the Description.")]
		public string GroupDescriptionColumn
		{
			get { return FGroupDescriptionColumn; }
			set
			{
				if (FGroupDescriptionColumn != value)
				{
					FGroupDescriptionColumn = value;
					UpdateGroupData();
				}
			}
		}

		// GroupConditionColumn

		private string FGroupConditionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("GroupSource")]
		[Description("The optional column in the Group source that represents the Condition.")]
		public string GroupConditionColumn
		{
			get { return FGroupConditionColumn; }
			set
			{
				if (FGroupConditionColumn != value)
				{
					FGroupConditionColumn = value;
					UpdateGroupData();
				}
			}
		}

		private void UpdateGroupData()
		{
			var LActive = Active
				&& (FGroupSourceLink != null) && FGroupSourceLink.Active && !FGroupSourceLink.DataSet.IsEmpty()
				&& !String.IsNullOrEmpty(FGroupColumn);
			if (LActive)
				ReconcileGroupData();
			else
				if (FControl != null)
					FControl.GroupSource = null;
		}

		private void ReconcileGroupData()
		{
			if (FControl != null)
			{
				// Expand the buffer to capture all rows in the set
				while (FGroupSourceLink.LastOffset == FGroupSourceLink.BufferCount - 1)
					FGroupSourceLink.BufferCount++;

				var LData = new List<ScheduleGroupData>(FGroupSourceLink.LastOffset + 1);
				for (int i = 0; i <= FGroupSourceLink.LastOffset; i++)
				{
					var LRow = FGroupSourceLink.Buffer(i);
					if (String.IsNullOrEmpty(FGroupConditionColumn) || ((Scalar)LRow.GetValue(FGroupConditionColumn)).AsBoolean)
						LData.Add
						(
							new ScheduleGroupData
							{
								Group = (LRow.HasValue(FGroupColumn) ? ((Scalar)LRow.GetValue(FGroupColumn)).AsNative : null),
								Description = 
								(
									String.IsNullOrEmpty(FGroupDescriptionColumn) 
										? null 
										: (LRow.HasValue(FGroupDescriptionColumn) ? ((Scalar)LRow.GetValue(FGroupDescriptionColumn)).AsString : null)
								),
							}
						);
				}
				FControl.GroupSource = LData;
			}
		}

		// Control

		private Scheduler FControl;
		private ElementHost FElementHost;

		// Element
		
		protected override void InternalLayout(System.Drawing.Rectangle ABounds)
		{
			FElementHost.Bounds = ABounds;                                          
		}

		// Node
		
		protected override void Activate()
		{
			// Ensure that the application and resources are initialized
			if (Application.Current == null)
			{
				new Application();
				Application.Current.Resources.MergedDictionaries.Add(Application.LoadComponent(new Uri("Alphora.Dataphor.Frontend.Client.Windows;component/Schedule.xaml", UriKind.Relative)) as ResourceDictionary);
			}

			FControl = CreateControl();
			FControl.Style = Application.Current.Resources[GetStyle()] as Style;
			DependencyPropertyDescriptor.FromProperty(Scheduler.SelectedAppointmentProperty, typeof(Scheduler)).AddValueChanged(FControl, new EventHandler(SelectedAppointmentChanged));
			InternalUpdateStartDate();
						
			FAppointmentSourceLink = new DataLink();
			FAppointmentSourceLink.OnDataChanged += new DataLinkHandler(AppointmentSourceLinkChanged);
			FAppointmentSourceLink.OnRowChanged += new DataLinkFieldHandler(AppointmentSourceLinkRowChanged);
			FAppointmentSourceLink.OnActiveChanged += new DataLinkHandler(AppointmentSourceLinkChanged);
			FShiftSourceLink = new DataLink();
			FGroupSourceLink = new DataLink();
			FGroupSourceLink.OnDataChanged += new DataLinkHandler(GroupSourceLinkChanged);
			FGroupSourceLink.OnRowChanged += new DataLinkFieldHandler(GroupSourceLinkRowChanged);
			FGroupSourceLink.OnActiveChanged += new DataLinkHandler(GroupSourceLinkChanged);
			
			InternalUpdateGroupSource();
			InternalUpdateAppointmentSource();
			InternalUpdateShiftSource();
			
			UpdateGroupData();
			UpdateShiftData();
			UpdateAppointmentData();
			
			FElementHost = new ElementHost();
			FElementHost.Parent = ((IWindowsContainerElement)Parent).Control;
			FElementHost.Child = FControl;
			
			base.Activate();
		}

		protected virtual Scheduler CreateControl()
		{
			return new Scheduler();
		}

		protected virtual string GetStyle()
		{
			return "DefaultSchedulerStyle";
		}

		protected override void Deactivate()
		{
			if (FAppointmentSourceLink != null)
			{
				FAppointmentSourceLink.Dispose();
				FAppointmentSourceLink = null;
			}
			if (FShiftSourceLink != null)
			{
				FShiftSourceLink.Dispose();
				FShiftSourceLink = null;
			}
			if (FGroupSourceLink != null)
			{
				FGroupSourceLink.Dispose();
				FGroupSourceLink = null;
			}
			if (FElementHost != null)
			{
				DependencyPropertyDescriptor.FromProperty(Scheduler.SelectedAppointmentProperty, typeof(Scheduler)).RemoveValueChanged(FControl, new EventHandler(SelectedAppointmentChanged));
				FElementHost.Dispose();
				FElementHost = null;
				FControl = null;
			}
			base.Deactivate();
		}
	}
	
	public class ScheduleWeekGrouped : ScheduleDayGrouped
	{
		protected override Scheduler CreateControl()
		{
			return new ScheduleWeek();
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
		/// <summary> Gets and sets the key for the schedule item. </summary>
		/// <remarks> This member does not raise property notification when modified. </remarks>
		public Row Row { get; set; }

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
