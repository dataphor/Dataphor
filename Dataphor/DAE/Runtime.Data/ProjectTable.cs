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

    /// <remarks> 
    ///		ProjectTable expects its source to be ordered by the projection columns if it is required
    ///		to be distinct.  The compiler will ensure this is the case.
    /// </remarks>    
    public class ProjectTable : Table
    {
        public ProjectTable(ProjectNodeBase node, Program program) : base(node, program){}
        
        public new ProjectNodeBase Node
        {
            get
            {
                return (ProjectNodeBase)_node;
            }
        }
        
        // SourceTable
        protected Table _sourceTable;
        protected Row _sourceRow;
        protected Row _currentRow;
        protected Row _lastRow;
        
        protected bool _crack;
        
        // Table Support
        protected override void InternalOpen()
        {
			_sourceTable = (Table)Node.Nodes[0].Execute(Program);
			_sourceRow = new Row(Manager, ((Schema.TableType)Node.DataType).RowType); // Prepare the row on the projected nodes, the select will only fill in what it can
			if (Node.DistinctRequired)
			{
				_currentRow = new Row(Manager, Node.DataType.RowType);
				_lastRow = new Row(Manager, Node.DataType.RowType);
				_crack = true;
			}
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
            
            if (_currentRow != null)
			{
				_currentRow.Dispose();
				_currentRow = null;
			}
            
            if (_lastRow != null)
            {
				_lastRow.Dispose();
				_lastRow = null;
            }
        }
        
        protected override void InternalReset()
        {
            _sourceTable.Reset();
            if (Node.DistinctRequired)
			{
				_crack = true;
                _lastRow.ClearValues();
            }
        }
        
        protected override void InternalSelect(IRow row)
        {
			_sourceTable.Select(_sourceRow);
			_sourceRow.CopyTo(row);
        }
    
        protected override bool InternalNext()
        {
            if (Node.DistinctRequired)
            {
				while (true)
				{
					if (_sourceTable.Next())
					{
						_sourceTable.Select(_currentRow);
						if (_crack)
						{
							_crack = false;
							break;
						}
						
						Program.Stack.Push(_currentRow);
						try
						{
							Program.Stack.Push(_lastRow);
							try
							{
								object equal = Node.EqualNode.Execute(Program);
								if ((equal == null) || !(bool)equal)
									break;
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
						_crack = true;
						_lastRow.ClearValues();
						return false;
					}
				}

				_currentRow.CopyTo(_lastRow);            
				return true;
            }
            else
                return _sourceTable.Next();
        }
        
        protected override void InternalLast()
        {
            _sourceTable.Last();
            if (Node.DistinctRequired)
            {
                _lastRow.ClearValues();
                _crack = true;
            }
        }
        
        protected override bool InternalBOF()
        {
            return _sourceTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
            return _sourceTable.EOF();
        }
    }
}