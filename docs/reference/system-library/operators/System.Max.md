# System.Max

**Declarations**

```d4
create operator .System.Max(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Byte
 class "System.ByteMaxNode";
create operator .System.Max(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Short
 class "System.ShortMaxNode";
create operator .System.Max(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Integer
 class "System.IntegerMaxNode";
create operator .System.Max(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Long
 class "System.LongMaxNode";
create operator .System.Max(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalMaxNode";
create operator .System.Max(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.TimeSpan
 class "System.TimeSpanMaxNode";
create operator .System.Max(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.DateTime
 class "System.DateTimeMaxNode";
create operator .System.Max(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Date
 class "System.DateMaxNode";
create operator .System.Max(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Time
 class "System.TimeMaxNode";
create operator .System.Max(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Money
 class "System.MoneyMaxNode";
create operator .System.Max(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.VersionNumber
 class "System.VersionNumberMaxNode";
create aggregate operator .System.Max(AValue : System.Integer) : System.Integer
initialization
 class "System.IntegerInitializationNode"
aggregation
 class "System.IntegerMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.Decimal) : System.Decimal
initialization
 class "System.DecimalInitializationNode"
aggregation
 class "System.DecimalMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.Money) : System.Money
initialization
 class "System.MoneyInitializationNode"
aggregation
 class "System.MoneyMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.String) : System.String
initialization
 class "System.StringInitializationNode"
aggregation
 class "System.StringMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.VersionNumber) : System.VersionNumber
initialization
 class "System.EmptyFinalizationNode"
aggregation
 class "System.VersionNumberMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.TimeSpan) : System.TimeSpan
initialization
 class "System.TimeSpanMaxInitializationNode"
aggregation
 class "System.TimeSpanMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.DateTime) : System.DateTime
initialization
 class "System.DateTimeMaxInitializationNode"
aggregation
 class "System.DateTimeMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.Date) : System.Date
initialization
 class "System.DateMaxInitializationNode"
aggregation
 class "System.DateMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Max(AValue : System.Time) : System.Time
initialization
 class "System.TimeMaxInitializationNode"
aggregation
 class "System.TimeMaxAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
```

**Remarks**

The `Max` aggregate operator returns the largest value in a given column. `Max` returns nil when the input contains no rows. Only rows that have a value for the specified column are considered in the aggregation.

The `Max` scalar operator returns the larger of the two arguments. If both arguments are nil, the operator returns nil. If only one argument is nil, the non-nil argument is returned.

**Using System.Max**

```d4
select Libraries
 where Version = Max(Version from Libaries)
 over { Name, Version };

select Max(3, 5);
```

```text
Name   Version
------ --------------
System 2.0.1592.24169

5
```

**See Also**

[System.Min](System.Min.md) \| [Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
