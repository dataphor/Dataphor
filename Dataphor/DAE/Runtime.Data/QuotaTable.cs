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
		public QuotaTable(QuotaNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}
		
		public new QuotaNode Node { get { return (QuotaNode)FNode; } }
		
		protected Table FSourceTable;
		protected int FSourceCount;
		protected int FSourceCounter;
		protected Row FSourceRow;
		protected Row FCompareRow;
		protected Row FLastCompareRow;
		protected bool FHasLastCompareRow;
		
        protected override void InternalOpen()
        {
			FSourceTable = (Table)Node.Nodes[0].Execute(Process);
			try
			{
				FCompareRow = new Row(Process, new Schema.RowType(Node.Order.Columns));
				FLastCompareRow = new Row(Process, FCompareRow.DataType);
				FHasLastCompareRow = false;
				object LObject = Node.Nodes[1].Execute(Process);
				#if NILPROPOGATION
				FSourceCount = LObject == null ? 0 : (int)LObject;
				#else
				if (LObject == null)
					throw new RuntimeException(RuntimeException.Codes.NilEncountered, Node);
				FSourceCount = (int)LObject;
				#endif
				FSourceCounter = 0;
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
			
			if (FCompareRow != null)
			{
				FCompareRow.Dispose();
				FCompareRow = null;
			}

			if (FLastCompareRow != null)
			{
				FLastCompareRow.Dispose();
				FLastCompareRow = null;
			}
        }
        
        protected override void InternalReset()
        {
            FSourceTable.Reset();
            FSourceCounter = 0;
            FLastCompareRow.ClearValues();
            FHasLastCompareRow = false;
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FSourceTable.Select(ARow);
        }
        
        protected override bool InternalNext()
        {
			if (!InternalEOF())
			{
	            if (FSourceTable.Next())
	            {
		            FSourceTable.Select(FCompareRow);

					if (FHasLastCompareRow)
					{		            
						Process.Stack.Push(FCompareRow);
						try
						{
							Process.Stack.Push(FLastCompareRow);
							try
							{
								object LResult = Node.EqualNode.Execute(Process);
								if ((LResult == null) || !(bool)LResult)
								{
									FSourceCounter++;
									FCompareRow.CopyTo(FLastCompareRow);
								}
							}
							finally
							{
								Process.Stack.Pop();
							}
						}
						finally
						{
							Process.Stack.Pop();
						}
					}
					else
					{
						FSourceCounter++;
						FCompareRow.CopyTo(FLastCompareRow);
						FHasLastCompareRow = true;
					}
					return !InternalEOF();
		        }
			}
			return false;
        }
        
        protected override bool InternalBOF()
        {
			return (FSourceCount == 0) || (FSourceCounter == 0);
        }
        
        protected override bool InternalEOF()
        {
			return (FSourceCount == 0) || (FSourceCounter > FSourceCount) || (FSourceTable.EOF());
        }
    }
}