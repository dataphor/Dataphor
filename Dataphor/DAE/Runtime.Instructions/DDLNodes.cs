/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Schema = Alphora.Dataphor.DAE.Schema;

	public abstract class DDLNode : PlanNode 
	{
		protected void DropDeviceMaps(Program program, Schema.Device device)
		{
			List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(device.ID, false);
			List<string> dependents = new List<string>();
			for (int index = 0; index < headers.Count; index++)
			{
				if ((headers[index].ObjectType == "DeviceScalarType") || (headers[index].ObjectType == "DeviceOperator"))
					dependents.Add(headers[index].Name);
			}
			
			if (dependents.Count > 0)
			{
				string[] dependentNames = new string[dependents.Count];
				dependents.CopyTo(dependentNames, 0);
				Compiler.Compile(program.Plan, program.Catalog.EmitDropStatement(program.CatalogDeviceSession, dependentNames, String.Empty, false, true, false, true)).Execute(program);
			}
		}
		
		protected void DropGeneratedObjects(Program program, Schema.Object objectValue)
		{
			List<Schema.CatalogObjectHeader> headers = program.CatalogDeviceSession.SelectGeneratedObjects(objectValue.ID);
			
			if (headers.Count > 0)
			{
				string[] objectNames = new string[headers.Count];
				for (int index = 0; index < headers.Count; index++)
					objectNames[index] = headers[index].Name;
					
				Compiler.Compile(program.Plan, program.Catalog.EmitDropStatement(program.CatalogDeviceSession, objectNames, String.Empty, false, true, false, true)).Execute(program);
			}
		}
		
		protected void DropGeneratedDependents(Program program, Schema.Object objectValue)
		{
			List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(objectValue.ID, false);
			List<string> deviceMaps = new List<string>(); // Device maps need to be dropped first, because the dependency of a device map on a generated operator will be reported as a dependency on the generator, not the operator
			List<string> dependents = new List<string>();
			for (int index = 0; index < headers.Count; index++)
			{
				if (headers[index].IsATObject || headers[index].IsSessionObject || headers[index].IsGenerated)
				{
					if (headers[index].ResolveObject(program.CatalogDeviceSession) is Schema.DeviceObject)
						deviceMaps.Add(headers[index].Name);
					else
						dependents.Add(headers[index].Name);
				}
			}
			
			if (deviceMaps.Count > 0)
			{
				string[] deviceMapNames = new string[deviceMaps.Count];
				deviceMaps.CopyTo(deviceMapNames, 0);
				PlanNode node = Compiler.Compile(program.Plan, program.Catalog.EmitDropStatement(program.CatalogDeviceSession, deviceMapNames, String.Empty, false, true, true, true));
				node.Execute(program);
			}
						
			if (dependents.Count > 0)
			{
				string[] dependentNames = new string[dependents.Count];
				dependents.CopyTo(dependentNames, 0);
				PlanNode node = Compiler.Compile(program.Plan, program.Catalog.EmitDropStatement(program.CatalogDeviceSession, dependentNames, String.Empty, false, true, true, true));
				node.Execute(program);
			}
		}
		
		protected void DropGeneratedSorts(Program program, Schema.Object objectValue)
		{
			List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(objectValue.ID, false);
			List<string> dependents = new List<string>();
			for (int index = 0; index < headers.Count; index++)
			{
				if ((headers[index].IsATObject || headers[index].IsSessionObject || headers[index].IsGenerated) && (headers[index].ObjectType == "Sort"))
					dependents.Add(headers[index].Name);
			}
						
			if (dependents.Count > 0)
			{
				string[] dependentNames = new string[dependents.Count];
				dependents.CopyTo(dependentNames, 0);
				Compiler.Compile(program.Plan, program.Catalog.EmitDropStatement(program.CatalogDeviceSession, dependentNames, String.Empty, false, true, false, true)).Execute(program);
			}
		}
		
		protected void CheckNoDependents(Program program, Schema.Object objectValue)
		{
			if (!program.ServerProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(objectValue.ID, false);
				List<string> dependents = new List<string>();
				for (int index = 0; index < headers.Count; index++)
				{
					dependents.Add(headers[index].Description);
					if (dependents.Count >= 5)
						break;
				}

				if (dependents.Count > 0)
					throw new CompilerException(CompilerException.Codes.ObjectHasDependents, objectValue.Name, ExceptionUtility.StringsToCommaList(dependents));
			}
		}
		
		protected void CheckNoBaseTableVarDependents(Program program, Schema.Object objectValue)
		{
			if (!program.ServerProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(objectValue.ID, false);
				List<string> baseTableVarDependents = new List<string>();
				for (int index = 0; index < headers.Count; index++)
				{
					Schema.DependentObjectHeader header = headers[index];
					if (header.ObjectType == "BaseTableVar")
					{
						baseTableVarDependents.Add(header.Description);
						if (baseTableVarDependents.Count >= 5)
							break;
					}
				}

				if (baseTableVarDependents.Count > 0)
					throw new CompilerException(CompilerException.Codes.ObjectHasDependents, objectValue.Name, ExceptionUtility.StringsToCommaList(baseTableVarDependents));
			}
		}

		protected void CheckNoOperatorDependents(Program program, Schema.Object objectValue)
		{
			if (!program.ServerProcess.IsLoading())
			{
				List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(objectValue.ID, false);
				List<string> operatorDependents = new List<string>();
				for (int index = 0; index < headers.Count; index++)
				{
					Schema.DependentObjectHeader header = headers[index];
					if ((header.ObjectType == "Operator") || (header.ObjectType == "AggregateOperator"))
					{
						operatorDependents.Add(header.Description);
						if (operatorDependents.Count >= 5)
							break;
					}
				}

				if (operatorDependents.Count > 0)
					throw new CompilerException(CompilerException.Codes.ObjectHasDependents, objectValue.Name, ExceptionUtility.StringsToCommaList(operatorDependents));
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
		public CreateTableNode(Schema.BaseTableVar table) : base()
		{
			_table = table;
			//_device = _table.Device;
			//_deviceSupported = false;
		}
		
		// Table
		protected Schema.BaseTableVar _table;
		public Schema.BaseTableVar Table
		{
			get { return _table; }
			set { _table = value; }
		}
		
		public override Schema.TableVar TableVar { get { return _table; } }
		
		public override void DeterminePotentialDevice(Plan plan)
		{
			_potentialDevice = _table.Device;
		}

		public override void DetermineDevice(Plan plan)
		{
			base.DetermineDevice(plan);
			DeviceSupported = false;
		}

		public override void BindToProcess(Plan plan)
		{
			if (_device != null)
				plan.CheckRight(_device.GetRight(Schema.RightNames.CreateStore));
			plan.CheckRight(Schema.RightNames.CreateTable);
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.CreateTable(_table);
			return null;
		}
    }
    
    public class CreateViewNode : CreateTableVarNode
    {
		// View
		protected Schema.DerivedTableVar _view;
		public Schema.DerivedTableVar View
		{
			get { return _view; }
			set { _view = value; }
		}
		
		public override Schema.TableVar TableVar { get { return _view; } }
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateView);
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			if (_view.InvocationExpression == null)
				Error.Fail("Derived table variable invocation expression reference is null");
				
			program.CatalogDeviceSession.CreateView(_view);
			return null;
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			base.WritePlanAttributes(writer);
			#if USEORIGINALEXPRESSION
			AWriter.WriteAttributeString("Expression", new D4TextEmitter().Emit(FView.OriginalExpression));
			#else
			writer.WriteAttributeString("Expression", new D4TextEmitter().Emit(_view.InvocationExpression));
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
		public CreateConversionNode(Schema.Conversion conversion) : base()
		{
			Conversion = conversion;
		}
		
		public Schema.Conversion Conversion;
		
		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.CreateConversion(Conversion);
			return null;
		}
    }
    
    public class DropConversionNode : DropObjectNode
    {
		public Schema.ScalarType SourceScalarType;
		public Schema.ScalarType TargetScalarType;
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				foreach (Schema.Conversion conversion in SourceScalarType.ImplicitConversions)
					if (conversion.TargetScalarType.Equals(TargetScalarType))
					{
						CheckNotSystem(program, conversion);
						DropGeneratedSorts(program, conversion);
						CheckNoDependents(program, conversion);
						
						program.CatalogDeviceSession.DropConversion(conversion);
						break;
					}
				
			}
			return null;
		}
    }
    
    public class CreateSortNode : DDLNode
    {
		private Schema.ScalarType _scalarType;
		public Schema.ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}
		
		private Schema.Sort _sort;
		public Schema.Sort Sort
		{
			get { return _sort; }
			set { _sort = value; }
		}
		
		private bool _isUnique;
		public bool IsUnique
		{
			get { return _isUnique; }
			set { _isUnique = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				program.CatalogDeviceSession.CreateSort(_sort);
				program.CatalogDeviceSession.AttachSort(_scalarType, _sort, _isUnique);
				program.CatalogDeviceSession.UpdateCatalogObject(_scalarType);
				return null;
			}
		}
    }
    
    public class AlterSortNode : DDLNode
    {
		private Schema.ScalarType _scalarType;
		public Schema.ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}
		
		private Schema.Sort _sort;
		public Schema.Sort Sort
		{
			get { return _sort; }
			set { _sort = value; }
		}

		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				if (_scalarType.Sort != null)
				{
					Schema.Sort sort = _scalarType.Sort;
					program.CatalogDeviceSession.DetachSort(_scalarType, sort, false);
					program.CatalogDeviceSession.DropSort(sort);
				}
				program.CatalogDeviceSession.CreateSort(_sort);
				program.CatalogDeviceSession.AttachSort(_scalarType, _sort, false);
				program.CatalogDeviceSession.UpdateCatalogObject(_scalarType);
				return null;
			}
		}
    }
    
    public class DropSortNode : DropObjectNode
    {
		private Schema.ScalarType _scalarType;
		public Schema.ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}
		
		private bool _isUnique;
		public bool IsUnique
		{
			get { return _isUnique; }
			set { _isUnique = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				Schema.Sort sort = _isUnique ? _scalarType.UniqueSort : _scalarType.Sort;
				if (sort != null)
				{
					CheckNotSystem(program, sort);
					CheckNoDependents(program, sort);
					
					program.CatalogDeviceSession.DetachSort(_scalarType, sort, _isUnique);
					program.CatalogDeviceSession.DropSort(sort);
				}
				program.CatalogDeviceSession.UpdateCatalogObject(_scalarType);
				return null;
			}
		}
    }
    
    public class CreateRoleNode : DDLNode
    {
		private Schema.Role _role;
		public Schema.Role Role
		{
			get { return _role; }
			set { _role = value; }
		}

		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.InsertRole(_role);
			return null;
		}
    }
    
    public class AlterRoleNode : AlterNode
    {
		private Schema.Role _role;
		public Schema.Role Role
		{
			get { return _role; }
			set { _role = value; }
		}
		
		private AlterRoleStatement _statement;
		public AlterRoleStatement Statement
		{
			get { return _statement; }
			set { _statement = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.AlterMetaData(_role, _statement.AlterMetaData);
			program.CatalogDeviceSession.UpdateCatalogObject(_role);
			return null;
		}
    }
    
    public class DropRoleNode : DropObjectNode
    {
		private Schema.Role _role;
		public Schema.Role Role
		{
			get { return _role; }
			set { _role = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.DeleteRole(_role);
			return null;
		}
    }
    
    public class CreateRightNode : DDLNode
    {
		private string _rightName;
		public string RightName
		{
			get { return _rightName; }
			set { _rightName = value; }
		}

		public static void CreateRight(Program program, string rightName, string userID)
		{
			Schema.User user = program.CatalogDeviceSession.ResolveUser(userID);
			if (user.ID != program.Plan.User.ID)
				program.Plan.CheckRight(Schema.RightNames.AlterUser);
			
			if (program.CatalogDeviceSession.RightExists(rightName))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateRightName, rightName);
				
			program.CatalogDeviceSession.InsertRight(rightName, user.ID);
		}

		public override object InternalExecute(Program program)
		{
			CreateRight(program, _rightName, program.User.ID);
			return null;
		}
    }
    
    public class DropRightNode : DDLNode
    {
		private string _rightName;
		public string RightName
		{
			get { return _rightName; }
			set { _rightName = value; }
		}

		public static void DropRight(Program program, string rightName)
		{
			Schema.Right right = program.CatalogDeviceSession.ResolveRight(rightName);
			if (right.OwnerID != program.Plan.User.ID)
				if ((right.OwnerID == Server.Engine.SystemUserID) || (program.Plan.User.ID != Server.Engine.AdminUserID))
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.Plan.User.ID);
			if (right.IsGenerated)
				throw new ServerException(ServerException.Codes.CannotDropGeneratedRight, right.Name);
				
			program.CatalogDeviceSession.DeleteRight(rightName);
		}
		
		public override object InternalExecute(Program program)
		{
			DropRight(program, _rightName);
			return null;
		}
    }

    public class CreateScalarTypeNode : CreateObjectNode
    {
		// ScalarType
		protected Schema.ScalarType _scalarType;
		public Schema.ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}

		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateType);

			if ((_scalarType.ClassDefinition != null) && !_scalarType.IsDefaultConveyor)
				plan.CheckRight(Schema.RightNames.HostImplementation);

			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.CreateScalarType(_scalarType);
			return null;
		}
    }
    
    public class CreateOperatorNode : CreateObjectNode
    {
		public CreateOperatorNode() : base(){}
		public CreateOperatorNode(Schema.Operator operatorValue) : base()
		{
			_createOperator = operatorValue;
		}
		
		// Operator
		protected Schema.Operator _createOperator;
		public Schema.Operator CreateOperator
		{
			get { return _createOperator; }
			set { _createOperator = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateOperator);
			
			// Only check host implementation rights for non-generated operators
			if (!_createOperator.IsGenerated)
			{
				Schema.AggregateOperator aggregateOperator = _createOperator as Schema.AggregateOperator;
				if (aggregateOperator != null)
				{
					if (aggregateOperator.Initialization.ClassDefinition != null)
						plan.CheckRight(Schema.RightNames.HostImplementation);

					if (aggregateOperator.Aggregation.ClassDefinition != null)
						plan.CheckRight(Schema.RightNames.HostImplementation);

					if (aggregateOperator.Finalization.ClassDefinition != null)
						plan.CheckRight(Schema.RightNames.HostImplementation);
				}
				else
				{
					if (_createOperator.Block.ClassDefinition != null)
						plan.CheckRight(Schema.RightNames.HostImplementation);
				}
			}

			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.CreateOperator(_createOperator);
			return null;
		}
    }
    
    public class CreateConstraintNode : CreateObjectNode
    {		
		protected Schema.CatalogConstraint _constraint;
		public Schema.CatalogConstraint Constraint
		{
			get { return _constraint; }
			set { _constraint = value; }
		}
		
		// For each retrieve in the constraint,
		//		add the constraint to the list of catalog constraints to be checked
		//		when the table being retrieved is affected.
		public static void AttachConstraint(Schema.CatalogConstraint constraint, PlanNode node)
		{
			BaseTableVarNode baseTableVarNode = node as BaseTableVarNode;
			if ((baseTableVarNode != null) && !baseTableVarNode.TableVar.CatalogConstraints.ContainsName(constraint.Name))
				baseTableVarNode.TableVar.CatalogConstraints.Add(constraint);
			else
			{
				foreach (PlanNode localNode in node.Nodes)
					AttachConstraint(constraint, localNode);
			}
		}
		
		public static void DetachConstraint(Schema.CatalogConstraint constraint, PlanNode node)
		{
			BaseTableVarNode baseTableVarNode = node as BaseTableVarNode;
			if (baseTableVarNode != null)
				baseTableVarNode.TableVar.CatalogConstraints.SafeRemove(constraint);
			else
			{
				foreach (PlanNode localNode in node.Nodes)
					DetachConstraint(constraint, localNode);
			}
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateConstraint);
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			if (_constraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
			{
				object objectValue;

				try
				{
					objectValue = _constraint.Node.Execute(program);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, _constraint.Name);
				}
				
				if ((objectValue != null) && !(bool)objectValue)
					throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, _constraint.Name);
			}
			
			program.CatalogDeviceSession.CreateConstraint(_constraint);
			return null;
		}
    }
    
    public class CreateReferenceNode : CreateObjectNode
    {		
		public CreateReferenceNode() : base(){}
		public CreateReferenceNode(Schema.Reference reference) : base()
		{
			_reference = reference;
		}
		
		protected Schema.Reference _reference;
		public Schema.Reference Reference
		{
			get { return _reference; }
			set { _reference = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateReference);
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			// Validate the catalog level enforcement constraint
			if (!program.ServerProcess.ServerSession.Server.IsEngine && _reference.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
			{
				object objectValue;

				try
				{
					objectValue = _reference.CatalogConstraint.Node.Execute(program);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, _reference.Name);
				}
				
				if ((objectValue != null) && !(bool)objectValue)
					throw new RuntimeException(RuntimeException.Codes.ReferenceConstraintViolation, _reference.Name);
			}
			
			program.CatalogDeviceSession.CreateReference(_reference);
			program.Catalog.UpdatePlanCacheTimeStamp();
			program.Catalog.UpdateDerivationTimeStamp();
			
			return null;
		}
    }

    public class CreateServerNode : CreateObjectNode
    {
		protected Schema.ServerLink _serverLink;
		public Schema.ServerLink ServerLink
		{
			get { return _serverLink; }
			set { _serverLink = value; }
		}

		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateServer);
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			_serverLink.ApplyMetaData();
			program.CatalogDeviceSession.InsertCatalogObject(_serverLink);
			return null;
		}
    }
    
    public class CreateDeviceNode : CreateObjectNode
    {
		private Schema.Device _newDevice;
		public Schema.Device NewDevice
		{
			get { return _newDevice; }
			set { _newDevice = value; }
		}

		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(Schema.RightNames.CreateDevice);
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			program.CatalogDeviceSession.CreateDevice(_newDevice);
			return null;
		}
    }
    
    public class CreateEventHandlerNode : CreateObjectNode
    {
		private Schema.EventHandler _eventHandler;
		public Schema.EventHandler EventHandler
		{
			get { return _eventHandler; }
			set { _eventHandler = value; }
		}
		
		private Schema.Object _eventSource;
		public Schema.Object EventSource
		{
			get { return _eventSource; }
			set { _eventSource = value; }
		}
		
		private int _eventSourceColumnIndex = -1;
		public int EventSourceColumnIndex
		{
			get { return _eventSourceColumnIndex; }
			set { _eventSourceColumnIndex = value; }
		}

		private List<string> _beforeOperatorNames;
		public List<string> BeforeOperatorNames
		{
			get { return _beforeOperatorNames; }
			set { _beforeOperatorNames = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			Schema.TableVar tableVar = _eventSource as Schema.TableVar;
			if ((tableVar != null) && (!program.ServerProcess.InLoadingContext()))
				program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, tableVar);
			program.CatalogDeviceSession.CreateEventHandler(_eventHandler, _eventSource, _eventSourceColumnIndex, _beforeOperatorNames);
			return null;
		}
    }
    
    public class AlterEventHandlerNode : DDLNode
    {
		public AlterEventHandlerNode() : base(){}
		public AlterEventHandlerNode(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex) : base()
		{
			_eventHandler = eventHandler;
			_eventSource = eventSource;
			_eventSourceColumnIndex = eventSourceColumnIndex;
		}
		
		private Schema.EventHandler _eventHandler;
		public Schema.EventHandler EventHandler
		{
			get { return _eventHandler; }
			set { _eventHandler = value; }
		}
		
		private Schema.Object _eventSource;
		public Schema.Object EventSource
		{
			get { return _eventSource; }
			set { _eventSource = value; }
		}
		
		private int _eventSourceColumnIndex = -1;
		public int EventSourceColumnIndex
		{
			get { return _eventSourceColumnIndex; }
			set { _eventSourceColumnIndex = value; }
		}

		private List<string> _beforeOperatorNames;
		public List<string> BeforeOperatorNames
		{
			get { return _beforeOperatorNames; }
			set { _beforeOperatorNames = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			Schema.TableVar tableVar = _eventSource as Schema.TableVar;
			if ((tableVar != null) && (!program.ServerProcess.InLoadingContext()))
				program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, tableVar);
			program.CatalogDeviceSession.AlterEventHandler(_eventHandler, _eventSource, _eventSourceColumnIndex, _beforeOperatorNames);
			program.CatalogDeviceSession.UpdateCatalogObject(_eventHandler);
			return null;
		}
    }
    
    public class DropEventHandlerNode : DDLNode
    {
		public DropEventHandlerNode() : base(){}
		public DropEventHandlerNode(Schema.EventHandler eventHandler, Schema.Object eventSource, int eventSourceColumnIndex) : base()
		{
			_eventHandler = eventHandler;
			_eventSource = eventSource;
			_eventSourceColumnIndex = eventSourceColumnIndex;
		}
		
		private Schema.EventHandler _eventHandler;
		public Schema.EventHandler EventHandler
		{
			get { return _eventHandler; }
			set { _eventHandler = value; }
		}
		
		private Schema.Object _eventSource;
		public Schema.Object EventSource
		{
			get { return _eventSource; }
			set { _eventSource = value; }
		}
		
		private int _eventSourceColumnIndex = -1;
		public int EventSourceColumnIndex
		{
			get { return _eventSourceColumnIndex; }
			set { _eventSourceColumnIndex = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			Schema.TableVar tableVar = _eventSource as Schema.TableVar;
			if ((tableVar != null) && (!program.ServerProcess.InLoadingContext()))
				program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, tableVar);

			if (_eventHandler.IsDeferred)
				program.ServerProcess.ServerSession.Server.RemoveDeferredHandlers(_eventHandler);

			program.CatalogDeviceSession.DropEventHandler(_eventHandler, _eventSource, _eventSourceColumnIndex);
			return null;
		}
    }
    
    public abstract class AlterNode : DDLNode
    {
		protected virtual Schema.Object FindObject(Plan plan, string objectName)
		{
			return Compiler.ResolveCatalogIdentifier(plan, objectName, true);
		}

		public static void AlterMetaData(Schema.Object objectValue, AlterMetaData alterMetaData)
		{
			AlterMetaData(objectValue, alterMetaData, false);
		}
		
		public static void AlterMetaData(Schema.Object objectValue, AlterMetaData alterMetaData, bool optimistic)
		{
			if (alterMetaData != null)
			{
				if (objectValue.MetaData == null)
					objectValue.MetaData = new MetaData();
					
				if (optimistic)
				{
					objectValue.MetaData.Tags.SafeRemoveRange(alterMetaData.DropTags);
					objectValue.MetaData.Tags.AddOrUpdateRange(alterMetaData.AlterTags);
					objectValue.MetaData.Tags.AddOrUpdateRange(alterMetaData.CreateTags);
				}
				else
				{
					objectValue.MetaData.Tags.RemoveRange(alterMetaData.DropTags);
					objectValue.MetaData.Tags.UpdateRange(alterMetaData.AlterTags);
					objectValue.MetaData.Tags.AddRange(alterMetaData.CreateTags);
				}
			}
		}
		
		public static void AlterClassDefinition(ClassDefinition classDefinition, AlterClassDefinition alterClassDefinition)
		{
			AlterClassDefinition(classDefinition, alterClassDefinition, null);
		}
		
		public static void AlterClassDefinition(ClassDefinition classDefinition, AlterClassDefinition alterClassDefinition, object instance)
		{
			if (alterClassDefinition != null)
			{
				if (alterClassDefinition.ClassName != String.Empty)
				{
					if (instance != null)
						throw new RuntimeException(RuntimeException.Codes.UnimplementedAlterCommand, String.Format("Class name for objects of type {0}", instance.GetType().Name));

					classDefinition.ClassName = alterClassDefinition.ClassName;
				}
					
				foreach (ClassAttributeDefinition attributeDefinition in alterClassDefinition.DropAttributes)
					classDefinition.Attributes.RemoveAt(classDefinition.Attributes.IndexOf(attributeDefinition.AttributeName));
					
				foreach (ClassAttributeDefinition attributeDefinition in alterClassDefinition.AlterAttributes)
				{
					if (instance != null)
						ClassLoader.SetProperty(instance, attributeDefinition.AttributeName, attributeDefinition.AttributeValue);
					
					classDefinition.Attributes[attributeDefinition.AttributeName].AttributeValue = attributeDefinition.AttributeValue;
				}
					
				foreach (ClassAttributeDefinition attributeDefinition in alterClassDefinition.CreateAttributes)
				{
					if (instance != null)
						ClassLoader.SetProperty(instance, attributeDefinition.AttributeName, attributeDefinition.AttributeValue);

					classDefinition.Attributes.Add(attributeDefinition);
				}
			}
		}
    }
    
    public abstract class AlterTableVarNode : AlterNode
    {		
		// AlterTableVarStatement
		protected AlterTableVarStatement _alterTableVarStatement;
		public virtual AlterTableVarStatement AlterTableVarStatement
		{
			get { return _alterTableVarStatement; }
			set { _alterTableVarStatement = value; }
		}
		
		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		protected void DropKeys(Plan plan, Program program, Schema.TableVar tableVar, DropKeyDefinitions dropKeys)
		{
			if (dropKeys.Count > 0)
				CheckNoDependents(program, tableVar);

			foreach (DropKeyDefinition keyDefinition in dropKeys)
			{
				Schema.Key oldKey = Compiler.FindKey(plan, tableVar, keyDefinition);

				if (oldKey.IsInherited)
					throw new CompilerException(CompilerException.Codes.InheritedObject, oldKey.Name);
				
				plan.CatalogDeviceSession.DropKey(tableVar, oldKey);
			}
		}
		
		protected void AlterKeys(Plan plan, Program program, Schema.TableVar tableVar, AlterKeyDefinitions alterKeys)
		{
			foreach (AlterKeyDefinition keyDefinition in alterKeys)
			{
				Schema.Key oldKey = Compiler.FindKey(plan, tableVar, keyDefinition);
					
				program.CatalogDeviceSession.AlterMetaData(oldKey, keyDefinition.AlterMetaData);
			}
		}

		protected void CreateKeys(Plan plan, Program program, Schema.TableVar tableVar, KeyDefinitions createKeys)
		{
			foreach (KeyDefinition keyDefinition in createKeys)
			{
				Schema.Key newKey = Compiler.CompileKeyDefinition(plan, tableVar, keyDefinition);
				if (!tableVar.Keys.Contains(newKey))
				{
					if (newKey.Enforced)
					{
						// Validate that the key can be created
						Compiler.CompileCatalogConstraintForKey(plan, tableVar, newKey).Validate(program);

						newKey.Constraint = Compiler.CompileKeyConstraint(plan, tableVar, newKey);
					}
					program.CatalogDeviceSession.CreateKey(tableVar, newKey);
				}
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, newKey.Name);
			}
		}
		
		protected void DropOrders(Plan plan, Program program, Schema.TableVar tableVar, DropOrderDefinitions dropOrders)
		{
			if (dropOrders.Count > 0)
				CheckNoDependents(program, tableVar);

			foreach (DropOrderDefinition orderDefinition in dropOrders)
			{
				
				Schema.Order oldOrder = Compiler.FindOrder(plan, tableVar, orderDefinition);
				if (oldOrder.IsInherited)
					throw new CompilerException(CompilerException.Codes.InheritedObject, oldOrder.Name);
					
				program.CatalogDeviceSession.DropOrder(tableVar, oldOrder);
			}
		}
		
		protected void AlterOrders(Plan plan, Program program, Schema.TableVar tableVar, AlterOrderDefinitions alterOrders)
		{
			foreach (AlterOrderDefinition orderDefinition in alterOrders)
				program.CatalogDeviceSession.AlterMetaData(Compiler.FindOrder(plan, tableVar, orderDefinition), orderDefinition.AlterMetaData);
		}

		protected void CreateOrders(Plan plan, Program program, Schema.TableVar tableVar, OrderDefinitions createOrders)
		{
			foreach (OrderDefinition orderDefinition in createOrders)
			{
				Schema.Order newOrder = Compiler.CompileOrderDefinition(plan, tableVar, orderDefinition, false);
				if (!tableVar.Orders.Contains(newOrder))
					program.CatalogDeviceSession.CreateOrder(tableVar, newOrder);
				else
					throw new Schema.SchemaException(Schema.SchemaException.Codes.DuplicateObjectName, newOrder.Name);
			}
		}

		protected void DropConstraints(Plan plan, Program program, Schema.TableVar tableVar, DropConstraintDefinitions dropConstraints)
		{
			foreach (DropConstraintDefinition constraintDefinition in dropConstraints)
			{
				Schema.TableVarConstraint constraint = tableVar.Constraints[constraintDefinition.ConstraintName];
					
				if (constraintDefinition.IsTransition && (!(constraint is Schema.TransitionConstraint)))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsNotTransitionConstraint, constraint.Name);
					
				if (!constraintDefinition.IsTransition && !(constraint is Schema.RowConstraint))
					throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsTransitionConstraint, constraint.Name);
					
				program.CatalogDeviceSession.DropTableVarConstraint(tableVar, constraint);
			}
		}
		
		protected void ValidateConstraint(Plan plan, Program program, Schema.TableVar tableVar, Schema.TableVarConstraint constraint)
		{
			Schema.RowConstraint rowConstraint = constraint as Schema.RowConstraint;
			if (rowConstraint != null)
			{
				// Ensure that all data in the given table var satisfies the new constraint
				// if exists (table rename new where not (expression)) then raise
				var planNode = 
					Compiler.Compile
					(
						plan, 
						new UnaryExpression
						(
							Instructions.Exists, 
							new RestrictExpression
							(
								new IdentifierExpression(tableVar.Name), 
								new UnaryExpression(Instructions.Not, (Expression)rowConstraint.Node.EmitStatement(EmitMode.ForCopy))
							)
						)
					);

				object objectValue;
				try
				{
					objectValue = planNode.Execute(program);
				}
				catch (Exception E)
				{
					throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, constraint.Name);
				}
				
				if ((objectValue != null) && (bool)objectValue)
					throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, constraint.Name);
			}
		}
		
		protected void AlterConstraints(Plan plan, Program program, Schema.TableVar tableVar, AlterConstraintDefinitions alterConstraints)
		{
			foreach (AlterConstraintDefinitionBase constraintDefinition in alterConstraints)
			{
				Schema.TableVarConstraint oldConstraint = tableVar.Constraints[constraintDefinition.ConstraintName];
				
				if (constraintDefinition is AlterConstraintDefinition)
				{
					if (!(oldConstraint is Schema.RowConstraint))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsTransitionConstraint, oldConstraint.Name);
						
					AlterConstraintDefinition alterConstraintDefinition = (AlterConstraintDefinition)constraintDefinition;

					if (alterConstraintDefinition.Expression != null)
					{
						ConstraintDefinition newConstraintDefinition = new ConstraintDefinition();
						newConstraintDefinition.ConstraintName = alterConstraintDefinition.ConstraintName;
						newConstraintDefinition.MetaData = oldConstraint.MetaData.Copy();
						newConstraintDefinition.Expression = alterConstraintDefinition.Expression;
						Schema.TableVarConstraint newConstraint = Compiler.CompileTableVarConstraint(plan, tableVar, newConstraintDefinition);
							
						// Validate LNewConstraint
						if (newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
							ValidateConstraint(plan, program, tableVar, newConstraint);
							
						program.CatalogDeviceSession.DropTableVarConstraint(tableVar, oldConstraint);
						program.CatalogDeviceSession.CreateTableVarConstraint(tableVar, newConstraint);
						oldConstraint = newConstraint;
					}
				}
				else
				{
					if (!(oldConstraint is Schema.TransitionConstraint))
						throw new Schema.SchemaException(Schema.SchemaException.Codes.ConstraintIsNotTransitionConstraint, oldConstraint.Name);
						
					AlterTransitionConstraintDefinition alterConstraintDefinition = (AlterTransitionConstraintDefinition)constraintDefinition;

					if ((alterConstraintDefinition.OnInsert != null) || (alterConstraintDefinition.OnUpdate != null) || (alterConstraintDefinition.OnDelete != null))
					{
						Schema.TransitionConstraint oldTransitionConstraint = (Schema.TransitionConstraint)oldConstraint;
						
						// Compile the new constraint
						TransitionConstraintDefinition newConstraintDefinition = new TransitionConstraintDefinition();

						newConstraintDefinition.ConstraintName = alterConstraintDefinition.ConstraintName;
						newConstraintDefinition.MetaData = oldConstraint.MetaData.Copy();

						if (alterConstraintDefinition.OnInsert is AlterTransitionConstraintDefinitionCreateItem)
						{
							if (oldTransitionConstraint.OnInsertNode != null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.InsertTransitionExists, oldTransitionConstraint.Name);
							newConstraintDefinition.OnInsertExpression = ((AlterTransitionConstraintDefinitionCreateItem)alterConstraintDefinition.OnInsert).Expression;
						}
						else if (alterConstraintDefinition.OnInsert is AlterTransitionConstraintDefinitionAlterItem)
						{
							if (oldTransitionConstraint.OnInsertNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoInsertTransition, oldTransitionConstraint.Name);
							newConstraintDefinition.OnInsertExpression = ((AlterTransitionConstraintDefinitionAlterItem)alterConstraintDefinition.OnInsert).Expression;
						}
						else if (alterConstraintDefinition.OnInsert is AlterTransitionConstraintDefinitionDropItem)
						{
							if (oldTransitionConstraint.OnInsertNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoInsertTransition, oldTransitionConstraint.Name);
						}
						
						if (alterConstraintDefinition.OnUpdate is AlterTransitionConstraintDefinitionCreateItem)
						{
							if (oldTransitionConstraint.OnUpdateNode != null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.UpdateTransitionExists, oldTransitionConstraint.Name);
							newConstraintDefinition.OnUpdateExpression = ((AlterTransitionConstraintDefinitionCreateItem)alterConstraintDefinition.OnUpdate).Expression;
						}
						else if (alterConstraintDefinition.OnUpdate is AlterTransitionConstraintDefinitionAlterItem)
						{
							if (oldTransitionConstraint.OnUpdateNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoUpdateTransition, oldTransitionConstraint.Name);
							newConstraintDefinition.OnUpdateExpression = ((AlterTransitionConstraintDefinitionAlterItem)alterConstraintDefinition.OnUpdate).Expression;
						}
						else if (alterConstraintDefinition.OnUpdate is AlterTransitionConstraintDefinitionDropItem)
						{
							if (oldTransitionConstraint.OnUpdateNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoUpdateTransition, oldTransitionConstraint.Name);
						}
						
						if (alterConstraintDefinition.OnDelete is AlterTransitionConstraintDefinitionCreateItem)
						{
							if (oldTransitionConstraint.OnDeleteNode != null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.DeleteTransitionExists, oldTransitionConstraint.Name);
							newConstraintDefinition.OnDeleteExpression = ((AlterTransitionConstraintDefinitionCreateItem)alterConstraintDefinition.OnDelete).Expression;
						}
						else if (alterConstraintDefinition.OnDelete is AlterTransitionConstraintDefinitionAlterItem)
						{
							if (oldTransitionConstraint.OnDeleteNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoDeleteTransition, oldTransitionConstraint.Name);
							newConstraintDefinition.OnDeleteExpression = ((AlterTransitionConstraintDefinitionAlterItem)alterConstraintDefinition.OnDelete).Expression;
						}
						else if (alterConstraintDefinition.OnDelete is AlterTransitionConstraintDefinitionDropItem)
						{
							if (oldTransitionConstraint.OnDeleteNode == null)
								throw new Schema.SchemaException(Schema.SchemaException.Codes.NoDeleteTransition, oldTransitionConstraint.Name);
						}
						
						Schema.TransitionConstraint newConstraint = (Schema.TransitionConstraint)Compiler.CompileTableVarConstraint(plan, tableVar, newConstraintDefinition);
							
						// Validate LNewConstraint
						if ((newConstraint.OnInsertNode != null) && newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
							ValidateConstraint(plan, program, tableVar, newConstraint);
							
						program.CatalogDeviceSession.DropTableVarConstraint(tableVar, oldTransitionConstraint);
						program.CatalogDeviceSession.CreateTableVarConstraint(tableVar, newConstraint);
						oldConstraint = newConstraint;
					}
				}
				
				program.CatalogDeviceSession.AlterMetaData(oldConstraint, constraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateConstraints(Plan plan, Program program, Schema.TableVar tableVar, CreateConstraintDefinitions createConstraints)
		{
			foreach (CreateConstraintDefinition constraintDefinition in createConstraints)
			{
				Schema.TableVarConstraint newConstraint = Compiler.CompileTableVarConstraint(program.Plan, tableVar, constraintDefinition);
				
				if (((newConstraint is Schema.RowConstraint) || (((Schema.TransitionConstraint)newConstraint).OnInsertNode != null)) && newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
					ValidateConstraint(plan, program, tableVar, newConstraint);
					
				program.CatalogDeviceSession.CreateTableVarConstraint(tableVar, newConstraint);
			}
		}

		protected void DropReferences(Plan plan, Program program, Schema.TableVar tableVar, DropReferenceDefinitions dropReferences)
		{
			foreach (DropReferenceDefinition referenceDefinition in dropReferences)
			{
				Schema.Reference reference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(program.Plan, referenceDefinition.ReferenceName);
				//AProgram.Plan.AcquireCatalogLock(LReference, LockMode.Exclusive);
				new DropReferenceNode(reference).Execute(program);
			}
		}

		protected void AlterReferences(Plan plan, Program program, Schema.TableVar tableVar, AlterReferenceDefinitions alterReferences)
		{
			foreach (AlterReferenceDefinition referenceDefinition in alterReferences)
			{
				Schema.Reference reference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(program.Plan, referenceDefinition.ReferenceName);
				//AProgram.Plan.AcquireCatalogLock(LReference, LockMode.Exclusive);
				program.CatalogDeviceSession.AlterMetaData(reference, referenceDefinition.AlterMetaData);
			}
		}

		protected void CreateReferences(Plan plan, Program program, Schema.TableVar tableVar, ReferenceDefinitions createReferences)
		{
			foreach (ReferenceDefinition referenceDefinition in createReferences)
			{
				CreateReferenceStatement statement = new CreateReferenceStatement();
				statement.TableVarName = tableVar.Name;
				statement.ReferenceName = referenceDefinition.ReferenceName;
				statement.Columns.AddRange(referenceDefinition.Columns);
				statement.ReferencesDefinition = referenceDefinition.ReferencesDefinition;
				statement.MetaData = referenceDefinition.MetaData;
				Compiler.CompileCreateReferenceStatement(program.Plan, statement).Execute(program);
			}
		}
    }
    
    public class AlterTableNode : AlterTableVarNode
    {
		// AlterTableVarStatement
		public override AlterTableVarStatement AlterTableVarStatement
		{
			get { return _alterTableVarStatement; }
			set
			{
				if (!(value is AlterTableStatement))
					throw new RuntimeException(RuntimeException.Codes.InvalidAlterTableVarStatement);
				_alterTableVarStatement = value;
			}
		}
		
		// AlterTableStatement
		public AlterTableStatement AlterTableStatement
		{
			get { return (AlterTableStatement)_alterTableVarStatement; }
			set { _alterTableVarStatement = value; }
		}
		
		private Schema.BaseTableVar _tableVar;
		public Schema.BaseTableVar TableVar { get { return _tableVar; } }

		protected void DropColumns(Plan plan, Program program, Schema.BaseTableVar table, DropColumnDefinitions dropColumns)
		{
			if (dropColumns.Count > 0)
				CheckNoDependents(program, table);

			foreach (DropColumnDefinition columnDefinition in dropColumns)
			{
				Schema.TableVarColumn column = table.Columns[columnDefinition.ColumnName];

				foreach (Schema.Key key in table.Keys)
					if (key.Columns.ContainsName(column.Name))
						throw new CompilerException(CompilerException.Codes.ObjectIsReferenced, column.Name, key.Name);
						
				foreach (Schema.Order order in table.Orders)
					if (order.Columns.Contains(column.Name))
						throw new CompilerException(CompilerException.Codes.ObjectIsReferenced, column.Name, order.Name);
						
				program.CatalogDeviceSession.DropTableVarColumn(table, column);
			}
		}
		
		public static void ReplaceValueNodes(PlanNode node, List<PlanNode> valueNodes, string columnName)
		{
			if ((node is StackReferenceNode) && (Schema.Object.EnsureUnrooted(((StackReferenceNode)node).Identifier) == Keywords.Value))
			{
				((StackReferenceNode)node).Identifier = columnName;
				valueNodes.Add(node);
			}

			for (int index = 0; index < node.NodeCount; index++)
				ReplaceValueNodes(node.Nodes[index], valueNodes, columnName);
		}
		
		public static void RestoreValueNodes(List<PlanNode> valueNodes)
		{
			for (int index = 0; index < valueNodes.Count; index++)
				((StackReferenceNode)valueNodes[index]).Identifier = Schema.Object.EnsureRooted(Keywords.Value);
		}
		
		protected void ValidateConstraint(Plan plan, Program program, Schema.BaseTableVar table, Schema.TableVarColumn column, Schema.TableVarColumnConstraint constraint)
		{
			// Ensure that all values in the given column of the given base table variable satisfy the new constraint
			List<PlanNode> valueNodes = new List<PlanNode>();
			ReplaceValueNodes(constraint.Node, valueNodes, column.Name);
			Expression constraintExpression = new UnaryExpression(Instructions.Not, (Expression)constraint.Node.EmitStatement(EmitMode.ForCopy));
			RestoreValueNodes(valueNodes);
			PlanNode planNode = 
				Compiler.Compile
				(
					plan,
					new UnaryExpression
					(
						Instructions.Exists,
						new RestrictExpression
						(
							new ProjectExpression
							(
								new IdentifierExpression(table.Name),
								new string[] { column.Name }
							),
							constraintExpression
						)
					)
				);
				
			object objectValue;

			try
			{
				objectValue = planNode.Execute(program);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, constraint.Name);
			}

			if ((bool)objectValue)
				throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, constraint.Name);
		}
		
		protected void DropColumnConstraints(Plan plan, Program program, Schema.BaseTableVar table, Schema.TableVarColumn column, DropConstraintDefinitions dropConstraints)
		{
			foreach (DropConstraintDefinition constraintDefinition in dropConstraints)
			{
				Schema.Constraint constraint = column.Constraints[constraintDefinition.ConstraintName];
				
				program.CatalogDeviceSession.DropTableVarColumnConstraint(column, (Schema.TableVarColumnConstraint)constraint);
			}
		}
		
		protected void AlterColumnConstraints(Plan plan, Program program, Schema.BaseTableVar table, Schema.TableVarColumn column, AlterConstraintDefinitions alterConstraints)
		{
			foreach (AlterConstraintDefinition constraintDefinition in alterConstraints)
			{
				Schema.TableVarColumnConstraint constraint = column.Constraints[constraintDefinition.ConstraintName];
				
				if (constraintDefinition.Expression != null)
				{
					Schema.TableVarColumnConstraint newConstraint = Compiler.CompileTableVarColumnConstraint(plan, table, column, new ConstraintDefinition(constraintDefinition.ConstraintName, constraintDefinition.Expression, constraint.MetaData == null ? null : constraint.MetaData.Copy()));
					if (newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
						ValidateConstraint(plan, program, table, column, newConstraint);
					program.CatalogDeviceSession.DropTableVarColumnConstraint(column, constraint);
					program.CatalogDeviceSession.CreateTableVarColumnConstraint(column, newConstraint);
					constraint = newConstraint;
				}
					
				AlterMetaData(constraint, constraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateColumnConstraints(Plan plan, Program program, Schema.BaseTableVar table, Schema.TableVarColumn column, ConstraintDefinitions createConstraints)
		{
			foreach (ConstraintDefinition constraintDefinition in createConstraints)
			{
				Schema.TableVarColumnConstraint newConstraint = Compiler.CompileTableVarColumnConstraint(plan, table, column, constraintDefinition);
				if (newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
					ValidateConstraint(plan, program, table, column, newConstraint);
				program.CatalogDeviceSession.CreateTableVarColumnConstraint(column, newConstraint);
			}
		}
		
		// TODO: Alter table variable column scalar type
		protected void AlterColumns(Plan plan, Program program, Schema.BaseTableVar table, AlterColumnDefinitions alterColumns)
		{
			Schema.TableVarColumn column;
			foreach (AlterColumnDefinition columnDefinition in alterColumns)
			{
				column = table.Columns[columnDefinition.ColumnName];
				
				if (columnDefinition.TypeSpecifier != null)
					throw new RuntimeException(RuntimeException.Codes.UnimplementedAlterCommand, "Column type");
					
				if ((columnDefinition.ChangeNilable) && (column.IsNilable != columnDefinition.IsNilable))
				{
					if (column.IsNilable)
					{
						// verify that no data exists in the column that is nil
						PlanNode node = 
							Compiler.Compile
							(
								plan,
								new UnaryExpression
								(
									Instructions.Exists,
									new RestrictExpression
									(
										new IdentifierExpression(table.Name),
										new UnaryExpression("IsNil", new IdentifierExpression(column.Name))
									)
								)
							);
							
						object result;
						try
						{
							result = node.Execute(program);
						}
						catch (Exception E)
						{
							throw new RuntimeException(RuntimeException.Codes.ErrorValidatingColumnConstraint, E, "NotNil", column.Name, table.DisplayName);
						}
						
						if ((result == null) || (bool)result)
							throw new RuntimeException(RuntimeException.Codes.NonNilConstraintViolation, column.Name, table.DisplayName);
					}

					program.CatalogDeviceSession.SetTableVarColumnIsNilable(column, columnDefinition.IsNilable);
				}
					
				if (columnDefinition.Default is DefaultDefinition)
				{
					if (column.Default != null)
						throw new RuntimeException(RuntimeException.Codes.DefaultDefined, column.Name, table.DisplayName);
						
					program.CatalogDeviceSession.SetTableVarColumnDefault(column, Compiler.CompileTableVarColumnDefault(program.Plan, table, column, (DefaultDefinition)columnDefinition.Default));
				}
				else if (columnDefinition.Default is AlterDefaultDefinition)
				{
					if (column.Default == null)
						throw new RuntimeException(RuntimeException.Codes.DefaultNotDefined, column.Name, table.DisplayName);

					AlterDefaultDefinition defaultDefinition = (AlterDefaultDefinition)columnDefinition.Default;
					if (defaultDefinition.Expression != null)
					{
						Schema.TableVarColumnDefault newDefault = Compiler.CompileTableVarColumnDefault(program.Plan, table, column, new DefaultDefinition(defaultDefinition.Expression, column.Default.MetaData == null ? null : column.Default.MetaData.Copy()));
						program.CatalogDeviceSession.SetTableVarColumnDefault(column, newDefault);
					}

					program.CatalogDeviceSession.AlterMetaData(column.Default, defaultDefinition.AlterMetaData);
				}
				else if (columnDefinition.Default is DropDefaultDefinition)
				{
					if (column.Default == null)
						throw new RuntimeException(RuntimeException.Codes.DefaultNotDefined, column.Name, table.DisplayName);
					program.CatalogDeviceSession.SetTableVarColumnDefault(column, null);
				}
				
				DropColumnConstraints(plan, program, table, column, columnDefinition.DropConstraints);
				AlterColumnConstraints(plan, program, table, column, columnDefinition.AlterConstraints);
				CreateColumnConstraints(plan, program, table, column, columnDefinition.CreateConstraints);
				
				program.CatalogDeviceSession.AlterMetaData(column, columnDefinition.AlterMetaData);
			}
		}
		
		protected Schema.Objects<Schema.TableVarColumn> _nonNilableColumns;
		protected Schema.Objects<Schema.TableVarColumn> _defaultColumns;

		protected void CreateColumns(Plan plan, Program program, Schema.BaseTableVar table, ColumnDefinitions createColumns)
		{
			_nonNilableColumns = new Schema.Objects<Schema.TableVarColumn>();
			_defaultColumns = new Schema.Objects<Schema.TableVarColumn>();
			Schema.BaseTableVar dummy = new Schema.BaseTableVar(table.Name);
			dummy.Library = table.Library;
			dummy.IsGenerated = table.IsGenerated;
			plan.PushCreationObject(dummy);
			try
			{
				if (createColumns.Count > 0)
					CheckNoOperatorDependents(program, table);
				
				foreach (ColumnDefinition columnDefinition in createColumns)
				{
					Schema.TableVarColumn tableVarColumn = Compiler.CompileTableVarColumnDefinition(plan, table, columnDefinition);
					if (tableVarColumn.Default != null)
						_defaultColumns.Add(tableVarColumn);
					if (!tableVarColumn.IsNilable)
					{
						if (tableVarColumn.Default == null)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidAlterTableVarCreateColumnStatement, tableVarColumn.Name, table.DisplayName);
						tableVarColumn.IsNilable = true;
						_nonNilableColumns.Add(tableVarColumn);
					}
					
					program.CatalogDeviceSession.CreateTableVarColumn(table, tableVarColumn);
				}
				table.DetermineRemotable(program.CatalogDeviceSession);
			}
			finally
			{
				plan.PopCreationObject();
			}
			if (dummy.HasDependencies())
				table.AddDependencies(dummy.Dependencies);
		}

		public override void DeterminePotentialDevice(Plan plan)
		{
			_tableVar = (Schema.BaseTableVar)FindObject(plan, _alterTableVarStatement.TableVarName);
			_potentialDevice = _tableVar.Device;
		}

		public override void DetermineDevice(Plan plan)
		{
			base.DetermineDevice(plan);
			DeviceSupported = false;
		}
		
		public override void BindToProcess(Plan plan)
		{
			if (Device != null)
				plan.CheckRight(Device.GetRight(Schema.RightNames.AlterStore));
			plan.CheckRight(_tableVar.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}
		
		private void UpdateDefaultColumns(Program program)
		{
			if (_defaultColumns.Count != 0)
			{
				// Update the data in all columns that have been added
				// update <table name> set { <column name> := <default expression> [, ...] }
				UpdateStatement updateStatement = new UpdateStatement(new IdentifierExpression(_tableVar.Name));
				foreach (Schema.TableVarColumn column in _defaultColumns)
					updateStatement.Columns.Add(new UpdateColumnExpression(new IdentifierExpression(column.Name), (Expression)column.Default.Node.EmitStatement(EmitMode.ForCopy)));
					
				Compiler.Compile(program.Plan, updateStatement).Execute(program);
				
				// Set the nilable for each column that is not nil
				// alter table <table name> { alter column <column name> { not nil } };
				AlterTableStatement alterStatement = new AlterTableStatement();
				alterStatement.TableVarName = _tableVar.Name;
				foreach (Schema.TableVarColumn column in _nonNilableColumns)
				{
					AlterColumnDefinition alterColumn = new AlterColumnDefinition();
					alterColumn.ColumnName = column.Name;
					alterColumn.ChangeNilable = true;
					alterColumn.IsNilable = false;
					alterStatement.AlterColumns.Add(alterColumn);
				}
				
				Compiler.Compile(program.Plan, alterStatement).Execute(program);
			}
		}
		
		public override object InternalExecute(Program program)
		{
			using (var plan = new Plan(program.ServerProcess))
			{
				_tableVar = (Schema.BaseTableVar)FindObject(plan, _alterTableVarStatement.TableVarName);
				if (!program.ServerProcess.InLoadingContext())
					program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, _tableVar);

				DropColumns(plan, program, _tableVar, AlterTableStatement.DropColumns);
				AlterColumns(plan, program, _tableVar, AlterTableStatement.AlterColumns);
				CreateColumns(plan, program, _tableVar, AlterTableStatement.CreateColumns);

				// Change columns in the device			
				AlterTableStatement statement = new AlterTableStatement();
				statement.TableVarName = AlterTableStatement.TableVarName;
				statement.DropColumns.AddRange(AlterTableStatement.DropColumns);
				statement.AlterColumns.AddRange(AlterTableStatement.AlterColumns);
				statement.CreateColumns.AddRange(AlterTableStatement.CreateColumns);
				AlterTableNode node = new AlterTableNode();
				node.AlterTableStatement = statement;
				node.DeterminePotentialDevice(plan);
				node.DetermineDevice(plan);
				node.DetermineAccessPath(plan);
				_tableVar.Device.Prepare(plan, node);
				program.DeviceExecute(_tableVar.Device, node);
				UpdateDefaultColumns(program);

				// Drop keys and orders
				statement = new AlterTableStatement();
				statement.TableVarName = AlterTableStatement.TableVarName;
				statement.DropKeys.AddRange(AlterTableStatement.DropKeys);
				statement.DropOrders.AddRange(AlterTableStatement.DropOrders);
				node = new AlterTableNode();
				node.AlterTableStatement = statement;
				node.DeterminePotentialDevice(plan);
				node.DetermineDevice(plan);
				node.DetermineAccessPath(plan);
				_tableVar.Device.Prepare(plan, node);
				program.DeviceExecute(_tableVar.Device, node);
				DropKeys(plan, program, _tableVar, _alterTableVarStatement.DropKeys);
				DropOrders(plan, program, _tableVar, _alterTableVarStatement.DropOrders);

				AlterKeys(plan, program, _tableVar, _alterTableVarStatement.AlterKeys);
				AlterOrders(plan, program, _tableVar, _alterTableVarStatement.AlterOrders);
				CreateKeys(plan, program, _tableVar, _alterTableVarStatement.CreateKeys);
				CreateOrders(plan, program, _tableVar, _alterTableVarStatement.CreateOrders);

				statement = new AlterTableStatement();
				statement.TableVarName = AlterTableStatement.TableVarName;
				statement.CreateKeys.AddRange(AlterTableStatement.CreateKeys);
				statement.CreateOrders.AddRange(AlterTableStatement.CreateOrders);
				node = new AlterTableNode();
				node.AlterTableStatement = statement;
				node.DeterminePotentialDevice(plan);
				node.DetermineDevice(plan);
				node.DetermineAccessPath(plan);
				_tableVar.Device.Prepare(plan, node);
				program.DeviceExecute(_tableVar.Device, node);

				DropReferences(plan, program, _tableVar, _alterTableVarStatement.DropReferences);
				AlterReferences(plan, program, _tableVar, _alterTableVarStatement.AlterReferences);
				CreateReferences(plan, program, _tableVar, _alterTableVarStatement.CreateReferences);
				DropConstraints(plan, program, _tableVar, _alterTableVarStatement.DropConstraints);
				AlterConstraints(plan, program, _tableVar, _alterTableVarStatement.AlterConstraints);
				CreateConstraints(plan, program, _tableVar, _alterTableVarStatement.CreateConstraints);
				program.CatalogDeviceSession.AlterMetaData(_tableVar, _alterTableVarStatement.AlterMetaData);

				if (ShouldAffectDerivationTimeStamp)
				{
					program.Catalog.UpdateCacheTimeStamp();
					program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: alter table {1}", program.Catalog.CacheTimeStamp.ToString(), _alterTableVarStatement.TableVarName));
					program.Catalog.UpdatePlanCacheTimeStamp();
					program.Catalog.UpdateDerivationTimeStamp();
				}

				program.CatalogDeviceSession.UpdateCatalogObject(_tableVar);
			
				return null;
			}
		}
    }
    
    public class AlterViewNode : AlterTableVarNode
    {
		// AlterTableVarStatement
		public override AlterTableVarStatement AlterTableVarStatement
		{
			get { return _alterTableVarStatement; }
			set
			{
				if (!(value is AlterViewStatement))
					throw new RuntimeException(RuntimeException.Codes.InvalidAlterTableVarStatement);
				_alterTableVarStatement = value;
			}
		}

		// AlterViewStatement
		public AlterViewStatement AlterViewStatement
		{
			get { return (AlterViewStatement)_alterTableVarStatement; }
			set { _alterTableVarStatement = value; }
		}

		protected override Schema.Object FindObject(Plan plan, string objectName)
		{
			Schema.Object objectValue = base.FindObject(plan, objectName);
			if (!(objectValue is Schema.DerivedTableVar))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotView, objectName);
			return objectValue;
		}

		public override void BindToProcess(Plan plan)
		{
			Schema.DerivedTableVar view = (Schema.DerivedTableVar)FindObject(plan, _alterTableVarStatement.TableVarName);
			plan.CheckRight(view.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			using (var plan = new Plan(program.ServerProcess))
			{
				Schema.DerivedTableVar view = (Schema.DerivedTableVar)FindObject(plan, _alterTableVarStatement.TableVarName);
				if (!program.ServerProcess.InLoadingContext())
					program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, view);

				DropKeys(plan, program, view, _alterTableVarStatement.DropKeys);
				AlterKeys(plan, program, view, _alterTableVarStatement.AlterKeys);
				CreateKeys(plan, program, view, _alterTableVarStatement.CreateKeys);
				DropOrders(plan, program, view, _alterTableVarStatement.DropOrders);
				AlterOrders(plan, program, view, _alterTableVarStatement.AlterOrders);
				CreateOrders(plan, program, view, _alterTableVarStatement.CreateOrders);
				DropReferences(plan, program, view, _alterTableVarStatement.DropReferences);
				AlterReferences(plan, program, view, _alterTableVarStatement.AlterReferences);
				CreateReferences(plan, program, view, _alterTableVarStatement.CreateReferences);
				DropConstraints(plan, program, view, _alterTableVarStatement.DropConstraints);
				AlterConstraints(plan, program, view, _alterTableVarStatement.AlterConstraints);
				CreateConstraints(plan, program, view, _alterTableVarStatement.CreateConstraints);
				program.CatalogDeviceSession.AlterMetaData(view, _alterTableVarStatement.AlterMetaData);

				if (ShouldAffectDerivationTimeStamp)
				{
					program.Catalog.UpdateCacheTimeStamp();
					program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: alter view {1}", program.Catalog.CacheTimeStamp.ToString(), _alterTableVarStatement.TableVarName));
					program.Catalog.UpdatePlanCacheTimeStamp();
					program.Catalog.UpdateDerivationTimeStamp();
				}
			
				program.CatalogDeviceSession.UpdateCatalogObject(view);
			
				return null;
			}
		}
    }
    
    public class AlterScalarTypeNode : AlterNode
    {		
		// AlterScalarTypeStatement
		protected AlterScalarTypeStatement _alterScalarTypeStatement;
		public AlterScalarTypeStatement AlterScalarTypeStatement
		{
			get { return _alterScalarTypeStatement; }
			set { _alterScalarTypeStatement = value; }
		}

		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		protected override Schema.Object FindObject(Plan plan, string objectName)
		{
			Schema.Object objectValue = base.FindObject(plan, objectName);
			if (!(objectValue is Schema.ScalarType))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotScalarType, objectName);
			return objectValue;
		}
		
		protected void DropConstraints(Program program, Schema.ScalarType scalarType, DropConstraintDefinitions dropConstraints)
		{
			foreach (DropConstraintDefinition constraintDefinition in dropConstraints)
			{
				Schema.Constraint constraint = scalarType.Constraints[constraintDefinition.ConstraintName];
				program.CatalogDeviceSession.DropScalarTypeConstraint(scalarType, (Schema.ScalarTypeConstraint)constraint);
			}
		}
		
		protected void ValidateConstraint(Program program, Schema.ScalarType scalarType, Schema.ScalarTypeConstraint constraint)
		{
			// Ensure that all base table vars in the catalog with columns defined on this scalar type, or descendents of this scalar type, satisfy the new constraint
			List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(scalarType.ID, false);
			for (int index = 0; index < headers.Count; index++)
			{
				if (headers[index].ObjectType == "ScalarType")
				{
					// This is a descendent scalar type
					ValidateConstraint(program, (Schema.ScalarType)program.CatalogDeviceSession.ResolveObject(headers[index].ID), constraint);
				}
				else if ((headers[index].ObjectType == "BaseTableVar") && !headers[index].IsATObject)
				{
					// This is a tablevar with columns defined on this scalar type
					// Open a cursor on the expression <tablevar> over { <columns defined on this scalar type> }
					Schema.BaseTableVar baseTableVar = (Schema.BaseTableVar)program.CatalogDeviceSession.ResolveObject(headers[index].ID);
					foreach (Schema.Column column in baseTableVar.DataType.Columns)
						if (column.DataType.Equals(scalarType))
						{
							// if exists (table over { column } where not (constraint expression))) then raise
							List<PlanNode> valueNodes = new List<PlanNode>();
							AlterTableNode.ReplaceValueNodes(constraint.Node, valueNodes, column.Name);
							Expression constraintExpression = new UnaryExpression(Instructions.Not, (Expression)constraint.Node.EmitStatement(EmitMode.ForCopy));
							AlterTableNode.RestoreValueNodes(valueNodes);

							var planNode = 
								Compiler.Compile
								(
									program.Plan, 
									new UnaryExpression
									(
										Instructions.Exists, 
										new RestrictExpression
										(
											new ProjectExpression
											(
												new IdentifierExpression(baseTableVar.Name), 
												new string[] { column.Name }
											), 
											constraintExpression
										)
									)
								);

							object result;
							try
							{
								result = planNode.Execute(program);
							}
							catch (Exception E)
							{
								throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, constraint.Name);
							}
							
							if ((bool)result)
								throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, constraint.Name);
						}
				}
			}
		}
		
		protected void AlterConstraints(Program program, Schema.ScalarType scalarType, AlterConstraintDefinitions alterConstraints)
		{
			Schema.ScalarTypeConstraint constraint;
			foreach (AlterConstraintDefinition constraintDefinition in alterConstraints)
			{
				constraint = scalarType.Constraints[constraintDefinition.ConstraintName];
				
				if (constraintDefinition.Expression != null)
				{
					Schema.ScalarTypeConstraint newConstraint = Compiler.CompileScalarTypeConstraint(program.Plan, scalarType, new ConstraintDefinition(constraintDefinition.ConstraintName, constraintDefinition.Expression, constraint.MetaData == null ? null : constraint.MetaData.Copy()));
					if (newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
						ValidateConstraint(program, scalarType, newConstraint);
					
					program.CatalogDeviceSession.DropScalarTypeConstraint(scalarType, constraint);
					program.CatalogDeviceSession.CreateScalarTypeConstraint(scalarType, newConstraint);
					constraint = newConstraint;
				}
					
				program.CatalogDeviceSession.AlterMetaData(constraint, constraintDefinition.AlterMetaData);
			}
		}
		
		protected void CreateConstraints(Program program, Schema.ScalarType scalarType, ConstraintDefinitions createConstraints)
		{
			foreach (ConstraintDefinition constraintDefinition in createConstraints)
			{
				Schema.ScalarTypeConstraint newConstraint = Compiler.CompileScalarTypeConstraint(program.Plan, scalarType, constraintDefinition);

				if (newConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
					ValidateConstraint(program, scalarType, newConstraint);

				program.CatalogDeviceSession.CreateScalarTypeConstraint(scalarType, newConstraint);
			}
		}
		
		protected void DropSpecials(Program program, Schema.ScalarType scalarType, DropSpecialDefinitions dropSpecials)
		{
			foreach (DropSpecialDefinition specialDefinition in dropSpecials)
			{
				if (scalarType.IsSpecialOperator != null)
				{
					new DropOperatorNode(scalarType.IsSpecialOperator).Execute(program);
					program.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(scalarType, null);
				}
				
				// Dropping a special
				Schema.Special special = scalarType.Specials[specialDefinition.Name];

				// drop operator ScalarTypeNameSpecialName()
				new DropOperatorNode(special.Comparer).Execute(program);
				// drop operator IsSpecialName(ScalarType)
				new DropOperatorNode(special.Selector).Execute(program);

				program.CatalogDeviceSession.DropSpecial(scalarType, special);
			}
		}
		
		protected void AlterSpecials(Program program, Schema.ScalarType scalarType, AlterSpecialDefinitions alterSpecials)
		{
			Schema.Special special;
			foreach (AlterSpecialDefinition specialDefinition in alterSpecials)
			{
				special = scalarType.Specials[specialDefinition.Name];

				if (specialDefinition.Value != null)
				{
					if (scalarType.IsSpecialOperator != null)
					{
						new DropOperatorNode(scalarType.IsSpecialOperator).Execute(program);
						program.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(scalarType, null);
					}
					
					Schema.Special newSpecial = new Schema.Special(Schema.Object.GetNextObjectID(), special.Name);
					newSpecial.Library = scalarType.Library == null ? null : program.Plan.CurrentLibrary;
					program.Plan.PushCreationObject(newSpecial);
					try
					{
						newSpecial.ValueNode = Compiler.CompileTypedExpression(program.Plan, specialDefinition.Value, scalarType);

						// Recompilation of the comparer and selector for the special
						Schema.Operator newSelector = Compiler.CompileSpecialSelector(program.Plan, scalarType, newSpecial, specialDefinition.Name, newSpecial.ValueNode);
						if (newSpecial.HasDependencies())
							newSelector.AddDependencies(newSpecial.Dependencies);
						newSelector.DetermineRemotable(program.CatalogDeviceSession);

						Schema.Operator newComparer = Compiler.CompileSpecialComparer(program.Plan, scalarType, newSpecial, specialDefinition.Name, newSpecial.ValueNode);
						if (newSpecial.HasDependencies())
							newComparer.AddDependencies(newSpecial.Dependencies);
						newComparer.DetermineRemotable(program.CatalogDeviceSession);

						new DropOperatorNode(special.Selector).Execute(program);
						new CreateOperatorNode(newSelector).Execute(program);
						newSpecial.Selector = newSelector;

						new DropOperatorNode(special.Comparer).Execute(program);
						new CreateOperatorNode(newComparer).Execute(program);
						newSpecial.Comparer = newComparer;
						
						program.CatalogDeviceSession.DropSpecial(scalarType, special);
						program.CatalogDeviceSession.CreateSpecial(scalarType, newSpecial);
						special = newSpecial;
					}
					finally
					{
						program.Plan.PopCreationObject();
					}
				}

				program.CatalogDeviceSession.AlterMetaData(special, specialDefinition.AlterMetaData);
			}
		}
		
		protected void EnsureIsSpecialOperator(Program program, Schema.ScalarType scalarType)
		{
			if (scalarType.IsSpecialOperator == null)
			{
				OperatorBindingContext context = new OperatorBindingContext(null, "IsSpecial", program.Plan.NameResolutionPath, new Schema.Signature(new Schema.SignatureElement[]{new Schema.SignatureElement(scalarType)}), true);
				Compiler.ResolveOperator(program.Plan, context);
				if (context.Operator == null)
				{
					program.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(scalarType, Compiler.CompileSpecialOperator(program.Plan, scalarType));
					new CreateOperatorNode(scalarType.IsSpecialOperator).Execute(program);
				}
			}
		}
		
		protected void CreateSpecials(Program program, Schema.ScalarType scalarType, SpecialDefinitions createSpecials)
		{
			foreach (SpecialDefinition specialDefinition in createSpecials)
			{
				// If we are deserializing, then there is no need to recreate the IsSpecial operator
				if ((!program.ServerProcess.InLoadingContext()) && (scalarType.IsSpecialOperator != null))
				{
					new DropOperatorNode(scalarType.IsSpecialOperator).Execute(program);
					program.CatalogDeviceSession.SetScalarTypeIsSpecialOperator(scalarType, null);
				}
				
				Schema.Special special = Compiler.CompileSpecial(program.Plan, scalarType, specialDefinition); 
				program.CatalogDeviceSession.CreateSpecial(scalarType, special);
				if (!program.ServerProcess.InLoadingContext())
				{
					new CreateOperatorNode(special.Selector).Execute(program);
					new CreateOperatorNode(special.Comparer).Execute(program);
				}
			}
			
			if (!program.ServerProcess.InLoadingContext())
				EnsureIsSpecialOperator(program, scalarType);
		}

		protected void DropRepresentations(Program program, Schema.ScalarType scalarType, DropRepresentationDefinitions dropRepresentations)
		{
			Schema.Representation representation;
			foreach (DropRepresentationDefinition representationDefinition in dropRepresentations)
			{
				representation = scalarType.Representations[representationDefinition.RepresentationName];
				foreach (Schema.Property property in representation.Properties)
				{
					if (property.ReadAccessor != null)
						new DropOperatorNode(property.ReadAccessor).Execute(program);
					if (property.WriteAccessor != null)
						new DropOperatorNode(property.WriteAccessor).Execute(program);
				}

				if (representation.Selector != null)				
					new DropOperatorNode(representation.Selector).Execute(program);
				program.CatalogDeviceSession.DropRepresentation(scalarType, representation);
				scalarType.ResetNativeRepresentationCache();
			}
		}
		
		protected void AlterRepresentations(Program program, Schema.ScalarType scalarType, AlterRepresentationDefinitions alterRepresentations)
		{
			Schema.Representation representation;
			foreach (AlterRepresentationDefinition representationDefinition in alterRepresentations)
			{
				representation = scalarType.Representations[representationDefinition.RepresentationName];
				
				DropProperties(program, scalarType, representation, representationDefinition.DropProperties);
				AlterProperties(program, scalarType, representation, representationDefinition.AlterProperties);
				CreateProperties(program, scalarType, representation, representationDefinition.CreateProperties);
				
				if (representationDefinition.SelectorAccessorBlock != null)
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

				program.CatalogDeviceSession.AlterMetaData(representation, representationDefinition.AlterMetaData);
				scalarType.ResetNativeRepresentationCache();
			}
		}
		
		protected void CreateRepresentations(Program program, Schema.ScalarType scalarType, RepresentationDefinitions createRepresentations)
		{
			Schema.Objects operators = new Schema.Objects();
			foreach (RepresentationDefinition representationDefinition in createRepresentations)
			{
				// Generated representations may be replaced by explicit representation definitions
				int representationIndex = scalarType.Representations.IndexOf(representationDefinition.RepresentationName);
				if ((representationIndex >= 0) && scalarType.Representations[representationIndex].IsGenerated)
				{
					DropRepresentationDefinitions dropRepresentationDefinitions = new DropRepresentationDefinitions();
					dropRepresentationDefinitions.Add(new DropRepresentationDefinition(representationDefinition.RepresentationName));
					DropRepresentations(program, scalarType, dropRepresentationDefinitions);
				}

				Schema.Representation representation = Compiler.CompileRepresentation(program.Plan, scalarType, operators, representationDefinition);
				program.CatalogDeviceSession.CreateRepresentation(scalarType, representation);
				program.Plan.CheckCompiled(); // Throw the error after the call to the catalog device session so the error occurs after the create is logged.
				scalarType.ResetNativeRepresentationCache();
			}

			if (!program.ServerProcess.InLoadingContext())
				foreach (Schema.Operator operatorValue in operators)
					new CreateOperatorNode(operatorValue).Execute(program);
		}

		protected void DropProperties(Program program, Schema.ScalarType scalarType, Schema.Representation representation, DropPropertyDefinitions dropProperties)
		{
			foreach (DropPropertyDefinition propertyDefinition in dropProperties)
			{
				Schema.Property property = representation.Properties[propertyDefinition.PropertyName];
				new DropOperatorNode(property.ReadAccessor).Execute(program);
				new DropOperatorNode(property.WriteAccessor).Execute(program);
				program.CatalogDeviceSession.DropProperty(representation, property);
			}
		}
		
		protected void AlterProperties(Program program, Schema.ScalarType scalarType, Schema.Representation representation, AlterPropertyDefinitions alterProperties)
		{
			Schema.Property property;
			foreach (AlterPropertyDefinition propertyDefinition in alterProperties)
			{
				property = representation.Properties[propertyDefinition.PropertyName];
				
				if (propertyDefinition.PropertyType != null)
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
					
				if (propertyDefinition.ReadAccessorBlock != null)
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

				if (propertyDefinition.WriteAccessorBlock != null)
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

				program.CatalogDeviceSession.AlterMetaData(property, propertyDefinition.AlterMetaData);
			}
		}
		
		protected void CreateProperties(Program program, Schema.ScalarType scalarType, Schema.Representation representation, PropertyDefinitions createProperties)
		{
			Schema.Objects operators = new Schema.Objects();
			foreach (PropertyDefinition propertyDefinition in createProperties)
			{
				Schema.Property property = Compiler.CompileProperty(program.Plan, scalarType, representation, propertyDefinition);
				program.CatalogDeviceSession.CreateProperty(representation, property);
				program.Plan.CheckCompiled(); // Throw the error after the CreateProperty call so that the operation is logged in the catalog device
			}
			
			foreach (Schema.Operator operatorValue in operators)
				new CreateOperatorNode(operatorValue).Execute(program);
		}
		
		public override void BindToProcess(Plan plan)
		{
			Schema.ScalarType scalarType = (Schema.ScalarType)FindObject(plan, _alterScalarTypeStatement.ScalarTypeName);
			plan.CheckRight(scalarType.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			Schema.ScalarType scalarType = (Schema.ScalarType)FindObject(program.Plan, _alterScalarTypeStatement.ScalarTypeName);
			
			// Constraints			
			DropConstraints(program, scalarType, _alterScalarTypeStatement.DropConstraints);
			AlterConstraints(program, scalarType, _alterScalarTypeStatement.AlterConstraints);
			CreateConstraints(program, scalarType, _alterScalarTypeStatement.CreateConstraints);

			// Default
			if (_alterScalarTypeStatement.Default is DefaultDefinition)
			{
				if (scalarType.Default != null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultDefined, scalarType.Name);
					
				program.CatalogDeviceSession.SetScalarTypeDefault(scalarType, Compiler.CompileScalarTypeDefault(program.Plan, scalarType, (DefaultDefinition)_alterScalarTypeStatement.Default));
			}
			else if (_alterScalarTypeStatement.Default is AlterDefaultDefinition)
			{
				if (scalarType.Default == null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultNotDefined, scalarType.Name);

				AlterDefaultDefinition defaultDefinition = (AlterDefaultDefinition)_alterScalarTypeStatement.Default;
				if (defaultDefinition.Expression != null)
				{
					Schema.ScalarTypeDefault newDefault = Compiler.CompileScalarTypeDefault(program.Plan, scalarType, new DefaultDefinition(defaultDefinition.Expression, scalarType.Default.MetaData == null ? null : scalarType.Default.MetaData.Copy()));
					
					program.CatalogDeviceSession.SetScalarTypeDefault(scalarType, newDefault);
				}

				program.CatalogDeviceSession.AlterMetaData(scalarType.Default, defaultDefinition.AlterMetaData);
			}
			else if (_alterScalarTypeStatement.Default is DropDefaultDefinition)
			{
				if (scalarType.Default == null)
					throw new RuntimeException(RuntimeException.Codes.ScalarTypeDefaultNotDefined, scalarType.Name);
					
				program.CatalogDeviceSession.SetScalarTypeDefault(scalarType, null);
			}
			
			// Specials
			DropSpecials(program, scalarType, _alterScalarTypeStatement.DropSpecials);
			AlterSpecials(program, scalarType, _alterScalarTypeStatement.AlterSpecials);
			CreateSpecials(program, scalarType, _alterScalarTypeStatement.CreateSpecials);
			
			// Representations
			DropRepresentations(program, scalarType, _alterScalarTypeStatement.DropRepresentations);
			AlterRepresentations(program, scalarType, _alterScalarTypeStatement.AlterRepresentations);
			CreateRepresentations(program, scalarType, _alterScalarTypeStatement.CreateRepresentations);
			
			// If this scalar type has no representations defined, but it is based on a single branch inheritance hierarchy leading to a system type, build a default representation
			#if USETYPEINHERITANCE
			Schema.Objects operators = new Schema.Objects();
			if ((FAlterScalarTypeStatement.DropRepresentations.Count > 0) && (scalarType.Representations.Count == 0))
				Compiler.CompileDefaultRepresentation(AProgram.Plan, scalarType, operators);
			foreach (Schema.Operator operatorValue in operators)
				new CreateOperatorNode(operatorValue).Execute(AProgram);
			#endif
			
			// TODO: Alter semantics for types and representations

			program.CatalogDeviceSession.AlterClassDefinition(scalarType.ClassDefinition, _alterScalarTypeStatement.AlterClassDefinition);
			program.CatalogDeviceSession.AlterMetaData(scalarType, _alterScalarTypeStatement.AlterMetaData);
			
			if 
			(
				(_alterScalarTypeStatement.AlterClassDefinition != null) || 
				(_alterScalarTypeStatement.AlterMetaData != null)
			)
				scalarType.ResetNativeRepresentationCache();

			if (ShouldAffectDerivationTimeStamp)
			{
				program.Catalog.UpdateCacheTimeStamp();
				program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: alter scalar {1}", program.Catalog.CacheTimeStamp.ToString(), _alterScalarTypeStatement.ScalarTypeName));
				program.Catalog.UpdatePlanCacheTimeStamp();
				program.Catalog.UpdateDerivationTimeStamp();
			}
			program.CatalogDeviceSession.UpdateCatalogObject(scalarType);
			
			return null;
		}
    }
    
    public abstract class AlterOperatorNodeBase : AlterNode
    {
		protected virtual Schema.Operator FindOperator(Plan plan, OperatorSpecifier operatorSpecifier)
		{
			return Compiler.ResolveOperatorSpecifier(plan, operatorSpecifier);
		}
    }
    
    public class AlterOperatorNode : AlterOperatorNodeBase
    {
		// AlterOperatorStatement
		protected AlterOperatorStatement _alterOperatorStatement;
		public AlterOperatorStatement AlterOperatorStatement
		{
			get { return _alterOperatorStatement; }
			set { _alterOperatorStatement = value; }
		}

		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			Schema.Operator operatorValue = FindOperator(plan, _alterOperatorStatement.OperatorSpecifier);
			plan.CheckRight(operatorValue.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			Schema.Operator operatorValue = FindOperator(program.Plan, _alterOperatorStatement.OperatorSpecifier);
			program.ServerProcess.ServerSession.Server.ATDevice.ReportOperatorChange(program.ServerProcess, operatorValue);
				
			if (_alterOperatorStatement.Block.AlterClassDefinition != null)
			{
				CheckNoDependents(program, operatorValue);
				
				program.CatalogDeviceSession.AlterClassDefinition(operatorValue.Block.ClassDefinition, _alterOperatorStatement.Block.AlterClassDefinition);
			}

			if (_alterOperatorStatement.Block.Block != null)
			{
				CheckNoDependents(program, operatorValue);
					
				Schema.Operator tempOperator = new Schema.Operator(operatorValue.Name);
				program.Plan.PushCreationObject(tempOperator);
				try
				{
					tempOperator.Block.BlockNode = Compiler.CompileOperatorBlock(program.Plan, operatorValue, _alterOperatorStatement.Block.Block);
					
					program.CatalogDeviceSession.AlterOperator(operatorValue, tempOperator);
				}
				finally
				{
					program.Plan.PopCreationObject();
				}
			}

			if (ShouldAffectDerivationTimeStamp)
			{
				program.CatalogDeviceSession.AlterMetaData(operatorValue, _alterOperatorStatement.AlterMetaData);
				program.Catalog.UpdateCacheTimeStamp();
				program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: alter operator {1}", program.Catalog.CacheTimeStamp.ToString(), _alterOperatorStatement.OperatorSpecifier));
				program.Catalog.UpdatePlanCacheTimeStamp();
			}

			program.CatalogDeviceSession.UpdateCatalogObject(operatorValue);
			
			return null;
		}
    }
    
    public class AlterAggregateOperatorNode : AlterOperatorNodeBase
    {		
		// AlterAggregateOperatorStatement
		protected AlterAggregateOperatorStatement _alterAggregateOperatorStatement;
		public AlterAggregateOperatorStatement AlterAggregateOperatorStatement
		{
			get { return _alterAggregateOperatorStatement; }
			set { _alterAggregateOperatorStatement = value; }
		}

		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}

		public override void BindToProcess(Plan plan)
		{
			Schema.Operator operatorValue = FindOperator(plan, _alterAggregateOperatorStatement.OperatorSpecifier);
			plan.CheckRight(operatorValue.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			Schema.Operator operatorValue = FindOperator(program.Plan, _alterAggregateOperatorStatement.OperatorSpecifier);
			if (!(operatorValue is Schema.AggregateOperator))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotAggregateOperator, _alterAggregateOperatorStatement.OperatorSpecifier.OperatorName);
				
			program.ServerProcess.ServerSession.Server.ATDevice.ReportOperatorChange(program.ServerProcess, operatorValue);

			Schema.AggregateOperator aggregateOperator = (Schema.AggregateOperator)operatorValue;

			if 
			(
				(_alterAggregateOperatorStatement.Initialization.Block != null) || 
				(_alterAggregateOperatorStatement.Aggregation.Block != null) || 
				(_alterAggregateOperatorStatement.Finalization.Block != null) || 
				(_alterAggregateOperatorStatement.Initialization.AlterClassDefinition != null) || 
				(_alterAggregateOperatorStatement.Aggregation.AlterClassDefinition != null) || 
				(_alterAggregateOperatorStatement.Finalization.AlterClassDefinition != null)
			)
			{
				CheckNoDependents(program, aggregateOperator);
				
				program.Plan.PushCreationObject(aggregateOperator);
				try
				{
					program.Plan.Symbols.PushWindow(0);
					try
					{
						Symbol resultVar = new Symbol(Keywords.Result, aggregateOperator.ReturnDataType);
						program.Plan.Symbols.Push(resultVar);
						
						if (_alterAggregateOperatorStatement.Initialization.AlterClassDefinition != null)
							program.CatalogDeviceSession.AlterClassDefinition(aggregateOperator.Initialization.ClassDefinition, _alterAggregateOperatorStatement.Initialization.AlterClassDefinition);
							
						if (_alterAggregateOperatorStatement.Initialization.Block != null)
							program.CatalogDeviceSession.AlterOperatorBlockNode(aggregateOperator.Initialization, Compiler.CompileStatement(program.Plan, _alterAggregateOperatorStatement.Initialization.Block));
						
						program.Plan.Symbols.PushFrame();
						try
						{
							foreach (Schema.Operand operand in aggregateOperator.Operands)
								program.Plan.Symbols.Push(new Symbol(operand.Name, operand.DataType, operand.Modifier == Modifier.Const));
								
							if (_alterAggregateOperatorStatement.Aggregation.AlterClassDefinition != null)
								program.CatalogDeviceSession.AlterClassDefinition(aggregateOperator.Aggregation.ClassDefinition, _alterAggregateOperatorStatement.Aggregation.AlterClassDefinition);
								
							if (_alterAggregateOperatorStatement.Aggregation.Block != null)
								program.CatalogDeviceSession.AlterOperatorBlockNode(aggregateOperator.Aggregation, Compiler.CompileDeallocateFrameVariablesNode(program.Plan, Compiler.CompileStatement(program.Plan, _alterAggregateOperatorStatement.Aggregation.Block)));
						}
						finally
						{
							program.Plan.Symbols.PopFrame();
						}
						
						if (_alterAggregateOperatorStatement.Finalization.AlterClassDefinition != null)
							program.CatalogDeviceSession.AlterClassDefinition(aggregateOperator.Finalization.ClassDefinition, _alterAggregateOperatorStatement.Finalization.AlterClassDefinition);
							
						if (_alterAggregateOperatorStatement.Finalization.Block != null)
							program.CatalogDeviceSession.AlterOperatorBlockNode(aggregateOperator.Finalization, Compiler.CompileDeallocateVariablesNode(program.Plan, Compiler.CompileStatement(program.Plan, _alterAggregateOperatorStatement.Finalization.Block), resultVar));
					}
					finally
					{
						program.Plan.Symbols.PopWindow();
					}
				}
				finally
				{
					program.Plan.PopCreationObject();
				}

				aggregateOperator.DetermineRemotable(program.CatalogDeviceSession);
			}

			if (ShouldAffectDerivationTimeStamp)
			{
				program.CatalogDeviceSession.AlterMetaData(aggregateOperator, _alterAggregateOperatorStatement.AlterMetaData);
				program.Catalog.UpdateCacheTimeStamp();
				program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: alter aggregate operator {1}", program.Catalog.CacheTimeStamp.ToString(), _alterAggregateOperatorStatement.OperatorSpecifier));
				program.Catalog.UpdatePlanCacheTimeStamp();
			}

			program.CatalogDeviceSession.UpdateCatalogObject(aggregateOperator);

			return null;
		}
    }
    
    public class AlterConstraintNode : AlterNode
    {		
		// AlterConstraintStatement
		protected AlterConstraintStatement _alterConstraintStatement;
		public AlterConstraintStatement AlterConstraintStatement
		{
			get { return _alterConstraintStatement; }
			set { _alterConstraintStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan plan, string objectName)
		{
			Schema.Object objectValue = base.FindObject(plan, objectName);
			if (!(objectValue is Schema.CatalogConstraint))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotConstraint, objectName);
			return objectValue;
		}
		
		public override void BindToProcess(Plan plan)
		{
			Schema.CatalogConstraint constraint = (Schema.CatalogConstraint)FindObject(plan, _alterConstraintStatement.ConstraintName);
			plan.CheckRight(constraint.GetRight(Schema.RightNames.Alter));
			base.BindToProcess (plan);
		}

		public override object InternalExecute(Program program)
		{
			Schema.CatalogConstraint constraint = (Schema.CatalogConstraint)FindObject(program.Plan, _alterConstraintStatement.ConstraintName);
			
			if (_alterConstraintStatement.Expression != null)
			{
				Schema.CatalogConstraint tempConstraint = new Schema.CatalogConstraint(constraint.Name);
				tempConstraint.ConstraintType = constraint.ConstraintType;
				program.Plan.PushCreationObject(tempConstraint);
				try
				{
					tempConstraint.Node = Compiler.Compile(program.Plan, _alterConstraintStatement.Expression);
					
					// Validate the new constraint
					if (tempConstraint.Enforced && !program.ServerProcess.IsLoading() && program.ServerProcess.IsReconciliationEnabled())
					{
						object objectValue;
						try
						{
							objectValue = tempConstraint.Node.Execute(program);
						}
						catch (Exception E)
						{
							throw new RuntimeException(RuntimeException.Codes.ErrorValidatingConstraint, E, tempConstraint.Name);
						}
						
						if (!(bool)objectValue)
							throw new RuntimeException(RuntimeException.Codes.ConstraintViolation, tempConstraint.Name);
					}
					
					program.ServerProcess.ServerSession.Server.RemoveCatalogConstraintCheck(constraint);
					
					program.CatalogDeviceSession.AlterConstraint(constraint, tempConstraint);
				}
				finally
				{
					program.Plan.PopCreationObject();
				}
			}
			
			program.CatalogDeviceSession.AlterMetaData(constraint, _alterConstraintStatement.AlterMetaData);
			program.CatalogDeviceSession.UpdateCatalogObject(constraint);
			return null;
		}
    }
    
    public class AlterReferenceNode : AlterNode
    {		
		public AlterReferenceNode() : base(){}
		public AlterReferenceNode(Schema.Reference reference) : base()
		{
			_reference = reference;
		}
		
		private Schema.Reference _reference;

		// AlterReferenceStatement
		protected AlterReferenceStatement _alterReferenceStatement;
		public AlterReferenceStatement AlterReferenceStatement
		{
			get { return _alterReferenceStatement; }
			set { _alterReferenceStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan plan, string objectName)
		{
			Schema.Object objectValue = base.FindObject(plan, objectName);
			if (!(objectValue is Schema.Reference))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotReference, objectName);
			return objectValue;
		}
		
		public override void BindToProcess(Plan plan)
		{
			Schema.Reference reference = (Schema.Reference)FindObject(plan, _alterReferenceStatement.ReferenceName);
			plan.CheckRight(reference.GetRight(Schema.RightNames.Alter));
			base.BindToProcess (plan);
		}

		public override object InternalExecute(Program program)
		{
			if (_reference == null)
				_reference = (Schema.Reference)FindObject(program.Plan, _alterReferenceStatement.ReferenceName);
			
			program.CatalogDeviceSession.AlterMetaData(_reference, _alterReferenceStatement.AlterMetaData);

			_reference.SourceTable.SetShouldReinferReferences(program.CatalogDeviceSession);
			_reference.TargetTable.SetShouldReinferReferences(program.CatalogDeviceSession);

			program.Catalog.UpdatePlanCacheTimeStamp();
			program.Catalog.UpdateDerivationTimeStamp();
			
			program.CatalogDeviceSession.UpdateCatalogObject(_reference);
			
			return null;
		}
    }
    
    public class AlterServerNode : AlterNode
    {		
		// AlterServerStatement
		protected AlterServerStatement _alterServerStatement;
		public AlterServerStatement AlterServerStatement
		{
			get { return _alterServerStatement; }
			set { _alterServerStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan plan, string objectName)
		{
			Schema.Object objectValue = base.FindObject(plan, objectName);
			if (!(objectValue is Schema.ServerLink))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotServer, objectName);
			return objectValue;
		}
		
		public override void BindToProcess(Plan plan)
		{
			Schema.ServerLink serverLink = (Schema.ServerLink)FindObject(plan, _alterServerStatement.ServerName);
			plan.CheckRight(serverLink.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			Schema.ServerLink server = (Schema.ServerLink)FindObject(program.Plan, _alterServerStatement.ServerName);
			
			// TODO: Prevent altering the server link with active sessions? (May not be necessary, the active sessions would just continue to use the current settings)
			program.CatalogDeviceSession.AlterMetaData(server, _alterServerStatement.AlterMetaData);
			server.ResetServerLink();
			server.ApplyMetaData();
			program.CatalogDeviceSession.UpdateCatalogObject(server);

			return null;
		}
    }
    
    public class AlterDeviceNode : AlterNode
    {		
		// AlterDeviceStatement
		protected AlterDeviceStatement _alterDeviceStatement;
		public AlterDeviceStatement AlterDeviceStatement
		{
			get { return _alterDeviceStatement; }
			set { _alterDeviceStatement = value; }
		}
		
		protected override Schema.Object FindObject(Plan plan, string objectName)
		{
			Schema.Object objectValue = base.FindObject(plan, objectName);
			if (!(objectValue is Schema.Device))
				throw new RuntimeException(RuntimeException.Codes.ObjectNotDevice, objectName);
			return objectValue;
		}
		
		private Schema.ScalarType FindScalarType(Program program, string scalarTypeName)
		{
			Schema.IDataType dataType = Compiler.CompileTypeSpecifier(program.Plan, new ScalarTypeSpecifier(scalarTypeName));
			if (!(dataType is Schema.ScalarType))
				throw new CompilerException(CompilerException.Codes.ScalarTypeExpected);
			return (Schema.ScalarType)dataType;
		}

		public override void BindToProcess(Plan plan)
		{
			Schema.Device device = (Schema.Device)FindObject(plan, _alterDeviceStatement.DeviceName);
			plan.CheckRight(device.GetRight(Schema.RightNames.Alter));
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			Schema.Device device = (Schema.Device)FindObject(program.Plan, _alterDeviceStatement.DeviceName);
			program.ServerProcess.EnsureDeviceStarted(device);
			
			if (_alterDeviceStatement.AlterClassDefinition != null)
				program.CatalogDeviceSession.AlterClassDefinition(device.ClassDefinition, _alterDeviceStatement.AlterClassDefinition, device);

			foreach (DropDeviceScalarTypeMap deviceScalarTypeMap in _alterDeviceStatement.DropDeviceScalarTypeMaps)
			{
				Schema.ScalarType scalarType = FindScalarType(program, deviceScalarTypeMap.ScalarTypeName);
				Schema.DeviceScalarType deviceScalarType = device.ResolveDeviceScalarType(program.Plan, scalarType);
				if (deviceScalarType != null)
				{
					Schema.CatalogObjectHeaders headers = program.CatalogDeviceSession.SelectGeneratedObjects(deviceScalarType.ID);
					for (int index = 0; index < headers.Count; index++)
					{
						Schema.DeviceOperator operatorValue = program.CatalogDeviceSession.ResolveCatalogObject(headers[index].ID) as Schema.DeviceOperator;
						CheckNoDependents(program, operatorValue);
						program.CatalogDeviceSession.DropDeviceOperator(operatorValue);
					}
					
					CheckNoDependents(program, deviceScalarType);
					program.CatalogDeviceSession.DropDeviceScalarType(deviceScalarType);
				}
			}
				
			foreach (DropDeviceOperatorMap deviceOperatorMap in _alterDeviceStatement.DropDeviceOperatorMaps)
			{
				Schema.Operator operatorValue = Compiler.ResolveOperatorSpecifier(program.Plan, deviceOperatorMap.OperatorSpecifier);
				Schema.DeviceOperator deviceOperator = device.ResolveDeviceOperator(program.Plan, operatorValue);
				if (deviceOperator != null)
				{
					CheckNoDependents(program, deviceOperator);
					program.CatalogDeviceSession.DropDeviceOperator(deviceOperator);
				}
			}
			
			foreach (AlterDeviceScalarTypeMap deviceScalarTypeMap in _alterDeviceStatement.AlterDeviceScalarTypeMaps)
			{
				Schema.ScalarType scalarType = FindScalarType(program, deviceScalarTypeMap.ScalarTypeName);
				Schema.DeviceScalarType deviceScalarType = device.ResolveDeviceScalarType(program.Plan, scalarType);
				if (deviceScalarTypeMap.AlterClassDefinition != null)
					program.CatalogDeviceSession.AlterClassDefinition(deviceScalarType.ClassDefinition, deviceScalarTypeMap.AlterClassDefinition, deviceScalarType);
				program.CatalogDeviceSession.AlterMetaData(deviceScalarType, deviceScalarTypeMap.AlterMetaData);
				program.CatalogDeviceSession.UpdateCatalogObject(deviceScalarType);
			}
				
			foreach (AlterDeviceOperatorMap deviceOperatorMap in _alterDeviceStatement.AlterDeviceOperatorMaps)
			{
				Schema.Operator operatorValue = Compiler.ResolveOperatorSpecifier(program.Plan, deviceOperatorMap.OperatorSpecifier);
				Schema.DeviceOperator deviceOperator = device.ResolveDeviceOperator(program.Plan, operatorValue);
				if (deviceOperatorMap.AlterClassDefinition != null)
					program.CatalogDeviceSession.AlterClassDefinition(deviceOperator.ClassDefinition, deviceOperatorMap.AlterClassDefinition, deviceOperator);
				program.CatalogDeviceSession.AlterMetaData(deviceOperator, deviceOperatorMap.AlterMetaData);
				program.CatalogDeviceSession.UpdateCatalogObject(deviceOperator);
			}
				
			foreach (DeviceScalarTypeMap deviceScalarTypeMap in _alterDeviceStatement.CreateDeviceScalarTypeMaps)
			{
				Schema.DeviceScalarType scalarType = Compiler.CompileDeviceScalarTypeMap(program.Plan, device, deviceScalarTypeMap);
				if (!program.ServerProcess.InLoadingContext())
				{
					Schema.DeviceScalarType existingScalarType = device.ResolveDeviceScalarType(program.Plan, scalarType.ScalarType);
					if ((existingScalarType != null) && existingScalarType.IsGenerated)
					{
						CheckNoDependents(program, existingScalarType); // TODO: These could be updated to point to the new scalar type
						program.CatalogDeviceSession.DropDeviceScalarType(existingScalarType);
					}
				}
				program.CatalogDeviceSession.CreateDeviceScalarType(scalarType);
				
				if (!program.ServerProcess.InLoadingContext())
					Compiler.CompileDeviceScalarTypeMapOperatorMaps(program.Plan, device, scalarType);
			}
				
			foreach (DeviceOperatorMap deviceOperatorMap in _alterDeviceStatement.CreateDeviceOperatorMaps)
			{
				Schema.DeviceOperator deviceOperator = Compiler.CompileDeviceOperatorMap(program.Plan, device, deviceOperatorMap);
				if (!program.ServerProcess.InLoadingContext())
				{
					Schema.DeviceOperator existingDeviceOperator = device.ResolveDeviceOperator(program.Plan, deviceOperator.Operator);
					if ((existingDeviceOperator != null) && existingDeviceOperator.IsGenerated)
					{
						CheckNoDependents(program, existingDeviceOperator); // TODO: These could be updated to point to the new operator
						program.CatalogDeviceSession.DropDeviceOperator(existingDeviceOperator);
					}
				}
				
				program.CatalogDeviceSession.CreateDeviceOperator(deviceOperator);
			}
				
			if (_alterDeviceStatement.ReconciliationSettings != null)
			{
				if (_alterDeviceStatement.ReconciliationSettings.ReconcileModeSet)
					program.CatalogDeviceSession.SetDeviceReconcileMode(device, _alterDeviceStatement.ReconciliationSettings.ReconcileMode);
				
				if (_alterDeviceStatement.ReconciliationSettings.ReconcileMasterSet)
					program.CatalogDeviceSession.SetDeviceReconcileMaster(device, _alterDeviceStatement.ReconciliationSettings.ReconcileMaster);
			}
				
			program.CatalogDeviceSession.AlterMetaData(device, _alterDeviceStatement.AlterMetaData);
			program.CatalogDeviceSession.UpdateCatalogObject(device);

			return null;
		}
    }
    
	public abstract class DropObjectNode : DDLNode
	{
		protected void CheckNotSystem(Program program, Schema.Object objectValue)
		{
			if (objectValue.IsSystem && !program.Plan.User.IsSystemUser())
				throw new RuntimeException(RuntimeException.Codes.ObjectIsSystem, objectValue.Name);
		}
	}
	
	public abstract class DropTableVarNode : DropObjectNode
	{
		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		protected void DropSourceReferences(Program program, Schema.TableVar tableVar)
		{
			BlockNode blockNode = new BlockNode();
			if (tableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in tableVar.References)
					if (reference.SourceTable.Equals(tableVar) && !reference.IsDerived)
					blockNode.Nodes.Add(Compiler.Compile(program.Plan, reference.EmitDropStatement(EmitMode.ForCopy)));
				
			blockNode.Execute(program);
		}
		
		protected void DropEventHandlers(Program program, Schema.TableVar tableVar)
		{
			BlockNode blockNode = new BlockNode();
			List<Schema.DependentObjectHeader> headers = program.CatalogDeviceSession.SelectObjectDependents(tableVar.ID, false);
			for (int index = 0; index < headers.Count; index++)
			{
				Schema.DependentObjectHeader header = headers[index];
				if ((header.ObjectType == "TableVarEventHandler") || (header.ObjectType == "TableVarColumnEventHandler"))
					blockNode.Nodes.Add(Compiler.Compile(program.Plan, ((Schema.EventHandler)program.CatalogDeviceSession.ResolveObject(header.ID)).EmitDropStatement(EmitMode.ForCopy)));
			}
			
			blockNode.Execute(program);
		}
		
		public static void RemoveDeferredConstraintChecks(Program program, Schema.TableVar tableVar)
		{
			program.ServerProcess.ServerSession.Server.RemoveDeferredConstraintChecks(tableVar);
		}
	}
    
    public class DropTableNode : DropTableVarNode
    {
		public DropTableNode() : base(){}
		public DropTableNode(Schema.BaseTableVar table) : base()
		{
			Table = table;
		}
		
		public DropTableNode(Schema.BaseTableVar table, bool shouldAffectDerivationTimeStamp) : base()
		{
			Table = table;
			ShouldAffectDerivationTimeStamp = shouldAffectDerivationTimeStamp;
		}
						
		// Table
		private Schema.BaseTableVar _table;
		public Schema.BaseTableVar Table 
		{ 
			get { return _table; } 
			set 
			{ 
				_table = value; 
				_device = _table == null ? null : _table.Device;
			}
		}
		
		public override void DeterminePotentialDevice(Plan plan)
		{
			_potentialDevice = _table.Device;
		}

		public override void DetermineDevice(Plan plan)
		{
			base.DetermineDevice(plan);
			DeviceSupported = false;
		}

		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(_table.GetRight(Schema.RightNames.Drop));
			if (_device != null)
				plan.CheckRight(_device.GetRight(Schema.RightNames.DropStore));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				CheckNotSystem(program, _table);
				program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, _table);
				DropSourceReferences(program, _table);
				DropEventHandlers(program, _table);
				CheckNoDependents(program, _table);

				RemoveDeferredConstraintChecks(program, _table);

				program.CatalogDeviceSession.DropTable(_table);

				if (ShouldAffectDerivationTimeStamp)
				{
					program.Catalog.UpdateCacheTimeStamp();
                    program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: drop table {1}", program.Catalog.CacheTimeStamp.ToString(), _table.Name));
					program.Catalog.UpdatePlanCacheTimeStamp();
					program.Catalog.UpdateDerivationTimeStamp();
				}
				return null;
			}
		}
    }
    
    public class DropViewNode : DropTableVarNode
    {		
		public DropViewNode() : base() {}
		public DropViewNode(Schema.DerivedTableVar derivedTableVar) : base()
		{
			_derivedTableVar = derivedTableVar;
		}
		
		public DropViewNode(Schema.DerivedTableVar derivedTableVar, bool shouldAffectDerivationTimeStamp)
		{
			_derivedTableVar = derivedTableVar;
			ShouldAffectDerivationTimeStamp = shouldAffectDerivationTimeStamp;
		}
		
		private Schema.DerivedTableVar _derivedTableVar;
		public Schema.DerivedTableVar DerivedTableVar
		{
			get { return _derivedTableVar; }
			set { _derivedTableVar = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(_derivedTableVar.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				CheckNotSystem(program, _derivedTableVar);
				program.ServerProcess.ServerSession.Server.ATDevice.ReportTableChange(program.ServerProcess, _derivedTableVar);
				DropSourceReferences(program, _derivedTableVar);
				DropEventHandlers(program, _derivedTableVar);
				CheckNoDependents(program, _derivedTableVar);

				RemoveDeferredConstraintChecks(program, _derivedTableVar);

				program.CatalogDeviceSession.DropView(_derivedTableVar);

				if (ShouldAffectDerivationTimeStamp)
				{
					program.Catalog.UpdateCacheTimeStamp();
                    program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: drop view {1}", program.Catalog.CacheTimeStamp.ToString(), _derivedTableVar.Name));
					program.Catalog.UpdatePlanCacheTimeStamp();
					program.Catalog.UpdateDerivationTimeStamp();
				}

				return null;
			}
		}
    }


    public class DropScalarTypeNode : DropObjectNode
	{		
		private Schema.ScalarType _scalarType;
		public Schema.ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}

		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(_scalarType.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}
		
		private void DropChildObjects(Program program, Schema.ScalarType scalarType)
		{
			AlterScalarTypeStatement statement = new AlterScalarTypeStatement();
			statement.ScalarTypeName = scalarType.Name;

			// Drop Constraints
			foreach (Schema.ScalarTypeConstraint constraint in scalarType.Constraints)
				statement.DropConstraints.Add(new DropConstraintDefinition(constraint.Name));
				
			// Drop Default
			if (scalarType.Default != null)
				statement.Default = new DropDefaultDefinition();
				
			// Drop Specials
			foreach (Schema.Special special in scalarType.Specials)
				statement.DropSpecials.Add(new DropSpecialDefinition(special.Name));
				
			Compiler.Compile(program.Plan, statement).Execute(program);
		}
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				CheckNotSystem(program, _scalarType);
				
				DropChildObjects(program, _scalarType);

				// If the ScalarType has dependents, prevent its destruction
				DropGeneratedDependents(program, _scalarType);
				CheckNoDependents(program, _scalarType);
				
				program.CatalogDeviceSession.DropScalarType(_scalarType);
				program.Catalog.OperatorResolutionCache.Clear(_scalarType, _scalarType);

				if (ShouldAffectDerivationTimeStamp)
				{
					program.Catalog.UpdateCacheTimeStamp();
					program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: drop scalar {1}", program.Catalog.CacheTimeStamp.ToString(), _scalarType.Name));
					program.Catalog.UpdatePlanCacheTimeStamp();
					program.Catalog.UpdateDerivationTimeStamp();
				}
				return null;
			}
		}
	}
    
	public class DropOperatorNode : DropObjectNode
	{
		public DropOperatorNode() : base(){}
		public DropOperatorNode(Schema.Operator operatorValue) : base()
		{
			_dropOperator = operatorValue;
		}
		
		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		// OperatorSpecifier
		protected OperatorSpecifier _operatorSpecifier;
		public OperatorSpecifier OperatorSpecifier
		{
			get { return _operatorSpecifier; }
			set { _operatorSpecifier = value; }
		}
		
		// Operator
		protected Schema.Operator _dropOperator;
		public Schema.Operator DropOperator
		{
			get { return _dropOperator; }
			set { _dropOperator = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			if (_dropOperator == null)
				_dropOperator = Compiler.ResolveOperatorSpecifier(plan, _operatorSpecifier);
			plan.CheckRight(_dropOperator.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			if (_dropOperator == null)
				_dropOperator = Compiler.ResolveOperatorSpecifier(program.Plan, _operatorSpecifier);
				
			CheckNotSystem(program, _dropOperator);
			program.ServerProcess.ServerSession.Server.ATDevice.ReportOperatorChange(program.ServerProcess, _dropOperator);

			DropGeneratedSorts(program, _dropOperator);
			CheckNoDependents(program, _dropOperator);
			
			program.CatalogDeviceSession.DropOperator(_dropOperator);
			
			if (ShouldAffectDerivationTimeStamp)
			{
				program.Catalog.UpdateCacheTimeStamp();
                program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: drop operator {1}", program.Catalog.CacheTimeStamp.ToString(), _operatorSpecifier));					
				program.Catalog.UpdatePlanCacheTimeStamp();
			}
			return null;
		}
	}
    
	public class DropConstraintNode : DropObjectNode
	{		
		private Schema.CatalogConstraint _constraint;
		public Schema.CatalogConstraint Constraint
		{
			get { return _constraint; }
			set { _constraint = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(_constraint.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				CheckNotSystem(program, _constraint);
				CheckNoDependents(program, _constraint);
					
				program.ServerProcess.ServerSession.Server.RemoveCatalogConstraintCheck(_constraint);
				
				program.CatalogDeviceSession.DropConstraint(_constraint);

				return null;
			}
		}
	}
    
	public class DropReferenceNode : DropObjectNode
	{		
		public DropReferenceNode() : base(){}
		public DropReferenceNode(Schema.Reference reference) : base()
		{
			_reference = reference;
		}

		private bool _shouldAffectDerivationTimeStamp = true;
		public bool ShouldAffectDerivationTimeStamp
		{
			get { return _shouldAffectDerivationTimeStamp; }
			set { _shouldAffectDerivationTimeStamp = value; }
		}
		
		private Schema.Reference _reference;
		
		private string _referenceName;
		public string ReferenceName
		{
			get { return _referenceName; }
			set { _referenceName = value; }
		}

		public override void BindToProcess(Plan plan)
		{
			if (_reference == null)
				_reference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(plan, _referenceName);
			plan.CheckRight(_reference.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}
		
		public override object InternalExecute(Program program)
		{
			if (_reference == null)
				_reference = (Schema.Reference)Compiler.ResolveCatalogIdentifier(program.Plan, _referenceName);

			lock (program.Catalog)
			{
				CheckNotSystem(program, _reference);
				CheckNoDependents(program, _reference);
				
				program.CatalogDeviceSession.DropReference(_reference);

				if (ShouldAffectDerivationTimeStamp)
				{
					program.Catalog.UpdateCacheTimeStamp();
					program.ServerProcess.ServerSession.Server.LogMessage(String.Format("Catalog CacheTimeStamp updated to {0}: drop  reference {1}", program.Catalog.CacheTimeStamp.ToString(), _referenceName));
					program.Catalog.UpdatePlanCacheTimeStamp();
					program.Catalog.UpdateDerivationTimeStamp();
				}
				return null;
			}
		}
	}
    
	public class DropServerNode : DropObjectNode
	{		
		private Schema.ServerLink _serverLink;
		public Schema.ServerLink ServerLink
		{
			get { return _serverLink; }
			set { _serverLink = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(_serverLink.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				// TODO: Prevent dropping when server sessions are active
				// TODO: Drop server link users
				
				CheckNotSystem(program, _serverLink);
				CheckNoDependents(program, _serverLink);
				
				program.CatalogDeviceSession.DeleteCatalogObject(_serverLink);
			}
			
			return null;
		}
	}
    
	public class DropDeviceNode : DropObjectNode
	{
		private Schema.Device _dropDevice;
		public Schema.Device DropDevice
		{
			get { return _dropDevice; }
			set { _dropDevice = value; }
		}
		
		public override void BindToProcess(Plan plan)
		{
			plan.CheckRight(_dropDevice.GetRight(Schema.RightNames.Drop));
			base.BindToProcess(plan);
		}

		public override object InternalExecute(Program program)
		{
			lock (program.Catalog)
			{
				program.CatalogDeviceSession.StopDevice(_dropDevice);
				
				CheckNotSystem(program, _dropDevice);
				CheckNoBaseTableVarDependents(program, _dropDevice);

				DropDeviceMaps(program, _dropDevice);				
				CheckNoDependents(program, _dropDevice);
				
				program.CatalogDeviceSession.DropDevice(_dropDevice);

				return null;
			}
		}
	}
}
