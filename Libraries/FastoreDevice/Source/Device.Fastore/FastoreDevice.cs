/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
    /// <summary>
    /// Fastore Storage device
    /// </summary>
	public class FastoreDevice : Schema.Device
	{
        //TODO: RowID Generators, Table Groupings, etc.
        
        //TODO: FastoreTables?
        private NativeTables _tables;
        public NativeTables Tables { get { return _tables; } }

        public FastoreDevice(int iD, string name)
            : base(iD, name)
        {
            IgnoreUnsupported = true;
            RequiresAuthentication = false;
        }

        protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
        {
            return new FastoreDeviceSession(this, serverProcess, deviceSessionInfo);
        }

        public override DeviceCapability Capabilities
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

        //TODO: Figure this out.
        protected override DevicePlanNode InternalPrepare(Schema.DevicePlan plan, PlanNode planNode)
        {
            if (planNode is BaseTableVarNode)
            {
                BaseTableVarNode node = (BaseTableVarNode)planNode;
                node.CursorType = CursorType.Dynamic;
                node.RequestedCursorType = plan.Plan.CursorContext.CursorType;
                node.CursorCapabilities =
                    CursorCapability.Navigable |
                    CursorCapability.BackwardsNavigable |
                    CursorCapability.Bookmarkable |
                    CursorCapability.Searchable |
                    (plan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
                node.CursorIsolation = plan.Plan.CursorContext.CursorIsolation;
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

                    node.CursorType = CursorType.Dynamic;
                    node.RequestedCursorType = plan.Plan.CursorContext.CursorType;
                    node.CursorCapabilities =
                        CursorCapability.Navigable |
                        CursorCapability.BackwardsNavigable |
                        CursorCapability.Bookmarkable |
                        CursorCapability.Searchable |
                        (plan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
                    node.CursorIsolation = plan.Plan.CursorContext.CursorIsolation;

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
	}

    //TODO: Transactions

	public class FastoreDeviceSession : Schema.DeviceSession
	{
		protected internal FastoreDeviceSession
		(
			Schema.Device device, 
			ServerProcess serverProcess, 
			Schema.DeviceSessionInfo deviceSessionInfo
		) : base(device, serverProcess, deviceSessionInfo){}

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public new FastoreDevice Device { get { return (FastoreDevice)base.Device; } }

        private NativeTables _tables;
        
        //TODO: Scope? Huh?
        public virtual NativeTables GetTables(Schema.TableVarScope scope)
        {
            switch (scope)
            {
                case Schema.TableVarScope.Process: return _tables == null ? _tables = new NativeTables() : _tables;
                case Schema.TableVarScope.Session: return ServerProcess.ServerSession.Tables;
                case Schema.TableVarScope.Database:
                default: return Device.Tables;
            }
        }

        //TODO: Transactions
        protected override void InternalBeginTransaction(IsolationLevel isolationLevel) { }
        protected override void InternalPrepareTransaction() { }
        protected override void InternalCommitTransaction() { }
        protected override void InternalRollbackTransaction() { }

        //TODO: FastoreTable? This function is used internally to make sure tables exist
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
                FastoreScan scan = new FastoreScan(program, this, (BaseTableVarNode)planNode);
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
                FastoreScan scan = new FastoreScan(program, this, (BaseTableVarNode)planNode.Nodes[0]);
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
                        nativeTable.Drop(program.ValueManager);
                        tables.RemoveAt(tableIndex);
                    }
                }
                return null;
            }
            else
                throw new DeviceException(DeviceException.Codes.InvalidExecuteRequest, Device.Name, planNode.ToString());
        }

        protected override void InternalInsertRow(Program program, Schema.TableVar table, Row row, BitArray valueFlags)
        {
            NativeTable localTable = GetTables(table.Scope)[table];
            localTable.Insert(ServerProcess.ValueManager, row);
        }

        protected override void InternalUpdateRow(Program program, Schema.TableVar table, Row oldRow, Row newRow, BitArray valueFlags)
        {
            GetTables(table.Scope)[table].Update(ServerProcess.ValueManager, oldRow, newRow);
        }

        protected override void InternalDeleteRow(Program program, Schema.TableVar table, Row row)
        {
            GetTables(table.Scope)[table].Delete(ServerProcess.ValueManager, row);
        }
	}

    public class FastoreScan : TableScan
    {
        public FastoreScan(Program program, FastoreDeviceSession session, TableNode node)
            : base(node, program)
        {
            _session = session;
        }

        private FastoreDeviceSession _session;
        public FastoreDeviceSession Session { get { return _session; } }
    }
}
