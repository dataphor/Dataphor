# System.iInclusiveLess

**Declarations**

```d4
create operator .System.iInclusiveLess(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Boolean
 class "System.ByteInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Boolean
 class "System.ShortInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Boolean
 class "System.IntegerInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Boolean
 class "System.LongInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Guid, const ARightValue : System.Guid) : System.Boolean
 class "System.GuidInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Boolean
 class "System.DecimalInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.String, const ARightValue : System.String) : System.Boolean
 class "System.StringInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Boolean
 class "System.TimeSpanInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.Boolean
 class "System.DateTimeInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Boolean
 class "System.DateInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Boolean
 class "System.TimeInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Boolean
 class "System.MoneyInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.Name, const ARightValue : System.Name) : System.Boolean
 class "System.StringInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(ALeftValue : generic table, ARightValue : generic table) : System.Boolean
 class "System.TableInclusiveLessNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iInclusiveLess(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.Boolean
 class "System.VersionNumberInclusiveLessNode";
```

**Remarks**

`<=` is the parser recognized symbol for `iInclusiveLess`. The inclusive less than operator is a comparison operator. It compares the left and right values. If the left value is less than or equal to the right value, the operator returns true, otherwise it returns false.

**Using System.iInclusiveLess**

```d4
select Manager add { Manager_ID <= Employee_ID Greater};
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
