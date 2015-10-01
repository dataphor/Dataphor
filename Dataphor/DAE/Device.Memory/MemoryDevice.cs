/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
	
namespace Alphora.Dataphor.DAE.Device.Memory
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	/// <summary> In-memory storage device. </summary>
	public class MemoryDevice : Schema.Device
	{
		public MemoryDevice(int iD, string name) : base(iD, name) 
		{
			IgnoreUnsupported = true;
			RequiresAuthentication = false;
		}
		
		protected override Schema.DeviceSession InternalConnect(ServerProcess serverProcess, Schema.DeviceSessionInfo deviceSessionInfo)
		{
			return new MemoryDeviceSession(this, serverProcess, deviceSessionInfo);
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
		private int _maxRowCount = -1;
		public int MaxRowCount
		{
			get { return _maxRowCount; }
			set { _maxRowCount = value; }
		}

		protected void PrepareTableNode(Schema.DevicePlan plan, TableNode node)
		{
			node.CursorType = CursorType.Dynamic;
			node.RequestedCursorType = plan.Plan.CursorContext.CursorType;
			node.CursorCapabilities =
				CursorCapability.Navigable |
				CursorCapability.BackwardsNavigable |
				CursorCapability.Bookmarkable |
				CursorCapability.Searchable |
				(plan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
			node.CursorIsolation = plan.Plan.CursorContext.CursorIsolation;
		}

		protected override DevicePlanNode InternalPrepare(Schema.DevicePlan plan, PlanNode planNode)
		{
			if (planNode is BaseTableVarNode)
			{
				BaseTableVarNode node = (BaseTableVarNode)planNode;
				PrepareTableNode(plan, node);
				node.Order = Compiler.OrderFromKey(plan.Plan, Compiler.FindClusteringKey(plan.Plan, node.TableVar));
				return new DevicePlanNode(node);
			}
			else if ((planNode is OrderNode) && (planNode.Nodes[0] is BaseTableVarNode) && (plan.Plan.CursorContext.CursorType != CursorType.Static))
			{
				OrderNode node = (OrderNode)planNode;
				BaseTableVarNode tableVarNode = (BaseTableVarNode)planNode.Nodes[0];
				Schema.Order tableOrder;

				bool isSupported = false;
				foreach (Schema.Key key in tableVarNode.TableVar.Keys)
				{
					tableOrder = Compiler.OrderFromKey(plan.Plan, key);
					if (node.RequestedOrder.Equivalent(tableOrder))
					{
						node.PhysicalOrder = tableOrder;
						node.ScanDirection = ScanDirection.Forward;
						isSupported = true;
						break;
					}
					else if (node.RequestedOrder.Equivalent(new Schema.Order(tableOrder, true)))
					{
						node.PhysicalOrder = tableOrder;
						node.ScanDirection = ScanDirection.Backward;
						isSupported = true;
						break;
					}
				}

				if (!isSupported)
					foreach (Schema.Order order in tableVarNode.TableVar.Orders)
						if (node.RequestedOrder.Equivalent(order))
						{
							node.PhysicalOrder = order;
							node.ScanDirection = ScanDirection.Forward;
							isSupported = true;
							break;
						}
						else if (node.RequestedOrder.Equivalent(new Schema.Order(order, true)))
						{
							node.PhysicalOrder = order;
							node.ScanDirection = ScanDirection.Backward;
							isSupported = true;
							break;
						}

				if (isSupported)
				{
					node.Order = new Schema.Order();
					node.Order.MergeMetaData(node.RequestedOrder.MetaData);
					node.Order.IsInherited = false;
					Schema.OrderColumn orderColumn;
					Schema.OrderColumn newOrderColumn;
					for (int index = 0; index < node.PhysicalOrder.Columns.Count; index++)
					{
						orderColumn = node.PhysicalOrder.Columns[index];
						newOrderColumn = 
							new Schema.OrderColumn
							(
								node.TableVar.Columns[orderColumn.Column], 
								node.ScanDirection == ScanDirection.Forward ? 
									orderColumn.Ascending : 
									!orderColumn.Ascending
							);
						newOrderColumn.Sort = orderColumn.Sort;
						newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
						Error.AssertWarn(newOrderColumn.Sort != null, "Sort is null");
						node.Order.Columns.Add(newOrderColumn);
					}
					if (!node.TableVar.Orders.Contains(node.Order))
						node.TableVar.Orders.Add(node.Order);

					PrepareTableNode(plan, node);
					PrepareTableNode(plan, tableVarNode);
					
					return new DevicePlanNode(node);
				}
			}
			else if (planNode is CreateTableVarBaseNode)
			{
				plan.Plan.CheckRight(GetRight(Schema.RightNames.CreateStore));
				return new DevicePlanNode(planNode);
			}
			else if (planNode is AlterTableNode)
			{
				plan.Plan.CheckRight(GetRight(Schema.RightNames.AlterStore));
				AlterTableNode alterTableNode = (AlterTableNode)planNode;
				if (alterTableNode.AlterTableStatement.CreateColumns.Count > 0)
					throw new RuntimeException(RuntimeException.Codes.UnimplementedCreateCommand, "Columns in a memory device");
				if (alterTableNode.AlterTableStatement.DropColumns.Count > 0)
					throw new RuntimeException(RuntimeException.Codes.UnimplementedDropCommand, "Columns in a memory device");
				return new DevicePlanNode(planNode);
			}
			else if (planNode is DropTableNode)
			{
				plan.Plan.CheckRight(GetRight(Schema.RightNames.DropStore));
				return new DevicePlanNode(planNode);
			}
			plan.IsSupported = false;
			return null;
		}
		
		protected override void InternalStart(ServerProcess process)
		{
			base.InternalStart(process);
			_tables = new NativeTables();
		}
		
		protected override void InternalStop(ServerProcess process)
		{
			if (_tables != null)
			{
				while (_tables.Count > 0)
				{
					_tables[0].Drop(process.ValueManager);
					_tables.RemoveAt(0);
				}

				_tables = null;
			}
			base.InternalStop(process);
		}
		
		private NativeTables _tables;
		public NativeTables Tables { get { return _tables; } }
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

	#if USETYPEDLIST
	public class MemoryDeviceTransactions : TypedList
	{
		public MemoryDeviceTransactions() : base(typeof(MemoryDeviceTransaction)){}
		
		public new MemoryDeviceTransaction this[int AIndex]
		{
			get { return (MemoryDeviceTransaction)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class MemoryDeviceTransactions : BaseList<MemoryDeviceTransaction>
	{
	#endif
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
			Schema.Device device, 
			ServerProcess serverProcess, 
			Schema.DeviceSessionInfo deviceSessionInfo
		) : base(device, serverProcess, deviceSessionInfo){}
		
		protected override void Dispose(bool disposing)
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
				base.Dispose(disposing);
			}
		}
		
		public new MemoryDevice Device { get { return (MemoryDevice)base.Device; } }
		
		private NativeTables _tables;

		public virtual NativeTables GetTables(Schema.TableVarScope scope) 
		{
			switch (scope)
			{
				case Schema.TableVarScope.Process : return _tables == null ? _tables = new NativeTables() : _tables;
				case Schema.TableVarScope.Session : return ServerProcess.ServerSession.Tables;
				case Schema.TableVarScope.Database :
				default : return Device.Tables;
			}
		}
		
		#if USEMEMORYDEVICETRANSACTIONS
		private MemoryDeviceTransactions FTransactions = new MemoryDeviceTransactions();
		public MemoryDeviceTransactions Transactions { get { return FTransactions; } }
		#endif
		
		protected override void InternalBeginTransaction(IsolationLevel isolationLevel)
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
		
		protected NativeTable EnsureNativeTable(Schema.TableVar tableVar)
		{
			if (!tableVar.IsSessionObject && !tableVar.IsATObject)
				tableVar.Scope = (Schema.TableVarScope)Enum.Parse(typeof(Schema.TableVarScope), MetaData.GetTag(tableVar.MetaData, "Storage.Scope", "Database"), false);

			NativeTables tables = GetTables(tableVar.Scope);
			lock (tables)
			{
				int index = tables.IndexOf(tableVar);
				if (index < 0)
					index = tables.Add(new NativeTable(ServerProcess.ValueManager, tableVar));
				return tables[index];
			}
		}

		protected override object InternalExecute(Program program, PlanNode planNode)
		{
			if (planNode is BaseTableVarNode)
			{
				MemoryScan scan = new MemoryScan(program, this, (BaseTableVarNode)planNode);
				try
				{
					scan.NativeTable = EnsureNativeTable(((BaseTableVarNode)planNode).TableVar);
					scan.Key = scan.NativeTable.ClusteredIndex.Key;
					scan.Open();
					return scan;
				}
				catch
				{
					scan.Dispose();
					throw;
				}
			}
			else if (planNode is OrderNode)
			{
				MemoryScan scan = new MemoryScan(program, this, (BaseTableVarNode)planNode.Nodes[0]);
				try
				{
					scan.NativeTable = EnsureNativeTable(((BaseTableVarNode)planNode.Nodes[0]).TableVar);
					scan.Key = ((OrderNode)planNode).PhysicalOrder;
					scan.Direction = ((OrderNode)planNode).ScanDirection;
					scan.Node.Order = ((OrderNode)planNode).Order;
					scan.Open();
					return scan;
				}
				catch
				{
					scan.Dispose();
					throw;
				}
			}
			else if (planNode is CreateTableVarBaseNode)
			{
				EnsureNativeTable(((CreateTableVarBaseNode)planNode).GetTableVar());
				return null;
			}
			else if (planNode is AlterTableNode)
			{
				// TODO: Memory device alter table support
				return null;
			}
			else if (planNode is DropTableNode)
			{
				Schema.TableVar tableVar = ((DropTableNode)planNode).Table;
				NativeTables tables = GetTables(tableVar.Scope);
				lock (tables)
				{
					int tableIndex = tables.IndexOf(tableVar);
					if (tableIndex >= 0)
					{
						NativeTable nativeTable = tables[tableIndex];
						#if USEMEMORYDEVICETRANSACTIONS
						RemoveTransactionReferences(nativeTable.TableVar);
						#endif
						nativeTable.Drop(program.ValueManager);
						tables.RemoveAt(tableIndex);
					}
				}
				return null;
			}
			else
				throw new DeviceException(DeviceException.Codes.InvalidExecuteRequest, Device.Name, planNode.ToString());
		}
		
		protected override void InternalInsertRow(Program program, Schema.TableVar table, IRow row, BitArray valueFlags)
		{
			NativeTable localTable = GetTables(table.Scope)[table];
			
			if ((Device.MaxRowCount >= 0) && (localTable.RowCount >= Device.MaxRowCount) && (!InTransaction || !ServerProcess.CurrentTransaction.InRollback))
				throw new DeviceException(DeviceException.Codes.MaxRowCountExceeded, Device.MaxRowCount.ToString(), table.DisplayName, Device.DisplayName);

			localTable.Insert(ServerProcess.ValueManager, row);

			#if USEMEMORYDEVICETRANSACTIONS
			if (InTransaction && !ServerProcess.NonLogged && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new DeleteOperation(ATable, (IRow)ARow.Copy()));
			#endif
		}
		
		protected override void InternalUpdateRow(Program program, Schema.TableVar table, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			GetTables(table.Scope)[table].Update(ServerProcess.ValueManager, oldRow, newRow);
			#if USEMEMORYDEVICETRANSACTIONS
			if (InTransaction && !ServerProcess.NonLogged && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new UpdateOperation(ATable, (IRow)ANewRow.Copy(), (IRow)AOldRow.Copy()));
			#endif
		}
		
		protected override void InternalDeleteRow(Program program, Schema.TableVar table, IRow row)
		{
			GetTables(table.Scope)[table].Delete(ServerProcess.ValueManager, row);
			#if USEMEMORYDEVICETRANSACTIONS
			if (InTransaction && !ServerProcess.NonLogged && !ServerProcess.CurrentTransaction.InRollback)
				Transactions.CurrentTransaction().Operations.Add(new InsertOperation(ATable, (IRow)ARow.Copy()));
			#endif
		}
	}
	
	public class MemoryScan : TableScan
	{
		public MemoryScan(Program program, MemoryDeviceSession session, TableNode node) : base(node, program)
		{
			_session = session;
		}
		
		private MemoryDeviceSession _session; 
		public MemoryDeviceSession Session { get { return _session; } }
	}
}

