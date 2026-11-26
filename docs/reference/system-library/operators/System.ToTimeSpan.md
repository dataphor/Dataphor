# System.ToTimeSpan

**Declarations**

```d4
create operator .System.ToTimeSpan(const AValue : System.String) : System.TimeSpan
 class "System.StringToTimeSpanNode";
```

**Remarks**

A `String` type can be explicitly converted to a `TimeSpan` type using `ToTimeSpan`.

**Using System.ToTimeSpan**

```d4
select ToTimeSpan("2356wks 4days 14hrs 17min 1sec 84mil");
select ToTimeSpan("3h 15m");
select ToTimeSpan("9d 15m 18s");
select ToTimeSpan("123456789mil");
select ToDateTime("Dec 24, 2004 11:59 pm")+ToTimeSpan("7days");
```

```text
2356wks 4days 14hrs 17min 1sec 84mil
3hrs 15min
1wks 2days 0hrs 15min 18sec
1days 10hrs 17min 36sec 789mil
2004-12-31 23:59:00
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [Standard DateTime Format Strings Web Link](http://msdn.microsoft.com/library/en-us/cpguide/html/cpconstandarddatetimeformatstrings.asp) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
