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
using Alphora.Dataphor.DAE.Connection;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDevice : MemoryDevice
	{
		public CatalogDevice(int AID, string AName) : base(AID, AName){}
		
		protected override DeviceSession InternalConnect(ServerProcess AServerProcess, DeviceSessionInfo ADeviceSessionInfo)
		{
			return new CatalogDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		private CatalogHeaders FHeaders = new CatalogHeaders();
		public CatalogHeaders Headers { get { return FHeaders; } }
		
		private CatalogStore FStore;
		internal CatalogStore Store 
		{ 
			get 
			{ 
				Error.AssertFail(FStore != null, "Server is configured as a repository and has no catalog store."); 
				return FStore; 
			} 
		}

		// Users cache, maintained exclusively through the catalog maintenance API on the CatalogDeviceSession		
		private Users FUsersCache = new Users();
		internal Users UsersCache { get { return FUsersCache; } }
		
		// Catalog index, maintained exclusively through the catalog maintenance API on the CatalogDeviceSession
		// The catalog index stores the name of each object in the catalog cache by it's object ID
		// Note that the names are stored rooted in this index.
		internal Dictionary<int, string> FCatalogIndex = new Dictionary<int, string>();
		
		internal NameResolutionCache FNameCache = new NameResolutionCache(CDefaultNameResolutionCacheSize);
		
		internal NameResolutionCache FOperatorNameCache = new NameResolutionCache(CDefaultNameResolutionCacheSize);
		
		public const int CDefaultNameResolutionCacheSize = 1000;

		public int NameResolutionCacheSize
		{
			get { return FNameCache.Size; }
			set { FNameCache.Resize(value); }
		}
		
		public int OperatorNameResolutionCacheSize
		{
			get { return FOperatorNameCache.Size; }
			set { FOperatorNameCache.Resize(value); }
		}
		
		public int MaxStoreConnections
		{
			get { return FStore.MaxConnections; }
			set { FStore.MaxConnections = value; }
		}
		
		protected override void InternalStart(ServerProcess AProcess)
		{
			base.InternalStart(AProcess);
			if (!AProcess.ServerSession.Server.IsEngine)
			{
				FStore = new CatalogStore();
				FStore.StoreClassName = AProcess.ServerSession.Server.GetCatalogStoreClassName();
				FStore.StoreConnectionString = AProcess.ServerSession.Server.GetCatalogStoreConnectionString();
				FStore.Initialize(AProcess.ServerSession.Server);
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
		
		protected override DevicePlan CreateDevicePlan(Plan APlan, PlanNode APlanNode)
		{
			return new CatalogDevicePlan(APlan, this, APlanNode);
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
			
			LDevicePlan.IsSupported = true;
			return base.InternalPrepare(ADevicePlan, APlanNode);
		}
		
		protected virtual void PopulateServerSettings(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
		}

		private void PopulateConnections(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
/*
			foreach (ServerConnection LConnection in AProgram.ServerProcess.ServerSession.Server.Connections)
			{
				ARow[0] = LConnection.ConnectionName;
				ARow[1] = LConnection.HostName;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
*/
		}
		
		private void PopulateSessions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
			{
				ARow[0] = LSession.SessionID;
				ARow[1] = LSession.User.ID;
				ARow[2] = LSession.SessionInfo.HostName;
				ARow[3] = LSession.SessionInfo.CatalogCacheName;
				ARow[4] = LSession.CurrentLibrary.Name;
				ARow[5] = LSession.SessionInfo.DefaultIsolationLevel.ToString();
				ARow[6] = LSession.SessionInfo.DefaultUseDTC;
				ARow[7] = LSession.SessionInfo.DefaultUseImplicitTransactions;
				ARow[8] = LSession.SessionInfo.Language.ToString();
				ARow[9] = LSession.SessionInfo.FetchCount;
				ARow[10] = LSession.SessionInfo.DefaultMaxStackDepth;
				ARow[11] = LSession.SessionInfo.DefaultMaxCallDepth;
				ARow[12] = LSession.SessionInfo.UsePlanCache;
				ARow[13] = LSession.SessionInfo.ShouldEmitIL;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateProcesses(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
				{
					ARow[0] = LProcess.ProcessID;
					ARow[1] = LSession.SessionID;
					ARow[2] = LProcess.DefaultIsolationLevel.ToString();
					ARow[3] = LProcess.UseDTC;
					ARow[4] = LProcess.UseImplicitTransactions;
					ARow[5] = LProcess.MaxStackDepth;
					ARow[6] = LProcess.MaxCallDepth;
					ARow[7] = LProcess.ExecutingThread != null;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}
		
		private void PopulateLocks(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			LockManager LLockManager = AProgram.ServerProcess.ServerSession.Server.LockManager;
			KeyValuePair<LockID, LockHeader>[] LEntries;
			lock (LLockManager)
			{
				LEntries = new KeyValuePair<LockID, LockHeader>[LLockManager.Locks.Count];
				var LIndex = 0;
				foreach (KeyValuePair<LockID, LockHeader> LEntry in LLockManager.Locks)
				{
					LEntries[LIndex] = LEntry;
					LIndex++;
				}
			}

			foreach (KeyValuePair<LockID, LockHeader> LEntry in LEntries)
			{
				LockHeader LLockHeader = LEntry.Value;
				ARow[0] = LLockHeader.LockID.Owner.ToString();
				ARow[1] = LLockHeader.LockID.LockName;
				ARow[2] = LLockHeader.Semaphore.Mode.ToString();
				ARow[3] = LLockHeader.Semaphore.GrantCount();
				ARow[4] = LLockHeader.Semaphore.WaitCount();
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateScripts(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (ServerScript LScript in LProcess.Scripts)
					{
						ARow[0] = LScript.GetHashCode();
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}

		private void PopulatePlans(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (ServerPlan LPlan in LProcess.Plans)
					{
						ARow[0] = LPlan.ID;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}

		private void PopulateLibraries(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
			{
				ARow[0] = LLibrary.Name;
				ARow[1] = LLibrary.Directory;
				ARow[2] = LLibrary.Version;
				ARow[3] = LLibrary.DefaultDeviceName;
				ARow[4] = true; //AProgram.ServerProcess.ServerSession.Server.CanLoadLibrary(LLibrary.Name);
				ARow[5] = LLibrary.IsSuspect;
				ARow[6] = LLibrary.SuspectReason;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}											
		
		private void PopulateLibraryFiles(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
				foreach (Schema.FileReference LReference in LLibrary.Files)
				{
					ARow[0] = LLibrary.Name;
					ARow[1] = LReference.FileName;
					ARow[2] = LReference.IsAssembly;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}											
		
		private void PopulateLibraryRequisites(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
				foreach (LibraryReference LReference in LLibrary.Libraries)
				{
					ARow[0] = LLibrary.Name;
					ARow[1] = LReference.Name;
					ARow[2] = LReference.Version; 
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}											
		
		private void PopulateLibrarySettings(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProgram.Catalog.Libraries)
				if (LLibrary.MetaData != null)
				{
					#if USEHASHTABLEFORTAGS
					foreach (Tag LTag in LLibrary.MetaData.Tags)
					{
					#else
					Tag LTag;
					for (int LIndex = 0; LIndex < LLibrary.MetaData.Tags.Count; LIndex++)
					{
						LTag = LLibrary.MetaData.Tags[LIndex];
					#endif
						ARow[0] = LLibrary.Name;
						ARow[1] = LTag.Name;
						ARow[2] = LTag.Value;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
		}
		
		private void PopulateLibraryOwners(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			AProgram.CatalogDeviceSession.SelectLibraryOwners(AProgram, ANativeTable, ARow);
		}

		private void PopulateLibraryVersions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			AProgram.CatalogDeviceSession.SelectLibraryVersions(AProgram, ANativeTable, ARow);
		}

		private void PopulateRegisteredAssemblies(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (RegisteredAssembly LAssembly in AProgram.Catalog.ClassLoader.Assemblies)
			{
				ARow[0] = LAssembly.Name.ToString();
				ARow[1] = LAssembly.Library.Name;
				ARow[2] = LAssembly.Assembly.Location;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}											
		
		private void PopulateRegisteredClasses(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (RegisteredClass LClass in AProgram.Catalog.ClassLoader.Classes)
			{
				ARow[0] = LClass.Name;
				ARow[1] = LClass.Library.Name;
				ARow[2] = LClass.Assembly.Name.ToString();
				ARow[3] = LClass.ClassName;
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}											
		
		private void PopulateLoadedLibraries(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			List<string> LLibraryNames = AProgram.CatalogDeviceSession.SelectLoadedLibraries();
			for (int LIndex = 0; LIndex < LLibraryNames.Count; LIndex++)
			{
				ARow[0] = LLibraryNames[LIndex];
				ANativeTable.Insert(AProgram.ValueManager, ARow);
			}
		}
		
		private void PopulateSessionCatalogObjects(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (Schema.SessionObject LObject in LSession.SessionObjects)
				{
					ARow[0] = LSession.SessionID;
					ARow[1] = LObject.Name;
					ARow[2] = LObject.GlobalName;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
		}

		#if USETYPEINHERITANCE
		private void PopulateScalarTypeParentScalarTypes(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
				if (LObject is Schema.ScalarType)
					foreach (Schema.ScalarType LParentScalarType in ((Schema.ScalarType)LObject).ParentTypes)
					{
						ARow[0] = LObject.Name;
						ARow[1] = LParentScalarType.Name;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
			
		}
		#endif
		
		#if USETYPEINHERITANCE		
		private void PopulateScalarTypeExplicitCastFunctions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
				if (LObject is Schema.ScalarType)
				{
					Schema.ScalarType LScalarType = (Schema.ScalarType)LObject;
					foreach (Schema.Operator LOperator in LScalarType.ExplicitCastOperators)
					{
						ARow[0] = LScalarType.Name;
						ARow[1] = LOperator.Name;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
		}
		#endif
		
		private void PopulateServerLinks(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
			{
				Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
				if (LServerLink != null)
				{
					ARow[0] = LServerLink.ID;
					ARow[1] = LServerLink.Name;
					ARow[2] = LServerLink.Library.Name;
					ARow[3] = LServerLink.Owner.ID;
					ARow[4] = LServerLink.IsSystem;
					ARow[5] = LServerLink.IsGenerated;
					ARow[6] = LServerLink.HostName;
					ARow[7] = LServerLink.InstanceName;
					ARow[8] = LServerLink.OverridePortNumber;
					ARow[9] = LServerLink.UseSessionInfo;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
			}
		}
		
		private void PopulateServerLinkUsers(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProgram.Catalog)
			{
				Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
				if (LServerLink != null)
				{
					foreach (ServerLinkUser LLinkUser in LServerLink.Users.Values)
					{
						ARow[0] = LLinkUser.UserID;
						ARow[1] = LServerLink.Name;
						ARow[2] = LLinkUser.ServerLinkUserID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
				}
			}
		}
		
		private void PopulateRemoteSessions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (RemoteSession LRemoteSession in LProcess.RemoteSessions)
					{
						ARow[0] = LRemoteSession.ServerLink.Name;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}
		
		private void PopulateDeviceSessions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProgram.ServerProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (DeviceSession LDeviceSession in LProcess.DeviceSessions)
					{
						ARow[0] = LDeviceSession.Device.Name;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProgram.ValueManager, ARow);
					}
		}
		
		private void PopulateApplicationTransactions(Program AProgram, NativeTable ANativeTable, Row ARow)
		{
			lock (AProgram.ServerProcess.ServerSession.Server.ATDevice.ApplicationTransactions.SyncRoot)
			{
				foreach (ApplicationTransaction.ApplicationTransaction LTransaction in AProgram.ServerProcess.ServerSession.Server.ATDevice.ApplicationTransactions.Values)
				{
					ARow[0] = LTransaction.ID;
					ARow[1] = LTransaction.Session.SessionID;
					ARow[2] = LTransaction.Device.Name;
					ANativeTable.Insert(AProgram.ValueManager, ARow);
				}
			}
		}
		
		protected internal virtual void PopulateTableVar(Program AProgram, CatalogHeader AHeader)
		{
			AHeader.NativeTable.Truncate(AProgram.ValueManager);
			Row LRow = new Row(AProgram.ValueManager, AHeader.TableVar.DataType.RowType);
			try
			{
				switch (AHeader.TableVar.Name)
				{
					case "System.ServerSettings" : PopulateServerSettings(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Connections" : PopulateConnections(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Sessions" : PopulateSessions(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Processes" : PopulateProcesses(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Locks" : PopulateLocks(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Scripts" : PopulateScripts(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Plans" : PopulatePlans(AProgram, AHeader.NativeTable, LRow); break;
					case "System.Libraries" : PopulateLibraries(AProgram, AHeader.NativeTable, LRow); break;
					case "System.LibraryFiles" : PopulateLibraryFiles(AProgram, AHeader.NativeTable, LRow); break;
					case "System.LibraryRequisites" : PopulateLibraryRequisites(AProgram, AHeader.NativeTable, LRow); break;
					case "System.LibrarySettings" : PopulateLibrarySettings(AProgram, AHeader.NativeTable, LRow); break;
					case "System.LibraryOwners" : PopulateLibraryOwners(AProgram, AHeader.NativeTable, LRow); break;
					case "System.LibraryVersions" : PopulateLibraryVersions(AProgram, AHeader.NativeTable, LRow); break;
					case "System.RegisteredAssemblies" : PopulateRegisteredAssemblies(AProgram, AHeader.NativeTable, LRow); break;
					case "System.RegisteredClasses" : PopulateRegisteredClasses(AProgram, AHeader.NativeTable, LRow); break;
					case "System.LoadedLibraries" : PopulateLoadedLibraries(AProgram, AHeader.NativeTable, LRow); break;
					case "System.SessionCatalogObjects" : PopulateSessionCatalogObjects(AProgram, AHeader.NativeTable, LRow); break;
					#if USETYPEINHERITANCE
					case "System.ScalarTypeParentScalarTypes" : PopulateScalarTypeParentScalarTypes(AProgram, AHeader.NativeTable, LRow); break;
					case "System.ScalarTypeExplicitCastFunctions" : PopulateScalarTypeExplicitCastFunctions(AProgram, AHeader.NativeTable, LRow); break;
					#endif
					case "System.DeviceSessions" : PopulateDeviceSessions(AProgram, AHeader.NativeTable, LRow); break;
					case "System.ApplicationTransactions" : PopulateApplicationTransactions(AProgram, AHeader.NativeTable, LRow); break;
					case "System.ServerLinks": PopulateServerLinks(AProgram, AHeader.NativeTable, LRow); break;
					case "System.ServerLinkUsers": PopulateServerLinkUsers(AProgram, AHeader.NativeTable, LRow); break;
					case "System.RemoteSessions": PopulateRemoteSessions(AProgram, AHeader.NativeTable, LRow); break;
				}
			}
			finally
			{
				LRow.Dispose();
			}

			AHeader.TimeStamp = AProgram.Catalog.TimeStamp;
		}
	}
}

