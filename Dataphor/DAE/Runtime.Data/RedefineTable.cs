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

    public class RedefineTable : Table
    {
		public RedefineTable(RedefineNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}

        public new RedefineNode Node { get { return (RedefineNode)FNode; } }
        
		protected DataVar FSourceObject;        
		protected Table FSourceTable;
		protected Row FSourceRow;
        
        protected override void InternalOpen()
        {
			FSourceObject = Node.Nodes[0].Execute(Process);
			try
			{
				FSourceTable = (Table)FSourceObject.Value;
				FSourceRow = new Row(Process, FSourceTable.DataType.RowType);
			}
			catch
			{
				((Table)FSourceObject.Value).Dispose();
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

			int LColumnIndex;            
            for (int LIndex = 0; LIndex < Node.DataType.Columns.Count; LIndex++)
            {
				if (!((IList)Node.RedefineColumnOffsets).Contains(LIndex))
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(DataType.Columns[LIndex].Name);
					if (LColumnIndex >= 0)
						if (FSourceRow.HasValue(LIndex))
							ARow[LColumnIndex] = FSourceRow[LIndex];
						else
							ARow.ClearValue(LColumnIndex);
				}
            }

	        Process.Context.Push(new DataVar(String.Empty, FSourceRow.DataType, FSourceRow));
            try
            {
				for (int LIndex = 0; LIndex < Node.RedefineColumnOffsets.Length; LIndex++)
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RedefineColumnOffsets[LIndex]].Name);
					if (LColumnIndex >= 0)
					{
						DataVar LResult = Node.Nodes[LIndex + 1].Execute(Process);
						ARow[LColumnIndex] = LResult.Value;
					}
				}
            }
            finally
            {
				Process.Context.Pop();
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