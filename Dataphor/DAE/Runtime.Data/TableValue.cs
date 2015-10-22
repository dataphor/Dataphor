/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using System.Collections.Generic;
	using System.Text;

	public class TableValue : DataValue
	{
		public TableValue(IValueManager manager, Schema.ITableType tableType, NativeTable table) : base(manager, tableType)
		{	
			_table = table;
		}
		
		private NativeTable _table;

        public new Schema.ITableType DataType { get { return (Schema.ITableType)base.DataType; } }
		
		public override bool IsNil { get { return _table == null; } }
		
		public override object AsNative
		{
			get { return _table; }
			set 
			{
				if (_table != null)
					_table.Drop(Manager);
				_table = (NativeTable)value; 
			} 
		}
		
		/*
			Physical representation format ->
			
				00 -> Value indicator 0 - nil, 1 - non-nil
				01-05 -> Number of rows
				06-XX -> N row values written using Row physical representation
		*/
		private List<IRow> _rowList;
		private List<int> _sizeList;
		
		public override int GetPhysicalSize(bool expandStreams)
		{
			int size = 1;
			
			if (!IsNil)
			{
				size += sizeof(int);
				_rowList = new List<IRow>();
				_sizeList = new List<int>();
				
				ITable table = OpenCursor();
				try
				{
					while (table.Next())
					{
						IRow row = table.Select();
						int rowSize = row.GetPhysicalSize(expandStreams);
						size += rowSize;
						_rowList.Add(row);
						_sizeList.Add(rowSize);
					}
				}
				finally
				{
					table.Dispose();
				}
				
				size += sizeof(int) * _rowList.Count;
			}
			
			return size;
		}

		public override void WriteToPhysical(byte[] buffer, int offset, bool expandStreams)
		{
			if (IsNil)
				buffer[offset] = 0;
			else
			{
				buffer[offset] = 1;
				offset++;
				
				int rowSize;
				Streams.IConveyor int32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);
				
				int32Conveyor.Write(_rowList.Count, buffer, offset);
				offset += sizeof(int);
				
				for (int index = 0; index < _rowList.Count; index++)
				{
					rowSize = (int)_sizeList[index];
					int32Conveyor.Write(rowSize, buffer, offset);
					offset += sizeof(int);
					IRow row = _rowList[index];
					row.WriteToPhysical(buffer, offset, expandStreams);
					offset += rowSize;
					row.ValuesOwned = false;
					row.Dispose();
				}
			}
		}
		
		public override void ReadFromPhysical(byte[] buffer, int offset)
		{
			_table.Truncate(Manager);
			
			Streams.IConveyor int32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);
			
			if (buffer[offset] != 0)
			{
				offset++;
				
				int rowSize;
				int count = (int)int32Conveyor.Read(buffer, offset);
				
				for (int index = 0; index < count; index++)
				{
					rowSize = (int)int32Conveyor.Read(buffer, offset);
					offset += sizeof(int);
					using (IRow row = (IRow)DataValue.FromPhysical(Manager, _table.RowType, buffer, offset))
					{
						_table.Insert(Manager, row);
					}
					offset += rowSize;
				}
			}
		}

		public override ITable OpenCursor()
		{
			Table table = new TableScan(Manager, _table, Manager.FindClusteringOrder(_table.TableVar), ScanDirection.Forward, null, null);
			table.Open();
			return table;
		}
		
		public override object CopyNativeAs(Schema.IDataType dataType)
		{
			NativeTable newTable = new NativeTable(Manager, _table.TableVar);
			using (Scan scan = new Scan(Manager, _table, _table.ClusteredIndex, ScanDirection.Forward, null, null))
			{
				scan.Open();
				while (scan.Next())
				{
					using (IRow row = scan.GetRow())
					{
						newTable.Insert(Manager, row);
					}
				}
			}
			return newTable;
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("table { ");

			using (Scan scan = new Scan(Manager, _table, _table.ClusteredIndex, ScanDirection.Forward, null, null))
			{
				scan.Open();
				int rowCount = 0;
				while (scan.Next())
				{
					if (rowCount >= 1)
						result.Append(", ");

					if (rowCount > 10)
					{
						result.Append("...");
						break;
					}

					using (IRow row = scan.GetRow())
					{
						result.Append(row.ToString());
					}

					rowCount++;
				}

				if (rowCount > 0)
					result.Append(" ");
			}

			result.Append("}");
			return result.ToString();
		}
	}
	
    /// <remarks> Table </remarks>
    public abstract class Table : DataValue, ITable
    {        
		protected Table(IValueManager manager, Schema.ITableType tableType) : base(manager, tableType) { }
		
		protected Table(IValueManager manager, TableNode node) : base(manager, node.DataType)
		{
			_node = node;
		}
		
		protected Table(TableNode node, Program program) : base(program.ValueManager, node.DataType)
		{
			_node = node;
			_program = program;
		}
		
		protected override void Dispose(bool disposing)
		{
			Close();
			base.Dispose(disposing);
			_node = null;
		}
		
		protected Program _program;
		public Program Program { get { return _program; } }
        
        // DataType
        public new Schema.ITableType DataType { get { return (Schema.ITableType)base.DataType; } }
        
		// Node
		protected TableNode _node;
		public TableNode Node
		{
			get
			{
				#if DEBUG
				if (_node == null)
					throw new RuntimeException(RuntimeException.Codes.InternalError, "FNode is null.");
				#endif
				return _node;
			}
		}

		// CursorType
		public virtual CursorType CursorType { get { return Node.RequestedCursorType; } }
		
        // Capabilities
        public virtual CursorCapability Capabilities { get { return Node.CursorCapabilities; }  }
		
		// Isolation
		public virtual CursorIsolation Isolation { get { return Node.CursorIsolation; } }
		
        public bool Supports(CursorCapability capability)
        {
			return ((capability & Capabilities) != 0);
        }
        
        public void CheckCapability(CursorCapability capability)
        {
			if (!Supports(capability))
				throw new RuntimeException(RuntimeException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), capability));
        }
        
        protected void CheckAborted()
        {
            if (_program != null)
				_program.CheckAborted();
        }
        
        // Open        
        protected abstract void InternalOpen();
        public void Open()
        {
            if (!_active)
            {
				#if USETABLEEVENTS
                DoBeforeOpen();
                #endif
                InternalOpen();
                _active = true;
                #if USETABLEEVENTS
                DoAfterOpen();
                #endif
            }
        }
        
        // Close
        protected abstract void InternalClose();
        public void Close()
        {
            if (_active)
            {
				#if USETABLEEVENTS
                DoBeforeClose();
                #endif
                InternalClose();
                _active = false;
                #if USETABLEEVENTS
                DoAfterClose();
                #endif
            }
        }
        
        // Active
        protected bool _active;
        public bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                if (value)
                    Open();
                else
                    Close();
            }
        }
        
        protected void CheckActive()
        {
            if (!Active)
                throw new RuntimeException(RuntimeException.Codes.TableInactive);
        }
        
        protected void CheckInactive()
        {
            if (Active)
                throw new RuntimeException(RuntimeException.Codes.TableActive);
        }

        // Reset        
        protected virtual void InternalReset()
        {
            Close();
            Open();
        }

        public virtual void Reset()
        {
			#if SAFETABLES
            CheckActive();
            #endif
            InternalReset();
        }
      
        // Select
        protected abstract void InternalSelect(IRow row);
        public void Select(IRow row)
        {
			#if SAFETABLES
            CheckActive();
            #endif
            CheckNotOnCrack();
            InternalSelect(row);
        }

        public IRow Select()
        {
			IRow row = new Row(Manager, DataType.RowType);
			try
			{
				Select(row);
				return row;
			}
			catch
			{
				row.Dispose();
				throw;
			}
        }

        // Next
        protected abstract bool InternalNext();
        public bool Next()
        {
			#if SAFETABLES
            CheckActive();
            #endif
            CheckAborted();
			return InternalNext();
        }
        
        // Last
        protected virtual void InternalLast()
        {
            while (!EOF())
                Next();
        }
        
        public void Last()
        {
			#if SAFETABLES
            CheckActive();
            #endif
            InternalLast();
        }
        
        // BOF
        protected abstract bool InternalBOF();
        public bool BOF()
        {
			#if SAFETABLES
            CheckActive();
            #endif
            return InternalBOF();
        }
        
        // EOF
        protected abstract bool InternalEOF();
        public bool EOF()
        {
			#if SAFETABLES
            CheckActive();
            #endif
            return InternalEOF();
        }

        public virtual bool IsEmpty()
        {
            return BOF() && EOF();
        }
        
        protected void CheckNotOnCrack()
        {
            if (BOF() || EOF())
                throw new RuntimeException(RuntimeException.Codes.NoCurrentRow);
        }

        // BackwardsNavigable
		protected virtual bool InternalPrior()
		{
			throw new RuntimeException(RuntimeException.Codes.NotBackwardsNavigable);
		}
		
        public bool Prior()
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.BackwardsNavigable);
            #endif
            CheckAborted();
			return InternalPrior();
        }
        
        protected virtual void InternalFirst()
        {
            throw new RuntimeException(RuntimeException.Codes.NotBackwardsNavigable);
        }
        
        public void First()
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.BackwardsNavigable);
            #endif
            InternalFirst();
        }
        
        // Bookmarkable

		protected virtual IRow InternalGetBookmark()
        {
            throw new RuntimeException(RuntimeException.Codes.NotBookmarkable);
        }
        
        public IRow GetBookmark()
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Bookmarkable);
            #endif
            CheckNotOnCrack();
			return InternalGetBookmark();
        }

		protected virtual bool InternalGotoBookmark(IRow bookmark, bool forward)
        {
            throw new RuntimeException(RuntimeException.Codes.NotBookmarkable);
        }

		public bool GotoBookmark(IRow bookmark, bool forward)
		{
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Bookmarkable);
			#endif
			return InternalGotoBookmark(bookmark, forward);
		}

		public bool GotoBookmark(IRow bookmark)
        {
			return GotoBookmark(bookmark, true);
        }
        
        protected virtual int InternalCompareBookmarks(IRow bokmark1, IRow bookmark2)
        {
            throw new RuntimeException(RuntimeException.Codes.NotBookmarkable);
        }
        
        public int CompareBookmarks(IRow bookmark1, IRow bookmark2)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Bookmarkable);
            #endif
            return InternalCompareBookmarks(bookmark1, bookmark2);
        }
		
		// Searchable
        protected virtual Schema.Order InternalGetOrder()
        {
            return Node.Order;
        }
        
        public Schema.Order Order
        { 
            get
            {
				#if SAFETABLES
                CheckActive();
                CheckCapability(CursorCapability.Searchable);
                #endif
                return InternalGetOrder();
            }
        }
        
        protected virtual IRow InternalGetKey()
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }
        
        public IRow GetKey()
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
            #endif
            CheckNotOnCrack();
            return InternalGetKey();
        }
        
        protected virtual bool InternalFindKey(IRow row, bool forward)
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }

		/// <summary>
		///	Attempts to position the cursor on the key specified by the given row.  
		///	The row must be a superset of the current order key of the table.
		/// Returns true if successful, false otherwise.
		/// </summary>        
        public bool FindKey(IRow row)
        {
			return FindKey(row, true);
        }

		/// <param name="forward"> Provides a hint about the intended direction for bi-directionally navigable cursors. </param>
		public bool FindKey(IRow row, bool forward)
		{
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
			#endif
			return InternalFindKey(row, forward);
		}
        
        protected virtual void InternalFindNearest(IRow row)
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }
        
        /// <summary>
        /// Attempts to position the cursor on the key most closely matching the
        /// key specified by the given row.  If the given row is not already a
        /// key or partial key of the current order of the table, a partial
        /// key will be constructed from the row containing the same or fewer
        /// columns of the current key of the order.  If any column in the key
        /// has no value, the rest of the columns in the key must also have no
        /// value.  If a row cannot be constructed meeting this criteria, the
        /// FindNearest will fail.
        /// </summary>
        public void FindNearest(IRow row)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
            #endif
            InternalFindNearest(row);
        }
        
        protected virtual bool InternalRefresh(IRow row)
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }
        
        public bool Refresh(IRow row)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
            #endif
            return InternalRefresh(row);
        }
        
        public bool OptimisticRefresh(IRow row)
        {
            if (Supports(CursorCapability.Searchable))
				return Refresh(row);
            else
            {
                Reset();
				return false;
			}
        }
        
        // Countable
        protected virtual int InternalRowCount()
        {
            throw new RuntimeException(RuntimeException.Codes.NotCountable);
        }
        
        public int RowCount()
        {
			#if SAFETABLES
            CheckActive();
			CheckCapability(CursorCapability.Countable);
			#endif
            return InternalRowCount();
        }
        
		// Updateable        
		protected virtual void InternalInsert(IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			Node.Insert(Program, oldRow, newRow, valueFlags, uncheckedValue);
			if (CursorType == CursorType.Dynamic)
				OptimisticRefresh(newRow);
		}
        
        public void Insert(IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Updateable);
            #endif
            InternalInsert(oldRow, newRow, valueFlags, uncheckedValue);
        }
        
        public void Insert(IRow row)
        {
			BitArray valueFlags = new BitArray(row.DataType.Columns.Count);
			for (int index = 0; index < valueFlags.Length; index++)
				valueFlags[index] = true;
			Insert(null, row, valueFlags, false);
        }
        
		protected virtual void InternalUpdate(IRow row, BitArray valueFlags, bool uncheckedValue)
		{
			using (IRow localRow = Select())
			{
				Node.Update(Program, localRow, row, valueFlags, Isolation != CursorIsolation.Isolated, uncheckedValue);
				if (CursorType == CursorType.Dynamic)
				{
					row.CopyTo(localRow);
					OptimisticRefresh(localRow);
				}
			}
		}
        
        public void Update(IRow row, BitArray valueFlags, bool uncheckedValue)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Updateable);
            CheckNotOnCrack(); // Don't need this, the select will do it
            #endif
            InternalUpdate(row, valueFlags, uncheckedValue);
        }
        
        public void Update(IRow row)
        {
			BitArray valueFlags = new BitArray(row.DataType.Columns.Count);
			for (int index = 0; index < valueFlags.Length; index++)
				valueFlags[index] = true;
			Update(row, valueFlags, false);
        }
        
		protected virtual void InternalDelete(bool uncheckedValue)
		{
			using (IRow row = Select())
			{
				Node.Delete(Program, row, Isolation != CursorIsolation.Isolated, uncheckedValue);
				if (CursorType == CursorType.Dynamic)
					OptimisticRefresh(row);
			}
		}
		
        public void Delete(bool uncheckedValue)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Updateable);
            CheckNotOnCrack(); // Don't need this, the select will do it
            #endif
            InternalDelete(uncheckedValue);
        }

		public void Delete()
		{
			Delete(false);
		}

		protected virtual void InternalTruncate()
		{
			throw new RuntimeException(RuntimeException.Codes.NotTruncateable);
		}        
		
		public void Truncate()
		{
			#if SAFETABLES
			CheckActive();
			CheckCapability(CursorCapability.Truncateable);
			#endif
			InternalTruncate();
		}

        // Events
        #if USETABLEEVENTS
        public event EventHandler BeforeOpen;
        protected virtual void DoBeforeOpen()
        {
            if (BeforeOpen != null)
                BeforeOpen(this, EventArgs.Empty);
        }
        
        public event EventHandler AfterOpen;
        protected virtual void DoAfterOpen()
        {
            if (AfterOpen != null)
                AfterOpen(this, EventArgs.Empty);
        }
        
        public event EventHandler BeforeClose;
        protected virtual void DoBeforeClose()
        {
            if (BeforeClose != null)
                BeforeClose(this, EventArgs.Empty);
        }
        
        public event EventHandler AfterClose;
        protected virtual void DoAfterClose()
        {
            if (AfterClose != null)
                AfterClose(this, EventArgs.Empty);
        }
        #endif
        
		public override bool IsNil { get { return false; } }
		
		public override object AsNative
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); } 
		}
		
		public override int GetPhysicalSize(bool expandStreams)
		{
			throw new NotSupportedException();
		}

		public override void ReadFromPhysical(byte[] buffer, int offset)
		{
			throw new NotSupportedException();
		}

		public override void WriteToPhysical(byte[] buffer, int offset, bool expandStreams)
		{
			throw new NotSupportedException();
		}
		
		public override ITable OpenCursor()
		{
			return this;
		}
		
        public override object CopyNativeAs(Schema.IDataType dataType)
        {
			// We need to construct a new TableVar here because otherwise the native table
			// will be constructed with the keys inferred for the source, resulting in
			// incorrect access plans (see Defect #33978 for more).
			Schema.ResultTableVar tableVar = new Schema.ResultTableVar(Node);
			tableVar.Owner = Program.Plan.User;
			tableVar.EnsureTableVarColumns();
			Program.EnsureKey(tableVar);

			if (!Active)
				Open();
			else
				Reset();

			NativeTable nativeTable = new NativeTable(Manager, tableVar);
			while (Next())
			{
				using (IRow row = Select())
				{	
					nativeTable.Insert(Manager, row);
				}
			}

			return nativeTable;
        }

		///<summary>Returns true if the given key has the same number of columns in the same order as the node order key.</summary>
        protected bool IsKeyRow(IRow key)
        {
			Schema.Order order = Order;
			if (key.DataType.Columns.Count != order.Columns.Count)
				return false;
				
			for (int index = 0; index < order.Columns.Count; index++)
				if ((key.DataType.Columns.Count <= index) || !Schema.Object.NamesEqual(key.DataType.Columns[index].Name, order.Columns[index].Column.Name))
					return false;

			return true;
        }

		///<summary>Returns true if the given key has the same or fewer columns in the same order as the node order key, and once any column is null, the rest of the columns are also null.</summary>        
        protected bool IsPartialKeyRow(IRow key)
        {
			bool isNull = false;
			Schema.Order order = Order;
			if (key.DataType.Columns.Count > order.Columns.Count)
				return false;
			
			for (int index = 0; index < order.Columns.Count; index++)
				if (key.DataType.Columns.Count > index)
				{
					if (!Schema.Object.NamesEqual(key.DataType.Columns[index].Name, order.Columns[index].Column.Name))
						return false;
					if (isNull && key.HasValue(index))
						return false;
					if (!key.HasValue(index))
						isNull = true;
				}
				
			return true;
        }
        
        ///	<summary>
        ///	Returns a row that is guaranteed to contain the same columns in the same order as the node order.  
        /// If the given row does not satisfy this requirement, a row of the proper row type is created and the values from the given row are copied into it.
        /// </summary>
        protected IRow EnsureKeyRow(IRow key)
        {
			if (IsKeyRow(key))
				return key;
			else
			{
				Schema.IRowType rowType = DataType.CreateRowType(String.Empty, false);
				Schema.Order order = Order;
				for (int index = 0; index < order.Columns.Count; index++)
				{
					//int LColumnIndex = AKey.DataType.Columns.IndexOfName(LOrder.Columns[LIndex].Column.Name);
					//if (LColumnIndex >= 0)
						rowType.Columns.Add(order.Columns[index].Column.Column.Copy());
					// BTR 4/25/2005 -> There is no difference between not having the column, and having the column, but not having a value.
					// as such, I see no reason to throw this error, simply create the row and leave the column empty.
					//else
					//	throw new RuntimeException(RuntimeException.Codes.InvalidSearchArgument);
				}

				Row localKey = new Row(Manager, rowType);
				key.CopyTo(localKey);
				return localKey;
			}
        }

		/// <summary>
		/// Returns a row that is guaranteed to contain the same or fewer columns in the same order as the node order,
		///	and once a column is null, the rest of the columns are null as well.  If the given row does not satisfy this
		/// requirement, a row of the proper row type is created and the values from the given row are copied into it.
		/// If no such row can be created, null is returned.
		/// </summary>
        protected IRow EnsurePartialKeyRow(IRow key)
        {
			if (IsPartialKeyRow(key))
				return key;
			else
			{
				bool isNull = false;
				Schema.IRowType rowType = DataType.CreateRowType(String.Empty, false);
				Schema.Order order = Order;
				for (int index = 0; index < order.Columns.Count; index++)
				{
					int columnIndex = key.DataType.Columns.IndexOfName(order.Columns[index].Column.Name);
					if (columnIndex >= 0)
					{
						rowType.Columns.Add(order.Columns[index].Column.Column.Copy());
						if (isNull && key.HasValue(columnIndex))
							return null;
							
						if (!key.HasValue(index))
							isNull = true;
					}
					else
						break;
				}
				
				Row localKey = new Row(Manager, rowType);
				key.CopyTo(localKey);
				return localKey;
			}
        }
    }
    
	public class TableScan : Table
	{
		protected TableScan(TableNode node, Program program) : base(node, program) { }
		protected TableScan(IValueManager manager, TableNode node) : base(manager, node) { }
		
		public TableScan(IValueManager manager, NativeTable table, Schema.Order key, ScanDirection direction, Row firstKey, Row lastKey) : base(manager, table.TableType)
		{
			_nativeTable = table;
			_key = key;
			_direction = direction;
			_firstKey = firstKey;
			_lastKey = lastKey;
		}

		protected NativeTable _nativeTable;
		public NativeTable NativeTable
		{
			get { return _nativeTable; }
			set { _nativeTable = value; }
		}

		private Schema.Order _key;
		public Schema.Order Key
		{
			get { return _key; }
			set { _key = value; }
		}
		
		private ScanDirection _direction;
		public ScanDirection Direction
		{	
			get { return _direction; }
			set { _direction = value; }
		}
		
		private Row _firstKey;
		public Row FirstKey
		{
			get { return _firstKey; }
			set { _firstKey = value; }
		}
		
		private Row _lastKey;
		public Row LastKey
		{
			get { return _lastKey; }
			set { _lastKey = value; }
		}
		
		private Scan _scan;

		protected override void InternalOpen()
		{
			if (_key.Equivalent(_nativeTable.ClusteredIndex.Key))
				_scan = new Scan(Manager, _nativeTable, _nativeTable.ClusteredIndex, _direction, _firstKey, _lastKey);
			else
				_scan = new Scan(Manager, _nativeTable, _nativeTable.NonClusteredIndexes[_key], _direction, _firstKey, _lastKey);
			_scan.Open();
		}
		
		protected override void InternalClose()
		{
			if (_scan != null)
			{
				_scan.Dispose();
				_scan = null;
			}
		}
		
		protected override void InternalReset()
		{
			_scan.Reset();
		}
		
		protected override void InternalSelect(IRow row)
		{
			_scan.GetRow(row);
		}
		
		protected override void InternalFirst()
		{
			_scan.First();
		}
		
		protected override bool InternalPrior()
		{
			return _scan.Prior();
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

		// Bookmarkable

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

		// Searchable

		protected override Schema.Order InternalGetOrder()
		{
			return _key;
		}
		
		protected override IRow InternalGetKey()
		{
			return _scan.GetKey();
		}

		protected override bool InternalFindKey(IRow key, bool forward)
		{
			return _scan.FindKey(key);
		}
		
		protected override void InternalFindNearest(IRow key)
		{
			_scan.FindNearest(key);
		}
		
		protected override bool InternalRefresh(IRow key)
		{
			return _scan.FindNearest(key);
		}
	}
	
	public class TableValueScan : TableScan
	{
		public TableValueScan(TableValue tableValue) : base(tableValue.Manager, tableValue.AsNative as NativeTable, tableValue.Manager.FindClusteringOrder(((NativeTable)tableValue.AsNative).TableVar), ScanDirection.Forward, null, null) { }
		public TableValueScan(TableNode node, TableValue tableValue) : base(tableValue.Manager, node)
		{
			_nativeTable = tableValue.AsNative as NativeTable;
			Key = Manager.FindClusteringOrder(_nativeTable.TableVar); // ?? Why doesn't this use the order from the compile (ANode.Order)?
			Direction = ScanDirection.Forward;
		}
		
		// Updatable
		protected override void InternalInsert(IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			_nativeTable.Insert(Manager, newRow);
			if (CursorType == CursorType.Dynamic)
				Refresh(newRow);
		}
		
		protected override void InternalUpdate(IRow row, BitArray valueFlags, bool uncheckedValue)
		{
			using (IRow localRow = Select())
			{
				_nativeTable.Update(Manager, localRow, row);
				if (CursorType == CursorType.Dynamic)
				{
					row.CopyTo(localRow);
					Refresh(localRow);
				}
			}
		}
		
		protected override void InternalDelete(bool uncheckedValue)
		{
			using (IRow row = Select())
			{
				_nativeTable.Delete(Manager, row);
				if (CursorType == CursorType.Dynamic)
					Refresh(row);
			}
		}
	}
}

