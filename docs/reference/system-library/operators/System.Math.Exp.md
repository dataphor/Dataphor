# System.Math.Exp

**Declarations**

```d4
create operator .System.Math.Exp(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalExpNode";
```

**Remarks**

`Exp` calculates the natural exponent of the specified value. In other words it returns *e* raised to the specified power (*e<sup>x</sup>*)

*e<sup>6</sup> = 403.428793492735*

**Using System.Math.Exp**

```d4
select TableDee add { Exp(6) NaturalExponent};
```

```text
NaturalExponent 
---------------- 
403.428793492735
```

**See Also**

[System.Math.Ln](System.Math.Ln.md) \| [System.Math.Log](System.Math.Log.md) \| [System.Math.Log10](System.Math.Log10.md)
