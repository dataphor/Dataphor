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

    public class UnionTable : Table
    {
		public UnionTable(UnionNode ANode, Program AProgram) : base(ANode, AProgram){}

        public new UnionNode Node { get { return (UnionNode)FNode; } }
        
		protected Table FLeftTable;
		protected Table FRightTable;
		protected Row FSourceRow;
		protected NativeTable FBuffer;
		protected Scan FScan;
        
        protected override void InternalOpen()
        {
			FSourceRow = new Row(Manager, Node.DataType.RowType);
			FLeftTable = Node.Nodes[0].Execute(Program) as Table;
			try
			{
				FRightTable = Node.Nodes[1].Execute(Program) as Table;
			}
			catch
			{
				FLeftTable.Dispose();
				throw;
			}
			
			Schema.TableType LTableType = new Schema.TableType();
			Schema.BaseTableVar LTableVar = new Schema.BaseTableVar(LTableType, Program.TempDevice);
			Schema.TableVarColumn LNewColumn;
			foreach (Schema.TableVarColumn LColumn in FLeftTable.Node.TableVar.Columns)
			{
				LNewColumn = LColumn.Inherit();
				LTableType.Columns.Add(LColumn.Column);
				LTableVar.Columns.Add(LColumn);
			}
			
			Schema.Key LKey = new Schema.Key();
			foreach (Schema.TableVarColumn LColumn in Node.TableVar.Keys.MinimumKey(true).Columns)
				LKey.Columns.Add(LTableVar.Columns[LColumn.Name]);
			LTableVar.Keys.Add(LKey);
			
			FBuffer = new NativeTable(Manager, LTableVar);
			PopulateBuffer();

			FScan = new Scan(Manager, FBuffer, FBuffer.ClusteredIndex, ScanDirection.Forward, null, null);
			FScan.Open();
        }
        
        protected override void InternalClose()
        {
			if (FScan != null)
			{
				FScan.Dispose();
				FScan = null;
			}
			
			if (FBuffer != null)
			{
				FBuffer.Drop(Manager);
				FBuffer = null;
			}
			
			if (FLeftTable != null)
			{
				FLeftTable.Dispose();
				FLeftTable = null;
			}

			if (FRightTable != null)
			{
				FRightTable.Dispose();
				FRightTable = null;
			}

            if (FSourceRow != null)
            {
				FSourceRow.Dispose();
                FSourceRow = null;
            }
        }
        
        protected void PopulateBuffer()
        {
			while (FLeftTable.Next())
			{
				FLeftTable.Select(FSourceRow);
				FBuffer.Insert(Manager, FSourceRow);
			}
			
			while (FRightTable.Next())
			{
				FRightTable.Select(FSourceRow);
				if (!FBuffer.HasRow(Manager, FSourceRow))
					FBuffer.Insert(Manager, FSourceRow);
			}
        }
        
        protected override void InternalReset()
        {
			FLeftTable.Reset();
			FRightTable.Reset();
			FScan.Close();
			FScan.Dispose();
			FBuffer.Truncate(Manager);
			PopulateBuffer();
			FScan = new Scan(Manager, FBuffer, FBuffer.ClusteredIndex, ScanDirection.Forward, null, null);
			FScan.Open();
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FScan.GetRow(ARow);
        }
        
        protected override bool InternalNext()
        {
			return FScan.Next();
        }
        
        protected override void InternalLast()
        {
			FScan.Last();
			//while (Next()); ??
        }
        
        protected override bool InternalBOF()
        {
			return FScan.BOF();
        }
        
        protected override bool InternalEOF()
        {
			return FScan.EOF();
        }
        
        protected override bool InternalPrior()
        {
			return FScan.Prior();
        }
        
        protected override void InternalFirst()
        {
			FScan.First();
        }
    }
}