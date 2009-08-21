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
	
	public abstract class ConditionedTable : Table
	{
		public ConditionedTable(ConditionedTableNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		public new ConditionedTableNode Node { get { return (ConditionedTableNode)FNode; } }

		protected Table FLeftTable;
		protected Table FRightTable;
		protected Row FLeftRow;
		protected Row FRightRow;

        protected override void InternalOpen()
        {
			FLeftTable = (Table)Node.Nodes[0].Execute(Process);
			FLeftRow = new Row(Process, new Schema.RowType(Node.LeftKey.Columns));
			FRightTable = (Table)Node.Nodes[1].Execute(Process);
			FRightRow = new Row(Process, new Schema.RowType(Node.RightKey.Columns));
        }
        
        protected override void InternalClose()
        {
			if (FLeftRow != null)
			{
				FLeftRow.Dispose();
				FLeftRow = null;
			}
			
			if (FLeftTable != null)
			{
				FLeftTable.Dispose();
				FLeftTable = null;
			}

			if (FRightRow != null)
			{
				FRightRow.Dispose();
				FRightRow = null;
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
			FRightTable.Reset();
        }

        protected override bool InternalBOF()
        {
            return FLeftTable.BOF();
        }

        protected override bool InternalEOF()
        {
			if (FLeftTable.BOF())
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
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
					return false;
				}
			}
			return FLeftTable.EOF();
        }
        
        protected override void InternalLast()
        {
			FLeftTable.Last();
			FRightTable.Last();
        }

        protected override void InternalFirst()
        {
			FLeftTable.First();
			FRightTable.First();
        }
        
        protected bool IsMatch()
        {
			return CompareKeys(FLeftRow, FRightRow) == 0;
        }

        protected int CompareKeyValues(object AKeyValue1, object AKeyValue2, PlanNode ACompareNode)
        {
			Process.Stack.Push(AKeyValue1);
			Process.Stack.Push(AKeyValue2);
			int LResult = (int)ACompareNode.Execute(Process);
			Process.Stack.Pop();
			Process.Stack.Pop();
			return LResult;
        }
        
        protected int CompareKeys(Row AKey1, Row AKey2)
        {
			int LResult = 0;
			for (int LIndex = 0; LIndex < Node.JoinOrder.Columns.Count; LIndex++)
			{
				if ((LIndex >= AKey1.DataType.Columns.Count) || !AKey1.HasValue(LIndex))
					return -1;
				else if ((LIndex >= AKey2.DataType.Columns.Count) || !AKey2.HasValue(LIndex))
					return 1;
				else
				{
					LResult = CompareKeyValues(AKey1[LIndex], AKey2[LIndex], Node.JoinOrder.Columns[LIndex].Sort.CompareNode) * (Node.JoinOrder.Columns[LIndex].Ascending ? 1 : -1);
					if (LResult != 0)
						return LResult;
				}
			}
			return LResult;
        }

		// Gets the right key row for the current left row		
		protected void GetRightKey()
		{
			for (int LIndex = 0; LIndex < Node.LeftKey.Columns.Count; LIndex++)
				if (FLeftRow.HasValue(LIndex))
					FRightRow[LIndex] = FLeftRow[LIndex];
				else
					FRightRow.ClearValue(LIndex);
		}

		// Gets the left key row for the current right row		
		protected void GetLeftKey()
		{
			for (int LIndex = 0; LIndex < Node.RightKey.Columns.Count; LIndex++)
				if (FRightRow.HasValue(LIndex))
					FLeftRow[LIndex] = FRightRow[LIndex];
				else
					FLeftRow.ClearValue(LIndex);
		}
	}
	
	public abstract class SemiTable : ConditionedTable
	{
		public SemiTable(SemiTableNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
        protected override void InternalSelect(Row ARow)
        {
			FLeftTable.Select(ARow);
        }
	}
	
	public abstract class HavingTable : SemiTable
	{
		public HavingTable(HavingNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
	}
	
	/*
	// TODO: Implement Merge having algorithm
	public class MergeHavingTable : HavingTable
	{
		public MergeHavingTable(HavingNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
	}
	*/
	
	public class SearchedHavingTable : HavingTable
	{
		public SearchedHavingTable(HavingNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		protected override bool InternalNext()
		{
			while (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FRightTable.FindKey(FRightRow))
					return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (FLeftTable.Prior())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FRightTable.FindKey(FRightRow))
					return true;
			}
			return false;
		}
	}
	
	public abstract class WithoutTable : SemiTable
	{
		public WithoutTable(WithoutNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
	}
	
	/*
	// TODO: Implement Merge without algorithm
	public class MergeWithoutTable : WithoutTable
	{
		public MergeWithoutTable(WithoutNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
	}
	*/
	
	public class SearchedWithoutTable : WithoutTable
	{
		public SearchedWithoutTable(WithoutNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		protected bool FBOF;

        protected override void InternalOpen()
        {
			base.InternalOpen();
			FBOF = true;
        }

        protected override void InternalReset()
        {
			FLeftTable.Reset();
			FBOF = true;
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

		protected override bool InternalNext()
		{
			while (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (!FRightTable.FindKey(FRightRow))
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
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (!FRightTable.FindKey(FRightRow))
					return true;
			}
			FBOF = true;
			return false;
		}
	}

    public abstract class JoinTable : ConditionedTable
    {
		public JoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		public new JoinNode Node { get { return (JoinNode)FNode; } }

        protected override void InternalSelect(Row ARow)
        {
			FLeftTable.Select(ARow);
			FRightTable.Select(ARow);
        }
    }
    
    // Left Unique nested loop join algorithm
    // left join key unique
    public class LeftUniqueNestedLoopJoinTable : JoinTable
    {
		public LeftUniqueNestedLoopJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalBOF()
		{
			return FRightTable.BOF();
		}
		
		protected override bool InternalEOF()
		{
			if (FRightTable.BOF())
			{
				InternalNext();
				if (FRightTable.EOF())
					return true;
				else
				{
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
					if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
						FLeftTable.First();
					else
						FLeftTable.Reset();
					return false;
				}
			}
			return FRightTable.EOF();
		}
		
		protected override bool InternalNext()
		{
			while (FRightTable.Next())
			{
				if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
					FLeftTable.First();
				else
					FLeftTable.Reset();
				FRightTable.Select(FRightRow);
				while (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
						return true;
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (FRightTable.Prior())
			{
				FLeftTable.Last();
				FRightTable.Select(FRightRow);
				while (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
						return true;
				}
			}
			return false;
		}
    }
    
	// Right Unique nested loop join algorithm
	// right join key unique
    public class RightUniqueNestedLoopJoinTable : JoinTable
    {
		public RightUniqueNestedLoopJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
        protected override bool InternalNext()
        {
			while (FLeftTable.Next())
			{
				if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
					FRightTable.First();
				else
					FRightTable.Reset();
				FLeftTable.Select(FLeftRow);
				while (FRightTable.Next())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
						return true;
				}
			}
			return false;
		}
			
        protected override bool InternalPrior()
        {
			while (FLeftTable.Prior())
			{
				FRightTable.Last();
				FLeftTable.Select(FLeftRow);
				while (FRightTable.Prior())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
						return true;
				}
			}
			return false;
        }
    }

	// Non unique nested loop join algorithm, works on any input
    public class NonUniqueNestedLoopJoinTable : JoinTable
    {
		public NonUniqueNestedLoopJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

        protected override bool InternalNext()
        {
			if (FLeftTable.BOF())
				FLeftTable.Next();
				
			while (!FLeftTable.EOF())
			{
				FLeftTable.Select(FLeftRow);
				FRightTable.Next();
				while (!FRightTable.EOF())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
						return true;
					FRightTable.Next();
				}
				FLeftTable.Next();
				if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
					FRightTable.First();
				else
					FRightTable.Reset();
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (FLeftTable.EOF())
				FLeftTable.Prior();
				
			while (!FLeftTable.BOF())
			{
				FLeftTable.Select(FLeftRow);
				FRightTable.Prior();
				while (!FRightTable.BOF())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
						return true;
					FRightTable.Prior();
				}
				FLeftTable.Prior();
				FRightTable.Last();
			}
			return false;
        }
    }

	// times algorithm, works on any input
    public class TimesTable : JoinTable
    {
		public TimesTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		private bool FHitEOF;
		private bool FHitBOF;
		
		protected override bool InternalNext()
		{
			if (FHitEOF)
				return false;
				
			while (true)
			{
				if (FRightTable.Next())
				{
					if (FLeftTable.BOF())
					{
						FHitEOF = !FLeftTable.Next();
						if (!FHitEOF)
							FHitBOF = false;
						return !FHitEOF;
					}
					FHitBOF = false;
					return true;
				}
				else
				{
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
						
					if (!FLeftTable.Next())
					{
						FHitEOF = true;
						return false;
					}
				}				
			}
		}
		
		protected override bool InternalPrior()
		{
			while (true)
			{
				if (FHitBOF)
					return false;
					
				if (FRightTable.Prior())
				{
					if (FLeftTable.EOF())
					{
						FHitBOF = !FLeftTable.Prior();
						if (!FHitBOF)
							FHitEOF = false;
						return !FHitBOF;
					}
					FHitEOF = false;
					return true;
				}
				else
				{
					FRightTable.Last();
				
					if (!FLeftTable.Prior())
					{
						FHitBOF = true;
						return false;
					}
				}
			}
		}

        protected override void InternalSelect(Row ARow)
        {
			base.InternalSelect(ARow);
			OuterJoinNode LOuterJoinNode = Node as OuterJoinNode;
			if ((LOuterJoinNode != null) && LOuterJoinNode.RowExistsColumnIndex >= 0)
			{
				int LColumnIndex = ARow.DataType.Columns.IndexOfName(Node.DataType.Columns[LOuterJoinNode.RowExistsColumnIndex].Name);
				if (LColumnIndex >= 0)
					ARow[LColumnIndex] = true;
			}
        }
    }

	// unique merge join algorithm
	// both inputs are ordered by the join keys, ascending in the same order
	// left and right join keys are unique
    public class UniqueMergeJoinTable : JoinTable
    {
		public UniqueMergeJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			int LCompareValue;			
			FLeftTable.Next();
			FRightTable.Next();
			while (!(FLeftTable.EOF() || FRightTable.EOF()))
			{
				FLeftTable.Select(FLeftRow);
				FRightTable.Select(FRightRow);
				LCompareValue = CompareKeys(FLeftRow, FRightRow);
				if (LCompareValue == 0)
					return true;
				else if (LCompareValue < 0)
					FLeftTable.Next();
				else
					FRightTable.Next();
			}
			FLeftTable.Last();
			FRightTable.Last();
			return false;
		}
		
		protected override bool InternalPrior()
		{
			int LCompareValue;
			FLeftTable.Prior();
			FRightTable.Prior();
			while (!(FLeftTable.BOF() || FRightTable.BOF()))
			{
				FLeftTable.Select(FLeftRow);
				FRightTable.Select(FRightRow);
				LCompareValue = CompareKeys(FLeftRow, FRightRow);
				if (LCompareValue == 0)
					return true;
				else if (LCompareValue < 0)
					FRightTable.Prior();
				else
					FLeftTable.Prior();
			}
			FLeftTable.First();
			FRightTable.First();
			return false;
		}
    }

	// Left unique merge join algorithm    
	// left and right inputs ordered by the join keys, ascending in the same order
	// left key is unique, right key is non-unique
    public class LeftUniqueMergeJoinTable : JoinTable
    {
		public LeftUniqueMergeJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			if (FLeftTable.BOF() && FLeftTable.Next())
				FLeftTable.Select(FLeftRow);
			FRightTable.Next();
			int LCompareValue;
			while (!(FLeftTable.EOF() || FRightTable.EOF()))
			{
				FRightTable.Select(FRightRow);
				LCompareValue = CompareKeys(FLeftRow, FRightRow);
				if (LCompareValue == 0)
					return true;
				else if (LCompareValue < 0)
				{
					if (FLeftTable.Next())
						FLeftTable.Select(FLeftRow);
				}
				else
					FRightTable.Next();
			}
			FLeftTable.Last();
			FRightTable.Last();
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.EOF() && FLeftTable.Prior())
				FLeftTable.Select(FLeftRow);
			FRightTable.Prior();
			int LCompareValue;
			while (!(FLeftTable.BOF() || FRightTable.BOF()))
			{
				FRightTable.Select(FRightRow);
				LCompareValue = CompareKeys(FLeftRow, FRightRow);
				if (LCompareValue == 0)
					return true;
				else if (LCompareValue < 0)
					FRightTable.Prior();
				else
				{
					if (FLeftTable.Prior())
						FLeftTable.Select(FLeftRow);
				}
			}
			FLeftTable.First();
			FRightTable.First();
			return false;
		}
    }

	// Right unique merge join algorithm
	// left and right inputs are ordered by the join keys, ascending in the same order
	// left key is non-unique, right key is unique
    public class RightUniqueMergeJoinTable : JoinTable
    {
		public RightUniqueMergeJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			if (FRightTable.BOF() && FRightTable.Next())
				FRightTable.Select(FRightRow);
			FLeftTable.Next();
			int LCompareValue;
			while (!(FLeftTable.EOF() || FRightTable.EOF()))
			{
				FLeftTable.Select(FLeftRow);
				LCompareValue = CompareKeys(FLeftRow, FRightRow);
				if (LCompareValue == 0)
					return true;
				else if (LCompareValue < 0)
					FLeftTable.Next();
				else
				{
					if (FRightTable.Next())
						FRightTable.Select(FRightRow);
				}
			}
			FRightTable.Last();
			FLeftTable.Last();
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.EOF() && FRightTable.Prior())
				FRightTable.Select(FRightRow);
			FLeftTable.Prior();
			int LCompareValue;
			while (!(FLeftTable.BOF() || FRightTable.BOF()))
			{
				FLeftTable.Select(FLeftRow);
				LCompareValue = CompareKeys(FLeftRow, FRightRow);
				if (LCompareValue == 0)
					return true;
				else if (LCompareValue < 0)
				{
					if (FRightTable.Prior())
						FRightTable.Select(FRightRow);
				}
				else
					FLeftTable.Prior();
			}
			FRightTable.First();
			FLeftTable.First();
			return false;
		}
    }
    
    public abstract class RightSearchedJoinTable : JoinTable
    {
		public RightSearchedJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
    }
    
    public abstract class LeftSearchedJoinTable : JoinTable
    {
		public LeftSearchedJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
        protected override bool InternalBOF()
        {
            return FRightTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
			if (FRightTable.BOF())
			{
				InternalNext();
				if (FRightTable.EOF())
					return true;
				else
				{
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
					if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
						FLeftTable.First();
					else
						FLeftTable.Reset();
					return false;
				}
			}
			return FRightTable.EOF();
        }
    }
    
    // Left Unique Searched join algorithm
    // works when only the left input is ordered and the left key is unique
    public class LeftUniqueSearchedJoinTable : LeftSearchedJoinTable
    {
		public LeftUniqueSearchedJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			while (FRightTable.Next())
			{
				FRightTable.Select(FRightRow);
				GetLeftKey();
				if (FLeftTable.FindKey(FLeftRow))
					return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (FRightTable.Prior())
			{
				FRightTable.Select(FRightRow);
				GetLeftKey();
				if (FLeftTable.FindKey(FLeftRow))
					return true;
			}
			return false;
		}
    }
    
	// Right Unique Searched join algorithm
	// works when only the right input is ordered and the right key is unique
    public class RightUniqueSearchedJoinTable : RightSearchedJoinTable
    {
		public RightUniqueSearchedJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			while (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FRightTable.FindKey(FRightRow))
					return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (FLeftTable.Prior())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FRightTable.FindKey(FRightRow))
					return true;
			}
			return false;
		}
    }
    
    // Non Unique Right Searched join algorithm
    // works when only the right input is ordered and the right key is non-unique
    public class NonUniqueRightSearchedJoinTable : RightSearchedJoinTable
    {
		public NonUniqueRightSearchedJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected bool OnRightRow()
		{
			if (FRightTable.BOF() || FRightTable.EOF())
				return false;
				
			Row LCurrentKey = FRightTable.GetKey();
			try
			{
				return CompareKeys(LCurrentKey, FRightRow) == 0;
			}
			finally
			{
				LCurrentKey.Dispose();
			}
		}

		protected bool FindRightKey(bool AForward)
		{
			FRightTable.FindNearest(FRightRow);
			if (OnRightRow())
			{
				if (AForward)
				{
					while (!FRightTable.BOF())
					{
						if (OnRightRow())
							FRightTable.Prior();
						else
						{
							FRightTable.Next();
							break;
						}
					}

					if (FRightTable.BOF())
						FRightTable.Next();
				}
				else
				{
					while (!FRightTable.EOF())
					{
						if (OnRightRow())
							FRightTable.Next();
						else
						{
							FRightTable.Prior();
							break;
						}
					}

					if (FRightTable.EOF())
						FRightTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextLeft()
		{
			while (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FindRightKey(true))
					return true;
			}
			return false;
		}
		
		protected bool PriorLeft()
		{
			while (FLeftTable.Prior())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FindRightKey(false))
					return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (FLeftTable.BOF())
				return NextLeft();
			else
			{
				if (FRightTable.Next())
				{
					FRightTable.Select(FRightRow);
					if (!IsMatch())
						return NextLeft();
					return true;
				}
				else
					return NextLeft();
			}
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.EOF())
				return PriorLeft();
			else
			{
				if (FRightTable.Prior())
				{
					FRightTable.Select(FRightRow);
					if (!IsMatch())
						return PriorLeft();
					return true;
				}
				else
					return PriorLeft();
			}
		}
    }
    
    // Non Unique Left Searched join algorithm
    // works when only the left input is ordered and the left key is non-unique
    public class NonUniqueLeftSearchedJoinTable : LeftSearchedJoinTable
    {
		public NonUniqueLeftSearchedJoinTable(JoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected bool OnLeftRow()
		{
			if (FLeftTable.BOF() || FLeftTable.EOF())
				return false;
				
			Row LCurrentKey = FLeftTable.GetKey();
			try
			{
				return CompareKeys(LCurrentKey, FLeftRow) == 0;
			}
			finally
			{
				LCurrentKey.Dispose();
			}
		}

		protected bool FindLeftKey(bool AForward)
		{
			FLeftTable.FindNearest(FLeftRow);
			if (OnLeftRow())
			{
				if (AForward)
				{
					while (!FLeftTable.BOF())
					{
						if (OnLeftRow())
							FLeftTable.Prior();
						else
						{
							FLeftTable.Next();
							break;
						}
					}
					
					if (FLeftTable.BOF())
						FLeftTable.Next();
				}
				else
				{
					while (!FLeftTable.EOF())
					{
						if (OnLeftRow())
							FLeftTable.Next();
						else
						{
							FLeftTable.Prior();
							break;
						}
					}
					
					if (FLeftTable.EOF())
						FLeftTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextRight()
		{
			while (FRightTable.Next())
			{
				FRightTable.Select(FRightRow);
				GetLeftKey();
				if (FindLeftKey(true))
					return true;
			}
			return false;
		}
		
		protected bool PriorRight()
		{
			while (FRightTable.Prior())
			{
				FRightTable.Select(FRightRow);
				GetLeftKey();
				if (FindLeftKey(false))
					return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (FRightTable.BOF())
				return NextRight();
			else
			{
				if (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					if (!IsMatch())
						return NextRight();
					return true;
				}
				else
					return NextRight();
			}
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.EOF())
				return PriorRight();
			else
			{
				if (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					if (!IsMatch())
						return PriorRight();
					return true;
				}
				else
					return PriorRight();
			}
		}
    }
    
    public abstract class OuterJoinTable : JoinTable
    {
		public OuterJoinTable(OuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		/// <summary>Indicates that the current row in the right cursor is a match for the current row in the left cursor.</summary>
		protected bool FRowFound;
		
		/// <summary>Indicates that some right side match has been found for the current row of the left cursor.</summary>
		protected bool FRowIncluded;
		
		protected override void InternalReset()
		{
			base.InternalReset();
			FRowFound = false;
			FRowIncluded = false;
		}
    }
    
    public abstract class LeftJoinTable : OuterJoinTable
    {
		public LeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		public new LeftOuterJoinNode Node { get { return (LeftOuterJoinNode)FNode; } }
        
		protected override void InternalSelect(Row ARow)
        {
			FLeftTable.Select(ARow);
			if (Node.RowExistsColumnIndex >= 0)
			{
				int LColumnIndex = ARow.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RowExistsColumnIndex].Name);
				if (LColumnIndex >= 0)
					ARow[LColumnIndex] = FRowFound;
			}
			
			if (FRowFound)
				FRightTable.Select(ARow);
			else
			{
				int LColumnIndex;
				foreach (Schema.Column LColumn in FRightTable.DataType.Columns)
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
					if ((LColumnIndex >= 0) && (!Node.IsNatural || !Node.RightKey.Columns.ContainsName(LColumn.Name)))
						ARow.ClearValue(LColumnIndex);
				}
			}
        }
    }

	// Right Unique nested loop left join algorithm
	// right join key unique
    public class RightUniqueNestedLoopLeftJoinTable : LeftJoinTable
    {
		public RightUniqueNestedLoopLeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			if (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				FRowFound = false;
				
				if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
					FRightTable.First();
				else
					FRightTable.Reset();
				while (FRightTable.Next())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
					{
						FRowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.Prior())
			{
				FLeftTable.Select(FLeftRow);
				FRowFound = false;
				
				FRightTable.Last();
				while (FRightTable.Prior())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
					{
						FRowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
		}
    }

	// nested loop left outer join algorithm    
	// works on any inputs
    public class NonUniqueNestedLoopLeftJoinTable : LeftJoinTable
    {
		public NonUniqueNestedLoopLeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

        protected override bool InternalNext()
        {
			if (FLeftTable.BOF())
			{
				FLeftTable.Next();
				FRowFound = false;
				FRowIncluded = false;
			}
				
			while (!FLeftTable.EOF())
			{
				if (!FRightTable.EOF())
				{
					FLeftTable.Select(FLeftRow);
					FRightTable.Next();
					while (!FRightTable.EOF())
					{
						FRightTable.Select(FRightRow);
						if (IsMatch())
						{
							FRowFound = true;
							FRowIncluded = true;
							return true;
						}
						FRightTable.Next();
					}
				}
						
				if ((!FRowFound) && (!FRowIncluded))
				{
					FRowIncluded = true;
					return true;
				}

				FLeftTable.Next();
				if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
					FRightTable.First();
				else
					FRightTable.Reset();
				FRowFound = false;
				FRowIncluded = false;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (FLeftTable.EOF())
			{
				FLeftTable.Prior();
				FRowFound = false;
				FRowIncluded = false;
			}
				
			while (!FLeftTable.BOF())
			{
				if (!FRightTable.BOF())
				{
					FLeftTable.Select(FLeftRow);
					FRightTable.Prior();
					while (!FRightTable.BOF())
					{
						FRightTable.Select(FRightRow);
						if (IsMatch())
						{
							FRowFound = true;
							FRowIncluded = true;
							return true;
						}
						FRightTable.Prior();
					}
				}
						
				if ((!FRowFound) && (!FRowIncluded))
				{
					FRowIncluded = true;
					return true;
				}

				FLeftTable.Prior();
				FRightTable.Last();
				FRowFound = false;
				FRowIncluded = false;
			}
			return false;
        }
    }

	// Merge left join table
	// Works when both inputs are ordered ascending compatible by their join keys
	// left or right keys unique    
    public class MergeLeftJoinTable : LeftJoinTable
    {
		public MergeLeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected void NextLeft()
		{
			if (FLeftTable.Next())
				FLeftTable.Select(FLeftRow);
			FRowIncluded = false;
			FRowFound = false;
		}
		
		protected void PriorLeft()
		{
			if (FLeftTable.Prior())
				FLeftTable.Select(FLeftRow);
			FRowIncluded = false;
			FRowFound = false;
		}
		
		protected void NextRight()
		{
			if (FRightTable.Next())
				FRightTable.Select(FRightRow);
		}
		
		protected void PriorRight()
		{
			if (FRightTable.Prior())
				FRightTable.Select(FRightRow);
		}
		
		protected override bool InternalNext()
		{
			if (FLeftTable.BOF())
				NextLeft();
			if (FRightTable.BOF())
				NextRight();
				
			if (FRowFound)
				if (Node.LeftKey.IsUnique)
					NextRight();
				else
					NextLeft();
				
			int LCompareValue;
			while (!FLeftTable.EOF())
			{
				if (!FRightTable.EOF())
				{
					LCompareValue = CompareKeys(FLeftRow, FRightRow);
					if (LCompareValue == 0)
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					else if (LCompareValue > 0)
					{
						NextRight();
					}
					else
					{
						if (FRowIncluded)
							NextLeft();
						else
						{
							FRowFound = false;
							FRowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (FRowIncluded)
						NextLeft();
					else
					{
						FRowFound = false;
						FRowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.EOF())
				PriorLeft();
			if (FRightTable.EOF())
				PriorRight();
				
			if (FRowFound)
				if (Node.LeftKey.IsUnique)
					PriorRight();				
				else
					PriorLeft();

			int LCompareValue;
			while (!FLeftTable.BOF())
			{
				if (!FRightTable.BOF())
				{
					LCompareValue = CompareKeys(FLeftRow, FRightRow);
					if (LCompareValue == 0)
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					else if (LCompareValue < 0)
					{
						PriorRight();
					}
					else
					{
						if (FRowIncluded)
							PriorLeft();
						else
						{
							FRowFound = false;
							FRowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (FRowIncluded)
						PriorLeft();
					else
					{
						FRowFound = false;
						FRowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
    }
    
	public abstract class SearchedLeftJoinTable : LeftJoinTable
	{
		public SearchedLeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
	}
	
	// right unique searched left join algorithm
	// works when only the right input is ordered by the join key, ascending ignored
	// right join key is unique
	public class RightUniqueSearchedLeftJoinTable : SearchedLeftJoinTable
	{
		public RightUniqueSearchedLeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			if (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				FRowFound = FRightTable.FindKey(FRightRow);
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.Prior())
			{
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				FRowFound = FRightTable.FindKey(FRightRow);
				return true;
			}
			return false;
		}
	}
	
	// non unique searched left join algorithm
	// works when only the right input is ordered by the join key, ascending compatible
	// right join key is nonunique
	public class NonUniqueSearchedLeftJoinTable : SearchedLeftJoinTable
	{
		public NonUniqueSearchedLeftJoinTable(LeftOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected bool OnRightRow()
		{
			if (FRightTable.BOF() || FRightTable.EOF())
				return false;
				
			Row LCurrentKey = FRightTable.GetKey();
			try
			{
				return CompareKeys(LCurrentKey, FRightRow) == 0;
			}
			finally
			{
				LCurrentKey.Dispose();
			}
		}

		protected bool FindRightKey(bool AForward)
		{
			FRightTable.FindNearest(FRightRow);
			if (OnRightRow())
			{
				if (AForward)
				{
					while (!FRightTable.BOF())
					{
						if (OnRightRow())
							FRightTable.Prior();
						else
						{
							FRightTable.Next();
							break;
						}
					}
					if (FRightTable.BOF())
						FRightTable.Next();
				}
				else
				{
					while (!FRightTable.EOF())
					{
						if (OnRightRow())
							FRightTable.Next();
						else
						{
							FRightTable.Prior();
							break;
						}
					}
					if (FRightTable.EOF())
						FRightTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextLeft()
		{
			if (FLeftTable.Next())
			{
				FRowFound = false;
				FRowIncluded = false;
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FindRightKey(true))
				{
					FRowFound = true;
					FRowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected bool PriorLeft()
		{
			if (FLeftTable.Prior())
			{
				FRowFound = false;
				FRowIncluded = false;
				FLeftTable.Select(FLeftRow);
				GetRightKey();
				if (FindRightKey(false))
				{
					FRowFound = true;
					FRowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (FLeftTable.BOF() || !FRowFound)
				return NextLeft();

			if (FRowFound)
			{
				while (FRightTable.Next())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
						return true;
					else
						break;
				}
				return NextLeft();
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.EOF() || !FRowFound)
				return PriorLeft();
			
			if (FRowFound)
			{
				while (FRightTable.Prior())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
						return true;
					else
						break;
				}
				return PriorLeft();
			}
			return false;
		}
	}

    public abstract class RightJoinTable : OuterJoinTable
    {
		public RightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
        public new RightOuterJoinNode Node { get { return (RightOuterJoinNode)FNode; } }
        
        protected override void InternalSelect(Row ARow)
        {
			FRightTable.Select(ARow);
			if (Node.RowExistsColumnIndex >= 0)
			{
				int LColumnIndex = ARow.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RowExistsColumnIndex].Name);
				if (LColumnIndex >= 0)
					ARow[Node.DataType.Columns[Node.RowExistsColumnIndex].Name] = FRowFound;
			}
			
			if (FRowFound)
				FLeftTable.Select(ARow);
			else
			{
				int LColumnIndex;
				foreach (Schema.Column LColumn in FLeftTable.DataType.Columns)
				{
					LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
					if ((LColumnIndex >= 0) && (!Node.IsNatural || !Node.LeftKey.Columns.Contains(LColumn.Name)))
						ARow.ClearValue(LColumnIndex);
				}
			}
        }

        protected override bool InternalBOF()
        {
            return FRightTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
			if (FRightTable.BOF())
			{
				InternalNext();
				if (FRightTable.EOF())
					return true;
				else
				{
					if (FRightTable.Supports(CursorCapability.BackwardsNavigable))
						FRightTable.First();
					else
						FRightTable.Reset();
					if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
						FLeftTable.First();
					else
						FLeftTable.Reset();
					return false;
				}
			}
			return FRightTable.EOF();
        }
    }

	// Left unique nested loop right join algorithm
	// left join key unique
    public class LeftUniqueNestedLoopRightJoinTable : RightJoinTable
    {
		public LeftUniqueNestedLoopRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
        protected override bool InternalNext()
        {
			if (FRightTable.Next())
			{
				FRightTable.Select();
				FRowFound = false;
				if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
					FLeftTable.First();
				else
					FLeftTable.Reset();
					
				while (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
					{
						FRowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (FRightTable.Prior())
			{
				FRightTable.Select();
				FRowFound = false;
				FLeftTable.Last();
				
				while (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
					{
						FRowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
        }
    }

	// nested loop right join algorithm
	// works on any inputs    
    public class NonUniqueNestedLoopRightJoinTable : RightJoinTable
    {
		public NonUniqueNestedLoopRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}

        protected override bool InternalNext()
        {
			if (FRightTable.BOF())
			{
				FRightTable.Next();
				FRowFound = false;
				FRowIncluded = false;
			}
				
			while (!FRightTable.EOF())
			{
				if (!FLeftTable.EOF())
				{
					FRightTable.Select(FRightRow);
					FLeftTable.Next();
					while (!FLeftTable.EOF())
					{
						FLeftTable.Select(FLeftRow);
						if (IsMatch())
						{
							FRowFound = true;
							FRowIncluded = true;
							return true;
						}
						FLeftTable.Next();
					}
				}
						
				if ((!FRowFound) && (!FRowIncluded))
				{
					FRowIncluded = true;
					return true;
				}
				
				FRightTable.Next();
				if (FLeftTable.Supports(CursorCapability.BackwardsNavigable))
					FLeftTable.First();
				else
					FLeftTable.Reset();
				FRowFound = false;
				FRowIncluded = false;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (FRightTable.EOF())
			{
				FRightTable.Prior();
				FRowFound = false;
				FRowIncluded = false;
			}
				
			while (!FRightTable.BOF())
			{
				if (!FLeftTable.BOF())
				{
					FRightTable.Select(FRightRow);
					FLeftTable.Prior();
					while (!FLeftTable.BOF())
					{
						FLeftTable.Select(FLeftRow);
						if (IsMatch())
						{
							FRowFound = true;
							FRowIncluded = true;
							return true;
						}
						FLeftTable.Prior();
					}
				}
					
				if ((!FRowFound) && (!FRowIncluded))
				{
					FRowIncluded = true;
					return true;
				}
				
				FRightTable.Prior();
				FLeftTable.Last();
				FRowFound = false;
				FRowIncluded = false;
			}
			return false;
        }
    }
    
    // unique merge right join algorithm
    // works when both inputs are ordered by their join keys, ascending ignored
    // left or right join keys unique
    public class MergeRightJoinTable : RightJoinTable
    {
		public MergeRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		protected void NextRight()
		{
			if (FRightTable.Next())
				FRightTable.Select(FRightRow);
			FRowIncluded = false;
			FRowFound = false;
		}
		
		protected void PriorRight()
		{
			if (FRightTable.Prior())
				FRightTable.Select(FRightRow);
			FRowIncluded = false;
			FRowFound = false;
		}
		
		protected void NextLeft()
		{
			if (FLeftTable.Next())
				FLeftTable.Select(FLeftRow);
		}
		
		protected void PriorLeft()
		{
			if (FLeftTable.Prior())
				FLeftTable.Select(FLeftRow);
		}
		
		protected override bool InternalNext()
		{
			if (FRightTable.BOF())
				NextRight();
			if (FLeftTable.BOF())
				NextLeft();
				
			if (FRowFound)
				if (Node.RightKey.IsUnique)
					NextLeft();
				else
					NextRight();
				
			int LCompareValue;
			while (!FRightTable.EOF())
			{
				if (!FLeftTable.EOF())
				{
					LCompareValue = CompareKeys(FRightRow, FLeftRow);
					if (LCompareValue == 0)
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					else if (LCompareValue > 0)
					{
						NextLeft();
					}
					else
					{
						if (FRowIncluded)
							NextRight();
						else
						{
							FRowFound = false;
							FRowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (FRowIncluded)
						NextRight();
					else
					{
						FRowFound = false;
						FRowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.EOF())
				PriorRight();
			if (FLeftTable.EOF())
				PriorLeft();
				
			if (FRowFound)
				if (Node.RightKey.IsUnique)
					PriorLeft();				
				else
					PriorRight();

			int LCompareValue;
			while (!FRightTable.BOF())
			{
				if (!FLeftTable.BOF())
				{
					LCompareValue = CompareKeys(FRightRow, FLeftRow);
					if (LCompareValue == 0)
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					else if (LCompareValue < 0)
					{
						PriorLeft();
					}
					else
					{
						if (FRowIncluded)
							PriorRight();
						else
						{
							FRowFound = false;
							FRowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (FRowIncluded)
						PriorRight();
					else
					{
						FRowFound = false;
						FRowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
	}

    public class UniqueMergeRightJoinTable : RightJoinTable
    {
		public UniqueMergeRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		protected override bool InternalNext()
		{
			if (FRightTable.Next())
			{
				FRightTable.Select(FRightRow);
				FRowFound = false;

				while (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
					{
						FRowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.Prior())
			{
				FRightTable.Select(FRightRow);
				FRowFound = false;
				
				while (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
					{
						FRowFound = true;
						return true;
					}
				}
				return true;
			}			
			return false;
		}
    }
    
    // left unique merge right join algorithm
    // works when both inputs are ordered by their join keys, ascending ignored
    // left join key is unique, right join key is not
	public class LeftUniqueMergeRightJoinTable : RightJoinTable
	{
		public LeftUniqueMergeRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			if (FRightTable.BOF())
				if (FRightTable.Next())
				{
					FRightTable.Select(FRightRow);
					FRowFound = false;
					FRowIncluded = false;
				}
				else
					return false;
					
			if (!FRowFound && FRowIncluded)
			{
				if (FRightTable.Next())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					FRowFound = false;
					FRowIncluded = false;
				}
			}

			if (FLeftTable.Next())
			{
				FLeftTable.Select(FLeftRow);
				if (IsMatch())
				{
					FRowFound = true;
					FRowIncluded = true;
					return true;
				}
			}
			
			if (!FRowIncluded)
			{
				FRowIncluded = true;
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.EOF())
				if (FRightTable.Prior())
				{
					FRightTable.Select(FRightRow);
					FRowFound = false;
					FRowIncluded = false;
				}
				else
					return false;
					
			if (!FRowFound && FRowIncluded)
			{
				if (FRightTable.Prior())
				{
					FRightTable.Select(FRightRow);
					if (IsMatch())
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					FRowFound = false;
					FRowIncluded = false;
				}
			}

			if (FLeftTable.Prior())
			{
				FLeftTable.Select(FLeftRow);
				if (IsMatch())
				{
					FRowFound = true;
					FRowIncluded = true;
					return true;
				}
			}
			
			if (!FRowIncluded)
			{
				FRowIncluded = true;
				return true;
			}
			return false;
		}
	}
    
    // right unique merge right join algorithm
    // works when both inputs are ordered by their join keys, ascending ignored
    // right join key is unique, left join key is not
	public class RightUniqueMergeRightJoinTable : RightJoinTable
	{
		public RightUniqueMergeRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		protected override bool InternalNext()
		{
			if (FLeftTable.BOF())
				if (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					FRowFound = false;
					FRowIncluded = false;
				}
				else
					return false;
					
			if (!FRowFound && FRowIncluded)
			{
				if (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					FRowFound = false;
					FRowIncluded = false;
				}
			}

			if (FRightTable.Next())
			{
				FRightTable.Select(FRightRow);
				if (IsMatch())
				{
					FRowFound = true;
					FRowIncluded = true;
					return true;
				}
			}
			
			if (!FRowIncluded)
			{
				FRowIncluded = true;
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FLeftTable.EOF())
				if (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					FRowFound = false;
					FRowIncluded = false;
				}
				else
					return false;
					
			if (!FRowFound && FRowIncluded)
			{
				if (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
					{
						FRowFound = true;
						FRowIncluded = true;
						return true;
					}
					FRowFound = false;
					FRowIncluded = false;
				}
			}

			if (FRightTable.Prior())
			{
				FRightTable.Select(FRightRow);
				if (IsMatch())
				{
					FRowFound = true;
					FRowIncluded = true;
					return true;
				}
			}
			
			if (!FRowIncluded)
			{
				FRowIncluded = true;
				return true;
			}
			
			return false;
		}
	}
    
	public abstract class SearchedRightJoinTable : RightJoinTable
	{
		public SearchedRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
	}
	
    // left unique searched right join algorithm
    // works when only the left input is ordered by the join key, ascending ignored
    // left join key is unique
	public class LeftUniqueSearchedRightJoinTable : SearchedRightJoinTable
	{
		public LeftUniqueSearchedRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}
		
		protected override bool InternalNext()
		{
			if (FRightTable.Next())
			{
				FRightTable.Select(FRightRow);
				GetLeftKey();
				FRowFound = FLeftTable.FindKey(FLeftRow);
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.Prior())
			{
				FRightTable.Select(FRightRow);
				GetLeftKey();
				FRowFound = FLeftTable.FindKey(FLeftRow);
				return true;
			}
			return false;
		}
	}
    
    // non unique searched right join algorithm
    // works when only the left input is ordered by the join key, ascending ignored
    // left join key is nonunique
    public class NonUniqueSearchedRightJoinTable : SearchedRightJoinTable
    {
		public NonUniqueSearchedRightJoinTable(RightOuterJoinNode ANode, ServerProcess AProcess) : base(ANode, AProcess) {}

		protected bool OnLeftRow()
		{
			if (FLeftTable.BOF() || FLeftTable.EOF())
				return false;
				
			Row LCurrentKey = FLeftTable.GetKey();
			try
			{
				return CompareKeys(LCurrentKey, FLeftRow) == 0;
			}
			finally
			{
				LCurrentKey.Dispose();
			}
		}

		protected bool FindLeftKey(bool AForward)
		{
			FLeftTable.FindNearest(FLeftRow);
			if (OnLeftRow())
			{
				if (AForward)
				{
					while (!FLeftTable.BOF())
					{
						if (OnLeftRow())
							FLeftTable.Prior();
						else
						{
							FLeftTable.Next();
							break;
						}
					}
					if (FLeftTable.BOF())
						FLeftTable.Next();
				}
				else
				{
					while (!FLeftTable.EOF())
					{
						if (OnLeftRow())
							FLeftTable.Next();
						else
						{
							FLeftTable.Prior();
							break;
						}
					}
					if (FLeftTable.EOF())
						FLeftTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextRight()
		{
			if (FRightTable.Next())
			{
				FRowFound = false;
				FRowIncluded = false;
				FRightTable.Select(FRightRow);
				GetLeftKey();
				if (FindLeftKey(true))
				{
					FRowFound = true;
					FRowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected bool PriorRight()
		{
			if (FRightTable.Prior())
			{
				FRowFound = false;
				FRowIncluded = false;
				FRightTable.Select(FRightRow);
				GetLeftKey();
				if (FindLeftKey(false))
				{
					FRowFound = true;
					FRowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (FRightTable.BOF() || !FRowFound)
				return NextRight();

			if (FRowFound)
			{
				while (FLeftTable.Next())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
						return true;
					else
						break;
				}
				return NextRight();
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (FRightTable.EOF() || !FRowFound)
				return PriorRight();
			
			if (FRowFound)
			{
				while (FLeftTable.Prior())
				{
					FLeftTable.Select(FLeftRow);
					if (IsMatch())
						return true;
					else
						break;
				}
				return PriorRight();
			}
			return false;
		}
    }
}