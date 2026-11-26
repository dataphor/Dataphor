# System.ActualDate

**Remarks**

`ActualDate` returns a `Date` value representing the current date. Unlike the `ActualDateTime` operator, this operator does not return the time. Only the date portion is returned.

Note that `ActualDate` will always return the current date. It is not affected by the current transaction. As a result, the operator is non-repeatable, and cannot be used in contexts that require repeatable expressions, such as a `where` clause. For a repeatable invocation, use the `Date` operator.

Note also that the `ActualDate` operator will return the date for the machine on which it is running. In particular, if the operator is invoked in a client-side context such as a proposable call, the client date will be returned, rather than the server date. As a result, expressions involving this operator are remotable. To ensure the server date is always obtained, use the `Date` operator.

**Using System.ActualDate**

```d4
select TableDee add { ActualDate() ActualDate };
```

```text
ActualDate
--------
14:28:56
```

**See Also**

[System.Date Scalar Type](../types/System.Date.md) \| [System.Date](System.Date.md) \| [System.ActualDateTime](System.ActualDateTime.md) \| [System.ActualTime](System.ActualTime.md) \| [Type Conversions](../../../developers-guide/d4-language-elements.md#type-conversions) \| [System.ToDate](System.ToDate.md)
