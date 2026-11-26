# System.ToString

**Declarations**

```d4
create operator .System.ToString(const AValue : System.Boolean) : System.String
 class "System.BooleanToStringNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToString(const AValue : System.Byte) : System.String
 class "System.ByteToStringNode";
create operator .System.ToString(const AValue : System.Short) : System.String
 class "System.ShortToStringNode";
create operator .System.ToString(const AValue : System.Integer) : System.String
 class "System.IntegerToStringNode";
create operator .System.ToString(const AValue : System.Long) : System.String
 class "System.LongToStringNode";
create operator .System.ToString(const AValue : System.Decimal) : System.String
 class "System.DecimalToStringNode";
create operator .System.ToString(const AValue : System.Money) : System.String
 class "System.MoneyToStringNode";
create operator .System.ToString(const AValue : System.Guid) : System.String
 class "System.GuidToStringNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToString(const AValue : System.DateTime) : System.String
 class "System.DateTimeToStringNode";
create operator .System.ToString(const AValue : System.Date) : System.String
 class "System.DateToStringNode";
create operator .System.ToString(const AValue : System.TimeSpan) : System.String
 class "System.TimeSpanToStringNode";
create operator .System.ToString(const AValue : System.Time) : System.String
 class "System.TimeToStringNode";
create operator .System.ToString(const AValue : System.VersionNumber) : System.String
 class "System.VersionNumberToStringNode";
```

**Remarks**

`Boolean, Byte, Date, DateTime, Decimal, Guid, Integer, Long, Money, Short, Time, TimeSpan,` and `VersionNumber` types can be explicitly converted to `String` using `ToString`.

**Using System.ToString**

```d4
select ToString(123.45*83); // Long
select ToString(DateTime() + ToTimeSpan("714hours")); // DateTime
select ToString("yes" = "Yes"); // boolean
```

```text
10246.35
2004-05-26 19:53:49
false
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
