# System.iDivision

**Declarations**

```d4
create operator .System.iDivision(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Decimal
 class "System.ByteDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Decimal
 class "System.ShortDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Decimal
 class "System.IntegerDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Decimal
 class "System.LongDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.Money, const ARightValue : System.Decimal) : System.Money
 class "System.MoneyDecimalDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.Money, const ARightValue : System.Integer) : System.Money
 class "System.MoneyIntegerDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.TimeSpan, const ARightValue : System.Integer) : System.TimeSpan
 class "System.TimeSpanIntegerDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.TimeSpan, const ARightValue : System.Decimal) : System.TimeSpan
 class "System.TimeSpanDecimalDivisionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDivision(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Decimal
 class "System.TimeSpanTimeSpanDivisionNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`/` is the parser recognized symbol for `iDivision`. The `/` operator returns a quotient of the two values. In all cases, decimal division will be performed. For integer division, even in the case of two decimals, use [`div`](System.iDiv.md).

The result type of this operator depends on the types of arguments passed to it. See the Declarations section above for more information.

**Using /**

```d4
select TableDee add {17 / 5 IntDiv, 1.2 / 0.35 DecIntDiv };
```

```text
IntDiv DecIntDiv
------ ------------------------------
3.4    3.4285714285714285714285714286 
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Arithmetic Expressions](../../../developers-guide/d4-language-elements.md#arithmetic-expression) \| [System.iMod (`mod`)](System.iMod.md) \| [System.iDiv (`div`)](System.iDiv.md)
