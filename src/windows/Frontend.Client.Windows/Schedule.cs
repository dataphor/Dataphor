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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			AppointmentSource = null;
			ShiftSource = null;
			GroupSource = null;
		}

		// StartDate

		private DateTime _startDate = DateTime.MinValue;
		[Alphora.Dataphor.BOP.DefaultValueMember("StartDateDefault")]
		[Description("The date of the first day shown in the schedule.")]
		public DateTime StartDate
		{
			get { return _startDate; }
			set
			{
				if (_startDate != value)
				{
					_startDate = value;
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
			_control.StartDate = _startDate;
		}

		// AppointmentSource

		private ISource _appointmentSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the AppointmentSource node the control will be attached to.")]
		public ISource AppointmentSource
		{
			get { return _appointmentSource; }
			set
			{
				if (_appointmentSource != value)
					SetAppointmentSource(value);
			}
		}

		protected virtual void SetAppointmentSource(ISource appointmentSource)
		{
			if (_appointmentSource != null)
				_appointmentSource.Disposed -= new EventHandler(AppointmentSourceDisposed);
			_appointmentSource = appointmentSource;
			if (_appointmentSource != null)
				_appointmentSource.Disposed += new EventHandler(AppointmentSourceDisposed);
			if (Active)
				InternalUpdateAppointmentSource();
		}

		protected virtual void AppointmentSourceDisposed(object sender, EventArgs args)
		{
			AppointmentSource = null;
		}

		private DataLink _appointmentSourceLink;

		protected virtual void InternalUpdateAppointmentSource()
		{
			_appointmentSourceLink.Source = AppointmentSource == null ? null : AppointmentSource.DataSource;
		}

		private void AppointmentSourceLinkRowChanged(DataLink lInk, DataSet dataSet, DataField field)
		{
			UpdateAppointmentData();
		}

		private void AppointmentSourceLinkChanged(DataLink link, DataSet dataSet)
		{
			if (!_navigatingSelection)
				UpdateAppointmentData();
		}

		private bool _settingSelection;
		private bool _navigatingSelection;
		
		private void SelectedAppointmentChanged(object sender, EventArgs args)
		{
			if (!_settingSelection && _appointmentSourceLink != null && _appointmentSourceLink.Active)
			{
				_navigatingSelection = true;
				try
				{
					var newOffset = Array.IndexOf<ScheduleData>((ScheduleData[])_control.AppointmentSource, (ScheduleData)_control.SelectedAppointment);
					if (newOffset >= 0)
						_appointmentSourceLink.DataSet.MoveBy(newOffset - _appointmentSourceLink.ActiveOffset);
				}
				finally
				{
					_navigatingSelection = false;
				}
			}
		}
		
		// AppointmentDateColumn

		private string _appointmentDateColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the date.")]
		public string AppointmentDateColumn
		{
			get { return _appointmentDateColumn; }
			set
			{
				if (_appointmentDateColumn != value)
				{
					_appointmentDateColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentStartTimeColumn

		private string _appointmentStartTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the StartTime.")]
		public string AppointmentStartTimeColumn
		{
			get { return _appointmentStartTimeColumn; }
			set
			{
				if (_appointmentStartTimeColumn != value)
				{
					_appointmentStartTimeColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentEndTimeColumn

		private string _appointmentEndTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the EndTime.")]
		public string AppointmentEndTimeColumn
		{
			get { return _appointmentEndTimeColumn; }
			set
			{
				if (_appointmentEndTimeColumn != value)
				{
					_appointmentEndTimeColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentGroupColumn

		private string _appointmentGroupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the Group.")]
		public string AppointmentGroupColumn
		{
			get { return _appointmentGroupColumn; }
			set
			{
				if (_appointmentGroupColumn != value)
				{
					_appointmentGroupColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentDescriptionColumn

		private string _appointmentDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the Description.")]
		public string AppointmentDescriptionColumn
		{
			get { return _appointmentDescriptionColumn; }
			set
			{
				if (_appointmentDescriptionColumn != value)
				{
					_appointmentDescriptionColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentImageColumn

		private string _appointmentImageColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the Image.")]
		public string AppointmentImageColumn
		{
			get { return _appointmentImageColumn; }
			set
			{
				if (_appointmentImageColumn != value)
				{
					_appointmentImageColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentBackgroundColorColumn

		private string _appointmentBackgroundColorColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("AppointmentSource")]
		[Description("The column in the appointment source that represents the BackgroundColor.")]
		public string AppointmentBackgroundColorColumn
		{
			get { return _appointmentBackgroundColorColumn; }
			set
			{
				if (_appointmentBackgroundColorColumn != value)
				{
					_appointmentBackgroundColorColumn = value;
					UpdateAppointmentData();
				}
			}
		}

		// AppointmentData
		
		protected void UpdateAppointmentData()
		{
			if 
			(
				Active && _appointmentSourceLink.Active && !_appointmentSourceLink.DataSet.IsEmpty()
					&& !String.IsNullOrEmpty(_appointmentDateColumn) && !String.IsNullOrEmpty(_appointmentStartTimeColumn)
					&& !String.IsNullOrEmpty(_appointmentEndTimeColumn) && !String.IsNullOrEmpty(_appointmentDescriptionColumn)
			)
				ReconcileAppointmentData();
			else
				if (_control != null)
					_control.AppointmentSource = null;
		}

		private void ReconcileAppointmentData()
		{
			if (_control != null)
			{
				// Expand the buffer to capture all rows in the set
				while (_appointmentSourceLink.LastOffset == _appointmentSourceLink.BufferCount - 1)
					_appointmentSourceLink.BufferCount++;

				// Replace the appointment source
				ScheduleData activeItem = null;
				var items = new ScheduleAppointmentData[_appointmentSourceLink.LastOffset + 1];
				for (int i = 0; i <= _appointmentSourceLink.LastOffset; i++)
				{
					var row = _appointmentSourceLink.Buffer(i);
					var item =
						new ScheduleAppointmentData
						{
							Date = ((IScalar)row.GetValue(_appointmentDateColumn)).AsDateTime,
							StartTime = ((IScalar)row.GetValue(_appointmentStartTimeColumn)).AsDateTime,
							EndTime = ((IScalar)row.GetValue(_appointmentEndTimeColumn)).AsDateTime,
							Description = ((IScalar)row.GetValue(_appointmentDescriptionColumn)).AsString,
							Group = (String.IsNullOrEmpty(_appointmentGroupColumn) ? null : ((IScalar)row.GetValue(_appointmentGroupColumn)).AsNative),
							Image = (String.IsNullOrEmpty(_appointmentImageColumn) || !row.HasValue(_appointmentImageColumn) 
								? null 
								: LoadImage((IScalar)row.GetValue(_appointmentImageColumn))),
							BackgroundColor = (String.IsNullOrEmpty(_appointmentBackgroundColorColumn) || !row.HasValue(_appointmentBackgroundColorColumn)
								? String.Empty
								: ((IScalar)row.GetValue(_appointmentBackgroundColorColumn)).AsString)
						};
					items[i] = item;
					if (i == _appointmentSourceLink.ActiveOffset)
						activeItem = item;
				}
				_control.AppointmentSource = items;
				_settingSelection = true;
				try
				{
					_control.SelectedAppointment = activeItem;
				}
				finally
				{
					_settingSelection = false;
				}
			}
		}
		
		// Appointment Image Loading
		
		private ImageSource LoadImage(IScalar value)
		{
			Stream stream = value.OpenStream();
			try
			{
				var image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = stream;
				image.EndInit();
				return image;
			}
			finally
			{
				stream.Close();
			}
		}

		// ShiftSource

		private ISource _shiftSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the ShiftSource node the control will be attached to.")]
		public ISource ShiftSource
		{
			get { return _shiftSource; }
			set
			{
				if (_shiftSource != value)
					SetShiftSource(value);
			}
		}

		protected virtual void SetShiftSource(ISource shiftSource)
		{
			if (_shiftSource != null)
				_shiftSource.Disposed -= new EventHandler(ShiftSourceDisposed);
			_shiftSource = shiftSource;
			if (_shiftSource != null)
				_shiftSource.Disposed += new EventHandler(ShiftSourceDisposed);
			if (Active)
				InternalUpdateShiftSource();
		}

		protected virtual void ShiftSourceDisposed(object sender, EventArgs args)
		{
			ShiftSource = null;
		}

		private DataLink _shiftSourceLink;

		protected virtual void InternalUpdateShiftSource()
		{
			_shiftSourceLink.Source = ShiftSource == null ? null : ShiftSource.DataSource;
		}

		private void ShiftSourceLinkRowChanged(DataLink lInk, DataSet dataSet, DataField field)
		{
			UpdateShiftData();
		}

		private void ShiftSourceLinkChanged(DataLink link, DataSet dataSet)
		{
			UpdateShiftData();
		}

		// ShiftDateColumn

		private string _shiftDateColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the date.")]
		public string ShiftDateColumn
		{
			get { return _shiftDateColumn; }
			set
			{
				if (_shiftDateColumn != value)
				{
					_shiftDateColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftStartTimeColumn

		private string _shiftStartTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Time.")]
		public string ShiftStartTimeColumn
		{
			get { return _shiftStartTimeColumn; }
			set
			{
				if (_shiftStartTimeColumn != value)
				{
					_shiftStartTimeColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftEndTimeColumn

		private string _shiftEndTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Time.")]
		public string ShiftEndTimeColumn
		{
			get { return _shiftEndTimeColumn; }
			set
			{
				if (_shiftEndTimeColumn != value)
				{
					_shiftEndTimeColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftGroupColumn

		private string _shiftGroupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Group.")]
		public string ShiftGroupColumn
		{
			get { return _shiftGroupColumn; }
			set
			{
				if (_shiftGroupColumn != value)
				{
					_shiftGroupColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftDescriptionColumn

		private string _shiftDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the Description.")]
		public string ShiftDescriptionColumn
		{
			get { return _shiftDescriptionColumn; }
			set
			{
				if (_shiftDescriptionColumn != value)
				{
					_shiftDescriptionColumn = value;
					UpdateShiftData();
				}
			}
		}
		
		// ShiftHighlightIntervalColumn

		private string _shiftHighlightIntervalColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("ShiftSource")]
		[Description("The column in the Shift source that represents the highlighting interval.")]
		public string ShiftHighlightIntervalColumn
		{
			get { return _shiftHighlightIntervalColumn; }
			set
			{
				if (_shiftHighlightIntervalColumn != value)
				{
					_shiftHighlightIntervalColumn = value;
					UpdateShiftData();
				}
			}
		}

		// ShiftData
		
		protected void UpdateShiftData()
		{
			if 
			(
				Active && _shiftSourceLink.Active && !_shiftSourceLink.DataSet.IsEmpty()
					&& !String.IsNullOrEmpty(_shiftDateColumn) && !String.IsNullOrEmpty(_shiftStartTimeColumn)
					&& !String.IsNullOrEmpty(_shiftEndTimeColumn) && !String.IsNullOrEmpty(_shiftDescriptionColumn)
			)
				ReconcileShiftData();
			else
				if (_control != null)
					_control.ShiftSource = null;
		}

		private void ReconcileShiftData()
		{
			if (_control != null)
			{
				// Expand the buffer to capture all rows in the set
				while (_shiftSourceLink.LastOffset == _shiftSourceLink.BufferCount - 1)
					_shiftSourceLink.BufferCount++;

				var data = new List<ScheduleData>(_shiftSourceLink.LastOffset + 1);
				for (int i = 0; i <= _shiftSourceLink.LastOffset; i++)
				{
					var row = _shiftSourceLink.Buffer(i);
					data.Add
					(
						new ScheduleShiftData
						{
							Date = ((IScalar)row.GetValue(_shiftDateColumn)).AsDateTime,
							StartTime = ((IScalar)row.GetValue(_shiftStartTimeColumn)).AsDateTime,
							EndTime = ((IScalar)row.GetValue(_shiftEndTimeColumn)).AsDateTime,
							Description = ((IScalar)row.GetValue(_shiftDescriptionColumn)).AsString,
							Group = (String.IsNullOrEmpty(_shiftGroupColumn) ? null : ((IScalar)row.GetValue(_shiftGroupColumn)).AsNative),
							HighlightInterval = 
							(
								String.IsNullOrEmpty(_shiftHighlightIntervalColumn) 
									? (int?)null 
									: 
									(
										row.HasValue(_shiftHighlightIntervalColumn) 
											? ((IScalar)row.GetValue(_shiftHighlightIntervalColumn)).AsInt32
											: (int?)null
									)
							)
						}
					);
				}
				_control.ShiftSource = data;
			}
		}

		// GroupSource

		private ISource _groupSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the GroupSource node the control will be attached to.")]
		public ISource GroupSource
		{
			get { return _groupSource; }
			set
			{
				if (_groupSource != value)
					SetGroupSource(value);
			}
		}

		protected virtual void SetGroupSource(ISource groupSource)
		{
			if (_groupSource != null)
				_groupSource.Disposed -= new EventHandler(GroupSourceDisposed);
			_groupSource = groupSource;
			if (_groupSource != null)
				_groupSource.Disposed += new EventHandler(GroupSourceDisposed);
			if (Active)
				InternalUpdateGroupSource();
		}

		protected virtual void GroupSourceDisposed(object sender, EventArgs args)
		{
			GroupSource = null;
		}

		private DataLink _groupSourceLink;

		protected virtual void InternalUpdateGroupSource()
		{
			_groupSourceLink.Source = GroupSource == null ? null : GroupSource.DataSource;
		}

		private void GroupSourceLinkRowChanged(DataLink lInk, DataSet dataSet, DataField field)
		{
			UpdateGroupData();
		}

		private void GroupSourceLinkChanged(DataLink link, DataSet dataSet)
		{
			UpdateGroupData();
		}

		// GroupColumn

		private string _groupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("GroupSource")]
		[Description("The column in the Group source that represents the Group.")]
		public string GroupColumn
		{
			get { return _groupColumn; }
			set
			{
				if (_groupColumn != value)
				{
					_groupColumn = value;
					UpdateGroupData();
				}
			}
		}

		// GroupDescriptionColumn

		private string _groupDescriptionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("GroupSource")]
		[Description("The column in the Group source that represents the Description.")]
		public string GroupDescriptionColumn
		{
			get { return _groupDescriptionColumn; }
			set
			{
				if (_groupDescriptionColumn != value)
				{
					_groupDescriptionColumn = value;
					UpdateGroupData();
				}
			}
		}

		// GroupConditionColumn

		private string _groupConditionColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[ColumnNameSourceProperty("GroupSource")]
		[Description("The optional column in the Group source that represents the Condition.")]
		public string GroupConditionColumn
		{
			get { return _groupConditionColumn; }
			set
			{
				if (_groupConditionColumn != value)
				{
					_groupConditionColumn = value;
					UpdateGroupData();
				}
			}
		}

		private void UpdateGroupData()
		{
			var active = Active
				&& (_groupSourceLink != null) && _groupSourceLink.Active && !_groupSourceLink.DataSet.IsEmpty()
				&& !String.IsNullOrEmpty(_groupColumn);
			if (active)
				ReconcileGroupData();
			else
				if (_control != null)
					_control.GroupSource = null;
		}

		private void ReconcileGroupData()
		{
			if (_control != null)
			{
				// Expand the buffer to capture all rows in the set
				while (_groupSourceLink.LastOffset == _groupSourceLink.BufferCount - 1)
					_groupSourceLink.BufferCount++;

				var data = new List<ScheduleGroupData>(_groupSourceLink.LastOffset + 1);
				for (int i = 0; i <= _groupSourceLink.LastOffset; i++)
				{
					var row = _groupSourceLink.Buffer(i);
					if (String.IsNullOrEmpty(_groupConditionColumn) || ((IScalar)row.GetValue(_groupConditionColumn)).AsBoolean)
						data.Add
						(
							new ScheduleGroupData
							{
								Group = (row.HasValue(_groupColumn) ? ((IScalar)row.GetValue(_groupColumn)).AsNative : null),
								Description = 
								(
									String.IsNullOrEmpty(_groupDescriptionColumn) 
										? null 
										: (row.HasValue(_groupDescriptionColumn) ? ((IScalar)row.GetValue(_groupDescriptionColumn)).AsString : null)
								),
							}
						);
				}
				_control.GroupSource = data;
			}
		}

		// Control

		private Scheduler _control;
		private ElementHost _elementHost;

		// Element
		
		protected override void InternalLayout(System.Drawing.Rectangle bounds)
		{
			_elementHost.Bounds = bounds;                                          
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

			_control = CreateControl();
			_control.Style = Application.Current.Resources[GetStyle()] as Style;
			_control.AddHandler(System.Windows.Controls.ContextMenuService.ContextMenuOpeningEvent, new RoutedEventHandler(ContextMenuOpened));
			DependencyPropertyDescriptor.FromProperty(Scheduler.SelectedAppointmentProperty, typeof(Scheduler)).AddValueChanged(_control, new EventHandler(SelectedAppointmentChanged));
			_control.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(ControlDoubleClicked);
			_control.KeyDown += new System.Windows.Input.KeyEventHandler(ControlKeyDown);
			InternalUpdateStartDate();
						
			_appointmentSourceLink = new DataLink();
			_appointmentSourceLink.OnDataChanged += new DataLinkHandler(AppointmentSourceLinkChanged);
			_appointmentSourceLink.OnRowChanged += new DataLinkFieldHandler(AppointmentSourceLinkRowChanged);
			_appointmentSourceLink.OnActiveChanged += new DataLinkHandler(AppointmentSourceLinkChanged);
			_shiftSourceLink = new DataLink();
			_shiftSourceLink.OnDataChanged += new DataLinkHandler(ShiftSourceLinkChanged);
			_shiftSourceLink.OnRowChanged += new DataLinkFieldHandler(ShiftSourceLinkRowChanged);
			_shiftSourceLink.OnActiveChanged += new DataLinkHandler(ShiftSourceLinkChanged);
			_groupSourceLink = new DataLink();
			_groupSourceLink.OnDataChanged += new DataLinkHandler(GroupSourceLinkChanged);
			_groupSourceLink.OnRowChanged += new DataLinkFieldHandler(GroupSourceLinkRowChanged);
			_groupSourceLink.OnActiveChanged += new DataLinkHandler(GroupSourceLinkChanged);
			
			InternalUpdateGroupSource();
			InternalUpdateAppointmentSource();
			InternalUpdateShiftSource();
			
			UpdateGroupData();
			UpdateShiftData();
			UpdateAppointmentData();
			
			_elementHost = new ElementHost();
			_elementHost.Parent = ((IWindowsContainerElement)Parent).Control;
			_elementHost.Child = _control;
			
			base.Activate();
		}

		private void HandleUnhandledWPFException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
		{
			args.Handled = true;
			Session.HandleException(args.Exception);
		}

		protected virtual Scheduler CreateControl()
		{
			return new Scheduler();
		}

		protected virtual string GetStyle()
		{
			return "DefaultSchedulerStyle";
		}

		private void ContextMenuOpened(object sender, RoutedEventArgs args)
		{
			var item = args.OriginalSource as DependencyObject;
			if (item != null)
			{
				FrameworkElement element = Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>(item);
				if (element != null)
				{
					args.Handled = true;
					var menu = new ContextMenu();
					BuildMenu(menu, typeof(ScheduleTimeBlockVerb), element);
					if (menu.Items.Count > 0)
					{
						element.ContextMenu = menu;
						menu.IsOpen = true;
					}
				}
				else
				{
					element = Utilities.GetAncestorOfTypeInVisualTree<ScheduleAppointment>(item);
					if (element != null)
					{
						args.Handled = true;
						var menu = new ContextMenu();
						BuildMenu(menu, typeof(ScheduleAppointmentVerb), element);
						if (menu.Items.Count > 0)
						{
							element.ContextMenu = menu;
							menu.IsOpen = true;
						}
					}
				}
			}
		}

		protected virtual void BuildMenu(ContextMenu menu, Type type, object owner)
		{
			var first = true;
			foreach (INode child in Children)
			{
				if (type.IsAssignableFrom(child.GetType()))
				{
					var verb = child as BaseVerb;
					if (verb != null)
					{
						var menuItem = verb.BuildMenuItem(owner);
						if (first)
						{
							menuItem.FontWeight = FontWeights.Bold;
							first = false;
						}
						menu.Items.Add(menuItem);
					}
				}
			}
		}
		
		protected override void Deactivate()
		{
			if (_appointmentSourceLink != null)
			{
				_appointmentSourceLink.Dispose();
				_appointmentSourceLink = null;
			}
			if (_shiftSourceLink != null)
			{
				_shiftSourceLink.Dispose();
				_shiftSourceLink = null;
			}
			if (_groupSourceLink != null)
			{
				_groupSourceLink.Dispose();
				_groupSourceLink = null;
			}
			if (_elementHost != null)
			{
				DependencyPropertyDescriptor.FromProperty(Scheduler.SelectedAppointmentProperty, typeof(Scheduler)).RemoveValueChanged(_control, new EventHandler(SelectedAppointmentChanged));
				_elementHost.Dispose();
				_elementHost = null;
				_control = null;
			}
			base.Deactivate();
		}

		public override bool IsValidChild(Type childType)
		{
			return typeof(BaseVerb).IsAssignableFrom(childType)
				|| base.IsValidChild(childType);
		}

		private void ControlDoubleClicked(object sender, System.Windows.Input.MouseButtonEventArgs args)
		{
			// Double click executes the enabled appointment verb if on an appointment, or block verb if on a block
			var item = args.OriginalSource as DependencyObject;
			if (item != null)
			{
				FrameworkElement element = Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>(item);
				if (element != null)
				{
					args.Handled = true;
					var verb = FindFirstVerb(typeof(ScheduleTimeBlockVerb));
					if (verb != null && verb.GetEnabled())
						verb.Execute(args.OriginalSource);
				}
				else
				{
					element = Utilities.GetAncestorOfTypeInVisualTree<ScheduleAppointment>(item);
					if (element != null)
					{
						args.Handled = true;
						var verb = FindFirstVerb(typeof(ScheduleAppointmentVerb));
						if (verb != null && verb.GetEnabled())
							verb.Execute(args.OriginalSource);
					}
				}
			}
		}

		private BaseVerb FindFirstVerb(Type type)
		{
			foreach (INode child in Children)
			{
				if (type.IsAssignableFrom(child.GetType()))
				{
					var verb = child as BaseVerb;
					if (verb != null && verb.Enabled)
						return verb;
				}
			}
			return null;
		}

		private void ControlKeyDown(object sender, System.Windows.Input.KeyEventArgs args)
		{
			var item = args.OriginalSource as DependencyObject;
			if (item != null)
			{
				FrameworkElement element = Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>(item);
				if (element != null)
				{
					var verb = FindVerbForKey(typeof(ScheduleTimeBlockVerb), args.Key);
					if (verb != null && verb.GetEnabled())
					{
						args.Handled = true;
						verb.Execute(args.OriginalSource);
					}
				}
				else
				{
					element = Utilities.GetAncestorOfTypeInVisualTree<ScheduleAppointment>(item);
					if (element != null)
					{
						var verb = FindVerbForKey(typeof(ScheduleAppointmentVerb), args.Key);
						if (verb != null && verb.GetEnabled())
						{
							args.Handled = true;
							verb.Execute(args.OriginalSource);
						}
					}
				}
			}
		}

		private BaseVerb FindVerbForKey(Type type, System.Windows.Input.Key key)
		{
			foreach (INode child in Children)
			{
				if (type.IsAssignableFrom(child.GetType()))
				{
					var verb = child as BaseVerb;
					if (verb != null && verb.Enabled && verb.KeyboardShortcut != null && verb.KeyboardShortcut.Key == key && verb.KeyboardShortcut.Modifiers == Keyboard.Modifiers)
						return verb;
				}
			}
			return null;
		}
	}

	public abstract class BaseVerb : Node
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}
		
		// Action

		protected IAction _action;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed when this control is pressed.")]
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (_action != value)
				{
					if (_action != null)
						_action.Disposed -= new EventHandler(ActionDisposed);
					_action = value;
					if (_action != null)
						_action.Disposed += new EventHandler(ActionDisposed);
				}
			}
		}

		protected void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}

		// Text

		private string _text = String.Empty;
		[Publish(PublishMethod.Value)]
		[DefaultValue("")]
		[Description("A text string that will be used on the context menu.  If this is not set the text property of the action will be used.")]
		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		public string GetText()
		{
			if ((Action != null) && (_text == String.Empty))
				return Action.Text;
			else
				return _text;
		}

		// Enabled

		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("When this is set to false this context menu item will be disabled.  This control will also be disabled if the action is disabled.")]
		public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}

		/// <summary> Gets whether the node is actuall enabled (accounting for action). </summary>
		/// <remarks>
		///		The enabled state of the node is the most restrictive between 
		///		the action and the Enabled property.
		///	</remarks>
		public bool GetEnabled()
		{
			return (Action == null ? false : Action.GetEnabled()) && _enabled;
		}
		
		// KeyboardShortcut
		
		private KeyGesture _keyboardShortcut = null;
		public KeyGesture KeyboardShortcut
		{
			get { return _keyboardShortcut; }
			set { _keyboardShortcut = value; }
		}
		
		protected internal virtual MenuItem BuildMenuItem(object owner)
		{
			var item = 
				new MenuItem
				{
					Header = GetText().Replace('&', '_'),
					IsEnabled = GetEnabled(),
					Icon = (Action == null ? null : new System.Windows.Controls.Image() { Source = WPFInteropUtility.ImageToBitmapSource(((Action)Action).LoadedImage) }),
					Tag = owner,
					InputGestureText = _keyboardShortcut == null  ? "" : new KeyGestureConverter().ConvertToString(_keyboardShortcut)
				};
			item.Click += new RoutedEventHandler(ItemClicked);
			return item;
		}
		
		protected virtual void ItemClicked(object sender, RoutedEventArgs args)
		{
			if (Action != null)
			{
				args.Handled = true;
				Execute((FrameworkElement)((MenuItem)sender).Tag);
			}
		}

		public abstract void Execute(object sender);
	}

	[DesignerImage("Image('Frontend', 'Nodes.Schedule')")]
	[DesignerCategory("Data Controls")]
	public class ScheduleAppointmentVerb : BaseVerb
	{
		public override void Execute(object sender)
		{
			var appointment = WPF.Utilities.GetAncestorOfTypeInVisualTree<ListBoxItem>((DependencyObject)sender);
			var day = WPF.Utilities.GetAncestorOfTypeInVisualTree<ScheduleDay>((DependencyObject)sender);
			var paramsValue = new EventParams();
			if (appointment != null)
			{
				paramsValue.Add("AStartTime", ScheduleDayAppointments.GetStart(appointment));
				paramsValue.Add("AEndTime", ScheduleDayAppointments.GetEnd(appointment));
			}
			if (day != null)
			{
				paramsValue.Add("ADate", day.Date);
				paramsValue.Add("AGroup", day.GroupID);
			}
			Action.Execute(this, paramsValue);
		}
	}

	[DesignerImage("Image('Frontend', 'Nodes.Schedule')")]
	[DesignerCategory("Data Controls")]
	public class ScheduleTimeBlockVerb : BaseVerb
	{
		public override void Execute(object sender)
		{
			var timeBlock = WPF.Utilities.GetAncestorOfTypeInVisualTree<ScheduleTimeBlock>((DependencyObject)sender);
			var day = WPF.Utilities.GetAncestorOfTypeInVisualTree<ScheduleDay>((DependencyObject)sender);
			var paramsValue = new EventParams();
			if (timeBlock != null)
				paramsValue.Add("ATime", timeBlock.Time);
			if (day != null)
			{
				paramsValue.Add("ADate", day.Date);
				paramsValue.Add("AGroup", day.GroupID);
			}
			Action.Execute(this, paramsValue);
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
		private object _group;
		/// <summary> Gets and sets the object data used to group items under this grouping. </summary>
		public object Group
		{
			get { return _group; }
			set
			{
				if (_group != value)
				{
					_group = value;
					NotifyPropertyChanged("Group");
				}
			}
		}

		private string _description;
		public string Description
		{
			get { return _description; }
			set
			{
				if (_description != value)
				{
					_description = value;
					NotifyPropertyChanged("Description");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ScheduleData : INotifyPropertyChanged
	{
		private DateTime _date;
		public DateTime Date
		{
			get { return _date; }
			set
			{
				if (_date != value)
				{
					_date = value;
					NotifyPropertyChanged("Date");
				}
			}
		}

		private DateTime _startTime;
		public DateTime StartTime
		{
			get { return _startTime; }
			set
			{
				if (_startTime != value)
				{
					_startTime = value;
					NotifyPropertyChanged("StartTime");
				}
			}
		}

		private DateTime _endTime;
		public DateTime EndTime
		{
			get { return _endTime; }
			set
			{
				if (_endTime != value)
				{
					_endTime = value;
					NotifyPropertyChanged("EndTime");
				}
			}
		}

		private object _group;
		/// <summary> Gets and sets the object data used to group this item. </summary>
		public object Group
		{
			get { return _group; }
			set
			{
				if (_group != value)
				{
					_group = value;
					NotifyPropertyChanged("Group");
				}
			}
		}

		private string _description;
		/// <summary> Gets and sets the textual description of the item. </summary>
		public string Description
		{
			get { return _description; }
			set
			{
				if (_description != value)
				{
					_description = value;
					NotifyPropertyChanged("Description");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
	
	public class ScheduleAppointmentData : ScheduleData
	{
		private ImageSource _image;
		public ImageSource Image
		{
			get { return _image; }
			set
			{
				if (_image != value)
				{
					_image = value;
					NotifyPropertyChanged("Image");
				}
			}
		}

		private string _backgroundColor;
		public string BackgroundColor
		{
			get { return _backgroundColor; }
			set
			{
				if (_backgroundColor != value)
				{
					_backgroundColor = value;
					NotifyPropertyChanged("BackgroundColor");
				}
			}
		}
	}
	
	public class ScheduleShiftData : ScheduleData
	{
		private int? _highlightInterval;
		public int? HighlightInterval
		{
			get { return _highlightInterval; }
			set
			{
				if (_highlightInterval != value)
				{
					_highlightInterval = value;
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
			//TODO: investigate programmatically creating the Binding so the BackgroundColor can be passed as the parameter and the Border can be set to an opacity of the BackgroundColor.
			SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
			if (!(bool)value)
				brush.Opacity = 0d;
			return brush;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}	   
	} 
	
	public class StringToBorderBackgroundBrushConverter : IValueConverter
	{
		public const string DefaultBrush = "#9BA5DA";
		static StringToBorderBackgroundBrushConverter()
		{
		   Brushes.Add(DefaultBrush, GetBrush(DefaultBrush));
		}

		public static LinearGradientBrush GetBrush(string color)
		{
			LinearGradientBrush brush = new LinearGradientBrush(); 
			try
			{
				byte red = System.Convert.ToByte(color.Substring(1, 2), 16);
				byte green = System.Convert.ToByte(color.Substring(3, 2), 16);
				byte blue = System.Convert.ToByte(color.Substring(5, 2), 16);					
				
				brush.EndPoint = new Point(0.5, 1);
				brush.StartPoint = new Point(0.5, 0);
				brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, red, green, blue), 1)); 			
				//TODO: investigate putting three gradients back in.  The default color example is below:
				//brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBC, 0xC1, 0xE8), 0));
				//brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB0, 0xB8, 0xE4), 0.935));
				//brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xAA, 0xB4, 0xE6), 0.069));

			}
			catch
			{
				return GetBrush(DefaultBrush);
			}
			return brush;
		}	 
		
		private static Dictionary<string, LinearGradientBrush> Brushes = new Dictionary<string, LinearGradientBrush>();
	
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{  			
			string color = (String)value;
			if (!Brushes.ContainsKey(color))
			{
				Brushes.Add(color, GetBrush(color));
			}	  		           

			return Brushes[color]; 
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
