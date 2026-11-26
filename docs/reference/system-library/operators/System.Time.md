# System.Time

**Declarations**

```d4
create operator .System.Time(const Hour : System.Integer, const Minute : System.Integer, const Second : System.Integer) : System.Time
 class "System.SystemTimeTimeSecondsSelectorNode";
create operator .System.Time(const AHours : System.Integer, const AMinutes : System.Integer) : System.Time
 class "System.SystemTimeTimeMinutesSelectorNode";
create operator .System.Time() : System.Time
 class "System.TimeNode" static tags { DAE.IsDeterministic = "False" };
```

**Remarks**

`Time` returns a `Time` representing the current time. Unlike the `DateTime` operator, this operator does not return the Date. Only the time portion is returned.

Note that when the `Time` operator is invoked within a transaction, it will return the start time of the outermost transaction. This allows the `Time` operator to have the *repeatable* characteristic, although it is still non-deterministic. To return the actual time independent of the current transaction, use the `ActualTime` operator.

Note also that the `Time` operator invoked with no arguments will always return the server time, regardless of whether the invocation occurs server-side, or in a client-side context. This means in particular that expressions involving this operator are not remotable and will force a server round-trip when used in potentially client-side contexts such as proposables. The `ActualTime` operator can be used to obtain the machine-dependent time in a client-side context.

**Using System.Time**

```d4
select TableDee add { Time() Time };
```

```text
Time
--------
14:28:56
```

**See Also**

[System.Time Scalar Type](../types/System.Time.md) \| [System.ActualTime](System.ActualTime.md) \| [System.DateTime](System.DateTime.md) \| [System.Date](System.Date.md) \| [System.TimeSpan](../types/System.TimeSpan.md) \| [Conveyors](../../../developers-guide/d4-catalog.md#conveyors) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToTime](System.ToTime.md)
