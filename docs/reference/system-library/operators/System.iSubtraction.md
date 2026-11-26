# System.iSubtraction

**Declarations**

```d4
create operator .System.iSubtraction(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Money
 class "System.MoneySubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.TimeSpan
 class "System.TimeSpanSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.DateTime, const ARightValue : System.TimeSpan) : System.DateTime
 class "System.DateTimeTimeSpanSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.TimeSpan
 class "System.DateTimeSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Date, const ARightValue : System.Date) : System.TimeSpan
 class "System.DateSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Date, const ARightValue : System.TimeSpan) : System.Date
 class "System.DateTimeSpanSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Time, const ARightValue : System.Time) : System.TimeSpan
 class "System.TimeSubtractionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iSubtraction(const ALeftValue : System.Time, const ARightValue : System.TimeSpan) : System.Time
 class "System.TimeTimeSpanSubtractionNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`-` is the parser recognized symbol for `iSubtraction`. If the operands are numeric, this operator subtracts one value from another (arithmetic difference). For date/time values, this operator can be used to subtract a `TimeSpan` value from a `Date`, `Time`, `DateTime`, or `TimeSpan` value, or to compute the difference between two `Date`, `Time`, or `DateTime` values as a `TimeSpan`. As with the date/time addition operators, the subtracting a `TimeSpan` value from a date/time type will result in a value of the same type.

Note that when subtracting a `TimeSpan` value from a date/time value, the type of the result is unchanged. This can lead to some unexpected results, for example:

```d4
select Date(2005, 10, 20) - Seconds(1);
select DateTime(2005, 10, 20) - Seconds(1);
```

```text
10/19/2005
10/19/2005 11:59:59 PM
```

Because the result of the subtraction is "rounded" to the nearest `Date` value, the first invocation of `-` results in the previous day. Because changing precision in date/time operations is unusual, this behavior results in less need for explicit conversion, in general. In other words, if we are starting with a `Date` value, we are generally dealing with days, or multiples of days when we perform date manipulation. Of course, if further granularity is required, we can always convert a `Date` to a `DateTime` to obtain the desired result.

**Using -**

```d4
select Date() - Date(1945,5,8); // Timespan since VE Day
select ToString(571951 - 261797) + " square miles"; // difference in area of AK and TX
```

```text
3082wks 6days
310154 square miles
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [System.iNegate (`-`)](System.iNegate.md)
