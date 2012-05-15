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
using Alphora.Fastore.Client;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
    /// <summary>
    /// Fastore Storage device
    /// </summary>
	public class FastoreDevice : Schema.Device
	{
        //TODO: RowID Generators, Table Groupings, etc.
        
        //TableVar Mappings
        private FastoreTables _tables;
        public FastoreTables Tables { get { return _tables; } }

        //Hosting Fastore in process. Tied to the device.
        //If the device dies, it's game over.
        private Database _db = null;
        public Database Database { get { return _db; } }

        public FastoreDevice(int iD, string name)
            : base(iD, name)
        {
            IgnoreUnsupported = true;
            RequiresAuthentication = false;
        }

        protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
        {
            return new FastoreDeviceSession(this, serverProcess, deviceSessionInfo, Database.Start());
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

            //Start the fastore service
            //Connect to the service. For now this is just one "Host"
            //Create the host (will eventually connect to host)
           // ManagedHostFactory hf = new ManagedHostFactory();
            //ManagedTopology topo = new ManagedTopology();
            //_host = hf.Create(topo);

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
			Schema.DeviceSessionInfo deviceSessionInfo,
            Session session
		) : base(device, serverProcess, deviceSessionInfo)
        {
            _session = session;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private Session _session;

        public new FastoreDevice Device { get { return (FastoreDevice)base.Device; } }
        
        //TODO: Scope? Hmm... Our scope is tied explicitly to device for now. Once it disappears, it's gone.
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
                FastoreScan scan = new FastoreScan(program, _session, (BaseTableVarNode)planNode);
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
            GetTables(table.Scope)[table].Insert(ServerProcess.ValueManager, row, _session);
        }

        protected override void InternalUpdateRow(Program program, Schema.TableVar table, Row oldRow, Row newRow, BitArray valueFlags)
        {
            GetTables(table.Scope)[table].Update(ServerProcess.ValueManager, oldRow, newRow, _session);
        }

        protected override void InternalDeleteRow(Program program, Schema.TableVar table, Row row)
        {
            GetTables(table.Scope)[table].Delete(ServerProcess.ValueManager, row, _session);
        }
	}

    //This will have to be a buffered read of the FastoreStorage engine
    public class FastoreScan : Table
    {
        public FastoreScan(Program program, Session session, TableNode node)
            : base(node, program)
        {
            _session = session;
        }

        public FastoreTable FastoreTable;

        private Session _session;  

        private DataSet _set;
        private int _currow = -1;

        public Orders Orders = new Orders();

        private Alphora.Fastore.Client.Order[] OrdersToFastoreOrders()
        {
            return new Alphora.Fastore.Client.Order[0];
        }

        //TODO: set parameters before opening. Start and end range, Orders, etc.
        protected override void InternalOpen()
        {
            _set = _session.GetRange(FastoreTable.Columns, OrdersToFastoreOrders(), new Range[0]);
        }

        protected override void InternalClose()
        {
            _set = null;
        }

        protected override void InternalSelect(Row row)
        {
                object[] managedRow = _set[_currow];

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

            return _currow < _set.Count;
        }

        protected override bool InternalBOF()
        {
            return false;
        }

        protected override bool InternalEOF()
        {
            return _currow == _set.Count;
        }
    }

    //TODO: FIX NASTY ASSUMPTIONS! (First column is ID column -- This is clearly wrong, but I'm just getting things up and running)
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
                case "System.Boolean":
                    name = "Bool";
                    break;
                case "System.Long":
                    name = "Long";
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
                ColumnDef def = new ColumnDef();

                columnIds.Add(col.ID);

                def.ColumnID = col.ID;
                def.Type = MapTypeNames(col.DataType.Name);
                def.Name = col.Description;
                def.IDType = "Int";

                //Unique? Required?
                if (i == 1)
                {
                    def.IsUnique = true;

                }
                else
                {
                    def.IsUnique = false;
                }              

                if (!Device.Database.ExistsColumn(col.ID))
                {
                    Device.Database.CreateColumn(def);
                }
            }

            _columns = columnIds.ToArray();
        }

        public void Insert(IValueManager manager, Row row, Session session)
        {
            if (row.HasValue(0))
            {
                object[] items = ((NativeRow)row.AsNative).Values;

                session.Include(_columns, items[0], items);
            }
        }

        public void Update(IValueManager manager, Row oldrow, Row newrow, Session session)
        {
            if (oldrow.HasValue(0))
            {
                var id = oldrow[0];
                for (int i = 0; i < _columns.Length; i++)
                {
                    session.Exclude(_columns, id);
                }
            }

            if (newrow.HasValue(0))
            {
                object[] items = ((NativeRow)newrow.AsNative).Values;
                session.Include(_columns, items[0], items);
            }
        }

        public void Delete(IValueManager manager, Row row, Session session)
        {
            //If no value at zero, no ID (Based on our wrong assumptions)
            if (row.HasValue(0))
            {
                session.Exclude(_columns, row[0]);
            }
        }

        public void Drop(IValueManager manager)
        {
            foreach (var col in _columns)
            {
                if (Device.Database.ExistsColumn(col))
                {
                    Device.Database.DeleteColumn(col);
                }
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
