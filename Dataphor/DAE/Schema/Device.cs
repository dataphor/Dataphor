/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device;
using Alphora.Dataphor.DAE.Device.Catalog;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	[Flags]    
    public enum DeviceCapability : byte 
    { 
		RowLevelInsert = 1, 
		RowLevelUpdate = 2, 
		RowLevelDelete = 4, 
		NonLoggedOperations = 8 
	}
    
	[Flags]
	public enum ReconcileOptions 
	{ 
		None = 0,
		ShouldReconcileColumns = 1, 
		ShouldDropTables = 2,
		ShouldDropColumns = 4, 
		ShouldDropKeys = 8, 
		ShouldDropOrders = 16, 
		All = ShouldReconcileColumns | ShouldDropTables | ShouldDropColumns | ShouldDropKeys | ShouldDropOrders
	}
	
    public class DevicePlan : Disposable
    {
		public DevicePlan(Plan plan, Device device, PlanNode planNode)
		{   
			_plan = plan;
			_device = device;
			_planNode = planNode;
		}

		protected override void Dispose(bool disposing)
		{
			_plan = null;
			_device = null;
			_planNode = null;
			base.Dispose(disposing);
		}

		// Plan
		[Reference]
		private Plan _plan;
		public Plan Plan { get { return _plan; } }

		// Device
		[Reference]
		private Device _device;
		public Device Device { get { return _device; } }

		// PlanNode
		[Reference]
        private PlanNode _planNode;
		public PlanNode Node { get { return _planNode; } }
		
		// IsSupported
		private bool _isSupported = true;
		public bool IsSupported 
		{ 
			get { return _isSupported; } 
			set { _isSupported = value; }
		}
		
		// TranslationMessages
		private TranslationMessages _translationMessages = new TranslationMessages();
		public TranslationMessages TranslationMessages { get { return _translationMessages; } }
	}
    
    public abstract class DeviceObject : Schema.CatalogObject
    {
		public DeviceObject(int iD, string name) : base(iD, name)
		{
			IsRemotable = false;
		}

		public DeviceObject(int iD, string name, bool isSystem) : base(iD, name)
		{
			IsRemotable = false;
			IsSystem = isSystem;
		}
		
		public override string DisplayName { get { return String.Format("Device_{0}_Map_{1}", Device.Name, Name); } }

		[Reference]
		private Device _device;
		public Device Device 
		{ 
			get { return _device; } 
			set { _device = value; }
		}
    }
    
    public abstract class DeviceOperator : DeviceObject
    {		
		public DeviceOperator(int iD, string name) : base(iD, name){}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.DeviceOperator"), _operator.OperatorName, _operator.Signature.ToString(), Device.DisplayName); } }

		// Operator		
		[Reference]
		private Schema.Operator _operator;
		public Schema.Operator Operator
		{
			get { return _operator; }
			set { _operator = value; }
		}
		
        // ClassDefinition
        private ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return _classDefinition; }
			set { _classDefinition = value; }
        }
        
		public abstract Statement Translate(DevicePlan devicePlan, PlanNode planNode);
		
		private void EmitOperatorSpecifier(DeviceOperatorMapBase operatorMap, EmitMode mode)
		{
			operatorMap.OperatorSpecifier = new OperatorSpecifier();
			operatorMap.OperatorSpecifier.OperatorName = Schema.Object.EnsureRooted(Operator.OperatorName);
			foreach (Operand operand in Operator.Operands)
			{
				FormalParameterSpecifier specifier = new FormalParameterSpecifier();
				specifier.Modifier = operand.Modifier;
				specifier.TypeSpecifier = operand.DataType.EmitSpecifier(mode);
				operatorMap.OperatorSpecifier.FormalParameterSpecifiers.Add(specifier);
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			AlterDeviceStatement statement = new AlterDeviceStatement();
			statement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			statement.CreateDeviceOperatorMaps.Add(EmitCreateDefinition(mode));
			return statement;
		}
		
		public DeviceOperatorMap EmitCreateDefinition(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				DeviceOperatorMap operatorMap = new DeviceOperatorMap();
				EmitOperatorSpecifier(operatorMap, mode);
				operatorMap.ClassDefinition = ClassDefinition == null ? null : (ClassDefinition)ClassDefinition.Clone();
				operatorMap.MetaData = MetaData == null ? null : MetaData.Copy();
				return operatorMap;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public DropDeviceOperatorMap EmitDropDefinition(EmitMode mode)
		{
			DropDeviceOperatorMap operatorMap = new DropDeviceOperatorMap();
			EmitOperatorSpecifier(operatorMap, mode);
			return operatorMap;
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			AlterDeviceStatement statement = new AlterDeviceStatement();
			statement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			statement.DropDeviceOperatorMaps.Add(EmitDropDefinition(mode));
			return statement;
		}
    }
    
	public class DeviceOperators : Dictionary<Operator, DeviceOperator>
	{		
		public DeviceOperators() : base(){}
		
		public void Add(DeviceOperator operatorValue)
		{
			Add(operatorValue.Operator, operatorValue);
		}
	}

    public abstract class DeviceScalarType : DeviceObject
    {
		public DeviceScalarType(int iD, string name) : base(iD, name) {}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.DeviceScalarType"), _scalarType.DisplayName, Device.DisplayName); } }

		// ScalarType
		[Reference]
		private ScalarType _scalarType;
		public ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}
		
		// IsDefaultClassDefinition
		private bool _isDefaultClassDefinition;
		public bool IsDefaultClassDefinition
		{
			get { return _isDefaultClassDefinition; }
			set { _isDefaultClassDefinition = value; }
		}
		
        // ClassDefinition
        private ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return _classDefinition; }
			set { _classDefinition = value; }
        }
        
		/// <summary>Override this method to provide transformation services to the device for a particular data type.</summary>
		public abstract object ToScalar(IValueManager manager, object tempValue);
		
		/// <summary>Override this method to provide transformation services to the device for a particular data type.</summary>
		public abstract object FromScalar(IValueManager manager, object tempValue);
		
		/// <summary>Override this method to provide transformation services to the device for stream access data types.</summary>
		public virtual Stream GetStreamAdapter(IValueManager manager, Stream stream)
		{
			return stream;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			AlterDeviceStatement statement = new AlterDeviceStatement();
			statement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			statement.CreateDeviceScalarTypeMaps.Add(EmitCreateDefinition(mode));
			return statement;
		}
		
		public DeviceScalarTypeMap EmitCreateDefinition(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				DeviceScalarTypeMap scalarTypeMap = new DeviceScalarTypeMap();
				scalarTypeMap.ScalarTypeName = Schema.Object.EnsureRooted(ScalarType.Name);
				scalarTypeMap.ClassDefinition = (IsDefaultClassDefinition || (ClassDefinition == null)) ? null : (ClassDefinition)ClassDefinition.Clone();
				scalarTypeMap.MetaData = MetaData == null ? null : MetaData.Copy();
				return scalarTypeMap;
			}
			finally
			{
				 if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public DropDeviceScalarTypeMap EmitDropDefinition(EmitMode mode)
		{
			DropDeviceScalarTypeMap map = new DropDeviceScalarTypeMap();
			map.ScalarTypeName = Schema.Object.EnsureRooted(ScalarType.Name);
			return map;
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			AlterDeviceStatement statement = new AlterDeviceStatement();
			statement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			statement.DropDeviceScalarTypeMaps.Add(EmitDropDefinition(mode));
			return statement;
		}
    }
    
	public class DeviceScalarTypes : Dictionary<ScalarType, DeviceScalarType>
	{
		public DeviceScalarTypes() : base(){}
		
		public void Add(DeviceScalarType scalarType)
		{
			Add(scalarType.ScalarType, scalarType);
		}
	}
	
	#if USESTORES
	public class Index : Object
    {		
		public Index() : base(String.Empty)
		{
			CreateColumns();
			UpdateIndexName();
		}

		public Index(MetaData AMetaData) : base(String.Empty)
		{
			MetaData = AMetaData;
			CreateColumns();
			UpdateIndexName();
		}
		
		public Index(Index AIndex) : base(String.Empty)
		{
			CreateColumns();
			UpdateIndexName();
			OrderColumn LNewOrderColumn;
			foreach (OrderColumn LColumn in AIndex.Columns)
			{
				LNewOrderColumn = new OrderColumn(LColumn.Column, LColumn.Ascending);
				LNewOrderColumn.Sort = LColumn.Sort;
				LNewOrderColumn.IsDefaultSort = LColumn.IsDefaultSort;
				FColumns.Add(LNewOrderColumn);
			}
		}
		
		public Index(Index AIndex, bool AReverse) : base(String.Empty)
		{
			CreateColumns();
			UpdateIndexName();
			OrderColumn LNewOrderColumn;
			foreach (OrderColumn LColumn in AIndex.Columns)
			{
				LNewOrderColumn = new OrderColumn(LColumn.Column, AReverse ? !LColumn.Ascending : LColumn.Ascending);
				LNewOrderColumn.Sort = LColumn.Sort;
				LNewOrderColumn.IsDefaultSort = LColumn.IsDefaultSort;
				FColumns.Add(LNewOrderColumn);
			}
		}
		
		public Index(Key AKey) : base(String.Empty)
		{
			CreateColumns();
			UpdateIndexName();
			OrderColumn LOrderColumn;
			foreach (TableVarColumn LColumn in AKey.Columns)
			{
				LOrderColumn = new OrderColumn(LColumn, true);
				FColumns.Add(LOrderColumn);
			}
		}
		
		public Index(Key AKey, Plan APlan) : base(String.Empty)
		{
			CreateColumns();
			UpdateIndexName();
			OrderColumn LOrderColumn;
			foreach (TableVarColumn LColumn in AKey.Columns)
			{
				LOrderColumn = new OrderColumn(LColumn, true);
				if (LColumn.DataType is Schema.ScalarType)
					LOrderColumn.Sort = ((Schema.ScalarType)LColumn.DataType).GetUniqueSort(APlan);
				else
				{
					LOrderColumn.Sort = Compiler.CompileSortDefinition(APlan, LColumn.DataType);
					//LOrderColumn.Sort.AttachDependencies(APlan.Catalog);
				}
				LOrderColumn.IsDefaultSort = true;
				FColumns.Add(LOrderColumn);
			}
		}

		public override string Name
		{
			get { return FName; }
			set { }
		}

		private void UpdateIndexName()
		{
			FName = Keywords.Index + FColumns.ToString();
		}

		private void CreateColumns()
		{
			FColumns.OnAdding += new ListEventHandler(ColumnAdding);
			FColumns.OnRemoving += new ListEventHandler(ColumnRemoving);
		}

		private void ColumnAdding(object ASender, object AItem)
		{
			UpdateIndexName();
		}

		private void ColumnRemoving(object ASender, object AItem)
		{
			UpdateIndexName();
		}

		// Columns
		private OrderColumns FColumns = new OrderColumns();
		public OrderColumns Columns { get { return FColumns; } }

		// IsInherited
		private bool FIsInherited;
		public bool IsInherited
		{
			get { return FIsInherited; }
			set { FIsInherited = value; }
		}
		
		public bool Equivalent(Index AIndex)
		{
			// returns true if AIndex can be used to satisfy an ordering by this order 
			if (Columns.Count > AIndex.Columns.Count)
				return false;

			for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
				if 
				(
					(Columns[LIndex].Ascending != AIndex.Columns[LIndex].Ascending) || 
					!Schema.Object.NamesEqual(Columns[LIndex].Column.Name, AIndex.Columns[LIndex].Column.Name) ||
					!Columns[LIndex].Sort.Equivalent(AIndex.Columns[LIndex].Sort)
				)
					return false;
			
			return true;		
		}

		// returns true if the order includes the key as a subset, including the use of the unique sort algorithm for the type of each column
		public bool Includes(Plan APlan, Key AKey)
		{
			foreach (TableVarColumn LColumn in AKey.Columns)
				if (!Columns.Contains(LColumn.Name) || !Columns[LColumn.Name].Sort.Equivalent(Compiler.GetUniqueSort(APlan, LColumn.DataType)))
					return false;
			return true;
		}

        public override bool Equals(object AObject)
        {
            // An order is equal to another order if it contains the same columns (by order and ascending)
            if (AObject is Index)
            {
				Index LObject = (Index)AObject;
                if (Columns.Count == LObject.Columns.Count)
                {
					for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
						if 
						(
							(Columns[LIndex].Ascending != LObject.Columns[LIndex].Ascending) ||
							!Schema.Object.NamesEqual(Columns[LIndex].Column.Name, LObject.Columns[LIndex].Column.Name) ||
							!Columns[LIndex].Sort.Equivalent(LObject.Columns[LIndex].Sort)
						)
							return false;
                    return true;
                }
                else
                    return false;
            }
            System.Diagnostics.Debug.Assert(!(AObject is Key), "Index.Equals called with a Key as argument");
            return base.Equals(AObject);
        }

        // GetHashCode
        public override int GetHashCode()
        {
			int LHashCode = 0;
			for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
				LHashCode ^= FColumns[LIndex].Column.Name.GetHashCode();
			return LHashCode;
        }

        public override string ToString()
        {
			return FName;
        }
        
        public override Statement EmitStatement(EmitMode AMode)
        {
			IndexDefinition LIndex = new IndexDefinition();
			foreach (OrderColumn LColumn in Columns)
				LIndex.Columns.Add(LColumn.EmitStatement(AMode));
			LIndex.MetaData = MetaData == null ? null : MetaData.Copy();
			return LIndex;
        }
        
        public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			
			foreach (OrderColumn LColumn in Columns)
				if (LColumn.Sort != null)
					LColumn.Sort.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
        }
        
