/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
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
using System.Collections.Generic;
using Alphora.Fastore.Client;


namespace Alphora.Dataphor.DAE.Device.Fastore
{
    //This will have to be a buffered read of the FastoreStorage engine
    public class FastoreCursor : Table
    {
        public const int MaxLimit = 10;

        public FastoreCursor(Program program, Database db, TableNode node)
            : base(node, program)
        {
            _db = db;
        }

        private Database _db;
        public Schema.Order Key;
        public ScanDirection Direction;

        private int[] TableNodeColumns()
        {
            List<int> ids = new List<int>();
            foreach (var col in Node.DataType.Columns)
            {
                ids.Add(col.ID);
            }

            return ids.ToArray();
        }

        protected Range CreateRange()
        {
            //if order is present use it for ordering, if not, attempt to use key. If no key, use the first column.
            int orderColumnId = Node.Order != null && Node.Order.Columns.Count > 0 ? Node.Order.Columns[0].Column.ID :
                (Key != null && Key.Columns.Count > 0) ? Key.Columns[0].Column.ID :
                Node.TableVar.Columns[0].Column.ID;            

            //Find which column in the dataset corresponds to the order column
            int orderColumnIndex = 0;
            for (int i = 0; i < Node.TableVar.Columns.Count; i++)
            {
                if (Node.TableVar.Columns[i].Column.ID == orderColumnId)
                {
                    orderColumnIndex = i;
                    break;
                }
            }

            //if order is forward (asc) and the buffer is forward (getnext), use the last item as resume.
            //if order is forward (asc) and the buffer is backward (getprior), use the first item as resume, and grab in reverse order
            //if order is backward (desc) and the buffer is forward (getnext), use that last item as resume.
            //if order is backward (desc) and the buffer is backward (getprior), use the first item as resume, and grab in reverse order
            //Does the Scan Direction determine the order?
            Range range = new Range();
            range.ColumnID = orderColumnId;      

            range.Ascending = (_bufferDirection == BufferDirection.Forward && Direction == ScanDirection.Forward) ||
              (_bufferDirection == BufferDirection.Backward && Direction == ScanDirection.Backward);  

            if (BufferActive())
            {
                RangeBound bound = new RangeBound();
                bound.Inclusive = true;

                if (range.Ascending)
                {
                    bound.Bound = ((object[])_buffer[_buffer.Count - 1].Row.AsNative)[orderColumnIndex];
                    range.Start = bound;
                }
                else
                {
                    bound.Bound = ((object[])_buffer[0].Row.AsNative)[orderColumnIndex];
                    range.End = bound;
                }
            }         
         
            return range;
        }

        protected LocalBuffer _buffer;
        protected int _bufferIndex = -1;
        protected bool _bufferFull;
        protected BufferDirection _bufferDirection = BufferDirection.Forward;

        protected bool BufferActive()
        {
            return _bufferFull;
        }

        protected void ClearBuffer()
        {
            _buffer.Clear();
            _bufferIndex = -1;
            _bufferFull = false;
        }

        protected void SetBufferDirection(BufferDirection bufferDirection)
        {
            _bufferDirection = bufferDirection;
        }

        protected override void InternalOpen()
        {
            // _set = _db.GetRange(TableNodeColumns(), GetFastoreOrders(), new Range[0]);
        }

        protected override void InternalClose()
        {
            if (BufferActive())
                ClearBuffer();
        }

        // Flags tracks the current status of the remote cursor, BOF, EOF, none, or both
        protected bool _flagsCached;
        protected CursorGetFlags _flags;
        protected bool _trivialBOF;
        // TrivialEOF unnecessary because Last returns flags

        protected void SetFlags(CursorGetFlags flags)
        {
            _flags = flags;
            _flagsCached = true;
            _trivialBOF = false;
        }

        protected CursorGetFlags GetFlags()
        {
            if (!_flagsCached)
            {
                SetFlags(CursorGetFlags.None);
            }
            return _flags;
        }

        public override void Reset()
        {
            if (BufferActive())
                ClearBuffer();
            SetFlags(CursorGetFlags.None);
            SetBufferDirection(BufferDirection.Forward);
        }

        protected override void InternalSelect(Row row)
        {
            if (BufferActive())
                BufferSelect(row);
            else
            {
                SourceFetch(false);
                BufferSelect(row);
            }
        }

        private void BufferSelect(Row row)
        {
            _buffer[_bufferIndex].Row.CopyTo(row);
        }

        protected void SourceFetch(bool isFirst)
        {
            SourceFetch(isFirst, CreateRange());
        }

        protected void SourceFetch(bool isFirst, Range range, object startId = null)
        {
            //Grab data from current connection...
            DataSet _set = _db.GetRange(TableNodeColumns(), range, MaxLimit, startId);
            ProcessFetchData(_set, isFirst);
        }

