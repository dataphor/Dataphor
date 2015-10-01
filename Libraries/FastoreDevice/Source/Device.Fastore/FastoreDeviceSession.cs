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
	//TODO: Transactions
	public class FastoreDeviceSession : Schema.DeviceSession
	{
		protected internal FastoreDeviceSession
		(
			Schema.Device device,
			ServerProcess serverProcess,
			Schema.DeviceSessionInfo deviceSessionInfo,
			Database db
		)
			: base(device, serverProcess, deviceSessionInfo)
		{
			_db = db;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		private Database _db;

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
				FastoreCursor scan = new FastoreCursor(program, _db, (BaseTableVarNode)planNode);
				try
				{
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
               
                OrderNode orderNode = (OrderNode)planNode;
                if (orderNode.Order.Columns.Count == 1)
                {
                    FastoreCursor scan = new FastoreCursor(program, _db, (BaseTableVarNode)planNode.Nodes[0]);
                    try
                    {
                        scan.Key = orderNode.PhysicalOrder;
                        scan.Direction = orderNode.ScanDirection;
                        scan.Node.Order = orderNode.Order;
                        scan.Open();
                        return scan;
                    }
                    catch
                    {
                        scan.Dispose();
                        throw;
                    }
                }
                else
                {
                    FastoreStackedCursor scan = new FastoreStackedCursor(program, _db, orderNode.Order, orderNode.PhysicalOrder, (BaseTableVarNode)planNode.Nodes[0]);
                    try
                    {
                        scan.Open();
                        return scan;
                    }
                    catch
                    {
                        scan.Dispose();
                        throw;
                    }
                }
			}
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
		protected override void InternalInsertRow(Program program, Schema.TableVar table, IRow row, BitArray valueFlags)
		{
			GetTables(table.Scope)[table].Insert(ServerProcess.ValueManager, row, _db);
		}

		protected override void InternalUpdateRow(Program program, Schema.TableVar table, IRow oldRow, IRow newRow, BitArray valueFlags)
		{
			GetTables(table.Scope)[table].Update(ServerProcess.ValueManager, oldRow, newRow, _db);
		}

		protected override void InternalDeleteRow(Program program, Schema.TableVar table, IRow row)
		{
			GetTables(table.Scope)[table].Delete(ServerProcess.ValueManager, row, _db);
		}
	}
}
