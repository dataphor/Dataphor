# System.Math.Ln

**Declarations**

```d4
create operator .System.Math.Ln(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalLnNode";
```

**Remarks**

`Ln` returns the natural logarithm (base *e*) of the specified value. The value that is returned is the value that if raised to the power of *e* would result in the value passed to the function.

*6<sup>e</sup> = 1.79175946922806*

**Using System.Math.Ln**

```d4
select TableDee add {Math.Ln(6) ln };
```

```text
ln 
---------------- 
1.79175946922806
```

**See Also**

[System.Math.Exp](System.Math.Exp.md) \| [System.Math.Log](System.Math.Log.md) \| [System.Math.Log10](System.Math.Log10.md)
