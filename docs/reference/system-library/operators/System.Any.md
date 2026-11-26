# System.Any

**Declarations**

```d4
create aggregate operator .System.Any(AValue : System.Boolean) : System.Boolean
initialization
 class "System.BooleanAnyInitializationNode"
aggregation
 class "System.BooleanAnyAggregationNode"
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True" };
```

**Remarks**

The `Any` aggregate operator returns true if any value in the input is true, and false otherwise. `Any` returns false when the input contains no rows. This operator is the logical equivalent of an iterated `or`. Only rows that have a value for the specified column are considered in the aggregation.

**Using System.Any**

```d4
select Employee
 add { (Name.Length() = 4) LengthIsFour };
```

```text
ID  Name    LengthIsFour
--  ----    ------------
1   Joe     False
2   John    True
3   Josh    True
4   Jeff    True
5   Jake    True
6   Jeff    True
```

```d4
select Employee
 add { (Name.Length() = 4) Length4 }
 group add {Any(Length4) AreAnyLengthOf4};

select Any(Length4 from Employee add { (Name.Length() = 4) Length4 } );
```

```text
AreAnyLengthOf4
---------------
True

True
```

**See Also**

[System.All](System.All.md) \| [Aggregate Table Operator](../../../developers-guide/d4-table-operators.md#aggregate) \| [Aggregate Operators](../../../developers-guide/d4-catalog.md#aggregate-operators)
