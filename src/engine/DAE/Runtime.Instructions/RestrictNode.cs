/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define UseElaborable
//#define ENFORCERESTRICTIONPREDICATE
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Compiling.Visitors;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using System.Collections.Generic;

	/*
		Context literal ->
		
			A given expression is context literal with respect to a given stack location if it 
			does not reference the given stack location. In other words, if it's evaluation does
			not depend on the value of the given stack location.
		
		Order preserving ->
		
			A given expression is order preserving if it is functional and the result of evaluating
			the expression is equivalent semantically to the original value.
			
			A unary operator O with argument type Ta and result type Tr is order preserving if
				for every pair of values V1 and V2 of type Ta for which the expression V1 >= V2 
				evaluates to true, the expression O(V1) >= O(V2) also evaluates to true.
				
			A given expression is order preserving if it consists entirely of invocations of
			order preserving operators.
			
			Note that the definition of order preserving could be extended to include arbitrary
			expressions, for example, the expression X + 1 is order preserving.
			
		Sargable ->
			
			A given expression is sargable if it consists entirely of the logical and of one or more expressions of the form
				A op B
			where A is an order preserving expression with a single column reference to a column of the current row,
			op is a positive relative comparison operator (=, >, <, >=, <=)
			and B is a functional, repeatable, and deterministic context literal expression with respect to the current row.
			
			Note that the expression can of course be of the form B op A. The actual requirement is that one operand
			reference a column of the current row, and the other operand be a context literal expression with respect to the current row.
			
	*/

	public class ColumnCondition : System.Object
	{
		public ColumnCondition(PlanNode columnReference, string instruction, PlanNode argument) : base()
		{
			ColumnReference = columnReference;
			Instruction = instruction;
			Argument = argument;
		}

		public PlanNode ColumnReference;		
		public string Instruction;
		public PlanNode Argument;
	}
	
	public class ColumnConditions : List
	{
		public ColumnConditions(Schema.TableVarColumn column) : base()
		{
			Column = column;
		}
		
		public Schema.TableVarColumn Column;
		
		public new ColumnCondition this[int index]
		{
			get { return (ColumnCondition)base[index]; }
			set { base[index] = value; }
		}
	}
	
	public class Conditions : List
	{
		public Conditions() : base() {}
		
		public new ColumnConditions this[int index]
		{
			get { return (ColumnConditions)base[index]; }
			set { base[index] = value; }
		}
		
		public ColumnConditions this[Schema.TableVarColumn column]
		{
			get 
			{ 
				int index = IndexOf(column);
				if (index < 0)
				{
					ColumnConditions conditions = new ColumnConditions(column);
					Add(conditions);
					return conditions;
				}
				else
					return this[index];
			}
		}

		public int IndexOf(Schema.TableVarColumn column)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].Column == column)
					return index;
			return -1;
		}
		
		public bool Contains(Schema.TableVarColumn column)
		{
			return IndexOf(column) >= 0;
		}
	}
	
	public class Condition : System.Object
	{
		public Condition(PlanNode argument, bool isExclusive)
		{
			Argument = argument;
			IsExclusive = isExclusive;
		}

		public PlanNode Argument;
		public bool IsExclusive;
	}

    // operator iRestrict(table{}, bool) : table{}
    // operator iRestrict(table{}, object) : table{}
    public class RestrictNode : UnaryTableNode
    {
		protected bool TranslateCompareOperator(ref string operatorName, int tempValue)
		{
			switch (tempValue)
			{
				case 0: break;
				case 1:
					switch (operatorName)
					{
						case Instructions.Equal : operatorName = Instructions.Greater; break;
						case Instructions.Less : operatorName = Instructions.InclusiveLess; break;
						default : return false;
					}
				break;
				case -1:
					switch (operatorName)
					{
						case Instructions.Equal : operatorName = Instructions.Less; break;
						case Instructions.Greater : operatorName = Instructions.InclusiveGreater; break;
						default : return false;
					}
				break;
				default : return false;
			}
			return true;
		}
		
		protected bool IsColumnReferencing(PlanNode node, ref string columnName)
		{
			StackColumnReferenceNode localNode = node as StackColumnReferenceNode;
			if ((localNode != null) && (localNode.Location == 0))
			{
				columnName = localNode.Identifier;
				return true;
			}
			else if (node.IsOrderPreserving && (node.Nodes.Count == 1))
				return IsColumnReferencing(node.Nodes[0], ref columnName);
			else
				return false;
		}
		
		protected bool IsSargable(Plan plan, PlanNode planNode)
		{
			InstructionNodeBase node = planNode as InstructionNodeBase;
			if ((node != null) && (node.Operator != null))
			{
				string columnName = String.Empty;
				switch (Schema.Object.Unqualify(node.Operator.OperatorName))
				{
					case Instructions.And : return IsSargable(plan, planNode.Nodes[0]) && IsSargable(plan, planNode.Nodes[1]);
					case Instructions.NotEqual : return false;
					case Instructions.Equal : 
					case Instructions.Greater :
					case Instructions.InclusiveGreater :
					case Instructions.Less :
					case Instructions.InclusiveLess :
						if (IsColumnReferencing(node.Nodes[0], ref columnName))
						{
							if (node.Nodes[1].IsContextLiteral(0))
							{
								_conditions[TableVar.Columns[columnName]].Add
								(
									new ColumnCondition
									(
										node.Nodes[0], 
										Schema.Object.Unqualify(node.Operator.OperatorName), 
										node.Nodes[1]
									)
								);
								return true;
							}
							return false;
						}
						else if (IsColumnReferencing(node.Nodes[1], ref columnName))
						{
							if (node.Nodes[0].IsContextLiteral(0))
							{
								string operatorName = Schema.Object.Unqualify(node.Operator.OperatorName);
								switch (operatorName)
								{
									case Instructions.Greater : operatorName = Instructions.Less; break;
									case Instructions.InclusiveGreater : operatorName = Instructions.InclusiveLess; break;
									case Instructions.Less : operatorName = Instructions.Greater; break;
									case Instructions.InclusiveLess : operatorName = Instructions.InclusiveGreater; break;
								}
								_conditions[TableVar.Columns[columnName]].Add(new ColumnCondition(node.Nodes[1], operatorName, node.Nodes[0]));
								return true;
							}
							return false;
						}
						else if 
						(
							(node.Nodes[0] is InstructionNodeBase) 
							&& 
							(
								((InstructionNodeBase)node.Nodes[0]).Operator != null) 
									&& (String.Compare(Schema.Object.Unqualify(((InstructionNodeBase)node.Nodes[0]).Operator.OperatorName), Instructions.Compare) == 0) 
									&& (node.Nodes[1] is ValueNode) 
									&& node.Nodes[1].DataType.Is(plan.DataTypes.SystemInteger)
							)
						{
							if (IsColumnReferencing(node.Nodes[0].Nodes[0], ref columnName))
							{
								string operatorName = Schema.Object.Unqualify(node.Operator.OperatorName);
								if (node.Nodes[0].Nodes[1].IsContextLiteral(0) && TranslateCompareOperator(ref operatorName, (int)((ValueNode)node.Nodes[1]).Value))
								{
									_conditions[TableVar.Columns[columnName]].Add(new ColumnCondition(node.Nodes[0].Nodes[0], operatorName, node.Nodes[0].Nodes[1]));
									return true;
								}
								return false;
							}
							else if (IsColumnReferencing(node.Nodes[0].Nodes[1], ref columnName))
							{
								string operatorName = Schema.Object.Unqualify(node.Operator.OperatorName);
								switch (operatorName)
								{
									case Instructions.Equal : break;
									case Instructions.Less : operatorName = Instructions.Greater; break;
									case Instructions.InclusiveLess : operatorName = Instructions.InclusiveGreater; break;
									case Instructions.Greater : operatorName = Instructions.Less; break;
									case Instructions.InclusiveGreater : operatorName = Instructions.InclusiveLess; break;
								}
								
								if (node.Nodes[0].Nodes[0].IsContextLiteral(0) && TranslateCompareOperator(ref operatorName, (int)((ValueNode)node.Nodes[1]).Value))
								{
									_conditions[TableVar.Columns[columnName]].Add(new ColumnCondition(node.Nodes[0].Nodes[1], operatorName, node.Nodes[0].Nodes[0]));
									return true;
								}
								return false;
							}
							return false;
						}
						else if 
						(
							(node.Nodes[1] is InstructionNodeBase) 
								&& 
								(
									((InstructionNodeBase)node.Nodes[1]).Operator != null) 
										&& (String.Compare(Schema.Object.Unqualify(((InstructionNodeBase)node.Nodes[1]).Operator.OperatorName), Instructions.Compare) == 0) 
										&& (node.Nodes[0] is ValueNode) 
										&& node.Nodes[0].DataType.Is(plan.DataTypes.SystemInteger)
								)
						{
							if (IsColumnReferencing(node.Nodes[1].Nodes[0], ref columnName))
							{
								string operatorName = Schema.Object.Unqualify(node.Operator.OperatorName);
								switch (operatorName)
								{
									case Instructions.Equal : break;
									case Instructions.Less : operatorName = Instructions.Greater; break;
									case Instructions.InclusiveLess : operatorName = Instructions.InclusiveGreater; break;
									case Instructions.Greater : operatorName = Instructions.Less; break;
									case Instructions.InclusiveGreater : operatorName = Instructions.InclusiveLess; break;
								}
								
								if (node.Nodes[1].Nodes[1].IsContextLiteral(0) && TranslateCompareOperator(ref operatorName, (int)((ValueNode)node.Nodes[0]).Value))
								{
									_conditions[TableVar.Columns[columnName]].Add(new ColumnCondition(node.Nodes[1].Nodes[0], operatorName, node.Nodes[1].Nodes[1]));
									return true;
								}
								return false;
							}
							else if (IsColumnReferencing(node.Nodes[1].Nodes[1], ref columnName))
							{
								string operatorName = Schema.Object.Unqualify(node.Operator.OperatorName);
								
								if (node.Nodes[1].Nodes[0].IsContextLiteral(0) && TranslateCompareOperator(ref operatorName, (int)((ValueNode)node.Nodes[0]).Value))
								{
									_conditions[TableVar.Columns[columnName]].Add(new ColumnCondition(node.Nodes[1].Nodes[1], operatorName, node.Nodes[1].Nodes[0]));
									return true;
								}
								return false;
							}
							return false;
						}
						return false;
							
					default : return false;
				}
			}
			return false;
		}
		
		protected bool DetermineIsSeekable()
		{
			for (int index = 0; index < _conditions.Count; index++)
				if ((_conditions[index].Count > 1) || (_conditions[index][0].Instruction != Instructions.Equal) || !_conditions[index][0].Argument.IsContextLiteral(0))
					return false;

			Schema.KeyColumns keyColumns = new Schema.KeyColumns(null);
			for (int index = 0; index < _conditions.Count; index++)
				keyColumns.Add(_conditions[index].Column);

			foreach (Schema.Key key in SourceTableVar.Keys)
				if (!key.IsSparse && (key.Columns.Count == keyColumns.Count) && key.Columns.IsSupersetOf(keyColumns))
					return true;
			
			return false;
		}
		
		protected Schema.Order FindSeekOrder(Plan plan)
		{
			Schema.KeyColumns keyColumns = new Schema.KeyColumns(null);
			for (int index = 0; index < _conditions.Count; index++)
				keyColumns.Add(_conditions[index].Column);

			foreach (Schema.Key key in SourceTableVar.Keys)
				if (!key.IsSparse && (key.Columns.Count == keyColumns.Count) && key.Columns.IsSupersetOf(keyColumns))
					return Compiler.OrderFromKey(plan, key);

			return null;
		}
		
		/*
			if instruction is >, and successor is defined, promote to >=
			if instruction is <, and predecessor is defined, promote to <= 
		
			=		first key and last key are set to the same argument
			>		first key is set to the successor of the argument
			>=		first key is set to the argument
			<		last key is set to the predecessor of the argument
			<=		last key is set to the argument
			
			=, =	scanable if equal, degrades to single = condition
			=, >	scanable if = argument is greater than > argument, degrades to single > condition
			=, >=	scanable if = argument is greater or equal to >= argument, degrades to single >= condition
			=, <	scanable if = argument is less than < argument, degrades to single < condition
			=, <=	scanable if = argument is less or equal to <= argument, degrades to single <= condition
			
			>, >	scanable, degrades to single condition with argument and instruction of the lesser condition
			>, >=	scanable, degrades to single condition with argument and instruction of the lesser condition
			>=, >	scanable, degrades to single condition with argument and instruction of the lesser condition
			>=, >=	scanable, degrades to single condition with argument and instruction of the lesser condition
			
			<, <	scanable, degrades to single condition with argument and instruction of the greater condition
			<, <=	scanable, degrades to single condition with argument and instruction of the greater condition, exclusive condition if arguments ==
			<=, <	scanable, degrades to single condition with argument and instruction of the greater condition, exclusive condition if arguments ==
			<=, <=	scanable, degrades to single condition with argument and instruction of the greater condition
			
			>, <	scanable if > argument is less than < argument, first key succ(> argument), last key pred(< argument)
			>, <=	scanable if > argument is less or equal to <= argument, first key succ(> argument), last key <= argument
			>=, <	scanable if >= argument is less or equal to < argument, first key >= argument, last key pred(< argument)
			>=, <=	scanable if >= argument is less or equal to <= argument, first key >= argument, last key <= argument
			
			<, >	scanable if < argument is greater than > argument, last key pred(< argument), first key succ(> argument)
			<, >=	scanable if < argument is greater or equal to >= argument, last key pred(< argument), first key >= argument
			<=, >	scanable if <= argument is greater or equal to > argument, last key <= argument, first key succ(> argument)
			<=, >=	scanable if <= argument is greater or equal to >= argument, last key <= argument, first key >= argument
		*/
		
		protected bool DetermineIsScanable(Plan plan)
		{
			for (int index = 0; index < _conditions.Count; index++)
			{
				if (_conditions[index].Count > 2)
					return false;
					
				switch (_conditions[index].Count)
				{
					case 1 :
						if 
						(
							!_conditions[index][0].Argument.IsFunctional || 
							!_conditions[index][0].Argument.IsDeterministic || 
							!_conditions[index][0].Argument.IsRepeatable ||
							!_conditions[index][0].Argument.IsContextLiteral(0)
						)
							return false;
					break;
					
					case 2 :
						if 
						(
							!_conditions[index][0].Argument.IsFunctional || 
							!_conditions[index][0].Argument.IsDeterministic || 
							!_conditions[index][0].Argument.IsRepeatable ||
							!_conditions[index][0].Argument.IsContextLiteral(0) || 
							!_conditions[index][1].Argument.IsFunctional || 
							!_conditions[index][1].Argument.IsDeterministic || 
							!_conditions[index][1].Argument.IsRepeatable ||
							!_conditions[index][1].Argument.IsContextLiteral(0)
						)
							return false;
					break;
				}
			}

			// there can be only one scan condition, open or closed
			bool hasScanCondition = false;
			for (int index = 0; index < _conditions.Count; index++)
			{
				switch (_conditions[index].Count)
				{
					case 1 :
						if (_conditions[index][0].Instruction != Instructions.Equal)
							if (hasScanCondition)
								return false;
							else
								hasScanCondition = true;
					break;
					
					case 2 :
						if ((_conditions[index][0].Instruction != Instructions.Equal) || (_conditions[index][1].Instruction != Instructions.Equal))
							if (hasScanCondition)
								return false;
							else
								hasScanCondition = true;
					break;
				}

				for (int conditionIndex = 0; conditionIndex < _conditions[index].Count; conditionIndex++)
				{
					switch (_conditions[index][conditionIndex].Instruction)
					{
						case Instructions.Less : 
							PlanNode predNode = Compiler.EmitCallNode(plan, "Pred", new PlanNode[]{_conditions[index][conditionIndex].Argument}, false);
							if (predNode != null)
							{
								_conditions[index][conditionIndex].Argument = predNode;
								_conditions[index][conditionIndex].Instruction = Instructions.InclusiveLess;
							}
						break;
						
						case Instructions.Greater : 
							PlanNode succNode = Compiler.EmitCallNode(plan, "Succ", new PlanNode[]{_conditions[index][conditionIndex].Argument}, false);
							if (succNode != null)
							{
								_conditions[index][conditionIndex].Argument = succNode;
								_conditions[index][conditionIndex].Instruction = Instructions.InclusiveGreater;
							}
						break;
					}
				}				
			}
			
			return true;
		}
		
		protected bool IsValidScanOrder(Plan plan, Schema.Order order, Conditions closedConditions, Conditions openConditions, ColumnConditions scanCondition)
		{
			int columnIndex;
			for (int index = 0; index < closedConditions.Count; index++)
			{
				columnIndex = order.Columns.IndexOf(closedConditions[index].Column.Name, Compiler.GetUniqueSort(plan, closedConditions[index].Column.DataType));
				if ((columnIndex < 0) || (columnIndex >= closedConditions.Count))
					return false;
			}
			
			for (int index = 0; index < openConditions.Count; index++)
			{
				columnIndex = order.Columns.IndexOf(openConditions[index].Column.Name, Compiler.GetUniqueSort(plan, openConditions[index].Column.DataType));
				if ((columnIndex < closedConditions.Count) || (columnIndex >= closedConditions.Count + openConditions.Count))
					return false;
			}
			
			if (scanCondition != null)
			{
				columnIndex = order.Columns.IndexOf(scanCondition.Column.Name, Compiler.GetUniqueSort(plan, scanCondition.Column.DataType));
				if (columnIndex != (closedConditions.Count + openConditions.Count - 1))
					return false;
			}
			
			return true;
		}
		
		// A scan order can be any order which includes all the columns in closed conditions, in any order,
		// followed by all the columns in open conditions, in any order,
		// if there is a scan condition, it must be the last order column, open or closed
		protected Schema.Order FindScanOrder(Plan plan)
		{
			Schema.Order newOrder;				

			foreach (Schema.Key key in SourceTableVar.Keys)
			{
				newOrder = Compiler.OrderFromKey(plan, key);
				if (IsValidScanOrder(plan, newOrder, _closedConditions, _openConditions, _scanCondition))
					return newOrder;
			}
					
			foreach (Schema.Order order in TableVar.Orders)
				if (IsValidScanOrder(plan, order, _closedConditions, _openConditions, _scanCondition))
					return order;
					
			newOrder = new Schema.Order();
			Schema.OrderColumn newOrderColumn;
			for (int index = 0; index < _closedConditions.Count; index++)
				if (!Object.ReferenceEquals(_closedConditions[index], _scanCondition))
				{
					newOrderColumn = new Schema.OrderColumn(_closedConditions[index].Column, true);
					newOrderColumn.Sort = Compiler.GetUniqueSort(plan, newOrderColumn.Column.DataType);
					plan.AttachDependency(newOrderColumn.Sort);
					newOrder.Columns.Add(newOrderColumn);
				}

			for (int index = 0; index < _openConditions.Count; index++)
				if (!Object.ReferenceEquals(_openConditions[index], _scanCondition))
				{
					newOrderColumn = new Schema.OrderColumn(_openConditions[index].Column, true);
					newOrderColumn.Sort = Compiler.GetUniqueSort(plan, newOrderColumn.Column.DataType);
					plan.AttachDependency(newOrderColumn.Sort);
					newOrder.Columns.Add(newOrderColumn);
				}

			if (_scanCondition != null)
			{
				newOrderColumn = new Schema.OrderColumn(_scanCondition.Column, true);
				newOrderColumn.Sort = Compiler.GetUniqueSort(plan, newOrderColumn.Column.DataType);
				plan.AttachDependency(newOrderColumn.Sort);
				newOrder.Columns.Add(newOrderColumn);
			}

			return newOrder;
		}
		
		protected override void DetermineModifiers(Plan plan)
		{
			base.DetermineModifiers(plan);
			
			if (Modifiers != null)
				EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			if (!Nodes[1].IsRepeatable)
				throw new CompilerException(CompilerException.Codes.InvalidRestrictionCondition);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			DetermineRemotable(plan);
			
			DetermineSargability(plan);
			
			CopyKeys(SourceTableVar.Keys);

			int columnIndex;				
			foreach (ColumnConditions condition in _conditions)
				foreach (ColumnCondition columnCondition in condition)
					if (columnCondition.Instruction == Instructions.Equal)
						foreach (Schema.Key key in _tableVar.Keys)
						{
							columnIndex = key.Columns.IndexOfName(condition.Column.Name);
							if (columnIndex >= 0)
							{
								key.Columns.RemoveAt(columnIndex);
								
								// A key reduced to the empty key by restriction is no longer sparse
								if (key.Columns.Count == 0)
									key.IsSparse = false;
							}
						}
						
			RemoveSuperKeys();
						
			CopyOrders(SourceTableVar.Orders);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif

			if ((Order == null) && (SourceNode.Order != null))
				Order = CopyOrder(SourceNode.Order);
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, DataType.RowType));
				try
				{
					#if USEVISIT
					Nodes[1] = visitor.Visit(plan, Nodes[1]);
					#else
					Nodes[1].BindingTraversal(plan, visitor);
					#endif
				}
				finally
				{
					plan.Symbols.Pop();
				}
			}
			finally
			{
				plan.ExitRowContext();
			}
		}
		
		// open condition - a condition for which only one side of the range is specified (e.g. x > 5)
		// closed condition - a condition for which both sides of the range are specified (e.g. x > 5 and x < 10) An = condition is considered closed (because it sets both the high and low range)
		// scan condition - a condition which uses a relative comparison operator, can be open or closed (e.g. x > 5)
		private void DetermineScanConditions(Plan plan)
		{
			_closedConditions = new Conditions();
			_openConditions = new Conditions();
			_scanCondition = null;
			
			for (int index = 0; index < _conditions.Count; index++)
			{
				if 
				(
					((_conditions[index].Count == 1) && (_conditions[index][0].Instruction != Instructions.Equal)) || // There is only one condition, and its instruction is not equal, or
					(
						(_conditions[index].Count == 2) && // There are two conditions and
						(
							((_conditions[index][0].Instruction == Instructions.Equal) ^ (_conditions[index][1].Instruction == Instructions.Equal)) || // one or the other is equal, but not both, or
							(
								(Instructions.IsGreaterInstruction(_conditions[index][0].Instruction) && Instructions.IsGreaterInstruction(_conditions[index][1].Instruction)) || // both are greater instructions, or
								(Instructions.IsLessInstruction(_conditions[index][0].Instruction) && Instructions.IsLessInstruction(_conditions[index][1].Instruction)) // both are less instructions
							)
						)
					)
				)
				{
					_scanCondition = _conditions[index];
					_openConditions.Add(_conditions[index]);
					if (Instructions.IsGreaterInstruction(_conditions[index][0].Instruction) || ((_conditions[index].Count > 1) && Instructions.IsGreaterInstruction(_conditions[index][1].Instruction)))
						_openConditionsUseFirstKey = true;
					else
						_openConditionsUseFirstKey = false;
				}
				else
				{
					_closedConditions.Add(_conditions[index]);
					if (_conditions[index][0].Instruction != Instructions.Equal)
						_scanCondition = _conditions[index];
				}
			}
		}
		
		private bool ConvertSargableArguments(Plan plan)
		{
			// For each comparison,
				// if the column referencing branch contains conversions, attempt to push the conversion to the context literal branch
				// if successful, the comparison is still sargable, and a conversion is placed on the argument node for the condition
				// otherwise, the comparison is not sargable, and a warning should be produced indicating the failure
				// note that the original argument node is still participating in the condition expression
				// this is acceptable because for sargable restrictions, the condition expression is only used
				// for statement emission and device compilation, not for execution of the actual restriction.
			bool canConvert = true;
			for (int columnIndex = 0; columnIndex < _conditions.Count; columnIndex++)
			{
				Schema.TableVarColumn column = _conditions[columnIndex].Column;
				for (int conditionIndex = 0; conditionIndex < _conditions[columnIndex].Count; conditionIndex++)
				{
					ColumnCondition condition = _conditions[columnIndex][conditionIndex];
					if (!condition.ColumnReference.DataType.Equals(column.DataType))
					{
						// Find a conversion path to convert the type of the argument to the type of the column
						ConversionContext conversionContext = Compiler.FindConversionPath(plan, condition.Argument.DataType, column.DataType);
						if (conversionContext.CanConvert)
						{
							condition.Argument = Compiler.ConvertNode(plan, condition.Argument, conversionContext);
						}
						else
						{
							plan.Messages.Add(new CompilerException(CompilerException.Codes.CouldNotConvertSargableArgument, CompilerErrorLevel.Warning, plan.CurrentStatement(), column.Name, conversionContext.SourceType.Name, conversionContext.TargetType.Name));
							canConvert = false;
						}
					}
				}
			}
			
			return canConvert;
		}
		
		private void DetermineSargability(Plan plan)
		{
			_isSeekable = false;
			_isScanable = false;
			_conditions = new Conditions();			
			
			if (IsSargable(plan, Nodes[1]) && ConvertSargableArguments(plan))
			{
				_isSeekable = DetermineIsSeekable();
				if (!_isSeekable)
				{
					_isScanable = DetermineIsScanable(plan);
					if (_isScanable)
						DetermineScanConditions(plan);
				}
			}
		}
		
		private void DetermineRestrictionAlgorithm(Plan plan)
		{
			// determine restriction algorithm
			_restrictionAlgorithm = typeof(FilterTable);

			// If the node is not supported because of chunking, or because the scalar condition on the restrict was not supported,
			// a filter must be used because a seek or scan would not be supported either			
			// Note that the call to HasDeviceOperator is preventing a duplicate resolution in the case that the operator is not mapped.
			if (((_device == null) || (_device.ResolveDeviceOperator(plan, Operator) == null)) && (_isSeekable || _isScanable))
			{
				// if IsSargable returns true, a column conditions list has been built where
				// the conditions for each column are separated and the comparison instructions are known
				if (_isSeekable)
				{
					// The condition contains only equal comparisons against all the columns of some key
					Schema.Order seekOrder = FindSeekOrder(plan);
					Nodes[0] = Compiler.EnsureSearchableNode(plan, SourceNode, seekOrder);
					Order = CopyOrder(SourceNode.Order);
					_restrictionAlgorithm = typeof(SeekTable);
					_firstKeyNodes = new Condition[Order.Columns.Count];
					for (int index = 0; index < _firstKeyNodes.Length; index++)
						_firstKeyNodes[index] = new Condition(_conditions[Order.Columns[index].Column][0].Argument, false);
				}
				else if (_isScanable)
				{
					// The condition contains range comparisons against the columns of some order
					Schema.Order scanOrder = FindScanOrder(plan);
					Nodes[0] = Compiler.EnsureSearchableNode(plan, SourceNode, scanOrder);
					Order = CopyOrder(SourceNode.Order);
					_restrictionAlgorithm = typeof(ScanTable);
				}
			}
			else
			{
				if ((Order == null) && (SourceNode.Order != null))
					Order = CopyOrder(SourceNode.Order);
			}
		}

		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			if (!DeviceSupported)
			{
				DetermineRestrictionAlgorithm(plan);
				if (_restrictionAlgorithm.Equals(typeof(ScanTable)))
					_cursorCapabilities = _cursorCapabilities | CursorCapability.BackwardsNavigable | CursorCapability.Searchable;
				else if (_restrictionAlgorithm.Equals(typeof(SeekTable)))
					_cursorCapabilities = _cursorCapabilities | CursorCapability.BackwardsNavigable;
			}
		}
		
		private Conditions _conditions;
		/// <summary>Sargable conditions for the restriction.</summary>
		public Conditions Conditions { get { return _conditions; } }

		private Conditions _closedConditions;
		/// <summary>Conditions where both sides of the range are specified.</summary>
		public Conditions ClosedConditions { get { return _closedConditions; } }

		private Conditions _openConditions;
		/// <summary>Conditions where only one side of the range is specified.</summary>
		public Conditions OpenConditions { get { return _openConditions; } }

		// TODO: This does not seem to work in general, because it cannot capture the information for the case of multiple open conditions.
		private bool _openConditionsUseFirstKey;
		/// <summary>Indicates whether the open conditions should use the first key.</summary>
		public bool OpenConditionsUseFirstKey { get { return _openConditionsUseFirstKey; } }
		
		private bool _isSeekable;
		/// <summary>Indicates whether the restriction condition is seekable.</summary>
		public bool IsSeekable { get { return _isSeekable; } }
		
		private bool _isScanable;
		/// <summary>Indicates wheter the restriction condition is scanable.</summary>
		public bool IsScanable { get { return _isScanable; } }

		private Type _restrictionAlgorithm;
		public Type RestrictionAlgorithm { get { return _restrictionAlgorithm; } }
		
		private ColumnConditions _scanCondition;
		public ColumnConditions ScanCondition { get { return _scanCondition; } }
		
		private Condition[] _firstKeyNodes;
		public Condition[] FirstKeyNodes { get { return _firstKeyNodes; } }
		
		// BTR 10/22/2004 -> Removed to avoid warning
		//private Condition[] FLastKeyNodes;
		//public Condition[] LastKeyNodes { get { return FLastKeyNodes; } }
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = SourceNode.CursorType;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;
		}
		
		// Execute
		public override object InternalExecute(Program program)
		{
			RestrictTable table = (RestrictTable)Activator.CreateInstance(_restrictionAlgorithm, new object[]{this, program});
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}
		
		// Statement
		public override Statement EmitStatement(EmitMode mode)
		{
			if (_shouldEmit)
			{
				Expression expression = new RestrictExpression((Expression)Nodes[0].EmitStatement(mode), (Expression)Nodes[1].EmitStatement(mode));
				expression.Modifiers = Modifiers;
				return expression;
			}
			else
				return Nodes[0].EmitStatement(mode);
		}

		// Compiling an insert statement includes a 'where false' on the target expression as an optimization. This node is marked as ShouldEmit = false.
		protected bool _shouldEmit = true;
		public bool ShouldEmit
		{
			get { return _shouldEmit; }
			set { _shouldEmit = value; }
		}
		
		#if ENFORCERESTRICTIONPREDICATE
		protected bool FEnforcePredicate = true;
		#else
		protected bool _enforcePredicate = false;
		#endif
		public bool EnforcePredicate
		{
			get { return _enforcePredicate; }
			set { _enforcePredicate = value; }
		}
		
		public override void DetermineRemotable(Plan plan)
		{
			base.DetermineRemotable(plan);
			
			_tableVar.ShouldValidate = _tableVar.ShouldValidate || _enforcePredicate;
		}
		
		// Validate
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (_enforcePredicate && (columnName == String.Empty))
			{
				PushRow(program, newRow);
				try
				{
					object objectValue = Nodes[1].Execute(program);
					// BTR 05/03/2005 -> Because the restriction considers nil to be false, the validation should consider it false as well.
					if ((objectValue == null) || !(bool)objectValue)
						throw new RuntimeException(RuntimeException.Codes.NewRowViolatesRestrictPredicate, ErrorSeverity.User);
				}
				finally
				{
					PopRow(program);
				}
			}

			return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
		}

		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			bool changed = false;
			if (columnName == String.Empty)
			{
				PushRow(program, newRow);
				try
				{
					// The condition contains only equal comparisons against all the columns of some key
					// foreach condition
						// foreach column condition
							// if the instruction is equal
								// if the newRow has no value for the column
									// set it to the evaluation of the argument
					for (int index = 0; index < _conditions.Count; index++)
					{
						int rowIndex = newRow.DataType.Columns.IndexOfName(_conditions[index].Column.Name);
						if (rowIndex >= 0)
						{
							for (int columnIndex = 0; columnIndex < _conditions[index].Count; columnIndex++)
							{
								if (_conditions[index][columnIndex].Instruction == Instructions.Equal)
								{
									if (!newRow.HasValue(rowIndex) && ((valueFlags == null) || !valueFlags[rowIndex]))
									{
										newRow[rowIndex] = _conditions[index][columnIndex].Argument.Execute(program);
										changed = true;
										if (valueFlags != null)
										{
											valueFlags[rowIndex] = true;
										}
									}
								}
							}
						}
					}
				}
				finally
				{
					PopRow(program);
				}
			}

			return base.InternalDefault(program, oldRow, newRow, valueFlags, columnName, isDescending) || changed;
		}

		public override bool IsContextLiteral(int location, IList<string> columnReferences)
		{
			if (!Nodes[0].IsContextLiteral(location, columnReferences))
				return false;
			return Nodes[1].IsContextLiteral(location + 1, columnReferences);
		}
    }
}