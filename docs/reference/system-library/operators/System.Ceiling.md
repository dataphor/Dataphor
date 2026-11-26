# System.Ceiling

**Declarations**

```d4
create operator .System.Ceiling(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalCeilingNode";
```

**Remarks**

`Ceiling` takes a value and returns the smallest integer value that is greater than or equal to the specified value. It is essentially a round-up operator.

**Using System.Ceiling**

```d4
select TableDee add { Ceiling(-3.4) Neg, Ceiling(3.4) Pos };
```

```text
Neg Pos 
--- --- 
-3  4
```

**See Also**

[System.Floor](System.Floor.md) \| [System.Round](System.Round.md)
