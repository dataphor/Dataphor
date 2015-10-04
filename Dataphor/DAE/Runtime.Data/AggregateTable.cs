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
		public AggregateTable(AggregateNode node, Program program) : base(node, program){}

        public new AggregateNode Node { get { return (AggregateNode)_node; } }
        
		protected ITable _sourceTable;
		protected IRow _sourceRow;
        
        protected override void InternalOpen()
        {
			_sourceTable = (ITable)Node.Nodes[0].Execute(Program);
			_sourceRow = new Row(Manager, _sourceTable.DataType.RowType);
        }
        
        protected override void InternalClose()
        {
			if (_sourceTable != null)
			{
				_sourceTable.Dispose();
				_sourceTable = null;
			}
			
            if (_sourceRow != null)
            {
				_sourceRow.Dispose();
                _sourceRow = null;
            }
        }
        
        protected override void InternalReset()
        {
			_sourceTable.Reset();
        }
        
        protected override void InternalSelect(IRow row)
        {
			_sourceTable.Select(_sourceRow);
			_sourceRow.CopyTo(row);
			
			Program.Stack.Push(_sourceRow);
			try
			{
				int nodeIndex;
				int columnIndex;
				for (int index = 0; index < DataType.Columns.Count; index++)
				{
					columnIndex = row.DataType.Columns.IndexOfName(DataType.Columns[index].Name);
					if (columnIndex >= 0)
					{
						nodeIndex = (index - Node.AggregateColumnOffset) + 1;
						if (nodeIndex >= 1)
						{
							row[columnIndex] = Node.Nodes[nodeIndex].Execute(Program);
						}
						else
						{
							if (_sourceRow.HasValue(index))
								row[columnIndex] = _sourceRow[index];
							else
								row.ClearValue(columnIndex);
						}
					}
				}
			}
			finally
			{
				Program.Stack.Pop();
			}
        }
        
        protected override bool InternalNext()
        {
			return _sourceTable.Next();
        }
        
        protected override bool InternalBOF()
        {
			return _sourceTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
			return _sourceTable.EOF();
        }
        
        protected override bool InternalPrior()
        {
			return _sourceTable.Prior();
        }
        
        protected override void InternalFirst()
        {
			_sourceTable.First();
        }
        
        protected override IRow InternalGetBookmark()
        {
			return _sourceTable.GetBookmark();
        }

		protected override bool InternalGotoBookmark(IRow bookmark, bool forward)
        {
			return _sourceTable.GotoBookmark(bookmark, forward);
        }
        
        protected override int InternalCompareBookmarks(IRow bookmark1, IRow bookmark2)
        {
			return _sourceTable.CompareBookmarks(bookmark1, bookmark2);
        }

        protected override IRow InternalGetKey()
        {
			return _sourceTable.GetKey();
        }

		protected override bool InternalFindKey(IRow row, bool forward)
        {
			return _sourceTable.FindKey(row, forward);
        }
        
        protected override void InternalFindNearest(IRow row)
        {
			_sourceTable.FindNearest(row);
        }
        
        protected override bool InternalRefresh(IRow row)
        {					
			return _sourceTable.Refresh(row);
        }

        // ICountable
        protected override int InternalRowCount()
        {
			return _sourceTable.RowCount();
        }
    }
}