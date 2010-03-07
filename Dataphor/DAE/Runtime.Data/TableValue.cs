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
	
	public class TableValue : DataValue
	{
		public TableValue(IValueManager AManager, INativeTable ATable) : base(AManager, ATable.TableType)
		{	
			FTable = ATable;
		}
		
		private INativeTable FTable;
		
		public override bool IsNil { get { return FTable == null; } }
		
		public override object AsNative
		{
			get { return FTable; }
			set 
			{
				if (FTable != null)
					FTable.Drop(Manager);
				FTable = (INativeTable)value; 
			} 
		}
		
		/*
			Physical representation format ->
			
				00 -> Value indicator 0 - nil, 1 - non-nil
				01-05 -> Number of rows
				06-XX -> N row values written using Row physical representation
		*/
		private List<Row> FRowList;
		private List<int> FSizeList;
		
		public override int GetPhysicalSize(bool AExpandStreams)
		{
			int LSize = 1;
			
			if (!IsNil)
			{
				LSize += sizeof(int);
				FRowList = new List<Row>();
				FSizeList = new List<int>();
				
				Table LTable = OpenCursor();
				try
				{
					while (LTable.Next())
					{
						Row LRow = LTable.Select();
						int LRowSize = LRow.GetPhysicalSize(AExpandStreams);
						LSize += LRowSize;
						FRowList.Add(LRow);
						FSizeList.Add(LRowSize);
					}
				}
				finally
				{
					LTable.Dispose();
				}
				
				LSize += sizeof(int) * FRowList.Count;
			}
			
			return LSize;
		}

		public override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			if (IsNil)
				ABuffer[AOffset] = 0;
			else
			{
				ABuffer[AOffset] = 1;
				AOffset++;
				
				int LRowSize;
				Streams.Conveyor LInt32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);
				
				LInt32Conveyor.Write(FRowList.Count, ABuffer, AOffset);
				AOffset += sizeof(int);
				
				for (int LIndex = 0; LIndex < FRowList.Count; LIndex++)
				{
					LRowSize = (int)FSizeList[LIndex];
					LInt32Conveyor.Write(LRowSize, ABuffer, AOffset);
					AOffset += sizeof(int);
					Row LRow = FRowList[LIndex];
					LRow.WriteToPhysical(ABuffer, AOffset, AExpandStreams);
					AOffset += LRowSize;
					LRow.ValuesOwned = false;
					LRow.Dispose();
				}
			}
		}
		
		public override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			FTable.Truncate(Manager);
			
			Streams.Conveyor LInt32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);
			
			if (ABuffer[AOffset] != 0)
			{
				AOffset++;
				
				int LRowSize;
				int LCount = (int)LInt32Conveyor.Read(ABuffer, AOffset);
				
				for (int LIndex = 0; LIndex < LCount; LIndex++)
				{
					LRowSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
					AOffset += sizeof(int);
					using (Row LRow = (Row)DataValue.FromPhysical(Manager, FTable.RowType, ABuffer, AOffset))
					{
						FTable.Insert(Manager, LRow);
					}
					AOffset += LRowSize;
				}
			}
		}

		public override Table OpenCursor()
		{
			Table LTable = new TableScan(Manager, FTable, Manager.FindClusteringOrder(FTable.TableVar), ScanDirection.Forward, null, null);
			LTable.Open();
			return LTable;
		}
		
		public override object CopyNativeAs(Schema.IDataType ADataType)
		{
		    return new NativeTableCopy(Manager,FTable);
            /*INativeTable LNewTable = new NativeTable(Manager, FTable.TableVar);
			using (Scan LScan = new Scan(Manager, FTable, FTable.ClusteredIndex, ScanDirection.Forward, null, null))
			{
				LScan.Open();
				while (LScan.Next())
				{
					using (Row LRow = LScan.GetRow())
					{
						LNewTable.Insert(Manager, LRow);
					}
				}
			}
			return LNewTable;*/		             
		}
	}
	
    /// <remarks> Table </remarks>
    public abstract class Table : DataValue
    {        
		protected Table(IValueManager AManager, Schema.ITableType ATableType) : base(AManager, ATableType) { }
		
		protected Table(IValueManager AManager, TableNode ANode) : base(AManager, ANode.DataType)
		{
			FNode = ANode;
		}
		
		protected Table(TableNode ANode, Program AProgram) : base(AProgram.ValueManager, ANode.DataType)
		{
			FNode = ANode;
			FProgram = AProgram;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			Close();
			base.Dispose(ADisposing);
			FNode = null;
		}
		
		protected Program FProgram;
		public Program Program { get { return FProgram; } }
        
        // DataType
        public new Schema.ITableType DataType { get { return (Schema.ITableType)base.DataType; } }
        
		// Node
		protected TableNode FNode;
		public TableNode Node
		{
			get
			{
				#if DEBUG
				if (FNode == null)
					throw new RuntimeException(RuntimeException.Codes.InternalError, "FNode is null.");
				#endif
				return FNode;
			}
		}

		// CursorType
		public virtual CursorType CursorType { get { return Node.RequestedCursorType; } }
		
        // Capabilities
        public virtual CursorCapability Capabilities { get { return Node.CursorCapabilities; }  }
		
		// Isolation
		public virtual CursorIsolation Isolation { get { return Node.CursorIsolation; } }
		
        public bool Supports(CursorCapability ACapability)
        {
			return ((ACapability & Capabilities) != 0);
        }
        
        public void CheckCapability(CursorCapability ACapability)
        {
			if (!Supports(ACapability))
				throw new RuntimeException(RuntimeException.Codes.CapabilityNotSupported, Enum.GetName(typeof(CursorCapability), ACapability));
        }
        
        protected void CheckAborted()
        {
            if (FProgram != null)
				FProgram.CheckAborted();
        }
        
        // Open        
        protected abstract void InternalOpen();
        public void Open()
        {
            if (!FActive)
            {
				#if USETABLEEVENTS
                DoBeforeOpen();
                #endif
                InternalOpen();
                FActive = true;
                #if USETABLEEVENTS
                DoAfterOpen();
                #endif
            }
        }
        
        // Close
        protected abstract void InternalClose();
        public void Close()
        {
            if (FActive)
            {
				#if USETABLEEVENTS
                DoBeforeClose();
                #endif
                InternalClose();
                FActive = false;
                #if USETABLEEVENTS
                DoAfterClose();
                #endif
            }
        }
        
        // Active
        protected bool FActive;
        public bool Active
        {
            get
            {
                return FActive;
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
        protected abstract void InternalSelect(Row ARow);
        public void Select(Row ARow)
        {
			#if SAFETABLES
            CheckActive();
            #endif
            CheckNotOnCrack();
            InternalSelect(ARow);
        }

        public Row Select()
        {
			Row LRow = new Row(Manager, DataType.RowType);
			try
			{
				Select(LRow);
				return LRow;
			}
			catch
			{
				LRow.Dispose();
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

		protected virtual Row InternalGetBookmark()
        {
            throw new RuntimeException(RuntimeException.Codes.NotBookmarkable);
        }
        
        public Row GetBookmark()
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Bookmarkable);
            #endif
            CheckNotOnCrack();
			return InternalGetBookmark();
        }

		protected virtual bool InternalGotoBookmark(Row ABookmark, bool AForward)
        {
            throw new RuntimeException(RuntimeException.Codes.NotBookmarkable);
        }

		public bool GotoBookmark(Row ABookmark, bool AForward)
		{
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Bookmarkable);
			#endif
			return InternalGotoBookmark(ABookmark, AForward);
		}

		public bool GotoBookmark(Row ABookmark)
        {
			return GotoBookmark(ABookmark, true);
        }
        
        protected virtual int InternalCompareBookmarks(Row ABokmark1, Row ABookmark2)
        {
            throw new RuntimeException(RuntimeException.Codes.NotBookmarkable);
        }
        
        public int CompareBookmarks(Row ABookmark1, Row ABookmark2)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Bookmarkable);
            #endif
            return InternalCompareBookmarks(ABookmark1, ABookmark2);
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
        
        protected virtual Row InternalGetKey()
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }
        
        public Row GetKey()
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
            #endif
            CheckNotOnCrack();
            return InternalGetKey();
        }
        
        protected virtual bool InternalFindKey(Row ARow, bool AForward)
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }

		/// <summary>
		///	Attempts to position the cursor on the key specified by the given row.  
		///	The row must be a superset of the current order key of the table.
		/// Returns true if successful, false otherwise.
		/// </summary>        
        public bool FindKey(Row ARow)
        {
			return FindKey(ARow, true);
        }

		/// <param name="AForward"> Provides a hint about the intended direction for bi-directionally navigable cursors. </param>
		public bool FindKey(Row ARow, bool AForward)
		{
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
			#endif
			return InternalFindKey(ARow, AForward);
		}
        
        protected virtual void InternalFindNearest(Row ARow)
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
        public void FindNearest(Row ARow)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
            #endif
            InternalFindNearest(ARow);
        }
        
        protected virtual bool InternalRefresh(Row ARow)
        {
            throw new RuntimeException(RuntimeException.Codes.NotSearchable);
        }
        
        public bool Refresh(Row ARow)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Searchable);
            #endif
            return InternalRefresh(ARow);
        }
        
        public bool OptimisticRefresh(Row ARow)
        {
            if (Supports(CursorCapability.Searchable))
				return Refresh(ARow);
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
		protected virtual void InternalInsert(Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			Node.Insert(Program, AOldRow, ANewRow, AValueFlags, AUnchecked);
			if (CursorType == CursorType.Dynamic)
				OptimisticRefresh(ANewRow);
		}
        
        public void Insert(Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Updateable);
            #endif
            InternalInsert(AOldRow, ANewRow, AValueFlags, AUnchecked);
        }
        
        public void Insert(Row ARow)
        {
			BitArray LValueFlags = new BitArray(ARow.DataType.Columns.Count);
			for (int LIndex = 0; LIndex < LValueFlags.Length; LIndex++)
				LValueFlags[LIndex] = true;
			Insert(null, ARow, LValueFlags, false);
        }
        
		protected virtual void InternalUpdate(Row ARow, BitArray AValueFlags, bool AUnchecked)
		{
			using (Row LRow = Select())
			{
				Node.Update(Program, LRow, ARow, AValueFlags, Isolation != CursorIsolation.Isolated, AUnchecked);
				if (CursorType == CursorType.Dynamic)
				{
					ARow.CopyTo(LRow);
					OptimisticRefresh(LRow);
				}
			}
		}
        
        public void Update(Row ARow, BitArray AValueFlags, bool AUnchecked)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Updateable);
            CheckNotOnCrack(); // Don't need this, the select will do it
            #endif
            InternalUpdate(ARow, AValueFlags, AUnchecked);
        }
        
        public void Update(Row ARow)
        {
			BitArray LValueFlags = new BitArray(ARow.DataType.Columns.Count);
			for (int LIndex = 0; LIndex < LValueFlags.Length; LIndex++)
				LValueFlags[LIndex] = true;
			Update(ARow, LValueFlags, false);
        }
        
		protected virtual void InternalDelete(bool AUnchecked)
		{
			using (Row LRow = Select())
			{
				Node.Delete(Program, LRow, Isolation != CursorIsolation.Isolated, AUnchecked);
				if (CursorType == CursorType.Dynamic)
					OptimisticRefresh(LRow);
			}
		}
		
        public void Delete(bool AUnchecked)
        {
			#if SAFETABLES
            CheckActive();
            CheckCapability(CursorCapability.Updateable);
            CheckNotOnCrack(); // Don't need this, the select will do it
            #endif
            InternalDelete(AUnchecked);
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
		
		public override int GetPhysicalSize(bool AExpandStreams)
		{
			throw new NotSupportedException();
		}

		public override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			throw new NotSupportedException();
		}

		public override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			throw new NotSupportedException();
		}
		
		public override Table OpenCursor()
		{
			throw new NotSupportedException();
		}
		
        public override object CopyNativeAs(Schema.IDataType ADataType)
        {
			throw new NotSupportedException();
        }

		///<summary>Returns true if the given key has the same number of columns in the same order as the node order key.</summary>
        protected bool IsKeyRow(Row AKey)
        {
			Schema.Order LOrder = Order;
			if (AKey.DataType.Columns.Count != LOrder.Columns.Count)
				return false;
				
			for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				if ((AKey.DataType.Columns.Count <= LIndex) || !Schema.Object.NamesEqual(AKey.DataType.Columns[LIndex].Name, LOrder.Columns[LIndex].Column.Name))
					return false;

			return true;
        }

		///<summary>Returns true if the given key has the same or fewer columns in the same order as the node order key, and once any column is null, the rest of the columns are also null.</summary>        
        protected bool IsPartialKeyRow(Row AKey)
        {
			bool LIsNull = false;
			Schema.Order LOrder = Order;
			if (AKey.DataType.Columns.Count > LOrder.Columns.Count)
				return false;
			
			for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				if (AKey.DataType.Columns.Count > LIndex)
				{
					if (!Schema.Object.NamesEqual(AKey.DataType.Columns[LIndex].Name, LOrder.Columns[LIndex].Column.Name))
						return false;
					if (LIsNull && AKey.HasValue(LIndex))
						return false;
					if (!AKey.HasValue(LIndex))
						LIsNull = true;
				}
				
			return true;
        }
        
        ///	<summary>
        ///	Returns a row that is guaranteed to contain the same columns in the same order as the node order.  
        /// If the given row does not satisfy this requirement, a row of the proper row type is created and the values from the given row are copied into it.
        /// </summary>
        protected Row EnsureKeyRow(Row AKey)
        {
			if (IsKeyRow(AKey))
				return AKey;
			else
			{
				Schema.IRowType LRowType = DataType.CreateRowType(String.Empty, false);
				Schema.Order LOrder = Order;
				for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				{
					//int LColumnIndex = AKey.DataType.Columns.IndexOfName(LOrder.Columns[LIndex].Column.Name);
					//if (LColumnIndex >= 0)
						LRowType.Columns.Add(LOrder.Columns[LIndex].Column.Column.Copy());
					// BTR 4/25/2005 -> There is no difference between not having the column, and having the column, but not having a value.
					// as such, I see no reason to throw this error, simply create the row and leave the column empty.
					//else
					//	throw new RuntimeException(RuntimeException.Codes.InvalidSearchArgument);
				}

				Row LKey = new Row(Manager, LRowType);
				AKey.CopyTo(LKey);
				return LKey;
			}
        }

		/// <summary>
		/// Returns a row that is guaranteed to contain the same or fewer columns in the same order as the node order,
		///	and once a column is null, the rest of the columns are null as well.  If the given row does not satisfy this
		/// requirement, a row of the proper row type is created and the values from the given row are copied into it.
		/// If no such row can be created, null is returned.
		/// </summary>
        protected Row EnsurePartialKeyRow(Row AKey)
        {
			if (IsPartialKeyRow(AKey))
				return AKey;
			else
			{
				bool LIsNull = false;
				Schema.IRowType LRowType = DataType.CreateRowType(String.Empty, false);
				Schema.Order LOrder = Order;
				for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				{
					int LColumnIndex = AKey.DataType.Columns.IndexOfName(LOrder.Columns[LIndex].Column.Name);
					if (LColumnIndex >= 0)
					{
						LRowType.Columns.Add(LOrder.Columns[LIndex].Column.Column.Copy());
						if (LIsNull && AKey.HasValue(LColumnIndex))
							return null;
							
						if (!AKey.HasValue(LIndex))
							LIsNull = true;
					}
					else
						break;
				}
				
				Row LKey = new Row(Manager, LRowType);
				AKey.CopyTo(LKey);
				return LKey;
			}
        }
    }
    
	public class TableScan : Table
	{
		protected TableScan(TableNode ANode, Program AProgram) : base(ANode, AProgram) { }
		protected TableScan(IValueManager AManager, TableNode ANode) : base(AManager, ANode) { }
		
		public TableScan(IValueManager AManager, INativeTable ATable, Schema.Order AKey, ScanDirection ADirection, Row AFirstKey, Row ALastKey) : base(AManager, ATable.TableType)
		{
			FNativeTable = ATable;
			FKey = AKey;
			FDirection = ADirection;
			FFirstKey = AFirstKey;
			FLastKey = ALastKey;
		}
		
		protected INativeTable FNativeTable;
		public INativeTable NativeTable
		{
			get { return FNativeTable; }
			set { FNativeTable = value; }
		}

		private Schema.Order FKey;
		public Schema.Order Key
		{
			get { return FKey; }
			set { FKey = value; }
		}
		
		private ScanDirection FDirection;
		public ScanDirection Direction
		{	
			get { return FDirection; }
			set { FDirection = value; }
		}
		
		private Row FFirstKey;
		public Row FirstKey
		{
			get { return FFirstKey; }
			set { FFirstKey = value; }
		}
		
		private Row FLastKey;
		public Row LastKey
		{
			get { return FLastKey; }
			set { FLastKey = value; }
		}
		
		private Scan FScan;

		protected override void InternalOpen()
		{
			if (FKey.Equivalent(FNativeTable.ClusteredIndex.Key))
				FScan = new Scan(Manager, FNativeTable, FNativeTable.ClusteredIndex, FDirection, FFirstKey, FLastKey);
			else
				FScan = new Scan(Manager, FNativeTable, FNativeTable.NonClusteredIndexes[FKey], FDirection, FFirstKey, FLastKey);
			FScan.Open();
		}
		
		protected override void InternalClose()
		{
			if (FScan != null)
			{
				FScan.Dispose();
				FScan = null;
			}
		}
		
		protected override void InternalReset()
		{
			FScan.Reset();
		}
		
		protected override void InternalSelect(Row ARow)
		{
			FScan.GetRow(ARow);
		}
		
		protected override void InternalFirst()
		{
			FScan.First();
		}
		
		protected override bool InternalPrior()
		{
			return FScan.Prior();
		}
		
		protected override bool InternalNext()
		{
			return FScan.Next();
		}
		
		protected override void InternalLast()
		{
			FScan.Last();
		}
		
		protected override bool InternalBOF()
		{
			return FScan.BOF();
		}
		
		protected override bool InternalEOF()
		{
			return FScan.EOF();
		}

		// Bookmarkable

		protected override Row InternalGetBookmark()
		{
			return FScan.GetKey();
		}

		protected override bool InternalGotoBookmark(Row ABookmark, bool AForward)
		{
			return FScan.FindKey(ABookmark);
		}
        
		protected override int InternalCompareBookmarks(Row ABookmark1, Row ABookmark2)
		{
			return FScan.CompareKeys(ABookmark1, ABookmark2);
		}

		// Searchable

		protected override Schema.Order InternalGetOrder()
		{
			return FKey;
		}
		
		protected override Row InternalGetKey()
		{
			return FScan.GetKey();
		}

		protected override bool InternalFindKey(Row AKey, bool AForward)
		{
			return FScan.FindKey(AKey);
		}
		
		protected override void InternalFindNearest(Row AKey)
		{
			FScan.FindNearest(AKey);
		}
		
		protected override bool InternalRefresh(Row AKey)
		{
			return FScan.FindNearest(AKey);
		}
	}
	
	public class TableValueScan : TableScan
	{
		public TableValueScan(TableNode ANode, TableValue ATableValue) : base(ATableValue.Manager, ANode)
		{
			FNativeTable = ATableValue.AsNative as INativeTable;
			Key = Manager.FindClusteringOrder(FNativeTable.TableVar); // ?? Why doesn't this use the order from the compile (ANode.Order)?
			Direction = ScanDirection.Forward;
		}
		
		// Updatable
		protected override void InternalInsert(Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			FNativeTable.Insert(Manager, ANewRow);
			if (CursorType == CursorType.Dynamic)
				Refresh(ANewRow);
		}
		
		protected override void InternalUpdate(Row ARow, BitArray AValueFlags, bool AUnchecked)
		{
			using (Row LRow = Select())
			{
				FNativeTable.Update(Manager, LRow, ARow);
				if (CursorType == CursorType.Dynamic)
				{
					ARow.CopyTo(LRow);
					Refresh(LRow);
				}
			}
		}
		
		protected override void InternalDelete(bool AUnchecked)
		{
			using (Row LRow = Select())
			{
				FNativeTable.Delete(Manager, LRow);
				if (CursorType == CursorType.Dynamic)
					Refresh(LRow);
			}
		}
	}
}

