# System.Avg

**Declarations**

```d4
create aggregate operator .System.Avg(AValue : System.Integer) : System.Decimal
initialization
 class "System.IntegerAvgInitializationNode"
aggregation
 class "System.IntegerAvgAggregationNode"
finalization
 class "System.IntegerAvgFinalizationNode" static tags { DAE.Initialization.StackDisplacement = "1", DAE.IsNilable = "True" };
create aggregate operator .System.Avg(AValue : System.Decimal) : System.Decimal
initialization
 class "System.DecimalAvgInitializationNode"
aggregation
 class "System.DecimalAvgAggregationNode"
finalization
 class "System.DecimalAvgFinalizationNode" static tags { DAE.Initialization.StackDisplacement = "1", DAE.IsNilable = "True" };
create aggregate operator .System.Avg(AValue : System.Money) : System.Money
initialization
 class "System.MoneyAvgInitializationNode"
aggregation
 class "System.MoneyAvgAggregationNode"
finalization
 class "System.MoneyAvgFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Avg(AValue : System.TimeSpan) : System.TimeSpan
initialization
 class "System.TimeSpanAvgInitializationNode"
aggregation
 class "System.TimeSpanAvgAggregationNode"
finalization
 class "System.TimeSpanAvgFinalizationNode" static tags { DAE.Initialization.StackDisplacement = "1", DAE.IsNilable = "True" };
```

**Remarks**

The `Avg` aggregate operator computes the average value of the values in a given column. `Avg` uses the formula `Sum(<values>) / Count(<values>)` to compute the average. `Avg` returns nil when the input contains no rows. `Avg` returns a value of type `System.Decimal`, even if the column being averaged is an integer type. Only rows that have a value for the specified column are considered in the aggregation.

**Using System.Avg**

```d4
select Employee group add { Avg(ID) AvgID };

select Avg(ID from Employee);
```

```text
AvgID
-----
3.5
```

**See Also**

[Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
