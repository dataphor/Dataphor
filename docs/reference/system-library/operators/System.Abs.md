# System.Abs

**Declarations**

```d4
create operator .System.Abs(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalAbsNode";
create operator .System.Abs(const AValue : System.Integer) : System.Integer
 class "System.IntegerAbsNode";
```

**Remarks**

`System.Abs` returns the Absolute value (nonnegative) of a given value. If the given value is negative, then this function will return the opposite (additive inverse) of the given number.

**Using System.Abs**

```d4
select
    TableDee add
    {
        Abs(3) Pos, // returns 3
        Abs(-3.14159) Neg // returns 3.14159
    };
```

```text
Pos Neg
--- -------
3   3.14159
```

**See Also**

[System.Duration](System.Duration.md)
