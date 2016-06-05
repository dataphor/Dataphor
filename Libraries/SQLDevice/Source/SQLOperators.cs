/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USENAMEDROWVARIABLES

namespace Alphora.Dataphor.DAE.Device.SQL
{
	using System;
	using System.Collections;
	
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.SQL;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Device;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public abstract class SQLDeviceOperator : DeviceOperator
	{
		public SQLDeviceOperator(int iD, string name) : base(iD, name)
		{
			_isTruthValued = GetIsTruthValued();
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
		
		protected bool _isTruthValued = false;
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
			get { return _isTruthValued; }
			set { _isTruthValued = value; }
		}
		
		private bool[] _isParameterContextLiteral;
		/// <summary>
		/// Indicates whether or not the given operand is required to be context literal.
		/// </summary>
		public bool IsParameterContextLiteral(int parameterIndex)
		{
			if (_isParameterContextLiteral == null)
			{
				_isParameterContextLiteral = new bool[Operator.Operands.Count];
				ReadIsParameterContextLiteral();
			}
			
			return _isParameterContextLiteral[parameterIndex];
		}
		
		public void ReadIsParameterContextLiteral()
		{
			for (int index = 0; index < _isParameterContextLiteral.Length; index++)
				_isParameterContextLiteral[index] = false;
				
			if (_contextLiteralParameterIndexes != String.Empty)
				foreach (string stringIndex in _contextLiteralParameterIndexes.Split(';'))
				{
					int index = Convert.ToInt32(stringIndex);
					if ((index >= 0) && (index < Operator.Operands.Count))
						_isParameterContextLiteral[index] = true;
				}
		}

		private string _contextLiteralParameterIndexes = String.Empty;
		/// <summary>
		/// A semi-colon separated list of indexes indicating which operands of the operator are required to be context literal
		/// </summary>
		public string ContextLiteralParameterIndexes
		{
			get { return _contextLiteralParameterIndexes; }
			set 
			{ 
				if (_contextLiteralParameterIndexes != value)
				{
					_contextLiteralParameterIndexes = value == null ? String.Empty : value;
					if (_isParameterContextLiteral != null)
						ReadIsParameterContextLiteral();
				}
			}
		}
	}
	
    public class SQLRetrieve : SQLDeviceOperator
    {
		public SQLRetrieve(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			TableVar tableVar = ((TableVarNode)planNode).TableVar;

			if (tableVar is BaseTableVar)
			{
				SQLRangeVar rangeVar = new SQLRangeVar(localDevicePlan.GetNextTableAlias());
				localDevicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
				SelectExpression selectExpression = new SelectExpression();
				selectExpression.FromClause = new AlgebraicFromClause(new TableSpecifier(new TableExpression(D4.MetaData.GetTag(tableVar.MetaData, "Storage.Schema", localDevicePlan.Device.Schema), localDevicePlan.Device.ToSQLIdentifier(tableVar)), rangeVar.Name));
				selectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn column in tableVar.Columns)
				{
					SQLRangeVarColumn rangeVarColumn = new SQLRangeVarColumn(column, rangeVar.Name, localDevicePlan.Device.ToSQLIdentifier(column), localDevicePlan.Device.ToSQLIdentifier(column.Name));
					rangeVar.Columns.Add(rangeVarColumn);
					selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
				}

				selectExpression.SelectClause.Distinct = 
					(tableVar.Keys.Count == 1) && 
					Convert.ToBoolean(D4.MetaData.GetTag(tableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));
				
				return selectExpression;
			}
			else
				return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
    }
    
    public class SQLAdorn : SQLDeviceOperator
    {
		public SQLAdorn(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return statement;
		}
    }

    public class SQLRestrict : SQLDeviceOperator
    {
		public SQLRestrict(int iD, string name) : base(iD, name) {}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			if (localDevicePlan.IsSupported)
			{
				SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, statement, true);
				statement = selectExpression;

				localDevicePlan.PushScalarContext();
				try
				{
					localDevicePlan.CurrentQueryContext().IsWhereClause = true;

					localDevicePlan.Stack.Push(new Symbol(String.Empty, ((TableNode)planNode).DataType.RowType));
					try
					{
						localDevicePlan.CurrentQueryContext().ResetReferenceFlags();
						
						Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], true);
						
						FilterClause clause = null;
						if ((localDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasAggregateExpressions) != 0)
						{
							if (selectExpression.HavingClause == null)
								selectExpression.HavingClause = new HavingClause();
							clause = selectExpression.HavingClause;
							
							if (((localDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0) && !localDevicePlan.Device.SupportsSubSelectInHavingClause)
							{
								localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the having clause contains sub-select expressions and the device does not support sub-selects in the having clause.", planNode));
								localDevicePlan.IsSupported = false;
							}
						}
						else
						{
							if (selectExpression.WhereClause == null)
								selectExpression.WhereClause = new WhereClause();
							clause = selectExpression.WhereClause;
							
							if (((localDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0) && !localDevicePlan.Device.SupportsSubSelectInWhereClause)
							{
								localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because the where clause contains sub-select expressions and the device does not support sub-selects in the where clause.", planNode));
								localDevicePlan.IsSupported = false;
							}
						}
							
						if (clause.Expression == null)				
							clause.Expression = expression;
						else
							clause.Expression = new BinaryExpression(clause.Expression, "iAnd", expression);

						return statement;
					}
					finally
					{
						localDevicePlan.Stack.Pop();
					}
				}
				finally
				{
					localDevicePlan.PopScalarContext();
				}
			}
			
