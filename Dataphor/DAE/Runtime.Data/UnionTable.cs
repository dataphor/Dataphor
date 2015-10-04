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

    public class UnionTable : Table
    {
		public UnionTable(UnionNode node, Program program) : base(node, program){}

        public new UnionNode Node { get { return (UnionNode)_node; } }
        
		protected ITable _leftTable;
		protected ITable _rightTable;
		protected IRow _sourceRow;
		protected NativeTable _buffer;
		protected Scan _scan;
        
        protected override void InternalOpen()
        {
			_sourceRow = new Row(Manager, Node.DataType.RowType);
			_leftTable = (ITable)Node.Nodes[0].Execute(Program);
			try
			{
				_rightTable = (ITable)Node.Nodes[1].Execute(Program);
			}
			catch
			{
				_leftTable.Dispose();
				throw;
			}
			
			Schema.TableType tableType = new Schema.TableType();
			Schema.BaseTableVar tableVar = new Schema.BaseTableVar(tableType, Program.TempDevice);
			Schema.TableVarColumn newColumn;
			foreach (Schema.TableVarColumn column in _leftTable.Node.TableVar.Columns)
			{
				newColumn = column.Inherit();
				tableType.Columns.Add(column.Column);
				tableVar.Columns.Add(column);
			}
			
			Schema.Key key = new Schema.Key();
			foreach (Schema.TableVarColumn column in Node.TableVar.Keys.MinimumKey(true).Columns)
				key.Columns.Add(tableVar.Columns[column.Name]);
			tableVar.Keys.Add(key);
			
			_buffer = new NativeTable(Manager, tableVar);
			PopulateBuffer();

			_scan = new Scan(Manager, _buffer, _buffer.ClusteredIndex, ScanDirection.Forward, null, null);
			_scan.Open();
        }
        
        protected override void InternalClose()
        {
			if (_scan != null)
			{
				_scan.Dispose();
				_scan = null;
			}
			
			if (_buffer != null)
			{
				_buffer.Drop(Manager);
				_buffer = null;
			}
			
			if (_leftTable != null)
			{
				_leftTable.Dispose();
				_leftTable = null;
			}

			if (_rightTable != null)
			{
				_rightTable.Dispose();
				_rightTable = null;
			}

            if (_sourceRow != null)
            {
				_sourceRow.Dispose();
                _sourceRow = null;
            }
        }
        
        protected void PopulateBuffer()
        {
			while (_leftTable.Next())
			{
				_leftTable.Select(_sourceRow);
				_buffer.Insert(Manager, _sourceRow);
			}
			
			while (_rightTable.Next())
			{
				_rightTable.Select(_sourceRow);
				if (!_buffer.HasRow(Manager, _sourceRow))
					_buffer.Insert(Manager, _sourceRow);
			}
        }
        
        protected override void InternalReset()
        {
			_leftTable.Reset();
			_rightTable.Reset();
			_scan.Close();
			_scan.Dispose();
			_buffer.Truncate(Manager);
			PopulateBuffer();
			_scan = new Scan(Manager, _buffer, _buffer.ClusteredIndex, ScanDirection.Forward, null, null);
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
			//while (Next()); ??
        }
        
        protected override bool InternalBOF()
        {
			return _scan.BOF();
        }
        
        protected override bool InternalEOF()
        {
			return _scan.EOF();
        }
        
        protected override bool InternalPrior()
        {
			return _scan.Prior();
        }
        
        protected override void InternalFirst()
        {
			_scan.First();
        }
    }
}