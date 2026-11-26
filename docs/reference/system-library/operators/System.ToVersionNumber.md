# System.ToVersionNumber

**Declarations**

```d4
create operator .System.ToVersionNumber(const AValue : System.String) : System.VersionNumber
 class "System.StringToVersionNumberNode";
```

**Remarks**

`String` types can be explicitly converted to `VersionNumber` using `ToVersionNumber`.

With strings, converts the specified `String` representation of a number, with optional plus sign, to an equivalent 96-bit signed integer.

An error will occur if the value is outside that range of a Decimal (-79,228,162,514,264,337,593,543,950,335 to +79,228,162,514,264,337,593,543,950,335).

**Using System.ToVersionNumber**

```d4
select ToVersionNumber("2.0.15.9");
```

```text
2.0.15.9
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md)
