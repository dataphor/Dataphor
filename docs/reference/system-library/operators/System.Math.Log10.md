# System.Math.Log10

**Declarations**

```d4
create operator .System.Math.Log10(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalLog10Node";
```

**Remarks**

`Log10` returns the common logarithm (base 10) of the specified number. The number that is returned is the number that if 10 was raised to that power would result in the specified number.

*10<sup>3</sup> = 1000*

**Using System.Math.Log10**

```d4
select TableDee add { Math.Log10(1000) log };
```

```text
log 
--- 
3
```

**See Also**

[System.Math.Exp](System.Math.Exp.md) \| [System.Math.Ln](System.Math.Ln.md) \| [System.Math.Log](System.Math.Log.md)
