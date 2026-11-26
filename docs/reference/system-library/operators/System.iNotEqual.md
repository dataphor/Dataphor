# System.iNotEqual

**Declarations**

```d4
create operator .System.iNotEqual(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Boolean
 class "System.ByteNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Boolean
 class "System.ShortNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Boolean
 class "System.IntegerNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Boolean
 class "System.LongNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Guid, const ARightValue : System.Guid) : System.Boolean
 class "System.GuidNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Boolean
 class "System.DecimalNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.String, const ARightValue : System.String) : System.Boolean
 class "System.StringNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Boolean
 class "System.TimeSpanNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.Boolean
 class "System.DateTimeNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Boolean
 class "System.DateNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Boolean
 class "System.TimeNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Boolean
 class "System.MoneyNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.Name, const ARightValue : System.Name) : System.Boolean
 class "System.StringNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(ALeftValue : generic table, ARightValue : generic table) : System.Boolean
 class "System.TableNotEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNotEqual(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.Boolean
 class "System.VersionNumberNotEqualNode";
```

**Remarks**

`<>` is the parser recognized symbol for `iNotEqual`. This operator is a comparison operator that checks to see if the two values on either side of the operator are not equal. This operator returns a boolean value representing the comparison.

**Using System.iNotEqual**

```d4
select Manager where Manager_ID <> Employee_ID;
```

```text
Manager_ID Employee_ID 
---------- ----------- 
1          2           
1          3           
2          4           
2          6           
4          5
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Table Comparison Operators](../../../developers-guide/d4-table-operators.md#comparison-operators)
