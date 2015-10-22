/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Diagnostics;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public class ExtendTable : Table
    {
		public ExtendTable(ExtendNode node, Program program) : base(node, program){}

        public new ExtendNode Node { get { return (ExtendNode)_node; } }
        
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
            _sourceRow.CopyTo(row);
            int index;
            int columnIndex;
	        Program.Stack.Push(_sourceRow);
            try
            {
				for (index = 1; index < Node.Nodes.Count; index++)
				{
					columnIndex = row.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.ExtendColumnOffset + index - 1].Name);
					if (columnIndex >= 0)
					{
						object objectValue = Node.Nodes[index].Execute(Program);
						try
						{
							if (row.DataType.Columns[columnIndex].DataType is Schema.ScalarType)
								objectValue = ValueUtility.ValidateValue(Program, (Schema.ScalarType)row.DataType.Columns[columnIndex].DataType, objectValue);
							row[columnIndex] = objectValue;
						}
						finally
						{
							DataValue.DisposeValue(Program.ValueManager, objectValue);
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
    }
}