/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	/* BTR 5/21/2001 -> Replaced by the natural join
    public class IntersectTable : Table
    {
		public IntersectTable(IntersectNode ANode, Plan APlan) : base(ANode, APlan){}

        public new IntersectNode Node
        {
            get
            {
                return (IntersectNode)FNode;
            }
        }
        
        public override CursorCapability Capabilities
        {
			get
			{
				return 
					base.Capabilities | 
					(
						(FLeftTable.Capabilities & CursorCapability.BackwardsNavigable) & 
						(FRightTable.Capabilities & CursorCapability.BackwardsNavigable)
					);
			}
        }

		protected DataVar FLeftObject;
		protected DataVar FRightObject;
		protected Table FLeftTable;
		protected Table FRightTable;
		protected Row FLeftRow;
		protected Row FRightRow;
		protected DataVar FLeftRowObject;
		protected DataVar FRightRowObject;
        
        protected override void InternalOpen()
        {
			FLeftObject = ((ExpressionNode)Node.Nodes[0]).Evaluate(FPlan);
			try
			{
				FLeftTable = (Table)FLeftObject.Value;
				FLeftRow = new Row(FLeftTable.TableType);
				FLeftRowObject = new DataVar(String.Empty, FLeftRow.RowType, FLeftRow);
			}
			catch
			{
				FLeftObject.Dispose();
				throw;
			}

			FRightObject = ((ExpressionNode)Node.Nodes[1]).Evaluate(FPlan);
			try
			{
				FRightTable = (Table)FRightObject.Value;
				FRightRow = new Row(FRightTable.TableType);
				FRightRowObject =  new DataVar(String.Empty, FRightRow.RowType, FRightRow);
			}
			catch
			{
				FRightObject.Dispose();
				throw;
			}
        }
        
        protected override void InternalClose()
        {
			if (FLeftObject != null)
			{
				FLeftObject.Dispose();
				FLeftObject = null;
			}

			if (FRightObject != null)
			{
				FRightObject.Dispose();
				FRightObject = null;
			}

            if (FLeftRow != null)
                FLeftRow = null;
                
            if (FLeftRowObject != null)
            {
				FLeftRowObject.Dispose();
				FLeftRowObject = null;
            }

            if (FRightRow != null)
                FRightRow = null;
                
            if (FRightRowObject != null)
            {
				FRightRowObject.Dispose();
				FRightRowObject = null;
            }
        }
        
        protected override void InternalReset()
        {
			FLeftTable.Reset();
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FLeftTable.Select(ARow);
        }
        
        protected bool RowsEqual()
        {
			FPlan.Symbols.Push(FLeftRowObject);
			try
			{
				FPlan.Symbols.Push(FRightRowObject);
				try
				{
					using (DataVar LObject = Node.EqualNode.Evaluate(FPlan))
					{
						return LObject.AsBoolean();
					}
				}
				finally
				{
					FPlan.Symbols.Pop();
				}
			}
			finally
			{
				FPlan.Symbols.Pop();
			}
        }
        
        protected override void InternalNext()
        {
			bool LFound = false;
			while (!LFound && !FLeftTable.EOF())
			{
				FLeftTable.Next();
				if (!FLeftTable.EOF())
				{
					FLeftTable.Select(FLeftRow);
					FRightTable.Reset();
					while (FRightTable.Next())
					{
						FRightTable.Select(FRightRow);
						if (RowsEqual())
						{
							LFound = true;
							break;
						}
					}
				}
			}
        }
        
        protected override void InternalLast()
        {
			FLeftTable.Last();
        }
        
        protected override bool InternalBOF()
        {
            return FLeftTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
            return FLeftTable.EOF();
        }
        
        protected override void InternalPrior()
        {
			bool LFound = false;
			while (!LFound && !FLeftTable.BOF())
			{
				FLeftTable.Prior();
				if (!FLeftTable.BOF())
				{
					FLeftTable.Select(FLeftRow);
					FRightTable.Reset();
					while (FRightTable.Next())
					{
						FRightTable.Select(FRightRow);
						if (RowsEqual())
						{
							LFound = true;
							break;
						}
					}
				}
			}
        }
        
        protected override void InternalFirst()
        {
			FLeftTable.First();
        }
    }
    */

	/* BTR 5/21/2001 -> Replaced by the natural join
    public class ProductTable : Table
    {
		public ProductTable(ProductNode ANode, Plan APlan) : base(ANode, APlan){}

        public new ProductNode Node
        {
            get
            {
                return (ProductNode)FNode;
            }
        }
        
        public override CursorCapability Capabilities
        {
			get
			{
				return 
					base.Capabilities | 
					(
						(FLeftTable.Capabilities & CursorCapability.BackwardsNavigable) & 
						(FRightTable.Capabilities & CursorCapability.BackwardsNavigable)
					);
			}
        }

		protected DataVar FLeftObject;
		protected DataVar FRightObject;
		protected Table FLeftTable;
		protected Table FRightTable;
		protected Row FLeftRow; // TODO: make sure these are required, or remove them
		protected Row FRightRow;
        
        protected override void InternalOpen()
        {
			FLeftObject = ((ExpressionNode)Node.Nodes[0]).Evaluate(FPlan);
			try
			{
				FLeftTable = (Table)FLeftObject.Value;
				FLeftRow = new Row(FLeftTable.TableType);
			}
			catch
			{
				FLeftObject.Dispose();
				throw;
			}

			FRightObject = ((ExpressionNode)Node.Nodes[1]).Evaluate(FPlan);
			try
			{
				FRightTable = (Table)FRightObject.Value;
				FRightRow = new Row(FRightTable.TableType);
			}
			catch
			{
				FRightObject.Dispose();
				throw;
			}
        }
        
        protected override void InternalClose()
        {
			if (FLeftObject != null)
			{
				FLeftObject.Dispose();
				FLeftObject = null;
			}

			if (FRightObject != null)
			{
				FRightObject.Dispose();
				FRightObject = null;
			}

            if (FLeftRow != null)
                FLeftRow = null;

            if (FRightRow != null)
                FRightRow = null;
        }
        
        protected override void InternalReset()
        {
			FLeftTable.Reset();
			FRightTable.Reset();
        }
        
        protected override void InternalSelect(Row ARow)
        {
			FLeftTable.Select(ARow);
			FRightTable.Select(ARow);
        }
        
        protected override void InternalNext()
        {
			if (!FLeftTable.IsEmpty() && !FRightTable.IsEmpty())
			{
				if (FLeftTable.BOF())
				{
					FLeftTable.Next();
					if (FRightTable.BOF())
						FRightTable.Next();
				}
				else if (!FRightTable.EOF())
				{
					FRightTable.Next();
					if (FRightTable.EOF())
					{
						FLeftTable.Next();
						if (!FLeftTable.EOF())
						{
							FRightTable.Reset();
							FRightTable.Next();
						}
					}
				}
			}
        }
        
        protected override void InternalLast()
        {
			FLeftTable.Last();
			FRightTable.Last();
        }
        
        protected override bool InternalBOF()
        {
            return FLeftTable.BOF() || FRightTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
            return FLeftTable.EOF() || FRightTable.EOF();
        }
        
        protected override void InternalPrior()
        {
			if (!FLeftTable.IsEmpty() && !FRightTable.IsEmpty())
			{
				if (FLeftTable.EOF())
				{
					FLeftTable.Prior();
					if (FRightTable.EOF())
						FRightTable.Prior();
				}
				else if (!FRightTable.BOF())
				{
					FRightTable.Prior();
					if (FRightTable.BOF())
					{
						FLeftTable.Prior();
						if (!FLeftTable.BOF())
						{
							FRightTable.Last();
							FRightTable.Prior();
						}
					}
				}
			}
        }
        
        protected override void InternalFirst()
        {
			FLeftTable.First();
			FRightTable.First();
        }
    }
    */
}