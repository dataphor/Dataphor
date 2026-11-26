# System.Math.Log

**Declarations**

```d4
create operator .System.Math.Log(const AValue : System.Decimal, const ABase : System.Decimal) : System.Decimal
 class "System.DecimalLogNode";
```

**Remarks**

`Log` calculates the logarithm to the specified base. The number that results is the number needed to raise the specified base to, to make it equal the number of the the logarithm.

*2<sup>4.75488750216347</sup> = 27*

**Using System.Math.Log**

```d4
select TableDee add { Math.Log(27, 2) log };
```

```text
log 
----------------
4.75488750216347
```

**See Also**

[System.Math.Exp](System.Math.Exp.md) \| [System.Math.Ln](System.Math.Ln.md) \| [System.Math.Log10](System.Math.Log10.md)
