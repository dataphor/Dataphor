/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USENAMEDROWVARIABLES

using System;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;

	/*
		The explode table works by maintaining a stack of cursors, one for the root result set defined by the where clause of the explode operator,
		and one for each child cursor encountered while iterating through the result set.
	*/
    public class ExplodeTable : Table
    {
		public ExplodeTable(ExplodeNode node, Program program) : base(node, program){}
		
		public new ExplodeNode Node { get { return (ExplodeNode)_node; } }
		
		// The current set of cursors being used to produce the result set
		protected Stack _sourceTables;
		
		// For every cursor but the root, this stack will contain the parent row variable for that cursor.
		protected Stack _parentRows;
		
		protected IRow _sourceRow;
		protected IRow _targetRow;
		protected Schema.TableType _tableType;
		protected Schema.BaseTableVar _tableVar;
		protected Schema.Order _sequenceOrder;
		protected NativeTable _buffer;
		protected Scan _scan;
		protected ITable _rootTable;
		protected int _sequence;
		protected int _sequenceColumnIndex = -1;
		protected bool _empty;
		
		protected IRow NewParentRow(IRow currentRow)
		{
			#if USENAMEDROWVARIABLES
			Row row = new Row(Manager, currentRow.DataType);
			#else
			Row row = new Row(Manager, new Schema.RowType(ACurrentRow.DataType.Columns, Keywords.Parent));
			#endif
			currentRow.CopyTo(row);
			return row;
		}
		
		protected void PushSourceTable(IRow currentRow)
		{
			if (currentRow == null)
			{
				// Open the root expression, if it is not empty, save it on the cursor stack
				_rootTable = (ITable)Node.Nodes[1].Execute(Program);
				if (!_rootTable.IsEmpty())
					_sourceTables.Push(_rootTable);
				else
					_rootTable.Dispose();
			}
			else
			{
				// Otherwise, use the given row to build a parent row and open a new cursor for the child expression with that row to parameterize it
				IRow parentRow = NewParentRow(currentRow);
				Program.Stack.Push(parentRow);
				try
				{
					ITable table = (ITable)Node.Nodes[2].Execute(Program);
					
					// If it is not empty, save it on the cursor stack, and save the parent row on the parent row stack
					if (!table.IsEmpty())
					{
						_sourceTables.Push(table);
						_parentRows.Push(parentRow);
					}
					else
					{
						table.Dispose();
						parentRow.Dispose();
					}
				}
				finally
				{
					Program.Stack.Pop();
				}
			}
		}
		
		protected void PopSourceTable()
		{
			ITable table = (ITable)_sourceTables.Pop();
			if (table != _rootTable)
				((IRow)_parentRows.Pop()).Dispose();
			else
				_rootTable = null;

			table.Dispose();
		}
		
		protected override void InternalOpen()
		{
			_sourceTables = new Stack(Program.Stack.MaxStackDepth, Program.Stack.MaxCallDepth);
			_sourceTables.PushWindow(0);
			_parentRows = new Stack(Program.Stack.MaxStackDepth, Program.Stack.MaxCallDepth);
			_parentRows.PushWindow(0);
		 	PushSourceTable(null);
			_sourceRow = new Row(Manager, ((TableNode)Node.Nodes[0]).DataType.RowType);
			_tableType = new Schema.TableType();
			_tableVar = new Schema.BaseTableVar(_tableType, Program.TempDevice);
			Schema.TableVarColumn newColumn;
			foreach (Schema.TableVarColumn column in Node.TableVar.Columns)
			{
				newColumn = (Schema.TableVarColumn)column.Copy();
				_tableType.Columns.Add(newColumn.Column);
				_tableVar.Columns.Add(newColumn);
			}

			if (Node.SequenceColumnIndex < 0)
			{
				newColumn = new Schema.TableVarColumn(new Schema.Column(Keywords.Sequence, Program.DataTypes.SystemInteger), Schema.TableVarColumnType.Stored);
				_tableType.Columns.Add(newColumn.Column);
				_tableVar.Columns.Add(newColumn);
				_sequenceColumnIndex = _tableVar.Columns.Count - 1;
			}
			else
				_sequenceColumnIndex = Node.SequenceColumnIndex;

			_targetRow = new Row(Manager, _tableType.RowType);
			Schema.Key key = new Schema.Key();
			key.Columns.Add(_tableVar.Columns[_sequenceColumnIndex]);
			_tableVar.Keys.Add(key);
			_buffer = new NativeTable(Manager, _tableVar);
			_scan = new Scan(Manager, _buffer, _buffer.ClusteredIndex, ScanDirection.Forward, null, null);
			_scan.Open();
			_sequence = 0;
			_empty = false;
			InternalNext();
			_empty = _scan.EOF();
			_scan.First();
		}
		
		protected override void InternalClose()
		{
			while (_sourceTables.Count > 0)
				PopSourceTable();

			if (_sourceRow != null)
			{				
				_sourceRow.Dispose();
				_sourceRow = null;
			}
			
			if (_targetRow != null)
			{
				_targetRow.Dispose();
				_targetRow = null;
			}
			
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

			_tableType = null;
			_sequenceColumnIndex = -1;
		}
		
		protected override void InternalReset()
		{
			while (_sourceTables.Count > 0)
				PopSourceTable();
				
			PushSourceTable(null);

			_scan.Dispose();
			_buffer.Truncate(Manager);
			_scan = new Scan(Manager, _buffer, _buffer.ClusteredIndex, ScanDirection.Forward, null, null);
			_scan.Open();
			_sequence = 0;
			_empty = false;
			InternalNext();
			_empty = _scan.EOF();
			_scan.First();
		}
		
		protected override bool InternalNext()
		{
			if (!_scan.Next())
			{
				ITable table;
				while (_sourceTables.Count > 0)
				{
					// Retrieve the current cursor to be iterated
					table = (ITable)_sourceTables[0];
					bool contextPopped = false;
					bool contextPushed = false;
					if (_sourceTables.Count > 1)
					{
						// Push it's parent row context, if necessary
						Program.Stack.Push(_parentRows.Peek(0));
						contextPushed = true;
					}
					try
					{
						if (table.Next())
						{
							_sequence++;
							table.Select(_sourceRow);
							
							if (contextPushed)
							{
								contextPopped = true;
								Program.Stack.Pop();
							}
							
							_targetRow.ClearValues();
							for (int index = 0; index < _sourceRow.DataType.Columns.Count; index++)
								_targetRow[index] = _sourceRow[index];
							if (Node.LevelColumnIndex >= 0)
								_targetRow[Node.LevelColumnIndex] = _sourceTables.Count;
							if (_sequenceColumnIndex >= 0)
								_targetRow[_sequenceColumnIndex] = _sequence;
								
							_buffer.Insert(Manager, _targetRow);
							if (!_scan.FindKey(_targetRow))
								throw new RuntimeException(RuntimeException.Codes.NewRowNotFound);
							
							// Use the current row to push a new cursor looking for children of this row
							PushSourceTable(_sourceRow);
							return true;
						}
						else
							// The current cursor has been exhausted, pop it.
							PopSourceTable();
					}
					finally
					{
						if (contextPushed && !contextPopped)
							Program.Stack.Pop();
					}
				}
				return false;
			}
			return true;
		}
		
		protected override void InternalSelect(IRow row)
		{
			_scan.GetRow(row);
		}
		
		protected override bool InternalBOF()
		{
			return _scan.BOF() || _empty;
		}
		
		protected override bool InternalEOF()
		{
			return (_scan.EOF() && (_sourceTables.Count == 0)) || _empty;
		}
    }
}