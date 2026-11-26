# System.Round

**Declarations**

```d4
create operator .System.Round(const AValue : System.Decimal, const ADigits : System.Integer) : System.Decimal
 class "System.DecimalRoundNode";
create operator .System.Round(const AValue : System.Decimal) : System.Decimal
 class "System.DecimalRoundNode";
create operator .System.Round(const AValue : System.Money, const ADigits : System.Integer) : System.Money
 class "System.DecimalRoundNode";
create operator .System.Round(const AValue : System.Money) : System.Money
 class "System.DecimalRoundNode";
```

**Remarks**

`Round` returns the specifiied value rounded to the number of decimal spaces speficied by the precision. If the number of decimal places specified is positive then rounding will occur right of the decimel point that many digits. If the number of decimal places specified is zero then it rounds to the nearest whole number. If the number of decimal places specified is negative then rounding will occur left of the decimal point that many digits. If the number of decimal places is not specified, the value is rounded to two (2) places.

**Using System.Round**

```d4
select TableDee
 add { 3141.59265 Number    }
 add 
 {
  Round(Number, 0) RoundZero,
  Round(Number, 2) RoundPositive,
  Round(Number,-2) RoundNegative
 };
```

```text
Number     RoundZero RoundPositive RoundNegative
---------- --------- ------------- -------------
3141.59265 3142      3141.59       3100         
```

**See Also**

[System.Ceiling](System.Ceiling.md) \| [System.Floor](System.Floor.md)
