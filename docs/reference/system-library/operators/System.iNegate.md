# System.iNegate

**Declarations**

```d4
create operator .System.iNegate(const AValue : System.Short) : System.Short
 class "System.ShortNegateNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNegate(const AValue : System.Integer) : System.Integer
 class "System.IntegerNegateNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNegate(const AValue : System.Long) : System.Long
 class "System.LongNegateNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNegate(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalNegateNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNegate(const AValue : System.Money) : System.Money
 class "System.DecimalNegateNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iNegate(const AValue : System.TimeSpan) : System.TimeSpan
 class "System.TimeSpanNegateNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`-` is the parser recognized symbol for `iNegate`. This is a unary operator that returns the negative equivalent (additive inverse) of a given positive value. It has the same effect as taking a positive number and multiplying it by negative one, except for the fact that this operator has a higher precedence than the multiplicative operators.

**Using Unary -**

```d4
select Employee where ID > Abs(-2);
```

```text
ID Name 
-- ---- 
3  Josh 
4  Jeff 
5  Jake 
6  Jeff
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iSubtraction (`-`)](System.iSubtraction.md)
