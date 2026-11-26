# System.Date

**Declarations**

```d4
create operator .System.Date(const Year : System.Integer, const Month : System.Integer, const Day : System.Integer) : System.Date
 class "System.SystemDateTimeDateTimeDaysSelectorNode";
create operator .System.Date() : System.Date
 class "System.DateNode" static tags { DAE.IsDeterministic = "False" };
```

**Remarks**

The `Date` operator, with no arguments, returns a `Date` value representing the current date. Unlike the `DateTime` operator, this operator does not return the time. Only the date portion is returned.

Note that when the `Date` operator is invoked within a transaction, it will return the date portion of the start time of the outermost transaction. This allows the `Date` operator to have the *repeatable* characteristic, although it is still non-deterministic. To return the actual date independent of the current transaction, use the `ActualDate` operator.

Note also that the `Date` operator invoked with no arguments will always return the server date, regardless of whether the invocation occurs server-side, or in a client-side context. This means in particular that expressions involving this operator are not remotable and will force a server round-trip when used in potentially client-side contexts such as proposables. The `ActualDate` operator can be used to obtain the machine-dependent date in a client-side context.

With integer year, month, and day specified, this operator returns a `Date` value representing the date specified.

**Using System.Date**

```d4
select Date();
select Date(2003,1,18);
select Date.Ticks(627346080000000000);
```

```text
2004-04-20
2003-01-18
1988-12-25
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.ActualDate](System.ActualDate.md) \|[System.DateTime](System.DateTime.md) \| [System.Time](System.Time.md) \| [System.TimeSpan](../types/System.TimeSpan.md) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToDate](System.ToDate.md)
