using System;
using Alphora.Dataphor.DAE.Client;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Forms.Integration;
using Alphora.Dataphor.Frontend.Client.WPF;
using System.Collections.Generic;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class Schedule : Element
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			AppointmentSource = null;
			ShiftSource = null;
			GroupSource = null;
		}

		// AppointmentSource

		private IAppointmentSource FAppointmentSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the AppointmentSource node the control will be attached to.")]
		public IAppointmentSource AppointmentSource
		{
			get { return FAppointmentSource; }
			set
			{
				if (FAppointmentSource != value)
					SetAppointmentSource(value);
			}
		}

		protected virtual void SetAppointmentSource(IAppointmentSource AAppointmentSource)
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
			FAppointmentSourceLink.Source = AppointmentSource.DataAppointmentSource;
		}
		
		// AppointmentDateColumn

		private string FAppointmentDateColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
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
		
		private ObservableCollection<ScheduleData> FAppointmentData = new ObservableCollection<ScheduleData>();
		
		protected void UpdateAppointmentData()
		{
			if 
			(
				Active && FAppointmentSourceLink.Active && !FAppointmentSourceLink.DataSet.IsEmpty()
					&& !String.IsNullOrEmpty(FAppointmentDateColumn) && !String.IsNullOrEmpty(FAppointmentStartTimeColumn)
					&& !String.IsNullOrEmpty(FAppointmentEndTimeColumn) && !String.IsNullOrEmpty(FAppointmentGroupColumn) 
					&& !String.IsNullOrEmpty(FAppointmentDescriptionColumn)
			)
				ReconcileAppointmentData();
			else
				if (FWeekControl != null)
					FWeekControl.AppointmentSource = null;
		}

		private void ReconcileAppointmentData()
		{
			if (FWeekControl != null)
			{
				// Expand the buffer to capture all rows in the set
				while (FAppointmentSourceLink.LastOffset == FAppointmentSourceLink.BufferCount - 1)
					FAppointmentSourceLink.BufferCount++;

				// TODO: Reconciliation
				//var LToDelete = new List<ScheduleData>(FAppointmentData);
				
				//for (int i = 0; i <= FAppointmentSourceLink.LastOffset; i++)
				//{
				//}
				
				//foreach (ScheduleData LData in LToDelete)
				//{
				//}
				
				// Replace the appointment source
				var LItems = new List<ScheduleData>(FAppointmentSourceLink.LastOffset + 1);
				for (int i = 0; i <= FAppointmentSourceLink.LastOffset; i++)
					LItems.Add
					(
						new ScheduleData
						{
							Date = ((Scalar)FAppointmentSourceLink.Buffer(i).GetValue(FAppointmentDateColumn)).AsDateTime,
							StartTime = ((Scalar)FAppointmentSourceLink.Buffer(i).GetValue(FAppointmentStartTimeColumn)).AsDateTime,
							EndTime = ((Scalar)FAppointmentSourceLink.Buffer(i).GetValue(FAppointmentEndTimeColumn)).AsDateTime,
						}
					);
				FWeekControl.AppointmentSource = LItems;
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
			FShiftSourceLink.Source = ShiftSource.DataSource;
		}

		// ShiftDateColumn

		private string FShiftDateColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
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
		
		private ObservableCollection<Shift> FShiftData = new ObservableCollection<Shift>();
		
		protected void UpdateShiftData()
		{
			var LActive = Active
				&& (ShiftSource != null) && (ShiftSource.DataView != null) && !ShiftSource.IsEmpty
				&& !String.IsNullOrEmpty(FShiftDateColumn) && !String.IsNullOrEmpty(FShiftStartTimeColumn)
				&& !String.IsNullOrEmpty(FShiftEndTimeColumn) && !String.IsNullOrEmpty(FShiftGroupColumn);
			if (LActive)
				ReconcileShiftData();
			else
				FShiftData.Clear();
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
			FGroupSourceLink.Source = GroupSource.DataSource;
		}

		// GroupIDColumn

		private string FGroupIDColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the Shift source that represents the Description.")]
		public string GroupIDColumn
		{
			get { return FGroupIDColumn; }
			set
			{
				if (FGroupIDColumn != value)
				{
					FGroupIDColumn = value;
					UpdateGroupData();
				}
			}
		}

		// GroupDescriptionColumn

		private string FGroupDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the Shift source that represents the Description.")]
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

		private void UpdateGroupData()
		{
			var LActive = Active
				&& (GroupSource != null) && (GroupSource.DataView != null) && !GroupSource.IsEmpty
				&& !String.IsNullOrEmpty(FGroupIDColumn) && !String.IsNullOrEmpty(FGroupDescriptionColumn);
			if (LActive)
				ReconcileGroupData();
			else
				if (FWeekControl != null)
					FWeekControl.GroupSource = null;
		}

		private void ReconcileGroupData()
		{
			FWeekControl.GroupSource = FGroupData;
		}

		// WeekControl

		private ScheduleWeek FWeekControl;
		private ElementHost FElementHost;

		// Element
		
		protected override void InternalLayout(System.Drawing.Rectangle ABounds)
		{
			FElementHost.Bounds = ABounds;                                          
		}

		// Node
		
		protected override void Activate()
		{
			FWeekControl = new ScheduleWeek();
						
			FAppointmentSourceLink = new DataLink();
			FShiftSourceLink = new DataLink();
			FGroupSourceLink = new DataLink();
			
			InternalUpdateGroupSource();
			InternalUpdateAppointmentSource();
			InternalUpdateShiftSource();
			
			UpdateGroupData();
			UpdateShiftData();
			UpdateAppointmentData();
			
			FElementHost = new ElementHost();
			FElementHost.Parent = ((IWindowsContainerElement)Parent).Control;
			FElementHost.Child = FWeekControl;
			
			base.Activate();
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
				FElementHost.Dispose();
				FElementHost = null;
				FWeekControl = null;
			}
			base.Deactivate();
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
		public Row Key { get; set; }

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
}
