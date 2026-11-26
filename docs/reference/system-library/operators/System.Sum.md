# System.Sum

**Declarations**

```d4
create aggregate operator .System.Sum(AValue : System.Integer) : System.Integer
initialization
 class "System.IntegerInitializationNode"
aggregation
 class "System.IntegerSumAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Sum(AValue : System.Decimal) : System.Decimal
initialization
 class "System.DecimalInitializationNode"
aggregation
 class "System.DecimalSumAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Sum(AValue : System.Money) : System.Money
initialization
 class "System.MoneyInitializationNode"
aggregation
 class "System.MoneySumAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
create aggregate operator .System.Sum(AValue : System.TimeSpan) : System.TimeSpan
initialization
 class "System.TimeSpanInitializationNode"
aggregation
 class "System.TimeSpanSumAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
```

**Remarks**

The `Sum` aggregate operator computes the sum of the values of a given column. `Sum` returns nil when the input contains no rows.

Only rows that have a value for the specified column are considered in the aggregation.

**Using System.Sum**

```d4
select Employee group add { Sum(ID) IDSum };
```

```text
IDSum
-----
21
```

**See Also**

[Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
