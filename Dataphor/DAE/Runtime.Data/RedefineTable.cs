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

    public class RedefineTable : Table
    {
		public RedefineTable(RedefineNode node, Program program) : base(node, program){}

        public new RedefineNode Node { get { return (RedefineNode)_node; } }
        
		protected ITable _sourceTable;
		protected IRow _sourceRow;
        
        protected override void InternalOpen()
        {
			_sourceTable = (ITable)Node.Nodes[0].Execute(Program);
			try
			{
				_sourceRow = new Row(Manager, _sourceTable.DataType.RowType);
			}
			catch
			{
				_sourceTable.Dispose();
				_sourceTable = null;
				throw;
			}
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

			int columnIndex;            
            for (int index = 0; index < Node.DataType.Columns.Count; index++)
            {
				if (!((IList)Node.RedefineColumnOffsets).Contains(index))
				{
					columnIndex = row.DataType.Columns.IndexOfName(DataType.Columns[index].Name);
					if (columnIndex >= 0)
						if (_sourceRow.HasValue(index))
							row[columnIndex] = _sourceRow[index];
						else
							row.ClearValue(columnIndex);
				}
            }

	        Program.Stack.Push(_sourceRow);
            try
            {
				for (int index = 0; index < Node.RedefineColumnOffsets.Length; index++)
				{
					columnIndex = row.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RedefineColumnOffsets[index]].Name);
					if (columnIndex >= 0)
					{
						row[columnIndex] = Node.Nodes[index + 1].Execute(Program);
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
    }
}