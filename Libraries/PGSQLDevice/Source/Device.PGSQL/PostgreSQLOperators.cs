using System;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
//using TableExpression = Alphora.Dataphor.DAE.Language.PGSQL.TableExpression;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Device.PGSQL
{
    public class PostgreSQLRetrieve : SQLRetrieve
    {
        public PostgreSQLRetrieve(int AID, string AName) : base(AID, AName) { }
		
		public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
		{
            SQLDevicePlan LDevicePlan = (SQLDevicePlan)ADevicePlan;
            TableVar LTableVar = ((TableVarNode)APlanNode).TableVar;

            if (LTableVar is BaseTableVar)
            {
                SQLRangeVar LRangeVar = new SQLRangeVar(LDevicePlan.GetNextTableAlias());
                LDevicePlan.CurrentQueryContext().RangeVars.Add(LRangeVar);
                var LSelectExpression = new SelectExpression();
                string LSQLIdentifier = LDevicePlan.Device.ToSQLIdentifier(LTableVar);
                string LTag = D4.MetaData.GetTag(LTableVar.MetaData, "Storage.Schema", LDevicePlan.Device.Schema);
                var LTableExpression = new TableExpression(LTag,LSQLIdentifier);
                var LTableSpecifier = new TableSpecifier(LTableExpression, LRangeVar.Name);
                LSelectExpression.FromClause = new AlgebraicFromClause(LTableSpecifier);
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
		   // return base.Translate(ADevicePlan, APlanNode);
		}
 
    }

    public class PostgreSQLToday : SQLDeviceOperator
    {
        public PostgreSQLToday(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            return new CallExpression("Round",
                                      new Expression[]
                                          {
                                              new CallExpression("GetDate", new Expression[] {}), new ValueExpression(0)
                                              ,
                                              new ValueExpression(1)
                                          });
        }
    }

    public class PostgreSQLSubString : SQLDeviceOperator
    {
        public PostgreSQLSubString(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Substring",
                    new[]
                        {
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                            new BinaryExpression
                                (
                                LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                                "iAddition",
                                new ValueExpression(1, TokenType.Integer)
                                ),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[2], false)
                        }
                    );
        }
    }

    // Pos(ASubString, AString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
    public class PostgreSQLPos : SQLDeviceOperator
    {
        public PostgreSQLPos(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new BinaryExpression
                    (
                    new CaseExpression
                        (
                        new[]
                            {
                                new CaseItemExpression
                                    (
                                    new BinaryExpression
                                        (
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                        "iEqual",
                                        new ValueExpression(String.Empty, TokenType.String)
                                        ),
                                    new ValueExpression(1, TokenType.Integer)
                                    )
                            },
                        new CaseElseExpression
                            (
                            new CallExpression
                                (
                                "CharIndex",
                                new[]
                                    {
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
                                    }
                                )
                            )
                        ),
                    "iSubtraction",
                    new ValueExpression(1, TokenType.Integer)
                    );
        }
    }

    // IndexOf(AString, ASubString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
    public class PostgreSQLIndexOf : SQLDeviceOperator
    {
        public PostgreSQLIndexOf(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new BinaryExpression
                    (
                    new CaseExpression
                        (
                        new[]
                            {
                                new CaseItemExpression
                                    (
                                    new BinaryExpression
                                        (
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                                        "iEqual",
                                        new ValueExpression(String.Empty, TokenType.String)
                                        ),
                                    new ValueExpression(1, TokenType.Integer)
                                    )
                            },
                        new CaseElseExpression
                            (
                            new CallExpression
                                (
                                "CharIndex",
                                new[]
                                    {
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false),
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                                    }
                                )
                            )
                        ),
                    "iSubtraction",
                    new ValueExpression(1, TokenType.Integer)
                    );
        }
    }

    // CompareText(ALeftValue, ARightValue) ::= case when Upper(ALeftValue) = Upper(ARightValue) then 0 when Upper(ALeftValue) < Upper(ARightValue) then -1 else 1 end
    public class PostgreSQLCompareText : SQLDeviceOperator
    {
        public PostgreSQLCompareText(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CaseExpression
                    (
                    new[]
                        {
                            new CaseItemExpression
                                (
                                new BinaryExpression
                                    (
                                    new CallExpression("Upper",
                                                       new[]
                                                           {
                                                               LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                      APlanNode.Nodes[0],
                                                                                                      false)
                                                           }),
                                    "iEqual",
                                    new CallExpression("Upper",
                                                       new[]
                                                           {
                                                               LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                      APlanNode.Nodes[1],
                                                                                                      false)
                                                           })
                                    ),
                                new ValueExpression(0)
                                ),
                            new CaseItemExpression
                                (
                                new BinaryExpression
                                    (
                                    new CallExpression("Upper",
                                                       new[]
                                                           {
                                                               LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                      APlanNode.Nodes[0],
                                                                                                      false)
                                                           }),
                                    "iLess",
                                    new CallExpression("Upper",
                                                       new[]
                                                           {
                                                               LDevicePlan.Device.TranslateExpression(LDevicePlan,
                                                                                                      APlanNode.Nodes[1],
                                                                                                      false)
                                                           })
                                    ),
                                new ValueExpression(-1)
                                )
                        },
                    new CaseElseExpression(new ValueExpression(1))
                    );
        }
    }


    // ToString(AValue) ::= Convert(varchar, AValue)
    public class PostgreSQLToString : SQLDeviceOperator
    {
        public PostgreSQLToString(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("varchar"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    public class PostgreSQLToBit : SQLDeviceOperator
    {
        public PostgreSQLToBit(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("bit"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    public class PostgreSQLToTinyInt : SQLDeviceOperator
    {
        public PostgreSQLToTinyInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("tinyint"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    // ToByte(AValue) ::= convert(tinyint, AValue & (power(2, 8) - 1))	
    public class PostgreSQLToByte : SQLDeviceOperator
    {
        public PostgreSQLToByte(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("tinyint"),
                            new BinaryExpression
                                (
                                LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                "iBitwiseAnd",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new ValueExpression(2, TokenType.Integer),
                                                new ValueExpression(8, TokenType.Integer)
                                            }
                                        ),
                                    "iSubtraction",
                                    new ValueExpression(1, TokenType.Integer)
                                    )
                                )
                        }
                    );
        }
    }

    public class PostgreSQLToSmallInt : SQLDeviceOperator
    {
        public PostgreSQLToSmallInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("smallint"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    // ToSByte(AValue) ::= convert(smallint, ((AValue & (power(2, 8) - 1) & ~power(2, 7)) - (power(2, 7) & AValue)))
    public class PostgreSQLToSByte : SQLDeviceOperator
    {
        public PostgreSQLToSByte(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("smallint"),
                            new BinaryExpression
                                (
                                new BinaryExpression
                                    (
                                    new BinaryExpression
                                        (
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                        "iBitwiseAnd",
                                        new BinaryExpression
                                            (
                                            new CallExpression
                                                (
                                                "Power",
                                                new Expression[]
                                                    {
                                                        new ValueExpression(2, TokenType.Integer),
                                                        new ValueExpression(8, TokenType.Integer)
                                                    }
                                                ),
                                            "iSubtraction",
                                            new ValueExpression(1, TokenType.Integer)
                                            )
                                        ),
                                    "iBitwiseAnd",
                                    new UnaryExpression
                                        (
                                        "iBitwiseNot",
                                        new CallExpression
                                            (
                                            "Power",
                                            new Expression[]
                                                {
                                                    new ValueExpression(2, TokenType.Integer),
                                                    new ValueExpression(7, TokenType.Integer)
                                                }
                                            )
                                        )
                                    ),
                                "iSubtraction",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new ValueExpression(2, TokenType.Integer),
                                                new ValueExpression(7, TokenType.Integer)
                                            }
                                        ),
                                    "iBitwiseAnd",
                                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                                    )
                                )
                        }
                    );
        }
    }

    // ToShort(AValue) ::= convert(smallint, ((AValue & (power(2, 16) - 1) & ~power(2, 15)) - (power(2, 15) & AValue)))
    public class PostgreSQLToShort : SQLDeviceOperator
    {
        public PostgreSQLToShort(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("smallint"),
                            new BinaryExpression
                                (
                                new BinaryExpression
                                    (
                                    new BinaryExpression
                                        (
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                        "iBitwiseAnd",
                                        new BinaryExpression
                                            (
                                            new CallExpression
                                                (
                                                "Power",
                                                new Expression[]
                                                    {
                                                        new ValueExpression(2, TokenType.Integer),
                                                        new ValueExpression(16, TokenType.Integer)
                                                    }
                                                ),
                                            "iSubtraction",
                                            new ValueExpression(1, TokenType.Integer)
                                            )
                                        ),
                                    "iBitwiseAnd",
                                    new UnaryExpression
                                        (
                                        "iBitwiseNot",
                                        new CallExpression
                                            (
                                            "Power",
                                            new Expression[]
                                                {
                                                    new ValueExpression(2, TokenType.Integer),
                                                    new ValueExpression(15, TokenType.Integer)
                                                }
                                            )
                                        )
                                    ),
                                "iSubtraction",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new ValueExpression(2, TokenType.Integer),
                                                new ValueExpression(15, TokenType.Integer)
                                            }
                                        ),
                                    "iBitwiseAnd",
                                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                                    )
                                )
                        }
                    );
        }
    }

    public class PostgreSQLToInt : SQLDeviceOperator
    {
        public PostgreSQLToInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("int"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    // ToUShort(AValue) ::= convert(int, AValue & (power(2, 16) - 1))	
    public class PostgreSQLToUShort : SQLDeviceOperator
    {
        public PostgreSQLToUShort(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("int"),
                            new BinaryExpression
                                (
                                LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                "iBitwiseAnd",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new ValueExpression(2, TokenType.Integer),
                                                new ValueExpression(16, TokenType.Integer)
                                            }
                                        ),
                                    "iSubtraction",
                                    new ValueExpression(1, TokenType.Integer)
                                    )
                                )
                        }
                    );
        }
    }

    // ToInteger(AValue) ::= convert(int, ((AValue & ((power(convert(bigint, 2), 32) - 1) & ~(power(convert(bigint, 2), 31)) - (power(convert(bigint, 2), 31) & AValue)))
    public class PostgreSQLToInteger : SQLDeviceOperator
    {
        public PostgreSQLToInteger(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("int"),
                            new BinaryExpression
                                (
                                new BinaryExpression
                                    (
                                    new BinaryExpression
                                        (
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                        "iBitwiseAnd",
                                        new BinaryExpression
                                            (
                                            new CallExpression
                                                (
                                                "Power",
                                                new Expression[]
                                                    {
                                                        new CallExpression
                                                            (
                                                            "Convert",
                                                            new Expression[]
                                                                {
                                                                    new IdentifierExpression("bigint"),
                                                                    new ValueExpression(2, TokenType.Integer),
                                                                }
                                                            ),
                                                        new ValueExpression(32, TokenType.Integer)
                                                    }
                                                ),
                                            "iSubtraction",
                                            new ValueExpression(1, TokenType.Integer)
                                            )
                                        ),
                                    "iBitwiseAnd",
                                    new UnaryExpression
                                        (
                                        "iBitwiseNot",
                                        new CallExpression
                                            (
                                            "Power",
                                            new Expression[]
                                                {
                                                    new CallExpression
                                                        (
                                                        "Convert",
                                                        new Expression[]
                                                            {
                                                                new IdentifierExpression("bigint"),
                                                                new ValueExpression(2, TokenType.Integer)
                                                            }
                                                        ),
                                                    new ValueExpression(31, TokenType.Integer)
                                                }
                                            )
                                        )
                                    ),
                                "iSubtraction",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new CallExpression
                                                    (
                                                    "Convert",
                                                    new Expression[]
                                                        {
                                                            new IdentifierExpression("bigint"),
                                                            new ValueExpression(2, TokenType.Integer)
                                                        }
                                                    ),
                                                new ValueExpression(31, TokenType.Integer)
                                            }
                                        ),
                                    "iBitwiseAnd",
                                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                                    )
                                )
                        }
                    );
        }
    }

    public class PostgreSQLToBigInt : SQLDeviceOperator
    {
        public PostgreSQLToBigInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("bigint"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    // ToUInteger(AValue) ::= convert(bigint, AValue & (power(convert(bigint, 2), 32) - 1))	
    public class PostgreSQLToUInteger : SQLDeviceOperator
    {
        public PostgreSQLToUInteger(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("bigint"),
                            new BinaryExpression
                                (
                                LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                "iBitwiseAnd",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new CallExpression
                                                    (
                                                    "Convert",
                                                    new Expression[]
                                                        {
                                                            new IdentifierExpression("bigint"),
                                                            new ValueExpression(2, TokenType.Integer)
                                                        }
                                                    ),
                                                new ValueExpression(32, TokenType.Integer)
                                            }
                                        ),
                                    "iSubtraction",
                                    new ValueExpression(1, TokenType.Integer)
                                    )
                                )
                        }
                    );
        }
    }

    // ToLong(AValue) ::= convert(bigint, ((AValue & ((power(2, 64) * 1) - 1) & ~power(2, 63)) - (power(2, 63) & AValue)))
    public class PostgreSQLToLong : SQLDeviceOperator
    {
        public PostgreSQLToLong(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("bigint"),
                            new BinaryExpression
                                (
                                new BinaryExpression
                                    (
                                    new BinaryExpression
                                        (
                                        LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                        "iBitwiseAnd",
                                        new BinaryExpression
                                            (
                                            new BinaryExpression
                                                (
                                                new CallExpression
                                                    (
                                                    "Power",
                                                    new Expression[]
                                                        {
                                                            new ValueExpression(2, TokenType.Integer),
                                                            new ValueExpression(64, TokenType.Integer)
                                                        }
                                                    ),
                                                "iMultiplication",
                                                new ValueExpression(1, TokenType.Integer)
                                                ),
                                            "iSubtraction",
                                            new ValueExpression(1, TokenType.Integer)
                                            )
                                        ),
                                    "iBitwiseAnd",
                                    new UnaryExpression
                                        (
                                        "iBitwiseNot",
                                        new CallExpression
                                            (
                                            "Power",
                                            new Expression[]
                                                {
                                                    new ValueExpression(2, TokenType.Integer),
                                                    new ValueExpression(63, TokenType.Integer)
                                                }
                                            )
                                        )
                                    ),
                                "iSubtraction",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new ValueExpression(2, TokenType.Integer),
                                                new ValueExpression(63, TokenType.Integer)
                                            }
                                        ),
                                    "iBitwiseAnd",
                                    LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                                    )
                                )
                        }
                    );
        }
    }

    public class PostgreSQLToDecimal20 : SQLDeviceOperator
    {
        public PostgreSQLToDecimal20(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("decimal(20, 0)"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    public class PostgreSQLToDecimal288 : SQLDeviceOperator
    {
        public PostgreSQLToDecimal288(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("decimal(28, 8)"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    // ToULong(AValue) ::= convert(decimal(20, 0), AValue & (power(2, 64) - 1))	
    public class PostgreSQLToULong : SQLDeviceOperator
    {
        public PostgreSQLToULong(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new Expression[]
                        {
                            new IdentifierExpression("decimal(20, 0)"),
                            new BinaryExpression
                                (
                                LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                                "iBitwiseAnd",
                                new BinaryExpression
                                    (
                                    new CallExpression
                                        (
                                        "Power",
                                        new Expression[]
                                            {
                                                new ValueExpression(2, TokenType.Integer),
                                                new ValueExpression(64, TokenType.Integer)
                                            }
                                        ),
                                    "iSubtraction",
                                    new ValueExpression(1, TokenType.Integer)
                                    )
                                )
                        }
                    );
        }
    }

    public class PostgreSQLToDecimal : SQLDeviceOperator
    {
        public PostgreSQLToDecimal(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("decimal"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    public class PostgreSQLToMoney : SQLDeviceOperator
    {
        public PostgreSQLToMoney(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("money"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    public class PostgreSQLToUniqueIdentifier : SQLDeviceOperator
    {
        public PostgreSQLToUniqueIdentifier(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Convert",
                    new[]
                        {
                            new IdentifierExpression("uniqueidentifier"),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false)
                        }
                    );
        }
    }

    // Class to put all of the static math operators that will be reused.
    public class PostgreSQLMath
    {
        public static Expression Truncate(Expression AExpression)
        {
            return new CallExpression("Round", new[] { AExpression, new ValueExpression(0), new ValueExpression(1) });
        }

        public static Expression Frac(Expression AExpression, Expression AExpressionCopy)
        // note that it takes two different refrences to the same value
        {
            Expression LRounded = new CallExpression("Round",
                                                     new[]
                                                         {
                                                             AExpressionCopy, new ValueExpression(0),
                                                             new ValueExpression(1)
                                                         });
            return new BinaryExpression(AExpression, "iSubtraction", LRounded);
        }
    }

    public class PostgreSQLTimeSpan
    {
        public static Expression ReadMillisecond(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
            Expression LFromFrac = PostgreSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(1000));
            return PostgreSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadSecond(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
            Expression LFromFrac = PostgreSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
            return PostgreSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadMinute(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
            Expression LFromFrac = PostgreSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
            return PostgreSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadHour(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
            Expression LFromFrac = PostgreSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(24));
            return PostgreSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadDay(Expression AValue)
        {
            Expression LToTrunc = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
            return PostgreSQLMath.Truncate(LToTrunc);
        }
    }

    public class PostgreSQLDateTimeFunctions
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
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime });
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
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime });
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
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, ADateTime });
        }
    }

    // Operators that PostgreSQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.

    // Math
    public class PostgreSQLPower : SQLDeviceOperator
    {
        public PostgreSQLPower(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            return
                new CallExpression
                    (
                    "Power",
                    new[]
                        {
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false),
                            LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false)
                        }
                    );
        }
    }

    public class PostgreSQLTruncate : SQLDeviceOperator
    {
        public PostgreSQLTruncate(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLMath.Truncate(LValue);
        }
    }

    public class PostgreSQLFrac : SQLDeviceOperator
    {
        public PostgreSQLFrac(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LValueCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLMath.Frac(LValue, LValueCopy);
        }
    }

    public class PostgreSQLLogB : SQLDeviceOperator
    {
        public PostgreSQLLogB(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LBase = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            LValue = new CallExpression("Log", new[] { LValue });
            LBase = new CallExpression("Log", new[] { LBase });
            return new BinaryExpression(LValue, "iDivision", LBase);
        }
    }

    // TimeSpan
    public class PostgreSQLTimeSpanReadMillisecond : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanReadMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLTimeSpan.ReadMillisecond(LValue);
        }
    }

    public class PostgreSQLTimeSpanReadSecond : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanReadSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLTimeSpan.ReadSecond(LValue);
        }
    }

    public class PostgreSQLTimeSpanReadMinute : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanReadMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLTimeSpan.ReadMinute(LValue);
        }
    }

    public class PostgreSQLTimeSpanReadHour : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanReadHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLTimeSpan.ReadHour(LValue);
        }
    }

    public class PostgreSQLTimeSpanReadDay : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanReadDay(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return PostgreSQLTimeSpan.ReadDay(LValue);
        }
    }

    public class PostgreSQLTimeSpanWriteMillisecond : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanWriteMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = PostgreSQLTimeSpan.ReadMillisecond(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class PostgreSQLTimeSpanWriteSecond : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanWriteSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = PostgreSQLTimeSpan.ReadSecond(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class PostgreSQLTimeSpanWriteMinute : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanWriteMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = PostgreSQLTimeSpan.ReadMinute(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(600000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class PostgreSQLTimeSpanWriteHour : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanWriteHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = PostgreSQLTimeSpan.ReadHour(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(36000000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class PostgreSQLTimeSpanWriteDay : SQLDeviceOperator
    {
        public PostgreSQLTimeSpanWriteDay(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = PostgreSQLTimeSpan.ReadDay(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(864000000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }


    public class PostgreSQLAddMonths : SQLDeviceOperator
    {
        public PostgreSQLAddMonths(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return new CallExpression("DateAdd", new[] { new ValueExpression("mm", TokenType.Symbol), LMonths, LDateTime });
        }
    }

    public class PostgreSQLAddYears : SQLDeviceOperator
    {
        public PostgreSQLAddYears(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return new CallExpression("DateAdd",
                                      new[] { new ValueExpression("yyyy", TokenType.Symbol), LMonths, LDateTime });
        }
    }

    public class PostgreSQLDayOfWeek : SQLDeviceOperator
    // TODO: do for removal as replaced with Storage.TranslationString in SystemCatalog.d4
    {
        public PostgreSQLDayOfWeek(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] { new ValueExpression("dw", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDayOfYear : SQLDeviceOperator
    {
        public PostgreSQLDayOfYear(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] { new ValueExpression("dy", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDateTimeReadHour : SQLDeviceOperator
    {
        public PostgreSQLDateTimeReadHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] { new ValueExpression("hh", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDateTimeReadMinute : SQLDeviceOperator
    {
        public PostgreSQLDateTimeReadMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] { new ValueExpression("mi", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDateTimeReadSecond : SQLDeviceOperator
    {
        public PostgreSQLDateTimeReadSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] { new ValueExpression("ss", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDateTimeReadMillisecond : SQLDeviceOperator
    {
        public PostgreSQLDateTimeReadMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] { new ValueExpression("ms", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDateTimeWriteMillisecond : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "ms";
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[] { new ValueExpression(LPartString, TokenType.Symbol), LDateTime });
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime });
        }
    }

    public class PostgreSQLDateTimeWriteSecond : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "ss";
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[] { new ValueExpression(LPartString, TokenType.Symbol), LDateTime });
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime });
        }
    }

    public class PostgreSQLDateTimeWriteMinute : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "mi";
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[] { new ValueExpression(LPartString, TokenType.Symbol), LDateTime });
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime });
        }
    }

    public class PostgreSQLDateTimeWriteHour : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "hh";
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[] { new ValueExpression(LPartString, TokenType.Symbol), LDateTime });
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] { new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime });
        }
    }

    public class PostgreSQLDateTimeWriteDay : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteDay(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return PostgreSQLDateTimeFunctions.WriteDay(LDateTime, LDateTimeCopy, LPart);
        }
    }

    public class PostgreSQLDateTimeWriteMonth : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteMonth(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return PostgreSQLDateTimeFunctions.WriteMonth(LDateTime, LDateTimeCopy, LPart);
        }
    }

    public class PostgreSQLDateTimeWriteYear : SQLDeviceOperator
    {
        public PostgreSQLDateTimeWriteYear(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return PostgreSQLDateTimeFunctions.WriteYear(LDateTime, LDateTimeCopy, LPart);
        }
    }

    public class PostgreSQLDateTimeDatePart : SQLDeviceOperator
    {
        public PostgreSQLDateTimeDatePart(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LFromConvert = new CallExpression("Convert",
                                                         new[] { new ValueExpression("Float", TokenType.Symbol), LDateTime });
            Expression LFromMath = new CallExpression("Floor", new[] { LFromConvert });
            return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), LDateTime });
        }
    }

    public class PostgreSQLDateTimeTimePart : SQLDeviceOperator
    {
        public PostgreSQLDateTimeTimePart(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LFromConvert = new CallExpression("Convert",
                                                         new[] { new ValueExpression("Float", TokenType.Symbol), LDateTime });
            Expression LFromConvertCopy = new CallExpression("Convert",
                                                             new[]
                                                                 {
                                                                     new ValueExpression("Float", TokenType.Symbol),
                                                                     LDateTime
                                                                 });
            Expression LFromMath = PostgreSQLMath.Frac(LFromConvert, LFromConvertCopy);
            return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), LDateTime });
        }
    }


    /// <summary>
    ///  DateTime selector is done by constructing a string representation of the value and converting it to a datetime using style 121 (ODBC Canonical)	
    ///  Convert(DateTime, Convert(VarChar, AYear) + '-' + Convert(VarChar, AMonth) + '-' + Convert(VarChar, ADay) + ' ' + Convert(VarChar, AHours) + ':' + Convert(VarChar, AMinutes) + ':' + Convert(VarChar, ASeconds) + '.' + Convert(VarChar, AMilliseconds), 121)
    /// </summary>
    public class PostgreSQLDateTimeSelector : SQLDeviceOperator
    {
        public PostgreSQLDateTimeSelector(int AID, string AName)
            : base(AID, AName)
        {
        }

        public static Expression DateTimeSelector(Expression AYear, Expression AMonth, Expression ADay,
                                                  Expression AHours, Expression AMinutes, Expression ASeconds,
                                                  Expression AMilliseconds)
        {
            Expression LExpression =
                new BinaryExpression(
                    new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), AYear }), "+",
                    new ValueExpression("-"));
            LExpression = new BinaryExpression(LExpression, "+",
                                               new CallExpression("Convert",
                                                                  new[]
                                                                      {
                                                                          new ValueExpression("VarChar",
                                                                                              TokenType.Symbol),
                                                                          AMonth
                                                                      }));
            LExpression = new BinaryExpression(LExpression, "+", new ValueExpression("-"));
            LExpression = new BinaryExpression(LExpression, "+",
                                               new CallExpression("Convert",
                                                                  new[]
                                                                      {
                                                                          new ValueExpression("VarChar",
                                                                                              TokenType.Symbol),
                                                                          ADay
                                                                      }));
            if (AHours != null)
            {
                LExpression = new BinaryExpression(LExpression, "+", new ValueExpression(" "));
                LExpression = new BinaryExpression(LExpression, "+",
                                                   new CallExpression("Convert",
                                                                      new[]
                                                                          {
                                                                              new ValueExpression("VarChar",
                                                                                                  TokenType.Symbol),
                                                                              AHours
                                                                          }));
                LExpression = new BinaryExpression(LExpression, "+", new ValueExpression(":"));
                LExpression = new BinaryExpression(LExpression, "+",
                                                   new CallExpression("Convert",
                                                                      new[]
                                                                          {
                                                                              new ValueExpression("VarChar",
                                                                                                  TokenType.Symbol),
                                                                              AMinutes
                                                                          }));
                if (ASeconds != null)
                {
                    LExpression = new BinaryExpression(LExpression, "+", new ValueExpression(":"));
                    LExpression = new BinaryExpression(LExpression, "+",
                                                       new CallExpression("Convert",
                                                                          new[]
                                                                              {
                                                                                  new ValueExpression("VarChar",
                                                                                                      TokenType.Symbol),
                                                                                  ASeconds
                                                                              }));
                    if (AMilliseconds != null)
                    {
                        LExpression = new BinaryExpression(LExpression, "+", new ValueExpression("."));
                        LExpression = new BinaryExpression(LExpression, "+",
                                                           new CallExpression("Convert",
                                                                              new[]
                                                                                  {
                                                                                      new ValueExpression("VarChar",
                                                                                                          TokenType.
                                                                                                              Symbol),
                                                                                      AMilliseconds
                                                                                  }));
                    }
                }
            }

            return new CallExpression("Convert",
                                      new[]
                                          {
                                              new ValueExpression("DateTime", TokenType.Symbol), LExpression,
                                              new ValueExpression(121)
                                          });
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan)ADevicePlan;
            var LArguments = new Expression[APlanNode.Nodes.Count];
            for (int LIndex = 0; LIndex < APlanNode.Nodes.Count; LIndex++)
                LArguments[LIndex] = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[LIndex], false);
            switch (APlanNode.Nodes.Count)
            {
                case 7:
                    return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], LArguments[3], LArguments[4],
                                            LArguments[5], LArguments[6]);
                case 6:
                    return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], LArguments[3], LArguments[4],
                                            LArguments[5], null);
                case 5:
                    return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], LArguments[3], LArguments[4],
                                            null, null);
                case 3:
                    return DateTimeSelector(LArguments[0], LArguments[1], LArguments[2], null, null, null, null);
                default:
                    return null;
            }
        }
    }

}
