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
		private string _serviceAddresses = "localhost:8765";
		/// <summary> Semicolon separated list of address:port pairs. </summary>
		public string ServiceAddresses { get { return _serviceAddresses; } }

		//TODO: Table Groupings, etc.
        
        // TableVar Mappings
        private FastoreTables _tables;
        public FastoreTables Tables { get { return _tables; } }

        private Database _db = null;
        public Database Database { get { return _db; } }

        // Generates new ids for a table.
        private Generator _generator = null;
        public Generator IDGenerator { get { return _generator; } }

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

			var addresses = GetAddresses();

            //Connect to the Fastore Service
            _db = Alphora.Fastore.Client.Client.Connect(addresses);
            _generator = new Alphora.Fastore.Client.Generator(_db);

            _tables = new FastoreTables();
        }

		private ServiceAddress[] GetAddresses()
		{
			var addresses = new List<ServiceAddress>();
			foreach (var address in _serviceAddresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var items = address.Split(':');
				if (items.Length < 1 || String.IsNullOrWhiteSpace(items[0]))
					throw new Exception("Must specify a complete service address.");
				int port = ServiceAddress.DefaultPort;
				if (items.Length > 1)
					Int32.TryParse(items[1], out port);
				addresses.Add(new ServiceAddress { Name = items[0].Trim(), Port = port });
			}
			if (addresses.Count == 0)
				throw new Exception("Must specify at least one service address.");
			return addresses.ToArray();
		}

        //Free all fastore memory.
        protected override void InternalStop(ServerProcess process)
        {
            //TODO: Kill db, Free resources, etc.

            base.InternalStop(process);
        }

        //TODO: Add support as we can.
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
                    CursorCapability.Searchable |
                    CursorCapability.Updateable;

                node.CursorIsolation = plan.Plan.CursorContext.CursorIsolation;
                node.Order = Compiler.OrderFromKey(plan.Plan, Compiler.FindClusteringKey(plan.Plan, node.TableVar));
                return new DevicePlanNode(node);
            }
            else if ((planNode is OrderNode) && (planNode.Nodes[0] is BaseTableVarNode) && (plan.Plan.CursorContext.CursorType != CursorType.Static))
            {
                OrderNode node = (OrderNode)planNode;
                BaseTableVarNode tableVarNode = (BaseTableVarNode)planNode.Nodes[0];

                bool isSupported = false;

                foreach (Schema.Key key in tableVarNode.TableVar.Keys)
                {
                    var tableOrder = Compiler.OrderFromKey(plan.Plan, key);
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
                {                  
                    foreach (Schema.Order order in tableVarNode.TableVar.Orders)
                    {
                        //We support one column (or one column plus a single-column key ordered in the same direction).
                        if (order.Columns.Count > 1)
                            break;

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

                        var rowIdKey = tableVarNode.TableVar.Keys.MinimumSubsetKey(tableVarNode.TableVar.Columns);
                        var tableOrder = Compiler.OrderFromKey(plan.Plan, rowIdKey);
                        //If we have a rowId key... Add it to the ordering and see if we match
                        if (rowIdKey.Columns.Count == 1 && tableOrder.Columns.Count == 1)
                        {
                            Order newOrder = new Order(order);
                            newOrder.Columns.Add(tableOrder.Columns[0]);

                            if (node.RequestedOrder.Equivalent(newOrder))
                            {
                                node.PhysicalOrder = newOrder;
                                node.ScanDirection = ScanDirection.Forward;
                                isSupported = true;
                                break;
                            }
                            else if (node.RequestedOrder.Equivalent(new Schema.Order(newOrder, true)))
                            {
                                node.PhysicalOrder = newOrder;
                                node.ScanDirection = ScanDirection.Backward;
                                isSupported = true;
                                break;
                            }
                        }
                    }
                }

                if (!isSupported)
                {
                    //Support every ordering... Use nestedFilterCursor to emulate support...
                    node.PhysicalOrder = node.RequestedOrder;
                    node.ScanDirection = ScanDirection.Forward;
                    isSupported = true;
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
                //Don't support altering tables in V1

                //plan.Plan.CheckRight(GetRight(Schema.RightNames.AlterStore));
                //AlterTableNode alterTableNode = (AlterTableNode)planNode;
                //if (alterTableNode.AlterTableStatement.CreateColumns.Count > 0)
                //    throw new RuntimeException(RuntimeException.Codes.UnimplementedCreateCommand, "Columns in a memory device");
                //if (alterTableNode.AlterTableStatement.DropColumns.Count > 0)
                //    throw new RuntimeException(RuntimeException.Codes.UnimplementedDropCommand, "Columns in a memory device");
                //return new DevicePlanNode(planNode);
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
