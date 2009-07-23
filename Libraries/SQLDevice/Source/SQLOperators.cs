/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Device.SQL
{
	using System;
	using System.Collections;
	
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public abstract class SQLDeviceOperator : DeviceOperator
	{
		public SQLDeviceOperator(int AID, string AName) : base(AID, AName)
		{
			FIsTruthValued = GetIsTruthValued();
		}
		
		/// <summary>
		/// This method is used to set the initial value of IsTruthValued.  It should not be used to determine whether or not this
		/// operator is truth-valued as the property can be set after the initialization by a class definition attribute.  Always
		/// use the IsTruthValued property.
		/// </summary>
		protected virtual bool GetIsTruthValued()
		{
			return false;
		}
		
		protected bool FIsTruthValued = false;
		/// <summary>
		///	A given operator is SQL truth valued if it results in an actual SQL boolean value.
		/// In a typical SQL system, only built-in operators such as AND, OR and LIKE can return an actual boolean value.
		/// Boolean values are simulated on these systems using an integer value of 0 for false and 1 for true.
		/// D4 operators that return values of type System.Boolean and are mapped into user-defined functions in a
		/// target system where boolean values are simulated with integers must be marked as non-truth-valued so that
		/// the SQL translator can correctly produce a boolean-valued expression using the function.
		/// </summary>
		public bool IsTruthValued
		{
			get { return FIsTruthValued; }
			set { FIsTruthValued = value; }
		}
		
		private bool[] FIsParameterContextLiteral;
		/// <summary>
		/// Indicates whether or not the given operand is required to be context literal.
		/// </summary>
		public bool IsParameterContextLiteral(int AParameterIndex)
		{
			if (FIsParameterContextLiteral == null)
			{
				FIsParameterContextLiteral = new bool[Operator.Operands.Count];
				ReadIsParameterContextLiteral();
			}
			
			return FIsParameterContextLiteral[AParameterIndex];
		}
		
		public void ReadIsParameterContextLiteral()
		{
			for (int LIndex = 0; LIndex < FIsParameterContextLiteral.Length; LIndex++)
				FIsParameterContextLiteral[LIndex] = false;
				
			if (FContextLiteralParameterIndexes != String.Empty)
				foreach (string LStringIndex in FContextLiteralParameterIndexes.Split(';'))
				{
					int LIndex = Convert.ToInt32(LStringIndex);
					if ((LIndex >= 0) && (LIndex < Operator.Operands.Count))
						FIsParameterContextLiteral[LIndex] = true;
				}
		}

		private string FContextLiteralParameterIndexes = String.Empty;
		/// <summary>
		/// A semi-colon separated list of indexes indicating which operands of the operator are required to be context literal
		/// </summary>
		public string ContextLiteralParameterIndexes
		{
			get { return FContextLiteralParameterIndexes; }
			set 
			{ 
				if (FContextLiteralParameterIndexes != value)
				{
					FContextLiteralParameterIndexes = value == null ? String.Empty : value;
					if (FIsParameterContextLiteral != null)
						ReadIsParameterContextLiteral();
				}
			}
		}
	}
	
    public class SQLRetrieve : SQLDeviceOperator
    {
		public SQLRetrieve(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			TableVar LTableVar = ((TableVarNode)APlanNode).TableVar;

			if (LTableVar is BaseTableVar)
			{
				SQLRangeVar LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
				LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
				SelectExpression LSelectExpression = new SelectExpression();
				LSelectExpression.FromClause = new AlgebraicFromClause(new TableSpecifier(new TableExpression(D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema), LDevicePlan.Device.ToSQLIdentifier(LTableVar)), LRangeVar.Name));
				LSelectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn LColumn in LTableVar.Columns)
				{
					SQLRangeVarColumn LRangeVarColumn = new SQLRangeVarColumn(LColumn, LRangeVar.Name, LDevicePlan.Device.ToSQLIdentifier(LColumn), LDevicePlan.Device.ToSQLIdentifier(LColumn.Name));
					LRangeVar.Columns.Add(LRangeVarColumn);
					LSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
				}

				LSelectExpression.SelectClause.Distinct = 
					(LTableVar.Keys.Count == 1) && 
					Convert.ToBoolean(D4.MetaData.GetTag(LTableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));
				
				return LSelectExpression;
			}
			else
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
    }
    
    public class SQLAdorn : SQLDeviceOperator
    {
		public SQLAdorn(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return LStatement;
		}
    }

    public class SQLRestrict : SQLDeviceOperator
    {
		public SQLRestrict(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			if (LDevicePlan.IsSupported)
			{
				SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LStatement, true);
				LStatement = LSelectExpression;

				LDevicePlan.PushScalarContext();
				try
				{
					LDevicePlan.CurrentQueryContext().IsWhereClause = true;

					LDevicePlan.Stack.Push(new Symbol(((TableNode)APlanNode).DataType.CreateRowType()));
					try
					{
						LDevicePlan.CurrentQueryContext().ResetReferenceFlags();
						
						Expression LExpression = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], true);
						
						FilterClause LClause = null;
						if ((LDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasAggregateExpressions) != 0)
						{
							if (LSelectExpression.HavingClause == null)
								LSelectExpression.HavingClause = new HavingClause();
							LClause = LSelectExpression.HavingClause;
							
							if (((LDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0) && !LDevicePlan.Device.SupportsSubSelectInHavingClause)
							{
								LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the having clause contains sub-select expressions and the device does not support sub-selects in the having clause.", APlanNode));
								LDevicePlan.IsSupported = false;
							}
						}
						else
						{
							if (LSelectExpression.WhereClause == null)
								LSelectExpression.WhereClause = new WhereClause();
							LClause = LSelectExpression.WhereClause;
							
							if (((LDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0) && !LDevicePlan.Device.SupportsSubSelectInWhereClause)
							{
								LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the where clause contains sub-select expressions and the device does not support sub-selects in the where clause.", APlanNode));
								LDevicePlan.IsSupported = false;
							}
						}
							
						if (LClause.Expression == null)				
							LClause.Expression = LExpression;
						else
							LClause.Expression = new BinaryExpression(LClause.Expression, "iAnd", LExpression);

						return LStatement;
					}
					finally
					{
						LDevicePlan.Stack.Pop();
					}
				}
				finally
				{
					LDevicePlan.PopScalarContext();
				}
			}
			
			return LStatement;
		}
    }

    public class SQLProject : SQLDeviceOperator
    {
		public SQLProject(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			
			// Project where distinct is required is only supported if the device supports iEqual or iCompare for the data types of each column involved in the projection
			ProjectNodeBase LProjectNode = (ProjectNodeBase)APlanNode;
			
			if (LProjectNode.DistinctRequired)
				foreach (Schema.TableVarColumn LColumn in LProjectNode.TableVar.Columns)
					if (!LDevicePlan.Device.SupportsEqual(ADevicePlan.Plan, LColumn.Column.DataType))
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support equality comparison for values of type ""{0}"" which is the type of column ""{1}"" included in the projection.", LColumn.Column.DataType.Name, LColumn.Column.Name), APlanNode));
						LDevicePlan.IsSupported = false;
						break;
					}
			
			if (LDevicePlan.IsSupported)
			{
				Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
				if (LDevicePlan.IsSupported)
				{
					SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LStatement, false);
					LStatement = LSelectExpression;

					if (LProjectNode.DistinctRequired)
						LSelectExpression.SelectClause.Distinct = true;
						
					LSelectExpression.SelectClause.Columns.Clear();
					foreach (TableVarColumn LColumn in ((TableNode)APlanNode).TableVar.Columns)
						LSelectExpression.SelectClause.Columns.Add(LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());
						
					LDevicePlan.CurrentQueryContext().ProjectColumns(((TableNode)APlanNode).TableVar.Columns);
					
					if (LSelectExpression.SelectClause.Columns.Count == 0)
						LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(1), "dummy"));
						
					return LStatement;
				}
			}

			return new SelectExpression();
		}
    }

	//	select customer as c ::=    
    //		select c_id as c_id, c_name as c_name from (select id as c_id, name as c_name from customer) as T1
    public class SQLRename : SQLDeviceOperator
    {
		public SQLRename(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{									
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			if (LDevicePlan.IsSupported)
			{
				SelectExpression LSelectExpression = 
						APlanNode is RowRenameNode ? 
							LDevicePlan.Device.FindSelectExpression(LStatement) : 
							LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LStatement, false);
				LStatement = LSelectExpression;
				SQLRangeVarColumn LRangeVarColumn;
				LSelectExpression.SelectClause.Columns.Clear();
				
				if (APlanNode is RowRenameNode)
				{
					Schema.IRowType LSourceRowType = APlanNode.Nodes[0].DataType as Schema.IRowType;
					Schema.IRowType LTargetRowType = APlanNode.DataType as Schema.IRowType;
					for (int LIndex = 0; LIndex < LSourceRowType.Columns.Count; LIndex++)
					{
						LRangeVarColumn = LDevicePlan.CurrentQueryContext().RenameColumn(LDevicePlan, new Schema.TableVarColumn(LSourceRowType.Columns[LIndex]), new Schema.TableVarColumn(LTargetRowType.Columns[LIndex]));
						LSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
					}
				} 
				else if (APlanNode is RenameNode)
				{
					TableVar LSourceTableVar = ((RenameNode)APlanNode).SourceTableVar;
					TableVar LTableVar = ((TableNode)APlanNode).TableVar;
					for (int LIndex = 0; LIndex < LTableVar.Columns.Count; LIndex++)
					{
						LRangeVarColumn = LDevicePlan.CurrentQueryContext().RenameColumn(LDevicePlan, LSourceTableVar.Columns[LIndex], LTableVar.Columns[LIndex]);
						LSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
					}
				}
			}

			return LStatement;
		}
    }

	/*
		select A add { <expression> X } ::=
			select <column list>, X from (select <column list>, <expression> X from A)
	*/    
    public class SQLExtend : SQLDeviceOperator
    {
		public SQLExtend(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			ExtendNode LExtendNode = (ExtendNode)APlanNode;
			TableVar LTableVar = LExtendNode.TableVar;
			Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			if (LDevicePlan.IsSupported)
			{
				SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LStatement, true);
				LStatement = LSelectExpression;

				LDevicePlan.Stack.Push(new Symbol(LExtendNode.DataType.CreateRowType()));
				try
				{
					LDevicePlan.CurrentQueryContext().IsExtension = true;
					LDevicePlan.PushScalarContext();
					try
					{
						LDevicePlan.CurrentQueryContext().IsSelectClause = true;
						int LExtendColumnIndex = 1;
						for (int LIndex = LExtendNode.ExtendColumnOffset; LIndex < LExtendNode.DataType.Columns.Count; LIndex++)
						{
							LDevicePlan.CurrentQueryContext().ResetReferenceFlags();
							SQLRangeVarColumn LRangeVarColumn = 
								new SQLRangeVarColumn
								(
									LTableVar.Columns[LIndex], 
									LDevicePlan.Device.TranslateExpression(LDevicePlan, LExtendNode.Nodes[LExtendColumnIndex], false), 
									LDevicePlan.Device.ToSQLIdentifier(LTableVar.Columns[LIndex])
								);
							LRangeVarColumn.ReferenceFlags = LDevicePlan.CurrentQueryContext().ReferenceFlags;
							LDevicePlan.CurrentQueryContext().ParentContext.AddedColumns.Add(LRangeVarColumn);
							LDevicePlan.CurrentQueryContext().ParentContext.ReferenceFlags |= LRangeVarColumn.ReferenceFlags;
							LSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
							LExtendColumnIndex++;
						}
					}
					finally
					{
						LDevicePlan.PopScalarContext();
					}
				}
				finally
				{
					LDevicePlan.Stack.Pop();
				}
			}
				
			return LStatement;
		}
    }

	/*
		select A redefine { ID := <expression> } ::=
			select <column list>, ID from (select <column list>, <expression> as ID from A);
	*/    
    public class SQLRedefine : SQLDeviceOperator
    {
		public SQLRedefine(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			RedefineNode LRedefineNode = (RedefineNode)APlanNode;
			TableVar LTableVar = LRedefineNode.TableVar;
			Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			if (LDevicePlan.IsSupported)
			{
				SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LStatement, true);
				LStatement = LSelectExpression;

				LDevicePlan.Stack.Push(new Symbol(LRedefineNode.DataType.CreateRowType()));
				try
				{
					LDevicePlan.CurrentQueryContext().IsExtension = true;
					LDevicePlan.PushScalarContext();
					try
					{
						LDevicePlan.CurrentQueryContext().IsSelectClause = true;
						for (int LColumnIndex = 0; LColumnIndex < LTableVar.Columns.Count; LColumnIndex++)
						{
							int LRedefineIndex = ((IList)LRedefineNode.RedefineColumnOffsets).IndexOf(LColumnIndex);
							if (LRedefineIndex >= 0)
							{
								SQLRangeVarColumn LRangeVarColumn = LDevicePlan.GetRangeVarColumn(LTableVar.Columns[LColumnIndex].Name, true);
								LDevicePlan.CurrentQueryContext().ResetReferenceFlags();
								LRangeVarColumn.Expression = LDevicePlan.Device.TranslateExpression(LDevicePlan, LRedefineNode.Nodes[LRedefineIndex + 1], false);
								LRangeVarColumn.ReferenceFlags = LDevicePlan.CurrentQueryContext().ReferenceFlags;
								LDevicePlan.CurrentQueryContext().ParentContext.ReferenceFlags |= LRangeVarColumn.ReferenceFlags;
								LSelectExpression.SelectClause.Columns[LColumnIndex].Expression = LRangeVarColumn.Expression;
							}
						}
					}
					finally
					{
						LDevicePlan.PopScalarContext();
					}
				}
				finally
				{
					LDevicePlan.Stack.Pop();
				}
			}

			return LStatement;
		}
    }
    
	/*
		<expression> group [by <column list>] add { <aggregate expression list> };
		
		select <column list>, <aggregate expression list>
			from <expression>
			group by <column list>
	*/
    public class SQLAggregate : SQLDeviceOperator
    {
		public SQLAggregate(int AID, string AName) : base(AID, AName) {}

		protected AggregateCallExpression FindAggregateCallExpression(Expression AExpression)
		{
			AggregateCallExpression LCallExpression = AExpression as AggregateCallExpression;
			if (LCallExpression == null)
			{
				CaseExpression LCaseExpression = AExpression as CaseExpression;
				if (LCaseExpression != null)
					LCallExpression = ((BinaryExpression)LCaseExpression.CaseItems[0].WhenExpression).LeftExpression as AggregateCallExpression;
			}
			return LCallExpression;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			AggregateNode LAggregateNode = (AggregateNode)APlanNode;
			TableVar LTableVar = LAggregateNode.TableVar;
			Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, LAggregateNode.OriginalSourceNode, false);
			if (LDevicePlan.IsSupported)
			{
				SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)LAggregateNode.OriginalSourceNode).TableVar, LStatement, true);
				LStatement = LSelectExpression;

				string LNestingReason = String.Empty;
				bool LNest = LDevicePlan.CurrentQueryContext().IsAggregate || LSelectExpression.SelectClause.Distinct || ((LDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0);
				if (LNest)
					if (LDevicePlan.CurrentQueryContext().IsAggregate)
						LNestingReason = "The argument to the aggregate operator must be nested because it contains aggregation.";
					else if (LSelectExpression.SelectClause.Distinct)
						LNestingReason = "The argument to the aggregate operator must be nested because it contains a distinct specification.";
					else
						LNestingReason = "The argument to the aggregate operator must be nested because it contains sub-select expressions.";
				else
				{
					// If the group by columns are not literals in SQL, we must nest
					for (int LIndex = 0; LIndex < LAggregateNode.AggregateColumnOffset; LIndex++)
						if ((LDevicePlan.GetRangeVarColumn(LTableVar.Columns[LIndex].Name, true).ReferenceFlags & SQLReferenceFlags.HasParameters) != 0)
						{
							LNest = true;
							break;
						}
						
					if (LNest)
						LNestingReason = "The argument to the aggregate operator must be nested because it contains expressions which reference parameters.";
				}

				if (LNest)
				{
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LNestingReason, APlanNode));
					LStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, ((TableNode)LAggregateNode.OriginalSourceNode).TableVar, LStatement);
					LSelectExpression = LDevicePlan.Device.FindSelectExpression(LStatement);				
				}

				LDevicePlan.CurrentQueryContext().IsAggregate = true;			
				LSelectExpression.SelectClause = new SelectClause();
				for (int LIndex = 0; LIndex < LAggregateNode.AggregateColumnOffset; LIndex++)
				{
					SQLRangeVarColumn LGroupByColumn = LDevicePlan.GetRangeVarColumn(LTableVar.Columns[LIndex].Name, true);
					LSelectExpression.SelectClause.Columns.Add(LGroupByColumn.GetColumnExpression());
					if (LSelectExpression.GroupClause == null)
						LSelectExpression.GroupClause = new GroupClause();
					LSelectExpression.GroupClause.Columns.Add(LGroupByColumn.GetExpression());
				}

				for (int LIndex = LAggregateNode.AggregateColumnOffset; LIndex < LAggregateNode.DataType.Columns.Count; LIndex++)
				{
					LDevicePlan.CurrentQueryContext().ResetReferenceFlags();
					Expression LExpression = LDevicePlan.Device.TranslateExpression(LDevicePlan, LAggregateNode.Nodes[(LIndex - LAggregateNode.AggregateColumnOffset) + 1], false);
					FindAggregateCallExpression(LExpression).IsDistinct = LAggregateNode.ComputeColumns[LIndex - LAggregateNode.AggregateColumnOffset].Distinct;
					SQLRangeVarColumn LRangeVarColumn = 
						new SQLRangeVarColumn
						(
							LTableVar.Columns[LIndex],
							LExpression,
							LDevicePlan.Device.ToSQLIdentifier(LTableVar.Columns[LIndex])
						);
					LRangeVarColumn.ReferenceFlags = LDevicePlan.CurrentQueryContext().ReferenceFlags | SQLReferenceFlags.HasAggregateExpressions;
					LDevicePlan.CurrentQueryContext().AddedColumns.Add(LRangeVarColumn);
					LSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
				}
			}
			
			return LStatement;
		}
    }
    
    public class SQLAggregateOperator : SQLDeviceOperator
    {
		public SQLAggregateOperator(int AID, string AName) : base(AID, AName) {}
		
		private string FOperatorName;
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value; }
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			// If this is a scalar invocation, it must be translated as a subselect
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			AggregateCallNode LNode = (AggregateCallNode)APlanNode;
			TableVar LSourceTableVar = ((TableNode)LNode.Nodes[0]).TableVar;
			AggregateCallExpression LExpression = new AggregateCallExpression();
			LExpression.Identifier = FOperatorName;
			
			if (!LDevicePlan.CurrentQueryContext().IsScalarContext)
			{
				if (LNode.AggregateColumnIndexes.Length > 0)
					for (int LIndex = 0; LIndex < LNode.AggregateColumnIndexes.Length; LIndex++)
					{
						SQLRangeVarColumn LRangeVarColumn = LDevicePlan.FindRangeVarColumn(LSourceTableVar.Columns[LNode.AggregateColumnIndexes[LIndex]].Name, true);
						if (LRangeVarColumn == null)
							LExpression.Expressions.Add(new QualifiedFieldExpression("*")); // If we don't find the column, we are being evaluated out of context, and must return true in order to prevent the overall aggregate from being incorrectly unsupported
						else
							LExpression.Expressions.Add(LRangeVarColumn.GetExpression());
					}
				else	
					LExpression.Expressions.Add(new QualifiedFieldExpression("*")); 
					
				return LExpression;
			}
			else
			{
				LDevicePlan.CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasSubSelectExpressions;
				bool LIsSupported = LDevicePlan.IsSubSelectSupported();
				if (!LIsSupported)
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LDevicePlan.GetSubSelectNotSupportedReason(), APlanNode));
				LDevicePlan.IsSupported = LDevicePlan.IsSupported && LIsSupported;
				LDevicePlan.Stack.Push(new Symbol(D4.Keywords.Result, LNode.DataType));
				try
				{
					for (int LIndex = 0; LIndex < LNode.Operator.Initialization.StackDisplacement; LIndex++)
						LDevicePlan.Stack.Push(new Symbol(String.Empty, ADevicePlan.Plan.Catalog.DataTypes.SystemScalar));
						
					for (int LIndex = 0; LIndex < LNode.AggregateColumnIndexes.Length; LIndex++)
						LDevicePlan.Stack.Push(new Symbol(LNode.ValueNames[LIndex], LSourceTableVar.Columns[LNode.AggregateColumnIndexes[LIndex]].DataType));
					try
					{
						LDevicePlan.PushQueryContext();
						try
						{
							Statement LStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
							if (LDevicePlan.IsSupported)
							{
								SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LStatement, false);
								LStatement = LSelectExpression;
									
								string LNestingReason = String.Empty;
								bool LNest = 
									LDevicePlan.CurrentQueryContext().IsAggregate 
										|| LSelectExpression.SelectClause.Distinct 
										|| ((LDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0);

								if (LNest)
								{
									if (LDevicePlan.CurrentQueryContext().IsAggregate)
										LNestingReason = "The argument to the aggregate operator must be nested because it contains aggregation.";
									else if ((LDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasAggregateExpressions) != 0)
										LNestingReason = "The argument to the aggregate operator must be nested because it contains subselect expressions.";
									else
										LNestingReason = "The argument to the aggregate operator must be nested because it contains a distinct specification.";

									LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LNestingReason, APlanNode));
									LStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LSourceTableVar, LStatement);
									LSelectExpression = LDevicePlan.Device.FindSelectExpression(LStatement);				
								}
									
								if (LNode.AggregateColumnIndexes.Length > 0)
									for (int LIndex = 0; LIndex < LNode.AggregateColumnIndexes.Length; LIndex++)
										LExpression.Expressions.Add(LDevicePlan.CurrentQueryContext().GetRangeVarColumn(LSourceTableVar.Columns[LNode.AggregateColumnIndexes[LIndex]].Name).GetExpression());
								else	
									LExpression.Expressions.Add(new QualifiedFieldExpression("*"));

								LSelectExpression.SelectClause = new SelectClause();
								LSelectExpression.SelectClause.Columns.Add(new ColumnExpression(LExpression, "dummy1"));
							}

							return LStatement;
						}
						finally
						{
							LDevicePlan.PopQueryContext();
						}
					}
					finally
					{
						for (int LIndex = 0; LIndex < LNode.AggregateColumnIndexes.Length; LIndex++)
							LDevicePlan.Stack.Pop();

						for (int LIndex = 0; LIndex < LNode.Operator.Initialization.StackDisplacement; LIndex++)
							LDevicePlan.Stack.Pop();
					}
				}
				finally
				{
					LDevicePlan.Stack.Pop();
				}
			}
		}
    }
    
    public class SQLOrder : SQLDeviceOperator
    {
		public SQLOrder(int AID, string AName) : base(AID, AName) {}
		
		// Order is ignored until the very last order in the expression, which is translated in the device
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			return ADevicePlan.Device.Translate(ADevicePlan, APlanNode.Nodes[0]);
		}
    }

    public class SQLUnion : SQLDeviceOperator
    {
		public SQLUnion(int AID, string AName) : base(AID, AName) {}
		
		public static void NormalizeSelectClause(DevicePlan ADevicePlan, ColumnExpressions ANormalColumns, ColumnExpressions ANonNormalColumns)
		{
			ColumnExpressions LNonNormalColumns = new ColumnExpressions();
			LNonNormalColumns.AddRange(ANonNormalColumns);
			ANonNormalColumns.Clear();
			foreach (ColumnExpression LColumnExpression in ANormalColumns)
				ANonNormalColumns.Add(LNonNormalColumns[LColumnExpression.ColumnAlias]);
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			LDevicePlan.PushQueryContext();
			Statement LLeftStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			LDevicePlan.PopQueryContext();
			LDevicePlan.PushQueryContext();
			Statement LRightStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			LDevicePlan.PopQueryContext();
			
			if (LDevicePlan.IsSupported)
			{
				SQLRangeVar LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
				LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
				Schema.TableVar LTableVar = ((TableNode)APlanNode).TableVar;
				foreach (Schema.TableVarColumn LColumn in LTableVar.Columns)
					LRangeVar.Columns.Add(new SQLRangeVarColumn(LColumn, LRangeVar.Name, LDevicePlan.Device.ToSQLIdentifier(LColumn.Name)));
				
				if (LLeftStatement is QueryExpression)
				{
					QueryExpression LLeftQueryExpression = (QueryExpression)LLeftStatement;
					if (!LLeftQueryExpression.IsCompatibleWith(TableOperator.Union))
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The left argument to the union operator must be nested because it contains non-union table operations.", APlanNode));
						LLeftStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[1]).TableVar, LLeftStatement);
					}
				}
				
				if (!(LLeftStatement is QueryExpression))
				{
					QueryExpression LQueryExpression = new QueryExpression();
					LQueryExpression.SelectExpression = (SelectExpression)LLeftStatement;
					LLeftStatement = LQueryExpression;
				}
				
				ColumnExpressions LNormalColumns = LDevicePlan.Device.FindSelectExpression(LLeftStatement).SelectClause.Columns;
				
				if (LRightStatement is QueryExpression)
				{
					QueryExpression LRightQueryExpression = (QueryExpression)LRightStatement;
					if (!LRightQueryExpression.IsCompatibleWith(TableOperator.Union))
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The right argument to the union operator must be nested because it contains non-union table operations.", APlanNode));
						LRightStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[1]).TableVar, LRightStatement);
					}
					else
						foreach (TableOperatorExpression LTableOperatorExpression in LRightQueryExpression.TableOperators)
							NormalizeSelectClause(ADevicePlan, LNormalColumns, LTableOperatorExpression.SelectExpression.SelectClause.Columns);
				}
				
				NormalizeSelectClause(ADevicePlan, LNormalColumns, LDevicePlan.Device.FindSelectExpression(LRightStatement).SelectClause.Columns);
				
				if (LRightStatement is QueryExpression)
					LRightStatement = ((QueryExpression)LRightStatement).SelectExpression;
					
				((QueryExpression)LLeftStatement).TableOperators.Add(new TableOperatorExpression(TableOperator.Union, true, (SelectExpression)LRightStatement));
			}
			
			return LLeftStatement;
		}
    }
    
    public class SQLDifference : SQLDeviceOperator
    {
		public SQLDifference(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			LDevicePlan.PushQueryContext();
			Statement LLeftStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			LDevicePlan.PopQueryContext();
			LDevicePlan.PushQueryContext();
			Statement LRightStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
			LDevicePlan.PopQueryContext();
			
			if (LDevicePlan.IsSupported)
			{
				SQLRangeVar LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
				LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
				Schema.TableVar LTableVar = ((TableNode)APlanNode).TableVar;
				foreach (Schema.TableVarColumn LColumn in LTableVar.Columns)
					LRangeVar.Columns.Add(new SQLRangeVarColumn(LColumn, LRangeVar.Name, LDevicePlan.Device.ToSQLIdentifier(LColumn.Name)));
				
				if (LLeftStatement is QueryExpression)
				{
					QueryExpression LLeftQueryExpression = (QueryExpression)LLeftStatement;
					if (!LLeftQueryExpression.IsCompatibleWith(TableOperator.Difference))
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The left argument to the difference operator must be nested because it contains non-difference table operations.", APlanNode));
						LLeftStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[1]).TableVar, LLeftStatement);
					}
				}
				
				if (!(LLeftStatement is QueryExpression))
				{
					QueryExpression LQueryExpression = new QueryExpression();
					LQueryExpression.SelectExpression = (SelectExpression)LLeftStatement;
					LLeftStatement = LQueryExpression;
				}
				
				ColumnExpressions LNormalColumns = LDevicePlan.Device.FindSelectExpression(LLeftStatement).SelectClause.Columns;
				
				if (LRightStatement is QueryExpression)
				{
					QueryExpression LRightQueryExpression = (QueryExpression)LRightStatement;
					if (!LRightQueryExpression.IsCompatibleWith(TableOperator.Union))
					{
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The right argument to the difference operator must be nested because it contains non-difference table operations.", APlanNode));
						LRightStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[1]).TableVar, LRightStatement);
					}
					else
						foreach (TableOperatorExpression LTableOperatorExpression in LRightQueryExpression.TableOperators)
							SQLUnion.NormalizeSelectClause(ADevicePlan, LNormalColumns, LTableOperatorExpression.SelectExpression.SelectClause.Columns);
				}
				
				SQLUnion.NormalizeSelectClause(ADevicePlan, LNormalColumns, LDevicePlan.Device.FindSelectExpression(LRightStatement).SelectClause.Columns);
				
				if (LRightStatement is QueryExpression)
					LRightStatement = ((QueryExpression)LRightStatement).SelectExpression;
					
				((QueryExpression)LLeftStatement).TableOperators.Add(new TableOperatorExpression(TableOperator.Difference, true, (SelectExpression)LRightStatement));
			}
			
			return LLeftStatement;
		}
    }
    
    public class SQLJoin : SQLDeviceOperator
    {
		public SQLJoin(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;

			// Translate left operand
			LDevicePlan.PushQueryContext();
			Statement LLeftStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			if (LDevicePlan.IsSupported)
			{
				SelectExpression LLeftSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LLeftStatement, false);
				LLeftStatement = LLeftSelectExpression;
				TableVar LLeftTableVar = ((TableNode)APlanNode.Nodes[0]).TableVar;

				ConditionedTableNode LConditionedTableNode = (ConditionedTableNode)APlanNode;

				// if any column in the left join key is a computed column in the current query context, the left argument must nested
				bool LIsLeftKeyColumnComputed = false;
				foreach (TableVarColumn LLeftKeyColumn in LConditionedTableNode.LeftKey.Columns)
					if (LDevicePlan.GetRangeVarColumn(LLeftKeyColumn.Name, true).Expression != null)
					{
						LIsLeftKeyColumnComputed = true;
						break;
					}
				
				if 
				(
					LDevicePlan.CurrentQueryContext().IsAggregate || 
					LLeftSelectExpression.SelectClause.Distinct || 
					LIsLeftKeyColumnComputed ||
					(
						(APlanNode is RightOuterJoinNode) && 
						(
							LDevicePlan.CurrentQueryContext().IsExtension ||
							(((RightOuterJoinNode)APlanNode).IsNatural && (LDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)) ||
							(LLeftSelectExpression.WhereClause != null) || 
							LLeftSelectExpression.FromClause.HasJoins()
						)
					)
				)
				{
					string LNestingReason = "The left argument to the join operator must be nested because ";
					if (LIsLeftKeyColumnComputed)
						LNestingReason += "the join condition columns in the left argument are computed.";
					else if (APlanNode is RightOuterJoinNode)
					{
						if (LLeftSelectExpression.WhereClause != null)
							LNestingReason += "the join is right outer and the left argument has a where clause.";
						else if (LDevicePlan.CurrentQueryContext().IsExtension)
							LNestingReason += "the join is right outer and the left argument has computed columns.";
						else if (LDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)
							LNestingReason += "the join is a natural right outer and the left argument has renamed columns.";
						else if (LLeftSelectExpression.FromClause.HasJoins())
							LNestingReason += "the join is right outer and the left argument contains at least one join.";
					}
					else if (LDevicePlan.CurrentQueryContext().IsAggregate)
						LNestingReason += "it contains aggregation.";
					else
						LNestingReason += "it contains a distinct specification.";
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LNestingReason, APlanNode));
					LLeftStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LLeftTableVar, LLeftStatement);
					LLeftSelectExpression = LDevicePlan.Device.FindSelectExpression(LLeftStatement);
				}
				SQLQueryContext LLeftContext = LDevicePlan.CurrentQueryContext();
				LDevicePlan.PopQueryContext();
				
				// Translate right operand
				LDevicePlan.PushQueryContext();
				Statement LRightStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
				if (LDevicePlan.IsSupported)
				{
					SelectExpression LRightSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[1]).TableVar, LRightStatement, false);
					LRightStatement = LRightSelectExpression;
					TableVar LRightTableVar = ((TableNode)APlanNode.Nodes[1]).TableVar;
					
					/*
						If the join is inner
							If the left argument has joins
								If the right argument has joins
									The right argument must be nested
							else
								The right from clause is pulled first (right-deep translation of the join)
						else
							The left from clause is pulled first (left-deep translation of the join)
							
						The nesting resulting from a join with joins in both arguments could be avoided if one of two things occurred:
							1. A join-column context tracking subsystem were introduced into the SQL translation system so that the origin of a join column
								could be tracked and this information used to determine what column a given column was equivalent to in the set of columns
								available in a given join context.
							2. Or, the DAE could always produce left-deep join trees, resulting in correct translation in all cases.
					*/

					// if any column in the left join key is a computed column in the current query context, the left argument must nested
					bool LIsRightKeyColumnComputed = false;
					foreach (TableVarColumn LRightKeyColumn in LConditionedTableNode.RightKey.Columns)
						if (LDevicePlan.GetRangeVarColumn(LRightKeyColumn.Name, true).Expression != null)
						{
							LIsRightKeyColumnComputed = true;
							break;
						}
					
					bool LIsRightDeep = (!(APlanNode is OuterJoinNode) && !(APlanNode is WithoutNode) && !LLeftSelectExpression.FromClause.HasJoins() && LRightSelectExpression.FromClause.HasJoins());
					bool LIsBushy = (!(APlanNode is OuterJoinNode) && LLeftSelectExpression.FromClause.HasJoins() && LRightSelectExpression.FromClause.HasJoins());
					if 
					(
						LDevicePlan.CurrentQueryContext().IsAggregate || 
						LRightSelectExpression.SelectClause.Distinct || 
						LIsRightKeyColumnComputed ||
						(
							((APlanNode is LeftOuterJoinNode) || (APlanNode is WithoutNode)) && 
							(
								LDevicePlan.CurrentQueryContext().IsExtension ||
								(((ConditionedTableNode)APlanNode).IsNatural && (LDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)) ||
								(LRightSelectExpression.WhereClause != null) || 
								LRightSelectExpression.FromClause.HasJoins()
							)
						) || 
						LIsBushy
					)
					{
						string LNestingReason = "The right argument to the join operator must be nested because ";
						if (LIsRightKeyColumnComputed)
							LNestingReason += "the join condition columns in the right argument are computed.";
						else if ((APlanNode is LeftOuterJoinNode) || (APlanNode is WithoutNode))
						{
							if (LRightSelectExpression.WhereClause != null)
								LNestingReason += "the join is left outer and the right argument has a where clause.";
							else if (LDevicePlan.CurrentQueryContext().IsExtension)
								LNestingReason += "the join is left outer and the right argument has computed columns.";
							else if (LDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)
								LNestingReason += "the join is a natural left outer and the right argument has renamed columns.";
							else if (LRightSelectExpression.FromClause.HasJoins())
								LNestingReason += "the join is left outer and the right argument has at least one join.";
						}
						else if (LDevicePlan.CurrentQueryContext().IsAggregate)
							LNestingReason += "it contains aggregation.";
						else if (LIsBushy)
							LNestingReason += "both the left and right join arguments contain joins.";
						else
							LNestingReason += "it contains a distinct specification.";
						LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(LNestingReason, APlanNode));
						LRightStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LRightTableVar, LRightStatement);
						LRightSelectExpression = LDevicePlan.Device.FindSelectExpression(LRightStatement);
					}
					SQLQueryContext LRightContext = LDevicePlan.CurrentQueryContext();
					LDevicePlan.PopQueryContext();
					
					// Merge the query contexts
					LDevicePlan.CurrentQueryContext().RangeVars.AddRange(LLeftContext.RangeVars);
					LDevicePlan.CurrentQueryContext().AddedColumns.AddRange(LLeftContext.AddedColumns);
					if ((APlanNode is WithoutNode) || (APlanNode is HavingNode))
					{
						// If this is a Without or Having then the non-join columns of the right argument are not relevant and should not be available in the query context
						foreach (SQLRangeVar LRangeVar in LRightContext.RangeVars)
						{
							SQLRangeVar LNewRangeVar = new SQLRangeVar(LRangeVar.Name);
							foreach (SQLRangeVarColumn LRangeVarColumn in LRangeVar.Columns)
								if (LConditionedTableNode.RightKey.Columns.ContainsName(LRangeVarColumn.TableVarColumn.Name))
									LNewRangeVar.Columns.Add(LRangeVarColumn);
							LDevicePlan.CurrentQueryContext().RangeVars.Add(LNewRangeVar);
						}
						
						foreach (SQLRangeVarColumn LRangeVarColumn in LRightContext.AddedColumns)
							if (LConditionedTableNode.RightKey.Columns.ContainsName(LRangeVarColumn.TableVarColumn.Name))
								LDevicePlan.CurrentQueryContext().AddedColumns.Add(LRangeVarColumn);
					}
					else
					{
						LDevicePlan.CurrentQueryContext().RangeVars.AddRange(LRightContext.RangeVars);
						LDevicePlan.CurrentQueryContext().AddedColumns.AddRange(LRightContext.AddedColumns);
					}
					
					JoinClause LJoinClause = new JoinClause();
					if (LIsRightDeep)
					{
						LJoinClause.FromClause = (AlgebraicFromClause)LLeftSelectExpression.FromClause;
						LLeftSelectExpression.FromClause = LRightSelectExpression.FromClause;
					}
					else
						LJoinClause.FromClause = (AlgebraicFromClause)LRightSelectExpression.FromClause;

					LDevicePlan.PushJoinContext(new SQLJoinContext(LLeftContext, LRightContext));
					try
					{
						LeftOuterJoinNode LLeftOuterJoinNode = APlanNode as LeftOuterJoinNode;
						RightOuterJoinNode LRightOuterJoinNode = APlanNode as RightOuterJoinNode;
						SemiTableNode LSemiTableNode = APlanNode as SemiTableNode;
						HavingNode LHavingNode = APlanNode as HavingNode;
						WithoutNode LWithoutNode = APlanNode as WithoutNode;

						if (LLeftOuterJoinNode != null)
							LJoinClause.JoinType = JoinType.Left;
						else if (LRightOuterJoinNode != null)
							LJoinClause.JoinType = JoinType.Right;
						else
						{
							if (LWithoutNode != null)
								LJoinClause.JoinType = JoinType.Left;
							else
								LJoinClause.JoinType = JoinType.Inner;
						}

						LDevicePlan.Stack.Push(new Symbol(new Schema.RowType(((TableNode)APlanNode.Nodes[0]).DataType.Columns, Keywords.Left)));
						try
						{
							LDevicePlan.Stack.Push(new Symbol(new Schema.RowType(((TableNode)APlanNode.Nodes[1]).DataType.Columns, Keywords.Right)));
							try
							{
								LJoinClause.JoinExpression = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[2], true);
							}
							finally
							{
								LDevicePlan.Stack.Pop();
							}
						}
						finally
						{
							LDevicePlan.Stack.Pop();
						}
							
						// Translate rowexists column
						if ((APlanNode is OuterJoinNode) && (((OuterJoinNode)APlanNode).RowExistsColumnIndex >= 0))
						{
							OuterJoinNode LOuterJoinNode = (OuterJoinNode)APlanNode;
							TableVarColumn LRowExistsColumn = LOuterJoinNode.TableVar.Columns[LOuterJoinNode.RowExistsColumnIndex];
							Expression LRowExistsExpression = null;
							if (LOuterJoinNode.LeftKey.Columns.Count == 0)
								LRowExistsExpression = new ValueExpression(1);
							else
							{
								CaseExpression LCaseExpression = new CaseExpression();
								CaseItemExpression LCaseItem = new CaseItemExpression();
								if (LLeftOuterJoinNode != null)
									LCaseItem.WhenExpression = new UnaryExpression("iIsNull", LDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(LOuterJoinNode.RightKey.Columns[0].Name).GetExpression());
								else
									LCaseItem.WhenExpression = new UnaryExpression("iIsNull", LDevicePlan.CurrentJoinContext().LeftQueryContext.GetRangeVarColumn(LOuterJoinNode.LeftKey.Columns[0].Name).GetExpression());
								LCaseItem.ThenExpression = new ValueExpression(0);
								LCaseExpression.CaseItems.Add(LCaseItem);
								LCaseExpression.ElseExpression = new CaseElseExpression(new ValueExpression(1));
								LRowExistsExpression = LCaseExpression;
							}
							SQLRangeVarColumn LRangeVarColumn = new SQLRangeVarColumn(LRowExistsColumn, LRowExistsExpression, LDevicePlan.Device.ToSQLIdentifier(LRowExistsColumn));
							LDevicePlan.CurrentQueryContext().AddedColumns.Add(LRangeVarColumn);
							LLeftSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
						}

						((AlgebraicFromClause)LLeftSelectExpression.FromClause).Joins.Add(LJoinClause);
						
						// Build select clause
						LLeftSelectExpression.SelectClause = new SelectClause();
						foreach (TableVarColumn LColumn in ((TableNode)APlanNode).TableVar.Columns)
							if ((LLeftOuterJoinNode != null) && LLeftOuterJoinNode.LeftKey.Columns.ContainsName(LColumn.Name))
								LLeftSelectExpression.SelectClause.Columns.Add(LLeftContext.GetRangeVarColumn(LColumn.Name).GetColumnExpression());
							else if ((LRightOuterJoinNode != null) && LRightOuterJoinNode.RightKey.Columns.ContainsName(LColumn.Name))
								LLeftSelectExpression.SelectClause.Columns.Add(LRightContext.GetRangeVarColumn(LColumn.Name).GetColumnExpression());
							else if ((LWithoutNode != null) && LWithoutNode.LeftKey.Columns.ContainsName(LColumn.Name))
								LLeftSelectExpression.SelectClause.Columns.Add(LLeftContext.GetRangeVarColumn(LColumn.Name).GetColumnExpression());
							else
								LLeftSelectExpression.SelectClause.Columns.Add(LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());
							
						// Merge where clauses
						if (LRightSelectExpression.WhereClause != null)
							if (LLeftSelectExpression.WhereClause == null)
								LLeftSelectExpression.WhereClause = LRightSelectExpression.WhereClause;
							else
								LLeftSelectExpression.WhereClause.Expression = new BinaryExpression(LLeftSelectExpression.WhereClause.Expression, "iAnd", LRightSelectExpression.WhereClause.Expression);
								
						// Distinct if necessary
						if ((LSemiTableNode != null) && !LSemiTableNode.RightKey.IsUnique)
							LLeftSelectExpression.SelectClause.Distinct = true;
								
						// Add without where clause
						if (LWithoutNode != null)
						{
							Expression LWithoutExpression = null;
							
							foreach (TableVarColumn LColumn in LWithoutNode.RightKey.Columns.Count > 0 ? (TableVarColumnsBase)LWithoutNode.RightKey.Columns : (TableVarColumnsBase)LRightTableVar.Columns)
							{
								Expression LColumnExpression = new UnaryExpression("iIsNull", LDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(LColumn.Name).GetExpression());
								
								if (LWithoutExpression == null)
									LWithoutExpression = LColumnExpression;
								else
									LWithoutExpression = new BinaryExpression(LWithoutExpression, "iAnd", LColumnExpression);
							}
							
							if (LWithoutExpression != null)
							{
								if (LLeftSelectExpression.WhereClause == null)
									LLeftSelectExpression.WhereClause = new WhereClause(LWithoutExpression);
								else
									LLeftSelectExpression.WhereClause.Expression = new BinaryExpression(LLeftSelectExpression.WhereClause.Expression, "iAnd", LWithoutExpression);
							}
						}
						
						return LLeftStatement;
					}
					finally
					{
						LDevicePlan.PopJoinContext();
					}
				}
			}
			
			return new SelectExpression();
		}
    }
    
    public class SQLScalarSelector : SQLDeviceOperator
    {
		public SQLScalarSelector(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
    }
    
    public class SQLScalarReadAccessor : SQLDeviceOperator
    {
		public SQLScalarReadAccessor(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
    }
    
    public class SQLScalarWriteAccessor : SQLDeviceOperator
    {
		public SQLScalarWriteAccessor(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
		}
    }
    
    public class SQLScalarIsSpecialOperator : SQLDeviceOperator
    {
		public SQLScalarIsSpecialOperator(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			return new BinaryExpression(new ValueExpression(1, TokenType.Integer), "iEqual", new ValueExpression(0, TokenType.Integer));
		}
    }
    
    #if USEISTRING
    public class SQLIStringSelector : SQLDeviceOperator
    {
		public SQLIStringSelector() : base(){}
		public SQLIStringSelector(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		public SQLIStringSelector(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if (APlanNode.Nodes[0].IsLiteral)
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, new ValueNode(new Scalar(LDevicePlan.Plan.ServerProcess, LDevicePlan.Plan.Catalog.DataTypes.SystemIString, ((Scalar)APlanNode.Nodes[0].Execute(ADevicePlan.Plan.ServerProcess).Value).ToString())), false);
			else
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
    }
    
    public class SQLIStringWriteAccessor : SQLDeviceOperator
    {
		public SQLIStringWriteAccessor() : base(){}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if (APlanNode.Nodes[1].IsLiteral)
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, new ValueNode(new Scalar(LDevicePlan.Plan.ServerProcess, LDevicePlan.Plan.Catalog.DataTypes.SystemIString, ((Scalar)APlanNode.Nodes[1].Execute(ADevicePlan.Plan.ServerProcess).Value).ToString())), false);
			else
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
		}
    }
    #endif
    
    public class SQLVersionNumberIsUndefinedOperator : SQLDeviceOperator
    {
		public SQLVersionNumberIsUndefinedOperator(int AID, string AName) : base(AID, AName)  {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LVersionNumber = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
			return new BinaryExpression(LVersionNumber, "iEqual", new ValueExpression("****************************************", TokenType.String));
		}
    }
    
    public abstract class SQLOperator : SQLDeviceOperator
    {
		public SQLOperator(int AID, string AName) : base(AID, AName) {}
		
		public abstract string GetInstruction();
		public abstract bool GetIsBooleanContext();
    }
    
    public abstract class SQLUnaryOperator : SQLOperator
    {
		public SQLUnaryOperator(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return new UnaryExpression(GetInstruction(), LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext()));
		}
    }
    
    public abstract class SQLBinaryOperator : SQLOperator
    {
		public SQLBinaryOperator(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return new BinaryExpression
			(
				LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext()), 
				GetInstruction(), 
				LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], GetIsBooleanContext())
			);
		}
    }
    
    public class SQLCallOperator : SQLDeviceOperator
    {
		public SQLCallOperator(int AID, string AName) : base(AID, AName) {}
	
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression[] LExpressions = new Expression[APlanNode.Nodes.Count];
			for (int LIndex = 0; LIndex < LExpressions.Length; LIndex++)
			{
				if (IsParameterContextLiteral(LIndex) && !APlanNode.Nodes[LIndex].IsContextLiteral(0))
				{
					LDevicePlan.IsSupported = false;
					LDevicePlan.TranslationMessages.Add(new TranslationMessage(String.Format(@"Plan is not supported because argument ({0}) to operator ""{1}"" is not context literal", LIndex.ToString(), Operator.OperatorName), APlanNode));
					return null;
				}
				LExpressions[LIndex] = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[LIndex], false);
			}

			return new CallExpression(FOperatorName, LExpressions);
		}

		private string FOperatorName;		
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = value; }
		}
    }
    
    public class SQLUserOperator : SQLDeviceOperator
    {
		public SQLUserOperator(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression[] LExpressions = new Expression[APlanNode.Nodes.Count];
			for (int LIndex = 0; LIndex < LExpressions.Length; LIndex++)
			{
				if (IsParameterContextLiteral(LIndex) && !APlanNode.Nodes[LIndex].IsContextLiteral(0))
				{
					LDevicePlan.IsSupported = false;
					LDevicePlan.TranslationMessages.Add(new TranslationMessage(String.Format(@"Plan is not supported because argument ({0}) to operator ""{1}"" is not context literal", LIndex.ToString(), Operator.OperatorName), APlanNode));
					return null;
				}
				LExpressions[LIndex] = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[LIndex], false);
			}
				
			return new UserExpression(FTranslationString, LExpressions);
		}

		private string FTranslationString = String.Empty;
		public string TranslationString
		{
			get { return FTranslationString; }
			set { FTranslationString = value == null ? String.Empty : value; }
		}
    }
    
    public class SQLIntegerDivision : SQLDeviceOperator
    {
		public SQLIntegerDivision(int AID, string AName) : base(AID, AName) {}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new BinaryExpression
				(
					new CastExpression
					(
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
						((SQLScalarType)LDevicePlan.Device.ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType)).DomainName()
					),
					"iDivision", 
					new CastExpression
					(
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
						((SQLScalarType)LDevicePlan.Device.ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType)).DomainName()
					)
				);
		}
    }
    
    public class SQLDecimalDiv : SQLDeviceOperator
    {
		public SQLDecimalDiv(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new CastExpression
				(
					new BinaryExpression
					(
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false), 
						"iDivision", 
						LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
					),
					((SQLScalarType)ADevicePlan.Device.ResolveDeviceScalarType(ADevicePlan.Plan, (Schema.ScalarType)APlanNode.DataType)).DomainName()
				);
		}
    }
    
	// Comparison operators    
    public class SQLEqual : SQLBinaryOperator 
    { 
		public SQLEqual(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iEqual"; } 
		public override bool GetIsBooleanContext() { return false; }
	}

    public class SQLNotEqual : SQLBinaryOperator 
    { 
		public SQLNotEqual(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iNotEqual"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLLess : SQLBinaryOperator 
    { 
		public SQLLess(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iLess"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLInclusiveLess : SQLBinaryOperator 
    { 
		public SQLInclusiveLess(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iInclusiveLess"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLGreater : SQLBinaryOperator 
    { 
		public SQLGreater(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iGreater"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLInclusiveGreater : SQLBinaryOperator 
    { 
		public SQLInclusiveGreater(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iInclusiveGreater"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
	
    public class SQLLike : SQLBinaryOperator 
    { 
		public SQLLike(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iLike"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
	
    public class SQLMatches : SQLBinaryOperator 
    { 
		public SQLMatches(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iMatches"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
	
	// Left ?= Right ::= case when Left = Right then 0 when Left < Right then -1 else 1 end
	public class SQLCompare : SQLDeviceOperator
	{
		public SQLCompare(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							new BinaryExpression
							(
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
								"iEqual",
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
							), 
							new ValueExpression(0)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
								"iLess",
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
							),
							new ValueExpression(-1)
						)
					},
					new CaseElseExpression(new ValueExpression(1))
				);
		}
	}
	
	public class SQLBetween : SQLDeviceOperator
	{
		public SQLBetween(int AID, string AName) : base(AID, AName) {}

		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return 
				new BetweenExpression
				(
					LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
					LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
					LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[2], false)
				);
		}
	}
	
	// Conversions
	
	// ToString(AValue) ::= case when AValue = 0 then 'False' else 'True' end
	public class SQLBooleanToString : SQLDeviceOperator
	{
		public SQLBooleanToString(int AID, string AName) : base(AID, AName) {}
	
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							new BinaryExpression
							(
								LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
								"iEqual",
								new ValueExpression(0, TokenType.Integer)
							),
							new ValueExpression("False", TokenType.String)
						)
					},
					new CaseElseExpression(new ValueExpression("True", TokenType.String))
				);
		}	
	}
	
	// Null handling operators
	public class SQLIsNull : SQLUnaryOperator
	{
		public SQLIsNull(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iIsNull"; }
		public override bool GetIsBooleanContext() { return false; }

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if (APlanNode.Nodes[0].DataType is Schema.IScalarType)
			{
				if (APlanNode.Nodes[0].DataType.Is(ADevicePlan.Plan.ServerProcess.DataTypes.SystemBoolean))
				{
					LDevicePlan.IsSupported = false;
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because there is no SQL equivalent of IsNil() for a boolean-valued expression. Consider rewriting the D4 using a conditional expression.", APlanNode));
					return new SelectExpression();
				}
				return new UnaryExpression(GetInstruction(), LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext()));
			}
			else if (APlanNode.Nodes[0].DataType is Schema.IRowType)
				return new UnaryExpression("iNot", new UnaryExpression("iExists", LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext())));
			else
			{
				LDevicePlan.IsSupported = false;
				LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because invocation of IsNil for non-scalar- or row-valued expressions is not supported.", APlanNode));
				return new SelectExpression();
			}
		}
	}
	
	// Null handling operators
	public class SQLIsNotNull : SQLUnaryOperator
	{
		public SQLIsNotNull(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iIsNotNull"; }
		public override bool GetIsBooleanContext() { return false; }

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if (APlanNode.Nodes[0].DataType is Schema.IScalarType)
			{
				if (APlanNode.Nodes[0].DataType.Is(ADevicePlan.Plan.ServerProcess.DataTypes.SystemBoolean))
				{
					LDevicePlan.IsSupported = false;
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because there is no SQL equivalent of IsNotNil() for a boolean-valued expression. Consider rewriting the D4 using a conditional expression.", APlanNode));
					return new SelectExpression();
				}
				return new UnaryExpression(GetInstruction(), LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext()));
			}
			else if (APlanNode.Nodes[0].DataType is Schema.IRowType)
				return new UnaryExpression("iExists", LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext()));
			else
			{
				LDevicePlan.IsSupported = false;
				LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because invocation of IsNotNil for non-scalar- or row-valued expressions is not supported.", APlanNode));
				return new SelectExpression();
			}
		}
	}
	
	public class SQLIfNull : SQLBinaryOperator
	{
		public SQLIfNull(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iNullValue"; }
		public override bool GetIsBooleanContext() { return false; }

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if ((APlanNode.Nodes[0].DataType is Schema.IScalarType) && (APlanNode.Nodes[1].DataType is Schema.IScalarType))
			{
				if ((APlanNode.Nodes[0].DataType.Is(ADevicePlan.Plan.ServerProcess.DataTypes.SystemBoolean)))
				{
					LDevicePlan.IsSupported = false;
					LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because there is no SQL equivalent of IfNil() for a boolean-valued expression. Consider rewriting the D4 using a conditional expression.", APlanNode));
					return new SelectExpression();
				}
				
				return new BinaryExpression
				(
					LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext()), 
					GetInstruction(), 
					LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], GetIsBooleanContext())
				);
			}
			else
			{
				LDevicePlan.IsSupported = false;
				LDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because invocation of IfNil for non-scalar valued expressions is not supported.", APlanNode));
				return new SelectExpression();
			}
		}
	}
    
    // Logical operators
    public class SQLNot : SQLDeviceOperator
    { 
		public SQLNot(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			if (LDevicePlan.IsBooleanContext())
				return new UnaryExpression("iNot", LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], true));
			else
				return new CaseExpression(new CaseItemExpression[]{new CaseItemExpression(LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], true), new ValueExpression(0))}, new CaseElseExpression(new ValueExpression(1)));
		}
	}
    
    public class SQLAnd : SQLBinaryOperator 
    { 
		public SQLAnd(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iAnd"; } 
		public override bool GetIsBooleanContext() { return true; }
	}
    
    public class SQLOr : SQLBinaryOperator 
    { 
		public SQLOr(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iOr"; } 
		public override bool GetIsBooleanContext() { return true; }
	}
    
    public class SQLXor : SQLBinaryOperator 
    { 
		public SQLXor(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iXor"; } 
		public override bool GetIsBooleanContext() { return true; }

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LExpression1 = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], true);
			Expression LExpression2 = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], true);
			Expression LFirst = new BinaryExpression(LExpression1, "iOr", LExpression2);
			LExpression1 = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], true);
			LExpression2 = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], true);
			Expression LSecond = new BinaryExpression(LExpression1, "iAnd", LExpression2);
			Expression LThird = new UnaryExpression("iNot", LSecond);
			return new BinaryExpression(LFirst, "iAnd", LThird);
		}
	}
    
    // Arithmetic operators
    public class SQLNegate : SQLUnaryOperator 
    { 
		public SQLNegate(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iNegate"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
	public class SQLConcatenation : SQLBinaryOperator
	{
		public SQLConcatenation(int AID, string AName) : base(AID, AName) {}

		public override string GetInstruction() { return "iConcatenation"; }
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLAddition : SQLBinaryOperator 
    { 
		public SQLAddition(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iAddition"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLSubtraction : SQLBinaryOperator 
    { 
		public SQLSubtraction(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iSubtraction"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLMultiplication : SQLBinaryOperator 
    { 
		public SQLMultiplication(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iMultiplication"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLDivision : SQLBinaryOperator 
    { 
		public SQLDivision(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iDivision"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLMod : SQLBinaryOperator 
    { 
		public SQLMod(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iMod"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLPower : SQLBinaryOperator 
    { 
		public SQLPower(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iPower"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    // Bitwise operators
    public class SQLBitwiseNot : SQLUnaryOperator 
    { 
		public SQLBitwiseNot(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iBitwiseNot"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLBitwiseAnd : SQLBinaryOperator 
    { 
		public SQLBitwiseAnd(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iBitwiseAnd"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLBitwiseOr : SQLBinaryOperator 
    { 
		public SQLBitwiseOr(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iBitwiseOr"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLBitwiseXor : SQLBinaryOperator 
    { 
		public SQLBitwiseXor(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iBitwiseXor"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLLeftShift : SQLBinaryOperator 
    { 
		public SQLLeftShift(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iShiftLeft"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLRightShift : SQLBinaryOperator 
    { 
		public SQLRightShift(int AID, string AName) : base(AID, AName) {}
		
		public override string GetInstruction() { return "iShiftRight"; } 
		public override bool GetIsBooleanContext() { return false; }
	}

	// Existential
    public class SQLExists : SQLDeviceOperator
    { 
		public SQLExists(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			LDevicePlan.PushQueryContext(); // Push a query context to get us out of the scalar context
			try
			{
				Expression LExpression = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
				if (LDevicePlan.IsSupported)
				{
					SelectExpression LSelectExpression = LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode)APlanNode.Nodes[0]).TableVar, LExpression, false);
					LSelectExpression.SelectClause.Columns.Clear();
					LSelectExpression.SelectClause.NonProject = true;
					return new UnaryExpression("iExists", LSelectExpression);
				}
				return LExpression; // not supported so it doesn't matter what gets returned
			}
			finally
			{
				LDevicePlan.PopQueryContext();
			}
		}
	}
    
    public class SQLIn : SQLBinaryOperator 
    { 
		public SQLIn(int AID, string AName) : base(AID, AName) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			Expression LLeftExpression = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], GetIsBooleanContext());
			Expression LRightExpression = null;
			LDevicePlan.PushQueryContext();
			if (APlanNode.Nodes[1].DataType is Schema.ListType)
				LDevicePlan.CurrentQueryContext().IsListContext = true;
			try
			{
				LRightExpression = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], GetIsBooleanContext());
			}
			finally
			{
				LDevicePlan.PopQueryContext();
			}
			return new BinaryExpression
			(
				LLeftExpression, 
				GetInstruction(), 
				LRightExpression
			);
		}

		public override string GetInstruction() { return "iIn"; } 
		public override bool GetIsBooleanContext() { return false; }
	}

	public class SQLDoNothing : SQLDeviceOperator
	{
		public SQLDoNothing(int AID, string AName) : base(AID, AName) {}

		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
			SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
			return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
		}
	}
}