			return statement;
		}
    }

    public class SQLProject : SQLDeviceOperator
    {
		public SQLProject(int iD, string name) : base(iD, name) {}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			
			// Project where distinct is required is only supported if the device supports iEqual or iCompare for the data types of each column involved in the projection
			ProjectNodeBase projectNode = (ProjectNodeBase)planNode;
			
			if (projectNode.DistinctRequired)
				foreach (Schema.TableVarColumn column in projectNode.TableVar.Columns)
					if (!localDevicePlan.Device.SupportsEqual(devicePlan.Plan, column.Column.DataType))
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support equality comparison for values of type ""{0}"" which is the type of column ""{1}"" included in the projection.", column.Column.DataType.Name, column.Column.Name), planNode));
						localDevicePlan.IsSupported = false;
						break;
					}
			
			if (localDevicePlan.IsSupported)
			{
				Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
				if (localDevicePlan.IsSupported)
				{
					SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, statement, false);
					statement = selectExpression;

					if (projectNode.DistinctRequired)
						selectExpression.SelectClause.Distinct = true;
						
					selectExpression.SelectClause.Columns.Clear();
					foreach (TableVarColumn column in ((TableNode)planNode).TableVar.Columns)
						selectExpression.SelectClause.Columns.Add(localDevicePlan.GetRangeVarColumn(column.Name, true).GetColumnExpression());
						
					localDevicePlan.CurrentQueryContext().ProjectColumns(((TableNode)planNode).TableVar.Columns);
					
					if (selectExpression.SelectClause.Columns.Count == 0)
						selectExpression.SelectClause.Columns.Add(new ColumnExpression(new ValueExpression(1), "dummy"));
						
					return statement;
				}
			}

			return new SelectExpression();
		}
    }

	//	select customer as c ::=    
    //		select c_id as c_id, c_name as c_name from (select id as c_id, name as c_name from customer) as T1
    public class SQLRename : SQLDeviceOperator
    {
		public SQLRename(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{									
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			if (localDevicePlan.IsSupported)
			{
				SelectExpression selectExpression = 
						planNode is RowRenameNode ? 
							localDevicePlan.Device.FindSelectExpression(statement) : 
							localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, statement, false);
				statement = selectExpression;
				SQLRangeVarColumn rangeVarColumn;
				selectExpression.SelectClause.Columns.Clear();
				
				var renamedColumns = new SQLRangeVarColumns();

				if (planNode is RowRenameNode)
				{
					Schema.IRowType sourceRowType = planNode.Nodes[0].DataType as Schema.IRowType;
					Schema.IRowType targetRowType = planNode.DataType as Schema.IRowType;
					for (int index = 0; index < sourceRowType.Columns.Count; index++)
					{
						rangeVarColumn = localDevicePlan.CurrentQueryContext().RenameColumn(localDevicePlan, new Schema.TableVarColumn(sourceRowType.Columns[index]), new Schema.TableVarColumn(targetRowType.Columns[index]));
						selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
						renamedColumns.Add(rangeVarColumn);
					}
				} 
				else if (planNode is RenameNode)
				{
					TableVar sourceTableVar = ((RenameNode)planNode).SourceTableVar;
					TableVar tableVar = ((TableNode)planNode).TableVar;
					for (int index = 0; index < tableVar.Columns.Count; index++)
					{
						rangeVarColumn = localDevicePlan.CurrentQueryContext().RenameColumn(localDevicePlan, sourceTableVar.Columns[index], tableVar.Columns[index]);
						selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
						renamedColumns.Add(rangeVarColumn);
					}
				}

				localDevicePlan.CurrentQueryContext().AddedColumns.AddRange(renamedColumns);
			}

			return statement;
		}
    }

	/*
		select A add { <expression> X } ::=
			select <column list>, X from (select <column list>, <expression> X from A)
	*/    
    public class SQLExtend : SQLDeviceOperator
    {
		public SQLExtend(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			ExtendNode extendNode = (ExtendNode)planNode;
			TableVar tableVar = extendNode.TableVar;
			Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			if (localDevicePlan.IsSupported)
			{
				SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, statement, true);
				statement = selectExpression;

				localDevicePlan.Stack.Push(new Symbol(String.Empty, extendNode.DataType.RowType));
				try
				{
					localDevicePlan.CurrentQueryContext().IsExtension = true;
					localDevicePlan.PushScalarContext();
					try
					{
						localDevicePlan.CurrentQueryContext().IsSelectClause = true;
						int extendColumnIndex = 1;
						for (int index = extendNode.ExtendColumnOffset; index < extendNode.DataType.Columns.Count; index++)
						{
							if (!(extendNode.Nodes[extendColumnIndex].DataType is Schema.ScalarType))
							{
								localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(String.Format(@"Plan is not supported because the device does not support non-scalar-valued attributes for column ""{0}"".", extendNode.DataType.Columns[index].Name), planNode));
								localDevicePlan.IsSupported = false;
								break;
							}

							localDevicePlan.CurrentQueryContext().ResetReferenceFlags();
							SQLRangeVarColumn rangeVarColumn = 
								new SQLRangeVarColumn
								(
									tableVar.Columns[index], 
									localDevicePlan.Device.TranslateExpression(localDevicePlan, extendNode.Nodes[extendColumnIndex], false), 
									localDevicePlan.Device.ToSQLIdentifier(tableVar.Columns[index])
								);
							rangeVarColumn.ReferenceFlags = localDevicePlan.CurrentQueryContext().ReferenceFlags;
							localDevicePlan.CurrentQueryContext().ParentContext.AddedColumns.Add(rangeVarColumn);
							localDevicePlan.CurrentQueryContext().ParentContext.ReferenceFlags |= rangeVarColumn.ReferenceFlags;
							selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
							extendColumnIndex++;
						}
					}
					finally
					{
						localDevicePlan.PopScalarContext();
					}
				}
				finally
				{
					localDevicePlan.Stack.Pop();
				}
			}
				
			return statement;
		}
    }

	/*
		select A redefine { ID := <expression> } ::=
			select <column list>, ID from (select <column list>, <expression> as ID from A);
	*/    
    public class SQLRedefine : SQLDeviceOperator
    {
		public SQLRedefine(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			RedefineNode redefineNode = (RedefineNode)planNode;
			TableVar tableVar = redefineNode.TableVar;
			Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			if (localDevicePlan.IsSupported)
			{
				SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, statement, true);
				statement = selectExpression;

				localDevicePlan.Stack.Push(new Symbol(String.Empty, redefineNode.DataType.RowType));
				try
				{
					localDevicePlan.CurrentQueryContext().IsExtension = true;
					localDevicePlan.PushScalarContext();
					try
					{
						localDevicePlan.CurrentQueryContext().IsSelectClause = true;
						for (int columnIndex = 0; columnIndex < tableVar.Columns.Count; columnIndex++)
						{
							int redefineIndex = ((IList)redefineNode.RedefineColumnOffsets).IndexOf(columnIndex);
							if (redefineIndex >= 0)
							{
								SQLRangeVarColumn rangeVarColumn = localDevicePlan.GetRangeVarColumn(tableVar.Columns[columnIndex].Name, true);
								localDevicePlan.CurrentQueryContext().ResetReferenceFlags();
								rangeVarColumn.Expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, redefineNode.Nodes[redefineIndex + 1], false);
								rangeVarColumn.ReferenceFlags = localDevicePlan.CurrentQueryContext().ReferenceFlags;
								localDevicePlan.CurrentQueryContext().ParentContext.ReferenceFlags |= rangeVarColumn.ReferenceFlags;
								selectExpression.SelectClause.Columns[columnIndex].Expression = rangeVarColumn.Expression;
							}
						}
					}
					finally
					{
						localDevicePlan.PopScalarContext();
					}
				}
				finally
				{
					localDevicePlan.Stack.Pop();
				}
			}

			return statement;
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
		public SQLAggregate(int iD, string name) : base(iD, name) {}

		protected AggregateCallExpression FindAggregateCallExpression(Expression expression)
		{
			AggregateCallExpression callExpression = expression as AggregateCallExpression;
			if (callExpression == null)
			{
				CaseExpression caseExpression = expression as CaseExpression;
				if (caseExpression != null)
					callExpression = ((BinaryExpression)caseExpression.CaseItems[0].WhenExpression).LeftExpression as AggregateCallExpression;
			}
			return callExpression;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			AggregateNode aggregateNode = (AggregateNode)planNode;
			TableVar tableVar = aggregateNode.TableVar;
			Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, aggregateNode.OriginalSourceNode, false);
			if (localDevicePlan.IsSupported)
			{
				SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)aggregateNode.OriginalSourceNode).TableVar, statement, false); // true);
				statement = selectExpression;

				string nestingReason = String.Empty;
				bool nest = localDevicePlan.CurrentQueryContext().IsAggregate || selectExpression.SelectClause.Distinct || ((localDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0);
				if (nest)
					if (localDevicePlan.CurrentQueryContext().IsAggregate)
						nestingReason = "The argument to the aggregate operator must be nested because it contains aggregation.";
					else if (selectExpression.SelectClause.Distinct)
						nestingReason = "The argument to the aggregate operator must be nested because it contains a distinct specification.";
					else
						nestingReason = "The argument to the aggregate operator must be nested because it contains sub-select expressions.";
				else
				{
					// If the group by columns are not literals in SQL, we must nest
					for (int index = 0; index < aggregateNode.AggregateColumnOffset; index++)
						if ((localDevicePlan.GetRangeVarColumn(tableVar.Columns[index].Name, true).ReferenceFlags & SQLReferenceFlags.HasParameters) != 0)
						{
							nest = true;
							break;
						}
						
					if (nest)
						nestingReason = "The argument to the aggregate operator must be nested because it contains expressions which reference parameters.";
				}

				if (nest)
				{
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(nestingReason, planNode));
					statement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, ((TableNode)aggregateNode.OriginalSourceNode).TableVar, statement);
					selectExpression = localDevicePlan.Device.FindSelectExpression(statement);				
				}

				localDevicePlan.CurrentQueryContext().IsAggregate = true;			
				selectExpression.SelectClause = new SelectClause();
				var groupByColumns = new SQLRangeVarColumns();
				for (int index = 0; index < aggregateNode.AggregateColumnOffset; index++)
				{
					SQLRangeVarColumn groupByColumn = localDevicePlan.GetRangeVarColumn(tableVar.Columns[index].Name, true);
					groupByColumns.Add(groupByColumn);
					selectExpression.SelectClause.Columns.Add(groupByColumn.GetColumnExpression());
					if (selectExpression.GroupClause == null)
						selectExpression.GroupClause = new GroupClause();
					selectExpression.GroupClause.Columns.Add(groupByColumn.GetExpression());
				}

				var addedColumns = new SQLRangeVarColumns();

				// Preserve added columns that appear in the group by clause
				foreach (var addedColumn in localDevicePlan.CurrentQueryContext().AddedColumns)
				{
					var groupByColumnIndex = groupByColumns.IndexOf(addedColumn.TableVarColumn.Name);
					if (groupByColumnIndex >= 0)
					{
						addedColumns.Add(addedColumn);
					}
				}

				for (int index = aggregateNode.AggregateColumnOffset; index < aggregateNode.DataType.Columns.Count; index++)
				{
					localDevicePlan.CurrentQueryContext().ResetReferenceFlags();
					Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, aggregateNode.Nodes[(index - aggregateNode.AggregateColumnOffset) + 1], false);
					if (localDevicePlan.IsSupported)
					{
						FindAggregateCallExpression(expression).IsDistinct = aggregateNode.ComputeColumns[index - aggregateNode.AggregateColumnOffset].Distinct;
						SQLRangeVarColumn rangeVarColumn = 
							new SQLRangeVarColumn
							(
								tableVar.Columns[index],
								expression,
								localDevicePlan.Device.ToSQLIdentifier(tableVar.Columns[index])
							);
						rangeVarColumn.ReferenceFlags = localDevicePlan.CurrentQueryContext().ReferenceFlags | SQLReferenceFlags.HasAggregateExpressions;
						addedColumns.Add(rangeVarColumn);
						selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
					}
				}

				// Replace added columns (except those in the group by) because they can no longer be referenced within this query context
				localDevicePlan.CurrentQueryContext().AddedColumns.Clear();
				localDevicePlan.CurrentQueryContext().AddedColumns.AddRange(addedColumns);
			}
			
			return statement;
		}
    }
    
    public class SQLAggregateOperator : SQLDeviceOperator
    {
		public SQLAggregateOperator(int iD, string name) : base(iD, name) {}
		
		private string _operatorName;
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value; }
		}

		protected virtual AggregateCallExpression CreateAggregateCallExpression(SQLDevicePlan devicePlan, PlanNode planNode)
		{
			AggregateCallExpression expression = new AggregateCallExpression();
			expression.Identifier = _operatorName;
			return expression;
		}

		protected virtual AggregateCallExpression TranslateOrderDependentAggregateCallExpression(SQLDevicePlan devicePlan, PlanNode planNode, AggregateCallExpression expression)
		{
			return expression;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			// If this is a scalar invocation, it must be translated as a subselect
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			AggregateCallNode node = (AggregateCallNode)planNode;
			TableVar sourceTableVar = ((TableNode)node.Nodes[0]).TableVar;
			AggregateCallExpression expression = CreateAggregateCallExpression(localDevicePlan, planNode);
			
			if (!localDevicePlan.CurrentQueryContext().IsScalarContext)
			{
				if (node.AggregateColumnIndexes.Length > 0)
					for (int index = 0; index < node.AggregateColumnIndexes.Length; index++)
					{
						SQLRangeVarColumn rangeVarColumn = localDevicePlan.FindRangeVarColumn(sourceTableVar.Columns[node.AggregateColumnIndexes[index]].Name, true);
						if (rangeVarColumn == null)
							expression.Expressions.Add(new QualifiedFieldExpression("*")); // If we don't find the column, we are being evaluated out of context, and must return true in order to prevent the overall aggregate from being incorrectly unsupported
						else
							expression.Expressions.Add(rangeVarColumn.GetExpression());
					}
				else	
					expression.Expressions.Add(new QualifiedFieldExpression("*")); 

				expression = TranslateOrderDependentAggregateCallExpression(localDevicePlan, planNode, expression);
					
				return expression;
			}
			else
			{
				localDevicePlan.CurrentQueryContext().ReferenceFlags |= SQLReferenceFlags.HasSubSelectExpressions;
				bool isSupported = localDevicePlan.IsSubSelectSupported();
				if (!isSupported)
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(localDevicePlan.GetSubSelectNotSupportedReason(), planNode));
				localDevicePlan.IsSupported = localDevicePlan.IsSupported && isSupported;
				localDevicePlan.Stack.Push(new Symbol(D4.Keywords.Result, node.DataType));
				try
				{
					for (int index = 0; index < node.Operator.Initialization.StackDisplacement; index++)
						localDevicePlan.Stack.Push(new Symbol(String.Empty, devicePlan.Plan.Catalog.DataTypes.SystemScalar));
						
					for (int index = 0; index < node.AggregateColumnIndexes.Length; index++)
						localDevicePlan.Stack.Push(new Symbol(node.ValueNames[index], sourceTableVar.Columns[node.AggregateColumnIndexes[index]].DataType));
					try
					{
						localDevicePlan.PushQueryContext();
						try
						{
							Statement statement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
							if (localDevicePlan.IsSupported)
							{
								SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, statement, false);
								statement = selectExpression;
									
								string nestingReason = String.Empty;
								bool nest = 
									localDevicePlan.CurrentQueryContext().IsAggregate 
										|| selectExpression.SelectClause.Distinct 
										|| ((localDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasSubSelectExpressions) != 0);

								if (nest)
								{
									if (localDevicePlan.CurrentQueryContext().IsAggregate)
										nestingReason = "The argument to the aggregate operator must be nested because it contains aggregation.";
									else if ((localDevicePlan.CurrentQueryContext().ReferenceFlags & SQLReferenceFlags.HasAggregateExpressions) != 0)
										nestingReason = "The argument to the aggregate operator must be nested because it contains subselect expressions.";
									else
										nestingReason = "The argument to the aggregate operator must be nested because it contains a distinct specification.";

									localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(nestingReason, planNode));
									statement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, sourceTableVar, statement);
									selectExpression = localDevicePlan.Device.FindSelectExpression(statement);				
								}
									
								if (node.AggregateColumnIndexes.Length > 0)
									for (int index = 0; index < node.AggregateColumnIndexes.Length; index++)
										expression.Expressions.Add(localDevicePlan.CurrentQueryContext().GetRangeVarColumn(sourceTableVar.Columns[node.AggregateColumnIndexes[index]].Name).GetExpression());
								else	
									expression.Expressions.Add(new QualifiedFieldExpression("*"));

								expression = TranslateOrderDependentAggregateCallExpression(localDevicePlan, planNode, expression);

								selectExpression.SelectClause = new SelectClause();
								selectExpression.SelectClause.Columns.Add(new ColumnExpression(expression, "dummy1"));
							}

							return statement;
						}
						finally
						{
							localDevicePlan.PopQueryContext();
						}
					}
					finally
					{
						for (int index = 0; index < node.AggregateColumnIndexes.Length; index++)
							localDevicePlan.Stack.Pop();

						for (int index = 0; index < node.Operator.Initialization.StackDisplacement; index++)
							localDevicePlan.Stack.Pop();
					}
				}
				finally
				{
					localDevicePlan.Stack.Pop();
				}
			}
		}
    }
    
    public class SQLOrder : SQLDeviceOperator
    {
		public SQLOrder(int iD, string name) : base(iD, name) {}
		
		// Order is ignored until the very last order in the expression, which is translated in the device
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			return devicePlan.Device.Translate(devicePlan, planNode.Nodes[0]);
		}
    }

    public class SQLUnion : SQLDeviceOperator
    {
		public SQLUnion(int iD, string name) : base(iD, name) {}
		
		public static void NormalizeSelectClause(DevicePlan devicePlan, ColumnExpressions normalColumns, ColumnExpressions nonNormalColumns)
		{
			ColumnExpressions localNonNormalColumns = new ColumnExpressions();
			localNonNormalColumns.AddRange(nonNormalColumns);
			nonNormalColumns.Clear();
			foreach (ColumnExpression columnExpression in normalColumns)
				nonNormalColumns.Add(localNonNormalColumns[columnExpression.ColumnAlias]);
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			localDevicePlan.PushQueryContext();
			Statement leftStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			localDevicePlan.PopQueryContext();
			localDevicePlan.PushQueryContext();
			Statement rightStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			localDevicePlan.PopQueryContext();
			
			if (localDevicePlan.IsSupported)
			{
				SQLRangeVar rangeVar = new SQLRangeVar(localDevicePlan.GetNextTableAlias());
				localDevicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
				Schema.TableVar tableVar = ((TableNode)planNode).TableVar;
				foreach (Schema.TableVarColumn column in tableVar.Columns)
					rangeVar.Columns.Add(new SQLRangeVarColumn(column, rangeVar.Name, localDevicePlan.Device.ToSQLIdentifier(column.Name)));
				
				if (leftStatement is QueryExpression)
				{
					QueryExpression leftQueryExpression = (QueryExpression)leftStatement;
					if (!leftQueryExpression.IsCompatibleWith(TableOperator.Union))
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The left argument to the union operator must be nested because it contains non-union table operations.", planNode));
						leftStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, ((TableNode)planNode.Nodes[1]).TableVar, leftStatement);
					}
				}
				
				if (!(leftStatement is QueryExpression))
				{
					QueryExpression queryExpression = new QueryExpression();
					queryExpression.SelectExpression = (SelectExpression)leftStatement;
					leftStatement = queryExpression;
				}
				
				ColumnExpressions normalColumns = localDevicePlan.Device.FindSelectExpression(leftStatement).SelectClause.Columns;
				
				if (rightStatement is QueryExpression)
				{
					QueryExpression rightQueryExpression = (QueryExpression)rightStatement;
					if (!rightQueryExpression.IsCompatibleWith(TableOperator.Union))
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The right argument to the union operator must be nested because it contains non-union table operations.", planNode));
						rightStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, ((TableNode)planNode.Nodes[1]).TableVar, rightStatement);
					}
					else
						foreach (TableOperatorExpression tableOperatorExpression in rightQueryExpression.TableOperators)
							NormalizeSelectClause(devicePlan, normalColumns, tableOperatorExpression.SelectExpression.SelectClause.Columns);
				}
				
				NormalizeSelectClause(devicePlan, normalColumns, localDevicePlan.Device.FindSelectExpression(rightStatement).SelectClause.Columns);
				
				if (rightStatement is QueryExpression)
					rightStatement = ((QueryExpression)rightStatement).SelectExpression;
					
				((QueryExpression)leftStatement).TableOperators.Add(new TableOperatorExpression(TableOperator.Union, true, (SelectExpression)rightStatement));
			}
			
			return leftStatement;
		}
    }
    
    public class SQLDifference : SQLDeviceOperator
    {
		public SQLDifference(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			localDevicePlan.PushQueryContext();
			Statement leftStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			localDevicePlan.PopQueryContext();
			localDevicePlan.PushQueryContext();
			Statement rightStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			localDevicePlan.PopQueryContext();
			
			if (localDevicePlan.IsSupported)
			{
				SQLRangeVar rangeVar = new SQLRangeVar(localDevicePlan.GetNextTableAlias());
				localDevicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
				Schema.TableVar tableVar = ((TableNode)planNode).TableVar;
				foreach (Schema.TableVarColumn column in tableVar.Columns)
					rangeVar.Columns.Add(new SQLRangeVarColumn(column, rangeVar.Name, localDevicePlan.Device.ToSQLIdentifier(column.Name)));
				
				if (leftStatement is QueryExpression)
				{
					QueryExpression leftQueryExpression = (QueryExpression)leftStatement;
					if (!leftQueryExpression.IsCompatibleWith(TableOperator.Difference))
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The left argument to the difference operator must be nested because it contains non-difference table operations.", planNode));
						leftStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, ((TableNode)planNode.Nodes[1]).TableVar, leftStatement);
					}
				}
				
				if (!(leftStatement is QueryExpression))
				{
					QueryExpression queryExpression = new QueryExpression();
					queryExpression.SelectExpression = (SelectExpression)leftStatement;
					leftStatement = queryExpression;
				}
				
				ColumnExpressions normalColumns = localDevicePlan.Device.FindSelectExpression(leftStatement).SelectClause.Columns;
				
				if (rightStatement is QueryExpression)
				{
					QueryExpression rightQueryExpression = (QueryExpression)rightStatement;
					if (!rightQueryExpression.IsCompatibleWith(TableOperator.Union))
					{
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("The right argument to the difference operator must be nested because it contains non-difference table operations.", planNode));
						rightStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, ((TableNode)planNode.Nodes[1]).TableVar, rightStatement);
					}
					else
						foreach (TableOperatorExpression tableOperatorExpression in rightQueryExpression.TableOperators)
							SQLUnion.NormalizeSelectClause(devicePlan, normalColumns, tableOperatorExpression.SelectExpression.SelectClause.Columns);
				}
				
				SQLUnion.NormalizeSelectClause(devicePlan, normalColumns, localDevicePlan.Device.FindSelectExpression(rightStatement).SelectClause.Columns);
				
				if (rightStatement is QueryExpression)
					rightStatement = ((QueryExpression)rightStatement).SelectExpression;
					
				((QueryExpression)leftStatement).TableOperators.Add(new TableOperatorExpression(TableOperator.Difference, true, (SelectExpression)rightStatement));
			}
			
			return leftStatement;
		}
    }
    
    public class SQLJoin : SQLDeviceOperator
    {
		public SQLJoin(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;

			// Translate left operand
			localDevicePlan.PushQueryContext();
			Statement leftStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			if (localDevicePlan.IsSupported)
			{
				SelectExpression leftSelectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, leftStatement, false);
				leftStatement = leftSelectExpression;
				TableVar leftTableVar = ((TableNode)planNode.Nodes[0]).TableVar;

				ConditionedTableNode conditionedTableNode = (ConditionedTableNode)planNode;

				// if any column in the left join key is a computed or renamed column in the current query context, the left argument must be nested
				bool isLeftKeyColumnComputed = false;
				foreach (TableVarColumn leftKeyColumn in conditionedTableNode.LeftKey.Columns)
				{
					var leftKeyRangeVarColumn = localDevicePlan.GetRangeVarColumn(leftKeyColumn.Name, true);
					if (leftKeyRangeVarColumn.Expression != null || (leftKeyRangeVarColumn.Alias != null && leftKeyRangeVarColumn.Alias != leftKeyRangeVarColumn.ColumnName))
					{
						isLeftKeyColumnComputed = true;
						break;
					}
				}

				if 
				(
					localDevicePlan.CurrentQueryContext().IsAggregate || 
					leftSelectExpression.SelectClause.Distinct || 
					isLeftKeyColumnComputed ||
					(
						(planNode is RightOuterJoinNode) && 
						(
							localDevicePlan.CurrentQueryContext().IsExtension ||
							(((RightOuterJoinNode)planNode).IsNatural && (localDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)) ||
							(leftSelectExpression.WhereClause != null) || 
							leftSelectExpression.FromClause.HasJoins()
						)
					)
				)
				{
					string nestingReason = "The left argument to the join operator must be nested because ";
					if (isLeftKeyColumnComputed)
						nestingReason += "the join condition columns in the left argument are computed or renamed.";
					else if (planNode is RightOuterJoinNode)
					{
						if (leftSelectExpression.WhereClause != null)
							nestingReason += "the join is right outer and the left argument has a where clause.";
						else if (localDevicePlan.CurrentQueryContext().IsExtension)
							nestingReason += "the join is right outer and the left argument has computed columns.";
						else if (localDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)
							nestingReason += "the join is a natural right outer and the left argument has renamed columns.";
						else if (leftSelectExpression.FromClause.HasJoins())
							nestingReason += "the join is right outer and the left argument contains at least one join.";
					}
					else if (localDevicePlan.CurrentQueryContext().IsAggregate)
						nestingReason += "it contains aggregation.";
					else
						nestingReason += "it contains a distinct specification.";
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(nestingReason, planNode));
					leftStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, leftTableVar, leftStatement);
					leftSelectExpression = localDevicePlan.Device.FindSelectExpression(leftStatement);
				}
				SQLQueryContext leftContext = localDevicePlan.CurrentQueryContext();
				localDevicePlan.PopQueryContext();
				
				// Translate right operand
				localDevicePlan.PushQueryContext();
				Statement rightStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
				if (localDevicePlan.IsSupported)
				{
					SelectExpression rightSelectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[1]).TableVar, rightStatement, false);
					rightStatement = rightSelectExpression;
					TableVar rightTableVar = ((TableNode)planNode.Nodes[1]).TableVar;
					
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

					// if any column in the right join key is a computed or renamed column in the current query context, the right argument must be nested
					bool isRightKeyColumnComputed = false;
					foreach (TableVarColumn rightKeyColumn in conditionedTableNode.RightKey.Columns)
					{
						var rightKeyRangeVarColumn = localDevicePlan.GetRangeVarColumn(rightKeyColumn.Name, true);
						if (rightKeyRangeVarColumn.Expression != null || (rightKeyRangeVarColumn.Alias != null && rightKeyRangeVarColumn.Alias != rightKeyRangeVarColumn.ColumnName))
						{
							isRightKeyColumnComputed = true;
							break;
						}
					}
					
					bool isRightDeep = (!(planNode is OuterJoinNode) && !(planNode is WithoutNode) && !leftSelectExpression.FromClause.HasJoins() && rightSelectExpression.FromClause.HasJoins());
					bool isBushy = (!(planNode is OuterJoinNode) && leftSelectExpression.FromClause.HasJoins() && rightSelectExpression.FromClause.HasJoins());
					if 
					(
						localDevicePlan.CurrentQueryContext().IsAggregate || 
						rightSelectExpression.SelectClause.Distinct || 
						isRightKeyColumnComputed ||
						(
							((planNode is LeftOuterJoinNode) || (planNode is WithoutNode)) && 
							(
								localDevicePlan.CurrentQueryContext().IsExtension ||
								(((ConditionedTableNode)planNode).IsNatural && (localDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)) ||
								(rightSelectExpression.WhereClause != null) || 
								rightSelectExpression.FromClause.HasJoins()
							)
						) || 
						isBushy
					)
					{
						string nestingReason = "The right argument to the join operator must be nested because ";
						if (isRightKeyColumnComputed)
							nestingReason += "the join condition columns in the right argument are computed or renamed.";
						else if ((planNode is LeftOuterJoinNode) || (planNode is WithoutNode))
						{
							if (rightSelectExpression.WhereClause != null)
								nestingReason += "the join is left outer and the right argument has a where clause.";
							else if (localDevicePlan.CurrentQueryContext().IsExtension)
								nestingReason += "the join is left outer and the right argument has computed columns.";
							else if (localDevicePlan.CurrentQueryContext().AddedColumns.Count > 0)
								nestingReason += "the join is a natural left outer and the right argument has renamed columns.";
							else if (rightSelectExpression.FromClause.HasJoins())
								nestingReason += "the join is left outer and the right argument has at least one join.";
						}
						else if (localDevicePlan.CurrentQueryContext().IsAggregate)
							nestingReason += "it contains aggregation.";
						else if (isBushy)
							nestingReason += "both the left and right join arguments contain joins.";
						else
							nestingReason += "it contains a distinct specification.";
						localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage(nestingReason, planNode));
						rightStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, rightTableVar, rightStatement);
						rightSelectExpression = localDevicePlan.Device.FindSelectExpression(rightStatement);
					}
					SQLQueryContext rightContext = localDevicePlan.CurrentQueryContext();
					localDevicePlan.PopQueryContext();
					
					// Merge the query contexts
					localDevicePlan.CurrentQueryContext().RangeVars.AddRange(leftContext.RangeVars);
					localDevicePlan.CurrentQueryContext().AddedColumns.AddRange(leftContext.AddedColumns);
					if ((planNode is WithoutNode) || (planNode is HavingNode))
					{
						// If this is a Without or Having then the non-join columns of the right argument are not relevant and should not be available in the query context
						foreach (SQLRangeVar rangeVar in rightContext.RangeVars)
						{
							SQLRangeVar newRangeVar = new SQLRangeVar(rangeVar.Name);
							foreach (SQLRangeVarColumn rangeVarColumn in rangeVar.Columns)
								if (conditionedTableNode.RightKey.Columns.ContainsName(rangeVarColumn.TableVarColumn.Name))
									newRangeVar.Columns.Add(rangeVarColumn);
							localDevicePlan.CurrentQueryContext().RangeVars.Add(newRangeVar);
						}
						
						foreach (SQLRangeVarColumn rangeVarColumn in rightContext.AddedColumns)
							if (conditionedTableNode.RightKey.Columns.ContainsName(rangeVarColumn.TableVarColumn.Name))
								localDevicePlan.CurrentQueryContext().AddedColumns.Add(rangeVarColumn);
					}
					else
					{
						localDevicePlan.CurrentQueryContext().RangeVars.AddRange(rightContext.RangeVars);
						localDevicePlan.CurrentQueryContext().AddedColumns.AddRange(rightContext.AddedColumns);
					}
					
					JoinClause joinClause = new JoinClause();
					if (isRightDeep)
					{
						joinClause.FromClause = (AlgebraicFromClause)leftSelectExpression.FromClause;
						leftSelectExpression.FromClause = rightSelectExpression.FromClause;
					}
					else
						joinClause.FromClause = (AlgebraicFromClause)rightSelectExpression.FromClause;

					localDevicePlan.PushJoinContext(new SQLJoinContext(leftContext, rightContext));
					try
					{
						LeftOuterJoinNode leftOuterJoinNode = planNode as LeftOuterJoinNode;
						RightOuterJoinNode rightOuterJoinNode = planNode as RightOuterJoinNode;
						SemiTableNode semiTableNode = planNode as SemiTableNode;
						HavingNode havingNode = planNode as HavingNode;
						WithoutNode withoutNode = planNode as WithoutNode;

						if (leftOuterJoinNode != null)
							joinClause.JoinType = JoinType.Left;
						else if (rightOuterJoinNode != null)
							joinClause.JoinType = JoinType.Right;
						else
						{
							if (withoutNode != null)
								joinClause.JoinType = JoinType.Left;
							else
								joinClause.JoinType = JoinType.Inner;
						}

						#if USENAMEDROWVARIABLES
						localDevicePlan.Stack.Push(new Symbol(Keywords.Left, ((TableNode)planNode.Nodes[0]).DataType.RowType));
						#else
						localDevicePlan.Stack.Push(new Symbol(String.Empty, new Schema.RowType(((TableNode)APlanNode.Nodes[0]).DataType.Columns, Keywords.Left)));
						#endif
						try
						{
							#if USENAMEDROWVARIABLES
							localDevicePlan.Stack.Push(new Symbol(Keywords.Right, ((TableNode)planNode.Nodes[1]).DataType.RowType));
							#else
							localDevicePlan.Stack.Push(new Symbol(String.Empty, new Schema.RowType(((TableNode)APlanNode.Nodes[1]).DataType.Columns, Keywords.Right)));
							#endif
							try
							{
								joinClause.JoinExpression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], true);
							}
							finally
							{
								localDevicePlan.Stack.Pop();
							}
						}
						finally
						{
							localDevicePlan.Stack.Pop();
						}
							
						// Translate rowexists column
						if ((planNode is OuterJoinNode) && (((OuterJoinNode)planNode).RowExistsColumnIndex >= 0))
						{
							OuterJoinNode outerJoinNode = (OuterJoinNode)planNode;
							TableVarColumn rowExistsColumn = outerJoinNode.TableVar.Columns[outerJoinNode.RowExistsColumnIndex];
							Expression rowExistsExpression = null;
							if (outerJoinNode.LeftKey.Columns.Count == 0)
								rowExistsExpression = new ValueExpression(1);
							else
							{
								CaseExpression caseExpression = new CaseExpression();
								CaseItemExpression caseItem = new CaseItemExpression();
								if (leftOuterJoinNode != null)
									caseItem.WhenExpression = new UnaryExpression("iIsNull", localDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(outerJoinNode.RightKey.Columns[0].Name).GetExpression());
								else
									caseItem.WhenExpression = new UnaryExpression("iIsNull", localDevicePlan.CurrentJoinContext().LeftQueryContext.GetRangeVarColumn(outerJoinNode.LeftKey.Columns[0].Name).GetExpression());
								caseItem.ThenExpression = new ValueExpression(0);
								caseExpression.CaseItems.Add(caseItem);
								caseExpression.ElseExpression = new CaseElseExpression(new ValueExpression(1));
								rowExistsExpression = caseExpression;
							}
							SQLRangeVarColumn rangeVarColumn = new SQLRangeVarColumn(rowExistsColumn, rowExistsExpression, localDevicePlan.Device.ToSQLIdentifier(rowExistsColumn));
							localDevicePlan.CurrentQueryContext().AddedColumns.Add(rangeVarColumn);
							leftSelectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
						}

						((AlgebraicFromClause)leftSelectExpression.FromClause).Joins.Add(joinClause);
						
						// Build select clause
						leftSelectExpression.SelectClause = new SelectClause();
						foreach (TableVarColumn column in ((TableNode)planNode).TableVar.Columns)
							if ((leftOuterJoinNode != null) && leftOuterJoinNode.LeftKey.Columns.ContainsName(column.Name))
								leftSelectExpression.SelectClause.Columns.Add(leftContext.GetRangeVarColumn(column.Name).GetColumnExpression());
							else if ((rightOuterJoinNode != null) && rightOuterJoinNode.RightKey.Columns.ContainsName(column.Name))
								leftSelectExpression.SelectClause.Columns.Add(rightContext.GetRangeVarColumn(column.Name).GetColumnExpression());
							else if ((withoutNode != null) && withoutNode.LeftKey.Columns.ContainsName(column.Name))
								leftSelectExpression.SelectClause.Columns.Add(leftContext.GetRangeVarColumn(column.Name).GetColumnExpression());
							else
								leftSelectExpression.SelectClause.Columns.Add(localDevicePlan.GetRangeVarColumn(column.Name, true).GetColumnExpression());
							
						// Merge where clauses
						if (rightSelectExpression.WhereClause != null)
							if (leftSelectExpression.WhereClause == null)
								leftSelectExpression.WhereClause = rightSelectExpression.WhereClause;
							else
								leftSelectExpression.WhereClause.Expression = new BinaryExpression(leftSelectExpression.WhereClause.Expression, "iAnd", rightSelectExpression.WhereClause.Expression);
								
						// Distinct if necessary
						if ((semiTableNode != null) && !semiTableNode.RightKey.IsUnique)
							leftSelectExpression.SelectClause.Distinct = true;
								
						// Add without where clause
						if (withoutNode != null)
						{
							Expression withoutExpression = null;
							
							foreach (TableVarColumn column in withoutNode.RightKey.Columns.Count > 0 ? (TableVarColumnsBase)withoutNode.RightKey.Columns : (TableVarColumnsBase)rightTableVar.Columns)
							{
								Expression columnExpression = new UnaryExpression("iIsNull", localDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(column.Name).GetExpression());
								
								if (withoutExpression == null)
									withoutExpression = columnExpression;
								else
									withoutExpression = new BinaryExpression(withoutExpression, "iAnd", columnExpression);
							}
							
							if (withoutExpression != null)
							{
								if (leftSelectExpression.WhereClause == null)
									leftSelectExpression.WhereClause = new WhereClause(withoutExpression);
								else
									leftSelectExpression.WhereClause.Expression = new BinaryExpression(leftSelectExpression.WhereClause.Expression, "iAnd", withoutExpression);
							}
						}
						
						return leftStatement;
					}
					finally
					{
						localDevicePlan.PopJoinContext();
					}
				}
			}
			
			return new SelectExpression();
		}
    }
    
    public class SQLScalarSelector : SQLDeviceOperator
    {
		public SQLScalarSelector(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
    }
    
    public class SQLScalarReadAccessor : SQLDeviceOperator
    {
		public SQLScalarReadAccessor(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
    }
    
    public class SQLScalarWriteAccessor : SQLDeviceOperator
    {
		public SQLScalarWriteAccessor(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
		}
    }
    
    public class SQLScalarIsSpecialOperator : SQLDeviceOperator
    {
		public SQLScalarIsSpecialOperator(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
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
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, new ValueNode(new Scalar(LDevicePlan.Plan.ServerProcess, LDevicePlan.Plan.Catalog.DataTypes.SystemIString, ((IScalar)APlanNode.Nodes[0].Execute(ADevicePlan.Plan.ServerProcess).Value).ToString())), false);
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
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, new ValueNode(new Scalar(LDevicePlan.Plan.ServerProcess, LDevicePlan.Plan.Catalog.DataTypes.SystemIString, ((IScalar)APlanNode.Nodes[1].Execute(ADevicePlan.Plan.ServerProcess).Value).ToString())), false);
			else
				return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
		}
    }
    #endif
    
    public class SQLVersionNumberIsUndefinedOperator : SQLDeviceOperator
    {
		public SQLVersionNumberIsUndefinedOperator(int iD, string name) : base(iD, name)  {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression versionNumber = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(versionNumber, "iEqual", new ValueExpression("****************************************", TokenType.String));
		}
    }
    
    public abstract class SQLOperator : SQLDeviceOperator
    {
		public SQLOperator(int iD, string name) : base(iD, name) {}
		
		public abstract string GetInstruction();
		public abstract bool GetIsBooleanContext();
    }
    
    public abstract class SQLUnaryOperator : SQLOperator
    {
		public SQLUnaryOperator(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return new UnaryExpression(GetInstruction(), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext()));
		}
    }
    
    public abstract class SQLBinaryOperator : SQLOperator
    {
		public SQLBinaryOperator(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return new BinaryExpression
			(
				localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext()), 
				GetInstruction(), 
				localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], GetIsBooleanContext())
			);
		}
    }
    
    public class SQLCallOperator : SQLDeviceOperator
    {
		public SQLCallOperator(int iD, string name) : base(iD, name) {}
	
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression[] expressions = new Expression[planNode.Nodes.Count];
			for (int index = 0; index < expressions.Length; index++)
			{
				if (IsParameterContextLiteral(index) && !planNode.Nodes[index].IsContextLiteral(0))
				{
					localDevicePlan.IsSupported = false;
					localDevicePlan.TranslationMessages.Add(new TranslationMessage(String.Format(@"Plan is not supported because argument ({0}) to operator ""{1}"" is not context literal", index.ToString(), Operator.OperatorName), planNode));
					return null;
				}
				expressions[index] = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[index], false);
			}

			return new CallExpression(_operatorName, expressions);
		}

		private string _operatorName;		
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = value; }
		}
    }
    
    public class SQLUserOperator : SQLDeviceOperator
    {
		public SQLUserOperator(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression[] expressions = new Expression[planNode.Nodes.Count];
			for (int index = 0; index < expressions.Length; index++)
			{
				if (IsParameterContextLiteral(index) && !planNode.Nodes[index].IsContextLiteral(0))
				{
					localDevicePlan.IsSupported = false;
					localDevicePlan.TranslationMessages.Add(new TranslationMessage(String.Format(@"Plan is not supported because argument ({0}) to operator ""{1}"" is not context literal", index.ToString(), Operator.OperatorName), planNode));
					return null;
				}
				expressions[index] = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[index], false);
			}
				
			return new UserExpression(_translationString, expressions);
		}

		private string _translationString = String.Empty;
		public string TranslationString
		{
			get { return _translationString; }
			set { _translationString = value == null ? String.Empty : value; }
		}
    }
    
    public class SQLIntegerDivision : SQLDeviceOperator
    {
		public SQLIntegerDivision(int iD, string name) : base(iD, name) {}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return 
				new BinaryExpression
				(
					new CastExpression
					(
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
						((SQLScalarType)localDevicePlan.Device.ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType)).DomainName()
					),
					"iDivision", 
					new CastExpression
					(
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
						((SQLScalarType)localDevicePlan.Device.ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType)).DomainName()
					)
				);
		}
    }
    
    public class SQLDecimalDiv : SQLDeviceOperator
    {
		public SQLDecimalDiv(int iD, string name) : base(iD, name) {}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return 
				new CastExpression
				(
					new BinaryExpression
					(
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false), 
						"iDivision", 
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false)
					),
					((SQLScalarType)devicePlan.Device.ResolveDeviceScalarType(devicePlan.Plan, (Schema.ScalarType)planNode.DataType)).DomainName()
				);
		}
    }
    
	// Comparison operators    
    public class SQLEqual : SQLBinaryOperator 
    { 
		public SQLEqual(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iEqual"; } 
		public override bool GetIsBooleanContext() { return false; }
	}

    public class SQLNotEqual : SQLBinaryOperator 
    { 
		public SQLNotEqual(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iNotEqual"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLLess : SQLBinaryOperator 
    { 
		public SQLLess(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iLess"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLInclusiveLess : SQLBinaryOperator 
    { 
		public SQLInclusiveLess(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iInclusiveLess"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLGreater : SQLBinaryOperator 
    { 
		public SQLGreater(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iGreater"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLInclusiveGreater : SQLBinaryOperator 
    { 
		public SQLInclusiveGreater(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iInclusiveGreater"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
	
    public class SQLLike : SQLBinaryOperator 
    { 
		public SQLLike(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iLike"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
	
    public class SQLMatches : SQLBinaryOperator 
    { 
		public SQLMatches(int iD, string name) : base(iD, name) {}
		
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
		public SQLCompare(int iD, string name) : base(iD, name) {}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return 
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							new BinaryExpression
							(
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
								"iEqual",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false)
							), 
							new ValueExpression(0)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
								"iLess",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false)
							),
							new ValueExpression(-1)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
								"iGreater",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false)
							),
							new ValueExpression(1)
						),
					},
					new CaseElseExpression(new ValueExpression(null, TokenType.Nil))
				);
		}
	}
	
	public class SQLBetween : SQLDeviceOperator
	{
		public SQLBetween(int iD, string name) : base(iD, name) {}

		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return 
				new BetweenExpression
				(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false)
				);
		}
	}
	
	// Conversions
	
	// ToString(AValue) ::= case when AValue = 0 then 'False' else 'True' end
	public class SQLBooleanToString : SQLDeviceOperator
	{
		public SQLBooleanToString(int iD, string name) : base(iD, name) {}
	
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CaseExpression
				(
					new CaseItemExpression[]
					{
						new CaseItemExpression
						(
							new BinaryExpression
							(
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
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
		public SQLIsNull(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iIsNull"; }
		public override bool GetIsBooleanContext() { return false; }

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			if (planNode.Nodes[0].DataType is Schema.IScalarType)
			{
				if (planNode.Nodes[0].DataType.Is(devicePlan.Plan.ServerProcess.DataTypes.SystemBoolean) && !((planNode.Nodes[0] is StackReferenceNode || planNode.Nodes[0] is StackColumnReferenceNode)))
				{
					localDevicePlan.IsSupported = false;
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because there is no SQL equivalent of IsNil() for a boolean-valued expression. Consider rewriting the D4 using a conditional expression.", planNode));
					return new SelectExpression();
				}
				return new UnaryExpression(GetInstruction(), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext()));
			}
			else if (planNode.Nodes[0].DataType is Schema.IRowType)
				return new UnaryExpression("iNot", new UnaryExpression("iExists", localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext())));
			else
			{
				localDevicePlan.IsSupported = false;
				localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because invocation of IsNil for non-scalar- or row-valued expressions is not supported.", planNode));
				return new SelectExpression();
			}
		}
	}
	
	// Null handling operators
	public class SQLIsNotNull : SQLUnaryOperator
	{
		public SQLIsNotNull(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iIsNotNull"; }
		public override bool GetIsBooleanContext() { return false; }

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			if (planNode.Nodes[0].DataType is Schema.IScalarType)
			{
				if (planNode.Nodes[0].DataType.Is(devicePlan.Plan.ServerProcess.DataTypes.SystemBoolean))
				{
					localDevicePlan.IsSupported = false;
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because there is no SQL equivalent of IsNotNil() for a boolean-valued expression. Consider rewriting the D4 using a conditional expression.", planNode));
					return new SelectExpression();
				}
				return new UnaryExpression(GetInstruction(), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext()));
			}
			else if (planNode.Nodes[0].DataType is Schema.IRowType)
				return new UnaryExpression("iExists", localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext()));
			else
			{
				localDevicePlan.IsSupported = false;
				localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because invocation of IsNotNil for non-scalar- or row-valued expressions is not supported.", planNode));
				return new SelectExpression();
			}
		}
	}
	
	public class SQLIfNull : SQLBinaryOperator
	{
		public SQLIfNull(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iNullValue"; }
		public override bool GetIsBooleanContext() { return false; }

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			if ((planNode.Nodes[0].DataType is Schema.IScalarType) && (planNode.Nodes[1].DataType is Schema.IScalarType))
			{
				if ((planNode.Nodes[0].DataType.Is(devicePlan.Plan.ServerProcess.DataTypes.SystemBoolean)))
				{
					localDevicePlan.IsSupported = false;
					localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because there is no SQL equivalent of IfNil() for a boolean-valued expression. Consider rewriting the D4 using a conditional expression.", planNode));
					return new SelectExpression();
				}
				
				return new BinaryExpression
				(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext()), 
					GetInstruction(), 
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], GetIsBooleanContext())
				);
			}
			else
			{
				localDevicePlan.IsSupported = false;
				localDevicePlan.TranslationMessages.Add(new Schema.TranslationMessage("Plan is not supported because invocation of IfNil for non-scalar valued expressions is not supported.", planNode));
				return new SelectExpression();
			}
		}
	}
    
    // Logical operators
    public class SQLNot : SQLDeviceOperator
    { 
		public SQLNot(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			if (localDevicePlan.IsBooleanContext())
				return new UnaryExpression("iNot", localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], true));
			else
				return new CaseExpression(new CaseItemExpression[]{new CaseItemExpression(localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], true), new ValueExpression(0))}, new CaseElseExpression(new ValueExpression(1)));
		}
	}
    
    public class SQLAnd : SQLBinaryOperator 
    { 
		public SQLAnd(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iAnd"; } 
		public override bool GetIsBooleanContext() { return true; }
	}
    
    public class SQLOr : SQLBinaryOperator 
    { 
		public SQLOr(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iOr"; } 
		public override bool GetIsBooleanContext() { return true; }
	}
    
    public class SQLXor : SQLBinaryOperator 
    { 
		public SQLXor(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override string GetInstruction() { return "iXor"; } 
		public override bool GetIsBooleanContext() { return true; }

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], true);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], true);
			Expression first = new BinaryExpression(expression1, "iOr", expression2);
			expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], true);
			expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], true);
			Expression second = new BinaryExpression(expression1, "iAnd", expression2);
			Expression third = new UnaryExpression("iNot", second);
			return new BinaryExpression(first, "iAnd", third);
		}
	}
    
    // Arithmetic operators
    public class SQLNegate : SQLUnaryOperator 
    { 
		public SQLNegate(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iNegate"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
	public class SQLConcatenation : SQLBinaryOperator
	{
		public SQLConcatenation(int iD, string name) : base(iD, name) {}

		public override string GetInstruction() { return "iConcatenation"; }
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLAddition : SQLBinaryOperator 
    { 
		public SQLAddition(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iAddition"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLSubtraction : SQLBinaryOperator 
    { 
		public SQLSubtraction(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iSubtraction"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLMultiplication : SQLBinaryOperator 
    { 
		public SQLMultiplication(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iMultiplication"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLDivision : SQLBinaryOperator 
    { 
		public SQLDivision(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iDivision"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLMod : SQLBinaryOperator 
    { 
		public SQLMod(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iMod"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLPower : SQLBinaryOperator 
    { 
		public SQLPower(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iPower"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    // Bitwise operators
    public class SQLBitwiseNot : SQLUnaryOperator 
    { 
		public SQLBitwiseNot(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iBitwiseNot"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLBitwiseAnd : SQLBinaryOperator 
    { 
		public SQLBitwiseAnd(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iBitwiseAnd"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLBitwiseOr : SQLBinaryOperator 
    { 
		public SQLBitwiseOr(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iBitwiseOr"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLBitwiseXor : SQLBinaryOperator 
    { 
		public SQLBitwiseXor(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iBitwiseXor"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLLeftShift : SQLBinaryOperator 
    { 
		public SQLLeftShift(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iShiftLeft"; } 
		public override bool GetIsBooleanContext() { return false; }
	}
    
    public class SQLRightShift : SQLBinaryOperator 
    { 
		public SQLRightShift(int iD, string name) : base(iD, name) {}
		
		public override string GetInstruction() { return "iShiftRight"; } 
		public override bool GetIsBooleanContext() { return false; }
	}

	// Existential
    public class SQLExists : SQLDeviceOperator
    { 
		public SQLExists(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			localDevicePlan.PushQueryContext(); // Push a query context to get us out of the scalar context
			try
			{
				Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
				if (localDevicePlan.IsSupported)
				{
					SelectExpression selectExpression = localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, expression, false);
					selectExpression.SelectClause.Columns.Clear();
					selectExpression.SelectClause.NonProject = true;
					return new UnaryExpression("iExists", selectExpression);
				}
				return expression; // not supported so it doesn't matter what gets returned
			}
			finally
			{
				localDevicePlan.PopQueryContext();
			}
		}
	}
    
    public class SQLIn : SQLBinaryOperator 
    { 
		public SQLIn(int iD, string name) : base(iD, name) {}
		
		protected override bool GetIsTruthValued()
		{
			return true;
		}
		
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression leftExpression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], GetIsBooleanContext());
			Expression rightExpression = null;
			localDevicePlan.PushQueryContext();
			if (planNode.Nodes[1].DataType is Schema.ListType)
				localDevicePlan.CurrentQueryContext().IsListContext = true;
			try
			{
				rightExpression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], GetIsBooleanContext());
			}
			finally
			{
				localDevicePlan.PopQueryContext();
			}
			return new BinaryExpression
			(
				leftExpression, 
				GetInstruction(), 
				rightExpression
			);
		}

		public override string GetInstruction() { return "iIn"; } 
		public override bool GetIsBooleanContext() { return false; }
	}

	public class SQLDoNothing : SQLDeviceOperator
	{
		public SQLDoNothing(int iD, string name) : base(iD, name) {}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
	}
}

