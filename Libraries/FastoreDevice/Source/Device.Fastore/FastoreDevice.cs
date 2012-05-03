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
using System.Collections.Generic;
using Wrapper;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
    /// <summary>
    /// Fastore Storage device
    /// </summary>
	public class FastoreDevice : Schema.Device
	{
        //TODO: RowID Generators, Table Groupings, etc.
        
        private static FastoreTables _tables;
        public FastoreTables Tables { get { return _tables; } }

        private static ManagedHost _host;
        public ManagedHost Host { get { return _host; } }

        private static ManagedSession _msession;
        public ManagedSession MSession { get { return _msession; } }

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

        //Fire up Fastore, create a host, connect to a session.
        protected override void InternalStart(ServerProcess process)
        {
            base.InternalStart(process);

            ManagedHostFactory hf = new ManagedHostFactory();
            ManagedTopology topo = new ManagedTopology();
            _host = hf.Create(topo);
            _msession = new ManagedDatabase(_host).Start();
            _tables = new FastoreTables();
        }

        //Free all fastore memory.
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

            //TODO: Kill db, Free resources, etc.

            base.InternalStop(process);
        }

        //TODO: Figure this out. This decides whether this device can support a given plan.
        //What does each node mean? And What do we support? Leave as is for now.
        protected override DevicePlanNode InternalPrepare(Schema.DevicePlan plan, PlanNode planNode)
        {
            if (planNode is BaseTableVarNode)
            {
                BaseTableVarNode node = (BaseTableVarNode)planNode;
                node.CursorType = CursorType.Dynamic;
                node.RequestedCursorType = plan.Plan.CursorContext.CursorType;
                node.CursorCapabilities =
                    CursorCapability.Navigable;
                node.CursorIsolation = plan.Plan.CursorContext.CursorIsolation;
                node.Order = Compiler.OrderFromKey(plan.Plan, Compiler.FindClusteringKey(plan.Plan, node.TableVar));
                return new DevicePlanNode(node);
            }
            else if ((planNode is OrderNode) && (planNode.Nodes[0] is BaseTableVarNode) && (plan.Plan.CursorContext.CursorType != CursorType.Static))
            {
                //Wah Wah... Ordering not supported at all!! For now...


                //OrderNode node = (OrderNode)planNode;
                //BaseTableVarNode tableVarNode = (BaseTableVarNode)planNode.Nodes[0];
                //Schema.Order tableOrder;

                //bool isSupported = false;

                //var fastTable = Tables[tableVarNode.TableVar];
                //foreach (Schema.Key key in tableVarNode.TableVar.Keys)
                //{
                //    tableOrder = Compiler.OrderFromKey(plan.Plan, key);
                //    if (node.RequestedOrder.Equivalent(tableOrder))
                //    {
                //        node.PhysicalOrder = tableOrder;
                //        node.ScanDirection = ScanDirection.Forward;
                //        isSupported = true;
                //        break;
                //    }
                //    else if (node.RequestedOrder.Equivalent(new Schema.Order(tableOrder, true)))
                //    {
                //        node.PhysicalOrder = tableOrder;
                //        node.ScanDirection = ScanDirection.Backward;
                //        isSupported = true;
                //        break;
                //    }
                //}

                //if (!isSupported)
                //    foreach (Schema.Order order in tableVarNode.TableVar.Orders)
                //        if (node.RequestedOrder.Equivalent(order))
                //        {
                //            node.PhysicalOrder = order;
                //            node.ScanDirection = ScanDirection.Forward;
                //            isSupported = true;
                //            break;
                //        }
                //        else if (node.RequestedOrder.Equivalent(new Schema.Order(order, true)))
                //        {
                //            node.PhysicalOrder = order;
                //            node.ScanDirection = ScanDirection.Backward;
                //            isSupported = true;
                //            break;
                //        }

                //if (isSupported)
                //{
                //    node.Order = new Schema.Order();
                //    node.Order.MergeMetaData(node.RequestedOrder.MetaData);
                //    node.Order.IsInherited = false;
                //    Schema.OrderColumn orderColumn;
                //    Schema.OrderColumn newOrderColumn;
                //    for (int index = 0; index < node.PhysicalOrder.Columns.Count; index++)
                //    {
                //        orderColumn = node.PhysicalOrder.Columns[index];
                //        newOrderColumn =
                //            new Schema.OrderColumn
                //            (
                //                node.TableVar.Columns[orderColumn.Column],
                //                node.ScanDirection == ScanDirection.Forward ?
                //                    orderColumn.Ascending :
                //                    !orderColumn.Ascending
                //            );
                //        newOrderColumn.Sort = orderColumn.Sort;
                //        newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
                //        Error.AssertWarn(newOrderColumn.Sort != null, "Sort is null");
                //        node.Order.Columns.Add(newOrderColumn);
                //    }
                //    if (!node.TableVar.Orders.Contains(node.Order))
                //        node.TableVar.Orders.Add(node.Order);

                //    node.CursorType = CursorType.Dynamic;
                //    node.RequestedCursorType = plan.Plan.CursorContext.CursorType;
                //    node.CursorCapabilities =
                //        CursorCapability.Navigable |
                //        CursorCapability.BackwardsNavigable |
                //        CursorCapability.Bookmarkable |
                //        CursorCapability.Searchable |
                //        (plan.Plan.CursorContext.CursorCapabilities & CursorCapability.Updateable);
                //    node.CursorIsolation = plan.Plan.CursorContext.CursorIsolation;

                //    return new DevicePlanNode(node);
                //}
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
		) : base(device, serverProcess, deviceSessionInfo)
        {
            _fdevice = (FastoreDevice)device;
            if (device == null)
                throw new Exception("Device is null");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private FastoreDevice _fdevice;

        public new FastoreDevice Device { get { return _fdevice; } }
        
        //TODO: Scope? Hmm... Our scope is tied explicitly to session for now.
        public virtual FastoreTables GetTables(Schema.TableVarScope scope)
        {
            return Device.Tables;
        }

        //TODO: Transactions - Not in V1
        protected override void InternalBeginTransaction(IsolationLevel isolationLevel) { }
        protected override void InternalPrepareTransaction() { }
        protected override void InternalCommitTransaction() { }
        protected override void InternalRollbackTransaction() { }

        //TODO: FastoreTable? This function is used internally to make sure tables exist
        //In the case of Fastore, this means reading the tableVar, deciding what the columns will look like, and creating them.
        protected FastoreTable EnsureFastoreTable(Schema.TableVar tableVar)
        {
            if (!tableVar.IsSessionObject && !tableVar.IsATObject)
                tableVar.Scope = (Schema.TableVarScope)Enum.Parse(typeof(Schema.TableVarScope), MetaData.GetTag(tableVar.MetaData, "Storage.Scope", "Database"), false);

            FastoreTables tables = GetTables(tableVar.Scope);
            lock (tables)
            {
                int index = tables.IndexOf(tableVar);
                if (index < 0)
                    index = tables.Add(new FastoreTable(ServerProcess.ValueManager, tableVar, this.Device));
                return tables[index];
            }
        }

        //Execute is what actually returns a value? Plan is executed which return a scan.
        protected override object InternalExecute(Program program, PlanNode planNode)
        {
            if (planNode is BaseTableVarNode)
            {
                FastoreScan scan = new FastoreScan(program, this, (BaseTableVarNode)planNode);
                try
                {
                    scan.FastoreTable = EnsureFastoreTable(((BaseTableVarNode)planNode).TableVar);
                    //scan.Key = scan.NativeTable.ClusteredIndex.Key;
                    scan.Open();
                    return scan;
                }
                catch
                {
                    scan.Dispose();
                    throw;
                }
            }
            //else if (planNode is OrderNode)
            //{
            //    FastoreScan scan = new FastoreScan(program, this, (BaseTableVarNode)planNode.Nodes[0]);
            //    try
            //    {
            //        scan.FastoreTable = EnsureFastoreTable(((BaseTableVarNode)planNode.Nodes[0]).TableVar);
            //        //scan.Key = ((OrderNode)planNode).PhysicalOrder;
            //        //scan.Direction = ((OrderNode)planNode).ScanDirection;
            //        scan.Node.Order = ((OrderNode)planNode).Order;
            //        scan.Open();
            //        return scan;
            //    }
            //    catch
            //    {
            //        scan.Dispose();
            //        throw;
            //    }
            //}
            else if (planNode is CreateTableVarBaseNode)
            {
                EnsureFastoreTable(((CreateTableVarBaseNode)planNode).GetTableVar());
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
                FastoreTables tables = GetTables(tableVar.Scope);
                lock (tables)
                {
                    int tableIndex = tables.IndexOf(tableVar);
                    if (tableIndex >= 0)
                    {
                        FastoreTable nativeTable = tables[tableIndex];
                        nativeTable.Drop(program.ValueManager);
                        tables.RemoveAt(tableIndex);
                    }
                }
                return null;
            }
            else
                throw new DeviceException(DeviceException.Codes.InvalidExecuteRequest, Device.Name, planNode.ToString());
        }

        //Row level operations. Called from base class which calls internal functions.
        protected override void InternalInsertRow(Program program, Schema.TableVar table, Row row, BitArray valueFlags)
        {
            GetTables(table.Scope)[table].Insert(ServerProcess.ValueManager, row);
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

    //This will have to be a buffered read of the FastoreStorage engine
    public class FastoreScan : Table
    {
        public FastoreScan(Program program, FastoreDeviceSession session, TableNode node)
            : base(node, program)
        {
            _session = session;
        }

        public FastoreTable FastoreTable;

        private FastoreDeviceSession _session;
        public FastoreDeviceSession Session { get { return _session; } }

        private ManagedDataSet _set;
        private int _currow = -1;

        protected override void InternalOpen()
        {
            _set = new ManagedDatabase(Session.Device.Host).Start().GetRange(FastoreTable.Columns, new ManagedOrder[0], new ManagedRange[0]);
        }

        protected override void InternalClose()
        {
            _set = null;
        }

        protected override void InternalSelect(Row row)
        {
                object[] managedRow = _set.Row(_currow);

                NativeRow nRow = new NativeRow(FastoreTable.TableVar.Columns.Count);
                for (int i = 0; i < FastoreTable.TableVar.Columns.Count; i++)
                {
                    nRow.Values[i] = managedRow[i]; 
                }

                Row localRow = new Row(Manager, FastoreTable.TableVar.DataType.RowType, nRow);               

                localRow.CopyTo(row);
        }

        protected override bool InternalNext()
        {
            _currow++;

            return _currow < _set.Size();
        }

        protected override bool InternalBOF()
        {
            return false;
        }

        protected override bool InternalEOF()
        {
            return _currow == _set.Size();
        }
    }

    //TODO: FIX NASTY ASSUMPTIONS! (First column is ID column -- This is clearly wrong, but I"m just getting things up and running)
    public class FastoreTable : System.Object
    {
        //Tied Directly to device for time being...
        public FastoreTable(IValueManager manager, Schema.TableVar tableVar, FastoreDevice device)
        {
            TableVar = tableVar;
            Device = device;

            _keys = new Schema.Keys();
            _orders = new Schema.Orders();
            EnsureColumns();
        }

        public FastoreDevice Device;
        public Schema.TableVar TableVar;

        private Keys _keys;
        public Keys Keys { get { return _keys; } }

        private Orders _orders;
        public Orders Orders { get { return _orders; } }

        private int[] _columns;
        public int[] Columns { get { return _columns; } }


        protected string MapTypeNames(string tname)
        {
            string name = "";
            switch (tname)
            {
                case "System.String":
                    name = "WString";
                    break;
                case "System.Integer":
                    name = "Int";
                    break;
                default:
                    break;
            }

            return name;
        }

        protected void EnsureColumns()
        {
            List<int> columnIds = new List<int>();

            for (int i = 0; i < TableVar.DataType.Columns.Count; i++)
            {
                var col = TableVar.DataType.Columns[i];
                ManagedColumnDef def = new ManagedColumnDef();

                columnIds.Add(col.ID);

                def.ColumnID = col.ID;
                def.ValueType = MapTypeNames(col.DataType.Name);
                def.Name = col.Description;
                def.RowIDType = "Int";

                //Unique? Required?
                if (i == 1)
                {
                    def.IsUnique = true;
                    def.IsRequired = true;
                }
                else
                {
                    def.IsUnique = false;
                    def.IsRequired = false;
                }              

                if (!Device.Host.ExistsColumn(col.ID))
                {
                    Device.Host.CreateColumn(def);
                }
            }

            _columns = columnIds.ToArray();
        }

        public void Insert(IValueManager manager, Row row)
        {
            if (row.HasValue(0))
            {
                List<object> items = new List<object>();

                for (int i = 0; i < row.DataType.Columns.Count; i++)
                {
                    items.Add(row[i]);
                }

                new ManagedDatabase(Device.Host).Start().Include(items[0], items.ToArray(), _columns);
            }
        }

        public void Update(IValueManager manager, Row oldrow, Row newrow)
        {
            if (oldrow.HasValue(0))
            {
                var id = oldrow[0];
                for (int i = 0; i < _columns.Length; i++)
                {
                    //TODO: Fix Api.. Exclude and Include and not symmetrical (multiple rows on exclude?)
                    new ManagedDatabase(Device.Host).Start().Exclude(new object[] { id }, _columns);
                }
            }

            if (newrow.HasValue(0))
            {
                List<object> items = new List<object>();

                for (int i = 0; i < newrow.DataType.Columns.Count; i++)
                {
                    items.Add(newrow[i]);
                }

                new ManagedDatabase(Device.Host).Start().Include(newrow[0], items.ToArray(), _columns);
            }
        }

        public void Delete(IValueManager manager, Row row)
        {
            //If no value at zero, no ID (Based on our wrong assumptions)
            if (row.HasValue(0))
            {
                var id = row[0];
                for (int i = 0; i < _columns.Length; i++)
                {
                    new ManagedDatabase(Device.Host).Start().Exclude(id, _columns);
                }
            }
        }

        public void Drop(IValueManager manager)
        {
            foreach (var col in _columns)
            {
                Device.Host.DeleteColumn(col);
            }
        }
    }

    public class FastoreTables : List
    {
        public FastoreTables() : base() { }

        public new FastoreTable this[int index]
        {
            get { lock (this) { return (FastoreTable)base[index]; } }
            set { lock (this) { base[index] = value; } }
        }

        public int IndexOf(Schema.TableVar tableVar)
        {
            lock (this)
            {
                for (int index = 0; index < Count; index++)
                    if (this[index].TableVar == tableVar)
                        return index;
                return -1;
            }
        }

        public bool Contains(Schema.TableVar tableVar)
        {
            return IndexOf(tableVar) >= 0;
        }

        public FastoreTable this[Schema.TableVar tableVar]
        {
            get
            {
                lock (this)
                {
                    int index = IndexOf(tableVar);
                    if (index < 0)
                        throw new RuntimeException(RuntimeException.Codes.NativeTableNotFound, tableVar.DisplayName);
                    return this[index];
                }
            }
        }
    }    
}
