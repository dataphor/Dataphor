# System.iMod

**Declarations**

```d4
create operator .System.iMod(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteModNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMod(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortModNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMod(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerModNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMod(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongModNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iMod(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalModNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`mod` is the parser recognized symbol for `iMod`. This operator can be used on both integer types and decimal types.

This operator behaves as follows:

```d4
(a mod b = r) given (a div b = q) and (a = b*q + r where 0 <= r < b).
```

**Using System.iMod**

```d4
select TableDee add {17 mod 5 IntMod, 1.2 mod 0.35 DecIntMod, -340 mod 60 NegIntMod };
```

```text
IntMod DecIntMod NegIntMod
------ --------- ---------
2      0.15      -40
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Arithmetic Expressions](../../../developers-guide/d4-language-elements.md#arithmetic-expression) \| [System.iDiv (`div`)](System.iDiv.md) \| [System.iDivison (`/`)](System.iDivision.md)
