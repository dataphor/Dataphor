# System.ToDateTime

**Declarations**

```d4
create operator .System.ToDateTime(const AValue : System.Date) : System.DateTime
 class "System.ScalarSelectorNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToDateTime(const AValue : System.String) : System.DateTime
 class "System.StringToDateTimeNode";
```

**Remarks**

`Date` and `String` types can be explicitly converted to a `DateTime` type using `ToDateTime`.

A string that takes any of the forms described in [Standard DateTime Format Strings (Web link)](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) can be converted to a `DateTime` type.

**Using System.ToDateTime**

```d4
select ToDateTime(Date()); // time will be 0
select ToDateTime("Dec 25, 2004 8:15");
select ToDateTime("30 December 2004 11:59:59 pm");
select ToDateTime("2004, 7, 4 18:30:23");
```

```text
2004-04-26 00:00:00
2004-12-25 08:15:00
2004-12-30 23:59:59
2004-07-04 18:30:23
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Standard DateTime Format Strings Web Link](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
