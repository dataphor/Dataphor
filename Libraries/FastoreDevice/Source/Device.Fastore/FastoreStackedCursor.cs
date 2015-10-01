using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Fastore.Client;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
    class FastoreStackedCursor : Table
    {
        public FastoreStackedCursor(Program program, Database db, Order order, Order key, TableNode node)
            :base(node, program)
        {
            if (order.Columns.Count == 1)
                throw new Exception("Use stacked cursor only when order consists of more than one column");

            Stack(program, db, order, key, node);
        }

        protected Table _nestedTable;
        protected Order _order;
        protected Order _nestedOrder;
        protected Order _key;

        protected ScanDirection _direction;

        List<Row> _buffer;
        int _bufferIndex;

        RowComparer _nestedComparer;
        RowComparer _comparer;

        private void Stack(Program program, Database db, Order order, Order key, TableNode node)
        {
            _key = key;

            OrderColumn column = order.Columns[order.Columns.Count - 1];
            _direction = column.Ascending ? ScanDirection.Forward : ScanDirection.Backward;

            _order = new Order();
            _order.Columns.Add(column);

            _nestedOrder = new Order();
            for (int i = 0; i < order.Columns.Count - 1; i++ )
            {
                _nestedOrder.Columns.Add(order.Columns[i]);
            }

            _nestedComparer = new RowComparer(_nestedOrder.Columns[_nestedOrder.Columns.Count - 1], Program.ValueManager);
            _comparer = new RowComparer(_order.Columns[0], Program.ValueManager);

            if (_nestedOrder.Columns.Count == 1)
            {
                FastoreCursor scan = new FastoreCursor(program, db, node);
                scan.Key = key;
                scan.Direction = _nestedOrder.Columns[0].Ascending ? ScanDirection.Forward : ScanDirection.Backward;
                scan.Node.Order = _nestedOrder;
                _nestedTable = scan;
            }
            else
            {
                _nestedTable = new FastoreStackedCursor(program, db, _nestedOrder, key, node);
            }
        }        

        protected bool BufferActive()
        {
            return _buffer != null;
        }

        protected void ClearBuffer()
        {
            _buffer = null;
        }

        protected override void InternalOpen()
        {
            _nestedTable.Open();
        }

        protected override void InternalClose()
        {
            if (BufferActive())
                ClearBuffer();

            _nestedTable.Close();
        }

        public override void Reset()
        {
            if (BufferActive())
                ClearBuffer();

            _nestedTable.Reset();
        }

        protected void GetGroup(bool isFirst, ScanDirection direction)
        {
            _buffer = new List<Row>();  
      
            if (direction == ScanDirection.Forward)
            {
                if (isFirst)
                {
                    _nestedTable.First();
                }
                else
                {
                    _nestedTable.Prior();
                }

                while (_nestedTable.Next())
                {
                    var row = new Row(Program.ValueManager, Node.TableVar.DataType.RowType);
                    _nestedTable.Select(row);
                    if (_buffer.Count == 0)
                    {
                        _buffer.Add(row);
                    }
                    else if (_nestedComparer.Compare(_buffer[0], row) == 0)
                    {
                        _buffer.Add(row);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                if (isFirst)
                    _nestedTable.Last();

                while (_nestedTable.Prior())
                {
                    var row = new Row(Program.ValueManager, Node.TableVar.DataType.RowType);
                    _nestedTable.Select(row);
                    if (_buffer.Count == 0)
                    {
                        _buffer.Add(row);
                    }
                    else if (_nestedComparer.Compare(_buffer[0], row) == 0)
                    {
                        _buffer.Add(row);
                    }
                    else
                    {
                        break;
                    }
                }

                _nestedTable.Next();
            }

            _buffer.Sort(_comparer);

            if (isFirst)
            {
                if (direction == ScanDirection.Forward)
                    _bufferIndex = -1;
                else
                    _bufferIndex = _buffer.Count;
            }
            else
            {
                if (direction == ScanDirection.Forward)
                    _bufferIndex = 0;
                else
                    _bufferIndex = _buffer.Count - 1;
            }
        }

        protected override void InternalSelect(IRow row)
        {
            if (BufferActive())
                BufferSelect(row);
            else
            {
                GetGroup(true, ScanDirection.Forward);
                BufferSelect(row);
            }
        }

        private void BufferSelect(IRow row)
        {
            var bufferrow = _buffer[_bufferIndex];
            bufferrow.CopyTo(row);
        }

        protected override bool InternalBOF()
        {
            return _nestedTable.BOF();
        }

        protected override bool InternalEOF()
        {
            return _nestedTable.EOF();
        }

        protected override bool InternalNext()
        {
            if (!BufferActive())
                GetGroup(true, ScanDirection.Forward);

            if (_bufferIndex >= _buffer.Count() - 1)
            {
                if (EOF())
                {
                    _bufferIndex++;
                    return false;
                }
                else
                {
                    GetGroup(false, ScanDirection.Forward);
                    return !EOF();
                }
            }

            _bufferIndex++;
            return true;
        }

        protected override bool InternalPrior()
        {
            if (!BufferActive())
                GetGroup(true, ScanDirection.Backward);

            if (_bufferIndex <= 0)
            {
                if (BOF())
                {
                    _bufferIndex--;
                    return false;
                }
                else
                {
                    GetGroup(false, ScanDirection.Backward);
                    return !BOF();
                }
            }

            _bufferIndex--;
            return true;
        }

        protected override void InternalLast()
        {
            ClearBuffer();
            _nestedTable.Last();
            GetGroup(true, ScanDirection.Backward);
        }

        protected override void InternalFirst()
        {
            ClearBuffer();
            _nestedTable.First();
            GetGroup(true, ScanDirection.Forward);
        }

        protected override bool InternalFindKey(IRow row, bool forward)
        {
            ClearBuffer();
            var result = _nestedTable.FindKey(row, forward);
            if (result)
            {
                GetGroup(false, forward ? ScanDirection.Forward : ScanDirection.Backward);
                SeekBuffer(row);
            }

            return result;
        }

        protected override IRow InternalGetKey()
        {
            if (!BufferActive())
                GetGroup(false, ScanDirection.Forward);

            //Should be the outermost stack if we are here, and thus have a complete order.
            Row row = new Row(Manager, new RowType(InternalGetOrder().Columns));
            InternalSelect(row);
            return row;
        }

        protected override bool InternalRefresh(IRow row)
        {
            InternalReset();
            if (row != null)
            {
                bool result = InternalFindKey(row, true);
                if (!result)
                    InternalFindNearest(row);
                return result;
            }
            return false;
        }

        protected override Order InternalGetOrder()
        {
            var order = new Order(_nestedOrder);
            order.Columns.Add(_order.Columns[0]);
            return order;
        }

        protected override void InternalFindNearest(IRow row)
        {
            ClearBuffer();
            _nestedTable.FindNearest(row);
            GetGroup(false, ScanDirection.Forward);
            SeekBuffer(row);
        }     

        protected void SeekBuffer(IRow row)
        {
            //Find matching value;
            if (_buffer.Count > 0)
            {
                int lo = 0;
                int hi = _buffer.Count - 1;
                int split = 0;
                int result = -1;

                while (lo <= hi)
                {
                    split = (lo + hi) >> 1;
                    result = _comparer.Compare(row, _buffer[split]);

                    if (result == 0)
                    {
                        lo = split;
                        break;
                    }
                    else if (result < 0)
                        hi = split - 1;
                    else
                        lo = split + 1;
                }

                _bufferIndex = lo;

                //Go to beginning of matching values.
                while (_bufferIndex > 0 && _comparer.Compare(row, _buffer[_bufferIndex -1]) == 0) { --_bufferIndex; }
            }
        }
    }

    class RowComparer : IComparer<IRow>
    {
        public RowComparer(OrderColumn column, IValueManager manager)
        {
            _order = column;
            _manager = manager;
        }

        private OrderColumn _order;
        private IValueManager _manager;

        public int Compare(IRow x, IRow y)
        {
            return _manager.EvaluateSort(_order, x[_order.Column.Name], y[_order.Column.Name]);
        }
    }
}
