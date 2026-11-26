# System.iInclusiveGreater

**Declarations**

```d4
create operator .System.iInclusiveGreater(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Boolean
 class "System.ByteInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Boolean
 class "System.ShortInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Boolean
 class "System.IntegerInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Boolean
 class "System.LongInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Guid, const ARightValue : System.Guid) : System.Boolean
 class "System.GuidInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Boolean
 class "System.DecimalInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.String, const ARightValue : System.String) : System.Boolean
 class "System.StringInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Boolean
 class "System.TimeSpanInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.Boolean
 class "System.DateTimeInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Boolean
 class "System.DateInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Boolean
 class "System.TimeInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Boolean
 class "System.MoneyInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.Name, const ARightValue : System.Name) : System.Boolean
 class "System.StringInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(ALeftValue : generic table, ARightValue : generic table) : System.Boolean
 class "System.TableInclusiveGreaterNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveGreater(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.Boolean
 class "System.VersionNumberInclusiveGreaterNode";
```

**Remarks**

`>=` is the parser recognized symbol for `iInclusiveGreater`. The inclusive greater than operator is a comparison operator. D4 compares the left and right values. If the left value is greater than or equal to the right value, the operator returns true, otherwise it returns false.

**Using System.iInclusiveGreater**

```d4
select Manager add { Manager_ID >= Employee_ID Greater};
```

```text
Manager_ID Employee_ID Greater
---------- ----------- -------
1          2           False
1          3           False
2          4           False
2          6           False
4          5           False
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Table Comparison Operators](../../../developers-guide/d4-table-operators.md#comparison-operators)
