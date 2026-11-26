# System.Min

**Declarations**

```d4
create operator .System.Min(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteMinNode";
create operator .System.Min(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortMinNode";
create operator .System.Min(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerMinNode";
create operator .System.Min(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongMinNode";
create operator .System.Min(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalMinNode";
create operator .System.Min(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.TimeSpan
 class "System.TimeSpanMinNode";
create operator .System.Min(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.DateTime
 class "System.DateTimeMinNode";
create operator .System.Min(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Date
 class "System.DateMinNode";
create operator .System.Min(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Time
 class "System.TimeMinNode";
create operator .System.Min(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Money
 class "System.MoneyMinNode";
create operator .System.Min(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.VersionNumber
 class "System.VersionNumberMinNode";
create aggregate operator .System.Min(AValue : System.Integer) : System.Integer
initialization
 class "System.IntegerInitializationNode"
aggregation
 class "System.IntegerMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.Decimal) : System.Decimal
initialization
 class "System.DecimalInitializationNode"
aggregation
 class "System.DecimalMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.Money) : System.Money
initialization
 class "System.MoneyInitializationNode"
aggregation
 class "System.MoneyMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode";
create aggregate operator .System.Min(AValue : System.String) : System.String
initialization
 class "System.StringInitializationNode"
aggregation
 class "System.StringMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.VersionNumber) : System.VersionNumber
initialization
 class "System.EmptyFinalizationNode"
aggregation
 class "System.VersionNumberMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.TimeSpan) : System.TimeSpan
initialization
 class "System.TimeSpanMinInitializationNode"
aggregation
 class "System.TimeSpanMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.DateTime) : System.DateTime
initialization
 class "System.DateTimeMinInitializationNode"
aggregation
 class "System.DateTimeMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.Date) : System.Date
initialization
 class "System.DateMinInitializationNode"
aggregation
 class "System.DateMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Min(AValue : System.Time) : System.Time
initialization
 class "System.TimeMinInitializationNode"
aggregation
 class "System.TimeMinAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
```

**Remarks**

The `Min` aggregate operator returns the smallest value in a given column. `Min` returns nil when the input contains no rows. Only rows that have a value for the specified column are considered in the aggregation.

The `Min` scalar operator returns the smaller of the two arguments. If both arguments are nil, the operator returns nil. If only one argument is nil, the non-nil argument is returned.

**Using System.Min**

```d4
select Libraries
 where Version = Min(Version from Libraries)
 over { Name, Version };

select Min(3, 5);
```

```text
Name    Version
------- -------
General *

3
```

**See Also**

[System.Max](System.Max.md) \| [Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
