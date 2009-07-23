/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using System.Text;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;

	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public abstract class DDLNode : PlanNode 
	{
		protected void DropDeviceMaps(ServerProcess AProcess, Schema.Device ADevice)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(ADevice.ID, false);
			StringCollection LDependents = new StringCollection();
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if ((LHeaders[LIndex].ObjectType == "DeviceScalarType") || (LHeaders[LIndex].ObjectType == "DeviceOperator"))
					LDependents.Add(LHeaders[LIndex].Name);
			}
			
			if (LDependents.Count > 0)
			{
				string[] LDependentNames = new string[LDependents.Count];
				LDependents.CopyTo(LDependentNames, 0);
				Compiler.BindNode(AProcess.Plan, Compiler.Compile(AProcess.Plan, AProcess.Plan.Catalog.EmitDropStatement(AProcess, LDependentNames, String.Empty, false, true, false, true))).Execute(AProcess);
			}
		}
		
		protected void DropGeneratedObjects(ServerProcess AProcess, Schema.Object AObject)
		{
			List<Schema.CatalogObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectGeneratedObjects(AObject.ID);
			
			if (LHeaders.Count > 0)
			{
				string[] LObjectNames = new string[LHeaders.Count];
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					LObjectNames[LIndex] = LHeaders[LIndex].Name;
					
				Compiler.Compile(AProcess.Plan, AProcess.Plan.Catalog.EmitDropStatement(AProcess, LObjectNames, String.Empty, false, true, false, true)).Execute(AProcess);
			}
		}
		
		protected void DropGeneratedDependents(ServerProcess AProcess, Schema.Object AObject)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
			StringCollection LDeviceMaps = new StringCollection(); // Device maps need to be dropped first, because the dependency of a device map on a generated operator will be reported as a dependency on the generator, not the operator
			StringCollection LDependents = new StringCollection();
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if (LHeaders[LIndex].IsATObject || LHeaders[LIndex].IsSessionObject || LHeaders[LIndex].IsGenerated)
				{
					if (LHeaders[LIndex].ResolveObject(AProcess) is Schema.DeviceObject)
						LDeviceMaps.Add(LHeaders[LIndex].Name);
					else
						LDependents.Add(LHeaders[LIndex].Name);
				}
			}
			
			if (LDeviceMaps.Count > 0)
			{
				string[] LDeviceMapNames = new string[LDeviceMaps.Count];
				LDeviceMaps.CopyTo(LDeviceMapNames, 0);
				PlanNode LNode = Compiler.Compile(AProcess.Plan, AProcess.Plan.Catalog.EmitDropStatement(AProcess, LDeviceMapNames, String.Empty, false, true, true, true));
				LNode.Execute(AProcess);
			}
						
			if (LDependents.Count > 0)
			{
				string[] LDependentNames = new string[LDependents.Count];
				LDependents.CopyTo(LDependentNames, 0);
				PlanNode LNode = Compiler.Compile(AProcess.Plan, AProcess.Plan.Catalog.EmitDropStatement(AProcess, LDependentNames, String.Empty, false, true, true, true));
				LNode.Execute(AProcess);
			}
		}
		
		protected void DropGeneratedSorts(ServerProcess AProcess, Schema.Object AObject)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
			StringCollection LDependents = new StringCollection();
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if ((LHeaders[LIndex].IsATObject || LHeaders[LIndex].IsSessionObject || LHeaders[LIndex].IsGenerated) && (LHeaders[LIndex].ObjectType == "Sort"))
					LDependents.Add(LHeaders[LIndex].Name);
			}
						
			if (LDependents.Count > 0)
			{
				string[] LDependentNames = new string[LDependents.Count];
				LDependents.CopyTo(LDependentNames, 0);
				Compiler.Compile(AProcess.Plan, AProcess.Plan.Catalog.EmitDropStatement(AProcess, LDependentNames, String.Empty, false, true, false, true)).Execute(AProcess);
			}
		}
		
		protected void CheckNoDependents(ServerProcess AProcess, Schema.Object AObject)
		{
			if (!AProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
				StringCollection LDependents = new StringCollection();
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
				{
					LDependents.Add(LHeaders[LIndex].Description);
					if (LDependents.Count >= 5)
						break;
				}

				if (LDependents.Count > 0)
					throw new CompilerException(CompilerException.Codes.ObjectHasDependents, AObject.Name, ExceptionUtility.StringsToCommaList(LDependents));
			}
		}
		
		protected void CheckNoBaseTableVarDependents(ServerProcess AProcess, Schema.Object AObject)
		{
			if (!AProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
				StringCollection LBaseTableVarDependents = new StringCollection();
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
				{
					Schema.DependentObjectHeader LHeader = LHeaders[LIndex];
					if (LHeader.ObjectType == "BaseTableVar")
					{
						LBaseTableVarDependents.Add(LHeader.Description);
						if (LBaseTableVarDependents.Count >= 5)
							break;
					}
				}

				if (LBaseTableVarDependents.Count > 0)
					throw new CompilerException(CompilerException.Codes.ObjectHasDependents, AObject.Name, ExceptionUtility.StringsToCommaList(LBaseTableVarDependents));
			}
		}

		protected void CheckNoOperatorDependents(ServerProcess AProcess, Schema.Object AObject)
		{
			if (!AProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
				StringCollection LOperatorDependents = new StringCollection();
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
				{
					Schema.DependentObjectHeader LHeader = LHeaders[LIndex];
					if ((LHeader.ObjectType == "Operator") || (LHeader.ObjectType == "AggregateOperator"))
					{
						LOperatorDependents.Add(LHeader.Description);
						if (LOperatorDependents.Count >= 5)
							break;
					}
				}

				if (LOperatorDependents.Count > 0)
					throw new CompilerException(CompilerException.Codes.ObjectHasDependents, AObject.Name, ExceptionUtility.StringsToCommaList(LOperatorDependents));
			}
		}
	}
	
	public abstract class CreateObjectNode : DDLNode {}
	
	public abstract class CreateTableVarBaseNode : CreateObjectNode
	{
		public abstract Schema.TableVar TableVar { get; }

		public Schema.TableVar GetTableVar()
		{
			return TableVar;
		}
	}

	public abstract class CreateTableVarNode : CreateTableVarBaseNode { }
	
    public class CreateTableNode : CreateTableVarNode
    {
		public CreateTableNode() : base(){}
		public CreateTableNode(Schema.BaseTableVar ATable) : base()
		{
			FTable = ATable;
			FDevice = FTable.Device;
			FDeviceSupported = false;
		}
		
		// Table
		protected Schema.BaseTableVar FTable;
		public Schema.BaseTableVar Table
		{
			get { return FTable; }
			set { FTable = value; }
		}
		
		public override Schema.TableVar TableVar { get { return FTable; } }
		
		public override void DetermineDevice(Plan APlan)
		{
			FDevice = FTable.Device;
			FDeviceSupported = false;
		}
		
		public override void BindToProcess(Plan APlan)
		{
			if (FDevice != null)
				APlan.CheckRight(FDevice.GetRight(Schema.RightNames.CreateStore));
			APlan.CheckRight(Schema.RightNames.CreateTable);
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.CreateTable(FTable);
			return null;
		}
    }
    
    public class CreateViewNode : CreateTableVarNode
    {
		// View
		protected Schema.DerivedTableVar FView;
		public Schema.DerivedTableVar View
		{
			get { return FView; }
			set { FView = value; }
		}
		
		public override Schema.TableVar TableVar { get { return FView; } }
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateView);
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (FView.InvocationExpression == null)
				Error.Fail("Derived table variable invocation expression reference is null");
				
			AProcess.CatalogDeviceSession.CreateView(FView);
			return null;
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter AWriter)
		{
			base.WritePlanAttributes(AWriter);
			#if USEORIGINALEXPRESSION
			AWriter.WriteAttributeString("Expression", new D4TextEmitter().Emit(FView.OriginalExpression));
			#else
			AWriter.WriteAttributeString("Expression", new D4TextEmitter().Emit(FView.InvocationExpression));
			#endif
		}
    }
    
    /*
		The following create scalar type statement
		
			create type System.String
			{
				representation System.String
				{
					AsString : System.String
				}
			};
			
		Will create the following operators to manipulate values of this type:
		
			operator System.String(AAsString: System.String): System.String; // default selector
			operator System.String.GetAsString(AValue: System.String): System.String; // AsString get accessor
			operator System.String.SetAsString(AValue: System.String; AAsString: System.String): System.String; // AsString set accessor
    */
    
    public class CreateConversionNode : DDLNode
    {
		public CreateConversionNode(Schema.Conversion AConversion) : base()
		{
			Conversion = AConversion;
		}
		
		public Schema.Conversion Conversion;
		
		public override	object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.CreateConversion(Conversion);
			return null;
		}
    }
    
    public class DropConversionNode : DropObjectNode
    {
		public Schema.ScalarType SourceScalarType;
		public Schema.ScalarType TargetScalarType;
		
		public override	object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				foreach (Schema.Conversion LConversion in SourceScalarType.ImplicitConversions)
					if (LConversion.TargetScalarType.Equals(TargetScalarType))
					{
						CheckNotSystem(AProcess, LConversion);
						DropGeneratedSorts(AProcess, LConversion);
						CheckNoDependents(AProcess, LConversion);
						
						AProcess.CatalogDeviceSession.DropConversion(LConversion);
						break;
					}
				
			}
			return null;
		}
    }
    
    public class CreateSortNode : DDLNode
    {
		private Schema.ScalarType FScalarType;
		public Schema.ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		private Schema.Sort FSort;
		public Schema.Sort Sort
		{
			get { return FSort; }
			set { FSort = value; }
		}
		
		private bool FIsUnique;
		public bool IsUnique
		{
			get { return FIsUnique; }
			set { FIsUnique = value; }
		}
		
		public override	object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				AProcess.CatalogDeviceSession.CreateSort(FSort);
				AProcess.CatalogDeviceSession.AttachSort(FScalarType, FSort, FIsUnique);
				AProcess.CatalogDeviceSession.UpdateCatalogObject(FScalarType);
				return null;
			}
		}
    }
    
    public class AlterSortNode : DDLNode
    {
		private Schema.ScalarType FScalarType;
		public Schema.ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		private Schema.Sort FSort;
		public Schema.Sort Sort
		{
			get { return FSort; }
			set { FSort = value; }
		}

		public override	object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				if (FScalarType.Sort != null)
				{
					Schema.Sort LSort = FScalarType.Sort;
					AProcess.CatalogDeviceSession.DetachSort(FScalarType, LSort, false);
					AProcess.CatalogDeviceSession.DropSort(LSort);
				}
				AProcess.CatalogDeviceSession.CreateSort(FSort);
				AProcess.CatalogDeviceSession.AttachSort(FScalarType, FSort, false);
				AProcess.CatalogDeviceSession.UpdateCatalogObject(FScalarType);
				return null;
			}
		}
    }
    
    public class DropSortNode : DropObjectNode
    {
		private Schema.ScalarType FScalarType;
		public Schema.ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		private bool FIsUnique;
		public bool IsUnique
		{
			get { return FIsUnique; }
			set { FIsUnique = value; }
		}
		
		public override	object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				Schema.Sort LSort = FIsUnique ? FScalarType.UniqueSort : FScalarType.Sort;
				if (LSort != null)
				{
					CheckNotSystem(AProcess, LSort);
					CheckNoDependents(AProcess, LSort);
					
					AProcess.CatalogDeviceSession.DetachSort(FScalarType, LSort, FIsUnique);
					AProcess.CatalogDeviceSession.DropSort(LSort);
				}
				AProcess.CatalogDeviceSession.UpdateCatalogObject(FScalarType);
				return null;
			}
		}
    }
    
    public class CreateRoleNode : DDLNode
    {
		private Schema.Role FRole;
		public Schema.Role Role
		{
			get { return FRole; }
			set { FRole = value; }
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.InsertRole(FRole);
			return null;
		}
    }
    
    public class AlterRoleNode : AlterNode
    {
		private Schema.Role FRole;
		public Schema.Role Role
		{
			get { return FRole; }
			set { FRole = value; }
		}
		
		private AlterRoleStatement FStatement;
		public AlterRoleStatement Statement
		{
			get { return FStatement; }
			set { FStatement = value; }
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.AlterMetaData(FRole, FStatement.AlterMetaData);
			AProcess.CatalogDeviceSession.UpdateCatalogObject(FRole);
			return null;
		}
    }
    
    public class DropRoleNode : DropObjectNode
    {
		private Schema.Role FRole;
		public Schema.Role Role
		{
			get { return FRole; }
			set { FRole = value; }
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.DeleteRole(FRole);
			return null;
		}
    }
    
    public class CreateRightNode : DDLNode
    {
		private string FRightName;
		public string RightName
		{
			get { return FRightName; }
			set { FRightName = value; }
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			SystemCreateRightNode.CreateRight(AProcess, FRightName, AProcess.Plan.User.ID);
			return null;
		}
    }
    
    public class DropRightNode : DDLNode
    {
		private string FRightName;
		public string RightName
		{
			get { return FRightName; }
			set { FRightName = value; }
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			SystemDropRightNode.DropRight(AProcess, FRightName);
			return null;
		}
    }
    
    public class CreateScalarTypeNode : CreateObjectNode
    {
		// ScalarType
		protected Schema.ScalarType FScalarType;
		public Schema.ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateType);
			
			if ((FScalarType.ClassDefinition != null) && !FScalarType.IsDefaultConveyor)
				APlan.CheckRight(Schema.RightNames.HostImplementation);

			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.CreateScalarType(FScalarType);
			return null;
		}
    }
    
    public class CreateOperatorNode : CreateObjectNode
    {
		public CreateOperatorNode() : base(){}
		public CreateOperatorNode(Schema.Operator AOperator) : base()
		{
			FCreateOperator = AOperator;
		}
		
		// Operator
		protected Schema.Operator FCreateOperator;
		public Schema.Operator CreateOperator
		{
			get { return FCreateOperator; }
			set { FCreateOperator = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateOperator);
			
			// Only check host implementation rights for non-generated operators
			if (!FCreateOperator.IsGenerated)
			{
				Schema.AggregateOperator LAggregateOperator = FCreateOperator as Schema.AggregateOperator;
				if (LAggregateOperator != null)
				{
					if (LAggregateOperator.Initialization.ClassDefinition != null)
						APlan.CheckRight(Schema.RightNames.HostImplementation);

					if (LAggregateOperator.Aggregation.ClassDefinition != null)
						APlan.CheckRight(Schema.RightNames.HostImplementation);

					if (LAggregateOperator.Finalization.ClassDefinition != null)
						APlan.CheckRight(Schema.RightNames.HostImplementation);
				}
				else
				{
					if (FCreateOperator.Block.ClassDefinition != null)
						APlan.CheckRight(Schema.RightNames.HostImplementation);
				}
			}

			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.CreateOperator(FCreateOperator);
			return null;
		}
    }
    
    public class CreateConstraintNode : CreateObjectNode
    {		
		protected Schema.CatalogConstraint FConstraint;
		public Schema.CatalogConstraint Constraint
		{
			get { return FConstraint; }
			set { FConstraint = value; }
		}
		
		// For each retrieve in the constraint,
		//		add the constraint to the list of catalog constraints to be checked
		//		when the table being retrieved is affected.
		public static void AttachConstraint(Schema.CatalogConstraint AConstraint, PlanNode ANode)
		{
			BaseTableVarNode LBaseTableVarNode = ANode as BaseTableVarNode;
			if ((LBaseTableVarNode != null) && !LBaseTableVarNode.TableVar.CatalogConstraints.ContainsName(AConstraint.Name))
				LBaseTableVarNode.TableVar.CatalogConstraints.Add(AConstraint);
			else
			{
				foreach (PlanNode LNode in ANode.Nodes)
					AttachConstraint(AConstraint, LNode);
			}
		}
		
		public static void DetachConstraint(Schema.CatalogConstraint AConstraint, PlanNode ANode)
		{
			BaseTableVarNode LBaseTableVarNode = ANode as BaseTableVarNode;
			if (LBaseTableVarNode != null)
				LBaseTableVarNode.TableVar.CatalogConstraints.SafeRemove(AConstraint);
			else
			{
				foreach (PlanNode LNode in ANode.Nodes)
					DetachConstraint(AConstraint, LNode);
			}
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateConstraint);
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			if (FConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
			{
				object LObject;

				try
				{
					LObject = FConstraint.Node.Execute(AProcess);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, FConstraint.Name);
				}
				
				if ((LObject != null) && !(bool)LObject)
					throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, FConstraint.Name);
			}
			
			AProcess.CatalogDeviceSession.CreateConstraint(FConstraint);
			return null;
		}
    }
    
    public class CreateReferenceNode : CreateObjectNode
    {		
		public CreateReferenceNode() : base(){}
		public CreateReferenceNode(Schema.Reference AReference) : base()
		{
			FReference = AReference;
		}
		
		protected Schema.Reference FReference;
		public Schema.Reference Reference
		{
			get { return FReference; }
			set { FReference = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateReference);
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			// Validate the catalog level enforcement constraint
			if (!AProcess.ServerSession.Server.IsRepository && FReference.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
			{
				object LObject;

				try
				{
					LObject = FReference.CatalogConstraint.Node.Execute(AProcess);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, FReference.Name);
				}
				
				if ((LObject != null) && !(bool)LObject)
					throw new RuntimeException(RuntimeException.Codes.ReferenceConstraintViolation, FReference.Name);
			}
			
			AProcess.CatalogDeviceSession.CreateReference(FReference);
			AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
			AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
			
			return null;
		}
    }

    public class CreateServerNode : CreateObjectNode
    {
		protected Schema.ServerLink FServerLink;
		public Schema.ServerLink ServerLink
		{
			get { return FServerLink; }
			set { FServerLink = value; }
		}

		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateServer);
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			FServerLink.ApplyMetaData();
			AProcess.CatalogDeviceSession.InsertCatalogObject(FServerLink);
			return null;
		}
    }
    
    public class CreateDeviceNode : CreateObjectNode
    {
		private Schema.Device FNewDevice;
		public Schema.Device NewDevice
		{
			get { return FNewDevice; }
			set { FNewDevice = value; }
		}

		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(Schema.RightNames.CreateDevice);
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.CatalogDeviceSession.CreateDevice(FNewDevice);
			return null;
		}
    }
    
    public class CreateEventHandlerNode : CreateObjectNode
    {
		private Schema.EventHandler FEventHandler;
		public Schema.EventHandler EventHandler
		{
			get { return FEventHandler; }
			set { FEventHandler = value; }
		}
		
		private Schema.Object FEventSource;
		public Schema.Object EventSource
		{
			get { return FEventSource; }
			set { FEventSource = value; }
		}
		
		private int FEventSourceColumnIndex = -1;
		public int EventSourceColumnIndex
		{
			get { return FEventSourceColumnIndex; }
			set { FEventSourceColumnIndex = value; }
		}
		
		private StringCollection FBeforeOperatorNames;
		public StringCollection BeforeOperatorNames
		{
			get { return FBeforeOperatorNames; }
			set { FBeforeOperatorNames = value; }
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.TableVar LTableVar = FEventSource as Schema.TableVar;
			if ((LTableVar != null) && (!AProcess.InLoadingContext()))
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, LTableVar);
			AProcess.CatalogDeviceSession.CreateEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex, FBeforeOperatorNames);
			return null;
		}
    }
    
    public class AlterEventHandlerNode : DDLNode
    {
		public AlterEventHandlerNode() : base(){}
		public AlterEventHandlerNode(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex) : base()
		{
			FEventHandler = AEventHandler;
			FEventSource = AEventSource;
			FEventSourceColumnIndex = AEventSourceColumnIndex;
		}
		
		private Schema.EventHandler FEventHandler;
		public Schema.EventHandler EventHandler
		{
			get { return FEventHandler; }
			set { FEventHandler = value; }
		}
		
		private Schema.Object FEventSource;
		public Schema.Object EventSource
		{
			get { return FEventSource; }
			set { FEventSource = value; }
		}
		
		private int FEventSourceColumnIndex = -1;
		public int EventSourceColumnIndex
		{
			get { return FEventSourceColumnIndex; }
			set { FEventSourceColumnIndex = value; }
		}
		
		private StringCollection FBeforeOperatorNames;
		public StringCollection BeforeOperatorNames
		{
			get { return FBeforeOperatorNames; }
			set { FBeforeOperatorNames = value; }
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.TableVar LTableVar = FEventSource as Schema.TableVar;
			if ((LTableVar != null) && (!AProcess.InLoadingContext()))
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, LTableVar);
			AProcess.CatalogDeviceSession.AlterEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex, FBeforeOperatorNames);
			AProcess.CatalogDeviceSession.UpdateCatalogObject(FEventHandler);
			return null;
		}
    }
    
    public class DropEventHandlerNode : DDLNode
    {
		public DropEventHandlerNode() : base(){}
		public DropEventHandlerNode(Schema.EventHandler AEventHandler, Schema.Object AEventSource, int AEventSourceColumnIndex) : base()
		{
			FEventHandler = AEventHandler;
			FEventSource = AEventSource;
			FEventSourceColumnIndex = AEventSourceColumnIndex;
		}
		
		private Schema.EventHandler FEventHandler;
		public Schema.EventHandler EventHandler
		{
			get { return FEventHandler; }
			set { FEventHandler = value; }
		}
		
		private Schema.Object FEventSource;
		public Schema.Object EventSource
		{
			get { return FEventSource; }
			set { FEventSource = value; }
		}
		
		private int FEventSourceColumnIndex = -1;
		public int EventSourceColumnIndex
		{
			get { return FEventSourceColumnIndex; }
			set { FEventSourceColumnIndex = value; }
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.TableVar LTableVar = FEventSource as Schema.TableVar;
			if ((LTableVar != null) && (!AProcess.InLoadingContext()))
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, LTableVar);

			if (FEventHandler.IsDeferred)
				AProcess.ServerSession.Server.RemoveDeferredHandlers(FEventHandler);

			AProcess.CatalogDeviceSession.DropEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex);
			return null;
		}
    }
    
    public abstract class AlterNode : DDLNode
    {
		protected virtual Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			return Compiler.ResolveCatalogIdentifier(APlan, AObjectName, true);
		}

		public static void AlterMetaData(Plan APlan, Schema.Object AObject, AlterMetaData AAlterMetaData)
		{
			AlterMetaData(APlan, AObject, AAlterMetaData, false);
		}
		
		public static void AlterMetaData(Plan APlan, Schema.Object AObject, AlterMetaData AAlterMetaData, bool AOptimistic)
		{
			if (AAlterMetaData != null)
			{
				if (AObject.MetaData == null)
					AObject.MetaData = new MetaData();
					
				if (AOptimistic)
				{
					AObject.MetaData.Tags.SafeRemoveRange(AAlterMetaData.DropTags);
					AObject.MetaData.Tags.AddOrUpdateRange(AAlterMetaData.AlterTags);
					AObject.MetaData.Tags.AddOrUpdateRange(AAlterMetaData.CreateTags);
				}
				else
				{
					AObject.MetaData.Tags.RemoveRange(AAlterMetaData.DropTags);
					AObject.MetaData.Tags.UpdateRange(AAlterMetaData.AlterTags);
					AObject.MetaData.Tags.AddRange(AAlterMetaData.CreateTags);
				}
			}
		}
		
		public static void AlterClassDefinition(ServerProcess AProcess, ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition)
		{
			AlterClassDefinition(AProcess, AClassDefinition, AAlterClassDefinition, null);
		}
		
		public static void AlterClassDefinition(ServerProcess AProcess, ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition, object AInstance)
		{
			if (AAlterClassDefinition != null)
			{
				if (AAlterClassDefinition.ClassName != String.Empty)
				{
					if (AInstance != null)
						throw new RuntimeException(RuntimeException.Codes.UnimplementedAlterCommand, String.Format("Class name for objects of type {0}", AInstance.GetType().Name));

					AClassDefinition.ClassName = AAlterClassDefinition.ClassName;
				}
					
				foreach (ClassAttributeDefinition LAttributeDefinition in AAlterClassDefinition.DropAttributes)
					AClassDefinition.Attributes.RemoveAt(AClassDefinition.Attributes.IndexOf(LAttributeDefinition.AttributeName));
					
				foreach (ClassAttributeDefinition LAttributeDefinition in AAlterClassDefinition.AlterAttributes)
				{
					if (AInstance != null)
						Schema.ClassLoader.SetProperty(AInstance, LAttributeDefinition.AttributeName, LAttributeDefinition.AttributeValue);
					
					AClassDefinition.Attributes[LAttributeDefinition.AttributeName].AttributeValue = LAttributeDefinition.AttributeValue;
				}
					
				foreach (ClassAttributeDefinition LAttributeDefinition in AAlterClassDefinition.CreateAttributes)
				{
					if (AInstance != null)
						Schema.ClassLoader.SetProperty(AInstance, LAttributeDefinition.AttributeName, LAttributeDefinition.AttributeValue);

					AClassDefinition.Attributes.Add(LAttributeDefinition);
				}
			}
		}
    }
    
    public abstract class AlterTableVarNode : AlterNode
    {		
		// AlterTableVarStatement
		protected AlterTableVarStatement FAlterTableVarStatement;
		public virtual AlterTableVarStatement AlterTableVarStatement
		{
			get { return FAlterTableVarStatement; }
			set { FAlterTableVarStatement = value; }
		}
		
		private bool FShouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return FShouldAffectDerivationTimeStamp; }
			set { FShouldAffectDerivationTimeStamp = value; }
		}
		
		protected void DropKeys(ServerProcess AProcess, Schema.TableVar ATableVar, DropKeyDefinitions ADropKeys)
		{
			if (ADropKeys.Count > 0)
				CheckNoDependents(AProcess, ATableVar);

			foreach (DropKeyDefinition LKeyDefinition in ADropKeys)
			{
				Schema.Key LOldKey = ATableVar.FindKey(LKeyDefinition);

				if (LOldKey.IsInherited)
					throw new CompilerException(CompilerException.Codes.InheritedObject, LOldKey.Name);
				
				AProcess.CatalogDeviceSession.DropKey(ATableVar, LOldKey);
			}
		}
		
		protected void AlterKeys(ServerProcess AProcess, Schema.TableVar ATableVar, AlterKeyDefinitions AAlterKeys)
		{
			foreach (AlterKeyDefinition LKeyDefinition in AAlterKeys)
			{
				Schema.Key LOldKey = ATableVar.FindKey(LKeyDefinition);
					
				AProcess.CatalogDeviceSession.AlterMetaData(LOldKey, LKeyDefinition.AlterMetaData);
			}
		}

		protected void CreateKeys(ServerProcess AProcess, Schema.TableVar ATableVar, KeyDefinitions ACreateKeys)
		{
			foreach (KeyDefinition LKeyDefinition in ACreateKeys)
			{
				Schema.Key LNewKey = Compiler.CompileKeyDefinition(AProcess.Plan, ATableVar, LKeyDefinition);
				if (!ATableVar.Keys.Contains(LNewKey))
				{
					if (LNewKey.Enforced)
					{
						// Validate that the key can be created
						Compiler.CompileCatalogConstraintForKey(AProcess.Plan, ATableVar, LNewKey).Validate(AProcess);

						LNewKey.Constraint = Compiler.CompileKeyConstraint(AProcess.Plan, ATableVar, LNewKey);
					}
					AProcess.CatalogDeviceSession.CreateKey(ATableVar, LNewKey);
				}
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, LNewKey.Name);
			}
		}
		
		protected void DropOrders(ServerProcess AProcess, Schema.TableVar ATableVar, DropOrderDefinitions ADropOrders)
		{
			if (ADropOrders.Count > 0)
				CheckNoDependents(AProcess, ATableVar);

			foreach (DropOrderDefinition LOrderDefinition in ADropOrders)
			{
				
				Schema.Order LOldOrder = ATableVar.FindOrder(AProcess.Plan, LOrderDefinition);
				if (LOldOrder.IsInherited)
					throw new CompilerException(CompilerException.Codes.InheritedObject, LOldOrder.Name);
					
				AProcess.CatalogDeviceSession.DropOrder(ATableVar, LOldOrder);
			}
		}
		
		protected void AlterOrders(ServerProcess AProcess, Schema.TableVar ATableVar, AlterOrderDefinitions AAlterOrders)
		{
			foreach (AlterOrderDefinition LOrderDefinition in AAlterOrders)
				AProcess.CatalogDeviceSession.AlterMetaData(ATableVar.FindOrder(AProcess.Plan, LOrderDefinition), LOrderDefinition.AlterMetaData);
		}

		protected void CreateOrders(ServerProcess AProcess, Schema.TableVar ATableVar, OrderDefinitions ACreateOrders)
		{
			foreach (OrderDefinition LOrderDefinition in ACreateOrders)
			{
				Schema.Order LNewOrder = Compiler.CompileOrderDefinition(AProcess.Plan, ATableVar, LOrderDefinition, false);
				if (!ATableVar.Orders.Contains(LNewOrder))
					AProcess.CatalogDeviceSession.CreateOrder(ATableVar, LNewOrder);
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, LNewOrder.Name);
			}
		}

		protected void DropConstraints(ServerProcess AProcess, Schema.TableVar ATableVar, DropConstraintDefinitions ADropConstraints)
		{
			foreach (DropConstraintDefinition LConstraintDefinition in ADropConstraints)
			{
				Schema.TableVarConstraint LConstraint = ATableVar.Constraints[LConstraintDefinition.ConstraintName];
					
				if (LConstraintDefinition.IsTransition && (!(LConstraint is Schema.TransitionConstraint)))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsNotTransitionConstraint, LConstraint.Name);
					
				if (!LConstraintDefinition.IsTransition && !(LConstraint is Schema.RowConstraint))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsTransitionConstraint, LConstraint.Name);
					
				AProcess.CatalogDeviceSession.DropTableVarConstraint(ATableVar, LConstraint);
			}
		}
		
		protected void ValidateConstraint(ServerProcess AProcess, Schema.TableVar ATableVar, Schema.TableVarConstraint AConstraint)
		{
			Schema.RowConstraint LRowConstraint = AConstraint as Schema.RowConstraint;
			if (LRowConstraint != null)
			{
				// Ensure that all data in the given table var satisfies the new constraint
				// if exists (table rename new where not (expression)) then raise
				PlanNode LPlanNode = Compiler.EmitTableVarNode(AProcess.Plan, ATableVar);
				LPlanNode = Compiler.EmitRestrictNode(AProcess.Plan, LPlanNode, new UnaryExpression(Instructions.Not, (Expression)LRowConstraint.Node.EmitStatement(EmitMode.ForCopy)));
				LPlanNode = Compiler.EmitUnaryNode(AProcess.Plan, Instructions.Exists, LPlanNode);
				LPlanNode = Compiler.BindNode(AProcess.Plan, Compiler.OptimizeNode(AProcess.Plan, LPlanNode));
				object LObject;

				try
				{
					LObject = LPlanNode.Execute(AProcess);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, AConstraint.Name);
				}
				
				if ((LObject != null) && (bool)LObject)
					throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, AConstraint.Name);
			}
		}
		
		protected void AlterConstraints(ServerProcess AProcess, Schema.TableVar ATableVar, AlterConstraintDefinitions AAlterConstraints)
		{
			foreach (AlterConstraintDefinitionBase LConstraintDefinition in AAlterConstraints)
			{
				Schema.TableVarConstraint LOldConstraint = ATableVar.Constraints[LConstraintDefinition.ConstraintName];
				
				if (LConstraintDefinition is AlterConstraintDefinition)
				{
					if (!(LOldConstraint is Schema.RowConstraint))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsTransitionConstraint, LOldConstraint.Name);
						
					AlterConstraintDefinition LAlterConstraintDefinition = (AlterConstraintDefinition)LConstraintDefinition;

					if (LAlterConstraintDefinition.Expression != null)
					{
						ConstraintDefinition LNewConstraintDefinition = new ConstraintDefinition();
						LNewConstraintDefinition.ConstraintName = LAlterConstraintDefinition.ConstraintName;
						LNewConstraintDefinition.MetaData = LOldConstraint.MetaData.Copy();
						LNewConstraintDefinition.Expression = LAlterConstraintDefinition.Expression;
						Schema.TableVarConstraint LNewConstraint = Compiler.CompileTableVarConstraint(AProcess.Plan, ATableVar, LNewConstraintDefinition);
							
						// Validate LNewConstraint
						if (LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
							ValidateConstraint(AProcess, ATableVar, LNewConstraint);
							
						AProcess.CatalogDeviceSession.DropTableVarConstraint(ATableVar, LOldConstraint);
						AProcess.CatalogDeviceSession.CreateTableVarConstraint(ATableVar, LNewConstraint);
						LOldConstraint = LNewConstraint;
					}
				}
				else
				{
					if (!(LOldConstraint is Schema.TransitionConstraint))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsNotTransitionConstraint, LOldConstraint.Name);
						
					AlterTransitionConstraintDefinition LAlterConstraintDefinition = (AlterTransitionConstraintDefinition)LConstraintDefinition;

					if ((LAlterConstraintDefinition.OnInsert != null) || (LAlterConstraintDefinition.OnUpdate != null) || (LAlterConstraintDefinition.OnDelete != null))
					{
						Schema.TransitionConstraint LOldTransitionConstraint = (Schema.TransitionConstraint)LOldConstraint;
						
						// Compile the new constraint
						TransitionConstraintDefinition LNewConstraintDefinition = new TransitionConstraintDefinition();

						LNewConstraintDefinition.ConstraintName = LAlterConstraintDefinition.ConstraintName;
						LNewConstraintDefinition.MetaData = LOldConstraint.MetaData.Copy();

						if (LAlterConstraintDefinition.OnInsert is AlterTransitionConstraintDefinitionCreateItem)
						{
							if (LOldTransitionConstraint.OnInsertNode != null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.InsertTransitionExists, LOldTransitionConstraint.Name);
							LNewConstraintDefinition.OnInsertExpression = ((AlterTransitionConstraintDefinitionCreateItem)LAlterConstraintDefinition.OnInsert).Expression;
						}
						else if (LAlterConstraintDefinition.OnInsert is AlterTransitionConstraintDefinitionAlterItem)
						{
							if (LOldTransitionConstraint.OnInsertNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoInsertTransition, LOldTransitionConstraint.Name);
							LNewConstraintDefinition.OnInsertExpression = ((AlterTransitionConstraintDefinitionAlterItem)LAlterConstraintDefinition.OnInsert).Expression;
						}
						else if (LAlterConstraintDefinition.OnInsert is AlterTransitionConstraintDefinitionDropItem)
						{
							if (LOldTransitionConstraint.OnInsertNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoInsertTransition, LOldTransitionConstraint.Name);
						}
						
						if (LAlterConstraintDefinition.OnUpdate is AlterTransitionConstraintDefinitionCreateItem)
						{
							if (LOldTransitionConstraint.OnUpdateNode != null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.UpdateTransitionExists, LOldTransitionConstraint.Name);
							LNewConstraintDefinition.OnUpdateExpression = ((AlterTransitionConstraintDefinitionCreateItem)LAlterConstraintDefinition.OnUpdate).Expression;
						}
						else if (LAlterConstraintDefinition.OnUpdate is AlterTransitionConstraintDefinitionAlterItem)
						{
							if (LOldTransitionConstraint.OnUpdateNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoUpdateTransition, LOldTransitionConstraint.Name);
							LNewConstraintDefinition.OnUpdateExpression = ((AlterTransitionConstraintDefinitionAlterItem)LAlterConstraintDefinition.OnUpdate).Expression;
						}
						else if (LAlterConstraintDefinition.OnUpdate is AlterTransitionConstraintDefinitionDropItem)
						{
							if (LOldTransitionConstraint.OnUpdateNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoUpdateTransition, LOldTransitionConstraint.Name);
						}
						
						if (LAlterConstraintDefinition.OnDelete is AlterTransitionConstraintDefinitionCreateItem)
						{
							if (LOldTransitionConstraint.OnDeleteNode != null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.DeleteTransitionExists, LOldTransitionConstraint.Name);
							LNewConstraintDefinition.OnDeleteExpression = ((AlterTransitionConstraintDefinitionCreateItem)LAlterConstraintDefinition.OnDelete).Expression;
						}
						else if (LAlterConstraintDefinition.OnDelete is AlterTransitionConstraintDefinitionAlterItem)
						{
							if (LOldTransitionConstraint.OnDeleteNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoDeleteTransition, LOldTransitionConstraint.Name);
							LNewConstraintDefinition.OnDeleteExpression = ((AlterTransitionConstraintDefinitionAlterItem)LAlterConstraintDefinition.OnDelete).Expression;
						}
						else if (LAlterConstraintDefinition.OnDelete is AlterTransitionConstraintDefinitionDropItem)
						{
							if (LOldTransitionConstraint.OnDeleteNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoDeleteTransition, LOldTransitionConstraint.Name);
						}
						
						Schema.TransitionConstraint LNewConstraint = (Schema.TransitionConstraint)Compiler.CompileTableVarConstraint(AProcess.Plan, ATableVar, LNewConstraintDefinition);
							
						// Validate LNewConstraint
						if ((LNewConstraint.OnInsertNode != null) && LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
							ValidateConstraint(AProcess, ATableVar, LNewConstraint);
							
						AProcess.CatalogDeviceSession.DropTableVarConstraint(ATableVar, LOldTransitionConstraint);
						AProcess.CatalogDeviceSession.CreateTableVarConstraint(ATableVar, LNewConstraint);
						LOldConstraint = LNewConstraint;
					}
				}
				
				AProcess.CatalogDeviceSession.AlterMetaData(LOldConstraint, LConstraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateConstraints(ServerProcess AProcess, Schema.TableVar ATableVar, CreateConstraintDefinitions ACreateConstraints)
		{
			foreach (CreateConstraintDefinition LConstraintDefinition in ACreateConstraints)
			{
				Schema.TableVarConstraint LNewConstraint = Compiler.CompileTableVarConstraint(AProcess.Plan, ATableVar, LConstraintDefinition);
				
				if (((LNewConstraint is Schema.RowConstraint) || (((Schema.TransitionConstraint)LNewConstraint).OnInsertNode != null)) && LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
					ValidateConstraint(AProcess, ATableVar, LNewConstraint);
					
				AProcess.CatalogDeviceSession.CreateTableVarConstraint(ATableVar, LNewConstraint);
			}
		}

		protected void DropReferences(ServerProcess AProcess, Schema.TableVar ATableVar, DropReferenceDefinitions ADropReferences)
		{
			foreach (DropReferenceDefinition LReferenceDefinition in ADropReferences)
			{
				Schema.Reference LReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(AProcess.Plan, LReferenceDefinition.ReferenceName);
				AProcess.Plan.AcquireCatalogLock(LReference, LockMode.Exclusive);
				new DropReferenceNode(LReference).Execute(AProcess);
			}
		}

		protected void AlterReferences(ServerProcess AProcess, Schema.TableVar ATableVar, AlterReferenceDefinitions AAlterReferences)
		{
			foreach (AlterReferenceDefinition LReferenceDefinition in AAlterReferences)
			{
				Schema.Reference LReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(AProcess.Plan, LReferenceDefinition.ReferenceName);
				AProcess.Plan.AcquireCatalogLock(LReference, LockMode.Exclusive);
				AProcess.CatalogDeviceSession.AlterMetaData(LReference, LReferenceDefinition.AlterMetaData);
			}
		}

		protected void CreateReferences(ServerProcess AProcess, Schema.TableVar ATableVar, ReferenceDefinitions ACreateReferences)
		{
			foreach (ReferenceDefinition LReferenceDefinition in ACreateReferences)
			{
				CreateReferenceStatement LStatement = new CreateReferenceStatement();
				LStatement.TableVarName = ATableVar.Name;
				LStatement.ReferenceName = LReferenceDefinition.ReferenceName;
				LStatement.Columns.AddRange(LReferenceDefinition.Columns);
				LStatement.ReferencesDefinition = LReferenceDefinition.ReferencesDefinition;
				LStatement.MetaData = LReferenceDefinition.MetaData;
				Compiler.CompileCreateReferenceStatement(AProcess.Plan, LStatement).Execute(AProcess);
			}
		}
    }
    
    public class AlterTableNode : AlterTableVarNode
    {
		// AlterTableVarStatement
		public override AlterTableVarStatement AlterTableVarStatement
		{
			get { return FAlterTableVarStatement; }
			set
			{
				if (!(value is AlterTableStatement))
					throw new RuntimeException(RuntimeException.Codes.InvalidAlterTableVarStatement);
				FAlterTableVarStatement = value;
			}
		}
		
		// AlterTableStatement
		public AlterTableStatement AlterTableStatement
		{
			get { return (AlterTableStatement)FAlterTableVarStatement; }
			set { FAlterTableVarStatement = value; }
		}
		
		private Schema.BaseTableVar FTableVar;
		public Schema.BaseTableVar TableVar { get { return FTableVar; } }

		protected void DropColumns(ServerProcess AProcess, Schema.BaseTableVar ATable, DropColumnDefinitions ADropColumns)
		{
			if (ADropColumns.Count > 0)
				CheckNoDependents(AProcess, ATable);

			foreach (DropColumnDefinition LColumnDefinition in ADropColumns)
			{
				Schema.TableVarColumn LColumn = ATable.Columns[LColumnDefinition.ColumnName];

				foreach (Schema.Key LKey in ATable.Keys)
					if (LKey.Columns.ContainsName(LColumn.Name))
						throw new CompilerException(CompilerException.Codes.ObjectIsReferenced, LColumn.Name, LKey.Name);
						
				foreach (Schema.Order LOrder in ATable.Orders)
					if (LOrder.Columns.Contains(LColumn.Name))
						throw new CompilerException(CompilerException.Codes.ObjectIsReferenced, LColumn.Name, LOrder.Name);
						
				AProcess.CatalogDeviceSession.DropTableVarColumn(ATable, LColumn);
			}
		}
		
		public static void ReplaceValueNodes(PlanNode ANode, ArrayList AValueNodes, string AColumnName)
		{
			if ((ANode is StackReferenceNode) && (Schema.Object.EnsureUnrooted(((StackReferenceNode)ANode).Identifier) == Keywords.Value))
			{
				((StackReferenceNode)ANode).Identifier = AColumnName;
				AValueNodes.Add(ANode);
			}

			for (int LIndex = 0; LIndex < ANode.Nodes.Count; LIndex++)
				ReplaceValueNodes(ANode.Nodes[LIndex], AValueNodes, AColumnName);
		}
		
		public static void RestoreValueNodes(ArrayList AValueNodes)
		{
			for (int LIndex = 0; LIndex < AValueNodes.Count; LIndex++)
				((StackReferenceNode)AValueNodes[LIndex]).Identifier = Schema.Object.EnsureRooted(Keywords.Value);
		}
		
		protected void ValidateConstraint(ServerProcess AProcess, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, Schema.TableVarColumnConstraint AConstraint)
		{
			// Ensure that all values in the given column of the given base table variable satisfy the new constraint
			ArrayList LValueNodes = new ArrayList();
			ReplaceValueNodes(AConstraint.Node, LValueNodes, AColumn.Name);
			Expression LConstraintExpression = new UnaryExpression(Instructions.Not, (Expression)AConstraint.Node.EmitStatement(EmitMode.ForCopy));
			RestoreValueNodes(LValueNodes);
			PlanNode LPlanNode = 
				Compiler.Bind
				(
					AProcess.Plan, 
					Compiler.Optimize
					(
						AProcess.Plan, 
						Compiler.EmitUnaryNode
						(
							AProcess.Plan, 
							Instructions.Exists, 
							Compiler.EmitRestrictNode
							(
								AProcess.Plan, 
								Compiler.EmitProjectNode
								(
									AProcess.Plan, 
									Compiler.EmitBaseTableVarNode
									(
										AProcess.Plan, 
										ATable
									), 
									new string[]{AColumn.Name}, 
									true
								), 
								LConstraintExpression
							)
						)
					)
				);
				
			object LObject;

			try
			{
				LObject = LPlanNode.Execute(AProcess);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, AConstraint.Name);
			}

			if ((bool)LObject)
				throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, AConstraint.Name);
		}
		
		protected void DropColumnConstraints(ServerProcess AProcess, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, DropConstraintDefinitions ADropConstraints)
		{
			foreach (DropConstraintDefinition LConstraintDefinition in ADropConstraints)
			{
				Schema.Constraint LConstraint = AColumn.Constraints[LConstraintDefinition.ConstraintName];
				
				AProcess.CatalogDeviceSession.DropTableVarColumnConstraint(AColumn, (Schema.TableVarColumnConstraint)LConstraint);
			}
		}
		
		protected void AlterColumnConstraints(ServerProcess AProcess, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, AlterConstraintDefinitions AAlterConstraints)
		{
			foreach (AlterConstraintDefinition LConstraintDefinition in AAlterConstraints)
			{
				Schema.TableVarColumnConstraint LConstraint = AColumn.Constraints[LConstraintDefinition.ConstraintName];
				
				if (LConstraintDefinition.Expression != null)
				{
					Schema.TableVarColumnConstraint LNewConstraint = Compiler.CompileTableVarColumnConstraint(AProcess.Plan, ATable, AColumn, new ConstraintDefinition(LConstraintDefinition.ConstraintName, LConstraintDefinition.Expression, LConstraint.MetaData == null ? null : LConstraint.MetaData.Copy()));
					if (LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
						ValidateConstraint(AProcess, ATable, AColumn, LNewConstraint);
					AProcess.CatalogDeviceSession.DropTableVarColumnConstraint(AColumn, LConstraint);
					AProcess.CatalogDeviceSession.CreateTableVarColumnConstraint(AColumn, LNewConstraint);
					LConstraint = LNewConstraint;
				}
					
				AlterMetaData(AProcess.Plan, LConstraint, LConstraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateColumnConstraints(ServerProcess AProcess, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, ConstraintDefinitions ACreateConstraints)
		{
			foreach (ConstraintDefinition LConstraintDefinition in ACreateConstraints)
			{
				Schema.TableVarColumnConstraint LNewConstraint = Compiler.CompileTableVarColumnConstraint(AProcess.Plan, ATable, AColumn, LConstraintDefinition);
				if (LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
					ValidateConstraint(AProcess, ATable, AColumn, LNewConstraint);
				AProcess.CatalogDeviceSession.CreateTableVarColumnConstraint(AColumn, LNewConstraint);
			}
		}
		
		// TODO: Alter table variable column scalar type
		protected void AlterColumns(ServerProcess AProcess, Schema.BaseTableVar ATable, AlterColumnDefinitions AAlterColumns)
		{
			Schema.TableVarColumn LColumn;
			foreach (AlterColumnDefinition LColumnDefinition in AAlterColumns)
			{
				LColumn = ATable.Columns[LColumnDefinition.ColumnName];
				
				if (LColumnDefinition.TypeSpecifier != null)
					throw new RuntimeException(RuntimeException.Codes.UnimplementedAlterCommand, "Column type");
					
				if ((LColumnDefinition.ChangeNilable) && (LColumn.IsNilable != LColumnDefinition.IsNilable))
				{
					if (LColumn.IsNilable)
					{
						// verify that no data exists in the column that is nil
						PlanNode LNode = 
							Compiler.Bind
							(
								AProcess.Plan, 
								Compiler.Bind
								(
									AProcess.Plan, 
									Compiler.CompileExpression
									(
										AProcess.Plan, 
										new UnaryExpression
										(
											Language.D4.Instructions.Exists, 
											new RestrictExpression
											(
												new IdentifierExpression(ATable.Name), 
												new UnaryExpression("IsNil", new IdentifierExpression(LColumn.Name))
											)
										)
									)
								)
							);
							
						object LResult;
						try
						{
							LResult = LNode.Execute(AProcess);
						}
						catch (Exception E)
						{
							throw new RuntimeException(RuntimeException.Codes.ErrorValidatingColumnConstraint, E, "NotNil", LColumn.Name, ATable.DisplayName);
						}
						
						if ((LResult == null) || (bool)LResult)
							throw new RuntimeException(RuntimeException.Codes.NonNilConstraintViolation, LColumn.Name, ATable.DisplayName);
					}

					AProcess.CatalogDeviceSession.SetTableVarColumnIsNilable(LColumn, LColumnDefinition.IsNilable);
				}
					
				if (LColumnDefinition.Default is DefaultDefinition)
				{
					if (LColumn.Default != null)
						throw new RuntimeException(RuntimeException.Codes.DefaultDefined, LColumn.Name, ATable.DisplayName);
						
					AProcess.CatalogDeviceSession.SetTableVarColumnDefault(LColumn, Compiler.CompileTableVarColumnDefault(AProcess.Plan, ATable, LColumn, (DefaultDefinition)LColumnDefinition.Default));
				}
				else if (LColumnDefinition.Default is AlterDefaultDefinition)
				{
					if (LColumn.Default == null)
						throw new RuntimeException(RuntimeException.Codes.DefaultNotDefined, LColumn.Name, ATable.DisplayName);

					AlterDefaultDefinition LDefaultDefinition = (AlterDefaultDefinition)LColumnDefinition.Default;
					if (LDefaultDefinition.Expression != null)
					{
						Schema.TableVarColumnDefault LNewDefault = Compiler.CompileTableVarColumnDefault(AProcess.Plan, ATable, LColumn, new DefaultDefinition(LDefaultDefinition.Expression, LColumn.Default.MetaData == null ? null : LColumn.Default.MetaData.Copy()));
						AProcess.CatalogDeviceSession.SetTableVarColumnDefault(LColumn, LNewDefault);
					}

					AProcess.CatalogDeviceSession.AlterMetaData(LColumn.Default, LDefaultDefinition.AlterMetaData);
				}
				else if (LColumnDefinition.Default is DropDefaultDefinition)
				{
					if (LColumn.Default == null)
						throw new RuntimeException(RuntimeException.Codes.DefaultNotDefined, LColumn.Name, ATable.DisplayName);
					AProcess.CatalogDeviceSession.SetTableVarColumnDefault(LColumn, null);
				}
				
				DropColumnConstraints(AProcess, ATable, LColumn, LColumnDefinition.DropConstraints);
				AlterColumnConstraints(AProcess, ATable, LColumn, LColumnDefinition.AlterConstraints);
				CreateColumnConstraints(AProcess, ATable, LColumn, LColumnDefinition.CreateConstraints);
				
				AProcess.CatalogDeviceSession.AlterMetaData(LColumn, LColumnDefinition.AlterMetaData);
			}
		}
		
		protected Schema.Objects FNonNilableColumns;
		protected Schema.Objects FDefaultColumns;

		protected void CreateColumns(ServerProcess AProcess, Schema.BaseTableVar ATable, ColumnDefinitions ACreateColumns)
		{
			FNonNilableColumns = new Schema.Objects();
			FDefaultColumns = new Schema.Objects();
			Schema.BaseTableVar LDummy = new Schema.BaseTableVar(ATable.Name);
			LDummy.Library = ATable.Library;
			LDummy.IsGenerated = ATable.IsGenerated;
			AProcess.Plan.PushCreationObject(LDummy);
			try
			{
				if (ACreateColumns.Count > 0)
					CheckNoOperatorDependents(AProcess, ATable);
				
				foreach (ColumnDefinition LColumnDefinition in ACreateColumns)
				{
					Schema.TableVarColumn LTableVarColumn = Compiler.CompileTableVarColumnDefinition(AProcess.Plan, ATable, LColumnDefinition);
					if (LTableVarColumn.Default != null)
						FDefaultColumns.Add(LTableVarColumn);
					if (!LTableVarColumn.IsNilable)
					{
						if (LTableVarColumn.Default == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidAlterTableVarCreateColumnStatement, LTableVarColumn.Name, ATable.DisplayName);
						LTableVarColumn.IsNilable = true;
						FNonNilableColumns.Add(LTableVarColumn);
					}
					
					AProcess.CatalogDeviceSession.CreateTableVarColumn(ATable, LTableVarColumn);
				}
				ATable.DetermineRemotable(AProcess);
			}
			finally
			{
				AProcess.Plan.PopCreationObject();
			}
			if (LDummy.HasDependencies())
				ATable.AddDependencies(LDummy.Dependencies);
		}

		public override void DetermineDevice(Plan APlan)
		{
			FTableVar = (Schema.BaseTableVar)FindObject(APlan, FAlterTableVarStatement.TableVarName);
			FDevice = FTableVar.Device;
			FDeviceSupported = false;
		}
		
		public override void BindToProcess(Plan APlan)
		{
			if (Device != null)
				APlan.CheckRight(Device.GetRight(Schema.RightNames.AlterStore));
			APlan.CheckRight(FTableVar.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}
		
		private void UpdateDefaultColumns(ServerProcess AProcess)
		{
			if (FDefaultColumns.Count != 0)
			{
				// Update the data in all columns that have been added
				// update <table name> set { <column name> := <default expression> [, ...] }
				UpdateStatement LUpdateStatement = new UpdateStatement(new IdentifierExpression(FTableVar.Name));
				foreach (Schema.TableVarColumn LColumn in FDefaultColumns)
					LUpdateStatement.Columns.Add(new UpdateColumnExpression(new IdentifierExpression(LColumn.Name), (Expression)LColumn.Default.Node.EmitStatement(EmitMode.ForCopy)));
					
				Compiler.BindNode(AProcess.Plan, Compiler.CompileUpdateStatement(AProcess.Plan, LUpdateStatement)).Execute(AProcess);
				
				// Set the nilable for each column that is not nil
				// alter table <table name> { alter column <column name> { not nil } };
				AlterTableStatement LAlterStatement = new AlterTableStatement();
				LAlterStatement.TableVarName = FTableVar.Name;
				foreach (Schema.TableVarColumn LColumn in FNonNilableColumns)
				{
					AlterColumnDefinition LAlterColumn = new AlterColumnDefinition();
					LAlterColumn.ColumnName = LColumn.Name;
					LAlterColumn.ChangeNilable = true;
					LAlterColumn.IsNilable = false;
					LAlterStatement.AlterColumns.Add(LAlterColumn);
				}
				
				Compiler.BindNode(AProcess.Plan, Compiler.CompileAlterTableStatement(AProcess.Plan, LAlterStatement)).Execute(AProcess);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			FTableVar = (Schema.BaseTableVar)FindObject(AProcess.Plan, FAlterTableVarStatement.TableVarName);
			if (!AProcess.InLoadingContext())
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, FTableVar);

			DropColumns(AProcess, FTableVar, AlterTableStatement.DropColumns);
			AlterColumns(AProcess, FTableVar, AlterTableStatement.AlterColumns);
			CreateColumns(AProcess, FTableVar, AlterTableStatement.CreateColumns);

			// Change columns in the device			
			AlterTableStatement LStatement = new AlterTableStatement();
			LStatement.TableVarName = AlterTableStatement.TableVarName;
			LStatement.DropColumns.AddRange(AlterTableStatement.DropColumns);
			LStatement.AlterColumns.AddRange(AlterTableStatement.AlterColumns);
			LStatement.CreateColumns.AddRange(AlterTableStatement.CreateColumns);
			AlterTableNode LNode = new AlterTableNode();
			LNode.AlterTableStatement = LStatement;
			LNode.DetermineDevice(AProcess.Plan);
			AProcess.DeviceExecute(FTableVar.Device, LNode);
			UpdateDefaultColumns(AProcess);

			// Drop keys and orders
			LStatement = new AlterTableStatement();
			LStatement.TableVarName = AlterTableStatement.TableVarName;
			LStatement.DropKeys.AddRange(AlterTableStatement.DropKeys);
			LStatement.DropOrders.AddRange(AlterTableStatement.DropOrders);
			LNode = new AlterTableNode();
			LNode.AlterTableStatement = LStatement;
			LNode.DetermineDevice(AProcess.Plan);
			AProcess.DeviceExecute(FTableVar.Device, LNode);
			DropKeys(AProcess, FTableVar, FAlterTableVarStatement.DropKeys);
			DropOrders(AProcess, FTableVar, FAlterTableVarStatement.DropOrders);

			AlterKeys(AProcess, FTableVar, FAlterTableVarStatement.AlterKeys);
			AlterOrders(AProcess, FTableVar, FAlterTableVarStatement.AlterOrders);
			CreateKeys(AProcess, FTableVar, FAlterTableVarStatement.CreateKeys);
			CreateOrders(AProcess, FTableVar, FAlterTableVarStatement.CreateOrders);

			LStatement = new AlterTableStatement();
			LStatement.TableVarName = AlterTableStatement.TableVarName;
			LStatement.CreateKeys.AddRange(AlterTableStatement.CreateKeys);
			LStatement.CreateOrders.AddRange(AlterTableStatement.CreateOrders);
			LNode = new AlterTableNode();
			LNode.AlterTableStatement = LStatement;
			LNode.DetermineDevice(AProcess.Plan);
			AProcess.DeviceExecute(FTableVar.Device, LNode);

			DropReferences(AProcess, FTableVar, FAlterTableVarStatement.DropReferences);
			AlterReferences(AProcess, FTableVar, FAlterTableVarStatement.AlterReferences);
			CreateReferences(AProcess, FTableVar, FAlterTableVarStatement.CreateReferences);
			DropConstraints(AProcess, FTableVar, FAlterTableVarStatement.DropConstraints);
			AlterConstraints(AProcess, FTableVar, FAlterTableVarStatement.AlterConstraints);
			CreateConstraints(AProcess, FTableVar, FAlterTableVarStatement.CreateConstraints);
			AProcess.CatalogDeviceSession.AlterMetaData(FTableVar, FAlterTableVarStatement.AlterMetaData);
			if (ShouldAffectDerivationTimeStamp)
			{
				AProcess.Plan.Catalog.UpdateCacheTimeStamp();
				AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
				AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
			}

			AProcess.CatalogDeviceSession.UpdateCatalogObject(FTableVar);
			
			return null;
		}
    }
    
    public class AlterViewNode : AlterTableVarNode
    {
		// AlterTableVarStatement
		public override AlterTableVarStatement AlterTableVarStatement
		{
			get { return FAlterTableVarStatement; }
			set
			{
				if (!(value is AlterViewStatement))
					throw new RuntimeException(RuntimeException.Codes.InvalidAlterTableVarStatement);
				FAlterTableVarStatement = value;
			}
		}

		// AlterViewStatement
		public AlterViewStatement AlterViewStatement
		{
			get { return (AlterViewStatement)FAlterTableVarStatement; }
			set { FAlterTableVarStatement = value; }
		}

		protected override Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			Schema.Object LObject = base.FindObject(APlan, AObjectName);
			if (!(LObject is Schema.DerivedTableVar))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotView, AObjectName);
			return LObject;
		}

		public override void BindToProcess(Plan APlan)
		{
			Schema.DerivedTableVar LView = (Schema.DerivedTableVar)FindObject(APlan, FAlterTableVarStatement.TableVarName);
			APlan.CheckRight(LView.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.DerivedTableVar LView = (Schema.DerivedTableVar)FindObject(AProcess.Plan, FAlterTableVarStatement.TableVarName);
			if (!AProcess.InLoadingContext())
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, LView);

			DropKeys(AProcess, LView, FAlterTableVarStatement.DropKeys);
			AlterKeys(AProcess, LView, FAlterTableVarStatement.AlterKeys);
			CreateKeys(AProcess, LView, FAlterTableVarStatement.CreateKeys);
			DropOrders(AProcess, LView, FAlterTableVarStatement.DropOrders);
			AlterOrders(AProcess, LView, FAlterTableVarStatement.AlterOrders);
			CreateOrders(AProcess, LView, FAlterTableVarStatement.CreateOrders);
			DropReferences(AProcess, LView, FAlterTableVarStatement.DropReferences);
			AlterReferences(AProcess, LView, FAlterTableVarStatement.AlterReferences);
			CreateReferences(AProcess, LView, FAlterTableVarStatement.CreateReferences);
			DropConstraints(AProcess, LView, FAlterTableVarStatement.DropConstraints);
			AlterConstraints(AProcess, LView, FAlterTableVarStatement.AlterConstraints);
			CreateConstraints(AProcess, LView, FAlterTableVarStatement.CreateConstraints);
			AProcess.CatalogDeviceSession.AlterMetaData(LView, FAlterTableVarStatement.AlterMetaData);

			if (ShouldAffectDerivationTimeStamp)
			{
				AProcess.Plan.Catalog.UpdateCacheTimeStamp();
				AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
				AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
			}
			
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LView);
			
			return null;
		}
    }
    
    public class AlterScalarTypeNode : AlterNode
    {		
		// AlterScalarTypeStatement
		protected AlterScalarTypeStatement FAlterScalarTypeStatement;
		public AlterScalarTypeStatement AlterScalarTypeStatement
		{
			get { return FAlterScalarTypeStatement; }
			set { FAlterScalarTypeStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			Schema.Object LObject = base.FindObject(APlan, AObjectName);
			if (!(LObject is Schema.ScalarType))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotScalarType, AObjectName);
			return LObject;
		}
		
		protected void DropConstraints(ServerProcess AProcess, Schema.ScalarType AScalarType, DropConstraintDefinitions ADropConstraints)
		{
			foreach (DropConstraintDefinition LConstraintDefinition in ADropConstraints)
			{
				Schema.Constraint LConstraint = AScalarType.Constraints[LConstraintDefinition.ConstraintName];
				AProcess.CatalogDeviceSession.DropScalarTypeConstraint(AScalarType, (Schema.ScalarTypeConstraint)LConstraint);
			}
		}
		
		protected void ValidateConstraint(ServerProcess AProcess, Schema.ScalarType AScalarType, Schema.ScalarTypeConstraint AConstraint)
		{
			// Ensure that all base table vars in the catalog with columns defined on this scalar type, or descendents of this scalar type, satisfy the new constraint
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(AScalarType.ID, false);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if (LHeaders[LIndex].ObjectType == "ScalarType")
				{
					// This is a descendent scalar type
					ValidateConstraint(AProcess, (Schema.ScalarType)AProcess.CatalogDeviceSession.ResolveObject(LHeaders[LIndex].ID), AConstraint);
				}
				else if ((LHeaders[LIndex].ObjectType == "BaseTableVar") && !LHeaders[LIndex].IsATObject)
				{
					// This is a tablevar with columns defined on this scalar type
					// Open a cursor on the expression <tablevar> over { <columns defined on this scalar type> }
					Schema.BaseTableVar LBaseTableVar = (Schema.BaseTableVar)AProcess.CatalogDeviceSession.ResolveObject(LHeaders[LIndex].ID);
					foreach (Schema.Column LColumn in LBaseTableVar.DataType.Columns)
						if (LColumn.DataType.Equals(AScalarType))
						{
							// if exists (table over { column } where not (constraint expression))) then raise
							ArrayList LValueNodes = new ArrayList();
							AlterTableNode.ReplaceValueNodes(AConstraint.Node, LValueNodes, LColumn.Name);
							Expression LConstraintExpression = new UnaryExpression(Instructions.Not, (Expression)AConstraint.Node.EmitStatement(EmitMode.ForCopy));
							AlterTableNode.RestoreValueNodes(LValueNodes);
							PlanNode LPlanNode = Compiler.EmitBaseTableVarNode(AProcess.Plan, LBaseTableVar);
							LPlanNode = Compiler.EmitProjectNode(AProcess.Plan, LPlanNode, new string[]{LColumn.Name}, true);
							LPlanNode = Compiler.EmitRestrictNode(AProcess.Plan, LPlanNode, LConstraintExpression);
							LPlanNode = Compiler.EmitUnaryNode(AProcess.Plan, Instructions.Exists, LPlanNode);
							LPlanNode = Compiler.BindNode(AProcess.Plan, LPlanNode);
							object LResult;

							try
							{
								LResult = LPlanNode.Execute(AProcess);
							}
							catch (Exception E)
							{
								throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, AConstraint.Name);
							}
							
							if ((bool)LResult)
								throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, AConstraint.Name);
						}
				}
			}
		}
		
		protected void AlterConstraints(ServerProcess AProcess, Schema.ScalarType AScalarType, AlterConstraintDefinitions AAlterConstraints)
		{
			Schema.ScalarTypeConstraint LConstraint;
			foreach (AlterConstraintDefinition LConstraintDefinition in AAlterConstraints)
			{
				LConstraint = AScalarType.Constraints[LConstraintDefinition.ConstraintName];
				
				if (LConstraintDefinition.Expression != null)
				{
					Schema.ScalarTypeConstraint LNewConstraint = Compiler.CompileScalarTypeConstraint(AProcess.Plan, AScalarType, new ConstraintDefinition(LConstraintDefinition.ConstraintName, LConstraintDefinition.Expression, LConstraint.MetaData == null ? null : LConstraint.MetaData.Copy()));
					if (LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
						ValidateConstraint(AProcess, AScalarType, LNewConstraint);
					
					AProcess.CatalogDeviceSession.DropScalarTypeConstraint(AScalarType, LConstraint);
					AProcess.CatalogDeviceSession.CreateScalarTypeConstraint(AScalarType, LNewConstraint);
					LConstraint = LNewConstraint;
				}
					
				AProcess.CatalogDeviceSession.AlterMetaData(LConstraint, LConstraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateConstraints(ServerProcess AProcess, Schema.ScalarType AScalarType, ConstraintDefinitions ACreateConstraints)
		{
			foreach (ConstraintDefinition LConstraintDefinition in ACreateConstraints)
			{
				Schema.ScalarTypeConstraint LNewConstraint = Compiler.CompileScalarTypeConstraint(AProcess.Plan, AScalarType, LConstraintDefinition);

				if (LNewConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
					ValidateConstraint(AProcess, AScalarType, LNewConstraint);

				AProcess.CatalogDeviceSession.CreateScalarTypeConstraint(AScalarType, LNewConstraint);
			}
		}
		
		protected void DropSpecials(ServerProcess AProcess, Schema.ScalarType AScalarType, DropSpecialDefinitions ADropSpecials)
		{
			foreach (DropSpecialDefinition LSpecialDefinition in ADropSpecials)
			{
				if (AScalarType.IsSpecialOperator != null)
				{
					new DropOperatorNode(AScalarType.IsSpecialOperator).Execute(AProcess);
					AProcess.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, null);
				}
				
				// Dropping a special
				Schema.Special LSpecial = AScalarType.Specials[LSpecialDefinition.Name];

				// drop operator ScalarTypeNameSpecialName()
				new DropOperatorNode(LSpecial.Comparer).Execute(AProcess);
				// drop operator IsSpecialName(ScalarType)
				new DropOperatorNode(LSpecial.Selector).Execute(AProcess);

				AProcess.CatalogDeviceSession.DropSpecial(AScalarType, LSpecial);
			}
		}
		
		protected void AlterSpecials(ServerProcess AProcess, Schema.ScalarType AScalarType, AlterSpecialDefinitions AAlterSpecials)
		{
			Schema.Special LSpecial;
			foreach (AlterSpecialDefinition LSpecialDefinition in AAlterSpecials)
			{
				LSpecial = AScalarType.Specials[LSpecialDefinition.Name];

				if (LSpecialDefinition.Value != null)
				{
					if (AScalarType.IsSpecialOperator != null)
					{
						new DropOperatorNode(AScalarType.IsSpecialOperator).Execute(AProcess);
						AProcess.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, null);
					}
					
					Schema.Special LNewSpecial = new Schema.Special(Schema.Object.GetNextObjectID(), LSpecial.Name);
					LNewSpecial.Library = AScalarType.Library == null ? null : AProcess.Plan.CurrentLibrary;
					AProcess.Plan.PushCreationObject(LNewSpecial);
					try
					{
						LNewSpecial.ValueNode = Compiler.CompileTypedExpression(AProcess.Plan, LSpecialDefinition.Value, AScalarType);

						// Recompilation of the comparer and selector for the special
						Schema.Operator LNewSelector = Compiler.CompileSpecialSelector(AProcess.Plan, AScalarType, LNewSpecial, LSpecialDefinition.Name, LNewSpecial.ValueNode);
						if (LNewSpecial.HasDependencies())
							LNewSelector.AddDependencies(LNewSpecial.Dependencies);
						LNewSelector.DetermineRemotable(AProcess);

						Schema.Operator LNewComparer = Compiler.CompileSpecialComparer(AProcess.Plan, AScalarType, LNewSpecial, LSpecialDefinition.Name, LNewSpecial.ValueNode);
						if (LNewSpecial.HasDependencies())
							LNewComparer.AddDependencies(LNewSpecial.Dependencies);
						LNewComparer.DetermineRemotable(AProcess);

						new DropOperatorNode(LSpecial.Selector).Execute(AProcess);
						new CreateOperatorNode(LNewSelector).Execute(AProcess);
						LNewSpecial.Selector = LNewSelector;

						new DropOperatorNode(LSpecial.Comparer).Execute(AProcess);
						new CreateOperatorNode(LNewComparer).Execute(AProcess);
						LNewSpecial.Comparer = LNewComparer;
						
						AProcess.CatalogDeviceSession.DropSpecial(AScalarType, LSpecial);
						AProcess.CatalogDeviceSession.CreateSpecial(AScalarType, LNewSpecial);
						LSpecial = LNewSpecial;
					}
					finally
					{
						AProcess.Plan.PopCreationObject();
					}
				}

				AProcess.CatalogDeviceSession.AlterMetaData(LSpecial, LSpecialDefinition.AlterMetaData);
			}
		}
		
		protected void EnsureIsSpecialOperator(ServerProcess AProcess, Schema.ScalarType AScalarType)
		{
			if (AScalarType.IsSpecialOperator == null)
			{
				OperatorBindingContext LContext = new OperatorBindingContext(null, "IsSpecial", AProcess.Plan.NameResolutionPath, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(AScalarType)}), true);
				Compiler.ResolveOperator(AProcess.Plan, LContext);
				if (LContext.Operator == null)
				{
					AProcess.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, Compiler.CompileSpecialOperator(AProcess.Plan, AScalarType));
					new CreateOperatorNode(AScalarType.IsSpecialOperator).Execute(AProcess);
				}
			}
		}
		
		protected void CreateSpecials(ServerProcess AProcess, Schema.ScalarType AScalarType, SpecialDefinitions ACreateSpecials)
		{
			foreach (SpecialDefinition LSpecialDefinition in ACreateSpecials)
			{
				// If we are deserializing, then there is no need to recreate the IsSpecial operator
				if ((!AProcess.InLoadingContext()) && (AScalarType.IsSpecialOperator != null))
				{
					new DropOperatorNode(AScalarType.IsSpecialOperator).Execute(AProcess);
					AProcess.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, null);
				}
				
				Schema.Special LSpecial = Compiler.CompileSpecial(AProcess.Plan, AScalarType, LSpecialDefinition); 
				AProcess.CatalogDeviceSession.CreateSpecial(AScalarType, LSpecial);
				if (!AProcess.InLoadingContext())
				{
					new CreateOperatorNode(LSpecial.Selector).Execute(AProcess);
					new CreateOperatorNode(LSpecial.Comparer).Execute(AProcess);
				}
			}
			
			if (!AProcess.InLoadingContext())
				EnsureIsSpecialOperator(AProcess, AScalarType);
		}

		protected void DropRepresentations(ServerProcess AProcess, Schema.ScalarType AScalarType, DropRepresentationDefinitions ADropRepresentations)
		{
			Schema.Representation LRepresentation;
			foreach (DropRepresentationDefinition LRepresentationDefinition in ADropRepresentations)
			{
				LRepresentation = AScalarType.Representations[LRepresentationDefinition.RepresentationName];
				foreach (Schema.Property LProperty in LRepresentation.Properties)
				{
					new DropOperatorNode(LProperty.ReadAccessor).Execute(AProcess);
					new DropOperatorNode(LProperty.WriteAccessor).Execute(AProcess);
				}
				
				new DropOperatorNode(LRepresentation.Selector).Execute(AProcess);
				AProcess.CatalogDeviceSession.DropRepresentation(AScalarType, LRepresentation);
				AScalarType.ResetNativeRepresentationCache();
			}
		}
		
		protected void AlterRepresentations(ServerProcess AProcess, Schema.ScalarType AScalarType, AlterRepresentationDefinitions AAlterRepresentations)
		{
			Schema.Representation LRepresentation;
			foreach (AlterRepresentationDefinition LRepresentationDefinition in AAlterRepresentations)
			{
				LRepresentation = AScalarType.Representations[LRepresentationDefinition.RepresentationName];
				
				DropProperties(AProcess, AScalarType, LRepresentation, LRepresentationDefinition.DropProperties);
				AlterProperties(AProcess, AScalarType, LRepresentation, LRepresentationDefinition.AlterProperties);
				CreateProperties(AProcess, AScalarType, LRepresentation, LRepresentationDefinition.CreateProperties);
				
				if (LRepresentationDefinition.SelectorAccessorBlock != null)
				{
					Error.Fail("Altering the selector for a representation is not implemented");
					/*
					AlterClassDefinition(AProcess, LRepresentation.Selector, LRepresentationDefinition.AlterClassDefinition);
					
					Schema.Operator LOperator = Compiler.CompileRepresentationSelector(AProcess.Plan, AScalarType, LRepresentation);
					Schema.Operator LOldOperator = LRepresentation.Operator;
					new DropOperatorNode(LOldOperator).Execute(AProcess);
					try
					{
						LRepresentation.Operator = LOperator;
						new CreateOperatorNode(LOperator).Execute(AProcess);
					}
					catch
					{
						new CreateOperatorNode(LOldOperator).Execute(AProcess);
						LRepresentation.Operator = LOldOperator;
						throw;
					}
					*/
				}

				AProcess.CatalogDeviceSession.AlterMetaData(LRepresentation, LRepresentationDefinition.AlterMetaData);
				AScalarType.ResetNativeRepresentationCache();
			}
		}
		
		protected void CreateRepresentations(ServerProcess AProcess, Schema.ScalarType AScalarType, RepresentationDefinitions ACreateRepresentations)
		{
			Schema.Objects LOperators = new Schema.Objects();
			foreach (RepresentationDefinition LRepresentationDefinition in ACreateRepresentations)
			{
				// Generated representations may be replaced by explicit representation definitions
				int LRepresentationIndex = AScalarType.Representations.IndexOf(LRepresentationDefinition.RepresentationName);
				if ((LRepresentationIndex >= 0) && AScalarType.Representations[LRepresentationIndex].IsGenerated)
				{
					DropRepresentationDefinitions LDropRepresentationDefinitions = new DropRepresentationDefinitions();
					LDropRepresentationDefinitions.Add(new DropRepresentationDefinition(LRepresentationDefinition.RepresentationName));
					DropRepresentations(AProcess, AScalarType, LDropRepresentationDefinitions);
				}

				Schema.Representation LRepresentation = Compiler.CompileRepresentation(AProcess.Plan, AScalarType, LOperators, LRepresentationDefinition);
				AProcess.CatalogDeviceSession.CreateRepresentation(AScalarType, LRepresentation);
				AProcess.Plan.CheckCompiled(); // Throw the error after the call to the catalog device session so the error occurs after the create is logged.
				AScalarType.ResetNativeRepresentationCache();
			}

			if (!AProcess.InLoadingContext())
				foreach (Schema.Operator LOperator in LOperators)
					new CreateOperatorNode(LOperator).Execute(AProcess);
		}

		protected void DropProperties(ServerProcess AProcess, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, DropPropertyDefinitions ADropProperties)
		{
			foreach (DropPropertyDefinition LPropertyDefinition in ADropProperties)
			{
				Schema.Property LProperty = ARepresentation.Properties[LPropertyDefinition.PropertyName];
				new DropOperatorNode(LProperty.ReadAccessor).Execute(AProcess);
				new DropOperatorNode(LProperty.WriteAccessor).Execute(AProcess);
				AProcess.CatalogDeviceSession.DropProperty(ARepresentation, LProperty);
			}
		}
		
		protected void AlterProperties(ServerProcess AProcess, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, AlterPropertyDefinitions AAlterProperties)
		{
			Schema.Property LProperty;
			foreach (AlterPropertyDefinition LPropertyDefinition in AAlterProperties)
			{
				LProperty = ARepresentation.Properties[LPropertyDefinition.PropertyName];
				
				if (LPropertyDefinition.PropertyType != null)
				{
					Error.Fail("Altering the type of a property is not implemented");
					/*
					LProperty.DetachDependencies();
					AProcess.Plan.PushCreationObject(LProperty);
					try
					{
						LProperty.DataType = (Schema.ScalarType)Compiler.CompileTypeSpecifier(AProcess.Plan, LPropertyDefinition.PropertyType);
						LProperty.AttachDependencies();
					}
					finally
					{
						AProcess.Plan.PopCreationObject();
					}
					*/
				}
					
				if (LPropertyDefinition.ReadAccessorBlock != null)
				{
					Error.Fail("Altering the read accessor for a property is not implemented");
					/*
					AlterClassDefinition(AProcess, LProperty.ReadAccessor, LPropertyDefinition.ReadClassDefinition);
					Schema.Operator LOldOperator = LProperty.ReadOperator;
					Schema.Operator LOperator = Compiler.CompilePropertyReadAccessor(AProcess.Plan, AScalarType, ARepresentation, LProperty);
					new DropOperatorNode(LOldOperator).Execute(AProcess);
					try
					{
						new CreateOperatorNode(LOperator).Execute(AProcess);
						LProperty.ReadOperator = LOperator;
					}
					catch
					{
						new CreateOperatorNode(LOldOperator).Execute(AProcess);
						throw;
					}
					*/
				}

				if (LPropertyDefinition.WriteAccessorBlock != null)
				{
					Error.Fail("Altering the write accessor for a property is not implemented");
					/*
					AlterClassDefinition(AProcess, LProperty.WriteAccessor, LPropertyDefinition.WriteClassDefinition);
					Schema.Operator LOldOperator = LProperty.WriteOperator;
					Schema.Operator LOperator = Compiler.CompilePropertyWriteAccessor(AProcess.Plan, AScalarType, ARepresentation, LProperty);
					new DropOperatorNode(LOldOperator).Execute(AProcess);
					try
					{
						new CreateOperatorNode(LOperator).Execute(AProcess);
						LProperty.WriteOperator = LOperator;
					}
					catch
					{
						new CreateOperatorNode(LOldOperator).Execute(AProcess);
						throw;
					}
					*/
				}

				AProcess.CatalogDeviceSession.AlterMetaData(LProperty, LPropertyDefinition.AlterMetaData);
			}
		}
		
		protected void CreateProperties(ServerProcess AProcess, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, PropertyDefinitions ACreateProperties)
		{
			Schema.Objects LOperators = new Schema.Objects();
			foreach (PropertyDefinition LPropertyDefinition in ACreateProperties)
			{
				Schema.Property LProperty = Compiler.CompileProperty(AProcess.Plan, AScalarType, ARepresentation, LPropertyDefinition);
				AProcess.CatalogDeviceSession.CreateProperty(ARepresentation, LProperty);
				AProcess.Plan.CheckCompiled(); // Throw the error after the CreateProperty call so that the operation is logged in the catalog device
			}
			
			foreach (Schema.Operator LOperator in LOperators)
				new CreateOperatorNode(LOperator).Execute(AProcess);
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.ScalarType LScalarType = (Schema.ScalarType)FindObject(APlan, FAlterScalarTypeStatement.ScalarTypeName);
			APlan.CheckRight(LScalarType.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.ScalarType LScalarType = (Schema.ScalarType)FindObject(AProcess.Plan, FAlterScalarTypeStatement.ScalarTypeName);
			
			// Constraints			
			DropConstraints(AProcess, LScalarType, FAlterScalarTypeStatement.DropConstraints);
			AlterConstraints(AProcess, LScalarType, FAlterScalarTypeStatement.AlterConstraints);
			CreateConstraints(AProcess, LScalarType, FAlterScalarTypeStatement.CreateConstraints);

			// Default
			if (FAlterScalarTypeStatement.Default is DefaultDefinition)
			{
				if (LScalarType.Default != null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultDefined, LScalarType.Name);
					
				AProcess.CatalogDeviceSession.SetScalarTypeDefault(LScalarType, Compiler.CompileScalarTypeDefault(AProcess.Plan, LScalarType, (DefaultDefinition)FAlterScalarTypeStatement.Default));
			}
			else if (FAlterScalarTypeStatement.Default is AlterDefaultDefinition)
			{
				if (LScalarType.Default == null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultNotDefined, LScalarType.Name);

				AlterDefaultDefinition LDefaultDefinition = (AlterDefaultDefinition)FAlterScalarTypeStatement.Default;
				if (LDefaultDefinition.Expression != null)
				{
					Schema.ScalarTypeDefault LNewDefault = Compiler.CompileScalarTypeDefault(AProcess.Plan, LScalarType, new DefaultDefinition(LDefaultDefinition.Expression, LScalarType.Default.MetaData == null ? null : LScalarType.Default.MetaData.Copy()));
					
					AProcess.CatalogDeviceSession.SetScalarTypeDefault(LScalarType, LNewDefault);
				}

				AProcess.CatalogDeviceSession.AlterMetaData(LScalarType.Default, LDefaultDefinition.AlterMetaData);
			}
			else if (FAlterScalarTypeStatement.Default is DropDefaultDefinition)
			{
				if (LScalarType.Default == null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultNotDefined, LScalarType.Name);
					
				AProcess.CatalogDeviceSession.SetScalarTypeDefault(LScalarType, null);
			}
			
			// Specials
			DropSpecials(AProcess, LScalarType, FAlterScalarTypeStatement.DropSpecials);
			AlterSpecials(AProcess, LScalarType, FAlterScalarTypeStatement.AlterSpecials);
			CreateSpecials(AProcess, LScalarType, FAlterScalarTypeStatement.CreateSpecials);
			
			// Representations
			DropRepresentations(AProcess, LScalarType, FAlterScalarTypeStatement.DropRepresentations);
			AlterRepresentations(AProcess, LScalarType, FAlterScalarTypeStatement.AlterRepresentations);
			CreateRepresentations(AProcess, LScalarType, FAlterScalarTypeStatement.CreateRepresentations);
			
			// If this scalar type has no representations defined, but it is based on a single branch inheritance hierarchy leading to a system type, build a default representation
			#if USETYPEINHERITANCE
			Schema.Objects LOperators = new Schema.Objects();
			if ((FAlterScalarTypeStatement.DropRepresentations.Count > 0) && (LScalarType.Representations.Count == 0))
				Compiler.CompileDefaultRepresentation(AProcess.Plan, LScalarType, LOperators);
			foreach (Schema.Operator LOperator in LOperators)
				new CreateOperatorNode(LOperator).Execute(AProcess);
			#endif
			
			// TODO: Alter semantics for types and representations

			AProcess.CatalogDeviceSession.AlterClassDefinition(LScalarType.ClassDefinition, FAlterScalarTypeStatement.AlterClassDefinition);
			AProcess.CatalogDeviceSession.AlterMetaData(LScalarType, FAlterScalarTypeStatement.AlterMetaData);
			
			if 
			(
				(FAlterScalarTypeStatement.AlterClassDefinition != null) || 
				(FAlterScalarTypeStatement.AlterMetaData != null)
			)
				LScalarType.ResetNativeRepresentationCache();
			
			AProcess.Plan.Catalog.UpdateCacheTimeStamp();
			AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
			AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
			
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LScalarType);
			
			return null;
		}
    }
    
    public abstract class AlterOperatorNodeBase : AlterNode
    {
		protected virtual Schema.Operator FindOperator(Plan APlan, OperatorSpecifier AOperatorSpecifier)
		{
			return Compiler.ResolveOperatorSpecifier(APlan, AOperatorSpecifier);
		}
    }
    
    public class AlterOperatorNode : AlterOperatorNodeBase
    {
		// AlterOperatorStatement
		protected AlterOperatorStatement FAlterOperatorStatement;
		public AlterOperatorStatement AlterOperatorStatement
		{
			get { return FAlterOperatorStatement; }
			set { FAlterOperatorStatement = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.Operator LOperator = FindOperator(APlan, FAlterOperatorStatement.OperatorSpecifier);
			APlan.CheckRight(LOperator.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.Operator LOperator = FindOperator(AProcess.Plan, FAlterOperatorStatement.OperatorSpecifier);
			AProcess.ServerSession.Server.ATDevice.ReportOperatorChange(AProcess, LOperator);
				
			if (FAlterOperatorStatement.Block.AlterClassDefinition != null)
			{
				CheckNoDependents(AProcess, LOperator);
				
				AProcess.CatalogDeviceSession.AlterClassDefinition(LOperator.Block.ClassDefinition, FAlterOperatorStatement.Block.AlterClassDefinition);
			}

			if (FAlterOperatorStatement.Block.Block != null)
			{
				CheckNoDependents(AProcess, LOperator);
					
				Schema.Operator LTempOperator = new Schema.Operator(LOperator.Name);
				AProcess.Plan.PushCreationObject(LTempOperator);
				try
				{
					LTempOperator.Block.BlockNode = Compiler.CompileOperatorBlock(AProcess.Plan, LOperator, FAlterOperatorStatement.Block.Block);
					
					AProcess.CatalogDeviceSession.AlterOperator(LOperator, LTempOperator);
				}
				finally
				{
					AProcess.Plan.PopCreationObject();
				}
			}
				
			AProcess.CatalogDeviceSession.AlterMetaData(LOperator, FAlterOperatorStatement.AlterMetaData);
			AProcess.Plan.Catalog.UpdateCacheTimeStamp();
			AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LOperator);
			
			return null;
		}
    }
    
    public class AlterAggregateOperatorNode : AlterOperatorNodeBase
    {		
		// AlterAggregateOperatorStatement
		protected AlterAggregateOperatorStatement FAlterAggregateOperatorStatement;
		public AlterAggregateOperatorStatement AlterAggregateOperatorStatement
		{
			get { return FAlterAggregateOperatorStatement; }
			set { FAlterAggregateOperatorStatement = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.Operator LOperator = FindOperator(APlan, FAlterAggregateOperatorStatement.OperatorSpecifier);
			APlan.CheckRight(LOperator.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.Operator LOperator = FindOperator(AProcess.Plan, FAlterAggregateOperatorStatement.OperatorSpecifier);
			if (!(LOperator is Schema.AggregateOperator))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotAggregateOperator, FAlterAggregateOperatorStatement.OperatorSpecifier.OperatorName);
				
			AProcess.ServerSession.Server.ATDevice.ReportOperatorChange(AProcess, LOperator);

			Schema.AggregateOperator LAggregateOperator = (Schema.AggregateOperator)LOperator;

			if 
			(
				(FAlterAggregateOperatorStatement.Initialization.Block != null) || 
				(FAlterAggregateOperatorStatement.Aggregation.Block != null) || 
				(FAlterAggregateOperatorStatement.Finalization.Block != null) || 
				(FAlterAggregateOperatorStatement.Initialization.AlterClassDefinition != null) || 
				(FAlterAggregateOperatorStatement.Aggregation.AlterClassDefinition != null) || 
				(FAlterAggregateOperatorStatement.Finalization.AlterClassDefinition != null)
			)
			{
				CheckNoDependents(AProcess, LAggregateOperator);
				
				AProcess.Plan.PushCreationObject(LAggregateOperator);
				try
				{
					AProcess.Plan.Symbols.PushWindow(0);
					try
					{
						Symbol LResultVar = new Symbol(Keywords.Result, LAggregateOperator.ReturnDataType);
						AProcess.Plan.Symbols.Push(LResultVar);
						
						if (FAlterAggregateOperatorStatement.Initialization.AlterClassDefinition != null)
							AProcess.CatalogDeviceSession.AlterClassDefinition(LAggregateOperator.Initialization.ClassDefinition, FAlterAggregateOperatorStatement.Initialization.AlterClassDefinition);
							
						if (FAlterAggregateOperatorStatement.Initialization.Block != null)
							AProcess.CatalogDeviceSession.AlterOperatorBlockNode(LAggregateOperator.Initialization, Compiler.CompileStatement(AProcess.Plan, FAlterAggregateOperatorStatement.Initialization.Block));
						
						AProcess.Plan.Symbols.PushFrame();
						try
						{
							foreach (Schema.Operand LOperand in LAggregateOperator.Operands)
								AProcess.Plan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));
								
							if (FAlterAggregateOperatorStatement.Aggregation.AlterClassDefinition != null)
								AProcess.CatalogDeviceSession.AlterClassDefinition(LAggregateOperator.Aggregation.ClassDefinition, FAlterAggregateOperatorStatement.Aggregation.AlterClassDefinition);
								
							if (FAlterAggregateOperatorStatement.Aggregation.Block != null)
								AProcess.CatalogDeviceSession.AlterOperatorBlockNode(LAggregateOperator.Aggregation, Compiler.CompileDeallocateFrameVariablesNode(AProcess.Plan, Compiler.CompileStatement(AProcess.Plan, FAlterAggregateOperatorStatement.Aggregation.Block)));
						}
						finally
						{
							AProcess.Plan.Symbols.PopFrame();
						}
						
						if (FAlterAggregateOperatorStatement.Finalization.AlterClassDefinition != null)
							AProcess.CatalogDeviceSession.AlterClassDefinition(LAggregateOperator.Finalization.ClassDefinition, FAlterAggregateOperatorStatement.Finalization.AlterClassDefinition);
							
						if (FAlterAggregateOperatorStatement.Finalization.Block != null)
							AProcess.CatalogDeviceSession.AlterOperatorBlockNode(LAggregateOperator.Finalization, Compiler.CompileDeallocateVariablesNode(AProcess.Plan, Compiler.CompileStatement(AProcess.Plan, FAlterAggregateOperatorStatement.Finalization.Block), LResultVar));
					}
					finally
					{
						AProcess.Plan.Symbols.PopWindow();
					}
				}
				finally
				{
					AProcess.Plan.PopCreationObject();
				}

				LAggregateOperator.DetermineRemotable(AProcess);
			}

			AProcess.CatalogDeviceSession.AlterMetaData(LAggregateOperator, FAlterAggregateOperatorStatement.AlterMetaData);
			AProcess.Plan.Catalog.UpdateCacheTimeStamp();
			AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LAggregateOperator);

			return null;
		}
    }
    
    public class AlterConstraintNode : AlterNode
    {		
		// AlterConstraintStatement
		protected AlterConstraintStatement FAlterConstraintStatement;
		public AlterConstraintStatement AlterConstraintStatement
		{
			get { return FAlterConstraintStatement; }
			set { FAlterConstraintStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			Schema.Object LObject = base.FindObject(APlan, AObjectName);
			if (!(LObject is Schema.CatalogConstraint))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotConstraint, AObjectName);
			return LObject;
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.CatalogConstraint LConstraint = (Schema.CatalogConstraint)FindObject(APlan, FAlterConstraintStatement.ConstraintName);
			APlan.CheckRight(LConstraint.GetRight(Schema.RightNames.Alter));
			base.BindToProcess (APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.CatalogConstraint LConstraint = (Schema.CatalogConstraint)FindObject(AProcess.Plan, FAlterConstraintStatement.ConstraintName);
			
			if (FAlterConstraintStatement.Expression != null)
			{
				Schema.CatalogConstraint LTempConstraint = new Schema.CatalogConstraint(LConstraint.Name);
				LTempConstraint.ConstraintType = LConstraint.ConstraintType;
				AProcess.Plan.PushCreationObject(LTempConstraint);
				try
				{
					LTempConstraint.Node = Compiler.OptimizeNode(AProcess.Plan, Compiler.BindNode(AProcess.Plan, Compiler.CompileBooleanExpression(AProcess.Plan, FAlterConstraintStatement.Expression)));
					
					// Validate the new constraint
					if (LTempConstraint.Enforced && !AProcess.IsLoading() && AProcess.IsReconciliationEnabled())
					{
						object LObject;
						try
						{
							LObject = LTempConstraint.Node.Execute(AProcess);
						}
						catch (Exception E)
						{
							throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, LTempConstraint.Name);
						}
						
						if (!(bool)LObject)
							throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, LTempConstraint.Name);
					}
					
					AProcess.ServerSession.Server.RemoveCatalogConstraintCheck(LConstraint);
					
					AProcess.CatalogDeviceSession.AlterConstraint(LConstraint, LTempConstraint);
				}
				finally
				{
					AProcess.Plan.PopCreationObject();
				}
			}
			
			AProcess.CatalogDeviceSession.AlterMetaData(LConstraint, FAlterConstraintStatement.AlterMetaData);
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LConstraint);
			return null;
		}
    }
    
    public class AlterReferenceNode : AlterNode
    {		
		public AlterReferenceNode() : base(){}
		public AlterReferenceNode(Schema.Reference AReference) : base()
		{
			FReference = AReference;
		}
		
		private Schema.Reference FReference;

		// AlterReferenceStatement
		protected AlterReferenceStatement FAlterReferenceStatement;
		public AlterReferenceStatement AlterReferenceStatement
		{
			get { return FAlterReferenceStatement; }
			set { FAlterReferenceStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			Schema.Object LObject = base.FindObject(APlan, AObjectName);
			if (!(LObject is Schema.Reference))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotReference, AObjectName);
			return LObject;
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.Reference LReference = (Schema.Reference)FindObject(APlan, FAlterReferenceStatement.ReferenceName);
			APlan.CheckRight(LReference.GetRight(Schema.RightNames.Alter));
			base.BindToProcess (APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			if (FReference == null)
				FReference = (Schema.Reference)FindObject(AProcess.Plan, FAlterReferenceStatement.ReferenceName);
			
			AProcess.CatalogDeviceSession.AlterMetaData(FReference, FAlterReferenceStatement.AlterMetaData);

			FReference.SourceTable.SetShouldReinferReferences(AProcess);
			FReference.TargetTable.SetShouldReinferReferences(AProcess);

			AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
			AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
			
			AProcess.CatalogDeviceSession.UpdateCatalogObject(FReference);
			
			return null;
		}
    }
    
    public class AlterServerNode : AlterNode
    {		
		// AlterServerStatement
		protected AlterServerStatement FAlterServerStatement;
		public AlterServerStatement AlterServerStatement
		{
			get { return FAlterServerStatement; }
			set { FAlterServerStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			Schema.Object LObject = base.FindObject(APlan, AObjectName);
			if (!(LObject is Schema.ServerLink))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotServer, AObjectName);
			return LObject;
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.ServerLink LServerLink = (Schema.ServerLink)FindObject(APlan, FAlterServerStatement.ServerName);
			APlan.CheckRight(LServerLink.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.ServerLink LServer = (Schema.ServerLink)FindObject(AProcess.Plan, FAlterServerStatement.ServerName);
			
			// TODO: Prevent altering the server link with active sessions? (May not be necessary, the active sessions would just continue to use the current settings)
			AProcess.CatalogDeviceSession.AlterMetaData(LServer, FAlterServerStatement.AlterMetaData);
			LServer.ResetServerLink();
			LServer.ApplyMetaData();
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LServer);

			return null;
		}
    }
    
    public class AlterDeviceNode : AlterNode
    {		
		// AlterDeviceStatement
		protected AlterDeviceStatement FAlterDeviceStatement;
		public AlterDeviceStatement AlterDeviceStatement
		{
			get { return FAlterDeviceStatement; }
			set { FAlterDeviceStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			Schema.Object LObject = base.FindObject(APlan, AObjectName);
			if (!(LObject is Schema.Device))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotDevice, AObjectName);
			return LObject;
		}
		
		private Schema.ScalarType FindScalarType(ServerProcess AProcess, string AScalarTypeName)
		{
			Schema.IDataType LDataType = Compiler.CompileTypeSpecifier(AProcess.Plan, new ScalarTypeSpecifier(AScalarTypeName));
			if (!(LDataType is Schema.ScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeExpected);
			return (Schema.ScalarType)LDataType;
		}

		public override void BindToProcess(Plan APlan)
		{
			Schema.Device LDevice = (Schema.Device)FindObject(APlan, FAlterDeviceStatement.DeviceName);
			APlan.CheckRight(LDevice.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			Schema.Device LDevice = (Schema.Device)FindObject(AProcess.Plan, FAlterDeviceStatement.DeviceName);
			AProcess.EnsureDeviceStarted(LDevice);
			
			if (FAlterDeviceStatement.AlterClassDefinition != null)
				AProcess.CatalogDeviceSession.AlterClassDefinition(LDevice.ClassDefinition, FAlterDeviceStatement.AlterClassDefinition, LDevice);

			foreach (DropDeviceScalarTypeMap LDeviceScalarTypeMap in FAlterDeviceStatement.DropDeviceScalarTypeMaps)
			{
				Schema.ScalarType LScalarType = FindScalarType(AProcess, LDeviceScalarTypeMap.ScalarTypeName);
				Schema.DeviceScalarType LDeviceScalarType = LDevice.ResolveDeviceScalarType(AProcess.Plan, LScalarType);
				if (LDeviceScalarType != null)
				{
					Schema.CatalogObjectHeaders LHeaders = AProcess.CatalogDeviceSession.SelectGeneratedObjects(LDeviceScalarType.ID);
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						Schema.DeviceOperator LOperator = AProcess.CatalogDeviceSession.ResolveCatalogObject(LHeaders[LIndex].ID) as Schema.DeviceOperator;
						CheckNoDependents(AProcess, LOperator);
						AProcess.CatalogDeviceSession.DropDeviceOperator(LOperator);
					}
					
					CheckNoDependents(AProcess, LDeviceScalarType);
					AProcess.CatalogDeviceSession.DropDeviceScalarType(LDeviceScalarType);
				}
			}
				
			foreach (DropDeviceOperatorMap LDeviceOperatorMap in FAlterDeviceStatement.DropDeviceOperatorMaps)
			{
				Schema.Operator LOperator = Compiler.ResolveOperatorSpecifier(AProcess.Plan, LDeviceOperatorMap.OperatorSpecifier);
				Schema.DeviceOperator LDeviceOperator = LDevice.ResolveDeviceOperator(AProcess.Plan, LOperator);
				if (LDeviceOperator != null)
				{
					CheckNoDependents(AProcess, LDeviceOperator);
					AProcess.CatalogDeviceSession.DropDeviceOperator(LDeviceOperator);
				}
			}
			
			foreach (AlterDeviceScalarTypeMap LDeviceScalarTypeMap in FAlterDeviceStatement.AlterDeviceScalarTypeMaps)
			{
				Schema.ScalarType LScalarType = FindScalarType(AProcess, LDeviceScalarTypeMap.ScalarTypeName);
				Schema.DeviceScalarType LDeviceScalarType = LDevice.ResolveDeviceScalarType(AProcess.Plan, LScalarType);
				if (LDeviceScalarTypeMap.AlterClassDefinition != null)
					AProcess.CatalogDeviceSession.AlterClassDefinition(LDeviceScalarType.ClassDefinition, LDeviceScalarTypeMap.AlterClassDefinition, LDeviceScalarType);
				AProcess.CatalogDeviceSession.AlterMetaData(LDeviceScalarType, LDeviceScalarTypeMap.AlterMetaData);
				AProcess.CatalogDeviceSession.UpdateCatalogObject(LDeviceScalarType);
			}
				
			foreach (AlterDeviceOperatorMap LDeviceOperatorMap in FAlterDeviceStatement.AlterDeviceOperatorMaps)
			{
				Schema.Operator LOperator = Compiler.ResolveOperatorSpecifier(AProcess.Plan, LDeviceOperatorMap.OperatorSpecifier);
				Schema.DeviceOperator LDeviceOperator = LDevice.ResolveDeviceOperator(AProcess.Plan, LOperator);
				if (LDeviceOperatorMap.AlterClassDefinition != null)
					AProcess.CatalogDeviceSession.AlterClassDefinition(LDeviceOperator.ClassDefinition, LDeviceOperatorMap.AlterClassDefinition, LDeviceOperator);
				AProcess.CatalogDeviceSession.AlterMetaData(LDeviceOperator, LDeviceOperatorMap.AlterMetaData);
				AProcess.CatalogDeviceSession.UpdateCatalogObject(LDeviceOperator);
			}
				
			foreach (DeviceScalarTypeMap LDeviceScalarTypeMap in FAlterDeviceStatement.CreateDeviceScalarTypeMaps)
			{
				Schema.DeviceScalarType LScalarType = Compiler.CompileDeviceScalarTypeMap(AProcess.Plan, LDevice, LDeviceScalarTypeMap);
				if (!AProcess.InLoadingContext())
				{
					Schema.DeviceScalarType LExistingScalarType = LDevice.ResolveDeviceScalarType(AProcess.Plan, LScalarType.ScalarType);
					if ((LExistingScalarType != null) && LExistingScalarType.IsGenerated)
					{
						CheckNoDependents(AProcess, LExistingScalarType); // TODO: These could be updated to point to the new scalar type
						AProcess.CatalogDeviceSession.DropDeviceScalarType(LExistingScalarType);
					}
				}
				AProcess.CatalogDeviceSession.CreateDeviceScalarType(LScalarType);
				
				if (!AProcess.InLoadingContext())
					Compiler.CompileDeviceScalarTypeMapOperatorMaps(AProcess.Plan, LDevice, LScalarType);
			}
				
			foreach (DeviceOperatorMap LDeviceOperatorMap in FAlterDeviceStatement.CreateDeviceOperatorMaps)
			{
				Schema.DeviceOperator LDeviceOperator = Compiler.CompileDeviceOperatorMap(AProcess.Plan, LDevice, LDeviceOperatorMap);
				if (!AProcess.InLoadingContext())
				{
					Schema.DeviceOperator LExistingDeviceOperator = LDevice.ResolveDeviceOperator(AProcess.Plan, LDeviceOperator.Operator);
					if ((LExistingDeviceOperator != null) && LExistingDeviceOperator.IsGenerated)
					{
						CheckNoDependents(AProcess, LExistingDeviceOperator); // TODO: These could be updated to point to the new operator
						AProcess.CatalogDeviceSession.DropDeviceOperator(LExistingDeviceOperator);
					}
				}
				
				AProcess.CatalogDeviceSession.CreateDeviceOperator(LDeviceOperator);
			}
				
			if (FAlterDeviceStatement.ReconciliationSettings != null)
			{
				if (FAlterDeviceStatement.ReconciliationSettings.ReconcileModeSet)
					AProcess.CatalogDeviceSession.SetDeviceReconcileMode(LDevice, FAlterDeviceStatement.ReconciliationSettings.ReconcileMode);
				
				if (FAlterDeviceStatement.ReconciliationSettings.ReconcileMasterSet)
					AProcess.CatalogDeviceSession.SetDeviceReconcileMaster(LDevice, FAlterDeviceStatement.ReconciliationSettings.ReconcileMaster);
			}
				
			AProcess.CatalogDeviceSession.AlterMetaData(LDevice, FAlterDeviceStatement.AlterMetaData);
			AProcess.CatalogDeviceSession.UpdateCatalogObject(LDevice);

			return null;
		}
    }
    
	public abstract class DropObjectNode : DDLNode
	{
		protected void CheckNotSystem(ServerProcess AProcess, Schema.Object AObject)
		{
			if (AObject.IsSystem && !AProcess.Plan.User.IsSystemUser())
				throw new RuntimeException(RuntimeException.Codes.ObjectIsSystem, AObject.Name);
		}
	}
	
	public abstract class DropTableVarNode : DropObjectNode
	{
		private bool FShouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return FShouldAffectDerivationTimeStamp; }
			set { FShouldAffectDerivationTimeStamp = value; }
		}
		
		protected void DropSourceReferences(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			BlockNode LBlockNode = new BlockNode();
			foreach (Schema.Reference LReference in ATableVar.SourceReferences)
				if (LReference.ParentReference == null)
					LBlockNode.Nodes.Add(Compiler.Compile(AProcess.Plan, LReference.EmitDropStatement(EmitMode.ForCopy)));
				
			LBlockNode.Execute(AProcess);
		}
		
		protected void DropEventHandlers(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			BlockNode LBlockNode = new BlockNode();
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(ATableVar.ID, false);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				Schema.DependentObjectHeader LHeader = LHeaders[LIndex];
				if ((LHeader.ObjectType == "TableVarEventHandler") || (LHeader.ObjectType == "TableVarColumnEventHandler"))
					LBlockNode.Nodes.Add(Compiler.Compile(AProcess.Plan, ((Schema.EventHandler)AProcess.CatalogDeviceSession.ResolveObject(LHeader.ID)).EmitDropStatement(EmitMode.ForCopy)));
			}
			
			LBlockNode.Execute(AProcess);
		}
		
		public static void RemoveDeferredConstraintChecks(ServerProcess AProcess, Schema.TableVar ATableVar)
		{
			AProcess.ServerSession.Server.RemoveDeferredConstraintChecks(ATableVar);
		}
	}
    
    public class DropTableNode : DropTableVarNode
    {
		public DropTableNode() : base(){}
		public DropTableNode(Schema.BaseTableVar ATable) : base()
		{
			Table = ATable;
		}
		
		public DropTableNode(Schema.BaseTableVar ATable, bool AShouldAffectDerivationTimeStamp) : base()
		{
			Table = ATable;
			ShouldAffectDerivationTimeStamp = AShouldAffectDerivationTimeStamp;
		}
						
		// Table
		private Schema.BaseTableVar FTable;
		public Schema.BaseTableVar Table 
		{ 
			get { return FTable; } 
			set 
			{ 
				FTable = value; 
				FDevice = FTable == null ? null : FTable.Device;
			}
		}
		
		public override void DetermineDevice(Plan APlan)
		{
			FDevice = FTable.Device;
			FDeviceSupported = false;
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(FTable.GetRight(Schema.RightNames.Drop));
			if (FDevice != null)
				APlan.CheckRight(FDevice.GetRight(Schema.RightNames.DropStore));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				CheckNotSystem(AProcess, FTable);
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, FTable);
				DropSourceReferences(AProcess, FTable);
				DropEventHandlers(AProcess, FTable);
				CheckNoDependents(AProcess, FTable);

				RemoveDeferredConstraintChecks(AProcess, FTable);

				AProcess.CatalogDeviceSession.DropTable(FTable);

				if (ShouldAffectDerivationTimeStamp)
				{
					AProcess.Plan.Catalog.UpdateCacheTimeStamp();
					AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
					AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
				}
				return null;
			}
		}
    }
    
    public class DropViewNode : DropTableVarNode
    {		
		public DropViewNode() : base() {}
		public DropViewNode(Schema.DerivedTableVar ADerivedTableVar) : base()
		{
			FDerivedTableVar = ADerivedTableVar;
		}
		
		public DropViewNode(Schema.DerivedTableVar ADerivedTableVar, bool AShouldAffectDerivationTimeStamp)
		{
			FDerivedTableVar = ADerivedTableVar;
			ShouldAffectDerivationTimeStamp = AShouldAffectDerivationTimeStamp;
		}
		
		private Schema.DerivedTableVar FDerivedTableVar;
		public Schema.DerivedTableVar DerivedTableVar
		{
			get { return FDerivedTableVar; }
			set { FDerivedTableVar = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(FDerivedTableVar.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				CheckNotSystem(AProcess, FDerivedTableVar);
				AProcess.ServerSession.Server.ATDevice.ReportTableChange(AProcess, FDerivedTableVar);
				DropSourceReferences(AProcess, FDerivedTableVar);
				DropEventHandlers(AProcess, FDerivedTableVar);
				CheckNoDependents(AProcess, FDerivedTableVar);

				RemoveDeferredConstraintChecks(AProcess, FDerivedTableVar);

				AProcess.CatalogDeviceSession.DropView(FDerivedTableVar);

				if (ShouldAffectDerivationTimeStamp)
				{
					AProcess.Plan.Catalog.UpdateCacheTimeStamp();
					AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
					AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
				}

				return null;
			}
		}
    }

    public class DropScalarTypeNode : DropObjectNode
	{		
		private Schema.ScalarType FScalarType;
		public Schema.ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(FScalarType.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}
		
		private void DropChildObjects(ServerProcess AProcess, Schema.ScalarType AScalarType)
		{
			AlterScalarTypeStatement LStatement = new AlterScalarTypeStatement();
			LStatement.ScalarTypeName = AScalarType.Name;

			// Drop Constraints
			foreach (Schema.ScalarTypeConstraint LConstraint in AScalarType.Constraints)
				LStatement.DropConstraints.Add(new DropConstraintDefinition(LConstraint.Name));
				
			// Drop Default
			if (AScalarType.Default != null)
				LStatement.Default = new DropDefaultDefinition();
				
			// Drop Specials
			foreach (Schema.Special LSpecial in AScalarType.Specials)
				LStatement.DropSpecials.Add(new DropSpecialDefinition(LSpecial.Name));
				
			Compiler.Compile(AProcess.Plan, LStatement).Execute(AProcess);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				CheckNotSystem(AProcess, FScalarType);
				
				DropChildObjects(AProcess, FScalarType);

				// If the ScalarType has dependents, prevent its destruction
				DropGeneratedDependents(AProcess, FScalarType);
				CheckNoDependents(AProcess, FScalarType);
				
				AProcess.CatalogDeviceSession.DropScalarType(FScalarType);
				AProcess.Plan.Catalog.OperatorResolutionCache.Clear(FScalarType, FScalarType);					
				
				AProcess.Plan.Catalog.UpdateCacheTimeStamp();
				AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
				AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
				return null;
			}
		}
	}
    
	public class DropOperatorNode : DropObjectNode
	{
		public DropOperatorNode() : base(){}
		public DropOperatorNode(Schema.Operator AOperator) : base()
		{
			FDropOperator = AOperator;
		}
		
		private bool FShouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return FShouldAffectDerivationTimeStamp; }
			set { FShouldAffectDerivationTimeStamp = value; }
		}
		
		// OperatorSpecifier
		protected OperatorSpecifier FOperatorSpecifier;
		public OperatorSpecifier OperatorSpecifier
		{
			get { return FOperatorSpecifier; }
			set { FOperatorSpecifier = value; }
		}
		
		// Operator
		protected Schema.Operator FDropOperator;
		public Schema.Operator DropOperator
		{
			get { return FDropOperator; }
			set { FDropOperator = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			if (FDropOperator == null)
				FDropOperator = Compiler.ResolveOperatorSpecifier(APlan, FOperatorSpecifier);
			APlan.CheckRight(FDropOperator.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (FDropOperator == null)
				FDropOperator = Compiler.ResolveOperatorSpecifier(AProcess.Plan, FOperatorSpecifier);
				
			CheckNotSystem(AProcess, FDropOperator);
			AProcess.ServerSession.Server.ATDevice.ReportOperatorChange(AProcess, FDropOperator);

			DropGeneratedSorts(AProcess, FDropOperator);
			CheckNoDependents(AProcess, FDropOperator);
			
			AProcess.CatalogDeviceSession.DropOperator(FDropOperator);
			
			if (ShouldAffectDerivationTimeStamp)
			{
				AProcess.Plan.Catalog.UpdateCacheTimeStamp();
				AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
			}
			return null;
		}
	}
    
	public class DropConstraintNode : DropObjectNode
	{		
		private Schema.CatalogConstraint FConstraint;
		public Schema.CatalogConstraint Constraint
		{
			get { return FConstraint; }
			set { FConstraint = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(FConstraint.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				CheckNotSystem(AProcess, FConstraint);
				CheckNoDependents(AProcess, FConstraint);
					
				AProcess.ServerSession.Server.RemoveCatalogConstraintCheck(FConstraint);
				
				AProcess.CatalogDeviceSession.DropConstraint(FConstraint);

				return null;
			}
		}
	}
    
	public class DropReferenceNode : DropObjectNode
	{		
		public DropReferenceNode() : base(){}
		public DropReferenceNode(Schema.Reference AReference) : base()
		{
			FReference = AReference;
		}
		
		private Schema.Reference FReference;
		
		private string FReferenceName;
		public string ReferenceName
		{
			get { return FReferenceName; }
			set { FReferenceName = value; }
		}

		public override void BindToProcess(Plan APlan)
		{
			if (FReference == null)
				FReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(APlan, FReferenceName);
			APlan.CheckRight(FReference.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (FReference == null)
				FReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(AProcess.Plan, FReferenceName);

			lock (AProcess.Plan.Catalog)
			{
				CheckNotSystem(AProcess, FReference);
				CheckNoDependents(AProcess, FReference);
				
				AProcess.CatalogDeviceSession.DropReference(FReference);

				AProcess.Plan.Catalog.UpdateCacheTimeStamp();
				AProcess.Plan.Catalog.UpdatePlanCacheTimeStamp();
				AProcess.Plan.Catalog.UpdateDerivationTimeStamp();
				return null;
			}
		}
	}
    
	public class DropServerNode : DropObjectNode
	{		
		private Schema.ServerLink FServerLink;
		public Schema.ServerLink ServerLink
		{
			get { return FServerLink; }
			set { FServerLink = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(FServerLink.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				// TODO: Prevent dropping when server sessions are active
				// TODO: Drop server link users
				
				CheckNotSystem(AProcess, FServerLink);
				CheckNoDependents(AProcess, FServerLink);
				
				AProcess.CatalogDeviceSession.DeleteCatalogObject(FServerLink);
			}
			
			return null;
		}
	}
    
	public class DropDeviceNode : DropObjectNode
	{
		private Schema.Device FDropDevice;
		public Schema.Device DropDevice
		{
			get { return FDropDevice; }
			set { FDropDevice = value; }
		}
		
		public override void BindToProcess(Plan APlan)
		{
			APlan.CheckRight(FDropDevice.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			lock (AProcess.Plan.Catalog)
			{
				AProcess.CatalogDeviceSession.StopDevice(FDropDevice);
				
				CheckNotSystem(AProcess, FDropDevice);
				CheckNoBaseTableVarDependents(AProcess, FDropDevice);

				DropDeviceMaps(AProcess, FDropDevice);				
				CheckNoDependents(AProcess, FDropDevice);
				
				AProcess.CatalogDeviceSession.DropDevice(FDropDevice);

				return null;
			}
		}
	}
}
