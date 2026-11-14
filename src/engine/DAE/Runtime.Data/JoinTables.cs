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
		public ConditionedTable(ConditionedTableNode node, Program program) : base(node, program) {}
		
		public new ConditionedTableNode Node { get { return (ConditionedTableNode)_node; } }

		protected ITable _leftTable;
		protected ITable _rightTable;
		protected IRow _leftRow;
		protected IRow _rightRow;

        protected override void InternalOpen()
        {
			_leftTable = (ITable)Node.Nodes[0].Execute(Program);
			_leftRow = new Row(Manager, new Schema.RowType(Node.LeftKey.Columns));
			_rightTable = (ITable)Node.Nodes[1].Execute(Program);
			_rightRow = new Row(Manager, new Schema.RowType(Node.RightKey.Columns));
        }
        
        protected override void InternalClose()
        {
			if (_leftRow != null)
			{
				_leftRow.Dispose();
				_leftRow = null;
			}
			
			if (_leftTable != null)
			{
				_leftTable.Dispose();
				_leftTable = null;
			}

			if (_rightRow != null)
			{
				_rightRow.Dispose();
				_rightRow = null;
			}

			if (_rightTable != null)
			{
				_rightTable.Dispose();
				_rightTable = null;
			}
        }

        protected override void InternalReset()
        {
			_leftTable.Reset();
			_rightTable.Reset();
        }

        protected override bool InternalBOF()
        {
            return _leftTable.BOF();
        }

        protected override bool InternalEOF()
        {
			if (_leftTable.BOF())
			{
				InternalNext();
				if (_leftTable.EOF())
					return true;
				else
				{
					if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
						_leftTable.First();
					else
						_leftTable.Reset();
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
					return false;
				}
			}
			return _leftTable.EOF();
        }
        
        protected override void InternalLast()
        {
			_leftTable.Last();
			_rightTable.Last();
        }

        protected override void InternalFirst()
        {
			_leftTable.First();
			_rightTable.First();
        }
        
        protected bool IsMatch()
        {
			return CompareKeys(_leftRow, _rightRow) == 0;
        }

        protected int CompareKeyValues(object keyValue1, object keyValue2, PlanNode compareNode)
        {
			Program.Stack.Push(keyValue1);
			Program.Stack.Push(keyValue2);
			int result = (int)compareNode.Execute(Program);
			Program.Stack.Pop();
			Program.Stack.Pop();
			return result;
        }
        
        protected int CompareKeys(IRow key1, IRow key2)
        {
			int result = 0;
			for (int index = 0; index < Node.JoinOrder.Columns.Count; index++)
			{
				if ((index >= key1.DataType.Columns.Count) || !key1.HasValue(index))
					return -1;
				else if ((index >= key2.DataType.Columns.Count) || !key2.HasValue(index))
					return 1;
				else
				{
					result = CompareKeyValues(key1[index], key2[index], Node.JoinOrder.Columns[index].Sort.CompareNode) * (Node.JoinOrder.Columns[index].Ascending ? 1 : -1);
					if (result != 0)
						return result;
				}
			}
			return result;
        }

		// Gets the right key row for the current left row		
		protected void GetRightKey()
		{
			for (int index = 0; index < Node.LeftKey.Columns.Count; index++)
				if (_leftRow.HasValue(index))
					_rightRow[index] = _leftRow[index];
				else
					_rightRow.ClearValue(index);
		}

		// Gets the left key row for the current right row		
		protected void GetLeftKey()
		{
			for (int index = 0; index < Node.RightKey.Columns.Count; index++)
				if (_rightRow.HasValue(index))
					_leftRow[index] = _rightRow[index];
				else
					_leftRow.ClearValue(index);
		}
	}
	
	public abstract class SemiTable : ConditionedTable
	{
		public SemiTable(SemiTableNode node, Program program) : base(node, program) {}
		
        protected override void InternalSelect(IRow row)
        {
			_leftTable.Select(row);
        }
	}
	
	public abstract class HavingTable : SemiTable
	{
		public HavingTable(HavingNode node, Program program) : base(node, program) {}
	}
	
	/*
	// TODO: Implement Merge having algorithm
	public class MergeHavingTable : HavingTable
	{
		public MergeHavingTable(HavingNode ANode, Program AProgram) : base(ANode, AProgram) {}
	}
	*/
	
	public class SearchedHavingTable : HavingTable
	{
		public SearchedHavingTable(HavingNode node, Program program) : base(node, program) {}

		protected override bool InternalNext()
		{
			while (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (_rightTable.FindKey(_rightRow))
					return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (_rightTable.FindKey(_rightRow))
					return true;
			}
			return false;
		}
	}
	
	public abstract class WithoutTable : SemiTable
	{
		public WithoutTable(WithoutNode node, Program program) : base(node, program) {}
	}
	
	/*
	// TODO: Implement Merge without algorithm
	public class MergeWithoutTable : WithoutTable
	{
		public MergeWithoutTable(WithoutNode ANode, Program AProgram) : base(ANode, AProgram) {}
	}
	*/
	
	public class SearchedWithoutTable : WithoutTable
	{
		public SearchedWithoutTable(WithoutNode node, Program program) : base(node, program) {}

		protected bool _bOF;

        protected override void InternalOpen()
        {
			base.InternalOpen();
			_bOF = true;
        }

        protected override void InternalReset()
        {
			_leftTable.Reset();
			_bOF = true;
        }
        
        protected override void InternalLast()
        {
			_leftTable.Last();
        }
        
        protected override bool InternalBOF()
        {
			return _bOF;
        }
        
        protected override bool InternalEOF()
        {
			if (_bOF)
			{
				InternalNext();
				if (_leftTable.EOF())
					return true;
				else
				{
					if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
						_leftTable.First();
					else
						_leftTable.Reset();
					_bOF = true;
					return false;
				}
			}
			return _leftTable.EOF();
        }
        
        protected override void InternalFirst()
        {
			_leftTable.First();
			_bOF = true;
        }

		protected override bool InternalNext()
		{
			while (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (!_rightTable.FindKey(_rightRow))
				{
					_bOF = false;
					return true;
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (!_rightTable.FindKey(_rightRow))
					return true;
			}
			_bOF = true;
			return false;
		}
	}

    public abstract class JoinTable : ConditionedTable
    {
		public JoinTable(JoinNode node, Program program) : base(node, program) {}
		
		public new JoinNode Node { get { return (JoinNode)_node; } }

        protected override void InternalSelect(IRow row)
        {
			_leftTable.Select(row);
			_rightTable.Select(row);
        }
    }
    
    // Left Unique nested loop join algorithm
    // left join key unique
    public class LeftUniqueNestedLoopJoinTable : JoinTable
    {
		public LeftUniqueNestedLoopJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalBOF()
		{
			return _rightTable.BOF();
		}
		
		protected override bool InternalEOF()
		{
			if (_rightTable.BOF())
			{
				InternalNext();
				if (_rightTable.EOF())
					return true;
				else
				{
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
					if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
						_leftTable.First();
					else
						_leftTable.Reset();
					return false;
				}
			}
			return _rightTable.EOF();
		}
		
		protected override bool InternalNext()
		{
			while (_rightTable.Next())
			{
				if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
					_leftTable.First();
				else
					_leftTable.Reset();
				_rightTable.Select(_rightRow);
				while (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
						return true;
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (_rightTable.Prior())
			{
				_leftTable.Last();
				_rightTable.Select(_rightRow);
				while (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
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
		public RightUniqueNestedLoopJoinTable(JoinNode node, Program program) : base(node, program) {}
		
        protected override bool InternalNext()
        {
			while (_leftTable.Next())
			{
				if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
					_rightTable.First();
				else
					_rightTable.Reset();
				_leftTable.Select(_leftRow);
				while (_rightTable.Next())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
						return true;
				}
			}
			return false;
		}
			
        protected override bool InternalPrior()
        {
			while (_leftTable.Prior())
			{
				_rightTable.Last();
				_leftTable.Select(_leftRow);
				while (_rightTable.Prior())
				{
					_rightTable.Select(_rightRow);
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
		public NonUniqueNestedLoopJoinTable(JoinNode node, Program program) : base(node, program) {}

        protected override bool InternalNext()
        {
			if (_leftTable.BOF())
				_leftTable.Next();
				
			while (!_leftTable.EOF())
			{
				_leftTable.Select(_leftRow);
				_rightTable.Next();
				while (!_rightTable.EOF())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
						return true;
					_rightTable.Next();
				}
				_leftTable.Next();
				if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
					_rightTable.First();
				else
					_rightTable.Reset();
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (_leftTable.EOF())
				_leftTable.Prior();
				
			while (!_leftTable.BOF())
			{
				_leftTable.Select(_leftRow);
				_rightTable.Prior();
				while (!_rightTable.BOF())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
						return true;
					_rightTable.Prior();
				}
				_leftTable.Prior();
				_rightTable.Last();
			}
			return false;
        }
    }

	// times algorithm, works on any input
    public class TimesTable : JoinTable
    {
		public TimesTable(JoinNode node, Program program) : base(node, program) {}
		
		private bool _hitEOF;
		private bool _hitBOF;

		protected override void InternalReset()
		{
			base.InternalReset();
			_hitBOF = false;
			_hitEOF = false;
		}

		protected override void InternalFirst()
		{
			base.InternalFirst();
			_hitBOF = true;
			_hitEOF = false;
		}

		protected override void InternalLast()
		{
			base.InternalLast();
			_hitEOF = true;
			_hitBOF = false;
		}
		
		protected override bool InternalNext()
		{
			if (_hitEOF)
				return false;
				
			while (true)
			{
				if (_rightTable.Next())
				{
					if (_leftTable.BOF())
					{
						_hitEOF = !_leftTable.Next();
						if (!_hitEOF)
							_hitBOF = false;
						return !_hitEOF;
					}
					_hitBOF = false;
					return true;
				}
				else
				{
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
						
					if (!_leftTable.Next())
					{
						_hitEOF = true;
						return false;
					}
				}				
			}
		}
		
		protected override bool InternalPrior()
		{
			while (true)
			{
				if (_hitBOF)
					return false;
					
				if (_rightTable.Prior())
				{
					if (_leftTable.EOF())
					{
						_hitBOF = !_leftTable.Prior();
						if (!_hitBOF)
							_hitEOF = false;
						return !_hitBOF;
					}
					_hitEOF = false;
					return true;
				}
				else
				{
					_rightTable.Last();
				
					if (!_leftTable.Prior())
					{
						_hitBOF = true;
						return false;
					}
				}
			}
		}

        protected override void InternalSelect(IRow row)
        {
			base.InternalSelect(row);
			OuterJoinNode outerJoinNode = Node as OuterJoinNode;
			if ((outerJoinNode != null) && outerJoinNode.RowExistsColumnIndex >= 0)
			{
				int columnIndex = row.DataType.Columns.IndexOfName(Node.DataType.Columns[outerJoinNode.RowExistsColumnIndex].Name);
				if (columnIndex >= 0)
					row[columnIndex] = true;
			}
        }
    }

	// unique merge join algorithm
	// both inputs are ordered by the join keys, ascending in the same order
	// left and right join keys are unique
    public class UniqueMergeJoinTable : JoinTable
    {
		public UniqueMergeJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			int compareValue;			
			_leftTable.Next();
			_rightTable.Next();
			while (!(_leftTable.EOF() || _rightTable.EOF()))
			{
				_leftTable.Select(_leftRow);
				_rightTable.Select(_rightRow);
				compareValue = CompareKeys(_leftRow, _rightRow);
				if (compareValue == 0)
					return true;
				else if (compareValue < 0)
					_leftTable.Next();
				else
					_rightTable.Next();
			}
			_leftTable.Last();
			_rightTable.Last();
			return false;
		}
		
		protected override bool InternalPrior()
		{
			int compareValue;
			_leftTable.Prior();
			_rightTable.Prior();
			while (!(_leftTable.BOF() || _rightTable.BOF()))
			{
				_leftTable.Select(_leftRow);
				_rightTable.Select(_rightRow);
				compareValue = CompareKeys(_leftRow, _rightRow);
				if (compareValue == 0)
					return true;
				else if (compareValue < 0)
					_rightTable.Prior();
				else
					_leftTable.Prior();
			}
			_leftTable.First();
			_rightTable.First();
			return false;
		}
    }

	// Left unique merge join algorithm    
	// left and right inputs ordered by the join keys, ascending in the same order
	// left key is unique, right key is non-unique
    public class LeftUniqueMergeJoinTable : JoinTable
    {
		public LeftUniqueMergeJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			if (_leftTable.BOF() && _leftTable.Next())
				_leftTable.Select(_leftRow);
			_rightTable.Next();
			int compareValue;
			while (!(_leftTable.EOF() || _rightTable.EOF()))
			{
				_rightTable.Select(_rightRow);
				compareValue = CompareKeys(_leftRow, _rightRow);
				if (compareValue == 0)
					return true;
				else if (compareValue < 0)
				{
					if (_leftTable.Next())
						_leftTable.Select(_leftRow);
				}
				else
					_rightTable.Next();
			}
			_leftTable.Last();
			_rightTable.Last();
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_leftTable.EOF() && _leftTable.Prior())
				_leftTable.Select(_leftRow);
			_rightTable.Prior();
			int compareValue;
			while (!(_leftTable.BOF() || _rightTable.BOF()))
			{
				_rightTable.Select(_rightRow);
				compareValue = CompareKeys(_leftRow, _rightRow);
				if (compareValue == 0)
					return true;
				else if (compareValue < 0)
					_rightTable.Prior();
				else
				{
					if (_leftTable.Prior())
						_leftTable.Select(_leftRow);
				}
			}
			_leftTable.First();
			_rightTable.First();
			return false;
		}
    }

	// Right unique merge join algorithm
	// left and right inputs are ordered by the join keys, ascending in the same order
	// left key is non-unique, right key is unique
    public class RightUniqueMergeJoinTable : JoinTable
    {
		public RightUniqueMergeJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			if (_rightTable.BOF() && _rightTable.Next())
				_rightTable.Select(_rightRow);
			_leftTable.Next();
			int compareValue;
			while (!(_leftTable.EOF() || _rightTable.EOF()))
			{
				_leftTable.Select(_leftRow);
				compareValue = CompareKeys(_leftRow, _rightRow);
				if (compareValue == 0)
					return true;
				else if (compareValue < 0)
					_leftTable.Next();
				else
				{
					if (_rightTable.Next())
						_rightTable.Select(_rightRow);
				}
			}
			_rightTable.Last();
			_leftTable.Last();
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_rightTable.EOF() && _rightTable.Prior())
				_rightTable.Select(_rightRow);
			_leftTable.Prior();
			int compareValue;
			while (!(_leftTable.BOF() || _rightTable.BOF()))
			{
				_leftTable.Select(_leftRow);
				compareValue = CompareKeys(_leftRow, _rightRow);
				if (compareValue == 0)
					return true;
				else if (compareValue < 0)
				{
					if (_rightTable.Prior())
						_rightTable.Select(_rightRow);
				}
				else
					_leftTable.Prior();
			}
			_rightTable.First();
			_leftTable.First();
			return false;
		}
    }
    
    public abstract class RightSearchedJoinTable : JoinTable
    {
		public RightSearchedJoinTable(JoinNode node, Program program) : base(node, program) {}
    }
    
    public abstract class LeftSearchedJoinTable : JoinTable
    {
		public LeftSearchedJoinTable(JoinNode node, Program program) : base(node, program) {}
		
        protected override bool InternalBOF()
        {
            return _rightTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
			if (_rightTable.BOF())
			{
				InternalNext();
				if (_rightTable.EOF())
					return true;
				else
				{
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
					if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
						_leftTable.First();
					else
						_leftTable.Reset();
					return false;
				}
			}
			return _rightTable.EOF();
        }
    }
    
    // Left Unique Searched join algorithm
    // works when only the left input is ordered and the left key is unique
    public class LeftUniqueSearchedJoinTable : LeftSearchedJoinTable
    {
		public LeftUniqueSearchedJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			while (_rightTable.Next())
			{
				_rightTable.Select(_rightRow);
				GetLeftKey();
				if (_leftTable.FindKey(_leftRow))
					return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (_rightTable.Prior())
			{
				_rightTable.Select(_rightRow);
				GetLeftKey();
				if (_leftTable.FindKey(_leftRow))
					return true;
			}
			return false;
		}
    }
    
	// Right Unique Searched join algorithm
	// works when only the right input is ordered and the right key is unique
    public class RightUniqueSearchedJoinTable : RightSearchedJoinTable
    {
		public RightUniqueSearchedJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			while (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (_rightTable.FindKey(_rightRow))
					return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			while (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (_rightTable.FindKey(_rightRow))
					return true;
			}
			return false;
		}
    }
    
    // Non Unique Right Searched join algorithm
    // works when only the right input is ordered and the right key is non-unique
    public class NonUniqueRightSearchedJoinTable : RightSearchedJoinTable
    {
		public NonUniqueRightSearchedJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected bool OnRightRow()
		{
			if (_rightTable.BOF() || _rightTable.EOF())
				return false;
				
			IRow currentKey = _rightTable.GetKey();
			try
			{
				return CompareKeys(currentKey, _rightRow) == 0;
			}
			finally
			{
				currentKey.Dispose();
			}
		}

		protected bool FindRightKey(bool forward)
		{
			_rightTable.FindNearest(_rightRow);
			if (OnRightRow())
			{
				if (forward)
				{
					while (!_rightTable.BOF())
					{
						if (OnRightRow())
							_rightTable.Prior();
						else
						{
							_rightTable.Next();
							break;
						}
					}

					if (_rightTable.BOF())
						_rightTable.Next();
				}
				else
				{
					while (!_rightTable.EOF())
					{
						if (OnRightRow())
							_rightTable.Next();
						else
						{
							_rightTable.Prior();
							break;
						}
					}

					if (_rightTable.EOF())
						_rightTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextLeft()
		{
			while (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (FindRightKey(true))
					return true;
			}
			return false;
		}
		
		protected bool PriorLeft()
		{
			while (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (FindRightKey(false))
					return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (_leftTable.BOF())
				return NextLeft();
			else
			{
				if (_rightTable.Next())
				{
					_rightTable.Select(_rightRow);
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
			if (_leftTable.EOF())
				return PriorLeft();
			else
			{
				if (_rightTable.Prior())
				{
					_rightTable.Select(_rightRow);
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
		public NonUniqueLeftSearchedJoinTable(JoinNode node, Program program) : base(node, program) {}
		
		protected bool OnLeftRow()
		{
			if (_leftTable.BOF() || _leftTable.EOF())
				return false;
				
			IRow currentKey = _leftTable.GetKey();
			try
			{
				return CompareKeys(currentKey, _leftRow) == 0;
			}
			finally
			{
				currentKey.Dispose();
			}
		}

		protected bool FindLeftKey(bool forward)
		{
			_leftTable.FindNearest(_leftRow);
			if (OnLeftRow())
			{
				if (forward)
				{
					while (!_leftTable.BOF())
					{
						if (OnLeftRow())
							_leftTable.Prior();
						else
						{
							_leftTable.Next();
							break;
						}
					}
					
					if (_leftTable.BOF())
						_leftTable.Next();
				}
				else
				{
					while (!_leftTable.EOF())
					{
						if (OnLeftRow())
							_leftTable.Next();
						else
						{
							_leftTable.Prior();
							break;
						}
					}
					
					if (_leftTable.EOF())
						_leftTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextRight()
		{
			while (_rightTable.Next())
			{
				_rightTable.Select(_rightRow);
				GetLeftKey();
				if (FindLeftKey(true))
					return true;
			}
			return false;
		}
		
		protected bool PriorRight()
		{
			while (_rightTable.Prior())
			{
				_rightTable.Select(_rightRow);
				GetLeftKey();
				if (FindLeftKey(false))
					return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (_rightTable.BOF())
				return NextRight();
			else
			{
				if (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
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
			if (_rightTable.EOF())
				return PriorRight();
			else
			{
				if (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
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
		public OuterJoinTable(OuterJoinNode node, Program program) : base(node, program) {}
		
		/// <summary>Indicates that the current row in the right cursor is a match for the current row in the left cursor.</summary>
		protected bool _rowFound;
		
		/// <summary>Indicates that some right side match has been found for the current row of the left cursor.</summary>
		protected bool _rowIncluded;
		
		protected override void InternalReset()
		{
			base.InternalReset();
			_rowFound = false;
			_rowIncluded = false;
		}
    }
    
    public abstract class LeftJoinTable : OuterJoinTable
    {
		public LeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}

		public new LeftOuterJoinNode Node { get { return (LeftOuterJoinNode)_node; } }
        
		protected override void InternalSelect(IRow row)
        {
			_leftTable.Select(row);
			if (Node.RowExistsColumnIndex >= 0)
			{
				int columnIndex = row.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RowExistsColumnIndex].Name);
				if (columnIndex >= 0)
					row[columnIndex] = _rowFound;
			}
			
			if (_rowFound)
				_rightTable.Select(row);
			else
			{
				int columnIndex;
				foreach (Schema.Column column in _rightTable.DataType.Columns)
				{
					columnIndex = row.DataType.Columns.IndexOfName(column.Name);
					if ((columnIndex >= 0) && (!Node.IsNatural || !Node.RightKey.Columns.ContainsName(column.Name)))
						row.ClearValue(columnIndex);
				}
			}
        }
    }

	// Right Unique nested loop left join algorithm
	// right join key unique
    public class RightUniqueNestedLoopLeftJoinTable : LeftJoinTable
    {
		public RightUniqueNestedLoopLeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			if (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				_rowFound = false;
				
				if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
					_rightTable.First();
				else
					_rightTable.Reset();
				while (_rightTable.Next())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
					{
						_rowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				_rowFound = false;
				
				_rightTable.Last();
				while (_rightTable.Prior())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
					{
						_rowFound = true;
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
		public NonUniqueNestedLoopLeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}

        protected override bool InternalNext()
        {
			if (_leftTable.BOF())
			{
				_leftTable.Next();
				_rowFound = false;
				_rowIncluded = false;
			}
				
			while (!_leftTable.EOF())
			{
				if (!_rightTable.EOF())
				{
					_leftTable.Select(_leftRow);
					_rightTable.Next();
					while (!_rightTable.EOF())
					{
						_rightTable.Select(_rightRow);
						if (IsMatch())
						{
							_rowFound = true;
							_rowIncluded = true;
							return true;
						}
						_rightTable.Next();
					}
				}
						
				if ((!_rowFound) && (!_rowIncluded))
				{
					_rowIncluded = true;
					return true;
				}

				_leftTable.Next();
				if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
					_rightTable.First();
				else
					_rightTable.Reset();
				_rowFound = false;
				_rowIncluded = false;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (_leftTable.EOF())
			{
				_leftTable.Prior();
				_rowFound = false;
				_rowIncluded = false;
			}
				
			while (!_leftTable.BOF())
			{
				if (!_rightTable.BOF())
				{
					_leftTable.Select(_leftRow);
					_rightTable.Prior();
					while (!_rightTable.BOF())
					{
						_rightTable.Select(_rightRow);
						if (IsMatch())
						{
							_rowFound = true;
							_rowIncluded = true;
							return true;
						}
						_rightTable.Prior();
					}
				}
						
				if ((!_rowFound) && (!_rowIncluded))
				{
					_rowIncluded = true;
					return true;
				}

				_leftTable.Prior();
				_rightTable.Last();
				_rowFound = false;
				_rowIncluded = false;
			}
			return false;
        }
    }

	// Merge left join table
	// Works when both inputs are ordered ascending compatible by their join keys
	// left or right keys unique    
    public class MergeLeftJoinTable : LeftJoinTable
    {
		public MergeLeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}
		
		protected void NextLeft()
		{
			if (_leftTable.Next())
				_leftTable.Select(_leftRow);
			_rowIncluded = false;
			_rowFound = false;
		}
		
		protected void PriorLeft()
		{
			if (_leftTable.Prior())
				_leftTable.Select(_leftRow);
			_rowIncluded = false;
			_rowFound = false;
		}
		
		protected void NextRight()
		{
			if (_rightTable.Next())
				_rightTable.Select(_rightRow);
		}
		
		protected void PriorRight()
		{
			if (_rightTable.Prior())
				_rightTable.Select(_rightRow);
		}
		
		protected override bool InternalNext()
		{
			if (_leftTable.BOF())
				NextLeft();
			if (_rightTable.BOF())
				NextRight();
				
			if (_rowFound)
				if (Node.LeftKey.IsUnique)
					NextRight();
				else
					NextLeft();
				
			int compareValue;
			while (!_leftTable.EOF())
			{
				if (!_rightTable.EOF())
				{
					compareValue = CompareKeys(_leftRow, _rightRow);
					if (compareValue == 0)
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					else if (compareValue > 0)
					{
						NextRight();
					}
					else
					{
						if (_rowIncluded)
							NextLeft();
						else
						{
							_rowFound = false;
							_rowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (_rowIncluded)
						NextLeft();
					else
					{
						_rowFound = false;
						_rowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_leftTable.EOF())
				PriorLeft();
			if (_rightTable.EOF())
				PriorRight();
				
			if (_rowFound)
				if (Node.LeftKey.IsUnique)
					PriorRight();				
				else
					PriorLeft();

			int compareValue;
			while (!_leftTable.BOF())
			{
				if (!_rightTable.BOF())
				{
					compareValue = CompareKeys(_leftRow, _rightRow);
					if (compareValue == 0)
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					else if (compareValue < 0)
					{
						PriorRight();
					}
					else
					{
						if (_rowIncluded)
							PriorLeft();
						else
						{
							_rowFound = false;
							_rowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (_rowIncluded)
						PriorLeft();
					else
					{
						_rowFound = false;
						_rowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
    }
    
	public abstract class SearchedLeftJoinTable : LeftJoinTable
	{
		public SearchedLeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}
	}
	
	// right unique searched left join algorithm
	// works when only the right input is ordered by the join key, ascending ignored
	// right join key is unique
	public class RightUniqueSearchedLeftJoinTable : SearchedLeftJoinTable
	{
		public RightUniqueSearchedLeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			if (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				_rowFound = _rightTable.FindKey(_rightRow);
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				GetRightKey();
				_rowFound = _rightTable.FindKey(_rightRow);
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
		public NonUniqueSearchedLeftJoinTable(LeftOuterJoinNode node, Program program) : base(node, program) {}
		
		protected bool OnRightRow()
		{
			if (_rightTable.BOF() || _rightTable.EOF())
				return false;
				
			IRow currentKey = _rightTable.GetKey();
			try
			{
				return CompareKeys(currentKey, _rightRow) == 0;
			}
			finally
			{
				currentKey.Dispose();
			}
		}

		protected bool FindRightKey(bool forward)
		{
			_rightTable.FindNearest(_rightRow);
			if (OnRightRow())
			{
				if (forward)
				{
					while (!_rightTable.BOF())
					{
						if (OnRightRow())
							_rightTable.Prior();
						else
						{
							_rightTable.Next();
							break;
						}
					}
					if (_rightTable.BOF())
						_rightTable.Next();
				}
				else
				{
					while (!_rightTable.EOF())
					{
						if (OnRightRow())
							_rightTable.Next();
						else
						{
							_rightTable.Prior();
							break;
						}
					}
					if (_rightTable.EOF())
						_rightTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextLeft()
		{
			if (_leftTable.Next())
			{
				_rowFound = false;
				_rowIncluded = false;
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (FindRightKey(true))
				{
					_rowFound = true;
					_rowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected bool PriorLeft()
		{
			if (_leftTable.Prior())
			{
				_rowFound = false;
				_rowIncluded = false;
				_leftTable.Select(_leftRow);
				GetRightKey();
				if (FindRightKey(false))
				{
					_rowFound = true;
					_rowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (_leftTable.BOF() || !_rowFound)
				return NextLeft();

			if (_rowFound)
			{
				while (_rightTable.Next())
				{
					_rightTable.Select(_rightRow);
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
			if (_leftTable.EOF() || !_rowFound)
				return PriorLeft();
			
			if (_rowFound)
			{
				while (_rightTable.Prior())
				{
					_rightTable.Select(_rightRow);
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
		public RightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}
		
        public new RightOuterJoinNode Node { get { return (RightOuterJoinNode)_node; } }
        
        protected override void InternalSelect(IRow row)
        {
			_rightTable.Select(row);
			if (Node.RowExistsColumnIndex >= 0)
			{
				int columnIndex = row.DataType.Columns.IndexOfName(Node.DataType.Columns[Node.RowExistsColumnIndex].Name);
				if (columnIndex >= 0)
					row[Node.DataType.Columns[Node.RowExistsColumnIndex].Name] = _rowFound;
			}
			
			if (_rowFound)
				_leftTable.Select(row);
			else
			{
				int columnIndex;
				foreach (Schema.Column column in _leftTable.DataType.Columns)
				{
					columnIndex = row.DataType.Columns.IndexOfName(column.Name);
					if ((columnIndex >= 0) && (!Node.IsNatural || !Node.LeftKey.Columns.Contains(column.Name)))
						row.ClearValue(columnIndex);
				}
			}
        }

        protected override bool InternalBOF()
        {
            return _rightTable.BOF();
        }
        
        protected override bool InternalEOF()
        {
			if (_rightTable.BOF())
			{
				InternalNext();
				if (_rightTable.EOF())
					return true;
				else
				{
					if (_rightTable.Supports(CursorCapability.BackwardsNavigable))
						_rightTable.First();
					else
						_rightTable.Reset();
					if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
						_leftTable.First();
					else
						_leftTable.Reset();
					return false;
				}
			}
			return _rightTable.EOF();
        }
    }

	// Left unique nested loop right join algorithm
	// left join key unique
    public class LeftUniqueNestedLoopRightJoinTable : RightJoinTable
    {
		public LeftUniqueNestedLoopRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}
		
        protected override bool InternalNext()
        {
			if (_rightTable.Next())
			{
				_rightTable.Select();
				_rowFound = false;
				if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
					_leftTable.First();
				else
					_leftTable.Reset();
					
				while (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
					{
						_rowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (_rightTable.Prior())
			{
				_rightTable.Select();
				_rowFound = false;
				_leftTable.Last();
				
				while (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
					{
						_rowFound = true;
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
		public NonUniqueNestedLoopRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program){}

        protected override bool InternalNext()
        {
			if (_rightTable.BOF())
			{
				_rightTable.Next();
				_rowFound = false;
				_rowIncluded = false;
			}
				
			while (!_rightTable.EOF())
			{
				if (!_leftTable.EOF())
				{
					_rightTable.Select(_rightRow);
					_leftTable.Next();
					while (!_leftTable.EOF())
					{
						_leftTable.Select(_leftRow);
						if (IsMatch())
						{
							_rowFound = true;
							_rowIncluded = true;
							return true;
						}
						_leftTable.Next();
					}
				}
						
				if ((!_rowFound) && (!_rowIncluded))
				{
					_rowIncluded = true;
					return true;
				}
				
				_rightTable.Next();
				if (_leftTable.Supports(CursorCapability.BackwardsNavigable))
					_leftTable.First();
				else
					_leftTable.Reset();
				_rowFound = false;
				_rowIncluded = false;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (_rightTable.EOF())
			{
				_rightTable.Prior();
				_rowFound = false;
				_rowIncluded = false;
			}
				
			while (!_rightTable.BOF())
			{
				if (!_leftTable.BOF())
				{
					_rightTable.Select(_rightRow);
					_leftTable.Prior();
					while (!_leftTable.BOF())
					{
						_leftTable.Select(_leftRow);
						if (IsMatch())
						{
							_rowFound = true;
							_rowIncluded = true;
							return true;
						}
						_leftTable.Prior();
					}
				}
					
				if ((!_rowFound) && (!_rowIncluded))
				{
					_rowIncluded = true;
					return true;
				}
				
				_rightTable.Prior();
				_leftTable.Last();
				_rowFound = false;
				_rowIncluded = false;
			}
			return false;
        }
    }
    
    // unique merge right join algorithm
    // works when both inputs are ordered by their join keys, ascending ignored
    // left or right join keys unique
    public class MergeRightJoinTable : RightJoinTable
    {
		public MergeRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}

		protected void NextRight()
		{
			if (_rightTable.Next())
				_rightTable.Select(_rightRow);
			_rowIncluded = false;
			_rowFound = false;
		}
		
		protected void PriorRight()
		{
			if (_rightTable.Prior())
				_rightTable.Select(_rightRow);
			_rowIncluded = false;
			_rowFound = false;
		}
		
		protected void NextLeft()
		{
			if (_leftTable.Next())
				_leftTable.Select(_leftRow);
		}
		
		protected void PriorLeft()
		{
			if (_leftTable.Prior())
				_leftTable.Select(_leftRow);
		}
		
		protected override bool InternalNext()
		{
			if (_rightTable.BOF())
				NextRight();
			if (_leftTable.BOF())
				NextLeft();
				
			if (_rowFound)
				if (Node.RightKey.IsUnique)
					NextLeft();
				else
					NextRight();
				
			int compareValue;
			while (!_rightTable.EOF())
			{
				if (!_leftTable.EOF())
				{
					compareValue = CompareKeys(_rightRow, _leftRow);
					if (compareValue == 0)
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					else if (compareValue > 0)
					{
						NextLeft();
					}
					else
					{
						if (_rowIncluded)
							NextRight();
						else
						{
							_rowFound = false;
							_rowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (_rowIncluded)
						NextRight();
					else
					{
						_rowFound = false;
						_rowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_rightTable.EOF())
				PriorRight();
			if (_leftTable.EOF())
				PriorLeft();
				
			if (_rowFound)
				if (Node.RightKey.IsUnique)
					PriorLeft();				
				else
					PriorRight();

			int compareValue;
			while (!_rightTable.BOF())
			{
				if (!_leftTable.BOF())
				{
					compareValue = CompareKeys(_rightRow, _leftRow);
					if (compareValue == 0)
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					else if (compareValue < 0)
					{
						PriorLeft();
					}
					else
					{
						if (_rowIncluded)
							PriorRight();
						else
						{
							_rowFound = false;
							_rowIncluded = true;
							return true;
						}
					}
				}
				else
				{
					if (_rowIncluded)
						PriorRight();
					else
					{
						_rowFound = false;
						_rowIncluded = true;
						return true;
					}
				}
			}
			return false;
		}
	}

    public class UniqueMergeRightJoinTable : RightJoinTable
    {
		public UniqueMergeRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}

		protected override bool InternalNext()
		{
			if (_rightTable.Next())
			{
				_rightTable.Select(_rightRow);
				_rowFound = false;

				while (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
					{
						_rowFound = true;
						return true;
					}
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_rightTable.Prior())
			{
				_rightTable.Select(_rightRow);
				_rowFound = false;
				
				while (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
					{
						_rowFound = true;
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
		public LeftUniqueMergeRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			if (_rightTable.BOF())
				if (_rightTable.Next())
				{
					_rightTable.Select(_rightRow);
					_rowFound = false;
					_rowIncluded = false;
				}
				else
					return false;
					
			if (!_rowFound && _rowIncluded)
			{
				if (_rightTable.Next())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					_rowFound = false;
					_rowIncluded = false;
				}
			}

			if (_leftTable.Next())
			{
				_leftTable.Select(_leftRow);
				if (IsMatch())
				{
					_rowFound = true;
					_rowIncluded = true;
					return true;
				}
			}
			
			if (!_rowIncluded)
			{
				_rowIncluded = true;
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_rightTable.EOF())
				if (_rightTable.Prior())
				{
					_rightTable.Select(_rightRow);
					_rowFound = false;
					_rowIncluded = false;
				}
				else
					return false;
					
			if (!_rowFound && _rowIncluded)
			{
				if (_rightTable.Prior())
				{
					_rightTable.Select(_rightRow);
					if (IsMatch())
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					_rowFound = false;
					_rowIncluded = false;
				}
			}

			if (_leftTable.Prior())
			{
				_leftTable.Select(_leftRow);
				if (IsMatch())
				{
					_rowFound = true;
					_rowIncluded = true;
					return true;
				}
			}
			
			if (!_rowIncluded)
			{
				_rowIncluded = true;
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
		public RightUniqueMergeRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}

		protected override bool InternalNext()
		{
			if (_leftTable.BOF())
				if (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
					_rowFound = false;
					_rowIncluded = false;
				}
				else
					return false;
					
			if (!_rowFound && _rowIncluded)
			{
				if (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					_rowFound = false;
					_rowIncluded = false;
				}
			}

			if (_rightTable.Next())
			{
				_rightTable.Select(_rightRow);
				if (IsMatch())
				{
					_rowFound = true;
					_rowIncluded = true;
					return true;
				}
			}
			
			if (!_rowIncluded)
			{
				_rowIncluded = true;
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_leftTable.EOF())
				if (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
					_rowFound = false;
					_rowIncluded = false;
				}
				else
					return false;
					
			if (!_rowFound && _rowIncluded)
			{
				if (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
					if (IsMatch())
					{
						_rowFound = true;
						_rowIncluded = true;
						return true;
					}
					_rowFound = false;
					_rowIncluded = false;
				}
			}

			if (_rightTable.Prior())
			{
				_rightTable.Select(_rightRow);
				if (IsMatch())
				{
					_rowFound = true;
					_rowIncluded = true;
					return true;
				}
			}
			
			if (!_rowIncluded)
			{
				_rowIncluded = true;
				return true;
			}
			
			return false;
		}
	}
    
	public abstract class SearchedRightJoinTable : RightJoinTable
	{
		public SearchedRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}
	}
	
    // left unique searched right join algorithm
    // works when only the left input is ordered by the join key, ascending ignored
    // left join key is unique
	public class LeftUniqueSearchedRightJoinTable : SearchedRightJoinTable
	{
		public LeftUniqueSearchedRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}
		
		protected override bool InternalNext()
		{
			if (_rightTable.Next())
			{
				_rightTable.Select(_rightRow);
				GetLeftKey();
				_rowFound = _leftTable.FindKey(_leftRow);
				return true;
			}
			return false;
		}
		
		protected override bool InternalPrior()
		{
			if (_rightTable.Prior())
			{
				_rightTable.Select(_rightRow);
				GetLeftKey();
				_rowFound = _leftTable.FindKey(_leftRow);
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
		public NonUniqueSearchedRightJoinTable(RightOuterJoinNode node, Program program) : base(node, program) {}

		protected bool OnLeftRow()
		{
			if (_leftTable.BOF() || _leftTable.EOF())
				return false;
				
			IRow currentKey = _leftTable.GetKey();
			try
			{
				return CompareKeys(currentKey, _leftRow) == 0;
			}
			finally
			{
				currentKey.Dispose();
			}
		}

		protected bool FindLeftKey(bool forward)
		{
			_leftTable.FindNearest(_leftRow);
			if (OnLeftRow())
			{
				if (forward)
				{
					while (!_leftTable.BOF())
					{
						if (OnLeftRow())
							_leftTable.Prior();
						else
						{
							_leftTable.Next();
							break;
						}
					}
					if (_leftTable.BOF())
						_leftTable.Next();
				}
				else
				{
					while (!_leftTable.EOF())
					{
						if (OnLeftRow())
							_leftTable.Next();
						else
						{
							_leftTable.Prior();
							break;
						}
					}
					if (_leftTable.EOF())
						_leftTable.Prior();
				}
				return true;
			}
			return false;
		}
		
		protected bool NextRight()
		{
			if (_rightTable.Next())
			{
				_rowFound = false;
				_rowIncluded = false;
				_rightTable.Select(_rightRow);
				GetLeftKey();
				if (FindLeftKey(true))
				{
					_rowFound = true;
					_rowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected bool PriorRight()
		{
			if (_rightTable.Prior())
			{
				_rowFound = false;
				_rowIncluded = false;
				_rightTable.Select(_rightRow);
				GetLeftKey();
				if (FindLeftKey(false))
				{
					_rowFound = true;
					_rowIncluded = true;
				}
				return true;
			}
			return false;
		}
		
		protected override bool InternalNext()
		{
			if (_rightTable.BOF() || !_rowFound)
				return NextRight();

			if (_rowFound)
			{
				while (_leftTable.Next())
				{
					_leftTable.Select(_leftRow);
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
			if (_rightTable.EOF() || !_rowFound)
				return PriorRight();
			
			if (_rowFound)
			{
				while (_leftTable.Prior())
				{
					_leftTable.Select(_leftRow);
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