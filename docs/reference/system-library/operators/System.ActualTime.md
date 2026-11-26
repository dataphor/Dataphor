# System.ActualTime

**Remarks**

`ActualTime` returns a `Time` value representing the current time. Unlike the `ActualDateTime` operator, this operator does not return the Date. Only the time portion is returned.

Note that `ActualTime` will always return the server time. It is not affected by the current transaction. As a result, the operator is non-repeatable, and cannot be used in contexts that require repeatable expressions, such as a `where` clause. For a repeatable invocation, use the `Time` operator.

Note also that the `ActualTime` operator will return the time for the machine on which it is running. In particular, if the operator is invoked in a client-side context such as a proposable call, the client time will be returned, rather than the server time. As a result, expressions involving this operator are remotable. To ensure the server time is always obtained, use the `Time` operator.

**Using System.ActualTime**

```d4
select TableDee add { ActualTime() ActualTime };
```

```text
ActualTime
--------
14:28:56
```

**See Also**

[System.Time Scalar Type](../types/System.Time.md) \| [System.Time](System.Time.md) \| [System.ActualDateTime](System.ActualDateTime.md) \| [System.ActualDate](System.ActualDate.md) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToTime](System.ToTime.md)
