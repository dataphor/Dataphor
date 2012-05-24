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
	public class FastoreScan : Table
	{
		public FastoreScan(Program program, Database db, TableNode node)
			: base(node, program)
		{
			_db = db;
            _trivialBOF = true;
		}

		private Database _db;

        public Schema.Order Key;
        public ScanDirection Direction;

        protected bool BufferActive()
        {
            return _bufferFull;
        }

        protected void ClearBuffer()
        {
            _buffer = null;
            _bufferIndex = -1;
            _bufferFull = false;
           // _sourceCursorIndex = SourceCursorIndexUnknown;
        }

        protected DataSet _buffer;
        protected int _bufferIndex = -1;
        protected bool _bufferFull;
        //protected bool FBufferFirst;

        /// <summary> Index of FCursor relative to the buffer or CSourceCursorIndexUnknown if unknown. </summary>
        protected int _sourceCursorIndex;

        protected bool _trivialBOF;
        protected bool _serverEOF;
        protected bool _serverBOF = true;
        // TrivialEOF unneccesary because Last returns flags

        private Alphora.Fastore.Client.Order[] GetFastoreOrders()
        {
            if (Key == null || Key.Columns.Count == 0)
                return new Alphora.Fastore.Client.Order[0];
            else
            {
                Alphora.Fastore.Client.Order order = new Alphora.Fastore.Client.Order();
                order.ColumnID = Key.Columns[0].Column.ID;
                order.Ascending = Direction == ScanDirection.Forward;

                return new Alphora.Fastore.Client.Order[] { order };
            }
        }

        private int[] TableNodeColumns()
        {
            List<int> ids = new List<int>();
            foreach (var col in Node.DataType.Columns)
            {
                ids.Add(col.ID);
            }

            return ids.ToArray();
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
            object[] managedRow = _buffer[_bufferIndex];

            NativeRow nRow = new NativeRow(Node.TableVar.Columns.Count);
            for (int i = 0; i < Node.TableVar.Columns.Count; i++)
            {
                nRow.Values[i] = managedRow[i];
            }

            Row localRow = new Row(Manager, Node.TableVar.DataType.RowType, nRow);

            localRow.CopyTo(row);
        }

        protected void SourceFetch(bool isFirst)
        {
            SourceFetch(isFirst, new Range[0]);
        }

        protected void SourceFetch(bool isFirst, Range[] ranges, object startId = null)
        {

            _buffer = _db.GetRange(TableNodeColumns(), GetFastoreOrders(), ranges, startId);

            if ((_buffer.Count > 0) && !isFirst)
                _bufferIndex = 0;
            else
                _bufferIndex = -1;

            //TODO: Backwards Navigable will need better logic...
            _serverBOF = false;
            _serverEOF = _buffer.EndOfRange;
            _bufferFull = true;
        }

		protected override bool InternalNext()
		{
            if (!BufferActive())
                SourceFetch(SourceBOF());

            if (_bufferIndex >= _buffer.Count - 1)
            {
                if (SourceEOF())
                {
                    _bufferIndex++;
                    return false;
                }

                Range range = CreateRange();
                object rowID = _buffer[_bufferIndex][_buffer[_bufferIndex].Length - 1];

                ClearBuffer();
                SourceFetch(false, new Range[] { range }, rowID);
                return !EOF();
            }

            _bufferIndex++;
            return true;            
		}

        protected Range CreateRange()
        {           
            Range range = new Range();
            int columnID;
            //If no order is present, use the first column in the TableType for resume
            if (Key == null || Key.Columns.Count == 0)
            {
                columnID = Node.TableVar.Columns[0].Column.ID;
            }
            //Otherwise, use the order column to range by
            else
            {
                columnID = Order.Columns[0].Column.ID;
                int idindex = 0;

                //Find which column in the dataset corresponds to the order column (ordercolumn must equal rangecolumn if both are present)
                for (int i = 0; i < Node.TableVar.Columns.Count; i++)
                {
                    if (Node.TableVar.Columns[i].Column.ID == columnID)
                    {
                        idindex = i;
                        break;
                    }
                }       
                
                RangeBound start = new RangeBound();
                start.Bound = _buffer[_buffer.Count - 1][idindex];
                start.Inclusive = true;

                range.Start = start;
            }           

            range.ColumnID = columnID;

            return range;
        }

		protected override bool InternalBOF()
		{
            if (BufferActive())
                return SourceBOF() && ((_buffer.Count == 0) || (_bufferIndex < 0));
            else
                return SourceBOF();
		}

        protected bool SourceBOF()
        {
            return _trivialBOF || _serverBOF;
        }

		protected override bool InternalEOF()
		{
            if (BufferActive())
                return SourceEOF() && ((_buffer.Count == 0) || (_bufferIndex >= _buffer.Count));
            else
                return SourceEOF();
		}

        protected bool SourceEOF()
        {
            return _serverEOF;
        }

        protected override bool InternalFindKey(Row row, bool forward)
        {
            return base.InternalFindKey(row, forward);
        }

        protected override void InternalFindNearest(Row row)
        {
            base.InternalFindNearest(row);
        }
	}

#if USETYPEDLIST
    public class LocalRows : DisposableTypedList
    {
		public LocalRows() : base(typeof(LocalRow), true, false){}

		public new LocalRow this[int AIndex]
		{
			get { return (LocalRow)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
#else
    public class LocalRows : DisposableList<Row>
    {
#endif
    }
}