/*
        public override void AttachDependencies(Catalog ACatalog)
        {
			base.AttachDependencies(ACatalog);
			foreach (OrderColumn LColumn in Columns)
				if (LColumn.Sort != null)
					LColumn.Sort.AttachDependencies(ACatalog);
		}
        
        public override void DetachDependencies(Catalog ACatalog)
        {
			base.DetachDependencies(ACatalog);
			foreach (OrderColumn LColumn in Columns)
				if (LColumn.Sort != null)
					LColumn.Sort.DetachDependencies(ACatalog);
        }
*/
    }

/*
	public class Indexes : Objects
    {
		#if USEOBJECTVALIDATE
        protected override void Validate(Object AObject)
        {
            if (!(AObject is Index))
                throw new SchemaException(SchemaException.Codes.InvalidContainer, "Index");
            base.Validate(AObject);
        }
        #endif

        public new Index this[int AIndex]
        {
            get { return (Index)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
        
        public new Index this[string AName]
        {
			get { return (Index)base[AName]; }
			set { base[AName] = value; }
        }
        
        // ToString
        public override string ToString()
        {
			StringBuilder LString = new StringBuilder();
			foreach (Index LIndex in this)
			{
				if (LString.Length != 0)
				{
					LString.Append(Keywords.ListSeparator);
					LString.Append(" ");
				}
				LString.Append(LIndex.ToString());
			}
			return LString.ToString();
        }
    }
    
    public class Store : DeviceObject
    {
		public Store() : base(){}
		
		private TableVar FTableVar;
		public TableVar TableVar
		{
			get { return FTableVar; }
			set { FTableVar = value; }
		}
		
		private Index FClusteredIndex;
		public Index ClusteredIndex
		{
			get { return FClusteredIndex; }
			set { FClusteredIndex = value; }
		}
		
		private Indexes FIndexes = new Indexes();
		public Indexes Indexes { get { return FIndexes; } }
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			DeviceStoreDefinition LDefinition = new DeviceStoreDefinition();
			LDefinition.StoreName = Name;
			LDefinition.Expression = (Expression)TableVar.EmitStatement(AMode);
			LDefinition.ClusteredIndexDefinition = (IndexDefinition)FClusteredIndex.EmitStatement(AMode);
			foreach (Index LIndex in FIndexes)
				LDefinition.IndexDefinitions.Add(LIndex.EmitStatement(AMode));
			LDefinition.MetaData = MetaData == null ? null : MetaData.Copy();
			return LDefinition;
		}
    }
    #endif

	/*
		Reconciliation ->
			The process of synchronization of structure between the Dataphor catalog and the device catalogs.
			This process can be automatic, or user initiated. Each device has two settings which determine how
			and when reconciliation occurs: Mode, and Master.
			
			Mode can be either None, or any combination of Startup, Command, and Automatic.  Startup indicates that
			the device should be reconciled on server startup, Command indicates that DDL commands executed against
			the D4 server should be passed through to the device as well, and Automatic indicates that catalog should
			be verified as it is encountered in DML statements executed against the server.
			
			Users can also initiate the reconciliation process through use of the Reconcile operator, passing the name
			of the device to be reconciled.  The current device settings will be used to control the reconciliation
			process.
			
			Catalog reconciliation for a device proceeds as follows.  Beyond this basic process, each device provides
			specific reconciliation. ->
				
				If the Master setting for a device is Server or Both, each table in the Server catalog is reconciled against the
				device, if it does not exist in the device, it is created, otherwise, it is reconciled with the Server as master.
				
				If the Master setting for a device is Device or Both, each table in the Device catalog is reconciled against the
				server, if it does not exist in the server, it is created, otherwise, it is reconciled with the Device as master.
				
			Catalog reconciliation for a table proceeds as follows ->
			
				If the Server table is master, the columns in the device table must be a superset of the columns in the server table.
				
				If the Device table is master, the columns in the server table must be a superset of the columns in the device table.
				
	*/

	public abstract class Device : CatalogObject
    {						        
		// constructor
		public Device(int iD, string name) : base(iD, name)
		{
			IsRemotable = false;
			_reconcileMode = ReconcileMode.Command;
			_requiresAuthentication = true;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Device"), DisplayName); } }

        // ClassDefinition
        private ClassDefinition _classDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return _classDefinition; }
			set { _classDefinition = value; }
        }
        
        // IgnoreUnsupported
        private bool _ignoreUnsupported;
        public bool IgnoreUnsupported
        {
			get { return _ignoreUnsupported; }
			set { _ignoreUnsupported = value; }
		}
        
		// Sessions
        private DeviceSessions _sessions;
        public DeviceSessions Sessions { get { return _sessions; } }
        
        // Start
        protected virtual void ApplyDeviceSettings(ServerProcess process)
        {
			foreach (DeviceSetting setting in process.ServerSession.Server.DeviceSettings.GetSettingsForDevice(Name))
			{
				try
				{
					ClassLoader.SetProperty(this, setting.SettingName, setting.SettingValue);
				}
				catch (Exception exception)
				{
					process.ServerSession.Server.LogError(new SchemaException(SchemaException.Codes.ErrorApplyingDeviceSetting, exception, Name, setting.SettingName, setting.SettingValue));
				}
			}
        }
        
        protected virtual void InternalStart(ServerProcess process)
        {
			_sessions = new DeviceSessions();
        }								  
        
        protected virtual void InternalStarted(ServerProcess process) { }
        
        public void Start(ServerProcess process)
        {
			if (!_running)
			{
				try
				{
					ApplyDeviceSettings(process);
					InternalStart(process);
					_running = true;
					InternalStarted(process);
				}
				catch
				{
					Stop(process);
					throw;
				}
			}
        }
        
        // Stop
        protected virtual void InternalStop(ServerProcess process)
        {
			if (_sessions != null)
			{
				_sessions.Dispose();
				_sessions = null;
			}
        }

        public void Stop(ServerProcess process)
        {
			if (_running)
			{
				InternalStop(process);
				_running = false;
			}
        }

        // Running
        private bool _running;
        public bool Running { get { return _running; } }
        
        protected void CheckRunning()
        {
			if (!_running)
				throw new SchemaException(SchemaException.Codes.DeviceNotRunning, Name);
        }
        
        protected void CheckNotRunning()
        {
			if (_running)
				throw new SchemaException(SchemaException.Codes.DeviceRunning, Name);
        }
        
        // Registered
        private bool _registered;
        public bool Registered { get { return _registered; } }
        
		public void LoadRegistered()
		{
			Tag tag = RemoveMetaDataTag("DAE.Registered");
			if (tag != Tag.None)
				_registered = Boolean.Parse(tag.Value);
		}

		public void SaveRegistered()
		{
			if (_registered)
				AddMetaDataTag("DAE.Registered", _registered.ToString(), true);
		}
		
		public void RemoveRegistered()
		{
			RemoveMetaDataTag("DAE.Registered");
		}
		
		/// <summary>Used for backwards compatibility only</summary>
		/// <remarks>
		/// This method is used to force the registered flag to be set for devices that have already been registered but were saved to the
		/// catalog before the existence of the registered flag. This method will be deprecated in a future release.
		/// </remarks>
		public void SetRegistered()
		{
			_registered = true;
		}
		
		/// <summary>Used by the catalog device to undo the effects of the register call</summary>
		/// <remarks>
		/// This method is only used by the transactional DDL mechansim of the catalog device to ensure that the effects of the register
		/// are rolled back.
		/// </remarks>
		public void ClearRegistered()
		{
			_registered = false;
		}

        public void Register(ServerProcess process)
        {
			if (!_registered)
			{
				InternalRegister(process);
				_registered = true;
			}
        }
        
        protected virtual void InternalRegister(ServerProcess process) { }
        
        // Connect
        protected abstract DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo);
		public DeviceSession Connect(ServerProcess serverProcess, SessionInfo sessionInfo)
        {
			DeviceSession session = InternalConnect(serverProcess, GetDeviceSessionInfo(serverProcess, serverProcess.ServerSession.User, sessionInfo));
			try
			{
				lock (_sessions.SyncRoot)
				{
					_sessions.Add(session);
				}
				return session;
			}
			catch
			{
				Disconnect(session);
				throw;
			}
        }
        
        // Disconnect
        public void Disconnect(DeviceSession session)
        {
			lock (_sessions.SyncRoot)
			{
				session.Dispose();
			}
        }

        // Prepare
        protected abstract DevicePlanNode InternalPrepare(DevicePlan plan, PlanNode planNode);
        public DevicePlan Prepare(Plan plan, PlanNode planNode)
        {
			CheckRunning();
			Schema.DevicePlan devicePlan = CreateDevicePlan(plan, planNode);
			if (planNode.DeviceNode == null)
			{
				planNode.DeviceNode = InternalPrepare(devicePlan, planNode);
				if (planNode.DeviceNode == null)
					devicePlan.IsSupported = false;
			}
					
			return devicePlan;
        }
        
        protected virtual DevicePlan CreateDevicePlan(Plan plan, PlanNode planNode)
        {
			return new DevicePlan(plan, this, planNode);
        }
        
        protected virtual void InternalUnprepare(DevicePlan plan) {}
        public void Unprepare(DevicePlan plan)
        {
			InternalUnprepare(plan);
			plan.Dispose();
        }
        
        // Translate
        public virtual Statement Translate(DevicePlan devicePlan, PlanNode planNode) { return null; }

        // Reconcile
		public virtual ErrorList Reconcile(ServerProcess process, Catalog serverCatalog, Catalog deviceCatalog)
		{
			ErrorList errors = new ErrorList();

			if ((ReconcileMaster == D4.ReconcileMaster.Server) || (ReconcileMaster == D4.ReconcileMaster.Both))
				foreach (Schema.Object objectValue in serverCatalog)
					if (objectValue is Schema.BaseTableVar)
					{
						try
						{
							Schema.BaseTableVar tableVar = (Schema.BaseTableVar)objectValue;
							if (Convert.ToBoolean(D4.MetaData.GetTag(tableVar.MetaData, "Storage.ShouldReconcile", "true")))
							{
								int objectIndex = deviceCatalog.IndexOf(tableVar.Name);
								if (objectIndex < 0)
									CreateTable(process, tableVar, D4.ReconcileMaster.Server);
								else
									ReconcileTable(process, tableVar, (Schema.BaseTableVar)deviceCatalog[objectIndex], D4.ReconcileMaster.Server);
							}
						}
						catch (Exception exception)
						{
							errors.Add(exception);
						}
					}
			
			if ((ReconcileMaster == D4.ReconcileMaster.Device) || (ReconcileMaster == D4.ReconcileMaster.Both))
				foreach (Schema.Object objectValue in deviceCatalog)
					if (objectValue is Schema.BaseTableVar)
					{
						try
						{
							Schema.BaseTableVar tableVar = (Schema.BaseTableVar)objectValue;
							if (Convert.ToBoolean(D4.MetaData.GetTag(tableVar.MetaData, "Storage.ShouldReconcile", "true")))
							{
								int objectIndex = serverCatalog.IndexOf(tableVar.Name);
								if ((objectIndex < 0) || (serverCatalog[objectIndex].Library == null)) // Library will be null if this table was created only to specify a name for the table to reconcile
									CreateTable(process, tableVar, D4.ReconcileMaster.Device);
								else
									ReconcileTable(process, (Schema.BaseTableVar)serverCatalog[objectIndex], tableVar, D4.ReconcileMaster.Device);
							}
						}
						catch (Exception exception)
						{
							errors.Add(exception);
						}
					}
					else if (objectValue is Schema.Reference)
					{
						try
						{
							Schema.Reference reference = (Schema.Reference)objectValue;
							if (Convert.ToBoolean(D4.MetaData.GetTag(reference.MetaData, "Storage.ShouldReconcile", "true")))
							{
								int objectIndex = serverCatalog.IndexOf(reference.Name);
								if (objectIndex < 0)
									CreateReference(process, reference, D4.ReconcileMaster.Device);
								else
									ReconcileReference(process, (Schema.Reference)serverCatalog[objectIndex], reference, D4.ReconcileMaster.Device);
							}
						}
						catch (Exception exception)
						{
							errors.Add(exception);
						}
					}
					
			return errors;
		}
        
        public Catalog GetServerCatalog(ServerProcess process, TableVar tableVar)
        {
			Catalog serverCatalog = new Catalog();
			if (tableVar != null)
				serverCatalog.Add(tableVar);
			else
			{
				DataParams paramsValue = new DataParams();
				paramsValue.Add(new DataParam("ADeviceID", process.DataTypes.SystemInteger, Modifier.In, ID));
				IServerExpressionPlan plan = 
					((IServerProcess)process).PrepareExpression
					(
						@"
							select System.ObjectDependencies where Dependency_Object_ID = ADeviceID { Object_ID ID }
								join (Objects where Type = 'BaseTableVar' { ID })
						",
						paramsValue
					);
				try
				{
					IServerCursor cursor = plan.Open(paramsValue);
					try
					{
						while (cursor.Next())
						{
							using (IRow row = cursor.Select())
							{
								serverCatalog.Add(process.CatalogDeviceSession.ResolveCatalogObject((int)row[0]));
							}
						}
					}
					finally
					{
						plan.Close(cursor);
					}
				}
				finally
				{
					((IServerProcess)process).UnprepareExpression(plan);
				}
			}

			return serverCatalog;
        }
        
        public ErrorList Reconcile(ServerProcess process)
        {
			Catalog serverCatalog = GetServerCatalog(process, null);
			return Reconcile(process, serverCatalog, GetDeviceCatalog(process, serverCatalog));
        }
        
        public ErrorList Reconcile(ServerProcess process, TableVar tableVar)
        {			
			Catalog serverCatalog = GetServerCatalog(process, tableVar);
			return Reconcile(process, serverCatalog, GetDeviceCatalog(process, serverCatalog, tableVar));
        }
        
        public void CheckReconcile(ServerProcess process, TableVar tableVar)
        {
			if ((ReconcileMode & ReconcileMode.Automatic) != 0)
			{
				ErrorList errors = Reconcile(process, tableVar);	
				if (errors.Count > 0)
					throw errors[0];
			}
        }
        
        // CreateTable
		protected virtual void CreateServerTableInDevice(ServerProcess process, TableVar tableVar)
		{
			Error.Fail("Device does not support reconciliation of server tables to device.");
		}

		protected virtual void CreateDeviceTableInServer(ServerProcess process, TableVar tableVar)
		{
			// Add the TableVar to the Catalog
			// Note that this does not call the usual CreateTable method because there is no need to request device storage.
			Plan plan = new Plan(process);
			try
			{
				plan.PlanCatalog.Add(tableVar);
				try
				{
					plan.PushCreationObject(tableVar);
					try
					{
						CheckSupported(plan, tableVar);
						if (!process.ServerSession.Server.IsEngine)
							Compiler.CompileTableVarKeyConstraints(plan, tableVar);
					}
					finally
					{
						plan.PopCreationObject();
					}
				}
				finally
				{
					plan.PlanCatalog.Remove(tableVar);
				}
			}
			finally
			{
				plan.Dispose();
			}

			process.CatalogDeviceSession.InsertCatalogObject(tableVar);
		}

		protected virtual void CreateTable(ServerProcess process, TableVar tableVar, D4.ReconcileMaster master)
		{
			if (master == D4.ReconcileMaster.Server)
			{
				CreateServerTableInDevice(process, tableVar);
			}
			else if (master == D4.ReconcileMaster.Device)
			{
				CreateDeviceTableInServer(process, tableVar);
			}
		}
		
        // ReconcileTable
		protected virtual D4.AlterTableStatement ReconcileTable(Plan plan, TableVar sourceTableVar, TableVar targetTableVar, ReconcileOptions options, out bool reconciliationRequired)
		{
			Error.Fail("Device does not support table-level reconciliation.");
			reconciliationRequired = false;
			return null;
		}

		protected virtual void ReconcileServerTableToDevice(ServerProcess process, TableVar serverTableVar, TableVar deviceTableVar)
		{
			using (Plan plan = new Plan(process))
			{
				bool reconciliationRequired;
				D4.AlterTableStatement statement = ReconcileTable(plan, serverTableVar, deviceTableVar, ReconcileOptions.All, out reconciliationRequired);
				if (reconciliationRequired)
				{
					D4.ReconcileMode saveMode = ReconcileMode;
					try
					{
						ReconcileMode = D4.ReconcileMode.None; // turn off reconciliation to avoid a command being re-issued to the target system
						Program program = new Program(process);
						program.Code = Compiler.Compile(plan, statement);
						plan.CheckCompiled();
						program.Start(null);
						try
						{
							program.DeviceExecute(this, program.Code);
						}
						finally
						{
							program.Stop(null);
						}
					}
					finally
					{
						ReconcileMode = saveMode;
					}
				}
			}
		}

		protected virtual void ReconcileDeviceTableToServer(ServerProcess process, TableVar deviceTableVar, TableVar serverTableVar)
		{
			using (Plan plan = new Plan(process))
			{
				bool reconciliationRequired;
				D4.AlterTableStatement statement = ReconcileTable(plan, deviceTableVar, serverTableVar, ReconcileOptions.None, out reconciliationRequired);
				if (reconciliationRequired)
				{
					D4.ReconcileMode saveMode = ReconcileMode;
					try
					{
						ReconcileMode = D4.ReconcileMode.None; // turn off reconciliation to avoid a command being re-issued to the target system
						Program program = new Program(process);
						program.Code = Compiler.Compile(plan, statement);
						plan.CheckCompiled();
						program.Execute(null);
					}
					finally
					{
						ReconcileMode = saveMode;
					}
				}
			}
		}

		protected virtual void ReconcileTable(ServerProcess process, TableVar serverTableVar, TableVar deviceTableVar, D4.ReconcileMaster master)
		{
			if ((master == D4.ReconcileMaster.Server) || (master == D4.ReconcileMaster.Both))
			{
				ReconcileServerTableToDevice(process, serverTableVar, deviceTableVar);
			}
			
			if ((master == D4.ReconcileMaster.Device) || (master == D4.ReconcileMaster.Both))
			{
				ReconcileDeviceTableToServer(process, deviceTableVar, serverTableVar);
			}
		}

		// CreateReference
		public virtual void CreateReference(ServerProcess process, Reference reference, D4.ReconcileMaster master)
		{
			if (master == D4.ReconcileMaster.Server)
			{
				Error.Fail("Reconciliation of foreign keys to the target device is not yet implemented");
			}
			else if (master == D4.ReconcileMaster.Device)
			{
				using (Plan plan = new Plan(process))
				{
					Program program = new Program(process);
					program.Code = Compiler.CompileCreateReferenceStatement(plan, reference.EmitStatement(D4.EmitMode.ForCopy));
					program.Execute(null);
				}
			}
		}
		
		// ReconcileReference
		public virtual void ReconcileReference(ServerProcess process, Reference serverReference, Reference deviceReference, D4.ReconcileMaster master)
		{
			if ((master == D4.ReconcileMaster.Server) || (master == D4.ReconcileMaster.Both))
			{
				// TODO: Reference reconciliation
			}
			
			if ((master == D4.ReconcileMaster.Device) || (master == D4.ReconcileMaster.Both))
			{
				// TODO: Reference reconciliation
			}
		}
		
        // GetDeviceCatalog
        public virtual Catalog GetDeviceCatalog(ServerProcess process, Catalog serverCatalog, TableVar tableVar) { return new Catalog(); }
        public Catalog GetDeviceCatalog(ServerProcess process, Catalog serverCatalog)
        {
			return GetDeviceCatalog(process, serverCatalog, null);
        }
        
        // DeviceOperators
        private DeviceOperators _deviceOperators = new DeviceOperators();

		/// <summary>Resolves the operator map for the given operator, caching it if it exists. Returns null if the operator is not mapped.</summary>
        public DeviceOperator ResolveDeviceOperator(Plan plan, Schema.Operator operatorValue)
        {
			Schema.DeviceOperator deviceOperator;
			if (!_deviceOperators.TryGetValue(operatorValue, out deviceOperator))
				deviceOperator = plan.CatalogDeviceSession.ResolveDeviceOperator(this, operatorValue);
			if (deviceOperator != null)
				plan.AttachDependency(deviceOperator);
			return deviceOperator;
        }
        
        /// <summary>Returns true if this device contains a mapping for the given operator, and it is in the cache.</summary>
        /// <remarks>
        /// Note that this method does not perform any resolution. It is only used for cache maintenance.
        /// </remarks>
        public bool HasDeviceOperator(Schema.Operator operatorValue)
        {
			return _deviceOperators.ContainsKey(operatorValue);
        }
        
        /// <summary>Adds the given operator map to the cache.</summary>
        public void AddDeviceOperator(Schema.DeviceOperator deviceOperator)
        {
			_deviceOperators.Add(deviceOperator);
        }
        
        /// <summary>Removes the given operator map from the cache.</summary>
        public void RemoveDeviceOperator(Schema.DeviceOperator deviceOperator)
        {
			_deviceOperators.Remove(deviceOperator.Operator);
        }
        
        // DeviceScalarTypes
        private DeviceScalarTypes _deviceScalarTypes = new DeviceScalarTypes();

		/// <summary>Returns the scalar type map for the given scalar type, caching it if it exists. Returns null if the scalar type is not mapped.</summary>
        public DeviceScalarType ResolveDeviceScalarType(Plan plan, Schema.ScalarType scalarType)
        {
			Schema.DeviceScalarType deviceScalarType;
			if (!_deviceScalarTypes.TryGetValue(scalarType, out deviceScalarType))
				deviceScalarType = plan.CatalogDeviceSession.ResolveDeviceScalarType(this, scalarType);
			if (deviceScalarType != null)
				plan.AttachDependency(deviceScalarType);
			return deviceScalarType;
        }
        
        /// <summary>Returns true if this device contains a mapping for the given scalar type and it is in the cache.</summary>
        /// <remarks>
        /// Note that this method does not perform any resolution. It is only used for cache maintenance.
        /// </remarks>
        public bool HasDeviceScalarType(Schema.ScalarType scalarType)
        {
			return _deviceScalarTypes.ContainsKey(scalarType);
        }
        
		/// <summary>Adds the given scalar type map to the cache.</summary>
        public void AddDeviceScalarType(Schema.DeviceScalarType deviceScalarType)
        {
			_deviceScalarTypes.Add(deviceScalarType);
        }
        
        /// <summary>Removes the given scalar type map from the cache.</summary>
        public void RemoveDeviceScalarType(Schema.DeviceScalarType deviceScalarType)
        {
			_deviceScalarTypes.Remove(deviceScalarType.ScalarType);
        }
        
        // RequiresAuthentication
        private bool _requiresAuthentication;
        /// <summary>Indicates whether the device will attempt to resolve a device user when establishing a connection.</summary>
        /// <remarks>
        /// For the internal devices such as the catalog, temp, and A/T devices, the DAE manages access security, and no
        /// user device mapping is required.
        /// </remarks>
        public bool RequiresAuthentication
        {
			get { return _requiresAuthentication; }
			set { _requiresAuthentication = false; }
		}
        
        // DeviceUsers
        private DeviceUsers _users = new DeviceUsers();
        public DeviceUsers Users { get { return _users; } }
        
        // UserID
		private string _userID = String.Empty;
		public string UserID { get { return _userID; } set { _userID = value == null ? String.Empty : value; } }
		
		// Password
		private string _password = String.Empty;
		public string Password { get { return _password; } set { _password = value == null ? String.Empty : SecurityUtility.EncryptPassword(value); } }
		
		// EncryptedPassword
		public string EncryptedPassword { get { return _password; } set { _password = value == null ? String.Empty : value; } }

		// ConnectionParameters
		private string _connectionParameters = String.Empty;
		public string ConnectionParameters { get { return _connectionParameters; } set { _connectionParameters = value == null ? String.Empty : value; } } 

		public DeviceSessionInfo GetDeviceSessionInfo(ServerProcess process, User user, SessionInfo sessionInfo)
		{
			DeviceUser localUser = null;
			if (_requiresAuthentication)
				localUser = process.CatalogDeviceSession.ResolveDeviceUser(this, user, false);
			DeviceSessionInfo deviceSessionInfo = null;
			if (localUser != null)
				deviceSessionInfo = new DeviceSessionInfo(localUser.DeviceUserID, SecurityUtility.DecryptPassword(localUser.DevicePassword), localUser.ConnectionParameters);
			else
			{
				if (UserID != String.Empty)
					deviceSessionInfo = new DeviceSessionInfo(UserID, SecurityUtility.DecryptPassword(Password), ConnectionParameters);
			}

			if (deviceSessionInfo == null)
				deviceSessionInfo = new DeviceSessionInfo(sessionInfo.UserID, sessionInfo.Password);
			return deviceSessionInfo;
		}
		
        // ReconcileMode
        private ReconcileMode _reconcileMode;
        public ReconcileMode ReconcileMode
        {
			get { return _reconcileMode; }
			set { _reconcileMode = value; }
        }

        // ReconcileMaster
        private ReconcileMaster _reconcileMaster;
        public ReconcileMaster ReconcileMaster
        {
			get { return _reconcileMaster; }
			set { _reconcileMaster = value; }
        }
        
		// verify that all types in the given table are mapped into this device
        public virtual void CheckSupported(Plan plan, TableVar tableVar)
        {
        }
        
        // Capabilities
		public virtual DeviceCapability Capabilities
		{
			get { return 0; }
        }
        
        // Supports
        public bool Supports(DeviceCapability capability)
        {
			return (Capabilities & capability) != 0;
        }
        
        protected void CheckCapability(DeviceCapability capability)
        {
			if (!Supports(capability))
				throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, Enum.GetName(typeof(DeviceCapability), capability), Name);
        }
        
        // SupportsTransactions
        protected bool _supportsTransactions;
		public bool SupportsTransactions { get { return _supportsTransactions; } }
		
		// SupportsNestedTransactions
		protected bool _supportsNestedTransactions;
		public bool SupportsNestedTransactions { get { return _supportsNestedTransactions; } }

        public override Statement EmitStatement(EmitMode mode)
        {
			if (mode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveRegistered();
			}
			else
			{
				RemoveObjectID();
				RemoveRegistered();
			}
			try
			{
				CreateDeviceStatement statement = new CreateDeviceStatement();
				statement.DeviceName = Schema.Object.EnsureRooted(Name);
				statement.ClassDefinition = ClassDefinition == null ? null : (ClassDefinition)ClassDefinition.Clone();
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				statement.ReconciliationSettings = new ReconciliationSettings();
				statement.ReconciliationSettings.ReconcileMaster = ReconcileMaster;
				statement.ReconciliationSettings.ReconcileMode = ReconcileMode;

				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
				{
					RemoveObjectID();
					RemoveRegistered();
				}
			}
        }
        
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropDeviceStatement statement = new DropDeviceStatement();
			statement.ObjectName = Schema.Object.EnsureRooted(Name);
			return statement;
		}
		
		public override string[] GetRights()
		{
			return new string[]
			{
				Name + Schema.RightNames.Alter,
				Name + Schema.RightNames.Drop,
				Name + Schema.RightNames.Read,
				Name + Schema.RightNames.Write,
				Name + Schema.RightNames.CreateStore,
				Name + Schema.RightNames.AlterStore,
				Name + Schema.RightNames.DropStore,
				Name + Schema.RightNames.Reconcile,
				Name + Schema.RightNames.MaintainUsers
			};
		}
		
		public virtual ClassDefinition GetDefaultOperatorClassDefinition(MetaData metaData)
		{
			return null;
		}
		
		public virtual ClassDefinition GetDefaultSelectorClassDefinition()
		{
			return null;
		}
		
		public virtual ClassDefinition GetDefaultReadAccessorClassDefinition()
		{
			return null;
		}
		
		public virtual ClassDefinition GetDefaultWriteAccessorClassDefinition()
		{
			return null;
		}
    }
    
    public class DeviceSetting : System.Object
    {
		private string _deviceName;
		public string DeviceName
		{
			get { return _deviceName; }
			set { _deviceName = value; }
		}
		
		private string _settingName;
		public string SettingName
		{
			get { return _settingName; }
			set { _settingName = value; }
		}
		
		private string _settingValue;
		public string SettingValue
		{
			get { return _settingValue; }
			set { _settingValue = value; }
		}
		
		public override string ToString()
		{
			return String.Format("{0}\\{1}={2}", _deviceName, _settingName, _settingValue);
		}
	}
	
	#if USETYPEDLIST
	public class DeviceSettings : TypedList
	{
		public DeviceSettings() : base(typeof(DeviceSetting)) { }
		
		public new DeviceSetting this[int AIndex]
		{
			get { return (DeviceSetting)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	#else
	public class DeviceSettings : BaseList<DeviceSetting>
	{
	#endif
		public DeviceSettings GetSettingsForDevice(string deviceName)
		{
			DeviceSettings result = new DeviceSettings();
			for (int index = 0; index < Count; index++)
				if (this[index].DeviceName == deviceName)
					result.Add(this[index]);
			return result;
		}
	}
	
	public class DeviceSessionInfo : System.Object
	{
		public DeviceSessionInfo(string userName, string password) : base()
		{
			UserName = userName;
			Password = password;
			ConnectionParameters = string.Empty;
		}

		public DeviceSessionInfo(string userName, string password, string connectionParameters) : base()
		{
			UserName = userName;
			Password = password;
			ConnectionParameters = connectionParameters;
		}

		private string _userName;
		public string UserName 
		{ 
			get { return _userName; } 
			set { _userName = value; } 
		}

		private string _password;
		public string Password 
		{
			get { return _password; } 
			set { _password = value; } 
		} 

		private string _connectionParameters;
		public string ConnectionParameters 
		{ 
			get { return _connectionParameters; } 
			set { _connectionParameters = value; } 
		}
	}

	public class DeviceTransaction : Disposable
	{
		public DeviceTransaction(IsolationLevel isolationLevel) : base()
		{
			_isolationLevel = isolationLevel;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_operations != null)
			{
				foreach (Operation operation in _operations)
					operation.Dispose();
				_operations.Clear();
				_operations = null;
			}

			base.Dispose(disposing);
		}		

		private Operations _operations = new Operations();
		public Operations Operations { get { return _operations; } }
		
		private IsolationLevel _isolationLevel;
		public IsolationLevel IsolationLevel { get { return _isolationLevel; } }
	}
	
	#if USETYPEDLIST
	public class DeviceTransactions : TypedList
	{
		public DeviceTransactions() : base(typeof(DeviceTransaction)){}
		
		public new DeviceTransaction this[int AIndex]
		{
			get { return (DeviceTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
	#else
	public class DeviceTransactions : BaseList<DeviceTransaction>
	{
	#endif
		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			Add(new DeviceTransaction(isolationLevel));
		}
		
		public void EndTransaction(bool success)
		{
			// If we successfully committed a nested transaction, append it's log to the current transaction so that a subsequent rollback will undo it's affects as well.
			if ((success) && (Count > 1))
			{
				#if USETYPEDLIST
				DeviceTransaction transaction = (DeviceTransaction)RemoveItemAt(Count - 1);
				#else
				DeviceTransaction transaction = RemoveAt(Count - 1);
				#endif
				CurrentTransaction().Operations.AddRange(transaction.Operations);
				transaction.Operations.Clear();
				transaction.Dispose();
			}
			else
				#if USETYPEDLIST
				((DeviceTransaction)RemoveItemAt(Count - 1)).Dispose();
				#else
				RemoveAt(Count - 1).Dispose();
				#endif
		}
		
		public DeviceTransaction CurrentTransaction()
		{
			return this[Count - 1];
		}
	}
	
	public abstract class DeviceSession : Disposable
    {		
		protected internal DeviceSession(Device device, ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo) : base()
		{
			_device = device;
			_serverProcess = serverProcess;
			_deviceSessionInfo = deviceSessionInfo;
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_transactions != null)
				{
					EnsureTransactionsRolledback();
					_transactions = null;
				}
			}
			finally
			{
				_device = null;
				_serverProcess = null;
				_deviceSessionInfo = null;

				base.Dispose(disposing);
			}
		}
		
		[Reference]
		private Device _device;
		public Device Device { get { return _device; } }
		
		// ServerProcess
		[Reference]
		private ServerProcess _serverProcess;
		public ServerProcess ServerProcess { get { return _serverProcess; } } 

		// DeviceSessionInfo
		private DeviceSessionInfo _deviceSessionInfo;
		public DeviceSessionInfo DeviceSessionInfo { get { return _deviceSessionInfo; } }
		
		#if REFERENCECOUNTDEVICESESSIONS
		protected internal int FReferenceCount;
		#endif
		
		// Exception Handling
        protected Exception InternalWrapException(Exception exception)
        {
			return exception;
        }
        
        protected virtual bool IsConnectionFailure(Exception exception)
        {
			return false;
        }
        
        protected virtual bool IsTransactionFailure(Exception exception)
        {
			return false;
        }

		// All exceptions from the device layer must come through this point
        public Exception WrapException(Exception exception)
        {
			// IsConnectionFailure
			if (IsConnectionFailure(exception))
			{
				ServerProcess.StopError = true;
				exception = new DeviceException(DeviceException.Codes.ConnectionFailure, ErrorSeverity.Environment, exception, Device.Name);
			}

			// IsTransactionFailure
			if (IsTransactionFailure(exception))
			{
				ServerProcess.StopError = true;
				exception = new DeviceException(DeviceException.Codes.TransactionFailure, ErrorSeverity.Environment, exception, Device.Name);
			}
			
			return InternalWrapException(exception);
        }

		// Transactions
		private DeviceTransactions _transactions = new DeviceTransactions();
		public DeviceTransactions Transactions { get { return _transactions; } }
		
        // BeginTransaction
        protected abstract void InternalBeginTransaction(IsolationLevel isolationLevel);
        public void BeginTransaction(IsolationLevel isolationLevel)
        {
			try
			{
				_transactions.BeginTransaction(isolationLevel);
				InternalBeginTransaction(isolationLevel);
			} 
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
        }
        
        // PrepareTransaction
        protected abstract void InternalPrepareTransaction();
        public void PrepareTransaction()
        {
			CheckInTransaction();
			try
			{
				InternalPrepareTransaction();
			} 
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
		}

		// CommitTransaction
		protected abstract void InternalCommitTransaction();
		public void CommitTransaction()
		{
			CheckInTransaction();
			try
			{
				InternalCommitTransaction();
				_transactions.EndTransaction(true);
			} 
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
		}
		
		protected void RemoveTransactionReferences(DeviceTransaction transaction, Schema.TableVar tableVar)
		{
			Operation operation;
			for (int operationIndex = transaction.Operations.Count - 1; operationIndex >= 0; operationIndex--)
			{
				operation = transaction.Operations[operationIndex];
				if (operation.TableVar.Equals(tableVar))
				{
					transaction.Operations.RemoveAt(operationIndex);
					operation.Dispose();
				}
			}
		}
		
		protected void RemoveTransactionReferences(Schema.TableVar tableVar)
		{
			foreach (DeviceTransaction transaction in _transactions)
				RemoveTransactionReferences(transaction, tableVar);
		}
		
		// RollbackTransaction
		protected void InternalRollbackTransaction(DeviceTransaction transaction)
		{
			Operation operation;
			InsertOperation insertOperation;
			UpdateOperation updateOperation;
			DeleteOperation deleteOperation;
			Exception exception = null;
			Program program = new Program(ServerProcess);
			program.Start(null);
			try
			{
				for (int index = transaction.Operations.Count - 1; index >= 0; index--)
				{
					try
					{
						operation = transaction.Operations[index];
						try
						{
							insertOperation = operation as InsertOperation;
							if (insertOperation != null)
								InternalInsertRow(program, insertOperation.TableVar, insertOperation.Row, insertOperation.ValueFlags);

							updateOperation = operation as UpdateOperation;
							if (updateOperation != null)
								InternalUpdateRow(program, updateOperation.TableVar, updateOperation.OldRow, updateOperation.NewRow, updateOperation.ValueFlags);

							deleteOperation = operation as DeleteOperation;
							if (deleteOperation != null)
								InternalDeleteRow(program, deleteOperation.TableVar, deleteOperation.Row);
						}
						finally
						{
							transaction.Operations.RemoveAt(index);
							operation.Dispose();
						}
					}
					catch (Exception E)
					{
						exception = E;
						ServerProcess.ServerSession.Server.LogError(E);
					}
				}
			}
			finally
			{
				program.Stop(null);
			}
			
			if (exception != null)
				throw exception;
		}
		
		protected abstract void InternalRollbackTransaction();
		public void RollbackTransaction()
		{
			CheckInTransaction();
			try
			{
				try
				{
					try
					{
						InternalRollbackTransaction(_transactions.CurrentTransaction());
					}
					finally
					{
						InternalRollbackTransaction();
					}
				}
				finally
				{
					_transactions.EndTransaction(false);
				}
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
		}
		
		// InTransaction
		public bool InTransaction { get { return _transactions.Count > 0; } }

        protected void CheckInTransaction()
        {
            if (!InTransaction)
                throw new SchemaException(SchemaException.Codes.NoActiveTransaction);
        }
        
        // EnsureTransactionsRolledback
        protected void EnsureTransactionsRolledback()
        {
			while (_transactions.Count > 0)
				RollbackTransaction();
        }
        
        // Execute
        protected virtual object InternalExecute(Program program, PlanNode planNode)
        {
			throw new DAE.Device.DeviceException(DAE.Device.DeviceException.Codes.InvalidExecuteRequest, Device.Name, planNode.GetType().Name);
        }
        
        public object Execute(Program program, PlanNode planNode)
        {
			var dropTableNode = planNode as DropTableNode;
			if (dropTableNode != null)
				RemoveTransactionReferences(dropTableNode.Table);

			try
			{
				return InternalExecute(program, planNode);
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}
        }
        
        protected void CheckCapability(DeviceCapability capability)
        {
			if (!Device.Supports(capability))
				throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, Enum.GetName(typeof(DeviceCapability), capability), Device.Name);
        }
        
        // Row level operations are provided as implementation details only, not exposed through the Dataphor language.
        protected virtual void InternalInsertRow(Program program, TableVar table, IRow row, BitArray valueFlags)
        {
			throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, DeviceCapability.RowLevelInsert, Device.Name);
        }

        public void InsertRow(Program program, TableVar table, IRow row, BitArray valueFlags)
        {
			if (ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			CheckCapability(DeviceCapability.RowLevelInsert);

			try
			{
				InternalInsertRow(program, table, row, valueFlags);
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}

			if (!ServerProcess.NonLogged && ((!Device.SupportsTransactions && (Transactions.Count == 1)) || (!Device.SupportsNestedTransactions && (Transactions.Count > 1))) && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new DeleteOperation(table, (IRow)row.Copy()));
        }
        
        protected virtual void InternalUpdateRow(Program program, TableVar table, IRow oldRow, IRow newRow, BitArray valueFlags)
        {
			throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, DeviceCapability.RowLevelUpdate, Device.Name);
        }
        
        public void UpdateRow(Program program, TableVar table, IRow oldRow, IRow newRow, BitArray valueFlags)
        {
			if (ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			CheckCapability(DeviceCapability.RowLevelUpdate);
			
			try
			{
				InternalUpdateRow(program, table, oldRow, newRow, valueFlags);
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}

			if (!ServerProcess.NonLogged && ((!Device.SupportsTransactions && (Transactions.Count == 1)) || (!Device.SupportsNestedTransactions && (Transactions.Count > 1))) && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new UpdateOperation(table, (IRow)newRow.Copy(), (IRow)oldRow.Copy(), valueFlags));
        }
        
        protected virtual void InternalDeleteRow(Program program, TableVar table, IRow row)
        {
			throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, DeviceCapability.RowLevelDelete, Device.Name);
		}
        
        public void DeleteRow(Program program, TableVar table, IRow row)
        {
			if (ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			CheckCapability(DeviceCapability.RowLevelDelete);
			try
			{
				InternalDeleteRow(program, table, row);
			}
			catch (Exception exception)
			{
				throw WrapException(exception);
			}

			if (!ServerProcess.NonLogged && ((!Device.SupportsTransactions && (Transactions.Count == 1)) || (!Device.SupportsNestedTransactions && (Transactions.Count > 1))) && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new InsertOperation(table, (IRow)row.Copy(), null));
        }
    }

	#if USETYPEDLIST    
	public class DeviceSessions : DisposableTypedList
    {
		public DeviceSessions() : base()
		{
			FItemsOwned = true;
			FItemType = typeof(DeviceSession);
		}
		
		public DeviceSessions(bool AItemsOwned) : base()
		{
			FItemsOwned = AItemsOwned;
			FItemType = typeof(DeviceSession);
		}
		
		public new DeviceSession this[int AIndex]
		{
			get { return (DeviceSession)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	#else
	public class DeviceSessions : DisposableList<DeviceSession>
	{
		public DeviceSessions() : base() { }
		public DeviceSessions(bool itemsOwned) : base(itemsOwned) { }
	#endif

		public int IndexOf(Device device)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].Device == device)
					return index;
			return -1;
		}
		
		public bool Contains(Device device)
		{
			return IndexOf(device) >= 0;
		}
		
		public DeviceSession this[Device device]
		{
			get { return this[IndexOf(device)]; }
		}
		
		private object _syncRoot = new object();
		public object SyncRoot { get { return _syncRoot; } }
    }

	public class DeviceUser : System.Object
	{
		public DeviceUser() : base(){}
		public DeviceUser(User user, Device device, string deviceUserID, string devicePassword) : base()
		{
			User = user;
			Device = device;
			DeviceUserID = deviceUserID;
			DevicePassword = devicePassword;
		}
		
		public DeviceUser(User user, Device device, string deviceUserID, string devicePassword, string connectionParameters) : base()
		{
			User = user;
			Device = device;
			DeviceUserID = deviceUserID;
			DevicePassword = devicePassword;
			ConnectionParameters = connectionParameters;
		}

		public DeviceUser(User user, Device device, string deviceUserID, string devicePassword, bool encryptPassword) : base()
		{
			User = user;
			Device = device;
			DeviceUserID = deviceUserID;
			if (encryptPassword)
				DevicePassword = Schema.SecurityUtility.EncryptPassword(devicePassword);
			else
				DevicePassword = devicePassword;
		}

		public DeviceUser(User user, Device device, string deviceUserID, string devicePassword, bool encryptPassword, string connectionParameters) : base()
		{
			User = user;
			Device = device;
			DeviceUserID = deviceUserID;
			if (encryptPassword)
				DevicePassword = Schema.SecurityUtility.EncryptPassword(devicePassword);
			else
				DevicePassword = devicePassword;
			ConnectionParameters = connectionParameters;
		}
		
		[Reference]
		private User _user;
		public User User { get { return _user; } set { _user = value; } }
		
		[Reference]
		private Device _device;
		public Device Device { get { return _device; } set { _device = value; } }
		
		private string _deviceUserID = String.Empty;
		public string DeviceUserID { get { return _deviceUserID; } set { _deviceUserID = value == null ? String.Empty : value; } }
		
		private string _devicePassword = String.Empty;
		public string DevicePassword { get { return _devicePassword; } set { _devicePassword = value == null ? String.Empty : value; } }

		private string _connectionParameters = String.Empty;
		public string ConnectionParameters { get { return _connectionParameters; } set { _connectionParameters = value == null ? String.Empty : value; } } 
	}
	
	public class DeviceUsers : Dictionary<string, DeviceUser>
	{		
		public DeviceUsers() : base(StringComparer.OrdinalIgnoreCase){}
		
		public void Add(DeviceUser deviceUser)
		{
			Add(deviceUser.User.ID, deviceUser);
		}
	}

    public class TranslationMessage
    {
		public TranslationMessage(string message) : base()
		{
			_message = message;
		}
		
		public TranslationMessage(string message, PlanNode context) : base()
		{
			_message = message;
			if (context.Line == -1)
				_context = context.SafeEmitStatementAsString();
			else
			{
			    _line = context.Line;
			    _linePos = context.LinePos;
			}
		}
		
		private string _message;
		public string Message { get { return _message; } }
		
		private int _line = -1;
		public int Line
		{
			get { return _line; }
			set { _line = value; }
		}
		
		private int _linePos = -1;
		public int LinePos
		{
			get { return _linePos; }
			set { _linePos = value; }
		}
		
		private string _context = null;
		public string Context
		{
			get { return _context; }
			set { _context = value; }
		}
		
		public override string ToString()
		{
			if (_linePos == -1)
				return String.Format("{0}\r\n\t{1}", _message, _context);

			if (_context == null)
				return _message;

			return String.Format("{0} ({1}, {2})", _message, _line.ToString(), _linePos.ToString());
		}
    }

	#if USETYPEDLIST
	public class TranslationMessages : TypedList
	{
		public TranslationMessages() : base(typeof(TranslationMessage)) {}
		
		public new TranslationMessage this[int AIndex]
		{
			get { return (TranslationMessage)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class TranslationMessages : BaseList<TranslationMessage>
	{
	#endif
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach (TranslationMessage message in this)
				ExceptionUtility.AppendMessage(builder, 1, message.ToString());
			return builder.ToString();
		}
	}
}