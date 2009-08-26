/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

	public abstract class DDLNode : PlanNode 
	{
		protected void DropDeviceMaps(Program AProgram, Schema.Device ADevice)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(ADevice.ID, false);
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
				Compiler.BindNode(AProgram.Plan, Compiler.Compile(AProgram.Plan, AProgram.Catalog.EmitDropStatement(AProgram.CatalogDeviceSession, LDependentNames, String.Empty, false, true, false, true))).Execute(AProgram);
			}
		}
		
		protected void DropGeneratedObjects(Program AProgram, Schema.Object AObject)
		{
			List<Schema.CatalogObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectGeneratedObjects(AObject.ID);
			
			if (LHeaders.Count > 0)
			{
				string[] LObjectNames = new string[LHeaders.Count];
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					LObjectNames[LIndex] = LHeaders[LIndex].Name;
					
				Compiler.Compile(AProgram.Plan, AProgram.Catalog.EmitDropStatement(AProgram.CatalogDeviceSession, LObjectNames, String.Empty, false, true, false, true)).Execute(AProgram);
			}
		}
		
		protected void DropGeneratedDependents(Program AProgram, Schema.Object AObject)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
			StringCollection LDeviceMaps = new StringCollection(); // Device maps need to be dropped first, because the dependency of a device map on a generated operator will be reported as a dependency on the generator, not the operator
			StringCollection LDependents = new StringCollection();
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if (LHeaders[LIndex].IsATObject || LHeaders[LIndex].IsSessionObject || LHeaders[LIndex].IsGenerated)
				{
					if (LHeaders[LIndex].ResolveObject(AProgram.CatalogDeviceSession) is Schema.DeviceObject)
						LDeviceMaps.Add(LHeaders[LIndex].Name);
					else
						LDependents.Add(LHeaders[LIndex].Name);
				}
			}
			
			if (LDeviceMaps.Count > 0)
			{
				string[] LDeviceMapNames = new string[LDeviceMaps.Count];
				LDeviceMaps.CopyTo(LDeviceMapNames, 0);
				PlanNode LNode = Compiler.Compile(AProgram.Plan, AProgram.Catalog.EmitDropStatement(AProgram.CatalogDeviceSession, LDeviceMapNames, String.Empty, false, true, true, true));
				LNode.Execute(AProgram);
			}
						
			if (LDependents.Count > 0)
			{
				string[] LDependentNames = new string[LDependents.Count];
				LDependents.CopyTo(LDependentNames, 0);
				PlanNode LNode = Compiler.Compile(AProgram.Plan, AProgram.Catalog.EmitDropStatement(AProgram.CatalogDeviceSession, LDependentNames, String.Empty, false, true, true, true));
				LNode.Execute(AProgram);
			}
		}
		
		protected void DropGeneratedSorts(Program AProgram, Schema.Object AObject)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
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
				Compiler.Compile(AProgram.Plan, AProgram.Catalog.EmitDropStatement(AProgram.CatalogDeviceSession, LDependentNames, String.Empty, false, true, false, true)).Execute(AProgram);
			}
		}
		
		protected void CheckNoDependents(Program AProgram, Schema.Object AObject)
		{
			if (!AProgram.ServerProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
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
		
		protected void CheckNoBaseTableVarDependents(Program AProgram, Schema.Object AObject)
		{
			if (!AProgram.ServerProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
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

		protected void CheckNoOperatorDependents(Program AProgram, Schema.Object AObject)
		{
			if (!AProgram.ServerProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(AObject.ID, false);
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
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.CreateTable(FTable);
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
		
		public override object InternalExecute(Program AProgram)
		{
			if (FView.InvocationExpression == null)
				Error.Fail("Derived table variable invocation expression reference is null");
				
			AProgram.CatalogDeviceSession.CreateView(FView);
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
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.CreateConversion(Conversion);
			return null;
		}
    }
    
    public class DropConversionNode : DropObjectNode
    {
		public Schema.ScalarType SourceScalarType;
		public Schema.ScalarType TargetScalarType;
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				foreach (Schema.Conversion LConversion in SourceScalarType.ImplicitConversions)
					if (LConversion.TargetScalarType.Equals(TargetScalarType))
					{
						CheckNotSystem(AProgram, LConversion);
						DropGeneratedSorts(AProgram, LConversion);
						CheckNoDependents(AProgram, LConversion);
						
						AProgram.CatalogDeviceSession.DropConversion(LConversion);
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
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				AProgram.CatalogDeviceSession.CreateSort(FSort);
				AProgram.CatalogDeviceSession.AttachSort(FScalarType, FSort, FIsUnique);
				AProgram.CatalogDeviceSession.UpdateCatalogObject(FScalarType);
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

		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				if (FScalarType.Sort != null)
				{
					Schema.Sort LSort = FScalarType.Sort;
					AProgram.CatalogDeviceSession.DetachSort(FScalarType, LSort, false);
					AProgram.CatalogDeviceSession.DropSort(LSort);
				}
				AProgram.CatalogDeviceSession.CreateSort(FSort);
				AProgram.CatalogDeviceSession.AttachSort(FScalarType, FSort, false);
				AProgram.CatalogDeviceSession.UpdateCatalogObject(FScalarType);
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
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				Schema.Sort LSort = FIsUnique ? FScalarType.UniqueSort : FScalarType.Sort;
				if (LSort != null)
				{
					CheckNotSystem(AProgram, LSort);
					CheckNoDependents(AProgram, LSort);
					
					AProgram.CatalogDeviceSession.DetachSort(FScalarType, LSort, FIsUnique);
					AProgram.CatalogDeviceSession.DropSort(LSort);
				}
				AProgram.CatalogDeviceSession.UpdateCatalogObject(FScalarType);
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

		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.InsertRole(FRole);
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
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.AlterMetaData(FRole, FStatement.AlterMetaData);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(FRole);
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
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.DeleteRole(FRole);
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

		public override object InternalExecute(Program AProgram)
		{
			SystemCreateRightNode.CreateRight(AProgram, FRightName, AProgram.User.ID);
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

		public override object InternalExecute(Program AProgram)
		{
			SystemDropRightNode.DropRight(AProgram, FRightName);
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
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.CreateScalarType(FScalarType);
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
		
		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.CreateOperator(FCreateOperator);
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

		public override object InternalExecute(Program AProgram)
		{
			if (FConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
			{
				object LObject;

				try
				{
					LObject = FConstraint.Node.Execute(AProgram);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, FConstraint.Name);
				}
				
				if ((LObject != null) && !(bool)LObject)
					throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, FConstraint.Name);
			}
			
			AProgram.CatalogDeviceSession.CreateConstraint(FConstraint);
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

		public override object InternalExecute(Program AProgram)
		{
			// Validate the catalog level enforcement constraint
			if (!AProgram.ServerProcess.ServerSession.Server.IsRepository && FReference.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
			{
				object LObject;

				try
				{
					LObject = FReference.CatalogConstraint.Node.Execute(AProgram);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, FReference.Name);
				}
				
				if ((LObject != null) && !(bool)LObject)
					throw new RuntimeException(RuntimeException.Codes.ReferenceConstraintViolation, FReference.Name);
			}
			
			AProgram.CatalogDeviceSession.CreateReference(FReference);
			AProgram.Catalog.UpdatePlanCacheTimeStamp();
			AProgram.Catalog.UpdateDerivationTimeStamp();
			
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
		
		public override object InternalExecute(Program AProgram)
		{
			FServerLink.ApplyMetaData();
			AProgram.CatalogDeviceSession.InsertCatalogObject(FServerLink);
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

		public override object InternalExecute(Program AProgram)
		{
			AProgram.CatalogDeviceSession.CreateDevice(FNewDevice);
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
		
		public override object InternalExecute(Program AProgram)
		{
			Schema.TableVar LTableVar = FEventSource as Schema.TableVar;
			if ((LTableVar != null) && (!AProgram.ServerProcess.InLoadingContext()))
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, LTableVar);
			AProgram.CatalogDeviceSession.CreateEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex, FBeforeOperatorNames);
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
		
		public override object InternalExecute(Program AProgram)
		{
			Schema.TableVar LTableVar = FEventSource as Schema.TableVar;
			if ((LTableVar != null) && (!AProgram.ServerProcess.InLoadingContext()))
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, LTableVar);
			AProgram.CatalogDeviceSession.AlterEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex, FBeforeOperatorNames);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(FEventHandler);
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
		
		public override object InternalExecute(Program AProgram)
		{
			Schema.TableVar LTableVar = FEventSource as Schema.TableVar;
			if ((LTableVar != null) && (!AProgram.ServerProcess.InLoadingContext()))
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, LTableVar);

			if (FEventHandler.IsDeferred)
				AProgram.ServerProcess.ServerSession.Server.RemoveDeferredHandlers(FEventHandler);

			AProgram.CatalogDeviceSession.DropEventHandler(FEventHandler, FEventSource, FEventSourceColumnIndex);
			return null;
		}
    }
    
    public abstract class AlterNode : DDLNode
    {
		protected virtual Schema.Object FindObject(Plan APlan, string AObjectName)
		{
			return Compiler.ResolveCatalogIdentifier(APlan, AObjectName, true);
		}

		public static void AlterMetaData(Schema.Object AObject, AlterMetaData AAlterMetaData)
		{
			AlterMetaData(AObject, AAlterMetaData, false);
		}
		
		public static void AlterMetaData(Schema.Object AObject, AlterMetaData AAlterMetaData, bool AOptimistic)
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
		
		public static void AlterClassDefinition(ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition)
		{
			AlterClassDefinition(AClassDefinition, AAlterClassDefinition, null);
		}
		
		public static void AlterClassDefinition(ClassDefinition AClassDefinition, AlterClassDefinition AAlterClassDefinition, object AInstance)
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
		
		protected void DropKeys(Program AProgram, Schema.TableVar ATableVar, DropKeyDefinitions ADropKeys)
		{
			if (ADropKeys.Count > 0)
				CheckNoDependents(AProgram, ATableVar);

			foreach (DropKeyDefinition LKeyDefinition in ADropKeys)
			{
				Schema.Key LOldKey = Compiler.FindKey(AProgram.Plan, ATableVar, LKeyDefinition);

				if (LOldKey.IsInherited)
					throw new CompilerException(CompilerException.Codes.InheritedObject, LOldKey.Name);
				
				AProgram.CatalogDeviceSession.DropKey(ATableVar, LOldKey);
			}
		}
		
		protected void AlterKeys(Program AProgram, Schema.TableVar ATableVar, AlterKeyDefinitions AAlterKeys)
		{
			foreach (AlterKeyDefinition LKeyDefinition in AAlterKeys)
			{
				Schema.Key LOldKey = Compiler.FindKey(AProgram.Plan, ATableVar, LKeyDefinition);
					
				AProgram.CatalogDeviceSession.AlterMetaData(LOldKey, LKeyDefinition.AlterMetaData);
			}
		}

		protected void CreateKeys(Program AProgram, Schema.TableVar ATableVar, KeyDefinitions ACreateKeys)
		{
			foreach (KeyDefinition LKeyDefinition in ACreateKeys)
			{
				Schema.Key LNewKey = Compiler.CompileKeyDefinition(AProgram.Plan, ATableVar, LKeyDefinition);
				if (!ATableVar.Keys.Contains(LNewKey))
				{
					if (LNewKey.Enforced)
					{
						// Validate that the key can be created
						Compiler.CompileCatalogConstraintForKey(AProgram.Plan, ATableVar, LNewKey).Validate(AProgram);

						LNewKey.Constraint = Compiler.CompileKeyConstraint(AProgram.Plan, ATableVar, LNewKey);
					}
					AProgram.CatalogDeviceSession.CreateKey(ATableVar, LNewKey);
				}
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, LNewKey.Name);
			}
		}
		
		protected void DropOrders(Program AProgram, Schema.TableVar ATableVar, DropOrderDefinitions ADropOrders)
		{
			if (ADropOrders.Count > 0)
				CheckNoDependents(AProgram, ATableVar);

			foreach (DropOrderDefinition LOrderDefinition in ADropOrders)
			{
				
				Schema.Order LOldOrder = Compiler.FindOrder(AProgram.Plan, ATableVar, LOrderDefinition);
				if (LOldOrder.IsInherited)
					throw new CompilerException(CompilerException.Codes.InheritedObject, LOldOrder.Name);
					
				AProgram.CatalogDeviceSession.DropOrder(ATableVar, LOldOrder);
			}
		}
		
		protected void AlterOrders(Program AProgram, Schema.TableVar ATableVar, AlterOrderDefinitions AAlterOrders)
		{
			foreach (AlterOrderDefinition LOrderDefinition in AAlterOrders)
				AProgram.CatalogDeviceSession.AlterMetaData(Compiler.FindOrder(AProgram.Plan, ATableVar, LOrderDefinition), LOrderDefinition.AlterMetaData);
		}

		protected void CreateOrders(Program AProgram, Schema.TableVar ATableVar, OrderDefinitions ACreateOrders)
		{
			foreach (OrderDefinition LOrderDefinition in ACreateOrders)
			{
				Schema.Order LNewOrder = Compiler.CompileOrderDefinition(AProgram.Plan, ATableVar, LOrderDefinition, false);
				if (!ATableVar.Orders.Contains(LNewOrder))
					AProgram.CatalogDeviceSession.CreateOrder(ATableVar, LNewOrder);
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, LNewOrder.Name);
			}
		}

		protected void DropConstraints(Program AProgram, Schema.TableVar ATableVar, DropConstraintDefinitions ADropConstraints)
		{
			foreach (DropConstraintDefinition LConstraintDefinition in ADropConstraints)
			{
				Schema.TableVarConstraint LConstraint = ATableVar.Constraints[LConstraintDefinition.ConstraintName];
					
				if (LConstraintDefinition.IsTransition && (!(LConstraint is Schema.TransitionConstraint)))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsNotTransitionConstraint, LConstraint.Name);
					
				if (!LConstraintDefinition.IsTransition && !(LConstraint is Schema.RowConstraint))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsTransitionConstraint, LConstraint.Name);
					
				AProgram.CatalogDeviceSession.DropTableVarConstraint(ATableVar, LConstraint);
			}
		}
		
		protected void ValidateConstraint(Program AProgram, Schema.TableVar ATableVar, Schema.TableVarConstraint AConstraint)
		{
			Schema.RowConstraint LRowConstraint = AConstraint as Schema.RowConstraint;
			if (LRowConstraint != null)
			{
				// Ensure that all data in the given table var satisfies the new constraint
				// if exists (table rename new where not (expression)) then raise
				PlanNode LPlanNode = Compiler.EmitTableVarNode(AProgram.Plan, ATableVar);
				LPlanNode = Compiler.EmitRestrictNode(AProgram.Plan, LPlanNode, new UnaryExpression(Instructions.Not, (Expression)LRowConstraint.Node.EmitStatement(EmitMode.ForCopy)));
				LPlanNode = Compiler.EmitUnaryNode(AProgram.Plan, Instructions.Exists, LPlanNode);
				LPlanNode = Compiler.BindNode(AProgram.Plan, Compiler.OptimizeNode(AProgram.Plan, LPlanNode));
				object LObject;

				try
				{
					LObject = LPlanNode.Execute(AProgram);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, AConstraint.Name);
				}
				
				if ((LObject != null) && (bool)LObject)
					throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, AConstraint.Name);
			}
		}
		
		protected void AlterConstraints(Program AProgram, Schema.TableVar ATableVar, AlterConstraintDefinitions AAlterConstraints)
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
						Schema.TableVarConstraint LNewConstraint = Compiler.CompileTableVarConstraint(AProgram.Plan, ATableVar, LNewConstraintDefinition);
							
						// Validate LNewConstraint
						if (LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
							ValidateConstraint(AProgram, ATableVar, LNewConstraint);
							
						AProgram.CatalogDeviceSession.DropTableVarConstraint(ATableVar, LOldConstraint);
						AProgram.CatalogDeviceSession.CreateTableVarConstraint(ATableVar, LNewConstraint);
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
						
						Schema.TransitionConstraint LNewConstraint = (Schema.TransitionConstraint)Compiler.CompileTableVarConstraint(AProgram.Plan, ATableVar, LNewConstraintDefinition);
							
						// Validate LNewConstraint
						if ((LNewConstraint.OnInsertNode != null) && LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
							ValidateConstraint(AProgram, ATableVar, LNewConstraint);
							
						AProgram.CatalogDeviceSession.DropTableVarConstraint(ATableVar, LOldTransitionConstraint);
						AProgram.CatalogDeviceSession.CreateTableVarConstraint(ATableVar, LNewConstraint);
						LOldConstraint = LNewConstraint;
					}
				}
				
				AProgram.CatalogDeviceSession.AlterMetaData(LOldConstraint, LConstraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateConstraints(Program AProgram, Schema.TableVar ATableVar, CreateConstraintDefinitions ACreateConstraints)
		{
			foreach (CreateConstraintDefinition LConstraintDefinition in ACreateConstraints)
			{
				Schema.TableVarConstraint LNewConstraint = Compiler.CompileTableVarConstraint(AProgram.Plan, ATableVar, LConstraintDefinition);
				
				if (((LNewConstraint is Schema.RowConstraint) || (((Schema.TransitionConstraint)LNewConstraint).OnInsertNode != null)) && LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
					ValidateConstraint(AProgram, ATableVar, LNewConstraint);
					
				AProgram.CatalogDeviceSession.CreateTableVarConstraint(ATableVar, LNewConstraint);
			}
		}

		protected void DropReferences(Program AProgram, Schema.TableVar ATableVar, DropReferenceDefinitions ADropReferences)
		{
			foreach (DropReferenceDefinition LReferenceDefinition in ADropReferences)
			{
				Schema.Reference LReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(AProgram.Plan, LReferenceDefinition.ReferenceName);
				AProgram.Plan.AcquireCatalogLock(LReference, LockMode.Exclusive);
				new DropReferenceNode(LReference).Execute(AProgram);
			}
		}

		protected void AlterReferences(Program AProgram, Schema.TableVar ATableVar, AlterReferenceDefinitions AAlterReferences)
		{
			foreach (AlterReferenceDefinition LReferenceDefinition in AAlterReferences)
			{
				Schema.Reference LReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(AProgram.Plan, LReferenceDefinition.ReferenceName);
				AProgram.Plan.AcquireCatalogLock(LReference, LockMode.Exclusive);
				AProgram.CatalogDeviceSession.AlterMetaData(LReference, LReferenceDefinition.AlterMetaData);
			}
		}

		protected void CreateReferences(Program AProgram, Schema.TableVar ATableVar, ReferenceDefinitions ACreateReferences)
		{
			foreach (ReferenceDefinition LReferenceDefinition in ACreateReferences)
			{
				CreateReferenceStatement LStatement = new CreateReferenceStatement();
				LStatement.TableVarName = ATableVar.Name;
				LStatement.ReferenceName = LReferenceDefinition.ReferenceName;
				LStatement.Columns.AddRange(LReferenceDefinition.Columns);
				LStatement.ReferencesDefinition = LReferenceDefinition.ReferencesDefinition;
				LStatement.MetaData = LReferenceDefinition.MetaData;
				Compiler.CompileCreateReferenceStatement(AProgram.Plan, LStatement).Execute(AProgram);
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

		protected void DropColumns(Program AProgram, Schema.BaseTableVar ATable, DropColumnDefinitions ADropColumns)
		{
			if (ADropColumns.Count > 0)
				CheckNoDependents(AProgram, ATable);

			foreach (DropColumnDefinition LColumnDefinition in ADropColumns)
			{
				Schema.TableVarColumn LColumn = ATable.Columns[LColumnDefinition.ColumnName];

				foreach (Schema.Key LKey in ATable.Keys)
					if (LKey.Columns.ContainsName(LColumn.Name))
						throw new CompilerException(CompilerException.Codes.ObjectIsReferenced, LColumn.Name, LKey.Name);
						
				foreach (Schema.Order LOrder in ATable.Orders)
					if (LOrder.Columns.Contains(LColumn.Name))
						throw new CompilerException(CompilerException.Codes.ObjectIsReferenced, LColumn.Name, LOrder.Name);
						
				AProgram.CatalogDeviceSession.DropTableVarColumn(ATable, LColumn);
			}
		}
		
		public static void ReplaceValueNodes(PlanNode ANode, ArrayList AValueNodes, string AColumnName)
		{
			if ((ANode is StackReferenceNode) && (Schema.Object.EnsureUnrooted(((StackReferenceNode)ANode).Identifier) == Keywords.Value))
			{
				((StackReferenceNode)ANode).Identifier = AColumnName;
				AValueNodes.Add(ANode);
			}

			for (int LIndex = 0; LIndex < ANode.NodeCount; LIndex++)
				ReplaceValueNodes(ANode.Nodes[LIndex], AValueNodes, AColumnName);
		}
		
		public static void RestoreValueNodes(ArrayList AValueNodes)
		{
			for (int LIndex = 0; LIndex < AValueNodes.Count; LIndex++)
				((StackReferenceNode)AValueNodes[LIndex]).Identifier = Schema.Object.EnsureRooted(Keywords.Value);
		}
		
		protected void ValidateConstraint(Program AProgram, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, Schema.TableVarColumnConstraint AConstraint)
		{
			// Ensure that all values in the given column of the given base table variable satisfy the new constraint
			ArrayList LValueNodes = new ArrayList();
			ReplaceValueNodes(AConstraint.Node, LValueNodes, AColumn.Name);
			Expression LConstraintExpression = new UnaryExpression(Instructions.Not, (Expression)AConstraint.Node.EmitStatement(EmitMode.ForCopy));
			RestoreValueNodes(LValueNodes);
			PlanNode LPlanNode = 
				Compiler.Bind
				(
					AProgram.Plan, 
					Compiler.Optimize
					(
						AProgram.Plan, 
						Compiler.EmitUnaryNode
						(
							AProgram.Plan, 
							Instructions.Exists, 
							Compiler.EmitRestrictNode
							(
								AProgram.Plan, 
								Compiler.EmitProjectNode
								(
									AProgram.Plan, 
									Compiler.EmitBaseTableVarNode
									(
										AProgram.Plan, 
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
				LObject = LPlanNode.Execute(AProgram);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, AConstraint.Name);
			}

			if ((bool)LObject)
				throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, AConstraint.Name);
		}
		
		protected void DropColumnConstraints(Program AProgram, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, DropConstraintDefinitions ADropConstraints)
		{
			foreach (DropConstraintDefinition LConstraintDefinition in ADropConstraints)
			{
				Schema.Constraint LConstraint = AColumn.Constraints[LConstraintDefinition.ConstraintName];
				
				AProgram.CatalogDeviceSession.DropTableVarColumnConstraint(AColumn, (Schema.TableVarColumnConstraint)LConstraint);
			}
		}
		
		protected void AlterColumnConstraints(Program AProgram, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, AlterConstraintDefinitions AAlterConstraints)
		{
			foreach (AlterConstraintDefinition LConstraintDefinition in AAlterConstraints)
			{
				Schema.TableVarColumnConstraint LConstraint = AColumn.Constraints[LConstraintDefinition.ConstraintName];
				
				if (LConstraintDefinition.Expression != null)
				{
					Schema.TableVarColumnConstraint LNewConstraint = Compiler.CompileTableVarColumnConstraint(AProgram.Plan, ATable, AColumn, new ConstraintDefinition(LConstraintDefinition.ConstraintName, LConstraintDefinition.Expression, LConstraint.MetaData == null ? null : LConstraint.MetaData.Copy()));
					if (LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
						ValidateConstraint(AProgram, ATable, AColumn, LNewConstraint);
					AProgram.CatalogDeviceSession.DropTableVarColumnConstraint(AColumn, LConstraint);
					AProgram.CatalogDeviceSession.CreateTableVarColumnConstraint(AColumn, LNewConstraint);
					LConstraint = LNewConstraint;
				}
					
				AlterMetaData(LConstraint, LConstraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateColumnConstraints(Program AProgram, Schema.BaseTableVar ATable, Schema.TableVarColumn AColumn, ConstraintDefinitions ACreateConstraints)
		{
			foreach (ConstraintDefinition LConstraintDefinition in ACreateConstraints)
			{
				Schema.TableVarColumnConstraint LNewConstraint = Compiler.CompileTableVarColumnConstraint(AProgram.Plan, ATable, AColumn, LConstraintDefinition);
				if (LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
					ValidateConstraint(AProgram, ATable, AColumn, LNewConstraint);
				AProgram.CatalogDeviceSession.CreateTableVarColumnConstraint(AColumn, LNewConstraint);
			}
		}
		
		// TODO: Alter table variable column scalar type
		protected void AlterColumns(Program AProgram, Schema.BaseTableVar ATable, AlterColumnDefinitions AAlterColumns)
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
								AProgram.Plan, 
								Compiler.Bind
								(
									AProgram.Plan, 
									Compiler.CompileExpression
									(
										AProgram.Plan, 
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
							LResult = LNode.Execute(AProgram);
						}
						catch (Exception E)
						{
							throw new RuntimeException(RuntimeException.Codes.ErrorValidatingColumnConstraint, E, "NotNil", LColumn.Name, ATable.DisplayName);
						}
						
						if ((LResult == null) || (bool)LResult)
							throw new RuntimeException(RuntimeException.Codes.NonNilConstraintViolation, LColumn.Name, ATable.DisplayName);
					}

					AProgram.CatalogDeviceSession.SetTableVarColumnIsNilable(LColumn, LColumnDefinition.IsNilable);
				}
					
				if (LColumnDefinition.Default is DefaultDefinition)
				{
					if (LColumn.Default != null)
						throw new RuntimeException(RuntimeException.Codes.DefaultDefined, LColumn.Name, ATable.DisplayName);
						
					AProgram.CatalogDeviceSession.SetTableVarColumnDefault(LColumn, Compiler.CompileTableVarColumnDefault(AProgram.Plan, ATable, LColumn, (DefaultDefinition)LColumnDefinition.Default));
				}
				else if (LColumnDefinition.Default is AlterDefaultDefinition)
				{
					if (LColumn.Default == null)
						throw new RuntimeException(RuntimeException.Codes.DefaultNotDefined, LColumn.Name, ATable.DisplayName);

					AlterDefaultDefinition LDefaultDefinition = (AlterDefaultDefinition)LColumnDefinition.Default;
					if (LDefaultDefinition.Expression != null)
					{
						Schema.TableVarColumnDefault LNewDefault = Compiler.CompileTableVarColumnDefault(AProgram.Plan, ATable, LColumn, new DefaultDefinition(LDefaultDefinition.Expression, LColumn.Default.MetaData == null ? null : LColumn.Default.MetaData.Copy()));
						AProgram.CatalogDeviceSession.SetTableVarColumnDefault(LColumn, LNewDefault);
					}

					AProgram.CatalogDeviceSession.AlterMetaData(LColumn.Default, LDefaultDefinition.AlterMetaData);
				}
				else if (LColumnDefinition.Default is DropDefaultDefinition)
				{
					if (LColumn.Default == null)
						throw new RuntimeException(RuntimeException.Codes.DefaultNotDefined, LColumn.Name, ATable.DisplayName);
					AProgram.CatalogDeviceSession.SetTableVarColumnDefault(LColumn, null);
				}
				
				DropColumnConstraints(AProgram, ATable, LColumn, LColumnDefinition.DropConstraints);
				AlterColumnConstraints(AProgram, ATable, LColumn, LColumnDefinition.AlterConstraints);
				CreateColumnConstraints(AProgram, ATable, LColumn, LColumnDefinition.CreateConstraints);
				
				AProgram.CatalogDeviceSession.AlterMetaData(LColumn, LColumnDefinition.AlterMetaData);
			}
		}
		
		protected Schema.Objects FNonNilableColumns;
		protected Schema.Objects FDefaultColumns;

		protected void CreateColumns(Program AProgram, Schema.BaseTableVar ATable, ColumnDefinitions ACreateColumns)
		{
			FNonNilableColumns = new Schema.Objects();
			FDefaultColumns = new Schema.Objects();
			Schema.BaseTableVar LDummy = new Schema.BaseTableVar(ATable.Name);
			LDummy.Library = ATable.Library;
			LDummy.IsGenerated = ATable.IsGenerated;
			AProgram.Plan.PushCreationObject(LDummy);
			try
			{
				if (ACreateColumns.Count > 0)
					CheckNoOperatorDependents(AProgram, ATable);
				
				foreach (ColumnDefinition LColumnDefinition in ACreateColumns)
				{
					Schema.TableVarColumn LTableVarColumn = Compiler.CompileTableVarColumnDefinition(AProgram.Plan, ATable, LColumnDefinition);
					if (LTableVarColumn.Default != null)
						FDefaultColumns.Add(LTableVarColumn);
					if (!LTableVarColumn.IsNilable)
					{
						if (LTableVarColumn.Default == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidAlterTableVarCreateColumnStatement, LTableVarColumn.Name, ATable.DisplayName);
						LTableVarColumn.IsNilable = true;
						FNonNilableColumns.Add(LTableVarColumn);
					}
					
					AProgram.CatalogDeviceSession.CreateTableVarColumn(ATable, LTableVarColumn);
				}
				ATable.DetermineRemotable(AProgram.CatalogDeviceSession);
			}
			finally
			{
				AProgram.Plan.PopCreationObject();
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
		
		private void UpdateDefaultColumns(Program AProgram)
		{
			if (FDefaultColumns.Count != 0)
			{
				// Update the data in all columns that have been added
				// update <table name> set { <column name> := <default expression> [, ...] }
				UpdateStatement LUpdateStatement = new UpdateStatement(new IdentifierExpression(FTableVar.Name));
				foreach (Schema.TableVarColumn LColumn in FDefaultColumns)
					LUpdateStatement.Columns.Add(new UpdateColumnExpression(new IdentifierExpression(LColumn.Name), (Expression)LColumn.Default.Node.EmitStatement(EmitMode.ForCopy)));
					
				Compiler.BindNode(AProgram.Plan, Compiler.CompileUpdateStatement(AProgram.Plan, LUpdateStatement)).Execute(AProgram);
				
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
				
				Compiler.BindNode(AProgram.Plan, Compiler.CompileAlterTableStatement(AProgram.Plan, LAlterStatement)).Execute(AProgram);
			}
		}
		
		public override object InternalExecute(Program AProgram)
		{
			FTableVar = (Schema.BaseTableVar)FindObject(AProgram.Plan, FAlterTableVarStatement.TableVarName);
			if (!AProgram.ServerProcess.InLoadingContext())
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, FTableVar);

			DropColumns(AProgram, FTableVar, AlterTableStatement.DropColumns);
			AlterColumns(AProgram, FTableVar, AlterTableStatement.AlterColumns);
			CreateColumns(AProgram, FTableVar, AlterTableStatement.CreateColumns);

			// Change columns in the device			
			AlterTableStatement LStatement = new AlterTableStatement();
			LStatement.TableVarName = AlterTableStatement.TableVarName;
			LStatement.DropColumns.AddRange(AlterTableStatement.DropColumns);
			LStatement.AlterColumns.AddRange(AlterTableStatement.AlterColumns);
			LStatement.CreateColumns.AddRange(AlterTableStatement.CreateColumns);
			AlterTableNode LNode = new AlterTableNode();
			LNode.AlterTableStatement = LStatement;
			LNode.DetermineDevice(AProgram.Plan);
			AProgram.DeviceExecute(FTableVar.Device, LNode);
			UpdateDefaultColumns(AProgram);

			// Drop keys and orders
			LStatement = new AlterTableStatement();
			LStatement.TableVarName = AlterTableStatement.TableVarName;
			LStatement.DropKeys.AddRange(AlterTableStatement.DropKeys);
			LStatement.DropOrders.AddRange(AlterTableStatement.DropOrders);
			LNode = new AlterTableNode();
			LNode.AlterTableStatement = LStatement;
			LNode.DetermineDevice(AProgram.Plan);
			AProgram.DeviceExecute(FTableVar.Device, LNode);
			DropKeys(AProgram, FTableVar, FAlterTableVarStatement.DropKeys);
			DropOrders(AProgram, FTableVar, FAlterTableVarStatement.DropOrders);

			AlterKeys(AProgram, FTableVar, FAlterTableVarStatement.AlterKeys);
			AlterOrders(AProgram, FTableVar, FAlterTableVarStatement.AlterOrders);
			CreateKeys(AProgram, FTableVar, FAlterTableVarStatement.CreateKeys);
			CreateOrders(AProgram, FTableVar, FAlterTableVarStatement.CreateOrders);

			LStatement = new AlterTableStatement();
			LStatement.TableVarName = AlterTableStatement.TableVarName;
			LStatement.CreateKeys.AddRange(AlterTableStatement.CreateKeys);
			LStatement.CreateOrders.AddRange(AlterTableStatement.CreateOrders);
			LNode = new AlterTableNode();
			LNode.AlterTableStatement = LStatement;
			LNode.DetermineDevice(AProgram.Plan);
			AProgram.DeviceExecute(FTableVar.Device, LNode);

			DropReferences(AProgram, FTableVar, FAlterTableVarStatement.DropReferences);
			AlterReferences(AProgram, FTableVar, FAlterTableVarStatement.AlterReferences);
			CreateReferences(AProgram, FTableVar, FAlterTableVarStatement.CreateReferences);
			DropConstraints(AProgram, FTableVar, FAlterTableVarStatement.DropConstraints);
			AlterConstraints(AProgram, FTableVar, FAlterTableVarStatement.AlterConstraints);
			CreateConstraints(AProgram, FTableVar, FAlterTableVarStatement.CreateConstraints);
			AProgram.CatalogDeviceSession.AlterMetaData(FTableVar, FAlterTableVarStatement.AlterMetaData);
			if (ShouldAffectDerivationTimeStamp)
			{
				AProgram.Catalog.UpdateCacheTimeStamp();
				AProgram.Catalog.UpdatePlanCacheTimeStamp();
				AProgram.Catalog.UpdateDerivationTimeStamp();
			}

			AProgram.CatalogDeviceSession.UpdateCatalogObject(FTableVar);
			
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
		
		public override object InternalExecute(Program AProgram)
		{
			Schema.DerivedTableVar LView = (Schema.DerivedTableVar)FindObject(AProgram.Plan, FAlterTableVarStatement.TableVarName);
			if (!AProgram.ServerProcess.InLoadingContext())
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, LView);

			DropKeys(AProgram, LView, FAlterTableVarStatement.DropKeys);
			AlterKeys(AProgram, LView, FAlterTableVarStatement.AlterKeys);
			CreateKeys(AProgram, LView, FAlterTableVarStatement.CreateKeys);
			DropOrders(AProgram, LView, FAlterTableVarStatement.DropOrders);
			AlterOrders(AProgram, LView, FAlterTableVarStatement.AlterOrders);
			CreateOrders(AProgram, LView, FAlterTableVarStatement.CreateOrders);
			DropReferences(AProgram, LView, FAlterTableVarStatement.DropReferences);
			AlterReferences(AProgram, LView, FAlterTableVarStatement.AlterReferences);
			CreateReferences(AProgram, LView, FAlterTableVarStatement.CreateReferences);
			DropConstraints(AProgram, LView, FAlterTableVarStatement.DropConstraints);
			AlterConstraints(AProgram, LView, FAlterTableVarStatement.AlterConstraints);
			CreateConstraints(AProgram, LView, FAlterTableVarStatement.CreateConstraints);
			AProgram.CatalogDeviceSession.AlterMetaData(LView, FAlterTableVarStatement.AlterMetaData);

			if (ShouldAffectDerivationTimeStamp)
			{
				AProgram.Catalog.UpdateCacheTimeStamp();
				AProgram.Catalog.UpdatePlanCacheTimeStamp();
				AProgram.Catalog.UpdateDerivationTimeStamp();
			}
			
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LView);
			
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
		
		protected void DropConstraints(Program AProgram, Schema.ScalarType AScalarType, DropConstraintDefinitions ADropConstraints)
		{
			foreach (DropConstraintDefinition LConstraintDefinition in ADropConstraints)
			{
				Schema.Constraint LConstraint = AScalarType.Constraints[LConstraintDefinition.ConstraintName];
				AProgram.CatalogDeviceSession.DropScalarTypeConstraint(AScalarType, (Schema.ScalarTypeConstraint)LConstraint);
			}
		}
		
		protected void ValidateConstraint(Program AProgram, Schema.ScalarType AScalarType, Schema.ScalarTypeConstraint AConstraint)
		{
			// Ensure that all base table vars in the catalog with columns defined on this scalar type, or descendents of this scalar type, satisfy the new constraint
			List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(AScalarType.ID, false);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				if (LHeaders[LIndex].ObjectType == "ScalarType")
				{
					// This is a descendent scalar type
					ValidateConstraint(AProgram, (Schema.ScalarType)AProgram.CatalogDeviceSession.ResolveObject(LHeaders[LIndex].ID), AConstraint);
				}
				else if ((LHeaders[LIndex].ObjectType == "BaseTableVar") && !LHeaders[LIndex].IsATObject)
				{
					// This is a tablevar with columns defined on this scalar type
					// Open a cursor on the expression <tablevar> over { <columns defined on this scalar type> }
					Schema.BaseTableVar LBaseTableVar = (Schema.BaseTableVar)AProgram.CatalogDeviceSession.ResolveObject(LHeaders[LIndex].ID);
					foreach (Schema.Column LColumn in LBaseTableVar.DataType.Columns)
						if (LColumn.DataType.Equals(AScalarType))
						{
							// if exists (table over { column } where not (constraint expression))) then raise
							ArrayList LValueNodes = new ArrayList();
							AlterTableNode.ReplaceValueNodes(AConstraint.Node, LValueNodes, LColumn.Name);
							Expression LConstraintExpression = new UnaryExpression(Instructions.Not, (Expression)AConstraint.Node.EmitStatement(EmitMode.ForCopy));
							AlterTableNode.RestoreValueNodes(LValueNodes);
							PlanNode LPlanNode = Compiler.EmitBaseTableVarNode(AProgram.Plan, LBaseTableVar);
							LPlanNode = Compiler.EmitProjectNode(AProgram.Plan, LPlanNode, new string[]{LColumn.Name}, true);
							LPlanNode = Compiler.EmitRestrictNode(AProgram.Plan, LPlanNode, LConstraintExpression);
							LPlanNode = Compiler.EmitUnaryNode(AProgram.Plan, Instructions.Exists, LPlanNode);
							LPlanNode = Compiler.BindNode(AProgram.Plan, LPlanNode);
							object LResult;

							try
							{
								LResult = LPlanNode.Execute(AProgram);
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
		
		protected void AlterConstraints(Program AProgram, Schema.ScalarType AScalarType, AlterConstraintDefinitions AAlterConstraints)
		{
			Schema.ScalarTypeConstraint LConstraint;
			foreach (AlterConstraintDefinition LConstraintDefinition in AAlterConstraints)
			{
				LConstraint = AScalarType.Constraints[LConstraintDefinition.ConstraintName];
				
				if (LConstraintDefinition.Expression != null)
				{
					Schema.ScalarTypeConstraint LNewConstraint = Compiler.CompileScalarTypeConstraint(AProgram.Plan, AScalarType, new ConstraintDefinition(LConstraintDefinition.ConstraintName, LConstraintDefinition.Expression, LConstraint.MetaData == null ? null : LConstraint.MetaData.Copy()));
					if (LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
						ValidateConstraint(AProgram, AScalarType, LNewConstraint);
					
					AProgram.CatalogDeviceSession.DropScalarTypeConstraint(AScalarType, LConstraint);
					AProgram.CatalogDeviceSession.CreateScalarTypeConstraint(AScalarType, LNewConstraint);
					LConstraint = LNewConstraint;
				}
					
				AProgram.CatalogDeviceSession.AlterMetaData(LConstraint, LConstraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateConstraints(Program AProgram, Schema.ScalarType AScalarType, ConstraintDefinitions ACreateConstraints)
		{
			foreach (ConstraintDefinition LConstraintDefinition in ACreateConstraints)
			{
				Schema.ScalarTypeConstraint LNewConstraint = Compiler.CompileScalarTypeConstraint(AProgram.Plan, AScalarType, LConstraintDefinition);

				if (LNewConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
					ValidateConstraint(AProgram, AScalarType, LNewConstraint);

				AProgram.CatalogDeviceSession.CreateScalarTypeConstraint(AScalarType, LNewConstraint);
			}
		}
		
		protected void DropSpecials(Program AProgram, Schema.ScalarType AScalarType, DropSpecialDefinitions ADropSpecials)
		{
			foreach (DropSpecialDefinition LSpecialDefinition in ADropSpecials)
			{
				if (AScalarType.IsSpecialOperator != null)
				{
					new DropOperatorNode(AScalarType.IsSpecialOperator).Execute(AProgram);
					AProgram.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, null);
				}
				
				// Dropping a special
				Schema.Special LSpecial = AScalarType.Specials[LSpecialDefinition.Name];

				// drop operator ScalarTypeNameSpecialName()
				new DropOperatorNode(LSpecial.Comparer).Execute(AProgram);
				// drop operator IsSpecialName(ScalarType)
				new DropOperatorNode(LSpecial.Selector).Execute(AProgram);

				AProgram.CatalogDeviceSession.DropSpecial(AScalarType, LSpecial);
			}
		}
		
		protected void AlterSpecials(Program AProgram, Schema.ScalarType AScalarType, AlterSpecialDefinitions AAlterSpecials)
		{
			Schema.Special LSpecial;
			foreach (AlterSpecialDefinition LSpecialDefinition in AAlterSpecials)
			{
				LSpecial = AScalarType.Specials[LSpecialDefinition.Name];

				if (LSpecialDefinition.Value != null)
				{
					if (AScalarType.IsSpecialOperator != null)
					{
						new DropOperatorNode(AScalarType.IsSpecialOperator).Execute(AProgram);
						AProgram.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, null);
					}
					
					Schema.Special LNewSpecial = new Schema.Special(Schema.Object.GetNextObjectID(), LSpecial.Name);
					LNewSpecial.Library = AScalarType.Library == null ? null : AProgram.Plan.CurrentLibrary;
					AProgram.Plan.PushCreationObject(LNewSpecial);
					try
					{
						LNewSpecial.ValueNode = Compiler.CompileTypedExpression(AProgram.Plan, LSpecialDefinition.Value, AScalarType);

						// Recompilation of the comparer and selector for the special
						Schema.Operator LNewSelector = Compiler.CompileSpecialSelector(AProgram.Plan, AScalarType, LNewSpecial, LSpecialDefinition.Name, LNewSpecial.ValueNode);
						if (LNewSpecial.HasDependencies())
							LNewSelector.AddDependencies(LNewSpecial.Dependencies);
						LNewSelector.DetermineRemotable(AProgram.CatalogDeviceSession);

						Schema.Operator LNewComparer = Compiler.CompileSpecialComparer(AProgram.Plan, AScalarType, LNewSpecial, LSpecialDefinition.Name, LNewSpecial.ValueNode);
						if (LNewSpecial.HasDependencies())
							LNewComparer.AddDependencies(LNewSpecial.Dependencies);
						LNewComparer.DetermineRemotable(AProgram.CatalogDeviceSession);

						new DropOperatorNode(LSpecial.Selector).Execute(AProgram);
						new CreateOperatorNode(LNewSelector).Execute(AProgram);
						LNewSpecial.Selector = LNewSelector;

						new DropOperatorNode(LSpecial.Comparer).Execute(AProgram);
						new CreateOperatorNode(LNewComparer).Execute(AProgram);
						LNewSpecial.Comparer = LNewComparer;
						
						AProgram.CatalogDeviceSession.DropSpecial(AScalarType, LSpecial);
						AProgram.CatalogDeviceSession.CreateSpecial(AScalarType, LNewSpecial);
						LSpecial = LNewSpecial;
					}
					finally
					{
						AProgram.Plan.PopCreationObject();
					}
				}

				AProgram.CatalogDeviceSession.AlterMetaData(LSpecial, LSpecialDefinition.AlterMetaData);
			}
		}
		
		protected void EnsureIsSpecialOperator(Program AProgram, Schema.ScalarType AScalarType)
		{
			if (AScalarType.IsSpecialOperator == null)
			{
				OperatorBindingContext LContext = new OperatorBindingContext(null, "IsSpecial", AProgram.Plan.NameResolutionPath, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(AScalarType)}), true);
				Compiler.ResolveOperator(AProgram.Plan, LContext);
				if (LContext.Operator == null)
				{
					AProgram.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, Compiler.CompileSpecialOperator(AProgram.Plan, AScalarType));
					new CreateOperatorNode(AScalarType.IsSpecialOperator).Execute(AProgram);
				}
			}
		}
		
		protected void CreateSpecials(Program AProgram, Schema.ScalarType AScalarType, SpecialDefinitions ACreateSpecials)
		{
			foreach (SpecialDefinition LSpecialDefinition in ACreateSpecials)
			{
				// If we are deserializing, then there is no need to recreate the IsSpecial operator
				if ((!AProgram.ServerProcess.InLoadingContext()) && (AScalarType.IsSpecialOperator != null))
				{
					new DropOperatorNode(AScalarType.IsSpecialOperator).Execute(AProgram);
					AProgram.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(AScalarType, null);
				}
				
				Schema.Special LSpecial = Compiler.CompileSpecial(AProgram.Plan, AScalarType, LSpecialDefinition); 
				AProgram.CatalogDeviceSession.CreateSpecial(AScalarType, LSpecial);
				if (!AProgram.ServerProcess.InLoadingContext())
				{
					new CreateOperatorNode(LSpecial.Selector).Execute(AProgram);
					new CreateOperatorNode(LSpecial.Comparer).Execute(AProgram);
				}
			}
			
			if (!AProgram.ServerProcess.InLoadingContext())
				EnsureIsSpecialOperator(AProgram, AScalarType);
		}

		protected void DropRepresentations(Program AProgram, Schema.ScalarType AScalarType, DropRepresentationDefinitions ADropRepresentations)
		{
			Schema.Representation LRepresentation;
			foreach (DropRepresentationDefinition LRepresentationDefinition in ADropRepresentations)
			{
				LRepresentation = AScalarType.Representations[LRepresentationDefinition.RepresentationName];
				foreach (Schema.Property LProperty in LRepresentation.Properties)
				{
					new DropOperatorNode(LProperty.ReadAccessor).Execute(AProgram);
					new DropOperatorNode(LProperty.WriteAccessor).Execute(AProgram);
				}
				
				new DropOperatorNode(LRepresentation.Selector).Execute(AProgram);
				AProgram.CatalogDeviceSession.DropRepresentation(AScalarType, LRepresentation);
				AScalarType.ResetNativeRepresentationCache();
			}
		}
		
		protected void AlterRepresentations(Program AProgram, Schema.ScalarType AScalarType, AlterRepresentationDefinitions AAlterRepresentations)
		{
			Schema.Representation LRepresentation;
			foreach (AlterRepresentationDefinition LRepresentationDefinition in AAlterRepresentations)
			{
				LRepresentation = AScalarType.Representations[LRepresentationDefinition.RepresentationName];
				
				DropProperties(AProgram, AScalarType, LRepresentation, LRepresentationDefinition.DropProperties);
				AlterProperties(AProgram, AScalarType, LRepresentation, LRepresentationDefinition.AlterProperties);
				CreateProperties(AProgram, AScalarType, LRepresentation, LRepresentationDefinition.CreateProperties);
				
				if (LRepresentationDefinition.SelectorAccessorBlock != null)
				{
					Error.Fail("Altering the selector for a representation is not implemented");
					/*
					AlterClassDefinition(AProgram, LRepresentation.Selector, LRepresentationDefinition.AlterClassDefinition);
					
					Schema.Operator LOperator = Compiler.CompileRepresentationSelector(AProgram.Plan, AScalarType, LRepresentation);
					Schema.Operator LOldOperator = LRepresentation.Operator;
					new DropOperatorNode(LOldOperator).Execute(AProgram);
					try
					{
						LRepresentation.Operator = LOperator;
						new CreateOperatorNode(LOperator).Execute(AProgram);
					}
					catch
					{
						new CreateOperatorNode(LOldOperator).Execute(AProgram);
						LRepresentation.Operator = LOldOperator;
						throw;
					}
					*/
				}

				AProgram.CatalogDeviceSession.AlterMetaData(LRepresentation, LRepresentationDefinition.AlterMetaData);
				AScalarType.ResetNativeRepresentationCache();
			}
		}
		
		protected void CreateRepresentations(Program AProgram, Schema.ScalarType AScalarType, RepresentationDefinitions ACreateRepresentations)
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
					DropRepresentations(AProgram, AScalarType, LDropRepresentationDefinitions);
				}

				Schema.Representation LRepresentation = Compiler.CompileRepresentation(AProgram.Plan, AScalarType, LOperators, LRepresentationDefinition);
				AProgram.CatalogDeviceSession.CreateRepresentation(AScalarType, LRepresentation);
				AProgram.Plan.CheckCompiled(); // Throw the error after the call to the catalog device session so the error occurs after the create is logged.
				AScalarType.ResetNativeRepresentationCache();
			}

			if (!AProgram.ServerProcess.InLoadingContext())
				foreach (Schema.Operator LOperator in LOperators)
					new CreateOperatorNode(LOperator).Execute(AProgram);
		}

		protected void DropProperties(Program AProgram, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, DropPropertyDefinitions ADropProperties)
		{
			foreach (DropPropertyDefinition LPropertyDefinition in ADropProperties)
			{
				Schema.Property LProperty = ARepresentation.Properties[LPropertyDefinition.PropertyName];
				new DropOperatorNode(LProperty.ReadAccessor).Execute(AProgram);
				new DropOperatorNode(LProperty.WriteAccessor).Execute(AProgram);
				AProgram.CatalogDeviceSession.DropProperty(ARepresentation, LProperty);
			}
		}
		
		protected void AlterProperties(Program AProgram, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, AlterPropertyDefinitions AAlterProperties)
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
					AProgram.Plan.PushCreationObject(LProperty);
					try
					{
						LProperty.DataType = (Schema.ScalarType)Compiler.CompileTypeSpecifier(AProgram.Plan, LPropertyDefinition.PropertyType);
						LProperty.AttachDependencies();
					}
					finally
					{
						AProgram.Plan.PopCreationObject();
					}
					*/
				}
					
				if (LPropertyDefinition.ReadAccessorBlock != null)
				{
					Error.Fail("Altering the read accessor for a property is not implemented");
					/*
					AlterClassDefinition(AProgram, LProperty.ReadAccessor, LPropertyDefinition.ReadClassDefinition);
					Schema.Operator LOldOperator = LProperty.ReadOperator;
					Schema.Operator LOperator = Compiler.CompilePropertyReadAccessor(AProgram.Plan, AScalarType, ARepresentation, LProperty);
					new DropOperatorNode(LOldOperator).Execute(AProgram);
					try
					{
						new CreateOperatorNode(LOperator).Execute(AProgram);
						LProperty.ReadOperator = LOperator;
					}
					catch
					{
						new CreateOperatorNode(LOldOperator).Execute(AProgram);
						throw;
					}
					*/
				}

				if (LPropertyDefinition.WriteAccessorBlock != null)
				{
					Error.Fail("Altering the write accessor for a property is not implemented");
					/*
					AlterClassDefinition(AProgram, LProperty.WriteAccessor, LPropertyDefinition.WriteClassDefinition);
					Schema.Operator LOldOperator = LProperty.WriteOperator;
					Schema.Operator LOperator = Compiler.CompilePropertyWriteAccessor(AProgram.Plan, AScalarType, ARepresentation, LProperty);
					new DropOperatorNode(LOldOperator).Execute(AProgram);
					try
					{
						new CreateOperatorNode(LOperator).Execute(AProgram);
						LProperty.WriteOperator = LOperator;
					}
					catch
					{
						new CreateOperatorNode(LOldOperator).Execute(AProgram);
						throw;
					}
					*/
				}

				AProgram.CatalogDeviceSession.AlterMetaData(LProperty, LPropertyDefinition.AlterMetaData);
			}
		}
		
		protected void CreateProperties(Program AProgram, Schema.ScalarType AScalarType, Schema.Representation ARepresentation, PropertyDefinitions ACreateProperties)
		{
			Schema.Objects LOperators = new Schema.Objects();
			foreach (PropertyDefinition LPropertyDefinition in ACreateProperties)
			{
				Schema.Property LProperty = Compiler.CompileProperty(AProgram.Plan, AScalarType, ARepresentation, LPropertyDefinition);
				AProgram.CatalogDeviceSession.CreateProperty(ARepresentation, LProperty);
				AProgram.Plan.CheckCompiled(); // Throw the error after the CreateProperty call so that the operation is logged in the catalog device
			}
			
			foreach (Schema.Operator LOperator in LOperators)
				new CreateOperatorNode(LOperator).Execute(AProgram);
		}
		
		public override void BindToProcess(Plan APlan)
		{
			Schema.ScalarType LScalarType = (Schema.ScalarType)FindObject(APlan, FAlterScalarTypeStatement.ScalarTypeName);
			APlan.CheckRight(LScalarType.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(APlan);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			Schema.ScalarType LScalarType = (Schema.ScalarType)FindObject(AProgram.Plan, FAlterScalarTypeStatement.ScalarTypeName);
			
			// Constraints			
			DropConstraints(AProgram, LScalarType, FAlterScalarTypeStatement.DropConstraints);
			AlterConstraints(AProgram, LScalarType, FAlterScalarTypeStatement.AlterConstraints);
			CreateConstraints(AProgram, LScalarType, FAlterScalarTypeStatement.CreateConstraints);

			// Default
			if (FAlterScalarTypeStatement.Default is DefaultDefinition)
			{
				if (LScalarType.Default != null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultDefined, LScalarType.Name);
					
				AProgram.CatalogDeviceSession.SetScalarTypeDefault(LScalarType, Compiler.CompileScalarTypeDefault(AProgram.Plan, LScalarType, (DefaultDefinition)FAlterScalarTypeStatement.Default));
			}
			else if (FAlterScalarTypeStatement.Default is AlterDefaultDefinition)
			{
				if (LScalarType.Default == null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultNotDefined, LScalarType.Name);

				AlterDefaultDefinition LDefaultDefinition = (AlterDefaultDefinition)FAlterScalarTypeStatement.Default;
				if (LDefaultDefinition.Expression != null)
				{
					Schema.ScalarTypeDefault LNewDefault = Compiler.CompileScalarTypeDefault(AProgram.Plan, LScalarType, new DefaultDefinition(LDefaultDefinition.Expression, LScalarType.Default.MetaData == null ? null : LScalarType.Default.MetaData.Copy()));
					
					AProgram.CatalogDeviceSession.SetScalarTypeDefault(LScalarType, LNewDefault);
				}

				AProgram.CatalogDeviceSession.AlterMetaData(LScalarType.Default, LDefaultDefinition.AlterMetaData);
			}
			else if (FAlterScalarTypeStatement.Default is DropDefaultDefinition)
			{
				if (LScalarType.Default == null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultNotDefined, LScalarType.Name);
					
				AProgram.CatalogDeviceSession.SetScalarTypeDefault(LScalarType, null);
			}
			
			// Specials
			DropSpecials(AProgram, LScalarType, FAlterScalarTypeStatement.DropSpecials);
			AlterSpecials(AProgram, LScalarType, FAlterScalarTypeStatement.AlterSpecials);
			CreateSpecials(AProgram, LScalarType, FAlterScalarTypeStatement.CreateSpecials);
			
			// Representations
			DropRepresentations(AProgram, LScalarType, FAlterScalarTypeStatement.DropRepresentations);
			AlterRepresentations(AProgram, LScalarType, FAlterScalarTypeStatement.AlterRepresentations);
			CreateRepresentations(AProgram, LScalarType, FAlterScalarTypeStatement.CreateRepresentations);
			
			// If this scalar type has no representations defined, but it is based on a single branch inheritance hierarchy leading to a system type, build a default representation
			#if USETYPEINHERITANCE
			Schema.Objects LOperators = new Schema.Objects();
			if ((FAlterScalarTypeStatement.DropRepresentations.Count > 0) && (LScalarType.Representations.Count == 0))
				Compiler.CompileDefaultRepresentation(AProgram.Plan, LScalarType, LOperators);
			foreach (Schema.Operator LOperator in LOperators)
				new CreateOperatorNode(LOperator).Execute(AProgram);
			#endif
			
			// TODO: Alter semantics for types and representations

			AProgram.CatalogDeviceSession.AlterClassDefinition(LScalarType.ClassDefinition, FAlterScalarTypeStatement.AlterClassDefinition);
			AProgram.CatalogDeviceSession.AlterMetaData(LScalarType, FAlterScalarTypeStatement.AlterMetaData);
			
			if 
			(
				(FAlterScalarTypeStatement.AlterClassDefinition != null) || 
				(FAlterScalarTypeStatement.AlterMetaData != null)
			)
				LScalarType.ResetNativeRepresentationCache();
			
			AProgram.Catalog.UpdateCacheTimeStamp();
			AProgram.Catalog.UpdatePlanCacheTimeStamp();
			AProgram.Catalog.UpdateDerivationTimeStamp();
			
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LScalarType);
			
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

		public override object InternalExecute(Program AProgram)
		{
			Schema.Operator LOperator = FindOperator(AProgram.Plan, FAlterOperatorStatement.OperatorSpecifier);
			AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportOperatorChange(AProgram.ServerProcess, LOperator);
				
			if (FAlterOperatorStatement.Block.AlterClassDefinition != null)
			{
				CheckNoDependents(AProgram, LOperator);
				
				AProgram.CatalogDeviceSession.AlterClassDefinition(LOperator.Block.ClassDefinition, FAlterOperatorStatement.Block.AlterClassDefinition);
			}

			if (FAlterOperatorStatement.Block.Block != null)
			{
				CheckNoDependents(AProgram, LOperator);
					
				Schema.Operator LTempOperator = new Schema.Operator(LOperator.Name);
				AProgram.Plan.PushCreationObject(LTempOperator);
				try
				{
					LTempOperator.Block.BlockNode = Compiler.CompileOperatorBlock(AProgram.Plan, LOperator, FAlterOperatorStatement.Block.Block);
					
					AProgram.CatalogDeviceSession.AlterOperator(LOperator, LTempOperator);
				}
				finally
				{
					AProgram.Plan.PopCreationObject();
				}
			}
				
			AProgram.CatalogDeviceSession.AlterMetaData(LOperator, FAlterOperatorStatement.AlterMetaData);
			AProgram.Catalog.UpdateCacheTimeStamp();
			AProgram.Catalog.UpdatePlanCacheTimeStamp();
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LOperator);
			
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
		
		public override object InternalExecute(Program AProgram)
		{
			Schema.Operator LOperator = FindOperator(AProgram.Plan, FAlterAggregateOperatorStatement.OperatorSpecifier);
			if (!(LOperator is Schema.AggregateOperator))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotAggregateOperator, FAlterAggregateOperatorStatement.OperatorSpecifier.OperatorName);
				
			AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportOperatorChange(AProgram.ServerProcess, LOperator);

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
				CheckNoDependents(AProgram, LAggregateOperator);
				
				AProgram.Plan.PushCreationObject(LAggregateOperator);
				try
				{
					AProgram.Plan.Symbols.PushWindow(0);
					try
					{
						Symbol LResultVar = new Symbol(Keywords.Result, LAggregateOperator.ReturnDataType);
						AProgram.Plan.Symbols.Push(LResultVar);
						
						if (FAlterAggregateOperatorStatement.Initialization.AlterClassDefinition != null)
							AProgram.CatalogDeviceSession.AlterClassDefinition(LAggregateOperator.Initialization.ClassDefinition, FAlterAggregateOperatorStatement.Initialization.AlterClassDefinition);
							
						if (FAlterAggregateOperatorStatement.Initialization.Block != null)
							AProgram.CatalogDeviceSession.AlterOperatorBlockNode(LAggregateOperator.Initialization, Compiler.CompileStatement(AProgram.Plan, FAlterAggregateOperatorStatement.Initialization.Block));
						
						AProgram.Plan.Symbols.PushFrame();
						try
						{
							foreach (Schema.Operand LOperand in LAggregateOperator.Operands)
								AProgram.Plan.Symbols.Push(new Symbol(LOperand.Name, LOperand.DataType, LOperand.Modifier == Modifier.Const));
								
							if (FAlterAggregateOperatorStatement.Aggregation.AlterClassDefinition != null)
								AProgram.CatalogDeviceSession.AlterClassDefinition(LAggregateOperator.Aggregation.ClassDefinition, FAlterAggregateOperatorStatement.Aggregation.AlterClassDefinition);
								
							if (FAlterAggregateOperatorStatement.Aggregation.Block != null)
								AProgram.CatalogDeviceSession.AlterOperatorBlockNode(LAggregateOperator.Aggregation, Compiler.CompileDeallocateFrameVariablesNode(AProgram.Plan, Compiler.CompileStatement(AProgram.Plan, FAlterAggregateOperatorStatement.Aggregation.Block)));
						}
						finally
						{
							AProgram.Plan.Symbols.PopFrame();
						}
						
						if (FAlterAggregateOperatorStatement.Finalization.AlterClassDefinition != null)
							AProgram.CatalogDeviceSession.AlterClassDefinition(LAggregateOperator.Finalization.ClassDefinition, FAlterAggregateOperatorStatement.Finalization.AlterClassDefinition);
							
						if (FAlterAggregateOperatorStatement.Finalization.Block != null)
							AProgram.CatalogDeviceSession.AlterOperatorBlockNode(LAggregateOperator.Finalization, Compiler.CompileDeallocateVariablesNode(AProgram.Plan, Compiler.CompileStatement(AProgram.Plan, FAlterAggregateOperatorStatement.Finalization.Block), LResultVar));
					}
					finally
					{
						AProgram.Plan.Symbols.PopWindow();
					}
				}
				finally
				{
					AProgram.Plan.PopCreationObject();
				}

				LAggregateOperator.DetermineRemotable(AProgram.CatalogDeviceSession);
			}

			AProgram.CatalogDeviceSession.AlterMetaData(LAggregateOperator, FAlterAggregateOperatorStatement.AlterMetaData);
			AProgram.Catalog.UpdateCacheTimeStamp();
			AProgram.Catalog.UpdatePlanCacheTimeStamp();
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LAggregateOperator);

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

		public override object InternalExecute(Program AProgram)
		{
			Schema.CatalogConstraint LConstraint = (Schema.CatalogConstraint)FindObject(AProgram.Plan, FAlterConstraintStatement.ConstraintName);
			
			if (FAlterConstraintStatement.Expression != null)
			{
				Schema.CatalogConstraint LTempConstraint = new Schema.CatalogConstraint(LConstraint.Name);
				LTempConstraint.ConstraintType = LConstraint.ConstraintType;
				AProgram.Plan.PushCreationObject(LTempConstraint);
				try
				{
					LTempConstraint.Node = Compiler.OptimizeNode(AProgram.Plan, Compiler.BindNode(AProgram.Plan, Compiler.CompileBooleanExpression(AProgram.Plan, FAlterConstraintStatement.Expression)));
					
					// Validate the new constraint
					if (LTempConstraint.Enforced && !AProgram.ServerProcess.IsLoading() && AProgram.ServerProcess.IsReconciliationEnabled())
					{
						object LObject;
						try
						{
							LObject = LTempConstraint.Node.Execute(AProgram);
						}
						catch (Exception E)
						{
							throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, LTempConstraint.Name);
						}
						
						if (!(bool)LObject)
							throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, LTempConstraint.Name);
					}
					
					AProgram.ServerProcess.ServerSession.Server.RemoveCatalogConstraintCheck(LConstraint);
					
					AProgram.CatalogDeviceSession.AlterConstraint(LConstraint, LTempConstraint);
				}
				finally
				{
					AProgram.Plan.PopCreationObject();
				}
			}
			
			AProgram.CatalogDeviceSession.AlterMetaData(LConstraint, FAlterConstraintStatement.AlterMetaData);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LConstraint);
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

		public override object InternalExecute(Program AProgram)
		{
			if (FReference == null)
				FReference = (Schema.Reference)FindObject(AProgram.Plan, FAlterReferenceStatement.ReferenceName);
			
			AProgram.CatalogDeviceSession.AlterMetaData(FReference, FAlterReferenceStatement.AlterMetaData);

			FReference.SourceTable.SetShouldReinferReferences(AProgram.CatalogDeviceSession);
			FReference.TargetTable.SetShouldReinferReferences(AProgram.CatalogDeviceSession);

			AProgram.Catalog.UpdatePlanCacheTimeStamp();
			AProgram.Catalog.UpdateDerivationTimeStamp();
			
			AProgram.CatalogDeviceSession.UpdateCatalogObject(FReference);
			
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

		public override object InternalExecute(Program AProgram)
		{
			Schema.ServerLink LServer = (Schema.ServerLink)FindObject(AProgram.Plan, FAlterServerStatement.ServerName);
			
			// TODO: Prevent altering the server link with active sessions? (May not be necessary, the active sessions would just continue to use the current settings)
			AProgram.CatalogDeviceSession.AlterMetaData(LServer, FAlterServerStatement.AlterMetaData);
			LServer.ResetServerLink();
			LServer.ApplyMetaData();
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServer);

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
		
		private Schema.ScalarType FindScalarType(Program AProgram, string AScalarTypeName)
		{
			Schema.IDataType LDataType = Compiler.CompileTypeSpecifier(AProgram.Plan, new ScalarTypeSpecifier(AScalarTypeName));
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

		public override object InternalExecute(Program AProgram)
		{
			Schema.Device LDevice = (Schema.Device)FindObject(AProgram.Plan, FAlterDeviceStatement.DeviceName);
			AProgram.ServerProcess.EnsureDeviceStarted(LDevice);
			
			if (FAlterDeviceStatement.AlterClassDefinition != null)
				AProgram.CatalogDeviceSession.AlterClassDefinition(LDevice.ClassDefinition, FAlterDeviceStatement.AlterClassDefinition, LDevice);

			foreach (DropDeviceScalarTypeMap LDeviceScalarTypeMap in FAlterDeviceStatement.DropDeviceScalarTypeMaps)
			{
				Schema.ScalarType LScalarType = FindScalarType(AProgram, LDeviceScalarTypeMap.ScalarTypeName);
				Schema.DeviceScalarType LDeviceScalarType = LDevice.ResolveDeviceScalarType(AProgram.Plan, LScalarType);
				if (LDeviceScalarType != null)
				{
					Schema.CatalogObjectHeaders LHeaders = AProgram.CatalogDeviceSession.SelectGeneratedObjects(LDeviceScalarType.ID);
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						Schema.DeviceOperator LOperator = AProgram.CatalogDeviceSession.ResolveCatalogObject(LHeaders[LIndex].ID) as Schema.DeviceOperator;
						CheckNoDependents(AProgram, LOperator);
						AProgram.CatalogDeviceSession.DropDeviceOperator(LOperator);
					}
					
					CheckNoDependents(AProgram, LDeviceScalarType);
					AProgram.CatalogDeviceSession.DropDeviceScalarType(LDeviceScalarType);
				}
			}
				
			foreach (DropDeviceOperatorMap LDeviceOperatorMap in FAlterDeviceStatement.DropDeviceOperatorMaps)
			{
				Schema.Operator LOperator = Compiler.ResolveOperatorSpecifier(AProgram.Plan, LDeviceOperatorMap.OperatorSpecifier);
				Schema.DeviceOperator LDeviceOperator = LDevice.ResolveDeviceOperator(AProgram.Plan, LOperator);
				if (LDeviceOperator != null)
				{
					CheckNoDependents(AProgram, LDeviceOperator);
					AProgram.CatalogDeviceSession.DropDeviceOperator(LDeviceOperator);
				}
			}
			
			foreach (AlterDeviceScalarTypeMap LDeviceScalarTypeMap in FAlterDeviceStatement.AlterDeviceScalarTypeMaps)
			{
				Schema.ScalarType LScalarType = FindScalarType(AProgram, LDeviceScalarTypeMap.ScalarTypeName);
				Schema.DeviceScalarType LDeviceScalarType = LDevice.ResolveDeviceScalarType(AProgram.Plan, LScalarType);
				if (LDeviceScalarTypeMap.AlterClassDefinition != null)
					AProgram.CatalogDeviceSession.AlterClassDefinition(LDeviceScalarType.ClassDefinition, LDeviceScalarTypeMap.AlterClassDefinition, LDeviceScalarType);
				AProgram.CatalogDeviceSession.AlterMetaData(LDeviceScalarType, LDeviceScalarTypeMap.AlterMetaData);
				AProgram.CatalogDeviceSession.UpdateCatalogObject(LDeviceScalarType);
			}
				
			foreach (AlterDeviceOperatorMap LDeviceOperatorMap in FAlterDeviceStatement.AlterDeviceOperatorMaps)
			{
				Schema.Operator LOperator = Compiler.ResolveOperatorSpecifier(AProgram.Plan, LDeviceOperatorMap.OperatorSpecifier);
				Schema.DeviceOperator LDeviceOperator = LDevice.ResolveDeviceOperator(AProgram.Plan, LOperator);
				if (LDeviceOperatorMap.AlterClassDefinition != null)
					AProgram.CatalogDeviceSession.AlterClassDefinition(LDeviceOperator.ClassDefinition, LDeviceOperatorMap.AlterClassDefinition, LDeviceOperator);
				AProgram.CatalogDeviceSession.AlterMetaData(LDeviceOperator, LDeviceOperatorMap.AlterMetaData);
				AProgram.CatalogDeviceSession.UpdateCatalogObject(LDeviceOperator);
			}
				
			foreach (DeviceScalarTypeMap LDeviceScalarTypeMap in FAlterDeviceStatement.CreateDeviceScalarTypeMaps)
			{
				Schema.DeviceScalarType LScalarType = Compiler.CompileDeviceScalarTypeMap(AProgram.Plan, LDevice, LDeviceScalarTypeMap);
				if (!AProgram.ServerProcess.InLoadingContext())
				{
					Schema.DeviceScalarType LExistingScalarType = LDevice.ResolveDeviceScalarType(AProgram.Plan, LScalarType.ScalarType);
					if ((LExistingScalarType != null) && LExistingScalarType.IsGenerated)
					{
						CheckNoDependents(AProgram, LExistingScalarType); // TODO: These could be updated to point to the new scalar type
						AProgram.CatalogDeviceSession.DropDeviceScalarType(LExistingScalarType);
					}
				}
				AProgram.CatalogDeviceSession.CreateDeviceScalarType(LScalarType);
				
				if (!AProgram.ServerProcess.InLoadingContext())
					Compiler.CompileDeviceScalarTypeMapOperatorMaps(AProgram.Plan, LDevice, LScalarType);
			}
				
			foreach (DeviceOperatorMap LDeviceOperatorMap in FAlterDeviceStatement.CreateDeviceOperatorMaps)
			{
				Schema.DeviceOperator LDeviceOperator = Compiler.CompileDeviceOperatorMap(AProgram.Plan, LDevice, LDeviceOperatorMap);
				if (!AProgram.ServerProcess.InLoadingContext())
				{
					Schema.DeviceOperator LExistingDeviceOperator = LDevice.ResolveDeviceOperator(AProgram.Plan, LDeviceOperator.Operator);
					if ((LExistingDeviceOperator != null) && LExistingDeviceOperator.IsGenerated)
					{
						CheckNoDependents(AProgram, LExistingDeviceOperator); // TODO: These could be updated to point to the new operator
						AProgram.CatalogDeviceSession.DropDeviceOperator(LExistingDeviceOperator);
					}
				}
				
				AProgram.CatalogDeviceSession.CreateDeviceOperator(LDeviceOperator);
			}
				
			if (FAlterDeviceStatement.ReconciliationSettings != null)
			{
				if (FAlterDeviceStatement.ReconciliationSettings.ReconcileModeSet)
					AProgram.CatalogDeviceSession.SetDeviceReconcileMode(LDevice, FAlterDeviceStatement.ReconciliationSettings.ReconcileMode);
				
				if (FAlterDeviceStatement.ReconciliationSettings.ReconcileMasterSet)
					AProgram.CatalogDeviceSession.SetDeviceReconcileMaster(LDevice, FAlterDeviceStatement.ReconciliationSettings.ReconcileMaster);
			}
				
			AProgram.CatalogDeviceSession.AlterMetaData(LDevice, FAlterDeviceStatement.AlterMetaData);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LDevice);

			return null;
		}
    }
    
	public abstract class DropObjectNode : DDLNode
	{
		protected void CheckNotSystem(Program AProgram, Schema.Object AObject)
		{
			if (AObject.IsSystem && !AProgram.Plan.User.IsSystemUser())
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
		
		protected void DropSourceReferences(Program AProgram, Schema.TableVar ATableVar)
		{
			BlockNode LBlockNode = new BlockNode();
			foreach (Schema.Reference LReference in ATableVar.SourceReferences)
				if (LReference.ParentReference == null)
					LBlockNode.Nodes.Add(Compiler.Compile(AProgram.Plan, LReference.EmitDropStatement(EmitMode.ForCopy)));
				
			LBlockNode.Execute(AProgram);
		}
		
		protected void DropEventHandlers(Program AProgram, Schema.TableVar ATableVar)
		{
			BlockNode LBlockNode = new BlockNode();
			List<Schema.DependentObjectHeader> LHeaders = AProgram.CatalogDeviceSession.SelectObjectDependents(ATableVar.ID, false);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				Schema.DependentObjectHeader LHeader = LHeaders[LIndex];
				if ((LHeader.ObjectType == "TableVarEventHandler") || (LHeader.ObjectType == "TableVarColumnEventHandler"))
					LBlockNode.Nodes.Add(Compiler.Compile(AProgram.Plan, ((Schema.EventHandler)AProgram.CatalogDeviceSession.ResolveObject(LHeader.ID)).EmitDropStatement(EmitMode.ForCopy)));
			}
			
			LBlockNode.Execute(AProgram);
		}
		
		public static void RemoveDeferredConstraintChecks(Program AProgram, Schema.TableVar ATableVar)
		{
			AProgram.ServerProcess.ServerSession.Server.RemoveDeferredConstraintChecks(ATableVar);
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
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				CheckNotSystem(AProgram, FTable);
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, FTable);
				DropSourceReferences(AProgram, FTable);
				DropEventHandlers(AProgram, FTable);
				CheckNoDependents(AProgram, FTable);

				RemoveDeferredConstraintChecks(AProgram, FTable);

				AProgram.CatalogDeviceSession.DropTable(FTable);

				if (ShouldAffectDerivationTimeStamp)
				{
					AProgram.Catalog.UpdateCacheTimeStamp();
					AProgram.Catalog.UpdatePlanCacheTimeStamp();
					AProgram.Catalog.UpdateDerivationTimeStamp();
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
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				CheckNotSystem(AProgram, FDerivedTableVar);
				AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(AProgram.ServerProcess, FDerivedTableVar);
				DropSourceReferences(AProgram, FDerivedTableVar);
				DropEventHandlers(AProgram, FDerivedTableVar);
				CheckNoDependents(AProgram, FDerivedTableVar);

				RemoveDeferredConstraintChecks(AProgram, FDerivedTableVar);

				AProgram.CatalogDeviceSession.DropView(FDerivedTableVar);

				if (ShouldAffectDerivationTimeStamp)
				{
					AProgram.Catalog.UpdateCacheTimeStamp();
					AProgram.Catalog.UpdatePlanCacheTimeStamp();
					AProgram.Catalog.UpdateDerivationTimeStamp();
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
		
		private void DropChildObjects(Program AProgram, Schema.ScalarType AScalarType)
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
				
			Compiler.Compile(AProgram.Plan, LStatement).Execute(AProgram);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				CheckNotSystem(AProgram, FScalarType);
				
				DropChildObjects(AProgram, FScalarType);

				// If the ScalarType has dependents, prevent its destruction
				DropGeneratedDependents(AProgram, FScalarType);
				CheckNoDependents(AProgram, FScalarType);
				
				AProgram.CatalogDeviceSession.DropScalarType(FScalarType);
				AProgram.Catalog.OperatorResolutionCache.Clear(FScalarType, FScalarType);					
				
				AProgram.Catalog.UpdateCacheTimeStamp();
				AProgram.Catalog.UpdatePlanCacheTimeStamp();
				AProgram.Catalog.UpdateDerivationTimeStamp();
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
		
		public override object InternalExecute(Program AProgram)
		{
			if (FDropOperator == null)
				FDropOperator = Compiler.ResolveOperatorSpecifier(AProgram.Plan, FOperatorSpecifier);
				
			CheckNotSystem(AProgram, FDropOperator);
			AProgram.ServerProcess.ServerSession.Server.ATDevice.ReportOperatorChange(AProgram.ServerProcess, FDropOperator);

			DropGeneratedSorts(AProgram, FDropOperator);
			CheckNoDependents(AProgram, FDropOperator);
			
			AProgram.CatalogDeviceSession.DropOperator(FDropOperator);
			
			if (ShouldAffectDerivationTimeStamp)
			{
				AProgram.Catalog.UpdateCacheTimeStamp();
				AProgram.Catalog.UpdatePlanCacheTimeStamp();
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
		
		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				CheckNotSystem(AProgram, FConstraint);
				CheckNoDependents(AProgram, FConstraint);
					
				AProgram.ServerProcess.ServerSession.Server.RemoveCatalogConstraintCheck(FConstraint);
				
				AProgram.CatalogDeviceSession.DropConstraint(FConstraint);

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
		
		public override object InternalExecute(Program AProgram)
		{
			if (FReference == null)
				FReference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(AProgram.Plan, FReferenceName);

			lock (AProgram.Catalog)
			{
				CheckNotSystem(AProgram, FReference);
				CheckNoDependents(AProgram, FReference);
				
				AProgram.CatalogDeviceSession.DropReference(FReference);

				AProgram.Catalog.UpdateCacheTimeStamp();
				AProgram.Catalog.UpdatePlanCacheTimeStamp();
				AProgram.Catalog.UpdateDerivationTimeStamp();
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

		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				// TODO: Prevent dropping when server sessions are active
				// TODO: Drop server link users
				
				CheckNotSystem(AProgram, FServerLink);
				CheckNoDependents(AProgram, FServerLink);
				
				AProgram.CatalogDeviceSession.DeleteCatalogObject(FServerLink);
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

		public override object InternalExecute(Program AProgram)
		{
			lock (AProgram.Catalog)
			{
				AProgram.CatalogDeviceSession.StopDevice(FDropDevice);
				
				CheckNotSystem(AProgram, FDropDevice);
				CheckNoBaseTableVarDependents(AProgram, FDropDevice);

				DropDeviceMaps(AProgram, FDropDevice);				
				CheckNoDependents(AProgram, FDropDevice);
				
				AProgram.CatalogDeviceSession.DropDevice(FDropDevice);

				return null;
			}
		}
	}
}
