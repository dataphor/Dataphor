# System.ToDate

**Declarations**

```d4
create operator .System.ToDate(const AValue : System.DateTime) : System.Date
 class "System.ScalarSelectorNode";
create operator .System.ToDate(const AValue : System.String) : System.Date
 class "System.StringToDateNode";
```

**Remarks**

`DateTime` and `String` types can be explicitly converted to a `Date` type using `ToDate`.

A string that takes any of the forms described in [Standard DateTime Format Strings (Web link)](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) can be converted to a `Data` type.

**Using System.ToDate**

```d4
select ToDate(DateTime()); // time is removed
select ToDate("Dec 25, 2004 8:15"); // time is ignored
select ToDate("2004, 7, 4 18:30:59");
```

```text
2004-04-26
2004-12-25
2004-07-04
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Standard DateTime Format Strings Web Link](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
