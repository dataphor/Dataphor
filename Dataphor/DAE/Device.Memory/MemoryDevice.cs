/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
	
using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Memory
{
	/// <summary> In-memory storage device. </summary>
	public class MemoryDevice : Schema.Device
	{
		public MemoryDevice(int AID, string AName, int AResourceManagerID) : base(AID, AName, AResourceManagerID) 
		{
			IgnoreUnsupported = true;
			RequiresAuthentication = false;
		}
		
		protected override Schema.DeviceSession InternalConnect(ServerProcess AServerProcess, Schema.DeviceSessionInfo ADeviceSessionInfo)
		{
			return new MemoryDeviceSession(this, AServerProcess, ADeviceSessionInfo);
		}
		
		public override Schema.DeviceCapability Capabilities 
		{ 
			get 
			{ 
				return 
					Schema.DeviceCapability.RowLevelInsert | 
					Schema.DeviceCapability.RowLevelUpdate | 
					Schema.DeviceCapability.RowLevelDelete | 
					Schema.DeviceCapability.NonLoggedOperations;
			}
		}
		
		// MaxRowCount
		private int FMaxRowCount = -1;
		public int MaxRowCount
		{
			get { return FMaxRowCount; }
			set { FMaxRowCount = value; }
		}
		
		protected override DevicePlanNode InternalPrepare(Schema.DevicePlan APlan, PlanNode APlanNode)
		{
			if (APlanNode is BaseTableVarNode)
			{
				BaseTableVarNode LNode = (BaseTableVarNode)APlanNode;
				LNode.CursorType = CursorType.Dynamic;
				LNode.RequestedCursorType = APlan.Plan.CursorContext.CursorType;
				LNode.CursorCapabilities =
					CursorCapability.Navigable |
					CursorCapability.BackwardsNavigable |
					CursorCapability.Bookmarkable |
					CursorCapability.Searchable |
					(APlan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
				LNode.CursorIsolation = APlan.Plan.CursorContext.CursorIsolation;
				LNode.Order = new Schema.Order(LNode.TableVar.FindClusteringKey(), APlan.Plan);
				return new DevicePlanNode(LNode);
			}
			else if ((APlanNode is OrderNode) && (APlanNode.Nodes[0] is BaseTableVarNode) && (APlan.Plan.CursorContext.CursorType != CursorType.Static))
			{
				OrderNode LNode = (OrderNode)APlanNode;
				BaseTableVarNode LTableVarNode = (BaseTableVarNode)APlanNode.Nodes[0];
				Schema.Order LTableOrder;

				bool LIsSupported = false;
				foreach (Schema.Key LKey in LTableVarNode.TableVar.Keys)
				{
					LTableOrder = new Schema.Order(LKey, APlan.Plan);
					if (LNode.RequestedOrder.Equivalent(LTableOrder))
					{
						LNode.PhysicalOrder = LTableOrder;
						LNode.ScanDirection = ScanDirection.Forward;
						LIsSupported = true;
						break;
					}
					else if (LNode.RequestedOrder.Equivalent(new Schema.Order(LTableOrder, true)))
					{
						LNode.PhysicalOrder = LTableOrder;
						LNode.ScanDirection = ScanDirection.Backward;
						LIsSupported = true;
						break;
					}
				}

				if (!LIsSupported)
					foreach (Schema.Order LOrder in LTableVarNode.TableVar.Orders)
						if (LNode.RequestedOrder.Equivalent(LOrder))
						{
							LNode.PhysicalOrder = LOrder;
							LNode.ScanDirection = ScanDirection.Forward;
							LIsSupported = true;
							break;
						}
						else if (LNode.RequestedOrder.Equivalent(new Schema.Order(LOrder, true)))
						{
							LNode.PhysicalOrder = LOrder;
							LNode.ScanDirection = ScanDirection.Backward;
							LIsSupported = true;
							break;
						}

				if (LIsSupported)
				{
					LNode.Order = new Schema.Order();
					LNode.Order.MergeMetaData(LNode.RequestedOrder.MetaData);
					LNode.Order.IsInherited = false;
					Schema.OrderColumn LOrderColumn;
					Schema.OrderColumn LNewOrderColumn;
					for (int LIndex = 0; LIndex < LNode.PhysicalOrder.Columns.Count; LIndex++)
					{
						LOrderColumn = LNode.PhysicalOrder.Columns[LIndex];
						LNewOrderColumn = 
							new Schema.OrderColumn
							(
								LNode.TableVar.Columns[LOrderColumn.Column], 
								LNode.ScanDirection == ScanDirection.Forward ? 
									LOrderColumn.Ascending : 
									!LOrderColumn.Ascending
							);
						LNewOrderColumn.Sort = LOrderColumn.Sort;
						LNewOrderColumn.IsDefaultSort = LOrderColumn.IsDefaultSort;
						Error.AssertWarn(LNewOrderColumn.Sort != null, "Sort is null");
						LNode.Order.Columns.Add(LNewOrderColumn);
					}
					if (!LNode.TableVar.Orders.Contains(LNode.Order))
						LNode.TableVar.Orders.Add(LNode.Order);

					LNode.CursorType = CursorType.Dynamic;
					LNode.RequestedCursorType = APlan.Plan.CursorContext.CursorType;
					LNode.CursorCapabilities =
						CursorCapability.Navigable |
						CursorCapability.BackwardsNavigable |
						CursorCapability.Bookmarkable |
						CursorCapability.Searchable |
						(APlan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
					LNode.CursorIsolation = APlan.Plan.CursorContext.CursorIsolation;
					
					return new DevicePlanNode(LNode);
				}
			}
			else if (APlanNode is CreateTableVarBaseNode)
			{
				APlan.Plan.CheckRight(GetRight(Schema.RightNames.CreateStore));
				return new DevicePlanNode(APlanNode);
			}
			else if (APlanNode is AlterTableNode)
			{
				APlan.Plan.CheckRight(GetRight(Schema.RightNames.AlterStore));
				AlterTableNode LAlterTableNode = (AlterTableNode)APlanNode;
				if (LAlterTableNode.AlterTableStatement.CreateColumns.Count > 0)
					throw new RuntimeException(RuntimeException.Codes.UnimplementedCreateCommand, "Columns in a memory device");
				if (LAlterTableNode.AlterTableStatement.DropColumns.Count > 0)
					throw new RuntimeException(RuntimeException.Codes.UnimplementedDropCommand, "Columns in a memory device");
				return new DevicePlanNode(APlanNode);
			}
			else if (APlanNode is DropTableNode)
			{
				APlan.Plan.CheckRight(GetRight(Schema.RightNames.DropStore));
				return new DevicePlanNode(APlanNode);
			}
			APlan.IsSupported = false;
			return null;
		}
		
		protected override void InternalStart(ServerProcess AProcess)
		{
			base.InternalStart(AProcess);
			FTables = new NativeTables();
		}
		
		protected override void InternalStop(ServerProcess AProcess)
		{
			if (FTables != null)
			{
				while (FTables.Count > 0)
				{
					FTables[0].Drop(AProcess);
					FTables.RemoveAt(0);
				}

				FTables = null;
			}
			base.InternalStop(AProcess);
		}
		
		private NativeTables FTables;
		public NativeTables Tables { get { return FTables; } }
	}
	
	#if USEMEMORYDEVICETRANSACTIONS
	public class MemoryDeviceTransaction : Disposable
	{
		public MemoryDeviceTransaction(IsolationLevel AIsolationLevel) : base()
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
			
			if (FTransactions != null)
			{
				foreach (MemoryDeviceTransaction LTransaction in FTransactions)
					LTransaction.Dispose();
				FTransactions.Clear();
				FTransactions = null;
			}

			base.Dispose(ADisposing);
		}		

		private Operations FOperations = new Operations();
		public Operations Operations { get { return FOperations; } }
		
		private IsolationLevel FIsolationLevel;
		public IsolationLevel IsolationLevel { get { return FIsolationLevel; } }
		
		private MemoryDeviceTransactions FTransactions;
		public MemoryDeviceTransactions Transactions 
		{ 
			get 
			{ 
				if (FTransactions == null)
					FTransactions = new MemoryDeviceTransactions();
				return FTransactions; 
			} 
		}
	}
	
	public class MemoryDeviceTransactions : TypedList
	{
		public MemoryDeviceTransactions() : base(typeof(MemoryDeviceTransaction)){}
		
		public new MemoryDeviceTransaction this[int AIndex]
		{
			get { return (MemoryDeviceTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			Add(new MemoryDeviceTransaction(AIsolationLevel));
		}
		
		public void EndTransaction(bool ASuccess)
		{
			if ((ASuccess) && (Count > 1))
				this[Count - 2].Transactions.Add(RemoveItemAt(Count - 1));
			else
				((MemoryDeviceTransaction)RemoveItemAt(Count - 1)).Dispose();
		}
		
		public MemoryDeviceTransaction CurrentTransaction()
		{
			return this[Count - 1];
		}
	}
	#endif
	
	public class MemoryDeviceSession : Schema.DeviceSession
	{
		protected internal MemoryDeviceSession
		(
			Schema.Device ADevice, 
			ServerProcess AServerProcess, 
			Schema.DeviceSessionInfo ADeviceSessionInfo
		) : base(ADevice, AServerProcess, ADeviceSessionInfo){}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				#if USEMEMORYDEVICETRANSACTIONS
				if (FTransactions != null)
				{
					while (FTransactions.Count > 0)
						InternalRollbackTransaction();
					FTransactions = null;
				}
				#endif
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		public new MemoryDevice Device { get { return (MemoryDevice)base.Device; } }
		
		private NativeTables FTables;

		public virtual NativeTables GetTables(Schema.TableVarScope AScope) 
		{
			switch (AScope)
			{
				case Schema.TableVarScope.Process : return FTables == null ? FTables = new NativeTables() : FTables;
				case Schema.TableVarScope.Session : return ServerProcess.ServerSession.Tables;
				case Schema.TableVarScope.Database :
				default : return Device.Tables;
			}
		}
		
		#if USEMEMORYDEVICETRANSACTIONS
		private MemoryDeviceTransactions FTransactions = new MemoryDeviceTransactions();
		public MemoryDeviceTransactions Transactions { get { return FTransactions; } }
		#endif
		
		protected override void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			#if USEMEMORYDEVICETRANSACTIONS
			FTransactions.BeginTransaction(AIsolationLevel);
			#endif
		}
		
		protected override void InternalPrepareTransaction() {}

		protected override void InternalCommitTransaction()
		{
			#if USEMEMORYDEVICETRANSACTIONS
			FTransactions.EndTransaction(true);
			#endif
		}
		
		#if USEMEMORYDEVICETRANSACTIONS
		protected void RemoveTransactionReferences(MemoryDeviceTransaction ATransaction, Schema.TableVar ATableVar)
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
			
			foreach (MemoryDeviceTransaction LTransaction in ATransaction.Transactions)
				RemoveTransactionReferences(LTransaction, ATableVar);
		}
		
		protected void RemoveTransactionReferences(Schema.TableVar ATableVar)
		{
			foreach (MemoryDeviceTransaction LTransaction in FTransactions)
				RemoveTransactionReferences(LTransaction, ATableVar);
		}
		
		protected void InternalRollbackTransaction(MemoryDeviceTransaction ATransaction)
		{
			Operation LOperation;
			InsertOperation LInsertOperation;
			UpdateOperation LUpdateOperation;
			DeleteOperation LDeleteOperation;
			for (int LIndex = ATransaction.Operations.Count - 1; LIndex >= 0; LIndex--)
			{
				LOperation = ATransaction.Operations[LIndex];

				LInsertOperation = LOperation as InsertOperation;
				if (LInsertOperation != null)
					InsertRow(LInsertOperation.TableVar, LInsertOperation.Row);

				LUpdateOperation = LOperation as UpdateOperation;
				if (LUpdateOperation != null)
					UpdateRow(LUpdateOperation.TableVar, LUpdateOperation.OldRow, LUpdateOperation.NewRow);

				LDeleteOperation = LOperation as DeleteOperation;
				if (LDeleteOperation != null)
					DeleteRow(LDeleteOperation.TableVar, LDeleteOperation.Row);
					
				ATransaction.Operations.RemoveAt(LIndex);
				LOperation.Dispose();
			}
			
			foreach (MemoryDeviceTransaction LTransaction in ATransaction.Transactions)
				InternalRollbackTransaction(LTransaction);
		}
		#endif
		
		protected override void InternalRollbackTransaction()
		{
			#if USEMEMORYDEVICETRANSACTIONS
			InternalRollbackTransaction(FTransactions.CurrentTransaction());
			FTransactions.EndTransaction(false);
			#endif
		}
		
		protected NativeTable EnsureNativeTable(Schema.TableVar ATableVar)
		{
			if (!ATableVar.IsSessionObject && !ATableVar.IsATObject)
				ATableVar.Scope = (Schema.TableVarScope)Enum.Parse(typeof(Schema.TableVarScope), MetaData.GetTag(ATableVar.MetaData, "Storage.Scope", "Database"));

			NativeTables LTables = GetTables(ATableVar.Scope);
			lock (LTables)
			{
				int LIndex = LTables.IndexOf(ATableVar);
				if (LIndex < 0)
					LIndex = LTables.Add(new NativeTable(ServerProcess, ATableVar));
				return LTables[LIndex];
			}
		}

		protected override DataVar InternalExecute(Schema.DevicePlan ADevicePlan)
		{
			PlanNode LPlanNode = ADevicePlan.Node;
			if (LPlanNode is BaseTableVarNode)
			{
				MemoryScan LScan = new MemoryScan((BaseTableVarNode)LPlanNode, ServerProcess, this);
				try
				{
					LScan.NativeTable = EnsureNativeTable(((BaseTableVarNode)LPlanNode).TableVar);
					LScan.Key = LScan.NativeTable.ClusteredIndex.Key;
					LScan.Open();
					return new DataVar(String.Empty, LScan.DataType, LScan);
				}
				catch
				{
					LScan.Dispose();
					throw;
				}
			}
			else if (LPlanNode is OrderNode)
			{
				MemoryScan LScan = new MemoryScan((BaseTableVarNode)LPlanNode.Nodes[0], ServerProcess, this);
				try
				{
					LScan.NativeTable = EnsureNativeTable(((BaseTableVarNode)LPlanNode.Nodes[0]).TableVar);
					LScan.Key = ((OrderNode)LPlanNode).PhysicalOrder;
					LScan.Direction = ((OrderNode)LPlanNode).ScanDirection;
					LScan.Node.Order = ((OrderNode)LPlanNode).Order;
					LScan.Open();
					return new DataVar(String.Empty, LScan.DataType, LScan);
				}
				catch
				{
					LScan.Dispose();
					throw;
				}
			}
			else if (LPlanNode is CreateTableVarBaseNode)
			{
				EnsureNativeTable(((CreateTableVarBaseNode)LPlanNode).GetTableVar());
				return null;
			}
			else if (LPlanNode is AlterTableNode)
			{
				// TODO: Memory device alter table support
				return null;
			}
			else if (LPlanNode is DropTableNode)
			{
				Schema.TableVar LTableVar = ((DropTableNode)LPlanNode).Table;
				NativeTables LTables = GetTables(LTableVar.Scope);
				lock (LTables)
				{
					int LTableIndex = LTables.IndexOf(LTableVar);
					if (LTableIndex >= 0)
					{
						NativeTable LNativeTable = LTables[LTableIndex];
						#if USEMEMORYDEVICETRANSACTIONS
						RemoveTransactionReferences(LNativeTable.TableVar);
						#endif
						LNativeTable.Drop(ServerProcess);
						LTables.RemoveAt(LTableIndex);
					}
				}
				return null;
			}
			else
				throw new DeviceException(DeviceException.Codes.InvalidExecuteRequest, Device.Name, LPlanNode.ToString());
		}
		
		protected override void InternalInsertRow(Schema.TableVar ATable, Row ARow, BitArray AValueFlags)
		{
			NativeTable LTable = GetTables(ATable.Scope)[ATable];
			
			if ((Device.MaxRowCount >= 0) && (LTable.RowCount >= Device.MaxRowCount) && (!InTransaction || !ServerProcess.CurrentTransaction.InRollback))
				throw new DeviceException(DeviceException.Codes.MaxRowCountExceeded, Device.MaxRowCount.ToString(), ATable.DisplayName, Device.DisplayName);

			LTable.Insert(ServerProcess, ARow);

			#if USEMEMORYDEVICETRANSACTIONS
			if (InTransaction && !ServerProcess.NonLogged && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new DeleteOperation(ATable, (Row)ARow.Copy()));
			#endif
		}
		
		protected override void InternalUpdateRow(Schema.TableVar ATable, Row AOldRow, Row ANewRow, BitArray AValueFlags)
		{
			GetTables(ATable.Scope)[ATable].Update(ServerProcess, AOldRow, ANewRow);
			#if USEMEMORYDEVICETRANSACTIONS
			if (InTransaction && !ServerProcess.NonLogged && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new UpdateOperation(ATable, (Row)ANewRow.Copy(), (Row)AOldRow.Copy()));
			#endif
		}
		
		protected override void InternalDeleteRow(Schema.TableVar ATable, Row ARow)
		{
			GetTables(ATable.Scope)[ATable].Delete(ServerProcess, ARow);
			#if USEMEMORYDEVICETRANSACTIONS
			if (InTransaction && !ServerProcess.NonLogged && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new InsertOperation(ATable, (Row)ARow.Copy()));
			#endif
		}
	}
	
	public class MemoryScan : TableScan
	{
		public MemoryScan(TableNode ANode, ServerProcess AProcess, MemoryDeviceSession ASession) : base(ANode, AProcess)
		{
			FSession = ASession;
		}
		
		private MemoryDeviceSession FSession; 
		public MemoryDeviceSession Session { get { return FSession; } }
	}
}

