/*
	Dataphor
	© Copyright 2000-2012 Alphora
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
            return new FastoreDeviceSession(this, serverProcess, deviceSessionInfo, Database);
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

            //Connect to the Fastore Service
            _db = Client.Connect("localhost", 8064);

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
}
