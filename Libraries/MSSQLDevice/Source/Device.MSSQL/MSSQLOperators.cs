using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.MSSQL
{

    #region Operators

    // ToString(AValue) ::= Convert(varchar, AValue)
    public class MSSQLToString : SQLDeviceOperator
    {
        public MSSQLToString(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToBit : SQLDeviceOperator
    {
        public MSSQLToBit(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToTinyInt : SQLDeviceOperator
    {
        public MSSQLToTinyInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToByte : SQLDeviceOperator
    {
        public MSSQLToByte(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToSmallInt : SQLDeviceOperator
    {
        public MSSQLToSmallInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToSByte : SQLDeviceOperator
    {
        public MSSQLToSByte(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToShort : SQLDeviceOperator
    {
        public MSSQLToShort(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToInt : SQLDeviceOperator
    {
        public MSSQLToInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToUShort : SQLDeviceOperator
    {
        public MSSQLToUShort(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToInteger : SQLDeviceOperator
    {
        public MSSQLToInteger(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToBigInt : SQLDeviceOperator
    {
        public MSSQLToBigInt(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToUInteger : SQLDeviceOperator
    {
        public MSSQLToUInteger(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToLong : SQLDeviceOperator
    {
        public MSSQLToLong(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToDecimal20 : SQLDeviceOperator
    {
        public MSSQLToDecimal20(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToDecimal288 : SQLDeviceOperator
    {
        public MSSQLToDecimal288(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLToULong : SQLDeviceOperator
    {
        public MSSQLToULong(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToDecimal : SQLDeviceOperator
    {
        public MSSQLToDecimal(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToMoney : SQLDeviceOperator
    {
        public MSSQLToMoney(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLToUniqueIdentifier : SQLDeviceOperator
    {
        public MSSQLToUniqueIdentifier(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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
    public class MSSQLMath
    {
        public static Expression Truncate(Expression AExpression)
        {
            return new CallExpression("Round", new[] {AExpression, new ValueExpression(0), new ValueExpression(1)});
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

    public class MSSQLTimeSpan
    {
        public static Expression ReadMillisecond(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(10000000));
            Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(1000));
            return MSSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadSecond(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(600000000));
            Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
            return MSSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadMinute(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(36000000000));
            Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(60));
            return MSSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadHour(Expression AValue)
        {
            Expression LToFrac = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
            Expression LToFracCopy = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
            Expression LFromFrac = MSSQLMath.Frac(LToFrac, LToFracCopy);
            Expression LToTrunc = new BinaryExpression(LFromFrac, "iMultiplication", new ValueExpression(24));
            return MSSQLMath.Truncate(LToTrunc);
        }

        public static Expression ReadDay(Expression AValue)
        {
            Expression LToTrunc = new BinaryExpression(AValue, "iDivision", new ValueExpression(864000000000));
            return MSSQLMath.Truncate(LToTrunc);
        }
    }

    public class MSSQLDateTimeFunctions
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

    // Operators that MSSQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.

    // Math
    public class MSSQLPower : SQLDeviceOperator
    {
        public MSSQLPower(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    public class MSSQLTruncate : SQLDeviceOperator
    {
        public MSSQLTruncate(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLMath.Truncate(LValue);
        }
    }

    public class MSSQLFrac : SQLDeviceOperator
    {
        public MSSQLFrac(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LValueCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLMath.Frac(LValue, LValueCopy);
        }
    }

    public class MSSQLLogB : SQLDeviceOperator
    {
        public MSSQLLogB(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LBase = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            LValue = new CallExpression("Log", new[] {LValue});
            LBase = new CallExpression("Log", new[] {LBase});
            return new BinaryExpression(LValue, "iDivision", LBase);
        }
    }

    // TimeSpan
    public class MSSQLTimeSpanReadMillisecond : SQLDeviceOperator
    {
        public MSSQLTimeSpanReadMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLTimeSpan.ReadMillisecond(LValue);
        }
    }

    public class MSSQLTimeSpanReadSecond : SQLDeviceOperator
    {
        public MSSQLTimeSpanReadSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLTimeSpan.ReadSecond(LValue);
        }
    }

    public class MSSQLTimeSpanReadMinute : SQLDeviceOperator
    {
        public MSSQLTimeSpanReadMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLTimeSpan.ReadMinute(LValue);
        }
    }

    public class MSSQLTimeSpanReadHour : SQLDeviceOperator
    {
        public MSSQLTimeSpanReadHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLTimeSpan.ReadHour(LValue);
        }
    }

    public class MSSQLTimeSpanReadDay : SQLDeviceOperator
    {
        public MSSQLTimeSpanReadDay(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LValue = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return MSSQLTimeSpan.ReadDay(LValue);
        }
    }

    public class MSSQLTimeSpanWriteMillisecond : SQLDeviceOperator
    {
        public MSSQLTimeSpanWriteMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = MSSQLTimeSpan.ReadMillisecond(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class MSSQLTimeSpanWriteSecond : SQLDeviceOperator
    {
        public MSSQLTimeSpanWriteSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = MSSQLTimeSpan.ReadSecond(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(10000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class MSSQLTimeSpanWriteMinute : SQLDeviceOperator
    {
        public MSSQLTimeSpanWriteMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = MSSQLTimeSpan.ReadMinute(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(600000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class MSSQLTimeSpanWriteHour : SQLDeviceOperator
    {
        public MSSQLTimeSpanWriteHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = MSSQLTimeSpan.ReadHour(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(36000000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }

    public class MSSQLTimeSpanWriteDay : SQLDeviceOperator
    {
        public MSSQLTimeSpanWriteDay(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LTimeSpan = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LTimeSpanCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LFromPart = MSSQLTimeSpan.ReadDay(LTimeSpanCopy);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LFromPart);
            LPart = new BinaryExpression(LParts, "iMultiplication", new ValueExpression(864000000000));
            return new BinaryExpression(LTimeSpan, "iAddition", LPart);
        }
    }


    public class MSSQLAddMonths : SQLDeviceOperator
    {
        public MSSQLAddMonths(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return new CallExpression("DateAdd", new[] {new ValueExpression("mm", TokenType.Symbol), LMonths, LDateTime});
        }
    }

    public class MSSQLAddYears : SQLDeviceOperator
    {
        public MSSQLAddYears(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LMonths = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression("yyyy", TokenType.Symbol), LMonths, LDateTime});
        }
    }

    public class MSSQLDayOfWeek : SQLDeviceOperator
        // TODO: do for removal as replaced with Storage.TranslationString in SystemCatalog.d4
    {
        public MSSQLDayOfWeek(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] {new ValueExpression("dw", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDayOfYear : SQLDeviceOperator
    {
        public MSSQLDayOfYear(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] {new ValueExpression("dy", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDateTimeReadHour : SQLDeviceOperator
    {
        public MSSQLDateTimeReadHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] {new ValueExpression("hh", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDateTimeReadMinute : SQLDeviceOperator
    {
        public MSSQLDateTimeReadMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] {new ValueExpression("mi", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDateTimeReadSecond : SQLDeviceOperator
    {
        public MSSQLDateTimeReadSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] {new ValueExpression("ss", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDateTimeReadMillisecond : SQLDeviceOperator
    {
        public MSSQLDateTimeReadMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            return new CallExpression("DatePart", new[] {new ValueExpression("ms", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDateTimeWriteMillisecond : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteMillisecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "ms";
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
        }
    }

    public class MSSQLDateTimeWriteSecond : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteSecond(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "ss";
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
        }
    }

    public class MSSQLDateTimeWriteMinute : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteMinute(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "mi";
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
        }
    }

    public class MSSQLDateTimeWriteHour : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteHour(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            string LPartString = "hh";
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            Expression LOldPart = new CallExpression("DatePart",
                                                     new[]
                                                         {new ValueExpression(LPartString, TokenType.Symbol), LDateTime});
            LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LParts = new BinaryExpression(LPart, "iSubtraction", LOldPart);
            return new CallExpression("DateAdd",
                                      new[] {new ValueExpression(LPartString, TokenType.Symbol), LParts, LDateTime});
        }
    }

    public class MSSQLDateTimeWriteDay : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteDay(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return MSSQLDateTimeFunctions.WriteDay(LDateTime, LDateTimeCopy, LPart);
        }
    }

    public class MSSQLDateTimeWriteMonth : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteMonth(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return MSSQLDateTimeFunctions.WriteMonth(LDateTime, LDateTimeCopy, LPart);
        }
    }

    public class MSSQLDateTimeWriteYear : SQLDeviceOperator
    {
        public MSSQLDateTimeWriteYear(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LDateTimeCopy = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LPart = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[1], false);
            return MSSQLDateTimeFunctions.WriteYear(LDateTime, LDateTimeCopy, LPart);
        }
    }

    public class MSSQLDateTimeDatePart : SQLDeviceOperator
    {
        public MSSQLDateTimeDatePart(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LFromConvert = new CallExpression("Convert",
                                                         new[]
                                                             {new ValueExpression("Float", TokenType.Symbol), LDateTime});
            Expression LFromMath = new CallExpression("Floor", new[] {LFromConvert});
            return new CallExpression("Convert", new[] {new ValueExpression("DateTime", TokenType.Symbol), LDateTime});
        }
    }

    public class MSSQLDateTimeTimePart : SQLDeviceOperator
    {
        public MSSQLDateTimeTimePart(int AID, string AName)
            : base(AID, AName)
        {
        }

        public override Statement Translate(DevicePlan ADevicePlan, PlanNode APlanNode)
        {
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
            Expression LDateTime = LDevicePlan.Device.TranslateExpression(LDevicePlan, APlanNode.Nodes[0], false);
            Expression LFromConvert = new CallExpression("Convert",
                                                         new[]
                                                             {new ValueExpression("Float", TokenType.Symbol), LDateTime});
            Expression LFromConvertCopy = new CallExpression("Convert",
                                                             new[]
                                                                 {
                                                                     new ValueExpression("Float", TokenType.Symbol),
                                                                     LDateTime
                                                                 });
            Expression LFromMath = MSSQLMath.Frac(LFromConvert, LFromConvertCopy);
            return new CallExpression("Convert", new[] {new ValueExpression("DateTime", TokenType.Symbol), LDateTime});
        }
    }


    /// <summary>
    ///  DateTime selector is done by constructing a string representation of the value and converting it to a datetime using style 121 (ODBC Canonical)	
    ///  Convert(DateTime, Convert(VarChar, AYear) + '-' + Convert(VarChar, AMonth) + '-' + Convert(VarChar, ADay) + ' ' + Convert(VarChar, AHours) + ':' + Convert(VarChar, AMinutes) + ':' + Convert(VarChar, ASeconds) + '.' + Convert(VarChar, AMilliseconds), 121)
    /// </summary>
    public class MSSQLDateTimeSelector : SQLDeviceOperator
    {
        public MSSQLDateTimeSelector(int AID, string AName)
            : base(AID, AName)
        {
        }

        public static Expression DateTimeSelector(Expression AYear, Expression AMonth, Expression ADay,
                                                  Expression AHours, Expression AMinutes, Expression ASeconds,
                                                  Expression AMilliseconds)
        {
            Expression LExpression =
                new BinaryExpression(
                    new CallExpression("Convert", new[] {new ValueExpression("VarChar", TokenType.Symbol), AYear}), "+",
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
            var LDevicePlan = (SQLDevicePlan) ADevicePlan;
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

    #endregion
}