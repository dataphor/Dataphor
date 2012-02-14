/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define LOGDDLINSTRUCTIONS

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class ServerCatalogDevice : CatalogDevice
	{
		public ServerCatalogDevice(int iD, string name) : base(iD, name) { }
		
		private CatalogStore _store;
		internal CatalogStore Store
		{
			get
			{
				Error.AssertFail(_store != null, "Server is configured as a repository and has no catalog store.");
				return _store;
			}
		}
		
		internal new Users UsersCache { get { return base.UsersCache; } }
		
		internal new Dictionary<int, string> CatalogIndex { get { return base.CatalogIndex; } }
		
		internal new NameResolutionCache NameCache { get { return base.NameCache; } }
		
		internal new NameResolutionCache OperatorNameCache { get { return base.OperatorNameCache; } }

		public int MaxStoreConnections
		{
			get { return _store.MaxConnections; }
			set { _store.MaxConnections = value; }
		}

		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new ServerCatalogDeviceSession(this, serverProcess, deviceSessionInfo);
		}

		protected override void InternalStart(ServerProcess process)
		{
			base.InternalStart(process);
			if (!process.ServerSession.Server.IsEngine)
			{
				_store = new CatalogStore();
				_store.StoreClassName = ((Server.Server)process.ServerSession.Server).GetCatalogStoreClassName();
				_store.StoreConnectionString = ((Server.Server)process.ServerSession.Server).GetCatalogStoreConnectionString();
				_store.Initialize(process.ServerSession.Server);
			}
		}

		protected override DevicePlanNode InternalPrepare(DevicePlan devicePlan, PlanNode planNode)
		{
			CatalogDevicePlan localDevicePlan = (CatalogDevicePlan)devicePlan;
			CatalogDevicePlanNode devicePlanNode = new CatalogDevicePlanNode(planNode);
			TranslatePlanNode(localDevicePlan, devicePlanNode, planNode);
			if (planNode is TableNode)
				TranslateOrder(localDevicePlan, devicePlanNode, (TableNode)planNode);
			
			if (localDevicePlan.IsSupported)
				return devicePlanNode;
			else
				return base.InternalPrepare(devicePlan, planNode);
		}
		
		private void PopulateServerSettings(Program program, NativeTable nativeTable, Row row)
		{
			DAE.Server.Server server = (DAE.Server.Server)program.ServerProcess.ServerSession.Server;
			row[0] = server.Name;
			row[1] = GetType().Assembly.GetName().Version.ToString();
			row[2] = server.LogErrors;
			row[3] = server.Catalog.TimeStamp;
			row[4] = server.CacheTimeStamp;
			row[5] = server.PlanCacheTimeStamp;
			row[6] = server.DerivationTimeStamp;
			row[7] = server.InstanceDirectory;
			row[8] = server.LibraryDirectory;
			row[9] = server.IsEngine;
			row[10] = server.MaxConcurrentProcesses;
			row[11] = server.ProcessWaitTimeout;
			row[12] = server.ProcessTerminationTimeout;
			row[13] = server.PlanCache.Size;
			nativeTable.Insert(program.ValueManager, row);
		}

		private void PopulateLoadedLibraries(Program program, NativeTable nativeTable, Row row)
		{
			List<string> libraryNames = ((ServerCatalogDeviceSession)program.CatalogDeviceSession).SelectLoadedLibraries();
			for (int index = 0; index < libraryNames.Count; index++)
			{
				row[0] = libraryNames[index];
				nativeTable.Insert(program.ValueManager, row);
			}
		}
		
		private void PopulateLibraryOwners(Program program, NativeTable nativeTable, Row row)
		{
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SelectLibraryOwners(program, nativeTable, row);
		}

		private void PopulateLibraryVersions(Program program, NativeTable nativeTable, Row row)
		{
			((ServerCatalogDeviceSession)program.CatalogDeviceSession).SelectLibraryVersions(program, nativeTable, row);
		}

		protected override void InternalPopulateTableVar(Program program, CatalogHeader header, Row row)
		{
			switch (header.TableVar.Name)
			{
				case "System.ServerSettings" : PopulateServerSettings(program, header.NativeTable, row); break;
				case "System.LibraryOwners" : PopulateLibraryOwners(program, header.NativeTable, row); break;
				case "System.LibraryVersions" : PopulateLibraryVersions(program, header.NativeTable, row); break;
				case "System.LoadedLibraries" : PopulateLoadedLibraries(program, header.NativeTable, row); break;
				default: base.InternalPopulateTableVar(program, header, row); break;
			}
		}

		protected void TranslateOrderNode(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, OrderNode orderNode)
		{
			TranslatePlanNode(devicePlan, devicePlanNode, orderNode.SourceNode);
			if (devicePlan.IsSupported)
			{
				orderNode.CursorType = orderNode.SourceNode.CursorType;
				orderNode.RequestedCursorType = orderNode.SourceNode.RequestedCursorType;
				orderNode.CursorCapabilities = orderNode.SourceNode.CursorCapabilities;
				orderNode.CursorIsolation = orderNode.SourceNode.CursorIsolation;
			}
		}
		
		protected void GetViewDefinition(string tableVarName, StringBuilder statement, StringBuilder whereCondition)
		{
			switch (tableVarName)
			{
				case "Users" :
					statement.Append("select ID, Name from DAEUsers");
				break;
					
				case "Operators" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, J.IsSystem, J.IsGenerated, O.OperatorName, O.Signature, O.Locator, O.Line, O.LinePos
	from DAEOperators O
		join DAECatalogObjects C on O.ID = C.ID
		join DAEObjects J on O.ID = J.ID
						"
					);
				break;
					
				case "ScalarTypes" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type = 'ScalarType'");
				break;
				
				case "Sorts" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type = 'Sort'");
				break;
					
				case "TableVars" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type in ('BaseTableVar', 'DerivedTableVar')");
				break;
					
				case "BaseTableVars" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type = 'BaseTableVar'");
				break;
					
				case "DerivedTableVars" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type = 'DerivedTableVar'");
				break;
					
				case "References" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type = 'Reference'");
				break;
					
				case "CatalogConstraints" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					whereCondition.Append("O.Type = 'CatalogConstraint'");
				break;
					
				case "Roles" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAECatalogObjects C
		join DAEObjects O on C.ID = O.ID
						"
					);
					whereCondition.Append("O.Type = 'Role'");
				break;
					
				case "UserRoles" :
					statement.Append
					(
						@"
select UR.User_ID, R.Name Role_Name
	from DAEUserRoles UR
		join DAEObjects R on UR.Role_ID = R.ID
						"
					);
				break;
					
				case "RoleRightAssignments" :
					statement.Append
					(
						@"
select O.Name Role_Name, A.Right_Name, A.IsGranted
	from DAERoleRightAssignments A
		join DAEObjects O on A.Role_ID = O.ID
						"
					);
				break;
					
				case "Devices" :
					statement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated, D.ReconciliationMaster, D.ReconciliationMode
	from DAEDevices D
		join DAECatalogObjects C on D.ID = C.ID
		join DAEObjects O on D.ID = O.ID
						"
					);
				break;
					
				case "DeviceUsers" :
					statement.Append
					(
						@"
select DU.User_ID, C.Name Device_Name, DU.UserID, DU.ConnectionParameters
	from DAEDeviceUsers DU
		join DAECatalogObjects C on DU.Device_ID = C.ID
						"
					);
				break;
					
				case "DeviceScalarTypes" :
					statement.Append
					(
						@"
select O.Name, D.Name Device_Name, S.Name ScalarType_Name
	from DAEDeviceObjects DAEDO
		join DAEObjects O on DAEDO.ID = O.ID
		join DAECatalogObjects D on DAEDO.Device_ID = D.ID
		join DAECatalogObjects S on DAEDO.Mapped_Object_ID = S.ID
			join DAEObjects SO on S.ID = SO.ID
						"
					);
					whereCondition.Append("SO.Type = 'ScalarType'");
				break;
					
				case "DeviceOperators" :
					statement.Append
					(
						@"
select O.Name, D.Name Device_Name, S.Name Operator_Name
	from DAEDeviceObjects DAEDO
		join DAEObjects O on DAEDO.ID = O.ID
		join DAECatalogObjects D on DAEDO.Device_ID = D.ID
		join DAECatalogObjects S on DAEDO.Mapped_Object_ID = S.ID
			join DAEObjects SO on S.ID = SO.ID
						"
					);
					whereCondition.Append("SO.Type in ('Operator', 'AggregateOperator')");
				break;
				
				default :
					Error.Fail("Could not build view definition for catalog store table '{0}'.", tableVarName);
				break;
			}
		}
		
		protected void TranslateBaseTableVarNode(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, BaseTableVarNode baseTableVarNode)
		{
			Tag tag = MetaData.GetTag(baseTableVarNode.TableVar.MetaData, "Catalog.CacheLevel");
			if (tag != Tag.None)
			{
				if ((tag.Value == "StoreTable") || (tag.Value == "StoreView"))
				{
					devicePlan.TableContext = baseTableVarNode.TableVar;
					baseTableVarNode.CursorType = CursorType.Dynamic;
					baseTableVarNode.RequestedCursorType = devicePlan.Plan.CursorContext.CursorType;
					baseTableVarNode.CursorCapabilities = CursorCapability.Navigable | (devicePlan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
					baseTableVarNode.CursorIsolation = devicePlan.Plan.CursorContext.CursorIsolation;
					baseTableVarNode.Order = Compiler.OrderFromKey(devicePlan.Plan, Compiler.FindClusteringKey(devicePlan.Plan, baseTableVarNode.TableVar));
					if (tag.Value == "StoreTable")
						devicePlanNode.Statement.AppendFormat("select * from DAE{0}", Schema.Object.Unqualify(baseTableVarNode.TableVar.Name));
					else
						GetViewDefinition(Schema.Object.Unqualify(baseTableVarNode.TableVar.Name), devicePlanNode.Statement, devicePlanNode.WhereCondition);
				}
				else
					devicePlan.IsSupported = false;
			}
			else
				devicePlan.IsSupported = false;
		}
		
		protected string GetInstructionKeyword(string instruction)
		{
			switch (instruction)
			{
				case Instructions.And: return "and";
				case Instructions.Equal: return "=";
				case Instructions.NotEqual: return "<>";
				case Instructions.Greater: return ">";
				case Instructions.InclusiveGreater: return ">=";
				case Instructions.Less: return "<";
				case Instructions.InclusiveLess: return "<=";
				case Instructions.Like: return "like";
			}
			
			Error.Fail("Unknown instruction '{0}' encountered in catalog device.", instruction);
			return String.Empty;
		}
		
		protected void TranslateScalarParameter(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, PlanNode planNode)
		{
			SQLType type = null;
			switch (planNode.DataType.Name)
			{
				case "System.String" :
				case "System.Name" :
				case "System.UserID" :
					type = new SQLStringType(200);
				break;
				
				case "System.Integer" :
					type = new SQLIntegerType(4);
				break;
				
				case "System.Boolean" :
					type = new SQLIntegerType(1);
				break;
			}
			
			if (type != null)
			{
				string parameterName = String.Format("P{0}", devicePlanNode.PlanParameters.Count + 1);
				devicePlanNode.PlanParameters.Add(new CatalogPlanParameter(new SQLParameter(parameterName, type, null), planNode));
				devicePlanNode.WhereCondition.AppendFormat("@{0}", parameterName);
			}
			else
				devicePlan.IsSupported = false;
		}

		protected void TranslateExpression(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, PlanNode planNode)
		{
			InstructionNodeBase instructionNode = planNode as InstructionNodeBase;
			if (instructionNode != null)
			{
				if ((instructionNode.DataType != null) && (instructionNode.Operator != null))
				{
					switch (Schema.Object.Unqualify(instructionNode.Operator.OperatorName))
					{
						case Instructions.And:
						case Instructions.Equal:
						case Instructions.NotEqual:
						case Instructions.Greater:
						case Instructions.InclusiveGreater:
						case Instructions.Less:
						case Instructions.InclusiveLess:
						case Instructions.Like:
							TranslateExpression(devicePlan, devicePlanNode, instructionNode.Nodes[0]);
							devicePlanNode.WhereCondition.AppendFormat(" {0} ", GetInstructionKeyword(Schema.Object.Unqualify(instructionNode.Operator.OperatorName)));
							TranslateExpression(devicePlan, devicePlanNode, instructionNode.Nodes[1]);
						return;
						
						case "ReadValue" : TranslateExpression(devicePlan, devicePlanNode, instructionNode.Nodes[0]); return;
						
						default: devicePlan.IsSupported = false; return;
					}
				}
			}
			
			ValueNode valueNode = planNode as ValueNode;
			if (valueNode != null)
			{
				TranslateScalarParameter(devicePlan, devicePlanNode, planNode);
				return;
			}
			
			StackReferenceNode stackReferenceNode = planNode as StackReferenceNode;
			if (stackReferenceNode != null)
			{
				TranslateScalarParameter(devicePlan, devicePlanNode, new StackReferenceNode(stackReferenceNode.DataType, stackReferenceNode.Location - 1));
				return;
			}
			
			StackColumnReferenceNode stackColumnReferenceNode = planNode as StackColumnReferenceNode;
			if (stackColumnReferenceNode != null)
			{
				if (stackColumnReferenceNode.Location == 0)
					if (devicePlan.TableContext != null)
						devicePlanNode.WhereCondition.Append(Schema.Object.EnsureUnrooted(MetaData.GetTag(devicePlan.TableContext.Columns[stackColumnReferenceNode.Identifier].MetaData, "Storage.Name", stackColumnReferenceNode.Identifier)));
					else
						devicePlanNode.WhereCondition.Append(Schema.Object.EnsureUnrooted(stackColumnReferenceNode.Identifier));
				else
					TranslateScalarParameter(devicePlan, devicePlanNode, new StackColumnReferenceNode(stackColumnReferenceNode.Identifier, stackColumnReferenceNode.DataType, stackColumnReferenceNode.Location - 1));

				return;
			}
			
			devicePlan.IsSupported = false;
		}
		
		protected void TranslateRestrictNode(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, RestrictNode restrictNode)
		{
			if ((restrictNode.SourceNode is BaseTableVarNode) && (restrictNode.Nodes[1] is InstructionNodeBase))
			{
				TranslateBaseTableVarNode(devicePlan, devicePlanNode, (BaseTableVarNode)restrictNode.SourceNode);
				if (devicePlan.IsSupported)
				{
					if (devicePlanNode.WhereCondition.Length > 0)
					{
						devicePlanNode.WhereCondition.Insert(0, "(");
						devicePlanNode.WhereCondition.AppendFormat(") and ");
					}
					TranslateExpression(devicePlan, devicePlanNode, restrictNode.Nodes[1]);

					if (devicePlan.IsSupported)
					{
						restrictNode.CursorType = restrictNode.SourceNode.CursorType;
						restrictNode.RequestedCursorType = restrictNode.SourceNode.RequestedCursorType;
						restrictNode.CursorCapabilities = restrictNode.SourceNode.CursorCapabilities;
						restrictNode.CursorIsolation = restrictNode.SourceNode.CursorIsolation;
						restrictNode.Order = restrictNode.SourceNode.Order;
					}
				}
			}
			else
				devicePlan.IsSupported = false;
		}
		
		protected void TranslatePlanNode(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, PlanNode planNode)
		{
			if (planNode is BaseTableVarNode) TranslateBaseTableVarNode(devicePlan, devicePlanNode, (BaseTableVarNode)planNode);
			else if (planNode is RestrictNode) TranslateRestrictNode(devicePlan, devicePlanNode, (RestrictNode)planNode);
			else if (planNode is OrderNode) TranslateOrderNode(devicePlan, devicePlanNode, (OrderNode)planNode);
			else devicePlan.IsSupported = false;
		}
		
		protected void TranslateOrder(CatalogDevicePlan devicePlan, CatalogDevicePlanNode devicePlanNode, TableNode tableNode)
		{
			if (devicePlanNode.WhereCondition.Length > 0)
				devicePlanNode.Statement.AppendFormat(" where {0}", devicePlanNode.WhereCondition.ToString());
				
			if ((tableNode.Order != null) && (tableNode.Order.Columns.Count > 0))
			{
				devicePlanNode.Statement.AppendFormat(" order by ");
				for (int index = 0; index < tableNode.Order.Columns.Count; index++)
				{
					OrderColumn orderColumn = tableNode.Order.Columns[index];
					if (index > 0)
						devicePlanNode.Statement.Append(", ");
					if (devicePlan.TableContext != null)
						devicePlanNode.Statement.Append(Schema.Object.EnsureUnrooted(MetaData.GetTag(devicePlan.TableContext.Columns[orderColumn.Column.Name].MetaData, "Storage.Name", orderColumn.Column.Name)));
					else
						devicePlanNode.Statement.Append(Schema.Object.EnsureUnrooted(orderColumn.Column.Name));
					if (!orderColumn.Ascending)
						devicePlanNode.Statement.Append(" desc");
				}
			}
		}
	}
}
