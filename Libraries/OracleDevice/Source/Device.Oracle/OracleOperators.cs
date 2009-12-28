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

namespace Alphora.Dataphor.DAE.Device.Oracle
{
    
    public class OracleRetrieve : SQLDeviceOperator
    {
        public OracleRetrieve(int AID, string AName) : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            TableVar LTableVar = ((TableVarNode) APlanNode).TableVar;

            if (LTableVar is BaseTableVar)
            {
                var LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
                foreach (TableVarColumn LColumn in LTableVar.Columns)
                    LRangeVar.Columns.Add(new SQLRangeVarColumn(LColumn, LRangeVar.Name,
                                                                LDevicePlan.Device.ToSQLIdentifier(LColumn),
                                                                LDevicePlan.Device.ToSQLIdentifier(LColumn.Name)));
                LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
                var LSelectExpression = new SelectExpression();
                LSelectExpression.OptimizerHints = "FIRST_ROWS(20)";
                LSelectExpression.FromClause =
                    new AlgebraicFromClause
                        (
                        new TableSpecifier
                            (
                            new TableExpression
                                (
                                MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema),
                                LDevicePlan.Device.ToSQLIdentifier(LTableVar)
                                ),
                            LRangeVar.Name
                            )
                        );
                LSelectExpression.SelectClause = new SelectClause();
                foreach (TableVarColumn LColumn in LTableVar.Columns)
                    LSelectExpression.SelectClause.Columns.Add(
                        LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());

                LSelectExpression.SelectClause.Distinct =
                    (LTableVar.Keys.Count == 1) &&
                    Convert.ToBoolean(MetaData.GetTag(LTableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));

