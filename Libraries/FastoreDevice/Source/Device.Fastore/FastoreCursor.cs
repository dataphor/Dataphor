/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;

using Alphora.Fastore.Client;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
	// This cursor should do nothing on InternalOpen. The assumption being that
	// it will most often be used from a ScanTable or SeekTable.
	// If access is attempted prior to a searched find, then open on the unbounded range.
    public class FastoreCursor : Table
    {
        public const int MaxLimit = 500;

        public FastoreCursor(Program program, Database db, TableNode node)
            : base(node, program)
        {
            _db = db;
        }

        public Schema.Order Key;
        public ScanDirection Direction;

        private Database _db;
		private DataSet _buffer;
        private bool _sourceBof;
        private bool _sourceEof;
		private int _bufferIndex = -1; // current index of the cursor in the result set
		private ScanDirection _bufferDirection; // indicates the order of rows in the result set

		private int[] _tableNodeColumns;
        private int[] TableNodeColumns()
        {
			if (_tableNodeColumns == null)
			{
				_tableNodeColumns = new int[Node.TableVar.Columns.Count];
				for (int index = 0; index < _tableNodeColumns.Length; index++)
					_tableNodeColumns[index] = Node.TableVar.Columns[index].ID;
			}

			return _tableNodeColumns;
        }

		private Schema.TableVarColumn _orderColumn;
		protected Schema.TableVarColumn GetOrderColumn()
		{
			if (_orderColumn == null)
			{
				//if order is present use it for ordering, if not, attempt to use key. If no key, use the first column.
				_orderColumn =
					Node.Order != null && Node.Order.Columns.Count > 0 
						? Node.TableVar.Columns[Node.TableVar.Columns.IndexOfName(Node.Order.Columns[0].Column.Name)]
						:
							(Key != null && Key.Columns.Count > 0) 
								? Key.Columns[0].Column
								: Node.TableVar.Columns[0];            
			}

			return _orderColumn;
		}

		protected int GetOrderColumnID()
		{
			return GetOrderColumn().ID;
		}

        protected Range CreateRange(ScanDirection direction, bool isFirst)
        {
            if (!isFirst)
            {
                RangeBound bound = CreateRangeBound(direction);
                if (direction == ScanDirection.Forward)
                {
                    return CreateRange(direction, bound, null);
                }
                else
                {
                    return CreateRange(direction, null, bound);
                }
            }
            else
                return CreateRange(direction, null, null);
        }

          protected RangeBound CreateRangeBound(ScanDirection direction)
        {
            RangeBound rangeBound = new RangeBound();
            rangeBound.Bound = GetBoundValue(direction);
            rangeBound.Inclusive = true;
            return rangeBound;
        }

        protected object GetBoundValue(ScanDirection direction)
        {
            int orderColumnIndex = Node.TableVar.Columns.IndexOfName(GetOrderColumn().Name);

            if (direction == _bufferDirection)
            {
                return _buffer[_buffer.Count - 1].Values[orderColumnIndex];
            }
            else
            {
                return _buffer[0].Values[orderColumnIndex];
            }
        }

        protected Range CreateRange(ScanDirection direction, RangeBound? start, RangeBound? end)
        {
            Range range = new Range();
            range.ColumnID = GetOrderColumnID();      

            range.Ascending = direction == ScanDirection.Forward; 
			range.Start = start;
			range.End = end;
         
            return range;
        }

        protected bool BufferActive()
        {
			return _buffer != null;
        }

        protected void ClearBuffer()
        {
			_buffer = null;
			_bufferIndex = -1;
        }

        protected override void InternalOpen()
        {
			// Do nothing on open
        }

        protected override void InternalClose()
        {
            if (BufferActive())
                ClearBuffer();
        }

        public override void Reset()
        {
            if (BufferActive())
                ClearBuffer();
        }

        protected override void InternalSelect(Row row)
        {
            if (BufferActive())
                BufferSelect(row);
            else
            {
                SourceFetch(true, Direction);
                BufferSelect(row);
            }
        }

        private void BufferSelect(Row row)
        {
			// TODO: This needs to be aware that row may not be of an equivalent type
            object[] managedRow = _buffer[_bufferIndex].Values;            
            for (int i = 0; i < row.DataType.Columns.Count; i++)
            {
                var index = Node.TableVar.Columns.IndexOfName(row.DataType.Columns[i].Name);
                ((NativeRow)row.AsNative).Values[i] = managedRow[index];
            }
        }

		protected object GetStartId(ScanDirection direction)
		{
			// if current buffer direction is forward and a forward range is requested, use the row Id of the last row in the buffer
			// If current buffer direction is forward and a backward range is requested, use the row Id of the first row in the buffer
			// If current buffer direction is backward and a forward range is requested, use the row Id of the first row in the buffer
			// If current buffer direction is backward and a backward range is requested, use the row Id of the last row in the buffer
			if (_bufferDirection == direction)
				return _buffer[_buffer.Count - 1].ID;
			else
				return _buffer[0].ID;
		}

        protected void SetSourceFlags(RangeSet set)
        {
            _sourceBof = set.Bof;
            _sourceEof = set.Eof;
        }

        protected void SourceFetch(bool isFirst, ScanDirection direction)
        {
            SourceFetch
            (
                isFirst, 
                direction,
                CreateRange(direction, isFirst),
                !isFirst ? GetStartId(direction) : null
            );
        }      

        protected void SourceFetch(bool isFirst, ScanDirection direction, Range range, object startId = null)
        {
            //Grab data from current connection...
            var set = _db.GetRange(TableNodeColumns(), range, MaxLimit, startId);
            ProcessFetchData(set, isFirst, direction);
        }

        public void ProcessFetchData(RangeSet fetchData, bool isFirst, ScanDirection direction)
        {
            ClearBuffer();
            SetSourceFlags(fetchData);
			_buffer = fetchData.Data;
			_bufferDirection = direction;
			if (Direction == _bufferDirection)
			{
				if (_buffer.Count > 0 && !isFirst)
					_bufferIndex = 0;
				else
					_bufferIndex = -1;
			}
			else
			{
				if (_buffer.Count > 0 && !isFirst)
					_bufferIndex = _buffer.Count - 1;
				else
					_bufferIndex = _buffer.Count;
			}
        }

        protected bool SourceBOF()
        {
			return _sourceBof;
        }

        protected override bool InternalBOF()
        {
			if (!BufferActive())
				SourceFetch(true, Direction);

			return 
				Direction == _bufferDirection 
					? (SourceBOF() && ((_buffer.Count == 0) || (_bufferIndex < 0)))
					: (SourceEOF() && ((_buffer.Count == 0) || (_bufferIndex >= _buffer.Count)));
        }

        protected bool SourceEOF()
        {
			return _sourceEof;
        }

        protected override bool InternalEOF()
        {
			if (!BufferActive())
				SourceFetch(true, Direction);

			return
				Direction == _bufferDirection
					? (SourceEOF() && ((_buffer.Count == 0) || (_bufferIndex >= _buffer.Count)))
					: (SourceBOF() && ((_buffer.Count == 0) || (_bufferIndex < 0)));
        }

        protected override bool InternalNext()
        {
            if (!BufferActive())
                SourceFetch(true, Direction);

			if (Direction == _bufferDirection)
			{
				if (_bufferIndex >= _buffer.Count - 1)
				{
                    if ((Direction == ScanDirection.Forward && SourceEOF()) || (Direction == ScanDirection.Backward && SourceBOF()))
					{
						if (_bufferIndex == _buffer.Count - 1)
							_bufferIndex++;
						return false;
					}

					SourceFetch(false, Direction);
					return !EOF();
				}
				_bufferIndex++;
				return true;
			}
			else
			{
				if (_bufferIndex <= 0)
				{
                    if ((Direction == ScanDirection.Forward && SourceEOF()) || (Direction == ScanDirection.Backward && SourceBOF()))
					{
						if (_bufferIndex == 0)
							_bufferIndex--;
						return false;
					}

					SourceFetch(false, Direction);
					return !EOF();
				}
				_bufferIndex--;
				return true;
			}
        }

        protected override bool InternalPrior()
        {
			if (!BufferActive())
				SourceFetch(true, Direction);

			if (Direction == _bufferDirection)
			{
				if (_bufferIndex <= 0)
				{
					if ((Direction == ScanDirection.Backward && SourceBOF()) || (Direction == ScanDirection.Forward && SourceEOF()))
					{
						if (_bufferIndex == 0)
							_bufferIndex--;
						return false;
					}

					SourceFetch(false, Direction == ScanDirection.Forward ? ScanDirection.Backward : ScanDirection.Forward);
					return !BOF();
				}
				_bufferIndex--;
				return true;
			}
			else
			{
				if (_bufferIndex >= _buffer.Count - 1)
				{
					if ((Direction == ScanDirection.Backward && SourceBOF()) || (Direction == ScanDirection.Forward && SourceEOF()))
					{
						if (_bufferIndex == _buffer.Count - 1)
							_bufferIndex++;
						return false;
					}

					SourceFetch(false, _bufferDirection);
					return !BOF();
				}
				_bufferIndex++;
				return true;
			}
        }

		protected override void InternalLast()
		{
			ClearBuffer();
			SourceFetch(true, Direction == ScanDirection.Forward ? ScanDirection.Backward : ScanDirection.Forward);
		}

        protected override void InternalFirst()
        {
			ClearBuffer();
            SourceFetch(true, Direction == ScanDirection.Forward ? ScanDirection.Forward : ScanDirection.Backward);
        }

        protected override bool InternalFindKey(Row row, bool forward)
        {
			var keyRow = EnsureKeyRow(row);
			try
			{
				// TODO: Expand this to work with multiple column searches
				var rangeBound = new RangeBound { Bound = keyRow[0], Inclusive = true };
				var direction = (Direction == ScanDirection.Forward && forward || Direction == ScanDirection.Backward && !forward) ? ScanDirection.Forward : ScanDirection.Backward;
				var range = CreateRange(direction, rangeBound, null);
				var set = _db.GetRange(TableNodeColumns(), range, MaxLimit);
				if (set.Data.Count > 0)
				{  
					ProcessFetchData(set, true, direction);
					return true; 
				}
				return false;
			}
			finally
			{
				if (!object.ReferenceEquals(row, keyRow))
					keyRow.Dispose();
			}
        }

        protected override Row InternalGetKey()
        {
            Row row = new Row(Manager, new RowType(Node.Order.Columns));
            InternalSelect(row);
            return row;
        }

        protected override void InternalFindNearest(Row row)
        {
			var keyRow = EnsurePartialKeyRow(row);
			try
			{
				ClearBuffer();
				if (keyRow != null)
				{
					// TODO: Expand this to work with multiple column searches
					var rangeBound = new RangeBound { Bound = keyRow[0], Inclusive = true };
					var range = CreateRange(Direction, rangeBound, null);
					SourceFetch(true, Direction, range);
				}
			}
			finally
			{
				if (keyRow != null && !object.ReferenceEquals(row, keyRow))
					keyRow.Dispose();
			}
        }

        protected override void InternalInsert(Row oldRow, Row newRow, BitArray valueFlags, bool uncheckedValue)
        {
            object[] nrow = (object[])newRow.AsNative;

            //Generate ID here... (or pick the correct column)

            //_db.Include(TableNodeColumns(), id,nrow);

            if (BufferActive())
                ClearBuffer();
        }

        protected override void InternalUpdate(Row row, BitArray valueFlags, bool uncheckedValue)
        {
            object id = _buffer[_bufferIndex].ID;
            object[] nrow = (object[])row.AsNative;

            _db.Exclude(TableNodeColumns(), id);
            _db.Include(TableNodeColumns(), id, nrow);

            if (BufferActive())
                ClearBuffer();
        }

        protected override void InternalDelete(bool uncheckedValue)
        {
            object id = _buffer[_bufferIndex].ID;
            _db.Exclude(TableNodeColumns(), id);

            if (BufferActive())
                ClearBuffer();
        }
    }
}