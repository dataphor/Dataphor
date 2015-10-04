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

    public class RenameTable : Table
    {
		public RenameTable(RenameNode node, Program program) : base(node, program){}

        public new RenameNode Node { get { return (RenameNode)_node; } }
        
		protected ITable _sourceTable;
		protected IRow _sourceRow;
		protected Schema.RowType _keyRowType;
        
        protected override void InternalOpen()
        {
			_sourceTable = (ITable)Node.Nodes[0].Execute(Program);
			_sourceRow = new Row(Manager, _sourceTable.DataType.RowType);
        }
        
        protected override void InternalClose()
        {
            if (_sourceRow != null)
            {
				_sourceRow.Dispose();
                _sourceRow = null;
            }
            
			if (_sourceTable != null)
			{
				_sourceTable.Dispose();
				_sourceTable = null;
			}
        }
        
        protected override void InternalReset()
        {
            _sourceTable.Reset();
        }
        
        protected override void InternalSelect(IRow row)
        {
			// alternative rename algorithm could construct a row of the source type first
			_sourceTable.Select(_sourceRow);
            
            int columnIndex;
            for (int index = 0; index < row.DataType.Columns.Count; index++)
			{
				columnIndex = DataType.Columns.IndexOfName(row.DataType.Columns[index].Name);
				if (columnIndex >= 0)
					if (_sourceRow.HasValue(columnIndex))
						row[index] = _sourceRow[columnIndex];
					else
						row.ClearValue(index);
			}
        }
        
        protected override bool InternalNext()
        {
            return _sourceTable.Next();
        }
        
        protected override void InternalLast()
        {
			_sourceTable.Last();
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
        
        protected override IRow InternalGetKey()
        {
			IRow key = _sourceTable.GetKey();
			try
			{
				if (_keyRowType == null)
				{
					_keyRowType = new Schema.RowType();
					for (int index = 0; index < key.DataType.Columns.Count; index++)
						_keyRowType.Columns.Add(DataType.Columns[_sourceTable.DataType.Columns.IndexOfName(key.DataType.Columns[index].Name)].Copy());
				}
				Row row = new Row(Manager, _keyRowType);
				for (int index = 0; index < key.DataType.Columns.Count; index++)
					if (key.HasValue(index))
						row[index] = key[index];
				return row;
			}
			finally
			{
				key.Dispose();
			}
        }
        
        protected Row BuildSourceKey(IRow key)
        {
			Schema.RowType rowType = new Schema.RowType();
			for (int index = 0; index < key.DataType.Columns.Count; index++)
				rowType.Columns.Add(_sourceTable.DataType.Columns[DataType.Columns.IndexOfName(key.DataType.Columns[index].Name)].Copy());
			Row localKey = new Row(Manager, rowType);
			for (int index = 0; index < key.DataType.Columns.Count; index++)
				if (key.HasValue(index))
					localKey[index] = key[index];
			return localKey;
        }

		protected override bool InternalRefresh(IRow key)
		{
			IRow row = BuildSourceKey(key);
			try
			{
				return _sourceTable.Refresh(row);
			}
			finally
			{
				row.Dispose();
			}
		}

		protected override bool InternalFindKey(IRow key, bool forward)
        {
			IRow localKey = BuildSourceKey(key);
			try
			{
				return _sourceTable.FindKey(localKey, forward);
			}
			finally
			{
				localKey.Dispose();
			}
				
        }
        
        protected override void InternalFindNearest(IRow key)
        {
			IRow localKey = BuildSourceKey(key);
			try
			{
				_sourceTable.FindNearest(localKey);
			}
			finally
			{
				localKey.Dispose();
			}
        }
    }
}