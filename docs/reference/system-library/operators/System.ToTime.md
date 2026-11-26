# System.ToTime

**Declarations**

```d4
create operator .System.ToTime(const AValue : System.String) : System.Time
 class "System.StringToTimeNode";
```

**Remarks**

A `String` type can be explicitly converted to a `Time` type using `ToTime`.

A string that takes any of the forms described in [Standard DateTime Format Strings (Web link)](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) can be converted to a `Time` type.

**Using System.ToTime**

```d4
select ToTime("8:15");
select ToTime("8:15 pm");
select ToTime("18:30:59");
```

```text
08:15:00
20:15:00
18:30:59
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Standard DateTime Format Strings Web Link](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
