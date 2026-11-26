# System.Frac

**Declarations**

```d4
create operator .System.Frac(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalFracNode";
```

**Remarks**

`Frac` returns the fractional portion of the specified decimal value.

**Using System.Frac**

```d4
select TableDee add { Frac(-3.4) Neg, Frac(3.4) Pos };
```

```text
Neg  Pos
---- ---
-0.4 0.4
```

**See Also**

[System.Floor](System.Floor.md)
