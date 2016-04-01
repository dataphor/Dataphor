/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.Oracle;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using SelectExpression=Alphora.Dataphor.DAE.Language.Oracle.SelectExpression;
using SelectStatement=Alphora.Dataphor.DAE.Language.SQL.SelectStatement;

namespace Alphora.Dataphor.DAE.Device.Oracle
{
	
	public class OracleRetrieve : SQLDeviceOperator
	{
		public OracleRetrieve(int iD, string name) : base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			TableVar tableVar = ((TableVarNode) planNode).TableVar;

			if (tableVar is BaseTableVar)
			{
				var rangeVar = new SQLRangeVar(localDevicePlan.GetNextTableAlias());
				foreach (TableVarColumn column in tableVar.Columns)
					rangeVar.Columns.Add(new SQLRangeVarColumn(column, rangeVar.Name,
																localDevicePlan.Device.ToSQLIdentifier(column),
																localDevicePlan.Device.ToSQLIdentifier(column.Name)));
				localDevicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
				var selectExpression = new SelectExpression();
				selectExpression.OptimizerHints = "FIRST_ROWS(20)";
				selectExpression.FromClause =
					new AlgebraicFromClause
						(
						new TableSpecifier
							(
							new TableExpression
								(
								MetaData.GetTag(tableVar.MetaData, "Storage.Schema", localDevicePlan.Device.Schema),
								localDevicePlan.Device.ToSQLIdentifier(tableVar)
								),
							rangeVar.Name
							)
						);
				selectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn column in tableVar.Columns)
					selectExpression.SelectClause.Columns.Add(
						localDevicePlan.GetRangeVarColumn(column.Name, true).GetColumnExpression());

				selectExpression.SelectClause.Distinct =
					(tableVar.Keys.Count == 1) &&
					Convert.ToBoolean(MetaData.GetTag(tableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));

