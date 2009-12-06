using System;
using Alphora.Dataphor.DAE.Client;
using System.ComponentModel;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public class Schedule : Element
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			AppointmentSource = null;
			ShiftSource = null;
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
					if (Active)
						InternalUpdateAppointmentDateColumn();
				}
			}
		}

		protected virtual void InternalUpdateAppointmentDateColumn()
		{
			
		}

		// AppointmentTimeColumn

		private string FAppointmentTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the appointment source that represents the Time.")]
		public string AppointmentTimeColumn
		{
			get { return FAppointmentTimeColumn; }
			set
			{
				if (FAppointmentTimeColumn != value)
				{
					FAppointmentTimeColumn = value;
					if (Active)
						InternalUpTimeAppointmentTimeColumn();
				}
			}
		}

		protected virtual void InternalUpTimeAppointmentTimeColumn()
		{

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
					if (Active)
						InternalUpGroupAppointmentGroupColumn();
				}
			}
		}

		protected virtual void InternalUpGroupAppointmentGroupColumn()
		{

		}

		// AppointmentDisplayColumn

		private string FAppointmentDisplayColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the appointment source that represents the Display.")]
		public string AppointmentDisplayColumn
		{
			get { return FAppointmentDisplayColumn; }
			set
			{
				if (FAppointmentDisplayColumn != value)
				{
					FAppointmentDisplayColumn = value;
					if (Active)
						InternalUpDisplayAppointmentDisplayColumn();
				}
			}
		}

		protected virtual void InternalUpDisplayAppointmentDisplayColumn()
		{

		}

		// ShiftSource

		private IShiftSource FShiftSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the ShiftSource node the control will be attached to.")]
		public IShiftSource ShiftSource
		{
			get { return FShiftSource; }
			set
			{
				if (FShiftSource != value)
					SetShiftSource(value);
			}
		}

		protected virtual void SetShiftSource(IShiftSource AShiftSource)
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
			FShiftSourceLink.Source = ShiftSource.DataShiftSource;
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
					if (Active)
						InternalUpdateShiftDateColumn();
				}
			}
		}

		protected virtual void InternalUpdateShiftDateColumn()
		{

		}

		// ShiftTimeColumn

		private string FShiftTimeColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the Shift source that represents the Time.")]
		public string ShiftTimeColumn
		{
			get { return FShiftTimeColumn; }
			set
			{
				if (FShiftTimeColumn != value)
				{
					FShiftTimeColumn = value;
					if (Active)
						InternalUpTimeShiftTimeColumn();
				}
			}
		}

		protected virtual void InternalUpTimeShiftTimeColumn()
		{

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
					if (Active)
						InternalUpGroupShiftGroupColumn();
				}
			}
		}

		protected virtual void InternalUpGroupShiftGroupColumn()
		{

		}

		// ShiftDisplayColumn

		private string FShiftDisplayColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the Shift source that represents the Display.")]
		public string ShiftDisplayColumn
		{
			get { return FShiftDisplayColumn; }
			set
			{
				if (FShiftDisplayColumn != value)
				{
					FShiftDisplayColumn = value;
					if (Active)
						InternalUpDisplayShiftDisplayColumn();
				}
			}
		}

		protected virtual void InternalUpDisplayShiftDisplayColumn()
		{

		}

		// GroupSource

		private IGroupSource FGroupSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the GroupSource node the control will be attached to.")]
		public IGroupSource GroupSource
		{
			get { return FGroupSource; }
			set
			{
				if (FGroupSource != value)
					SetGroupSource(value);
			}
		}

		protected virtual void SetGroupSource(IGroupSource AGroupSource)
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
			FGroupSourceLink.Source = GroupSource.DataGroupSource;
		}

		// GroupColumn

		private string FGroupColumn = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The column in the Shift source that represents the Display.")]
		public string GroupColumn
		{
			get { return FGroupColumn; }
			set
			{
				if (FGroupColumn != value)
				{
					FGroupColumn = value;
					if (Active)
						InternalUpDisplayGroupColumn();
				}
			}
		}

		protected virtual void InternalUpDisplayGroupColumn()
		{

		}

		// Element
		
		protected override void InternalLayout(System.Drawing.Rectangle ABounds)
		{
			throw new NotImplementedException();
		}

		// Node
		
		protected override void Activate()
		{
			FAppointmentSourceLink = new DataLink();
			FShiftSourceLink = new DataLink();
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
			base.Deactivate();
		}
	}
}
