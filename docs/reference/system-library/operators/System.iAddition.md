# System.iAddition

**Declarations**

```d4
create operator .System.iAddition(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.String, const ARightValue : System.String) : System.String
 class "System.StringAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Money
 class "System.MoneyAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.TimeSpan
 class "System.TimeSpanAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.DateTime, const ARightValue : System.TimeSpan) : System.DateTime
 class "System.DateTimeTimeSpanAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Date, const ARightValue : System.TimeSpan) : System.Date
 class "System.DateTimeSpanAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Time, const ARightValue : System.TimeSpan) : System.Time
 class "System.TimeTimeSpanAdditionNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iAddition(const ALeftValue : System.Name, const ARightValue : System.Name) : System.String
 class "System.StringAdditionNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

The `+` operator adds two values together. For numeric operands, this operator is arithmetic sum. For string operands, this operator is string concatenation. For date/time operands, this operator can be used to add a `TimeSpan` value to a `Date`, `Time`, `DateTime`, or `TimeSpan` value. For these operators, the result type is a `Date`, `Time`, `DateTime`, or `TimeSpan`, respectively.

Note that when adding a `TimeSpan` value to a date/time value, the type of the result is unchanged. This can lead to some unexpected results, for example:

```d4
select Date(2005, 10, 20) + Seconds(1);
select DateTime(2005, 10, 20) + Seconds(1);
```

```text
10/20/2005
10/20/2005 12:00:01 AM
```

The effect of the first invocation of `+` is undetectable because the result is "rounded" to the nearest `Date` value. Because changing precision in date/time operations is unusual, this behavior results in less need for explicit conversion, in general. In other words, if we are starting with a `Date` value, we are generally dealing with days, or multiples of days when we perform date manipulation. Of course, if further granularity is required, we can always convert a `Date` to a `DateTime` to obtain the desired result.

`System.iAddition` is the operator name for the `+` operator.

The result of evaluating this operator is nil if either or both of its arguments are nil.

**Using +**

```d4
select TableDee add { 4.7 + 6.1 sum };
select Manager add { Manager_ID + Employee_ID sum};
```

```text
sum
----
10.8

Manager_ID Employee_ID sum
---------- ----------- ---
1          2           3
1          3           4
2          4           6
2          6           8
4          5           9
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Arithmetic Operators](System.ArithmeticOperators.md) \| [String Operators](System.StringOperators.md)

## System.iAddition (String)

**Remarks**

`iAddition` is an Arithmetic and a String operator.

**See**

[System.iAddition Arithmetic Operator](System.iAddition.md)
