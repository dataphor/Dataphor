# System.iLess

**Declarations**

```d4
create operator .System.iLess(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Boolean
 class "System.ByteLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Boolean
 class "System.ShortLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Boolean
 class "System.IntegerLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Boolean
 class "System.LongLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Guid, const ARightValue : System.Guid) : System.Boolean
 class "System.GuidLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Boolean
 class "System.DecimalLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.String, const ARightValue : System.String) : System.Boolean
 class "System.StringLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Boolean
 class "System.TimeSpanLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.Boolean
 class "System.DateTimeLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Boolean
 class "System.DateLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Boolean
 class "System.TimeLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Boolean
 class "System.MoneyLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.Name, const ARightValue : System.Name) : System.Boolean
 class "System.StringLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(ALeftValue : generic table, ARightValue : generic table) : System.Boolean
 class "System.TableLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iLess(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.Boolean
 class "System.VersionNumberLessNode";
```

**Remarks**

`<` is the parser recognized symbol for `iLess`. The less than operator is a comparison operator. It looks at the left and right values and compares them. If the left value is less than the right value, the operator returns true, otherwise it returns false. When the left and right values are equal, the operator returns false.

**Using System.iLess**

```d4
select Manager add { Manager_ID < Employee_ID Greater};
```

```text
Manager_ID Employee_ID Greater 
---------- ----------- ------- 
1          2           True    
1          3           True    
2          4           True    
2          6           True    
4          5           True
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Table Comparison Operators](../../../developers-guide/d4-table-operators.md#comparison-operators)
