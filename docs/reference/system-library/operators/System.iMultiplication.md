# System.iMultiplication

**Declarations**

```d4
create operator .System.iMultiplication(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Money, const ARightValue : System.Decimal) : System.Money
 class "System.MoneyDecimalMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Decimal, const ARightValue : System.Money) : System.Money
 class "System.DecimalMoneyMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Money, const ARightValue : System.Integer) : System.Money
 class "System.MoneyIntegerMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Integer, const ARightValue : System.Money) : System.Money
 class "System.IntegerMoneyMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.TimeSpan, const ARightValue : System.Integer) : System.TimeSpan
 class "System.TimeSpanIntegerMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Integer, const ARightValue : System.TimeSpan) : System.TimeSpan
 class "System.IntegerTimeSpanMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.TimeSpan, const ARightValue : System.Decimal) : System.TimeSpan
 class "System.TimeSpanDecimalMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMultiplication(const ALeftValue : System.Decimal, const ARightValue : System.TimeSpan) : System.TimeSpan
 class "System.DecimalTimeSpanMultiplicationNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`*` is the parser recognized symbol for `iMultiplication`. This operator returns the arithmetic product of the two operands.

**Using \***

```d4
select Manager add {Employee_ID * Manager_ID Product};
```

```text
Manager_ID Employee_ID Product
---------- ----------- -------
1          2           2
1          3           3
2          4           8
2          6           12
4          5           20
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
