/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
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
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

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
		public ColumnCondition(PlanNode AColumnReference, string AInstruction, PlanNode AArgument) : base()
		{
			ColumnReference = AColumnReference;
			Instruction = AInstruction;
			Argument = AArgument;
		}

		public PlanNode ColumnReference;		
		public string Instruction;
		public PlanNode Argument;
	}
	
	public class ColumnConditions : List
	{
		public ColumnConditions(Schema.TableVarColumn AColumn) : base()
		{
			Column = AColumn;
		}
		
		public Schema.TableVarColumn Column;
		
		public new ColumnCondition this[int AIndex]
		{
			get { return (ColumnCondition)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	
	public class Conditions : List
	{
		public Conditions() : base() {}
		
		public new ColumnConditions this[int AIndex]
		{
			get { return (ColumnConditions)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public ColumnConditions this[Schema.TableVarColumn AColumn]
		{
			get 
			{ 
				int LIndex = IndexOf(AColumn);
				if (LIndex < 0)
				{
					ColumnConditions LConditions = new ColumnConditions(AColumn);
					Add(LConditions);
					return LConditions;
				}
				else
					return this[LIndex];
			}
		}

		public int IndexOf(Schema.TableVarColumn AColumn)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].Column == AColumn)
					return LIndex;
			return -1;
		}
		
		public bool Contains(Schema.TableVarColumn AColumn)
		{
			return IndexOf(AColumn) >= 0;
		}
	}
	
	public class Condition : System.Object
	{
		public Condition(PlanNode AArgument, bool AIsExclusive)
		{
			Argument = AArgument;
			IsExclusive = AIsExclusive;
		}

		public PlanNode Argument;
		public bool IsExclusive;
	}

    // operator iRestrict(table{}, bool) : table{}
    // operator iRestrict(table{}, object) : table{}
    public class RestrictNode : UnaryTableNode
    {
		protected bool TranslateCompareOperator(ref string AOperatorName, int AValue)
		{
			switch (AValue)
			{
				case 0: break;
				case 1:
					switch (AOperatorName)
					{
						case Instructions.Equal : AOperatorName = Instructions.Greater; break;
						case Instructions.Less : AOperatorName = Instructions.InclusiveLess; break;
						default : return false;
					}
				break;
				case -1:
					switch (AOperatorName)
					{
						case Instructions.Equal : AOperatorName = Instructions.Less; break;
						case Instructions.Greater : AOperatorName = Instructions.InclusiveGreater; break;
						default : return false;
					}
				break;
				default : return false;
			}
			return true;
		}
		
		protected bool IsColumnReferencing(PlanNode ANode, ref string AColumnName)
		{
			StackColumnReferenceNode LNode = ANode as StackColumnReferenceNode;
			if ((LNode != null) && (LNode.Location == 0))
			{
				AColumnName = LNode.Identifier;
				return true;
			}
			else if (ANode.IsOrderPreserving && (ANode.Nodes.Count == 1))
				return IsColumnReferencing(ANode.Nodes[0], ref AColumnName);
			else
				return false;
		}
		
		protected bool IsSargable(Plan APlan, PlanNode APlanNode)
		{
			InstructionNodeBase LNode = APlanNode as InstructionNodeBase;
			if ((LNode != null) && (LNode.Operator != null))
			{
				string LColumnName = String.Empty;
				switch (Schema.Object.Unqualify(LNode.Operator.OperatorName))
				{
					case Instructions.And : return IsSargable(APlan, APlanNode.Nodes[0]) && IsSargable(APlan, APlanNode.Nodes[1]);
					case Instructions.NotEqual : return false;
					case Instructions.Equal : 
					case Instructions.Greater :
					case Instructions.InclusiveGreater :
					case Instructions.Less :
					case Instructions.InclusiveLess :
						if (IsColumnReferencing(LNode.Nodes[0], ref LColumnName))
						{
							if (LNode.Nodes[1].IsContextLiteral(0))
							{
								FConditions[TableVar.Columns[LColumnName]].Add
								(
									new ColumnCondition
									(
										LNode.Nodes[0], 
										Schema.Object.Unqualify(LNode.Operator.OperatorName), 
										LNode.Nodes[1]
									)
								);
								return true;
							}
							return false;
						}
						else if (IsColumnReferencing(LNode.Nodes[1], ref LColumnName))
						{
							if (LNode.Nodes[0].IsContextLiteral(0))
							{
								string LOperatorName = Schema.Object.Unqualify(LNode.Operator.OperatorName);
								switch (LOperatorName)
								{
									case Instructions.Greater : LOperatorName = Instructions.Less; break;
									case Instructions.InclusiveGreater : LOperatorName = Instructions.InclusiveLess; break;
									case Instructions.Less : LOperatorName = Instructions.Greater; break;
									case Instructions.InclusiveLess : LOperatorName = Instructions.InclusiveGreater; break;
								}
								FConditions[TableVar.Columns[LColumnName]].Add(new ColumnCondition(LNode.Nodes[1], LOperatorName, LNode.Nodes[0]));
								return true;
							}
							return false;
						}
						else if 
						(
							(LNode.Nodes[0] is InstructionNodeBase) 
							&& 
							(
								((InstructionNodeBase)LNode.Nodes[0]).Operator != null) 
									&& (String.Compare(Schema.Object.Unqualify(((InstructionNodeBase)LNode.Nodes[0]).Operator.OperatorName), Instructions.Compare) == 0) 
									&& (LNode.Nodes[1] is ValueNode) 
									&& LNode.Nodes[1].DataType.Is(APlan.Catalog.DataTypes.SystemInteger)
							)
						{
							if (IsColumnReferencing(LNode.Nodes[0].Nodes[0], ref LColumnName))
							{
								string LOperatorName = Schema.Object.Unqualify(LNode.Operator.OperatorName);
								if (LNode.Nodes[0].Nodes[1].IsContextLiteral(0) && TranslateCompareOperator(ref LOperatorName, (int)((ValueNode)LNode.Nodes[1]).Value))
								{
									FConditions[TableVar.Columns[LColumnName]].Add(new ColumnCondition(LNode.Nodes[0].Nodes[0], LOperatorName, LNode.Nodes[0].Nodes[1]));
									return true;
								}
								return false;
							}
							else if (IsColumnReferencing(LNode.Nodes[0].Nodes[1], ref LColumnName))
							{
								string LOperatorName = Schema.Object.Unqualify(LNode.Operator.OperatorName);
								switch (LOperatorName)
								{
									case Instructions.Equal : break;
									case Instructions.Less : LOperatorName = Instructions.Greater; break;
									case Instructions.InclusiveLess : LOperatorName = Instructions.InclusiveGreater; break;
									case Instructions.Greater : LOperatorName = Instructions.Less; break;
									case Instructions.InclusiveGreater : LOperatorName = Instructions.InclusiveLess; break;
								}
								
								if (LNode.Nodes[0].Nodes[0].IsContextLiteral(0) && TranslateCompareOperator(ref LOperatorName, (int)((ValueNode)LNode.Nodes[1]).Value))
								{
									FConditions[TableVar.Columns[LColumnName]].Add(new ColumnCondition(LNode.Nodes[0].Nodes[1], LOperatorName, LNode.Nodes[0].Nodes[0]));
									return true;
								}
								return false;
							}
							return false;
						}
						else if 
						(
							(LNode.Nodes[1] is InstructionNodeBase) 
								&& 
								(
									((InstructionNodeBase)LNode.Nodes[1]).Operator != null) 
										&& (String.Compare(Schema.Object.Unqualify(((InstructionNodeBase)LNode.Nodes[1]).Operator.OperatorName), Instructions.Compare) == 0) 
										&& (LNode.Nodes[0] is ValueNode) 
										&& LNode.Nodes[0].DataType.Is(APlan.Catalog.DataTypes.SystemInteger)
								)
						{
							if (IsColumnReferencing(LNode.Nodes[1].Nodes[0], ref LColumnName))
							{
								string LOperatorName = Schema.Object.Unqualify(LNode.Operator.OperatorName);
								switch (LOperatorName)
								{
									case Instructions.Equal : break;
									case Instructions.Less : LOperatorName = Instructions.Greater; break;
									case Instructions.InclusiveLess : LOperatorName = Instructions.InclusiveGreater; break;
									case Instructions.Greater : LOperatorName = Instructions.Less; break;
									case Instructions.InclusiveGreater : LOperatorName = Instructions.InclusiveLess; break;
								}
								
								if (LNode.Nodes[1].Nodes[1].IsContextLiteral(0) && TranslateCompareOperator(ref LOperatorName, (int)((ValueNode)LNode.Nodes[0]).Value))
								{
									FConditions[TableVar.Columns[LColumnName]].Add(new ColumnCondition(LNode.Nodes[1].Nodes[0], LOperatorName, LNode.Nodes[1].Nodes[1]));
									return true;
								}
								return false;
							}
							else if (IsColumnReferencing(LNode.Nodes[1].Nodes[1], ref LColumnName))
							{
								string LOperatorName = Schema.Object.Unqualify(LNode.Operator.OperatorName);
								
								if (LNode.Nodes[1].Nodes[0].IsContextLiteral(0) && TranslateCompareOperator(ref LOperatorName, (int)((ValueNode)LNode.Nodes[0]).Value))
								{
									FConditions[TableVar.Columns[LColumnName]].Add(new ColumnCondition(LNode.Nodes[1].Nodes[1], LOperatorName, LNode.Nodes[1].Nodes[0]));
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
			for (int LIndex = 0; LIndex < FConditions.Count; LIndex++)
				if ((FConditions[LIndex].Count > 1) || (FConditions[LIndex][0].Instruction != Instructions.Equal) || !FConditions[LIndex][0].Argument.IsContextLiteral(0))
					return false;

			Schema.KeyColumns LKeyColumns = new Schema.KeyColumns(null);
			for (int LIndex = 0; LIndex < FConditions.Count; LIndex++)
				LKeyColumns.Add(FConditions[LIndex].Column);

			foreach (Schema.Key LKey in SourceTableVar.Keys)
				if (!LKey.IsSparse && (LKey.Columns.Count == LKeyColumns.Count) && LKey.Columns.IsSupersetOf(LKeyColumns))
					return true;
			
			return false;
		}
		
		protected Schema.Order FindSeekOrder(Plan APlan)
		{
			Schema.KeyColumns LKeyColumns = new Schema.KeyColumns(null);
			for (int LIndex = 0; LIndex < FConditions.Count; LIndex++)
				LKeyColumns.Add(FConditions[LIndex].Column);

			foreach (Schema.Key LKey in SourceTableVar.Keys)
				if (!LKey.IsSparse && (LKey.Columns.Count == LKeyColumns.Count) && LKey.Columns.IsSupersetOf(LKeyColumns))
					return new Schema.Order(LKey, APlan);

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
		
		protected bool DetermineIsScanable(Plan APlan)
		{
			for (int LIndex = 0; LIndex < FConditions.Count; LIndex++)
			{
				if (FConditions[LIndex].Count > 2)
					return false;
					
				switch (FConditions[LIndex].Count)
				{
					case 1 :
						if 
						(
							!FConditions[LIndex][0].Argument.IsFunctional || 
							!FConditions[LIndex][0].Argument.IsDeterministic || 
							!FConditions[LIndex][0].Argument.IsRepeatable ||
							!FConditions[LIndex][0].Argument.IsContextLiteral(0)
						)
							return false;
					break;
					
					case 2 :
						if 
						(
							!FConditions[LIndex][0].Argument.IsFunctional || 
							!FConditions[LIndex][0].Argument.IsDeterministic || 
							!FConditions[LIndex][0].Argument.IsRepeatable ||
							!FConditions[LIndex][0].Argument.IsContextLiteral(0) || 
							!FConditions[LIndex][1].Argument.IsFunctional || 
							!FConditions[LIndex][1].Argument.IsDeterministic || 
							!FConditions[LIndex][1].Argument.IsRepeatable ||
							!FConditions[LIndex][1].Argument.IsContextLiteral(0)
						)
							return false;
					break;
				}
			}

			// there can be only one scan condition, open or closed
			bool LHasScanCondition = false;
			for (int LIndex = 0; LIndex < FConditions.Count; LIndex++)
			{
				switch (FConditions[LIndex].Count)
				{
					case 1 :
						if (FConditions[LIndex][0].Instruction != Instructions.Equal)
							if (LHasScanCondition)
								return false;
							else
								LHasScanCondition = true;
					break;
					
					case 2 :
						if ((FConditions[LIndex][0].Instruction != Instructions.Equal) || (FConditions[LIndex][1].Instruction != Instructions.Equal))
							if (LHasScanCondition)
								return false;
							else
								LHasScanCondition = true;
					break;
				}

				for (int LConditionIndex = 0; LConditionIndex < FConditions[LIndex].Count; LConditionIndex++)
				{
					switch (FConditions[LIndex][LConditionIndex].Instruction)
					{
						case Instructions.Less : 
							PlanNode LPredNode = Compiler.EmitCallNode(APlan, "Pred", new PlanNode[]{FConditions[LIndex][LConditionIndex].Argument}, false);
							if (LPredNode != null)
							{
								FConditions[LIndex][LConditionIndex].Argument = LPredNode;
								FConditions[LIndex][LConditionIndex].Instruction = Instructions.InclusiveLess;
							}
						break;
						
						case Instructions.Greater : 
							PlanNode LSuccNode = Compiler.EmitCallNode(APlan, "Succ", new PlanNode[]{FConditions[LIndex][LConditionIndex].Argument}, false);
							if (LSuccNode != null)
							{
								FConditions[LIndex][LConditionIndex].Argument = LSuccNode;
								FConditions[LIndex][LConditionIndex].Instruction = Instructions.InclusiveGreater;
							}
						break;
					}
				}				
			}
			
			return true;
		}
		
		protected bool IsValidScanOrder(Plan APlan, Schema.Order AOrder, Conditions AClosedConditions, Conditions AOpenConditions, ColumnConditions AScanCondition)
		{
			int LColumnIndex;
			for (int LIndex = 0; LIndex < AClosedConditions.Count; LIndex++)
			{
				LColumnIndex = AOrder.Columns.IndexOf(AClosedConditions[LIndex].Column.Name, Compiler.GetUniqueSort(APlan, AClosedConditions[LIndex].Column.DataType));
				if ((LColumnIndex < 0) || (LColumnIndex >= AClosedConditions.Count))
					return false;
			}
			
			for (int LIndex = 0; LIndex < AOpenConditions.Count; LIndex++)
			{
				LColumnIndex = AOrder.Columns.IndexOf(AOpenConditions[LIndex].Column.Name, Compiler.GetUniqueSort(APlan, AOpenConditions[LIndex].Column.DataType));
				if ((LColumnIndex < AClosedConditions.Count) || (LColumnIndex >= AClosedConditions.Count + AOpenConditions.Count))
					return false;
			}
			
			if (AScanCondition != null)
			{
				LColumnIndex = AOrder.Columns.IndexOf(AScanCondition.Column.Name, Compiler.GetUniqueSort(APlan, AScanCondition.Column.DataType));
				if (LColumnIndex != (AClosedConditions.Count + AOpenConditions.Count - 1))
					return false;
			}
			
			return true;
		}
		
		// A scan order can be any order which includes all the columns in closed conditions, in any order,
		// followed by all the columns in open conditions, in any order,
		// if there is a scan condition, it must be the last order column, open or closed
		protected Schema.Order FindScanOrder(Plan APlan)
		{
			Schema.Order LNewOrder;				

			foreach (Schema.Key LKey in SourceTableVar.Keys)
			{
				LNewOrder = new Schema.Order(LKey, APlan);
				if (IsValidScanOrder(APlan, LNewOrder, FClosedConditions, FOpenConditions, FScanCondition))
					return LNewOrder;
			}
					
			foreach (Schema.Order LOrder in TableVar.Orders)
				if (IsValidScanOrder(APlan, LOrder, FClosedConditions, FOpenConditions, FScanCondition))
					return LOrder;
					
			LNewOrder = new Schema.Order();
			Schema.OrderColumn LNewOrderColumn;
			for (int LIndex = 0; LIndex < FClosedConditions.Count; LIndex++)
				if (!Object.ReferenceEquals(FClosedConditions[LIndex], FScanCondition))
				{
					LNewOrderColumn = new Schema.OrderColumn(FClosedConditions[LIndex].Column, true);
					LNewOrderColumn.Sort = Compiler.GetUniqueSort(APlan, LNewOrderColumn.Column.DataType);
					if (LNewOrderColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LNewOrderColumn.Sort.Dependencies);
					LNewOrder.Columns.Add(LNewOrderColumn);
				}

			for (int LIndex = 0; LIndex < FOpenConditions.Count; LIndex++)
				if (!Object.ReferenceEquals(FOpenConditions[LIndex], FScanCondition))
				{
					LNewOrderColumn = new Schema.OrderColumn(FOpenConditions[LIndex].Column, true);
					LNewOrderColumn.Sort = Compiler.GetUniqueSort(APlan, LNewOrderColumn.Column.DataType);
					if (LNewOrderColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LNewOrderColumn.Sort.Dependencies);
					LNewOrder.Columns.Add(LNewOrderColumn);
				}

			if (FScanCondition != null)
			{
				LNewOrderColumn = new Schema.OrderColumn(FScanCondition.Column, true);
				LNewOrderColumn.Sort = Compiler.GetUniqueSort(APlan, LNewOrderColumn.Column.DataType);
				if (LNewOrderColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LNewOrderColumn.Sort.Dependencies);
				LNewOrder.Columns.Add(LNewOrderColumn);
			}

			return LNewOrder;
		}
		
		protected override void DetermineModifiers(Plan APlan)
		{
			base.DetermineModifiers(APlan);
			
			if (Modifiers != null)
				EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			if (!Nodes[1].IsRepeatable)
				throw new CompilerException(CompilerException.Codes.InvalidRestrictionCondition);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			DetermineRemotable(APlan);
			
			DetermineSargability(APlan);
			
			CopyKeys(SourceTableVar.Keys);

			int LColumnIndex;				
			foreach (ColumnConditions LCondition in FConditions)
				foreach (ColumnCondition LColumnCondition in LCondition)
					if (LColumnCondition.Instruction == Instructions.Equal)
						foreach (Schema.Key LKey in FTableVar.Keys)
						{
							LColumnIndex = LKey.Columns.IndexOfName(LCondition.Column.Name);
							if (LColumnIndex >= 0)
							{
								LKey.Columns.RemoveAt(LColumnIndex);
								
								// A key reduced to the empty key by restriction is no longer sparse
								if (LKey.Columns.Count == 0)
									LKey.IsSparse = false;
							}
						}
						
			RemoveSuperKeys();
						
			CopyOrders(SourceTableVar.Orders);

			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif

			if ((Order == null) && (SourceNode.Order != null))
				Order = CopyOrder(SourceNode.Order);
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(DataType.RowType));
				try
				{
					Nodes[1].DetermineBinding(APlan);
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		// open condition - a condition for which only one side of the range is specified (e.g. x > 5)
		// closed condition - a condition for which both sides of the range are specified (e.g. x > 5 and x < 10) An = condition is considered closed (because it sets both the high and low range)
		// scan condition - a condition which uses a relative comparison operator, can be open or closed (e.g. x > 5)
		private void DetermineScanConditions(Plan APlan)
		{
			FClosedConditions = new Conditions();
			FOpenConditions = new Conditions();
			FScanCondition = null;
			
			for (int LIndex = 0; LIndex < FConditions.Count; LIndex++)
			{
				if 
				(
					((FConditions[LIndex].Count == 1) && (FConditions[LIndex][0].Instruction != Instructions.Equal)) || // There is only one condition, and its instruction is not equal, or
					(
						(FConditions[LIndex].Count == 2) && // There are two conditions and
						(
							((FConditions[LIndex][0].Instruction == Instructions.Equal) ^ (FConditions[LIndex][1].Instruction == Instructions.Equal)) || // one or the other is equal, but not both, or
							(
								(Instructions.IsGreaterInstruction(FConditions[LIndex][0].Instruction) && Instructions.IsGreaterInstruction(FConditions[LIndex][1].Instruction)) || // both are greater instructions, or
								(Instructions.IsLessInstruction(FConditions[LIndex][0].Instruction) && Instructions.IsLessInstruction(FConditions[LIndex][1].Instruction)) // both are less instructions
							)
						)
					)
				)
				{
					FScanCondition = FConditions[LIndex];
					FOpenConditions.Add(FConditions[LIndex]);
					if (Instructions.IsGreaterInstruction(FConditions[LIndex][0].Instruction) || ((FConditions[LIndex].Count > 1) && Instructions.IsGreaterInstruction(FConditions[LIndex][1].Instruction)))
						FOpenConditionsUseFirstKey = true;
					else
						FOpenConditionsUseFirstKey = false;
				}
				else
				{
					FClosedConditions.Add(FConditions[LIndex]);
					if (FConditions[LIndex][0].Instruction != Instructions.Equal)
						FScanCondition = FConditions[LIndex];
				}
			}
		}
		
		private bool ConvertSargableArguments(Plan APlan)
		{
			// For each comparison,
				// if the column referencing branch contains conversions, attempt to push the conversion to the context literal branch
				// if successful, the comparison is still sargable, and a conversion is placed on the argument node for the condition
				// otherwise, the comparison is not sargable, and a warning should be produced indicating the failure
				// note that the original argument node is still participating in the condition expression
				// this is acceptable because for sargable restrictions, the condition expression is only used
				// for statement emission and device compilation, not for execution of the actual restriction.
			bool LCanConvert = true;
			for (int LColumnIndex = 0; LColumnIndex < FConditions.Count; LColumnIndex++)
			{
				Schema.TableVarColumn LColumn = FConditions[LColumnIndex].Column;
				for (int LConditionIndex = 0; LConditionIndex < FConditions[LColumnIndex].Count; LConditionIndex++)
				{
					ColumnCondition LCondition = FConditions[LColumnIndex][LConditionIndex];
					if (!LCondition.ColumnReference.DataType.Equals(LColumn.DataType))
					{
						// Find a conversion path to convert the type of the argument to the type of the column
						ConversionContext LConversionContext = Compiler.FindConversionPath(APlan, LCondition.Argument.DataType, LColumn.DataType);
						if (LConversionContext.CanConvert)
						{
							LCondition.Argument = Compiler.ConvertNode(APlan, LCondition.Argument, LConversionContext);
						}
						else
						{
							APlan.Messages.Add(new CompilerException(CompilerException.Codes.CouldNotConvertSargableArgument, CompilerErrorLevel.Warning, APlan.CurrentStatement(), LColumn.Name, LConversionContext.SourceType.Name, LConversionContext.TargetType.Name));
							LCanConvert = false;
						}
					}
				}
			}
			
			return LCanConvert;
		}
		
		private void DetermineSargability(Plan APlan)
		{
			FIsSeekable = false;
			FIsScanable = false;
			FConditions = new Conditions();			
			
			if (IsSargable(APlan, Nodes[1]) && ConvertSargableArguments(APlan))
			{
				FIsSeekable = DetermineIsSeekable();
				if (!FIsSeekable)
				{
					FIsScanable = DetermineIsScanable(APlan);
					if (FIsScanable)
						DetermineScanConditions(APlan);
				}
			}
		}
		
		private void DetermineRestrictionAlgorithm(Plan APlan)
		{
			// determine restriction algorithm
			FRestrictionAlgorithm = typeof(FilterTable);

			// If the node is not supported because of chunking, or because the scalar condition on the restrict was not supported,
			// a filter must be used because a seek or scan would not be supported either			
			// Note that the call to HasDeviceOperator is preventing a duplicate resolution in the case that the operator is not mapped.
			if (((FDevice == null) || (FDevice.ResolveDeviceOperator(APlan, Operator) == null)) && (FIsSeekable || FIsScanable))
			{
				// if IsSargable returns true, a column conditions list has been built where
				// the conditions for each column are separated and the comparison instructions are known
				if (FIsSeekable)
				{
					// The condition contains only equal comparisons against all the columns of some key
					Schema.Order LSeekOrder = FindSeekOrder(APlan);
					Nodes[0] = Compiler.EnsureSearchableNode(APlan, SourceNode, LSeekOrder);
					Order = CopyOrder(SourceNode.Order);
					FRestrictionAlgorithm = typeof(SeekTable);
					FFirstKeyNodes = new Condition[Order.Columns.Count];
					for (int LIndex = 0; LIndex < FFirstKeyNodes.Length; LIndex++)
						FFirstKeyNodes[LIndex] = new Condition(FConditions[Order.Columns[LIndex].Column][0].Argument, false);
				}
				else if (FIsScanable)
				{
					// The condition contains range comparisons against the columns of some order
					Schema.Order LScanOrder = FindScanOrder(APlan);
					Nodes[0] = Compiler.EnsureSearchableNode(APlan, SourceNode, LScanOrder);
					Order = CopyOrder(SourceNode.Order);
					FRestrictionAlgorithm = typeof(ScanTable);
				}
			}
			else
			{
				if ((Order == null) && (SourceNode.Order != null))
					Order = CopyOrder(SourceNode.Order);
			}
		}

		public override void DetermineDevice(Plan APlan)
		{
			base.DetermineDevice(APlan);
			if (!FDeviceSupported)
			{
				DetermineRestrictionAlgorithm(APlan);
				if (FRestrictionAlgorithm.Equals(typeof(ScanTable)))
					FCursorCapabilities = FCursorCapabilities | CursorCapability.BackwardsNavigable | CursorCapability.Searchable;
				else if (FRestrictionAlgorithm.Equals(typeof(SeekTable)))
					FCursorCapabilities = FCursorCapabilities | CursorCapability.BackwardsNavigable;
			}
		}
		
		private Conditions FConditions;

		private Conditions FClosedConditions;
		/// <summary>Conditions where both sides of the range are specified.</summary>
		public Conditions ClosedConditions { get { return FClosedConditions; } }

		private Conditions FOpenConditions;
		/// <summary>Conditions where only one side of the range is specified.</summary>
		public Conditions OpenConditions { get { return FOpenConditions; } }

		// TODO: This does not seem to work in general, because it cannot capture the information for the case of multiple open conditions.
		private bool FOpenConditionsUseFirstKey;
		/// <summary>Indicates whether the open conditions should use the first key.</summary>
		public bool OpenConditionsUseFirstKey { get { return FOpenConditionsUseFirstKey; } }
		
		private bool FIsSeekable;
		/// <summary>Indicates whether the restriction condition is seekable.</summary>
		public bool IsSeekable { get { return FIsSeekable; } }
		
		private bool FIsScanable;
		/// <summary>Indicates wheter the restriction condition is scanable.</summary>
		public bool IsScanable { get { return FIsScanable; } }

		private Type FRestrictionAlgorithm;
		public Type RestrictionAlgorithm { get { return FRestrictionAlgorithm; } }
		
		private ColumnConditions FScanCondition;
		public ColumnConditions ScanCondition { get { return FScanCondition; } }
		
		private Condition[] FFirstKeyNodes;
		public Condition[] FirstKeyNodes { get { return FFirstKeyNodes; } }
		
		// BTR 10/22/2004 -> Removed to avoid warning
		//private Condition[] FLastKeyNodes;
		//public Condition[] LastKeyNodes { get { return FLastKeyNodes; } }
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = SourceNode.CursorType;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
		}
		
		// Execute
		public override object InternalExecute(ServerProcess AProcess)
		{
			RestrictTable LTable = (RestrictTable)Activator.CreateInstance(FRestrictionAlgorithm, new object[]{this, AProcess});
			try
			{
				LTable.Open();
				return LTable;
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}
		
		// Statement
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (FShouldEmit)
			{
				Expression LExpression = new RestrictExpression((Expression)Nodes[0].EmitStatement(AMode), (Expression)Nodes[1].EmitStatement(AMode));
				LExpression.Modifiers = Modifiers;
				return LExpression;
			}
			else
				return Nodes[0].EmitStatement(AMode);
		}

		// Compiling an insert statement includes a 'where false' on the target expression as an optimization. This node is marked as ShouldEmit = false.
		protected bool FShouldEmit = true;
		public bool ShouldEmit
		{
			get { return FShouldEmit; }
			set { FShouldEmit = value; }
		}
		
		#if ENFORCERESTRICTIONPREDICATE
		protected bool FEnforcePredicate = true;
		#else
		protected bool FEnforcePredicate = false;
		#endif
		public bool EnforcePredicate
		{
			get { return FEnforcePredicate; }
			set { FEnforcePredicate = value; }
		}
		
		public override void DetermineRemotable(Plan APlan)
		{
			base.DetermineRemotable(APlan);
			
			FTableVar.ShouldValidate = FTableVar.ShouldValidate || FEnforcePredicate;
		}
		
		// Validate
		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (FEnforcePredicate && (AColumnName == String.Empty))
			{
				PushRow(AProcess, ANewRow);
				try
				{
					object LObject = Nodes[1].Execute(AProcess);
					// BTR 05/03/2005 -> Because the restriction considers nil to be false, the validation should consider it false as well.
					if ((LObject == null) || !(bool)LObject)
						throw new RuntimeException(RuntimeException.Codes.NewRowViolatesRestrictPredicate, ErrorSeverity.User);
				}
				finally
				{
					PopRow(AProcess);
				}
			}

			return base.InternalValidate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
		}
		
		public override bool IsContextLiteral(int ALocation)
		{
			if (!Nodes[0].IsContextLiteral(ALocation))
				return false;
			return Nodes[1].IsContextLiteral(ALocation + 1);
		}
    }
}