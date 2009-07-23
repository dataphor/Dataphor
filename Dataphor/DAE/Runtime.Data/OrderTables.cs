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
		public SortTableBase(BaseOrderNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		public new BaseOrderNode Node { get { return (BaseOrderNode)FNode; } }
		
        protected NativeTable FTable;
        protected Scan FScan;
        protected int FRowCount;
        
        protected abstract void PopulateTable();

		protected override void InternalOpen()
		{
			// TODO: Rewrite this...
			Schema.TableType LTableType = new Schema.TableType();
			Schema.BaseTableVar LTableVar = new Schema.BaseTableVar(LTableType);
			Schema.TableVarColumn LNewColumn;
			foreach (Schema.TableVarColumn LColumn in Node.TableVar.Columns)
			{
				LNewColumn = (Schema.TableVarColumn)LColumn.Copy();
				LTableType.Columns.Add(LNewColumn.Column);
				LTableVar.Columns.Add(LNewColumn);
			}
			
			Schema.Order LOrder = new Schema.Order();
			Schema.OrderColumn LNewOrderColumn;
			Schema.OrderColumn LOrderColumn;
			for (int LIndex = 0; LIndex < Node.Order.Columns.Count; LIndex++)
			{
				LOrderColumn = Node.Order.Columns[LIndex];
				LNewOrderColumn = new Schema.OrderColumn(LTableVar.Columns[LOrderColumn.Column], LOrderColumn.Ascending, LOrderColumn.IncludeNils);
				LNewOrderColumn.Sort = LOrderColumn.Sort;
				LNewOrderColumn.IsDefaultSort = LOrderColumn.IsDefaultSort;
				LOrder.Columns.Add(LNewOrderColumn);
			}
			LTableVar.Orders.Add(LOrder);

			FTable = new NativeTable(Process, LTableVar);
			PopulateTable();
			FScan = new Scan(Process, FTable, FTable.ClusteredIndex, ScanDirection.Forward, null, null);
			FScan.Open();
		}

		protected override void InternalClose()
		{
			if (FScan != null)
				FScan.Dispose();
			FScan = null;
			if (FTable != null)
			{
				FTable.Drop(Process);
				FTable = null;
			}
		}
		
		protected override void InternalReset()
		{
			FScan.Dispose();
			FTable.Truncate(Process);
			PopulateTable();
			FScan = new Scan(Process, FTable, FTable.ClusteredIndex, ScanDirection.Forward, null, null);
			FScan.Open();
		}
		
		protected override void InternalSelect(Row ARow)
		{
			FScan.GetRow(ARow);
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
		
		protected override void InternalFirst()
		{
			FScan.First();
		}
		
		protected override bool InternalPrior()
		{
			return FScan.Prior();
		}

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

        protected override Row InternalGetKey()
        {
			return FScan.GetKey();
        }
        
        protected override bool InternalFindKey(Row ARow, bool AForward)
        {
			return FScan.FindKey(ARow);
        }
        
        protected override void InternalFindNearest(Row ARow)
        {
			FScan.FindNearest(ARow);
        }
        
        protected override bool InternalRefresh(Row ARow)
        {					
			if (ARow == null)
				ARow = Select();	 
			InternalReset();
			return FScan.FindNearest(ARow);
        }

        // ICountable
        protected override int InternalRowCount()
        {
			return FRowCount;
        }
    }
    
    public class CopyTable : SortTableBase
    {
		public CopyTable(CopyNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}
		
		public new CopyNode Node { get { return (CopyNode)FNode; } }

		protected override void PopulateTable()
		{
			using (Table LTable = (Table)Node.Nodes[0].Execute(Process))
			{
				Row LRow = new Row(Process, DataType.RowType);
				try
				{
					FRowCount = 0;
					while (LTable.Next())
					{
						LTable.Select(LRow);
						FRowCount++;
						FTable.Insert(Process, LRow); // no validation is required because FTable will never be changed
						LRow.ClearValues();
						Process.CheckAborted(); // Yield
					}
				}
				finally
				{
					LRow.Dispose();
				}
			}
		}
    }
    
    public class OrderTable : SortTableBase
    {
		public OrderTable(OrderNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}
		
		public new OrderNode Node { get { return (OrderNode)FNode; } }

		protected override void PopulateTable()
		{
			using (Table LTable = (Table)Node.Nodes[0].Execute(Process))
			{
				Row LRow = new Row(Process, DataType.RowType);
				try
				{
					FRowCount = 0;
					while (LTable.Next())
					{
						LTable.Select(LRow);
						FRowCount++;
						if (Node.SequenceColumnIndex >= 0)
							LRow[Node.SequenceColumnIndex] = FRowCount;
						FTable.Insert(Process, LRow); // no validation is required because FTable will never be changed
						LRow.ClearValues();
						Process.CheckAborted();	// Yield
					}
				}
				finally
				{
					LRow.Dispose();
				}
			}
		}
    }
    
    public class BrowseTableItem : Disposable
    {
        // constructor
        protected internal BrowseTableItem(BrowseTable ABrowseTable, Table ATable, object AContextVar, Row AOrigin, bool AForward, bool AInclusive)
        {
			FBrowseTable = ABrowseTable;
            FTable = ATable;
			FContextVar = AContextVar;
            FRow = new Row(FBrowseTable.Process, FTable.DataType.RowType);
            FOrigin = AOrigin;
            FForward = AForward;
            FInclusive = AInclusive;
        }
        
        // Dispose
        protected override void Dispose(bool ADisposing)
        {
            if (FTable != null)
            {
                FTable.Dispose();
                FTable = null;
            }
            
            if (FOrigin != null)
            {
				FOrigin.Dispose();
                FOrigin = null;
            }
            
            if (FOrderKey != null)
            {
				FOrderKey.Dispose();
                FOrderKey = null;
            }
            
            if (FUniqueKey != null)
            {
				FUniqueKey.Dispose();
                FUniqueKey = null;
            }
            
            if (FRow != null)
            {
				FRow.Dispose();
                FRow = null;
            }

            base.Dispose(ADisposing);
        }

        // Table
        protected Table FTable;
        public Table Table { get { return FTable; } }
        
        // ContextVar
        protected object FContextVar;
        public object ContextVar { get { return FContextVar; } }

        // Origin
        protected Row FOrigin;
        public Row Origin { get { return FOrigin; } }
        
        // OrderKey
        protected Row FOrderKey;
        public Row OrderKey
        {
            get
            {
                FindOrderKey();
                return FOrderKey;
            }
        }
        
        protected void FindOrderKey()
        {
            if (!(FTable.BOF() || FTable.EOF()))
            {
                if (FOrderKey == null)
                    FOrderKey = BuildOrderKeyRow();
                else
                    FOrderKey.ClearValues();
                    
                FTable.Select(FOrderKey);

            }
            else
                if (FOrderKey != null)
                {
					FOrderKey.Dispose();
                    FOrderKey = null;
                }
        }
        
        // UniqueKey
        protected Row FUniqueKey;
        public Row UniqueKey
        {
            get
            {
                FindUniqueKey();
                return FUniqueKey;
            }
        }
        
        protected void FindUniqueKey()
        {
            if (!(FTable.BOF() || FTable.EOF()))
            {
                if (FUniqueKey == null)
                    FUniqueKey = BuildUniqueKeyRow();
                else
                    FUniqueKey.ClearValues();

                FTable.Select(FUniqueKey);

            }
            else
                if (FUniqueKey != null)
                {
					FUniqueKey.Dispose();
                    FUniqueKey = null;
                }
        }
        
        protected Row BuildOrderKeyRow()
        {
			return new Row(FBrowseTable.Process, new Schema.RowType(FBrowseTable.Node.Order.Columns));
        }
        
        protected Row BuildUniqueKeyRow()
        {
			Schema.RowType LRowType = new Schema.RowType(FBrowseTable.Node.Order.Columns);
			return new Row(FBrowseTable.Process, LRowType);
        }
        
        // Forward
        protected bool FForward;
        public bool Forward { get { return FForward; } }
        
        // Inclusive
        protected bool FInclusive;
        public bool Inclusive { get { return FInclusive; } }
        
        // OnOrigin
        public bool OnOrigin
        {
            get
            {
                FindOrderKey();
                if ((FOrigin == null) || (FOrderKey == null))
					return false;
				else
	                return FBrowseTable.CompareKeys(FOrigin, FOrderKey) == 0;
            }
        }
        
        // OnCrack
        public bool OnCrack { get { return (FTable.BOF() && !FTable.EOF()) || (FTable.EOF() && !FTable.BOF()); } }
        
        public void MoveCrack()
        {
            if (OnCrack)
                if (FTable.BOF())
                    FTable.Next();
                else
                    if (FTable.EOF())
                        FTable.Prior();
        }
        
        protected BrowseTable FBrowseTable;
        public BrowseTable BrowseTable { get { return FBrowseTable; } }
        
        protected Row FRow;
    }
    
    public class BrowseTableList : DisposableTypedList
    {
        public const int CMinTables = 1;

        // BrowseTable
        protected BrowseTable FBrowseTable;
        public BrowseTable BrowseTable { get { return FBrowseTable; } }
        
        // MaxTables
        protected int FMaxTables = 2;
        public int MaxTables
        {
            get { return FMaxTables; }
            set
            {
                if (value <= Count)
                    throw new RuntimeException(RuntimeException.Codes.CurrentListSizeExceedsNewSetting);
				if (value < CMinTables)
					throw new RuntimeException(RuntimeException.Codes.NewValueViolatesMinimumTableCount);
                FMaxTables = value;
            }
        }
        
        public void Add(BrowseTableItem ATable)
        {
            int LIndex = IndexOf(ATable);
            if (LIndex < 0)
            {
                if (Count >= FMaxTables)
					RemoveAt(Count - 1);
                Insert(0, ATable);
            }
            else
            {
                if (LIndex > 0)
                {
					DisownAt(LIndex);
                    Insert(0, ATable);
                }
            }
        }
        
        public override void Clear()
        {
			base.Clear();
        }
        
        public new BrowseTableItem this[int AIndex] { get { return (BrowseTableItem)base[AIndex]; } }
        
        public BrowseTableItem this[Table AIndex]
        {
            get
            {
                for (int LIndex = 0; LIndex < Count; LIndex++)
                {
                    if (this[LIndex].Table == AIndex)
                        return this[LIndex];
                }
                return null;
            }
        }
        
        public BrowseTableList(BrowseTable ABrowseTable) : base()
        {
            FBrowseTable = ABrowseTable;
            FItemsOwned = true;
            FItemType = typeof(BrowseTableItem);
        }
    }
    
    public class BrowseTable : Table
    {
		public BrowseTable(BrowseNode ANode, ServerProcess AProcess) : base(ANode, AProcess)
        {
            FTables = new BrowseTableList(this);
        }
        
        protected override void Dispose(bool ADisposing)
        {
			try
			{
	            base.Dispose(ADisposing);
	        }
	        finally
	        {
	            FTables.Dispose();
	        }
        }
        
        protected BrowseTableList FTables;
        
        public new BrowseNode Node { get { return (BrowseNode)FNode; } }

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
        
        protected void EnterTableContext(BrowseTableItem ATableItem)
        {
			Process.Context.Push(ATableItem.ContextVar);
        }
        
        protected void ExitTableContext(BrowseTableItem ATableItem)
        {
			Process.Context.Pop();
        }
        
        // Must be called with the original stack
        protected BrowseTableItem CreateTable(Row AOrigin, bool AForward, bool AInclusive)
        {
			// Prepare the context variable to contain the origin value (0 if this is an unanchored set)
			object LContextVar;
			Row LOrigin;
			if (AOrigin == null)
			{
				LOrigin = null;
				LContextVar = 0;
			}
			else
			{
				if ((AOrigin.DataType.Columns.Count > 0) && (Schema.Object.Qualifier(AOrigin.DataType.Columns[0].Name) != Keywords.Origin))
					LOrigin = new Row(Process, new Schema.RowType(AOrigin.DataType.Columns, Keywords.Origin));
				else
					LOrigin = new Row(Process, new Schema.RowType(AOrigin.DataType.Columns));
				AOrigin.CopyTo(LOrigin);
				LContextVar = LOrigin;
			}
				
			int LOriginIndex = ((LOrigin == null) ? -1 : LOrigin.DataType.Columns.Count - 1);
			bool LInclusive = (LOrigin == null) ? true : AInclusive;

			// Ensure the browse node has the appropriate browse variant				
			lock (Node)
			{
				if (!Node.HasBrowseVariant(LOriginIndex, AForward, LInclusive))
				{
					if (Process.ApplicationTransactionID != Guid.Empty)
					{
						ApplicationTransaction LTransaction = Process.GetApplicationTransaction();
						try
						{
							LTransaction.PushGlobalContext();
							try
							{
								Node.CompileBrowseVariant(Process, LOriginIndex, AForward, LInclusive);
							}
							finally
							{
								LTransaction.PopGlobalContext();
							}
						}
						finally
						{
							Monitor.Exit(LTransaction);
						}
					}
					else
						Node.CompileBrowseVariant(Process, LOriginIndex, AForward, LInclusive);
				}
			}
		
			// Execute the variant with the current context variable
			Process.Context.Push(LContextVar);
			try
			{
				PlanNode LBrowseVariantNode = Node.GetBrowseVariantNode(Process.Plan, LOriginIndex, AForward, LInclusive);
				#if TRACEBROWSEEVENTS
				Trace.WriteLine(String.Format("BrowseTableItem created with query: {0}", new D4TextEmitter().Emit(LBrowseVariantNode.EmitStatement(EmitMode.ForCopy))));
				#endif
				return 
					new BrowseTableItem
					(
						this, 
						(Table)LBrowseVariantNode.Execute(Process),
						LContextVar,
						LOrigin, 
						AForward, 
						AInclusive
					);
			}
			finally
			{
				Process.Context.Pop();
			}
        }
        
        // Must be called with the original stack
        protected BrowseTableItem FindTable(Row AOrigin, bool AForward, bool AInclusive)
        {
            foreach (BrowseTableItem LTable in FTables)
            {
				EnterTableContext(LTable);
				try
				{
					if 
						(
							(
								(AOrigin != null) &&
								(LTable.OrderKey != null) &&
								(LTable.Forward == AForward) &&
								(LTable.Inclusive == AInclusive) &&
								(CompareKeys(AOrigin, LTable.OrderKey) == 0)
							) ||
							(
								(AOrigin == null) &&
								(LTable.Origin == null) &&
								(LTable.Forward == AForward) &&
								(LTable.Table.BOF())
							)
						)
					{
						if (!AInclusive)
							LTable.Table.Next();
						return LTable;
					}
				}
				finally
				{
					ExitTableContext(LTable);
				}
            }
            return null;
        }

		// Must be called with the original stack        
        protected BrowseTableItem GetTable(Row AOrigin, bool AForward, bool AInclusive)
        {
            BrowseTableItem LResult = FindTable(AOrigin, AForward, AInclusive);
            if (LResult == null)
				LResult = CreateTable(AOrigin, AForward, AInclusive);
            return LResult;
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
                if (FTables.Count == 0)
                    throw new RuntimeException(RuntimeException.Codes.NoTopTable);
                return FTables[0];
            }
        }
        
        protected override void InternalOpen()
        {
            FTables.Add(GetTable());
        }
        
        protected override void InternalClose()
        {
            FTables.Clear();
        }
        
        protected override void InternalReset()
        {
            FTables.Clear();
	        FTables.Add(GetTable());
        }
        
        protected override bool InternalRefresh(Row ARow)
        {					
			if (ARow == null)
				ARow = Select();

			FTables.Clear();
			try
			{
				bool LResult = InternalFindKey(ARow, true);
				if (!LResult)
					InternalFindNearest(ARow);
				return LResult;
			}
			catch
			{
				if (FTables.Count == 0)
					FTables.Add(GetTable());
				throw;
			}
        }

        protected override void InternalSelect(Row ARow)
        {
			EnterTableContext(TopTable);
			try
			{
				TopTable.Table.Select(ARow);
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }

		// Must be called with the original stack        
        protected void SwapReader(Row AOrigin, bool AForward, bool AInclusive)
        {
			BrowseTableItem LItem = GetTable(AOrigin, AForward, AInclusive);
			FTables.Add(LItem);
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
        protected void SwapReader(bool AForward)
        {
			Row LOrigin = null;
			try
			{
				EnterTableContext(TopTable);
				try
				{
					LOrigin = TopTable.Origin != null ? (Row)TopTable.Origin.Copy() : null;
				}
				finally
				{
					ExitTableContext(TopTable);
				}
				SwapReader(LOrigin, AForward, !TopTable.Inclusive);
			}
			finally
			{
				if (LOrigin != null)
					LOrigin.Dispose();
			}
        }
        
        protected void Move(bool AForward)
        {
            if (TopTable.Forward == AForward)
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
				bool LInTableContext = true;
				EnterTableContext(TopTable);
				try
				{
					if (TopTable.Table.BOF() && TopTable.Origin != null)
					{
						ExitTableContext(TopTable);
						LInTableContext = false;
						SwapReader(AForward);
					}
					else
					{
						if (TopTable.Table.Supports(CursorCapability.BackwardsNavigable))
						{
							TopTable.Table.Prior();
							if (TopTable.Table.BOF() && (TopTable.Origin != null))
							{
								ExitTableContext(TopTable);
								LInTableContext = false;
								SwapReader(AForward);
							}
						}
						else
						{
							Row LOrigin = TopTable.OrderKey != null ? (Row)TopTable.OrderKey.Copy() : TopTable.Origin != null ? (Row)TopTable.Origin.Copy() : null;
							try
							{
								ExitTableContext(TopTable);
								LInTableContext = false;
								SwapReader(LOrigin, AForward, false);
							}
							finally
							{
								if (LOrigin != null)
									LOrigin.Dispose();
							}
						}
					}
				}
				finally
				{
					if (LInTableContext)
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
			BrowseTableItem LItem = GetTable(null, false, true);
            FTables.Add(LItem);
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
			BrowseTableItem LItem = GetTable(null, true, true);
            FTables.Add(LItem);
        }
        
        protected override Row InternalGetBookmark()
        {
			return InternalGetKey();
        }

		protected override bool InternalGotoBookmark(Row ABookmark, bool AForward)
        {
            return InternalFindKey(ABookmark, AForward);
        }
        
        protected override int InternalCompareBookmarks(Row ABookmark1, Row ABookmark2)
        {
			return CompareKeys(ABookmark1, ABookmark2);
        }
        
		public int CompareKeys(Row AIndexKey, Row ACompareKey)
        {
			int LResult = 0;
			for (int LIndex = 0; LIndex < AIndexKey.DataType.Columns.Count; LIndex++)
			{
				if (LIndex >= ACompareKey.DataType.Columns.Count)
					break;

				if (AIndexKey.HasValue(LIndex) && ACompareKey.HasValue(LIndex))
				{
					Process.Context.Push(AIndexKey[LIndex]);
					try
					{
						Process.Context.Push(ACompareKey[LIndex]);
						try
						{
							LResult = (int)Node.Order.Columns[LIndex].Sort.CompareNode.Execute(Process);

							// Swap polarity for descending columns
							if (!Node.Order.Columns[LIndex].Ascending)
								LResult = -LResult;
						}
						finally
						{
							Process.Context.Pop();
						}
					}
					finally
					{
						Process.Context.Pop();
					}
				}
				else if (AIndexKey.HasValue(LIndex))
				{
					// Index Key Has A Value
					LResult = Node.Order.Columns[LIndex].Ascending ? 1 : -1;
				}
				else if (ACompareKey.HasValue(LIndex))
				{
					// Compare Key Has A Value
					LResult = Node.Order.Columns[LIndex].Ascending ? -1 : 1;
				}
				else
				{
					// Neither key has a value
					LResult = 0;
				}
				
				if (LResult != 0)
					break;
			}

			return LResult;
        }
        
        protected override Row InternalGetKey()
        {
			EnterTableContext(TopTable);
			try
			{
				return (Row)TopTable.OrderKey.Copy();
			}
			finally
			{
				ExitTableContext(TopTable);
			}
        }

		protected override bool InternalFindKey(Row ARow, bool AForward)
        {
			Row LRow = EnsureKeyRow(ARow);
			try
			{
				bool LTableCreated = false;
				BrowseTableItem LTable = FindTable(LRow, AForward, true);
				if (LTable == null)
				{
					LTable = CreateTable(LRow, AForward, true);
					LTableCreated = true;
				}
				try
				{
					EnterTableContext(LTable);
					try
					{
						LTable.MoveCrack();
						bool LResult = (LTable.UniqueKey != null) && (CompareKeys(LRow, LTable.UniqueKey) == 0);
						if (LResult)
							FTables.Add(LTable);
						else
							if (LTableCreated)
								LTable.Dispose();
						return LResult;
					}
					finally
					{
						ExitTableContext(LTable);
					}
				}
				catch
				{
					if (LTableCreated)
						LTable.Dispose();
					throw;
				}
			}
			finally
			{
				if (!Object.ReferenceEquals(ARow, LRow))
					LRow.Dispose();
			}
        }
        
        protected override void InternalFindNearest(Row ARow)
        {
			Row LRow = EnsurePartialKeyRow(ARow);
			try
			{
				if (LRow != null)
				{
					BrowseTableItem LItem = GetTable(LRow, true, true);
		            FTables.Add(LItem);
		            EnterTableContext(LItem);
		            try
		            {
				        TopTable.MoveCrack();
				    }
				    finally
				    {
						ExitTableContext(LItem);
				    }
			    }
			    else
					FTables.Add(GetTable());
		    }
		    finally
		    {
				if ((LRow != null) && !Object.ReferenceEquals(ARow, LRow))
					LRow.Dispose();
		    }
        }
        
        protected override int InternalRowCount()
        {
            throw new RuntimeException(RuntimeException.Codes.UnimplementedInternalRowCount);
        }
	}
}