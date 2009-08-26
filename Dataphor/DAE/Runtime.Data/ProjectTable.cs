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
        public ProjectTable(ProjectNodeBase ANode, Program AProgram) : base(ANode, AProgram){}
        
        public new ProjectNodeBase Node
        {
            get
            {
                return (ProjectNodeBase)FNode;
            }
        }
        
        // SourceTable
        protected Table FSourceTable;
        protected Row FSourceRow;
        protected Row FCurrentRow;
        protected Row FLastRow;
        
        protected bool FCrack;
        
        // Table Support
        protected override void InternalOpen()
        {
			FSourceTable = (Table)Node.Nodes[0].Execute(Program);
			FSourceRow = new Row(Manager, ((Schema.TableType)Node.DataType).RowType); // Prepare the row on the projected nodes, the select will only fill in what it can
			if (Node.DistinctRequired)
			{
				FCurrentRow = new Row(Manager, Node.DataType.RowType);
				FLastRow = new Row(Manager, Node.DataType.RowType);
				FCrack = true;
			}
        }
        
        protected override void InternalClose()
        {
            if (FSourceTable != null)
            {
				FSourceTable.Dispose();
                FSourceTable = null;
            }
            
            if (FSourceRow != null)
			{
				FSourceRow.Dispose();
                FSourceRow = null;
			}
            
            if (FCurrentRow != null)
			{
				FCurrentRow.Dispose();
				FCurrentRow = null;
			}
            
            if (FLastRow != null)
            {
				FLastRow.Dispose();
				FLastRow = null;
            }
        }
        
        protected override void InternalReset()
        {
            FSourceTable.Reset();
            if (Node.DistinctRequired)
			{
				FCrack = true;
                FLastRow.ClearValues();
            }
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FSourceTable.Select(FSourceRow);
			FSourceRow.CopyTo(ARow);
        }
    
        protected override bool InternalNext()
        {
            if (Node.DistinctRequired)
            {
				while (true)
				{
					if (FSourceTable.Next())
					{
						FSourceTable.Select(FCurrentRow);
						if (FCrack)
						{
							FCrack = false;
							break;
						}
						
						Program.Stack.Push(FCurrentRow);
						try
						{
							Program.Stack.Push(FLastRow);
							try
							{
								object LEqual = Node.EqualNode.Execute(Program);
								if ((LEqual == null) || !(bool)LEqual)
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
						FCrack = true;
						FLastRow.ClearValues();
						return false;
					}
				}

				FCurrentRow.CopyTo(FLastRow);            
				return true;
            }
            else
                return FSourceTable.Next();
        }
        
        protected override void InternalLast()
        {
            FSourceTable.Last();
            if (Node.DistinctRequired)
            {
                FLastRow.ClearValues();
                FCrack = true;
            }
        }
        
        protected override bool InternalBOF()
        {
            return FSourceTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
            return FSourceTable.EOF();
        }
    }
}