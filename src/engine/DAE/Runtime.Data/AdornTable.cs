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

    public class AdornTable : Table
    {
        public AdornTable(AdornNode node, Program program) : base(node, program){}
        
        public new AdornNode Node { get { return (AdornNode)_node; } }
        
		protected ITable _sourceTable;
		protected IRow _sourceRow;
		protected bool _bOF;
        
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
				throw;
			}
			_bOF = true;
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
            _bOF = true;
        }
        
        protected override void InternalSelect(IRow row)
        {
            _sourceTable.Select(row);
        }
        
        protected override bool InternalNext()
        {
			if (_sourceTable.Next())
			{
				_bOF = false;
				return true;
			}
			_bOF = _sourceTable.BOF();
			return false;
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
				if (_sourceTable.EOF())
					return true;
				else
				{
					if (_sourceTable.Supports(CursorCapability.BackwardsNavigable))
						_sourceTable.First();
					else
						_sourceTable.Reset();
					_bOF = true;
					return false;
				}
			}
			return _sourceTable.EOF();
        }
    }
}