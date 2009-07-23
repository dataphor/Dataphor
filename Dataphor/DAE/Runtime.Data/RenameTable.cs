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

    public class RenameTable : Table
    {
		public RenameTable(RenameNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}

        public new RenameNode Node { get { return (RenameNode)FNode; } }
        
		protected Table FSourceTable;
		protected Row FSourceRow;
		protected Schema.RowType FKeyRowType;
        
        protected override void InternalOpen()
        {
			FSourceTable = (Table)Node.Nodes[0].Execute(Process);
			FSourceRow = new Row(Process, FSourceTable.DataType.RowType);
        }
        
        protected override void InternalClose()
        {
            if (FSourceRow != null)
            {
				FSourceRow.Dispose();
                FSourceRow = null;
            }
            
			if (FSourceTable != null)
			{
				FSourceTable.Dispose();
				FSourceTable = null;
			}
        }
        
        protected override void InternalReset()
        {
            FSourceTable.Reset();
        }
        
        protected override void InternalSelect(Row ARow)
        {
			// alternative rename algorithm could construct a row of the source type first
			FSourceTable.Select(FSourceRow);
            
            int LColumnIndex;
            for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
			{
				LColumnIndex = DataType.Columns.IndexOfName(ARow.DataType.Columns[LIndex].Name);
				if (LColumnIndex >= 0)
					if (FSourceRow.HasValue(LColumnIndex))
						ARow[LIndex] = FSourceRow[LColumnIndex];
					else
						ARow.ClearValue(LIndex);
			}
        }
        
        protected override bool InternalNext()
        {
            return FSourceTable.Next();
        }
        
        protected override void InternalLast()
        {
			FSourceTable.Last();
        }
        
        protected override bool InternalBOF()
        {
            return FSourceTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
            return FSourceTable.EOF();
        }
        
        protected override bool InternalPrior()
        {
			return FSourceTable.Prior();
        }
        
        protected override void InternalFirst()
        {
			FSourceTable.First();
        }
        
        protected override Row InternalGetKey()
        {
			Row LKey = FSourceTable.GetKey();
			try
			{
				if (FKeyRowType == null)
				{
					FKeyRowType = new Schema.RowType();
					for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
						FKeyRowType.Columns.Add(DataType.Columns[FSourceTable.DataType.Columns.IndexOfName(LKey.DataType.Columns[LIndex].Name)].Copy());
				}
				Row LRow = new Row(Process, FKeyRowType);
				for (int LIndex = 0; LIndex < LKey.DataType.Columns.Count; LIndex++)
					if (LKey.HasValue(LIndex))
						LRow[LIndex] = LKey[LIndex];
				return LRow;
			}
			finally
			{
				LKey.Dispose();
			}
        }
        
        protected Row BuildSourceKey(Row AKey)
        {
			Schema.RowType LRowType = new Schema.RowType();
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
				LRowType.Columns.Add(FSourceTable.DataType.Columns[DataType.Columns.IndexOfName(AKey.DataType.Columns[LIndex].Name)].Copy());
			Row LKey = new Row(Process, LRowType);
			for (int LIndex = 0; LIndex < AKey.DataType.Columns.Count; LIndex++)
				if (AKey.HasValue(LIndex))
					LKey[LIndex] = AKey[LIndex];
			return LKey;
        }

		protected override bool InternalRefresh(Row AKey)
		{
			Row LRow = BuildSourceKey(AKey);
			try
			{
				return FSourceTable.Refresh(LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}

		protected override bool InternalFindKey(Row AKey, bool AForward)
        {
			Row LKey = BuildSourceKey(AKey);
			try
			{
				return FSourceTable.FindKey(LKey, AForward);
			}
			finally
			{
				LKey.Dispose();
			}
				
        }
        
        protected override void InternalFindNearest(Row AKey)
        {
			Row LKey = BuildSourceKey(AKey);
			try
			{
				FSourceTable.FindNearest(LKey);
			}
			finally
			{
				LKey.Dispose();
			}
        }
    }
}