                return LSelectExpression;
            }
            else
                return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
        }
    }

    public class OracleJoin : SQLDeviceOperator
    {
        public OracleJoin(int AID, string AName) : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            var LJoinNode = (JoinNode) APlanNode;
            JoinType LJoinType;
            if ((((LJoinNode.Nodes[2] is ValueNode) &&
                  ((LJoinNode.Nodes[2]).DataType.Is(ADevicePlan.Plan.Catalog.DataTypes.SystemBoolean)) &&
                  ((bool) ((ValueNode) LJoinNode.Nodes[2]).Value))))
                LJoinType = JoinType.Cross;
            else if (LJoinNode is LeftOuterJoinNode)
                LJoinType = JoinType.Left;
            else if (LJoinNode is RightOuterJoinNode)
                LJoinType = JoinType.Right;
            else
                LJoinType = JoinType.Inner;

            bool LHasOuterColumnExpressions = false;

            LDevicePlan.PushQueryContext();
            Statement LLeftStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Language.SQL.SelectExpression LLeftSelectExpression =
                LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode) APlanNode.Nodes[0]).TableVar,
                                                               LLeftStatement, false);
            TableVar LLeftTableVar = ((TableNode) APlanNode.Nodes[0]).TableVar;
            for (int LIndex = 0; LIndex < LJoinNode.LeftKey.Columns.Count; LIndex++)
                if (LDevicePlan.GetRangeVarColumn(LJoinNode.LeftKey.Columns[LIndex].Name, true).Expression != null)
                {
                    LHasOuterColumnExpressions = true;
                    break;
                }

            if (LHasOuterColumnExpressions || LDevicePlan.CurrentQueryContext().IsAggregate ||
                LLeftSelectExpression.SelectClause.Distinct)
            {
                string LNestingReason = "The left argument to the join operator must be nested because ";
                if (LHasOuterColumnExpressions)
                    LNestingReason +=
                        "the join is to be performed on columns which are introduced as expressions in the current context.";
                else if (LDevicePlan.CurrentQueryContext().IsAggregate)
                    LNestingReason += "it contains aggregation.";
                else
                    LNestingReason += "it contains a distinct specification.";
                LDevicePlan.TranslationMessages.Add(new TranslationMessage(LNestingReason, APlanNode));
                LLeftStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LLeftTableVar, LLeftStatement);
                LLeftSelectExpression = LDevicePlan.Device.FindSelectExpression(LLeftStatement);
            }
            SQLQueryContext LLeftContext = LDevicePlan.CurrentQueryContext();
            LDevicePlan.PopQueryContext();

            LDevicePlan.PushQueryContext();
            Statement LRightStatement = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Language.SQL.SelectExpression LRightSelectExpression =
                LDevicePlan.Device.EnsureUnarySelectExpression(LDevicePlan, ((TableNode) APlanNode.Nodes[1]).TableVar,
                                                               LRightStatement, false);
            TableVar LRightTableVar = ((TableNode) APlanNode.Nodes[1]).TableVar;
            LHasOuterColumnExpressions = false;
            for (int LIndex = 0; LIndex < LJoinNode.RightKey.Columns.Count; LIndex++)
                if (LDevicePlan.GetRangeVarColumn(LJoinNode.RightKey.Columns[LIndex].Name, true).Expression != null)
                {
                    LHasOuterColumnExpressions = true;
                    break;
                }

            if (LHasOuterColumnExpressions || LDevicePlan.CurrentQueryContext().IsAggregate ||
                LRightSelectExpression.SelectClause.Distinct)
            {
                string LNestingReason = "The right argument to the join operator must be nested because ";
                if (LHasOuterColumnExpressions)
                    LNestingReason +=
                        "the join is to be performed on columns which are introduced as expressions in the current context.";
                else if (LDevicePlan.CurrentQueryContext().IsAggregate)
                    LNestingReason += "it contains aggregation.";
                else
                    LNestingReason += "it contains a distinct specification.";
                LDevicePlan.TranslationMessages.Add(new TranslationMessage(LNestingReason, APlanNode));
                LRightStatement = LDevicePlan.Device.NestQueryExpression(LDevicePlan, LRightTableVar, LRightStatement);
                LRightSelectExpression = LDevicePlan.Device.FindSelectExpression(LRightStatement);
            }
            SQLQueryContext LRightContext = LDevicePlan.CurrentQueryContext();
            LDevicePlan.PopQueryContext();

            // Merge the query contexts
            LDevicePlan.CurrentQueryContext().RangeVars.AddRange(LLeftContext.RangeVars);
            LDevicePlan.CurrentQueryContext().AddedColumns.AddRange(LLeftContext.AddedColumns);
            LDevicePlan.CurrentQueryContext().RangeVars.AddRange(LRightContext.RangeVars);
            LDevicePlan.CurrentQueryContext().AddedColumns.AddRange(LRightContext.AddedColumns);

            // Merge the from clauses
            var LLeftFromClause = (CalculusFromClause) LLeftSelectExpression.FromClause;
            var LRightFromClause = (CalculusFromClause) LRightSelectExpression.FromClause;
            foreach (TableSpecifier LTableSpecifier in LRightFromClause.TableSpecifiers)
                LLeftFromClause.TableSpecifiers.Add(LTableSpecifier);

            LDevicePlan.PushJoinContext(new SQLJoinContext(LLeftContext, LRightContext));
            try
            {
                if (LJoinType != JoinType.Cross)
                {
                    Expression LJoinCondition = null;

                    for (int LIndex = 0; LIndex < LJoinNode.LeftKey.Columns.Count; LIndex++)
                    {
                        SQLRangeVarColumn LLeftColumn =
                            LDevicePlan.CurrentJoinContext().LeftQueryContext.GetRangeVarColumn(
                                LJoinNode.LeftKey.Columns[LIndex].Name);
                        SQLRangeVarColumn LRightColumn =
                            LDevicePlan.CurrentJoinContext().RightQueryContext.GetRangeVarColumn(
                                LJoinNode.RightKey.Columns[LIndex].Name);
                        Expression LLeftExpression = LLeftColumn.GetExpression();
                        Expression LRightExpression = LRightColumn.GetExpression();
                        if (LJoinType == JoinType.Right)
                        {
                            var LFieldExpression = (QualifiedFieldExpression) LLeftExpression;
                            LLeftExpression = new OuterJoinFieldExpression(LFieldExpression.FieldName,
                                                                           LFieldExpression.TableAlias);
                        }
                        else if (LJoinType == JoinType.Left)
                        {
                            var LFieldExpression = (QualifiedFieldExpression) LRightExpression;
                            LRightExpression = new OuterJoinFieldExpression(LFieldExpression.FieldName,
                                                                            LFieldExpression.TableAlias);
                        }

                        Expression LEqualExpression =
                            new BinaryExpression
                                (
                                LLeftExpression,
                                "iEqual",
                                LRightExpression
                                );

                        if (LJoinCondition != null)
                            LJoinCondition = new BinaryExpression(LJoinCondition, "iAnd", LEqualExpression);
                        else
                            LJoinCondition = LEqualExpression;
                    }

                    if (LLeftSelectExpression.WhereClause == null)
                        LLeftSelectExpression.WhereClause = new WhereClause(LJoinCondition);
                    else
                        LLeftSelectExpression.WhereClause.Expression =
                            new BinaryExpression(LLeftSelectExpression.WhereClause.Expression, "iAnd", LJoinCondition);

                    var LOuterJoinNode = LJoinNode as OuterJoinNode;
                    if ((LOuterJoinNode != null) && (LOuterJoinNode.RowExistsColumnIndex >= 0))
                    {
                        TableVarColumn LRowExistsColumn =
                            LOuterJoinNode.TableVar.Columns[LOuterJoinNode.RowExistsColumnIndex];
                        var LCaseExpression = new CaseExpression();
                        var LCaseItem = new CaseItemExpression();
                        if (LOuterJoinNode is LeftOuterJoinNode)
                            LCaseItem.WhenExpression = new UnaryExpression("iIsNull",
                                                                           LDevicePlan.CurrentJoinContext().
                                                                               RightQueryContext.GetRangeVarColumn(
                                                                               LOuterJoinNode.RightKey.Columns[0].Name).
                                                                               GetExpression());
                        else
                            LCaseItem.WhenExpression = new UnaryExpression("iIsNull",
                                                                           LDevicePlan.CurrentJoinContext().
                                                                               LeftQueryContext.GetRangeVarColumn(
                                                                               LOuterJoinNode.LeftKey.Columns[0].Name).
                                                                               GetExpression());
                        LCaseItem.ThenExpression = new ValueExpression(0);
                        LCaseExpression.CaseItems.Add(LCaseItem);
                        LCaseExpression.ElseExpression = new CaseElseExpression(new ValueExpression(1));
                        var LRangeVarColumn = new SQLRangeVarColumn(LRowExistsColumn, LCaseExpression,
                                                                    LDevicePlan.Device.ToSQLIdentifier(LRowExistsColumn));
                        LDevicePlan.CurrentQueryContext().AddedColumns.Add(LRangeVarColumn);
                        LLeftSelectExpression.SelectClause.Columns.Add(LRangeVarColumn.GetColumnExpression());
                    }
                }

                // Build select clause
                LLeftSelectExpression.SelectClause = new SelectClause();
                foreach (TableVarColumn LColumn in ((TableNode) APlanNode).TableVar.Columns)
                    LLeftSelectExpression.SelectClause.Columns.Add(
                        LDevicePlan.GetRangeVarColumn(LColumn.Name, true).GetColumnExpression());

                // Merge where clauses
                if (LRightSelectExpression.WhereClause != null)
                    if (LLeftSelectExpression.WhereClause == null)
                        LLeftSelectExpression.WhereClause = LRightSelectExpression.WhereClause;
                    else
                        LLeftSelectExpression.WhereClause.Expression =
                            new BinaryExpression(LLeftSelectExpression.WhereClause.Expression, "iAnd",
                                                 LRightSelectExpression.WhereClause.Expression);

                return LLeftStatement;
            }
            finally
            {
                LDevicePlan.PopJoinContext();
            }
        }
    }


    public class OracleMathUtility
    {
        public static Expression Truncate(Expression AExpression)
        {
            return new CallExpression("TRUNC", new[] {AExpression, new ValueExpression(0)});
        }

        public static Expression Frac(Expression AExpression, Expression AExpressionCopy)
            // note that it takes two different refrences to the same value
        {
            return new BinaryExpression(AExpression, "iSubtraction", Truncate(AExpressionCopy));
        }
    }

    public class OracleTimeSpanUtility
    {
        //LReturnVal := TRUNC(DAE_Frac(ATimeSpan / 10000000) * 1000);
        public static Expression ReadMillisecond(Expression AValue)
        {
            return
                OracleMathUtility.Truncate
                    (
                    new BinaryExpression
                        (
                        OracleMathUtility.Frac
                            (
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000)),
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000))
                            ),
                        "iMultiplication",
                        new ValueExpression(1000)
                        )
                    );
        }

        public static Expression ReadSecond(Expression AValue)
        {
            //LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (10000000 * 60)) * 60);
            return
                OracleMathUtility.Truncate
                    (
                    new BinaryExpression
                        (
                        OracleMathUtility.Frac
                            (
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000)),
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000))
                            ),
                        "iMultiplication",
                        new ValueExpression(60)
                        )
                    );
        }

        public static Expression ReadMinute(Expression AValue)
        {
            //LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (600000000 * 60)) * 60);
            return
                OracleMathUtility.Truncate
                    (
                    new BinaryExpression
                        (
                        OracleMathUtility.Frac
                            (
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000)),
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000))
                            ),
                        "iMultiplication",
                        new ValueExpression(60)
                        )
                    );
        }

        public static Expression ReadHour(Expression AValue)
        {
            //LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (36000000000 * 24)) * 24);
            return
                OracleMathUtility.Truncate
                    (
                    new BinaryExpression
                        (
                        OracleMathUtility.Frac
                            (
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000)),
                            new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000))
                            ),
                        "iMultiplication",
                        new ValueExpression(24)
                        )
                    );
        }

        public static Expression ReadDay(Expression AValue)
        {
            //LReturnVal := TRUNC(ATimeSpan / 864000000000);
            return
                OracleMathUtility.Truncate(new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000)));
        }
    }

    public class OracleDateTimeFunctions
    {
        public static Expression WriteMonth(Expression ADateTime, Expression ADateTimeCopy, Expression APart)
        {
            string LPartString = "mm";
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {
                                                             new ValueExpression(LPartString, TokenType.Symbol),
                                                             ADateTimeCopy
                                                         });
            Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime});
        }

        public static Expression WriteDay(Expression ADateTime, Expression ADateTimeCopy, Expression APart)
            //pass the DateTime twice
        {
            string LPartString = "dd";
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {
                                                             new ValueExpression(LPartString, TokenType.Symbol),
                                                             ADateTimeCopy
                                                         });
            Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime});
        }

        public static Expression WriteYear(Expression ADateTime, Expression ADateTimeCopy, Expression APart)
            //pass the DateTime twice
        {
            string LPartString = "yyyy";
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {
                                                             new ValueExpression(LPartString, TokenType.Symbol),
                                                             ADateTimeCopy
                                                         });
            Expression LParts = new BinaryExpression(APart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime});
        }
    }

    public class OracleFrac : DeviceOperator
    {
        public OracleFrac(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleFrac(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleFrac(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                OracleMathUtility.Frac
                    (
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                    );
        }
    }

    // TimeSpan
    public class OracleTimeSpanReadMillisecond : DeviceOperator
    {
        public OracleTimeSpanReadMillisecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = ((SQLDevicePlan) ADevicePlan);
            return
                OracleTimeSpanUtility.ReadMillisecond
                    (
                    LDevicePlan.Device.TranslateExpression
                        (
                        LDevicePlan,
                        APlanNode.Nodes[0],
                        false
                        )
                    );
        }
    }

    public class OracleTimeSpanReadSecond : DeviceOperator
    {
        public OracleTimeSpanReadSecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                OracleTimeSpanUtility.ReadSecond(LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                        false));
        }
    }

    public class OracleTimeSpanReadMinute : DeviceOperator
    {
        public OracleTimeSpanReadMinute(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                OracleTimeSpanUtility.ReadMinute(LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                        false));
        }
    }

    public class OracleTimeSpanReadHour : DeviceOperator
    {
        public OracleTimeSpanReadHour(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                OracleTimeSpanUtility.ReadHour(LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                      false));
        }
    }

    public class OracleTimeSpanReadDay : DeviceOperator
    {
        public OracleTimeSpanReadDay(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                OracleTimeSpanUtility.ReadDay(LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false));
        }
    }

    public class OracleTimeSpanWriteMillisecond : DeviceOperator
    {
        public OracleTimeSpanWriteMillisecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := ATimeSpan + (APart - ReadMillisecond(ATimeSpan)) * 10000;
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                new BinaryExpression
                    (
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                    "iAddition",
                    new BinaryExpression
                        (
                        new BinaryExpression
                            (
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                            "iSubtraction",
                            OracleTimeSpanUtility.ReadMillisecond(LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                         APlanNode.Nodes
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
        public OracleTimeSpanWriteSecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := ATimeSpan + (APart - ReadSecond(ATimeSpan)) * 10000000;
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                new BinaryExpression
                    (
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                    "iAddition",
                    new BinaryExpression
                        (
                        new BinaryExpression
                            (
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                            "iSubtraction",
                            OracleTimeSpanUtility.ReadSecond(LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                    APlanNode.Nodes[0],
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
        public OracleTimeSpanWriteMinute(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := ATimeSpan + (APart - ReadMinute(ATimeSpan)) * 600000000;
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                new BinaryExpression
                    (
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                    "iAddition",
                    new BinaryExpression
                        (
                        new BinaryExpression
                            (
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                            "iSubtraction",
                            OracleTimeSpanUtility.ReadMinute(LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                    APlanNode.Nodes[0],
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
        public OracleTimeSpanWriteHour(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := ATimeSpan + (APart - ReadHour(ATimeSpan)) * 36000000000;
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                new BinaryExpression
                    (
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                    "iAddition",
                    new BinaryExpression
                        (
                        new BinaryExpression
                            (
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                            "iSubtraction",
                            OracleTimeSpanUtility.ReadHour(LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleTimeSpanWriteDay(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := ATimeSpan + (APart - ReadDay(ATimeSpan)) * 864000000000;
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                new BinaryExpression
                    (
                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                    "iAddition",
                    new BinaryExpression
                        (
                        new BinaryExpression
                            (
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                            "iSubtraction",
                            OracleTimeSpanUtility.ReadDay(LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                 APlanNode.Nodes[0],
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
        public OracleAddYears(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleAddYears(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleAddYears(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := ADD_MONTHS(ADateTime, AYears * 12);
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return
                new CallExpression
                    (
                    "ADD_MONTHS",
                    new[]
                        {
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                            new BinaryExpression
                                (
                                LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                                "iMultiplication",
                                new ValueExpression(12)
                                )
                        }
                    );
        }
    }

    public class OracleDayOfWeek : DeviceOperator
    {
        public OracleDayOfWeek(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDayOfWeek(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDayOfWeek(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'd');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("d")
                                          });
        }
    }

    public class OracleDayOfYear : DeviceOperator
    {
        public OracleDayOfYear(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDayOfYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDayOfYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'ddd');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("ddd")
                                          });
        }
    }

    public class OracleDateTimeReadYear : DeviceOperator
    {
        public OracleDateTimeReadYear(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'yyyy');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("yyyy")
                                          });
        }
    }

    public class OracleDateTimeReadMonth : DeviceOperator
    {
        public OracleDateTimeReadMonth(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadMonth(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadMonth(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'mm');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("mm")
                                          });
        }
    }

    public class OracleDateTimeReadDay : DeviceOperator
    {
        public OracleDateTimeReadDay(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'dd');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("dd")
                                          });
        }
    }

    public class OracleDateTimeReadHour : DeviceOperator
    {
        public OracleDateTimeReadHour(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'hh');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("hh")
                                          });
        }
    }

    public class OracleDateTimeReadMinute : DeviceOperator
    {
        public OracleDateTimeReadMinute(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'mi');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("mi")
                                          });
        }
    }

    public class OracleDateTimeReadSecond : DeviceOperator
    {
        public OracleDateTimeReadSecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Char(ADateTime, 'ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return new CallExpression("TO_CHAR",
                                      new[]
                                          {
                                              LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0],
                                                                                     false), new ValueExpression("ss")
                                          });
        }
    }

    public class OracleDateTimeReadMillisecond : DeviceOperator
    {
        public OracleDateTimeReadMillisecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeReadMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            return new ValueExpression(0);
        }
    }

    public class OracleDateTimeWriteMillisecond : DeviceOperator
    {
        public OracleDateTimeWriteMillisecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteMillisecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            return LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
        }
    }

    public class OracleDateTimeWriteSecond : DeviceOperator
    {
        public OracleDateTimeWriteSecond(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteSecond(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(APart), 'yyyy/mm/dd hh24:mi:ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[0],
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
                                                                                           LDevicePlan.Device.
                                                                                               TranslateExpression(
                                                                                               LDevicePlan,
                                                                                               APlanNode.Nodes[0], false)
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
                                                                                   LDevicePlan.Device.
                                                                                       TranslateExpression(LDevicePlan,
                                                                                                           APlanNode.
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
                                                                           LDevicePlan.Device.TranslateExpression(
                                                                               LDevicePlan, APlanNode.Nodes[0], false),
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
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[1],
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
        public OracleDateTimeWriteMinute(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteMinute(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(APart) || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[0],
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
                                                                                           LDevicePlan.Device.
                                                                                               TranslateExpression(
                                                                                               LDevicePlan,
                                                                                               APlanNode.Nodes[0], false)
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
                                                                                   LDevicePlan.Device.
                                                                                       TranslateExpression(LDevicePlan,
                                                                                                           APlanNode.
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
                                                                           LDevicePlan.Device.TranslateExpression(
                                                                               LDevicePlan, APlanNode.Nodes[0], false),
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
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeWriteHour(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteHour(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(APart) || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[0],
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
                                                                                           LDevicePlan.Device.
                                                                                               TranslateExpression(
                                                                                               LDevicePlan,
                                                                                               APlanNode.Nodes[0], false)
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
                                                                                   LDevicePlan.Device.
                                                                                       TranslateExpression(LDevicePlan,
                                                                                                           APlanNode.
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
                                                                           LDevicePlan.Device.TranslateExpression(
                                                                               LDevicePlan, APlanNode.Nodes[1], false)
                                                                       })
                                                ),
                                            "iConcatenation",
                                            new ValueExpression(":")
                                            ),
                                        "iConcatenation",
                                        new CallExpression("TO_CHAR",
                                                           new[]
                                                               {
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeWriteDay(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteDay(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(APart) || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[0],
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
                                                                                           LDevicePlan.Device.
                                                                                               TranslateExpression(
                                                                                               LDevicePlan,
                                                                                               APlanNode.Nodes[0], false)
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
                                                                                   LDevicePlan.Device.
                                                                                       TranslateExpression(LDevicePlan,
                                                                                                           APlanNode.
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
                                                                           LDevicePlan.Device.TranslateExpression(
                                                                               LDevicePlan, APlanNode.Nodes[0], false),
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
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeWriteMonth(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteMonth(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteMonth(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Date(To_Char(ADateTime,'yyyy') || '/' || To_Char(APart) || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[0],
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
                                                                                           LDevicePlan.Device.
                                                                                               TranslateExpression(
                                                                                               LDevicePlan,
                                                                                               APlanNode.Nodes[1], false)
                                                                                       })
                                                                ),
                                                            "iConcatenation",
                                                            new ValueExpression("/")
                                                            ),
                                                        "iConcatenation",
                                                        new CallExpression("TO_CHAR",
                                                                           new[]
                                                                               {
                                                                                   LDevicePlan.Device.
                                                                                       TranslateExpression(LDevicePlan,
                                                                                                           APlanNode.
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
                                                                           LDevicePlan.Device.TranslateExpression(
                                                                               LDevicePlan, APlanNode.Nodes[0], false),
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
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeWriteYear(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeWriteYear(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeWriteYear(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := To_Date(To_Char(APart) || '/' || To_Char(ADateTime,'mm') || '/' || To_Char(ADateTime,'dd') || ' ' || To_Char(ADateTime,'hh24') || ':' || To_Char(ADateTime,'mi') || ':' || to_Char(ADateTime,'ss'), 'yyyy/mm/dd hh24:mi:ss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[1],
                                                                                                   false)
                                                                                           }),
                                                                    "iConcatenation",
                                                                    new ValueExpression("/")
                                                                    ),
                                                                "iConcatenation",
                                                                new CallExpression("TO_CHAR",
                                                                                   new[]
                                                                                       {
                                                                                           LDevicePlan.Device.
                                                                                               TranslateExpression(
                                                                                               LDevicePlan,
                                                                                               APlanNode.Nodes[0], false)
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
                                                                                   LDevicePlan.Device.
                                                                                       TranslateExpression(LDevicePlan,
                                                                                                           APlanNode.
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
                                                                           LDevicePlan.Device.TranslateExpression(
                                                                               LDevicePlan, APlanNode.Nodes[0], false),
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
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeDatePart(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeDatePart(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeDatePart(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnValue := TO_DATE(TO_CHAR(ADateTime, 'yyyy') || '/' || TO_CHAR(ADateTime, 'mm') || '/' TO_CHAR(ADateTime, 'dd'), "yyyy/mm/dd");
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                       LDevicePlan.Device.TranslateExpression(
                                                                           LDevicePlan, APlanNode.Nodes[0], false),
                                                                       new ValueExpression("yyyy")
                                                                   }),
                                            "iConcatenation",
                                            new ValueExpression("/")
                                            ),
                                        "iConcatenation",
                                        new CallExpression("TO_CHAR",
                                                           new[]
                                                               {
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeTimePart(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeTimePart(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeTimePart(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnValue := TO_DATE(TO_CHAR(ADateTime, 'hh24') || '/' || TO_CHAR(ADateTime, 'mi') || '/' TO_CHAR(ADateTime, 'ss'), "hh24:mi:ss");
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                       LDevicePlan.Device.TranslateExpression(
                                                                           LDevicePlan, APlanNode.Nodes[0], false),
                                                                       new ValueExpression("hh24")
                                                                   }),
                                            "iConcatenation",
                                            new ValueExpression(":")
                                            ),
                                        "iConcatenation",
                                        new CallExpression("TO_CHAR",
                                                           new[]
                                                               {
                                                                   LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                          APlanNode.
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
                                                           LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                  APlanNode.Nodes[0],
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
        public OracleDateTimeSelector(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeSelector(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeSelector(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            // TO_DATE(TO_CHAR(AYears) || "/" || TO_CHAR(AMonths) || "/" || TO_CHAR(ADays) || " " || TO_CHAR(AHours) || ":" || TO_CHAR(AMinutes) ":" || TO_CHAR(ASeconds), "yyyy/mm/dd hh24:mi:ss")
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                                                                               LDevicePlan.Device.
                                                                                                   TranslateExpression(
                                                                                                   LDevicePlan,
                                                                                                   APlanNode.Nodes[0],
                                                                                                   false)
                                                                                           }),
                                                                    "iConcatenation",
                                                                    new ValueExpression("/")
                                                                    ),
                                                                "iConcatenation",
                                                                new CallExpression("TO_CHAR",
                                                                                   new[]
                                                                                       {
                                                                                           APlanNode.Nodes.Count > 1
                                                                                               ? LDevicePlan.Device.
                                                                                                     TranslateExpression
                                                                                                     (LDevicePlan,
                                                                                                      APlanNode.Nodes[1],
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
                                                                                   APlanNode.Nodes.Count > 2
                                                                                       ? LDevicePlan.Device.
                                                                                             TranslateExpression(
                                                                                             LDevicePlan,
                                                                                             APlanNode.Nodes[2], false)
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
                                                                           APlanNode.Nodes.Count > 3
                                                                               ? LDevicePlan.Device.TranslateExpression(
                                                                                     LDevicePlan, APlanNode.Nodes[3],
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
                                                                   APlanNode.Nodes.Count > 4
                                                                       ? LDevicePlan.Device.TranslateExpression(
                                                                             LDevicePlan, APlanNode.Nodes[4], false)
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
                                                           APlanNode.Nodes.Count > 5
                                                               ? LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                        APlanNode.Nodes[
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
        public OracleDateTimeToTimeSpan(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleDateTimeToTimeSpan(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleDateTimeToTimeSpan(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := 631139040000000000 + ((ADateTime - TO_DATE('01-JAN-2001')) * 86400 + TO_CHAR(ADateTime, 'sssss')) * 10000000;
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
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
                                                       LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                              APlanNode.Nodes[0], false)
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
        public OracleTimeSpanToDateTime(int AID, string AName) : base(AID, AName)
        {
        }

        //public OracleTimeSpanToDateTime(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
        //public OracleTimeSpanToDateTime(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            //LReturnVal := TRUNC((ATimeSpan - 630822816000000000) / 864000000000) + TO_DATE(20000101 * 100000 + TRUNC(MOD((ATimeSpan / 10000000), 86400)), 'yyyy dd mm sssss');
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
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
                                                                LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                       APlanNode.Nodes[0
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
}