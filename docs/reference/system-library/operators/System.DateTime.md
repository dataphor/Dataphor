# System.DateTime

**Declarations**

```d4
create operator .System.DateTime(const Year : System.Integer, const Month : System.Integer, const Day : System.Integer, const Hour : System.Integer, const Minute : System.Integer, const Second : System.Integer) : System.DateTime
 class "System.SystemDateTimeDateTimeSecondsSelectorNode";
create operator .System.DateTime(const AYears : System.Integer, const AMonths : System.Integer, const ADays : System.Integer, const AHours : System.Integer, const AMinutes : System.Integer) : System.DateTime
 class "System.SystemDateTimeDateTimeMinutesSelectorNode";
create operator .System.DateTime(const AYears : System.Integer, const AMonths : System.Integer, const ADays : System.Integer) : System.DateTime
 class "System.SystemDateTimeDateTimeDaysSelectorNode";
create operator .System.DateTime() : System.DateTime
 class "System.DateTimeNode" static tags { DAE.IsDeterministic = "False" };
```

**Remarks**

The `DateTime` operator, with no arguments, returns a `DateTime` value representing the current date and time. Unlike the `Date` operator, this operator returns both the date and time.

Note that when the `DateTime` operator is invoked within a transaction, it will return the date and time of the start time of the outermost transaction. This allows the `DateTime` operator to have the *repeatable* characteristic, although it is still non-deterministic. To return the actual date and time independent of the current transaction, use the `ActualDateTime` operator.

Note also that the `DateTime` operator invoked with no arguments will always return the server date and time, regardless of whether the invocation occurs server-side, or in a client-side context. This means in particular that expressions involving this operator are not remotable and will force a server round-trip when used in potentially client-side contexts such as proposables. The `ActualDateTime` operator can be used to obtain the machine-dependent date and time in a client-side context.

With integer year, month, day, \[hours, minutes, \[seconds\]\] specified, this operator returns a `DateTime` value representing the date and time specified.

**Using System.DateTime**

```d4
select DateTime();
select DateTime(1998, 11, 22, 10, 15);
select DateTime.Ticks(631680313950000000);
```

```text
2004-04-20 07:25:13
1998-11-22 10:15:00
2002-09-19 11:23:15
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.ActualDateTime](System.ActualDateTime.md) \| [System.Date](System.Date.md) \| [System.Time](System.Time.md) \| [System.TimeSpan](../types/System.TimeSpan.md) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToDateTime](System.ToDateTime.md)
