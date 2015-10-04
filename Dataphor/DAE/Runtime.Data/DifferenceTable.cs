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

    public abstract class DifferenceTable : Table
    {
		public DifferenceTable(DifferenceNode node, Program program) : base(node, program){}

        public new DifferenceNode Node { get { return (DifferenceNode)_node; } }
        
		protected ITable _leftTable;
		protected ITable _rightTable;
		protected bool _bOF;
        
        protected override void InternalOpen()
        {
			_leftTable = (ITable)Node.Nodes[0].Execute(Program);
			_rightTable = (ITable)Node.Nodes[1].Execute(Program);
			_bOF = true;
        }
        
        protected override void InternalClose()
        {
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
        }
        
        protected override void InternalReset()
        {
			_leftTable.Reset();
			_bOF = true;
        }
        
        protected override void InternalSelect(IRow row)
        {
			_leftTable.Select(row);
        }
        
        protected override void InternalLast()
        {
			_leftTable.Last();
        }
        
        protected override bool InternalBOF()
        {
			return _bOF;
        }
        
        protected override bool InternalEOF()
        {
			if (_bOF)
			{
				InternalNext();
				if (_leftTable.EOF())
					return true;
				else
				{
					if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
						_leftTable.First();
					else
						_leftTable.Reset();
					_bOF = true;
					return false;
				}
			}
			return _leftTable.EOF();
        }
        
        protected override void InternalFirst()
        {
			_leftTable.First();
			_bOF = true;
        }
    }
    
    public class SearchedDifferenceTable : DifferenceTable
    {
		public SearchedDifferenceTable(DifferenceNode node, Program program) : base(node, program) {}
		
		protected Row _keyRow;
		
		protected override void InternalOpen()
		{
			base.InternalOpen();
			_keyRow = new Row(Manager, new Schema.RowType(Node.RightNode.Order.Columns));
		}
		
		protected override void InternalClose()
		{
			if (_keyRow != null)
			{
				_keyRow.Dispose();
				_keyRow = null;
			}
			
			base.InternalClose();
		}
		
		protected override bool InternalNext()
		{
			while (_leftTable.Next())
			{
				_leftTable.Select(_keyRow);
				if (!_rightTable.FindKey(_keyRow))
				{
					_bOF = false;
					return true;
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (_leftTable.Prior())
			{
				_leftTable.Select(_keyRow);
				if (!_rightTable.FindKey(_keyRow))
					return true;
			}
			_bOF = true;
			return false;
		}
    }
    
    public class ScannedDifferenceTable : DifferenceTable
    {
		public ScannedDifferenceTable(DifferenceNode node, Program program) : base(node, program) {}
		
		protected Row _leftRow;
		protected Row _rightRow;

        protected override void InternalOpen()
        {
			base.InternalOpen();
			_leftRow = new Row(Manager, _leftTable.DataType.RowType);
			_rightRow = new Row(Manager, _rightTable.DataType.RowType);
        }
        
        protected override void InternalClose()
        {
            if (_leftRow != null)
            {
				_leftRow.Dispose();
                _leftRow = null;
            }

            if (_rightRow != null)
            {
				_rightRow.Dispose();
                _rightRow = null;
            }

			base.InternalClose();
        }
        
        protected bool RowsEqual()
        {
			Program.Stack.Push(_rightRow);
			try
			{
				Program.Stack.Push(_leftRow);
				try
				{
					object objectValue = Node.EqualNode.Execute(Program);
					return (objectValue != null) && (bool)objectValue;
				}
				finally
				{
					Program.Stack.Pop();
				}
			}
			finally
			{
				Program.Stack.Pop();
			}
        }
        
        protected override bool InternalNext()
        {
			bool found = false;
			bool hasRow;
			while (!found && !_leftTable.EOF())
			{
				_leftTable.Next();
				if (!_leftTable.EOF())
				{
					_leftTable.Select(_leftRow);
					hasRow = false;
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
					while (_rightTable.Next())
					{
						_rightTable.Select(_rightRow);
						if (RowsEqual())
						{
							hasRow = true;
							break;
						}
					}
					if (!hasRow)
					{
						_bOF = false;
						found = true;
					}
				}
			}
			return found;
        }
        
        protected override bool InternalPrior()
        {
			bool found = false;
			bool hasRow;
			while (!found && !_leftTable.BOF())
			{
				_leftTable.Prior();
				if (!_leftTable.BOF())
				{
					_leftTable.Select(_leftRow);
					hasRow = false;
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
					while (_rightTable.Next())
					{
						_rightTable.Select(_rightRow);
						if (RowsEqual())
						{
							hasRow = true;
							break;
						}
					}
					if (!hasRow)
						found = true;
				}
			}
			return found;
        }
    }
}