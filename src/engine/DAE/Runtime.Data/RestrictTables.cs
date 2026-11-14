/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;

    public abstract class RestrictTable : Table
    {
        public RestrictTable(RestrictNode node, Program program) : base(node, program){}
        
        public new RestrictNode Node { get { return (RestrictNode)_node; } }
		
		protected ITable _sourceTable;
		protected IRow _sourceRow;
        
        protected override void InternalOpen()
        {
			_sourceTable = (ITable)Node.Nodes[0].Execute(Program);
			try
			{
				_sourceRow = new Row(Manager, _sourceTable.DataType.RowType);
			}
			catch
			{
				_sourceTable.Dispose();
				throw;
			}
        }
        
        protected override void InternalClose()
        {
            if (_sourceRow != null)
            {
				_sourceRow.Dispose();
				_sourceRow = null;
            }

			if (_sourceTable != null)
			{
				_sourceTable.Dispose();
				_sourceTable = null;
			}
        }
        
        protected override void InternalSelect(IRow row)
        {
            _sourceTable.Select(row);
        }
    }
    
    public class FilterTable : RestrictTable
    {
		public FilterTable(RestrictNode node, Program program) : base(node, program) {}
		
		protected bool _bOF;
		
		protected override void InternalOpen()
		{
			base.InternalOpen();
			_bOF = true;
		}

        protected override void InternalReset()
        {
            _sourceTable.Reset();
            _bOF = true;
        }
        
        protected override bool InternalNext()
        {
            while (_sourceTable.Next())
            {
                _sourceTable.Select(_sourceRow);
                Program.Stack.Push(_sourceRow);
                try
                {
					object tempValue = Node.Nodes[1].Execute(Program);
					if ((tempValue != null) && (bool)tempValue)
					{
						_bOF = false;
					    return true;
					}
				}
				finally
				{
					Program.Stack.Pop();
				}
            }
            return false;
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
				if (_sourceTable.EOF())
					return true;
				else
				{
					if (_sourceTable.Supports(CursorCapability.BackwardsNavigable))
						_sourceTable.First();
					else
						_sourceTable.Reset();
					_bOF = true;
					return false;
				}
			}
			return _sourceTable.EOF();
        }
    }
    
    public class ScanKey : System.Object
    {
		public ScanKey(object argument, bool isExclusive) : base()
		{
			Argument = argument;
			IsExclusive = isExclusive;
		}
		
		public object Argument;
		public bool IsExclusive;
    }
    
    public class ScanTable : RestrictTable
    {
		public ScanTable(RestrictNode node, Program program) : base(node, program) {}
		
		protected bool _bOF;
		protected bool _eOF;

		protected IRow _firstKey;
		protected IRow _lastKey;

		protected bool _isFirstKeyExclusive;
		protected bool _isLastKeyExclusive;
		protected bool _isContradiction;
		public ScanKey[] _firstKeys;
		public ScanKey[] _lastKeys;
		
		public ScanDirection Direction; // ??
		
		protected void ResolveKeys()
		{
			if (Node.OpenConditionsUseFirstKey)
			{
				_firstKeys = new ScanKey[Node.ClosedConditions.Count + Node.OpenConditions.Count];
				_lastKeys = new ScanKey[Node.ClosedConditions.Count];
			}
			else
			{
				_firstKeys = new ScanKey[Node.ClosedConditions.Count];
				_lastKeys = new ScanKey[Node.ClosedConditions.Count + Node.OpenConditions.Count];
			}
		
			Program.Stack.Push(null); // var Dummy : Scalar?
			try
			{
				for (int index = 0; index < Node.Order.Columns.Count; index++)
				{
					ColumnConditions condition;
					if (index < Node.ClosedConditions.Count)
					{
						condition = Node.ClosedConditions[Node.Order.Columns[index].Column];
						
						// A condition in which there is only one condition that is equal, or either both conditions are equal, or both conditions are not equal, and comparison operators are opposite
						if (condition.Count == 1)
						{
							_firstKeys[index] = new ScanKey(condition[0].Argument.Execute(Program), false);
							_lastKeys[index] = new ScanKey(_firstKeys[index].Argument, false);
						}
						else
						{
							// If both conditions are equal, and the arguments are not equal, this is a contradiction
							if (condition[0].Instruction == Instructions.Equal)
							{
								_firstKeys[index] = new ScanKey(condition[0].Argument.Execute(Program), false);
								_lastKeys[index] = new ScanKey(condition[1].Argument.Execute(Program), false);
								if (!(bool)Compiler.EmitEqualNode(Program.Plan, new ValueNode(condition[0].Argument.DataType, _firstKeys[index].Argument), new ValueNode(condition[1].Argument.DataType, _lastKeys[index].Argument)).Execute(Program))
								{
									_isContradiction = true;
									return;
								}
							}
							else
							{
								// Instructions are opposite (one is a less, the other is a greater)
								// If both conditions are not equal, and the arguments are not consistent, this is a contradiction
								ColumnCondition lessCondition;
								ColumnCondition greaterCondition;
								bool isFirstLess;
								if (Instructions.IsLessInstruction(condition[0].Instruction))
								{
									isFirstLess = true;
									lessCondition = condition[0];
									greaterCondition = condition[1];
								}
								else
								{
									isFirstLess = false;
									lessCondition = condition[1];
									greaterCondition = condition[0];
								}
								
								_firstKeys[index] = new ScanKey(greaterCondition.Argument.Execute(Program), Instructions.IsExclusiveInstruction(Node.ClosedConditions[index][isFirstLess ? 1 : 0].Instruction));
								_lastKeys[index] = new ScanKey(lessCondition.Argument.Execute(Program), Instructions.IsExclusiveInstruction(Node.ClosedConditions[index][isFirstLess ? 0 : 1].Instruction));
								
								int compareValue = (int)Compiler.EmitBinaryNode(Program.Plan, new ValueNode(lessCondition.Argument.DataType, _lastKeys[index].Argument), Instructions.Compare, new ValueNode(greaterCondition.Argument.DataType, _firstKeys[index].Argument)).Execute(Program);
								if ((lessCondition.Instruction == Instructions.Less) && (greaterCondition.Instruction == Instructions.Greater))
								{
									if (compareValue <= 0)
									{
										_isContradiction = true;
										return;
									}
								}
								else
								{
									if (compareValue < 0)
									{
										_isContradiction = true;
										return;
									}
								}
							}
						}

						if ((_firstKeys[index].Argument == null) || (_lastKeys[index].Argument == null))
						{
							_isContradiction = true;
							return;
						}
					}
					else if (index < (Node.ClosedConditions.Count + Node.OpenConditions.Count))
					{
						condition = Node.OpenConditions[Node.Order.Columns[index].Column];

						// A condition in which there is at least one non-equal comparison
						if (condition.Count == 1)
							if (Instructions.IsLessInstruction(condition[0].Instruction))
							{
								_lastKeys[index] = new ScanKey(condition[0].Argument.Execute(Program), Instructions.IsExclusiveInstruction(condition[0].Instruction));
								if ((_lastKeys[index].Argument == null))
								{
									_isContradiction = true;
									return;
								}
							}
							else
							{
								_firstKeys[index] = new ScanKey(condition[0].Argument.Execute(Program), Instructions.IsExclusiveInstruction(condition[0].Instruction));
								if ((_firstKeys[index].Argument == null))
								{
									_isContradiction = true;
									return;
								}
							}
						else
						{
							if ((condition[0].Instruction == Instructions.Equal) || (condition[1].Instruction == Instructions.Equal))
							{
								// one is equal, one is a relative operator
								ColumnCondition equalCondition;
								ColumnCondition compareCondition;
								if (condition[0].Instruction == Instructions.Equal)
								{
									equalCondition = condition[0];
									compareCondition = condition[1];
								}
								else
								{
									equalCondition = condition[1];
									compareCondition = condition[0];
								}
								
								object equalVar = equalCondition.Argument.Execute(Program);
								object compareVar = compareCondition.Argument.Execute(Program);
								
								if (Instructions.IsLessInstruction(compareCondition.Instruction))
								{
									_lastKeys[index] = new ScanKey(compareVar, Instructions.IsExclusiveInstruction(compareCondition.Instruction));
									if ((_lastKeys[index].Argument == null))
									{
										_isContradiction = true;
										return;
									}
								}
								else
								{
									_firstKeys[index] = new ScanKey(compareVar, Instructions.IsExclusiveInstruction(compareCondition.Instruction));
									if ((_firstKeys[index].Argument == null))
									{
										_isContradiction = true;
										return;
									}
								}
									
								if (!(bool)Compiler.EmitBinaryNode(Program.Plan, new ValueNode(equalCondition.Argument.DataType, equalVar), compareCondition.Instruction, new ValueNode(compareCondition.Argument.DataType, compareVar)).Execute(Program))
								{
									_isContradiction = true;
									return;
								}
							}
							else 
							{
								object zeroVar = condition[0].Argument.Execute(Program);
								object oneVar = condition[1].Argument.Execute(Program);
								int compareValue = (int)Compiler.EmitBinaryNode(Program.Plan, new ValueNode(condition[0].Argument.DataType, zeroVar), Instructions.Compare, new ValueNode(condition[1].Argument.DataType, oneVar)).Execute(Program);
								if (Instructions.IsLessInstruction(condition[0].Instruction))
								{
									// both are less instructions
									if (compareValue == 0)
										if (condition[0].Instruction == Instructions.Less)
											_lastKeys[index] = new ScanKey(zeroVar, Instructions.IsExclusiveInstruction(condition[0].Instruction));
										else
											_lastKeys[index] = new ScanKey(oneVar, Instructions.IsExclusiveInstruction(condition[1].Instruction));
									else if (compareValue > 0)
										_lastKeys[index] = new ScanKey(zeroVar, Instructions.IsExclusiveInstruction(condition[0].Instruction));
									else
										_lastKeys[index] = new ScanKey(oneVar, Instructions.IsExclusiveInstruction(condition[1].Instruction));

									if ((_lastKeys[index].Argument == null))
									{
										_isContradiction = true;
										return;
									}
								}
								else
								{
									// both are greater instructions
									if (compareValue == 0)
										if (condition[0].Instruction == Instructions.Greater)
											_firstKeys[index] = new ScanKey(zeroVar, Instructions.IsExclusiveInstruction(condition[0].Instruction));
										else
											_firstKeys[index] = new ScanKey(oneVar, Instructions.IsExclusiveInstruction(condition[1].Instruction));
									else if (compareValue < 0)
										_firstKeys[index] = new ScanKey(zeroVar, Instructions.IsExclusiveInstruction(condition[0].Instruction));
									else
										_firstKeys[index] = new ScanKey(oneVar, Instructions.IsExclusiveInstruction(condition[1].Instruction));

									if ((_firstKeys[index].Argument == null))
									{
										_isContradiction = true;
										return;
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				Program.Stack.Pop();
			}
		}
		
		protected void CreateKeys()
		{
			Schema.RowType rowType;
			
			if (_firstKeys.Length > 0)
			{
				rowType = new Schema.RowType();
				for (int index = 0; index < _firstKeys.Length; index++)
					rowType.Columns.Add(Node.Order.Columns[index].Column.Column.Copy());
				_firstKey = new Row(Manager, rowType);
				for (int index = 0; index < _firstKeys.Length; index++)
					if (_firstKeys[index].Argument != null)
						_firstKey[index] = _firstKeys[index].Argument;

				_isFirstKeyExclusive = _firstKeys[_firstKeys.Length - 1].IsExclusive;
			}
			
			if (_lastKeys.Length > 0)
			{
				rowType = new Schema.RowType();
				for (int index = 0; index < _lastKeys.Length; index++)
					rowType.Columns.Add(Node.Order.Columns[index].Column.Column.Copy());
				_lastKey = new Row(Manager, rowType);
				for (int index = 0; index < _lastKeys.Length; index++)
					if (_lastKeys[index].Argument != null)
						_lastKey[index] = _lastKeys[index].Argument;
				
				_isLastKeyExclusive = _lastKeys[_lastKeys.Length - 1].IsExclusive;
			}
		}
		
		protected override void InternalOpen()
		{
			_isContradiction = false;
			ResolveKeys();
			if (!_isContradiction)
			{
				base.InternalOpen();
				CreateKeys();
				InternalFirst();
			}
			else
			{
				_bOF = true;
				_eOF = true;
			}
		}
		
        protected override void InternalClose()
        {
			if (_firstKey != null)
			{
				_firstKey.Dispose();
				_firstKey = null;
			}
			
			if (_lastKey != null)
			{
				_lastKey.Dispose();
				_lastKey = null;
			}
			base.InternalClose();
        }
        
        protected override void InternalReset()
        {
			Close();
			Open();
        }
        
        protected override bool InternalRefresh(IRow row)
        {
			InternalReset();
			if (row != null)
			{
				bool result = InternalFindKey(row, true);
				if (!result)
					InternalFindNearest(row);
				return result;
			}
			return false;
        }
        
        protected override bool InternalBOF()
        {
			return _bOF;
        }
        
        protected override bool InternalEOF()
        {
			return _eOF;
        }
        
        protected override void InternalFirst()
        {
			if (!_isContradiction)
			{
				_bOF = true;
				if (Direction == ScanDirection.Forward)
				{
					if (_firstKey != null)
					{
						_sourceTable.FindNearest(_firstKey);
						
						if (_isFirstKeyExclusive)
						{
							// navigate to the first key that is greater than the search key
							while (!_sourceTable.EOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _firstKey) > 0)
									{
										_sourceTable.Prior();
										break;
									}
									else
										_sourceTable.Next();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
						else
						{
							if (_sourceTable.EOF())
								_sourceTable.Prior();

							while (!_sourceTable.BOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _firstKey) < 0)
										break;
									else
										_sourceTable.Prior();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
					}
					else
						_sourceTable.First();
						
					_eOF = _sourceTable.EOF();
				}
				else
				{
					if (_lastKey != null)
					{
						if (!_sourceTable.FindKey(_firstKey))
						{
							_sourceTable.FindNearest(_firstKey);
							_sourceTable.Prior();
						}
						
						if (_isFirstKeyExclusive)
						{
							while (!_sourceTable.BOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _firstKey) > 0)
									{
										_sourceTable.Next();
										break;
									}
									else
										_sourceTable.Prior();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
						else
						{
							if (_sourceTable.BOF())
								_sourceTable.Next();

							while (!_sourceTable.EOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _firstKey) < 0)
										break;
									else
										_sourceTable.Next();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
					}
					else
						_sourceTable.Last();
						
					_eOF = _sourceTable.BOF();
				}

				InternalNext();
				if (Direction == ScanDirection.Forward)
					_sourceTable.Prior();
				else
					_sourceTable.Next();
				_bOF = true;
			}
        }
        
        protected override void InternalLast()
        {
			if (!_isContradiction)
			{
				_eOF = true;
				if (Direction == ScanDirection.Forward)
				{
					if (_lastKey != null)
					{
						if (!_sourceTable.FindKey(_lastKey))
						{
							_sourceTable.FindNearest(_lastKey);
							_sourceTable.Prior();
						}
						
						if (_isLastKeyExclusive)
						{
							while (!_sourceTable.BOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _lastKey) < 0)
									{
										_sourceTable.Next();
										break;
									}
									else
										_sourceTable.Prior();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
						else
						{
							if (_sourceTable.BOF())
								_sourceTable.Next();

							while (!_sourceTable.EOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _lastKey) > 0)
										break;
									else
										_sourceTable.Next();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
					}
					else
						_sourceTable.Last();
					_bOF = _sourceTable.BOF();
				}
				else
				{
					if (_lastKey != null)
					{
						_sourceTable.FindNearest(_lastKey);
						
						if (_isLastKeyExclusive)
						{
							while (!_sourceTable.EOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _lastKey) < 0)
									{
										_sourceTable.Prior();
										break;
									}
									else
										_sourceTable.Next();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
						else
						{
							if (_sourceTable.EOF())
								_sourceTable.Prior();
								
							while (!_sourceTable.BOF())
							{
								IRow currentKey = _sourceTable.GetKey();
								try
								{
									if (CompareKeys(currentKey, _lastKey) > 0)
										break;
									else
										_sourceTable.Prior();
								}
								finally
								{
									currentKey.Dispose();
								}
							}
						}
					}
					else
						_sourceTable.First();
					_bOF = _sourceTable.EOF();
				}
				InternalPrior();
				if (Direction == ScanDirection.Forward)
					_sourceTable.Next();
				else
					_sourceTable.Prior();
				_eOF = true;
			}
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
			for (int index = 0; index < Node.Order.Columns.Count; index++)
			{
				if ((index >= key1.DataType.Columns.Count) || (index >= key2.DataType.Columns.Count))
					return result;
				else if (key1.HasValue(index))
					if (key2.HasValue(index))
						result = CompareKeyValues(key1[index], key2[index], Node.Order.Columns[index].Sort.CompareNode) * (Node.Order.Columns[index].Ascending ? 1 : -1);
					else
						result = Node.Order.Columns[index].Ascending ? 1 : -1;
				else
					if (key2.HasValue(index))
						result = Node.Order.Columns[index].Ascending ? -1 : 1;
					else
						result = 0;
				
				if (result != 0)
					return result;
			}
			return result;
        }
        
        protected bool IsGreater(IRow key1, IRow key2, bool isExclusive)
        {
			return CompareKeys(key1, key2) >= (isExclusive ? 0 : 1);
        }
        
		protected bool IsLess(IRow key1, IRow key2, bool isExclusive)
		{
			return CompareKeys(key1, key2) <= (isExclusive ? 0 : -1);
		}

        protected void CheckEOF()
        {
			if (!_eOF && (_lastKey != null) && !_sourceTable.BOF())
			{
				IRow key = _sourceTable.GetKey();
				try
				{
					_eOF = IsGreater(key, _lastKey, _isLastKeyExclusive);
				}
				finally
				{
					key.Dispose();
				}
			}
        }
        
        protected void CheckBOF()
        {
			if (!_bOF && (_firstKey != null) && !_sourceTable.EOF())
			{
				IRow key = _sourceTable.GetKey();
				try
				{
					_bOF = IsLess(key, _firstKey, _isFirstKeyExclusive);
				}
				finally
				{
					key.Dispose();
				}
			}
        }

        protected override bool InternalNext()
        {
			if (!_isContradiction)
			{
				if (Direction == ScanDirection.Forward)
				{
					if (_sourceTable.Next())
					{
						_bOF = false;
						_eOF = false;
					}
					else
					{
						_bOF = _sourceTable.BOF();
						_eOF = true;
					}
				}
				else
				{
					if (_sourceTable.Prior())
					{
						_bOF = false;
						_eOF = false;
					}
					else
					{
						_bOF = _sourceTable.EOF();
						_eOF = true;
					}
				}
				CheckEOF();
				return !_eOF;
			}
			return false;
        }
        
        protected override bool InternalPrior()
        {
			if (!_isContradiction)
			{
				if (Direction == ScanDirection.Forward)
				{
					if (_sourceTable.Prior())
					{
						_eOF = false;
						_bOF = false;
					}
					else
					{
						_eOF = _sourceTable.EOF();
						_bOF = true;
					}
				}
				else
				{
					if (_sourceTable.Next())
					{
						_eOF = false;
						_bOF = false;
					}
					else
					{
						_eOF = _sourceTable.BOF();
						_bOF = true;
					}
				}
				CheckBOF();
				return !_bOF;
			}
			return false;
        }
        
        protected override IRow InternalGetKey()
        {
			return _sourceTable.GetKey();
        }

		protected override bool InternalFindKey(IRow key, bool forward)
        {
			if (!_isContradiction)
			{
				IRow localKey = EnsureKeyRow(key);
				try
				{
					if ((_firstKey != null) && IsLess(localKey, _firstKey, _isFirstKeyExclusive))
						return false;
					else if ((_lastKey != null) && IsGreater(localKey, _lastKey, _isLastKeyExclusive))
						return false;
					else
					{
						if (_sourceTable.FindKey(localKey, forward))
						{
							_bOF = false;
							_eOF = false;
							return true;
						}
						else
							return false;
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(localKey, key))
						localKey.Dispose();
				}
			}
			else
				return false;
        }
        
        protected override void InternalFindNearest(IRow key)
        {
			if (!_isContradiction)
			{
				IRow localKey = EnsurePartialKeyRow(key);
				try
				{
					if (localKey != null)
					{
						if (Direction == ScanDirection.Forward)
						{
							if ((_firstKey != null) && IsLess(localKey, _firstKey, _isFirstKeyExclusive))
							{
								InternalFirst();
								InternalNext();
							}
							else if ((_lastKey != null) && IsGreater(localKey, _lastKey, _isLastKeyExclusive))
							{
								InternalLast();
								InternalPrior();
							}
							else
							{
								_sourceTable.FindNearest(localKey);
								_bOF = _sourceTable.BOF();
								_eOF = _sourceTable.EOF();
							}
						}
						else
						{
							if ((_firstKey != null) && IsLess(localKey, _firstKey, _isFirstKeyExclusive))
							{
								InternalLast();
								InternalPrior();
							}
							else if ((_lastKey != null) && IsGreater(localKey, _lastKey, _isLastKeyExclusive))
							{
								InternalFirst();
								InternalNext();
							}
							else
							{
								if (IsKeyRow(localKey) && _sourceTable.FindKey(localKey))
								{
									_bOF = false;
									_eOF = false;
								}
								else
								{
									_sourceTable.FindNearest(localKey);
									_sourceTable.Prior();
									_bOF = _sourceTable.BOF();
									_eOF = _sourceTable.EOF();
								}
							}				
						}
					}
				}
				finally
				{
					if (!Object.ReferenceEquals(key, localKey))
						localKey.Dispose();
				}
			}
        }
    }
    
    public class SeekTable : RestrictTable
    {
		public SeekTable(RestrictNode node, Program program) : base(node, program) {}
		
		protected bool _bOF;
		protected bool _eOF;
		protected bool _rowFound;
		protected Row _keyRow;
		
		protected override void InternalOpen()
		{
			base.InternalOpen();
			int FKeyCount = 0;
			_keyRow = new Row(Manager, new Schema.RowType(_sourceTable.Order.Columns));
			Program.Stack.Push(_keyRow);
			try
			{
				object var;
				for (int index = 0; index < Node.FirstKeyNodes.Length; index++)
				{
					var = Node.FirstKeyNodes[index].Argument.Execute(Program);
					if ((var != null))
					{
						_keyRow[index] = var;
						FKeyCount++;
					}
				}
			}
			finally
			{
				Program.Stack.Pop();
			}
			_rowFound = (FKeyCount > 0) && _sourceTable.FindKey(_keyRow);
			InternalFirst();
		}
		
		protected override void InternalClose()
		{
			if (_keyRow != null)
			{
				_keyRow.Dispose();
				_keyRow = null;
			}				   
			base.InternalClose();
		}

		protected override bool InternalBOF()
		{
			return _bOF;
		}		
		
		protected override bool InternalEOF()
		{
			return _eOF;
		}
		
		protected override bool InternalNext()
		{
			if (_bOF)
				_bOF = !_rowFound;
			else
				_eOF = true;
			return !_eOF;
		}
		
		protected override bool InternalPrior()
		{
			if (_eOF)
				_eOF = !_rowFound;
			else
				_bOF = true;
			return !_bOF;
		}
		
		protected override void InternalFirst()
		{
			_bOF = true;
			_eOF = !_rowFound;
		}
		
		protected override void InternalLast()
		{
			_eOF = true;
			_bOF = !_rowFound;
		}
		
		protected override void InternalReset()
		{
			_sourceTable.Reset();
			_rowFound = _sourceTable.FindKey(_keyRow);
			InternalFirst();
		}
    }
}