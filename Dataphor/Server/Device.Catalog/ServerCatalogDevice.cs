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
		public ServerCatalogDevice(int AID, string AName) : base(AID, AName) { }
		
		private CatalogStore FStore;
		internal CatalogStore Store
		{
			get
			{
				Error.AssertFail(FStore != null, "Server is configured as a repository and has no catalog store.");
				return FStore;
			}
		}
		
		internal new Users UsersCache { get { return base.UsersCache; } }
		
		internal new Dictionary<int, string> CatalogIndex { get { return base.CatalogIndex; } }
		
		internal new NameResolutionCache NameCache { get { return base.NameCache; } }
		
		internal new NameResolutionCache OperatorNameCache { get { return base.OperatorNameCache; } }

		public int MaxStoreConnections
		{
			get { return FStore.MaxConnections; }
			set { FStore.MaxConnections = value; }
		}

		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new ServerCatalogDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}

		protected override void InternalStart(ServerProcess AProcess)
		{
			base.InternalStart(AProcess);
			if (!AProcess.ServerSession.Server.IsEngine)
			{
				FStore = new CatalogStore();
				FStore.StoreClassName = ((Server.Server)AProcess.ServerSession.Server).GetCatalogStoreClassName();
				FStore.StoreConnectionString = ((Server.Server)AProcess.ServerSession.Server).GetCatalogStoreConnectionString();
				FStore.Initialize(AProcess.ServerSession.Server);
			}
		}

		protected override DevicePlanNode InternalPrepare(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			CatalogDevicePlan LDevicePlan = (CatalogDevicePlan)ADevicePlan;
			CatalogDevicePlanNode LDevicePlanNode = new CatalogDevicePlanNode(APlanNode);
			TranslatePlanNode(LDevicePlan, LDevicePlanNode, APlanNode);
			if (APlanNode is TableNode)
				TranslateOrder(LDevicePlan, LDevicePlanNode, (TableNode)APlanNode);
			
			if (LDevicePlan.IsSupported)
			{
				LDevicePlan.IsStorePlan = true;
				return LDevicePlanNode;
			}
			else
				return base.InternalPrepare(ADevicePlan, APlanNode);
		}
		
		protected override DevicePlan CreateDevicePlan(Plan APlan, PlanNode APlanNode)
		{
			var LDevicePlan = base.CreateDevicePlan(APlan, APlanNode);
			if (APlanNode.DeviceNode is CatalogDevicePlanNode)
				((CatalogDevicePlan)LDevicePlan).IsStorePlan = true;
			return LDevicePlan;
		}
		
		private void PopulateServerSettings(Program AProgram, INativeTable ANativeTable, Row ARow)
		{
			DAE.Server.Server LServer = (DAE.Server.Server)AProgram.ServerProcess.ServerSession.Server;
			ARow[0] = LServer.Name;
			ARow[1] = GetType().Assembly.GetName().Version.ToString();
			ARow[2] = LServer.LogErrors;
			ARow[3] = LServer.Catalog.TimeStamp;
			ARow[4] = LServer.CacheTimeStamp;
			ARow[5] = LServer.PlanCacheTimeStamp;
			ARow[6] = LServer.DerivationTimeStamp;
			ARow[7] = LServer.InstanceDirectory;
			ARow[8] = LServer.LibraryDirectory;
			ARow[9] = LServer.IsEngine;
			ARow[10] = LServer.MaxConcurrentProcesses;
			ARow[11] = LServer.ProcessWaitTimeout;
			ARow[12] = LServer.ProcessTerminationTimeout;
			ARow[13] = LServer.PlanCache.Size;
			ANativeTable.Insert(AProgram.ValueManager, ARow);
		}

		private void PopulateLoadedLibraries(Program AProgram, INativeTable ANativeTable, Row ARow)
		{
			List<string> LLibraryNames = ((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SelectLoadedLibraries();
			for (int LIndex = 0; LIndex < LLibraryNames.Count; LIndex++)
			{
				ARow[0] = LLibraryNames[LIndex];
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateLibraryOwners(Program AProgram, INativeTable ANativeTable, Row ARow)
		{
			((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SelectLibraryOwners(AProgram, ANativeTable, ARow);
		}

		private void PopulateLibraryVersions(Program AProgram, INativeTable ANativeTable, Row ARow)
		{
			((ServerCatalogDeviceSession)AProgram.CatalogDeviceSession).SelectLibraryVersions(AProgram, ANativeTable, ARow);
		}

		protected override void InternalPopulateTableVar(Program AProgram, CatalogHeader AHeader, Row ARow)
		{
			switch (AHeader.TableVar.Name)
			{
				case "System.ServerSettings" : PopulateServerSettings(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibraryOwners" : PopulateLibraryOwners(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LibraryVersions" : PopulateLibraryVersions(AProgram, AHeader.NativeTable, ARow); break;
				case "System.LoadedLibraries" : PopulateLoadedLibraries(AProgram, AHeader.NativeTable, ARow); break;
				default: base.InternalPopulateTableVar(AProgram, AHeader, ARow); break;
			}
		}

		protected void TranslateOrderNode(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, OrderNode AOrderNode)
		{
			TranslatePlanNode(ADevicePlan, ADevicePlanNode, AOrderNode.SourceNode);
			if (ADevicePlan.IsSupported)
			{
				AOrderNode.CursorType = AOrderNode.SourceNode.CursorType;
				AOrderNode.RequestedCursorType = AOrderNode.SourceNode.RequestedCursorType;
				AOrderNode.CursorCapabilities = AOrderNode.SourceNode.CursorCapabilities;
				AOrderNode.CursorIsolation = AOrderNode.SourceNode.CursorIsolation;
			}
		}
		
		protected void GetViewDefinition(string ATableVarName, StringBuilder AStatement, StringBuilder AWhereCondition)
		{
			switch (ATableVarName)
			{
				case "Users" :
					AStatement.Append("select ID, Name from DAEUsers");
				break;
					
				case "Operators" :
					AStatement.Append
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
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type = 'ScalarType'");
				break;
				
				case "Sorts" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type = 'Sort'");
				break;
					
				case "TableVars" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type in ('BaseTableVar', 'DerivedTableVar')");
				break;
					
				case "BaseTableVars" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type = 'BaseTableVar'");
				break;
					
				case "DerivedTableVars" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type = 'DerivedTableVar'");
				break;
					
				case "References" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type = 'Reference'");
				break;
					
				case "CatalogConstraints" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAEObjects O
		join DAECatalogObjects C on O.ID = C.ID
						"
					);
					AWhereCondition.Append("O.Type = 'CatalogConstraint'");
				break;
					
				case "Roles" :
					AStatement.Append
					(
						@"
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated
	from DAECatalogObjects C
		join DAEObjects O on C.ID = O.ID
						"
					);
					AWhereCondition.Append("O.Type = 'Role'");
				break;
					
				case "UserRoles" :
					AStatement.Append
					(
						@"
select UR.User_ID, R.Name Role_Name
	from DAEUserRoles UR
		join DAEObjects R on UR.Role_ID = R.ID
						"
					);
				break;
					
				case "RoleRightAssignments" :
					AStatement.Append
					(
						@"
select O.Name Role_Name, A.Right_Name, A.IsGranted
	from DAERoleRightAssignments A
		join DAEObjects O on A.Role_ID = O.ID
						"
					);
				break;
					
				case "Devices" :
					AStatement.Append
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
					AStatement.Append
					(
						@"
select DU.User_ID, C.Name Device_Name, DU.UserID, DU.ConnectionParameters
	from DAEDeviceUsers DU
		join DAECatalogObjects C on DU.Device_ID = C.ID
						"
					);
				break;
					
				case "DeviceScalarTypes" :
					AStatement.Append
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
					AWhereCondition.Append("SO.Type = 'ScalarType'");
				break;
					
				case "DeviceOperators" :
					AStatement.Append
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
					AWhereCondition.Append("SO.Type in ('Operator', 'AggregateOperator')");
				break;
				
				default :
					Error.Fail("Could not build view definition for catalog store table '{0}'.", ATableVarName);
				break;
			}
		}
		
		protected void TranslateBaseTableVarNode(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, BaseTableVarNode ABaseTableVarNode)
		{
			Tag LTag = MetaData.GetTag(ABaseTableVarNode.TableVar.MetaData, "Catalog.CacheLevel");
			if (LTag != Tag.None)
			{
				if ((LTag.Value == "StoreTable") || (LTag.Value == "StoreView"))
				{
					ADevicePlan.TableContext = ABaseTableVarNode.TableVar;
					ABaseTableVarNode.CursorType = CursorType.Dynamic;
					ABaseTableVarNode.RequestedCursorType = ADevicePlan.Plan.CursorContext.CursorType;
					ABaseTableVarNode.CursorCapabilities = CursorCapability.Navigable | (ADevicePlan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
					ABaseTableVarNode.CursorIsolation = ADevicePlan.Plan.CursorContext.CursorIsolation;
					ABaseTableVarNode.Order = Compiler.OrderFromKey(ADevicePlan.Plan, Compiler.FindClusteringKey(ADevicePlan.Plan, ABaseTableVarNode.TableVar));
					if (LTag.Value == "StoreTable")
						ADevicePlanNode.Statement.AppendFormat("select * from DAE{0}", Schema.Object.Unqualify(ABaseTableVarNode.TableVar.Name));
					else
						GetViewDefinition(Schema.Object.Unqualify(ABaseTableVarNode.TableVar.Name), ADevicePlanNode.Statement, ADevicePlanNode.WhereCondition);
				}
				else
					ADevicePlan.IsSupported = false;
			}
			else
				ADevicePlan.IsSupported = false;
		}
		
		protected string GetInstructionKeyword(string AInstruction)
		{
			switch (AInstruction)
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
			
			Error.Fail("Unknown instruction '{0}' encountered in catalog device.", AInstruction);
			return String.Empty;
		}
		
		protected void TranslateScalarParameter(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, PlanNode APlanNode)
		{
			SQLType LType = null;
			switch (APlanNode.DataType.Name)
			{
				case "System.String" :
				case "System.Name" :
				case "System.UserID" :
					LType = new SQLStringType(200);
				break;
				
				case "System.Integer" :
					LType = new SQLIntegerType(4);
				break;
				
				case "System.Boolean" :
					LType = new SQLIntegerType(1);
				break;
			}
			
			if (LType != null)
			{
				string LParameterName = String.Format("P{0}", ADevicePlanNode.PlanParameters.Count + 1);
				ADevicePlanNode.PlanParameters.Add(new CatalogPlanParameter(new SQLParameter(LParameterName, LType, null), APlanNode));
				ADevicePlanNode.WhereCondition.AppendFormat("@{0}", LParameterName);
			}
			else
				ADevicePlan.IsSupported = false;
		}

		protected void TranslateExpression(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, PlanNode APlanNode)
		{
			InstructionNodeBase LInstructionNode = APlanNode as InstructionNodeBase;
			if (LInstructionNode != null)
			{
				if ((LInstructionNode.DataType != null) && (LInstructionNode.Operator != null))
				{
					switch (Schema.Object.Unqualify(LInstructionNode.Operator.OperatorName))
					{
						case Instructions.And:
						case Instructions.Equal:
						case Instructions.NotEqual:
						case Instructions.Greater:
						case Instructions.InclusiveGreater:
						case Instructions.Less:
						case Instructions.InclusiveLess:
						case Instructions.Like:
							TranslateExpression(ADevicePlan, ADevicePlanNode, LInstructionNode.Nodes[0]);
							ADevicePlanNode.WhereCondition.AppendFormat(" {0} ", GetInstructionKeyword(Schema.Object.Unqualify(LInstructionNode.Operator.OperatorName)));
							TranslateExpression(ADevicePlan, ADevicePlanNode, LInstructionNode.Nodes[1]);
						return;
						
						case "ReadValue" : TranslateExpression(ADevicePlan, ADevicePlanNode, LInstructionNode.Nodes[0]); return;
						
						default: ADevicePlan.IsSupported = false; return;
					}
				}
			}
			
			ValueNode LValueNode = APlanNode as ValueNode;
			if (LValueNode != null)
			{
				TranslateScalarParameter(ADevicePlan, ADevicePlanNode, APlanNode);
				return;
			}
			
			StackReferenceNode LStackReferenceNode = APlanNode as StackReferenceNode;
			if (LStackReferenceNode != null)
			{
				TranslateScalarParameter(ADevicePlan, ADevicePlanNode, new StackReferenceNode(LStackReferenceNode.DataType, LStackReferenceNode.Location - 1));
				return;
			}
			
			StackColumnReferenceNode LStackColumnReferenceNode = APlanNode as StackColumnReferenceNode;
			if (LStackColumnReferenceNode != null)
			{
				if (LStackColumnReferenceNode.Location == 0)
					if (ADevicePlan.TableContext != null)
						ADevicePlanNode.WhereCondition.Append(Schema.Object.EnsureUnrooted(MetaData.GetTag(ADevicePlan.TableContext.Columns[LStackColumnReferenceNode.Identifier].MetaData, "Storage.Name", LStackColumnReferenceNode.Identifier)));
					else
						ADevicePlanNode.WhereCondition.Append(Schema.Object.EnsureUnrooted(LStackColumnReferenceNode.Identifier));
				else
					TranslateScalarParameter(ADevicePlan, ADevicePlanNode, new StackColumnReferenceNode(LStackColumnReferenceNode.Identifier, LStackColumnReferenceNode.DataType, LStackColumnReferenceNode.Location - 1));

				return;
			}
			
			ADevicePlan.IsSupported = false;
		}
		
		protected void TranslateRestrictNode(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, RestrictNode ARestrictNode)
		{
			if ((ARestrictNode.SourceNode is BaseTableVarNode) && (ARestrictNode.Nodes[1] is InstructionNodeBase))
			{
				TranslateBaseTableVarNode(ADevicePlan, ADevicePlanNode, (BaseTableVarNode)ARestrictNode.SourceNode);
				if (ADevicePlan.IsSupported)
				{
					if (ADevicePlanNode.WhereCondition.Length > 0)
					{
						ADevicePlanNode.WhereCondition.Insert(0, "(");
						ADevicePlanNode.WhereCondition.AppendFormat(") and ");
					}
					TranslateExpression(ADevicePlan, ADevicePlanNode, ARestrictNode.Nodes[1]);

					if (ADevicePlan.IsSupported)
					{
						ARestrictNode.CursorType = ARestrictNode.SourceNode.CursorType;
						ARestrictNode.RequestedCursorType = ARestrictNode.SourceNode.RequestedCursorType;
						ARestrictNode.CursorCapabilities = ARestrictNode.SourceNode.CursorCapabilities;
						ARestrictNode.CursorIsolation = ARestrictNode.SourceNode.CursorIsolation;
						ARestrictNode.Order = ARestrictNode.SourceNode.Order;
					}
				}
			}
			else
				ADevicePlan.IsSupported = false;
		}
		
		protected void TranslatePlanNode(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, PlanNode APlanNode)
		{
			if (APlanNode is BaseTableVarNode) TranslateBaseTableVarNode(ADevicePlan, ADevicePlanNode, (BaseTableVarNode)APlanNode);
			else if (APlanNode is RestrictNode) TranslateRestrictNode(ADevicePlan, ADevicePlanNode, (RestrictNode)APlanNode);
			else if (APlanNode is OrderNode) TranslateOrderNode(ADevicePlan, ADevicePlanNode, (OrderNode)APlanNode);
			else ADevicePlan.IsSupported = false;
		}
		
		protected void TranslateOrder(CatalogDevicePlan ADevicePlan, CatalogDevicePlanNode ADevicePlanNode, TableNode ATableNode)
		{
			if (ADevicePlanNode.WhereCondition.Length > 0)
				ADevicePlanNode.Statement.AppendFormat(" where {0}", ADevicePlanNode.WhereCondition.ToString());
				
			if ((ATableNode.Order != null) && (ATableNode.Order.Columns.Count > 0))
			{
				ADevicePlanNode.Statement.AppendFormat(" order by ");
				for (int LIndex = 0; LIndex < ATableNode.Order.Columns.Count; LIndex++)
				{
					OrderColumn LOrderColumn = ATableNode.Order.Columns[LIndex];
					if (LIndex > 0)
						ADevicePlanNode.Statement.Append(", ");
					if (ADevicePlan.TableContext != null)
						ADevicePlanNode.Statement.Append(Schema.Object.EnsureUnrooted(MetaData.GetTag(ADevicePlan.TableContext.Columns[LOrderColumn.Column.Name].MetaData, "Storage.Name", LOrderColumn.Column.Name)));
					else
						ADevicePlanNode.Statement.Append(Schema.Object.EnsureUnrooted(LOrderColumn.Column.Name));
					if (!LOrderColumn.Ascending)
						ADevicePlanNode.Statement.Append(" desc");
				}
			}
		}
	}
}
