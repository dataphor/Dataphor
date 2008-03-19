/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
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
	using Schema = Alphora.Dataphor.DAE.Schema;

    public class AggregateTable : Table
    {
		public AggregateTable(AggregateNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}

        public new AggregateNode Node { get { return (AggregateNode)FNode; } }
        
		protected Table FSourceTable;
		protected Row FSourceRow;
		protected DataVar FSourceRowVar;
        
        protected override void InternalOpen()
        {
			FSourceTable = (Table)Node.Nodes[0].Execute(Process).Value;
			FSourceRow = new Row(Process, FSourceTable.DataType.RowType);
			FSourceRowVar = new DataVar(FSourceRow.DataType, FSourceRow);
        }
        
        protected override void InternalClose()
        {
			if (FSourceTable != null)
			{
				FSourceTable.Dispose();
				FSourceTable = null;
			}
			
            if (FSourceRow != null)
            {
				FSourceRow.Dispose();
                FSourceRow = null;
            }
            
            FSourceRowVar = null;
        }
        
        protected override void InternalReset()
        {
			FSourceTable.Reset();
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FSourceTable.Select(FSourceRow);
			FSourceRow.CopyTo(ARow);
			
			Process.Context.Push(FSourceRowVar);
			try
			{
				int LNodeIndex;
				int LColumnIndex;
				DataVar LAggregateValue;
				for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(DataType.Columns[LIndex].Name);
					if (LColumnIndex >= 0)
					{
						LNodeIndex = (LIndex - Node.AggregateColumnOffset) + 1;
						if (LNodeIndex >= 1)
						{
							LAggregateValue = Node.Nodes[LNodeIndex].Execute(Process);
							ARow[LColumnIndex] = LAggregateValue.Value;
						}
						else
						{
							if (FSourceRow.HasValue(LIndex))
								ARow[LColumnIndex] = FSourceRow[LIndex];
							else
								ARow.ClearValue(LColumnIndex);
						}
					}
				}
			}
			finally
			{
				Process.Context.Pop();
			}
        }
        
        protected override bool InternalNext()
        {
			return FSourceTable.Next();
        }
        
        protected override bool InternalBOF()
        {
			return FSourceTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
			return FSourceTable.EOF();
        }
        
        protected override bool InternalPrior()
        {
			return FSourceTable.Prior();
        }
        
        protected override void InternalFirst()
        {
			FSourceTable.First();
        }
        
        protected override Row InternalGetBookmark()
        {
			return FSourceTable.GetBookmark();
        }

		protected override bool InternalGotoBookmark(Row ABookmark, bool AForward)
        {
			return FSourceTable.GotoBookmark(ABookmark, AForward);
        }
        
        protected override int InternalCompareBookmarks(Row ABookmark1, Row ABookmark2)
        {
			return FSourceTable.CompareBookmarks(ABookmark1, ABookmark2);
        }

        protected override Row InternalGetKey()
        {
			return FSourceTable.GetKey();
        }

		protected override bool InternalFindKey(Row ARow, bool AForward)
        {
			return FSourceTable.FindKey(ARow, AForward);
        }
        
        protected override void InternalFindNearest(Row ARow)
        {
			FSourceTable.FindNearest(ARow);
        }
        
        protected override bool InternalRefresh(Row ARow)
        {					
			return FSourceTable.Refresh(ARow);
        }

        // ICountable
        protected override int InternalRowCount()
        {
			return FSourceTable.RowCount();
        }
    }
}