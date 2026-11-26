# System.ToDecimal

**Declarations**

```d4
create operator .System.ToDecimal(const AValue : System.String) : System.Decimal
 class "System.StringToDecimalNode";
create operator .System.ToDecimal(const AValue : System.Integer) : System.Decimal
 class "System.IntegerToDecimalNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToDecimal(const AValue : System.Long) : System.Decimal
 class "System.LongToDecimalNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToDecimal(const AValue : System.Money) : System.Decimal
 class "System.ScalarSelectorNode" static tags { DAE.IsOrderPreserving = "True" };
```

**Remarks**

`Integer`, `Long`, `Money`, and `String` types can be explicitly converted to `Decimal` type using `ToDecimal`.

With strings, converts the specified `String` representation of a number, with optional sign, to an equivalent 96-bit signed integer.

An error will occur if the value is outside that range of a `Decimal` (-79,228,162,514,264,337,593,543,950,335 to +79,228,162,514,264,337,593,543,950,335).

**Using System.ToDecimal**

```d4
select ToDecimal($357/84);
select ToDecimal("87.14");
select ToDecimal(193);
```

```text
4.25
87.14
193
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
