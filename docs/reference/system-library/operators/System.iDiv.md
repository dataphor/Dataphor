# System.iDiv

**Declarations**

```d4
create operator .System.iDiv(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteDivNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDiv(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortDivNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDiv(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerDivNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDiv(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongDivNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iDiv(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalDivNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`div` is the parser recognized symbol for `iDiv`. The `div` operator returns a quotient of the two values. In all cases, including dividing decimal numbers, integer division is performed. For decimal division, use [`/`](System.iDivision.md).

Integer divison is defined as iterated subtraction. The divisor is subtracted from the dividend until the divisor cannot be subtracted from what remains of the dividend without changing the sign of the remainder of the dividend. The result of this operator is the number of times this can be done. This same definition applies to decimal numbers. What remains of the dividend can be accessed via the modulus ([`mod`](System.iMod.md)) operator.

**Using div**

```d4
select TableDee add {17 div 5 IntDiv, 1.2 div 0.35 DecIntDiv };
```

```text
IntDiv DecIntDiv
------ ---------
3      3
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Arithmetic Expressions](../../../developers-guide/d4-language-elements.md#arithmetic-expression) \| [System.iMod (`mod`)](System.iMod.md) \| [System.iDivison (`/`)](System.iDivision.md) \| [Table Expressions](../../../developers-guide/d4-table-operators.md)
