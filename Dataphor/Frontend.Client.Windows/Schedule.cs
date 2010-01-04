using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms.Integration;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.Frontend.Client.WPF;
using Alphora.Dataphor.BOP;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;

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

		// AppointmentImageColumn

		private string FAppointmentImageColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the Image.")]
		public string AppointmentImageColumn
		{
			get { return FAppointmentImageColumn; }
			set
			{
				if (FAppointmentImageColumn != value)
				{
					FAppointmentImageColumn = value;
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
				var LItems = new ScheduleAppointmentData[FAppointmentSourceLink.LastOffset + 1];
				for (int i = 0; i <= FAppointmentSourceLink.LastOffset; i++)
				{
					var LRow = FAppointmentSourceLink.Buffer(i);
					var LItem =
						new ScheduleAppointmentData
						{
							Date = ((Scalar)LRow.GetValue(FAppointmentDateColumn)).AsDateTime,
							StartTime = ((Scalar)LRow.GetValue(FAppointmentStartTimeColumn)).AsDateTime,
							EndTime = ((Scalar)LRow.GetValue(FAppointmentEndTimeColumn)).AsDateTime,
							Description = ((Scalar)LRow.GetValue(FAppointmentDescriptionColumn)).AsString,
							Group = (String.IsNullOrEmpty(FAppointmentGroupColumn) ? null : ((Scalar)LRow.GetValue(FAppointmentGroupColumn)).AsNative),
							Image = (String.IsNullOrEmpty(FAppointmentImageColumn) || !LRow.HasValue(FAppointmentImageColumn) 
								? null 
								: LoadImage((Scalar)LRow.GetValue(FAppointmentImageColumn)))
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
		
		// Appointment Image Loading
		
		private ImageSource LoadImage(Scalar AValue)
		{
			Stream LStream = AValue.OpenStream();
			try
			{
				var LImage = new BitmapImage();
				LImage.BeginInit();
				LImage.CacheOption = BitmapCacheOption.OnLoad;
				LImage.StreamSource = LStream;
				LImage.EndInit();
				return LImage;
			}
			finally
			{
				LStream.Close();
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

		private void ShiftSourceLinkRowChanged(DataLink ALInk, DataSet ADataSet, DataField AField)
		{
			UpdateShiftData();
		}

		private void ShiftSourceLinkChanged(DataLink ALink, DataSet ADataSet)
		{
			UpdateShiftData();
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
		
		// ShiftHighlightIntervalColumn

		private string FShiftHighlightIntervalColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the highlighting interval.")]
		public string ShiftHighlightIntervalColumn
		{
			get { return FShiftHighlightIntervalColumn; }
			set
			{
				if (FShiftHighlightIntervalColumn != value)
				{
					FShiftHighlightIntervalColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftData
		
		protected void UpdateShiftData()
		{
			if 
			(
				Active && FShiftSourceLink.Active && !FShiftSourceLink.DataSet.IsEmpty()
					&& !String.IsNullOrEmpty(FShiftDateColumn) && !String.IsNullOrEmpty(FShiftStartTimeColumn)
					&& !String.IsNullOrEmpty(FShiftEndTimeColumn) && !String.IsNullOrEmpty(FShiftDescriptionColumn)
			)
				ReconcileShiftData();
			else
				if (FControl != null)
					FControl.ShiftSource = null;
		}

		private void ReconcileShiftData()
		{
			if (FControl != null)
			{
				// Expand the buffer to capture all rows in the set
				while (FShiftSourceLink.LastOffset == FShiftSourceLink.BufferCount - 1)
					FShiftSourceLink.BufferCount++;

				var LData = new List<ScheduleData>(FShiftSourceLink.LastOffset + 1);
				for (int i = 0; i <= FShiftSourceLink.LastOffset; i++)
				{
					var LRow = FShiftSourceLink.Buffer(i);
					LData.Add
					(
						new ScheduleShiftData
						{
							Date = ((Scalar)LRow.GetValue(FShiftDateColumn)).AsDateTime,
							StartTime = ((Scalar)LRow.GetValue(FShiftStartTimeColumn)).AsDateTime,
							EndTime = ((Scalar)LRow.GetValue(FShiftEndTimeColumn)).AsDateTime,
							Description = ((Scalar)LRow.GetValue(FShiftDescriptionColumn)).AsString,
							Group = (String.IsNullOrEmpty(FShiftGroupColumn) ? null : ((Scalar)LRow.GetValue(FShiftGroupColumn)).AsNative),
							HighlightInterval = 
							(
								String.IsNullOrEmpty(FShiftHighlightIntervalColumn) 
									? (int?)null 
									: 
									(
										LRow.HasValue(FShiftHighlightIntervalColumn) 
											? ((Scalar)LRow.GetValue(FShiftHighlightIntervalColumn)).AsInt32
											: (int?)null
									)
							)
						}
					);
				}
				FControl.ShiftSource = LData;
			}
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
				Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(HandleUnhandledWPFException);
			}

			FControl = CreateControl();
			FControl.Style = Application.Current.Resources[GetStyle()] as Style;
			FControl.AddHandler(System.Windows.Controls.ContextMenuService.ContextMenuOpeningEvent, new RoutedEventHandler(ContextMenuOpened));
			DependencyPropertyDescriptor.FromProperty(Scheduler.SelectedAppointmentProperty, typeof(Scheduler)).AddValueChanged(FControl, new EventHandler(SelectedAppointmentChanged));
			FControl.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(ControlDoubleClicked);
			FControl.KeyDown += new System.Windows.Input.KeyEventHandler(ControlKeyDown);
			InternalUpdateStartDate();
						
			FAppointmentSourceLink = new DataLink();
			FAppointmentSourceLink.OnDataChanged += new DataLinkHandler(AppointmentSourceLinkChanged);
			FAppointmentSourceLink.OnRowChanged += new DataLinkFieldHandler(AppointmentSourceLinkRowChanged);
			FAppointmentSourceLink.OnActiveChanged += new DataLinkHandler(AppointmentSourceLinkChanged);
			FShiftSourceLink = new DataLink();
			FShiftSourceLink.OnDataChanged += new DataLinkHandler(ShiftSourceLinkChanged);
			FShiftSourceLink.OnRowChanged += new DataLinkFieldHandler(ShiftSourceLinkRowChanged);
			FShiftSourceLink.OnActiveChanged += new DataLinkHandler(ShiftSourceLinkChanged);
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

		private void HandleUnhandledWPFException(object ASender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs AArgs)
		{
			AArgs.Handled = true;
			Session.HandleException(AArgs.Exception);
		}

		protected virtual Scheduler CreateControl()
		{
			return new Scheduler();
		}

		protected virtual string GetStyle()
		{
			return "DefaultSchedulerStyle";
		}

		private void ContextMenuOpened(object ASender, RoutedEventArgs AArgs)
		{
			var LItem = AArgs.OriginalSource as DependencyObject;
			if (LItem != null)
			{
				FrameworkElement LElement = Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>(LItem);
				if (LElement != null)
				{
					AArgs.Handled = true;
					var LMenu = new ContextMenu();
					BuildMenu(LMenu, typeof(ScheduleTimeBlockVerb), LElement);
					if (LMenu.Items.Count > 0)
					{
						LElement.ContextMenu = LMenu;
						LMenu.IsOpen = true;
					}
				}
				else
				{
					LElement = Utilities.GetAncestorOfTypeInVisualTree<ScheduleAppointment>(LItem);
					if (LElement != null)
					{
						AArgs.Handled = true;
						var LMenu = new ContextMenu();
						BuildMenu(LMenu, typeof(ScheduleAppointmentVerb), LElement);
						if (LMenu.Items.Count > 0)
						{
							LElement.ContextMenu = LMenu;
							LMenu.IsOpen = true;
						}
					}
				}
			}
		}

		protected virtual void BuildMenu(ContextMenu AMenu, Type AType, object AOwner)
		{
			var LFirst = true;
			foreach (INode LChild in Children)
			{
				if (AType.IsAssignableFrom(LChild.GetType()))
				{
					var LVerb = LChild as BaseVerb;
					if (LVerb != null)
					{
						var LMenuItem = LVerb.BuildMenuItem(AOwner);
						if (LFirst)
						{
							LMenuItem.FontWeight = FontWeights.Bold;
							LFirst = false;
						}
						AMenu.Items.Add(LMenuItem);
					}
				}
			}
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

		public override bool IsValidChild(Type AChildType)
		{
			return typeof(BaseVerb).IsAssignableFrom(AChildType)
				|| base.IsValidChild(AChildType);
		}

		private void ControlDoubleClicked(object ASender, System.Windows.Input.MouseButtonEventArgs AArgs)
		{
			// Double click executes the enabled appointment verb if on an appointment, or block verb if on a block
			var LItem = AArgs.OriginalSource as DependencyObject;
			if (LItem != null)
			{
				FrameworkElement LElement = Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>(LItem);
				if (LElement != null)
				{
					AArgs.Handled = true;
					var LVerb = FindFirstVerb(typeof(ScheduleTimeBlockVerb));
					if (LVerb != null && LVerb.GetEnabled())
						LVerb.Execute(AArgs.OriginalSource);
				}
				else
				{
					LElement = Utilities.GetAncestorOfTypeInVisualTree<ScheduleAppointment>(LItem);
					if (LElement != null)
					{
						AArgs.Handled = true;
						var LVerb = FindFirstVerb(typeof(ScheduleAppointmentVerb));
						if (LVerb != null && LVerb.GetEnabled())
							LVerb.Execute(AArgs.OriginalSource);
					}
				}
			}
		}

		private BaseVerb FindFirstVerb(Type AType)
		{
			foreach (INode LChild in Children)
			{
				if (AType.IsAssignableFrom(LChild.GetType()))
				{
					var LVerb = LChild as BaseVerb;
					if (LVerb != null && LVerb.Enabled)
						return LVerb;
				}
			}
			return null;
		}

		private void ControlKeyDown(object ASender, System.Windows.Input.KeyEventArgs AArgs)
		{
			var LItem = AArgs.OriginalSource as DependencyObject;
			if (LItem != null)
			{
				FrameworkElement LElement = Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>(LItem);
				if (LElement != null)
				{
					var LVerb = FindVerbForKey(typeof(ScheduleTimeBlockVerb), AArgs.Key);
					if (LVerb != null && LVerb.GetEnabled())
					{
						AArgs.Handled = true;
						LVerb.Execute(AArgs.OriginalSource);
					}
				}
				else
				{
					LElement = Utilities.GetAncestorOfTypeInVisualTree<ScheduleAppointment>(LItem);
					if (LElement != null)
					{
						var LVerb = FindVerbForKey(typeof(ScheduleAppointmentVerb), AArgs.Key);
						if (LVerb != null && LVerb.GetEnabled())
						{
							AArgs.Handled = true;
							LVerb.Execute(AArgs.OriginalSource);
						}
					}
				}
			}
		}

		private BaseVerb FindVerbForKey(Type AType, System.Windows.Input.Key AKey)
		{
			foreach (INode LChild in Children)
			{
				if (AType.IsAssignableFrom(LChild.GetType()))
				{
					var LVerb = LChild as BaseVerb;
					if (LVerb != null && LVerb.Enabled && LVerb.KeyboardShortcut != null && LVerb.KeyboardShortcut.Key == AKey && LVerb.KeyboardShortcut.Modifiers == Keyboard.Modifiers)
						return LVerb;
				}
			}
			return null;
		}
	}

	public abstract class BaseVerb : Node
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}
		
		// Action

		protected IAction FAction;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed when this control is pressed.")]
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (FAction != value)
				{
					if (FAction != null)
						FAction.Disposed -= new EventHandler(ActionDisposed);
					FAction = value;
					if (FAction != null)
						FAction.Disposed += new EventHandler(ActionDisposed);
				}
			}
		}

		protected void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}

		// Text

		private string FText = String.Empty;
		[Publish(PublishMethod.Value)]
		[DefaultValue("")]
		[Description("A text string that will be used on the context menu.  If this is not set the text property of the action will be used.")]
		public string Text
		{
			get { return FText; }
			set { FText = value; }
		}

		public string GetText()
		{
			if ((Action != null) && (FText == String.Empty))
				return Action.Text;
			else
				return FText;
		}

		// Enabled

		private bool FEnabled = true;
		[DefaultValue(true)]
		[Description("When this is set to false this context menu item will be disabled.  This control will also be disabled if the action is disabled.")]
		public bool Enabled
		{
			get { return FEnabled; }
			set { FEnabled = value; }
		}

		/// <summary> Gets whether the node is actuall enabled (accounting for action). </summary>
		/// <remarks>
		///		The enabled state of the node is the most restrictive between 
		///		the action and the Enabled property.
		///	</remarks>
		public bool GetEnabled()
		{
			return (Action == null ? false : Action.GetEnabled()) && FEnabled;
		}
		
		// KeyboardShortcut
		
		private KeyGesture FKeyboardShortcut = null;
		public KeyGesture KeyboardShortcut
		{
			get { return FKeyboardShortcut; }
			set { FKeyboardShortcut = value; }
		}
		
		protected internal virtual MenuItem BuildMenuItem(object AOwner)
		{
			var LItem = 
				new MenuItem
				{
					Header = GetText().Replace('&', '_'),
					IsEnabled = GetEnabled(),
					Icon = (Action == null ? null : new System.Windows.Controls.Image() { Source = WPFInteropUtility.ImageToBitmapSource(((Action)Action).LoadedImage) }),
					Tag = AOwner,
					InputGestureText = FKeyboardShortcut == null  ? "" : new KeyGestureConverter().ConvertToString(FKeyboardShortcut)
				};
			LItem.Click += new RoutedEventHandler(ItemClicked);
			return LItem;
		}
		
		protected virtual void ItemClicked(object ASender, RoutedEventArgs AArgs)
		{
			if (Action != null)
			{
				AArgs.Handled = true;
				Execute((FrameworkElement)((MenuItem)ASender).Tag);
			}
		}

		public abstract void Execute(object ASender);
	}

	[DesignerImage("Image('Frontend', 'Nodes.Schedule')")]
	[DesignerCategory("Data Controls")]
	public class ScheduleAppointmentVerb : BaseVerb
	{
		public override void Execute(object ASender)
		{
			var LAppointment = WPF.Utilities.GetAncestorOfTypeInVisualTree<ListBoxItem>((DependencyObject)ASender);
			var LDay = WPF.Utilities.GetAncestorOfTypeInVisualTree<ScheduleDay>((DependencyObject)ASender);
			var LParams = new EventParams();
			if (LAppointment != null)
			{
				LParams.Add("AStartTime", ScheduleDayAppointments.GetStart(LAppointment));
				LParams.Add("AEndTime", ScheduleDayAppointments.GetEnd(LAppointment));
			}
			if (LDay != null)
			{
				LParams.Add("ADate", LDay.Date);
				LParams.Add("AGroup", LDay.GroupID);
			}
			Action.Execute(this, LParams);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.Schedule')")]
	[DesignerCategory("Data Controls")]
	public class ScheduleTimeBlockVerb : BaseVerb
	{
		public override void Execute(object ASender)
		{
			var LTimeBlock = WPF.Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>((DependencyObject)ASender);
			var LDay = WPF.Utilities.GetAncestorOfTypeInVisualTree<ScheduleDay>((DependencyObject)ASender);
			var LParams = new EventParams();
			if (LTimeBlock != null)
				LParams.Add("ATime", LTimeBlock.Time);
			if (LDay != null)
			{
				LParams.Add("ADate", LDay.Date);
				LParams.Add("AGroup", LDay.GroupID);
			}
			Action.Execute(this, LParams);
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
	
	public class ScheduleAppointmentData : ScheduleData
	{
		private ImageSource FImage;
		public ImageSource Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					NotifyPropertyChanged("Image");
				}
			}
		}
	}
	
	public class ScheduleShiftData : ScheduleData
	{
		private int? FHighlightInterval;
		public int? HighlightInterval
		{
			get { return FHighlightInterval; }
			set
			{
				if (FHighlightInterval != value)
				{
					FHighlightInterval = value;
					NotifyPropertyChanged("HighlightInterval");
				}
			}
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

	public class BooleanToBorderBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var LBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x55, 0x55, 0xFF));
			if (!(bool)value)
				LBrush.Opacity = 0d;
			return LBrush;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
