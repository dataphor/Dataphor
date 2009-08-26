/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Diagnostics;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public class ExtendTable : Table
    {
		public ExtendTable(ExtendNode ANode, Program AProgram) : base(ANode, AProgram){}

        public new ExtendNode Node { get { return (ExtendNode)FNode; } }
        
		protected Table FSourceTable;
		protected Row FSourceRow;
        
        protected override void InternalOpen()
        {
			FSourceTable = Node.Nodes[0].Execute(Program) as Table;
			try
			{
				FSourceRow = new Row(Manager, FSourceTable.DataType.RowType);
			}
			catch
			{
				FSourceTable.Dispose();
				FSourceTable = null;
				throw;
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
        }
        
        protected override void InternalReset()
        {
            FSourceTable.Reset();
        }
        
        protected override void InternalSelect(Row ARow)
        {
            FSourceTable.Select(FSourceRow);
            FSourceRow.CopyTo(ARow);
            int LIndex;
            int LColumnIndex;
	        Program.Stack.Push(FSourceRow);
            try
            {
				for (LIndex = 1; LIndex < Node.Nodes.Count; LIndex++)
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.ExtendColumnOffset + LIndex - 1].Name);
					if (LColumnIndex >= 0)
					{
						object LObject = Node.Nodes[LIndex].Execute(Program);
						if (ARow.DataType.Columns[LColumnIndex].DataType is Schema.ScalarType)
							LObject = ValueUtility.ValidateValue(Program, (Schema.ScalarType)ARow.DataType.Columns[LColumnIndex].DataType, LObject);
						ARow[LColumnIndex] = LObject;
					}
				}
            }
            finally
            {
				Program.Stack.Pop();
            }
        }
        
        protected override bool InternalNext()
        {
            return FSourceTable.Next();
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