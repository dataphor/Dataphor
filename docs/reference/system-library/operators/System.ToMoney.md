# System.ToMoney

**Declarations**

```d4
create operator .System.ToMoney(const AValue : System.String) : System.Money
 class "System.StringToMoneyNode";
create operator .System.ToMoney(const AValue : System.Integer) : System.Money
 class "System.IntegerToDecimalNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToMoney(const AValue : System.Long) : System.Money
 class "System.LongToDecimalNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToMoney(const AValue : System.Decimal) : System.Money
 class "System.DecimalToMoneyNode" static tags { DAE.IsOrderPreserving = "True" };
```

**Remarks**

`Decimal`, `Integer`, `Long`, and `String` types can be explicitly converted to `Money` type using `ToMoney`.

With strings, converts the specified `String` representation of a number, with optional sign, to an equivalent 96-bit signed integer.

An error will occur if the value is outside that range of a `Money` (-79,228,162,514,264,337,593,543,950,335 to +79,228,162,514,264,337,593,543,950,335).

**Using System.ToMoney**

```d4
select 123.45*83;
select ToMoney(123.45*83);
select ToMoney("1234567.89");
```

```text
10246.35
$10,246.35
$1,234,567.89
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
