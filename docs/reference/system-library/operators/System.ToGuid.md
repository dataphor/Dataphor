# System.ToGuid

**Declarations**

```d4
create operator .System.ToGuid(const AValue : System.String) : System.Guid
 class "System.StringToGuidNode";
```

**Remarks**

`String` types can be explicitly converted to `Guid` using `ToGuid`.

A Guid string must contain 32 digits and 4 dashes in the format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

**Using System.ToGuid**

```d4
select ToGuid("12345678-1234-1234-1234-123456789012");
```

```text
12345678-1234-1234-1234-123456789012
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.Guid](../types/System.Guid.md) \| [System.NewGuid](System.NewGuid.md) \| [System.NameFromGuid](System.NameFromGuid.md) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