        public void ProcessFetchData(DataSet fetchData, bool isFirst)
        {
            ClearBuffer();
            _buffer.BufferDirection = _bufferDirection;
            Schema.IRowType rowType = DataType.RowType;
            CursorGetFlags flags = GetCursorFlags(fetchData);

            if (_bufferDirection == BufferDirection.Forward)
            {
                for (int index = 0; index < fetchData.Count; index++)
                {
                    DataSetRow dsrow = fetchData[index];

                    Row row = new Row(Manager, rowType);
                    row.AsNative = dsrow.Values;

                    FastoreRow frow = new FastoreRow(row, dsrow.ID);
                    _buffer.Add(frow);
                }

                if ((fetchData.Count > 0) && !isFirst)
                    _bufferIndex = 0;
                else
                    _bufferIndex = -1;
            }
            else
            {
                for (int index = 0; index < fetchData.Count; index++)
                {
                    DataSetRow dsrow = fetchData[index];

                    Row row = new Row(Manager, rowType);
                    row.AsNative = dsrow.Values;

                    FastoreRow frow = new FastoreRow(row, dsrow.ID);
                    _buffer.Insert(0, frow);
                }

                if ((fetchData.Count > 0) && !isFirst)
                    _bufferIndex = _buffer.Count - 1;
                else
                    _bufferIndex = _buffer.Count;

            }

            SetFlags(flags);
            _bufferFull = true;
        }

        private CursorGetFlags GetCursorFlags(DataSet ds)
        {
            CursorGetFlags flags = CursorGetFlags.None;
            if (ds.Bof)
                flags |= CursorGetFlags.BOF;
            if (ds.Eof)
                flags |= CursorGetFlags.EOF;

            return flags;
        }

        protected override bool InternalNext()
        {
            SetBufferDirection(BufferDirection.Forward);
          
            if (!BufferActive())
                SourceFetch(SourceBOF());

            if (_bufferIndex >= _buffer.Count - 1)
            {
                if (SourceEOF())
                {
                    _bufferIndex++;
                    return false;
                }

                SourceFetch(false);
                return !EOF();
            }
            _bufferIndex++;
            return true;
        }

        protected bool SourceBOF()
        {
            return _trivialBOF || ((GetFlags() & CursorGetFlags.BOF) != 0);
        }

        protected override bool InternalBOF()
        {
            if (BufferActive())
                return SourceBOF() && ((_buffer.Count == 0) || (_bufferIndex < 0));
            else
                return SourceBOF();
        }

        protected bool SourceEOF()
        {
            return (GetFlags() & CursorGetFlags.EOF) != 0;
        }

        protected override bool InternalEOF()
        {
            if (BufferActive())
                return SourceEOF() && ((_buffer.Count == 0) || (_bufferIndex >= _buffer.Count));
            else
                return SourceEOF();
        }

        protected override void InternalInsert(Row oldRow, Row newRow, BitArray valueFlags, bool uncheckedValue)
        {
            object[] nrow = (object[])newRow.AsNative;

            //Generate ID here... (or pick the correct column)

            //_db.Include(TableNodeColumns(), id,nrow);

            if (BufferActive())
                ClearBuffer();
            SetBufferDirection(BufferDirection.Backward);
        }

        protected override void InternalUpdate(Row row, BitArray valueFlags, bool uncheckedValue)
        {
            object id = _buffer[_bufferIndex].ID;
            object[] nrow = (object[])row.AsNative;

            _db.Exclude(TableNodeColumns(), id);
            _db.Include(TableNodeColumns(), id, nrow);

            if (BufferActive())
                ClearBuffer();
            SetBufferDirection(BufferDirection.Backward);
        }

        protected override void InternalDelete(bool uncheckedValue)
        {
            object id = _buffer[_bufferIndex].ID;
            _db.Exclude(TableNodeColumns(), id);

            if (BufferActive())
                ClearBuffer();
            SetBufferDirection(BufferDirection.Backward);
        }

        protected override void  InternalFirst()
        {
            SetBufferDirection(BufferDirection.Forward);
            if (BufferActive())
                ClearBuffer();
        }

        protected override bool  InternalPrior()
        {
            SetBufferDirection(BufferDirection.Backward);
            if (!BufferActive())
                SourceFetch(SourceEOF());

            if (_bufferIndex <= 0)
            {
                if (SourceBOF())
                {
                    _bufferIndex--;
                    return false;
                }

                SourceFetch(false);
                return !BOF();
            }
            _bufferIndex--;
            return true;
        }

        protected override bool InternalFindKey(Row row, bool forward)
        {
            //TODO: Have to figure this out.. We need a way to determine the rowId for row being found
            //and then load the buffer on that row in the correct order. Shouldn't be hard once we know
            //how to pull a row id from a Dataphor row.
            return base.InternalFindKey(row, forward);
        }

        protected override void InternalFindNearest(Row row)
        {
            //TODO: Same as above.
            base.InternalFindNearest(row);
        }
    }
}

public class FastoreRow
{
    public FastoreRow(Row row, object id)
    {
        _row = row;
        _id = id;
    }

    private Row _row;
    public Row Row { get { return _row; } }

    private object _id;
    public object ID { get { return _id; } }
}

public class LocalBuffer : List<FastoreRow>
{
    public BufferDirection BufferDirection { get; set; }
}