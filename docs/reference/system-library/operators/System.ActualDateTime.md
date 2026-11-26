# System.ActualDateTime

**Remarks**

`ActualDateTime` returns a `DateTime` value representing the current date and time.

Note that `ActualDateTime` will always return the server date and time. It is not affected by the current transaction. As a result, the operator is non-repeatable, and cannot be used in contexts that require repeatable expressions, such as a `where` clause. For a repeatable invocation, use the `DateTime` operator.

Note also that the `ActualDateTime` operator will return the date and time for the machine on which it is running. In particular, if the operator is invoked in a client-side context such as a proposable call, the client date and time will be returned, rather than the server date and time. As a result, expressions involving this operator are remotable. To ensure the server date and time is always obtained, use the `DateTime` operator.

**Using System.ActualDateTime**

```d4
select TableDee add { ActualDateTime() ActualDateTime };
```

```text
ActualDateTime
--------
14:28:56
```

**See Also**

[System.DateTime Scalar Type](../types/System.DateTime.md) \| [System.DateTime](System.DateTime.md) \| [System.ActualDate](System.ActualDate.md) \| [System.ActualTime](System.ActualTime.md) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToDateTime](System.ToDateTime.md)
