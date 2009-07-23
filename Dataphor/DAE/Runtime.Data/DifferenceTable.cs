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
		public DifferenceTable(DifferenceNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}

        public new DifferenceNode Node { get { return (DifferenceNode)FNode; } }
        
		protected Table FLeftTable;
		protected Table FRightTable;
		protected bool FBOF;
        
        protected override void InternalOpen()
        {
			FLeftTable = (Table)Node.Nodes[0].Execute(Process);
			FRightTable = (Table)Node.Nodes[1].Execute(Process);
			FBOF = true;
        }
        
        protected override void InternalClose()
        {
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
        }
        
        protected override void InternalReset()
        {
			FLeftTable.Reset();
			FBOF = true;
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FLeftTable.Select(ARow);
        }
        
        protected override void InternalLast()
        {
			FLeftTable.Last();
        }
        
        protected override bool InternalBOF()
        {
			return FBOF;
        }
        
        protected override bool InternalEOF()
        {
			if (FBOF)
			{
				InternalNext();
				if (FLeftTable.EOF())
					return true;
				else
				{
					if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
						FLeftTable.First();
					else
						FLeftTable.Reset();
					FBOF = true;
					return false;
				}
			}
			return FLeftTable.EOF();
        }
        
        protected override void InternalFirst()
        {
			FLeftTable.First();
			FBOF = true;
        }
    }
    
    public class SearchedDifferenceTable : DifferenceTable
    {
		public SearchedDifferenceTable(DifferenceNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected Row FKeyRow;
		
		protected override void InternalOpen()
		{
			base.InternalOpen();
			FKeyRow = new Row(Process, new Schema.RowType(Node.RightNode.Order.Columns));
		}
		
		protected override void InternalClose()
		{
			if (FKeyRow != null)
			{
				FKeyRow.Dispose();
				FKeyRow = null;
			}
			
			base.InternalClose();
		}
		
		protected override bool InternalNext()
		{
			while (FLeftTable.Next())
			{
				FLeftTable.Select(FKeyRow);
				if (!FRightTable.FindKey(FKeyRow))
				{
					FBOF = false;
					return true;
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (FLeftTable.Prior())
			{
				FLeftTable.Select(FKeyRow);
				if (!FRightTable.FindKey(FKeyRow))
					return true;
			}
			FBOF = true;
			return false;
		}
    }
    
    public class ScannedDifferenceTable : DifferenceTable
    {
		public ScannedDifferenceTable(DifferenceNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected Row FLeftRow;
		protected Row FRightRow;

        protected override void InternalOpen()
        {
			base.InternalOpen();
			FLeftRow = new Row(Process, FLeftTable.DataType.RowType);
			FRightRow = new Row(Process, FRightTable.DataType.RowType);
        }
        
        protected override void InternalClose()
        {
            if (FLeftRow != null)
            {
				FLeftRow.Dispose();
                FLeftRow = null;
            }

            if (FRightRow != null)
            {
				FRightRow.Dispose();
                FRightRow = null;
            }

			base.InternalClose();
        }
        
        protected bool RowsEqual()
        {
			Process.Context.Push(FRightRow);
			try
			{
				Process.Context.Push(FLeftRow);
				try
				{
					object LObject = Node.EqualNode.Execute(Process);
					return (LObject != null) && (bool)LObject;
				}
				finally
				{
					Process.Context.Pop();
				}
			}
			finally
			{
				Process.Context.Pop();
			}
        }
        
        protected override bool InternalNext()
        {
			bool LFound = false;
			bool LHasRow;
			while (!LFound && !FLeftTable.EOF())
			{
				FLeftTable.Next();
				if (!FLeftTable.EOF())
				{
					FLeftTable.Select(FLeftRow);
					LHasRow = false;
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
					while (FRightTable.Next())
					{
						FRightTable.Select(FRightRow);
						if (RowsEqual())
						{
							LHasRow = true;
							break;
						}
					}
					if (!LHasRow)
					{
						FBOF = false;
						LFound = true;
					}
				}
			}
			return LFound;
        }
        
        protected override bool InternalPrior()
        {
			bool LFound = false;
			bool LHasRow;
			while (!LFound && !FLeftTable.BOF())
			{
				FLeftTable.Prior();
				if (!FLeftTable.BOF())
				{
					FLeftTable.Select(FLeftRow);
					LHasRow = false;
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
					while (FRightTable.Next())
					{
						FRightTable.Select(FRightRow);
						if (RowsEqual())
						{
							LHasRow = true;
							break;
						}
					}
					if (!LHasRow)
						LFound = true;
				}
			}
			return LFound;
        }
    }
}