				return selectExpression;
			}
			else
				return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
	}

	public class OracleJoin : SQLDeviceOperator
	{
		public OracleJoin(int iD, string name) : base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			var joinNode = (JoinNode) planNode;
			JoinType joinType;
			if ((((joinNode.Nodes[2] is ValueNode) &&
				  ((joinNode.Nodes[2]).DataType.Is(devicePlan.Plan.Catalog.DataTypes.SystemBoolean)) &&
				  ((bool) ((ValueNode) joinNode.Nodes[2]).Value))))
				joinType = JoinType.Cross;
			else if (joinNode is LeftOuterJoinNode)
				joinType = JoinType.Left;
			else if (joinNode is RightOuterJoinNode)
				joinType = JoinType.Right;
			else
				joinType = JoinType.Inner;

			bool hasOuterColumnExpressions = false;

			localDevicePlan.PushQueryContext();
			Statement leftStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Language.SQL.SelectExpression leftSelectExpression =
				localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode) planNode.Nodes[0]).TableVar,
															   leftStatement, false);
			TableVar leftTableVar = ((TableNode) planNode.Nodes[0]).TableVar;
			for (int index = 0; index < joinNode.LeftKey.Columns.Count; index++)
				if (localDevicePlan.GetRangeVarColumn(joinNode.LeftKey.Columns[index].Name, true).Expression != null)
				{
					hasOuterColumnExpressions = true;
					break;
				}

			if (hasOuterColumnExpressions || localDevicePlan.CurrentQueryContext().IsAggregate ||
				leftSelectExpression.SelectClause.Distinct)
			{
				string nestingReason = "The left argument to the join operator must be nested because ";
				if (hasOuterColumnExpressions)
					nestingReason +=
						"the join is to be performed on columns which are introduced as expressions in the current context.";
				else if (localDevicePlan.CurrentQueryContext().IsAggregate)
					nestingReason += "it contains aggregation.";
				else
					nestingReason += "it contains a distinct specification.";
				localDevicePlan.TranslationMessages.Add(new TranslationMessage(nestingReason, planNode));
				leftStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, leftTableVar, leftStatement);
				leftSelectExpression = localDevicePlan.Device.FindSelectExpression(leftStatement);
			}
			SQLQueryContext leftContext = localDevicePlan.CurrentQueryContext();
			localDevicePlan.PopQueryContext();

			localDevicePlan.PushQueryContext();
			Statement rightStatement = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Language.SQL.SelectExpression rightSelectExpression =
				localDevicePlan.Device.EnsureUnarySelectExpression(localDevicePlan, ((TableNode) planNode.Nodes[1]).TableVar,
															   rightStatement, false);
			TableVar rightTableVar = ((TableNode) planNode.Nodes[1]).TableVar;
			hasOuterColumnExpressions = false;
			for (int index = 0; index < joinNode.RightKey.Columns.Count; index++)
				if (localDevicePlan.GetRangeVarColumn(joinNode.RightKey.Columns[index].Name, true).Expression != null)
				{
					hasOuterColumnExpressions = true;
					break;
				}

			if (hasOuterColumnExpressions || localDevicePlan.CurrentQueryContext().IsAggregate ||
				rightSelectExpression.SelectClause.Distinct)
			{
				string nestingReason = "The right argument to the join operator must be nested because ";
				if (hasOuterColumnExpressions)
					nestingReason +=
						"the join is to be performed on columns which are introduced as expressions in the current context.";
				else if (localDevicePlan.CurrentQueryContext().IsAggregate)
					nestingReason += "it contains aggregation.";
				else
					nestingReason += "it contains a distinct specification.";
				localDevicePlan.TranslationMessages.Add(new TranslationMessage(nestingReason, planNode));
				rightStatement = localDevicePlan.Device.NestQueryExpression(localDevicePlan, rightTableVar, rightStatement);
				rightSelectExpression = localDevicePlan.Device.FindSelectExpression(rightStatement);
			}
			SQLQueryContext rightContext = localDevicePlan.CurrentQueryContext();
			localDevicePlan.PopQueryContext();

			// Merge the query contexts
			localDevicePlan.CurrentQueryContext().RangeVars.AddRange(leftContext.RangeVars);
			localDevicePlan.CurrentQueryContext().AddedColumns.AddRange(leftContext.AddedColumns);
			localDevicePlan.CurrentQueryContext().RangeVars.AddRange(rightContext.RangeVars);
			localDevicePlan.CurrentQueryContext().AddedColumns.AddRange(rightContext.AddedColumns);

			// Merge the from clauses
			var leftFromClause = (CalculusFromClause) leftSelectExpression.FromClause;
			var rightFromClause = (CalculusFromClause) rightSelectExpression.FromClause;
			foreach (TableSpecifier tableSpecifier in rightFromClause.TableSpecifiers)
				leftFromClause.TableSpecifiers.Add(tableSpecifier);

			localDevicePlan.PushJoinContext(new SQLJoinContext(leftContext, rightContext));
			try
			{
				if (joinType != JoinType.Cross)
				{
					Expression joinCondition = null;

					for (int index = 0; index < joinNode.LeftKey.Columns.Count; index++)
					{
						SQLRangeVarColumn leftColumn =
							localDevicePlan.CurrentJoinContext().LeftQueryContext.GetRangeVarColumn(
								joinNode.LeftKey.Columns[index].Name);
						SQLRangeVarColumn rightColumn =
							localDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(
								joinNode.RightKey.Columns[index].Name);
						Expression leftExpression = leftColumn.GetExpression();
						Expression rightExpression = rightColumn.GetExpression();
						if (joinType == JoinType.Right)
						{
							var fieldExpression = (QualifiedFieldExpression) leftExpression;
							leftExpression = new OuterJoinFieldExpression(fieldExpression.FieldName,
																		   fieldExpression.TableAlias);
						}
						else if (joinType == JoinType.Left)
						{
							var fieldExpression = (QualifiedFieldExpression) rightExpression;
							rightExpression = new OuterJoinFieldExpression(fieldExpression.FieldName,
																			fieldExpression.TableAlias);
						}

						Expression equalExpression =
							new BinaryExpression
								(
								leftExpression,
								"iEqual",
								rightExpression
								);

						if (joinCondition != null)
							joinCondition = new BinaryExpression(joinCondition, "iAnd", equalExpression);
						else
							joinCondition = equalExpression;
					}

					if (leftSelectExpression.WhereClause == null)
						leftSelectExpression.WhereClause = new WhereClause(joinCondition);
					else
						leftSelectExpression.WhereClause.Expression =
							new BinaryExpression(leftSelectExpression.WhereClause.Expression, "iAnd", joinCondition);

					var outerJoinNode = joinNode as OuterJoinNode;
					if ((outerJoinNode != null) && (outerJoinNode.RowExistsColumnIndex >= 0))
					{
						TableVarColumn rowExistsColumn =
							outerJoinNode.TableVar.Columns[outerJoinNode.RowExistsColumnIndex];
						var caseExpression = new CaseExpression();
						var caseItem = new CaseItemExpression();
						if (outerJoinNode is LeftOuterJoinNode)
							caseItem.WhenExpression = new UnaryExpression("iIsNull",
																		   localDevicePlan.CurrentJoinContext().
																			   RightQueryContext.GetRangeVarColumn(
																			   outerJoinNode.RightKey.Columns[0].Name).
																			   GetExpression());
						else
							caseItem.WhenExpression = new UnaryExpression("iIsNull",
																		   localDevicePlan.CurrentJoinContext().
																			   LeftQueryContext.GetRangeVarColumn(
																			   outerJoinNode.LeftKey.Columns[0].Name).
																			   GetExpression());
						caseItem.ThenExpression = new ValueExpression(0);
						caseExpression.CaseItems.Add(caseItem);
						caseExpression.ElseExpression = new CaseElseExpression(new ValueExpression(1));
						var rangeVarColumn = new SQLRangeVarColumn(rowExistsColumn, caseExpression,
																	localDevicePlan.Device.ToSQLIdentifier(rowExistsColumn));
						localDevicePlan.CurrentQueryContext().AddedColumns.Add(rangeVarColumn);
						leftSelectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
					}
				}

				// Build select clause
				leftSelectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn column in ((TableNode) planNode).TableVar.Columns)
					leftSelectExpression.SelectClause.Columns.Add(
						localDevicePlan.GetRangeVarColumn(column.Name, true).GetColumnExpression());

				// Merge where clauses
				if (rightSelectExpression.WhereClause != null)
					if (leftSelectExpression.WhereClause == null)
						leftSelectExpression.WhereClause = rightSelectExpression.WhereClause;
					else
						leftSelectExpression.WhereClause.Expression =
							new BinaryExpression(leftSelectExpression.WhereClause.Expression, "iAnd",
												 rightSelectExpression.WhereClause.Expression);

				return leftStatement;
			}
			finally
			{
				localDevicePlan.PopJoinContext();
			}
		}
	}


	public class OracleMathUtility
	{
		public static Expression Mod(Expression expression, Expression moduloExpression)
		{
			return new CallExpression("MOD", new[] { expression, moduloExpression });
		}

		public static Expression Truncate(Expression expression)
		{
			return new CallExpression("TRUNC", new[] { expression, new ValueExpression(0) });
		}

		public static Expression Round(Expression expression)
		{
			return new CallExpression("ROUND", new[] { expression });
		}

		public static Expression Frac(Expression expression, Expression expressionCopy)
			// note that it takes two different refrences to the same value
		{
			return new BinaryExpression(expression, "iSubtraction", Truncate(expressionCopy));
		}
	}

	public class OracleTimeSpanUtility
	{
		//LReturnVal := TRUNC(MOD(ATimeSpan, TicksPerSecond) / TicksPerMillisecond);
		public static Expression ReadMillisecond(Expression tempValue)
		{
			return 
				OracleMathUtility.Truncate
				(
					new BinaryExpression
					(
						OracleMathUtility.Mod(tempValue, new ValueExpression(TimeSpan.TicksPerSecond)), 
						"iDivision", 
						new ValueExpression(TimeSpan.TicksPerMillisecond)
					)
				);
		}

		public static Expression ReadSecond(Expression tempValue)
		{
			//LReturnVal := TRUNC(MOD(ATimeSpan, TicksPerMinute) / TicksPerSecond);
			return 
				OracleMathUtility.Truncate
				(
					new BinaryExpression
					(
						OracleMathUtility.Mod(tempValue, new ValueExpression(TimeSpan.TicksPerMinute)), 
						"iDivision", 
						new ValueExpression(TimeSpan.TicksPerSecond)
					)
				);
		}

		public static Expression ReadMinute(Expression tempValue)
		{
			//LReturnVal := TRUNC(MOD(ATimeSpan, TicksPerHour) / TicksPerMinute);
			return 
				OracleMathUtility.Truncate
				(
					new BinaryExpression
					(
						OracleMathUtility.Mod(tempValue, new ValueExpression(TimeSpan.TicksPerHour)), 
						"iDivision", 
						new ValueExpression(TimeSpan.TicksPerMinute)
					)
				);
		}

		public static Expression ReadHour(Expression tempValue)
		{
			//LReturnVal := TRUNC(MOD(ATimeSpan, TicksPerDay) / TicksPerHour)
			return 
				OracleMathUtility.Truncate
				(
					new BinaryExpression
					(
						OracleMathUtility.Mod(tempValue, new ValueExpression(TimeSpan.TicksPerDay)), 
						"iDivision", 
						new ValueExpression(TimeSpan.TicksPerHour)
					)
				);
		}

		public static Expression ReadDay(Expression tempValue)
		{
			//LReturnVal := TRUNC(ATimeSpan / TicksPerDay);
			return
				OracleMathUtility.Truncate(new BinaryExpression(tempValue, "iDivision", new ValueExpression(TimeSpan.TicksPerDay)));
		}
	}

	public class OracleDateTimeFunctions
	{
		public static Expression WriteMonth(Expression dateTime, Expression dateTimeCopy, Expression part)
		{
			string partString = "mm";
			Expression oldPart = new CallExpression("DatePart",
													 new[]
														 {
															 new ValueExpression(partString, TokenType.Symbol),
															 dateTimeCopy
														 });
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd",
									  new[] {new ValueExpression(partString, TokenType.Symbol), parts, dateTime});
		}

		public static Expression WriteDay(Expression dateTime, Expression dateTimeCopy, Expression part)
			//pass the DateTime twice
		{
			string partString = "dd";
			Expression oldPart = new CallExpression("DatePart",
													 new[]
														 {
															 new ValueExpression(partString, TokenType.Symbol),
															 dateTimeCopy
														 });
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd",
									  new[] {new ValueExpression(partString, TokenType.Symbol), parts, dateTime});
		}

		public static Expression WriteYear(Expression dateTime, Expression dateTimeCopy, Expression part)
			//pass the DateTime twice
		{
			string partString = "yyyy";
			Expression oldPart = new CallExpression("DatePart",
													 new[]
														 {
															 new ValueExpression(partString, TokenType.Symbol),
															 dateTimeCopy
														 });
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd",
									  new[] {new ValueExpression(partString, TokenType.Symbol), parts, dateTime});
		}
	}

	public class OracleFrac : DeviceOperator
	{
		public OracleFrac(int iD, string name) : base(iD, name)
		{
		}

		//public OracleFrac(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleFrac(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				OracleMathUtility.Frac
					(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					);
		}
	}

	// TimeSpan
	public class OracleTimeSpanReadMillisecond : DeviceOperator
	{
		public OracleTimeSpanReadMillisecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = ((SQLDevicePlan) devicePlan);
			return
				OracleTimeSpanUtility.ReadMillisecond
					(
					localDevicePlan.Device.TranslateExpression
						(
						localDevicePlan,
						planNode.Nodes[0],
						false
						)
					);
		}
	}

	public class OracleTimeSpanReadSecond : DeviceOperator
	{
		public OracleTimeSpanReadSecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				OracleTimeSpanUtility.ReadSecond(localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																						false));
		}
	}

	public class OracleTimeSpanReadMinute : DeviceOperator
	{
		public OracleTimeSpanReadMinute(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				OracleTimeSpanUtility.ReadMinute(localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																						false));
		}
	}

	public class OracleTimeSpanReadHour : DeviceOperator
	{
		public OracleTimeSpanReadHour(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				OracleTimeSpanUtility.ReadHour(localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					  false));
		}
	}

	public class OracleTimeSpanReadDay : DeviceOperator
	{
		public OracleTimeSpanReadDay(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				OracleTimeSpanUtility.ReadDay(localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false));
		}
	}

	public class OracleTimeSpanWriteMillisecond : DeviceOperator
	{
		public OracleTimeSpanWriteMillisecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := ATimeSpan + (APart - ReadMillisecond(ATimeSpan)) * 10000;
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					"iAddition",
					new BinaryExpression
						(
						new BinaryExpression
							(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
							"iSubtraction",
							OracleTimeSpanUtility.ReadMillisecond(localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										 planNode.Nodes
																											 [0], false))
							),
						"iMultiplication",
						new ValueExpression(10000)
						)
					);
		}
	}

	public class OracleTimeSpanWriteSecond : DeviceOperator
	{
		public OracleTimeSpanWriteSecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := ATimeSpan + (APart - ReadSecond(ATimeSpan)) * 10000000;
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					"iAddition",
					new BinaryExpression
						(
						new BinaryExpression
							(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
							"iSubtraction",
							OracleTimeSpanUtility.ReadSecond(localDevicePlan.Device.TranslateExpression(localDevicePlan,
																									planNode.Nodes[0],
																									false))
							),
						"iMultiplication",
						new ValueExpression(10000000)
						)
					);
		}
	}

	public class OracleTimeSpanWriteMinute : DeviceOperator
	{
		public OracleTimeSpanWriteMinute(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := ATimeSpan + (APart - ReadMinute(ATimeSpan)) * 600000000;
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					"iAddition",
					new BinaryExpression
						(
						new BinaryExpression
							(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
							"iSubtraction",
							OracleTimeSpanUtility.ReadMinute(localDevicePlan.Device.TranslateExpression(localDevicePlan,
																									planNode.Nodes[0],
																									false))
							),
						"iMultiplication",
						new ValueExpression(600000000)
						)
					);
		}
	}

	public class OracleTimeSpanWriteHour : DeviceOperator
	{
		public OracleTimeSpanWriteHour(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := ATimeSpan + (APart - ReadHour(ATimeSpan)) * 36000000000;
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					"iAddition",
					new BinaryExpression
						(
						new BinaryExpression
							(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
							"iSubtraction",
							OracleTimeSpanUtility.ReadHour(localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false))
							),
						"iMultiplication",
						new ValueExpression(36000000000)
						)
					);
		}
	}

	public class OracleTimeSpanWriteDay : DeviceOperator
	{
		public OracleTimeSpanWriteDay(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := ATimeSpan + (APart - ReadDay(ATimeSpan)) * 864000000000;
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
					"iAddition",
					new BinaryExpression
						(
						new BinaryExpression
							(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
							"iSubtraction",
							OracleTimeSpanUtility.ReadDay(localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								 planNode.Nodes[0],
																								 false))
							),
						"iMultiplication",
						new ValueExpression(864000000000)
						)
					);
		}
	}

	public class OracleAddYears : DeviceOperator
	{
		public OracleAddYears(int iD, string name) : base(iD, name)
		{
		}

		//public OracleAddYears(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleAddYears(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := ADD_MONTHS(ADateTime, AYears * 12);
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"ADD_MONTHS",
					new[]
						{
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
							new BinaryExpression
								(
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
								"iMultiplication",
								new ValueExpression(12)
								)
						}
					);
		}
	}

	public class OracleDayOfWeek : DeviceOperator
	{
		public OracleDayOfWeek(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDayOfWeek(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDayOfWeek(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'd');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("d")
										  });
		}
	}

	public class OracleDayOfYear : DeviceOperator
	{
		public OracleDayOfYear(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDayOfYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDayOfYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'ddd');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("ddd")
										  });
		}
	}

	public class OracleDateTimeReadYear : DeviceOperator
	{
		public OracleDateTimeReadYear(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'yyyy');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("yyyy")
										  });
		}
	}

	public class OracleDateTimeReadMonth : DeviceOperator
	{
		public OracleDateTimeReadMonth(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadMonth(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadMonth(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'mm');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("mm")
										  });
		}
	}

	public class OracleDateTimeReadDay : DeviceOperator
	{
		public OracleDateTimeReadDay(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'dd');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("dd")
										  });
		}
	}

	public class OracleDateTimeReadHour : DeviceOperator
	{
		public OracleDateTimeReadHour(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'hh');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("hh")
										  });
		}
	}

	public class OracleDateTimeReadMinute : DeviceOperator
	{
		public OracleDateTimeReadMinute(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'mi');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("mi")
										  });
		}
	}

	public class OracleDateTimeReadSecond : DeviceOperator
	{
		public OracleDateTimeReadSecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Char(ADateTime, 'ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return new CallExpression("TO_CHAR",
									  new[]
										  {
											  localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0],
																					 false), new ValueExpression("ss")
										  });
		}
	}

	public class OracleDateTimeReadMillisecond : DeviceOperator
	{
		public OracleDateTimeReadMillisecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			return new ValueExpression(0);
		}
	}

	public class OracleDateTimeWriteMillisecond : DeviceOperator
	{
		public OracleDateTimeWriteMillisecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
	}

	public class OracleDateTimeWriteSecond : DeviceOperator
	{
		public OracleDateTimeWriteSecond(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(APart), 'yyyy/mm/dd hh24:mi:ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[0],
																								   false),
																							   new ValueExpression(
																								   "yyyy")
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   localDevicePlan.Device.
																							   TranslateExpression(
																							   localDevicePlan,
																							   planNode.Nodes[0], false)
																						   , new ValueExpression("mm")
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   localDevicePlan.Device.
																					   TranslateExpression(localDevicePlan,
																										   planNode.
																											   Nodes[0],
																										   false),
																				   new ValueExpression("dd")
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   localDevicePlan.Device.TranslateExpression(
																			   localDevicePlan, planNode.Nodes[0], false),
																		   new ValueExpression("hh24")
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mi")
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[1],
																								  false)
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeWriteMinute : DeviceOperator
	{
		public OracleDateTimeWriteMinute(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(APart) || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[0],
																								   false),
																							   new ValueExpression(
																								   "yyyy")
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   localDevicePlan.Device.
																							   TranslateExpression(
																							   localDevicePlan,
																							   planNode.Nodes[0], false)
																						   , new ValueExpression("mm")
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   localDevicePlan.Device.
																					   TranslateExpression(localDevicePlan,
																										   planNode.
																											   Nodes[0],
																										   false),
																				   new ValueExpression("dd")
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   localDevicePlan.Device.TranslateExpression(
																			   localDevicePlan, planNode.Nodes[0], false),
																		   new ValueExpression("hh24")
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[1],
																										  false)
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("ss")
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeWriteHour : DeviceOperator
	{
		public OracleDateTimeWriteHour(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(APart) || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[0],
																								   false),
																							   new ValueExpression(
																								   "yyyy")
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   localDevicePlan.Device.
																							   TranslateExpression(
																							   localDevicePlan,
																							   planNode.Nodes[0], false)
																						   , new ValueExpression("mm")
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   localDevicePlan.Device.
																					   TranslateExpression(localDevicePlan,
																										   planNode.
																											   Nodes[0],
																										   false),
																				   new ValueExpression("dd")
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   localDevicePlan.Device.TranslateExpression(
																			   localDevicePlan, planNode.Nodes[1], false)
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mi")
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("ss")
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeWriteDay : DeviceOperator
	{
		public OracleDateTimeWriteDay(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(APart) || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[0],
																								   false),
																							   new ValueExpression(
																								   "yyyy")
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   localDevicePlan.Device.
																							   TranslateExpression(
																							   localDevicePlan,
																							   planNode.Nodes[0], false)
																						   , new ValueExpression("mm")
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   localDevicePlan.Device.
																					   TranslateExpression(localDevicePlan,
																										   planNode.
																											   Nodes[1],
																										   false)
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   localDevicePlan.Device.TranslateExpression(
																			   localDevicePlan, planNode.Nodes[0], false),
																		   new ValueExpression("hh24")
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mi")
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("ss")
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeWriteMonth : DeviceOperator
	{
		public OracleDateTimeWriteMonth(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteMonth(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteMonth(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(APart) || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[0],
																								   false),
																							   new ValueExpression(
																								   "yyyy")
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   localDevicePlan.Device.
																							   TranslateExpression(
																							   localDevicePlan,
																							   planNode.Nodes[1], false)
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   localDevicePlan.Device.
																					   TranslateExpression(localDevicePlan,
																										   planNode.
																											   Nodes[0],
																										   false),
																				   new ValueExpression("dd")
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   localDevicePlan.Device.TranslateExpression(
																			   localDevicePlan, planNode.Nodes[0], false),
																		   new ValueExpression("hh24")
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mi")
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("ss")
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeWriteYear : DeviceOperator
	{
		public OracleDateTimeWriteYear(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeWriteYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeWriteYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := To_Date(To_Char(APart) || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[1],
																								   false)
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   localDevicePlan.Device.
																							   TranslateExpression(
																							   localDevicePlan,
																							   planNode.Nodes[0], false)
																						   , new ValueExpression("mm")
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   localDevicePlan.Device.
																					   TranslateExpression(localDevicePlan,
																										   planNode.
																											   Nodes[0],
																										   false),
																				   new ValueExpression("dd")
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   localDevicePlan.Device.TranslateExpression(
																			   localDevicePlan, planNode.Nodes[0], false),
																		   new ValueExpression("hh24")
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mi")
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("ss")
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeDatePart : DeviceOperator
	{
		public OracleDateTimeDatePart(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeDatePart(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeDatePart(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnValue := TO_DATE(TO_CHAR(ADateTime, 'yyyy') || '/' || TO_CHAR(ADateTime, 'mm') || '/' TO_CHAR(ADateTime, 'dd'), "yyyy/mm/dd");
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new CallExpression("TO_CHAR",
															   new[]
																   {
																	   localDevicePlan.Device.TranslateExpression(
																		   localDevicePlan, planNode.Nodes[0], false),
																	   new ValueExpression("yyyy")
																   }),
											"iConcatenation",
											new ValueExpression("/")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mm")
															   })
										),
									"iConcatenation",
									new ValueExpression("/")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("dd")
													   })
								),
							new ValueExpression("yyyy/mm/dd")
						}
					);
		}
	}

	public class OracleDateTimeTimePart : DeviceOperator
	{
		public OracleDateTimeTimePart(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeTimePart(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeTimePart(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnValue := TO_DATE(TO_CHAR(ADateTime, 'hh24') || '/' || TO_CHAR(ADateTime, 'mi') || '/' TO_CHAR(ADateTime, 'ss'), "hh24:mi:ss");
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new CallExpression("TO_CHAR",
															   new[]
																   {
																	   localDevicePlan.Device.TranslateExpression(
																		   localDevicePlan, planNode.Nodes[0], false),
																	   new ValueExpression("hh24")
																   }),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										  planNode.
																											  Nodes[0],
																										  false),
																   new ValueExpression("mi")
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																								  planNode.Nodes[0],
																								  false),
														   new ValueExpression("ss")
													   })
								),
							new ValueExpression("hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeSelector : DeviceOperator
	{
		public OracleDateTimeSelector(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeSelector(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeSelector(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			// TO_DATE(TO_CHAR(AYears) || "/" || TO_CHAR(AMonths) || "/" || TO_CHAR(ADays) || " " || TO_CHAR(AHours) || ":" || TO_CHAR(AMinutes) ":" || TO_CHAR(ASeconds), "yyyy/mm/dd hh24:mi:ss")
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new CallExpression
					(
					"TO_DATE",
					new Expression[]
						{
							new BinaryExpression
								(
								new BinaryExpression
									(
									new BinaryExpression
										(
										new BinaryExpression
											(
											new BinaryExpression
												(
												new BinaryExpression
													(
													new BinaryExpression
														(
														new BinaryExpression
															(
															new BinaryExpression
																(
																new BinaryExpression
																	(
																	new CallExpression("TO_CHAR",
																					   new[]
																						   {
																							   localDevicePlan.Device.
																								   TranslateExpression(
																								   localDevicePlan,
																								   planNode.Nodes[0],
																								   false)
																						   }),
																	"iConcatenation",
																	new ValueExpression("/")
																	),
																"iConcatenation",
																new CallExpression("TO_CHAR",
																				   new[]
																					   {
																						   planNode.Nodes.Count > 1
																							   ? localDevicePlan.Device.
																									 TranslateExpression
																									 (localDevicePlan,
																									  planNode.Nodes[1],
																									  false)
																							   : new ValueExpression(1)
																					   })
																),
															"iConcatenation",
															new ValueExpression("/")
															),
														"iConcatenation",
														new CallExpression("TO_CHAR",
																		   new[]
																			   {
																				   planNode.Nodes.Count > 2
																					   ? localDevicePlan.Device.
																							 TranslateExpression(
																							 localDevicePlan,
																							 planNode.Nodes[2], false)
																					   : new ValueExpression(1)
																			   })
														),
													"iConcatenation",
													new ValueExpression(" ")
													),
												"iConcatenation",
												new CallExpression("TO_CHAR",
																   new[]
																	   {
																		   planNode.Nodes.Count > 3
																			   ? localDevicePlan.Device.TranslateExpression(
																					 localDevicePlan, planNode.Nodes[3],
																					 false)
																			   : new ValueExpression(12)
																	   })
												),
											"iConcatenation",
											new ValueExpression(":")
											),
										"iConcatenation",
										new CallExpression("TO_CHAR",
														   new[]
															   {
																   planNode.Nodes.Count > 4
																	   ? localDevicePlan.Device.TranslateExpression(
																			 localDevicePlan, planNode.Nodes[4], false)
																	   : new ValueExpression(0)
															   })
										),
									"iConcatenation",
									new ValueExpression(":")
									),
								"iConcatenation",
								new CallExpression("TO_CHAR",
												   new[]
													   {
														   planNode.Nodes.Count > 5
															   ? localDevicePlan.Device.TranslateExpression(localDevicePlan,
																										planNode.Nodes[
																											5], false)
															   : new ValueExpression(0)
													   })
								),
							new ValueExpression("yyyy/mm/dd hh24:mi:ss")
						}
					);
		}
	}

	public class OracleDateTimeToTimeSpan : DeviceOperator
	{
		public OracleDateTimeToTimeSpan(int iD, string name) : base(iD, name)
		{
		}

		//public OracleDateTimeToTimeSpan(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleDateTimeToTimeSpan(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := 631139040000000000 + ((ADateTime - TO_DATE('01-JAN-2001')) * 86400 + TO_CHAR(ADateTime, 'sssss')) * 10000000;
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					new ValueExpression(631139040000000000d, TokenType.Decimal),
					"iAddition",
					new BinaryExpression
						(
						new BinaryExpression
							(
							new BinaryExpression
								(
								new BinaryExpression
									(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									"iSubtraction",
									new CallExpression("TO_DATE", new Expression[] {new ValueExpression("01-JAN-2001")})
									),
								"iMultiplication",
								new ValueExpression(86400)
								),
							"iAddition",
							new CallExpression("TO_CHAR",
											   new[]
												   {
													   localDevicePlan.Device.TranslateExpression(localDevicePlan,
																							  planNode.Nodes[0], false)
													   , new ValueExpression("sssss")
												   })
							),
						"iMultiplication",
						new ValueExpression(10000000)
						)
					);
		}
	}

	public class OracleTimeSpanToDateTime : DeviceOperator
	{
		public OracleTimeSpanToDateTime(int iD, string name) : base(iD, name)
		{
		}

		//public OracleTimeSpanToDateTime(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public OracleTimeSpanToDateTime(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			//LReturnVal := TRUNC((ATimeSpan - 630822816000000000) / 864000000000) + TO_DATE(20000101 * 100000 + TRUNC(MOD((ATimeSpan / 10000000), 86400)), 'yyyy dd mm sssss');
			var localDevicePlan = (SQLDevicePlan) devicePlan;
			return
				new BinaryExpression
					(
					new CallExpression
						(
						"TRUNC",
						new Expression[]
							{
								new BinaryExpression
									(
									new BinaryExpression
										(
										localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
										"iSubtraction",
										new ValueExpression(630822816000000000d)
										),
									"iDivision",
									new ValueExpression(864000000000d)
									)
							}
						),
					"iAddition",
					new CallExpression
						(
						"TO_DATE",
						new Expression[]
							{
								new BinaryExpression
									(
									new BinaryExpression
										(
										new ValueExpression(20000101),
										"iMultiplication",
										new ValueExpression(10000)
										),
									"iAddition",
									new CallExpression
										(
										"TRUNC",
										new Expression[]
											{
												new CallExpression
													(
													"MOD",
													new Expression[]
														{
															new BinaryExpression
																(
																localDevicePlan.Device.TranslateExpression(localDevicePlan,
																									   planNode.Nodes[0
																										   ], false),
																"iDivision",
																new ValueExpression(10000000)
																),
															new ValueExpression(86400)
														}
													)
											}
										)
									),
								new ValueExpression("yyyy dd mm sssss")
							}
						)
					);
		}
	}

	public class OracleAggregateOperator : SQLAggregateOperator
	{
		public OracleAggregateOperator(int iD, string name) : base(iD, name) {}

		protected override AggregateCallExpression CreateAggregateCallExpression(SQLDevicePlan devicePlan, PlanNode planNode)
		{
			OracleAggregateCallExpression expression = new OracleAggregateCallExpression();
			expression.Identifier = OperatorName;
			return expression;
		}

		protected override AggregateCallExpression TranslateOrderDependentAggregateCallExpression(SQLDevicePlan devicePlan, PlanNode planNode, AggregateCallExpression expression)
		{
			expression = base.TranslateOrderDependentAggregateCallExpression(devicePlan, planNode, expression);
			AggregateCallNode node = (AggregateCallNode)planNode;
			TableVar sourceTableVar = ((TableNode)node.Nodes[0]).TableVar;
			OracleAggregateCallExpression ace = expression as OracleAggregateCallExpression;
			if (ace != null && node.Operator.IsOrderDependent)
			{
				OrderNode sourceNode = node.SourceNode as OrderNode;
				if (sourceNode == null)
				{
					devicePlan.IsSupported = false;
					devicePlan.TranslationMessages.Add(new TranslationMessage(String.Format("Aggregate operator {0} is not supported by this device because the operator is order dependent and the argument is not ordered.", node.Operator.Name, node)));
				}
				else
				{
					SelectStatement selectStatement = new SelectStatement();
					selectStatement = devicePlan.Device.TranslateOrder(devicePlan, sourceNode, selectStatement, true);
					ace.OrderClause = selectStatement.OrderClause;
				}
			}

			return expression;
		}
	}
}