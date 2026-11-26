# System.Floor

**Declarations**

```d4
create operator .System.Floor(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalFloorNode";
```

**Remarks**

`Floor` rounds down to the nearest Integer. It takes and returns values of type decimal, but will return a value that could be represented as an integer. This is done because it still allows you to multiply and divide by a power of ten (or any other number) to round to different precisions. An integer can be easily attained by converting to an integer.

**Using System.Floor**

```d4
select TableDee add { Floor(-3.4) Neg, Floor(3.4) Pos };
```

```text
Neg Pos
--- ---
-4  3
```

**See Also**

[System.Ceiling](System.Ceiling.md) \| [System.Round](System.Round.md)
