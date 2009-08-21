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
    
    public class DevicePlan : Disposable
    {
		public DevicePlan(Plan APlan, Device ADevice, PlanNode APlanNode)
		{   
			FPlan = APlan;
			FDevice = ADevice;
			FPlanNode = APlanNode;
		}

		protected override void Dispose(bool ADisposing)
		{
			FPlan = null;
			FDevice = null;
			FPlanNode = null;
			base.Dispose(ADisposing);
		}

		// Plan
		[Reference]
		private Plan FPlan;
		public Plan Plan { get { return FPlan; } }

		// Device
		[Reference]
		private Device FDevice;
		public Device Device { get { return FDevice; } }

		// PlanNode
		[Reference]
        private PlanNode FPlanNode;
		public PlanNode Node { get { return FPlanNode; } }
		
		// IsSupported
		private bool FIsSupported = true;
		public bool IsSupported 
		{ 
			get { return FIsSupported; } 
			set { FIsSupported = value; }
		}
		
		// TranslationMessages
		private TranslationMessages FTranslationMessages = new TranslationMessages();
		public TranslationMessages TranslationMessages { get { return FTranslationMessages; } }
	}
    
    public abstract class DeviceObject : Schema.CatalogObject
    {
		public DeviceObject(int AID, string AName) : base(AID, AName)
		{
			IsRemotable = false;
		}

		public DeviceObject(int AID, string AName, bool AIsSystem) : base(AID, AName)
		{
			IsRemotable = false;
			IsSystem = AIsSystem;
		}
		
		public override string DisplayName { get { return String.Format("Device_{0}_Map_{1}", Device.Name, Name); } }

		[Reference]
		private Device FDevice;
		public Device Device 
		{ 
			get { return FDevice; } 
			set { FDevice = value; }
		}
    }
    
    public abstract class DeviceOperator : DeviceObject
    {		
		public DeviceOperator(int AID, string AName) : base(AID, AName){}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.DeviceOperator"), FOperator.OperatorName, FOperator.Signature.ToString(), Device.DisplayName); } }

		// Operator		
		[Reference]
		private Schema.Operator FOperator;
		public Schema.Operator Operator
		{
			get { return FOperator; }
			set { FOperator = value; }
		}
		
        // ClassDefinition
        private ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
        }
        
		public abstract Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode);
		
		private void EmitOperatorSpecifier(DeviceOperatorMapBase AOperatorMap, EmitMode AMode)
		{
			AOperatorMap.OperatorSpecifier = new OperatorSpecifier();
			AOperatorMap.OperatorSpecifier.OperatorName = Schema.Object.EnsureRooted(Operator.OperatorName);
			foreach (Operand LOperand in Operator.Operands)
			{
				FormalParameterSpecifier LSpecifier = new FormalParameterSpecifier();
				LSpecifier.Modifier = LOperand.Modifier;
				LSpecifier.TypeSpecifier = LOperand.DataType.EmitSpecifier(AMode);
				AOperatorMap.OperatorSpecifier.FormalParameterSpecifiers.Add(LSpecifier);
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AlterDeviceStatement LStatement = new AlterDeviceStatement();
			LStatement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			LStatement.CreateDeviceOperatorMaps.Add(EmitCreateDefinition(AMode));
			return LStatement;
		}
		
		public DeviceOperatorMap EmitCreateDefinition(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			DeviceOperatorMap LOperatorMap = new DeviceOperatorMap();
			EmitOperatorSpecifier(LOperatorMap, AMode);
			LOperatorMap.ClassDefinition = ClassDefinition == null ? null : (ClassDefinition)ClassDefinition.Clone();
			LOperatorMap.MetaData = MetaData == null ? null : MetaData.Copy();
			return LOperatorMap;
		}
		
		public DropDeviceOperatorMap EmitDropDefinition(EmitMode AMode)
		{
			DropDeviceOperatorMap LOperatorMap = new DropDeviceOperatorMap();
			EmitOperatorSpecifier(LOperatorMap, AMode);
			return LOperatorMap;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			AlterDeviceStatement LStatement = new AlterDeviceStatement();
			LStatement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			LStatement.DropDeviceOperatorMaps.Add(EmitDropDefinition(AMode));
			return LStatement;
		}
    }
    
	public class DeviceOperators : Hashtable
	{		
		public DeviceOperators() : base(){}
		
		public DeviceOperator this[Operator AOperator]
		{
			get
			{
				DeviceOperator LResult = (DeviceOperator)base[AOperator];
				return LResult;
			}
		}
		
		public void Add(DeviceOperator AOperator)
		{
			Add(AOperator.Operator, AOperator);
		}
	}

    public abstract class DeviceScalarType : DeviceObject
    {
		public DeviceScalarType(int AID, string AName) : base(AID, AName) {}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.DeviceScalarType"), FScalarType.DisplayName, Device.DisplayName); } }

		// ScalarType
		[Reference]
		private ScalarType FScalarType;
		public ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		// IsDefaultClassDefinition
		private bool FIsDefaultClassDefinition;
		public bool IsDefaultClassDefinition
		{
			get { return FIsDefaultClassDefinition; }
			set { FIsDefaultClassDefinition = value; }
		}
		
        // ClassDefinition
        private ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
        }
        
		/// <summary>Override this method to provide transformation services to the device for a particular data type.</summary>
		public abstract object ToScalar(IServerProcess AProcess, object AValue);
		
		/// <summary>Override this method to provide transformation services to the device for a particular data type.</summary>
		public abstract object FromScalar(object AValue);
		
		/// <summary>Override this method to provide transformation services to the device for stream access data types.</summary>
		public virtual Stream GetStreamAdapter(IServerProcess AProcess, Stream AStream)
		{
			return AStream;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AlterDeviceStatement LStatement = new AlterDeviceStatement();
			LStatement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			LStatement.CreateDeviceScalarTypeMaps.Add(EmitCreateDefinition(AMode));
			return LStatement;
		}
		
		public DeviceScalarTypeMap EmitCreateDefinition(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			DeviceScalarTypeMap LScalarTypeMap = new DeviceScalarTypeMap();
			LScalarTypeMap.ScalarTypeName = Schema.Object.EnsureRooted(ScalarType.Name);
			LScalarTypeMap.ClassDefinition = (IsDefaultClassDefinition || (ClassDefinition == null)) ? null : (ClassDefinition)ClassDefinition.Clone();
			LScalarTypeMap.MetaData = MetaData == null ? null : MetaData.Copy();
			return LScalarTypeMap;
		}
		
		public DropDeviceScalarTypeMap EmitDropDefinition(EmitMode AMode)
		{
			DropDeviceScalarTypeMap LMap = new DropDeviceScalarTypeMap();
			LMap.ScalarTypeName = Schema.Object.EnsureRooted(ScalarType.Name);
			return LMap;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			AlterDeviceStatement LStatement = new AlterDeviceStatement();
			LStatement.DeviceName = Schema.Object.EnsureRooted(Device.Name);
			LStatement.DropDeviceScalarTypeMaps.Add(EmitDropDefinition(AMode));
			return LStatement;
		}
    }
    
	public class DeviceScalarTypes : Hashtable
	{
		public DeviceScalarTypes() : base(){}
		
		public DeviceScalarType this[ScalarType AScalarType]
		{
			get
			{
				DeviceScalarType LResult = (DeviceScalarType)base[AScalarType];
				return LResult;
			}
		}
		
		public void Add(DeviceScalarType AScalarType)
		{
			Add(AScalarType.ScalarType, AScalarType);
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
		public Device(int AID, string AName, int AResourceManagerID) : base(AID, AName)
		{
			FResourceManagerID = AResourceManagerID;
			IsRemotable = false;
			FReconcileMode = ReconcileMode.Command;
			FRequiresAuthentication = true;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Device"), DisplayName); } }

        // ClassDefinition
        private ClassDefinition FClassDefinition;
		public ClassDefinition ClassDefinition
        {
			get { return FClassDefinition; }
			set { FClassDefinition = value; }
        }
        
        // IgnoreUnsupported
        private bool FIgnoreUnsupported;
        public bool IgnoreUnsupported
        {
			get { return FIgnoreUnsupported; }
			set { FIgnoreUnsupported = value; }
		}
        
		// Sessions
        private DeviceSessions FSessions;
        public DeviceSessions Sessions { get { return FSessions; } }
        
        // ResourceManagerID
        private int FResourceManagerID;
        public int ResourceManagerID { get { return FResourceManagerID; } }
        
        // Start
        protected virtual void ApplyDeviceSettings(ServerProcess AProcess)
        {
			foreach (DeviceSetting LSetting in AProcess.ServerSession.Server.DeviceSettings.GetSettingsForDevice(Name))
			{
				try
				{
					ClassLoader.SetProperty(this, LSetting.SettingName, LSetting.SettingValue);
				}
				catch (Exception LException)
				{
					AProcess.ServerSession.Server.LogError(new SchemaException(SchemaException.Codes.ErrorApplyingDeviceSetting, LException, Name, LSetting.SettingName, LSetting.SettingValue));
				}
			}
        }
        
        protected virtual void InternalStart(ServerProcess AProcess)
        {
			FSessions = new DeviceSessions();
        }								  
        
        protected virtual void InternalStarted(ServerProcess AProcess) { }
        
        public void Start(ServerProcess AProcess)
        {
			if (!FRunning)
			{
				try
				{
					ApplyDeviceSettings(AProcess);
					InternalStart(AProcess);
					FRunning = true;
					InternalStarted(AProcess);
				}
				catch
				{
					Stop(AProcess);
					throw;
				}
			}
        }
        
        // Stop
        protected virtual void InternalStop(ServerProcess AProcess)
        {
			if (FSessions != null)
			{
				FSessions.Dispose();
				FSessions = null;
			}
        }

        public void Stop(ServerProcess AProcess)
        {
			if (FRunning)
			{
				InternalStop(AProcess);
				FRunning = false;
			}
        }

        // Running
        private bool FRunning;
        public bool Running { get { return FRunning; } }
        
        protected void CheckRunning()
        {
			if (!FRunning)
				throw new SchemaException(SchemaException.Codes.DeviceNotRunning, Name);
        }
        
        protected void CheckNotRunning()
        {
			if (FRunning)
				throw new SchemaException(SchemaException.Codes.DeviceRunning, Name);
        }
        
        // Registered
        private bool FRegistered;
        public bool Registered { get { return FRegistered; } }
        
		public void LoadRegistered()
		{
			Tag LTag = MetaData.RemoveTag(MetaData, "DAE.Registered");
			if (LTag != Tag.None)
				FRegistered = Boolean.Parse(LTag.Value);
		}

		public void SaveRegistered()
		{
			if (FRegistered)
				MetaData.Tags.AddOrUpdate("DAE.Registered", FRegistered.ToString(), true);
		}
		
		public void RemoveRegistered()
		{
			if (MetaData != null)
				MetaData.Tags.RemoveTag("DAE.Registered");
		}
		
		/// <summary>Used for backwards compatibility only</summary>
		/// <remarks>
		/// This method is used to force the registered flag to be set for devices that have already been registered but were saved to the
		/// catalog before the existence of the registered flag. This method will be deprecated in a future release.
		/// </remarks>
		public void SetRegistered()
		{
			FRegistered = true;
		}
		
		/// <summary>Used by the catalog device to undo the effects of the register call</summary>
		/// <remarks>
		/// This method is only used by the transactional DDL mechansim of the catalog device to ensure that the effects of the register
		/// are rolled back.
		/// </remarks>
		public void ClearRegistered()
		{
			FRegistered = false;
		}

        public void Register(ServerProcess AProcess)
        {
			if (!FRegistered)
			{
				InternalRegister(AProcess);
				FRegistered = true;
			}
        }
        
        protected virtual void InternalRegister(ServerProcess AProcess) { }
        
        // Connect
        protected abstract DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo);
		public DeviceSession Connect(ServerProcess AServerProcess, SessionInfo ASessionInfo)
        {
			DeviceSession LSession = InternalConnect(AServerProcess, GetDeviceSessionInfo(AServerProcess, AServerProcess.ServerSession.User, ASessionInfo));
			try
			{
				lock (FSessions.SyncRoot)
				{
					FSessions.Add(LSession);
				}
				return LSession;
			}
			catch
			{
				Disconnect(LSession);
				throw;
			}
        }
        
        // Disconnect
        public void Disconnect(DeviceSession ASession)
        {
			lock (FSessions.SyncRoot)
			{
				ASession.Dispose();
			}
        }

        // Prepare
        protected abstract DevicePlanNode InternalPrepare(DevicePlan APlan, PlanNode APlanNode);
        public DevicePlan Prepare(Plan APlan, PlanNode APlanNode)
        {
			CheckRunning();
			Schema.DevicePlan LDevicePlan = CreateDevicePlan(APlan, APlanNode);
			if (APlanNode.DeviceNode == null)
			{
				APlanNode.DeviceNode = InternalPrepare(LDevicePlan, APlanNode);
				if (APlanNode.DeviceNode == null)
					LDevicePlan.IsSupported = false;
			}
					
			return LDevicePlan;
        }
        
        protected virtual DevicePlan CreateDevicePlan(Plan APlan, PlanNode APlanNode)
        {
			return new DevicePlan(APlan, this, APlanNode);
        }
        
        protected virtual void InternalUnprepare(DevicePlan APlan) {}
        public void Unprepare(DevicePlan APlan)
        {
			InternalUnprepare(APlan);
			APlan.Dispose();
        }
        
        // Translate
        public virtual Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode) { return null; }
        
        // Reconcile
        public virtual ErrorList Reconcile(ServerProcess AProcess, Catalog AServerCatalog, Catalog ADeviceCatalog) { return new ErrorList(); }
        
        public Catalog GetServerCatalog(ServerProcess AProcess, TableVar ATableVar)
        {
			Catalog LServerCatalog = new Catalog();
			if (ATableVar != null)
				LServerCatalog.Add(ATableVar);
			else
			{
				AProcess.Stack.PushWindow(0);
				try
				{
					DataParams LParams = new DataParams();
					LParams.Add(new DataParam("ADeviceID", AProcess.DataTypes.SystemInteger, Modifier.In, ID));
					IServerExpressionPlan LPlan = 
						((IServerProcess)AProcess).PrepareExpression
						(
							@"
								select System.ObjectDependencies where Dependency_Object_ID = ADeviceID { Object_ID ID }
									join (Objects where Type = 'BaseTableVar' { ID })
							",
							LParams
						);
					try
					{
						IServerCursor LCursor = LPlan.Open(LParams);
						try
						{
							while (LCursor.Next())
							{
								using (Row LRow = LCursor.Select())
								{
									LServerCatalog.Add(AProcess.CatalogDeviceSession.ResolveCatalogObject((int)LRow[0]));
								}
							}
						}
						finally
						{
							LPlan.Close(LCursor);
						}
					}
					finally
					{
						((IServerProcess)AProcess).UnprepareExpression(LPlan);
					}
				}
				finally
				{
					AProcess.Stack.PopWindow();
				}
			}

			return LServerCatalog;
        }
        
        public ErrorList Reconcile(ServerProcess AProcess)
        {
			Catalog LServerCatalog = GetServerCatalog(AProcess, null);
			return Reconcile(AProcess, LServerCatalog, GetDeviceCatalog(AProcess, LServerCatalog));
        }
        
        public ErrorList Reconcile(ServerProcess AProcess, TableVar ATableVar)
        {			
			Catalog LServerCatalog = GetServerCatalog(AProcess, ATableVar);
			return Reconcile(AProcess, LServerCatalog, GetDeviceCatalog(AProcess, LServerCatalog, ATableVar));
        }
        
        public void CheckReconcile(ServerProcess AProcess, TableVar ATableVar)
        {
			if ((ReconcileMode & ReconcileMode.Automatic) != 0)
			{
				ErrorList LErrors = Reconcile(AProcess, ATableVar);	
				if (LErrors.Count > 0)
					throw LErrors[0];
			}
        }
        
        // CreateTable
        public virtual void CreateTable(ServerProcess AProcess, TableVar ATableVar, ReconcileMaster AMaster){}
        
        // ReconcileTable
        public virtual void ReconcileTable(ServerProcess AProcess, TableVar AServerTableVar, TableVar ADeviceTableVar, ReconcileMaster AMaster){}
        
        // GetDeviceCatalog
        public virtual Catalog GetDeviceCatalog(ServerProcess AProcess, Catalog AServerCatalog, TableVar ATableVar) { return new Catalog(); }
        public Catalog GetDeviceCatalog(ServerProcess AProcess, Catalog AServerCatalog)
        {
			return GetDeviceCatalog(AProcess, AServerCatalog, null);
        }
        
        // DeviceOperators
        private DeviceOperators FDeviceOperators = new DeviceOperators();

		/// <summary>Resolves the operator map for the given operator, caching it if it exists. Returns null if the operator is not mapped.</summary>
        public DeviceOperator ResolveDeviceOperator(Plan APlan, Schema.Operator AOperator)
        {
			Schema.DeviceOperator LDeviceOperator = FDeviceOperators[AOperator];
			if (LDeviceOperator == null)
				LDeviceOperator = APlan.CatalogDeviceSession.ResolveDeviceOperator(this, AOperator);
			if (LDeviceOperator != null)
				APlan.AttachDependency(LDeviceOperator);
			return LDeviceOperator;
        }
        
        /// <summary>Returns true if this device contains a mapping for the given operator, and it is in the cache.</summary>
        /// <remarks>
        /// Note that this method does not perform any resolution. It is only used for cache maintenance.
        /// </remarks>
        public bool HasDeviceOperator(Schema.Operator AOperator)
        {
			return FDeviceOperators.Contains(AOperator);
        }
        
        /// <summary>Adds the given operator map to the cache.</summary>
        public void AddDeviceOperator(Schema.DeviceOperator ADeviceOperator)
        {
			FDeviceOperators.Add(ADeviceOperator);
        }
        
        /// <summary>Removes the given operator map from the cache.</summary>
        public void RemoveDeviceOperator(Schema.DeviceOperator ADeviceOperator)
        {
			FDeviceOperators.Remove(ADeviceOperator.Operator);
        }
        
        // DeviceScalarTypes
        private DeviceScalarTypes FDeviceScalarTypes = new DeviceScalarTypes();

		/// <summary>Returns the scalar type map for the given scalar type, caching it if it exists. Returns null if the scalar type is not mapped.</summary>
        public DeviceScalarType ResolveDeviceScalarType(Plan APlan, Schema.ScalarType AScalarType)
        {
			Schema.DeviceScalarType LDeviceScalarType = FDeviceScalarTypes[AScalarType];
			if (LDeviceScalarType == null)
				LDeviceScalarType = APlan.CatalogDeviceSession.ResolveDeviceScalarType(this, AScalarType);
			if (LDeviceScalarType != null)
				APlan.AttachDependency(LDeviceScalarType);
			return LDeviceScalarType;
        }
        
        /// <summary>Returns true if this device contains a mapping for the given scalar type and it is in the cache.</summary>
        /// <remarks>
        /// Note that this method does not perform any resolution. It is only used for cache maintenance.
        /// </remarks>
        public bool HasDeviceScalarType(Schema.ScalarType AScalarType)
        {
			return FDeviceScalarTypes.Contains(AScalarType);
        }
        
		/// <summary>Adds the given scalar type map to the cache.</summary>
        public void AddDeviceScalarType(Schema.DeviceScalarType ADeviceScalarType)
        {
			FDeviceScalarTypes.Add(ADeviceScalarType);
        }
        
        /// <summary>Removes the given scalar type map from the cache.</summary>
        public void RemoveDeviceScalarType(Schema.DeviceScalarType ADeviceScalarType)
        {
			FDeviceScalarTypes.Remove(ADeviceScalarType.ScalarType);
        }
        
        // RequiresAuthentication
        private bool FRequiresAuthentication;
        /// <summary>Indicates whether the device will attempt to resolve a device user when establishing a connection.</summary>
        /// <remarks>
        /// For the internal devices such as the catalog, temp, and A/T devices, the DAE manages access security, and no
        /// user device mapping is required.
        /// </remarks>
        public bool RequiresAuthentication
        {
			get { return FRequiresAuthentication; }
			set { FRequiresAuthentication = false; }
		}
        
        // DeviceUsers
        private DeviceUsers FUsers = new DeviceUsers();
        public DeviceUsers Users { get { return FUsers; } }
        
        // UserID
		private string FUserID = String.Empty;
		public string UserID { get { return FUserID; } set { FUserID = value == null ? String.Empty : value; } }
		
		// Password
		private string FPassword = String.Empty;
		public string Password { get { return FPassword; } set { FPassword = value == null ? String.Empty : SecurityUtility.EncryptPassword(value); } }
		
		// EncryptedPassword
		public string EncryptedPassword { get { return FPassword; } set { FPassword = value == null ? String.Empty : value; } }

		// ConnectionParameters
		private string FConnectionParameters = String.Empty;
		public string ConnectionParameters { get { return FConnectionParameters; } set { FConnectionParameters = value == null ? String.Empty : value; } } 

		public DeviceSessionInfo GetDeviceSessionInfo(ServerProcess AProcess, User AUser, SessionInfo ASessionInfo)
		{
			DeviceUser LUser = null;
			if (FRequiresAuthentication)
				LUser = AProcess.CatalogDeviceSession.ResolveDeviceUser(this, AUser, false);
			DeviceSessionInfo LDeviceSessionInfo = null;
			if (LUser != null)
				LDeviceSessionInfo = new DeviceSessionInfo(LUser.DeviceUserID, SecurityUtility.DecryptPassword(LUser.DevicePassword), LUser.ConnectionParameters);
			else
			{
				if (UserID != String.Empty)
					LDeviceSessionInfo = new DeviceSessionInfo(UserID, SecurityUtility.DecryptPassword(Password), ConnectionParameters);
			}

			if (LDeviceSessionInfo == null)
				LDeviceSessionInfo = new DeviceSessionInfo(ASessionInfo.UserID, ASessionInfo.Password);
			return LDeviceSessionInfo;
		}
		
        // ReconcileMode
        private ReconcileMode FReconcileMode;
        public ReconcileMode ReconcileMode
        {
			get { return FReconcileMode; }
			set { FReconcileMode = value; }
        }

        // ReconcileMaster
        private ReconcileMaster FReconcileMaster;
        public ReconcileMaster ReconcileMaster
        {
			get { return FReconcileMaster; }
			set { FReconcileMaster = value; }
        }
        
		// verify that all types in the given table are mapped into this device
        public virtual void CheckSupported(Plan APlan, TableVar ATableVar)
        {
        }
        
        // Capabilities
		public virtual DeviceCapability Capabilities
		{
			get { return 0; }
        }
        
        // Supports
        public bool Supports(DeviceCapability ACapability)
        {
			return (Capabilities & ACapability) != 0;
        }
        
        protected void CheckCapability(DeviceCapability ACapability)
        {
			if (!Supports(ACapability))
				throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, Enum.GetName(typeof(DeviceCapability), ACapability), Name);
        }
        
        // SupportsTransactions
        protected bool FSupportsTransactions;
		public bool SupportsTransactions { get { return FSupportsTransactions; } }
		
		// SupportsNestedTransactions
		protected bool FSupportsNestedTransactions;
		public bool SupportsNestedTransactions { get { return FSupportsNestedTransactions; } }

        public override Statement EmitStatement(EmitMode AMode)
        {
			if (AMode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveRegistered();
			}
			else
			{
				RemoveObjectID();
				RemoveRegistered();
			}

			CreateDeviceStatement LStatement = new CreateDeviceStatement();
			LStatement.DeviceName = Schema.Object.EnsureRooted(Name);
			LStatement.ClassDefinition = ClassDefinition == null ? null : (ClassDefinition)ClassDefinition.Clone();
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			LStatement.ReconciliationSettings = new ReconciliationSettings();
			LStatement.ReconciliationSettings.ReconcileMaster = ReconcileMaster;
			LStatement.ReconciliationSettings.ReconcileMode = ReconcileMode;

			return LStatement;
        }
        
		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropDeviceStatement LStatement = new DropDeviceStatement();
			LStatement.ObjectName = Schema.Object.EnsureRooted(Name);
			return LStatement;
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
		
		public virtual ClassDefinition GetDefaultOperatorClassDefinition(MetaData AMetaData)
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
		private string FDeviceName;
		public string DeviceName
		{
			get { return FDeviceName; }
			set { FDeviceName = value; }
		}
		
		private string FSettingName;
		public string SettingName
		{
			get { return FSettingName; }
			set { FSettingName = value; }
		}
		
		private string FSettingValue;
		public string SettingValue
		{
			get { return FSettingValue; }
			set { FSettingValue = value; }
		}
		
		public override string ToString()
		{
			return String.Format("{0}\\{1}={2}", FDeviceName, FSettingName, FSettingValue);
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
		public DeviceSettings GetSettingsForDevice(string ADeviceName)
		{
			DeviceSettings LResult = new DeviceSettings();
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].DeviceName == ADeviceName)
					LResult.Add(this[LIndex]);
			return LResult;
		}
	}
	
	public class DeviceSessionInfo : System.Object
	{
		public DeviceSessionInfo(string AUserName, string APassword) : base()
		{
			UserName = AUserName;
			Password = APassword;
			ConnectionParameters = string.Empty;
		}

		public DeviceSessionInfo(string AUserName, string APassword, string AConnectionParameters) : base()
		{
			UserName = AUserName;
			Password = APassword;
			ConnectionParameters = AConnectionParameters;
		}

		private string FUserName;
		public string UserName 
		{ 
			get { return FUserName; } 
			set { FUserName = value; } 
		}

		private string FPassword;
		public string Password 
		{
			get { return FPassword; } 
			set { FPassword = value; } 
		} 

		private string FConnectionParameters;
		public string ConnectionParameters 
		{ 
			get { return FConnectionParameters; } 
			set { FConnectionParameters = value; } 
		}
	}

	public class DeviceTransaction : Disposable
	{
		public DeviceTransaction(IsolationLevel AIsolationLevel) : base()
		{
			FIsolationLevel = AIsolationLevel;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FOperations != null)
			{
				foreach (Operation LOperation in FOperations)
					LOperation.Dispose();
				FOperations.Clear();
				FOperations = null;
			}

			base.Dispose(ADisposing);
		}		

		private Operations FOperations = new Operations();
		public Operations Operations { get { return FOperations; } }
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel { get { return FIsolationLevel; } }
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
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			Add(new DeviceTransaction(AIsolationLevel));
		}
		
		public void EndTransaction(bool ASuccess)
		{
			// If we successfully committed a nested transaction, append it's log to the current transaction so that a subsequent rollback will undo it's affects as well.
			if ((ASuccess) && (Count > 1))
			{
				#if USETYPEDLIST
				DeviceTransaction LTransaction = (DeviceTransaction)RemoveItemAt(Count - 1);
				#else
				DeviceTransaction LTransaction = RemoveAt(Count - 1);
				#endif
				CurrentTransaction().Operations.AddRange(LTransaction.Operations);
				LTransaction.Operations.Clear();
				LTransaction.Dispose();
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
		protected internal DeviceSession(Device ADevice, ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo) : base()
		{
			FDevice = ADevice;
			FServerProcess = AServerProcess;
			FDeviceSessionInfo = ADeviceSessionInfo;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FTransactions != null)
				{
					EnsureTransactionsRolledback();
					FTransactions = null;
				}
			}
			finally
			{
				FDevice = null;
				FServerProcess = null;
				FDeviceSessionInfo = null;

				base.Dispose(ADisposing);
			}
		}
		
		[Reference]
		private Device FDevice;
		public Device Device { get { return FDevice; } }
		
		// ServerProcess
		[Reference]
		private ServerProcess FServerProcess;
		public ServerProcess ServerProcess { get { return FServerProcess; } } 

		// DeviceSessionInfo
		private DeviceSessionInfo FDeviceSessionInfo;
		public DeviceSessionInfo DeviceSessionInfo { get { return FDeviceSessionInfo; } }
		
		#if REFERENCECOUNTDEVICESESSIONS
		protected internal int FReferenceCount;
		#endif
		
		// Exception Handling
        protected Exception InternalWrapException(Exception AException)
        {
			return AException;
        }
        
        protected virtual bool IsConnectionFailure(Exception AException)
        {
			return false;
        }
        
        protected virtual bool IsTransactionFailure(Exception AException)
        {
			return false;
        }

		// All exceptions from the device layer must come through this point
        public Exception WrapException(Exception AException)
        {
			// IsConnectionFailure
			if (IsConnectionFailure(AException))
			{
				ServerProcess.StopError = true;
				AException = new DeviceException(DeviceException.Codes.ConnectionFailure, ErrorSeverity.Environment, AException, Device.Name);
			}

			// IsTransactionFailure
			if (IsTransactionFailure(AException))
			{
				ServerProcess.StopError = true;
				AException = new DeviceException(DeviceException.Codes.TransactionFailure, ErrorSeverity.Environment, AException, Device.Name);
			}
			
			return InternalWrapException(AException);
        }

		// Transactions
		private DeviceTransactions FTransactions = new DeviceTransactions();
		public DeviceTransactions Transactions { get { return FTransactions; } }
		
        // BeginTransaction
        protected abstract void InternalBeginTransaction(IsolationLevel AIsolationLevel);
        public void BeginTransaction(IsolationLevel AIsolationLevel)
        {
			try
			{
				FTransactions.BeginTransaction(AIsolationLevel);
				InternalBeginTransaction(AIsolationLevel);
			} 
			catch (Exception LException)
			{
				throw WrapException(LException);
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
			catch (Exception LException)
			{
				throw WrapException(LException);
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
				FTransactions.EndTransaction(true);
			} 
			catch (Exception LException)
			{
				throw WrapException(LException);
			}
		}
		
		protected void RemoveTransactionReferences(DeviceTransaction ATransaction, Schema.TableVar ATableVar)
		{
			Operation LOperation;
			for (int LOperationIndex = ATransaction.Operations.Count - 1; LOperationIndex >= 0; LOperationIndex--)
			{
				LOperation = ATransaction.Operations[LOperationIndex];
				if (LOperation.TableVar.Equals(ATableVar))
				{
					ATransaction.Operations.RemoveAt(LOperationIndex);
					LOperation.Dispose();
				}
			}
		}
		
		protected void RemoveTransactionReferences(Schema.TableVar ATableVar)
		{
			foreach (DeviceTransaction LTransaction in FTransactions)
				RemoveTransactionReferences(LTransaction, ATableVar);
		}
		
		// RollbackTransaction
		protected void InternalRollbackTransaction(DeviceTransaction ATransaction)
		{
			Operation LOperation;
			InsertOperation LInsertOperation;
			UpdateOperation LUpdateOperation;
			DeleteOperation LDeleteOperation;
			Exception LException = null;
			for (int LIndex = ATransaction.Operations.Count - 1; LIndex >= 0; LIndex--)
			{
				try
				{
					LOperation = ATransaction.Operations[LIndex];
					try
					{
						LInsertOperation = LOperation as InsertOperation;
						if (LInsertOperation != null)
							InternalInsertRow(LInsertOperation.TableVar, LInsertOperation.Row, LInsertOperation.ValueFlags);

						LUpdateOperation = LOperation as UpdateOperation;
						if (LUpdateOperation != null)
							InternalUpdateRow(LUpdateOperation.TableVar, LUpdateOperation.OldRow, LUpdateOperation.NewRow, LUpdateOperation.ValueFlags);

						LDeleteOperation = LOperation as DeleteOperation;
						if (LDeleteOperation != null)
							InternalDeleteRow(LDeleteOperation.TableVar, LDeleteOperation.Row);
					}
					finally
					{
						ATransaction.Operations.RemoveAt(LIndex);
						LOperation.Dispose();
					}
				}
				catch (Exception E)
				{
					LException = E;
					ServerProcess.ServerSession.Server.LogError(E);
				}
			}
			
			if (LException != null)
				throw LException;
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
						InternalRollbackTransaction(FTransactions.CurrentTransaction());
					}
					finally
					{
						InternalRollbackTransaction();
					}
				}
				finally
				{
					FTransactions.EndTransaction(false);
				}
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}
		}
		
		// InTransaction
		public bool InTransaction { get { return FTransactions.Count > 0; } }

        protected void CheckInTransaction()
        {
            if (!InTransaction)
                throw new SchemaException(SchemaException.Codes.NoActiveTransaction);
        }
        
        // EnsureTransactionsRolledback
        protected void EnsureTransactionsRolledback()
        {
			while (FTransactions.Count > 0)
				RollbackTransaction();
        }
        
        // Execute
        protected virtual object InternalExecute(DevicePlan ADevicePlan)
        {
			throw new DAE.Device.DeviceException(DAE.Device.DeviceException.Codes.InvalidExecuteRequest, Device.Name, ADevicePlan.Node.GetType().Name);
        }
        
        public object Execute(DevicePlan ADevicePlan)
        {
			if (ADevicePlan.Node is DropTableNode)
				RemoveTransactionReferences(((DropTableNode)ADevicePlan.Node).Table);
			try
			{
				return InternalExecute(ADevicePlan);
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}
        }
        
        protected void CheckCapability(DeviceCapability ACapability)
        {
			if (!Device.Supports(ACapability))
				throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, Enum.GetName(typeof(DeviceCapability), ACapability), Device.Name);
        }
        
        // Row level operations are provided as implementation details only, not exposed through the Dataphor language.
        protected virtual void InternalInsertRow(TableVar ATable, Row ARow, BitArray AValueFlags)
        {
			throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, DeviceCapability.RowLevelInsert, Device.Name);
        }

        public void InsertRow(TableVar ATable, Row ARow, BitArray AValueFlags)
        {
			if (ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			CheckCapability(DeviceCapability.RowLevelInsert);

			try
			{
				InternalInsertRow(ATable, ARow, AValueFlags);
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}

			if (!ServerProcess.NonLogged && ((!Device.SupportsTransactions && (Transactions.Count == 1)) || (!Device.SupportsNestedTransactions && (Transactions.Count > 1))) && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new DeleteOperation(ATable, (Row)ARow.Copy()));
        }
        
        protected virtual void InternalUpdateRow(TableVar ATable, Row AOldRow, Row ANewRow, BitArray AValueFlags)
        {
			throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, DeviceCapability.RowLevelUpdate, Device.Name);
        }
        
        public void UpdateRow(TableVar ATable, Row AOldRow, Row ANewRow, BitArray AValueFlags)
        {
			if (ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			CheckCapability(DeviceCapability.RowLevelUpdate);
			
			try
			{
				InternalUpdateRow(ATable, AOldRow, ANewRow, AValueFlags);
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}

			if (!ServerProcess.NonLogged && ((!Device.SupportsTransactions && (Transactions.Count == 1)) || (!Device.SupportsNestedTransactions && (Transactions.Count > 1))) && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new UpdateOperation(ATable, (Row)ANewRow.Copy(), (Row)AOldRow.Copy(), AValueFlags));
        }
        
        protected virtual void InternalDeleteRow(TableVar ATable, Row ARow)
        {
			throw new SchemaException(SchemaException.Codes.CapabilityNotSupported, DeviceCapability.RowLevelDelete, Device.Name);
		}
        
        public void DeleteRow(TableVar ATable, Row ARow)
        {
			if (ServerProcess.NonLogged)
				CheckCapability(DeviceCapability.NonLoggedOperations);
			CheckCapability(DeviceCapability.RowLevelDelete);
			try
			{
				InternalDeleteRow(ATable, ARow);
			}
			catch (Exception LException)
			{
				throw WrapException(LException);
			}

			if (!ServerProcess.NonLogged && ((!Device.SupportsTransactions && (Transactions.Count == 1)) || (!Device.SupportsNestedTransactions && (Transactions.Count > 1))) && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new InsertOperation(ATable, (Row)ARow.Copy(), null));
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
		public DeviceSessions(bool AItemsOwned) : base(AItemsOwned) { }
	#endif

		public int IndexOf(Device ADevice)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].Device == ADevice)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Device ADevice)
		{
			return IndexOf(ADevice) >= 0;
		}
		
		public DeviceSession this[Device ADevice]
		{
			get { return this[IndexOf(ADevice)]; }
		}
		
		private object FSyncRoot = new object();
		public object SyncRoot { get { return FSyncRoot; } }
    }

	public class DeviceUser : System.Object
	{
		public DeviceUser() : base(){}
		public DeviceUser(User AUser, Device ADevice, string ADeviceUserID, string ADevicePassword) : base()
		{
			User = AUser;
			Device = ADevice;
			DeviceUserID = ADeviceUserID;
			DevicePassword = ADevicePassword;
		}
		
		public DeviceUser(User AUser, Device ADevice, string ADeviceUserID, string ADevicePassword, string AConnectionParameters) : base()
		{
			User = AUser;
			Device = ADevice;
			DeviceUserID = ADeviceUserID;
			DevicePassword = ADevicePassword;
			ConnectionParameters = AConnectionParameters;
		}

		public DeviceUser(User AUser, Device ADevice, string ADeviceUserID, string ADevicePassword, bool AEncryptPassword) : base()
		{
			User = AUser;
			Device = ADevice;
			DeviceUserID = ADeviceUserID;
			if (AEncryptPassword)
				DevicePassword = Schema.SecurityUtility.EncryptPassword(ADevicePassword);
			else
				DevicePassword = ADevicePassword;
		}

		public DeviceUser(User AUser, Device ADevice, string ADeviceUserID, string ADevicePassword, bool AEncryptPassword, string AConnectionParameters) : base()
		{
			User = AUser;
			Device = ADevice;
			DeviceUserID = ADeviceUserID;
			if (AEncryptPassword)
				DevicePassword = Schema.SecurityUtility.EncryptPassword(ADevicePassword);
			else
				DevicePassword = ADevicePassword;
			ConnectionParameters = AConnectionParameters;
		}
		
		[Reference]
		private User FUser;
		public User User { get { return FUser; } set { FUser = value; } }
		
		[Reference]
		private Device FDevice;
		public Device Device { get { return FDevice; } set { FDevice = value; } }
		
		private string FDeviceUserID = String.Empty;
		public string DeviceUserID { get { return FDeviceUserID; } set { FDeviceUserID = value == null ? String.Empty : value; } }
		
		private string FDevicePassword = String.Empty;
		public string DevicePassword { get { return FDevicePassword; } set { FDevicePassword = value == null ? String.Empty : value; } }

		private string FConnectionParameters = String.Empty;
		public string ConnectionParameters { get { return FConnectionParameters; } set { FConnectionParameters = value == null ? String.Empty : value; } } 
	}
	
	public class DeviceUsers : Hashtable
	{		
		public DeviceUsers() : base(StringComparer.OrdinalIgnoreCase){}
		
		public DeviceUser this[string AID] { get { return (DeviceUser)base[AID]; } }
		
		public void Add(DeviceUser ADeviceUser)
		{
			Add(ADeviceUser.User.ID, ADeviceUser);
		}
	}

    public class TranslationMessage
    {
		public TranslationMessage(string AMessage) : base()
		{
			FMessage = AMessage;
		}
		
		public TranslationMessage(string AMessage, PlanNode AContext) : base()
		{
			FMessage = AMessage;
			if (AContext.Line == -1)
				FContext = AContext.SafeEmitStatementAsString();
			else
			{
			    FLine = AContext.Line;
			    FLinePos = AContext.LinePos;
			}
		}
		
		private string FMessage;
		public string Message { get { return FMessage; } }
		
		private int FLine = -1;
		public int Line
		{
			get { return FLine; }
			set { FLine = value; }
		}
		
		private int FLinePos = -1;
		public int LinePos
		{
			get { return FLinePos; }
			set { FLinePos = value; }
		}
		
		private string FContext = null;
		public string Context
		{
			get { return FContext; }
			set { FContext = value; }
		}
		
		public override string ToString()
		{
			if (FLinePos == -1)
				return String.Format("{0}\r\n\t{1}", FMessage, FContext);

			if (FContext == null)
				return FMessage;

			return String.Format("{0} ({1}, {2})", FMessage, FLine.ToString(), FLinePos.ToString());
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
			StringBuilder LBuilder = new StringBuilder();
			foreach (TranslationMessage LMessage in this)
				ExceptionUtility.AppendMessage(LBuilder, 1, LMessage.ToString());
			return LBuilder.ToString();
		}
	}
}