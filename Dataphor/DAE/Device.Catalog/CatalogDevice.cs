/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define LOGDDLINSTRUCTIONS

using System;
using System.IO;
using System.Data;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Store;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Alphora.Dataphor.DAE.Connection;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class CatalogDevice : MemoryDevice
	{
		public CatalogDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID){}
		
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
			if (!AProcess.ServerSession.Server.IsRepository)
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
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, J.IsSystem, J.IsGenerated, O.OperatorName, O.Signature
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
select C.ID, C.Name, C.Library_Name, C.Owner_User_ID, O.IsSystem, O.IsGenerated, D.ResourceManagerID, D.ReconciliationMaster, D.ReconciliationMode
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
	from DAEDeviceObjects DO
		join DAEObjects O on DO.ID = O.ID
		join DAECatalogObjects D on DO.Device_ID = D.ID
		join DAECatalogObjects S on DO.Mapped_Object_ID = S.ID
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
	from DAEDeviceObjects DO
		join DAEObjects O on DO.ID = O.ID
		join DAECatalogObjects D on DO.Device_ID = D.ID
		join DAECatalogObjects S on DO.Mapped_Object_ID = S.ID
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
			if (LTag != null)
			{
				if ((LTag.Value == "StoreTable") || (LTag.Value == "StoreView"))
				{
					ADevicePlan.TableContext = ABaseTableVarNode.TableVar;
					ABaseTableVarNode.CursorType = CursorType.Dynamic;
					ABaseTableVarNode.RequestedCursorType = ADevicePlan.Plan.CursorContext.CursorType;
					ABaseTableVarNode.CursorCapabilities = CursorCapability.Navigable | (ADevicePlan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
					ABaseTableVarNode.CursorIsolation = ADevicePlan.Plan.CursorContext.CursorIsolation;
					ABaseTableVarNode.Order = new Schema.Order(ABaseTableVarNode.TableVar.FindClusteringKey(), ADevicePlan.Plan);
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
		
		private void PopulateServerSettings(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			Server.Server LServer = AProcess.ServerSession.Server;
			ARow[0] = LServer.Name;
			ARow[1] = GetType().Assembly.GetName().Version.ToString();
			ARow[2] = LServer.TracingEnabled;
			ARow[3] = LServer.LogErrors;
			ARow[4] = LServer.Catalog.TimeStamp;
			ARow[5] = LServer.CacheTimeStamp;
			ARow[6] = LServer.PlanCacheTimeStamp;
			ARow[7] = LServer.DerivationTimeStamp;
			ARow[8] = LServer.InstanceDirectory;
			ARow[9] = LServer.LibraryDirectory;
			ARow[10] = LServer.IsRepository;
			ARow[11] = LServer.IsEmbedded;
			ARow[12] = LServer.MaxConcurrentProcesses;
			ARow[13] = LServer.ProcessWaitTimeout;
			ARow[14] = LServer.ProcessTerminationTimeout;
			ARow[15] = LServer.PlanCache.Size;
			ANativeTable.Insert(AProcess, ARow);
		}
		
		private void PopulateConnections(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
/*
			foreach (ServerConnection LConnection in AProcess.ServerSession.Server.Connections)
			{
				ARow[0] = LConnection.ConnectionName;
				ARow[1] = LConnection.HostName;
				ANativeTable.Insert(AProcess, ARow);
			}
*/
		}
		
		private void PopulateSessions(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
			{
				ARow[0] = LSession.SessionID;
				ARow[1] = LSession.User.ID;
				ARow[2] = LSession.SessionInfo.HostName;
				ARow[3] = LSession.SessionInfo.CatalogCacheName;
				ARow[4] = LSession.CurrentLibrary.Name;
				ARow[5] = LSession.SessionInfo.DefaultIsolationLevel.ToString();
				ARow[6] = LSession.SessionInfo.SessionTracingEnabled;
				ARow[7] = LSession.SessionInfo.DefaultUseDTC;
				ARow[8] = LSession.SessionInfo.DefaultUseImplicitTransactions;
				ARow[9] = LSession.SessionInfo.Language.ToString();
				ARow[10] = LSession.SessionInfo.FetchCount;
				ARow[11] = LSession.SessionInfo.DefaultMaxStackDepth;
				ARow[12] = LSession.SessionInfo.DefaultMaxCallDepth;
				ARow[13] = LSession.SessionInfo.UsePlanCache;
				ARow[14] = LSession.SessionInfo.ShouldEmitIL;
				ANativeTable.Insert(AProcess, ARow);
			}
		}
		
		private void PopulateProcesses(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
				{
					ARow[0] = LProcess.ProcessID;
					ARow[1] = LSession.SessionID;
					ARow[2] = LProcess.DefaultIsolationLevel.ToString();
					ARow[3] = LProcess.UseDTC;
					ARow[4] = LProcess.UseImplicitTransactions;
					ARow[5] = LProcess.Context.MaxStackDepth;
					ARow[6] = LProcess.Context.MaxCallDepth;
					ARow[7] = LProcess.ExecutingThread != null;
					ANativeTable.Insert(AProcess, ARow);
				}
		}
		
		private void PopulateLocks(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			LockManager LLockManager = AProcess.ServerSession.Server.LockManager;
			DictionaryEntry[] LEntries;
			lock (LLockManager)
			{
				LEntries = new DictionaryEntry[LLockManager.Locks.Count];
				LLockManager.Locks.CopyTo(LEntries, 0);
			}
			
			foreach (DictionaryEntry LEntry in LEntries)
			{
				LockHeader LLockHeader = (LockHeader)LEntry.Value;
				ARow[0] = LLockHeader.LockID.ResourceManagerID;
				ARow[1] = LLockHeader.LockID.LockName;
				ARow[2] = LLockHeader.Semaphore.Mode.ToString();
				ARow[3] = LLockHeader.Semaphore.GrantCount();
				ARow[4] = LLockHeader.Semaphore.WaitCount();
				ANativeTable.Insert(AProcess, ARow);
			}
		}
		
		private void PopulateScripts(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (ServerScript LScript in LProcess.Scripts)
					{
						ARow[0] = LScript.GetHashCode();
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProcess, ARow);
					}
		}

		private void PopulatePlans(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (ServerPlan LPlan in LProcess.Plans)
					{
						ARow[0] = LPlan.ID;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProcess, ARow);
					}
		}

		private void PopulateLibraries(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProcess.Plan.Catalog.Libraries)
			{
				ARow[0] = LLibrary.Name;
				ARow[1] = LLibrary.Directory;
				ARow[2] = LLibrary.Version;
				ARow[3] = LLibrary.DefaultDeviceName;
				ARow[4] = true; //AProcess.ServerSession.Server.CanLoadLibrary(LLibrary.Name);
				ARow[5] = LLibrary.IsSuspect;
				ARow[6] = LLibrary.SuspectReason;
				ANativeTable.Insert(AProcess, ARow);
			}
		}											
		
		private void PopulateLibraryFiles(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProcess.Plan.Catalog.Libraries)
				foreach (Schema.FileReference LReference in LLibrary.Files)
				{
					ARow[0] = LLibrary.Name;
					ARow[1] = LReference.FileName;
					ARow[2] = LReference.IsAssembly;
					ANativeTable.Insert(AProcess, ARow);
				}
		}											
		
		private void PopulateLibraryRequisites(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProcess.Plan.Catalog.Libraries)
				foreach (LibraryReference LReference in LLibrary.Libraries)
				{
					ARow[0] = LLibrary.Name;
					ARow[1] = LReference.Name;
					ARow[2] = LReference.Version; 
					ANativeTable.Insert(AProcess, ARow);
				}
		}											
		
		private void PopulateLibrarySettings(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Library LLibrary in AProcess.Plan.Catalog.Libraries)
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
						ANativeTable.Insert(AProcess, ARow);
					}
				}
		}
		
		private void PopulateLibraryOwners(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			AProcess.CatalogDeviceSession.SelectLibraryOwners(ANativeTable, ARow);
		}

		private void PopulateLibraryVersions(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			AProcess.CatalogDeviceSession.SelectLibraryVersions(ANativeTable, ARow);
		}

		private void PopulateRegisteredAssemblies(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (RegisteredAssembly LAssembly in AProcess.Plan.Catalog.ClassLoader.Assemblies)
			{
				ARow[0] = LAssembly.Name.ToString();
				ARow[1] = LAssembly.Library.Name;
				ARow[2] = LAssembly.Assembly.Location;
				ANativeTable.Insert(AProcess, ARow);
			}
		}											
		
		private void PopulateRegisteredClasses(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (RegisteredClass LClass in AProcess.Plan.Catalog.ClassLoader.Classes)
			{
				ARow[0] = LClass.Name;
				ARow[1] = LClass.Library.Name;
				ARow[2] = LClass.Assembly.Name.ToString();
				ARow[3] = LClass.ClassName;
				ANativeTable.Insert(AProcess, ARow);
			}
		}											
		
		private void PopulateLoadedLibraries(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			List<string> LLibraryNames = AProcess.CatalogDeviceSession.SelectLoadedLibraries();
			for (int LIndex = 0; LIndex < LLibraryNames.Count; LIndex++)
			{
				ARow[0] = LLibraryNames[LIndex];
				ANativeTable.Insert(AProcess, ARow);
			}
		}
		
		private void PopulateSessionCatalogObjects(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				foreach (Schema.SessionObject LObject in LSession.SessionObjects)
				{
					ARow[0] = LSession.SessionID;
					ARow[1] = LObject.Name;
					ARow[2] = LObject.GlobalName;
					ANativeTable.Insert(AProcess, ARow);
				}
		}

		#if USETYPEINHERITANCE
		private void PopulateScalarTypeParentScalarTypes(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProcess.Plan.Catalog)
				if (LObject is Schema.ScalarType)
					foreach (Schema.ScalarType LParentScalarType in ((Schema.ScalarType)LObject).ParentTypes)
					{
						ARow[0] = LObject.Name;
						ARow[1] = LParentScalarType.Name;
						ANativeTable.Insert(AProcess, ARow);
					}
			
		}
		#endif
		
		#if USETYPEINHERITANCE		
		private void PopulateScalarTypeExplicitCastFunctions(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProcess.Plan.Catalog)
				if (LObject is Schema.ScalarType)
				{
					Schema.ScalarType LScalarType = (Schema.ScalarType)LObject;
					foreach (Schema.Operator LOperator in LScalarType.ExplicitCastOperators)
					{
						ARow[0] = LScalarType.Name;
						ARow[1] = LOperator.Name;
						ANativeTable.Insert(AProcess, ARow);
					}
				}
		}
		#endif
		
		private void PopulateServerLinks(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProcess.Plan.Catalog)
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
					ANativeTable.Insert(AProcess, ARow);
				}
			}
		}
		
		private void PopulateServerLinkUsers(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (Schema.Object LObject in AProcess.Plan.Catalog)
			{
				Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
				if (LServerLink != null)
				{
					foreach (ServerLinkUser LLinkUser in LServerLink.Users)
					{
						ARow[0] = LLinkUser.UserID;
						ARow[1] = LServerLink.Name;
						ARow[2] = LLinkUser.ServerLinkUserID;
						ANativeTable.Insert(AProcess, ARow);
					}
				}
			}
		}
		
		private void PopulateRemoteSessions(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (RemoteSession LRemoteSession in LProcess.RemoteSessions)
					{
						ARow[0] = LRemoteSession.ServerLink.Name;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProcess, ARow);
					}
		}
		
		private void PopulateDeviceSessions(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
				foreach (ServerProcess LProcess in LSession.Processes)
					foreach (DeviceSession LDeviceSession in LProcess.DeviceSessions)
					{
						ARow[0] = LDeviceSession.Device.Name;
						ARow[1] = LProcess.ProcessID;
						ANativeTable.Insert(AProcess, ARow);
					}
		}
		
		private void PopulateApplicationTransactions(ServerProcess AProcess, NativeTable ANativeTable, Row ARow)
		{
			lock (AProcess.ServerSession.Server.ATDevice.ApplicationTransactions.SyncRoot)
			{
				foreach (ApplicationTransaction.ApplicationTransaction LTransaction in AProcess.ServerSession.Server.ATDevice.ApplicationTransactions.Values)
				{
					ARow[0] = LTransaction.ID;
					ARow[1] = LTransaction.Session.SessionID;
					ARow[2] = LTransaction.Device.Name;
					ANativeTable.Insert(AProcess, ARow);
				}
			}
		}
		
		protected internal virtual void PopulateTableVar(ServerProcess AProcess, CatalogHeader AHeader)
		{
			AHeader.NativeTable.Truncate(AProcess);
			Row LRow = new Row(AProcess, AHeader.TableVar.DataType.RowType);
			try
			{
				switch (AHeader.TableVar.Name)
				{
					case "System.ServerSettings" : PopulateServerSettings(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Connections" : PopulateConnections(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Sessions" : PopulateSessions(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Processes" : PopulateProcesses(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Locks" : PopulateLocks(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Scripts" : PopulateScripts(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Plans" : PopulatePlans(AProcess, AHeader.NativeTable, LRow); break;
					case "System.Libraries" : PopulateLibraries(AProcess, AHeader.NativeTable, LRow); break;
					case "System.LibraryFiles" : PopulateLibraryFiles(AProcess, AHeader.NativeTable, LRow); break;
					case "System.LibraryRequisites" : PopulateLibraryRequisites(AProcess, AHeader.NativeTable, LRow); break;
					case "System.LibrarySettings" : PopulateLibrarySettings(AProcess, AHeader.NativeTable, LRow); break;
					case "System.LibraryOwners" : PopulateLibraryOwners(AProcess, AHeader.NativeTable, LRow); break;
					case "System.LibraryVersions" : PopulateLibraryVersions(AProcess, AHeader.NativeTable, LRow); break;
					case "System.RegisteredAssemblies" : PopulateRegisteredAssemblies(AProcess, AHeader.NativeTable, LRow); break;
					case "System.RegisteredClasses" : PopulateRegisteredClasses(AProcess, AHeader.NativeTable, LRow); break;
					case "System.LoadedLibraries" : PopulateLoadedLibraries(AProcess, AHeader.NativeTable, LRow); break;
					case "System.SessionCatalogObjects" : PopulateSessionCatalogObjects(AProcess, AHeader.NativeTable, LRow); break;
					#if USETYPEINHERITANCE
					case "System.ScalarTypeParentScalarTypes" : PopulateScalarTypeParentScalarTypes(AProcess, AHeader.NativeTable, LRow); break;
					case "System.ScalarTypeExplicitCastFunctions" : PopulateScalarTypeExplicitCastFunctions(AProcess, AHeader.NativeTable, LRow); break;
					#endif
					case "System.DeviceSessions" : PopulateDeviceSessions(AProcess, AHeader.NativeTable, LRow); break;
					case "System.ApplicationTransactions" : PopulateApplicationTransactions(AProcess, AHeader.NativeTable, LRow); break;
					case "System.ServerLinks": PopulateServerLinks(AProcess, AHeader.NativeTable, LRow); break;
					case "System.ServerLinkUsers": PopulateServerLinkUsers(AProcess, AHeader.NativeTable, LRow); break;
					case "System.RemoteSessions": PopulateRemoteSessions(AProcess, AHeader.NativeTable, LRow); break;
				}
			}
			finally
			{
				LRow.Dispose();
			}

			AHeader.TimeStamp = AProcess.Plan.Catalog.TimeStamp;
		}
	}
}

