/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.Threading;
	using System.Collections;
	using System.Diagnostics;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public abstract class SortTableBase : Table
    {
		public SortTableBase(BaseOrderNode node, Program program) : base(node, program) {}
		
		public new BaseOrderNode Node { get { return (BaseOrderNode)_node; } }
		
        protected NativeTable _table;
        protected Scan _scan;
        protected int _rowCount;
        
        protected abstract void PopulateTable();

		protected override void InternalOpen()
		{
			// TODO: Rewrite this...
			Schema.TableType tableType = new Schema.TableType();
			Schema.BaseTableVar tableVar = new Schema.BaseTableVar(tableType);
			Schema.TableVarColumn newColumn;
			foreach (Schema.TableVarColumn column in Node.TableVar.Columns)
			{
				newColumn = (Schema.TableVarColumn)column.Copy();
				tableType.Columns.Add(newColumn.Column);
				tableVar.Columns.Add(newColumn);
			}
			
			Schema.Order order = new Schema.Order();
			Schema.OrderColumn newOrderColumn;
			Schema.OrderColumn orderColumn;
			for (int index = 0; index < Node.Order.Columns.Count; index++)
			{
				orderColumn = Node.Order.Columns[index];
				newOrderColumn = new Schema.OrderColumn(tableVar.Columns[orderColumn.Column], orderColumn.Ascending, orderColumn.IncludeNils);
				newOrderColumn.Sort = orderColumn.Sort;
				newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
				order.Columns.Add(newOrderColumn);
			}
			tableVar.Orders.Add(order);

			_table = new NativeTable(Manager, tableVar);
			PopulateTable();
			_scan = new Scan(Manager, _table, _table.ClusteredIndex, ScanDirection.Forward, null, null);
			_scan.Open();
		}

		protected override void InternalClose()
		{
			if (_scan != null)
				_scan.Dispose();
			_scan = null;
			if (_table != null)
			{
				_table.Drop(Manager);
				_table = null;
			}
		}
		
		protected override void InternalReset()
		{
			_scan.Dispose();
			_table.Truncate(Manager);
			PopulateTable();
			_scan = new Scan(Manager, _table, _table.ClusteredIndex, ScanDirection.Forward, null, null);
			_scan.Open();
		}
		
		protected override void InternalSelect(IRow row)
		{
			_scan.GetRow(row);
		}
		
		protected override bool InternalNext()
		{
			return _scan.Next();
		}
		
		protected override void InternalLast()
		{
			_scan.Last();
		}

		protected override bool InternalBOF()
		{
			return _scan.BOF();
		}
		
		protected override bool InternalEOF()
		{
			return _scan.EOF();
		}
		
		protected override void InternalFirst()
		{
			_scan.First();
		}
		
		protected override bool InternalPrior()
		{
			return _scan.Prior();
		}

        protected override IRow InternalGetBookmark()
        {
			return _scan.GetKey();
        }

		protected override bool InternalGotoBookmark(IRow bookmark, bool forward)
        {
			return _scan.FindKey(bookmark);
        }
        
        protected override int InternalCompareBookmarks(IRow bookmark1, IRow bookmark2)
        {
			return _scan.CompareKeys(bookmark1, bookmark2);
        }

        protected override IRow InternalGetKey()
        {
			return _scan.GetKey();
        }
        
        protected override bool InternalFindKey(IRow row, bool forward)
        {
			return _scan.FindKey(row);
        }
        
        protected override void InternalFindNearest(IRow row)
        {
			_scan.FindNearest(row);
        }
        
        protected override bool InternalRefresh(IRow row)
        {					
			if (row == null)
				row = Select();	 
			InternalReset();
			return _scan.FindNearest(row);
        }

        // ICountable
        protected override int InternalRowCount()
        {
			return _rowCount;
        }
    }
    
    public class CopyTable : SortTableBase
    {
		public CopyTable(CopyNode node, Program program) : base(node, program){}
		
		public new CopyNode Node { get { return (CopyNode)_node; } }

		protected override void PopulateTable()
		{
			using (ITable table = (ITable)Node.Nodes[0].Execute(Program))
			{
				Row row = new Row(Manager, DataType.RowType);
				try
				{
					_rowCount = 0;
					while (table.Next())
					{
						table.Select(row);
						_rowCount++;
						_table.Insert(Manager, row); // no validation is required because FTable will never be changed
						row.ClearValues();
						Program.CheckAborted(); // Yield
					}
				}
				finally
				{
					row.Dispose();
				}
			}
		}
    }
    
    public class OrderTable : SortTableBase
    {
		public OrderTable(OrderNode node, Program program) : base(node, program){}
		
		public new OrderNode Node { get { return (OrderNode)_node; } }

		protected override void PopulateTable()
		{
			using (ITable table = (ITable)Node.Nodes[0].Execute(Program))
			{
				Row row = new Row(Manager, DataType.RowType);
				try
				{
					_rowCount = 0;
					while (table.Next())
					{
						table.Select(row);
						_rowCount++;
						if (Node.SequenceColumnIndex >= 0)
							row[Node.SequenceColumnIndex] = _rowCount;
						_table.Insert(Manager, row); // no validation is required because FTable will never be changed
						row.ClearValues();
						Program.CheckAborted();	// Yield
					}
				}
				finally
				{
					row.Dispose();
				}
			}
		}
    }
    
    public class BrowseTableItem : Disposable
    {
        // constructor
        protected internal BrowseTableItem(BrowseTable browseTable, ITable table, object contextVar, IRow origin, bool forward, bool inclusive)
        {
			_browseTable = browseTable;
            _table = table;
			_contextVar = contextVar;
            _row = new Row(_browseTable.Manager, _table.DataType.RowType);
            _origin = origin;
            _forward = forward;
            _inclusive = inclusive;
        }
        
        // Dispose
        protected override void Dispose(bool disposing)
        {
            if (_table != null)
            {
                _table.Dispose();
                _table = null;
            }
            
            if (_origin != null)
            {
				_origin.Dispose();
                _origin = null;
            }
            
            if (_orderKey != null)
            {
				_orderKey.Dispose();
                _orderKey = null;
            }
            
            if (_uniqueKey != null)
            {
				_uniqueKey.Dispose();
                _uniqueKey = null;
            }
            
            if (_row != null)
            {
				_row.Dispose();
                _row = null;
            }

            base.Dispose(disposing);
        }

        // Table
        protected ITable _table;
        public ITable Table { get { return _table; } }
        
        // ContextVar
        protected object _contextVar;
        public object ContextVar { get { return _contextVar; } }

        // Origin
        protected IRow _origin;
        public IRow Origin { get { return _origin; } }
        
        // OrderKey
        protected IRow _orderKey;
        public IRow OrderKey
        {
            get
            {
                FindOrderKey();
                return _orderKey;
            }
        }
        
        protected void FindOrderKey()
        {
            if (!(_table.BOF() || _table.EOF()))
            {
                if (_orderKey == null)
                    _orderKey = BuildOrderKeyRow();
                else
                    _orderKey.ClearValues();
                    
                _table.Select(_orderKey);

            }
            else
                if (_orderKey != null)
                {
					_orderKey.Dispose();
                    _orderKey = null;
                }
        }
        
        // UniqueKey
        protected IRow _uniqueKey;
        public IRow UniqueKey
        {
            get
            {
                FindUniqueKey();
                return _uniqueKey;
            }
        }
        
        protected void FindUniqueKey()
        {
            if (!(_table.BOF() || _table.EOF()))
            {
                if (_uniqueKey == null)
                    _uniqueKey = BuildUniqueKeyRow();
                else
                    _uniqueKey.ClearValues();

                _table.Select(_uniqueKey);

            }
            else
                if (_uniqueKey != null)
                {
					_uniqueKey.Dispose();
                    _uniqueKey = null;
                }
        }
        
        protected IRow BuildOrderKeyRow()
        {
			return new Row(_browseTable.Manager, new Schema.RowType(_browseTable.Order.Columns));
        }
        
        protected IRow BuildUniqueKeyRow()
        {
			Schema.RowType rowType = new Schema.RowType(_browseTable.Order.Columns);
			return new Row(_browseTable.Manager, rowType);
        }
        
        // Forward
        protected bool _forward;
        public bool Forward { get { return _forward; } }
        
        // Inclusive
        protected bool _inclusive;
        public bool Inclusive { get { return _inclusive; } }
        
        // OnOrigin
        public bool OnOrigin
        {
            get
            {
                FindOrderKey();
                if ((_origin == null) || (_orderKey == null))
					return false;
				else
	                return _browseTable.CompareKeys(_origin, _orderKey) == 0;
            }
        }
        
        // OnCrack
        public bool OnCrack { get { return (_table.BOF() && !_table.EOF()) || (_table.EOF() && !_table.BOF()); } }
        
        public void MoveCrack()
        {
            if (OnCrack)
                if (_table.BOF())
                    _table.Next();
                else
                    if (_table.EOF())
                        _table.Prior();
        }
        
        protected BrowseTable _browseTable;
        public BrowseTable BrowseTable { get { return _browseTable; } }
        
        protected IRow _row;
    }
    
    public class BrowseTableList : DisposableList<BrowseTableItem>
    {
        public const int MinTables = 1;

		public BrowseTableList(BrowseTable browseTable) : base(true)
		{
			_browseTable = browseTable;
		}
		
		// BrowseTable
        protected BrowseTable _browseTable;
        public BrowseTable BrowseTable { get { return _browseTable; } }
        
        // MaxTables
        protected int _maxTables = 2;
        public int MaxTables
        {
            get { return _maxTables; }
            set
            {
                if (value <= Count)
                    throw new RuntimeException(RuntimeException.Codes.CurrentListSizeExceedsNewSetting);
				if (value < MinTables)
					throw new RuntimeException(RuntimeException.Codes.NewValueViolatesMinimumTableCount);
                _maxTables = value;
            }
        }
        
        public new void Add(BrowseTableItem table)
        {
            int index = IndexOf(table);
            if (index < 0)
            {
                if (Count >= _maxTables)
					RemoveAt(Count - 1);
                Insert(0, table);
            }
            else
            {
                if (index > 0)
					Move(index, 0);
            }
        }
        
        public new BrowseTableItem this[int index] { get { return base[index]; } }
        
        public BrowseTableItem this[Table index]
        {
            get
            {
                for (int localIndex = 0; localIndex < Count; localIndex++)
                {
                    if (base[localIndex].Table == index)
                        return this[localIndex];
                }
                return null;
            }
        }
    }
    
    public class BrowseTable : Table
    {
		public BrowseTable(BrowseNode node, Program program) : base(node, program)
        {
            _tables = new BrowseTableList(this);
        }
        
        protected override void Dispose(bool disposing)
        {
			try
			{
	            base.Dispose(disposing);
	        }
	        finally
	        {
	            _tables.Dispose();
	        }
        }
        
        protected BrowseTableList _tables;
        
        public new BrowseNode Node { get { return (BrowseNode)_node; } }

		/*
            for each column in the order descending
                if the current order column is also in the origin
                    [or]
                    for each column in the origin less than the current order column
                        [and] current origin column = current origin value
                        
                    [and]
                    if the current order column is ascending xor the requested set is forward
                        if the current order column includes nulls
                            current order column is null or
                        if requested set is inclusive and current order column is the last origin column
                            current order column <= current origin value
                        else
                            current order column < current origin value
                    else
                        if requested set is inclusive and the current order column is the last origin column
                            current order column >= current origin value
                        else
                            current order column > current origin value
                            
                    for each column in the order greater than the current order column
                        if the current order column does not include nulls
                            and current order column is not null
                else
                    if the current order column does not include nulls
                        [and] current order column is not null
        */
        
        protected void EnterTableContext(BrowseTableItem tableItem)
        {
			Program.Stack.Push(tableItem.ContextVar);
        }
        
        protected void ExitTableContext(BrowseTableItem tableItem)
        {
			Program.Stack.Pop();
        }
        
        // Must be called with the original stack
        protected BrowseTableItem CreateTable(IRow origin, bool forward, bool inclusive)
        {
			// Prepare the context variable to contain the origin value (0 if this is an unanchored set)
			object contextVar;
			Row localOrigin;
			if (origin == null)
			{
				localOrigin = null;
				contextVar = 0;
			}
			else
			{
				if ((origin.DataType.Columns.Count > 0) && (Schema.Object.Qualifier(origin.DataType.Columns[0].Name) != Keywords.Origin))
					localOrigin = new Row(Manager, new Schema.RowType(origin.DataType.Columns, Keywords.Origin));
				else
					localOrigin = new Row(Manager, new Schema.RowType(origin.DataType.Columns));
				origin.CopyTo(localOrigin);
				contextVar = localOrigin;
			}
				
			int originIndex = ((localOrigin == null) ? -1 : localOrigin.DataType.Columns.Count - 1);
			bool localInclusive = (localOrigin == null) ? true : inclusive;

			// Ensure the browse node has the appropriate browse variant				
			lock (Node)
			{
				if (!Node.HasBrowseVariant(originIndex, forward, localInclusive))
				{
					Program.ServerProcess.PushGlobalContext();
					try
					{
						Node.CompileBrowseVariant(Program, originIndex, forward, localInclusive);
					}
					finally
					{
						Program.ServerProcess.PopGlobalContext();
					}
				}
			}
		
			// Execute the variant with the current context variable
			Program.Stack.Push(contextVar);
			try
			{
				PlanNode browseVariantNode = Node.GetBrowseVariantNode(Program.Plan, originIndex, forward, localInclusive);
				#if TRACEBROWSEEVENTS
				Trace.WriteLine(String.Format("BrowseTableItem created with query: {0}", new D4TextEmitter().Emit(browseVariantNode.EmitStatement(EmitMode.ForCopy))));
				#endif
				return 
					new BrowseTableItem
					(
						this, 
						(ITable)browseVariantNode.Execute(Program),
						contextVar,
						localOrigin, 
						forward, 
						inclusive
					);
			}
			finally
			{
				Program.Stack.Pop();
			}
        }
        
        // Must be called with the original stack
        protected BrowseTableItem FindTable(IRow origin, bool forward, bool inclusive)
        {
            foreach (BrowseTableItem table in _tables)
            {
				EnterTableContext(table);
				try
				{
					if 
						(
							(
								(origin != null) &&
								(table.OrderKey != null) &&
								(table.Forward == forward) &&
								(table.Inclusive == inclusive) &&
								(origin.DataType.Columns.Count == table.OrderKey.DataType.Columns.Count) &&
								(CompareKeys(origin, table.OrderKey) == 0)
							) ||
							(
								(origin == null) &&
								(table.Origin == null) &&
								(table.Forward == forward) &&
								(table.Table.BOF())
							)
						)
					{
						if (!inclusive)
							table.Table.Next();
						return table;
					}
				}
				finally
				{
					ExitTableContext(table);
				}
            }
            return null;
        }

		// Must be called with the original stack        
        protected BrowseTableItem GetTable(IRow origin, bool forward, bool inclusive)
        {
            BrowseTableItem result = FindTable(origin, forward, inclusive);
            if (result == null)
				result = CreateTable(origin, forward, inclusive);
            return result;
        }

		// Must be called with the original stack        
        protected BrowseTableItem GetTable()
        {
            return GetTable(null, true, true);
        }
        
        // TopTable
        public BrowseTableItem TopTable
        {
            get
            {
                if (_tables.Count == 0)
                    throw new RuntimeException(RuntimeException.Codes.NoTopTable);
                return _tables[0];
            }
        }
        
        protected override void InternalOpen()
        {
            _tables.Add(GetTable());
        }
        
        protected override void InternalClose()
        {
            _tables.Clear();
        }
        
        protected override void InternalReset()
        {
            _tables.Clear();
	        _tables.Add(GetTable());
        }
        
        protected override bool InternalRefresh(IRow row)
        {					
			if (row == null)
				row = Select();

			_tables.Clear();
			try
			{
				bool result = InternalFindKey(row, true);
				if (!result)
					InternalFindNearest(row);
				return result;
			}
			catch
			{
				if (_tables.Count == 0)
					_tables.Add(GetTable());
				throw;
			}
        }

        protected override void InternalSelect(IRow row)
        {
			EnterTableContext(TopTable);
			try
			{
				TopTable.Table.Select(row);
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }

		// Must be called with the original stack        
        protected void SwapReader(IRow origin, bool forward, bool inclusive)
        {
			BrowseTableItem item = GetTable(origin, forward, inclusive);
			_tables.Add(item);
			EnterTableContext(TopTable);
			try
			{
				TopTable.MoveCrack();
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }
        
        // Must be called with the original stack
        protected void SwapReader(bool forward)
        {
			IRow origin = null;
			try
			{
				EnterTableContext(TopTable);
				try
				{
					origin = TopTable.Origin != null ? (IRow)TopTable.Origin.Copy() : null;
				}
				finally
				{
					ExitTableContext(TopTable);
				}
				SwapReader(origin, forward, !TopTable.Inclusive);
			}
			finally
			{
				if (origin != null)
					origin.Dispose();
			}
        }
        
        protected void Move(bool forward)
        {
            if (TopTable.Forward == forward)
            {
				EnterTableContext(TopTable);
				try
				{
	                TopTable.Table.Next();
	            }
	            finally
	            {
					ExitTableContext(TopTable);
				}
			}
            else
            {
				bool inTableContext = true;
				EnterTableContext(TopTable);
				try
				{
					if (TopTable.Table.BOF() && TopTable.Origin != null)
					{
						ExitTableContext(TopTable);
						inTableContext = false;
						SwapReader(forward);
					}
					else
					{
						if (TopTable.Table.Supports(CursorCapability.BackwardsNavigable))
						{
							TopTable.Table.Prior();
							if (TopTable.Table.BOF() && (TopTable.Origin != null))
							{
								ExitTableContext(TopTable);
								inTableContext = false;
								SwapReader(forward);
							}
						}
						else
						{
							IRow origin = TopTable.OrderKey != null ? (IRow)TopTable.OrderKey.Copy() : TopTable.Origin != null ? (IRow)TopTable.Origin.Copy() : null;
							try
							{
								ExitTableContext(TopTable);
								inTableContext = false;
								SwapReader(origin, forward, false);
							}
							finally
							{
								if (origin != null)
									origin.Dispose();
							}
						}
					}
				}
				finally
				{
					if (inTableContext)
						ExitTableContext(TopTable);
				}
            }
        }
        
        protected override bool InternalNext()
        {
            Move(true);
            return !InternalEOF();
        }
        
        protected override void InternalLast()
        {
			BrowseTableItem item = GetTable(null, false, true);
            _tables.Add(item);
        }
        
        protected override bool InternalBOF()
        {
			EnterTableContext(TopTable);
			try
			{
				return (TopTable.Forward && (TopTable.Origin == null) && TopTable.Table.BOF()) || (!TopTable.Forward && TopTable.Table.EOF());
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }
        
        protected override bool InternalEOF()
        {
			EnterTableContext(TopTable);
			try
			{
				return (!TopTable.Forward && (TopTable.Origin == null) && TopTable.Table.BOF()) || (TopTable.Forward && TopTable.Table.EOF());
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }
        
        protected override bool InternalPrior()
        {
            Move(false);
            return !InternalBOF();
        }
        
        protected override void InternalFirst()
        {
			BrowseTableItem item = GetTable(null, true, true);
            _tables.Add(item);
        }
        
        protected override IRow InternalGetBookmark()
        {
			return InternalGetKey();
        }

		protected override bool InternalGotoBookmark(IRow bookmark, bool forward)
        {
            return InternalFindKey(bookmark, forward);
        }
        
        protected override int InternalCompareBookmarks(IRow bookmark1, IRow bookmark2)
        {
			return CompareKeys(bookmark1, bookmark2);
        }
        
		public int CompareKeys(IRow indexKey, IRow compareKey)
        {
			int result = 0;
			for (int index = 0; index < indexKey.DataType.Columns.Count; index++)
			{
				if (index >= compareKey.DataType.Columns.Count)
					break;

				if (indexKey.HasValue(index) && compareKey.HasValue(index))
				{
					Program.Stack.Push(indexKey[index]);
					try
					{
						Program.Stack.Push(compareKey[index]);
						try
						{
							result = (int)Node.Order.Columns[index].Sort.CompareNode.Execute(Program);

							// Swap polarity for descending columns
							if (!Node.Order.Columns[index].Ascending)
								result = -result;
						}
						finally
						{
							Program.Stack.Pop();
						}
					}
					finally
					{
						Program.Stack.Pop();
					}
				}
				else if (indexKey.HasValue(index))
				{
					// Index Key Has A Value
					result = Node.Order.Columns[index].Ascending ? 1 : -1;
				}
				else if (compareKey.HasValue(index))
				{
					// Compare Key Has A Value
					result = Node.Order.Columns[index].Ascending ? -1 : 1;
				}
				else
				{
					// Neither key has a value
					result = 0;
				}
				
				if (result != 0)
					break;
			}

			return result;
        }
        
        protected override IRow InternalGetKey()
        {
			EnterTableContext(TopTable);
			try
			{
				return (IRow)TopTable.OrderKey.Copy();
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }

		protected override bool InternalFindKey(IRow row, bool forward)
        {
			IRow localRow = EnsureKeyRow(row);
			try
			{
				bool tableCreated = false;
				BrowseTableItem table = FindTable(localRow, forward, true);
				if (table == null)
				{
					table = CreateTable(localRow, forward, true);
					tableCreated = true;
				}
				try
				{
					EnterTableContext(table);
					try
					{
						table.MoveCrack();
						bool result = (table.UniqueKey != null) && (CompareKeys(localRow, table.UniqueKey) == 0);
						if (result)
							_tables.Add(table);
						else
							if (tableCreated)
								table.Dispose();
						return result;
					}
					finally
					{
						ExitTableContext(table);
					}
				}
				catch
				{
					if (tableCreated)
						table.Dispose();
					throw;
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(row, localRow))
					localRow.Dispose();
			}
        }
        
        protected override void InternalFindNearest(IRow row)
        {
			IRow localRow = EnsurePartialKeyRow(row);
			try
			{
				if (localRow != null)
				{
					BrowseTableItem item = GetTable(localRow, true, true);
		            _tables.Add(item);
		            EnterTableContext(item);
		            try
		            {
				        TopTable.MoveCrack();
				    }
				    finally
				    {
						ExitTableContext(item);
				    }
			    }
			    else
					_tables.Add(GetTable());
		    }
		    finally
		    {
				if ((localRow != null) && !Object.ReferenceEquals(row, localRow))
					localRow.Dispose();
		    }
        }
        
        protected override int InternalRowCount()
        {
            throw new RuntimeException(RuntimeException.Codes.UnimplementedInternalRowCount);
        }
	}
}