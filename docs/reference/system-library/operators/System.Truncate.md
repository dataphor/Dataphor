# System.Truncate

**Declarations**

```d4
create operator .System.Truncate(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalTruncateNode";
```

**Remarks**

`Truncate` returns the integer portion of a decimal value.

**Using System.Truncate**

```d4
select TableDee add { Truncate(-3.2) Neg, Truncate(3.2) Pos };
```

**See Also**

[System.Ceiling](System.Ceiling.md) \| [System.Floor](System.Floor.md)
