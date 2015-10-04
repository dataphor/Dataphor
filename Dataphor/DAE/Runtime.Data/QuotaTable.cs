/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

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

    public class QuotaTable : Table
    {
		public QuotaTable(QuotaNode node, Program program) : base(node, program){}
		
		public new QuotaNode Node { get { return (QuotaNode)_node; } }
		
		protected ITable _sourceTable;
		protected int _sourceCount;
		protected int _sourceCounter;
		protected IRow _sourceRow;
		protected IRow _compareRow;
		protected IRow _lastCompareRow;
		protected bool _hasLastCompareRow;
		
        protected override void InternalOpen()
        {
			_sourceTable = (ITable)Node.Nodes[0].Execute(Program);
			try
			{
				_compareRow = new Row(Manager, new Schema.RowType(Node.Order.Columns));
				_lastCompareRow = new Row(Manager, _compareRow.DataType);
				_hasLastCompareRow = false;
				object objectValue = Node.Nodes[1].Execute(Program);
				#if NILPROPOGATION
				_sourceCount = objectValue == null ? 0 : (int)objectValue;
				#else
				if (objectValue == null)
					throw new RuntimeException(RuntimeException.Codes.NilEncountered, Node);
				FSourceCount = (int)objectValue;
				#endif
				_sourceCounter = 0;
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
			
			if (_compareRow != null)
			{
				_compareRow.Dispose();
				_compareRow = null;
			}

			if (_lastCompareRow != null)
			{
				_lastCompareRow.Dispose();
				_lastCompareRow = null;
			}
        }
        
        protected override void InternalReset()
        {
            _sourceTable.Reset();
            _sourceCounter = 0;
            _lastCompareRow.ClearValues();
            _hasLastCompareRow = false;
        }
        
        protected override void InternalSelect(IRow row)
        {
			_sourceTable.Select(row);
        }
        
        protected override bool InternalNext()
        {
			if (!InternalEOF())
			{
	            if (_sourceTable.Next())
	            {
		            _sourceTable.Select(_compareRow);

					if (_hasLastCompareRow)
					{		            
						Program.Stack.Push(_compareRow);
						try
						{
							Program.Stack.Push(_lastCompareRow);
							try
							{
								object result = Node.EqualNode.Execute(Program);
								if ((result == null) || !(bool)result)
								{
									_sourceCounter++;
									_compareRow.CopyTo(_lastCompareRow);
								}
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
					else
					{
						_sourceCounter++;
						_compareRow.CopyTo(_lastCompareRow);
						_hasLastCompareRow = true;
					}
					return !InternalEOF();
		        }
			}
			return false;
        }
        
        protected override bool InternalBOF()
        {
			return (_sourceCount == 0) || (_sourceCounter == 0);
        }
        
        protected override bool InternalEOF()
        {
			return (_sourceCount == 0) || (_sourceCounter > _sourceCount) || (_sourceTable.EOF());
        }
    }
}