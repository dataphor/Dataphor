# System.iCompare

**Declarations**

```d4
create operator .System.iCompare(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Integer
 class "System.BooleanCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Integer
 class "System.ByteCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Integer
 class "System.ShortCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Integer
 class "System.LongCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Guid, const ARightValue : System.Guid) : System.Integer
 class "System.GuidCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Integer
 class "System.DecimalCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.String, const ARightValue : System.String) : System.Integer
 class "System.StringCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Integer
 class "System.TimeSpanCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.Integer
 class "System.DateTimeCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Integer
 class "System.DateCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Integer
 class "System.TimeCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Integer
 class "System.MoneyCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.Name, const ARightValue : System.Name) : System.Integer
 class "System.StringCompareNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.UserID, const ARightValue : System.UserID) : System.Integer
 class "System.StringCompareNode" tags { DAE.IsBuiltin = "True" };
create operator .System.iCompare(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.Integer
 class "System.VersionNumberCompareNode";
create operator .System.iCompare(const ALeftValue : System.LibraryReference, const ARightValue : System.LibraryReference) : System.Integer
 class "System.LibraryReferenceCompareNode";
```

**Remarks**

`?=` is the parser recognized symbol for `iCompare`. The `Compare` operator is a comparison operator that returns an integer.

`x ?= y`

is defined as

`if x < y then -1 else if x = y then 0 else 1`

It is a good operator to use to figure out if two values are the same, or how they are different.

**Using System.iCompare**

```d4
select Manager add { Manager_ID ?= Employee_ID compare };
```

```text
Manager_ID Employee_ID compare
---------- ----------- -------
1          2           -1
1          3           -1
2          4           -1
2          6           -1
4          5           -1
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Table Comparison Operators](../../../developers-guide/d4-table-operators.md#comparison-operators)
